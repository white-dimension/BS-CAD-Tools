using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using BS.CAD.Tools.Models;
using BS.CAD.Tools.Services;
using BS.CAD.Tools.Utils;
using WinForms = System.Windows.Forms;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace BS.CAD.Tools.Views
{
    public partial class MainPanelView : WpfUserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly SettingsService _settingsService = new();

        public MainPanelView()
        {
            InitializeComponent();
            LoadInstalledInputLanguages();
            LoadLocalFonts();
            ApplyModuleVisibility();
            SetStatus("就绪");

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            _timer.Tick += (s, e) => UpdateCurrentIMEDisplay();
            _timer.Start();
        }

        private void ApplyModuleVisibility()
        {
            try
            {
                var settings = _settingsService.Load();
                LayerToolsPanel.Visibility = IsEnabled(settings, SettingsService.LayerTools) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                ImeToolsPanel.Visibility = IsEnabled(settings, SettingsService.ImeTools) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                FontToolsPanel.Visibility = IsEnabled(settings, SettingsService.FontTools) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                CleanupToolsPanel.Visibility = IsEnabled(settings, SettingsService.CleanupTools) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                StandardToolsPanel.Visibility = IsEnabled(settings, SettingsService.StandardTools) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatus("模块设置读取失败，已使用默认显示。", true);
            }
        }

        private static bool IsEnabled(AppSettings settings, string moduleKey)
        {
            return settings.EnabledModules.TryGetValue(moduleKey, out bool enabled) && enabled;
        }

        private void OnBtnModuleSettingsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new ModuleSettingsWindow(_settingsService);
                bool? result = window.ShowDialog();
                if (result == true && window.SettingsSaved)
                {
                    ApplyModuleVisibility();
                    SetStatus("模块设置已保存，主面板已刷新。");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatus("模块设置窗口打开失败。", true);
                System.Windows.MessageBox.Show("模块设置窗口打开失败，请查看日志。", "CAD助手", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateCurrentIMEDisplay()
        {
            try
            {
                var current = WinForms.InputLanguage.CurrentInputLanguage;
                string iso = current.Culture.TwoLetterISOLanguageName ?? string.Empty;
                string layoutName = GetInputLanguageDisplayName(current);

                if (string.Equals(iso, "zh", StringComparison.OrdinalIgnoreCase))
                {
                    TxtCurrentIME.Text = $"当前状态：中文输入 · {layoutName}";
                }
                else if (string.Equals(iso, "en", StringComparison.OrdinalIgnoreCase))
                {
                    TxtCurrentIME.Text = $"当前状态：英文输入 · {layoutName}";
                }
                else
                {
                    TxtCurrentIME.Text = $"当前状态：其他输入 · {layoutName}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                TxtCurrentIME.Text = "当前状态：无法读取输入法";
            }
        }

        private void LoadLocalFonts()
        {
            try
            {
                var allFonts = new List<string>();
                string acadPath = HostApplicationServices.Current.GetEnvironmentVariable("ACAD");

                if (!string.IsNullOrEmpty(acadPath))
                {
                    foreach (string path in acadPath.Split(';'))
                    {
                        if (!Directory.Exists(path))
                            continue;

                        foreach (string file in Directory.GetFiles(path, "*.shx"))
                        {
                            allFonts.Add(Path.GetFileName(file).ToLowerInvariant());
                        }
                    }
                }

                using (var installedFonts = new InstalledFontCollection())
                {
                    foreach (var family in installedFonts.Families)
                    {
                        allFonts.Add(family.Name);
                    }
                }

                var finalSortedList = allFonts
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(f => f)
                    .ToList();

                ComboShx.Items.Clear();
                ComboBoxBig.Items.Clear();
                foreach (string font in finalSortedList)
                {
                    ComboShx.Items.Add(font);
                    ComboBoxBig.Items.Add(font);
                }

                ComboShx.Text = finalSortedList.Contains("txt.shx") ? "txt.shx" : finalSortedList.FirstOrDefault() ?? "txt.shx";
                ComboBoxBig.Text = finalSortedList.Contains("gbcbig.shx") ? "gbcbig.shx" : "gbcbig.shx";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatus("字体列表读取失败，仍可手动输入字体名。", true);
            }
        }

        private void LoadInstalledInputLanguages()
        {
            try
            {
                ComboChineseIME.Items.Clear();
                ComboEnglishIME.Items.Clear();
                ComboChineseIME.IsEnabled = true;
                ComboEnglishIME.IsEnabled = true;

                var settings = _settingsService.Load();
                var preferredChineseList = new List<InputLanguageItem>();
                var fallbackChineseList = new List<InputLanguageItem>();
                var preferredEnglishList = new List<InputLanguageItem>();
                var fallbackEnglishList = new List<InputLanguageItem>();

                foreach (WinForms.InputLanguage lang in WinForms.InputLanguage.InstalledInputLanguages)
                {
                    string iso = lang.Culture.TwoLetterISOLanguageName ?? string.Empty;
                    var item = new InputLanguageItem
                    {
                        DisplayName = GetInputLanguageDisplayName(lang),
                        CultureName = lang.Culture.Name,
                        HklHex = InputLanguageItem.FormatHkl(lang.Handle),
                        Handle = lang.Handle
                    };

                    if (string.Equals(iso, "zh", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsLikelyChineseIme(lang))
                        {
                            preferredChineseList.Add(item);
                        }
                        else
                        {
                            fallbackChineseList.Add(item);
                        }
                    }
                    else if (string.Equals(iso, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsLikelyEnglishKeyboard(lang))
                        {
                            preferredEnglishList.Add(item);
                        }
                        else
                        {
                            fallbackEnglishList.Add(item);
                        }
                    }
                }

                var chineseList = preferredChineseList.Count > 0 ? preferredChineseList : fallbackChineseList;
                var englishList = preferredEnglishList.Count > 0 ? preferredEnglishList : fallbackEnglishList;

                foreach (var item in chineseList.DistinctBy(x => x.HklHex))
                {
                    ComboChineseIME.Items.Add(item);
                }

                foreach (var item in englishList.DistinctBy(x => x.HklHex))
                {
                    ComboEnglishIME.Items.Add(item);
                }

                RestoreSavedImeSelection(settings, chineseList, englishList);
                ApplySelectedImeHandles();

                if (ComboChineseIME.Items.Count == 0)
                {
                    ComboChineseIME.IsEnabled = false;
                    ComboChineseIME.Items.Add("未检测到中文输入法");
                    ComboChineseIME.SelectedIndex = 0;
                }

                if (ComboEnglishIME.Items.Count == 0)
                {
                    ComboEnglishIME.IsEnabled = false;
                    ComboEnglishIME.Items.Add("未检测到英文键盘");
                    ComboEnglishIME.SelectedIndex = 0;
                }

                UpdateCurrentIMEDisplay();
                SetStatus($"输入法检测完成：中文 {ComboChineseIME.Items.Count} 个，英文 {ComboEnglishIME.Items.Count} 个。", ComboChineseIME.Items.Count == 0 || ComboEnglishIME.Items.Count == 0);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatus("输入法列表读取失败。", true);
                TxtCurrentIME.Text = "当前状态：无法读取输入法";
            }
        }

        private void RestoreSavedImeSelection(AppSettings settings, List<InputLanguageItem> chineseList, List<InputLanguageItem> englishList)
        {
            ComboChineseIME.SelectedItem = FindImeByHandleOrName(chineseList, settings.TargetChineseHkl, settings.ChineseImeName);
            if (ComboChineseIME.SelectedItem == null && ComboChineseIME.Items.Count > 0)
            {
                ComboChineseIME.SelectedIndex = 0;
            }

            ComboEnglishIME.SelectedItem = FindImeByHandleOrName(englishList, settings.TargetEnglishHkl, settings.EnglishImeName);
            if (ComboEnglishIME.SelectedItem == null && ComboEnglishIME.Items.Count > 0)
            {
                ComboEnglishIME.SelectedIndex = 0;
            }
        }

        private static InputLanguageItem? FindImeByHandleOrName(List<InputLanguageItem> items, string? hklHex, string? displayName)
        {
            if (!string.IsNullOrWhiteSpace(hklHex))
            {
                var handleMatch = items.FirstOrDefault(item => string.Equals(item.HklHex, hklHex, StringComparison.OrdinalIgnoreCase));
                if (handleMatch != null)
                {
                    return handleMatch;
                }
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return items.FirstOrDefault(item => string.Equals(item.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private void ApplySelectedImeHandles()
        {
            if (ComboChineseIME.SelectedItem is InputLanguageItem chineseItem)
            {
                CadApp.TargetChineseHKL = chineseItem.Handle;
            }

            if (ComboEnglishIME.SelectedItem is InputLanguageItem englishItem)
            {
                CadApp.TargetEnglishHKL = englishItem.Handle;
            }
        }

        private void OnBtnSaveIMESettingsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ComboChineseIME.SelectedItem is not InputLanguageItem chiItem || ComboEnglishIME.SelectedItem is not InputLanguageItem engItem)
                {
                    SetStatus("请先选择中文输入法和英文键盘。", true);
                    return;
                }

                CadApp.TargetChineseHKL = chiItem.Handle;
                CadApp.TargetEnglishHKL = engItem.Handle;

                var settings = _settingsService.Load();
                settings.TargetChineseHkl = chiItem.HklHex;
                settings.TargetEnglishHkl = engItem.HklHex;
                settings.ChineseImeName = chiItem.DisplayName;
                settings.EnglishImeName = engItem.DisplayName;

                if (!_settingsService.Save(settings))
                {
                    SetStatus("输入法设置保存失败，请查看日志。", true);
                    return;
                }

                SetStatus($"输入法自动切换已启动：中文 {chiItem.DisplayName}，英文 {engItem.DisplayName}。");
                System.Windows.MessageBox.Show(
                    $"输入法自动切换已启动。\n\n中文：{chiItem.DisplayName}\n英文：{engItem.DisplayName}",
                    "CAD助手",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatus("输入法设置保存失败。", true);
            }
        }

        private static string GetInputLanguageDisplayName(WinForms.InputLanguage lang)
        {
            if (!string.IsNullOrWhiteSpace(lang.LayoutName))
            {
                return lang.LayoutName;
            }

            if (!string.IsNullOrWhiteSpace(lang.Culture.DisplayName))
            {
                return lang.Culture.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(lang.Culture.Name))
            {
                return lang.Culture.Name;
            }

            return "未知输入法";
        }

        private static bool IsLikelyChineseIme(WinForms.InputLanguage lang)
        {
            string name = GetInputLanguageDisplayName(lang).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string lower = name.ToLowerInvariant();

            string[] imeKeywords =
            {
                "拼音", "双拼", "五笔", "郑码", "仓颉", "速成", "注音", "输入法", "搜狗", "微软", "qq", "微信", "小鹤"
            };

            if (imeKeywords.Any(keyword => lower.Contains(keyword.ToLowerInvariant())))
            {
                return true;
            }

            string[] keyboardKeywords =
            {
                "美式键盘", "keyboard", "us", "abc", "键盘"
            };

            return !keyboardKeywords.Any(keyword => lower.Contains(keyword));
        }

        private static bool IsLikelyEnglishKeyboard(WinForms.InputLanguage lang)
        {
            string name = GetInputLanguageDisplayName(lang).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string lower = name.ToLowerInvariant();
            return lower.Contains("keyboard") || lower.Contains("us") || lower.Contains("美式") || lower.Contains("英语");
        }

        private void OnBtnLayerManagerClick(object sender, RoutedEventArgs e)
        {
            ExecuteCommand("LY ", "打开图层管理器");
        }

        private void OnBtnTextStandardClick(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDanger("标准化文字会修改全图文字样式，建议先保存图纸。\n\n是否继续执行 TS？"))
                return;

            CadApp.SwitchToIME(CadApp.TargetChineseHKL);
            ExecuteCommand("TS ", "标准化文字");
        }

        private void OnBtnStandardClick(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDanger("BZ 目前是测试入口，暂未对接 BS-CAD-Standard。\n\n是否继续执行标准环境初始化？"))
                return;

            CadApp.SwitchToIME(CadApp.TargetEnglishHKL);
            ExecuteCommand("BZ ", "标准环境初始化");
        }

        private void OnBtnByLayerClick(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDanger("SETBYLAYER 会把全图对象的颜色、线型、线宽改为随层。\n\n这会影响图纸显示效果，建议先保存图纸。是否继续？"))
                return;

            ExecuteCommand("SETBYLAYER ", "全图物体随层");
        }

        private void OnBtnFixFontsClick(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDanger("FIXFONTS 会修改全图文字样式。\n\n建议先保存图纸。是否继续？"))
                return;

            CadApp.SwitchToIME(CadApp.TargetChineseHKL);
            CadApp.SelectedShx = ComboShx.Text;
            CadApp.SelectedBigFont = ComboBoxBig.Text;
            ExecuteCommand("FIXFONTS ", "修复全图字体");
        }

        private void ExecuteCommand(string command, string description)
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    SetStatus("没有活动图纸，无法执行命令。", true);
                    return;
                }

                doc.SendStringToExecute(command, true, false, false);
                SetStatus($"已发送命令：{description}（{command.Trim()}）");
                Logger.Info($"MainPanel 执行命令: {command.Trim()} / {description}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatus($"命令执行失败：{description}", true);
            }
        }

        private static bool ConfirmDanger(string message)
        {
            return System.Windows.MessageBox.Show(
                message,
                "CAD助手 - 操作确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void SetStatus(string message, bool isError = false)
        {
            if (TxtStatus == null)
                return;

            TxtStatus.Text = message;
            TxtStatus.Foreground = isError
                ? System.Windows.Media.Brushes.IndianRed
                : System.Windows.Media.Brushes.LightGray;
        }
    }
}

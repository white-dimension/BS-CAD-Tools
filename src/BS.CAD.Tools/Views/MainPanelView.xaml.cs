using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using BS.CAD.Tools.Models;
using BS.CAD.Tools.Services;
using BS.CAD.Tools.Utils;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace BS.CAD.Tools.Views
{
    public partial class MainPanelView : WpfUserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly SettingsService _settingsService = new();

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ImmGetDescription(IntPtr hKL, StringBuilder lpszDescription, uint uBufLen);

        public MainPanelView()
        {
            InitializeComponent();
            ConfigureIme();
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

        private void ConfigureIme()
        {
            ImeManager.enableEnglishIme(ComboChineseIME);
            ImeManager.enableEnglishIme(ComboEnglishIME);
            ImeManager.enableEnglishIme(ComboShx);
            ImeManager.enableEnglishIme(ComboBoxBig);
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
                var secondaryChineseList = new List<InputLanguageItem>();
                var keyboardFallbackChineseList = new List<InputLanguageItem>();
                var preferredEnglishList = new List<InputLanguageItem>();
                var fallbackEnglishList = new List<InputLanguageItem>();

                foreach (WinForms.InputLanguage lang in WinForms.InputLanguage.InstalledInputLanguages)
                {
                    var item = new InputLanguageItem
                    {
                        DisplayName = GetInputLanguageDisplayName(lang),
                        CultureName = lang.Culture.Name,
                        HklHex = InputLanguageItem.FormatHkl(lang.Handle),
                        Handle = lang.Handle
                    };

                    string iso = lang.Culture.TwoLetterISOLanguageName ?? string.Empty;

                    if (IsPreferredChineseIme(lang))
                    {
                        preferredChineseList.Add(item);
                        continue;
                    }

                    if (string.Equals(iso, "zh", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsKeyboardLayoutOnly(lang))
                        {
                            keyboardFallbackChineseList.Add(item);
                        }
                        else
                        {
                            secondaryChineseList.Add(item);
                        }

                        continue;
                    }

                    if (IsLikelyEnglishKeyboard(lang))
                    {
                        preferredEnglishList.Add(item);
                    }
                    else if (string.Equals(iso, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        fallbackEnglishList.Add(item);
                    }
                }

                var chineseList = preferredChineseList.Count > 0
                    ? preferredChineseList
                    : secondaryChineseList.Count > 0
                        ? secondaryChineseList
                        : keyboardFallbackChineseList;

                var englishList = preferredEnglishList.Count > 0
                    ? preferredEnglishList
                    : fallbackEnglishList;

                foreach (var item in UniqueByHandle(chineseList))
                {
                    ComboChineseIME.Items.Add(item);
                }

                foreach (var item in UniqueByHandle(englishList))
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
                else if (preferredChineseList.Count == 0)
                {
                    SetStatus("未检测到微信/搜狗/百度/Rime/拼音等真实中文输入法，已使用中文键盘布局兜底。", true);
                }

                if (ComboEnglishIME.Items.Count == 0)
                {
                    ComboEnglishIME.IsEnabled = false;
                    ComboEnglishIME.Items.Add("未检测到英文键盘");
                    ComboEnglishIME.SelectedIndex = 0;
                }

                UpdateCurrentIMEDisplay();

                if (preferredChineseList.Count > 0 && ComboEnglishIME.Items.Count > 0)
                {
                    SetStatus($"输入法检测完成：中文输入法 {ComboChineseIME.Items.Count} 个，英文键盘 {ComboEnglishIME.Items.Count} 个。");
                }
                else if (ComboEnglishIME.Items.Count == 0)
                {
                    SetStatus("未检测到英文键盘，请在 Windows 语言设置中添加英语/美式键盘。", true);
                }
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
            string layoutName = lang.LayoutName?.Trim() ?? string.Empty;
            string imeDescription = GetImeDescription(lang.Handle);
            string registryName = GetKeyboardLayoutText(lang.Handle);

            if (!string.IsNullOrWhiteSpace(imeDescription))
            {
                if (!string.IsNullOrWhiteSpace(layoutName)
                    && !string.Equals(imeDescription, layoutName, StringComparison.OrdinalIgnoreCase)
                    && !layoutName.Contains(imeDescription, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{imeDescription} ({layoutName})";
                }

                return imeDescription;
            }

            if (!string.IsNullOrWhiteSpace(registryName)
                && !string.Equals(registryName, layoutName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(layoutName))
                {
                    return $"{registryName} ({layoutName})";
                }

                return registryName;
            }

            if (!string.IsNullOrWhiteSpace(layoutName))
            {
                return layoutName;
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

        private static string GetImeDescription(IntPtr handle)
        {
            try
            {
                var buffer = new StringBuilder(256);
                uint length = ImmGetDescription(handle, buffer, (uint)buffer.Capacity);
                return length > 0 ? buffer.ToString().Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetKeyboardLayoutText(IntPtr handle)
        {
            try
            {
                string layoutId = (handle.ToInt64() & 0xFFFFFFFF).ToString("X8");
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\{layoutId}");
                return (key?.GetValue("Layout Text") as string)?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


        private static IEnumerable<InputLanguageItem> UniqueByHandle(IEnumerable<InputLanguageItem> items)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (seen.Add(item.HklHex))
                {
                    yield return item;
                }
            }
        }

        private static bool IsPreferredChineseIme(WinForms.InputLanguage lang)
        {
            string name = BuildInputLanguageSearchText(lang);
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string[] chineseImeKeywords =
            {
                "微信", "wechat", "weixin",
                "搜狗", "sogou",
                "百度", "baidu",
                "微软拼音", "microsoft pinyin", "ms pinyin",
                "拼音", "pinyin",
                "双拼", "五笔", "wubi",
                "qq拼音", "qq input", "qq",
                "手心", "小鹤", "自然码", "紫光", "谷歌拼音", "google pinyin",
                "rime", "小狼毫", "weasel", "中州韵", "ibus-rime",
                "仓颉", "cangjie", "速成", "quick", "注音", "zhuyin", "bopomofo",
                "输入法", "ime", "input method"
            };

            if (!chineseImeKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return !IsKeyboardLayoutOnly(lang) || ContainsStrongChineseImeBrand(name);
        }

        private static bool ContainsStrongChineseImeBrand(string searchText)
        {
            string[] strongBrands =
            {
                "微信", "wechat", "weixin",
                "搜狗", "sogou",
                "百度", "baidu",
                "微软拼音", "microsoft pinyin",
                "拼音", "pinyin",
                "双拼", "五笔", "wubi",
                "rime", "小狼毫", "weasel",
                "qq拼音", "手心", "小鹤", "自然码"
            };

            return strongBrands.Any(keyword => searchText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKeyboardLayoutOnly(WinForms.InputLanguage lang)
        {
            string name = BuildInputLanguageSearchText(lang);

            string[] keyboardOnlyKeywords =
            {
                "中文(简体) - 美式键盘",
                "中文(繁体) - 美式键盘",
                "chinese (simplified) - us keyboard",
                "chinese (traditional) - us keyboard",
                "美式键盘",
                "us keyboard",
                "united states",
                "keyboard",
                "abc",
                "dvorak"
            };

            return keyboardOnlyKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsLikelyEnglishKeyboard(WinForms.InputLanguage lang)
        {
            string iso = lang.Culture.TwoLetterISOLanguageName ?? string.Empty;
            string name = BuildInputLanguageSearchText(lang);

            if (!string.Equals(iso, "en", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] englishKeyboardKeywords =
            {
                "美式键盘",
                "us keyboard",
                "united states",
                "english",
                "英语",
                "keyboard",
                "abc"
            };

            return englishKeyboardKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                   || !IsPreferredChineseIme(lang);
        }

        private static string BuildInputLanguageSearchText(WinForms.InputLanguage lang)
        {
            return string.Join(" ",
                lang.LayoutName ?? string.Empty,
                lang.Culture.DisplayName ?? string.Empty,
                lang.Culture.EnglishName ?? string.Empty,
                lang.Culture.Name ?? string.Empty).Trim();
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

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BS.CAD.Tools;
using WinForms = System.Windows.Forms;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace BS.CAD.Tools.Utils
{
    public static class ImeManager
    {
        private static readonly CultureInfo ChineseCulture = CultureInfo.GetCultureInfo("zh-CN");
        private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

        public static void enableChineseIme(FrameworkElement component)
        {
            Register(component, preferChinese: true);
        }

        public static void enableEnglishIme(FrameworkElement component)
        {
            Register(component, preferChinese: false);
        }

        private static void Register(FrameworkElement component, bool preferChinese)
        {
            try
            {
                if (component == null)
                    return;

                component.GotKeyboardFocus -= OnChineseFocus;
                component.GotKeyboardFocus -= OnEnglishFocus;
                component.GotKeyboardFocus += preferChinese ? OnChineseFocus : OnEnglishFocus;

                ApplyWpfImePreference(component, preferChinese);

                if (component is WpfComboBox comboBox)
                {
                    component.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            comboBox.ApplyTemplate();
                            if (FindVisualChild<WpfTextBox>(comboBox) is WpfTextBox editor)
                            {
                                if (preferChinese)
                                {
                                    enableChineseIme(editor);
                                }
                                else
                                {
                                    enableEnglishIme(editor);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"ComboBox IME editor bind failed: {ex.Message}");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"IME register failed: {ex.Message}");
            }
        }

        private static void OnChineseFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                SwitchIme(element, preferChinese: true);
            }
        }

        private static void OnEnglishFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                SwitchIme(element, preferChinese: false);
            }
        }

        private static void SwitchIme(FrameworkElement element, bool preferChinese)
        {
            try
            {
                ApplyWpfImePreference(element, preferChinese);
                ApplyInputLanguage(preferChinese);
                CadApp.SwitchToIME(preferChinese ? CadApp.TargetChineseHKL : CadApp.TargetEnglishHKL);
            }
            catch (Exception ex)
            {
                Logger.Warn($"IME switch failed ({(preferChinese ? "Chinese" : "English")}): {ex.Message}");
            }
        }

        private static void ApplyWpfImePreference(FrameworkElement element, bool preferChinese)
        {
            try
            {
                InputMethod.SetIsInputMethodEnabled(element, preferChinese);
                InputMethod.SetPreferredImeState(element, preferChinese ? InputMethodState.On : InputMethodState.Off);

                if (preferChinese)
                {
                    InputMethod.SetPreferredImeConversionMode(element, ImeConversionModeValues.Native);
                }
                else
                {
                    InputMethod.SetPreferredImeConversionMode(element, ImeConversionModeValues.Alphanumeric);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"WPF IME preference failed: {ex.Message}");
            }
        }

        private static void ApplyInputLanguage(bool preferChinese)
        {
            try
            {
                IntPtr hkl = preferChinese ? CadApp.TargetChineseHKL : CadApp.TargetEnglishHKL;
                if (hkl != IntPtr.Zero)
                {
                    var lang = FindInputLanguageByHandle(hkl);
                    if (lang != null)
                    {
                        WinForms.InputLanguage.CurrentInputLanguage = lang;
                        InputLanguageManager.Current.CurrentInputLanguage = lang.Culture;
                        return;
                    }
                }

                var targetCulture = preferChinese ? ChineseCulture : EnglishCulture;
                var installed = WinForms.InputLanguage.InstalledInputLanguages
                    .Cast<WinForms.InputLanguage>()
                    .FirstOrDefault(lang => string.Equals(lang.Culture.TwoLetterISOLanguageName, targetCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));

                if (installed != null)
                {
                    WinForms.InputLanguage.CurrentInputLanguage = installed;
                    InputLanguageManager.Current.CurrentInputLanguage = installed.Culture;
                }
                else
                {
                    InputLanguageManager.Current.CurrentInputLanguage = targetCulture;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"InputLanguage switch failed: {ex.Message}");
            }
        }

        private static WinForms.InputLanguage? FindInputLanguageByHandle(IntPtr hkl)
        {
            try
            {
                return WinForms.InputLanguage.InstalledInputLanguages
                    .Cast<WinForms.InputLanguage>()
                    .FirstOrDefault(lang => lang.Handle == hkl);
            }
            catch (Exception ex)
            {
                Logger.Warn($"InputLanguage lookup failed: {ex.Message}");
                return null;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var nested = FindVisualChild<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}

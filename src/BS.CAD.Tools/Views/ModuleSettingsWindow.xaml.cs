using System.Windows;
using BS.CAD.Tools.Models;
using BS.CAD.Tools.Services;

namespace BS.CAD.Tools.Views
{
    public partial class ModuleSettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        public ModuleSettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _settings = _settingsService.Load();
            LoadControlsFromSettings();
        }

        public bool SettingsSaved { get; private set; }

        private void LoadControlsFromSettings()
        {
            ChkLayerTools.IsChecked = IsEnabled(SettingsService.LayerTools);
            ChkImeTools.IsChecked = IsEnabled(SettingsService.ImeTools);
            ChkFontTools.IsChecked = IsEnabled(SettingsService.FontTools);
            ChkCleanupTools.IsChecked = IsEnabled(SettingsService.CleanupTools);
            ChkStandardTools.IsChecked = IsEnabled(SettingsService.StandardTools);
        }

        private bool IsEnabled(string key)
        {
            return _settings.EnabledModules.TryGetValue(key, out bool enabled) && enabled;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            _settings.EnabledModules[SettingsService.LayerTools] = ChkLayerTools.IsChecked == true;
            _settings.EnabledModules[SettingsService.ImeTools] = ChkImeTools.IsChecked == true;
            _settings.EnabledModules[SettingsService.FontTools] = ChkFontTools.IsChecked == true;
            _settings.EnabledModules[SettingsService.CleanupTools] = ChkCleanupTools.IsChecked == true;
            _settings.EnabledModules[SettingsService.StandardTools] = ChkStandardTools.IsChecked == true;

            if (!_settingsService.Save(_settings))
            {
                System.Windows.MessageBox.Show("设置保存失败，请检查 %APPDATA%\\BS-CAD-Tools 是否可写。", "CAD助手", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SettingsSaved = true;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

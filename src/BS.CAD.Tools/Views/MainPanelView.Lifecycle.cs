using System;
using System.Windows;

using BS.CAD.Tools.Utils;

namespace BS.CAD.Tools.Views
{
    public partial class MainPanelView
    {
        private void OnImeTimerTick(object? sender, EventArgs e)
        {
            UpdateCurrentIMEDisplay();
        }

        private void OnMainPanelUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer.Stop();
                _timer.Tick -= OnImeTimerTick;
                Unloaded -= OnMainPanelUnloaded;
                Logger.Info("MainPanelView unloaded; IME display timer stopped.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}

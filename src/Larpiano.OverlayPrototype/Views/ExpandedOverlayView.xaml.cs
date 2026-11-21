using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Larpiano.OverlayPrototype.ViewModels;

namespace Larpiano.OverlayPrototype.Views
{
    public partial class ExpandedOverlayView : UserControl
    {
        public ExpandedOverlayView()
        {
            InitializeComponent();
        }

        private OverlayViewModel? VM => DataContext as OverlayViewModel;

        private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Request non-activating drag on MainWindow
                var window = Window.GetWindow(this) as MainWindow;
                window?.StartNonActivatingDrag(e);
            }
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            VM?.TogglePlayPause();
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            VM?.Stop();
        }

        private void OnPreviousClick(object sender, RoutedEventArgs e)
        {
            VM?.PreviousTrack();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            VM?.NextTrack();
        }

        private void OnSpeedDropdownClick(object sender, RoutedEventArgs e)
        {
            VM?.ToggleSpeedDropdown();
        }

        private void OnSelectSpeed(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr && double.TryParse(tagStr, out double speed))
            {
                VM?.SetSpeed(speed);
            }
        }

        private void OnCompactClick(object sender, RoutedEventArgs e)
        {
            VM?.ToggleCompact();
        }

        private void OnPinClick(object sender, RoutedEventArgs e)
        {
            VM?.TogglePin();
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            VM?.ToggleSettingsPopover();
        }

        private void OnMappingClick(object sender, RoutedEventArgs e)
        {
            VM?.ToggleMappingPopover();
        }

        private void OnDiagnosticsClick(object sender, RoutedEventArgs e)
        {
            VM?.ToggleDiagnosticsPopover();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}

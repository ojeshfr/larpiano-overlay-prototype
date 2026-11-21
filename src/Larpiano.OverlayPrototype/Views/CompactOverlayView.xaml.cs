using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Larpiano.OverlayPrototype.ViewModels;

namespace Larpiano.OverlayPrototype.Views
{
    public partial class CompactOverlayView : UserControl
    {
        public CompactOverlayView()
        {
            InitializeComponent();
        }

        private OverlayViewModel? VM => DataContext as OverlayViewModel;

        private void OnBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
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

        private void OnSpeedClick(object sender, RoutedEventArgs e)
        {
            if (VM != null)
            {
                // Cycle speed: 0.75 -> 1.00 -> 1.25 -> 1.50 -> 0.75
                double nextSpeed = VM.PlaybackSpeed switch
                {
                    < 0.9 => 1.00,
                    < 1.1 => 1.25,
                    < 1.4 => 1.50,
                    _ => 0.75
                };
                VM.SetSpeed(nextSpeed);
            }
        }

        private void OnPinClick(object sender, RoutedEventArgs e)
        {
            VM?.TogglePin();
        }

        private void OnExpandClick(object sender, RoutedEventArgs e)
        {
            VM?.ToggleCompact();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}

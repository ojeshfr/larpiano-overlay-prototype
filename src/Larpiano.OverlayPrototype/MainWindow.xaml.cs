using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Larpiano.OverlayPrototype.ViewModels;

namespace Larpiano.OverlayPrototype
{
    public partial class MainWindow : Window
    {
        private readonly OverlayViewModel _viewModel;

        // Win32 Constants
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TOPMOST = 0x00000008;

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Dragging & Resizing State
        private bool _isDragging = false;
        private POINT _dragStartCursor;
        private Point _dragStartWindowPos;

        private bool _isResizing = false;
        private POINT _resizeStartCursor;
        private Size _resizeStartSize;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new OverlayViewModel();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayViewModel.IsCompact))
            {
                ApplyLayoutMode(_viewModel.IsCompact);
            }
            else if (e.PropertyName == nameof(OverlayViewModel.IsPinned))
            {
                UpdateTopmost(_viewModel.IsPinned);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Left = _viewModel.WindowLeft;
            Top = _viewModel.WindowTop;

            ApplyLayoutMode(_viewModel.IsCompact);
            UpdateTopmost(_viewModel.IsPinned);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            // Apply WS_EX_NOACTIVATE and WS_EX_TOOLWINDOW
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

            // Hook window procedure for WM_MOUSEACTIVATE
            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                // MA_NOACTIVATE: Do not activate window, but allow mouse event to be processed
                return new IntPtr(MA_NOACTIVATE);
            }
            return IntPtr.Zero;
        }

        private void ApplyLayoutMode(bool isCompact)
        {
            if (isCompact)
            {
                // Compact Bar dimensions
                MinHeight = 60;
                MaxHeight = 120;
                MinWidth = 600;
                MaxWidth = 1000;

                Width = 760;
                Height = 96; // includes shadow margin
                MainGlassCard.CornerRadius = new CornerRadius(14);
            }
            else
            {
                // Expanded dimensions
                MinHeight = 360;
                MaxHeight = 800;
                MinWidth = 640;
                MaxWidth = 1400;

                Width = _viewModel.ExpandedWidth;
                Height = _viewModel.ExpandedHeight;
                MainGlassCard.CornerRadius = new CornerRadius(18);
            }
        }

        private void UpdateTopmost(bool isPinned)
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetWindowPos(
                    helper.Handle,
                    isPinned ? HWND_TOPMOST : HWND_NOTOPMOST,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            Topmost = isPinned;
        }

        // --- Non-Activating Window Dragging ---

        public void StartNonActivatingDrag(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (GetCursorPos(out POINT pt))
                {
                    _dragStartCursor = pt;
                    _dragStartWindowPos = new Point(Left, Top);
                    _isDragging = true;
                    CaptureMouse();
                }
            }
        }

        // --- Non-Activating Window Resizing ---

        private void OnResizeGripMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_viewModel.IsCompact)
            {
                if (GetCursorPos(out POINT pt))
                {
                    _resizeStartCursor = pt;
                    _resizeStartSize = new Size(ActualWidth, ActualHeight);
                    _isResizing = true;
                    CaptureMouse();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (GetCursorPos(out POINT curPt))
                {
                    var source = PresentationSource.FromVisual(this);
                    double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                    double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                    double deltaX = (curPt.X - _dragStartCursor.X) / dpiX;
                    double deltaY = (curPt.Y - _dragStartCursor.Y) / dpiY;

                    Left = _dragStartWindowPos.X + deltaX;
                    Top = _dragStartWindowPos.Y + deltaY;

                    _viewModel.WindowLeft = Left;
                    _viewModel.WindowTop = Top;
                }
            }
            else if (_isResizing && e.LeftButton == MouseButtonState.Pressed && !_viewModel.IsCompact)
            {
                if (GetCursorPos(out POINT curPt))
                {
                    var source = PresentationSource.FromVisual(this);
                    double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                    double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                    double deltaW = (curPt.X - _resizeStartCursor.X) / dpiX;
                    double deltaH = (curPt.Y - _resizeStartCursor.Y) / dpiY;

                    double newW = Math.Clamp(_resizeStartSize.Width + deltaW, MinWidth, MaxWidth);
                    double newH = Math.Clamp(_resizeStartSize.Height + deltaH, MinHeight, MaxHeight);

                    Width = newW;
                    Height = newH;

                    _viewModel.ExpandedWidth = newW;
                    _viewModel.ExpandedHeight = newH;
                }
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                if (_isDragging || _isResizing)
                {
                    _isDragging = false;
                    _isResizing = false;
                    ReleaseMouseCapture();
                    _viewModel.SaveSettings();
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isDragging || _isResizing)
            {
                _isDragging = false;
                _isResizing = false;
                ReleaseMouseCapture();
                _viewModel.SaveSettings();
            }
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            _viewModel.WindowLeft = Left;
            _viewModel.WindowTop = Top;
            if (!_viewModel.IsCompact)
            {
                _viewModel.ExpandedWidth = Width;
                _viewModel.ExpandedHeight = Height;
            }
            _viewModel.SaveSettings();
        }
    }
}

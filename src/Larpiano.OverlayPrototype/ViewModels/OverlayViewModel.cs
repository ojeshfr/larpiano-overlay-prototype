using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace Larpiano.OverlayPrototype.ViewModels
{
    public enum PlaybackState
    {
        Ready,
        Playing,
        Paused,
        Stopped
    }

    public class OverlayViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _playbackTimer;
        private DateTime _lastTickTime;

        // Playback state
        private PlaybackState _playbackState = PlaybackState.Playing;
        private double _currentSeconds = 26.0;
        private double _totalSeconds = 122.0;
        private double _playbackSpeed = 1.0;
        private double _volume = 0.8;

        // Song data (Mock)
        private string _songTitle = "Cornfield Chase";
        private string _songSubtitle = "4 tracks • 1,127 notes • 2:02";
        private int _currentSongIndex = 0;

        // Window & Layout
        private bool _isCompact = false;
        private bool _isPinned = true;
        private double _windowOpacity = 0.94;
        private double _windowLeft = 200;
        private double _windowTop = 150;
        private double _expandedWidth = 920;
        private double _expandedHeight = 440;

        // Popovers
        private bool _isMappingPopoverOpen = false;
        private bool _isDiagnosticsPopoverOpen = false;
        private bool _isSettingsPopoverOpen = false;
        private bool _isSpeedDropdownOpen = false;

        // Non-activation diagnostics
        private string _lastControlClicked = "None";
        private int _nonActivatingClickCount = 0;

        public OverlayViewModel()
        {
            _playbackTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _playbackTimer.Tick += OnPlaybackTimerTick;
            _lastTickTime = DateTime.UtcNow;
            _playbackTimer.Start();

            LoadSettings();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // --- Playback Properties ---

        public PlaybackState PlaybackState
        {
            get => _playbackState;
            set
            {
                if (SetField(ref _playbackState, value))
                {
                    OnPropertyChanged(nameof(PlaybackStateText));
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        public string PlaybackStateText => _playbackState switch
        {
            PlaybackState.Playing => "Playing",
            PlaybackState.Paused => "Paused",
            PlaybackState.Stopped => "Stopped",
            PlaybackState.Ready => "Ready",
            _ => "Ready"
        };

        public bool IsPlaying => _playbackState == PlaybackState.Playing;

        public double CurrentSeconds
        {
            get => _currentSeconds;
            set
            {
                double clamped = Math.Clamp(value, 0, _totalSeconds);
                if (SetField(ref _currentSeconds, clamped))
                {
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(TimeString));
                }
            }
        }

        public double TotalSeconds
        {
            get => _totalSeconds;
            set
            {
                if (SetField(ref _totalSeconds, Math.Max(1, value)))
                {
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(TimeString));
                }
            }
        }

        public double Progress
        {
            get => _totalSeconds > 0 ? _currentSeconds / _totalSeconds : 0.0;
            set
            {
                double newSeconds = Math.Clamp(value, 0.0, 1.0) * _totalSeconds;
                CurrentSeconds = newSeconds;
            }
        }

        public string TimeString
        {
            get
            {
                TimeSpan current = TimeSpan.FromSeconds(_currentSeconds);
                TimeSpan total = TimeSpan.FromSeconds(_totalSeconds);
                return $"{current.Minutes}:{current.Seconds:D2} / {total.Minutes}:{total.Seconds:D2}";
            }
        }

        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                if (SetField(ref _playbackSpeed, value))
                {
                    OnPropertyChanged(nameof(SpeedText));
                }
            }
        }

        public string SpeedText => $"{_playbackSpeed:0.00}×";

        public double Volume
        {
            get => _volume;
            set
            {
                double clamped = Math.Clamp(value, 0.0, 1.0);
                if (SetField(ref _volume, clamped))
                {
                    OnPropertyChanged(nameof(VolumePercent));
                }
            }
        }

        public int VolumePercent => (int)Math.Round(_volume * 100);

        // --- Song & Target Info ---

        public string SongTitle
        {
            get => _songTitle;
            set => SetField(ref _songTitle, value);
        }

        public string SongSubtitle
        {
            get => _songSubtitle;
            set => SetField(ref _songSubtitle, value);
        }

        public string TargetWindowTitle => "Roblox: Demo Window (Mock)";
        public string TargetForegroundStatus => "• Demo foreground";
        public string MappingStatus => "Demo mapping • not connected";

        // --- Window Layout & State ---

        public bool IsCompact
        {
            get => _isCompact;
            set
            {
                if (SetField(ref _isCompact, value))
                {
                    OnPropertyChanged(nameof(WindowWidth));
                    OnPropertyChanged(nameof(WindowHeight));
                }
            }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set => SetField(ref _isPinned, value);
        }

        public double WindowOpacity
        {
            get => _windowOpacity;
            set => SetField(ref _windowOpacity, Math.Clamp(value, 0.70, 1.0));
        }

        public double WindowLeft
        {
            get => _windowLeft;
            set => SetField(ref _windowLeft, value);
        }

        public double WindowTop
        {
            get => _windowTop;
            set => SetField(ref _windowTop, value);
        }

        public double ExpandedWidth
        {
            get => _expandedWidth;
            set
            {
                if (SetField(ref _expandedWidth, Math.Clamp(value, 640, 1400)))
                {
                    if (!IsCompact) OnPropertyChanged(nameof(WindowWidth));
                }
            }
        }

        public double ExpandedHeight
        {
            get => _expandedHeight;
            set
            {
                if (SetField(ref _expandedHeight, Math.Clamp(value, 360, 800)))
                {
                    if (!IsCompact) OnPropertyChanged(nameof(WindowHeight));
                }
            }
        }

        public double WindowWidth
        {
            get => _isCompact ? 760 : _expandedWidth;
            set
            {
                if (!_isCompact)
                {
                    ExpandedWidth = value;
                }
            }
        }

        public double WindowHeight
        {
            get => _isCompact ? 96 : _expandedHeight;
            set
            {
                if (!_isCompact)
                {
                    ExpandedHeight = value;
                }
            }
        }

        // --- Popovers ---

        public bool IsMappingPopoverOpen
        {
            get => _isMappingPopoverOpen;
            set
            {
                if (SetField(ref _isMappingPopoverOpen, value) && value)
                {
                    IsDiagnosticsPopoverOpen = false;
                    IsSettingsPopoverOpen = false;
                }
            }
        }

        public bool IsDiagnosticsPopoverOpen
        {
            get => _isDiagnosticsPopoverOpen;
            set
            {
                if (SetField(ref _isDiagnosticsPopoverOpen, value) && value)
                {
                    IsMappingPopoverOpen = false;
                    IsSettingsPopoverOpen = false;
                }
            }
        }

        public bool IsSettingsPopoverOpen
        {
            get => _isSettingsPopoverOpen;
            set
            {
                if (SetField(ref _isSettingsPopoverOpen, value) && value)
                {
                    IsMappingPopoverOpen = false;
                    IsDiagnosticsPopoverOpen = false;
                }
            }
        }

        public bool IsSpeedDropdownOpen
        {
            get => _isSpeedDropdownOpen;
            set => SetField(ref _isSpeedDropdownOpen, value);
        }

        // --- Non-Activation Diagnostics Info ---

        public string LastControlClicked
        {
            get => _lastControlClicked;
            set => SetField(ref _lastControlClicked, value);
        }

        public int NonActivatingClickCount
        {
            get => _nonActivatingClickCount;
            set => SetField(ref _nonActivatingClickCount, value);
        }

        // --- Actions & Commands ---

        public void RecordClick(string controlName)
        {
            LastControlClicked = $"{controlName} ({DateTime.Now:HH:mm:ss})";
            NonActivatingClickCount++;
        }

        public void TogglePlayPause()
        {
            RecordClick("Play/Pause");
            if (PlaybackState == PlaybackState.Playing)
            {
                PlaybackState = PlaybackState.Paused;
            }
            else
            {
                if (CurrentSeconds >= TotalSeconds)
                {
                    CurrentSeconds = 0;
                }
                PlaybackState = PlaybackState.Playing;
            }
        }

        public void Stop()
        {
            RecordClick("Stop");
            PlaybackState = PlaybackState.Stopped;
            CurrentSeconds = 0;
        }

        public void PreviousTrack()
        {
            RecordClick("Previous");
            CurrentSeconds = 0;
        }

        public void NextTrack()
        {
            RecordClick("Next Track");
            _currentSongIndex = (_currentSongIndex + 1) % 2;
            if (_currentSongIndex == 0)
            {
                SongTitle = "Cornfield Chase";
                SongSubtitle = "4 tracks • 1,127 notes • 2:02";
                TotalSeconds = 122.0;
            }
            else
            {
                SongTitle = "Interstellar Main Theme";
                SongSubtitle = "6 tracks • 1,840 notes • 3:15";
                TotalSeconds = 195.0;
            }
            CurrentSeconds = 0;
            PlaybackState = PlaybackState.Playing;
        }

        public void SetSpeed(double speed)
        {
            RecordClick($"Speed {speed:0.00}x");
            PlaybackSpeed = speed;
            IsSpeedDropdownOpen = false;
        }

        public void ToggleSpeedDropdown()
        {
            RecordClick("Speed Dropdown");
            IsSpeedDropdownOpen = !IsSpeedDropdownOpen;
        }

        public void ToggleCompact()
        {
            RecordClick("Toggle Compact");
            IsCompact = !IsCompact;
            IsMappingPopoverOpen = false;
            IsDiagnosticsPopoverOpen = false;
            IsSettingsPopoverOpen = false;
            IsSpeedDropdownOpen = false;
            SaveSettings();
        }

        public void TogglePin()
        {
            RecordClick("Toggle Pin");
            IsPinned = !IsPinned;
            SaveSettings();
        }

        public void ToggleMappingPopover()
        {
            RecordClick("Toggle Mapping");
            IsMappingPopoverOpen = !IsMappingPopoverOpen;
        }

        public void ToggleDiagnosticsPopover()
        {
            RecordClick("Toggle Diagnostics");
            IsDiagnosticsPopoverOpen = !IsDiagnosticsPopoverOpen;
        }

        public void ToggleSettingsPopover()
        {
            RecordClick("Toggle Settings");
            IsSettingsPopoverOpen = !IsSettingsPopoverOpen;
        }

        private void OnPlaybackTimerTick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double elapsed = (now - _lastTickTime).TotalSeconds;
            _lastTickTime = now;

            if (_playbackState == PlaybackState.Playing)
            {
                double next = _currentSeconds + (elapsed * _playbackSpeed);
                if (next >= _totalSeconds)
                {
                    CurrentSeconds = _totalSeconds;
                    PlaybackState = PlaybackState.Stopped;
                }
                else
                {
                    CurrentSeconds = next;
                }
            }
        }

        // --- Settings Persistence ---

        private string GetSettingsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "LarpianoOverlayPrototype");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.json");
        }

        public void SaveSettings()
        {
            try
            {
                var settings = new OverlaySettingsDto
                {
                    Left = WindowLeft,
                    Top = WindowTop,
                    ExpandedWidth = _expandedWidth,
                    ExpandedHeight = _expandedHeight,
                    IsCompact = IsCompact,
                    IsPinned = IsPinned,
                    Opacity = WindowOpacity,
                    PlaybackSpeed = PlaybackSpeed,
                    Volume = Volume
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetSettingsFilePath(), json);
            }
            catch
            {
            }
        }

        public void LoadSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<OverlaySettingsDto>(json);
                    if (settings != null)
                    {
                        double width = settings.ExpandedWidth > 0 ? settings.ExpandedWidth : 920;
                        double height = settings.ExpandedHeight > 0 ? settings.ExpandedHeight : 440;
                        ValidateAndApplyBounds(settings.Left, settings.Top, width, height);

                        _isCompact = settings.IsCompact;
                        _isPinned = settings.IsPinned;
                        _windowOpacity = Math.Clamp(settings.Opacity > 0 ? settings.Opacity : 0.94, 0.70, 1.0);
                        if (settings.PlaybackSpeed > 0) _playbackSpeed = settings.PlaybackSpeed;
                        if (settings.Volume >= 0) _volume = settings.Volume;
                    }
                }
                else
                {
                    CenterOnPrimaryScreen();
                }
            }
            catch
            {
                CenterOnPrimaryScreen();
            }
        }

        private void CenterOnPrimaryScreen()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            _expandedWidth = 920;
            _expandedHeight = 440;
            _windowLeft = Math.Max(20, (screenW - _expandedWidth) / 2);
            _windowTop = 100;
        }

        private void ValidateAndApplyBounds(double left, double top, double width, double height)
        {
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualWidth = SystemParameters.VirtualScreenWidth;
            double virtualHeight = SystemParameters.VirtualScreenHeight;

            double w = Math.Clamp(width, 640, 1400);
            double h = Math.Clamp(height, 360, 800);

            bool isVisible = (left + w > virtualLeft + 50) &&
                             (left < virtualLeft + virtualWidth - 50) &&
                             (top + h > virtualTop + 50) &&
                             (top < virtualTop + virtualHeight - 50);

            if (isVisible)
            {
                _windowLeft = left;
                _windowTop = top;
                _expandedWidth = w;
                _expandedHeight = h;
            }
            else
            {
                CenterOnPrimaryScreen();
            }
        }
    }

    public class OverlaySettingsDto
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double ExpandedWidth { get; set; }
        public double ExpandedHeight { get; set; }
        public bool IsCompact { get; set; }
        public bool IsPinned { get; set; }
        public double Opacity { get; set; }
        public double PlaybackSpeed { get; set; }
        public double Volume { get; set; }
    }
}

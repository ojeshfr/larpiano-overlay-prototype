using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Larpiano.OverlayPrototype.Controls
{
    public partial class PianoRollPreview : UserControl
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register(
                nameof(Progress),
                typeof(double),
                typeof(PianoRollPreview),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnProgressChanged));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PianoRollPreview control)
            {
                control.UpdatePlayhead();
            }
        }

        private bool _isDragging = false;
        private readonly List<NoteData> _mockNotes = new();

        public PianoRollPreview()
        {
            InitializeComponent();
            GenerateMockNotes();
            Loaded += (s, e) =>
            {
                RenderNotes();
                UpdatePlayhead();
            };
            SizeChanged += (s, e) =>
            {
                RenderNotes();
                UpdatePlayhead();
            };
        }

        private void GenerateMockNotes()
        {
            _mockNotes.Clear();
            var rand = new Random(42); // Deterministic seed for consistent layout

            // C2 & C3: Bass pads and slow chord progressions
            for (double t = 0.02; t < 0.98; t += 0.07)
            {
                _mockNotes.Add(new NoteData(t, 0.05 + rand.NextDouble() * 0.04, 0.85, 0)); // C2
                _mockNotes.Add(new NoteData(t + 0.01, 0.04 + rand.NextDouble() * 0.03, 0.68, 1)); // C3
                if (rand.NextDouble() > 0.4)
                {
                    _mockNotes.Add(new NoteData(t + 0.03, 0.03, 0.73, 1));
                }
            }

            // C4 & C5: Main Arpeggiated melody (dense rhythmic pulses matching Cornfield Chase)
            for (double t = 0.03; t < 0.98; t += 0.012)
            {
                // Arpeggio motif
                double notePitch = 0.35 + (Math.Sin(t * 50) * 0.15); // C4 to C5 range
                double len = 0.007 + (rand.NextDouble() * 0.008);
                _mockNotes.Add(new NoteData(t, len, notePitch, 2));

                if (rand.NextDouble() > 0.5)
                {
                    _mockNotes.Add(new NoteData(t + 0.004, 0.006, notePitch - 0.08, 3));
                }
            }

            // C6: High twinkling accents
            for (double t = 0.05; t < 0.96; t += 0.025)
            {
                if (rand.NextDouble() > 0.3)
                {
                    double len = 0.005 + (rand.NextDouble() * 0.006);
                    _mockNotes.Add(new NoteData(t, len, 0.08 + (rand.NextDouble() * 0.12), 4));
                }
            }
        }

        private void RenderNotes()
        {
            double width = PianoRollContainer.ActualWidth;
            double height = PianoRollContainer.ActualHeight;

            if (width <= 0 || height <= 0) return;

            NotesCanvas.Children.Clear();

            // 1. Horizontal lane divider lines
            var dividerBrush = new SolidColorBrush(Color.FromArgb(50, 80, 60, 110));
            dividerBrush.Freeze();

            for (int i = 1; i <= 4; i++)
            {
                double y = (height / 5.0) * i;
                var line = new Line
                {
                    X1 = 0,
                    X2 = width,
                    Y1 = y,
                    Y2 = y,
                    Stroke = dividerBrush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };
                NotesCanvas.Children.Add(line);
            }

            // Brushes for note layers
            var bassBrush = new SolidColorBrush(Color.FromRgb(112, 69, 184)); // Deep purple
            var midBrush = new SolidColorBrush(Color.FromRgb(168, 121, 255)); // Primary lavender
            var highBrush = new SolidColorBrush(Color.FromRgb(199, 167, 255)); // Soft glow lavender
            var brightBrush = new SolidColorBrush(Color.FromRgb(230, 215, 255)); // Bright accent
            bassBrush.Freeze();
            midBrush.Freeze();
            highBrush.Freeze();
            brightBrush.Freeze();

            // 2. Render Note rectangles
            foreach (var note in _mockNotes)
            {
                double x = note.StartTime * width;
                double noteW = Math.Max(3, note.Duration * width);
                double y = note.PitchFraction * (height - 6);
                double noteH = (note.Layer == 0 || note.Layer == 1) ? 4.5 : 3.2;

                var brush = note.Layer switch
                {
                    0 => bassBrush,
                    1 => bassBrush,
                    2 => midBrush,
                    3 => highBrush,
                    _ => brightBrush
                };

                var rect = new Rectangle
                {
                    Width = noteW,
                    Height = noteH,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Fill = brush,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                NotesCanvas.Children.Add(rect);
            }
        }

        private void UpdatePlayhead()
        {
            double width = PianoRollContainer.ActualWidth;
            if (width <= 0) return;

            double x = Math.Clamp(Progress, 0.0, 1.0) * width;
            PlayheadLine.X1 = x;
            PlayheadLine.X2 = x;
            PlayheadLine.Y2 = PianoRollContainer.ActualHeight;
        }

        private void OnPianoRollMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                PianoRollContainer.CaptureMouse();
                SeekToMouse(e.GetPosition(PianoRollContainer));
            }
        }

        private void OnPianoRollMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                SeekToMouse(e.GetPosition(PianoRollContainer));
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                _isDragging = false;
                PianoRollContainer.ReleaseMouseCapture();
            }
        }

        private void SeekToMouse(Point pos)
        {
            double width = PianoRollContainer.ActualWidth;
            if (width > 0)
            {
                double newProgress = Math.Clamp(pos.X / width, 0.0, 1.0);
                Progress = newProgress;
            }
        }

        private class NoteData
        {
            public double StartTime { get; }
            public double Duration { get; }
            public double PitchFraction { get; }
            public int Layer { get; }

            public NoteData(double startTime, double duration, double pitchFraction, int layer)
            {
                StartTime = startTime;
                Duration = duration;
                PitchFraction = pitchFraction;
                Layer = layer;
            }
        }
    }
}

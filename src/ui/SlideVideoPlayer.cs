using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GenOffice.Core;

// Explicit aliases to completely prevent WinForms vs WPF name collisions
using WinForms = System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;
using WinFormsPanel = System.Windows.Forms.Panel;
using GdiBitmap = System.Drawing.Bitmap;
using GdiImage = System.Drawing.Image;
using GdiColor = System.Drawing.Color;
using GdiRectangle = System.Drawing.Rectangle;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;
using WpfThickness = System.Windows.Thickness;
using WpfCornerRadius = System.Windows.CornerRadius;
using WpfGridLength = System.Windows.GridLength;
using WpfGridUnitType = System.Windows.GridUnitType;
using WpfAlignment = System.Windows.VerticalAlignment;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfStretch = System.Windows.Media.Stretch;
using WpfPath = System.Windows.Shapes.Path;
using WpfGeometry = System.Windows.Media.Geometry;

namespace GenOffice.UI
{
    /// <summary>
    /// Helper to extract authentic high-resolution video thumbnail frames from video files
    /// </summary>
    public static class VideoThumbnailHelper
    {
        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static GdiBitmap GetVideoPosterFrame(string filePath, int width = 1280, int height = 720)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            // 1. Try Windows Shell Item Image Factory (fast, native thumbnail extraction)
            try
            {
                Guid guid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                IShellItemImageFactory factory;
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, guid, out factory);
                if (factory != null)
                {
                    IntPtr hBitmap;
                    int hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_THUMBNAILONLY, out hBitmap);
                    }
                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_RESIZETOFIT, out hBitmap);
                    }

                    if (hr == 0 && hBitmap != IntPtr.Zero)
                    {
                        GdiBitmap bmp = GdiImage.FromHbitmap(hBitmap);
                        DeleteObject(hBitmap);
                        return bmp;
                    }
                }
            }
            catch { }

            // 2. Fallback: Extract first frame via Windows Media Foundation MediaPlayer
            try
            {
                GdiBitmap frame = ExtractFrameViaMediaPlayer(filePath, width, height);
                if (frame != null) return frame;
            }
            catch { }

            return null;
        }

        private static GdiBitmap ExtractFrameViaMediaPlayer(string filePath, int width, int height)
        {
            try
            {
                MediaPlayer player = new MediaPlayer();
                player.Volume = 0;
                player.ScrubbingEnabled = true;
                player.Open(new Uri(filePath));
                player.Position = TimeSpan.FromMilliseconds(200);

                int w = width > 0 ? width : 640;
                int h = height > 0 ? height : 360;

                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    dc.DrawVideo(player, new Rect(0, 0, w, h));
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);

                using (MemoryStream ms = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return new GdiBitmap(ms);
                }
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Legacy ActiveX compatibility stub (preserved for API compatibility)
    /// </summary>
    public class WmpAxControl : WinForms.AxHost
    {
        public WmpAxControl() : base("6bf52a52-394a-11d3-b153-00c04f79faa6") { }
        public dynamic PlayerOcx { get { return null; } }
    }

    /// <summary>
    /// PowerPoint-Grade Native In-Slide Video Player View (WPF with Direct3D hardware-accelerated Media Foundation)
    /// </summary>
    public class WpfSlideVideoPlayerView : Grid
    {
        private MediaElement _media;
        private Border _barBorder;
        private Button _btnPlayPause;
        private Slider _slider;
        private TextBlock _txtTime;
        private Button _btnVolume;
        private Canvas _bookmarksCanvas;
        private DispatcherTimer _trackerTimer;
        private DispatcherTimer _autoHideTimer;
        private bool _isUserSeeking = false;
        private bool _isMuted = false;
        private double _previousVolume = 0.8;
        private ShapeNode _shape;
        private bool _isPresentation;
        private Action _onStateChanged;

        public MediaElement Media { get { return _media; } }
        public bool IsPlaying { get; private set; }

        public WpfSlideVideoPlayerView(ShapeNode shape, string videoPath, bool isPresentation, Action onStateChanged)
        {
            _shape = shape;
            _isPresentation = isPresentation;
            _onStateChanged = onStateChanged;

            Background = WpfBrushes.Black;
            ClipToBounds = true;

            // 1. Direct3D Media Foundation Video Surface
            _media = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Stretch = WpfStretch.Uniform,
                Source = new Uri(videoPath)
            };
            Children.Add(_media);

            // Click anywhere on video surface -> Toggle Play / Pause!
            _media.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    TogglePlay();
                    e.Handled = true;
                }
            };

            // 2. PowerPoint Floating Media Control Bar Overlay (Docked to bottom)
            BuildPlaybackBar();

            // 3. Event Handlers for MediaElement
            _media.MediaOpened += Media_MediaOpened;
            _media.MediaEnded += Media_MediaEnded;
            _media.MediaFailed += Media_MediaFailed;

            // 4. Position & Duration Tracker Timer (100ms)
            _trackerTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _trackerTimer.Tick += TrackerTimer_Tick;

            // 5. Auto-Hide Timer for Control Bar (2.5s)
            _autoHideTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(2500)
            };
            _autoHideTimer.Tick += (s, e) =>
            {
                _autoHideTimer.Stop();
                if (IsPlaying && _barBorder != null)
                {
                    _barBorder.Opacity = 0.0;
                }
            };

            // Mouse hover interactions for control bar visibility
            MouseMove += (s, e) =>
            {
                if (_barBorder != null)
                {
                    _barBorder.Opacity = 1.0;
                }
                _autoHideTimer.Stop();
                if (IsPlaying)
                {
                    _autoHideTimer.Start();
                }
            };

            MouseLeave += (s, e) =>
            {
                if (IsPlaying && _barBorder != null)
                {
                    _barBorder.Opacity = 0.0;
                }
            };
        }

        private void BuildPlaybackBar()
        {
            _barBorder = new Border
            {
                Height = 36,
                VerticalAlignment = WpfAlignment.Bottom,
                Margin = new WpfThickness(10, 0, 10, 10),
                CornerRadius = new WpfCornerRadius(6),
                Background = new WpfSolidColorBrush(WpfColor.FromArgb(225, 15, 23, 42)),
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromArgb(120, 71, 85, 105)),
                BorderThickness = new WpfThickness(1),
                Opacity = 1.0
            };

            Grid barGrid = new Grid();
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new WpfGridLength(40) });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new WpfGridLength(1, WpfGridUnitType.Star) });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = WpfGridLength.Auto });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new WpfGridLength(40) });

            // 1. Play/Pause Button
            _btnPlayPause = new Button
            {
                Background = WpfBrushes.Transparent,
                BorderThickness = new WpfThickness(0),
                Cursor = Cursors.Hand,
                Padding = new WpfThickness(0)
            };
            UpdatePlayButtonIcon(false);
            _btnPlayPause.Click += (s, e) => TogglePlay();
            Grid.SetColumn(_btnPlayPause, 0);
            barGrid.Children.Add(_btnPlayPause);

            // 2. Timeline Track with Bookmarks Canvas
            Grid trackGrid = new Grid
            {
                VerticalAlignment = WpfAlignment.Center,
                Margin = new WpfThickness(6, 0, 6, 0)
            };

            _slider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                VerticalAlignment = WpfAlignment.Center,
                Cursor = Cursors.Hand
            };
            _slider.PreviewMouseDown += (s, e) => _isUserSeeking = true;
            _slider.PreviewMouseUp += (s, e) =>
            {
                _isUserSeeking = false;
                Seek((float)_slider.Value);
            };
            _slider.ValueChanged += (s, e) =>
            {
                if (_isUserSeeking)
                {
                    Seek((float)e.NewValue);
                }
            };
            trackGrid.Children.Add(_slider);

            _bookmarksCanvas = new Canvas
            {
                IsHitTestVisible = false,
                VerticalAlignment = WpfAlignment.Center,
                Height = 16
            };
            trackGrid.Children.Add(_bookmarksCanvas);

            Grid.SetColumn(trackGrid, 1);
            barGrid.Children.Add(trackGrid);

            // 3. Time Display Text
            _txtTime = new TextBlock
            {
                Text = "00:00 / 00:00",
                Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(226, 232, 240)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 11,
                VerticalAlignment = WpfAlignment.Center,
                Margin = new WpfThickness(8, 0, 8, 0)
            };
            Grid.SetColumn(_txtTime, 2);
            barGrid.Children.Add(_txtTime);

            // 4. Volume Button
            _btnVolume = new Button
            {
                Background = WpfBrushes.Transparent,
                BorderThickness = new WpfThickness(0),
                Cursor = Cursors.Hand,
                Padding = new WpfThickness(0)
            };
            UpdateVolumeButtonIcon(false);
            _btnVolume.Click += (s, e) => ToggleVolume();
            Grid.SetColumn(_btnVolume, 3);
            barGrid.Children.Add(_btnVolume);

            _barBorder.Child = barGrid;
            Children.Add(_barBorder);
        }

        private void UpdatePlayButtonIcon(bool isPlaying)
        {
            WpfPath icon = new WpfPath
            {
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                VerticalAlignment = WpfAlignment.Center
            };

            if (isPlaying)
            {
                // Crisp Pause Icon (Two vertical bars)
                icon.Fill = new WpfSolidColorBrush(WpfColor.FromRgb(56, 189, 248));
                icon.Data = WpfGeometry.Parse("M 0 0 H 3.5 V 12 H 0 Z M 6.5 0 H 10 V 12 H 6.5 Z");
            }
            else
            {
                // Crisp Play Icon (Right triangle)
                icon.Fill = WpfBrushes.White;
                icon.Data = WpfGeometry.Parse("M 1 0 L 11 6 L 1 12 Z");
            }

            _btnPlayPause.Content = icon;
        }

        private void UpdateVolumeButtonIcon(bool isMuted)
        {
            WpfPath icon = new WpfPath
            {
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                VerticalAlignment = WpfAlignment.Center
            };

            if (isMuted)
            {
                icon.Fill = new WpfSolidColorBrush(WpfColor.FromRgb(239, 68, 68));
                icon.Data = WpfGeometry.Parse("M 1 4 H 3.5 L 7 1 V 13 L 3.5 10 H 1 Z M 9.5 4 L 13.5 8 M 13.5 4 L 9.5 8");
            }
            else
            {
                icon.Fill = new WpfSolidColorBrush(WpfColor.FromRgb(203, 213, 225));
                icon.Data = WpfGeometry.Parse("M 1 4 H 3.5 L 7 1 V 13 L 3.5 10 H 1 Z M 9 4.5 C 10.5 5.8 10.5 8.2 9 9.5 M 10.5 2.5 C 12.8 4.8 12.8 9.2 10.5 11.5");
            }

            _btnVolume.Content = icon;
        }

        private void RenderBookmarks()
        {
            if (_bookmarksCanvas == null || _slider == null || _slider.ActualWidth <= 0 || _shape == null || _shape.VideoBookmarks == null) return;
            _bookmarksCanvas.Children.Clear();

            double maxT = _slider.Maximum > 0 ? _slider.Maximum : 150.0;
            double trackW = _slider.ActualWidth;

            foreach (float bSec in _shape.VideoBookmarks)
            {
                double frac = Math.Max(0.0, Math.Min(1.0, bSec / maxT));
                double bx = frac * trackW;

                WpfPath mark = new WpfPath
                {
                    Fill = new WpfSolidColorBrush(WpfColor.FromRgb(250, 204, 21)),
                    Stroke = new WpfSolidColorBrush(WpfColor.FromRgb(161, 98, 7)),
                    StrokeThickness = 0.5,
                    Data = WpfGeometry.Parse("M 0 0 L 3 3 L 0 6 L -3 3 Z")
                };
                Canvas.SetLeft(mark, bx);
                Canvas.SetTop(mark, 5);
                _bookmarksCanvas.Children.Add(mark);
            }
        }

        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (_media.NaturalDuration.HasTimeSpan)
            {
                double totalSec = _media.NaturalDuration.TimeSpan.TotalSeconds;
                _slider.Maximum = totalSec;
                if (_shape != null)
                {
                    _shape.VideoTotalTime = (float)totalSec;
                }
            }

            // Apply Trim Start
            if (_shape != null && _shape.VideoTrimStart > 0)
            {
                _media.Position = TimeSpan.FromSeconds(_shape.VideoTrimStart);
            }

            // Apply Volume
            SetVolumeLevel(_shape != null ? _shape.VideoVolume : VideoVolumeLevel.High, _shape != null ? _shape.VideoVolumeLevel : "High");

            UpdateTimes();
            RenderBookmarks();

            // Auto-start playback
            Play();
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_shape != null && _shape.VideoLoopUntilStopped)
            {
                double startSec = (_shape.VideoTrimStart > 0) ? _shape.VideoTrimStart : 0.0;
                _media.Position = TimeSpan.FromSeconds(startSec);
                _media.Play();
            }
            else
            {
                Pause();
                if (_shape != null && _shape.VideoRewindAfterPlaying)
                {
                    double startSec = (_shape.VideoTrimStart > 0) ? _shape.VideoTrimStart : 0.0;
                    _media.Position = TimeSpan.FromSeconds(startSec);
                    UpdateTimes();
                }
            }
        }

        private void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Console.WriteLine("MediaElement error: " + (e.ErrorException != null ? e.ErrorException.Message : "Unknown error"));
        }

        private void TrackerTimer_Tick(object sender, EventArgs e)
        {
            if (!_isUserSeeking && _media.NaturalDuration.HasTimeSpan)
            {
                double pos = _media.Position.TotalSeconds;
                _slider.Value = pos;
                if (_shape != null)
                {
                    _shape.VideoCurrentTime = (float)pos;
                }

                // Check Trim End
                if (_shape != null && _shape.VideoTrimEnd > 0 && pos >= _shape.VideoTrimEnd)
                {
                    if (_shape.VideoLoopUntilStopped)
                    {
                        double startSec = (_shape.VideoTrimStart > 0) ? _shape.VideoTrimStart : 0.0;
                        _media.Position = TimeSpan.FromSeconds(startSec);
                    }
                    else
                    {
                        Pause();
                    }
                }

                UpdateTimes();
            }
        }

        private void UpdateTimes()
        {
            TimeSpan cur = _media.Position;
            TimeSpan tot = _media.NaturalDuration.HasTimeSpan ? _media.NaturalDuration.TimeSpan : TimeSpan.FromSeconds(_shape != null && _shape.VideoTotalTime > 0 ? _shape.VideoTotalTime : 0);

            string curStr = cur.TotalHours >= 1 ? string.Format("{0:D2}:{1:D2}:{2:D2}", (int)cur.TotalHours, cur.Minutes, cur.Seconds) : string.Format("{0:D2}:{1:D2}", cur.Minutes, cur.Seconds);
            string totStr = tot.TotalHours >= 1 ? string.Format("{0:D2}:{1:D2}:{2:D2}", (int)tot.TotalHours, tot.Minutes, tot.Seconds) : string.Format("{0:D2}:{1:D2}", tot.Minutes, tot.Seconds);

            _txtTime.Text = curStr + " / " + totStr;
        }

        public void Play()
        {
            _media.Play();
            IsPlaying = true;
            if (_shape != null) _shape.IsVideoPlaying = true;
            UpdatePlayButtonIcon(true);
            _trackerTimer.Start();
            _autoHideTimer.Start();
            if (_onStateChanged != null) _onStateChanged();
        }

        public void Pause()
        {
            _media.Pause();
            IsPlaying = false;
            if (_shape != null) _shape.IsVideoPlaying = false;
            UpdatePlayButtonIcon(false);
            _trackerTimer.Stop();
            _autoHideTimer.Stop();
            if (_barBorder != null) _barBorder.Opacity = 1.0;
            if (_onStateChanged != null) _onStateChanged();
        }

        public void TogglePlay()
        {
            if (IsPlaying) Pause();
            else Play();
        }

        public void Seek(float seconds)
        {
            _media.Position = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (_shape != null) _shape.VideoCurrentTime = seconds;
            UpdateTimes();
            if (_onStateChanged != null) _onStateChanged();
        }

        public void ToggleVolume()
        {
            if (_isMuted)
            {
                _isMuted = false;
                _media.Volume = _previousVolume > 0 ? _previousVolume : 0.8;
                UpdateVolumeButtonIcon(false);
                if (_shape != null) _shape.VideoVolume = VideoVolumeLevel.High;
            }
            else
            {
                _previousVolume = _media.Volume;
                _isMuted = true;
                _media.Volume = 0.0;
                UpdateVolumeButtonIcon(true);
                if (_shape != null) _shape.VideoVolume = VideoVolumeLevel.Mute;
            }
        }

        public void SetVolumeLevel(VideoVolumeLevel level, string levelStr)
        {
            double vol = 0.8;
            if (!string.IsNullOrEmpty(levelStr))
            {
                if (levelStr.Equals("Mute", StringComparison.OrdinalIgnoreCase)) vol = 0.0;
                else if (levelStr.Equals("Low", StringComparison.OrdinalIgnoreCase)) vol = 0.25;
                else if (levelStr.Equals("Medium", StringComparison.OrdinalIgnoreCase)) vol = 0.60;
                else if (levelStr.Equals("High", StringComparison.OrdinalIgnoreCase)) vol = 1.0;
            }
            else
            {
                switch (level)
                {
                    case VideoVolumeLevel.Mute: vol = 0.0; break;
                    case VideoVolumeLevel.Low: vol = 0.25; break;
                    case VideoVolumeLevel.Medium: vol = 0.60; break;
                    case VideoVolumeLevel.High: vol = 1.0; break;
                }
            }

            _isMuted = (vol <= 0.001);
            _media.Volume = vol;
            if (!_isMuted) _previousVolume = vol;
            UpdateVolumeButtonIcon(_isMuted);
        }

        public void UpdateShape(ShapeNode shape)
        {
            _shape = shape;
            RenderBookmarks();
        }

        public void DisposePlayer()
        {
            _trackerTimer.Stop();
            _autoHideTimer.Stop();
            try
            {
                _media.Stop();
                _media.Close();
            }
            catch { }
        }
    }

    /// <summary>
    /// PowerPoint-Grade Seamless Native In-Slide Video Playback Engine
    /// Hosts Direct3D hardware-accelerated Media Foundation player directly in slide bounds
    /// </summary>
    public class SlideVideoPlayerManager
    {
        private static SlideVideoPlayerManager _instance;
        public static SlideVideoPlayerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new SlideVideoPlayerManager();
                return _instance;
            }
        }

        private WinFormsPanel _playerContainer;
        private ElementHost _elementHost;
        private WpfSlideVideoPlayerView _wpfPlayer;
        private ShapeNode _activeShape;
        private WinFormsControl _parentControl;
        private bool _isPresentation;

        public bool IsPlaying
        {
            get
            {
                return _wpfPlayer != null && _wpfPlayer.IsPlaying;
            }
        }

        public ShapeNode ActiveShape
        {
            get { return _activeShape; }
        }

        public SlideVideoPlayerManager() { }

        public void Play(ShapeNode shape, WinFormsControl container, GdiRectangle bounds, bool isPresentation = false)
        {
            if (shape == null || container == null) return;

            string videoPath = ResolveVideoPath(shape.VideoPath, container);
            if (string.IsNullOrEmpty(videoPath) || (!videoPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !File.Exists(videoPath)))
            {
                using (WinForms.OpenFileDialog ofd = new WinForms.OpenFileDialog())
                {
                    ofd.Title = "Chọn tệp video để phát (" + (!string.IsNullOrEmpty(shape.VideoPath) ? Path.GetFileName(shape.VideoPath) : "Video") + ")...";
                    ofd.Filter = "Tệp Video (*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm)|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(container) == WinForms.DialogResult.OK)
                    {
                        shape.VideoPath = ofd.FileName;
                        videoPath = ofd.FileName;
                        if (shape.CachedImage == null)
                        {
                            shape.CachedImage = VideoThumbnailHelper.GetVideoPosterFrame(videoPath);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (shape.CachedImage == null && File.Exists(videoPath))
            {
                shape.CachedImage = VideoThumbnailHelper.GetVideoPosterFrame(videoPath);
            }

            // If already playing this shape, just update bounds and ensure playing
            if (_activeShape == shape && _playerContainer != null && _wpfPlayer != null)
            {
                _playerContainer.Bounds = bounds;
                _isPresentation = isPresentation;
                _wpfPlayer.Play();
                shape.IsVideoPlaying = true;
                _wpfPlayer.UpdateShape(shape);
                return;
            }

            Stop();

            _activeShape = shape;
            _parentControl = container;
            _isPresentation = isPresentation;

            // Pure Seamless WinForms Container & ElementHost
            _playerContainer = new WinFormsPanel
            {
                Bounds = bounds,
                BackColor = GdiColor.Black,
                Margin = WinForms.Padding.Empty,
                Padding = WinForms.Padding.Empty
            };

            _elementHost = new ElementHost
            {
                Dock = WinForms.DockStyle.Fill,
                BackColor = GdiColor.Black,
                Margin = WinForms.Padding.Empty,
                Padding = WinForms.Padding.Empty
            };

            // PowerPoint Native WPF Direct3D Media Foundation Player
            _wpfPlayer = new WpfSlideVideoPlayerView(shape, videoPath, isPresentation, () =>
            {
                if (_parentControl != null && !_parentControl.IsDisposed)
                {
                    _parentControl.Invalidate();
                }
            });

            _elementHost.Child = _wpfPlayer;
            _playerContainer.Controls.Add(_elementHost);

            container.Controls.Add(_playerContainer);
            _playerContainer.BringToFront();

            _wpfPlayer.Play();
            shape.IsVideoPlaying = true;
            container.Invalidate();
        }

        public void TogglePlay(ShapeNode shape = null, WinFormsControl container = null, GdiRectangle bounds = default(GdiRectangle), bool isPresentation = false)
        {
            if (shape == null) shape = _activeShape;
            if (_activeShape == shape && _wpfPlayer != null)
            {
                _wpfPlayer.TogglePlay();
                if (_parentControl != null && !_parentControl.IsDisposed) _parentControl.Invalidate();
                return;
            }

            if (shape != null && container != null)
            {
                Play(shape, container, bounds, isPresentation);
            }
        }

        public void ToggleVolume()
        {
            if (_wpfPlayer != null)
            {
                _wpfPlayer.ToggleVolume();
            }
        }

        public void Pause()
        {
            if (_wpfPlayer != null)
            {
                _wpfPlayer.Pause();
            }
            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = false;
            }
            if (_parentControl != null && !_parentControl.IsDisposed) _parentControl.Invalidate();
        }

        public void Resume()
        {
            if (_wpfPlayer != null)
            {
                _wpfPlayer.Play();
            }
            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = true;
            }
            if (_parentControl != null && !_parentControl.IsDisposed) _parentControl.Invalidate();
        }

        public void Seek(float seconds)
        {
            if (_wpfPlayer != null)
            {
                _wpfPlayer.Seek(seconds);
            }
            if (_activeShape != null)
            {
                _activeShape.VideoCurrentTime = seconds;
            }
            if (_parentControl != null && !_parentControl.IsDisposed) _parentControl.Invalidate();
        }

        public void Stop()
        {
            if (_wpfPlayer != null)
            {
                _wpfPlayer.DisposePlayer();
                _wpfPlayer = null;
            }

            if (_elementHost != null)
            {
                try { _elementHost.Dispose(); } catch { }
                _elementHost = null;
            }

            if (_playerContainer != null && _parentControl != null)
            {
                try
                {
                    _parentControl.Controls.Remove(_playerContainer);
                    _playerContainer.Dispose();
                }
                catch { }
                _playerContainer = null;
            }

            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = false;
                _activeShape = null;
            }

            if (_parentControl != null)
            {
                if (!_parentControl.IsDisposed) _parentControl.Invalidate();
                _parentControl = null;
            }
        }

        public void UpdateBounds(GdiRectangle newBounds)
        {
            if (_playerContainer != null && (_playerContainer.Bounds != newBounds))
            {
                _playerContainer.Bounds = newBounds;
            }
        }

        private string ResolveVideoPath(string rawPath, WinFormsControl parent)
        {
            if (string.IsNullOrEmpty(rawPath)) return null;

            if (rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return rawPath;
            }

            if (File.Exists(rawPath)) return rawPath;

            try
            {
                string full = Path.GetFullPath(rawPath);
                if (File.Exists(full)) return full;
            }
            catch { }

            try
            {
                string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileName(rawPath));
                if (File.Exists(desktopPath)) return desktopPath;
            }
            catch { }

            try
            {
                string myVideos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), Path.GetFileName(rawPath));
                if (File.Exists(myVideos)) return myVideos;
            }
            catch { }

            return null;
        }
    }
}

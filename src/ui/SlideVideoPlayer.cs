using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GenOffice.Core;

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

        public static Bitmap GetVideoPosterFrame(string filePath, int width = 960, int height = 540)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
            try
            {
                Guid guid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                IShellItemImageFactory factory;
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, guid, out factory);
                if (factory != null)
                {
                    IntPtr hBitmap;
                    int hr = factory.GetImage(new SIZE(width, height), SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                    if (hr == 0 && hBitmap != IntPtr.Zero)
                    {
                        Bitmap bmp = Image.FromHbitmap(hBitmap);
                        DeleteObject(hBitmap);
                        return bmp;
                    }
                }
            }
            catch { }
            return null;
        }
    }

    /// <summary>
    /// ActiveX Control wrapper for Windows Media Player with zero chrome (Pure Video Surface)
    /// </summary>
    public class WmpAxControl : AxHost
    {
        public WmpAxControl() : base("6bf52a52-394a-11d3-b153-00c04f79faa6") { }

        public dynamic PlayerOcx
        {
            get
            {
                try
                {
                    return GetOcx();
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    /// <summary>
    /// Sleek PowerPoint Native Video Playback Bar Control
    /// </summary>
    public class VideoPlaybackBarControl : Control
    {
        private ShapeNode _shape;
        private Action _onTogglePlay;
        private Action<float> _onSeek;
        private Action _onToggleVolume;
        private bool _isDraggingScrubber = false;

        public VideoPlaybackBarControl(ShapeNode shape, Action onTogglePlay, Action<float> onSeek, Action onToggleVolume)
        {
            _shape = shape;
            _onTogglePlay = onTogglePlay;
            _onSeek = onSeek;
            _onToggleVolume = onToggleVolume;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 32;
            Dock = DockStyle.Bottom;
            Cursor = Cursors.Default;
        }

        public void UpdateShape(ShapeNode shape)
        {
            _shape = shape;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int w = Width;
            int h = Height;

            // Sleek Dark Background
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(240, 15, 23, 42)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            using (Pen topPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
            {
                g.DrawLine(topPen, 0, 0, w, 0);
            }

            // 1. Play / Pause Icon Button
            float btnSz = h * 0.55f;
            float btnX = 10;
            float btnY = (h - btnSz) / 2f;
            bool isPlaying = (_shape != null && _shape.IsVideoPlaying);

            using (SolidBrush pb = new SolidBrush(Color.FromArgb(56, 189, 248)))
            {
                if (isPlaying)
                {
                    float barW = btnSz * 0.3f;
                    g.FillRectangle(pb, btnX + btnSz * 0.1f, btnY, barW, btnSz);
                    g.FillRectangle(pb, btnX + btnSz * 0.6f, btnY, barW, btnSz);
                }
                else
                {
                    PointF[] tri = new PointF[] {
                        new PointF(btnX + 2, btnY),
                        new PointF(btnX + btnSz, btnY + btnSz / 2f),
                        new PointF(btnX + 2, btnY + btnSz)
                    };
                    g.FillPolygon(pb, tri);
                }
            }

            // 2. Timeline Track
            float timeX = btnX + btnSz + 12;
            float timeW = Math.Max(40, w - timeX - (w > 240 ? 115 : 45));
            float timeY = h / 2f - 2.5f;
            float trackH = 5f;

            using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.FillRectangle(trackBg, timeX, timeY, timeW, trackH);
            }

            float curT = _shape != null ? _shape.VideoCurrentTime : 0f;
            float totT = _shape != null && _shape.VideoTotalTime > 0 ? _shape.VideoTotalTime : 150f;
            float playedFrac = Math.Max(0f, Math.Min(1f, curT / totT));

            // Played red progress
            using (SolidBrush progB = new SolidBrush(Color.FromArgb(239, 68, 68)))
            {
                g.FillRectangle(progB, timeX, timeY, timeW * playedFrac, trackH);
            }

            // Scrubber knob
            float scrubX = timeX + timeW * playedFrac;
            float scrubR = 6f;
            using (SolidBrush scrubB = new SolidBrush(Color.White))
            using (Pen scrubPen = new Pen(Color.FromArgb(239, 68, 68), 1.5f))
            {
                g.FillEllipse(scrubB, scrubX - scrubR, timeY + trackH / 2f - scrubR, scrubR * 2, scrubR * 2);
                g.DrawEllipse(scrubPen, scrubX - scrubR, timeY + trackH / 2f - scrubR, scrubR * 2, scrubR * 2);
            }

            // Yellow Bookmarks
            if (_shape != null && _shape.VideoBookmarks != null && _shape.VideoBookmarks.Count > 0)
            {
                using (SolidBrush bmB = new SolidBrush(Color.FromArgb(250, 204, 21)))
                using (Pen bmP = new Pen(Color.FromArgb(161, 98, 7), 1f))
                {
                    foreach (float bSec in _shape.VideoBookmarks)
                    {
                        float bFrac = Math.Max(0.05f, Math.Min(0.95f, bSec / totT));
                        float bx = timeX + timeW * bFrac;
                        g.FillRectangle(bmB, bx - 2f, timeY - 3f, 4f, trackH + 6f);
                        g.DrawRectangle(bmP, bx - 2f, timeY - 3f, 4f, trackH + 6f);
                    }
                }
            }

            // 3. Time Text & Speaker on Right
            if (w > 240)
            {
                float rightX = timeX + timeW + 8;
                using (Font timeFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    int curM = (int)(curT / 60);
                    int curS = (int)(curT % 60);
                    int totM = (int)(totT / 60);
                    int totS = (int)(totT % 60);
                    string timeStr = string.Format("{0:D2}:{1:D2} / {2:D2}:{3:D2}", curM, curS, totM, totS);
                    g.DrawString(timeStr, timeFont, tb, rightX, (h - 14) / 2f);
                }

                // Volume icon
                float spkX = w - 24;
                float spkY = h / 2f;
                using (Pen spkPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
                {
                    g.DrawLine(spkPen, spkX, spkY - 4, spkX + 4, spkY - 4);
                    g.DrawLine(spkPen, spkX + 4, spkY - 4, spkX + 8, spkY - 8);
                    g.DrawLine(spkPen, spkX + 8, spkY - 8, spkX + 8, spkY + 8);
                    g.DrawLine(spkPen, spkX + 8, spkY + 8, spkX + 4, spkY + 4);
                    g.DrawLine(spkPen, spkX + 4, spkY + 4, spkX, spkY + 4);
                    g.DrawLine(spkPen, spkX, spkY + 4, spkX, spkY - 4);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int w = Width;
            int h = Height;
            float btnSz = h * 0.55f;
            float btnX = 10;
            float timeX = btnX + btnSz + 12;
            float timeW = Math.Max(40, w - timeX - (w > 240 ? 115 : 45));

            if (e.X < timeX)
            {
                if (_onTogglePlay != null) _onTogglePlay();
            }
            else if (e.X >= timeX && e.X <= timeX + timeW)
            {
                _isDraggingScrubber = true;
                SeekToMouseX(e.X, timeX, timeW);
            }
            else if (e.X > timeX + timeW)
            {
                if (e.X >= w - 30)
                {
                    if (_onToggleVolume != null) _onToggleVolume();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDraggingScrubber && (e.Button & MouseButtons.Left) != 0)
            {
                int w = Width;
                int h = Height;
                float btnSz = h * 0.55f;
                float btnX = 10;
                float timeX = btnX + btnSz + 12;
                float timeW = Math.Max(40, w - timeX - (w > 240 ? 115 : 45));
                SeekToMouseX(e.X, timeX, timeW);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingScrubber = false;
        }

        private void SeekToMouseX(int mouseX, float timeX, float timeW)
        {
            float frac = Math.Max(0f, Math.Min(1f, (mouseX - timeX) / timeW));
            float totT = _shape != null && _shape.VideoTotalTime > 0 ? _shape.VideoTotalTime : 150f;
            if (_onSeek != null) _onSeek(frac * totT);
            Invalidate();
        }
    }

    /// <summary>
    /// PowerPoint-grade Seamless Video Playback Engine (100% borderless, real-time interactive click-to-pause/play)
    /// </summary>
    public class SlideVideoPlayerManager : IMessageFilter
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

        private Panel _playerContainer;
        private WmpAxControl _wmpControl;
        private VideoPlaybackBarControl _playbackBar;
        private ShapeNode _activeShape;
        private Control _parentControl;
        private bool _isPresentation;
        private Timer _playbackTracker;

        public bool IsPlaying
        {
            get
            {
                if (_wmpControl == null) return false;
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null && ocx.playState == 3) // 3 = Playing
                    {
                        return true;
                    }
                }
                catch { }
                return false;
            }
        }

        public ShapeNode ActiveShape
        {
            get { return _activeShape; }
        }

        public SlideVideoPlayerManager()
        {
            _playbackTracker = new Timer();
            _playbackTracker.Interval = 100;
            _playbackTracker.Tick += PlaybackTracker_Tick;
            Application.AddMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            // Intercept mouse clicks on video: 0x0201 = WM_LBUTTONDOWN, 0x0203 = WM_LBUTTONDBLCLK
            if (m.Msg == 0x0201 || m.Msg == 0x0203)
            {
                if (_playerContainer != null && _playerContainer.Visible && !_playerContainer.IsDisposed && _activeShape != null)
                {
                    Point screenPt = Cursor.Position;
                    Rectangle panelScreen = _playerContainer.RectangleToScreen(_playerContainer.ClientRectangle);
                    if (panelScreen.Contains(screenPt))
                    {
                        Point localPt = _playerContainer.PointToClient(screenPt);
                        float barH = (_playbackBar != null && _playbackBar.Visible) ? _playbackBar.Height : 0;

                        // If clicked on playback bar, let the playback bar handle its mouse event directly
                        if (localPt.Y >= _playerContainer.Height - barH)
                        {
                            return false;
                        }

                        // If clicked anywhere on the main video screen -> IMMEDIATELY TOGGLE PLAY/PAUSE!
                        TogglePlay();
                        return true; // Eat message so WMP does not capture
                    }
                }
            }
            return false;
        }

        private void PlaybackTracker_Tick(object sender, EventArgs e)
        {
            if (_wmpControl == null || _activeShape == null) return;
            try
            {
                dynamic ocx = _wmpControl.PlayerOcx;
                if (ocx != null)
                {
                    double pos = (double)ocx.controls.currentPosition;
                    _activeShape.VideoCurrentTime = (float)pos;

                    if (ocx.currentMedia != null)
                    {
                        double dur = (double)ocx.currentMedia.duration;
                        if (dur > 0) _activeShape.VideoTotalTime = (float)dur;
                    }

                    // Check Trim End
                    if (_activeShape.VideoTrimEnd > 0 && pos >= _activeShape.VideoTrimEnd)
                    {
                        if (_activeShape.VideoLoopUntilStopped)
                        {
                            ocx.controls.currentPosition = (double)_activeShape.VideoTrimStart;
                        }
                        else
                        {
                            ocx.controls.pause();
                            _activeShape.IsVideoPlaying = false;
                        }
                    }

                    if (_playbackBar != null && !_playbackBar.IsDisposed)
                    {
                        _playbackBar.Invalidate();
                    }

                    if (_parentControl != null)
                    {
                        _parentControl.Invalidate();
                    }
                }
            }
            catch { }
        }

        public void Play(ShapeNode shape, Control container, Rectangle bounds, bool isPresentation = false)
        {
            if (shape == null || container == null) return;

            string videoPath = ResolveVideoPath(shape.VideoPath, container);
            if (string.IsNullOrEmpty(videoPath) || (!videoPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !File.Exists(videoPath)))
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn tệp video để phát (" + (!string.IsNullOrEmpty(shape.VideoPath) ? Path.GetFileName(shape.VideoPath) : "Video") + ")...";
                    ofd.Filter = "Tệp Video (*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm)|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(container) == DialogResult.OK)
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

            Rectangle adjustedBounds = isPresentation ? bounds : new Rectangle(bounds.X + 4, bounds.Y + 4, Math.Max(40, bounds.Width - 8), Math.Max(40, bounds.Height - 8));

            // If already playing this shape, just update bounds and ensure playing
            if (_activeShape == shape && _playerContainer != null && _wmpControl != null)
            {
                _playerContainer.Bounds = adjustedBounds;
                _isPresentation = isPresentation;
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null)
                    {
                        ocx.controls.play();
                    }
                }
                catch { }
                shape.IsVideoPlaying = true;
                if (_playbackBar != null) _playbackBar.UpdateShape(shape);
                _playbackTracker.Start();
                return;
            }

            Stop();

            _activeShape = shape;
            _parentControl = container;
            _isPresentation = isPresentation;

            // Pure Seamless Container
            _playerContainer = new Panel();
            _playerContainer.Bounds = adjustedBounds;
            _playerContainer.BackColor = Color.Black;

            // 1. Playback Bar Control at the bottom
            _playbackBar = new VideoPlaybackBarControl(shape, () => TogglePlay(), s => Seek(s), () => ToggleVolume());
            _playerContainer.Controls.Add(_playbackBar);

            // 2. Video Player on top
            _wmpControl = new WmpAxControl();
            _wmpControl.Dock = DockStyle.Fill;
            _playerContainer.Controls.Add(_wmpControl);

            container.Controls.Add(_playerContainer);
            _playerContainer.BringToFront();

            // Set uiMode = "none" (PURE VIDEO FRAMES, 0% WINDOWS/WMP CHROME)
            try
            {
                dynamic ocx = _wmpControl.PlayerOcx;
                if (ocx != null)
                {
                    ocx.uiMode = "none";
                    ocx.settings.autoStart = true;
                    ocx.settings.volume = GetVolumeValue(shape.VideoVolume, shape.VideoVolumeLevel);
                    ocx.settings.setMode("loop", shape.VideoLoopUntilStopped);
                    ocx.URL = videoPath;

                    if (shape.VideoTrimStart > 0)
                    {
                        ocx.controls.currentPosition = (double)shape.VideoTrimStart;
                    }
                    ocx.controls.play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WMP play error: " + ex.Message);
            }

            shape.IsVideoPlaying = true;
            _playbackTracker.Start();
            container.Invalidate();
        }

        public void TogglePlay(ShapeNode shape = null, Control container = null, Rectangle bounds = default(Rectangle), bool isPresentation = false)
        {
            if (shape == null) shape = _activeShape;
            if (_activeShape == shape && _playerContainer != null && _wmpControl != null)
            {
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null)
                    {
                        if (shape.IsVideoPlaying || (ocx.playState == 3)) // If currently playing -> PAUSE!
                        {
                            ocx.controls.pause();
                            shape.IsVideoPlaying = false;
                        }
                        else // If paused -> PLAY!
                        {
                            ocx.controls.play();
                            shape.IsVideoPlaying = true;
                        }
                        if (_playbackBar != null) _playbackBar.UpdateShape(shape);
                        if (_parentControl != null) _parentControl.Invalidate();
                        return;
                    }
                }
                catch { }
            }

            if (shape != null && container != null)
            {
                Play(shape, container, bounds, isPresentation);
            }
        }

        public void ToggleVolume()
        {
            if (_activeShape != null && _wmpControl != null)
            {
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null)
                    {
                        _activeShape.VideoVolume = (_activeShape.VideoVolume == VideoVolumeLevel.Mute) ? VideoVolumeLevel.High : VideoVolumeLevel.Mute;
                        ocx.settings.volume = GetVolumeValue(_activeShape.VideoVolume, _activeShape.VideoVolumeLevel);
                    }
                }
                catch { }
                if (_playbackBar != null) _playbackBar.Invalidate();
            }
        }

        public void Pause()
        {
            if (_wmpControl != null)
            {
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null) ocx.controls.pause();
                }
                catch { }
            }
            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = false;
                if (_playbackBar != null) _playbackBar.UpdateShape(_activeShape);
            }
            if (_parentControl != null) _parentControl.Invalidate();
        }

        public void Resume()
        {
            if (_wmpControl != null)
            {
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null) ocx.controls.play();
                }
                catch { }
            }
            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = true;
                if (_playbackBar != null) _playbackBar.UpdateShape(_activeShape);
            }
            if (_parentControl != null) _parentControl.Invalidate();
        }

        public void Seek(float seconds)
        {
            if (_wmpControl != null)
            {
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null)
                    {
                        ocx.controls.currentPosition = (double)seconds;
                    }
                }
                catch { }
            }
            if (_activeShape != null)
            {
                _activeShape.VideoCurrentTime = seconds;
                if (_playbackBar != null) _playbackBar.Invalidate();
            }
            if (_parentControl != null) _parentControl.Invalidate();
        }

        public void Stop()
        {
            _playbackTracker.Stop();
            if (_wmpControl != null)
            {
                try
                {
                    dynamic ocx = _wmpControl.PlayerOcx;
                    if (ocx != null) ocx.controls.stop();
                }
                catch { }
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
                _wmpControl = null;
                _playbackBar = null;
            }

            if (_activeShape != null)
            {
                _activeShape.IsVideoPlaying = false;
                _activeShape = null;
            }

            if (_parentControl != null)
            {
                _parentControl.Invalidate();
                _parentControl = null;
            }
        }

        public void UpdateBounds(Rectangle newBounds)
        {
            Rectangle adjusted = _isPresentation ? newBounds : new Rectangle(newBounds.X + 4, newBounds.Y + 4, Math.Max(40, newBounds.Width - 8), Math.Max(40, newBounds.Height - 8));
            if (_playerContainer != null && (_playerContainer.Bounds != adjusted))
            {
                _playerContainer.Bounds = adjusted;
            }
        }

        private int GetVolumeValue(VideoVolumeLevel level, string levelStr)
        {
            if (!string.IsNullOrEmpty(levelStr))
            {
                if (levelStr.Equals("Mute", StringComparison.OrdinalIgnoreCase)) return 0;
                if (levelStr.Equals("Low", StringComparison.OrdinalIgnoreCase)) return 25;
                if (levelStr.Equals("Medium", StringComparison.OrdinalIgnoreCase)) return 60;
                if (levelStr.Equals("High", StringComparison.OrdinalIgnoreCase)) return 100;
            }
            switch (level)
            {
                case VideoVolumeLevel.Mute: return 0;
                case VideoVolumeLevel.Low: return 25;
                case VideoVolumeLevel.Medium: return 60;
                case VideoVolumeLevel.High: return 100;
                default: return 80;
            }
        }

        private string ResolveVideoPath(string rawPath, Control parent)
        {
            if (string.IsNullOrEmpty(rawPath)) return null;

            if (rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return rawPath;
            }

            if (File.Exists(rawPath)) return rawPath;

            string full = Path.GetFullPath(rawPath);
            if (File.Exists(full)) return full;

            string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileName(rawPath));
            if (File.Exists(desktopPath)) return desktopPath;

            string myVideos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), Path.GetFileName(rawPath));
            if (File.Exists(myVideos)) return myVideos;

            return null;
        }
    }
}

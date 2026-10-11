using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bộ máy điều khiển và phát âm thanh trên Slide chuẩn PowerPoint (Slide Audio Engine)
    /// Hỗ trợ phát nhạc nền, phát đa định dạng (MP3, WAV, M4A, WMA, AAC),
    /// thanh điều khiển trực tiếp trên canvas (Seekbar, Play/Pause, Thời lượng, Âm lượng),
    /// hiệu ứng Fade In/Out, Trim Audio và phát xuyên slide.
    /// </summary>
    public class SlideAudioPlayerManager
    {
        private static SlideAudioPlayerManager _instance;
        public static SlideAudioPlayerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new SlideAudioPlayerManager();
                return _instance;
            }
        }

        private MediaPlayer _player;
        private Timer _trackerTimer;
        private ShapeNode _activeShape;
        private Control _parentControl;
        private bool _isMuted = false;
        private double _previousVolume = 1.0;
        private float _targetVolume = 1.0f;

        public bool IsPlaying
        {
            get { return _activeShape != null && _activeShape.IsAudioPlaying; }
        }

        public ShapeNode ActiveShape
        {
            get { return _activeShape; }
        }

        public Action OnAudioStateChanged { get; set; }

        public SlideAudioPlayerManager()
        {
            _player = new MediaPlayer();
            _player.MediaOpened += Player_MediaOpened;
            _player.MediaEnded += Player_MediaEnded;
            _player.MediaFailed += Player_MediaFailed;

            _trackerTimer = new Timer();
            _trackerTimer.Interval = 100;
            _trackerTimer.Tick += TrackerTimer_Tick;
        }

        private void Player_MediaOpened(object sender, EventArgs e)
        {
            if (_player.NaturalDuration.HasTimeSpan && _activeShape != null)
            {
                float totalSec = (float)_player.NaturalDuration.TimeSpan.TotalSeconds;
                _activeShape.AudioTotalTime = totalSec;
                if (_activeShape.AudioTrimEnd <= 0 || _activeShape.AudioTrimEnd > totalSec)
                {
                    _activeShape.AudioTrimEnd = totalSec;
                }
                InvalidateUI();
            }
        }

        private void Player_MediaEnded(object sender, EventArgs e)
        {
            if (_activeShape == null) return;

            float startSec = (_activeShape.AudioTrimStart > 0) ? _activeShape.AudioTrimStart : 0f;
            if (_activeShape.AudioLoopUntilStopped)
            {
                Seek(_activeShape, startSec);
                _player.Play();
            }
            else
            {
                Seek(_activeShape, startSec);
                Pause(_activeShape);
            }
        }

        private void Player_MediaFailed(object sender, ExceptionEventArgs e)
        {
            if (_activeShape != null) _activeShape.IsAudioPlaying = false;
            _trackerTimer.Stop();
            InvalidateUI();
        }

        private void TrackerTimer_Tick(object sender, EventArgs e)
        {
            if (_activeShape == null || !_activeShape.IsAudioPlaying)
            {
                _trackerTimer.Stop();
                return;
            }

            try
            {
                float curSec = (float)_player.Position.TotalSeconds;
                _activeShape.AudioCurrentTime = curSec;

                // 1. Kiểm tra điểm kết thúc cắt đoạn (Trim End)
                if (_activeShape.AudioTrimEnd > 0 && curSec >= _activeShape.AudioTrimEnd)
                {
                    float startSec = (_activeShape.AudioTrimStart > 0) ? _activeShape.AudioTrimStart : 0f;
                    if (_activeShape.AudioLoopUntilStopped)
                    {
                        Seek(_activeShape, startSec);
                        _player.Play();
                    }
                    else
                    {
                        Seek(_activeShape, startSec);
                        Pause(_activeShape);
                        return;
                    }
                }

                // 2. Xử lý Fade In
                if (_activeShape.AudioFadeInDuration > 0.05f)
                {
                    float elapsed = curSec - _activeShape.AudioTrimStart;
                    if (elapsed < _activeShape.AudioFadeInDuration && elapsed >= 0)
                    {
                        float frac = elapsed / _activeShape.AudioFadeInDuration;
                        _player.Volume = _targetVolume * Math.Max(0.01f, Math.Min(1.0f, frac));
                    }
                    else
                    {
                        _player.Volume = _targetVolume;
                    }
                }

                // 3. Xử lý Fade Out
                if (_activeShape.AudioFadeOutDuration > 0.05f)
                {
                    float maxTime = (_activeShape.AudioTrimEnd > 0) ? _activeShape.AudioTrimEnd : _activeShape.AudioTotalTime;
                    if (maxTime > 0)
                    {
                        float remaining = maxTime - curSec;
                        if (remaining <= _activeShape.AudioFadeOutDuration && remaining >= 0)
                        {
                            float frac = remaining / _activeShape.AudioFadeOutDuration;
                            _player.Volume = _targetVolume * Math.Max(0.0f, Math.Min(1.0f, frac));
                        }
                    }
                }

                InvalidateUI();
            }
            catch { }
        }

        public void Play(ShapeNode shape, Control container = null)
        {
            if (shape == null) return;
            if (container != null) _parentControl = container;

            string path = shape.AudioPath;
            if (!string.IsNullOrEmpty(path) && !File.Exists(path))
            {
                string resolved = SlideVideoPlayerManager.Instance.ResolveVideoPath(path);
                if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                {
                    path = resolved;
                    shape.AudioPath = resolved;
                }
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn tệp âm thanh (" + (shape.Name ?? "Audio") + ")...";
                    ofd.Filter = "Tệp Âm thanh (*.mp3;*.wav;*.wma;*.m4a;*.aac;*.flac)|*.mp3;*.wav;*.wma;*.m4a;*.aac;*.flac|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(container) == DialogResult.OK)
                    {
                        shape.AudioPath = ofd.FileName;
                        path = ofd.FileName;
                        shape.Name = "🔊 " + Path.GetFileName(ofd.FileName);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            try
            {
                float startSec = (shape.AudioTrimStart > 0) ? shape.AudioTrimStart : 0f;
                float maxTime = (shape.AudioTrimEnd > 0) ? shape.AudioTrimEnd : shape.AudioTotalTime;

                if (_activeShape != shape)
                {
                    if (_activeShape != null) _activeShape.IsAudioPlaying = false;
                    _activeShape = shape;

                    _player.Open(new Uri(Path.GetFullPath(path)));
                    ApplyVolumeForShape(shape);

                    if (shape.AudioTrimStart > 0 && shape.AudioCurrentTime < shape.AudioTrimStart)
                    {
                        shape.AudioCurrentTime = shape.AudioTrimStart;
                        _player.Position = TimeSpan.FromSeconds(shape.AudioTrimStart);
                    }
                    else if (maxTime > 0 && shape.AudioCurrentTime >= maxTime - 0.15f)
                    {
                        shape.AudioCurrentTime = startSec;
                        _player.Position = TimeSpan.FromSeconds(startSec);
                    }
                    else if (shape.AudioCurrentTime > 0)
                    {
                        _player.Position = TimeSpan.FromSeconds(shape.AudioCurrentTime);
                    }
                }
                else
                {
                    ApplyVolumeForShape(shape);
                    double curPosSec = _player.Position.TotalSeconds;
                    double natDurSec = _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan.TotalSeconds : maxTime;
                    if ((maxTime > 0 && shape.AudioCurrentTime >= maxTime - 0.15f) ||
                        (natDurSec > 0 && curPosSec >= natDurSec - 0.15))
                    {
                        shape.AudioCurrentTime = startSec;
                        _player.Position = TimeSpan.FromSeconds(startSec);
                    }
                    else if (shape.AudioTrimStart > 0 && shape.AudioCurrentTime < shape.AudioTrimStart)
                    {
                        shape.AudioCurrentTime = shape.AudioTrimStart;
                        _player.Position = TimeSpan.FromSeconds(shape.AudioTrimStart);
                    }
                }

                _activeShape.IsAudioPlaying = true;
                _player.Play();
                _trackerTimer.Start();
                InvalidateUI();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi phát âm thanh: " + ex.Message);
            }
        }

        public void Pause(ShapeNode shape = null)
        {
            try
            {
                _player.Pause();
            }
            catch { }

            if (shape != null) shape.IsAudioPlaying = false;
            if (_activeShape != null) _activeShape.IsAudioPlaying = false;

            _trackerTimer.Stop();
            InvalidateUI();
        }

        public void TogglePlay(ShapeNode shape, Control container = null)
        {
            if (shape == null) return;
            if (shape.IsAudioPlaying)
            {
                Pause(shape);
            }
            else
            {
                Play(shape, container);
            }
        }

        public void Stop()
        {
            try
            {
                _player.Stop();
            }
            catch { }

            if (_activeShape != null)
            {
                _activeShape.IsAudioPlaying = false;
                _activeShape.AudioCurrentTime = (_activeShape.AudioTrimStart > 0) ? _activeShape.AudioTrimStart : 0f;
            }
            _trackerTimer.Stop();
            InvalidateUI();
        }

        public void Seek(ShapeNode shape, float seconds)
        {
            if (shape == null) return;
            float min = Math.Max(0f, shape.AudioTrimStart);
            float max = (shape.AudioTrimEnd > 0) ? shape.AudioTrimEnd : (shape.AudioTotalTime > 0 ? shape.AudioTotalTime : 300f);
            float target = Math.Max(min, Math.Min(max, seconds));

            shape.AudioCurrentTime = target;
            if (_activeShape == shape)
            {
                try
                {
                    _player.Position = TimeSpan.FromSeconds(target);
                }
                catch { }
            }
            InvalidateUI();
        }

        public void StepRelative(ShapeNode shape, float deltaSeconds, Control container = null)
        {
            if (shape == null) return;
            float newSec = shape.AudioCurrentTime + deltaSeconds;
            Seek(shape, newSec);
        }

        public void ApplyVolumeForShape(ShapeNode shape)
        {
            if (shape == null) return;
            float vol = 1.0f;
            switch (shape.AudioVolume)
            {
                case VideoVolumeLevel.Mute: vol = 0.0f; break;
                case VideoVolumeLevel.Low: vol = 0.25f; break;
                case VideoVolumeLevel.Medium: vol = 0.60f; break;
                case VideoVolumeLevel.High: vol = 1.0f; break;
            }
            _targetVolume = vol;
            if (!_isMuted)
            {
                _player.Volume = vol;
            }
        }

        public void SetVolume(ShapeNode shape, VideoVolumeLevel level)
        {
            if (shape != null) shape.AudioVolume = level;
            ApplyVolumeForShape(shape);
            InvalidateUI();
        }

        public void ToggleMute(ShapeNode shape = null)
        {
            if (_isMuted)
            {
                _isMuted = false;
                _player.Volume = _targetVolume > 0 ? _targetVolume : 1.0;
                if (shape != null) shape.AudioVolume = VideoVolumeLevel.High;
            }
            else
            {
                _isMuted = true;
                _previousVolume = _player.Volume;
                _player.Volume = 0.0;
                if (shape != null) shape.AudioVolume = VideoVolumeLevel.Mute;
            }
            InvalidateUI();
        }

        public void AddBookmark(ShapeNode shape, Control container = null)
        {
            if (shape == null) return;
            if (shape.AudioBookmarks == null) shape.AudioBookmarks = new List<float>();

            float cur = shape.AudioCurrentTime;
            if (!shape.AudioBookmarks.Contains(cur))
            {
                shape.AudioBookmarks.Add(cur);
                shape.AudioBookmarks.Sort();
            }

            int m = (int)(cur / 60);
            int s = (int)(cur % 60);
            MessageBox.Show(container, string.Format("Đã thêm Dấu trang tại mốc thời gian {0:D2}:{1:D2} ({2:0.0}s).", m, s, cur), "Thêm Dấu trang Âm thanh", MessageBoxButtons.OK, MessageBoxIcon.Information);
            InvalidateUI();
        }

        public void RemoveBookmark(ShapeNode shape, Control container = null)
        {
            if (shape == null) return;
            if (shape.AudioBookmarks == null || shape.AudioBookmarks.Count == 0)
            {
                MessageBox.Show(container, "Âm thanh này hiện chưa có dấu trang nào để xóa.", "Xóa Dấu trang Âm thanh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            float last = shape.AudioBookmarks[shape.AudioBookmarks.Count - 1];
            shape.AudioBookmarks.RemoveAt(shape.AudioBookmarks.Count - 1);
            int m = (int)(last / 60);
            int s = (int)(last % 60);
            MessageBox.Show(container, string.Format("Đã xóa Dấu trang tại mốc {0:D2}:{1:D2}.", m, s), "Xóa Dấu trang Âm thanh", MessageBoxButtons.OK, MessageBoxIcon.Information);
            InvalidateUI();
        }

        public void SetPlayInBackground(ShapeNode shape, bool enable, Control container = null)
        {
            if (shape == null) return;
            if (enable)
            {
                shape.AudioPlayAcrossSlides = true;
                shape.AudioLoopUntilStopped = true;
                shape.AudioHideDuringShow = true;
                shape.AudioStartMode = VideoStartMode.Automatically;
                shape.AudioRewindAfterPlaying = true;
                MessageBox.Show(container, "Đã thiết lập âm thanh làm NHẠC NỀN (Play in Background):\n- Phát tự động khi vào trang\n- Tiếp tục phát xuyên suốt các trang chiếu\n- Tự động lặp lại liên tục\n- Ẩn biểu tượng khi trình chiếu.", "Phát trong Nền (Play in Background)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                shape.AudioPlayAcrossSlides = false;
                shape.AudioLoopUntilStopped = false;
                shape.AudioHideDuringShow = false;
                shape.AudioStartMode = VideoStartMode.InClickSequence;
                MessageBox.Show(container, "Đã khôi phục cài đặt phát tiêu chuẩn cho âm thanh.", "Tùy chọn Âm thanh", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            InvalidateUI();
        }

        public void ShowTrimAudioDialog(ShapeNode shape, Control container = null)
        {
            if (shape == null) return;
            using (TrimAudioForm trimForm = new TrimAudioForm(shape))
            {
                if (trimForm.ShowDialog(container) == DialogResult.OK)
                {
                    shape.AudioTrimStart = trimForm.TrimStart;
                    shape.AudioTrimEnd = trimForm.TrimEnd;
                    if (shape.AudioCurrentTime < shape.AudioTrimStart)
                    {
                        Seek(shape, shape.AudioTrimStart);
                    }
                    InvalidateUI();
                }
            }
        }

        public void InvalidateUI()
        {
            if (_parentControl != null && !_parentControl.IsDisposed)
            {
                try { _parentControl.Invalidate(); } catch { }
            }
            if (OnAudioStateChanged != null)
            {
                try { OnAudioStateChanged(); } catch { }
            }
        }

        public void InspectAudioDuration(ShapeNode shape)
        {
            if (shape == null || string.IsNullOrEmpty(shape.AudioPath) || !File.Exists(shape.AudioPath)) return;
            try
            {
                MediaPlayer probe = new MediaPlayer();
                probe.MediaOpened += (s, e) =>
                {
                    if (probe.NaturalDuration.HasTimeSpan)
                    {
                        shape.AudioTotalTime = (float)probe.NaturalDuration.TimeSpan.TotalSeconds;
                        if (shape.AudioTrimEnd <= 0) shape.AudioTrimEnd = shape.AudioTotalTime;
                        probe.Close();
                        InvalidateUI();
                    }
                };
                probe.Open(new Uri(shape.AudioPath));
            }
            catch { }
        }
    }
}

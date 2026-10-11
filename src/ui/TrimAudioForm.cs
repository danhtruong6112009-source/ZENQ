using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Cắt Âm thanh (Trim Audio Dialog) chuẩn PowerPoint
    /// Cho phép thiết lập mốc thời gian bắt đầu và kết thúc với độ chính xác cao,
    /// kèm nghe trước trực tiếp và trực quan hóa waveform/timeline.
    /// </summary>
    public class TrimAudioForm : Form
    {
        private ShapeNode _shape;
        private MediaPlayer _previewPlayer;
        private Timer _previewTimer;
        private bool _isPlaying = false;

        private NumericUpDown _numStart;
        private NumericUpDown _numEnd;
        private Label _lblCurrentTime;
        private Label _lblTrimmedDuration;
        private Button _btnPlayPause;
        private Panel _timelinePanel;

        private float _totalDuration = 60f;
        private float _currentPlayTime = 0f;

        public float TrimStart { get { return (float)_numStart.Value; } }
        public float TrimEnd { get { return (float)_numEnd.Value; } }

        public TrimAudioForm(ShapeNode shape)
        {
            _shape = shape;
            InitializeComponent();
            LoadAudio();
        }

        private void InitializeComponent()
        {
            this.Text = "Cắt Âm thanh (Trim Audio)";
            this.Size = new Size(580, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9f);

            // 1. Header Banner
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = System.Drawing.Color.FromArgb(241, 245, 249)
            };
            Label title = new Label
            {
                Text = "✂️ Cắt bỏ phần đầu và phần cuối của tệp âm thanh",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(16, 8)
            };
            string fileName = (_shape != null && !string.IsNullOrEmpty(_shape.AudioPath)) ? Path.GetFileName(_shape.AudioPath) : "Tệp âm thanh";
            Label sub = new Label
            {
                Text = "Tệp đang chọn: " + fileName,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = System.Drawing.Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(18, 29)
            };
            header.Controls.Add(title);
            header.Controls.Add(sub);
            this.Controls.Add(header);

            // 2. Main Content Card
            Panel card = new Panel
            {
                Location = new Point(16, 64),
                Size = new Size(532, 280),
                BackColor = System.Drawing.Color.White
            };
            card.Paint += (s, e) =>
            {
                using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            // Audio Waveform / Timeline Graphic Panel
            _timelinePanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(492, 70),
                BackColor = System.Drawing.Color.FromArgb(15, 23, 42)
            };
            _timelinePanel.Paint += TimelinePanel_Paint;
            _timelinePanel.MouseDown += TimelinePanel_MouseDown;
            card.Controls.Add(_timelinePanel);

            // Play / Pause Button
            _btnPlayPause = new Button
            {
                Text = "▶ Phát",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(20, 105),
                Size = new Size(88, 36),
                BackColor = System.Drawing.Color.FromArgb(37, 99, 235),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPlayPause.FlatAppearance.BorderSize = 0;
            _btnPlayPause.Click += (s, e) => TogglePlay();
            card.Controls.Add(_btnPlayPause);

            _lblCurrentTime = new Label
            {
                Text = "Vị trí: 00:00.00",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(30, 41, 59),
                Location = new Point(120, 114),
                AutoSize = true
            };
            card.Controls.Add(_lblCurrentTime);

            _lblTrimmedDuration = new Label
            {
                Text = "Thời lượng sau cắt: 00:00.00",
                Font = new Font("Segoe UI", 9f),
                ForeColor = System.Drawing.Color.FromArgb(71, 85, 105),
                Location = new Point(320, 114),
                AutoSize = true
            };
            card.Controls.Add(_lblTrimmedDuration);

            // Time inputs group
            GroupBox grpTimes = new GroupBox
            {
                Text = "Điểm cắt thời gian (Giây)",
                Location = new Point(20, 155),
                Size = new Size(492, 105),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = System.Drawing.Color.FromArgb(30, 41, 59)
            };

            Label lblStart = new Label
            {
                Text = "🟢 Bắt đầu (Start):",
                Location = new Point(20, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(22, 163, 74)
            };
            _numStart = new NumericUpDown
            {
                Location = new Point(20, 56),
                Size = new Size(160, 26),
                DecimalPlaces = 2,
                Increment = 0.5m,
                Maximum = 7200m,
                Minimum = 0m,
                Font = new Font("Segoe UI", 9.5f)
            };
            _numStart.ValueChanged += (s, e) =>
            {
                if (_numStart.Value >= _numEnd.Value && _numEnd.Value > 0)
                {
                    _numEnd.Value = Math.Min(_numEnd.Maximum, _numStart.Value + 1m);
                }
                UpdateDurationReadout();
                _timelinePanel.Invalidate();
            };

            Label lblEnd = new Label
            {
                Text = "🔴 Kết thúc (End):",
                Location = new Point(290, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(220, 38, 38)
            };
            _numEnd = new NumericUpDown
            {
                Location = new Point(290, 56),
                Size = new Size(160, 26),
                DecimalPlaces = 2,
                Increment = 0.5m,
                Maximum = 7200m,
                Minimum = 0m,
                Font = new Font("Segoe UI", 9.5f)
            };
            _numEnd.ValueChanged += (s, e) =>
            {
                if (_numEnd.Value <= _numStart.Value && _numEnd.Value > 0)
                {
                    _numStart.Value = Math.Max(0m, _numEnd.Value - 1m);
                }
                UpdateDurationReadout();
                _timelinePanel.Invalidate();
            };

            grpTimes.Controls.Add(lblStart);
            grpTimes.Controls.Add(_numStart);
            grpTimes.Controls.Add(lblEnd);
            grpTimes.Controls.Add(_numEnd);
            card.Controls.Add(grpTimes);

            this.Controls.Add(card);

            // 3. Bottom Action Buttons
            Button btnOk = new Button
            {
                Text = "Đồng ý (OK)",
                DialogResult = DialogResult.OK,
                Location = new Point(340, 355),
                Size = new Size(100, 32),
                BackColor = System.Drawing.Color.FromArgb(37, 99, 235),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(448, 355),
                Size = new Size(100, 32),
                BackColor = System.Drawing.Color.FromArgb(241, 245, 249),
                ForeColor = System.Drawing.Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);

            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            // Timer for playback tracking
            _previewTimer = new Timer();
            _previewTimer.Interval = 50;
            _previewTimer.Tick += PreviewTimer_Tick;

            this.FormClosing += (s, e) =>
            {
                StopPlayback();
                if (_previewPlayer != null)
                {
                    try { _previewPlayer.Close(); } catch { }
                }
            };
        }

        private void LoadAudio()
        {
            if (_shape == null) return;
            _totalDuration = (_shape.AudioTotalTime > 0) ? _shape.AudioTotalTime : 60f;

            float initStart = Math.Max(0f, _shape.AudioTrimStart);
            float initEnd = (_shape.AudioTrimEnd > 0) ? _shape.AudioTrimEnd : _totalDuration;

            _numStart.Value = (decimal)initStart;
            _numEnd.Value = (decimal)initEnd;

            if (!string.IsNullOrEmpty(_shape.AudioPath) && File.Exists(_shape.AudioPath))
            {
                try
                {
                    _previewPlayer = new MediaPlayer();
                    _previewPlayer.MediaOpened += (s, e) =>
                    {
                        if (_previewPlayer.NaturalDuration.HasTimeSpan)
                        {
                            _totalDuration = (float)_previewPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                            _shape.AudioTotalTime = _totalDuration;
                            _numEnd.Maximum = (decimal)_totalDuration;
                            _numStart.Maximum = (decimal)_totalDuration;
                            if (_numEnd.Value > (decimal)_totalDuration || _numEnd.Value <= 0)
                            {
                                _numEnd.Value = (decimal)_totalDuration;
                            }
                            UpdateDurationReadout();
                            _timelinePanel.Invalidate();
                        }
                    };
                    _previewPlayer.Open(new Uri(_shape.AudioPath));
                }
                catch { }
            }

            UpdateDurationReadout();
        }

        private void UpdateDurationReadout()
        {
            float trimmedSec = Math.Max(0f, (float)(_numEnd.Value - _numStart.Value));
            int m = (int)(trimmedSec / 60);
            float s = trimmedSec % 60;
            _lblTrimmedDuration.Text = string.Format("Thời lượng sau cắt: {0:D2}:{1:00.00}", m, s);
        }

        private void TogglePlay()
        {
            if (_isPlaying)
            {
                StopPlayback();
            }
            else
            {
                StartPlayback();
            }
        }

        private void StartPlayback()
        {
            if (_previewPlayer == null) return;
            try
            {
                float startSec = (float)_numStart.Value;
                _previewPlayer.Position = TimeSpan.FromSeconds(startSec);
                _previewPlayer.Play();
                _isPlaying = true;
                _btnPlayPause.Text = "❚❚ Tạm dừng";
                _btnPlayPause.BackColor = System.Drawing.Color.FromArgb(234, 88, 12);
                _previewTimer.Start();
            }
            catch { }
        }

        private void StopPlayback()
        {
            if (_previewPlayer != null)
            {
                try { _previewPlayer.Pause(); } catch { }
            }
            _isPlaying = false;
            _btnPlayPause.Text = "▶ Phát";
            _btnPlayPause.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            _previewTimer.Stop();
            _timelinePanel.Invalidate();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            if (_previewPlayer == null || !_isPlaying) return;
            try
            {
                float pos = (float)_previewPlayer.Position.TotalSeconds;
                _currentPlayTime = pos;

                int m = (int)(pos / 60);
                float s = pos % 60;
                _lblCurrentTime.Text = string.Format("Vị trí: {0:D2}:{1:00.00}", m, s);

                // Stop if exceeded TrimEnd
                if (pos >= (float)_numEnd.Value)
                {
                    StopPlayback();
                    _previewPlayer.Position = TimeSpan.FromSeconds((float)_numStart.Value);
                    _currentPlayTime = (float)_numStart.Value;
                }

                _timelinePanel.Invalidate();
            }
            catch { }
        }

        private void TimelinePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _timelinePanel.Width;
            int h = _timelinePanel.Height;

            // Pseudo-waveform visualization bars
            float maxTime = Math.Max(1f, _totalDuration);
            int barCount = w / 4;
            float step = (float)w / barCount;

            using (System.Drawing.SolidBrush dimWave = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(51, 65, 85)))
            using (System.Drawing.SolidBrush activeWave = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(56, 189, 248)))
            {
                float startFrac = (float)_numStart.Value / maxTime;
                float endFrac = (float)_numEnd.Value / maxTime;

                for (int i = 0; i < barCount; i++)
                {
                    float frac = (float)i / barCount;
                    // Synthetic pleasing waveform pattern
                    float waveH = (float)(Math.Sin(i * 0.28) * Math.Cos(i * 0.12) * 0.5 + 0.5) * (h - 16) + 4;
                    float x = i * step;
                    float y = (h - waveH) / 2f;

                    bool isInTrim = (frac >= startFrac && frac <= endFrac);
                    g.FillRectangle(isInTrim ? activeWave : dimWave, x, y, 2.5f, waveH);
                }
            }

            // Draw Dim zones outside trim
            float startX = ((float)_numStart.Value / maxTime) * w;
            float endX = ((float)_numEnd.Value / maxTime) * w;

            using (System.Drawing.SolidBrush dimBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                if (startX > 0) g.FillRectangle(dimBrush, 0, 0, startX, h);
                if (endX < w) g.FillRectangle(dimBrush, endX, 0, w - endX, h);
            }

            // Start Marker (Green line & handle)
            using (System.Drawing.Pen pStart = new System.Drawing.Pen(System.Drawing.Color.FromArgb(34, 197, 94), 3f))
            using (System.Drawing.SolidBrush bStart = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(34, 197, 94)))
            {
                g.DrawLine(pStart, startX, 0, startX, h);
                PointF[] markerStart = new PointF[] {
                    new PointF(startX - 6, 0),
                    new PointF(startX + 6, 0),
                    new PointF(startX, 10)
                };
                g.FillPolygon(bStart, markerStart);
            }

            // End Marker (Red line & handle)
            using (System.Drawing.Pen pEnd = new System.Drawing.Pen(System.Drawing.Color.FromArgb(239, 68, 68), 3f))
            using (System.Drawing.SolidBrush bEnd = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(239, 68, 68)))
            {
                g.DrawLine(pEnd, endX, 0, endX, h);
                PointF[] markerEnd = new PointF[] {
                    new PointF(endX - 6, h),
                    new PointF(endX + 6, h),
                    new PointF(endX, h - 10)
                };
                g.FillPolygon(bEnd, markerEnd);
            }

            // Playback Head (White line)
            if (_isPlaying || _currentPlayTime > 0)
            {
                float playX = (_currentPlayTime / maxTime) * w;
                using (System.Drawing.Pen pPlay = new System.Drawing.Pen(System.Drawing.Color.White, 2f))
                {
                    g.DrawLine(pPlay, playX, 0, playX, h);
                }
            }
        }

        private void TimelinePanel_MouseDown(object sender, MouseEventArgs e)
        {
            float maxTime = Math.Max(1f, _totalDuration);
            float clickedSec = ((float)e.X / _timelinePanel.Width) * maxTime;

            if (e.Button == MouseButtons.Left)
            {
                // Drag start or end closer to click
                float distStart = Math.Abs(clickedSec - (float)_numStart.Value);
                float distEnd = Math.Abs(clickedSec - (float)_numEnd.Value);

                if (distStart < distEnd)
                {
                    _numStart.Value = (decimal)Math.Max(0f, Math.Min((float)_numEnd.Value - 0.5f, clickedSec));
                }
                else
                {
                    _numEnd.Value = (decimal)Math.Max((float)_numStart.Value + 0.5f, Math.Min(_totalDuration, clickedSec));
                }
            }
        }
    }
}

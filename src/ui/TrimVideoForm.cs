using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class TrimVideoForm : Form
    {
        private ShapeNode _shape;
        private NumericUpDown _numStart;
        private NumericUpDown _numEnd;
        private Label _lblDuration;
        private Panel _timelinePanel;

        public float TrimStart { get { return (float)_numStart.Value; } }
        public float TrimEnd { get { return (float)_numEnd.Value; } }

        private PictureBox _videoPreviewBox;

        public TrimVideoForm(ShapeNode shape)
        {
            _shape = shape;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Cắt Video (Trim Video)";
            this.Size = new Size(600, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9f);

            // Header Banner
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            Label title = new Label
            {
                Text = "✂️ Cắt bỏ phần đầu và phần cuối của đoạn video",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(16, 12)
            };
            header.Controls.Add(title);
            this.Controls.Add(header);

            // Embedded Video Player Panel (Native WMF Canvas Preview)
            Panel videoContainer = new Panel
            {
                Location = new Point(20, 54),
                Size = new Size(544, 210),
                BackColor = Color.Black
            };
            _videoPreviewBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            videoContainer.Controls.Add(_videoPreviewBox);
            this.Controls.Add(videoContainer);

            // Timeline Track Visualizer
            _timelinePanel = new Panel
            {
                Location = new Point(20, 272),
                Size = new Size(544, 50),
                BackColor = Color.FromArgb(15, 23, 42)
            };
            _timelinePanel.Paint += TimelinePanel_Paint;
            this.Controls.Add(_timelinePanel);

            // Controls Group
            Label lblStart = new Label { Text = "Thời gian Bắt đầu:", Location = new Point(20, 330), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _numStart = new NumericUpDown
            {
                Location = new Point(20, 354),
                Width = 140,
                DecimalPlaces = 2,
                Increment = 0.5M,
                Minimum = 0M,
                Maximum = 3600M,
                Value = (decimal)(_shape != null ? _shape.VideoTrimStart : 0f)
            };
            _numStart.ValueChanged += (s, e) => {
                if (_numStart.Value >= _numEnd.Value) _numEnd.Value = _numStart.Value + 1M;
                UpdateDuration();
                SeekPlayer((double)_numStart.Value);
                _timelinePanel.Invalidate();
            };

            Label lblEnd = new Label { Text = "Thời gian Kết thúc:", Location = new Point(424, 330), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _numEnd = new NumericUpDown
            {
                Location = new Point(424, 354),
                Width = 140,
                DecimalPlaces = 2,
                Increment = 0.5M,
                Minimum = 1M,
                Maximum = 3600M,
                Value = (decimal)(_shape != null && _shape.VideoTrimEnd > 0f ? _shape.VideoTrimEnd : 30f)
            };
            _numEnd.ValueChanged += (s, e) => {
                if (_numEnd.Value <= _numStart.Value) _numStart.Value = Math.Max(0M, _numEnd.Value - 1M);
                UpdateDuration();
                SeekPlayer((double)_numEnd.Value);
                _timelinePanel.Invalidate();
            };

            _lblDuration = new Label
            {
                Text = "Thời lượng sau cắt: 00:30.00",
                Location = new Point(180, 358),
                Width = 224,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            this.Controls.AddRange(new Control[] { lblStart, _numStart, lblEnd, _numEnd, _lblDuration });

            // Bottom Buttons
            Button btnOk = new Button
            {
                Text = "Cắt (OK)",
                DialogResult = DialogResult.OK,
                Location = new Point(374, 396),
                Size = new Size(94, 32),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Location = new Point(476, 396),
                Size = new Size(88, 32),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            this.Load += (s, e) => {
                if (_shape != null && !string.IsNullOrEmpty(_shape.VideoPath))
                {
                    SeekPlayer(_shape.VideoTrimStart > 0 ? (double)_shape.VideoTrimStart : 0.0);
                }
            };

            this.FormClosing += (s, e) => {
                if (_videoPreviewBox != null && _videoPreviewBox.Image != null && _videoPreviewBox.Image != _shape?.CachedImage)
                {
                    try { _videoPreviewBox.Image.Dispose(); } catch { }
                    _videoPreviewBox.Image = null;
                }
            };

            UpdateDuration();
        }

        private void SeekPlayer(double sec)
        {
            if (_shape != null && !string.IsNullOrEmpty(_shape.VideoPath) && System.IO.File.Exists(_shape.VideoPath))
            {
                try
                {
                    var frame = NativeWmfVideoDecoder.ExtractFrameAtTime(_shape.VideoPath, sec);
                    if (frame != null)
                    {
                        var old = _videoPreviewBox.Image;
                        _videoPreviewBox.Image = frame;
                        if (old != null && old != _shape.CachedImage)
                        {
                            try { old.Dispose(); } catch { }
                        }
                    }
                    else if (_videoPreviewBox.Image == null && _shape.CachedImage != null)
                    {
                        _videoPreviewBox.Image = _shape.CachedImage;
                    }
                }
                catch { }
            }
            else if (_videoPreviewBox != null && _shape != null && _shape.CachedImage != null)
            {
                _videoPreviewBox.Image = _shape.CachedImage;
            }
        }

        private void UpdateDuration()
        {
            decimal diff = Math.Max(0M, _numEnd.Value - _numStart.Value);
            int mins = (int)(diff / 60M);
            decimal secs = diff % 60M;
            _lblDuration.Text = string.Format("Thời lượng: {0:D2}:{1:00.00}", mins, secs);
        }

        private void TimelinePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _timelinePanel.Width;
            int h = _timelinePanel.Height;

            // Filmstrip background
            using (SolidBrush darkB = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillRectangle(darkB, 0, 0, w, h);
            }

            // Film sprocket holes
            using (SolidBrush spB = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                for (int x = 6; x < w; x += 16)
                {
                    g.FillRectangle(spB, x, 3, 8, 6);
                    g.FillRectangle(spB, x, h - 9, 8, 6);
                }
            }

            // Active Track Region
            float maxVal = Math.Max(30f, (float)_numEnd.Value * 1.2f);
            float startRatio = Math.Max(0f, Math.Min(1f, (float)_numStart.Value / maxVal));
            float endRatio = Math.Max(0f, Math.Min(1f, (float)_numEnd.Value / maxVal));

            float startX = 20 + startRatio * (w - 40);
            float endX = 20 + endRatio * (w - 40);

            // Active Selected Range
            using (SolidBrush activeB = new SolidBrush(Color.FromArgb(120, 56, 189, 248)))
            {
                g.FillRectangle(activeB, startX, 12, endX - startX, h - 24);
            }

            // Green Start Handle
            using (SolidBrush greenB = new SolidBrush(Color.FromArgb(34, 197, 94)))
            {
                g.FillRectangle(greenB, startX - 4, 10, 6, h - 20);
                g.FillPolygon(greenB, new PointF[] {
                    new PointF(startX - 4, 10),
                    new PointF(startX + 8, 10),
                    new PointF(startX - 4, 20)
                });
            }

            // Red End Handle
            using (SolidBrush redB = new SolidBrush(Color.FromArgb(239, 68, 68)))
            {
                g.FillRectangle(redB, endX - 2, 10, 6, h - 20);
                g.FillPolygon(redB, new PointF[] {
                    new PointF(endX + 4, 10),
                    new PointF(endX - 8, 10),
                    new PointF(endX + 4, 20)
                });
            }
        }
    }
}

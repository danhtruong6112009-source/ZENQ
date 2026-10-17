using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    internal class ColorDoubleBufferedPanel : Panel
    {
        public ColorDoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do not paint background to completely eliminate flicker
        }
    }

    public class PhotoColorPickerDialogForm : Form
    {
        public Color SelectedColor { get; private set; }

        private Color _initialColor;
        private int _alpha = 255;
        private float _hue = 0f;        // 0..360
        private float _sat = 1f;        // 0..1
        private float _val = 1f;        // 0..1

        private ColorDoubleBufferedPanel _pnlSpectrum;
        private ColorDoubleBufferedPanel _pnlHue;
        private ColorDoubleBufferedPanel _pnlPreviewNew;
        private ColorDoubleBufferedPanel _pnlPreviewOld;

        private NumericUpDown _numR, _numG, _numB;
        private NumericUpDown _numH, _numS, _numL;
        private TrackBar _tbAlpha;
        private NumericUpDown _numAlpha;
        private TextBox _txtHex;
        private bool _isUpdating = false;
        private Bitmap _spectrumBmp;
        private Bitmap _hueBmp;


        public PhotoColorPickerDialogForm(Color initialColor)
        {
            _initialColor = initialColor;
            _alpha = initialColor.A;
            SelectedColor = initialColor;
            ColorToHsv(initialColor, out _hue, out _sat, out _val);

            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            InitializeUI();
            UpdateAllInputs();
        }

        private void InitializeUI()
        {
            Text = "Bảng chọn màu sắc & Kênh Alpha (Color & Transparency)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 440);
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // 1. Spectrum Box (256x256)
            _pnlSpectrum = new ColorDoubleBufferedPanel
            {
                Location = new Point(16, 16),
                Size = new Size(240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Cross
            };
            _pnlSpectrum.Paint += PnlSpectrum_Paint;
            _pnlSpectrum.MouseDown += PnlSpectrum_MouseDown;
            _pnlSpectrum.MouseMove += PnlSpectrum_MouseMove;
            Controls.Add(_pnlSpectrum);

            // 2. Hue Slider (24x240)
            _pnlHue = new ColorDoubleBufferedPanel
            {
                Location = new Point(268, 16),
                Size = new Size(26, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _pnlHue.Paint += PnlHue_Paint;
            _pnlHue.MouseDown += PnlHue_MouseDown;
            _pnlHue.MouseMove += PnlHue_MouseMove;
            Controls.Add(_pnlHue);
            // Pre-render Hue bar once
            _hueBmp = new Bitmap(_pnlHue.Width, _pnlHue.Height);
            using (Graphics hg = Graphics.FromImage(_hueBmp))
            {
                for (int y = 0; y < _pnlHue.Height; y++)
                {
                    float hue = (float)y / _pnlHue.Height * 360f;
                    using (Pen p = new Pen(HsvToRgb(hue, 1f, 1f)))
                    {
                        hg.DrawLine(p, 0, y, _pnlHue.Width, y);
                    }
                }
            }
            RenderSpectrumBitmap();


            // 3. Preview Area (New vs Old) với nền trong suốt Checkerboard
            Label lblPreview = new Label { Text = "Màu sắc & Độ trong", Location = new Point(312, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            Controls.Add(lblPreview);

            _pnlPreviewNew = new ColorDoubleBufferedPanel { Location = new Point(314, 38), Size = new Size(95, 34), BorderStyle = BorderStyle.FixedSingle };
            _pnlPreviewOld = new ColorDoubleBufferedPanel { Location = new Point(415, 38), Size = new Size(95, 34), BorderStyle = BorderStyle.FixedSingle };
            _pnlPreviewNew.Paint += PnlPreviewNew_Paint;
            _pnlPreviewOld.Paint += PnlPreviewOld_Paint;

            Label lblNew = new Label { Text = "Màu mới", Location = new Point(314, 74), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 7.8f) };
            Label lblOld = new Label { Text = "Hiện tại", Location = new Point(415, 74), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 7.8f) };
            Controls.Add(_pnlPreviewNew);
            Controls.Add(_pnlPreviewOld);
            Controls.Add(lblNew);
            Controls.Add(lblOld);

            // 4. Hex Input
            Label lblHex = new Label { Text = "Mã màu Hex:", Location = new Point(312, 100), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _txtHex = new TextBox { Location = new Point(395, 97), Size = new Size(115, 23), Font = new Font("Consolas", 9.5f) };
            _txtHex.TextChanged += TxtHex_TextChanged;
            Controls.Add(lblHex);
            Controls.Add(_txtHex);

            // 5. Hệ màu RGB
            Label lblRgbSec = new Label { Text = "Hệ màu RGB:", Location = new Point(312, 132), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Controls.Add(lblRgbSec);

            _numR = CreateNumeric(360, 156, 0, 255);
            _numG = CreateNumeric(415, 156, 0, 255);
            _numB = CreateNumeric(470, 156, 0, 255);
            Label lblR = new Label { Text = "R:", Location = new Point(344, 158), AutoSize = true, ForeColor = Color.FromArgb(185, 28, 28) };
            Label lblG = new Label { Text = "G:", Location = new Point(400, 158), AutoSize = true, ForeColor = Color.FromArgb(21, 128, 61) };
            Label lblB = new Label { Text = "B:", Location = new Point(455, 158), AutoSize = true, ForeColor = Color.FromArgb(29, 78, 216) };
            Controls.Add(lblR); Controls.Add(_numR);
            Controls.Add(lblG); Controls.Add(_numG);
            Controls.Add(lblB); Controls.Add(_numB);

            _numR.ValueChanged += (s, e) => OnRgbChanged();
            _numG.ValueChanged += (s, e) => OnRgbChanged();
            _numB.ValueChanged += (s, e) => OnRgbChanged();

            // 6. Hệ màu HSL
            Label lblHslSec = new Label { Text = "Hệ màu HSL:", Location = new Point(312, 192), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Controls.Add(lblHslSec);

            _numH = CreateNumeric(360, 216, 0, 360);
            _numS = CreateNumeric(415, 216, 0, 100);
            _numL = CreateNumeric(470, 216, 0, 100);
            Label lblH = new Label { Text = "H:", Location = new Point(344, 218), AutoSize = true };
            Label lblS = new Label { Text = "S:", Location = new Point(400, 218), AutoSize = true };
            Label lblL = new Label { Text = "L:", Location = new Point(455, 218), AutoSize = true };
            Controls.Add(lblH); Controls.Add(_numH);
            Controls.Add(lblS); Controls.Add(_numS);
            Controls.Add(lblL); Controls.Add(_numL);

            _numH.ValueChanged += (s, e) => OnHslChanged();
            _numS.ValueChanged += (s, e) => OnHslChanged();
            _numL.ValueChanged += (s, e) => OnHslChanged();

            // 7. Kênh Alpha / Độ trong suốt (0 - 100%)
            Label lblAlphaSec = new Label { Text = "Độ đục / Alpha (Opacity):", Location = new Point(312, 250), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Controls.Add(lblAlphaSec);

            _tbAlpha = new TrackBar
            {
                Location = new Point(310, 272),
                Size = new Size(150, 30),
                Minimum = 0,
                Maximum = 255,
                Value = _alpha,
                TickFrequency = 32
            };
            _tbAlpha.ValueChanged += (s, e) =>
            {
                if (_isUpdating) return;
                _alpha = _tbAlpha.Value;
                SelectedColor = Color.FromArgb(_alpha, SelectedColor.R, SelectedColor.G, SelectedColor.B);
                _isUpdating = true;
                _numAlpha.Value = (decimal)Math.Round(_alpha / 2.55f);
                _isUpdating = false;
                _pnlPreviewNew.Invalidate();
                UpdateHexText();
            };
            Controls.Add(_tbAlpha);

            _numAlpha = CreateNumeric(465, 273, 0, 100);
            _numAlpha.Value = (decimal)Math.Round(_alpha / 2.55f);
            _numAlpha.ValueChanged += (s, e) =>
            {
                if (_isUpdating) return;
                _alpha = Math.Max(0, Math.Min(255, (int)Math.Round((float)_numAlpha.Value * 2.55f)));
                _isUpdating = true;
                _tbAlpha.Value = _alpha;
                _isUpdating = false;
                SelectedColor = Color.FromArgb(_alpha, SelectedColor.R, SelectedColor.G, SelectedColor.B);
                _pnlPreviewNew.Invalidate();
                UpdateHexText();
            };
            Controls.Add(_numAlpha);

            Label lblPct = new Label { Text = "%", Location = new Point(518, 276), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139) };
            Controls.Add(lblPct);

            // 8. Bảng màu cơ bản (Quick Swatches)
            Label lblSw = new Label { Text = "Màu cơ bản:", Location = new Point(16, 270), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Controls.Add(lblSw);

            Color[] basicSwatches = new Color[]
            {
                Color.Black, Color.FromArgb(64, 64, 64), Color.FromArgb(128, 128, 128), Color.White,
                Color.FromArgb(239, 68, 68), Color.FromArgb(249, 115, 22), Color.FromArgb(245, 158, 11),
                Color.FromArgb(16, 185, 129), Color.FromArgb(6, 182, 212), Color.FromArgb(59, 130, 246),
                Color.FromArgb(99, 102, 241), Color.FromArgb(168, 85, 247), Color.FromArgb(236, 72, 153),
                Color.FromArgb(244, 63, 94)
            };

            for (int i = 0; i < basicSwatches.Length; i++)
            {
                Color sc = basicSwatches[i];
                Panel sw = new Panel
                {
                    Location = new Point(16 + i * 28, 292),
                    Size = new Size(24, 24),
                    BackColor = sc,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };
                sw.Click += (s, e) =>
                {
                    SetColor(Color.FromArgb(_alpha, sc.R, sc.G, sc.B));
                };
                Controls.Add(sw);
            }

            // Nút nhanh trong suốt 50% và 0%
            Button btn50 = new Button
            {
                Text = "Trong suốt 50%",
                Location = new Point(16, 328),
                Size = new Size(110, 26),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btn50.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn50.Click += (s, e) =>
            {
                _alpha = 128;
                _tbAlpha.Value = _alpha;
                _numAlpha.Value = 50;
                SelectedColor = Color.FromArgb(_alpha, SelectedColor.R, SelectedColor.G, SelectedColor.B);
                _pnlPreviewNew.Invalidate();
                UpdateHexText();
            };
            Controls.Add(btn50);

            Button btn100 = new Button
            {
                Text = "Đậm đặc 100%",
                Location = new Point(132, 328),
                Size = new Size(110, 26),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btn100.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn100.Click += (s, e) =>
            {
                _alpha = 255;
                _tbAlpha.Value = _alpha;
                _numAlpha.Value = 100;
                SelectedColor = Color.FromArgb(_alpha, SelectedColor.R, SelectedColor.G, SelectedColor.B);
                _pnlPreviewNew.Invalidate();
                UpdateHexText();
            };
            Controls.Add(btn100);

            // 9. Buttons OK / Cancel
            Button btnOk = new Button
            {
                Text = "Đồng ý",
                DialogResult = DialogResult.OK,
                Location = new Point(328, 385),
                Size = new Size(88, 32),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => Close();

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(426, 385),
                Size = new Size(88, 32),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) =>
            {
                SelectedColor = _initialColor;
                Close();
            };

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private NumericUpDown CreateNumeric(int x, int y, int min, int max)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(50, 23),
                Minimum = min,
                Maximum = max
            };
        }

        private void PnlSpectrum_Paint(object sender, PaintEventArgs e)
        {
            if (_spectrumBmp != null)
            {
                e.Graphics.DrawImageUnscaled(_spectrumBmp, 0, 0);
            }

            // Draw Selector Crosshair
            int cx = (int)(_sat * _pnlSpectrum.Width);
            int cy = (int)((1f - _val) * _pnlSpectrum.Height);
            using (Pen p = new Pen((_val > 0.5f) ? Color.Black : Color.White, 1.5f))
            {
                e.Graphics.DrawEllipse(p, cx - 4, cy - 4, 8, 8);
            }
        }

        private void PnlSpectrum_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PickSpectrum(e.X, e.Y);
        }

        private void PnlSpectrum_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PickSpectrum(e.X, e.Y);
        }

        private void PickSpectrum(int x, int y)
        {
            _sat = Math.Max(0f, Math.Min(1f, (float)x / _pnlSpectrum.Width));
            _val = Math.Max(0f, Math.Min(1f, 1f - ((float)y / _pnlSpectrum.Height)));
            Color rgb = HsvToRgb(_hue, _sat, _val);
            SelectedColor = Color.FromArgb(_alpha, rgb.R, rgb.G, rgb.B);
            _pnlSpectrum.Invalidate();
            UpdateAllInputs();
        }

        private void PnlHue_Paint(object sender, PaintEventArgs e)
        {
            if (_hueBmp != null)
            {
                e.Graphics.DrawImageUnscaled(_hueBmp, 0, 0);
            }

            int h = _pnlHue.Height;
            int w = _pnlHue.Width;
            int hy = (int)(_hue / 360f * h);
            using (Pen p = new Pen(Color.White, 2f))
            {
                e.Graphics.DrawRectangle(p, 1, Math.Max(0, hy - 2), w - 2, 4);
            }
            using (Pen p = new Pen(Color.Black, 1f))
            {
                e.Graphics.DrawRectangle(p, 0, Math.Max(0, hy - 3), w, 6);
            }
        }

        private void PnlHue_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PickHue(e.Y);
        }

        private void PnlHue_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PickHue(e.Y);
        }

        private void PickHue(int y)
        {
            _hue = Math.Max(0f, Math.Min(360f, (float)y / _pnlHue.Height * 360f));
            RenderSpectrumBitmap();
            Color rgb = HsvToRgb(_hue, _sat, _val);
            SelectedColor = Color.FromArgb(_alpha, rgb.R, rgb.G, rgb.B);
            _pnlHue.Invalidate();
            _pnlSpectrum.Invalidate();
            UpdateAllInputs();
        }

        private void SetColor(Color c)
        {
            _alpha = c.A;
            SelectedColor = c;
            ColorToHsv(c, out _hue, out _sat, out _val);
            RenderSpectrumBitmap();
            _pnlSpectrum.Invalidate();
            _pnlHue.Invalidate();
            UpdateAllInputs();
        }

        private void RenderSpectrumBitmap()
        {
            if (_pnlSpectrum == null) return;
            int w = _pnlSpectrum.Width;
            int h = _pnlSpectrum.Height;
            if (w <= 0 || h <= 0) return;

            if (_spectrumBmp == null || _spectrumBmp.Width != w || _spectrumBmp.Height != h)
            {
                _spectrumBmp?.Dispose();
                _spectrumBmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            var bmpData = _spectrumBmp.LockBits(
                new Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;

                    for (int y = 0; y < h; y++)
                    {
                        float v = 1f - ((float)y / (h - 1));
                        byte* row = ptr + (y * stride);

                        for (int x = 0; x < w; x++)
                        {
                            float s = (float)x / (w - 1);
                            Color c = HsvToRgb(_hue, s, v);

                            int px = x * 4;
                            row[px + 0] = c.B;
                            row[px + 1] = c.G;
                            row[px + 2] = c.R;
                            row[px + 3] = 255;
                        }
                    }
                }
            }
            finally
            {
                _spectrumBmp.UnlockBits(bmpData);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _spectrumBmp?.Dispose();
            _hueBmp?.Dispose();
        }

        private void UpdateAllInputs()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            _pnlPreviewNew.Invalidate();
            _pnlPreviewOld.Invalidate();
            UpdateHexText();

            _numR.Value = SelectedColor.R;
            _numG.Value = SelectedColor.G;
            _numB.Value = SelectedColor.B;

            if (_tbAlpha != null) _tbAlpha.Value = _alpha;
            if (_numAlpha != null) _numAlpha.Value = (decimal)Math.Round(_alpha / 2.55f);

            RgbToHsl(SelectedColor, out float h, out float s, out float l);
            _numH.Value = (decimal)Math.Round(h);
            _numS.Value = (decimal)Math.Round(s * 100);
            _numL.Value = (decimal)Math.Round(l * 100);

            _isUpdating = false;
        }

        private void UpdateHexText()
        {
            if (_alpha < 255)
                _txtHex.Text = $"#{SelectedColor.A:X2}{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
            else
                _txtHex.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        }

        private void OnRgbChanged()
        {
            if (_isUpdating) return;
            Color c = Color.FromArgb(_alpha, (int)_numR.Value, (int)_numG.Value, (int)_numB.Value);
            SetColor(c);
        }

        private void OnHslChanged()
        {
            if (_isUpdating) return;
            float h = (float)_numH.Value;
            float s = (float)_numS.Value / 100f;
            float l = (float)_numL.Value / 100f;
            Color rgb = HslToRgb(h, s, l);
            Color c = Color.FromArgb(_alpha, rgb.R, rgb.G, rgb.B);
            SetColor(c);
        }

        private void TxtHex_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            string hex = _txtHex.Text.Trim().TrimStart('#');
            if (hex.Length == 8)
            {
                try
                {
                    int a = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int r = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(4, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(6, 2), 16);
                    SetColor(Color.FromArgb(a, r, g, b));
                }
                catch { }
            }
            else if (hex.Length == 6)
            {
                try
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    SetColor(Color.FromArgb(_alpha, r, g, b));
                }
                catch { }
            }
        }

        private void PnlPreviewNew_Paint(object sender, PaintEventArgs e)
        {
            DrawCheckerboard(e.Graphics, _pnlPreviewNew.ClientRectangle);
            using (SolidBrush b = new SolidBrush(SelectedColor))
            {
                e.Graphics.FillRectangle(b, _pnlPreviewNew.ClientRectangle);
            }
        }

        private void PnlPreviewOld_Paint(object sender, PaintEventArgs e)
        {
            DrawCheckerboard(e.Graphics, _pnlPreviewOld.ClientRectangle);
            using (SolidBrush b = new SolidBrush(_initialColor))
            {
                e.Graphics.FillRectangle(b, _pnlPreviewOld.ClientRectangle);
            }
        }

        private static void DrawCheckerboard(Graphics g, Rectangle r)
        {
            int sq = 6;
            using (SolidBrush wB = new SolidBrush(Color.White))
            using (SolidBrush gB = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                for (int py = r.Y; py < r.Bottom; py += sq)
                {
                    for (int px = r.X; px < r.Right; px += sq)
                    {
                        bool alt = ((px / sq) + (py / sq)) % 2 == 0;
                        g.FillRectangle(alt ? wB : gB, px, py, Math.Min(sq, r.Right - px), Math.Min(sq, r.Bottom - py));
                    }
                }
            }
        }

        #region Color Conversions (HSV, HSL, RGB)
        private static Color HsvToRgb(float h, float s, float v)
        {
            h = (h % 360f + 360f) % 360f;
            int hi = (int)(h / 60f) % 6;
            float f = (h / 60f) - (int)(h / 60f);
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);

            float r = 0, g = 0, b = 0;
            switch (hi)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, (int)(r * 255f))),
                Math.Max(0, Math.Min(255, (int)(g * 255f))),
                Math.Max(0, Math.Min(255, (int)(b * 255f)))
            );
        }

        private static void ColorToHsv(Color color, out float h, out float s, out float v)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            v = max;
            s = (max == 0f) ? 0f : (delta / max);

            if (delta == 0f) h = 0f;
            else if (max == r) h = (60f * ((g - b) / delta) + 360f) % 360f;
            else if (max == g) h = 60f * ((b - r) / delta) + 120f;
            else h = 60f * ((r - g) / delta) + 240f;
        }

        private static void RgbToHsl(Color color, out float h, out float s, out float l)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            l = (max + min) / 2f;

            if (max == min)
            {
                h = 0f;
                s = 0f;
            }
            else
            {
                float d = max - min;
                s = (l > 0.5f) ? (d / (2f - max - min)) : (d / (max + min));
                if (max == r) h = (60f * ((g - b) / d) + 360f) % 360f;
                else if (max == g) h = 60f * ((b - r) / d) + 120f;
                else h = 60f * ((r - g) / d) + 240f;
            }
        }

        private static Color HslToRgb(float h, float s, float l)
        {
            h = (h % 360f + 360f) % 360f / 360f;
            float r, g, b;
            if (s == 0f)
            {
                r = g = b = l;
            }
            else
            {
                float q = (l < 0.5f) ? (l * (1f + s)) : (l + s - l * s);
                float p = 2f * l - q;
                r = HueToRgb(p, q, h + 1f / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3f);
            }
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, (int)(r * 255f))),
                Math.Max(0, Math.Min(255, (int)(g * 255f))),
                Math.Max(0, Math.Min(255, (int)(b * 255f)))
            );
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }
        #endregion
    }
}

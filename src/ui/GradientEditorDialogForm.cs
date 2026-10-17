using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class GradientEditorDialogForm : Form
    {
        public VectorFillType SelectedFillType { get; private set; } = VectorFillType.LinearGradient;
        public float SelectedAngle { get; private set; } = 0f;
        public List<PhotoGradientStop> SelectedStops { get; private set; } = new List<PhotoGradientStop>();

        private int _selectedStopIndex = 0;
        private ColorDoubleBufferedPanel _pnlGradientBar;
        private ColorDoubleBufferedPanel _pnlPreview;
        private RadioButton _rbLinear;
        private RadioButton _rbRadial;
        private TrackBar _tbAngle;
        private Label _lblAngleVal;
        private Panel _pnlStopColor;
        private TrackBar _tbStopAlpha;
        private Label _lblStopAlphaVal;
        private NumericUpDown _numStopPos;
        private Button _btnDeleteStop;
        private bool _isUpdating = false;

        public GradientEditorDialogForm(VectorFillType initialFillType, float initialAngle, Color color1, Color color2, List<PhotoGradientStop> stops)
        {
            SelectedFillType = (initialFillType == VectorFillType.RadialGradient) ? VectorFillType.RadialGradient : VectorFillType.LinearGradient;
            SelectedAngle = initialAngle;

            if (stops != null && stops.Count >= 2)
            {
                SelectedStops = stops.Select(s => s.Clone()).OrderBy(s => s.Position).ToList();
            }
            else
            {
                SelectedStops = new List<PhotoGradientStop>
                {
                    new PhotoGradientStop(color1.A > 0 ? color1 : Color.FromArgb(239, 68, 68), 0.0f),
                    new PhotoGradientStop(color2.A > 0 ? color2 : Color.FromArgb(59, 130, 246), 1.0f)
                };
            }

            DoubleBuffered = true;
            InitializeUI();
            UpdateStopControls();
        }

        private void InitializeUI()
        {
            this.Text = "Bộ Thiết Kế Dải Màu Gradient & Kênh Alpha (Gradient Editor)";
            this.Size = new Size(580, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f);

            // 1. Header
            Label lblHeader = new Label
            {
                Text = "Cấu hình dải màu đa sắc & độ trong suốt từng điểm màu",
                Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 14),
                AutoSize = true
            };
            this.Controls.Add(lblHeader);

            // 2. Chế độ dải màu: Tuyến tính vs Tỏa tròn
            Label lblType = new Label { Text = "Dạng chuyển sắc:", Location = new Point(20, 46), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            this.Controls.Add(lblType);

            _rbLinear = new RadioButton
            {
                Text = "Tuyến tính (Linear)",
                Location = new Point(150, 44),
                AutoSize = true,
                Checked = (SelectedFillType == VectorFillType.LinearGradient)
            };
            _rbLinear.CheckedChanged += (s, e) => {
                if (_rbLinear.Checked) { SelectedFillType = VectorFillType.LinearGradient; _tbAngle.Enabled = true; InvalidateCanvas(); }
            };
            this.Controls.Add(_rbLinear);

            _rbRadial = new RadioButton
            {
                Text = "Tỏa tròn (Radial)",
                Location = new Point(310, 44),
                AutoSize = true,
                Checked = (SelectedFillType == VectorFillType.RadialGradient)
            };
            _rbRadial.CheckedChanged += (s, e) => {
                if (_rbRadial.Checked) { SelectedFillType = VectorFillType.RadialGradient; _tbAngle.Enabled = false; InvalidateCanvas(); }
            };
            this.Controls.Add(_rbRadial);

            // 3. Góc dải màu (Angle)
            Label lblAngle = new Label { Text = "Góc xoay (Angle):", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            this.Controls.Add(lblAngle);

            _tbAngle = new TrackBar
            {
                Location = new Point(145, 76),
                Width = 230,
                Minimum = 0,
                Maximum = 360,
                Value = Math.Max(0, Math.Min(360, (int)SelectedAngle)),
                TickFrequency = 45,
                Enabled = (SelectedFillType == VectorFillType.LinearGradient)
            };
            _lblAngleVal = new Label { Text = $"{(int)SelectedAngle}°", Location = new Point(385, 80), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _tbAngle.ValueChanged += (s, e) => {
                SelectedAngle = _tbAngle.Value;
                _lblAngleVal.Text = $"{_tbAngle.Value}°";
                InvalidateCanvas();
            };
            this.Controls.Add(_tbAngle);
            this.Controls.Add(_lblAngleVal);

            // Nút bấm góc nhanh
            int[] quickAngles = new int[] { 0, 45, 90, 180, 270 };
            string[] quickLabels = new string[] { "0° (Trái->Phải)", "45° (Chéo)", "90° (Trên->Dưới)", "180° (Phải->Trái)", "270° (Dưới->Trên)" };
            int btnAngX = 20;
            for (int ai = 0; ai < quickAngles.Length; ai++)
            {
                int aVal = quickAngles[ai];
                Button bAng = new Button
                {
                    Text = quickLabels[ai],
                    Location = new Point(btnAngX, 114),
                    Size = new Size(100, 25),
                    BackColor = Color.FromArgb(241, 245, 249),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.8f)
                };
                bAng.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                bAng.Click += (s, e) => {
                    SelectedAngle = aVal;
                    _tbAngle.Value = aVal;
                    _lblAngleVal.Text = $"{aVal}°";
                    InvalidateCanvas();
                };
                this.Controls.Add(bAng);
                btnAngX += 106;
            }

            // 4. Thanh Gradient trực quan (Draggable Stops Bar)
            Label lblBarTitle = new Label { Text = "Thanh dải màu & các điểm dừng (Bấm vào thanh để thêm điểm):", Location = new Point(20, 150), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            this.Controls.Add(lblBarTitle);

            _pnlGradientBar = new ColorDoubleBufferedPanel
            {
                Location = new Point(20, 174),
                Size = new Size(520, 52),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _pnlGradientBar.Paint += PnlGradientBar_Paint;
            _pnlGradientBar.MouseDown += PnlGradientBar_MouseDown;
            _pnlGradientBar.MouseMove += PnlGradientBar_MouseMove;
            this.Controls.Add(_pnlGradientBar);

            // 5. Khung tinh chỉnh điểm màu đang chọn
            GroupBox gbStop = new GroupBox
            {
                Text = "Thuộc tính điểm dừng đang chọn",
                Location = new Point(20, 236),
                Size = new Size(330, 176),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(gbStop);

            Label lblColor = new Label { Text = "Màu sắc:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            gbStop.Controls.Add(lblColor);

            _pnlStopColor = new Panel
            {
                Location = new Point(80, 25),
                Size = new Size(42, 26),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _pnlStopColor.Click += (s, e) => PickActiveStopColor();
            gbStop.Controls.Add(_pnlStopColor);

            Button btnPickColor = new Button
            {
                Text = "Chọn màu & Alpha...",
                Location = new Point(130, 25),
                Size = new Size(130, 26),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.2f)
            };
            btnPickColor.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnPickColor.Click += (s, e) => PickActiveStopColor();
            gbStop.Controls.Add(btnPickColor);

            // Alpha slider cho stop này
            Label lblAlpha = new Label { Text = "Độ đục:", Location = new Point(14, 66), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            gbStop.Controls.Add(lblAlpha);

            _tbStopAlpha = new TrackBar
            {
                Location = new Point(80, 60),
                Width = 180,
                Minimum = 0,
                Maximum = 255,
                TickFrequency = 32
            };
            _lblStopAlphaVal = new Label { Text = "100%", Location = new Point(268, 66), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _tbStopAlpha.ValueChanged += (s, e) => {
                if (_isUpdating || _selectedStopIndex < 0 || _selectedStopIndex >= SelectedStops.Count) return;
                var st = SelectedStops[_selectedStopIndex];
                st.Color = Color.FromArgb(_tbStopAlpha.Value, st.Color.R, st.Color.G, st.Color.B);
                _lblStopAlphaVal.Text = $"{(int)(_tbStopAlpha.Value / 2.55f)}%";
                _pnlStopColor.BackColor = st.Color;
                InvalidateCanvas();
            };
            gbStop.Controls.Add(_tbStopAlpha);
            gbStop.Controls.Add(_lblStopAlphaVal);

            // Vị trí stop (0..100%)
            Label lblPos = new Label { Text = "Vị trí (%):", Location = new Point(14, 102), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            gbStop.Controls.Add(lblPos);

            _numStopPos = new NumericUpDown
            {
                Location = new Point(80, 100),
                Width = 65,
                Minimum = 0,
                Maximum = 100
            };
            _numStopPos.ValueChanged += (s, e) => {
                if (_isUpdating || _selectedStopIndex < 0 || _selectedStopIndex >= SelectedStops.Count) return;
                SelectedStops[_selectedStopIndex].Position = (float)_numStopPos.Value / 100f;
                InvalidateCanvas();
            };
            gbStop.Controls.Add(_numStopPos);

            // Thêm & Xóa điểm dừng
            Button btnAddStop = new Button
            {
                Text = "+ Thêm điểm",
                Location = new Point(14, 136),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.2f)
            };
            btnAddStop.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnAddStop.Click += (s, e) => AddNewStopAt(0.5f);
            gbStop.Controls.Add(btnAddStop);

            _btnDeleteStop = new Button
            {
                Text = "- Xóa điểm",
                Location = new Point(132, 136),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.2f)
            };
            _btnDeleteStop.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            _btnDeleteStop.Click += (s, e) => DeleteActiveStop();
            gbStop.Controls.Add(_btnDeleteStop);

            // 6. Preview Box (Hình xem trước trực tiếp)
            GroupBox gbPrev = new GroupBox
            {
                Text = "Hình xem trước (Live Preview)",
                Location = new Point(362, 236),
                Size = new Size(178, 176),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(gbPrev);

            _pnlPreview = new ColorDoubleBufferedPanel
            {
                Location = new Point(14, 26),
                Size = new Size(150, 136),
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlPreview.Paint += PnlPreview_Paint;
            gbPrev.Controls.Add(_pnlPreview);

            // 7. Mẫu Gradient dựng sẵn (Presets)
            Label lblPresets = new Label { Text = "Mẫu Gradient thiết kế sẵn:", Location = new Point(20, 420), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            this.Controls.Add(lblPresets);

            var presets = new (string Name, List<PhotoGradientStop> Stops, float Angle)[]
            {
                ("Đỏ 50% -> Xanh", new List<PhotoGradientStop> {
                    new PhotoGradientStop(Color.FromArgb(128, 239, 68, 68), 0.0f),
                    new PhotoGradientStop(Color.FromArgb(255, 59, 130, 246), 1.0f)
                }, 0f),
                ("Đen -> Trong suốt", new List<PhotoGradientStop> {
                    new PhotoGradientStop(Color.FromArgb(255, 15, 23, 42), 0.0f),
                    new PhotoGradientStop(Color.FromArgb(0, 15, 23, 42), 1.0f)
                }, 90f),
                ("Cầu vồng 7 sắc", new List<PhotoGradientStop> {
                    new PhotoGradientStop(Color.FromArgb(239, 68, 68), 0.0f),
                    new PhotoGradientStop(Color.FromArgb(249, 115, 22), 0.17f),
                    new PhotoGradientStop(Color.FromArgb(234, 179, 8), 0.33f),
                    new PhotoGradientStop(Color.FromArgb(34, 197, 94), 0.50f),
                    new PhotoGradientStop(Color.FromArgb(6, 182, 212), 0.67f),
                    new PhotoGradientStop(Color.FromArgb(59, 130, 246), 0.83f),
                    new PhotoGradientStop(Color.FromArgb(168, 85, 247), 1.0f)
                }, 0f),
                ("Hoàng hôn", new List<PhotoGradientStop> {
                    new PhotoGradientStop(Color.FromArgb(245, 158, 11), 0.0f),
                    new PhotoGradientStop(Color.FromArgb(236, 72, 153), 0.5f),
                    new PhotoGradientStop(Color.FromArgb(79, 70, 229), 1.0f)
                }, 45f)
            };

            int preX = 20;
            for (int pi = 0; pi < presets.Length; pi++)
            {
                var p = presets[pi];
                Button bPre = new Button
                {
                    Text = p.Name,
                    Location = new Point(preX, 442),
                    Size = new Size(122, 26),
                    BackColor = Color.FromArgb(241, 245, 249),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.8f)
                };
                bPre.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                bPre.Click += (s, e) => {
                    SelectedStops = p.Stops.Select(st => st.Clone()).ToList();
                    SelectedAngle = p.Angle;
                    _tbAngle.Value = (int)SelectedAngle;
                    _lblAngleVal.Text = $"{(int)SelectedAngle}°";
                    _selectedStopIndex = 0;
                    UpdateStopControls();
                    InvalidateCanvas();
                };
                this.Controls.Add(bPre);
                preX += 130;
            }

            // 8. Buttons OK / Cancel
            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(452, 480),
                Size = new Size(88, 30),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;

            Button btnOk = new Button
            {
                Text = "Áp dụng",
                DialogResult = DialogResult.OK,
                Location = new Point(354, 480),
                Size = new Size(88, 30),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;
        }

        private void InvalidateCanvas()
        {
            _pnlGradientBar?.Invalidate();
            _pnlPreview?.Invalidate();
        }

        private void UpdateStopControls()
        {
            if (_selectedStopIndex < 0 || _selectedStopIndex >= SelectedStops.Count)
            {
                _selectedStopIndex = 0;
            }

            var st = SelectedStops[_selectedStopIndex];
            _isUpdating = true;
            _pnlStopColor.BackColor = st.Color;
            _tbStopAlpha.Value = st.Color.A;
            _lblStopAlphaVal.Text = $"{(int)(st.Color.A / 2.55f)}%";
            _numStopPos.Value = (decimal)Math.Round(st.Position * 100f);
            _btnDeleteStop.Enabled = (SelectedStops.Count > 2);
            _isUpdating = false;

            InvalidateCanvas();
        }

        private void PickActiveStopColor()
        {
            if (_selectedStopIndex < 0 || _selectedStopIndex >= SelectedStops.Count) return;
            var st = SelectedStops[_selectedStopIndex];
            using (var dlg = new PhotoColorPickerDialogForm(st.Color))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    st.Color = dlg.SelectedColor;
                    UpdateStopControls();
                }
            }
        }

        private void AddNewStopAt(float pos)
        {
            Color interp = GetInterpolatedColor(pos);
            SelectedStops.Add(new PhotoGradientStop(interp, Math.Max(0f, Math.Min(1f, pos))));
            SelectedStops = SelectedStops.OrderBy(s => s.Position).ToList();
            _selectedStopIndex = SelectedStops.FindIndex(s => Math.Abs(s.Position - pos) < 0.01f);
            if (_selectedStopIndex < 0) _selectedStopIndex = SelectedStops.Count - 1;
            UpdateStopControls();
        }

        private void DeleteActiveStop()
        {
            if (SelectedStops.Count <= 2) return;
            SelectedStops.RemoveAt(_selectedStopIndex);
            if (_selectedStopIndex >= SelectedStops.Count) _selectedStopIndex = SelectedStops.Count - 1;
            UpdateStopControls();
        }

        private Color GetInterpolatedColor(float pos)
        {
            var sorted = SelectedStops.OrderBy(s => s.Position).ToList();
            if (pos <= sorted[0].Position) return sorted[0].Color;
            if (pos >= sorted[sorted.Count - 1].Position) return sorted[sorted.Count - 1].Color;

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (pos >= sorted[i].Position && pos <= sorted[i + 1].Position)
                {
                    float span = sorted[i + 1].Position - sorted[i].Position;
                    float factor = (span > 0) ? (pos - sorted[i].Position) / span : 0f;
                    Color c1 = sorted[i].Color;
                    Color c2 = sorted[i + 1].Color;
                    int a = (int)(c1.A + (c2.A - c1.A) * factor);
                    int r = (int)(c1.R + (c2.R - c1.R) * factor);
                    int g = (int)(c1.G + (c2.G - c1.G) * factor);
                    int b = (int)(c1.B + (c2.B - c1.B) * factor);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            return sorted[0].Color;
        }

        private void PnlGradientBar_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int barH = 26;
            Rectangle barRect = new Rectangle(12, 4, _pnlGradientBar.Width - 24, barH);

            // Nền bàn cờ trong suốt (Checkerboard)
            DrawCheckerboard(g, barRect);

            // Dải màu gradient chính
            if (SelectedStops.Count >= 2 && barRect.Width > 2)
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(barRect, Color.Black, Color.White, 0f))
                {
                    var sorted = SelectedStops.OrderBy(s => s.Position).ToList();
                    ColorBlend cb = new ColorBlend(sorted.Count);
                    cb.Colors = sorted.Select(s => s.Color).ToArray();
                    cb.Positions = sorted.Select(s => Math.Max(0f, Math.Min(1f, s.Position))).ToArray();
                    cb.Positions[0] = 0f;
                    cb.Positions[cb.Positions.Length - 1] = 1f;
                    lgb.InterpolationColors = cb;
                    g.FillRectangle(lgb, barRect);
                }
            }

            using (Pen borderPen = new Pen(Color.FromArgb(148, 163, 184), 1f))
            {
                g.DrawRectangle(borderPen, barRect);
            }

            // Vẽ các con trỏ Stops
            for (int i = 0; i < SelectedStops.Count; i++)
            {
                var st = SelectedStops[i];
                float markerX = barRect.X + st.Position * barRect.Width;
                bool isSelected = (i == _selectedStopIndex);

                PointF[] markerArrow = new PointF[]
                {
                    new PointF(markerX, barRect.Bottom + 1),
                    new PointF(markerX - 6, barRect.Bottom + 8),
                    new PointF(markerX - 6, barRect.Bottom + 18),
                    new PointF(markerX + 6, barRect.Bottom + 18),
                    new PointF(markerX + 6, barRect.Bottom + 8)
                };

                using (SolidBrush b = new SolidBrush(st.Color))
                using (Pen p = new Pen(isSelected ? Color.FromArgb(37, 99, 235) : Color.FromArgb(71, 85, 105), isSelected ? 2.5f : 1f))
                {
                    g.FillPolygon(b, markerArrow);
                    g.DrawPolygon(p, markerArrow);
                }
            }
        }

        private void PnlGradientBar_MouseDown(object sender, MouseEventArgs e)
        {
            Rectangle barRect = new Rectangle(12, 4, _pnlGradientBar.Width - 24, 26);
            if (e.Button == MouseButtons.Left)
            {
                // Kiểm tra click vào stop marker
                for (int i = 0; i < SelectedStops.Count; i++)
                {
                    float markerX = barRect.X + SelectedStops[i].Position * barRect.Width;
                    RectangleF mHit = new RectangleF(markerX - 8, barRect.Bottom, 16, 22);
                    if (mHit.Contains(e.Location))
                    {
                        _selectedStopIndex = i;
                        UpdateStopControls();
                        return;
                    }
                }

                // Click trên thanh: Thêm điểm dừng mới tại vị trí click
                if (barRect.Contains(e.Location))
                {
                    float clickedPos = Math.Max(0f, Math.Min(1f, (float)(e.X - barRect.X) / barRect.Width));
                    AddNewStopAt(clickedPos);
                }
            }
        }

        private void PnlGradientBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _selectedStopIndex >= 0 && _selectedStopIndex < SelectedStops.Count)
            {
                Rectangle barRect = new Rectangle(12, 4, _pnlGradientBar.Width - 24, 26);
                float newPos = Math.Max(0f, Math.Min(1f, (float)(e.X - barRect.X) / barRect.Width));
                SelectedStops[_selectedStopIndex].Position = (float)Math.Round(newPos, 2);
                _numStopPos.Value = (decimal)Math.Round(newPos * 100f);
                InvalidateCanvas();
            }
        }

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(10, 10, _pnlPreview.Width - 20, _pnlPreview.Height - 20);
            DrawCheckerboard(g, r);

            if (SelectedFillType == VectorFillType.LinearGradient)
            {
                if (r.Width > 2 && r.Height > 2 && SelectedStops.Count >= 2)
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.Black, Color.White, SelectedAngle))
                    {
                        var sorted = SelectedStops.OrderBy(s => s.Position).ToList();
                        ColorBlend cb = new ColorBlend(sorted.Count);
                        cb.Colors = sorted.Select(s => s.Color).ToArray();
                        cb.Positions = sorted.Select(s => Math.Max(0f, Math.Min(1f, s.Position))).ToArray();
                        cb.Positions[0] = 0f;
                        cb.Positions[cb.Positions.Length - 1] = 1f;
                        lgb.InterpolationColors = cb;
                        g.FillRectangle(lgb, r);
                    }
                }
            }
            else // RadialGradient
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(r);
                    using (PathGradientBrush pgb = new PathGradientBrush(gp))
                    {
                        if (SelectedStops.Count >= 2)
                        {
                            var sorted = SelectedStops.OrderBy(s => s.Position).ToList();
                            ColorBlend cb = new ColorBlend(sorted.Count);
                            cb.Colors = sorted.Select(s => s.Color).ToArray();
                            cb.Positions = sorted.Select(s => Math.Max(0f, Math.Min(1f, s.Position))).ToArray();
                            cb.Positions[0] = 0f;
                            cb.Positions[cb.Positions.Length - 1] = 1f;
                            pgb.InterpolationColors = cb;
                        }
                        g.FillEllipse(pgb, r);
                    }
                }
            }

            using (Pen borderPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            {
                g.DrawRectangle(borderPen, r);
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
    }
}

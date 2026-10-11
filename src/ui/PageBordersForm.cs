using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class PageBordersForm : Form
    {
        public bool HasBorder { get; set; }
        public Color BorderColor { get; set; }
        public float BorderWidth { get; set; }
        public int BorderStyleType { get; set; } // 0: None, 1: Box, 2: Shadow, 3: 3-D, 4: Double

        private int SelectedSetting = 1; // 0: None, 1: Box, 2: Shadow, 3: 3-D
        private int SelectedStyleIndex = 0; // 0: Solid, 1: Dash, 2: Double, 3: Triple
        private Color CurrentBorderColor = Color.FromArgb(43, 87, 154);
        private float CurrentBorderWidth = 2.0f;

        public PageBordersForm(bool currentHasBorder, Color currentColor, float currentWidth)
        {
            this.HasBorder = currentHasBorder;
            this.CurrentBorderColor = currentColor;
            this.CurrentBorderWidth = currentWidth;
            this.SelectedSetting = currentHasBorder ? 1 : 0;

            this.Text = "Viền trang và Đổ bóng (Borders and Shading)";
            this.Size = new Size(540, 440);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.DoubleBuffered = true;

            InitControls();
        }

        private void InitControls()
        {
            Panel pnlPreview = new Panel
            {
                Location = new Point(290, 60),
                Size = new Size(215, 260),
                BackColor = Color.White
            };
            pnlPreview.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                Rectangle b = new Rectangle(10, 10, pnlPreview.Width - 20, pnlPreview.Height - 20);

                g.FillRectangle(Brushes.White, b);
                g.DrawRectangle(Pens.LightGray, b);

                if (SelectedSetting > 0)
                {
                    using (Pen p = new Pen(CurrentBorderColor, SelectedSetting == 2 ? CurrentBorderWidth + 1 : CurrentBorderWidth))
                    {
                        if (SelectedStyleIndex == 1) p.DashStyle = DashStyle.Dash;

                        if (SelectedSetting == 2) // Shadow
                        {
                            using (SolidBrush shB = new SolidBrush(Color.FromArgb(100, 100, 100)))
                            {
                                g.FillRectangle(shB, b.X + 24, b.Y + b.Height - 18, b.Width - 44, 4);
                                g.FillRectangle(shB, b.X + b.Width - 18, b.Y + 24, 4, b.Height - 44);
                            }
                        }

                        g.DrawRectangle(p, b.X + 20, b.Y + 20, b.Width - 40, b.Height - 40);

                        if (SelectedSetting == 3 || SelectedStyleIndex == 2) // Double
                        {
                            g.DrawRectangle(p, b.X + 24, b.Y + 24, b.Width - 48, b.Height - 48);
                        }
                    }
                }

                // Sample document lines inside preview
                using (Pen linePen = new Pen(Color.FromArgb(220, 220, 220), 2f))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        g.DrawLine(linePen, b.X + 32, b.Y + 36 + i * 16, b.X + b.Width - 32, b.Y + 36 + i * 16);
                    }
                }

                using (Font pf = new Font("Segoe UI", 8f))
                {
                    g.DrawString("Xem trước viền trang", pf, Brushes.DimGray, b.X + 30, b.Y + b.Height - 32);
                }
            };
            this.Controls.Add(pnlPreview);

            // Setting Buttons (None, Box, Shadow, 3-D)
            string[] settingNames = new string[] { "Không (None)", "Khung (Box)", "Bóng đổ (Shadow)", "3-D" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                Button btn = new Button
                {
                    Text = settingNames[i],
                    Location = new Point(20, 60 + i * 44),
                    Size = new Size(110, 36),
                    BackColor = (SelectedSetting == idx) ? Color.FromArgb(225, 238, 255) : Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btn.FlatAppearance.BorderColor = (SelectedSetting == idx) ? Color.FromArgb(68, 114, 196) : Color.LightGray;
                btn.Click += (s, e) =>
                {
                    SelectedSetting = idx;
                    HasBorder = (SelectedSetting > 0);
                    pnlPreview.Invalidate();
                    this.Invalidate();
                };
                this.Controls.Add(btn);
            }

            // Style List Box
            Label lblStyle = new Label { Text = "Kiểu viền (Style):", Location = new Point(145, 42), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            this.Controls.Add(lblStyle);

            ListBox lbStyle = new ListBox
            {
                Location = new Point(145, 60),
                Size = new Size(130, 100),
                Font = new Font("Segoe UI", 9)
            };
            lbStyle.Items.AddRange(new object[] { "────── Nét liền", "─ ─ ─ Nét đứt", "══════ Nét đôi", "≡≡≡≡≡≡ Nét ba" });
            lbStyle.SelectedIndex = 0;
            lbStyle.SelectedIndexChanged += (s, e) =>
            {
                SelectedStyleIndex = lbStyle.SelectedIndex;
                pnlPreview.Invalidate();
            };
            this.Controls.Add(lbStyle);

            // Color Picker Button
            Label lblColor = new Label { Text = "Màu viền (Color):", Location = new Point(145, 170), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            this.Controls.Add(lblColor);

            Button btnColor = new Button
            {
                Text = "🎨 Chọn màu...",
                Location = new Point(145, 190),
                Size = new Size(130, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = CurrentBorderColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        CurrentBorderColor = cd.Color;
                        pnlPreview.Invalidate();
                    }
                }
            };
            this.Controls.Add(btnColor);

            // Width DropDown
            Label lblWidth = new Label { Text = "Độ dày (Width):", Location = new Point(145, 230), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            this.Controls.Add(lblWidth);

            ComboBox cbWidth = new ComboBox
            {
                Location = new Point(145, 250),
                Size = new Size(130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbWidth.Items.AddRange(new object[] { "1/2 pt (Mảnh)", "1 pt (Chuẩn)", "1 1/2 pt", "2 1/4 pt (Vừa)", "3 pt (Dày)", "4 1/2 pt", "6 pt (Đậm)" });
            cbWidth.SelectedIndex = 2;
            cbWidth.SelectedIndexChanged += (s, e) =>
            {
                float[] widths = new float[] { 0.5f, 1.0f, 1.5f, 2.25f, 3.0f, 4.5f, 6.0f };
                CurrentBorderWidth = widths[Math.Max(0, Math.Min(widths.Length - 1, cbWidth.SelectedIndex))];
                pnlPreview.Invalidate();
            };
            this.Controls.Add(cbWidth);

            // OK & Cancel Buttons
            Button btnOK = new Button
            {
                Text = "Đồng ý (OK)",
                Location = new Point(320, 345),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(43, 87, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOK.Click += (s, e) =>
            {
                this.HasBorder = (SelectedSetting > 0);
                this.BorderColor = CurrentBorderColor;
                this.BorderWidth = CurrentBorderWidth;
                this.BorderStyleType = SelectedSetting;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(425, 345),
                Size = new Size(80, 34),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            using (Font tf = new Font("Segoe UI", 11, FontStyle.Bold))
            {
                g.DrawString("Cài đặt Viền trang (Page Border Settings)", tf, Brushes.Navy, 20, 15);
            }
        }
    }
}

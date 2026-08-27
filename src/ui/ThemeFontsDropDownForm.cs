using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ThemeFontPairItem
    {
        public string Name { get; set; }
        public string HeadingFont { get; set; }
        public string BodyFont { get; set; }

        public ThemeFontPairItem(string name, string hFont, string bFont)
        {
            Name = name;
            HeadingFont = hFont;
            BodyFont = bFont;
        }
    }

    public class ThemeFontsDropDownForm : Form
    {
        public Action<ThemeFontPairItem> OnFontPairSelected;
        public Action OnCustomizeFontsClicked;

        private List<ThemeFontPairItem> _fontPairs;
        private string _activePairName;

        public ThemeFontsDropDownForm(Point location, string activePairName = "Office")
        {
            _activePairName = activePairName ?? "Office";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(270, 420);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _fontPairs = new List<ThemeFontPairItem>
            {
                new ThemeFontPairItem("Office (Calibri)", "Calibri Light", "Calibri"),
                new ThemeFontPairItem("Truyền thống (Times New Roman)", "Times New Roman", "Times New Roman"),
                new ThemeFontPairItem("Hiện đại (Segoe UI)", "Segoe UI Semibold", "Segoe UI"),
                new ThemeFontPairItem("Phổ thông (Arial)", "Arial Bold", "Arial"),
                new ThemeFontPairItem("Sách báo & Văn chương (Georgia)", "Georgia", "Garamond"),
                new ThemeFontPairItem("Trang nhã (Trebuchet MS)", "Trebuchet MS", "Calibri"),
                new ThemeFontPairItem("Rõ nét (Verdana)", "Verdana", "Tahoma"),
                new ThemeFontPairItem("Thư tịch (Cambria)", "Cambria", "Cambria"),
                new ThemeFontPairItem("Aptos (Office 365)", "Aptos Display", "Aptos"),
                new ThemeFontPairItem("Kỹ thuật & Lập trình (Consolas)", "Consolas", "Segoe UI")
            };

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(8)
            };

            Label lblHeader = new Label
            {
                Text = "Cặp phông chữ chủ đề (Theme Fonts)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(10, 8),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblHeader);

            int itemH = 34;
            int startY = 32;

            for (int i = 0; i < _fontPairs.Count; i++)
            {
                var pair = _fontPairs[i];
                int py = startY + i * (itemH + 2);
                bool isSel = string.Equals(pair.Name, _activePairName, StringComparison.OrdinalIgnoreCase);

                Control row = new Control
                {
                    Location = new Point(8, py),
                    Size = new Size(236, itemH),
                    Cursor = Cursors.Hand
                };

                bool isHovered = false;
                row.MouseEnter += (s, e) => { isHovered = true; row.Invalidate(); };
                row.MouseLeave += (s, e) => { isHovered = false; row.Invalidate(); };

                row.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    if (isSel || isHovered)
                    {
                        using (SolidBrush b = new SolidBrush(isSel ? Color.FromArgb(239, 246, 255) : Color.FromArgb(241, 245, 249)))
                        {
                            g.FillRectangle(b, 0, 0, row.Width, row.Height);
                        }
                        using (Pen p = new Pen(isSel ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225), 1f))
                        {
                            g.DrawRectangle(p, 0, 0, row.Width - 1, row.Height - 1);
                        }
                    }

                    // Large "A" sample icon
                    using (Font af = new Font("Segoe UI", 11.0f, FontStyle.Bold))
                    using (SolidBrush ab = new SolidBrush(isSel ? Color.FromArgb(37, 99, 235) : Color.FromArgb(71, 85, 105)))
                    {
                        g.DrawString("A", af, ab, 8, 5);
                    }

                    // Heading font name
                    using (Font hf = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    using (SolidBrush hb = new SolidBrush(isSel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(30, 41, 59)))
                    {
                        g.DrawString(pair.HeadingFont, hf, hb, 30, 3);
                    }

                    // Body font name
                    using (Font bf = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                    using (SolidBrush bb = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(pair.BodyFont, bf, bb, 30, 18);
                    }
                };

                row.Click += (s, e) =>
                {
                    if (OnFontPairSelected != null) OnFontPairSelected(pair);
                    Close();
                };

                scrollPanel.Controls.Add(row);
            }

            // Bottom Customize Button
            int customY = startY + _fontPairs.Count * (itemH + 2) + 6;
            Button btnCustom = new Button
            {
                Text = "🔤  Tùy chỉnh phông chữ (Customize Fonts)...",
                Location = new Point(8, customY),
                Size = new Size(236, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnCustom.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCustom.Click += (s, e) =>
            {
                Close();
                if (OnCustomizeFontsClicked != null) OnCustomizeFontsClicked();
            };
            scrollPanel.Controls.Add(btnCustom);

            Controls.Add(scrollPanel);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class WriterThemeItem
    {
        public string Name { get; set; }
        public string HeadingFont { get; set; }
        public string BodyFont { get; set; }
        public Color[] Palette { get; set; }

        public WriterThemeItem(string name, string hFont, string bFont, Color[] palette)
        {
            Name = name;
            HeadingFont = hFont;
            BodyFont = bFont;
            Palette = palette;
        }
    }

    public class WriterThemesDropDownForm : Form
    {
        public Action<WriterThemeItem> OnThemeSelected;
        private List<WriterThemeItem> _themes;
        private string _activeThemeName;

        public WriterThemesDropDownForm(Point location, string activeThemeName = "Chuẩn Word (Blue)")
        {
            _activeThemeName = activeThemeName ?? "Office";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(410, 480);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _themes = new List<WriterThemeItem>
            {
                new WriterThemeItem("Office", "Calibri Light", "Calibri", new Color[] {
                    Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0)
                }),
                new WriterThemeItem("Hiện đại (Modern)", "Segoe UI Semibold", "Segoe UI", new Color[] {
                    Color.FromArgb(37, 99, 235), Color.FromArgb(59, 130, 246), Color.FromArgb(147, 197, 253), Color.FromArgb(219, 234, 254)
                }),
                new WriterThemeItem("Trang trọng (Formal)", "Times New Roman", "Times New Roman", new Color[] {
                    Color.FromArgb(30, 58, 138), Color.FromArgb(30, 64, 175), Color.FromArgb(59, 130, 246), Color.FromArgb(147, 197, 253)
                }),
                new WriterThemeItem("Cổ điển (Classic)", "Georgia", "Garamond", new Color[] {
                    Color.FromArgb(120, 53, 15), Color.FromArgb(180, 83, 9), Color.FromArgb(217, 119, 6), Color.FromArgb(253, 230, 138)
                }),
                new WriterThemeItem("Ngọc lục bảo (Emerald)", "Trebuchet MS", "Segoe UI", new Color[] {
                    Color.FromArgb(6, 95, 70), Color.FromArgb(5, 150, 105), Color.FromArgb(16, 185, 129), Color.FromArgb(110, 231, 183)
                }),
                new WriterThemeItem("Hoàng hôn (Sunset)", "Century Gothic", "Calibri", new Color[] {
                    Color.FromArgb(154, 52, 18), Color.FromArgb(234, 88, 12), Color.FromArgb(249, 115, 22), Color.FromArgb(254, 215, 170)
                }),
                new WriterThemeItem("Hoàng gia (Royal Purple)", "Calibri", "Calibri", new Color[] {
                    Color.FromArgb(88, 28, 135), Color.FromArgb(124, 58, 237), Color.FromArgb(139, 92, 246), Color.FromArgb(221, 214, 254)
                }),
                new WriterThemeItem("Thạch phiến (Slate)", "Segoe UI", "Arial", new Color[] {
                    Color.FromArgb(30, 41, 59), Color.FromArgb(51, 65, 85), Color.FromArgb(100, 116, 139), Color.FromArgb(203, 213, 225)
                }),
                new WriterThemeItem("Đại dương (Teal Ocean)", "Aptos Display", "Aptos", new Color[] {
                    Color.FromArgb(19, 78, 74), Color.FromArgb(13, 148, 136), Color.FromArgb(20, 184, 166), Color.FromArgb(153, 246, 228)
                }),
                new WriterThemeItem("Đơn sắc (Monochrome)", "Arial", "Arial", new Color[] {
                    Color.FromArgb(17, 24, 39), Color.FromArgb(55, 65, 81), Color.FromArgb(107, 114, 128), Color.FromArgb(229, 231, 235)
                })
            };

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12)
            };

            Label lblHeader = new Label
            {
                Text = "Chủ đề tài liệu Word & Office (Themes)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, 10),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblHeader);

            int cardW = 180;
            int cardH = 68;
            int startX = 12;
            int startY = 36;
            int gapX = 10;
            int gapY = 8;

            for (int i = 0; i < _themes.Count; i++)
            {
                var theme = _themes[i];
                int row = i / 2;
                int col = i % 2;
                int px = startX + col * (cardW + gapX);
                int py = startY + row * (cardH + gapY);

                bool isSel = string.Equals(theme.Name, _activeThemeName, StringComparison.OrdinalIgnoreCase) ||
                             (_activeThemeName.StartsWith("Chuẩn") && theme.Name == "Office");

                Control card = new Control
                {
                    Location = new Point(px, py),
                    Size = new Size(cardW, cardH),
                    Cursor = Cursors.Hand
                };

                bool isHovered = false;
                card.MouseEnter += (s, e) => { isHovered = true; card.Invalidate(); };
                card.MouseLeave += (s, e) => { isHovered = false; card.Invalidate(); };

                card.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Rectangle bounds = new Rectangle(0, 0, cardW, cardH);
                    using (SolidBrush bgBrush = new SolidBrush(isSel ? Color.FromArgb(239, 246, 255) : (isHovered ? Color.FromArgb(241, 245, 249) : Color.White)))
                    {
                        g.FillRectangle(bgBrush, bounds);
                    }

                    using (Pen borderPen = new Pen(isSel ? Color.FromArgb(37, 99, 235) : (isHovered ? Color.FromArgb(148, 163, 184) : Color.FromArgb(226, 232, 240)), isSel ? 1.8f : 1.0f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, cardW - 1, cardH - 1);
                    }

                    // 4-Color Swatch Bar
                    int swatchX = 8;
                    int swatchY = 8;
                    for (int c = 0; c < 4; c++)
                    {
                        using (SolidBrush swBrush = new SolidBrush(theme.Palette[c]))
                        {
                            g.FillRectangle(swBrush, swatchX + c * 10, swatchY, 9, 20);
                        }
                    }

                    // Theme Name
                    using (Font titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    using (SolidBrush titleBrush = new SolidBrush(isSel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(30, 41, 59)))
                    {
                        g.DrawString(theme.Name, titleFont, titleBrush, 54, 8);
                    }

                    // Font Pairing preview
                    using (Font fontLabel = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                    using (SolidBrush fontBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(theme.HeadingFont + " / " + theme.BodyFont, fontLabel, fontBrush, 8, 38);
                    }
                };

                card.Click += (s, e) =>
                {
                    if (OnThemeSelected != null) OnThemeSelected(theme);
                    Close();
                };

                scrollPanel.Controls.Add(card);
            }

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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ThemeColorPaletteItem
    {
        public string Name { get; set; }
        public Color[] Colors { get; set; }

        public ThemeColorPaletteItem(string name, Color[] colors)
        {
            Name = name;
            Colors = colors;
        }
    }

    public class ThemeColorsDropDownForm : Form
    {
        public Action<ThemeColorPaletteItem> OnPaletteSelected;
        public Action OnCustomizeColorsClicked;

        private List<ThemeColorPaletteItem> _palettes;
        private string _activePaletteName;

        public ThemeColorsDropDownForm(Point location, string activePaletteName = "Office")
        {
            _activePaletteName = activePaletteName ?? "Office";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(260, 390);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _palettes = new List<ThemeColorPaletteItem>
            {
                new ThemeColorPaletteItem("Office", new Color[] {
                    Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0), Color.FromArgb(91, 155, 213), Color.FromArgb(112, 173, 71)
                }),
                new ThemeColorPaletteItem("Xanh lam (Blue)", new Color[] {
                    Color.FromArgb(43, 87, 154), Color.FromArgb(65, 138, 179), Color.FromArgb(58, 110, 165), Color.FromArgb(104, 176, 171), Color.FromArgb(143, 193, 212), Color.FromArgb(200, 225, 235)
                }),
                new ThemeColorPaletteItem("Xanh lam ấm (Blue Warm)", new Color[] {
                    Color.FromArgb(32, 84, 147), Color.FromArgb(71, 115, 170), Color.FromArgb(217, 56, 30), Color.FromArgb(242, 142, 43), Color.FromArgb(225, 87, 89), Color.FromArgb(118, 183, 178)
                }),
                new ThemeColorPaletteItem("Xanh lục (Green Emerald)", new Color[] {
                    Color.FromArgb(5, 150, 105), Color.FromArgb(16, 185, 129), Color.FromArgb(52, 211, 153), Color.FromArgb(110, 231, 183), Color.FromArgb(4, 120, 87), Color.FromArgb(6, 78, 59)
                }),
                new ThemeColorPaletteItem("Đỏ cam (Red Orange)", new Color[] {
                    Color.FromArgb(234, 88, 12), Color.FromArgb(249, 115, 22), Color.FromArgb(220, 38, 38), Color.FromArgb(239, 68, 68), Color.FromArgb(251, 146, 60), Color.FromArgb(254, 215, 170)
                }),
                new ThemeColorPaletteItem("Tím hoàng gia (Violet)", new Color[] {
                    Color.FromArgb(124, 58, 237), Color.FromArgb(139, 92, 246), Color.FromArgb(167, 139, 250), Color.FromArgb(196, 181, 253), Color.FromArgb(91, 33, 182), Color.FromArgb(76, 29, 149)
                }),
                new ThemeColorPaletteItem("Hổ phách & Vàng (Amber)", new Color[] {
                    Color.FromArgb(217, 119, 6), Color.FromArgb(245, 158, 11), Color.FromArgb(251, 191, 36), Color.FromArgb(253, 230, 138), Color.FromArgb(180, 83, 9), Color.FromArgb(120, 53, 15)
                }),
                new ThemeColorPaletteItem("Xanh mòng két (Teal)", new Color[] {
                    Color.FromArgb(13, 148, 136), Color.FromArgb(20, 184, 166), Color.FromArgb(45, 212, 191), Color.FromArgb(94, 234, 212), Color.FromArgb(15, 118, 110), Color.FromArgb(17, 94, 89)
                }),
                new ThemeColorPaletteItem("Đơn sắc (Grayscale)", new Color[] {
                    Color.FromArgb(31, 41, 55), Color.FromArgb(75, 85, 99), Color.FromArgb(156, 163, 175), Color.FromArgb(209, 213, 219), Color.FromArgb(17, 24, 39), Color.FromArgb(243, 244, 246)
                }),
                new ThemeColorPaletteItem("Hồng ngọc (Rose Pink)", new Color[] {
                    Color.FromArgb(225, 29, 72), Color.FromArgb(244, 63, 94), Color.FromArgb(251, 113, 133), Color.FromArgb(253, 164, 175), Color.FromArgb(190, 18, 60), Color.FromArgb(136, 19, 55)
                })
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
                Text = "Bảng màu chủ đề (Theme Colors)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(10, 8),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblHeader);

            int itemH = 28;
            int startY = 32;

            for (int i = 0; i < _palettes.Count; i++)
            {
                var pal = _palettes[i];
                int py = startY + i * (itemH + 2);
                bool isSel = string.Equals(pal.Name, _activePaletteName, StringComparison.OrdinalIgnoreCase);

                Control row = new Control
                {
                    Location = new Point(8, py),
                    Size = new Size(226, itemH),
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

                    // 6 color chips
                    int swX = 6;
                    int swY = 6;
                    for (int c = 0; c < pal.Colors.Length; c++)
                    {
                        using (SolidBrush swB = new SolidBrush(pal.Colors[c]))
                        {
                            g.FillRectangle(swB, swX + c * 11, swY, 10, 16);
                        }
                    }

                    // Name
                    using (Font nf = new Font("Segoe UI", 8.5f, isSel ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush nb = new SolidBrush(isSel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(30, 41, 59)))
                    {
                        g.DrawString(pal.Name, nf, nb, 78, 6);
                    }
                };

                row.Click += (s, e) =>
                {
                    if (OnPaletteSelected != null) OnPaletteSelected(pal);
                    Close();
                };

                scrollPanel.Controls.Add(row);
            }

            // Bottom Customize Button
            int customY = startY + _palettes.Count * (itemH + 2) + 6;
            Button btnCustom = new Button
            {
                Text = "🎨  Tùy chỉnh màu sắc (Customize Colors)...",
                Location = new Point(8, customY),
                Size = new Size(226, 28),
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
                if (OnCustomizeColorsClicked != null) OnCustomizeColorsClicked();
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

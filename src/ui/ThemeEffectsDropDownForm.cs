using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ThemeEffectItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int EffectIndex { get; set; }

        public ThemeEffectItem(string name, string desc, int idx)
        {
            Name = name;
            Description = desc;
            EffectIndex = idx;
        }
    }

    public class ThemeEffectsDropDownForm : Form
    {
        public Action<ThemeEffectItem> OnEffectSelected;
        private List<ThemeEffectItem> _effects;
        private int _activeEffectIndex;

        public ThemeEffectsDropDownForm(Point location, int activeEffectIndex = 0)
        {
            _activeEffectIndex = activeEffectIndex;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(250, 310);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _effects = new List<ThemeEffectItem>
            {
                new ThemeEffectItem("Văn phòng (Office)", "Đường nét phẳng, đổ bóng nhẹ 2px", 0),
                new ThemeEffectItem("Mềm mại (Subtle Soft)", "Viền mờ êm dịu, bóng khuếch tán", 1),
                new ThemeEffectItem("Bóng nổi 3D (3D Elevation)", "Đổ bóng sâu 8px, nổi khối đa tầng", 2),
                new ThemeEffectItem("Kính mờ (Glassmorphism)", "Bán trong suốt mờ ảo, viền phản quang", 3),
                new ThemeEffectItem("Rực rỡ (Glow / Neon)", "Viền phát sáng hào quang năng động", 4),
                new ThemeEffectItem("Kim loại bóng (Glossy Metallic)", "Ánh kim chuyển sắc sáng cao cấp", 5)
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
                Text = "Hiệu ứng đối tượng (Theme Effects)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(10, 8),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblHeader);

            int itemH = 38;
            int startY = 32;

            for (int i = 0; i < _effects.Count; i++)
            {
                var eff = _effects[i];
                int py = startY + i * (itemH + 4);
                bool isSel = (eff.EffectIndex == _activeEffectIndex);

                Control row = new Control
                {
                    Location = new Point(8, py),
                    Size = new Size(216, itemH),
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

                    // Circle Preview
                    using (Pen iconPen = new Pen(isSel ? Color.FromArgb(37, 99, 235) : Color.FromArgb(71, 85, 105), 1.5f))
                    {
                        g.DrawEllipse(iconPen, 8, 10, 16, 16);
                        g.DrawEllipse(iconPen, 14, 10, 16, 16);
                    }

                    // Effect Name
                    using (Font nf = new Font("Segoe UI", 8.5f, isSel ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush nb = new SolidBrush(isSel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(30, 41, 59)))
                    {
                        g.DrawString(eff.Name, nf, nb, 38, 4);
                    }

                    // Description
                    using (Font df = new Font("Segoe UI", 7.0f, FontStyle.Regular))
                    using (SolidBrush db = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(eff.Description, df, db, 38, 20);
                    }
                };

                row.Click += (s, e) =>
                {
                    if (OnEffectSelected != null) OnEffectSelected(eff);
                    Close();
                };

                scrollPanel.Controls.Add(row);
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

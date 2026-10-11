using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ParagraphBordersDropDownForm : Form
    {
        public Action<int> OnBorderSelected;

        private class BorderMenuItem
        {
            public int Id;
            public string Text;
            public bool IsSeparator;
            public Rectangle Bounds;
        }

        private readonly BorderMenuItem[] items = new BorderMenuItem[]
        {
            new BorderMenuItem { Id = 0, Text = "Bottom Border (Viền dưới)" },
            new BorderMenuItem { Id = 1, Text = "Top Border (Viền trên)" },
            new BorderMenuItem { Id = 2, Text = "Left Border (Viền trái)" },
            new BorderMenuItem { Id = 3, Text = "Right Border (Viền phải)" },
            new BorderMenuItem { Id = 4, Text = "No Border (Không có viền)" },
            new BorderMenuItem { Id = 5, Text = "All Borders (Tất cả viền)" },
            new BorderMenuItem { Id = 6, Text = "Outside Borders (Viền bao ngoài)" },
            new BorderMenuItem { Id = 7, Text = "Inside Borders (Viền bên trong)" },
            new BorderMenuItem { Id = 8, Text = "Inside Horizontal Border (Viền ngang trong)" },
            new BorderMenuItem { Id = 9, Text = "Inside Vertical Border (Viền dọc trong)" },
            new BorderMenuItem { Id = 10, Text = "Diagonal Down Border (Viền chéo xuống)" },
            new BorderMenuItem { Id = 11, Text = "Diagonal Up Border (Viền chéo lên)" },
            new BorderMenuItem { Id = -1, IsSeparator = true },
            new BorderMenuItem { Id = 13, Text = "Horizontal Line (Đường kẻ ngang)" },
            new BorderMenuItem { Id = 14, Text = "Draw Table (Vẽ bảng)" },
            new BorderMenuItem { Id = 15, Text = "View Gridlines (Xem đường lưới)" },
            new BorderMenuItem { Id = 16, Text = "Borders and Shading... (Viền và Tô nền...)" }
        };

        private int hoveredIndex = -1;
        private readonly Font itemFont = new Font("Segoe UI", 9.0f);

        public ParagraphBordersDropDownForm(Point location, Action<int> onSelect) : this(onSelect)
        {
            Location = location;
        }

        public ParagraphBordersDropDownForm(Action<int> onSelect)
        {
            OnBorderSelected = onSelect;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            BackColor = Color.White;
            Width = 245;

            int totalH = 8;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].IsSeparator) totalH += 6;
                else totalH += 25;
            }
            totalH += 6;
            Height = totalH;

            Deactivate += (s, e) => Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = hoveredIndex;
            hoveredIndex = -1;
            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i].IsSeparator && items[i].Bounds.Contains(e.Location))
                {
                    hoveredIndex = i;
                    break;
                }
            }
            if (prev != hoveredIndex) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoveredIndex != -1)
            {
                hoveredIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left && hoveredIndex >= 0 && hoveredIndex < items.Length)
            {
                var item = items[hoveredIndex];
                if (!item.IsSeparator)
                {
                    Close();
                    if (OnBorderSelected != null) OnBorderSelected(item.Id);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background & Border
            using (Pen borderPen = new Pen(Color.FromArgb(180, 180, 180), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            int curY = 4;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.IsSeparator)
                {
                    item.Bounds = new Rectangle(4, curY + 2, Width - 8, 2);
                    using (Pen sepPen = new Pen(Color.FromArgb(225, 225, 225), 1))
                    {
                        g.DrawLine(sepPen, 6, curY + 2, Width - 6, curY + 2);
                    }
                    curY += 6;
                    continue;
                }

                item.Bounds = new Rectangle(2, curY, Width - 4, 24);

                if (i == hoveredIndex)
                {
                    using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(232, 240, 254)))
                    using (Pen hPen = new Pen(Color.FromArgb(160, 200, 245), 1))
                    {
                        g.FillRectangle(hBrush, item.Bounds);
                        g.DrawRectangle(hPen, item.Bounds);
                    }
                }

                // Draw Vector Icon for each Border option (16x16 at X: 8, Y: curY + 4)
                DrawBorderIcon(g, 8, curY + 4, item.Id);

                // Draw Text
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                {
                    g.DrawString(item.Text, itemFont, textBrush, 32, curY + 3);
                }

                curY += 25;
            }
        }

        private void DrawBorderIcon(Graphics g, int x, int y, int id)
        {
            // Outer bounding box / base grid (light dashed/thin lines)
            using (Pen lightPen = new Pen(Color.FromArgb(200, 200, 200), 1))
            using (Pen darkPen = new Pen(Color.FromArgb(20, 20, 20), 1.8f))
            {
                lightPen.DashStyle = DashStyle.Dot;
                g.DrawRectangle(lightPen, x, y, 16, 16);
                g.DrawLine(lightPen, x, y + 8, x + 16, y + 8);
                g.DrawLine(lightPen, x + 8, y, x + 8, y + 16);

                switch (id)
                {
                    case 0: // Bottom Border
                        g.DrawLine(darkPen, x, y + 16, x + 16, y + 16);
                        break;
                    case 1: // Top Border
                        g.DrawLine(darkPen, x, y, x + 16, y);
                        break;
                    case 2: // Left Border
                        g.DrawLine(darkPen, x, y, x, y + 16);
                        break;
                    case 3: // Right Border
                        g.DrawLine(darkPen, x + 16, y, x + 16, y + 16);
                        break;
                    case 4: // No Border
                        // Just dot grid
                        break;
                    case 5: // All Borders
                        g.DrawRectangle(darkPen, x, y, 16, 16);
                        g.DrawLine(darkPen, x, y + 8, x + 16, y + 8);
                        g.DrawLine(darkPen, x + 8, y, x + 8, y + 16);
                        break;
                    case 6: // Outside Borders (Box)
                        g.DrawRectangle(darkPen, x, y, 16, 16);
                        break;
                    case 7: // Inside Borders
                        g.DrawLine(darkPen, x, y + 8, x + 16, y + 8);
                        g.DrawLine(darkPen, x + 8, y, x + 8, y + 16);
                        break;
                    case 8: // Inside Horizontal Border
                        g.DrawLine(darkPen, x, y + 8, x + 16, y + 8);
                        break;
                    case 9: // Inside Vertical Border
                        g.DrawLine(darkPen, x + 8, y, x + 8, y + 16);
                        break;
                    case 10: // Diagonal Down Border
                        g.DrawRectangle(darkPen, x, y, 16, 16);
                        g.DrawLine(darkPen, x, y, x + 16, y + 16);
                        break;
                    case 11: // Diagonal Up Border
                        g.DrawRectangle(darkPen, x, y, 16, 16);
                        g.DrawLine(darkPen, x, y + 16, x + 16, y);
                        break;
                    case 13: // Horizontal Line
                        using (Pen hPen = new Pen(Color.FromArgb(50, 50, 50), 2.0f))
                        {
                            g.DrawLine(hPen, x, y + 8, x + 16, y + 8);
                        }
                        break;
                    case 14: // Draw Table (Pencil)
                        using (Pen pPen = new Pen(Color.FromArgb(0, 102, 204), 1.5f))
                        {
                            g.DrawLine(pPen, x + 3, y + 13, x + 13, y + 3);
                            g.DrawLine(pPen, x + 1, y + 15, x + 3, y + 13);
                        }
                        break;
                    case 15: // View Gridlines
                        using (Pen glPen = new Pen(Color.FromArgb(0, 120, 215), 1))
                        {
                            glPen.DashStyle = DashStyle.Dash;
                            g.DrawRectangle(glPen, x, y, 16, 16);
                            g.DrawLine(glPen, x, y + 8, x + 16, y + 8);
                            g.DrawLine(glPen, x + 8, y, x + 8, y + 16);
                        }
                        break;
                    case 16: // Borders and Shading
                        using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(200, 220, 255)))
                        {
                            g.FillRectangle(sBrush, x + 2, y + 2, 12, 12);
                        }
                        g.DrawRectangle(darkPen, x, y, 16, 16);
                        break;
                }
            }
        }
    }
}

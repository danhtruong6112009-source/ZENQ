using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class ShapesGalleryDropDownForm : Form
    {
        public event Action<ShapeType> OnShapeSelected;

        public ShapesGalleryDropDownForm(Point screenLocation)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(340, 520);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(6)
            };

            int curY = 6;

            // 1. Cây Cỏ & Tự Nhiên (Botanical & Nature)
            AddSection(scrollPanel, ref curY, "🌿 Cây Cỏ & Tự Nhiên (Botanical & Nature)", new ShapeType[] {
                ShapeType.Leaf, ShapeType.Clover, ShapeType.Flower, ShapeType.Sprout, ShapeType.Sun, ShapeType.Cloud, ShapeType.Moon
            });

            // 2. Hình Học Sang Trọng & Huy Hiệu (Luxury Badges & Jewels)
            AddSection(scrollPanel, ref curY, "💎 Hình Học Sang Trọng (Luxury Badges & Jewels)", new ShapeType[] {
                ShapeType.LuxuryHexagon, ShapeType.Hexagon, ShapeType.Gem, ShapeType.Shield, ShapeType.Banner, ShapeType.Starburst, ShapeType.Arch, ShapeType.Octagon, ShapeType.Pentagon, ShapeType.Heart
            });

            // 3. Hình Khối Cơ Bản (Basic Shapes)
            AddSection(scrollPanel, ref curY, "🔷 Hình Cơ Bản (Basic Shapes)", new ShapeType[] {
                ShapeType.TextBox, ShapeType.Rectangle, ShapeType.RoundedRectangle, ShapeType.Ellipse, ShapeType.Triangle, ShapeType.RightTriangle, ShapeType.Diamond, ShapeType.Parallelogram, ShapeType.Trapezoid, ShapeType.Cross, ShapeType.Donut, ShapeType.Cube, ShapeType.Can
            });

            // 4. Mũi Tên & Ký Hiệu (Block Arrows & Symbols)
            AddSection(scrollPanel, ref curY, "➡️ Mũi Tên & Ký Hiệu (Block Arrows & Symbols)", new ShapeType[] {
                ShapeType.Arrow, ShapeType.Chevron, ShapeType.BlockArrowRight, ShapeType.BlockArrowLeft, ShapeType.BlockArrowUp, ShapeType.BlockArrowDown, ShapeType.Lightning, ShapeType.Callout, ShapeType.Smiley
            });

            // 5. Đường Kẻ (Lines)
            AddSection(scrollPanel, ref curY, "➖ Đường Kẻ (Lines)", new ShapeType[] {
                ShapeType.Line
            });

            Controls.Add(scrollPanel);

            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 180, 180), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AddSection(Panel parent, ref int curY, string title, ShapeType[] shapes)
        {
            Label lblHeader = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 40, 80),
                Location = new Point(8, curY),
                AutoSize = true
            };
            parent.Controls.Add(lblHeader);
            curY += 22;

            int col = 0;
            int rowY = curY;
            int btnSize = 32;

            for (int i = 0; i < shapes.Length; i++)
            {
                ShapeType st = shapes[i];
                Button btn = new Button
                {
                    Location = new Point(8 + col * (btnSize + 4), rowY),
                    Size = new Size(btnSize, btnSize),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(248, 249, 250),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 230);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 235, 252);

                btn.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    DrawShapeIcon(e.Graphics, 5, 5, 20, 20, st, Color.FromArgb(40, 40, 40));
                };

                btn.Click += (s, e) =>
                {
                    if (OnShapeSelected != null) OnShapeSelected(st);
                    Close();
                };

                parent.Controls.Add(btn);

                col++;
                if (col >= 8)
                {
                    col = 0;
                    rowY += btnSize + 4;
                }
            }

            if (col > 0) rowY += btnSize + 4;
            curY = rowY + 8;
        }

        private void DrawShapeIcon(Graphics g, int x, int y, int w, int h, ShapeType type, Color col)
        {
            using (Pen p = new Pen(col, 1.3f))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(35, col)))
            {
                Rectangle r = new Rectangle(x, y, w, h);
                if (type == ShapeType.TextBox)
                {
                    g.DrawRectangle(p, r);
                    using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        g.DrawString("A", f, Brushes.Black, x + 4, y + 2);
                    }
                }
                else if (type == ShapeType.Line)
                {
                    g.DrawLine(p, x + 2, y + h - 2, x + w - 2, y + 2);
                }
                else
                {
                    using (GraphicsPath path = SlidesView.CreateShapePath(type, x, y, w, h))
                    {
                        g.FillPath(b, path);
                        g.DrawPath(p, path);
                    }
                }
            }
        }
    }
}

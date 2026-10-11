using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class SmartArtColorItem
    {
        public string Name { get; set; }
        public Color[] Colors { get; set; }
        public bool IsOutlineOnly { get; set; }
        public bool IsGradient { get; set; }
        public Rectangle Bounds { get; set; }
    }

    public class SmartArtColorGroup
    {
        public string Title { get; set; }
        public List<SmartArtColorItem> Items { get; set; }
        public SmartArtColorGroup(string title)
        {
            Title = title;
            Items = new List<SmartArtColorItem>();
        }
    }

    public class SmartArtColorGalleryPopup : Form
    {
        public Action<Color[], bool> OnPaletteSelected;

        private Panel scrollContainer;
        private DoubleBufferedCanvas canvas;
        private List<SmartArtColorGroup> groups = new List<SmartArtColorGroup>();
        private List<SmartArtColorItem> allItems = new List<SmartArtColorItem>();
        private SmartArtColorItem hoveredItem = null;

        private class DoubleBufferedCanvas : Panel
        {
            public DoubleBufferedCanvas()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }

        public SmartArtColorGalleryPopup()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(310, 420);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
            this.Padding = new Padding(1);

            BuildColorGroups();
            InitUI();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(170, 180, 195), 1.0f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Close();
        }

        private void BuildColorGroups()
        {
            // Group 1: Primary Theme Colors
            var gPrimary = new SmartArtColorGroup("Màu chủ đề chính (Primary Theme Colors)");
            gPrimary.Items.Add(new SmartArtColorItem
            {
                Name = "Theme Outline",
                Colors = new Color[] { Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154) },
                IsOutlineOnly = true
            });
            gPrimary.Items.Add(new SmartArtColorItem
            {
                Name = "Theme Light",
                Colors = new Color[] { Color.FromArgb(180, 205, 237), Color.FromArgb(180, 205, 237), Color.FromArgb(180, 205, 237), Color.FromArgb(180, 205, 237), Color.FromArgb(180, 205, 237) }
            });
            gPrimary.Items.Add(new SmartArtColorItem
            {
                Name = "Theme Solid",
                Colors = new Color[] { Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154) }
            });
            groups.Add(gPrimary);

            // Group 2: Colorful (Đa sắc màu)
            var gColorful = new SmartArtColorGroup("Đa sắc màu (Colorful)");
            gColorful.Items.Add(new SmartArtColorItem
            {
                Name = "Colorful Accent Colors",
                Colors = new Color[] { Color.FromArgb(237, 125, 49), Color.FromArgb(91, 155, 213), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0), Color.FromArgb(165, 165, 165) }
            });
            gColorful.Items.Add(new SmartArtColorItem
            {
                Name = "Colorful - Warm Accents",
                Colors = new Color[] { Color.FromArgb(192, 0, 0), Color.FromArgb(237, 125, 49), Color.FromArgb(255, 192, 0), Color.FromArgb(244, 177, 131), Color.FromArgb(197, 90, 17) }
            });
            gColorful.Items.Add(new SmartArtColorItem
            {
                Name = "Colorful - Cool Accents",
                Colors = new Color[] { Color.FromArgb(31, 78, 121), Color.FromArgb(91, 155, 213), Color.FromArgb(46, 117, 182), Color.FromArgb(157, 195, 230), Color.FromArgb(68, 114, 196) }
            });
            gColorful.Items.Add(new SmartArtColorItem
            {
                Name = "Colorful - Nature Accents",
                Colors = new Color[] { Color.FromArgb(56, 86, 35), Color.FromArgb(112, 173, 71), Color.FromArgb(84, 130, 53), Color.FromArgb(169, 209, 142), Color.FromArgb(197, 224, 180) }
            });
            gColorful.Items.Add(new SmartArtColorItem
            {
                Name = "Colorful - Violet Accents",
                Colors = new Color[] { Color.FromArgb(112, 48, 160), Color.FromArgb(166, 77, 121), Color.FromArgb(194, 123, 160), Color.FromArgb(142, 36, 170), Color.FromArgb(216, 27, 96) }
            });
            groups.Add(gColorful);

            // Group 3: Accent 1 (Office Blue)
            var gAcc1 = new SmartArtColorGroup("Màu nhấn 1 (Accent 1 - Xanh Lam)");
            gAcc1.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 1 - Outline",
                Colors = new Color[] { Color.FromArgb(91, 155, 213), Color.FromArgb(91, 155, 213), Color.FromArgb(91, 155, 213), Color.FromArgb(91, 155, 213), Color.FromArgb(91, 155, 213) },
                IsOutlineOnly = true
            });
            gAcc1.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 1 - Solid",
                Colors = new Color[] { Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154), Color.FromArgb(43, 87, 154) }
            });
            gAcc1.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 1 - Tint Varying",
                Colors = new Color[] { Color.FromArgb(43, 87, 154), Color.FromArgb(91, 155, 213), Color.FromArgb(157, 195, 230), Color.FromArgb(189, 215, 238), Color.FromArgb(222, 235, 247) }
            });
            gAcc1.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 1 - Dark Tint",
                Colors = new Color[] { Color.FromArgb(24, 48, 89), Color.FromArgb(31, 78, 121), Color.FromArgb(46, 117, 182), Color.FromArgb(91, 155, 213), Color.FromArgb(157, 195, 230) }
            });
            groups.Add(gAcc1);

            // Group 4: Accent 2 (Orange)
            var gAcc2 = new SmartArtColorGroup("Màu nhấn 2 (Accent 2 - Cam)");
            gAcc2.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 2 - Outline",
                Colors = new Color[] { Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49) },
                IsOutlineOnly = true
            });
            gAcc2.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 2 - Solid",
                Colors = new Color[] { Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49), Color.FromArgb(237, 125, 49) }
            });
            gAcc2.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 2 - Tint Varying",
                Colors = new Color[] { Color.FromArgb(197, 90, 17), Color.FromArgb(237, 125, 49), Color.FromArgb(244, 177, 131), Color.FromArgb(248, 203, 173), Color.FromArgb(251, 228, 213) }
            });
            groups.Add(gAcc2);

            // Group 5: Accent 3 (Monochrome Gray)
            var gAcc3 = new SmartArtColorGroup("Màu nhấn 3 (Accent 3 - Xám)");
            gAcc3.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 3 - Outline",
                Colors = new Color[] { Color.FromArgb(128, 128, 128), Color.FromArgb(128, 128, 128), Color.FromArgb(128, 128, 128), Color.FromArgb(128, 128, 128), Color.FromArgb(128, 128, 128) },
                IsOutlineOnly = true
            });
            gAcc3.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 3 - Solid",
                Colors = new Color[] { Color.FromArgb(89, 89, 89), Color.FromArgb(89, 89, 89), Color.FromArgb(89, 89, 89), Color.FromArgb(89, 89, 89), Color.FromArgb(89, 89, 89) }
            });
            gAcc3.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 3 - Tint Varying",
                Colors = new Color[] { Color.FromArgb(59, 59, 59), Color.FromArgb(89, 89, 89), Color.FromArgb(128, 128, 128), Color.FromArgb(175, 175, 175), Color.FromArgb(217, 217, 217) }
            });
            groups.Add(gAcc3);

            // Group 6: Accent 4 (Gold Yellow)
            var gAcc4 = new SmartArtColorGroup("Màu nhấn 4 (Accent 4 - Vàng Hoàng Yến)");
            gAcc4.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 4 - Solid",
                Colors = new Color[] { Color.FromArgb(255, 192, 0), Color.FromArgb(255, 192, 0), Color.FromArgb(255, 192, 0), Color.FromArgb(255, 192, 0), Color.FromArgb(255, 192, 0) }
            });
            gAcc4.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 4 - Tint Varying",
                Colors = new Color[] { Color.FromArgb(204, 153, 0), Color.FromArgb(255, 192, 0), Color.FromArgb(255, 217, 102), Color.FromArgb(255, 230, 153), Color.FromArgb(255, 242, 204) }
            });
            groups.Add(gAcc4);

            // Group 7: Accent 6 (Green)
            var gAcc6 = new SmartArtColorGroup("Màu nhấn 6 (Accent 6 - Xanh Lục)");
            gAcc6.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 6 - Solid",
                Colors = new Color[] { Color.FromArgb(112, 173, 71), Color.FromArgb(112, 173, 71), Color.FromArgb(112, 173, 71), Color.FromArgb(112, 173, 71), Color.FromArgb(112, 173, 71) }
            });
            gAcc6.Items.Add(new SmartArtColorItem
            {
                Name = "Accent 6 - Tint Varying",
                Colors = new Color[] { Color.FromArgb(56, 86, 35), Color.FromArgb(112, 173, 71), Color.FromArgb(169, 209, 142), Color.FromArgb(197, 224, 180), Color.FromArgb(226, 239, 218) }
            });
            groups.Add(gAcc6);

            foreach (var g in groups)
            {
                foreach (var it in g.Items)
                {
                    allItems.Add(it);
                }
            }
        }

        private void InitUI()
        {
            scrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            canvas = new DoubleBufferedCanvas
            {
                BackColor = Color.White,
                Location = new Point(0, 0),
                Width = 290
            };

            canvas.Paint += Canvas_Paint;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeave += Canvas_MouseLeave;
            canvas.MouseClick += Canvas_MouseClick;

            LayoutItems();

            scrollContainer.Controls.Add(canvas);
            this.Controls.Add(scrollContainer);
        }

        private void LayoutItems()
        {
            int y = 6;
            int itemW = 52;
            int itemH = 34;
            int gapX = 4;

            foreach (var g in groups)
            {
                y += 20; // Header space
                int x = 12;

                for (int i = 0; i < g.Items.Count; i++)
                {
                    if (x + itemW > 280)
                    {
                        x = 12;
                        y += itemH + 6;
                    }
                    g.Items[i].Bounds = new Rectangle(x, y, itemW, itemH);
                    x += itemW + gapX;
                }
                y += itemH + 10;
            }

            canvas.Height = y + 10;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (Font groupFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (Brush groupBrush = new SolidBrush(Color.FromArgb(80, 85, 95)))
            {
                foreach (var grp in groups)
                {
                    g.DrawString(grp.Title, groupFont, groupBrush, 10, grp.Items[0].Bounds.Y - 18);

                    foreach (var it in grp.Items)
                    {
                        bool isHover = (hoveredItem == it);
                        if (isHover)
                        {
                            using (SolidBrush hovB = new SolidBrush(Color.FromArgb(235, 243, 252)))
                            using (Pen hovP = new Pen(Color.FromArgb(43, 87, 154), 1.2f))
                            {
                                g.FillRectangle(hovB, it.Bounds);
                                g.DrawRectangle(hovP, it.Bounds);
                            }
                        }
                        else
                        {
                            using (Pen bP = new Pen(Color.FromArgb(225, 230, 238), 1f))
                            {
                                g.DrawRectangle(bP, it.Bounds);
                            }
                        }

                        // Draw miniature thumbnail: 5 connected nodes
                        DrawMiniDiagram(g, it.Bounds, it.Colors, it.IsOutlineOnly);
                    }
                }
            }
        }

        private void DrawMiniDiagram(Graphics g, Rectangle bounds, Color[] colors, bool isOutlineOnly)
        {
            int cx = bounds.X + bounds.Width / 2;
            int cy = bounds.Y + bounds.Height / 2;
            int r = 11;
            int nodeR = 3;

            // Draw center connecting ring
            using (Pen ringPen = new Pen(Color.FromArgb(190, 195, 205), 1.0f))
            {
                g.DrawEllipse(ringPen, cx - r, cy - r, r * 2, r * 2);
            }

            // Draw 5 nodes around circle
            int count = (colors != null && colors.Length > 0) ? colors.Length : 5;
            for (int i = 0; i < count; i++)
            {
                double angle = (i * 2.0 * Math.PI / count) - Math.PI / 2.0;
                int nx = cx + (int)(r * Math.Cos(angle));
                int ny = cy + (int)(r * Math.Sin(angle));
                Rectangle nRect = new Rectangle(nx - nodeR, ny - nodeR, nodeR * 2, nodeR * 2);

                Color c = colors[i % colors.Length];
                if (isOutlineOnly)
                {
                    g.FillRectangle(Brushes.White, nRect);
                    using (Pen p = new Pen(c, 1.2f))
                    {
                        g.DrawRectangle(p, nRect);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillRectangle(b, nRect);
                    }
                    using (Pen p = new Pen(ControlPaint.Dark(c, 0.15f), 0.8f))
                    {
                        g.DrawRectangle(p, nRect);
                    }
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            SmartArtColorItem found = null;
            foreach (var it in allItems)
            {
                if (it.Bounds.Contains(e.Location))
                {
                    found = it;
                    break;
                }
            }
            if (found != hoveredItem)
            {
                hoveredItem = found;
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseLeave(object sender, EventArgs e)
        {
            if (hoveredItem != null)
            {
                hoveredItem = null;
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (hoveredItem != null && OnPaletteSelected != null)
            {
                OnPaletteSelected(hoveredItem.Colors, hoveredItem.IsOutlineOnly);
                this.Close();
            }
        }
    }
}

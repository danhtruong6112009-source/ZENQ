using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ShapeGalleryItem
    {
        public string ShapeType { get; set; }
        public string DisplayName { get; set; }
        public Rectangle Bounds { get; set; }
    }

    public class ShapeDoubleBufferedPanel : Panel
    {
        public ShapeDoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }

    public class ShapeGalleryPopup : Form
    {
        public Action<string> OnShapeSelected;

        private Panel scrollContainer;
        private List<ShapeGalleryItem> shapeItems = new List<ShapeGalleryItem>();
        private ShapeGalleryItem hoveredItem = null;

        public ShapeGalleryPopup()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(270, 480);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            scrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            this.Controls.Add(scrollContainer);

            // Inner Double-Buffered Canvas to eliminate ALL flicker
            ShapeDoubleBufferedPanel canvas = new ShapeDoubleBufferedPanel
            {
                Location = new Point(0, 0),
                Size = new Size(250, 950),
                BackColor = Color.White
            };
            canvas.Paint += Canvas_Paint;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeave += (s, e) => { hoveredItem = null; canvas.Invalidate(); };
            canvas.MouseClick += Canvas_MouseClick;

            scrollContainer.Controls.Add(canvas);

            BuildGalleryLayout();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Close();
        }

        private void BuildGalleryLayout()
        {
            shapeItems.Clear();

            int startY = 10;
            int itemW = 26;
            int itemH = 24;
            int gap = 3;
            int cols = 8;
            int leftMargin = 12;

            // 1. Recently Used
            string[] recent = new string[] { "TextBox", "Line", "ArrowLine", "Rectangle", "Oval", "RoundedRectangle", "Triangle", "RightArrow", "DownArrow", "Cloud", "Star5" };
            startY = AddGroup("Hình dạng đã dùng gần đây", recent, startY, leftMargin, itemW, itemH, gap, cols);

            // 2. Lines
            string[] lines = new string[] { "Line", "ArrowLine", "DoubleArrowLine" };
            startY = AddGroup("Đường thẳng (Lines)", lines, startY, leftMargin, itemW, itemH, gap, cols);

            // 3. Rectangles
            string[] rects = new string[] { "Rectangle", "RoundedRectangle", "SingleCornerSnipped", "TopCornersSnipped" };
            startY = AddGroup("Hình chữ nhật (Rectangles)", rects, startY, leftMargin, itemW, itemH, gap, cols);

            // 4. Basic Shapes
            string[] basic = new string[] { "TextBox", "Oval", "Triangle", "RightTriangle", "Parallelogram", "Trapezoid", "Diamond", "Pentagon", "Hexagon", "Octagon", "Cross", "Cylinder", "Cube", "SmileFace", "Heart", "Lightning", "Sun", "Moon", "Cloud" };
            startY = AddGroup("Hình dạng cơ bản (Basic Shapes)", basic, startY, leftMargin, itemW, itemH, gap, cols);

            // 5. Block Arrows
            string[] arrows = new string[] { "RightArrow", "LeftArrow", "UpArrow", "DownArrow", "LeftRightArrow", "UpDownArrow" };
            startY = AddGroup("Mũi tên khối (Block Arrows)", arrows, startY, leftMargin, itemW, itemH, gap, cols);

            // 6. Equation Shapes
            string[] equations = new string[] { "Plus", "Minus", "Multiply", "Equal" };
            startY = AddGroup("Phương trình (Equation Shapes)", equations, startY, leftMargin, itemW, itemH, gap, cols);

            // 7. Flowchart
            string[] flowchart = new string[] { "Rectangle", "FlowchartDecision", "Parallelogram", "FlowchartPreparation", "FlowchartDocument", "Oval" };
            startY = AddGroup("Lưu trình (Flowchart)", flowchart, startY, leftMargin, itemW, itemH, gap, cols);

            // 8. Stars and Banners
            string[] stars = new string[] { "Star4", "Star5", "Star6", "Star8", "Star12", "CalloutRect" };
            startY = AddGroup("Ngôi sao & Chú thích (Stars & Callouts)", stars, startY, leftMargin, itemW, itemH, gap, cols);
        }

        private int AddGroup(string groupTitle, string[] types, int startY, int leftMargin, int itemW, int itemH, int gap, int cols)
        {
            // Title height is 22
            startY += 22;

            for (int i = 0; i < types.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                int ix = leftMargin + c * (itemW + gap);
                int iy = startY + r * (itemH + gap);

                shapeItems.Add(new ShapeGalleryItem
                {
                    ShapeType = types[i],
                    DisplayName = types[i],
                    Bounds = new Rectangle(ix, iy, itemW, itemH)
                });
            }

            int numRows = (int)Math.Ceiling(types.Length / (double)cols);
            return startY + numRows * (itemH + gap) + 12;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Outer Border
            using (Pen borderPen = new Pen(Color.FromArgb(200, 205, 215), 1.0f))
            {
                g.DrawRectangle(borderPen, 0, 0, ((Control)sender).Width - 1, ((Control)sender).Height - 1);
            }

            // Group Titles
            using (Font groupFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush groupBrush = new SolidBrush(Color.FromArgb(0, 51, 102)))
            {
                g.DrawString("Hình dạng đã dùng gần đây", groupFont, groupBrush, 10, 10);
                g.DrawString("Đường thẳng (Lines)", groupFont, groupBrush, 10, 95);
                g.DrawString("Hình chữ nhật (Rectangles)", groupFont, groupBrush, 10, 155);
                g.DrawString("Hình dạng cơ bản (Basic Shapes)", groupFont, groupBrush, 10, 215);
                g.DrawString("Mũi tên khối (Block Arrows)", groupFont, groupBrush, 10, 335);
                g.DrawString("Phương trình (Equation Shapes)", groupFont, groupBrush, 10, 395);
                g.DrawString("Lưu trình (Flowchart)", groupFont, groupBrush, 10, 455);
                g.DrawString("Ngôi sao & Chú thích (Stars & Callouts)", groupFont, groupBrush, 10, 515);
            }

            // Draw Separator Lines
            using (Pen sepPen = new Pen(Color.FromArgb(235, 238, 245), 1.0f))
            {
                g.DrawLine(sepPen, 10, 90, 240, 90);
                g.DrawLine(sepPen, 10, 150, 240, 150);
                g.DrawLine(sepPen, 10, 210, 240, 210);
                g.DrawLine(sepPen, 10, 330, 240, 330);
                g.DrawLine(sepPen, 10, 390, 240, 390);
                g.DrawLine(sepPen, 10, 450, 240, 450);
                g.DrawLine(sepPen, 10, 510, 240, 510);
            }

            // Draw Items
            using (Pen itemPen = new Pen(Color.FromArgb(45, 55, 72), 1.2f))
            {
                foreach (var item in shapeItems)
                {
                    bool isHover = (hoveredItem == item);
                    if (isHover)
                    {
                        using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(215, 235, 255)))
                        {
                            g.FillRectangle(hBrush, item.Bounds);
                        }
                        g.DrawRectangle(new Pen(Color.FromArgb(0, 120, 215), 1.2f), item.Bounds);
                    }

                    Rectangle iconBounds = new Rectangle(item.Bounds.X + 4, item.Bounds.Y + 3, item.Bounds.Width - 8, item.Bounds.Height - 6);

                    if (item.ShapeType == "TextBox")
                    {
                        g.DrawRectangle(itemPen, iconBounds);
                        using (Font tf = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                        {
                            g.DrawString("A", tf, Brushes.DarkBlue, iconBounds.X + 2, iconBounds.Y - 1);
                        }
                    }
                    else if (item.ShapeType == "Line")
                    {
                        g.DrawLine(itemPen, iconBounds.Left, iconBounds.Bottom, iconBounds.Right, iconBounds.Top);
                    }
                    else if (item.ShapeType == "ArrowLine")
                    {
                        using (Pen aPen = new Pen(Color.FromArgb(45, 55, 72), 1.4f))
                        {
                            aPen.CustomEndCap = new AdjustableArrowCap(3, 3);
                            g.DrawLine(aPen, iconBounds.Left, iconBounds.Bottom, iconBounds.Right, iconBounds.Top);
                        }
                    }
                    else if (item.ShapeType == "DoubleArrowLine")
                    {
                        using (Pen aPen = new Pen(Color.FromArgb(45, 55, 72), 1.4f))
                        {
                            aPen.CustomStartCap = new AdjustableArrowCap(3, 3);
                            aPen.CustomEndCap = new AdjustableArrowCap(3, 3);
                            g.DrawLine(aPen, iconBounds.Left, iconBounds.Bottom, iconBounds.Right, iconBounds.Top);
                        }
                    }
                    else
                    {
                        using (GraphicsPath path = ShapeRenderer.CreateShapePath(item.ShapeType, iconBounds))
                        {
                            g.DrawPath(itemPen, path);
                        }
                    }
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            ShapeGalleryItem hit = null;
            foreach (var item in shapeItems)
            {
                if (item.Bounds.Contains(e.Location))
                {
                    hit = item;
                    break;
                }
            }

            if (hoveredItem != hit)
            {
                hoveredItem = hit;
                ((Control)sender).Invalidate();
            }
        }

        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && hoveredItem != null)
            {
                string chosenType = hoveredItem.ShapeType;
                this.Close();
                if (OnShapeSelected != null)
                {
                    OnShapeSelected(chosenType);
                }
            }
        }
    }
}

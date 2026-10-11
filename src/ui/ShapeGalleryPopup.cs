using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ShapeGalleryItem
    {
        public string ShapeType { get; set; }
        public string DisplayName { get; set; }
        public Rectangle Bounds { get; set; }
    }

    public class ShapeGalleryGroup
    {
        public string Title { get; set; }
        public int Y { get; set; }
        public List<ShapeGalleryItem> Items { get; set; }
        public ShapeGalleryGroup()
        {
            Items = new List<ShapeGalleryItem>();
        }
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
        private ShapeDoubleBufferedPanel canvas;
        private List<ShapeGalleryGroup> groups = new List<ShapeGalleryGroup>();
        private List<ShapeGalleryItem> shapeItems = new List<ShapeGalleryItem>();
        private ShapeGalleryItem hoveredItem = null;
        private ToolTip tooltip;

        public ShapeGalleryPopup()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(304, 520);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
            this.Padding = new Padding(1);

            tooltip = new ToolTip
            {
                InitialDelay = 200,
                ReshowDelay = 100,
                AutoPopDelay = 4000
            };

            scrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            this.Controls.Add(scrollContainer);

            canvas = new ShapeDoubleBufferedPanel
            {
                Location = new Point(0, 0),
                Size = new Size(280, 1850),
                BackColor = Color.White
            };
            canvas.Paint += Canvas_Paint;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeave += (s, e) => {
                hoveredItem = null;
                tooltip.SetToolTip(canvas, null);
                canvas.Invalidate();
            };
            canvas.MouseClick += Canvas_MouseClick;

            scrollContainer.Controls.Add(canvas);

            BuildGalleryLayout();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Screen scr = Screen.FromControl(this);
            Rectangle work = scr.WorkingArea;
            if (this.Bottom > work.Bottom)
            {
                this.Top = Math.Max(work.Top, work.Bottom - this.Height - 8);
            }
            if (this.Right > work.Right)
            {
                this.Left = Math.Max(work.Left, work.Right - this.Width - 8);
            }
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

        private void BuildGalleryLayout()
        {
            groups.Clear();
            shapeItems.Clear();

            int startY = 10;
            int itemW = 26;
            int itemH = 24;
            int gap = 3;
            int cols = 8;
            int leftMargin = 14;

            // 1. Hình dạng đã dùng gần đây (Recently Used Shapes)
            string[] recent = new string[] {
                "TextBox", "Line", "ArrowLine", "Rectangle", "Oval", "RoundedRectangle", "Triangle", "RightArrow",
                "DownArrow", "Cross", "Star5", "CalloutRect"
            };
            startY = AddGroupSection("Hình dạng đã dùng gần đây", recent, startY, leftMargin, itemW, itemH, gap, cols);

            // 2. Đường thẳng (Lines)
            string[] lines = new string[] {
                "Line", "ArrowLine", "DoubleArrowLine", "ElbowConnector", "ElbowArrowConnector", "CurvedConnector"
            };
            startY = AddGroupSection("Đường thẳng (Lines)", lines, startY, leftMargin, itemW, itemH, gap, cols);

            // 3. Hình chữ nhật (Rectangles)
            string[] rects = new string[] {
                "Rectangle", "RoundedRectangle", "SingleCornerSnipped", "TopCornersSnipped", "SnippedAndRounded",
                "RoundSingleCorner", "RoundSameSideCorner", "RoundDiagonalCorner", "SnipSameSideCorner"
            };
            startY = AddGroupSection("Hình chữ nhật (Rectangles)", rects, startY, leftMargin, itemW, itemH, gap, cols);

            // 4. Hình dạng cơ bản (Basic Shapes)
            string[] basic = new string[] {
                "TextBox", "Oval", "Triangle", "RightTriangle", "Parallelogram", "Trapezoid", "Diamond", "Pentagon",
                "Hexagon", "Octagon", "Decagon", "Cross", "Cylinder", "Cube", "Bevel", "Donut",
                "SmileFace", "Heart", "Lightning", "Sun", "Moon", "Cloud", "Teardrop", "Arc"
            };
            startY = AddGroupSection("Hình dạng cơ bản (Basic Shapes)", basic, startY, leftMargin, itemW, itemH, gap, cols);

            // 5. Mũi tên khối (Block Arrows)
            string[] arrows = new string[] {
                "RightArrow", "LeftArrow", "UpArrow", "DownArrow", "LeftRightArrow", "UpDownArrow", "QuadArrow", "BentArrow",
                "UTurnArrow", "Chevron"
            };
            startY = AddGroupSection("Mũi tên khối (Block Arrows)", arrows, startY, leftMargin, itemW, itemH, gap, cols);

            // 6. Phương trình (Equation Shapes)
            string[] equations = new string[] {
                "Plus", "Minus", "Multiply", "Divide", "Equal", "NotEqual"
            };
            startY = AddGroupSection("Phương trình (Equation Shapes)", equations, startY, leftMargin, itemW, itemH, gap, cols);

            // 7. Lưu trình (Flowchart)
            string[] flowchart = new string[] {
                "Rectangle", "FlowchartAlternateProcess", "FlowchartDecision", "FlowchartData", "FlowchartPredefinedProcess", "FlowchartInternalStorage",
                "FlowchartDocument", "FlowchartMultidocument", "FlowchartTerminator", "FlowchartPreparation", "FlowchartManualInput", "FlowchartManualOperation",
                "FlowchartConnector", "FlowchartOffpageConnector", "FlowchartCard", "FlowchartPunchedTape"
            };
            startY = AddGroupSection("Lưu trình (Flowchart)", flowchart, startY, leftMargin, itemW, itemH, gap, cols);

            // 8. Sao và Biểu ngữ (Stars and Banners)
            string[] stars = new string[] {
                "Explosion1", "Explosion2", "Star4", "Star5", "Star6", "Star8", "Star10", "Star12",
                "Star16", "Star24", "Star32", "HorizontalScroll", "VerticalScroll"
            };
            startY = AddGroupSection("Sao và Biểu ngữ (Stars and Banners)", stars, startY, leftMargin, itemW, itemH, gap, cols);

            // 9. Chú thích (Callouts)
            string[] callouts = new string[] {
                "CalloutRect", "CalloutRoundedRect", "CalloutOval", "CalloutCloud"
            };
            startY = AddGroupSection("Chú thích (Callouts)", callouts, startY, leftMargin, itemW, itemH, gap, cols);

            // 10. Nút tác vụ (Action Buttons)
            string[] buttons = new string[] {
                "ActionButtonBack", "ActionButtonForward", "ActionButtonBeginning", "ActionButtonEnd",
                "ActionButtonHome", "ActionButtonInformation", "ActionButtonReturn", "ActionButtonMovie",
                "ActionButtonDocument", "ActionButtonSound", "ActionButtonHelp", "ActionButtonBlank"
            };
            startY = AddGroupSection("Nút tác vụ (Action Buttons)", buttons, startY, leftMargin, itemW, itemH, gap, cols);

            canvas.Height = startY + 20;
        }

        private int AddGroupSection(string groupTitle, string[] types, int startY, int leftMargin, int itemW, int itemH, int gap, int cols)
        {
            ShapeGalleryGroup grp = new ShapeGalleryGroup();
            grp.Title = groupTitle;
            grp.Y = startY;

            int itemsStartY = startY + 22;

            for (int i = 0; i < types.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                int ix = leftMargin + c * (itemW + gap);
                int iy = itemsStartY + r * (itemH + gap);

                ShapeGalleryItem item = new ShapeGalleryItem
                {
                    ShapeType = types[i],
                    DisplayName = GetFriendlyName(types[i]),
                    Bounds = new Rectangle(ix, iy, itemW, itemH)
                };
                grp.Items.Add(item);
                shapeItems.Add(item);
            }

            groups.Add(grp);

            int numRows = (int)Math.Ceiling(types.Length / (double)cols);
            return itemsStartY + numRows * (itemH + gap) + 14;
        }

        public void RenderGallery(Graphics g, int width, int height)
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (Font groupFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush groupBrush = new SolidBrush(Color.FromArgb(0, 51, 102)))
            using (Pen sepPen = new Pen(Color.FromArgb(235, 238, 245), 1.0f))
            using (Pen itemPen = new Pen(Color.FromArgb(45, 55, 72), 1.2f))
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    ShapeGalleryGroup grp = groups[i];
                    if (grp.Y > height) break;

                    // Separator line before group (except first)
                    if (i > 0)
                    {
                        g.DrawLine(sepPen, 10, grp.Y - 6, width - 15, grp.Y - 6);
                    }

                    // Group Title
                    g.DrawString(grp.Title, groupFont, groupBrush, 12, grp.Y);
                }

                // Draw Items
                for (int idx = 0; idx < shapeItems.Count; idx++)
                {
                    ShapeGalleryItem item = shapeItems[idx];
                    if (item.Bounds.Bottom > height) continue;

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
                        using (Pen aPen = new Pen(Color.FromArgb(45, 55, 72), 1.3f))
                        {
                            aPen.CustomEndCap = new AdjustableArrowCap(3, 3);
                            g.DrawLine(aPen, iconBounds.Left, iconBounds.Bottom, iconBounds.Right, iconBounds.Top);
                        }
                    }
                    else if (item.ShapeType == "DoubleArrowLine")
                    {
                        using (Pen aPen = new Pen(Color.FromArgb(45, 55, 72), 1.3f))
                        {
                            aPen.CustomStartCap = new AdjustableArrowCap(3, 3);
                            aPen.CustomEndCap = new AdjustableArrowCap(3, 3);
                            g.DrawLine(aPen, iconBounds.Left, iconBounds.Bottom, iconBounds.Right, iconBounds.Top);
                        }
                    }
                    else if (item.ShapeType.StartsWith("ActionButton"))
                    {
                        g.DrawRectangle(itemPen, iconBounds);
                        // Draw symbol inside button
                        using (Brush b = new SolidBrush(Color.FromArgb(45, 55, 72)))
                        {
                            float cx = iconBounds.X + iconBounds.Width / 2f;
                            float cy = iconBounds.Y + iconBounds.Height / 2f;
                            if (item.ShapeType == "ActionButtonBack")
                            {
                                PointF[] pts = new PointF[] { new PointF(cx + 4, cy - 4), new PointF(cx - 4, cy), new PointF(cx + 4, cy + 4) };
                                g.FillPolygon(b, pts);
                            }
                            else if (item.ShapeType == "ActionButtonForward")
                            {
                                PointF[] pts = new PointF[] { new PointF(cx - 4, cy - 4), new PointF(cx + 4, cy), new PointF(cx - 4, cy + 4) };
                                g.FillPolygon(b, pts);
                            }
                            else if (item.ShapeType == "ActionButtonHome")
                            {
                                PointF[] pts = new PointF[] { new PointF(cx, cy - 5), new PointF(cx + 5, cy), new PointF(cx + 3, cy), new PointF(cx + 3, cy + 5), new PointF(cx - 3, cy + 5), new PointF(cx - 3, cy), new PointF(cx - 5, cy) };
                                g.FillPolygon(b, pts);
                            }
                            else
                            {
                                g.FillEllipse(b, cx - 3, cy - 3, 6, 6);
                            }
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

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            RenderGallery(e.Graphics, ((Control)sender).Width, ((Control)sender).Height);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            ShapeGalleryItem hit = null;
            for (int i = 0; i < shapeItems.Count; i++)
            {
                if (shapeItems[i].Bounds.Contains(e.Location))
                {
                    hit = shapeItems[i];
                    break;
                }
            }

            if (hoveredItem != hit)
            {
                hoveredItem = hit;
                if (hoveredItem != null)
                {
                    tooltip.SetToolTip(canvas, hoveredItem.DisplayName);
                }
                else
                {
                    tooltip.SetToolTip(canvas, null);
                }
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

        private static string GetFriendlyName(string type)
        {
            switch (type)
            {
                case "TextBox": return "Hộp văn bản (Text Box)";
                case "Line": return "Đường thẳng (Line)";
                case "ArrowLine": return "Đường mũi tên (Arrow Line)";
                case "DoubleArrowLine": return "Đường hai đầu mũi tên (Double Arrow)";
                case "ElbowConnector": return "Đường nối khuỷu (Elbow Connector)";
                case "ElbowArrowConnector": return "Đường nối khuỷu mũi tên (Elbow Arrow Connector)";
                case "CurvedConnector": return "Đường nối cong (Curved Connector)";
                case "Rectangle": return "Hình chữ nhật (Rectangle)";
                case "RoundedRectangle": return "Hình chữ nhật bo góc (Rounded Rectangle)";
                case "SingleCornerSnipped": return "Cắt một góc (Single Corner Snipped)";
                case "TopCornersSnipped": return "Cắt hai góc trên (Top Corners Snipped)";
                case "SnippedAndRounded": return "Cắt và bo tròn góc (Snipped and Rounded)";
                case "RoundSingleCorner": return "Bo tròn một góc (Round Single Corner)";
                case "RoundSameSideCorner": return "Bo tròn hai góc cùng phía (Round Same Side Corner)";
                case "RoundDiagonalCorner": return "Bo tròn góc đối diện (Round Diagonal Corner)";
                case "SnipSameSideCorner": return "Cắt hai góc cùng phía (Snip Same Side Corner)";
                case "Oval": return "Hình oval / Tròn (Oval)";
                case "Triangle": return "Hình tam giác cân (Isosceles Triangle)";
                case "RightTriangle": return "Hình tam giác vuông (Right Triangle)";
                case "Parallelogram": return "Hình bình hành (Parallelogram)";
                case "Trapezoid": return "Hình thang (Trapezoid)";
                case "Diamond": return "Hình thoi (Diamond)";
                case "Pentagon": return "Hình ngũ giác đều (Regular Pentagon)";
                case "Hexagon": return "Hình lục giác (Hexagon)";
                case "Octagon": return "Hình bát giác (Octagon)";
                case "Decagon": return "Hình thập giác (Decagon)";
                case "Cross": return "Dấu chữ thập (Cross)";
                case "Cylinder": return "Hình trụ (Cylinder)";
                case "Cube": return "Khối lập phương 3D (Cube)";
                case "Bevel": return "Khung vát góc (Bevel)";
                case "Donut": return "Vành khuyên (Donut)";
                case "SmileFace": return "Mặt cười (Smiley Face)";
                case "Heart": return "Hình trái tim (Heart)";
                case "Lightning": return "Tia chớp (Lightning Bolt)";
                case "Sun": return "Mặt trời (Sun)";
                case "Moon": return "Mặt trăng (Moon)";
                case "Cloud": return "Đám mây (Cloud)";
                case "Teardrop": return "Giọt nước (Teardrop)";
                case "Arc": return "Cung tròn (Arc)";
                case "RightArrow": return "Mũi tên sang phải (Right Arrow)";
                case "LeftArrow": return "Mũi tên sang trái (Left Arrow)";
                case "UpArrow": return "Mũi tên lên trên (Up Arrow)";
                case "DownArrow": return "Mũi tên xuống dưới (Down Arrow)";
                case "LeftRightArrow": return "Mũi tên hai chiều trái-phải (Left-Right Arrow)";
                case "UpDownArrow": return "Mũi tên hai chiều lên-xuống (Up-Down Arrow)";
                case "QuadArrow": return "Mũi tên bốn hướng (Quad Arrow)";
                case "BentArrow": return "Mũi tên gập (Bent Arrow)";
                case "UTurnArrow": return "Mũi tên chữ U (U-Turn Arrow)";
                case "Chevron": return "Mũi tên Chevron (Chevron Arrow)";
                case "Plus": return "Dấu cộng (Plus)";
                case "Minus": return "Dấu trừ (Minus)";
                case "Multiply": return "Dấu nhân (Multiply)";
                case "Divide": return "Dấu chia (Divide)";
                case "Equal": return "Dấu bằng (Equal)";
                case "NotEqual": return "Dấu khác (Not Equal)";
                case "FlowchartProcess": return "Lưu trình: Tiến trình (Process)";
                case "FlowchartAlternateProcess": return "Lưu trình: Tiến trình thay thế (Alternate Process)";
                case "FlowchartDecision": return "Lưu trình: Quyết định (Decision)";
                case "FlowchartData": return "Lưu trình: Dữ liệu (Data)";
                case "FlowchartPredefinedProcess": return "Lưu trình: Tiến trình định sẵn (Predefined Process)";
                case "FlowchartInternalStorage": return "Lưu trình: Lưu trữ nội bộ (Internal Storage)";
                case "FlowchartDocument": return "Lưu trình: Tài liệu (Document)";
                case "FlowchartMultidocument": return "Lưu trình: Đa tài liệu (Multidocument)";
                case "FlowchartTerminator": return "Lưu trình: Điểm đầu/cuối (Terminator)";
                case "FlowchartPreparation": return "Lưu trình: Chuẩn bị (Preparation)";
                case "FlowchartManualInput": return "Lưu trình: Nhập thủ công (Manual Input)";
                case "FlowchartManualOperation": return "Lưu trình: Thao tác thủ công (Manual Operation)";
                case "FlowchartConnector": return "Lưu trình: Điểm nối (Connector)";
                case "FlowchartOffpageConnector": return "Lưu trình: Nối ngoài trang (Offpage Connector)";
                case "FlowchartCard": return "Lưu trình: Thẻ đục lỗ (Card)";
                case "FlowchartPunchedTape": return "Lưu trình: Băng đục lỗ (Punched Tape)";
                case "Explosion1": return "Nổ 1 (Explosion 1)";
                case "Explosion2": return "Nổ 2 (Explosion 2)";
                case "Star4": return "Sao 4 cánh (4-Point Star)";
                case "Star5": return "Sao 5 cánh (5-Point Star)";
                case "Star6": return "Sao 6 cánh (6-Point Star)";
                case "Star8": return "Sao 8 cánh (8-Point Star)";
                case "Star10": return "Sao 10 cánh (10-Point Star)";
                case "Star12": return "Sao 12 cánh (12-Point Star)";
                case "Star16": return "Sao 16 cánh (16-Point Star)";
                case "Star24": return "Sao 24 cánh (24-Point Star)";
                case "Star32": return "Sao 32 cánh (32-Point Star)";
                case "HorizontalScroll": return "Cuộn giấy ngang (Horizontal Scroll)";
                case "VerticalScroll": return "Cuộn giấy dọc (Vertical Scroll)";
                case "CalloutRect": return "Chú thích chữ nhật (Rectangular Callout)";
                case "CalloutRoundedRect": return "Chú thích bo góc (Rounded Rectangular Callout)";
                case "CalloutOval": return "Chú thích oval (Oval Callout)";
                case "CalloutCloud": return "Chú thích đám mây (Cloud Callout)";
                case "ActionButtonBack": return "Nút tác vụ: Quay lại (Action Button: Back)";
                case "ActionButtonForward": return "Nút tác vụ: Tiến tới (Action Button: Forward)";
                case "ActionButtonBeginning": return "Nút tác vụ: Về đầu (Action Button: Beginning)";
                case "ActionButtonEnd": return "Nút tác vụ: Đến cuối (Action Button: End)";
                case "ActionButtonHome": return "Nút tác vụ: Trang chủ (Action Button: Home)";
                case "ActionButtonInformation": return "Nút tác vụ: Thông tin (Action Button: Information)";
                case "ActionButtonReturn": return "Nút tác vụ: Trở lại (Action Button: Return)";
                case "ActionButtonMovie": return "Nút tác vụ: Phim (Action Button: Movie)";
                case "ActionButtonDocument": return "Nút tác vụ: Tài liệu (Action Button: Document)";
                case "ActionButtonSound": return "Nút tác vụ: Âm thanh (Action Button: Sound)";
                case "ActionButtonHelp": return "Nút tác vụ: Trợ giúp (Action Button: Help)";
                case "ActionButtonBlank": return "Nút tác vụ: Trống (Action Button: Blank)";
                default: return type;
            }
        }
    }
}

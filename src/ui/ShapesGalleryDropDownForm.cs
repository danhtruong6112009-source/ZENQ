using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class ShapeItemDefinition
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public ShapeType FallbackType { get; set; }

        public ShapeItemDefinition(string name, string displayName, ShapeType fallbackType)
        {
            Name = name;
            DisplayName = displayName;
            FallbackType = fallbackType;
        }
    }

    public class ShapesGalleryDropDownForm : Form
    {
        public event Action<ShapeType> OnShapeSelected;
        public event Action<string> OnShapeNameSelected;

        private ToolTip tooltip;

        public ShapesGalleryDropDownForm(Point screenLocation) : this(screenLocation, null)
        {
        }

        public ShapesGalleryDropDownForm(Point screenLocation, Action<string> onStringSelected)
        {
            if (onStringSelected != null)
            {
                OnShapeNameSelected += onStringSelected;
            }

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(360, 560);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            tooltip = new ToolTip
            {
                InitialDelay = 300,
                ReshowDelay = 150,
                AutoPopDelay = 3000
            };

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(6)
            };

            int curY = 6;

            // 1. Đã dùng gần đây (Recently Used Shapes)
            AddSection(scrollPanel, ref curY, "Đã dùng gần đây (Recently Used Shapes)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("TextBox", "Hộp văn bản (Text Box)", ShapeType.TextBox),
                new ShapeItemDefinition("Line", "Đường thẳng (Line)", ShapeType.Line),
                new ShapeItemDefinition("ArrowLine", "Mũi tên (Arrow)", ShapeType.Arrow),
                new ShapeItemDefinition("Rectangle", "Hình chữ nhật (Rectangle)", ShapeType.Rectangle),
                new ShapeItemDefinition("RoundedRectangle", "Hình chữ nhật bo tròn (Rounded Rectangle)", ShapeType.RoundedRectangle),
                new ShapeItemDefinition("Oval", "Hình elip / tròn (Oval)", ShapeType.Ellipse),
                new ShapeItemDefinition("Triangle", "Tam giác cân (Isosceles Triangle)", ShapeType.Triangle),
                new ShapeItemDefinition("RightArrow", "Mũi tên phải (Right Arrow)", ShapeType.BlockArrowRight),
                new ShapeItemDefinition("Star5", "Sao 5 cánh (5-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Cloud", "Đám mây (Cloud)", ShapeType.Cloud)
            });

            // 2. Đường thẳng (Lines)
            AddSection(scrollPanel, ref curY, "Đường thẳng (Lines)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("Line", "Đường thẳng (Line)", ShapeType.Line),
                new ShapeItemDefinition("ArrowLine", "Đường mũi tên (Arrow)", ShapeType.Arrow),
                new ShapeItemDefinition("DoubleArrowLine", "Mũi tên hai chiều (Double Arrow)", ShapeType.Arrow),
                new ShapeItemDefinition("ElbowConnector", "Đầu nối góc (Elbow Connector)", ShapeType.Line),
                new ShapeItemDefinition("ElbowArrowConnector", "Đầu nối góc mũi tên (Elbow Arrow)", ShapeType.Arrow),
                new ShapeItemDefinition("CurvedConnector", "Đầu nối cong (Curved Connector)", ShapeType.Line)
            });

            // 3. Hình chữ nhật (Rectangles)
            AddSection(scrollPanel, ref curY, "Hình chữ nhật (Rectangles)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("Rectangle", "Hình chữ nhật (Rectangle)", ShapeType.Rectangle),
                new ShapeItemDefinition("RoundedRectangle", "Hình chữ nhật bo tròn (Rounded Rectangle)", ShapeType.RoundedRectangle),
                new ShapeItemDefinition("SingleCornerSnipped", "Cắt 1 góc (Single Corner Snipped)", ShapeType.Rectangle),
                new ShapeItemDefinition("TopCornersSnipped", "Cắt 2 góc trên (Top Corners Snipped)", ShapeType.Rectangle),
                new ShapeItemDefinition("SnippedAndRounded", "Cắt và bo tròn (Snipped and Rounded)", ShapeType.RoundedRectangle),
                new ShapeItemDefinition("RoundSingleCorner", "Bo tròn 1 góc (Round Single Corner)", ShapeType.RoundedRectangle),
                new ShapeItemDefinition("RoundSameSideCorner", "Bo tròn 2 góc cùng phía (Round Same Side)", ShapeType.RoundedRectangle),
                new ShapeItemDefinition("RoundDiagonalCorner", "Bo tròn 2 góc đối (Round Diagonal)", ShapeType.RoundedRectangle)
            });

            // 4. Hình dạng cơ bản (Basic Shapes)
            AddSection(scrollPanel, ref curY, "Hình dạng cơ bản (Basic Shapes)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("TextBox", "Hộp văn bản (Text Box)", ShapeType.TextBox),
                new ShapeItemDefinition("Oval", "Hình tròn / Elip (Oval)", ShapeType.Ellipse),
                new ShapeItemDefinition("Triangle", "Tam giác cân (Triangle)", ShapeType.Triangle),
                new ShapeItemDefinition("RightTriangle", "Tam giác vuông (Right Triangle)", ShapeType.RightTriangle),
                new ShapeItemDefinition("Parallelogram", "Hình bình hành (Parallelogram)", ShapeType.Parallelogram),
                new ShapeItemDefinition("Trapezoid", "Hình thang (Trapezoid)", ShapeType.Trapezoid),
                new ShapeItemDefinition("Diamond", "Hình thoi (Diamond)", ShapeType.Diamond),
                new ShapeItemDefinition("Pentagon", "Ngũ giác (Pentagon)", ShapeType.Pentagon),
                new ShapeItemDefinition("Hexagon", "Lục giác (Hexagon)", ShapeType.Hexagon),
                new ShapeItemDefinition("Octagon", "Bát giác (Octagon)", ShapeType.Octagon),
                new ShapeItemDefinition("Decagon", "Thập giác (Decagon)", ShapeType.Octagon),
                new ShapeItemDefinition("Cross", "Dấu thập (Cross)", ShapeType.Cross),
                new ShapeItemDefinition("Cylinder", "Hình trụ (Cylinder)", ShapeType.Can),
                new ShapeItemDefinition("Cube", "Hình lập phương (Cube)", ShapeType.Cube),
                new ShapeItemDefinition("Donut", "Hình vành khăn (Donut)", ShapeType.Donut),
                new ShapeItemDefinition("Heart", "Hình trái tim (Heart)", ShapeType.Heart),
                new ShapeItemDefinition("Lightning", "Tia sét (Lightning)", ShapeType.Lightning),
                new ShapeItemDefinition("Arc", "Cung tròn (Arc)", ShapeType.Ellipse),
                new ShapeItemDefinition("Moon", "Mặt trăng (Moon)", ShapeType.Moon),
                new ShapeItemDefinition("Sun", "Mặt trời (Sun)", ShapeType.Sun),
                new ShapeItemDefinition("Cloud", "Đám mây (Cloud)", ShapeType.Cloud),
                new ShapeItemDefinition("Teardrop", "Hình giọt nước (Teardrop)", ShapeType.Ellipse),
                new ShapeItemDefinition("SmileFace", "Mặt cười (Smiley)", ShapeType.Smiley)
            });

            // 5. Mũi tên khối (Block Arrows)
            AddSection(scrollPanel, ref curY, "Mũi tên khối (Block Arrows)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("RightArrow", "Mũi tên phải (Right Arrow)", ShapeType.BlockArrowRight),
                new ShapeItemDefinition("LeftArrow", "Mũi tên trái (Left Arrow)", ShapeType.BlockArrowLeft),
                new ShapeItemDefinition("UpArrow", "Mũi tên lên (Up Arrow)", ShapeType.BlockArrowUp),
                new ShapeItemDefinition("DownArrow", "Mũi tên xuống (Down Arrow)", ShapeType.BlockArrowDown),
                new ShapeItemDefinition("LeftRightArrow", "Mũi tên trái-phải (Left-Right Arrow)", ShapeType.BlockArrowRight),
                new ShapeItemDefinition("UpDownArrow", "Mũi tên lên-xuống (Up-Down Arrow)", ShapeType.BlockArrowUp),
                new ShapeItemDefinition("QuadArrow", "Mũi tên 4 chiều (Quad Arrow)", ShapeType.BlockArrowRight),
                new ShapeItemDefinition("BentArrow", "Mũi tên gấp khúc (Bent Arrow)", ShapeType.BlockArrowRight),
                new ShapeItemDefinition("UTurnArrow", "Mũi tên chữ U (U-Turn Arrow)", ShapeType.BlockArrowRight),
                new ShapeItemDefinition("Chevron", "Mũi tên Chevron (Chevron)", ShapeType.Chevron)
            });

            // 6. Toán học / Phương trình (Equation Shapes)
            AddSection(scrollPanel, ref curY, "Toán học / Phương trình (Equation Shapes)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("Plus", "Dấu cộng (+)", ShapeType.Cross),
                new ShapeItemDefinition("Minus", "Dấu trừ (-)", ShapeType.Rectangle),
                new ShapeItemDefinition("Multiply", "Dấu nhân (×)", ShapeType.Cross),
                new ShapeItemDefinition("Divide", "Dấu chia (÷)", ShapeType.Rectangle),
                new ShapeItemDefinition("Equal", "Dấu bằng (=)", ShapeType.Rectangle),
                new ShapeItemDefinition("NotEqual", "Dấu khác (≠)", ShapeType.Rectangle)
            });

            // 7. Lưu trình (Flowchart)
            AddSection(scrollPanel, ref curY, "Lưu trình (Flowchart)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("FlowchartProcess", "Tiến trình (Process)", ShapeType.Rectangle),
                new ShapeItemDefinition("FlowchartDecision", "Quyết định (Decision)", ShapeType.Diamond),
                new ShapeItemDefinition("FlowchartData", "Dữ liệu (Data / I/O)", ShapeType.Parallelogram),
                new ShapeItemDefinition("FlowchartTerminator", "Điểm bắt đầu/kết thúc (Terminator)", ShapeType.RoundedRectangle),
                new ShapeItemDefinition("FlowchartDocument", "Tài liệu (Document)", ShapeType.CustomPath),
                new ShapeItemDefinition("FlowchartPreparation", "Chuẩn bị (Preparation)", ShapeType.Hexagon)
            });

            // 8. Sao và Biểu ngữ (Stars and Banners)
            AddSection(scrollPanel, ref curY, "Sao và Biểu ngữ (Stars and Banners)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("Star4", "Sao 4 cánh (4-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star5", "Sao 5 cánh (5-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star6", "Sao 6 cánh (6-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star8", "Sao 8 cánh (8-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star10", "Sao 10 cánh (10-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star12", "Sao 12 cánh (12-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star16", "Sao 16 cánh (16-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star24", "Sao 24 cánh (24-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Star32", "Sao 32 cánh (32-Point Star)", ShapeType.Star),
                new ShapeItemDefinition("Explosion1", "Vụ nổ 1 (Explosion 8-pt)", ShapeType.Star),
                new ShapeItemDefinition("Explosion2", "Vụ nổ 2 (Explosion 14-pt)", ShapeType.Star),
                new ShapeItemDefinition("HorizontalScroll", "Cuộn giấy ngang (Horizontal Scroll)", ShapeType.Banner),
                new ShapeItemDefinition("VerticalScroll", "Cuộn giấy dọc (Vertical Scroll)", ShapeType.Banner),
                new ShapeItemDefinition("RibbonUp", "Dải băng hướng lên (Ribbon Up)", ShapeType.Banner),
                new ShapeItemDefinition("RibbonDown", "Dải băng hướng xuống (Ribbon Down)", ShapeType.Banner)
            });

            // 9. Chú thích (Callouts)
            AddSection(scrollPanel, ref curY, "Chú thích (Callouts)", new ShapeItemDefinition[] {
                new ShapeItemDefinition("CalloutRect", "Chú thích chữ nhật (Rectangular Callout)", ShapeType.Callout),
                new ShapeItemDefinition("CalloutRoundedRect", "Chú thích chữ nhật bo tròn (Rounded Callout)", ShapeType.Callout),
                new ShapeItemDefinition("CalloutOval", "Chú thích elip (Oval Callout)", ShapeType.Callout),
                new ShapeItemDefinition("CalloutCloud", "Chú thích đám mây (Cloud Callout)", ShapeType.Callout),
                new ShapeItemDefinition("LineCallout1", "Chú thích đường 1 (Line Callout 1)", ShapeType.Callout),
                new ShapeItemDefinition("LineCallout2", "Chú thích đường 2 (Line Callout 2)", ShapeType.Callout)
            });

            Controls.Add(scrollPanel);

            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 185, 195), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AddSection(Panel parent, ref int curY, string title, ShapeItemDefinition[] shapes)
        {
            Label lblHeader = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 45, 85),
                Location = new Point(8, curY),
                AutoSize = true
            };
            parent.Controls.Add(lblHeader);
            curY += 22;

            int col = 0;
            int rowY = curY;
            int btnSize = 30;
            int btnGap = 4;
            int maxCols = 9;

            for (int i = 0; i < shapes.Length; i++)
            {
                ShapeItemDefinition def = shapes[i];
                Button btn = new Button
                {
                    Location = new Point(8 + col * (btnSize + btnGap), rowY),
                    Size = new Size(btnSize, btnSize),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(248, 249, 251),
                    Cursor = Cursors.Hand,
                    Tag = def
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 232);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(222, 236, 252);

                tooltip.SetToolTip(btn, def.DisplayName);

                btn.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    Button b = s as Button;
                    ShapeItemDefinition itm = b != null ? (b.Tag as ShapeItemDefinition) : null;
                    if (itm != null)
                    {
                        DrawMonolineIcon(e.Graphics, 4, 4, btnSize - 8, btnSize - 8, itm.Name, Color.FromArgb(45, 55, 72));
                    }
                };

                btn.Click += (s, e) =>
                {
                    Button b = s as Button;
                    ShapeItemDefinition itm = b != null ? (b.Tag as ShapeItemDefinition) : null;
                    if (itm != null)
                    {
                        if (OnShapeNameSelected != null) OnShapeNameSelected(itm.Name);
                        if (OnShapeSelected != null) OnShapeSelected(itm.FallbackType);
                    }
                    Close();
                };

                parent.Controls.Add(btn);

                col++;
                if (col >= maxCols)
                {
                    col = 0;
                    rowY += btnSize + btnGap;
                }
            }

            if (col > 0) rowY += btnSize + btnGap;
            curY = rowY + 8;
        }

        private void DrawMonolineIcon(Graphics g, int x, int y, int w, int h, string shapeName, Color col)
        {
            using (Pen p = new Pen(col, 1.25f))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(28, 43, 87, 154)))
            {
                Rectangle r = new Rectangle(x, y, w, h);

                if (shapeName == "TextBox")
                {
                    g.DrawRectangle(p, r);
                    using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        g.DrawString("A", f, Brushes.Black, x + (w - 11) / 2, y + (h - 13) / 2);
                    }
                    return;
                }

                if (shapeName == "Line")
                {
                    g.DrawLine(p, x + 2, y + h - 2, x + w - 2, y + 2);
                    return;
                }

                if (shapeName == "ArrowLine")
                {
                    using (AdjustableArrowCap cap = new AdjustableArrowCap(3, 3, true))
                    {
                        using (Pen ap = new Pen(col, 1.3f))
                        {
                            ap.CustomEndCap = cap;
                            g.DrawLine(ap, x + 2, y + h - 2, x + w - 2, y + 2);
                        }
                    }
                    return;
                }

                if (shapeName == "DoubleArrowLine")
                {
                    using (AdjustableArrowCap cap = new AdjustableArrowCap(3, 3, true))
                    {
                        using (Pen ap = new Pen(col, 1.3f))
                        {
                            ap.CustomStartCap = cap;
                            ap.CustomEndCap = cap;
                            g.DrawLine(ap, x + 2, y + h - 2, x + w - 2, y + 2);
                        }
                    }
                    return;
                }

                if (shapeName == "ElbowConnector")
                {
                    Point[] pts = new Point[] { new Point(x + 2, y + h - 2), new Point(x + w / 2, y + h - 2), new Point(x + w / 2, y + 2), new Point(x + w - 2, y + 2) };
                    g.DrawLines(p, pts);
                    return;
                }

                if (shapeName == "ElbowArrowConnector")
                {
                    using (AdjustableArrowCap cap = new AdjustableArrowCap(3, 3, true))
                    {
                        using (Pen ap = new Pen(col, 1.3f))
                        {
                            ap.CustomEndCap = cap;
                            Point[] pts = new Point[] { new Point(x + 2, y + h - 2), new Point(x + w / 2, y + h - 2), new Point(x + w / 2, y + 2), new Point(x + w - 2, y + 2) };
                            g.DrawLines(ap, pts);
                        }
                    }
                    return;
                }

                if (shapeName == "CurvedConnector")
                {
                    g.DrawBezier(p, x + 2, y + h - 2, x + 2, y + 2, x + w - 2, y + h - 2, x + w - 2, y + 2);
                    return;
                }

                if (shapeName == "Divide")
                {
                    g.DrawLine(p, x + 3, y + h / 2, x + w - 3, y + h / 2);
                    using (SolidBrush dotB = new SolidBrush(col))
                    {
                        g.FillEllipse(dotB, x + w / 2 - 2, y + h * 0.25f - 2, 4, 4);
                        g.FillEllipse(dotB, x + w / 2 - 2, y + h * 0.75f - 2, 4, 4);
                    }
                    return;
                }

                if (shapeName == "NotEqual")
                {
                    g.DrawLine(p, x + 3, y + h * 0.38f, x + w - 3, y + h * 0.38f);
                    g.DrawLine(p, x + 3, y + h * 0.62f, x + w - 3, y + h * 0.62f);
                    g.DrawLine(p, x + w * 0.75f, y + 2, x + w * 0.25f, y + h - 2);
                    return;
                }

                using (GraphicsPath path = ShapeRenderer.CreateShapePath(shapeName, r))
                {
                    if (path != null && path.PointCount > 1)
                    {
                        g.FillPath(b, path);
                        g.DrawPath(p, path);
                    }
                    else
                    {
                        g.FillRectangle(b, r);
                        g.DrawRectangle(p, r);
                    }
                }
            }
        }
    }
}

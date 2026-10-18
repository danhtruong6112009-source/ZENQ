using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class TransitionsGalleryDropDownForm : Form
    {
        public event Action<TransitionType> OnTransitionSelected;

        public TransitionsGalleryDropDownForm(Point screenLocation, TransitionType currentTransition)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(680, 520);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 12)
            };

            int curY = 10;

            // Section 1: Nhẹ nhàng (Subtle) - 13 items
            AddCategory(scrollPanel, ref curY, "Nhẹ nhàng (Subtle)", currentTransition, new Tuple<TransitionType, string>[] {
                Tuple.Create(TransitionType.None, "Không có"),
                Tuple.Create(TransitionType.Morph, "Biến hình ⭐"),
                Tuple.Create(TransitionType.Fade, "Mờ dần"),
                Tuple.Create(TransitionType.Push, "Đẩy"),
                Tuple.Create(TransitionType.Wipe, "Quét"),
                Tuple.Create(TransitionType.Split, "Tách đôi"),
                Tuple.Create(TransitionType.Reveal, "Hiển thị"),
                Tuple.Create(TransitionType.Cut, "Cắt nhanh"),
                Tuple.Create(TransitionType.RandomBars, "Thanh sọc"),
                Tuple.Create(TransitionType.Shape, "Hình dạng"),
                Tuple.Create(TransitionType.Uncover, "Mở màn"),
                Tuple.Create(TransitionType.Cover, "Phủ lên"),
                Tuple.Create(TransitionType.Flash, "Chớp sáng")
            }, true);

            // Section 2: Nổi bật & Nghệ thuật (Exciting) - 27 items
            AddCategory(scrollPanel, ref curY, "Nổi bật & Nghệ thuật (Exciting)", currentTransition, new Tuple<TransitionType, string>[] {
                Tuple.Create(TransitionType.FallOver, "Đổ xuống"),
                Tuple.Create(TransitionType.Curtains, "Kéo rèm"),
                Tuple.Create(TransitionType.Wind, "Gió cuốn"),
                Tuple.Create(TransitionType.Prestige, "Hào quang"),
                Tuple.Create(TransitionType.Fracture, "Rạn vỡ"),
                Tuple.Create(TransitionType.Crush, "Vò nát"),
                Tuple.Create(TransitionType.PeelOff, "Lột trang"),
                Tuple.Create(TransitionType.PageCurl, "Lật sách"),
                Tuple.Create(TransitionType.Airplane, "Máy bay"),
                Tuple.Create(TransitionType.Origami, "Gấp giấy"),
                Tuple.Create(TransitionType.Dissolve, "Hòa tan"),
                Tuple.Create(TransitionType.Checker, "Bàn cờ"),
                Tuple.Create(TransitionType.Blinds, "Mành sáo"),
                Tuple.Create(TransitionType.Clock, "Đồng hồ"),
                Tuple.Create(TransitionType.Ripple, "Gợn sóng"),
                Tuple.Create(TransitionType.Honeycomb, "Tổ ong"),
                Tuple.Create(TransitionType.Glitter, "Kim tuyến"),
                Tuple.Create(TransitionType.Vortex, "Lốc xoáy"),
                Tuple.Create(TransitionType.Shred, "Cắt sợi"),
                Tuple.Create(TransitionType.Switch, "Đổi thẻ"),
                Tuple.Create(TransitionType.Flip, "Lật thẻ"),
                Tuple.Create(TransitionType.Gallery, "Triển lãm"),
                Tuple.Create(TransitionType.Cube, "Lập phương"),
                Tuple.Create(TransitionType.Doors, "Mở cửa"),
                Tuple.Create(TransitionType.Box, "Hộp 3D"),
                Tuple.Create(TransitionType.Comb, "Răng lược"),
                Tuple.Create(TransitionType.Zoom, "Thu phóng 3D")
            }, true);

            // Section 3: Nội dung động (Dynamic Content) - 7 items
            AddCategory(scrollPanel, ref curY, "Nội dung động (Dynamic Content)", currentTransition, new Tuple<TransitionType, string>[] {
                Tuple.Create(TransitionType.Pan, "Toàn cảnh"),
                Tuple.Create(TransitionType.FerrisWheel, "Vòng quay"),
                Tuple.Create(TransitionType.Conveyor, "Băng chuyền"),
                Tuple.Create(TransitionType.Rotate, "Xoay vòng"),
                Tuple.Create(TransitionType.Window, "Cửa sổ"),
                Tuple.Create(TransitionType.Orbit, "Quỹ đạo"),
                Tuple.Create(TransitionType.FlyThrough, "Bay xuyên")
            }, true);

            // Section 4: Độc quyền ZenQ (ZenQ Exclusive) - 10 items
            AddCategory(scrollPanel, ref curY, "⭐ Độc quyền ZenQ (ZenQ Exclusive)", currentTransition, new Tuple<TransitionType, string>[] {
                Tuple.Create(TransitionType.GenOffice_CinematicPrism, "Lăng kính"),
                Tuple.Create(TransitionType.GenOffice_LiquidFluid, "Thủy dịch"),
                Tuple.Create(TransitionType.GenOffice_MatrixRain, "Mưa số"),
                Tuple.Create(TransitionType.GenOffice_NeonPulse, "Xung Neon"),
                Tuple.Create(TransitionType.GenOffice_PortalWarp, "Lỗ sâu"),
                Tuple.Create(TransitionType.GenOffice_Supernova, "Siêu tân tinh"),
                Tuple.Create(TransitionType.GenOffice_HologramGlitch, "Hologram"),
                Tuple.Create(TransitionType.GenOffice_Kaleidoscope, "Vạn hoa"),
                Tuple.Create(TransitionType.GenOffice_Starlight, "Bão sao"),
                Tuple.Create(TransitionType.GenOffice_GoldenRibbon, "Lụa Vàng"),
                Tuple.Create(TransitionType.GenOffice_SmoothFlow, "Chảy tràn")
            }, false);

            Controls.Add(scrollPanel);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(170, 170, 170), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AddCategory(Panel parent, ref int curY, string title, TransitionType currentTransition, Tuple<TransitionType, string>[] items, bool addDivider)
        {
            Label lblHeader = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(12, curY),
                AutoSize = true
            };
            parent.Controls.Add(lblHeader);
            curY += 24;

            int col = 0;
            int rowY = curY;
            int itemW = 68;
            int itemH = 56;
            int colsPerRow = 9;

            for (int i = 0; i < items.Length; i++)
            {
                TransitionType tType = items[i].Item1;
                string tName = items[i].Item2;
                bool isSel = (currentTransition == tType);

                Panel pnl = new Panel
                {
                    Location = new Point(12 + col * (itemW + 4), rowY),
                    Size = new Size(itemW, itemH),
                    BackColor = isSel ? Color.FromArgb(204, 232, 255) : Color.White,
                    Cursor = Cursors.Hand
                };

                bool isHovered = false;

                pnl.MouseEnter += (s, e) => { isHovered = true; pnl.Invalidate(); };
                pnl.MouseLeave += (s, e) => { isHovered = false; pnl.Invalidate(); };

                pnl.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.Half;

                    // Hover / Selection Background
                    if (isSel)
                    {
                        using (SolidBrush selBg = new SolidBrush(Color.FromArgb(204, 232, 255)))
                        using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                        {
                            g.FillRectangle(selBg, 0, 0, itemW - 1, itemH - 1);
                            g.DrawRectangle(selPen, 0, 0, itemW - 1, itemH - 1);
                        }
                    }
                    else if (isHovered)
                    {
                        using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(229, 241, 251)))
                        using (Pen hovPen = new Pen(Color.FromArgb(112, 192, 231), 1.0f))
                        {
                            g.FillRectangle(hovBg, 0, 0, itemW - 1, itemH - 1);
                            g.DrawRectangle(hovPen, 0, 0, itemW - 1, itemH - 1);
                        }
                    }

                    // Draw Pixel-Accurate PowerPoint Vector Icon
                    Rectangle iconRect = new Rectangle((itemW - 28) / 2, 6, 28, 18);
                    TransitionIconRenderer.DrawIcon(g, iconRect, tType, isSel);

                    // Label
                    using (Font f = new Font("Segoe UI", 7.5f, isSel ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush txtBrush = new SolidBrush(isSel ? Color.FromArgb(0, 80, 160) : Color.FromArgb(38, 38, 38)))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.NoWrap
                        };
                        g.DrawString(tName, f, txtBrush, new RectangleF(1, 28, itemW - 2, 24), sf);
                    }
                };

                pnl.Click += (s, e) =>
                {
                    if (OnTransitionSelected != null) OnTransitionSelected(tType);
                    Close();
                };

                parent.Controls.Add(pnl);

                col++;
                if (col >= colsPerRow)
                {
                    col = 0;
                    rowY += itemH + 4;
                }
            }

            if (col > 0) rowY += itemH + 4;

            if (addDivider)
            {
                Panel div = new Panel
                {
                    Location = new Point(12, rowY + 6),
                    Size = new Size(colsPerRow * (itemW + 4) - 4, 1),
                    BackColor = Color.FromArgb(225, 225, 225)
                };
                parent.Controls.Add(div);
                curY = rowY + 16;
            }
            else
            {
                curY = rowY + 10;
            }
        }
    }

    /// <summary>
    /// Bộ vẽ đồ họa vector chính xác từng pixel cho toàn bộ biểu tượng Transition chuẩn Microsoft PowerPoint 365
    /// </summary>
    public static class TransitionIconRenderer
    {
        private static readonly Color BluePrimary = Color.FromArgb(37, 99, 235);    // #2563EB
        private static readonly Color BlueDark = Color.FromArgb(29, 78, 216);       // #1D4ED8
        private static readonly Color BlueLight = Color.FromArgb(147, 197, 253);   // #93C5FD
        private static readonly Color BluePale = Color.FromArgb(224, 242, 254);    // #E0F2FE
        private static readonly Color BorderColor = Color.FromArgb(30, 64, 175);    // #1E40AF
        private static readonly Color WhiteColor = Color.White;

        public static void DrawIcon(D2DGraphics g, Rectangle r, TransitionType type, bool isSelected)
        {
            if (g == null) return;
            g.DrawGdi(nativeG => DrawIcon(nativeG, r, type, isSelected));
        }

        public static void DrawIcon(Graphics g, Rectangle r, TransitionType type, bool isSelected)
        {
            int x = r.X;
            int y = r.Y;
            int w = r.Width;
            int h = r.Height;

            switch (type)
            {
                case TransitionType.None:
                    using (Pen p = new Pen(Color.FromArgb(140, 160, 185), 1.2f))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 242, 250)))
                    {
                        g.FillRectangle(b, x, y, w, h);
                        g.DrawRectangle(p, x, y, w, h);
                    }
                    break;

                case TransitionType.Morph:
                    // 4 cascading curved morph squares
                    using (SolidBrush b1 = new SolidBrush(Color.FromArgb(30, 64, 175)))
                    using (SolidBrush b2 = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    using (SolidBrush b3 = new SolidBrush(Color.FromArgb(96, 165, 250)))
                    using (SolidBrush b4 = new SolidBrush(Color.FromArgb(147, 197, 253)))
                    using (Pen p = new Pen(BorderColor, 1f))
                    {
                        g.FillRectangle(b1, x + 1, y + 1, 6, 6);
                        g.DrawRectangle(p, x + 1, y + 1, 6, 6);

                        g.FillRectangle(b2, x + 7, y + 3, 7, 7);
                        g.DrawRectangle(p, x + 7, y + 3, 7, 7);

                        g.FillRectangle(b3, x + 13, y + 6, 8, 8);
                        g.DrawRectangle(p, x + 13, y + 6, 8, 8);

                        g.FillRectangle(b4, x + 19, y + 9, 8, 8);
                        g.DrawRectangle(p, x + 19, y + 9, 8, 8);
                    }
                    break;

                case TransitionType.Fade:
                    // Two overlapping fading slides
                    using (SolidBrush bBack = new SolidBrush(BlueLight))
                    using (Pen pBack = new Pen(BorderColor, 1.0f))
                    using (SolidBrush bFront = new SolidBrush(BlueDark))
                    using (Pen pFront = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(bBack, x, y, 18, 12);
                        g.DrawRectangle(pBack, x, y, 18, 12);

                        g.FillRectangle(bFront, x + 8, y + 5, 19, 12);
                        g.DrawRectangle(pFront, x + 8, y + 5, 19, 12);
                    }
                    break;

                case TransitionType.Push:
                    // Slide with horizontal dividing line & upward arrow
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (Pen p = new Pen(WhiteColor, 1.5f))
                    {
                        g.DrawLine(p, x, y + 11, x + w, y + 11);
                        DrawArrowUp(g, x + w / 2, y + 7, 8, WhiteColor);
                    }
                    break;

                case TransitionType.Wipe:
                    // Horizontal gradient wipe with left arrow
                    using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(x, y, w, h), BlueDark, BluePale, 0f))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(lgb, x, y, w, h);
                        g.DrawRectangle(p, x, y, w, h);
                        DrawArrowLeft(g, x + w / 2, y + h / 2, 7, WhiteColor);
                    }
                    break;

                case TransitionType.Split:
                    // Split in half with left and right outward arrows
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (Pen p = new Pen(Color.FromArgb(15, 23, 42), 1.2f))
                    {
                        g.DrawLine(p, x + w / 2, y, x + w / 2, y + h);
                    }
                    DrawArrowLeft(g, x + 8, y + h / 2, 5, WhiteColor);
                    DrawArrowRight(g, x + w - 8, y + h / 2, 5, WhiteColor);
                    break;

                case TransitionType.Reveal:
                    // Back light slide + front slide sliding away
                    using (SolidBrush b1 = new SolidBrush(BlueLight))
                    using (Pen p1 = new Pen(BorderColor, 1f))
                    using (SolidBrush b2 = new SolidBrush(BlueDark))
                    using (Pen p2 = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(b1, x + 1, y + 1, 18, 12);
                        g.DrawRectangle(p1, x + 1, y + 1, 18, 12);

                        g.FillRectangle(b2, x + 9, y + 4, 18, 13);
                        g.DrawRectangle(p2, x + 9, y + 4, 18, 13);
                    }
                    break;

                case TransitionType.Cut:
                    // Sharp cut overlapping slides
                    using (SolidBrush b1 = new SolidBrush(BlueLight))
                    using (SolidBrush b2 = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(b1, x + 2, y + 2, 22, 14);
                        g.DrawRectangle(p, x + 2, y + 2, 22, 14);
                        g.FillRectangle(b2, x + 8, y + 6, 18, 11);
                        g.DrawRectangle(p, x + 8, y + 6, 18, 11);
                    }
                    break;

                case TransitionType.RandomBars:
                    // Vertical bars
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawRectangle(p, x, y, w, h);
                        int barW = w / 5;
                        Color[] bCols = new Color[] { BlueDark, BlueLight, BluePrimary, BluePale, BlueDark };
                        for (int i = 0; i < 5; i++)
                        {
                            using (SolidBrush sb = new SolidBrush(bCols[i]))
                            {
                                g.FillRectangle(sb, x + 1 + i * barW, y + 1, barW, h - 1);
                            }
                        }
                    }
                    break;

                case TransitionType.Shape:
                    // Expanding concentric circles
                    DrawSlideFrame(g, x, y, w, h, BluePale);
                    using (SolidBrush bOuter = new SolidBrush(BlueLight))
                    using (SolidBrush bMid = new SolidBrush(BluePrimary))
                    using (SolidBrush bInner = new SolidBrush(BlueDark))
                    {
                        g.FillEllipse(bOuter, x + (w - 18) / 2, y + (h - 18) / 2, 18, 18);
                        g.FillEllipse(bMid, x + (w - 12) / 2, y + (h - 12) / 2, 12, 12);
                        g.FillEllipse(bInner, x + (w - 6) / 2, y + (h - 6) / 2, 6, 6);
                    }
                    break;

                case TransitionType.Uncover:
                    // Left pale background, right slide with left arrow
                    using (SolidBrush b1 = new SolidBrush(BluePale))
                    using (SolidBrush b2 = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(b1, x, y, w, h);
                        g.FillRectangle(b2, x + w - 8, y, 8, h);
                        g.DrawRectangle(p, x, y, w, h);
                        g.DrawLine(p, x + w - 8, y, x + w - 8, y + h);
                        DrawArrowLeft(g, x + 10, y + h / 2, 6, BorderColor);
                    }
                    break;

                case TransitionType.Cover:
                    // Right slide covering left with left arrow
                    using (SolidBrush b1 = new SolidBrush(BluePale))
                    using (SolidBrush b2 = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(b1, x, y, w, h);
                        g.FillRectangle(b2, x + 6, y, w - 6, h);
                        g.DrawRectangle(p, x, y, w, h);
                        DrawArrowLeft(g, x + w / 2 + 2, y + h / 2, 6, WhiteColor);
                    }
                    break;

                case TransitionType.Flash:
                    // Blue background with bright 8-point flash starburst
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    DrawStarburst(g, x + w / 2, y + h / 2, 7, WhiteColor);
                    break;

                case TransitionType.FallOver:
                    // 3D perspective falling trapezoid
                    Point[] fallPts = new Point[] {
                        new Point(x + 4, y + 2),
                        new Point(x + w - 4, y + 2),
                        new Point(x + w, y + h),
                        new Point(x, y + h)
                    };
                    using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(x, y, w, h), BlueLight, BlueDark, 90f))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillPolygon(lgb, fallPts);
                        g.DrawPolygon(p, fallPts);
                    }
                    break;

                case TransitionType.Curtains:
                    // Two draped theatre curtains parting
                    using (SolidBrush bDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (SolidBrush bCurtain = new SolidBrush(BluePrimary))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillRectangle(bDark, x, y, w, h);
                        Point[] leftCurtain = new Point[] {
                            new Point(x, y), new Point(x + 12, y),
                            new Point(x + 5, y + h), new Point(x, y + h)
                        };
                        Point[] rightCurtain = new Point[] {
                            new Point(x + w - 12, y), new Point(x + w, y),
                            new Point(x + w, y + h), new Point(x + w - 5, y + h)
                        };
                        g.FillPolygon(bCurtain, leftCurtain);
                        g.FillPolygon(bCurtain, rightCurtain);
                        g.DrawRectangle(p, x, y, w, h);
                    }
                    break;

                case TransitionType.Wind:
                    // Wavy wind flag lines
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (Pen p = new Pen(WhiteColor, 1.4f))
                    {
                        g.DrawArc(p, x + 3, y + 4, 10, 8, 180, 180);
                        g.DrawArc(p, x + 13, y + 4, 10, 8, 0, 180);
                        g.DrawArc(p, x + 3, y + 10, 10, 8, 180, 180);
                        g.DrawArc(p, x + 13, y + 10, 10, 8, 0, 180);
                    }
                    break;

                case TransitionType.Prestige:
                    // Elegant arched drape
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    using (Pen p = new Pen(WhiteColor, 1.5f))
                    using (SolidBrush b = new SolidBrush(BlueLight))
                    {
                        g.FillPie(b, x + 2, y - 6, w - 4, 20, 0, 180);
                        g.DrawArc(p, x + 2, y - 6, w - 4, 20, 0, 180);
                    }
                    break;

                case TransitionType.Fracture:
                    // Glass fracture cracks
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (Pen p = new Pen(WhiteColor, 1.3f))
                    {
                        int cx = x + w / 2;
                        int cy = y + h / 2;
                        g.DrawLine(p, cx, cy, x + 2, y + 3);
                        g.DrawLine(p, cx, cy, x + w - 3, y + 2);
                        g.DrawLine(p, cx, cy, x + 4, y + h - 2);
                        g.DrawLine(p, cx, cy, x + w - 2, y + h - 4);
                        g.DrawLine(p, cx, cy, cx + 4, y + h);
                        g.DrawLine(p, x + 6, y + 7, x + 12, y + 3);
                    }
                    break;

                case TransitionType.Crush:
                    // Crumpled paper polygon facets
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(240, 240, 240));
                    using (SolidBrush b1 = new SolidBrush(BluePrimary))
                    using (SolidBrush b2 = new SolidBrush(BlueLight))
                    using (SolidBrush b3 = new SolidBrush(BlueDark))
                    {
                        Point[] p1 = new Point[] { new Point(x + 5, y + 3), new Point(x + 15, y + 2), new Point(x + 11, y + 9) };
                        Point[] p2 = new Point[] { new Point(x + 15, y + 2), new Point(x + 23, y + 6), new Point(x + 17, y + 12) };
                        Point[] p3 = new Point[] { new Point(x + 5, y + 3), new Point(x + 11, y + 9), new Point(x + 6, y + 15) };
                        Point[] p4 = new Point[] { new Point(x + 11, y + 9), new Point(x + 17, y + 12), new Point(x + 13, y + 16) };
                        g.FillPolygon(b1, p1);
                        g.FillPolygon(b2, p2);
                        g.FillPolygon(b3, p3);
                        g.FillPolygon(b1, p4);
                    }
                    break;

                case TransitionType.PeelOff:
                    // Page corner peeling off top-right
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    Point[] peel = new Point[] {
                        new Point(x + w - 10, y),
                        new Point(x + w, y + 10),
                        new Point(x + w - 10, y + 10)
                    };
                    using (SolidBrush b = new SolidBrush(WhiteColor))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillPolygon(b, peel);
                        g.DrawPolygon(p, peel);
                    }
                    break;

                case TransitionType.PageCurl:
                    // 3D curled roll on right edge
                    DrawSlideFrame(g, x, y, w - 6, h, BluePrimary);
                    using (SolidBrush bCurl = new SolidBrush(BlueLight))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillEllipse(bCurl, x + w - 10, y, 9, h);
                        g.DrawEllipse(p, x + w - 10, y, 9, h);
                    }
                    break;

                case TransitionType.Airplane:
                    // Origami airplane flying top-right
                    using (SolidBrush b1 = new SolidBrush(BluePrimary))
                    using (SolidBrush b2 = new SolidBrush(BlueLight))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        Point[] wing1 = new Point[] { new Point(x + 2, y + 14), new Point(x + 24, y + 2), new Point(x + 12, y + 15) };
                        Point[] wing2 = new Point[] { new Point(x + 12, y + 15), new Point(x + 24, y + 2), new Point(x + 15, y + 9) };
                        g.FillPolygon(b1, wing1);
                        g.FillPolygon(b2, wing2);
                        g.DrawPolygon(p, wing1);
                        g.DrawPolygon(p, wing2);
                    }
                    break;

                case TransitionType.Origami:
                    // Folded origami swan/bird
                    using (SolidBrush b1 = new SolidBrush(BluePrimary))
                    using (SolidBrush b2 = new SolidBrush(BlueLight))
                    using (SolidBrush b3 = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1f))
                    {
                        Point[] f1 = new Point[] { new Point(x + 4, y + 13), new Point(x + 14, y + 3), new Point(x + 16, y + 13) };
                        Point[] f2 = new Point[] { new Point(x + 14, y + 3), new Point(x + 23, y + 7), new Point(x + 16, y + 13) };
                        Point[] f3 = new Point[] { new Point(x + 14, y + 3), new Point(x + 7, y + 5), new Point(x + 4, y + 13) };
                        g.FillPolygon(b1, f1);
                        g.FillPolygon(b2, f2);
                        g.FillPolygon(b3, f3);
                        g.DrawPolygon(p, f1);
                        g.DrawPolygon(p, f2);
                    }
                    break;

                case TransitionType.Dissolve:
                    // Checkerboard pixel dissolving matrix
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawRectangle(p, x, y, w, h);
                        int pw = w / 6;
                        int ph = h / 4;
                        for (int rIndex = 0; rIndex < 4; rIndex++)
                        {
                            for (int cIndex = 0; cIndex < 6; cIndex++)
                            {
                                if ((rIndex + cIndex) % 2 == 0)
                                {
                                    using (SolidBrush b = new SolidBrush(BluePrimary))
                                    {
                                        g.FillRectangle(b, x + cIndex * pw + 1, y + rIndex * ph + 1, pw, ph);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case TransitionType.Checker:
                    // Checkerboard pattern with right arrow
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawRectangle(p, x, y, w, h);
                        int pw = w / 4;
                        int ph = h / 3;
                        for (int rIndex = 0; rIndex < 3; rIndex++)
                        {
                            for (int cIndex = 0; cIndex < 4; cIndex++)
                            {
                                Color col = ((rIndex + cIndex) % 2 == 0) ? BlueDark : BlueLight;
                                using (SolidBrush b = new SolidBrush(col))
                                {
                                    g.FillRectangle(b, x + cIndex * pw + 1, y + rIndex * ph + 1, pw, ph);
                                }
                            }
                        }
                        DrawArrowRight(g, x + w / 2, y + h / 2, 7, WhiteColor);
                    }
                    break;

                case TransitionType.Blinds:
                    // Vertical blind slats
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawRectangle(p, x, y, w, h);
                        int slatW = w / 5;
                        for (int i = 0; i < 5; i++)
                        {
                            using (SolidBrush b = new SolidBrush(BluePrimary))
                            {
                                g.FillRectangle(b, x + 1 + i * slatW, y + 1, slatW - 1, h - 1);
                            }
                        }
                    }
                    break;

                case TransitionType.Clock:
                    // Radial pie sweep
                    DrawSlideFrame(g, x, y, w, h, BluePale);
                    using (SolidBrush b = new SolidBrush(BluePrimary))
                    {
                        g.FillPie(b, x + (w - 18) / 2, y + (h - 18) / 2, 18, 18, -90, 140);
                    }
                    break;

                case TransitionType.Ripple:
                    // Water ripple concentric rings
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    using (Pen p = new Pen(WhiteColor, 1.2f))
                    {
                        int cx = x + w / 2;
                        int cy = y + h / 2;
                        g.DrawEllipse(p, cx - 4, cy - 3, 8, 6);
                        g.DrawEllipse(p, cx - 8, cy - 6, 16, 12);
                        g.DrawEllipse(p, cx - 12, cy - 8, 24, 16);
                    }
                    break;

                case TransitionType.Honeycomb:
                    // Hexagonal honeycomb cells
                    DrawSlideFrame(g, x, y, w, h, BluePale);
                    DrawHexagon(g, x + 7, y + 5, 4, BluePrimary);
                    DrawHexagon(g, x + 14, y + 5, 4, BluePrimary);
                    DrawHexagon(g, x + 21, y + 5, 4, BluePrimary);
                    DrawHexagon(g, x + 10, y + 12, 4, BluePrimary);
                    DrawHexagon(g, x + 17, y + 12, 4, BluePrimary);
                    break;

                case TransitionType.Glitter:
                    // Sparkling glitter stars & hexagons
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    DrawStarburst(g, x + 6, y + 5, 4, WhiteColor);
                    DrawStarburst(g, x + 21, y + 6, 4, WhiteColor);
                    DrawStarburst(g, x + 14, y + 11, 5, WhiteColor);
                    DrawStarburst(g, x + 6, y + 14, 3, WhiteColor);
                    break;

                case TransitionType.Vortex:
                    // Swirling vortex dots
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    using (SolidBrush b = new SolidBrush(WhiteColor))
                    {
                        int cx = x + w / 2;
                        int cy = y + h / 2;
                        g.FillEllipse(b, cx, cy, 2, 2);
                        g.FillEllipse(b, cx + 4, cy - 3, 2, 2);
                        g.FillEllipse(b, cx + 7, cy + 2, 2, 2);
                        g.FillEllipse(b, cx - 4, cy + 4, 2, 2);
                        g.FillEllipse(b, cx - 7, cy - 2, 2, 2);
                        g.FillEllipse(b, cx + 9, cy - 5, 2, 2);
                        g.FillEllipse(b, cx - 9, cy + 5, 2, 2);
                    }
                    break;

                case TransitionType.Shred:
                    // Vertical shredded ribbons
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawRectangle(p, x, y, w, h);
                        int sW = w / 5;
                        for (int i = 0; i < 5; i++)
                        {
                            int offY = (i % 2 == 0) ? 2 : 5;
                            using (SolidBrush b = new SolidBrush(BluePrimary))
                            {
                                g.FillRectangle(b, x + 1 + i * sW, y + offY, sW - 1, h - 6);
                            }
                        }
                    }
                    break;

                case TransitionType.Switch:
                    // Two 3D perspective cards switching
                    using (SolidBrush b1 = new SolidBrush(BlueLight))
                    using (SolidBrush b2 = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        Point[] card1 = new Point[] { new Point(x + 2, y + 4), new Point(x + 13, y + 2), new Point(x + 13, y + 14), new Point(x + 2, y + 16) };
                        Point[] card2 = new Point[] { new Point(x + 15, y + 2), new Point(x + 26, y + 4), new Point(x + 26, y + 16), new Point(x + 15, y + 14) };
                        g.FillPolygon(b1, card1);
                        g.FillPolygon(b2, card2);
                        g.DrawPolygon(p, card1);
                        g.DrawPolygon(p, card2);
                    }
                    break;

                case TransitionType.Flip:
                    // Flipping 3D card with center split
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (Pen p = new Pen(WhiteColor, 1.4f))
                    {
                        g.DrawLine(p, x + w / 2, y, x + w / 2, y + h);
                    }
                    break;

                case TransitionType.Gallery:
                    // Curved 3D gallery perspective
                    Point[] galPts = new Point[] {
                        new Point(x + 3, y + 3), new Point(x + w - 3, y + 3),
                        new Point(x + w - 1, y + h - 3), new Point(x + 1, y + h - 3)
                    };
                    using (SolidBrush b = new SolidBrush(BluePrimary))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.FillPolygon(b, galPts);
                        g.DrawPolygon(p, galPts);
                    }
                    break;

                case TransitionType.Cube:
                    // 3D isometric cube
                    using (SolidBrush bTop = new SolidBrush(BlueLight))
                    using (SolidBrush bLeft = new SolidBrush(BluePrimary))
                    using (SolidBrush bRight = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1f))
                    {
                        int cx = x + w / 2;
                        Point[] topF = new Point[] { new Point(cx, y + 1), new Point(cx + 9, y + 5), new Point(cx, y + 9), new Point(cx - 9, y + 5) };
                        Point[] leftF = new Point[] { new Point(cx - 9, y + 5), new Point(cx, y + 9), new Point(cx, y + 17), new Point(cx - 9, y + 13) };
                        Point[] rightF = new Point[] { new Point(cx, y + 9), new Point(cx + 9, y + 5), new Point(cx + 9, y + 13), new Point(cx, y + 17) };
                        g.FillPolygon(bTop, topF);
                        g.FillPolygon(bLeft, leftF);
                        g.FillPolygon(bRight, rightF);
                        g.DrawPolygon(p, topF);
                        g.DrawPolygon(p, leftF);
                        g.DrawPolygon(p, rightF);
                    }
                    break;

                case TransitionType.Doors:
                    // Double doors opening in 3D
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(15, 23, 42));
                    using (SolidBrush bDoor = new SolidBrush(BluePrimary))
                    using (Pen p = new Pen(WhiteColor, 1.2f))
                    {
                        Point[] d1 = new Point[] { new Point(x + 1, y + 1), new Point(x + 11, y + 3), new Point(x + 11, y + h - 3), new Point(x + 1, y + h - 1) };
                        Point[] d2 = new Point[] { new Point(x + w - 1, y + 1), new Point(x + w - 11, y + 3), new Point(x + w - 11, y + h - 3), new Point(x + w - 1, y + h - 1) };
                        g.FillPolygon(bDoor, d1);
                        g.FillPolygon(bDoor, d2);
                        g.DrawPolygon(p, d1);
                        g.DrawPolygon(p, d2);
                    }
                    break;

                case TransitionType.Box:
                    // Inside box perspective
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (SolidBrush bInner = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(WhiteColor, 1.2f))
                    {
                        g.FillRectangle(bInner, x + 6, y + 4, w - 12, h - 8);
                        g.DrawRectangle(p, x + 6, y + 4, w - 12, h - 8);
                        g.DrawLine(p, x, y, x + 6, y + 4);
                        g.DrawLine(p, x + w, y, x + w - 6, y + 4);
                        g.DrawLine(p, x, y + h, x + 6, y + h - 4);
                        g.DrawLine(p, x + w, y + h, x + w - 6, y + h - 4);
                    }
                    break;

                case TransitionType.Comb:
                    // Interlocking horizontal comb teeth
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawRectangle(p, x, y, w, h);
                        int rowH = h / 4;
                        for (int i = 0; i < 4; i++)
                        {
                            using (SolidBrush b = new SolidBrush((i % 2 == 0) ? BluePrimary : BlueLight))
                            {
                                int rw = (i % 2 == 0) ? (w - 6) : (w - 10);
                                int rx = (i % 2 == 0) ? x + 1 : x + 9;
                                g.FillRectangle(b, rx, y + 1 + i * rowH, rw, rowH - 1);
                            }
                        }
                    }
                    break;

                case TransitionType.Zoom:
                    // Center small slide with 4 outward arrows
                    DrawSlideFrame(g, x, y, w, h, BluePale);
                    using (SolidBrush b = new SolidBrush(BlueDark))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        Rectangle inner = new Rectangle(x + 7, y + 4, w - 14, h - 8);
                        g.FillRectangle(b, inner);
                        g.DrawRectangle(p, inner);
                    }
                    using (Pen ap = new Pen(BorderColor, 1.3f))
                    {
                        g.DrawLine(ap, x + 7, y + 4, x + 2, y + 1);
                        g.DrawLine(ap, x + w - 7, y + 4, x + w - 2, y + 1);
                        g.DrawLine(ap, x + 7, y + h - 4, x + 2, y + h - 1);
                        g.DrawLine(ap, x + w - 7, y + h - 4, x + w - 2, y + h - 1);
                    }
                    break;

                case TransitionType.Pan:
                    // Slide with vertical upward pan arrow
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    DrawArrowUp(g, x + w / 2, y + 6, 7, WhiteColor);
                    using (Pen p = new Pen(WhiteColor, 1.3f))
                    {
                        g.DrawLine(p, x + w / 2, y + 6, x + w / 2, y + h - 3);
                    }
                    break;

                case TransitionType.FerrisWheel:
                    // Carousel / Ferris wheel spoke arch
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    using (Pen p = new Pen(WhiteColor, 1.3f))
                    using (SolidBrush b = new SolidBrush(BlueLight))
                    {
                        g.DrawArc(p, x + 2, y + 2, w - 4, (h - 4) * 2, 180, 180);
                        int cx = x + w / 2;
                        int cy = y + h - 2;
                        g.DrawLine(p, cx, cy, x + 4, y + 5);
                        g.DrawLine(p, cx, cy, x + w / 2, y + 2);
                        g.DrawLine(p, cx, cy, x + w - 4, y + 5);
                        g.FillEllipse(b, x + 3, y + 4, 4, 4);
                        g.FillEllipse(b, x + w / 2 - 2, y + 1, 4, 4);
                        g.FillEllipse(b, x + w - 7, y + 4, 4, 4);
                    }
                    break;

                case TransitionType.Conveyor:
                    // Isometric conveyor belt track
                    using (SolidBrush bTrack = new SolidBrush(BluePrimary))
                    using (SolidBrush bBox = new SolidBrush(BlueLight))
                    using (Pen p = new Pen(BorderColor, 1.2f))
                    {
                        Point[] track = new Point[] { new Point(x + 1, y + 12), new Point(x + 16, y + 5), new Point(x + w - 1, y + 9), new Point(x + 12, y + 16) };
                        g.FillPolygon(bTrack, track);
                        g.DrawPolygon(p, track);
                        g.FillRectangle(bBox, x + 10, y + 4, 8, 8);
                        g.DrawRectangle(p, x + 10, y + 4, 8, 8);
                    }
                    break;

                case TransitionType.Rotate:
                    // 3D cube with orbit rotation arrow
                    DrawSlideFrame(g, x + 4, y + 2, w - 8, h - 4, BluePrimary);
                    using (Pen p = new Pen(Color.FromArgb(234, 88, 12), 1.6f))
                    {
                        g.DrawArc(p, x + 1, y + 1, w - 2, h - 2, 45, 270);
                    }
                    break;

                case TransitionType.Window:
                    // 4-pane window frame
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    using (Pen p = new Pen(WhiteColor, 1.5f))
                    using (SolidBrush b = new SolidBrush(BlueLight))
                    {
                        g.FillRectangle(b, x + 3, y + 2, 10, 6);
                        g.FillRectangle(b, x + 15, y + 2, 10, 6);
                        g.FillRectangle(b, x + 3, y + 10, 10, 6);
                        g.FillRectangle(b, x + 15, y + 10, 10, 6);
                        g.DrawLine(p, x + w / 2, y, x + w / 2, y + h);
                        g.DrawLine(p, x, y + h / 2, x + w, y + h / 2);
                    }
                    break;

                case TransitionType.Orbit:
                    // Central sphere with orbit ellipse
                    DrawSlideFrame(g, x, y, w, h, BlueDark);
                    using (SolidBrush b = new SolidBrush(BlueLight))
                    using (Pen p = new Pen(WhiteColor, 1.3f))
                    {
                        g.DrawEllipse(p, x + 2, y + 3, w - 4, h - 6);
                        g.FillEllipse(b, x + (w - 8) / 2, y + (h - 8) / 2, 8, 8);
                    }
                    break;

                case TransitionType.FlyThrough:
                    // Concentric frames with 4 outward arrows
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    using (SolidBrush b = new SolidBrush(BlueLight))
                    using (Pen p = new Pen(WhiteColor, 1.2f))
                    {
                        g.FillRectangle(b, x + 7, y + 4, w - 14, h - 8);
                        g.DrawRectangle(p, x + 7, y + 4, w - 14, h - 8);
                    }
                    DrawArrowUp(g, x + w / 2, y + 2, 4, WhiteColor);
                    DrawArrowDown(g, x + w / 2, y + h - 2, 4, WhiteColor);
                    break;

                // ==================== 10 Biểu tượng Độc quyền GenOffice ====================
                case TransitionType.GenOffice_CinematicPrism:
                    // Rainbow prism with RGB dispersion
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(15, 23, 42));
                    Point[] prismPts = new Point[] { new Point(x + 5, y + h - 3), new Point(x + 10, y + 3), new Point(x + 15, y + h - 3) };
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                    using (Pen prPen = new Pen(Color.FromArgb(56, 189, 248), 1.0f))
                    {
                        g.FillPolygon(pb, prismPts);
                        g.DrawPolygon(prPen, prismPts);
                    }
                    using (Pen rPen = new Pen(Color.FromArgb(239, 68, 68), 1.2f))
                    using (Pen gPen = new Pen(Color.FromArgb(34, 197, 94), 1.2f))
                    using (Pen bPen = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
                    {
                        g.DrawLine(rPen, x + 12, y + 6, x + w - 2, y + 4);
                        g.DrawLine(gPen, x + 13, y + 8, x + w - 2, y + 8);
                        g.DrawLine(bPen, x + 14, y + 10, x + w - 2, y + 12);
                    }
                    break;

                case TransitionType.GenOffice_LiquidFluid:
                    // Liquid mercury wave
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(30, 58, 138));
                    using (GraphicsPath wave = new GraphicsPath())
                    {
                        wave.AddBezier(x + 2, y + h - 4, x + 8, y + 2, x + 16, y + h, x + w - 2, y + 5);
                        wave.AddLine(x + w - 2, y + h - 2, x + 2, y + h - 2);
                        wave.CloseFigure();
                        using (SolidBrush wb = new SolidBrush(Color.FromArgb(96, 165, 250)))
                        using (Pen wp = new Pen(Color.White, 1.2f))
                        {
                            g.FillPath(wb, wave);
                            g.DrawPath(wp, wave);
                        }
                    }
                    break;

                case TransitionType.GenOffice_MatrixRain:
                    // Green digital cyber matrix rain
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(2, 44, 34));
                    using (Pen mp = new Pen(Color.FromArgb(74, 222, 128), 1.1f))
                    using (SolidBrush dotB = new SolidBrush(Color.White))
                    {
                        g.DrawLine(mp, x + 5, y + 3, x + 5, y + 12);
                        g.FillRectangle(dotB, x + 4, y + 12, 2, 2);
                        g.DrawLine(mp, x + 11, y + 5, x + 11, y + 15);
                        g.FillRectangle(dotB, x + 10, y + 15, 2, 2);
                        g.DrawLine(mp, x + 17, y + 2, x + 17, y + 10);
                        g.FillRectangle(dotB, x + 16, y + 10, 2, 2);
                        g.DrawLine(mp, x + 23, y + 4, x + 23, y + 14);
                        g.FillRectangle(dotB, x + 22, y + 14, 2, 2);
                    }
                    break;

                case TransitionType.GenOffice_NeonPulse:
                    // Concentric neon pulse shockwave rings
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(30, 27, 75));
                    using (Pen p1 = new Pen(Color.FromArgb(168, 85, 247), 1.2f))
                    using (Pen p2 = new Pen(Color.FromArgb(6, 182, 212), 1.2f))
                    using (SolidBrush cb = new SolidBrush(Color.FromArgb(244, 63, 94)))
                    {
                        g.DrawEllipse(p1, x + 3, y + 2, w - 6, h - 4);
                        g.DrawEllipse(p2, x + 8, y + 4, w - 16, h - 8);
                        g.FillEllipse(cb, x + (w - 4) / 2, y + (h - 4) / 2, 4, 4);
                    }
                    break;

                case TransitionType.GenOffice_PortalWarp:
                    // Cosmic stargate portal
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(15, 23, 42));
                    using (Pen p1 = new Pen(Color.FromArgb(236, 72, 153), 1.4f))
                    using (Pen p2 = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(147, 51, 234)))
                    {
                        g.FillEllipse(pb, x + 5, y + 3, w - 10, h - 6);
                        g.DrawEllipse(p1, x + 5, y + 3, w - 10, h - 6);
                        g.DrawEllipse(p2, x + 9, y + 5, w - 18, h - 10);
                    }
                    break;

                case TransitionType.GenOffice_Supernova:
                    // Exploding cosmic star burst
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(45, 10, 10));
                    DrawStarburst(g, x + w / 2, y + h / 2, 7, Color.FromArgb(245, 158, 11));
                    DrawStarburst(g, x + w / 2, y + h / 2, 4, Color.White);
                    break;

                case TransitionType.GenOffice_HologramGlitch:
                    // Cyberpunk horizontal scanline slices
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(8, 51, 68));
                    using (SolidBrush s1 = new SolidBrush(Color.FromArgb(6, 182, 212)))
                    using (SolidBrush s2 = new SolidBrush(Color.FromArgb(244, 63, 94)))
                    using (SolidBrush s3 = new SolidBrush(Color.FromArgb(250, 204, 21)))
                    {
                        g.FillRectangle(s1, x + 2, y + 3, w - 6, 2);
                        g.FillRectangle(s2, x + 6, y + 7, w - 8, 2);
                        g.FillRectangle(s3, x + 3, y + 11, w - 7, 2);
                    }
                    break;

                case TransitionType.GenOffice_Kaleidoscope:
                    // Stained glass crystal kaleidoscope
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(49, 46, 129));
                    DrawHexagon(g, x + w / 2, y + h / 2, 6, Color.FromArgb(139, 92, 246));
                    DrawStarburst(g, x + w / 2, y + h / 2, 5, Color.FromArgb(236, 72, 153));
                    break;

                case TransitionType.GenOffice_Starlight:
                    // Celestial galaxy swarm
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(15, 23, 42));
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(253, 224, 71)))
                    using (SolidBrush wb = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(sb, x + 5, y + 4, 3, 3);
                        g.FillEllipse(wb, x + 12, y + 2, 2, 2);
                        g.FillEllipse(sb, x + 21, y + 5, 3, 3);
                        g.FillEllipse(wb, x + 8, y + 11, 2, 2);
                        g.FillEllipse(sb, x + 16, y + 12, 3, 3);
                        g.FillEllipse(wb, x + 22, y + 10, 2, 2);
                    }
                    break;

                case TransitionType.GenOffice_GoldenRibbon:
                    // Flowing royal gold waving silk ribbon
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(69, 26, 3));
                    using (GraphicsPath ribbon = new GraphicsPath())
                    {
                        ribbon.AddBezier(x + 2, y + h - 2, x + 8, y + 2, x + 18, y + h, x + w - 2, y + 4);
                        using (Pen ribPen = new Pen(Color.FromArgb(245, 158, 11), 2.2f))
                        using (Pen ribGold = new Pen(Color.FromArgb(253, 230, 138), 1.0f))
                        {
                            g.DrawPath(ribPen, ribbon);
                            g.DrawPath(ribGold, ribbon);
                        }
                    }
                    break;

                case TransitionType.GenOffice_SmoothFlow:
                    // Organic fluid spill
                    DrawSlideFrame(g, x, y, w, h, Color.FromArgb(13, 148, 136));
                    using (GraphicsPath flowPath = new GraphicsPath())
                    {
                        flowPath.AddBezier(x + 2, y + 3, x + 10, y + h - 4, x + 18, y + 2, x + w - 2, y + h - 3);
                        flowPath.AddLine(x + w - 2, y + h - 2, x + 2, y + h - 2);
                        flowPath.CloseFigure();
                        using (SolidBrush fb = new SolidBrush(Color.FromArgb(45, 212, 191)))
                        using (Pen fp = new Pen(Color.White, 1.2f))
                        {
                            g.FillPath(fb, flowPath);
                            g.DrawPath(fp, flowPath);
                        }
                    }
                    break;

                default:
                    DrawSlideFrame(g, x, y, w, h, BluePrimary);
                    break;
            }
        }

        private static void DrawSlideFrame(Graphics g, int x, int y, int w, int h, Color fill)
        {
            using (SolidBrush b = new SolidBrush(fill))
            using (Pen p = new Pen(BorderColor, 1.2f))
            {
                g.FillRectangle(b, x, y, w, h);
                g.DrawRectangle(p, x, y, w, h);
            }
        }

        private static void DrawArrowUp(Graphics g, int cx, int cy, int size, Color col)
        {
            using (Pen p = new Pen(col, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, cx, cy, cx - size / 2, cy + size / 2);
                g.DrawLine(p, cx, cy, cx + size / 2, cy + size / 2);
                g.DrawLine(p, cx, cy, cx, cy + size);
            }
        }

        private static void DrawArrowDown(Graphics g, int cx, int cy, int size, Color col)
        {
            using (Pen p = new Pen(col, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, cx, cy, cx - size / 2, cy - size / 2);
                g.DrawLine(p, cx, cy, cx + size / 2, cy - size / 2);
                g.DrawLine(p, cx, cy, cx, cy - size);
            }
        }

        private static void DrawArrowLeft(Graphics g, int cx, int cy, int size, Color col)
        {
            using (Pen p = new Pen(col, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, cx, cy, cx + size / 2, cy - size / 2);
                g.DrawLine(p, cx, cy, cx + size / 2, cy + size / 2);
                g.DrawLine(p, cx, cy, cx + size, cy);
            }
        }

        private static void DrawArrowRight(Graphics g, int cx, int cy, int size, Color col)
        {
            using (Pen p = new Pen(col, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, cx, cy, cx - size / 2, cy - size / 2);
                g.DrawLine(p, cx, cy, cx - size / 2, cy + size / 2);
                g.DrawLine(p, cx, cy, cx - size, cy);
            }
        }

        private static void DrawStarburst(Graphics g, int cx, int cy, int r, Color col)
        {
            using (Pen p = new Pen(col, 1.3f))
            using (SolidBrush b = new SolidBrush(col))
            {
                g.DrawLine(p, cx - r, cy, cx + r, cy);
                g.DrawLine(p, cx, cy - r, cx, cy + r);
                int d = (int)(r * 0.7f);
                g.DrawLine(p, cx - d, cy - d, cx + d, cy + d);
                g.DrawLine(p, cx - d, cy + d, cx + d, cy - d);
                g.FillEllipse(b, cx - 1, cy - 1, 3, 3);
            }
        }

        private static void DrawHexagon(Graphics g, int cx, int cy, int r, Color col)
        {
            PointF[] hex = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double a = i * Math.PI / 3.0;
                hex[i] = new PointF((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a)));
            }
            using (SolidBrush b = new SolidBrush(col))
            using (Pen p = new Pen(WhiteColor, 1.0f))
            {
                g.FillPolygon(b, hex);
                g.DrawPolygon(p, hex);
            }
        }
    }
}

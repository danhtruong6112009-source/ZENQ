using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class RecentDocItem
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public OfficeAppMode Mode { get; set; }
        public string LastModified { get; set; }

        public RecentDocItem(string title, string path, OfficeAppMode mode, string date)
        {
            Title = title;
            FilePath = path;
            Mode = mode;
            LastModified = date;
        }
    }

    public class HomeView
    {
        public List<RecentDocItem> RecentDocs { get; set; }
        public List<RecentDocItem> TrashDocs { get; set; }
        public int ActiveTab { get; set; } // 0: Tính năng, 1: Tài liệu gần đây, 2: Thùng rác
        public float SmoothScrollY = 0f;
        public float TargetScrollY = 0f;
        public int HoverCardIndex = -1;
        public int HoverRowIndex = -1;
        public int HoverTabIndex = -1;
        public bool HoverHelpBtn = false;

        // Sliding Liquid Glass Pill Animation Properties
        public float IndicatorCurrentX = -1f;
        public float IndicatorTargetX = -1f;
        public float IndicatorCurrentW = -1f;
        public float IndicatorTargetW = -1f;

        public int ScrollY
        {
            get { return (int)Math.Round(SmoothScrollY); }
            set { SmoothScrollY = value; TargetScrollY = value; }
        }

        public bool UpdateSmoothScroll(int windowHeight = 800)
        {
            int maxScroll = GetMaxScroll(windowHeight);
            TargetScrollY = Math.Max(0, Math.Min(maxScroll, TargetScrollY));
            bool isAnimating = false;

            if (Math.Abs(TargetScrollY - SmoothScrollY) > 0.2f)
            {
                SmoothScrollY += (TargetScrollY - SmoothScrollY) * 0.22f;
                isAnimating = true;
            }
            else if (SmoothScrollY != TargetScrollY)
            {
                SmoothScrollY = TargetScrollY;
                isAnimating = true;
            }

            // Animate Sliding Indicator Pill
            if (IndicatorCurrentX > 0 && IndicatorTargetX > 0)
            {
                if (Math.Abs(IndicatorTargetX - IndicatorCurrentX) > 0.3f)
                {
                    IndicatorCurrentX += (IndicatorTargetX - IndicatorCurrentX) * 0.25f;
                    isAnimating = true;
                }
                else
                {
                    IndicatorCurrentX = IndicatorTargetX;
                }
            }

            if (IndicatorCurrentW > 0 && IndicatorTargetW > 0)
            {
                if (Math.Abs(IndicatorTargetW - IndicatorCurrentW) > 0.3f)
                {
                    IndicatorCurrentW += (IndicatorTargetW - IndicatorCurrentW) * 0.25f;
                    isAnimating = true;
                }
                else
                {
                    IndicatorCurrentW = IndicatorTargetW;
                }
            }

            return isAnimating;
        }

        public int GetTotalContentHeight(int width, int windowHeight = 800)
        {
            if (ActiveTab == 0)
            {
                return 160 + (118 + 20) * 2 + 20;
            }
            else
            {
                List<RecentDocItem> currentList = (ActiveTab == 1) ? RecentDocs : TrashDocs;
                return currentList.Count * 64 + 20;
            }
        }

        public int GetMaxScroll(int windowHeight = 800, int width = 1200)
        {
            if (ActiveTab == 0)
            {
                int totalH = 160 + (118 + 20) * 2 + 20;
                return Math.Max(0, totalH - windowHeight);
            }

            List<RecentDocItem> currentList = (ActiveTab == 1) ? RecentDocs : TrashDocs;
            int totalListHeight = currentList.Count * 64 + 16;
            int tableBodyHeight = Math.Max(120, windowHeight - 160 - 55 - 48);
            return Math.Max(0, totalListHeight - tableBodyHeight);
        }

        public RectangleF GetDockCapsuleBounds(int width, int scrollY)
        {
            int marginX = Math.Max(32, (width - 1200) / 2);
            float logoSize = 64f;
            float dockX = marginX + logoSize + 20;
            float dockY = 85 + 11; // Cố định ở đầu trang
            return new RectangleF(dockX, dockY, 586, 42);
        }

        public RectangleF GetHelpBtnBounds(int width, int scrollY)
        {
            int marginX = Math.Max(32, (width - 1200) / 2);
            int contentW = width - marginX * 2;
            float btnSize = 42f;
            float btnX = marginX + contentW - btnSize;
            float btnY = 85 + 11; // Cố định thẳng hàng đối diện 3 tab
            return new RectangleF(btnX, btnY, btnSize, btnSize);
        }

        public RectangleF GetFeaturesTabBounds(int width, int scrollY)
        {
            RectangleF dock = GetDockCapsuleBounds(width, scrollY);
            return new RectangleF(dock.X + 4, dock.Y + 4, 140, 34);
        }

        public RectangleF GetMyDocsTabBounds(int width, int scrollY)
        {
            RectangleF dock = GetDockCapsuleBounds(width, scrollY);
            return new RectangleF(dock.X + 4 + 140 + 4, dock.Y + 4, 195, 34);
        }

        public RectangleF GetTrashTabBounds(int width, int scrollY)
        {
            RectangleF dock = GetDockCapsuleBounds(width, scrollY);
            return new RectangleF(dock.X + 4 + 140 + 4 + 195 + 4, dock.Y + 4, 235, 34);
        }

        public RectangleF GetCardBounds(int index, int width, int windowHeight)
        {
            if (ActiveTab != 0) return new RectangleF(-1000, -1000, 0, 0);

            int marginX = Math.Max(32, (width - 1200) / 2);
            int contentW = width - marginX * 2;
            int cardGap = 20;
            int cardW = (contentW - cardGap * 2) / 3;
            if (cardW < 280) cardW = 280;

            int startY = 160; // Hạ xuống vừa chạm dưới mép Logo không bị đè
            int cardH = 118;  // Kích thước chuẩn gọn gàng thanh thoát

            int col = index % 3;
            int row = index / 3;

            return new RectangleF(marginX + (cardW + cardGap) * col, startY + (cardH + cardGap) * row, cardW, cardH);
        }

        public RectangleF GetTableCardBounds(int width, int windowHeight)
        {
            if (ActiveTab == 0) return new RectangleF(-1000, -1000, 0, 0);

            int marginX = Math.Max(32, (width - 1200) / 2);
            int contentW = width - marginX * 2;
            int tableY = 160; // Hạ xuống dưới mép logo
            int tableH = Math.Max(180, windowHeight - tableY - 55); // Thu ngắn lên 1cm so với đáy màn hình

            return new RectangleF(marginX, tableY, contentW, tableH);
        }

        public HomeView()
        {
            ScrollY = 0;
            ActiveTab = 0;

            RecentDocs = new List<RecentDocItem>();
            TrashDocs = new List<RecentDocItem>();
        }

        #region Vector Graphics Drawing Primitives
        private GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawVectorAppEmblem(Graphics g, float x, float y, float size, OfficeAppMode mode)
        {
            RectangleF box = new RectangleF(x, y, size, size);
            Color baseCol, darkCol;

            if (mode == OfficeAppMode.Writer)
            {
                baseCol = Color.FromArgb(37, 99, 235);
                darkCol = Color.FromArgb(29, 78, 216);
            }
            else if (mode == OfficeAppMode.Calc)
            {
                baseCol = Color.FromArgb(16, 185, 129);
                darkCol = Color.FromArgb(5, 150, 105);
            }
            else if (mode == OfficeAppMode.Slides)
            {
                baseCol = Color.FromArgb(249, 115, 22);
                darkCol = Color.FromArgb(217, 70, 239);
            }
            else // PDF
            {
                baseCol = Color.FromArgb(220, 38, 38);
                darkCol = Color.FromArgb(185, 28, 28);
            }

            using (GraphicsPath bPath = CreateRoundedRectanglePath(box, size * 0.24f))
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(box, baseCol, darkCol, 45f))
            {
                g.FillPath(bgBrush, bPath);
            }

            float pad = size * 0.22f;
            RectangleF inner = new RectangleF(x + pad, y + pad, size - pad * 2, size - pad * 2);

            if (mode == OfficeAppMode.Writer)
            {
                using (GraphicsPath docP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, docP);
                }
                using (SolidBrush foldB = new SolidBrush(Color.FromArgb(219, 234, 254)))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(inner.Right - 8, inner.Top),
                        new PointF(inner.Right, inner.Top + 8),
                        new PointF(inner.Right - 8, inner.Top + 8)
                    });
                    g.FillPath(foldB, foldP);
                }
                using (Pen linePen = new Pen(Color.FromArgb(37, 99, 235), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 9, inner.Right - 11, inner.Top + 9);
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 15, inner.Right - 5, inner.Top + 15);
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 21, inner.Right - 5, inner.Top + 21);
                    g.DrawLine(linePen, inner.Left + 5, inner.Top + 27, inner.Left + (inner.Width * 0.6f), inner.Top + 27);
                }
            }
            else if (mode == OfficeAppMode.Calc)
            {
                using (GraphicsPath sheetP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, sheetP);
                }
                using (SolidBrush thB = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillRectangle(thB, inner.Left, inner.Top, inner.Width, 8);
                }
                using (Pen gridPen = new Pen(Color.FromArgb(167, 243, 208), 1.2f))
                {
                    g.DrawLine(gridPen, inner.Left + inner.Width * 0.48f, inner.Top + 8, inner.Left + inner.Width * 0.48f, inner.Bottom);
                    g.DrawLine(gridPen, inner.Left, inner.Top + 15, inner.Right, inner.Top + 15);
                    g.DrawLine(gridPen, inner.Left, inner.Top + 22, inner.Right, inner.Top + 22);
                    g.DrawLine(gridPen, inner.Left, inner.Top + 29, inner.Right, inner.Top + 29);
                }
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(5, 150, 105)))
                {
                    g.FillRectangle(barB, inner.Left + 4, inner.Bottom - 8, 3, 6);
                    g.FillRectangle(barB, inner.Left + 9, inner.Bottom - 12, 3, 10);
                    g.FillRectangle(barB, inner.Left + 14, inner.Bottom - 15, 3, 13);
                }
            }
            else if (mode == OfficeAppMode.Slides)
            {
                using (GraphicsPath slideP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, slideP);
                }
                RectangleF sInner = new RectangleF(inner.Left + 3, inner.Top + 3, inner.Width - 6, inner.Height - 6);
                using (LinearGradientBrush sBg = new LinearGradientBrush(sInner, Color.FromArgb(254, 243, 199), Color.FromArgb(254, 215, 170), 45f))
                {
                    g.FillRectangle(sBg, sInner);
                }
                using (SolidBrush pieB = new SolidBrush(Color.FromArgb(234, 88, 12)))
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(249, 115, 22)))
                {
                    g.FillPie(pieB, sInner.Left + 3, sInner.Top + 4, 14, 14, 0, 270);
                    g.FillRectangle(barB, sInner.Left + 20, sInner.Top + 6, 8, 3);
                    g.FillRectangle(barB, sInner.Left + 20, sInner.Top + 11, 6, 3);
                }
            }
            else // PDF
            {
                using (GraphicsPath pdfP = CreateRoundedRectanglePath(inner, 3f))
                {
                    g.FillPath(Brushes.White, pdfP);
                }
                using (SolidBrush foldB = new SolidBrush(Color.FromArgb(254, 202, 202)))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(inner.Right - 8, inner.Top),
                        new PointF(inner.Right, inner.Top + 8),
                        new PointF(inner.Right - 8, inner.Top + 8)
                    });
                    g.FillPath(foldB, foldP);
                }
                using (Font pdfFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (SolidBrush pdfTxt = new SolidBrush(Color.FromArgb(220, 38, 38)))
                {
                    g.DrawString("PDF", pdfFont, pdfTxt, inner.Left + 2, inner.Top + 8);
                }
            }
        }

        private void DrawVectorDocumentRowIcon(Graphics g, float x, float y, float w, float h, OfficeAppMode mode)
        {
            RectangleF rect = new RectangleF(x, y, w, h);
            Color col = (mode == OfficeAppMode.Writer) ? Color.FromArgb(37, 99, 235) :
                        ((mode == OfficeAppMode.Calc) ? Color.FromArgb(5, 150, 105) :
                        ((mode == OfficeAppMode.Slides) ? Color.FromArgb(234, 88, 12) : Color.FromArgb(220, 38, 38)));

            using (GraphicsPath page = new GraphicsPath())
            using (SolidBrush pageBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(col, 1.4f))
            {
                page.AddLine(x, y, x + w - 4, y);
                page.AddLine(x + w - 4, y, x + w, y + 4);
                page.AddLine(x + w, y + 4, x + w, y + h);
                page.AddLine(x + w, y + h, x, y + h);
                page.CloseFigure();

                g.FillPath(pageBrush, page);
                g.DrawPath(borderPen, page);

                using (SolidBrush foldB = new SolidBrush(col))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(x + w - 4, y),
                        new PointF(x + w, y + 4),
                        new PointF(x + w - 4, y + 4)
                    });
                    g.FillPath(foldB, foldP);
                }

                using (Pen linePen = new Pen(Color.FromArgb(140, col), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(linePen, x + 3, y + 6, x + w - 5, y + 6);
                    g.DrawLine(linePen, x + 3, y + 9, x + w - 3, y + 9);
                    g.DrawLine(linePen, x + 3, y + 12, x + w * 0.7f, y + 12);
                }
            }
        }

        private void DrawVectorSparkleIcon(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 16f;
            using (SolidBrush b = new SolidBrush(color))
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] {
                    new PointF(x + 8 * s, y + 1 * s),
                    new PointF(x + 10 * s, y + 6 * s),
                    new PointF(x + 15 * s, y + 8 * s),
                    new PointF(x + 10 * s, y + 10 * s),
                    new PointF(x + 8 * s, y + 15 * s),
                    new PointF(x + 6 * s, y + 10 * s),
                    new PointF(x + 1 * s, y + 8 * s),
                    new PointF(x + 6 * s, y + 6 * s)
                });
                g.FillPath(b, p);
            }
        }

        private void DrawVectorFolderIcon(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 24f;
            using (SolidBrush b = new SolidBrush(color))
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] {
                    new PointF(x + 2 * s, y + 4 * s),
                    new PointF(x + 9 * s, y + 4 * s),
                    new PointF(x + 11 * s, y + 7 * s),
                    new PointF(x + 22 * s, y + 7 * s),
                    new PointF(x + 22 * s, y + 20 * s),
                    new PointF(x + 2 * s, y + 20 * s)
                });
                g.FillPath(b, p);

                using (SolidBrush frontB = new SolidBrush(Color.FromArgb(Math.Min(255, color.R + 25), Math.Min(255, color.G + 25), Math.Min(255, color.B + 25))))
                using (GraphicsPath frontP = new GraphicsPath())
                {
                    frontP.AddPolygon(new PointF[] {
                        new PointF(x + 3 * s, y + 9 * s),
                        new PointF(x + 21 * s, y + 9 * s),
                        new PointF(x + 19 * s, y + 19 * s),
                        new PointF(x + 1 * s, y + 19 * s)
                    });
                    g.FillPath(frontB, frontP);
                }
            }
        }

        private void DrawVectorTrashIcon(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 24f;
            using (Pen p = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, x + 4 * s, y + 6 * s, x + 20 * s, y + 6 * s);
                g.DrawLine(p, x + 9 * s, y + 4 * s, x + 15 * s, y + 4 * s);

                PointF[] body = new PointF[] {
                    new PointF(x + 6 * s, y + 6 * s),
                    new PointF(x + 7 * s, y + 19 * s),
                    new PointF(x + 17 * s, y + 19 * s),
                    new PointF(x + 18 * s, y + 6 * s)
                };
                g.DrawLines(p, body);

                g.DrawLine(p, x + 10 * s, y + 9 * s, x + 10 * s, y + 16 * s);
                g.DrawLine(p, x + 14 * s, y + 9 * s, x + 14 * s, y + 16 * s);
            }
        }

        private void DrawVectorRestoreIcon(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 20f;
            using (Pen p = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (SolidBrush b = new SolidBrush(color))
            {
                g.DrawArc(p, x + 3 * s, y + 3 * s, 14 * s, 14 * s, 45, 270);
                PointF[] arr = new PointF[] {
                    new PointF(x + 7 * s, y + 2 * s),
                    new PointF(x + 3 * s, y + 5 * s),
                    new PointF(x + 7 * s, y + 8 * s)
                };
                g.FillPolygon(b, arr);
            }
        }

        private void DrawVectorCrossIcon(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 16f;
            using (Pen p = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, x + 4 * s, y + 4 * s, x + 12 * s, y + 12 * s);
                g.DrawLine(p, x + 12 * s, y + 4 * s, x + 4 * s, y + 12 * s);
            }
        }

        private void DrawVectorArrowRight(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 16f;
            using (Pen p = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.DrawLine(p, x + 3 * s, y + 8 * s, x + 12 * s, y + 8 * s);
                g.DrawLine(p, x + 8 * s, y + 4 * s, x + 12 * s, y + 8 * s);
                g.DrawLine(p, x + 8 * s, y + 12 * s, x + 12 * s, y + 8 * s);
            }
        }

        private void DrawVectorQuestionIcon(Graphics g, float cx, float cy, float size, Color color)
        {
            float s = size / 20f;
            using (Pen p = new Pen(color, 2.2f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (SolidBrush b = new SolidBrush(color))
            {
                g.DrawArc(p, cx - 4.5f * s, cy - 7.5f * s, 9f * s, 8f * s, 180, 200);

                using (GraphicsPath stem = new GraphicsPath())
                {
                    stem.AddBezier(
                        new PointF(cx + 4.5f * s, cy - 3.5f * s),
                        new PointF(cx + 4.0f * s, cy + 0.5f * s),
                        new PointF(cx, cy + 1.0f * s),
                        new PointF(cx, cy + 3.5f * s)
                    );
                    g.DrawPath(p, stem);
                }

                float dotR = 1.4f * s;
                g.FillEllipse(b, cx - dotR, cy + 6.5f * s - dotR, dotR * 2f, dotR * 2f);
            }
        }

        private void ShowHelpDialog()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Giới thiệu & Hướng dẫn - GENOFFICE";
                dlg.Size = new Size(620, 520);
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.Font = new Font("Segoe UI", 9.5f);

                Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24) };
                dlg.Controls.Add(pnl);

                Label lblTitle = new Label
                {
                    Text = "GENOFFICE v3.5",
                    Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    AutoSize = true,
                    Location = new Point(20, 20),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblTitle);

                Label lblSub = new Label
                {
                    Text = "Phát triển bởi NEXTGENEDUSPACE STUDIO • Tác giả: Trương Nguyễn Thế Danh © 2026",
                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    AutoSize = true,
                    Location = new Point(20, 52),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblSub);

                Label lblDesc = new Label
                {
                    Text = "Bộ ứng dụng văn phòng đa năng hiện đại tích hợp đầy đủ:\n" +
                           "• Writer: Soạn thảo văn bản chuyên nghiệp (.docx)\n" +
                           "• Calc: Bảng tính số liệu thông minh (.xlsx, .csv)\n" +
                           "• Slides: Trình chiếu & Thuyết trình chuyên nghiệp (.pptx)\n" +
                           "• PDF: Đọc, chú thích, ký & chỉnh sửa tài liệu PDF\n\n" +
                           "Phím tắt nhanh:\n" +
                           "• Ctrl + N: Tạo tài liệu mới          • Ctrl + O: Mở tài liệu có sẵn\n" +
                           "• Ctrl + S: Lưu tài liệu hiện tại        • Ctrl + F: Tìm kiếm & Thay thế\n" +
                           "• F5: Bắt đầu trình chiếu Slide",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Location = new Point(20, 82),
                    Size = new Size(560, 185),
                    UseMnemonic = false
                };
                pnl.Controls.Add(lblDesc);

                // Khung thông báo lưu ý tương thích định dạng (Disclaimer Box)
                Panel pnlNote = new Panel
                {
                    Location = new Point(20, 275),
                    Size = new Size(560, 125),
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(12)
                };
                pnlNote.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                    using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    using (GraphicsPath gp = CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, pnlNote.Width - 1f, pnlNote.Height - 1f), 8f))
                    {
                        pe.Graphics.DrawPath(bPen, gp);
                    }
                    // Dải màu nhấn bên trái (Accent bar)
                    using (SolidBrush barB = new SolidBrush(Color.FromArgb(245, 158, 11)))
                    {
                        pe.Graphics.FillRectangle(barB, 0, 0, 4, pnlNote.Height);
                    }
                };

                Label lblNoteTitle = new Label
                {
                    Text = "• Lưu ý về tính tương thích định dạng:",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 83, 9),
                    AutoSize = true,
                    Location = new Point(14, 10),
                    UseMnemonic = false
                };
                pnlNote.Controls.Add(lblNoteTitle);

                Label lblNoteContent = new Label
                {
                    Text = "GENOFFICE luôn nỗ lực tối đa để hiển thị và xử lý tài liệu chuẩn xác nhất. Tuy nhiên, do sự khác biệt về đặc tả kỹ thuật và tiêu chuẩn phần mềm giữa các nền tảng, chúng tôi không thể cam kết rằng mọi định dạng nâng cao sẽ hiển thị giống hệt 100% như trên các bộ ứng dụng Office khác.",
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Location = new Point(14, 34),
                    Size = new Size(530, 80),
                    UseMnemonic = false
                };
                pnlNote.Controls.Add(lblNoteContent);

                pnl.Controls.Add(pnlNote);

                Button btnClose = new Button
                {
                    Text = "Đóng",
                    DialogResult = DialogResult.OK,
                    Size = new Size(95, 34),
                    Location = new Point(485, 418),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderSize = 0;
                pnl.Controls.Add(btnClose);
                dlg.AcceptButton = btnClose;

                dlg.ShowDialog();
            }
        }
        #endregion

        #region Main Dashboard Drawing (iOS 26 / VisionOS Liquid Glassmorphism)
        public void Draw(Graphics g, int width, int height)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int maxScroll = GetMaxScroll(height, width);
            TargetScrollY = Math.Max(0, Math.Min(maxScroll, TargetScrollY));
            if (SmoothScrollY > maxScroll) SmoothScrollY = maxScroll;
            if (SmoothScrollY < 0) SmoothScrollY = 0;

            int currentScroll = ScrollY;

            // 1. Toàn bộ nền Dashboard: Liquid Glass Backdrop đa sắc
            LiquidGlassHelper.DrawLiquidGlassBackdrop(g, width, height);

            int marginX = Math.Max(32, (width - 1200) / 2);
            int contentW = width - marginX * 2;
            int currentY = 85;

            // 2. Header: Logo chính thức GENOFFICE
            float logoSize = 64f;
            float logoX = marginX;
            float logoY = currentY;

            GenOfficeLogoRenderer.DrawLogo(g, logoX, logoY, logoSize, true, true);

            // Tab Capsule đặt ngay cạnh Icon với hiệu ứng LIQUID GLASS LƯỚT MƯỢT MÀ
            RectangleF dockRect = GetDockCapsuleBounds(width, 0);
            RectangleF featTab = GetFeaturesTabBounds(width, 0);
            RectangleF myDocsTab = GetMyDocsTabBounds(width, 0);
            RectangleF trashTab = GetTrashTabBounds(width, 0);

            RectangleF currentActiveBounds = (ActiveTab == 0) ? featTab : ((ActiveTab == 1) ? myDocsTab : trashTab);
            IndicatorTargetX = currentActiveBounds.X;
            IndicatorTargetW = currentActiveBounds.Width;

            if (IndicatorCurrentX <= 0)
            {
                IndicatorCurrentX = currentActiveBounds.X;
                IndicatorCurrentW = currentActiveBounds.Width;
            }

            // A. Khung Dock chứa (Outer Liquid Glass Chamber)
            using (GraphicsPath dp = CreateRoundedRectanglePath(dockRect, 21f))
            using (LinearGradientBrush db = new LinearGradientBrush(dockRect, Color.FromArgb(145, 255, 255, 255), Color.FromArgb(95, 240, 245, 255), 90f))
            using (Pen dpPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f))
            {
                g.FillPath(db, dp);
                g.DrawPath(dpPen, dp);
            }

            // B. Nền Hover cho các Tab chưa chọn
            if (HoverTabIndex == 0 && ActiveTab != 0)
            {
                using (GraphicsPath hp = CreateRoundedRectanglePath(featTab, 17f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                    g.FillPath(hb, hp);
            }
            else if (HoverTabIndex == 1 && ActiveTab != 1)
            {
                using (GraphicsPath hp = CreateRoundedRectanglePath(myDocsTab, 17f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                    g.FillPath(hb, hp);
            }
            else if (HoverTabIndex == 2 && ActiveTab != 2)
            {
                using (GraphicsPath hp = CreateRoundedRectanglePath(trashTab, 17f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                    g.FillPath(hb, hp);
            }

            // C. VIÊN THUỐC KÍNH LỎNG LƯỚT MƯỢT MÀ (Sliding Liquid Glass Active Indicator)
            float indX = (IndicatorCurrentX > 0) ? IndicatorCurrentX : currentActiveBounds.X;
            float indW = (IndicatorCurrentW > 0) ? IndicatorCurrentW : currentActiveBounds.Width;
            RectangleF indicatorRect = new RectangleF(indX, dockRect.Y + 4, indW, 34);

            RectangleF indShadow = new RectangleF(indicatorRect.X, indicatorRect.Y + 2f, indicatorRect.Width, indicatorRect.Height);
            using (GraphicsPath sp = CreateRoundedRectanglePath(indShadow, 17f))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(28, 0, 0, 0)))
            {
                g.FillPath(sb, sp);
            }

            using (GraphicsPath ip = CreateRoundedRectanglePath(indicatorRect, 17f))
            using (LinearGradientBrush ib = new LinearGradientBrush(indicatorRect, Color.FromArgb(238, 255, 255, 255), Color.FromArgb(185, 240, 250, 255), 90f))
            using (Pen ipPen = new Pen(Color.FromArgb(245, 255, 255, 255), 1.4f))
            {
                g.FillPath(ib, ip);
                g.DrawPath(ipPen, ip);

                RectangleF glossRect = new RectangleF(indicatorRect.X + 8, indicatorRect.Y + 2, indicatorRect.Width - 16, 12);
                using (GraphicsPath gp = CreateRoundedRectanglePath(glossRect, 6f))
                using (LinearGradientBrush gb = new LinearGradientBrush(glossRect, Color.FromArgb(160, 255, 255, 255), Color.FromArgb(10, 255, 255, 255), 90f))
                {
                    g.FillPath(gb, gp);
                }
            }

            // D. Nhãn chữ và Icon của 3 Tab
            using (Font tabBoldFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (Font tabRegFont = new Font("Segoe UI", 10f, FontStyle.Regular))
            {
                // Tab 0: Tính năng
                bool isTab0 = (ActiveTab == 0);
                Color c0 = isTab0 ? Color.FromArgb(67, 56, 202) : Color.FromArgb(90, 105, 130);
                DrawVectorSparkleIcon(g, featTab.X + 14, featTab.Y + 9, 16, c0);
                using (SolidBrush tb0 = new SolidBrush(c0))
                {
                    g.DrawString("Tính năng", isTab0 ? tabBoldFont : tabRegFont, tb0, featTab.X + 38, featTab.Y + 7);
                }

                // Tab 1: Tài liệu gần đây
                bool isTab1 = (ActiveTab == 1);
                Color c1 = isTab1 ? Color.FromArgb(29, 78, 216) : Color.FromArgb(90, 105, 130);
                DrawVectorFolderIcon(g, myDocsTab.X + 14, myDocsTab.Y + 8, 18, c1);
                using (SolidBrush tb1 = new SolidBrush(c1))
                {
                    g.DrawString("Tài liệu gần đây (" + RecentDocs.Count + ")", isTab1 ? tabBoldFont : tabRegFont, tb1, myDocsTab.X + 38, myDocsTab.Y + 7);
                }

                // Tab 2: Thùng rác
                bool isTab2 = (ActiveTab == 2);
                Color c2 = isTab2 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(90, 105, 130);
                DrawVectorTrashIcon(g, trashTab.X + 14, trashTab.Y + 8, 18, c2);
                using (SolidBrush tb2 = new SolidBrush(c2))
                {
                    g.DrawString("Thùng rác (" + TrashDocs.Count + ") [Tối đa 30 ngày]", isTab2 ? tabBoldFont : tabRegFont, tb2, trashTab.X + 38, trashTab.Y + 7);
                }
            }

            // D. NÚT VECTOR DẤU ? (Help / Trợ giúp & Hướng dẫn) ĐỐI DIỆN 3 TAB
            RectangleF helpRect = GetHelpBtnBounds(width, 0);
            using (GraphicsPath hp = CreateRoundedRectanglePath(helpRect, 21f))
            {
                if (HoverHelpBtn)
                {
                    RectangleF hShadow = new RectangleF(helpRect.X, helpRect.Y + 2f, helpRect.Width, helpRect.Height);
                    using (GraphicsPath hsp = CreateRoundedRectanglePath(hShadow, 21f))
                    using (SolidBrush hsb = new SolidBrush(Color.FromArgb(40, 37, 99, 235)))
                    {
                        g.FillPath(hsb, hsp);
                    }

                    using (LinearGradientBrush hbg = new LinearGradientBrush(helpRect, Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), 90f))
                    using (Pen hBorder = new Pen(Color.FromArgb(147, 197, 253), 1.5f))
                    {
                        g.FillPath(hbg, hp);
                        g.DrawPath(hBorder, hp);
                    }
                    DrawVectorQuestionIcon(g, helpRect.X + helpRect.Width / 2f, helpRect.Y + helpRect.Height / 2f, 19f, Color.White);
                }
                else
                {
                    using (LinearGradientBrush db = new LinearGradientBrush(helpRect, Color.FromArgb(145, 255, 255, 255), Color.FromArgb(95, 240, 245, 255), 90f))
                    using (Pen dpPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f))
                    {
                        g.FillPath(db, hp);
                        g.DrawPath(dpPen, hp);
                    }
                    DrawVectorQuestionIcon(g, helpRect.X + helpRect.Width / 2f, helpRect.Y + helpRect.Height / 2f, 19f, Color.FromArgb(37, 99, 235));
                }
            }

            // 3. Nội dung theo Tab đang chọn
            if (ActiveTab == 0)
            {
                // Tab 0: 4 Thẻ Ứng Dụng Lớn (Writer, Calc, Slides, PDF Editor) vừa vặn màn hình
                RectangleF c0 = GetCardBounds(0, width, height);
                RectangleF c1 = GetCardBounds(1, width, height);
                RectangleF c2 = GetCardBounds(2, width, height);
                RectangleF c3 = GetCardBounds(3, width, height);

                DrawAppCreationCard(g, (int)c0.X, (int)c0.Y, (int)c0.Width, (int)c0.Height,
                    "Writer", "Soạn thảo văn bản",
                    OfficeAppMode.Writer, HoverCardIndex == 0);

                DrawAppCreationCard(g, (int)c1.X, (int)c1.Y, (int)c1.Width, (int)c1.Height,
                    "Calc", "Bảng tính số liệu",
                    OfficeAppMode.Calc, HoverCardIndex == 1);

                DrawAppCreationCard(g, (int)c2.X, (int)c2.Y, (int)c2.Width, (int)c2.Height,
                    "Slides", "Trình chiếu thuyết trình",
                    OfficeAppMode.Slides, HoverCardIndex == 2);

                DrawAppCreationCard(g, (int)c3.X, (int)c3.Y, (int)c3.Width, (int)c3.Height,
                    "PDF Editor", "Chỉnh sửa & Ký PDF",
                    OfficeAppMode.Pdf, HoverCardIndex == 3);
            }
            else
            {
                // Tab 1 hoặc Tab 2: BẢNG CỐ ĐỊNH MỞ RỘNG VỪA VẶN MÀN HÌNH + MỖI FILE LÀ 1 THẺ LIQUID GLASS ĐỘC LẬP
                List<RecentDocItem> currentList = (ActiveTab == 1) ? RecentDocs : TrashDocs;
                RectangleF tableCardRect = GetTableCardBounds(width, height);

                // Khung bảng lớn cố định mở vừa màn hình
                LiquidGlassHelper.DrawLiquidGlassCard(g, tableCardRect, 18f, false, Color.FromArgb(59, 130, 246));

                // Table Header Bar (Cố định ở đỉnh bảng)
                RectangleF thRect = new RectangleF(tableCardRect.X + 2, tableCardRect.Y + 2, tableCardRect.Width - 4, 44);
                using (GraphicsPath thPath = LiquidGlassHelper.CreateRoundedRectanglePath(thRect, 16f))
                using (SolidBrush thBg = new SolidBrush(Color.FromArgb(135, 255, 255, 255)))
                using (Pen thBorder = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                {
                    GraphicsState gs = g.Save();
                    g.SetClip(new RectangleF(tableCardRect.X, tableCardRect.Y, tableCardRect.Width, 44));
                    g.FillPath(thBg, thPath);
                    g.Restore(gs);
                    g.DrawLine(thBorder, tableCardRect.X + 16, tableCardRect.Y + 44, tableCardRect.Right - 16, tableCardRect.Y + 44);
                }

                using (Font thFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush thBrush = new SolidBrush(Color.FromArgb(90, 105, 130)))
                {
                    g.DrawString("TÊN TÀI LIỆU", thFont, thBrush, tableCardRect.X + 28, tableCardRect.Y + 14);
                    g.DrawString("PHÂN HỆ", thFont, thBrush, tableCardRect.Right - 460, tableCardRect.Y + 14);
                    g.DrawString("LẦN SỬA CUỐI", thFont, thBrush, tableCardRect.Right - 330, tableCardRect.Y + 14);
                    g.DrawString("THAO TÁC", thFont, thBrush, tableCardRect.Right - 175, tableCardRect.Y + 14);
                }

                // Vùng Viewport cuộn nội dung danh sách file bên trong bảng
                RectangleF rowsViewport = new RectangleF(tableCardRect.X + 4, tableCardRect.Y + 46, tableCardRect.Width - 8, tableCardRect.Height - 52);

                GraphicsState clipState = g.Save();
                g.SetClip(rowsViewport);

                int rowHeight = 56;
                int rowGap = 8;
                int rowTotalH = rowHeight + rowGap;
                float startY = tableCardRect.Y + 52 - currentScroll;

                if (currentList.Count == 0)
                {
                    using (Font emptyFont = new Font("Segoe UI", 11.5f, FontStyle.Regular))
                    using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(120, 135, 160)))
                    {
                        string emptyMsg = (ActiveTab == 1) ? "Chưa có tài liệu nào gần đây. Hãy chọn tab 'Tính năng' để tạo tài liệu mới!" : "Thùng rác đang trống.";
                        g.DrawString(emptyMsg, emptyFont, emptyBrush, tableCardRect.X + 32, tableCardRect.Y + 80);
                    }
                }
                else
                {
                    using (Font rTitleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                    using (Font rFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (Font badgeFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        for (int i = 0; i < currentList.Count; i++)
                        {
                            RecentDocItem doc = currentList[i];
                            float rowY = startY + i * rowTotalH;

                            // Chỉ vẽ các dòng nằm trong hoặc gần khung nhìn
                            if (rowY + rowHeight < rowsViewport.Top - 50 || rowY > rowsViewport.Bottom + 50) continue;

                            RectangleF rowCard = new RectangleF(tableCardRect.X + 12, rowY, tableCardRect.Width - 24, rowHeight);
                            bool isRowHover = (HoverRowIndex == i);

                            // MỖI FILE LÀ 1 THẺ LIQUID GLASS ĐỘC LẬP
                            Color rowAccent = (doc.Mode == OfficeAppMode.Writer) ? Color.FromArgb(37, 99, 235) :
                                              ((doc.Mode == OfficeAppMode.Calc) ? Color.FromArgb(16, 185, 129) :
                                              ((doc.Mode == OfficeAppMode.Slides) ? Color.FromArgb(249, 115, 22) : Color.FromArgb(220, 38, 38)));

                            // Đổ bóng 3D cho thẻ file
                            RectangleF cardShadow = new RectangleF(rowCard.X, rowCard.Y + (isRowHover ? 3f : 1.5f), rowCard.Width, rowCard.Height);
                            using (GraphicsPath csp = CreateRoundedRectanglePath(cardShadow, 13f))
                            using (SolidBrush csb = new SolidBrush(Color.FromArgb(isRowHover ? 26 : 14, 0, 0, 0)))
                            {
                                g.FillPath(csb, csp);
                            }

                            // Thân Kính lỏng Liquid Glass riêng biệt cho từng File
                            using (GraphicsPath crp = CreateRoundedRectanglePath(rowCard, 13f))
                            using (LinearGradientBrush crb = new LinearGradientBrush(rowCard,
                                isRowHover ? Color.FromArgb(235, 255, 255, 255) : Color.FromArgb(170, 255, 255, 255),
                                isRowHover ? Color.FromArgb(195, 245, 255, 255) : Color.FromArgb(120, 245, 250, 255), 90f))
                            using (Pen crPen = new Pen(isRowHover ? Color.FromArgb(245, 255, 255, 255) : Color.FromArgb(180, 255, 255, 255), isRowHover ? 1.4f : 1.0f))
                            {
                                g.FillPath(crb, crp);
                                g.DrawPath(crPen, crp);
                            }

                            // Vạch màu phân hệ phát sáng ở cạnh trái thẻ file
                            RectangleF leftPill = new RectangleF(rowCard.X + 5, rowCard.Y + 12, 3.5f, rowCard.Height - 24);
                            using (GraphicsPath lpp = CreateRoundedRectanglePath(leftPill, 1.75f))
                            using (SolidBrush lpb = new SolidBrush(rowAccent))
                            {
                                g.FillPath(lpb, lpp);
                            }

                            // Biểu tượng loại tệp Vector
                            DrawVectorDocumentRowIcon(g, rowCard.X + 18, rowCard.Y + 17, 18, 22, doc.Mode);

                            // Tên tài liệu
                            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                            {
                                g.DrawString(doc.Title, rTitleFont, titleBrush, rowCard.X + 46, rowCard.Y + 16);
                            }

                            // Badge Phân hệ (Writer, Calc, Slides, PDF)
                            string modeName = (doc.Mode == OfficeAppMode.Writer) ? "Writer" :
                                              ((doc.Mode == OfficeAppMode.Calc) ? "Calc" :
                                              ((doc.Mode == OfficeAppMode.Slides) ? "Slides" : "PDF"));
                            RectangleF badgeRect = new RectangleF(tableCardRect.Right - 460, rowCard.Y + 15, 68, 26);
                            LiquidGlassHelper.DrawLiquidGlassPill(g, badgeRect, 13f, false, false, rowAccent);
                            using (SolidBrush btxt = new SolidBrush(rowAccent))
                            {
                                g.DrawString(modeName, badgeFont, btxt, badgeRect.X + 13, badgeRect.Y + 4);
                            }

                            // Thời gian sửa đổi
                            using (SolidBrush timeBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                            {
                                g.DrawString(doc.LastModified, rFont, timeBrush, tableCardRect.Right - 330, rowCard.Y + 18);
                            }

                            // Các nút thao tác dạng viên thuốc Kính lỏng nằm gọn gàng bên trong thẻ
                            if (ActiveTab == 1)
                            {
                                RectangleF delBox = new RectangleF(rowCard.Right - 132, rowCard.Y + 13, 116, 30);
                                LiquidGlassHelper.DrawLiquidGlassPill(g, delBox, 15f, false, false, Color.FromArgb(220, 38, 38));
                                DrawVectorTrashIcon(g, delBox.X + 8, delBox.Y + 7, 16, Color.FromArgb(185, 28, 28));
                                using (SolidBrush dbTxt = new SolidBrush(Color.FromArgb(185, 28, 28)))
                                {
                                    g.DrawString("Bỏ thùng rác", rFont, dbTxt, delBox.X + 28, delBox.Y + 6);
                                }
                            }
                            else
                            {
                                RectangleF resBox = new RectangleF(rowCard.Right - 170, rowCard.Y + 13, 86, 30);
                                LiquidGlassHelper.DrawLiquidGlassPill(g, resBox, 15f, false, false, Color.FromArgb(4, 120, 87));
                                DrawVectorRestoreIcon(g, resBox.X + 8, resBox.Y + 7, 16, Color.FromArgb(4, 120, 87));
                                using (SolidBrush rbTxt = new SolidBrush(Color.FromArgb(4, 120, 87)))
                                {
                                    g.DrawString("Phục hồi", rFont, rbTxt, resBox.X + 28, resBox.Y + 6);
                                }

                                RectangleF purgeBox = new RectangleF(rowCard.Right - 76, rowCard.Y + 13, 60, 30);
                                LiquidGlassHelper.DrawLiquidGlassPill(g, purgeBox, 15f, false, false, Color.FromArgb(220, 38, 38));
                                DrawVectorCrossIcon(g, purgeBox.X + 8, purgeBox.Y + 8, 14, Color.FromArgb(220, 38, 38));
                                using (SolidBrush pbTxt = new SolidBrush(Color.FromArgb(220, 38, 38)))
                                {
                                    g.DrawString("Xóa", rFont, pbTxt, purgeBox.X + 24, purgeBox.Y + 6);
                                }
                            }
                        }
                    }
                }

                g.Restore(clipState);

                // Vẽ thanh cuộn kính lỏng mờ (Scrollbar) nếu số file tràn màn hình
                if (maxScroll > 0)
                {
                    float trackH = rowsViewport.Height - 16;
                    float thumbH = Math.Max(30, trackH * (rowsViewport.Height / (float)(currentList.Count * rowTotalH + 16)));
                    float thumbY = rowsViewport.Top + 8 + (currentScroll / (float)maxScroll) * (trackH - thumbH);
                    RectangleF thumbRect = new RectangleF(tableCardRect.Right - 10, thumbY, 5, thumbH);

                    using (GraphicsPath sbp = CreateRoundedRectanglePath(thumbRect, 2.5f))
                    using (SolidBrush sbb = new SolidBrush(Color.FromArgb(80, 100, 120, 150)))
                    {
                        g.FillPath(sbb, sbp);
                    }
                }
            }
        }

        private void DrawAppCreationCard(Graphics g, int x, int y, int w, int h, string name, string subtitle, OfficeAppMode mode, bool isHovered)
        {
            RectangleF cardRect = new RectangleF(x, y, w, h);

            Color accentColor, lightColor;
            if (mode == OfficeAppMode.Writer)
            {
                accentColor = Color.FromArgb(37, 99, 235);
                lightColor = Color.FromArgb(96, 165, 250);
            }
            else if (mode == OfficeAppMode.Calc)
            {
                accentColor = Color.FromArgb(5, 150, 105);
                lightColor = Color.FromArgb(52, 211, 153);
            }
            else if (mode == OfficeAppMode.Slides)
            {
                accentColor = Color.FromArgb(234, 88, 12);
                lightColor = Color.FromArgb(251, 146, 60);
            }
            else // PDF
            {
                accentColor = Color.FromArgb(220, 38, 38);
                lightColor = Color.FromArgb(248, 113, 113);
            }

            LiquidGlassHelper.DrawLiquidGlassCard(g, cardRect, 16f, isHovered, accentColor);

            // Dải ruy băng phát sáng ở đỉnh thẻ
            RectangleF topStrip = new RectangleF(x + 18, y + 6, 42, 4);
            using (GraphicsPath tp = LiquidGlassHelper.CreateRoundedRectanglePath(topStrip, 2f))
            using (LinearGradientBrush tgb = new LinearGradientBrush(topStrip, accentColor, lightColor, 0f))
            {
                g.FillPath(tgb, tp);
            }

            // Khung biểu tượng
            RectangleF emblemBg = new RectangleF(x + 20, y + (h - 50) / 2, 50, 50);
            using (GraphicsPath ep = LiquidGlassHelper.CreateRoundedRectanglePath(emblemBg, 13f))
            using (SolidBrush eb = new SolidBrush(Color.FromArgb(isHovered ? 45 : 25, accentColor.R, accentColor.G, accentColor.B)))
            using (Pen epen = new Pen(Color.FromArgb(isHovered ? 140 : 80, accentColor.R, accentColor.G, accentColor.B), 1.2f))
            {
                g.FillPath(eb, ep);
                g.DrawPath(epen, ep);
            }
            DrawVectorAppEmblem(g, emblemBg.X + 2, emblemBg.Y + 2, 46, mode);

            // Tiêu đề & phụ đề
            using (Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 9.8f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush subBrush = new SolidBrush(accentColor))
            {
                g.DrawString(name, titleFont, titleBrush, x + 82, y + (h - 48) / 2 + 1);
                g.DrawString(subtitle, subFont, subBrush, x + 82, y + (h - 48) / 2 + 26);
            }

            // Nút bấm "Tạo mới →"
            RectangleF actionBtn = new RectangleF(x + w - 108, y + (h - 32) / 2, 94, 32);
            LiquidGlassHelper.DrawLiquidGlassPill(g, actionBtn, 16f, isHovered, isHovered, accentColor);
            using (Font btnFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush abTxt = new SolidBrush(isHovered ? Color.White : accentColor))
            {
                g.DrawString("Tạo mới", btnFont, abTxt, actionBtn.X + 11, actionBtn.Y + 6);
                DrawVectorArrowRight(g, actionBtn.X + 68, actionBtn.Y + 8, 14, isHovered ? Color.White : accentColor);
            }
        }
        #endregion

        #region Interaction Handlers
        public bool HandleMouseMove(int x, int y, int width, int height = 800)
        {
            int prevTab = HoverTabIndex;
            int prevCard = HoverCardIndex;
            int prevRow = HoverRowIndex;
            bool prevHelp = HoverHelpBtn;

            // 1. Check Hover Tabs (Tính năng vs Tài liệu gần đây vs Thùng rác vs Nút ?)
            HoverTabIndex = -1;
            RectangleF featTab = GetFeaturesTabBounds(width, 0);
            RectangleF myDocsTab = GetMyDocsTabBounds(width, 0);
            RectangleF trashTab = GetTrashTabBounds(width, 0);
            RectangleF helpRect = GetHelpBtnBounds(width, 0);

            if (featTab.Contains(x, y)) HoverTabIndex = 0;
            else if (myDocsTab.Contains(x, y)) HoverTabIndex = 1;
            else if (trashTab.Contains(x, y)) HoverTabIndex = 2;

            HoverHelpBtn = helpRect.Contains(x, y);

            // 2. Check Hover Cards (Tab 0)
            HoverCardIndex = -1;
            if (ActiveTab == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    RectangleF cb = GetCardBounds(i, width, height);
                    if (cb.Contains(x, y))
                    {
                        HoverCardIndex = i;
                        break;
                    }
                }
            }

            // 3. Check Hover Rows (Tab 1 & 2)
            HoverRowIndex = -1;
            if (ActiveTab != 0)
            {
                List<RecentDocItem> currentList = (ActiveTab == 1) ? RecentDocs : TrashDocs;
                RectangleF tb = GetTableCardBounds(width, height);
                float rowStartY = tb.Y + 52 - ScrollY;
                int rowTotalH = 56 + 8;

                if (x >= tb.X + 12 && x <= tb.Right - 12 && y >= tb.Y + 46 && y <= tb.Bottom - 6)
                {
                    float relY = y - (tb.Y + 52) + ScrollY;
                    if (relY >= 0)
                    {
                        int r = (int)(relY / rowTotalH);
                        float withinRowY = relY % rowTotalH;
                        if (r >= 0 && r < currentList.Count && withinRowY <= 56)
                        {
                            HoverRowIndex = r;
                        }
                    }
                }
            }

            return (prevCard != HoverCardIndex || prevRow != HoverRowIndex || prevTab != HoverTabIndex || prevHelp != HoverHelpBtn);
        }

        public OfficeAppMode? HandleClick(int x, int y, int width, int height, out string docTitle)
        {
            docTitle = "";
            int currentScroll = ScrollY;

            // Check Click Nút ? (Trợ giúp)
            RectangleF helpRect = GetHelpBtnBounds(width, 0);
            if (helpRect.Contains(x, y))
            {
                ShowHelpDialog();
                return null;
            }

            // 1. Click Tabs (Tính năng vs Tài liệu gần đây vs Thùng rác)
            RectangleF featTab = GetFeaturesTabBounds(width, 0);
            RectangleF myDocsTab = GetMyDocsTabBounds(width, 0);
            RectangleF trashTab = GetTrashTabBounds(width, 0);

            if (featTab.Contains(x, y))
            {
                ActiveTab = 0;
                IndicatorTargetX = featTab.X;
                IndicatorTargetW = featTab.Width;
                ScrollY = 0;
                return null;
            }
            if (myDocsTab.Contains(x, y))
            {
                ActiveTab = 1;
                IndicatorTargetX = myDocsTab.X;
                IndicatorTargetW = myDocsTab.Width;
                ScrollY = 0;
                return null;
            }
            if (trashTab.Contains(x, y))
            {
                ActiveTab = 2;
                IndicatorTargetX = trashTab.X;
                IndicatorTargetW = trashTab.Width;
                ScrollY = 0;
                return null;
            }

            // 2. Click Cards (Khi đang ở Tab 0 - Tính năng)
            if (ActiveTab == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    RectangleF cb = GetCardBounds(i, width, height);
                    if (cb.Contains(x, y))
                    {
                        if (i == 0) { docTitle = "Văn bản Mới.docx"; return OfficeAppMode.Writer; }
                        if (i == 1) { docTitle = "Bảng tính Mới.xlsx"; return OfficeAppMode.Calc; }
                        if (i == 2) { docTitle = "Trình chiếu Mới.pptx"; return OfficeAppMode.Slides; }
                        if (i == 3) { docTitle = "Tài liệu PDF Mới.pdf"; return OfficeAppMode.Pdf; }
                    }
                }
            }
            else
            {
                // 3. Click Document Card or Action Buttons (Khi ở Tab 1 hoặc Tab 2)
                List<RecentDocItem> currentList = (ActiveTab == 1) ? RecentDocs : TrashDocs;
                RectangleF tb = GetTableCardBounds(width, height);
                int rowTotalH = 56 + 8;

                if (x >= tb.X + 12 && x <= tb.Right - 12 && y >= tb.Y + 46 && y <= tb.Bottom - 6)
                {
                    float relY = y - (tb.Y + 52) + currentScroll;
                    if (relY >= 0)
                    {
                        int r = (int)(relY / rowTotalH);
                        float withinRowY = relY % rowTotalH;
                        if (r >= 0 && r < currentList.Count && withinRowY <= 56)
                        {
                            float rowY = tb.Y + 52 + r * rowTotalH - currentScroll;

                            float rowRight = tb.Right - 12;

                            if (ActiveTab == 1)
                            {
                                // Click 🗑 Bỏ thùng rác button (rowRight - 132..rowRight - 16)
                                if (x >= rowRight - 132 && x <= rowRight - 16 && y >= rowY + 13 && y <= rowY + 43)
                                {
                                    RecentDocItem item = RecentDocs[r];
                                    RecentDocs.RemoveAt(r);
                                    TrashDocs.Add(item);
                                    return null;
                                }

                                RecentDocItem doc = RecentDocs[r];
                                docTitle = doc.Title;
                                return doc.Mode;
                            }
                            else
                            {
                                // Click ↺ Phục hồi button (rowRight - 170..rowRight - 84)
                                if (x >= rowRight - 170 && x <= rowRight - 84 && y >= rowY + 13 && y <= rowY + 43)
                                {
                                    RecentDocItem item = TrashDocs[r];
                                    TrashDocs.RemoveAt(r);
                                    RecentDocs.Add(item);
                                    return null;
                                }
                                // Click ❌ Xóa hẳn button (rowRight - 76..rowRight - 16)
                                if (x >= rowRight - 76 && x <= rowRight - 16 && y >= rowY + 13 && y <= rowY + 43)
                                {
                                    TrashDocs.RemoveAt(r);
                                    return null;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
        #endregion
    }
}

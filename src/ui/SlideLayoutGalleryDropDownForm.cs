using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Popup chọn bố cục trang chiếu (Slide Layout Gallery) chuẩn Microsoft PowerPoint.
    /// Hiển thị dạng lưới 3x3 với thumbnail minh họa chi tiết từng bố cục.
    /// </summary>
    public class SlideLayoutGalleryDropDownForm : Form
    {
        public SlideLayoutGalleryDropDownForm(Point screenLocation, SlideLayoutType currentLayout, Action<SlideLayoutType> onLayoutSelected)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(346, 326);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            // Đảm bảo không tràn màn hình
            Rectangle scr = Screen.FromPoint(screenLocation).WorkingArea;
            if (Right > scr.Right) Left = scr.Right - Width - 8;
            if (Bottom > scr.Bottom) Top = scr.Bottom - Height - 8;
            if (Left < scr.Left) Left = scr.Left + 8;
            if (Top < scr.Top) Top = scr.Top + 8;

            SlideLayoutType[] layouts = new SlideLayoutType[]
            {
                SlideLayoutType.TitleSlide,
                SlideLayoutType.TitleAndContent,
                SlideLayoutType.SectionHeader,
                SlideLayoutType.TwoContent,
                SlideLayoutType.Comparison,
                SlideLayoutType.TitleOnly,
                SlideLayoutType.Blank,
                SlideLayoutType.ContentWithCaption,
                SlideLayoutType.PictureWithCaption
            };

            string[] names = new string[]
            {
                "Trang Tiêu đề",
                "Tiêu đề & Nội dung",
                "Tiêu đề Phần",
                "Hai Nội dung",
                "So sánh",
                "Chỉ Tiêu đề",
                "Trang Trống",
                "Nội dung & Chú thích",
                "Ảnh & Chú thích"
            };

            int startX = 10;
            int startY = 36;
            int cardW = 104;
            int cardH = 88;
            int gapX = 6;
            int gapY = 6;

            for (int i = 0; i < layouts.Length; i++)
            {
                int r = i / 3;
                int c = i % 3;
                int x = startX + c * (cardW + gapX);
                int y = startY + r * (cardH + gapY);

                SlideLayoutType lType = layouts[i];
                string lName = names[i];
                bool isSelected = (lType == currentLayout);

                Panel card = new Panel
                {
                    Location = new Point(x, y),
                    Size = new Size(cardW, cardH),
                    BackColor = isSelected ? Color.FromArgb(255, 237, 213) : Color.White,
                    Cursor = Cursors.Hand
                };

                bool isHovered = false;
                card.MouseEnter += (s, e) => { isHovered = true; card.Invalidate(); };
                card.MouseLeave += (s, e) => { isHovered = false; card.Invalidate(); };
                card.Click += (s, e) =>
                {
                    Close();
                    onLayoutSelected?.Invoke(lType);
                };

                card.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.Half;

                    // 1. Viền và nền thẻ
                    if (isSelected)
                    {
                        using (SolidBrush selBg = new SolidBrush(Color.FromArgb(255, 237, 213)))
                        using (Pen selPen = new Pen(Color.FromArgb(210, 71, 38), 1.5f))
                        {
                            g.FillRectangle(selBg, 0, 0, cardW - 1, cardH - 1);
                            g.DrawRectangle(selPen, 0, 0, cardW - 1, cardH - 1);
                        }
                    }
                    else if (isHovered)
                    {
                        using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                        using (Pen hovPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
                        {
                            g.FillRectangle(hovBg, 0, 0, cardW - 1, cardH - 1);
                            g.DrawRectangle(hovPen, 0, 0, cardW - 1, cardH - 1);
                        }
                    }

                    // 2. Thumbnail Slide thu nhỏ (66 x 44 px)
                    int sw = 66;
                    int sh = 44;
                    int sx = (cardW - sw) / 2;
                    int sy = 6;
                    Rectangle slideRect = new Rectangle(sx, sy, sw, sh);

                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    using (Pen slideBorder = new Pen(isSelected ? Color.FromArgb(210, 71, 38) : Color.FromArgb(148, 163, 184), 1.0f))
                    {
                        g.FillRectangle(whiteB, slideRect);
                        g.DrawRectangle(slideBorder, slideRect);
                    }

                    // 3. Vẽ các khung placeholder bên trong thumbnail
                    DrawLayoutThumbnailPlaceholders(g, lType, slideRect);

                    // 4. Tiêu đề bố cục
                    using (Font nameFont = new Font("Segoe UI", 7.6f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush textBrush = new SolidBrush(isSelected ? Color.FromArgb(196, 62, 28) : Color.FromArgb(30, 41, 59)))
                    using (StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    })
                    {
                        Rectangle textRect = new Rectangle(2, sy + sh + 4, cardW - 4, cardH - (sy + sh + 6));
                        g.DrawString(lName, nameFont, textBrush, textRect, sf);
                    }
                };

                Controls.Add(card);
            }

            Deactivate += (s, e) => Close();
        }

        private static void DrawLayoutThumbnailPlaceholders(Graphics g, SlideLayoutType type, Rectangle s)
        {
            using (Pen dashPen = new Pen(Color.FromArgb(100, 116, 139), 0.9f) { DashStyle = DashStyle.Dash })
            using (SolidBrush titleFill = new SolidBrush(Color.FromArgb(224, 231, 255)))
            using (SolidBrush bodyFill = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 0.8f))
            {
                switch (type)
                {
                    case SlideLayoutType.TitleSlide:
                        // Tiêu đề chính giữa slide
                        Rectangle rT = new Rectangle(s.X + 8, s.Y + 11, s.Width - 16, 10);
                        g.FillRectangle(titleFill, rT);
                        g.DrawRectangle(dashPen, rT);
                        // Tiêu đề phụ
                        Rectangle rS = new Rectangle(s.X + 13, s.Y + 25, s.Width - 26, 8);
                        g.FillRectangle(bodyFill, rS);
                        g.DrawRectangle(dashPen, rS);
                        break;

                    case SlideLayoutType.TitleAndContent:
                        // Tiêu đề trên cùng
                        Rectangle rTop = new Rectangle(s.X + 5, s.Y + 4, s.Width - 10, 7);
                        g.FillRectangle(titleFill, rTop);
                        g.DrawRectangle(dashPen, rTop);
                        // Nội dung lớn phía dưới
                        Rectangle rBody = new Rectangle(s.X + 5, s.Y + 14, s.Width - 10, 24);
                        g.FillRectangle(bodyFill, rBody);
                        g.DrawRectangle(dashPen, rBody);
                        // Mô phỏng 3 dòng đầu dòng
                        g.DrawLine(linePen, s.X + 9, s.Y + 19, s.Right - 9, s.Y + 19);
                        g.DrawLine(linePen, s.X + 9, s.Y + 25, s.Right - 9, s.Y + 25);
                        g.DrawLine(linePen, s.X + 9, s.Y + 31, s.Right - 15, s.Y + 31);
                        break;

                    case SlideLayoutType.SectionHeader:
                        Rectangle rSec = new Rectangle(s.X + 6, s.Y + 15, s.Width - 12, 13);
                        g.FillRectangle(titleFill, rSec);
                        g.DrawRectangle(dashPen, rSec);
                        g.DrawLine(linePen, s.X + 10, s.Y + 21, s.Right - 10, s.Y + 21);
                        break;

                    case SlideLayoutType.TwoContent:
                        Rectangle r2T = new Rectangle(s.X + 5, s.Y + 4, s.Width - 10, 7);
                        g.FillRectangle(titleFill, r2T);
                        g.DrawRectangle(dashPen, r2T);
                        // 2 cột nội dung
                        int colW = (s.Width - 13) / 2;
                        Rectangle col1 = new Rectangle(s.X + 5, s.Y + 14, colW, 24);
                        Rectangle col2 = new Rectangle(s.X + 5 + colW + 3, s.Y + 14, colW, 24);
                        g.FillRectangle(bodyFill, col1);
                        g.DrawRectangle(dashPen, col1);
                        g.FillRectangle(bodyFill, col2);
                        g.DrawRectangle(dashPen, col2);
                        g.DrawLine(linePen, col1.X + 3, col1.Y + 5, col1.Right - 3, col1.Y + 5);
                        g.DrawLine(linePen, col1.X + 3, col1.Y + 11, col1.Right - 3, col1.Y + 11);
                        g.DrawLine(linePen, col2.X + 3, col2.Y + 5, col2.Right - 3, col2.Y + 5);
                        g.DrawLine(linePen, col2.X + 3, col2.Y + 11, col2.Right - 3, col2.Y + 11);
                        break;

                    case SlideLayoutType.Comparison:
                        Rectangle rCompT = new Rectangle(s.X + 5, s.Y + 3, s.Width - 10, 6);
                        g.FillRectangle(titleFill, rCompT);
                        g.DrawRectangle(dashPen, rCompT);
                        int cW = (s.Width - 13) / 2;
                        Rectangle h1 = new Rectangle(s.X + 5, s.Y + 11, cW, 5);
                        Rectangle b1 = new Rectangle(s.X + 5, s.Y + 18, cW, 20);
                        Rectangle h2 = new Rectangle(s.X + 5 + cW + 3, s.Y + 11, cW, 5);
                        Rectangle b2 = new Rectangle(s.X + 5 + cW + 3, s.Y + 18, cW, 20);
                        g.FillRectangle(titleFill, h1);
                        g.DrawRectangle(dashPen, h1);
                        g.FillRectangle(bodyFill, b1);
                        g.DrawRectangle(dashPen, b1);
                        g.FillRectangle(titleFill, h2);
                        g.DrawRectangle(dashPen, h2);
                        g.FillRectangle(bodyFill, b2);
                        g.DrawRectangle(dashPen, b2);
                        break;

                    case SlideLayoutType.TitleOnly:
                        Rectangle rOnlyT = new Rectangle(s.X + 5, s.Y + 5, s.Width - 10, 8);
                        g.FillRectangle(titleFill, rOnlyT);
                        g.DrawRectangle(dashPen, rOnlyT);
                        break;

                    case SlideLayoutType.Blank:
                        // Để trắng hoàn toàn
                        break;

                    case SlideLayoutType.ContentWithCaption:
                        Rectangle rCptT = new Rectangle(s.X + 4, s.Y + 6, 20, 6);
                        Rectangle rCptB = new Rectangle(s.X + 4, s.Y + 14, 20, 23);
                        Rectangle rCptC = new Rectangle(s.X + 27, s.Y + 6, s.Width - 31, 31);
                        g.FillRectangle(titleFill, rCptT);
                        g.DrawRectangle(dashPen, rCptT);
                        g.FillRectangle(bodyFill, rCptB);
                        g.DrawRectangle(dashPen, rCptB);
                        g.FillRectangle(bodyFill, rCptC);
                        g.DrawRectangle(dashPen, rCptC);
                        break;

                    case SlideLayoutType.PictureWithCaption:
                        Rectangle rPicBox = new Rectangle(s.X + 6, s.Y + 4, s.Width - 12, 23);
                        g.FillRectangle(bodyFill, rPicBox);
                        g.DrawRectangle(dashPen, rPicBox);
                        // Biểu tượng tranh ảnh nhỏ
                        using (SolidBrush iconB = new SolidBrush(Color.FromArgb(148, 163, 184)))
                        {
                            g.FillEllipse(iconB, rPicBox.X + 6, rPicBox.Y + 4, 4, 4);
                            Point[] mt = new Point[] {
                                new Point(rPicBox.X + 4, rPicBox.Bottom - 3),
                                new Point(rPicBox.X + 13, rPicBox.Y + 11),
                                new Point(rPicBox.X + 22, rPicBox.Bottom - 3)
                            };
                            g.FillPolygon(iconB, mt);
                        }
                        Rectangle rPicT = new Rectangle(s.X + 6, s.Y + 29, s.Width - 12, 5);
                        Rectangle rPicC = new Rectangle(s.X + 6, s.Y + 36, s.Width - 12, 4);
                        g.FillRectangle(titleFill, rPicT);
                        g.DrawRectangle(dashPen, rPicT);
                        g.FillRectangle(bodyFill, rPicC);
                        g.DrawRectangle(dashPen, rPicC);
                        break;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Viền ngoài dropdown tinh tế
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // 2. Tiêu đề Header
            using (Font hFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Bố cục Office (Slide Layouts)", hFont, hBrush, 12, 10);
            }

            // 3. Đường phân cách mảnh
            using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                g.DrawLine(sepPen, 10, 30, Width - 10, 30);
            }
        }
    }
}

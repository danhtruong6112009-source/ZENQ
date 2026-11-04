using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class TransitionsGalleryDropDownForm : Form
    {
        public event Action<TransitionType> OnTransitionSelected;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW: Buttery-soft native Windows drop shadow
                return cp;
            }
        }

        public TransitionsGalleryDropDownForm(Point screenLocation, TransitionType currentTransition)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(768, 540);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            // Clamp screen location to current monitor working area
            Rectangle screenBounds = Screen.FromPoint(screenLocation).WorkingArea;
            if (screenLocation.X + Width > screenBounds.Right)
                screenLocation.X = Math.Max(screenBounds.Left, screenBounds.Right - Width - 8);
            if (screenLocation.Y + Height > screenBounds.Bottom)
                screenLocation.Y = Math.Max(screenBounds.Top, screenBounds.Bottom - Height - 8);
            Location = screenLocation;

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(252, 253, 254),
                Padding = new Padding(14, 10, 14, 14)
            };

            int curY = 12;

            // Section 1: Nhẹ nhàng (Subtle)
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
                Tuple.Create(TransitionType.Uncover, "Lộ ra (Uncover)"),
                Tuple.Create(TransitionType.Cover, "Phủ lên"),
                Tuple.Create(TransitionType.Flash, "Chớp sáng")
            }, true);

            // Section 2: Nổi bật & Nghệ thuật (Exciting)
            AddCategory(scrollPanel, ref curY, "Nổi bật & Nghệ thuật (Exciting)", currentTransition, new Tuple<TransitionType, string>[] {
                Tuple.Create(TransitionType.FallOver, "Đổ xuống"),
                Tuple.Create(TransitionType.Drape, "Rủ màn"),
                Tuple.Create(TransitionType.Curtains, "Mở màn (Curtains)"),
                Tuple.Create(TransitionType.Wind, "Gió cuốn"),
                Tuple.Create(TransitionType.Prestige, "Hào quang"),
                Tuple.Create(TransitionType.Fracture, "Rạn vỡ"),
                Tuple.Create(TransitionType.Crush, "Vò giấy (Crush)"),
                Tuple.Create(TransitionType.PeelOff, "Lột trang"),
                Tuple.Create(TransitionType.PageCurl, "Lật sách"),
                Tuple.Create(TransitionType.Airplane, "Máy bay giấy (Airplane)"),
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
                Tuple.Create(TransitionType.Zoom, "Thu phóng 3D"),
                Tuple.Create(TransitionType.Random, "Ngẫu nhiên")
            }, true);

            // Section 3: Nội dung động (Dynamic Content)
            AddCategory(scrollPanel, ref curY, "Nội dung động (Dynamic Content)", currentTransition, new Tuple<TransitionType, string>[] {
                Tuple.Create(TransitionType.Pan, "Toàn cảnh"),
                Tuple.Create(TransitionType.FerrisWheel, "Vòng quay"),
                Tuple.Create(TransitionType.Conveyor, "Băng chuyền"),
                Tuple.Create(TransitionType.Rotate, "Xoay vòng"),
                Tuple.Create(TransitionType.Window, "Cửa sổ"),
                Tuple.Create(TransitionType.Orbit, "Quỹ đạo"),
                Tuple.Create(TransitionType.FlyThrough, "Bay xuyên")
            }, true);

            // Section 4: Độc quyền ZenQ (ZenQ Exclusive)
            AddCategory(scrollPanel, ref curY, "✦ Độc quyền ZenQ (ZenQ Exclusive)", currentTransition, new Tuple<TransitionType, string>[] {
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
                Tuple.Create(TransitionType.GenOffice_SmoothFlow, "Chảy tràn"),
                Tuple.Create(TransitionType.GenOffice_ColorWipe, "Gạt màu")
            }, false);

            Controls.Add(scrollPanel);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1))
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
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, curY),
                AutoSize = true
            };
            parent.Controls.Add(lblHeader);
            curY += 26;

            int col = 0;
            int rowY = curY;
            int itemW = 76;
            int itemH = 68;
            int colsPerRow = 9;
            int gap = 5;

            for (int i = 0; i < items.Length; i++)
            {
                TransitionType tType = items[i].Item1;
                string tName = items[i].Item2;
                bool isSel = (currentTransition == tType);

                Panel pnl = new Panel
                {
                    Location = new Point(14 + col * (itemW + gap), rowY),
                    Size = new Size(itemW, itemH),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                bool isHovered = false;

                pnl.MouseEnter += (s, e) => { isHovered = true; pnl.Invalidate(); };
                pnl.MouseLeave += (s, e) => { isHovered = false; pnl.Invalidate(); };

                pnl.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    Rectangle tileBounds = new Rectangle(0, 0, itemW - 1, itemH - 1);
                    using (GraphicsPath tilePath = TransitionIconRenderer.GetRoundedRect(tileBounds, 5f))
                    {
                        if (isSel)
                        {
                            using (SolidBrush selBg = new SolidBrush(Color.FromArgb(235, 245, 255)))
                            using (Pen selPen = new Pen(Color.FromArgb(15, 108, 189), 1.6f))
                            {
                                g.FillPath(selBg, tilePath);
                                g.DrawPath(selPen, tilePath);
                            }
                        }
                        else if (isHovered)
                        {
                            using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                            using (Pen hovPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
                            {
                                g.FillPath(hovBg, tilePath);
                                g.DrawPath(hovPen, tilePath);
                            }
                        }
                        else
                        {
                            using (SolidBrush normBg = new SolidBrush(Color.White))
                            using (Pen normPen = new Pen(Color.Transparent, 1.0f))
                            {
                                g.FillPath(normBg, tilePath);
                                g.DrawPath(normPen, tilePath);
                            }
                        }
                    }

                    // Larger slide preview with space for a two-line caption
                    Rectangle iconRect = new Rectangle((itemW - 42) / 2, 5, 42, 30);
                    TransitionIconRenderer.DrawIcon(g, iconRect, tType, isSel);

                    // Crisp Typography with ClearTypeGridFit
                    using (Font f = new Font("Segoe UI", 8f, isSel ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush txtBrush = new SolidBrush(isSel ? Color.FromArgb(15, 108, 189) : Color.FromArgb(30, 41, 59)))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.LineLimit
                        };
                        g.DrawString(tName.Split(new[] { " (" }, StringSplitOptions.None)[0].Replace(" ⭐", ""), f, txtBrush, new RectangleF(2, 38, itemW - 4, 27), sf);
                    }
                };

                pnl.Click += (s, e) =>
                {
                    OnTransitionSelected?.Invoke(tType);
                    Close();
                };

                parent.Controls.Add(pnl);

                col++;
                if (col >= colsPerRow)
                {
                    col = 0;
                    rowY += itemH + gap;
                }
            }

            if (col > 0) rowY += itemH + gap;

            if (addDivider)
            {
                Panel div = new Panel
                {
                    Location = new Point(14, rowY + 8),
                    Size = new Size(colsPerRow * (itemW + gap) - gap, 1),
                    BackColor = Color.FromArgb(226, 232, 240)
                };
                parent.Controls.Add(div);
                curY = rowY + 20;
            }
            else
            {
                curY = rowY + 12;
            }
        }
    }

    /// <summary>
    /// Bộ vẽ đồ họa vector siêu mẫu (Super-Sampled Vector Renderer) chuẩn Microsoft PowerPoint 365
    /// Khử răng cưa tuyệt đối với độ phân giải cao và hạ mẫu Bicubic mượt mà.
    /// </summary>
    public static class TransitionIconRenderer
    {
        // Fluent Design Color Palette
        public static readonly Color FluentBlue = Color.FromArgb(15, 108, 189);      // #0F6CBD
        public static readonly Color FluentBlueDark = Color.FromArgb(17, 94, 163);  // #115EA3
        public static readonly Color FluentBlueLight = Color.FromArgb(96, 165, 250); // #60A5FA
        public static readonly Color FluentBluePale = Color.FromArgb(219, 234, 254); // #DBEAFE
        public static readonly Color FluentBlueIce = Color.FromArgb(239, 246, 255);  // #EFF6FF
        public static readonly Color SlideBorder = Color.FromArgb(203, 213, 225);   // #CBD5E1
        public static readonly Color GoldColor = Color.FromArgb(245, 158, 11);       // #F59E0B
        public static readonly Color EmeraldColor = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color PurpleColor = Color.FromArgb(139, 92, 246);    // #8B5CF6
        public static readonly Color RoseColor = Color.FromArgb(244, 63, 94);       // #F43F5E

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Bitmap> _iconCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, Bitmap>(StringComparer.Ordinal);

        public static GraphicsPath GetRoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawIcon(D2DGraphics g, Rectangle r, TransitionType type, bool isSelected)
        {
            if (g == null || r.Width <= 0 || r.Height <= 0) return;
            Bitmap bmp = GetOrCreateSuperSampledIcon(r.Width, r.Height, type, isSelected);
            if (bmp != null)
            {
                g.DrawImage(bmp, r);
            }
        }

        public static void DrawIcon(Graphics g, Rectangle r, TransitionType type, bool isSelected)
        {
            if (g == null || r.Width <= 0 || r.Height <= 0) return;
            Bitmap bmp = GetOrCreateSuperSampledIcon(r.Width, r.Height, type, isSelected);
            if (bmp != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(bmp, r);
            }
        }

        private static Bitmap GetOrCreateSuperSampledIcon(int targetW, int targetH, TransitionType type, bool isSelected)
        {
            string key = $"{type}_{targetW}_{targetH}_{isSelected}";
            return _iconCache.GetOrAdd(key, _ =>
            {
                using (var stream = typeof(TransitionIconRenderer).Assembly.GetManifestResourceStream("transitions." + type + ".svg"))
                {
                    if (stream != null)
                    {
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            var rendered = SvgRasterizer.RenderSvgToBitmap(reader.ReadToEnd(), targetW, targetH);
                            // The SVG renderer owns its cache; keep an independent bitmap.
                            if (rendered != null) return new Bitmap(rendered);
                        }
                    }
                }
                const int scale = 3; // 3x Supersampling for ultra-crisp Retina vectors
                int canvasW = targetW * scale;
                int canvasH = targetH * scale;

                using (Bitmap hiBmp = new Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (Graphics gHi = Graphics.FromImage(hiBmp))
                    {
                        gHi.Clear(Color.Transparent);
                        gHi.SmoothingMode = SmoothingMode.HighQuality;
                        gHi.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        gHi.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        // Draw in the original design coordinates so fixed details scale
                        // together with the slide silhouette at every target size.
                        float iconScale = Math.Min(canvasW / 114f, canvasH / 72f);
                        gHi.TranslateTransform((canvasW - 114f * iconScale) / 2f,
                            (canvasH - 72f * iconScale) / 2f);
                        gHi.ScaleTransform(iconScale, iconScale);
                        DrawVectorIcon3x(gHi, 114, 72, type, isSelected);
                    }

                    Bitmap finalBmp = new Bitmap(targetW, targetH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (Graphics gFin = Graphics.FromImage(finalBmp))
                    {
                        gFin.Clear(Color.Transparent);
                        gFin.SmoothingMode = SmoothingMode.HighQuality;
                        gFin.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        gFin.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gFin.DrawImage(hiBmp, new Rectangle(0, 0, targetW, targetH));
                    }
                    return finalBmp;
                }
            });
        }

        private static void DrawSlideCardBase(Graphics g, float x, float y, float w, float h, Color? bg = null, Color? border = null)
        {
            RectangleF r = new RectangleF(x, y, w, h);
            using (GraphicsPath p = GetRoundedRect(r, 6f))
            using (SolidBrush b = new SolidBrush(bg ?? Color.White))
            using (Pen pen = new Pen(border ?? SlideBorder, 2.0f))
            {
                g.FillPath(b, p);
                g.DrawPath(pen, p);
            }
        }

        private static void DrawVectorIcon3x(Graphics g, int w, int h, TransitionType type, bool isSelected)
        {
            float m = 3.0f; // Outer margin at 3x
            float sw = w - m * 2;
            float sh = h - m * 2;

            switch (type)
            {
                case TransitionType.None:
                    // Clean blank presentation slide with soft dashed guideline
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White, SlideBorder);
                    using (Pen pDash = new Pen(Color.FromArgb(203, 213, 225), 2.0f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(pDash, m + 14, m + 14, m + sw - 14, m + sh - 14);
                    }
                    break;

                case TransitionType.Morph:
                    // PowerPoint 365 exact: 3 cascading blue squares
                    RectangleF sq1 = new RectangleF(m + 16, m + 10, 24, 24);
                    RectangleF sq2 = new RectangleF(m + 44, m + 22, 28, 28);
                    RectangleF sq3 = new RectangleF(m + 74, m + 32, 32, 32);

                    using (SolidBrush bSq1 = new SolidBrush(FluentBlueDark))
                    using (Pen pSq1 = new Pen(Color.White, 1.5f))
                    using (GraphicsPath pathSq1 = GetRoundedRect(sq1, 4f))
                    {
                        g.FillPath(bSq1, pathSq1);
                        g.DrawPath(pSq1, pathSq1);
                    }

                    using (SolidBrush bSq2 = new SolidBrush(FluentBlue))
                    using (Pen pSq2 = new Pen(Color.White, 1.5f))
                    using (GraphicsPath pathSq2 = GetRoundedRect(sq2, 4f))
                    {
                        g.FillPath(bSq2, pathSq2);
                        g.DrawPath(pSq2, pathSq2);
                    }

                    using (SolidBrush bSq3 = new SolidBrush(FluentBlueLight))
                    using (Pen pSq3 = new Pen(Color.White, 1.5f))
                    using (GraphicsPath pathSq3 = GetRoundedRect(sq3, 4f))
                    {
                        g.FillPath(bSq3, pathSq3);
                        g.DrawPath(pSq3, pathSq3);
                    }
                    break;

                case TransitionType.Fade:
                    // Two cascading slides with smooth alpha transparency
                    DrawSlideCardBase(g, m + 4, m + 4, sw - 20, sh - 16, FluentBluePale, FluentBlueLight);
                    DrawSlideCardBase(g, m + 16, m + 14, sw - 20, sh - 16, Color.White, FluentBlue);
                    using (LinearGradientBrush lgb = new LinearGradientBrush(
                        new RectangleF(m + 16, m + 14, sw - 20, sh - 16),
                        Color.FromArgb(160, FluentBlue),
                        Color.FromArgb(20, FluentBlue),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        using (GraphicsPath gp = GetRoundedRect(new RectangleF(m + 16, m + 14, sw - 20, sh - 16), 6f))
                            g.FillPath(lgb, gp);
                    }
                    break;

                case TransitionType.Push:
                    // Slide split horizontally with smooth upward pushing arrow
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clipPath = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clipPath);
                        using (SolidBrush pushB = new SolidBrush(FluentBlue))
                        {
                            g.FillRectangle(pushB, m, m + sh / 2, sw, sh / 2);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    DrawModernArrow(g, m + sw / 2, m + sh / 2 + 10, m + sw / 2, m + sh / 2 - 14, 14, Color.White);
                    break;

                case TransitionType.Wipe:
                    // Smooth gradient sweep with modern rightward arrow
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        using (LinearGradientBrush lgb = new LinearGradientBrush(
                            new RectangleF(m, m, sw, sh), FluentBlue, Color.FromArgb(0, FluentBlue), 0f))
                        {
                            g.FillRectangle(lgb, m, m, sw, sh);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    DrawModernArrow(g, m + sw / 2 - 12, m + sh / 2, m + sw / 2 + 14, m + sh / 2, 12, Color.White);
                    break;

                case TransitionType.Split:
                    // Center parting with outward left & right arrows
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        using (SolidBrush sb = new SolidBrush(FluentBlue))
                        {
                            g.FillRectangle(sb, m, m, sw / 2 - 2, sh);
                            g.FillRectangle(sb, m + sw / 2 + 2, m, sw / 2, sh);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    DrawModernArrow(g, m + sw / 4 + 8, m + sh / 2, m + sw / 4 - 8, m + sh / 2, 10, Color.White);
                    DrawModernArrow(g, m + sw * 3 / 4 - 8, m + sh / 2, m + sw * 3 / 4 + 8, m + sh / 2, 10, Color.White);
                    break;

                case TransitionType.Reveal:
                    // Back card revealed as front card slides away
                    DrawSlideCardBase(g, m + 4, m + 4, sw - 8, sh - 8, FluentBluePale, FluentBlueLight);
                    DrawSlideCardBase(g, m + 22, m + 10, sw - 24, sh - 14, FluentBlue, SlideBorder);
                    DrawModernArrow(g, m + sw / 2, m + sh / 2, m + sw - 12, m + sh / 2, 12, Color.White);
                    break;

                case TransitionType.Cut:
                    // Sharp diagonal slice with contrasting card faces
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        PointF[] cutPts = new PointF[] {
                            new PointF(m + 16, m),
                            new PointF(m + sw, m + sh - 16),
                            new PointF(m + sw, m + sh),
                            new PointF(m, m + sh),
                            new PointF(m, m + 32)
                        };
                        using (SolidBrush b = new SolidBrush(FluentBlue)) g.FillPolygon(b, cutPts);
                        using (Pen p = new Pen(Color.White, 3f)) g.DrawLine(p, m + 16, m, m + sw, m + sh - 16);
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.RandomBars:
                    // High-aesthetic vertical rhythm bars in Fluent Blues
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        Color[] barCols = new Color[] { FluentBlueDark, FluentBlue, FluentBlueLight, FluentBluePale, FluentBlue };
                        float bw = sw / 5.0f;
                        for (int bi = 0; bi < 5; bi++)
                        {
                            using (SolidBrush bb = new SolidBrush(barCols[bi]))
                                g.FillRectangle(bb, m + bi * bw, m, bw + 0.5f, sh);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Wheel:
                    // Radial circular sector sweep
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    float wheelD = sh - 16;
                    RectangleF wheelR = new RectangleF(m + (sw - wheelD) / 2, m + 8, wheelD, wheelD);
                    using (SolidBrush bgB = new SolidBrush(FluentBluePale))
                    using (SolidBrush swB = new SolidBrush(FluentBlue))
                    using (Pen wp = new Pen(FluentBlueDark, 2f))
                    {
                        g.FillEllipse(bgB, wheelR);
                        g.FillPie(swB, wheelR.X, wheelR.Y, wheelR.Width, wheelR.Height, -90, 160);
                        g.DrawEllipse(wp, wheelR);
                    }
                    break;

                case TransitionType.Shape:
                    // Concentric expanding rounded diamond aperture
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        using (SolidBrush b1 = new SolidBrush(FluentBluePale))
                        using (SolidBrush b2 = new SolidBrush(FluentBlueLight))
                        using (SolidBrush b3 = new SolidBrush(FluentBlue))
                        {
                            g.FillEllipse(b1, m + sw / 2 - 38, m + sh / 2 - 26, 76, 52);
                            g.FillEllipse(b2, m + sw / 2 - 24, m + sh / 2 - 16, 48, 32);
                            g.FillEllipse(b3, m + sw / 2 - 12, m + sh / 2 - 8, 24, 16);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Uncover:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBluePale);
                    DrawSlideCardBase(g, m + 36, m, sw - 36, sh, FluentBlue, SlideBorder);
                    DrawModernArrow(g, m + 50, m + sh / 2, m + 14, m + sh / 2, 12, FluentBlueDark);
                    break;

                case TransitionType.Cover:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    DrawSlideCardBase(g, m + 28, m, sw - 28, sh, FluentBlue, SlideBorder);
                    DrawModernArrow(g, m + sw - 14, m + sh / 2, m + 40, m + sh / 2, 12, Color.White);
                    break;

                case TransitionType.Flash:
                    // Deep Royal blue slide with luminous radiant starburst
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    DrawSparkleStar(g, m + sw / 2, m + sh / 2, 22, Color.White);
                    DrawSparkleStar(g, m + sw / 2, m + sh / 2, 12, FluentBlueLight);
                    break;

                case TransitionType.FallOver:
                    // 3D perspective falling slide with soft drop shadow
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        PointF[] shPts = new PointF[] {
                            new PointF(m + 16, m + sh - 8), new PointF(m + sw - 16, m + sh - 8),
                            new PointF(m + sw - 6, m + sh), new PointF(m + 6, m + sh)
                        };
                        g.FillPolygon(shB, shPts);
                    }
                    PointF[] fallPts = new PointF[] {
                        new PointF(m + 16, m + 8), new PointF(m + sw - 16, m + 8),
                        new PointF(m + sw - 4, m + sh - 6), new PointF(m + 4, m + sh - 6)
                    };
                    using (LinearGradientBrush lgb = new LinearGradientBrush(
                        new RectangleF(m, m, sw, sh), FluentBlueLight, FluentBlueDark, 90f))
                    using (Pen p = new Pen(FluentBlue, 2f))
                    {
                        g.FillPolygon(lgb, fallPts);
                        g.DrawPolygon(p, fallPts);
                    }
                    break;

                case TransitionType.Curtains:
                    // PowerPoint 365 authentic: Royal red velvet theater curtains parting from center
                    using (GraphicsPath stageClip = GetRoundedRect(new RectangleF(m + 4, m + 4, sw - 8, sh - 8), 4f))
                    {
                        using (SolidBrush stageBg = new SolidBrush(Color.FromArgb(20, 20, 25)))
                        {
                            g.FillPath(stageBg, stageClip);
                        }
                        g.SetClip(stageClip);

                        Color redVelvetDark = Color.FromArgb(70, 8, 16);
                        Color redVelvetMid = Color.FromArgb(145, 15, 30);
                        Color redVelvetLight = Color.FromArgb(205, 35, 55);
                        Color goldTrim = Color.FromArgb(235, 190, 50);

                        // Top horizontal pelmet / valance with gold fringe
                        using (LinearGradientBrush pelmetB = new LinearGradientBrush(new RectangleF(m + 4, m + 4, sw - 8, 9), redVelvetLight, redVelvetDark, 90f))
                        {
                            g.FillRectangle(pelmetB, m + 4, m + 4, sw - 8, 9);
                        }
                        using (Pen pGold = new Pen(goldTrim, 1.2f))
                        {
                            g.DrawLine(pGold, m + 4, m + 13, m + sw - 4, m + 13);
                        }

                        // Left curtain drape pulling back towards bottom-left
                        PointF[] leftCurtain = new PointF[] {
                            new PointF(m + 4, m + 13),
                            new PointF(m + sw * 0.46f, m + 13),
                            new PointF(m + sw * 0.22f, m + sh - 4),
                            new PointF(m + 4, m + sh - 4)
                        };
                        using (LinearGradientBrush lCurtB = new LinearGradientBrush(
                            new RectangleF(m + 4, m + 13, sw * 0.46f, sh - 17),
                            redVelvetLight, redVelvetDark, 0f))
                        using (Pen pEdge = new Pen(goldTrim, 1.0f))
                        using (Pen pFold = new Pen(Color.FromArgb(100, redVelvetLight), 1.0f))
                        {
                            g.FillPolygon(lCurtB, leftCurtain);
                            g.DrawLine(pEdge, m + sw * 0.46f, m + 13, m + sw * 0.22f, m + sh - 4);
                            g.DrawLine(pFold, m + sw * 0.2f, m + 13, m + sw * 0.12f, m + sh - 4);
                            g.DrawLine(pFold, m + sw * 0.33f, m + 13, m + sw * 0.17f, m + sh - 4);
                        }

                        // Right curtain drape pulling back towards bottom-right
                        PointF[] rightCurtain = new PointF[] {
                            new PointF(m + sw * 0.54f, m + 13),
                            new PointF(m + sw - 4, m + 13),
                            new PointF(m + sw - 4, m + sh - 4),
                            new PointF(m + sw * 0.78f, m + sh - 4)
                        };
                        using (LinearGradientBrush rCurtB = new LinearGradientBrush(
                            new RectangleF(m + sw * 0.54f, m + 13, sw * 0.46f, sh - 17),
                            redVelvetDark, redVelvetLight, 0f))
                        using (Pen pEdge = new Pen(goldTrim, 1.0f))
                        using (Pen pFold = new Pen(Color.FromArgb(100, redVelvetLight), 1.0f))
                        {
                            g.FillPolygon(rCurtB, rightCurtain);
                            g.DrawLine(pEdge, m + sw * 0.54f, m + 13, m + sw * 0.78f, m + sh - 4);
                            g.DrawLine(pFold, m + sw * 0.8f, m + 13, m + sw * 0.88f, m + sh - 4);
                            g.DrawLine(pFold, m + sw * 0.67f, m + 13, m + sw * 0.83f, m + sh - 4);
                        }

                        g.ResetClip();
                    }
                    break;

                case TransitionType.Wind:
                    // Aerodynamic waving wind streaks
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (Pen wp = new Pen(FluentBlue, 3.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    using (Pen wpLight = new Pen(FluentBlueLight, 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawBezier(wp, m + 10, m + 18, m + 40, m + 8, m + 70, m + 26, m + sw - 12, m + 14);
                        g.DrawBezier(wpLight, m + 18, m + 34, m + 50, m + 24, m + 80, m + 42, m + sw - 8, m + 32);
                        g.DrawBezier(wp, m + 12, m + 50, m + 44, m + 40, m + 74, m + 58, m + sw - 16, m + 48);
                    }
                    break;

                case TransitionType.Prestige:
                    // Majestic arched theater drape
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        using (SolidBrush pb = new SolidBrush(FluentBlueLight))
                        using (Pen p = new Pen(Color.White, 2.5f))
                        {
                            g.FillPie(pb, m + 8, m - 20, sw - 16, sh + 20, 0, 180);
                            g.DrawArc(p, m + 8, m - 20, sw - 16, sh + 20, 0, 180);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Fracture:
                    // Glass fracture cracks with specular highlights
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlue);
                    using (Pen fp = new Pen(Color.White, 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        float cx = m + sw / 2;
                        float cy = m + sh / 2;
                        g.DrawLine(fp, cx, cy, m + 12, m + 10);
                        g.DrawLine(fp, cx, cy, m + sw - 12, m + 8);
                        g.DrawLine(fp, cx, cy, m + 14, m + sh - 8);
                        g.DrawLine(fp, cx, cy, m + sw - 10, m + sh - 10);
                        g.DrawLine(fp, cx, cy, cx + 8, m + sh - 4);
                        g.DrawLine(fp, m + 24, m + 20, m + 36, m + 10);
                    }
                    break;

                case TransitionType.Crush:
                    // Authentic 3D Crumpled Paper Ball
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        float ballCx = m + sw / 2f;
                        float ballCy = m + sh / 2f;
                        float ballR = sh * 0.34f;

                        // Drop shadow
                        using (SolidBrush sBr = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                        {
                            g.FillEllipse(sBr, ballCx - ballR * 0.9f, ballCy + ballR * 0.4f, ballR * 1.8f, ballR * 0.7f);
                        }

                        // Crumpled paper ball perimeter vertices (irregular polygon)
                        int bVerts = 14;
                        PointF[] cPts = new PointF[bVerts];
                        for (int bi = 0; bi < bVerts; bi++)
                        {
                            float bAng = bi * ((float)Math.PI * 2f / bVerts);
                            float bNoise = 1.0f + (float)Math.Sin(bi * 3.7f) * 0.16f + (float)Math.Cos(bi * 5.2f) * 0.12f;
                            cPts[bi] = new PointF(ballCx + (float)Math.Cos(bAng) * ballR * bNoise, ballCy + (float)Math.Sin(bAng) * ballR * bNoise);
                        }

                        using (GraphicsPath bPath = new GraphicsPath())
                        {
                            bPath.AddPolygon(cPts);
                            using (SolidBrush bBg = new SolidBrush(Color.FromArgb(245, 246, 250)))
                            {
                                g.FillPath(bBg, bPath);
                            }

                            // Triangular facet shading
                            for (int bi = 0; bi < bVerts; bi++)
                            {
                                int nxt = (bi + 1) % bVerts;
                                PointF[] tri = new PointF[] { new PointF(ballCx, ballCy), cPts[bi], cPts[nxt] };
                                float mAng = (bi + 0.5f) * ((float)Math.PI * 2f / bVerts);
                                float lt = (float)Math.Cos(mAng - (float)Math.PI * 1.3f);
                                Color fCol = (lt > 0)
                                    ? Color.FromArgb((int)(lt * 80), 255, 255, 255)
                                    : Color.FromArgb((int)(Math.Abs(lt) * 110), 160, 168, 185);
                                using (SolidBrush fBr = new SolidBrush(fCol))
                                {
                                    g.FillPolygon(fBr, tri);
                                }
                            }

                            // Crease lines
                            using (Pen dPen = new Pen(Color.FromArgb(140, 150, 165), 1.0f))
                            using (Pen lPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.0f))
                            {
                                for (int bi = 0; bi < bVerts; bi += 2)
                                {
                                    g.DrawLine(dPen, ballCx, ballCy, cPts[bi].X, cPts[bi].Y);
                                    int opp = (bi + bVerts / 2) % bVerts;
                                    g.DrawLine(lPen, cPts[bi].X, cPts[bi].Y, cPts[opp].X, cPts[opp].Y);
                                }
                            }

                            using (Pen edgeP = new Pen(Color.FromArgb(160, 170, 185), 1.0f))
                            {
                                g.DrawPolygon(edgeP, cPts);
                            }
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.PeelOff:
                    // PowerPoint 365 exact: Blue slide card with top-right corner peeled over diagonally
                    float peelX = m + sw - 34;
                    float peelY = m + 34;

                    // Base slide card (Fluent Blue)
                    PointF[] slideShape = new PointF[] {
                        new PointF(m + 8, m + 8),
                        new PointF(peelX, m + 8),
                        new PointF(m + sw - 8, peelY),
                        new PointF(m + sw - 8, m + sh - 8),
                        new PointF(m + 8, m + sh - 8)
                    };
                    using (SolidBrush bSlide = new SolidBrush(FluentBlue))
                    using (Pen pSlide = new Pen(FluentBlueDark, 1.2f))
                    {
                        g.FillPolygon(bSlide, slideShape);
                        g.DrawPolygon(pSlide, slideShape);
                    }

                    // Shadow underneath the peeled flap
                    PointF[] flapShadow = new PointF[] {
                        new PointF(peelX, m + 8),
                        new PointF(m + sw - 8, peelY),
                        new PointF(peelX - 4, peelY + 4)
                    };
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                    {
                        g.FillPolygon(shB, flapShadow);
                    }

                    // Peeled corner flap (folded over showing the back of page)
                    PointF[] peeledFlap = new PointF[] {
                        new PointF(peelX, m + 8),
                        new PointF(m + sw - 8, peelY),
                        new PointF(peelX, peelY)
                    };
                    using (SolidBrush bFlap = new SolidBrush(FluentBlueIce))
                    using (Pen pFlap = new Pen(SlideBorder, 1.5f))
                    {
                        g.FillPolygon(bFlap, peeledFlap);
                        g.DrawPolygon(pFlap, peeledFlap);
                    }
                    break;

                case TransitionType.PageCurl:
                    // PowerPoint 365 exact: Flat blue slide page with 3D cylindrical roll on right edge
                    float curlW = 28f;
                    float flatW = sw - 16 - curlW;

                    // Flat page part
                    using (SolidBrush bFlat = new SolidBrush(FluentBlue))
                    using (Pen pBorder = new Pen(FluentBlueDark, 1.2f))
                    {
                        g.FillRectangle(bFlat, m + 8, m + 8, flatW, sh - 16);
                        g.DrawRectangle(pBorder, m + 8, m + 8, flatW, sh - 16);
                    }

                    // 3D cylindrical curl roll on right
                    RectangleF curlRect = new RectangleF(m + 8 + flatW - 4, m + 8, curlW, sh - 16);
                    using (LinearGradientBrush rollB = new LinearGradientBrush(
                        curlRect, FluentBlueDark, FluentBlueIce, 0f))
                    {
                        ColorBlend cb = new ColorBlend(3);
                        cb.Colors = new Color[] { FluentBlueDark, Color.White, FluentBlue };
                        cb.Positions = new float[] { 0.0f, 0.45f, 1.0f };
                        rollB.InterpolationColors = cb;

                        using (GraphicsPath rollPath = new GraphicsPath())
                        {
                            rollPath.AddArc(curlRect.X, curlRect.Y, curlRect.Width, 8, 180, 180);
                            rollPath.AddLine(curlRect.Right, curlRect.Y + 4, curlRect.Right, curlRect.Bottom - 4);
                            rollPath.AddArc(curlRect.X, curlRect.Bottom - 8, curlRect.Width, 8, 0, 180);
                            rollPath.CloseFigure();

                            g.FillPath(rollB, rollPath);
                            using (Pen pRoll = new Pen(FluentBlueLight, 1.2f))
                            {
                                g.DrawPath(pRoll, rollPath);
                            }
                        }
                    }
                    break;

                case TransitionType.Airplane:
                    // PowerPoint 365 exact: Standalone origami paper airplane flying up-right
                    PointF nose = new PointF(m + sw - 14, m + 12);
                    PointF leftWingTip = new PointF(m + 16, m + 26);
                    PointF rightWingTip = new PointF(m + 42, m + sh - 10);
                    PointF keelTail = new PointF(m + 18, m + sh - 14);
                    PointF tailCenter = new PointF(m + 32, m + 44);

                    // Upper wing (Light sky blue)
                    PointF[] upperWing = new PointF[] { nose, leftWingTip, tailCenter };
                    using (SolidBrush bUpper = new SolidBrush(FluentBlueLight))
                    using (Pen pBorder = new Pen(FluentBlueDark, 1.2f))
                    {
                        g.FillPolygon(bUpper, upperWing);
                        g.DrawPolygon(pBorder, upperWing);
                    }

                    // Lower wing (Medium royal blue)
                    PointF[] lowerWing = new PointF[] { nose, tailCenter, rightWingTip };
                    using (SolidBrush bLower = new SolidBrush(FluentBlue))
                    using (Pen pBorder = new Pen(FluentBlueDark, 1.2f))
                    {
                        g.FillPolygon(bLower, lowerWing);
                        g.DrawPolygon(pBorder, lowerWing);
                    }

                    // Center keel underbody (Deep navy shadow)
                    PointF[] keel = new PointF[] { keelTail, tailCenter, nose };
                    using (SolidBrush bKeel = new SolidBrush(FluentBlueDark))
                    using (Pen pBorder = new Pen(Color.FromArgb(15, 23, 42), 1.0f))
                    {
                        g.FillPolygon(bKeel, keel);
                        g.DrawPolygon(pBorder, keel);
                    }

                    // Sleek central spine highlight
                    using (Pen pSpine = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(pSpine, tailCenter, nose);
                    }
                    break;

                case TransitionType.Origami:
                    // Clean geometric folded crane
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (SolidBrush b1 = new SolidBrush(FluentBlue))
                    using (SolidBrush b2 = new SolidBrush(FluentBlueLight))
                    using (SolidBrush b3 = new SolidBrush(FluentBlueDark))
                    {
                        PointF[] f1 = new PointF[] { new PointF(m + 22, m + 50), new PointF(m + 58, m + 14), new PointF(m + 68, m + 50) };
                        PointF[] f2 = new PointF[] { new PointF(m + 58, m + 14), new PointF(m + 92, m + 28), new PointF(m + 68, m + 50) };
                        PointF[] f3 = new PointF[] { new PointF(m + 58, m + 14), new PointF(m + 34, m + 20), new PointF(m + 22, m + 50) };
                        g.FillPolygon(b1, f1);
                        g.FillPolygon(b2, f2);
                        g.FillPolygon(b3, f3);
                    }
                    break;

                case TransitionType.Dissolve:
                    // Frosted pixel dissolve pattern
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        using (SolidBrush db = new SolidBrush(FluentBlue))
                        {
                            float pw = sw / 10.0f;
                            float ph = sh / 6.0f;
                            for (int ri = 0; ri < 6; ri++)
                            {
                                for (int ci = 0; ci < 10; ci++)
                                {
                                    if ((ri * 3 + ci * 7) % 3 == 0)
                                        g.FillRectangle(db, m + ci * pw, m + ri * ph, pw, ph);
                                }
                            }
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Checker:
                    // Crisp checkerboard pattern
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        float cw = sw / 6.0f;
                        float ch = sh / 4.0f;
                        for (int ri = 0; ri < 4; ri++)
                        {
                            for (int ci = 0; ci < 6; ci++)
                            {
                                Color col = ((ri + ci) % 2 == 0) ? FluentBlue : FluentBlueIce;
                                using (SolidBrush cb = new SolidBrush(col))
                                    g.FillRectangle(cb, m + ci * cw, m + ri * ch, cw + 0.5f, ch + 0.5f);
                            }
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Blinds:
                    // Venetian blinds with subtle slats
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        float slatH = sh / 5.0f;
                        for (int i = 0; i < 5; i++)
                        {
                            using (SolidBrush sb = new SolidBrush(FluentBlue))
                                g.FillRectangle(sb, m, m + i * slatH, sw, slatH - 2.0f);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Clock:
                    // Clock face with sector sweep
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBluePale);
                    using (SolidBrush b = new SolidBrush(FluentBlue))
                    using (Pen p = new Pen(Color.White, 2.5f))
                    {
                        float d = sh - 16;
                        g.FillPie(b, m + (sw - d) / 2, m + 8, d, d, -90, 140);
                        g.DrawLine(p, m + sw / 2, m + sh / 2, m + sw / 2, m + 14);
                        g.DrawLine(p, m + sw / 2, m + sh / 2, m + sw / 2 + 14, m + sh / 2 + 8);
                    }
                    break;

                case TransitionType.Ripple:
                    // Anti-aliased concentric water ripple waves
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    using (Pen rp1 = new Pen(Color.FromArgb(120, Color.White), 3.0f))
                    using (Pen rp2 = new Pen(Color.FromArgb(200, Color.White), 2.5f))
                    using (Pen rp3 = new Pen(Color.White, 2.0f))
                    {
                        float cx = m + sw / 2;
                        float cy = m + sh / 2;
                        g.DrawEllipse(rp1, cx - 36, cy - 22, 72, 44);
                        g.DrawEllipse(rp2, cx - 22, cy - 14, 44, 28);
                        g.DrawEllipse(rp3, cx - 10, cy - 6, 20, 12);
                    }
                    break;

                case TransitionType.Honeycomb:
                    // Crisp hexagonal honeycomb grid
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBluePale);
                    float hr = 10f;
                    DrawSmoothHexagon(g, m + sw / 2 - 20, m + sh / 2 - 10, hr, FluentBlue);
                    DrawSmoothHexagon(g, m + sw / 2, m + sh / 2 - 10, hr, FluentBlueLight);
                    DrawSmoothHexagon(g, m + sw / 2 + 20, m + sh / 2 - 10, hr, FluentBlue);
                    DrawSmoothHexagon(g, m + sw / 2 - 10, m + sh / 2 + 10, hr, FluentBlueLight);
                    DrawSmoothHexagon(g, m + sw / 2 + 10, m + sh / 2 + 10, hr, FluentBlue);
                    break;

                case TransitionType.Glitter:
                    // Twinkling glitter stars and diamond sparkles
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    DrawSparkleStar(g, m + 26, m + 20, 12, Color.White);
                    DrawSparkleStar(g, m + sw - 28, m + 22, 14, GoldColor);
                    DrawSparkleStar(g, m + sw / 2, m + sh - 22, 16, Color.White);
                    DrawSparkleStar(g, m + 36, m + sh - 18, 9, FluentBlueLight);
                    break;

                case TransitionType.Vortex:
                    // Swirling particle vortex
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    using (SolidBrush vb = new SolidBrush(Color.White))
                    using (Pen vp = new Pen(FluentBlueLight, 2f))
                    {
                        float cx = m + sw / 2;
                        float cy = m + sh / 2;
                        g.DrawArc(vp, cx - 26, cy - 16, 52, 32, 0, 240);
                        g.DrawArc(vp, cx - 14, cy - 9, 28, 18, 180, 240);
                        g.FillEllipse(vb, cx - 3, cy - 3, 6, 6);
                    }
                    break;

                case TransitionType.Shred:
                    // Shredded ribbons falling vertically
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        float rw = sw / 6.0f;
                        for (int i = 0; i < 6; i++)
                        {
                            float off = (i % 2 == 0) ? 6 : 16;
                            using (SolidBrush sb = new SolidBrush(FluentBlue))
                                g.FillRectangle(sb, m + i * rw, m + off, rw - 2, sh - 20);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Switch:
                    // Two 3D perspective playing-card slides swapping
                    PointF[] card1 = new PointF[] { new PointF(m + 10, m + 14), new PointF(m + 48, m + 6), new PointF(m + 48, m + sh - 8), new PointF(m + 10, m + sh) };
                    PointF[] card2 = new PointF[] { new PointF(m + 56, m + 6), new PointF(m + sw - 10, m + 14), new PointF(m + sw - 10, m + sh), new PointF(m + 56, m + sh - 8) };
                    using (SolidBrush b1 = new SolidBrush(FluentBlueLight))
                    using (SolidBrush b2 = new SolidBrush(FluentBlue))
                    using (Pen p = new Pen(Color.White, 2f))
                    {
                        g.FillPolygon(b1, card1);
                        g.FillPolygon(b2, card2);
                        g.DrawPolygon(p, card1);
                        g.DrawPolygon(p, card2);
                    }
                    break;

                case TransitionType.Flip:
                    // 3D slide flipping on vertical axis
                    DrawSlideCardBase(g, m + sw / 4, m + 4, sw / 2, sh - 8, FluentBlue);
                    using (Pen p = new Pen(Color.White, 2.5f))
                    {
                        g.DrawLine(p, m + sw / 2, m + 4, m + sw / 2, m + sh - 4);
                    }
                    break;

                case TransitionType.Gallery:
                    // 3D perspective gallery carousel
                    PointF[] galPts = new PointF[] {
                        new PointF(m + 12, m + 10), new PointF(m + sw - 12, m + 10),
                        new PointF(m + sw - 4, m + sh - 10), new PointF(m + 4, m + sh - 10)
                    };
                    using (SolidBrush b = new SolidBrush(FluentBlue))
                    using (Pen p = new Pen(SlideBorder, 2f))
                    {
                        g.FillPolygon(b, galPts);
                        g.DrawPolygon(p, galPts);
                    }
                    break;

                case TransitionType.Cube:
                    // 3D isometric cube
                    int ccx = (int)(m + sw / 2);
                    PointF[] topF = new PointF[] { new PointF(ccx, m + 8), new PointF(ccx + 30, m + 22), new PointF(ccx, m + 36), new PointF(ccx - 30, m + 22) };
                    PointF[] leftF = new PointF[] { new PointF(ccx - 30, m + 22), new PointF(ccx, m + 36), new PointF(ccx, m + sh - 8), new PointF(ccx - 30, m + sh - 22) };
                    PointF[] rightF = new PointF[] { new PointF(ccx, m + 36), new PointF(ccx + 30, m + 22), new PointF(ccx + 30, m + sh - 22), new PointF(ccx, m + sh - 8) };
                    using (SolidBrush bTop = new SolidBrush(FluentBlueLight))
                    using (SolidBrush bLeft = new SolidBrush(FluentBlue))
                    using (SolidBrush bRight = new SolidBrush(FluentBlueDark))
                    using (Pen p = new Pen(Color.White, 2f))
                    {
                        g.FillPolygon(bTop, topF);
                        g.FillPolygon(bLeft, leftF);
                        g.FillPolygon(bRight, rightF);
                        g.DrawPolygon(p, topF);
                        g.DrawPolygon(p, leftF);
                        g.DrawPolygon(p, rightF);
                    }
                    break;

                case TransitionType.Doors:
                    // Double French doors opening in perspective
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(15, 23, 42));
                    PointF[] d1 = new PointF[] { new PointF(m + 4, m + 4), new PointF(m + sw * 0.42f, m + 12), new PointF(m + sw * 0.42f, m + sh - 12), new PointF(m + 4, m + sh - 4) };
                    PointF[] d2 = new PointF[] { new PointF(m + sw - 4, m + 4), new PointF(m + sw * 0.58f, m + 12), new PointF(m + sw * 0.58f, m + sh - 12), new PointF(m + sw - 4, m + sh - 4) };
                    using (SolidBrush db = new SolidBrush(FluentBlue))
                    using (Pen dp = new Pen(Color.White, 2f))
                    {
                        g.FillPolygon(db, d1);
                        g.FillPolygon(db, d2);
                        g.DrawPolygon(dp, d1);
                        g.DrawPolygon(dp, d2);
                    }
                    break;

                case TransitionType.Box:
                    // 3D box interior perspective
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlue);
                    RectangleF innerBox = new RectangleF(m + 22, m + 14, sw - 44, sh - 28);
                    using (SolidBrush ib = new SolidBrush(FluentBlueDark))
                    using (Pen p = new Pen(Color.White, 2f))
                    {
                        g.FillRectangle(ib, innerBox);
                        g.DrawRectangle(p, innerBox.X, innerBox.Y, innerBox.Width, innerBox.Height);
                        g.DrawLine(p, m, m, innerBox.Left, innerBox.Top);
                        g.DrawLine(p, m + sw, m, innerBox.Right, innerBox.Top);
                        g.DrawLine(p, m, m + sh, innerBox.Left, innerBox.Bottom);
                        g.DrawLine(p, m + sw, m + sh, innerBox.Right, innerBox.Bottom);
                    }
                    break;

                case TransitionType.Comb:
                    // Interlocking horizontal comb teeth
                    DrawSlideCardBase(g, m, m, sw, sh, Color.White);
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        float th = sh / 4.0f;
                        for (int i = 0; i < 4; i++)
                        {
                            Color c = (i % 2 == 0) ? FluentBlue : FluentBlueLight;
                            float tw = (i % 2 == 0) ? (sw - 20) : (sw - 36);
                            float tx = (i % 2 == 0) ? m : (m + 36);
                            using (SolidBrush b = new SolidBrush(c))
                                g.FillRectangle(b, tx, m + i * th, tw, th - 2);
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.Zoom:
                    // Concentric perspective zoom frames
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBluePale);
                    RectangleF zoomInner = new RectangleF(m + 24, m + 16, sw - 48, sh - 32);
                    DrawSlideCardBase(g, zoomInner.X, zoomInner.Y, zoomInner.Width, zoomInner.Height, FluentBlue);
                    DrawModernArrow(g, m + sw / 2 - 16, m + sh / 2, m + 12, m + sh / 2, 10, FluentBlueDark);
                    DrawModernArrow(g, m + sw / 2 + 16, m + sh / 2, m + sw - 12, m + sh / 2, 10, FluentBlueDark);
                    break;

                case TransitionType.Pan:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlue);
                    DrawModernArrow(g, m + sw / 2, m + sh - 14, m + sw / 2, m + 14, 14, Color.White);
                    break;

                case TransitionType.FerrisWheel:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    using (Pen p = new Pen(Color.White, 2.5f))
                    using (SolidBrush b = new SolidBrush(FluentBlueLight))
                    {
                        g.DrawArc(p, m + 10, m + 10, sw - 20, (sh - 20) * 2, 180, 180);
                        g.FillEllipse(b, m + 18, m + 16, 12, 12);
                        g.FillEllipse(b, m + sw / 2 - 6, m + 6, 12, 12);
                        g.FillEllipse(b, m + sw - 30, m + 16, 12, 12);
                    }
                    break;

                case TransitionType.Conveyor:
                    using (SolidBrush bTrack = new SolidBrush(FluentBlue))
                    using (SolidBrush bBox = new SolidBrush(FluentBlueLight))
                    using (Pen p = new Pen(Color.White, 2f))
                    {
                        PointF[] track = new PointF[] { new PointF(m + 4, m + sh - 16), new PointF(m + sw * 0.45f, m + 14), new PointF(m + sw - 4, m + 28), new PointF(m + sw * 0.45f, m + sh) };
                        g.FillPolygon(bTrack, track);
                        g.DrawPolygon(p, track);
                        RectangleF boxR = new RectangleF(m + sw * 0.35f, m + 12, 26, 26);
                        g.FillRectangle(bBox, boxR);
                        g.DrawRectangle(p, boxR.X, boxR.Y, boxR.Width, boxR.Height);
                    }
                    break;

                case TransitionType.Rotate:
                    DrawSlideCardBase(g, m + 14, m + 8, sw - 28, sh - 16, FluentBlue);
                    using (Pen p = new Pen(GoldColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawArc(p, m + 6, m + 4, sw - 12, sh - 8, 45, 270);
                    }
                    break;

                case TransitionType.Window:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    using (SolidBrush b = new SolidBrush(FluentBlueLight))
                    using (Pen p = new Pen(Color.White, 3f))
                    {
                        float paneW = (sw - 20) / 2;
                        float paneH = (sh - 20) / 2;
                        g.FillRectangle(b, m + 6, m + 6, paneW, paneH);
                        g.FillRectangle(b, m + 14 + paneW, m + 6, paneW, paneH);
                        g.FillRectangle(b, m + 6, m + 14 + paneH, paneW, paneH);
                        g.FillRectangle(b, m + 14 + paneW, m + 14 + paneH, paneW, paneH);
                        g.DrawLine(p, m + sw / 2, m, m + sw / 2, m + sh);
                        g.DrawLine(p, m, m + sh / 2, m + sw, m + sh / 2);
                    }
                    break;

                case TransitionType.Orbit:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlueDark);
                    using (Pen p = new Pen(Color.White, 2.5f))
                    using (SolidBrush b = new SolidBrush(FluentBlueLight))
                    {
                        g.DrawEllipse(p, m + 10, m + 12, sw - 20, sh - 24);
                        g.FillEllipse(b, m + sw / 2 - 14, m + sh / 2 - 14, 28, 28);
                    }
                    break;

                case TransitionType.FlyThrough:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlue);
                    DrawSlideCardBase(g, m + 24, m + 16, sw - 48, sh - 32, FluentBlueLight);
                    DrawModernArrow(g, m + sw / 2, m + 12, m + sw / 2, m + 2, 10, Color.White);
                    DrawModernArrow(g, m + sw / 2, m + sh - 12, m + sw / 2, m + sh - 2, 10, Color.White);
                    break;

                // ==================== 10 ZenQ Exclusive Transitions ====================
                case TransitionType.GenOffice_CinematicPrism:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(15, 23, 42));
                    PointF[] prism = new PointF[] { new PointF(m + 16, m + sh - 12), new PointF(m + 36, m + 12), new PointF(m + 56, m + sh - 12) };
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                    using (Pen rP = new Pen(RoseColor, 3f))
                    using (Pen gP = new Pen(EmeraldColor, 3f))
                    using (Pen bP = new Pen(FluentBlue, 3f))
                    {
                        g.FillPolygon(pb, prism);
                        g.DrawLine(rP, m + 44, m + 24, m + sw - 6, m + 16);
                        g.DrawLine(gP, m + 46, m + 32, m + sw - 6, m + 32);
                        g.DrawLine(bP, m + 48, m + 40, m + sw - 6, m + 48);
                    }
                    break;

                case TransitionType.GenOffice_LiquidFluid:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(30, 58, 138));
                    using (GraphicsPath clip = GetRoundedRect(new RectangleF(m, m, sw, sh), 6f))
                    {
                        g.SetClip(clip);
                        using (GraphicsPath wave = new GraphicsPath())
                        {
                            wave.AddBezier(m + 4, m + sh - 10, m + 28, m + 8, m + 60, m + sh + 6, m + sw - 4, m + 18);
                            wave.AddLine(m + sw, m + sh, m, m + sh);
                            wave.CloseFigure();
                            using (LinearGradientBrush wb = new LinearGradientBrush(
                                new RectangleF(m, m, sw, sh), FluentBlueLight, Color.FromArgb(45, 212, 191), 0f))
                            {
                                g.FillPath(wb, wave);
                            }
                        }
                        g.ResetClip();
                    }
                    DrawSlideCardBase(g, m, m, sw, sh, Color.Transparent, SlideBorder);
                    break;

                case TransitionType.GenOffice_MatrixRain:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(2, 44, 34));
                    using (Pen mp = new Pen(Color.FromArgb(74, 222, 128), 2.5f))
                    using (SolidBrush headB = new SolidBrush(Color.White))
                    {
                        float[] colX = new float[] { m + 16, m + 38, m + 60, m + 82 };
                        float[] lengths = new float[] { 34, 44, 28, 40 };
                        for (int mi = 0; mi < 4; mi++)
                        {
                            g.DrawLine(mp, colX[mi], m + 10, colX[mi], m + 10 + lengths[mi]);
                            g.FillEllipse(headB, colX[mi] - 2.5f, m + 10 + lengths[mi], 5, 5);
                        }
                    }
                    break;

                case TransitionType.GenOffice_NeonPulse:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(24, 24, 37));
                    using (Pen p1 = new Pen(PurpleColor, 3f))
                    using (Pen p2 = new Pen(Color.FromArgb(6, 182, 212), 2.5f))
                    using (SolidBrush cb = new SolidBrush(RoseColor))
                    {
                        g.DrawEllipse(p1, m + 12, m + 8, sw - 24, sh - 16);
                        g.DrawEllipse(p2, m + 26, m + 16, sw - 52, sh - 32);
                        g.FillEllipse(cb, m + sw / 2 - 8, m + sh / 2 - 8, 16, 16);
                    }
                    break;

                case TransitionType.GenOffice_PortalWarp:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(15, 23, 42));
                    using (Pen p1 = new Pen(RoseColor, 3.5f))
                    using (Pen p2 = new Pen(FluentBlue, 2.5f))
                    using (SolidBrush pb = new SolidBrush(PurpleColor))
                    {
                        g.FillEllipse(pb, m + 18, m + 12, sw - 36, sh - 24);
                        g.DrawEllipse(p1, m + 18, m + 12, sw - 36, sh - 24);
                        g.DrawEllipse(p2, m + 30, m + 18, sw - 60, sh - 36);
                    }
                    break;

                case TransitionType.GenOffice_Supernova:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(45, 10, 10));
                    DrawSparkleStar(g, m + sw / 2, m + sh / 2, 26, GoldColor);
                    DrawSparkleStar(g, m + sw / 2, m + sh / 2, 14, Color.White);
                    break;

                case TransitionType.GenOffice_HologramGlitch:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(8, 51, 68));
                    using (SolidBrush s1 = new SolidBrush(Color.FromArgb(6, 182, 212)))
                    using (SolidBrush s2 = new SolidBrush(RoseColor))
                    using (SolidBrush s3 = new SolidBrush(GoldColor))
                    {
                        g.FillRectangle(s1, m + 8, m + 12, sw - 20, 6);
                        g.FillRectangle(s2, m + 22, m + 28, sw - 30, 6);
                        g.FillRectangle(s3, m + 12, m + 44, sw - 24, 6);
                    }
                    break;

                case TransitionType.GenOffice_Kaleidoscope:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(49, 46, 129));
                    DrawSmoothHexagon(g, m + sw / 2, m + sh / 2, 18, PurpleColor);
                    DrawSparkleStar(g, m + sw / 2, m + sh / 2, 14, RoseColor);
                    break;

                case TransitionType.GenOffice_Starlight:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(15, 23, 42));
                    DrawSparkleStar(g, m + 20, m + 18, 12, GoldColor);
                    DrawSparkleStar(g, m + 44, m + 12, 8, Color.White);
                    DrawSparkleStar(g, m + sw - 24, m + 22, 14, GoldColor);
                    DrawSparkleStar(g, m + 32, m + sh - 20, 10, Color.White);
                    DrawSparkleStar(g, m + sw / 2 + 10, m + sh - 18, 12, GoldColor);
                    DrawSparkleStar(g, m + sw - 26, m + sh - 16, 9, Color.White);
                    break;

                case TransitionType.GenOffice_GoldenRibbon:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(69, 26, 3));
                    using (GraphicsPath ribbon = new GraphicsPath())
                    {
                        ribbon.AddBezier(m + 8, m + sh - 8, m + 28, m + 10, m + 68, m + sh + 4, m + sw - 8, m + 16);
                        using (Pen ribPen = new Pen(GoldColor, 6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        using (Pen ribLight = new Pen(Color.FromArgb(254, 240, 138), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        {
                            g.DrawPath(ribPen, ribbon);
                            g.DrawPath(ribLight, ribbon);
                        }
                    }
                    break;

                case TransitionType.GenOffice_SmoothFlow:
                    DrawSlideCardBase(g, m, m, sw, sh, Color.FromArgb(13, 148, 136));
                    using (GraphicsPath flow = new GraphicsPath())
                    {
                        flow.AddBezier(m + 8, m + 12, m + 36, m + sh - 8, m + 64, m + 8, m + sw - 8, m + sh - 12);
                        flow.AddLine(m + sw - 8, m + sh - 4, m + 8, m + sh - 4);
                        flow.CloseFigure();
                        using (SolidBrush fb = new SolidBrush(Color.FromArgb(45, 212, 191)))
                        using (Pen fp = new Pen(Color.White, 3f))
                        {
                            g.FillPath(fb, flow);
                            g.DrawPath(fp, flow);
                        }
                    }
                    break;

                default:
                    DrawSlideCardBase(g, m, m, sw, sh, FluentBlue);
                    break;
            }
        }

        private static void DrawModernArrow(Graphics g, float x1, float y1, float x2, float y2, float headSize, Color col)
        {
            using (Pen p = new Pen(col, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, x1, y1, x2, y2);
                float angle = (float)Math.Atan2(y2 - y1, x2 - x1);
                float sin = (float)Math.Sin(angle);
                float cos = (float)Math.Cos(angle);

                PointF arrowLeft = new PointF(
                    x2 - headSize * cos + headSize * 0.6f * sin,
                    y2 - headSize * sin - headSize * 0.6f * cos);
                PointF arrowRight = new PointF(
                    x2 - headSize * cos - headSize * 0.6f * sin,
                    y2 - headSize * sin + headSize * 0.6f * cos);

                using (SolidBrush b = new SolidBrush(col))
                {
                    PointF[] head = new PointF[] { new PointF(x2, y2), arrowLeft, arrowRight };
                    g.FillPolygon(b, head);
                }
            }
        }

        private static void DrawSparkleStar(Graphics g, float cx, float cy, float r, Color col)
        {
            using (Pen p = new Pen(col, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (SolidBrush b = new SolidBrush(col))
            {
                g.DrawLine(p, cx - r, cy, cx + r, cy);
                g.DrawLine(p, cx, cy - r, cx, cy + r);
                float d = r * 0.65f;
                using (Pen pDiag = new Pen(col, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pDiag, cx - d, cy - d, cx + d, cy + d);
                    g.DrawLine(pDiag, cx - d, cy + d, cx + d, cy - d);
                }
                g.FillEllipse(b, cx - 3, cy - 3, 6, 6);
            }
        }

        private static void DrawSmoothHexagon(Graphics g, float cx, float cy, float r, Color col)
        {
            PointF[] hex = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double a = i * Math.PI / 3.0;
                hex[i] = new PointF((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a)));
            }
            using (SolidBrush b = new SolidBrush(col))
            using (Pen p = new Pen(Color.White, 2.0f))
            {
                g.FillPolygon(b, hex);
                g.DrawPolygon(p, hex);
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.UI;

namespace ZenQOffice
{
    public enum LauncherState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    public class LauncherCardItem
    {
        public OfficeAppMode Mode;
        public string Title;
        public string Subtitle;
        public string Label;
        public string BadgeText;
        public Color AccentColor;
        public Color DarkColor;
        public Color BadgeBgColor;
        public Color BadgeBorderColor;
        public RectangleF Bounds;

        public LauncherCardItem(OfficeAppMode mode, string title, string subtitle, string badgeText, Color accent, Color dark, Color badgeBg, Color badgeBorder)
        {
            Mode = mode;
            Title = title;
            Subtitle = subtitle;
            Label = title + " - " + subtitle;
            BadgeText = badgeText;
            AccentColor = accent;
            DarkColor = dark;
            BadgeBgColor = badgeBg;
            BadgeBorderColor = badgeBorder;
            Bounds = RectangleF.Empty;
        }

        public LauncherCardItem(OfficeAppMode mode, string title, string subtitle, Color accent, Color dark)
            : this(mode, title, subtitle, mode.ToString().ToUpper(), accent, dark, Color.FromArgb(240, 240, 240), Color.FromArgb(200, 200, 200))
        {
        }
    }

    /// <summary>
    /// Modern Microsoft 365 / Apple style Popover Dropdown Launcher (+) with Fluid Motion.
    /// Provides sleek, compact dispatch to spawn Writer, Calc, Slides, and PDF document tabs.
    /// </summary>
    public static class NewTabRadialLauncher
    {
        private static LauncherState _state = LauncherState.Closed;
        private static Point _center = new Point(300, 21);
        private static Stopwatch _stopwatch = new Stopwatch();
        private static float _animProgress = 0f;
        private static float _animStartProgress = 0f;
        private static int _hoveredIndex = -1;
        private static RectangleF _menuBounds = RectangleF.Empty;

        private const float OPEN_DURATION_MS = 160f;
        private const float CLOSE_DURATION_MS = 120f;

        private static readonly LauncherCardItem[] _items;

        static NewTabRadialLauncher()
        {
            _items = new LauncherCardItem[]
            {
                new LauncherCardItem(
                    OfficeAppMode.Writer,
                    "Writer",
                    "Văn bản Word",
                    "DOCX",
                    Color.FromArgb(37, 99, 235),
                    Color.FromArgb(29, 78, 216),
                    Color.FromArgb(239, 246, 255),
                    Color.FromArgb(191, 219, 254)),

                new LauncherCardItem(
                    OfficeAppMode.Calc,
                    "Calc",
                    "Bảng tính Excel",
                    "XLSX",
                    Color.FromArgb(16, 185, 129),
                    Color.FromArgb(5, 150, 105),
                    Color.FromArgb(236, 253, 245),
                    Color.FromArgb(167, 243, 208)),

                new LauncherCardItem(
                    OfficeAppMode.Slides,
                    "Slides",
                    "Trình chiếu PowerPoint",
                    "PPTX",
                    Color.FromArgb(234, 88, 12),
                    Color.FromArgb(194, 65, 12),
                    Color.FromArgb(255, 247, 237),
                    Color.FromArgb(254, 215, 170)),

                new LauncherCardItem(
                    OfficeAppMode.Pdf,
                    "PDF",
                    "Tài liệu PDF",
                    "PDF",
                    Color.FromArgb(220, 38, 38),
                    Color.FromArgb(185, 28, 28),
                    Color.FromArgb(254, 242, 242),
                    Color.FromArgb(254, 202, 202)),

                new LauncherCardItem(
                    OfficeAppMode.Photo,
                    "Photo",
                    "Thiết kế & Đồ họa",
                    "PSD",
                    Color.FromArgb(99, 102, 241),
                    Color.FromArgb(79, 70, 229),
                    Color.FromArgb(238, 242, 255),
                    Color.FromArgb(199, 210, 254))
            };
        }

        public static AppForm TargetAppForm { get; set; }

        #region Public Interface Contracts
        public static bool IsOpen
        {
            get { return WpfNewTabLauncherWindow.IsCurrentlyOpen || _state != LauncherState.Closed; }
            set
            {
                if (value)
                {
                    Open(_center, TargetAppForm);
                }
                else
                {
                    Close();
                }
            }
        }

        public static bool IsVisible
        {
            get { return WpfNewTabLauncherWindow.IsCurrentlyOpen || _state != LauncherState.Closed; }
        }

        public static bool IsAnimating
        {
            get { return !WpfNewTabLauncherWindow.IsCurrentlyOpen && (_state == LauncherState.Opening || _state == LauncherState.Closing); }
        }

        public static float Progress
        {
            get { return _animProgress; }
        }

        public static Point Center
        {
            get { return _center; }
        }

        public static LauncherState State
        {
            get { return _state; }
        }

        public static RectangleF MenuBounds
        {
            get { return _menuBounds; }
        }

        public static void Open(Point center, AppForm form = null)
        {
            _center = center;
            AppForm target = form ?? TargetAppForm;
            if (target != null)
            {
                WpfNewTabLauncherWindow.ShowLauncher(center, target, (mode) =>
                {
                    target.SwitchAppMode(mode);
                });
                return;
            }

            _animStartProgress = _animProgress;
            _state = LauncherState.Opening;
            _stopwatch.Restart();
        }

        public static void Close()
        {
            WpfNewTabLauncherWindow.CloseActive();
            if (_state != LauncherState.Closed && _state != LauncherState.Closing)
            {
                _animStartProgress = _animProgress;
                _state = LauncherState.Closing;
                _stopwatch.Restart();
                _hoveredIndex = -1;
            }
        }

        public static void Toggle(Point center, AppForm form = null)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open(center, form);
            }
        }

        /// <summary>
        /// Advances animation frame using Stopwatch delta-time. Called by 16ms animTimer loop.
        /// </summary>
        public static void UpdateAnimation()
        {
            if (WpfNewTabLauncherWindow.IsCurrentlyOpen) return;
            if (_state == LauncherState.Closed) return;

            if (_state == LauncherState.Opening)
            {
                float elapsed = (float)_stopwatch.ElapsedMilliseconds;
                float t = Math.Min(1.0f, elapsed / OPEN_DURATION_MS);
                float easeVal = CalculateEaseOutCubic(t);
                _animProgress = _animStartProgress + (1.0f - _animStartProgress) * easeVal;
                if (t >= 1.0f)
                {
                    _animProgress = 1.0f;
                    _state = LauncherState.Open;
                    _stopwatch.Stop();
                }
            }
            else if (_state == LauncherState.Closing)
            {
                float elapsed = (float)_stopwatch.ElapsedMilliseconds;
                float t = Math.Min(1.0f, elapsed / CLOSE_DURATION_MS);
                float easeVal = CalculateEaseOutCubic(t);
                _animProgress = Math.Max(0.0f, _animStartProgress * (1.0f - easeVal));
                if (t >= 1.0f || _animProgress <= 0.001f)
                {
                    _animProgress = 0.0f;
                    _state = LauncherState.Closed;
                    _stopwatch.Stop();
                }
            }
        }

        /// <summary>
        /// Draws modern Microsoft 365 / Apple style Popover Dropdown Card anchored cleanly under the '+' button.
        /// </summary>
        public static void Draw(Graphics g, int clientWidth, int clientHeight)
        {
            if (WpfNewTabLauncherWindow.IsCurrentlyOpen) return;
            if (_state == LauncherState.Closed || _animProgress <= 0.001f) return;
            if (clientWidth <= 0 || clientHeight <= 0) return;

            GraphicsState origState = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                float alpha = Math.Max(0f, Math.Min(1f, _animProgress));

                // Popover Dimensions
                float menuW = 264f;
                float rowH = 46f;
                float headerH = 32f;
                float padBottom = 8f;
                float menuH = headerH + _items.Length * rowH + padBottom;

                // Anchor directly below document tab '+' button
                float topY = Math.Max(34f, _center.Y + 14f);
                float targetX = _center.X - 32f;

                // Keep fully inside client window
                if (targetX + menuW > clientWidth - 12f) targetX = clientWidth - menuW - 12f;
                if (targetX < 12f) targetX = 12f;

                float beakW = 14f;
                float beakH = 7f;
                float cardTopY = topY + beakH;
                RectangleF cardRect = new RectangleF(targetX, cardTopY, menuW, menuH);

                float beakCenterX = (float)_center.X;
                float minBeakX = cardRect.X + 16f;
                float maxBeakX = cardRect.Right - 16f;
                if (beakCenterX < minBeakX) beakCenterX = minBeakX;
                if (beakCenterX > maxBeakX) beakCenterX = maxBeakX;

                _menuBounds = new RectangleF(cardRect.X, topY, cardRect.Width, cardRect.Height + beakH);

                // Subtle fluid slide-down animation (moves 6px down as it fades in)
                float slideY = (1f - alpha) * -6f;
                g.TranslateTransform(0, slideY);

                // Create unified seamless path (beak + rounded card)
                using (GraphicsPath popoverPath = CreatePopoverPath(cardRect, beakCenterX, topY, beakW, beakH, 10f))
                {
                    // 1. Multi-layer soft ambient drop shadow
                    int[] shadowOffsets = new int[] { 1, 3, 6, 10 };
                    int[] shadowAlphas = new int[] { 10, 14, 16, 18 };
                    for (int s = 0; s < shadowOffsets.Length; s++)
                    {
                        int sa = (int)(shadowAlphas[s] * alpha);
                        if (sa > 0)
                        {
                            GraphicsState sState = g.Save();
                            g.TranslateTransform(0, shadowOffsets[s]);
                            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(sa, 15, 23, 42)))
                            {
                                g.FillPath(shadowBrush, popoverPath);
                            }
                            g.Restore(sState);
                        }
                    }

                    // 2. Card Background Fill (Pure Crisp White)
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 255, 255, 255)))
                    {
                        g.FillPath(bgBrush, popoverPath);
                    }

                    // 3. Crisp Border
                    using (Pen borderPen = new Pen(Color.FromArgb((int)(210 * alpha), 226, 232, 240), 1f))
                    {
                        borderPen.LineJoin = LineJoin.Round;
                        g.DrawPath(borderPen, popoverPath);
                    }
                }

                // 4. Header Section ("TẠO TÀI LIỆU MỚI")
                using (Font headerFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb((int)(180 * alpha), 100, 116, 139)))
                {
                    RectangleF headerRect = new RectangleF(cardRect.X + 16f, cardRect.Y + 8f, cardRect.Width - 32f, 18f);
                    StringFormat hSf = new StringFormat();
                    hSf.LineAlignment = StringAlignment.Center;
                    g.DrawString("TẠO TÀI LIỆU MỚI", headerFont, headerBrush, headerRect, hSf);
                }

                // Subtle divider under header
                float divY = cardRect.Y + 30f;
                using (Pen divPen = new Pen(Color.FromArgb((int)(160 * alpha), 241, 245, 249), 1f))
                {
                    g.DrawLine(divPen, cardRect.X + 12f, divY, cardRect.Right - 12f, divY);
                }

                // 5. Four Modern Document Rows
                float startRowY = divY + 4f;
                float rowW = cardRect.Width - 16f;
                float rowX = cardRect.X + 8f;

                using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (Font badgeFont = new Font("Segoe UI", 7.0f, FontStyle.Bold))
                using (StringFormat badgeSf = new StringFormat())
                {
                    badgeSf.Alignment = StringAlignment.Center;
                    badgeSf.LineAlignment = StringAlignment.Center;

                    for (int i = 0; i < _items.Length; i++)
                    {
                        LauncherCardItem item = _items[i];
                        float curRowY = startRowY + i * rowH;
                        RectangleF rowRect = new RectangleF(rowX, curRowY, rowW, rowH);
                        item.Bounds = rowRect;

                        bool isHovered = (_hoveredIndex == i);

                        // Row hover background highlight
                        if (isHovered)
                        {
                            using (GraphicsPath rowPath = CreateRoundedRectanglePath(rowRect, 7f))
                            using (SolidBrush hovBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 241, 245, 249)))
                            using (Pen hovPen = new Pen(Color.FromArgb((int)(180 * alpha), 226, 232, 240), 1f))
                            {
                                g.FillPath(hovBrush, rowPath);
                                g.DrawPath(hovPen, rowPath);
                            }
                        }

                        // Vector Icon Emblem
                        float emblemSize = 28f;
                        float emblemX = rowRect.X + 8f;
                        float emblemY = rowRect.Y + (rowRect.Height - emblemSize) / 2f;
                        DrawEmblem(g, emblemX, emblemY, emblemSize, item.Mode);

                        // Title & Subtitle
                        float textX = emblemX + emblemSize + 11f;
                        Color titleColor = isHovered ? Color.FromArgb(15, 23, 42) : Color.FromArgb(30, 41, 59);
                        using (SolidBrush tBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), titleColor)))
                        using (SolidBrush sBrush = new SolidBrush(Color.FromArgb((int)(200 * alpha), 100, 116, 139)))
                        {
                            g.DrawString(item.Title, titleFont, tBrush, textX, rowRect.Y + 6f);
                            g.DrawString(item.Subtitle, subFont, sBrush, textX, rowRect.Y + 23f);
                        }

                        // Right-side Pill Badge ("DOCX", "XLSX", "PPTX", "PDF")
                        float badgeW = 42f;
                        float badgeH = 20f;
                        float badgeX = rowRect.Right - badgeW - 8f;
                        float badgeY = rowRect.Y + (rowRect.Height - badgeH) / 2f;
                        RectangleF badgeRect = new RectangleF(badgeX, badgeY, badgeW, badgeH);

                        using (GraphicsPath bPath = CreateRoundedRectanglePath(badgeRect, 5f))
                        using (SolidBrush bBg = new SolidBrush(Color.FromArgb((int)(255 * alpha), item.BadgeBgColor)))
                        using (Pen bPen = new Pen(Color.FromArgb((int)(200 * alpha), item.BadgeBorderColor), 1f))
                        using (SolidBrush bTxt = new SolidBrush(Color.FromArgb((int)(255 * alpha), item.AccentColor)))
                        {
                            g.FillPath(bBg, bPath);
                            g.DrawPath(bPen, bPath);
                            g.DrawString(item.BadgeText, badgeFont, bTxt, badgeRect, badgeSf);
                        }
                    }
                }
            }
            finally
            {
                g.Restore(origState);
            }
        }

        public static bool HandleMouseDown(Point pt, RibbonBar ribbon, Action<OfficeAppMode> onOpenTab = null, SlidesView slidesView = null, WriterView writerView = null, CalcView calcView = null, PdfView pdfView = null, PhotoView photoView = null)
        {
            if (!IsOpen) return false;

            if (WpfNewTabLauncherWindow.IsCurrentlyOpen)
            {
                Close();
                return false;
            }

            // 1. Check item row clicks
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].Bounds.Contains(pt))
                {
                    OfficeAppMode mode = _items[i].Mode;

                    if (onOpenTab != null)
                    {
                        try
                        {
                            onOpenTab(mode);
                        }
                        catch
                        {
                        }
                    }
                    else if (ribbon != null)
                    {
                        int idx = (ribbon.DocTabs != null) ? ribbon.DocTabs.Count : 1;
                        string ext;
                        string namePrefix;
                        if (mode == OfficeAppMode.Slides)
                        {
                            ext = ".pptx";
                            namePrefix = "Trình chiếu ";
                        }
                        else if (mode == OfficeAppMode.Calc)
                        {
                            ext = ".xlsx";
                            namePrefix = "Bảng tính ";
                        }
                        else if (mode == OfficeAppMode.Pdf)
                        {
                            ext = ".pdf";
                            namePrefix = "Tài liệu PDF ";
                        }
                        else if (mode == OfficeAppMode.Photo)
                        {
                            ext = ".psd";
                            namePrefix = "Thiết kế ";
                        }
                        else
                        {
                            ext = ".docx";
                            namePrefix = "Văn bản ";
                        }

                        ribbon.AddDocTab(namePrefix + idx + ext, mode, slidesView, writerView, calcView, pdfView, photoView);
                    }

                    Close();
                    return true;
                }
            }

            // 2. Click inside menu card (e.g. header or margins): consume click without closing
            if (_menuBounds.Contains(pt))
            {
                return true;
            }

            // 3. Click outside menu: dismiss popover cleanly
            Close();
            return true;
        }

        public static bool HandleMouseMove(Point pt)
        {
            if (!IsOpen || WpfNewTabLauncherWindow.IsCurrentlyOpen) return false;

            int prevHovered = _hoveredIndex;
            int newHovered = -1;
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].Bounds.Contains(pt))
                {
                    newHovered = i;
                    break;
                }
            }

            _hoveredIndex = newHovered;
            if (_hoveredIndex >= 0)
            {
                Cursor.Current = Cursors.Hand;
                return true;
            }

            if (_menuBounds.Contains(pt))
            {
                Cursor.Current = Cursors.Default;
                return true;
            }

            if (prevHovered != -1)
            {
                return true;
            }

            return false;
        }

        public static bool HandleKeyDown(Keys key)
        {
            if (!IsOpen || WpfNewTabLauncherWindow.IsCurrentlyOpen) return false;

            if (key == Keys.Escape)
            {
                Close();
                return true;
            }

            if (key == Keys.Down)
            {
                _hoveredIndex = (_hoveredIndex + 1) % _items.Length;
                return true;
            }

            if (key == Keys.Up)
            {
                _hoveredIndex = (_hoveredIndex <= 0) ? _items.Length - 1 : _hoveredIndex - 1;
                return true;
            }

            return false;
        }
        #endregion

        #region Mathematics & Easing Functions
        private static float CalculateEaseOutCubic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
        #endregion

        #region Graphics Geometry & Rendering Details
        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0.5f)
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

        private static GraphicsPath CreatePopoverPath(RectangleF cardRect, float beakCenterX, float beakTopY, float beakW, float beakH, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(cardRect.Width / 2f, cardRect.Height / 2f));
            float d = r * 2f;

            float minBeakX = cardRect.X + r + beakW / 2f + 2f;
            float maxBeakX = cardRect.Right - r - beakW / 2f - 2f;
            float bx = Math.Max(minBeakX, Math.Min(maxBeakX, beakCenterX));

            // Top-Left corner arc
            path.AddArc(cardRect.X, cardRect.Y, d, d, 180, 90);

            // Top edge up to left of beak
            path.AddLine(cardRect.X + r, cardRect.Y, bx - beakW / 2f, cardRect.Y);

            // Beak peak pointing up towards '+' button
            path.AddLine(bx - beakW / 2f, cardRect.Y, bx, beakTopY);

            // Beak right side down to top edge
            path.AddLine(bx, beakTopY, bx + beakW / 2f, cardRect.Y);

            // Top edge from right of beak to top-right corner
            path.AddLine(bx + beakW / 2f, cardRect.Y, cardRect.Right - r, cardRect.Y);

            // Top-Right corner arc
            path.AddArc(cardRect.Right - d, cardRect.Y, d, d, 270, 90);

            // Right edge
            path.AddLine(cardRect.Right, cardRect.Y + r, cardRect.Right, cardRect.Bottom - r);

            // Bottom-Right corner arc
            path.AddArc(cardRect.Right - d, cardRect.Bottom - d, d, d, 0, 90);

            // Bottom edge
            path.AddLine(cardRect.Right - r, cardRect.Bottom, cardRect.X + r, cardRect.Bottom);

            // Bottom-Left corner arc
            path.AddArc(cardRect.X, cardRect.Bottom - d, d, d, 90, 90);

            // Left edge
            path.AddLine(cardRect.X, cardRect.Bottom - r, cardRect.X, cardRect.Y + r);

            path.CloseFigure();
            return path;
        }

        private static void DrawEmblem(Graphics g, float x, float y, float size, OfficeAppMode mode)
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
            else if (mode == OfficeAppMode.Photo)
            {
                baseCol = Color.FromArgb(99, 102, 241);
                darkCol = Color.FromArgb(79, 70, 229);
            }
            else // PDF
            {
                baseCol = Color.FromArgb(220, 38, 38);
                darkCol = Color.FromArgb(185, 28, 28);
            }

            // Outer rounded tile
            using (GraphicsPath bPath = CreateRoundedRectanglePath(box, size * 0.24f))
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(box, baseCol, darkCol, 45f))
            {
                g.FillPath(bgBrush, bPath);
            }

            float pad = size * 0.20f;
            RectangleF inner = new RectangleF(x + pad, y + pad, size - pad * 2f, size - pad * 2f);

            if (mode == OfficeAppMode.Writer)
            {
                using (GraphicsPath docP = CreateRoundedRectanglePath(inner, 2.5f))
                {
                    g.FillPath(Brushes.White, docP);
                }
                float foldSize = inner.Width * 0.35f;
                using (SolidBrush foldB = new SolidBrush(Color.FromArgb(219, 234, 254)))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(inner.Right - foldSize, inner.Top),
                        new PointF(inner.Right, inner.Top + foldSize),
                        new PointF(inner.Right - foldSize, inner.Top + foldSize)
                    });
                    g.FillPath(foldB, foldP);
                }
                using (Pen linePen = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
                {
                    linePen.StartCap = LineCap.Round;
                    linePen.EndCap = LineCap.Round;
                    float lineX = inner.Left + 3f;
                    float lineW = inner.Width - 6f;
                    g.DrawLine(linePen, lineX, inner.Top + inner.Height * 0.32f, inner.Right - foldSize - 1f, inner.Top + inner.Height * 0.32f);
                    g.DrawLine(linePen, lineX, inner.Top + inner.Height * 0.52f, lineX + lineW, inner.Top + inner.Height * 0.52f);
                    g.DrawLine(linePen, lineX, inner.Top + inner.Height * 0.72f, lineX + lineW * 0.65f, inner.Top + inner.Height * 0.72f);
                }
            }
            else if (mode == OfficeAppMode.Calc)
            {
                using (GraphicsPath sheetP = CreateRoundedRectanglePath(inner, 2.5f))
                {
                    g.FillPath(Brushes.White, sheetP);
                }
                using (SolidBrush thB = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillRectangle(thB, inner.Left, inner.Top, inner.Width, inner.Height * 0.30f);
                }
                using (Pen gridPen = new Pen(Color.FromArgb(167, 243, 208), 1.0f))
                {
                    float midX = inner.Left + inner.Width * 0.50f;
                    g.DrawLine(gridPen, midX, inner.Top + inner.Height * 0.30f, midX, inner.Bottom);
                    g.DrawLine(gridPen, inner.Left, inner.Top + inner.Height * 0.62f, inner.Right, inner.Top + inner.Height * 0.62f);
                }
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(5, 150, 105)))
                {
                    float bw = 2.5f;
                    g.FillRectangle(barB, inner.Left + 3f, inner.Bottom - 5f, bw, 4f);
                    g.FillRectangle(barB, inner.Left + 7f, inner.Bottom - 8f, bw, 7f);
                    g.FillRectangle(barB, inner.Left + 11f, inner.Bottom - 11f, bw, 10f);
                }
            }
            else if (mode == OfficeAppMode.Slides)
            {
                using (GraphicsPath slideP = CreateRoundedRectanglePath(inner, 2.5f))
                {
                    g.FillPath(Brushes.White, slideP);
                }
                RectangleF sInner = new RectangleF(inner.Left + 2f, inner.Top + 2f, inner.Width - 4f, inner.Height - 4f);
                using (LinearGradientBrush sBg = new LinearGradientBrush(sInner, Color.FromArgb(254, 243, 199), Color.FromArgb(254, 215, 170), 45f))
                {
                    g.FillRectangle(sBg, sInner);
                }
                using (SolidBrush pieB = new SolidBrush(Color.FromArgb(234, 88, 12)))
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(249, 115, 22)))
                {
                    g.FillPie(pieB, sInner.Left + 2f, sInner.Top + 2f, sInner.Width * 0.55f, sInner.Height * 0.55f, 0, 270);
                    g.FillRectangle(barB, sInner.Left + sInner.Width * 0.62f, sInner.Top + 3f, sInner.Width * 0.32f, 2.5f);
                    g.FillRectangle(barB, sInner.Left + sInner.Width * 0.62f, sInner.Top + 7f, sInner.Width * 0.22f, 2.5f);
                }
            }
            else if (mode == OfficeAppMode.Photo)
            {
                using (GraphicsPath photoP = CreateRoundedRectanglePath(inner, 2.5f))
                {
                    g.FillPath(Brushes.White, photoP);
                }
                using (Font psFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush psTxt = new SolidBrush(Color.FromArgb(99, 102, 241)))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("Ps", psFont, psTxt, new RectangleF(inner.Left, inner.Top + 2f, inner.Width, inner.Height - 2f), sf);
                }
            }
            else // PDF
            {
                using (GraphicsPath pdfP = CreateRoundedRectanglePath(inner, 2.5f))
                {
                    g.FillPath(Brushes.White, pdfP);
                }
                float foldSize = inner.Width * 0.35f;
                using (SolidBrush foldB = new SolidBrush(Color.FromArgb(254, 202, 202)))
                using (GraphicsPath foldP = new GraphicsPath())
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(inner.Right - foldSize, inner.Top),
                        new PointF(inner.Right, inner.Top + foldSize),
                        new PointF(inner.Right - foldSize, inner.Top + foldSize)
                    });
                    g.FillPath(foldB, foldP);
                }
                using (Font pdfFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
                using (SolidBrush pdfTxt = new SolidBrush(Color.FromArgb(220, 38, 38)))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("PDF", pdfFont, pdfTxt, new RectangleF(inner.Left, inner.Top + 3f, inner.Width - 2f, inner.Height - 3f), sf);
                }
            }
        }
        #endregion
    }
}

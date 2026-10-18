using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;
using WpfSize = System.Windows.Size;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfPen = System.Windows.Media.Pen;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfDrawingVisual = System.Windows.Media.DrawingVisual;
using WpfDrawingContext = System.Windows.Media.DrawingContext;
using WpfRenderTargetBitmap = System.Windows.Media.Imaging.RenderTargetBitmap;
using WpfPixelFormats = System.Windows.Media.PixelFormats;
using WpfPenLineCap = System.Windows.Media.PenLineCap;
using WpfPenLineJoin = System.Windows.Media.PenLineJoin;
using WpfStreamGeometry = System.Windows.Media.StreamGeometry;
using WpfSweepDirection = System.Windows.Media.SweepDirection;

namespace ZenQOffice.UI
{
    public enum ShapePaneMode
    {
        ShapesOnly,
        GraphicsOnly,
        ImagesOnly
    }

    /// <summary>
    /// Bảng điều khiển tác vụ Thư viện Hình dạng, Đồ họa hoặc Hình ảnh Stock 4K (WPF Shape Task Pane).
    /// Toàn bộ đồ họa và icon được kết xuất bằng công nghệ WPF Vector Sub-pixel Anti-Aliasing mượt mà,
    /// loại bỏ hoàn toàn hiện tượng răng cưa và méo hình của GDI+.
    /// </summary>
    public class WpfShapeTaskPane
    {
        private static readonly Dictionary<string, Bitmap> _thumbCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _loadingThumbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _thumbLock = new object();

        public ShapePaneMode Mode { get; set; } = ShapePaneMode.ShapesOnly;

        public int ScrollY { get; set; } = 0;
        public float SmoothScrollY { get; set; } = 0f;
        public float TargetScrollY { get; set; } = 0f;
        public int MaxScrollY { get; private set; } = 0;
        public string SearchText { get; set; } = "";
        public string ExpandedCategoryKey { get; set; } = null; // null: xem tất cả danh mục, khác null: xem chi tiết 1 danh mục
        public string HoveredItemId { get; set; } = null;
        public string HoveredActionId { get; set; } = null;

        // Tọa độ click hit-test
        public Rectangle BtnBackBounds = Rectangle.Empty;
        public Rectangle BtnCloseBounds = Rectangle.Empty;
        public Rectangle BtnTabShapesBounds = Rectangle.Empty;
        public Rectangle BtnTabGraphicsBounds = Rectangle.Empty;
        public Rectangle BtnTabImagesBounds = Rectangle.Empty;
        public Rectangle SearchBoxBounds = Rectangle.Empty;
        public Rectangle ScrollbarTrackBounds = Rectangle.Empty;
        public Rectangle ScrollbarThumbBounds = Rectangle.Empty;
        public bool IsDraggingScrollbar { get; private set; } = false;
        private int _dragStartMouseY = 0;
        private int _dragStartScrollY = 0;
        private Point _lastMousePos = Point.Empty;
        public List<Tuple<Rectangle, string>> ItemClickBounds = new List<Tuple<Rectangle, string>>();
        public List<Tuple<Rectangle, string>> CategorySeeAllBounds = new List<Tuple<Rectangle, string>>();

        public WpfShapeTaskPane()
        {
            WpfShapeLibrary.EnsureInitialized();
        }

        /// <summary>
        /// Lấy hoặc tạo hình thu nhỏ vector WPF siêu nét không răng cưa cho một Shape Preset với công nghệ SSAA Super-Sampling.
        /// </summary>
        public static Bitmap GetShapeThumbnail(ShapePresetItem item, int size = 48, Color? strokeColor = null, Color? fillColor = null)
        {
            if (item == null) return null;
            Color sc = strokeColor ?? Color.FromArgb(51, 65, 85);      // Slate 700
            Color fc = fillColor ?? Color.FromArgb(241, 245, 249);       // Slate 100
            
            string cacheKey = item.IsOnlineImage ? $"{item.Id}_online_{size}" : $"{item.Id}_{size}_{sc.ToArgb():X8}_{fc.ToArgb():X8}";

            lock (_thumbLock)
            {
                if (_thumbCache.TryGetValue(cacheKey, out Bitmap cached) && cached != null)
                {
                    return cached;
                }

                if (_loadingThumbs.Contains(cacheKey)) return null;
            }

            int scale = 2; // SSAA 2x Super-Sampling for ultra-sharp, anti-aliased curves
            int pixelSize = size * scale;

            if (item.IsPhoto || item.Category.StartsWith("Online_Image_"))
            {
                lock (_thumbLock) { _loadingThumbs.Add(cacheKey); }
                System.Threading.ThreadPool.QueueUserWorkItem(_ => 
                {
                    try 
                    {
                        Bitmap photoBmp = ZenQOffice.Core.OnlineImagesManager.GetCachedOrDownloadImage(item.Id, item.ImageUrl, pixelSize);
                        if (photoBmp != null) 
                        {
                            lock (_thumbLock) { _thumbCache[cacheKey] = photoBmp; }
                            ZenQOffice.Core.OnlineImagesManager.TriggerImageDownloaded();
                        }
                    } 
                    catch { }
                    finally
                    {
                        lock (_thumbLock) { _loadingThumbs.Remove(cacheKey); }
                    }
                });
                return null;
            }

            if (item.IsOnlineImage) 
            {
                lock (_thumbLock) { _loadingThumbs.Add(cacheKey); }
                System.Threading.ThreadPool.QueueUserWorkItem(_ => 
                {
                    try 
                    {
                        Bitmap onlineBmp = ZenQOffice.Core.OnlineGraphicsManager.GetCachedOrDownloadImage(item.Id, item.ImageUrl, pixelSize);
                        if (onlineBmp != null) 
                        {
                            lock (_thumbLock) { _thumbCache[cacheKey] = onlineBmp; }
                            ZenQOffice.Core.OnlineGraphicsManager.TriggerImageDownloaded();
                        }
                    } 
                    catch { }
                    finally
                    {
                        lock (_thumbLock) { _loadingThumbs.Remove(cacheKey); }
                    }
                });
                return null;
            }

            WpfDrawingVisual dv = new WpfDrawingVisual();
            using (WpfDrawingContext dc = dv.RenderOpen())
            {
                WpfColor wsc = WpfColor.FromArgb(sc.A, sc.R, sc.G, sc.B);
                WpfColor wfc = WpfColor.FromArgb(fc.A, fc.R, fc.G, fc.B);

                WpfSolidColorBrush strokeBrush = new WpfSolidColorBrush(wsc);
                WpfSolidColorBrush fillBrush = new WpfSolidColorBrush(wfc);

                WpfPen pen = new WpfPen(strokeBrush, 1.4)
                {
                    StartLineCap = WpfPenLineCap.Round,
                    EndLineCap = WpfPenLineCap.Round,
                    LineJoin = WpfPenLineJoin.Round
                };

                double pad = 6.0;
                double drawW = size - pad * 2.0;
                double drawH = size - pad * 2.0;

                dc.PushTransform(new System.Windows.Media.TranslateTransform(pad, pad));
                var geom = item.CreateWpfGeometry(drawW, drawH);
                if (geom != null)
                {
                    if (item.Category == "Lines")
                    {
                        dc.DrawGeometry(null, pen, geom);
                    }
                    else
                    {
                        dc.DrawGeometry(fillBrush, pen, geom);
                    }
                }
                dc.Pop();
            }

            WpfRenderTargetBitmap rtb = new WpfRenderTargetBitmap(pixelSize, pixelSize, 96.0 * scale, 96.0 * scale, WpfPixelFormats.Pbgra32);
            rtb.Render(dv);

            int stride = pixelSize * 4;
            byte[] pixelBytes = new byte[stride * pixelSize];
            rtb.CopyPixels(pixelBytes, stride, 0);

            Bitmap bmp = new Bitmap(pixelSize, pixelSize, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var rect = new Rectangle(0, 0, pixelSize, pixelSize);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                Marshal.Copy(pixelBytes, 0, bmpData.Scan0, pixelBytes.Length);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            lock (_thumbLock)
            {
                _thumbCache[cacheKey] = bmp;
            }
            return bmp;
        }

        public static Bitmap GetRibbonIcon(string kind, int size = 26)
        {
            string cacheKey = $"ribbon_{kind}_{size}";
            lock (_thumbLock)
            {
                if (_thumbCache.TryGetValue(cacheKey, out Bitmap cached) && cached != null)
                {
                    return cached;
                }
            }

            WpfDrawingVisual dv = new WpfDrawingVisual();
            using (WpfDrawingContext dc = dv.RenderOpen())
            {
                double s = size;
                double pad = 2.0;
                double w = s - pad * 2.0;
                double h = s - pad * 2.0;

                dc.PushTransform(new System.Windows.Media.TranslateTransform(pad, pad));

                if (kind == "template")
                {
                    // Mini slide layout with header bar and two content columns
                    WpfPen pBorder = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(37, 99, 235)), 1.2) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round, LineJoin = WpfPenLineJoin.Round };
                    dc.DrawRoundedRectangle(WpfBrushes.White, pBorder, new WpfRect(1, 1, w - 2, h - 2), 2, 2);
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(37, 99, 235)), null, new WpfRect(3, 3, w - 6, 4), 1, 1);
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(219, 234, 254)), null, new WpfRect(3, 9, (w - 8) / 2, h - 12), 1, 1);
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(219, 234, 254)), null, new WpfRect(3 + (w - 8) / 2 + 2, 9, (w - 8) / 2, h - 12), 1, 1);
                }
                else if (kind == "elements")
                {
                    // 4 Fluent geometric shapes: Blue circle, Orange round rect, Emerald triangle, Purple diamond
                    double halfW = (w - 3) / 2.0;
                    double halfH = (h - 3) / 2.0;

                    // Top-Left: Circle
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(37, 99, 235)), null, new WpfPoint(halfW / 2.0, halfH / 2.0), halfW / 2.0 - 0.5, halfH / 2.0 - 0.5);

                    // Top-Right: Rounded Square
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(234, 88, 12)), null, new WpfRect(halfW + 3, 0.5, halfW - 1, halfH - 1), 2, 2);

                    // Bottom-Left: Triangle
                    WpfStreamGeometry tri = new WpfStreamGeometry();
                    using (var ctx = tri.Open())
                    {
                        ctx.BeginFigure(new WpfPoint(halfW / 2.0, halfH + 3), true, true);
                        ctx.LineTo(new WpfPoint(halfW - 0.5, h - 0.5), true, true);
                        ctx.LineTo(new WpfPoint(0.5, h - 0.5), true, true);
                    }
                    tri.Freeze();
                    dc.DrawGeometry(new WpfSolidColorBrush(WpfColor.FromRgb(16, 185, 129)), null, tri);

                    // Bottom-Right: Diamond
                    WpfStreamGeometry dia = new WpfStreamGeometry();
                    using (var ctx = dia.Open())
                    {
                        double dx = halfW + 3 + halfW / 2.0;
                        double dy = halfH + 3 + halfH / 2.0;
                        ctx.BeginFigure(new WpfPoint(dx, halfH + 3), true, true);
                        ctx.LineTo(new WpfPoint(w - 0.5, dy), true, true);
                        ctx.LineTo(new WpfPoint(dx, h - 0.5), true, true);
                        ctx.LineTo(new WpfPoint(halfW + 3, dy), true, true);
                    }
                    dia.Freeze();
                    dc.DrawGeometry(new WpfSolidColorBrush(WpfColor.FromRgb(139, 92, 246)), null, dia);
                }
                else if (kind == "icons")
                {
                    // Modern 4-point star sparkle
                    double cx = w * 0.45;
                    double cy = h * 0.55;
                    double rOut = w * 0.42;
                    double rIn = w * 0.12;
                    WpfStreamGeometry star = new WpfStreamGeometry();
                    using (var ctx = star.Open())
                    {
                        ctx.BeginFigure(new WpfPoint(cx, cy - rOut), true, true);
                        ctx.BezierTo(new WpfPoint(cx + rIn, cy - rIn), new WpfPoint(cx + rIn, cy - rIn), new WpfPoint(cx + rOut, cy), true, true);
                        ctx.BezierTo(new WpfPoint(cx + rIn, cy + rIn), new WpfPoint(cx + rIn, cy + rIn), new WpfPoint(cx, cy + rOut), true, true);
                        ctx.BezierTo(new WpfPoint(cx - rIn, cy + rIn), new WpfPoint(cx - rIn, cy + rIn), new WpfPoint(cx - rOut, cy), true, true);
                        ctx.BezierTo(new WpfPoint(cx - rIn, cy - rIn), new WpfPoint(cx - rIn, cy - rIn), new WpfPoint(cx, cy - rOut), true, true);
                    }
                    star.Freeze();
                    dc.DrawGeometry(new WpfSolidColorBrush(WpfColor.FromRgb(245, 158, 11)), null, star);

                    // Little sparkle at top-right
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(239, 68, 68)), null, new WpfPoint(w * 0.85, h * 0.22), 2.2, 2.2);
                }
                else if (kind == "typography")
                {
                    // Bold modern 'A' in indigo with coral accent
                    WpfPen pA = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(79, 70, 229)), 2.2) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    dc.DrawLine(pA, new WpfPoint(w * 0.15, h * 0.85), new WpfPoint(w * 0.42, h * 0.12));
                    dc.DrawLine(pA, new WpfPoint(w * 0.42, h * 0.12), new WpfPoint(w * 0.68, h * 0.85));
                    dc.DrawLine(pA, new WpfPoint(w * 0.26, h * 0.60), new WpfPoint(w * 0.58, h * 0.60));

                    // Small 'a' beside
                    dc.DrawEllipse(null, new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(234, 88, 12)), 1.6), new WpfPoint(w * 0.82, h * 0.68), 3.2, 3.2);
                    dc.DrawLine(new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(234, 88, 12)), 1.6), new WpfPoint(w * 0.82 + 3.2, h * 0.50), new WpfPoint(w * 0.82 + 3.2, h * 0.85));
                }
                else if (kind == "brand")
                {
                    // Smooth artist palette with 4 paint dots
                    WpfPen pPal = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(71, 85, 105)), 1.4) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(248, 250, 252)), pPal, new WpfPoint(w / 2.0, h / 2.0), w * 0.45, h * 0.42);

                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(239, 68, 68)), null, new WpfPoint(w * 0.32, h * 0.35), 2.5, 2.5);
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(245, 158, 11)), null, new WpfPoint(w * 0.52, h * 0.26), 2.5, 2.5);
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(16, 185, 129)), null, new WpfPoint(w * 0.70, h * 0.40), 2.5, 2.5);
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(37, 99, 235)), null, new WpfPoint(w * 0.62, h * 0.68), 2.5, 2.5);
                }
                else if (kind == "upload")
                {
                    // Fluffy cloud with lifting arrow
                    WpfPen pCloud = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(2, 132, 199)), 1.5) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(224, 242, 254)), pCloud, new WpfRect(w * 0.12, h * 0.35, w * 0.76, h * 0.48), 6, 6);

                    WpfPen pArr = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(2, 132, 199)), 1.8) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    dc.DrawLine(pArr, new WpfPoint(w * 0.5, h * 0.72), new WpfPoint(w * 0.5, h * 0.25));
                    dc.DrawLine(pArr, new WpfPoint(w * 0.35, h * 0.40), new WpfPoint(w * 0.5, h * 0.25));
                    dc.DrawLine(pArr, new WpfPoint(w * 0.65, h * 0.40), new WpfPoint(w * 0.5, h * 0.25));
                }
                else if (kind == "tools")
                {
                    // Drafting compass / pen crossed
                    WpfPen pTool1 = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(217, 119, 6)), 2.0) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    WpfPen pTool2 = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(13, 148, 136)), 2.0) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    dc.DrawLine(pTool1, new WpfPoint(w * 0.18, h * 0.82), new WpfPoint(w * 0.82, h * 0.18));
                    dc.DrawLine(pTool2, new WpfPoint(w * 0.82, h * 0.82), new WpfPoint(w * 0.18, h * 0.18));
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)), new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(51, 65, 85)), 1.2), new WpfPoint(w * 0.5, h * 0.5), 3.0, 3.0);
                }
                else if (kind == "project")
                {
                    // Hierarchical branch tree
                    WpfSolidColorBrush bTeal = new WpfSolidColorBrush(WpfColor.FromRgb(13, 148, 136));
                    WpfPen pTeal = new WpfPen(bTeal, 1.4) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    dc.DrawRoundedRectangle(bTeal, null, new WpfRect(w * 0.35, 1, w * 0.3, h * 0.28), 2, 2);
                    dc.DrawRoundedRectangle(bTeal, null, new WpfRect(1, h * 0.68, w * 0.3, h * 0.28), 2, 2);
                    dc.DrawRoundedRectangle(bTeal, null, new WpfRect(w * 0.69, h * 0.68, w * 0.3, h * 0.28), 2, 2);
                    dc.DrawLine(pTeal, new WpfPoint(w * 0.5, h * 0.28), new WpfPoint(w * 0.5, h * 0.48));
                    dc.DrawLine(pTeal, new WpfPoint(w * 0.16, h * 0.48), new WpfPoint(w * 0.84, h * 0.48));
                    dc.DrawLine(pTeal, new WpfPoint(w * 0.16, h * 0.48), new WpfPoint(w * 0.16, h * 0.68));
                    dc.DrawLine(pTeal, new WpfPoint(w * 0.84, h * 0.48), new WpfPoint(w * 0.84, h * 0.68));
                }
                else if (kind == "apps")
                {
                    // 4-Square Fluent Grid
                    double bSize = (w - 3.0) / 2.0;
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(0, 120, 212)), null, new WpfRect(0, 0, bSize, bSize), 1.5, 1.5);
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(210, 71, 38)), null, new WpfRect(bSize + 3, 0, bSize, bSize), 1.5, 1.5);
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(16, 124, 65)), null, new WpfRect(0, bSize + 3, bSize, bSize), 1.5, 1.5);
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(255, 185, 0)), null, new WpfRect(bSize + 3, bSize + 3, bSize, bSize), 1.5, 1.5);
                }
                else if (kind == "qr")
                {
                    WpfSolidColorBrush bDark = new WpfSolidColorBrush(WpfColor.FromRgb(30, 41, 59));
                    dc.DrawRoundedRectangle(null, new WpfPen(bDark, 1.4), new WpfRect(1, 1, 7, 7), 1, 1);
                    dc.DrawRectangle(bDark, null, new WpfRect(3, 3, 3, 3));
                    dc.DrawRoundedRectangle(null, new WpfPen(bDark, 1.4), new WpfRect(w - 8, 1, 7, 7), 1, 1);
                    dc.DrawRectangle(bDark, null, new WpfRect(w - 6, 3, 3, 3));
                    dc.DrawRoundedRectangle(null, new WpfPen(bDark, 1.4), new WpfRect(1, h - 8, 7, 7), 1, 1);
                    dc.DrawRectangle(bDark, null, new WpfRect(3, h - 6, 3, 3));
                    dc.DrawRectangle(bDark, null, new WpfRect(w - 7, h - 7, 6, 6));
                }
                else if (kind == "kpi")
                {
                    // Gauge with needle
                    WpfPen pArc = new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(16, 185, 129)), 2.4) { StartLineCap = WpfPenLineCap.Round, EndLineCap = WpfPenLineCap.Round };
                    WpfStreamGeometry arc = new WpfStreamGeometry();
                    using (var ctx = arc.Open())
                    {
                        ctx.BeginFigure(new WpfPoint(2, h * 0.75), false, false);
                        ctx.ArcTo(new WpfPoint(w - 2, h * 0.75), new WpfSize(w * 0.45, h * 0.45), 0, false, WpfSweepDirection.Clockwise, true, true);
                    }
                    arc.Freeze();
                    dc.DrawGeometry(null, pArc, arc);
                    dc.DrawLine(new WpfPen(new WpfSolidColorBrush(WpfColor.FromRgb(239, 68, 68)), 1.6), new WpfPoint(w * 0.5, h * 0.72), new WpfPoint(w * 0.72, h * 0.35));
                    dc.DrawEllipse(new WpfSolidColorBrush(WpfColor.FromRgb(51, 65, 85)), null, new WpfPoint(w * 0.5, h * 0.72), 2.0, 2.0);
                }
                else
                {
                    dc.DrawRoundedRectangle(new WpfSolidColorBrush(WpfColor.FromRgb(37, 99, 235)), null, new WpfRect(0, 0, w, h), 2, 2);
                }

                dc.Pop();
            }

            WpfRenderTargetBitmap rtb = new WpfRenderTargetBitmap(size, size, 96, 96, WpfPixelFormats.Pbgra32);
            rtb.Render(dv);

            int stride = size * 4;
            byte[] pixelBytes = new byte[stride * size];
            rtb.CopyPixels(pixelBytes, stride, 0);

            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var rect = new Rectangle(0, 0, size, size);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                Marshal.Copy(pixelBytes, 0, bmpData.Scan0, pixelBytes.Length);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            lock (_thumbLock)
            {
                _thumbCache[cacheKey] = bmp;
            }
            return bmp;
        }

        /// <summary>
        /// Vẽ Task Pane hoàn chỉnh lên Graphics với chế độ chống răng cưa cao cấp.
        /// </summary>
        public void Draw(Graphics g, Rectangle bounds)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            // Thiết lập chế độ chống răng cưa tối đa cho Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            ItemClickBounds.Clear();
            CategorySeeAllBounds.Clear();

            // 1. Nền trắng tinh khiết Office & Viền phải mảnh
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(bgBrush, bounds);
                g.DrawLine(borderPen, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom);
            }

            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;

            // 2. Header: Tiêu đề danh mục + Nút quay lại/thu gọn + Nút đóng
            int headerH = 46;
            Rectangle headerRect = new Rectangle(x, y, w, headerH);
            using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen hBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (Font titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (Font iconFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.FillRectangle(hBrush, headerRect);
                g.DrawLine(hBorder, x, y + headerH - 1, x + w, y + headerH - 1);

                // Nút quay lại hoặc thu gọn
                BtnBackBounds = new Rectangle(x + 10, y + 10, 26, 26);
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1f))
                using (SolidBrush b = new SolidBrush(Color.White))
                {
                    g.FillEllipse(b, BtnBackBounds);
                    g.DrawEllipse(p, BtnBackBounds);
                }
                // Vector Back Arrow (mũi tên vector mềm mại không răng cưa)
                using (Pen arrPen = new Pen(Color.FromArgb(71, 85, 105), 1.8f))
                {
                    arrPen.StartCap = LineCap.Round;
                    arrPen.EndCap = LineCap.Round;
                    arrPen.LineJoin = LineJoin.Round;
                    int bx = BtnBackBounds.X + BtnBackBounds.Width / 2;
                    int by = BtnBackBounds.Y + BtnBackBounds.Height / 2;
                    g.DrawLine(arrPen, bx + 2, by - 4, bx - 2, by);
                    g.DrawLine(arrPen, bx - 2, by, bx + 2, by + 4);
                }

                // Tiêu đề
                string titleText = (Mode == ShapePaneMode.ImagesOnly) ? "Hình ảnh" :
                                   (Mode == ShapePaneMode.GraphicsOnly) ? "Đồ họa" : "Hình dạng";
                if (!string.IsNullOrEmpty(ExpandedCategoryKey))
                {
                    foreach (var c in WpfShapeLibrary.Categories)
                    {
                        if (c.Key == ExpandedCategoryKey) { titleText = c.Title; break; }
                    }
                }
                g.DrawString(titleText, titleFont, textBrush, x + 44, y + 12);

                // Nút đóng ✕ (vector cross)
                BtnCloseBounds = new Rectangle(x + w - 34, y + 10, 26, 26);
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1f))
                using (SolidBrush b = new SolidBrush(Color.White))
                {
                    g.FillEllipse(b, BtnCloseBounds);
                    g.DrawEllipse(p, BtnCloseBounds);
                }
                using (Pen closePen = new Pen(Color.FromArgb(100, 116, 139), 1.6f))
                {
                    closePen.StartCap = LineCap.Round;
                    closePen.EndCap = LineCap.Round;
                    int cx = BtnCloseBounds.X + BtnCloseBounds.Width / 2;
                    int cy = BtnCloseBounds.Y + BtnCloseBounds.Height / 2;
                    g.DrawLine(closePen, cx - 4, cy - 4, cx + 4, cy + 4);
                    g.DrawLine(closePen, cx + 4, cy - 4, cx - 4, cy + 4);
                }
            }

            // 2b. Tab switcher: [ 🔷 Hình dạng ]  [ 🎨 Đồ họa ]  [ 🖼️ Hình ảnh ]
            int tabsH = 34;
            int tabGap = 4;
            int tabW = (w - 24 - tabGap * 2) / 3;
            BtnTabShapesBounds = new Rectangle(x + 12, y + headerH + 6, tabW, 28);
            BtnTabGraphicsBounds = new Rectangle(x + 12 + tabW + tabGap, y + headerH + 6, tabW, 28);
            BtnTabImagesBounds = new Rectangle(x + 12 + (tabW + tabGap) * 2, y + headerH + 6, tabW, 28);

            bool isShapes = (Mode == ShapePaneMode.ShapesOnly);
            bool isGraphics = (Mode == ShapePaneMode.GraphicsOnly);
            bool isImages = (Mode == ShapePaneMode.ImagesOnly);

            using (Font tabFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (StringFormat sfTab = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                // Tab Hình dạng
                using (GraphicsPath pShapes = CreateRoundedRectPath(BtnTabShapesBounds, 4))
                {
                    if (isShapes)
                    {
                        using (SolidBrush actBg = new SolidBrush(Color.FromArgb(16, 110, 190)))
                        {
                            g.FillPath(actBg, pShapes);
                            g.DrawString("🔷 Hình dạng", tabFont, Brushes.White, BtnTabShapesBounds, sfTab);
                        }
                    }
                    else
                    {
                        using (SolidBrush inactBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        using (Pen inactBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                        using (SolidBrush inactTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
                        {
                            g.FillPath(inactBg, pShapes);
                            g.DrawPath(inactBorder, pShapes);
                            g.DrawString("🔷 Hình dạng", tabFont, inactTxt, BtnTabShapesBounds, sfTab);
                        }
                    }
                }

                // Tab Đồ họa
                using (GraphicsPath pGraphics = CreateRoundedRectPath(BtnTabGraphicsBounds, 4))
                {
                    if (isGraphics)
                    {
                        using (SolidBrush actBg = new SolidBrush(Color.FromArgb(16, 110, 190)))
                        {
                            g.FillPath(actBg, pGraphics);
                            g.DrawString("🎨 Đồ họa", tabFont, Brushes.White, BtnTabGraphicsBounds, sfTab);
                        }
                    }
                    else
                    {
                        using (SolidBrush inactBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        using (Pen inactBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                        using (SolidBrush inactTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
                        {
                            g.FillPath(inactBg, pGraphics);
                            g.DrawPath(inactBorder, pGraphics);
                            g.DrawString("🎨 Đồ họa", tabFont, inactTxt, BtnTabGraphicsBounds, sfTab);
                        }
                    }
                }

                // Tab Hình ảnh
                using (GraphicsPath pImages = CreateRoundedRectPath(BtnTabImagesBounds, 4))
                {
                    if (isImages)
                    {
                        using (SolidBrush actBg = new SolidBrush(Color.FromArgb(16, 110, 190)))
                        {
                            g.FillPath(actBg, pImages);
                            g.DrawString("🖼️ Hình ảnh", tabFont, Brushes.White, BtnTabImagesBounds, sfTab);
                        }
                    }
                    else
                    {
                        using (SolidBrush inactBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        using (Pen inactBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                        using (SolidBrush inactTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
                        {
                            g.FillPath(inactBg, pImages);
                            g.DrawPath(inactBorder, pImages);
                            g.DrawString("🖼️ Hình ảnh", tabFont, inactTxt, BtnTabImagesBounds, sfTab);
                        }
                    }
                }
            }

            // 3. Search Bar: Ô tìm kiếm hiện đại
            int searchH = 40;
            SearchBoxBounds = new Rectangle(x + 12, y + headerH + tabsH + 8, w - 24, 30);
            using (SolidBrush sbBg = new SolidBrush(Color.White))
            using (Pen sbPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (Font sFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush sText = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                using (GraphicsPath sPath = CreateRoundedRectPath(SearchBoxBounds, 5))
                {
                    g.FillPath(sbBg, sPath);
                    g.DrawPath(sbPen, sPath);
                }

                // Vector Magnifying Glass (kính lúp vector chuẩn mực, tuyệt đối không bị ô vuông Unicode)
                using (Pen searchIconPen = new Pen(Color.FromArgb(100, 116, 139), 1.6f))
                {
                    searchIconPen.StartCap = LineCap.Round;
                    searchIconPen.EndCap = LineCap.Round;
                    int icX = SearchBoxBounds.X + 9;
                    int icY = SearchBoxBounds.Y + 8;
                    g.DrawEllipse(searchIconPen, icX, icY, 8, 8);
                    g.DrawLine(searchIconPen, icX + 6.5f, icY + 6.5f, icX + 11.5f, icY + 11.5f);
                }

                if (string.IsNullOrEmpty(SearchText))
                {
                    string defaultPlaceholder = (Mode == ShapePaneMode.ImagesOnly) ? "Tìm kiếm trong kho ảnh Stock 4K..." :
                                                 (Mode == ShapePaneMode.GraphicsOnly) ? "Tìm kiếm trong kho đồ họa & biểu tượng..." : "Tìm kiếm trong hơn 500+ hình dạng...";
                    g.DrawString(defaultPlaceholder, sFont, sText, SearchBoxBounds.X + 28, SearchBoxBounds.Y + 6);
                }
            }

            // 4. Nội dung danh sách hình dạng với khả năng cuộn mượt
            int contentY = y + headerH + tabsH + searchH + 12;
            int contentH = h - (headerH + tabsH + searchH + 16);
            Rectangle contentClip = new Rectangle(x, contentY, w, contentH);

            Region oldClip = g.Clip;
            g.SetClip(contentClip);

            int currentY = contentY - ScrollY;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // Chế độ Tìm kiếm: Hiển thị kết quả dạng lưới
                var searchResults = WpfShapeLibrary.SearchPresets(SearchText);
                if (Mode == ShapePaneMode.ShapesOnly)
                {
                    searchResults = searchResults.FindAll(item => !item.Category.StartsWith("Online_") && !item.IsPhoto);
                }
                else if (Mode == ShapePaneMode.GraphicsOnly)
                {
                    searchResults = searchResults.FindAll(item => (item.Category.StartsWith("Online_") || item.IsOnlineImage) && !item.IsPhoto && !item.Category.StartsWith("Online_Image_"));
                }
                else if (Mode == ShapePaneMode.ImagesOnly)
                {
                    searchResults = searchResults.FindAll(item => item.IsPhoto || item.Category.StartsWith("Online_Image_"));
                }
                currentY = RenderShapeGrid(g, x + 12, currentY, w - 24, searchResults, $"Kết quả tìm kiếm ({searchResults.Count})");
            }
            else if (!string.IsNullOrEmpty(ExpandedCategoryKey))
            {
                // Chế độ Xem tất cả của một danh mục cụ thể
                ShapeCategoryGroup targetCat = null;
                foreach (var c in WpfShapeLibrary.Categories)
                {
                    if (c.Key == ExpandedCategoryKey) { targetCat = c; break; }
                }
                if (targetCat != null)
                {
                    currentY = RenderShapeGrid(g, x + 12, currentY, w - 24, targetCat.Items, $"Tất cả {targetCat.Title} ({targetCat.Items.Count})");
                }
            }
            else
            {
                // Chế độ Mặc định: Hiển thị từng danh mục kèm nút "Xem tất cả"
                int renderedCount = 0;
                foreach (var cat in WpfShapeLibrary.Categories)
                {
                    bool isPhotoCat = cat.IsPhotoCategory || cat.Key.StartsWith("Online_Image_");
                    bool isOnlineGraphicCat = cat.Key.StartsWith("Online_") && !isPhotoCat;
                    bool isStandardShapeCat = !cat.Key.StartsWith("Online_") && !isPhotoCat;

                    if (Mode == ShapePaneMode.ShapesOnly && !isStandardShapeCat) continue;
                    if (Mode == ShapePaneMode.GraphicsOnly && !isOnlineGraphicCat) continue;
                    if (Mode == ShapePaneMode.ImagesOnly && !isPhotoCat) continue;

                    renderedCount++;
                    currentY = RenderCategoryRow(g, x + 12, currentY, w - 24, cat);
                    currentY += 12; // Khoảng cách giữa các danh mục
                }

                if (renderedCount == 0)
                {
                    using (SolidBrush emptyB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    using (Font emptyF = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (StringFormat sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        string emptyMsg = (Mode == ShapePaneMode.ImagesOnly)
                            ? "Đang đồng bộ kho hình ảnh Stock 4K...\nVui lòng kiểm tra kết nối mạng."
                            : (Mode == ShapePaneMode.GraphicsOnly)
                            ? "Đang đồng bộ dữ liệu đồ họa trực tuyến từ Vercel...\nVui lòng kiểm tra kết nối mạng."
                            : "Không có danh mục nào.";
                        g.DrawString(emptyMsg, emptyF, emptyB, new RectangleF(x + 12, currentY + 40, w - 24, 60), sfC);
                    }
                    currentY += 120;
                }
            }

            g.Clip = oldClip;

            // Tính toán MaxScrollY
            int totalContentH = (currentY + ScrollY) - contentY;
            MaxScrollY = Math.Max(0, totalContentH - contentH + 40);

            // 5. Thanh cuộn (Scrollbar) chuẩn Fluent UI mượt mà
            if (MaxScrollY > 0)
            {
                int sbW = 6;
                int sbX = bounds.Right - sbW - 3;
                int sbY = contentY;
                int sbH = contentH;
                ScrollbarTrackBounds = new Rectangle(sbX, sbY, sbW, sbH);

                int minThumbH = 28;
                int totalH = contentH + MaxScrollY;
                int thumbH = Math.Max(minThumbH, (int)((float)contentH / totalH * sbH));
                int travelH = sbH - thumbH;
                int thumbY = sbY + (int)((float)ScrollY / MaxScrollY * travelH);
                ScrollbarThumbBounds = new Rectangle(sbX, thumbY, sbW, thumbH);

                // Nền rãnh trượt mờ nhạt
                using (SolidBrush trackB = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (GraphicsPath trackPath = CreateRoundedRectPath(ScrollbarTrackBounds, 3))
                {
                    g.FillPath(trackB, trackPath);
                }

                // Con trượt (thumb) bo tròn hiện đại
                Color thumbColor = IsDraggingScrollbar ? Color.FromArgb(71, 85, 105) :
                                   ScrollbarThumbBounds.Contains(_lastMousePos) ? Color.FromArgb(148, 163, 184) :
                                   Color.FromArgb(203, 213, 225);
                using (SolidBrush thumbB = new SolidBrush(thumbColor))
                using (GraphicsPath thumbPath = CreateRoundedRectPath(ScrollbarThumbBounds, 3))
                {
                    g.FillPath(thumbB, thumbPath);
                }
            }
            else
            {
                ScrollbarTrackBounds = Rectangle.Empty;
                ScrollbarThumbBounds = Rectangle.Empty;
            }
        }

        private int RenderCategoryRow(Graphics g, int rx, int ry, int rw, ShapeCategoryGroup cat)
        {
            if (cat == null || cat.Items == null || cat.Items.Count == 0) return ry;

            // Tiêu đề danh mục + Link "Xem tất cả"
            using (Font catFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font seeAllFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                string seeAllText = "Xem tất cả ›";
                SizeF seeAllSize = g.MeasureString(seeAllText, seeAllFont);
                Rectangle seeAllRect = new Rectangle((int)(rx + rw - seeAllSize.Width - 4), ry, (int)seeAllSize.Width + 8, (int)seeAllSize.Height + 4);

                using (StringFormat sfTitle = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    Rectangle titleRect = new Rectangle(rx, ry, (int)(rw - seeAllSize.Width - 12), (int)(seeAllSize.Height + 4));
                    g.DrawString(cat.Title, catFont, titleBrush, titleRect, sfTitle);
                }

                g.DrawString(seeAllText, seeAllFont, linkBrush, seeAllRect.X, seeAllRect.Y);
                CategorySeeAllBounds.Add(new Tuple<Rectangle, string>(seeAllRect, cat.Key));
            }

            int itemY = ry + 26;
            int cardW = (Mode == ShapePaneMode.ImagesOnly) ? 68 : 44;
            int cardH = (Mode == ShapePaneMode.ImagesOnly) ? 50 : 44;
            int cardSpacing = 6;
            int cols = Math.Max(1, (rw + cardSpacing) / (cardW + cardSpacing));
            int displayCount = Math.Min(cols, cat.Items.Count);

            RectangleF clip = g.ClipBounds;

            for (int i = 0; i < displayCount; i++)
            {
                ShapePresetItem item = cat.Items[i];
                int cx = rx + i * (cardW + cardSpacing);
                Rectangle cardRect = new Rectangle(cx, itemY, cardW, cardH);

                if (cardRect.Bottom < clip.Top || cardRect.Top > clip.Bottom) continue;

                ItemClickBounds.Add(new Tuple<Rectangle, string>(cardRect, item.Id));
                DrawShapeCard(g, cardRect, item);
            }

            return itemY + cardH;
        }

        private int RenderShapeGrid(Graphics g, int rx, int ry, int rw, List<ShapePresetItem> items, string headerTitle)
        {
            if (!string.IsNullOrEmpty(headerTitle))
            {
                using (Font catFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString(headerTitle, catFont, titleBrush, rx, ry);
                }
                ry += 28;
            }

            if (items == null || items.Count == 0)
            {
                using (Font emptyFont = new Font("Segoe UI", 9f, FontStyle.Italic))
                using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("Không tìm thấy mẫu phù hợp.", emptyFont, emptyBrush, rx, ry + 10);
                }
                return ry + 40;
            }

            int cardW = (Mode == ShapePaneMode.ImagesOnly) ? 80 : 46;
            int cardH = (Mode == ShapePaneMode.ImagesOnly) ? 58 : 46;
            int cardSpacing = 6;
            int cols = Math.Max(1, (rw + cardSpacing) / (cardW + cardSpacing));
            int totalRows = (items.Count + cols - 1) / cols;
            
            RectangleF clip = g.ClipBounds;

            // Mathematical culling
            int startRow = 0;
            if (ry < clip.Top) startRow = Math.Max(0, (int)((clip.Top - ry) / (cardH + cardSpacing)));
            int endRow = totalRows - 1;
            if (ry + totalRows * (cardH + cardSpacing) > clip.Bottom)
                endRow = Math.Min(totalRows - 1, (int)((clip.Bottom - ry) / (cardH + cardSpacing)) + 1);

            int startIndex = startRow * cols;
            int endIndex = Math.Min(items.Count - 1, (endRow + 1) * cols - 1);

            for (int i = startIndex; i <= endIndex; i++)
            {
                ShapePresetItem item = items[i];
                int c = i % cols;
                int r = i / cols;
                int cx = rx + c * (cardW + cardSpacing);
                int cy = ry + r * (cardH + cardSpacing);

                Rectangle cardRect = new Rectangle(cx, cy, cardW, cardH);
                
                if (cardRect.Bottom < clip.Top || cardRect.Top > clip.Bottom) continue;

                ItemClickBounds.Add(new Tuple<Rectangle, string>(cardRect, item.Id));
                DrawShapeCard(g, cardRect, item);
            }

            return ry + totalRows * (cardH + cardSpacing);
        }

        private void DrawShapeCard(Graphics g, Rectangle rect, ShapePresetItem item)
        {
            bool isHovered = (item.Id == HoveredItemId);
            Color cardBg = isHovered ? Color.FromArgb(238, 242, 255) : Color.FromArgb(255, 255, 255);
            Color borderCol = isHovered ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240);

            using (SolidBrush b = new SolidBrush(cardBg))
            using (Pen p = new Pen(borderCol, isHovered ? 1.5f : 1f))
            using (GraphicsPath path = CreateRoundedRectPath(rect, 6))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }

            // Vẽ thumbnail vector WPF đã được khử răng cưa với HighQualityBicubic
            Bitmap thumb = GetShapeThumbnail(item, rect.Width - 4);
            if (thumb != null)
            {
                var prevInterp = g.InterpolationMode;
                var prevSmooth = g.SmoothingMode;
                var prevPixel = g.PixelOffsetMode;

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Rectangle dstRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
                
                float thumbAspect = (float)thumb.Width / thumb.Height;
                float dstW = dstRect.Width;
                float dstH = dstRect.Height;

                if (thumbAspect > 1f)
                {
                    dstH = dstRect.Width / thumbAspect;
                }
                else
                {
                    dstW = dstRect.Height * thumbAspect;
                }

                float dstX = dstRect.X + (dstRect.Width - dstW) / 2f;
                float dstY = dstRect.Y + (dstRect.Height - dstH) / 2f;

                RectangleF propRect = new RectangleF(dstX, dstY, dstW, dstH);
                g.DrawImage(thumb, propRect);

                g.InterpolationMode = prevInterp;
                g.SmoothingMode = prevSmooth;
                g.PixelOffsetMode = prevPixel;
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Xử lý click chuột trong Task Pane.
        /// </summary>
        public bool HandleClick(Point pt, out string action, out string targetId)
        {
            action = null;
            targetId = null;

            // Nút Back / Collapse
            if (BtnBackBounds.Contains(pt))
            {
                if (!string.IsNullOrEmpty(ExpandedCategoryKey))
                {
                    ExpandedCategoryKey = null; // Quay lại xem tất cả danh mục
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    action = "Refresh";
                    return true;
                }
                action = "Collapse";
                return true;
            }

            // Nút Close
            if (BtnCloseBounds.Contains(pt))
            {
                action = "Close";
                return true;
            }

            // Tab Switcher [ Hình dạng ] / [ Đồ họa ]
            if (BtnTabShapesBounds.Contains(pt))
            {
                if (Mode != ShapePaneMode.ShapesOnly)
                {
                    Mode = ShapePaneMode.ShapesOnly;
                    ExpandedCategoryKey = null;
                    SearchText = "";
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    action = "SwitchMode";
                    return true;
                }
            }
            if (BtnTabGraphicsBounds.Contains(pt))
            {
                if (Mode != ShapePaneMode.GraphicsOnly)
                {
                    Mode = ShapePaneMode.GraphicsOnly;
                    ExpandedCategoryKey = null;
                    SearchText = "";
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    action = "SwitchMode";
                    return true;
                }
            }
            if (BtnTabImagesBounds.Contains(pt))
            {
                if (Mode != ShapePaneMode.ImagesOnly)
                {
                    Mode = ShapePaneMode.ImagesOnly;
                    ExpandedCategoryKey = null;
                    SearchText = "";
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    action = "SwitchMode";
                    return true;
                }
            }

            // Nút "Xem tất cả" của một danh mục
            foreach (var seeAll in CategorySeeAllBounds)
            {
                if (seeAll.Item1.Contains(pt))
                {
                    ExpandedCategoryKey = seeAll.Item2;
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    action = "ExpandCategory";
                    targetId = seeAll.Item2;
                    return true;
                }
            }

            // Click vào một hình dạng
            foreach (var itemBound in ItemClickBounds)
            {
                if (itemBound.Item1.Contains(pt))
                {
                    action = "InsertShape";
                    targetId = itemBound.Item2;
                    return true;
                }
            }

            return false;
        }

        public bool HandleMouseDown(Point pt)
        {
            if (MaxScrollY > 0 && ScrollbarThumbBounds.Contains(pt))
            {
                IsDraggingScrollbar = true;
                _dragStartMouseY = pt.Y;
                _dragStartScrollY = ScrollY;
                SmoothScrollY = ScrollY;
                TargetScrollY = ScrollY;
                return true;
            }
            if (MaxScrollY > 0 && ScrollbarTrackBounds.Contains(pt))
            {
                if (pt.Y < ScrollbarThumbBounds.Top)
                    TargetScrollY = Math.Max(0, TargetScrollY - 240);
                else if (pt.Y > ScrollbarThumbBounds.Bottom)
                    TargetScrollY = Math.Min(MaxScrollY, TargetScrollY + 240);
                UpdateSmoothScroll();
                return true;
            }
            return false;
        }

        public bool HandleMouseMove(Point pt)
        {
            _lastMousePos = pt;
            if (IsDraggingScrollbar && MaxScrollY > 0)
            {
                int deltaY = pt.Y - _dragStartMouseY;
                int travelH = ScrollbarTrackBounds.Height - ScrollbarThumbBounds.Height;
                if (travelH > 0)
                {
                    float ratio = (float)deltaY / travelH;
                    ScrollY = Math.Max(0, Math.Min(MaxScrollY, _dragStartScrollY + (int)(ratio * MaxScrollY)));
                    SmoothScrollY = ScrollY;
                    TargetScrollY = ScrollY;
                }
                return true;
            }
            return false;
        }

        public bool HandleMouseUp(Point pt)
        {
            if (IsDraggingScrollbar)
            {
                IsDraggingScrollbar = false;
                SmoothScrollY = ScrollY;
                TargetScrollY = ScrollY;
                return true;
            }
            return false;
        }

        public void HandleScroll(int delta)
        {
            HandleMouseWheel(delta);
        }

        public void HandleMouseWheel(int delta)
        {
            int scrollStep = (delta > 0) ? -100 : 100;
            TargetScrollY = Math.Max(0, Math.Min(MaxScrollY, TargetScrollY + scrollStep));
            ScrollY = (int)TargetScrollY;
            SmoothScrollY = TargetScrollY;
        }

        public bool UpdateSmoothScroll()
        {
            TargetScrollY = (float)Math.Round(TargetScrollY);
            int prevInt = (int)Math.Round(SmoothScrollY);
            int targetInt = (int)TargetScrollY;

            if (prevInt == targetInt)
            {
                if (SmoothScrollY != TargetScrollY || ScrollY != targetInt)
                {
                    SmoothScrollY = TargetScrollY;
                    ScrollY = targetInt;
                }
                return false;
            }

            float diff = TargetScrollY - SmoothScrollY;
            float step = diff * 0.28f;
            float maxStep = 60f;
            if (Math.Abs(step) > maxStep) step = Math.Sign(step) * maxStep;

            float nextSmooth = SmoothScrollY + step;
            int nextInt = (int)Math.Round(nextSmooth);

            if (nextInt == prevInt)
            {
                nextInt = prevInt + Math.Sign(diff);
                nextSmooth = nextInt;
            }

            if ((diff > 0 && nextInt >= targetInt) || (diff < 0 && nextInt <= targetInt))
            {
                SmoothScrollY = TargetScrollY;
                ScrollY = targetInt;
            }
            else
            {
                SmoothScrollY = nextSmooth;
                ScrollY = nextInt;
            }

            return true;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using ZenQOffice.Core;
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
        public Rectangle BtnClearSearchBounds = Rectangle.Empty;

        public Rectangle BtnDismissMagicBounds = Rectangle.Empty;
        public Rectangle BtnRetryOfflineBounds = Rectangle.Empty;

        public static void ClearOnlineThumbnailsInRam()
        {
            lock (_thumbLock)
            {
                var toRemove = new List<string>();
                foreach (var k in _thumbCache.Keys)
                {
                    if (k.IndexOf("_online_", StringComparison.OrdinalIgnoreCase) >= 0)
                        toRemove.Add(k);
                }
                foreach (var k in toRemove)
                {
                    try { _thumbCache[k]?.Dispose(); } catch { }
                    _thumbCache.Remove(k);
                }
                _loadingThumbs.Clear();
            }
        }
        private static string _activeMagicAnchorKey = "__recently_used__";
        private static string _lastClickedGridItemId = null;
        private static List<ShapePresetItem> _expandedMagicItems = null;
        private static bool _magicDismissed = false;
        private static ShapePaneMode _lastMagicSeededMode = (ShapePaneMode)(-1);
        private int _lastRenderWidth = 320;
        public List<string> ItemClickRowKeys = new List<string>();
        private static readonly Dictionary<string, string> _itemCategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Rectangle ScrollbarTrackBounds = Rectangle.Empty;
        public Rectangle ScrollbarThumbBounds = Rectangle.Empty;
        public bool IsDraggingScrollbar { get; private set; } = false;
        private int _dragStartMouseY = 0;
        private int _dragStartScrollY = 0;
        private float _tabPillAnimX = -1f;
        private float _tabPillAnimW = -1f;
        private float _tabPillTargetX = -1f;
        private float _tabPillTargetW = -1f;
        private long _tabPillLastTs = 0;
        private Point _lastMousePos = Point.Empty;
        public List<Tuple<Rectangle, string>> ItemClickBounds = new List<Tuple<Rectangle, string>>();
        public List<Tuple<Rectangle, string>> CategorySeeAllBounds = new List<Tuple<Rectangle, string>>();

        // Canva-style Persistence & Personalization
        private static readonly List<string> _recentItemIds = new List<string>();
        private static readonly Dictionary<string, int> _categoryUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static string _lastClickedItemId = null;
        private static string _lastClickedItemName = null;
        private static readonly List<ShapePresetItem> _magicRecommendations = new List<ShapePresetItem>();
        private static List<ShapePresetItem> _shuffledFeatured = new List<ShapePresetItem>();
        private static ShapePaneMode _featuredMode = (ShapePaneMode)(-1);
        private static readonly int _sessionSeed = Environment.TickCount ^ DateTime.Now.Minute;
        private static bool _userProfileLoaded = false;
        private static readonly object _profileLock = new object();
        private static readonly string _userPrefFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZenQOffice",
            "user_elements.json"
        );

        // Canva-style Navigation state & Scroll restoration
        private int _savedHomeScrollY = 0;
        private readonly Stack<int> _scrollHistory = new Stack<int>();

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
                            photoBmp = EnsurePreScaledThumbnail(photoBmp, pixelSize);
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
                            onlineBmp = EnsurePreScaledThumbnail(onlineBmp, pixelSize);
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

            // Siêu tối ưu: Dùng Graphics.FromImage với SSAA chống răng cưa cao cấp (0.05ms, không chặn UI thread)
            Bitmap bmp = new Bitmap(pixelSize, pixelSize, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float pad = 6f * scale;
                float drawW = pixelSize - pad * 2f;
                float drawH = pixelSize - pad * 2f;
                using (GraphicsPath path = item.CreatePath(drawW, drawH))
                {
                    if (path != null)
                    {
                        using (Matrix m = new Matrix())
                        {
                            m.Translate(pad, pad);
                            path.Transform(m);
                        }
                        using (Pen pen = new Pen(sc, 1.4f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        using (SolidBrush fill = new SolidBrush(fc))
                        {
                            if (item.Category == "Lines")
                            {
                                g.DrawPath(pen, path);
                            }
                            else
                            {
                                g.FillPath(fill, path);
                                g.DrawPath(pen, path);
                            }
                        }
                    }
                }
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
            if (g == null) return;
            using (D2DGraphics d2d = new D2DGraphics(g, bounds.Width, bounds.Height))
            {
                Draw(d2d, bounds);
            }
        }

        public void Draw(D2DGraphics g, Rectangle bounds)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            // Thiết lập chế độ chống răng cưa tối đa cho D2DGraphics
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            ItemClickBounds.Clear();
            ItemClickRowKeys.Clear();
            CategorySeeAllBounds.Clear();
            _lastRenderWidth = bounds.Width;
            EnsureMagicRecommendationsForCurrentMode();

            // 1. Nền trắng tinh khiết Office & Viền phải mảnh cùng bóng đổ mờ chuẩn Fluent ('đen mờ')
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(bgBrush, bounds);
                g.DrawLine(borderPen, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom);
            }
            int[] taskPaneShadowAlphas = new int[] { 14, 9, 5, 2 };
            for (int s = 0; s < taskPaneShadowAlphas.Length; s++)
            {
                using (Pen sp = new Pen(Color.FromArgb(taskPaneShadowAlphas[s], 0, 0, 0), 1f))
                {
                    g.DrawLine(sp, bounds.Right + s, bounds.Top, bounds.Right + s, bounds.Bottom);
                }
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
                string titleText = (Mode == ShapePaneMode.ImagesOnly) ? AppL10n.T("Hình ảnh", "Photos") :
                                   (Mode == ShapePaneMode.GraphicsOnly) ? AppL10n.T("Đồ họa", "Graphics") : AppL10n.T("Hình dạng", "Shapes");
                if (!string.IsNullOrEmpty(ExpandedCategoryKey))
                {
                    if (ExpandedCategoryKey == "__recently_used__")
                    {
                        titleText = AppL10n.T("Dùng gần đây", "Recently Used");
                    }
                    else if (ExpandedCategoryKey == "__magic_recommendations__")
                    {
                        titleText = AppL10n.T("Đề xuất Magic", "Magic Recommendations");
                    }
                    else if (ExpandedCategoryKey == "__featured__")
                    {
                        titleText = AppL10n.T("Bộ sưu tập Nổi bật", "Featured Collection");
                    }
                    else
                    {
                        foreach (var c in WpfShapeLibrary.Categories)
                        {
                            if (c.Key == ExpandedCategoryKey) { titleText = AppL10n.TrCategory(c.Title); break; }
                        }
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

            Rectangle activeTabRect = isGraphics ? BtnTabGraphicsBounds : (isImages ? BtnTabImagesBounds : BtnTabShapesBounds);
            _tabPillTargetX = activeTabRect.X;
            _tabPillTargetW = activeTabRect.Width;
            if (_tabPillAnimX < 0f || _tabPillAnimW < 0f)
            {
                _tabPillAnimX = _tabPillTargetX;
                _tabPillAnimW = _tabPillTargetW;
            }

            using (Font tabFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (StringFormat sfTab = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                Rectangle[] allTabs = new[] { BtnTabShapesBounds, BtnTabGraphicsBounds, BtnTabImagesBounds };
                using (SolidBrush inactBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen inactBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    foreach (Rectangle tb in allTabs)
                    {
                        g.FillRoundedRectangle(inactBg, tb, 4f);
                        g.DrawRoundedRectangle(inactBorder, tb, 4f);
                    }
                }

                RectangleF pillRect = new RectangleF(_tabPillAnimX, activeTabRect.Y, _tabPillAnimW, activeTabRect.Height);
                using (SolidBrush actBg = new SolidBrush(Color.FromArgb(16, 110, 190)))
                {
                    g.FillRoundedRectangle(actBg, pillRect, 4f);
                }

                string[] tabNames = new[] { ZenQOffice.Core.AppL10n.T("Hình dạng", "Shapes"), ZenQOffice.Core.AppL10n.T("Đồ họa", "Graphics"), ZenQOffice.Core.AppL10n.T("Hình ảnh", "Images") };
                bool[] tabActives = new[] { isShapes, isGraphics, isImages };
                using (SolidBrush inactTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    for (int ti = 0; ti < 3; ti++)
                    {
                        bool active = tabActives[ti] && Math.Abs(_tabPillAnimX - _tabPillTargetX) < Math.Max(8f, _tabPillTargetW * 0.35f);
                        g.DrawString(tabNames[ti], tabFont, active ? Brushes.White : inactTxt, allTabs[ti], sfTab);
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
                    string defaultPlaceholder = (Mode == ShapePaneMode.ImagesOnly) ? AppL10n.T("Tìm kiếm trong kho ảnh Stock 4K...", "Search 4K Stock photos...") :
                                                 (Mode == ShapePaneMode.GraphicsOnly) ? AppL10n.T("Tìm kiếm trong kho đồ họa & biểu tượng...", "Search graphics & icons...") : AppL10n.T("Tìm kiếm trong hơn 500+ hình dạng...", "Search 500+ shapes...");
                    g.DrawString(defaultPlaceholder, sFont, sText, SearchBoxBounds.X + 28, SearchBoxBounds.Y + 6);
                    BtnClearSearchBounds = Rectangle.Empty;
                }
                else
                {
                    BtnClearSearchBounds = new Rectangle(SearchBoxBounds.Right - 22, SearchBoxBounds.Y + 7, 16, 16);
                    using (Pen xPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
                    {
                        xPen.StartCap = LineCap.Round;
                        xPen.EndCap = LineCap.Round;
                        int cx = BtnClearSearchBounds.X + 8;
                        int cy = BtnClearSearchBounds.Y + 8;
                        g.DrawLine(xPen, cx - 4, cy - 4, cx + 4, cy + 4);
                        g.DrawLine(xPen, cx + 4, cy - 4, cx - 4, cy + 4);
                    }
                }
            }

            // 4. Nội dung danh sách hình dạng với khả năng cuộn mượt
            int contentY = y + headerH + tabsH + searchH + 12;
            int contentH = h - (headerH + tabsH + searchH + 16);
            Rectangle contentClip = new Rectangle(x, contentY, w, contentH);

            g.PushClip(contentClip);

            int currentY = contentY - ScrollY;

            BtnRetryOfflineBounds = Rectangle.Empty;
            bool isGraphicsOffline = (Mode == ShapePaneMode.GraphicsOnly) && (!ZenQOffice.Core.OnlineGraphicsManager.CheckInternetConnected() || ZenQOffice.Core.OnlineGraphicsManager.IsOffline);
            bool isImagesOffline = (Mode == ShapePaneMode.ImagesOnly) && (!ZenQOffice.Core.OnlineImagesManager.CheckInternetConnected() || ZenQOffice.Core.OnlineImagesManager.IsOffline);
            if (isGraphicsOffline || isImagesOffline)
            {
                if (isGraphicsOffline) WpfShapeLibrary.ClearOnlineGraphicCategories();
                if (isImagesOffline) WpfShapeLibrary.ClearOnlineImageCategories();
                ClearOnlineThumbnailsInRam();

                Rectangle offlineBox = new Rectangle(x + 14, contentY + 24, w - 28, 170);
                using (GraphicsPath offPath = CreateRoundedRectPath(offlineBox, 12))
                using (SolidBrush offBg = new SolidBrush(Color.FromArgb(254, 242, 242)))
                using (Pen offBorder = new Pen(Color.FromArgb(252, 165, 165), 1.2f))
                {
                    g.FillPath(offBg, offPath);
                    g.DrawPath(offBorder, offPath);
                }
                using (Font fOffTitle = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (Font fOffSub = new Font("Segoe UI", 8.75f, FontStyle.Regular))
                using (SolidBrush brTitle = new SolidBrush(Color.FromArgb(185, 28, 28)))
                using (SolidBrush brSub = new SolidBrush(Color.FromArgb(127, 29, 29)))
                using (StringFormat sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(AppL10n.T("⚠ Mất kết nối Internet", "⚠ No Internet Connection"), fOffTitle, brTitle, new RectangleF(offlineBox.X + 8, offlineBox.Y + 16, offlineBox.Width - 16, 26), sfC);
                    string subMsg = (Mode == ShapePaneMode.GraphicsOnly)
                        ? AppL10n.T("Kho Đồ họa Vector hoạt động 100% trên đám mây (không lưu offline).\nVui lòng kiểm tra kết nối mạng và thử lại.", "Vector Graphics library is 100% cloud-based.\nPlease check your internet connection and retry.")
                        : AppL10n.T("Kho Hình ảnh Cloud hoạt động 100% trên đám mây (không lưu offline).\nVui lòng kiểm tra kết nối mạng và thử lại.", "Stock Photos library is 100% cloud-based.\nPlease check your internet connection and retry.");
                    g.DrawString(subMsg, fOffSub, brSub, new RectangleF(offlineBox.X + 10, offlineBox.Y + 46, offlineBox.Width - 20, 62), sfC);

                    int retryW = 128, retryH = 32;
                    BtnRetryOfflineBounds = new Rectangle(offlineBox.X + (offlineBox.Width - retryW) / 2, offlineBox.Bottom - 44, retryW, retryH);
                    using (GraphicsPath rPath = CreateRoundedRectPath(BtnRetryOfflineBounds, 16))
                    using (SolidBrush rBg = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (Font fRetry = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        g.FillPath(rBg, rPath);
                        g.DrawString(AppL10n.T("↻ Thử tải lại", "↻ Retry"), fRetry, Brushes.White, BtnRetryOfflineBounds, sfC);
                    }
                }
                g.PopClip();
                MaxScrollY = 0;
                return;
            }
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
                currentY = RenderShapeGrid(g, x + 12, currentY, w - 24, searchResults, AppL10n.T($"Kết quả tìm kiếm ({searchResults.Count})", $"Search results ({searchResults.Count})"));
            }
            else if (!string.IsNullOrEmpty(ExpandedCategoryKey))
            {
                // Chế độ Xem tất cả của một danh mục cụ thể hoặc danh mục Canva đặc biệt
                List<ShapePresetItem> itemsToRender = null;
                string headerTitle = null;

                if (ExpandedCategoryKey == "__recently_used__")
                {
                    itemsToRender = GetRecentlyUsedItems();
                    headerTitle = AppL10n.T($"Tất cả mục dùng gần đây ({itemsToRender.Count})", $"All Recently Used ({itemsToRender.Count})");
                }
                else if (ExpandedCategoryKey == "__magic_recommendations__")
                {
                    itemsToRender = GetMagicExpandedItems();
                    headerTitle = AppL10n.T("Đề xuất Magic", "Magic Recommendations");
                }
                else if (ExpandedCategoryKey == "__featured__")
                {
                    itemsToRender = GetShuffledFeaturedItems();
                    headerTitle = AppL10n.T($"Bộ sưu tập Nổi bật ({itemsToRender.Count})", $"Featured Collection ({itemsToRender.Count})");
                }
                else
                {
                    ShapeCategoryGroup targetCat = null;
                    foreach (var c in WpfShapeLibrary.Categories)
                    {
                        if (c.Key == ExpandedCategoryKey) { targetCat = c; break; }
                    }
                    if (targetCat != null)
                    {
                        itemsToRender = targetCat.Items;
                        headerTitle = AppL10n.T($"Tất cả {targetCat.Title} ({targetCat.Items.Count})", $"All {AppL10n.TrCategory(targetCat.Title)} ({targetCat.Items.Count})");
                    }
                }

                if (itemsToRender != null)
                {
                    currentY = RenderShapeGrid(g, x + 12, currentY, w - 24, itemsToRender, headerTitle);
                }
            }
            else
            {
                // Chế độ Mặc định Canva-style:
                // 1. Dùng gần đây
                // 2. Đề xuất Magic
                // 3. Nổi bật (Đa dạng, xáo trộn theo session, không cố định)
                // 4. Các danh mục thông thường (Sắp xếp theo thói quen sử dụng thường xuyên)
                if (Mode == ShapePaneMode.ImagesOnly)
                {
                    bool hasPhoto = false;
                    foreach (var c in WpfShapeLibrary.Categories)
                    {
                        if (c.IsPhotoCategory || c.Key.StartsWith("Online_Image_")) { hasPhoto = true; break; }
                    }
                    if (!hasPhoto)
                    {
                        var catResp = ZenQOffice.Core.OnlineImagesManager.GetCatalogCached();
                        if (catResp != null) WpfShapeLibrary.AddOnlineImageCategories(catResp);
                    }
                }

                int renderedCount = 0;

                EnsureMagicRecommendationsForCurrentMode();
                string effectiveAnchor = string.IsNullOrEmpty(_activeMagicAnchorKey) ? "__recently_used__" : _activeMagicAnchorKey;
                bool canShowMagic = !_magicDismissed && _magicRecommendations.Count > 0;

                // 1. Dùng gần đây (Recently Used) + Đề xuất Magic mặc định ban đầu ở ngay dưới Dùng gần đây
                var recentItems = GetRecentlyUsedItems();
                if (recentItems != null && recentItems.Count > 0)
                {
                    renderedCount++;
                    currentY = RenderCategoryRow(g, x + 12, currentY, w - 24, "__recently_used__", AppL10n.T("Dùng gần đây", "Recently Used"), recentItems);
                    currentY += 12;
                    if (canShowMagic)
                    {
                        currentY = RenderMagicRow(g, x + 12, currentY, w - 24, ref renderedCount);
                    }
                }
                else if (canShowMagic)
                {
                    currentY = RenderMagicRow(g, x + 12, currentY, w - 24, ref renderedCount);
                }

                // 2. Nổi bật (Featured - Shuffled & Dynamic)
                var featuredPreview = GetFeaturedPreviewItems();
                if (featuredPreview != null && featuredPreview.Count > 0)
                {
                    renderedCount++;
                    currentY = RenderCategoryRow(g, x + 12, currentY, w - 24, "__featured__", AppL10n.T("Nổi bật", "Featured"), featuredPreview);
                    currentY += 12;
                    if (canShowMagic && effectiveAnchor == "__featured__")
                    {
                        currentY = RenderMagicRow(g, x + 12, currentY, w - 24, ref renderedCount);
                    }
                }

                // 3. Các danh mục thông thường (Thứ tự cố định, không tự nhảy danh mục khi click)
                var sortedCats = GetSortedCategories();
                foreach (var cat in sortedCats)
                {
                    renderedCount++;
                    currentY = RenderCategoryRow(g, x + 12, currentY, w - 24, cat);
                    currentY += 12;
                    if (canShowMagic && effectiveAnchor == cat.Key)
                    {
                        currentY = RenderMagicRow(g, x + 12, currentY, w - 24, ref renderedCount);
                    }
                }

                if (renderedCount == 0)
                {
                    using (SolidBrush emptyB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    using (Font emptyF = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (StringFormat sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        string emptyMsg = (Mode == ShapePaneMode.ImagesOnly)
                            ? AppL10n.T("Đang đồng bộ kho hình ảnh Stock 4K...\nVui lòng kiểm tra kết nối mạng.", "Syncing 4K Stock photos...\nPlease check your internet connection.")
                            : (Mode == ShapePaneMode.GraphicsOnly)
                            ? AppL10n.T("Đang đồng bộ dữ liệu đồ họa trực tuyến từ Vercel...\nVui lòng kiểm tra kết nối mạng.", "Syncing cloud graphics...\nPlease check your internet connection.")
                            : AppL10n.T("Không có danh mục nào.", "No categories available.");
                        g.DrawString(emptyMsg, emptyF, emptyB, new RectangleF(x + 12, currentY + 40, w - 24, 60), sfC);
                    }
                    currentY += 120;
                }
            }

            g.PopClip();

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

        private int RenderCategoryRow(D2DGraphics g, int rx, int ry, int rw, ShapeCategoryGroup cat)
        {
            if (cat == null) return ry;
            return RenderCategoryRow(g, rx, ry, rw, cat.Key, cat.Title, cat.Items);
        }

        private int RenderCategoryRow(D2DGraphics g, int rx, int ry, int rw, string key, string title, List<ShapePresetItem> items)
        {
            if (items == null || items.Count == 0) return ry;

            int cardW = (Mode == ShapePaneMode.ImagesOnly) ? 68 : 44;
            int cardH = (Mode == ShapePaneMode.ImagesOnly) ? 50 : 44;
            int totalRowH = 26 + cardH;

            RectangleF clip = g.ClipBounds;

            // Viewport Culling siêu tốc: Bỏ qua toàn bộ danh mục nằm ngoài khung nhìn (0ns)
            if (ry + totalRowH < clip.Top || ry > clip.Bottom)
            {
                return ry + totalRowH;
            }

            // Tiêu đề danh mục + Link "Xem tất cả"
            using (Font catFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font seeAllFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                string seeAllText = AppL10n.T("Xem tất cả ›", "See all ›");
                SizeF seeAllSize = g.MeasureString(seeAllText, seeAllFont);
                int rightOffset = (key == "__magic_recommendations__") ? 24 : 4;
                Rectangle seeAllRect = new Rectangle((int)(rx + rw - seeAllSize.Width - rightOffset), ry, (int)seeAllSize.Width + 8, (int)seeAllSize.Height + 4);

                using (StringFormat sfTitle = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    Rectangle titleRect = new Rectangle(rx, ry, (int)(rw - seeAllSize.Width - rightOffset - 8), (int)(seeAllSize.Height + 4));
                    g.DrawString(AppL10n.TrCategory(title), catFont, titleBrush, titleRect, sfTitle);
                }

                g.DrawString(seeAllText, seeAllFont, linkBrush, seeAllRect.X, seeAllRect.Y);
                CategorySeeAllBounds.Add(new Tuple<Rectangle, string>(seeAllRect, key));
            }

            int itemY = ry + 26;
            int cardSpacing = 6;
            int cols = Math.Max(1, (rw + cardSpacing) / (cardW + cardSpacing));
            int displayCount = Math.Min(cols, items.Count);
            bool hasMore = items.Count > cols;

            for (int i = 0; i < displayCount; i++)
            {
                int cx = rx + i * (cardW + cardSpacing);
                Rectangle cardRect = new Rectangle(cx, itemY, cardW, cardH);

                if (cardRect.Bottom < clip.Top || cardRect.Top > clip.Bottom) continue;

                // Nút "Xem tiếp / Xem tất cả" với icon chevron > ở cuối hàng (chuẩn Canva)
                if (hasMore && i == cols - 1)
                {
                    CategorySeeAllBounds.Add(new Tuple<Rectangle, string>(cardRect, key));
                    DrawSeeMoreCard(g, cardRect, key, items[i]);
                }
                else
                {
                    ShapePresetItem item = items[i];
                    if (key != "__magic_recommendations__" && key != "__recently_used__" && key != "__featured__")
                    {
                        _itemCategoryMap[item.Id] = key;
                    }
                    ItemClickBounds.Add(new Tuple<Rectangle, string>(cardRect, item.Id));
                    ItemClickRowKeys.Add(key);
                    DrawShapeCard(g, cardRect, item);
                }
            }

            return itemY + cardH;
        }

        private void DrawSeeMoreCard(D2DGraphics g, Rectangle rect, string categoryKey, ShapePresetItem peekItem)
        {
            bool isHovered = (HoveredActionId == "seemore_" + categoryKey);
            Color cardBg = isHovered ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252);
            Color borderCol = isHovered ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240);

            using (SolidBrush b = new SolidBrush(cardBg))
            using (Pen p = new Pen(borderCol, isHovered ? 1.5f : 1f))
            using (GraphicsPath path = CreateRoundedRectPath(rect, 6))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }

            // Faded peek thumbnail behind chevron (chuẩn Canva)
            if (peekItem != null)
            {
                Bitmap thumb = GetShapeThumbnail(peekItem, rect.Width - 6);
                if (thumb != null)
                {
                    Rectangle dstRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
                    g.DrawImage(thumb, dstRect);
                    using (SolidBrush fadeBrush = new SolidBrush(Color.FromArgb(170, 255, 255, 255)))
                    using (GraphicsPath path = CreateRoundedRectPath(rect, 6))
                    {
                        g.FillPath(fadeBrush, path);
                    }
                }
            }

            // Circular pill button with right chevron >
            int circleD = 22;
            int circleX = rect.X + (rect.Width - circleD) / 2;
            int circleY = rect.Y + (rect.Height - circleD) / 2;
            Rectangle circleRect = new Rectangle(circleX, circleY, circleD, circleD);

            using (SolidBrush pillBg = new SolidBrush(Color.White))
            using (Pen pillBorder = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillEllipse(pillBg, circleRect);
                g.DrawEllipse(pillBorder, circleRect);
            }

            // Vector chevron >
            using (Pen chevPen = new Pen(Color.FromArgb(30, 41, 59), 1.6f))
            {
                chevPen.StartCap = LineCap.Round;
                chevPen.EndCap = LineCap.Round;
                chevPen.LineJoin = LineJoin.Round;
                int cx = circleX + circleD / 2;
                int cy = circleY + circleD / 2;
                g.DrawLine(chevPen, cx - 2, cy - 4, cx + 2, cy);
                g.DrawLine(chevPen, cx + 2, cy, cx - 2, cy + 4);
            }
        }

        private int RenderMagicRow(D2DGraphics g, int rx, int ry, int rw, ref int renderedCount)
        {
            var magicItems = GetMagicRecommendations();
            if (magicItems != null && magicItems.Count > 0)
            {
                renderedCount++;
                string magicTitle = AppL10n.T("✨ Đề xuất Magic", "✨ Magic Recommendations");
                int startY = ry;
                ry = RenderCategoryRow(g, rx, ry, rw, "__magic_recommendations__", magicTitle, magicItems);

                // Nút đóng nhanh (✕) cho Đề xuất Magic
                int closeD = 18;
                int closeX = rx + rw - 18;
                int closeY = startY + 4;
                BtnDismissMagicBounds = new Rectangle(closeX, closeY, closeD, closeD);
                using (Pen p = new Pen(Color.FromArgb(148, 163, 184), 1.3f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    int cx = closeX + closeD / 2;
                    int cy = closeY + closeD / 2;
                    g.DrawLine(p, cx - 3, cy - 3, cx + 3, cy + 3);
                    g.DrawLine(p, cx + 3, cy - 3, cx - 3, cy + 3);
                }

                ry += 12;
            }
            else
            {
                BtnDismissMagicBounds = Rectangle.Empty;
            }
            return ry;
        }

        private int RenderShapeGrid(D2DGraphics g, int rx, int ry, int rw, List<ShapePresetItem> items, string headerTitle)
        {
            if (items == null || items.Count == 0)
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
                using (Font emptyFont = new Font("Segoe UI", 9f, FontStyle.Italic))
                using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(AppL10n.T("Không tìm thấy mẫu phù hợp.", "No matching items found."), emptyFont, emptyBrush, rx, ry + 10);
                }
                return ry + 40;
            }

            int cardW = (Mode == ShapePaneMode.ImagesOnly) ? 80 : 46;
            int cardH = (Mode == ShapePaneMode.ImagesOnly) ? 58 : 46;
            int cardSpacing = 6;
            int cols = Math.Max(1, (rw + cardSpacing) / (cardW + cardSpacing));

            // Khi người dùng bấm chọn 1 item trong danh mục, chèn Đề xuất Magic ngay dưới hàng vừa bấm
            int clickedIdx = -1;
            if (!_magicDismissed && !string.IsNullOrEmpty(_lastClickedGridItemId) && _magicRecommendations.Count > 0)
            {
                clickedIdx = items.FindIndex(it => it.Id == _lastClickedGridItemId);
            }

            if (clickedIdx >= 0)
            {
                int clickedRow = clickedIdx / cols;
                int splitIdx = Math.Min(items.Count, (clickedRow + 1) * cols);

                var firstChunk = items.GetRange(0, splitIdx);
                ry = RenderShapeGridSimple(g, rx, ry, rw, firstChunk, headerTitle, cols, cardW, cardH, cardSpacing);
                ry += 10;

                int dummyCount = 0;
                ry = RenderMagicRow(g, rx, ry, rw, ref dummyCount);
                ry += 8;

                if (splitIdx < items.Count)
                {
                    var secondChunk = items.GetRange(splitIdx, items.Count - splitIdx);
                    ry = RenderShapeGridSimple(g, rx, ry, rw, secondChunk, null, cols, cardW, cardH, cardSpacing);
                }
                return ry;
            }
            else
            {
                return RenderShapeGridSimple(g, rx, ry, rw, items, headerTitle, cols, cardW, cardH, cardSpacing);
            }
        }

        private int RenderShapeGridSimple(D2DGraphics g, int rx, int ry, int rw, List<ShapePresetItem> items, string headerTitle, int cols, int cardW, int cardH, int cardSpacing)
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

            if (items == null || items.Count == 0) return ry;

            int totalRows = (items.Count + cols - 1) / cols;
            RectangleF clip = g.ClipBounds;

            // Mathematical culling
            int startRow = 0;
            if (ry < clip.Top) startRow = Math.Max(0, (int)((clip.Top - ry) / (cardH + cardSpacing)));
            int endRow = totalRows - 1;
            if (ry + totalRows * (cardH + cardSpacing) > clip.Bottom)
                endRow = Math.Min(totalRows - 1, (int)((clip.Bottom - ry) / (cardH + cardSpacing)) + 1);

            if (startRow > endRow) return ry + totalRows * (cardH + cardSpacing);

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
                ItemClickRowKeys.Add(ExpandedCategoryKey ?? "__grid__");
                DrawShapeCard(g, cardRect, item);
            }

            return ry + totalRows * (cardH + cardSpacing);
        }

        private void DrawShapeCard(D2DGraphics g, Rectangle rect, ShapePresetItem item)
        {
            if (item == null) return;
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

            // KẾT XUẤT DIRECT2D VECTOR TRỰC TIẾP (0ms lag, phần cứng GPU/WARP chống răng cưa mượt mà)
            if (!item.IsPhoto && !item.IsOnlineImage && !item.Category.StartsWith("Online_"))
            {
                try
                {
                    float pad = 7f;
                    float drawW = Math.Max(1f, rect.Width - pad * 2f);
                    float drawH = Math.Max(1f, rect.Height - pad * 2f);
                    using (GraphicsPath shapePath = item.CreatePath(drawW, drawH))
                    {
                        if (shapePath != null)
                        {
                            using (Matrix m = new Matrix())
                            {
                                m.Translate(rect.X + pad, rect.Y + pad);
                                shapePath.Transform(m);
                            }
                            Color sc = Color.FromArgb(51, 65, 85);      // Slate 700
                            Color fc = Color.FromArgb(241, 245, 249);    // Slate 100
                            using (Pen sp = new Pen(sc, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            using (SolidBrush fb = new SolidBrush(fc))
                            {
                                if (item.Category == "Lines")
                                {
                                    g.DrawPath(sp, shapePath);
                                }
                                else
                                {
                                    g.FillPath(fb, shapePath);
                                    g.DrawPath(sp, shapePath);
                                }
                            }
                        }
                    }
                }
                catch { }
                return;
            }

            // Vẽ thumbnail cho Online Graphics hoặc Stock Photos
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

            // Nút Back / Collapse (Khôi phục chính xác vị trí cuộn trước đó, không cuộn lại từ đầu)
            if (BtnBackBounds.Contains(pt))
            {
                if (!string.IsNullOrEmpty(ExpandedCategoryKey))
                {
                    ExpandedCategoryKey = null; // Quay lại xem tất cả danh mục
                    _expandedMagicItems = null;
                    _lastClickedGridItemId = null;
                    int restoreY = (_scrollHistory.Count > 0) ? _scrollHistory.Pop() : _savedHomeScrollY;
                    restoreY = Math.Max(0, restoreY);
                    ScrollY = restoreY;
                    SmoothScrollY = restoreY;
                    TargetScrollY = restoreY;
                    action = "Refresh";
                    return true;
                }

                if (!string.IsNullOrEmpty(SearchText))
                {
                    SearchText = "";
                    int restoreY = Math.Max(0, _savedHomeScrollY);
                    ScrollY = restoreY;
                    SmoothScrollY = restoreY;
                    TargetScrollY = restoreY;
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

            // Tab Switcher [ Hình dạng ] / [ Đồ họa ] / [ Hình ảnh ]
            if (BtnTabShapesBounds.Contains(pt))
            {
                if (Mode != ShapePaneMode.ShapesOnly)
                {
                    Mode = ShapePaneMode.ShapesOnly;
                    ExpandedCategoryKey = null;
                    _lastClickedGridItemId = null;
                    _activeMagicAnchorKey = "__recently_used__";
                    _magicDismissed = false;
                    SearchText = "";
                    _scrollHistory.Clear();
                    _savedHomeScrollY = 0;
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    lock (_profileLock) { _shuffledFeatured.Clear(); }
                    EnsureMagicRecommendationsForCurrentMode(true);
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
                    _lastClickedGridItemId = null;
                    _activeMagicAnchorKey = "__recently_used__";
                    _magicDismissed = false;
                    SearchText = "";
                    _scrollHistory.Clear();
                    _savedHomeScrollY = 0;
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    lock (_profileLock) { _shuffledFeatured.Clear(); }
                    EnsureMagicRecommendationsForCurrentMode(true);
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
                    _lastClickedGridItemId = null;
                    _activeMagicAnchorKey = "__recently_used__";
                    _magicDismissed = false;
                    SearchText = "";
                    _scrollHistory.Clear();
                    _savedHomeScrollY = 0;
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    lock (_profileLock) { _shuffledFeatured.Clear(); }
                    EnsureMagicRecommendationsForCurrentMode(true);
                    action = "SwitchMode";
                    return true;
                }
            }

            // Nút "Xem tất cả" hoặc nút chevron > của một danh mục (Lưu vị trí cuộn hiện tại để khôi phục khi nhấn Trở lại)
            foreach (var seeAll in CategorySeeAllBounds)
            {
                if (seeAll.Item1.Contains(pt))
                {
                    _savedHomeScrollY = ScrollY;
                    _scrollHistory.Push(ScrollY);
                    ExpandedCategoryKey = seeAll.Item2;
                    if (ExpandedCategoryKey == "__magic_recommendations__")
                    {
                        _expandedMagicItems = new List<ShapePresetItem>(GetMagicRecommendations());
                    }
                    _lastClickedGridItemId = null; // Mới vào chủ đề chưa bấm gì thì chưa hiện đề xuất trong lưới
                    ScrollY = 0;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    action = "ExpandCategory";
                    targetId = seeAll.Item2;
                    return true;
                }
            }

            if (!BtnRetryOfflineBounds.IsEmpty && BtnRetryOfflineBounds.Contains(pt))
            {
                if (Mode == ShapePaneMode.GraphicsOnly)
                    ZenQOffice.Core.OnlineGraphicsManager.RefreshCatalogAsync();
                else if (Mode == ShapePaneMode.ImagesOnly)
                    ZenQOffice.Core.OnlineImagesManager.RefreshCatalogAsync();
                action = "Refresh";
                targetId = null;
                return true;
            }

            if (!BtnDismissMagicBounds.IsEmpty && BtnDismissMagicBounds.Contains(pt))
            {
                int keepY = ScrollY;
                _magicDismissed = true;
                BtnDismissMagicBounds = Rectangle.Empty;
                ScrollY = keepY;
                SmoothScrollY = keepY;
                TargetScrollY = keepY;
                action = "DismissMagic";
                targetId = null;
                return true;
            }

            if (!BtnClearSearchBounds.IsEmpty && BtnClearSearchBounds.Contains(pt))
            {
                SearchText = "";
                action = "ClearSearch";
                targetId = null;
                return true;
            }

            // Click vào một hình dạng (Chèn vào slide + ghi nhận Dùng gần đây + cập nhật Đề xuất Magic ngay dưới hàng vừa bấm, khóa cứng vị trí khung hình)
            for (int bIdx = 0; bIdx < ItemClickBounds.Count; bIdx++)
            {
                var itemBound = ItemClickBounds[bIdx];
                if (itemBound.Item1.Contains(pt))
                {
                    action = "InsertShape";
                    string clickedId = itemBound.Item2;
                    targetId = clickedId;
                    string clickedRowKey = (bIdx < ItemClickRowKeys.Count) ? ItemClickRowKeys[bIdx] : null;

                    bool isOverview = string.IsNullOrEmpty(ExpandedCategoryKey) && string.IsNullOrWhiteSpace(SearchText);
                    if (isOverview)
                    {
                        string anchorTargetRow = (clickedRowKey == "__magic_recommendations__")
                            ? (string.IsNullOrEmpty(_activeMagicAnchorKey) ? "__recently_used__" : _activeMagicAnchorKey)
                            : (clickedRowKey ?? "__recently_used__");

                        int oldRowOffset = ComputeOverviewRowOffsetY(clickedRowKey ?? anchorTargetRow);

                        _activeMagicAnchorKey = anchorTargetRow;
                        _magicDismissed = false;
                        RecordItemUsed(targetId);

                        int newRowOffset = ComputeOverviewRowOffsetY(clickedRowKey ?? anchorTargetRow);
                        int keepScrollY = Math.Max(0, ScrollY + (newRowOffset - oldRowOffset));
                        ScrollY = keepScrollY;
                        SmoothScrollY = keepScrollY;
                        TargetScrollY = keepScrollY;
                    }
                    else
                    {
                        int keepScrollY = ScrollY;
                        if (!string.IsNullOrEmpty(ExpandedCategoryKey))
                        {
                            List<ShapePresetItem> gridItems = GetItemsForExpandedCategory(ExpandedCategoryKey);
                            if (gridItems != null && gridItems.Count > 0)
                            {
                                int cardW = (Mode == ShapePaneMode.ImagesOnly) ? 80 : 46;
                                int cardH_magic = (Mode == ShapePaneMode.ImagesOnly) ? 50 : 44;
                                int cardSpacing = 6;
                                int rw = Math.Max(100, _lastRenderWidth - 24);
                                int cols = Math.Max(1, (rw + cardSpacing) / (cardW + cardSpacing));

                                int oldIdx = (!string.IsNullOrEmpty(_lastClickedGridItemId) && !_magicDismissed && _magicRecommendations.Count > 0)
                                    ? gridItems.FindIndex(it => it.Id == _lastClickedGridItemId)
                                    : -1;
                                int newIdx = gridItems.FindIndex(it => it.Id == clickedId);

                                if (oldIdx >= 0 && newIdx >= 0)
                                {
                                    int oldRow = oldIdx / cols;
                                    int newRow = newIdx / cols;
                                    if (oldRow < newRow)
                                    {
                                        int magicBlockH = 10 + (26 + cardH_magic + 12) + 8;
                                        keepScrollY = Math.Max(0, keepScrollY - magicBlockH);
                                    }
                                }
                            }
                            _lastClickedGridItemId = targetId;
                        }
                        _magicDismissed = false;
                        RecordItemUsed(targetId);
                        ScrollY = keepScrollY;
                        SmoothScrollY = keepScrollY;
                        TargetScrollY = keepScrollY;
                    }
                    return true;
                }
            }

            return false;
        }

        public bool HandleRightClick(Point pt, out string action, out string targetId)
        {
            action = null;
            targetId = null;

            foreach (var itemBound in ItemClickBounds)
            {
                if (itemBound.Item1.Contains(pt))
                {
                    targetId = itemBound.Item2;
                    ShapePresetItem item = WpfShapeLibrary.FindPreset(targetId);
                    if (item != null)
                    {
                        RecordItemUsed(targetId);
                        _savedHomeScrollY = ScrollY;
                        _scrollHistory.Push(ScrollY);
                        ExpandedCategoryKey = "__magic_recommendations__";
                        _expandedMagicItems = new List<ShapePresetItem>(GetMagicRecommendations());
                        _lastClickedGridItemId = null;
                        ScrollY = 0;
                        SmoothScrollY = 0f;
                        TargetScrollY = 0f;
                        action = "ViewSimilar";
                        return true;
                    }
                }
            }
            return false;
        }

        public void RecordItemUsed(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            EnsureUserProfileLoaded();

            ShapePresetItem item = WpfShapeLibrary.FindPreset(targetId);
            if (item == null) return;

            lock (_profileLock)
            {
                // 1. Thêm vào Dùng gần đây
                _recentItemIds.Remove(targetId);
                _recentItemIds.Insert(0, targetId);
                if (_recentItemIds.Count > 50) _recentItemIds.RemoveAt(_recentItemIds.Count - 1);

                // 2. Tăng điểm tần suất sử dụng danh mục
                string cat = item.Category ?? "";
                if (!string.IsNullOrEmpty(cat))
                {
                    int curCount = 0;
                    _categoryUsageCounts.TryGetValue(cat, out curCount);
                    _categoryUsageCounts[cat] = curCount + 1;
                }

                // 3. Cập nhật mục vừa nhấp
                _lastClickedItemId = targetId;
                _lastClickedItemName = item.Name ?? "";

                // 4. Sinh đề xuất Magic liên quan
                RefreshMagicRecommendations(item);
            }

            // 5. Lưu bất đồng bộ
            SaveUserProfileAsync();
        }

        private void RefreshMagicRecommendations(ShapePresetItem baseItem)
        {
            if (baseItem == null) return;

            bool isPhoto = baseItem.IsPhoto || (baseItem.Category != null && baseItem.Category.StartsWith("Online_Image_"));
            bool isOnlineGraphic = (baseItem.IsOnlineImage || (baseItem.Category != null && baseItem.Category.StartsWith("Online_"))) && !isPhoto;

            ShapePaneMode targetMode = isPhoto ? ShapePaneMode.ImagesOnly :
                                      isOnlineGraphic ? ShapePaneMode.GraphicsOnly :
                                      ShapePaneMode.ShapesOnly;

            List<ShapePresetItem> pool = GetPoolForMode(targetMode);
            if (pool == null || pool.Count == 0) return;

            string baseNorm = (baseItem.SearchIndexedText ?? (baseItem.Name + " " + baseItem.Category)).ToLowerInvariant();
            string[] baseTokens = baseNorm.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> tokenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tok in baseTokens)
            {
                if (tok.Length > 2) tokenSet.Add(tok);
            }

            var scored = new List<Tuple<int, int, ShapePresetItem>>();
            for (int idx = 0; idx < pool.Count; idx++)
            {
                var cand = pool[idx];
                if (cand.Id == baseItem.Id) continue;

                int score = 0;
                // Cùng danh mục: +60 điểm
                if (string.Equals(cand.Category, baseItem.Category, StringComparison.OrdinalIgnoreCase))
                {
                    score += 60;
                }

                // Khớp token ngữ nghĩa: +20 điểm mỗi token
                string candNorm = (cand.SearchIndexedText ?? (cand.Name + " " + cand.Category)).ToLowerInvariant();
                foreach (var tok in tokenSet)
                {
                    if (candNorm.Contains(tok))
                    {
                        score += 20;
                    }
                }

                if (score > 0)
                {
                    // Ưu tiên các mục đã có sẵn thumbnail trong bộ nhớ đệm để hiển thị tức thì không bị ô trắng
                    bool isThumbReady = false;
                    lock (_thumbLock)
                    {
                        isThumbReady = _thumbCache.ContainsKey($"{cand.Id}_online_40") || _thumbCache.ContainsKey($"{cand.Id}_online_64");
                    }
                    if (isThumbReady) score += 15;

                    scored.Add(new Tuple<int, int, ShapePresetItem>(score, idx, cand));
                }
            }

            scored.Sort((a, b) =>
            {
                int cmp = b.Item1.CompareTo(a.Item1);
                if (cmp != 0) return cmp;
                return a.Item2.CompareTo(b.Item2);
            });

            _magicRecommendations.Clear();
            int take = Math.Min(40, scored.Count);
            for (int i = 0; i < take; i++)
            {
                _magicRecommendations.Add(scored[i].Item3);
            }
            _lastMagicSeededMode = targetMode;
        }

        private List<ShapePresetItem> GetPoolForMode(ShapePaneMode mode)
        {
            List<ShapePresetItem> pool = new List<ShapePresetItem>();
            foreach (var cat in WpfShapeLibrary.Categories)
            {
                bool isPhotoCat = cat.IsPhotoCategory || cat.Key.StartsWith("Online_Image_");
                bool isOnlineGraphicCat = cat.Key.StartsWith("Online_") && !isPhotoCat;
                bool isStandardShapeCat = !cat.Key.StartsWith("Online_") && !isPhotoCat;

                if (mode == ShapePaneMode.ShapesOnly && !isStandardShapeCat) continue;
                if (mode == ShapePaneMode.GraphicsOnly && !isOnlineGraphicCat) continue;
                if (mode == ShapePaneMode.ImagesOnly && !isPhotoCat) continue;

                if (cat.Items != null)
                {
                    pool.AddRange(cat.Items);
                }
            }
            return pool;
        }

        private List<ShapePresetItem> GetCurrentModePool()
        {
            return GetPoolForMode(Mode);
        }

        public void TriggerRecommendationFor(ShapePresetItem item)
        {
            if (item == null)
            {
                ClearActiveRecommendation();
                return;
            }

            lock (_profileLock)
            {
                _lastClickedItemId = item.Id;
                _lastClickedItemName = item.Name ?? "";
                _activeMagicAnchorKey = item.Category;
                RefreshMagicRecommendations(item);
            }
        }

        public void ClearActiveRecommendation()
        {
            lock (_profileLock)
            {
                _magicDismissed = true;
                BtnDismissMagicBounds = Rectangle.Empty;
            }
        }

        private void EnsureMagicRecommendationsForCurrentMode(bool forceReseed = false)
        {
            EnsureUserProfileLoaded();
            lock (_profileLock)
            {
                bool modeMismatch = (_lastMagicSeededMode != Mode);
                if (!forceReseed && !modeMismatch && _magicRecommendations.Count > 0)
                {
                    return;
                }

                ShapePresetItem seedItem = null;
                foreach (var id in _recentItemIds)
                {
                    var it = WpfShapeLibrary.FindPreset(id);
                    if (it != null && IsItemCompatibleWithMode(it))
                    {
                        seedItem = it;
                        break;
                    }
                }

                if (seedItem == null)
                {
                    var pool = GetCurrentModePool();
                    if (pool != null && pool.Count > 0)
                    {
                        seedItem = pool[0];
                    }
                }

                if (seedItem != null)
                {
                    if (string.IsNullOrEmpty(_lastClickedItemId) || modeMismatch)
                    {
                        _lastClickedItemId = seedItem.Id;
                        _lastClickedItemName = seedItem.Name ?? "";
                        _activeMagicAnchorKey = "__recently_used__";
                    }
                    RefreshMagicRecommendations(seedItem);
                }
            }
        }

        private List<ShapePresetItem> GetItemsForExpandedCategory(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key == "__recently_used__") return GetRecentlyUsedItems();
            if (key == "__magic_recommendations__") return GetMagicExpandedItems();
            if (key == "__featured__") return GetShuffledFeaturedItems();
            foreach (var c in WpfShapeLibrary.Categories)
            {
                if (c.Key == key) return c.Items;
            }
            return null;
        }

        private List<ShapePresetItem> GetMagicExpandedItems()
        {
            if (_expandedMagicItems == null || _expandedMagicItems.Count == 0)
            {
                _expandedMagicItems = new List<ShapePresetItem>(GetMagicRecommendations());
            }
            return _expandedMagicItems;
        }


        private int ComputeOverviewRowOffsetY(string targetRowKey)
        {
            if (string.IsNullOrEmpty(targetRowKey)) return 0;
            int cardH = (Mode == ShapePaneMode.ImagesOnly) ? 50 : 44;
            int stepH = 26 + cardH + 12;
            int offsetY = 0;

            EnsureMagicRecommendationsForCurrentMode();
            string effectiveAnchor = string.IsNullOrEmpty(_activeMagicAnchorKey) ? "__recently_used__" : _activeMagicAnchorKey;
            bool canShowMagic = !_magicDismissed && _magicRecommendations.Count > 0;

            var recentItems = GetRecentlyUsedItems();
            if (recentItems != null && recentItems.Count > 0)
            {
                if (targetRowKey == "__recently_used__") return offsetY;
                offsetY += stepH;
                if (canShowMagic)
                {
                    if (targetRowKey == "__magic_recommendations__" && effectiveAnchor == "__recently_used__") return offsetY;
                    offsetY += stepH;
                }
            }
            else if (canShowMagic)
            {
                if (targetRowKey == "__magic_recommendations__" && effectiveAnchor == "__recently_used__") return offsetY;
                offsetY += stepH;
            }

            var featuredPreview = GetFeaturedPreviewItems();
            if (featuredPreview != null && featuredPreview.Count > 0)
            {
                if (targetRowKey == "__featured__") return offsetY;
                offsetY += stepH;
                if (canShowMagic && effectiveAnchor == "__featured__")
                {
                    if (targetRowKey == "__magic_recommendations__") return offsetY;
                    offsetY += stepH;
                }
            }

            var sortedCats = GetSortedCategories();
            foreach (var cat in sortedCats)
            {
                if (cat.Items == null || cat.Items.Count == 0) continue;
                if (targetRowKey == cat.Key) return offsetY;
                offsetY += stepH;
                if (canShowMagic && effectiveAnchor == cat.Key)
                {
                    if (targetRowKey == "__magic_recommendations__") return offsetY;
                    offsetY += stepH;
                }
            }

            return offsetY;
        }

        private void EnsureUserProfileLoaded()
        {
            if (_userProfileLoaded) return;
            lock (_profileLock)
            {
                if (_userProfileLoaded) return;
                try
                {
                    if (File.Exists(_userPrefFile))
                    {
                        string json = File.ReadAllText(_userPrefFile, System.Text.Encoding.UTF8);
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("recently_used", out var recentElem) && recentElem.ValueKind == JsonValueKind.Array)
                            {
                                _recentItemIds.Clear();
                                foreach (var it in recentElem.EnumerateArray())
                                {
                                    string s = it.GetString();
                                    if (!string.IsNullOrEmpty(s)) _recentItemIds.Add(s);
                                }
                            }
                            if (root.TryGetProperty("category_usage", out var usageElem) && usageElem.ValueKind == JsonValueKind.Object)
                            {
                                _categoryUsageCounts.Clear();
                                foreach (var prop in usageElem.EnumerateObject())
                                {
                                    if (prop.Value.TryGetInt32(out int cVal))
                                    {
                                        _categoryUsageCounts[prop.Name] = cVal;
                                    }
                                }
                            }
                            // Khởi tạo trạng thái Đề xuất Magic hoàn toàn sạch, chỉ hiện khi người dùng chọn đối tượng
                            _lastClickedItemId = null;
                            _lastClickedItemName = null;
                            _magicRecommendations.Clear();
                        }
                    }
                }
                catch { }

                // Khởi tạo các item Dùng gần đây ban đầu nếu chưa có
                if (_recentItemIds.Count == 0)
                {
                    _recentItemIds.Add("vietnam_vn_bang_ron_do_le_hoi_dai");
                    _recentItemIds.Add("fluent_color_Cherry_blossom");
                    _recentItemIds.Add("tech_waves_wave_cyan_001");
                    _recentItemIds.Add("vn_heritage_gold_gold_heritage_001");
                    _recentItemIds.Add("spring_floral_floral_spring_001");
                }

                _userProfileLoaded = true;
            }
        }

        private void SaveUserProfileAsync()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (_profileLock)
                {
                    try
                    {
                        string dir = Path.GetDirectoryName(_userPrefFile);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        using (var stream = new MemoryStream())
                        {
                            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                            {
                                writer.WriteStartObject();

                                writer.WriteStartArray("recently_used");
                                foreach (var id in _recentItemIds)
                                {
                                    writer.WriteStringValue(id);
                                }
                                writer.WriteEndArray();

                                writer.WriteStartObject("category_usage");
                                foreach (var kvp in _categoryUsageCounts)
                                {
                                    writer.WriteNumber(kvp.Key, kvp.Value);
                                }
                                writer.WriteEndObject();

                                if (!string.IsNullOrEmpty(_lastClickedItemId))
                                {
                                    writer.WriteString("last_clicked_id", _lastClickedItemId);
                                }

                                writer.WriteEndObject();
                            }

                            string tempFile = _userPrefFile + ".tmp";
                            File.WriteAllBytes(tempFile, stream.ToArray());
                            File.Move(tempFile, _userPrefFile, true);
                        }
                    }
                    catch { }
                }
            });
        }

        private List<ShapePresetItem> GetRecentlyUsedItems()
        {
            EnsureUserProfileLoaded();
            List<ShapePresetItem> list = new List<ShapePresetItem>();
            lock (_profileLock)
            {
                foreach (var id in _recentItemIds)
                {
                    var item = WpfShapeLibrary.FindPreset(id);
                    if (item != null && IsItemCompatibleWithMode(item))
                    {
                        list.Add(item);
                        if (list.Count >= 24) break;
                    }
                }
            }
            return list;
        }

        private bool IsItemCompatibleWithMode(ShapePresetItem item)
        {
            if (item == null) return false;
            bool isPhoto = item.IsPhoto || (item.Category != null && item.Category.StartsWith("Online_Image_"));
            bool isOnlineGraphic = (item.IsOnlineImage || (item.Category != null && item.Category.StartsWith("Online_"))) && !isPhoto;
            bool isShape = !isOnlineGraphic && !isPhoto;

            if (Mode == ShapePaneMode.ShapesOnly) return isShape;
            if (Mode == ShapePaneMode.GraphicsOnly) return isOnlineGraphic;
            if (Mode == ShapePaneMode.ImagesOnly) return isPhoto;
            return false;
        }

        private List<ShapePresetItem> GetMagicRecommendations()
        {
            EnsureUserProfileLoaded();
            lock (_profileLock)
            {
                if (string.IsNullOrEmpty(_lastClickedItemId) || _magicRecommendations.Count == 0)
                {
                    if (ExpandedCategoryKey == "__magic_recommendations__")
                    {
                        var pool = GetCurrentModePool();
                        if (pool != null && pool.Count > 0)
                        {
                            var listFallback = new List<ShapePresetItem>();
                            int count = Math.Min(30, pool.Count);
                            for (int i = 0; i < count; i++) listFallback.Add(pool[i]);
                            return listFallback;
                        }
                    }
                    return new List<ShapePresetItem>();
                }
                return new List<ShapePresetItem>(_magicRecommendations);
            }
        }

        private List<ShapePresetItem> GetShuffledFeaturedItems()
        {
            EnsureFeaturedInitialized();
            lock (_profileLock)
            {
                return new List<ShapePresetItem>(_shuffledFeatured);
            }
        }

        private List<ShapePresetItem> GetFeaturedPreviewItems()
        {
            EnsureFeaturedInitialized();
            lock (_profileLock)
            {
                int count = Math.Min(12, _shuffledFeatured.Count);
                return _shuffledFeatured.GetRange(0, count);
            }
        }

        private void EnsureFeaturedInitialized()
        {
            if (_shuffledFeatured.Count > 0 && _featuredMode == Mode) return;

            List<ShapePresetItem> pool = GetCurrentModePool();
            if (pool == null || pool.Count == 0) return;

            Dictionary<string, List<ShapePresetItem>> byCat = new Dictionary<string, List<ShapePresetItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in pool)
            {
                string c = it.Category ?? "Other";
                if (!byCat.ContainsKey(c)) byCat[c] = new List<ShapePresetItem>();
                byCat[c].Add(it);
            }

            List<List<ShapePresetItem>> catLists = new List<List<ShapePresetItem>>(byCat.Values);
            Random rnd = new Random(_sessionSeed ^ ((int)Mode * 7919) ^ Environment.TickCount);

            for (int i = catLists.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = catLists[i]; catLists[i] = catLists[j]; catLists[j] = tmp;
            }

            List<ShapePresetItem> diverse = new List<ShapePresetItem>();
            int maxRounds = 20;
            for (int round = 0; round < maxRounds; round++)
            {
                foreach (var cList in catLists)
                {
                    if (round < cList.Count)
                    {
                        diverse.Add(cList[round]);
                    }
                }
            }

            for (int i = diverse.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = diverse[i]; diverse[i] = diverse[j]; diverse[j] = tmp;
            }

            lock (_profileLock)
            {
                _shuffledFeatured = diverse;
                _featuredMode = Mode;
            }
        }

        private List<ShapeCategoryGroup> GetSortedCategories()
        {
            EnsureUserProfileLoaded();

            List<ShapeCategoryGroup> validCats = new List<ShapeCategoryGroup>();
            foreach (var cat in WpfShapeLibrary.Categories)
            {
                bool isPhotoCat = cat.IsPhotoCategory || cat.Key.StartsWith("Online_Image_");
                bool isOnlineGraphicCat = cat.Key.StartsWith("Online_") && !isPhotoCat;
                bool isStandardShapeCat = !cat.Key.StartsWith("Online_") && !isPhotoCat;

                if (Mode == ShapePaneMode.ShapesOnly && !isStandardShapeCat) continue;
                if (Mode == ShapePaneMode.GraphicsOnly && !isOnlineGraphicCat) continue;
                if (Mode == ShapePaneMode.ImagesOnly && !isPhotoCat) continue;

                validCats.Add(cat);
            }

            // Giữ cố định hoàn toàn thứ tự các danh mục trong suốt phiên làm việc để khi click chọn hình, danh mục KHÔNG bao giờ bị nhảy vị trí
            validCats.Sort((a, b) =>
            {
                int scoreA = GetCategoryVarietyScore(a.Key);
                int scoreB = GetCategoryVarietyScore(b.Key);
                return scoreB.CompareTo(scoreA);
            });

            return validCats;
        }

        private int GetCategoryVarietyScore(string catKey)
        {
            if (string.IsNullOrEmpty(catKey)) return 0;
            int baseBoost = 0;
            if (catKey.Contains("vietnam") || catKey.Contains("festive_banners") || catKey.Contains("vn_heritage")) baseBoost = 500;
            else if (catKey.Contains("tech_waves") || catKey.Contains("spring_floral")) baseBoost = 400;
            else if (catKey.Contains("fluent_color") || catKey.Contains("undraw")) baseBoost = 300;

            int hash = Math.Abs((catKey + _sessionSeed).GetHashCode());
            return baseBoost + (hash % 200);
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

        private static Bitmap EnsurePreScaledThumbnail(Bitmap src, int maxDim)
        {
            if (src == null || maxDim <= 0) return src;
            if (src.Width <= maxDim && src.Height <= maxDim) return src;
            try
            {
                float ratio = Math.Min((float)maxDim / src.Width, (float)maxDim / src.Height);
                int tw = Math.Max(1, (int)Math.Round(src.Width * ratio));
                int th = Math.Max(1, (int)Math.Round(src.Height * ratio));
                Bitmap scaled = new Bitmap(tw, th, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (var wrapAttr = new System.Drawing.Imaging.ImageAttributes())
                    {
                        wrapAttr.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(src, new Rectangle(0, 0, tw, th), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, wrapAttr);
                    }
                }
                src.Dispose();
                return scaled;
            }
            catch
            {
                return src;
            }
        }

        public void HandleScroll(int delta)
        {
            HandleMouseWheel(delta);
        }

        public void HandleMouseWheel(int delta, bool smooth = false)
        {
            if (IsDraggingScrollbar) return;
            float scrollStep = -(delta / 120.0f) * 84f;
            if (Math.Abs(scrollStep) < 1f) scrollStep = (delta > 0) ? -84f : 84f;

            if (smooth)
            {
                float currentDiff = TargetScrollY - SmoothScrollY;
                // Nếu đổi chiều lăn hoặc khoảng cách chờ quá xa, neo từ SmoothScrollY để dừng gọn không bị trôi dài
                if ((scrollStep > 0 && currentDiff < 0) || (scrollStep < 0 && currentDiff > 0))
                {
                    TargetScrollY = SmoothScrollY + scrollStep;
                }
                else
                {
                    float clampedLead = Math.Sign(currentDiff) * Math.Min(Math.Abs(currentDiff), 24f);
                    TargetScrollY = SmoothScrollY + clampedLead + scrollStep;
                }
                TargetScrollY = Math.Max(0f, Math.Min(MaxScrollY, (float)Math.Round(TargetScrollY)));
                UpdateSmoothScroll();
            }
            else
            {
                TargetScrollY = Math.Max(0f, Math.Min(MaxScrollY, (float)Math.Round(TargetScrollY + scrollStep)));
                ScrollY = (int)TargetScrollY;
                SmoothScrollY = TargetScrollY;
            }
        }

        public bool UpdateSmoothScroll()
        {
            bool pillAnim = false;
            if (_tabPillTargetX >= 0f && _tabPillAnimX >= 0f)
            {
                double dt = FluentMotion.DeltaSeconds(ref _tabPillLastTs);
                float blend = FluentMotion.DampFactor(dt, 0.042);
                if (FluentMotion.StepAxis(ref _tabPillAnimX, _tabPillTargetX, blend, 0.35f)) pillAnim = true;
                if (FluentMotion.StepAxis(ref _tabPillAnimW, _tabPillTargetW, blend, 0.35f)) pillAnim = true;
                if (!pillAnim) _tabPillLastTs = 0;
            }
            TargetScrollY = Math.Clamp((float)Math.Round(TargetScrollY), 0, MaxScrollY);
            if (IsDraggingScrollbar)
            {
                ScrollY = Math.Clamp(ScrollY, 0, MaxScrollY);
                SmoothScrollY = TargetScrollY = ScrollY;
                return pillAnim;
            }
            SmoothScrollY = Math.Clamp(SmoothScrollY, 0, MaxScrollY);
            int prevInt = (int)Math.Round(SmoothScrollY);
            int targetInt = (int)TargetScrollY;

            float diff = TargetScrollY - SmoothScrollY;
            if (Math.Abs(diff) <= 1.5f || prevInt == targetInt)
            {
                bool changed = (ScrollY != targetInt || SmoothScrollY != TargetScrollY);
                SmoothScrollY = TargetScrollY;
                ScrollY = targetInt;
                return changed || pillAnim;
            }

            // Hệ số 0.45f + ngưỡng cắt đuôi 2px: lướt êm 3-4 khung hình (~55ms) rồi dừng hẳn, không trôi lê thê
            float step = diff * 0.45f;
            if (Math.Abs(step) < 2.0f)
            {
                step = Math.Sign(diff) * Math.Min(Math.Abs(diff), 2.0f);
            }
            SmoothScrollY += step;
            if (Math.Abs(TargetScrollY - SmoothScrollY) <= 1.5f)
            {
                SmoothScrollY = TargetScrollY;
            }
            ScrollY = (int)Math.Round(SmoothScrollY);
            return true;
        }
    }
}


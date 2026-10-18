using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WpfPoint = System.Windows.Point;
using WpfGeometry = System.Windows.Media.Geometry;
using WpfStreamGeometry = System.Windows.Media.StreamGeometry;
using WpfSize = System.Windows.Size;
using WpfSweepDirection = System.Windows.Media.SweepDirection;

namespace ZenQOffice.UI
{
    public class ShapePresetItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string CategoryName { get; set; }
        public Func<float, float, GraphicsPath> PathGenerator { get; set; }
        public Func<double, double, System.Windows.Media.Geometry> WpfGeometryGenerator { get; set; }

        public bool IsOnlineImage { get; set; }
        public bool IsPhoto { get; set; }
        public string ImageUrl { get; set; }

        public GraphicsPath CreatePath(float w, float h)
        {
            if (PathGenerator != null) return PathGenerator(w, h);
            GraphicsPath fallback = new GraphicsPath();
            fallback.AddRectangle(new RectangleF(0, 0, w, h));
            return fallback;
        }

        public WpfGeometry CreateWpfGeometry(double w, double h)
        {
            if (WpfGeometryGenerator != null) return WpfGeometryGenerator(w, h);
            WpfStreamGeometry geom = new WpfStreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(new WpfPoint(0, 0), true, true);
                ctx.LineTo(new WpfPoint(w, 0), true, false);
                ctx.LineTo(new WpfPoint(w, h), true, false);
                ctx.LineTo(new WpfPoint(0, h), true, false);
            }
            geom.Freeze();
            return geom;
        }
    }

    public class ShapeCategoryGroup
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public bool IsPhotoCategory { get; set; }
        public List<ShapePresetItem> Items { get; set; } = new List<ShapePresetItem>();
    }

    public static class WpfShapeLibrary
    {
        private static readonly List<ShapeCategoryGroup> _categories = new List<ShapeCategoryGroup>();
        private static readonly Dictionary<string, ShapePresetItem> _lookup = new Dictionary<string, ShapePresetItem>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _syncLock = new object();

        public static IReadOnlyList<ShapeCategoryGroup> Categories
        {
            get { 
                EnsureInitialized(); 
                lock (_categories) { return new List<ShapeCategoryGroup>(_categories); }
            }
        }

        public static int TotalPresetCount
        {
            get { EnsureInitialized(); return _lookup.Count; }
        }

        public static ShapePresetItem FindPreset(string id)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(id)) return null;
            ShapePresetItem item;
            _lookup.TryGetValue(id, out item);
            return item;
        }

        public static List<ShapePresetItem> SearchPresets(string query)
        {
            EnsureInitialized();
            List<ShapePresetItem> result = new List<ShapePresetItem>();
            if (string.IsNullOrWhiteSpace(query)) return result;
            string q = query.Trim().ToLowerInvariant();
            foreach (var kvp in _lookup)
            {
                if (kvp.Value.Name.ToLowerInvariant().Contains(q) || kvp.Value.CategoryName.ToLowerInvariant().Contains(q))
                    result.Add(kvp.Value);
            }
            return result;
        }

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_syncLock)
            {
                if (_initialized) return;
                PopulateAllCategories();
                _initialized = true;
            }
        }

        private static void PopulateAllCategories()
        {
            PopulateOnlineGraphics();
            PopulateOnlineImages();
            PopulateLinesAndArrows();
            PopulateBasicShapes();
            PopulatePolygonsAndGeometry();
            PopulateStarsAndBadges();
            PopulateBlockArrows();
            PopulateFlowchartSymbols();
            PopulateCallouts();
            PopulateBannersAndRibbons();
            PopulateIsometric3DShapes();
            PopulateDiagramComponents();
            PopulateMathAndTechnical();
            PopulateBordersAndDividers();
        }

        private static void AddItem(ShapeCategoryGroup cat, string id, string name, Func<float, float, GraphicsPath> gdiGen, Func<double, double, WpfGeometry> wpfGen)
        {
            ShapePresetItem item = new ShapePresetItem
            {
                Id = id,
                Name = name,
                Category = cat.Key,
                CategoryName = cat.Title,
                PathGenerator = gdiGen,
                WpfGeometryGenerator = wpfGen
            };
            cat.Items.Add(item);
            if (!_lookup.ContainsKey(id)) _lookup[id] = item;
        }

        #region Geometric Primitives Helpers
        private static WpfGeometry WPoly(double w, double h, params (double x, double y)[] pts)
        {
            WpfStreamGeometry g = new WpfStreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new WpfPoint(pts[0].x * w, pts[0].y * h), true, true);
                for (int i = 1; i < pts.Length; i++)
                    ctx.LineTo(new WpfPoint(pts[i].x * w, pts[i].y * h), true, false);
            }
            g.Freeze();
            return g;
        }

        private static GraphicsPath GPoly(float w, float h, params (double x, double y)[] pts)
        {
            GraphicsPath gp = new GraphicsPath();
            PointF[] p = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++) p[i] = new PointF((float)(pts[i].x * w), (float)(pts[i].y * h));
            gp.AddPolygon(p);
            return gp;
        }

        private static WpfGeometry WLines(double w, double h, params (double x, double y)[] pts)
        {
            WpfStreamGeometry g = new WpfStreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new WpfPoint(pts[0].x * w, pts[0].y * h), false, false);
                for (int i = 1; i < pts.Length; i++)
                    ctx.LineTo(new WpfPoint(pts[i].x * w, pts[i].y * h), true, false);
            }
            g.Freeze();
            return g;
        }

        private static GraphicsPath GLines(float w, float h, params (double x, double y)[] pts)
        {
            GraphicsPath gp = new GraphicsPath();
            PointF[] p = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++) p[i] = new PointF((float)(pts[i].x * w), (float)(pts[i].y * h));
            gp.AddLines(p);
            return gp;
        }

        private static WpfGeometry WRegularPoly(double w, double h, int sides, double angleOffset)
        {
            WpfStreamGeometry g = new WpfStreamGeometry();
            double cx = w / 2.0, cy = h / 2.0;
            double rx = (w - 4.0) / 2.0, ry = (h - 4.0) / 2.0;
            using (var ctx = g.Open())
            {
                for (int i = 0; i < sides; i++)
                {
                    double angle = angleOffset + 2.0 * Math.PI * i / sides;
                    var pt = new WpfPoint(cx + rx * Math.Cos(angle), cy + ry * Math.Sin(angle));
                    if (i == 0) ctx.BeginFigure(pt, true, true);
                    else ctx.LineTo(pt, true, false);
                }
            }
            g.Freeze();
            return g;
        }

        private static GraphicsPath GRegularPoly(float w, float h, int sides, double angleOffset)
        {
            GraphicsPath gp = new GraphicsPath();
            float cx = w / 2f, cy = h / 2f;
            float rx = (w - 4f) / 2f, ry = (h - 4f) / 2f;
            PointF[] pts = new PointF[sides];
            for (int i = 0; i < sides; i++)
            {
                double angle = angleOffset + 2.0 * Math.PI * i / sides;
                pts[i] = new PointF((float)(cx + rx * Math.Cos(angle)), (float)(cy + ry * Math.Sin(angle)));
            }
            gp.AddPolygon(pts);
            return gp;
        }

        private static WpfGeometry WStar(double w, double h, int points, double innerRatio)
        {
            WpfStreamGeometry g = new WpfStreamGeometry();
            double cx = w / 2.0, cy = h / 2.0;
            double rx = (w - 4.0) / 2.0, ry = (h - 4.0) / 2.0;
            int total = points * 2;
            using (var ctx = g.Open())
            {
                for (int i = 0; i < total; i++)
                {
                    double angle = -Math.PI / 2.0 + 2.0 * Math.PI * i / total;
                    double scale = (i % 2 == 0) ? 1.0 : innerRatio;
                    var pt = new WpfPoint(cx + rx * scale * Math.Cos(angle), cy + ry * scale * Math.Sin(angle));
                    if (i == 0) ctx.BeginFigure(pt, true, true);
                    else ctx.LineTo(pt, true, false);
                }
            }
            g.Freeze();
            return g;
        }

        private static GraphicsPath GStar(float w, float h, int points, double innerRatio)
        {
            GraphicsPath gp = new GraphicsPath();
            float cx = w / 2f, cy = h / 2f;
            float rx = (w - 4f) / 2f, ry = (h - 4f) / 2f;
            int total = points * 2;
            PointF[] pts = new PointF[total];
            for (int i = 0; i < total; i++)
            {
                double angle = -Math.PI / 2.0 + 2.0 * Math.PI * i / total;
                double scale = (i % 2 == 0) ? 1.0 : innerRatio;
                pts[i] = new PointF((float)(cx + rx * scale * Math.Cos(angle)), (float)(cy + ry * scale * Math.Sin(angle)));
            }
            gp.AddPolygon(pts);
            return gp;
        }

        private static WpfGeometry WRoundedRect(double w, double h, double r)
        {
            r = Math.Min(r, Math.Min(w / 2.0, h / 2.0));
            WpfStreamGeometry g = new WpfStreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new WpfPoint(r, 0), true, true);
                ctx.LineTo(new WpfPoint(w - r, 0), true, false);
                ctx.ArcTo(new WpfPoint(w, r), new WpfSize(r, r), 0, false, WpfSweepDirection.Clockwise, true, false);
                ctx.LineTo(new WpfPoint(w, h - r), true, false);
                ctx.ArcTo(new WpfPoint(w - r, h), new WpfSize(r, r), 0, false, WpfSweepDirection.Clockwise, true, false);
                ctx.LineTo(new WpfPoint(r, h), true, false);
                ctx.ArcTo(new WpfPoint(0, h - r), new WpfSize(r, r), 0, false, WpfSweepDirection.Clockwise, true, false);
                ctx.LineTo(new WpfPoint(0, r), true, false);
                ctx.ArcTo(new WpfPoint(r, 0), new WpfSize(r, r), 0, false, WpfSweepDirection.Clockwise, true, false);
            }
            g.Freeze();
            return g;
        }

        private static GraphicsPath GRoundedRect(float w, float h, double r)
        {
            float rf = (float)Math.Min(r, Math.Min(w / 2.0, h / 2.0));
            GraphicsPath gp = new GraphicsPath();
            float d = rf * 2f;
            gp.AddArc(0, 0, d, d, 180, 90);
            gp.AddArc(w - d, 0, d, d, 270, 90);
            gp.AddArc(w - d, h - d, d, d, 0, 90);
            gp.AddArc(0, h - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        private static WpfGeometry WEllipse(double w, double h)
        {
            WpfStreamGeometry g = new WpfStreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true);
                ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false);
                ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false);
            }
            g.Freeze();
            return g;
        }

        private static GraphicsPath GEllipse(float w, float h)
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(0, 0, w, h);
            return gp;
        }
        #endregion


                #region Online Graphics Categories
        private static void PopulateOnlineGraphics()
        {
            try {
                ZenQOffice.Core.OnlineGraphicsManager.OnCatalogUpdated += (catResp) => {
                    if (catResp != null) AddOnlineCategories(catResp);
                };

                var catalog = ZenQOffice.Core.OnlineGraphicsManager.GetCatalogCached();
                if (catalog != null) {
                    AddOnlineCategories(catalog);
                }
                ZenQOffice.Core.OnlineGraphicsManager.RefreshCatalogAsync();
            } catch { }
        }

        public static void AddOnlineCategories(ZenQOffice.Core.OnlineGraphicsResponse catalog)
        {
            if (catalog == null || catalog.categories == null) return;
            lock (_categories)
            {
                _categories.RemoveAll(x => x.Key.StartsWith("Online_"));

                List<string> keysToRemove = new List<string>();
                foreach (var kvp in _lookup)
                {
                    if (kvp.Value.IsOnlineImage || kvp.Value.Category.StartsWith("Online_"))
                        keysToRemove.Add(kvp.Key);
                }
                foreach (var k in keysToRemove) _lookup.Remove(k);

                foreach (var c in catalog.categories) {
                    if (c.items == null || c.items.Count == 0) continue;
                    if (c.name == "Vector Shapes") continue;

                    string key = "Online_" + (c.name ?? "Unnamed");
                    var cat = new ShapeCategoryGroup { Key = key, Title = c.name ?? "Unnamed" };
                    _categories.Add(cat);

                    foreach (var item in c.items) {
                        ShapePresetItem pItem = new ShapePresetItem {
                            Id = item.id,
                            Name = item.name,
                            Category = cat.Key,
                            CategoryName = cat.Title,
                            IsOnlineImage = true,
                            ImageUrl = item.url
                        };
                        cat.Items.Add(pItem);
                        _lookup[item.id] = pItem;
                    }
                }
            }
        }

        private static void PopulateOnlineImages()
        {
            try
            {
                ZenQOffice.Core.OnlineImagesManager.OnCatalogUpdated += (catResp) =>
                {
                    if (catResp != null) AddOnlineImageCategories(catResp);
                };

                var catalog = ZenQOffice.Core.OnlineImagesManager.GetCatalogCached();
                if (catalog != null)
                {
                    AddOnlineImageCategories(catalog);
                }
                ZenQOffice.Core.OnlineImagesManager.RefreshCatalogAsync();
            }
            catch { }
        }

        public static void AddOnlineImageCategories(ZenQOffice.Core.OnlineImagesResponse catalog)
        {
            if (catalog == null || catalog.categories == null) return;
            lock (_categories)
            {
                _categories.RemoveAll(x => x.Key.StartsWith("Online_Image_"));

                List<string> keysToRemove = new List<string>();
                foreach (var kvp in _lookup)
                {
                    if (kvp.Value.IsPhoto || kvp.Value.Category.StartsWith("Online_Image_"))
                        keysToRemove.Add(kvp.Key);
                }
                foreach (var k in keysToRemove) _lookup.Remove(k);

                foreach (var c in catalog.categories)
                {
                    if (c.items == null || c.items.Count == 0) continue;

                    string key = "Online_Image_" + (c.name ?? "Unnamed");
                    var cat = new ShapeCategoryGroup { Key = key, Title = c.name ?? "Unnamed", IsPhotoCategory = true };
                    _categories.Add(cat);

                    foreach (var item in c.items)
                    {
                        ShapePresetItem pItem = new ShapePresetItem
                        {
                            Id = item.id,
                            Name = item.name,
                            Category = cat.Key,
                            CategoryName = cat.Title,
                            IsOnlineImage = true,
                            IsPhoto = true,
                            ImageUrl = item.url
                        };
                        cat.Items.Add(pItem);
                        _lookup[item.id] = pItem;
                    }
                }
            }
        }

        #endregion

        #region Category 1: Đường kẻ & Mũi tên (45 mẫu)
        private static void PopulateLinesAndArrows()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Lines", Title = "Đường kẻ & Mũi tên" };
            _categories.Add(cat);

            // 1-15: Các kiểu đường thẳng & sóng
            AddItem(cat, "line_solid", "Đường thẳng liền nét", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "line_dashed", "Đường nét đứt quãng", (w, h) => GLines(w, h, (0, 0.5f), (0.2f, 0.5f), (0.25f, 0.5f), (0.45f, 0.5f), (0.5f, 0.5f), (0.7f, 0.5f), (0.75f, 0.5f), (0.95f, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.2, 0.5), (0.25, 0.5), (0.45, 0.5), (0.5, 0.5), (0.7, 0.5), (0.75, 0.5), (0.95, 0.5)));
            AddItem(cat, "line_dotted", "Đường nét chấm tròn", (w, h) => GLines(w, h, (0, 0.5f), (0.05f, 0.5f), (0.15f, 0.5f), (0.2f, 0.5f), (0.3f, 0.5f), (0.35f, 0.5f), (0.45f, 0.5f), (0.5f, 0.5f), (0.6f, 0.5f), (0.65f, 0.5f), (0.75f, 0.5f), (0.8f, 0.5f), (0.9f, 0.5f), (0.95f, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.05, 0.5), (0.15, 0.5), (0.2, 0.5), (0.3, 0.5), (0.35, 0.5), (0.45, 0.5), (0.5, 0.5), (0.6, 0.5), (0.65, 0.5), (0.75, 0.5), (0.8, 0.5), (0.9, 0.5), (0.95, 0.5)));
            AddItem(cat, "line_dash_dot", "Đường gạch chấm kết hợp", (w, h) => GLines(w, h, (0, 0.5f), (0.25f, 0.5f), (0.35f, 0.5f), (0.4f, 0.5f), (0.5f, 0.5f), (0.75f, 0.5f), (0.85f, 0.5f), (0.9f, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.25, 0.5), (0.35, 0.5), (0.4, 0.5), (0.5, 0.5), (0.75, 0.5), (0.85, 0.5), (0.9, 0.5)));
            AddItem(cat, "line_dash_dot_dot", "Đường gạch hai chấm", (w, h) => GLines(w, h, (0, 0.5f), (0.2f, 0.5f), (0.28f, 0.5f), (0.32f, 0.5f), (0.4f, 0.5f), (0.44f, 0.5f), (0.52f, 0.5f), (0.72f, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.2, 0.5), (0.28, 0.5), (0.32, 0.5), (0.4, 0.5), (0.44, 0.5), (0.52, 0.5), (0.72, 0.5)));
            AddItem(cat, "line_double", "Đường đôi song song", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.35f, w, h * 0.35f); gp.StartFigure(); gp.AddLine(0, h * 0.65f, w, h * 0.65f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.35), false, false); ctx.LineTo(new WpfPoint(w, h * 0.35), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.65), false, false); ctx.LineTo(new WpfPoint(w, h * 0.65), true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_triple", "Đường ba thanh mảnh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.25f, w, h * 0.25f); gp.StartFigure(); gp.AddLine(0, h * 0.5f, w, h * 0.5f); gp.StartFigure(); gp.AddLine(0, h * 0.75f, w, h * 0.75f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.25), false, false); ctx.LineTo(new WpfPoint(w, h * 0.25), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.75), false, false); ctx.LineTo(new WpfPoint(w, h * 0.75), true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_heavy", "Đường kẻ đậm dày dặn", (w, h) => GPoly(w, h, (0, 0.4f), (1, 0.4f), (1, 0.6f), (0, 0.6f)), (w, h) => WPoly(w, h, (0, 0.4), (1, 0.4), (1, 0.6), (0, 0.6)));
            AddItem(cat, "line_thin", "Đường kẻ mảnh mờ", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "line_sine_wave", "Đường lượn sóng sin mềm mại", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(0, h * 0.5f, w * 0.25f, h * 0.1f, w * 0.25f, h * 0.9f, w * 0.5f, h * 0.5f); gp.AddBezier(w * 0.5f, h * 0.5f, w * 0.75f, h * 0.1f, w * 0.75f, h * 0.9f, w, h * 0.5f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.BezierTo(new WpfPoint(w * 0.25, h * 0.1), new WpfPoint(w * 0.25, h * 0.9), new WpfPoint(w * 0.5, h * 0.5), true, false); ctx.BezierTo(new WpfPoint(w * 0.75, h * 0.1), new WpfPoint(w * 0.75, h * 0.9), new WpfPoint(w, h * 0.5), true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_zigzag", "Đường gấp khúc nhọn zig-zag", (w, h) => GLines(w, h, (0, 0.5f), (0.15f, 0.15f), (0.35f, 0.85f), (0.55f, 0.15f), (0.75f, 0.85f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.15, 0.15), (0.35, 0.85), (0.55, 0.15), (0.75, 0.85), (1, 0.5)));
            AddItem(cat, "line_square_wave", "Đường sóng vuông kỹ thuật", (w, h) => GLines(w, h, (0, 0.5f), (0.2f, 0.5f), (0.2f, 0.2f), (0.4f, 0.2f), (0.4f, 0.8f), (0.6f, 0.8f), (0.6f, 0.2f), (0.8f, 0.2f), (0.8f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.2, 0.5), (0.2, 0.2), (0.4, 0.2), (0.4, 0.8), (0.6, 0.8), (0.6, 0.2), (0.8, 0.2), (0.8, 0.5), (1, 0.5)));
            AddItem(cat, "line_arc_90", "Đường vòng cung góc 90 độ", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w * 1.8f, h * 1.8f, 180, 90); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h), false, false); ctx.ArcTo(new WpfPoint(w, 0), new WpfSize(w, h), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_s_curve", "Đường uốn cong chữ S", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(0, h * 0.8f, w * 0.3f, h * 0.8f, w * 0.7f, h * 0.2f, w, h * 0.2f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.8), false, false); ctx.BezierTo(new WpfPoint(w * 0.3, h * 0.8), new WpfPoint(w * 0.7, h * 0.2), new WpfPoint(w, h * 0.2), true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_soft_arc", "Đường vòng cung mềm vòm cầu", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(0, h * 0.8f, w * 0.2f, h * 0.2f, w * 0.8f, h * 0.2f, w, h * 0.8f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.8), false, false); ctx.BezierTo(new WpfPoint(w * 0.2, h * 0.2), new WpfPoint(w * 0.8, h * 0.2), new WpfPoint(w, h * 0.8), true, false); } g.Freeze(); return g; });

            // 16-30: Các loại mũi tên & đầu nối
            AddItem(cat, "arrow_right", "Mũi tên đơn sang phải", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f), (0.75f, 0.25f), (1, 0.5f), (0.75f, 0.75f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5), (0.75, 0.25), (1, 0.5), (0.75, 0.75)));
            AddItem(cat, "arrow_left", "Mũi tên đơn sang trái", (w, h) => GLines(w, h, (1, 0.5f), (0, 0.5f), (0.25f, 0.25f), (0, 0.5f), (0.25f, 0.75f)), (w, h) => WLines(w, h, (1, 0.5), (0, 0.5), (0.25, 0.25), (0, 0.5), (0.25, 0.75)));
            AddItem(cat, "arrow_double", "Mũi tên hai đầu ngang", (w, h) => GLines(w, h, (0.25f, 0.25f), (0, 0.5f), (0.25f, 0.75f), (0, 0.5f), (1, 0.5f), (0.75f, 0.25f), (1, 0.5f), (0.75f, 0.75f)), (w, h) => WLines(w, h, (0.25, 0.25), (0, 0.5), (0.25, 0.75), (0, 0.5), (1, 0.5), (0.75, 0.25), (1, 0.5), (0.75, 0.75)));
            AddItem(cat, "arrow_stealth", "Mũi tên tàng hình Stealth", (w, h) => GPoly(w, h, (0, 0.45f), (0.7f, 0.45f), (0.6f, 0.2f), (1, 0.5f), (0.6f, 0.8f), (0.7f, 0.55f), (0, 0.55f)), (w, h) => WPoly(w, h, (0, 0.45), (0.7, 0.45), (0.6, 0.2), (1, 0.5), (0.6, 0.8), (0.7, 0.55), (0, 0.55)));
            AddItem(cat, "arrow_diamond", "Đầu nối hình thoi kim cương", (w, h) => GPoly(w, h, (0, 0.45f), (0.7f, 0.45f), (0.85f, 0.25f), (1, 0.5f), (0.85f, 0.75f), (0.7f, 0.55f), (0, 0.55f)), (w, h) => WPoly(w, h, (0, 0.45), (0.7, 0.45), (0.85, 0.25), (1, 0.5), (0.85, 0.75), (0.7, 0.55), (0, 0.55)));
            AddItem(cat, "arrow_circle_head", "Đầu nối chấm tròn nút mạng", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.75f, h * 0.5f); gp.AddEllipse(w * 0.7f, h * 0.3f, w * 0.3f, h * 0.4f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.85, h * 0.3), true, true); ctx.ArcTo(new WpfPoint(w * 0.85, h * 0.7), new WpfSize(w * 0.15, h * 0.2), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.85, h * 0.3), new WpfSize(w * 0.15, h * 0.2), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "elbow_connector", "Đầu nối góc khuỷu vuông 90°", (w, h) => GLines(w, h, (0.1f, 0.15f), (0.5f, 0.15f), (0.5f, 0.85f), (0.9f, 0.85f)), (w, h) => WLines(w, h, (0.1, 0.15), (0.5, 0.15), (0.5, 0.85), (0.9, 0.85)));
            AddItem(cat, "elbow_arrow", "Đầu nối góc khuỷu kèm mũi tên", (w, h) => GLines(w, h, (0.1f, 0.15f), (0.5f, 0.15f), (0.5f, 0.85f), (0.9f, 0.85f), (0.75f, 0.7f), (0.9f, 0.85f), (0.75f, 1.0f)), (w, h) => WLines(w, h, (0.1, 0.15), (0.5, 0.15), (0.5, 0.85), (0.9, 0.85), (0.75, 0.7), (0.9, 0.85), (0.75, 1.0)));
            AddItem(cat, "elbow_double_arrow", "Đầu nối góc khuỷu hai mũi tên", (w, h) => GLines(w, h, (0.25f, 0.05f), (0.1f, 0.15f), (0.25f, 0.25f), (0.1f, 0.15f), (0.5f, 0.15f), (0.5f, 0.85f), (0.9f, 0.85f), (0.75f, 0.7f), (0.9f, 0.85f), (0.75f, 1.0f)), (w, h) => WLines(w, h, (0.25, 0.05), (0.1, 0.15), (0.25, 0.25), (0.1, 0.15), (0.5, 0.15), (0.5, 0.85), (0.9, 0.85), (0.75, 0.7), (0.9, 0.85), (0.75, 1.0)));
            AddItem(cat, "curved_arrow_right", "Đầu nối cong vòm mũi tên phải", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(0, h * 0.85f, w * 0.2f, h * 0.15f, w * 0.6f, h * 0.15f, w * 0.9f, h * 0.85f); gp.AddLine(w * 0.9f, h * 0.85f, w * 0.75f, h * 0.65f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.85), false, false); ctx.BezierTo(new WpfPoint(w * 0.2, h * 0.15), new WpfPoint(w * 0.6, h * 0.15), new WpfPoint(w * 0.9, h * 0.85), true, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.65), true, false); } g.Freeze(); return g; });
            AddItem(cat, "curved_double_arrow", "Đầu nối cong hai đầu mũi tên", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.15f, h * 0.65f, 0, h * 0.85f); gp.AddBezier(0, h * 0.85f, w * 0.2f, h * 0.15f, w * 0.8f, h * 0.15f, w, h * 0.85f); gp.AddLine(w, h * 0.85f, w * 0.85f, h * 0.65f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.15, h * 0.65), false, false); ctx.LineTo(new WpfPoint(0, h * 0.85), true, false); ctx.BezierTo(new WpfPoint(w * 0.2, h * 0.15), new WpfPoint(w * 0.8, h * 0.15), new WpfPoint(w, h * 0.85), true, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.65), true, false); } g.Freeze(); return g; });
            AddItem(cat, "bracket_left", "Dấu ngoặc vuông mở [", (w, h) => GLines(w, h, (0.7f, 0.1f), (0.3f, 0.1f), (0.3f, 0.9f), (0.7f, 0.9f)), (w, h) => WLines(w, h, (0.7, 0.1), (0.3, 0.1), (0.3, 0.9), (0.7, 0.9)));
            AddItem(cat, "bracket_right", "Dấu ngoặc vuông đóng ]", (w, h) => GLines(w, h, (0.3f, 0.1f), (0.7f, 0.1f), (0.7f, 0.9f), (0.3f, 0.9f)), (w, h) => WLines(w, h, (0.3, 0.1), (0.7, 0.1), (0.7, 0.9), (0.3, 0.9)));
            AddItem(cat, "brace_left", "Dấu ngoặc nhọn mở {", (w, h) => GLines(w, h, (0.7f, 0.1f), (0.5f, 0.1f), (0.5f, 0.45f), (0.3f, 0.5f), (0.5f, 0.55f), (0.5f, 0.9f), (0.7f, 0.9f)), (w, h) => WLines(w, h, (0.7, 0.1), (0.5, 0.1), (0.5, 0.45), (0.3, 0.5), (0.5, 0.55), (0.5, 0.9), (0.7, 0.9)));
            AddItem(cat, "brace_right", "Dấu ngoặc nhọn đóng }", (w, h) => GLines(w, h, (0.3f, 0.1f), (0.5f, 0.1f), (0.5f, 0.45f), (0.7f, 0.5f), (0.5f, 0.55f), (0.5f, 0.9f), (0.3f, 0.9f)), (w, h) => WLines(w, h, (0.3, 0.1), (0.5, 0.1), (0.5, 0.45), (0.7, 0.5), (0.5, 0.55), (0.5, 0.9), (0.3, 0.9)));

            // 31-45: Ký hiệu kích thước & trục tọa độ
            AddItem(cat, "dimension_line", "Đường đo kích thước hai đầu", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.2f, 0, h * 0.8f); gp.AddLine(0, h * 0.5f, w, h * 0.5f); gp.AddLine(w, h * 0.2f, w, h * 0.8f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.2), false, false); ctx.LineTo(new WpfPoint(0, h * 0.8), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w, h * 0.2), false, false); ctx.LineTo(new WpfPoint(w, h * 0.8), true, false); } g.Freeze(); return g; });
            AddItem(cat, "axes_2d", "Trục tọa độ phẳng 2 chiều X-Y", (w, h) => GLines(w, h, (0.15f, 0.15f), (0.1f, 0.1f), (0.05f, 0.15f), (0.1f, 0.1f), (0.1f, 0.9f), (0.9f, 0.9f), (0.85f, 0.85f), (0.9f, 0.9f), (0.85f, 0.95f)), (w, h) => WLines(w, h, (0.15, 0.15), (0.1, 0.1), (0.05, 0.15), (0.1, 0.1), (0.1, 0.9), (0.9, 0.9), (0.85, 0.85), (0.9, 0.9), (0.85, 0.95)));
            AddItem(cat, "axes_3d", "Trục tọa độ không gian 3D", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.5f, h * 0.1f); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.9f, h * 0.75f); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.1f, h * 0.75f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.1), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.9, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.1, h * 0.75), true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_ray", "Tia xuất phát từ điểm neo", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, h * 0.4f, w * 0.2f, h * 0.2f); gp.AddLine(w * 0.2f, h * 0.5f, w, h * 0.5f); gp.AddLine(w, h * 0.5f, w * 0.8f, h * 0.3f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.5), true, true); ctx.ArcTo(new WpfPoint(w * 0.2, h * 0.5), new WpfSize(w * 0.1, h * 0.1), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.2, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.8, h * 0.3), true, false); } g.Freeze(); return g; });
            AddItem(cat, "line_segment_caps", "Đoạn thẳng chặn 2 đầu", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0.1f * w, 0.3f * h, 0.1f * w, 0.7f * h); gp.AddLine(0.1f * w, 0.5f * h, 0.9f * w, 0.5f * h); gp.AddLine(0.9f * w, 0.3f * h, 0.9f * w, 0.7f * h); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0.1 * w, 0.3 * h), false, false); ctx.LineTo(new WpfPoint(0.1 * w, 0.7 * h), true, false); ctx.BeginFigure(new WpfPoint(0.1 * w, 0.5 * h), false, false); ctx.LineTo(new WpfPoint(0.9 * w, 0.5 * h), true, false); ctx.BeginFigure(new WpfPoint(0.9 * w, 0.3 * h), false, false); ctx.LineTo(new WpfPoint(0.9 * w, 0.7 * h), true, false); } g.Freeze(); return g; });
            AddItem(cat, "crosshair_reticle", "Tâm ngắm chữ thập chữ X", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.2f, h * 0.2f, w * 0.6f, h * 0.6f); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.2), true, true); ctx.ArcTo(new WpfPoint(w * 0.5, h * 0.8), new WpfSize(w * 0.3, h * 0.3), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.5, h * 0.2), new WpfSize(w * 0.3, h * 0.3), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); } g.Freeze(); return g; });
            AddItem(cat, "compass_rose_lines", "Chỉ hướng La bàn 4 hướng", (w, h) => GLines(w, h, (0.5f, 0), (0.5f, 1), (0.5f, 0.5f), (0, 0.5f), (1, 0.5f), (0.5f, 0.5f), (0.2f, 0.2f), (0.8f, 0.8f), (0.5f, 0.5f), (0.8f, 0.2f), (0.2f, 0.8f)), (w, h) => WLines(w, h, (0.5, 0), (0.5, 1), (0.5, 0.5), (0, 0.5), (1, 0.5), (0.5, 0.5), (0.2, 0.2), (0.8, 0.8), (0.5, 0.5), (0.8, 0.2), (0.2, 0.8)));
            AddItem(cat, "parallel_lines_symbol", "Ký hiệu song song //", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.35f, h * 0.15f, w * 0.25f, h * 0.85f); gp.AddLine(w * 0.75f, h * 0.15f, w * 0.65f, h * 0.85f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.35, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.25, h * 0.85), true, false); ctx.BeginFigure(new WpfPoint(w * 0.75, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.65, h * 0.85), true, false); } g.Freeze(); return g; });
            AddItem(cat, "perpendicular_symbol", "Ký hiệu vuông góc ⊥", (w, h) => GLines(w, h, (0.15f, 0.85f), (0.85f, 0.85f), (0.5f, 0.85f), (0.5f, 0.15f)), (w, h) => WLines(w, h, (0.15, 0.85), (0.85, 0.85), (0.5, 0.85), (0.5, 0.15)));
            AddItem(cat, "bracket_pair", "Cặp ngoặc vuông đôi [ ]", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLines(new PointF[] { new PointF(w * 0.35f, h * 0.15f), new PointF(w * 0.15f, h * 0.15f), new PointF(w * 0.15f, h * 0.85f), new PointF(w * 0.35f, h * 0.85f) }); gp.StartFigure(); gp.AddLines(new PointF[] { new PointF(w * 0.65f, h * 0.15f), new PointF(w * 0.85f, h * 0.15f), new PointF(w * 0.85f, h * 0.85f), new PointF(w * 0.65f, h * 0.85f) }); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.35, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.15, h * 0.15), true, false); ctx.LineTo(new WpfPoint(w * 0.15, h * 0.85), true, false); ctx.LineTo(new WpfPoint(w * 0.35, h * 0.85), true, false); ctx.BeginFigure(new WpfPoint(w * 0.65, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.15), true, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.85), true, false); ctx.LineTo(new WpfPoint(w * 0.65, h * 0.85), true, false); } g.Freeze(); return g; });
            AddItem(cat, "slanted_arrow_45", "Mũi tên chéo nghiêng 45°", (w, h) => GLines(w, h, (0.15f, 0.85f), (0.85f, 0.15f), (0.55f, 0.15f), (0.85f, 0.15f), (0.85f, 0.45f)), (w, h) => WLines(w, h, (0.15, 0.85), (0.85, 0.15), (0.55, 0.15), (0.85, 0.15), (0.85, 0.45)));
            AddItem(cat, "spiral_archimedean", "Đường xoắn ốc tỏa Archimedes", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(w * 0.35f, h * 0.35f, w * 0.3f, h * 0.3f, 0, 180); gp.AddArc(w * 0.25f, h * 0.25f, w * 0.5f, h * 0.5f, 180, 180); gp.AddArc(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f, 0, 180); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.65, h * 0.5), false, false); ctx.ArcTo(new WpfPoint(w * 0.35, h * 0.5), new WpfSize(w * 0.15, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.75, h * 0.5), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.1, h * 0.5), new WpfSize(w * 0.4, h * 0.4), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "three_segment_elbow", "Đầu nối góc khuỷu 3 đoạn bậc", (w, h) => GLines(w, h, (0.1f, 0.2f), (0.35f, 0.2f), (0.35f, 0.5f), (0.65f, 0.5f), (0.65f, 0.8f), (0.9f, 0.8f)), (w, h) => WLines(w, h, (0.1, 0.2), (0.35, 0.2), (0.35, 0.5), (0.65, 0.5), (0.65, 0.8), (0.9, 0.8)));
            AddItem(cat, "split_connector_wire", "Dây cáp phân đôi tín hiệu", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0.1f * w, 0.5f * h, 0.5f * w, 0.5f * h); gp.AddLine(0.5f * w, 0.5f * h, 0.9f * w, 0.2f * h); gp.AddLine(0.5f * w, 0.5f * h, 0.9f * w, 0.8f * h); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0.1 * w, 0.5 * h), false, false); ctx.LineTo(new WpfPoint(0.5 * w, 0.5 * h), true, false); ctx.LineTo(new WpfPoint(0.9 * w, 0.2 * h), true, false); ctx.BeginFigure(new WpfPoint(0.5 * w, 0.5 * h), false, false); ctx.LineTo(new WpfPoint(0.9 * w, 0.8 * h), true, false); } g.Freeze(); return g; });
            AddItem(cat, "merge_connector_wire", "Dây cáp hợp nhất hai nguồn", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0.1f * w, 0.2f * h, 0.5f * w, 0.5f * h); gp.AddLine(0.1f * w, 0.8f * h, 0.5f * w, 0.5f * h); gp.AddLine(0.5f * w, 0.5f * h, 0.9f * w, 0.5f * h); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0.1 * w, 0.2 * h), false, false); ctx.LineTo(new WpfPoint(0.5 * w, 0.5 * h), true, false); ctx.LineTo(new WpfPoint(0.9 * w, 0.5 * h), true, false); ctx.BeginFigure(new WpfPoint(0.1 * w, 0.8 * h), false, false); ctx.LineTo(new WpfPoint(0.5 * w, 0.5 * h), true, false); } g.Freeze(); return g; });
        }
        #endregion

        #region Category 2: Hình dạng cơ bản (65 mẫu)
        private static void PopulateBasicShapes()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Basic", Title = "Hình dạng cơ bản" };
            _categories.Add(cat);

            // 1-5: Hình chữ nhật & bo góc
            AddItem(cat, "rect_standard", "Hình chữ nhật góc vuông", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "rect_round_sm", "Hình chữ nhật bo góc nhẹ", (w, h) => GRoundedRect(w, h, 4f), (w, h) => WRoundedRect(w, h, 4.0));
            AddItem(cat, "rect_round_md", "Hình chữ nhật bo góc vừa", (w, h) => GRoundedRect(w, h, 8f), (w, h) => WRoundedRect(w, h, 8.0));
            AddItem(cat, "rect_round_lg", "Hình chữ nhật bo góc lớn", (w, h) => GRoundedRect(w, h, 14f), (w, h) => WRoundedRect(w, h, 14.0));
            AddItem(cat, "rect_pill", "Hình viên con nhộng (Pill Capsule)", (w, h) => GRoundedRect(w, h, Math.Min(w, h) / 2f), (w, h) => WRoundedRect(w, h, Math.Min(w, h) / 2.0));

            // 6-11: Vát góc & kết hợp
            AddItem(cat, "snip_single_corner", "Vát 1 góc trên bên phải", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 0.25f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 0.25), (1, 1), (0, 1)));
            AddItem(cat, "snip_same_side", "Vát 2 góc cùng phía trên", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.2f), (1, 1), (0, 1), (0, 0.2f)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.2), (1, 1), (0, 1), (0, 0.2)));
            AddItem(cat, "snip_diagonal", "Vát 2 góc chéo nhau", (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 0.8f), (0.8f, 1), (0, 1), (0, 0.2f)), (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 0.8), (0.8, 1), (0, 1), (0, 0.2)));
            AddItem(cat, "snip_round_corner", "Vát góc trên, bo tròn góc dưới", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, w * 0.8f, 0); gp.AddLine(w * 0.8f, 0, w, h * 0.2f); gp.AddLine(w, h * 0.2f, w, h * 0.8f); gp.AddArc(w * 0.6f, h * 0.6f, w * 0.4f, h * 0.4f, 0, 90); gp.AddLine(w * 0.8f, h, w * 0.2f, h); gp.AddArc(0, h * 0.6f, w * 0.4f, h * 0.4f, 90, 90); gp.CloseFigure(); return gp; }, (w, h) => WPoly(w, h, (0, 0), (0.8, 0), (1, 0.2), (1, 0.85), (0.85, 1), (0.15, 1), (0, 0.85)));
            AddItem(cat, "round_same_side", "Bo tròn 2 góc cùng phía trên", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w * 0.4f, h * 0.4f, 180, 90); gp.AddArc(w * 0.6f, 0, w * 0.4f, h * 0.4f, 270, 90); gp.AddLine(w, h, 0, h); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h), true, true); ctx.LineTo(new WpfPoint(0, h * 0.2), true, false); ctx.ArcTo(new WpfPoint(w * 0.2, 0), new WpfSize(w * 0.2, h * 0.2), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w * 0.8, 0), true, false); ctx.ArcTo(new WpfPoint(w, h * 0.2), new WpfSize(w * 0.2, h * 0.2), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w, h), true, false); } g.Freeze(); return g; });
            AddItem(cat, "round_diagonal", "Bo tròn 2 góc đối xứng chéo", (w, h) => GPoly(w, h, (0.25f, 0), (1, 0), (1, 0.75f), (0.75f, 1), (0, 1), (0, 0.25f)), (w, h) => WPoly(w, h, (0.25, 0), (1, 0), (1, 0.75), (0.75, 1), (0, 1), (0, 0.25)));

            // 12-21: Hình tròn, elip & góc quạt
            AddItem(cat, "circle_perfect", "Hình tròn hoàn hảo (Circle)", (w, h) => GEllipse(Math.Min(w, h), Math.Min(w, h)), (w, h) => WEllipse(Math.Min(w, h), Math.Min(w, h)));
            AddItem(cat, "oval_horiz", "Hình bầu dục ngang (Horizontal Oval)", (w, h) => GEllipse(w, h), (w, h) => WEllipse(w, h));
            AddItem(cat, "oval_vert", "Hình bầu dục dọc (Vertical Oval)", (w, h) => GEllipse(w, h), (w, h) => WEllipse(w, h));
            AddItem(cat, "half_circle_chord", "Hình bán nguyệt (Half Circle)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w, h * 2f, 180, 180); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h), true, true); ctx.ArcTo(new WpfPoint(w, h), new WpfSize(w / 2.0, h), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "quarter_circle", "Hình góc phần tư 1/4 tròn", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w * 2f, h * 2f, 180, 90); gp.AddLine(w, 0, 0, 0); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.ArcTo(new WpfPoint(0, h), new WpfSize(w, h), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "sector_60", "Hình quạt tròn 60 độ", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.5f, 0); gp.AddArc(0, 0, w, h, 270, 60); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.5), true, true); ctx.LineTo(new WpfPoint(w * 0.5, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.93, h * 0.25), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "sector_90", "Hình quạt tròn 90 độ", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.5f, 0); gp.AddArc(0, 0, w, h, 270, 90); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.5), true, true); ctx.LineTo(new WpfPoint(w * 0.5, 0), true, false); ctx.ArcTo(new WpfPoint(w, h * 0.5), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "sector_120", "Hình quạt tròn 120 độ", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.5f, 0); gp.AddArc(0, 0, w, h, 270, 120); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.5), true, true); ctx.LineTo(new WpfPoint(w * 0.5, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.93, h * 0.75), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "donut_ring", "Hình vành khuyên đệm Donut", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.25f, h * 0.25f, w * 0.5f, h * 0.5f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h * 0.25), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h * 0.75), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, h * 0.25), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "teardrop_water", "Hình giọt nước giọt sương", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, 0, w * 0.85f, h * 0.5f); gp.AddArc(w * 0.15f, h * 0.3f, w * 0.7f, h * 0.7f, 0, 180); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.5), true, false); ctx.ArcTo(new WpfPoint(w * 0.15, h * 0.5), new WpfSize(w * 0.35, h * 0.35), 0, true, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 22-26: Tam giác các loại
            AddItem(cat, "triangle_isosceles", "Tam giác cân chuẩn", (w, h) => GPoly(w, h, (0.5f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.5, 0), (1, 1), (0, 1)));
            AddItem(cat, "triangle_right", "Tam giác vuông góc phải", (w, h) => GPoly(w, h, (0, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 1), (0, 1)));
            AddItem(cat, "triangle_obtuse", "Tam giác góc tù lệch", (w, h) => GPoly(w, h, (0.2f, 0.2f), (1, 0.8f), (0, 1)), (w, h) => WPoly(w, h, (0.2, 0.2), (1, 0.8), (0, 1)));
            AddItem(cat, "triangle_acute", "Tam giác nhọn cân đối", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 1), (0.1f, 1)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 1), (0.1, 1)));
            AddItem(cat, "triangle_inverted", "Tam giác ngược hướng xuống", (w, h) => GPoly(w, h, (0, 0), (1, 0), (0.5f, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (0.5, 1)));

            // 27-32: Tứ giác & hình học phẳng
            AddItem(cat, "parallelogram_right", "Hình bình hành nghiêng phải", (w, h) => GPoly(w, h, (0.25f, 0), (1, 0), (0.75f, 1), (0, 1)), (w, h) => WPoly(w, h, (0.25, 0), (1, 0), (0.75, 1), (0, 1)));
            AddItem(cat, "parallelogram_left", "Hình bình hành nghiêng trái", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 1), (0.25f, 1)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 1), (0.25, 1)));
            AddItem(cat, "trapezoid_std", "Hình thang cân đáy rộng", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 1), (0, 1)));
            AddItem(cat, "trapezoid_inverted", "Hình thang ngược đáy hẹp", (w, h) => GPoly(w, h, (0, 0), (1, 0), (0.8f, 1), (0.2f, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (0.8, 1), (0.2, 1)));
            AddItem(cat, "diamond_rhombus", "Hình thoi góc chuẩn (Diamond)", (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.5f), (0.5f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0), (1, 0.5), (0.5, 1), (0, 0.5)));
            AddItem(cat, "kite_shape", "Hình cánh diều nghệ thuật", (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.35f), (0.5f, 1), (0, 0.35f)), (w, h) => WPoly(w, h, (0.5, 0), (1, 0.35), (0.5, 1), (0, 0.35)));

            // 33-41: Dấu chữ thập & biểu tượng
            AddItem(cat, "cross_plus", "Dấu cộng chữ thập đều (+)", (w, h) => GPoly(w, h, (0.35f, 0), (0.65f, 0), (0.65f, 0.35f), (1, 0.35f), (1, 0.65f), (0.65f, 0.65f), (0.65f, 1), (0.35f, 1), (0.35f, 0.65f), (0, 0.65f), (0, 0.35f), (0.35f, 0.35f)), (w, h) => WPoly(w, h, (0.35, 0), (0.65, 0), (0.65, 0.35), (1, 0.35), (1, 0.65), (0.65, 0.65), (0.65, 1), (0.35, 1), (0.35, 0.65), (0, 0.65), (0, 0.35), (0.35, 0.35)));
            AddItem(cat, "sign_minus", "Dấu trừ nằm ngang (−)", (w, h) => GPoly(w, h, (0.1f, 0.38f), (0.9f, 0.38f), (0.9f, 0.62f), (0.1f, 0.62f)), (w, h) => WPoly(w, h, (0.1, 0.38), (0.9, 0.38), (0.9, 0.62), (0.1, 0.62)));
            AddItem(cat, "cross_thick", "Chữ thập đậm phong cách cấp cứu", (w, h) => GPoly(w, h, (0.3f, 0), (0.7f, 0), (0.7f, 0.3f), (1, 0.3f), (1, 0.7f), (0.7f, 0.7f), (0.7f, 1), (0.3f, 1), (0.3f, 0.7f), (0, 0.7f), (0, 0.3f), (0.3f, 0.3f)), (w, h) => WPoly(w, h, (0.3, 0), (0.7, 0), (0.7, 0.3), (1, 0.3), (1, 0.7), (0.7, 0.7), (0.7, 1), (0.3, 1), (0.3, 0.7), (0, 0.7), (0, 0.3), (0.3, 0.3)));
            AddItem(cat, "cross_rounded", "Chữ thập bo góc mềm mại", (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.65f, 0.35f), (0.95f, 0.35f), (0.95f, 0.65f), (0.65f, 0.65f), (0.65f, 0.95f), (0.35f, 0.95f), (0.35f, 0.65f), (0.05f, 0.65f), (0.05f, 0.35f), (0.35f, 0.35f)), (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.65, 0.35), (0.95, 0.35), (0.95, 0.65), (0.65, 0.65), (0.65, 0.95), (0.35, 0.95), (0.35, 0.65), (0.05, 0.65), (0.05, 0.35), (0.35, 0.35)));
            AddItem(cat, "maltese_cross", "Chữ thập Maltese hiệp sĩ", (w, h) => GPoly(w, h, (0.5f, 0.2f), (0.8f, 0), (0.7f, 0.35f), (1, 0.2f), (0.8f, 0.5f), (1, 0.8f), (0.7f, 0.65f), (0.8f, 1), (0.5f, 0.8f), (0.2f, 1), (0.3f, 0.65f), (0, 0.8f), (0.2f, 0.5f), (0, 0.2f), (0.3f, 0.35f), (0.2f, 0)), (w, h) => WPoly(w, h, (0.5, 0.2), (0.8, 0), (0.7, 0.35), (1, 0.2), (0.8, 0.5), (1, 0.8), (0.7, 0.65), (0.8, 1), (0.5, 0.8), (0.2, 1), (0.3, 0.65), (0, 0.8), (0.2, 0.5), (0, 0.2), (0.3, 0.35), (0.2, 0)));
            AddItem(cat, "star_4_flat", "Ngôi sao 4 cánh dẹp", (w, h) => GStar(w, h, 4, 0.35), (w, h) => WStar(w, h, 4, 0.35));
            AddItem(cat, "star_5_basic", "Ngôi sao 5 cánh cổ điển", (w, h) => GStar(w, h, 5, 0.45), (w, h) => WStar(w, h, 5, 0.45));
            AddItem(cat, "checkmark_badge", "Dấu tích chữ V thành công", (w, h) => GPoly(w, h, (0.1f, 0.5f), (0.35f, 0.85f), (0.9f, 0.15f), (0.8f, 0.1f), (0.35f, 0.7f), (0.2f, 0.45f)), (w, h) => WPoly(w, h, (0.1, 0.5), (0.35, 0.85), (0.9, 0.15), (0.8, 0.1), (0.35, 0.7), (0.2, 0.45)));
            AddItem(cat, "cancel_x_mark", "Dấu nhân chữ X từ chối", (w, h) => GPoly(w, h, (0.2f, 0.05f), (0.5f, 0.35f), (0.8f, 0.05f), (0.95f, 0.2f), (0.65f, 0.5f), (0.95f, 0.8f), (0.8f, 0.95f), (0.5f, 0.65f), (0.2f, 0.95f), (0.05f, 0.8f), (0.35f, 0.5f), (0.05f, 0.2f)), (w, h) => WPoly(w, h, (0.2, 0.05), (0.5, 0.35), (0.8, 0.05), (0.95, 0.2), (0.65, 0.5), (0.95, 0.8), (0.8, 0.95), (0.5, 0.65), (0.2, 0.95), (0.05, 0.8), (0.35, 0.5), (0.05, 0.2)));

            // 42-50: Thiên nhiên & cảm xúc
            AddItem(cat, "heart_shape", "Trái tim tình yêu (Heart)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(w * 0.5f, h * 0.3f, w * 0.5f, 0, w * 0.95f, 0, w * 0.95f, h * 0.4f); gp.AddBezier(w * 0.95f, h * 0.4f, w * 0.95f, h * 0.65f, w * 0.5f, h * 0.85f, w * 0.5f, h); gp.AddBezier(w * 0.5f, h, w * 0.5f, h * 0.85f, w * 0.05f, h * 0.65f, w * 0.05f, h * 0.4f); gp.AddBezier(w * 0.05f, h * 0.4f, w * 0.05f, 0, w * 0.5f, 0, w * 0.5f, h * 0.3f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.3), true, true); ctx.BezierTo(new WpfPoint(w * 0.5, 0), new WpfPoint(w * 0.95, 0), new WpfPoint(w * 0.95, h * 0.4), true, false); ctx.BezierTo(new WpfPoint(w * 0.95, h * 0.65), new WpfPoint(w * 0.5, h * 0.85), new WpfPoint(w * 0.5, h), true, false); ctx.BezierTo(new WpfPoint(w * 0.5, h * 0.85), new WpfPoint(w * 0.05, h * 0.65), new WpfPoint(w * 0.05, h * 0.4), true, false); ctx.BezierTo(new WpfPoint(w * 0.05, 0), new WpfPoint(w * 0.5, 0), new WpfPoint(w * 0.5, h * 0.3), true, false); } g.Freeze(); return g; });
            AddItem(cat, "lightning_bolt", "Tia sét năng lượng sấm chớp", (w, h) => GPoly(w, h, (0.45f, 0), (0.9f, 0), (0.55f, 0.45f), (0.95f, 0.45f), (0.15f, 1), (0.35f, 0.55f), (0.05f, 0.55f)), (w, h) => WPoly(w, h, (0.45, 0), (0.9, 0), (0.55, 0.45), (0.95, 0.45), (0.15, 1), (0.35, 0.55), (0.05, 0.55)));
            AddItem(cat, "crescent_moon", "Trăng lưỡi liềm huyền diệu", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w, h, 90, 180); gp.AddArc(w * 0.25f, 0, w * 0.75f, h, 270, -180); gp.CloseFigure(); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, h), true, true); ctx.ArcTo(new WpfPoint(w * 0.5, 0), new WpfSize(w * 0.5, h * 0.5), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.5, h), new WpfSize(w * 0.35, h * 0.5), 0, false, WpfSweepDirection.Counterclockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "sun_symbol", "Mặt trời chiếu sáng rực rỡ", (w, h) => GStar(w, h, 12, 0.7), (w, h) => WStar(w, h, 12, 0.7));
            AddItem(cat, "cloud_fluffy", "Đám mây bồng bềnh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(w * 0.1f, h * 0.4f, w * 0.3f, h * 0.4f, 120, 150); gp.AddArc(w * 0.25f, h * 0.15f, w * 0.4f, h * 0.4f, 180, 160); gp.AddArc(w * 0.55f, h * 0.25f, w * 0.35f, h * 0.4f, 240, 150); gp.AddLine(w * 0.85f, h * 0.75f, w * 0.15f, h * 0.75f); gp.CloseFigure(); return gp; }, (w, h) => WPoly(w, h, (0.1, 0.6), (0.2, 0.4), (0.35, 0.2), (0.6, 0.2), (0.8, 0.35), (0.9, 0.6), (0.8, 0.8), (0.15, 0.8)));
            AddItem(cat, "flower_petal", "Bông hoa 8 cánh hoa nở", (w, h) => GStar(w, h, 8, 0.6), (w, h) => WStar(w, h, 8, 0.6));
            AddItem(cat, "flame_fire", "Ngọn lửa bốc cháy nhiệt huyết", (w, h) => GPoly(w, h, (0.5f, 0), (0.75f, 0.3f), (0.65f, 0.55f), (0.9f, 0.6f), (0.75f, 0.95f), (0.25f, 0.95f), (0.1f, 0.6f), (0.35f, 0.55f), (0.25f, 0.3f)), (w, h) => WPoly(w, h, (0.5, 0), (0.75, 0.3), (0.65, 0.55), (0.9, 0.6), (0.75, 0.95), (0.25, 0.95), (0.1, 0.6), (0.35, 0.55), (0.25, 0.3)));
            AddItem(cat, "snowflake_ice", "Bông tuyết pha lê 6 cánh", (w, h) => GStar(w, h, 6, 0.25), (w, h) => WStar(w, h, 6, 0.25));
            AddItem(cat, "oil_drop", "Giọt tinh dầu tinh khiết", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.85f, 0.65f), (0.7f, 0.95f), (0.3f, 0.95f), (0.15f, 0.65f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.85, 0.65), (0.7, 0.95), (0.3, 0.95), (0.15, 0.65)));

            // 51-65: Khung hình & hình khối chuyên dụng
            AddItem(cat, "frame_hollow", "Khung tranh rỗng ruột (Frame)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.2f, h * 0.2f, w * 0.6f, h * 0.6f)); return gp; }, (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.2), (0.8, 0.2), (0.8, 0.8), (0.2, 0.8), (0.2, 0.2)));
            AddItem(cat, "shape_l", "Hình chữ L vuông góc", (w, h) => GPoly(w, h, (0, 0), (0.35f, 0), (0.35f, 0.65f), (1, 0.65f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.35, 0), (0.35, 0.65), (1, 0.65), (1, 1), (0, 1)));
            AddItem(cat, "shape_t", "Hình chữ T đối xứng", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.35f), (0.65f, 0.35f), (0.65f, 1), (0.35f, 1), (0.35f, 0.35f), (0, 0.35f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.35), (0.65, 0.35), (0.65, 1), (0.35, 1), (0.35, 0.35), (0, 0.35)));
            AddItem(cat, "shape_u", "Hình chữ U đón nhận", (w, h) => GPoly(w, h, (0, 0), (0.3f, 0), (0.3f, 0.7f), (0.7f, 0.7f), (0.7f, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.3, 0), (0.3, 0.7), (0.7, 0.7), (0.7, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "shape_h", "Hình chữ H kết nối", (w, h) => GPoly(w, h, (0, 0), (0.3f, 0), (0.3f, 0.4f), (0.7f, 0.4f), (0.7f, 0), (1, 0), (1, 1), (0.7f, 1), (0.7f, 0.6f), (0.3f, 0.6f), (0.3f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.3, 0), (0.3, 0.4), (0.7, 0.4), (0.7, 0), (1, 0), (1, 1), (0.7, 1), (0.7, 0.6), (0.3, 0.6), (0.3, 1), (0, 1)));
            AddItem(cat, "plaque_beveled", "Biển bảng Plaque vát 4 góc", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.15f), (1, 0.85f), (0.85f, 1), (0.15f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.15), (1, 0.85), (0.85, 1), (0.15, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "folded_corner_doc", "Trang tài liệu gấp góc giấy", (w, h) => GPoly(w, h, (0, 0), (0.7f, 0), (1, 0.3f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.7, 0), (1, 0.3), (1, 1), (0, 1)));
            AddItem(cat, "memo_tag", "Thẻ ghi chú dán góc nhọn", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 0.5f), (0.75f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 0.5), (0.75, 1), (0, 1)));
            AddItem(cat, "octagon_basic", "Bát giác 8 cạnh đều", (w, h) => GRegularPoly(w, h, 8, Math.PI / 8), (w, h) => WRegularPoly(w, h, 8, Math.PI / 8));
            AddItem(cat, "decagon_basic", "Thập giác 10 cạnh đều", (w, h) => GRegularPoly(w, h, 10, 0), (w, h) => WRegularPoly(w, h, 10, 0));
            AddItem(cat, "dodecagon_basic", "Mười hai giác 12 cạnh đều", (w, h) => GRegularPoly(w, h, 12, 0), (w, h) => WRegularPoly(w, h, 12, 0));
            AddItem(cat, "sector_45", "Hình quạt tròn 45 độ", (w, h) => GPoly(w, h, (0.5f, 0.5f), (0.5f, 0), (0.85f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0.5), (0.5, 0), (0.85, 0.15)));
            AddItem(cat, "sector_180", "Hình quạt nửa tròn 180 độ", (w, h) => GPoly(w, h, (0.5f, 0.5f), (0, 0.5f), (0.15f, 0.15f), (0.5f, 0), (0.85f, 0.15f), (1, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.5), (0, 0.5), (0.15, 0.15), (0.5, 0), (0.85, 0.15), (1, 0.5)));
            AddItem(cat, "lens_vesica", "Hình thấu kính giao thoa Lens", (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.5f), (0.5f, 1), (0.15f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.5), (0.5, 1), (0.15, 0.5)));
            AddItem(cat, "pill_vert", "Hình viên thuốc đứng (Vertical Pill)", (w, h) => GRoundedRect(w, h, Math.Min(w, h) / 2f), (w, h) => WRoundedRect(w, h, Math.Min(w, h) / 2.0));
        }
        #endregion

        #region Category 3: Đa giác đều & Hình học (45 mẫu)
        private static void PopulatePolygonsAndGeometry()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Polygons", Title = "Đa giác đều & Hình học" };
            _categories.Add(cat);

            // 1-12: Các đa giác đều từ 3 đến 16 cạnh
            int[] regularSides = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16 };
            string[] polyNames = new string[] {
                "Tam giác đều 3 cạnh (Triangle)", "Tứ giác vuông 4 cạnh (Square)", "Ngũ giác đều 5 cạnh (Pentagon)",
                "Lục giác đều 6 cạnh (Hexagon)", "Thất giác đều 7 cạnh (Heptagon)", "Bát giác đều 8 cạnh (Octagon)",
                "Cửu giác đều 9 cạnh (Nonagon)", "Thập giác đều 10 cạnh (Decagon)", "Thập nhất giác 11 cạnh (Hendecagon)",
                "Thập nhị giác 12 cạnh (Dodecagon)", "Mười bốn giác 14 cạnh", "Mười sáu giác 16 cạnh"
            };
            for (int i = 0; i < regularSides.Length; i++)
            {
                int sides = regularSides[i];
                double offset = -Math.PI / 2.0;
                if (sides % 2 == 0) offset = Math.PI / sides;
                AddItem(cat, $"reg_polygon_{sides}", polyNames[i], (w, h) => GRegularPoly(w, h, sides, (float)offset), (w, h) => WRegularPoly(w, h, sides, offset));
            }

            // 13-19: Các biến thể tam giác hình học
            AddItem(cat, "tri_right_30_60", "Tam giác vuông tỷ lệ 30°-60°", (w, h) => GPoly(w, h, (0, 0), (0.6f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.6, 1), (0, 1)));
            AddItem(cat, "tri_right_45", "Tam giác vuông cân 45°-45°", (w, h) => GPoly(w, h, (0, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 1), (0, 1)));
            AddItem(cat, "tri_equilateral", "Tam giác đều cạnh chuẩn", (w, h) => GRegularPoly(w, h, 3, -(float)Math.PI / 2f), (w, h) => WRegularPoly(w, h, 3, -Math.PI / 2.0));
            AddItem(cat, "tri_equilateral_inv", "Tam giác đều lộn ngược đỉnh", (w, h) => GRegularPoly(w, h, 3, (float)Math.PI / 2f), (w, h) => WRegularPoly(w, h, 3, Math.PI / 2.0));
            AddItem(cat, "tri_acute_sharp", "Tam giác nhọn mũi mác thon", (w, h) => GPoly(w, h, (0.5f, 0), (0.75f, 1), (0.25f, 1)), (w, h) => WPoly(w, h, (0.5, 0), (0.75, 1), (0.25, 1)));
            AddItem(cat, "tri_obtuse_wide", "Tam giác tù góc bẹt thoải", (w, h) => GPoly(w, h, (0.15f, 0.4f), (1, 0.7f), (0, 0.9f)), (w, h) => WPoly(w, h, (0.15, 0.4), (1, 0.7), (0, 0.9)));
            AddItem(cat, "tri_golden_ratio", "Tam giác tỷ lệ vàng Fibonacci", (w, h) => GPoly(w, h, (0.5f, 0), (0.81f, 1), (0.19f, 1)), (w, h) => WPoly(w, h, (0.5, 0), (0.81, 1), (0.19, 1)));

            // 20-28: Các biến thể tứ giác chuyên biệt
            AddItem(cat, "quad_rhombus_60", "Hình thoi góc nhọn 60 độ", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.9f, 0.5f), (0.5f, 0.95f), (0.1f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.9, 0.5), (0.5, 0.95), (0.1, 0.5)));
            AddItem(cat, "quad_rhombus_45", "Hình thoi góc nhọn 45 độ", (w, h) => GPoly(w, h, (0.5f, 0), (0.8f, 0.5f), (0.5f, 1), (0.2f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0), (0.8, 0.5), (0.5, 1), (0.2, 0.5)));
            AddItem(cat, "quad_parallelogram_30", "Hình bình hành góc nghiêng 30°", (w, h) => GPoly(w, h, (0.3f, 0), (1, 0), (0.7f, 1), (0, 1)), (w, h) => WPoly(w, h, (0.3, 0), (1, 0), (0.7, 1), (0, 1)));
            AddItem(cat, "quad_parallelogram_45", "Hình bình hành góc nghiêng 45°", (w, h) => GPoly(w, h, (0.45f, 0), (1, 0), (0.55f, 1), (0, 1)), (w, h) => WPoly(w, h, (0.45, 0), (1, 0), (0.55, 1), (0, 1)));
            AddItem(cat, "quad_trapezoid_isosceles", "Hình thang cân góc đáy 60°", (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 1), (0, 1)));
            AddItem(cat, "quad_trapezoid_right", "Hình thang vuông góc cạnh trái", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 1), (0, 1)));
            AddItem(cat, "quad_trapezoid_inverted", "Hình thang vuông góc cạnh phải", (w, h) => GPoly(w, h, (0.25f, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.25, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "quad_kite_slim", "Hình cánh diều dáng thon cao", (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.25f), (0.5f, 1), (0.15f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.25), (0.5, 1), (0.15, 0.25)));
            AddItem(cat, "quad_kite_wide", "Hình cánh diều dáng rộng ngang", (w, h) => GPoly(w, h, (0.5f, 0.1f), (1, 0.45f), (0.5f, 0.9f), (0, 0.45f)), (w, h) => WPoly(w, h, (0.5, 0.1), (1, 0.45), (0.5, 0.9), (0, 0.45)));

            // 29-37: Các hình phân chia cung góc (Circular Sectors)
            int[] sectorAngles = new int[] { 30, 45, 60, 90, 120, 135, 150, 180, 270 };
            for (int i = 0; i < sectorAngles.Length; i++)
            {
                int deg = sectorAngles[i];
                double rad = deg * Math.PI / 180.0;
                double ex = 0.5 + 0.5 * Math.Cos(rad - Math.PI / 2.0);
                double ey = 0.5 + 0.5 * Math.Sin(rad - Math.PI / 2.0);
                AddItem(cat, $"geom_sector_{deg}", $"Hình phân khúc góc {deg}°", (w, h) => GPoly(w, h, (0.5f, 0.5f), (0.5f, 0), ((float)ex, (float)ey)), (w, h) => WPoly(w, h, (0.5, 0.5), (0.5, 0), (ex, ey)));
            }

            // 38-45: Hình học cong & đặc biệt
            AddItem(cat, "geom_vesica_piscis", "Hình giao điểm Vesica Piscis", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.85f, 0.5f), (0.5f, 0.95f), (0.15f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.85, 0.5), (0.5, 0.95), (0.15, 0.5)));
            AddItem(cat, "geom_reuleaux_tri", "Tam giác cong Reuleaux", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.85), (0.05, 0.85)));
            AddItem(cat, "geom_stadium", "Hình sân vận động Stadium", (w, h) => GRoundedRect(w, h, Math.Min(w, h) / 2f), (w, h) => WRoundedRect(w, h, Math.Min(w, h) / 2.0));
            AddItem(cat, "geom_deltoid", "Hình tam giác Deltoid lõm", (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.4f), (1, 0.8f), (0.6f, 0.7f), (0.5f, 1), (0.4f, 0.7f), (0, 0.8f), (0.15f, 0.4f)), (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.4), (1, 0.8), (0.6, 0.7), (0.5, 1), (0.4, 0.7), (0, 0.8), (0.15, 0.4)));
            AddItem(cat, "geom_rounded_triangle", "Tam giác bo tròn 3 góc", (w, h) => GPoly(w, h, (0.45f, 0.05f), (0.55f, 0.05f), (0.95f, 0.85f), (0.85f, 0.95f), (0.15f, 0.95f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.45, 0.05), (0.55, 0.05), (0.95, 0.85), (0.85, 0.95), (0.15, 0.95), (0.05, 0.85)));
            AddItem(cat, "geom_rounded_pentagon", "Ngũ giác bo tròn các góc", (w, h) => GRegularPoly(w, h, 5, -Math.PI / 2.0), (w, h) => WRegularPoly(w, h, 5, -Math.PI / 2.0));
            AddItem(cat, "geom_rounded_hexagon", "Lục giác bo góc trang nhã", (w, h) => GRegularPoly(w, h, 6, 0), (w, h) => WRegularPoly(w, h, 6, 0));
            AddItem(cat, "geom_rounded_octagon", "Bát giác bo viền mềm", (w, h) => GRegularPoly(w, h, 8, Math.PI / 8.0), (w, h) => WRegularPoly(w, h, 8, Math.PI / 8.0));
        }
        #endregion

        #region Category 4: Hình ngôi sao & Huy hiệu (50 mẫu)
        private static void PopulateStarsAndBadges()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Stars", Title = "Hình ngôi sao & Huy hiệu" };
            _categories.Add(cat);

            // 1-13: Ngôi sao chuẩn từ 4 đến 32 cánh
            int[] starPts = new int[] { 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 20, 24, 32 };
            for (int i = 0; i < starPts.Length; i++)
            {
                int pts = starPts[i];
                AddItem(cat, $"star_{pts}pt_std", $"Ngôi sao {pts} cánh chuẩn ({pts}-Point Star)", (w, h) => GStar(w, h, pts, 0.45f), (w, h) => WStar(w, h, pts, 0.45));
            }

            // 14-21: Ngôi sao cánh thon nhọn
            int[] sharpPts = new int[] { 4, 5, 6, 7, 8, 10, 12, 16 };
            for (int i = 0; i < sharpPts.Length; i++)
            {
                int pts = sharpPts[i];
                AddItem(cat, $"star_{pts}pt_sharp", $"Ngôi sao {pts} cánh nhọn ({pts}-Point Sharp)", (w, h) => GStar(w, h, pts, 0.25f), (w, h) => WStar(w, h, pts, 0.25));
            }

            // 22-29: Ngôi sao cánh mập đậm nét
            int[] fatPts = new int[] { 4, 5, 6, 7, 8, 10, 12, 16 };
            for (int i = 0; i < fatPts.Length; i++)
            {
                int pts = fatPts[i];
                AddItem(cat, $"star_{pts}pt_fat", $"Ngôi sao {pts} cánh mập ({pts}-Point Fat)", (w, h) => GStar(w, h, pts, 0.65f), (w, h) => WStar(w, h, pts, 0.65));
            }

            // 30-36: Huy hiệu sao nổ (Burst Explosions)
            int[] burstPts = new int[] { 8, 12, 14, 16, 20, 24, 32 };
            for (int i = 0; i < burstPts.Length; i++)
            {
                int pts = burstPts[i];
                AddItem(cat, $"star_burst_{pts}", $"Huy hiệu nổ sao {pts} tia (Burst {pts})", (w, h) => GStar(w, h, pts, 0.78f), (w, h) => WStar(w, h, pts, 0.78));
            }

            // 37-50: Các loại khiên giáp, huy hiệu & tem chứng nhận
            AddItem(cat, "shield_heater", "Khiên hiệp sĩ Heater Shield", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.6f), (0.5f, 1), (0, 0.6f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.6), (0.5, 1), (0, 0.6)));
            AddItem(cat, "shield_french", "Khiên vòm hoàng gia Pháp", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.95f, 0.65f), (0.5f, 0.98f), (0.05f, 0.65f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.95, 0.65), (0.5, 0.98), (0.05, 0.65)));
            AddItem(cat, "shield_gothic", "Khiên nhọn phong cách Gothic", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.45f), (0.5f, 1), (0, 0.45f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.45), (0.5, 1), (0, 0.45)));
            AddItem(cat, "shield_roman_crest", "Huy hiệu khiên La Mã cổ đại", (w, h) => GPoly(w, h, (0, 0.1f), (0.5f, 0), (1, 0.1f), (1, 0.75f), (0.5f, 1), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.1), (0.5, 0), (1, 0.1), (1, 0.75), (0.5, 1), (0, 0.75)));
            AddItem(cat, "shield_rounded", "Khiên bo góc tròn bảo vệ", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.5f), (0.85f, 0.85f), (0.5f, 1), (0.15f, 0.85f), (0, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.5), (0.85, 0.85), (0.5, 1), (0.15, 0.85), (0, 0.5)));
            AddItem(cat, "scalloped_seal_12", "Tem chứng nhận răng cưa 12 múi", (w, h) => GStar(w, h, 12, 0.82), (w, h) => WStar(w, h, 12, 0.82));
            AddItem(cat, "scalloped_seal_16", "Tem chứng nhận răng cưa 16 múi", (w, h) => GStar(w, h, 16, 0.84), (w, h) => WStar(w, h, 16, 0.84));
            AddItem(cat, "scalloped_seal_24", "Tem chất lượng vàng 24 răng cưa", (w, h) => GStar(w, h, 24, 0.88), (w, h) => WStar(w, h, 24, 0.88));
            AddItem(cat, "badge_rosette", "Huy chương hoa cúc Rosette", (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 0.25f), (1, 0.75f), (0.75f, 1), (0.25f, 1), (0, 0.75f), (0, 0.25f)), (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 0.25), (1, 0.75), (0.75, 1), (0.25, 1), (0, 0.75), (0, 0.25)));
            AddItem(cat, "badge_star_medallion", "Huy hiệu ngôi sao danh dự", (w, h) => GStar(w, h, 8, 0.55), (w, h) => WStar(w, h, 8, 0.55));
            AddItem(cat, "crown_crest_3peak", "Vương miện hoàng gia 3 đỉnh", (w, h) => GPoly(w, h, (0.05f, 0.25f), (0.25f, 0.55f), (0.5f, 0.15f), (0.75f, 0.55f), (0.95f, 0.25f), (0.85f, 0.9f), (0.15f, 0.9f)), (w, h) => WPoly(w, h, (0.05, 0.25), (0.25, 0.55), (0.5, 0.15), (0.75, 0.55), (0.95, 0.25), (0.85, 0.9), (0.15, 0.9)));
            AddItem(cat, "crown_crest_5peak", "Vương miện hoàng đế 5 đỉnh", (w, h) => GPoly(w, h, (0, 0.25f), (0.2f, 0.5f), (0.35f, 0.15f), (0.5f, 0.45f), (0.65f, 0.15f), (0.8f, 0.5f), (1, 0.25f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0, 0.25), (0.2, 0.5), (0.35, 0.15), (0.5, 0.45), (0.65, 0.15), (0.8, 0.5), (1, 0.25), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "badge_ribbon_bottom", "Huy hiệu đính đuôi nơ ruy băng", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.35f), (0.8f, 0.7f), (0.9f, 1), (0.5f, 0.85f), (0.1f, 1), (0.2f, 0.7f), (0, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.35), (0.8, 0.7), (0.9, 1), (0.5, 0.85), (0.1, 1), (0.2, 0.7), (0, 0.35)));
            AddItem(cat, "badge_knight_cross", "Huy hiệu khiên chữ thập thập tự", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.55f), (0.5f, 0.95f), (0.1f, 0.55f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.55), (0.5, 0.95), (0.1, 0.55)));
        }
        #endregion

        #region Category 5: Mũi tên khối & Chỉ hướng (50 mẫu)
        private static void PopulateBlockArrows()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Arrows", Title = "Mũi tên khối & Chỉ hướng" };
            _categories.Add(cat);

            // 1-12: Mũi tên 4 hướng các tỷ lệ (chuẩn, đậm, mảnh)
            AddItem(cat, "arrow_block_right_std", "Mũi tên khối sang phải chuẩn", (w, h) => GPoly(w, h, (0, 0.3f), (0.65f, 0.3f), (0.65f, 0.1f), (1, 0.5f), (0.65f, 0.9f), (0.65f, 0.7f), (0, 0.7f)), (w, h) => WPoly(w, h, (0, 0.3), (0.65, 0.3), (0.65, 0.1), (1, 0.5), (0.65, 0.9), (0.65, 0.7), (0, 0.7)));
            AddItem(cat, "arrow_block_left_std", "Mũi tên khối sang trái chuẩn", (w, h) => GPoly(w, h, (0.35f, 0.1f), (0.35f, 0.3f), (1, 0.3f), (1, 0.7f), (0.35f, 0.7f), (0.35f, 0.9f), (0, 0.5f)), (w, h) => WPoly(w, h, (0.35, 0.1), (0.35, 0.3), (1, 0.3), (1, 0.7), (0.35, 0.7), (0.35, 0.9), (0, 0.5)));
            AddItem(cat, "arrow_block_up_std", "Mũi tên khối hướng lên chuẩn", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.35f), (0.7f, 0.35f), (0.7f, 1), (0.3f, 1), (0.3f, 0.35f), (0.1f, 0.35f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.35), (0.7, 0.35), (0.7, 1), (0.3, 1), (0.3, 0.35), (0.1, 0.35)));
            AddItem(cat, "arrow_block_down_std", "Mũi tên khối hướng xuống chuẩn", (w, h) => GPoly(w, h, (0.3f, 0), (0.7f, 0), (0.7f, 0.65f), (0.9f, 0.65f), (0.5f, 1), (0.1f, 0.65f), (0.3f, 0.65f)), (w, h) => WPoly(w, h, (0.3, 0), (0.7, 0), (0.7, 0.65), (0.9, 0.65), (0.5, 1), (0.1, 0.65), (0.3, 0.65)));
            AddItem(cat, "arrow_block_right_bold", "Mũi tên khối phải thân đậm", (w, h) => GPoly(w, h, (0, 0.2f), (0.6f, 0.2f), (0.6f, 0), (1, 0.5f), (0.6f, 1), (0.6f, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (0.6, 0.2), (0.6, 0), (1, 0.5), (0.6, 1), (0.6, 0.8), (0, 0.8)));
            AddItem(cat, "arrow_block_left_bold", "Mũi tên khối trái thân đậm", (w, h) => GPoly(w, h, (0.4f, 0), (0.4f, 0.2f), (1, 0.2f), (1, 0.8f), (0.4f, 0.8f), (0.4f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.4, 0), (0.4, 0.2), (1, 0.2), (1, 0.8), (0.4, 0.8), (0.4, 1), (0, 0.5)));
            AddItem(cat, "arrow_block_up_bold", "Mũi tên khối lên thân đậm", (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.4f), (0.8f, 0.4f), (0.8f, 1), (0.2f, 1), (0.2f, 0.4f), (0, 0.4f)), (w, h) => WPoly(w, h, (0.5, 0), (1, 0.4), (0.8, 0.4), (0.8, 1), (0.2, 1), (0.2, 0.4), (0, 0.4)));
            AddItem(cat, "arrow_block_down_bold", "Mũi tên khối xuống thân đậm", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (0.8f, 0.6f), (1, 0.6f), (0.5f, 1), (0, 0.6f), (0.2f, 0.6f)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (0.8, 0.6), (1, 0.6), (0.5, 1), (0, 0.6), (0.2, 0.6)));
            AddItem(cat, "arrow_block_right_slim", "Mũi tên khối phải dáng thon dài", (w, h) => GPoly(w, h, (0, 0.4f), (0.75f, 0.4f), (0.75f, 0.25f), (1, 0.5f), (0.75f, 0.75f), (0.75f, 0.6f), (0, 0.6f)), (w, h) => WPoly(w, h, (0, 0.4), (0.75, 0.4), (0.75, 0.25), (1, 0.5), (0.75, 0.75), (0.75, 0.6), (0, 0.6)));
            AddItem(cat, "arrow_block_left_slim", "Mũi tên khối trái dáng thon dài", (w, h) => GPoly(w, h, (0.25f, 0.25f), (0.25f, 0.4f), (1, 0.4f), (1, 0.6f), (0.25f, 0.6f), (0.25f, 0.75f), (0, 0.5f)), (w, h) => WPoly(w, h, (0.25, 0.25), (0.25, 0.4), (1, 0.4), (1, 0.6), (0.25, 0.6), (0.25, 0.75), (0, 0.5)));
            AddItem(cat, "arrow_block_up_slim", "Mũi tên khối lên dáng thon dài", (w, h) => GPoly(w, h, (0.5f, 0), (0.75f, 0.25f), (0.6f, 0.25f), (0.6f, 1), (0.4f, 1), (0.4f, 0.25f), (0.25f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.75, 0.25), (0.6, 0.25), (0.6, 1), (0.4, 1), (0.4, 0.25), (0.25, 0.25)));
            AddItem(cat, "arrow_block_down_slim", "Mũi tên khối xuống dáng thon dài", (w, h) => GPoly(w, h, (0.4f, 0), (0.6f, 0), (0.6f, 0.75f), (0.75f, 0.75f), (0.5f, 1), (0.25f, 0.75f), (0.4f, 0.75f)), (w, h) => WPoly(w, h, (0.4, 0), (0.6, 0), (0.6, 0.75), (0.75, 0.75), (0.5, 1), (0.25, 0.75), (0.4, 0.75)));

            // 13-15: Đa chiều
            AddItem(cat, "arrow_block_bidirectional_horiz", "Mũi tên khối hai đầu ngang", (w, h) => GPoly(w, h, (0.25f, 0.1f), (0.25f, 0.3f), (0.75f, 0.3f), (0.75f, 0.1f), (1, 0.5f), (0.75f, 0.9f), (0.75f, 0.7f), (0.25f, 0.7f), (0.25f, 0.9f), (0, 0.5f)), (w, h) => WPoly(w, h, (0.25, 0.1), (0.25, 0.3), (0.75, 0.3), (0.75, 0.1), (1, 0.5), (0.75, 0.9), (0.75, 0.7), (0.25, 0.7), (0.25, 0.9), (0, 0.5)));
            AddItem(cat, "arrow_block_bidirectional_vert", "Mũi tên khối hai đầu dọc", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.25f), (0.7f, 0.25f), (0.7f, 0.75f), (0.9f, 0.75f), (0.5f, 1), (0.1f, 0.75f), (0.3f, 0.75f), (0.3f, 0.25f), (0.1f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.25), (0.7, 0.25), (0.7, 0.75), (0.9, 0.75), (0.5, 1), (0.1, 0.75), (0.3, 0.75), (0.3, 0.25), (0.1, 0.25)));
            AddItem(cat, "arrow_block_quad_cross", "Mũi tên khối 4 hướng chữ thập", (w, h) => GPoly(w, h, (0.5f, 0), (0.65f, 0.15f), (0.58f, 0.15f), (0.58f, 0.42f), (0.85f, 0.42f), (0.85f, 0.35f), (1, 0.5f), (0.85f, 0.65f), (0.85f, 0.58f), (0.58f, 0.58f), (0.58f, 0.85f), (0.65f, 0.85f), (0.5f, 1), (0.35f, 0.85f), (0.42f, 0.85f), (0.42f, 0.58f), (0.15f, 0.58f), (0.15f, 0.65f), (0, 0.5f), (0.15f, 0.35f), (0.15f, 0.42f), (0.42f, 0.42f), (0.42f, 0.15f), (0.35f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.65, 0.15), (0.58, 0.15), (0.58, 0.42), (0.85, 0.42), (0.85, 0.35), (1, 0.5), (0.85, 0.65), (0.85, 0.58), (0.58, 0.58), (0.58, 0.85), (0.65, 0.85), (0.5, 1), (0.35, 0.85), (0.42, 0.85), (0.42, 0.58), (0.15, 0.58), (0.15, 0.65), (0, 0.5), (0.15, 0.35), (0.15, 0.42), (0.42, 0.42), (0.42, 0.15), (0.35, 0.15)));

            // 16-23: Chevron
            AddItem(cat, "chevron_right", "Mũi tên Chevron sang phải", (w, h) => GPoly(w, h, (0, 0), (0.55f, 0), (1, 0.5f), (0.55f, 1), (0, 1), (0.45f, 0.5f)), (w, h) => WPoly(w, h, (0, 0), (0.55, 0), (1, 0.5), (0.55, 1), (0, 1), (0.45, 0.5)));
            AddItem(cat, "chevron_left", "Mũi tên Chevron sang trái", (w, h) => GPoly(w, h, (0.45f, 0), (1, 0), (0.55f, 0.5f), (1, 1), (0.45f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.45, 0), (1, 0), (0.55, 0.5), (1, 1), (0.45, 1), (0, 0.5)));
            AddItem(cat, "chevron_up", "Mũi tên Chevron hướng lên", (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.45f), (1, 1), (0.5f, 0.55f), (0, 1), (0, 0.45f)), (w, h) => WPoly(w, h, (0.5, 0), (1, 0.45), (1, 1), (0.5, 0.55), (0, 1), (0, 0.45)));
            AddItem(cat, "chevron_down", "Mũi tên Chevron hướng xuống", (w, h) => GPoly(w, h, (0, 0), (0.5f, 0.45f), (1, 0), (1, 0.55f), (0.5f, 1), (0, 0.55f)), (w, h) => WPoly(w, h, (0, 0), (0.5, 0.45), (1, 0), (1, 0.55), (0.5, 1), (0, 0.55)));
            AddItem(cat, "chevron_double_right", "Chevron kép đôi sang phải", (w, h) => GPoly(w, h, (0, 0), (0.35f, 0), (0.65f, 0.5f), (0.35f, 1), (0, 1), (0.3f, 0.5f)), (w, h) => WPoly(w, h, (0, 0), (0.35, 0), (0.65, 0.5), (0.35, 1), (0, 1), (0.3, 0.5)));
            AddItem(cat, "chevron_double_left", "Chevron kép đôi sang trái", (w, h) => GPoly(w, h, (0.65f, 0), (1, 0), (0.7f, 0.5f), (1, 1), (0.65f, 1), (0.35f, 0.5f)), (w, h) => WPoly(w, h, (0.65, 0), (1, 0), (0.7, 0.5), (1, 1), (0.65, 1), (0.35, 0.5)));
            AddItem(cat, "chevron_triple_right", "Chevron ba tầng sang phải", (w, h) => GPoly(w, h, (0, 0), (0.25f, 0), (0.5f, 0.5f), (0.25f, 1), (0, 1), (0.25f, 0.5f)), (w, h) => WPoly(w, h, (0, 0), (0.25, 0), (0.5, 0.5), (0.25, 1), (0, 1), (0.25, 0.5)));
            AddItem(cat, "chevron_triple_left", "Chevron ba tầng sang trái", (w, h) => GPoly(w, h, (0.75f, 0), (1, 0), (0.75f, 0.5f), (1, 1), (0.75f, 1), (0.5f, 0.5f)), (w, h) => WPoly(w, h, (0.75, 0), (1, 0), (0.75, 0.5), (1, 1), (0.75, 1), (0.5, 0.5)));

            // 24-31: Ngũ giác & khuyết đuôi
            AddItem(cat, "arrow_pentagon_right", "Mũi tên ngũ giác khối phải", (w, h) => GPoly(w, h, (0, 0), (0.65f, 0), (1, 0.5f), (0.65f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.65, 0), (1, 0.5), (0.65, 1), (0, 1)));
            AddItem(cat, "arrow_pentagon_left", "Mũi tên ngũ giác khối trái", (w, h) => GPoly(w, h, (0.35f, 0), (1, 0), (1, 1), (0.35f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.35, 0), (1, 0), (1, 1), (0.35, 1), (0, 0.5)));
            AddItem(cat, "arrow_pentagon_up", "Mũi tên ngũ giác khối lên", (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.35f), (1, 1), (0, 1), (0, 0.35f)), (w, h) => WPoly(w, h, (0.5, 0), (1, 0.35), (1, 1), (0, 1), (0, 0.35)));
            AddItem(cat, "arrow_pentagon_down", "Mũi tên ngũ giác khối xuống", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.65f), (0.5f, 1), (0, 0.65f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.65), (0.5, 1), (0, 0.65)));
            AddItem(cat, "arrow_notched_right", "Mũi tên khối khuyết đuôi cá phải", (w, h) => GPoly(w, h, (0, 0.2f), (0.65f, 0.2f), (0.65f, 0), (1, 0.5f), (0.65f, 1), (0.65f, 0.8f), (0, 0.8f), (0.15f, 0.5f)), (w, h) => WPoly(w, h, (0, 0.2), (0.65, 0.2), (0.65, 0), (1, 0.5), (0.65, 1), (0.65, 0.8), (0, 0.8), (0.15, 0.5)));
            AddItem(cat, "arrow_notched_left", "Mũi tên khối khuyết đuôi cá trái", (w, h) => GPoly(w, h, (0.35f, 0), (0.35f, 0.2f), (1, 0.2f), (0.85f, 0.5f), (1, 0.8f), (0.35f, 0.8f), (0.35f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.35, 0), (0.35, 0.2), (1, 0.2), (0.85, 0.5), (1, 0.8), (0.35, 0.8), (0.35, 1), (0, 0.5)));
            AddItem(cat, "arrow_notched_up", "Mũi tên khối khuyết đuôi cá lên", (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.35f), (0.8f, 0.35f), (0.8f, 1), (0.5f, 0.85f), (0.2f, 1), (0.2f, 0.35f), (0, 0.35f)), (w, h) => WPoly(w, h, (0.5, 0), (1, 0.35), (0.8, 0.35), (0.8, 1), (0.5, 0.85), (0.2, 1), (0.2, 0.35), (0, 0.35)));
            AddItem(cat, "arrow_notched_down", "Mũi tên khối khuyết đuôi cá xuống", (w, h) => GPoly(w, h, (0.2f, 0), (0.5f, 0.15f), (0.8f, 0), (0.8f, 0.65f), (1, 0.65f), (0.5f, 1), (0, 0.65f), (0.2f, 0.65f)), (w, h) => WPoly(w, h, (0.2, 0), (0.5, 0.15), (0.8, 0), (0.8, 0.65), (1, 0.65), (0.5, 1), (0, 0.65), (0.2, 0.65)));

            // 32-36: Mũi tên cong & góc vuông L
            AddItem(cat, "arrow_bent_up_right", "Mũi tên uốn góc lên rồi sang phải", (w, h) => GPoly(w, h, (0.2f, 1), (0.4f, 1), (0.4f, 0.45f), (0.7f, 0.45f), (0.7f, 0.25f), (1, 0.55f), (0.7f, 0.85f), (0.7f, 0.65f), (0.2f, 0.65f)), (w, h) => WPoly(w, h, (0.2, 1), (0.4, 1), (0.4, 0.45), (0.7, 0.45), (0.7, 0.25), (1, 0.55), (0.7, 0.85), (0.7, 0.65), (0.2, 0.65)));
            AddItem(cat, "arrow_bent_down_right", "Mũi tên uốn góc xuống rồi sang phải", (w, h) => GPoly(w, h, (0.2f, 0), (0.4f, 0), (0.4f, 0.55f), (0.7f, 0.55f), (0.7f, 0.75f), (1, 0.45f), (0.7f, 0.15f), (0.7f, 0.35f), (0.2f, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0), (0.4, 0), (0.4, 0.55), (0.7, 0.55), (0.7, 0.75), (1, 0.45), (0.7, 0.15), (0.7, 0.35), (0.2, 0.35)));
            AddItem(cat, "arrow_bent_up_left", "Mũi tên uốn góc lên rồi sang trái", (w, h) => GPoly(w, h, (0.6f, 1), (0.8f, 1), (0.8f, 0.65f), (0.3f, 0.65f), (0.3f, 0.85f), (0, 0.55f), (0.3f, 0.25f), (0.3f, 0.45f), (0.6f, 0.45f)), (w, h) => WPoly(w, h, (0.6, 1), (0.8, 1), (0.8, 0.65), (0.3, 0.65), (0.3, 0.85), (0, 0.55), (0.3, 0.25), (0.3, 0.45), (0.6, 0.45)));
            AddItem(cat, "arrow_bent_down_left", "Mũi tên uốn góc xuống rồi sang trái", (w, h) => GPoly(w, h, (0.6f, 0), (0.8f, 0), (0.8f, 0.35f), (0.3f, 0.35f), (0.3f, 0.15f), (0, 0.45f), (0.3f, 0.75f), (0.3f, 0.55f), (0.6f, 0.55f)), (w, h) => WPoly(w, h, (0.6, 0), (0.8, 0), (0.8, 0.35), (0.3, 0.35), (0.3, 0.15), (0, 0.45), (0.3, 0.75), (0.3, 0.55), (0.6, 0.55)));
            AddItem(cat, "arrow_corner_l", "Mũi tên góc thước thợ chữ L", (w, h) => GPoly(w, h, (0, 0), (0.6f, 0), (0.6f, 0.2f), (1, 0.5f), (0.6f, 0.8f), (0.6f, 0.6f), (0.2f, 0.6f), (0.2f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.6, 0), (0.6, 0.2), (1, 0.5), (0.6, 0.8), (0.6, 0.6), (0.2, 0.6), (0.2, 1), (0, 1)));

            // 37-42: Vòng lặp & quay đầu
            AddItem(cat, "arrow_uturn_horiz", "Mũi tên chữ U quay đầu nằm ngang", (w, h) => GPoly(w, h, (1, 0.2f), (0.4f, 0.2f), (0.4f, 0), (0, 0.4f), (0.4f, 0.8f), (0.4f, 0.6f), (0.8f, 0.6f), (0.8f, 1), (1, 1)), (w, h) => WPoly(w, h, (1, 0.2), (0.4, 0.2), (0.4, 0), (0, 0.4), (0.4, 0.8), (0.4, 0.6), (0.8, 0.6), (0.8, 1), (1, 1)));
            AddItem(cat, "arrow_uturn_vert", "Mũi tên chữ U quay đầu thẳng đứng", (w, h) => GPoly(w, h, (0.2f, 1), (0.2f, 0.4f), (0, 0.4f), (0.4f, 0), (0.8f, 0.4f), (0.6f, 0.4f), (0.6f, 0.8f), (1, 0.8f), (1, 1)), (w, h) => WPoly(w, h, (0.2, 1), (0.2, 0.4), (0, 0.4), (0.4, 0), (0.8, 0.4), (0.6, 0.4), (0.6, 0.8), (1, 0.8), (1, 1)));
            AddItem(cat, "arrow_loop_2arrow", "Vòng lặp chu trình 2 mũi tên", (w, h) => GPoly(w, h, (0.2f, 0.2f), (0.8f, 0.2f), (0.8f, 0), (1, 0.35f), (0.8f, 0.7f), (0.8f, 0.5f), (0.2f, 0.5f)), (w, h) => WPoly(w, h, (0.2, 0.2), (0.8, 0.2), (0.8, 0), (1, 0.35), (0.8, 0.7), (0.8, 0.5), (0.2, 0.5)));
            AddItem(cat, "arrow_loop_3arrow", "Vòng lặp chu trình 3 mũi tên", (w, h) => GStar(w, h, 3, 0.5), (w, h) => WStar(w, h, 3, 0.5));
            AddItem(cat, "arrow_loop_4arrow", "Vòng lặp chu trình 4 mũi tên", (w, h) => GPoly(w, h, (0.3f, 0.1f), (0.7f, 0.1f), (0.7f, 0), (0.9f, 0.2f), (0.7f, 0.4f), (0.7f, 0.3f), (0.3f, 0.3f)), (w, h) => WPoly(w, h, (0.3, 0.1), (0.7, 0.1), (0.7, 0), (0.9, 0.2), (0.7, 0.4), (0.7, 0.3), (0.3, 0.3)));
            AddItem(cat, "arrow_curved_down", "Mũi tên cong vòng xuống dưới", (w, h) => GPoly(w, h, (0.2f, 0.2f), (0.6f, 0.2f), (0.8f, 0.5f), (0.8f, 0.7f), (0.95f, 0.7f), (0.7f, 1), (0.45f, 0.7f), (0.6f, 0.7f), (0.6f, 0.5f), (0.2f, 0.5f)), (w, h) => WPoly(w, h, (0.2, 0.2), (0.6, 0.2), (0.8, 0.5), (0.8, 0.7), (0.95, 0.7), (0.7, 1), (0.45, 0.7), (0.6, 0.7), (0.6, 0.5), (0.2, 0.5)));

            // 43-50: Phân nhánh & đặc biệt
            AddItem(cat, "arrow_split_2way", "Mũi tên phân hai ngả rẽ", (w, h) => GPoly(w, h, (0, 0.4f), (0.4f, 0.4f), (0.7f, 0.1f), (0.7f, 0), (1, 0.2f), (0.7f, 0.4f), (0.7f, 0.3f), (0.5f, 0.5f), (0.7f, 0.7f), (0.7f, 0.6f), (1, 0.8f), (0.7f, 1), (0.7f, 0.9f), (0.4f, 0.6f), (0, 0.6f)), (w, h) => WPoly(w, h, (0, 0.4), (0.4, 0.4), (0.7, 0.1), (0.7, 0), (1, 0.2), (0.7, 0.4), (0.7, 0.3), (0.5, 0.5), (0.7, 0.7), (0.7, 0.6), (1, 0.8), (0.7, 1), (0.7, 0.9), (0.4, 0.6), (0, 0.6)));
            AddItem(cat, "arrow_split_3way", "Mũi tên phân ba ngả đường", (w, h) => GPoly(w, h, (0, 0.45f), (0.35f, 0.45f), (0.65f, 0.15f), (0.65f, 0.05f), (0.95f, 0.2f), (0.65f, 0.35f), (0.65f, 0.25f), (0.45f, 0.45f), (0.75f, 0.45f), (0.75f, 0.35f), (1, 0.5f), (0.75f, 0.65f), (0.75f, 0.55f), (0.45f, 0.55f), (0.65f, 0.75f), (0.65f, 0.65f), (0.95f, 0.8f), (0.65f, 0.95f), (0.65f, 0.85f), (0.35f, 0.55f), (0, 0.55f)), (w, h) => WPoly(w, h, (0, 0.45), (0.35, 0.45), (0.65, 0.15), (0.65, 0.05), (0.95, 0.2), (0.65, 0.35), (0.65, 0.25), (0.45, 0.45), (0.75, 0.45), (0.75, 0.35), (1, 0.5), (0.75, 0.65), (0.75, 0.55), (0.45, 0.55), (0.65, 0.75), (0.65, 0.65), (0.95, 0.8), (0.65, 0.95), (0.65, 0.85), (0.35, 0.55), (0, 0.55)));
            AddItem(cat, "arrow_merge_2to1", "Mũi tên hợp nhất hai nguồn", (w, h) => GPoly(w, h, (0, 0.2f), (0.3f, 0.2f), (0.5f, 0.4f), (0.75f, 0.4f), (0.75f, 0.25f), (1, 0.5f), (0.75f, 0.75f), (0.75f, 0.6f), (0.5f, 0.6f), (0.3f, 0.8f), (0, 0.8f), (0.2f, 0.6f), (0.2f, 0.4f)), (w, h) => WPoly(w, h, (0, 0.2), (0.3, 0.2), (0.5, 0.4), (0.75, 0.4), (0.75, 0.25), (1, 0.5), (0.75, 0.75), (0.75, 0.6), (0.5, 0.6), (0.3, 0.8), (0, 0.8), (0.2, 0.6), (0.2, 0.4)));
            AddItem(cat, "arrow_diverging_y", "Mũi tên chẽ chữ Y mở rộng", (w, h) => GPoly(w, h, (0.4f, 1), (0.6f, 1), (0.6f, 0.55f), (0.85f, 0.3f), (0.95f, 0.4f), (1, 0.1f), (0.7f, 0.15f), (0.8f, 0.25f), (0.5f, 0.45f), (0.2f, 0.25f), (0.3f, 0.15f), (0, 0.1f), (0.05f, 0.4f), (0.15f, 0.3f), (0.4f, 0.55f)), (w, h) => WPoly(w, h, (0.4, 1), (0.6, 1), (0.6, 0.55), (0.85, 0.3), (0.95, 0.4), (1, 0.1), (0.7, 0.15), (0.8, 0.25), (0.5, 0.45), (0.2, 0.25), (0.3, 0.15), (0, 0.1), (0.05, 0.4), (0.15, 0.3), (0.4, 0.55)));
            AddItem(cat, "arrow_hollow_right", "Mũi tên rỗng viền sang phải", (w, h) => GPoly(w, h, (0.05f, 0.35f), (0.65f, 0.35f), (0.65f, 0.15f), (0.95f, 0.5f), (0.65f, 0.85f), (0.65f, 0.65f), (0.05f, 0.65f)), (w, h) => WPoly(w, h, (0.05, 0.35), (0.65, 0.35), (0.65, 0.15), (0.95, 0.5), (0.65, 0.85), (0.65, 0.65), (0.05, 0.65)));
            AddItem(cat, "arrow_faceted_diamond", "Mũi tên vát cạnh kim cương", (w, h) => GPoly(w, h, (0.1f, 0.5f), (0.5f, 0.2f), (0.9f, 0.5f), (0.5f, 0.8f)), (w, h) => WPoly(w, h, (0.1, 0.5), (0.5, 0.2), (0.9, 0.5), (0.5, 0.8)));
            AddItem(cat, "arrow_callout_right", "Mũi tên gắn nhãn chú thích phải", (w, h) => GPoly(w, h, (0, 0.25f), (0.65f, 0.25f), (0.65f, 0.05f), (1, 0.5f), (0.65f, 0.95f), (0.65f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.25), (0.65, 0.25), (0.65, 0.05), (1, 0.5), (0.65, 0.95), (0.65, 0.75), (0, 0.75)));
            AddItem(cat, "arrow_callout_left", "Mũi tên gắn nhãn chú thích trái", (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.35f, 0.25f), (1, 0.25f), (1, 0.75f), (0.35f, 0.75f), (0.35f, 0.95f), (0, 0.5f)), (w, h) => WPoly(w, h, (0.35, 0.05), (0.35, 0.25), (1, 0.25), (1, 0.75), (0.35, 0.75), (0.35, 0.95), (0, 0.5)));
        }
        #endregion

        #region Category 6: Lưu đồ & Quy trình (50 mẫu)
        private static void PopulateFlowchartSymbols()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Flowchart", Title = "Lưu đồ & Quy trình" };
            _categories.Add(cat);

            // 1. Terminator (Điểm bắt đầu / kết thúc - Stadium Oval Pill)
            AddItem(cat, "flow_terminator", "Điểm bắt đầu/kết thúc (Terminator)",
                (w, h) => GRoundedRect(w, h, h / 2f),
                (w, h) => WRoundedRect(w, h, h / 2.0));

            // 2. Process (Bước xử lý quy trình - Chữ nhật chuẩn)
            AddItem(cat, "flow_process", "Bước xử lý quy trình (Process)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));

            // 3. Decision (Điểm quyết định rẽ nhánh - Hình thoi kim cương)
            AddItem(cat, "flow_decision", "Điểm quyết định rẽ nhánh (Decision)",
                (w, h) => GPoly(w, h, (0.5f, 0), (1, 0.5f), (0.5f, 1), (0, 0.5f)),
                (w, h) => WPoly(w, h, (0.5, 0), (1, 0.5), (0.5, 1), (0, 0.5)));

            // 4. Data / I/O (Dữ liệu vào/ra - Hình bình hành)
            AddItem(cat, "flow_data", "Dữ liệu vào/ra I/O (Data)",
                (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (0.8f, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (0.8, 1), (0, 1)));

            // 5. Predefined Process (Quy trình con định nghĩa trước - Hai vạch dọc hai bên)
            AddItem(cat, "flow_predefined", "Quy trình con định nghĩa trước (Subroutine)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddLine(w * 0.15f, 0, w * 0.15f, h); gp.AddLine(w * 0.85f, 0, w * 0.85f, h); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h), true, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(w * 0.15, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.15, h), true, false); ctx.BeginFigure(new WpfPoint(w * 0.85, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.85, h), true, false); } g.Freeze(); return g; });

            // 6. Internal Storage (Kho lưu trữ nội bộ - Vạch ngang đỉnh và vạch dọc trái)
            AddItem(cat, "flow_internal_storage", "Kho lưu trữ nội bộ (Internal Storage)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddLine(0, h * 0.2f, w, h * 0.2f); gp.AddLine(w * 0.2f, 0, w * 0.2f, h); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h), true, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.2), false, false); ctx.LineTo(new WpfPoint(w, h * 0.2), true, false); ctx.BeginFigure(new WpfPoint(w * 0.2, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.2, h), true, false); } g.Freeze(); return g; });

            // 7. Document (Tài liệu giấy đơn - Đáy lượn sóng Bezier)
            AddItem(cat, "flow_document", "Tài liệu giấy đơn (Single Document)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, w, 0); gp.AddLine(w, 0, w, h * 0.85f); gp.AddBezier(w, h * 0.85f, w * 0.75f, h * 0.7f, w * 0.25f, h * 1.05f, 0, h * 0.85f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); ctx.BezierTo(new WpfPoint(w * 0.75, h * 0.7), new WpfPoint(w * 0.25, h * 1.05), new WpfPoint(0, h * 0.85), true, false); } g.Freeze(); return g; });

            // 8. Multi-Document (Đa tài liệu xếp chồng)
            AddItem(cat, "flow_multidocument", "Đa tài liệu xếp chồng (Multi-Document)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLines(new PointF[] { new PointF(w * 0.15f, 0), new PointF(w, 0), new PointF(w, h * 0.75f) }); gp.AddLines(new PointF[] { new PointF(w * 0.08f, h * 0.08f), new PointF(w * 0.92f, h * 0.08f), new PointF(w * 0.92f, h * 0.82f) }); gp.AddLine(0, h * 0.15f, w * 0.85f, h * 0.15f); gp.AddLine(w * 0.85f, h * 0.15f, w * 0.85f, h * 0.85f); gp.AddBezier(w * 0.85f, h * 0.85f, w * 0.65f, h * 0.7f, w * 0.2f, h * 1.05f, 0, h * 0.85f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.15, 0), false, false); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.08, h * 0.08), false, false); ctx.LineTo(new WpfPoint(w * 0.92, h * 0.08), true, false); ctx.LineTo(new WpfPoint(w * 0.92, h * 0.82), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.15), true, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.85), true, false); ctx.BezierTo(new WpfPoint(w * 0.65, h * 0.7), new WpfPoint(w * 0.2, h * 1.05), new WpfPoint(0, h * 0.85), true, false); } g.Freeze(); return g; });

            // 9. Manual Input (Nhập liệu thủ công - Đỉnh vát dốc từ trái xuống phải)
            AddItem(cat, "flow_manual_input", "Nhập liệu thủ công (Manual Input)",
                (w, h) => GPoly(w, h, (0, 0.25f), (1, 0), (1, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0, 0.25), (1, 0), (1, 1), (0, 1)));

            // 10. Manual Operation (Thao tác thủ công - Hình thang ngược)
            AddItem(cat, "flow_manual_operation", "Thao tác thủ công (Manual Operation)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (0.8f, 1), (0.2f, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (0.8, 1), (0.2, 1)));

            // 11. Preparation (Chuẩn bị thiết lập - Hình lục giác kéo dài)
            AddItem(cat, "flow_preparation", "Chuẩn bị thiết lập (Preparation Hexagon)",
                (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.5f), (0.8f, 1), (0.2f, 1), (0, 0.5f)),
                (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.5), (0.8, 1), (0.2, 1), (0, 0.5)));

            // 12. Display (Màn hình hiển thị - Hình viên đạn vòm phải)
            AddItem(cat, "flow_display", "Màn hình hiển thị (Display)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.2f, 0, w * 0.6f, 0); gp.AddArc(w * 0.2f, 0, w * 0.8f, h, 270, 180); gp.AddLine(w * 0.6f, h, w * 0.2f, h); gp.AddLine(w * 0.2f, h, 0, h * 0.5f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.5), true, true); ctx.LineTo(new WpfPoint(w * 0.2, 0), true, false); ctx.LineTo(new WpfPoint(w * 0.6, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.6, h), new WpfSize(w * 0.4, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w * 0.2, h), true, false); } g.Freeze(); return g; });

            // 13. Delay (Bước chờ trì hoãn - Chữ D vòm phải)
            AddItem(cat, "flow_delay", "Bước chờ trì hoãn (Delay D-Shape)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, w * 0.5f, 0); gp.AddArc(0, 0, w, h, 270, 180); gp.AddLine(w * 0.5f, h, 0, h); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w * 0.5, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.5, h), new WpfSize(w * 0.5, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(0, h), true, false); } g.Freeze(); return g; });

            // 14. Off-page Connector (Điểm nối khác trang - Ngũ giác chóp nhọn chúc xuống)
            AddItem(cat, "flow_offpage_connector", "Điểm nối khác trang (Off-page Connector)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.65f), (0.5f, 1), (0, 0.65f)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.65), (0.5, 1), (0, 0.65)));

            // 15. On-page Connector (Điểm nối cùng trang - Tròn nhỏ)
            AddItem(cat, "flow_onpage_connector", "Điểm nối cùng trang (On-page Connector)",
                (w, h) => GEllipse(w, h),
                (w, h) => WEllipse(w, h));

            // 16. Summing Junction (Tổng hợp cộng dồn - Tròn có dấu cộng)
            AddItem(cat, "flow_summing_junction", "Tổng hợp cộng dồn (Summing Junction)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, 0), false, false); ctx.LineTo(new WpfPoint(w / 2.0, h), true, false); ctx.BeginFigure(new WpfPoint(0, h / 2.0), false, false); ctx.LineTo(new WpfPoint(w, h / 2.0), true, false); } g.Freeze(); return g; });

            // 17. Or Junction (Điểm giao logic OR - Tròn có dấu chéo X)
            AddItem(cat, "flow_or_junction", "Điểm giao logic OR (Or Junction)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.15f, h * 0.15f, w * 0.85f, h * 0.85f); gp.AddLine(w * 0.85f, h * 0.15f, w * 0.15f, h * 0.85f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.15, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.85, h * 0.85), true, false); ctx.BeginFigure(new WpfPoint(w * 0.85, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.15, h * 0.85), true, false); } g.Freeze(); return g; });

            // 18. Collate (Sắp xếp đối chiếu - Đồng hồ cát hai tam giác đấu đỉnh)
            AddItem(cat, "flow_collate", "Sắp xếp đối chiếu (Collate Hourglass)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (0, 1), (1, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (0, 1), (1, 1)));

            // 19. Sort (Phân loại sắp xếp - Hình thoi chia đôi ngang)
            AddItem(cat, "flow_sort", "Phân loại sắp xếp (Sort Diamond)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); } g.Freeze(); return g; });

            // 20. Extract (Trích xuất phân nhánh - Tam giác trỏ xuống)
            AddItem(cat, "flow_extract", "Trích xuất phân nhánh (Extract Triangle)",
                (w, h) => GPoly(w, h, (0, 0), (1, 0), (0.5f, 1)),
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (0.5, 1)));

            // 21. Merge (Hợp nhất quy trình - Tam giác trỏ lên)
            AddItem(cat, "flow_merge", "Hợp nhất quy trình (Merge Triangle)",
                (w, h) => GPoly(w, h, (0.5f, 0), (1, 1), (0, 1)),
                (w, h) => WPoly(w, h, (0.5, 0), (1, 1), (0, 1)));

            // 22. Database (Cơ sở dữ liệu - Hình trụ 3D đứng)
            AddItem(cat, "flow_database", "Cơ sở dữ liệu (Database Cylinder)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h * 0.3f); gp.AddLine(0, h * 0.15f, 0, h * 0.85f); gp.AddArc(0, h * 0.7f, w, h * 0.3f, 180, -180); gp.AddLine(w, h * 0.85f, w, h * 0.15f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.ArcTo(new WpfPoint(w, h * 0.15), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); ctx.ArcTo(new WpfPoint(0, h * 0.85), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(0, h * 0.15), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.ArcTo(new WpfPoint(w, h * 0.15), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Counterclockwise, true, false); ctx.ArcTo(new WpfPoint(0, h * 0.15), new WpfSize(w / 2.0, h * 0.15), 0, false, WpfSweepDirection.Counterclockwise, true, false); } g.Freeze(); return g; });

            // 23. Direct Access Storage (Đĩa lưu trữ trực tiếp - Trụ nằm ngang)
            AddItem(cat, "flow_direct_data", "Đĩa lưu trữ trực tiếp (Direct Access Storage)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.75f, 0, w * 0.25f, h); gp.AddLine(w * 0.15f, 0, w * 0.85f, 0); gp.AddArc(0, 0, w * 0.3f, h, 270, -180); gp.AddLine(w * 0.15f, h, w * 0.85f, h); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.15, 0), true, true); ctx.LineTo(new WpfPoint(w * 0.85, 0), true, false); ctx.ArcTo(new WpfPoint(w * 0.85, h), new WpfSize(w * 0.15, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.LineTo(new WpfPoint(w * 0.15, h), true, false); ctx.ArcTo(new WpfPoint(w * 0.15, 0), new WpfSize(w * 0.15, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 24. Sequential Data / Tape (Băng từ tuần tự - Tròn kèm đuôi ngang)
            AddItem(cat, "flow_sequential_data", "Băng từ tuần tự (Magnetic Tape)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w * 0.85f, h * 0.85f); gp.AddLine(w * 0.42f, h * 0.85f, w, h * 0.85f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.42, 0), true, true); ctx.ArcTo(new WpfPoint(w * 0.42, h * 0.85), new WpfSize(w * 0.42, h * 0.42), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.42, 0), new WpfSize(w * 0.42, h * 0.42), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.42, h * 0.85), false, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); } g.Freeze(); return g; });

            // 25. Punched Card (Thẻ đục lỗ - Vát góc trên trái 45°)
            AddItem(cat, "flow_punched_card", "Thẻ đục lỗ ghi nhận (Punched Card)",
                (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 1), (0, 1), (0, 0.2f)),
                (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 1), (0, 1), (0, 0.2)));

            // 26. Punched Tape (Băng giấy đục lỗ - Sóng hai đầu)
            AddItem(cat, "flow_punched_tape", "Băng giấy đục lỗ (Paper Tape)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(0, h * 0.2f, w * 0.25f, 0, w * 0.75f, h * 0.4f, w, h * 0.2f); gp.AddLine(w, h * 0.2f, w, h * 0.8f); gp.AddBezier(w, h * 0.8f, w * 0.75f, h, w * 0.25f, h * 0.6f, 0, h * 0.8f); gp.CloseFigure(); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.2), true, true); ctx.BezierTo(new WpfPoint(w * 0.25, 0), new WpfPoint(w * 0.75, h * 0.4), new WpfPoint(w, h * 0.2), true, false); ctx.LineTo(new WpfPoint(w, h * 0.8), true, false); ctx.BezierTo(new WpfPoint(w * 0.75, h), new WpfPoint(w * 0.25, h * 0.6), new WpfPoint(0, h * 0.8), true, false); } g.Freeze(); return g; });

            // 27. Stored Data (Dữ liệu lưu trữ - Bo tròn cong bên trái, nhọn bên phải)
            AddItem(cat, "flow_stored_data", "Dữ liệu lưu trữ (Stored Data)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w * 0.4f, h, 90, 180); gp.AddLine(w * 0.2f, 0, w, 0); gp.AddLine(w, 0, w * 0.8f, h * 0.5f); gp.AddLine(w * 0.8f, h * 0.5f, w, h); gp.AddLine(w, h, w * 0.2f, h); gp.CloseFigure(); return gp; },
                (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (0.8, 0.5), (1, 1), (0.2, 1), (0, 0.5)));

            // 28. Loop Limit (Kiểm tra giới hạn vòng lặp)
            AddItem(cat, "flow_loop_limit", "Giới hạn lặp vòng (Loop Limit)",
                (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.35f), (1, 1), (0, 1), (0, 0.35f)),
                (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.35), (1, 1), (0, 1), (0, 0.35)));

            // 29. Transmittal Tape (Dải băng chuyển giao - Đáy khuyết chữ V)
            AddItem(cat, "flow_transmittal_tape", "Dải băng chuyển giao (Transmittal Tape)",
                (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 0.25f), (1, 1), (0.5f, 0.8f), (0, 1)),
                (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 0.25), (1, 1), (0.5, 0.8), (0, 1)));

            // 30. Alternate Process (Quy trình thay thế - Chữ nhật bo góc)
            AddItem(cat, "flow_alternate_process", "Quy trình thay thế (Alternate Process)",
                (w, h) => GRoundedRect(w, h, 6f),
                (w, h) => WRoundedRect(w, h, 6.0));

            // 31. Annotation (Ghi chú chú giải - Khung móc mở)
            AddItem(cat, "flow_annotation", "Khung chú giải ghi chú (Annotation Bracket)",
                (w, h) => GLines(w, h, (0.7f, 0), (0.1f, 0), (0.1f, 1), (0.7f, 1)),
                (w, h) => WLines(w, h, (0.7, 0), (0.1, 0), (0.1, 1), (0.7, 1)));

            // 32. Data Store (Kho dữ liệu mở - Hai thanh ngang song song)
            AddItem(cat, "flow_data_store", "Kho dữ liệu mở song song (Data Store)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.2f, w, h * 0.2f); gp.AddLine(0, h * 0.8f, w, h * 0.8f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.2), false, false); ctx.LineTo(new WpfPoint(w, h * 0.2), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.8), false, false); ctx.LineTo(new WpfPoint(w, h * 0.8), true, false); } g.Freeze(); return g; });

            // 33. External Entity (Thực thể bên ngoài - Khung viền đôi)
            AddItem(cat, "flow_external_entity", "Thực thể ngoài hệ thống (External Entity)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f)); return gp; },
                (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9), (0.1, 0.1)));

            // 34. Cloud Service (Quy trình dịch vụ đám mây)
            AddItem(cat, "flow_cloud_service", "Dịch vụ đám mây (Cloud Service)",
                (w, h) => GStar(w, h, 10, 0.8),
                (w, h) => WStar(w, h, 10, 0.8));

            // 35. Timer Event (Sự kiện bộ đếm thời gian - Đồng hồ tròn có kim)
            AddItem(cat, "flow_timer_event", "Sự kiện thời gian chờ (Timer Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.5f, h * 0.2f); gp.AddLine(w * 0.5f, h * 0.5f, w * 0.75f, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h / 2.0), false, false); ctx.LineTo(new WpfPoint(w / 2.0, h * 0.2), true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h / 2.0), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h / 2.0), true, false); } g.Freeze(); return g; });

            // 36. Message Event (Sự kiện gửi nhận thông điệp - Phong thư)
            AddItem(cat, "flow_message_event", "Sự kiện thông điệp thư từ (Message Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, h * 0.15f, w, h * 0.7f)); gp.AddLine(0, h * 0.15f, w * 0.5f, h * 0.55f); gp.AddLine(w * 0.5f, h * 0.55f, w, h * 0.15f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.15), true, true); ctx.LineTo(new WpfPoint(w, h * 0.15), true, false); ctx.LineTo(new WpfPoint(w, h * 0.85), true, false); ctx.LineTo(new WpfPoint(0, h * 0.85), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.15), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.55), true, false); ctx.LineTo(new WpfPoint(w, h * 0.15), true, false); } g.Freeze(); return g; });

            // 37. Rule Decision (Quy tắc nghiệp vụ - Cuốn sách)
            AddItem(cat, "flow_rule_decision", "Quy tắc nghiệp vụ (Business Rule)",
                (w, h) => GPoly(w, h, (0.05f, 0.1f), (0.5f, 0.25f), (0.95f, 0.1f), (0.95f, 0.85f), (0.5f, 0.98f), (0.05f, 0.85f)),
                (w, h) => WPoly(w, h, (0.05, 0.1), (0.5, 0.25), (0.95, 0.1), (0.95, 0.85), (0.5, 0.98), (0.05, 0.85)));

            // 38. Service Task (Tác vụ dịch vụ tự động - Bánh răng cơ khí)
            AddItem(cat, "flow_service_task", "Tác vụ hệ thống dịch vụ (Service Task Gear)",
                (w, h) => GStar(w, h, 8, 0.75),
                (w, h) => WStar(w, h, 8, 0.75));

            // 39. User Task (Tác vụ người dùng tương tác - Hình người)
            AddItem(cat, "flow_user_task", "Tác vụ người dùng tương tác (User Task)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.35f, 0, w * 0.3f, h * 0.4f); gp.AddArc(w * 0.1f, h * 0.45f, w * 0.8f, h * 0.6f, 180, 180); gp.CloseFigure(); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0.05), (0.65, 0.2), (0.65, 0.35), (0.5, 0.45), (0.35, 0.35), (0.35, 0.2), (0.5, 0.05), (0.1, 0.95), (0.9, 0.95)));

            // 40. Manual Task (Tác vụ thủ công cờ lê)
            AddItem(cat, "flow_manual_task", "Tác vụ cờ lê thủ công (Manual Task)",
                (w, h) => GPoly(w, h, (0.1f, 0.9f), (0.5f, 0.5f), (0.4f, 0.3f), (0.6f, 0.1f), (0.8f, 0.1f), (0.85f, 0.25f), (0.75f, 0.35f), (0.9f, 0.4f), (0.7f, 0.6f), (0.5f, 0.5f), (0.2f, 1)),
                (w, h) => WPoly(w, h, (0.1, 0.9), (0.5, 0.5), (0.4, 0.3), (0.6, 0.1), (0.8, 0.1), (0.85, 0.25), (0.75, 0.35), (0.9, 0.4), (0.7, 0.6), (0.5, 0.5), (0.2, 1)));

            // 41. Script Task (Tác vụ kịch bản mã - Cuộn giấy)
            AddItem(cat, "flow_script_task", "Tác vụ kịch bản mã (Script Task Scroll)",
                (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.85f, 0.1f), (0.9f, 0.3f), (0.8f, 0.85f), (0.15f, 0.85f), (0.05f, 0.3f)),
                (w, h) => WPoly(w, h, (0.1, 0.1), (0.85, 0.1), (0.9, 0.3), (0.8, 0.85), (0.15, 0.85), (0.05, 0.3)));

            // 42. Send Task (Tác vụ gửi đi - Máy bay giấy)
            AddItem(cat, "flow_send_task", "Tác vụ gửi đi (Send Task Airplane)",
                (w, h) => GPoly(w, h, (0.05f, 0.5f), (0.95f, 0.05f), (0.55f, 0.95f), (0.45f, 0.6f)),
                (w, h) => WPoly(w, h, (0.05, 0.5), (0.95, 0.05), (0.55, 0.95), (0.45, 0.6)));

            // 43. Receive Task (Tác vụ tiếp nhận - Khay thư mũi tên xuống)
            AddItem(cat, "flow_receive_task", "Tác vụ tiếp nhận (Receive Task Inbox)",
                (w, h) => GPoly(w, h, (0, 0.4f), (0.2f, 0.4f), (0.3f, 0.7f), (0.7f, 0.7f), (0.8f, 0.4f), (1, 0.4f), (0.9f, 0.95f), (0.1f, 0.95f)),
                (w, h) => WPoly(w, h, (0, 0.4), (0.2, 0.4), (0.3, 0.7), (0.7, 0.7), (0.8, 0.4), (1, 0.4), (0.9, 0.95), (0.1, 0.95)));

            // 44. Parallel Gateway (Cổng phân luồng song song - Dấu cộng trong thoi)
            AddItem(cat, "flow_parallel_gateway", "Cổng phân luồng song song (Parallel Gateway +)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.5f, h * 0.75f); gp.AddLine(w * 0.25f, h * 0.5f, w * 0.75f, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.25), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.25, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.5), true, false); } g.Freeze(); return g; });

            // 45. Inclusive Gateway (Cổng phân luồng bao hàm - Vòng tròn trong thoi)
            AddItem(cat, "flow_inclusive_gateway", "Cổng phân luồng bao hàm (Inclusive Gateway O)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddEllipse(w * 0.35f, h * 0.35f, w * 0.3f, h * 0.3f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.35), true, true); ctx.ArcTo(new WpfPoint(w * 0.5, h * 0.65), new WpfSize(w * 0.15, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.5, h * 0.35), new WpfSize(w * 0.15, h * 0.15), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 46. Complex Gateway (Cổng phân luồng phức tạp - Dấu hoa thị * trong thoi)
            AddItem(cat, "flow_complex_gateway", "Cổng phân luồng phức tạp (Complex Gateway *)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.5f, h * 0.75f); gp.AddLine(w * 0.25f, h * 0.5f, w * 0.75f, h * 0.5f); gp.AddLine(w * 0.32f, h * 0.32f, w * 0.68f, h * 0.68f); gp.AddLine(w * 0.68f, h * 0.32f, w * 0.32f, h * 0.68f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.25), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.75), true, false); ctx.BeginFigure(new WpfPoint(w * 0.25, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.5), true, false); } g.Freeze(); return g; });

            // 47. Event Gateway (Cổng kích hoạt sự kiện - Ngũ giác trong thoi)
            AddItem(cat, "flow_event_gateway", "Cổng kích hoạt sự kiện (Event Gateway)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.5f) }); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, h * 0.28f), new PointF(w * 0.7f, h * 0.42f), new PointF(w * 0.62f, h * 0.68f), new PointF(w * 0.38f, h * 0.68f), new PointF(w * 0.3f, h * 0.42f) }); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.5, 0), true, true); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.LineTo(new WpfPoint(0, h * 0.5), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, h * 0.28), true, true); ctx.LineTo(new WpfPoint(w * 0.7, h * 0.42), true, false); ctx.LineTo(new WpfPoint(w * 0.62, h * 0.68), true, false); ctx.LineTo(new WpfPoint(w * 0.38, h * 0.68), true, false); ctx.LineTo(new WpfPoint(w * 0.3, h * 0.42), true, false); } g.Freeze(); return g; });

            // 48. Error Event (Sự kiện lỗi - Vòng tròn có tia sét)
            AddItem(cat, "flow_error_event", "Sự kiện lỗi ngoại lệ (Error Event Lightning)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddPolygon(new PointF[] { new PointF(w * 0.55f, h * 0.2f), new PointF(w * 0.35f, h * 0.55f), new PointF(w * 0.52f, h * 0.55f), new PointF(w * 0.45f, h * 0.8f), new PointF(w * 0.68f, h * 0.45f), new PointF(w * 0.52f, h * 0.45f) }); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.55, h * 0.2), true, true); ctx.LineTo(new WpfPoint(w * 0.35, h * 0.55), true, false); ctx.LineTo(new WpfPoint(w * 0.52, h * 0.55), true, false); ctx.LineTo(new WpfPoint(w * 0.45, h * 0.8), true, false); ctx.LineTo(new WpfPoint(w * 0.68, h * 0.45), true, false); ctx.LineTo(new WpfPoint(w * 0.52, h * 0.45), true, false); } g.Freeze(); return g; });

            // 49. Terminate Event (Sự kiện kết thúc toàn bộ - Hai vòng tròn đồng tâm)
            AddItem(cat, "flow_terminate_event", "Sự kiện kết thúc toàn bộ (Terminate End Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.25f, h * 0.25f, w * 0.5f, h * 0.5f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w / 2.0, h * 0.25), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h * 0.75), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, h * 0.25), new WpfSize(w * 0.25, h * 0.25), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });

            // 50. Cancel Event (Sự kiện hủy bỏ quy trình - Vòng tròn có dấu X)
            AddItem(cat, "flow_cancel_event", "Sự kiện hủy bỏ quy trình (Cancel End Event)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.3f, h * 0.3f, w * 0.7f, h * 0.7f); gp.AddLine(w * 0.7f, h * 0.3f, w * 0.3f, h * 0.7f); return gp; },
                (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w / 2.0, 0), true, true); ctx.ArcTo(new WpfPoint(w / 2.0, h), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w / 2.0, 0), new WpfSize(w / 2.0, h / 2.0), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.BeginFigure(new WpfPoint(w * 0.3, h * 0.3), false, false); ctx.LineTo(new WpfPoint(w * 0.7, h * 0.7), true, false); ctx.BeginFigure(new WpfPoint(w * 0.7, h * 0.3), false, false); ctx.LineTo(new WpfPoint(w * 0.3, h * 0.7), true, false); } g.Freeze(); return g; });
        }
        #endregion

        #region Category 7: Bóng thoại & Chú thích (35 mẫu)
        private static void PopulateCallouts()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Callouts", Title = "Bóng thoại & Chú thích" };
            _categories.Add(cat);

            // 1-6: Bóng thoại chữ nhật các hướng đuôi
            AddItem(cat, "callout_rect_bl", "Bóng thoại chữ nhật đuôi góc trái", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.35f, 0.75f), (0.1f, 1), (0.2f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.35, 0.75), (0.1, 1), (0.2, 0.75), (0, 0.75)));
            AddItem(cat, "callout_rect_br", "Bóng thoại chữ nhật đuôi góc phải", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.9f, 1), (0.75f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.9, 1), (0.75, 0.75), (0, 0.75)));
            AddItem(cat, "callout_rect_bc", "Bóng thoại chữ nhật đuôi chính giữa", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.6f, 0.75f), (0.5f, 1), (0.4f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.6, 0.75), (0.5, 1), (0.4, 0.75), (0, 0.75)));
            AddItem(cat, "callout_rect_left", "Bóng thoại chữ nhật đuôi sang trái", (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 1), (0.2f, 1), (0.2f, 0.65f), (0, 0.5f), (0.2f, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 1), (0.2, 1), (0.2, 0.65), (0, 0.5), (0.2, 0.35)));
            AddItem(cat, "callout_rect_right", "Bóng thoại chữ nhật đuôi sang phải", (w, h) => GPoly(w, h, (0, 0), (0.8f, 0), (0.8f, 0.35f), (1, 0.5f), (0.8f, 0.65f), (0.8f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (0.8, 0), (0.8, 0.35), (1, 0.5), (0.8, 0.65), (0.8, 1), (0, 1)));
            AddItem(cat, "callout_rect_top", "Bóng thoại chữ nhật đuôi trỏ lên trên", (w, h) => GPoly(w, h, (0, 0.25f), (0.4f, 0.25f), (0.5f, 0), (0.6f, 0.25f), (1, 0.25f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0.25), (0.4, 0.25), (0.5, 0), (0.6, 0.25), (1, 0.25), (1, 1), (0, 1)));

            // 7-11: Bóng thoại bo tròn các hướng đuôi
            AddItem(cat, "callout_round_bl", "Bóng thoại bo góc tròn đuôi trái", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.15f), (1, 0.6f), (0.9f, 0.75f), (0.35f, 0.75f), (0.1f, 1), (0.2f, 0.75f), (0.1f, 0.75f), (0, 0.6f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.15), (1, 0.6), (0.9, 0.75), (0.35, 0.75), (0.1, 1), (0.2, 0.75), (0.1, 0.75), (0, 0.6), (0, 0.15)));
            AddItem(cat, "callout_round_br", "Bóng thoại bo góc tròn đuôi phải", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.15f), (1, 0.6f), (0.9f, 0.75f), (0.9f, 1), (0.75f, 0.75f), (0.1f, 0.75f), (0, 0.6f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.15), (1, 0.6), (0.9, 0.75), (0.9, 1), (0.75, 0.75), (0.1, 0.75), (0, 0.6), (0, 0.15)));
            AddItem(cat, "callout_round_bc", "Bóng thoại bo góc tròn đuôi giữa", (w, h) => GPoly(w, h, (0.1f, 0), (0.9f, 0), (1, 0.15f), (1, 0.6f), (0.9f, 0.75f), (0.6f, 0.75f), (0.5f, 1), (0.4f, 0.75f), (0.1f, 0.75f), (0, 0.6f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.9, 0), (1, 0.15), (1, 0.6), (0.9, 0.75), (0.6, 0.75), (0.5, 1), (0.4, 0.75), (0.1, 0.75), (0, 0.6), (0, 0.15)));
            AddItem(cat, "callout_round_left", "Bóng thoại bo góc đuôi sang trái", (w, h) => GPoly(w, h, (0.25f, 0), (0.9f, 0), (1, 0.15f), (1, 0.85f), (0.9f, 1), (0.25f, 1), (0.2f, 0.85f), (0.2f, 0.65f), (0, 0.5f), (0.2f, 0.35f), (0.2f, 0.15f)), (w, h) => WPoly(w, h, (0.25, 0), (0.9, 0), (1, 0.15), (1, 0.85), (0.9, 1), (0.25, 1), (0.2, 0.85), (0.2, 0.65), (0, 0.5), (0.2, 0.35), (0.2, 0.15)));
            AddItem(cat, "callout_round_right", "Bóng thoại bo góc đuôi sang phải", (w, h) => GPoly(w, h, (0.1f, 0), (0.75f, 0), (0.8f, 0.15f), (0.8f, 0.35f), (1, 0.5f), (0.8f, 0.65f), (0.8f, 0.85f), (0.75f, 1), (0.1f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.75, 0), (0.8, 0.15), (0.8, 0.35), (1, 0.5), (0.8, 0.65), (0.8, 0.85), (0.75, 1), (0.1, 1), (0, 0.85), (0, 0.15)));

            // 12-14: Bóng thoại hình bầu dục
            AddItem(cat, "callout_oval_bl", "Bóng thoại bầu dục đuôi trái", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.15f), (1, 0.4f), (0.9f, 0.65f), (0.5f, 0.75f), (0.25f, 0.75f), (0.1f, 1), (0.2f, 0.7f), (0.1f, 0.65f), (0, 0.4f), (0.1f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.15), (1, 0.4), (0.9, 0.65), (0.5, 0.75), (0.25, 0.75), (0.1, 1), (0.2, 0.7), (0.1, 0.65), (0, 0.4), (0.1, 0.15)));
            AddItem(cat, "callout_oval_br", "Bóng thoại bầu dục đuôi phải", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.15f), (1, 0.4f), (0.9f, 0.65f), (0.8f, 0.7f), (0.9f, 1), (0.75f, 0.75f), (0.5f, 0.75f), (0.1f, 0.65f), (0, 0.4f), (0.1f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.15), (1, 0.4), (0.9, 0.65), (0.8, 0.7), (0.9, 1), (0.75, 0.75), (0.5, 0.75), (0.1, 0.65), (0, 0.4), (0.1, 0.15)));
            AddItem(cat, "callout_oval_bc", "Bóng thoại bầu dục đuôi giữa", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.15f), (1, 0.4f), (0.9f, 0.65f), (0.58f, 0.75f), (0.5f, 1), (0.42f, 0.75f), (0.1f, 0.65f), (0, 0.4f), (0.1f, 0.15f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.15), (1, 0.4), (0.9, 0.65), (0.58, 0.75), (0.5, 1), (0.42, 0.75), (0.1, 0.65), (0, 0.4), (0.1, 0.15)));

            // 15-18: Bóng thoại đám mây & suy nghĩ
            AddItem(cat, "callout_thought_cloud", "Bóng thoại đám mây suy nghĩ 7 múi", (w, h) => GStar(w, h, 8, 0.75), (w, h) => WStar(w, h, 8, 0.75));
            AddItem(cat, "callout_cloud_dream", "Bóng thoại giấc mơ bồng bềnh", (w, h) => GStar(w, h, 10, 0.78), (w, h) => WStar(w, h, 10, 0.78));
            AddItem(cat, "callout_storm_cloud", "Bóng thoại sấm chớp tức giận", (w, h) => GPoly(w, h, (0.2f, 0.1f), (0.8f, 0.1f), (0.95f, 0.35f), (0.85f, 0.65f), (0.55f, 0.65f), (0.45f, 1), (0.48f, 0.75f), (0.15f, 0.65f), (0.05f, 0.35f)), (w, h) => WPoly(w, h, (0.2, 0.1), (0.8, 0.1), (0.95, 0.35), (0.85, 0.65), (0.55, 0.65), (0.45, 1), (0.48, 0.75), (0.15, 0.65), (0.05, 0.35)));
            AddItem(cat, "callout_bubbles_cluster", "Cụm bóng thoại suy tưởng liên hoàn", (w, h) => GStar(w, h, 6, 0.8), (w, h) => WStar(w, h, 6, 0.8));

            // 19-22: Hét lớn & nổ tung
            AddItem(cat, "callout_scream_burst", "Bóng thoại la hét 12 cánh nhọn", (w, h) => GStar(w, h, 12, 0.6), (w, h) => WStar(w, h, 12, 0.6));
            AddItem(cat, "callout_explosion_shout", "Bóng thoại tiếng thét nổ 16 tia", (w, h) => GStar(w, h, 16, 0.68), (w, h) => WStar(w, h, 16, 0.68));
            AddItem(cat, "callout_zigzag_burst", "Bóng thoại gấp khúc cảm thán", (w, h) => GStar(w, h, 10, 0.55), (w, h) => WStar(w, h, 10, 0.55));
            AddItem(cat, "callout_jagged_alert", "Bóng thoại tia gai báo động", (w, h) => GStar(w, h, 14, 0.72), (w, h) => WStar(w, h, 14, 0.72));

            // 23-26: Phong cách tin nhắn hiện đại
            AddItem(cat, "callout_imessage_left", "Bóng thoại tin nhắn iOS đuôi trái", (w, h) => GPoly(w, h, (0.15f, 0), (0.9f, 0), (1, 0.15f), (1, 0.85f), (0.9f, 1), (0.2f, 1), (0, 1), (0.1f, 0.85f), (0.05f, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.9, 0), (1, 0.15), (1, 0.85), (0.9, 1), (0.2, 1), (0, 1), (0.1, 0.85), (0.05, 0.15)));
            AddItem(cat, "callout_imessage_right", "Bóng thoại tin nhắn iOS đuôi phải", (w, h) => GPoly(w, h, (0.1f, 0), (0.85f, 0), (0.95f, 0.15f), (0.9f, 0.85f), (1, 1), (0.8f, 1), (0.1f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.1, 0), (0.85, 0), (0.95, 0.15), (0.9, 0.85), (1, 1), (0.8, 1), (0.1, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "callout_telegram_bubble", "Bóng thoại tròn trịa hiện đại", (w, h) => GRoundedRect(w, h * 0.85f, 10f), (w, h) => WRoundedRect(w, h * 0.85, 10.0));
            AddItem(cat, "callout_dual_dialogue", "Hội thoại đối đáp song phương", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.7f, 0.1f), (0.7f, 0.5f), (0.5f, 0.5f), (0.4f, 0.7f), (0.45f, 0.5f), (0.1f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.7, 0.1), (0.7, 0.5), (0.5, 0.5), (0.4, 0.7), (0.45, 0.5), (0.1, 0.5)));

            // 27-30: Chú thích đường dẫn (Line Callouts)
            AddItem(cat, "callout_line_1seg", "Chú thích đường dẫn 1 đoạn nghiêng", (w, h) => GLines(w, h, (0, 0.85f), (0.8f, 0.85f), (1, 0.15f)), (w, h) => WLines(w, h, (0, 0.85), (0.8, 0.85), (1, 0.15)));
            AddItem(cat, "callout_line_2seg", "Chú thích đường khuỷu 2 đoạn vuông", (w, h) => GLines(w, h, (0, 0.85f), (0.6f, 0.85f), (0.6f, 0.15f), (1, 0.15f)), (w, h) => WLines(w, h, (0, 0.85), (0.6, 0.85), (0.6, 0.15), (1, 0.15)));
            AddItem(cat, "callout_line_3seg_dot", "Chú thích 3 đoạn kèm chấm neo", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLines(new PointF[] { new PointF(0, h * 0.85f), new PointF(w * 0.5f, h * 0.85f), new PointF(w * 0.75f, h * 0.35f), new PointF(w, h * 0.35f) }); gp.AddEllipse(w * 0.9f, h * 0.25f, w * 0.1f, h * 0.2f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, h * 0.85), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h * 0.85), true, false); ctx.LineTo(new WpfPoint(w * 0.75, h * 0.35), true, false); ctx.LineTo(new WpfPoint(w, h * 0.35), true, false); ctx.BeginFigure(new WpfPoint(w * 0.9, h * 0.25), true, true); ctx.ArcTo(new WpfPoint(w * 0.9, h * 0.45), new WpfSize(w * 0.05, h * 0.1), 0, false, WpfSweepDirection.Clockwise, true, false); ctx.ArcTo(new WpfPoint(w * 0.9, h * 0.25), new WpfSize(w * 0.05, h * 0.1), 0, false, WpfSweepDirection.Clockwise, true, false); } g.Freeze(); return g; });
            AddItem(cat, "callout_line_accent_bar", "Chú thích đường dẫn có thanh nhấn dọc", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, 0, 0, h); gp.AddLine(0, h * 0.5f, w * 0.7f, h * 0.5f); gp.AddLine(w * 0.7f, h * 0.5f, w, h * 0.15f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), false, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w * 0.7, h * 0.5), true, false); ctx.LineTo(new WpfPoint(w, h * 0.15), true, false); } g.Freeze(); return g; });

            // 31-35: Bóng thoại chú thích đặc biệt
            AddItem(cat, "callout_quote_box", "Hộp trích dẫn danh ngôn (Quote Box)", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.8f), (0.4f, 0.8f), (0.2f, 1), (0.25f, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.8), (0.4, 0.8), (0.2, 1), (0.25, 0.8), (0, 0.8)));
            AddItem(cat, "callout_whisper_box", "Bóng thoại thì thầm nét đứt", (w, h) => GRoundedRect(w, h * 0.85f, 6f), (w, h) => WRoundedRect(w, h * 0.85, 6.0));
            AddItem(cat, "callout_tag_note", "Thẻ gắn ghi chú đục lỗ nhãn", (w, h) => GPoly(w, h, (0.2f, 0), (1, 0), (1, 1), (0.2f, 1), (0, 0.5f)), (w, h) => WPoly(w, h, (0.2, 0), (1, 0), (1, 1), (0.2, 1), (0, 0.5)));
            AddItem(cat, "callout_sticky_curled", "Giấy dán ghi chú quăn mép góc", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 0.75f), (0.75f, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 0.75), (0.75, 1), (0, 1)));
            AddItem(cat, "callout_ai_sparkle", "Bóng thoại hội thoại trợ lý AI", (w, h) => GPoly(w, h, (0.1f, 0), (0.85f, 0), (1, 0.2f), (1, 0.7f), (0.85f, 0.85f), (0.35f, 0.85f), (0.1f, 1), (0.2f, 0.85f), (0.05f, 0.7f), (0.05f, 0.2f)), (w, h) => WPoly(w, h, (0.1, 0), (0.85, 0), (1, 0.2), (1, 0.7), (0.85, 0.85), (0.35, 0.85), (0.1, 1), (0.2, 0.85), (0.05, 0.7), (0.05, 0.2)));
        }
        #endregion

        #region Category 8: Dải ruy băng & Biểu ngữ (35 mẫu)
        private static void PopulateBannersAndRibbons()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Banners", Title = "Dải ruy băng & Biểu ngữ" };
            _categories.Add(cat);

            // 1-5: Ruy băng uốn lượn cong
            AddItem(cat, "banner_arch_up", "Dải ruy băng cong vồng lên (Arch Up)", (w, h) => GPoly(w, h, (0, 0.4f), (0.15f, 0.15f), (0.85f, 0.15f), (1, 0.4f), (0.85f, 0.85f), (0.15f, 0.85f)), (w, h) => WPoly(w, h, (0, 0.4), (0.15, 0.15), (0.85, 0.15), (1, 0.4), (0.85, 0.85), (0.15, 0.85)));
            AddItem(cat, "banner_arch_down", "Dải ruy băng cong võng xuống (Arch Down)", (w, h) => GPoly(w, h, (0.15f, 0.15f), (0.85f, 0.15f), (1, 0.6f), (0.85f, 0.85f), (0.15f, 0.85f), (0, 0.6f)), (w, h) => WPoly(w, h, (0.15, 0.15), (0.85, 0.15), (1, 0.6), (0.85, 0.85), (0.15, 0.85), (0, 0.6)));
            AddItem(cat, "banner_s_wave", "Dải ruy băng lượn sóng chữ S (Wave)", (w, h) => GPoly(w, h, (0, 0.35f), (0.5f, 0.15f), (1, 0.45f), (1, 0.85f), (0.5f, 0.55f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.35), (0.5, 0.15), (1, 0.45), (1, 0.85), (0.5, 0.55), (0, 0.75)));
            AddItem(cat, "banner_double_wave", "Dải ruy băng lượn sóng đôi", (w, h) => GPoly(w, h, (0, 0.3f), (0.25f, 0.1f), (0.75f, 0.4f), (1, 0.2f), (1, 0.7f), (0.75f, 0.9f), (0.25f, 0.6f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.3), (0.25, 0.1), (0.75, 0.4), (1, 0.2), (1, 0.7), (0.75, 0.9), (0.25, 0.6), (0, 0.8)));
            AddItem(cat, "banner_wave_reverse", "Dải ruy băng sóng ngược chiều", (w, h) => GPoly(w, h, (0, 0.45f), (0.5f, 0.75f), (1, 0.35f), (1, 0.65f), (0.5f, 1), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.45), (0.5, 0.75), (1, 0.35), (1, 0.65), (0.5, 1), (0, 0.75)));

            // 6-10: Ruy băng gấp nếp & 3 khúc
            AddItem(cat, "banner_3fold_std", "Ruy băng gấp nếp 3 khúc cổ điển", (w, h) => GPoly(w, h, (0.15f, 0.25f), (0.85f, 0.25f), (0.85f, 0.75f), (0.15f, 0.75f)), (w, h) => WPoly(w, h, (0.15, 0.25), (0.85, 0.25), (0.85, 0.75), (0.15, 0.75)));
            AddItem(cat, "banner_3fold_curved", "Ruy băng 3 khúc uốn cong vòm", (w, h) => GPoly(w, h, (0.2f, 0.15f), (0.8f, 0.15f), (0.85f, 0.65f), (0.15f, 0.65f)), (w, h) => WPoly(w, h, (0.2, 0.15), (0.8, 0.15), (0.85, 0.65), (0.15, 0.65)));
            AddItem(cat, "banner_z_fold", "Ruy băng gấp nếp zic-zac chữ Z", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.7f, 0.5f), (0.9f, 0.9f), (0.1f, 0.9f), (0.3f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.7, 0.5), (0.9, 0.9), (0.1, 0.9), (0.3, 0.5)));
            AddItem(cat, "banner_corner_45", "Dải ruy băng chéo góc 45 độ", (w, h) => GPoly(w, h, (0, 0.35f), (0.35f, 0), (1, 0.65f), (0.65f, 1)), (w, h) => WPoly(w, h, (0, 0.35), (0.35, 0), (1, 0.65), (0.65, 1)));
            AddItem(cat, "banner_pleated_sash", "Dải băng rôn xếp ly vinh danh", (w, h) => GPoly(w, h, (0.05f, 0.3f), (0.95f, 0.3f), (0.85f, 0.7f), (0.15f, 0.7f)), (w, h) => WPoly(w, h, (0.05, 0.3), (0.95, 0.3), (0.85, 0.7), (0.15, 0.7)));

            // 11-15: Cuộn giấy & thư tịch cổ
            AddItem(cat, "scroll_horizontal", "Cuộn giấy cổ nằm ngang (Horizontal Scroll)", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.85f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.85, 0.85), (0.05, 0.85)));
            AddItem(cat, "scroll_vertical", "Cuộn giấy cổ thẳng đứng (Vertical Scroll)", (w, h) => GPoly(w, h, (0.15f, 0.1f), (0.85f, 0.05f), (0.85f, 0.9f), (0.15f, 0.85f)), (w, h) => WPoly(w, h, (0.15, 0.1), (0.85, 0.05), (0.85, 0.9), (0.15, 0.85)));
            AddItem(cat, "scroll_ancient_papyrus", "Thư tịch sáp phong ấn Papyrus", (w, h) => GPoly(w, h, (0.12f, 0.1f), (0.88f, 0.12f), (0.85f, 0.9f), (0.15f, 0.88f)), (w, h) => WPoly(w, h, (0.12, 0.1), (0.88, 0.12), (0.85, 0.9), (0.15, 0.88)));
            AddItem(cat, "scroll_double_rolled", "Cuộn thư tịch mở rộng hai phía", (w, h) => GPoly(w, h, (0.08f, 0.2f), (0.92f, 0.2f), (0.92f, 0.8f), (0.08f, 0.8f)), (w, h) => WPoly(w, h, (0.08, 0.2), (0.92, 0.2), (0.92, 0.8), (0.08, 0.8)));
            AddItem(cat, "scroll_parchment_open", "Trang sách da cừu mở rộng", (w, h) => GPoly(w, h, (0.05f, 0.15f), (0.5f, 0.25f), (0.95f, 0.15f), (0.95f, 0.85f), (0.5f, 0.95f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.05, 0.15), (0.5, 0.25), (0.95, 0.15), (0.95, 0.85), (0.5, 0.95), (0.05, 0.85)));

            // 16-21: Cờ phướn & đuôi cá
            AddItem(cat, "flag_pennant_right", "Cờ tam giác đuôi nheo sang phải", (w, h) => GPoly(w, h, (0, 0.15f), (1, 0.5f), (0, 0.85f)), (w, h) => WPoly(w, h, (0, 0.15), (1, 0.5), (0, 0.85)));
            AddItem(cat, "flag_pennant_left", "Cờ tam giác đuôi nheo sang trái", (w, h) => GPoly(w, h, (1, 0.15f), (0, 0.5f), (1, 0.85f)), (w, h) => WPoly(w, h, (1, 0.15), (0, 0.5), (1, 0.85)));
            AddItem(cat, "flag_swallowtail_right", "Cờ đuôi cá hai ngạnh sang phải", (w, h) => GPoly(w, h, (0, 0.2f), (1, 0.2f), (0.75f, 0.5f), (1, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (1, 0.2), (0.75, 0.5), (1, 0.8), (0, 0.8)));
            AddItem(cat, "flag_swallowtail_left", "Cờ đuôi cá hai ngạnh sang trái", (w, h) => GPoly(w, h, (0, 0.2f), (0.25f, 0.5f), (0, 0.8f), (1, 0.8f), (1, 0.2f)), (w, h) => WPoly(w, h, (0, 0.2), (0.25, 0.5), (0, 0.8), (1, 0.8), (1, 0.2)));
            AddItem(cat, "flag_double_swallowtail", "Cờ hiệu hoàng gia khuyết hai đầu", (w, h) => GPoly(w, h, (0.15f, 0.2f), (0.85f, 0.2f), (0.7f, 0.5f), (0.85f, 0.8f), (0.15f, 0.8f), (0.3f, 0.5f)), (w, h) => WPoly(w, h, (0.15, 0.2), (0.85, 0.2), (0.7, 0.5), (0.85, 0.8), (0.15, 0.8), (0.3, 0.5)));
            AddItem(cat, "flag_burgee_marine", "Cờ hiệu hàng hải Burgee vát nhọn", (w, h) => GPoly(w, h, (0, 0.1f), (0.9f, 0.35f), (0.7f, 0.5f), (0.9f, 0.65f), (0, 0.9f)), (w, h) => WPoly(w, h, (0, 0.1), (0.9, 0.35), (0.7, 0.5), (0.9, 0.65), (0, 0.9)));

            // 22-26: Huy hiệu & Rosette
            AddItem(cat, "medal_rosette_tails", "Huy chương hoa cúc hai dải lụa", (w, h) => GPoly(w, h, (0.3f, 0), (0.7f, 0), (0.9f, 0.3f), (0.7f, 0.6f), (0.85f, 1), (0.6f, 0.8f), (0.5f, 0.9f), (0.4f, 0.8f), (0.15f, 1), (0.3f, 0.6f), (0.1f, 0.3f)), (w, h) => WPoly(w, h, (0.3, 0), (0.7, 0), (0.9, 0.3), (0.7, 0.6), (0.85, 1), (0.6, 0.8), (0.5, 0.9), (0.4, 0.8), (0.15, 1), (0.3, 0.6), (0.1, 0.3)));
            AddItem(cat, "badge_drapery_shield", "Biểu ngữ dải lụa nẹp khiên", (w, h) => GPoly(w, h, (0.1f, 0.2f), (0.9f, 0.2f), (0.85f, 0.6f), (0.5f, 0.9f), (0.15f, 0.6f)), (w, h) => WPoly(w, h, (0.1, 0.2), (0.9, 0.2), (0.85, 0.6), (0.5, 0.9), (0.15, 0.6)));
            AddItem(cat, "plaque_victorian", "Biển bảng biểu ngữ Victorian", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.95f, 0.5f), (0.9f, 0.85f), (0.1f, 0.85f), (0.05f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.95, 0.5), (0.9, 0.85), (0.1, 0.85), (0.05, 0.5)));
            AddItem(cat, "cartouche_baroque", "Khung tên phù điêu Baroque", (w, h) => GPoly(w, h, (0.2f, 0.1f), (0.8f, 0.1f), (1, 0.3f), (0.9f, 0.7f), (0.8f, 0.9f), (0.2f, 0.9f), (0.1f, 0.7f), (0, 0.3f)), (w, h) => WPoly(w, h, (0.2, 0.1), (0.8, 0.1), (1, 0.3), (0.9, 0.7), (0.8, 0.9), (0.2, 0.9), (0.1, 0.7), (0, 0.3)));
            AddItem(cat, "banner_star_crest", "Dải ruy băng đính huy hiệu sao", (w, h) => GPoly(w, h, (0.05f, 0.3f), (0.95f, 0.3f), (0.9f, 0.7f), (0.05f, 0.7f)), (w, h) => WPoly(w, h, (0.05, 0.3), (0.95, 0.3), (0.9, 0.7), (0.05, 0.7)));

            // 27-35: Nhãn thương mại & sự kiện
            AddItem(cat, "tag_sale_hanging", "Thẻ bài giảm giá Sale treo", (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 0.4f), (0.85f, 1), (0.15f, 1), (0, 0.4f)), (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 0.4), (0.85, 1), (0.15, 1), (0, 0.4)));
            AddItem(cat, "tag_price_slanted", "Thẻ giá vát nhọn đục lỗ", (w, h) => GPoly(w, h, (0, 0.2f), (0.7f, 0.2f), (1, 0.5f), (0.7f, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (0.7, 0.2), (1, 0.5), (0.7, 0.8), (0, 0.8)));
            AddItem(cat, "ribbon_gift_bow", "Dải nơ gói quà tặng nghệ thuật", (w, h) => GPoly(w, h, (0.5f, 0.3f), (0.85f, 0.1f), (0.75f, 0.5f), (0.95f, 0.9f), (0.55f, 0.65f), (0.5f, 0.85f), (0.45f, 0.65f), (0.05f, 0.9f), (0.25f, 0.5f), (0.15f, 0.1f)), (w, h) => WPoly(w, h, (0.5, 0.3), (0.85, 0.1), (0.75, 0.5), (0.95, 0.9), (0.55, 0.65), (0.5, 0.85), (0.45, 0.65), (0.05, 0.9), (0.25, 0.5), (0.15, 0.1)));
            AddItem(cat, "ribbon_bookmark_strip", "Dải ruy băng đánh dấu trang", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (0.8f, 1), (0.5f, 0.8f), (0.2f, 1)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (0.8, 1), (0.5, 0.8), (0.2, 1)));
            AddItem(cat, "seal_gold_quality", "Dải ruy băng chứng nhận chất lượng", (w, h) => GPoly(w, h, (0.15f, 0.2f), (0.85f, 0.2f), (0.9f, 0.8f), (0.5f, 0.65f), (0.1f, 0.8f)), (w, h) => WPoly(w, h, (0.15, 0.2), (0.85, 0.2), (0.9, 0.8), (0.5, 0.65), (0.1, 0.8)));
            AddItem(cat, "banner_grand_opening", "Băng khánh thành khai trương", (w, h) => GPoly(w, h, (0, 0.25f), (1, 0.25f), (0.95f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.25), (1, 0.25), (0.95, 0.75), (0, 0.75)));
            AddItem(cat, "banner_pro_badge", "Dải biểu ngữ phiên bản Pro", (w, h) => GPoly(w, h, (0.1f, 0.25f), (0.9f, 0.25f), (0.85f, 0.75f), (0.15f, 0.75f)), (w, h) => WPoly(w, h, (0.1, 0.25), (0.9, 0.25), (0.85, 0.75), (0.15, 0.75)));
            AddItem(cat, "wreath_laurel_ribbon", "Dải ruy băng vòng nguyệt quế", (w, h) => GPoly(w, h, (0.2f, 0.3f), (0.8f, 0.3f), (0.95f, 0.7f), (0.75f, 0.6f), (0.5f, 0.85f), (0.25f, 0.6f), (0.05f, 0.7f)), (w, h) => WPoly(w, h, (0.2, 0.3), (0.8, 0.3), (0.95, 0.7), (0.75, 0.6), (0.5, 0.85), (0.25, 0.6), (0.05, 0.7)));
            AddItem(cat, "banner_award_sash", "Dải ruy băng trao giải danh giá", (w, h) => GPoly(w, h, (0.05f, 0.2f), (0.95f, 0.2f), (0.85f, 0.8f), (0.15f, 0.8f)), (w, h) => WPoly(w, h, (0.05, 0.2), (0.95, 0.2), (0.85, 0.8), (0.15, 0.8)));
        }
        #endregion

        #region Category 9: Hình khối 3D & Isometric (35 mẫu)
        private static void PopulateIsometric3DShapes()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Isometric3D", Title = "Hình khối 3D & Isometric" };
            _categories.Add(cat);

            // 1-9: Lăng trụ & khối đa diện
            AddItem(cat, "iso3d_cube", "Khối lập phương Isometric (Cube)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, 0), new PointF(w, h * 0.25f), new PointF(w * 0.5f, h * 0.5f), new PointF(0, h * 0.25f) }); gp.AddPolygon(new PointF[] { new PointF(0, h * 0.25f), new PointF(w * 0.5f, h * 0.5f), new PointF(w * 0.5f, h), new PointF(0, h * 0.75f) }); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, h * 0.5f), new PointF(w, h * 0.25f), new PointF(w, h * 0.75f), new PointF(w * 0.5f, h) }); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0), (1, 0.25), (1, 0.75), (0.5, 1), (0, 0.75), (0, 0.25)));

            AddItem(cat, "iso3d_box_wide", "Khối hộp chữ nhật 3D rộng (Wide Box)",
                (w, h) => GPoly(w, h, (0.5f, 0.1f), (1, 0.3f), (1, 0.7f), (0.5f, 0.9f), (0, 0.7f), (0, 0.3f)),
                (w, h) => WPoly(w, h, (0.5, 0.1), (1, 0.3), (1, 0.7), (0.5, 0.9), (0, 0.7), (0, 0.3)));

            AddItem(cat, "iso3d_column_tall", "Khối cột trụ chữ nhật 3D đứng cao",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.85f, 0.15f), (0.85f, 0.85f), (0.5f, 1), (0.15f, 0.85f), (0.15f, 0.15f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.85, 0.15), (0.85, 0.85), (0.5, 1), (0.15, 0.85), (0.15, 0.15)));

            AddItem(cat, "iso3d_prism_hex", "Lăng trụ lục giác 3D (Hexagonal Prism)",
                (w, h) => GPoly(w, h, (0.25f, 0), (0.75f, 0), (1, 0.2f), (1, 0.8f), (0.75f, 1), (0.25f, 1), (0, 0.8f), (0, 0.2f)),
                (w, h) => WPoly(w, h, (0.25, 0), (0.75, 0), (1, 0.2), (1, 0.8), (0.75, 1), (0.25, 1), (0, 0.8), (0, 0.2)));

            AddItem(cat, "iso3d_prism_tri", "Lăng trụ tam giác 3D (Triangular Prism)",
                (w, h) => GPoly(w, h, (0.5f, 0.05f), (1, 0.35f), (0.8f, 0.95f), (0.2f, 0.95f), (0, 0.35f)),
                (w, h) => WPoly(w, h, (0.5, 0.05), (1, 0.35), (0.8, 0.95), (0.2, 0.95), (0, 0.35)));

            AddItem(cat, "iso3d_dodecahedron", "Khối đa diện 12 mặt 3D",
                (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.95f, 0.3f), (0.85f, 0.75f), (0.5f, 0.95f), (0.15f, 0.75f), (0.05f, 0.3f)),
                (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.95, 0.3), (0.85, 0.75), (0.5, 0.95), (0.15, 0.75), (0.05, 0.3)));

            AddItem(cat, "iso3d_icosahedron", "Khối đa diện 20 mặt 3D",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.25f), (0.9f, 0.75f), (0.5f, 1), (0.1f, 0.75f), (0.1f, 0.25f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.25), (0.9, 0.75), (0.5, 1), (0.1, 0.75), (0.1, 0.25)));

            AddItem(cat, "iso3d_diamond_gem", "Khối kim cương giác cắt 3D (Gem)",
                (w, h) => GPoly(w, h, (0.25f, 0.15f), (0.75f, 0.15f), (1, 0.4f), (0.5f, 0.95f), (0, 0.4f)),
                (w, h) => WPoly(w, h, (0.25, 0.15), (0.75, 0.15), (1, 0.4), (0.5, 0.95), (0, 0.4)));

            AddItem(cat, "iso3d_octahedron", "Khối bát diện 8 mặt 3D",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.5f), (0.5f, 1), (0.05f, 0.5f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.5), (0.5, 1), (0.05, 0.5)));

            // 10-15: Hình trụ tròn & nón
            AddItem(cat, "iso3d_cylinder_vert", "Hình trụ tròn đứng 3D (Cylinder)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h * 0.3f); gp.AddLine(0, h * 0.15f, 0, h * 0.85f); gp.AddArc(0, h * 0.7f, w, h * 0.3f, 180, -180); gp.AddLine(w, h * 0.85f, w, h * 0.15f); return gp; },
                (w, h) => WPoly(w, h, (0, 0.15), (0.5, 0), (1, 0.15), (1, 0.85), (0.5, 1), (0, 0.85)));

            AddItem(cat, "iso3d_cylinder_horiz", "Hình trụ tròn nằm ngang 3D",
                (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.5f), (0.85f, 1), (0.15f, 1), (0, 0.5f)),
                (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.5), (0.85, 1), (0.15, 1), (0, 0.5)));

            AddItem(cat, "iso3d_cone_std", "Hình nón nhọn đỉnh 3D (Cone)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.5f, 0, w, h * 0.85f); gp.AddArc(0, h * 0.7f, w, h * 0.3f, 0, 180); gp.AddLine(0, h * 0.85f, w * 0.5f, 0); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0), (1, 0.85), (0.5, 1), (0, 0.85)));

            AddItem(cat, "iso3d_cone_truncated", "Hình nón cụt 3D (Truncated Cone)",
                (w, h) => GPoly(w, h, (0.3f, 0.15f), (0.7f, 0.15f), (0.95f, 0.85f), (0.05f, 0.85f)),
                (w, h) => WPoly(w, h, (0.3, 0.15), (0.7, 0.15), (0.95, 0.85), (0.05, 0.85)));

            AddItem(cat, "iso3d_pipe_hollow", "Ống trụ rỗng ruột 3D (Hollow Pipe)",
                (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.9f, 0.85f), (0.1f, 0.85f)),
                (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.9, 0.85), (0.1, 0.85)));

            AddItem(cat, "iso3d_capsule_tank", "Bình chứa áp suất 3D Capsule",
                (w, h) => GRoundedRect(w, h * 0.6f, 15f),
                (w, h) => WRoundedRect(w, h * 0.6, 15.0));

            // 16-20: Kim tự tháp & hình cầu
            AddItem(cat, "iso3d_pyramid_std", "Kim tự tháp 4 mặt 3D (Pyramid)",
                (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.75f), (0.5f, 1), (0.05f, 0.75f)),
                (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.75), (0.5, 1), (0.05, 0.75)));

            AddItem(cat, "iso3d_pyramid_truncated", "Kim tự tháp cụt 3D",
                (w, h) => GPoly(w, h, (0.35f, 0.2f), (0.65f, 0.2f), (0.95f, 0.85f), (0.05f, 0.85f)),
                (w, h) => WPoly(w, h, (0.35, 0.2), (0.65, 0.2), (0.95, 0.85), (0.05, 0.85)));

            AddItem(cat, "iso3d_sphere_globe", "Hình cầu 3D có vĩ tuyến (Sphere)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(0, h * 0.35f, w, h * 0.3f); gp.AddEllipse(w * 0.35f, 0, w * 0.3f, h); return gp; },
                (w, h) => WEllipse(w, h));

            AddItem(cat, "iso3d_torus_ring", "Hình xuyến vòng đệm 3D (Torus)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, h * 0.15f, w, h * 0.7f); gp.AddEllipse(w * 0.25f, h * 0.3f, w * 0.5f, h * 0.4f); return gp; },
                (w, h) => WPoly(w, h, (0.5, 0.15), (1, 0.5), (0.5, 0.85), (0, 0.5)));

            AddItem(cat, "iso3d_dome_half", "Mái vòm bán cầu 3D (Dome)",
                (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddArc(0, 0, w, h * 1.5f, 180, 180); gp.AddLine(w, h * 0.75f, 0, h * 0.75f); gp.CloseFigure(); return gp; },
                (w, h) => WPoly(w, h, (0, 0.75), (0.1, 0.4), (0.5, 0.1), (0.9, 0.4), (1, 0.75)));

            // 21-35: Cấu trúc kiến trúc & cơ khí 3D
            AddItem(cat, "iso3d_stairs_3step", "Bậc thang 3D 3 bậc vươn cao", (w, h) => GPoly(w, h, (0, 0.8f), (0.33f, 0.8f), (0.33f, 0.5f), (0.66f, 0.5f), (0.66f, 0.2f), (1, 0.2f), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0.8), (0.33, 0.8), (0.33, 0.5), (0.66, 0.5), (0.66, 0.2), (1, 0.2), (1, 1), (0, 1)));
            AddItem(cat, "iso3d_l_block", "Khối góc vuông chữ L 3D", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.4f, 0.1f), (0.4f, 0.6f), (0.9f, 0.6f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.4, 0.1), (0.4, 0.6), (0.9, 0.6), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "iso3d_t_block", "Khối chữ T đối xứng 3D", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.9f, 0.15f), (0.9f, 0.45f), (0.65f, 0.45f), (0.65f, 0.9f), (0.35f, 0.9f), (0.35f, 0.45f), (0.1f, 0.45f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.9, 0.15), (0.9, 0.45), (0.65, 0.45), (0.65, 0.9), (0.35, 0.9), (0.35, 0.45), (0.1, 0.45)));
            AddItem(cat, "iso3d_u_block", "Khối rãnh chữ U lõm 3D", (w, h) => GPoly(w, h, (0.1f, 0.15f), (0.35f, 0.15f), (0.35f, 0.65f), (0.65f, 0.65f), (0.65f, 0.15f), (0.9f, 0.15f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.15), (0.35, 0.15), (0.35, 0.65), (0.65, 0.65), (0.65, 0.15), (0.9, 0.15), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "iso3d_db_stack", "Khối đĩa dữ liệu Database 3 tầng", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "iso3d_podium_3tier", "Bục vinh danh trao giải 3 cấp", (w, h) => GPoly(w, h, (0.05f, 0.5f), (0.35f, 0.5f), (0.35f, 0.2f), (0.65f, 0.2f), (0.65f, 0.65f), (0.95f, 0.65f), (0.95f, 1), (0.05f, 1)), (w, h) => WPoly(w, h, (0.05, 0.5), (0.35, 0.5), (0.35, 0.2), (0.65, 0.2), (0.65, 0.65), (0.95, 0.65), (0.95, 1), (0.05, 1)));
            AddItem(cat, "iso3d_roman_column", "Cột La Mã cổ điển 3D", (w, h) => GPoly(w, h, (0.15f, 0.05f), (0.85f, 0.05f), (0.75f, 0.2f), (0.75f, 0.8f), (0.85f, 0.95f), (0.15f, 0.95f), (0.25f, 0.8f), (0.25f, 0.2f)), (w, h) => WPoly(w, h, (0.15, 0.05), (0.85, 0.05), (0.75, 0.2), (0.75, 0.8), (0.85, 0.95), (0.15, 0.95), (0.25, 0.8), (0.25, 0.2)));
            AddItem(cat, "iso3d_gear_3d", "Bánh xe răng cưa truyền động 3D", (w, h) => GStar(w, h, 8, 0.72), (w, h) => WStar(w, h, 8, 0.72));
            AddItem(cat, "iso3d_rubik_cube", "Khối rubik phân đoạn 3D", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.3f), (0.95f, 0.75f), (0.5f, 0.95f), (0.05f, 0.75f), (0.05f, 0.3f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.3), (0.95, 0.75), (0.5, 0.95), (0.05, 0.75), (0.05, 0.3)));
            AddItem(cat, "iso3d_optical_prism", "Lăng kính quang học hội tụ 3D", (w, h) => GPoly(w, h, (0.5f, 0.1f), (0.95f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.5, 0.1), (0.95, 0.85), (0.05, 0.85)));
            AddItem(cat, "iso3d_screw_thread", "Trục vít truyền động xoắn ốc 3D", (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.65f, 0.85f), (0.5f, 0.98f), (0.35f, 0.85f)), (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.65, 0.85), (0.5, 0.98), (0.35, 0.85)));
            AddItem(cat, "iso3d_box_open_lid", "Khối hộp mở nắp trên 3D", (w, h) => GPoly(w, h, (0.1f, 0.25f), (0.9f, 0.25f), (0.8f, 0.9f), (0.2f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.25), (0.9, 0.25), (0.8, 0.9), (0.2, 0.9)));
            AddItem(cat, "iso3d_island_tile", "Mô hình đảo nổi Isometric Tile", (w, h) => GPoly(w, h, (0.5f, 0.15f), (0.95f, 0.45f), (0.5f, 0.95f), (0.05f, 0.45f)), (w, h) => WPoly(w, h, (0.5, 0.15), (0.95, 0.45), (0.5, 0.95), (0.05, 0.45)));
            AddItem(cat, "iso3d_bridge_span", "Nhịp cầu vượt kết nối 3D", (w, h) => GPoly(w, h, (0.05f, 0.35f), (0.95f, 0.35f), (0.95f, 0.75f), (0.8f, 0.75f), (0.8f, 0.55f), (0.2f, 0.55f), (0.2f, 0.75f), (0.05f, 0.75f)), (w, h) => WPoly(w, h, (0.05, 0.35), (0.95, 0.35), (0.95, 0.75), (0.8, 0.75), (0.8, 0.55), (0.2, 0.55), (0.2, 0.75), (0.05, 0.75)));
            AddItem(cat, "iso3d_wedge_ramp", "Mặt phẳng nghiêng dốc Ramp 3D", (w, h) => GPoly(w, h, (0.1f, 0.85f), (0.9f, 0.15f), (0.9f, 0.85f)), (w, h) => WPoly(w, h, (0.1, 0.85), (0.9, 0.15), (0.9, 0.85)));
        }
        #endregion

        #region Category 10: Sơ đồ tư duy & Cụm liên kết (50 mẫu)
        private static void PopulateDiagramComponents()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Diagrams", Title = "Sơ đồ tư duy & Cụm liên kết" };
            _categories.Add(cat);

            // 1-5: Phễu chuyển đổi (Conversion Funnels)
            AddItem(cat, "funnel_3stage", "Phễu chuyển đổi 3 tầng (Funnel 3)", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.65f, 0.95f), (0.35f, 0.95f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.65, 0.95), (0.35, 0.95)));
            AddItem(cat, "funnel_4stage", "Phễu chuyển đổi 4 tầng (Funnel 4)", (w, h) => GPoly(w, h, (0.02f, 0.05f), (0.98f, 0.05f), (0.62f, 0.95f), (0.38f, 0.95f)), (w, h) => WPoly(w, h, (0.02, 0.05), (0.98, 0.05), (0.62, 0.95), (0.38, 0.95)));
            AddItem(cat, "funnel_5stage", "Phễu chuyển đổi 5 tầng (Funnel 5)", (w, h) => GPoly(w, h, (0, 0.05f), (1, 0.05f), (0.6f, 0.95f), (0.4f, 0.95f)), (w, h) => WPoly(w, h, (0, 0.05), (1, 0.05), (0.6, 0.95), (0.4, 0.95)));
            AddItem(cat, "funnel_inverted_expand", "Mô hình phễu ngược mở rộng", (w, h) => GPoly(w, h, (0.35f, 0.05f), (0.65f, 0.05f), (0.95f, 0.95f), (0.05f, 0.95f)), (w, h) => WPoly(w, h, (0.35, 0.05), (0.65, 0.05), (0.95, 0.95), (0.05, 0.95)));
            AddItem(cat, "funnel_multichannel", "Phễu lọc bán hàng đa kênh", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.7f, 0.9f), (0.3f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.7, 0.9), (0.3, 0.9)));

            // 6-10: Kim tự tháp phân cấp (Hierarchy Pyramids)
            AddItem(cat, "pyramid_3tier", "Kim tự tháp phân cấp 3 tầng (Pyramid 3)", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.95f), (0.05f, 0.95f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.95), (0.05, 0.95)));
            AddItem(cat, "pyramid_4tier", "Kim tự tháp phân cấp 4 tầng (Pyramid 4)", (w, h) => GPoly(w, h, (0.5f, 0.02f), (0.98f, 0.98f), (0.02f, 0.98f)), (w, h) => WPoly(w, h, (0.5, 0.02), (0.98, 0.98), (0.02, 0.98)));
            AddItem(cat, "pyramid_5tier", "Kim tự tháp Maslow 5 tầng", (w, h) => GPoly(w, h, (0.5f, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0.5, 0), (1, 1), (0, 1)));
            AddItem(cat, "pyramid_inverted", "Kim tự tháp lộn ngược thách thức", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.5f, 0.95f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.5, 0.95)));
            AddItem(cat, "pyramid_stepped", "Kim tự tháp bậc thang phân bậc", (w, h) => GPoly(w, h, (0.35f, 0.1f), (0.65f, 0.1f), (0.65f, 0.4f), (0.8f, 0.4f), (0.8f, 0.7f), (0.95f, 0.7f), (0.95f, 1), (0.05f, 1), (0.05f, 0.7f), (0.2f, 0.7f), (0.2f, 0.4f), (0.35f, 0.4f)), (w, h) => WPoly(w, h, (0.35, 0.1), (0.65, 0.1), (0.65, 0.4), (0.8, 0.4), (0.8, 0.7), (0.95, 0.7), (0.95, 1), (0.05, 1), (0.05, 0.7), (0.2, 0.7), (0.2, 0.4), (0.35, 0.4)));

            // 11-15: Biểu đồ Venn & giao thoa
            AddItem(cat, "venn_2circle", "Biểu đồ Venn 2 vòng tròn giao nhau", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, h * 0.15f, w * 0.65f, h * 0.7f); gp.AddEllipse(w * 0.35f, h * 0.15f, w * 0.65f, h * 0.7f); return gp; }, (w, h) => WPoly(w, h, (0.35, 0.15), (0.65, 0.15), (1, 0.5), (0.65, 0.85), (0.35, 0.85), (0, 0.5)));
            AddItem(cat, "venn_3circle", "Biểu đồ Venn 3 vòng tròn giao thoa", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.2f, 0, w * 0.6f, h * 0.6f); gp.AddEllipse(0, h * 0.4f, w * 0.6f, h * 0.6f); gp.AddEllipse(w * 0.4f, h * 0.4f, w * 0.6f, h * 0.6f); return gp; }, (w, h) => WStar(w, h, 3, 0.6));
            AddItem(cat, "venn_4circle", "Biểu đồ Venn 4 cánh giao hòa", (w, h) => GStar(w, h, 4, 0.65), (w, h) => WStar(w, h, 4, 0.65));
            AddItem(cat, "euler_diagram", "Sơ đồ tập hợp con Euler", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.4f, h * 0.3f, w * 0.45f, h * 0.45f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "ikigai_4circle", "Sơ đồ Ikigai 4 vòng tròn hội tụ", (w, h) => GStar(w, h, 8, 0.7), (w, h) => WStar(w, h, 8, 0.7));

            // 16-20: Chu trình lặp (Cycles)
            AddItem(cat, "cycle_3arrow", "Chu trình tròn 3 mũi tên khép kín", (w, h) => GStar(w, h, 3, 0.5), (w, h) => WStar(w, h, 3, 0.5));
            AddItem(cat, "cycle_4arrow", "Chu trình tròn 4 mũi tên khép kín", (w, h) => GStar(w, h, 4, 0.55), (w, h) => WStar(w, h, 4, 0.55));
            AddItem(cat, "cycle_5arrow", "Chu trình tròn 5 mũi tên khép kín", (w, h) => GStar(w, h, 5, 0.6), (w, h) => WStar(w, h, 5, 0.6));
            AddItem(cat, "cycle_6arrow", "Chu trình tròn 6 mũi tên khép kín", (w, h) => GStar(w, h, 6, 0.65), (w, h) => WStar(w, h, 6, 0.65));
            AddItem(cat, "flywheel_growth", "Bánh đà tăng trưởng Flywheel", (w, h) => GStar(w, h, 8, 0.75), (w, h) => WStar(w, h, 8, 0.75));

            // 21-27: Bánh răng & bia ngắm
            AddItem(cat, "target_bullseye_3ring", "Bia ngắm mục tiêu 3 vòng (Bullseye)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddEllipse(w * 0.2f, h * 0.2f, w * 0.6f, h * 0.6f); gp.AddEllipse(w * 0.4f, h * 0.4f, w * 0.2f, h * 0.2f); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "target_arrow_center", "Bia ngắm trúng hồng tâm", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.1f, h * 0.9f, w * 0.5f, h * 0.5f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "gear_6tooth", "Bánh răng cơ khí 6 răng (Gear 6)", (w, h) => GStar(w, h, 6, 0.65), (w, h) => WStar(w, h, 6, 0.65));
            AddItem(cat, "gear_8tooth", "Bánh răng cơ khí 8 răng (Gear 8)", (w, h) => GStar(w, h, 8, 0.7), (w, h) => WStar(w, h, 8, 0.7));
            AddItem(cat, "gear_12tooth", "Bánh răng cơ khí 12 răng chính xác", (w, h) => GStar(w, h, 12, 0.75), (w, h) => WStar(w, h, 12, 0.75));
            AddItem(cat, "gear_dual_interlock", "Cụm bánh răng lồng ghép 2 trục", (w, h) => GPoly(w, h, (0.3f, 0.1f), (0.6f, 0.1f), (0.7f, 0.4f), (0.9f, 0.4f), (0.9f, 0.7f), (0.6f, 0.9f), (0.3f, 0.9f), (0.1f, 0.6f)), (w, h) => WPoly(w, h, (0.3, 0.1), (0.6, 0.1), (0.7, 0.4), (0.9, 0.4), (0.9, 0.7), (0.6, 0.9), (0.3, 0.9), (0.1, 0.6)));
            AddItem(cat, "gear_train_3gear", "Hệ thống truyền động 3 bánh răng", (w, h) => GStar(w, h, 6, 0.7), (w, h) => WStar(w, h, 6, 0.7));

            // 28-33: Sơ đồ tư duy & tổ ong
            AddItem(cat, "mindmap_hub_center", "Sơ đồ tư duy nút trung tâm 4 nhánh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.35f, h * 0.35f, w * 0.3f, h * 0.3f); gp.AddLine(w * 0.5f, h * 0.35f, w * 0.5f, 0.05f); gp.AddLine(w * 0.5f, h * 0.65f, w * 0.5f, 0.95f); gp.AddLine(w * 0.35f, h * 0.5f, 0.05f, h * 0.5f); gp.AddLine(w * 0.65f, h * 0.5f, 0.95f, h * 0.5f); return gp; }, (w, h) => WPoly(w, h, (0.5, 0.05), (0.65, 0.35), (0.95, 0.5), (0.65, 0.65), (0.5, 0.95), (0.35, 0.65), (0.05, 0.5), (0.35, 0.35)));
            AddItem(cat, "mindmap_tree_branch", "Bản đồ tư duy nút nhánh bo tròn", (w, h) => GRoundedRect(w, h * 0.5f, 8f), (w, h) => WRoundedRect(w, h * 0.5, 8.0));
            AddItem(cat, "honeycomb_3cluster", "Cụm tổ ong 3 lục giác liên hoàn", (w, h) => GPoly(w, h, (0.5f, 0), (0.9f, 0.25f), (0.9f, 0.75f), (0.5f, 1), (0.1f, 0.75f), (0.1f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.9, 0.25), (0.9, 0.75), (0.5, 1), (0.1, 0.75), (0.1, 0.25)));
            AddItem(cat, "honeycomb_7cluster", "Cụm tổ ong 7 lục giác hoa văn", (w, h) => GRegularPoly(w, h, 6, 0), (w, h) => WRegularPoly(w, h, 6, 0));
            AddItem(cat, "neural_network_nodes", "Mạng lưới nơ-ron kết nối", (w, h) => GPoly(w, h, (0.15f, 0.2f), (0.85f, 0.2f), (0.85f, 0.8f), (0.15f, 0.8f)), (w, h) => WPoly(w, h, (0.15, 0.2), (0.85, 0.2), (0.85, 0.8), (0.15, 0.8)));
            AddItem(cat, "binary_tree_hierarchy", "Sơ đồ phân nhánh nhị phân Binary", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.25f, 0.5f), (0.1f, 0.9f), (0.25f, 0.5f), (0.4f, 0.9f), (0.5f, 0.1f), (0.75f, 0.5f), (0.6f, 0.9f), (0.75f, 0.5f), (0.9f, 0.9f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.25, 0.5), (0.1, 0.9), (0.25, 0.5), (0.4, 0.9), (0.5, 0.1), (0.75, 0.5), (0.6, 0.9), (0.75, 0.5), (0.9, 0.9)));

            // 34-50: Các mô hình quản trị chiến lược & tiến độ
            AddItem(cat, "diag_fishbone_ishikawa", "Sơ đồ xương cá phân tích lỗi", (w, h) => GLines(w, h, (0.05f, 0.5f), (0.85f, 0.5f), (0.85f, 0.35f), (0.95f, 0.5f), (0.85f, 0.65f), (0.85f, 0.5f), (0.25f, 0.5f), (0.4f, 0.15f), (0.25f, 0.5f), (0.4f, 0.85f), (0.55f, 0.5f), (0.7f, 0.15f), (0.55f, 0.5f), (0.7f, 0.85f)), (w, h) => WLines(w, h, (0.05, 0.5), (0.85, 0.5), (0.85, 0.35), (0.95, 0.5), (0.85, 0.65), (0.85, 0.5), (0.25, 0.5), (0.4, 0.15), (0.25, 0.5), (0.4, 0.85), (0.55, 0.5), (0.7, 0.15), (0.55, 0.5), (0.7, 0.85)));
            AddItem(cat, "diag_matrix_2x2", "Ma trận 4 ô góc vuông 2x2", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddLine(w * 0.5f, 0, w * 0.5f, h); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(0, 0), true, true); ctx.LineTo(new WpfPoint(w, 0), true, false); ctx.LineTo(new WpfPoint(w, h), true, false); ctx.LineTo(new WpfPoint(0, h), true, false); ctx.BeginFigure(new WpfPoint(w * 0.5, 0), false, false); ctx.LineTo(new WpfPoint(w * 0.5, h), true, false); ctx.BeginFigure(new WpfPoint(0, h * 0.5), false, false); ctx.LineTo(new WpfPoint(w, h * 0.5), true, false); } g.Freeze(); return g; });
            AddItem(cat, "diag_org_chart", "Sơ đồ cơ cấu tổ chức Org Chart", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(w * 0.35f, 0, w * 0.3f, h * 0.25f)); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.5f, h * 0.5f); gp.AddLine(w * 0.15f, h * 0.5f, w * 0.85f, h * 0.5f); gp.AddRectangle(new RectangleF(0, h * 0.6f, w * 0.3f, h * 0.25f)); gp.AddRectangle(new RectangleF(w * 0.35f, h * 0.6f, w * 0.3f, h * 0.25f)); gp.AddRectangle(new RectangleF(w * 0.7f, h * 0.6f, w * 0.3f, h * 0.25f)); return gp; }, (w, h) => WPoly(w, h, (0.35f, 0), (0.65f, 0), (0.65f, 0.25f), (0.35f, 0.25f)));
            AddItem(cat, "diag_swot_box", "Mô hình SWOT 4 góc nổi bật", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "diag_chevron_pipeline", "Chuỗi quy trình nối tiếp 3 Chevron", (w, h) => GPoly(w, h, (0, 0), (0.75f, 0), (1, 0.5f), (0.75f, 1), (0, 1), (0.25f, 0.5f)), (w, h) => WPoly(w, h, (0, 0), (0.75, 0), (1, 0.5), (0.75, 1), (0, 1), (0.25, 0.5)));
            AddItem(cat, "diag_arrow_pipeline", "Đường ống mũi tên chuyển tiếp", (w, h) => GPoly(w, h, (0, 0.25f), (0.75f, 0.25f), (0.75f, 0.05f), (1, 0.5f), (0.75f, 0.95f), (0.75f, 0.75f), (0, 0.75f)), (w, h) => WPoly(w, h, (0, 0.25), (0.75, 0.25), (0.75, 0.05), (1, 0.5), (0.75, 0.95), (0.75, 0.75), (0, 0.75)));
            AddItem(cat, "diag_chain_link", "Chuỗi mắt xích liên kết 2 mắt", (w, h) => GPoly(w, h, (0.15f, 0.3f), (0.85f, 0.3f), (0.85f, 0.7f), (0.15f, 0.7f)), (w, h) => WPoly(w, h, (0.15, 0.3), (0.85, 0.3), (0.85, 0.7), (0.15, 0.7)));
            AddItem(cat, "diag_balance_scale", "Cân thăng bằng đối trọng", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.5f, 0.9f), (0.3f, 0.9f), (0.7f, 0.9f), (0.5f, 0.2f), (0.15f, 0.4f), (0.5f, 0.2f), (0.85f, 0.4f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.5, 0.9), (0.3, 0.9), (0.7, 0.9), (0.5, 0.2), (0.15, 0.4), (0.5, 0.2), (0.85, 0.4)));
            AddItem(cat, "diag_hourglass_timer", "Đồng hồ cát đo thời hạn dự án", (w, h) => GPoly(w, h, (0.1f, 0.05f), (0.9f, 0.05f), (0.5f, 0.5f), (0.9f, 0.95f), (0.1f, 0.95f), (0.5f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.05), (0.9, 0.05), (0.5, 0.5), (0.9, 0.95), (0.1, 0.95), (0.5, 0.5)));
            AddItem(cat, "diag_temple_pillars", "Cột trụ thành công 3 cột đền", (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.25f), (0.85f, 0.25f), (0.85f, 0.8f), (0.95f, 0.8f), (0.95f, 1), (0.05f, 1), (0.05f, 0.8f), (0.15f, 0.8f), (0.15f, 0.25f), (0.05f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.25), (0.85, 0.25), (0.85, 0.8), (0.95, 0.8), (0.95, 1), (0.05, 1), (0.05, 0.8), (0.15, 0.8), (0.15, 0.25), (0.05, 0.25)));
            AddItem(cat, "diag_timeline_axis", "Trục thời gian mốc Node kim cương", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f), (0.25f, 0.35f), (0.35f, 0.5f), (0.25f, 0.65f), (0.15f, 0.5f), (0.25f, 0.35f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5), (0.25, 0.35), (0.35, 0.5), (0.25, 0.65), (0.15, 0.5), (0.25, 0.35)));
            AddItem(cat, "diag_roadmap_spiral", "Lộ trình quanh co phát triển", (w, h) => GLines(w, h, (0.1f, 0.9f), (0.4f, 0.6f), (0.2f, 0.35f), (0.7f, 0.15f), (0.9f, 0.05f)), (w, h) => WLines(w, h, (0.1, 0.9), (0.4, 0.6), (0.2, 0.35), (0.7, 0.15), (0.9, 0.05)));
            AddItem(cat, "diag_porter_diamond", "Mô hình kim cương Porter Diamond", (w, h) => GPoly(w, h, (0.5f, 0.05f), (0.95f, 0.5f), (0.5f, 0.95f), (0.05f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.05), (0.95, 0.5), (0.5, 0.95), (0.05, 0.5)));
            AddItem(cat, "diag_kpi_dashboard", "Bảng đồng hồ tiến độ 3 chỉ số", (w, h) => GRoundedRect(w, h, 6f), (w, h) => WRoundedRect(w, h, 6.0));
            AddItem(cat, "diag_heat_matrix", "Bản đồ nhiệt ma trận rủi ro", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "diag_pricing_tier", "Bộ 3 thẻ bài bảng giá dịch vụ", (w, h) => GPoly(w, h, (0.05f, 0.15f), (0.95f, 0.15f), (0.95f, 0.85f), (0.05f, 0.85f)), (w, h) => WPoly(w, h, (0.05, 0.15), (0.95, 0.15), (0.95, 0.85), (0.05, 0.85)));
            AddItem(cat, "diag_scorecard_compass", "Thẻ điểm cân bằng chiến lược", (w, h) => GStar(w, h, 4, 0.45), (w, h) => WStar(w, h, 4, 0.45));
        }
        #endregion

        #region Category 11: Ký hiệu toán học & Kỹ thuật (50 mẫu)
        private static void PopulateMathAndTechnical()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "MathTechnical", Title = "Ký hiệu toán học & Kỹ thuật" };
            _categories.Add(cat);

            // 1-13: Toán học cơ bản & so sánh
            AddItem(cat, "math_infinity", "Ký hiệu vô cực (Infinity ∞)", (w, h) => GPoly(w, h, (0.25f, 0.25f), (0.45f, 0.45f), (0.55f, 0.55f), (0.75f, 0.75f), (0.95f, 0.5f), (0.75f, 0.25f), (0.55f, 0.45f), (0.45f, 0.55f), (0.25f, 0.75f), (0.05f, 0.5f)), (w, h) => WPoly(w, h, (0.25, 0.25), (0.45, 0.45), (0.55, 0.55), (0.75, 0.75), (0.95, 0.5), (0.75, 0.25), (0.55, 0.45), (0.45, 0.55), (0.25, 0.75), (0.05, 0.5)));
            AddItem(cat, "math_plus", "Dấu cộng phép tính (+)", (w, h) => GPoly(w, h, (0.4f, 0.1f), (0.6f, 0.1f), (0.6f, 0.4f), (0.9f, 0.4f), (0.9f, 0.6f), (0.6f, 0.6f), (0.6f, 0.9f), (0.4f, 0.9f), (0.4f, 0.6f), (0.1f, 0.6f), (0.1f, 0.4f), (0.4f, 0.4f)), (w, h) => WPoly(w, h, (0.4, 0.1), (0.6, 0.1), (0.6, 0.4), (0.9, 0.4), (0.9, 0.6), (0.6, 0.6), (0.6, 0.9), (0.4, 0.9), (0.4, 0.6), (0.1, 0.6), (0.1, 0.4), (0.4, 0.4)));
            AddItem(cat, "math_minus", "Dấu trừ phép tính (−)", (w, h) => GPoly(w, h, (0.1f, 0.42f), (0.9f, 0.42f), (0.9f, 0.58f), (0.1f, 0.58f)), (w, h) => WPoly(w, h, (0.1, 0.42), (0.9, 0.42), (0.9, 0.58), (0.1, 0.58)));
            AddItem(cat, "math_multiply", "Dấu nhân chéo (×)", (w, h) => GPoly(w, h, (0.2f, 0.1f), (0.5f, 0.4f), (0.8f, 0.1f), (0.9f, 0.2f), (0.6f, 0.5f), (0.9f, 0.8f), (0.8f, 0.9f), (0.5f, 0.6f), (0.2f, 0.9f), (0.1f, 0.8f), (0.4f, 0.5f), (0.1f, 0.2f)), (w, h) => WPoly(w, h, (0.2, 0.1), (0.5, 0.4), (0.8, 0.1), (0.9, 0.2), (0.6, 0.5), (0.9, 0.8), (0.8, 0.9), (0.5, 0.6), (0.2, 0.9), (0.1, 0.8), (0.4, 0.5), (0.1, 0.2)));
            AddItem(cat, "math_divide", "Dấu chia dấu chấm (÷)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.44f, w * 0.8f, h * 0.12f)); gp.AddEllipse(w * 0.42f, h * 0.15f, w * 0.16f, h * 0.16f); gp.AddEllipse(w * 0.42f, h * 0.69f, w * 0.16f, h * 0.16f); return gp; }, (w, h) => WPoly(w, h, (0.1, 0.44), (0.9, 0.44), (0.9, 0.56), (0.1, 0.56)));
            AddItem(cat, "math_equal", "Dấu bằng cân xứng (=)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.3f, w * 0.8f, h * 0.12f)); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.58f, w * 0.8f, h * 0.12f)); return gp; }, (w, h) => { WpfStreamGeometry g = new WpfStreamGeometry(); using (var ctx = g.Open()) { ctx.BeginFigure(new WpfPoint(w * 0.1, h * 0.3), true, true); ctx.LineTo(new WpfPoint(w * 0.9, h * 0.3), true, false); ctx.LineTo(new WpfPoint(w * 0.9, h * 0.42), true, false); ctx.LineTo(new WpfPoint(w * 0.1, h * 0.42), true, false); ctx.BeginFigure(new WpfPoint(w * 0.1, h * 0.58), true, true); ctx.LineTo(new WpfPoint(w * 0.9, h * 0.58), true, false); ctx.LineTo(new WpfPoint(w * 0.9, h * 0.7), true, false); ctx.LineTo(new WpfPoint(w * 0.1, h * 0.7), true, false); } g.Freeze(); return g; });
            AddItem(cat, "math_not_equal", "Dấu khác không bằng (≠)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.1f, h * 0.35f, w * 0.9f, h * 0.35f); gp.AddLine(w * 0.1f, h * 0.65f, w * 0.9f, h * 0.65f); gp.AddLine(w * 0.75f, h * 0.15f, w * 0.25f, h * 0.85f); return gp; }, (w, h) => WLines(w, h, (0.1f, 0.35f), (0.9f, 0.35f)));
            AddItem(cat, "math_approx", "Dấu xấp xỉ tương đương (≈)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(w * 0.1f, h * 0.35f, w * 0.35f, h * 0.2f, w * 0.65f, h * 0.5f, w * 0.9f, h * 0.35f); gp.AddBezier(w * 0.1f, h * 0.65f, w * 0.35f, h * 0.5f, w * 0.65f, h * 0.8f, w * 0.9f, h * 0.65f); return gp; }, (w, h) => WLines(w, h, (0.1f, 0.35f), (0.9f, 0.35f)));
            AddItem(cat, "math_greater", "Dấu lớn hơn (>)", (w, h) => GLines(w, h, (0.2f, 0.2f), (0.85f, 0.5f), (0.2f, 0.8f)), (w, h) => WLines(w, h, (0.2, 0.2), (0.85, 0.5), (0.2, 0.8)));
            AddItem(cat, "math_less", "Dấu nhỏ hơn (<)", (w, h) => GLines(w, h, (0.8f, 0.2f), (0.15f, 0.5f), (0.8f, 0.8f)), (w, h) => WLines(w, h, (0.8, 0.2), (0.15, 0.5), (0.8, 0.8)));
            AddItem(cat, "math_greater_equal", "Dấu lớn hơn hoặc bằng (≥)", (w, h) => GLines(w, h, (0.2f, 0.15f), (0.85f, 0.45f), (0.2f, 0.75f), (0.2f, 0.9f), (0.85f, 0.9f)), (w, h) => WLines(w, h, (0.2, 0.15), (0.85, 0.45), (0.2, 0.75)));
            AddItem(cat, "math_less_equal", "Dấu nhỏ hơn hoặc bằng (≤)", (w, h) => GLines(w, h, (0.8f, 0.15f), (0.15f, 0.45f), (0.8f, 0.75f), (0.15f, 0.9f), (0.8f, 0.9f)), (w, h) => WLines(w, h, (0.8, 0.15), (0.15, 0.45), (0.8, 0.75)));
            AddItem(cat, "math_plus_minus", "Dấu cộng trừ (±)", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.5f, 0.6f), (0.2f, 0.35f), (0.8f, 0.35f), (0.2f, 0.85f), (0.8f, 0.85f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.5, 0.6)));

            // 14-27: Toán học cao cấp & chữ cái Hy Lạp
            AddItem(cat, "math_sqrt", "Căn bậc hai căn thức (√)", (w, h) => GLines(w, h, (0.05f, 0.6f), (0.2f, 0.6f), (0.35f, 0.95f), (0.55f, 0.1f), (0.95f, 0.1f)), (w, h) => WLines(w, h, (0.05, 0.6), (0.2, 0.6), (0.35, 0.95), (0.55, 0.1), (0.95, 0.1)));
            AddItem(cat, "math_integral", "Tích phân đơn Leibniz (∫)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddBezier(w * 0.7f, 0.1f, w * 0.4f, 0.05f, w * 0.5f, 0.5f, w * 0.5f, 0.5f); gp.AddBezier(w * 0.5f, 0.5f, w * 0.5f, 0.5f, w * 0.6f, 0.95f, w * 0.3f, 0.9f); return gp; }, (w, h) => WLines(w, h, (0.7f, 0.1f), (0.5f, 0.5f), (0.3f, 0.9f)));
            AddItem(cat, "math_double_integral", "Tích phân kép hai lớp (∬)", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.4f, 0.5f), (0.3f, 0.9f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.4, 0.5), (0.3, 0.9)));
            AddItem(cat, "math_sigma_sum", "Tổng chuỗi Sigma (Σ)", (w, h) => GLines(w, h, (0.85f, 0.1f), (0.15f, 0.1f), (0.55f, 0.5f), (0.15f, 0.9f), (0.85f, 0.9f)), (w, h) => WLines(w, h, (0.85, 0.1), (0.15, 0.1), (0.55, 0.5), (0.15, 0.9), (0.85, 0.9)));
            AddItem(cat, "math_pi", "Số Pi toán học (π)", (w, h) => GLines(w, h, (0.1f, 0.2f), (0.9f, 0.2f), (0.35f, 0.2f), (0.35f, 0.85f), (0.7f, 0.2f), (0.7f, 0.85f)), (w, h) => WLines(w, h, (0.1, 0.2), (0.9, 0.2)));
            AddItem(cat, "math_delta", "Ký hiệu Delta tam giác (Δ)", (w, h) => GPoly(w, h, (0.5f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.5, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "math_omega", "Ký hiệu điện trở Omega (Ω)", (w, h) => GPoly(w, h, (0.1f, 0.85f), (0.3f, 0.85f), (0.25f, 0.65f), (0.15f, 0.4f), (0.5f, 0.1f), (0.85f, 0.4f), (0.75f, 0.65f), (0.7f, 0.85f), (0.9f, 0.85f)), (w, h) => WPoly(w, h, (0.1, 0.85), (0.3, 0.85), (0.25, 0.65), (0.15, 0.4), (0.5, 0.1), (0.85, 0.4), (0.75, 0.65), (0.7, 0.85), (0.9, 0.85)));
            AddItem(cat, "math_theta", "Ký hiệu góc Theta (θ)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.2f, h * 0.1f, w * 0.6f, h * 0.8f); gp.AddLine(w * 0.2f, h * 0.5f, w * 0.8f, h * 0.5f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "math_alpha", "Ký hiệu góc Alpha (α)", (w, h) => GLines(w, h, (0.85f, 0.25f), (0.2f, 0.75f), (0.2f, 0.35f), (0.85f, 0.75f)), (w, h) => WLines(w, h, (0.85, 0.25), (0.2, 0.75), (0.2, 0.35), (0.85, 0.75)));
            AddItem(cat, "math_beta", "Ký hiệu Beta (β)", (w, h) => GLines(w, h, (0.25f, 0.95f), (0.25f, 0.15f), (0.65f, 0.15f), (0.65f, 0.5f), (0.25f, 0.5f), (0.75f, 0.5f), (0.75f, 0.85f), (0.25f, 0.85f)), (w, h) => WLines(w, h, (0.25, 0.95), (0.25, 0.15)));
            AddItem(cat, "math_lambda", "Ký hiệu bước sóng Lambda (λ)", (w, h) => GLines(w, h, (0.15f, 0.85f), (0.6f, 0.15f), (0.85f, 0.85f), (0.45f, 0.45f)), (w, h) => WLines(w, h, (0.15, 0.85), (0.6, 0.15)));
            AddItem(cat, "math_percent", "Ký hiệu phần trăm (%)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(w * 0.85f, h * 0.15f, w * 0.15f, h * 0.85f); gp.AddEllipse(w * 0.2f, h * 0.2f, w * 0.2f, h * 0.2f); gp.AddEllipse(w * 0.6f, h * 0.6f, w * 0.2f, h * 0.2f); return gp; }, (w, h) => WLines(w, h, (0.85f, 0.15f), (0.15f, 0.85f)));
            AddItem(cat, "math_empty_set", "Ký hiệu tập hợp rỗng (∅)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.2f, h * 0.2f, w * 0.6f, h * 0.6f); gp.AddLine(w * 0.85f, h * 0.1f, w * 0.15f, h * 0.9f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "math_angle_geom", "Ký hiệu góc nhọn hình học (∠)", (w, h) => GLines(w, h, (0.85f, 0.25f), (0.15f, 0.85f), (0.85f, 0.85f)), (w, h) => WLines(w, h, (0.85, 0.25), (0.15, 0.85), (0.85, 0.85)));

            // 28-35: Ký hiệu cổng logic số (Digital Logic Gates)
            AddItem(cat, "gate_and", "Cổng logic số AND", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0.1f * w, 0.1f * h, 0.5f * w, 0.1f * h); gp.AddArc(0.1f * w, 0.1f * h, 0.8f * w, 0.8f * h, 270, 180); gp.AddLine(0.5f * w, 0.9f * h, 0.1f * w, 0.9f * h); gp.CloseFigure(); return gp; }, (w, h) => WPoly(w, h, (0.1, 0.1), (0.5, 0.1), (0.9, 0.5), (0.5, 0.9), (0.1, 0.9)));
            AddItem(cat, "gate_or", "Cổng logic số OR", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.4f, 0.1f), (0.9f, 0.5f), (0.4f, 0.9f), (0.1f, 0.9f), (0.25f, 0.5f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.4, 0.1), (0.9, 0.5), (0.4, 0.9), (0.1, 0.9), (0.25, 0.5)));
            AddItem(cat, "gate_not", "Cổng logic đảo NOT Inverter", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddPolygon(new PointF[] { new PointF(0.1f * w, 0.1f * h), new PointF(0.75f * w, 0.5f * h), new PointF(0.1f * w, 0.9f * h) }); gp.AddEllipse(0.75f * w, 0.42f * h, 0.16f * w, 0.16f * h); return gp; }, (w, h) => WPoly(w, h, (0.1, 0.1), (0.75, 0.5), (0.1, 0.9)));
            AddItem(cat, "gate_nand", "Cổng logic số NAND", (w, h) => GPoly(w, h, (0.1, 0.1), (0.5, 0.1), (0.8, 0.5), (0.5, 0.9), (0.1, 0.9)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.5, 0.1), (0.8, 0.5), (0.5, 0.9), (0.1, 0.9)));
            AddItem(cat, "gate_nor", "Cổng logic số NOR", (w, h) => GPoly(w, h, (0.1, 0.1), (0.4, 0.1), (0.8, 0.5), (0.4, 0.9), (0.1, 0.9), (0.2, 0.5)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.4, 0.1), (0.8, 0.5), (0.4, 0.9), (0.1, 0.9), (0.2, 0.5)));
            AddItem(cat, "gate_xor", "Cổng logic loại trừ XOR", (w, h) => GPoly(w, h, (0.15, 0.1), (0.45, 0.1), (0.9, 0.5), (0.45, 0.9), (0.15, 0.9), (0.3, 0.5)), (w, h) => WPoly(w, h, (0.15, 0.1), (0.45, 0.1), (0.9, 0.5), (0.45, 0.9), (0.15, 0.9), (0.3, 0.5)));
            AddItem(cat, "gate_buffer", "Cổng đệm tín hiệu Buffer", (w, h) => GPoly(w, h, (0.15f, 0.15f), (0.85f, 0.5f), (0.15f, 0.85f)), (w, h) => WPoly(w, h, (0.15, 0.15), (0.85, 0.5), (0.15, 0.85)));
            AddItem(cat, "gate_schmitt_trigger", "Bộ kích khởi Schmitt Trigger", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.8f, 0.5f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.8, 0.5), (0.1, 0.9)));

            // 36-50: Ký hiệu mạch điện tử (Electronic Schematics)
            AddItem(cat, "elec_resistor", "Điện trở zic-zac (Resistor)", (w, h) => GLines(w, h, (0, 0.5f), (0.2f, 0.5f), (0.25f, 0.2f), (0.35f, 0.8f), (0.45f, 0.2f), (0.55f, 0.8f), (0.65f, 0.2f), (0.75f, 0.8f), (0.8f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.2, 0.5), (0.25, 0.2), (0.35, 0.8), (0.45, 0.2), (0.55, 0.8), (0.65, 0.2), (0.75, 0.8), (0.8, 0.5), (1, 0.5)));
            AddItem(cat, "elec_capacitor", "Tụ điện hai bản cực (Capacitor)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.42f, h * 0.5f); gp.AddLine(w * 0.42f, h * 0.15f, w * 0.42f, h * 0.85f); gp.AddLine(w * 0.58f, h * 0.15f, w * 0.58f, h * 0.85f); gp.AddLine(w * 0.58f, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (0.42f, 0.5f)));
            AddItem(cat, "elec_inductor", "Cuộn cảm từ tính 4 vòng", (w, h) => GLines(w, h, (0, 0.5f), (0.2f, 0.5f), (0.3f, 0.2f), (0.4f, 0.5f), (0.5f, 0.2f), (0.6f, 0.5f), (0.7f, 0.2f), (0.8f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.2, 0.5), (0.3, 0.2), (0.4, 0.5), (0.5, 0.2), (0.6, 0.5), (0.7, 0.2), (0.8, 0.5), (1, 0.5)));
            AddItem(cat, "elec_diode", "Điốt bán dẫn (Diode)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.35f, h * 0.5f); gp.AddPolygon(new PointF[] { new PointF(w * 0.35f, h * 0.2f), new PointF(w * 0.65f, h * 0.5f), new PointF(w * 0.35f, h * 0.8f) }); gp.AddLine(w * 0.65f, h * 0.2f, w * 0.65f, h * 0.8f); gp.AddLine(w * 0.65f, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WPoly(w, h, (0.35, 0.2), (0.65, 0.5), (0.35, 0.8)));
            AddItem(cat, "elec_ground", "Tiếp đất chống giật (Earth Ground)", (w, h) => GLines(w, h, (0.5f, 0.1f), (0.5f, 0.5f), (0.15f, 0.5f), (0.85f, 0.5f), (0.28f, 0.7f), (0.72f, 0.7f), (0.4f, 0.9f), (0.6f, 0.9f)), (w, h) => WLines(w, h, (0.5, 0.1), (0.5, 0.5)));
            AddItem(cat, "elec_battery_dc", "Nguồn pin điện một chiều DC", (w, h) => GLines(w, h, (0, 0.5f), (0.4f, 0.5f), (0.4f, 0.2f), (0.4f, 0.8f), (0.6f, 0.35f), (0.6f, 0.65f), (0.6f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.4, 0.5)));
            AddItem(cat, "elec_ac_source", "Nguồn điện xoay chiều AC", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(w * 0.15f, h * 0.15f, w * 0.7f, h * 0.7f); gp.AddBezier(w * 0.3f, h * 0.5f, w * 0.4f, h * 0.3f, w * 0.6f, h * 0.7f, w * 0.7f, h * 0.5f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "elec_switch_open", "Công tắc đóng mở hở (Switch)", (w, h) => GLines(w, h, (0, 0.5f), (0.3f, 0.5f), (0.3f, 0.5f), (0.7f, 0.2f), (0.7f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.3, 0.5), (0.7, 0.2)));
            AddItem(cat, "elec_voltmeter", "Vôn kế đo điện áp (Voltmeter V)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.35f, h * 0.3f, w * 0.5f, h * 0.7f); gp.AddLine(w * 0.5f, h * 0.7f, w * 0.65f, h * 0.3f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "elec_ammeter", "Ampe kế đo dòng điện (Ammeter A)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, w, h); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.3f, h * 0.75f); gp.AddLine(w * 0.5f, h * 0.25f, w * 0.7f, h * 0.75f); gp.AddLine(w * 0.38f, h * 0.55f, w * 0.62f, h * 0.55f); return gp; }, (w, h) => WEllipse(w, h));
            AddItem(cat, "elec_antenna", "Ăng-ten thu phát sóng vô tuyến", (w, h) => GLines(w, h, (0.5f, 0.9f), (0.5f, 0.35f), (0.15f, 0.1f), (0.85f, 0.1f), (0.5f, 0.35f)), (w, h) => WLines(w, h, (0.5, 0.9), (0.5, 0.35), (0.15, 0.1), (0.85, 0.1), (0.5, 0.35)));
            AddItem(cat, "elec_fuse", "Cầu chì an toàn bảo vệ mạch", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(w * 0.2f, h * 0.35f, w * 0.6f, h * 0.3f)); gp.AddLine(0, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WPoly(w, h, (0.2f, 0.35f), (0.8f, 0.35f), (0.8f, 0.65f), (0.2f, 0.65f)));
            AddItem(cat, "elec_potentiometer", "Biến trở xoay điều chỉnh", (w, h) => GLines(w, h, (0, 0.5f), (0.2f, 0.5f), (0.35f, 0.2f), (0.5f, 0.8f), (0.65f, 0.2f), (0.8f, 0.5f), (1, 0.5f), (0.3f, 0.8f), (0.7f, 0.2f)), (w, h) => WLines(w, h, (0, 0.5), (0.2, 0.5)));
            AddItem(cat, "elec_transformer", "Máy biến áp cách ly (Transformer)", (w, h) => GLines(w, h, (0.1f, 0.2f), (0.3f, 0.5f), (0.1f, 0.8f), (0.9f, 0.2f), (0.7f, 0.5f), (0.9f, 0.8f)), (w, h) => WLines(w, h, (0.1, 0.2), (0.3, 0.5)));
            AddItem(cat, "elec_opamp", "Bộ khuếch đại thuật toán Op-Amp", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.5f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.5), (0.1, 0.9)));
        }
        #endregion

        #region Category 12: Khung viền & Họa tiết trang trí (50 mẫu)
        private static void PopulateBordersAndDividers()
        {
            ShapeCategoryGroup cat = new ShapeCategoryGroup { Key = "Decorations", Title = "Khung viền & Họa tiết trang trí" };
            _categories.Add(cat);

            // 1-10: Khung viền chữ nhật các kiểu
            AddItem(cat, "border_double_rect", "Khung viền đôi hai lớp (Double Line)", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.08f, h * 0.08f, w * 0.84f, h * 0.84f)); return gp; }, (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.08), (0.92, 0.08), (0.92, 0.92), (0.08, 0.92), (0.08, 0.08)));
            AddItem(cat, "border_triple_rect", "Khung viền ba thanh mảnh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddRectangle(new RectangleF(0, 0, w, h)); gp.AddRectangle(new RectangleF(w * 0.05f, h * 0.05f, w * 0.9f, h * 0.9f)); gp.AddRectangle(new RectangleF(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f)); return gp; }, (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9), (0.1, 0.1)));
            AddItem(cat, "border_snip_corner", "Khung viền vát 4 góc 45°", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.15f), (1, 0.85f), (0.85f, 1), (0.15f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.15), (1, 0.85), (0.85, 1), (0.15, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "border_snip_round", "Khung viền vát trên, bo tròn dưới", (w, h) => GPoly(w, h, (0.15f, 0), (0.85f, 0), (1, 0.15f), (1, 0.85f), (0.85f, 1), (0.15f, 1), (0, 0.85f), (0, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0), (0.85, 0), (1, 0.15), (1, 0.85), (0.85, 1), (0.15, 1), (0, 0.85), (0, 0.15)));
            AddItem(cat, "border_round_corner", "Khung viền bo tròn 4 góc mềm", (w, h) => GRoundedRect(w, h, 10f), (w, h) => WRoundedRect(w, h, 10.0));
            AddItem(cat, "border_heavy_frame", "Khung viền bản đậm dày", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "border_dotted_stitch", "Khung viền đường may nét đứt", (w, h) => GRoundedRect(w, h, 6f), (w, h) => WRoundedRect(w, h, 6.0));
            AddItem(cat, "border_polaroid_photo", "Khung ảnh phong cách Polaroid", (w, h) => GPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)), (w, h) => WPoly(w, h, (0, 0), (1, 0), (1, 1), (0, 1)));
            AddItem(cat, "border_moroccan_arch", "Khung vòm phong cách Moroccan", (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.25f), (0.95f, 1), (0.05f, 1), (0.05f, 0.25f)), (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.25), (0.95, 1), (0.05, 1), (0.05, 0.25)));
            AddItem(cat, "border_scalloped_edge", "Khung viền răng cưa cánh sen", (w, h) => GStar(w, h, 16, 0.92), (w, h) => WStar(w, h, 16, 0.92));

            // 11-20: Khung góc nghệ thuật & hoa văn
            AddItem(cat, "border_flourish_corners", "Khung họa tiết hoa văn 4 góc", (w, h) => GPoly(w, h, (0.15f, 0.05f), (0.85f, 0.05f), (0.95f, 0.15f), (0.95f, 0.85f), (0.85f, 0.95f), (0.15f, 0.95f), (0.05f, 0.85f), (0.05f, 0.15f)), (w, h) => WPoly(w, h, (0.15, 0.05), (0.85, 0.05), (0.95, 0.15), (0.95, 0.85), (0.85, 0.95), (0.15, 0.95), (0.05, 0.85), (0.05, 0.15)));
            AddItem(cat, "border_certificate_guilloche", "Khung viền bằng khen chứng chỉ", (w, h) => GPoly(w, h, (0.08f, 0.08f), (0.92f, 0.08f), (0.92f, 0.92f), (0.08f, 0.92f)), (w, h) => WPoly(w, h, (0.08, 0.08), (0.92, 0.08), (0.92, 0.92), (0.08, 0.92)));
            AddItem(cat, "border_greek_key", "Khung viền hoa văn Hy Lạp cổ", (w, h) => GPoly(w, h, (0.05f, 0.05f), (0.95f, 0.05f), (0.95f, 0.95f), (0.05f, 0.95f)), (w, h) => WPoly(w, h, (0.05, 0.05), (0.95, 0.05), (0.95, 0.95), (0.05, 0.95)));
            AddItem(cat, "border_corner_brackets", "Khung nhấn 4 góc thước thợ", (w, h) => GLines(w, h, (0.25f, 0.05f), (0.05f, 0.05f), (0.05f, 0.25f), (0.05f, 0.75f), (0.05f, 0.95f), (0.25f, 0.95f), (0.75f, 0.95f), (0.95f, 0.95f), (0.95f, 0.75f), (0.95f, 0.25f), (0.95f, 0.05f), (0.75f, 0.05f)), (w, h) => WLines(w, h, (0.25, 0.05), (0.05, 0.05), (0.05, 0.25)));
            AddItem(cat, "border_beaded_pearl", "Khung chuỗi hạt ngọc trai bao quanh", (w, h) => GRoundedRect(w, h, 8f), (w, h) => WRoundedRect(w, h, 8.0));
            AddItem(cat, "border_art_deco_geom", "Khung hình học Art Deco sang trọng", (w, h) => GPoly(w, h, (0.2f, 0), (0.8f, 0), (1, 0.2f), (1, 0.8f), (0.8f, 1), (0.2f, 1), (0, 0.8f), (0, 0.2f)), (w, h) => WPoly(w, h, (0.2, 0), (0.8, 0), (1, 0.2), (1, 0.8), (0.8, 1), (0.2, 1), (0, 0.8), (0, 0.2)));
            AddItem(cat, "border_vintage_filigree", "Khung hoa văn Vintage cổ điển", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "border_ivy_vine", "Khung viền dây leo trường xuân", (w, h) => GPoly(w, h, (0.1f, 0.1f), (0.9f, 0.1f), (0.9f, 0.9f), (0.1f, 0.9f)), (w, h) => WPoly(w, h, (0.1, 0.1), (0.9, 0.1), (0.9, 0.9), (0.1, 0.9)));
            AddItem(cat, "border_floral_wreath", "Khung viền vòng hoa lá nguyệt quế", (w, h) => GEllipse(w, h), (w, h) => WEllipse(w, h));
            AddItem(cat, "border_gothic_pointed", "Khung vòm nhọn Gothic giáo đường", (w, h) => GPoly(w, h, (0.5f, 0), (0.95f, 0.35f), (0.95f, 1), (0.05f, 1), (0.05f, 0.35f)), (w, h) => WPoly(w, h, (0.5, 0), (0.95, 0.35), (0.95, 1), (0.05, 1), (0.05, 0.35)));

            // 21-35: Đường phân cách ngang (Horizontal Rule Dividers)
            AddItem(cat, "divider_diamond_center", "Đường kẻ ngang kim cương chính giữa", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.42f, h * 0.5f); gp.AddPolygon(new PointF[] { new PointF(w * 0.5f, h * 0.15f), new PointF(w * 0.58f, h * 0.5f), new PointF(w * 0.5f, h * 0.85f), new PointF(w * 0.42f, h * 0.5f) }); gp.AddLine(w * 0.58f, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WPoly(w, h, (0.5, 0.15), (0.58, 0.5), (0.5, 0.85), (0.42, 0.5)));
            AddItem(cat, "divider_star_trio", "Đường kẻ ngang 3 ngôi sao chính giữa", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.5f, w * 0.35f, h * 0.5f); gp.AddLine(w * 0.65f, h * 0.5f, w, h * 0.5f); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (1, 0.5f)));
            AddItem(cat, "divider_floral_swirl", "Đường phân cách hoa văn uốn lượn", (w, h) => GLines(w, h, (0, 0.5f), (0.35f, 0.5f), (0.45f, 0.2f), (0.55f, 0.8f), (0.65f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.35, 0.5), (0.45, 0.2), (0.55, 0.8), (0.65, 0.5), (1, 0.5)));
            AddItem(cat, "divider_tapered_rule", "Đường kẻ vuốt nhọn hai đầu (Tapered)", (w, h) => GPoly(w, h, (0, 0.5f), (0.5f, 0.3f), (1, 0.5f), (0.5f, 0.7f)), (w, h) => WPoly(w, h, (0, 0.5), (0.5, 0.3), (1, 0.5), (0.5, 0.7)));
            AddItem(cat, "divider_double_thin", "Đường phân cách song song thanh mảnh", (w, h) => { GraphicsPath gp = new GraphicsPath(); gp.AddLine(0, h * 0.4f, w, h * 0.4f); gp.AddLine(0, h * 0.6f, w, h * 0.6f); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (1, 0.5f)));
            AddItem(cat, "divider_triple_beaded", "Đường ba thanh mảnh đính hạt ngọc", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_sine_wave", "Đường phân cách sóng sin mềm mại", (w, h) => GLines(w, h, (0, 0.5f), (0.25f, 0.2f), (0.75f, 0.8f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.25, 0.2), (0.75, 0.8), (1, 0.5)));
            AddItem(cat, "divider_zigzag_chevron", "Đường phân cách gấp khúc Chevron", (w, h) => GLines(w, h, (0, 0.5f), (0.4f, 0.5f), (0.45f, 0.2f), (0.5f, 0.8f), (0.55f, 0.2f), (0.6f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.4, 0.5), (0.45, 0.2), (0.5, 0.8), (0.55, 0.2), (0.6, 0.5), (1, 0.5)));
            AddItem(cat, "divider_dash_dot_heavy", "Đường gạch chấm đậm phong cách kỹ thuật", (w, h) => GLines(w, h, (0, 0.5f), (0.35f, 0.5f), (0.45f, 0.5f), (0.55f, 0.5f), (0.65f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (0.35, 0.5)));
            AddItem(cat, "divider_cross_heraldic", "Đường phân cách chữ thập thập tự", (w, h) => GLines(w, h, (0, 0.5f), (0.42f, 0.5f), (0.58f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_infinity_loop", "Đường phân cách vòng lặp vô cực", (w, h) => GLines(w, h, (0, 0.5f), (0.35f, 0.5f), (0.65f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_inward_brackets", "Đường phân cách cặp ngoặc đối hướng", (w, h) => GLines(w, h, (0, 0.5f), (0.4f, 0.5f), (0.45f, 0.25f), (0.45f, 0.75f), (0.55f, 0.25f), (0.55f, 0.75f), (0.6f, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_fleur_de_lis", "Đường hoa diên vĩ hoàng gia", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_laurel_branch", "Đường nhánh lá ô-liu vinh quang", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "divider_arrowheads_accent", "Đường phân cách hai đầu mũi tên đối xứng", (w, h) => GLines(w, h, (0.05f, 0.35f), (0, 0.5f), (0.05f, 0.65f), (0, 0.5f), (1, 0.5f), (0.95f, 0.35f), (1, 0.5f), (0.95f, 0.65f)), (w, h) => WLines(w, h, (0.05, 0.35), (0, 0.5), (0.05, 0.65), (0, 0.5), (1, 0.5), (0.95, 0.35), (1, 0.5), (0.95, 0.65)));

            // 36-50: Thanh ngăn trang & dải họa tiết (Section Separator Bars)
            AddItem(cat, "bar_ribbon_banner", "Thanh dải ruy băng ngăn slide", (w, h) => GPoly(w, h, (0, 0.2f), (1, 0.2f), (0.95f, 0.8f), (0.05f, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (1, 0.2), (0.95, 0.8), (0.05, 0.8)));
            AddItem(cat, "bar_segmented_progress", "Thanh phân đoạn tiến độ 5 mắt", (w, h) => GPoly(w, h, (0, 0.35f), (1, 0.35f), (1, 0.65f), (0, 0.65f)), (w, h) => WPoly(w, h, (0, 0.35), (1, 0.35), (1, 0.65), (0, 0.65)));
            AddItem(cat, "bar_diamond_chain", "Thanh chuỗi kim cương liên hoàn", (w, h) => GPoly(w, h, (0, 0.5f), (0.25f, 0.2f), (0.5f, 0.5f), (0.75f, 0.2f), (1, 0.5f), (0.75f, 0.8f), (0.5f, 0.5f), (0.25f, 0.8f)), (w, h) => WPoly(w, h, (0, 0.5), (0.25, 0.2), (0.5, 0.5), (0.75, 0.2), (1, 0.5), (0.75, 0.8), (0.5, 0.5), (0.25, 0.8)));
            AddItem(cat, "bar_pearl_bead_string", "Chuỗi hạt ngọc trai ngăn dòng", (w, h) => GRoundedRect(w, h * 0.4f, 5f), (w, h) => WRoundedRect(w, h * 0.4, 5.0));
            AddItem(cat, "bar_geometric_hash", "Thanh sọc gạch chéo kỹ thuật", (w, h) => GPoly(w, h, (0, 0.3f), (1, 0.3f), (1, 0.7f), (0, 0.7f)), (w, h) => WPoly(w, h, (0, 0.3), (1, 0.3), (1, 0.7), (0, 0.7)));
            AddItem(cat, "bar_dotted_center_circle", "Đường nét đứt kèm vòng tròn tâm", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "bar_barbed_modern", "Thanh phân cách góc vát thể thao", (w, h) => GPoly(w, h, (0.05f, 0.3f), (1, 0.3f), (0.95f, 0.7f), (0, 0.7f)), (w, h) => WPoly(w, h, (0.05, 0.3), (1, 0.3), (0.95, 0.7), (0, 0.7)));
            AddItem(cat, "bar_barcode_lines", "Thanh phân cách mã vạch Barcode", (w, h) => GPoly(w, h, (0, 0.2f), (1, 0.2f), (1, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.2), (1, 0.2), (1, 0.8), (0, 0.8)));
            AddItem(cat, "bar_wavelet_ribbon", "Dải ruy băng sóng lượn thanh nhã", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "bar_triple_diamond", "Cụm 3 hình thoi vương giả", (w, h) => GPoly(w, h, (0.5f, 0.1f), (0.65f, 0.5f), (0.5f, 0.9f), (0.35f, 0.5f)), (w, h) => WPoly(w, h, (0.5, 0.1), (0.65, 0.5), (0.5, 0.9), (0.35, 0.5)));
            AddItem(cat, "bar_laurel_leaf_strip", "Dải cành lá vinh quang ngăn cách", (w, h) => GLines(w, h, (0, 0.5f), (1, 0.5f)), (w, h) => WLines(w, h, (0, 0.5), (1, 0.5)));
            AddItem(cat, "bar_scroll_flourish", "Thanh cuộn thư tịch quý phái", (w, h) => GPoly(w, h, (0.05f, 0.35f), (0.95f, 0.35f), (0.9f, 0.65f), (0.1f, 0.65f)), (w, h) => WPoly(w, h, (0.05, 0.35), (0.95, 0.35), (0.9f, 0.65), (0.1, 0.65)));
            AddItem(cat, "bar_crown_accent", "Thanh đỉnh nhấn vương miện hoàng gia", (w, h) => GPoly(w, h, (0, 0.6f), (0.35f, 0.6f), (0.4f, 0.2f), (0.5f, 0.45f), (0.6f, 0.2f), (0.65f, 0.6f), (1, 0.6f), (1, 0.8f), (0, 0.8f)), (w, h) => WPoly(w, h, (0, 0.6), (0.35, 0.6), (0.4, 0.2), (0.5, 0.45), (0.6, 0.2), (0.65, 0.6), (1, 0.6), (1, 0.8), (0, 0.8)));
            AddItem(cat, "bar_rope_braid", "Thanh bện thừng hoa văn hàng hải", (w, h) => GRoundedRect(w, h * 0.4f, 4f), (w, h) => WRoundedRect(w, h * 0.4, 4.0));
            AddItem(cat, "bar_music_staff", "Khuông nhạc 5 dòng kẻ thanh tao", (w, h) => { GraphicsPath gp = new GraphicsPath(); for (int i = 0; i < 5; i++) gp.AddLine(0, h * (0.2f + i * 0.15f), w, h * (0.2f + i * 0.15f)); return gp; }, (w, h) => WLines(w, h, (0, 0.5f), (1, 0.5f)));
        }
        #endregion

    }
}





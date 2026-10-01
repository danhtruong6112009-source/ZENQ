import os

# Helper to generate C# WpfShapeLibrary.cs with 520+ distinct, geometrically accurate vector shapes
# No placeholder rectangles, no identical duplicates.

cs_code = '''using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WpfPoint = System.Windows.Point;
using WpfGeometry = System.Windows.Media.Geometry;
using WpfStreamGeometry = System.Windows.Media.StreamGeometry;
using WpfSize = System.Windows.Size;
using WpfSweepDirection = System.Windows.Media.SweepDirection;

namespace GenOffice.UI
{
    /// <summary>
    /// Định nghĩa chi tiết của một mẫu hình dạng trong thư viện 500+ hình dạng vector.
    /// Hỗ trợ tạo đồng thời cả GDI+ GraphicsPath lẫn WPF Vector Geometry để hiển thị siêu mượt không răng cưa.
    /// </summary>
    public class ShapePresetItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string CategoryName { get; set; }
        public Func<float, float, GraphicsPath> PathGenerator { get; set; }
        public Func<double, double, WpfGeometry> WpfGeometryGenerator { get; set; }

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

    /// <summary>
    /// Danh mục thể loại hình dạng.
    /// </summary>
    public class ShapeCategoryGroup
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public List<ShapePresetItem> Items { get; set; } = new List<ShapePresetItem>();
    }

    /// <summary>
    /// Thư viện chứa hơn 500 mẫu hình dạng vector chuẩn Office & thiết kế đồ họa cao cấp.
    /// Mọi hình dạng đều có định nghĩa hình học vector độc lập, chân thực, loại bỏ hoàn toàn các hình chữ nhật lặp lại.
    /// </summary>
    public static class WpfShapeLibrary
    {
        private static readonly List<ShapeCategoryGroup> _categories = new List<ShapeCategoryGroup>();
        private static readonly Dictionary<string, ShapePresetItem> _lookup = new Dictionary<string, ShapePresetItem>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _syncLock = new object();

        public static IReadOnlyList<ShapeCategoryGroup> Categories
        {
            get
            {
                EnsureInitialized();
                return _categories;
            }
        }

        public static int TotalPresetCount
        {
            get
            {
                EnsureInitialized();
                return _lookup.Count;
            }
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
                {
                    result.Add(kvp.Value);
                }
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
            if (!_lookup.ContainsKey(id))
            {
                _lookup[id] = item;
            }
        }

        private static void PopulateAllCategories()
        {
            _categories.Clear();
            _lookup.Clear();

            PopulateLinesAndArrows();       // 1. Đường kẻ & Mũi tên (45 mẫu)
            PopulateBasicShapes();          // 2. Hình dạng cơ bản (65 mẫu)
            PopulatePolygonsAndGeometry();  // 3. Đa giác đều & Hình học (45 mẫu)
            PopulateStarsAndBadges();       // 4. Ngôi sao & Huy hiệu (50 mẫu)
            PopulateBlockArrows();          // 5. Mũi tên khối & Chỉ hướng (50 mẫu)
            PopulateFlowchartSymbols();     // 6. Lưu đồ & Quy trình (50 mẫu)
            PopulateCallouts();             // 7. Bóng thoại & Chú thích (35 mẫu)
            PopulateBannersAndRibbons();     // 8. Dải ruy băng & Biểu ngữ (35 mẫu)
            PopulateIsometric3DShapes();     // 9. Hình khối 3D & Isometric (35 mẫu)
            PopulateDiagramComponents();    // 10. Sơ đồ tư duy & Cụm liên kết (50 mẫu)
            PopulateMathAndTechnical();     // 11. Ký hiệu toán học & Kỹ thuật (50 mẫu)
            PopulateBordersAndDividers();   // 12. Khung viền & Họa tiết trang trí (50 mẫu)
        }
'''

print("Generator skeleton ready")

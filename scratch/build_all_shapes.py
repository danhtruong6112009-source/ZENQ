# -*- coding: utf-8 -*-
import sys

out_file = r"C:\Users\Danh\Desktop\genofficepy\src\ui\WpfShapeLibrary.cs"

script = """# -*- coding: utf-8 -*-
import os

out_path = r"C:\\Users\\Danh\\Desktop\\genofficepy\\src\\ui\\WpfShapeLibrary.cs"

# We will generate C# file with 520+ distinct shapes
code = []

def a(s):
    code.append(s)

a('''using System;
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

    public class ShapeCategoryGroup
    {
        public string Key { get; set; }
        public string Title { get; set; }
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

        #region Geometry Primitives Helper
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

        private static GraphicsPath GPoly(float w, float h, params (float x, float y)[] pts)
        {
            GraphicsPath gp = new GraphicsPath();
            PointF[] p = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                p[i] = new PointF(pts[i].x * w, pts[i].y * h);
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

        private static GraphicsPath GLines(float w, float h, params (float x, float y)[] pts)
        {
            GraphicsPath gp = new GraphicsPath();
            PointF[] p = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                p[i] = new PointF(pts[i].x * w, pts[i].y * h);
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

        private static GraphicsPath GRegularPoly(float w, float h, int sides, float angleOffset)
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

        private static GraphicsPath GStar(float w, float h, int points, float innerRatio)
        {
            GraphicsPath gp = new GraphicsPath();
            float cx = w / 2f, cy = h / 2f;
            float rx = (w - 4f) / 2f, ry = (h - 4f) / 2f;
            int total = points * 2;
            PointF[] pts = new PointF[total];
            for (int i = 0; i < total; i++)
            {
                double angle = -Math.PI / 2.0 + 2.0 * Math.PI * i / total;
                float scale = (i % 2 == 0) ? 1f : innerRatio;
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

        private static GraphicsPath GRoundedRect(float w, float h, float r)
        {
            r = Math.Min(r, Math.Min(w / 2f, h / 2f));
            GraphicsPath gp = new GraphicsPath();
            float d = r * 2f;
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
''')

# Now let's assemble PopulateAllCategories() and all 12 category methods!
"""

with open("scratch/build_all_shapes.py", "w", encoding="utf-8") as f:
    f.write(script)

print("build_all_shapes.py skeleton written")

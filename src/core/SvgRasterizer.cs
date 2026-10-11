using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;
using WpfSize = System.Windows.Size;
using WpfColor = System.Windows.Media.Color;
using WpfPen = System.Windows.Media.Pen;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfLinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using WpfRadialGradientBrush = System.Windows.Media.RadialGradientBrush;
using WpfGradientStop = System.Windows.Media.GradientStop;
using WpfBrushMappingMode = System.Windows.Media.BrushMappingMode;
using WpfDrawingVisual = System.Windows.Media.DrawingVisual;
using WpfDrawingContext = System.Windows.Media.DrawingContext;
using WpfRenderTargetBitmap = System.Windows.Media.Imaging.RenderTargetBitmap;
using WpfPixelFormats = System.Windows.Media.PixelFormats;
using WpfPenLineCap = System.Windows.Media.PenLineCap;
using WpfPenLineJoin = System.Windows.Media.PenLineJoin;
using WpfGeometry = System.Windows.Media.Geometry;
using WpfPathGeometry = System.Windows.Media.PathGeometry;
using WpfGeometryGroup = System.Windows.Media.GeometryGroup;
using WpfFillRule = System.Windows.Media.FillRule;
using WpfMatrix = System.Windows.Media.Matrix;
using WpfMatrixTransform = System.Windows.Media.MatrixTransform;
using WpfDashStyle = System.Windows.Media.DashStyle;

namespace ZenQOffice.Core
{
    /// <summary>
    /// Bộ chuyển đổi và kết xuất ảnh vector SVG sang Bitmap chất lượng cao bằng WPF Vector Geometry Engine,
    /// hỗ trợ đầy đủ ma trận biến đổi (translate, rotate, scale, skew, matrix), phân cấp nhóm &lt;g&gt;,
    /// LinearGradient, RadialGradient (userSpaceOnUse &amp; objectBoundingBox, xlink:href), CSS style/class,
    /// biến màu var(--primary-svg-color), ClipPath, Opacity và nét vẽ khử răng cưa.
    /// </summary>
    public static class SvgRasterizer
    {
        private struct SvgRenderState
        {
            public string Fill;
            public string Stroke;
            public double StrokeWidth;
            public double FillOpacity;
            public double StrokeOpacity;
            public string FillRule;
            public string StrokeLineCap;
            public string StrokeLineJoin;
            public string StrokeDashArray;
            public string CurrentColor;

            public static SvgRenderState CreateDefault()
            {
                return new SvgRenderState
                {
                    Fill = "#000000",
                    Stroke = "none",
                    StrokeWidth = 1.0,
                    FillOpacity = 1.0,
                    StrokeOpacity = 1.0,
                    FillRule = "nonzero",
                    StrokeLineCap = "butt",
                    StrokeLineJoin = "miter",
                    StrokeDashArray = null,
                    CurrentColor = "#1E293B"
                };
            }
        }

        private static readonly Regex _transformRegex = new Regex(@"([a-zA-Z]+)\s*\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex _cssRuleRegex = new Regex(@"([^{]+)\{([^}]*)\}", RegexOptions.Compiled);

        public static Bitmap RenderSvgToBitmap(string svgXml, int targetWidth = 1200, int targetHeight = 1200)
        {
            if (string.IsNullOrWhiteSpace(svgXml)) return null;

            try
            {
                string trimmed = svgXml.Trim();
                if (!trimmed.StartsWith("<") && File.Exists(trimmed))
                {
                    svgXml = File.ReadAllText(trimmed);
                }

                XDocument doc = XDocument.Parse(svgXml);
                XElement root = doc.Root;
                if (root == null || !string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase)) return null;

                // 1. Xác định kích thước viewBox & width/height
                double vbX = 0, vbY = 0, vbW = 0, vbH = 0;
                bool hasViewBox = false;
                string viewBoxAttr = GetAttr(root, "viewBox");
                if (!string.IsNullOrEmpty(viewBoxAttr))
                {
                    double[] parts = ParseNumberList(viewBoxAttr);
                    if (parts.Length >= 4 && parts[2] > 0 && parts[3] > 0)
                    {
                        vbX = parts[0];
                        vbY = parts[1];
                        vbW = parts[2];
                        vbH = parts[3];
                        hasViewBox = true;
                    }
                }

                if (!hasViewBox)
                {
                    double w = ParseLength(GetAttr(root, "width"), 0);
                    double h = ParseLength(GetAttr(root, "height"), 0);
                    if (w > 0) vbW = w;
                    if (h > 0) vbH = h;
                }

                if (vbW <= 0) vbW = 24;
                if (vbH <= 0) vbH = 24;

                double scale = Math.Min((double)targetWidth / vbW, (double)targetHeight / vbH);
                int actualWidth = Math.Max(1, (int)Math.Ceiling(vbW * scale));
                int actualHeight = Math.Max(1, (int)Math.Ceiling(vbH * scale));

                // 2. Phân tích CSS <style> và lập chỉ mục các phần tử có id
                Dictionary<string, Dictionary<string, string>> cssRules = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, XElement> defsById = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);

                foreach (XElement elem in root.Descendants())
                {
                    string id = GetAttr(elem, "id");
                    if (!string.IsNullOrEmpty(id))
                    {
                        defsById[id] = elem;
                    }
                    if (string.Equals(elem.Name.LocalName, "style", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseCssStylesheet(elem.Value, cssRules);
                    }
                }

                // 3. Phân tích các định nghĩa Gradient trong toàn bộ tài liệu SVG
                Dictionary<string, WpfBrush> defBrushes = new Dictionary<string, WpfBrush>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement defElem in root.Descendants())
                {
                    string localName = defElem.Name.LocalName;
                    string id = GetAttr(defElem, "id");
                    if (string.IsNullOrEmpty(id)) continue;

                    if (string.Equals(localName, "linearGradient", StringComparison.OrdinalIgnoreCase))
                    {
                        WpfBrush lgb = BuildLinearGradientBrush(defElem, defsById);
                        if (lgb != null) defBrushes[id] = lgb;
                    }
                    else if (string.Equals(localName, "radialGradient", StringComparison.OrdinalIgnoreCase))
                    {
                        WpfBrush rgb = BuildRadialGradientBrush(defElem, defsById);
                        if (rgb != null) defBrushes[id] = rgb;
                    }
                }

                // 4. Kết xuất đồ họa vector theo cây phân cấp chuẩn SVG
                WpfDrawingVisual dv = new WpfDrawingVisual();
                using (WpfDrawingContext dc = dv.RenderOpen())
                {
                    double offsetX = -vbX * scale;
                    double offsetY = -vbY * scale;

                    dc.PushTransform(new System.Windows.Media.TranslateTransform(offsetX, offsetY));
                    dc.PushTransform(new System.Windows.Media.ScaleTransform(scale, scale));

                    SvgRenderState rootState = SvgRenderState.CreateDefault();
                    RenderElementRecursive(dc, root, rootState, defBrushes, defsById, cssRules, 0);

                    dc.Pop(); // Scale
                    dc.Pop(); // Offset
                }

                WpfRenderTargetBitmap rtb = new WpfRenderTargetBitmap(actualWidth, actualHeight, 96, 96, WpfPixelFormats.Pbgra32);
                rtb.Render(dv);

                int stride = actualWidth * 4;
                byte[] pixelBytes = new byte[stride * actualHeight];
                rtb.CopyPixels(pixelBytes, stride, 0);

                Bitmap bmp = new Bitmap(actualWidth, actualHeight, PixelFormat.Format32bppPArgb);
                var rect = new Rectangle(0, 0, actualWidth, actualHeight);
                var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                try
                {
                    Marshal.Copy(pixelBytes, 0, bmpData.Scan0, pixelBytes.Length);
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                return bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SvgRasterizer error: " + ex.Message);
                return null;
            }
        }

        private static void RenderElementRecursive(
            WpfDrawingContext dc,
            XElement elem,
            SvgRenderState parentState,
            Dictionary<string, WpfBrush> defBrushes,
            Dictionary<string, XElement> defsById,
            Dictionary<string, Dictionary<string, string>> cssRules,
            int depth)
        {
            if (elem == null || depth > 32) return;

            string localName = elem.Name.LocalName;
            if (string.Equals(localName, "defs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "style", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "title", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "desc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "metadata", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "linearGradient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "radialGradient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "stop", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "clipPath", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "mask", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "filter", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "font", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Dictionary<string, string> attrs = ResolveElementAttributes(elem, cssRules);

            if (attrs.TryGetValue("display", out string dispVal) &&
                string.Equals(dispVal, "none", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (attrs.TryGetValue("visibility", out string visVal) &&
                (string.Equals(visVal, "hidden", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(visVal, "collapse", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            SvgRenderState state = MergeRenderState(parentState, attrs);

            int pushedCount = 0;

            // 1. Áp dụng Transform của phần tử (nếu có)
            if (attrs.TryGetValue("transform", out string transformStr) && !string.IsNullOrWhiteSpace(transformStr))
            {
                WpfMatrix mat = ParseSvgTransform(transformStr);
                if (!mat.IsIdentity)
                {
                    dc.PushTransform(new WpfMatrixTransform(mat));
                    pushedCount++;
                }
            }

            // 2. Áp dụng ClipPath của phần tử (nếu có)
            if (attrs.TryGetValue("clip-path", out string clipPathStr) && !string.IsNullOrWhiteSpace(clipPathStr))
            {
                string clipId = ExtractUrlId(clipPathStr);
                if (!string.IsNullOrEmpty(clipId) && defsById.TryGetValue(clipId, out XElement clipElem))
                {
                    WpfGeometry clipGeom = BuildClipGeometry(clipElem, cssRules);
                    if (clipGeom != null)
                    {
                        dc.PushClip(clipGeom);
                        pushedCount++;
                    }
                }
            }

            // 3. Áp dụng Opacity của phần tử / nhóm <g>
            if (attrs.TryGetValue("opacity", out string opStr) && !string.IsNullOrWhiteSpace(opStr))
            {
                double elemOpacity = ParseCoordinate(opStr, 1.0);
                elemOpacity = Math.Max(0.0, Math.Min(1.0, elemOpacity));
                if (elemOpacity < 0.999)
                {
                    dc.PushOpacity(elemOpacity);
                    pushedCount++;
                }
            }

            try
            {
                // Nếu là thẻ chứa (<svg>, <g>, <a>) -> Đệ quy xuống các phần tử con
                if (string.Equals(localName, "svg", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localName, "g", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localName, "a", StringComparison.OrdinalIgnoreCase))
                {
                    bool isNestedSvg = string.Equals(localName, "svg", StringComparison.OrdinalIgnoreCase) && depth > 0;
                    int nestedPush = 0;
                    if (isNestedSvg)
                    {
                        double nx = ParseDouble(GetAttrFromDict(attrs, "x"));
                        double ny = ParseDouble(GetAttrFromDict(attrs, "y"));
                        if (Math.Abs(nx) > 1e-6 || Math.Abs(ny) > 1e-6)
                        {
                            dc.PushTransform(new System.Windows.Media.TranslateTransform(nx, ny));
                            nestedPush++;
                        }
                    }

                    foreach (XElement child in elem.Elements())
                    {
                        RenderElementRecursive(dc, child, state, defBrushes, defsById, cssRules, depth + 1);
                    }

                    for (int i = 0; i < nestedPush; i++) dc.Pop();
                    return;
                }

                // Nếu là thẻ <use href="#id" x="..." y="..."/>
                if (string.Equals(localName, "use", StringComparison.OrdinalIgnoreCase))
                {
                    string href = GetHrefAttribute(elem);
                    if (!string.IsNullOrEmpty(href) && href.StartsWith("#"))
                    {
                        string refId = href.Substring(1).Trim();
                        if (defsById.TryGetValue(refId, out XElement targetElem) && targetElem != elem)
                        {
                            double ux = ParseDouble(GetAttrFromDict(attrs, "x"));
                            double uy = ParseDouble(GetAttrFromDict(attrs, "y"));
                            bool pushedUseTranslate = false;
                            if (Math.Abs(ux) > 1e-6 || Math.Abs(uy) > 1e-6)
                            {
                                dc.PushTransform(new System.Windows.Media.TranslateTransform(ux, uy));
                                pushedUseTranslate = true;
                            }
                            RenderElementRecursive(dc, targetElem, state, defBrushes, defsById, cssRules, depth + 1);
                            if (pushedUseTranslate) dc.Pop();
                        }
                    }
                    return;
                }

                // Tạo FillBrush và StrokePen theo đúng chuẩn SVG
                bool isLineElement = string.Equals(localName, "line", StringComparison.OrdinalIgnoreCase);
                WpfBrush fillBrush = isLineElement ? null : CreateFillBrush(state, defBrushes);
                WpfPen pen = CreateStrokePen(state, defBrushes);

                if (fillBrush == null && pen == null) return;

                WpfFillRule fillRule = string.Equals(state.FillRule, "evenodd", StringComparison.OrdinalIgnoreCase)
                    ? WpfFillRule.EvenOdd
                    : WpfFillRule.Nonzero;

                if (string.Equals(localName, "path", StringComparison.OrdinalIgnoreCase))
                {
                    string d = GetAttrFromDict(attrs, "d");
                    if (!string.IsNullOrEmpty(d))
                    {
                        try
                        {
                            var geom = ParsePathGeometryWithFillRule(d, fillRule);
                            if (geom != null)
                            {
                                dc.DrawGeometry(fillBrush, pen, geom);
                            }
                        }
                        catch { }
                    }
                }
                else if (string.Equals(localName, "circle", StringComparison.OrdinalIgnoreCase))
                {
                    double cx = ParseDouble(GetAttrFromDict(attrs, "cx"));
                    double cy = ParseDouble(GetAttrFromDict(attrs, "cy"));
                    double r = ParseDouble(GetAttrFromDict(attrs, "r"));
                    if (r > 0)
                    {
                        dc.DrawEllipse(fillBrush, pen, new WpfPoint(cx, cy), r, r);
                    }
                }
                else if (string.Equals(localName, "ellipse", StringComparison.OrdinalIgnoreCase))
                {
                    double cx = ParseDouble(GetAttrFromDict(attrs, "cx"));
                    double cy = ParseDouble(GetAttrFromDict(attrs, "cy"));
                    double rx = ParseDouble(GetAttrFromDict(attrs, "rx"));
                    double ry = ParseDouble(GetAttrFromDict(attrs, "ry"));
                    if (rx > 0 && ry > 0)
                    {
                        dc.DrawEllipse(fillBrush, pen, new WpfPoint(cx, cy), rx, ry);
                    }
                }
                else if (string.Equals(localName, "rect", StringComparison.OrdinalIgnoreCase))
                {
                    double x = ParseDouble(GetAttrFromDict(attrs, "x"));
                    double y = ParseDouble(GetAttrFromDict(attrs, "y"));
                    double w = ParseDouble(GetAttrFromDict(attrs, "width"));
                    double h = ParseDouble(GetAttrFromDict(attrs, "height"));
                    if (w > 0 && h > 0)
                    {
                        double rx = ParseDouble(GetAttrFromDict(attrs, "rx"));
                        double ry = ParseDouble(GetAttrFromDict(attrs, "ry"));
                        if (rx <= 0 && ry > 0) rx = ry;
                        if (ry <= 0 && rx > 0) ry = rx;
                        dc.DrawRoundedRectangle(fillBrush, pen, new WpfRect(x, y, w, h), rx, ry);
                    }
                }
                else if (isLineElement)
                {
                    double x1 = ParseDouble(GetAttrFromDict(attrs, "x1"));
                    double y1 = ParseDouble(GetAttrFromDict(attrs, "y1"));
                    double x2 = ParseDouble(GetAttrFromDict(attrs, "x2"));
                    double y2 = ParseDouble(GetAttrFromDict(attrs, "y2"));
                    if (pen != null)
                    {
                        dc.DrawLine(pen, new WpfPoint(x1, y1), new WpfPoint(x2, y2));
                    }
                }
                else if (string.Equals(localName, "polyline", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(localName, "polygon", StringComparison.OrdinalIgnoreCase))
                {
                    string pts = GetAttrFromDict(attrs, "points");
                    if (!string.IsNullOrEmpty(pts))
                    {
                        try
                        {
                            bool isPolygon = string.Equals(localName, "polygon", StringComparison.OrdinalIgnoreCase);
                            var geom = BuildPolyGeometry(pts, isPolygon, fillRule);
                            if (geom != null)
                            {
                                dc.DrawGeometry(fillBrush, pen, geom);
                            }
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                for (int i = 0; i < pushedCount; i++)
                {
                    dc.Pop();
                }
            }
        }

        private static WpfGeometry ParsePathGeometryWithFillRule(string d, WpfFillRule fillRule)
        {
            var rawGeom = WpfGeometry.Parse(d);
            if (rawGeom == null) return null;
            if (fillRule == WpfFillRule.EvenOdd)
            {
                return rawGeom; // StreamGeometry mặc định là EvenOdd
            }
            WpfPathGeometry pg = WpfPathGeometry.CreateFromGeometry(rawGeom);
            if (pg != null)
            {
                pg.FillRule = fillRule;
                pg.Freeze();
                return pg;
            }
            return rawGeom;
        }

        private static WpfGeometry BuildPolyGeometry(string pointsStr, bool closeFigure, WpfFillRule fillRule)
        {
            double[] coords = ParseNumberList(pointsStr);
            if (coords.Length < 4) return null;

            var sg = new System.Windows.Media.StreamGeometry();
            sg.FillRule = fillRule;
            using (var ctx = sg.Open())
            {
                ctx.BeginFigure(new WpfPoint(coords[0], coords[1]), true, closeFigure);
                for (int i = 2; i + 1 < coords.Length; i += 2)
                {
                    ctx.LineTo(new WpfPoint(coords[i], coords[i + 1]), true, true);
                }
            }
            sg.Freeze();
            return sg;
        }

        private static WpfGeometry BuildClipGeometry(XElement clipElem, Dictionary<string, Dictionary<string, string>> cssRules)
        {
            WpfGeometryGroup group = new WpfGeometryGroup { FillRule = WpfFillRule.Nonzero };
            foreach (XElement child in clipElem.Elements())
            {
                var attrs = ResolveElementAttributes(child, cssRules);
                string localName = child.Name.LocalName;
                WpfGeometry g = null;
                try
                {
                    if (string.Equals(localName, "path", StringComparison.OrdinalIgnoreCase))
                    {
                        string d = GetAttrFromDict(attrs, "d");
                        if (!string.IsNullOrEmpty(d)) g = WpfGeometry.Parse(d);
                    }
                    else if (string.Equals(localName, "rect", StringComparison.OrdinalIgnoreCase))
                    {
                        double x = ParseDouble(GetAttrFromDict(attrs, "x"));
                        double y = ParseDouble(GetAttrFromDict(attrs, "y"));
                        double w = ParseDouble(GetAttrFromDict(attrs, "width"));
                        double h = ParseDouble(GetAttrFromDict(attrs, "height"));
                        double rx = ParseDouble(GetAttrFromDict(attrs, "rx"));
                        double ry = ParseDouble(GetAttrFromDict(attrs, "ry"));
                        if (rx <= 0 && ry > 0) rx = ry;
                        if (ry <= 0 && rx > 0) ry = rx;
                        if (w > 0 && h > 0) g = new System.Windows.Media.RectangleGeometry(new WpfRect(x, y, w, h), rx, ry);
                    }
                    else if (string.Equals(localName, "circle", StringComparison.OrdinalIgnoreCase))
                    {
                        double cx = ParseDouble(GetAttrFromDict(attrs, "cx"));
                        double cy = ParseDouble(GetAttrFromDict(attrs, "cy"));
                        double r = ParseDouble(GetAttrFromDict(attrs, "r"));
                        if (r > 0) g = new System.Windows.Media.EllipseGeometry(new WpfPoint(cx, cy), r, r);
                    }
                    else if (string.Equals(localName, "ellipse", StringComparison.OrdinalIgnoreCase))
                    {
                        double cx = ParseDouble(GetAttrFromDict(attrs, "cx"));
                        double cy = ParseDouble(GetAttrFromDict(attrs, "cy"));
                        double rx = ParseDouble(GetAttrFromDict(attrs, "rx"));
                        double ry = ParseDouble(GetAttrFromDict(attrs, "ry"));
                        if (rx > 0 && ry > 0) g = new System.Windows.Media.EllipseGeometry(new WpfPoint(cx, cy), rx, ry);
                    }
                    else if (string.Equals(localName, "polygon", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(localName, "polyline", StringComparison.OrdinalIgnoreCase))
                    {
                        string pts = GetAttrFromDict(attrs, "points");
                        if (!string.IsNullOrEmpty(pts)) g = BuildPolyGeometry(pts, true, WpfFillRule.Nonzero);
                    }

                    if (g != null)
                    {
                        if (attrs.TryGetValue("transform", out string tStr) && !string.IsNullOrWhiteSpace(tStr))
                        {
                            WpfMatrix m = ParseSvgTransform(tStr);
                            if (!m.IsIdentity)
                            {
                                g = g.Clone();
                                g.Transform = new WpfMatrixTransform(m);
                            }
                        }
                        group.Children.Add(g);
                    }
                }
                catch { }
            }
            if (group.Children.Count == 0) return null;
            group.Freeze();
            return group;
        }

        public static WpfMatrix ParseSvgTransform(string transformStr)
        {
            WpfMatrix combined = WpfMatrix.Identity;
            if (string.IsNullOrWhiteSpace(transformStr)) return combined;

            MatchCollection matches = _transformRegex.Matches(transformStr);
            foreach (Match match in matches)
            {
                string fn = match.Groups[1].Value.Trim().ToLowerInvariant();
                double[] args = ParseNumberList(match.Groups[2].Value);
                if (args.Length == 0) continue;

                WpfMatrix m = WpfMatrix.Identity;
                switch (fn)
                {
                    case "translate":
                    {
                        double tx = args[0];
                        double ty = (args.Length >= 2) ? args[1] : 0.0;
                        m.Translate(tx, ty);
                        break;
                    }
                    case "scale":
                    {
                        double sx = args[0];
                        double sy = (args.Length >= 2) ? args[1] : sx;
                        m.Scale(sx, sy);
                        break;
                    }
                    case "rotate":
                    {
                        double angle = args[0];
                        if (args.Length >= 3)
                        {
                            m.RotateAt(angle, args[1], args[2]);
                        }
                        else
                        {
                            m.Rotate(angle);
                        }
                        break;
                    }
                    case "skewx":
                    {
                        double rad = args[0] * Math.PI / 180.0;
                        m = new WpfMatrix(1.0, 0.0, Math.Tan(rad), 1.0, 0.0, 0.0);
                        break;
                    }
                    case "skewy":
                    {
                        double rad = args[0] * Math.PI / 180.0;
                        m = new WpfMatrix(1.0, Math.Tan(rad), 0.0, 1.0, 0.0, 0.0);
                        break;
                    }
                    case "matrix":
                    {
                        if (args.Length >= 6)
                        {
                            m = new WpfMatrix(args[0], args[1], args[2], args[3], args[4], args[5]);
                        }
                        break;
                    }
                }

                // Trong chuẩn SVG, danh sách transform="T1 T2 ... Tn" áp dụng Tn trước rồi đến T1:
                // Với biểu diễn vector hàng của WPF (P' = P * M), ta nhân tiền tố (Prepend) từ trái sang phải.
                combined.Prepend(m);
            }

            return combined;
        }

        private static double[] ParseNumberList(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<double>();
            List<double> list = new List<double>();
            // Tách các số thực (bao gồm số âm dính liền như "10-20" hoặc số mũ "1.5e-3")
            MatchCollection matches = Regex.Matches(input, @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");
            foreach (Match m in matches)
            {
                if (double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                {
                    list.Add(v);
                }
            }
            return list.ToArray();
        }

        private static SvgRenderState MergeRenderState(SvgRenderState parent, Dictionary<string, string> attrs)
        {
            SvgRenderState s = parent;

            if (attrs.TryGetValue("color", out string col) && !string.IsNullOrWhiteSpace(col))
                s.CurrentColor = col.Trim();

            if (attrs.TryGetValue("fill", out string fill) && !string.IsNullOrWhiteSpace(fill))
                s.Fill = fill.Trim();

            if (attrs.TryGetValue("stroke", out string stroke) && !string.IsNullOrWhiteSpace(stroke))
                s.Stroke = stroke.Trim();

            if (attrs.TryGetValue("stroke-width", out string sw) && !string.IsNullOrWhiteSpace(sw))
                s.StrokeWidth = ParseLength(sw, s.StrokeWidth);

            if (attrs.TryGetValue("fill-opacity", out string fop) && !string.IsNullOrWhiteSpace(fop))
                s.FillOpacity = Math.Max(0.0, Math.Min(1.0, ParseCoordinate(fop, s.FillOpacity)));

            if (attrs.TryGetValue("stroke-opacity", out string sop) && !string.IsNullOrWhiteSpace(sop))
                s.StrokeOpacity = Math.Max(0.0, Math.Min(1.0, ParseCoordinate(sop, s.StrokeOpacity)));

            if (attrs.TryGetValue("fill-rule", out string fr) && !string.IsNullOrWhiteSpace(fr))
                s.FillRule = fr.Trim();

            if (attrs.TryGetValue("stroke-linecap", out string slc) && !string.IsNullOrWhiteSpace(slc))
                s.StrokeLineCap = slc.Trim();

            if (attrs.TryGetValue("stroke-linejoin", out string slj) && !string.IsNullOrWhiteSpace(slj))
                s.StrokeLineJoin = slj.Trim();

            if (attrs.TryGetValue("stroke-dasharray", out string sda) && !string.IsNullOrWhiteSpace(sda))
                s.StrokeDashArray = sda.Trim();

            return s;
        }

        private static Dictionary<string, string> ResolveElementAttributes(XElement elem, Dictionary<string, Dictionary<string, string>> cssRules)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Luật CSS theo tên thẻ (ví dụ: path { ... })
            if (cssRules != null && cssRules.TryGetValue(elem.Name.LocalName, out var tagProps))
            {
                foreach (var kv in tagProps) result[kv.Key] = kv.Value;
            }

            // 2. Các thuộc tính XML trực tiếp trên thẻ
            foreach (XAttribute attr in elem.Attributes())
            {
                result[attr.Name.LocalName] = attr.Value;
            }

            // 3. Luật CSS theo class="..."
            if (result.TryGetValue("class", out string classAttr) && !string.IsNullOrWhiteSpace(classAttr) && cssRules != null)
            {
                string[] classes = classAttr.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string cls in classes)
                {
                    if (cssRules.TryGetValue("." + cls, out var clsProps))
                    {
                        foreach (var kv in clsProps) result[kv.Key] = kv.Value;
                    }
                }
            }

            // 4. Thuộc tính inline style="..." có độ ưu tiên cao nhất
            if (result.TryGetValue("style", out string styleAttr) && !string.IsNullOrWhiteSpace(styleAttr))
            {
                ParseStyleDeclarations(styleAttr, result);
            }

            return result;
        }

        private static void ParseCssStylesheet(string cssText, Dictionary<string, Dictionary<string, string>> cssRules)
        {
            if (string.IsNullOrWhiteSpace(cssText)) return;
            MatchCollection matches = _cssRuleRegex.Matches(cssText);
            foreach (Match m in matches)
            {
                string selectorsRaw = m.Groups[1].Value;
                string body = m.Groups[2].Value;
                string[] selectors = selectorsRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sel in selectors)
                {
                    string cleanSel = sel.Trim();
                    if (string.IsNullOrEmpty(cleanSel)) continue;
                    if (!cssRules.TryGetValue(cleanSel, out var dict))
                    {
                        dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        cssRules[cleanSel] = dict;
                    }
                    ParseStyleDeclarations(body, dict);
                }
            }
        }

        private static void ParseStyleDeclarations(string styleText, Dictionary<string, string> target)
        {
            string[] parts = styleText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int colon = part.IndexOf(':');
                if (colon > 0)
                {
                    string key = part.Substring(0, colon).Trim();
                    string val = part.Substring(colon + 1).Trim();
                    if (val.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
                    {
                        val = val.Substring(0, val.Length - 10).Trim();
                    }
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                    {
                        target[key] = val;
                    }
                }
            }
        }

        private static WpfBrush BuildLinearGradientBrush(XElement defElem, Dictionary<string, XElement> defsById)
        {
            List<XElement> chain = GetGradientInheritanceChain(defElem, defsById);
            string units = GetInheritedAttr(chain, "gradientUnits") ?? "objectBoundingBox";
            bool isUserSpace = string.Equals(units, "userSpaceOnUse", StringComparison.OrdinalIgnoreCase);

            double x1 = ParseCoordinate(GetInheritedAttr(chain, "x1"), 0.0);
            double y1 = ParseCoordinate(GetInheritedAttr(chain, "y1"), 0.0);
            double x2 = ParseCoordinate(GetInheritedAttr(chain, "x2"), 1.0);
            double y2 = ParseCoordinate(GetInheritedAttr(chain, "y2"), 0.0); // Chuẩn SVG mặc định y2 = 0%

            WpfLinearGradientBrush lgb = new WpfLinearGradientBrush
            {
                StartPoint = new WpfPoint(x1, y1),
                EndPoint = new WpfPoint(x2, y2),
                MappingMode = isUserSpace ? WpfBrushMappingMode.Absolute : WpfBrushMappingMode.RelativeToBoundingBox
            };

            string gradTransform = GetInheritedAttr(chain, "gradientTransform");
            if (!string.IsNullOrWhiteSpace(gradTransform))
            {
                WpfMatrix mat = ParseSvgTransform(gradTransform);
                if (!mat.IsIdentity)
                {
                    if (isUserSpace)
                        lgb.Transform = new WpfMatrixTransform(mat);
                    else
                        lgb.RelativeTransform = new WpfMatrixTransform(mat);
                }
            }

            PopulateGradientStops(lgb.GradientStops, chain);
            lgb.Freeze();
            return lgb;
        }

        private static WpfBrush BuildRadialGradientBrush(XElement defElem, Dictionary<string, XElement> defsById)
        {
            List<XElement> chain = GetGradientInheritanceChain(defElem, defsById);
            string units = GetInheritedAttr(chain, "gradientUnits") ?? "objectBoundingBox";
            bool isUserSpace = string.Equals(units, "userSpaceOnUse", StringComparison.OrdinalIgnoreCase);

            double cx = ParseCoordinate(GetInheritedAttr(chain, "cx"), 0.5);
            double cy = ParseCoordinate(GetInheritedAttr(chain, "cy"), 0.5);
            double r = ParseCoordinate(GetInheritedAttr(chain, "r"), 0.5);
            double fx = ParseCoordinate(GetInheritedAttr(chain, "fx"), cx);
            double fy = ParseCoordinate(GetInheritedAttr(chain, "fy"), cy);

            WpfRadialGradientBrush rgb = new WpfRadialGradientBrush
            {
                Center = new WpfPoint(cx, cy),
                GradientOrigin = new WpfPoint(fx, fy),
                RadiusX = r,
                RadiusY = r,
                MappingMode = isUserSpace ? WpfBrushMappingMode.Absolute : WpfBrushMappingMode.RelativeToBoundingBox
            };

            string gradTransform = GetInheritedAttr(chain, "gradientTransform");
            if (!string.IsNullOrWhiteSpace(gradTransform))
            {
                WpfMatrix mat = ParseSvgTransform(gradTransform);
                if (!mat.IsIdentity)
                {
                    if (isUserSpace)
                        rgb.Transform = new WpfMatrixTransform(mat);
                    else
                        rgb.RelativeTransform = new WpfMatrixTransform(mat);
                }
            }

            PopulateGradientStops(rgb.GradientStops, chain);
            rgb.Freeze();
            return rgb;
        }

        private static List<XElement> GetGradientInheritanceChain(XElement startElem, Dictionary<string, XElement> defsById)
        {
            List<XElement> chain = new List<XElement>();
            XElement cur = startElem;
            int safety = 0;
            while (cur != null && safety++ < 8)
            {
                chain.Add(cur);
                string href = GetHrefAttribute(cur);
                if (!string.IsNullOrEmpty(href) && href.StartsWith("#"))
                {
                    string refId = href.Substring(1).Trim();
                    if (defsById != null && defsById.TryGetValue(refId, out XElement nextElem) && !chain.Contains(nextElem))
                    {
                        cur = nextElem;
                        continue;
                    }
                }
                break;
            }
            return chain;
        }

        private static string GetInheritedAttr(List<XElement> chain, string attrName)
        {
            foreach (XElement e in chain)
            {
                string val = GetAttr(e, attrName);
                if (!string.IsNullOrEmpty(val)) return val;
            }
            return null;
        }

        private static void PopulateGradientStops(System.Windows.Media.GradientStopCollection stops, List<XElement> chain)
        {
            XElement stopsOwner = null;
            foreach (XElement e in chain)
            {
                foreach (XElement child in e.Elements())
                {
                    if (string.Equals(child.Name.LocalName, "stop", StringComparison.OrdinalIgnoreCase))
                    {
                        stopsOwner = e;
                        break;
                    }
                }
                if (stopsOwner != null) break;
            }

            if (stopsOwner == null) return;

            foreach (XElement stopElem in stopsOwner.Elements())
            {
                if (!string.Equals(stopElem.Name.LocalName, "stop", StringComparison.OrdinalIgnoreCase)) continue;

                Dictionary<string, string> sAttrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (XAttribute a in stopElem.Attributes()) sAttrs[a.Name.LocalName] = a.Value;
                if (sAttrs.TryGetValue("style", out string st)) ParseStyleDeclarations(st, sAttrs);

                double offset = ParseCoordinate(GetAttrFromDict(sAttrs, "offset"), 0.0);
                string colStr = GetAttrFromDict(sAttrs, "stop-color");
                if (string.IsNullOrEmpty(colStr)) colStr = "#000000";

                double op = 1.0;
                string opStr = GetAttrFromDict(sAttrs, "stop-opacity");
                if (!string.IsNullOrEmpty(opStr)) op = ParseCoordinate(opStr, 1.0);

                WpfColor c = ParseColor(colStr, op);
                stops.Add(new WpfGradientStop(c, offset));
            }
        }

        private static WpfBrush CreateFillBrush(SvgRenderState state, Dictionary<string, WpfBrush> defs)
        {
            string fillStr = state.Fill;
            if (string.IsNullOrWhiteSpace(fillStr)) fillStr = "#000000";
            return ParseBrush(fillStr, defs, state.CurrentColor, state.FillOpacity);
        }

        private static WpfPen CreateStrokePen(SvgRenderState state, Dictionary<string, WpfBrush> defs)
        {
            if (string.IsNullOrWhiteSpace(state.Stroke) ||
                string.Equals(state.Stroke, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state.Stroke, "transparent", StringComparison.OrdinalIgnoreCase) ||
                state.StrokeWidth <= 0)
            {
                return null;
            }

            WpfBrush strokeBrush = ParseBrush(state.Stroke, defs, state.CurrentColor, state.StrokeOpacity);
            if (strokeBrush == null) return null;

            WpfPenLineCap cap = WpfPenLineCap.Flat;
            if (string.Equals(state.StrokeLineCap, "round", StringComparison.OrdinalIgnoreCase)) cap = WpfPenLineCap.Round;
            else if (string.Equals(state.StrokeLineCap, "square", StringComparison.OrdinalIgnoreCase)) cap = WpfPenLineCap.Square;

            WpfPenLineJoin join = WpfPenLineJoin.Miter;
            if (string.Equals(state.StrokeLineJoin, "round", StringComparison.OrdinalIgnoreCase)) join = WpfPenLineJoin.Round;
            else if (string.Equals(state.StrokeLineJoin, "bevel", StringComparison.OrdinalIgnoreCase)) join = WpfPenLineJoin.Bevel;

            WpfPen pen = new WpfPen(strokeBrush, state.StrokeWidth)
            {
                StartLineCap = cap,
                EndLineCap = cap,
                LineJoin = join
            };

            if (!string.IsNullOrWhiteSpace(state.StrokeDashArray) &&
                !string.Equals(state.StrokeDashArray, "none", StringComparison.OrdinalIgnoreCase))
            {
                double[] dashes = ParseNumberList(state.StrokeDashArray);
                if (dashes.Length > 0 && state.StrokeWidth > 0)
                {
                    double[] normalized = new double[dashes.Length];
                    for (int i = 0; i < dashes.Length; i++)
                    {
                        normalized[i] = Math.Max(0.1, dashes[i] / state.StrokeWidth);
                    }
                    pen.DashStyle = new WpfDashStyle(normalized, 0);
                }
            }

            pen.Freeze();
            return pen;
        }

        private static string GetAttr(XElement elem, string localName)
        {
            if (elem == null) return null;
            foreach (XAttribute a in elem.Attributes())
            {
                if (string.Equals(a.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
                    return a.Value;
            }
            return null;
        }

        private static string GetHrefAttribute(XElement elem)
        {
            if (elem == null) return null;
            foreach (XAttribute a in elem.Attributes())
            {
                if (string.Equals(a.Name.LocalName, "href", StringComparison.OrdinalIgnoreCase))
                    return a.Value?.Trim();
            }
            return null;
        }

        private static string GetAttrFromDict(Dictionary<string, string> attrs, string key)
        {
            if (attrs != null && attrs.TryGetValue(key, out string val)) return val;
            return null;
        }

        private static string ExtractUrlId(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            val = val.Trim();
            if (val.StartsWith("url(", StringComparison.OrdinalIgnoreCase) && val.EndsWith(")"))
            {
                string inner = val.Substring(4, val.Length - 5).Trim().Trim('\'', '"');
                if (inner.StartsWith("#")) return inner.Substring(1).Trim();
            }
            return null;
        }

        private static double ParseDouble(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;
            return ParseLength(str, 0);
        }

        private static double ParseLength(string str, double defaultVal)
        {
            if (string.IsNullOrWhiteSpace(str)) return defaultVal;
            str = str.Trim();
            if (str.EndsWith("px", StringComparison.OrdinalIgnoreCase) ||
                str.EndsWith("pt", StringComparison.OrdinalIgnoreCase) ||
                str.EndsWith("em", StringComparison.OrdinalIgnoreCase))
            {
                str = str.Substring(0, str.Length - 2).Trim();
            }
            else if (str.EndsWith("%"))
            {
                str = str.Substring(0, str.Length - 1).Trim();
            }
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                return v;
            return defaultVal;
        }

        private static double ParseCoordinate(string str, double defaultVal)
        {
            if (string.IsNullOrWhiteSpace(str)) return defaultVal;
            str = str.Trim();
            if (str.EndsWith("%"))
            {
                if (double.TryParse(str.Substring(0, str.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
                    return pct / 100.0;
            }
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                return val;
            return defaultVal;
        }

        private static WpfBrush ParseBrush(string colorStr, Dictionary<string, WpfBrush> defs, string currentColorHex, double opacity = 1.0)
        {
            if (string.IsNullOrWhiteSpace(colorStr)) return null;
            colorStr = colorStr.Trim();

            if (colorStr.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                colorStr.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (colorStr.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            {
                colorStr = !string.IsNullOrWhiteSpace(currentColorHex) ? currentColorHex : "#1E293B";
            }

            string urlId = ExtractUrlId(colorStr);
            if (!string.IsNullOrEmpty(urlId))
            {
                if (defs != null && defs.TryGetValue(urlId, out WpfBrush b))
                {
                    if (opacity < 0.999)
                    {
                        WpfBrush cloned = b.Clone();
                        cloned.Opacity = opacity;
                        cloned.Freeze();
                        return cloned;
                    }
                    return b;
                }
                return null;
            }

            WpfColor c = ParseColor(colorStr, opacity);
            if (c.A == 0) return null;
            var brush = new WpfSolidColorBrush(c);
            brush.Freeze();
            return brush;
        }

        private static WpfColor ParseColor(string colorStr, double opacity = 1.0)
        {
            if (string.IsNullOrWhiteSpace(colorStr) ||
                colorStr.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                colorStr.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            {
                return WpfColor.FromArgb(0, 0, 0, 0);
            }

            colorStr = colorStr.Trim();

            // Hỗ trợ biến CSS trong kho minh họa unDraw / Scenes (ví dụ: var(--primary-svg-color))
            if (colorStr.StartsWith("var(", StringComparison.OrdinalIgnoreCase))
            {
                int commaIdx = colorStr.IndexOf(',');
                if (commaIdx > 0 && colorStr.EndsWith(")"))
                {
                    string fallback = colorStr.Substring(commaIdx + 1, colorStr.Length - commaIdx - 2).Trim();
                    if (!string.IsNullOrEmpty(fallback))
                        return ParseColor(fallback, opacity);
                }
                // Màu chủ đạo chuẩn unDraw Indigo-Blue
                return ApplyOpacity(WpfColor.FromRgb(108, 99, 255), opacity);
            }

            // Hỗ trợ rgb(...) và rgba(...)
            if (colorStr.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                double[] nums = ParseNumberList(colorStr);
                if (nums.Length >= 3)
                {
                    byte r = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(nums[0])));
                    byte g = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(nums[1])));
                    byte b = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(nums[2])));
                    double a = (nums.Length >= 4) ? Math.Max(0.0, Math.Min(1.0, nums[3])) : 1.0;
                    return ApplyOpacity(WpfColor.FromArgb(255, r, g, b), opacity * a);
                }
            }

            try
            {
                if (colorStr.StartsWith("#"))
                {
                    string hex = colorStr.Substring(1);
                    if (hex.Length == 3)
                    {
                        byte r = Convert.ToByte(new string(hex[0], 2), 16);
                        byte g = Convert.ToByte(new string(hex[1], 2), 16);
                        byte b = Convert.ToByte(new string(hex[2], 2), 16);
                        return ApplyOpacity(WpfColor.FromArgb(255, r, g, b), opacity);
                    }
                    else if (hex.Length == 4)
                    {
                        byte r = Convert.ToByte(new string(hex[0], 2), 16);
                        byte g = Convert.ToByte(new string(hex[1], 2), 16);
                        byte b = Convert.ToByte(new string(hex[2], 2), 16);
                        byte a = Convert.ToByte(new string(hex[3], 2), 16);
                        return ApplyOpacity(WpfColor.FromArgb(a, r, g, b), opacity);
                    }
                    else if (hex.Length == 6)
                    {
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        return ApplyOpacity(WpfColor.FromArgb(255, r, g, b), opacity);
                    }
                    else if (hex.Length == 8)
                    {
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        byte a = Convert.ToByte(hex.Substring(6, 2), 16);
                        return ApplyOpacity(WpfColor.FromArgb(a, r, g, b), opacity);
                    }
                }
            }
            catch { }

            // Named colors chuẩn SVG / CSS
            switch (colorStr.ToLowerInvariant())
            {
                case "white": return ApplyOpacity(WpfColor.FromRgb(255, 255, 255), opacity);
                case "black": return ApplyOpacity(WpfColor.FromRgb(0, 0, 0), opacity);
                case "gold": return ApplyOpacity(WpfColor.FromRgb(255, 215, 0), opacity);
                case "red": return ApplyOpacity(WpfColor.FromRgb(239, 68, 68), opacity);
                case "blue": return ApplyOpacity(WpfColor.FromRgb(59, 130, 246), opacity);
                case "green": return ApplyOpacity(WpfColor.FromRgb(16, 185, 129), opacity);
                case "yellow": return ApplyOpacity(WpfColor.FromRgb(250, 204, 21), opacity);
                case "crimson": return ApplyOpacity(WpfColor.FromRgb(158, 11, 15), opacity);
                case "gray":
                case "grey": return ApplyOpacity(WpfColor.FromRgb(128, 128, 128), opacity);
                default:
                    try
                    {
                        Color sysCol = Color.FromName(colorStr);
                        if (sysCol.IsKnownColor)
                        {
                            return ApplyOpacity(WpfColor.FromArgb(sysCol.A, sysCol.R, sysCol.G, sysCol.B), opacity);
                        }
                    }
                    catch { }
                    break;
            }

            return ApplyOpacity(WpfColor.FromArgb(255, 30, 41, 59), opacity);
        }

        private static WpfColor ApplyOpacity(WpfColor c, double opacity)
        {
            byte a = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(c.A * opacity)));
            return WpfColor.FromArgb(a, c.R, c.G, c.B);
        }
    }
}


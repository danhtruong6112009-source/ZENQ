using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
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
using WpfFormattedText = System.Windows.Media.FormattedText;

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
            public string MaskId;
            public string FontFamily;
            public double FontSize;
            public string FontWeight;
            public string FontStyle;
            public string TextAnchor;
            public double StrokeMiterLimit;

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
                    CurrentColor = "#1E293B",
                    MaskId = null,
                    FontFamily = "Arial, Segoe UI, sans-serif",
                    FontSize = 16.0,
                    FontWeight = "normal",
                    FontStyle = "normal",
                    TextAnchor = "start",
                    StrokeMiterLimit = 4.0
                };
            }
        }

        private static readonly Regex _transformRegex = new Regex(@"([a-zA-Z]+)\s*\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex _cssRuleRegex = new Regex(@"([^{]+)\{([^}]*)\}", RegexOptions.Compiled);

        private static readonly object _svgCacheLock = new object();
        private static readonly Dictionary<string, Bitmap> _svgCache = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        private static readonly LinkedList<string> _svgCacheLru = new LinkedList<string>();
        private const int MaxSvgCacheEntries = 128;

        public static void ClearCache()
        {
            lock (_svgCacheLock)
            {
                _svgCache.Clear();
                _svgCacheLru.Clear();
            }
        }

        public static Bitmap RenderSvgToBitmap(string svgXml, int targetWidth = 1200, int targetHeight = 1200)
        {
            if (string.IsNullOrWhiteSpace(svgXml)) return null;

            string trimmed = svgXml.Trim();
            string cacheKey = null;
            bool isFile = false;

            try
            {
                if (!trimmed.StartsWith("<") && File.Exists(trimmed))
                {
                    isFile = true;
                    cacheKey = "F:" + trimmed + "|" + targetWidth + "x" + targetHeight + "|" + File.GetLastWriteTimeUtc(trimmed).Ticks;
                }
                else
                {
                    ulong h = 14695981039346656037UL;
                    int len = trimmed.Length;
                    int step = Math.Max(1, len / 64);
                    for (int i = 0; i < len; i += step)
                    {
                        h ^= trimmed[i];
                        h *= 1099511628211UL;
                    }
                    cacheKey = "X:" + targetWidth + "x" + targetHeight + "|" + len + "|" + h;
                }

                lock (_svgCacheLock)
                {
                    if (_svgCache.TryGetValue(cacheKey, out Bitmap cached) && cached != null)
                    {
                        try
                        {
                            int w = cached.Width;
                            _svgCacheLru.Remove(cacheKey);
                            _svgCacheLru.AddLast(cacheKey);
                            return cached;
                        }
                        catch
                        {
                            _svgCache.Remove(cacheKey);
                            _svgCacheLru.Remove(cacheKey);
                        }
                    }
                }

                if (isFile)
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

                if (targetWidth <= 0 || targetHeight <= 0)
                {
                    targetWidth = Math.Max(1, (int)Math.Ceiling(vbW));
                    targetHeight = Math.Max(1, (int)Math.Ceiling(vbH));
                }

                double scale = Math.Min((double)targetWidth / vbW, (double)targetHeight / vbH);
                int actualWidth = Math.Max(1, (int)Math.Ceiling(vbW * scale));
                int actualHeight = Math.Max(1, (int)Math.Ceiling(vbH * scale));

                // 2. Phân tích CSS <style> và lập chỉ mục các phần tử có id
                Dictionary<string, Dictionary<string, string>> cssRules = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, XElement> defsById = new Dictionary<string, XElement>(StringComparer.Ordinal);

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
                Dictionary<string, WpfBrush> defBrushes = new Dictionary<string, WpfBrush>(StringComparer.Ordinal);
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

                if (cacheKey != null && bmp != null)
                {
                    lock (_svgCacheLock)
                    {
                        if (_svgCache.Count >= MaxSvgCacheEntries)
                        {
                            var oldest = _svgCacheLru.First?.Value;
                            if (oldest != null)
                            {
                                _svgCacheLru.RemoveFirst();
                                _svgCache.Remove(oldest);
                            }
                        }
                        _svgCache[cacheKey] = bmp;
                        _svgCacheLru.AddLast(cacheKey);
                    }
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
                string.Equals(localName, "font", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(localName, "symbol", StringComparison.OrdinalIgnoreCase))
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
                    WpfGeometry clipGeom = BuildClipGeometry(clipElem, cssRules, defsById);
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

            // 4. Áp dụng Mask của phần tử / nhóm <g> (trừ khi là thẻ <image> có cơ chế kết hợp ảnh trực tiếp)
            if (!string.Equals(localName, "image", StringComparison.OrdinalIgnoreCase) &&
                attrs.TryGetValue("mask", out string maskAttrVal) && !string.IsNullOrWhiteSpace(maskAttrVal))
            {
                string maskId = ExtractUrlId(maskAttrVal);
                if (!string.IsNullOrEmpty(maskId) && defsById.TryGetValue(maskId, out XElement maskElem))
                {
                    bool hasImageChild = elem.Descendants().Any(d => string.Equals(d.Name.LocalName, "image", StringComparison.OrdinalIgnoreCase));
                    if (!hasImageChild)
                    {
                        double uniformMaskOpacity = ExtractMaskUniformOpacity(maskElem);
                        if (uniformMaskOpacity < 0.999)
                        {
                            dc.PushOpacity(uniformMaskOpacity);
                            pushedCount++;
                        }
                        else
                        {
                            var maskBrush = CreateMaskOpacityBrush(maskElem, defBrushes, defsById, cssRules);
                            if (maskBrush != null)
                            {
                                dc.PushOpacityMask(maskBrush);
                                pushedCount++;
                            }
                        }
                    }
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
                            double uw = ParseLength(GetAttrFromDict(attrs, "width"), 0);
                            double uh = ParseLength(GetAttrFromDict(attrs, "height"), 0);

                            int pushUse = 0;
                            if (Math.Abs(ux) > 1e-6 || Math.Abs(uy) > 1e-6)
                            {
                                dc.PushTransform(new System.Windows.Media.TranslateTransform(ux, uy));
                                pushUse++;
                            }

                            if (string.Equals(targetElem.Name.LocalName, "symbol", StringComparison.OrdinalIgnoreCase))
                            {
                                string vbStr = GetAttr(targetElem, "viewBox");
                                if (!string.IsNullOrEmpty(vbStr) && uw > 0 && uh > 0)
                                {
                                    double[] vbParts = ParseNumberList(vbStr);
                                    if (vbParts.Length >= 4 && vbParts[2] > 0 && vbParts[3] > 0)
                                    {
                                        double vx = vbParts[0], vy = vbParts[1], vw = vbParts[2], vh = vbParts[3];
                                        string par = GetAttr(targetElem, "preserveAspectRatio") ?? "xMidYMid meet";
                                        double sx = uw / vw;
                                        double sy = uh / vh;
                                        double tx = -vx * sx;
                                        double ty = -vy * sy;

                                        if (!par.Trim().StartsWith("none", StringComparison.OrdinalIgnoreCase))
                                        {
                                            double s = Math.Min(sx, sy);
                                            tx = -vx * s + (uw - vw * s) / 2.0;
                                            ty = -vy * s + (uh - vh * s) / 2.0;
                                            sx = s;
                                            sy = s;
                                        }

                                        WpfMatrix symMat = new WpfMatrix(sx, 0, 0, sy, tx, ty);
                                        dc.PushTransform(new WpfMatrixTransform(symMat));
                                        pushUse++;
                                    }
                                }

                                foreach (XElement child in targetElem.Elements())
                                {
                                    RenderElementRecursive(dc, child, state, defBrushes, defsById, cssRules, depth + 1);
                                }
                            }
                            else
                            {
                                RenderElementRecursive(dc, targetElem, state, defBrushes, defsById, cssRules, depth + 1);
                            }

                            for (int i = 0; i < pushUse; i++) dc.Pop();
                        }
                    }
                    return;
                }

                // Nếu là thẻ <image .../>
                if (string.Equals(localName, "image", StringComparison.OrdinalIgnoreCase))
                {
                    string href = GetAttrFromDict(attrs, "href");
                    if (string.IsNullOrEmpty(href)) href = GetHrefAttribute(elem);

                    if (!string.IsNullOrEmpty(href))
                    {
                        System.Windows.Media.ImageSource imgSource = LoadSvgImageSource(href);
                        if (imgSource != null)
                        {
                            string maskId = null;
                            if (attrs.TryGetValue("mask", out string elemMask))
                                maskId = ExtractUrlId(elemMask);
                            if (string.IsNullOrEmpty(maskId))
                                maskId = state.MaskId;

                            if (!string.IsNullOrEmpty(maskId) && defsById.TryGetValue(maskId, out XElement maskElem))
                            {
                                XElement maskImgElem = maskElem.Descendants().FirstOrDefault(d => string.Equals(d.Name.LocalName, "image", StringComparison.OrdinalIgnoreCase));
                                if (maskImgElem != null)
                                {
                                    string maskHref = GetAttr(maskImgElem, "href") ?? GetHrefAttribute(maskImgElem);
                                    if (!string.IsNullOrEmpty(maskHref))
                                    {
                                        System.Windows.Media.ImageSource maskSource = LoadSvgImageSource(maskHref);
                                        if (maskSource != null)
                                        {
                                            var combinedSource = CombineImageWithAlphaMask(imgSource, maskSource);
                                            if (combinedSource != null)
                                            {
                                                imgSource = combinedSource;
                                            }
                                        }
                                    }
                                }
                            }

                            double ix = ParseDouble(GetAttrFromDict(attrs, "x"));
                            double iy = ParseDouble(GetAttrFromDict(attrs, "y"));
                            double iw = ParseLength(GetAttrFromDict(attrs, "width"), imgSource.Width);
                            double ih = ParseLength(GetAttrFromDict(attrs, "height"), imgSource.Height);
                            if (iw <= 0) iw = imgSource.Width;
                            if (ih <= 0) ih = imgSource.Height;

                            string par = GetAttrFromDict(attrs, "preserveAspectRatio");
                            bool preserveAspect = string.IsNullOrEmpty(par) || !par.Trim().StartsWith("none", StringComparison.OrdinalIgnoreCase);

                            if (preserveAspect && imgSource.Width > 0 && imgSource.Height > 0 && iw > 0 && ih > 0)
                            {
                                double srcW = imgSource.Width;
                                double srcH = imgSource.Height;
                                double scaleImg = Math.Min(iw / srcW, ih / srcH);
                                double renderW = srcW * scaleImg;
                                double renderH = srcH * scaleImg;
                                double drawX = ix + (iw - renderW) / 2.0;
                                double drawY = iy + (ih - renderH) / 2.0;
                                dc.DrawImage(imgSource, new WpfRect(drawX, drawY, renderW, renderH));
                            }
                            else
                            {
                                dc.DrawImage(imgSource, new WpfRect(ix, iy, iw, ih));
                            }
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
                                WpfBrush elemFill = AdaptBrushToBoundingBox(fillBrush, geom.Bounds);
                                dc.DrawGeometry(elemFill, pen, geom);
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
                        WpfBrush elemFill = AdaptBrushToBoundingBox(fillBrush, new WpfRect(cx - r, cy - r, 2 * r, 2 * r));
                        dc.DrawEllipse(elemFill, pen, new WpfPoint(cx, cy), r, r);
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
                        WpfBrush elemFill = AdaptBrushToBoundingBox(fillBrush, new WpfRect(cx - rx, cy - ry, 2 * rx, 2 * ry));
                        dc.DrawEllipse(elemFill, pen, new WpfPoint(cx, cy), rx, ry);
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

                        WpfRect rectBounds = new WpfRect(x, y, w, h);
                        WpfBrush elemFill = AdaptBrushToBoundingBox(fillBrush, rectBounds);
                        WpfPen elemPen = pen;
                        if (pen != null && pen.Brush is WpfLinearGradientBrush)
                        {
                            var adaptedPenBrush = AdaptBrushToBoundingBox(pen.Brush, rectBounds);
                            if (adaptedPenBrush != pen.Brush)
                            {
                                elemPen = pen.Clone();
                                elemPen.Brush = adaptedPenBrush;
                                elemPen.Freeze();
                            }
                        }

                        if (rx <= 0 && ry <= 0 && elemPen != null && (elemPen.LineJoin == WpfPenLineJoin.Round || elemPen.LineJoin == WpfPenLineJoin.Bevel))
                        {
                            var sg = new System.Windows.Media.StreamGeometry();
                            using (var ctx = sg.Open())
                            {
                                ctx.BeginFigure(new WpfPoint(x, y), elemFill != null, true);
                                ctx.LineTo(new WpfPoint(x + w, y), true, false);
                                ctx.LineTo(new WpfPoint(x + w, y + h), true, false);
                                ctx.LineTo(new WpfPoint(x, y + h), true, false);
                            }
                            sg.Freeze();
                            dc.DrawGeometry(elemFill, elemPen, sg);
                        }
                        else
                        {
                            dc.DrawRoundedRectangle(elemFill, elemPen, rectBounds, rx, ry);
                        }
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
                                WpfBrush elemFill = AdaptBrushToBoundingBox(fillBrush, geom.Bounds);
                                dc.DrawGeometry(elemFill, pen, geom);
                            }
                        }
                        catch { }
                    }
                }
                else if (string.Equals(localName, "text", StringComparison.OrdinalIgnoreCase))
                {
                    RenderSvgTextElement(dc, elem, state, defBrushes, defsById, cssRules, fillBrush, pen, attrs);
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

        private static void RenderSvgTextElement(
            WpfDrawingContext dc,
            XElement elem,
            SvgRenderState state,
            Dictionary<string, WpfBrush> defBrushes,
            Dictionary<string, XElement> defsById,
            Dictionary<string, Dictionary<string, string>> cssRules,
            WpfBrush fillBrush,
            WpfPen pen,
            Dictionary<string, string> attrs)
        {
            double curX = ParseDouble(GetAttrFromDict(attrs, "x"));
            double curY = ParseDouble(GetAttrFromDict(attrs, "y"));
            curX += ParseDouble(GetAttrFromDict(attrs, "dx"));
            curY += ParseDouble(GetAttrFromDict(attrs, "dy"));
            string anchor = GetAttrFromDict(attrs, "text-anchor") ?? state.TextAnchor ?? "start";
            double textLength = ParseLength(GetAttrFromDict(attrs, "textLength"), 0);
            string lengthAdjust = GetAttrFromDict(attrs, "lengthAdjust") ?? "spacing";

            bool hasTspan = elem.Elements().Any(e => string.Equals(e.Name.LocalName, "tspan", StringComparison.OrdinalIgnoreCase));

            if (!hasTspan)
            {
                string text = CleanSvgText(elem.Value, true);
                if (string.IsNullOrEmpty(text)) return;

                WpfFormattedText ft = CreateFormattedText(text, state, fillBrush);
                DrawFormattedSvgText(dc, ft, curX, curY, anchor, fillBrush, pen, textLength, lengthAdjust, text, state);
            }
            else
            {
                foreach (XNode node in elem.Nodes())
                {
                    if (node is XText textNode)
                    {
                        string text = CleanSvgText(textNode.Value, false);
                        if (!string.IsNullOrEmpty(text))
                        {
                            WpfFormattedText ft = CreateFormattedText(text, state, fillBrush);
                            DrawFormattedSvgText(dc, ft, curX, curY, anchor, fillBrush, pen, 0, "spacing", text, state);
                            curX += ft.WidthIncludingTrailingWhitespace;
                        }
                    }
                    else if (node is XElement childElem && string.Equals(childElem.Name.LocalName, "tspan", StringComparison.OrdinalIgnoreCase))
                    {
                        Dictionary<string, string> tAttrs = ResolveElementAttributes(childElem, cssRules);
                        SvgRenderState tState = MergeRenderState(state, tAttrs);
                        WpfBrush tFill = CreateFillBrush(tState, defBrushes);
                        WpfPen tPen = CreateStrokePen(tState, defBrushes);

                        if (tAttrs.TryGetValue("x", out string txStr) && !string.IsNullOrWhiteSpace(txStr))
                            curX = ParseDouble(txStr);
                        if (tAttrs.TryGetValue("y", out string tyStr) && !string.IsNullOrWhiteSpace(tyStr))
                            curY = ParseDouble(tyStr);
                        if (tAttrs.TryGetValue("dx", out string tdxStr) && !string.IsNullOrWhiteSpace(tdxStr))
                            curX += ParseDouble(tdxStr);
                        if (tAttrs.TryGetValue("dy", out string tdyStr) && !string.IsNullOrWhiteSpace(tdyStr))
                            curY += ParseDouble(tdyStr);

                        string tAnchor = GetAttrFromDict(tAttrs, "text-anchor") ?? tState.TextAnchor ?? anchor;
                        double tLength = ParseLength(GetAttrFromDict(tAttrs, "textLength"), 0);
                        string tLengthAdjust = GetAttrFromDict(tAttrs, "lengthAdjust") ?? lengthAdjust;

                        string text = CleanSvgText(childElem.Value, false);
                        if (!string.IsNullOrEmpty(text))
                        {
                            WpfFormattedText ft = CreateFormattedText(text, tState, tFill);
                            DrawFormattedSvgText(dc, ft, curX, curY, tAnchor, tFill, tPen, tLength, tLengthAdjust, text, tState);
                            curX += ft.WidthIncludingTrailingWhitespace;
                        }
                    }
                }
            }
        }

        private static WpfFormattedText CreateFormattedText(string text, SvgRenderState state, WpfBrush fillBrush)
        {
            string fontName = !string.IsNullOrWhiteSpace(state.FontFamily) ? state.FontFamily : "Arial, Segoe UI, sans-serif";
            string normalizedFont;
            if (fontName.IndexOf("sans", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                normalizedFont = fontName.Replace("sans-serif", "Arial, Segoe UI");
                if (!normalizedFont.Contains("Arial")) normalizedFont += ", Arial, Segoe UI";
            }
            else if (fontName.IndexOf("mono", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                normalizedFont = fontName.Replace("monospace", "Consolas, Courier New");
                if (!normalizedFont.Contains("Consolas")) normalizedFont += ", Consolas";
            }
            else
            {
                normalizedFont = fontName.Replace("serif", "Times New Roman");
                if (!normalizedFont.Contains("Times New Roman")) normalizedFont += ", Times New Roman, Arial";
            }

            var fontFamily = new System.Windows.Media.FontFamily(normalizedFont);

            System.Windows.FontWeight weight = System.Windows.FontWeights.Normal;
            if (!string.IsNullOrEmpty(state.FontWeight))
            {
                if (state.FontWeight.Equals("bold", StringComparison.OrdinalIgnoreCase) ||
                    state.FontWeight.Equals("bolder", StringComparison.OrdinalIgnoreCase) ||
                    state.FontWeight.Equals("700") || state.FontWeight.Equals("800") || state.FontWeight.Equals("900"))
                {
                    weight = System.Windows.FontWeights.Bold;
                }
            }

            System.Windows.FontStyle style = System.Windows.FontStyles.Normal;
            if (!string.IsNullOrEmpty(state.FontStyle))
            {
                if (state.FontStyle.Equals("italic", StringComparison.OrdinalIgnoreCase) ||
                    state.FontStyle.Equals("oblique", StringComparison.OrdinalIgnoreCase))
                {
                    style = System.Windows.FontStyles.Italic;
                }
            }

            double fontSize = state.FontSize > 0 ? state.FontSize : 16.0;

            var typeface = new System.Windows.Media.Typeface(fontFamily, style, weight, System.Windows.FontStretches.Normal);
            WpfBrush fg = fillBrush ?? System.Windows.Media.Brushes.Black;

            return new WpfFormattedText(
                text,
                CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                fontSize,
                fg,
                1.0);
        }

        private static void DrawFormattedSvgText(
            WpfDrawingContext dc,
            WpfFormattedText ft,
            double x,
            double y,
            string anchor,
            WpfBrush fillBrush,
            WpfPen pen,
            double textLength,
            string lengthAdjust,
            string rawText,
            SvgRenderState state)
        {
            if (ft == null) return;

            double drawX = x;
            double drawY = y - ft.Baseline;

            string a = (anchor ?? "start").ToLowerInvariant().Trim();
            if (a == "middle")
            {
                drawX -= ft.Width / 2.0;
            }
            else if (a == "end")
            {
                drawX -= ft.Width;
            }

            if (textLength > 0 && ft.Width > 0 && Math.Abs(textLength - ft.Width) > 0.5)
            {
                if (string.Equals(lengthAdjust, "spacingAndGlyphs", StringComparison.OrdinalIgnoreCase))
                {
                    double xfactor = textLength / ft.Width;
                    dc.PushTransform(new System.Windows.Media.ScaleTransform(xfactor, 1.0, drawX, drawY));
                    try
                    {
                        DrawSingleFormattedSvgText(dc, ft, drawX, drawY, fillBrush, pen);
                    }
                    finally
                    {
                        dc.Pop();
                    }
                    return;
                }
                else if (!string.IsNullOrEmpty(rawText) && rawText.Length > 1)
                {
                    double delta = (textLength - ft.Width) / (rawText.Length - 1);
                    double curCharX = drawX;
                    for (int i = 0; i < rawText.Length; i++)
                    {
                        string ch = rawText[i].ToString();
                        var charFt = CreateFormattedText(ch, state, fillBrush);
                        DrawSingleFormattedSvgText(dc, charFt, curCharX, drawY, fillBrush, pen);
                        curCharX += charFt.WidthIncludingTrailingWhitespace + delta;
                    }
                    return;
                }
            }

            DrawSingleFormattedSvgText(dc, ft, drawX, drawY, fillBrush, pen);
        }

        private static void DrawSingleFormattedSvgText(
            WpfDrawingContext dc,
            WpfFormattedText ft,
            double drawX,
            double drawY,
            WpfBrush fillBrush,
            WpfPen pen)
        {
            if (pen == null && fillBrush is WpfSolidColorBrush)
            {
                dc.DrawText(ft, new WpfPoint(drawX, drawY));
            }
            else
            {
                var geom = ft.BuildGeometry(new WpfPoint(drawX, drawY));
                if (geom != null)
                {
                    dc.DrawGeometry(fillBrush, pen, geom);
                }
                else if (fillBrush != null)
                {
                    dc.DrawText(ft, new WpfPoint(drawX, drawY));
                }
            }
        }

        private static string CleanSvgText(string input, bool isStandalone)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string res = Regex.Replace(input, @"[\r\n\t]+", " ");
            if (string.IsNullOrWhiteSpace(res)) return string.Empty;
            return isStandalone ? res.Trim() : res;
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

        private static WpfGeometry BuildClipGeometry(
            XElement clipElem,
            Dictionary<string, Dictionary<string, string>> cssRules,
            Dictionary<string, XElement> defsById = null)
        {
            WpfGeometryGroup group = new WpfGeometryGroup { FillRule = WpfFillRule.Nonzero };
            ExtractClipChildren(clipElem, group, cssRules, defsById, 0);
            if (group.Children.Count == 0) return null;
            group.Freeze();
            return group;
        }

        private static void ExtractClipChildren(
            XElement parent,
            WpfGeometryGroup group,
            Dictionary<string, Dictionary<string, string>> cssRules,
            Dictionary<string, XElement> defsById,
            int depth)
        {
            if (parent == null || depth > 8) return;

            foreach (XElement child in parent.Elements())
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
                    else if (string.Equals(localName, "use", StringComparison.OrdinalIgnoreCase))
                    {
                        string href = GetHrefAttribute(child);
                        if (!string.IsNullOrEmpty(href) && href.StartsWith("#") && defsById != null)
                        {
                            string refId = href.Substring(1).Trim();
                            if (defsById.TryGetValue(refId, out XElement refElem))
                            {
                                WpfGeometryGroup subGroup = new WpfGeometryGroup { FillRule = WpfFillRule.Nonzero };
                                ExtractClipChildren(new XElement("clipPath", refElem), subGroup, cssRules, defsById, depth + 1);
                                if (subGroup.Children.Count > 0)
                                {
                                    double ux = ParseDouble(GetAttrFromDict(attrs, "x"));
                                    double uy = ParseDouble(GetAttrFromDict(attrs, "y"));
                                    if (Math.Abs(ux) > 1e-6 || Math.Abs(uy) > 1e-6)
                                    {
                                        subGroup.Transform = new System.Windows.Media.TranslateTransform(ux, uy);
                                    }
                                    g = subGroup;
                                }
                            }
                        }
                    }
                    else if (string.Equals(localName, "g", StringComparison.OrdinalIgnoreCase))
                    {
                        ExtractClipChildren(child, group, cssRules, defsById, depth + 1);
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

            if (attrs.TryGetValue("mask", out string maskVal) && !string.IsNullOrWhiteSpace(maskVal))
            {
                string mid = ExtractUrlId(maskVal);
                if (!string.IsNullOrEmpty(mid)) s.MaskId = mid;
            }

            if (attrs.TryGetValue("font-family", out string ff) && !string.IsNullOrWhiteSpace(ff))
                s.FontFamily = ff.Trim();

            if (attrs.TryGetValue("font-size", out string fs) && !string.IsNullOrWhiteSpace(fs))
                s.FontSize = ParseLength(fs, s.FontSize);

            if (attrs.TryGetValue("font-weight", out string fw) && !string.IsNullOrWhiteSpace(fw))
                s.FontWeight = fw.Trim();

            if (attrs.TryGetValue("font-style", out string fst) && !string.IsNullOrWhiteSpace(fst))
                s.FontStyle = fst.Trim();

            if (attrs.TryGetValue("text-anchor", out string ta) && !string.IsNullOrWhiteSpace(ta))
                s.TextAnchor = ta.Trim();

            if (attrs.TryGetValue("stroke-miterlimit", out string sml) && !string.IsNullOrWhiteSpace(sml))
                s.StrokeMiterLimit = ParseDouble(sml);

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
                    {
                        lgb.StartPoint = mat.Transform(lgb.StartPoint);
                        lgb.EndPoint = mat.Transform(lgb.EndPoint);
                    }
                    else
                    {
                        lgb.RelativeTransform = new WpfMatrixTransform(mat);
                    }
                }
            }

            string spread = GetInheritedAttr(chain, "spreadMethod");
            if (!string.IsNullOrEmpty(spread))
            {
                if (string.Equals(spread, "reflect", StringComparison.OrdinalIgnoreCase))
                    lgb.SpreadMethod = System.Windows.Media.GradientSpreadMethod.Reflect;
                else if (string.Equals(spread, "repeat", StringComparison.OrdinalIgnoreCase))
                    lgb.SpreadMethod = System.Windows.Media.GradientSpreadMethod.Repeat;
                else
                    lgb.SpreadMethod = System.Windows.Media.GradientSpreadMethod.Pad;
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

            string rSpread = GetInheritedAttr(chain, "spreadMethod");
            if (!string.IsNullOrEmpty(rSpread))
            {
                if (string.Equals(rSpread, "reflect", StringComparison.OrdinalIgnoreCase))
                    rgb.SpreadMethod = System.Windows.Media.GradientSpreadMethod.Reflect;
                else if (string.Equals(rSpread, "repeat", StringComparison.OrdinalIgnoreCase))
                    rgb.SpreadMethod = System.Windows.Media.GradientSpreadMethod.Repeat;
                else
                    rgb.SpreadMethod = System.Windows.Media.GradientSpreadMethod.Pad;
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
                LineJoin = join,
                MiterLimit = state.StrokeMiterLimit > 0 ? state.StrokeMiterLimit : 4.0
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

        private static WpfBrush AdaptBrushToBoundingBox(WpfBrush brush, WpfRect bbox)
        {
            if (brush is WpfLinearGradientBrush lgb && lgb.MappingMode == WpfBrushMappingMode.RelativeToBoundingBox)
            {
                if (bbox.Width > 0 && bbox.Height > 0)
                {
                    double x1 = lgb.StartPoint.X, y1 = lgb.StartPoint.Y;
                    double x2 = lgb.EndPoint.X, y2 = lgb.EndPoint.Y;
                    double dx = x2 - x1;
                    double dy = y2 - y1;
                    double L2 = dx * dx + dy * dy;
                    if (L2 > 1e-9 && Math.Abs(dx) > 1e-5 && Math.Abs(dy) > 1e-5)
                    {
                        double A = dx / (bbox.Width * L2);
                        double B = dy / (bbox.Height * L2);
                        double denom = A * A + B * B;
                        if (denom > 1e-9)
                        {
                            double dPx = A / denom;
                            double dPy = B / denom;
                            double P1x = bbox.X + x1 * bbox.Width;
                            double P1y = bbox.Y + y1 * bbox.Height;

                            var adapted = lgb.Clone();
                            adapted.MappingMode = WpfBrushMappingMode.Absolute;
                            adapted.StartPoint = new WpfPoint(P1x, P1y);
                            adapted.EndPoint = new WpfPoint(P1x + dPx, P1y + dPy);
                            adapted.Freeze();
                            return adapted;
                        }
                    }
                    else if (L2 > 1e-9)
                    {
                        var adapted = lgb.Clone();
                        adapted.MappingMode = WpfBrushMappingMode.Absolute;
                        adapted.StartPoint = new WpfPoint(bbox.X + x1 * bbox.Width, bbox.Y + y1 * bbox.Height);
                        adapted.EndPoint = new WpfPoint(bbox.X + x2 * bbox.Width, bbox.Y + y2 * bbox.Height);
                        adapted.Freeze();
                        return adapted;
                    }
                }
            }
            return brush;
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

        private static System.Windows.Media.ImageSource LoadSvgImageSource(string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;
            try
            {
                href = href.Trim();
                if (href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    int base64Idx = href.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                    if (base64Idx >= 0)
                    {
                        string base64Str = href.Substring(base64Idx + 7).Trim();
                        if (base64Str.Contains("%"))
                        {
                            base64Str = Uri.UnescapeDataString(base64Str);
                        }
                        byte[] bytes = Convert.FromBase64String(base64Str);
                        using (var ms = new MemoryStream(bytes))
                        {
                            var bi = new System.Windows.Media.Imaging.BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            bi.StreamSource = ms;
                            bi.EndInit();
                            bi.Freeze();
                            return bi;
                        }
                    }
                }
                else
                {
                    string filePath = href;
                    if (filePath.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                    {
                        filePath = filePath.Substring(8).Replace('/', '\\');
                    }
                    if (File.Exists(filePath))
                    {
                        var bi = new System.Windows.Media.Imaging.BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bi.UriSource = new Uri(filePath, UriKind.Absolute);
                        bi.EndInit();
                        bi.Freeze();
                        return bi;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadSvgImageSource error: " + ex.Message);
            }
            return null;
        }

        private static System.Windows.Media.Imaging.BitmapSource CombineImageWithAlphaMask(
            System.Windows.Media.ImageSource colorSource,
            System.Windows.Media.ImageSource maskSource)
        {
            try
            {
                var cbmp = colorSource as System.Windows.Media.Imaging.BitmapSource;
                var mbmp = maskSource as System.Windows.Media.Imaging.BitmapSource;
                if (cbmp == null || mbmp == null) return null;

                int w = cbmp.PixelWidth;
                int h = cbmp.PixelHeight;
                if (w <= 0 || h <= 0) return null;

                var colorFormatted = new System.Windows.Media.Imaging.FormatConvertedBitmap(cbmp, WpfPixelFormats.Bgra32, null, 0);
                var maskFormatted = new System.Windows.Media.Imaging.FormatConvertedBitmap(mbmp, WpfPixelFormats.Bgra32, null, 0);

                int stride = w * 4;
                byte[] colorPixels = new byte[stride * h];
                colorFormatted.CopyPixels(colorPixels, stride, 0);

                byte[] maskPixels = new byte[stride * h];
                if (maskFormatted.PixelWidth == w && maskFormatted.PixelHeight == h)
                {
                    maskFormatted.CopyPixels(maskPixels, stride, 0);
                }
                else
                {
                    var scaledMask = new System.Windows.Media.Imaging.TransformedBitmap(maskFormatted,
                        new System.Windows.Media.ScaleTransform((double)w / maskFormatted.PixelWidth, (double)h / maskFormatted.PixelHeight));
                    var scaledFormatted = new System.Windows.Media.Imaging.FormatConvertedBitmap(scaledMask, WpfPixelFormats.Bgra32, null, 0);
                    scaledFormatted.CopyPixels(maskPixels, stride, 0);
                }

                byte[] resultPixels = new byte[stride * h];

                bool isAlphaOnlyMask = true;
                for (int i = 0; i < maskPixels.Length; i += 4)
                {
                    if (maskPixels[i] != 0 || maskPixels[i + 1] != 0 || maskPixels[i + 2] != 0)
                    {
                        isAlphaOnlyMask = false;
                        break;
                    }
                }

                for (int i = 0; i < colorPixels.Length; i += 4)
                {
                    byte b = colorPixels[i];
                    byte g = colorPixels[i + 1];
                    byte r = colorPixels[i + 2];
                    byte origA = colorPixels[i + 3];

                    // SVG mask luminance formula: Y = 0.2126 * R + 0.7152 * G + 0.0722 * B
                    byte mb = maskPixels[i];
                    byte mg = maskPixels[i + 1];
                    byte mr = maskPixels[i + 2];
                    byte ma = maskPixels[i + 3];

                    int lum;
                    if (isAlphaOnlyMask)
                    {
                        lum = ma;
                    }
                    else
                    {
                        lum = (int)(0.2126 * mr + 0.7152 * mg + 0.0722 * mb);
                        if (lum > 255) lum = 255;
                        lum = (lum * ma) / 255;
                    }
                    int finalAlpha = (lum * origA) / 255;
                    if (finalAlpha > 255) finalAlpha = 255;

                    byte pA = (byte)finalAlpha;
                    resultPixels[i] = (byte)((b * pA) / 255);
                    resultPixels[i + 1] = (byte)((g * pA) / 255);
                    resultPixels[i + 2] = (byte)((r * pA) / 255);
                    resultPixels[i + 3] = pA;
                }

                var resultBmp = System.Windows.Media.Imaging.BitmapSource.Create(
                    w, h, 96, 96, WpfPixelFormats.Pbgra32, null, resultPixels, stride);
                resultBmp.Freeze();
                return resultBmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CombineImageWithAlphaMask error: " + ex.Message);
                return null;
            }
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

            // Hỗ trợ rgb(...) và rgba(...) bao gồm cả đơn vị phần trăm rgb(100%, 0%, 0%)
            if (colorStr.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                int open = colorStr.IndexOf('(');
                int close = colorStr.LastIndexOf(')');
                if (open >= 0 && close > open)
                {
                    string inner = colorStr.Substring(open + 1, close - open - 1);
                    string[] parts = inner.Split(new char[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        double rVal = ParseColorChannel(parts[0], 255.0);
                        double gVal = ParseColorChannel(parts[1], 255.0);
                        double bVal = ParseColorChannel(parts[2], 255.0);
                        double aVal = (parts.Length >= 4) ? ParseAlphaChannel(parts[3]) : 1.0;
                        byte r = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(rVal)));
                        byte g = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(gVal)));
                        byte b = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(bVal)));
                        return ApplyOpacity(WpfColor.FromArgb(255, r, g, b), opacity * aVal);
                    }
                }
            }

            // Hỗ trợ hsl(...) và hsla(...)
            if (colorStr.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
            {
                int open = colorStr.IndexOf('(');
                int close = colorStr.LastIndexOf(')');
                if (open >= 0 && close > open)
                {
                    string inner = colorStr.Substring(open + 1, close - open - 1);
                    string[] parts = inner.Split(new char[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        double h = ParseColorChannel(parts[0], 360.0);
                        double s = ParseAlphaChannel(parts[1]);
                        double l = ParseAlphaChannel(parts[2]);
                        double a = (parts.Length >= 4) ? ParseAlphaChannel(parts[3]) : 1.0;
                        WpfColor rgb = HslToRgb(h, s, l);
                        return ApplyOpacity(rgb, opacity * a);
                    }
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

            // Named colors chuẩn SVG / CSS W3C (hỗ trợ tất cả 140 màu chuẩn)
            try
            {
                object converted = System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                if (converted is WpfColor convColor)
                {
                    return ApplyOpacity(convColor, opacity);
                }
            }
            catch { }

            try
            {
                Color sysCol = Color.FromName(colorStr);
                if (sysCol.IsKnownColor)
                {
                    return ApplyOpacity(WpfColor.FromArgb(sysCol.A, sysCol.R, sysCol.G, sysCol.B), opacity);
                }
            }
            catch { }

            return ApplyOpacity(WpfColor.FromArgb(255, 30, 41, 59), opacity);
        }

        private static double ParseColorChannel(string str, double maxVal)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;
            str = str.Trim();
            if (str.EndsWith("%"))
            {
                if (double.TryParse(str.Substring(0, str.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
                    return Math.Max(0, Math.Min(maxVal, pct / 100.0 * maxVal));
            }
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                return Math.Max(0, Math.Min(maxVal, val));
            return 0;
        }

        private static double ParseAlphaChannel(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 1.0;
            str = str.Trim();
            if (str.EndsWith("%"))
            {
                if (double.TryParse(str.Substring(0, str.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
                    return Math.Max(0.0, Math.Min(1.0, pct / 100.0));
            }
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                return Math.Max(0.0, Math.Min(1.0, val));
            return 1.0;
        }

        private static WpfColor HslToRgb(double h, double s, double l)
        {
            h = (h % 360.0 + 360.0) % 360.0;
            s = Math.Max(0.0, Math.Min(1.0, s));
            l = Math.Max(0.0, Math.Min(1.0, l));

            double r, g, b;
            if (s <= 1e-6)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
                double p = 2.0 * l - q;
                r = HueToRgb(p, q, h / 360.0 + 1.0 / 3.0);
                g = HueToRgb(p, q, h / 360.0);
                b = HueToRgb(p, q, h / 360.0 - 1.0 / 3.0);
            }

            byte R = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(r * 255.0)));
            byte G = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(g * 255.0)));
            byte B = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(b * 255.0)));
            return WpfColor.FromArgb(255, R, G, B);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0.0) t += 1.0;
            if (t > 1.0) t -= 1.0;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }

        private static double ExtractMaskUniformOpacity(XElement maskElem)
        {
            if (maskElem == null) return 1.0;
            if (maskElem.Descendants().Any(d => string.Equals(d.Name.LocalName, "image", StringComparison.OrdinalIgnoreCase)))
            {
                return 1.0;
            }

            if (maskElem.Descendants().Any(d =>
                string.Equals(d.Name.LocalName, "linearGradient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Name.LocalName, "radialGradient", StringComparison.OrdinalIgnoreCase)))
            {
                return 1.0;
            }

            var shapes = maskElem.Descendants().Where(d =>
            {
                string ln = d.Name.LocalName.ToLowerInvariant();
                return ln == "rect" || ln == "path" || ln == "circle" || ln == "polygon";
            }).ToList();

            if (shapes.Count != 1) return 1.0;

            XElement d = shapes[0];
            string fill = GetAttr(d, "fill");
            if (!string.IsNullOrEmpty(fill) && fill.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            double op = 1.0;
            string fo = GetAttr(d, "fill-opacity");
            if (!string.IsNullOrEmpty(fo)) op *= ParseCoordinate(fo, 1.0);

            string o = GetAttr(d, "opacity");
            if (!string.IsNullOrEmpty(o)) op *= ParseCoordinate(o, 1.0);

            string so = GetAttr(d, "stroke-opacity");
            if (!string.IsNullOrEmpty(so) && op >= 0.999) op = ParseCoordinate(so, 1.0);

            if (op < 0.999) return Math.Max(0.0, Math.Min(1.0, op));
            return 1.0;
        }

        private static System.Windows.Media.Brush CreateMaskOpacityBrush(
            XElement maskElem,
            Dictionary<string, WpfBrush> defBrushes,
            Dictionary<string, XElement> defsById,
            Dictionary<string, Dictionary<string, string>> cssRules)
        {
            if (maskElem == null) return null;
            try
            {
                XElement maskImgElem = maskElem.Descendants().FirstOrDefault(d => string.Equals(d.Name.LocalName, "image", StringComparison.OrdinalIgnoreCase));
                if (maskImgElem != null)
                {
                    string maskHref = GetAttr(maskImgElem, "href") ?? GetHrefAttribute(maskImgElem);
                    if (!string.IsNullOrEmpty(maskHref))
                    {
                        var maskSource = LoadSvgImageSource(maskHref);
                        if (maskSource != null)
                        {
                            var maskBmp = ConvertMaskToAlphaSource(maskSource);
                            if (maskBmp != null)
                            {
                                double mx = ParseDouble(GetAttr(maskImgElem, "x"));
                                double my = ParseDouble(GetAttr(maskImgElem, "y"));
                                double mw = ParseLength(GetAttr(maskImgElem, "width"), maskBmp.PixelWidth);
                                double mh = ParseLength(GetAttr(maskImgElem, "height"), maskBmp.PixelHeight);
                                if (mw <= 0) mw = maskBmp.PixelWidth;
                                if (mh <= 0) mh = maskBmp.PixelHeight;

                                var maskBrush = new System.Windows.Media.ImageBrush(maskBmp)
                                {
                                    Viewbox = new WpfRect(0, 0, maskBmp.PixelWidth, maskBmp.PixelHeight),
                                    ViewboxUnits = WpfBrushMappingMode.Absolute,
                                    Viewport = new WpfRect(mx, my, mw, mh),
                                    ViewportUnits = WpfBrushMappingMode.Absolute,
                                    Stretch = System.Windows.Media.Stretch.Fill
                                };

                                WpfMatrix combinedMat = WpfMatrix.Identity;
                                XElement cur = maskImgElem;
                                while (cur != null && cur != maskElem)
                                {
                                    string tStr = GetAttr(cur, "transform");
                                    if (!string.IsNullOrWhiteSpace(tStr))
                                    {
                                        WpfMatrix m = ParseSvgTransform(tStr);
                                        if (!m.IsIdentity)
                                        {
                                            combinedMat.Append(m);
                                        }
                                    }
                                    cur = cur.Parent;
                                }

                                if (!combinedMat.IsIdentity)
                                {
                                    maskBrush.Transform = new WpfMatrixTransform(combinedMat);
                                }

                                maskBrush.Freeze();
                                return maskBrush;
                            }
                        }
                    }
                }

                // Vector mask fallback: Render mask children into RenderTargetBitmap and convert luminance to alpha
                double maskX = 0, maskY = 0, maskW = 0, maskH = 0;
                string xStr = GetAttr(maskElem, "x");
                string yStr = GetAttr(maskElem, "y");
                string wStr = GetAttr(maskElem, "width");
                string hStr = GetAttr(maskElem, "height");
                if (!string.IsNullOrEmpty(xStr)) maskX = ParseDouble(xStr);
                if (!string.IsNullOrEmpty(yStr)) maskY = ParseDouble(yStr);
                if (!string.IsNullOrEmpty(wStr)) maskW = ParseDouble(wStr);
                if (!string.IsNullOrEmpty(hStr)) maskH = ParseDouble(hStr);

                if (maskW <= 0 || maskH <= 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    foreach (var d in maskElem.Descendants())
                    {
                        string ln = d.Name.LocalName.ToLowerInvariant();
                        if (ln == "rect")
                        {
                            double rx = ParseDouble(GetAttr(d, "x"));
                            double ry = ParseDouble(GetAttr(d, "y"));
                            double rw = ParseDouble(GetAttr(d, "width"));
                            double rh = ParseDouble(GetAttr(d, "height"));
                            if (rw > 0 && rh > 0)
                            {
                                minX = Math.Min(minX, rx);
                                minY = Math.Min(minY, ry);
                                maxX = Math.Max(maxX, rx + rw);
                                maxY = Math.Max(maxY, ry + rh);
                            }
                        }
                    }
                    if (maxX > minX && maxY > minY)
                    {
                        maskX = minX;
                        maskY = minY;
                        maskW = maxX - minX;
                        maskH = maxY - minY;
                    }
                    else
                    {
                        maskX = 0; maskY = 0; maskW = 2000; maskH = 2000;
                    }
                }

                int pw = (int)Math.Ceiling(maskW);
                int ph = (int)Math.Ceiling(maskH);
                if (pw <= 0 || ph <= 0) return null;

                WpfDrawingVisual dv = new WpfDrawingVisual();
                using (WpfDrawingContext dc = dv.RenderOpen())
                {
                    dc.PushTransform(new System.Windows.Media.TranslateTransform(-maskX, -maskY));
                    foreach (XElement child in maskElem.Elements())
                    {
                        RenderElementRecursive(dc, child, SvgRenderState.CreateDefault(), defBrushes, defsById, cssRules, 0);
                    }
                    dc.Pop();
                }

                WpfRenderTargetBitmap rtb = new WpfRenderTargetBitmap(pw, ph, 96, 96, WpfPixelFormats.Pbgra32);
                rtb.Render(dv);

                var alphaBmp = ConvertMaskToAlphaSource(rtb);
                if (alphaBmp == null) return null;

                var resBrush = new System.Windows.Media.ImageBrush(alphaBmp)
                {
                    Viewbox = new WpfRect(0, 0, pw, ph),
                    ViewboxUnits = WpfBrushMappingMode.Absolute,
                    Viewport = new WpfRect(maskX, maskY, maskW, maskH),
                    ViewportUnits = WpfBrushMappingMode.Absolute,
                    Stretch = System.Windows.Media.Stretch.Fill
                };
                resBrush.Freeze();
                return resBrush;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateMaskOpacityBrush error: " + ex.Message);
                return null;
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource ConvertMaskToAlphaSource(System.Windows.Media.ImageSource maskSource)
        {
            var mbmp = maskSource as System.Windows.Media.Imaging.BitmapSource;
            if (mbmp == null) return null;

            var formatted = new System.Windows.Media.Imaging.FormatConvertedBitmap(mbmp, WpfPixelFormats.Bgra32, null, 0);
            int w = formatted.PixelWidth;
            int h = formatted.PixelHeight;
            if (w <= 0 || h <= 0) return null;

            int stride = w * 4;
            byte[] pixels = new byte[stride * h];
            formatted.CopyPixels(pixels, stride, 0);

            bool isAlphaOnlyMask = true;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i] != 0 || pixels[i + 1] != 0 || pixels[i + 2] != 0)
                {
                    isAlphaOnlyMask = false;
                    break;
                }
            }

            byte[] outPixels = new byte[stride * h];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                int lum;
                if (isAlphaOnlyMask)
                {
                    lum = a;
                }
                else
                {
                    lum = (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                    if (lum > 255) lum = 255;
                    lum = (lum * a) / 255;
                }
                if (lum > 255) lum = 255;

                outPixels[i] = 255;
                outPixels[i + 1] = 255;
                outPixels[i + 2] = 255;
                outPixels[i + 3] = (byte)lum;
            }

            var res = System.Windows.Media.Imaging.BitmapSource.Create(
                w, h, 96, 96, WpfPixelFormats.Bgra32, null, outPixels, stride);
            res.Freeze();
            return res;
        }

        private static WpfColor ApplyOpacity(WpfColor c, double opacity)
        {
            byte a = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(c.A * opacity)));
            return WpfColor.FromArgb(a, c.R, c.G, c.B);
        }
    }
}


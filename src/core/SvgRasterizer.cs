using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
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

namespace GenOffice.Core
{
    /// <summary>
    /// Bộ chuyển đổi và kết xuất ảnh vector SVG sang Bitmap chất lượng cao bằng WPF Vector Geometry Engine,
    /// hỗ trợ đầy đủ LinearGradient, RadialGradient, Opacity, đa màu sắc và nét vẽ khử răng cưa.
    /// </summary>
    public static class SvgRasterizer
    {
        public static Bitmap RenderSvgToBitmap(string svgXml, int targetWidth = 1200, int targetHeight = 1200)
        {
            if (string.IsNullOrWhiteSpace(svgXml)) return null;

            try
            {
                XDocument doc = XDocument.Parse(svgXml);
                XElement root = doc.Root;
                if (root == null || root.Name.LocalName != "svg") return null;

                // 1. Xác định kích thước viewBox
                double vbX = 0, vbY = 0, vbW = 24, vbH = 24;
                string viewBoxAttr = (string)root.Attribute("viewBox");
                if (!string.IsNullOrEmpty(viewBoxAttr))
                {
                    string[] parts = viewBoxAttr.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out vbX);
                        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out vbY);
                        double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out vbW);
                        double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out vbH);
                    }
                }
                else
                {
                    double w, h;
                    if (double.TryParse((string)root.Attribute("width"), NumberStyles.Any, CultureInfo.InvariantCulture, out w) && w > 0) vbW = w;
                    if (double.TryParse((string)root.Attribute("height"), NumberStyles.Any, CultureInfo.InvariantCulture, out h) && h > 0) vbH = h;
                }

                if (vbW <= 0) vbW = 24;
                if (vbH <= 0) vbH = 24;

                double pad = (vbW <= 48 && vbH <= 48) ? (targetWidth * 0.04) : 0.0;
                double drawW = targetWidth - pad * 2.0;
                double drawH = targetHeight - pad * 2.0;
                double scale = Math.Min(drawW / vbW, drawH / vbH);

                string rootStroke = (string)root.Attribute("stroke");
                string rootFill = (string)root.Attribute("fill");
                double rootStrokeWidth = 0.0;
                string rswStr = (string)root.Attribute("stroke-width");
                if (!string.IsNullOrEmpty(rswStr)) double.TryParse(rswStr, NumberStyles.Any, CultureInfo.InvariantCulture, out rootStrokeWidth);

                // 2. Phân tích các định nghĩa Gradient trong <defs>
                Dictionary<string, WpfBrush> defBrushes = new Dictionary<string, WpfBrush>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement defElem in root.Descendants())
                {
                    string localName = defElem.Name.LocalName;
                    string id = (string)defElem.Attribute("id");
                    if (string.IsNullOrEmpty(id)) continue;

                    if (localName == "linearGradient")
                    {
                        double x1 = ParseCoordinate((string)defElem.Attribute("x1"), 0);
                        double y1 = ParseCoordinate((string)defElem.Attribute("y1"), 0);
                        double x2 = ParseCoordinate((string)defElem.Attribute("x2"), 1);
                        double y2 = ParseCoordinate((string)defElem.Attribute("y2"), 1);

                        WpfLinearGradientBrush lgb = new WpfLinearGradientBrush();
                        lgb.StartPoint = new WpfPoint(x1, y1);
                        lgb.EndPoint = new WpfPoint(x2, y2);
                        lgb.MappingMode = WpfBrushMappingMode.RelativeToBoundingBox;

                        foreach (XElement stopElem in defElem.Elements())
                        {
                            if (stopElem.Name.LocalName != "stop") continue;
                            double offset = ParseCoordinate((string)stopElem.Attribute("offset"), 0);
                            string colStr = (string)stopElem.Attribute("stop-color") ?? "#000000";
                            double op = 1.0;
                            string opStr = (string)stopElem.Attribute("stop-opacity");
                            if (!string.IsNullOrEmpty(opStr)) double.TryParse(opStr, NumberStyles.Any, CultureInfo.InvariantCulture, out op);

                            WpfColor c = ParseColor(colStr, op);
                            lgb.GradientStops.Add(new WpfGradientStop(c, offset));
                        }
                        lgb.Freeze();
                        defBrushes[id] = lgb;
                    }
                    else if (localName == "radialGradient")
                    {
                        double cx = ParseCoordinate((string)defElem.Attribute("cx"), 0.5);
                        double cy = ParseCoordinate((string)defElem.Attribute("cy"), 0.5);
                        double r = ParseCoordinate((string)defElem.Attribute("r"), 0.5);

                        WpfRadialGradientBrush rgb = new WpfRadialGradientBrush();
                        rgb.Center = new WpfPoint(cx, cy);
                        rgb.GradientOrigin = rgb.Center;
                        rgb.RadiusX = r;
                        rgb.RadiusY = r;
                        rgb.MappingMode = WpfBrushMappingMode.RelativeToBoundingBox;

                        foreach (XElement stopElem in defElem.Elements())
                        {
                            if (stopElem.Name.LocalName != "stop") continue;
                            double offset = ParseCoordinate((string)stopElem.Attribute("offset"), 0);
                            string colStr = (string)stopElem.Attribute("stop-color") ?? "#000000";
                            double op = 1.0;
                            string opStr = (string)stopElem.Attribute("stop-opacity");
                            if (!string.IsNullOrEmpty(opStr)) double.TryParse(opStr, NumberStyles.Any, CultureInfo.InvariantCulture, out op);

                            WpfColor c = ParseColor(colStr, op);
                            rgb.GradientStops.Add(new WpfGradientStop(c, offset));
                        }
                        rgb.Freeze();
                        defBrushes[id] = rgb;
                    }
                }

                // 3. Kết xuất đồ họa vector
                WpfDrawingVisual dv = new WpfDrawingVisual();
                using (WpfDrawingContext dc = dv.RenderOpen())
                {
                    double offsetX = (targetWidth - vbW * scale) / 2.0 - vbX * scale;
                    double offsetY = (targetHeight - vbH * scale) / 2.0 - vbY * scale;

                    dc.PushTransform(new System.Windows.Media.TranslateTransform(offsetX, offsetY));
                    dc.PushTransform(new System.Windows.Media.ScaleTransform(scale, scale));

                    foreach (XElement elem in root.Descendants())
                    {
                        string localName = elem.Name.LocalName;
                        if (localName == "svg" || localName == "g" || localName == "defs" || localName == "linearGradient" || localName == "radialGradient" || localName == "stop") continue;

                        string strokeStr = (string)elem.Attribute("stroke") ?? rootStroke;
                        string fillStr = (string)elem.Attribute("fill") ?? rootFill;
                        string swStr = (string)elem.Attribute("stroke-width");
                        double sw = rootStrokeWidth;
                        if (!string.IsNullOrEmpty(swStr)) double.TryParse(swStr, NumberStyles.Any, CultureInfo.InvariantCulture, out sw);

                        bool hasFill = (!string.IsNullOrEmpty(fillStr) && !fillStr.Equals("none", StringComparison.OrdinalIgnoreCase) && !fillStr.Equals("transparent", StringComparison.OrdinalIgnoreCase));
                        if (string.IsNullOrEmpty(strokeStr))
                        {
                            if (!hasFill && (localName == "path" || localName == "line" || localName == "polyline"))
                            {
                                strokeStr = "#1E293B";
                                if (sw <= 0) sw = 1.5;
                            }
                            else
                            {
                                strokeStr = null;
                            }
                        }
                        else if (strokeStr.Equals("none", StringComparison.OrdinalIgnoreCase) || strokeStr.Equals("transparent", StringComparison.OrdinalIgnoreCase))
                        {
                            strokeStr = null;
                        }

                        if (sw <= 0 && !string.IsNullOrEmpty(strokeStr)) sw = 1.0;

                        double elemOpacity = 1.0;
                        string elemOpStr = (string)elem.Attribute("opacity");
                        if (!string.IsNullOrEmpty(elemOpStr)) double.TryParse(elemOpStr, NumberStyles.Any, CultureInfo.InvariantCulture, out elemOpacity);

                        WpfBrush strokeBrush = (!string.IsNullOrEmpty(strokeStr) && sw > 0) ? ParseBrush(strokeStr, defBrushes, "#1E293B", elemOpacity) : null;
                        WpfBrush fillBrush = ParseBrush(fillStr, defBrushes, null, elemOpacity);

                        WpfPen pen = null;
                        if (strokeBrush != null && sw > 0)
                        {
                            pen = new WpfPen(strokeBrush, sw)
                            {
                                StartLineCap = WpfPenLineCap.Round,
                                EndLineCap = WpfPenLineCap.Round,
                                LineJoin = WpfPenLineJoin.Round
                            };
                        }

                        if (localName == "path")
                        {
                            string d = (string)elem.Attribute("d");
                            if (!string.IsNullOrEmpty(d))
                            {
                                try
                                {
                                    var geom = WpfGeometry.Parse(d);
                                    dc.DrawGeometry(fillBrush, pen, geom);
                                }
                                catch { }
                            }
                        }
                        else if (localName == "circle")
                        {
                            double cx = ParseDouble((string)elem.Attribute("cx"));
                            double cy = ParseDouble((string)elem.Attribute("cy"));
                            double r = ParseDouble((string)elem.Attribute("r"));
                            dc.DrawEllipse(fillBrush, pen, new WpfPoint(cx, cy), r, r);
                        }
                        else if (localName == "ellipse")
                        {
                            double cx = ParseDouble((string)elem.Attribute("cx"));
                            double cy = ParseDouble((string)elem.Attribute("cy"));
                            double rx = ParseDouble((string)elem.Attribute("rx"));
                            double ry = ParseDouble((string)elem.Attribute("ry"));
                            dc.DrawEllipse(fillBrush, pen, new WpfPoint(cx, cy), rx, ry);
                        }
                        else if (localName == "rect")
                        {
                            double x = ParseDouble((string)elem.Attribute("x"));
                            double y = ParseDouble((string)elem.Attribute("y"));
                            double w = ParseDouble((string)elem.Attribute("width"));
                            double h = ParseDouble((string)elem.Attribute("height"));
                            double rx = ParseDouble((string)elem.Attribute("rx"));
                            double ry = ParseDouble((string)elem.Attribute("ry"));
                            if (rx <= 0 && ry > 0) rx = ry;
                            if (ry <= 0 && rx > 0) ry = rx;
                            dc.DrawRoundedRectangle(fillBrush, pen, new WpfRect(x, y, w, h), rx, ry);
                        }
                        else if (localName == "line")
                        {
                            double x1 = ParseDouble((string)elem.Attribute("x1"));
                            double y1 = ParseDouble((string)elem.Attribute("y1"));
                            double x2 = ParseDouble((string)elem.Attribute("x2"));
                            double y2 = ParseDouble((string)elem.Attribute("y2"));
                            if (pen != null) dc.DrawLine(pen, new WpfPoint(x1, y1), new WpfPoint(x2, y2));
                        }
                        else if (localName == "polyline" || localName == "polygon")
                        {
                            string pts = (string)elem.Attribute("points");
                            if (!string.IsNullOrEmpty(pts))
                            {
                                try
                                {
                                    string d = "M " + pts + (localName == "polygon" ? " Z" : "");
                                    var geom = WpfGeometry.Parse(d);
                                    dc.DrawGeometry(fillBrush, pen, geom);
                                }
                                catch { }
                            }
                        }
                    }

                    dc.Pop(); // Scale
                    dc.Pop(); // Offset
                }

                WpfRenderTargetBitmap rtb = new WpfRenderTargetBitmap(targetWidth, targetHeight, 96, 96, WpfPixelFormats.Pbgra32);
                rtb.Render(dv);

                int stride = targetWidth * 4;
                byte[] pixelBytes = new byte[stride * targetHeight];
                rtb.CopyPixels(pixelBytes, stride, 0);

                Bitmap bmp = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppPArgb);
                var rect = new Rectangle(0, 0, targetWidth, targetHeight);
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

        private static double ParseDouble(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            double v;
            double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
            return v;
        }

        private static double ParseCoordinate(string str, double defaultVal)
        {
            if (string.IsNullOrEmpty(str)) return defaultVal;
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

        private static WpfBrush ParseBrush(string colorStr, Dictionary<string, WpfBrush> defs, string fallbackHex, double opacity = 1.0)
        {
            if (string.IsNullOrWhiteSpace(colorStr))
            {
                if (string.IsNullOrEmpty(fallbackHex)) return null;
                colorStr = fallbackHex;
            }
            colorStr = colorStr.Trim();
            if (colorStr.Equals("none", StringComparison.OrdinalIgnoreCase) || colorStr.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (colorStr.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            {
                colorStr = fallbackHex ?? "#1E293B";
            }

            if (colorStr.StartsWith("url(#") && colorStr.EndsWith(")"))
            {
                string id = colorStr.Substring(5, colorStr.Length - 6).Trim();
                if (defs != null && defs.TryGetValue(id, out WpfBrush b))
                    return b;
            }

            WpfColor c = ParseColor(colorStr, opacity);
            if (c.A == 0) return null;
            var brush = new WpfSolidColorBrush(c);
            brush.Freeze();
            return brush;
        }

        private static WpfColor ParseColor(string colorStr, double opacity = 1.0)
        {
            if (string.IsNullOrWhiteSpace(colorStr) || colorStr == "none")
                return WpfColor.FromArgb(0, 0, 0, 0);

            colorStr = colorStr.Trim();

            // Named colors
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
                case "gray": case "grey": return ApplyOpacity(WpfColor.FromRgb(100, 116, 139), opacity);
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
                    else if (hex.Length == 6)
                    {
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        return ApplyOpacity(WpfColor.FromArgb(255, r, g, b), opacity);
                    }
                    else if (hex.Length == 8)
                    {
                        byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                        return ApplyOpacity(WpfColor.FromArgb(a, r, g, b), opacity);
                    }
                }
            }
            catch { }

            return ApplyOpacity(WpfColor.FromArgb(255, 30, 41, 59), opacity);
        }

        private static WpfColor ApplyOpacity(WpfColor c, double opacity)
        {
            byte a = (byte)Math.Max(0, Math.Min(255, (int)(c.A * opacity)));
            return WpfColor.FromArgb(a, c.R, c.G, c.B);
        }
    }
}

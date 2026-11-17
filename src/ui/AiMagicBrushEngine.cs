using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bộ xử lý điểm ảnh & hình học cho Cọ Đa Năng AI:
    /// - Mặt nạ cọ (mask) mềm theo đúng nét vẽ
    /// - Hòa trộn 2 lớp ảnh (làm tan lớp trên vào lớp dưới theo vệt cọ)
    /// - Xóa vật thể giữ nền (inpainting lan tỏa từ biên)
    /// - Tách vùng đã tô thành lớp ảnh mới
    /// - Nhận diện nét phác thảo (RDP): đường thẳng, gấp khúc, cong, mũi tên, tam giác, chữ nhật, đa giác, ellipse
    /// </summary>
    public static class AiMagicBrushEngine
    {
        // =====================================================================================
        // HÌNH HỌC CƠ BẢN
        // =====================================================================================
        public static float Dist(PointF a, PointF b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float DistToSegment(PointF p, PointF a, PointF b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float l2 = dx * dx + dy * dy;
            if (l2 < 1e-6f) return Dist(p, a);
            float t = Math.Max(0f, Math.Min(1f, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2));
            return Dist(p, new PointF(a.X + t * dx, a.Y + t * dy));
        }

        /// <summary>Làm mượt Chaikin để nét cọ không bị bẻ góc.</summary>
        public static List<PointF> Smooth(List<PointF> pts, int iterations = 2)
        {
            var cur = Dedupe(pts, 0.5f);
            if (cur.Count < 3) return cur;
            for (int it = 0; it < iterations; it++)
            {
                var next = new List<PointF>(cur.Count * 2) { cur[0] };
                for (int i = 0; i < cur.Count - 1; i++)
                {
                    PointF a = cur[i], b = cur[i + 1];
                    next.Add(new PointF(0.75f * a.X + 0.25f * b.X, 0.75f * a.Y + 0.25f * b.Y));
                    next.Add(new PointF(0.25f * a.X + 0.75f * b.X, 0.25f * a.Y + 0.75f * b.Y));
                }
                next.Add(cur[cur.Count - 1]);
                cur = next;
            }
            return cur;
        }

        public static List<PointF> Dedupe(List<PointF> pts, float minDist)
        {
            var res = new List<PointF>();
            foreach (var p in pts)
                if (res.Count == 0 || Dist(res[res.Count - 1], p) >= minDist) res.Add(p);
            return res;
        }

        /// <summary>Ramer–Douglas–Peucker: rút gọn nét về các đỉnh góc thực sự.</summary>
        public static List<PointF> Simplify(List<PointF> pts, float eps)
        {
            if (pts.Count < 3) return new List<PointF>(pts);
            var keep = new bool[pts.Count];
            keep[0] = keep[pts.Count - 1] = true;
            var stack = new Stack<(int, int)>();
            stack.Push((0, pts.Count - 1));
            while (stack.Count > 0)
            {
                var (s, e) = stack.Pop();
                float maxD = 0; int idx = -1;
                for (int i = s + 1; i < e; i++)
                {
                    float d = DistToSegment(pts[i], pts[s], pts[e]);
                    if (d > maxD) { maxD = d; idx = i; }
                }
                if (idx >= 0 && maxD > eps)
                {
                    keep[idx] = true;
                    stack.Push((s, idx));
                    stack.Push((idx, e));
                }
            }
            var res = new List<PointF>();
            for (int i = 0; i < pts.Count; i++) if (keep[i]) res.Add(pts[i]);
            return res;
        }

        public static RectangleF Bounds(IEnumerable<PointF> pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
            }
            if (minX > maxX) return RectangleF.Empty;
            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }

        private static float TurnAngleDeg(PointF a, PointF b, PointF c)
        {
            double a1 = Math.Atan2(b.Y - a.Y, b.X - a.X);
            double a2 = Math.Atan2(c.Y - b.Y, c.X - b.X);
            double d = Math.Abs(a2 - a1) * 180.0 / Math.PI;
            if (d > 180) d = 360 - d;
            return (float)d;
        }

        /// <summary>Bắt góc đoạn thẳng về bội số 45° khi lệch nhỏ (vẽ tay vuông vắn).</summary>
        private static List<PointF> SnapPolyline(List<PointF> pts, float tolDeg = 10f)
        {
            var res = new List<PointF> { pts[0] };
            for (int i = 1; i < pts.Count; i++)
            {
                PointF prev = res[res.Count - 1];
                float dx = pts[i].X - pts[i - 1].X, dy = pts[i].Y - pts[i - 1].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                double ang = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                double snapped = Math.Round(ang / 45.0) * 45.0;
                if (Math.Abs(ang - snapped) <= tolDeg) ang = snapped;
                double rad = ang * Math.PI / 180.0;
                res.Add(new PointF(prev.X + (float)(Math.Cos(rad) * len), prev.Y + (float)(Math.Sin(rad) * len)));
            }
            return res;
        }

        // =====================================================================================
        // NHẬN DIỆN PHÁC THẢO
        // =====================================================================================
        public static bool Recognize(List<PointF> raw, SlideNode slide, out ShapeNode shape, out string kind)
        {
            shape = null; kind = "";
            if (raw == null || slide == null) return false;
            var pts = Dedupe(raw, 1.5f);
            if (pts.Count < 3) return false;
            RectangleF box = Bounds(pts);
            float diag = (float)Math.Sqrt(box.Width * box.Width + box.Height * box.Height);
            if (diag < 15f) return false;

            float pathLen = 0;
            for (int i = 1; i < pts.Count; i++) pathLen += Dist(pts[i - 1], pts[i]);
            float gap = Dist(pts[0], pts[pts.Count - 1]);
            float eps = Math.Max(5f, diag * 0.045f);
            bool closed = gap < Math.Max(18f, diag * 0.22f) && pathLen > diag * 1.6f;

            if (closed)
            {
                // Kiểm tra ellipse: khoảng cách chuẩn hóa tới tâm ≈ 1 đều mọi điểm
                float cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2;
                float rx = Math.Max(1f, box.Width / 2), ry = Math.Max(1f, box.Height / 2);
                double dev = 0;
                foreach (var p in pts)
                {
                    double nx = (p.X - cx) / rx, ny = (p.Y - cy) / ry;
                    dev += Math.Abs(Math.Sqrt(nx * nx + ny * ny) - 1.0);
                }
                dev /= pts.Count;

                var poly = Simplify(pts, eps);
                if (poly.Count > 2 && Dist(poly[0], poly[poly.Count - 1]) < Math.Max(18f, diag * 0.22f))
                    poly.RemoveAt(poly.Count - 1);
                // Loại các đỉnh gần thẳng hàng (vòng tròn quanh)
                bool removed = true;
                while (removed && poly.Count > 3)
                {
                    removed = false;
                    for (int i = 0; i < poly.Count; i++)
                    {
                        var a = poly[(i - 1 + poly.Count) % poly.Count];
                        var b = poly[i];
                        var c = poly[(i + 1) % poly.Count];
                        if (TurnAngleDeg(a, b, c) < 28f) { poly.RemoveAt(i); removed = true; break; }
                    }
                }

                // So sánh sai số tới đường ellipse và tới 4 cạnh khung chữ nhật (lấy mẫu đều theo chiều dài)
                var rs = Resample(pts, 64);
                double errE = 0, errR = 0;
                float md = Math.Max(1f, Math.Min(box.Width, box.Height));
                foreach (var p in rs)
                {
                    double ang = Math.Atan2((p.Y - cy) / ry, (p.X - cx) / rx);
                    double ex = cx + rx * Math.Cos(ang), ey = cy + ry * Math.Sin(ang);
                    errE += Math.Sqrt((p.X - ex) * (p.X - ex) + (p.Y - ey) * (p.Y - ey));
                    errR += Math.Min(Math.Min(Math.Abs(p.X - box.Left), Math.Abs(p.X - box.Right)), Math.Min(Math.Abs(p.Y - box.Top), Math.Abs(p.Y - box.Bottom)));
                }
                errE /= rs.Count * md; errR /= rs.Count * md;

                if (errE < 0.07 && errE < errR * 0.8 && poly.Count != 3)
                {
                    kind = "Ellipse";
                    shape = NewFilledShape(ShapeType.Ellipse, box);
                }
                else if (poly.Count == 3)
                {
                    kind = AppL10n.T("Tam giác", "Triangle");
                    shape = PathShape(poly, true, false, false);
                }
                else if (poly.Count == 4)
                {
                    bool axis = true;
                    for (int i = 0; i < 4; i++)
                    {
                        var a = poly[i]; var b = poly[(i + 1) % 4];
                        double ang = Math.Abs(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI) % 90;
                        if (ang > 14 && ang < 76) { axis = false; break; }
                    }
                    if (axis)
                    {
                        kind = AppL10n.T("Hình chữ nhật", "Rectangle");
                        shape = NewFilledShape(ShapeType.Rectangle, Bounds(poly));
                    }
                    else
                    {
                        kind = AppL10n.T("Tứ giác", "Quadrilateral");
                        shape = PathShape(poly, true, false, false);
                    }
                }
                else
                {
                    kind = AppL10n.T("Đa giác", "Polygon");
                    shape = PathShape(poly, true, false, false);
                }
            }
            else
            {
                var poly = Simplify(pts, eps);
                bool arrow = false;
                // Mũi tên: nét kết thúc bằng một cánh quay ngược ngắn (đầu mũi tên)
                if (poly.Count >= 3)
                {
                    int n = poly.Count;
                    float lastLen = Dist(poly[n - 2], poly[n - 1]);
                    float turn = TurnAngleDeg(poly[n - 3], poly[n - 2], poly[n - 1]);
                    if (turn > 125f && lastLen < pathLen * 0.3f)
                    {
                        arrow = true;
                        poly.RemoveAt(n - 1);
                    }
                }

                if (poly.Count == 2 || MaxDevFromChord(pts) < Dist(pts[0], pts[pts.Count - 1]) * 0.06f)
                {
                    var two = new List<PointF> { pts[0], poly[poly.Count - 1] };
                    kind = arrow ? AppL10n.T("Mũi tên thẳng", "Straight Arrow") : AppL10n.T("Đường thẳng", "Line");
                    shape = PathShape(SnapPolyline(two), false, false, arrow);
                }
                else
                {
                    // Gấp khúc: có góc gãy rõ (>35°) và các đoạn giữa các góc gần thẳng; ngược lại là đường cong
                    bool sharp = false;
                    for (int i = 1; i < poly.Count - 1; i++)
                        if (TurnAngleDeg(poly[i - 1], poly[i], poly[i + 1]) > 35f) { sharp = true; break; }
                    bool legsStraight = true;
                    if (sharp)
                    {
                        // kiểm tra điểm gốc nằm sát polyline đã rút gọn
                        float tol = Math.Max(4f, diag * 0.03f);
                        foreach (var p in pts)
                        {
                            float best = float.MaxValue;
                            for (int i = 1; i < poly.Count; i++) best = Math.Min(best, DistToSegment(p, poly[i - 1], poly[i]));
                            if (best > tol) { legsStraight = false; break; }
                        }
                    }
                    bool smoothCurve = !(sharp && legsStraight);

                    if (smoothCurve)
                    {
                        kind = arrow ? AppL10n.T("Mũi tên cong", "Curved Arrow") : AppL10n.T("Đường cong", "Curve");
                        shape = PathShape(Simplify(pts, eps * 0.5f), false, true, arrow);
                    }
                    else
                    {
                        kind = arrow ? AppL10n.T("Mũi tên gấp khúc", "Elbow Arrow") : AppL10n.T("Đường gấp khúc", "Polyline");
                        shape = PathShape(SnapPolyline(poly), false, false, arrow);
                    }
                }
            }

            if (shape == null) return false;
            shape.Name = AppL10n.T("Phác thảo - ", "Sketch - ") + kind;
            slide.AddShape(shape);
            slide.IsThumbnailDirty = true;
            return true;
        }

        private static float MaxDevFromChord(List<PointF> pts)
        {
            float m = 0;
            foreach (var p in pts) m = Math.Max(m, DistToSegment(p, pts[0], pts[pts.Count - 1]));
            return m;
        }

        private static List<PointF> Resample(List<PointF> pts, int n)
        {
            float total = 0;
            for (int i = 1; i < pts.Count; i++) total += Dist(pts[i - 1], pts[i]);
            var res = new List<PointF> { pts[0] };
            if (total <= 0) return res;
            float step = total / (n - 1), acc = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                PointF a = pts[i - 1], b = pts[i];
                float d = Dist(a, b);
                while (acc + d >= step && d > 0)
                {
                    float t = (step - acc) / d;
                    a = new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                    res.Add(a);
                    d = Dist(a, b); acc = 0;
                }
                acc += d;
            }
            return res;
        }

        private static ShapeNode NewFilledShape(ShapeType type, RectangleF b)
        {
            return new ShapeNode
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = type,
                Transform = new Transform2D(b.X, b.Y, Math.Max(8f, b.Width), Math.Max(8f, b.Height)),
                HasFill = true,
                FillColor = 0x3B82F6,
                FillOpacity = 0.15f,
                StrokeColor = 0x2563EB,
                StrokeWidth = 2.5f,
                StrokeOpacity = 1f,
                Text = ""
            };
        }

        private static ShapeNode PathShape(List<PointF> pts, bool closed, bool curve, bool arrowEnd)
        {
            var all = new List<PointF>(pts);
            PointF left = PointF.Empty, right = PointF.Empty, tip = pts[pts.Count - 1];
            if (arrowEnd)
            {
                PointF from = pts[pts.Count - 2];
                float dx = tip.X - from.X, dy = tip.Y - from.Y;
                float len = Math.Max(0.001f, (float)Math.Sqrt(dx * dx + dy * dy));
                dx /= len; dy /= len;
                float size = 16f, half = 9f;
                left = new PointF(tip.X - dx * size - dy * half, tip.Y - dy * size + dx * half);
                right = new PointF(tip.X - dx * size + dy * half, tip.Y - dy * size - dx * half);
                all.Add(left); all.Add(right);
            }
            RectangleF b = Bounds(all);
            float w = Math.Max(4f, b.Width), h = Math.Max(4f, b.Height);
            PointF L(PointF p) => new PointF(p.X - b.X, p.Y - b.Y);

            var path = new GraphicsPath();
            var local = pts.Select(L).ToArray();
            if (curve && local.Length >= 3) path.AddCurve(local, 0.5f);
            else path.AddLines(local);
            if (closed) path.CloseFigure();
            if (arrowEnd)
            {
                path.StartFigure();
                path.AddLines(new[] { L(left), L(tip), L(right) });
            }

            return new ShapeNode
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = ShapeType.CustomPath,
                CustomPath = path,
                PathWidth = w,
                PathHeight = h,
                Transform = new Transform2D(b.X, b.Y, w, h),
                HasFill = closed,
                FillColor = closed ? 0x3B82F6 : 0x00FFFFFF,
                FillOpacity = closed ? 0.15f : 0f,
                StrokeColor = 0x2563EB,
                StrokeWidth = 3f,
                StrokeOpacity = 1f,
                Text = ""
            };
        }

        // =====================================================================================
        // ẢNH & MẶT NẠ
        // =====================================================================================
        public static RectangleF ShapeBounds(ShapeNode s) =>
            s?.Transform == null ? RectangleF.Empty : new RectangleF(s.Transform.X, s.Transform.Y, s.Transform.Width, s.Transform.Height);

        public static bool IsBitmapShape(ShapeNode s) =>
            s != null && (s.Type == ShapeType.Image || s.CachedImage != null) && !s.IsSvg;

        public static Bitmap LoadBitmap(ShapeNode s)
        {
            Image src = s.CachedImage;
            bool dispose = false;
            if (src == null && !string.IsNullOrEmpty(s.ImagePath) && File.Exists(s.ImagePath))
            {
                try { src = Image.FromFile(s.ImagePath); dispose = true; } catch { }
            }
            if (src == null) return null;
            var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            if (dispose) src.Dispose();
            return bmp;
        }

        public static int[] ReadPixels(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new int[bmp.Width * bmp.Height];
            for (int y = 0; y < bmp.Height; y++)
                Marshal.Copy(data.Scan0 + y * data.Stride, px, y * bmp.Width, bmp.Width);
            bmp.UnlockBits(data);
            return px;
        }

        public static Bitmap WritePixels(int[] px, int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < h; y++)
                Marshal.Copy(px, y * w, data.Scan0 + y * data.Stride, w);
            bmp.UnlockBits(data);
            return bmp;
        }

        /// <summary>
        /// Dựng mặt nạ 0..1 trong hệ pixel của ảnh, đúng hình dạng nét cọ (đường kính = 2 × bán kính),
        /// sau đó làm mềm biên bằng box-blur 3 lượt (xấp xỉ Gaussian).
        /// </summary>
        public static float[] BuildMask(int w, int h, RectangleF imgB, IEnumerable<MagicBrushStroke> strokes, float featherPx, out Rectangle bbox)
        {
            float sx = w / Math.Max(1f, imgB.Width), sy = h / Math.Max(1f, imgB.Height);
            var mask = new float[w * h];
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Black);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    foreach (var st in strokes)
                    {
                        var sp = Smooth(st.SlidePoints, 2).Select(p => new PointF((p.X - imgB.X) * sx, (p.Y - imgB.Y) * sy)).ToArray();
                        float width = Math.Max(1f, 2f * st.Radius * (sx + sy) * 0.5f);
                        if (sp.Length == 0) continue;
                        if (sp.Length == 1)
                        {
                            g.FillEllipse(Brushes.White, sp[0].X - width / 2, sp[0].Y - width / 2, width, width);
                            continue;
                        }
                        using (var pen = new Pen(Color.White, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            g.DrawLines(pen, sp);
                    }
                }
                var px = ReadPixels(bmp);
                for (int i = 0; i < px.Length; i++) mask[i] = ((px[i] >> 16) & 0xFF) / 255f;
            }

            bbox = MaskBounds(mask, w, h, 0.004f);
            if (bbox.IsEmpty) return mask;
            if (featherPx >= 1f)
            {
                int r = Math.Max(1, (int)(featherPx / 2f));
                Rectangle region = Rectangle.Intersect(new Rectangle(0, 0, w, h), Rectangle.Inflate(bbox, r * 4, r * 4));
                for (int pass = 0; pass < 3; pass++) BoxBlur(mask, w, region, r);
                bbox = MaskBounds(mask, w, h, 0.004f);
            }
            return mask;
        }

        private static Rectangle MaskBounds(float[] m, int w, int h, float thr)
        {
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                    if (m[row + x] > thr)
                    {
                        if (x < x0) x0 = x; if (x > x1) x1 = x;
                        if (y < y0) y0 = y; if (y > y1) y1 = y;
                    }
            }
            return x1 < 0 ? Rectangle.Empty : Rectangle.FromLTRB(x0, y0, x1 + 1, y1 + 1);
        }

        private static void BoxBlur(float[] m, int w, Rectangle reg, int r)
        {
            var tmp = new float[reg.Width];
            for (int y = reg.Top; y < reg.Bottom; y++)
            {
                int row = y * w;
                float sum = 0; int cnt = 0;
                for (int x = reg.Left - r; x <= reg.Left + r; x++) if (x >= reg.Left && x < reg.Right) { sum += m[row + x]; cnt++; }
                for (int x = reg.Left; x < reg.Right; x++)
                {
                    tmp[x - reg.Left] = sum / cnt;
                    int xo = x - r, xi = x + r + 1;
                    if (xo >= reg.Left) { sum -= m[row + xo]; cnt--; }
                    if (xi < reg.Right) { sum += m[row + xi]; cnt++; }
                }
                Array.Copy(tmp, 0, m, row + reg.Left, reg.Width);
            }
            var col = new float[reg.Height];
            for (int x = reg.Left; x < reg.Right; x++)
            {
                float sum = 0; int cnt = 0;
                for (int y = reg.Top - r; y <= reg.Top + r; y++) if (y >= reg.Top && y < reg.Bottom) { sum += m[y * w + x]; cnt++; }
                for (int y = reg.Top; y < reg.Bottom; y++)
                {
                    col[y - reg.Top] = sum / cnt;
                    int yo = y - r, yi = y + r + 1;
                    if (yo >= reg.Top) { sum -= m[yo * w + x]; cnt--; }
                    if (yi < reg.Bottom) { sum += m[yi * w + x]; cnt++; }
                }
                for (int y = reg.Top; y < reg.Bottom; y++) m[y * w + x] = col[y - reg.Top];
            }
        }

        /// <summary>
        /// Inpainting lan tỏa từ biên (onion-peel): mỗi vòng lấp các pixel chưa biết bằng trung bình
        /// có trọng số của láng giềng đã biết, sau đó khuếch tán vài lượt để xóa vệt — nền được "mọc" lại.
        /// </summary>
        public static void Inpaint(int[] px, int w, int h, bool[] hole, Rectangle bb)
        {
            Rectangle reg = Rectangle.Intersect(new Rectangle(0, 0, w, h), Rectangle.Inflate(bb, 2, 2));
            var unknown = (bool[])hole.Clone();
            var updates = new List<(int idx, int color)>();
            int[] ox = { -1, 1, 0, 0, -1, 1, -1, 1 };
            int[] oy = { 0, 0, -1, 1, -1, -1, 1, 1 };
            float[] wt = { 1f, 1f, 1f, 1f, 0.7f, 0.7f, 0.7f, 0.7f };
            int remaining = 0;
            for (int y = reg.Top; y < reg.Bottom; y++) for (int x = reg.Left; x < reg.Right; x++) if (unknown[y * w + x]) remaining++;
            int guard = 0;
            while (remaining > 0 && guard++ < 4000)
            {
                updates.Clear();
                for (int y = reg.Top; y < reg.Bottom; y++)
                    for (int x = reg.Left; x < reg.Right; x++)
                    {
                        int i = y * w + x;
                        if (!unknown[i]) continue;
                        float a = 0, r = 0, g = 0, b = 0, ws = 0;
                        for (int k = 0; k < 8; k++)
                        {
                            int nx = x + ox[k], ny = y + oy[k];
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            int j = ny * w + nx;
                            if (unknown[j]) continue;
                            int c = px[j];
                            a += ((c >> 24) & 0xFF) * wt[k]; r += ((c >> 16) & 0xFF) * wt[k];
                            g += ((c >> 8) & 0xFF) * wt[k]; b += (c & 0xFF) * wt[k]; ws += wt[k];
                        }
                        if (ws > 0)
                            updates.Add((i, ((int)(a / ws) << 24) | ((int)(r / ws) << 16) | ((int)(g / ws) << 8) | (int)(b / ws)));
                    }
                if (updates.Count == 0) break;
                foreach (var (idx, color) in updates) { px[idx] = color; unknown[idx] = false; }
                remaining -= updates.Count;
            }

            // Khuếch tán làm mịn trong vùng lỗ để không còn vệt sọc
            for (int pass = 0; pass < 6; pass++)
            {
                var copy = (int[])px.Clone();
                for (int y = reg.Top; y < reg.Bottom; y++)
                    for (int x = reg.Left; x < reg.Right; x++)
                    {
                        int i = y * w + x;
                        if (!hole[i]) continue;
                        int a = 0, r = 0, g = 0, b = 0, n = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                                int c = copy[ny * w + nx];
                                a += (c >> 24) & 0xFF; r += (c >> 16) & 0xFF; g += (c >> 8) & 0xFF; b += c & 0xFF; n++;
                            }
                        px[i] = ((a / n) << 24) | ((r / n) << 16) | ((g / n) << 8) | (b / n);
                    }
            }
        }

        public static void CommitBitmap(ShapeNode shape, Bitmap bmp)
        {
            try { shape.InvalidateFeatherCache(); } catch { }
            shape.NativeD2DBitmap = null;
            shape.NativeD2DOwnerTarget = IntPtr.Zero;
            shape.NativeGifFrames = null;
            shape.IsGifPlaying = false;
            shape.ImagePath = "";
            shape.CachedImage = bmp;
            shape.InvalidateDisplayCache();
        }
    }
}

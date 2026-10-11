using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Vortice.Direct2D1;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Phân loại hình thái uốn cong của điểm neo vector theo chuẩn Inkscape/SVG.
    /// </summary>
    public enum VectorNodeType
    {
        /// <summary>
        /// Điểm neo góc nhọn (Corner / Cusp): Hai tay nắm tiếp tuyến hoạt động độc lập về cả góc và độ dài.
        /// </summary>
        Corner = 0,
        Cusp = 0,

        /// <summary>
        /// Điểm neo uốn mượt (Smooth): Hai tay nắm luôn thẳng hàng (180 độ), độ dài độc lập.
        /// </summary>
        Smooth = 1,

        /// <summary>
        /// Điểm neo đối xứng (Symmetric): Hai tay nắm thẳng hàng 180 độ VÀ có độ dài luôn bằng nhau.
        /// </summary>
        Symmetric = 2,

        /// <summary>
        /// Điểm neo tự động mượt (Auto-smooth): Tay nắm tự động nội suy tiếp tuyến theo giải thuật Catmull-Rom.
        /// </summary>
        AutoSmooth = 3
    }

    /// <summary>
    /// Đại diện cho một điểm neo trên đường cong Bezier bậc 3, bao gồm tọa độ neo và cặp tay nắm tiếp tuyến.
    /// </summary>
    public class VectorPathNode
    {
        public PointF Point { get; set; }
        public PointF InHandle { get; set; }
        public bool HasInHandle { get; set; } = false;
        public PointF OutHandle { get; set; }
        public bool HasOutHandle { get; set; } = false;
        public VectorNodeType NodeType { get; set; } = VectorNodeType.Corner;
        public bool IsSelected { get; set; } = false;

        public VectorPathNode() { }

        public VectorPathNode(PointF pt, VectorNodeType type = VectorNodeType.Corner)
        {
            Point = pt;
            InHandle = pt;
            OutHandle = pt;
            HasInHandle = false;
            HasOutHandle = false;
            NodeType = type;
        }

        public VectorPathNode(PointF pt, PointF inHandle, PointF outHandle, VectorNodeType type = VectorNodeType.Smooth)
        {
            Point = pt;
            InHandle = inHandle;
            HasInHandle = (inHandle.X != pt.X || inHandle.Y != pt.Y);
            OutHandle = outHandle;
            HasOutHandle = (outHandle.X != pt.X || outHandle.Y != pt.Y);
            NodeType = type;
        }

        public VectorPathNode Clone()
        {
            return new VectorPathNode
            {
                Point = this.Point,
                InHandle = this.InHandle,
                HasInHandle = this.HasInHandle,
                OutHandle = this.OutHandle,
                HasOutHandle = this.HasOutHandle,
                NodeType = this.NodeType,
                IsSelected = this.IsSelected
            };
        }

        public void Translate(float dx, float dy)
        {
            Point = new PointF(Point.X + dx, Point.Y + dy);
            InHandle = new PointF(InHandle.X + dx, InHandle.Y + dy);
            OutHandle = new PointF(OutHandle.X + dx, OutHandle.Y + dy);
        }

        public void ResetHandles()
        {
            InHandle = Point;
            OutHandle = Point;
            HasInHandle = false;
            HasOutHandle = false;
        }
    }

    /// <summary>
    /// Đại diện cho một phân đoạn liên tục của đường dẫn vector (có thể khép kín hoặc mở).
    /// </summary>
    public class VectorSubPath
    {
        public List<VectorPathNode> Nodes { get; set; } = new List<VectorPathNode>();
        public bool IsClosed { get; set; } = true;

        public VectorSubPath() { }

        public VectorSubPath(bool isClosed)
        {
            IsClosed = isClosed;
        }

        public VectorSubPath Clone()
        {
            var sp = new VectorSubPath(IsClosed);
            foreach (var node in Nodes)
            {
                sp.Nodes.Add(node.Clone());
            }
            return sp;
        }

        /// <summary>
        /// Đảo ngược thứ tự các điểm neo trong phân đoạn đường dẫn và hoán đổi cặp tay nắm tiếp tuyến (InHandle <-> OutHandle)
        /// của từng điểm neo để bảo toàn hướng tiếp tuyến Bezier.
        /// </summary>
        public void Reverse()
        {
            if (Nodes == null || Nodes.Count == 0) return;
            Nodes.Reverse();
            foreach (var node in Nodes)
            {
                (node.InHandle, node.OutHandle) = (node.OutHandle, node.InHandle);
                (node.HasInHandle, node.HasOutHandle) = (node.HasOutHandle, node.HasInHandle);
            }
        }

        public void AppendToGraphicsPath(GraphicsPath gp)
        {
            if (Nodes == null || Nodes.Count == 0) return;
            if (Nodes.Count == 1) return;

            int count = Nodes.Count;
            int segCount = IsClosed ? count : count - 1;

            for (int i = 0; i < segCount; i++)
            {
                var n1 = Nodes[i];
                var n2 = Nodes[(i + 1) % count];

                PointF p0 = n1.Point;
                PointF p3 = n2.Point;

                bool isCurve = n1.HasOutHandle || n2.HasInHandle;
                if (!isCurve)
                {
                    gp.AddLine(p0, p3);
                }
                else
                {
                    PointF p1 = n1.HasOutHandle ? n1.OutHandle : new PointF(p0.X + (p3.X - p0.X) / 3f, p0.Y + (p3.Y - p0.Y) / 3f);
                    PointF p2 = n2.HasInHandle ? n2.InHandle : new PointF(p3.X - (p3.X - p0.X) / 3f, p3.Y - (p3.Y - p0.Y) / 3f);
                    gp.AddBezier(p0, p1, p2, p3);
                }
            }

            if (IsClosed)
            {
                gp.CloseFigure();
            }
        }

        public RectangleF GetBounds()
        {
            if (Nodes.Count == 0) return RectangleF.Empty;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var n in Nodes)
            {
                minX = Math.Min(minX, n.Point.X);
                minY = Math.Min(minY, n.Point.Y);
                maxX = Math.Max(maxX, n.Point.X);
                maxY = Math.Max(maxY, n.Point.Y);

                if (n.HasInHandle)
                {
                    minX = Math.Min(minX, n.InHandle.X);
                    minY = Math.Min(minY, n.InHandle.Y);
                    maxX = Math.Max(maxX, n.InHandle.X);
                    maxY = Math.Max(maxY, n.InHandle.Y);
                }
                if (n.HasOutHandle)
                {
                    minX = Math.Min(minX, n.OutHandle.X);
                    minY = Math.Min(minY, n.OutHandle.Y);
                    maxX = Math.Max(maxX, n.OutHandle.X);
                    maxY = Math.Max(maxY, n.OutHandle.Y);
                }
            }
            return new RectangleF(minX, minY, Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY));
        }
    }

    /// <summary>
    /// Động cơ hình học giải tích và các thuật toán xử lý đường cong Bezier bậc 3 cho Node Tool.
    /// </summary>
    public static class VectorPathModel
    {
        public const float Kappa = 0.55228475f; // (4/3)*(sqrt(2)-1)

        /// <summary>
        /// Đảo ngược phân đoạn đường dẫn vector: đảo ngược danh sách điểm neo và hoán đổi tay nắm In/Out để giữ nguyên tiếp tuyến Bezier.
        /// </summary>
        public static void ReverseSubPath(VectorSubPath sp)
        {
            if (sp == null) return;
            sp.Reverse();
        }

        // ==========================================
        // 1. OBJECT TO PATH CONVERSIONS
        // ==========================================

        public static List<VectorSubPath> CreateSubPathsFromShape(VectorShapeType type, RectangleF r, float radius = 12f, int pointCount = 5)
        {
            switch (type)
            {
                case VectorShapeType.Rectangle:
                    if (radius > 0)
                    {
                        using (var gp = PhotoDocument.CreateShapePath(type, r, radius, pointCount))
                        {
                            return ConvertGraphicsPathToSubPaths(gp);
                        }
                    }
                    return CreateRectangleSubPaths(r);

                case VectorShapeType.Ellipse:
                    return CreateEllipseSubPaths(r);

                default:
                    using (var gp = PhotoDocument.CreateShapePath(type, r, radius, pointCount))
                    {
                        return ConvertGraphicsPathToSubPaths(gp);
                    }
            }
        }

        public static List<VectorSubPath> CreateRectangleSubPaths(RectangleF r)
        {
            var sp = new VectorSubPath(isClosed: true);
            sp.Nodes.Add(new VectorPathNode(new PointF(r.Left, r.Top), VectorNodeType.Corner));
            sp.Nodes.Add(new VectorPathNode(new PointF(r.Right, r.Top), VectorNodeType.Corner));
            sp.Nodes.Add(new VectorPathNode(new PointF(r.Right, r.Bottom), VectorNodeType.Corner));
            sp.Nodes.Add(new VectorPathNode(new PointF(r.Left, r.Bottom), VectorNodeType.Corner));
            return new List<VectorSubPath> { sp };
        }

        public static List<VectorSubPath> CreateEllipseSubPaths(RectangleF r)
        {
            var sp = new VectorSubPath(isClosed: true);
            float cx = r.Left + r.Width / 2f;
            float cy = r.Top + r.Height / 2f;
            float rx = r.Width / 2f;
            float ry = r.Height / 2f;
            float dx = rx * Kappa;
            float dy = ry * Kappa;

            // 0: Đỉnh trên (12 giờ)
            sp.Nodes.Add(new VectorPathNode(
                new PointF(cx, cy - ry),
                new PointF(cx - dx, cy - ry),
                new PointF(cx + dx, cy - ry),
                VectorNodeType.Symmetric));

            // 1: Bên phải (3 giờ)
            sp.Nodes.Add(new VectorPathNode(
                new PointF(cx + rx, cy),
                new PointF(cx + rx, cy - dy),
                new PointF(cx + rx, cy + dy),
                VectorNodeType.Symmetric));

            // 2: Đáy dưới (6 giờ)
            sp.Nodes.Add(new VectorPathNode(
                new PointF(cx, cy + ry),
                new PointF(cx + dx, cy + ry),
                new PointF(cx - dx, cy + ry),
                VectorNodeType.Symmetric));

            // 3: Bên trái (9 giờ)
            sp.Nodes.Add(new VectorPathNode(
                new PointF(cx - rx, cy),
                new PointF(cx - rx, cy + dy),
                new PointF(cx - rx, cy - dy),
                VectorNodeType.Symmetric));

            return new List<VectorSubPath> { sp };
        }

        public static List<VectorSubPath> ConvertGraphicsPathToSubPaths(GraphicsPath gp)
        {
            var subpaths = new List<VectorSubPath>();
            if (gp == null || gp.PointCount == 0) return subpaths;

            PointF[] pts = gp.PathPoints;
            byte[] types = gp.PathTypes;

            VectorSubPath currentSub = null;
            int i = 0;
            while (i < pts.Length)
            {
                byte bType = (byte)(types[i] & 0x07);
                bool isClose = (types[i] & 0x80) != 0;

                if (bType == 0) // PathPointType.Start
                {
                    currentSub = new VectorSubPath(isClosed: false);
                    currentSub.Nodes.Add(new VectorPathNode(pts[i], VectorNodeType.Corner));
                    subpaths.Add(currentSub);
                    i++;
                }
                else if (bType == 1) // PathPointType.Line
                {
                    if (currentSub != null)
                    {
                        currentSub.Nodes.Add(new VectorPathNode(pts[i], VectorNodeType.Corner));
                        if (isClose) currentSub.IsClosed = true;
                    }
                    i++;
                }
                else if (bType == 3 && i + 2 < pts.Length) // PathPointType.Bezier
                {
                    if (currentSub != null && currentSub.Nodes.Count > 0)
                    {
                        var prevNode = currentSub.Nodes[currentSub.Nodes.Count - 1];
                        prevNode.OutHandle = pts[i];
                        prevNode.HasOutHandle = true;

                        var nextNode = new VectorPathNode(pts[i + 2], VectorNodeType.Smooth);
                        nextNode.InHandle = pts[i + 1];
                        nextNode.HasInHandle = true;
                        currentSub.Nodes.Add(nextNode);

                        if ((types[i + 2] & 0x80) != 0) currentSub.IsClosed = true;
                    }
                    i += 3;
                }
                else
                {
                    i++;
                }
            }
            return subpaths;
        }

        public static bool ObjectToPath(PhotoLayer layer)
        {
            if (layer == null) return false;
            if (layer.Type != LayerType.VectorShape) return false;

            EnsureNodesInitialized(layer);
            layer.ShapeType = VectorShapeType.Path;
            if (layer.SubPaths != null)
            {
                foreach (var sp in layer.SubPaths)
                {
                    if (sp.Nodes != null)
                    {
                        foreach (var n in sp.Nodes) n.IsSelected = false;
                    }
                }
            }
            UpdateLayerBounds(layer);
            return true;
        }

        // ==========================================
        // 2. STROKE TO PATH CONVERSION
        // ==========================================

        public static bool StrokeToPath(PhotoLayer layer)
        {
            if (layer == null) return false;
            using (var originalPath = layer.BuildGraphicsPath())
            {
                if (originalPath == null || originalPath.PointCount == 0) return false;

                using (Pen widenPen = new Pen(Color.Black, Math.Max(1f, layer.StrokeWidth)))
                {
                    widenPen.StartCap = layer.StrokeLineCap;
                    widenPen.EndCap = layer.StrokeLineCap;
                    widenPen.LineJoin = layer.StrokeLineJoin;
                    widenPen.DashStyle = layer.StrokeDashStyle;

                    using (GraphicsPath outline = (GraphicsPath)originalPath.Clone())
                    {
                        outline.Widen(widenPen);
                        var widenedSubPaths = ConvertGraphicsPathToSubPaths(outline);
                        if (widenedSubPaths.Count == 0) return false;

                        layer.SubPaths = widenedSubPaths;
                        layer.ShapeType = VectorShapeType.Path;
                        layer.FillColor = layer.StrokeColor;
                        layer.FillType = VectorFillType.Solid;
                        layer.StrokeType = VectorStrokeType.None;
                        layer.StrokeWidth = 0;
                        if (layer.Nodes != null)
                        {
                            foreach (var n in layer.Nodes) n.IsSelected = true;
                        }
                        UpdateLayerBounds(layer);
                        return true;
                    }
                }
            }
        }

        // ==========================================
        // 3. HANDLE SYNCHRONIZATION ALGORITHMS
        // ==========================================

        public static void SynchronizeHandles(VectorPathNode node, bool isDraggingOutHandle)
        {
            if (node == null || node.NodeType == VectorNodeType.Corner) return;

            PointF P = node.Point;
            PointF activeH = isDraggingOutHandle ? node.OutHandle : node.InHandle;
            float vx = activeH.X - P.X;
            float vy = activeH.Y - P.Y;
            float lenActive = (float)Math.Sqrt(vx * vx + vy * vy);

            if (node.NodeType == VectorNodeType.Symmetric)
            {
                PointF oppH = new PointF(2 * P.X - activeH.X, 2 * P.Y - activeH.Y);
                if (isDraggingOutHandle)
                {
                    node.InHandle = oppH;
                    node.HasInHandle = true;
                }
                else
                {
                    node.OutHandle = oppH;
                    node.HasOutHandle = true;
                }
            }
            else if (node.NodeType == VectorNodeType.Smooth)
            {
                if (lenActive < 0.001f) return;
                float ux = vx / lenActive;
                float uy = vy / lenActive;

                PointF oppH = isDraggingOutHandle ? node.InHandle : node.OutHandle;
                bool oppHas = isDraggingOutHandle ? node.HasInHandle : node.HasOutHandle;
                float oppVx = oppH.X - P.X;
                float oppVy = oppH.Y - P.Y;
                float lenOpp = (float)Math.Sqrt(oppVx * oppVx + oppVy * oppVy);

                if (!oppHas || lenOpp < 1.0f) lenOpp = Math.Max(12.0f, lenActive * 0.5f);

                PointF synchronizedOpp = new PointF(P.X - ux * lenOpp, P.Y - uy * lenOpp);
                if (isDraggingOutHandle)
                {
                    node.InHandle = synchronizedOpp;
                    node.HasInHandle = true;
                }
                else
                {
                    node.OutHandle = synchronizedOpp;
                    node.HasOutHandle = true;
                }
            }
            else if (node.NodeType == VectorNodeType.AutoSmooth)
            {
                node.NodeType = VectorNodeType.Smooth;
                SynchronizeHandles(node, isDraggingOutHandle);
            }
        }

        public static void SynchronizeOppositeHandle(VectorPathNode node, bool isDraggingInHandle)
        {
            SynchronizeHandles(node, isDraggingOutHandle: !isDraggingInHandle);
        }

        public static void UpdateAutoSmoothNode(VectorSubPath subpath, int index)
        {
            if (subpath == null || index < 0 || index >= subpath.Nodes.Count) return;
            var node = subpath.Nodes[index];
            if (node.NodeType != VectorNodeType.AutoSmooth) return;

            int count = subpath.Nodes.Count;
            PointF P = node.Point;

            PointF Pprev, Pnext;
            if (subpath.IsClosed)
            {
                Pprev = subpath.Nodes[(index - 1 + count) % count].Point;
                Pnext = subpath.Nodes[(index + 1) % count].Point;
            }
            else
            {
                if (index == 0)
                {
                    if (count > 1)
                    {
                        PointF nextPt = subpath.Nodes[1].Point;
                        node.OutHandle = new PointF(P.X + (nextPt.X - P.X) / 3f, P.Y + (nextPt.Y - P.Y) / 3f);
                        node.HasOutHandle = true;
                        node.HasInHandle = false;
                    }
                    return;
                }
                else if (index == count - 1)
                {
                    PointF prevPt = subpath.Nodes[count - 2].Point;
                    node.InHandle = new PointF(P.X - (P.X - prevPt.X) / 3f, P.Y - (P.Y - prevPt.Y) / 3f);
                    node.HasInHandle = true;
                    node.HasOutHandle = false;
                    return;
                }
                Pprev = subpath.Nodes[index - 1].Point;
                Pnext = subpath.Nodes[index + 1].Point;
            }

            float dx = Pnext.X - Pprev.X;
            float dy = Pnext.Y - Pprev.Y;
            float chordLen = (float)Math.Sqrt(dx * dx + dy * dy);
            if (chordLen < 0.001f)
            {
                node.ResetHandles();
                return;
            }

            float tx = dx / chordLen;
            float ty = dy / chordLen;

            float dPrev = (float)Math.Sqrt((P.X - Pprev.X) * (P.X - Pprev.X) + (P.Y - Pprev.Y) * (P.Y - Pprev.Y));
            float dNext = (float)Math.Sqrt((Pnext.X - P.X) * (Pnext.X - P.X) + (Pnext.Y - P.Y) * (Pnext.Y - P.Y));

            float lenIn = dPrev / 3.0f;
            float lenOut = dNext / 3.0f;

            node.InHandle = new PointF(P.X - tx * lenIn, P.Y - ty * lenIn);
            node.HasInHandle = true;
            node.OutHandle = new PointF(P.X + tx * lenOut, P.Y + ty * lenOut);
            node.HasOutHandle = true;
        }

        public static void UpdateAutoSmoothHandles(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return;
            foreach (var sp in layer.SubPaths)
            {
                for (int i = 0; i < sp.Nodes.Count; i++)
                {
                    if (sp.Nodes[i].NodeType == VectorNodeType.AutoSmooth)
                    {
                        UpdateAutoSmoothNode(sp, i);
                    }
                }
            }
        }

        // ==========================================
        // 4. DE CASTELJAU SUBDIVISION (ADD NODE)
        // ==========================================

        public static bool InsertNodeOnSegment(VectorSubPath subpath, int segIndex, float t = 0.5f)
        {
            if (subpath == null || segIndex < 0 || segIndex >= subpath.Nodes.Count) return false;
            int count = subpath.Nodes.Count;
            if (!subpath.IsClosed && segIndex >= count - 1) return false;

            int nextIndex = (segIndex + 1) % count;
            var n1 = subpath.Nodes[segIndex];
            var n2 = subpath.Nodes[nextIndex];

            PointF p0 = n1.Point;
            PointF p3 = n2.Point;

            bool isCurve = n1.HasOutHandle || n2.HasInHandle;
            if (!isCurve)
            {
                PointF pNew = new PointF(p0.X + (p3.X - p0.X) * t, p0.Y + (p3.Y - p0.Y) * t);
                var newNode = new VectorPathNode(pNew, VectorNodeType.Corner) { IsSelected = true };
                subpath.Nodes.Insert(segIndex + 1, newNode);
                return true;
            }

            PointF p1 = n1.HasOutHandle ? n1.OutHandle : new PointF(p0.X + (p3.X - p0.X) / 3f, p0.Y + (p3.Y - p0.Y) / 3f);
            PointF p2 = n2.HasInHandle ? n2.InHandle : new PointF(p3.X - (p3.X - p0.X) / 3f, p3.Y - (p3.Y - p0.Y) / 3f);

            PointF p01 = Lerp(p0, p1, t);
            PointF p12 = Lerp(p1, p2, t);
            PointF p23 = Lerp(p2, p3, t);

            PointF p012 = Lerp(p01, p12, t);
            PointF p123 = Lerp(p12, p23, t);

            PointF pNewBezier = Lerp(p012, p123, t);

            n1.OutHandle = p01;
            n1.HasOutHandle = true;

            var newCubicNode = new VectorPathNode(pNewBezier, p012, p123, VectorNodeType.Smooth) { IsSelected = true };

            n2.InHandle = p23;
            n2.HasInHandle = true;

            subpath.Nodes.Insert(segIndex + 1, newCubicNode);
            return true;
        }

        private static PointF Lerp(PointF a, PointF b, float t)
        {
            return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        public static bool InsertNodeAtMidpoint(PhotoLayer layer)
        {
            if (layer == null) return false;
            EnsureNodesInitialized(layer);
            if (layer.SubPaths == null || layer.SubPaths.Count == 0) return false;

            // Scan 1: Check across ALL subpaths for 2 adjacent selected nodes
            foreach (var sp in layer.SubPaths)
            {
                if (sp.Nodes == null) continue;
                int count = sp.Nodes.Count;
                if (count < 2) continue;
                int segCount = sp.IsClosed ? count : count - 1;
                for (int i = 0; i < segCount; i++)
                {
                    if (sp.Nodes[i].IsSelected && sp.Nodes[(i + 1) % count].IsSelected)
                    {
                        InsertNodeOnSegment(sp, i, 0.5f);
                        UpdateLayerBounds(layer);
                        return true;
                    }
                }
            }

            // Scan 2: Check across ALL subpaths for 1 selected node
            foreach (var sp in layer.SubPaths)
            {
                if (sp.Nodes == null) continue;
                int count = sp.Nodes.Count;
                if (count == 0) continue;
                int segCount = sp.IsClosed ? count : count - 1;
                for (int i = 0; i < count; i++)
                {
                    if (sp.Nodes[i].IsSelected)
                    {
                        int targetSeg = i;
                        if (!sp.IsClosed && targetSeg >= segCount) targetSeg = Math.Max(0, segCount - 1);
                        InsertNodeOnSegment(sp, targetSeg, 0.5f);
                        sp.Nodes[i].IsSelected = false;
                        UpdateLayerBounds(layer);
                        return true;
                    }
                }
            }

            // Scan 3: Fallback when NO nodes in ANY subpath are selected
            foreach (var sp in layer.SubPaths)
            {
                if (sp.Nodes == null) continue;
                int count = sp.Nodes.Count;
                int segCount = sp.IsClosed ? count : count - 1;
                if (segCount > 0)
                {
                    InsertNodeOnSegment(sp, 0, 0.5f);
                    UpdateLayerBounds(layer);
                    return true;
                }
            }

            return false;
        }

        public static bool InsertNodeAt(PhotoLayer layer, PointF point)
        {
            if (layer == null) return false;
            EnsureNodesInitialized(layer);

            int subPathIdx;
            int segIdx = HitTestSegment(layer, point, 16f, out subPathIdx);
            if (segIdx >= 0 && subPathIdx >= 0 && layer.SubPaths != null && subPathIdx < layer.SubPaths.Count)
            {
                bool ok = InsertNodeOnSegment(layer.SubPaths[subPathIdx], segIdx, 0.5f);
                if (ok)
                {
                    var sub = layer.SubPaths[subPathIdx];
                    if (segIdx + 1 < sub.Nodes.Count)
                    {
                        foreach (var sp in layer.SubPaths)
                        {
                            if (sp.Nodes != null) foreach (var n in sp.Nodes) n.IsSelected = false;
                        }
                        sub.Nodes[segIdx + 1].IsSelected = true;
                    }
                    UpdateLayerBounds(layer);
                }
                return ok;
            }
            return InsertNodeAtMidpoint(layer);
        }

        // ==========================================
        // 5. NODE DELETION & PATH MANIPULATIONS
        // ==========================================

        public static bool DeleteSelectedNodes(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;
            bool anySelected = layer.SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Any(n => n.IsSelected));
            if (!anySelected)
            {
                foreach (var sp in layer.SubPaths)
                {
                    if (sp.Nodes != null && sp.Nodes.Count > 0)
                    {
                        sp.Nodes[sp.Nodes.Count - 1].IsSelected = true;
                        break;
                    }
                }
            }
            bool modified = false;

            for (int s = layer.SubPaths.Count - 1; s >= 0; s--)
            {
                var sp = layer.SubPaths[s];
                int removed = sp.Nodes.RemoveAll(n => n.IsSelected);
                if (removed > 0) modified = true;

                if (sp.Nodes.Count == 0)
                {
                    layer.SubPaths.RemoveAt(s);
                }
                else if (sp.IsClosed && sp.Nodes.Count < 3)
                {
                    sp.IsClosed = false;
                }
            }

            if (modified)
            {
                UpdateAutoSmoothHandles(layer);
                UpdateLayerBounds(layer);
            }
            return modified;
        }

        public static bool JoinSelectedNodes(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;

            var selected = new List<(VectorSubPath sub, int index, VectorPathNode node)>();
            foreach (var sp in layer.SubPaths)
            {
                for (int i = 0; i < sp.Nodes.Count; i++)
                {
                    if (sp.Nodes[i].IsSelected) selected.Add((sp, i, sp.Nodes[i]));
                }
            }

            // Theo chuẩn Inkscape: Người dùng phải chọn ít nhất 2 neo để gộp
            if (selected.Count < 2) return false;

            // TH 1: 2 mút của cùng 1 SubPath hở -> Gộp lại khép kín
            if (selected.Count == 2 && selected[0].sub == selected[1].sub && !selected[0].sub.IsClosed)
            {
                var sp = selected[0].sub;
                int i1 = selected[0].index;
                int i2 = selected[1].index;
                if ((i1 == 0 && i2 == sp.Nodes.Count - 1) || (i2 == 0 && i1 == sp.Nodes.Count - 1))
                {
                    PointF mid = new PointF((sp.Nodes[0].Point.X + sp.Nodes[sp.Nodes.Count - 1].Point.X) / 2f,
                                           (sp.Nodes[0].Point.Y + sp.Nodes[sp.Nodes.Count - 1].Point.Y) / 2f);
                    sp.Nodes[0].Point = mid;
                    sp.Nodes[0].IsSelected = true;
                    sp.Nodes.RemoveAt(sp.Nodes.Count - 1);
                    sp.IsClosed = true;
                    UpdateLayerBounds(layer);
                    return true;
                }
            }

            // TH 2: 2 mút của 2 SubPath hở khác nhau -> Nối 2 SubPath thành 1
            if (selected.Count == 2 && selected[0].sub != selected[1].sub && !selected[0].sub.IsClosed && !selected[1].sub.IsClosed)
            {
                var sp1 = selected[0].sub;
                var sp2 = selected[1].sub;
                int i1 = selected[0].index;
                int i2 = selected[1].index;

                bool isEnd1 = (i1 == 0 || i1 == sp1.Nodes.Count - 1);
                bool isEnd2 = (i2 == 0 || i2 == sp2.Nodes.Count - 1);

                if (isEnd1 && isEnd2)
                {
                    if (i1 == 0) sp1.Reverse();
                    if (i2 != 0) sp2.Reverse();

                    PointF mid = new PointF((sp1.Nodes[sp1.Nodes.Count - 1].Point.X + sp2.Nodes[0].Point.X) / 2f,
                                           (sp1.Nodes[sp1.Nodes.Count - 1].Point.Y + sp2.Nodes[0].Point.Y) / 2f);
                    var last1 = sp1.Nodes[sp1.Nodes.Count - 1];
                    var first2 = sp2.Nodes[0];

                    if (first2.HasOutHandle)
                    {
                        last1.OutHandle = new PointF(first2.OutHandle.X + (mid.X - first2.Point.X),
                                                     first2.OutHandle.Y + (mid.Y - first2.Point.Y));
                        last1.HasOutHandle = true;
                    }
                    if (last1.HasInHandle)
                    {
                        last1.InHandle = new PointF(last1.InHandle.X + (mid.X - last1.Point.X),
                                                    last1.InHandle.Y + (mid.Y - last1.Point.Y));
                    }
                    last1.Point = mid;

                    sp2.Nodes.RemoveAt(0);
                    sp1.Nodes.AddRange(sp2.Nodes);
                    layer.SubPaths.Remove(sp2);

                    UpdateLayerBounds(layer);
                    return true;
                }
            }

            // TH 3: 2 node liên tiếp trong cùng SubPath -> Gộp thành 1 node tại trung điểm
            if (selected.Count == 2 && selected[0].sub == selected[1].sub)
            {
                var sp = selected[0].sub;
                int minIdx = Math.Min(selected[0].index, selected[1].index);
                int maxIdx = Math.Max(selected[0].index, selected[1].index);
                if (maxIdx == minIdx + 1 || (sp.IsClosed && minIdx == 0 && maxIdx == sp.Nodes.Count - 1))
                {
                    if (sp.IsClosed && minIdx == 0 && maxIdx == sp.Nodes.Count - 1)
                    {
                        PointF mid = new PointF((sp.Nodes[0].Point.X + sp.Nodes[maxIdx].Point.X) / 2f,
                                               (sp.Nodes[0].Point.Y + sp.Nodes[maxIdx].Point.Y) / 2f);
                        sp.Nodes[0].Point = mid;
                        sp.Nodes[0].InHandle = sp.Nodes[maxIdx].InHandle;
                        sp.Nodes[0].HasInHandle = sp.Nodes[maxIdx].HasInHandle;
                        sp.Nodes.RemoveAt(maxIdx);
                    }
                    else
                    {
                        PointF mid = new PointF((sp.Nodes[minIdx].Point.X + sp.Nodes[maxIdx].Point.X) / 2f,
                                               (sp.Nodes[minIdx].Point.Y + sp.Nodes[maxIdx].Point.Y) / 2f);
                        sp.Nodes[minIdx].Point = mid;
                        sp.Nodes[minIdx].OutHandle = sp.Nodes[maxIdx].OutHandle;
                        sp.Nodes[minIdx].HasOutHandle = sp.Nodes[maxIdx].HasOutHandle;
                        sp.Nodes.RemoveAt(maxIdx);
                    }
                    UpdateLayerBounds(layer);
                    return true;
                }
            }

            // TH 4: Cụm nhiều node -> gom về trọng tâm của nhóm
            float avgX = 0f, avgY = 0f;
            foreach (var item in selected)
            {
                avgX += item.node.Point.X;
                avgY += item.node.Point.Y;
            }
            avgX /= selected.Count;
            avgY /= selected.Count;

            var primary = selected[0];
            primary.node.Point = new PointF(avgX, avgY);
            for (int k = 1; k < selected.Count; k++)
            {
                selected[k].sub.Nodes.Remove(selected[k].node);
            }
            UpdateLayerBounds(layer);
            return true;
        }

        public static bool ConnectEndpoints(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;

            // Kiểm tra 2 mút của cùng 1 SubPath hở
            foreach (var sp in layer.SubPaths)
            {
                if (!sp.IsClosed && sp.Nodes.Count >= 2)
                {
                    if (sp.Nodes[0].IsSelected && sp.Nodes[sp.Nodes.Count - 1].IsSelected)
                    {
                        sp.IsClosed = true;
                        UpdateLayerBounds(layer);
                        return true;
                    }
                }
            }

            // Kiểm tra 2 mút của 2 SubPath khác nhau -> nối bằng đoạn thẳng
            var endNodes = new List<(VectorSubPath sub, bool isStart, VectorPathNode node)>();
            foreach (var sp in layer.SubPaths)
            {
                if (!sp.IsClosed && sp.Nodes.Count >= 2)
                {
                    if (sp.Nodes[0].IsSelected) endNodes.Add((sp, true, sp.Nodes[0]));
                    if (sp.Nodes[sp.Nodes.Count - 1].IsSelected) endNodes.Add((sp, false, sp.Nodes[sp.Nodes.Count - 1]));
                }
            }

            if (endNodes.Count == 2 && endNodes[0].sub != endNodes[1].sub)
            {
                var sp1 = endNodes[0].sub;
                var sp2 = endNodes[1].sub;
                if (endNodes[0].isStart) sp1.Reverse();
                if (!endNodes[1].isStart) sp2.Reverse();

                sp1.Nodes.AddRange(sp2.Nodes);
                layer.SubPaths.Remove(sp2);
                UpdateLayerBounds(layer);
                return true;
            }

            // Dự phòng: Tự động khép kín SubPath hở đầu tiên nếu người dùng bấm nối
            foreach (var sp in layer.SubPaths)
            {
                if (!sp.IsClosed && sp.Nodes.Count >= 2)
                {
                    sp.IsClosed = true;
                    sp.Nodes[0].IsSelected = true;
                    sp.Nodes[sp.Nodes.Count - 1].IsSelected = true;
                    UpdateLayerBounds(layer);
                    return true;
                }
            }

            return false;
        }

        public static bool JoinSelectedEndNodes(PhotoLayer layer)
        {
            return ConnectEndpoints(layer);
        }

        public static bool BreakPathAtSelectedNodes(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;
            bool anySelected = layer.SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Any(n => n.IsSelected));
            if (!anySelected)
            {
                foreach (var sp in layer.SubPaths)
                {
                    if (sp.Nodes != null && sp.Nodes.Count > 0)
                    {
                        sp.Nodes[0].IsSelected = true;
                        break;
                    }
                }
            }
            bool broken = false;

            for (int s = layer.SubPaths.Count - 1; s >= 0; s--)
            {
                var sp = layer.SubPaths[s];
                for (int i = 0; i < sp.Nodes.Count; i++)
                {
                    if (sp.Nodes[i].IsSelected)
                    {
                        if (sp.IsClosed)
                        {
                            var rotated = new List<VectorPathNode>();
                            for (int k = i; k < sp.Nodes.Count; k++) rotated.Add(sp.Nodes[k]);
                            for (int k = 0; k < i; k++) rotated.Add(sp.Nodes[k]);
                            var endClone = sp.Nodes[i].Clone();
                            endClone.IsSelected = true;
                            rotated.Add(endClone);
                            sp.Nodes = rotated;
                            sp.IsClosed = false;
                            broken = true;
                            break;
                        }
                        else if (i > 0 && i < sp.Nodes.Count - 1)
                        {
                            var sub1 = new VectorSubPath(false);
                            for (int k = 0; k <= i; k++) sub1.Nodes.Add(sp.Nodes[k]);

                            var sub2 = new VectorSubPath(false);
                            for (int k = i; k < sp.Nodes.Count; k++) sub2.Nodes.Add(sp.Nodes[k].Clone());

                            layer.SubPaths.RemoveAt(s);
                            layer.SubPaths.Insert(s, sub2);
                            layer.SubPaths.Insert(s, sub1);
                            broken = true;
                            break;
                        }
                    }
                }
            }
            if (broken) UpdateLayerBounds(layer);
            return broken;
        }

        public static bool DeleteSelectedSegment(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;
            bool deleted = false;

            for (int s = layer.SubPaths.Count - 1; s >= 0; s--)
            {
                var sp = layer.SubPaths[s];
                int count = sp.Nodes.Count;
                int segCount = sp.IsClosed ? count : count - 1;

                for (int i = 0; i < segCount; i++)
                {
                    int nextIdx = (i + 1) % count;
                    if (sp.Nodes[i].IsSelected && sp.Nodes[nextIdx].IsSelected)
                    {
                        if (sp.IsClosed)
                        {
                            // Mở đường kín tại phân đoạn này: nextIdx trở thành đầu, i trở thành cuối
                            var rotated = new List<VectorPathNode>();
                            for (int k = nextIdx; k < count; k++) rotated.Add(sp.Nodes[k]);
                            for (int k = 0; k <= i; k++) rotated.Add(sp.Nodes[k]);
                            sp.Nodes[i].HasOutHandle = false;
                            sp.Nodes[nextIdx].HasInHandle = false;
                            sp.Nodes = rotated;
                            sp.IsClosed = false;
                            deleted = true;
                            break;
                        }
                        else
                        {
                            // Cắt đường hở thành 2 subpaths
                            var sub1 = new VectorSubPath(false);
                            for (int k = 0; k <= i; k++) sub1.Nodes.Add(sp.Nodes[k]);

                            var sub2 = new VectorSubPath(false);
                            for (int k = nextIdx; k < count; k++) sub2.Nodes.Add(sp.Nodes[k]);

                            layer.SubPaths.RemoveAt(s);
                            if (sub2.Nodes.Count > 0) layer.SubPaths.Insert(s, sub2);
                            if (sub1.Nodes.Count > 0) layer.SubPaths.Insert(s, sub1);
                            deleted = true;
                            break;
                        }
                    }
                }
                if (deleted) break;
            }

            if (!deleted)
            {
                // Dự phòng 1: Nếu có 1 neo được chọn, chọn thêm neo kế tiếp
                for (int s = 0; s < layer.SubPaths.Count; s++)
                {
                    var sp = layer.SubPaths[s];
                    int count = sp.Nodes.Count;
                    int segCount = sp.IsClosed ? count : count - 1;
                    if (segCount <= 0) continue;
                    for (int i = 0; i < count; i++)
                    {
                        if (sp.Nodes[i].IsSelected)
                        {
                            int nextIdx = (i + 1) % count;
                            sp.Nodes[nextIdx].IsSelected = true;
                            return DeleteSelectedSegment(layer);
                        }
                    }
                }

                // Dự phòng 2: Nếu chưa chọn neo nào, chọn đoạn đầu tiên
                for (int s = 0; s < layer.SubPaths.Count; s++)
                {
                    var sp = layer.SubPaths[s];
                    int count = sp.Nodes.Count;
                    int segCount = sp.IsClosed ? count : count - 1;
                    if (segCount > 0)
                    {
                        sp.Nodes[0].IsSelected = true;
                        sp.Nodes[1 % count].IsSelected = true;
                        return DeleteSelectedSegment(layer);
                    }
                }
            }

            if (deleted) UpdateLayerBounds(layer);
            return deleted;
        }

        // ==========================================
        // 6. SEGMENT CONVERSIONS (LINE <-> CURVE)
        // ==========================================

        public static bool ConvertSegmentToLine(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;
            bool anySelected = layer.SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Any(n => n.IsSelected));
            bool changed = false;
            foreach (var sp in layer.SubPaths)
            {
                if (sp.Nodes == null) continue;
                int count = sp.Nodes.Count;
                if (count < 2) continue;
                int segCount = sp.IsClosed ? count : count - 1;
                for (int i = 0; i < segCount; i++)
                {
                    var n1 = sp.Nodes[i];
                    var n2 = sp.Nodes[(i + 1) % count];
                    if (!anySelected || (n1.IsSelected && n2.IsSelected) || (n1.IsSelected || n2.IsSelected))
                    {
                        n1.HasOutHandle = false;
                        n1.OutHandle = n1.Point;
                        n2.HasInHandle = false;
                        n2.InHandle = n2.Point;
                        changed = true;
                    }
                }
            }
            if (changed) UpdateLayerBounds(layer);
            return changed;
        }

        public static bool ConvertSegmentToCurve(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null) return false;
            bool anySelected = layer.SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Any(n => n.IsSelected));
            bool changed = false;
            foreach (var sp in layer.SubPaths)
            {
                if (sp.Nodes == null) continue;
                int count = sp.Nodes.Count;
                if (count < 2) continue;
                int segCount = sp.IsClosed ? count : count - 1;
                for (int i = 0; i < segCount; i++)
                {
                    var n1 = sp.Nodes[i];
                    var n2 = sp.Nodes[(i + 1) % count];
                    if (!anySelected || (n1.IsSelected && n2.IsSelected) || (n1.IsSelected || n2.IsSelected))
                    {
                        float dx = n2.Point.X - n1.Point.X;
                        float dy = n2.Point.Y - n1.Point.Y;

                        float nx = -dy * 0.2f;
                        float ny = dx * 0.2f;

                        n1.OutHandle = new PointF(n1.Point.X + dx / 3f + nx, n1.Point.Y + dy / 3f + ny);
                        n1.HasOutHandle = true;

                        n2.InHandle = new PointF(n2.Point.X - dx / 3f + nx, n2.Point.Y - dy / 3f + ny);
                        n2.HasInHandle = true;

                        changed = true;
                    }
                }
            }
            if (changed) UpdateLayerBounds(layer);
            return changed;
        }

        public static void SetSelectedNodesType(PhotoLayer layer, VectorNodeType type)
        {
            if (layer == null || layer.SubPaths == null) return;
            bool anySelected = layer.SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Any(n => n.IsSelected));
            foreach (var sp in layer.SubPaths)
            {
                if (sp.Nodes == null || sp.Nodes.Count == 0) continue;
                int count = sp.Nodes.Count;
                for (int i = 0; i < count; i++)
                {
                    var node = sp.Nodes[i];
                    if (node.IsSelected || !anySelected)
                    {
                        node.NodeType = type;
                        if (type == VectorNodeType.Corner)
                        {
                            // Góc nhọn: Thu tay đòn về để đỉnh nhọn sắc nét
                            node.HasInHandle = false;
                            node.HasOutHandle = false;
                            node.InHandle = node.Point;
                            node.OutHandle = node.Point;
                        }
                        else if (type == VectorNodeType.AutoSmooth)
                        {
                            UpdateAutoSmoothNode(sp, i);
                        }
                        else if (type == VectorNodeType.Smooth || type == VectorNodeType.Symmetric)
                        {
                            // Uốn mượt / Đối xứng: Tự động tính toán hướng tiếp tuyến và sinh 2 tay đòn chuẩn Inkscape
                            PointF P = node.Point;
                            PointF Pprev = (sp.IsClosed || i > 0) ? sp.Nodes[(i - 1 + count) % count].Point : P;
                            PointF Pnext = (sp.IsClosed || i < count - 1) ? sp.Nodes[(i + 1) % count].Point : P;

                            float dx = Pnext.X - Pprev.X;
                            float dy = Pnext.Y - Pprev.Y;
                            float dLen = (float)Math.Sqrt(dx * dx + dy * dy);

                            if (dLen < 0.001f)
                            {
                                dx = 30f;
                                dy = 0f;
                                dLen = 30f;
                            }

                            float hLen = Math.Max(15f, Math.Min(60f, dLen / 3.0f));
                            float ux = (dx / dLen) * hLen;
                            float uy = (dy / dLen) * hLen;

                            if (i > 0 || sp.IsClosed)
                            {
                                node.InHandle = new PointF(P.X - ux, P.Y - uy);
                                node.HasInHandle = true;
                            }
                            if (i < count - 1 || sp.IsClosed)
                            {
                                node.OutHandle = new PointF(P.X + ux, P.Y + uy);
                                node.HasOutHandle = true;
                            }

                            if (type == VectorNodeType.Symmetric)
                            {
                                SynchronizeHandles(node, true);
                            }
                        }
                    }
                }
            }
            UpdateLayerBounds(layer);
        }

        // ==========================================
        // 7. HIT TESTING & MANIPULATION HELPERS
        // ==========================================

        public static int HitTestSegment(PhotoLayer layer, PointF canvasPt, float hitTolerance, out int subPathIndex)
        {
            subPathIndex = -1;
            if (layer == null || layer.SubPaths == null || layer.SubPaths.Count == 0) return -1;

            float tolSq = hitTolerance * hitTolerance;

            for (int s = 0; s < layer.SubPaths.Count; s++)
            {
                var sp = layer.SubPaths[s];
                if (sp == null || sp.Nodes == null) continue;
                int count = sp.Nodes.Count;
                if (count < 2) continue;
                int segCount = sp.IsClosed ? count : count - 1;

                for (int i = 0; i < segCount; i++)
                {
                    var n1 = sp.Nodes[i];
                    var n2 = sp.Nodes[(i + 1) % count];

                    PointF p0 = n1.Point;
                    PointF p3 = n2.Point;

                    bool isCurve = n1.HasOutHandle || n2.HasInHandle;
                    if (!isCurve)
                    {
                        if (DistSqToLineSegment(canvasPt, p0, p3) <= tolSq)
                        {
                            subPathIndex = s;
                            return i;
                        }
                    }
                    else
                    {
                        PointF p1 = n1.HasOutHandle ? n1.OutHandle : new PointF(p0.X + (p3.X - p0.X) / 3f, p0.Y + (p3.Y - p0.Y) / 3f);
                        PointF p2 = n2.HasInHandle ? n2.InHandle : new PointF(p3.X - (p3.X - p0.X) / 3f, p3.Y - (p3.Y - p0.Y) / 3f);

                        // Sample 16 points along Bezier curve
                        PointF prevPt = p0;
                        for (int step = 1; step <= 16; step++)
                        {
                            float t = step / 16f;
                            float oneMinusT = 1f - t;
                            float b0 = oneMinusT * oneMinusT * oneMinusT;
                            float b1 = 3f * oneMinusT * oneMinusT * t;
                            float b2 = 3f * oneMinusT * t * t;
                            float b3 = t * t * t;

                            PointF curPt = new PointF(
                                b0 * p0.X + b1 * p1.X + b2 * p2.X + b3 * p3.X,
                                b0 * p0.Y + b1 * p1.Y + b2 * p2.Y + b3 * p3.Y
                            );

                            if (DistSqToLineSegment(canvasPt, prevPt, curPt) <= tolSq)
                            {
                                subPathIndex = s;
                                return i;
                            }
                            prevPt = curPt;
                        }
                    }
                }
            }
            return -1;
        }

        public static int HitTestSegment(PhotoLayer layer, PointF canvasPt, float hitTolerance)
        {
            int dummySubPath;
            return HitTestSegment(layer, canvasPt, hitTolerance, out dummySubPath);
        }

        private static float DistSqToLineSegment(PointF pt, PointF a, PointF b)
        {
            float l2 = (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y);
            if (l2 < 0.0001f)
            {
                return (pt.X - a.X) * (pt.X - a.X) + (pt.Y - a.Y) * (pt.Y - a.Y);
            }
            float t = Math.Max(0f, Math.Min(1f, ((pt.X - a.X) * (b.X - a.X) + (pt.Y - a.Y) * (b.Y - a.Y)) / l2));
            float projX = a.X + t * (b.X - a.X);
            float projY = a.Y + t * (b.Y - a.Y);
            return (pt.X - projX) * (pt.X - projX) + (pt.Y - projY) * (pt.Y - projY);
        }

        public static void MoveSelectedNodes(PhotoLayer layer, float dx, float dy)
        {
            if (layer == null || layer.SubPaths == null) return;
            bool moved = false;
            foreach (var sp in layer.SubPaths)
            {
                foreach (var node in sp.Nodes)
                {
                    if (node.IsSelected)
                    {
                        node.Translate(dx, dy);
                        moved = true;
                    }
                }
            }
            if (moved)
            {
                UpdateAutoSmoothHandles(layer);
                UpdateLayerBounds(layer);
            }
        }

        public static void UpdateLayerBounds(PhotoLayer layer)
        {
            if (layer == null || layer.SubPaths == null || layer.SubPaths.Count == 0) return;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var sp in layer.SubPaths)
            {
                var b = sp.GetBounds();
                if (!b.IsEmpty)
                {
                    minX = Math.Min(minX, b.Left);
                    minY = Math.Min(minY, b.Top);
                    maxX = Math.Max(maxX, b.Right);
                    maxY = Math.Max(maxY, b.Bottom);
                }
            }

            if (minX <= maxX && minY <= maxY)
            {
                layer.Bounds = new RectangleF(minX, minY, Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY));
            }
        }

        public static void EnsureNodesInitialized(PhotoLayer layer)
        {
            if (layer == null) return;
            if (layer.SubPaths == null) layer.SubPaths = new List<VectorSubPath>();
            bool hasAnyNodes = layer.SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Count > 0);
            if (!hasAnyNodes)
            {
                if (layer.PathPoints != null && layer.PathPoints.Count > 0)
                {
                    var sp = new VectorSubPath(false);
                    for (int i = 0; i < layer.PathPoints.Count; i++)
                    {
                        sp.Nodes.Add(new VectorPathNode(layer.PathPoints[i], VectorNodeType.Corner));
                    }
                    layer.SubPaths.Add(sp);
                    layer.ShapeType = VectorShapeType.Path;
                }
                else
                {
                    layer.SubPaths = CreateSubPathsFromShape(layer.ShapeType, layer.Bounds, layer.CornerRadius, layer.ShapePointCount);
                    layer.ShapeType = VectorShapeType.Path;
                }
            }
        }

        public static GraphicsPath BuildGraphicsPath(PhotoLayer layer)
        {
            if (layer == null) return new GraphicsPath();
            return layer.BuildGraphicsPath();
        }
    }
}

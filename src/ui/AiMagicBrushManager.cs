using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public enum MagicBrushType
    {
        Masking,        // Cọ Tô Vùng
        Blend,          // Cọ Hòa Trộn
        Eraser,         // Cọ Xóa Vật Thể
        SketchToAsset   // Phác Thảo Tạo Hình
    }

    public class MagicBrushStroke
    {
        public List<PointF> SlidePoints { get; set; } = new List<PointF>();
        public MagicBrushType BrushType { get; set; } = MagicBrushType.Masking;
        public float Radius { get; set; } = 35f;
        public float Opacity { get; set; } = 0.65f;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public RectangleF GetBoundingBox()
        {
            if (SlidePoints == null || SlidePoints.Count == 0) return RectangleF.Empty;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var pt in SlidePoints)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }
            float pad = Radius * 1.5f;
            return new RectangleF(minX - pad, minY - pad, Math.Max(1f, maxX - minX + pad * 2f), Math.Max(1f, maxY - minY + pad * 2f));
        }
    }

    /// <summary>
    /// AI Magic Brush & Region Masking Canvas
    /// Quản lý thanh công cụ nổi (Floating Dock), cọ vẽ tương tác bán trong suốt,
    /// hòa quyện 2 lớp (Seamless Blend), xóa vật thể thông minh và nhận diện nét phác thảo (Sketch-to-Asset).
    /// KẾT XUẤT HOÀN TOÀN BẰNG DIRECT2D & ICON VECTOR FLUENT WIN 11.
    /// </summary>
    public class AiMagicBrushManager
    {
        // Trạng thái bật/tắt
        public bool IsActive { get; set; } = false;
        public MagicBrushType ActiveBrush { get; set; } = MagicBrushType.Masking;
        public float BrushRadius { get; set; } = 35f;
        public float BrushOpacity { get; set; } = 0.65f;

        // Danh sách các nét cọ trên slide hiện tại
        public List<MagicBrushStroke> CommittedStrokes { get; } = new List<MagicBrushStroke>();
        public MagicBrushStroke CurrentStroke { get; set; } = null;
        public bool IsPainting { get; set; } = false;

        // Vị trí chuột thời gian thực (để vẽ con trỏ vòng cọ phát sáng)
        public PointF MouseScreenPos { get; set; } = PointF.Empty;
        public bool IsMouseOverCanvas { get; set; } = false;

        // Floating Dock Bounds & Kéo thả
        public RectangleF DockBounds { get; set; } = new RectangleF(0, 0, 220, 316);
        public bool HasCustomDockPos { get; set; } = false;
        public bool IsDraggingDock { get; set; } = false;
        public PointF DockDragStartOffset { get; set; } = PointF.Empty;

        // Hit-test buttons trong Floating Dock
        public RectangleF BtnCloseDockRect { get; set; }
        public RectangleF BtnBrushMaskingRect { get; set; }
        public RectangleF BtnBrushBlendRect { get; set; }
        public RectangleF BtnBrushEraserRect { get; set; }
        public RectangleF BtnBrushSketchRect { get; set; }

        // Thanh trượt điều chỉnh cỡ cọ (Interactive Size Slider)
        public RectangleF SliderHitRect { get; set; }
        public RectangleF SliderTrackRect { get; set; }
        public RectangleF SliderThumbRect { get; set; }
        public bool IsDraggingSizeSlider { get; set; } = false;

        // Nút hành động chính và nút hoàn tác
        public RectangleF BtnApplyActionRect { get; set; }
        public RectangleF BtnUndoStrokeRect { get; set; }

        public static RectangleF GetShapeBounds(ShapeNode shape)
        {
            if (shape == null || shape.Transform == null) return RectangleF.Empty;
            return new RectangleF(shape.Transform.X, shape.Transform.Y, shape.Transform.Width, shape.Transform.Height);
        }

        // Màu sắc đặc trưng cho từng loại cọ
        public static Color GetBrushColor(MagicBrushType type, int alpha = 140)
        {
            switch (type)
            {
                case MagicBrushType.Masking:
                    return Color.FromArgb(alpha, 124, 58, 237); // Tím neon #7C3AED
                case MagicBrushType.Blend:
                    return Color.FromArgb(alpha, 244, 63, 94);  // Hồng ấm / Hổ phách #F43F5E
                case MagicBrushType.Eraser:
                    return Color.FromArgb(alpha, 239, 68, 68);  // Đỏ san hô #EF4444
                case MagicBrushType.SketchToAsset:
                    return Color.FromArgb(alpha, 16, 185, 129); // Xanh lục ngọc #10B981
                default:
                    return Color.FromArgb(alpha, 59, 130, 246);
            }
        }

        public static Color GetBrushAccentGlow(MagicBrushType type)
        {
            switch (type)
            {
                case MagicBrushType.Masking:
                    return Color.FromArgb(200, 6, 182, 212);   // Cyan glow
                case MagicBrushType.Blend:
                    return Color.FromArgb(220, 245, 158, 11);  // Amber glow
                case MagicBrushType.Eraser:
                    return Color.FromArgb(200, 252, 165, 165); // Soft red glow
                case MagicBrushType.SketchToAsset:
                    return Color.FromArgb(220, 52, 211, 153);  // Emerald glow
                default:
                    return Color.FromArgb(200, 96, 165, 250);
            }
        }

        public void ShowNotification(string message)
        {
            // Bỏ thông báo theo yêu cầu người dùng
        }

        public void ClearStrokes()
        {
            CommittedStrokes.Clear();
            CurrentStroke = null;
            IsPainting = false;
        }

        public void UndoLastStroke()
        {
            if (CommittedStrokes.Count > 0)
            {
                CommittedStrokes.RemoveAt(CommittedStrokes.Count - 1);
            }
        }

        // =========================================================================================
        // TIỆN ÍCH CHỌN ĐỐI TƯỢNG DƯỚI VỆT CỌ
        // =========================================================================================
        private RectangleF StrokesUnion(IEnumerable<MagicBrushStroke> strokes)
        {
            RectangleF u = RectangleF.Empty;
            foreach (var s in strokes)
            {
                var b = s.GetBoundingBox();
                u = u.IsEmpty ? b : RectangleF.Union(u, b);
            }
            return u;
        }

        private static bool PointCovered(PointF p, IEnumerable<MagicBrushStroke> strokes)
        {
            foreach (var st in strokes)
            {
                var pts = st.SlidePoints;
                if (pts.Count == 1 && AiMagicBrushEngine.Dist(p, pts[0]) <= st.Radius) return true;
                for (int i = 1; i < pts.Count; i++)
                    if (AiMagicBrushEngine.DistToSegment(p, pts[i - 1], pts[i]) <= st.Radius) return true;
            }
            return false;
        }

        /// <summary>Tỉ lệ diện tích khung đối tượng bị vệt cọ phủ (lấy mẫu lưới 12×12).</summary>
        private static float Coverage(RectangleF b, List<MagicBrushStroke> strokes)
        {
            int hit = 0, n = 12;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (PointCovered(new PointF(b.X + (i + 0.5f) * b.Width / n, b.Y + (j + 0.5f) * b.Height / n), strokes)) hit++;
            return hit / (float)(n * n);
        }

        /// <summary>Các ảnh bitmap nằm dưới vệt cọ, theo thứ tự từ trên xuống.</summary>
        private List<ShapeNode> ImagesUnderStrokes(SlideNode slide, List<MagicBrushStroke> strokes)
        {
            var res = new List<ShapeNode>();
            for (int i = slide.Shapes.Count - 1; i >= 0; i--)
            {
                var s = slide.Shapes[i];
                if (!AiMagicBrushEngine.IsBitmapShape(s)) continue;
                var b = AiMagicBrushEngine.ShapeBounds(s);
                bool touches = false;
                foreach (var st in strokes)
                {
                    foreach (var p in st.SlidePoints)
                        if (RectangleF.Inflate(b, st.Radius, st.Radius).Contains(p)) { touches = true; break; }
                    if (touches) break;
                }
                if (touches) res.Add(s);
            }
            return res;
        }

        private List<MagicBrushStroke> StrokesOf(MagicBrushType type) =>
            CommittedStrokes.Where(s => s.BrushType == type).ToList();

        // =========================================================================================
        // HÒA TRỘN 2 LỚP: lớp ảnh phía trên tan dần vào lớp bên dưới theo đúng vệt cọ
        // =========================================================================================
        public bool ExecuteSeamlessBlend(SlideNode slide, out string message)
        {
            message = "";
            var strokes = StrokesOf(MagicBrushType.Blend);
            if (slide == null || strokes.Count == 0) { message = AppL10n.T("Chưa có vệt cọ hòa trộn.", "No blend brush strokes yet."); return false; }

            var images = ImagesUnderStrokes(slide, strokes);
            if (images.Count == 0) { message = AppL10n.T("Không có ảnh dưới vệt cọ.", "No image under brush stroke."); return false; }

            ShapeNode top = images[0];
            using (Bitmap src = AiMagicBrushEngine.LoadBitmap(top))
            {
                if (src == null) { message = AppL10n.T("Không đọc được ảnh.", "Could not read image."); return false; }
                int w = src.Width, h = src.Height;
                RectangleF b = AiMagicBrushEngine.ShapeBounds(top);
                float sx = w / Math.Max(1f, b.Width);
                // Biên chuyển tiếp rộng ≈ bán kính cọ để hai lớp hòa liền mạch, không có đường cắt
                float feather = strokes.Average(s => s.Radius) * sx * 1.2f;
                Rectangle bb;
                float[] mask = AiMagicBrushEngine.BuildMask(w, h, b, strokes, feather, out bb);
                if (bb.IsEmpty) { message = AppL10n.T("Vệt cọ không nằm trên ảnh.", "Brush stroke is outside image bounds."); return false; }

                int[] px = AiMagicBrushEngine.ReadPixels(src);
                for (int i = 0; i < px.Length; i++)
                {
                    float m = mask[i];
                    if (m <= 0.002f) continue;
                    int a = (px[i] >> 24) & 0xFF;
                    int na = (int)(a * (1f - Math.Min(1f, m)));
                    px[i] = (na << 24) | (px[i] & 0x00FFFFFF);
                }
                AiMagicBrushEngine.CommitBitmap(top, AiMagicBrushEngine.WritePixels(px, w, h));
            }

            slide.IsThumbnailDirty = true;
            CommittedStrokes.RemoveAll(s => s.BrushType == MagicBrushType.Blend);
            message = AppL10n.T("Đã hòa trộn.", "Blended.");
            return true;
        }

        // =========================================================================================
        // XÓA VẬT THỂ GIỮ NỀN: inpainting trên ảnh + gỡ đối tượng vector/chữ bị tô phủ
        // =========================================================================================
        public bool ExecuteEraser(SlideNode slide, out string message)
        {
            message = "";
            var strokes = StrokesOf(MagicBrushType.Eraser);
            if (slide == null || strokes.Count == 0) { message = AppL10n.T("Chưa có vệt cọ xóa.", "No eraser brush strokes yet."); return false; }

            bool changed = false;

            // 1. Đối tượng không phải ảnh (chữ, hình vector) bị phủ phần lớn → gỡ bỏ
            for (int i = slide.Shapes.Count - 1; i >= 0; i--)
            {
                var s = slide.Shapes[i];
                if (AiMagicBrushEngine.IsBitmapShape(s)) continue;
                var b = AiMagicBrushEngine.ShapeBounds(s);
                if (b.Width <= 0 || b.Height <= 0) continue;
                if (Coverage(b, strokes) >= 0.55f) { slide.Shapes.RemoveAt(i); changed = true; }
            }

            // 2. Ảnh dưới vệt cọ → xóa vật thể, tái tạo nền từ vùng xung quanh
            foreach (var img in ImagesUnderStrokes(slide, strokes))
            {
                using (Bitmap src = AiMagicBrushEngine.LoadBitmap(img))
                {
                    if (src == null) continue;
                    int w = src.Width, h = src.Height;
                    RectangleF b = AiMagicBrushEngine.ShapeBounds(img);
                    Rectangle bb;
                    float[] mask = AiMagicBrushEngine.BuildMask(w, h, b, strokes, 6f * w / Math.Max(1f, b.Width), out bb);
                    if (bb.IsEmpty) continue;

                    var hole = new bool[w * h];
                    for (int i = 0; i < hole.Length; i++) hole[i] = mask[i] > 0.03f;
                    int[] px = AiMagicBrushEngine.ReadPixels(src);
                    AiMagicBrushEngine.Inpaint(px, w, h, hole, bb);
                    AiMagicBrushEngine.CommitBitmap(img, AiMagicBrushEngine.WritePixels(px, w, h));
                    changed = true;
                }
            }

            if (!changed) { message = AppL10n.T("Không có vật thể dưới vệt cọ.", "No object under brush stroke."); return false; }
            slide.IsThumbnailDirty = true;
            CommittedStrokes.RemoveAll(s => s.BrushType == MagicBrushType.Eraser);
            message = AppL10n.T("Đã xóa vật thể.", "Removed object.");
            return true;
        }

        // =========================================================================================
        // TÔ VÙNG: tách vùng đã tô thành một lớp ảnh riêng để di chuyển / biến đổi cục bộ
        // =========================================================================================
        public bool ExecuteMasking(SlideNode slide, out string message) => ExecuteMasking(slide, null, out message);

        public bool ExecuteMasking(SlideNode slide, SlidesView view, out string message)
        {
            message = "";
            var strokes = StrokesOf(MagicBrushType.Masking);
            if (slide == null || strokes.Count == 0) { message = AppL10n.T("Chưa có vệt cọ tô vùng.", "No mask strokes yet."); return false; }
            var images = ImagesUnderStrokes(slide, strokes);
            if (images.Count == 0) { message = AppL10n.T("Không có ảnh dưới vùng tô.", "No image under mask."); return false; }

            ShapeNode img = images[0];
            ShapeNode layer;
            using (Bitmap src = AiMagicBrushEngine.LoadBitmap(img))
            {
                if (src == null) { message = AppL10n.T("Không đọc được ảnh.", "Could not read image."); return false; }
                int w = src.Width, h = src.Height;
                RectangleF b = AiMagicBrushEngine.ShapeBounds(img);
                float sx = w / Math.Max(1f, b.Width), sy = h / Math.Max(1f, b.Height);
                Rectangle bb;
                float[] mask = AiMagicBrushEngine.BuildMask(w, h, b, strokes, 2f * sx, out bb);
                bb = Rectangle.Intersect(bb, new Rectangle(0, 0, w, h));
                if (bb.Width < 2 || bb.Height < 2) { message = AppL10n.T("Vùng tô không nằm trên ảnh.", "Mask is outside image bounds."); return false; }

                int[] px = AiMagicBrushEngine.ReadPixels(src);
                int[] cut = new int[bb.Width * bb.Height];
                for (int y = 0; y < bb.Height; y++)
                    for (int x = 0; x < bb.Width; x++)
                    {
                        int si = (bb.Y + y) * w + bb.X + x;
                        int a = (int)(((px[si] >> 24) & 0xFF) * Math.Min(1f, mask[si]));
                        cut[y * bb.Width + x] = (a << 24) | (px[si] & 0x00FFFFFF);
                    }

                layer = new ShapeNode
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = AppL10n.T("Vùng tách ", "Selection ") + (slide.Shapes.Count + 1),
                    Type = ShapeType.Image,
                    CachedImage = AiMagicBrushEngine.WritePixels(cut, bb.Width, bb.Height),
                    Transform = new Transform2D(b.X + bb.X / sx, b.Y + bb.Y / sy, bb.Width / sx, bb.Height / sy),
                    Text = ""
                };
            }

            int insertAt = slide.Shapes.IndexOf(img) + 1;
            insertAt--; slide.Shapes[insertAt] = layer; // thay ảnh gốc bằng vùng đã tô (ngoài vùng trong suốt)
            slide.IsThumbnailDirty = true;
            CommittedStrokes.RemoveAll(s => s.BrushType == MagicBrushType.Masking);

            if (view != null)
            {
                // Thoát cọ & chọn lớp mới để người dùng kéo, xoay, chỉnh màu ngay
                IsActive = false;
                ClearStrokes();
                view.SelectedShapeIndex = insertAt;
                view.SelectedShapeIndices?.Clear();
                view.SelectedShapeIndices?.Add(insertAt);
            }
            message = AppL10n.T("Đã tách vùng thành lớp mới.", "Extracted selection to new layer.");
            return true;
        }

        public static RectangleF GetPointsBoundingBox(List<PointF> pts) => AiMagicBrushEngine.Bounds(pts);

        // =========================================================================================
        // PHÁC THẢO TẠO HÌNH
        // =========================================================================================
        public bool RecognizeAndCreateVectorAsset(MagicBrushStroke stroke, SlideNode slide, out ShapeNode newShape, out string recognizedType)
        {
            newShape = null; recognizedType = "";
            if (stroke == null) return false;
            return AiMagicBrushEngine.Recognize(stroke.SlidePoints, slide, out newShape, out recognizedType);
        }

        // =========================================================================================
        // KẾT XUẤT DIRECT2D (RENDER TO CANVAS)
        // =========================================================================================
        public void Draw(D2DGraphics g, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, SlidesView slidesView)
        {
            if (!IsActive || g == null) return;

            foreach (var stroke in CommittedStrokes)
                DrawSingleStroke(g, stroke, slideX, slideY, scale);

            if (IsPainting && CurrentStroke != null)
                DrawSingleStroke(g, CurrentStroke, slideX, slideY, scale);

            if (IsMouseOverCanvas && !MouseScreenPos.IsEmpty && !IsDraggingDock && !IsDraggingSizeSlider && !DockBounds.Contains(MouseScreenPos))
                DrawBrushCursorRing(g, MouseScreenPos, ActiveBrush == MagicBrushType.SketchToAsset ? 4f : BrushRadius * scale, GetBrushColor(ActiveBrush, 220));

            DrawFloatingDock(g, slideX, slideY, scale, canvasX, contentY, canvasW, contentH);
        }

        /// <summary>
        /// Vẽ nét cọ như một khối liền: làm mượt Chaikin → Widen thành đường bao → tô 1 lần (Winding).
        /// Không chồng lớp, không quầng sáng tràn ra ngoài, không gãy góc; độ rộng đúng bằng vòng con trỏ.
        /// </summary>
        private void DrawSingleStroke(D2DGraphics g, MagicBrushStroke stroke, int slideX, int slideY, float scale)
        {
            if (stroke == null || stroke.SlidePoints == null || stroke.SlidePoints.Count == 0) return;

            var sp = AiMagicBrushEngine.Smooth(stroke.SlidePoints, 2)
                .Select(p => new PointF(slideX + p.X * scale, slideY + p.Y * scale)).ToList();
            if (sp.Count == 1) sp.Add(new PointF(sp[0].X + 0.1f, sp[0].Y));

            bool sketch = stroke.BrushType == MagicBrushType.SketchToAsset;
            float width = sketch ? 3f : Math.Max(2f, 2f * stroke.Radius * scale);
            Color c = GetBrushColor(stroke.BrushType, sketch ? 230 : 110);

            using (var path = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var brush = new SolidBrush(c))
            {
                path.AddLines(sp.ToArray());
                try
                {
                    path.Widen(pen);
                    path.FillMode = FillMode.Winding;
                    g.FillPath(brush, path);
                }
                catch
                {
                    using (var p2 = new Pen(c, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        g.DrawLines(p2, sp.ToArray());
                }
            }
        }

        private void DrawBrushCursorRing(D2DGraphics g, PointF pos, float radius, Color accent)
        {
            float r = Math.Max(4f, radius);
            RectangleF circleRect = new RectangleF(pos.X - r, pos.Y - r, r * 2f, r * 2f);
            using (Pen outerPen = new Pen(Color.FromArgb(160, 0, 0, 0), 1.0f))
                g.DrawEllipse(outerPen, new RectangleF(circleRect.X - 1, circleRect.Y - 1, circleRect.Width + 2, circleRect.Height + 2));
            using (Pen innerPen = new Pen(Color.FromArgb(230, 255, 255, 255), 1.2f))
                g.DrawEllipse(innerPen, circleRect);
        }

        private void DrawFloatingDock(D2DGraphics g, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH)
        {
            // Định vị mặc định nếu chưa kéo thả: đặt cạnh mép phải slide
            if (!HasCustomDockPos)
            {
                float slideWidthEst = Math.Min(canvasW * 0.72f, 1920f * scale);
                float targetX = Math.Min(canvasX + canvasW - 235, slideX + slideWidthEst + 16f);
                float targetY = Math.Max(contentY + 20, slideY + 30);
                DockBounds = new RectangleF(targetX, targetY, 210, 218);
            }

            RectangleF dock = DockBounds;

            // 1. Hiệu ứng nổi đều 4 cạnh (Uniform Floating Elevation Shadow - Không méo, không phân tầng)
            for (int s = 14; s >= 1; s--)
            {
                float spread = s * 0.55f; // Độ nở vi sai 0.55px siêu mịn, đều cả 4 cạnh
                int shadowAlpha = (15 - s) * 2;
                RectangleF shadowRect = new RectangleF(dock.X - spread, dock.Y - spread, dock.Width + spread * 2f, dock.Height + spread * 2f);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(shadowAlpha / 3, 0, 0, 0)))
                {
                    g.FillRoundedRectangle(sb, shadowRect, 14f + spread);
                }
            }

            // 2. Thân Dock phong cách Fluent Acrylic trắng tinh khôi (Clean Frosted Surface)
            using (SolidBrush glassBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
            {
                g.FillRoundedRectangle(glassBrush, dock, 14f);
            }

            // 3. Viền tinh tế (Subtle Border)
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawRoundedRectangle(borderPen, dock, 14f);
            }

            // 4. Header Grip & Tiêu đề
            using (Font titleFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
            using (Font labelFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            using (Font valFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            using (Font btnFont = new Font("Segoe UI", 8.4f, FontStyle.Regular))
            using (Font boldBtnFont = new Font("Segoe UI", 8.6f, FontStyle.Bold))
            using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            using (StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                // Biểu tượng lấp lánh AI (Fluent Magic Sparkle)
                DrawFluentSparkleIcon(g, dock.X + 14, dock.Y + 12, 16, Color.FromArgb(244, 63, 94));

                // Tiêu đề thuần tiếng Việt
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString(AppL10n.T("Cọ Đa Năng AI", "AI Magic Brush"), titleFont, titleBrush, new RectangleF(dock.X + 36, dock.Y + 11, 140, 18), sfLeft);
                }

                // Nút Đóng (✕)
                BtnCloseDockRect = new RectangleF(dock.X + dock.Width - 28, dock.Y + 10, 20, 20);
                using (SolidBrush closeBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("✕", btnFont, closeBrush, BtnCloseDockRect, sfCenter);
                }

                // Đường phân cách mờ
                using (Pen sepPen = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
                {
                    g.DrawLine(sepPen, dock.X + 12, dock.Y + 38, dock.X + dock.Width - 12, dock.Y + 38);
                }

                // 5. Bốn nút cọ vẽ thuần tiếng Việt
                float curY = dock.Y + 45;

                BtnBrushMaskingRect = new RectangleF(dock.X + 12, curY, dock.Width - 24, 28);
                DrawBrushSelectPill(g, BtnBrushMaskingRect, AppL10n.T("Cọ tô vùng", "Mask Brush"), MagicBrushType.Masking, ActiveBrush == MagicBrushType.Masking, btnFont, boldBtnFont, sfLeft);
                curY += 32;

                BtnBrushBlendRect = new RectangleF(dock.X + 12, curY, dock.Width - 24, 28);
                DrawBrushSelectPill(g, BtnBrushBlendRect, AppL10n.T("Cọ hòa trộn", "Blend Brush"), MagicBrushType.Blend, ActiveBrush == MagicBrushType.Blend, btnFont, boldBtnFont, sfLeft);
                curY += 32;

                BtnBrushEraserRect = new RectangleF(dock.X + 12, curY, dock.Width - 24, 28);
                DrawBrushSelectPill(g, BtnBrushEraserRect, AppL10n.T("Cọ xóa vật thể", "Object Eraser"), MagicBrushType.Eraser, ActiveBrush == MagicBrushType.Eraser, btnFont, boldBtnFont, sfLeft);
                curY += 32;

                BtnBrushSketchRect = new RectangleF(dock.X + 12, curY, dock.Width - 24, 28);
                DrawBrushSelectPill(g, BtnBrushSketchRect, AppL10n.T("Phác thảo tạo hình", "Sketch to Shape"), MagicBrushType.SketchToAsset, ActiveBrush == MagicBrushType.SketchToAsset, btnFont, boldBtnFont, sfLeft);
                curY += 36;

                // 6. Thanh trượt kéo chỉnh cỡ cọ (Interactive Size Slider)
                using (SolidBrush lblBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                using (SolidBrush numBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.DrawString(AppL10n.T("Cỡ cọ", "Brush Size"), labelFont, lblBrush, new RectangleF(dock.X + 14, curY, 60, 14), sfLeft);
                    g.DrawString($"{(int)Math.Round(BrushRadius)} px", valFont, numBrush, new RectangleF(dock.X + dock.Width - 74, curY, 60, 14), sfRight);
                }
                curY += 18;

                float trackX = dock.X + 14;
                float trackW = dock.Width - 28;
                float trackH = 6f;
                SliderTrackRect = new RectangleF(trackX, curY + 4, trackW, trackH);
                SliderHitRect = new RectangleF(trackX - 6, curY - 4, trackW + 12, 22);

                // Đường ray thanh trượt nền
                using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    g.FillRoundedRectangle(trackBg, SliderTrackRect, 3f);
                }

                // Đoạn ray tích cực theo cỡ cọ
                float ratio = Math.Max(0f, Math.Min(1f, (BrushRadius - 10f) / 90f));
                float activeW = Math.Max(6f, trackW * ratio);
                RectangleF activeTrack = new RectangleF(trackX, curY + 4, activeW, trackH);
                Color activeAccent = GetBrushColor(ActiveBrush, 255);
                using (SolidBrush activeBg = new SolidBrush(activeAccent))
                {
                    g.FillRoundedRectangle(activeBg, activeTrack, 3f);
                }

                // Núm trượt (Thumb Knob)
                float thumbCx = trackX + trackW * ratio;
                float thumbCy = curY + 7;
                float thumbR = 7f;
                SliderThumbRect = new RectangleF(thumbCx - thumbR, thumbCy - thumbR, thumbR * 2f, thumbR * 2f);

                using (SolidBrush thumbShadow = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                {
                    g.FillEllipse(thumbShadow, new RectangleF(SliderThumbRect.X, SliderThumbRect.Y + 1.2f, SliderThumbRect.Width, SliderThumbRect.Height));
                }
                using (SolidBrush thumbBody = new SolidBrush(Color.White))
                using (Pen thumbBorder = new Pen(activeAccent, 2.0f))
                {
                    g.FillEllipse(thumbBody, SliderThumbRect);
                    g.DrawEllipse(thumbBorder, SliderThumbRect);
                }

            }
        }

        private void DrawBrushSelectPill(D2DGraphics g, RectangleF rect, string title, MagicBrushType type, bool isSelected, Font regFont, Font boldFont, StringFormat sfLeft)
        {
            Color accent = GetBrushColor(type, 255);

            if (isSelected)
            {
                using (SolidBrush selBg = new SolidBrush(Color.FromArgb(32, accent.R, accent.G, accent.B)))
                using (Pen selBorder = new Pen(accent, 1.4f))
                {
                    g.FillRoundedRectangle(selBg, rect, 7f);
                    g.DrawRoundedRectangle(selBorder, rect, 7f);
                }
            }
            else
            {
                using (SolidBrush normalBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen normBorder = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
                {
                    g.FillRoundedRectangle(normalBg, rect, 7f);
                    g.DrawRoundedRectangle(normBorder, rect, 7f);
                }
            }

            // Điểm đèn LED chỉ báo
            float dotY = rect.Y + rect.Height / 2;
            using (SolidBrush dotBrush = new SolidBrush(isSelected ? accent : Color.FromArgb(160, accent)))
            {
                g.FillEllipse(dotBrush, new RectangleF(rect.X + 10, dotY - 4, 8, 8));
            }

            // Nhãn văn bản thuần tiếng Việt
            using (SolidBrush txtBrush = new SolidBrush(isSelected ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105)))
            {
                g.DrawString(title, isSelected ? boldFont : regFont, txtBrush, new RectangleF(rect.X + 26, rect.Y, rect.Width - 30, rect.Height), sfLeft);
            }
        }

        private static void DrawFluentSparkleIcon(D2DGraphics g, float x, float y, float size, Color color)
        {
            float cx = x + size / 2;
            float cy = y + size / 2;
            float r = size / 2;

            // Ngôi sao 4 đỉnh mượt mà phong cách Windows 11 Fluent
            using (SolidBrush b = new SolidBrush(color))
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(cx, cy - r),
                    new PointF(cx + r * 0.28f, cy - r * 0.28f),
                    new PointF(cx + r, cy),
                    new PointF(cx + r * 0.28f, cy + r * 0.28f),
                    new PointF(cx, cy + r),
                    new PointF(cx - r * 0.28f, cy + r * 0.28f),
                    new PointF(cx - r, cy),
                    new PointF(cx - r * 0.28f, cy - r * 0.28f)
                };
                g.FillPolygon(b, pts);
            }
        }

        // =========================================================================================
        // XỬ LÝ SỰ KIỆN CHUỘT TRÊN CANVAS & FLOATING DOCK
        // =========================================================================================
        public bool HandleMouseDown(Point mousePos, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, MouseButtons buttons, SlidesView slidesView, Control parentControl)
        {
            if (!IsActive) return false;

            PointF pt = mousePos;

            // 1. Kiểm tra bấm vào Floating Dock
            if (DockBounds.Contains(pt))
            {
                // Nút Đóng
                if (BtnCloseDockRect.Contains(pt))
                {
                    IsActive = false;
                    ClearStrokes();
                    parentControl?.Invalidate();
                    return true;
                }

                // Chọn loại cọ
                if (BtnBrushMaskingRect.Contains(pt)) { ActiveBrush = MagicBrushType.Masking; parentControl?.Invalidate(); return true; }
                if (BtnBrushBlendRect.Contains(pt)) { ActiveBrush = MagicBrushType.Blend; parentControl?.Invalidate(); return true; }
                if (BtnBrushEraserRect.Contains(pt)) { ActiveBrush = MagicBrushType.Eraser; parentControl?.Invalidate(); return true; }
                if (BtnBrushSketchRect.Contains(pt)) { ActiveBrush = MagicBrushType.SketchToAsset; parentControl?.Invalidate(); return true; }

                // Kéo thanh trượt cỡ cọ
                if (SliderHitRect.Contains(pt))
                {
                    IsDraggingSizeSlider = true;
                    float r = Math.Max(0f, Math.Min(1f, (pt.X - SliderTrackRect.X) / SliderTrackRect.Width));
                    BrushRadius = 10f + r * 90f;
                    parentControl?.Invalidate();
                    return true;
                }

                // Kéo thả Dock (vùng Header)
                if (pt.Y <= DockBounds.Y + 40)
                {
                    IsDraggingDock = true;
                    DockDragStartOffset = new PointF(pt.X - DockBounds.X, pt.Y - DockBounds.Y);
                    return true;
                }

                return true; // Chặn hoàn toàn không cho click xuyên qua dock xuống slide
            }

            // 2. Nếu click vào nền slide/canvas -> Luôn bắt đầu vẽ và chặn các tương tác chọn/quét khối của SlidesView
            bool isInsideCanvas = (mousePos.X >= canvasX && mousePos.X <= canvasX + canvasW && mousePos.Y >= contentY && mousePos.Y <= contentY + contentH);
            if (isInsideCanvas && buttons == MouseButtons.Left && scale > 0.0001f)
            {
                float slidePtX = (mousePos.X - slideX) / scale;
                float slidePtY = (mousePos.Y - slideY) / scale;

                IsPainting = true;

                CurrentStroke = new MagicBrushStroke
                {
                    BrushType = ActiveBrush,
                    Radius = BrushRadius,
                    Opacity = BrushOpacity
                };
                CurrentStroke.SlidePoints.Add(new PointF(slidePtX, slidePtY));
                parentControl?.Invalidate();
                return true; // Luôn trả về true để ngăn SlidesView kích hoạt quét khối hay chọn hình!
            }

            if (isInsideCanvas)
            {
                return true;
            }

            return false;
        }

        public bool HandleMouseMove(Point mousePos, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, Control parentControl)
        {
            if (!IsActive) return false;

            MouseScreenPos = mousePos;
            IsMouseOverCanvas = (mousePos.X >= canvasX && mousePos.X <= canvasX + canvasW && mousePos.Y >= contentY && mousePos.Y <= contentY + contentH);

            // Kéo thanh trượt cỡ cọ
            if (IsDraggingSizeSlider)
            {
                float r = Math.Max(0f, Math.Min(1f, (mousePos.X - SliderTrackRect.X) / SliderTrackRect.Width));
                BrushRadius = 10f + r * 90f;
                parentControl?.Invalidate();
                return true;
            }

            // Kéo thả Floating Dock
            if (IsDraggingDock)
            {
                float newX = Math.Max(canvasX, Math.Min(canvasX + canvasW - DockBounds.Width, mousePos.X - DockDragStartOffset.X));
                float newY = Math.Max(contentY, Math.Min(contentY + contentH - DockBounds.Height, mousePos.Y - DockDragStartOffset.Y));
                DockBounds = new RectangleF(newX, newY, DockBounds.Width, DockBounds.Height);
                HasCustomDockPos = true;
                parentControl?.Invalidate();
                return true;
            }

            // Đang vẽ nét cọ
            if (IsPainting && CurrentStroke != null && scale > 0.0001f)
            {
                float slidePtX = (mousePos.X - slideX) / scale;
                float slidePtY = (mousePos.Y - slideY) / scale;

                PointF lastPt = CurrentStroke.SlidePoints[CurrentStroke.SlidePoints.Count - 1];
                float dx = slidePtX - lastPt.X;
                float dy = slidePtY - lastPt.Y;

                if (dx * dx + dy * dy >= 4.0f) // Chỉ thêm nếu chuột di chuyển ít nhất 2px
                {
                    CurrentStroke.SlidePoints.Add(new PointF(slidePtX, slidePtY));
                    parentControl?.Invalidate();
                }
                return true;
            }

            if (IsMouseOverCanvas)
            {
                parentControl?.Invalidate();
                return true; // Chặn không cho SlidesView đổi con trỏ hay kích hoạt kéo hình khi đang bật Cọ AI
            }

            return false;
        }

        public bool HandleMouseUp(Point mousePos, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, SlidesView slidesView, Control parentControl)
        {
            if (!IsActive) return false;

            if (IsDraggingSizeSlider)
            {
                IsDraggingSizeSlider = false;
                parentControl?.Invalidate();
                return true;
            }

            if (IsDraggingDock)
            {
                IsDraggingDock = false;
                parentControl?.Invalidate();
                return true;
            }

            if (IsPainting && CurrentStroke != null)
            {
                IsPainting = false;

                if (CurrentStroke.SlidePoints.Count >= 2)
                {
                    CommittedStrokes.Add(CurrentStroke);

                    // Tự động áp dụng hiệu ứng ngay khi buông chuột (Auto-apply on stroke release)
                    slidesView.SaveUndoState();
                    string msg;
                    switch (CurrentStroke.BrushType)
                    {
                        case MagicBrushType.Blend:
                            ExecuteSeamlessBlend(slidesView.ActiveSlide, out msg);
                            break;
                        case MagicBrushType.Eraser:
                            ExecuteEraser(slidesView.ActiveSlide, out msg);
                            break;
                        case MagicBrushType.SketchToAsset:
                            ShapeNode created;
                            string recType;
                            bool ok = RecognizeAndCreateVectorAsset(CurrentStroke, slidesView.ActiveSlide, out created, out recType);
                            if (ok)
                            {
                                CommittedStrokes.Remove(CurrentStroke);
                            }
                            break;
                        case MagicBrushType.Masking:
                        default:
                            ExecuteMasking(slidesView.ActiveSlide, slidesView, out msg);
                            break;
                    }
                    // Nét cọ đã áp dụng xong (hoặc vẽ trên nền trống) -> xóa ngay để không tồn tại trên canvas
                    CommittedStrokes.Clear();
                }

                CurrentStroke = null;
                parentControl?.Invalidate();
                return true;
            }

            bool isOverCanvas = (mousePos.X >= canvasX && mousePos.X <= canvasX + canvasW && mousePos.Y >= contentY && mousePos.Y <= contentY + contentH);
            if (isOverCanvas || DockBounds.Contains(mousePos))
            {
                return true;
            }

            return false;
        }
    }
}

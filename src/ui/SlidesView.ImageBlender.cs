using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public partial class SlidesView
    {
        public enum BlenderFadeMode
        {
            Linear, // Đường kẻ
            Radial  // Tỏa tròn
        }

        public bool IsImageBlenderActive { get; set; } = false;
        public BlenderFadeMode CurrentBlenderFadeMode = BlenderFadeMode.Linear;
        public int BlenderIntensity = 100; // 0 - 100

        // Tọa độ chuẩn hóa (-0.2f -> 1.2f) so với bounding box của Shape
        public PointF BlenderLinearStart = new PointF(0.85f, 0.2f);
        public PointF BlenderLinearEnd = new PointF(0.85f, 0.8f);

        public PointF BlenderRadialCenter = new PointF(0.5f, 0.5f);
        public float BlenderRadialRadius = 0.35f;
        public float BlenderRadialAngle = 0f;

        public Rectangle LastBlenderSliderTrackRect = Rectangle.Empty;
        public bool IsDraggingBlenderSlider = false;

        private int _blenderDraggingHandle = -1; // -1: không drag; 0: start/center; 1: end/radius
        private Bitmap _blenderOriginalBackup = null;
        private ShapeNode _blenderActiveShape = null;
        private object _blenderSlide = null;

        public void OpenImageBlender(ShapeNode targetShape = null)
        {
            var shape = targetShape ?? SelectedShape;
            if (shape == null || (shape.CachedImage == null && string.IsNullOrEmpty(shape.ImagePath)))
            {
                if (ActiveSlide != null && SelectedShape != null && (SelectedShape.Type == ShapeType.Image || SelectedShape.CachedImage != null || !string.IsNullOrEmpty(SelectedShape.ImagePath)))
                {
                    shape = SelectedShape;
                }
            }

            IsImageBlenderActive = true;
            ElementsDrawerSelectedTab = 7;
            IsElementsDrawerOpen = true;
            IsElementsDrawerCollapsed = false;

            if (shape != null && (shape.CachedImage != null || (!string.IsNullOrEmpty(shape.ImagePath) && File.Exists(shape.ImagePath))))
            {
                SetBlenderActiveShape(shape);
            }
            else
            {
                _blenderActiveShape = null;
                try { _blenderOriginalBackup?.Dispose(); _blenderOriginalBackup = null; } catch { }
            }

            InvalidateCallback?.Invoke();
            ParentControl?.Invalidate();
        }

        public void SetBlenderActiveShape(ShapeNode shape)
        {
            if (shape == null || (shape.CachedImage == null && string.IsNullOrEmpty(shape.ImagePath)))
            {
                ClearBlenderActiveShape();
                return;
            }

            if (_blenderActiveShape == shape && _blenderOriginalBackup != null)
            {
                return;
            }

            try
            {
                _blenderOriginalBackup?.Dispose();
                _blenderOriginalBackup = null;
            }
            catch { }

            _blenderActiveShape = shape;
            _blenderSlide = ActiveSlide;

            try
            {
                if (shape.CachedImage != null)
                {
                    _blenderOriginalBackup = new Bitmap(shape.CachedImage);
                }
                else if (!string.IsNullOrEmpty(shape.ImagePath) && File.Exists(shape.ImagePath))
                {
                    using var tempImg = Image.FromFile(shape.ImagePath);
                    _blenderOriginalBackup = new Bitmap(tempImg);
                }
            }
            catch { }

            if (_blenderOriginalBackup != null)
            {
                SaveUndoState();
                ApplyBlenderMask();
            }

            InvalidateCallback?.Invoke();
            ParentControl?.Invalidate();
        }

        public void ClearBlenderActiveShape()
        {
            if (_blenderActiveShape == null) return;

            try
            {
                _blenderOriginalBackup?.Dispose();
                _blenderOriginalBackup = null;
            }
            catch { }

            _blenderDraggingHandle = -1;
            _blenderActiveShape = null;

            InvalidateCallback?.Invoke();
            ParentControl?.Invalidate();
        }

        public void ExitImageBlender(bool applyChanges)
        {
            if (!applyChanges && _blenderOriginalBackup != null && _blenderActiveShape != null)
            {
                try
                {
                    PublishBlenderImageToShape(_blenderActiveShape, new Bitmap(_blenderOriginalBackup));
                }
                catch { }
            }
            else if (applyChanges && _blenderActiveShape != null)
            {
                ApplyBlenderMask();
                SaveUndoState();
            }

            try
            {
                _blenderOriginalBackup?.Dispose();
                _blenderOriginalBackup = null;
            }
            catch { }

            _blenderDraggingHandle = -1;
            IsDraggingBlenderSlider = false;
            IsImageBlenderActive = false;
            _blenderActiveShape = null;

            InvalidateCallback?.Invoke();
            ParentControl?.Invalidate();
        }

        public void ResetImageBlenderSettings()
        {
            BlenderLinearStart = new PointF(0.85f, 0.2f);
            BlenderLinearEnd = new PointF(0.85f, 0.8f);
            BlenderRadialCenter = new PointF(0.5f, 0.5f);
            BlenderRadialRadius = 0.35f;
            BlenderRadialAngle = 0f;
            BlenderIntensity = 100;

            if (_blenderActiveShape != null)
            {
                ApplyBlenderMask();
            }
            InvalidateCallback?.Invoke();
            ParentControl?.Invalidate();
        }

        public void UpdateBlenderSliderFromMouse(Point mousePos)
        {
            if (LastBlenderSliderTrackRect.Width <= 0 || _blenderActiveShape == null) return;
            float pct = Math.Clamp((float)(mousePos.X - LastBlenderSliderTrackRect.X) / LastBlenderSliderTrackRect.Width, 0f, 1f);
            int newIntensity = (int)Math.Round(pct * 100);
            if (newIntensity != BlenderIntensity)
            {
                BlenderIntensity = newIntensity;
                ApplyBlenderMask();
                InvalidateCallback?.Invoke();
                ParentControl?.Invalidate();
            }
        }

        /// <summary>
        /// Thuật toán tạo mặt nạ làm mờ dần (Alpha Gradient / Radial Mask) với tốc độ LockBits siêu tốc
        /// </summary>
        public void ApplyBlenderMask()
        {
            if (_blenderOriginalBackup == null || _blenderActiveShape == null) return;

            try
            {
                int w = _blenderOriginalBackup.Width;
                int h = _blenderOriginalBackup.Height;
                if (w <= 0 || h <= 0) return;

                Bitmap resultBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);

                BitmapData srcData = _blenderOriginalBackup.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData dstData = resultBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                float fadeFactor = Math.Clamp(BlenderIntensity / 100f, 0f, 1f);

                unsafe
                {
                    byte* pSrcBase = (byte*)srcData.Scan0;
                    byte* pDstBase = (byte*)dstData.Scan0;
                    int srcStride = srcData.Stride;
                    int dstStride = dstData.Stride;

                    if (CurrentBlenderFadeMode == BlenderFadeMode.Linear)
                    {
                        float p1x = BlenderLinearStart.X * w;
                        float p1y = BlenderLinearStart.Y * h;
                        float p2x = BlenderLinearEnd.X * w;
                        float p2y = BlenderLinearEnd.Y * h;

                        float dx = p2x - p1x;
                        float dy = p2y - p1y;
                        float lenSq = dx * dx + dy * dy;
                        if (lenSq < 1e-4f) lenSq = 1f;

                        for (int py = 0; py < h; py++)
                        {
                            byte* pSrc = pSrcBase + py * srcStride;
                            byte* pDst = pDstBase + py * dstStride;

                            float vpy = py - p1y;

                            for (int px = 0; px < w; px++)
                            {
                                float vpx = px - p1x;
                                float proj = (vpx * dx + vpy * dy) / lenSq;

                                float alphaRatio;
                                if (proj <= 0f)
                                {
                                    alphaRatio = 1.0f;
                                }
                                else if (proj >= 1.0f)
                                {
                                    alphaRatio = 1.0f - fadeFactor;
                                }
                                else
                                {
                                    // Smoothstep chuyển tiếp mềm mại tự nhiên
                                    float t = proj * proj * (3.0f - 2.0f * proj);
                                    alphaRatio = 1.0f - t * fadeFactor;
                                }

                                byte b = pSrc[0];
                                byte g = pSrc[1];
                                byte r = pSrc[2];
                                byte a = pSrc[3];

                                byte newA = (byte)Math.Clamp((int)(a * alphaRatio), 0, 255);

                                pDst[0] = b;
                                pDst[1] = g;
                                pDst[2] = r;
                                pDst[3] = newA;

                                pSrc += 4;
                                pDst += 4;
                            }
                        }
                    }
                    else
                    {
                        // Radial Fade (Tỏa tròn từ tâm ra ngoài)
                        float cx = BlenderRadialCenter.X * w;
                        float cy = BlenderRadialCenter.Y * h;
                        float rMax = Math.Max(10f, BlenderRadialRadius * Math.Max(w, h));
                        float rMin = 0f; // Tâm rõ 100%, viền trong suốt hoàn toàn

                        for (int py = 0; py < h; py++)
                        {
                            byte* pSrc = pSrcBase + py * srcStride;
                            byte* pDst = pDstBase + py * dstStride;
                            float dy = py - cy;

                            for (int px = 0; px < w; px++)
                            {
                                float dx = px - cx;
                                float dist = MathF.Sqrt(dx * dx + dy * dy);

                                float alphaRatio;
                                if (dist <= rMin)
                                {
                                    alphaRatio = 1.0f;
                                }
                                else if (dist >= rMax)
                                {
                                    alphaRatio = 1.0f - fadeFactor;
                                }
                                else
                                {
                                    float norm = (dist - rMin) / (rMax - rMin);
                                    float t = norm * norm * (3.0f - 2.0f * norm);
                                    alphaRatio = 1.0f - t * fadeFactor;
                                }

                                byte b = pSrc[0];
                                byte g = pSrc[1];
                                byte r = pSrc[2];
                                byte a = pSrc[3];

                                byte newA = (byte)Math.Clamp((int)(a * alphaRatio), 0, 255);

                                pDst[0] = b;
                                pDst[1] = g;
                                pDst[2] = r;
                                pDst[3] = newA;

                                pSrc += 4;
                                pDst += 4;
                            }
                        }
                    }
                }

                _blenderOriginalBackup.UnlockBits(srcData);
                resultBmp.UnlockBits(dstData);

                PublishBlenderImageToShape(_blenderActiveShape, resultBmp);
            }
            catch { }
        }

        private void PublishBlenderImageToShape(ShapeNode shape, Bitmap newBmp)
        {
            if (shape == null || newBmp == null) return;

            var oldImg = shape.CachedImage;
            shape.InvalidateFeatherCache(); // Also calls InvalidateDisplayCache() -> D2DGraphics.InvalidateBitmapCache(DisplayCachedImage)
            if (oldImg != null)
            {
                try { D2DGraphics.InvalidateBitmapCache(oldImg); } catch { }
                if (!ReferenceEquals(oldImg, newBmp))
                {
                    try { oldImg.Dispose(); } catch { }
                }
            }

            shape.NativeD2DBitmap = null;
            shape.NativeD2DOwnerTarget = IntPtr.Zero;
            shape.NativeGifFrames = null;
            shape.IsGifPlaying = false;
            shape.ImagePath = ""; // Ngăn đường vẽ GPU Native nạp lại file ảnh gốc chưa làm mờ
            shape.CachedImage = newBmp;

            ClearEditorScrollCache();
            if (ActiveSlide != null) ActiveSlide.IsThumbnailDirty = true;
        }

        private RectangleF GetActiveBlenderShapeScreenBounds(int slideX, int slideY, float scale)
        {
            if (_blenderActiveShape == null || _blenderActiveShape.Transform == null)
                return RectangleF.Empty;

            float sx = slideX + _blenderActiveShape.Transform.X * scale;
            float sy = slideY + _blenderActiveShape.Transform.Y * scale;
            float sw = _blenderActiveShape.Transform.Width * scale;
            float sh = _blenderActiveShape.Transform.Height * scale;
            return new RectangleF(sx, sy, sw, sh);
        }

        private void CheckBlenderSlideChanged()
        {
            if (_blenderActiveShape != null && !ReferenceEquals(_blenderSlide, ActiveSlide))
            {
                try { _blenderOriginalBackup?.Dispose(); } catch { }
                _blenderOriginalBackup = null;
                _blenderActiveShape = null;
                _blenderSlide = null;
                _blenderDraggingHandle = -1;
            }
        }

        public void DrawImageBlenderCanvasOverlay(D2DGraphics g, int slideX, int slideY, float scale)
        {
            CheckBlenderSlideChanged();
            if (!IsImageBlenderActive || _blenderActiveShape == null) return;

            RectangleF bounds = GetActiveBlenderShapeScreenBounds(slideX, slideY, scale);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // Khung viền tím neon bao quanh ảnh đang điều chỉnh
            using (Pen selPen = new Pen(Color.FromArgb(139, 92, 246), 2f))
            {
                g.DrawRectangle(selPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            float handleRadius = 11f;

            if (CurrentBlenderFadeMode == BlenderFadeMode.Linear)
            {
                PointF pt1 = new PointF(bounds.X + BlenderLinearStart.X * bounds.Width, bounds.Y + BlenderLinearStart.Y * bounds.Height);
                PointF pt2 = new PointF(bounds.X + BlenderLinearEnd.X * bounds.Width, bounds.Y + BlenderLinearEnd.Y * bounds.Height);

                // 1 đường line tím nối giữa 2 cục tròn
                using (Pen linePen = new Pen(Color.FromArgb(139, 92, 246), 3f))
                {
                    g.DrawLine(linePen, pt1.X, pt1.Y, pt2.X, pt2.Y);
                }

                // 2 cục tròn điều khiển
                DrawBlenderHandle(g, pt1, handleRadius, _blenderDraggingHandle == 0);
                DrawBlenderHandle(g, pt2, handleRadius, _blenderDraggingHandle == 1);
            }
            else
            {
                PointF center = new PointF(bounds.X + BlenderRadialCenter.X * bounds.Width, bounds.Y + BlenderRadialCenter.Y * bounds.Height);
                float radPx = BlenderRadialRadius * Math.Max(bounds.Width, bounds.Height);
                PointF edgeHandle = new PointF(center.X + radPx * MathF.Cos(BlenderRadialAngle), center.Y + radPx * MathF.Sin(BlenderRadialAngle));

                // Vòng tròn tỏa mờ từ tâm (nét đứt tím thanh lịch)
                using (Pen circlePen = new Pen(Color.FromArgb(167, 139, 250), 2f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawEllipse(circlePen, center.X - radPx, center.Y - radPx, radPx * 2f, radPx * 2f);
                }

                // Đường bán kính nối tâm tới cục tròn viền
                using (Pen radLinePen = new Pen(Color.FromArgb(196, 181, 253), 1.5f))
                {
                    g.DrawLine(radLinePen, center.X, center.Y, edgeHandle.X, edgeHandle.Y);
                }

                // Cục tròn tâm (di chuyển tâm)
                DrawBlenderHandle(g, center, handleRadius, _blenderDraggingHandle == 0);

                // Cục tròn trên đường tròn (kéo bán kính)
                DrawBlenderHandle(g, edgeHandle, handleRadius - 1f, _blenderDraggingHandle == 1);
            }
        }

        private void DrawBlenderHandle(D2DGraphics g, PointF pt, float radius, bool isDragging)
        {
            RectangleF rect = new RectangleF(pt.X - radius, pt.Y - radius, radius * 2f, radius * 2f);

            // Bóng đổ nhẹ
            using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
            {
                g.FillEllipse(shadowB, rect.X + 1.5f, rect.Y + 1.5f, rect.Width, rect.Height);
            }

            // Nhân trắng bên trong
            using (SolidBrush fillB = new SolidBrush(Color.White))
            {
                g.FillEllipse(fillB, rect);
            }

            // Viền tím neon nổi bật
            Color borderColor = isDragging ? Color.FromArgb(109, 40, 217) : Color.FromArgb(139, 92, 246);
            using (Pen borderP = new Pen(borderColor, 2.8f))
            {
                g.DrawEllipse(borderP, rect);
            }
        }

        public bool HandleImageBlenderMouseDown(Point mousePos, int slideX, int slideY, float scale, MouseButtons buttons)
        {
            if (!IsImageBlenderActive || _blenderActiveShape == null || buttons != MouseButtons.Left) return false;

            RectangleF bounds = GetActiveBlenderShapeScreenBounds(slideX, slideY, scale);
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            float hitRadius = 20f;

            if (CurrentBlenderFadeMode == BlenderFadeMode.Linear)
            {
                PointF pt1 = new PointF(bounds.X + BlenderLinearStart.X * bounds.Width, bounds.Y + BlenderLinearStart.Y * bounds.Height);
                PointF pt2 = new PointF(bounds.X + BlenderLinearEnd.X * bounds.Width, bounds.Y + BlenderLinearEnd.Y * bounds.Height);

                if (DistanceSq(mousePos, pt1) <= hitRadius * hitRadius)
                {
                    _blenderDraggingHandle = 0;
                    if (ParentControl != null) ParentControl.Capture = true;
                    return true;
                }
                if (DistanceSq(mousePos, pt2) <= hitRadius * hitRadius)
                {
                    _blenderDraggingHandle = 1;
                    if (ParentControl != null) ParentControl.Capture = true;
                    return true;
                }
            }
            else
            {
                PointF center = new PointF(bounds.X + BlenderRadialCenter.X * bounds.Width, bounds.Y + BlenderRadialCenter.Y * bounds.Height);
                float radPx = BlenderRadialRadius * Math.Max(bounds.Width, bounds.Height);
                PointF edgeHandle = new PointF(center.X + radPx * MathF.Cos(BlenderRadialAngle), center.Y + radPx * MathF.Sin(BlenderRadialAngle));

                if (DistanceSq(mousePos, center) <= hitRadius * hitRadius)
                {
                    _blenderDraggingHandle = 0;
                    if (ParentControl != null) ParentControl.Capture = true;
                    return true;
                }
                if (DistanceSq(mousePos, edgeHandle) <= hitRadius * hitRadius)
                {
                    _blenderDraggingHandle = 1;
                    if (ParentControl != null) ParentControl.Capture = true;
                    return true;
                }

                // Nhấp vào viền vòng tròn tỏa mờ để kéo bán kính
                float distToCenter = MathF.Sqrt((mousePos.X - center.X) * (mousePos.X - center.X) + (mousePos.Y - center.Y) * (mousePos.Y - center.Y));
                if (Math.Abs(distToCenter - radPx) <= hitRadius)
                {
                    _blenderDraggingHandle = 1;
                    BlenderRadialAngle = MathF.Atan2(mousePos.Y - center.Y, mousePos.X - center.X);
                    if (ParentControl != null) ParentControl.Capture = true;
                    return true;
                }
            }

            return false;
        }

        public bool HandleImageBlenderMouseMove(Point mousePos, int slideX, int slideY, float scale)
        {
            if (!IsImageBlenderActive || _blenderActiveShape == null || _blenderDraggingHandle < 0) return false;

            RectangleF bounds = GetActiveBlenderShapeScreenBounds(slideX, slideY, scale);
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            float normX = Math.Clamp((mousePos.X - bounds.X) / bounds.Width, -0.2f, 1.2f);
            float normY = Math.Clamp((mousePos.Y - bounds.Y) / bounds.Height, -0.2f, 1.2f);

            if (CurrentBlenderFadeMode == BlenderFadeMode.Linear)
            {
                if (_blenderDraggingHandle == 0)
                {
                    BlenderLinearStart = new PointF(normX, normY);
                }
                else if (_blenderDraggingHandle == 1)
                {
                    BlenderLinearEnd = new PointF(normX, normY);
                }
            }
            else
            {
                if (_blenderDraggingHandle == 0)
                {
                    BlenderRadialCenter = new PointF(normX, normY);
                }
                else if (_blenderDraggingHandle == 1)
                {
                    float cx = bounds.X + BlenderRadialCenter.X * bounds.Width;
                    float cy = bounds.Y + BlenderRadialCenter.Y * bounds.Height;
                    float dx = mousePos.X - cx;
                    float dy = mousePos.Y - cy;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    BlenderRadialRadius = Math.Clamp(dist / Math.Max(bounds.Width, bounds.Height), 0.05f, 1.6f);
                    BlenderRadialAngle = MathF.Atan2(dy, dx);
                }
            }

            ApplyBlenderMask();
            InvalidateCallback?.Invoke();
            ParentControl?.Invalidate();
            return true;
        }

        public bool HandleImageBlenderMouseUp(Point mousePos, int slideX, int slideY, float scale)
        {
            if (!IsImageBlenderActive) return false;

            if (_blenderDraggingHandle >= 0)
            {
                _blenderDraggingHandle = -1;
                if (ParentControl != null) ParentControl.Capture = false;
                InvalidateCallback?.Invoke();
                ParentControl?.Invalidate();
                return true;
            }

            return false;
        }

        public bool IsMouseOverBlenderHandle(Point mousePos, int slideX, int slideY, float scale)
        {
            if (!IsImageBlenderActive || _blenderActiveShape == null) return false;

            RectangleF bounds = GetActiveBlenderShapeScreenBounds(slideX, slideY, scale);
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            float hitRadius = 18f;

            if (CurrentBlenderFadeMode == BlenderFadeMode.Linear)
            {
                PointF pt1 = new PointF(bounds.X + BlenderLinearStart.X * bounds.Width, bounds.Y + BlenderLinearStart.Y * bounds.Height);
                PointF pt2 = new PointF(bounds.X + BlenderLinearEnd.X * bounds.Width, bounds.Y + BlenderLinearEnd.Y * bounds.Height);
                return (DistanceSq(mousePos, pt1) <= hitRadius * hitRadius || DistanceSq(mousePos, pt2) <= hitRadius * hitRadius);
            }
            else
            {
                PointF center = new PointF(bounds.X + BlenderRadialCenter.X * bounds.Width, bounds.Y + BlenderRadialCenter.Y * bounds.Height);
                float radPx = BlenderRadialRadius * Math.Max(bounds.Width, bounds.Height);
                PointF edgeHandle = new PointF(center.X + radPx * MathF.Cos(BlenderRadialAngle), center.Y + radPx * MathF.Sin(BlenderRadialAngle));
                float distToCenter = MathF.Sqrt((mousePos.X - center.X) * (mousePos.X - center.X) + (mousePos.Y - center.Y) * (mousePos.Y - center.Y));
                return (DistanceSq(mousePos, center) <= hitRadius * hitRadius || DistanceSq(mousePos, edgeHandle) <= hitRadius * hitRadius || Math.Abs(distToCenter - radPx) <= hitRadius);
            }
        }

        private static float DistanceSq(Point p, PointF pf)
        {
            float dx = p.X - pf.X;
            float dy = p.Y - pf.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Vẽ TaskPane điều khiển Image Blender (Hỗ trợ chế độ màu xám khi chưa chọn ảnh mà không chặn người dùng)
        /// </summary>
        public void DrawImageBlenderTaskPane(D2DGraphics g, int left, int top, int width, int height)
        {
            int y = top - ElementsScrollY;
            using var titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 8.5f);
            using var miniFont = new Font("Segoe UI", 8f);

            int pad = 12;
            int availW = Math.Max(120, width - pad * 2);

            CheckBlenderSlideChanged();
            bool hasImage = (_blenderActiveShape != null && _blenderOriginalBackup != null);

            // 1. Header: [←] Image Blender
            var backBtn = new Rectangle(left + pad, y + 2, 26, 26);
            using (var bFill = new SolidBrush(Color.FromArgb(243, 244, 246)))
            using (var bBorder = new Pen(Color.FromArgb(229, 231, 235), 1f))
            {
                g.FillRoundedRectangle(bFill, backBtn, 6);
                g.DrawRoundedRectangle(bBorder, backBtn, 6);
            }
            CatalogText(g, "←", new Font("Segoe UI", 11f, FontStyle.Bold), Color.FromArgb(55, 65, 81), backBtn, true);
            CatalogHit(backBtn, "blender:exit");

            CatalogText(g, "Image Blender", titleFont, Color.FromArgb(30, 41, 59), new RectangleF(left + pad + 32, y + 3, availW - 32, 24));
            y += 34;

            // Nếu CHƯA chọn ảnh: Hiển thị hộp thông báo hướng dẫn nhẹ nhàng, không chặn người dùng
            if (!hasImage)
            {
                var promptBox = new Rectangle(left + pad, y, availW, 58);
                using (var pBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (var pBorder = new Pen(Color.FromArgb(203, 213, 225), 1.2f) { DashStyle = DashStyle.Dash })
                {
                    g.FillRoundedRectangle(pBg, promptBox, 8);
                    g.DrawRoundedRectangle(pBorder, promptBox, 8);
                }
                CatalogText(g, AppL10n.T("🖼 Chưa chọn hình ảnh", "🖼 No image selected"), subFont, Color.FromArgb(71, 85, 105), new RectangleF(promptBox.X + 8, promptBox.Y + 8, promptBox.Width - 16, 20), true);
                CatalogText(g, AppL10n.T("Nhấp chọn một ảnh trên slide để điều chỉnh", "Click an image on the slide to adjust"), miniFont, Color.FromArgb(148, 163, 184), new RectangleF(promptBox.X + 8, promptBox.Y + 30, promptBox.Width - 16, 18), true);
                y += 66;
            }

            // 2. Tiêu đề nhóm: Công cụ làm rõ dần
            Color sectionTitleColor = hasImage ? Color.FromArgb(51, 65, 85) : Color.FromArgb(156, 163, 175);
            CatalogText(g, AppL10n.T("Công cụ làm rõ dần", "Fade Blend Tool"), subFont, sectionTitleColor, new RectangleF(left + pad, y, availW, 20));
            y += 26;

            // 3. 2 Dạng: Đường kẻ | Tỏa tròn (Đã bỏ cọ vẽ hoàn toàn theo yêu cầu)
            int cardGap = 8;
            int cardW = (availW - cardGap) / 2;
            int cardH = 92;

            // Thẻ Đường kẻ
            var cardLinear = new Rectangle(left + pad, y, cardW, cardH);
            bool isLinear = (CurrentBlenderFadeMode == BlenderFadeMode.Linear);
            Color linBg = !hasImage ? Color.FromArgb(248, 250, 252) : (isLinear ? Color.FromArgb(245, 243, 255) : Color.White);
            Color linBorder = !hasImage ? Color.FromArgb(226, 232, 240) : (isLinear ? Color.FromArgb(139, 92, 246) : Color.FromArgb(226, 232, 240));

            using (var lFill = new SolidBrush(linBg))
            using (var lBorder = new Pen(linBorder, (hasImage && isLinear) ? 2f : 1f))
            {
                g.FillRoundedRectangle(lFill, cardLinear, 8);
                g.DrawRoundedRectangle(lBorder, cardLinear, 8);
            }
            // Icon minh họa Đường kẻ
            var iconLin = new Rectangle(cardLinear.X + (cardW - 46) / 2, cardLinear.Y + 8, 46, 46);
            if (hasImage)
            {
                using (var linGrad = new LinearGradientBrush(iconLin, Color.FromArgb(139, 92, 246), Color.FromArgb(243, 244, 246), 0f))
                {
                    g.FillRoundedRectangle(linGrad, iconLin, 6);
                }
            }
            else
            {
                using (var linGrad = new LinearGradientBrush(iconLin, Color.FromArgb(203, 213, 225), Color.FromArgb(241, 245, 249), 0f))
                {
                    g.FillRoundedRectangle(linGrad, iconLin, 6);
                }
            }
            Color linTxtColor = !hasImage ? Color.FromArgb(156, 163, 175) : (isLinear ? Color.FromArgb(109, 40, 217) : Color.FromArgb(55, 65, 81));
            CatalogText(g, AppL10n.T("Đường kẻ", "Linear"), labelFont, linTxtColor, new RectangleF(cardLinear.X, cardLinear.Y + 60, cardW, 22), true);
            if (hasImage) CatalogHit(cardLinear, "blender:mode:linear");

            // Thẻ Tỏa tròn
            var cardRadial = new Rectangle(left + pad + cardW + cardGap, y, cardW, cardH);
            bool isRadial = (CurrentBlenderFadeMode == BlenderFadeMode.Radial);
            Color radBg = !hasImage ? Color.FromArgb(248, 250, 252) : (isRadial ? Color.FromArgb(245, 243, 255) : Color.White);
            Color radBorder = !hasImage ? Color.FromArgb(226, 232, 240) : (isRadial ? Color.FromArgb(139, 92, 246) : Color.FromArgb(226, 232, 240));

            using (var rFill = new SolidBrush(radBg))
            using (var rBorder = new Pen(radBorder, (hasImage && isRadial) ? 2f : 1f))
            {
                g.FillRoundedRectangle(rFill, cardRadial, 8);
                g.DrawRoundedRectangle(rBorder, cardRadial, 8);
            }
            // Icon minh họa Tỏa tròn
            var iconRad = new Rectangle(cardRadial.X + (cardW - 46) / 2, cardRadial.Y + 8, 46, 46);
            if (hasImage)
            {
                using (var pathRad = new GraphicsPath())
                {
                    pathRad.AddEllipse(iconRad);
                    using (var radBrush = new PathGradientBrush(pathRad))
                    {
                        radBrush.CenterColor = Color.FromArgb(139, 92, 246);
                        radBrush.SurroundColors = new[] { Color.FromArgb(243, 244, 246) };
                        g.FillRoundedRectangle(new SolidBrush(Color.FromArgb(243, 244, 246)), iconRad, 6);
                        g.FillEllipse(radBrush, iconRad);
                    }
                }
            }
            else
            {
                using (var pathRad = new GraphicsPath())
                {
                    pathRad.AddEllipse(iconRad);
                    using (var radBrush = new PathGradientBrush(pathRad))
                    {
                        radBrush.CenterColor = Color.FromArgb(203, 213, 225);
                        radBrush.SurroundColors = new[] { Color.FromArgb(241, 245, 249) };
                        g.FillRoundedRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), iconRad, 6);
                        g.FillEllipse(radBrush, iconRad);
                    }
                }
            }
            Color radTxtColor = !hasImage ? Color.FromArgb(156, 163, 175) : (isRadial ? Color.FromArgb(109, 40, 217) : Color.FromArgb(55, 65, 81));
            CatalogText(g, AppL10n.T("Tỏa tròn", "Radial"), labelFont, radTxtColor, new RectangleF(cardRadial.X, cardRadial.Y + 60, cardW, 22), true);
            if (hasImage) CatalogHit(cardRadial, "blender:mode:radial");

            y += cardH + 16;

            // 4. Thanh trượt Cường độ (Intensity) kèm ô số
            Color intLabelColor = hasImage ? Color.FromArgb(51, 65, 85) : Color.FromArgb(156, 163, 175);
            CatalogText(g, AppL10n.T("Cường độ", "Intensity"), subFont, intLabelColor, new RectangleF(left + pad, y, availW - 44, 18));

            // Ô số giá trị
            var numBox = new Rectangle(left + pad + availW - 38, y - 2, 38, 22);
            using (var nFill = new SolidBrush(Color.FromArgb(243, 244, 246)))
            using (var nBorder = new Pen(Color.FromArgb(229, 231, 235), 1f))
            {
                g.FillRoundedRectangle(nFill, numBox, 4);
                g.DrawRoundedRectangle(nBorder, numBox, 4);
            }
            Color numColor = hasImage ? Color.FromArgb(55, 65, 81) : Color.FromArgb(156, 163, 175);
            CatalogText(g, BlenderIntensity.ToString(), labelFont, numColor, numBox, true);
            y += 24;

            // Thanh trượt
            int trackH = 6;
            int trackW = availW;
            var trackRect = new Rectangle(left + pad, y + 6, trackW, trackH);
            using (var trackBg = new SolidBrush(Color.FromArgb(229, 231, 235)))
            {
                g.FillRoundedRectangle(trackBg, trackRect, 3);
            }
            int activeW = (int)(trackW * (BlenderIntensity / 100f));
            var activeRect = new Rectangle(left + pad, y + 6, activeW, trackH);
            using (var activeBg = new SolidBrush(hasImage ? Color.FromArgb(139, 92, 246) : Color.FromArgb(203, 213, 225)))
            {
                g.FillRoundedRectangle(activeBg, activeRect, 3);
            }
            // Nút kéo thumb
            var thumbRect = new Rectangle(left + pad + activeW - 8, y + 1, 16, 16);
            using (var thFill = new SolidBrush(Color.White))
            using (var thBorder = new Pen(hasImage ? Color.FromArgb(139, 92, 246) : Color.FromArgb(203, 213, 225), 2.5f))
            {
                g.FillEllipse(thFill, thumbRect);
                g.DrawEllipse(thBorder, thumbRect);
            }

            if (hasImage)
            {
                LastBlenderSliderTrackRect = new Rectangle(left + pad, y, trackW, 20);
                CatalogHit(LastBlenderSliderTrackRect, "blender:slider");
            }
            y += 32;

            // 5. Nút Khôi phục gốc và Nút Áp dụng / Xong
            int actBtnH = 34;
            int halfW = (availW - 8) / 2;

            var btnReset = new Rectangle(left + pad, y, halfW, actBtnH);
            using (var rstFill = new SolidBrush(Color.FromArgb(243, 244, 246)))
            using (var rstBorder = new Pen(Color.FromArgb(209, 213, 219), 1f))
            {
                g.FillRoundedRectangle(rstFill, btnReset, 6);
                g.DrawRoundedRectangle(rstBorder, btnReset, 6);
            }
            CatalogText(g, AppL10n.T("Khôi phục", "Reset"), labelFont, hasImage ? Color.FromArgb(55, 65, 81) : Color.FromArgb(156, 163, 175), btnReset, true);
            if (hasImage) CatalogHit(btnReset, "blender:reset");

            var btnDone = new Rectangle(left + pad + halfW + 8, y, halfW, actBtnH);
            if (hasImage)
            {
                using (var doneFill = new LinearGradientBrush(btnDone, Color.FromArgb(99, 102, 241), Color.FromArgb(139, 92, 246), 45f))
                {
                    g.FillRoundedRectangle(doneFill, btnDone, 6);
                }
                CatalogText(g, AppL10n.T("Áp dụng", "Apply"), subFont, Color.White, btnDone, true);
                CatalogHit(btnDone, "blender:apply");
            }
            else
            {
                using (var doneFill = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    g.FillRoundedRectangle(doneFill, btnDone, 6);
                }
                CatalogText(g, AppL10n.T("Áp dụng", "Apply"), subFont, Color.FromArgb(148, 163, 184), btnDone, true);
            }

            y += actBtnH + 18;

            // Hướng dẫn tương tác trực quan
            var tipBox = new Rectangle(left + pad, y, availW, 64);
            using (var tipBg = new SolidBrush(Color.FromArgb(249, 250, 252)))
            using (var tipBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRoundedRectangle(tipBg, tipBox, 6);
                g.DrawRoundedRectangle(tipBorder, tipBox, 6);
            }
            if (!hasImage)
            {
                CatalogText(g, AppL10n.T("💡 Hãy nhấp chọn một hình ảnh trên slide để bắt đầu làm mờ dần.", "💡 Select an image on the slide to start blending."), miniFont, Color.FromArgb(148, 163, 184), new RectangleF(tipBox.X + 8, tipBox.Y + 8, tipBox.Width - 16, tipBox.Height - 16), false);
            }
            else if (CurrentBlenderFadeMode == BlenderFadeMode.Linear)
            {
                CatalogText(g, AppL10n.T("💡 Chấm 1 là mốc rõ, chấm 2 là điểm mờ hẳn. Càng xa càng mượt.", "💡 Dot 1 is opaque start, dot 2 is full fade. Further apart = smoother."), miniFont, Color.FromArgb(100, 116, 139), new RectangleF(tipBox.X + 8, tipBox.Y + 8, tipBox.Width - 16, tipBox.Height - 16), false);
            }
            else
            {
                CatalogText(g, AppL10n.T("💡 Tâm rõ nét, viền mờ hẳn. Bán kính càng lớn càng mượt.", "💡 Clear center, faded edges. Larger radius = smoother."), miniFont, Color.FromArgb(100, 116, 139), new RectangleF(tipBox.X + 8, tipBox.Y + 8, tipBox.Width - 16, tipBox.Height - 16), false);
            }

            catalogContentHeight = Math.Max(0, y + 80 - top);
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public static class LiquidGlassHelper
    {
        #region Native Windows DWM Blur Behind / Acrylic API
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_ENABLE_BLURBEHIND = 3;
        private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public static void EnableWindowBlur(IntPtr hWnd, bool isAcrylic = true, int alpha = 180)
        {
            try
            {
                var accent = new AccentPolicy();
                accent.AccentState = isAcrylic ? ACCENT_ENABLE_ACRYLICBLURBEHIND : ACCENT_ENABLE_BLURBEHIND;
                accent.AccentFlags = 2;
                accent.GradientColor = (alpha << 24) | (0xF0F4F8 & 0xFFFFFF);

                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                SetWindowCompositionAttribute(hWnd, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch
            {
                // Fallback gracefully
            }
        }
        #endregion

        #region Rounded Rectangle Path Helper
        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0.5f)
            {
                path.AddRectangle(rect);
                return path;
            }

            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        #endregion

        #region iOS 26 / VisionOS Liquid Glass Drawing Engine
        private static Bitmap _cachedBackdrop = null;
        private static int _cachedW = 0;
        private static int _cachedH = 0;

        /// <summary>
        /// Vẽ hình nền Liquid Glass đa sắc lượn sóng được tối ưu hóa đệm Bitmap (Zero-Lag 60 FPS khi cuộn)
        /// </summary>
        public static void DrawLiquidGlassBackdrop(Graphics g, int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            // Cache background bitmap để cuộn trang mượt mà tuyệt đối (không tính toán lại gradient phức tạp mỗi khung hình)
            if (_cachedBackdrop == null || _cachedW != width || _cachedH != height)
            {
                if (_cachedBackdrop != null) _cachedBackdrop.Dispose();
                _cachedBackdrop = new Bitmap(width, height);
                _cachedW = width;
                _cachedH = height;

                using (Graphics bgG = Graphics.FromImage(_cachedBackdrop))
                {
                    bgG.SmoothingMode = SmoothingMode.HighQuality;
                    bgG.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // 1. Nền chuyển màu cơ sở thanh nhã chuẩn VisionOS / iOS 26
                    Rectangle bgRect = new Rectangle(0, 0, width, height);
                    using (LinearGradientBrush baseBrush = new LinearGradientBrush(bgRect, Color.FromArgb(242, 246, 252), Color.FromArgb(228, 236, 248), 45f))
                    {
                        bgG.FillRectangle(baseBrush, bgRect);
                    }

                    // 2. Các quả cầu ánh sáng hữu cơ phát quang mềm mại
                    DrawAmbientOrb(bgG, width * 0.16f, 180, 480, Color.FromArgb(65, 59, 130, 246), Color.FromArgb(0, 59, 130, 246));
                    DrawAmbientOrb(bgG, width * 0.82f, 250, 520, Color.FromArgb(55, 147, 51, 234), Color.FromArgb(0, 147, 51, 234));
                    DrawAmbientOrb(bgG, width * 0.38f, height * 0.75f, 560, Color.FromArgb(48, 244, 63, 94), Color.FromArgb(0, 244, 63, 94));
                    DrawAmbientOrb(bgG, width * 0.78f, height * 0.85f, 500, Color.FromArgb(42, 16, 185, 129), Color.FromArgb(0, 16, 185, 129));
                }
            }

            g.DrawImageUnscaled(_cachedBackdrop, 0, 0);
        }

        private static void DrawAmbientOrb(Graphics g, float cx, float cy, float diameter, Color centerCol, Color edgeCol)
        {
            RectangleF orbRect = new RectangleF(cx - diameter / 2f, cy - diameter / 2f, diameter, diameter);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(orbRect);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = centerCol;
                    pgb.SurroundColors = new Color[] { edgeCol };
                    g.FillEllipse(pgb, orbRect);
                }
            }
        }

        /// <summary>
        /// Vẽ tấm kính chất lỏng trong suốt (Crystal-Clear Liquid Glass)
        /// Đặc tính:
        /// 1. Kính trong suốt tinh khiết nhìn xuyên thấu ánh sáng nền (không phải kính mờ đục).
        /// 2. Viền trắng mờ đổ vô trong (Soft Inner White Bevel / Drop Glow).
        /// 3. Hiệu ứng ánh sáng bóng kính cong vòm (Glossy Specular Curved Glare & Gleam).
        /// </summary>
        public static void DrawLiquidGlassCard(Graphics g, RectangleF rect, float radius, bool isHovered, Color accentGlow)
        {
            if (rect.Width <= 4 || rect.Height <= 4) return;

            // A. Hào quang ánh sáng bao quanh khi Hover
            if (isHovered)
            {
                RectangleF auraRect = new RectangleF(rect.X - 8, rect.Y - 6, rect.Width + 16, rect.Height + 14);
                using (GraphicsPath auraPath = CreateRoundedRectanglePath(auraRect, radius + 6))
                using (Pen auraPen = new Pen(Color.FromArgb(45, accentGlow.R, accentGlow.G, accentGlow.B), 4f))
                {
                    g.DrawPath(auraPen, auraPath);
                }
            }

            // B. Bóng đổ tiếp xúc chân thực (Drop Shadow)
            using (GraphicsPath sp = CreateRoundedRectanglePath(new RectangleF(rect.X, rect.Y + (isHovered ? 4 : 2), rect.Width, rect.Height), radius))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(isHovered ? 22 : 12, 15, 23, 42)))
            {
                g.FillPath(sb, sp);
            }

            using (GraphicsPath cardPath = CreateRoundedRectanglePath(rect, radius))
            {
                // C. Thân kính trong suốt tinh khiết (Crystal Clear Translucent Glass Fill)
                // Trong suốt cao, cho phép nhìn thấu hoàn hảo ánh sáng nền phía sau
                Color topGlass = Color.FromArgb(isHovered ? 85 : 55, 255, 255, 255);
                Color botGlass = Color.FromArgb(isHovered ? 35 : 18, 255, 255, 255);
                using (LinearGradientBrush glassBrush = new LinearGradientBrush(rect, topGlass, botGlass, 55f))
                {
                    g.FillPath(glassBrush, cardPath);
                }

                // D. Viền trắng mờ đổ vô trong (Inner Soft White Bevel / Glow Inward)
                // Tạo ảo giác mặt kính vát cong khúc xạ ánh sáng trắng đổ vào lòng tấm kính
                using (Pen inGlow = new Pen(Color.FromArgb(isHovered ? 120 : 75, 255, 255, 255), 2.2f))
                {
                    g.DrawPath(inGlow, cardPath);
                }

                // E. Hiệu ứng ánh sáng bóng kính cong vòm (Glossy Specular Glare)
                RectangleF glossRect = new RectangleF(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height * 0.45f);
                if (glossRect.Height > 10)
                {
                    using (GraphicsPath glossPath = CreateRoundedRectanglePath(glossRect, radius - 2))
                    using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                        glossRect,
                        Color.FromArgb(isHovered ? 110 : 70, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255), 90f))
                    {
                        g.FillPath(glossBrush, glossPath);
                    }
                }

                // F. Vệt sáng cạnh trên (Top Gleam Specular Edge)
                using (Pen gleamPen = new Pen(Color.FromArgb(isHovered ? 235 : 185, 255, 255, 255), 1.5f))
                {
                    g.DrawLine(gleamPen, rect.X + radius, rect.Y + 1f, rect.Right - radius, rect.Y + 1f);
                }

                // G. Đường viền phản quang ngoài cùng (Outer Specular Border)
                Color borderTopLeft = Color.FromArgb(isHovered ? 245 : 200, 255, 255, 255);
                Color borderBotRight = isHovered ? Color.FromArgb(140, accentGlow.R, accentGlow.G, accentGlow.B) : Color.FromArgb(80, 255, 255, 255);
                using (LinearGradientBrush borderGrad = new LinearGradientBrush(rect, borderTopLeft, borderBotRight, 45f))
                using (Pen borderPen = new Pen(borderGrad, 1.2f))
                {
                    g.DrawPath(borderPen, cardPath);
                }
            }
        }

        /// <summary>
        /// Vẽ nút bấm viên thuốc kính lỏng trong suốt (Liquid Glass Pill)
        /// </summary>
        public static void DrawLiquidGlassPill(Graphics g, RectangleF rect, float radius, bool isSelected, bool isHovered, Color accent)
        {
            if (rect.Width <= 2 || rect.Height <= 2) return;

            using (GraphicsPath path = CreateRoundedRectanglePath(rect, radius))
            {
                if (isSelected)
                {
                    // Viên thuốc được chọn: Màu thương hiệu rực rỡ với bóng kính
                    Color topC = Color.FromArgb(245, accent.R, accent.G, accent.B);
                    Color botC = Color.FromArgb(210, Math.Max(0, accent.R - 25), Math.Max(0, accent.G - 25), Math.Max(0, accent.B - 25));
                    using (LinearGradientBrush b = new LinearGradientBrush(rect, topC, botC, 90f))
                    {
                        g.FillPath(b, path);
                    }

                    // Vệt bóng kính đỉnh
                    RectangleF specR = new RectangleF(rect.X + 2, rect.Y + 1, rect.Width - 4, rect.Height * 0.45f);
                    using (GraphicsPath sp = CreateRoundedRectanglePath(specR, radius - 1))
                    using (LinearGradientBrush sb = new LinearGradientBrush(specR, Color.FromArgb(95, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                    {
                        g.FillPath(sb, sp);
                    }

                    // Viền sáng
                    using (Pen p = new Pen(Color.FromArgb(220, 255, 255, 255), 1.2f))
                    {
                        g.DrawPath(p, path);
                    }
                }
                else
                {
                    // Viên thuốc chưa chọn: Kính trong suốt có viền trắng mờ đổ vô trong
                    Color topC = Color.FromArgb(isHovered ? 90 : 45, 255, 255, 255);
                    Color botC = Color.FromArgb(isHovered ? 40 : 18, 255, 255, 255);
                    using (LinearGradientBrush b = new LinearGradientBrush(rect, topC, botC, 90f))
                    {
                        g.FillPath(b, path);
                    }

                    // Viền mờ đổ vô trong (Inner Glow)
                    using (Pen inGlow = new Pen(Color.FromArgb(isHovered ? 90 : 50, 255, 255, 255), 2.2f))
                    {
                        g.DrawPath(inGlow, path);
                    }

                    // Viền ngoài
                    Color bCol = isHovered ? Color.FromArgb(190, accent.R, accent.G, accent.B) : Color.FromArgb(130, 255, 255, 255);
                    using (Pen p = new Pen(bCol, 1.0f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bộ vẽ Vector & Nút bấm / Input Liquid Glass cao cấp chuẩn VisionOS / macOS / GenOffice Theme.
    /// Thiết kế nút dạng kính trắng trong suốt (Frosted Translucent White Glass) đồng bộ hoàn hảo với thanh Tab,
    /// có viền trắng mờ tán xạ vô trong như kính, vệt bóng kính vòm, căn lề Pixel-Perfect.
    /// </summary>
    public static class AiVectorGlassRenderer
    {
        public static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
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

        public static void DrawGlassButton(Graphics g, RectangleF bounds, string text, Action<Graphics, float, float, float, Color> drawVectorIcon, bool isHovered, bool isPressed, Color accentColor, Color textColor, bool isPrimary = false)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (GraphicsPath path = CreateRoundedPath(bounds, bounds.Height / 2f))
            {
                // 1. Drop Shadow
                if (!isPressed)
                {
                    RectangleF shadowRect = new RectangleF(bounds.X, bounds.Y + (isHovered ? 2.5f : 1.5f), bounds.Width, bounds.Height);
                    using (GraphicsPath sp = CreateRoundedPath(shadowRect, bounds.Height / 2f))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(isHovered ? 26 : 14, 15, 23, 42)))
                    {
                        g.FillPath(sb, sp);
                    }
                }

                // 1.5. Opaque Base Fill (chặn chữ phía sau xuyên thấu vô)
                Color baseFill;
                if (isPrimary)
                {
                    baseFill = isPressed ? Color.FromArgb(224, 231, 255) : (isHovered ? Color.FromArgb(235, 240, 255) : Color.FromArgb(240, 244, 255));
                }
                else
                {
                    baseFill = isPressed ? Color.FromArgb(235, 240, 248) : (isHovered ? Color.FromArgb(245, 248, 255) : Color.FromArgb(246, 249, 254));
                }
                using (SolidBrush baseBrush = new SolidBrush(baseFill))
                {
                    g.FillPath(baseBrush, path);
                }

                // 2. Translucent Liquid Glass Sheen
                Color topCol, botCol;
                if (isPrimary)
                {
                    topCol = Color.FromArgb(isHovered ? 180 : 130, 255, 255, 255);
                    botCol = Color.FromArgb(isHovered ? 100 : 50, 210, 225, 250);
                }
                else
                {
                    topCol = Color.FromArgb(isHovered ? 180 : 120, 255, 255, 255);
                    botCol = Color.FromArgb(isHovered ? 80 : 30, 230, 240, 255);
                }

                if (isPressed)
                {
                    topCol = Color.FromArgb(90, 210, 220, 240);
                    botCol = Color.FromArgb(50, 200, 215, 235);
                }

                using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, topCol, botCol, LinearGradientMode.Vertical))
                {
                    g.FillPath(lgb, path);
                }

                // 3. Viền trắng mờ tán xạ vô trong như kính (Inner White Scattering Glow)
                using (Pen inGlow = new Pen(Color.FromArgb(isHovered ? 200 : 140, 255, 255, 255), 2.2f))
                {
                    g.DrawPath(inGlow, path);
                }

                // 4. Vệt bóng kính đỉnh vòm cong (Curved Glossy Specular Glare)
                RectangleF topHighlight = new RectangleF(bounds.X + 2, bounds.Y + 1, bounds.Width - 4, bounds.Height * 0.45f);
                using (GraphicsPath tp = CreateRoundedPath(topHighlight, topHighlight.Height / 2f))
                using (LinearGradientBrush hb = new LinearGradientBrush(topHighlight, Color.FromArgb(isHovered ? 160 : 110, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                {
                    g.FillPath(hb, tp);
                }

                // 5. Đường viền ngoài phản quang (Outer Specular Border)
                Color borderColor;
                if (isHovered)
                {
                    borderColor = isPrimary ? Color.FromArgb(210, 99, 102, 241) : Color.FromArgb(190, 99, 102, 241);
                }
                else
                {
                    borderColor = Color.FromArgb(180, 255, 255, 255);
                }

                using (Pen bp = new Pen(borderColor, isHovered ? 1.4f : 1.0f))
                {
                    g.DrawPath(bp, path);
                }
            }

            // 5. Draw Icon & Text (Mathematically Centered)
            Color finalTextColor = isHovered ? Color.FromArgb(79, 70, 229) : Color.FromArgb(30, 41, 59);
            Color finalIconColor = isHovered ? Color.FromArgb(79, 70, 229) : (isPrimary ? Color.FromArgb(99, 102, 241) : Color.FromArgb(71, 85, 105));

            float iconSize = string.IsNullOrEmpty(text) ? 20f : 15f;
            if (string.IsNullOrEmpty(text))
            {
                // Icon Only Button
                if (drawVectorIcon != null)
                {
                    float ix = bounds.X + (bounds.Width - iconSize) / 2f;
                    float iy = bounds.Y + (bounds.Height - iconSize) / 2f;
                    drawVectorIcon(g, ix, iy, iconSize, finalIconColor);
                }
            }
            else
            {
                // Icon + Text Button
                float contentX = bounds.X + 16;
                if (drawVectorIcon != null)
                {
                    float iconY = bounds.Y + (bounds.Height - iconSize) / 2f;
                    drawVectorIcon(g, contentX, iconY, iconSize, finalIconColor);
                    contentX += iconSize + 8;
                }

                using (Font font = new Font("Segoe UI", 9.2f, FontStyle.Bold))
                using (SolidBrush tb = new SolidBrush(finalTextColor))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = drawVectorIcon != null ? StringAlignment.Near : StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    RectangleF textRect = new RectangleF(contentX, bounds.Y, bounds.Right - contentX - 10, bounds.Height);
                    if (drawVectorIcon == null) textRect = bounds;
                    g.DrawString(text, font, tb, textRect, sf);
                }
            }
        }

        #region Vector Icon Drawing Methods
        public static void DrawVectorSparkle(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            float cx = x + size / 2f;
            float cy = y + size / 2f;
            float r = size / 2f;
            float innerR = size * 0.22f;

            using (GraphicsPath p = new GraphicsPath())
            using (SolidBrush b = new SolidBrush(color))
            {
                p.AddPolygon(new PointF[] {
                    new PointF(cx, cy - r),
                    new PointF(cx + innerR, cy - innerR),
                    new PointF(cx + r, cy),
                    new PointF(cx + innerR, cy + innerR),
                    new PointF(cx, cy + r),
                    new PointF(cx - innerR, cy + innerR),
                    new PointF(cx - r, cy),
                    new PointF(cx - innerR, cy - innerR)
                });
                g.FillPath(b, p);
            }
        }

        public static void DrawVectorKey(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (Pen p = new Pen(color, size * 0.12f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;

                float ringD = size * 0.45f;
                g.DrawEllipse(p, x + 1, y + 1, ringD, ringD);

                float stemX1 = x + 1 + ringD * 0.85f;
                float stemY1 = y + 1 + ringD * 0.85f;
                float stemX2 = x + size - 2;
                float stemY2 = y + size - 2;
                g.DrawLine(p, stemX1, stemY1, stemX2, stemY2);

                g.DrawLine(p, stemX2 - size * 0.12f, stemY2 - size * 0.12f, stemX2 - size * 0.12f + size * 0.18f, stemY2 - size * 0.12f - size * 0.18f);
                g.DrawLine(p, stemX2, stemY2, stemX2 + size * 0.15f, stemY2 - size * 0.15f);
            }
        }

        public static void DrawVectorPlus(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            float mid = size / 2f;
            using (Pen p = new Pen(color, size * 0.18f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, x + mid, y + 2, x + mid, y + size - 2);
                g.DrawLine(p, x + 2, y + mid, x + size - 2, y + mid);
            }
        }

        public static void DrawVectorTrash(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (Pen p = new Pen(color, size * 0.12f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;

                g.DrawLine(p, x + 2, y + size * 0.25f, x + size - 2, y + size * 0.25f);
                g.DrawLine(p, x + size * 0.35f, y + size * 0.25f, x + size * 0.35f, y + size * 0.12f);
                g.DrawLine(p, x + size * 0.35f, y + size * 0.12f, x + size * 0.65f, y + size * 0.12f);
                g.DrawLine(p, x + size * 0.65f, y + size * 0.12f, x + size * 0.65f, y + size * 0.25f);

                g.DrawLine(p, x + size * 0.2f, y + size * 0.28f, x + size * 0.26f, y + size - 2);
                g.DrawLine(p, x + size * 0.26f, y + size - 2, x + size * 0.74f, y + size - 2);
                g.DrawLine(p, x + size * 0.74f, y + size - 2, x + size * 0.8f, y + size * 0.28f);

                g.DrawLine(p, x + size * 0.42f, y + size * 0.42f, x + size * 0.42f, y + size * 0.82f);
                g.DrawLine(p, x + size * 0.58f, y + size * 0.42f, x + size * 0.58f, y + size * 0.82f);
            }
        }

        public static void DrawVectorEye(Graphics g, float x, float y, float size, Color color, bool isSlash)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float penWidth = Math.Max(1.35f, size * (2.0f / 24.0f));
            using (Pen p = new Pen(color, penWidth))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;

                // Scale from 24x24 Lucide viewBox coordinates
                float scale = size / 24.0f;
                float x0 = x;
                float y0 = y;

                // 1. Outer Eye Eyelid Almond Contour (Lucide path d="M2.062 12.348... 10.75 10.75...")
                using (GraphicsPath eyePath = new GraphicsPath())
                {
                    // Top eyelid
                    eyePath.AddBezier(
                        x0 + 2.0f * scale, y0 + 12.0f * scale,
                        x0 + 6.5f * scale, y0 + 4.8f * scale,
                        x0 + 17.5f * scale, y0 + 4.8f * scale,
                        x0 + 22.0f * scale, y0 + 12.0f * scale
                    );
                    // Bottom eyelid
                    eyePath.AddBezier(
                        x0 + 22.0f * scale, y0 + 12.0f * scale,
                        x0 + 17.5f * scale, y0 + 19.2f * scale,
                        x0 + 6.5f * scale, y0 + 19.2f * scale,
                        x0 + 2.0f * scale, y0 + 12.0f * scale
                    );
                    g.DrawPath(p, eyePath);
                }

                // 2. Iris/Pupil Center Circle (<circle cx="12" cy="12" r="3"/>)
                float pupilDiameter = 6.0f * scale;
                float pupilX = x0 + 12.0f * scale - pupilDiameter / 2f;
                float pupilY = y0 + 12.0f * scale - pupilDiameter / 2f;
                g.DrawEllipse(p, pupilX, pupilY, pupilDiameter, pupilDiameter);

                // 3. Eye-Off Slash Line (when password is hidden / slashed)
                if (isSlash)
                {
                    using (Pen slashPen = new Pen(Color.FromArgb(220, 38, 38), penWidth * 1.15f))
                    {
                        slashPen.StartCap = LineCap.Round;
                        slashPen.EndCap = LineCap.Round;
                        g.DrawLine(slashPen, x0 + 3.2f * scale, y0 + 3.2f * scale, x0 + 20.8f * scale, y0 + 20.8f * scale);
                    }
                }
            }
        }

        public static void DrawVectorLightning(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (GraphicsPath p = new GraphicsPath())
            using (SolidBrush b = new SolidBrush(color))
            {
                p.AddPolygon(new PointF[] {
                    new PointF(x + size * 0.55f, y + 1),
                    new PointF(x + size * 0.15f, y + size * 0.52f),
                    new PointF(x + size * 0.48f, y + size * 0.52f),
                    new PointF(x + size * 0.38f, y + size - 1),
                    new PointF(x + size * 0.85f, y + size * 0.44f),
                    new PointF(x + size * 0.52f, y + size * 0.44f)
                });
                g.FillPath(b, p);
            }
        }

        public static void DrawVectorSaveDisk(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (Pen p = new Pen(color, size * 0.12f))
            using (SolidBrush b = new SolidBrush(color))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;

                g.DrawRectangle(p, x + 2, y + 2, size - 4, size - 4);
                g.FillRectangle(b, x + size * 0.28f, y + 3, size * 0.44f, size * 0.32f);
                g.DrawRectangle(p, x + size * 0.24f, y + size * 0.54f, size * 0.52f, size * 0.34f);
            }
        }

        public static void DrawVectorRocket(Graphics g, float x, float y, float size, Color color)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (Pen p = new Pen(color, size * 0.12f))
            using (SolidBrush b = new SolidBrush(color))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;

                PointF[] pts = new PointF[] {
                    new PointF(x + size * 0.85f, y + size * 0.15f),
                    new PointF(x + size * 0.45f, y + size * 0.22f),
                    new PointF(x + size * 0.22f, y + size * 0.45f),
                    new PointF(x + size * 0.15f, y + size * 0.85f),
                    new PointF(x + size * 0.55f, y + size * 0.78f),
                    new PointF(x + size * 0.78f, y + size * 0.55f)
                };
                g.DrawPolygon(p, pts);
                g.FillEllipse(b, x + size * 0.52f, y + size * 0.32f, size * 0.2f, size * 0.2f);
            }
        }

        public static void DrawVectorSend(Graphics g, float x, float y, float size, Color color)
        {
            DrawVectorLucideSend(g, x, y, size, color);
        }

        public static void DrawVectorLucideSend(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.8f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                PointF[] pts = new PointF[]
                {
                    new PointF(x + 14.536f * scale, y + 21.686f * scale),
                    new PointF(x + 21.973f * scale, y + 2.662f * scale),
                    new PointF(x + 2.338f * scale, y + 8.527f * scale),
                    new PointF(x + 10.244f * scale, y + 12.644f * scale),
                    new PointF(x + 11.356f * scale, y + 13.754f * scale)
                };
                g.DrawPolygon(pen, pts);
                g.DrawLine(pen,
                    new PointF(x + 21.854f * scale, y + 2.147f * scale),
                    new PointF(x + 10.914f * scale, y + 13.086f * scale));
            }
        }

        public static void DrawVectorCog(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.8f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                float r2 = 2f * scale;
                g.DrawEllipse(pen, x + 12f * scale - r2, y + 12f * scale - r2, r2 * 2f, r2 * 2f);

                float r8 = 8f * scale;
                g.DrawEllipse(pen, x + 12f * scale - r8, y + 12f * scale - r8, r8 * 2f, r8 * 2f);

                g.DrawLine(pen, new PointF(x + 11f * scale, y + 10.27f * scale), new PointF(x + 7f * scale, y + 3.34f * scale));
                g.DrawLine(pen, new PointF(x + 11f * scale, y + 13.73f * scale), new PointF(x + 7f * scale, y + 20.66f * scale));
                g.DrawLine(pen, new PointF(x + 12f * scale, y + 22f * scale), new PointF(x + 12f * scale, y + 20f * scale));
                g.DrawLine(pen, new PointF(x + 12f * scale, y + 2f * scale), new PointF(x + 12f * scale, y + 4f * scale));
                g.DrawLine(pen, new PointF(x + 14f * scale, y + 12f * scale), new PointF(x + 22f * scale, y + 12f * scale));
                g.DrawLine(pen, new PointF(x + 17f * scale, y + 20.66f * scale), new PointF(x + 16f * scale, y + 18.93f * scale));
                g.DrawLine(pen, new PointF(x + 17f * scale, y + 3.34f * scale), new PointF(x + 16f * scale, y + 5.07f * scale));
                g.DrawLine(pen, new PointF(x + 2f * scale, y + 12f * scale), new PointF(x + 4f * scale, y + 12f * scale));
                g.DrawLine(pen, new PointF(x + 20.66f * scale, y + 17f * scale), new PointF(x + 18.93f * scale, y + 16f * scale));
                g.DrawLine(pen, new PointF(x + 20.66f * scale, y + 7f * scale), new PointF(x + 18.93f * scale, y + 8f * scale));
                g.DrawLine(pen, new PointF(x + 3.34f * scale, y + 17f * scale), new PointF(x + 5.07f * scale, y + 16f * scale));
                g.DrawLine(pen, new PointF(x + 3.34f * scale, y + 7f * scale), new PointF(x + 5.07f * scale, y + 8f * scale));
            }
        }

        public static void DrawVectorReply(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.6f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                // Mũi tên quay lại ↩
                g.DrawLine(pen, new PointF(x + 9f * scale, y + 14f * scale), new PointF(x + 4f * scale, y + 9f * scale));
                g.DrawLine(pen, new PointF(x + 4f * scale, y + 9f * scale), new PointF(x + 9f * scale, y + 4f * scale));
                
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(x + 4f * scale, y + 9f * scale, 14f * scale, 12f * scale, 180, 90);
                    path.AddLine(x + 11f * scale, y + 9f * scale, x + 14f * scale, y + 9f * scale);
                    path.AddArc(x + 8f * scale, y + 9f * scale, 12f * scale, 12f * scale, 270, 90);
                    path.AddLine(x + 20f * scale, y + 15f * scale, x + 20f * scale, y + 19f * scale);
                    g.DrawPath(pen, path);
                }
            }
        }

        public static void DrawVectorCopy(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.5f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                // Hộp phía sau
                RectangleF backR = new RectangleF(x + 8f * scale, y + 3f * scale, 12f * scale, 13f * scale);
                using (GraphicsPath pBack = CreateRoundedPath(backR, 2.5f * scale))
                {
                    g.DrawPath(pen, pBack);
                }

                // Hộp phía trước
                RectangleF frontR = new RectangleF(x + 4f * scale, y + 7f * scale, 12f * scale, 13f * scale);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (GraphicsPath pFront = CreateRoundedPath(frontR, 2.5f * scale))
                {
                    g.FillPath(sb, pFront);
                    g.DrawPath(pen, pFront);
                }
            }
        }

        public static void DrawVectorCheck(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.8f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLine(pen, new PointF(x + 4f * scale, y + 12f * scale), new PointF(x + 9f * scale, y + 17f * scale));
                g.DrawLine(pen, new PointF(x + 9f * scale, y + 17f * scale), new PointF(x + 20f * scale, y + 6f * scale));
            }
        }

        public static void DrawVectorHistory(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.8f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                // Vòng tròn đồng hồ lịch sử với mũi tên ngược
                g.DrawArc(pen, x + 3f * scale, y + 3f * scale, 18f * scale, 18f * scale, 130, 315);
                
                // Mũi tên ở đầu cung
                g.DrawLine(pen, new PointF(x + 3.5f * scale, y + 9f * scale), new PointF(x + 3f * scale, y + 3.5f * scale));
                g.DrawLine(pen, new PointF(x + 3f * scale, y + 3.5f * scale), new PointF(x + 8.5f * scale, y + 3.5f * scale));

                // Kim đồng hồ (12h và 3h)
                g.DrawLine(pen, new PointF(x + 12f * scale, y + 12f * scale), new PointF(x + 12f * scale, y + 7f * scale));
                g.DrawLine(pen, new PointF(x + 12f * scale, y + 12f * scale), new PointF(x + 16f * scale, y + 12f * scale));
            }
        }

        public static void DrawVectorPlus(Graphics g, float x, float y, float size, Color color, float strokeWidth = 2.0f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen, new PointF(x + 12f * scale, y + 5f * scale), new PointF(x + 12f * scale, y + 19f * scale));
                g.DrawLine(pen, new PointF(x + 5f * scale, y + 12f * scale), new PointF(x + 19f * scale, y + 12f * scale));
            }
        }

        public static void DrawVectorTrash(Graphics g, float x, float y, float size, Color color, float strokeWidth = 1.6f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = size / 24f;
            float penW = Math.Max(1.0f, strokeWidth * scale);

            using (Pen pen = new Pen(color, penW))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                // Nắp thùng rác
                g.DrawLine(pen, new PointF(x + 3f * scale, y + 6f * scale), new PointF(x + 21f * scale, y + 6f * scale));
                g.DrawLine(pen, new PointF(x + 8f * scale, y + 6f * scale), new PointF(x + 10f * scale, y + 3f * scale));
                g.DrawLine(pen, new PointF(x + 10f * scale, y + 3f * scale), new PointF(x + 14f * scale, y + 3f * scale));
                g.DrawLine(pen, new PointF(x + 14f * scale, y + 3f * scale), new PointF(x + 16f * scale, y + 6f * scale));

                // Thân thùng rác
                PointF[] body = new PointF[]
                {
                    new PointF(x + 5f * scale, y + 6f * scale),
                    new PointF(x + 6.5f * scale, y + 20f * scale),
                    new PointF(x + 17.5f * scale, y + 20f * scale),
                    new PointF(x + 19f * scale, y + 6f * scale)
                };
                g.DrawLines(pen, body);

                // Các đường gân bên trong
                g.DrawLine(pen, new PointF(x + 10f * scale, y + 10f * scale), new PointF(x + 10f * scale, y + 16f * scale));
                g.DrawLine(pen, new PointF(x + 14f * scale, y + 10f * scale), new PointF(x + 14f * scale, y + 16f * scale));
            }
        }
        #endregion
    }

    /// <summary>
    /// Nút bấm Liquid Glass màu trắng trong suốt cao cấp như thanh Tab.
    /// </summary>
    public class GlassVectorButton : Button
    {
        public Action<Graphics, float, float, float, Color> VectorIconDrawer { get; set; }
        public Color AccentColor { get; set; }
        public Color TextCustomColor { get; set; }
        public bool IsPrimary { get; set; }

        private bool _isHovered = false;
        private bool _isPressed = false;

        public GlassVectorButton()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.FlatAppearance.MouseDownBackColor = Color.Transparent;
            this.FlatAppearance.MouseOverBackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.AccentColor = Color.FromArgb(99, 102, 241);
            this.TextCustomColor = Color.FromArgb(30, 41, 59);
            this.IsPrimary = false;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (this.Width <= 0 || this.Height <= 0) return;
            using (GraphicsPath path = AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(0, 0, this.Width, this.Height), this.Height / 2f))
            {
                if (this.Region != null) this.Region.Dispose();
                this.Region = new Region(path);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            RectangleF bounds = new RectangleF(0f, 0f, this.Width, this.Height);
            AiVectorGlassRenderer.DrawGlassButton(pevent.Graphics, bounds, this.Text, this.VectorIconDrawer, _isHovered, _isPressed, this.AccentColor, this.TextCustomColor, this.IsPrimary);
        }
    }

    /// <summary>
    /// Thanh trượt Liquid Glass hỗ trợ độ trong suốt hoàn toàn 100% không lỗi WinForms.
    /// </summary>
    public class GlassSlider : Control
    {
        private float _value = 0.7f;
        private bool _isDragging = false;

        public event EventHandler ValueChanged;

        public float Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0.0f, Math.Min(2.0f, value));
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        public GlassSlider()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.Height = 28;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isDragging = true;
            UpdateValueFromMouse(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                UpdateValueFromMouse(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        private void UpdateValueFromMouse(int mouseX)
        {
            float trackStart = 10f;
            float trackEnd = this.Width - 10f;
            float ratio = (mouseX - trackStart) / (trackEnd - trackStart);
            ratio = Math.Max(0f, Math.Min(1f, ratio));
            this.Value = (float)Math.Round(ratio * 2.0f, 1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            float trackH = 6f;
            float trackY = (this.Height - trackH) / 2f;
            float trackStart = 10f;
            float trackEnd = this.Width - 10f;
            float trackW = trackEnd - trackStart;

            // 1. Background Track
            RectangleF trackRect = new RectangleF(trackStart, trackY, trackW, trackH);
            using (GraphicsPath tp = AiVectorGlassRenderer.CreateRoundedPath(trackRect, trackH / 2f))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(160, 203, 213, 225)))
            {
                g.FillPath(tb, tp);
            }

            // 2. Active Fill Track
            float ratio = _value / 2.0f;
            float fillW = trackW * ratio;
            if (fillW > 0)
            {
                RectangleF fillRect = new RectangleF(trackStart, trackY, fillW, trackH);
                using (GraphicsPath fp = AiVectorGlassRenderer.CreateRoundedPath(fillRect, trackH / 2f))
                using (LinearGradientBrush fgb = new LinearGradientBrush(fillRect, Color.FromArgb(99, 102, 241), Color.FromArgb(79, 70, 229), LinearGradientMode.Horizontal))
                {
                    g.FillPath(fgb, fp);
                }
            }

            // 3. Thumb Knob (Glass Pill)
            float thumbX = trackStart + fillW;
            float thumbSize = 18f;
            RectangleF thumbRect = new RectangleF(thumbX - thumbSize / 2f, (this.Height - thumbSize) / 2f, thumbSize, thumbSize);
            using (GraphicsPath thumbPath = AiVectorGlassRenderer.CreateRoundedPath(thumbRect, thumbSize / 2f))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(25, 0, 0, 0)))
                {
                    g.FillEllipse(sb, thumbRect.X, thumbRect.Y + 1.5f, thumbSize, thumbSize);
                }
                using (SolidBrush bb = new SolidBrush(Color.White))
                {
                    g.FillPath(bb, thumbPath);
                }
                using (Pen bp = new Pen(Color.FromArgb(99, 102, 241), 2.5f))
                {
                    g.DrawPath(bp, thumbPath);
                }
            }
        }
    }

    /// <summary>
    /// Ô nhập liệu Liquid Glass trong suốt cao cấp không viền đục, không hộp vuông trắng, có viền trắng khúc xạ tán xạ và Chữ ảo (Placeholder).
    /// </summary>
    public class GlassTextBox : Control
    {
        private TextBox _innerTextBox;
        private string _placeholderText = "";
        private bool _isHovered = false;
        private bool _isFocused = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        public static readonly Color GlassBaseColor = Color.FromArgb(246, 249, 254);

        public GlassTextBox()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Font = new Font("Segoe UI", 10f);

            _innerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = GlassBaseColor,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = this.Font
            };

            _innerTextBox.MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            _innerTextBox.MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            _innerTextBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _innerTextBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _innerTextBox.TextChanged += (s, e) =>
            {
                this.OnTextChanged(EventArgs.Empty);
                Invalidate();
            };
            _innerTextBox.HandleCreated += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_placeholderText))
                {
                    SendMessage(_innerTextBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholderText);
                }
            };

            this.Controls.Add(_innerTextBox);
            UpdateInnerLayout();
        }

        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                _placeholderText = value ?? "";
                if (_innerTextBox.IsHandleCreated)
                {
                    SendMessage(_innerTextBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholderText);
                }
                Invalidate();
            }
        }

        public override string Text
        {
            get { return _innerTextBox.Text; }
            set { _innerTextBox.Text = value; Invalidate(); }
        }

        public bool UseSystemPasswordChar
        {
            get { return _innerTextBox.UseSystemPasswordChar; }
            set { _innerTextBox.UseSystemPasswordChar = value; }
        }

        public bool Multiline
        {
            get { return _innerTextBox.Multiline; }
            set
            {
                _innerTextBox.Multiline = value;
                UpdateInnerLayout();
            }
        }

        public ScrollBars ScrollBars
        {
            get { return _innerTextBox.ScrollBars; }
            set { _innerTextBox.ScrollBars = value; }
        }

        public TextBox InnerControl
        {
            get { return _innerTextBox; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!string.IsNullOrEmpty(_placeholderText))
            {
                SendMessage(_innerTextBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholderText);
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_innerTextBox != null)
            {
                _innerTextBox.Font = this.Font;
                UpdateInnerLayout();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateInnerLayout();
        }

        private void UpdateInnerLayout()
        {
            if (_innerTextBox == null) return;
            if (_innerTextBox.Multiline)
            {
                _innerTextBox.Location = new Point(14, 10);
                _innerTextBox.Size = new Size(Math.Max(10, this.Width - 28), Math.Max(10, this.Height - 20));
            }
            else
            {
                int tbH = _innerTextBox.PreferredHeight;
                int tbY = (this.Height - tbH) / 2;
                _innerTextBox.Location = new Point(14, Math.Max(2, tbY));
                _innerTextBox.Size = new Size(Math.Max(10, this.Width - 28), tbH);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _innerTextBox.Focus();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float r = _innerTextBox.Multiline ? 14f : Math.Min(14f, this.Height / 2f);
            RectangleF rect = new RectangleF(1f, 1f, this.Width - 2f, this.Height - 2f);

            using (GraphicsPath path = AiVectorGlassRenderer.CreateRoundedPath(rect, r))
            {
                // A. Drop Shadow
                RectangleF shadowRect = new RectangleF(rect.X, rect.Y + (_isFocused ? 2f : 1f), rect.Width, rect.Height);
                using (GraphicsPath sp = AiVectorGlassRenderer.CreateRoundedPath(shadowRect, r))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(_isFocused ? 22 : (_isHovered ? 16 : 10), 15, 23, 42)))
                {
                    g.FillPath(sb, sp);
                }

                // B. Translucent Frosted Glass Base Fill
                using (SolidBrush baseB = new SolidBrush(GlassBaseColor))
                {
                    g.FillPath(baseB, path);
                }

                // C. Glass Sheen Gradient
                Color topGlass = Color.FromArgb(_isFocused ? 140 : (_isHovered ? 110 : 80), 255, 255, 255);
                Color botGlass = Color.FromArgb(_isFocused ? 60 : (_isHovered ? 30 : 10), 220, 230, 250);
                using (LinearGradientBrush glassBrush = new LinearGradientBrush(rect, topGlass, botGlass, 90f))
                {
                    g.FillPath(glassBrush, path);
                }

                // D. Viền trắng tán xạ vô trong như kính (Inner White Scattering Glow)
                using (Pen inGlow = new Pen(Color.FromArgb(_isFocused ? 210 : (_isHovered ? 160 : 110), 255, 255, 255), 2.2f))
                {
                    g.DrawPath(inGlow, path);
                }

                // E. Top Specular Gloss Highlight
                RectangleF glossRect = new RectangleF(rect.X + 2, rect.Y + 1, rect.Width - 4, Math.Min(16f, rect.Height * 0.45f));
                if (glossRect.Height > 4)
                {
                    using (GraphicsPath glossPath = AiVectorGlassRenderer.CreateRoundedPath(glossRect, r - 1))
                    using (LinearGradientBrush glossBrush = new LinearGradientBrush(glossRect, Color.FromArgb(_isFocused ? 150 : 100, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                    {
                        g.FillPath(glossBrush, glossPath);
                    }
                }

                // F. Outer Border
                Color borderCol = _isFocused ? Color.FromArgb(220, 99, 102, 241) : (_isHovered ? Color.FromArgb(180, 147, 197, 253) : Color.FromArgb(180, 255, 255, 255));
                float borderW = _isFocused ? 1.6f : 1.0f;
                using (Pen bp = new Pen(borderCol, borderW))
                {
                    g.DrawPath(bp, path);
                }
            }
        }
    }

    /// <summary>
    /// Dropdown ComboBox Liquid Glass trong suốt hoàn toàn, không viền đục vuông, có mũi tên Chevron vector cao cấp.
    /// </summary>
    public class GlassComboInput : Control
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);
        private const int EM_SETCUEBANNER = 0x1501;

        private List<string> _items = new List<string>();
        private int _selectedIndex = -1;
        private TextBox _editBox = null;
        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDropDownOpen = false;
        private ComboBoxStyle _dropDownStyle = ComboBoxStyle.DropDownList;
        private string _placeholderText = "";

        public event EventHandler SelectedIndexChanged;

        public GlassComboInput()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Font = new Font("Segoe UI", 9.8f);
            this.Cursor = Cursors.Hand;

            _editBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = GlassTextBox.GlassBaseColor,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = this.Font,
                Visible = false
            };
            _editBox.TextChanged += (s, e) =>
            {
                this.OnTextChanged(EventArgs.Empty);
                Invalidate();
            };
            _editBox.HandleCreated += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_placeholderText))
                {
                    SendMessage(_editBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholderText);
                }
            };
            _editBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _editBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _editBox.KeyDown += (s, e) => { this.OnKeyDown(e); };
            _editBox.KeyPress += (s, e) => { this.OnKeyPress(e); };
            _editBox.KeyUp += (s, e) => { this.OnKeyUp(e); };
            this.Controls.Add(_editBox);
            UpdateEditBoxLayout();
        }

        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                _placeholderText = value ?? "";
                if (_editBox != null && _editBox.IsHandleCreated)
                {
                    SendMessage(_editBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholderText);
                }
                Invalidate();
            }
        }

        public List<string> Items
        {
            get { return _items; }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                {
                    if (_editBox != null && _dropDownStyle == ComboBoxStyle.DropDown)
                    {
                        _editBox.Text = _items[_selectedIndex];
                    }
                }
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public object SelectedItem
        {
            get
            {
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count) return _items[_selectedIndex];
                return null;
            }
            set
            {
                if (value == null) { SelectedIndex = -1; return; }
                int idx = _items.IndexOf(value.ToString());
                if (idx >= 0) SelectedIndex = idx;
            }
        }

        public ComboBoxStyle DropDownStyle
        {
            get { return _dropDownStyle; }
            set
            {
                _dropDownStyle = value;
                if (_editBox != null)
                {
                    _editBox.Visible = (_dropDownStyle == ComboBoxStyle.DropDown);
                }
                UpdateEditBoxLayout();
                Invalidate();
            }
        }

        public override string Text
        {
            get
            {
                if (_dropDownStyle == ComboBoxStyle.DropDown && _editBox != null && _editBox.Visible)
                {
                    return _editBox.Text;
                }
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                {
                    return _items[_selectedIndex];
                }
                return base.Text;
            }
            set
            {
                base.Text = value;
                if (_editBox != null && _dropDownStyle == ComboBoxStyle.DropDown)
                {
                    _editBox.Text = value;
                }
                else
                {
                    int idx = _items.IndexOf(value);
                    if (idx >= 0) SelectedIndex = idx;
                }
                Invalidate();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateEditBoxLayout();
        }

        private void UpdateEditBoxLayout()
        {
            if (_editBox == null) return;
            int tbH = _editBox.PreferredHeight;
            int tbY = (this.Height - tbH) / 2;
            _editBox.Location = new Point(14, Math.Max(2, tbY));
            _editBox.Size = new Size(Math.Max(10, this.Width - 48), tbH);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                ShowDropDownMenu();
            }
        }

        private void ShowDropDownMenu()
        {
            if (_items.Count == 0) return;

            _isDropDownOpen = true;
            Invalidate();

            GlassDropDownPopup popup = new GlassDropDownPopup(_items, _selectedIndex, this.Font, this.Width);
            Point screenPt = this.PointToScreen(new Point(0, this.Height + 3));
            Rectangle screenRect = Screen.FromPoint(screenPt).WorkingArea;
            if (screenPt.X + popup.Width > screenRect.Right)
            {
                screenPt.X = screenRect.Right - popup.Width - 5;
            }
            if (screenPt.Y + popup.Height > screenRect.Bottom)
            {
                screenPt.Y = screenPt.Y - popup.Height - this.Height - 6;
            }
            popup.Location = screenPt;
            popup.ItemSelected += (s, args) =>
            {
                this.SelectedIndex = args.Index;
            };
            popup.FormClosed += (s, e) =>
            {
                _isDropDownOpen = false;
                Invalidate();
            };
            popup.Show();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float r = Math.Min(14f, this.Height / 2f);
            RectangleF rect = new RectangleF(1f, 1f, this.Width - 2f, this.Height - 2f);

            using (GraphicsPath path = AiVectorGlassRenderer.CreateRoundedPath(rect, r))
            {
                // A. Drop Shadow
                RectangleF shadowRect = new RectangleF(rect.X, rect.Y + (_isFocused || _isDropDownOpen ? 2f : 1f), rect.Width, rect.Height);
                using (GraphicsPath sp = AiVectorGlassRenderer.CreateRoundedPath(shadowRect, r))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(_isFocused || _isDropDownOpen ? 22 : (_isHovered ? 16 : 10), 15, 23, 42)))
                {
                    g.FillPath(sb, sp);
                }

                // B. Translucent Frosted Glass Base Fill
                using (SolidBrush baseB = new SolidBrush(GlassTextBox.GlassBaseColor))
                {
                    g.FillPath(baseB, path);
                }

                // C. Glass Sheen Gradient
                Color topGlass = Color.FromArgb(_isFocused || _isDropDownOpen ? 140 : (_isHovered ? 110 : 80), 255, 255, 255);
                Color botGlass = Color.FromArgb(_isFocused || _isDropDownOpen ? 60 : (_isHovered ? 30 : 10), 220, 230, 250);
                using (LinearGradientBrush glassBrush = new LinearGradientBrush(rect, topGlass, botGlass, 90f))
                {
                    g.FillPath(glassBrush, path);
                }

                // D. Viền trắng tán xạ vô trong như kính (Inner White Scattering Glow)
                using (Pen inGlow = new Pen(Color.FromArgb(_isFocused || _isDropDownOpen ? 210 : (_isHovered ? 160 : 110), 255, 255, 255), 2.2f))
                {
                    g.DrawPath(inGlow, path);
                }

                // E. Top Specular Gloss Highlight
                RectangleF glossRect = new RectangleF(rect.X + 2, rect.Y + 1, rect.Width - 4, Math.Min(16f, rect.Height * 0.45f));
                if (glossRect.Height > 4)
                {
                    using (GraphicsPath glossPath = AiVectorGlassRenderer.CreateRoundedPath(glossRect, r - 1))
                    using (LinearGradientBrush glossBrush = new LinearGradientBrush(glossRect, Color.FromArgb(_isFocused || _isDropDownOpen ? 150 : 100, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                    {
                        g.FillPath(glossBrush, glossPath);
                    }
                }

                // F. Outer Border
                Color borderCol = (_isFocused || _isDropDownOpen) ? Color.FromArgb(220, 99, 102, 241) : (_isHovered ? Color.FromArgb(180, 147, 197, 253) : Color.FromArgb(180, 255, 255, 255));
                float borderW = (_isFocused || _isDropDownOpen) ? 1.6f : 1.0f;
                using (Pen bp = new Pen(borderCol, borderW))
                {
                    g.DrawPath(bp, path);
                }
            }

            // G. Draw Display Text (if DropDownList or DropDown with empty text)
            if (_dropDownStyle == ComboBoxStyle.DropDownList)
            {
                string displayText = (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : "";
                RectangleF textBounds = new RectangleF(14f, 0f, this.Width - 44f, this.Height);
                if (!string.IsNullOrEmpty(displayText))
                {
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(displayText, this.Font, tb, textBounds, sf);
                    }
                }
                else if (!string.IsNullOrEmpty(this.PlaceholderText))
                {
                    using (Font phFont = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Italic))
                    using (SolidBrush phBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(this.PlaceholderText, phFont, phBrush, textBounds, sf);
                    }
                }
            }
            else if (_dropDownStyle == ComboBoxStyle.DropDown)
            {
                if (_editBox != null && string.IsNullOrEmpty(_editBox.Text) && !_isFocused && !string.IsNullOrEmpty(this.PlaceholderText))
                {
                    RectangleF textBounds = new RectangleF(14f, 0f, this.Width - 44f, this.Height);
                    using (Font phFont = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Italic))
                    using (SolidBrush phBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(this.PlaceholderText, phFont, phBrush, textBounds, sf);
                    }
                }
            }

            // H. Draw Vector Chevron Down Arrow
            float arrowCenterX = this.Width - 20f;
            float arrowCenterY = this.Height / 2f;
            Color arrowCol = _isHovered || _isDropDownOpen ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139);
            using (Pen ap = new Pen(arrowCol, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(ap, arrowCenterX - 4f, arrowCenterY - 2f, arrowCenterX, arrowCenterY + 2f);
                g.DrawLine(ap, arrowCenterX, arrowCenterY + 2f, arrowCenterX + 4f, arrowCenterY - 2f);
            }
        }
    }

    /// <summary>
    /// Sự kiện trả về chỉ mục được chọn.
    /// </summary>
    public class IndexEventArgs : EventArgs
    {
        public int Index { get; private set; }
        public IndexEventArgs(int index) { Index = index; }
    }

    /// <summary>
    /// Popup bảng chọn Liquid Glass cao cấp thay thế ContextMenuStrip mặc định.
    /// Bo tròn, nền kính mờ, hover indigo, checkmark cho item đang chọn.
    /// </summary>
    public class GlassDropDownPopup : Form
    {
        private List<string> _items;
        private int _selectedIndex;
        private int _hoverIndex = -1;
        private Font _itemFont;
        private Font _itemFontBold;
        private int _itemHeight = 36;
        private int _cornerRadius = 12;
        private int _padV = 6;

        public event EventHandler<IndexEventArgs> ItemSelected;

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x, int y, int w, int h, int cx, int cy);

        public GlassDropDownPopup(List<string> items, int selectedIndex, Font baseFont, int minWidth)
        {
            _items = items;
            _selectedIndex = selectedIndex;
            _itemFont = new Font("Segoe UI", 9.8f);
            _itemFontBold = new Font("Segoe UI", 9.8f, FontStyle.Bold);

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;

            int contentW = Math.Max(minWidth, 200);
            using (Graphics gm = Graphics.FromHwnd(IntPtr.Zero))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    SizeF sz = gm.MeasureString(_items[i], _itemFont);
                    contentW = Math.Max(contentW, (int)sz.Width + 60);
                }
            }
            int contentH = _items.Count * _itemHeight + _padV * 2;
            this.Size = new Size(contentW, contentH);

            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Replaced SetWindowRgn with TransparencyKey for High DPI support
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Close();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int newHover = GetItemIndexAt(e.Y);
            if (newHover != _hoverIndex)
            {
                _hoverIndex = newHover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int idx = GetItemIndexAt(e.Y);
            if (idx >= 0 && idx < _items.Count)
            {
                if (ItemSelected != null) ItemSelected(this, new IndexEventArgs(idx));
                this.Close();
            }
        }

        private int GetItemIndexAt(int y)
        {
            int idx = (y - _padV) / _itemHeight;
            if (idx < 0 || idx >= _items.Count) return -1;
            return idx;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF bounds = new RectangleF(0, 0, this.Width, this.Height);
            float r = _cornerRadius;

            using (GraphicsPath path = AiVectorGlassRenderer.CreateRoundedPath(bounds, r))
            {
                // A. Opaque frosted glass base
                using (SolidBrush baseBrush = new SolidBrush(Color.FromArgb(248, 250, 255)))
                {
                    g.FillPath(baseBrush, path);
                }

                // B. Glass sheen gradient
                using (LinearGradientBrush glassBrush = new LinearGradientBrush(bounds,
                    Color.FromArgb(120, 255, 255, 255), Color.FromArgb(30, 220, 232, 250), 90f))
                {
                    g.FillPath(glassBrush, path);
                }

                // C. Top specular highlight
                RectangleF glossRect = new RectangleF(2, 1, this.Width - 4, Math.Min(20f, this.Height * 0.15f));
                if (glossRect.Height > 4)
                {
                    using (GraphicsPath gp = AiVectorGlassRenderer.CreateRoundedPath(glossRect, r - 1))
                    using (LinearGradientBrush gb = new LinearGradientBrush(glossRect,
                        Color.FromArgb(130, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                    {
                        g.FillPath(gb, gp);
                    }
                }
            }

            // D. Draw items
            for (int i = 0; i < _items.Count; i++)
            {
                int itemY = _padV + i * _itemHeight;
                RectangleF itemRect = new RectangleF(_padV, itemY, this.Width - _padV * 2, _itemHeight);
                bool isSelected = (i == _selectedIndex);
                bool isHovered = (i == _hoverIndex);

                if (isHovered)
                {
                    using (GraphicsPath hp = AiVectorGlassRenderer.CreateRoundedPath(itemRect, 8f))
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(40, 99, 102, 241)))
                    {
                        g.FillPath(hb, hp);
                    }
                }

                if (isSelected && !isHovered)
                {
                    using (GraphicsPath sp = AiVectorGlassRenderer.CreateRoundedPath(itemRect, 8f))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(18, 99, 102, 241)))
                    {
                        g.FillPath(sb, sp);
                    }
                }

                string displayText = _items[i];
                bool isBlank = string.IsNullOrEmpty(displayText);
                if (isBlank) displayText = "<Chưa đặt tên>";

                Font f = isSelected ? _itemFontBold : _itemFont;
                if (isBlank) f = new Font(_itemFont.FontFamily, _itemFont.Size, FontStyle.Italic);
                
                Color textCol = isSelected ? Color.FromArgb(79, 70, 229)
                    : (isHovered ? Color.FromArgb(55, 48, 163) : (isBlank ? Color.FromArgb(148, 163, 184) : Color.FromArgb(30, 41, 59)));

                RectangleF textRect = new RectangleF(itemRect.X + 14, itemRect.Y, itemRect.Width - 40, itemRect.Height);
                using (SolidBrush tb = new SolidBrush(textCol))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(displayText, f, tb, textRect, sf);
                }
                if (isBlank) f.Dispose();

                if (isSelected)
                {
                    float checkX = itemRect.Right - 26;
                    float checkY = itemRect.Y + (itemRect.Height - 12) / 2f;
                    using (Pen cp = new Pen(Color.FromArgb(79, 70, 229), 2.2f))
                    {
                        cp.StartCap = LineCap.Round;
                        cp.EndCap = LineCap.Round;
                        g.DrawLine(cp, checkX, checkY + 6, checkX + 4, checkY + 10);
                        g.DrawLine(cp, checkX + 4, checkY + 10, checkX + 11, checkY + 2);
                    }
                }
            }

            // E. Outer border
            using (GraphicsPath bp = AiVectorGlassRenderer.CreateRoundedPath(
                new RectangleF(0.5f, 0.5f, this.Width - 1f, this.Height - 1f), r))
            {
                using (Pen ig = new Pen(Color.FromArgb(140, 255, 255, 255), 2f))
                {
                    g.DrawPath(ig, bp);
                }
                using (Pen ob = new Pen(Color.FromArgb(60, 99, 102, 241), 1f))
                {
                    g.DrawPath(ob, bp);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_itemFont != null) _itemFont.Dispose();
                if (_itemFontBold != null) _itemFontBold.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

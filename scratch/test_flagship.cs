using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace GenOffice.Test
{
    class FlagshipLogoStudio
    {
        static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0) { path.AddRectangle(rect); return path; }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // =========================================================================================
        // DESIGN D: THE "MÖBIUS TRIAD 'G'" (Flagship 3D Fluid Ribbon - Ultra Clean & Iconic)
        // 3 seamless dynamic curved ribbons forming a continuous modern 'G' with smooth gradient flow
        // =========================================================================================
        public static void DrawMobiusG(Graphics g, float x, float y, float size, bool drawBackdrop)
        {
            float s = size / 100f;
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                using (GraphicsPath bp = CreateRoundedRect(bRect, size * 0.24f))
                {
                    // Luxury Deep Space Glass (#0A0E1A -> #1A2338)
                    using (LinearGradientBrush bg = new LinearGradientBrush(bRect, Color.FromArgb(10, 14, 26), Color.FromArgb(26, 35, 56), 60f))
                    {
                        g.FillPath(bg, bp);
                    }
                    // Glass inner highlight
                    using (Pen p = new Pen(Color.FromArgb(40, 255, 255, 255), 1.2f * s)) g.DrawPath(p, bp);
                }
            }

            // G Ribbon parameters
            float outerR = 34f * s;
            float thickness = 13f * s;
            float innerR = outerR - thickness; // 21s
            RectangleF outRect = new RectangleF(cx - outerR, cy - outerR, outerR * 2, outerR * 2);
            RectangleF inRect = new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2);

            // =========================================================================================
            // 1. GREEN RIBBON (CALC - Bảng tính): Cung đáy uốn từ sống lưng (160°) vòng qua đáy sang phải (20°)
            // =========================================================================================
            using (GraphicsPath greenP = new GraphicsPath())
            {
                // Sweeps around bottom from 160 deg to 30 deg
                greenP.AddArc(outRect, 30, 130); // 30 to 160
                // Inner curve
                greenP.AddArc(inRect, 160, -130);
                greenP.CloseFigure();

                using (LinearGradientBrush gBr = new LinearGradientBrush(
                    new RectangleF(cx - outerR, cy, outerR * 2, outerR),
                    Color.FromArgb(52, 211, 153), // Emerald 400
                    Color.FromArgb(5, 150, 105),   // Emerald 600
                    135f))
                {
                    g.FillPath(gBr, greenP);
                }
            }

            // =========================================================================================
            // 2. ORANGE WING & SPUR (SLIDES - Trình chiếu): Vòng cung phải uốn lên và đâm thẳng vào tâm chữ G
            // =========================================================================================
            using (GraphicsPath orangeP = new GraphicsPath())
            {
                // Right wall starting from 30 deg up to 330 deg (-30)
                orangeP.AddArc(outRect, 330, 65); // 330 to 35 (overlap with green)
                
                // Then straight horizontal crossbar inward from (cx + outerR, cy) to (cx, cy)
                float spurTop = cy - 6.5f * s;
                float spurBottom = cy + 6.5f * s;
                float spurLeft = cx - 2f * s;
                float spurRight = cx + outerR;

                orangeP.AddLine(cx + outerR, cy, cx + outerR, spurTop);
                // Rounded tip of the spur in center
                orangeP.AddArc(spurLeft, spurTop, 13f * s, 13f * s, 270, -180);
                orangeP.AddLine(spurLeft + 6.5f * s, spurBottom, cx + outerR - thickness, spurBottom);
                orangeP.AddArc(inRect, 15, 30);
                orangeP.CloseFigure();

                // Drop shadow beneath orange spur
                using (GraphicsPath oShadow = (GraphicsPath)orangeP.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(-2 * s, 2 * s); oShadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(65, 0, 0, 0))) g.FillPath(sb, oShadow);
                }

                using (LinearGradientBrush oBr = new LinearGradientBrush(
                    new RectangleF(cx, cy - outerR, outerR, outerR * 2),
                    Color.FromArgb(251, 146, 60), // Orange 400
                    Color.FromArgb(220, 38, 38),   // Red-Orange 600
                    45f))
                {
                    g.FillPath(oBr, orangeP);
                }
            }

            // =========================================================================================
            // 3. BLUE RIBBON (WRITER - Soạn thảo): Vòng cung đỉnh chữ G uốn từ góc trên phải (330°) sang trái (160°)
            // =========================================================================
            using (GraphicsPath blueP = new GraphicsPath())
            {
                // Upper arch from 160 deg around top to 330 deg (-30 deg)
                blueP.AddArc(outRect, 160, 175); // 160 around to 335
                
                // Rounded terminal at top right (document corner fold silhouette)
                PointF pOut = new PointF(cx + outerR * (float)Math.Cos(335 * Math.PI / 180), cy + outerR * (float)Math.Sin(335 * Math.PI / 180));
                PointF pIn = new PointF(cx + innerR * (float)Math.Cos(335 * Math.PI / 180), cy + innerR * (float)Math.Sin(335 * Math.PI / 180));
                
                blueP.AddLine(pOut, pIn);
                blueP.AddArc(inRect, 335, -175);
                blueP.CloseFigure();

                // Drop shadow beneath blue ribbon casting over green & orange
                using (GraphicsPath bShadow = (GraphicsPath)blueP.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(0, 3f * s); bShadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(80, 0, 0, 0))) g.FillPath(sb, bShadow);
                }

                using (LinearGradientBrush bBr = new LinearGradientBrush(
                    new RectangleF(cx - outerR, cy - outerR, outerR * 2, outerR * 1.5f),
                    Color.FromArgb(96, 165, 250), // Blue 400
                    Color.FromArgb(29, 78, 216),  // Blue 700
                    45f))
                {
                    g.FillPath(bBr, blueP);
                }
            }

            // =========================================================================================
            // 4. ELEGANT MICRO-CREASES & SPECULAR HIGHLIGHTS (Chỉ báo chức năng tối giản, siêu sang)
            // =========================================================================================
            // A. Writer Folded Corner Specular Tip (Góc gấp giấy tinh xảo góc trên phải)
            using (GraphicsPath foldTip = new GraphicsPath())
            {
                foldTip.AddPolygon(new PointF[] {
                    new PointF(cx + 21 * s, cy - 29 * s),
                    new PointF(cx + 31 * s, cy - 19 * s),
                    new PointF(cx + 21 * s, cy - 19 * s)
                });
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(235, 245, 255)))
                {
                    g.FillPath(fb, foldTip);
                }
            }

            // B. Calc 3 Subtle Glass Bevels on Green curve (3 vạch phân tích số liệu kính)
            using (Pen gridPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.8f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(gridPen, cx - 18 * s, cy + 18 * s, cx - 18 * s, cy + 24 * s);
                g.DrawLine(gridPen, cx - 11 * s, cy + 21 * s, cx - 11 * s, cy + 29 * s);
                g.DrawLine(gridPen, cx - 4 * s, cy + 22 * s, cx - 4 * s, cy + 32 * s);
            }

            // C. Slides Play Spark on Orange Spur (Biểu tượng tam giác phát sáng ở tâm chữ G)
            PointF sparkCenter = new PointF(cx + 4.5f * s, cy);
            PointF[] playSpark = new PointF[] {
                new PointF(sparkCenter.X - 3.2f * s, sparkCenter.Y - 4.5f * s),
                new PointF(sparkCenter.X + 4.5f * s, sparkCenter.Y),
                new PointF(sparkCenter.X - 3.2f * s, sparkCenter.Y + 4.5f * s)
            };
            using (SolidBrush playB = new SolidBrush(Color.White))
            {
                g.FillPolygon(playB, playSpark);
            }
        }

        // =========================================================================================
        // DESIGN E: "THE 3-TIER ISOMETRIC GLASS SUITE STACK 'G'" (Apple VisionOS / Fluent 3D)
        // 3 floating rounded glass cards in dynamic isometric perspective forming the shape of 'G'
        // =========================================================================================
        public static void DrawGlassStackG(Graphics g, float x, float y, float size, bool drawBackdrop)
        {
            float s = size / 100f;
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                using (GraphicsPath bp = CreateRoundedRect(bRect, size * 0.24f))
                using (LinearGradientBrush bg = new LinearGradientBrush(bRect, Color.FromArgb(12, 16, 28), Color.FromArgb(28, 38, 62), 60f))
                {
                    g.FillPath(bg, bp);
                    using (Pen p = new Pen(Color.FromArgb(45, 255, 255, 255), 1.2f * s)) g.DrawPath(p, bp);
                }
            }

            // Card Dimensions: 38W x 38H rounded cards layered like an architectural origami 'G'
            // CARD 1: CALC (Green - Bottom Left: X: cx-34s, Y: cy-4s)
            RectangleF calcCard = new RectangleF(cx - 32 * s, cy - 2 * s, 36 * s, 36 * s);
            using (GraphicsPath cp = CreateRoundedRect(calcCard, 10 * s))
            {
                using (LinearGradientBrush cb = new LinearGradientBrush(calcCard, Color.FromArgb(16, 185, 129), Color.FromArgb(5, 150, 105), 45f))
                using (Pen cPen = new Pen(Color.FromArgb(167, 243, 208), 1.4f * s))
                {
                    g.FillPath(cb, cp);
                    g.DrawPath(cPen, cp);
                }
                // Mini 3-bar chart
                using (SolidBrush b1 = new SolidBrush(Color.FromArgb(209, 250, 229)))
                using (SolidBrush b2 = new SolidBrush(Color.FromArgb(236, 253, 245)))
                using (SolidBrush b3 = new SolidBrush(Color.White))
                {
                    g.FillRectangle(b1, calcCard.X + 8 * s, calcCard.Bottom - 14 * s, 4 * s, 6 * s);
                    g.FillRectangle(b2, calcCard.X + 15 * s, calcCard.Bottom - 19 * s, 4 * s, 11 * s);
                    g.FillRectangle(b3, calcCard.X + 22 * s, calcCard.Bottom - 24 * s, 4 * s, 16 * s);
                }
            }

            // CARD 2: SLIDES (Orange - Center Right: X: cx-4s, Y: cy-8s)
            RectangleF slidesCard = new RectangleF(cx - 4 * s, cy - 10 * s, 38 * s, 34 * s);
            using (GraphicsPath sp = CreateRoundedRect(slidesCard, 10 * s))
            {
                // Shadow
                using (GraphicsPath sShadow = (GraphicsPath)sp.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(-3 * s, 3 * s); sShadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillPath(sb, sShadow);
                }

                using (LinearGradientBrush sb = new LinearGradientBrush(slidesCard, Color.FromArgb(249, 115, 22), Color.FromArgb(217, 70, 239), 45f))
                using (Pen sPen = new Pen(Color.FromArgb(254, 215, 170), 1.4f * s))
                {
                    g.FillPath(sb, sp);
                    g.DrawPath(sPen, sp);
                }
                // Play icon
                PointF sc = new PointF(slidesCard.X + slidesCard.Width * 0.52f, slidesCard.Y + slidesCard.Height * 0.5f);
                PointF[] tri = new PointF[] {
                    new PointF(sc.X - 5 * s, sc.Y - 6.5f * s),
                    new PointF(sc.X + 6 * s, sc.Y),
                    new PointF(sc.X - 5 * s, sc.Y + 6.5f * s)
                };
                using (SolidBrush pb = new SolidBrush(Color.White)) g.FillPolygon(pb, tri);
            }

            // CARD 3: WRITER (Blue - Top Left: X: cx-32s, Y: cy-34s)
            RectangleF writerCard = new RectangleF(cx - 32 * s, cy - 34 * s, 38 * s, 38 * s);
            using (GraphicsPath wp = CreateRoundedRect(writerCard, 10 * s))
            {
                // Shadow
                using (GraphicsPath wShadow = (GraphicsPath)wp.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(0, 4 * s); wShadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(80, 0, 0, 0))) g.FillPath(sb, wShadow);
                }

                using (LinearGradientBrush wb = new LinearGradientBrush(writerCard, Color.FromArgb(59, 130, 246), Color.FromArgb(29, 78, 216), 45f))
                using (Pen wPen = new Pen(Color.FromArgb(191, 219, 254), 1.4f * s))
                {
                    g.FillPath(wb, wp);
                    g.DrawPath(wPen, wp);
                }

                // Folded corner
                using (GraphicsPath foldP = new GraphicsPath())
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(147, 197, 253)))
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(writerCard.Right - 10 * s, writerCard.Top),
                        new PointF(writerCard.Right, writerCard.Top + 10 * s),
                        new PointF(writerCard.Right - 10 * s, writerCard.Top + 10 * s)
                    });
                    g.FillPath(fb, foldP);
                }

                // 3 Document lines
                using (Pen lp = new Pen(Color.White, 1.8f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(lp, writerCard.Left + 8 * s, writerCard.Top + 12 * s, writerCard.Right - 14 * s, writerCard.Top + 12 * s);
                    g.DrawLine(lp, writerCard.Left + 8 * s, writerCard.Top + 18 * s, writerCard.Right - 8 * s, writerCard.Top + 18 * s);
                    g.DrawLine(lp, writerCard.Left + 8 * s, writerCard.Top + 24 * s, writerCard.Left + 20 * s, writerCard.Top + 24 * s);
                }
            }
        }

        static void Main(string[] args)
        {
            string outDir = @"C:\Users\Danh\.gemini\antigravity-ide\brain\b1e34c81-1d9d-4dbf-a2bd-168e6ec5fad6";

            using (Bitmap cmp = new Bitmap(800, 360, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(cmp))
            using (Font titleF = new Font("Segoe UI", 13f, FontStyle.Bold))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.FromArgb(10, 14, 26));

                // Design D: Mobius G (Möbius Tri-Flow 3D Ribbon)
                DrawMobiusG(g, 60, 40, 220, true);
                using (SolidBrush tb = new SolidBrush(Color.White))
                {
                    g.DrawString("Design D: Möbius 3D Ribbon 'G'", titleF, tb, 60, 280);
                }

                // Design E: Glass Stack G (3-Tier Layered Glass Suite)
                DrawGlassStackG(g, 460, 40, 220, true);
                using (SolidBrush tb = new SolidBrush(Color.White))
                {
                    g.DrawString("Design E: 3-Tier Glass Stack 'G'", titleF, tb, 460, 280);
                }

                cmp.Save(Path.Combine(outDir, "flagship_comparison.png"), ImageFormat.Png);
            }

            Console.WriteLine("[SUCCESS] Flagship comparison board generated!");
        }
    }
}

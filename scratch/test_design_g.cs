using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace GenOffice.Test
{
    class MasterDesignGStudio
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

        public static void DrawMasterLogoG(Graphics g, float x, float y, float size, bool drawBackdrop, bool highDetail)
        {
            if (g == null || size <= 0) return;

            SmoothingMode prevSmooth = g.SmoothingMode;
            InterpolationMode prevInterp = g.InterpolationMode;
            PixelOffsetMode prevPixel = g.PixelOffsetMode;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float s = size / 100f;
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            // 1. BACKDROP: Ultra-Deep Space Glass (#070A14 -> #141B2D)
            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                float bRad = size * 0.24f;

                // Soft ambient shadow
                if (size >= 32)
                {
                    RectangleF shadowRect = new RectangleF(x, y + 2.5f * s, size, size);
                    using (GraphicsPath sPath = CreateRoundedRect(shadowRect, bRad))
                    using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                    {
                        g.FillPath(shadowB, sPath);
                    }
                }

                using (GraphicsPath bp = CreateRoundedRect(bRect, bRad))
                {
                    using (LinearGradientBrush bg = new LinearGradientBrush(
                        bRect,
                        Color.FromArgb(7, 10, 20),
                        Color.FromArgb(20, 27, 45),
                        60f))
                    {
                        g.FillPath(bg, bp);
                    }

                    // Glass Gloss
                    if (size >= 24)
                    {
                        RectangleF glossRect = new RectangleF(x + 1.2f * s, y + 1.2f * s, size - 2.4f * s, size * 0.44f);
                        using (GraphicsPath glossPath = CreateRoundedRect(glossRect, bRad * 0.75f))
                        using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                            glossRect,
                            Color.FromArgb(45, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            90f))
                        {
                            g.FillPath(glossBrush, glossPath);
                        }
                    }

                    // Frosted Glass rim
                    using (Pen p = new Pen(Color.FromArgb(45, 255, 255, 255), Math.Max(1f, 1.2f * s)))
                    {
                        g.DrawPath(p, bp);
                    }
                }
            }

            // 2. THE DYNAMIC "G" EMBLEM FRAMEWORK (Connecting Glass Orbit)
            // An ultra-clean sweeping glass loop that subtly reinforces the "G" form
            if (highDetail && size >= 36)
            {
                RectangleF gOrbitBox = new RectangleF(cx - 36 * s, cy - 36 * s, 72 * s, 72 * s);
                using (Pen gOrbitPen = new Pen(Color.FromArgb(35, 255, 255, 255), 4f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(gOrbitPen, gOrbitBox, 310, 200); // 310 to 150
                }
            }

            // 3. THE 3 SUITE PILLARS (Precision Layered & Beveled)
            // =========================================================================================
            // A. CALC POD (Emerald Green - Bottom Foundation)
            // =========================================================================================
            RectangleF calcCard = new RectangleF(cx - 32 * s, cy - 1 * s, 36 * s, 36 * s);
            float cardRad = 9.5f * s;

            using (GraphicsPath cp = CreateRoundedRect(calcCard, cardRad))
            {
                using (LinearGradientBrush cb = new LinearGradientBrush(
                    calcCard,
                    Color.FromArgb(16, 185, 129), // Emerald 500
                    Color.FromArgb(4, 120, 87),    // Emerald 700
                    45f))
                using (Pen cPen = new Pen(Color.FromArgb(167, 243, 208), Math.Max(1f, 1.3f * s)))
                {
                    g.FillPath(cb, cp);
                    g.DrawPath(cPen, cp);
                }

                // Mini 3-bar Analytics Chart
                if (size >= 20)
                {
                    float bw = Math.Max(1.8f, 3.6f * s);
                    float bgap = 2.2f * s;
                    float baseBx = calcCard.X + 8.5f * s;
                    float baseBy = calcCard.Bottom - 9.5f * s;

                    using (SolidBrush b1 = new SolidBrush(Color.FromArgb(167, 243, 208)))
                    using (SolidBrush b2 = new SolidBrush(Color.FromArgb(236, 253, 245)))
                    using (SolidBrush b3 = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(b1, baseBx, baseBy - 6 * s, bw, 6 * s);
                        g.FillRectangle(b2, baseBx + (bw + bgap), baseBy - 11 * s, bw, 11 * s);
                        g.FillRectangle(b3, baseBx + (bw + bgap) * 2, baseBy - 16 * s, bw, 16 * s);
                    }
                }
            }

            // =========================================================================================
            // B. SLIDES POD (Coral Sunset - Center-Right Spur of 'G')
            // =========================================================================================
            RectangleF slidesCard = new RectangleF(cx - 3 * s, cy - 9 * s, 37 * s, 33 * s);
            using (GraphicsPath sp = CreateRoundedRect(slidesCard, cardRad))
            {
                // Soft depth shadow
                using (GraphicsPath sShadow = (GraphicsPath)sp.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(-2.5f * s, 3f * s); sShadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(75, 0, 0, 0))) g.FillPath(sb, sShadow);
                }

                using (LinearGradientBrush sb = new LinearGradientBrush(
                    slidesCard,
                    Color.FromArgb(249, 115, 22),  // Orange 500
                    Color.FromArgb(225, 29, 72),   // Rose 600
                    45f))
                using (Pen sPen = new Pen(Color.FromArgb(254, 215, 170), Math.Max(1f, 1.3f * s)))
                {
                    g.FillPath(sb, sp);
                    g.DrawPath(sPen, sp);
                }

                // Play Triangle
                if (size >= 18)
                {
                    PointF sc = new PointF(slidesCard.X + slidesCard.Width * 0.52f, slidesCard.Y + slidesCard.Height * 0.5f);
                    float triH = 5.5f * s;
                    float triW = 4.8f * s;
                    PointF[] tri = new PointF[] {
                        new PointF(sc.X - triW * 0.45f, sc.Y - triH * 0.55f),
                        new PointF(sc.X + triW * 0.7f, sc.Y),
                        new PointF(sc.X - triW * 0.45f, sc.Y + triH * 0.55f)
                    };
                    using (SolidBrush pb = new SolidBrush(Color.White)) g.FillPolygon(pb, tri);
                }
            }

            // =========================================================================================
            // C. WRITER POD (Royal Ocean Blue - Top-Left Arch of 'G')
            // =========================================================================================
            RectangleF writerCard = new RectangleF(cx - 32 * s, cy - 33 * s, 37 * s, 37 * s);
            using (GraphicsPath wp = CreateRoundedRect(writerCard, cardRad))
            {
                // Depth shadow
                using (GraphicsPath wShadow = (GraphicsPath)wp.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(0, 3.5f * s); wShadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillPath(sb, wShadow);
                }

                using (LinearGradientBrush wb = new LinearGradientBrush(
                    writerCard,
                    Color.FromArgb(59, 130, 246), // Blue 500
                    Color.FromArgb(29, 78, 216),  // Blue 700
                    45f))
                using (Pen wPen = new Pen(Color.FromArgb(191, 219, 254), Math.Max(1f, 1.3f * s)))
                {
                    g.FillPath(wb, wp);
                    g.DrawPath(wPen, wp);
                }

                // Folded Corner
                if (size >= 18)
                {
                    using (GraphicsPath foldP = new GraphicsPath())
                    using (SolidBrush fb = new SolidBrush(Color.FromArgb(147, 197, 253)))
                    {
                        float foldS = 9.5f * s;
                        foldP.AddPolygon(new PointF[] {
                            new PointF(writerCard.Right - foldS, writerCard.Top),
                            new PointF(writerCard.Right, writerCard.Top + foldS),
                            new PointF(writerCard.Right - foldS, writerCard.Top + foldS)
                        });
                        g.FillPath(fb, foldP);
                    }

                    // 3 Document Text Lines
                    using (Pen lp = new Pen(Color.White, Math.Max(1.2f, 1.8f * s)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(lp, writerCard.Left + 7.5f * s, writerCard.Top + 12 * s, writerCard.Right - 13 * s, writerCard.Top + 12 * s);
                        g.DrawLine(lp, writerCard.Left + 7.5f * s, writerCard.Top + 18 * s, writerCard.Right - 7.5f * s, writerCard.Top + 18 * s);
                        g.DrawLine(lp, writerCard.Left + 7.5f * s, writerCard.Top + 24 * s, writerCard.Left + 19 * s, writerCard.Top + 24 * s);
                    }
                }
            }

            g.SmoothingMode = prevSmooth;
            g.InterpolationMode = prevInterp;
            g.PixelOffsetMode = prevPixel;
        }

        static void Main(string[] args)
        {
            string outDir = @"C:\Users\Danh\.gemini\antigravity-ide\brain\b1e34c81-1d9d-4dbf-a2bd-168e6ec5fad6";

            // 1. Large 256x256 Master
            using (Bitmap b256 = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(b256))
            {
                g.Clear(Color.FromArgb(7, 10, 20));
                DrawMasterLogoG(g, 16, 16, 224, true, true);
                b256.Save(Path.Combine(outDir, "master_logo_g_256.png"), ImageFormat.Png);
            }

            // 2. Brand presentation board (520x160)
            using (Bitmap bBrand = new Bitmap(520, 160, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bBrand))
            using (Font bf = new Font("Segoe UI", 25f, FontStyle.Bold))
            using (Font sf = new Font("Segoe UI", 12f, FontStyle.Regular))
            {
                g.Clear(Color.FromArgb(7, 10, 20));
                DrawMasterLogoG(g, 24, 24, 112, true, true);
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString("GenOffice", bf, tb, 156, 32);
                }
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("Writer • Calc • Slides (Bộ ứng dụng 3-trong-1)", sf, sb, 158, 76);
                }
                bBrand.Save(Path.Combine(outDir, "brand_board_g.png"), ImageFormat.Png);
            }

            Console.WriteLine("[SUCCESS] Master Design G generated!");
        }
    }
}

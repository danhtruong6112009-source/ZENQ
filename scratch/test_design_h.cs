using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace GenOffice.Test
{
    class MasterDesignHStudio
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

        public static void DrawMasterLogoH(Graphics g, float x, float y, float size, bool drawBackdrop, bool highDetail)
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

            // 1. BACKDROP: Luxury Dark Obsidian Glass Squircle
            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                float bRad = size * 0.24f;

                // Ambient Shadow
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
                        Color.FromArgb(9, 13, 24),
                        Color.FromArgb(22, 30, 48),
                        60f))
                    {
                        g.FillPath(bg, bp);
                    }

                    // Glass Gloss highlight
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

                    // Glass Rim Border
                    using (Pen p = new Pen(Color.FromArgb(45, 255, 255, 255), Math.Max(1f, 1.2f * s)))
                    {
                        g.DrawPath(p, bp);
                    }
                }
            }

            // 2. THE CONTINUOUS FUSED MONOGRAM 'G'
            // Precise geometric parameters:
            float strokeW = 12.5f * s;
            float r = 26f * s; // radius of G center line
            RectangleF arcBox = new RectangleF(cx - r, cy - r, r * 2, r * 2);

            // =========================================================================
            // PART 1: GREEN FACET (CALC - Bottom Curve from 60° to 175°)
            // =========================================================================
            using (Pen gp = new Pen(Color.FromArgb(5, 150, 105), strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (LinearGradientBrush gGrad = new LinearGradientBrush(arcBox, Color.FromArgb(52, 211, 153), Color.FromArgb(4, 120, 87), 135f))
            {
                gp.Brush = gGrad;
                g.DrawArc(gp, arcBox, 50, 115); // 50 to 165
            }

            // Micro-detail: 3 Analytics Column Bars embedded on Green base
            if (size >= 20)
            {
                float barW = Math.Max(1.6f, 3.2f * s);
                float barGap = 1.8f * s;
                float startBx = cx - 18f * s;
                float baseBy = cy + 30f * s;

                using (SolidBrush b1 = new SolidBrush(Color.FromArgb(167, 243, 208)))
                using (SolidBrush b2 = new SolidBrush(Color.FromArgb(236, 253, 245)))
                using (SolidBrush b3 = new SolidBrush(Color.White))
                {
                    g.FillRectangle(b1, startBx, baseBy - 5 * s, barW, 5 * s);
                    g.FillRectangle(b2, startBx + (barW + barGap), baseBy - 9.5f * s, barW, 9.5f * s);
                    g.FillRectangle(b3, startBx + (barW + barGap) * 2, baseBy - 14f * s, barW, 14f * s);
                }
            }

            // =========================================================================
            // PART 2: ORANGE FACET (SLIDES - Right Wall & Inward Spur)
            // =========================================================================
            using (GraphicsPath orangeSpur = new GraphicsPath())
            {
                // Vertical right bar from cy + 18s to cy - 4s
                RectangleF spurRect = new RectangleF(cx + 2f * s, cy - 6f * s, 26f * s, 12.5f * s);
                using (GraphicsPath sp = CreateRoundedRect(spurRect, 6f * s))
                {
                    // Drop shadow
                    using (GraphicsPath sShadow = (GraphicsPath)sp.Clone())
                    {
                        using (Matrix m = new Matrix()) { m.Translate(-2f * s, 2.5f * s); sShadow.Transform(m); }
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillPath(sb, sShadow);
                    }

                    using (LinearGradientBrush oGrad = new LinearGradientBrush(spurRect, Color.FromArgb(251, 146, 60), Color.FromArgb(225, 29, 72), 45f))
                    using (Pen sBorder = new Pen(Color.FromArgb(254, 215, 170), Math.Max(1f, 1.2f * s)))
                    {
                        g.FillPath(oGrad, sp);
                        g.DrawPath(sBorder, sp);
                    }
                }

                // Play triangle on orange spur
                if (size >= 18)
                {
                    PointF pCenter = new PointF(cx + 15f * s, cy);
                    float triH = 5f * s;
                    float triW = 4.4f * s;
                    PointF[] tri = new PointF[] {
                        new PointF(pCenter.X - triW * 0.45f, pCenter.Y - triH * 0.55f),
                        new PointF(pCenter.X + triW * 0.7f, pCenter.Y),
                        new PointF(pCenter.X - triW * 0.45f, pCenter.Y + triH * 0.55f)
                    };
                    using (SolidBrush pb = new SolidBrush(Color.White)) g.FillPolygon(pb, tri);
                }
            }

            // Right vertical connector
            using (Pen op = new Pen(Color.FromArgb(249, 115, 22), strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (LinearGradientBrush oGrad = new LinearGradientBrush(arcBox, Color.FromArgb(251, 146, 60), Color.FromArgb(234, 88, 12), 45f))
            {
                op.Brush = oGrad;
                g.DrawArc(op, arcBox, 0, 55); // 0 to 55
            }

            // =========================================================================
            // PART 3: BLUE FACET (WRITER - Top Arch & Spine from 155° to 350°)
            // =========================================================================
            using (Pen bp = new Pen(Color.FromArgb(37, 99, 235), strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (LinearGradientBrush bGrad = new LinearGradientBrush(arcBox, Color.FromArgb(96, 165, 250), Color.FromArgb(29, 78, 216), 45f))
            {
                bp.Brush = bGrad;
                g.DrawArc(bp, arcBox, 160, 185); // 160 around to 345
            }

            // Document Fold on top-right of Blue Arch
            if (size >= 18)
            {
                PointF foldPt = new PointF(cx + 20f * s, cy - 25f * s);
                using (GraphicsPath foldP = new GraphicsPath())
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(224, 231, 255)))
                {
                    foldP.AddPolygon(new PointF[] {
                        new PointF(foldPt.X, foldPt.Y - 6.5f * s),
                        new PointF(foldPt.X + 7.5f * s, foldPt.Y),
                        new PointF(foldPt.X, foldPt.Y)
                    });
                    g.FillPath(fb, foldP);
                }

                // 2 White Document Lines on top arch
                using (Pen lp = new Pen(Color.White, Math.Max(1f, 1.4f * s)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(lp, cx - 18f * s, cy - 26f * s, cx + 12f * s, cy - 26f * s);
                    g.DrawLine(lp, cx - 18f * s, cy - 20f * s, cx + 5f * s, cy - 20f * s);
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
                g.Clear(Color.FromArgb(9, 13, 24));
                DrawMasterLogoH(g, 16, 16, 224, true, true);
                b256.Save(Path.Combine(outDir, "master_logo_h_256.png"), ImageFormat.Png);
            }

            // 2. Brand presentation board (520x160)
            using (Bitmap bBrand = new Bitmap(520, 160, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bBrand))
            using (Font bf = new Font("Segoe UI", 25f, FontStyle.Bold))
            using (Font sf = new Font("Segoe UI", 12f, FontStyle.Regular))
            {
                g.Clear(Color.FromArgb(9, 13, 24));
                DrawMasterLogoH(g, 24, 24, 112, true, true);
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString("GenOffice", bf, tb, 156, 32);
                }
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("Writer • Calc • Slides (Bộ ứng dụng 3-trong-1)", sf, sb, 158, 76);
                }
                bBrand.Save(Path.Combine(outDir, "brand_board_h.png"), ImageFormat.Png);
            }

            // 3. Titlebar preview (450x36)
            using (Bitmap bBar = new Bitmap(450, 36, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bBar))
            using (Font bf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            {
                using (LinearGradientBrush topBarBrush = new LinearGradientBrush(new Rectangle(0, 0, 450, 36), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 90f))
                {
                    g.FillRectangle(topBarBrush, 0, 0, 450, 36);
                }
                DrawMasterLogoH(g, 12, 5, 26, true, false);
                using (SolidBrush brandBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString("GenOffice", bf, brandBrush, 44, 8);
                }
                bBar.Save(Path.Combine(outDir, "titlebar_preview_h.png"), ImageFormat.Png);
            }

            Console.WriteLine("[SUCCESS] Master Design H generated!");
        }
    }
}

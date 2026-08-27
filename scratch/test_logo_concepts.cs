using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace GenOffice.Test
{
    class ConceptGenerator
    {
        static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
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

        // =========================================================================
        // CONCEPT A: FLUID ORIGAMI 3D RIBBON 'G' (Microsoft 365 / Apple Style)
        // 3 overlapping continuous sculptural ribbons forming a flawless dynamic 'G'
        // =========================================================================
        static void DrawConceptA(Graphics g, float x, float y, float size, bool drawBackdrop)
        {
            float s = size / 100f;
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                using (GraphicsPath bp = CreateRoundedRectanglePath(bRect, size * 0.23f))
                {
                    // Dark luxury obsidian glass background
                    using (LinearGradientBrush bg = new LinearGradientBrush(bRect, Color.FromArgb(13, 17, 28), Color.FromArgb(22, 30, 49), 45f))
                    {
                        g.FillPath(bg, bp);
                    }
                    // Subtle ambient edge highlight
                    using (Pen bpPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1.2f * s))
                    {
                        g.DrawPath(bpPen, bp);
                    }
                }
            }

            // Outer G center and radius
            float rOut = 36f * s;
            float rIn = 22f * s;
            float thickness = rOut - rIn; // 14s

            // 1. CALC LAYER (Green - Lower Curve): Sweeps from 175 deg to 360/0 deg (bottom half)
            RectangleF outRect = new RectangleF(cx - rOut, cy - rOut, rOut * 2, rOut * 2);
            RectangleF inRect = new RectangleF(cx - rIn, cy - rIn, rIn * 2, rIn * 2);

            using (GraphicsPath greenPath = new GraphicsPath())
            {
                greenPath.AddArc(outRect, 60, 125); // 60 to 185
                greenPath.AddArc(inRect, 185, -125);
                greenPath.CloseFigure();

                using (LinearGradientBrush gBrush = new LinearGradientBrush(
                    new RectangleF(cx - rOut, cy, rOut * 2, rOut),
                    Color.FromArgb(16, 185, 129), // Emerald 500
                    Color.FromArgb(5, 150, 105),   // Emerald 600
                    0f))
                {
                    g.FillPath(gBrush, greenPath);
                }
            }

            // 2. SLIDES LAYER (Orange - Inward Spur & Right Wall):
            // Curves up on the right and extends inward into the center
            using (GraphicsPath orangePath = new GraphicsPath())
            {
                // Right upward curve + horizontal crossbar
                orangePath.AddArc(outRect, 0, 65); // 0 to 65
                // Line inward
                orangePath.AddLine(cx + rOut * (float)Math.Cos(65 * Math.PI / 180), cy + rOut * (float)Math.Sin(65 * Math.PI / 180), cx + 5 * s, cy + 8 * s);
                // Tip of spur
                orangePath.AddArc(cx - 2 * s, cy - 6 * s, 14 * s, 14 * s, 180, 90);
                orangePath.AddLine(cx + 5 * s, cy - 6 * s, cx + rOut, cy - 6 * s);
                orangePath.AddArc(cx + rOut - thickness, cy - 6 * s, thickness, thickness, 270, 90);
                orangePath.CloseFigure();

                // Drop shadow beneath orange layer
                using (GraphicsPath oShadow = (GraphicsPath)orangePath.Clone())
                {
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(-2 * s, 2 * s);
                        oShadow.Transform(m);
                    }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        g.FillPath(sb, oShadow);
                    }
                }

                using (LinearGradientBrush oBrush = new LinearGradientBrush(
                    new RectangleF(cx, cy - 10 * s, rOut, rOut),
                    Color.FromArgb(251, 146, 60), // Orange 400
                    Color.FromArgb(234, 88, 12),  // Orange 600
                    45f))
                {
                    g.FillPath(oBrush, orangePath);
                }
            }

            // 3. WRITER LAYER (Blue - Upper Loop & Spine):
            // Sweeps from top-right (35 deg) around the top to the left spine (190 deg)
            using (GraphicsPath bluePath = new GraphicsPath())
            {
                bluePath.AddArc(outRect, 180, 185); // 180 to 5
                // Top-right folded terminal
                PointF pEndOut = new PointF(cx + rOut * (float)Math.Cos(5 * Math.PI / 180), cy + rOut * (float)Math.Sin(5 * Math.PI / 180));
                PointF pEndIn = new PointF(cx + rIn * (float)Math.Cos(5 * Math.PI / 180), cy + rIn * (float)Math.Sin(5 * Math.PI / 180));
                bluePath.AddLine(pEndOut, pEndIn);
                bluePath.AddArc(inRect, 5, -185);
                bluePath.CloseFigure();

                // Drop shadow beneath blue layer
                using (GraphicsPath bShadow = (GraphicsPath)bluePath.Clone())
                {
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, 3 * s);
                        bShadow.Transform(m);
                    }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    {
                        g.FillPath(sb, bShadow);
                    }
                }

                using (LinearGradientBrush bBrush = new LinearGradientBrush(
                    new RectangleF(cx - rOut, cy - rOut, rOut * 2, rOut),
                    Color.FromArgb(96, 165, 250), // Blue 400
                    Color.FromArgb(29, 78, 216),  // Blue 700
                    45f))
                {
                    g.FillPath(bBrush, bluePath);
                }
            }

            // Top-right document folded tip on Blue ribbon
            using (GraphicsPath foldTip = new GraphicsPath())
            {
                foldTip.AddPolygon(new PointF[] {
                    new PointF(cx + 26 * s, cy - 35 * s),
                    new PointF(cx + 36 * s, cy - 25 * s),
                    new PointF(cx + 26 * s, cy - 25 * s)
                });
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(219, 234, 254)))
                {
                    g.FillPath(fb, foldTip);
                }
            }
        }

        // =========================================================================
        // CONCEPT B: ULTRA-MODERN 3-TIER LAYERED GLASS EMBLEM 'G'
        // 3 floating isometric glass capsules / cards forming the letter G
        // =========================================================================
        static void DrawConceptB(Graphics g, float x, float y, float size, bool drawBackdrop)
        {
            float s = size / 100f;
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                using (GraphicsPath bp = CreateRoundedRectanglePath(bRect, size * 0.24f))
                using (LinearGradientBrush bg = new LinearGradientBrush(bRect, Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 90f))
                {
                    g.FillPath(bg, bp);
                    using (Pen p = new Pen(Color.FromArgb(40, 255, 255, 255), 1.2f * s)) g.DrawPath(p, bp);
                }
            }

            // Outer G curve in 3 sleek layered strokes with rounded caps
            float strokeW = 12f * s;
            RectangleF arcBox = new RectangleF(cx - 30 * s, cy - 30 * s, 60 * s, 60 * s);

            // Layer 1: Blue Arch (Writer) - Top & Left (140 to 360 deg)
            using (Pen bp = new Pen(Color.FromArgb(37, 99, 235), strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (LinearGradientBrush bGrad = new LinearGradientBrush(arcBox, Color.FromArgb(96, 165, 250), Color.FromArgb(29, 78, 216), 45f))
            {
                bp.Brush = bGrad;
                g.DrawArc(bp, arcBox, 145, 185);
            }

            // Layer 2: Green Arch (Calc) - Bottom (40 to 145 deg)
            using (Pen gp = new Pen(Color.FromArgb(5, 150, 105), strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (LinearGradientBrush gGrad = new LinearGradientBrush(arcBox, Color.FromArgb(52, 211, 153), Color.FromArgb(4, 120, 87), 135f))
            {
                gp.Brush = gGrad;
                g.DrawArc(gp, arcBox, 40, 105);
            }

            // Layer 3: Orange Crossbar & Right Post (Slides)
            // Vertical right post
            using (Pen op = new Pen(Color.FromArgb(234, 88, 12), strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (LinearGradientBrush oGrad = new LinearGradientBrush(new RectangleF(cx, cy - 10 * s, 35 * s, 35 * s), Color.FromArgb(251, 146, 60), Color.FromArgb(217, 70, 239), 45f))
            {
                op.Brush = oGrad;
                // Inward crossbar from right to center
                g.DrawLine(op, cx + 30 * s, cy + 2 * s, cx + 6 * s, cy + 2 * s);
            }

            // 3 Pure Minimalist Functional Badges floating subtly inside
            // Writer Badge: Mini folded doc at top right
            RectangleF docB = new RectangleF(cx + 12 * s, cy - 32 * s, 16 * s, 18 * s);
            using (GraphicsPath dp = CreateRoundedRectanglePath(docB, 3 * s))
            using (SolidBrush db = new SolidBrush(Color.White))
            using (Pen dPen = new Pen(Color.FromArgb(191, 219, 254), 1f * s))
            {
                g.FillPath(db, dp);
                g.DrawPath(dPen, dp);
                // Fold
                using (GraphicsPath fp = new GraphicsPath())
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(147, 197, 253)))
                {
                    fp.AddPolygon(new PointF[] { new PointF(docB.Right - 5 * s, docB.Top), new PointF(docB.Right, docB.Top + 5 * s), new PointF(docB.Right - 5 * s, docB.Top + 5 * s) });
                    g.FillPath(fb, fp);
                }
                // Lines
                using (Pen lp = new Pen(Color.FromArgb(37, 99, 235), 1.4f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(lp, docB.Left + 3 * s, docB.Top + 6 * s, docB.Right - 6 * s, docB.Top + 6 * s);
                    g.DrawLine(lp, docB.Left + 3 * s, docB.Top + 10 * s, docB.Right - 3 * s, docB.Top + 10 * s);
                }
            }

            // Calc Badge: 3 clean glowing columns inside bottom curve
            float barW = 3.5f * s;
            float barBase = cy + 26 * s;
            using (SolidBrush c1 = new SolidBrush(Color.FromArgb(167, 243, 208)))
            using (SolidBrush c2 = new SolidBrush(Color.FromArgb(236, 253, 245)))
            using (SolidBrush c3 = new SolidBrush(Color.White))
            {
                g.FillRectangle(c1, cx - 18 * s, barBase - 6 * s, barW, 6 * s);
                g.FillRectangle(c2, cx - 12 * s, barBase - 11 * s, barW, 11 * s);
                g.FillRectangle(c3, cx - 6 * s, barBase - 16 * s, barW, 16 * s);
            }

            // Slides Badge: Glowing play diamond in center spur
            PointF pTri = new PointF(cx + 8 * s, cy + 2 * s);
            PointF[] tri = new PointF[] {
                new PointF(pTri.X - 3 * s, pTri.Y - 4 * s),
                new PointF(pTri.X + 4 * s, pTri.Y),
                new PointF(pTri.X - 3 * s, pTri.Y + 4 * s)
            };
            using (SolidBrush pb = new SolidBrush(Color.White))
            {
                g.FillPolygon(pb, tri);
            }
        }

        // =========================================================================
        // CONCEPT C: INTERLOCKING GEOMETRIC PRISM "G" (Cleanest, World-Class Flagship)
        // 3 seamless geometric petals overlapping with glass gradients forming 'G'
        // =========================================================================
        static void DrawConceptC(Graphics g, float x, float y, float size, bool drawBackdrop)
        {
            float s = size / 100f;
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            if (drawBackdrop)
            {
                RectangleF bRect = new RectangleF(x, y, size, size);
                using (GraphicsPath bp = CreateRoundedRectanglePath(bRect, size * 0.24f))
                using (LinearGradientBrush bg = new LinearGradientBrush(bRect, Color.FromArgb(10, 14, 26), Color.FromArgb(24, 34, 53), 60f))
                {
                    g.FillPath(bg, bp);
                    using (Pen p = new Pen(Color.FromArgb(45, 255, 255, 255), 1.2f * s)) g.DrawPath(p, bp);
                }
            }

            // The 'G' is formed by 3 interlocking stylized swooshes / cards:
            // 1. Blue Card (Writer): Top rounded slab (36W x 14H) curving down left (14W x 34H)
            // 2. Green Card (Calc): Bottom rounded slab (44W x 14H) sweeping right
            // 3. Orange Card (Slides): Center horizontal slide card (30W x 18H) with right wall

            // --- GREEN (Bottom Curve) ---
            using (GraphicsPath gp = new GraphicsPath())
            {
                // Sweeping bottom curve from bottom-left to bottom-right
                gp.AddArc(cx - 32 * s, cy + 2 * s, 64 * s, 32 * s, 30, 150);
                gp.AddArc(cx - 20 * s, cy + 8 * s, 40 * s, 20 * s, 180, -150);
                gp.CloseFigure();

                using (LinearGradientBrush gBr = new LinearGradientBrush(
                    new RectangleF(cx - 32 * s, cy, 64 * s, 34 * s),
                    Color.FromArgb(16, 185, 129),
                    Color.FromArgb(5, 150, 105),
                    0f))
                {
                    g.FillPath(gBr, gp);
                }
            }

            // --- BLUE (Top Arch & Spine) ---
            using (GraphicsPath bp = new GraphicsPath())
            {
                bp.AddArc(cx - 32 * s, cy - 34 * s, 64 * s, 64 * s, 120, 205); // 120 around to 325
                bp.AddArc(cx - 20 * s, cy - 22 * s, 40 * s, 40 * s, 325, -205);
                bp.CloseFigure();

                // Drop shadow
                using (GraphicsPath shadow = (GraphicsPath)bp.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(0, 2.5f * s); shadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillPath(sb, shadow);
                }

                using (LinearGradientBrush bBr = new LinearGradientBrush(
                    new RectangleF(cx - 32 * s, cy - 34 * s, 64 * s, 40 * s),
                    Color.FromArgb(96, 165, 250),
                    Color.FromArgb(37, 99, 235),
                    45f))
                {
                    g.FillPath(bBr, bp);
                }
            }

            // Document Fold on top blue
            using (GraphicsPath foldP = new GraphicsPath())
            {
                foldP.AddPolygon(new PointF[] {
                    new PointF(cx + 20 * s, cy - 32 * s),
                    new PointF(cx + 31 * s, cy - 21 * s),
                    new PointF(cx + 20 * s, cy - 21 * s)
                });
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(224, 231, 255))) g.FillPath(fb, foldP);
            }

            // --- ORANGE (Presentation Slide Card & Crossbar) ---
            RectangleF slideCard = new RectangleF(cx - 4 * s, cy - 7 * s, 36 * s, 22 * s);
            using (GraphicsPath sp = CreateRoundedRectanglePath(slideCard, 5 * s))
            {
                // Drop shadow
                using (GraphicsPath shadow = (GraphicsPath)sp.Clone())
                {
                    using (Matrix m = new Matrix()) { m.Translate(-2 * s, 2.5f * s); shadow.Transform(m); }
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillPath(sb, shadow);
                }

                using (LinearGradientBrush sBr = new LinearGradientBrush(slideCard, Color.FromArgb(251, 146, 60), Color.FromArgb(234, 88, 12), 45f))
                using (Pen sPen = new Pen(Color.FromArgb(254, 215, 170), 1.4f * s))
                {
                    g.FillPath(sBr, sp);
                    g.DrawPath(sPen, sp);
                }

                // White Play Icon on Slide
                PointF sc = new PointF(slideCard.X + slideCard.Width * 0.5f, slideCard.Y + slideCard.Height * 0.5f);
                PointF[] tri = new PointF[] {
                    new PointF(sc.X - 4 * s, sc.Y - 5 * s),
                    new PointF(sc.X + 5 * s, sc.Y),
                    new PointF(sc.X - 4 * s, sc.Y + 5 * s)
                };
                using (SolidBrush pb = new SolidBrush(Color.White)) g.FillPolygon(pb, tri);
            }

            // Calc Data Bars on Green curve
            using (SolidBrush barB1 = new SolidBrush(Color.FromArgb(167, 243, 208)))
            using (SolidBrush barB2 = new SolidBrush(Color.FromArgb(236, 253, 245)))
            using (SolidBrush barB3 = new SolidBrush(Color.White))
            {
                float baseH = cy + 28 * s;
                g.FillRectangle(barB1, cx - 20 * s, baseH - 5 * s, 3.5f * s, 5 * s);
                g.FillRectangle(barB2, cx - 14 * s, baseH - 9 * s, 3.5f * s, 9 * s);
                g.FillRectangle(barB3, cx - 8 * s, baseH - 13 * s, 3.5f * s, 13 * s);
            }
        }

        static void Main(string[] args)
        {
            string outDir = @"C:\Users\Danh\.gemini\antigravity-ide\brain\b1e34c81-1d9d-4dbf-a2bd-168e6ec5fad6";

            // Render all 3 concepts side by side on a comparison board
            using (Bitmap cmp = new Bitmap(900, 320, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(cmp))
            using (Font titleF = new Font("Segoe UI", 13f, FontStyle.Bold))
            using (Font subF = new Font("Segoe UI", 10f, FontStyle.Regular))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.FromArgb(11, 15, 25));

                // Concept A
                DrawConceptA(g, 50, 40, 200, true);
                using (SolidBrush tb = new SolidBrush(Color.White))
                {
                    g.DrawString("Concept A: Origami 3D Flow", titleF, tb, 50, 255);
                }

                // Concept B
                DrawConceptB(g, 350, 40, 200, true);
                using (SolidBrush tb = new SolidBrush(Color.White))
                {
                    g.DrawString("Concept B: Liquid Glass Badges", titleF, tb, 350, 255);
                }

                // Concept C
                DrawConceptC(g, 650, 40, 200, true);
                using (SolidBrush tb = new SolidBrush(Color.White))
                {
                    g.DrawString("Concept C: Interlocking Prism", titleF, tb, 650, 255);
                }

                cmp.Save(Path.Combine(outDir, "concept_comparison.png"), ImageFormat.Png);
            }

            Console.WriteLine("[SUCCESS] Comparison board generated!");
        }
    }
}

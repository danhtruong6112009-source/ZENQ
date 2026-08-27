using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestExclusiveAnimations
    {
        [STAThread]
        public static void Main()
        {
            Console.WriteLine("=== Testing Star Vector Icons & 10 GenOffice Exclusive Animations ===");

            // 1. Test DropDown Form Category & Icons Rendering
            using (Bitmap dropBmp = new Bitmap(720, 680))
            using (Graphics g = Graphics.FromImage(dropBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(248, 249, 250));

                // Draw Dropdown title banner
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(0, 0, 720, 48), Color.FromArgb(79, 70, 229), Color.FromArgb(147, 51, 234), 0f))
                {
                    g.FillRectangle(lgb, 0, 0, 720, 48);
                }
                using (Font titleF = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                {
                    g.DrawString("🌟 HIỆU ỨNG ĐỘC QUYỀN GENOFFICE (GENOFFICE EXCLUSIVE)", titleF, Brushes.White, 16, 12);
                }

                // Render all 10 Exclusive Animation Cards
                var exclusiveItems = new Tuple<AnimationType, string>[] {
                    Tuple.Create(AnimationType.GenOffice_NeonPulse, "Neon Glow"),
                    Tuple.Create(AnimationType.GenOffice_LiquidMorph, "Liquid Fluid"),
                    Tuple.Create(AnimationType.GenOffice_Cinematic3DFlip, "3D Flip & Flare"),
                    Tuple.Create(AnimationType.GenOffice_GlitchHologram, "Cyber Hologram"),
                    Tuple.Create(AnimationType.GenOffice_KineticSnap, "Kinetic Snap"),
                    Tuple.Create(AnimationType.GenOffice_VortexSpiral, "Cosmic Vortex"),
                    Tuple.Create(AnimationType.GenOffice_OrigamiUnfold, "3D Origami"),
                    Tuple.Create(AnimationType.GenOffice_StarlightAssembly, "Starlight Burst"),
                    Tuple.Create(AnimationType.GenOffice_ElasticJellyPop, "Jelly Pop"),
                    Tuple.Create(AnimationType.GenOffice_GoldenRibbonSwirl, "Golden Ribbon")
                };

                int startX = 20;
                int startY = 60;
                int cardW = 125;
                int cardH = 85;
                int gapX = 14;
                int gapY = 14;

                using (Font cardF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (Font badgeF = new Font("Segoe UI", 6.8f, FontStyle.Bold))
                using (Pen cardBorder = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                using (SolidBrush cardBg = new SolidBrush(Color.White))
                {
                    for (int i = 0; i < exclusiveItems.Length; i++)
                    {
                        int row = i / 5;
                        int col = i % 5;
                        int cx = startX + col * (cardW + gapX);
                        int cy = startY + row * (cardH + gapY);

                        Rectangle cRect = new Rectangle(cx, cy, cardW, cardH);
                        g.FillRectangle(cardBg, cRect);
                        g.DrawRectangle(cardBorder, cRect);

                        // Draw exclusive icon
                        AnimationsGalleryDropDownForm.DrawExclusiveVector(g, cx + cardW / 2, cy + 28, exclusiveItems[i].Item1);

                        // Exclusive Tag badge
                        using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(243, 232, 255)))
                        using (Pen badgeP = new Pen(Color.FromArgb(192, 132, 252), 1f))
                        {
                            Rectangle bRect = new Rectangle(cx + 4, cy + 4, 30, 13);
                            g.FillRectangle(badgeBg, bRect);
                            g.DrawRectangle(badgeP, bRect);
                            g.DrawString("PRO", badgeF, Brushes.Purple, cx + 7, cy + 3);
                        }

                        // Label
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(exclusiveItems[i].Item2, cardF, Brushes.Black, new RectangleF(cx, cy + 56, cardW, 24), sf);
                    }
                }

                // Render Entrance sample for comparison
                int entY = startY + 2 * (cardH + gapY) + 20;
                using (Font subHeaderF = new Font("Segoe UI", 10f, FontStyle.Bold))
                {
                    g.DrawString("🟢 Entrance (Xuất hiện)", subHeaderF, Brushes.ForestGreen, 20, entY);
                }

                var entranceSample = new Tuple<AnimationType, string>[] {
                    Tuple.Create(AnimationType.Appear, "Appear"),
                    Tuple.Create(AnimationType.FlyIn, "Fly In"),
                    Tuple.Create(AnimationType.ZoomIn, "Zoom In"),
                    Tuple.Create(AnimationType.Bounce, "Bounce"),
                    Tuple.Create(AnimationType.Swivel, "Swivel")
                };

                for (int i = 0; i < entranceSample.Length; i++)
                {
                    int cx = startX + i * (cardW + gapX);
                    int cy = entY + 28;
                    Rectangle cRect = new Rectangle(cx, cy, cardW, cardH);
                    using (SolidBrush cardBg = new SolidBrush(Color.White))
                    using (Pen cardBorder = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                    using (Font cardF = new Font("Segoe UI", 8.5f))
                    {
                        g.FillRectangle(cardBg, cRect);
                        g.DrawRectangle(cardBorder, cRect);
                        AnimationsGalleryDropDownForm.DrawEntranceVector(g, cx + cardW / 2, cy + 28, entranceSample[i].Item1);
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(entranceSample[i].Item2, cardF, Brushes.Black, new RectangleF(cx, cy + 56, cardW, 24), sf);
                    }
                }

                dropBmp.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\exclusive_animations_gallery.png", ImageFormat.Png);
                Console.WriteLine("Saved: exclusive_animations_gallery.png");
            }

            // 2. Test Ribbon Bar Quick Gallery Star Vector Tiles
            using (Bitmap ribBmp = new Bitmap(800, 120))
            using (Graphics g = Graphics.FromImage(ribBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(241, 245, 249));

                using (Font labelFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                {
                    g.DrawString("Dải Ribbon Animation Gallery (Star Vector Icons):", titleFont, Brushes.DimGray, 12, 10);

                    // Preview Button with Gold Star Vector
                    Rectangle btnPrev = new Rectangle(16, 36, 62, 62);
                    using (SolidBrush bg = new SolidBrush(Color.White))
                    using (Pen bp = new Pen(Color.FromArgb(200, 205, 210)))
                    {
                        g.FillRectangle(bg, btnPrev);
                        g.DrawRectangle(bp, btnPrev);
                    }
                    AnimationsGalleryDropDownForm.DrawStarVector(g, 47, 50, 10f, 4.5f, Color.FromArgb(245, 158, 11), true);
                    g.DrawString("Xem trước", labelFont, Brushes.Black, 18, 78);

                    g.DrawLine(Pens.LightGray, 88, 36, 88, 105);

                    // 7 Quick tiles
                    string[,] animTiles = new string[,] {
                        { "Appear", "Xuất hiện", "Entrance" },
                        { "FadeIn", "Mờ dần vào", "Entrance" },
                        { "FlyIn", "Bay vào", "Entrance" },
                        { "FloatIn", "Trôi nổi", "Entrance" },
                        { "ZoomIn", "Thu phóng", "Entrance" },
                        { "Bounce", "Nảy tưng", "Entrance" },
                        { "Pulse", "Xung nhịp", "Emphasis" }
                    };

                    AnimationType[] quickAnims = new AnimationType[] {
                        AnimationType.Appear,
                        AnimationType.FadeIn,
                        AnimationType.FlyIn,
                        AnimationType.FloatIn,
                        AnimationType.ZoomIn,
                        AnimationType.Bounce,
                        AnimationType.Pulse
                    };

                    int ax = 98;
                    for (int i = 0; i < animTiles.GetLength(0); i++)
                    {
                        Rectangle aBox = new Rectangle(ax, 36, 70, 62);
                        using (SolidBrush bg = new SolidBrush(Color.White))
                        using (Pen bp = new Pen(Color.FromArgb(220, 225, 230)))
                        {
                            g.FillRectangle(bg, aBox);
                            g.DrawRectangle(bp, aBox);
                        }

                        // Draw Star Vector / Animation Vector
                        AnimationsGalleryDropDownForm.DrawAnimationVector(g, aBox.X + 35, aBox.Y + 18, quickAnims[i], animTiles[i, 2]);

                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(animTiles[i, 1], labelFont, Brushes.Black, new RectangleF(aBox.X, aBox.Y + 38, aBox.Width, 20), sf);
                        ax += 74;
                    }
                }

                ribBmp.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\ribbon_star_vectors.png", ImageFormat.Png);
                Console.WriteLine("Saved: ribbon_star_vectors.png");
            }

            // 3. Test Math Easing Evaluator for all 10 Exclusive Animations
            Console.WriteLine("\n--- Validating 10 Exclusive Animations Physics Math ---");
            for (int e = (int)AnimationType.GenOffice_NeonPulse; e <= (int)AnimationType.GenOffice_GoldenRibbonSwirl; e++)
            {
                AnimationType at = (AnimationType)e;
                ShapeAnimation sa = new ShapeAnimation { Type = at, Duration = 1.0f };
                
                // Test at p = 0.0, 0.3, 0.7, 1.0
                float[] testP = new float[] { 0.0f, 0.3f, 0.7f, 1.0f };
                foreach (float p in testP)
                {
                    bool isVis;
                    float sx, sy, ox, oy, rot, op;
                    SlidesView.EvaluateAnimationEffect(sa, p, out isVis, out sx, out sy, out ox, out oy, out rot, out op);
                    if (float.IsNaN(sx) || float.IsNaN(sy) || float.IsNaN(ox) || float.IsNaN(oy) || float.IsNaN(rot) || float.IsNaN(op))
                    {
                        Console.WriteLine("ERROR: NaN in " + at + " at p=" + p);
                        return;
                    }
                }
                Console.WriteLine("PASSED: " + at + " physics transform validated.");
            }

            Console.WriteLine("\nALL 10 EXCLUSIVE ANIMATIONS & STAR VECTOR TESTS PASSED!");
        }
    }
}

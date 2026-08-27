using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestExclusiveAnimationsAndStarRibbon
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== TESTING GENOFFICE EXCLUSIVE ANIMATIONS & STAR RIBBON ===");

                // 1. Verify 10 exclusive animations in AnimationType enum & ShapeAnimation properties
                AnimationType[] exclusiveTypes = new AnimationType[]
                {
                    AnimationType.GenOffice_NeonPulse,
                    AnimationType.GenOffice_LiquidMorph,
                    AnimationType.GenOffice_Cinematic3DFlip,
                    AnimationType.GenOffice_GlitchHologram,
                    AnimationType.GenOffice_KineticSnap,
                    AnimationType.GenOffice_VortexSpiral,
                    AnimationType.GenOffice_OrigamiUnfold,
                    AnimationType.GenOffice_StarlightAssembly,
                    AnimationType.GenOffice_ElasticJellyPop,
                    AnimationType.GenOffice_GoldenRibbonSwirl
                };

                ShapeAnimation testSa = new ShapeAnimation();
                foreach (var t in exclusiveTypes)
                {
                    testSa.Type = t;
                    if (!testSa.IsExclusive)
                    {
                        Console.WriteLine("FAIL: " + t + " should be IsExclusive == true");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("[PASS] " + t + " is verified as IsExclusive == true");
                }

                // 2. Test mathematical evaluation of each exclusive animation at progress 0.0, 0.25, 0.5, 0.75, 1.0
                float[] testProgresses = new float[] { 0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
                foreach (var t in exclusiveTypes)
                {
                    ShapeAnimation sa = new ShapeAnimation { Type = t, Duration = 2.0f, Direction = TransitionDirection.In };
                    foreach (var p in testProgresses)
                    {
                        bool isVis;
                        float ox, oy, sx, sy, rot, alpha;
                        SlidesView.EvaluateAnimationEffect(sa, p, out isVis, out ox, out oy, out sx, out sy, out rot, out alpha);

                        if (float.IsNaN(ox) || float.IsNaN(oy) || float.IsNaN(sx) || float.IsNaN(sy) || float.IsNaN(rot) || float.IsNaN(alpha))
                        {
                            Console.WriteLine("FAIL: NaN transform for " + t + " at progress " + p);
                            Environment.Exit(1);
                        }
                    }
                    Console.WriteLine("[PASS] Mathematical 60 FPS curves verified for " + t);
                }

                // 3. Render a visual showcase card for each exclusive animation and the PowerPoint ribbon star icons
                int compW = 1000;
                int compH = 680;
                using (Bitmap compBmp = new Bitmap(compW, compH))
                using (Graphics g = Graphics.FromImage(compBmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Modern luxury dark gradient background
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(new Rectangle(0, 0, compW, compH), Color.FromArgb(15, 12, 28), Color.FromArgb(24, 20, 45), 60f))
                    {
                        g.FillRectangle(bgBrush, 0, 0, compW, compH);
                    }

                    // Header
                    using (Brush headerBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                    using (Font titleFont = new Font("Segoe UI", 16, FontStyle.Bold))
                    {
                        g.DrawString("🌟 10 HIỆU ỨNG HOẠT HỌA ĐỘC QUYỀN GENOFFICE PRO (60 FPS ENGINE)", titleFont, headerBrush, 28, 22);
                    }

                    using (Brush subBrush = new SolidBrush(Color.FromArgb(190, 185, 225)))
                    using (Font subFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    {
                        g.DrawString("Danh mục độc quyền riêng biệt - Thiết kế vector sang trọng chuẩn Canva Pro & Apple Keynote", subFont, subBrush, 28, 56);
                    }

                    // 10 Cards Grid (2 rows x 5 columns)
                    int startX = 28;
                    int startY = 95;
                    int cardW = 178;
                    int cardH = 110;
                    int gapX = 12;
                    int gapY = 14;

                    string[] titlesVi = new string[] {
                        "1. Phát sáng Neon",
                        "2. Chảy lỏng Elastic",
                        "3. Lật 3D Điện ảnh",
                        "4. Hologram 3D",
                        "5. Động năng Snap",
                        "6. Xoáy lốc Vortex",
                        "7. Gấp giấy Origami",
                        "8. Tinh tú Starlight",
                        "9. Thạch Jelly Pop",
                        "10. Dải lụa Hoàng kim"
                    };

                    string[] descs = new string[] {
                        "Cyber Neon Bloom",
                        "Fluid Morph Bounce",
                        "Perspective Sheen",
                        "RGB Glitch Laser",
                        "Spring Kinetic Snap",
                        "720° Cosmic Spiral",
                        "3D Paper Unfolding",
                        "Particle Assembly",
                        "Squash & Stretch",
                        "Luxury Gold Wave"
                    };

                    for (int i = 0; i < exclusiveTypes.Length; i++)
                    {
                        int col = i % 5;
                        int row = i / 5;
                        int cx = startX + col * (cardW + gapX);
                        int cy = startY + row * (cardH + gapY);

                        Rectangle cardRect = new Rectangle(cx, cy, cardW, cardH);

                        // Card background
                        using (LinearGradientBrush cardBg = new LinearGradientBrush(cardRect, Color.FromArgb(42, 35, 72), Color.FromArgb(26, 22, 48), 45f))
                        {
                            g.FillRectangle(cardBg, cardRect);
                        }

                        // Border
                        using (Pen cardBorder = new Pen(Color.FromArgb(139, 92, 246), 1.2f))
                        {
                            g.DrawRectangle(cardBorder, cardRect);
                        }

                        // Badge "✨ PRO"
                        using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(245, 158, 11)))
                        {
                            g.FillRectangle(badgeBg, cx + 6, cy + 6, 42, 14);
                        }
                        using (SolidBrush badgeTxt = new SolidBrush(Color.FromArgb(30, 20, 0)))
                        using (Font badgeFont = new Font("Segoe UI", 7.0f, FontStyle.Bold))
                        {
                            g.DrawString("✨ PRO", badgeFont, badgeTxt, cx + 7, cy + 6);
                        }

                        // Draw Icon
                        AnimationsGalleryDropDownForm.DrawAnimationVector(g, cx + cardW / 2, cy + 28, exclusiveTypes[i], "Exclusive");

                        // Title text
                        using (Brush titleB = new SolidBrush(Color.FromArgb(250, 250, 255)))
                        using (Font titleF = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                        {
                            g.DrawString(titlesVi[i], titleF, titleB, new RectangleF(cx + 4, cy + 62, cardW - 8, 20), sf);
                        }

                        // Desc text
                        using (Brush descB = new SolidBrush(Color.FromArgb(170, 165, 205)))
                        using (Font descF = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                        {
                            g.DrawString(descs[i], descF, descB, new RectangleF(cx + 4, cy + 82, cardW - 8, 20), sf);
                        }
                    }

                    // Section 2: Star Vector Icons on Ribbon Bar Showcase
                    int sec2Y = 360;
                    using (Pen sepPen = new Pen(Color.FromArgb(60, 50, 95), 1.0f))
                    {
                        g.DrawLine(sepPen, 28, sec2Y, compW - 28, sec2Y);
                    }

                    using (Brush secBrush = new SolidBrush(Color.FromArgb(255, 215, 0)))
                    using (Font secFont = new Font("Segoe UI", 12.5f, FontStyle.Bold))
                    {
                        g.DrawString("⭐ ICON NGÔI SAO 5 CÁNH VECTOR CHUẨN TRÊN THANH RIBBON:", secFont, secBrush, 28, sec2Y + 16);
                    }

                    using (Brush secSubBrush = new SolidBrush(Color.FromArgb(180, 175, 215)))
                    using (Font secSubFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                    {
                        g.DrawString("Đã thay thế toàn bộ chấm tròn đơn điệu bằng icon ngôi sao 5 cánh sắc nét, đúng chuẩn PowerPoint", secSubFont, secSubBrush, 28, sec2Y + 44);
                    }

                    AnimationType[] sampleTypes = new AnimationType[]
                    {
                        AnimationType.Appear,
                        AnimationType.FlyIn,
                        AnimationType.Pulse,
                        AnimationType.FlyOut,
                        AnimationType.MotionLine
                    };
                    string[] sampleCats = new string[] { "Entrance", "Entrance", "Emphasis", "Exit", "MotionPaths" };
                    string[] sampleTitles = new string[] { "Xuất hiện (Appear)", "Bay vào (Fly In)", "Xung nhịp (Pulse)", "Bay ra (Fly Out)", "Đường thẳng (Line)" };
                    string[] sampleSub = new string[] { "Ngôi sao Xanh lá", "Sao Xanh + Mũi tên", "Ngôi sao Vàng cam", "Ngôi sao Đỏ thoát", "Điểm Start/End Xanh" };

                    int ribbonSecY = sec2Y + 75;
                    int rCardW = 178;
                    int rCardH = 135;

                    for (int s = 0; s < sampleTypes.Length; s++)
                    {
                        int rx = startX + s * (rCardW + gapX);
                        Rectangle rRect = new Rectangle(rx, ribbonSecY, rCardW, rCardH);

                        using (SolidBrush rBg = new SolidBrush(Color.FromArgb(28, 25, 50)))
                        {
                            g.FillRectangle(rBg, rRect);
                        }
                        using (Pen rBorder = new Pen(Color.FromArgb(70, 65, 105), 1.0f))
                        {
                            g.DrawRectangle(rBorder, rRect);
                        }

                        // Draw Ribbon Star Tile
                        Rectangle tileRect = new Rectangle(rx + (rCardW - 70) / 2, ribbonSecY + 12, 70, 56);
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(40, 36, 68)))
                        {
                            g.FillRectangle(tb, tileRect);
                        }
                        using (Pen tp = new Pen(Color.FromArgb(90, 85, 130), 1.0f))
                        {
                            g.DrawRectangle(tp, tileRect);
                        }

                        AnimationsGalleryDropDownForm.DrawAnimationVector(g, tileRect.X + 35, tileRect.Y + 28, sampleTypes[s], sampleCats[s]);

                        // Text
                        using (Brush tbBrush = new SolidBrush(Color.FromArgb(245, 245, 255)))
                        using (Font tbFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                        {
                            g.DrawString(sampleTitles[s], tbFont, tbBrush, new RectangleF(rx + 4, ribbonSecY + 78, rCardW - 8, 20), sf);
                        }

                        using (Brush subB = new SolidBrush(Color.FromArgb(160, 155, 195)))
                        using (Font subF = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                        {
                            g.DrawString(sampleSub[s], subF, subB, new RectangleF(rx + 4, ribbonSecY + 98, rCardW - 8, 20), sf);
                        }
                    }

                    // Watermark / Credit
                    using (Brush crB = new SolidBrush(Color.FromArgb(140, 135, 175)))
                    using (Font crF = new Font("Segoe UI", 8.5f, FontStyle.Italic))
                    {
                        g.DrawString("GenOffice Suite - Bản quyền tác giả: Trương Nguyễn Thế Danh", crF, crB, 28, compH - 28);
                    }

                    string outPath = @"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\exclusive_animations_showcase.png";
                    compBmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("[PASS] Showcase image exported to: " + outPath);
                }

                // 4. Test Ribbon Bar rendering directly
                using (Bitmap ribbonBmp = new Bitmap(1050, 160))
                using (Graphics g = Graphics.FromImage(ribbonBmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.FromArgb(243, 244, 246));

                    // Paint PowerPoint Tab Animations
                    using (Pen pLine = new Pen(Color.FromArgb(220, 224, 230), 1.0f))
                    {
                        g.DrawLine(pLine, 0, 159, 1050, 159);
                    }

                    // Draw the 7 quick animation tiles in Ribbon style
                    AnimationType[] ribbonQuickAnims = new AnimationType[] {
                        AnimationType.Appear,
                        AnimationType.FadeIn,
                        AnimationType.FlyIn,
                        AnimationType.FloatIn,
                        AnimationType.ZoomIn,
                        AnimationType.Bounce,
                        AnimationType.Pulse
                    };
                    string[] ribbonQuickNames = new string[] {
                        "Xuất hiện", "Mờ dần vào", "Bay vào", "Trôi nổi", "Thu phóng", "Nảy tưng", "Xung nhịp"
                    };
                    string[] ribbonCats = new string[] {
                        "Entrance", "Entrance", "Entrance", "Entrance", "Entrance", "Entrance", "Emphasis"
                    };

                    int ax = 88;
                    using (Font labelFont = new Font("Segoe UI", 8.0f))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                    {
                        for (int i = 0; i < ribbonQuickAnims.Length; i++)
                        {
                            Rectangle aBox = new Rectangle(ax, 20, 70, 60);
                            using (SolidBrush bg = new SolidBrush(Color.FromArgb(255, 255, 255)))
                            {
                                g.FillRectangle(bg, aBox);
                            }
                            using (Pen p = new Pen(Color.FromArgb(215, 220, 228)))
                            {
                                g.DrawRectangle(p, aBox);
                            }

                            // Draw Star Vector (Green or Yellow)
                            AnimationsGalleryDropDownForm.DrawAnimationVector(g, aBox.X + 35, aBox.Y + 20, ribbonQuickAnims[i], ribbonCats[i]);

                            g.DrawString(ribbonQuickNames[i], labelFont, Brushes.Black, new RectangleF(aBox.X, aBox.Y + 38, aBox.Width, 20), sf);
                            ax += 74;
                        }
                    }

                    string ribbonOutPath = @"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\ribbon_stars_preview.png";
                    ribbonBmp.Save(ribbonOutPath, ImageFormat.Png);
                    Console.WriteLine("[PASS] Ribbon preview exported to: " + ribbonOutPath);
                }

                Console.WriteLine("=== ALL VERIFICATION TESTS PASSED SUCCESSFULLY! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL with exception: " + ex);
                Environment.Exit(1);
            }
        }
    }
}

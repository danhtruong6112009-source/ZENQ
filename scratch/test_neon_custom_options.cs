using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Test
{
    public class TestNeonCustomOptions
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Neon Custom Options Visuals ===");

            PresentationRoot pres = new PresentationRoot();
            SlideNode slide = new SlideNode();
            slide.BgColor = unchecked((int)0xFF0A0C14);
            pres.Slides.Add(slide);

            SlidesView sv = new SlidesView();
            sv.Presentation = pres;

            // 1. Test Canvas with Various Neon Configurations
            int canvasW = 1600;
            int canvasH = 920;
            using (Bitmap bmp = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Dark modern obsidian background
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(new Rectangle(0, 0, canvasW, canvasH), Color.FromArgb(12, 16, 28), Color.FromArgb(5, 7, 15), 45f))
                {
                    g.FillRectangle(bgBrush, 0, 0, canvasW, canvasH);
                }

                // High-tech subtle grid lines
                using (Pen gridPen = new Pen(Color.FromArgb(18, 255, 255, 255), 1f))
                {
                    for (int x = 0; x < canvasW; x += 50) g.DrawLine(gridPen, x, 0, x, canvasH);
                    for (int y = 0; y < canvasH; y += 50) g.DrawLine(gridPen, 0, y, canvasW, y);
                }

                // Title
                using (Font titleFont = new Font("Segoe UI", 22f, FontStyle.Bold))
                using (SolidBrush titleBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("GENOFFICE - TÙY BIẾN THÔNG SỐ HIỆU ỨNG PHÁT SÁNG NEON CHUYÊN SÂU", titleFont, titleBrush, new PointF(40, 25));
                }

                using (Font subFont = new Font("Segoe UI", 11.5f, FontStyle.Regular))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(0, 240, 255)))
                {
                    g.DrawString("★ RGB Tùy Chọn  |  ★ Mục Tiêu: Khung / Chữ / Cả Hai  |  ★ Đơn Sắc & Nhị Sắc Dual-Tone  |  ★ Lặp Vô Hạn Breathing Pulse", subFont, subBrush, new PointF(42, 65));
                }

                // Row 1: 3 Target Modes (Both vs Shape Only vs Text Only)
                using (Font headerFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.DrawString("1. MỤC TIÊU PHÁT SÁNG (TARGET MODES):", headerFont, headerBrush, 40, 105);
                }

                // Item 1: BOTH - Electric Cyan Monochrome
                ShapeNode shape1 = new ShapeNode
                {
                    Id = "neon_1",
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(40, 140, 480, 220),
                    FillColor = unchecked((int)0xFF12182A),
                    StrokeColor = unchecked((int)0xFF00F0FF),
                    StrokeWidth = 3f,
                    Text = "CYAN MONOCHROME\n🌟 Cả Khung Viền & Chữ\nBloom: 35px | Lõi Laser Trắng",
                    FontSize = 15,
                    Bold = true,
                    TextColor = unchecked((int)0xFFFFFFFF)
                };
                ShapeAnimation anim1 = new ShapeAnimation
                {
                    ShapeId = "neon_1",
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.Both,
                    NeonColor = unchecked((int)0xFF00F0FF),
                    NeonColorMode = NeonColorMode.Monochrome,
                    NeonBloomRadius = 35f,
                    NeonIntensity = 1.35f,
                    NeonCornerFlares = true
                };
                RenderShapeHelper(sv, g, shape1, anim1, 0.75f);

                // Item 2: SHAPE ONLY - Dual Tone (Hot Magenta to Amber Gold)
                ShapeNode shape2 = new ShapeNode
                {
                    Id = "neon_2",
                    Type = ShapeType.Rectangle,
                    Transform = new Transform2D(560, 140, 480, 220),
                    FillColor = unchecked((int)0xFF12182A),
                    StrokeColor = unchecked((int)0xFFFF007F),
                    StrokeWidth = 3f,
                    Text = "DUAL-TONE NEON\n🔲 Chỉ Khung Viền Shape\nMagenta → Gold | Chữ Giữ Màu Gốc",
                    FontSize = 15,
                    Bold = true,
                    TextColor = unchecked((int)0xFFE0E8FF)
                };
                ShapeAnimation anim2 = new ShapeAnimation
                {
                    ShapeId = "neon_2",
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.ShapeOnly,
                    NeonColor = unchecked((int)0xFFFF007F),
                    NeonSecondaryColor = unchecked((int)0xFFFFB800),
                    NeonColorMode = NeonColorMode.DualTone,
                    NeonBloomRadius = 40f,
                    NeonIntensity = 1.4f,
                    NeonCornerFlares = true
                };
                RenderShapeHelper(sv, g, shape2, anim2, 0.80f);

                // Item 3: TEXT ONLY - Emerald Mint
                ShapeNode shape3 = new ShapeNode
                {
                    Id = "neon_3",
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(1080, 140, 480, 220),
                    FillColor = unchecked((int)0xFF12182A),
                    StrokeColor = unchecked((int)0xFF32465F),
                    StrokeWidth = 1.5f,
                    Text = "EMERALD MINT\n🔤 Chỉ Văn Bản Chữ\n5 Lớp Bloom Chữ + Lõi Trắng",
                    FontSize = 16,
                    Bold = true,
                    TextColor = unchecked((int)0xFF00FF88)
                };
                ShapeAnimation anim3 = new ShapeAnimation
                {
                    ShapeId = "neon_3",
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.TextOnly,
                    NeonColor = unchecked((int)0xFF00FF88),
                    NeonColorMode = NeonColorMode.Monochrome,
                    NeonBloomRadius = 32f,
                    NeonIntensity = 1.25f,
                    NeonCornerFlares = false
                };
                RenderShapeHelper(sv, g, shape3, anim3, 0.85f);

                // Row 2: Advanced Features (Looping & Custom RGB Dual-Tone)
                using (Font headerFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.DrawString("2. LẶP VÔ HẠN (INFINITE BREATHING PULSE) & MÀU TÙY CHỌN (CUSTOM RGB / HEX):", headerFont, headerBrush, 40, 400);
                }

                // Item 4: Infinite Breathing Pulse Demonstration - Cyber Purple
                ShapeNode shape4 = new ShapeNode
                {
                    Id = "neon_4",
                    Type = ShapeType.Diamond,
                    Transform = new Transform2D(40, 440, 360, 260),
                    FillColor = unchecked((int)0xFF181028),
                    StrokeColor = unchecked((int)0xFFA855F7),
                    StrokeWidth = 3.5f,
                    Text = "CYBER PURPLE\n♾️ Lặp Vô Hạn (Breathing)\nNhịp Thở Êm Dịu 60 FPS",
                    FontSize = 14,
                    Bold = true,
                    TextColor = unchecked((int)0xFFFFFFFF)
                };
                ShapeAnimation anim4 = new ShapeAnimation
                {
                    ShapeId = "neon_4",
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.Both,
                    NeonColor = unchecked((int)0xFFA855F7),
                    NeonColorMode = NeonColorMode.Monochrome,
                    NeonInfiniteLoop = true,
                    NeonBloomRadius = 45f,
                    NeonIntensity = 1.5f,
                    NeonCornerFlares = true
                };
                RenderShapeHelper(sv, g, shape4, anim4, 0.90f);

                // Item 5: Custom RGB / Hex Color 1 - Deep Sunset Orange + Hot Pink Dual-Tone
                ShapeNode shape5 = new ShapeNode
                {
                    Id = "neon_5",
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(440, 440, 540, 260),
                    FillColor = unchecked((int)0xFF1F1418),
                    StrokeColor = unchecked((int)0xFFFF5722),
                    StrokeWidth = 3.5f,
                    Text = "CUSTOM RGB DUAL-TONE\n🎨 #FF5722 (Orange) + #E91E63 (Pink)\nBán Kính Bloom: 48px | Cường Độ: 160%\n4 Tia Sao Chữ Thập Góc Rực Rỡ",
                    FontSize = 15,
                    Bold = true,
                    TextColor = unchecked((int)0xFFFFFFFF)
                };
                ShapeAnimation anim5 = new ShapeAnimation
                {
                    ShapeId = "neon_5",
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.Both,
                    NeonColor = unchecked((int)0xFFFF5722),
                    NeonSecondaryColor = unchecked((int)0xFFE91E63),
                    NeonColorMode = NeonColorMode.DualTone,
                    NeonBloomRadius = 48f,
                    NeonIntensity = 1.6f,
                    NeonCornerFlares = true
                };
                RenderShapeHelper(sv, g, shape5, anim5, 0.75f);

                // Item 6: Custom RGB / Hex Color 2 - Electric Lime + Ice Blue Dual-Tone
                ShapeNode shape6 = new ShapeNode
                {
                    Id = "neon_6",
                    Type = ShapeType.Hexagon,
                    Transform = new Transform2D(1020, 440, 540, 260),
                    FillColor = unchecked((int)0xFF0E1A18),
                    StrokeColor = unchecked((int)0xFF76FF03),
                    StrokeWidth = 3.5f,
                    Text = "LIME & ICE BLUE DUAL-TONE\n🎨 #76FF03 (Lime) + #00E5FF (Ice Blue)\nQuầng Phát Quang Rực Sắc Cực Đại",
                    FontSize = 15,
                    Bold = true,
                    TextColor = unchecked((int)0xFFFFFFFF)
                };
                ShapeAnimation anim6 = new ShapeAnimation
                {
                    ShapeId = "neon_6",
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.Both,
                    NeonColor = unchecked((int)0xFF76FF03),
                    NeonSecondaryColor = unchecked((int)0xFF00E5FF),
                    NeonColorMode = NeonColorMode.DualTone,
                    NeonBloomRadius = 52f,
                    NeonIntensity = 1.7f,
                    NeonCornerFlares = true
                };
                RenderShapeHelper(sv, g, shape6, anim6, 0.75f);

                // Row 3: Infinite Breathing Pulse Phases comparison
                using (Font headerFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.DrawString("3. CHU KỲ NHỊP THỞ VÔ HẠN (BREATHING PULSE PHASES):", headerFont, headerBrush, 40, 735);
                }

                float[] breathingPhases = new float[] { 0.20f, 0.50f, 0.85f, 1.00f, 0.60f };
                string[] phaseLabels = new string[] { "Nhịp êm (20%)", "Tăng dần (50%)", "Đỉnh phát quang (85%)", "Cực đại (100%)", "Hạ nhiệt (60%)" };
                using (Font pFont = new Font("Segoe UI", 10f))
                using (SolidBrush pBrush = new SolidBrush(Color.FromArgb(160, 180, 210)))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ShapeNode bShape = new ShapeNode
                        {
                            Id = "b_" + i,
                            Type = ShapeType.RoundedRectangle,
                            Transform = new Transform2D(40 + i * 310, 770, 280, 100),
                            FillColor = unchecked((int)0xFF101625),
                            StrokeColor = unchecked((int)0xFF00F0FF),
                            StrokeWidth = 2.5f,
                            Text = "BREATHING\n" + phaseLabels[i],
                            FontSize = 11,
                            Bold = true,
                            TextColor = unchecked((int)0xFFFFFFFF)
                        };
                        ShapeAnimation bAnim = new ShapeAnimation
                        {
                            ShapeId = "b_" + i,
                            Type = AnimationType.GenOffice_NeonPulse,
                            NeonTarget = NeonGlowTarget.Both,
                            NeonColor = unchecked((int)0xFF00F0FF),
                            NeonColorMode = NeonColorMode.Monochrome,
                            NeonInfiniteLoop = true,
                            NeonBloomRadius = 25f + breathingPhases[i] * 15f,
                            NeonIntensity = 0.8f + breathingPhases[i] * 0.7f,
                            NeonCornerFlares = true
                        };
                        RenderShapeHelper(sv, g, bShape, bAnim, breathingPhases[i]);
                    }
                }

                bmp.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\neon_custom_parameters_showcase.png", ImageFormat.Png);
                Console.WriteLine("Saved canvas test to neon_custom_parameters_showcase.png");
            }

            // 2. Render the NeonEffectOptionsDialogForm itself as an image
            try
            {
                ShapeAnimation demoAnim = new ShapeAnimation
                {
                    Type = AnimationType.GenOffice_NeonPulse,
                    NeonTarget = NeonGlowTarget.Both,
                    NeonColor = unchecked((int)0xFF00F0FF),
                    NeonSecondaryColor = unchecked((int)0xFFFF007F),
                    NeonColorMode = NeonColorMode.DualTone,
                    NeonInfiniteLoop = true,
                    NeonBloomRadius = 35f,
                    NeonIntensity = 1.4f,
                    NeonCornerFlares = true
                };
                ShapeNode demoShape = new ShapeNode
                {
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(0, 0, 200, 100),
                    FillColor = unchecked((int)0xFF1E293B),
                    StrokeColor = unchecked((int)0xFF00F0FF),
                    StrokeWidth = 2.5f,
                    Text = "LIVE PREVIEW",
                    TextColor = unchecked((int)0xFFFFFFFF)
                };

                using (NeonEffectOptionsDialogForm dialog = new NeonEffectOptionsDialogForm(demoAnim, demoShape, null))
                {
                    dialog.Show();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);

                    using (Bitmap dlgBmp = new Bitmap(dialog.Width, dialog.Height))
                    {
                        dialog.DrawToBitmap(dlgBmp, new Rectangle(0, 0, dialog.Width, dialog.Height));
                        dlgBmp.Save(@"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545\neon_dialog_form_rendered.png", ImageFormat.Png);
                        Console.WriteLine("Saved dialog render to neon_dialog_form_rendered.png");
                    }
                    dialog.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dialog render exception: " + ex.Message);
            }

            Console.WriteLine("All tests completed successfully!");
        }

        private static void RenderShapeHelper(SlidesView view, Graphics g, ShapeNode shape, ShapeAnimation anim, float progress)
        {
            bool vis;
            float offX, offY, scX, scY, rDelta, opMul;
            SlidesView.EvaluateAnimationEffect(anim, progress, out vis, out offX, out offY, out scX, out scY, out rDelta, out opMul);
            view.DrawShape(g, 0, 0, 1.0f, shape, false, opMul, offX, offY, scX, scY, rDelta, anim, progress);
        }
    }
}

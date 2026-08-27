using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TestHumanCoffee
{
    class Program
    {
        static void Main()
        {
            try
            {
                int width = 580;
                int height = 320;
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    g.Clear(Color.White);

                    // Draw chic human coffee vector at right: X=455, Y=110
                    DrawHumanCoffeeVector(g, 452, 110, 1.0f, 1.6f);

                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\test_human_coffee3.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SAVED: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }

        public static void DrawHumanCoffeeVector(Graphics g, float cx, float cy, float scale, float timeSec)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(scale, scale);

            // 1. Soft Pastel Circle Backdrop
            using (GraphicsPath circle = new GraphicsPath())
            using (SolidBrush circleBrush = new SolidBrush(Color.FromArgb(243, 246, 252)))
            {
                circle.AddEllipse(-65, -55, 130, 130);
                g.FillPath(circleBrush, circle);
            }

            // Accent floating sparkles & warm coffee beans
            float sp1 = (float)(0.6 + 0.4 * Math.Sin(timeSec * 3.2));
            DrawStar(g, -50, -42, 6.5f * sp1, Color.FromArgb(245, 158, 11)); // Amber star
            DrawStar(g, 56, -36, 6.0f * (1.2f - 0.4f * sp1), Color.FromArgb(59, 130, 246)); // Blue star

            // Floating cute coffee bean accent at top right
            using (SolidBrush beanBrush = new SolidBrush(Color.FromArgb(180, 120, 53, 15)))
            {
                g.FillEllipse(beanBrush, 46, -18, 10, 14);
            }

            // 2. Character Torso / Sweater (Warm Blue #3B82F6 & Royal Blue #2563EB)
            using (GraphicsPath torso = new GraphicsPath())
            using (LinearGradientBrush torsoBrush = new LinearGradientBrush(new RectangleF(-45, 15, 90, 60), Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), 90f))
            {
                torso.AddArc(-38, 14, 76, 68, 180, 180);
                torso.AddLine(38, 48, 38, 65);
                torso.AddLine(38, 65, -38, 65);
                torso.AddLine(-38, 65, -38, 48);
                torso.CloseFigure();
                g.FillPath(torsoBrush, torso);
            }

            // Collar
            using (SolidBrush collar = new SolidBrush(Color.White))
            {
                g.FillPie(collar, -14, 10, 28, 16, 0, 180);
            }

            // 3. Neck & Head (Warm Friendly Skin #FED7AA / #FDBA74)
            using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                // Neck
                g.FillRectangle(skin, -8, 6, 16, 14);

                // Head
                g.FillEllipse(skin, -24, -40, 48, 48);

                // Ears
                g.FillEllipse(skin, -27, -22, 9, 12);
                g.FillEllipse(skin, 18, -22, 9, 12);
            }

            // 4. Chic Modern Hairstyle (Dark Navy #0F172A)
            using (GraphicsPath hair = new GraphicsPath())
            using (SolidBrush hairBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                hair.AddArc(-26, -46, 52, 44, 160, 220);
                hair.AddCurve(new PointF[] {
                    new PointF(24, -22),
                    new PointF(16, -32),
                    new PointF(2, -28),
                    new PointF(-14, -34),
                    new PointF(-24, -20)
                });
                hair.CloseFigure();
                g.FillPath(hairBrush, hair);

                // Stylish hair swoosh
                g.FillEllipse(hairBrush, -16, -48, 26, 16);
            }

            // 5. Friendly Face with Stylish Round Glasses
            using (Pen glassPen = new Pen(Color.FromArgb(30, 41, 59), 2.0f))
            using (SolidBrush blush = new SolidBrush(Color.FromArgb(100, 244, 63, 94)))
            using (Pen eyePen = new Pen(Color.FromArgb(30, 41, 59), 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // Round Glasses Frames
                g.DrawEllipse(glassPen, -18, -26, 15, 15);
                g.DrawEllipse(glassPen, 3, -26, 15, 15);
                g.DrawLine(glassPen, -3, -18, 3, -18); // Bridge

                // Happy closed curved eyes behind glasses (^_^)
                g.DrawArc(eyePen, -15, -20, 9, 7, 190, 160);
                g.DrawArc(eyePen, 6, -20, 9, 7, 190, 160);

                // Cheeks
                g.FillEllipse(blush, -22, -12, 8, 5);
                g.FillEllipse(blush, 14, -12, 8, 5);

                // Small cheerful smile
                g.DrawArc(eyePen, -5, -12, 10, 8, 20, 140);
            }

            // 6. Right Arm Holding Coffee Mug Towards Center-Right
            using (Pen armPen = new Pen(Color.FromArgb(37, 99, 235), 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // Left arm resting
                g.DrawCurve(armPen, new PointF[] {
                    new PointF(-28, 24),
                    new PointF(-18, 42),
                    new PointF(-4, 46)
                });
                // Right arm holding mug up
                g.DrawCurve(armPen, new PointF[] {
                    new PointF(28, 24),
                    new PointF(18, 34),
                    new PointF(8, 28)
                });
            }

            // Hands
            using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                g.FillEllipse(skin, -8, 38, 11, 11);
                g.FillEllipse(skin, 4, 24, 11, 11);
            }

            // 7. Ceramic Coffee Mug (Sunny Warm Amber #F59E0B with Heart Badge)
            float mugX = 2;
            float mugY = 16;
            float mugW = 26;
            float mugH = 24;

            // Handle
            using (Pen handlePen = new Pen(Color.FromArgb(245, 158, 11), 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(handlePen, mugX + mugW - 3, mugY + 4, 11, 14, 280, 160);
            }

            // Cup Body
            using (GraphicsPath cupPath = new GraphicsPath())
            using (LinearGradientBrush cupBrush = new LinearGradientBrush(new RectangleF(mugX, mugY, mugW, mugH), Color.FromArgb(251, 191, 36), Color.FromArgb(245, 158, 11), 90f))
            {
                cupPath.AddArc(mugX, mugY + mugH - 10, 10, 10, 90, 90);
                cupPath.AddArc(mugX + mugW - 10, mugY + mugH - 10, 10, 10, 0, 90);
                cupPath.AddLine(mugX + mugW, mugY, mugX, mugY);
                cupPath.CloseFigure();
                g.FillPath(cupBrush, cupPath);

                // Cute White Heart on mug
                using (SolidBrush heartB = new SolidBrush(Color.White))
                {
                    float hx = mugX + 8.5f;
                    float hy = mugY + 8f;
                    g.FillEllipse(heartB, hx, hy, 4.5f, 4.5f);
                    g.FillEllipse(heartB, hx + 4.5f, hy, 4.5f, 4.5f);
                    g.FillPolygon(heartB, new PointF[] {
                        new PointF(hx - 0.5f, hy + 2.5f),
                        new PointF(hx + 9.5f, hy + 2.5f),
                        new PointF(hx + 4.5f, hy + 8f)
                    });
                }
            }

            // Coffee Surface (Rich coffee brown)
            using (SolidBrush coffee = new SolidBrush(Color.FromArgb(120, 53, 15)))
            {
                g.FillEllipse(coffee, mugX + 2, mugY - 2, mugW - 4, 6);
            }

            // 8. ANIMATED STEAM RISING (Khói cà phê bốc lên uốn lượn 60 FPS)
            DrawAnimatedSteam(g, mugX + mugW / 2f, mugY - 4, timeSec);

            g.Restore(state);
        }

        private static void DrawAnimatedSteam(Graphics g, float originX, float originY, float timeSec)
        {
            float[] offsets = new float[] { -5f, 0f, 6f };
            float[] speedMults = new float[] { 0.9f, 1.15f, 0.85f };
            float[] phaseShifts = new float[] { 0f, 2.1f, 4.2f };

            for (int i = 0; i < 3; i++)
            {
                float t = timeSec * 2.4f * speedMults[i] + phaseShifts[i];
                float heightRatio = (t % 3.0f) / 3.0f; // 0.0 to 1.0 cycle

                float alpha = (float)(Math.Sin(heightRatio * Math.PI) * 0.8);
                if (alpha <= 0.02f) continue;

                float baseY = originY - (heightRatio * 36f);
                float baseX = originX + offsets[i];
                float wave = (float)Math.Sin(t * 3.2f) * 4.0f;

                using (GraphicsPath steam = new GraphicsPath())
                using (Pen steamPen = new Pen(Color.FromArgb((int)(alpha * 255), 148, 163, 184), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    PointF p0 = new PointF(baseX, baseY);
                    PointF p1 = new PointF(baseX + wave, baseY - 8);
                    PointF p2 = new PointF(baseX - wave, baseY - 18);
                    PointF p3 = new PointF(baseX + wave * 0.6f, baseY - 26);

                    steam.AddBezier(p0, p1, p2, p3);
                    g.DrawPath(steamPen, steam);
                }
            }
        }

        private static void DrawStar(Graphics g, float x, float y, float size, Color color)
        {
            if (size <= 0.5f) return;
            using (SolidBrush b = new SolidBrush(color))
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] {
                    new PointF(x, y - size),
                    new PointF(x + size * 0.28f, y - size * 0.28f),
                    new PointF(x + size, y),
                    new PointF(x + size * 0.28f, y + size * 0.28f),
                    new PointF(x, y + size),
                    new PointF(x - size * 0.28f, y + size * 0.28f),
                    new PointF(x - size, y),
                    new PointF(x - size * 0.28f, y - size * 0.28f)
                });
                g.FillPath(b, p);
            }
        }
    }
}

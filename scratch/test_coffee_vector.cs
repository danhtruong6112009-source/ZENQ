using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TestCoffee
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

                    // Draw coffee character vector at right: X=450, Y=110
                    DrawGoogleCoffeeVector(g, 455, 112, 1.0f, 1.6f);

                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\test_coffee_vector2.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SAVED: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }

        public static void DrawGoogleCoffeeVector(Graphics g, float cx, float cy, float scale, float timeSec)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(scale, scale);

            // 1. Soft Pastel Cloud Backdrop (Google Doodle light gray/blue cloud)
            using (GraphicsPath cloud = new GraphicsPath())
            using (SolidBrush cloudBrush = new SolidBrush(Color.FromArgb(243, 246, 252)))
            {
                cloud.AddEllipse(-75, -55, 60, 50);
                cloud.AddEllipse(-35, -75, 75, 65);
                cloud.AddEllipse(25, -60, 60, 55);
                cloud.AddEllipse(-65, -25, 145, 80);
                g.FillPath(cloudBrush, cloud);
            }

            // Floating cute yellow stars
            float sp1 = (float)(0.6 + 0.4 * Math.Sin(timeSec * 3.5));
            DrawStar(g, -60, -50, 7f * sp1, Color.FromArgb(251, 188, 5));
            DrawStar(g, 65, -35, 6f * (1.2f - 0.4f * sp1), Color.FromArgb(66, 133, 244));

            // 2. Character Body & Spikes / Back (Google Blue #4285F4 & Royal Duo-Tone #2563EB)
            using (SolidBrush darkBlue = new SolidBrush(Color.FromArgb(29, 78, 216)))
            {
                // Soft spikes / hair tufts along head/back
                g.FillEllipse(darkBlue, -48, -48, 18, 18);
                g.FillEllipse(darkBlue, -56, -30, 18, 18);
                g.FillEllipse(darkBlue, -58, -10, 18, 18);
            }

            // Main Body Shape (Cute rounded body)
            using (GraphicsPath bodyPath = new GraphicsPath())
            using (SolidBrush bodyBrush = new SolidBrush(Color.FromArgb(59, 130, 246))) // Friendly Vibrant Blue #3B82F6
            {
                bodyPath.AddArc(-50, -45, 85, 95, 90, 180);
                bodyPath.AddCurve(new PointF[] {
                    new PointF(-7, -45),
                    new PointF(25, -35),
                    new PointF(38, -10),
                    new PointF(22, 18),
                    new PointF(8, 38),
                    new PointF(-15, 50),
                    new PointF(-50, 50)
                });
                bodyPath.CloseFigure();
                g.FillPath(bodyBrush, bodyPath);
            }

            // Belly Duo-tone Accent (Sky Blue #60A5FA)
            using (GraphicsPath belly = new GraphicsPath())
            using (SolidBrush bellyBrush = new SolidBrush(Color.FromArgb(96, 165, 250)))
            {
                belly.AddCurve(new PointF[] {
                    new PointF(-35, 48),
                    new PointF(-20, 20),
                    new PointF(0, 15),
                    new PointF(15, 30),
                    new PointF(5, 48)
                });
                belly.CloseFigure();
                g.FillPath(bellyBrush, belly);
            }

            // 3. Head & Snout (Extended charming Google Doodle snout)
            using (GraphicsPath snout = new GraphicsPath())
            using (SolidBrush snoutBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
            {
                snout.AddEllipse(0, -32, 42, 34);
                g.FillPath(snoutBrush, snout);
            }

            // Cute black button nose
            using (SolidBrush nose = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillEllipse(nose, 30, -28, 11, 11);
                // Tiny nose catchlight
                g.FillEllipse(Brushes.White, 32, -26, 3.5f, 3.5f);
            }

            // Happy Smile Arc
            using (Pen mouthPen = new Pen(Color.FromArgb(15, 23, 42), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(mouthPen, 14, -18, 16, 12, 20, 140);
            }

            // 4. Large Expressive Google Eye
            // Outer Big White Sclera
            using (SolidBrush eyeWhite = new SolidBrush(Color.White))
            using (SolidBrush pupil = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillEllipse(eyeWhite, -4, -38, 26, 32);

                // Pupil with gaze looking warmly at the coffee
                g.FillEllipse(pupil, 4, -28, 16, 20);
                // Big white highlight
                g.FillEllipse(eyeWhite, 6, -26, 6, 7);
                // Small sub-highlight
                g.FillEllipse(eyeWhite, 12, -16, 3, 3);
            }

            // Pink Blush on cheek
            using (SolidBrush blush = new SolidBrush(Color.FromArgb(90, 244, 63, 94)))
            {
                g.FillEllipse(blush, -6, -10, 12, 8);
            }

            // 5. Holding the Warm Coffee Mug with Both Hands
            // Left Arm
            using (Pen armPen = new Pen(Color.FromArgb(37, 99, 235), 8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawCurve(armPen, new PointF[] {
                    new PointF(-20, 10),
                    new PointF(-5, 24),
                    new PointF(10, 22)
                });
            }

            // Coffee Mug (Sunny Warm Amber/Yellow #F59E0B with White ceramic rim)
            float mugX = 6;
            float mugY = 12;
            float mugW = 28;
            float mugH = 26;

            // Mug Handle
            using (Pen handlePen = new Pen(Color.FromArgb(245, 158, 11), 4.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(handlePen, mugX + mugW - 4, mugY + 5, 12, 15, 280, 160);
            }

            // Mug Body (Rounded bottom cup)
            using (GraphicsPath cupPath = new GraphicsPath())
            using (LinearGradientBrush cupBrush = new LinearGradientBrush(new RectangleF(mugX, mugY, mugW, mugH), Color.FromArgb(251, 191, 36), Color.FromArgb(245, 158, 11), 90f))
            {
                cupPath.AddArc(mugX, mugY + mugH - 12, 12, 12, 90, 90);
                cupPath.AddArc(mugX + mugW - 12, mugY + mugH - 12, 12, 12, 0, 90);
                cupPath.AddLine(mugX + mugW, mugY, mugX, mugY);
                cupPath.CloseFigure();
                g.FillPath(cupBrush, cupPath);

                // Cute White Heart badge on mug
                using (SolidBrush heartB = new SolidBrush(Color.White))
                {
                    float hx = mugX + 9;
                    float hy = mugY + 9;
                    g.FillEllipse(heartB, hx, hy, 5, 5);
                    g.FillEllipse(heartB, hx + 5, hy, 5, 5);
                    g.FillPolygon(heartB, new PointF[] {
                        new PointF(hx - 0.5f, hy + 2.5f),
                        new PointF(hx + 10.5f, hy + 2.5f),
                        new PointF(hx + 5f, hy + 9f)
                    });
                }
            }

            // Coffee Surface (Rich dark coffee)
            using (SolidBrush coffee = new SolidBrush(Color.FromArgb(113, 63, 18)))
            {
                g.FillEllipse(coffee, mugX + 2, mugY - 2, mugW - 4, 7);
            }

            // Right Paws/Hands holding the cup
            using (SolidBrush pawBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
            {
                g.FillEllipse(pawBrush, mugX - 4, mugY + 8, 10, 10);
                g.FillEllipse(pawBrush, mugX + mugW - 6, mugY + 8, 10, 10);
            }

            // 6. ANIMATED STEAM RISING (Khói bốc lên uốn lượn 60 FPS)
            DrawAnimatedSteam(g, mugX + mugW / 2f, mugY - 3, timeSec);

            g.Restore(state);
        }

        private static void DrawAnimatedSteam(Graphics g, float originX, float originY, float timeSec)
        {
            // 3 Wisps of steam undulating and rising
            float[] offsets = new float[] { -6f, 0f, 7f };
            float[] speedMults = new float[] { 0.9f, 1.15f, 0.85f };
            float[] phaseShifts = new float[] { 0f, 2.1f, 4.2f };

            for (int i = 0; i < 3; i++)
            {
                float t = timeSec * 2.4f * speedMults[i] + phaseShifts[i];
                float heightRatio = (t % 3.0f) / 3.0f; // 0.0 to 1.0 cycle

                // Alpha curve: fades in, reaches peak, then fades out at top
                float alpha = (float)(Math.Sin(heightRatio * Math.PI) * 0.8);
                if (alpha <= 0.02f) continue;

                float baseY = originY - (heightRatio * 38f);
                float baseX = originX + offsets[i];
                float wave = (float)Math.Sin(t * 3.2f) * 4.5f;

                using (GraphicsPath steam = new GraphicsPath())
                using (Pen steamPen = new Pen(Color.FromArgb((int)(alpha * 255), 148, 163, 184), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    PointF p0 = new PointF(baseX, baseY);
                    PointF p1 = new PointF(baseX + wave, baseY - 9);
                    PointF p2 = new PointF(baseX - wave, baseY - 19);
                    PointF p3 = new PointF(baseX + wave * 0.6f, baseY - 28);

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

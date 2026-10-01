using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZenQOffice.Setup
{
    public enum MascotMood
    {
        HappySmiling,
        SadCrying
    }

    public static class MascotRenderer
    {
        public static void DrawMascot(Graphics g, float cx, float cy, float scale, float timeSec, MascotMood mood)
        {
            GraphicsState state = g.Save();
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(scale, scale);

            // 1. Nền tròn pastel mềm mại
            Color circleColor = (mood == MascotMood.SadCrying)
                ? Color.FromArgb(241, 245, 249) // Slate grey/ice
                : Color.FromArgb(243, 246, 252); // Soft light blue

            using (GraphicsPath circle = new GraphicsPath())
            using (SolidBrush circleBrush = new SolidBrush(circleColor))
            {
                circle.AddEllipse(-65, -55, 130, 130);
                g.FillPath(circleBrush, circle);
            }

            if (mood == MascotMood.HappySmiling)
            {
                // Hạt lấp lánh (Sparkles & Confetti) chính xác như hình mẫu
                float sp1 = (float)(0.6 + 0.4 * Math.Sin(timeSec * 3.2));
                DrawStar(g, -48, -42, 6.5f * sp1, Color.FromArgb(245, 158, 11)); // Amber star góc trái
                DrawStar(g, 54, -36, 6.0f * (1.2f - 0.4f * sp1), Color.FromArgb(16, 185, 129)); // Green star góc phải

                // Hạt chấm màu & Confetti bay lơ lửng
                using (SolidBrush bPink = new SolidBrush(Color.FromArgb(244, 63, 94)))
                    g.FillEllipse(bPink, -52, 2, 6, 6); // Pink dot trái

                using (SolidBrush bYellow = new SolidBrush(Color.FromArgb(234, 179, 8)))
                    g.FillRectangle(bYellow, 46, -16, 8, 5); // Yellow rect phải

                using (SolidBrush bPurple = new SolidBrush(Color.FromArgb(139, 92, 246)))
                    g.FillEllipse(bPurple, 56, 18, 6, 6); // Purple dot phải
            }
            else
            {
                // Mây mưa u buồn & Trái tim vỡ
                DrawSadCloudAndRain(g, timeSec);
            }

            // 2. Chuyển động nhịp thở và nghiêng đầu nhẹ nhàng
            float breathY = (mood == MascotMood.SadCrying)
                ? (float)(Math.Sin(timeSec * 1.6) * 1.0)
                : (float)(Math.Sin(timeSec * 2.6) * 1.6);

            float headTilt = (mood == MascotMood.SadCrying)
                ? -3.0f + (float)(Math.Sin(timeSec * 1.4) * 1.2)
                : (float)(Math.Sin(timeSec * 2.0) * 2.2);

            // Thân người / Áo len xanh (Vibrant Blue #2563EB / #3B82F6)
            Color topColor = (mood == MascotMood.SadCrying) ? Color.FromArgb(100, 116, 139) : Color.FromArgb(59, 130, 246);
            Color botColor = (mood == MascotMood.SadCrying) ? Color.FromArgb(71, 85, 105) : Color.FromArgb(37, 99, 235);

            using (GraphicsPath torso = new GraphicsPath())
            using (LinearGradientBrush torsoBrush = new LinearGradientBrush(new RectangleF(-45, 15, 90, 60), topColor, botColor, 90f))
            {
                torso.AddArc(-38, 14 + breathY * 0.4f, 76, 68, 180, 180);
                torso.AddLine(38, 48, 38, 65);
                torso.AddLine(38, 65, -38, 65);
                torso.AddLine(-38, 65, -38, 48);
                torso.CloseFigure();
                g.FillPath(torsoBrush, torso);
            }

            // Cổ
            using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                g.FillRectangle(skin, -8, 2 + breathY * 0.4f, 16, 16);
            }

            // Cổ áo trắng
            using (SolidBrush collar = new SolidBrush(Color.White))
            {
                g.FillPie(collar, -14, 10 + breathY * 0.5f, 28, 16, 0, 180);
            }

            // 3. Đầu & Khuôn mặt
            GraphicsState headState = g.Save();
            g.TranslateTransform(0, breathY);
            g.RotateTransform(headTilt);

            using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                // Đầu tròn
                g.FillEllipse(skin, -24, -40, 48, 48);
                // Tai
                g.FillEllipse(skin, -27, -22, 9, 12);
                g.FillEllipse(skin, 18, -22, 9, 12);
            }

            // Mái tóc thời trang gọn gàng (Dark Navy #0F172A)
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

                // Đỉnh tóc tròn
                g.FillEllipse(hairBrush, -16, -48, 26, 16);
            }

            // Lông mày
            using (Pen browPen = new Pen(Color.FromArgb(15, 23, 42), 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (mood == MascotMood.HappySmiling)
                {
                    g.DrawArc(browPen, -16, -30, 11, 7, 200, 140);
                    g.DrawArc(browPen, 5, -30, 11, 7, 200, 140);
                }
                else
                {
                    g.DrawLine(browPen, -15, -26, -6, -29);
                    g.DrawLine(browPen, 6, -29, 15, -26);
                }
            }

            // Gọng kính tròn
            using (Pen glassPen = new Pen(Color.FromArgb(30, 41, 59), 2.2f))
            {
                g.DrawEllipse(glassPen, -18, -26, 15, 15);
                g.DrawEllipse(glassPen, 3, -26, 15, 15);
                g.DrawLine(glassPen, -3, -18, 3, -18);

                // Ánh sáng phản chiếu kính
                using (Pen sheenPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(sheenPen, -15, -23, -11, -19);
                    g.DrawLine(sheenPen, 6, -23, 10, -19);
                }
            }

            // BIỂU CẢM MẮT & NỤ CƯỜI TƯƠI
            if (mood == MascotMood.HappySmiling)
            {
                using (Pen eyePen = new Pen(Color.FromArgb(30, 41, 59), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                using (SolidBrush blush = new SolidBrush(Color.FromArgb(140, 244, 63, 94)))
                {
                    // Mắt cười cong hạnh phúc rạng rỡ ( ^ ^ )
                    g.DrawArc(eyePen, -15, -21, 9, 8, 190, 160);
                    g.DrawArc(eyePen, 6, -21, 9, 8, 190, 160);

                    // Đôi má hồng hây hây
                    g.FillEllipse(blush, -22, -13, 9, 6);
                    g.FillEllipse(blush, 13, -13, 9, 6);

                    // NỤ CƯỜI TỎA NẮNG ĐƯỢC CLIP CHÍNH XÁC (RĂNG VÀ LƯỠI NẰM HOÀN TOÀN TRONG KHUÔN MIỆNG)
                    using (GraphicsPath mouthPath = new GraphicsPath())
                    using (SolidBrush mouthBg = new SolidBrush(Color.FromArgb(159, 18, 57))) // Dark Red #9F1239
                    using (SolidBrush tongue = new SolidBrush(Color.FromArgb(244, 114, 182))) // Pink Tongue #F472B6
                    using (SolidBrush teeth = new SolidBrush(Color.White))
                    {
                        mouthPath.AddArc(-7, -13, 14, 12, 0, 180);
                        mouthPath.CloseFigure();

                        // Clip strictly inside mouth contour
                        GraphicsState mouthState = g.Save();
                        g.SetClip(mouthPath);

                        // 1. Khoang miệng đỏ đậm
                        g.FillPath(mouthBg, mouthPath);

                        // 2. Hàm răng trắng nằm ngay ngắn ở viền trên
                        g.FillRectangle(teeth, -7, -13, 14, 4.5f);

                        // 3. Chiếc lưỡi hồng nhô lên từ đáy miệng
                        g.FillEllipse(tongue, -5, -6, 10, 8);

                        g.Restore(mouthState);

                        // Viền môi ngoài
                        g.DrawPath(eyePen, mouthPath);
                    }
                }
            }
            else
            {
                // U buồn, ngấn lệ
                using (Pen eyePen = new Pen(Color.FromArgb(51, 65, 85), 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    // Mắt nhắm buồn bã ( T _ T )
                    g.DrawArc(eyePen, -15, -18, 9, 6, 10, 160);
                    g.DrawArc(eyePen, 6, -18, 9, 6, 10, 160);

                    // Nụ cười mếu máo quay xuống
                    g.DrawArc(eyePen, -6, -8, 12, 7, 190, 160);

                    // GIỌT NƯỚC MẮT LĂN DÀI (Animated Falling Teardrops)
                    float tearPhase1 = (timeSec * 1.8f) % 2.5f;
                    float tearY1 = -16f + tearPhase1 * 14f;
                    float tearAlpha1 = Math.Max(0f, Math.Min(1f, 1.2f - tearPhase1 / 2.5f));

                    if (tearAlpha1 > 0.05f)
                    {
                        Color tc1 = Color.FromArgb((int)(tearAlpha1 * 220), 56, 189, 248);
                        using (SolidBrush tb1 = new SolidBrush(tc1))
                        {
                            GraphicsPath tear1 = new GraphicsPath();
                            tear1.AddArc(-12, tearY1, 6, 6, 0, 180);
                            tear1.AddLine(-6, tearY1 + 3, -9, tearY1 - 4);
                            tear1.CloseFigure();
                            g.FillPath(tb1, tear1);
                        }
                    }

                    float tearPhase2 = ((timeSec + 1.2f) * 1.8f) % 2.5f;
                    float tearY2 = -16f + tearPhase2 * 14f;
                    float tearAlpha2 = Math.Max(0f, Math.Min(1f, 1.2f - tearPhase2 / 2.5f));

                    if (tearAlpha2 > 0.05f)
                    {
                        Color tc2 = Color.FromArgb((int)(tearAlpha2 * 220), 56, 189, 248);
                        using (SolidBrush tb2 = new SolidBrush(tc2))
                        {
                            GraphicsPath tear2 = new GraphicsPath();
                            tear2.AddArc(6, tearY2, 6, 6, 0, 180);
                            tear2.AddLine(12, tearY2 + 3, 9, tearY2 - 4);
                            tear2.CloseFigure();
                            g.FillPath(tb2, tear2);
                        }
                    }
                }
            }

            g.Restore(headState);

            // 4. CÁNH TAY (TAY TRÁI ĐẶT BÊN HÔNG, TAY PHẢI GIƠ CAO VẪY TAY CHÀO NHIỆT LIỆT)
            Color armColor = (mood == MascotMood.SadCrying) ? Color.FromArgb(71, 85, 105) : Color.FromArgb(37, 99, 235);

            if (mood == MascotMood.HappySmiling)
            {
                using (Pen armPen = new Pen(armColor, 12f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
                {
                    // Tay trái uốn cong nhẹ ôm cạnh sườn
                    g.DrawCurve(armPen, new PointF[] {
                        new PointF(-28, 22 + breathY * 0.5f),
                        new PointF(-32, 40 + breathY * 0.8f),
                        new PointF(-18, 48 + breathY)
                    });
                    g.FillEllipse(skin, -22, 44 + breathY, 11, 11);

                    // Tay phải GIƠ CAO VẪY CHÀO NGHIÊNG GÓC 45 ĐỘ HƯỚNG RA NGOÀI
                    float waveAngle = (float)(Math.Sin(timeSec * 5.0) * 10.0);
                    GraphicsState armState = g.Save();
                    g.TranslateTransform(26, 22 + breathY * 0.5f);
                    g.RotateTransform(waveAngle);

                    // Cánh tay giơ chéo lên phải
                    g.DrawLine(armPen, 0, 0, 22, -28);
                    // Bàn tay tròn
                    g.FillEllipse(skin, 17, -36, 13, 13);

                    g.Restore(armState);
                }
            }
            else
            {
                // Cánh tay buông thõng buồn bã
                using (Pen armPen = new Pen(armColor, 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
                {
                    g.DrawCurve(armPen, new PointF[] {
                        new PointF(-28, 24 + breathY * 0.5f),
                        new PointF(-34, 46 + breathY),
                        new PointF(-30, 58 + breathY)
                    });
                    g.FillEllipse(skin, -34, 54 + breathY, 10, 10);

                    g.DrawCurve(armPen, new PointF[] {
                        new PointF(28, 24 + breathY * 0.5f),
                        new PointF(34, 46 + breathY),
                        new PointF(30, 58 + breathY)
                    });
                    g.FillEllipse(skin, 26, 54 + breathY, 10, 10);
                }
            }

            g.Restore(state);
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

        private static void DrawSadCloudAndRain(Graphics g, float timeSec)
        {
            using (SolidBrush cloudB = new SolidBrush(Color.FromArgb(203, 213, 225)))
            {
                g.FillEllipse(cloudB, -55, -48, 26, 16);
                g.FillEllipse(cloudB, -44, -54, 22, 20);
                g.FillEllipse(cloudB, -30, -48, 24, 16);
            }

            using (Pen rainPen = new Pen(Color.FromArgb(147, 197, 253), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                for (int i = 0; i < 4; i++)
                {
                    float rx = -50 + i * 9;
                    float ry = -32 + ((timeSec * 35f + i * 8f) % 22f);
                    g.DrawLine(rainPen, rx, ry, rx - 2, ry + 6);
                }
            }

            float hPhase = (float)(Math.Sin(timeSec * 2.0) * 3f);
            DrawBrokenHeart(g, 48, -38 + hPhase, 0.7f);
        }

        private static void DrawBrokenHeart(Graphics g, float x, float y, float scale)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(scale, scale);

            using (SolidBrush hb = new SolidBrush(Color.FromArgb(180, 244, 63, 94)))
            using (Pen crackPen = new Pen(Color.FromArgb(248, 250, 252), 1.2f))
            {
                GraphicsPath heart = new GraphicsPath();
                heart.AddArc(-10, -10, 10, 10, 140, 220);
                heart.AddArc(0, -10, 10, 10, 180, 220);
                heart.AddLine(10, -2, 0, 9);
                heart.AddLine(0, 9, -10, -2);
                heart.CloseFigure();
                g.FillPath(hb, heart);

                g.DrawLines(crackPen, new PointF[] {
                    new PointF(0, -7),
                    new PointF(-2, -3),
                    new PointF(2, 1),
                    new PointF(-1, 5),
                    new PointF(0, 9)
                });
            }

            g.Restore(state);
        }
    }
}

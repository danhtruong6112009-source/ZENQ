using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

class TestGradComparison
{
    static void Main()
    {
        string pptxPath = @"C:\Users\Danh\Desktop\bài 2.pptx";
        PresentationRoot pres = PptxHandler.LoadPptx(pptxPath);
        SlideNode s9 = pres.Slides[8];
        ShapeNode sh = s9.Shapes[2];

        // Draw quizShape with line-wide gradient directly
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        using (Bitmap bmp = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Black);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // We test 2 options for gradient bounds:
            // Opt A: line-wide: X = 303, W = 1308
            // Opt B: textBounds: X = sh.Transform.X, W = sh.Transform.Width (227.9 .. 1464.3)

            // Let's test both and compare MSE with bMs in the text region X: 250..1650, Y: 200..600
            for (int opt = 1; opt <= 2; opt++)
            {
                using (Bitmap optBmp = new Bitmap(1920, 1080))
                using (Graphics gOpt = Graphics.FromImage(optBmp))
                {
                    gOpt.SmoothingMode = SmoothingMode.AntiAlias;
                    gOpt.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    RectangleF gradRect = (opt == 1)
                        ? new RectangleF(303, 200, 1308, 450)
                        : new RectangleF(sh.Transform.X, 200, sh.Transform.Width, 450);

                    using (LinearGradientBrush lgb = new LinearGradientBrush(gradRect, Color.White, Color.FromArgb(207, 223, 253), 0.0f))
                    {
                        ColorBlend cb = new ColorBlend(3);
                        cb.Positions = new float[] { 0.0f, 0.5f, 1.0f };
                        cb.Colors = new Color[] { Color.White, Color.FromArgb(0, 133, 255), Color.FromArgb(207, 223, 253) };
                        lgb.InterpolationColors = cb;
                        lgb.WrapMode = WrapMode.TileFlipX;

                        using (Font f = SlideFontManager.CreateFont("Anton", sh.FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                        {
                            // Draw centered in text box
                            float textW = gOpt.MeasureString("QUIZ TIME", f, PointF.Empty, StringFormat.GenericTypographic).Width;
                            float x = sh.Transform.X + (sh.Transform.Width - textW) / 2f;
                            float y = 200; // top of glyph
                            gOpt.DrawString("QUIZ TIME", f, lgb, x, y, StringFormat.GenericTypographic);
                        }
                    }

                    // Compute error vs MS 365 text pixels
                    long diffSum = 0;
                    int counted = 0;
                    for (int y = 250; y < 550; y += 2)
                    {
                        for (int x = 300; x < 1620; x += 2)
                        {
                            Color cMs = bMs.GetPixel(x, y);
                            Color cOpt = optBmp.GetPixel(x, y);
                            if (cOpt.A > 50)
                            {
                                int dr = Math.Abs(cMs.R - cOpt.R);
                                int dg = Math.Abs(cMs.G - cOpt.G);
                                int db = Math.Abs(cMs.B - cOpt.B);
                                diffSum += dr + dg + db;
                                counted++;
                            }
                        }
                    }

                    double avgDiff = (double)diffSum / (counted * 3);
                    Console.WriteLine(string.Format("Option {0} ({1}): Avg color error per channel = {2:F2} (Sampled {3} pixels)",
                        opt, opt == 1 ? "line-wide [303..1611]" : "textBounds [228..1692]", avgDiff, counted));
                }
            }
        }
    }
}

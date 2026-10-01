using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

class TestGradLineDiff
{
    static void Main()
    {
        string scratchDir = "scratch";
        string pptxPath = @"C:\Users\Danh\Desktop\bài 2.pptx";
        PresentationRoot pres = PptxHandler.LoadPptx(pptxPath);
        SlideNode s9 = pres.Slides[8];
        ShapeNode quizShape = s9.Shapes[2];

        string msPath9 = Path.Combine(scratchDir, "ms365_slide9.png");
        using (Bitmap bMs = new Bitmap(msPath9))
        using (Bitmap bGen = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bGen))
        {
            g.Clear(Color.FromArgb(0, 0, 0, 0)); // transparent
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (Font f = SlideFontManager.CreateFont("Anton", quizShape.FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                float textW = g.MeasureString("QUIZ TIME", f, PointF.Empty, StringFormat.GenericTypographic).Width;
                float x = quizShape.Transform.X + (quizShape.Transform.Width - textW) / 2f;
                float y = 200f;

                RectangleF lineRect = new RectangleF(x, y, textW, 450);
                using (LinearGradientBrush lgb = new LinearGradientBrush(lineRect, Color.White, Color.FromArgb(207, 223, 253), 0.0f))
                {
                    ColorBlend cb = new ColorBlend(3);
                    cb.Positions = new float[] { 0f, 0.5f, 1f };
                    cb.Colors = new Color[] { Color.White, Color.FromArgb(0, 133, 255), Color.FromArgb(207, 223, 253) };
                    lgb.InterpolationColors = cb;
                    lgb.WrapMode = WrapMode.TileFlipX;

                    g.DrawString("QUIZ TIME", f, lgb, x, y, StringFormat.GenericTypographic);
                }
            }

            int diffCount = 0;
            int textPixelCount = 0;
            for (int y = 200; y < 600; y += 2)
            {
                for (int x = 200; x < 1700; x += 2)
                {
                    Color c2 = bGen.GetPixel(x, y);
                    if (c2.A > 100) // where text is actually drawn
                    {
                        textPixelCount++;
                        Color c1 = bMs.GetPixel(x, y);
                        if (Math.Abs(c1.R - c2.R) > 35 || Math.Abs(c1.G - c2.G) > 35 || Math.Abs(c1.B - c2.B) > 35)
                        {
                            diffCount++;
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("Line-wide: Text pixels = {0}, Mismatch = {1} ({2:F2}%)",
                textPixelCount, diffCount, (diffCount * 100.0 / textPixelCount)));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class TestParagraphSpacing
{
    static void Main()
    {
        string msPath = @"scratch\ms365_slide8.png";
        if (!File.Exists(msPath)) { Console.WriteLine("ms365_slide8.png not found"); return; }

        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];

        SlidesView sv = new SlidesView();
        sv.Presentation = pres;

        using (Bitmap bGen = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bGen))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            sv.DrawSlideBackground(g, new Rectangle(0, 0, 1920, 1080), s8);
            foreach (ShapeNode sh in s8.Shapes)
            {
                if (sh == null || !sh.IsVisible) continue;
                sv.DrawShape(g, 0, 0, 1.0f, sh, false);
            }
            bGen.Save(@"scratch\s8_current_test.png", ImageFormat.Png);
        }

        using (Bitmap b1 = new Bitmap(msPath))
        using (Bitmap b2 = new Bitmap(@"scratch\s8_current_test.png"))
        {
            int diffCount = 0;
            for (int y = 0; y < 1080; y += 2)
            {
                for (int x = 0; x < 1920; x += 2)
                {
                    Color c1 = b1.GetPixel(x, y);
                    Color c2 = b2.GetPixel(x, y);
                    if (Math.Abs(c1.R - c2.R) > 15 || Math.Abs(c1.G - c2.G) > 15 || Math.Abs(c1.B - c2.B) > 15)
                    {
                        diffCount++;
                    }
                }
            }
            double totalSampled = (1920 / 2) * (1080 / 2);
            Console.WriteLine(string.Format("Current Slide 8 Diff: {0} / {1} ({2:F2}%)",
                diffCount, totalSampled, diffCount * 100.0 / totalSampled));
        }
    }
}

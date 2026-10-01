using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class TestFixes
{
    static void Main()
    {
        string scratchDir = "scratch";
        string pptxPath = @"C:\Users\Danh\Desktop\bài 2.pptx";
        PresentationRoot pres = PptxHandler.LoadPptx(pptxPath);

        // Slide 9
        SlideNode s9 = pres.Slides[8];
        string msPath9 = Path.Combine(scratchDir, "ms365_slide9.png");
        if (File.Exists(msPath9))
        {
            using (Bitmap bMs = new Bitmap(msPath9))
            using (Bitmap bGen = new Bitmap(1920, 1080))
            using (Graphics g = Graphics.FromImage(bGen))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                SlidesView sv = new SlidesView { Presentation = pres };
                sv.DrawSlideBackground(g, new Rectangle(0, 0, 1920, 1080), s9);
                foreach (ShapeNode sh in s9.Shapes)
                {
                    if (sh == null || !sh.IsVisible) continue;
                    sv.DrawShape(g, 0, 0, 1.0f, sh, false);
                }

                // Check text box 4 differences
                int diffCount = 0;
                for (int y = 200; y < 600; y += 2)
                {
                    for (int x = 200; x < 1700; x += 2)
                    {
                        Color c1 = bMs.GetPixel(x, y);
                        Color c2 = bGen.GetPixel(x, y);
                        if (Math.Abs(c1.R - c2.R) > 25 || Math.Abs(c1.G - c2.G) > 25 || Math.Abs(c1.B - c2.B) > 25)
                        {
                            diffCount++;
                        }
                    }
                }
                Console.WriteLine("Slide 9 Text region diff count (current code): " + diffCount);
            }
        }
    }
}

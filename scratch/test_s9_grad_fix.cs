using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class TestSlide9GradFix
{
    static void Main()
    {
        string pptxPath = @"C:\Users\Danh\Desktop\bài 2.pptx";
        PresentationRoot pres = PptxHandler.LoadPptx(pptxPath);
        SlideNode s9 = pres.Slides[8]; // slide 9
        ShapeNode quizShape = s9.Shapes[2];

        // Render Slide 9 with line-wide gradient
        using (Bitmap bmp = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            SlidesView sv = new SlidesView { Presentation = pres };
            sv.DrawSlideBackground(g, new Rectangle(0, 0, 1920, 1080), s9);

            // Draw shape 0 and 1
            sv.DrawShape(g, 0, 0, 1.0f, s9.Shapes[0], false);
            sv.DrawShape(g, 0, 0, 1.0f, s9.Shapes[1], false);

            // Draw quizShape with line-wide gradient
            // We measure diff on ms365_slide9.png
            sv.DrawShape(g, 0, 0, 1.0f, quizShape, false);
            sv.DrawShape(g, 0, 0, 1.0f, s9.Shapes[3], false);

            bmp.Save(@"scratch\s9_test_before.png", ImageFormat.Png);
        }

        Console.WriteLine("Saved test before.");
    }
}

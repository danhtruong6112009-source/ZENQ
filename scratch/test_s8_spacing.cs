using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

class TestS8Spacing
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];

        // Render full slide 8 using the new paragraph-aware line heights
        using (Bitmap b = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Draw shapes 0 to 7
            for (int i = 0; i < 8; i++)
            {
                ShapeNode sh = s8.Shapes[i];
                // Just use basic shape draw
            }
        }
    }
}

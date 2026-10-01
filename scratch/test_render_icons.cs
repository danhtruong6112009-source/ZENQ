using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GenOffice.UI;

class TestRenderIcons
{
    static void Main()
    {
        // 1. Test Paint Bucket and Numbering rendered at 16x16 and 48x48
        int size = 16;
        var bmp16 = new Bitmap(120, 60);
        using (var g = Graphics.FromImage(bmp16))
        {
            g.Clear(System.Drawing.Color.White);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            // Draw Bullets, Numbering, DecIndent, IncIndent, LineSpacing
            WpfSlideIconRenderer.DrawIcon(g, SlideIconKind.Bullets, 10, 10, 16, 16, System.Drawing.Color.FromArgb(55, 65, 81));
            WpfSlideIconRenderer.DrawIcon(g, SlideIconKind.Numbering, 32, 10, 16, 16, System.Drawing.Color.FromArgb(55, 65, 81));
            WpfSlideIconRenderer.DrawIcon(g, SlideIconKind.Columns, 54, 10, 16, 16, System.Drawing.Color.FromArgb(55, 65, 81));
            
            // Draw ShapeFill (Bucket) with Office Blue
            WpfSlideIconRenderer.DrawIcon(g, SlideIconKind.ShapeFill, 76, 10, 16, 16, System.Drawing.Color.FromArgb(55, 65, 81), System.Drawing.Color.FromArgb(0, 120, 212));
            // Draw ShapeOutline (Pencil)
            WpfSlideIconRenderer.DrawIcon(g, SlideIconKind.ShapeOutline, 98, 10, 16, 16, System.Drawing.Color.FromArgb(55, 65, 81), System.Drawing.Color.FromArgb(0, 120, 212));
        }

        // Upscale 4x for crystal clear visual inspection
        var bmpBig = new Bitmap(480, 240);
        using (var g = Graphics.FromImage(bmpBig))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(bmp16, new Rectangle(0, 0, 480, 240), new Rectangle(0, 0, 120, 60), GraphicsUnit.Pixel);
        }
        bmpBig.Save("scratch/test_icons_zoom4x.png");
        Console.WriteLine("[PASS] Saved scratch/test_icons_zoom4x.png");
    }
}

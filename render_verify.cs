using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class Program
{
    [STAThread]
    static void Main()
    {
        PresentationRoot pres;
        bool ok = PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\bài 2.pptx", out pres);
        Console.WriteLine("Pptx loaded: " + ok + ", slide count: " + (pres != null ? pres.Slides.Count : 0));
        if (!ok || pres == null) return;

        Console.WriteLine("Slide 5 shape count: " + pres.Slides[4].Shapes.Count);
        for (int i = 0; i < pres.Slides[4].Shapes.Count; i++)
        {
            var shp = pres.Slides[4].Shapes[i];
            Console.WriteLine(string.Format("Shape[{0}]: Name='{1}', Type={2}, Bounds=({3:F1},{4:F1},{5:F1},{6:F1}), Fill=0x{7:X6}, HasGrad={8}, TextLen={9}, Runs={10}",
                i, shp.Name, shp.Type, shp.Transform.X, shp.Transform.Y, shp.Transform.Width, shp.Transform.Height,
                shp.FillColor, shp.HasGradientFill, (shp.Text ?? "").Length, shp.TextRuns != null ? shp.TextRuns.Count : 0));
        }

        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        sv.ActiveSlideIndex = 2; // Slide 3

        int w = 1600;
        int h = 900;
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(240, 240, 240));
            sv.Draw(g, w, h);
            bmp.Save("verify_app_render.png");
            Console.WriteLine("Saved verify_app_render.png");
        }

        // Also render active slide = Slide 5
        sv.ActiveSlideIndex = 4; // Slide 5
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(240, 240, 240));
            sv.Draw(g, w, h);
            bmp.Save("verify_slide5_app_render.png");
            Console.WriteLine("Saved verify_slide5_app_render.png");
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using GenOffice.Format;
using GenOffice.Core;
using GenOffice.UI;

class TestBreakdown {
    static void Main() {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        sv.ActiveSlideIndex = 6;
        
        int w = 1280, h = 720;
        
        // Warm up cache
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp)) {
            sv.Draw(g, w, h);
        }
        
        // Measure 50 frames of drawing
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++) {
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp)) {
                sv.Draw(g, w, h);
            }
        }
        sw.Stop();
        Console.WriteLine(string.Format("Total Draw: {0:F2} ms/frame", (double)sw.ElapsedMilliseconds / 50));
        
        // What is inside Slide 7 that takes 43ms?
        SlideNode s7 = pres.Slides[6];
        Console.WriteLine("Slide 7 shapes count: " + s7.Shapes.Count);
        foreach (ShapeNode sh in s7.Shapes) {
            Console.WriteLine(string.Format("  Shape: Type={0}, W={1}, H={2}, Img={3}, SoftEdge={4}, Blur={5}", 
                sh.Type, sh.Transform.Width, sh.Transform.Height, sh.CachedImage != null, sh.SoftEdgeRadius, sh.BlurRadius));
        }
    }
}

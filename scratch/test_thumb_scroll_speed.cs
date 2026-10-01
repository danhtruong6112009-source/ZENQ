using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.Core;
using GenOffice.UI;

class TestThumbScrollSpeed {
    static void Main() {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        
        int w = 1280, h = 720;
        
        // Pass 1: Cold scroll (generating uncached thumbnails)
        int frameCount = 60;
        for (int f = 0; f < frameCount; f++) {
            sv.ThumbScrollY = (f * 20) % 1500;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp)) {
                sv.Draw(g, w, h);
            }
        }
        
        // Pass 2: Pure warm cached scroll (all thumbnails already generated)
        Stopwatch swWarm = Stopwatch.StartNew();
        for (int f = 0; f < frameCount; f++) {
            sv.ThumbScrollY = (f * 20) % 1500;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp)) {
                sv.Draw(g, w, h);
            }
        }
        swWarm.Stop();
        
        double warmMs = (double)swWarm.ElapsedMilliseconds / frameCount;
        double warmFps = 1000.0 / Math.Max(0.1, warmMs);
        Console.WriteLine(string.Format("Pure cached scroll: {0:F2} ms/frame ({1:F0} FPS)", warmMs, warmFps));
    }
}

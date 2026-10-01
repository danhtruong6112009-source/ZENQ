using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

class Program {
    static void Main() {
        int w = 1920, h = 1080;
        using var backBmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var bmpA = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var bmpB = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(backBmp);
        
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.None;
        
        var sw = Stopwatch.StartNew();
        int frames = 60;
        for (int i = 0; i < frames; i++) {
            float t = i / (float)frames;
            int yA = -(int)(h * t);
            int yB = h - (int)(h * t);
            
            var state = g.Save();
            g.SetClip(new Rectangle(0, 0, w, h));
            g.DrawImage(bmpA, new Rectangle(0, yA, w, h), 0, 0, w, h, GraphicsUnit.Pixel);
            g.DrawImage(bmpB, new Rectangle(0, yB, w, h), 0, 0, w, h, GraphicsUnit.Pixel);
            g.Restore(state);
        }
        sw.Stop();
        Console.WriteLine($60 frames took {sw.ElapsedMilliseconds} ms ({(frames * 1000.0 / sw.ElapsedMilliseconds):F1} FPS, {(sw.ElapsedMilliseconds / (float)frames):F2} ms/frame));
    }
}

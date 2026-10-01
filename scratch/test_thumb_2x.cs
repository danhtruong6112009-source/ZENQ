using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.Core;
using GenOffice.UI;

class TestThumb2x {
    static void Main() {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        
        float slideAspect = (float)pres.CanvasWidth / Math.Max(1.0f, (float)pres.CanvasHeight);
        int thumbW = 160;
        int thumbH = (int)Math.Round(thumbW / slideAspect);
        
        Console.WriteLine(string.Format("Thumbnail size: {0}x{1} (Aspect={2:F3})", thumbW, thumbH, slideAspect));
        
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < pres.Slides.Count; i++) {
            SlideNode s = pres.Slides[i];
            int renderW = thumbW * 2;
            int renderH = thumbH * 2;
            float renderScale = (float)renderW / pres.CanvasWidth;
            
            using (Bitmap hiresBmp = new Bitmap(renderW, renderH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb)) {
                using (Graphics bg = Graphics.FromImage(hiresBmp)) {
                    bg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    bg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    bg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    bg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    
                    Rectangle miniFrame = new Rectangle(0, 0, renderW, renderH);
                    sv.DrawSlideBackground(bg, miniFrame, s);
                    foreach (ShapeNode shape in s.Shapes) {
                        if (shape == null || !shape.IsVisible || (shape.Name != null && shape.Name.StartsWith("__morph_ghost__"))) continue;
                        sv.DrawShape(bg, 0, 0, renderScale, shape, false);
                    }
                }
                
                Bitmap bmp = new Bitmap(thumbW, thumbH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics fg = Graphics.FromImage(bmp)) {
                    fg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    fg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    fg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    fg.DrawImage(hiresBmp, new Rectangle(0, 0, thumbW, thumbH));
                }
                s.ThumbnailCache = bmp;
                s.IsThumbnailDirty = false;
            }
        }
        sw.Stop();
        Console.WriteLine(string.Format("Generated all {0} thumbnails in {1} ms (avg {2:F2} ms/thumb)", 
            pres.Slides.Count, sw.ElapsedMilliseconds, (double)sw.ElapsedMilliseconds / pres.Slides.Count));
            
        pres.Slides[6].ThumbnailCache.Save(@"scratch/crisp_thumb_s7.png");
        pres.Slides[5].ThumbnailCache.Save(@"scratch/crisp_thumb_s6.png");
        pres.Slides[4].ThumbnailCache.Save(@"scratch/crisp_thumb_s5.png");
        Console.WriteLine("Saved crisp thumbnails for slides 5, 6, 7!");
    }
}

using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.Core;
using GenOffice.UI;

class TestThumb {
    static void Main() {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        Console.WriteLine("Loaded slides: " + pres.Slides.Count);
        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        
        int thumbW = 150;
        int thumbH = pres.IsWidescreen ? (thumbW * 9 / 16) : (thumbW * 3 / 4);
        Console.WriteLine(string.Format("thumbW={0}, thumbH={1}, CanvasW={2}, CanvasH={3}", thumbW, thumbH, pres.CanvasWidth, pres.CanvasHeight));
        
        // Render thumbnail of slide 7 (index 6)
        SlideNode s7 = pres.Slides[6];
        Bitmap bmp = new Bitmap(thumbW, thumbH);
        float miniScale = (float)thumbW / pres.CanvasWidth;
        using (Graphics bg = Graphics.FromImage(bmp)) {
            bg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            bg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
            bg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            Rectangle miniFrame = new Rectangle(0, 0, thumbW, thumbH);
            sv.DrawSlideBackground(bg, miniFrame, s7);
            foreach (ShapeNode shape in s7.Shapes) {
                if (shape == null || !shape.IsVisible || (shape.Name != null && shape.Name.StartsWith("__morph_ghost__"))) continue;
                sv.DrawShape(bg, 0, 0, miniScale, shape, false);
            }
        }
        bmp.Save(@"scratch/test_gen_thumb_s7.png");
        Console.WriteLine("Saved scratch/test_gen_thumb_s7.png");
    }
}

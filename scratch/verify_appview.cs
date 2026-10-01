using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Format;
using GenOffice.Core;
using GenOffice.UI;

class VerifyAppView {
    static void Main() {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        sv.ActiveSlideIndex = 6; // Slide 7 (0-indexed 6)
        sv.CanvasScrollX = 0;
        sv.CanvasScrollY = 0;
        sv.ZoomLevel = 1.0f;
        
        int w = 1280;
        int h = 720;
        
        Bitmap bmp = new Bitmap(w, h);
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.Clear(Color.FromArgb(240, 240, 240));
            sv.Draw(g, w, h);
        }
        
        bmp.Save(@"scratch/verified_slide7_appview.png");
        Console.WriteLine("[PASS] Saved scratch/verified_slide7_appview.png");
    }
}

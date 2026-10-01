using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.Core;
using GenOffice.UI;

class TestFitScale {
    static void Main() {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        sv.ActiveSlideIndex = 6;
        sv.CanvasScrollX = 0;
        sv.CanvasScrollY = 0;
        sv.ZoomLevel = 1.0f;
        
        int w = 1280;
        int h = 720;
        int leftSidebarW = 210;
        int rightSidebarW = 0;
        int topBarY = 155;
        int canvasX = leftSidebarW;
        int canvasY = topBarY;
        int canvasW = w - leftSidebarW - rightSidebarW;
        int canvasH = h - canvasY - 26;
        
        float marginX = 40f;
        float marginY = 40f;
        float fitScaleX = Math.Max(0.05f, (canvasW - marginX * 2) / Math.Max(1.0f, pres.CanvasWidth));
        float fitScaleY = Math.Max(0.05f, (canvasH - marginY * 2) / Math.Max(1.0f, pres.CanvasHeight));
        float baseFitScale = Math.Min(fitScaleX, fitScaleY);
        float scale = sv.ZoomLevel * baseFitScale;
        
        int slideW = (int)Math.Round(pres.CanvasWidth * scale);
        int slideH = (int)Math.Round(pres.CanvasHeight * scale);
        int slideX = canvasX + (canvasW - slideW) / 2;
        int slideY = canvasY + (canvasH - slideH) / 2;
        
        Console.WriteLine(string.Format("canvasW={0}, canvasH={1}", canvasW, canvasH));
        Console.WriteLine(string.Format("baseFitScale={0:F4}, slide={1}x{2} at ({3},{4})", baseFitScale, slideW, slideH, slideX, slideY));
        Console.WriteLine(string.Format("Top margin={0}px, Bottom margin={1}px, Left margin={2}px, Right margin={3}px",
            slideY - canvasY, (canvasY + canvasH) - (slideY + slideH), slideX - canvasX, (canvasX + canvasW) - (slideX + slideW)));
    }
}

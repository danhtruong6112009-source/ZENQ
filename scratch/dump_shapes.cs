using System;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

class DumpShapes
{
    static void Main()
    {
        string pptxPath = @"C:\Users\Danh\Desktop\bài 2.pptx";
        PresentationRoot pres = PptxHandler.LoadPptx(pptxPath);
        int[] checkSlides = new int[] { 5, 8, 9 };

        foreach (int sNum in checkSlides)
        {
            SlideNode sl = pres.Slides[sNum - 1];
            Console.WriteLine(string.Format("\n=== SLIDE {0} (Shapes: {1}) ===", sNum, sl.Shapes.Count));
            for (int i = 0; i < sl.Shapes.Count; i++)
            {
                ShapeNode sh = sl.Shapes[i];
                Console.WriteLine(string.Format("  [{0}] Name='{1}' Type={2} X={3:F1} Y={4:F1} W={5:F1} H={6:F1} Img={7} Text='{8}' CustomPath={9} Fill=0x{10:X8} Stroke=0x{11:X8} GradStops={12} Crop=[{13:F2},{14:F2},{15:F2},{16:F2}] FillRect=[{17:F2},{18:F2},{19:F2},{20:F2}] Blur={21} SoftEdge={22}",
                    i, sh.Name, sh.Type, sh.Transform.X, sh.Transform.Y, sh.Transform.Width, sh.Transform.Height,
                    sh.CachedImage != null || !string.IsNullOrEmpty(sh.ImagePath),
                    sh.Text != null ? (sh.Text.Length > 20 ? sh.Text.Substring(0, 20).Replace("\n"," ") + "..." : sh.Text.Replace("\n"," ")) : "",
                    sh.CustomPath != null,
                    sh.FillColor, sh.StrokeColor,
                    sh.GradientStops != null ? sh.GradientStops.Count : 0,
                    sh.CropLeft, sh.CropTop, sh.CropRight, sh.CropBottom,
                    sh.FillRectLeft, sh.FillRectTop, sh.FillRectRight, sh.FillRectBottom,
                    sh.BlurRadius, sh.SoftEdgeRadius));
            }
        }
    }
}

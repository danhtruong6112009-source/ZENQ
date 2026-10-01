using System;
using GenOffice.Core;
using GenOffice.Format;

class Program
{
    static void Main()
    {
        PresentationRoot pres;
        PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\bài 2.pptx", out pres);
        Console.WriteLine("CanvasWidth: " + pres.CanvasWidth + ", CanvasHeight: " + pres.CanvasHeight);
        Console.WriteLine("IsWidescreen: " + pres.IsWidescreen);
        var s5 = pres.Slides[4];
        foreach (var shp in s5.Shapes)
        {
            if (shp.Name == "TextBox 23")
            {
                Console.WriteLine("TextBox 23 X=" + shp.Transform.X + " Y=" + shp.Transform.Y + " W=" + shp.Transform.Width + " H=" + shp.Transform.Height);
                Console.WriteLine("FontSize=" + shp.FontSize + " LineSpacing=" + shp.LineSpacing);
            }
        }
    }
}

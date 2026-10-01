using System;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

class InspectSlide9
{
    static void Main()
    {
        string pptxPath = @"C:\Users\Danh\Desktop\bài 2.pptx";
        PresentationRoot pres = PptxHandler.LoadPptx(pptxPath);
        SlideNode sl = pres.Slides[8]; // 0-indexed 8 = slide 9
        ShapeNode sh = sl.Shapes[2]; // TextBox 4
        Console.WriteLine("Shape Name: " + sh.Name);
        Console.WriteLine("FontName: " + sh.FontName);
        Console.WriteLine("FontSize: " + sh.FontSize);
        Console.WriteLine("TextColor: " + string.Format("0x{0:X8}", sh.TextColor));
        Console.WriteLine("Bold: " + sh.Bold + " Italic: " + sh.Italic);
        Console.WriteLine("Alignment: " + sh.Alignment + " VAlign: " + sh.VerticalAlignment);
        Console.WriteLine("HasShadow: " + sh.HasShadow + " HasTextShadow: " + sh.HasTextShadow);
        Console.WriteLine("HasGradientFill: " + sh.HasGradientFill);
        Console.WriteLine("Text: " + sh.Text);
        Console.WriteLine("TextRuns Count: " + (sh.TextRuns != null ? sh.TextRuns.Count : 0));
        if (sh.TextRuns != null)
        {
            foreach (var r in sh.TextRuns)
            {
                Console.WriteLine(string.Format("  Run: Text='{0}' Font='{1}' Size={2} Bold={3} Color=0x{4:X8} HasGrad={5} Stops={6}",
                    r.Text, r.FontName, r.FontSize, r.Bold, r.TextColor, r.HasGradient, r.GradientStops != null ? r.GradientStops.Count : 0));
                if (r.GradientStops != null)
                {
                    foreach (var gs in r.GradientStops)
                    {
                        Console.WriteLine(string.Format("    GradStop: Pos={0} Color=0x{1:X8}", gs.Position, gs.Color));
                    }
                }
            }
        }
    }
}

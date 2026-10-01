using System;
using GenOffice.Core;
using GenOffice.Format;

class CheckTB12
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode sl = pres.Slides[7]; // Slide 8
        ShapeNode tb12 = sl.Shapes[8];
        Console.WriteLine("Shape: " + tb12.Name + " Align=" + tb12.Alignment + " Runs=" + (tb12.TextRuns != null ? tb12.TextRuns.Count : 0));
        if (tb12.TextRuns != null)
        {
            for (int i = 0; i < Math.Min(10, tb12.TextRuns.Count); i++)
            {
                var r = tb12.TextRuns[i];
                Console.WriteLine(string.Format("  Run[{0}]: HasGrad={1} Color=0x{2:X6} Text='{3}'",
                    i, r.HasGradient, r.TextColor & 0xFFFFFF, r.Text.Length > 25 ? r.Text.Substring(0, 25).Replace("\n"," ") + "..." : r.Text.Replace("\n"," ")));
            }
        }
    }
}

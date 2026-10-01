using System;
using GenOffice.Core;
using GenOffice.Format;

class CheckTB23
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode sl = pres.Slides[4]; // Slide 5
        ShapeNode tb23 = sl.Shapes[15];
        Console.WriteLine("Shape: " + tb23.Name + " Align=" + tb23.Alignment + " Runs=" + (tb23.TextRuns != null ? tb23.TextRuns.Count : 0));
        if (tb23.TextRuns != null)
        {
            for (int i = 0; i < tb23.TextRuns.Count; i++)
            {
                var r = tb23.TextRuns[i];
                Console.WriteLine(string.Format("  Run[{0}]: HasGrad={1} Color=0x{2:X6} Text='{3}'",
                    i, r.HasGradient, r.TextColor & 0xFFFFFF, r.Text.Length > 25 ? r.Text.Substring(0, 25) + "..." : r.Text));
            }
        }
    }
}

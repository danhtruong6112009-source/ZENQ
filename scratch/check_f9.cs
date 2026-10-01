using System;
using GenOffice.Core;
using GenOffice.Format;

class CheckF9
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode sl = pres.Slides[4]; // Slide 5
        ShapeNode f9 = sl.Shapes[5];
        Console.WriteLine("Shape: " + f9.Name + " HasGrad=" + f9.HasGradientFill);
        if (f9.GradientStops != null)
        {
            foreach (var st in f9.GradientStops)
            {
                Console.WriteLine(string.Format("  Stop: Pos={0} Color=0x{1:X6} Trans={2}", st.Position, st.Color & 0xFFFFFF, st.Transparency));
            }
        }
    }
}

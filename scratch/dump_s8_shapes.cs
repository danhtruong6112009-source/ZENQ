using System;
using GenOffice.Core;
using GenOffice.Format;

class DumpSlide8Shapes
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];
        Console.WriteLine("Slide 8 Shapes Count: " + s8.Shapes.Count);
        for (int i = 0; i < s8.Shapes.Count; i++)
        {
            ShapeNode sh = s8.Shapes[i];
            Console.WriteLine(string.Format("Shape {0}: Name='{1}', Type={2}, Bounds=({3},{4},{5},{6}), Text='{7}'",
                i, sh.Name, sh.Type, sh.Transform.X, sh.Transform.Y, sh.Transform.Width, sh.Transform.Height,
                sh.Text != null ? sh.Text.Replace("\n", "\\n") : ""));
            if (sh.TextRuns != null)
            {
                Console.WriteLine("  TextRuns Count: " + sh.TextRuns.Count);
                for (int r = 0; r < sh.TextRuns.Count; r++)
                {
                    var tr = sh.TextRuns[r];
                    Console.WriteLine(string.Format("    Run {0}: '{1}' (Size={2}, Font='{3}', Color={4:X6}, HasGrad={5})",
                        r, tr.Text.Replace("\n", "\\n"), tr.FontSize, tr.FontName, tr.TextColor, tr.HasGradient));
                }
            }
        }
    }
}

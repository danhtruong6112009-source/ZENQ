using System;
using System.Collections.Generic;
using GenOffice.Core;
using GenOffice.Format;

class TraceLinesBreak
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];
        ShapeNode sh = s8.Shapes[8]; // TextBox 12

        Console.WriteLine("LineSpacing on Shape: " + sh.LineSpacing);
        Console.WriteLine("Transform: " + sh.Transform.X + ", " + sh.Transform.Y + ", " + sh.Transform.Width + ", " + sh.Transform.Height);
        Console.WriteLine("Runs count: " + sh.TextRuns.Count);
        for (int i = 0; i < sh.TextRuns.Count; i++)
        {
            Console.WriteLine(string.Format("Run {0}: '{1}'", i, sh.TextRuns[i].Text.Replace("\n", "\\n")));
        }
    }
}

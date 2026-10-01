using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class TestActualLines
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];
        ShapeNode sh = s8.Shapes[8];

        Console.WriteLine("Shape 8 TextRuns count: " + (sh.TextRuns != null ? sh.TextRuns.Count : 0));
        if (sh.TextRuns != null)
        {
            for (int i = 0; i < sh.TextRuns.Count; i++)
            {
                Console.WriteLine(string.Format("Run {0}: '{1}'", i, sh.TextRuns[i].Text.Replace("\n", "\\n")));
            }
        }
    }
}

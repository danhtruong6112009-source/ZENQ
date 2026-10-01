using System;
using GenOffice.Core;
using GenOffice.Format;

class Program
{
    static void Main()
    {
        PresentationRoot pres1, pres2;
        PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\bài 2.pptx", out pres1);
        PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\genofficepy\scratch\test_roundtrip.pptx", out pres2);

        var s1 = pres1.Slides[4];
        var s2 = pres2.Slides[4];

        for (int i = 0; i < Math.Min(s1.Shapes.Count, s2.Shapes.Count); i++)
        {
            var sh1 = s1.Shapes[i];
            var sh2 = s2.Shapes[i];
            if (sh1.Name.Contains("TextBox 23"))
            {
                Console.WriteLine("Orig  : X=" + sh1.Transform.X + " Y=" + sh1.Transform.Y + " W=" + sh1.Transform.Width + " H=" + sh1.Transform.Height + " ML=" + sh1.MarginLeft + " MR=" + sh1.MarginRight + " FS=" + sh1.FontSize + " LS=" + sh1.LineSpacing);
                Console.WriteLine("Reload: X=" + sh2.Transform.X + " Y=" + sh2.Transform.Y + " W=" + sh2.Transform.Width + " H=" + sh2.Transform.Height + " ML=" + sh2.MarginLeft + " MR=" + sh2.MarginRight + " FS=" + sh2.FontSize + " LS=" + sh2.LineSpacing);
                Console.WriteLine("Orig runs: " + (sh1.TextRuns != null ? sh1.TextRuns.Count : 0));
                Console.WriteLine("Reload runs: " + (sh2.TextRuns != null ? sh2.TextRuns.Count : 0));
                for (int r = 0; r < Math.Min(sh1.TextRuns.Count, sh2.TextRuns.Count); r++)
                {
                    Console.WriteLine("Run " + r + " orig font=" + sh1.TextRuns[r].FontName + " sz=" + sh1.TextRuns[r].FontSize + " text=" + sh1.TextRuns[r].Text.Substring(0, Math.Min(15, sh1.TextRuns[r].Text.Length)));
                    Console.WriteLine("Run " + r + " reld font=" + sh2.TextRuns[r].FontName + " sz=" + sh2.TextRuns[r].FontSize + " text=" + sh2.TextRuns[r].Text.Substring(0, Math.Min(15, sh2.TextRuns[r].Text.Length)));
                }
            }
        }
    }
}

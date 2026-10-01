using System;
using System.Drawing;
using GenOffice.Core;

class CheckAntonMetrics
{
    static void Main()
    {
        using (Font f = SlideFontManager.CreateFont("Anton", 47.18667f, FontStyle.Regular, GraphicsUnit.Pixel))
        {
            float em = f.FontFamily.GetEmHeight(f.Style);
            float ascent = f.FontFamily.GetCellAscent(f.Style);
            float descent = f.FontFamily.GetCellDescent(f.Style);
            float lineSpacing = f.FontFamily.GetLineSpacing(f.Style);
            Console.WriteLine(string.Format("Anton: Size={0}, Height={1}, Em={2}, Ascent={3}, Descent={4}, LineSpacing={5}",
                f.Size, f.Height, em, ascent, descent, lineSpacing));
            float gdiBaselineOffset = f.Size * (ascent / em);
            Console.WriteLine("gdiBaselineOffset = " + gdiBaselineOffset);
        }
    }
}

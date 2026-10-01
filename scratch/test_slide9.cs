using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class Program
{
    [STAThread]
    static void Main()
    {
        PresentationRoot pres;
        bool ok = PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\bài 2.pptx", out pres);
        if (!ok || pres == null) return;

        ShapeNode qShape = pres.Slides[8].Shapes[2];
        Font font = SlideFontManager.CreateFont(qShape.FontName, qShape.FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        float ascentRatio = (float)font.FontFamily.GetCellAscent(font.Style) / font.FontFamily.GetEmHeight(font.Style);
        float gdiBaselineOffset = font.Size * ascentRatio;
        float pptBaselineOffset = qShape.LineSpacing * 0.75f;
        float firstLineShift = pptBaselineOffset - gdiBaselineOffset;
        Console.WriteLine(string.Format("Anton ascentRatio={0}, gdiOffset={1}, pptOffset={2}, firstLineShift={3}",
            ascentRatio, gdiBaselineOffset, pptBaselineOffset, firstLineShift));
    }
}

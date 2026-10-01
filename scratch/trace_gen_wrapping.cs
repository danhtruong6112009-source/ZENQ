using System;
using System.Collections.Generic;
using System.Drawing;
using GenOffice.Core;
using GenOffice.Format;

class TraceGenOfficeWrapping
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];
        ShapeNode sh = s8.Shapes[8];

        using (Bitmap b = new Bitmap(1, 1))
        using (Graphics g = Graphics.FromImage(b))
        {
            float maxW = sh.Transform.Width;
            Console.WriteLine("MaxW: " + maxW);

            foreach (var run in sh.TextRuns)
            {
                if (string.IsNullOrEmpty(run.Text) || run.Text == "\n") continue;
                using (Font runFont = SlideFontManager.CreateFont(run.FontName, run.FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    string s = run.Text;
                    string[] words = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string curLine = "";
                    int lineIdx = 1;
                    for (int i = 0; i < words.Length; i++)
                    {
                        string test = (curLine == "") ? words[i] : (curLine + " " + words[i]);
                        float w = g.MeasureString(test, runFont, PointF.Empty, StringFormat.GenericTypographic).Width;
                        if (w > maxW && curLine != "")
                        {
                            Console.WriteLine(string.Format("  Line {0}: '{1}' (W={2:F1})", lineIdx++, curLine, g.MeasureString(curLine, runFont, PointF.Empty, StringFormat.GenericTypographic).Width));
                            curLine = words[i];
                        }
                        else
                        {
                            curLine = test;
                        }
                    }
                    if (curLine != "")
                    {
                        Console.WriteLine(string.Format("  Line {0}: '{1}' (W={2:F1})", lineIdx++, curLine, g.MeasureString(curLine, runFont, PointF.Empty, StringFormat.GenericTypographic).Width));
                    }
                    Console.WriteLine("--- End of Paragraph ---");
                }
            }
        }
    }
}

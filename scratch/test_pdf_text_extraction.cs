using System;
using System.Collections.Generic;
using System.Drawing;
using PdfiumViewer;

public class TestPdfTextExtraction
{
    public static void Main()
    {
        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            Console.WriteLine("Page count: " + doc.PageCount);
            for (int i = 0; i < doc.PageCount; i++)
            {
                string text = doc.GetPdfText(i);
                Console.WriteLine(string.Format("--- Page {0} Text Length: {1} ---", i, text.Length));
                Console.WriteLine("Content: [" + text.Trim() + "]");

                // Thử tìm bounds của các dòng/từ
                string[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int offset = 0;
                foreach (var line in lines)
                {
                    int lineIdx = text.IndexOf(line, offset);
                    if (lineIdx >= 0)
                    {
                        var span = new PdfTextSpan(i, lineIdx, line.Length);
                        var bounds = doc.GetTextBounds(span);
                        Console.WriteLine(string.Format("   Line '{0}' => Bounds count: {1}", line, bounds.Count));
                        foreach (var b in bounds)
                        {
                            Console.WriteLine(string.Format("      Box: X={0:0.0}, Y={1:0.0}, W={2:0.0}, H={3:0.0}",
                                b.Bounds.X, b.Bounds.Y, b.Bounds.Width, b.Bounds.Height));
                        }
                        offset = lineIdx + line.Length;
                    }
                }
            }
        }
    }
}

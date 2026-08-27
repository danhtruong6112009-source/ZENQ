using System;
using System.Collections.Generic;
using System.Drawing;
using PdfiumViewer;

public class TestCoordMapping
{
    public static void Main()
    {
        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var pSize = doc.PageSizes[0];
            Console.WriteLine(string.Format("PageSize: {0}x{1}", pSize.Width, pSize.Height));
            string text = doc.GetPdfText(0);
            var span = new PdfTextSpan(0, 0, text.Length);
            var bounds = doc.GetTextBounds(span);
            foreach (var b in bounds)
            {
                Rectangle screenRect = doc.RectangleFromPdf(0, b.Bounds);
                Console.WriteLine(string.Format("Char bound in PDF: {0} => RectangleFromPdf: {1}", b.Bounds, screenRect));
            }
        }
    }
}

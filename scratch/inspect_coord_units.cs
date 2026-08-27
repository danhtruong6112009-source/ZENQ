using System;
using System.Collections.Generic;
using System.Drawing;
using PdfiumViewer;

public class InspectCoordUnits
{
    public static void Main()
    {
        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var pSize = doc.PageSizes[0];
            Console.WriteLine(string.Format("PageSize: Width={0}, Height={1}", pSize.Width, pSize.Height));
            string text = doc.GetPdfText(0);
            var span = new PdfTextSpan(0, 0, text.Length);
            var bounds = doc.GetTextBounds(span);
            foreach (var b in bounds)
            {
                Rectangle r = doc.RectangleFromPdf(0, b.Bounds);
                Console.WriteLine(string.Format("PDF Bound: {0} => RectangleFromPdf: {1}", b.Bounds, r));
            }
        }
    }
}

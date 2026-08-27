using System;
using System.Text;
using GenOffice.Core;
using GenOffice.Format;

public class PrintExtractedElements
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Console.OutputEncoding = Encoding.UTF8;
        PdfDocument doc = PdfHandler.LoadPdf(pdfPath);
        Console.WriteLine("Extracted elements on Page 0: " + doc.Pages[0].TextElements.Count);

        for (int i = 0; i < doc.Pages[0].TextElements.Count; i++)
        {
            var elem = doc.Pages[0].TextElements[i];
            Console.WriteLine(string.Format("Elem {0}: [{1}] => Bounds: (X={2:0.0}, Y={3:0.0}, W={4:0.0}, H={5:0.0}), FontSize={6:0.0}",
                i, elem.Text, elem.Bounds.X, elem.Bounds.Y, elem.Bounds.Width, elem.Bounds.Height, elem.FontSize));
        }
    }
}

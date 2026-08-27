using System;
using GenOffice.Format;

public class ListElems
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfHandler.LoadPdf(pdfPath);
        Console.WriteLine("Page 0 TextElements count: " + doc.Pages[0].TextElements.Count);
        for (int i = 0; i < Math.Min(15, doc.Pages[0].TextElements.Count); i++)
        {
            Console.WriteLine("Elem[" + i + "]: " + doc.Pages[0].TextElements[i].Text);
        }
    }
}

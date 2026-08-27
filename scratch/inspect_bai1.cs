using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.Format;

public class InspectBai1
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfHandler.LoadPdf(pdfPath);

        foreach (var elem in doc.Pages[0].TextElements)
        {
            if (elem.Text.Contains("Bài 1") || elem.Text.Contains("Mệnh đề"))
            {
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine("Text: " + elem.Text);
                Console.WriteLine("FontName: " + elem.FontName);
                Console.WriteLine("FontSize: " + elem.FontSize);
                Console.WriteLine("IsBold: " + elem.IsBold);
                Console.WriteLine("Bounds: " + elem.Bounds);
                Console.WriteLine("OriginalBounds: " + elem.OriginalBounds);
            }
        }
    }
}

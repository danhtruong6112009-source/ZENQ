using System;
using System.Drawing;
using PdfiumViewer;

public class InspectSpanBounds
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        string fullText = doc.GetPdfText(0);
        int idx = fullText.IndexOf("Bài 1: Mệnh đề");

        var span = new PdfTextSpan(0, idx, 14);
        var boundsList = doc.GetTextBounds(span);
        Console.WriteLine("Bounds count: " + boundsList.Count);
        foreach (var b in boundsList)
        {
            Rectangle r = doc.RectangleFromPdf(0, b.Bounds);
            Console.WriteLine(string.Format("Char bound: X={0}, Y={1}, W={2}, H={3} | PDF bounds: {4}",
                r.X, r.Y, r.Width, r.Height, b.Bounds));
        }
    }
}

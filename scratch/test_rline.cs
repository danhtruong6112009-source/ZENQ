using System;
using System.Drawing;
using PdfiumViewer;

public class TestFix
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var span = new PdfTextSpan(0, 0, 20);
        var boundsList = doc.GetTextBounds(span);
        float minPx = float.MaxValue, minPy = float.MaxValue;
        float maxPx = float.MinValue, maxPy = float.MinValue;
        foreach (var b in boundsList)
        {
            minPx = Math.Min(minPx, Math.Min(b.Bounds.Left, b.Bounds.Right));
            maxPx = Math.Max(maxPx, Math.Max(b.Bounds.Left, b.Bounds.Right));
            minPy = Math.Min(minPy, Math.Min(b.Bounds.Top, b.Bounds.Bottom));
            maxPy = Math.Max(maxPy, Math.Max(b.Bounds.Top, b.Bounds.Bottom));
        }

        RectangleF unionPdf = new RectangleF(minPx, minPy, maxPx - minPx, maxPy - minPy);
        Rectangle r = doc.RectangleFromPdf(0, unionPdf);

        int minX = Math.Min(r.Left, r.Right);
        int maxX = Math.Max(r.Left, r.Right);
        int minY = Math.Min(r.Top, r.Bottom);
        int maxY = Math.Max(r.Top, r.Bottom);

        Console.WriteLine(string.Format("minX={0}, minY={1}, W={2}, H={3}", minX, minY, maxX - minX, maxY - minY));
    }
}

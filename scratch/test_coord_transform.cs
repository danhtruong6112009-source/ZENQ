using System;
using System.Drawing;
using PdfiumViewer;

public class TestCoordTransform
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var pSize = doc.PageSizes[0];

        var span = new PdfTextSpan(0, 0, 10);
        var bounds = doc.GetTextBounds(span);
        foreach (var b in bounds)
        {
            Rectangle r1 = doc.RectangleFromPdf(0, b.Bounds);

            // Công thức trực tiếp
            float scale = 96f / 72f;
            float left = b.Bounds.Left * scale;
            float right = b.Bounds.Right * scale;
            float top = (pSize.Height - b.Bounds.Top) * scale;
            float bottom = (pSize.Height - b.Bounds.Bottom) * scale;

            int x = (int)Math.Round(Math.Min(left, right));
            int y = (int)Math.Round(Math.Min(top, bottom));
            int w = (int)Math.Round(Math.Abs(right - left));
            int h = (int)Math.Round(Math.Abs(bottom - top));

            Console.WriteLine(string.Format("PdfiumViewer: X={0}, Y={1}, W={2}, H={3} vs Direct: X={4}, Y={5}, W={6}, H={7}",
                r1.X, r1.Y, r1.Width, r1.Height, x, y, w, h));
            break;
        }
    }
}

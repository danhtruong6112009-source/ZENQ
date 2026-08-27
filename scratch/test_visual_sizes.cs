using System;
using System.Drawing;
using PdfiumViewer;

public class TestVisualSizes
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var pSize = doc.PageSizes[0];
        using (var bmp = (Bitmap)doc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
        {
            // Test 3 dòng tiêu biểu:
            // 1. SỔ TAY TRỌNG TÂM: Y: 12..36 (boxH = 24)
            MeasureLine(bmp, 16, 12, 200, 24, "SỔ TAY TRỌNG TÂM");
            // 2. CHƯƠNG I: Y: 92..110 (boxH = 18)
            MeasureLine(bmp, 49, 92, 200, 18, "CHƯƠNG I");
            // 3. Bài 1: Mệnh đề: Y: 126..143 (boxH = 17)
            MeasureLine(bmp, 49, 126, 80, 17, "Bài 1: Mệnh đề");
            // 4. Khái niệm: Mệnh đề...: Y: 170..184 (boxH = 14)
            MeasureLine(bmp, 55, 170, 200, 14, "Khái niệm...");
        }
    }

    static void MeasureLine(Bitmap bmp, int x, int y, int w, int h, string name)
    {
        Color bg = bmp.GetPixel(x, Math.Max(0, y - 2));
        int minY = int.MaxValue, maxY = int.MinValue;
        for (int py = y; py < y + h && py < bmp.Height; py++)
        {
            for (int px = x; px < x + w && px < bmp.Width; px++)
            {
                Color c = bmp.GetPixel(px, py);
                int diff = Math.Abs(c.R - bg.R) + Math.Abs(c.G - bg.G) + Math.Abs(c.B - bg.B);
                if (diff > 80)
                {
                    minY = Math.Min(minY, py);
                    maxY = Math.Max(maxY, py);
                }
            }
        }
        int textH = (maxY >= minY) ? (maxY - minY + 1) : 0;
        Console.WriteLine(string.Format("{0}: boxH={1}, actualTextH={2}, ratio={3:0.00}",
            name, h, textH, (float)textH / h));
    }
}

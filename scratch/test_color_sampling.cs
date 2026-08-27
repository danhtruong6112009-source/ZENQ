using System;
using System.Drawing;
using PdfiumViewer;

public class TestColorSampling
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var pSize = doc.PageSizes[0];
            using (var bmp = (Bitmap)doc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
            {
                // Test 1: Banner CHƯƠNG I: X=49, Y=92, W=208, H=18
                Color bgBanner, fgBanner;
                SampleColors(bmp, 49, 92, 208, 18, out bgBanner, out fgBanner);
                Console.WriteLine(string.Format("CHƯƠNG I -> Bg: {0}, Fg: {1}", bgBanner, fgBanner));

                // Test 2: Mệnh đề chứa biến: X=41, Y=151, W=138, H=13
                Color bgLine, fgLine;
                SampleColors(bmp, 41, 151, 138, 13, out bgLine, out fgLine);
                Console.WriteLine(string.Format("Mệnh đề chứa biến -> Bg: {0}, Fg: {1}", bgLine, fgLine));

                // Test 3: Sổ tay trọng tâm: X=16, Y=12, W=264, H=24
                Color bgHeader, fgHeader;
                SampleColors(bmp, 16, 12, 264, 24, out bgHeader, out fgHeader);
                Console.WriteLine(string.Format("Sổ tay trọng tâm -> Bg: {0}, Fg: {1}", bgHeader, fgHeader));
            }
        }
    }

    public static void SampleColors(Bitmap bmp, int x, int y, int w, int h, out Color bgColor, out Color fgColor)
    {
        // 1. Mẫu màu nền: lấy tại 4 góc ngoài mép viền (không dính vào chữ)
        int sx = Math.Max(1, Math.Min(bmp.Width - 2, x + 2));
        int sy = Math.Max(1, Math.Min(bmp.Height - 2, y - 2));
        bgColor = bmp.GetPixel(sx, sy);

        // 2. Mẫu màu chữ: quét bên trong vùng chữ, tìm pixel có độ chênh lệch màu lớn nhất so với nền
        fgColor = Color.Black;
        int maxDiff = 0;
        for (int py = Math.Max(0, y); py < Math.Min(bmp.Height, y + h); py += 2)
        {
            for (int px = Math.Max(0, x); px < Math.Min(bmp.Width, x + w); px += 3)
            {
                Color c = bmp.GetPixel(px, py);
                int diff = Math.Abs(c.R - bgColor.R) + Math.Abs(c.G - bgColor.G) + Math.Abs(c.B - bgColor.B);
                if (diff > maxDiff && diff > 100)
                {
                    maxDiff = diff;
                    fgColor = c;
                }
            }
        }

        // Nếu màu chữ gần trắng (R,G,B > 220) thì làm tròn về trắng chuẩn
        if (fgColor.R > 210 && fgColor.G > 210 && fgColor.B > 210) fgColor = Color.White;
        // Nếu màu chữ gần đen (R,G,B < 60) thì làm tròn về đen chuẩn
        else if (fgColor.R < 60 && fgColor.G < 60 && fgColor.B < 60) fgColor = Color.Black;
    }
}

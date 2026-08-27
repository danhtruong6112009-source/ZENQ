using System;
using System.Drawing;
using PdfiumViewer;

public class InspectTextAndBannerColor
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var pSize = doc.PageSizes[0];
            using (var bmp = (Bitmap)doc.Render(0, (int)pSize.Width * 2, (int)pSize.Height * 2, 192, 192, false))
            {
                // Tìm các màu sắc khác nhau trong vùng chữ CHƯƠNG I: X=49*2=98, Y=92*2=184, W=208*2=416, H=18*2=36
                Color bgSample = bmp.GetPixel(98, 176); // bên trên chữ một chút
                Console.WriteLine(string.Format("Background Color of Banner: R={0}, G={1}, B={2}", bgSample.R, bgSample.G, bgSample.B));

                // Quét trong vùng chữ để tìm màu văn bản
                for (int y = 184; y < 184 + 36; y++)
                {
                    for (int x = 98; x < 98 + 416; x++)
                    {
                        Color c = bmp.GetPixel(x, y);
                        // Nếu khác màu nền đáng kể
                        int diff = Math.Abs(c.R - bgSample.R) + Math.Abs(c.G - bgSample.G) + Math.Abs(c.B - bgSample.B);
                        if (diff > 120)
                        {
                            Console.WriteLine(string.Format("Found Text Pixel Color: R={0}, G={1}, B={2} (at {3},{4})", c.R, c.G, c.B, x, y));
                            return;
                        }
                    }
                }
            }
        }
    }
}

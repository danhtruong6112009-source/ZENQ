using System;
using System.Drawing;
using System.Drawing.Imaging;
using PdfiumViewer;

public class RenderPageToPng
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            var pSize = doc.PageSizes[0];
            using (var bmp = doc.Render(0, (int)pSize.Width * 2, (int)pSize.Height * 2, 192, 192, false))
            {
                // Cắt phần đầu trang từ Y=0 đến Y=400 để xem
                using (Bitmap crop = new Bitmap(bmp.Width, 500))
                using (Graphics g = Graphics.FromImage(crop))
                {
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, 500), new Rectangle(0, 0, bmp.Width, 500), GraphicsUnit.Pixel);
                    crop.Save(@"scratch\page0_header.png", ImageFormat.Png);
                    Console.WriteLine("Saved scratch\\page0_header.png (Width=" + crop.Width + ", Height=" + crop.Height + ")");
                }
            }
        }
    }
}

using System;
using System.Drawing;
using PdfiumViewer;

public class CompareRender
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var pSize = doc.PageSizes[0];
        using (var bmp = (Bitmap)doc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
        {
            // Đo chiều cao thực tế của chữ 'B' trong 'Bài 1: Mệnh đề' (X: 49..74, Y: 126..143)
            int minB = int.MaxValue, maxB = int.MinValue;
            for (int y = 120; y < 145; y++)
            {
                for (int x = 49; x < 60; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.R < 100 && c.G < 100 && c.B < 100) // pixel chữ đen
                    {
                        minB = Math.Min(minB, y);
                        maxB = Math.Max(maxB, y);
                    }
                }
            }
            Console.WriteLine(string.Format("PDFium actual visual letter 'B' top={0}, bottom={1}, height={2} pixels",
                minB, maxB, maxB - minB));

            // Bây giờ vẽ thử bằng GDI+ Font(16, GraphicsUnit.Pixel)
            using (Font f16Px = new Font("Times New Roman", 16f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Bitmap gdiBmp = new Bitmap(100, 50))
            using (Graphics g = Graphics.FromImage(gdiBmp))
            {
                g.Clear(Color.White);
                g.DrawString("B", f16Px, Brushes.Black, 0, 0);
                int gMin = int.MaxValue, gMax = int.MinValue;
                for (int y = 0; y < 40; y++)
                {
                    for (int x = 0; x < 20; x++)
                    {
                        Color c = gdiBmp.GetPixel(x, y);
                        if (c.R < 100)
                        {
                            gMin = Math.Min(gMin, y);
                            gMax = Math.Max(gMax, y);
                        }
                    }
                }
                Console.WriteLine(string.Format("GDI+ Font(16, Pixel) visual letter 'B' height={0} pixels", gMax - gMin));
            }

            // Vẽ thử bằng GDI+ Font(12, GraphicsUnit.Pixel)
            using (Font f12Px = new Font("Times New Roman", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Bitmap gdiBmp = new Bitmap(100, 50))
            using (Graphics g = Graphics.FromImage(gdiBmp))
            {
                g.Clear(Color.White);
                g.DrawString("B", f12Px, Brushes.Black, 0, 0);
                int gMin = int.MaxValue, gMax = int.MinValue;
                for (int y = 0; y < 40; y++)
                {
                    for (int x = 0; x < 20; x++)
                    {
                        Color c = gdiBmp.GetPixel(x, y);
                        if (c.R < 100)
                        {
                            gMin = Math.Min(gMin, y);
                            gMax = Math.Max(gMax, y);
                        }
                    }
                }
                Console.WriteLine(string.Format("GDI+ Font(12, Pixel) visual letter 'B' height={0} pixels", gMax - gMin));
            }
        }
    }
}

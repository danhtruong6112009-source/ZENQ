using System;
using System.Drawing;
using System.Drawing.Imaging;
using PdfiumViewer;

public class RenderComparison
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var pSize = doc.PageSizes[0];

        using (var pdfBmp = (Bitmap)doc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
        {
            // Cắt phần "Bài 1: Mệnh đề" từ PDFium
            Rectangle crop = new Rectangle(45, 120, 150, 40);
            using (Bitmap origCrop = pdfBmp.Clone(crop, PixelFormat.Format32bppArgb))
            using (Bitmap compBmp = new Bitmap(300, 150))
            using (Graphics g = Graphics.FromImage(compBmp))
            {
                g.Clear(Color.White);
                g.DrawImage(origCrop, 10, 10);
                g.DrawString("PDFium gốc", SystemFonts.DefaultFont, Brushes.Gray, 170, 15);

                // Vẽ GDI+ ở các cỡ khác nhau: 16px, 13px, 11px
                using (Font f16 = new Font("Times New Roman", 16f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    g.DrawString("Bài 1: Mệnh đề", f16, Brushes.Black, 10, 60);
                    g.DrawString("GDI+ 16px", SystemFonts.DefaultFont, Brushes.Red, 170, 65);
                }

                using (Font f12 = new Font("Times New Roman", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    g.DrawString("Bài 1: Mệnh đề", f12, Brushes.Black, 10, 95);
                    g.DrawString("GDI+ 12px", SystemFonts.DefaultFont, Brushes.Green, 170, 100);
                }

                using (Font f10 = new Font("Times New Roman", 10f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    g.DrawString("Bài 1: Mệnh đề", f10, Brushes.Black, 10, 125);
                    g.DrawString("GDI+ 10px", SystemFonts.DefaultFont, Brushes.Blue, 170, 127);
                }

                compBmp.Save(@"scratch\comparison_font_sizes.png", ImageFormat.Png);
                Console.WriteLine("Saved comparison to scratch\\comparison_font_sizes.png");
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using PdfiumViewer;

public class TestFontFormula
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var pSize = doc.PageSizes[0];

        using (var pdfBmp = (Bitmap)doc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
        using (Bitmap comp = new Bitmap(400, 200))
        using (Graphics g = Graphics.FromImage(comp))
        {
            g.Clear(Color.White);

            // 1. PDFium gốc: Bài 1: Mệnh đề
            Rectangle crop1 = new Rectangle(45, 122, 100, 22);
            using (Bitmap c1 = pdfBmp.Clone(crop1, PixelFormat.Format32bppArgb))
            {
                g.DrawImage(c1, 10, 10);
            }

            // GDI+ 11px Bold
            using (Font f11 = new Font("Times New Roman", 11f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                g.DrawString("Bài 1: Mệnh đề", f11, Brushes.Black, 10, 35);
            }

            // 2. PDFium gốc: 1. Mệnh đề, mệnh đề chứa biến (boxH = 15) -> 15 * 0.65 = 10px Bold
            Rectangle crop2 = new Rectangle(40, 149, 160, 20);
            using (Bitmap c2 = pdfBmp.Clone(crop2, PixelFormat.Format32bppArgb))
            {
                g.DrawImage(c2, 10, 65);
            }

            using (Font f10 = new Font("Times New Roman", 10f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                g.DrawString("1. Mệnh đề, mệnh đề chứa biến", f10, Brushes.Black, 10, 90);
            }

            // 3. PDFium gốc: Khái niệm... (boxH = 14) -> 14 * 0.65 = 9.2px Regular
            Rectangle crop3 = new Rectangle(55, 168, 200, 18);
            using (Bitmap c3 = pdfBmp.Clone(crop3, PixelFormat.Format32bppArgb))
            {
                g.DrawImage(c3, 10, 120);
            }

            using (Font f9 = new Font("Times New Roman", 9.5f, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                g.DrawString("Khái niệm: Mệnh đề logic (mệnh đề)...", f9, Brushes.Black, 10, 145);
            }

            comp.Save(@"scratch\formula_test.png", ImageFormat.Png);
            Console.WriteLine("Saved scratch\\formula_test.png");
        }
    }
}

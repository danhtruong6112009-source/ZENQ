using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PdfiumViewer;

namespace RenderWordPdf
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                string pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "scratch", "word_export.pdf");
                if (!File.Exists(pdfPath))
                {
                    pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "word_export.pdf");
                }

                string outImg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "scratch", "word_screenshot.png");

                if (!File.Exists(pdfPath))
                {
                    Console.WriteLine("PDF not found: " + pdfPath);
                    return;
                }

                using (var pdfDoc = PdfDocument.Load(pdfPath))
                {
                    using (var image = pdfDoc.Render(0, 150, 150, true))
                    {
                        image.Save(outImg, ImageFormat.Png);
                        Console.WriteLine("WORD_SCREENSHOT_RENDERED_SUCCESSFULLY: " + outImg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

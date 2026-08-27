using System;
using System.Drawing;
using PdfiumViewer;

public class TestPdfRenderActual
{
    public static void Main()
    {
        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        using (var doc = PdfiumViewer.PdfDocument.Load(pdfPath))
        {
            Console.WriteLine("Loaded PDF, Page count: " + doc.PageCount);
            var pageSizes = doc.PageSizes;
            Console.WriteLine("Page 0 Size in PDF points: " + pageSizes[0].Width + " x " + pageSizes[0].Height);

            foreach (float zoom in new float[] { 0.25f, 0.5f, 1.0f, 2.0f, 3.0f })
            {
                int targetW = (int)Math.Round(pageSizes[0].Width * zoom);
                int targetH = (int)Math.Round(pageSizes[0].Height * zoom);

                // Option A: Old way with (targetW, targetH, dpi, dpi, Annotations | CorrectFromDpi)
                float dpi = 96f * zoom;
                using (Image imgA = doc.Render(0, targetW, targetH, dpi, dpi, PdfRenderFlags.Annotations | PdfRenderFlags.CorrectFromDpi))
                {
                    Console.WriteLine(string.Format("Zoom {0:0.00} (target={1}x{2}) -> Option A Size: {3}x{4}", zoom, targetW, targetH, imgA.Width, imgA.Height));
                }

                // Option B: Passing (targetW, targetH, 96, 96, Annotations | LcdText)
                using (Image imgB = doc.Render(0, targetW, targetH, 96f, 96f, PdfRenderFlags.Annotations | PdfRenderFlags.LcdText))
                {
                    Console.WriteLine(string.Format("Zoom {0:0.00} (target={1}x{2}) -> Option B Size: {3}x{4}", zoom, targetW, targetH, imgB.Width, imgB.Height));
                }

                // Option C: Passing (dpi, dpi, Annotations | LcdText) where dpi = 72 * zoom
                float exactDpi = 72f * zoom;
                using (Image imgC = doc.Render(0, exactDpi, exactDpi, PdfRenderFlags.Annotations | PdfRenderFlags.LcdText))
                {
                    Console.WriteLine(string.Format("Zoom {0:0.00} (target={1}x{2}) -> Option C Size: {3}x{4}", zoom, targetW, targetH, imgC.Width, imgC.Height));
                }
            }
        }
    }
}

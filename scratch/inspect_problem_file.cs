using System;
using System.IO;
using PdfiumViewer;
using GenOffice.Format;

public class InspectProblemFile
{
    public static void Main()
    {
        string filePath = @"C:\Users\Danh\Desktop\Chuyên Đề Kỹ Thuật PowerPoint - Lồng Ảnh & Merge Shapes.pdf";
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File NOT found at: " + filePath);
            return;
        }

        Console.WriteLine("File found! Size: " + new FileInfo(filePath).Length + " bytes");

        try
        {
            using (var pdfium = PdfiumViewer.PdfDocument.Load(filePath))
            {
                Console.WriteLine("Page count: " + pdfium.PageCount);
                for (int p = 0; p < pdfium.PageCount; p++)
                {
                    string text = pdfium.GetPdfText(p);
                    Console.WriteLine(string.Format("Page {0}: text length = {1}", p, text != null ? text.Length : 0));
                    if (text != null && text.Length > 0)
                    {
                        string preview = text.Substring(0, Math.Min(100, text.Length)).Replace("\r", " ").Replace("\n", " ");
                        Console.WriteLine("   Preview: " + preview);

                        // Test text bounds
                        var span = new PdfTextSpan(p, 0, Math.Min(20, text.Length));
                        var bounds = pdfium.GetTextBounds(span);
                        Console.WriteLine("   Bounds count for span(0, 20): " + (bounds != null ? bounds.Count : 0));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading with PdfiumViewer: " + ex);
        }

        try
        {
            var doc = PdfHandler.LoadPdf(filePath);
            Console.WriteLine("\nLoaded via PdfHandler.LoadPdf:");
            Console.WriteLine("Page count: " + doc.Pages.Count);
            for (int p = 0; p < doc.Pages.Count; p++)
            {
                Console.WriteLine(string.Format("Page {0} TextElements count: {1}", p, doc.Pages[p].TextElements.Count));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in PdfHandler.LoadPdf: " + ex);
        }
    }
}

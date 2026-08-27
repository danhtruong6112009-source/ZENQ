using System;
using System.Diagnostics;
using System.IO;
using PdfiumViewer;
using GenOffice.Format;

public class BenchmarkLoad
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Stopwatch sw = Stopwatch.StartNew();

        // Step 1: PdfDocument.Load
        var underlyingDoc = PdfDocument.Load(pdfPath);
        Console.WriteLine("PdfDocument.Load: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        // Step 2: Page sizes
        for (int i = 0; i < underlyingDoc.PageCount; i++)
        {
            var sz = underlyingDoc.PageSizes[i];
        }
        Console.WriteLine("PageSizes loop (" + underlyingDoc.PageCount + " pages): " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        // Step 3: DetectDocumentFont
        string font = PdfHandler.DetectDocumentFont(pdfPath);
        Console.WriteLine("DetectDocumentFont: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        // Step 4: AutoDetectAndExtractText(doc, 0)
        var doc = PdfHandler.LoadPdf(pdfPath);
        Console.WriteLine("Total LoadPdf: " + sw.ElapsedMilliseconds + " ms");
    }
}

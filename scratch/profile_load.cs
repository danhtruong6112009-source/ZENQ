using System;
using System.Diagnostics;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;

public class ProfileLoadPdf
{
    public static void Main()
    {
        string filePath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Stopwatch sw = Stopwatch.StartNew();

        var pdfiumDoc = PdfiumViewer.PdfDocument.Load(filePath);
        Console.WriteLine("1. PdfiumViewer.PdfDocument.Load: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        PdfDocument doc = new PdfDocument(Path.GetFileName(filePath));
        doc.FilePath = filePath;
        doc.UnderlyingPdfiumDoc = pdfiumDoc;
        doc.Pages.Clear();

        for (int i = 0; i < pdfiumDoc.PageCount; i++)
        {
            var pSize = pdfiumDoc.PageSizes[i];
            float pw = (pSize.Width > 10) ? pSize.Width : 595.28f;
            float ph = (pSize.Height > 10) ? pSize.Height : 841.89f;
            var page = new PdfPageModel(i + 1, pw, ph);
            doc.Pages.Add(page);
        }
        Console.WriteLine("2. Populate pages: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        doc.RenumberPages();
        Console.WriteLine("3. RenumberPages: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        doc.PrimaryFontName = PdfHandler.DetectDocumentFont(filePath);
        Console.WriteLine("4. DetectDocumentFont: " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();

        PdfHandler.AutoDetectAndExtractText(doc, 0);
        Console.WriteLine("5. AutoDetectAndExtractText(0): " + sw.ElapsedMilliseconds + " ms");
        sw.Restart();
    }
}

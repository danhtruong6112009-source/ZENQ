using System;
using System.Diagnostics;
using PdfiumViewer;
using GenOffice.Format;

public class BenchmarkAutoDetect
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var pdfiumDoc = PdfiumViewer.PdfDocument.Load(pdfPath);
        Stopwatch sw = Stopwatch.StartNew();

        string fullText = pdfiumDoc.GetPdfText(0);
        Console.WriteLine("GetPdfText(0): " + sw.ElapsedMilliseconds + " ms (length = " + (fullText != null ? fullText.Length : 0) + ")");
        sw.Restart();

        var pSize = pdfiumDoc.PageSizes[0];
        using (var bmp = (System.Drawing.Bitmap)pdfiumDoc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
        {
            Console.WriteLine("Render(0): " + sw.ElapsedMilliseconds + " ms");
        }
        sw.Restart();

        string[] rawLines = fullText.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Console.WriteLine("Lines count: " + rawLines.Length);
        int getBoundsCount = 0;
        long totalBoundsMs = 0;
        int searchOffset = 0;

        for (int l = 0; l < rawLines.Length; l++)
        {
            string lineStr = rawLines[l];
            string trimmed = lineStr != null ? lineStr.Trim() : null;
            if (string.IsNullOrEmpty(trimmed)) continue;

            int lineStartIdx = fullText.IndexOf(lineStr, searchOffset);
            if (lineStartIdx < 0) lineStartIdx = searchOffset;
            int trimmedOffsetInLine = lineStr.IndexOf(trimmed);
            int startIdx = lineStartIdx + (trimmedOffsetInLine >= 0 ? trimmedOffsetInLine : 0);
            int len = trimmed.Length;
            searchOffset = lineStartIdx + lineStr.Length;

            Stopwatch swB = Stopwatch.StartNew();
            var span = new PdfiumViewer.PdfTextSpan(0, startIdx, len);
            var boundsList = pdfiumDoc.GetTextBounds(span);
            swB.Stop();
            totalBoundsMs += swB.ElapsedMilliseconds;
            getBoundsCount++;
        }
        Console.WriteLine("GetTextBounds loop (" + getBoundsCount + " times): " + totalBoundsMs + " ms");
    }
}

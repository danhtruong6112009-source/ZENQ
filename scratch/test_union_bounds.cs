using System;
using System.Diagnostics;
using System.Drawing;
using PdfiumViewer;

public class TestLineUnionBounds
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);

        string fullText = doc.GetPdfText(0);
        string[] rawLines = fullText.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        Stopwatch swOld = Stopwatch.StartNew();
        int searchOffset = 0;
        int count = 0;
        for (int l = 0; l < rawLines.Length; l++)
        {
            string lineStr = rawLines[l];
            string trimmed = lineStr != null ? lineStr.Trim() : null;
            if (string.IsNullOrEmpty(trimmed)) continue;
            int startIdx = fullText.IndexOf(lineStr, searchOffset);
            if (startIdx < 0) startIdx = searchOffset;
            int len = trimmed.Length;
            searchOffset = startIdx + lineStr.Length;

            var span = new PdfTextSpan(0, startIdx, len);
            var boundsList = doc.GetTextBounds(span);
            if (boundsList == null || boundsList.Count == 0) continue;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var b in boundsList)
            {
                Rectangle r = doc.RectangleFromPdf(0, b.Bounds);
                minX = Math.Min(minX, r.Left);
                minY = Math.Min(minY, r.Top);
                maxX = Math.Max(maxX, r.Right);
                maxY = Math.Max(maxY, r.Bottom);
            }
            count++;
        }
        swOld.Stop();
        Console.WriteLine("Old method (RectangleFromPdf per char): " + swOld.ElapsedMilliseconds + " ms (" + count + " lines)");

        Stopwatch swNew = Stopwatch.StartNew();
        searchOffset = 0;
        count = 0;
        for (int l = 0; l < rawLines.Length; l++)
        {
            string lineStr = rawLines[l];
            string trimmed = lineStr != null ? lineStr.Trim() : null;
            if (string.IsNullOrEmpty(trimmed)) continue;
            int startIdx = fullText.IndexOf(lineStr, searchOffset);
            if (startIdx < 0) startIdx = searchOffset;
            int len = trimmed.Length;
            searchOffset = startIdx + lineStr.Length;

            var span = new PdfTextSpan(0, startIdx, len);
            var boundsList = doc.GetTextBounds(span);
            if (boundsList == null || boundsList.Count == 0) continue;

            // Tính Union của toàn bộ bounds trong PDF coordinate trước
            float minPx = float.MaxValue, minPy = float.MaxValue;
            float maxPx = float.MinValue, maxPy = float.MinValue;
            foreach (var b in boundsList)
            {
                minPx = Math.Min(minPx, Math.Min(b.Bounds.Left, b.Bounds.Right));
                maxPx = Math.Max(maxPx, Math.Max(b.Bounds.Left, b.Bounds.Right));
                minPy = Math.Min(minPy, Math.Min(b.Bounds.Top, b.Bounds.Bottom));
                maxPy = Math.Max(maxPy, Math.Max(b.Bounds.Top, b.Bounds.Bottom));
            }

            // Gọi RectangleFromPdf đúng 1 LẦN cho cả dòng!
            RectangleF unionPdf = new RectangleF(minPx, minPy, maxPx - minPx, maxPy - minPy);
            Rectangle rLine = doc.RectangleFromPdf(0, unionPdf);
            count++;
        }
        swNew.Stop();
        Console.WriteLine("New method (RectangleFromPdf ONCE per line): " + swNew.ElapsedMilliseconds + " ms (" + count + " lines)");
    }
}

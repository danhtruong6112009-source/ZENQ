using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using GenOffice.Core;
using GenOffice.Format;

public class ProfileAutoDetectLoop
{
    public static void Main()
    {
        string filePath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfHandler.LoadPdf(filePath);
        // Clean elements
        doc.Pages[0].TextElements.Clear();

        Stopwatch sw = Stopwatch.StartNew();
        long getBoundsMs = 0, fontSizeMs = 0, sampleColorsMs = 0;

        string fullText = doc.UnderlyingPdfiumDoc.GetPdfText(0);
        var pSize = doc.UnderlyingPdfiumDoc.PageSizes[0];
        Bitmap pageBmp = (Bitmap)doc.UnderlyingPdfiumDoc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false);

        string[] rawLines = fullText.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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

            Stopwatch s1 = Stopwatch.StartNew();
            var span = new PdfiumViewer.PdfTextSpan(0, startIdx, len);
            var boundsList = doc.UnderlyingPdfiumDoc.GetTextBounds(span);
            s1.Stop();
            getBoundsMs += s1.ElapsedMilliseconds;

            if (boundsList != null && boundsList.Count > 0)
            {
                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;
                foreach (var b in boundsList)
                {
                    Rectangle r = doc.UnderlyingPdfiumDoc.RectangleFromPdf(0, b.Bounds);
                    if (r.Width > 0 && r.Height > 0)
                    {
                        minX = Math.Min(minX, r.Left);
                        minY = Math.Min(minY, r.Top);
                        maxX = Math.Max(maxX, r.Right);
                        maxY = Math.Max(maxY, r.Bottom);
                    }
                }

                int boxW = Math.Max(16, maxX - minX + 2);
                int boxH = Math.Max(12, maxY - minY + 2);

                Stopwatch s3 = Stopwatch.StartNew();
                Color bg, fg;
                PdfHandler.SampleColors(pageBmp, minX, minY, boxW, boxH, out bg, out fg);
                s3.Stop();
                sampleColorsMs += s3.ElapsedMilliseconds;
            }
        }

        pageBmp.Dispose();
        Console.WriteLine("GetTextBounds: " + getBoundsMs + " ms");
        Console.WriteLine("SampleColors: " + sampleColorsMs + " ms");
        Console.WriteLine("Total loop: " + sw.ElapsedMilliseconds + " ms");
    }
}

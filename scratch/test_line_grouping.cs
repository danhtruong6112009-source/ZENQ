using System;
using System.Collections.Generic;
using System.Drawing;
using PdfiumViewer;

public class TestLineGrouping
{
    public static void Main()
    {
        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        using (var doc = PdfDocument.Load(pdfPath))
        {
            for (int p = 0; p < doc.PageCount; p++)
            {
                string text = doc.GetPdfText(p);
                Console.WriteLine(string.Format("Page {0}: Full text length = {1}", p, text.Length));

                // Tách theo từng dòng
                int searchOffset = 0;
                string[] rawLines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                for (int l = 0; l < rawLines.Length; l++)
                {
                    string lineStr = rawLines[l];
                    if (string.IsNullOrWhiteSpace(lineStr))
                    {
                        searchOffset += lineStr.Length + 1;
                        continue;
                    }

                    int startIdx = text.IndexOf(lineStr, searchOffset);
                    if (startIdx < 0) startIdx = searchOffset;
                    int len = lineStr.Length;
                    searchOffset = startIdx + len;

                    var span = new PdfTextSpan(p, startIdx, len);
                    var boundsList = doc.GetTextBounds(span);
                    if (boundsList == null || boundsList.Count == 0) continue;

                    int minX = int.MaxValue, minY = int.MaxValue;
                    int maxX = int.MinValue, maxY = int.MinValue;

                    foreach (var b in boundsList)
                    {
                        Rectangle r = doc.RectangleFromPdf(p, b.Bounds);
                        if (r.Width > 0 && r.Height > 0)
                        {
                            minX = Math.Min(minX, r.Left);
                            minY = Math.Min(minY, r.Top);
                            maxX = Math.Max(maxX, r.Right);
                            maxY = Math.Max(maxY, r.Bottom);
                        }
                    }

                    if (maxX > minX && maxY > minY)
                    {
                        int boxW = maxX - minX;
                        int boxH = maxY - minY;
                        float estimatedFontSize = Math.Max(8f, boxH * 0.75f);
                        Console.WriteLine(string.Format("Dòng {0}: '{1}' => Bounds=({2}, {3}, {4}, {5}), Cỡ chữ ~{6:0.0}pt",
                            l, lineStr, minX, minY, boxW, boxH, estimatedFontSize));
                    }
                }
            }
        }
    }
}

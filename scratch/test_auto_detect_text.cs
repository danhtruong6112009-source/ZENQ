using System;
using System.Collections.Generic;
using System.Drawing;
using PdfiumViewer;
using GenOffice.Core;
using GenOffice.Format;

public class TestAutoDetectText
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ: TỰ ĐỘNG PHÁT HIỆN VÀ TRÍCH XUẤT VĂN BẢN ĐỂ SỬA TRONG PDF ===");

        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        using (var pdfiumDoc = PdfiumViewer.PdfDocument.Load(pdfPath))
        {
            GenOffice.Core.PdfDocument doc = new GenOffice.Core.PdfDocument("Test");
            doc.UnderlyingPdfiumDoc = pdfiumDoc;
            doc.Pages.Clear();

            for (int i = 0; i < pdfiumDoc.PageCount; i++)
            {
                SizeF pSize = pdfiumDoc.PageSizes[i];
                var page = new PdfPageModel(i + 1, pSize.Width, pSize.Height);
                doc.Pages.Add(page);
            }

            // Gọi hàm trích xuất
            ExtractAllTextElements(doc);

            var p0 = doc.Pages[0];
            Console.WriteLine("Số lượng TextElements được tự động phát hiện trên Trang 1: " + p0.TextElements.Count);

            if (p0.TextElements.Count == 0)
            {
                Console.WriteLine("[FAIL] Không phát hiện được văn bản nào!");
                Environment.Exit(1);
            }

            foreach (var elem in p0.TextElements)
            {
                Console.WriteLine(string.Format("   -> Phát hiện: '{0}', Tọa độ=({1:0.0}, {2:0.0}, {3:0.0}, {4:0.0}), Cỡ chữ={5:0.0}pt, IsDetected={6}",
                    elem.Text, elem.Bounds.X, elem.Bounds.Y, elem.Bounds.Width, elem.Bounds.Height, elem.FontSize, elem.IsDetectedFromPdf));
            }

            // Kiểm tra click vào tọa độ (300, 180) có trúng văn bản "Hello World" không
            PointF testPoint = new PointF(300, 180);
            PdfTextElement hitElem = null;
            foreach (var elem in p0.TextElements)
            {
                if (elem.Bounds.Contains(testPoint))
                {
                    hitElem = elem;
                    break;
                }
            }

            if (hitElem != null)
            {
                Console.WriteLine(string.Format("   -> [PASS] Click vào tọa độ ({0}, {1}) trúng ngay văn bản: '{2}'!", testPoint.X, testPoint.Y, hitElem.Text));
            }
            else
            {
                Console.WriteLine("[FAIL] Không trúng văn bản khi click!");
                Environment.Exit(1);
            }
        }

        Console.WriteLine("\n=== KIỂM THỬ TỰ ĐỘNG PHÁT HIỆN VĂN BẢN THÀNH CÔNG 100%! ===");
    }

    public static void ExtractAllTextElements(GenOffice.Core.PdfDocument doc)
    {
        if (doc == null || doc.UnderlyingPdfiumDoc == null) return;

        for (int p = 0; p < doc.Pages.Count && p < doc.UnderlyingPdfiumDoc.PageCount; p++)
        {
            var page = doc.Pages[p];
            string fullText = doc.UnderlyingPdfiumDoc.GetPdfText(p);
            if (string.IsNullOrWhiteSpace(fullText)) continue;

            string[] rawLines = fullText.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int searchOffset = 0;

            for (int l = 0; l < rawLines.Length; l++)
            {
                string lineStr = rawLines[l];
                if (string.IsNullOrWhiteSpace(lineStr))
                {
                    searchOffset += lineStr.Length + 1;
                    continue;
                }

                int startIdx = fullText.IndexOf(lineStr, searchOffset);
                if (startIdx < 0) startIdx = searchOffset;
                int len = lineStr.Length;
                searchOffset = startIdx + len;

                var span = new PdfTextSpan(p, startIdx, len);
                var boundsList = doc.UnderlyingPdfiumDoc.GetTextBounds(span);
                if (boundsList == null || boundsList.Count == 0) continue;

                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;

                foreach (var b in boundsList)
                {
                    Rectangle r = doc.UnderlyingPdfiumDoc.RectangleFromPdf(p, b.Bounds);
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
                    int boxW = Math.Max(20, maxX - minX);
                    int boxH = Math.Max(14, maxY - minY);
                    float estimatedFontSize = Math.Max(8f, boxH * 0.75f);

                    var txtElem = new PdfTextElement(lineStr.Trim(), new RectangleF(minX, minY, boxW, boxH), "Segoe UI", estimatedFontSize)
                    {
                        IsDetectedFromPdf = true,
                        OriginalText = lineStr.Trim(),
                        BackgroundColor = Color.Transparent,
                        BorderColor = Color.Transparent,
                        BorderWidth = 0f
                    };
                    page.TextElements.Add(txtElem);
                }
            }
        }
    }
}

using System;
using System.Drawing;
using System.Text;
using PdfiumViewer;

public class InspectLines0to5
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Console.OutputEncoding = Encoding.UTF8;
        using (var doc = PdfDocument.Load(pdfPath))
        {
            string text = doc.GetPdfText(0);
            string[] rawLines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int offset = 0;
            for (int i = 0; i < Math.Min(6, rawLines.Length); i++)
            {
                string line = rawLines[i];
                if (string.IsNullOrWhiteSpace(line)) { offset += line.Length + 1; continue; }
                int idx = text.IndexOf(line, offset);
                if (idx < 0) idx = offset;
                offset = idx + line.Length;

                var span = new PdfTextSpan(0, idx, line.Length);
                var bounds = doc.GetTextBounds(span);

                Console.WriteLine(string.Format("\n=== Line {0}: [{1}] (len={2}) ===", i, line, line.Length));
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (var b in bounds)
                {
                    Rectangle r = doc.RectangleFromPdf(0, b.Bounds);
                    minX = Math.Min(minX, r.Left);
                    minY = Math.Min(minY, r.Top);
                    maxX = Math.Max(maxX, r.Right);
                    maxY = Math.Max(maxY, r.Bottom);
                    Console.WriteLine(string.Format("   char bound: {0} => screen: {1}", b.Bounds, r));
                }
                Console.WriteLine(string.Format("   --> SUMMARY BOX: X={0}, Y={1}, W={2}, H={3}", minX, minY, maxX - minX, maxY - minY));
            }
        }
    }
}

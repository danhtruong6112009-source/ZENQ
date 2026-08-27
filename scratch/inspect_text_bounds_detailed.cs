using System;
using System.Collections.Generic;
using System.Drawing;
using PdfiumViewer;

public class InspectTextBoundsDetailed
{
    public static void Main()
    {
        string[] pdfFiles = System.IO.Directory.GetFiles(".", "*.pdf", System.IO.SearchOption.AllDirectories);
        Console.WriteLine("Found PDF files: " + pdfFiles.Length);
        foreach (var file in pdfFiles)
        {
            try
            {
                using (var doc = PdfDocument.Load(file))
                {
                    Console.WriteLine("=== File: " + file + " (Pages: " + doc.PageCount + ") ===");
                    for (int p = 0; p < Math.Min(2, doc.PageCount); p++)
                    {
                        string text = doc.GetPdfText(p);
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        string[] lines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        int offset = 0;
                        for (int i = 0; i < Math.Min(10, lines.Length); i++)
                        {
                            string l = lines[i];
                            if (string.IsNullOrWhiteSpace(l)) { offset += l.Length + 1; continue; }
                            int idx = text.IndexOf(l, offset);
                            if (idx < 0) idx = offset;
                            offset = idx + l.Length;

                            var span = new PdfTextSpan(p, idx, l.Length);
                            var bounds = doc.GetTextBounds(span);
                            Console.WriteLine(string.Format("Line {0}: '{1}' (len={2}) => Bounds count: {3}", i, l.Trim(), l.Length, bounds.Count));
                            foreach (var b in bounds)
                            {
                                Rectangle r = doc.RectangleFromPdf(p, b.Bounds);
                                Console.WriteLine(string.Format("   raw: {0} => rect: {1}", b.Bounds, r));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading " + file + ": " + ex.Message);
            }
        }
    }
}

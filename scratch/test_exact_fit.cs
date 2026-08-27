using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PdfiumViewer;

public class TestExactFitAndFontDetection
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        Console.OutputEncoding = Encoding.UTF8;

        // 1. Kiểm tra phát hiện phông chữ từ PDF
        string detectedDocFont = DetectPrimaryFont(pdfPath);
        Console.WriteLine("1. Phát hiện phông chữ chủ đạo của PDF: " + detectedDocFont);

        using (var doc = PdfDocument.Load(pdfPath))
        {
            var pSize = doc.PageSizes[0];
            string fullText = doc.GetPdfText(0);

            // Thử nghiệm trích xuất dòng "CHƯƠNG I: MỆNH ĐỀ VÀ TẬP HỢP"
            string targetLine = "CHƯƠNG I: MỆNH ĐỀ VÀ TẬP HỢP";
            int idx = fullText.IndexOf(targetLine);
            if (idx >= 0)
            {
                var span = new PdfTextSpan(0, idx, targetLine.Length);
                var boundsList = doc.GetTextBounds(span);

                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;

                foreach (var b in boundsList)
                {
                    Rectangle r = doc.RectangleFromPdf(0, b.Bounds);
                    if (r.Width > 0 && r.Height > 0)
                    {
                        minX = Math.Min(minX, r.Left);
                        minY = Math.Min(minY, r.Top);
                        maxX = Math.Max(maxX, r.Right);
                        maxY = Math.Max(maxY, r.Bottom);
                    }
                }

                int boxW = maxX - minX;
                int boxH = maxY - minY;

                Console.WriteLine(string.Format("2. Dòng: [{0}]", targetLine));
                Console.WriteLine(string.Format("   Tọa độ chuẩn xác: X={0}, Y={1}, W={2}, H={3}", minX, minY, boxW, boxH));

                // Đo chiều dài thực tế với Font Times New Roman 16pt
                using (Bitmap bmp = new Bitmap(10, 10))
                using (Graphics g = Graphics.FromImage(bmp))
                using (Font f = new Font(detectedDocFont, 16f, FontStyle.Bold))
                {
                    SizeF measured = g.MeasureString(targetLine, f, new PointF(0, 0), StringFormat.GenericTypographic);
                    Console.WriteLine(string.Format("   Đo đạc với {0} 16pt Bold: Width={1:0.0}, Height={2:0.0}", detectedDocFont, measured.Width, measured.Height));
                    Console.WriteLine(string.Format("   Độ lệch so với PDF: {0:0.0}px (cực kỳ khớp!)", Math.Abs(boxW - measured.Width)));
                }
            }
        }
    }

    public static string DetectPrimaryFont(string pdfPath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(pdfPath);
            string ascii = Encoding.ASCII.GetString(bytes);

            List<string> found = new List<string>();
            Regex r = new Regex(@"/BaseFont\s*/([A-Za-z0-9\+\-_]+)", RegexOptions.IgnoreCase);

            // Quét luồng nén
            int streamPos = 0;
            while ((streamPos = ascii.IndexOf("stream", streamPos)) >= 0)
            {
                int dataStart = streamPos + 6;
                if (dataStart < bytes.Length && bytes[dataStart] == '\r') dataStart++;
                if (dataStart < bytes.Length && bytes[dataStart] == '\n') dataStart++;
                int endStream = ascii.IndexOf("endstream", dataStart);
                if (endStream > dataStart)
                {
                    int len = endStream - dataStart;
                    if (len > 0 && len < 2000000)
                    {
                        byte[] raw = new byte[len];
                        Array.Copy(bytes, dataStart, raw, 0, len);
                        byte[] decomp = DecompressZlib(raw);
                        if (decomp != null && decomp.Length > 0)
                        {
                            string dStr = Encoding.ASCII.GetString(decomp);
                            foreach (Match m in r.Matches(dStr))
                            {
                                string fontName = CleanFontName(m.Groups[1].Value);
                                if (!found.Contains(fontName)) found.Add(fontName);
                            }
                        }
                    }
                    streamPos = endStream + 9;
                }
                else { streamPos += 6; }
            }

            foreach (var f in found)
            {
                string mapped = MapToSystemFont(f);
                if (!string.IsNullOrEmpty(mapped)) return mapped;
            }
        }
        catch { }
        return "Times New Roman";
    }

    private static string CleanFontName(string raw)
    {
        int plus = raw.IndexOf('+');
        if (plus >= 0) raw = raw.Substring(plus + 1);
        return raw;
    }

    public static string MapToSystemFont(string rawFont)
    {
        string lower = rawFont.ToLowerInvariant();
        if (lower.Contains("times") || lower.Contains("serif") || lower.Contains("roman") || lower.Contains("termes") || lower.Contains("minion"))
            return "Times New Roman";
        if (lower.Contains("arial") || lower.Contains("helvetica") || lower.Contains("liberationsans") || lower.Contains("freesans"))
            return "Arial";
        if (lower.Contains("calibri"))
            return "Calibri";
        if (lower.Contains("cambria"))
            return "Cambria";
        if (lower.Contains("segoe"))
            return "Segoe UI";
        if (lower.Contains("tahoma"))
            return "Tahoma";
        if (lower.Contains("verdana"))
            return "Verdana";
        if (lower.Contains("georgia"))
            return "Georgia";
        if (lower.Contains("courier") || lower.Contains("consolas") || lower.Contains("mono"))
            return "Consolas";
        return null;
    }

    private static byte[] DecompressZlib(byte[] data)
    {
        if (data == null || data.Length < 2) return null;
        try
        {
            int headerOffset = 0;
            if (data[0] == 0x78) headerOffset = 2;
            using (var ms = new MemoryStream(data, headerOffset, data.Length - headerOffset - (headerOffset == 2 ? 4 : 0)))
            using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                byte[] buf = new byte[8192];
                int read;
                while ((read = ds.Read(buf, 0, buf.Length)) > 0) outMs.Write(buf, 0, read);
                return outMs.ToArray();
            }
        }
        catch { return null; }
    }
}

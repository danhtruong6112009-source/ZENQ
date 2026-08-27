using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class TestDetectFontsInCSharp
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        byte[] bytes = File.ReadAllBytes(pdfPath);

        List<string> detectedFonts = new List<string>();

        // 1. Quét chuỗi ASCII trực tiếp nếu không nén
        string ascii = Encoding.ASCII.GetString(bytes);
        Regex r = new Regex(@"/BaseFont\s*/([A-Za-z0-9\+\-_]+)", RegexOptions.IgnoreCase);
        foreach (Match m in r.Matches(ascii))
        {
            AddFont(m.Groups[1].Value, detectedFonts);
        }

        // 2. Quét qua các luồng nén FlateDecode
        // Dùng DecompressZlib đã có trong PdfHandler hoặc DeflateStream
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
                byte[] raw = new byte[len];
                Array.Copy(bytes, dataStart, raw, 0, len);
                try
                {
                    // Thử giải nén zlib
                    byte[] decomp = DecompressZlib(raw);
                    if (decomp != null && decomp.Length > 0)
                    {
                        string decompStr = Encoding.ASCII.GetString(decomp);
                        foreach (Match m in r.Matches(decompStr))
                        {
                            AddFont(m.Groups[1].Value, detectedFonts);
                        }
                    }
                }
                catch { }
                streamPos = endStream + 9;
            }
            else
            {
                streamPos += 6;
            }
        }

        Console.WriteLine("Detected fonts count: " + detectedFonts.Count);
        foreach (var f in detectedFonts)
        {
            Console.WriteLine("   Font: " + f + " => Mapped System Font: " + MapToSystemFont(f));
        }
    }

    private static void AddFont(string raw, List<string> list)
    {
        int plus = raw.IndexOf('+');
        if (plus >= 0) raw = raw.Substring(plus + 1);
        if (!list.Contains(raw)) list.Add(raw);
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
        return "Times New Roman"; // Mặc định tài liệu văn bản chuẩn
    }

    public static byte[] DecompressZlib(byte[] data)
    {
        if (data == null || data.Length < 2) return null;
        try
        {
            // Bỏ 2 byte header zlib nếu có
            int headerOffset = 0;
            if (data[0] == 0x78) headerOffset = 2;

            using (var ms = new MemoryStream(data, headerOffset, data.Length - headerOffset - (headerOffset == 2 ? 4 : 0)))
            using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                byte[] buf = new byte[8192];
                int read;
                while ((read = ds.Read(buf, 0, buf.Length)) > 0)
                {
                    outMs.Write(buf, 0, read);
                }
                return outMs.ToArray();
            }
        }
        catch { return null; }
    }
}

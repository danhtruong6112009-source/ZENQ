using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GenOffice.Format;

public class InspectPdfFontsDecompressed
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        byte[] bytes = File.ReadAllBytes(pdfPath);
        // Dùng reflection gọi IndexPdfObjects hoặc đọc qua file
        string ascii = Encoding.ASCII.GetString(bytes);
        Console.WriteLine("File size: " + bytes.Length);

        // Tìm kiếm các từ khóa font
        int pos = 0;
        while ((pos = ascii.IndexOf("/Font", pos)) >= 0)
        {
            int snippetLen = Math.Min(200, ascii.Length - pos);
            Console.WriteLine("Snippet at " + pos + ": " + ascii.Substring(pos, snippetLen).Replace("\r", " ").Replace("\n", " "));
            pos += 5;
        }
    }
}

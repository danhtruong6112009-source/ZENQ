using System;
using System.IO;
using System.Text;

public class InspectHeader
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        byte[] bytes = File.ReadAllBytes(pdfPath);
        Console.WriteLine("Length: " + bytes.Length);
        Console.WriteLine("First 200 bytes: " + Encoding.ASCII.GetString(bytes, 0, Math.Min(200, bytes.Length)));
        Console.WriteLine("Last 500 bytes: " + Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 500), Math.Min(500, bytes.Length)));
    }
}

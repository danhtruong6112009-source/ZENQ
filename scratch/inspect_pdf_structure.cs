using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PdfiumViewer;

public class InspectPdfStructure
{
    public static void Main()
    {
        string filePath = @"C:\Users\Danh\Desktop\Chuyên Đề Kỹ Thuật PowerPoint - Lồng Ảnh & Merge Shapes.pdf";
        byte[] bytes = File.ReadAllBytes(filePath);
        string text = Encoding.ASCII.GetString(bytes);

        Console.WriteLine("=== PHÂN TÍCH CẤU TRÚC TỆP PDF ===");
        Console.WriteLine("Dung lượng file: " + bytes.Length + " bytes (~" + (bytes.Length / 1024) + " KB)");

        // 1. Kiểm tra Font
        int fontCount = Regex.Matches(text, @"/Type\s*/Font\b").Count;
        Console.WriteLine("Số lượng đối tượng /Font: " + fontCount);

        // 2. Kiểm tra XObject Image
        int imageCount = Regex.Matches(text, @"/Subtype\s*/Image\b").Count;
        Console.WriteLine("Số lượng đối tượng /Image: " + imageCount);

        // 3. Kiểm tra Creator / Producer
        Match creatorMatch = Regex.Match(text, @"/Creator\s*\((.*?)\)");
        if (creatorMatch.Success) Console.WriteLine("Creator: " + creatorMatch.Groups[1].Value);

        Match producerMatch = Regex.Match(text, @"/Producer\s*\((.*?)\)");
        if (producerMatch.Success) Console.WriteLine("Producer: " + producerMatch.Groups[1].Value);

        // 4. Render thử Trang 0 ra ảnh để xem nội dung trang 0 chứa gì
        using (var pdfium = PdfiumViewer.PdfDocument.Load(filePath))
        {
            var pSize = pdfium.PageSizes[0];
            Console.WriteLine(string.Format("Kích thước trang 0: {0} x {1} pt", pSize.Width, pSize.Height));
            using (var bmp = (System.Drawing.Bitmap)pdfium.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
            {
                bmp.Save(@"scratch\problem_file_page0.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("Đã lưu ảnh render trang 0 vào: scratch\\problem_file_page0.png");
            }
        }
    }
}

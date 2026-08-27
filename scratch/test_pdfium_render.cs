using System;
using System.Drawing;
using PdfiumViewer;

public class TestPdfiumRender
{
    public static void Main()
    {
        // Kiểm tra kích thước Bitmap trả về từ Render với các cờ
        // Giả sử có 1 trang PDF
        Console.WriteLine("DPI scaling analysis:");
        foreach (float zoom in new float[] { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f })
        {
            int targetW = (int)(595 * zoom);
            int targetH = (int)(842 * zoom);
            float dpi = 96f * zoom;
            Console.WriteLine("Zoom=" + zoom + ": targetW=" + targetW + ", targetH=" + targetH + ", dpi=" + dpi);
        }
    }
}

using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestTypingD
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfHandler.LoadPdf(pdfPath);
        var view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;

        PdfTextElement bai1 = null;
        foreach (var elem in doc.Pages[0].TextElements)
        {
            if (elem.Text.Contains("Bài 1")) { bai1 = elem; break; }
        }

        Console.WriteLine("Trước khi gõ:");
        Console.WriteLine("Bounds: " + bai1.Bounds);
        Console.WriteLine("FontSize: " + bai1.FontSize);

        view.StartInlineEdit(0, bai1, -1, 1000, 800);
        view.CursorPosition = 8; // ở chữ 'ệ'
        view.HandleKeyPress('d');

        Console.WriteLine("\nSau khi gõ chữ 'd':");
        Console.WriteLine("Bounds: " + bai1.Bounds);
        Console.WriteLine("Text: " + bai1.Text);
        Console.WriteLine("FontSize: " + bai1.FontSize);
    }
}

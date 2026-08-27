using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestOcrEditingVisual
{
    public static void Main()
    {
        Console.WriteLine("Testing visual appearance after OCR and during edit...");
        string pdfPath = @"C:\Users\Danh\Desktop\Chuyên Đề Kỹ Thuật PowerPoint - Lồng Ảnh & Merge Shapes.pdf";

        PdfDocument doc = PdfHandler.LoadPdf(pdfPath);
        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 0.45f;

        var elements = OcrHandler.RecognizePage(doc, 0);
        doc.Pages[0].TextElements.Clear();
        foreach (var el in elements) doc.Pages[0].TextElements.Add(el);

        // Find the title element "Xử Lý Hình Ảnh..."
        PdfTextElement titleElem = null;
        foreach (var el in doc.Pages[0].TextElements)
        {
            if (el.Bounds.Y > 500 && el.Bounds.Y < 600) { titleElem = el; break; }
        }

        if (titleElem != null)
        {
            // Set friendly text
            titleElem.Text = "Xử Lý Hình Ảnh Nâng Cao & Merge";
            view.SelectedText = titleElem;
            // Start editing it
            view.StartInlineEdit(0, titleElem, -1, 1280, 800);
            view.EditingBuffer = "Xử Lý Hình Ảnh Nâng Cao & Merge [ĐÃ SỬA THÀNH CÔNG!]";
            view.CursorPosition = view.EditingBuffer.Length;
        }

        int formW = 1280;
        int formH = 800;
        using (Bitmap bmp = new Bitmap(formW, formH))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            view.Draw(g, formW, formH, null, 155, 26);
            string outFrame = @"scratch\ocr_after_edit_frame.png";
            bmp.Save(outFrame, ImageFormat.Png);
            Console.WriteLine("Saved edited frame to: " + outFrame);
        }
        Console.WriteLine("SUCCESS!");
    }
}

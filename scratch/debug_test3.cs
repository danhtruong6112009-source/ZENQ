using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class DebugTest3
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        PdfDocument doc = PdfHandler.LoadPdf(pdfPath);
        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;

        PdfTextElement bai1 = null;
        foreach (var elem in doc.Pages[0].TextElements)
        {
            if (elem.Text.Contains("Bài 1")) { bai1 = elem; break; }
        }

        int viewW = 1200, viewH = 900;
        RectangleF pBounds = view.GetPageScreenBounds(0, viewW, viewH, 155);
        int clickX = (int)(pBounds.X + bai1.Bounds.X + 10);
        int clickY = (int)(pBounds.Y + bai1.Bounds.Y + 6);

        view.SelectedTool = 0;
        view.HandleDoubleClick(clickX, clickY, viewW, viewH, 155);
        Console.WriteLine("After DoubleClick: SelectedText = " + (view.SelectedText != null ? view.SelectedText.Text : "null"));
        Console.WriteLine("After DoubleClick: EditingText = " + (view.EditingText != null ? view.EditingText.Text : "null"));

        view.SelectedTool = 2;
        bool res = view.HandleMouseDown(clickX, clickY, viewW, viewH, MouseButtons.Left, null);
        Console.WriteLine("HandleMouseDown returned: " + res);
        Console.WriteLine("After HandleMouseDown: EditingText = " + (view.EditingText != null ? view.EditingText.Text : "null"));
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TraceBranch
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

        view.SelectedText = bai1;
        view.SelectedTool = 2; // Sửa chữ

        // Kiểm tra Handles:
        float sX = pBounds.X + (bai1.Bounds.X * view.ZoomLevel);
        float sY = pBounds.Y + (bai1.Bounds.Y * view.ZoomLevel);
        float sW = Math.Max(20f, bai1.Bounds.Width * view.ZoomLevel);
        float sH = Math.Max(15f, bai1.Bounds.Height * view.ZoomLevel);
        RectangleF selScreenRect = new RectangleF(sX, sY, sW, sH);

        PointF[] handles = new PointF[] {
            new PointF(selScreenRect.Left, selScreenRect.Top),
            new PointF(selScreenRect.Left + selScreenRect.Width / 2f, selScreenRect.Top),
            new PointF(selScreenRect.Right, selScreenRect.Top),
            new PointF(selScreenRect.Right, selScreenRect.Top + selScreenRect.Height / 2f),
            new PointF(selScreenRect.Right, selScreenRect.Bottom),
            new PointF(selScreenRect.Left + selScreenRect.Width / 2f, selScreenRect.Bottom),
            new PointF(selScreenRect.Left, selScreenRect.Bottom),
            new PointF(selScreenRect.Left, selScreenRect.Top + selScreenRect.Height / 2f)
        };

        for (int h = 0; h < handles.Length; h++)
        {
            if (Math.Abs(clickX - handles[h].X) <= 10 && Math.Abs(clickY - handles[h].Y) <= 10)
            {
                Console.WriteLine("HIT HANDLE " + h + " at (" + handles[h].X + ", " + handles[h].Y + ")! clickX=" + clickX + ", clickY=" + clickY);
            }
        }
    }
}

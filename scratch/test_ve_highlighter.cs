using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

class TestVeHighlighter
{
    [STAThread]
    static void Main()
    {
        // 1. Render Ribbon Bar Tab "Vẽ" with Custom Pen
        {
            RibbonBar ribbon = new RibbonBar();
            WriterView wv = new WriterView();
            ribbon.CurrentMode = OfficeAppMode.Writer;
            ribbon.DocTabs.Add(new DocTab("Tài liệu 1.docx", OfficeAppMode.Writer, false));
            ribbon.ActiveDocIndex = 1;
            ribbon.ActiveRibbonTab = 2; // "Vẽ" tab is index 2

            ribbon.CustomPenColor = Color.FromArgb(142, 68, 173); // Purple
            ribbon.ActiveDrawTool = 7; // Custom Pen active

            int w = 1200, h = 160;
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                ribbon.Draw(g, w, h, wv, null, null, null);
                bmp.Save("scratch\\rendered_tab_ve_with_custom_pen.png", ImageFormat.Png);
            }
            Console.WriteLine("[PASS] Saved: scratch\\rendered_tab_ve_with_custom_pen.png");
        }

        // 2. Render WriterView with text and highlighter strokes over text
        {
            WriterView writerView = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            ribbon.CurrentMode = OfficeAppMode.Writer;
            ribbon.ShowRuler = false;

            // Insert text with names like user's table
            writerView.PieceTable.Insert(0, 
                "DANH SÁCH SINH VIÊN XUẤT SẮC\r\n" +
                "1. Nguyễn Minh Phát - Lớp 12A1 - Điểm TB: 9.8\r\n" +
                "2. Nguyễn Thị Như Quỳnh - Lớp 12A2 - Điểm TB: 9.6\r\n" +
                "3. Trần Văn Hưng - Lớp 12A3 - Điểm TB: 9.5\r\n"
            );

            // Add yellow highlighter across line 2
            DocInkStroke yellowHL = new DocInkStroke();
            yellowHL.PageIndex = 0;
            yellowHL.Color = Color.FromArgb(250, 204, 21);
            yellowHL.StrokeWidth = 20.0f;
            yellowHL.IsHighlighter = true;
            for (float x = 100; x <= 450; x += 3)
            {
                yellowHL.Points.Add(new PointF(x, 105));
            }
            writerView.DocumentInkStrokes.Add(yellowHL);

            // Add green highlighter across line 3
            DocInkStroke greenHL = new DocInkStroke();
            greenHL.PageIndex = 0;
            greenHL.Color = Color.FromArgb(34, 197, 94);
            greenHL.StrokeWidth = 20.0f;
            greenHL.IsHighlighter = true;
            for (float x = 100; x <= 450; x += 3)
            {
                greenHL.Points.Add(new PointF(x, 135));
            }
            writerView.DocumentInkStrokes.Add(greenHL);

            // Add custom purple pen stroke circling or underlining
            DocInkStroke purplePen = new DocInkStroke();
            purplePen.PageIndex = 0;
            purplePen.Color = Color.FromArgb(142, 68, 173);
            purplePen.StrokeWidth = 2.5f;
            purplePen.IsHighlighter = false;
            for (float a = 0; a <= 6.28f; a += 0.1f)
            {
                purplePen.Points.Add(new PointF(280 + (float)Math.Cos(a) * 60, 105 + (float)Math.Sin(a) * 15));
            }
            writerView.DocumentInkStrokes.Add(purplePen);

            int docW = 1000, docH = 700;
            using (Bitmap bmp = new Bitmap(docW, docH))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(240, 240, 240));
                writerView.Draw(g, docW, docH, ribbon);
                bmp.Save("scratch\\rendered_highlighter_under_text.png", ImageFormat.Png);
            }
            Console.WriteLine("[PASS] Saved: scratch\\rendered_highlighter_under_text.png");
        }

        // 3. Render WriterView with Table and highlighter strokes over table cells (like user screenshot)
        {
            WriterView writerView = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            ribbon.CurrentMode = OfficeAppMode.Writer;
            ribbon.ShowRuler = false;

            writerView.PieceTable.Insert(0,
                "DANH SÁCH HỌC SINH LỚP 12A1\r\n" +
                "| STT | Họ và tên | Ngày sinh | Giới tính | Nơi sinh |\r\n" +
                "|---|---|---|---|---|\r\n" +
                "| 1 | Nguyễn Minh Phát | 01/01/2005 | Nam | TP.HCM |\r\n" +
                "| 2 | Nguyễn Thị Như Quỳnh | 02/02/2005 | Nữ | Hà Nội |\r\n" +
                "| 3 | Lê Hoàng Long | 15/03/2005 | Nam | Đà Nẵng |\r\n"
            );

            // Add yellow highlighter across "Nguyễn Minh Phát" row
            DocInkStroke yellowHL = new DocInkStroke();
            yellowHL.PageIndex = 0;
            yellowHL.Color = Color.FromArgb(250, 204, 21);
            yellowHL.StrokeWidth = 22.0f;
            yellowHL.IsHighlighter = true;
            for (float x = 110; x <= 650; x += 3)
            {
                yellowHL.Points.Add(new PointF(x, 155));
            }
            writerView.DocumentInkStrokes.Add(yellowHL);

            // Add green highlighter across "Nguyễn Thị Như Quỳnh" row
            DocInkStroke greenHL = new DocInkStroke();
            greenHL.PageIndex = 0;
            greenHL.Color = Color.FromArgb(34, 197, 94);
            greenHL.StrokeWidth = 22.0f;
            greenHL.IsHighlighter = true;
            for (float x = 110; x <= 650; x += 3)
            {
                greenHL.Points.Add(new PointF(x, 185));
            }
            writerView.DocumentInkStrokes.Add(greenHL);

            int docW = 1050, docH = 600;
            using (Bitmap bmp = new Bitmap(docW, docH))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(240, 240, 240));
                writerView.Draw(g, docW, docH, ribbon);
                bmp.Save("scratch\\rendered_table_highlighter.png", ImageFormat.Png);
            }
            Console.WriteLine("[PASS] Saved: scratch\\rendered_table_highlighter.png");
        }
    }
}

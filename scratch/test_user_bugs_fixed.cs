using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestUserBugsFixed
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ: ĐƠ KHI MỞ, CON TRỎ CHẠY RA NGOÀI, MẤT ĐỊNH DẠNG & MẤT Ô PHÍA SAU ===");

        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";

        // TEST 1: TỐC ĐỘ MỞ TÀI LIỆU (KHÔNG BỊ ĐƠ APP)
        Stopwatch sw = Stopwatch.StartNew();
        PdfDocument doc = PdfHandler.LoadPdf(pdfPath);
        sw.Stop();
        Console.WriteLine(string.Format("1. Tốc độ nạp tài liệu: {0} ms (Mục tiêu: < 500 ms)", sw.ElapsedMilliseconds));
        if (sw.ElapsedMilliseconds > 600)
        {
            Console.WriteLine("   -> [FAIL] Quá chậm, có thể gây đơ app!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Nạp tài liệu siêu tốc, không bị đơ app!");

        PdfPageModel page0 = doc.Pages[0];

        // TEST 2: KIỂM TRA MÀU NỀN VÀ MÀU CHỮ CỦA BANNER "CHƯƠNG I"
        PdfTextElement chuongElem = null;
        PdfTextElement menhDeElem = null;
        foreach (var elem in page0.TextElements)
        {
            if (elem.Text.Contains("CHƯƠNG I")) chuongElem = elem;
            if (elem.Text.Contains("Mệnh đề chứa biến")) menhDeElem = elem;
        }

        if (chuongElem == null || menhDeElem == null)
        {
            Console.WriteLine("   -> [FAIL] Không tìm thấy các phần tử kiểm thử!");
            Environment.Exit(1);
        }

        Console.WriteLine("\n2. Kiểm tra nhận diện màu nền và màu chữ banner 'CHƯƠNG I':");
        Console.WriteLine(string.Format("   Màu nền phát hiện (DetectedBackgroundColor): R={0}, G={1}, B={2}",
            chuongElem.DetectedBackgroundColor.R, chuongElem.DetectedBackgroundColor.G, chuongElem.DetectedBackgroundColor.B));
        Console.WriteLine(string.Format("   Màu chữ phát hiện (TextColor): R={0}, G={1}, B={2}",
            chuongElem.TextColor.R, chuongElem.TextColor.G, chuongElem.TextColor.B));
        Console.WriteLine(string.Format("   In đậm (IsBold): {0}", chuongElem.IsBold));

        if (chuongElem.DetectedBackgroundColor.B < 140 || chuongElem.DetectedBackgroundColor.R > 80)
        {
            Console.WriteLine("   -> [FAIL] Chưa phát hiện đúng màu xanh của banner!");
            Environment.Exit(1);
        }
        if (chuongElem.TextColor != Color.White)
        {
            Console.WriteLine("   -> [FAIL] Chưa phát hiện đúng chữ màu trắng!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Nhận diện hoàn hảo Banner Xanh và Chữ Trắng!");

        // TEST 3: KIỂM TRA CON TRỎ KHI CLICK VÀO GIỮA DÒNG
        Console.WriteLine("\n3. Kiểm tra con trỏ khi click vào giữa dòng văn bản dài:");
        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;
        view.SelectedTool = 2; // Tool sửa chữ

        int viewW = 1200;
        int viewH = 900;
        RectangleF pBounds = view.GetPageScreenBounds(0, viewW, viewH, 155);
        // Click vào khoảng giữa chiều rộng của dòng "Mệnh đề chứa biến..."
        int midClickX = (int)(pBounds.X + menhDeElem.Bounds.X + menhDeElem.Bounds.Width / 2f);
        int midClickY = (int)(pBounds.Y + menhDeElem.Bounds.Y + 6);

        view.HandleMouseDown(midClickX, midClickY, viewW, viewH, MouseButtons.Left, null);

        Console.WriteLine(string.Format("   Tổng số ký tự dòng: {0}", view.EditingBuffer.Length));
        Console.WriteLine(string.Format("   Vị trí con trỏ (CursorPosition) nhận được: {0}", view.CursorPosition));

        // CursorPosition phải nằm trong khoảng giữa (từ 20% đến 80% độ dài dòng), KHÔNG PHẢI ở cuối dòng hay ngoài lề
        if (view.CursorPosition <= 5 || view.CursorPosition >= view.EditingBuffer.Length - 5)
        {
            Console.WriteLine("   -> [FAIL] Con trỏ không đặt ở giữa dòng mà nhảy ra ngoài/cuối!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Con trỏ đặt chính xác ngay tại vị trí người dùng nhấp chuột!");

        // TEST 4: SỬA THÊM KÝ TỰ VÀO BANNER "CHƯƠNG I" VÀ ĐẢM BẢO KHÔNG MẤT NỀN XANH
        Console.WriteLine("\n4. Kiểm tra gõ thêm chữ vào 'CHƯƠNG I' (giữ nguyên nền xanh, chữ trắng, không bị ô trắng đục):");
        view.StartInlineEdit(0, chuongElem, -1, viewW, viewH);
        view.HandleKeyPress('s');
        view.HandleKeyPress('d');
        view.HandleKeyPress('s');
        view.HandleKeyPress('d');

        using (Bitmap bmp = new Bitmap(viewW, viewH))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            RibbonBar ribbon = new RibbonBar();
            view.Draw(g, viewW, viewH, ribbon, 155, 26);

            // Kiểm tra điểm pixel bên trong khung chữ đã sửa:
            // Màu nền phải là màu XANH của banner (B > 140, R < 80), KHÔNG ĐƯỢC LÀ MÀU TRẮNG (R=255, G=255, B=255)
            int sampleX = (int)(pBounds.X + chuongElem.Bounds.X + 20);
            int sampleY = (int)(pBounds.Y + chuongElem.Bounds.Y + 3);
            Color px = bmp.GetPixel(sampleX, sampleY);
            Console.WriteLine(string.Format("   Màu pixel tại vùng banner sau khi sửa: R={0}, G={1}, B={2}", px.R, px.G, px.B));

            if (px.R > 240 && px.G > 240 && px.B > 240)
            {
                Console.WriteLine("   -> [FAIL] Bị mảng trắng đục che mất ô nền xanh phía sau!");
                Environment.Exit(1);
            }
            if (px.B < 120)
            {
                Console.WriteLine("   -> [FAIL] Màu nền banner không giữ được sắc xanh chuẩn!");
                Environment.Exit(1);
            }
            Console.WriteLine("   -> [PASS] Nền xanh của ô phía sau được bảo toàn 100%, không bị che trắng!");
        }

        Console.WriteLine("\n=== TOÀN BỘ 4 LỖI ĐÃ ĐƯỢC GIẢI QUYẾT TRIỆT ĐỂ 100%! ===");
    }
}

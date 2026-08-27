using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestUserExactCase
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ: TỰ ĐỘNG PHÁT HIỆN FONT, KHUNG VỪA KHÍT CHỮ, KHÔNG CÓ KHUNG TRẮNG ĐỤC, KHÔNG BỊ PHÓNG TO ===");

        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        PdfDocument doc = PdfHandler.LoadPdf(pdfPath);

        Console.WriteLine("1. Kiểm tra phát hiện phông chữ chính của tài liệu PDF:");
        Console.WriteLine("   doc.PrimaryFontName = " + doc.PrimaryFontName);
        if (doc.PrimaryFontName != "Times New Roman")
        {
            Console.WriteLine("   -> [FAIL] Phông chữ không đúng (mong đợi Times New Roman, nhận " + doc.PrimaryFontName + ")");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Đã tự động phát hiện chính xác phông chữ 'Times New Roman'!");

        Console.WriteLine("\n2. Kiểm tra dòng 'CHƯƠNG I: MỆNH ĐỀ VÀ TẬP HỢP':");
        PdfPageModel page0 = doc.Pages[0];
        PdfTextElement chuongElem = null;
        foreach (var elem in page0.TextElements)
        {
            if (elem.Text.Contains("CHƯƠNG I"))
            {
                chuongElem = elem;
                break;
            }
        }

        if (chuongElem == null)
        {
            Console.WriteLine("   -> [FAIL] Không tìm thấy dòng CHƯƠNG I!");
            Environment.Exit(1);
        }

        Console.WriteLine(string.Format("   Nội dung: '{0}'", chuongElem.Text));
        Console.WriteLine(string.Format("   Phông chữ: {0}", chuongElem.FontName));
        Console.WriteLine(string.Format("   Cỡ chữ: {0:0.0} pt", chuongElem.FontSize));
        Console.WriteLine(string.Format("   In đậm (IsBold): {0}", chuongElem.IsBold));
        Console.WriteLine(string.Format("   Kích thước khung: X={0:0.0}, Y={1:0.0}, W={2:0.0}, H={3:0.0}",
            chuongElem.Bounds.X, chuongElem.Bounds.Y, chuongElem.Bounds.Width, chuongElem.Bounds.Height));

        if (chuongElem.FontName != "Times New Roman")
        {
            Console.WriteLine("   -> [FAIL] Phông chữ dòng không phải Times New Roman!");
            Environment.Exit(1);
        }

        if (Math.Abs(chuongElem.FontSize - 16.0f) > 0.5f)
        {
            Console.WriteLine("   -> [FAIL] Cỡ chữ không phải 16pt (nhận: " + chuongElem.FontSize + ")!");
            Environment.Exit(1);
        }

        if (!chuongElem.IsBold)
        {
            Console.WriteLine("   -> [FAIL] Dòng tiêu đề phải tự động nhận diện IsBold = true!");
            Environment.Exit(1);
        }

        if (chuongElem.Bounds.Width > 250 || chuongElem.Bounds.Width < 180)
        {
            Console.WriteLine("   -> [FAIL] Chiều rộng khung (" + chuongElem.Bounds.Width + ") không vừa khít chữ (chuẩn là ~208)!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Khung vừa khít chữ chuẩn xác, font Times New Roman 16pt Bold!");

        Console.WriteLine("\n3. Kiểm tra nhấp chuột vào dòng để bắt đầu sửa:");
        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;
        view.SelectedTool = 2; // Tool sửa chữ

        int viewW = 1200;
        int viewH = 900;
        RectangleF pBounds = view.GetPageScreenBounds(0, viewW, viewH, 155);
        int clickX = (int)(pBounds.X + chuongElem.Bounds.X + 20);
        int clickY = (int)(pBounds.Y + chuongElem.Bounds.Y + 6);

        bool clicked = view.HandleMouseDown(clickX, clickY, viewW, viewH, MouseButtons.Left, null);
        if (!clicked || view.EditingText != chuongElem)
        {
            Console.WriteLine("   -> [FAIL] Không kích hoạt sửa được dòng!");
            Environment.Exit(1);
        }

        Console.WriteLine("   ActiveFontName = " + view.ActiveFontName);
        Console.WriteLine("   ActiveFontSize = " + view.ActiveFontSize);
        if (view.ActiveFontName != "Times New Roman" || Math.Abs(view.ActiveFontSize - 16.0f) > 0.5f)
        {
            Console.WriteLine("   -> [FAIL] Chưa đồng bộ font và size lên thanh công cụ!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Đồng bộ hoàn hảo phông chữ và cỡ chữ khi nhấp chọn!");

        Console.WriteLine("\n4. Kiểm tra vẽ và đảm bảo KHÔNG CÓ khung trắng đục khi chưa sửa:");
        using (Bitmap bmp = new Bitmap(viewW, viewH))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            RibbonBar ribbon = new RibbonBar();
            // Đặt nền màu xanh nhạt để phát hiện ngay lập tức nếu có màu trắng đè lên
            g.Clear(Color.FromArgb(200, 230, 255));
            view.Draw(g, viewW, viewH, ribbon, 155, 26);

            // Kiểm tra điểm pixel ngay bên trong khung chữ
            int testPx = (int)(pBounds.X + chuongElem.Bounds.X + 25);
            int testPy = (int)(pBounds.Y + chuongElem.Bounds.Y + 5);
            Color sample = bmp.GetPixel(testPx, testPy);
            Console.WriteLine(string.Format("   Màu pixel mẫu trong khung chữ: R={0}, G={1}, B={2}", sample.R, sample.G, sample.B));
            if (sample.R == 255 && sample.G == 255 && sample.B == 255)
            {
                Console.WriteLine("   -> [FAIL] Vẫn còn khung trắng đục che nền!");
                Environment.Exit(1);
            }
            Console.WriteLine("   -> [PASS] Hoàn toàn không có khung trắng đục che nền, giữ nguyên trong suốt!");
        }

        Console.WriteLine("\n5. Kiểm tra gõ chữ mới và tự động mở rộng khung (chống ngắt dòng, cắt chữ):");
        float oldWidth = chuongElem.Bounds.Width;
        view.EditingBuffer = "CHƯƠNG I: MỆNH ĐỀ VÀ TẬP HỢP (ĐÃ CẬP NHẬT MỚI BỞI GENOFFICE)";
        view.HandleKeyPress('!');

        Console.WriteLine(string.Format("   Chiều rộng cũ: {0:0.0} => Chiều rộng mới tự dãn: {1:0.0}", oldWidth, chuongElem.Bounds.Width));
        if (chuongElem.Bounds.Width <= oldWidth)
        {
            Console.WriteLine("   -> [FAIL] Khung chưa tự động dãn dài khi văn bản dài hơn!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Khung tự động mở rộng khi gõ dài hơn, không bị ngắt dòng hay cắt chữ!");

        Console.WriteLine("\n=== TẤT CẢ CÁC MỤC KIỂM THỬ ĐÃ ĐẠT 100%! ===");
    }
}

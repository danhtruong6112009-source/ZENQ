using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestE2EPdfDetectAndEdit
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ TOÀN DIỆN (E2E): TỰ ĐỘNG PHÁT HIỆN CHỮ, CHỈNH SỬA VĂN BẢN VÀ KHAI THÁC TOÀN BỘ FONT HỆ THỐNG ===");

        // 1. Kiểm tra tải tệp PDF thật qua PdfHandler.LoadPdf
        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        PdfDocument doc = PdfHandler.LoadPdf(pdfPath);

        Console.WriteLine("1. Nạp tệp PDF: " + pdfPath);
        Console.WriteLine("   Số trang: " + doc.Pages.Count);
        if (doc.Pages.Count == 0)
        {
            Console.WriteLine("   -> [FAIL] Không có trang nào trong tài liệu!");
            Environment.Exit(1);
        }

        var page0 = doc.Pages[0];
        Console.WriteLine("   Số dòng văn bản được tự động phát hiện trên Trang 1: " + page0.TextElements.Count);
        if (page0.TextElements.Count == 0)
        {
            Console.WriteLine("   -> [FAIL] Chưa phát hiện được dòng văn bản nào tự động!");
            Environment.Exit(1);
        }

        PdfTextElement firstElem = page0.TextElements[0];
        Console.WriteLine(string.Format("   Dòng 1: '{0}', Tọa độ=({1:0.0}, {2:0.0}, {3:0.0}, {4:0.0}), Font={5}, Cỡ={6:0.0}pt, IsDetected={7}",
            firstElem.Text, firstElem.Bounds.X, firstElem.Bounds.Y, firstElem.Bounds.Width, firstElem.Bounds.Height,
            firstElem.FontName, firstElem.FontSize, firstElem.IsDetectedFromPdf));
        Console.WriteLine("   -> [PASS] Tự động phát hiện văn bản khi nạp PDF hoạt động hoàn hảo!");

        // 2. Khởi tạo PdfView và kiểm tra nhấp chuột vào dòng chữ bằng công cụ Sửa chữ (Tool 2)
        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;

        int viewW = 1200;
        int viewH = 800;
        int topOffset = 155;

        RectangleF pBounds = view.GetPageScreenBounds(0, viewW, viewH, topOffset);
        float clickX = pBounds.X + firstElem.Bounds.X + 15;
        float clickY = pBounds.Y + firstElem.Bounds.Y + 10;

        Console.WriteLine("\n2. Kiểm thử công cụ 'Sửa chữ' (Tool 2)...");
        view.SelectedTool = 2; // Bật công cụ Sửa chữ
        bool clicked = view.HandleMouseDown((int)clickX, (int)clickY, viewW, viewH, MouseButtons.Left, null);

        if (!clicked || view.EditingText == null)
        {
            Console.WriteLine("   -> [FAIL] Nhấp công cụ Sửa chữ không mở được trình soạn thảo trực tiếp!");
            Environment.Exit(1);
        }

        Console.WriteLine("   Đang soạn thảo dòng: '" + view.EditingText.Text + "'");
        if (view.EditingText != firstElem)
        {
            Console.WriteLine("   -> [FAIL] Dòng đang sửa không khớp với dòng được nhấp!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Bấm công cụ Sửa chữ kích hoạt ngay dòng văn bản được nhấp!");

        // 3. Soạn thảo sửa nội dung dòng chữ (Gõ chữ mới và Lưu)
        Console.WriteLine("\n3. Kiểm thử sửa nội dung văn bản trực tiếp...");
        view.EditingBuffer = "Xin Chào Thế Giới";
        view.CommitInlineEdit();

        Console.WriteLine("   Nội dung mới của dòng: '" + firstElem.Text + "'");
        if (firstElem.Text != "Xin Chào Thế Giới")
        {
            Console.WriteLine("   -> [FAIL] Nội dung không được cập nhật!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Sửa đổi nội dung văn bản thành công!");

        // 4. Kiểm thử nhấp đúp (Double-Click) để sửa dòng văn bản
        Console.WriteLine("\n4. Kiểm thử nhấp đúp chuột (Double Click) vào dòng văn bản...");
        view.CommitInlineEdit();
        view.SelectedText = null;
        view.EditingText = null;
        view.SelectedTool = 0; // Quay về con trỏ thông thường

        bool dblClicked = view.HandleDoubleClick((int)clickX, (int)clickY, viewW, viewH, topOffset);
        if (!dblClicked || view.EditingText == null)
        {
            Console.WriteLine("   -> [FAIL] Nhấp đúp không kích hoạt sửa dòng văn bản!");
            Environment.Exit(1);
        }
        Console.WriteLine("   Dòng đang sửa khi nhấp đúp: '" + view.EditingText.Text + "'");
        Console.WriteLine("   -> [PASS] Nhấp đúp kích hoạt sửa ngay lập tức!");

        // 5. Kiểm thử khai thác toàn bộ Font chữ hệ thống (AvailableSystemFonts)
        Console.WriteLine("\n5. Kiểm thử khai thác toàn bộ phông chữ hệ thống như Writer...");
        RibbonBar ribbon = new RibbonBar();
        Console.WriteLine("   Tổng số phông chữ hệ thống nạp được trong RibbonBar: " + ribbon.AvailableSystemFonts.Count);

        if (ribbon.AvailableSystemFonts.Count < 20)
        {
            Console.WriteLine("   -> [FAIL] Số phông chữ hệ thống quá ít (" + ribbon.AvailableSystemFonts.Count + ")!");
            Environment.Exit(1);
        }

        Console.WriteLine("   Ví dụ một số phông tìm thấy: " + string.Join(", ", ribbon.AvailableSystemFonts.GetRange(0, Math.Min(10, ribbon.AvailableSystemFonts.Count)).ToArray()));

        // Thử áp dụng font bất kỳ từ danh sách toàn bộ font máy tính
        string testFont = ribbon.AvailableSystemFonts.Contains("Arial") ? "Arial" : ribbon.AvailableSystemFonts[0];
        firstElem.FontName = testFont;
        firstElem.FontSize = 24.0f;
        Console.WriteLine(string.Format("   Đã áp dụng Font '{0}', Cỡ {1}pt cho dòng văn bản!", firstElem.FontName, firstElem.FontSize));
        Console.WriteLine("   -> [PASS] Khai thác toàn bộ phông chữ hệ thống thành công!");

        // 6. Kiểm thử vẽ rendering
        Console.WriteLine("\n6. Kiểm thử Rendering phủ nền và vẽ chữ mới...");
        using (Bitmap bmp = new Bitmap(800, 600))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            view.Draw(g, 800, 600, ribbon);
            Console.WriteLine("   -> [PASS] Vẽ trang PDF và các TextElements sửa đổi không bị lỗi!");
        }

        Console.WriteLine("\n=== TẤT CẢ 6 MỤC KIỂM THỬ ĐÃ VƯỢT QUA 100%! ===");
    }
}

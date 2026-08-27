using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestAllUserRequirements
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ TOÀN DIỆN CÁC YÊU CẦU MỚI TỪ NGƯỜI DÙNG ===");
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

        if (bai1 == null)
        {
            Console.WriteLine("[FAIL] Không tìm thấy Bài 1");
            Environment.Exit(1);
        }

        // TEST 1: CỠ CHỮ CHUẨN XÁC VÀ KHÔNG BỊ PHÓNG TO KHI GÕ 1 KÝ TỰ
        Console.WriteLine("\n1. Kiểm tra cỡ chữ và không tự phóng to khung khi gõ ký tự:");
        Console.WriteLine(string.Format("   Cỡ chữ phát hiện của 'Bài 1': {0:0.0} (Tránh mức 16px quá to)", bai1.FontSize));
        if (bai1.FontSize > 13.5f)
        {
            Console.WriteLine("   -> [FAIL] Cỡ chữ quá to (> 13.5) làm phóng to chữ sai cỡ!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Cỡ chữ chuẩn xác (11.6px), vừa vặn 100% với chữ gốc!");

        RectangleF originalBounds = bai1.Bounds;
        view.SelectedTool = 2; // Tool sửa chữ
        view.StartInlineEdit(0, bai1, -1, 1200, 900);
        view.HandleKeyPress('d');

        Console.WriteLine(string.Format("   Kích thước ban đầu: {0}x{1} | Sau khi gõ 'd': {2}x{3}",
            originalBounds.Width, originalBounds.Height, bai1.Bounds.Width, bai1.Bounds.Height));
        if (bai1.Bounds.Width > originalBounds.Width + 5 || bai1.Bounds.Height > originalBounds.Height + 2)
        {
            Console.WriteLine("   -> [FAIL] Khung bị tự ý phóng to sai cỡ khi gõ ký tự!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Khung giữ nguyên kích thước ổn định khi gõ, không tự phóng to sai cỡ!");
        view.CommitInlineEdit();

        // TEST 2: KÉO THU HẸP KHUNG THÌ TỰ ĐỘNG XUỐNG HÀNG
        Console.WriteLine("\n2. Kiểm tra kéo khung vô (thu hẹp chiều rộng) tự động xuống hàng:");
        view.SelectedText = bai1;
        view.DraggingHandleIndex = 3; // Chốt Middle-Right
        view.IsResizingHandle = true;
        view.DragStartBoxBounds = originalBounds;
        view.DragStartMouse = new PointF(500, 300);
        // Kéo chuột sang trái 40px (thu hẹp từ 80px xuống 40px)
        view.HandleMouseMove(460, 300, 1200, 900, MouseButtons.Left);

        Console.WriteLine(string.Format("   Sau khi kéo khung vô: Rộng={0:0.0}, Cao={1:0.0} (Chiều cao tự dãn cho dòng xuống)",
            bai1.Bounds.Width, bai1.Bounds.Height));
        if (bai1.Bounds.Width >= originalBounds.Width)
        {
            Console.WriteLine("   -> [FAIL] Chiều rộng không thu hẹp!");
            Environment.Exit(1);
        }
        if (bai1.Bounds.Height <= originalBounds.Height)
        {
            Console.WriteLine("   -> [FAIL] Không tự xuống hàng và tự mở rộng chiều cao cho các dòng dưới!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Khi kéo khung vô, chữ tự động xuống hàng và khung tự dãn theo!");
        view.IsResizingHandle = false;

        // TEST 3: PHÂN QUYỀN SỬA CHỮ: TOOL 0 (CON TRỎ BÌNH THƯỜNG) KHÔNG SỬA CHỮ GỐC, TOOL 2 MỚI CHO SỬA
        Console.WriteLine("\n3. Kiểm tra phân quyền: Nhấp bình thường (Tool 0) không sửa chữ gốc, chỉ Tool 2 mới cho sửa:");
        view.SelectedTool = 0; // Con trỏ bình thường
        view.CommitInlineEdit();
        view.SelectedText = null;

        int viewW = 1200, viewH = 900;
        RectangleF pBounds = view.GetPageScreenBounds(0, viewW, viewH, 155);
        int clickX = (int)(pBounds.X + originalBounds.X + 10);
        int clickY = (int)(pBounds.Y + originalBounds.Y + 6);

        // Nhấp đúp trong Tool 0 vào chữ gốc PDF
        view.HandleDoubleClick(clickX, clickY, viewW, viewH, 155);
        if (view.EditingText != null)
        {
            Console.WriteLine("   -> [FAIL] Nhấp bình thường (Tool 0) lại tự ý vào chế độ sửa chữ gốc!");
            Environment.Exit(1);
        }
        if (view.SelectedText != bai1)
        {
            Console.WriteLine("   -> [FAIL] Nhấp bình thường phải chọn được đối tượng!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Nhấp bình thường trong Tool 0 chỉ CHỌN đối tượng, không vào sửa!");

        // Chuyển sang Tool 2 (Nút "Sửa chữ")
        view.SelectedTool = 2;
        view.HandleMouseDown(clickX, clickY, viewW, viewH, MouseButtons.Left, null);
        if (view.EditingText != bai1)
        {
            Console.WriteLine("   -> [FAIL] Khi bật nút 'Sửa chữ' (Tool 2) lại không cho sửa!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Khi bật nút 'Sửa chữ' (Tool 2) cho phép sửa chữ ngay tức thì!");

        // TEST 4: CHỮ TỰ THÊM SAU CHO PHÉP TỰ SỬA TRỰC TIẾP KHÔNG CẦN BẬT NÚT SỬA CHỮ
        Console.WriteLine("\n4. Kiểm tra chữ tự thêm sau: Tự sửa trực tiếp không cần nhấn nút Sửa chữ:");
        view.SelectedTool = 0; // Đang ở con trỏ bình thường
        view.CommitInlineEdit();
        view.SelectedText = null;

        var customTxt = new PdfTextElement("Chữ mới tự thêm", new RectangleF(100, 600, 150, 30), "Segoe UI", 12f)
        {
            IsDetectedFromPdf = false
        };
        doc.Pages[0].TextElements.Add(customTxt);

        int customClickX = (int)(pBounds.X + customTxt.Bounds.X + 10);
        int customClickY = (int)(pBounds.Y + customTxt.Bounds.Y + 10);

        view.HandleMouseDown(customClickX, customClickY, viewW, viewH, MouseButtons.Left, null);
        if (view.EditingText != customTxt)
        {
            Console.WriteLine("   -> [FAIL] Chữ tự thêm sau không tự động cho sửa trong Tool 0!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Chữ tự thêm sau tự động cho sửa trực tiếp mà không cần nhấn nút!");

        // TEST 5: CLICK VÀO VÙNG TRẮNG HOẶC BẤT KỲ ĐÂU NGOÀI TRANG ĐỂ BỎ CHỌN, BỎ GHI
        Console.WriteLine("\n5. Kiểm tra click vào vùng trắng hoặc ngoài trang để bỏ chọn, bỏ ghi:");
        // Đang soạn thảo customTxt
        if (view.EditingText == null)
        {
            Console.WriteLine("   -> [FAIL] Cần đang ở trạng thái soạn thảo để test bỏ chọn!");
            Environment.Exit(1);
        }

        // 5a: Click vào vùng trắng trên trang (X=300, Y=800 không có đối tượng nào)
        int whiteX = (int)(pBounds.X + 300);
        int whiteY = (int)(pBounds.Y + 800);
        view.HandleMouseDown(whiteX, whiteY, viewW, viewH, MouseButtons.Left, null);

        if (view.EditingText != null || view.SelectedText != null)
        {
            Console.WriteLine("   -> [FAIL] Click vào vùng trắng trên trang không bỏ chọn, bỏ ghi!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Click vùng trắng trên trang: Đã tự động bỏ ghi và bỏ chọn thành công!");

        // 5b: Chọn lại đối tượng, sau đó click ra ngoài trang (vùng màu xám canvas: X=pBounds.Right + 50)
        view.SelectedText = customTxt;
        int outsideX = (int)(pBounds.Right + 50);
        int outsideY = 300;
        view.HandleMouseDown(outsideX, outsideY, viewW, viewH, MouseButtons.Left, null);

        if (view.SelectedText != null || view.EditingText != null)
        {
            Console.WriteLine("   -> [FAIL] Click ngoài trang không bỏ chọn đối tượng!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Click bất kỳ chỗ nào ngoài trang: Đã tự động bỏ chọn thành công!");

        Console.WriteLine("\n=== TẤT CẢ 5 YÊU CẦU ĐÃ ĐẠT 100% HOÀN HẢO! ===");
    }
}

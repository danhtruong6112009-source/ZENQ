using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

public class TestTransparentAndImmediateResize
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ: MẶC ĐỊNH TRONG SUỐT VÀ CÓ CHỐT KÉO NGAY LẬP TỨC ===");

        PdfDocument doc = new PdfDocument();
        PdfPageModel page = new PdfPageModel(0, 595, 842);
        doc.Pages.Add(page);

        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;
        view.SelectedTool = 1; // Chọn công cụ Thêm chữ

        int viewW = 1200;
        int viewH = 800;
        int topOffset = 155;

        // Giả lập nhấp chuột tại tọa độ (500, 300) trên màn hình (nằm trên trang PDF) để thêm chữ
        bool added = view.HandleMouseDown(500, 300, viewW, viewH, MouseButtons.Left, null);
        Console.WriteLine("1. Thêm chữ mới: Handled=" + added);

        var targetPage = doc.Pages[view.ActivePageIndex];
        if (targetPage.TextElements.Count == 0)
        {
            Console.WriteLine("[FAIL] Không có text element nào được tạo trên trang " + view.ActivePageIndex);
            Environment.Exit(1);
        }

        PdfTextElement newElem = targetPage.TextElements[0];
        Console.WriteLine("   Màu nền: " + newElem.BackgroundColor.ToString() + ", Màu viền: " + newElem.BorderColor.ToString() + ", Độ dày viền: " + newElem.BorderWidth);

        // Kiểm tra 1: Mặc định trong suốt 100%
        if (newElem.BackgroundColor == Color.Transparent && newElem.BorderColor == Color.Transparent && newElem.BorderWidth == 0f)
        {
            Console.WriteLine("   -> [PASS] Hộp chữ mới hoàn toàn TRONG SUỐT (Nền Transparent, Viền Transparent, Độ dày 0)!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Hộp chữ không trong suốt!");
            Environment.Exit(1);
        }

        // Kiểm tra 2: Đang ở chế độ soạn thảo (EditingText) và đã chọn (SelectedText)
        if (view.EditingText == newElem && view.SelectedText == newElem)
        {
            Console.WriteLine("   -> [PASS] Hộp chữ mới tự động kích hoạt soạn thảo và chọn luôn!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Không kích hoạt soạn thảo tự động!");
            Environment.Exit(1);
        }

        // Kiểm tra 3: Có ô chốt kéo to nhỏ NGAY LẬP TỨC mà không cần nhấn ra rồi nhấn lại
        RectangleF pageBounds = view.GetPageScreenBounds(0, viewW, viewH, topOffset);
        float screenRight = pageBounds.X + newElem.Bounds.Right;
        float screenMidY = pageBounds.Y + newElem.Bounds.Top + newElem.Bounds.Height / 2f;
        float screenInsideX = pageBounds.X + newElem.Bounds.X + 20;
        float screenInsideY = pageBounds.Y + newElem.Bounds.Y + 10;

        // Con trỏ chuột bên trong khi đang soạn thảo -> IBeam
        Cursor insideCur = view.GetCursorAtPoint((int)screenInsideX, (int)screenInsideY, viewW, viewH, topOffset);
        Console.WriteLine("2. Con trỏ bên trong hộp chữ đang soạn thảo: " + insideCur);
        if (insideCur == Cursors.IBeam)
        {
            Console.WriteLine("   -> [PASS] Con trỏ là IBeam khi rê chuột vào trong chữ!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Con trỏ không phải IBeam!");
            Environment.Exit(1);
        }

        // Con trỏ chuột trên chốt vuông kéo -> SizeWE
        Cursor handleCur = view.GetCursorAtPoint((int)screenRight, (int)screenMidY, viewW, viewH, topOffset);
        Console.WriteLine("3. Con trỏ trên chốt phải Right: " + handleCur);
        if (handleCur == Cursors.SizeWE)
        {
            Console.WriteLine("   -> [PASS] Con trỏ đổi thành SizeWE trên chốt phải ngay lập tức!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Con trỏ trên chốt không phải SizeWE!");
            Environment.Exit(1);
        }

        // Kiểm tra 4: Nhấp giữ và kéo chốt phải NGAY LẬP TỨC mà không cần nhấp ra ngoài
        Console.WriteLine("4. Chiều rộng ban đầu: " + newElem.Bounds.Width);
        bool handleHit = view.HandleMouseDown((int)screenRight, (int)screenMidY, viewW, viewH, MouseButtons.Left, null);
        if (handleHit && view.IsResizingHandle && view.DraggingHandleIndex == 3)
        {
            view.HandleMouseMove((int)screenRight + 80, (int)screenMidY, viewW, viewH, MouseButtons.Left);
            Console.WriteLine("   Chiều rộng sau khi kéo thêm 80px: " + newElem.Bounds.Width);
            if (Math.Abs(newElem.Bounds.Width - 280f) < 1f)
            {
                Console.WriteLine("   -> [PASS] Nắm kéo to nhỏ thành công trực tiếp ngay khi vừa thêm chữ!");
            }
            else
            {
                Console.WriteLine("   -> [FAIL] Kéo kích thước không đúng: " + newElem.Bounds.Width);
                Environment.Exit(1);
            }
            view.HandleMouseUp((int)screenRight + 80, (int)screenMidY, viewW, viewH, MouseButtons.Left);
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Không bắt được sự kiện kéo chốt khi đang soạn thảo!");
            Environment.Exit(1);
        }

        Console.WriteLine("\n=== TẤT CẢ CÁC BÀI KIỂM THỬ ĐẠT 100%! ===");
    }
}

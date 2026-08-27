using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

public class TestAllCornersResize
{
    public static void Main()
    {
        Console.WriteLine("=== KIỂM THỬ KÉO TẤT CẢ CÁC GÓC Ô VUÔNG (CORNER HANDLES) CỦA HỘP CHỮ ===");

        PdfDocument doc = new PdfDocument();
        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;

        int viewW = 1200;
        int viewH = 800;
        int topOffset = 155;

        var page = doc.Pages[0];
        page.TextElements.Clear();

        // Tạo 1 hộp chữ mẫu tại vị trí (100, 100) kích thước (200, 40)
        PdfTextElement txt = new PdfTextElement("Nội dung thử nghiệm", new RectangleF(100, 100, 200, 40));
        page.TextElements.Add(txt);
        view.SelectedText = txt;

        RectangleF pBounds = view.GetPageScreenBounds(0, viewW, viewH, topOffset);

        // Hàm hỗ trợ tính toán vị trí trên màn hình của 8 chốt
        Func<PointF[]> getHandles = () => {
            float sX = pBounds.X + (txt.Bounds.X * view.ZoomLevel);
            float sY = pBounds.Y + (txt.Bounds.Y * view.ZoomLevel);
            float sW = txt.Bounds.Width * view.ZoomLevel;
            float sH = txt.Bounds.Height * view.ZoomLevel;
            RectangleF r = new RectangleF(sX, sY, sW, sH);
            return new PointF[] {
                new PointF(r.Left, r.Top),                     // 0: Top-Left
                new PointF(r.Left + r.Width / 2f, r.Top),      // 1: Top-Middle
                new PointF(r.Right, r.Top),                    // 2: Top-Right
                new PointF(r.Right, r.Top + r.Height / 2f),    // 3: Middle-Right
                new PointF(r.Right, r.Bottom),                 // 4: Bottom-Right
                new PointF(r.Left + r.Width / 2f, r.Bottom),   // 5: Bottom-Middle
                new PointF(r.Left, r.Bottom),                  // 6: Bottom-Left
                new PointF(r.Left, r.Top + r.Height / 2f)      // 7: Middle-Left
            };
        };

        // --- TEST 1: Kéo góc dưới cùng bên phải (Bottom-Right, chốt 4) ---
        PointF[] hdls = getHandles();
        Console.WriteLine("1. Thử kéo góc Bottom-Right (chốt 4)...");
        bool hit4 = view.HandleMouseDown((int)hdls[4].X, (int)hdls[4].Y, viewW, viewH, MouseButtons.Left, null);
        if (!hit4 || !view.IsResizingHandle || view.DraggingHandleIndex != 4)
        {
            Console.WriteLine("   -> [FAIL] Không bắt được chốt 4 Bottom-Right!");
            Environment.Exit(1);
        }
        view.HandleMouseMove((int)hdls[4].X + 50, (int)hdls[4].Y + 30, viewW, viewH, MouseButtons.Left);
        view.HandleMouseUp((int)hdls[4].X + 50, (int)hdls[4].Y + 30, viewW, viewH, MouseButtons.Left);
        Console.WriteLine(string.Format("   Kích thước mới: {0}x{1} (Kỳ vọng: 250x70)", txt.Bounds.Width, txt.Bounds.Height));
        if (Math.Abs(txt.Bounds.Width - 250f) > 1f || Math.Abs(txt.Bounds.Height - 70f) > 1f)
        {
            Console.WriteLine("   -> [FAIL] Kéo góc 4 không đúng kích thước!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Kéo góc Bottom-Right thành công!");

        // --- TEST 2: Kéo góc trên bên trái (Top-Left, chốt 0) ---
        hdls = getHandles();
        Console.WriteLine("2. Thử kéo góc Top-Left (chốt 0)...");
        bool hit0 = view.HandleMouseDown((int)hdls[0].X, (int)hdls[0].Y, viewW, viewH, MouseButtons.Left, null);
        if (!hit0 || !view.IsResizingHandle || view.DraggingHandleIndex != 0)
        {
            Console.WriteLine("   -> [FAIL] Không bắt được chốt 0 Top-Left!");
            Environment.Exit(1);
        }
        // Kéo chuột sang trái 30px, lên trên 20px (mở rộng hộp chữ)
        view.HandleMouseMove((int)hdls[0].X - 30, (int)hdls[0].Y - 20, viewW, viewH, MouseButtons.Left);
        view.HandleMouseUp((int)hdls[0].X - 30, (int)hdls[0].Y - 20, viewW, viewH, MouseButtons.Left);
        Console.WriteLine(string.Format("   Tọa độ và Kích thước mới: X={0}, Y={1}, {2}x{3} (Kỳ vọng: X=70, Y=80, 280x90)",
            txt.Bounds.X, txt.Bounds.Y, txt.Bounds.Width, txt.Bounds.Height));
        if (Math.Abs(txt.Bounds.X - 70f) > 1f || Math.Abs(txt.Bounds.Y - 80f) > 1f ||
            Math.Abs(txt.Bounds.Width - 280f) > 1f || Math.Abs(txt.Bounds.Height - 90f) > 1f)
        {
            Console.WriteLine("   -> [FAIL] Kéo góc 0 Top-Left không đúng tọa độ/kích thước!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Kéo góc Top-Left thành công!");

        // --- TEST 3: Kéo góc trên bên phải (Top-Right, chốt 2) ---
        hdls = getHandles();
        Console.WriteLine("3. Thử kéo góc Top-Right (chốt 2)...");
        bool hit2 = view.HandleMouseDown((int)hdls[2].X, (int)hdls[2].Y, viewW, viewH, MouseButtons.Left, null);
        if (!hit2 || !view.IsResizingHandle || view.DraggingHandleIndex != 2)
        {
            Console.WriteLine("   -> [FAIL] Không bắt được chốt 2 Top-Right!");
            Environment.Exit(1);
        }
        // Kéo sang phải 40px, lên trên 15px
        view.HandleMouseMove((int)hdls[2].X + 40, (int)hdls[2].Y - 15, viewW, viewH, MouseButtons.Left);
        view.HandleMouseUp((int)hdls[2].X + 40, (int)hdls[2].Y - 15, viewW, viewH, MouseButtons.Left);
        Console.WriteLine(string.Format("   Tọa độ và Kích thước: Y={0}, {1}x{2} (Kỳ vọng: Y=65, 320x105)",
            txt.Bounds.Y, txt.Bounds.Width, txt.Bounds.Height));
        if (Math.Abs(txt.Bounds.Y - 65f) > 1f || Math.Abs(txt.Bounds.Width - 320f) > 1f || Math.Abs(txt.Bounds.Height - 105f) > 1f)
        {
            Console.WriteLine("   -> [FAIL] Kéo góc 2 Top-Right không đúng!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Kéo góc Top-Right thành công!");

        // --- TEST 4: Kéo góc dưới bên trái (Bottom-Left, chốt 6) ---
        hdls = getHandles();
        Console.WriteLine("4. Thử kéo góc Bottom-Left (chốt 6)...");
        bool hit6 = view.HandleMouseDown((int)hdls[6].X, (int)hdls[6].Y, viewW, viewH, MouseButtons.Left, null);
        if (!hit6 || !view.IsResizingHandle || view.DraggingHandleIndex != 6)
        {
            Console.WriteLine("   -> [FAIL] Không bắt được chốt 6 Bottom-Left!");
            Environment.Exit(1);
        }
        // Kéo sang trái 20px, xuống dưới 25px
        view.HandleMouseMove((int)hdls[6].X - 20, (int)hdls[6].Y + 25, viewW, viewH, MouseButtons.Left);
        view.HandleMouseUp((int)hdls[6].X - 20, (int)hdls[6].Y + 25, viewW, viewH, MouseButtons.Left);
        Console.WriteLine(string.Format("   Tọa độ và Kích thước: X={0}, {1}x{2} (Kỳ vọng: X=50, 340x130)",
            txt.Bounds.X, txt.Bounds.Width, txt.Bounds.Height));
        if (Math.Abs(txt.Bounds.X - 50f) > 1f || Math.Abs(txt.Bounds.Width - 340f) > 1f || Math.Abs(txt.Bounds.Height - 130f) > 1f)
        {
            Console.WriteLine("   -> [FAIL] Kéo góc 6 Bottom-Left không đúng!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Kéo góc Bottom-Left thành công!");

        // --- TEST 5: Thử nghiệm bấm lệch tâm 8px (vùng bấm nhạy) ---
        hdls = getHandles();
        Console.WriteLine("5. Thử bấm lệch tâm 8px vào chốt 4 Bottom-Right...");
        bool hitOffset = view.HandleMouseDown((int)hdls[4].X + 8, (int)hdls[4].Y + 8, viewW, viewH, MouseButtons.Left, null);
        if (!hitOffset || !view.IsResizingHandle || view.DraggingHandleIndex != 4)
        {
            Console.WriteLine("   -> [FAIL] Bấm lệch tâm 8px không bắt được!");
            Environment.Exit(1);
        }
        view.HandleMouseUp((int)hdls[4].X + 8, (int)hdls[4].Y + 8, viewW, viewH, MouseButtons.Left);
        Console.WriteLine("   -> [PASS] Vùng bấm nhạy 10px bắt chốt hoàn hảo!");

        Console.WriteLine("\n=== TẤT CẢ 5 BÀI KIỂM THỬ KÉO GÓC ĐÃ VƯỢT QUA 100%! ===");
    }
}

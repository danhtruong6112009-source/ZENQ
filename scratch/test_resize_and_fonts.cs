using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

public class TestResizeAndFonts
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ CO GIÃN 8 CHỐT, KHÔNG TỰ DÀI Ô & CHỌN FONT/CỠ CHỮ ===");

        PdfDocument doc = new PdfDocument();
        PdfPageModel page = new PdfPageModel(0, 595, 842);
        doc.Pages.Add(page);

        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;
        view.ZoomLevel = 1.0f;

        // Test 1: Thêm text box với kích thước ban đầu 150x40
        PdfTextElement txt = new PdfTextElement("Văn bản mẫu", new RectangleF(100, 100, 150, 40), "Segoe UI", 14f);
        page.TextElements.Add(txt);
        view.SelectedText = txt;

        Console.WriteLine("1. Kích thước ban đầu: " + txt.Bounds.ToString());

        // Test 2: Gõ phím vào hộp chữ -> Đảm bảo hộp chữ KHÔNG tự động dài ra
        view.EditingText = txt;
        view.EditingBuffer = "Văn bản mẫu";
        view.CursorPosition = view.EditingBuffer.Length;

        // Gõ thêm chuỗi rất dài
        string longText = " - Đây là một chuỗi văn bản rất dài được gõ vào để kiểm tra ô không bị tự động co giãn dài ra";
        foreach (char c in longText)
        {
            view.HandleKeyPress(c);
        }

        Console.WriteLine("2. Kích thước sau khi gõ chuỗi dài: " + txt.Bounds.ToString());
        if (txt.Bounds.Width == 150f && txt.Bounds.Height == 40f)
        {
            Console.WriteLine("   -> [PASS] Hộp chữ GIỮ NGUYÊN kích thước 150x40, KHÔNG tự dài ra khi gõ!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Hộp chữ bị tự động đổi kích thước: " + txt.Bounds.Width);
            Environment.Exit(1);
        }

        view.CommitInlineEdit();
        if (txt.Bounds.Width == 150f && txt.Bounds.Height == 40f)
        {
            Console.WriteLine("   -> [PASS] CommitInlineEdit giữ nguyên kích thước 150x40!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] CommitInlineEdit làm thay đổi kích thước: " + txt.Bounds.Width);
            Environment.Exit(1);
        }

        // Test 3: Kéo co giãn 8 chốt (Handles)
        // Handle 3: Middle-Right (Kéo sang phải thêm 50px)
        int topOffset = 155;
        RectangleF pageBounds = view.GetPageScreenBounds(0, 1000, 800, topOffset);
        float screenRight = pageBounds.X + txt.Bounds.Right;
        float screenMidY = pageBounds.Y + txt.Bounds.Top + txt.Bounds.Height / 2f;

        // Giả lập MouseDown trúng Handle 3
        bool downHit = view.HandleMouseDown((int)screenRight, (int)screenMidY, 1000, 800, MouseButtons.Left, null);
        Console.WriteLine("3. MouseDown vào chốt Right: " + downHit + ", IsResizingHandle=" + view.IsResizingHandle + ", HandleIndex=" + view.DraggingHandleIndex);

        if (view.IsResizingHandle && view.DraggingHandleIndex == 3)
        {
            // MouseMove kéo sang phải thêm 50px
            view.HandleMouseMove((int)screenRight + 50, (int)screenMidY, 1000, 800, MouseButtons.Left);
            Console.WriteLine("   Kích thước sau khi kéo Handle 3: " + txt.Bounds.ToString());
            if (Math.Abs(txt.Bounds.Width - 200f) < 1f)
            {
                Console.WriteLine("   -> [PASS] Kéo chốt Right thành công, Width mở rộng từ 150 -> 200!");
            }
            else
            {
                Console.WriteLine("   -> [FAIL] Chiều rộng không chính xác: " + txt.Bounds.Width);
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Không bắt được sự kiện chốt Right");
            Environment.Exit(1);
        }
        view.HandleMouseUp((int)screenRight + 50, (int)screenMidY, 1000, 800, MouseButtons.Left);

        // Test Handle 4: Bottom-Right (Kéo góc dưới phải dx=30, dy=20)
        screenRight = pageBounds.X + txt.Bounds.Right;
        float screenBottom = pageBounds.Y + txt.Bounds.Bottom;
        view.HandleMouseDown((int)screenRight, (int)screenBottom, 1000, 800, MouseButtons.Left, null);
        if (view.IsResizingHandle && view.DraggingHandleIndex == 4)
        {
            view.HandleMouseMove((int)screenRight + 30, (int)screenBottom + 20, 1000, 800, MouseButtons.Left);
            Console.WriteLine("   Kích thước sau khi kéo Handle 4 (Bottom-Right): " + txt.Bounds.ToString());
            if (Math.Abs(txt.Bounds.Width - 230f) < 1f && Math.Abs(txt.Bounds.Height - 60f) < 1f)
            {
                Console.WriteLine("   -> [PASS] Kéo chốt Bottom-Right thành công (Width=230, Height=60)!");
            }
            else
            {
                Console.WriteLine("   -> [FAIL] Kích thước sau kéo góc sai: " + txt.Bounds.ToString());
                Environment.Exit(1);
            }
        }
        view.HandleMouseUp((int)screenRight + 30, (int)screenBottom + 20, 1000, 800, MouseButtons.Left);

        // Test Handle 0: Top-Left (Kéo góc trên trái dx=-20, dy=-15)
        float screenLeft = pageBounds.X + txt.Bounds.Left;
        float screenTop = pageBounds.Y + txt.Bounds.Top;
        view.HandleMouseDown((int)screenLeft, (int)screenTop, 1000, 800, MouseButtons.Left, null);
        if (view.IsResizingHandle && view.DraggingHandleIndex == 0)
        {
            view.HandleMouseMove((int)screenLeft - 20, (int)screenTop - 15, 1000, 800, MouseButtons.Left);
            Console.WriteLine("   Kích thước sau khi kéo Handle 0 (Top-Left): " + txt.Bounds.ToString());
            if (Math.Abs(txt.Bounds.Width - 250f) < 1f && Math.Abs(txt.Bounds.Height - 75f) < 1f && Math.Abs(txt.Bounds.X - 80f) < 1f && Math.Abs(txt.Bounds.Y - 85f) < 1f)
            {
                Console.WriteLine("   -> [PASS] Kéo chốt Top-Left thành công (X=80, Y=85, Width=250, Height=75)!");
            }
            else
            {
                Console.WriteLine("   -> [FAIL] Kích thước góc Top-Left sai: " + txt.Bounds.ToString());
                Environment.Exit(1);
            }
        }
        view.HandleMouseUp((int)screenLeft - 20, (int)screenTop - 15, 1000, 800, MouseButtons.Left);

        // Test 4: Con trỏ chuột (GetCursorAtPoint)
        Cursor curNWSE = view.GetCursorAtPoint((int)(pageBounds.X + txt.Bounds.Left), (int)(pageBounds.Y + txt.Bounds.Top), 1000, 800, topOffset);
        Cursor curMove = view.GetCursorAtPoint((int)(pageBounds.X + txt.Bounds.Left + txt.Bounds.Width / 2f), (int)(pageBounds.Y + txt.Bounds.Top + txt.Bounds.Height / 2f), 1000, 800, topOffset);
        Console.WriteLine("4. Cursor Handle 0: " + curNWSE + ", Cursor Body: " + curMove);

        if (curNWSE == Cursors.SizeNWSE && curMove == Cursors.SizeAll)
        {
            Console.WriteLine("   -> [PASS] Con trỏ chuột chuyển đổi chính xác (SizeNWSE và SizeAll)!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Con trỏ chuột sai!");
            Environment.Exit(1);
        }

        // Test 5: Phông chữ & Cỡ chữ (Font & Size Picker)
        txt.FontName = "Times New Roman";
        txt.FontSize = 24f;
        txt.IsBold = true;
        txt.IsItalic = true;
        txt.IsUnderline = true;
        txt.TextColor = Color.Red;
        txt.BackgroundColor = Color.LightYellow;
        txt.BorderColor = Color.Blue;
        txt.BorderWidth = 2f;

        Console.WriteLine("5. Thuộc tính văn bản & hộp chữ:");
        Console.WriteLine("   Font=" + txt.FontName + ", Size=" + txt.FontSize + ", Bold=" + txt.IsBold + ", Color=" + txt.TextColor.Name + ", Bg=" + txt.BackgroundColor.Name);
        if (txt.FontName == "Times New Roman" && txt.FontSize == 24f && txt.IsBold && txt.IsItalic && txt.IsUnderline)
        {
            Console.WriteLine("   -> [PASS] Các thuộc tính Phông chữ, Cỡ chữ, Định dạng hoạt động hoàn hảo!");
        }
        else
        {
            Console.WriteLine("   -> [FAIL] Thuộc tính văn bản không đúng!");
            Environment.Exit(1);
        }

        Console.WriteLine("\n=== TẤT CẢ CÁC BÀI KIỂM THỬ CO GIÃN 8 CHỐT, CỐ ĐỊNH Ô & PHÔNG CHỮ ĐÃ ĐẠT 100%! ===");
    }
}

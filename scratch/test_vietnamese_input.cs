using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.UI;

public class TestVietnameseInput
{
    public static void Main()
    {
        Console.WriteLine("=== KIỂM THỬ GÕ TIẾNG VIỆT (TELEX / VNI / UNIKEY) ===");
        PdfView view = new PdfView();
        var txt = new PdfTextElement("", new RectangleF(100, 100, 200, 50), "Segoe UI", 12f);
        view.EditingText = txt;
        view.EditingBuffer = "";
        view.CursorPosition = 0;

        // Mô phỏng chuỗi phím UniKey Telex khi gõ từ "Tiếng":
        // 1. 'T'
        view.HandleKeyPress('T');
        // 2. 'i'
        view.HandleKeyPress('i');
        // 3. 'e'
        view.HandleKeyPress('e');
        // 4. 'e' -> UniKey gửi Backspace '\b' và 'ê'
        view.HandleKeyPress('\b');
        view.HandleKeyPress('ê');
        // 5. 'n'
        view.HandleKeyPress('n');
        // 6. 'g'
        view.HandleKeyPress('g');
        // 7. 's' (dấu sắc) -> UniKey gửi 4 phím backspace và gõ "iếng"
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('i');
        view.HandleKeyPress('ế');
        view.HandleKeyPress('n');
        view.HandleKeyPress('g');

        Console.WriteLine("Kết quả gõ 'Tiếng': " + view.EditingBuffer);
        if (view.EditingBuffer != "Tiếng")
        {
            Console.WriteLine("[FAIL] Lỗi gõ tiếng Việt! Kết quả thực tế: " + view.EditingBuffer);
            Environment.Exit(1);
        }
        Console.WriteLine("[PASS] Gõ Tiếng Việt thành công 100%!");
    }
}

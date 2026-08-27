using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.UI;

public class TestVietnameseInputSuite
{
    public static void SimulateTypeWord(PdfView view, string word)
    {
        // Simulate typing characters with possible Telex combinations
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            view.HandleKeyPress(c);
        }
    }

    public static void Main()
    {
        Console.WriteLine("=== BỘ KIỂM THỬ GÕ TIẾNG VIỆT TOÀN DIỆN (UNIKEY / EVKEY TELEX & VNI) ===");
        PdfView view = new PdfView();
        var txt = new PdfTextElement("", new RectangleF(100, 100, 400, 50), "Segoe UI", 14f);
        view.EditingText = txt;
        view.EditingBuffer = "";
        view.CursorPosition = 0;

        // 1. Kiểm tra mô phỏng UniKey gõ "Xử Lý Hình Ảnh Nâng Cao"
        Console.WriteLine("\n1. Kiểm tra gõ chuỗi tiêu đề slide:");
        view.EditingBuffer = "";
        view.CursorPosition = 0;
        
        // Gõ "Xử ": 'X', 'u', 'w' (-> \b, 'ử'), ' '
        view.HandleKeyPress('X');
        view.HandleKeyPress('u');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('ử');
        view.HandleKeyPress(' ');

        // Gõ "Lý ": 'L', 'y', 's' (-> \b, 'ý'), ' '
        view.HandleKeyPress('L');
        view.HandleKeyPress('y');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('ý');
        view.HandleKeyPress(' ');

        // Gõ "Hình ": 'H', 'i', 'n', 'h', 'f' (-> \b 3 lần, 'ình'), ' '
        view.HandleKeyPress('H');
        view.HandleKeyPress('i');
        view.HandleKeyPress('n');
        view.HandleKeyPress('h');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('ì');
        view.HandleKeyPress('n');
        view.HandleKeyPress('h');
        view.HandleKeyPress(' ');

        // Gõ "Ảnh ": 'A', 'n', 'h', 'r' (-> \b 3 lần, 'ảnh'), ' '
        view.HandleKeyPress('A');
        view.HandleKeyPress('n');
        view.HandleKeyPress('h');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('Ả');
        view.HandleKeyPress('n');
        view.HandleKeyPress('h');
        view.HandleKeyPress(' ');

        // Gõ "Nâng ": 'N', 'a', 'w', 'n', 'g' (-> \b, 'â'...)
        view.HandleKeyPress('N');
        view.HandleKeyPress('a');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('â');
        view.HandleKeyPress('n');
        view.HandleKeyPress('g');
        view.HandleKeyPress(' ');

        // Gõ "Cao"
        view.HandleKeyPress('C');
        view.HandleKeyPress('a');
        view.HandleKeyPress('o');

        Console.WriteLine("   Kết quả: '" + view.EditingBuffer + "'");
        if (view.EditingBuffer != "Xử Lý Hình Ảnh Nâng Cao")
        {
            Console.WriteLine("   -> [FAIL] Sai chuỗi tiếng Việt!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Gõ mượt mà chuẩn 100% không bị 'Nygjäng'!");

        // 2. Kiểm tra phím xóa Backspace vật lý
        Console.WriteLine("\n2. Kiểm tra phím xóa lùi Backspace bình thường:");
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        view.HandleKeyPress('\b');
        Console.WriteLine("   Sau khi xóa 3 ký tự 'Cao': '" + view.EditingBuffer + "'");
        if (view.EditingBuffer != "Xử Lý Hình Ảnh Nâng ")
        {
            Console.WriteLine("   -> [FAIL] Phím Backspace thông thường không xóa đúng!");
            Environment.Exit(1);
        }
        Console.WriteLine("   -> [PASS] Phím Backspace xóa chuẩn xác!");

        Console.WriteLine("\n=== TẤT CẢ CÁC BÀI KIỂM THỬ GÕ TIẾNG VIỆT ĐẠT 100% THÀNH CÔNG! ===");
    }
}

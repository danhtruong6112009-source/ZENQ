using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestWordPrediction
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== BẮT ĐẦU KIỂM TRA TÍNH NĂNG GỢI Ý TỪ THÔNG MINH (INTELLISENSE) ===");
            
            // 1. Kiểm tra VietnameseWordPredictor
            Console.WriteLine("\n[1] Kiểm tra bộ máy VietnameseWordPredictor:");
            var sugg1 = VietnameseWordPredictor.GetSuggestions("từ", 3);
            Console.WriteLine("Gợi ý cho 'từ' (lần 1): " + string.Join(", ", sugg1));
            if (sugg1.Count != 3) throw new Exception("Phải trả về đúng 3 gợi ý!");

            var sugg2 = VietnameseWordPredictor.GetSuggestions("từ", 3);
            Console.WriteLine("Gợi ý cho 'từ' (lần 2 - ngẫu nhiên): " + string.Join(", ", sugg2));
            if (sugg2.Count != 3) throw new Exception("Phải trả về đúng 3 gợi ý!");

            var suggCap = VietnameseWordPredictor.GetSuggestions("Từ", 3);
            Console.WriteLine("Gợi ý cho 'Từ' (TitleCase): " + string.Join(", ", suggCap));
            if (!suggCap[0].StartsWith("Từ")) throw new Exception("Không giữ được TitleCase!");

            var suggUpper = VietnameseWordPredictor.GetSuggestions("TỪ", 3);
            Console.WriteLine("Gợi ý cho 'TỪ' (UPPERCASE): " + string.Join(", ", suggUpper));
            if (!suggUpper[0].StartsWith("TỪ")) throw new Exception("Không giữ được UPPERCASE!");

            var suggUnacc = VietnameseWordPredictor.GetSuggestions("tu", 3);
            Console.WriteLine("Gợi ý cho 'tu' (không dấu): " + string.Join(", ", suggUnacc));
            if (suggUnacc.Count != 3) throw new Exception("Gợi ý không dấu thất bại!");

            var suggTelex = VietnameseWordPredictor.GetSuggestions("twf", 3);
            Console.WriteLine("Gợi ý cho 'twf' (telex fallback): " + string.Join(", ", suggTelex));
            if (suggTelex.Count != 3) throw new Exception("Gợi ý telex fallback thất bại!");

            // 2. Kiểm tra tính độc lập của cài đặt (Independence test)
            Console.WriteLine("\n[2] Kiểm tra tính độc lập của Cài đặt:");
            AppSettings.Instance.AutoCorrectVietnamese = true;
            AppSettings.Instance.WordPredictionEnabled = true;
            AppSettings.Instance.Save();

            // Tắt autocorrect, bật suggestion
            AppSettings.Instance.AutoCorrectVietnamese = false;
            AppSettings.Instance.WordPredictionEnabled = true;
            AppSettings.Instance.Save();
            var reloaded = AppSettings.Load();
            Console.WriteLine("AutoCorrect: " + reloaded.AutoCorrectVietnamese + ", WordPrediction: " + reloaded.WordPredictionEnabled);
            if (reloaded.AutoCorrectVietnamese != false || reloaded.WordPredictionEnabled != true)
                throw new Exception("Lưu/tải độc lập thất bại khi AutoCorrect=false, WordPrediction=true");

            // Bật autocorrect, tắt suggestion
            AppSettings.Instance.AutoCorrectVietnamese = true;
            AppSettings.Instance.WordPredictionEnabled = false;
            AppSettings.Instance.Save();
            reloaded = AppSettings.Load();
            Console.WriteLine("AutoCorrect: " + reloaded.AutoCorrectVietnamese + ", WordPrediction: " + reloaded.WordPredictionEnabled);
            if (reloaded.AutoCorrectVietnamese != true || reloaded.WordPredictionEnabled != false)
                throw new Exception("Lưu/tải độc lập thất bại khi AutoCorrect=true, WordPrediction=false");

            // Khôi phục cả hai về true
            AppSettings.Instance.AutoCorrectVietnamese = true;
            AppSettings.Instance.WordPredictionEnabled = true;
            AppSettings.Instance.Save();

            // 3. Kiểm tra thành phần UI WordSuggestionBox
            Console.WriteLine("\n[3] Kiểm tra WordSuggestionBox UI:");
            WordSuggestionBox box = new WordSuggestionBox();
            string selectedResult = null;
            box.Show(200, 300, 1200, 800, "từ", sugg1, (choice) => { selectedResult = choice; });
            if (!box.IsVisible) throw new Exception("Hộp gợi ý phải hiển thị!");
            if (box.SelectedIndex != 0) throw new Exception("Mục đầu tiên phải được chọn mặc định!");

            // Duyệt phím xuống
            box.MoveDown();
            if (box.SelectedIndex != 1) throw new Exception("MoveDown phải chọn index 1!");
            box.MoveDown();
            if (box.SelectedIndex != 2) throw new Exception("MoveDown phải chọn index 2!");
            box.MoveDown();
            if (box.SelectedIndex != 0) throw new Exception("MoveDown vòng tròn phải về index 0!");

            // Duyệt phím lên
            box.MoveUp();
            if (box.SelectedIndex != 2) throw new Exception("MoveUp vòng tròn phải về index 2!");

            // Chọn bằng Tab/Enter
            bool applied = box.ApplySelected();
            if (!applied || selectedResult != sugg1[2]) throw new Exception("ApplySelected thất bại!");
            if (box.IsVisible) throw new Exception("Hộp gợi ý phải đóng sau khi áp dụng!");

            // Click chuột trực tiếp
            box.Show(200, 300, 1200, 800, "từ", sugg1, (choice) => { selectedResult = choice; });
            int clickItem1Y = box.Bounds.Y + 3 + 28 / 2; // Giữa mục 0
            box.HandleClick(box.Bounds.X + 50, clickItem1Y);
            if (selectedResult != sugg1[0]) throw new Exception("Click chuột vào mục 0 thất bại!");

            Console.WriteLine("[SUCCESS] Tất cả các kiểm tra đơn vị hoàn toàn thành công!");
        }
    }
}

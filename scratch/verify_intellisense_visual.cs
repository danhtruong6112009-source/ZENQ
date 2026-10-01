using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class VerifyIntellisenseVisual
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== BẮT ĐẦU KIỂM TRA TRỰC QUAN GIAO DIỆN INTELLISENSE ===");

            AppSettings.Instance.AutoCorrectVietnamese = false;
            AppSettings.Instance.WordPredictionEnabled = false;
            AppSettings.Instance.RibbonStyle = RibbonStyle.Island;
            AppSettings.Instance.Save();

            // 1. Chụp ảnh màn hình Cài đặt (HomeView Settings) với cả 2 tính năng đang TẮT mặc định
            using (AppForm app = new AppForm())
            {
                app.Size = new Size(1280, 800);
                app.CreateControl();
                
                // Mở tab Cài đặt trên HomeView
                app.Ribbon.CurrentMode = OfficeAppMode.Home;
                app.HomeView.ActiveTab = 2; // Cài đặt
                string artifactDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";

                // Chụp phần đầu (Scroll 0)
                app.HomeView.TargetScrollY = 0;
                app.HomeView.SmoothScrollY = 0;
                Bitmap bmpTop = new Bitmap(1280, 800);
                using (Graphics g = Graphics.FromImage(bmpTop))
                {
                    app.HomeView.Draw(g, 1280, 800);
                }
                bmpTop.Save(Path.Combine(artifactDir, "verify_settings_top.png"), ImageFormat.Png);

                // Chụp phần thân & cuối (Scroll 320)
                app.HomeView.TargetScrollY = 320;
                app.HomeView.SmoothScrollY = 320;
                Bitmap bmpSettings = new Bitmap(1280, 800);
                using (Graphics g = Graphics.FromImage(bmpSettings))
                {
                    app.HomeView.Draw(g, 1280, 800);
                }
                string settingsPath = Path.Combine(artifactDir, "verify_settings_word_prediction.png");
                bmpSettings.Save(settingsPath, ImageFormat.Png);
                Console.WriteLine("[1] Đã lưu ảnh chụp Cài đặt (Mặc định cả 2 tính năng TẮT): " + settingsPath);
            }

            // 1b. Kiểm tra khi TẮT thì gõ phím không hiện popup gợi ý
            using (AppForm app = new AppForm())
            {
                app.Size = new Size(1280, 800);
                app.CreateControl();
                app.SwitchAppMode(OfficeAppMode.Writer);
                app.WriterView.PieceTable.Clear();
                app.WriterView.CursorPosition = 0;

                MethodInvoker onKeyPress = () =>
                {
                    System.Reflection.MethodInfo miKP = typeof(AppForm).GetMethod("OnKeyPress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    miKP.Invoke(app, new object[] { new KeyPressEventArgs('t') });
                    miKP.Invoke(app, new object[] { new KeyPressEventArgs('ừ') });
                };
                onKeyPress();

                if (app.WordSuggestions.IsVisible)
                {
                    throw new Exception("LỖI: Bảng gợi ý không được hiện khi tính năng đang TẮT!");
                }
                Console.WriteLine("[1b] Đã xác nhận: Khi tính năng TẮT, gõ phím hoàn toàn không hiện popup!");
            }

            // 2. Bật tính năng lên để kiểm tra hoạt động khi người dùng bật
            AppSettings.Instance.WordPredictionEnabled = true;
            using (AppForm app = new AppForm())
            {
                app.Size = new Size(1280, 800);
                app.CreateControl();

                // Chuyển sang Writer
                app.SwitchAppMode(OfficeAppMode.Writer);
                app.WriterView.PieceTable.Clear();
                app.WriterView.CursorPosition = 0;

                // Mô phỏng gõ "t" và "ừ"
                MethodInvoker onKeyPress = () =>
                {
                    KeyPressEventArgs e1 = new KeyPressEventArgs('t');
                    System.Reflection.MethodInfo miKP = typeof(AppForm).GetMethod("OnKeyPress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    miKP.Invoke(app, new object[] { e1 });

                    KeyPressEventArgs e2 = new KeyPressEventArgs('ừ');
                    miKP.Invoke(app, new object[] { e2 });
                };
                onKeyPress();

                Console.WriteLine("WordSuggestions visible: " + app.WordSuggestions.IsVisible);
                Console.WriteLine("Current suggestions: " + string.Join(", ", app.WordSuggestions.Suggestions));
                Console.WriteLine("SelectedIndex: " + app.WordSuggestions.SelectedIndex);
                Console.WriteLine("Suggestion Bounds: " + app.WordSuggestions.Bounds);

                Bitmap bmpPopup = new Bitmap(1280, 800);
                using (Graphics g = Graphics.FromImage(bmpPopup))
                {
                    System.Reflection.MethodInfo miPaint = typeof(AppForm).GetMethod("OnPaint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    PaintEventArgs pe = new PaintEventArgs(g, new Rectangle(0, 0, 1280, 800));
                    miPaint.Invoke(app, new object[] { pe });
                }
                string artifactDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";
                string popupPath = Path.Combine(artifactDir, "verify_word_suggestion_popup.png");
                bmpPopup.Save(popupPath, ImageFormat.Png);
                Console.WriteLine("[2] Đã lưu ảnh chụp Popup Gợi ý: " + popupPath);

                // 3. Mô phỏng phím Down rồi phím Tab để chọn gợi ý thứ 2
                System.Reflection.MethodInfo miKD = typeof(AppForm).GetMethod("OnKeyDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                KeyEventArgs eDown = new KeyEventArgs(Keys.Down);
                miKD.Invoke(app, new object[] { eDown });
                Console.WriteLine("Sau khi nhấn Mũi tên Xuống, SelectedIndex = " + app.WordSuggestions.SelectedIndex);

                string expectedChosen = app.WordSuggestions.Suggestions[app.WordSuggestions.SelectedIndex];
                KeyEventArgs eTab = new KeyEventArgs(Keys.Tab);
                miKD.Invoke(app, new object[] { eTab });

                Console.WriteLine("Sau khi nhấn Tab, WordSuggestions visible: " + app.WordSuggestions.IsVisible);
                string textAfterTab = app.WriterView.PieceTable.GetText();
                Console.WriteLine("Văn bản sau khi chọn Tab: '" + textAfterTab + "'");

                Bitmap bmpAfter = new Bitmap(1280, 800);
                using (Graphics g = Graphics.FromImage(bmpAfter))
                {
                    System.Reflection.MethodInfo miPaint = typeof(AppForm).GetMethod("OnPaint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    PaintEventArgs pe = new PaintEventArgs(g, new Rectangle(0, 0, 1280, 800));
                    miPaint.Invoke(app, new object[] { pe });
                }
                string afterTabPath = Path.Combine(artifactDir, "verify_word_suggestion_applied.png");
                bmpAfter.Save(afterTabPath, ImageFormat.Png);
                Console.WriteLine("[3] Đã lưu ảnh chụp sau khi nhấn Tab: " + afterTabPath);

                if (!textAfterTab.Contains(expectedChosen))
                {
                    throw new Exception("Văn bản chưa áp dụng đúng cụm từ: " + expectedChosen);
                }
            }

            // Đặt lại mặc định tắt cả 2 tính năng
            AppSettings.Instance.AutoCorrectVietnamese = false;
            AppSettings.Instance.WordPredictionEnabled = false;
            AppSettings.Instance.Save();

            Console.WriteLine("\n[SUCCESS] TOÀN BỘ KIỂM TRA TRỰC QUAN ĐÃ HOÀN TẤT THÀNH CÔNG RỰC RỠ!");
        }
    }
}

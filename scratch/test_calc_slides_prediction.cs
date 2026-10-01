using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestCalcSlidesPrediction
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KIỂM TRA GỢI Ý TỪ TRONG CALC VÀ SLIDES ===");

            // 1. CalcView
            using (AppForm app = new AppForm())
            {
                app.Size = new Size(1280, 800);
                app.CreateControl();
                app.SwitchAppMode(OfficeAppMode.Calc);

                // Bắt đầu gõ vào ô tính
                app.CalcView.IsEditing = true;
                app.CalcView.EditText = "";
                app.CalcView.EditCursorPos = 0;

                MethodInvoker onKeyPress = () =>
                {
                    KeyPressEventArgs e1 = new KeyPressEventArgs('t');
                    System.Reflection.MethodInfo miKP = typeof(AppForm).GetMethod("OnKeyPress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    miKP.Invoke(app, new object[] { e1 });

                    KeyPressEventArgs e2 = new KeyPressEventArgs('ừ');
                    miKP.Invoke(app, new object[] { e2 });
                };
                onKeyPress();

                Console.WriteLine("[Calc] WordSuggestions visible: " + app.WordSuggestions.IsVisible);
                Console.WriteLine("[Calc] Suggestions: " + string.Join(", ", app.WordSuggestions.Suggestions));
                if (!app.WordSuggestions.IsVisible || app.WordSuggestions.Suggestions.Count != 3)
                {
                    throw new Exception("Calc: Không hiển thị 3 gợi ý!");
                }

                // Chọn gợi ý bằng Tab
                string choice = app.WordSuggestions.Suggestions[0];
                System.Reflection.MethodInfo miKD = typeof(AppForm).GetMethod("OnKeyDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                KeyEventArgs eTab = new KeyEventArgs(Keys.Tab);
                miKD.Invoke(app, new object[] { eTab });

                Console.WriteLine("[Calc] Sau Tab, EditText = '" + app.CalcView.EditText + "'");
                if (!app.CalcView.EditText.Contains(choice))
                {
                    throw new Exception("Calc: Áp dụng gợi ý thất bại!");
                }
            }

            // 2. SlidesView
            using (AppForm app = new AppForm())
            {
                app.Size = new Size(1280, 800);
                app.CreateControl();
                app.SwitchAppMode(OfficeAppMode.Slides);

                // Thêm hình khối và bắt đầu chỉnh sửa text inline
                var shape = new ShapeNode { Type = ShapeType.Rectangle, Transform = new Transform2D(100, 100, 200, 100), Text = "" };
                app.SlidesView.Presentation.Slides[0].Shapes.Add(shape);
                app.SlidesView.StartInlineEditing(shape);

                MethodInvoker onKeyPress = () =>
                {
                    KeyPressEventArgs e1 = new KeyPressEventArgs('c');
                    System.Reflection.MethodInfo miKP = typeof(AppForm).GetMethod("OnKeyPress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    miKP.Invoke(app, new object[] { e1 });

                    KeyPressEventArgs e2 = new KeyPressEventArgs('ô');
                    miKP.Invoke(app, new object[] { e2 });

                    KeyPressEventArgs e3 = new KeyPressEventArgs('n');
                    miKP.Invoke(app, new object[] { e3 });

                    KeyPressEventArgs e4 = new KeyPressEventArgs('g');
                    miKP.Invoke(app, new object[] { e4 });
                };
                onKeyPress();

                Console.WriteLine("[Slides] WordSuggestions visible: " + app.WordSuggestions.IsVisible);
                Console.WriteLine("[Slides] Suggestions for 'công': " + string.Join(", ", app.WordSuggestions.Suggestions));
                if (!app.WordSuggestions.IsVisible || app.WordSuggestions.Suggestions.Count != 3)
                {
                    throw new Exception("Slides: Không hiển thị 3 gợi ý!");
                }

                // Chọn gợi ý bằng Tab
                string choice = app.WordSuggestions.Suggestions[0];
                System.Reflection.MethodInfo miKD = typeof(AppForm).GetMethod("OnKeyDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                KeyEventArgs eTab = new KeyEventArgs(Keys.Tab);
                miKD.Invoke(app, new object[] { eTab });

                Console.WriteLine("[Slides] Sau Tab, EditingTextBuffer = '" + app.SlidesView.EditingTextBuffer + "'");
                if (!app.SlidesView.EditingTextBuffer.Contains(choice))
                {
                    throw new Exception("Slides: Áp dụng gợi ý thất bại!");
                }
            }

            Console.WriteLine("\n[SUCCESS] CẢ CALC VÀ SLIDES ĐỀU HOẠT ĐỘNG HOÀN HẢO!");
        }
    }
}

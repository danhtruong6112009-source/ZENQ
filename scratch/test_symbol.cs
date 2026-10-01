using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Test
{
    class Program
    {
        static void Main()
        {
            using (Bitmap bmp = new Bitmap(600, 100))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (Font baseFont = new Font("Times New Roman", 12f, FontStyle.Bold))
                using (Font emojiFont = new Font("Segoe UI Emoji", 12f, FontStyle.Bold))
                using (SolidBrush greenBrush = new SolidBrush(Color.FromArgb(16, 137, 62)))
                {
                    string pre = "DANH SÁCH ĐÓNG TIỀN (Tick ";
                    string sym = "✔";
                    string post = " vào ô nếu đã đóng):";

                    float x = 20;
                    float y = 30;

                    g.DrawString(pre, baseFont, Brushes.Black, x, y);
                    float w1 = g.MeasureString(pre, baseFont).Width;

                    // Draw symbol in green
                    Font sFont = emojiFont;
                    g.DrawString(sym, sFont, greenBrush, x + w1 - 4, y);
                    float w2 = g.MeasureString(sym, sFont).Width;

                    g.DrawString(post, baseFont, Brushes.Black, x + w1 + w2 - 8, y);
                }

                bmp.Save("scratch\\test_symbol.png", ImageFormat.Png);
                Console.WriteLine("Saved test_symbol.png");
            }
        }
    }
}

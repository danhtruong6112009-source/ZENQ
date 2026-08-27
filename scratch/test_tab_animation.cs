using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.UI;

namespace TestTabAnim
{
    class Program
    {
        static void Main()
        {
            try
            {
                HomeView home = new HomeView();
                int width = 1280;
                int height = 800;

                // Frame 0: Initial draw (Tab 0)
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    home.Draw(g, width, height);
                    Console.WriteLine(string.Format("FRAME 0 (Tab 0): CurrX={0}, TargetX={1}, CurrW={2}, TargetW={3}",
                        home.IndicatorCurrentX, home.IndicatorTargetX, home.IndicatorCurrentW, home.IndicatorTargetW));
                }

                // Click Tab 1: "Tài liệu gần đây"
                var myDocsBounds = home.GetMyDocsTabBounds(width, 0);
                string docTitle;
                home.HandleClick((int)myDocsBounds.X + 10, (int)myDocsBounds.Y + 10, width, height, out docTitle);

                Console.WriteLine(string.Format("AFTER CLICK TAB 1: ActiveTab={0}, TargetX={1}, CurrX={2}",
                    home.ActiveTab, home.IndicatorTargetX, home.IndicatorCurrentX));

                // Step animation frames
                for (int f = 1; f <= 10; f++)
                {
                    bool anim = home.UpdateSmoothScroll(height);
                    Console.WriteLine(string.Format("STEP {0}: anim={1}, CurrX={2:F1}, TargetX={3:F1}, CurrW={4:F1}, TargetW={5:F1}",
                        f, anim, home.IndicatorCurrentX, home.IndicatorTargetX, home.IndicatorCurrentW, home.IndicatorTargetW));
                }

                // Save image of Tab 1 with indicator in place
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    home.Draw(g, width, height);
                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\test_tab1_active.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SAVED: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}

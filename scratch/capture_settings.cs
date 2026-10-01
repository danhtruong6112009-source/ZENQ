using System;
using System.Drawing;
using System.Drawing.Imaging;
using GenOffice.UI;

namespace CaptureHome
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            int width = 1366;
            int height = 768;
            using (Bitmap bmp = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                HomeView home = new HomeView();
                home.ActiveTab = 2; // Cài đặt
                home.Draw(g, width, height);
                bmp.Save(@"scratch/homeview_settings_tab.png", ImageFormat.Png);
                Console.WriteLine("SETTINGS TAB SAVED!");
            }
        }
    }
}

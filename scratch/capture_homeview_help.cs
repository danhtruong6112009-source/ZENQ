using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

namespace CaptureHome
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                int width = 1366;
                int height = 768;
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    HomeView home = new HomeView();
                    home.Draw(g, width, height);

                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\homeview_with_help_btn.png";
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

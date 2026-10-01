using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.UI;

namespace TestRender
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                int width = 1280;
                int height = 800;
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    HomeView home = new HomeView();
                    home.ActiveTab = 2;
                    home.TargetScrollY = 450;
                    home.SmoothScrollY = 450;
                    home.Draw(g, width, height);

                    string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";
                    string outPath = Path.Combine(outDir, "zenq_settings_scrolled_preview.png");
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SUCCESS: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

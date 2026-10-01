using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

class TestHomeAppPaint
{
    [STAThread]
    static void Main()
    {
        HomeView hv = new HomeView();
        RibbonBar rb = new RibbonBar();
        int w = 1200, h = 700;
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            hv.Draw(g, w, h);
            rb.Draw(g, w, h, null, null, null, null);
            bmp.Save("scratch\\test_home_app_paint.png", ImageFormat.Png);
        }
        Console.WriteLine("Saved: scratch\\test_home_app_paint.png");
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

class VerifyScrollBottom
{
    [STAThread]
    static void Main()
    {
        HomeView hv = new HomeView();
        int w = 1280, h = 780;
        int maxScroll = hv.GetMaxScroll(h, w);
        Console.WriteLine("MaxScroll: " + maxScroll);
        hv.ScrollY = maxScroll;

        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            hv.Draw(g, w, h);
            bmp.Save("scratch\\homeview_scroll_bottom.png", ImageFormat.Png);
        }
        Console.WriteLine("Saved: scratch\\homeview_scroll_bottom.png");
    }
}

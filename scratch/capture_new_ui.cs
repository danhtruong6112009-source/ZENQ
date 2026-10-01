using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

class CaptureHomeView
{
    [STAThread]
    static void Main()
    {
        HomeView hv = new HomeView();
        int w = 1280, h = 780;
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            hv.ActiveTab = 3;
            hv.SubFilter = 1; // Gần đây
            hv.Draw(g, w, h);
            bmp.Save("scratch\\homeview_tab3_recent.png", ImageFormat.Png);
        }
        Console.WriteLine("Saved: scratch\\homeview_tab3.png");
    }
}

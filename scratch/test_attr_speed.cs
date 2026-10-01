using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

class Test
{
    static void Main()
    {
        Bitmap smallBmp = new Bitmap(208, 88);
        Bitmap canvas = new Bitmap(1000, 800);
        Graphics g = Graphics.FromImage(canvas);
        g.SmoothingMode = SmoothingMode.HighQuality;
        Rectangle destRect = new Rectangle(198, 359, 104, 44);
        ImageAttributes attr = new ImageAttributes();

        Stopwatch sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            g.DrawImage(smallBmp, destRect, 0, 0, smallBmp.Width, smallBmp.Height, GraphicsUnit.Pixel, attr);
        }
        sw1.Stop();

        Stopwatch sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            g.DrawImage(smallBmp, destRect);
        }
        sw2.Stop();

        Console.WriteLine("1000 draws with empty attr: " + sw1.ElapsedMilliseconds + " ms (" + (sw1.ElapsedMilliseconds / 1000.0) + " ms/draw)");
        Console.WriteLine("1000 draws simple DrawImage: " + sw2.ElapsedMilliseconds + " ms (" + (sw2.ElapsedMilliseconds / 1000.0) + " ms/draw)");
    }
}

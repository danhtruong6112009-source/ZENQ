using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

class Test
{
    static void Main()
    {
        Bitmap bigBmp = new Bitmap(2560, 992);
        Bitmap smallBmp = new Bitmap(208, 88);
        Bitmap canvas = new Bitmap(1000, 800);
        Graphics g = Graphics.FromImage(canvas);
        g.SmoothingMode = SmoothingMode.HighQuality;
        Rectangle destRect = new Rectangle(198, 359, 104, 44);
        ImageAttributes attr = new ImageAttributes();

        // Warm up
        g.DrawImage(bigBmp, destRect, 0, 0, bigBmp.Width, bigBmp.Height, GraphicsUnit.Pixel, attr);
        g.DrawImage(smallBmp, destRect, 0, 0, smallBmp.Width, smallBmp.Height, GraphicsUnit.Pixel, attr);

        Stopwatch swBig = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            g.DrawImage(bigBmp, destRect, 0, 0, bigBmp.Width, bigBmp.Height, GraphicsUnit.Pixel, attr);
        }
        swBig.Stop();

        Stopwatch swSmall = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            g.DrawImage(smallBmp, destRect, 0, 0, smallBmp.Width, smallBmp.Height, GraphicsUnit.Pixel, attr);
        }
        swSmall.Stop();

        Console.WriteLine("100 draws BIG (2560x992): " + swBig.ElapsedMilliseconds + " ms (" + (swBig.ElapsedMilliseconds / 100.0) + " ms/draw)");
        Console.WriteLine("100 draws SMALL (208x88): " + swSmall.ElapsedMilliseconds + " ms (" + (swSmall.ElapsedMilliseconds / 100.0) + " ms/draw)");
        Console.WriteLine("Speedup: " + ((double)swBig.ElapsedMilliseconds / Math.Max(1, swSmall.ElapsedMilliseconds)) + "x faster!");
    }
}

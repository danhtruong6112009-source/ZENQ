using System;
using System.Drawing;
using System.Drawing.Imaging;

class MakeCmpFixed
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test.png"))
        using (Bitmap cmp = new Bitmap(1920 * 2, 1080))
        using (Graphics g = Graphics.FromImage(cmp))
        {
            g.DrawImage(bMs, 0, 0, 1920, 1080);
            g.DrawImage(bGen, 1920, 0, 1920, 1080);
            cmp.Save(@"scratch\cmp_s8_fixed.png", ImageFormat.Png);
            Console.WriteLine("Saved cmp_s8_fixed.png");
        }
    }
}

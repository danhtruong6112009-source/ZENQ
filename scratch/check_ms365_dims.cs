using System;
using System.Drawing;

class Program
{
    static void Main()
    {
        for (int i = 1; i <= 10; i++)
        {
            string p = string.Format(@"c:\Users\Danh\Desktop\genofficepy\ms365_ref\slide{0}.png", i);
            using (var bmp = new Bitmap(p))
            {
                Console.WriteLine(string.Format("Slide {0}: {1}x{2}", i, bmp.Width, bmp.Height));
            }
        }
    }
}

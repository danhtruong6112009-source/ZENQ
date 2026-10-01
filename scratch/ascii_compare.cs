using System;
using System.Drawing;

class AsciiCompare
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\q_ms.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\q_gen.png"))
        {
            Console.WriteLine("=== MS 365 (q_ms.png) ===");
            PrintAscii(bMs);

            Console.WriteLine("\n=== GENOFFICE (q_gen.png) ===");
            PrintAscii(bGen);
        }
    }

    static void PrintAscii(Bitmap bmp)
    {
        for (int y = 20; y < bmp.Height; y += 20)
        {
            string line = "";
            for (int x = 20; x < bmp.Width; x += 10)
            {
                Color c = bmp.GetPixel(x, y);
                int lum = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                line += (lum > 120) ? "#" : " ";
            }
            Console.WriteLine(string.Format("{0,3}: {1}", y, line));
        }
    }
}

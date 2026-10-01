using System;
using System.Drawing;

class CheckGradientColors
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            // Sample Y = 300 across X = 300, 500, 700, 900, 1100, 1300, 1500
            int y = 300;
            Console.WriteLine(string.Format("{0,5} | {1,-20} | {2,-20}", "X", "MS 365", "GenOffice"));
            Console.WriteLine(new string('-', 50));
            for (int x = 250; x < 1650; x += 100)
            {
                Color c1 = bMs.GetPixel(x, y);
                Color c2 = bGen.GetPixel(x, y);
                Console.WriteLine(string.Format("{0,5} | R={1,3} G={2,3} B={3,3}    | R={4,3} G={5,3} B={6,3}",
                    x, c1.R, c1.G, c1.B, c2.R, c2.G, c2.B));
            }
        }
    }
}

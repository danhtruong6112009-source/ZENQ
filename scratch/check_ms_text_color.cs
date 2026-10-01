using System;
using System.Drawing;

class CheckMsTextColor
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        {
            // Sample pixels on line 1:
            // Left word: "T?n d?ng" around X = 70..150, Y = 200
            // Middle word: "bn ngoi" around X = 400..500, Y = 200
            // Right word: "Ti?p thu" around X = 750..850, Y = 200
            Console.WriteLine("Color of Left word (X=90, Y=200): " + bMs.GetPixel(90, 200));
            Console.WriteLine("Color of Middle word (X=450, Y=200): " + bMs.GetPixel(450, 200));
            for (int x = 750; x < 870; x += 5)
            {
                Color c = bMs.GetPixel(x, 200);
                if (c.R < 100 && c.G < 100) // text pixel
                {
                    Console.WriteLine(string.Format("Color of Right word at X={0}: R={1}, G={2}, B={3} (#{4:X2}{5:X2}{6:X2})",
                        x, c.R, c.G, c.B, c.R, c.G, c.B));
                }
            }
        }
    }
}

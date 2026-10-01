using System;
using System.Drawing;

class TraceQGrad
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        {
            // The bar of Q at Y = 250 spans X = 76 to ~160
            Console.WriteLine("MS 365 Q bar at Y = 250:");
            for (int x = 76; x <= 160; x += 10)
            {
                Color c = bMs.GetPixel(x, 250);
                Console.WriteLine(string.Format("  X={0}: R={1} G={2} B={3}", x, c.R, c.G, c.B));
            }

            // End of TIME at Y = 250 spans X = ~1450 to 1546
            Console.WriteLine("\nMS 365 E bar at Y = 250:");
            for (int x = 1450; x <= 1546; x += 10)
            {
                Color c = bMs.GetPixel(x, 250);
                Console.WriteLine(string.Format("  X={0}: R={1} G={2} B={3}", x, c.R, c.G, c.B));
            }
        }
    }
}

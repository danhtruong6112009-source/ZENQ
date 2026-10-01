using System;
using System.Drawing;

class LocateQ
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            Console.WriteLine("Scanning for Q in MS 365...");
            FindLetter(bMs, "MS 365");

            Console.WriteLine("\nScanning for Q in GenOffice...");
            FindLetter(bGen, "GenOffice");
        }
    }

    static void FindLetter(Bitmap bmp, string label)
    {
        // Scan around X: 50..500, Y: 150..600 for high-luminance pixels
        int minX = int.MaxValue, maxX = 0, minY = int.MaxValue, maxY = 0;
        int ptCount = 0;
        for (int y = 150; y < 600; y++)
        {
            for (int x = 50; x < 500; x++)
            {
                Color c = bmp.GetPixel(x, y);
                // The letter Q starts white/light cyan
                if (c.R > 180 && c.G > 180 && c.B > 220)
                {
                    ptCount++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        Console.WriteLine(string.Format("{0}: Pts={1}, X=[{2}..{3}] (W={4}), Y=[{5}..{6}] (H={7})",
            label, ptCount, minX, maxX, maxX - minX, minY, maxY, maxY - minY));
    }
}

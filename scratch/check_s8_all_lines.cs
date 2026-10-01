using System;
using System.Drawing;
using System.IO;

class CheckS8AllLines
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide8.png"))
        {
            Console.WriteLine("MS365 vertical text bands (Y from 170 to 880, X from 50 to 850):");
            PrintBands(bMs);
            Console.WriteLine("\nGenOffice vertical text bands (Y from 170 to 880, X from 50 to 850):");
            PrintBands(bGen);
        }
    }

    static void PrintBands(Bitmap b)
    {
        int bandStart = -1, bandEnd = -1;
        for (int y = 170; y < 880; y++)
        {
            int darkCount = 0;
            for (int x = 60; x < 850; x += 2)
            {
                Color c = b.GetPixel(x, y);
                // Look for dark text pixels (not background light blue/white)
                if (c.R < 80 && c.G < 80 && c.B < 150)
                {
                    darkCount++;
                }
            }
            if (darkCount > 15)
            {
                if (bandStart == -1) bandStart = y;
                bandEnd = y;
            }
            else
            {
                if (bandStart != -1 && (y - bandEnd) > 4)
                {
                    Console.WriteLine(string.Format("  Band: Y = {0} to {1} (Height = {2})", bandStart, bandEnd, bandEnd - bandStart + 1));
                    bandStart = -1;
                }
            }
        }
        if (bandStart != -1)
        {
            Console.WriteLine(string.Format("  Band: Y = {0} to {1} (Height = {2})", bandStart, bandEnd, bandEnd - bandStart + 1));
        }
    }
}

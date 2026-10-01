using System;
using System.Drawing;

class MeasureExactGlyph
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        {
            // Find top and bottom of letter I in QUIZ (around X = 690)
            int x = 691;
            int minY = int.MaxValue, maxY = 0;
            for (int y = 200; y < 650; y++)
            {
                Color c = bMs.GetPixel(x, y);
                // I is blue/bright
                if (c.R > 50 || c.G > 100 || c.B > 200)
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            Console.WriteLine(string.Format("Letter 'I' in MS 365: Y=[{0}..{1}], CapHeight={2}", minY, maxY, maxY - minY));
        }

        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            int x = 691;
            int minY = int.MaxValue, maxY = 0;
            for (int y = 200; y < 650; y++)
            {
                Color c = bGen.GetPixel(x, y);
                if (c.R > 50 || c.G > 100 || c.B > 200)
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            Console.WriteLine(string.Format("Letter 'I' in GenOffice: Y=[{0}..{1}], CapHeight={2}", minY, maxY, maxY - minY));
        }
    }
}

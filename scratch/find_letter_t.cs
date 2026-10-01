using System;
using System.Drawing;

class FindLetterT
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test2.png"))
        {
            // Scan X from 45 to 100, Y from 160 to 220
            int msMinY = int.MaxValue, msMinX = int.MaxValue;
            int genMinY = int.MaxValue, genMinX = int.MaxValue;

            for (int y = 160; y < 220; y++)
            {
                for (int x = 45; x < 120; x++)
                {
                    Color c1 = bMs.GetPixel(x, y);
                    Color c2 = bGen.GetPixel(x, y);
                    if (c1.R < 80 && c1.G < 80 && c1.B < 150)
                    {
                        if (y < msMinY) { msMinY = y; msMinX = x; }
                    }
                    if (c2.R < 80 && c2.G < 80 && c2.B < 150)
                    {
                        if (y < genMinY) { genMinY = y; genMinX = x; }
                    }
                }
            }
            Console.WriteLine(string.Format("Letter 'T' Top: MS=({0},{1}), Gen=({2},{3}), DeltaY={4}, DeltaX={5}",
                msMinX, msMinY, genMinX, genMinY, msMinY - genMinY, msMinX - genMinX));
        }
    }
}

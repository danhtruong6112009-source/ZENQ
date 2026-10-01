using System;
using System.Drawing;

class MeasureTextPos
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\crop_s9_ms.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\crop_s9_gen.png"))
        {
            // Background is dark blue / purple (~ RGB 20, 20, 40)
            int msMinY = int.MaxValue, msMaxY = 0, msMinX = int.MaxValue, msMaxX = 0;
            int genMinY = int.MaxValue, genMaxY = 0, genMinX = int.MaxValue, genMaxX = 0;

            for (int y = 0; y < bMs.Height; y++)
            {
                for (int x = 0; x < bMs.Width; x++)
                {
                    Color c = bMs.GetPixel(x, y);
                    if (c.R > 100 || c.G > 100 || c.B > 150)
                    {
                        if (y < msMinY) msMinY = y;
                        if (y > msMaxY) msMaxY = y;
                        if (x < msMinX) msMinX = x;
                        if (x > msMaxX) msMaxX = x;
                    }

                    Color c2 = bGen.GetPixel(x, y);
                    if (c2.R > 100 || c2.G > 100 || c2.B > 150)
                    {
                        if (y < genMinY) genMinY = y;
                        if (y > genMaxY) genMaxY = y;
                        if (x < genMinX) genMinX = x;
                        if (x > genMaxX) genMaxX = x;
                    }
                }
            }

            Console.WriteLine(string.Format("MS 365 Text Box: X=[{0}..{1}] (W={2}), Y=[{3}..{4}] (H={5})",
                msMinX, msMaxX, msMaxX - msMinX, msMinY, msMaxY, msMaxY - msMinY));
            Console.WriteLine(string.Format("GenOffice Text Box: X=[{0}..{1}] (W={2}), Y=[{3}..{4}] (H={5})",
                genMinX, genMaxX, genMaxX - genMinX, genMinY, genMaxY, genMaxY - genMinY));
        }
    }
}

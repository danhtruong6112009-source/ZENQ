using System;
using System.Drawing;

class CheckMsLines
{
    static void Main()
    {
        for (int i = 1; i <= 9; i++)
        {
            using (Bitmap b = new Bitmap(string.Format(@"scratch\s8_ms_line{0}.png", i)))
            {
                int minX = int.MaxValue, maxX = int.MinValue;
                for (int y = 0; y < b.Height; y++)
                {
                    for (int x = 0; x < b.Width; x++)
                    {
                        Color c = b.GetPixel(x, y);
                        if (c.R < 80 && c.G < 80 && c.B < 150)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                        }
                    }
                }
                Console.WriteLine(string.Format("MS Line {0}: X = {1} to {2} (Width = {3})", i, minX, maxX, maxX - minX + 1));
            }
        }
    }
}

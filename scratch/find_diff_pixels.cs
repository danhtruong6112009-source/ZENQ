using System;
using System.Drawing;

class FindDiffPixels
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test2.png"))
        {
            int printed = 0;
            for (int y = 0; y < 1080; y += 10)
            {
                for (int x = 900; x < 1920; x += 10)
                {
                    Color cMs = bMs.GetPixel(x, y);
                    Color cGen = bGen.GetPixel(x, y);
                    int dr = Math.Abs(cMs.R - cGen.R);
                    int dg = Math.Abs(cMs.G - cGen.G);
                    int db = Math.Abs(cMs.B - cGen.B);
                    if ((dr > 25 || dg > 25 || db > 25) && printed < 10)
                    {
                        Console.WriteLine(string.Format("Diff at ({0},{1}): MS=({2},{3},{4}) Gen=({5},{6},{7}) Delta=({8},{9},{10})",
                            x, y, cMs.R, cMs.G, cMs.B, cGen.R, cGen.G, cGen.B, dr, dg, db));
                        printed++;
                    }
                }
            }
        }
    }
}

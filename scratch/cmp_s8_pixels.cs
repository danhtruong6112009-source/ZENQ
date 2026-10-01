using System;
using System.Drawing;

class CompareS8Pixels
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\s8_crop_ms.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_crop_gen.png"))
        {
            Console.WriteLine("Comparing S8 Crop (X: 100..500, Y: 450..750):");
            int bgDiffCount = 0;
            int txtDiffCount = 0;
            for (int y = 0; y < bMs.Height; y += 10)
            {
                for (int x = 0; x < bMs.Width; x += 10)
                {
                    Color c1 = bMs.GetPixel(x, y);
                    Color c2 = bGen.GetPixel(x, y);
                    int dr = Math.Abs(c1.R - c2.R);
                    int dg = Math.Abs(c1.G - c2.G);
                    int db = Math.Abs(c1.B - c2.B);
                    if (dr > 30 || dg > 30 || db > 30)
                    {
                        bool isText = (c1.R < 80 && c1.G < 80 && c1.B < 150) || (c2.R < 80 && c2.G < 80 && c2.B < 150);
                        if (isText) txtDiffCount++;
                        else
                        {
                            bgDiffCount++;
                            if (bgDiffCount <= 5)
                            {
                                Console.WriteLine(string.Format("  BG Diff at ({0},{1}): MS=[{2},{3},{4}], GEN=[{5},{6},{7}]",
                                    x + 100, y + 450, c1.R, c1.G, c1.B, c2.R, c2.G, c2.B));
                            }
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("Total Sampled: Text diffs = {0}, Background diffs = {1}", txtDiffCount, bgDiffCount));
        }
    }
}

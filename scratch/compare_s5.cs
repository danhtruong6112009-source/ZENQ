using System;
using System.Drawing;

class CompareS5
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\s5_topleft_ms.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s5_topleft_gen.png"))
        {
            for (int y = 0; y < bMs.Height; y += 40)
            {
                for (int x = 0; x < bMs.Width; x += 40)
                {
                    Color c1 = bMs.GetPixel(x, y);
                    Color c2 = bGen.GetPixel(x, y);
                    if (Math.Abs(c1.R - c2.R) > 40 || Math.Abs(c1.G - c2.G) > 40 || Math.Abs(c1.B - c2.B) > 40)
                    {
                        Console.WriteLine(string.Format("Diff at ({0},{1}): MS=[{2},{3},{4}], GEN=[{5},{6},{7}]",
                            x, y, c1.R, c1.G, c1.B, c2.R, c2.G, c2.B));
                    }
                }
            }
        }
    }
}

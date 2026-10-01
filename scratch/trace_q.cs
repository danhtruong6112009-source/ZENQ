using System;
using System.Drawing;

class TraceQ
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\crop_s9_ms.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\crop_s9_gen.png"))
        {
            // Center of Q is around X = 430 - 200 = 230, Y = 70 to 300
            for (int y = 50; y < 350; y += 15)
            {
                string rowMs = "";
                string rowGen = "";
                for (int x = 200; x < 350; x += 6)
                {
                    Color c1 = bMs.GetPixel(x, y);
                    Color c2 = bGen.GetPixel(x, y);
                    rowMs += (c1.R > 200 && c1.G > 200) ? "#" : ".";
                    rowGen += (c2.R > 200 && c2.G > 200) ? "#" : ".";
                }
                Console.WriteLine(string.Format("Y={0,3}: MS:[{1}]  GEN:[{2}]", y + 200, rowMs, rowGen));
            }
        }
    }
}

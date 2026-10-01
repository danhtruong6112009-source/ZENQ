using System;
using System.Drawing;

class TestLineShift
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test2.png"))
        {
            // Find top of line 1 in MS 365 vs GenOffice
            int msTop = -1, genTop = -1;
            for (int y = 160; y < 220; y++)
            {
                Color cMs = bMs.GetPixel(200, y);
                Color cGen = bGen.GetPixel(200, y);
                if (cMs.R < 80 && msTop == -1) msTop = y;
                if (cGen.R < 80 && genTop == -1) genTop = y;
            }
            Console.WriteLine(string.Format("Line 1 Top: MS={0}, Gen={1}, Shift needed={2}",
                msTop, genTop, msTop - genTop));
        }
    }
}

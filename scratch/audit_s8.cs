using System;
using System.Drawing;
using System.IO;

class AuditSlide8
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide8.png"))
        {
            Console.WriteLine("--- SLIDE 8 DETAILED AUDIT ---");
            for (int by = 0; by < 1080; by += 100)
            {
                for (int bx = 0; bx < 1920; bx += 100)
                {
                    int diffs = 0;
                    int bw = Math.Min(100, 1920 - bx);
                    int bh = Math.Min(100, 1080 - by);
                    for (int y = by; y < by + bh; y += 4)
                    {
                        for (int x = bx; x < bx + bw; x += 4)
                        {
                            Color c1 = bMs.GetPixel(x, y);
                            Color c2 = bGen.GetPixel(x, y);
                            if (Math.Abs(c1.R - c2.R) > 30 || Math.Abs(c1.G - c2.G) > 30 || Math.Abs(c1.B - c2.B) > 30)
                            {
                                diffs++;
                            }
                        }
                    }
                    if (diffs > 50)
                    {
                        Console.WriteLine(string.Format("  Block [{0},{1} - {2},{3}]: {4} diff points",
                            bx, by, bx + bw, by + bh, diffs));
                    }
                }
            }
        }
    }
}

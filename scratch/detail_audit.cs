using System;
using System.Drawing;
using System.IO;

class DetailAudit
{
    static void Main()
    {
        string scratchDir = "scratch";
        int[] slides = new int[] { 1, 2, 3, 4, 5, 8, 9 };

        foreach (int sNum in slides)
        {
            string msPath = Path.Combine(scratchDir, "ms365_slide" + sNum + ".png");
            string genPath = Path.Combine(scratchDir, "genoffice_slide" + sNum + ".png");
            if (!File.Exists(msPath) || !File.Exists(genPath)) continue;

            using (Bitmap bMs = new Bitmap(msPath))
            using (Bitmap bGen = new Bitmap(genPath))
            {
                Console.WriteLine("\n--- SLIDE " + sNum + " ---");
                // Check in 100x100 blocks
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
}

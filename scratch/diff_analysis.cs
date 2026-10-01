using System;
using System.Drawing;
using System.IO;

class DiffAnalysis
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

            using (Bitmap b1 = new Bitmap(msPath))
            using (Bitmap b2 = new Bitmap(genPath))
            {
                int diffCount = 0;
                long totalDiffR = 0, totalDiffG = 0, totalDiffB = 0;
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

                for (int y = 0; y < 1080; y += 2)
                {
                    for (int x = 0; x < 1920; x += 2)
                    {
                        Color c1 = b1.GetPixel(x, y);
                        Color c2 = b2.GetPixel(x, y);

                        int dr = Math.Abs(c1.R - c2.R);
                        int dg = Math.Abs(c1.G - c2.G);
                        int db = Math.Abs(c1.B - c2.B);

                        // Threshold to ignore subpixel antialiasing differences
                        if (dr > 15 || dg > 15 || db > 15)
                        {
                            diffCount++;
                            totalDiffR += dr;
                            totalDiffG += dg;
                            totalDiffB += db;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                double sampledPixels = (1920 / 2) * (1080 / 2);
                double diffPct = (diffCount / sampledPixels) * 100.0;
                Console.WriteLine(string.Format("Slide {0}: Diff={1:F2}%, DiffCount={2}, Region=[({3},{4}) to ({5},{6})]",
                    sNum, diffPct, diffCount, minX == int.MaxValue ? 0 : minX, minY == int.MaxValue ? 0 : minY,
                    maxX == int.MinValue ? 0 : maxX, maxY == int.MinValue ? 0 : maxY));
            }
        }
    }
}

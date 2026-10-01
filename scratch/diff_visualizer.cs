using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class DiffVisualizer
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
            using (Bitmap bDiff = new Bitmap(1920, 1080))
            {
                for (int y = 0; y < 1080; y++)
                {
                    for (int x = 0; x < 1920; x++)
                    {
                        Color c1 = bMs.GetPixel(x, y);
                        Color c2 = bGen.GetPixel(x, y);

                        int dr = Math.Abs(c1.R - c2.R);
                        int dg = Math.Abs(c1.G - c2.G);
                        int db = Math.Abs(c1.B - c2.B);

                        if (dr > 25 || dg > 25 || db > 25)
                        {
                            // Significant difference: bright magenta/red
                            bDiff.SetPixel(x, y, Color.FromArgb(255, 255, 0, 128));
                        }
                        else
                        {
                            // Subtle or match: dark grayscale
                            int gray = (c1.R + c1.G + c1.B) / 6;
                            bDiff.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                        }
                    }
                }

                // Create a 3-way side-by-side: MS365 | GenOffice | Diff Map
                int w = 1280;
                int h = 720;
                using (Bitmap triple = new Bitmap(w * 3, h + 40))
                using (Graphics g = Graphics.FromImage(triple))
                {
                    g.Clear(Color.Black);
                    using (Font font = new Font("Segoe UI", 16f, FontStyle.Bold))
                    {
                        g.DrawString("MS 365 (Ground Truth)", font, Brushes.White, 20, 10);
                        g.DrawString("GenOffice", font, Brushes.LightGreen, w + 20, 10);
                        g.DrawString("Diff Map (Neon = Discrepancy)", font, Brushes.OrangeRed, w * 2 + 20, 10);
                    }

                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bMs, new Rectangle(0, 40, w, h));
                    g.DrawImage(bGen, new Rectangle(w, 40, w, h));
                    g.DrawImage(bDiff, new Rectangle(w * 2, 40, w, h));

                    string outTriple = Path.Combine(scratchDir, "audit_slide" + sNum + "_triple.png");
                    triple.Save(outTriple, ImageFormat.Png);
                    Console.WriteLine("Saved: " + outTriple);
                }
            }
        }
    }
}

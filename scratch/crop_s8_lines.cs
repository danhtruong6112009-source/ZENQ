using System;
using System.Drawing;
using System.Drawing.Imaging;

class CropS8Lines
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        {
            // Bands:
            // 1: 178-226
            // 2: 243-292
            // 3: 311-353
            // 4: 407-449
            // 5: 474-516
            // 6: 550-583
            // 7: 637-684
            // 8: 702-750
            // 9: 773-812
            int[][] bands = new int[][] {
                new int[] { 178, 226 },
                new int[] { 243, 292 },
                new int[] { 311, 353 },
                new int[] { 407, 449 },
                new int[] { 474, 516 },
                new int[] { 550, 583 },
                new int[] { 637, 684 },
                new int[] { 702, 750 },
                new int[] { 773, 812 }
            };

            for (int i = 0; i < bands.Length; i++)
            {
                int y = bands[i][0];
                int h = bands[i][1] - y + 1;
                using (Bitmap crop = new Bitmap(800, h))
                using (Graphics g = Graphics.FromImage(crop))
                {
                    g.DrawImage(bMs, new Rectangle(0, 0, 800, h), new Rectangle(50, y, 800, h), GraphicsUnit.Pixel);
                    crop.Save(string.Format(@"scratch\s8_ms_line{0}.png", i + 1), ImageFormat.Png);
                }
            }
            Console.WriteLine("Cropped 9 lines from MS365 Slide 8 to scratch/s8_ms_line*.png");
        }
    }
}

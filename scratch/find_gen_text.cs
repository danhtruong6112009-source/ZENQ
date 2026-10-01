using System;
using System.Drawing;

class FindGenText
{
    static void Main()
    {
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            int minX = int.MaxValue, maxX = 0, minY = int.MaxValue, maxY = 0;
            int count = 0;
            for (int y = 0; y < 1080; y++)
            {
                for (int x = 0; x < 1920; x++)
                {
                    Color c = bGen.GetPixel(x, y);
                    if (c.R > 220 && c.G > 220 && c.B > 220)
                    {
                        // Exclude START button text at Y ~ 650
                        if (y < 600)
                        {
                            count++;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("GenOffice Bright Text (Y < 600): Count={0}, X=[{1}..{2}], Y=[{3}..{4}]",
                count, minX, maxX, minY, maxY));
        }

        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        {
            int minX = int.MaxValue, maxX = 0, minY = int.MaxValue, maxY = 0;
            int count = 0;
            for (int y = 0; y < 1080; y++)
            {
                for (int x = 0; x < 1920; x++)
                {
                    Color c = bMs.GetPixel(x, y);
                    if (c.R > 220 && c.G > 220 && c.B > 220)
                    {
                        if (y < 600)
                        {
                            count++;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("MS 365 Bright Text (Y < 600): Count={0}, X=[{1}..{2}], Y=[{3}..{4}]",
                count, minX, maxX, minY, maxY));
        }
    }
}

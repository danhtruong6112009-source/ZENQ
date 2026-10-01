using System;
using System.Drawing;

class TraceS8Lines
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide8.png"))
        {
            Console.WriteLine("Tracing vertical line baselines on Slide 8 (at X = 200):");
            Console.WriteLine(string.Format("{0,4} | {1,-20} | {2,-20}", "Y", "MS 365", "GenOffice"));
            for (int y = 170; y < 900; y += 10)
            {
                Color c1 = bMs.GetPixel(200, y);
                Color c2 = bGen.GetPixel(200, y);
                bool isTxt1 = (c1.R < 80 && c1.G < 80 && c1.B < 150);
                bool isTxt2 = (c2.R < 80 && c2.G < 80 && c2.B < 150);
                if (isTxt1 || isTxt2)
                {
                    Console.WriteLine(string.Format("{0,4} | {1,-20} | {2,-20}",
                        y, isTxt1 ? "TEXT" : "     ", isTxt2 ? "TEXT" : "     "));
                }
            }
        }
    }
}

using System;
using System.Drawing;

namespace InspectDiff
{
    class Program
    {
        static void Main()
        {
            Bitmap b1 = new Bitmap(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\switch_1_writer.png");
            Bitmap b2 = new Bitmap(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\switch_2_calc.png");

            Console.WriteLine("Image 1 size: " + b1.Size);
            Console.WriteLine("Image 2 size: " + b2.Size);

            // Check where the island card border is located in both images:
            // Island card top border is around Y=39
            for (int y = 35; y <= 45; y++)
            {
                Color c1 = b1.GetPixel(600, y);
                Color c2 = b2.GetPixel(600, y);
                Console.WriteLine(string.Format("Y={0}: W=({1},{2},{3}) C=({4},{5},{6})", y, c1.R, c1.G, c1.B, c2.R, c2.G, c2.B));
            }

            // Check bottom card border around Y=145..155
            for (int y = 145; y <= 155; y++)
            {
                Color c1 = b1.GetPixel(600, y);
                Color c2 = b2.GetPixel(600, y);
                Console.WriteLine(string.Format("Bottom Y={0}: W=({1},{2},{3}) C=({4},{5},{6})", y, c1.R, c1.G, c1.B, c2.R, c2.G, c2.B));
            }

            // Check divider between tabs and content around Y=64..68
            for (int y = 63; y <= 69; y++)
            {
                Color c1 = b1.GetPixel(600, y);
                Color c2 = b2.GetPixel(600, y);
                Console.WriteLine(string.Format("Divider Y={0}: W=({1},{2},{3}) C=({4},{5},{6})", y, c1.R, c1.G, c1.B, c2.R, c2.G, c2.B));
            }
        }
    }
}

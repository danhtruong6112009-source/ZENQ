using System;
using System.Drawing;

class TraceGenText
{
    static void Main()
    {
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            // Sample along Y = 350
            int y = 350;
            Console.WriteLine("Scanning X at Y = 350 in GenOffice for text:");
            int segStart = -1;
            Color segColor = Color.Empty;
            for (int x = 0; x < 1920; x++)
            {
                Color c = bGen.GetPixel(x, y);
                bool isText = (c.R > 50 || c.G > 80 || c.B > 200) && !(c.R < 10 && c.G < 40 && c.B < 150);
                if (isText)
                {
                    if (segStart == -1) { segStart = x; segColor = c; }
                }
                else
                {
                    if (segStart != -1)
                    {
                        Console.WriteLine(string.Format("  Text Segment: X=[{0}..{1}] (W={2}), StartColor=R={3},G={4},B={5}",
                            segStart, x - 1, x - segStart, segColor.R, segColor.G, segColor.B));
                        segStart = -1;
                    }
                }
            }
        }
    }
}

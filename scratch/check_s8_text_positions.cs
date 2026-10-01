using System;
using System.Drawing;
using System.IO;

class CheckS8TextPositions
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide8.png"))
        {
            // Sample X = 200, across Y from 170 to 880
            Console.WriteLine("Y range where dark text is detected (X=200):");
            int msStart = -1, msEnd = -1;
            int genStart = -1, genEnd = -1;

            Console.WriteLine("MS365 text lines at X=200:");
            for (int y = 170; y < 880; y++)
            {
                Color c = bMs.GetPixel(200, y);
                bool isDark = (c.R < 60 && c.G < 60 && c.B < 120);
                if (isDark)
                {
                    if (msStart == -1) msStart = y;
                    msEnd = y;
                }
                else
                {
                    if (msStart != -1 && (y - msEnd) > 5)
                    {
                        Console.WriteLine(string.Format("  Line: Y = {0} to {1} (Height = {2})", msStart, msEnd, msEnd - msStart + 1));
                        msStart = -1;
                    }
                }
            }

            Console.WriteLine("\nGenOffice text lines at X=200:");
            for (int y = 170; y < 880; y++)
            {
                Color c = bGen.GetPixel(200, y);
                bool isDark = (c.R < 60 && c.G < 60 && c.B < 120);
                if (isDark)
                {
                    if (genStart == -1) genStart = y;
                    genEnd = y;
                }
                else
                {
                    if (genStart != -1 && (y - genEnd) > 5)
                    {
                        Console.WriteLine(string.Format("  Line: Y = {0} to {1} (Height = {2})", genStart, genEnd, genEnd - genStart + 1));
                        genStart = -1;
                    }
                }
            }
        }
    }
}

using System;
using System.Drawing;

class AuditDiffSources
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test2.png"))
        {
            int txtDiffs = 0;
            int bgDiffs = 0;
            int imgDiffs = 0;
            int badgeDiffs = 0;

            for (int y = 0; y < 1080; y += 2)
            {
                for (int x = 0; x < 1920; x += 2)
                {
                    Color c1 = bMs.GetPixel(x, y);
                    Color c2 = bGen.GetPixel(x, y);
                    if (Math.Abs(c1.R - c2.R) > 15 || Math.Abs(c1.G - c2.G) > 15 || Math.Abs(c1.B - c2.B) > 15)
                    {
                        if (x < 900 && y >= 160 && y <= 850) txtDiffs++;
                        else if (y < 160 && x < 900) badgeDiffs++;
                        else if (x >= 900) imgDiffs++;
                        else bgDiffs++;
                    }
                }
            }
            Console.WriteLine(string.Format("Diff Breakdown: Text={0}, HeaderBadge={1}, RightImage={2}, Background={3}",
                txtDiffs, badgeDiffs, imgDiffs, bgDiffs));
        }
    }
}

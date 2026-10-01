using System;
using System.Drawing;

class CheckPatchDiff
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\s8_fixed_test2.png"))
        {
            Console.WriteLine("Sampling at X=1600, Y=800:");
            Color cMs = bMs.GetPixel(1600, 800);
            Color cGen = bGen.GetPixel(1600, 800);
            Console.WriteLine(string.Format("MS: R={0}, G={1}, B={2}", cMs.R, cMs.G, cMs.B));
            Console.WriteLine(string.Format("Gen: R={0}, G={1}, B={2}", cGen.R, cGen.G, cGen.B));

            // Check diff threshold:
            // Notice: diff threshold in TestS8StandaloneFix2 was:
            // Math.Abs(c1.R - c2.R) > 15 || Math.Abs(c1.G - c2.G) > 15 || Math.Abs(c1.B - c2.B) > 15
            int dr = Math.Abs(cMs.R - cGen.R);
            int dg = Math.Abs(cMs.G - cGen.G);
            int db = Math.Abs(cMs.B - cGen.B);
            Console.WriteLine(string.Format("Delta: dR={0}, dG={1}, dB={2}", dr, dg, db));
        }
    }
}

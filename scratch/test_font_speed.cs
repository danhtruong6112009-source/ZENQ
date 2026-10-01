using System;
using System.Diagnostics;
using System.Drawing;

class TestFont
{
    static void Main()
    {
        Bitmap bmp = new Bitmap(100, 100);
        Graphics g = Graphics.FromImage(bmp);
        Font tnr = new Font("Times New Roman", 12.0f);

        string[] tests = new string[] {
            "ASCII: The quick brown fox jumps over the lazy dog",
            "Vietnamese: C\u1ed9ng h\u00f2a x\u00e3 h\u1ed9i ch\u1ee7 ngh\u0129a Vi\u1ec7t Nam \u0111\u1ed9c l\u1eadp t\u1ef1 do",
            "Superscripts: x\u2070 + x\u00b9 + x\u00b2 + x\u00b3 + x\u2074 + x\u2075 + x\u2076 + x\u2077 + x\u2078 + x\u2079",
            "Math ops: \u222b \u2211 \u221a \u2260 \u2264 \u2265 \u2248 \u221e \u00b1 \u00d7 \u00f7",
            "Greek: \u03b1 \u03b2 \u03b3 \u03b4 \u03b5 \u03b6 \u03b7 \u03b8 \u03b9 \u03ba \u03bb \u03bc \u03bd \u03be \u03bf \u03c0",
            "Complex formula: f(x) = \u222b (x\u00b2 + y\u00b2) / \u221a(1 - x\u00b2) dx \u2264 \u222d \u222f"
        };

        foreach (var t in tests)
        {
            // MeasureString
            Stopwatch swM = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
            {
                SizeF sz = g.MeasureString(t, tnr);
            }
            swM.Stop();

            // DrawString
            Stopwatch swD = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
            {
                g.DrawString(t, tnr, Brushes.Black, 0, 0);
            }
            swD.Stop();

            Console.WriteLine(t.Substring(0, Math.Min(25, t.Length)) + "...");
            Console.WriteLine("   MeasureString (500x): " + swM.ElapsedMilliseconds + " ms (" + (swM.ElapsedMilliseconds / 500.0) + " ms/call)");
            Console.WriteLine("   DrawString    (500x): " + swD.ElapsedMilliseconds + " ms (" + (swD.ElapsedMilliseconds / 500.0) + " ms/call)");
        }

        g.Dispose();
        bmp.Dispose();
    }
}

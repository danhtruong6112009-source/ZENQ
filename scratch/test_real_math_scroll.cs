using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("GenOffice.exe");
            Assembly asm = Assembly.Load(bytes);
            Type writerType = asm.GetType("GenOffice.UI.WriterView");
            Type ribbonType = asm.GetType("GenOffice.UI.RibbonBar");

            dynamic w = Activator.CreateInstance(writerType);
            dynamic ribbon = Activator.CreateInstance(ribbonType);

            string docPath = "scratch\\test_math_doc.docx";
            if (args.Length > 0) docPath = args[0];

            Console.WriteLine("Loading doc: " + docPath);
            w.LoadDocumentFile(docPath, ribbon);

            Bitmap bmp = new Bitmap(1000, 800);
            Graphics g = Graphics.FromImage(bmp);

            // Initial layout
            w.Draw(g, 1000, 800, ribbon);

            int maxScroll = w.GetMaxScrollY(800, ribbon);
            Console.WriteLine("TotalPageCount = " + w.TotalPageCount + ", MaxScrollY = " + maxScroll);

            int steps = 60;
            int stepSize = maxScroll / steps;

            // Pass 1: Scroll Down
            int lagCount1 = 0;
            Stopwatch swPass1 = Stopwatch.StartNew();
            for (int s = 0; s < steps; s++)
            {
                w.ScrollY = s * stepSize;
                Stopwatch sw = Stopwatch.StartNew();
                w.Draw(g, 1000, 800, ribbon);
                sw.Stop();
                long ms = sw.ElapsedMilliseconds;
                if (ms > 16)
                {
                    lagCount1++;
                    Console.WriteLine("Pass 1 Step " + s + " (ScrollY=" + w.ScrollY + "): " + ms + " ms (LAG!)");
                }
            }
            swPass1.Stop();
            Console.WriteLine("Pass 1 (Scroll Down) completed in " + swPass1.ElapsedMilliseconds + " ms. Lag frames: " + lagCount1 + "/" + steps);

            // Pass 2: Scroll Up
            int lagCount2 = 0;
            Stopwatch swPass2 = Stopwatch.StartNew();
            for (int s = steps - 1; s >= 0; s--)
            {
                w.ScrollY = s * stepSize;
                Stopwatch sw = Stopwatch.StartNew();
                w.Draw(g, 1000, 800, ribbon);
                sw.Stop();
                long ms = sw.ElapsedMilliseconds;
                if (ms > 16)
                {
                    lagCount2++;
                    Console.WriteLine("Pass 2 Step " + s + " (ScrollY=" + w.ScrollY + "): " + ms + " ms (LAG!)");
                }
            }
            swPass2.Stop();
            Console.WriteLine("Pass 2 (Scroll Up) completed in " + swPass2.ElapsedMilliseconds + " ms. Lag frames: " + lagCount2 + "/" + steps);

            g.Dispose();
            bmp.Dispose();
        }
    }
}

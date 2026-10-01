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

            w.LoadDocumentFile("scratch\\test_math_doc.docx", ribbon);

            Bitmap bmp = new Bitmap(1000, 800);
            Graphics g = Graphics.FromImage(bmp);
            w.Draw(g, 1000, 800, ribbon);

            w.ScrollY = 10440; // Step 40

            // Detailed breakdown of Draw
            Stopwatch sw = Stopwatch.StartNew();
            w.Draw(g, 1000, 800, ribbon);
            sw.Stop();
            Console.WriteLine("Draw 1 total: " + sw.ElapsedMilliseconds + " ms");

            sw.Restart();
            w.Draw(g, 1000, 800, ribbon);
            sw.Stop();
            Console.WriteLine("Draw 2 total: " + sw.ElapsedMilliseconds + " ms");

            // Check _wordMeasureCache count
            var wordCache = (System.Collections.IDictionary)writerType.GetField("_wordMeasureCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(w);
            Console.WriteLine("_wordMeasureCache count: " + wordCache.Count);

            g.Dispose();
            bmp.Dispose();
        }
    }
}

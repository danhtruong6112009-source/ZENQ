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

            w.ScrollY = 14355; // Step 55

            // Test first draw vs subsequent draws
            Stopwatch sw1 = Stopwatch.StartNew();
            w.Draw(g, 1000, 800, ribbon);
            sw1.Stop();

            Stopwatch sw2 = Stopwatch.StartNew();
            w.Draw(g, 1000, 800, ribbon);
            sw2.Stop();

            Stopwatch sw3 = Stopwatch.StartNew();
            w.Draw(g, 1000, 800, ribbon);
            sw3.Stop();

            Console.WriteLine("Step 55 draw 1: " + sw1.ElapsedMilliseconds + " ms");
            Console.WriteLine("Step 55 draw 2: " + sw2.ElapsedMilliseconds + " ms");
            Console.WriteLine("Step 55 draw 3: " + sw3.ElapsedMilliseconds + " ms");

            g.Dispose();
            bmp.Dispose();
        }
    }
}

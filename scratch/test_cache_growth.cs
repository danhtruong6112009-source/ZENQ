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

            var wordCache = (System.Collections.IDictionary)writerType.GetField("_wordMeasureCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(w);
            Console.WriteLine("Cache count after initial draw: " + wordCache.Count);

            w.ScrollY = 10440; // Step 40
            w.Draw(g, 1000, 800, ribbon);
            Console.WriteLine("Cache count after Step 40 draw 1: " + wordCache.Count);

            g.Dispose();
            bmp.Dispose();
        }
    }
}

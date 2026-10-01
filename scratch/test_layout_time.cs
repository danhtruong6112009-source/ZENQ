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
            Type shapeRendererType = asm.GetType("GenOffice.UI.ShapeRenderer");

            dynamic w = Activator.CreateInstance(writerType);
            dynamic ribbon = Activator.CreateInstance(ribbonType);

            w.LoadDocumentFile("scratch\\test_math_doc.docx", ribbon);

            Bitmap bmp = new Bitmap(1000, 800);
            Graphics g = Graphics.FromImage(bmp);
            w.Draw(g, 1000, 800, ribbon);

            w.ScrollY = 10440; // Step 40

            // Measure RecomputeLayout if called:
            Stopwatch swLayout = Stopwatch.StartNew();
            w.RecomputeLayout(g, 1000, ribbon);
            swLayout.Stop();
            Console.WriteLine("Explicit RecomputeLayout at ScrollY=10440: " + swLayout.ElapsedMilliseconds + " ms");

            // Measure Draw
            Stopwatch swDraw = Stopwatch.StartNew();
            w.Draw(g, 1000, 800, ribbon);
            swDraw.Stop();
            Console.WriteLine("Draw after explicit layout: " + swDraw.ElapsedMilliseconds + " ms");

            g.Dispose();
            bmp.Dispose();
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using GenOffice.Core;
using GenOffice.UI;

namespace Diag
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            WriterView wv = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            wv.LoadDocumentFile("Full_Features_Document.docx", ribbon);

            Bitmap bmp = new Bitmap(1366, 768);
            Graphics g = Graphics.FromImage(bmp);

            // Prime
            wv.Draw(g, 1366, 768, ribbon);

            // Now let's see what happens during Draw!
            // Let's time 10 iterations of Draw and check what's going on
            Stopwatch sw = new Stopwatch();

            // Check if ShapeRenderer.DrawShape is taking time
            sw.Restart();
            foreach (var sh in wv.DocumentShapes)
            {
                // do nothing
            }
            sw.Stop();

            Console.WriteLine("Testing Draw with Stopwatches inside...");
        }
    }
}

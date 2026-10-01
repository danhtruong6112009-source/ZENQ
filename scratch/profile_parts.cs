using System;
using System.Diagnostics;
using System.Drawing;
using GenOffice.Core;
using GenOffice.UI;

namespace ProfileDoc
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

            // First draw to populate caches
            wv.Draw(g, 1366, 768, ribbon);

            Console.WriteLine("\n--- Testing ScrollY = 450 ---");
            wv.ScrollY = 450;

            Stopwatch sw = Stopwatch.StartNew();
            wv.Draw(g, 1366, 768, ribbon);
            sw.Stop();
            Console.WriteLine("Draw at ScrollY = 450 took: " + sw.ElapsedMilliseconds + " ms");
        }
    }
}

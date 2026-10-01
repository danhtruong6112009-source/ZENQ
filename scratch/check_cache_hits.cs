using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using GenOffice.Core;
using GenOffice.UI;

namespace CheckCacheHits
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

            // Draw 1
            Console.WriteLine("Draw 1 (initial):");
            wv.Draw(g, 1366, 768, ribbon);

            // Draw 2 (same position):
            Console.WriteLine("\nDraw 2 (same position):");
            wv.Draw(g, 1366, 768, ribbon);

            // Draw 3 (after scroll):
            wv.ScrollY = 300;
            Console.WriteLine("\nDraw 3 (after ScrollY = 300):");
            wv.Draw(g, 1366, 768, ribbon);
        }
    }
}

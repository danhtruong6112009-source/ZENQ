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
            Console.WriteLine("Loading Full_Features_Document.docx...");
            WriterView wv = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            Stopwatch sw = Stopwatch.StartNew();
            bool ok = wv.LoadDocumentFile("Full_Features_Document.docx", ribbon);
            sw.Stop();
            Console.WriteLine("Loaded in: " + sw.ElapsedMilliseconds + " ms | Success: " + ok);
            Console.WriteLine("Total text length: " + wv.PieceTable.Length);
            Console.WriteLine("DocumentShapes count: " + wv.DocumentShapes.Count);
            Console.WriteLine("DocumentCharts count: " + wv.DocumentCharts.Count);
            Console.WriteLine("DocumentTables count: " + wv.DocumentTables.Count);

            Bitmap bmp = new Bitmap(1366, 768);
            Graphics g = Graphics.FromImage(bmp);

            Console.WriteLine("\n--- Profiling First Draw ---");
            sw.Restart();
            wv.Draw(g, 1366, 768, ribbon);
            sw.Stop();
            Console.WriteLine("First Draw took: " + sw.ElapsedMilliseconds + " ms | Total Pages: " + wv.TotalPageCount);

            Console.WriteLine("\n--- Profiling Subsequent 10 Draws (same scroll) ---");
            for (int i = 1; i <= 10; i++)
            {
                sw.Restart();
                wv.Draw(g, 1366, 768, ribbon);
                sw.Stop();
                Console.WriteLine("Draw #" + i + ": " + sw.ElapsedMilliseconds + " ms");
            }

            Console.WriteLine("\n--- Profiling 30 Draws while Scrolling (Continuous Scroll) ---");
            long totalMs = 0;
            long maxMs = 0;
            long minMs = 99999;
            for (int i = 1; i <= 30; i++)
            {
                wv.ScrollY = i * 120;
                sw.Restart();
                wv.Draw(g, 1366, 768, ribbon);
                sw.Stop();
                long ms = sw.ElapsedMilliseconds;
                totalMs += ms;
                if (ms > maxMs) maxMs = ms;
                if (ms < minMs) minMs = ms;
                Console.WriteLine("Scroll Frame #" + i + ": " + ms + " ms");
            }
            Console.WriteLine("Avg: " + (totalMs / 30.0).ToString("F1") + " ms | Min: " + minMs + " ms | Max: " + maxMs + " ms");
        }
    }
}

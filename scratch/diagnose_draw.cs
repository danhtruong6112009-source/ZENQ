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

            // Let's inspect fields
            FieldInfo fiParas = typeof(WriterView).GetField("_cachedParagraphs", BindingFlags.NonPublic | BindingFlags.Instance);
            string[] paras = (string[])fiParas.GetValue(wv);
            Console.WriteLine("Paragraph count: " + paras.Length);

            FieldInfo fiCache = typeof(WriterView).GetField("_paraLayoutCache", BindingFlags.NonPublic | BindingFlags.Instance);
            var cache = fiCache.GetValue(wv) as System.Collections.IDictionary;
            Console.WriteLine("Cache count: " + cache.Count);

            FieldInfo fiLines = typeof(WriterView).GetField("RenderedLines", BindingFlags.Public | BindingFlags.Instance);
            var renderedLines = fiLines.GetValue(wv) as System.Collections.IList;
            Console.WriteLine("RenderedLines count: " + renderedLines.Count);

            FieldInfo fiGaps = typeof(WriterView).GetField("DocParagraphGaps", BindingFlags.Public | BindingFlags.Instance);
            var gaps = fiGaps.GetValue(wv) as System.Collections.IList;
            Console.WriteLine("DocParagraphGaps count: " + gaps.Count);

            // Now let's see how long a single pass over paragraphs takes
            Stopwatch sw = Stopwatch.StartNew();
            for (int p = 0; p < paras.Length; p++)
            {
                string s = paras[p];
            }
            sw.Stop();
            Console.WriteLine("Looping over array: " + sw.ElapsedTicks + " ticks (" + (sw.ElapsedMilliseconds) + " ms)");
        }
    }
}

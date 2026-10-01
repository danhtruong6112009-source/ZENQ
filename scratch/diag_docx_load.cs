using System;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

namespace TestDiag
{
    class Program
    {
        static void Main()
        {
            string docxPath = @"C:\Users\Danh\Downloads\Bài 1.ND GHI.docx";
            DocxMetadata meta;
            bool ok = DocxHandler.LoadDocxFile(docxPath, out meta);
            Console.WriteLine("LoadDocxFile: " + ok);
            Console.WriteLine("Text length: " + meta.TextContent.Length);
            Console.WriteLine("Runs count: " + meta.FormattedRuns.Count);
            
            Console.WriteLine("First 300 chars of text:");
            Console.WriteLine(meta.TextContent.Substring(0, Math.Min(300, meta.TextContent.Length)));
            
            Console.WriteLine("\nFirst 10 runs:");
            for (int i = 0; i < Math.Min(10, meta.FormattedRuns.Count); i++)
            {
                var r = meta.FormattedRuns[i];
                string sub = (r.StartIndex + r.Length <= meta.TextContent.Length) ? meta.TextContent.Substring(r.StartIndex, r.Length) : "?";
                Console.WriteLine(string.Format("Run {0}: Start={1}, Len={2}, Font='{3}', Size={4}, Bold={5}, Text='{6}'", 
                    i, r.StartIndex, r.Length, r.FontName, r.FontSize, r.Bold, sub.Replace("\r", "\\r").Replace("\n", "\\n")));
            }

            Console.WriteLine("\nParagraph Alignments:");
            foreach (var kvp in meta.ParagraphAlignments)
            {
                if (kvp.Key < 400)
                {
                    Console.WriteLine(string.Format("Pos {0}: Align={1}", kvp.Key, kvp.Value));
                }
            }

            Console.WriteLine("\nParagraph Line Spacings:");
            foreach (var kvp in meta.ParagraphLineSpacings)
            {
                if (kvp.Key < 400)
                {
                    Console.WriteLine(string.Format("Pos {0}: LineSpacing={1}", kvp.Key, kvp.Value));
                }
            }
        }
    }
}

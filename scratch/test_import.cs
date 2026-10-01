using System;
using System.IO;
using GenOffice.Format;

namespace TestImport
{
    class Program
    {
        static void Main(string[] args)
        {
            string path1 = @"C:\Users\Danh\Desktop\quy\Bang_Thu_Chi_Quy_Lop_1Trang.docx";
            if (File.Exists(path1))
            {
                Console.WriteLine("=== Testing " + path1 + " ===");
                DocxMetadata meta;
                bool ok = DocxHandler.LoadDocxFile(path1, out meta);
                Console.WriteLine("Import result: " + ok);
                Console.WriteLine("Has DIVIDER: " + meta.TextContent.Contains("![DIVIDER:"));
                Console.WriteLine("Tables count: " + meta.Tables.Count);
                if (meta.Tables.Count > 0)
                {
                    Console.WriteLine("Table 0 rows: " + meta.Tables[0].Rows.Count + ", cols: " + (meta.Tables[0].Rows.Count > 0 ? meta.Tables[0].Rows[0].Cells.Count : 0));
                }
                
                int divIdx = meta.TextContent.IndexOf("![DIVIDER:");
                if (divIdx >= 0)
                {
                    int endLine = meta.TextContent.IndexOf('\n', divIdx);
                    Console.WriteLine("Found divider line: " + meta.TextContent.Substring(divIdx, (endLine > divIdx ? endLine - divIdx : 30)));
                }
                else
                {
                    Console.WriteLine("First 300 chars: " + meta.TextContent.Substring(0, Math.Min(300, meta.TextContent.Length)));
                }
            }

            string path2 = @"C:\Users\Danh\Downloads\Bài 1 - Liên hợp quốc.docx";
            if (File.Exists(path2))
            {
                Console.WriteLine("=== Testing " + path2 + " ===");
                DocxMetadata meta2;
                bool ok2 = DocxHandler.LoadDocxFile(path2, out meta2);
                Console.WriteLine("Import result: " + ok2);
                Console.WriteLine("Has SHAPE: " + meta2.TextContent.Contains("![SHAPE:"));
                Console.WriteLine("Has IMAGE: " + meta2.TextContent.Contains("![IMAGE:"));
                int shpIdx = meta2.TextContent.IndexOf("![SHAPE:");
                if (shpIdx >= 0)
                {
                    int endLine = meta2.TextContent.IndexOf('\n', shpIdx);
                    Console.WriteLine("First shape tag: " + meta2.TextContent.Substring(shpIdx, Math.Min(120, meta2.TextContent.Length - shpIdx)));
                }
            }
        }
    }
}

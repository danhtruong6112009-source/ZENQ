using System;
using System.IO;
using System.IO.Compression;

namespace InspectPptx
{
    class Program
    {
        static void Main()
        {
            try
            {
                string refPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
                string ourPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\BaoCaoThuyetTrinh_2026.pptx";

                Console.WriteLine("=== REF ENTRIES ===");
                using (ZipArchive zip = ZipFile.OpenRead(refPptx))
                {
                    foreach (var e in zip.Entries)
                    {
                        Console.WriteLine("REF: " + e.FullName);
                    }
                }

                Console.WriteLine("\n=== OUR ENTRIES ===");
                using (ZipArchive zip = ZipFile.OpenRead(ourPptx))
                {
                    foreach (var e in zip.Entries)
                    {
                        Console.WriteLine("OUR: " + e.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}

using System;
using System.IO.Compression;

class CheckEmbeddedFonts
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.IndexOf("font", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("Font entry: " + entry.FullName + " (" + entry.Length + " bytes)");
                }
            }
        }
    }
}

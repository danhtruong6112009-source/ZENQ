using System;
using System.IO;
using System.IO.Compression;
using GenOffice.Format;

class Program
{
    static void Main()
    {
        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            SlideFontManager.ExtractAndRegisterFonts(zip);
            Console.WriteLine("Available fonts in PrivateFontCollection:");
            foreach (var fam in SlideFontManager.PrivateFonts.Families)
            {
                Console.WriteLine("  " + fam.Name);
            }
        }
    }
}

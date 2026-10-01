using System;
using System.IO;
using System.IO.Compression;
using GenOffice.Core;

class Program
{
    static void Main()
    {
        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            SlideFontManager.ExtractAndRegisterFonts(zip);
            Console.WriteLine("Font HasFont(Noto Serif Display Bold): " + SlideFontManager.HasFont("Noto Serif Display Bold"));
            Console.WriteLine("Font HasFont(Noto Serif Display): " + SlideFontManager.HasFont("Noto Serif Display"));
            using (var f1 = SlideFontManager.CreateFont("Noto Serif Display Bold", 24f, System.Drawing.FontStyle.Bold))
            {
                Console.WriteLine("Created font for 'Noto Serif Display Bold': " + f1.Name + ", style=" + f1.Style);
            }
            using (var f2 = SlideFontManager.CreateFont("Noto Serif Display", 24f, System.Drawing.FontStyle.Bold))
            {
                Console.WriteLine("Created font for 'Noto Serif Display': " + f2.Name + ", style=" + f2.Style);
            }
        }
    }
}

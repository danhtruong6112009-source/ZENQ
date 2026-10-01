using System;
using System.Drawing;
using System.IO.Compression;
using GenOffice.Core;

class TestFontManager
{
    static void Main()
    {
        string pptx = @"C:\Users\Danh\Desktop\bài 2.pptx";
        using (ZipArchive zip = ZipFile.OpenRead(pptx))
        {
            SlideFontManager.ExtractAndRegisterFonts(zip);
        }

        using (Font f = SlideFontManager.CreateFont("Anton", 40f, FontStyle.Regular))
        {
            Console.WriteLine("Font for 'Anton': Family=" + f.FontFamily.Name + ", Size=" + f.Size);
        }
        using (Font f = SlideFontManager.CreateFont("Muli Heavy", 40f, FontStyle.Regular))
        {
            Console.WriteLine("Font for 'Muli Heavy': Family=" + f.FontFamily.Name + ", Size=" + f.Size);
        }
        using (Font f = SlideFontManager.CreateFont("Proxima Nova Bold", 40f, FontStyle.Regular))
        {
            Console.WriteLine("Font for 'Proxima Nova Bold': Family=" + f.FontFamily.Name + ", Size=" + f.Size);
        }
    }
}

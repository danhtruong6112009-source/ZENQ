using System;
using System.Drawing;
using System.IO.Compression;
using GenOffice.Core;

class Program
{
    static void Main()
    {
        using (var zip = ZipFile.OpenRead(@"c:\Users\Danh\Desktop\bài 2.pptx"))
        {
            SlideFontManager.ExtractAndRegisterFonts(zip);
        }

        using (Bitmap bmp = new Bitmap(10, 10))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            using (Font f = SlideFontManager.CreateFont("Noto Serif Display Bold", 40f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                // Measure 'C' vs 'CỰC'
                Console.WriteLine("Font used: " + f.FontFamily.Name);
                Console.WriteLine("Measure 'C': " + g.MeasureString("C", f).Width);
                Console.WriteLine("Measure 'Ự': " + g.MeasureString("Ự", f).Width);
                Console.WriteLine("Measure 'CỰC': " + g.MeasureString("CỰC", f).Width);
            }
        }
    }
}

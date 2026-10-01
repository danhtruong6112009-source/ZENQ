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
            using (Font f = SlideFontManager.CreateFont("Noto Serif Display Bold", 49.2f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                string text = "VAI TRÒ VÀ TÁC ĐỘNG TÍCH CỰC";
                SizeF sz1 = g.MeasureString(text, f);
                SizeF sz2 = g.MeasureString(text, f, PointF.Empty, StringFormat.GenericTypographic);
                Console.WriteLine("Font: " + f.Name + ", size=" + f.Size + " " + f.Unit);
                Console.WriteLine("MeasureString default   : " + sz1.Width + " x " + sz1.Height);
                Console.WriteLine("MeasureString Typographic: " + sz2.Width + " x " + sz2.Height);
                Console.WriteLine("Box Width: 815.0");
            }
        }
    }
}

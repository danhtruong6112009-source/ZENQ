using System;
using System.Drawing;
using System.IO;

class Test
{
    static void Main()
    {
        string p = @"C:\Users\Danh\AppData\Local\Temp\genoffice_img_f717c75fb0544fb99ed07e8a702305d1.wmf";
        if (!File.Exists(p)) { Console.WriteLine("File not found"); return; }
        using (Image img = Image.FromFile(p))
        {
            Console.WriteLine("Type: " + img.GetType().FullName);
            Console.WriteLine("Width: " + img.Width + ", Height: " + img.Height);
            Console.WriteLine("HRes: " + img.HorizontalResolution + ", VRes: " + img.VerticalResolution);
            Console.WriteLine("Is Metafile: " + (img is System.Drawing.Imaging.Metafile));
        }
    }
}

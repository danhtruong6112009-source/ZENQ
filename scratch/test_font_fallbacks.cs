using System;
using System.Drawing;

class Program
{
    static void Main()
    {
        string text = "VAI TRÒ VÀ TÁC ĐỘNG TÍCH CỰC";
        string[] fonts = new string[] { "Arial", "Calibri", "Segoe UI", "Tahoma", "Muli Heavy", "Proxima Nova Bold" };
        using (var bmp = new Bitmap(10, 10))
        using (var g = Graphics.FromImage(bmp))
        {
            foreach (var fn in fonts)
            {
                using (var f = new Font(fn, 49.2f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    var sz = g.MeasureString(text, f, PointF.Empty, StringFormat.GenericTypographic);
                    Console.WriteLine(string.Format("{0,-20}: width = {1:F2}", fn, sz.Width));
                }
            }
        }
    }
}

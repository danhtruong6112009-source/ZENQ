using System;
using System.Drawing;
using GenOffice.Core;

class MeasureWords
{
    static void Main()
    {
        using (Font f = SlideFontManager.CreateFont("Anton", 47.18667f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (Bitmap b = new Bitmap(1, 1))
        using (Graphics g = Graphics.FromImage(b))
        {
            string[] testStrings = new string[] {
                "  ",
                "T?n d?ng",
                "T?n d?ng ngu?n l?c",
                "T?n d?ng ngu?n l?c bn ngoi:",
                "T?n d?ng ngu?n l?c bn ngoi: Ti?p thu",
                "  T?n d?ng ngu?n l?c bn ngoi:",
                "  T?n d?ng ngu?n l?c bn ngoi: Ti?p thu",
            };
            foreach (var s in testStrings)
            {
                float w = g.MeasureString(s, f, PointF.Empty, StringFormat.GenericTypographic).Width;
                Console.WriteLine(string.Format("'{0}': {1:F1} px", s, w));
            }
        }
    }
}

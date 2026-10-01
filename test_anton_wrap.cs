using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

class Program
{
    static void Main()
    {
        PrivateFontCollection pfc = new PrivateFontCollection();
        pfc.AddFontFile("Anton.ttf");
        FontFamily fam = pfc.Families[0];

        Bitmap bmp = new Bitmap(1920, 1080);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.DarkBlue);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // Box dimensions from slide3.xml:
            // x = 510755 / 9525 = 53.6 px
            // y = 2285538 / 9525 = 239.9 px
            // w = 11345478 / 9525 = 1191.1 px
            // h = 5712327 / 9525 = 599.7 px
            // sz = 12302 -> 123.02 pt -> 123.02 * 96 / 72 = 164.0 px
            float fontSizePx = (12302f / 100f) * (96f / 72f);
            RectangleF rect = new RectangleF(53.6f, 239.9f, 1191.1f, 599.7f);

            using (Font f = new Font(fam, fontSizePx, FontStyle.Regular, GraphicsUnit.Pixel))
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
                g.DrawRectangle(Pens.Yellow, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawString("KHÁI NIỆM VÀ SỰ CẦN THIẾT HỘI NHẬP KINH TẾ QUỐC TẾ", f, Brushes.White, rect, sf);
            }
        }
        bmp.Save("test_anton_wrap.png");
        Console.WriteLine("Saved test_anton_wrap.png");
    }
}

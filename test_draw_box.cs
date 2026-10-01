using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

class Program
{
    static void Main()
    {
        PrivateFontCollection pfc = new PrivateFontCollection();
        pfc.AddFontFile("font39.otf"); // Motoya Emboss

        Bitmap bmp = new Bitmap(1920, 1080);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.DarkBlue);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            FontFamily fam = pfc.Families[0];
            // sz is 14525 -> 145.25 pt
            using (Font f = new Font(fam, 145.25f, FontStyle.Regular, GraphicsUnit.Point))
            {
                RectangleF rect = new RectangleF(53.6f, -175.0f, 353.0f, 407.0f);
                g.DrawRectangle(Pens.Red, rect.X, rect.Y, rect.Width, rect.Height);

                // Try drawing with DrawString
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                g.DrawString("1.", f, Brushes.White, rect, sf);

                // Let's also measure text
                SizeF sz = g.MeasureString("1.", f);
                Console.WriteLine("Measured size: " + sz.Width + " x " + sz.Height);
            }
        }
        bmp.Save("test_draw_box.png");
        Console.WriteLine("Saved test_draw_box.png");
    }
}

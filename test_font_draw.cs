using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

class Program
{
    static void Main()
    {
        PrivateFontCollection pfc = new PrivateFontCollection();
        pfc.AddFontFile("font38.ttf"); // Anton
        pfc.AddFontFile("font39.otf"); // Motoya Emboss

        Bitmap bmp = new Bitmap(800, 400);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(0, 50, 150));
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // Draw "1." with font39
            FontFamily famEmboss = null;
            FontFamily famAnton = null;
            foreach (FontFamily fam in pfc.Families)
            {
                if (fam.Name == "Anton") famAnton = fam;
                else famEmboss = fam;
            }

            if (famEmboss != null)
            {
                using (Font f = new Font(famEmboss, 80, FontStyle.Regular))
                {
                    g.DrawString("1.", f, Brushes.White, new PointF(50, 20));
                }
            }

            if (famAnton != null)
            {
                using (Font f = new Font(famAnton, 40, FontStyle.Regular))
                {
                    g.DrawString("KHÁI NIỆM VÀ SỰ\nCẦN THIẾT HỘI NHẬP\nKINH TẾ QUỐC TẾ", f, Brushes.White, new PointF(50, 150));
                }
            }
        }
        bmp.Save("test_font_render.png");
        Console.WriteLine("Saved test_font_render.png");
    }
}

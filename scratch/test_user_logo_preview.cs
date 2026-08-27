using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.UI;

namespace GenOffice.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string outDir = @"C:\Users\Danh\.gemini\antigravity-ide\brain\b1e34c81-1d9d-4dbf-a2bd-168e6ec5fad6";

            // 1. Render Large 256x256 preview with rounded corners & shadow
            using (Bitmap b256 = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(b256))
            {
                g.Clear(Color.FromArgb(15, 23, 42));
                GenOfficeLogoRenderer.DrawLogo(g, 16, 16, 224, true, true);
                b256.Save(Path.Combine(outDir, "user_logo_rounded_256.png"), ImageFormat.Png);
            }

            // 2. Render Title bar preview (26px)
            using (Bitmap bBar = new Bitmap(450, 36, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bBar))
            using (Font bf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            {
                using (LinearGradientBrush topBarBrush = new LinearGradientBrush(new Rectangle(0, 0, 450, 36), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 90f))
                {
                    g.FillRectangle(topBarBrush, 0, 0, 450, 36);
                }
                GenOfficeLogoRenderer.DrawLogo(g, 12, 5, 26, false, false);
                using (SolidBrush brandBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString("GenOffice", bf, brandBrush, 44, 8);
                }
                bBar.Save(Path.Combine(outDir, "user_titlebar_preview.png"), ImageFormat.Png);
            }

            // 3. Render Brand Banner
            using (Bitmap bBrand = new Bitmap(520, 160, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bBrand))
            using (Font bf = new Font("Segoe UI", 24f, FontStyle.Bold))
            using (Font sf = new Font("Segoe UI", 11.5f, FontStyle.Regular))
            {
                g.Clear(Color.FromArgb(15, 23, 42));
                GenOfficeLogoRenderer.DrawLogoWithBrand(g, 24, 24, 112, bf, sf, Color.FromArgb(248, 250, 252), Color.FromArgb(148, 163, 184));
                bBrand.Save(Path.Combine(outDir, "user_brand_preview.png"), ImageFormat.Png);
            }

            Console.WriteLine("[SUCCESS] User logo preview rendered!");
        }
    }
}

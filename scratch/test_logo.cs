using System;
using System.Drawing;
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
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // 1. Render Logo 256x256
            using (Bitmap bmp256 = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp256))
            {
                g.Clear(Color.FromArgb(15, 23, 42));
                GenOfficeLogoRenderer.DrawLogo(g, 16, 16, 224, true, true);
                bmp256.Save(Path.Combine(outDir, "logo_master_256.png"), ImageFormat.Png);
            }

            // 2. Render Logo with Brand 400x120
            using (Bitmap bmpBrand = new Bitmap(500, 140, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmpBrand))
            using (Font bf = new Font("Segoe UI", 24f, FontStyle.Bold))
            using (Font sf = new Font("Segoe UI", 12f, FontStyle.Regular))
            {
                g.Clear(Color.FromArgb(15, 23, 42));
                GenOfficeLogoRenderer.DrawLogoWithBrand(g, 24, 24, 88, bf, sf, Color.FromArgb(248, 250, 252), Color.FromArgb(148, 163, 184));
                bmpBrand.Save(Path.Combine(outDir, "logo_brand_preview.png"), ImageFormat.Png);
            }

            // 3. Render Titlebar preview 400x36
            using (Bitmap bmpBar = new Bitmap(450, 36, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmpBar))
            using (Font bf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            {
                using (System.Drawing.Drawing2D.LinearGradientBrush topBarBrush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 450, 36), Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 90f))
                {
                    g.FillRectangle(topBarBrush, 0, 0, 450, 36);
                }
                GenOfficeLogoRenderer.DrawLogo(g, 12, 5, 26, true, false);
                using (SolidBrush brandBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.DrawString("GenOffice", bf, brandBrush, 44, 8);
                }
                bmpBar.Save(Path.Combine(outDir, "titlebar_preview.png"), ImageFormat.Png);
            }

            Console.WriteLine("[SUCCESS] Previews generated successfully!");
        }
    }
}

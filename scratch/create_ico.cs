using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace GenOffice.Tools
{
    class IcoBuilder
    {
        static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0) { path.AddRectangle(rect); return path; }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Bitmap CreateRoundedLogo(Image source, int size, float cornerRadiusPercent = 0.22f)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                RectangleF rect = new RectangleF(0, 0, size, size);
                float radius = size * cornerRadiusPercent;

                using (GraphicsPath clipPath = CreateRoundedRect(rect, radius))
                {
                    g.SetClip(clipPath);
                    g.DrawImage(source, rect);
                    g.ResetClip();

                    // Optional subtle glass rim border
                    using (Pen p = new Pen(Color.FromArgb(40, 255, 255, 255), Math.Max(1f, size / 64f)))
                    {
                        g.DrawPath(p, clipPath);
                    }
                }
            }
            return bmp;
        }

        public static void SavePngsAsIco(List<Bitmap> bitmaps, string outputPath)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((short)0); // reserved
                bw.Write((short)1); // type 1 = ICO
                bw.Write((short)bitmaps.Count); // image count

                int offset = 6 + (16 * bitmaps.Count);
                List<byte[]> pngBuffers = new List<byte[]>();

                foreach (var bmp in bitmaps)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        byte[] pngBytes = ms.ToArray();
                        pngBuffers.Add(pngBytes);

                        // ICONDIRENTRY
                        bw.Write((byte)(bmp.Width >= 256 ? 0 : bmp.Width));
                        bw.Write((byte)(bmp.Height >= 256 ? 0 : bmp.Height));
                        bw.Write((byte)0); // color count
                        bw.Write((byte)0); // reserved
                        bw.Write((short)1); // color planes
                        bw.Write((short)32); // bit depth
                        bw.Write((int)pngBytes.Length); // size of image data
                        bw.Write((int)offset); // offset

                        offset += pngBytes.Length;
                    }
                }

                // Write PNG image payloads
                foreach (var bytes in pngBuffers)
                {
                    bw.Write(bytes);
                }
            }
        }

        static void Main(string[] args)
        {
            string pngPath = @"c:\Users\Danh\Desktop\genofficepy\GENOFFICE.png";
            string icoPath = @"c:\Users\Danh\Desktop\genofficepy\GenOffice.ico";

            if (!File.Exists(pngPath))
            {
                Console.WriteLine("[ERROR] File not found: " + pngPath);
                return;
            }

            using (Image src = Image.FromFile(pngPath))
            {
                int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
                List<Bitmap> list = new List<Bitmap>();
                foreach (int s in sizes)
                {
                    list.Add(CreateRoundedLogo(src, s, 0.22f));
                }

                SavePngsAsIco(list, icoPath);
                Console.WriteLine("[SUCCESS] GenOffice.ico generated with " + sizes.Length + " resolutions!");
            }
        }
    }
}

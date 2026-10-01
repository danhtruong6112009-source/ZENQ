using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("GenOffice.exe");
            Assembly asm = Assembly.Load(bytes);
            Type writerType = asm.GetType("GenOffice.UI.WriterView");
            Type ribbonType = asm.GetType("GenOffice.UI.RibbonBar");

            dynamic w = Activator.CreateInstance(writerType);
            dynamic ribbon = Activator.CreateInstance(ribbonType);

            w.LoadDocumentFile("scratch\\test_math_doc.docx", ribbon);

            Bitmap bmp = new Bitmap(1000, 800);
            Graphics g = Graphics.FromImage(bmp);
            w.Draw(g, 1000, 800, ribbon);

            w.ScrollY = 10440; // Step 40
            w.Draw(g, 1000, 800, ribbon);

            foreach (var shp in w.DocumentShapes)
            {
                if (shp.LastBounds != Rectangle.Empty && shp.LastBounds.Y >= 0 && shp.LastBounds.Y <= 800)
                {
                    Image img = shp.CachedImage;
                    Console.WriteLine("Shape " + shp.Id + " type: " + (img == null ? "null" : img.GetType().FullName) + ", Size: " + (img == null ? "" : (img.Width + "x" + img.Height)) + ", bounds: " + shp.LastBounds);

                    if (img != null)
                    {
                        // Test 1: Draw with current g settings + attr
                        ImageAttributes attr = new ImageAttributes();
                        Stopwatch sw1 = Stopwatch.StartNew();
                        for (int k = 0; k < 100; k++)
                        {
                            g.DrawImage(img, (Rectangle)shp.LastBounds, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attr);
                        }
                        sw1.Stop();

                        // Test 2: Draw simple g.DrawImage(img, bounds)
                        Stopwatch sw2 = Stopwatch.StartNew();
                        for (int k = 0; k < 100; k++)
                        {
                            g.DrawImage(img, (Rectangle)shp.LastBounds);
                        }
                        sw2.Stop();

                        // Test 3: InterpolationMode = NearestNeighbor
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        Stopwatch sw3 = Stopwatch.StartNew();
                        for (int k = 0; k < 100; k++)
                        {
                            g.DrawImage(img, (Rectangle)shp.LastBounds);
                        }
                        sw3.Stop();

                        // Test 4: InterpolationMode = Bilinear
                        g.InterpolationMode = InterpolationMode.Bilinear;
                        Stopwatch sw4 = Stopwatch.StartNew();
                        for (int k = 0; k < 100; k++)
                        {
                            g.DrawImage(img, (Rectangle)shp.LastBounds);
                        }
                        sw4.Stop();

                        // Test 5: InterpolationMode = HighQualityBicubic
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        Stopwatch sw5 = Stopwatch.StartNew();
                        for (int k = 0; k < 100; k++)
                        {
                            g.DrawImage(img, (Rectangle)shp.LastBounds);
                        }
                        sw5.Stop();

                        Console.WriteLine("  100 draws with attr: " + sw1.ElapsedMilliseconds + " ms");
                        Console.WriteLine("  100 draws simple: " + sw2.ElapsedMilliseconds + " ms");
                        Console.WriteLine("  100 draws NearestNeighbor: " + sw3.ElapsedMilliseconds + " ms");
                        Console.WriteLine("  100 draws Bilinear: " + sw4.ElapsedMilliseconds + " ms");
                        Console.WriteLine("  100 draws HighQualityBicubic: " + sw5.ElapsedMilliseconds + " ms");
                        break; // Just test the first one
                    }
                }
            }

            g.Dispose();
            bmp.Dispose();
        }
    }
}

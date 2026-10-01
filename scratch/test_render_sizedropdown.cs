using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.UI;

namespace TestRender
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";

                // 1. Size Dropdown - Writer (Blue)
                using (SizeDropDownForm form = new SizeDropDownForm(12f, new Point(100, 100), Color.FromArgb(16, 110, 190)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(outDir, "sizedropdown_writer.png"), ImageFormat.Png);
                    }
                    form.Close();
                }

                // 2. Size Dropdown - Calc (Green)
                using (SizeDropDownForm form = new SizeDropDownForm(11f, new Point(100, 100), Color.FromArgb(16, 124, 65)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(outDir, "sizedropdown_calc.png"), ImageFormat.Png);
                    }
                    form.Close();
                }

                // 3. Size Dropdown - Slides (Terracotta)
                using (SizeDropDownForm form = new SizeDropDownForm(18f, new Point(100, 100), Color.FromArgb(210, 71, 38)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(outDir, "sizedropdown_slides.png"), ImageFormat.Png);
                    }
                    form.Close();
                }

                // 4. Size Dropdown - PDF (Red)
                using (SizeDropDownForm form = new SizeDropDownForm(14f, new Point(100, 100), Color.FromArgb(220, 38, 38)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(outDir, "sizedropdown_pdf.png"), ImageFormat.Png);
                    }
                    form.Close();
                }

                // 5. Font Dropdown - Writer (Blue)
                using (FontDropDownForm form = new FontDropDownForm("Times New Roman", new Point(200, 100), null, Color.FromArgb(16, 110, 190)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(outDir, "fontdropdown_writer.png"), ImageFormat.Png);
                    }
                    form.Close();
                }

                // 6. Font Dropdown - Calc (Green)
                using (FontDropDownForm form = new FontDropDownForm("Calibri", new Point(200, 100), null, Color.FromArgb(16, 124, 65)))
                {
                    form.Show();
                    Application.DoEvents();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(Path.Combine(outDir, "fontdropdown_calc.png"), ImageFormat.Png);
                    }
                    form.Close();
                }

                Console.WriteLine("ALL 6 RENDERED SUCCESSFULLY");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

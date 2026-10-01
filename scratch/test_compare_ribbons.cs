using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice;
using GenOffice.UI;

namespace CompareRibbons
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                int w = 1280;
                int h = 768;

                // Test 1: Writer
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(w, h);
                    form.Ribbon.StandaloneMode = true;
                    form.Ribbon.CurrentMode = OfficeAppMode.Writer;
                    form.Ribbon.ActiveRibbonTab = 0;
                    using (Bitmap bmp = new Bitmap(w, 160))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.Ribbon.Draw(g, w, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\ribbon_writer.png", ImageFormat.Png);
                    }
                }

                // Test 2: Calc
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(w, h);
                    form.Ribbon.StandaloneMode = true;
                    form.Ribbon.CurrentMode = OfficeAppMode.Calc;
                    form.Ribbon.ActiveRibbonTab = 0;
                    using (Bitmap bmp = new Bitmap(w, 160))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.Ribbon.Draw(g, w, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\ribbon_calc.png", ImageFormat.Png);
                    }
                }

                // Test 3: Slides
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(w, h);
                    form.Ribbon.StandaloneMode = true;
                    form.Ribbon.CurrentMode = OfficeAppMode.Slides;
                    form.Ribbon.ActiveRibbonTab = 0;
                    using (Bitmap bmp = new Bitmap(w, 160))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.Ribbon.Draw(g, w, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\ribbon_slides.png", ImageFormat.Png);
                    }
                }

                // Test 4: PDF
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(w, h);
                    form.Ribbon.StandaloneMode = true;
                    form.Ribbon.CurrentMode = OfficeAppMode.Pdf;
                    form.Ribbon.ActiveRibbonTab = 0;
                    using (Bitmap bmp = new Bitmap(w, 160))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.Ribbon.Draw(g, w, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        bmp.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\ribbon_pdf.png", ImageFormat.Png);
                    }
                }

                Console.WriteLine("ALL RIBBONS SAVED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex);
            }
        }
    }
}

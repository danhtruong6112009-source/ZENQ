using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.UI;

namespace InspectRibbons
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            OfficeAppMode[] modes = new OfficeAppMode[] { OfficeAppMode.Writer, OfficeAppMode.Calc, OfficeAppMode.Slides, OfficeAppMode.Pdf };
            int w = 1280;
            int h = 160;

            foreach (var mode in modes)
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(w, h);
                    form.Ribbon.StandaloneMode = true;
                    form.Ribbon.CurrentMode = mode;
                    form.Ribbon.ActiveRibbonTab = 0;
                    using (Bitmap bmp = new Bitmap(w, h))
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        form.Ribbon.Draw(g, w, h, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        Console.WriteLine("=== MODE: " + mode + " ===");
                        Console.WriteLine("RibbonIslandBounds: " + form.Ribbon.RibbonIslandBounds);
                        Console.WriteLine("RibbonFileBtnBounds: " + form.Ribbon.RibbonFileBtnBounds);
                        Console.WriteLine("RibbonTabs count: " + form.Ribbon.RibbonTabs.Length);
                        for (int i = 0; i < form.Ribbon.RibbonTabBounds.Count; i++)
                        {
                            Console.WriteLine("  Tab[" + i + "] '" + form.Ribbon.RibbonTabs[i] + "': " + form.Ribbon.RibbonTabBounds[i]);
                        }
                    }
                }
            }
        }
    }
}

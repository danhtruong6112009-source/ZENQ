using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace Diagnostic
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            using (AppForm form = new AppForm())
            {
                form.Size = new Size(1280, 768);
                form.Ribbon.StandaloneMode = false;
                form.Ribbon.DocTabs.Clear();
                form.Ribbon.DocTabs.Add(new DocTab("Trang chính", OfficeAppMode.Home, false));
                form.Ribbon.DocTabs.Add(new DocTab("Văn bản 1.docx", OfficeAppMode.Writer, false));
                form.Ribbon.DocTabs.Add(new DocTab("Bảng tính 1.xlsx", OfficeAppMode.Calc, false));
                form.Ribbon.DocTabs.Add(new DocTab("Trình chiếu 1.pptx", OfficeAppMode.Slides, false));

                // Switch to Writer
                form.Ribbon.SwitchDocTab(1, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                using (Bitmap bmp = new Bitmap(1280, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    form.Ribbon.Draw(g, 1280, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                }
                Console.WriteLine("=== WRITER ===");
                Console.WriteLine("ActiveDocIndex: " + form.Ribbon.ActiveDocIndex);
                Console.WriteLine("ActiveRibbonTab: " + form.Ribbon.ActiveRibbonTab);
                Console.WriteLine("DocTabs count: " + form.Ribbon.DocTabBounds.Count);
                for (int i = 0; i < form.Ribbon.DocTabBounds.Count; i++)
                {
                    Console.WriteLine("  DocTab[" + i + "]: " + form.Ribbon.DocTabBounds[i]);
                }
                Console.WriteLine("RibbonFileBtn: " + form.Ribbon.RibbonFileBtnBounds);
                Console.WriteLine("RibbonIsland: " + form.Ribbon.RibbonIslandBounds);
                Console.WriteLine("RibbonTabs count: " + form.Ribbon.RibbonTabBounds.Count);
                for (int i = 0; i < form.Ribbon.RibbonTabBounds.Count; i++)
                {
                    Console.WriteLine("  RTab[" + i + "] (" + form.Ribbon.RibbonTabs[i] + "): " + form.Ribbon.RibbonTabBounds[i]);
                }

                // Switch to Calc
                form.Ribbon.SwitchDocTab(2, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                using (Bitmap bmp = new Bitmap(1280, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    form.Ribbon.Draw(g, 1280, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                }
                Console.WriteLine("=== CALC ===");
                Console.WriteLine("ActiveDocIndex: " + form.Ribbon.ActiveDocIndex);
                Console.WriteLine("ActiveRibbonTab: " + form.Ribbon.ActiveRibbonTab);
                Console.WriteLine("DocTabs count: " + form.Ribbon.DocTabBounds.Count);
                for (int i = 0; i < form.Ribbon.DocTabBounds.Count; i++)
                {
                    Console.WriteLine("  DocTab[" + i + "]: " + form.Ribbon.DocTabBounds[i]);
                }
                Console.WriteLine("RibbonFileBtn: " + form.Ribbon.RibbonFileBtnBounds);
                Console.WriteLine("RibbonIsland: " + form.Ribbon.RibbonIslandBounds);
                Console.WriteLine("RibbonTabs count: " + form.Ribbon.RibbonTabBounds.Count);
                for (int i = 0; i < form.Ribbon.RibbonTabBounds.Count; i++)
                {
                    Console.WriteLine("  RTab[" + i + "] (" + form.Ribbon.RibbonTabs[i] + "): " + form.Ribbon.RibbonTabBounds[i]);
                }

                // Switch to Slides
                form.Ribbon.SwitchDocTab(3, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                using (Bitmap bmp = new Bitmap(1280, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    form.Ribbon.Draw(g, 1280, 160, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                }
                Console.WriteLine("=== SLIDES ===");
                Console.WriteLine("ActiveDocIndex: " + form.Ribbon.ActiveDocIndex);
                Console.WriteLine("ActiveRibbonTab: " + form.Ribbon.ActiveRibbonTab);
                Console.WriteLine("DocTabs count: " + form.Ribbon.DocTabBounds.Count);
                for (int i = 0; i < form.Ribbon.DocTabBounds.Count; i++)
                {
                    Console.WriteLine("  DocTab[" + i + "]: " + form.Ribbon.DocTabBounds[i]);
                }
                Console.WriteLine("RibbonFileBtn: " + form.Ribbon.RibbonFileBtnBounds);
                Console.WriteLine("RibbonIsland: " + form.Ribbon.RibbonIslandBounds);
                Console.WriteLine("RibbonTabs count: " + form.Ribbon.RibbonTabBounds.Count);
                for (int i = 0; i < form.Ribbon.RibbonTabBounds.Count; i++)
                {
                    Console.WriteLine("  RTab[" + i + "] (" + form.Ribbon.RibbonTabs[i] + "): " + form.Ribbon.RibbonTabBounds[i]);
                }
            }
        }
    }
}

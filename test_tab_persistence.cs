using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestTabPersistence
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

                // Start in Writer, select Ribbon Tab 4 (Bố trí)
                form.Ribbon.SwitchDocTab(1, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                form.Ribbon.ActiveRibbonTab = 4;
                Console.WriteLine("Writer active ribbon tab: " + form.Ribbon.ActiveRibbonTab);

                // Switch to Calc
                form.Ribbon.SwitchDocTab(2, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                Console.WriteLine("Switched to Calc - active ribbon tab: " + form.Ribbon.ActiveRibbonTab + " (" + form.Ribbon.RibbonTabs[form.Ribbon.ActiveRibbonTab] + ")");

                // In Calc, select Ribbon Tab 0 (Trang đầu)
                form.Ribbon.ActiveRibbonTab = 0;
                Console.WriteLine("In Calc, user clicked Trang đầu: " + form.Ribbon.ActiveRibbonTab + " (" + form.Ribbon.RibbonTabs[form.Ribbon.ActiveRibbonTab] + ")");

                // Switch back to Writer
                form.Ribbon.SwitchDocTab(1, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                Console.WriteLine("Switched back to Writer - active ribbon tab: " + form.Ribbon.ActiveRibbonTab + " (" + form.Ribbon.RibbonTabs[form.Ribbon.ActiveRibbonTab] + ")");
            }
        }
    }
}

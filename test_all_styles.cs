using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestAllStyles
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            int w = 1280;
            int h = 768;
            int ribbonH = 160;

            // Test 1: Standard (Classic) Style for all 4 apps
            AppSettings.Instance.RibbonStyle = RibbonStyle.Standard;

            Bitmap standardStacked = new Bitmap(w, ribbonH * 4);
            using (Graphics g = Graphics.FromImage(standardStacked))
            {
                g.Clear(Color.LightGray);
                OfficeAppMode[] modes = new OfficeAppMode[] { OfficeAppMode.Writer, OfficeAppMode.Calc, OfficeAppMode.Slides, OfficeAppMode.Pdf };
                string[] titles = new string[] { "Văn bản 1.docx", "Bảng tính 1.xlsx", "Trình chiếu 1.pptx", "Tài liệu 1.pdf" };

                for (int m = 0; m < 4; m++)
                {
                    using (AppForm form = new AppForm())
                    {
                        form.Size = new Size(w, h);
                        form.Ribbon.StandaloneMode = false;
                        form.Ribbon.DocTabs.Clear();
                        form.Ribbon.DocTabs.Add(new DocTab("Trang chính", OfficeAppMode.Home, false));
                        form.Ribbon.DocTabs.Add(new DocTab(titles[m], modes[m], true));
                        form.Ribbon.SwitchDocTab(1, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                        form.Ribbon.ActiveRibbonTab = 0;

                        using (Bitmap b = new Bitmap(w, ribbonH))
                        {
                            using (Graphics gb = Graphics.FromImage(b))
                            {
                                form.Ribbon.Draw(gb, w, ribbonH, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            g.DrawImage(b, 0, m * ribbonH);
                        }
                    }
                }
            }
            standardStacked.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\standard_all_four_stacked.png", ImageFormat.Png);

            // Test 2: In Calc (Island style), render ALL 7 tabs stacked
            AppSettings.Instance.RibbonStyle = RibbonStyle.Island;
            Bitmap calcAllTabs = new Bitmap(w, ribbonH * 7);
            using (Graphics g = Graphics.FromImage(calcAllTabs))
            {
                g.Clear(Color.LightGray);
                for (int t = 0; t < 7; t++)
                {
                    using (AppForm form = new AppForm())
                    {
                        form.Size = new Size(w, h);
                        form.Ribbon.StandaloneMode = false;
                        form.Ribbon.DocTabs.Clear();
                        form.Ribbon.DocTabs.Add(new DocTab("Trang chính", OfficeAppMode.Home, false));
                        form.Ribbon.DocTabs.Add(new DocTab("Bảng tính 1.xlsx", OfficeAppMode.Calc, true));
                        form.Ribbon.SwitchDocTab(1, form.SlidesView, form.WriterView, form.CalcView, form.PdfView);
                        form.Ribbon.ActiveRibbonTab = t;

                        using (Bitmap b = new Bitmap(w, ribbonH))
                        {
                            using (Graphics gb = Graphics.FromImage(b))
                            {
                                form.Ribbon.Draw(gb, w, ribbonH, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            g.DrawImage(b, 0, t * ribbonH);
                        }
                    }
                }
            }
            calcAllTabs.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\calc_all_7_tabs_stacked.png", ImageFormat.Png);

            Console.WriteLine("ALL TESTS SAVED!");
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace CompareAllFour
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            int w = 1280;
            int h = 768;
            int ribbonH = 160;

            Bitmap stacked = new Bitmap(w, ribbonH * 4);
            using (Graphics gStack = Graphics.FromImage(stacked))
            {
                gStack.Clear(Color.LightGray);

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
                            using (Graphics g = Graphics.FromImage(b))
                            {
                                form.Ribbon.Draw(g, w, ribbonH, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            gStack.DrawImage(b, 0, m * ribbonH);
                        }
                    }
                }
            }

            stacked.Save(@"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\all_four_ribbons_stacked.png", ImageFormat.Png);
            Console.WriteLine("STACKED IMAGE SAVED!");
        }
    }
}

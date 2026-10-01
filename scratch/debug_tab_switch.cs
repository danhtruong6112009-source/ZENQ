using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;
using GenOffice.Core;

namespace GenOffice.Test
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                RibbonBar ribbon = new RibbonBar();
                SlidesView slides = new SlidesView();
                slides.Presentation = new PresentationRoot();
                slides.Presentation.Slides.Add(new SlideNode());

                WriterView writer = new WriterView();
                CalcView calc = new CalcView();
                PdfView pdf = new PdfView();

                // Add tabs
                ribbon.AddDocTab("Văn bản Mới.docx", OfficeAppMode.Writer);
                ribbon.AddDocTab("Bảng tính Mới.xlsx", OfficeAppMode.Calc);
                ribbon.AddDocTab("Trình chiếu Mới.pptx", OfficeAppMode.Slides);

                // 1. Writer
                ribbon.SwitchDocTab(1, slides, writer, calc, pdf);
                using (Bitmap bmp = new Bitmap(1000, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    ribbon.Draw(g, 1000, 155, writer, slides, calc, pdf);
                }
                Console.WriteLine("Writer TabScrollX: " + ribbon.TabScrollX + ", TargetTabScrollX: " + ribbon.TargetTabScrollX);
                for (int i = 0; i < ribbon.DocTabBounds.Count; i++)
                {
                    Console.WriteLine("Writer DocTab[" + i + "]: " + ribbon.DocTabBounds[i]);
                }

                // 2. Calc
                ribbon.SwitchDocTab(2, slides, writer, calc, pdf);
                using (Bitmap bmp = new Bitmap(1000, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    ribbon.Draw(g, 1000, 155, writer, slides, calc, pdf);
                }
                Console.WriteLine("Calc TabScrollX: " + ribbon.TabScrollX + ", TargetTabScrollX: " + ribbon.TargetTabScrollX);
                for (int i = 0; i < ribbon.DocTabBounds.Count; i++)
                {
                    Console.WriteLine("Calc DocTab[" + i + "]: " + ribbon.DocTabBounds[i]);
                }

                // 3. Slides
                ribbon.SwitchDocTab(3, slides, writer, calc, pdf);
                using (Bitmap bmp = new Bitmap(1000, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    ribbon.Draw(g, 1000, 155, writer, slides, calc, pdf);
                }
                Console.WriteLine("Slides TabScrollX: " + ribbon.TabScrollX + ", TargetTabScrollX: " + ribbon.TargetTabScrollX);
                for (int i = 0; i < ribbon.DocTabBounds.Count; i++)
                {
                    Console.WriteLine("Slides DocTab[" + i + "]: " + ribbon.DocTabBounds[i]);
                }

                // Capture all 3 states
                ribbon.SwitchDocTab(1, slides, writer, calc, pdf);
                Bitmap bmpWriter = new Bitmap(1000, 160);
                using (Graphics g = Graphics.FromImage(bmpWriter))
                {
                    g.Clear(Color.FromArgb(243, 246, 252));
                    ribbon.Draw(g, 1000, 155, writer, slides, calc, pdf);
                }
                bmpWriter.Save(@"scratch\state_writer.png", ImageFormat.Png);

                ribbon.SwitchDocTab(2, slides, writer, calc, pdf);
                Bitmap bmpCalc = new Bitmap(1000, 160);
                using (Graphics g = Graphics.FromImage(bmpCalc))
                {
                    g.Clear(Color.FromArgb(243, 246, 252));
                    ribbon.Draw(g, 1000, 155, writer, slides, calc, pdf);
                }
                bmpCalc.Save(@"scratch\state_calc.png", ImageFormat.Png);

                ribbon.SwitchDocTab(3, slides, writer, calc, pdf);
                Bitmap bmpSlides = new Bitmap(1000, 160);
                using (Graphics g = Graphics.FromImage(bmpSlides))
                {
                    g.Clear(Color.FromArgb(243, 246, 252));
                    ribbon.Draw(g, 1000, 155, writer, slides, calc, pdf);
                }
                bmpSlides.Save(@"scratch\state_slides.png", ImageFormat.Png);

                Console.WriteLine("CAPTURED ALL STATES SUCCESSFULLY!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}

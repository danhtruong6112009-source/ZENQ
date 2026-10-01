using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.UI;

namespace TestSlidesRibbon
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                int width = 1280;
                int height = 155;
                string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";

                string[] tabNames = new string[] {
                    "slides_tab0_home.png",
                    "slides_tab1_insert.png",
                    "slides_tab2_design.png",
                    "slides_tab3_transitions.png",
                    "slides_tab4_animations.png",
                    "slides_tab5_slideshow.png",
                    "slides_tab6_view.png"
                };

                for (int t = 0; t <= 6; t++)
                {
                    using (Bitmap bmp = new Bitmap(width, height))
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(243, 244, 246));
                        RibbonBar ribbon = new RibbonBar();
                        ribbon.CurrentMode = OfficeAppMode.Slides;
                        ribbon.ActiveRibbonTab = t;
                        ribbon.StandaloneMode = true;

                        SlidesView slidesView = new SlidesView();

                        ribbon.Draw(g, width, height, null, slidesView, null, null);

                        string outPath = Path.Combine(outDir, tabNames[t]);
                        bmp.Save(outPath, ImageFormat.Png);
                        Console.WriteLine("SAVED TAB " + t + ": " + outPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

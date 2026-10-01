using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.UI;

namespace TestCalcRibbon
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                int width = 1260;
                int height = 155;
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(243, 244, 246));
                    RibbonBar ribbon = new RibbonBar();
                    ribbon.CurrentMode = OfficeAppMode.Calc;
                    ribbon.ActiveRibbonTab = 0;
                    ribbon.StandaloneMode = true;

                    CalcView calcView = new CalcView();
                    calcView.CurrentSheet.GetOrCreateCell(1, 1).Format.Align = 0; // Align left active to test active state

                    ribbon.Draw(g, width, height, null, null, calcView, null);

                    string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";
                    string outPath = Path.Combine(outDir, "calc_ribbon_new_preview.png");
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SUCCESS: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

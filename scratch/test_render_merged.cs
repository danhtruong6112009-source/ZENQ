using System;
using System.Drawing;
using System.IO;
using GenOffice.Core;
using GenOffice.UI;

namespace TestRenderMerged
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                CalcView cv = new CalcView();
                cv.SelectedRow = 1; cv.SelectedCol = 1;
                cv.SelEndRow = 5; cv.SelEndCol = 5; // A1:E5
                cv.MergeAndCenter();
                cv.CurrentSheet.SetCellText(1, 1, "BẢNG DỮ LIỆU ĐÃ GỘP 5x5");

                // Render into bitmap
                Bitmap bmp = new Bitmap(800, 500);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    cv.Draw(g, 800, 500);
                }

                string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "merged_render_test.png");
                bmp.Save(outPath);
                Console.WriteLine("MERGED_RENDER_SAVED: " + outPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

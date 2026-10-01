using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class CaptureRibbonAiButton
    {
        [STAThread]
        public static void Main()
        {
            RibbonBar ribbon = new RibbonBar();
            ribbon.CurrentMode = OfficeAppMode.Writer;

            int width = 1200;
            int height = 160;

            using (Bitmap bmp = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(243, 244, 246));
                ribbon.Draw(g, width, height, null, null, null, null);

                // Crop top-right ribbon area
                int cropW = 260;
                int cropH = 65;
                int cropX = width - cropW;
                int cropY = 15;
                using (Bitmap crop = new Bitmap(cropW, cropH))
                using (Graphics cg = Graphics.FromImage(crop))
                {
                    cg.Clear(Color.FromArgb(243, 244, 246));
                    cg.DrawImage(bmp, new Rectangle(0, 0, cropW, cropH), new Rectangle(cropX, cropY, cropW, cropH), GraphicsUnit.Pixel);
                    crop.Save("scratch/ribbon_ai_button_cropped.png");
                }
                bmp.Save("scratch/ribbon_ai_button_full.png");
            }

            Console.WriteLine("[PASS] Rendered ribbon AI button to scratch/ribbon_ai_button_cropped.png");
        }
    }
}

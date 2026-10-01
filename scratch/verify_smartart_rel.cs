using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.UI;

namespace GenOffice.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (SmartArtGalleryForm form = new SmartArtGalleryForm())
            {
                // Switch to Relationship category (index 5)
                var selCatField = typeof(SmartArtGalleryForm).GetField("selectedCategoryIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                selCatField.SetValue(form, 5); // Relationship

                var filterMethod = typeof(SmartArtGalleryForm).GetMethod("FilterTemplatesByCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                filterMethod.Invoke(form, null);

                int w = 780;
                int h = 520;
                using (Bitmap bmp = new Bitmap(w, h))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        form.RenderDialog(g, w, h);
                    }

                    string outPath = @"C:\Users\Danh\.gemini\antigravity\brain\2a53d4ad-f7b0-44a8-b92e-6ac96e18e730\verify_smartart_dialog_relationship.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("Saved relationship test screenshot to: " + outPath);
                }
            }
        }
    }
}

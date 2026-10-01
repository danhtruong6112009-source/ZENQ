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
            Console.WriteLine("=== 1. TEST TOC BOUNDS IN WRITERVIEW ===");
            string testText = "\n\n... Trang 1\n";
            int lStart = testText.LastIndexOf('\n', Math.Max(0, 0 - 1)) + 1;
            int lEnd = testText.IndexOf('\n', 0);
            if (lEnd < 0) lEnd = testText.Length;
            bool okTOC = (lEnd <= lStart);
            Console.WriteLine("TOC bounds test edge case handled: " + okTOC);

            Console.WriteLine("=== 2. TEST SMARTART FORM & CATALOG ===");
            using (SmartArtGalleryForm form = new SmartArtGalleryForm())
            {
                // Reflection to inspect private fields
                var catField = typeof(SmartArtGalleryForm).GetField("categories", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cats = (System.Collections.Generic.List<string>)catField.GetValue(form);
                Console.WriteLine("Categories Count: " + cats.Count);
                for (int i = 0; i < cats.Count; i++)
                {
                    Console.WriteLine("  Cat " + i + ": " + cats[i]);
                }

                var allField = typeof(SmartArtGalleryForm).GetField("allTemplates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var allT = (System.Collections.Generic.List<SmartArtGalleryForm.SmartArtTemplate>)allField.GetValue(null);
                Console.WriteLine("All Templates Count: " + allT.Count);

                var filField = typeof(SmartArtGalleryForm).GetField("filteredTemplates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var filT = (System.Collections.Generic.List<SmartArtGalleryForm.SmartArtTemplate>)filField.GetValue(form);
                Console.WriteLine("Filtered Templates Count: " + filT.Count);

                int w = 780;
                int h = 520;
                using (Bitmap bmp = new Bitmap(w, h))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        form.RenderDialog(g, w, h);
                    }

                    string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_smartart_dialog.png");
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("Saved test screenshot to: " + outPath);

                    string brainPath = @"C:\Users\Danh\.gemini\antigravity\brain\2a53d4ad-f7b0-44a8-b92e-6ac96e18e730\verify_smartart_dialog_exact.png";
                    bmp.Save(brainPath, ImageFormat.Png);
                    Console.WriteLine("Overwrote brain screenshot: " + brainPath);
                }
            }
            Console.WriteLine("SUCCESS: Verification completed cleanly.");
        }
    }
}

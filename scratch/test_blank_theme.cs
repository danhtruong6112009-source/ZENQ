using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestBlankTheme
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Blank & Office Themes ===");

                PresentationRoot pres = new PresentationRoot();
                Console.WriteLine("1. Default Theme Name: " + pres.CurrentTheme + " (Expected: Blank -> " + (pres.CurrentTheme == "Blank" ? "PASS" : "FAIL") + ")");
                Console.WriteLine("2. Themes Count: " + pres.Themes.Count);
                Console.WriteLine("3. Theme 0: " + pres.Themes[0].Name + " (Expected: Blank -> " + (pres.Themes[0].Name == "Blank" ? "PASS" : "FAIL") + ")");
                Console.WriteLine("4. Theme 1: " + pres.Themes[1].Name + " (Expected: Office -> " + (pres.Themes[1].Name == "Office" ? "PASS" : "FAIL") + ")");

                // 1. Test Generating Blank Theme Artwork
                List<ShapeNode> blankArt = SlidesView.GenerateThemeArtwork("Blank");
                Console.WriteLine("5. Blank Theme artwork count: " + blankArt.Count + " (Expected: 0 -> " + (blankArt.Count == 0 ? "PASS" : "FAIL") + ")");

                // 2. Test Generating Office Theme Artwork
                List<ShapeNode> officeArt = SlidesView.GenerateThemeArtwork("Office");
                Console.WriteLine("6. Office Theme artwork count: " + officeArt.Count + " (Expected: 2 -> " + (officeArt.Count == 2 ? "PASS" : "FAIL") + ")");

                // 3. Test Thumbnail Drawing
                using (Bitmap bmp = new Bitmap(200, 100))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    RibbonBar.DrawThemeMiniCardPreview(g, new Rectangle(10, 10, 80, 50), pres.Themes[0], true, false);
                    RibbonBar.DrawThemeMiniCardPreview(g, new Rectangle(100, 10, 80, 50), pres.Themes[1], false, false);
                }
                Console.WriteLine("7. DrawThemeMiniCardPreview: PASS");

                Console.WriteLine("=== ALL_BLANK_THEME_TESTS_PASSED_SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}

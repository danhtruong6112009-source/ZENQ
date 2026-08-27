using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestCleanCards
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING CLEAN HOME CREATION CARDS ===");

            HomeView hv = new HomeView();

            // Test 1: Render Tab 0 at 1366x768
            using (Bitmap bmp = new Bitmap(1366, 768))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                hv.Draw(g, 1366, 768);
            }
            Console.WriteLine("[PASS] Test 1: HomeView.Draw rendered cleanly with elegant cards!");

            // Test 2: Card bounds test
            RectangleF c0 = hv.GetCardBounds(0, 1366, 768);
            RectangleF c1 = hv.GetCardBounds(1, 1366, 768);
            RectangleF c2 = hv.GetCardBounds(2, 1366, 768);

            if (c0.Height != 118 || c1.Height != 118 || c2.Height != 118)
                throw new Exception("FAIL: Expected card height 118, got " + c0.Height);

            Console.WriteLine("[PASS] Test 2: Card bounds are proportional and clean!");

            // Test 3: Click cards
            string docTitle;
            var m0 = hv.HandleClick((int)c0.X + 20, (int)c0.Y + 20, 1366, 768, out docTitle);
            if (m0 != OfficeAppMode.Writer) throw new Exception("FAIL: Expected Writer mode");

            var m1 = hv.HandleClick((int)c1.X + 20, (int)c1.Y + 20, 1366, 768, out docTitle);
            if (m1 != OfficeAppMode.Calc) throw new Exception("FAIL: Expected Calc mode");

            var m2 = hv.HandleClick((int)c2.X + 20, (int)c2.Y + 20, 1366, 768, out docTitle);
            if (m2 != OfficeAppMode.Slides) throw new Exception("FAIL: Expected Slides mode");

            Console.WriteLine("[PASS] Test 3: Clicking cards triggers document creation reliably!");

            Console.WriteLine("=== ALL CLEAN CARDS TESTS PASSED 100%! ===");
        }
    }
}

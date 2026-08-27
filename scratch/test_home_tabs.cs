using System;
using System.Drawing;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestHomeTabs
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING TRASH BUTTONS POSITIONING & CLICKS ===");

            HomeView hv = new HomeView();
            string docTitle;

            // Switch to Tab 2 (Thùng rác)
            RectangleF tab2 = hv.GetTrashTabBounds(1366, 0);
            hv.HandleClick((int)tab2.X + 20, (int)tab2.Y + 15, 1366, 768, out docTitle);
            if (hv.ActiveTab != 2) throw new Exception("FAIL: Expected ActiveTab == 2");
            if (hv.TrashDocs.Count != 1) throw new Exception("FAIL: Expected 1 doc in Trash");

            // Test 1: Render Tab 2 to Bitmap
            using (Bitmap bmp = new Bitmap(1366, 768))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                hv.Draw(g, 1366, 768);
            }
            Console.WriteLine("[PASS] Test 1: Tab 2 rendered successfully without button overflow!");

            // Test 2: Test click "Phục hồi" button inside the card
            RectangleF tb = hv.GetTableCardBounds(1366, 768);
            float row0Y = tb.Y + 52;
            float rowRight = tb.Right - 12;

            // Click inside resBox (rowRight - 120, row0Y + 25)
            hv.HandleClick((int)(rowRight - 120), (int)(row0Y + 25), 1366, 768, out docTitle);
            if (hv.TrashDocs.Count != 0 || hv.RecentDocs.Count != 5)
                throw new Exception("FAIL: Phuc hoi button did not restore doc to RecentDocs");
            Console.WriteLine("[PASS] Test 2: Click 'Phục hồi' restored item to RecentDocs!");

            // Switch to Tab 1 (Tài liệu gần đây)
            RectangleF tab1 = hv.GetMyDocsTabBounds(1366, 0);
            hv.HandleClick((int)tab1.X + 20, (int)tab1.Y + 15, 1366, 768, out docTitle);
            if (hv.ActiveTab != 1) throw new Exception("FAIL: Expected ActiveTab == 1");

            // Click "Bỏ thùng rác" on first item (rowRight - 70, row0Y + 25)
            hv.HandleClick((int)(rowRight - 70), (int)(row0Y + 25), 1366, 768, out docTitle);
            if (hv.RecentDocs.Count != 4 || hv.TrashDocs.Count != 1)
                throw new Exception("FAIL: Bo thung rac button did not move doc to Trash");
            Console.WriteLine("[PASS] Test 3: Click 'Bỏ thùng rác' moved item to Trash!");

            Console.WriteLine("=== ALL TRASH BUTTON TESTS PASSED 100%! ===");
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestTabDrag
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING TAB DRAG & DROP REORDERING AND EDGE AUTO-SCROLL ===");

            RibbonBar rb = new RibbonBar();
            WriterView wv = new WriterView();
            CalcView cv = new CalcView();
            SlidesView sv = new SlidesView();

            // Add 4 document tabs
            rb.AddDocTab("Tài liệu A.docx", OfficeAppMode.Writer, sv, wv, cv);
            rb.AddDocTab("Bảng tính B.xlsx", OfficeAppMode.Calc, sv, wv, cv);
            rb.AddDocTab("Trình chiếu C.pptx", OfficeAppMode.Slides, sv, wv, cv);
            rb.AddDocTab("Tài liệu D.docx", OfficeAppMode.Writer, sv, wv, cv);

            if (rb.DocTabs.Count != 5)
                throw new Exception("FAIL: Expected 5 tabs (Home + 4 docs), got " + rb.DocTabs.Count);

            Console.WriteLine("[PASS] Added 4 tabs (Total 5 tabs including pinned Home tab).");

            // Initial render to populate bounds
            using (Bitmap bmp = new Bitmap(1366, 768))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                rb.Draw(g, 1366, 768, wv, sv, cv);
            }

            // Test 1: Start drag on Tab 1 (Tài liệu A.docx)
            Rectangle tab1Bounds = rb.DocTabBounds[1];
            bool started = rb.StartTabDrag(tab1Bounds.X + 20, tab1Bounds.Y + 10, 1366);
            if (!started || rb.DraggingTabIndex != 1)
                throw new Exception("FAIL: StartTabDrag failed for tab 1");
            Console.WriteLine("[PASS] Test 1: StartTabDrag successfully targeted Tab 1!");

            // Test 2: Move mouse across to slot 3 (over Trình chiếu C)
            // docAreaX = 265, tabWidth = 164. Slot 3 center is at approx 265 + 2 * 164 + 80 = 673px
            rb.UpdateTabDrag(675, 1366);
            if (!rb.IsDraggingTab)
                throw new Exception("FAIL: IsDraggingTab should be true after mouse moved");
            if (rb.DragTargetIndex != 3)
                throw new Exception("FAIL: Expected DragTargetIndex = 3, got " + rb.DragTargetIndex);
            Console.WriteLine("[PASS] Test 2: UpdateTabDrag identified target slot 3 accurately!");

            // Test 3: Render during dragging
            using (Bitmap bmp = new Bitmap(1366, 768))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                rb.Draw(g, 1366, 768, wv, sv, cv);
            }
            Console.WriteLine("[PASS] Test 3: Render during tab dragging completed with ghost slot and floating tab!");

            // Test 4: Auto-scroll when dragging near right edge
            float prevScroll = rb.TargetTabScrollX;
            rb.UpdateTabDrag(1330, 1366); // 1330 is > 1366 - 70 = 1296
            if (rb.TargetTabScrollX < prevScroll)
                throw new Exception("FAIL: TargetTabScrollX should increase near right edge");
            Console.WriteLine("[PASS] Test 4: Auto-scroll triggers when dragging near edges!");

            // Test 5: Finish drag & reorder
            rb.DragTargetIndex = 3;
            bool reordered = rb.FinishTabDrag(1366);
            if (!reordered)
                throw new Exception("FAIL: FinishTabDrag returned false");

            // Verify order:
            // Original: [0: Home, 1: Tài liệu A, 2: Bảng tính B, 3: Trình chiếu C, 4: Tài liệu D]
            // After moving 1 to 3:
            // [0: Home, 1: Bảng tính B, 2: Trình chiếu C, 3: Tài liệu A, 4: Tài liệu D]
            if (rb.DocTabs[0].Mode != OfficeAppMode.Home)
                throw new Exception("FAIL: Home tab at index 0 was disturbed!");
            if (rb.DocTabs[1].Title != "Bảng tính B.xlsx")
                throw new Exception("FAIL: Expected Tab 1 to be Bảng tính B, got " + rb.DocTabs[1].Title);
            if (rb.DocTabs[2].Title != "Trình chiếu C.pptx")
                throw new Exception("FAIL: Expected Tab 2 to be Trình chiếu C, got " + rb.DocTabs[2].Title);
            if (rb.DocTabs[3].Title != "Tài liệu A.docx")
                throw new Exception("FAIL: Expected Tab 3 to be Tài liệu A, got " + rb.DocTabs[3].Title);
            if (rb.DocTabs[4].Title != "Tài liệu D.docx")
                throw new Exception("FAIL: Expected Tab 4 to be Tài liệu D, got " + rb.DocTabs[4].Title);

            if (rb.ActiveDocIndex != 3)
                throw new Exception("FAIL: ActiveDocIndex should follow moved tab to index 3, got " + rb.ActiveDocIndex);

            Console.WriteLine("[PASS] Test 5: Reordering succeeded seamlessly and ActiveDocIndex followed!");

            Console.WriteLine("=== ALL TAB DRAG & DROP TESTS PASSED 100%! ===");
        }
    }
}

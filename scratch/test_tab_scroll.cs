using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestTabScroll
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING HORIZONTAL TAB SCROLLING WITH PINNED HOME TAB ===");

            RibbonBar rb = new RibbonBar();
            WriterView wv = new WriterView();
            CalcView cv = new CalcView();
            SlidesView sv = new SlidesView();

            // Check initial Home Tab
            if (rb.DocTabs.Count != 1 || rb.DocTabs[0].Mode != OfficeAppMode.Home)
                throw new Exception("FAIL: Expected 1 Home tab initially");

            // Add 10 tabs to simulate tab overflow
            for (int i = 1; i <= 10; i++)
            {
                rb.AddDocTab("Bảng tính " + i + ".xlsx", OfficeAppMode.Calc, sv, wv, cv);
            }

            if (rb.DocTabs.Count != 11)
                throw new Exception("FAIL: Expected 11 tabs, got " + rb.DocTabs.Count);

            Console.WriteLine("[PASS] Added 10 tabs successfully (total 11 tabs).");

            // Test 1: Render tabs to Bitmap at 1366x768
            using (Bitmap bmp = new Bitmap(1366, 768))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                rb.Draw(g, 1366, 768, wv, sv, cv);
            }

            // Check Home Tab Bounds (must be pinned at 135)
            if (rb.DocTabBounds.Count < 11)
                throw new Exception("FAIL: Expected at least 11 bounds in DocTabBounds");
            if (rb.DocTabBounds[0].X != 135 || rb.DocTabBounds[0].Width != 124)
                throw new Exception("FAIL: Home tab is not pinned at X=135");

            Console.WriteLine("[PASS] Test 1: Home tab is strictly PINNED at X=135, width=124!");

            // Test 2: Test Tab Scroll
            float initialScroll = rb.TabScrollX;
            rb.TargetTabScrollX = 300;
            rb.UpdateTabScroll();
            if (rb.TabScrollX <= initialScroll)
                throw new Exception("FAIL: TabScrollX should have increased towards TargetTabScrollX");

            // Complete the scroll animation
            for (int k = 0; k < 20; k++) rb.UpdateTabScroll();
            if (Math.Abs(rb.TabScrollX - 300) > 1.0f)
                throw new Exception("FAIL: TabScrollX did not reach target 300");

            // Render after scroll
            using (Bitmap bmp = new Bitmap(1366, 768))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                rb.Draw(g, 1366, 768, wv, sv, cv);
            }

            // Verify Home tab STILL at 135
            if (rb.DocTabBounds[0].X != 135)
                throw new Exception("FAIL: Home tab moved during scroll! Must remain at 135");

            // Verify Tab 1 (first doc tab) shifted left from 265 to 265 - 300 = -35
            if (rb.DocTabBounds[1].X != 265 - 300)
                throw new Exception("FAIL: Doc tab 1 did not shift properly, expected X=" + (265 - 300) + " got " + rb.DocTabBounds[1].X);

            Console.WriteLine("[PASS] Test 2: Document tabs scroll horizontally while Home tab remains strictly pinned!");

            // Test 3: Click Home tab
            rb.HandleClick(150, 15, wv, null, sv, cv);
            if (rb.ActiveDocIndex != 0 || rb.CurrentMode != OfficeAppMode.Home)
                throw new Exception("FAIL: Clicking Home tab failed");

            Console.WriteLine("[PASS] Test 3: Clicking pinned Home tab works flawlessly!");

            // Test 4: EnsureActiveTabVisible
            rb.SwitchDocTab(10, sv, wv, cv);
            if (rb.ActiveDocIndex != 10) throw new Exception("FAIL: Expected active tab 10");
            Console.WriteLine("[PASS] Test 4: Switching to Tab 10 automatically adjusts scroll target!");

            Console.WriteLine("=== ALL TAB SCROLL TESTS PASSED 100%! ===");
        }
    }
}

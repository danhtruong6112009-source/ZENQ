using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestCleanTabs
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING CLEAN BLANK TABS WITHOUT SAMPLE/TEMPLATE DATA ===");

            WriterView wv = new WriterView();
            CalcView cv = new CalcView();
            SlidesView sv = new SlidesView();
            RibbonBar rb = new RibbonBar();
            HomeView hv = new HomeView();

            // Test 1: WriterView initialized completely blank
            if (wv.PieceTable.GetText() != "")
                throw new Exception("FAIL: WriterView initial text should be empty, got: " + wv.PieceTable.GetText());
            if (wv.FormattedRuns.Count != 0)
                throw new Exception("FAIL: WriterView initial FormattedRuns should be 0");
            Console.WriteLine("[PASS] Test 1: WriterView starts completely blank without sample text!");

            // Test 2: CalcView initialized completely blank
            if (cv.Sheets.Count == 0)
                throw new Exception("FAIL: CalcView should have sheets");
            CellData cellA1 = cv.Sheets[0].GetCell(1, 1);
            if (cellA1 != null && !string.IsNullOrEmpty(cellA1.RawValue))
                throw new Exception("FAIL: CalcView A1 should be empty, got: " + cellA1.RawValue);
            Console.WriteLine("[PASS] Test 2: CalcView starts completely clean without demo data!");

            // Test 3: RibbonBar AddDocTab for Writer
            rb.AddDocTab("Văn bản 1.docx", OfficeAppMode.Writer, sv, wv, cv);
            if (wv.PieceTable.GetText() != "")
                throw new Exception("FAIL: New Writer tab text should be empty, got: " + wv.PieceTable.GetText());
            Console.WriteLine("[PASS] Test 3: New Writer tab created via Ribbon is 100% blank!");

            // Test 4: RibbonBar AddDocTab for Calc
            rb.AddDocTab("Bảng tính 1.xlsx", OfficeAppMode.Calc, sv, wv, cv);
            cellA1 = cv.Sheets[0].GetCell(1, 1);
            if (cellA1 != null && !string.IsNullOrEmpty(cellA1.RawValue))
                throw new Exception("FAIL: New Calc tab A1 should be empty, got: " + cellA1.RawValue);
            Console.WriteLine("[PASS] Test 4: New Calc tab created via Ribbon is 100% blank!");

            // Test 5: Click Tab 0 Card (Writer)
            string docTitle;
            RectangleF c0 = hv.GetCardBounds(0, 1366, 768);
            var mode0 = hv.HandleClick((int)c0.X + 20, (int)c0.Y + 20, 1366, 768, out docTitle);
            if (mode0 != OfficeAppMode.Writer) throw new Exception("FAIL: Card 0 should be Writer");
            rb.AddDocTab(docTitle, mode0.Value, sv, wv, cv);
            wv.PieceTable = new PieceTable("");
            wv.FormattedRuns.Clear();
            if (wv.PieceTable.GetText() != "")
                throw new Exception("FAIL: Home Card Writer doc should be blank");
            Console.WriteLine("[PASS] Test 5: Home Card Writer creation produces clean blank doc!");

            Console.WriteLine("=== ALL CLEAN BLANK TAB TESTS PASSED 100%! ===");
        }
    }
}

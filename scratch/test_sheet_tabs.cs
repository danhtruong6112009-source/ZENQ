using System;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestSheets
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Per-Sheet Independent Block Selection and Sheet Tab Management ===");

                CalcView cv = new CalcView();
                cv.PopulateSampleData("TestDoc");

                Console.WriteLine(string.Format("Initial Sheets count: {0} (Active: {1})", cv.Sheets.Count, cv.SheetNames[cv.ActiveSheetIndex]));

                // 1. In Sheet 1 (Index 0): Select range B2:D10
                cv.SelectedRow = 2;
                cv.SelectedCol = 2; // B2
                cv.SelEndRow = 10;
                cv.SelEndCol = 4;   // D10
                cv.CurrentSheet.SetCellText(2, 2, "12345");
                Console.WriteLine("Sheet1: Selected B2:D10 (Row 2..10, Col 2..4)");

                // 2. Switch to Sheet 2 (Index 1)
                cv.SwitchToSheet(1);
                Console.WriteLine(string.Format("Switched to {0}: Selected={1}:{2} to {3}:{4}",
                    cv.SheetNames[cv.ActiveSheetIndex], cv.SelectedRow, cv.SelectedCol, cv.SelEndRow, cv.SelEndCol));

                // In Sheet 2: Select range F5:H20
                cv.SelectedRow = 5;
                cv.SelectedCol = 6; // F5
                cv.SelEndRow = 20;
                cv.SelEndCol = 8;   // H20
                cv.CurrentSheet.SetCellText(5, 6, "99999");
                Console.WriteLine("Sheet2: Selected F5:H20 (Row 5..20, Col 6..8)");

                // 3. Switch to Sheet 3 (Index 2)
                cv.SwitchToSheet(2);
                cv.SelectedRow = 3;
                cv.SelectedCol = 3; // C3
                cv.SelEndRow = 7;
                cv.SelEndCol = 3;   // C7
                Console.WriteLine("Sheet3: Selected C3:C7 (Row 3..7, Col 3..3)");

                // 4. Switch BACK to Sheet 1 (Index 0) and verify selection is preserved!
                cv.SwitchToSheet(0);
                Console.WriteLine(string.Format("Switched back to {0}: Selection is Row {1}..{2}, Col {3}..{4}",
                    cv.SheetNames[cv.ActiveSheetIndex], cv.SelectedRow, cv.SelEndRow, cv.SelectedCol, cv.SelEndCol));

                bool sheet1Preserved = (cv.SelectedRow == 2 && cv.SelectedCol == 2 && cv.SelEndRow == 10 && cv.SelEndCol == 4);
                Console.WriteLine("  -> Sheet 1 selection preserved: " + (sheet1Preserved ? "PASS" : "FAIL"));

                // 5. Switch BACK to Sheet 2 (Index 1) and verify selection is preserved!
                cv.SwitchToSheet(1);
                Console.WriteLine(string.Format("Switched back to {0}: Selection is Row {1}..{2}, Col {3}..{4}",
                    cv.SheetNames[cv.ActiveSheetIndex], cv.SelectedRow, cv.SelEndRow, cv.SelectedCol, cv.SelEndCol));

                bool sheet2Preserved = (cv.SelectedRow == 5 && cv.SelectedCol == 6 && cv.SelEndRow == 20 && cv.SelEndCol == 8);
                Console.WriteLine("  -> Sheet 2 selection preserved: " + (sheet2Preserved ? "PASS" : "FAIL"));

                // 6. Test Add Sheet (+)
                cv.AddSheet();
                cv.SwitchToSheet(cv.Sheets.Count - 1);
                Console.WriteLine(string.Format("Added new sheet: {0} (Total sheets: {1})",
                    cv.SheetNames[cv.ActiveSheetIndex], cv.Sheets.Count));

                // 7. Test Rename Sheet
                cv.RenameSheet(cv.ActiveSheetIndex, "BaoCaoDoanhThu");
                Console.WriteLine("Renamed sheet to: " + cv.SheetNames[cv.ActiveSheetIndex]);
                bool renamePass = (cv.SheetNames[cv.ActiveSheetIndex] == "BaoCaoDoanhThu");
                Console.WriteLine("  -> Rename sheet: " + (renamePass ? "PASS" : "FAIL"));

                // 8. Test Duplicate Sheet
                int prevCount = cv.Sheets.Count;
                cv.DuplicateSheet(0); // Duplicate Sheet1
                Console.WriteLine(string.Format("Duplicated Sheet1. Total sheets: {0}, Current: {1}",
                    cv.Sheets.Count, cv.SheetNames[cv.ActiveSheetIndex]));
                bool dupPass = (cv.Sheets.Count == prevCount + 1 && cv.CurrentSheet.GetCell(2, 2) != null && cv.CurrentSheet.GetCell(2, 2).RawValue == "12345");
                Console.WriteLine("  -> Duplicate sheet & cell data: " + (dupPass ? "PASS" : "FAIL"));

                // 9. Test Delete Sheet
                cv.DeleteSheet(cv.ActiveSheetIndex);
                Console.WriteLine(string.Format("Deleted duplicated sheet. Total sheets now: {0}", cv.Sheets.Count));

                if (sheet1Preserved && sheet2Preserved && renamePass && dupPass)
                {
                    Console.WriteLine("\n=== ALL_PER_SHEET_SELECTION_AND_TAB_TESTS_PASSED_SUCCESSFULLY ===");
                }
                else
                {
                    Console.WriteLine("\n=== TEST_FAILED ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}

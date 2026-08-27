using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestCalcRealFeatures
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Calc Real Features & Security Protection ===");

                CalcView cv = new CalcView();

                // ----------------------------------------------------
                // 1. Test Real Sort (A-Z & Z-A) with contiguous range
                // ----------------------------------------------------
                Console.WriteLine("--- 1. Testing SortSelectedRange ---");
                cv.CurrentSheet.SetCellText(1, 1, "Tên");
                cv.CurrentSheet.SetCellText(1, 2, "Doanh Thu");
                cv.CurrentSheet.SetCellText(2, 1, "Chi nhánh C");
                cv.CurrentSheet.SetCellText(2, 2, "300");
                cv.CurrentSheet.SetCellText(3, 1, "Chi nhánh A");
                cv.CurrentSheet.SetCellText(3, 2, "100");
                cv.CurrentSheet.SetCellText(4, 1, "Chi nhánh B");
                cv.CurrentSheet.SetCellText(4, 2, "200");

                // Cursor on A2 (single cell) -> Sort A-Z by Name
                cv.SelectedRow = 2; cv.SelectedCol = 1;
                cv.SelEndRow = 2; cv.SelEndCol = 1;
                cv.SortSelectedRange(true);

                string row2Name = cv.CurrentSheet.GetCell(2, 1).RawValue;
                string row3Name = cv.CurrentSheet.GetCell(3, 1).RawValue;
                string row4Name = cv.CurrentSheet.GetCell(4, 1).RawValue;
                Console.WriteLine(string.Format("Sorted A-Z Names: Row2='{0}', Row3='{1}', Row4='{2}'", row2Name, row3Name, row4Name));
                bool sortAZPass = (row2Name == "Chi nhánh A" && row3Name == "Chi nhánh B" && row4Name == "Chi nhánh C");

                // Cursor on B2 (single cell) -> Sort Z-A by Revenue
                cv.SelectedRow = 2; cv.SelectedCol = 2;
                cv.SelEndRow = 2; cv.SelEndCol = 2;
                cv.SortSelectedRange(false);

                double row2Rev = cv.CurrentSheet.GetCell(2, 2).NumericValue;
                double row3Rev = cv.CurrentSheet.GetCell(3, 2).NumericValue;
                double row4Rev = cv.CurrentSheet.GetCell(4, 2).NumericValue;
                Console.WriteLine(string.Format("Sorted Z-A Revenue: Row2={0}, Row3={1}, Row4={2}", row2Rev, row3Rev, row4Rev));
                bool sortZAPass = (row2Rev == 300 && row3Rev == 200 && row4Rev == 100);

                // ----------------------------------------------------
                // 2. Test Real Remove Duplicates
                // ----------------------------------------------------
                Console.WriteLine("--- 2. Testing RemoveDuplicates ---");
                // Insert a duplicate of Row 2 at Row 5
                cv.CurrentSheet.SetCellText(5, 1, cv.CurrentSheet.GetCell(2, 1).RawValue);
                cv.CurrentSheet.SetCellText(5, 2, cv.CurrentSheet.GetCell(2, 2).RawValue);
                // Also another duplicate at Row 6
                cv.CurrentSheet.SetCellText(6, 1, cv.CurrentSheet.GetCell(2, 1).RawValue);
                cv.CurrentSheet.SetCellText(6, 2, cv.CurrentSheet.GetCell(2, 2).RawValue);

                cv.SelectedRow = 1; cv.SelectedCol = 1;
                cv.SelEndRow = 6; cv.SelEndCol = 2;
                cv.RemoveDuplicates(null);

                // After removing 2 duplicates, rows 5 and 6 should no longer have duplicate data
                bool dupRemovedPass = (cv.CurrentSheet.GetCell(5, 1) == null || string.IsNullOrEmpty(cv.CurrentSheet.GetCell(5, 1).RawValue));
                Console.WriteLine("Duplicate removal check: " + (dupRemovedPass ? "PASS" : "FAIL"));

                // ----------------------------------------------------
                // 3. Test Real Protect Sheet
                // ----------------------------------------------------
                Console.WriteLine("--- 3. Testing Sheet Protection ---");
                cv.IsSheetProtected = true;
                cv.ProtectPassword = "123";

                // Attempt to edit cell -> should be blocked
                cv.StartEditingCell("Hacked");
                bool editBlocked = !cv.IsEditing;
                Console.WriteLine("StartEditingCell while protected: Blocked=" + editBlocked);

                // Attempt to type char -> should be blocked
                cv.HandleChar('x');
                bool charBlocked = !cv.IsEditing;
                Console.WriteLine("HandleChar while protected: Blocked=" + charBlocked);

                // Attempt to delete -> should be blocked
                cv.HandleKeyDown(Keys.Delete);
                bool cellStillExists = (cv.CurrentSheet.GetCell(2, 1) != null);
                Console.WriteLine("Delete key while protected: Blocked=" + cellStillExists);

                // Unprotect
                cv.IsSheetProtected = false;
                cv.StartEditingCell("Normal Edit");
                bool editAllowedNow = cv.IsEditing;
                cv.IsEditing = false;
                Console.WriteLine("StartEditingCell after unprotect: Allowed=" + editAllowedNow);
                bool protectPass = editBlocked && charBlocked && cellStillExists && editAllowedNow;

                // ----------------------------------------------------
                // 4. Test Real Filter (ToggleFilter)
                // ----------------------------------------------------
                Console.WriteLine("--- 4. Testing ToggleFilter ---");
                cv.SelectedRow = 1; cv.SelectedCol = 1;
                cv.SelEndRow = 1; cv.SelEndCol = 1;
                cv.ToggleFilter();
                bool tableCreated = (cv.Tables != null && cv.Tables.Count > 0);
                Console.WriteLine("ToggleFilter created Table: " + tableCreated);

                if (sortAZPass && sortZAPass && dupRemovedPass && protectPass && tableCreated)
                {
                    Console.WriteLine("\n=== ALL_REAL_FEATURES_TESTS_PASSED_SUCCESSFULLY ===");
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

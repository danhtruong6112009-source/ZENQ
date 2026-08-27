using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestExcelTableFeatures
    {
        public static void Main()
        {
            Console.WriteLine("=== STARTING EXCEL TABLE & CURSOR VERIFICATION ===");

            // 1. Initialize CalcView
            CalcView calc = new CalcView();
            calc.ActiveSheetIndex = 1; // Use empty Sheet2
            SparseMatrix sheet = calc.CurrentSheet;

            // 2. Test Cursors
            Cursor downCur = CalcView.ExcelDownArrowCursor;
            Cursor rightCur = CalcView.ExcelRightArrowCursor;
            Cursor diagCur = CalcView.ExcelDiagArrowCursor;

            if (downCur == null || rightCur == null || diagCur == null)
            {
                throw new Exception("Custom cursors failed to initialize!");
            }
            Console.WriteLine("[PASS] Custom Cursors initialized: Down=" + downCur.Handle + ", Right=" + rightCur.Handle + ", Diag=" + diagCur.Handle);

            // 3. Test Full Column and Full Row Selection
            // Clicking Column Header 'B' (col 2):
            int headerW = 48;
            int colBX = headerW + calc.GetColWidth(1) + 10;
            calc.HandleMouseDown(colBX, 155 + 30 + 10, 1000, 700, MouseButtons.Left);
            if (calc.SelectedCol != 2 || calc.SelEndCol != 2 || calc.SelectedRow != 1 || calc.SelEndRow != 1000)
            {
                throw new Exception("Full Column Selection failed! Col=" + calc.SelectedCol + ", EndRow=" + calc.SelEndRow);
            }
            Console.WriteLine("[PASS] Full Column Selection (Column B): Row 1..1000, Col 2");

            // Clicking Row Header '5' (row 5):
            int row5Y = 155 + 30 + 26 + calc.GetRowHeight(1) * 4 + 10;
            calc.HandleMouseDown(20, row5Y, 1000, 700, MouseButtons.Left);
            if (calc.SelectedRow != 5 || calc.SelEndRow != 5 || calc.SelectedCol != 1 || calc.SelEndCol != 250)
            {
                throw new Exception("Full Row Selection failed! Row=" + calc.SelectedRow + ", EndCol=" + calc.SelEndCol);
            }
            Console.WriteLine("[PASS] Full Row Selection (Row 5): Row 5, Col 1..250");

            // 4. Test Table Creation & Auto-Expansion on Empty Sheet
            calc.SelectedRow = 3; calc.SelEndRow = 6;
            calc.SelectedCol = 2; calc.SelEndCol = 2;
            calc.CreateTableFromRange("B3:B6", true);

            if (calc.Tables.Count == 0) throw new Exception("Table was not created!");
            ExcelTableInfo tbl = calc.Tables[0];
            Console.WriteLine("[PASS] Table Created: " + tbl.Name + " Range: R" + tbl.StartRow + "C" + tbl.StartCol + " to R" + tbl.EndRow + "C" + tbl.EndCol);

            // Auto-expand table horizontally to column 5 (adds Column 2, Column 3, Column 4)
            calc.AutoExpandTable(tbl, 6, 5);
            if (tbl.EndCol != 5) throw new Exception("AutoExpandTable failed to expand columns to 5!");
            
            string h1 = sheet.GetCell(3, 2).GetDisplayText();
            string h2 = sheet.GetCell(3, 3).GetDisplayText();
            string h3 = sheet.GetCell(3, 4).GetDisplayText();
            string h4 = sheet.GetCell(3, 5).GetDisplayText();

            Console.WriteLine("Table Headers: [" + h1 + ", " + h2 + ", " + h3 + ", " + h4 + "]");
            if (h1 != "Column1" || h2 != "Column2" || h3 != "Column3" || h4 != "Column4")
            {
                throw new Exception("Auto-generated column names incorrect! Expected Column1, Column2, Column3, Column4");
            }
            Console.WriteLine("[PASS] Table Auto-Expansion verified: Auto-generated Column1, Column2, Column3, Column4 successfully!");

            // 5. Test Delete Key
            sheet.SetCellText(4, 2, "Test Value 123");
            calc.SelectedRow = 4; calc.SelEndRow = 4; calc.SelectedCol = 2; calc.SelEndCol = 2;
            calc.HandleKeyDown(Keys.Delete);
            CellData clearedCell = sheet.GetCell(4, 2);
            if (clearedCell != null && !string.IsNullOrEmpty(clearedCell.GetDisplayText()))
            {
                throw new Exception("ClearSelectedCells / Delete key failed to clear cell value!");
            }
            Console.WriteLine("[PASS] Delete key clears selected cells correctly.");

            // 6. Test Ribbon Contextual Tab "Table Design"
            RibbonBar ribbon = new RibbonBar();
            ribbon.CurrentMode = OfficeAppMode.Calc;
            ribbon.ActiveDocIndex = 0;
            ribbon.DocTabs.Add(new DocTab("Sheet2.xlsx", OfficeAppMode.Calc, true));

            calc.SelectedRow = 3; calc.SelectedCol = 2; calc.SelEndRow = 3; calc.SelEndCol = 2;
            using (Bitmap b = new Bitmap(1200, 200))
            using (Graphics g = Graphics.FromImage(b))
            {
                ribbon.Draw(g, 1200, 200, null, null, calc);
            }

            bool hasTableDesignTab = false;
            for (int i = 0; i < ribbon.RibbonTabs.Length; i++)
            {
                if (ribbon.RibbonTabs[i] == "Thiết kế Bảng")
                {
                    hasTableDesignTab = true;
                    break;
                }
            }

            if (!hasTableDesignTab)
            {
                throw new Exception("Thiết kế Bảng tab not present when inside table!");
            }
            Console.WriteLine("[PASS] Contextual 'Thiết kế Bảng' ribbon tab successfully activated!");

            // 7. Test Table Total Row toggle
            calc.ToggleTableTotalRow(tbl);
            if (!tbl.HasTotalRow) throw new Exception("HasTotalRow not true after toggle!");
            Console.WriteLine("[PASS] Table Total Row toggled and calculated successfully.");

            Console.WriteLine("=== ALL VERIFICATION TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

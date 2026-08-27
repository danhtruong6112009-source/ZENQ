using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestCellCursor
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING EXCEL CELL SELECT CURSOR ===");

            CalcView calc = new CalcView();
            Cursor cellCur = CalcView.ExcelCellSelectCursor;

            if (cellCur == null)
            {
                throw new Exception("FAIL: ExcelCellSelectCursor is null!");
            }
            Console.WriteLine("[PASS] ExcelCellSelectCursor created successfully: Handle=" + cellCur.Handle);

            // Test cursor at grid cell (e.g. x = 200, y = 300)
            Cursor curAtCell = calc.GetCursorForPosition(200, 300, 1000, 700);
            if (curAtCell != CalcView.ExcelCellSelectCursor)
            {
                throw new Exception("FAIL: Cursor over grid cell is not ExcelCellSelectCursor! Got: " + curAtCell);
            }
            Console.WriteLine("[PASS] Grid cell position (200, 300) returns ExcelCellSelectCursor (+)");

            // Test cursor at column header (e.g. x = 200, y = 155 + 30 + 10 = 195)
            Cursor curAtColHdr = calc.GetCursorForPosition(200, 195, 1000, 700);
            if (curAtColHdr != CalcView.ExcelDownArrowCursor)
            {
                throw new Exception("FAIL: Cursor over column header is not ExcelDownArrowCursor! Got: " + curAtColHdr);
            }
            Console.WriteLine("[PASS] Column header position returns ExcelDownArrowCursor (↓)");

            // Test cursor at row header (e.g. x = 20, y = 300)
            Cursor curAtRowHdr = calc.GetCursorForPosition(20, 300, 1000, 700);
            if (curAtRowHdr != CalcView.ExcelRightArrowCursor)
            {
                throw new Exception("FAIL: Cursor over row header is not ExcelRightArrowCursor! Got: " + curAtRowHdr);
            }
            Console.WriteLine("[PASS] Row header position returns ExcelRightArrowCursor (→)");

            Console.WriteLine("=== ALL CELL CURSOR TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

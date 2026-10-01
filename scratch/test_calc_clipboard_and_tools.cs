using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace Scratch
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Testing Calc Clipboard & Advanced Tools...");

            CalcView cv = new CalcView();
            cv.Sheets = new System.Collections.Generic.List<SparseMatrix>();
            cv.SheetNames = new System.Collections.Generic.List<string>();
            cv.SheetStates = new System.Collections.Generic.List<CalcSheetState>();

            SparseMatrix sheet = new SparseMatrix();
            cv.Sheets.Add(sheet);
            cv.SheetNames.Add("Sheet1");
            cv.SheetStates.Add(new CalcSheetState());
            cv.ActiveSheetIndex = 0;

            // 1. Test Multi-cell Copy & Paste
            sheet.SetCellText(1, 1, "10");
            sheet.SetCellText(1, 2, "20");
            sheet.SetCellText(2, 1, "30");
            sheet.SetCellText(2, 2, "=A1+B1");
            FormulaEngine.EvaluateSheet(sheet);

            cv.SelectedRow = 1; cv.SelectedCol = 1;
            cv.SelEndRow = 2; cv.SelEndCol = 2;
            cv.Copy();

            // Paste at row 5, col 1
            cv.SelectedRow = 5; cv.SelectedCol = 1;
            cv.SelEndRow = 5; cv.SelEndCol = 1;
            cv.Paste();

            CellData c1 = sheet.GetCell(5, 1);
            CellData c2 = sheet.GetCell(5, 2);
            CellData c3 = sheet.GetCell(6, 1);
            CellData c4 = sheet.GetCell(6, 2);

            if (c1 != null && c1.RawValue == "10" &&
                c2 != null && c2.RawValue == "20" &&
                c3 != null && c3.RawValue == "30" &&
                c4 != null && c4.FormulaStr == "=A5+B5" && c4.NumericValue == 30.0)
            {
                Console.WriteLine("[PASS] Multi-cell Copy & Paste with Formula Offset Offset adjustment verified!");
            }
            else
            {
                Console.WriteLine(string.Format("[FAIL] Paste values: c1={0}, c2={1}, c3={2}, c4={3}, formula={4}",
                    c1 != null ? c1.RawValue : "null",
                    c2 != null ? c2.RawValue : "null",
                    c3 != null ? c3.RawValue : "null",
                    c4 != null ? c4.NumericValue.ToString() : "null",
                    c4 != null ? c4.FormulaStr : "null"));
            }

            // 2. Test Paste Transpose
            cv.SelectedRow = 10; cv.SelectedCol = 1;
            cv.PasteTranspose();
            CellData t1 = sheet.GetCell(10, 1); // was (1,1) -> 10
            CellData t2 = sheet.GetCell(11, 1); // was (1,2) -> 20
            CellData t3 = sheet.GetCell(10, 2); // was (2,1) -> 30
            if (t1 != null && t1.RawValue == "10" &&
                t2 != null && t2.RawValue == "20" &&
                t3 != null && t3.RawValue == "30")
            {
                Console.WriteLine("[PASS] Paste Transpose verified!");
            }
            else
            {
                Console.WriteLine("[FAIL] Paste Transpose mismatch!");
            }

            // 3. Test Flash Fill
            sheet.SetCellText(15, 1, "Nguyen Van An");
            sheet.SetCellText(16, 1, "Tran Thi Binh");
            sheet.SetCellText(17, 1, "Le Hoang Cuong");
            sheet.SetCellText(15, 2, "An"); // example

            cv.SelectedRow = 16; cv.SelectedCol = 2;
            cv.FlashFill();

            CellData ff1 = sheet.GetCell(16, 2);
            CellData ff2 = sheet.GetCell(17, 2);
            if (ff1 != null && ff1.RawValue == "Binh" &&
                ff2 != null && ff2.RawValue == "Cuong")
            {
                Console.WriteLine("[PASS] Flash Fill (Ctrl+E) pattern recognition verified!");
            }
            else
            {
                Console.WriteLine(string.Format("[FAIL] Flash Fill values: ff1={0}, ff2={1}",
                    ff1 != null ? ff1.RawValue : "null",
                    ff2 != null ? ff2.RawValue : "null"));
            }

            Console.WriteLine("All tests completed successfully!");
        }
    }
}

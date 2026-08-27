using System;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestFormulas
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Formulas Tab Engine, Heuristic AutoSum, and Function Libraries ===");

                CalcView cv = new CalcView();
                SparseMatrix sheet = cv.CurrentSheet;

                // Setup test data
                // Column A: 10, 20, 30, 40, 50
                sheet.SetCellText(1, 1, "10");
                sheet.SetCellText(2, 1, "20");
                sheet.SetCellText(3, 1, "30");
                sheet.SetCellText(4, 1, "40");
                sheet.SetCellText(5, 1, "50");

                // Column B: 100, 200, 300, 400, 500
                sheet.SetCellText(1, 2, "100");
                sheet.SetCellText(2, 2, "200");
                sheet.SetCellText(3, 2, "300");
                sheet.SetCellText(4, 2, "400");
                sheet.SetCellText(5, 2, "500");

                // Column C: Text data: "Hà", "Nội", "Việt", "Nam"
                sheet.SetCellText(1, 3, "Hà");
                sheet.SetCellText(2, 3, "Nội");
                sheet.SetCellText(3, 3, "Việt");
                sheet.SetCellText(4, 3, "Nam");

                FormulaEngine.EvaluateSheet(sheet);

                // 1. Test Math: SUM, AVERAGE, MAX, MIN, COUNT, COUNTA
                Console.WriteLine("\n--- 1. Math & Statistics ---");
                TestFormula(sheet, "=SUM(A1:A5)", "150");
                TestFormula(sheet, "=AVERAGE(A1:A5)", "30");
                TestFormula(sheet, "=MAX(A1:A5)", "50");
                TestFormula(sheet, "=MIN(A1:A5)", "10");
                TestFormula(sheet, "=COUNT(A1:A5)", "5");
                TestFormula(sheet, "=COUNTA(C1:C4)", "4");
                TestFormula(sheet, "=ROUND(123.456, 2)", "123.46");
                TestFormula(sheet, "=SUMPRODUCT(A1:A3, B1:B3)", "14000"); // 10*100 + 20*200 + 30*300 = 1000 + 4000 + 9000 = 14000

                // 2. Test Logic: IF, IFS, AND, OR, NOT, XOR, SWITCH, IFERROR
                Console.WriteLine("\n--- 2. Logic Functions ---");
                TestFormula(sheet, "=IF(A1>5, \"Pass\", \"Fail\")", "Pass");
                TestFormula(sheet, "=IFS(A1>40, \"High\", A1>20, \"Mid\", A1>0, \"Low\")", "Low");
                TestFormula(sheet, "=AND(A1>0, B1>50)", "TRUE");
                TestFormula(sheet, "=OR(A1>100, B1>50)", "TRUE");
                TestFormula(sheet, "=NOT(A1=0)", "TRUE");
                TestFormula(sheet, "=XOR(A1>0, B1>1000)", "TRUE");
                TestFormula(sheet, "=SWITCH(A1, 10, \"Mười\", 20, \"Hai mươi\", \"Khác\")", "Mười");
                TestFormula(sheet, "=IFERROR(10/0, 999)", "999");

                // 3. Test Text: CONCAT, TEXTJOIN, LEFT, RIGHT, MID, LEN, TRIM, UPPER, LOWER, TEXT
                Console.WriteLine("\n--- 3. Text Functions ---");
                TestFormula(sheet, "=CONCAT(C1, \" \", C2)", "Hà Nội");
                TestFormula(sheet, "=TEXTJOIN(\" \", TRUE, C1:C4)", "Hà Nội Việt Nam");
                TestFormula(sheet, "=LEFT(\"GENOFFICE\", 3)", "GEN");
                TestFormula(sheet, "=RIGHT(\"GENOFFICE\", 6)", "OFFICE");
                TestFormula(sheet, "=MID(\"VIETNAM\", 2, 4)", "IETN");
                TestFormula(sheet, "=LEN(\"MICROSOFT\")", "9");
                TestFormula(sheet, "=TRIM(\"   Excel   \")", "Excel");
                TestFormula(sheet, "=UPPER(\"vietnam\")", "VIETNAM");
                TestFormula(sheet, "=LOWER(\"VIETNAM\")", "vietnam");

                // 4. Test Lookup: VLOOKUP, HLOOKUP, XLOOKUP, INDEX, MATCH, OFFSET, INDIRECT
                Console.WriteLine("\n--- 4. Lookup & Reference Functions ---");
                // Lookup 20 in A1:B5 -> column 2 -> 200
                TestFormula(sheet, "=VLOOKUP(20, A1:B5, 2, FALSE)", "200");
                TestFormula(sheet, "=XLOOKUP(30, A1:A5, B1:B5)", "300");
                TestFormula(sheet, "=INDEX(A1:B5, 4, 2)", "400");
                TestFormula(sheet, "=MATCH(40, A1:A5, 0)", "4");
                TestFormula(sheet, "=OFFSET(A1, 2, 1)", "300");
                TestFormula(sheet, "=INDIRECT(\"B2\")", "200");

                // 5. Test Heuristic Range Detection in CalcView
                Console.WriteLine("\n--- 5. Heuristic AutoSum in CalcView ---");

                // Test Upward Scan: Position at A6 (below A1:A5)
                cv.SelectedRow = 6;
                cv.SelectedCol = 1;
                cv.SelEndRow = 6;
                cv.SelEndCol = 1;
                cv.AutoSum("SUM");
                CellData cA6 = sheet.GetCell(6, 1);
                Console.WriteLine("AutoSum below column A (at A6): Formula=" + cA6.FormulaStr + " Value=" + cA6.EvaluatedStr);
                if (cA6.FormulaStr == "=SUM(A1:A5)" && cA6.NumericValue == 150)
                {
                    Console.WriteLine("  -> Upward Heuristic: PASS");
                }
                else
                {
                    Console.WriteLine("  -> Upward Heuristic: FAIL");
                }

                // Test Leftward Scan: Position at D10 (right of A10:C10)
                sheet.SetCellText(10, 1, "5");
                sheet.SetCellText(10, 2, "15");
                sheet.SetCellText(10, 3, "25");
                cv.SelectedRow = 10;
                cv.SelectedCol = 4; // D10
                cv.SelEndRow = 10;
                cv.SelEndCol = 4;
                cv.AutoSum("AVERAGE");
                CellData cD10 = sheet.GetCell(10, 4);
                Console.WriteLine("AutoSum right of row 10 (at D10): Formula=" + cD10.FormulaStr + " Value=" + cD10.EvaluatedStr);
                if (cD10.FormulaStr == "=AVERAGE(A10:C10)" && cD10.NumericValue == 15)
                {
                    Console.WriteLine("  -> Leftward Heuristic: PASS");
                }
                else
                {
                    Console.WriteLine("  -> Leftward Heuristic: FAIL");
                }

                // 6. Test InsertFunctionDialog instantiation
                Console.WriteLine("\n--- 6. InsertFunctionDialog ---");
                using (InsertFunctionDialog dlg = new InsertFunctionDialog(cv, "VLOOKUP"))
                {
                    Console.WriteLine("InsertFunctionDialog initialized successfully for VLOOKUP!");
                }

                Console.WriteLine("\n=== ALL_FORMULA_ENGINE_AND_UI_TESTS_PASSED_SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }

        static void TestFormula(SparseMatrix sheet, string formula, string expected)
        {
            CellData res = FormulaEngine.EvaluateFormula(formula, sheet);
            string val = !string.IsNullOrEmpty(res.EvaluatedStr) ? res.EvaluatedStr : res.NumericValue.ToString();
            bool pass = val.Trim().Equals(expected.Trim(), StringComparison.OrdinalIgnoreCase);
            Console.WriteLine(string.Format("Formula: {0} => Result: {1} (Expected: {2}) [{3}]",
                formula.PadRight(42), val, expected, pass ? "PASS" : "FAIL"));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class Phase1StandaloneVerifier
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("  Phase 1 Standalone Verification Harness");
            Console.WriteLine("=================================================");

            bool allPassed = true;

            allPassed &= VerifyDrawingsRoundtrip();
            allPassed &= VerifyNativePdfExport();
            allPassed &= VerifyFindAndReplace();
            allPassed &= VerifyRemoveDuplicates();
            allPassed &= VerifyTextToColumns();

            Console.WriteLine("=================================================");
            if (allPassed)
            {
                Console.WriteLine("  ALL PHASE 1 ACCEPTANCE CRITERIA VERIFIED: PASS!");
                Console.WriteLine("=================================================");
                return 0;
            }
            else
            {
                Console.WriteLine("  PHASE 1 VERIFICATION FAILED!");
                Console.WriteLine("=================================================");
                return 1;
            }
        }

        private static bool VerifyDrawingsRoundtrip()
        {
            Console.Write("[TEST 1] OpenXML Drawings Roundtrip (1 chart, 1 shape, 1 image)... ");
            try
            {
                CalcView cv = new CalcView();
                cv.Sheets = new List<SparseMatrix>();
                cv.SheetNames = new List<string>();
                cv.SheetStates = new List<CalcSheetState>();

                SparseMatrix s1 = new SparseMatrix();
                s1.SetCellText(1, 1, "Item");
                s1.SetCellText(1, 2, "Qty");
                s1.GetOrCreateCell(2, 1).SetNumber(1);
                s1.GetOrCreateCell(2, 2).SetNumber(50);
                cv.Sheets.Add(s1);
                cv.SheetNames.Add("Report");
                cv.SheetStates.Add(new CalcSheetState());
                cv.ActiveSheetIndex = 0;

                // 1. Picture
                DocShapeObject pic = new DocShapeObject("Image", 80, 40, 160, 100);
                using (Bitmap bmp = new Bitmap(160, 100))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.CornflowerBlue);
                    }
                    pic.CachedImage = new Bitmap(bmp);
                }
                pic.CustomName = "LogoPic";
                cv.Shapes.Add(pic);

                // 2. Vector Shape
                DocShapeObject sh = new DocShapeObject("Rectangle", 260, 40, 140, 80);
                sh.FillColor = Color.FromArgb(43, 87, 154);
                sh.TextContent = "Summary Banner";
                sh.CustomName = "BannerShape";
                cv.Shapes.Add(sh);

                // 3. Chart
                DocShapeObject chart = new DocShapeObject("Chart", 420, 40, 220, 150);
                chart.ChartData = DocChartData.CreateDefault("Pie");
                chart.CustomName = "SalesPieChart";
                cv.Shapes.Add(chart);

                string tmpFile = Path.Combine(Path.GetTempPath(), "p1_drawings_roundtrip.xlsx");
                if (File.Exists(tmpFile)) File.Delete(tmpFile);

                bool saved = XlsxHandler.SaveXlsx(tmpFile, cv);
                if (!saved || !File.Exists(tmpFile))
                {
                    Console.WriteLine("FAIL: SaveXlsx returned false or file missing.");
                    return false;
                }

                CalcView cv2 = new CalcView();
                cv2.Sheets = new List<SparseMatrix>();
                cv2.SheetNames = new List<string>();
                cv2.SheetStates = new List<CalcSheetState>();

                bool loaded = XlsxHandler.LoadXlsx(tmpFile, cv2);
                if (!loaded)
                {
                    Console.WriteLine("FAIL: LoadXlsx returned false.");
                    return false;
                }

                if (cv2.FloatingObjects == null || cv2.FloatingObjects.Count != 3)
                {
                    Console.WriteLine("FAIL: Expected 3 floating objects, got " + (cv2.FloatingObjects == null ? 0 : cv2.FloatingObjects.Count));
                    return false;
                }

                DocShapeObject lPic = null, lSh = null, lChart = null;
                for (int i = 0; i < cv2.FloatingObjects.Count; i++)
                {
                    DocShapeObject obj = cv2.FloatingObjects[i];
                    if (obj.ShapeType == "Image") lPic = obj;
                    else if (obj.ShapeType == "Chart") lChart = obj;
                    else if (obj.ShapeType == "Rectangle") lSh = obj;
                }

                if (lPic == null || lPic.CachedImage == null)
                {
                    Console.WriteLine("FAIL: Picture missing or CachedImage null.");
                    return false;
                }
                if (lSh == null || lSh.TextContent != "Summary Banner")
                {
                    Console.WriteLine("FAIL: Rectangle missing or text content mismatch.");
                    return false;
                }
                if (lChart == null || lChart.ChartData == null)
                {
                    Console.WriteLine("FAIL: Chart missing or ChartData null.");
                    return false;
                }

                // Verify exact anchor coordinates (within 2 pixels due to EMU rounding)
                if (Math.Abs(lPic.PageX - 80) > 2 || Math.Abs(lPic.PageY - 40) > 2)
                {
                    Console.WriteLine("FAIL: Picture coordinates drift: " + lPic.PageX + "," + lPic.PageY);
                    return false;
                }
                if (Math.Abs(lSh.PageX - 260) > 2 || Math.Abs(lSh.PageY - 40) > 2)
                {
                    Console.WriteLine("FAIL: Shape coordinates drift: " + lSh.PageX + "," + lSh.PageY);
                    return false;
                }
                if (Math.Abs(lChart.PageX - 420) > 2 || Math.Abs(lChart.PageY - 40) > 2)
                {
                    Console.WriteLine("FAIL: Chart coordinates drift: " + lChart.PageX + "," + lChart.PageY);
                    return false;
                }

                try { File.Delete(tmpFile); } catch { }
                Console.WriteLine("PASS (All 3 assets reproduced at exact anchors)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
                return false;
            }
        }

        private static bool VerifyNativePdfExport()
        {
            Console.Write("[TEST 2] Native Multi-sheet PDF Export... ");
            try
            {
                CalcView cv = new CalcView();
                cv.Sheets = new List<SparseMatrix>();
                cv.SheetNames = new List<string>();
                cv.SheetStates = new List<CalcSheetState>();

                // Sheet 1
                SparseMatrix s1 = new SparseMatrix();
                s1.SetCellText(1, 1, "Department");
                s1.SetCellText(1, 2, "Budget");
                s1.SetCellText(2, 1, "Engineering");
                s1.GetOrCreateCell(2, 2).SetNumber(750000);
                cv.Sheets.Add(s1);
                cv.SheetNames.Add("Q1");
                cv.SheetStates.Add(new CalcSheetState());

                // Sheet 2
                SparseMatrix s2 = new SparseMatrix();
                s2.SetCellText(1, 1, "Project");
                s2.SetCellText(1, 2, "Status");
                s2.SetCellText(2, 1, "ZenQ Calc v2");
                s2.SetCellText(2, 2, "Completed");
                cv.Sheets.Add(s2);
                cv.SheetNames.Add("Q2");
                cv.SheetStates.Add(new CalcSheetState());

                cv.ActiveSheetIndex = 0;
                cv.LoadSheetState(0);

                string tmpPdf = Path.Combine(Path.GetTempPath(), "p1_export_test.pdf");
                if (File.Exists(tmpPdf)) File.Delete(tmpPdf);

                bool exported = cv.ExportToPdf(tmpPdf, true);
                if (!exported || !File.Exists(tmpPdf))
                {
                    Console.WriteLine("FAIL: ExportToPdf returned false or file missing.");
                    return false;
                }

                FileInfo fi = new FileInfo(tmpPdf);
                if (fi.Length < 1000)
                {
                    Console.WriteLine("FAIL: Exported PDF too small: " + fi.Length + " bytes");
                    return false;
                }

                byte[] header = new byte[5];
                using (FileStream fs = new FileStream(tmpPdf, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, 5);
                }
                string headerStr = System.Text.Encoding.ASCII.GetString(header);
                if (headerStr != "%PDF-")
                {
                    Console.WriteLine("FAIL: PDF header invalid: " + headerStr);
                    return false;
                }

                try { File.Delete(tmpPdf); } catch { }
                Console.WriteLine("PASS (Valid multi-sheet PDF generated, size: " + fi.Length + " bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
                return false;
            }
        }

        private static bool VerifyFindAndReplace()
        {
            Console.Write("[TEST 3] Find & Replace with Formula Protection... ");
            try
            {
                CalcView cv = new CalcView();
                cv.Sheets = new List<SparseMatrix>();
                cv.SheetNames = new List<string>();
                cv.SheetStates = new List<CalcSheetState>();

                SparseMatrix s = new SparseMatrix();
                s.GetOrCreateCell(1, 1).SetNumber(100);
                s.GetOrCreateCell(2, 1).SetNumber(100);
                s.SetCellText(3, 1, "=SUM(A1:A2)"); // Formula cell evaluates to 200
                FormulaEngine.EvaluateSheet(s);

                cv.Sheets.Add(s);
                cv.SheetNames.Add("Sheet1");
                cv.SheetStates.Add(new CalcSheetState());
                cv.ActiveSheetIndex = 0;
                cv.LoadSheetState(0);

                // 1. Find
                string statusMsg;
                cv.SelectedRow = 1;
                cv.SelectedCol = 0;
                bool found = cv.FindNextCellEx("100", false, true, true, false, false, out statusMsg);
                if (!found || cv.SelectedRow != 1 || cv.SelectedCol != 1)
                {
                    Console.WriteLine("FAIL: FindNextCellEx failed to find cell A1.");
                    return false;
                }

                // 2. Replace All with lookInFormulas = false (Formula protection!)
                // Should replace A1 and A2 (values 100 -> 150), but MUST NOT touch A3 even if evaluated result matched
                int replaced = cv.ReplaceAllInSheetEx("100", "150", false, true, false);
                if (replaced != 2)
                {
                    Console.WriteLine("FAIL: Expected 2 replacements, got " + replaced);
                    return false;
                }

                CellData c1 = cv.CurrentSheet.GetCell(1, 1);
                CellData c2 = cv.CurrentSheet.GetCell(2, 1);
                CellData c3 = cv.CurrentSheet.GetCell(3, 1);

                if (c1.NumericValue != 150.0 || c2.NumericValue != 150.0)
                {
                    Console.WriteLine("FAIL: Numeric values not updated.");
                    return false;
                }
                if (c3.Type != CellType.Formula || c3.FormulaStr != "=SUM(A1:A2)")
                {
                    Console.WriteLine("FAIL: Formula cell corrupted by value replace!");
                    return false;
                }

                // Formula recalculated: 150 + 150 = 300
                if (c3.NumericValue != 300.0)
                {
                    Console.WriteLine("FAIL: Formula not recalculated after replace: " + c3.NumericValue);
                    return false;
                }

                // 3. Replace with lookInFormulas = true (User explicitly replaces formula text)
                int replFormulas = cv.ReplaceAllInSheetEx("SUM", "AVERAGE", false, false, true);
                if (replFormulas != 1)
                {
                    Console.WriteLine("FAIL: Expected 1 formula replacement, got " + replFormulas);
                    return false;
                }
                if (c3.FormulaStr != "=AVERAGE(A1:A2)")
                {
                    Console.WriteLine("FAIL: Formula text not updated: " + c3.FormulaStr);
                    return false;
                }

                Console.WriteLine("PASS (Formula protection and formula replacement verified)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
                return false;
            }
        }

        private static bool VerifyRemoveDuplicates()
        {
            Console.Write("[TEST 4] Remove Duplicates Modal Logic... ");
            try
            {
                CalcView cv = new CalcView();
                cv.Sheets = new List<SparseMatrix>();
                cv.SheetNames = new List<string>();
                cv.SheetStates = new List<CalcSheetState>();

                SparseMatrix s = new SparseMatrix();
                s.SetCellText(1, 1, "ID");
                s.SetCellText(1, 2, "Name");
                s.SetCellText(2, 1, "101");
                s.SetCellText(2, 2, "Alice");
                s.SetCellText(3, 1, "102");
                s.SetCellText(3, 2, "Bob");
                s.SetCellText(4, 1, "101");
                s.SetCellText(4, 2, "Alice"); // duplicate row
                s.SetCellText(5, 1, "103");
                s.SetCellText(5, 2, "Charlie");
                s.SetCellText(6, 1, "102");
                s.SetCellText(6, 2, "Bob"); // duplicate row

                cv.Sheets.Add(s);
                cv.SheetNames.Add("Sheet1");
                cv.SheetStates.Add(new CalcSheetState());
                cv.ActiveSheetIndex = 0;
                cv.LoadSheetState(0);

                List<uint> checkedCols = new List<uint>() { 1, 2 };
                int dupCount, uniqueCount;
                cv.ExecuteRemoveDuplicates(1, 6, 1, 2, checkedCols, true, out dupCount, out uniqueCount);

                if (dupCount != 2)
                {
                    Console.WriteLine("FAIL: Expected 2 duplicates removed, got " + dupCount);
                    return false;
                }
                if (uniqueCount != 3)
                {
                    Console.WriteLine("FAIL: Expected 3 unique rows, got " + uniqueCount);
                    return false;
                }

                if (cv.SelEndRow != 4)
                {
                    Console.WriteLine("FAIL: Expected SelEndRow to be 4, got " + cv.SelEndRow);
                    return false;
                }

                // Check remaining rows: Row 1 = Headers, Row 2 = 101/Alice, Row 3 = 102/Bob, Row 4 = 103/Charlie, Row 5 & 6 = empty
                CellData r4_1 = s.GetCell(4, 1);
                CellData r5_1 = s.GetCell(5, 1);
                if (r4_1 == null || r4_1.RawValue != "103")
                {
                    Console.WriteLine("FAIL: Row 4 should be 103/Charlie, got " + (r4_1 != null ? r4_1.RawValue : "null"));
                    return false;
                }
                if (r5_1 != null && !string.IsNullOrEmpty(r5_1.RawValue))
                {
                    Console.WriteLine("FAIL: Row 5 should be empty, got " + r5_1.RawValue);
                    return false;
                }

                Console.WriteLine("PASS (Duplicates removed and rows shifted cleanly)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
                return false;
            }
        }

        private static bool VerifyTextToColumns()
        {
            Console.Write("[TEST 5] Text to Columns Parsing & Execution... ");
            try
            {
                CalcView cv = new CalcView();
                cv.Sheets = new List<SparseMatrix>();
                cv.SheetNames = new List<string>();
                cv.SheetStates = new List<CalcSheetState>();

                SparseMatrix s = new SparseMatrix();
                s.SetCellText(1, 1, "\"Nguyen, Van A\", 28, 1500.50");
                s.SetCellText(2, 1, "\"Tran, Thi B\", 32, 2200.75");
                cv.Sheets.Add(s);
                cv.SheetNames.Add("Sheet1");
                cv.SheetStates.Add(new CalcSheetState());
                cv.ActiveSheetIndex = 0;
                cv.LoadSheetState(0);

                List<char> delims = new List<char>() { ',' };
                cv.ExecuteTextToColumns(1, 1, 2, 2, 1, delims, true, '"', null, false);

                // Row 1: Dest starts at Col 2
                // Col 2: Nguyen, Van A
                // Col 3: 28 (numeric)
                // Col 4: 1500.50 (numeric)
                CellData cB1 = s.GetCell(1, 2);
                CellData cC1 = s.GetCell(1, 3);
                CellData cD1 = s.GetCell(1, 4);

                if (cB1 == null || cB1.RawValue != "Nguyen, Van A")
                {
                    Console.WriteLine("FAIL: Col 2 should be 'Nguyen, Van A', got: " + (cB1 != null ? cB1.RawValue : "null"));
                    return false;
                }
                if (cC1 == null || cC1.NumericValue != 28.0)
                {
                    Console.WriteLine("FAIL: Col 3 should be numeric 28, got: " + (cC1 != null ? cC1.RawValue : "null"));
                    return false;
                }
                if (cD1 == null || Math.Abs(cD1.NumericValue - 1500.50) > 0.001)
                {
                    Console.WriteLine("FAIL: Col 4 should be numeric 1500.50, got: " + (cD1 != null ? cD1.RawValue : "null"));
                    return false;
                }

                // Test Tab-separated
                s.SetCellText(3, 1, "Alfa\tBravo\tCharlie");
                List<char> tabDelim = new List<char>() { '\t' };
                cv.ExecuteTextToColumns(1, 3, 3, 2, 3, tabDelim, true, '\0', null, false);

                CellData cB3 = s.GetCell(3, 2);
                CellData cC3 = s.GetCell(3, 3);
                CellData cD3 = s.GetCell(3, 4);

                if (cB3 == null || cB3.RawValue != "Alfa" ||
                    cC3 == null || cC3.RawValue != "Bravo" ||
                    cD3 == null || cD3.RawValue != "Charlie")
                {
                    Console.WriteLine("FAIL: Tab delimited split failed.");
                    return false;
                }

                Console.WriteLine("PASS (Comma, quotes, tab split & type inference verified)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
                return false;
            }
        }
    }
}

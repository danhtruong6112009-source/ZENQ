using System;
using System.Collections.Generic;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace TestMergeAndDiagonalSplit
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing Excel Merge & Diagonal Split Header Features ===");

                CalcView cv = new CalcView();

                // ----------------------------------------------------
                // 1. Test Merge & Center and Unmerge
                // ----------------------------------------------------
                Console.WriteLine("--- 1. Testing Merge & Center ---");
                cv.CurrentSheet.SetCellText(2, 2, "BẢNG BÁO CÁO TỔNG KẾT DOANH THU");
                cv.SelectedRow = 2; cv.SelectedCol = 2;
                cv.SelEndRow = 4; cv.SelEndCol = 4; // B2:D4 (3 rows x 3 cols)
                cv.MergeAndCenter();

                CellData master = cv.CurrentSheet.GetCell(2, 2);
                bool mergeSuccess = (master != null && master.Format.IsMergedParent && master.Format.MergeRowSpan == 3 && master.Format.MergeColSpan == 3);
                CellData child = cv.CurrentSheet.GetCell(3, 3);
                bool childSuccess = (child != null && child.Format.IsMergedChild && child.Format.MergedParentRow == 2 && child.Format.MergedParentCol == 2);
                Console.WriteLine(string.Format("Merge master: Parent={0}, RowSpan={1}, ColSpan={2}", master.Format.IsMergedParent, master.Format.MergeRowSpan, master.Format.MergeColSpan));
                Console.WriteLine(string.Format("Merge child: Child={0}, ParentRow={1}, ParentCol={2}", child.Format.IsMergedChild, child.Format.MergedParentRow, child.Format.MergedParentCol));

                // ----------------------------------------------------
                // 2. Test Merge Across
                // ----------------------------------------------------
                Console.WriteLine("--- 2. Testing Merge Across ---");
                cv.SelectedRow = 6; cv.SelectedCol = 2;
                cv.SelEndRow = 7; cv.SelEndCol = 4; // B6:D7 -> 2 rows, each merged across 3 cols
                cv.MergeAcross();
                CellData row6Master = cv.CurrentSheet.GetCell(6, 2);
                CellData row7Master = cv.CurrentSheet.GetCell(7, 2);
                bool mergeAcrossSuccess = (row6Master != null && row6Master.Format.IsMergedParent && row6Master.Format.MergeRowSpan == 1 && row6Master.Format.MergeColSpan == 3) &&
                                          (row7Master != null && row7Master.Format.IsMergedParent && row7Master.Format.MergeRowSpan == 1 && row7Master.Format.MergeColSpan == 3);
                Console.WriteLine(string.Format("Merge Across check: {0}", mergeAcrossSuccess ? "PASS" : "FAIL"));

                // ----------------------------------------------------
                // 3. Test Diagonal Split Header (Kẻ chéo ô & 2 tiêu đề)
                // ----------------------------------------------------
                Console.WriteLine("--- 3. Testing Diagonal Split Header ---");
                // Cell A1: Top-Right = "Tháng", Bottom-Left = "Sản phẩm"
                cv.SelectedRow = 1; cv.SelectedCol = 1;
                cv.SelEndRow = 1; cv.SelEndCol = 1;

                CellData diagCell = cv.CurrentSheet.GetOrCreateCell(1, 1);
                diagCell.Format.Borders.DiagonalDown = true;
                diagCell.Format.Borders.DiagonalThickness = 2;
                diagCell.Format.IsDiagonalHeader = true;
                diagCell.Format.DiagonalTopRightText = "Tháng";
                diagCell.Format.DiagonalBottomLeftText = "Sản phẩm";
                diagCell.RawValue = "Tháng\nSản phẩm";
                diagCell.EvaluatedStr = diagCell.RawValue;

                bool diagSuccess = (diagCell.Format.Borders.DiagonalDown && diagCell.Format.IsDiagonalHeader && diagCell.Format.DiagonalTopRightText == "Tháng" && diagCell.Format.DiagonalBottomLeftText == "Sản phẩm");
                Console.WriteLine(string.Format("Diagonal Header: DiagDown={0}, TopRight='{1}', BottomLeft='{2}'", diagCell.Format.Borders.DiagonalDown, diagCell.Format.DiagonalTopRightText, diagCell.Format.DiagonalBottomLeftText));

                // ----------------------------------------------------
                // 4. Test OpenXML .xlsx Export and Import Round-Trip
                // ----------------------------------------------------
                Console.WriteLine("--- 4. Testing .xlsx Export and Import Round-Trip ---");
                string testPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "test_merge_and_diagonal.xlsx");
                if (File.Exists(testPath)) File.Delete(testPath);

                bool saveOk = XlsxHandler.SaveXlsx(testPath, cv);
                Console.WriteLine("SaveXlsx: " + (saveOk ? "SUCCESS" : "FAILED"));

                CalcView loadedCv = new CalcView();
                bool loadOk = XlsxHandler.LoadXlsx(testPath, loadedCv);
                Console.WriteLine("LoadXlsx: " + (loadOk ? "SUCCESS" : "FAILED"));

                // Verify loaded merged cells
                CellData loadedMaster = loadedCv.CurrentSheet.GetCell(2, 2);
                bool loadedMergeOk = (loadedMaster != null && loadedMaster.Format.IsMergedParent && loadedMaster.Format.MergeRowSpan == 3 && loadedMaster.Format.MergeColSpan == 3);
                Console.WriteLine(string.Format("Loaded Merge Master: IsMergedParent={0}, RowSpan={1}, ColSpan={2}", loadedMaster != null ? loadedMaster.Format.IsMergedParent : false, loadedMaster != null ? loadedMaster.Format.MergeRowSpan : 0, loadedMaster != null ? loadedMaster.Format.MergeColSpan : 0));

                // Verify loaded diagonal border & multiline header
                CellData loadedDiagCell = loadedCv.CurrentSheet.GetCell(1, 1);
                bool loadedDiagOk = (loadedDiagCell != null && loadedDiagCell.Format.Borders.DiagonalDown && loadedDiagCell.Format.IsDiagonalHeader && loadedDiagCell.Format.DiagonalTopRightText == "Tháng" && loadedDiagCell.Format.DiagonalBottomLeftText == "Sản phẩm");
                Console.WriteLine(string.Format("Loaded Diag Cell: DiagDown={0}, IsDiagHeader={1}, TopRight='{2}', BottomLeft='{3}'",
                    loadedDiagCell != null ? loadedDiagCell.Format.Borders.DiagonalDown : false,
                    loadedDiagCell != null ? loadedDiagCell.Format.IsDiagonalHeader : false,
                    loadedDiagCell != null ? loadedDiagCell.Format.DiagonalTopRightText : "",
                    loadedDiagCell != null ? loadedDiagCell.Format.DiagonalBottomLeftText : ""));

                if (mergeSuccess && childSuccess && mergeAcrossSuccess && diagSuccess && saveOk && loadOk && loadedMergeOk && loadedDiagOk)
                {
                    Console.WriteLine("\n=== ALL_MERGE_AND_DIAGONAL_TESTS_PASSED_SUCCESSFULLY ===");
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

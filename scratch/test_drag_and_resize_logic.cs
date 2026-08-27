using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestDragAndResizeLogic
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING RANGE SELECTION VS CORNER DRAGGING ===");

            // 1. Initialize
            CalcView calc = new CalcView();
            calc.ActiveSheetIndex = 1; // Empty Sheet2
            SparseMatrix sheet = calc.CurrentSheet;

            // 2. Create Table B3:B6
            calc.SelectedRow = 3; calc.SelEndRow = 6;
            calc.SelectedCol = 2; calc.SelEndCol = 2;
            calc.CreateTableFromRange("B3:B6", true);
            ExcelTableInfo tbl = calc.Tables[0];

            Console.WriteLine("Initial Table: Range R" + tbl.StartRow + "C" + tbl.StartCol + " to R" + tbl.EndRow + "C" + tbl.EndCol);

            // 3. Test Marquee Selection Dragging over Blank Cells
            // Drag selection from B3 to E12
            calc.HandleMouseDown(100, 250, 1000, 700, MouseButtons.Left);
            calc.HandleMouseMove(400, 450, 1000, 700, MouseButtons.Left);
            calc.HandleMouseUp(400, 450, 1000, 700, MouseButtons.Left);

            // Table MUST NOT have expanded!
            if (tbl.EndRow != 6 || tbl.EndCol != 2)
            {
                throw new Exception("FAIL: Marquee selection erroneously expanded the table! Expected EndRow=6, EndCol=2, got EndRow=" + tbl.EndRow + ", EndCol=" + tbl.EndCol);
            }
            Console.WriteLine("[PASS] Normal range selection (quét khối) DOES NOT alter or expand table.");

            // 4. Test Table Corner Resize Dragging
            // Simulate MouseDown on corner handle
            calc.IsDraggingTableResize = true;
            calc.ResizingTable = tbl;
            calc.TableResizeTargetRow = tbl.EndRow;
            calc.TableResizeTargetCol = tbl.EndCol;

            // Simulate MouseMove dragging horizontally to Col 5
            int col5X = 48 + calc.GetColWidth(1) + calc.GetColWidth(2) + calc.GetColWidth(3) + calc.GetColWidth(4) + 10;
            int row6Y = 155 + 30 + 26 + calc.GetRowHeight(1) * 5 + 10;
            calc.HandleMouseMove(col5X, row6Y, 1000, 700, MouseButtons.Left);

            if (calc.TableResizeTargetCol != 5 || calc.TableResizeTargetRow != 6)
            {
                throw new Exception("FAIL: Horizontal corner dragging failed to target Col 5! Got Col=" + calc.TableResizeTargetCol + ", Row=" + calc.TableResizeTargetRow);
            }
            // During drag (before MouseUp), table is NOT modified yet
            if (tbl.EndCol != 2)
            {
                throw new Exception("FAIL: Table expanded before MouseUp!");
            }
            Console.WriteLine("[PASS] Dragging corner horizontally creates preview without modifying table prematurely.");

            // Simulate MouseUp -> Now table expands to Col 5!
            calc.HandleMouseUp(col5X, row6Y, 1000, 700, MouseButtons.Left);
            if (tbl.EndCol != 5 || tbl.EndRow != 6)
            {
                throw new Exception("FAIL: MouseUp failed to expand table to Col 5! Got EndCol=" + tbl.EndCol);
            }
            Console.WriteLine("[PASS] On MouseUp, table successfully expanded horizontally: EndCol=5, headers=" + sheet.GetCell(3, 5).GetDisplayText());

            // Simulate MouseMove dragging vertically to Row 10
            calc.IsDraggingTableResize = true;
            calc.ResizingTable = tbl;
            calc.TableResizeTargetRow = tbl.EndRow;
            calc.TableResizeTargetCol = tbl.EndCol;

            int row10Y = 155 + 30 + 26 + calc.GetRowHeight(1) * 9 + 10;
            calc.HandleMouseMove(col5X, row10Y, 1000, 700, MouseButtons.Left);

            if (calc.TableResizeTargetRow != 10 || calc.TableResizeTargetCol != 5)
            {
                throw new Exception("FAIL: Vertical corner dragging failed to target Row 10! Got Col=" + calc.TableResizeTargetCol + ", Row=" + calc.TableResizeTargetRow);
            }

            // Simulate MouseUp -> Expands to Row 10!
            calc.HandleMouseUp(col5X, row10Y, 1000, 700, MouseButtons.Left);
            if (tbl.EndRow != 10 || tbl.EndCol != 5)
            {
                throw new Exception("FAIL: MouseUp failed to expand table to Row 10! Got EndRow=" + tbl.EndRow);
            }
            Console.WriteLine("[PASS] On MouseUp, table successfully expanded vertically: EndRow=10.");

            Console.WriteLine("=== ALL DRAG & RESIZE TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

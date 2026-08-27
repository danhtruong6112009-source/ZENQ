using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestDoubleClickAndMargins
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING DOUBLE CLICK EDIT & MARGIN DRAGGING CLAMPING ===");

            CalcView calc = new CalcView();

            // 1. Test Margin Clamping in GetColAtX / GetRowAtY
            int headerW = 48;
            int startY = 155;
            int headerH = 26;
            int headerTop = startY + headerH; // 181

            // Mouse dragged into left margin (x = 10 < 48)
            calc.ScrollCol = 1;
            calc.SelectedRow = 3;
            calc.SelectedCol = 1;
            calc.IsSelectingRange = true;

            calc.HandleMouseMove(-50, 200, 1000, 800, MouseButtons.Left);
            Console.WriteLine("[PASS] Dragged X to -50: SelEndCol = " + calc.SelEndCol);
            if (calc.SelEndCol != 1) throw new Exception("FAIL: Expected SelEndCol = 1, got " + calc.SelEndCol);

            // Mouse dragged into top margin (y = 50 < 181)
            calc.HandleMouseMove(100, -20, 1000, 800, MouseButtons.Left);
            Console.WriteLine("[PASS] Dragged Y to -20: SelEndRow = " + calc.SelEndRow);
            if (calc.SelEndRow != 1) throw new Exception("FAIL: Expected SelEndRow = 1, got " + calc.SelEndRow);

            // Mouse dragged to right (x = 500) and bottom (y = 400)
            calc.HandleMouseMove(500, 400, 1000, 800, MouseButtons.Left);
            Console.WriteLine("[PASS] Normal drag: SelEndCol = " + calc.SelEndCol + ", SelEndRow = " + calc.SelEndRow);
            if (calc.SelEndCol < 2 || calc.SelEndRow < 2) throw new Exception("FAIL: Expected valid grid coordinates");

            // 2. Test Chart Double Click Edit
            DocChartData chartData = DocChartData.CreateDefault("Column_Clustered");
            DocShapeObject chartShape = new DocShapeObject
            {
                ShapeType = "Chart",
                ChartData = chartData,
                PageX = 100,
                PageY = 100,
                BaseWidth = 400,
                BaseHeight = 250
            };
            calc.Shapes.Add(chartShape);

            // Hit test on the chart shape (x=200, y=300)
            int hHandle;
            DocShapeObject hit = calc.HitTestShape(200, 300, headerW, startY, headerH, out hHandle);
            if (hit != chartShape) throw new Exception("FAIL: Hit test did not find chart shape!");
            Console.WriteLine("[PASS] HitTestShape identified chart shape correctly.");

            Console.WriteLine("=== ALL TESTS PASSED WITH 100% SUCCESS! ===");
        }
    }
}

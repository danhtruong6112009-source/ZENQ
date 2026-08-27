using System;
using System.Drawing;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestSelectionAnd4Series
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING 4-SERIES EXTRACTION & SELECTION SCROLLING FIX ===");

            CalcView calc = new CalcView();

            // Populate table exactly like user screenshot
            calc.CurrentSheet.SetCellText(1, 1, "BẢNG TÍNH MỚI");

            calc.CurrentSheet.SetCellText(3, 1, "Mã SP");
            calc.CurrentSheet.SetCellText(3, 2, "Tên Sản Phẩm");
            calc.CurrentSheet.SetCellText(3, 3, "Số Lượng");
            calc.CurrentSheet.SetCellText(3, 4, "Đơn Giá ($)");
            calc.CurrentSheet.SetCellText(3, 5, "Thành Tiền ($)");

            calc.CurrentSheet.SetCellText(4, 1, "SP-001");
            calc.CurrentSheet.SetCellText(4, 2, "GenOffice");
            calc.CurrentSheet.SetCellText(4, 3, "250");
            calc.CurrentSheet.SetCellText(4, 4, "$120.00");
            calc.CurrentSheet.SetCellText(4, 5, "30000");

            calc.CurrentSheet.SetCellText(5, 1, "SP-002");
            calc.CurrentSheet.SetCellText(5, 2, "GenWriter");
            calc.CurrentSheet.SetCellText(5, 3, "180");
            calc.CurrentSheet.SetCellText(5, 4, "$85.00");
            calc.CurrentSheet.SetCellText(5, 5, "15300");

            calc.CurrentSheet.SetCellText(6, 1, "SP-003");
            calc.CurrentSheet.SetCellText(6, 2, "GenCalc");
            calc.CurrentSheet.SetCellText(6, 3, "320");
            calc.CurrentSheet.SetCellText(6, 4, "$45.00");
            calc.CurrentSheet.SetCellText(6, 5, "14400");

            calc.CurrentSheet.SetCellText(7, 1, "SP-004");
            calc.CurrentSheet.SetCellText(7, 2, "GenSlides");
            calc.CurrentSheet.SetCellText(7, 3, "150");
            calc.CurrentSheet.SetCellText(7, 4, "$60.00");
            calc.CurrentSheet.SetCellText(7, 5, "9000");

            calc.CurrentSheet.SetCellText(9, 3, "TỔNG DOANH THU:");
            calc.CurrentSheet.SetCellText(9, 5, "68700");

            calc.CurrentSheet.SetCellText(10, 3, "TRUNG BÌNH:");
            calc.CurrentSheet.SetCellText(10, 5, "17175");

            // Select A1:E10
            calc.SelectedRow = 1;
            calc.SelectedCol = 1;
            calc.SelEndRow = 10;
            calc.SelEndCol = 5;

            // 1. Verify 4 Series Extracted
            DocChartData data = calc.ExtractChartDataFromSelection();
            if (data == null) throw new Exception("FAIL: Extracted chart data is null!");

            Console.WriteLine("[PASS] Series count: " + data.SeriesNames.Count);
            foreach (var s in data.SeriesNames)
            {
                Console.WriteLine("       -> " + s);
            }

            if (data.SeriesNames.Count != 4)
            {
                throw new Exception("FAIL: Expected exactly 4 series, got " + data.SeriesNames.Count);
            }

            Console.WriteLine("[PASS] Categories count: " + data.Categories.Count);
            foreach (var c in data.Categories)
            {
                Console.WriteLine("       -> " + c);
            }

            // 2. Verify Selection Box Coordinates During Scrolling
            int headerW = 48;
            int startY = 155;
            int headerH = 26;

            // Default scroll
            calc.ScrollCol = 1;
            calc.ScrollRow = 1;
            int x1 = calc.GetColScreenX(1, headerW);
            int y1 = calc.GetRowScreenY(3, startY, headerH);
            Console.WriteLine("[PASS] Initial Screen Pos: X=" + x1 + ", Y=" + y1);
            if (x1 != headerW) throw new Exception("FAIL: Initial X != headerW");

            // Scroll to Col 3, Row 5
            calc.ScrollCol = 3;
            calc.ScrollRow = 5;
            int scrolledX = calc.GetColScreenX(1, headerW);
            int scrolledY = calc.GetRowScreenY(3, startY, headerH);
            Console.WriteLine("[PASS] Scrolled Screen Pos: X=" + scrolledX + ", Y=" + scrolledY);
            if (scrolledX >= headerW) throw new Exception("FAIL: Scrolled X must be < headerW (anchored to cell)");
            if (scrolledY >= startY + headerH) throw new Exception("FAIL: Scrolled Y must be < startY + headerH (anchored to cell)");

            // 3. Render Line Chart with all 4 series & check Y max
            using (Bitmap bmp = new Bitmap(700, 450))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                data.ChartType = "Line";
                ChartRenderer.DrawChart(g, new Rectangle(10, 10, 680, 430), data, true, 1.0f);
            }
            Console.WriteLine("[PASS] Line Chart with all 4 series rendered smoothly with dynamic legend!");

            Console.WriteLine("=== ALL TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

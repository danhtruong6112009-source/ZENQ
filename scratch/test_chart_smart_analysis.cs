using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestChartSmartAnalysis
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING INTELLIGENT COLUMN & DATA ANALYSIS FOR CHARTS ===");

            CalcView calc = new CalcView();

            // Set up exact layout from user's screenshot:
            // Row 1: BẢNG TÍNH MỚI (Table Title)
            // Row 3: Mã SP | Tên Sản Phẩm | Số Lượng | Đơn Giá ($) | Thành Tiền ($)
            // Row 4: SP-001 | GenOffice    | 250      | 120         | 30000
            // Row 5: SP-002 | GenWrite     | 180      | 85          | 15300
            // Row 6: SP-003 | GenCalc      | 320      | 45          | 14400
            // Row 7: SP-004 | GenSlides    | 150      | 60          | 9000
            // Row 9: TỔNG DOANH THU | 68700
            // Row 10: TRUNG BÌNH    | 17175

            calc.CurrentSheet.SetCellText(1, 1, "BẢNG TÍNH MỚI");

            calc.CurrentSheet.SetCellText(3, 1, "Mã SP");
            calc.CurrentSheet.SetCellText(3, 2, "Tên Sản Phẩm");
            calc.CurrentSheet.SetCellText(3, 3, "Số Lượng");
            calc.CurrentSheet.SetCellText(3, 4, "Đơn Giá ($)");
            calc.CurrentSheet.SetCellText(3, 5, "Thành Tiền ($)");

            calc.CurrentSheet.SetCellText(4, 1, "SP-001");
            calc.CurrentSheet.SetCellText(4, 2, "GenOffice");
            calc.CurrentSheet.SetCellText(4, 3, "250");
            calc.CurrentSheet.SetCellText(4, 4, "120");
            calc.CurrentSheet.SetCellText(4, 5, "30000");

            calc.CurrentSheet.SetCellText(5, 1, "SP-002");
            calc.CurrentSheet.SetCellText(5, 2, "GenWrite");
            calc.CurrentSheet.SetCellText(5, 3, "180");
            calc.CurrentSheet.SetCellText(5, 4, "85");
            calc.CurrentSheet.SetCellText(5, 5, "15300");

            calc.CurrentSheet.SetCellText(6, 1, "SP-003");
            calc.CurrentSheet.SetCellText(6, 2, "GenCalc");
            calc.CurrentSheet.SetCellText(6, 3, "320");
            calc.CurrentSheet.SetCellText(6, 4, "45");
            calc.CurrentSheet.SetCellText(6, 5, "14400");

            calc.CurrentSheet.SetCellText(7, 1, "SP-004");
            calc.CurrentSheet.SetCellText(7, 2, "GenSlides");
            calc.CurrentSheet.SetCellText(7, 3, "150");
            calc.CurrentSheet.SetCellText(7, 4, "60");
            calc.CurrentSheet.SetCellText(7, 5, "9000");

            // Select entire area A1:E10
            calc.SelectedRow = 1;
            calc.SelectedCol = 1;
            calc.SelEndRow = 10;
            calc.SelEndCol = 5;

            DocChartData data = calc.ExtractChartDataFromSelection();
            if (data == null) throw new Exception("FAIL: Data analysis returned null!");

            Console.WriteLine("[PASS] Table Title analyzed: " + data.Title);
            Console.WriteLine("[PASS] Series Analyzed (" + data.SeriesNames.Count + "):");
            foreach (var s in data.SeriesNames)
            {
                Console.WriteLine("       -> " + s);
            }

            if (!data.SeriesNames.Contains("BẢNG TÍNH MỚI Số Lượng") ||
                !data.SeriesNames.Contains("BẢNG TÍNH MỚI Đơn Giá ($)") ||
                !data.SeriesNames.Contains("BẢNG TÍNH MỚI Thành Tiền ($)"))
            {
                throw new Exception("FAIL: Expected series names matching Excel screenshot!");
            }

            Console.WriteLine("[PASS] Categories Analyzed (" + data.Categories.Count + "):");
            foreach (var c in data.Categories)
            {
                Console.WriteLine("       -> " + c);
            }

            // Test Popups instantiation
            Point screenPt = new Point(100, 100);
            using (ChartElementsPopupForm elemForm = new ChartElementsPopupForm(screenPt, data, () => { }))
            {
                Console.WriteLine("[PASS] ChartElementsPopupForm initialized.");
            }

            using (ChartStylesPopupForm styleForm = new ChartStylesPopupForm(screenPt, data, () => { }))
            {
                Console.WriteLine("[PASS] ChartStylesPopupForm initialized.");
            }

            using (ChartFiltersPopupForm filterForm = new ChartFiltersPopupForm(screenPt, data, () => { }))
            {
                Console.WriteLine("[PASS] ChartFiltersPopupForm initialized.");
            }

            // Test Floating Buttons Drawing
            using (Bitmap bmp = new Bitmap(600, 400))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                calc.DrawChartFloatingButtons(g, new Rectangle(50, 50, 400, 250));
            }
            Console.WriteLine("[PASS] Chart Floating Action Buttons [ + ], [ 🖌 ], [ ⏚ ] rendered successfully.");

            Console.WriteLine("=== ALL SMART ANALYSIS AND FLOATING POPUP TESTS PASSED! ===");
        }
    }
}

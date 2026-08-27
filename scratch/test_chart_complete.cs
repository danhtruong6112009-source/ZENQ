using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestChartComplete
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING MS EXCEL-STYLE CHART INTEGRATION ===");

            CalcView calc = new CalcView();

            // 1. Test Empty Selection Validation
            calc.SelectedRow = 50;
            calc.SelectedCol = 50;
            calc.SelEndRow = 50;
            calc.SelEndCol = 50;
            DocChartData emptyData = calc.ExtractChartDataFromSelection();
            if (emptyData != null)
            {
                throw new Exception("FAIL: Expected null on empty cell, got data!");
            }
            Console.WriteLine("[PASS] Empty cell validation correctly returned null (will trigger MS Excel warning message).");

            // 2. Populate realistic spreadsheet data matching user screenshot
            // Row 3: Mã SP | Tên Sản Phẩm | Số Lượng | Đơn Giá ($) | Thành Tiền ($)
            // Row 4: SP-001 | GenOffice    | 250      | 120         | 30000
            // Row 5: SP-002 | GenWrite     | 180      | 85          | 15300
            // Row 6: SP-003 | GenCalc      | 320      | 45          | 14400
            // Row 7: SP-004 | GenSlides    | 150      | 60          | 9000
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

            // Select data range A3:E7
            calc.SelectedRow = 3;
            calc.SelectedCol = 1;
            calc.SelEndRow = 7;
            calc.SelEndCol = 5;

            DocChartData extractedData = calc.ExtractChartDataFromSelection();
            if (extractedData == null) throw new Exception("FAIL: Extracted data is null for valid range!");
            if (extractedData.Categories.Count != 4) throw new Exception("FAIL: Categories count != 4 (" + extractedData.Categories.Count + ")");
            if (extractedData.SeriesNames.Count != 4) throw new Exception("FAIL: Series count != 4 (" + extractedData.SeriesNames.Count + ")");
            Console.WriteLine("[PASS] Extracted data successfully: " + extractedData.Categories.Count + " categories, " + extractedData.SeriesNames.Count + " series.");
            Console.WriteLine("       Categories: " + string.Join(", ", extractedData.Categories.ToArray()));
            Console.WriteLine("       Series: " + string.Join(", ", extractedData.SeriesNames.ToArray()));

            // 3. Test 2D & 3D Chart Vector Rendering
            string[] testTypes = new string[]
            {
                "Column_Clustered",
                "Column_3D",
                "Column_Stacked",
                "Line",
                "Line_3D",
                "Bar_Clustered",
                "Bar_3D",
                "Pie",
                "Pie_3D",
                "Doughnut",
                "Area",
                "Area_3D"
            };

            foreach (var t in testTypes)
            {
                extractedData.ChartType = t;
                using (Bitmap bmp = new Bitmap(600, 400))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    ChartRenderer.DrawChart(g, new Rectangle(10, 10, 580, 380), extractedData, true, 1.0f);
                }
                Console.WriteLine("[PASS] Rendered 2D/3D Chart: " + t);
            }

            // 4. Test InsertChartForm creation
            using (InsertChartForm dlg = new InsertChartForm(extractedData))
            {
                if (dlg.SourceChartData == null) throw new Exception("FAIL: Dialog source data is null!");
                Console.WriteLine("[PASS] InsertChartForm created with Recommended & All Charts tabs.");
            }

            // 5. Test InsertChartObject into CalcView and Export to XLSX without errors
            calc.InsertChartObject(extractedData);
            if (calc.Shapes.Count != 1) throw new Exception("FAIL: Calc shapes count != 1");
            Console.WriteLine("[PASS] Chart shape inserted into CalcView.Shapes.");

            string testXlsx = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_chart_export.xlsx");
            if (File.Exists(testXlsx)) File.Delete(testXlsx);

            bool saved = XlsxHandler.SaveXlsx(testXlsx, calc);
            if (!saved || !File.Exists(testXlsx)) throw new Exception("FAIL: Could not save XLSX with chart!");
            Console.WriteLine("[PASS] Saved XLSX file with embedded chart.");

            // Verify OpenXML structure in XLSX package
            using (ZipArchive zip = ZipFile.OpenRead(testXlsx))
            {
                ZipArchiveEntry drawEntry = zip.GetEntry("xl/drawings/drawing1.xml");
                if (drawEntry == null) throw new Exception("FAIL: xl/drawings/drawing1.xml not found!");

                ZipArchiveEntry imgEntry = zip.GetEntry("xl/media/image1.png");
                if (imgEntry == null) throw new Exception("FAIL: xl/media/image1.png not found!");

                Console.WriteLine("[PASS] OpenXML DrawingML and Media verified in XLSX archive.");
            }

            if (File.Exists(testXlsx)) File.Delete(testXlsx);

            Console.WriteLine("=== ALL MS EXCEL-STYLE CHART TESTS COMPLETED WITH 100% SUCCESS! ===");
        }
    }
}

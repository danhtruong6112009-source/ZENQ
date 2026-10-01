using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace FullCalcTestSuite
{
    class Program
    {
        static string DesktopDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GenOffice_Test_All_Modules_Report");
        static string CalcDir = Path.Combine(DesktopDir, "02_CALC_TEST");
        static string CalcFilesDir = Path.Combine(CalcDir, "Files");
        static string CalcImagesDir = Path.Combine(CalcDir, "Images");
        static string CalcVideosDir = Path.Combine(CalcDir, "Videos");
        static string CalcFramesDir = Path.Combine(CalcDir, "AnimFrames");

        [STAThread]
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==================================================");
            Console.WriteLine(">>> BẮT ĐẦU KIỂM THỬ PHÂN HỆ CALC (SPREADSHEET) <<<");
            Console.WriteLine("==================================================");

            Directory.CreateDirectory(CalcFilesDir);
            Directory.CreateDirectory(CalcImagesDir);
            Directory.CreateDirectory(CalcVideosDir);
            Directory.CreateDirectory(CalcFramesDir);

            // 1. TẠO FILE XLSX TỪ GENOFFICE
            Console.WriteLine("[1/5] Tạo file XLSX từ GenOffice...");
            string genXlsxPath = Path.Combine(CalcFilesDir, "GenOffice_Created_Spreadsheet.xlsx");
            CalcView cv = CreateGenOfficeSpreadsheet();
            bool saveOk = XlsxHandler.SaveXlsxOrCsv(genXlsxPath, cv);
            Console.WriteLine("  -> Lưu XLSX: " + (saveOk ? "THÀNH CÔNG" : "THẤT BẠI") + " (" + (File.Exists(genXlsxPath) ? new FileInfo(genXlsxPath).Length : 0) + " bytes)");

            // 2. TẠO FILE XLSX TỪ MICROSOFT EXCEL (COM AUTOMATION)
            Console.WriteLine("[2/5] Tạo file XLSX từ Microsoft Excel COM...");
            string excelXlsxPath = Path.Combine(CalcFilesDir, "MSExcel_Created_Spreadsheet.xlsx");
            CreateExcelViaCOM(excelXlsxPath);

            // 3. KIỂM THỬ MỞ 2 CHIỀU (BIDIRECTIONAL COMPATIBILITY)
            Console.WriteLine("[3/5] Kiểm thử mở 2 chiều...");
            TestBidirectionalXlsx(genXlsxPath, excelXlsxPath);

            // 4. CHỤP ẢNH TẤT CẢ CÁC TAB RIBBON VÀ GIAO DIỆN CALC
            Console.WriteLine("[4/5] Chụp ảnh toàn bộ giao diện & Ribbon Tabs của Calc...");
            CaptureCalcRibbonAndViews(cv);

            // 5. RENDER CHUỖI KHUNG HÌNH TƯƠNG TÁC Ô TÍNH & BIỂU ĐỒ
            Console.WriteLine("[5/5] Render chuỗi khung hình tính toán công thức & tương tác bảng...");
            RenderCalcAnimationFrames(cv);

            Console.WriteLine("\n[HOÀN THÀNH] Đã hoàn tất kiểm thử Calc!");
        }

        static CalcView CreateGenOfficeSpreadsheet()
        {
            CalcView cv = new CalcView();
            SparseMatrix sheet = cv.CurrentSheet;

            // Column Widths
            cv.CustomColWidths[1] = 220; // Col A
            cv.CustomColWidths[2] = 130; // Col B
            cv.CustomColWidths[3] = 130; // Col C
            cv.CustomColWidths[4] = 130; // Col D
            cv.CustomColWidths[5] = 150; // Col E
            cv.CustomColWidths[6] = 140; // Col F
            cv.CustomColWidths[7] = 130; // Col G

            // Row Heights
            cv.CustomRowHeights[1] = 28;
            cv.CustomRowHeights[2] = 28;
            cv.CustomRowHeights[4] = 38;
            cv.CustomRowHeights[5] = 26;
            cv.CustomRowHeights[6] = 26;
            cv.CustomRowHeights[7] = 26;
            cv.CustomRowHeights[8] = 26;
            cv.CustomRowHeights[9] = 30;

            // 1. Title Banner (Merged A1:G2, Dark Green, White Bold Text)
            sheet.MergeCells(1, 1, 2, 7);
            sheet.SetCellText(1, 1, "BÁO CÁO DOANH THU & KẾT QUẢ KINH DOANH GENOFFICE 2026");
            CellData titleCell = sheet.GetCell(1, 1);
            titleCell.Format.BgColor = Color.FromArgb(16, 124, 65).ToArgb();
            titleCell.Format.TextColor = Color.White.ToArgb();
            titleCell.Format.FontSize = 14f;
            titleCell.Format.Bold = true;
            titleCell.Format.Align = 1; // Center
            titleCell.Format.VAlign = 1; // Middle

            // 2. Diagonal Split Header Cell A4 (Tháng \ Sản phẩm)
            sheet.SetCellText(4, 1, "Tháng\nSản phẩm");
            CellData diagCell = sheet.GetCell(4, 1);
            diagCell.Format.Borders.DiagonalDown = true;
            diagCell.Format.Borders.DiagonalThickness = 2;
            diagCell.Format.Borders.DiagonalColor = Color.FromArgb(30, 41, 59).ToArgb();
            diagCell.Format.Borders.Top = true;
            diagCell.Format.Borders.Bottom = true;
            diagCell.Format.Borders.Left = true;
            diagCell.Format.Borders.Right = true;
            diagCell.Format.IsDiagonalHeader = true;
            diagCell.Format.DiagonalTopRightText = "Tháng";
            diagCell.Format.DiagonalBottomLeftText = "Sản phẩm";
            diagCell.Format.BgColor = Color.FromArgb(241, 245, 249).ToArgb();
            diagCell.Format.Bold = true;

            // 3. Column Headers B4:G4
            string[] headers = new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tổng Quý 1", "Tăng trưởng", "Đánh giá" };
            for (uint c = 2; c <= 7; c++)
            {
                sheet.SetCellText(4, c, headers[c - 2]);
                CellData hCell = sheet.GetCell(4, c);
                hCell.Format.BgColor = Color.FromArgb(37, 99, 235).ToArgb(); // Royal Blue
                hCell.Format.TextColor = Color.White.ToArgb();
                hCell.Format.Bold = true;
                hCell.Format.Align = 1;
                hCell.Format.VAlign = 1;
                hCell.Format.Borders.Top = true;
                hCell.Format.Borders.Bottom = true;
                hCell.Format.Borders.Left = true;
                hCell.Format.Borders.Right = true;
            }

            // 4. Data Rows A5:G8
            string[] products = new string[] { "Laptop GenOffice Pro", "Màn hình 4K Ultra", "Bàn phím cơ Không dây", "Chuột Ergonomic Master" };
            double[,] values = new double[,] {
                { 15000000, 18500000, 22000000 },
                { 6200000, 7800000, 8500000 },
                { 1200000, 1650000, 2100000 },
                { 850000, 950000, 1300000 }
            };
            string[] growths = new string[] { "+15.5%", "+8.2%", "+25.0%", "+12.0%" };
            string[] status = new string[] { "Xuất sắc", "Đạt chuẩn", "Tăng mạnh", "Ổn định" };

            for (uint r = 5; r <= 8; r++)
            {
                uint idx = r - 5;
                // Col A
                sheet.SetCellText(r, 1, products[idx]);
                CellData pCell = sheet.GetCell(r, 1);
                pCell.Format.Bold = true;
                pCell.Format.Borders.Left = true;
                pCell.Format.Borders.Right = true;
                pCell.Format.Borders.Bottom = true;
                if (r % 2 == 1) pCell.Format.BgColor = Color.FromArgb(248, 250, 252).ToArgb();

                // Col B, C, D (Values)
                for (uint c = 2; c <= 4; c++)
                {
                    double val = values[idx, c - 2];
                    sheet.SetCellText(r, c, val.ToString("N0", CultureInfo.InvariantCulture));
                    CellData vCell = sheet.GetCell(r, c);
                    vCell.Format.Align = 2; // Right
                    vCell.Format.Borders.Left = true;
                    vCell.Format.Borders.Right = true;
                    vCell.Format.Borders.Bottom = true;
                    if (r % 2 == 1) vCell.Format.BgColor = Color.FromArgb(248, 250, 252).ToArgb();
                }

                // Col E: Formula SUM
                double sumVal = values[idx, 0] + values[idx, 1] + values[idx, 2];
                sheet.SetCellText(r, 5, string.Format("=SUM(B{0}:D{0})", r));
                CellData sumCell = sheet.GetCell(r, 5);
                sumCell.EvaluatedStr = sumVal.ToString("N0", CultureInfo.InvariantCulture) + " ₫";
                sumCell.Format.Bold = true;
                sumCell.Format.Align = 2;
                sumCell.Format.Borders.Left = true;
                sumCell.Format.Borders.Right = true;
                sumCell.Format.Borders.Bottom = true;
                sumCell.Format.BgColor = Color.FromArgb(239, 246, 255).ToArgb(); // Light Blue

                // Col F: Growth
                sheet.SetCellText(r, 6, growths[idx]);
                CellData gCell = sheet.GetCell(r, 6);
                gCell.Format.Align = 1;
                gCell.Format.TextColor = Color.FromArgb(22, 163, 74).ToArgb();
                gCell.Format.Bold = true;
                gCell.Format.Borders.Left = true;
                gCell.Format.Borders.Right = true;
                gCell.Format.Borders.Bottom = true;

                // Col G: Status
                sheet.SetCellText(r, 7, status[idx]);
                CellData sCell = sheet.GetCell(r, 7);
                sCell.Format.Align = 1;
                sCell.Format.Bold = true;
                sCell.Format.TextColor = Color.FromArgb(37, 99, 235).ToArgb();
                sCell.Format.Borders.Left = true;
                sCell.Format.Borders.Right = true;
                sCell.Format.Borders.Bottom = true;
            }

            // 5. Total Summary Row (Row 9)
            sheet.SetCellText(9, 1, "TỔNG CỘNG TOÀN BỘ");
            CellData t1 = sheet.GetCell(9, 1);
            t1.Format.Bold = true;
            t1.Format.BgColor = Color.FromArgb(220, 252, 231).ToArgb(); // Light Emerald
            t1.Format.Borders.Top = true;
            t1.Format.Borders.Bottom = true;
            t1.Format.Borders.Left = true;
            t1.Format.Borders.Right = true;

            for (uint c = 2; c <= 5; c++)
            {
                char colLetter = (char)('A' + c - 1);
                sheet.SetCellText(9, c, string.Format("=SUM({0}5:{0}8)", colLetter));
                CellData tc = sheet.GetCell(9, c);
                tc.Format.Bold = true;
                tc.Format.Align = 2;
                tc.Format.BgColor = Color.FromArgb(220, 252, 231).ToArgb();
                tc.Format.Borders.Top = true;
                tc.Format.Borders.Bottom = true;
                tc.Format.Borders.Left = true;
                tc.Format.Borders.Right = true;
            }

            sheet.SetCellText(9, 6, "+18.2%");
            CellData t6 = sheet.GetCell(9, 6);
            t6.Format.Bold = true;
            t6.Format.Align = 1;
            t6.Format.BgColor = Color.FromArgb(220, 252, 231).ToArgb();
            t6.Format.TextColor = Color.FromArgb(21, 128, 61).ToArgb();
            t6.Format.Borders.Top = true;
            t6.Format.Borders.Bottom = true;
            t6.Format.Borders.Left = true;
            t6.Format.Borders.Right = true;

            sheet.SetCellText(9, 7, "VƯỢT CHỈ TIÊU");
            CellData t7 = sheet.GetCell(9, 7);
            t7.Format.Bold = true;
            t7.Format.Align = 1;
            t7.Format.BgColor = Color.FromArgb(220, 252, 231).ToArgb();
            t7.Format.TextColor = Color.FromArgb(21, 128, 61).ToArgb();
            t7.Format.Borders.Top = true;
            t7.Format.Borders.Bottom = true;
            t7.Format.Borders.Left = true;
            t7.Format.Borders.Right = true;

            FormulaEngine.EvaluateSheet(sheet);

            return cv;
        }

        static void CreateExcelViaCOM(string outPath)
        {
            try
            {
                Type xlType = Type.GetTypeFromProgID("Excel.Application");
                if (xlType == null)
                {
                    Console.WriteLine("  [CẢNH BÁO] Không tìm thấy Excel COM.");
                    return;
                }

                object xlApp = Activator.CreateInstance(xlType);
                xlApp.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, xlApp, new object[] { false });
                xlApp.GetType().InvokeMember("DisplayAlerts", BindingFlags.SetProperty, null, xlApp, new object[] { false });

                object workbooks = xlType.InvokeMember("Workbooks", BindingFlags.GetProperty, null, xlApp, null);
                object wb = workbooks.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, workbooks, null);
                object sheets = wb.GetType().InvokeMember("Sheets", BindingFlags.GetProperty, null, wb, null);
                object ws = sheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, sheets, new object[] { 1 });

                // Set Title
                object rangeTitle = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "A1:E1" });
                rangeTitle.GetType().InvokeMember("Merge", BindingFlags.InvokeMethod, null, rangeTitle, null);
                rangeTitle.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rangeTitle, new object[] { "EXCEL OFFICIAL ENTERPRISE WORKBOOK 2026" });

                // Set Data & Formulas
                object rA3 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "A3" });
                rA3.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rA3, new object[] { "Hạng mục" });
                object rB3 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "B3" });
                rB3.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rB3, new object[] { "Số lượng" });
                object rC3 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "C3" });
                rC3.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rC3, new object[] { "Đơn giá" });
                object rD3 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "D3" });
                rD3.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rD3, new object[] { "Thành tiền" });

                object rA4 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "A4" });
                rA4.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rA4, new object[] { "Bản quyền GenOffice" });
                object rB4 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "B4" });
                rB4.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rB4, new object[] { 50 });
                object rC4 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "C4" });
                rC4.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, rC4, new object[] { 2500000 });
                object rD4 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "D4" });
                rD4.GetType().InvokeMember("Formula", BindingFlags.SetProperty, null, rD4, new object[] { "=B4*C4" });

                if (File.Exists(outPath)) File.Delete(outPath);
                // xlOpenXMLWorkbook = 51
                wb.GetType().InvokeMember("SaveAs", BindingFlags.InvokeMethod, null, wb, new object[] { outPath, 51 });
                wb.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, wb, new object[] { false });
                xlApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, xlApp, null);

                Marshal.ReleaseComObject(wb);
                Marshal.ReleaseComObject(xlApp);

                Console.WriteLine("  -> Đã tạo thành công file XLSX từ MS Excel: " + outPath + " (" + new FileInfo(outPath).Length + " bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI COM EXCEL] " + ex.Message);
            }
        }

        static void TestBidirectionalXlsx(string genXlsxPath, string excelXlsxPath)
        {
            // 1. Mở file của GenOffice trong Excel COM & kiểm tra tính toán công thức
            try
            {
                Type xlType = Type.GetTypeFromProgID("Excel.Application");
                if (xlType != null && File.Exists(genXlsxPath))
                {
                    object xlApp = Activator.CreateInstance(xlType);
                    xlApp.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, xlApp, new object[] { false });
                    xlApp.GetType().InvokeMember("DisplayAlerts", BindingFlags.SetProperty, null, xlApp, new object[] { false });

                    object workbooks = xlType.InvokeMember("Workbooks", BindingFlags.GetProperty, null, xlApp, null);
                    object wb = workbooks.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, workbooks, new object[] { genXlsxPath });
                    object sheets = wb.GetType().InvokeMember("Sheets", BindingFlags.GetProperty, null, wb, null);
                    object ws = sheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, sheets, new object[] { 1 });

                    object rangeE9 = ws.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, ws, new object[] { "E9" });
                    object valE9 = rangeE9.GetType().InvokeMember("Text", BindingFlags.GetProperty, null, rangeE9, null);
                    Console.WriteLine("  [THÀNH CÔNG] Excel đã mở file XLSX của GenOffice! Giá trị ô Tổng cộng E9: " + valE9);

                    wb.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, wb, new object[] { false });
                    xlApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, xlApp, null);
                    Marshal.ReleaseComObject(xlApp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI TEST EXCEL COM] " + ex.Message);
            }

            // 2. Mở file của Excel trong GenOffice
            try
            {
                if (File.Exists(excelXlsxPath))
                {
                    CalcView loadedCv = new CalcView();
                    bool loadOk = XlsxHandler.LoadXlsxOrCsv(excelXlsxPath, loadedCv);
                    if (loadOk)
                    {
                        FormulaEngine.EvaluateSheet(loadedCv.CurrentSheet);
                        Console.WriteLine("  [THÀNH CÔNG] GenOffice đã nạp thành công file XLSX của Excel! Số sheet: " + loadedCv.Sheets.Count);
                        string roundtripBack = Path.Combine(CalcFilesDir, "Roundtrip_MSExcel_re-saved_by_GenOffice.xlsx");
                        XlsxHandler.SaveXlsxOrCsv(roundtripBack, loadedCv);
                        Console.WriteLine("  -> Đã lưu lại bản roundtrip: " + roundtripBack);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI GENOFFICE LOAD XLSX] " + ex.Message);
            }
        }

        static void CaptureCalcRibbonAndViews(CalcView cv)
        {
            try
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(1366, 800);
                    form.Ribbon.CurrentMode = OfficeAppMode.Calc;
                    form.CalcView = cv;

                    string[] tabNames = new string[] {
                        "01_Home_TrangDau",
                        "02_Insert_Chen",
                        "03_PageLayout_BoTriTrang",
                        "04_Formulas_CongThuc",
                        "05_Data_DuLieu",
                        "06_Review_XemLai",
                        "07_View_Xem"
                    };

                    for (int i = 0; i < tabNames.Length; i++)
                    {
                        form.Ribbon.ActiveRibbonTab = i;
                        using (Bitmap bmp = new Bitmap(1366, 800))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.CalcView.Draw(g, 1366, 800);
                                form.Ribbon.Draw(g, 1366, 800, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            string imgPath = Path.Combine(CalcImagesDir, "Ribbon_Tab_" + tabNames[i] + ".png");
                            bmp.Save(imgPath, ImageFormat.Png);
                        }
                    }

                    // Capture High-Res Spreadsheet Grid & Formulas View
                    using (Bitmap bmp = new Bitmap(1366, 800))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            form.CalcView.Draw(g, 1366, 800);
                            form.Ribbon.Draw(g, 1366, 800, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                        }
                        string imgPath = Path.Combine(CalcImagesDir, "GenOffice_Calc_Full_Grid_And_Formulas.png");
                        bmp.Save(imgPath, ImageFormat.Png);
                    }
                }

                Console.WriteLine("  -> Đã chụp toàn bộ ảnh Ribbon tabs và Lưới tính toán của Calc!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI CAPTURE CALC] " + ex.Message);
            }
        }

        static void RenderCalcAnimationFrames(CalcView cv)
        {
            try
            {
                using (AppForm form = new AppForm())
                {
                    form.Size = new Size(1280, 720);
                    form.Ribbon.CurrentMode = OfficeAppMode.Calc;
                    form.CalcView = cv;

                    int totalFrames = 60;
                    uint[] targetRows = new uint[] { 5, 6, 7, 8, 9 };
                    uint[] targetCols = new uint[] { 2, 3, 4, 5 };

                    for (int f = 0; f < totalFrames; f++)
                    {
                        int rIdx = (f / 12) % targetRows.Length;
                        int cIdx = (f / 3) % targetCols.Length;
                        cv.SelectedRow = targetRows[rIdx];
                        cv.SelectedCol = targetCols[cIdx];

                        using (Bitmap bmp = new Bitmap(1280, 720))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                form.CalcView.Draw(g, 1280, 720);
                                form.Ribbon.Draw(g, 1280, 720, form.WriterView, form.SlidesView, form.CalcView, form.PdfView);
                            }
                            string framePath = Path.Combine(CalcFramesDir, string.Format("calc_frame_{0:D3}.png", f));
                            bmp.Save(framePath, ImageFormat.Png);
                        }
                    }

                    Console.WriteLine("  -> Đã xuất " + totalFrames + " khung hình tương tác ô tính & công thức chất lượng cao!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [LỖI RENDER CALC FRAMES] " + ex.Message);
            }
        }
    }
}

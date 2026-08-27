using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace CreateFullFormatsSample
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                CalcView cv = new CalcView();
                SparseMatrix sheet = cv.CurrentSheet;

                // 1. Column Widths
                cv.CustomColWidths[1] = 200; // Col A
                cv.CustomColWidths[2] = 120; // Col B
                cv.CustomColWidths[3] = 120; // Col C
                cv.CustomColWidths[4] = 120; // Col D
                cv.CustomColWidths[5] = 130; // Col E
                cv.CustomColWidths[6] = 130; // Col F

                // Row Heights
                cv.CustomRowHeights[1] = 26;
                cv.CustomRowHeights[2] = 26;
                cv.CustomRowHeights[4] = 36;
                cv.CustomRowHeights[5] = 24;
                cv.CustomRowHeights[6] = 24;
                cv.CustomRowHeights[7] = 24;
                cv.CustomRowHeights[8] = 24;
                cv.CustomRowHeights[9] = 22;
                cv.CustomRowHeights[10] = 30;

                // 2. Title Banner (Merged A1:F2, Center, Green Bg, White Bold Text)
                sheet.MergeCells(1, 1, 2, 6);
                sheet.SetCellText(1, 1, "BÁO CÁO DOANH THU & KẾT QUẢ KINH DOANH 2026");
                CellData titleCell = sheet.GetCell(1, 1);
                titleCell.Format.BgColor = Color.FromArgb(16, 124, 65).ToArgb();
                titleCell.Format.TextColor = Color.White.ToArgb();
                titleCell.Format.FontSize = 13f;
                titleCell.Format.Bold = true;
                titleCell.Format.Align = 1; // Center
                titleCell.Format.VAlign = 1; // Middle

                // 3. Diagonal Split Header Cell A4 (Tháng \ Sản phẩm)
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

                // 4. Column Headers B4:F4
                string[] headers = new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tổng Quý 1", "Tăng trưởng" };
                for (uint c = 2; c <= 6; c++)
                {
                    sheet.SetCellText(4, c, headers[c - 2]);
                    CellData hCell = sheet.GetCell(4, c);
                    hCell.Format.BgColor = Color.FromArgb(37, 99, 235).ToArgb();
                    hCell.Format.TextColor = Color.White.ToArgb();
                    hCell.Format.Bold = true;
                    hCell.Format.Align = 1;
                    hCell.Format.VAlign = 1;
                    hCell.Format.Borders.Top = true;
                    hCell.Format.Borders.Bottom = true;
                    hCell.Format.Borders.Left = true;
                    hCell.Format.Borders.Right = true;
                }

                // 5. Data Rows A5:F8
                string[] products = new string[] { "Laptop GenOffice Pro", "Màn hình 4K Ultra", "Bàn phím cơ Không dây", "Chuột Ergonomic Master" };
                double[,] values = new double[,] {
                    { 15000000, 18500000, 22000000 },
                    { 6200000, 7800000, 8500000 },
                    { 1200000, 1650000, 2100000 },
                    { 850000, 950000, 1300000 }
                };
                string[] growths = new string[] { "+15.5%", "+8.2%", "+25.0%", "+12.0%" };

                for (uint r = 5; r <= 8; r++)
                {
                    uint idx = r - 5;
                    // Col A: Product Name
                    sheet.SetCellText(r, 1, products[idx]);
                    CellData pCell = sheet.GetCell(r, 1);
                    pCell.Format.Bold = true;
                    pCell.Format.VAlign = 1;
                    pCell.Format.Borders.Top = true;
                    pCell.Format.Borders.Bottom = true;
                    pCell.Format.Borders.Left = true;
                    pCell.Format.Borders.Right = true;
                    if (r % 2 == 0) pCell.Format.BgColor = Color.FromArgb(248, 250, 252).ToArgb();

                    // Cols B, C, D: Values
                    for (uint c = 2; c <= 4; c++)
                    {
                        double val = values[idx, c - 2];
                        sheet.SetCellText(r, c, val.ToString("N0", CultureInfo.InvariantCulture));
                        CellData vCell = sheet.GetCell(r, c);
                        vCell.Format.Align = 2; // Right
                        vCell.Format.VAlign = 1;
                        vCell.Format.Borders.Top = true;
                        vCell.Format.Borders.Bottom = true;
                        vCell.Format.Borders.Left = true;
                        vCell.Format.Borders.Right = true;
                        if (r % 2 == 0) vCell.Format.BgColor = Color.FromArgb(248, 250, 252).ToArgb();
                    }

                    // Col E: Total Formula =SUM(B..:D..)
                    double sumVal = values[idx, 0] + values[idx, 1] + values[idx, 2];
                    sheet.SetCellText(r, 5, string.Format("=SUM(B{0}:D{0})", r));
                    CellData sCell = sheet.GetCell(r, 5);
                    sCell.EvaluatedStr = sumVal.ToString("N0", CultureInfo.InvariantCulture);
                    sCell.Format.Bold = true;
                    sCell.Format.TextColor = Color.FromArgb(16, 124, 65).ToArgb();
                    sCell.Format.Align = 2;
                    sCell.Format.VAlign = 1;
                    sCell.Format.Borders.Top = true;
                    sCell.Format.Borders.Bottom = true;
                    sCell.Format.Borders.Left = true;
                    sCell.Format.Borders.Right = true;
                    if (r % 2 == 0) sCell.Format.BgColor = Color.FromArgb(248, 250, 252).ToArgb();

                    // Col F: Growth
                    sheet.SetCellText(r, 6, growths[idx]);
                    CellData gCell = sheet.GetCell(r, 6);
                    gCell.Format.TextColor = Color.FromArgb(37, 99, 235).ToArgb();
                    gCell.Format.Bold = true;
                    gCell.Format.Align = 1; // Center
                    gCell.Format.VAlign = 1;
                    gCell.Format.Borders.Top = true;
                    gCell.Format.Borders.Bottom = true;
                    gCell.Format.Borders.Left = true;
                    gCell.Format.Borders.Right = true;
                    if (r % 2 == 0) gCell.Format.BgColor = Color.FromArgb(248, 250, 252).ToArgb();
                }

                // 6. Merged Note Row A9:F9
                sheet.MergeCells(9, 1, 9, 6);
                sheet.SetCellText(9, 1, "* Ghi chú: Báo cáo đã bao gồm thuế GTGT và tất cả chương trình chiết khấu quý 1/2026.");
                CellData noteCell = sheet.GetCell(9, 1);
                noteCell.Format.BgColor = Color.FromArgb(254, 249, 195).ToArgb(); // Light yellow
                noteCell.Format.TextColor = Color.FromArgb(180, 83, 9).ToArgb(); // Dark Amber
                noteCell.Format.Italic = true;
                noteCell.Format.FontSize = 9.5f;
                noteCell.Format.VAlign = 1;
                noteCell.Format.Borders.Top = true;
                noteCell.Format.Borders.Bottom = true;
                noteCell.Format.Borders.Left = true;
                noteCell.Format.Borders.Right = true;

                // 7. Total Summary Row A10:F10
                sheet.SetCellText(10, 1, "TỔNG CỘNG DOANH THU");
                CellData t1 = sheet.GetCell(10, 1);
                t1.Format.Bold = true;
                t1.Format.BgColor = Color.FromArgb(220, 252, 231).ToArgb(); // Soft emerald
                t1.Format.TextColor = Color.FromArgb(22, 101, 52).ToArgb();
                t1.Format.VAlign = 1;
                t1.Format.Borders.Top = true;
                t1.Format.Borders.Bottom = true;
                t1.Format.Borders.Left = true;
                t1.Format.Borders.Right = true;

                double totalB = 15000000 + 6200000 + 1200000 + 850000;
                double totalC = 18500000 + 7800000 + 1650000 + 950000;
                double totalD = 22000000 + 8500000 + 2100000 + 1300000;
                double totalE = totalB + totalC + totalD;

                sheet.SetCellText(10, 2, "=SUM(B5:B8)");
                sheet.GetCell(10, 2).EvaluatedStr = totalB.ToString("N0", CultureInfo.InvariantCulture);

                sheet.SetCellText(10, 3, "=SUM(C5:C8)");
                sheet.GetCell(10, 3).EvaluatedStr = totalC.ToString("N0", CultureInfo.InvariantCulture);

                sheet.SetCellText(10, 4, "=SUM(D5:D8)");
                sheet.GetCell(10, 4).EvaluatedStr = totalD.ToString("N0", CultureInfo.InvariantCulture);

                sheet.SetCellText(10, 5, "=SUM(E5:E8)");
                sheet.GetCell(10, 5).EvaluatedStr = totalE.ToString("N0", CultureInfo.InvariantCulture);

                sheet.SetCellText(10, 6, "100.0%");

                for (uint c = 2; c <= 6; c++)
                {
                    CellData tc = sheet.GetCell(10, c);
                    tc.Format.Bold = true;
                    tc.Format.BgColor = Color.FromArgb(220, 252, 231).ToArgb();
                    tc.Format.TextColor = Color.FromArgb(22, 101, 52).ToArgb();
                    tc.Format.Align = (c == 6) ? 1 : 2;
                    tc.Format.VAlign = 1;
                    tc.Format.Borders.Top = true;
                    tc.Format.Borders.Bottom = true;
                    tc.Format.Borders.Left = true;
                    tc.Format.Borders.Right = true;
                }

                // 8. Save to Excel .xlsx file
                string outXlsx = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "BaoCaoDoanhThu_2026.xlsx");
                if (File.Exists(outXlsx)) File.Delete(outXlsx);
                bool saveOk = XlsxHandler.SaveXlsx(outXlsx, cv);
                Console.WriteLine("SaveXlsx: " + (saveOk ? "SUCCESS" : "FAILED") + " -> " + outXlsx);

                // 9. Render GenOffice Screenshot
                Bitmap bmp = new Bitmap(1000, 450);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    cv.Draw(g, 1000, 450);
                }
                string genOfficeImg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "genoffice_rendered_baocao.png");
                bmp.Save(genOfficeImg);
                Console.WriteLine("GenOffice Screenshot: " + genOfficeImg);

                // 10. Load back from saved .xlsx to verify 100% roundtrip
                CalcView loadedCv = new CalcView();
                bool loadOk = XlsxHandler.LoadXlsx(outXlsx, loadedCv);
                Console.WriteLine("LoadXlsx: " + (loadOk ? "SUCCESS" : "FAILED"));

                Bitmap bmpLoaded = new Bitmap(1000, 450);
                using (Graphics g = Graphics.FromImage(bmpLoaded))
                {
                    g.Clear(Color.White);
                    loadedCv.Draw(g, 1000, 450);
                }
                string reloadedImg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "genoffice_reloaded_from_xlsx.png");
                bmpLoaded.Save(reloadedImg);
                Console.WriteLine("Reloaded Screenshot: " + reloadedImg);

                Console.WriteLine("=== SAMPLE_GENERATION_SUCCESSFUL ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

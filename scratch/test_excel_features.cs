using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Xml;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestExcelFeatures
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=== Starting Test: Excel Shapes, Pictures, Tables & PivotTables ===");
            bool allPassed = true;

            try
            {
                // Test 1: Insert Shape and Picture into CalcView
                Console.Write("[Test 1] Insert Shape and Picture into CalcView... ");
                CalcView calcView = new CalcView();
                calcView.InsertShape("RoundedRectangle");
                if (calcView.Shapes.Count != 1 || calcView.Shapes[0].ShapeType != "RoundedRectangle")
                {
                    Console.WriteLine("FAILED! Expected 1 RoundedRectangle shape.");
                    allPassed = false;
                }
                else
                {
                    calcView.Shapes[0].TextContent = "Hello Excel DrawingML!";
                    Console.WriteLine("PASSED!");
                }

                // Create a temporary test image
                string tempImgPath = Path.Combine(Path.GetTempPath(), "test_excel_pic.png");
                using (Bitmap bmp = new Bitmap(120, 80))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.RoyalBlue);
                    using (Font f = new Font("Arial", 9, FontStyle.Bold))
                    {
                        g.DrawString("TEST PIC", f, Brushes.White, 10, 30);
                    }
                    bmp.Save(tempImgPath, ImageFormat.Png);
                }

                Console.Write("[Test 2] Insert Picture into CalcView... ");
                calcView.InsertPicture(tempImgPath);
                if (calcView.Shapes.Count != 2 || calcView.Shapes[1].ShapeType != "Image")
                {
                    Console.WriteLine("FAILED! Expected 2 shapes (second is Image).");
                    allPassed = false;
                }
                else
                {
                    Console.WriteLine("PASSED!");
                }

                // Test 3: Export to .xlsx with OpenXML DrawingML
                Console.Write("[Test 3] Save to .xlsx with DrawingML... ");
                string testXlsx = Path.Combine(Path.GetTempPath(), "test_export_drawings.xlsx");
                bool saveOk = XlsxHandler.SaveXlsx(testXlsx, calcView);
                if (!saveOk || !File.Exists(testXlsx))
                {
                    Console.WriteLine("FAILED to save test_export_drawings.xlsx!");
                    allPassed = false;
                }
                else
                {
                    Console.WriteLine("PASSED!");
                }

                // Test 4: Validate OpenXML DrawingML zip structure
                Console.Write("[Test 4] Inspect OpenXML DrawingML structure... ");
                using (FileStream fs = new FileStream(testXlsx, FileMode.Open, FileAccess.Read))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry ct = zip.GetEntry("[Content_Types].xml");
                    ZipArchiveEntry drw = zip.GetEntry("xl/drawings/drawing1.xml");
                    ZipArchiveEntry drwRels = zip.GetEntry("xl/drawings/_rels/drawing1.xml.rels");
                    ZipArchiveEntry sRels = zip.GetEntry("xl/worksheets/_rels/sheet1.xml.rels");
                    ZipArchiveEntry imgEntry = zip.GetEntry("xl/media/image1.png");

                    if (ct == null || drw == null || drwRels == null || sRels == null || imgEntry == null)
                    {
                        Console.WriteLine("FAILED! Missing OpenXML drawing parts (ct=" + (ct != null) + ", drw=" + (drw != null) + ", drwRels=" + (drwRels != null) + ", sRels=" + (sRels != null) + ", img=" + (imgEntry != null) + ")");
                        allPassed = false;
                    }
                    else
                    {
                        // Verify drawing1.xml contains twoCellAnchor
                        using (StreamReader sr = new StreamReader(drw.Open()))
                        {
                            string drwXml = sr.ReadToEnd();
                            if (!drwXml.Contains("<xdr:twoCellAnchor") || !drwXml.Contains("<xdr:pic>") || !drwXml.Contains("<xdr:sp"))
                            {
                                Console.WriteLine("FAILED! drawing1.xml does not contain twoCellAnchor, pic, or sp elements.");
                                allPassed = false;
                            }
                            else
                            {
                                Console.WriteLine("PASSED!");
                            }
                        }
                    }
                }

                // Test 5: Load .xlsx back into new CalcView and verify imported shapes
                Console.Write("[Test 5] Load .xlsx and round-trip verify shapes & pictures... ");
                CalcView loadedCalc = new CalcView();
                bool loadOk = XlsxHandler.LoadXlsx(testXlsx, loadedCalc);
                if (!loadOk)
                {
                    Console.WriteLine("FAILED to load test_export_drawings.xlsx!");
                    allPassed = false;
                }
                else if (loadedCalc.Shapes.Count != 2)
                {
                    Console.WriteLine("FAILED! Expected 2 imported shapes, got: " + loadedCalc.Shapes.Count);
                    allPassed = false;
                }
                else
                {
                    bool hasRect = false, hasImg = false;
                    foreach (var sh in loadedCalc.Shapes)
                    {
                        if (sh.ShapeType == "RoundedRectangle") hasRect = true;
                        if (sh.ShapeType == "Image" && sh.CachedImage != null) hasImg = true;
                    }
                    if (hasRect && hasImg)
                    {
                        Console.WriteLine("PASSED! Successfully round-tripped shapes and pictures.");
                    }
                    else
                    {
                        Console.WriteLine("FAILED! hasRect=" + hasRect + ", hasImg=" + hasImg);
                        allPassed = false;
                    }
                }

                // Test 6: Test Table creation and Column filtering
                Console.Write("[Test 6] Test CreateTableFromRange and Column Filter... ");
                CalcView tableCalc = new CalcView();
                tableCalc.CreateTableFromRange("=$A$3:$E$8", true);
                if (tableCalc.Tables.Count != 1)
                {
                    Console.WriteLine("FAILED! Expected 1 table registered.");
                    allPassed = false;
                }
                else
                {
                    // Filter column 1 to SP-001 only
                    HashSet<string> allowed = new HashSet<string>();
                    allowed.Add("SP-001");
                    tableCalc.ApplyColumnFilter(tableCalc.Tables[0], 1, allowed);

                    if (!tableCalc.HiddenRows.Contains(5) || tableCalc.HiddenRows.Contains(4))
                    {
                        Console.WriteLine("FAILED! Row 5 should be hidden, row 4 (SP-001) should NOT be hidden.");
                        allPassed = false;
                    }
                    else
                    {
                        tableCalc.ClearColumnFilter(tableCalc.Tables[0], 1);
                        if (tableCalc.HiddenRows.Count != 0)
                        {
                            Console.WriteLine("FAILED! Clearing filter should unhide all rows.");
                            allPassed = false;
                        }
                        else
                        {
                            Console.WriteLine("PASSED!");
                        }
                    }
                }

                // Test 7: Test PivotTable creation
                Console.Write("[Test 7] Test CreatePivotTable... ");
                CalcView pivotCalc = new CalcView();
                int origSheetCount = pivotCalc.Sheets.Count;
                pivotCalc.CreatePivotTable("=$A$3:$E$8", true, "");
                if (pivotCalc.Sheets.Count != origSheetCount + 1)
                {
                    Console.WriteLine("FAILED! New PivotTable sheet not created.");
                    allPassed = false;
                }
                else
                {
                    // Check PivotTable title and Grand Total
                    string title = pivotCalc.CurrentSheet.GetCell(2, 2) != null ? pivotCalc.CurrentSheet.GetCell(2, 2).GetDisplayText() : "";
                    if (!title.Contains("PIVOTTABLE"))
                    {
                        Console.WriteLine("FAILED! Missing PivotTable title, found: " + title);
                        allPassed = false;
                    }
                    else
                    {
                        Console.WriteLine("PASSED!");
                    }
                }

                // Clean up temp files
                try { if (File.Exists(tempImgPath)) File.Delete(tempImgPath); } catch { }
                try { if (File.Exists(testXlsx)) File.Delete(testXlsx); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.ToString());
                allPassed = false;
            }

            Console.WriteLine("=== Final Verdict: " + (allPassed ? "ALL TESTS PASSED 100%!" : "SOME TESTS FAILED!") + " ===");
            return allPassed ? 0 : 1;
        }
    }
}

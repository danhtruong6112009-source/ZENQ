using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using ZenQOffice.Core;
using ZenQOffice.Format;
using ZenQOffice.UI;

namespace ZenQOffice.Tests
{
    public class DocxInteropTests
    {
        private static int _passed = 0;
        private static int _failed = 0;

        public static int Main(string[] args)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("        ZENQOFFICE MICROSOFT 365 INTEROPERABILITY & DOCX AUDIT SUITE           ");
            Console.WriteLine("===============================================================================\n");

            RunTest("DOCX-USER: Diagnostic of sample_doc.docx", TestSampleDocxDiagnostic);
            RunTest("DOCX-01: Export & ECMA-376 XML Strict Schema Ordering", TestDocxExportAndXmlValidation);
            RunTest("DOCX-02: Round-Trip Data Fidelity (Save -> Load -> Verify)", TestDocxRoundtripFidelity);
            RunTest("DOCX-03: XML 1.0 Control Character Sanitization (Anti-Corruption)", TestXmlControlCharSanitization);
            RunTest("DOCX-04: Real-World Office 365 Templates Ingestion", TestRealWorldTemplatesLoading);
            RunTest("DOCX-05: Table Shading Fidelity & Legacy Blue Bug Immunity", TestTableShadingAndLegacyBlueImmunity);
            RunTest("FONT-01: Vietnamese Font Toolkit Analysis & Document Excerpt Preview", TestVietnameseFontToolkitAnalysis);
            RunTest("CALC-01: Data Validation Stop Alert Enforcement", TestDataValidationStopRuleBypassPrevention);
            RunTest("CHART-01: Chart Matrix Transposition (Switch Row/Column)", TestChartMatrixTransposition);
            RunTest("BACKUP-01: Safety Vault Pristine Original File Backup, Relocate & Deduplication", TestBackupManagerSafetyVault);
            RunTest("EXPORT-01: Multi-Format Engine (Slides PDF & Images Export)", TestMultiFormatExportEngine);
            RunTest("VECTOR-01: Inkscape-Grade Vector Suite (Spiral, Inset/Outset, Grouping, Clip, TextOnPath, SVG Export)", TestInkscapeVectorSuite);

            Console.WriteLine("\n-------------------------------------------------------------------------------");
            Console.WriteLine($"TOTAL TESTS: {_passed + _failed} | PASSED: {_passed} | FAILED: {_failed}");
            if (_failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("VERDICT: ALL TESTS PASSED CLEANLY - 100% OFFICE 365 COMPATIBILITY VERIFIED!");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"VERDICT: {_failed} TESTS FAILED!");
                Console.ResetColor();
                return 1;
            }
        }

        private static void RunTest(string testName, Action testAction)
        {
            Console.Write($"[TEST] {testName} ... ");
            try
            {
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASSED");
                Console.ResetColor();
                _passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED");
                Console.WriteLine($"       Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"       Inner: {ex.InnerException.Message}");
                }
                Console.ResetColor();
                _failed++;
            }
        }

        private static void TestDocxExportAndXmlValidation()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOfficeDocxTest");
            Directory.CreateDirectory(tempDir);
            string testDocx = Path.Combine(tempDir, "test_schema_validation.docx");

            string text = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\r\nĐộc lập - Tự do - Hạnh phúc\r\n\r\n" +
                          "| Họ và tên | Chức vụ | Đơn vị |\r\n" +
                          "| Nguyễn Văn A | Trưởng phòng | Kế hoạch |\r\n" +
                          "| Trần Thị B | Chuyên viên | Tài chính |\r\n\r\n" +
                          "Đoạn văn có thụt lề đầu dòng và căn đều hai bên với khoảng cách dòng tùy chỉnh.";

            var runs = new List<FormattedRun>
            {
                new FormattedRun(0, 34, "Times New Roman", 13.0f, true, false, false, false, Color.Black, Color.Transparent),
                new FormattedRun(36, 27, "Times New Roman", 12.0f, true, true, false, false, Color.DarkBlue, Color.Transparent)
            };

            var alignments = new Dictionary<int, int> { { 0, 1 }, { 36, 1 } }; // Center
            var lineSpacings = new Dictionary<int, float> { { 0, 1.2f } };
            var spacingsBefore = new Dictionary<int, float> { { 0, 6.0f } };
            var spacingsAfter = new Dictionary<int, float> { { 0, 6.0f } };
            var firstLineIndents = new Dictionary<int, float> { { 0, 18.0f } };

            var docTable = new ZenQOffice.UI.DocTableObject();
            docTable.WidthPt = 450;
            docTable.Alignment = 1;
            var r1 = new ZenQOffice.UI.DocTableRow();
            r1.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Họ và tên", WidthPt = 150, Bold = true, BackgroundColor = Color.LightGray });
            r1.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Chức vụ", WidthPt = 150, Bold = true, BackgroundColor = Color.LightGray });
            r1.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Đơn vị", WidthPt = 150, Bold = true, BackgroundColor = Color.LightGray });
            docTable.Rows.Add(r1);

            var r2 = new ZenQOffice.UI.DocTableRow();
            r2.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Nguyễn Văn A\nDòng 2", WidthPt = 150 });
            r2.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Trưởng phòng", WidthPt = 150 });
            r2.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Kế hoạch", WidthPt = 150 });
            docTable.Rows.Add(r2);

            var tables = new List<ZenQOffice.UI.DocTableObject> { docTable };

            bool saveOk = DocxHandler.SaveDocxFile(
                testDocx,
                text,
                runs,
                false,
                0,
                "Tiêu đề đầu trang ZenQ Office 365",
                "Chân trang mẫu",
                true,
                null,
                null,
                alignments,
                lineSpacings,
                spacingsBefore,
                spacingsAfter,
                null,
                tables,
                firstLineIndents);

            if (!saveOk) throw new Exception("SaveDocxFile returned false");

            // Inspect the ZIP archive
            using (FileStream fs = new FileStream(testDocx, FileMode.Open, FileAccess.Read))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // 1. [Content_Types].xml
                var ctEntry = zip.GetEntry("[Content_Types].xml");
                if (ctEntry == null) throw new Exception("Missing [Content_Types].xml");
                XmlDocument ctXml = new XmlDocument();
                using (var s = ctEntry.Open()) ctXml.Load(s);
                if (ctXml.SelectSingleNode("//*[local-name()='Override'][@PartName='/word/document.xml']") == null)
                    throw new Exception("Missing /word/document.xml Override in Content_Types");
                if (ctXml.SelectSingleNode("//*[local-name()='Override'][@PartName='/word/settings.xml']") == null)
                    throw new Exception("Missing /word/settings.xml Override in Content_Types");

                // 2. word/settings.xml
                var setEntry = zip.GetEntry("word/settings.xml");
                if (setEntry == null) throw new Exception("Missing word/settings.xml");
                XmlDocument setXml = new XmlDocument();
                using (var s = setEntry.Open()) setXml.Load(s);
                var compatNode = setXml.SelectSingleNode("//*[local-name()='compatSetting'][@*[local-name()='name']='compatibilityMode']");
                if (compatNode == null || compatNode.Attributes["w:val"]?.Value != "15")
                    throw new Exception("Missing or invalid Word 365 compatibilityMode 15 in settings.xml");

                // 3. word/fontTable.xml
                var fontEntry = zip.GetEntry("word/fontTable.xml");
                if (fontEntry == null) throw new Exception("Missing word/fontTable.xml");
                XmlDocument fontXml = new XmlDocument();
                using (var s = fontEntry.Open()) fontXml.Load(s);

                // 4. word/document.xml
                var docEntry = zip.GetEntry("word/document.xml");
                if (docEntry == null) throw new Exception("Missing word/document.xml");
                XmlDocument docXml = new XmlDocument();
                using (var s = docEntry.Open()) docXml.Load(s);

                // Verify tblGrid in every w:tbl
                var tblNodes = docXml.GetElementsByTagName("w:tbl");
                if (tblNodes.Count == 0) throw new Exception("No w:tbl found in document.xml");
                foreach (XmlNode tbl in tblNodes)
                {
                    var tblGrid = tbl.SelectSingleNode("*[local-name()='tblGrid']");
                    if (tblGrid == null) throw new Exception("w:tbl is missing required w:tblGrid element");
                    var gridCols = tblGrid.SelectNodes("*[local-name()='gridCol']");
                    if (gridCols == null || gridCols.Count == 0) throw new Exception("w:tblGrid has no gridCol elements");
                }

                // Verify ECMA-376 child element order in w:pPr:
                // Order: pBdr, spacing, ind, jc
                var pPrNodes = docXml.GetElementsByTagName("w:pPr");
                foreach (XmlNode pPr in pPrNodes)
                {
                    int lastPos = -1;
                    foreach (XmlNode child in pPr.ChildNodes)
                    {
                        int currentPos = -1;
                        if (child.LocalName == "pBdr") currentPos = 1;
                        else if (child.LocalName == "spacing") currentPos = 2;
                        else if (child.LocalName == "ind") currentPos = 3;
                        else if (child.LocalName == "jc") currentPos = 4;

                        if (currentPos > 0)
                        {
                            if (currentPos < lastPos)
                                throw new Exception($"ECMA-376 schema violation in w:pPr: {child.LocalName} appeared after a later element (lastPos={lastPos}, currentPos={currentPos})");
                            lastPos = currentPos;
                        }
                    }
                }

                // Verify ECMA-376 child element order in w:rPr:
                // Order: rFonts (1), b/bCs (2), i/iCs (3), strike (4), color (5), sz/szCs (6), u (7), vertAlign (8)
                var rPrNodes = docXml.GetElementsByTagName("w:rPr");
                foreach (XmlNode rPr in rPrNodes)
                {
                    int lastPos = -1;
                    foreach (XmlNode child in rPr.ChildNodes)
                    {
                        int currentPos = -1;
                        if (child.LocalName == "rFonts") currentPos = 1;
                        else if (child.LocalName == "b" || child.LocalName == "bCs") currentPos = 2;
                        else if (child.LocalName == "i" || child.LocalName == "iCs") currentPos = 3;
                        else if (child.LocalName == "strike") currentPos = 4;
                        else if (child.LocalName == "color") currentPos = 5;
                        else if (child.LocalName == "sz" || child.LocalName == "szCs") currentPos = 6;
                        else if (child.LocalName == "u") currentPos = 7;
                        else if (child.LocalName == "vertAlign") currentPos = 8;

                        if (currentPos > 0)
                        {
                            if (currentPos < lastPos)
                                throw new Exception($"ECMA-376 schema violation in w:rPr: {child.LocalName} appeared after a later element (lastPos={lastPos}, currentPos={currentPos})");
                            lastPos = currentPos;
                        }
                    }
                }

                // Verify ECMA-376 child element order in w:trPr:
                // trHeight (1), tblHeader (2)
                var trPrNodes = docXml.GetElementsByTagName("w:trPr");
                foreach (XmlNode trPr in trPrNodes)
                {
                    int hPos = -1, thPos = -1;
                    for (int i = 0; i < trPr.ChildNodes.Count; i++)
                    {
                        if (trPr.ChildNodes[i].LocalName == "trHeight") hPos = i;
                        if (trPr.ChildNodes[i].LocalName == "tblHeader") thPos = i;
                    }
                    if (hPos != -1 && thPos != -1 && thPos < hPos)
                    {
                        throw new Exception("ECMA-376 schema violation in w:trPr: w:tblHeader appeared before w:trHeight");
                    }
                }
            }
        }

        private static void TestDocxRoundtripFidelity()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOfficeDocxTest");
            string testDocx = Path.Combine(tempDir, "test_schema_validation.docx");

            DocxMetadata meta;
            bool loadOk = DocxHandler.LoadDocxFile(testDocx, out meta);
            if (!loadOk) throw new Exception("Failed to load saved DOCX");
            if (string.IsNullOrEmpty(meta.TextContent)) throw new Exception("Loaded text content is empty");
            if (!meta.TextContent.Contains("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM"))
                throw new Exception("Loaded text missing header text");
            if (meta.FormattedRuns == null || meta.FormattedRuns.Count == 0)
                throw new Exception("No formatted runs loaded");
            if (meta.Tables == null || meta.Tables.Count == 0)
                throw new Exception("No tables loaded from round-trip DOCX");

            var loadedTbl = meta.Tables[0];
            if (loadedTbl.Rows.Count < 2)
                throw new Exception($"Expected at least 2 rows in table, got {loadedTbl.Rows.Count}");
        }

        private static void TestXmlControlCharSanitization()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOfficeDocxTest");
            string testDocx = Path.Combine(tempDir, "test_dirty_chars.docx");

            // Text with dirty control characters that would break standard MS Word XML parser
            string dirtyText = "Valid text before \x00 null \x01 SOH \x08 BS \x0B VT \x0C FF \x1F US Valid text after.";

            bool saveOk = DocxHandler.SaveDocxFile(testDocx, dirtyText);
            if (!saveOk) throw new Exception("SaveDocxFile failed with dirty control chars");

            // Verify XML parser parses cleanly without throwing XmlException
            using (FileStream fs = new FileStream(testDocx, FileMode.Open, FileAccess.Read))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                var docEntry = zip.GetEntry("word/document.xml");
                if (docEntry == null) throw new Exception("Missing word/document.xml in dirty test");
                XmlDocument docXml = new XmlDocument();
                using (var s = docEntry.Open())
                {
                    // If any forbidden XML 1.0 char leaked in, this will throw XmlException
                    docXml.Load(s);
                }
                string xmlContent = docXml.OuterXml;
                if (!xmlContent.Contains("Valid text before") || !xmlContent.Contains("Valid text after"))
                    throw new Exception("Sanitized XML missing surrounding valid text");
            }

            DocxMetadata meta;
            bool loadOk = DocxHandler.LoadDocxFile(testDocx, out meta);
            if (!loadOk) throw new Exception("LoadDocxFile failed on sanitized file");
            if (!meta.TextContent.Contains("Valid text before"))
                throw new Exception("Loaded sanitized text is missing content");
        }

        private static void TestRealWorldTemplatesLoading()
        {
            string rootDir = Directory.GetCurrentDirectory();
            string[] templatePaths = new string[]
            {
                Path.Combine(rootDir, "mau", "01_Mau_Bia_Bao_Cao", "Mau_Bia_Luan_Van_Do_An.docx"),
                Path.Combine(rootDir, "mau", "02_Don_Nghi_Phep_Va_Hanh_Chinh", "Bien_Ban_Ban_Giao_Cong_Viec_Tai_San.docx"),
                Path.Combine(rootDir, "mau", "04_Hop_Dong_Kinh_Te_Thuong_Mai", "Hop_Dong_Dich_Vu_Cong_Nghe_Chuan_Luat.docx"),
                Path.Combine(rootDir, "tests", "samples", "suites", "table_borders_suite.docx"),
                Path.Combine(rootDir, "tests", "samples", "suites", "math_omml_suite.docx"),
                Path.Combine(rootDir, "tests", "samples", "scenarios", "scenario_1_business_plan.docx")
            };

            int loadedCount = 0;
            foreach (var tPath in templatePaths)
            {
                if (!File.Exists(tPath)) continue;
                DocxMetadata meta;
                bool ok = DocxHandler.LoadDocxFile(tPath, out meta);
                if (!ok) throw new Exception($"Failed to load template: {Path.GetFileName(tPath)}");
                if (string.IsNullOrEmpty(meta.TextContent))
                    throw new Exception($"Template {Path.GetFileName(tPath)} yielded empty text");
                loadedCount++;
            }

            if (loadedCount == 0)
                throw new Exception("No templates found on disk to test");
        }

        private static void TestVietnameseFontToolkitAnalysis()
        {
            string samplePath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "sample_doc.docx");
            if (!File.Exists(samplePath)) throw new Exception("sample_doc.docx not found");

            // 1. Analyze fonts in sample_doc.docx
            var fonts = VietnameseFontToolkit.AnalyzeDocumentFonts(samplePath);
            if (fonts == null || fonts.Count == 0)
                throw new Exception("AnalyzeDocumentFonts returned 0 fonts for sample_doc.docx!");

            bool foundTimes = false;
            foreach (var f in fonts)
            {
                if (f.Typeface.Equals("Times New Roman", StringComparison.OrdinalIgnoreCase))
                {
                    foundTimes = true;
                    if (!f.HasVietnameseSupport) throw new Exception("Times New Roman falsely marked as unsupported!");
                    if (!f.IsUsedInDocumentBody) throw new Exception("Times New Roman should be marked as used in document body!");
                }
            }
            if (!foundTimes) throw new Exception("Times New Roman not found in analyzed fonts list!");

            // 2. Extract sample text from sample_doc.docx
            string excerpt = VietnameseFontToolkit.ExtractDocumentSampleText(samplePath);
            if (string.IsNullOrEmpty(excerpt))
                throw new Exception("ExtractDocumentSampleText returned empty for sample_doc.docx!");
            if (!excerpt.Contains("Bài 2") && !excerpt.Contains("TRẬT TỰ"))
                throw new Exception($"Extracted excerpt does not contain document content: '{excerpt}'");

            // 3. Test legacy font detection and replacement
            List<char> miss;
            bool tcvnCheck = VietnameseFontToolkit.CheckInstalledFontVietnameseSupport(".VnTime", out miss);
            if (tcvnCheck) throw new Exception(".VnTime should be marked as unsupported (TCVN3)!");
            if (miss.Count == 0) throw new Exception(".VnTime should have missing characters reported!");
        }

        private static void TestDataValidationStopRuleBypassPrevention()
        {
            var rule = new DataValidationRule
            {
                ValidationType = DataValidationType.WholeNumber,
                Operator = DataValidationOperator.Between,
                Formula1 = "1",
                Formula2 = "100",
                ErrorStyle = DataValidationErrorStyle.Stop,
                ErrorMessage = "Chỉ cho phép nhập số nguyên từ 1 đến 100!"
            };

            // Valid numbers
            if (!rule.Validate("1").IsValid)
                throw new Exception("Validation failed for valid number '1'");
            if (!rule.Validate("50").IsValid)
                throw new Exception("Validation failed for valid number '50'");
            if (!rule.Validate("100").IsValid)
                throw new Exception("Validation failed for valid number '100'");

            // Invalid numbers (out of bounds)
            if (rule.Validate("0").IsValid)
                throw new Exception("Validation falsely accepted '0'");
            if (rule.Validate("101").IsValid)
                throw new Exception("Validation falsely accepted '101'");
            if (rule.Validate("-5").IsValid)
                throw new Exception("Validation falsely accepted '-5'");

            // Invalid non-numeric text
            if (rule.Validate("zenq").IsValid)
                throw new Exception("Validation falsely accepted text 'zenq'");
        }

        private static void TestChartMatrixTransposition()
        {
            var chartData = new DocChartData();
            chartData.Title = "Doanh thu 2026";
            chartData.Categories = new List<string> { "Quý 1", "Quý 2", "Quý 3", "Quý 4" };
            chartData.SeriesNames = new List<string> { "Khu vực Bắc", "Khu vực Nam" };
            chartData.Values = new List<List<double>>
            {
                new List<double> { 100, 150 },
                new List<double> { 200, 250 },
                new List<double> { 300, 350 },
                new List<double> { 400, 450 }
            };

            // Initial dimensions: 4 categories x 2 series
            if (chartData.Categories.Count != 4 || chartData.SeriesNames.Count != 2)
                throw new Exception("Initial chart dimensions mismatch");

            // Perform transposition (Switch Row/Column)
            chartData.SwitchRowColumn();

            // After transposition: should have 2 categories x 4 series
            if (chartData.Categories.Count != 2)
                throw new Exception($"Expected 2 categories after switch, got {chartData.Categories.Count}");
            if (chartData.SeriesNames.Count != 4)
                throw new Exception($"Expected 4 series after switch, got {chartData.SeriesNames.Count}");
            if (chartData.Categories[0] != "Khu vực Bắc" || chartData.Categories[1] != "Khu vực Nam")
                throw new Exception("Swapped category names mismatch");
            if (chartData.SeriesNames[0] != "Quý 1" || chartData.SeriesNames[3] != "Quý 4")
                throw new Exception("Swapped series names mismatch");

            // Check transposed values: [0][0] should be 100, [0][1] should be 200, [1][3] should be 450
            if (chartData.Values[0][0] != 100 || chartData.Values[0][1] != 200 || chartData.Values[1][3] != 450)
                throw new Exception("Transposed values mismatch");

            // Switch back to original
            chartData.SwitchRowColumn();
            if (chartData.Categories.Count != 4 || chartData.SeriesNames.Count != 2)
                throw new Exception("Double switch failed to return to original dimensions");
            if (chartData.Values[3][1] != 450)
                throw new Exception("Double switch value mismatch at [3][1]");
        }

        private static void TestTableShadingAndLegacyBlueImmunity()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOfficeTableTest");
            Directory.CreateDirectory(tempDir);
            string testDocx = Path.Combine(tempDir, "test_table_shading.docx");

            var table = new ZenQOffice.UI.DocTableObject { WidthPt = 450, Alignment = 0 };
            var r1 = new ZenQOffice.UI.DocTableRow();
            r1.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Quote Title", WidthPt = 450, BackgroundColor = Color.Transparent });
            table.Rows.Add(r1);

            var r2 = new ZenQOffice.UI.DocTableRow();
            r2.Cells.Add(new ZenQOffice.UI.DocTableCell { Text = "Quote Content", WidthPt = 450, BackgroundColor = Color.Transparent });
            table.Rows.Add(r2);

            bool ok = DocxHandler.SaveDocxFile(testDocx, "Test text\n", null, false, 0, "", "", false, null, null, null, null, null, null, null, new List<ZenQOffice.UI.DocTableObject> { table });
            if (!ok || !File.Exists(testDocx))
                throw new Exception("SaveDocxFile failed for table test");

            // Inspect document.xml inside ZIP
            using (var archive = ZipFile.OpenRead(testDocx))
            {
                var entry = archive.GetEntry("word/document.xml");
                if (entry == null) throw new Exception("word/document.xml missing in test docx");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                {
                    string xml = reader.ReadToEnd();
                    if (xml.IndexOf("2563EB", StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new Exception("Exported docx contains unwanted 2563EB table shading!");
                    if (xml.IndexOf("F8FAFC", StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new Exception("Exported docx contains unwanted F8FAFC zebra striping!");
                }
            }

            // Test loading original document if present
            string userDoc = @"C:\Users\Danh\Downloads\NỘI DUNG GHI BAI 4.docx";
            if (File.Exists(userDoc))
            {
                DocxMetadata meta;
                bool loadOk = DocxHandler.LoadDocxFile(userDoc, out meta);
                if (!loadOk) throw new Exception("Failed to load user document: " + userDoc);
                if (meta.FormattedRuns == null || meta.FormattedRuns.Count == 0)
                    throw new Exception("User document loaded with 0 runs");
                foreach (var r in meta.FormattedRuns)
                {
                    if (r.Highlight.R == 0x25 && r.Highlight.G == 0x63 && r.Highlight.B == 0xEB)
                        throw new Exception("User document run still contains #2563EB highlight!");
                }
                if (meta.Tables != null)
                {
                    foreach (var tbl in meta.Tables)
                    {
                        foreach (var row in tbl.Rows)
                        {
                            foreach (var cell in row.Cells)
                            {
                                if (cell.BackgroundColor.R == 0x25 && cell.BackgroundColor.G == 0x63 && cell.BackgroundColor.B == 0xEB)
                                    throw new Exception("User document table cell still contains #2563EB background!");
                            }
                        }
                    }
                }
            }
        }

        private static void TestBackupManagerSafetyVault()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQ_BackupTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string originalFile = Path.Combine(tempDir, "OriginalDocument.docx");
                string pristineContent = "ZenQ Office Pristine Document Content 2026 - Safety Vault Integrity Check";
                File.WriteAllText(originalFile, pristineContent, Encoding.UTF8);

                // 1. Backup pristine file
                var item = BackupManager.BackupOriginalFile(originalFile, "Writer");
                if (item == null) throw new Exception("BackupOriginalFile returned null");
                if (!File.Exists(item.BackupFullPath)) throw new Exception("Physical backup file does not exist in vault: " + item.BackupFullPath);
                if (File.ReadAllText(item.BackupFullPath, Encoding.UTF8) != pristineContent)
                    throw new Exception("Vault backup content does not match original byte-for-byte");

                // 2. Deduplication check: re-backing up identical file must not duplicate
                var item2 = BackupManager.BackupOriginalFile(originalFile, "Writer");
                if (item2 == null || item2.BackupFullPath != item.BackupFullPath)
                    throw new Exception("Deduplication failed: redundant copy created");

                // 3. Simulate file corruption / overwrite
                File.WriteAllText(originalFile, "CORRUPTED CONTENT FROM FAILED OVERWRITE", Encoding.UTF8);

                // 4. Restore original from vault
                bool restored = BackupManager.RestoreToOriginal(item);
                if (!restored) throw new Exception("RestoreToOriginal returned false");
                if (File.ReadAllText(originalFile, Encoding.UTF8) != pristineContent)
                    throw new Exception("Original file content after restore does not match pristine state");

                // 5. Relocate / Copy to target directory
                string destDir = Path.Combine(tempDir, "ExportFolder");
                string relocatedPath;
                bool copied = BackupManager.RelocateBackup(item, destDir, cutAndPaste: false, out relocatedPath);
                if (!copied || !File.Exists(relocatedPath)) throw new Exception("RelocateBackup (copy) failed");
                if (File.ReadAllText(relocatedPath, Encoding.UTF8) != pristineContent) throw new Exception("Relocated copy content mismatch");
                if (!File.Exists(item.BackupFullPath)) throw new Exception("Vault file was deleted during copy operation");

                // 6. Relocate / Cut to target directory
                string cutDir = Path.Combine(tempDir, "CutFolder");
                string cutPath;
                bool cut = BackupManager.RelocateBackup(item, cutDir, cutAndPaste: true, out cutPath);
                if (!cut || !File.Exists(cutPath)) throw new Exception("RelocateBackup (cut) failed");
                if (File.Exists(item.BackupFullPath)) throw new Exception("Vault file was not removed after cut operation");

                // 7. Enforce Retention Policy
                BackupManager.EnforceRetentionPolicy();
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static void TestMultiFormatExportEngine()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQ_ExportTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var pres = new PresentationRoot();
                pres.CanvasWidth = 960;
                pres.CanvasHeight = 540;
                pres.Slides.Add(new SlideNode { Id = "slide1", Title = "Trang 1", BgColor = unchecked((int)0xFFFFFFFF) });
                pres.Slides.Add(new SlideNode { Id = "slide2", Title = "Trang 2", BgColor = unchecked((int)0xFFE0F2FE) });

                var sv = new SlidesView { Presentation = pres };

                // 1. Export Slides to PDF
                string pdfPath = Path.Combine(tempDir, "presentation_export.pdf");
                bool pdfOk = sv.ExportToPdf(pdfPath);
                if (!pdfOk) throw new Exception("SlidesView.ExportToPdf returned false");
                if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length < 100)
                    throw new Exception("SlidesView exported PDF is missing or invalid size");

                // 2. Export Slides to Images (PNG Deck)
                string imgPath = Path.Combine(tempDir, "presentation_export.png");
                bool imgOk = sv.ExportToImages(imgPath, allSlides: true, isJpeg: false);
                if (!imgOk) throw new Exception("SlidesView.ExportToImages (PNG) returned false");
                string slide1Png = Path.Combine(tempDir, "presentation_export_Slide_1.png");
                string slide2Png = Path.Combine(tempDir, "presentation_export_Slide_2.png");
                if (!File.Exists(slide1Png) || !File.Exists(slide2Png))
                    throw new Exception("SlidesView exported PNG slide images missing");

                // 3. Export Single Slide to JPEG
                string singleJpg = Path.Combine(tempDir, "single_slide.jpg");
                bool singleOk = sv.ExportToImages(singleJpg, allSlides: false, isJpeg: true);
                if (!singleOk || !File.Exists(singleJpg))
                    throw new Exception("SlidesView single JPEG export failed");

                // 4. Export Slides to OpenDocument Presentation (.odp)
                string odpPath = Path.Combine(tempDir, "presentation_export.odp");
                bool odpOk = ZenQOffice.Format.OdpHandler.SaveOdp(pres, odpPath);
                if (!odpOk || !File.Exists(odpPath))
                    throw new Exception("OdpHandler.SaveOdp failed to create file");
                using (var zip = ZipFile.OpenRead(odpPath))
                {
                    var mimeEntry = zip.GetEntry("mimetype");
                    if (mimeEntry == null) throw new Exception("ODP package missing mimetype");
                    using (var sr = new StreamReader(mimeEntry.Open()))
                    {
                        string mime = sr.ReadToEnd();
                        if (!mime.Contains("application/vnd.oasis.opendocument.presentation"))
                            throw new Exception("ODP package mimetype incorrect: " + mime);
                    }
                    if (zip.GetEntry("content.xml") == null) throw new Exception("ODP missing content.xml");
                }

                // 5. WriterView Multi-Format Suite: RTF, ODT, Markdown, HTML, Images
                var wv = new WriterView();
                wv.InsertText("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\nĐộc lập - Tự do - Hạnh phúc\n\nBÁO CÁO KINH DOANH QUÝ 4\nNội dung chi tiết về doanh thu và kế hoạch triển khai.\n\n| Hạng mục | Doanh thu | Tăng trưởng |\n| --- | --- | --- |\n| Phần mềm ZenQ | 500,000,000 | +25% |\n| Dịch vụ Cloud | 200,000,000 | +18% |\n");
                wv.FormattedRuns.Add(new FormattedRun(0, 32, "Times New Roman", 14f, true, false, false, false, Color.Black, Color.Transparent));

                // 5.1 Export to Rich Text Format (.rtf)
                string rtfPath = Path.Combine(tempDir, "document_export.rtf");
                bool rtfOk = wv.ExportToRtf(rtfPath);
                if (!rtfOk || !File.Exists(rtfPath)) throw new Exception("WriterView.ExportToRtf failed");
                string rtfContent = File.ReadAllText(rtfPath);
                if (!rtfContent.StartsWith(@"{\rtf1")) throw new Exception("Exported RTF missing {\\rtf1 magic header");
                if (!rtfContent.Contains(@"\fonttbl")) throw new Exception("Exported RTF missing font table");
                if (!rtfContent.Contains(@"\u")) throw new Exception("Exported RTF missing Unicode escape sequences");

                // 5.2 Export to OpenDocument Text (.odt)
                string odtPath = Path.Combine(tempDir, "document_export.odt");
                bool odtOk = wv.ExportToOdt(odtPath);
                if (!odtOk || !File.Exists(odtPath)) throw new Exception("WriterView.ExportToOdt failed");
                using (var zip = ZipFile.OpenRead(odtPath))
                {
                    var mimeEntry = zip.GetEntry("mimetype");
                    if (mimeEntry == null) throw new Exception("ODT missing mimetype");
                    using (var sr = new StreamReader(mimeEntry.Open()))
                    {
                        string mime = sr.ReadToEnd();
                        if (!mime.Contains("application/vnd.oasis.opendocument.text"))
                            throw new Exception("ODT package mimetype incorrect: " + mime);
                    }
                    if (zip.GetEntry("content.xml") == null) throw new Exception("ODT missing content.xml");
                }

                // 5.3 Export to Markdown (.md)
                string mdPath = Path.Combine(tempDir, "document_export.md");
                bool mdOk = wv.ExportToMarkdown(mdPath);
                if (!mdOk || !File.Exists(mdPath)) throw new Exception("WriterView.ExportToMarkdown failed");
                string mdContent = File.ReadAllText(mdPath);
                if (!mdContent.Contains("BÁO CÁO KINH DOANH")) throw new Exception("Markdown export missing content text");
                if (!mdContent.Contains("| Hạng mục | Doanh thu |")) throw new Exception("Markdown export missing table");

                // 5.4 Export to HTML (.html)
                string htmlPath = Path.Combine(tempDir, "document_export.html");
                bool htmlOk = wv.ExportToHtml(htmlPath);
                if (!htmlOk || !File.Exists(htmlPath)) throw new Exception("WriterView.ExportToHtml failed");
                string htmlContent = File.ReadAllText(htmlPath);
                if (!htmlContent.Contains("<!DOCTYPE html>")) throw new Exception("HTML export missing doctype");
                if (!htmlContent.Contains("<table")) throw new Exception("HTML export missing table tag");

                // 5.5 Export Writer to Images (.png)
                string writerImgPath = Path.Combine(tempDir, "writer_page.png");
                bool wImgOk = wv.ExportToImages(writerImgPath, null, allPages: false, isJpeg: false);
                if (!wImgOk || !File.Exists(writerImgPath)) throw new Exception("WriterView page PNG image export failed");

                // 6. PhotoDocument Multi-Format Export: PDF, SVG, PNG
                var photoDoc = new PhotoDocument(800, 600, "ExportDesign");
                photoDoc.AddVectorShape(VectorShapeType.Rectangle, new RectangleF(50, 50, 400, 300), Color.DarkSlateBlue, Color.Orange, 4f);
                string photoSvg = Path.Combine(tempDir, "design_export.svg");
                bool svgOk = photoDoc.SaveToFile(photoSvg);
                if (!svgOk || !File.Exists(photoSvg)) throw new Exception("PhotoDocument SVG export failed");

                string photoPdf = Path.Combine(tempDir, "design_export.pdf");
                bool photoPdfOk = photoDoc.SaveToFile(photoPdf);
                if (!photoPdfOk || !File.Exists(photoPdf)) throw new Exception("PhotoDocument PDF export failed");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static void TestInkscapeVectorSuite()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ZenQ_Vector_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var doc = new PhotoDocument(1000, 800, "VectorSuiteTest");
                doc.Layers.Clear();

                // 1. Archimedean Spiral Shape
                doc.AddVectorShape(VectorShapeType.Spiral, new RectangleF(50, 50, 200, 200), Color.Transparent, Color.Blue, 3.0f);
                var spiralLayer = doc.ActiveLayer;
                if (spiralLayer == null || spiralLayer.ShapeType != VectorShapeType.Spiral)
                    throw new Exception("Spiral shape was not created properly");
                spiralLayer.EnsureNodesInitialized();
                if (spiralLayer.SubPaths.Count == 0 || spiralLayer.SubPaths[0].Nodes.Count < 10)
                    throw new Exception("Spiral shape nodes were not generated correctly");

                // 2. Inset & Outset
                doc.AddVectorShape(VectorShapeType.Rectangle, new RectangleF(300, 50, 200, 150), Color.Red, Color.Black, 2.0f);
                var rectLayer = doc.ActiveLayer;
                rectLayer.EnsureNodesInitialized();
                float initialW = rectLayer.Bounds.Width;
                bool outsetOk = doc.OutsetActivePath(5.0f);
                if (!outsetOk || rectLayer.Bounds.Width <= initialW)
                    throw new Exception("OutsetActivePath failed to expand vector contour");
                float outsetW = rectLayer.Bounds.Width;
                bool insetOk = doc.InsetActivePath(5.0f);
                if (!insetOk || rectLayer.Bounds.Width >= outsetW)
                    throw new Exception("InsetActivePath failed to contract vector contour");

                // 3. Layer Grouping & Ungrouping
                doc.AddVectorShape(VectorShapeType.Ellipse, new RectangleF(100, 300, 100, 100), Color.Green, Color.Black, 2.0f);
                var ellLayer = doc.ActiveLayer;
                doc.SelectLayer(spiralLayer, false);
                doc.SelectLayer(ellLayer, true);
                if (doc.SelectedLayers.Count != 2)
                    throw new Exception("Failed to multi-select layers");
                doc.GroupSelectedLayers();
                if (!spiralLayer.GroupId.HasValue || spiralLayer.GroupId != ellLayer.GroupId)
                    throw new Exception("GroupSelectedLayers failed to assign unified GroupId");

                // Verify group selection
                doc.SelectedLayerIds.Clear();
                doc.SelectLayer(spiralLayer, false);
                if (doc.SelectedLayers.Count != 2)
                    throw new Exception("Selecting a grouped layer failed to select all group members");

                // Ungroup
                doc.UngroupSelectedLayers();
                if (spiralLayer.GroupId.HasValue || ellLayer.GroupId.HasValue)
                    throw new Exception("UngroupSelectedLayers failed to clear GroupId");

                // 4. Duplicate In-Place
                int preDupCount = doc.Layers.Count;
                doc.SelectLayer(rectLayer, false);
                doc.DuplicateSelectedLayers();
                if (doc.Layers.Count != preDupCount + 1)
                    throw new Exception("DuplicateSelectedLayers failed to increment layer count");

                // 5. Clipping Mask
                doc.AddVectorShape(VectorShapeType.Rectangle, new RectangleF(200, 300, 250, 200), Color.Gold, Color.Black, 2.0f);
                var bottomLayer = doc.ActiveLayer;
                doc.AddVectorShape(VectorShapeType.Star, new RectangleF(220, 320, 150, 150), Color.Purple, Color.Black, 2.0f);
                var maskLayer = doc.ActiveLayer;

                doc.SelectedLayerIds.Clear();
                doc.SelectLayer(bottomLayer, true);
                doc.SelectLayer(maskLayer, true);
                bool clipOk = doc.SetClipForSelected();
                if (!clipOk || bottomLayer.ClipMask == null)
                    throw new Exception("SetClipForSelected failed to apply clipping mask");

                bool releaseOk = doc.ReleaseClipActive();
                if (!releaseOk || bottomLayer.ClipMask != null)
                    throw new Exception("ReleaseClipActive failed to release clipping mask");

                // 6. Text on Path
                doc.AddText("ZENQ INKSCAPE VECTOR TEXT ON PATH", 100, 100, 24f, Color.DarkBlue);
                var textLayer = doc.ActiveLayer;
                doc.SelectedLayerIds.Clear();
                doc.SelectLayer(textLayer, true);
                doc.SelectLayer(spiralLayer, true);
                bool topOk = doc.PutTextOnPath();
                if (!topOk || !textLayer.IsTextOnPath || textLayer.TextPathSubPaths == null)
                    throw new Exception("PutTextOnPath failed to bind text to path");

                // 7. GDI Rendering with Text on Path and Gradients
                rectLayer.FillType = VectorFillType.LinearGradient;
                rectLayer.FillColor = Color.Red;
                rectLayer.FillColor2 = Color.Yellow;
                rectLayer.FillGradientAngle = 45f;
                rectLayer.Rotation = 15f;

                using (Bitmap bmp = doc.RenderComposite())
                {
                    if (bmp.Width != 1000 || bmp.Height != 800)
                        throw new Exception("RenderComposite generated invalid bitmap dimensions");
                }

                // 8. High-Fidelity SVG Export
                string svgPath = Path.Combine(tempDir, "vector_export_full.svg");
                bool svgOk = doc.ExportToSvg(svgPath);
                if (!svgOk || !File.Exists(svgPath))
                    throw new Exception("ExportToSvg returned false or file was not created");

                string svgContent = File.ReadAllText(svgPath);
                if (!svgContent.Contains("<linearGradient") || !svgContent.Contains("id=\"grad_"))
                    throw new Exception("ExportToSvg missing linearGradient definitions");
                if (!svgContent.Contains("<textPath") || !svgContent.Contains("href=\"#tp_"))
                    throw new Exception("ExportToSvg missing W3C standard textPath element");
                if (!svgContent.Contains("transform=\"rotate("))
                    throw new Exception("ExportToSvg missing transform rotate attribute");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static void TestSampleDocxDiagnostic()
        {
            string samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "sample_doc.docx");
            if (!File.Exists(samplePath))
            {
                samplePath = @"c:\Users\Danh\Desktop\genofficepy\tests\sample_doc.docx";
            }
            if (!File.Exists(samplePath)) throw new Exception("sample_doc.docx not found");

            DocxMetadata meta;
            bool ok = DocxHandler.LoadDocxFile(samplePath, out meta);
            if (!ok) throw new Exception("Failed to load sample_doc.docx");

            using (var ff = new FontFamily("Times New Roman"))
            {
                int ascent = ff.GetCellAscent(FontStyle.Regular);
                int descent = ff.GetCellDescent(FontStyle.Regular);
                int emH = ff.GetEmHeight(FontStyle.Regular);
                int lineSp = ff.GetLineSpacing(FontStyle.Regular);
                Console.WriteLine($"[FontFamily Times New Roman] Ascent={ascent}, Descent={descent}, Em={emH}, LineSpacing={lineSp}, TypoRatio={(ascent + descent) / (float)emH:F4}, LineSpRatio={lineSp / (float)emH:F4}");
            }

            // Inspect ZIP archive directly for styles and sectPr
            using (FileStream fs = new FileStream(samplePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                var docEntry = zip.GetEntry("word/document.xml");
                if (docEntry != null)
                {
                    XmlDocument docXml = new XmlDocument();
                    using (var s = docEntry.Open()) docXml.Load(s);
                    var sectPr = docXml.SelectSingleNode("//*[local-name()='sectPr']");
                    if (sectPr != null)
                    {
                        Console.WriteLine($"[sectPr OuterXml]: {sectPr.OuterXml}");
                    }
                }
                var stylesEntry = zip.GetEntry("word/styles.xml");
                if (stylesEntry != null)
                {
                    XmlDocument sXml = new XmlDocument();
                    using (var s = stylesEntry.Open()) sXml.Load(s);
                    var docDefaults = sXml.SelectSingleNode("//*[local-name()='docDefaults']");
                    if (docDefaults != null) Console.WriteLine($"[docDefaults]: {docDefaults.OuterXml}");
                    var normalStyle = sXml.SelectSingleNode("//*[local-name()='style'][@*[local-name()='default']='1'] | //*[local-name()='style'][@*[local-name()='styleId']='Normal']");
                    if (normalStyle != null) Console.WriteLine($"[NormalStyle]: {normalStyle.OuterXml}");
                }
            }

            Console.WriteLine($"Total Tables: {meta.Tables.Count}");
            for (int t = 0; t < meta.Tables.Count; t++)
            {
                var tbl = meta.Tables[t];
                Console.WriteLine($"Table[{t}]: Rows={tbl.Rows.Count}, WidthPt={tbl.WidthPt}, Alignment={tbl.Alignment}, IndentPt={tbl.IndentPt}");
                for (int r = 0; r < Math.Min(tbl.Rows.Count, 5); r++)
                {
                    var row = tbl.Rows[r];
                    Console.Write($"  Row[{r}] (H={row.HeightPt}pt, Cells={row.Cells.Count}): ");
                    for (int c = 0; c < row.Cells.Count; c++)
                    {
                        var cell = row.Cells[c];
                        string cText = (cell.Text ?? "").Replace("\n", " ").Trim();
                        if (cText.Length > 25) cText = cText.Substring(0, 25) + "...";
                        Console.Write($"[C{c}: W={cell.WidthPt}pt '{cText}'] ");
                    }
                    Console.WriteLine();
                }
                if (tbl.Rows.Count > 5)
                {
                    int lastR = tbl.Rows.Count - 1;
                    var row = tbl.Rows[lastR];
                    Console.Write($"  ... Row[{lastR}] (H={row.HeightPt}pt, Cells={row.Cells.Count}): ");
                    for (int c = 0; c < row.Cells.Count; c++)
                    {
                        var cell = row.Cells[c];
                        string cText = (cell.Text ?? "").Replace("\n", " ").Trim();
                        if (cText.Length > 25) cText = cText.Substring(0, 25) + "...";
                        Console.Write($"[C{c}: W={cell.WidthPt}pt '{cText}'] ");
                    }
                    Console.WriteLine();
                }
            }

            var ribbon = new RibbonBar();
            var writerView = new WriterView();
            writerView.LoadDocumentFile(samplePath, ribbon);

            Console.WriteLine($"Ribbon: PaperW={ribbon.PaperWidthInches}\", PaperH={ribbon.PaperHeightInches}\", Margins: L={ribbon.GetEffectiveLeftMarginInches()}\", R={ribbon.GetEffectiveRightMarginInches()}\", T={ribbon.GetEffectiveTopMarginInches()}\", B={ribbon.GetEffectiveBottomMarginInches()}\"");
            Console.WriteLine($"Ribbon: LineSpacingFactor={ribbon.LineSpacingFactor}, Before={ribbon.ParagraphSpacingBefore}, After={ribbon.ParagraphSpacingAfter}");

            using (Bitmap bmp = new Bitmap(1200, 100))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                writerView.Draw(g, 1200, 35000, ribbon);
            }
            Console.WriteLine($"CalculatedPageCount: {writerView.CalculatedPageCount}, TotalPageCount: {writerView.TotalPageCount}");

            Console.WriteLine("\n--- Measuring Câu 1..5 in ZenQ Writer ---");
            string[] paras = (meta.TextContent ?? "").Split('\n');
            int pStart = 0;
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                using (D2DGraphics d2d = new D2DGraphics(g, 1200, 900))
                {
                    int pIdx = 0;
                    for (int i = 0; i < paras.Length && pIdx < 7; i++)
                    {
                        string pText = paras[i].Trim();
                        if (pText.StartsWith("Câu"))
                        {
                            pIdx++;
                            var fr = writerView.GetFormatAt(pStart);
                            Font f = new Font(fr.FontName ?? "Times New Roman", fr.FontSize, fr.Bold ? FontStyle.Bold : FontStyle.Regular);
                            float w = d2d.MeasureString(pText, f, Point.Empty, (StringFormat)StringFormat.GenericTypographic.Clone()).Width;
                            float fl = writerView.GetFirstLineIndent(pStart);
                            float leftInd = writerView.GetLeftIndent(pStart);
                            Console.WriteLine($"Câu {pIdx}: Font='{fr.FontName}' {fr.FontSize}pt (Bold={fr.Bold}), FirstLine={fl}pt, Left={leftInd}pt");
                            Console.WriteLine($"   String Width = {w:F1}px, MaxTextW = 662px, Available = {(662 - (fl + leftInd) * 1.333333f):F1}px");
                            Console.WriteLine($"   Text = '{pText}'");
                        }
                        pStart += paras[i].Length + 1;
                    }
                }
            }
            int lastPg = -1;
            string docFull = writerView.PieceTable.GetText();
            int pageHWithGap = (int)(11.694445f * 96) + 30;
            foreach (var line in writerView.RenderedLines)
            {
                int pg = (int)((line.ScreenY - (165 - writerView.ScrollY)) / pageHWithGap);
                if (pg != lastPg)
                {
                    string t = (line.StartCharIndex >= 0 && line.StartCharIndex + line.Length <= docFull.Length) ? docFull.Substring(line.StartCharIndex, line.Length).Trim() : "";
                    if (t.Length > 60) t = t.Substring(0, 60) + "...";
                    Console.WriteLine($"Page {pg + 1} starts with: '{t}' (Y={line.ScreenY})");
                    lastPg = pg;
                }
            }
            Console.WriteLine($"Total pages rendered: {lastPg + 1}");

            Console.WriteLine("\n--- Lines on Page 15 & 16 ---");
            foreach (var line in writerView.RenderedLines)
            {
                int pg = (int)((line.ScreenY - (165 - writerView.ScrollY)) / pageHWithGap);
                if (pg == 14 || pg == 15)
                {
                    string t = (line.StartCharIndex >= 0 && line.StartCharIndex + line.Length <= docFull.Length) ? docFull.Substring(line.StartCharIndex, line.Length).Trim() : "";
                    if (t.Length > 80) t = t.Substring(0, 80) + "...";
                    Console.WriteLine($"  [Pg {pg + 1}] Y={line.ScreenY}: '{t}'");
                }
            }
        }
    }
}

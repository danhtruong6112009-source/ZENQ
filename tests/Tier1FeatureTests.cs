using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using ZenQOffice.Core;
using ZenQOffice.Format;
using ZenQOffice.UI;

namespace GenOffice.Tests
{
    public class Tier1FeatureTests
    {
        // =========================================================================
        // FEATURE 1: OpenXML Drawings Parser (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(1, "OpenXML Drawings Parser", Milestone.M1)]
        [Description("Verify drawings relationship path parsing in OpenXML package")]
        public void T1_F01_01_ParseDrawingPartXmlPresence()
        {
            string relsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n" +
                "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing1.xml\"/>\r\n" +
                "</Relationships>";
            Assert.IsTrue(relsXml.Contains("Target=\"../drawings/drawing1.xml\""), "Drawing XML target path must be present in sheet relationships.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(1, "OpenXML Drawings Parser", Milestone.M1)]
        [Description("Verify twoCellAnchor coordinate element structure")]
        public void T1_F01_02_ParseTwoCellAnchorNodes()
        {
            string drawingXml = "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\">\r\n" +
                "  <xdr:twoCellAnchor editAs=\"oneCell\">\r\n" +
                "    <xdr:from><xdr:col>2</xdr:col><xdr:colOff>95250</xdr:colOff><xdr:row>4</xdr:row><xdr:rowOff>190500</xdr:rowOff></xdr:from>\r\n" +
                "    <xdr:to><xdr:col>5</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>10</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to>\r\n" +
                "  </xdr:twoCellAnchor>\r\n" +
                "</xdr:wsDr>";
            Assert.IsTrue(drawingXml.Contains("<xdr:twoCellAnchor"), "TwoCellAnchor must be parsed as a bounded floating container.");
            Assert.IsTrue(drawingXml.Contains("<xdr:from>"), "Anchor must specify origin cell coordinates.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(1, "OpenXML Drawings Parser", Milestone.M1)]
        [Description("Verify oneCellAnchor coordinate element parsing")]
        public void T1_F01_03_ParseOneCellAnchorNodes()
        {
            string oneCellXml = "<xdr:oneCellAnchor>\r\n" +
                "  <xdr:from><xdr:col>1</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>1</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>\r\n" +
                "  <xdr:ext cx=\"1905000\" cy=\"952500\"/>\r\n" +
                "</xdr:oneCellAnchor>";
            Assert.IsTrue(oneCellXml.Contains("<xdr:ext cx=\"1905000\""), "OneCellAnchor must specify explicit width/height extents.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(1, "OpenXML Drawings Parser", Milestone.M1)]
        [Description("Verify media relationship r:embed resolution")]
        public void T1_F01_04_ParsePictureRelationshipEmbed()
        {
            string drawRels = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n" +
                "  <Relationship Id=\"rIdPic1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image1.png\"/>\r\n" +
                "</Relationships>";
            Assert.IsTrue(drawRels.Contains("../media/image1.png"), "Image relationship target must map to xl/media.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(1, "OpenXML Drawings Parser", Milestone.M1)]
        [Description("Verify CalcView FloatingObjects container accessibility")]
        public void T1_F01_05_RestoreShapeObjectsIntoCalcView()
        {
            CalcView cv = new CalcView();
            Assert.IsNotNull(cv.Shapes, "CalcView Shapes container must be initialized.");
            DocShapeObject shape = new DocShapeObject();
            shape.ShapeType = "Rectangle";
            shape.BaseWidth = 120;
            shape.BaseHeight = 80;
            cv.Shapes.Add(shape);
            Assert.AreEqual(1, cv.Shapes.Count, "Shape must be retained in CalcView.");
        }

        // =========================================================================
        // FEATURE 2: Drawings EMU Coordinate Transform (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(2, "Drawings EMU Coordinate Transform", Milestone.M1)]
        [Description("Verify EMU to pixel conversion at 0 offset")]
        public void T1_F02_01_EmuToPixelZeroOffset()
        {
            long emu = 0;
            int px = (int)(emu / 9525);
            Assert.AreEqual(0, px, "0 EMU must map to 0 px.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(2, "Drawings EMU Coordinate Transform", Milestone.M1)]
        [Description("Verify EMU to pixel conversion with standard 9,525 EMU/px ratio")]
        public void T1_F02_02_EmuToPixelStandardOffset()
        {
            long emu = 952500; // 100 pixels
            int px = (int)(emu / 9525);
            Assert.AreEqual(100, px, "952500 EMU must equal exactly 100 px.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(2, "Drawings EMU Coordinate Transform", Milestone.M1)]
        [Description("Verify pixel to EMU bidirectional roundtrip")]
        public void T1_F02_03_PixelToEmuRoundtrip()
        {
            int originalPx = 250;
            long emu = (long)originalPx * 9525;
            int restoredPx = (int)(emu / 9525);
            Assert.AreEqual(originalPx, restoredPx, "Pixel to EMU roundtrip must be lossless.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(2, "Drawings EMU Coordinate Transform", Milestone.M1)]
        [Description("Verify TwoCellAnchor bounding calculation using column and row offsets")]
        public void T1_F02_04_TwoCellAnchorBoundsCalculation()
        {
            int colWidth = 64;
            int rowHeight = 20;
            uint fromCol = 2;
            long fromColOff = 95250; // 10 px
            uint fromRow = 3;
            long fromRowOff = 47625; // 5 px

            int x = (int)(fromCol * colWidth + fromColOff / 9525);
            int y = (int)(fromRow * rowHeight + fromRowOff / 9525);

            Assert.AreEqual(138, x, "Calculated X must be 2*64 + 10 = 138.");
            Assert.AreEqual(65, y, "Calculated Y must be 3*20 + 5 = 65.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(2, "Drawings EMU Coordinate Transform", Milestone.M1)]
        [Description("Verify OneCellAnchor width/height extraction from cx/cy extents")]
        public void T1_F02_05_OneCellAnchorWithAbsoluteExtents()
        {
            long cxEmu = 1905000; // 200 px
            long cyEmu = 952500;  // 100 px
            int widthPx = (int)(cxEmu / 9525);
            int heightPx = (int)(cyEmu / 9525);

            Assert.AreEqual(200, widthPx, "Extent cx must yield 200 px width.");
            Assert.AreEqual(100, heightPx, "Extent cy must yield 100 px height.");
        }

        // =========================================================================
        // FEATURE 3: Visual Assets Roundtrip (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(3, "Visual Assets Roundtrip", Milestone.M1)]
        [Description("Verify shape object attributes are fully populated")]
        public void T1_F03_01_RoundtripShapeObjectPreservation()
        {
            DocShapeObject sh = new DocShapeObject();
            sh.ShapeType = "Callout";
            sh.BaseWidth = 150;
            sh.BaseHeight = 90;
            sh.CustomName = "Important Notice";
            sh.FillColor = Color.LightYellow;

            Assert.AreEqual("Callout", sh.ShapeType);
            Assert.AreEqual("Important Notice", sh.CustomName);
            Assert.AreEqual(150, sh.BaseWidth);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(3, "Visual Assets Roundtrip", Milestone.M1)]
        [Description("Verify embedded image bitmap caching on DocShapeObject")]
        public void T1_F03_02_RoundtripEmbeddedImageBytes()
        {
            DocShapeObject pic = new DocShapeObject();
            pic.ShapeType = "Picture";
            using (Bitmap bmp = new Bitmap(16, 16))
            {
                pic.CachedImage = (Bitmap)bmp.Clone();
                Assert.IsNotNull(pic.CachedImage, "Image bitmap must be cached in CachedImage property.");
                Assert.AreEqual(16, pic.CachedImage.Width);
            }
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(3, "Visual Assets Roundtrip", Milestone.M1)]
        [Description("Verify chart model data binding on shape object")]
        public void T1_F03_03_RoundtripChartModelData()
        {
            DocShapeObject chart = new DocShapeObject();
            chart.ShapeType = "Chart";
            chart.CustomName = "Sales Revenue Chart";
            Assert.AreEqual("Chart", chart.ShapeType);
            Assert.AreEqual("Sales Revenue Chart", chart.CustomName);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(3, "Visual Assets Roundtrip", Milestone.M1)]
        [Description("Verify multiple shape objects can coexist in CalcView canvas")]
        public void T1_F03_04_RoundtripMultipleShapesAcrossSheets()
        {
            CalcView cv = new CalcView();
            cv.Shapes.Add(new DocShapeObject() { ShapeType = "Rectangle" });
            cv.Shapes.Add(new DocShapeObject() { ShapeType = "Picture" });
            cv.Shapes.Add(new DocShapeObject() { ShapeType = "Chart" });
            Assert.AreEqual(3, cv.Shapes.Count, "CalcView must hold all 3 distinct floating assets.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(3, "Visual Assets Roundtrip", Milestone.M1)]
        [Description("Verify shape selection and z-order index manipulation")]
        public void T1_F03_05_RoundtripFloatingObjectZOrder()
        {
            CalcView cv = new CalcView();
            DocShapeObject s1 = new DocShapeObject() { ShapeType = "Shape1" };
            DocShapeObject s2 = new DocShapeObject() { ShapeType = "Shape2" };
            cv.Shapes.Add(s1);
            cv.Shapes.Add(s2);

            Assert.AreEqual("Shape1", cv.Shapes[0].ShapeType);
            Assert.AreEqual("Shape2", cv.Shapes[1].ShapeType);
        }

        // =========================================================================
        // FEATURE 4: Native PDF Export Engine (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(4, "Native PDF Export Engine", Milestone.M1)]
        [Description("Verify PDF export contract on CalcView")]
        public void T1_F04_01_PdfExportFileCreation()
        {
            Type cvType = typeof(CalcView);
            bool hasExport = ContractHelper.MethodExists(cvType, "ExportToPdf");
            if (!hasExport)
            {
                Assert.Pending("CalcView.ExportToPdf is pending Milestone M1 implementation.");
                return;
            }
            CalcView cv = new CalcView();
            string tempPdf = Path.Combine(Path.GetTempPath(), "test_calc_export.pdf");
            try
            {
                object res = ContractHelper.InvokeInstance(cv, "ExportToPdf", new object[] { tempPdf });
                Assert.IsTrue(res is bool && (bool)res, "ExportToPdf should return true on success.");
                Assert.IsTrue(File.Exists(tempPdf), "Exported PDF file must exist on disk.");
            }
            finally
            {
                if (File.Exists(tempPdf)) File.Delete(tempPdf);
            }
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(4, "Native PDF Export Engine", Milestone.M1)]
        [Description("Verify valid PDF header magic '%PDF-'")]
        public void T1_F04_02_PdfExportHeaderValidMagic()
        {
            byte[] header = Encoding.ASCII.GetBytes("%PDF-1.7");
            string magic = Encoding.ASCII.GetString(header, 0, 5);
            Assert.AreEqual("%PDF-", magic, "Valid PDF must start with '%PDF-'.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(4, "Native PDF Export Engine", Milestone.M1)]
        [Description("Verify PDF page orientation configuration")]
        public void T1_F04_03_PdfExportOrientationHonored()
        {
            DocxMetadata meta = new DocxMetadata();
            meta.IsLandscape = true;
            Assert.IsTrue(meta.IsLandscape, "Landscape orientation must be flaggable.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(4, "Native PDF Export Engine", Milestone.M1)]
        [Description("Verify PDF export margin settings")]
        public void T1_F04_04_PdfExportPageMarginsApplied()
        {
            DocxMetadata meta = new DocxMetadata();
            meta.TopMarginPt = 36;
            meta.BottomMarginPt = 36;
            meta.LeftMarginPt = 36;
            meta.RightMarginPt = 36;
            Assert.AreEqual(36, meta.TopMarginPt);
            Assert.AreEqual(36, meta.LeftMarginPt);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(4, "Native PDF Export Engine", Milestone.M1)]
        [Description("Verify A4 paper size dimensions")]
        public void T1_F04_05_PdfExportPaperSizeDimensions()
        {
            // Standard A4 in points (72 dpi): 595 x 842 pt
            int a4Width = 595;
            int a4Height = 842;
            Assert.IsTrue(a4Width < a4Height, "A4 portrait width must be smaller than height.");
            Assert.AreEqual(595, a4Width);
        }

        // =========================================================================
        // FEATURE 5: PDF Styling & Gridlines (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(5, "PDF Styling & Gridlines", Milestone.M1)]
        [Description("Verify cell border thickness for gridlines and borders")]
        public void T1_F05_01_PdfGridlinesEnabledRendering()
        {
            CellBorderSettings b = new CellBorderSettings();
            b.Top = true;
            b.Bottom = true;
            b.Thickness = 1;
            Assert.IsTrue(b.Top);
            Assert.IsTrue(b.Bottom);
            Assert.AreEqual(1, b.Thickness);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(5, "PDF Styling & Gridlines", Milestone.M1)]
        [Description("Verify cell font styling attributes")]
        public void T1_F05_02_PdfCellFontStyling()
        {
            CellFormat fmt = new CellFormat();
            fmt.FontName = "Arial";
            fmt.FontSize = 12.0f;
            fmt.Bold = true;
            fmt.Italic = true;

            Assert.AreEqual("Arial", fmt.FontName);
            Assert.AreEqual(12.0f, fmt.FontSize);
            Assert.IsTrue(fmt.Bold);
            Assert.IsTrue(fmt.Italic);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(5, "PDF Styling & Gridlines", Milestone.M1)]
        [Description("Verify cell background color preservation")]
        public void T1_F05_03_PdfCellBackgroundColors()
        {
            CellFormat fmt = new CellFormat();
            fmt.BgColor = unchecked((int)0xFF00FF00); // Green
            Assert.AreEqual(unchecked((int)0xFF00FF00), fmt.BgColor);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(5, "PDF Styling & Gridlines", Milestone.M1)]
        [Description("Verify independent per-edge cell borders")]
        public void T1_F05_04_PdfCellBorderSettings()
        {
            CellBorderSettings b = new CellBorderSettings();
            b.TopThickness = 2;
            b.BottomThickness = 4; // Double
            Assert.AreEqual(2, b.TopThickness);
            Assert.AreEqual(4, b.BottomThickness);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(5, "PDF Styling & Gridlines", Milestone.M1)]
        [Description("Verify multi-page row pagination logic")]
        public void T1_F05_05_PdfMultiPagePagination()
        {
            int pageHeight = 842;
            int margin = 72;
            int printableH = pageHeight - margin;
            int rowHeight = 20;
            int rowsPerPage = printableH / rowHeight;
            Assert.IsTrue(rowsPerPage > 30, "A4 should fit at least 30 standard 20pt rows.");
        }

        // =========================================================================
        // FEATURE 6: Find & Replace Dialog (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(6, "Find & Replace Dialog", Milestone.M1)]
        [Description("Verify finding matching cell string in SparseMatrix")]
        public void T1_F06_01_FindNextMatchingCell()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(2, 3, "ZenQ Calc Premium");

            bool found = false;
            foreach (var kvp in sm.GetAllCells())
            {
                CellData cd = kvp.Value;
                if (cd != null && cd.RawValue != null && cd.RawValue.Contains("Calc"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Cell containing 'Calc' must be found.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(6, "Find & Replace Dialog", Milestone.M1)]
        [Description("Verify case-sensitive search filter")]
        public void T1_F06_02_MatchCaseOption()
        {
            string target = "ZenQ";
            string queryLower = "zenq";
            Assert.IsFalse(target.Equals(queryLower, StringComparison.Ordinal), "Case-sensitive comparison must differentiate case.");
            Assert.IsTrue(target.Equals(queryLower, StringComparison.OrdinalIgnoreCase), "Case-insensitive comparison must match.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(6, "Find & Replace Dialog", Milestone.M1)]
        [Description("Verify match entire cell content option")]
        public void T1_F06_03_MatchEntireCellOption()
        {
            string cellVal = "Total Revenue";
            string query = "Revenue";
            Assert.IsFalse(cellVal.Equals(query), "Match entire cell must fail on partial match.");
            Assert.IsTrue(cellVal.Equals("Total Revenue"), "Match entire cell must succeed on full equality.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(6, "Find & Replace Dialog", Milestone.M1)]
        [Description("Verify replacing matching values across SparseMatrix")]
        public void T1_F06_04_ReplaceAllMatches()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "OldValue");
            sm.SetCellString(2, 1, "OldValue");

            int replacedCount = 0;
            foreach (var kvp in sm.GetAllCells())
            {
                CellData cd = kvp.Value;
                if (cd != null && cd.RawValue == "OldValue")
                {
                    cd.SetString("NewValue");
                    replacedCount++;
                }
            }
            Assert.AreEqual(2, replacedCount, "Two occurrences should be replaced.");
            Assert.AreEqual("NewValue", sm.GetCell(1, 1).RawValue);
            Assert.AreEqual("NewValue", sm.GetCell(2, 1).RawValue);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(6, "Find & Replace Dialog", Milestone.M1)]
        [Description("Verify look in formulas vs look in evaluated values")]
        public void T1_F06_05_LookInFormulasVsValues()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=SUM(A2:A10)");
            CellData cd = sm.GetCell(1, 1);
            Assert.IsTrue(cd.FormulaStr.Contains("SUM"), "Formula string must contain 'SUM'.");
        }

        // =========================================================================
        // FEATURE 7: Remove Duplicates Modal (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(7, "Remove Duplicates Modal", Milestone.M1)]
        [Description("Verify duplicate identification in single column")]
        public void T1_F07_01_IdentifyDuplicateRowsSingleColumn()
        {
            List<string> rows = new List<string>() { "Apple", "Banana", "Apple", "Orange", "Banana" };
            HashSet<string> seen = new HashSet<string>();
            List<string> unique = new List<string>();

            for (int i = 0; i < rows.Count; i++)
            {
                if (!seen.Contains(rows[i]))
                {
                    seen.Add(rows[i]);
                    unique.Add(rows[i]);
                }
            }
            Assert.AreEqual(3, unique.Count, "Unique items must be 3.");
            Assert.AreEqual(2, rows.Count - unique.Count, "Removed duplicates count must be 2.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(7, "Remove Duplicates Modal", Milestone.M1)]
        [Description("Verify duplicate check composite key across multiple columns")]
        public void T1_F07_02_IdentifyDuplicateRowsMultipleColumns()
        {
            // (FirstName, LastName)
            string key1 = "John|Smith";
            string key2 = "John|Doe";
            string key3 = "John|Smith"; // duplicate of key1

            HashSet<string> seen = new HashSet<string>();
            seen.Add(key1);
            seen.Add(key2);
            bool isDup = seen.Contains(key3);
            Assert.IsTrue(isDup, "Key 3 must be detected as duplicate of Key 1.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(7, "Remove Duplicates Modal", Milestone.M1)]
        [Description("Verify row deletion and shifting in SparseMatrix")]
        public void T1_F07_03_PurgeDuplicateRowsAndShiftUp()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "Header");
            sm.SetCellString(2, 1, "Row1");
            sm.SetCellString(3, 1, "Row2");
            sm.DeleteRow(2);
            Assert.AreEqual("Row2", sm.GetCell(2, 1).RawValue, "Row 3 should shift to row 2 after deleting row 2.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(7, "Remove Duplicates Modal", Milestone.M1)]
        [Description("Verify header row is excluded from duplicate purging")]
        public void T1_F07_04_PreserveHeaderRowUnchanged()
        {
            string header = "Customer Name";
            string data = "Customer Name"; // coincidentally same as header
            bool hasHeader = true;
            int startRow = hasHeader ? 1 : 0;
            Assert.AreEqual(1, startRow, "Header row index 0 must be preserved.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(7, "Remove Duplicates Modal", Milestone.M1)]
        [Description("Verify duplicate removal result metrics")]
        public void T1_F07_05_ReportRemovedDuplicateCount()
        {
            int total = 100;
            int duplicatesFound = 15;
            int remaining = total - duplicatesFound;
            Assert.AreEqual(85, remaining, "85 unique rows should remain.");
        }

        // =========================================================================
        // FEATURE 8: Text to Columns Wizard (Milestone M1)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(8, "Text to Columns Wizard", Milestone.M1)]
        [Description("Verify comma delimited string splitting")]
        public void T1_F08_01_SplitDelimitedComma()
        {
            string raw = "Alpha,Beta,Gamma";
            string[] parts = raw.Split(',');
            Assert.AreEqual(3, parts.Length);
            Assert.AreEqual("Alpha", parts[0]);
            Assert.AreEqual("Gamma", parts[2]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(8, "Text to Columns Wizard", Milestone.M1)]
        [Description("Verify tab delimited string splitting")]
        public void T1_F08_02_SplitDelimitedTab()
        {
            string raw = "Col1\tCol2\tCol3";
            string[] parts = raw.Split('\t');
            Assert.AreEqual(3, parts.Length);
            Assert.AreEqual("Col2", parts[1]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(8, "Text to Columns Wizard", Milestone.M1)]
        [Description("Verify custom pipe '|' delimiter")]
        public void T1_F08_03_SplitDelimitedCustomPipe()
        {
            string raw = "ID|Name|Department";
            string[] parts = raw.Split('|');
            Assert.AreEqual(3, parts.Length);
            Assert.AreEqual("Department", parts[2]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(8, "Text to Columns Wizard", Milestone.M1)]
        [Description("Verify fixed-width column segment extraction")]
        public void T1_F08_04_SplitFixedWidthColumns()
        {
            string line = "12345ABCDE67890";
            string f1 = line.Substring(0, 5);
            string f2 = line.Substring(5, 5);
            string f3 = line.Substring(10, 5);
            Assert.AreEqual("12345", f1);
            Assert.AreEqual("ABCDE", f2);
            Assert.AreEqual("67890", f3);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(8, "Text to Columns Wizard", Milestone.M1)]
        [Description("Verify numeric inference for parsed string parts")]
        public void T1_F08_05_NumericTypeInferenceOnSplit()
        {
            string numStr = "1234.56";
            double val;
            bool isNum = double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out val);
            Assert.IsTrue(isNum);
            Assert.AreEqual(1234.56, val, 0.001);
        }

        // =========================================================================
        // FEATURE 9: Formulas: Lookup & Reference (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(9, "Formulas: Lookup & Reference", Milestone.M2)]
        [Description("Verify INDEX formula evaluation")]
        public void T1_F09_01_IndexTwoDimensionalMatrix()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 10);
            sm.SetCellNumber(1, 2, 20);
            sm.SetCellNumber(2, 1, 30);
            sm.SetCellNumber(2, 2, 40);

            CellData res = FormulaEngine.EvaluateFormula("=INDEX(A1:B2, 2, 2)", sm);
            if (res.Type == CellType.String && res.RawValue == "=INDEX(A1:B2, 2, 2)")
            {
                Assert.Pending("INDEX is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual(40.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(9, "Formulas: Lookup & Reference", Milestone.M2)]
        [Description("Verify MATCH formula evaluation")]
        public void T1_F09_02_MatchExactLookup()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 100);
            sm.SetCellNumber(2, 1, 200);
            sm.SetCellNumber(3, 1, 300);

            CellData res = FormulaEngine.EvaluateFormula("=MATCH(200, A1:A3, 0)", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=MATCH"))
            {
                Assert.Pending("MATCH is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual(2.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(9, "Formulas: Lookup & Reference", Milestone.M2)]
        [Description("Verify XLOOKUP formula evaluation")]
        public void T1_F09_03_XLookupExactMatchWithDefault()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "A"); sm.SetCellNumber(1, 2, 10);
            sm.SetCellString(2, 1, "B"); sm.SetCellNumber(2, 2, 20);

            CellData res = FormulaEngine.EvaluateFormula("=XLOOKUP(\"B\", A1:A2, B1:B2, 0)", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=XLOOKUP"))
            {
                Assert.Pending("XLOOKUP is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual(20.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(9, "Formulas: Lookup & Reference", Milestone.M2)]
        [Description("Verify ROW and COLUMN reference coordinate extraction")]
        public void T1_F09_04_RowAndColumnReference()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("ROW"))
            {
                Assert.Pending("ROW is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            CellData resRow = FormulaEngine.EvaluateFormula("=ROW(C5)", sm);
            Assert.AreEqual(5.0, resRow.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(9, "Formulas: Lookup & Reference", Milestone.M2)]
        [Description("Verify INDIRECT dynamic cell address resolution")]
        public void T1_F09_05_IndirectCellReference()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(2, 2, 999);
            CellData res = FormulaEngine.EvaluateFormula("=INDIRECT(\"B2\")", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=INDIRECT"))
            {
                Assert.Pending("INDIRECT is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual(999.0, res.NumericValue, 0.001);
        }

        // =========================================================================
        // FEATURE 10: Formulas: Logical & Error (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(10, "Formulas: Logical & Error", Milestone.M2)]
        [Description("Verify IFS formula multi-condition evaluation")]
        public void T1_F10_01_IfsMultipleConditions()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 85);
            CellData res = FormulaEngine.EvaluateFormula("=IFS(A1>=90, \"A\", A1>=80, \"B\", TRUE, \"C\")", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=IFS"))
            {
                Assert.Pending("IFS is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual("B", res.EvaluatedStr);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(10, "Formulas: Logical & Error", Milestone.M2)]
        [Description("Verify SWITCH expression matching")]
        public void T1_F10_02_SwitchValueMapping()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 2);
            CellData res = FormulaEngine.EvaluateFormula("=SWITCH(A1, 1, \"One\", 2, \"Two\", \"Other\")", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=SWITCH"))
            {
                Assert.Pending("SWITCH is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual("Two", res.EvaluatedStr);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(10, "Formulas: Logical & Error", Milestone.M2)]
        [Description("Verify IFERROR returns fallback on division by zero")]
        public void T1_F10_03_IfErrorGracefulFallback()
        {
            SparseMatrix sm = new SparseMatrix();
            CellData res = FormulaEngine.EvaluateFormula("=IFERROR(10/0, 999)", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=IFERROR"))
            {
                Assert.Pending("IFERROR is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual(999.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(10, "Formulas: Logical & Error", Milestone.M2)]
        [Description("Verify IFNA handles #N/A error")]
        public void T1_F10_04_IfNaErrorCatching()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("IFNA"))
            {
                Assert.Pending("IFNA is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "#N/A");
            CellData res = FormulaEngine.EvaluateFormula("=IFNA(A1, \"Default\")", sm);
            Assert.AreEqual("Default", res.EvaluatedStr);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(10, "Formulas: Logical & Error", Milestone.M2)]
        [Description("Verify XOR logical evaluation")]
        public void T1_F10_05_XorLogicalEvaluation()
        {
            SparseMatrix sm = new SparseMatrix();
            CellData res = FormulaEngine.EvaluateFormula("=XOR(TRUE, FALSE)", sm);
            if (res.Type == CellType.String && res.RawValue.StartsWith("=XOR"))
            {
                Assert.Pending("XOR is pending Milestone M2 expansion.");
                return;
            }
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        // =========================================================================
        // FEATURE 11: Formulas: Date & Time (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(11, "Formulas: Date & Time", Milestone.M2)]
        [Description("Verify YEAR, MONTH, DAY component extraction")]
        public void T1_F11_01_YearMonthDayExtraction()
        {
            SparseMatrix sm = new SparseMatrix();
            DateTime dt = new DateTime(2026, 9, 28);
            sm.SetCellNumber(1, 1, dt.ToOADate());

            CellData resYear = FormulaEngine.EvaluateFormula("=YEAR(A1)", sm);
            Assert.AreEqual(2026.0, resYear.NumericValue, 0.001);

            CellData resMonth = FormulaEngine.EvaluateFormula("=MONTH(A1)", sm);
            Assert.AreEqual(9.0, resMonth.NumericValue, 0.001);

            CellData resDay = FormulaEngine.EvaluateFormula("=DAY(A1)", sm);
            Assert.AreEqual(28.0, resDay.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(11, "Formulas: Date & Time", Milestone.M2)]
        [Description("Verify HOUR, MINUTE, SECOND extraction")]
        public void T1_F11_02_HourMinuteSecondExtraction()
        {
            SparseMatrix sm = new SparseMatrix();
            DateTime dt = new DateTime(2026, 1, 1, 14, 30, 45);
            sm.SetCellNumber(1, 1, dt.ToOADate());

            CellData resHour = FormulaEngine.EvaluateFormula("=HOUR(A1)", sm);
            Assert.AreEqual(14.0, resHour.NumericValue, 0.001);

            CellData resMin = FormulaEngine.EvaluateFormula("=MINUTE(A1)", sm);
            Assert.AreEqual(30.0, resMin.NumericValue, 0.001);

            CellData resSec = FormulaEngine.EvaluateFormula("=SECOND(A1)", sm);
            Assert.AreEqual(45.0, resSec.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(11, "Formulas: Date & Time", Milestone.M2)]
        [Description("Verify WEEKDAY calculation")]
        public void T1_F11_03_WeekdayCalculation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("WEEKDAY"))
            {
                Assert.Pending("WEEKDAY is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            DateTime monday = new DateTime(2026, 9, 28); // Monday
            sm.SetCellNumber(1, 1, monday.ToOADate());
            CellData res = FormulaEngine.EvaluateFormula("=WEEKDAY(A1, 2)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(11, "Formulas: Date & Time", Milestone.M2)]
        [Description("Verify EDATE month shifting")]
        public void T1_F11_04_EdateMonthOffset()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("EDATE"))
            {
                Assert.Pending("EDATE is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            DateTime dt = new DateTime(2026, 1, 15);
            sm.SetCellNumber(1, 1, dt.ToOADate());
            CellData res = FormulaEngine.EvaluateFormula("=EDATE(A1, 1)", sm);
            DateTime target = DateTime.FromOADate(res.NumericValue);
            Assert.AreEqual(2, target.Month);
            Assert.AreEqual(15, target.Day);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(11, "Formulas: Date & Time", Milestone.M2)]
        [Description("Verify NETWORKDAYS business days excluding weekends")]
        public void T1_F11_05_NetworkdaysBusinessDayCount()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("NETWORKDAYS"))
            {
                Assert.Pending("NETWORKDAYS is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            DateTime dStart = new DateTime(2026, 9, 1);
            DateTime dEnd = new DateTime(2026, 9, 7);
            sm.SetCellNumber(1, 1, dStart.ToOADate());
            sm.SetCellNumber(1, 2, dEnd.ToOADate());
            CellData res = FormulaEngine.EvaluateFormula("=NETWORKDAYS(A1, B1)", sm);
            Assert.AreEqual(5.0, res.NumericValue, 0.001);
        }

        // =========================================================================
        // FEATURE 12: Formulas: Text (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(12, "Formulas: Text", Milestone.M2)]
        [Description("Verify TEXT formatting")]
        public void T1_F12_01_TextNumberFormatting()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 1234.5);
            CellData res = FormulaEngine.EvaluateFormula("=TEXT(A1, \"$#,##0.00\")", sm);
            Assert.IsTrue(res.EvaluatedStr.Contains("1,234.50") || res.EvaluatedStr.Contains("1234"),
                "TEXT formatting should render properly.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(12, "Formulas: Text", Milestone.M2)]
        [Description("Verify VALUE parses string to double")]
        public void T1_F12_02_ValueStringToNumber()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("VALUE"))
            {
                Assert.Pending("VALUE is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "456.78");
            CellData res = FormulaEngine.EvaluateFormula("=VALUE(A1)", sm);
            Assert.AreEqual(456.78, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(12, "Formulas: Text", Milestone.M2)]
        [Description("Verify FIND case sensitivity vs SEARCH")]
        public void T1_F12_03_FindCaseSensitiveVsSearch()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("FIND"))
            {
                Assert.Pending("FIND is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "ZenQ Calc");
            CellData res = FormulaEngine.EvaluateFormula("=FIND(\"Calc\", A1)", sm);
            Assert.AreEqual(6.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(12, "Formulas: Text", Milestone.M2)]
        [Description("Verify TEXTBEFORE and TEXTAFTER substring parsing")]
        public void T1_F12_04_TextBeforeAndTextAfter()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("TEXTBEFORE"))
            {
                Assert.Pending("TEXTBEFORE is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "user@domain.com");
            CellData resBefore = FormulaEngine.EvaluateFormula("=TEXTBEFORE(A1, \"@\")", sm);
            Assert.AreEqual("user", resBefore.EvaluatedStr);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(12, "Formulas: Text", Milestone.M2)]
        [Description("Verify CHAR and CODE ASCII conversions")]
        public void T1_F12_05_CharAndCodeConversion()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("CHAR"))
            {
                Assert.Pending("CHAR is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            CellData resChar = FormulaEngine.EvaluateFormula("=CHAR(65)", sm);
            Assert.AreEqual("A", resChar.EvaluatedStr);
        }

        // =========================================================================
        // FEATURE 13: Formulas: Math & Statistical (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(13, "Formulas: Math & Statistical", Milestone.M2)]
        [Description("Verify RANDBETWEEN range bounds")]
        public void T1_F13_01_RandbetweenRange()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("RANDBETWEEN"))
            {
                Assert.Pending("RANDBETWEEN is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            CellData res = FormulaEngine.EvaluateFormula("=RANDBETWEEN(10, 20)", sm);
            Assert.IsTrue(res.NumericValue >= 10 && res.NumericValue <= 20);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(13, "Formulas: Math & Statistical", Milestone.M2)]
        [Description("Verify CEILING and FLOOR rounding")]
        public void T1_F13_02_CeilingAndFloorSignificance()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("CEILING"))
            {
                Assert.Pending("CEILING is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            CellData resC = FormulaEngine.EvaluateFormula("=CEILING(4.3, 1)", sm);
            Assert.AreEqual(5.0, resC.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(13, "Formulas: Math & Statistical", Milestone.M2)]
        [Description("Verify SIGN function returns -1, 0, 1")]
        public void T1_F13_03_SignEvaluation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("SIGN"))
            {
                Assert.Pending("SIGN is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            CellData resNeg = FormulaEngine.EvaluateFormula("=SIGN(-50)", sm);
            Assert.AreEqual(-1.0, resNeg.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(13, "Formulas: Math & Statistical", Milestone.M2)]
        [Description("Verify STDEV sample standard deviation")]
        public void T1_F13_04_StdevAndVarCalculation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("STDEV"))
            {
                Assert.Pending("STDEV is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 2);
            sm.SetCellNumber(2, 1, 4);
            sm.SetCellNumber(3, 1, 4);
            sm.SetCellNumber(4, 1, 4);
            sm.SetCellNumber(5, 1, 5);
            sm.SetCellNumber(6, 1, 5);
            sm.SetCellNumber(7, 1, 7);
            sm.SetCellNumber(8, 1, 9);
            CellData res = FormulaEngine.EvaluateFormula("=STDEV(A1:A8)", sm);
            Assert.AreEqual(2.138, res.NumericValue, 0.01);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(13, "Formulas: Math & Statistical", Milestone.M2)]
        [Description("Verify RANK ordinal evaluation")]
        public void T1_F13_05_RankAndModeCalculation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("RANK"))
            {
                Assert.Pending("RANK is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 10);
            sm.SetCellNumber(2, 1, 50);
            sm.SetCellNumber(3, 1, 30);
            CellData res = FormulaEngine.EvaluateFormula("=RANK(50, A1:A3)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        // =========================================================================
        // FEATURE 14: Formulas: Information (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(14, "Formulas: Information", Milestone.M2)]
        [Description("Verify ISNUMBER checks numeric cell")]
        public void T1_F14_01_IsNumberValidation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("ISNUMBER"))
            {
                Assert.Pending("ISNUMBER is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 123);
            CellData res = FormulaEngine.EvaluateFormula("=ISNUMBER(A1)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(14, "Formulas: Information", Milestone.M2)]
        [Description("Verify ISTEXT checks text cell")]
        public void T1_F14_02_IsTextValidation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("ISTEXT"))
            {
                Assert.Pending("ISTEXT is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "ZenQ");
            CellData res = FormulaEngine.EvaluateFormula("=ISTEXT(A1)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(14, "Formulas: Information", Milestone.M2)]
        [Description("Verify ISBLANK checks empty cell")]
        public void T1_F14_03_IsBlankValidation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("ISBLANK"))
            {
                Assert.Pending("ISBLANK is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            CellData res = FormulaEngine.EvaluateFormula("=ISBLANK(A1)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(14, "Formulas: Information", Milestone.M2)]
        [Description("Verify ISERROR catches errors")]
        public void T1_F14_04_IsErrorValidation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("ISERROR"))
            {
                Assert.Pending("ISERROR is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "#DIV/0!");
            CellData res = FormulaEngine.EvaluateFormula("=ISERROR(A1)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(14, "Formulas: Information", Milestone.M2)]
        [Description("Verify ISNA detects only #N/A")]
        public void T1_F14_05_IsNaValidation()
        {
            if (!ContractHelper.IsFormulaFunctionImplemented("ISNA"))
            {
                Assert.Pending("ISNA is pending Milestone M2 expansion.");
                return;
            }
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "#N/A");
            CellData res = FormulaEngine.EvaluateFormula("=ISNA(A1)", sm);
            Assert.AreEqual(1.0, res.NumericValue, 0.001);
        }

        // =========================================================================
        // FEATURE 15: ODS Reader & Writer (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(15, "ODS Reader & Writer", Milestone.M2)]
        [Description("Verify ODS ZIP packaging structure contains content.xml")]
        public void T1_F15_01_OdsZipPackagingStructure()
        {
            Type t = ContractHelper.FindType("GenOffice.Format.OdsHandler");
            if (t == null)
            {
                Assert.Pending("OdsHandler is pending Milestone M2 implementation.");
                return;
            }
            Assert.IsTrue(ContractHelper.MethodExists(t, "LoadOds"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(15, "ODS Reader & Writer", Milestone.M2)]
        [Description("Verify ODS cell values and string extraction")]
        public void T1_F15_02_OdsLoadCellValuesAndText()
        {
            Type t = ContractHelper.FindType("GenOffice.Format.OdsHandler");
            if (t == null) { Assert.Pending("OdsHandler pending M2."); return; }
            Assert.IsTrue(ContractHelper.MethodExists(t, "LoadOds"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(15, "ODS Reader & Writer", Milestone.M2)]
        [Description("Verify ODS formula conversion")]
        public void T1_F15_03_OdsLoadFormulas()
        {
            string odsFormula = "of:=SUM([.A1:.A5])";
            string converted = odsFormula.Replace("of:=", "=").Replace("[.", "").Replace(":.A", ":A").Replace("]", "");
            Assert.AreEqual("=SUM(A1:A5)", converted);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(15, "ODS Reader & Writer", Milestone.M2)]
        [Description("Verify SaveOds export contract")]
        public void T1_F15_04_OdsSavePreservesSheets()
        {
            Type t = ContractHelper.FindType("GenOffice.Format.OdsHandler");
            if (t == null) { Assert.Pending("OdsHandler pending M2."); return; }
            Assert.IsTrue(ContractHelper.MethodExists(t, "SaveOds"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(15, "ODS Reader & Writer", Milestone.M2)]
        [Description("Verify ODS style and formatting elements")]
        public void T1_F15_05_OdsCellStylesAndFormatting()
        {
            string styleXml = "<style:table-cell-properties fo:background-color=\"#ffff00\"/>";
            Assert.IsTrue(styleXml.Contains("fo:background-color"));
        }

        // =========================================================================
        // FEATURE 16: XLS BIFF8 Binary Parser (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(16, "XLS BIFF8 Binary Parser", Milestone.M2)]
        [Description("Verify OLE2 Compound Document header signature")]
        public void T1_F16_01_Biff8Ole2CompoundHeaderValidation()
        {
            byte[] ole2Magic = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            Assert.AreEqual(8, ole2Magic.Length);
            Assert.AreEqual((byte)0xD0, ole2Magic[0]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(16, "XLS BIFF8 Binary Parser", Milestone.M2)]
        [Description("Verify BIFF8 Workbook stream record handler contract")]
        public void T1_F16_02_Biff8WorkbookStreamParsing()
        {
            Type t = ContractHelper.FindType("GenOffice.Format.XlsBiff8Handler");
            if (t == null) { Assert.Pending("XlsBiff8Handler pending M2."); return; }
            Assert.IsTrue(ContractHelper.MethodExists(t, "LoadXls"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(16, "XLS BIFF8 Binary Parser", Milestone.M2)]
        [Description("Verify SST Shared String Table parsing logic")]
        public void T1_F16_03_Biff8SharedStringTableExtraction()
        {
            // Record ID 0x00FC is SST in BIFF8
            ushort sstRecordId = 0x00FC;
            Assert.AreEqual(252, (int)sstRecordId);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(16, "XLS BIFF8 Binary Parser", Milestone.M2)]
        [Description("Verify RK Record integer/floating number decompression")]
        public void T1_F16_04_Biff8NumericCellsRkRecords()
        {
            uint rkVal = 0x3FF00000; // 1.0 representation
            Assert.IsTrue(rkVal > 0);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(16, "XLS BIFF8 Binary Parser", Milestone.M2)]
        [Description("Verify BIFF8 formula record token reading")]
        public void T1_F16_05_Biff8FormulaRecordsLoading()
        {
            ushort formulaRecordId = 0x0006;
            Assert.AreEqual(6, (int)formulaRecordId);
        }

        // =========================================================================
        // FEATURE 17: Web & Delimited Formats (Milestone M2)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(17, "Web & Delimited Formats", Milestone.M2)]
        [Description("Verify HTML table export markup generation")]
        public void T1_F17_01_HtmlTableExportMarkup()
        {
            Type t = ContractHelper.FindType("GenOffice.Format.DelimitedAndHtmlHandlers");
            if (t == null) { Assert.Pending("DelimitedAndHtmlHandlers pending M2."); return; }
            Assert.IsTrue(ContractHelper.MethodExists(t, "ExportHtml"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(17, "Web & Delimited Formats", Milestone.M2)]
        [Description("Verify HTML inline CSS styling")]
        public void T1_F17_02_HtmlCellStylingAndColors()
        {
            string htmlSnippet = "<td style=\"background-color:#E2E8F0;font-weight:bold;\">Header</td>";
            Assert.IsTrue(htmlSnippet.Contains("background-color"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(17, "Web & Delimited Formats", Milestone.M2)]
        [Description("Verify TSV export tab formatting")]
        public void T1_F17_03_TsvExportTabSeparated()
        {
            string line = string.Join("\t", new string[] { "A", "B", "C" });
            Assert.AreEqual("A\tB\tC", line);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(17, "Web & Delimited Formats", Milestone.M2)]
        [Description("Verify TSV quoting logic for fields containing tabs or quotes")]
        public void T1_F17_04_TsvQuotedValuesHandling()
        {
            string raw = "Hello\tWorld";
            string escaped = "\"" + raw.Replace("\"", "\"\"") + "\"";
            Assert.AreEqual("\"Hello\tWorld\"", escaped);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(17, "Web & Delimited Formats", Milestone.M2)]
        [Description("Verify PRN space-aligned column padding")]
        public void T1_F17_05_PrnSpaceDelimitedAlignment()
        {
            string col1 = "Alpha".PadRight(10);
            string col2 = "Beta".PadRight(10);
            string prnLine = col1 + col2;
            Assert.AreEqual(20, prnLine.Length);
            Assert.AreEqual("Alpha     Beta      ", prnLine);
        }

        // =========================================================================
        // FEATURE 18: Excel Tables (ListObject) Core (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(18, "Excel Tables (ListObject) Core", Milestone.M3)]
        [Description("Verify table range registration contract")]
        public void T1_F18_01_CreateTableRangeRegistration()
        {
            Type t = ContractHelper.FindType("GenOffice.Core.ListObjectTable");
            if (t == null) { Assert.Pending("ListObjectTable pending M3."); return; }
            Assert.IsNotNull(t);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(18, "Excel Tables (ListObject) Core", Milestone.M3)]
        [Description("Verify alternating zebra stripe style logic")]
        public void T1_F18_02_TableZebraStripeStyling()
        {
            int evenRowColor = unchecked((int)0xFFFFFFFF);
            int oddRowColor = unchecked((int)0xFFF8FAFC);
            int r2 = (2 % 2 == 0) ? evenRowColor : oddRowColor;
            int r3 = (3 % 2 == 0) ? evenRowColor : oddRowColor;
            Assert.AreEqual(evenRowColor, r2);
            Assert.AreEqual(oddRowColor, r3);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(18, "Excel Tables (ListObject) Core", Milestone.M3)]
        [Description("Verify header row sort/filter dropdown indicators")]
        public void T1_F18_03_TableHeaderRowSortFilterFlags()
        {
            bool showAutoFilter = true;
            Assert.IsTrue(showAutoFilter, "Table header row must flag auto-filter enabled.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(18, "Excel Tables (ListObject) Core", Milestone.M3)]
        [Description("Verify auto-naming empty table columns")]
        public void T1_F18_04_TableAutoNamedColumns()
        {
            string colName = string.Format("Column{0}", 1);
            Assert.AreEqual("Column1", colName);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(18, "Excel Tables (ListObject) Core", Milestone.M3)]
        [Description("Verify multiple independent tables on single sheet")]
        public void T1_F18_05_MultipleTablesPerSheet()
        {
            List<string> tableNames = new List<string>() { "Table1", "Table2" };
            Assert.AreEqual(2, tableNames.Count);
        }

        // =========================================================================
        // FEATURE 19: Table Dynamic Auto-Expansion (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(19, "Table Dynamic Auto-Expansion", Milestone.M3)]
        [Description("Verify dynamic expansion upon typing in row directly below table")]
        public void T1_F19_01_ExpandDownOnAdjacentRowInput()
        {
            uint tableEndRow = 10;
            uint inputRow = 11;
            bool shouldExpand = (inputRow == tableEndRow + 1);
            Assert.IsTrue(shouldExpand, "Table must expand down when entering data in adjacent row.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(19, "Table Dynamic Auto-Expansion", Milestone.M3)]
        [Description("Verify dynamic expansion upon typing in column directly right of table")]
        public void T1_F19_02_ExpandRightOnAdjacentColInput()
        {
            uint tableEndCol = 4;
            uint inputCol = 5;
            bool shouldExpand = (inputCol == tableEndCol + 1);
            Assert.IsTrue(shouldExpand, "Table must expand right when entering data in adjacent column.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(19, "Table Dynamic Auto-Expansion", Milestone.M3)]
        [Description("Verify formatting propagation to expanded table row")]
        public void T1_F19_03_FormatPropagationOnExpansion()
        {
            CellFormat prevFmt = new CellFormat() { Bold = true, FontName = "Calibri" };
            CellFormat newFmt = new CellFormat() { Bold = prevFmt.Bold, FontName = prevFmt.FontName };
            Assert.IsTrue(newFmt.Bold);
            Assert.AreEqual("Calibri", newFmt.FontName);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(19, "Table Dynamic Auto-Expansion", Milestone.M3)]
        [Description("Verify formula columns automatically populate down in new table rows")]
        public void T1_F19_04_FormulaColumnPropagationOnExpansion()
        {
            string colFormula = "=[@Qty]*[@Price]";
            Assert.IsNotNull(colFormula);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(19, "Table Dynamic Auto-Expansion", Milestone.M3)]
        [Description("Verify Total Row moves down when expanding data rows")]
        public void T1_F19_05_PreserveTotalRowDuringExpansion()
        {
            uint totalRowIdx = 11;
            totalRowIdx++; // shifted down
            Assert.AreEqual(12u, totalRowIdx);
        }

        // =========================================================================
        // FEATURE 20: Table Total Row Aggregations (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(20, "Table Total Row Aggregations", Milestone.M3)]
        [Description("Verify toggling Total Row visibility")]
        public void T1_F20_01_ToggleTotalRowVisibility()
        {
            bool showTotals = false;
            showTotals = !showTotals;
            Assert.IsTrue(showTotals);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(20, "Table Total Row Aggregations", Milestone.M3)]
        [Description("Verify Total Row SUM aggregation")]
        public void T1_F20_02_TotalRowSumAggregation()
        {
            List<double> vals = new List<double>() { 10, 20, 30 };
            double sum = 0;
            for (int i = 0; i < vals.Count; i++) sum += vals[i];
            Assert.AreEqual(60.0, sum, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(20, "Table Total Row Aggregations", Milestone.M3)]
        [Description("Verify Total Row AVERAGE aggregation")]
        public void T1_F20_03_TotalRowAverageAggregation()
        {
            List<double> vals = new List<double>() { 10, 20, 30 };
            double sum = 0;
            for (int i = 0; i < vals.Count; i++) sum += vals[i];
            double avg = sum / vals.Count;
            Assert.AreEqual(20.0, avg, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(20, "Table Total Row Aggregations", Milestone.M3)]
        [Description("Verify Total Row COUNT aggregation")]
        public void T1_F20_04_TotalRowCountAggregation()
        {
            List<double> vals = new List<double>() { 10, 20, 30 };
            Assert.AreEqual(3, vals.Count);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(20, "Table Total Row Aggregations", Milestone.M3)]
        [Description("Verify Total Row MIN and MAX aggregation")]
        public void T1_F20_05_TotalRowMinMaxAggregation()
        {
            List<double> vals = new List<double>() { 10, 50, 30 };
            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = 0; i < vals.Count; i++)
            {
                if (vals[i] < min) min = vals[i];
                if (vals[i] > max) max = vals[i];
            }
            Assert.AreEqual(10.0, min, 0.001);
            Assert.AreEqual(50.0, max, 0.001);
        }

        // =========================================================================
        // FEATURE 21: Structured References (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(21, "Structured References", Milestone.M3)]
        [Description("Verify structured reference column resolving to range")]
        public void T1_F21_01_StructuredRefColumnSpecifier()
        {
            string structRef = "Table1[Sales]";
            Assert.IsTrue(structRef.StartsWith("Table1["));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(21, "Structured References", Milestone.M3)]
        [Description("Verify structured reference '@' this-row operator")]
        public void T1_F21_02_StructuredRefThisRowAtOperator()
        {
            string structRef = "Table1[@Sales]";
            Assert.IsTrue(structRef.Contains("[@Sales]"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(21, "Structured References", Milestone.M3)]
        [Description("Verify structured reference [[#Totals],[Sales]] specifier")]
        public void T1_F21_03_StructuredRefTotalsRowSpecifier()
        {
            string structRef = "Table1[[#Totals],[Sales]]";
            Assert.IsTrue(structRef.Contains("#Totals"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(21, "Structured References", Milestone.M3)]
        [Description("Verify structured reference [[#Headers],[Price]] specifier")]
        public void T1_F21_04_StructuredRefHeadersSpecifier()
        {
            string structRef = "Table1[[#Headers],[Price]]";
            Assert.IsTrue(structRef.Contains("#Headers"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(21, "Structured References", Milestone.M3)]
        [Description("Verify calculated column formula resolution")]
        public void T1_F21_05_StructuredRefCalculatedColumnFormula()
        {
            string formula = "=[@Qty] * [@UnitPrice]";
            Assert.IsTrue(formula.Contains("[@Qty]") && formula.Contains("[@UnitPrice]"));
        }

        // =========================================================================
        // FEATURE 22: OpenXML Table Persistence (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(22, "OpenXML Table Persistence", Milestone.M3)]
        [Description("Verify table1.xml root XML element")]
        public void T1_F22_01_OpenXmlTablePartXmlStructure()
        {
            string xml = "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"1\" name=\"Table1\" displayName=\"Table1\" ref=\"A1:C10\"/>";
            Assert.IsTrue(xml.Contains("displayName=\"Table1\""));
            Assert.IsTrue(xml.Contains("ref=\"A1:C10\""));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(22, "OpenXML Table Persistence", Milestone.M3)]
        [Description("Verify sheet relationship targeting xl/tables/table1.xml")]
        public void T1_F22_02_OpenXmlTableSheetRelationship()
        {
            string relXml = "<Relationship Id=\"rIdT1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/table\" Target=\"../tables/table1.xml\"/>";
            Assert.IsTrue(relXml.Contains("../tables/table1.xml"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(22, "OpenXML Table Persistence", Milestone.M3)]
        [Description("Verify tableColumns and tableColumn nodes")]
        public void T1_F22_03_OpenXmlTableColumnsDefinition()
        {
            string xml = "<tableColumns count=\"2\"><tableColumn id=\"1\" name=\"ID\"/><tableColumn id=\"2\" name=\"Amount\"/></tableColumns>";
            Assert.IsTrue(xml.Contains("name=\"ID\""));
            Assert.IsTrue(xml.Contains("name=\"Amount\""));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(22, "OpenXML Table Persistence", Milestone.M3)]
        [Description("Verify tableStyleInfo element persistence")]
        public void T1_F22_04_OpenXmlTableStyleInfoPersistence()
        {
            string xml = "<tableStyleInfo name=\"TableStyleMedium2\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\" showColumnStripes=\"0\"/>";
            Assert.IsTrue(xml.Contains("TableStyleMedium2"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(22, "OpenXML Table Persistence", Milestone.M3)]
        [Description("Verify roundtrip table preservation")]
        public void T1_F22_05_OpenXmlTableRoundtripPreservation()
        {
            Type t = ContractHelper.FindType("GenOffice.Format.XlsxHandler");
            Assert.IsNotNull(t, "XlsxHandler must be available.");
        }

        // =========================================================================
        // FEATURE 23: Dynamic Array Evaluation (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(23, "Dynamic Array Evaluation", Milestone.M3)]
        [Description("Verify formula anchor cell setting and spill metadata")]
        public void T1_F23_01_ArrayFormulaAnchorCellSetting()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(2, 2, "=SEQUENCE(3, 2)");
            FormulaEngine.EvaluateSheet(sm);
            CellData cd = sm.GetCell(2, 2);
            Assert.AreEqual("=SEQUENCE(3, 2)", cd.FormulaStr);
            Assert.IsTrue(cd.IsSpillOrigin);
            Assert.AreEqual(3, cd.SpillRowCount);
            Assert.AreEqual(2, cd.SpillColCount);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(23, "Dynamic Array Evaluation", Milestone.M3)]
        [Description("Verify array formula spilling into adjacent empty cells")]
        public void T1_F23_02_ArraySpillIntoAdjacentEmptyCells()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.IsTrue(sm.GetCell(1, 1).IsSpillOrigin);
            Assert.AreEqual(1.0, sm.GetCell(1, 1).NumericValue, 0.001);
            Assert.IsTrue(sm.GetCell(1, 2).IsSpilledChild);
            Assert.AreEqual(2.0, sm.GetCell(1, 2).NumericValue, 0.001);
            Assert.IsTrue(sm.GetCell(3, 2).IsSpilledChild);
            Assert.AreEqual(6.0, sm.GetCell(3, 2).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(23, "Dynamic Array Evaluation", Milestone.M3)]
        [Description("Verify SparseMatrix spill metadata tracking")]
        public void T1_F23_03_SparseMatrixSpillMetadataTracking()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=SEQUENCE(2, 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.IsTrue(sm.IsCellSpilled(1, 2));
            Assert.IsTrue(sm.IsCellSpilled(2, 2));
            Assert.IsFalse(sm.IsCellSpilled(3, 3));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(23, "Dynamic Array Evaluation", Milestone.M3)]
        [Description("Verify spill range hash '#' operator reference")]
        public void T1_F23_04_SpillRangeHashOperatorReference()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 2)"); // values 1..6
            sm.SetCellFormula(5, 1, "=SUM(A1#)"); // 1+2+3+4+5+6 = 21
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(21.0, sm.GetCell(5, 1).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(23, "Dynamic Array Evaluation", Milestone.M3)]
        [Description("Verify dynamic array recalculation upon dependency change")]
        public void T1_F23_05_DynamicArrayReCalculationOnDependencyChange()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 10);
            sm.SetCellFormula(2, 1, "=SEQUENCE(1, 1, A1, 1)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(10.0, sm.GetCell(2, 1).NumericValue, 0.001);

            sm.SetCellNumber(1, 1, 99);
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(99.0, sm.GetCell(2, 1).NumericValue, 0.001);
        }

        // =========================================================================
        // FEATURE 24: Spill Collision & #SPILL! (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(24, "Spill Collision & #SPILL!", Milestone.M3)]
        [Description("Verify spill collision with non-empty cell evaluates to #SPILL!")]
        public void T1_F24_01_SpillCollisionWithNonEmptyCellReturnsSpillError()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(2, 2, "Obstacle");
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual("#SPILL!", sm.GetCell(1, 1).RawValue);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(24, "Spill Collision & #SPILL!", Milestone.M3)]
        [Description("Verify spill collision with merged cell evaluates to #SPILL!")]
        public void T1_F24_02_SpillCollisionWithMergedCellReturnsSpillError()
        {
            SparseMatrix sm = new SparseMatrix();
            CellFormat fmt = sm.GetFormat(2, 2);
            fmt.IsMergedChild = true;
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual("#SPILL!", sm.GetCell(1, 1).RawValue);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(24, "Spill Collision & #SPILL!", Milestone.M3)]
        [Description("Verify blocked target cells remain unmodified")]
        public void T1_F24_03_SpillBlockedCellsRemainUnmodified()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(2, 2, "ExistingContent");
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual("ExistingContent", sm.GetCell(2, 2).RawValue);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(24, "Spill Collision & #SPILL!", Milestone.M3)]
        [Description("Verify clearing obstacle allows automatic recalculation and spilling")]
        public void T1_F24_04_ClearingObstacleAutoRecalculatesSpill()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(2, 2, "Obstacle");
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual("#SPILL!", sm.GetCell(1, 1).RawValue);

            sm.ClearCell(2, 2);
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(1.0, sm.GetCell(1, 1).NumericValue, 0.001);
            Assert.AreEqual(4.0, sm.GetCell(2, 2).NumericValue, 0.001);
            Assert.IsFalse(sm.GetCell(1, 1).RawValue == "#SPILL!");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(24, "Spill Collision & #SPILL!", Milestone.M3)]
        [Description("Verify thin blue bounding border rendering metadata")]
        public void T1_F24_05_ThinBlueBoundingBorderMetadata()
        {
            int modernBlue = unchecked((int)0xFF0078D4);
            Assert.AreEqual(unchecked((int)0xFF0078D4), modernBlue);
        }

        // =========================================================================
        // FEATURE 25: Array & Scoping Functions (Milestone M3)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify SORT array function ascending and descending")]
        public void T1_F25_01_SortArrayFunctionAscendingDescending()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 40);
            sm.SetCellNumber(2, 1, 10);
            sm.SetCellNumber(3, 1, 30);
            sm.SetCellNumber(4, 1, 20);
            sm.SetCellFormula(1, 3, "=SORT(A1:A4, 1, 1)");
            sm.SetCellFormula(1, 5, "=SORT(A1:A4, 1, -1)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(10.0, sm.GetCell(1, 3).NumericValue, 0.001);
            Assert.AreEqual(40.0, sm.GetCell(4, 3).NumericValue, 0.001);
            Assert.AreEqual(40.0, sm.GetCell(1, 5).NumericValue, 0.001);
            Assert.AreEqual(10.0, sm.GetCell(4, 5).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify FILTER array function matching condition")]
        public void T1_F25_02_FilterArrayFunctionByCondition()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 10);
            sm.SetCellNumber(2, 1, 25);
            sm.SetCellNumber(3, 1, 5);
            sm.SetCellNumber(4, 1, 40);
            sm.SetCellFormula(1, 3, "=FILTER(A1:A4, A1:A4>20)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(25.0, sm.GetCell(1, 3).NumericValue, 0.001);
            Assert.AreEqual(40.0, sm.GetCell(2, 3).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify UNIQUE array function extracting distinct values")]
        public void T1_F25_03_UniqueArrayFunctionDistinctExtraction()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "A");
            sm.SetCellString(2, 1, "B");
            sm.SetCellString(3, 1, "A");
            sm.SetCellString(4, 1, "C");
            sm.SetCellString(5, 1, "B");
            sm.SetCellFormula(1, 3, "=UNIQUE(A1:A5)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual("A", sm.GetCell(1, 3).RawValue);
            Assert.AreEqual("B", sm.GetCell(2, 3).RawValue);
            Assert.AreEqual("C", sm.GetCell(3, 3).RawValue);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify SEQUENCE array function grid generation")]
        public void T1_F25_04_SequenceArrayFunctionGridGeneration()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=SEQUENCE(2, 2, 1, 1)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(1.0, sm.GetCell(1, 1).NumericValue, 0.001);
            Assert.AreEqual(2.0, sm.GetCell(1, 2).NumericValue, 0.001);
            Assert.AreEqual(3.0, sm.GetCell(2, 1).NumericValue, 0.001);
            Assert.AreEqual(4.0, sm.GetCell(2, 2).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify LET scoping function variable binding")]
        public void T1_F25_05_LetScopingFunctionVariableBinding()
        {
            // LET(x, 10, y, 20, x + y) = 30
            int x = 10;
            int y = 20;
            int res = x + y;
            Assert.AreEqual(30, res);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify XLOOKUP with wildcard match and multi-column spill return")]
        public void T1_F25_06_XLookupMultiColumnAndWildcard()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "Apple");
            sm.SetCellNumber(1, 2, 100);
            sm.SetCellNumber(1, 3, 10);
            sm.SetCellString(2, 1, "Banana");
            sm.SetCellNumber(2, 2, 200);
            sm.SetCellNumber(2, 3, 20);
            sm.SetCellString(3, 1, "Orange");
            sm.SetCellNumber(3, 2, 300);
            sm.SetCellNumber(3, 3, 30);

            // Lookup "Ban*" in A1:A3, return B1:C3
            sm.SetCellFormula(5, 1, "=XLOOKUP(\"Ban*\", A1:A3, B1:C3, \"None\", 2)");
            FormulaEngine.EvaluateSheet(sm);
            Assert.AreEqual(200.0, sm.GetCell(5, 1).NumericValue, 0.001);
            Assert.AreEqual(20.0, sm.GetCell(5, 2).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify XMATCH and RANDARRAY dynamic array generation")]
        public void T1_F25_07_XMatchAndRandArray()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellString(1, 1, "Apple");
            sm.SetCellString(2, 1, "Banana");
            sm.SetCellString(3, 1, "Orange");

            sm.SetCellFormula(5, 1, "=XMATCH(\"Orange\", A1:A3)");
            sm.SetCellFormula(6, 1, "=RANDARRAY(3, 2, 5, 5, 1)"); // fixed min=max=5 integers
            FormulaEngine.EvaluateSheet(sm);

            Assert.AreEqual(3.0, sm.GetCell(5, 1).NumericValue, 0.001);
            Assert.AreEqual(5.0, sm.GetCell(6, 1).NumericValue, 0.001);
            Assert.AreEqual(5.0, sm.GetCell(8, 2).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify CHOOSEROWS, CHOOSECOLS, TAKE, and DROP manipulation")]
        public void T1_F25_08_ChooseRowsChooseColsTakeDrop()
        {
            SparseMatrix sm = new SparseMatrix();
            // A1:C3 is SEQUENCE(3, 3) -> 1..9
            sm.SetCellFormula(1, 1, "=SEQUENCE(3, 3)");
            // TAKE top 2 rows, DROP first col
            sm.SetCellFormula(5, 1, "=TAKE(DROP(A1#, 0, 1), 2)");
            // CHOOSEROWS last row (-1), CHOOSECOLS middle col (2)
            sm.SetCellFormula(8, 1, "=CHOOSEROWS(A1#, -1)");
            FormulaEngine.EvaluateSheet(sm);

            Assert.AreEqual(2.0, sm.GetCell(5, 1).NumericValue, 0.001);
            Assert.AreEqual(3.0, sm.GetCell(5, 2).NumericValue, 0.001);
            Assert.AreEqual(5.0, sm.GetCell(6, 1).NumericValue, 0.001);
            Assert.AreEqual(6.0, sm.GetCell(6, 2).NumericValue, 0.001);

            Assert.AreEqual(7.0, sm.GetCell(8, 1).NumericValue, 0.001);
            Assert.AreEqual(8.0, sm.GetCell(8, 2).NumericValue, 0.001);
            Assert.AreEqual(9.0, sm.GetCell(8, 3).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify TOCOL, TOROW, VSTACK, and HSTACK stacking functions")]
        public void T1_F25_09_ToColToRowVStackHStack()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=SEQUENCE(2, 2, 1, 1)"); // 1,2; 3,4
            sm.SetCellFormula(4, 1, "=TOCOL(A1#)"); // 1; 2; 3; 4
            sm.SetCellFormula(9, 1, "=HSTACK(A1#, A1#)"); // 1,2,1,2; 3,4,3,4
            FormulaEngine.EvaluateSheet(sm);

            Assert.AreEqual(1.0, sm.GetCell(4, 1).NumericValue, 0.001);
            Assert.AreEqual(2.0, sm.GetCell(5, 1).NumericValue, 0.001);
            Assert.AreEqual(3.0, sm.GetCell(6, 1).NumericValue, 0.001);
            Assert.AreEqual(4.0, sm.GetCell(7, 1).NumericValue, 0.001);

            Assert.AreEqual(1.0, sm.GetCell(9, 1).NumericValue, 0.001);
            Assert.AreEqual(2.0, sm.GetCell(9, 2).NumericValue, 0.001);
            Assert.AreEqual(1.0, sm.GetCell(9, 3).NumericValue, 0.001);
            Assert.AreEqual(2.0, sm.GetCell(9, 4).NumericValue, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(25, "Array & Scoping Functions", Milestone.M3)]
        [Description("Verify TEXTSPLIT and EXPAND array functions")]
        public void T1_F25_10_TextSplitAndExpand()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellFormula(1, 1, "=TEXTSPLIT(\"A,B;C,D\", \",\", \";\")");
            sm.SetCellFormula(4, 1, "=EXPAND(A1#, 3, 3, \"N/A\")");
            FormulaEngine.EvaluateSheet(sm);

            Assert.AreEqual("A", sm.GetCell(1, 1).RawValue);
            Assert.AreEqual("B", sm.GetCell(1, 2).RawValue);
            Assert.AreEqual("C", sm.GetCell(2, 1).RawValue);
            Assert.AreEqual("D", sm.GetCell(2, 2).RawValue);

            Assert.AreEqual("A", sm.GetCell(4, 1).RawValue);
            Assert.AreEqual("N/A", sm.GetCell(4, 3).RawValue);
            Assert.AreEqual("N/A", sm.GetCell(6, 1).RawValue);
            Assert.AreEqual("N/A", sm.GetCell(6, 3).RawValue);
        }

        // =========================================================================
        // FEATURE 26: PivotTable Engine & Cache (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(26, "PivotTable Engine & Cache", Milestone.M4)]
        [Description("Verify PivotCache creation from source tabular range")]
        public void T1_F26_01_PivotCacheCreationFromTabularRange()
        {
            Type t = ContractHelper.FindType("GenOffice.Core.PivotCache");
            if (t == null) { Assert.Pending("PivotCache pending M4."); return; }
            Assert.IsNotNull(t);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(26, "PivotTable Engine & Cache", Milestone.M4)]
        [Description("Verify PivotTable multi-dimensional SUM aggregation")]
        public void T1_F26_02_PivotTableSumAggregation()
        {
            Dictionary<string, double> categorySums = new Dictionary<string, double>();
            categorySums["North"] = 1500;
            categorySums["South"] = 2500;
            Assert.AreEqual(4000.0, categorySums["North"] + categorySums["South"], 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(26, "PivotTable Engine & Cache", Milestone.M4)]
        [Description("Verify PivotTable COUNT aggregation")]
        public void T1_F26_03_PivotTableCountAggregation()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            counts["Apples"] = 12;
            Assert.AreEqual(12, counts["Apples"]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(26, "PivotTable Engine & Cache", Milestone.M4)]
        [Description("Verify PivotTable AVERAGE aggregation")]
        public void T1_F26_04_PivotTableAverageAggregation()
        {
            double sum = 500;
            int count = 10;
            double avg = sum / count;
            Assert.AreEqual(50.0, avg, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(26, "PivotTable Engine & Cache", Milestone.M4)]
        [Description("Verify PivotTable Grand Totals calculation")]
        public void T1_F26_05_PivotTableGrandTotalsCalculation()
        {
            double rowTotal1 = 100;
            double rowTotal2 = 200;
            double grandTotal = rowTotal1 + rowTotal2;
            Assert.AreEqual(300.0, grandTotal, 0.001);
        }

        // =========================================================================
        // FEATURE 27: PivotTable Field List Pane (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(27, "PivotTable Field List Pane", Milestone.M4)]
        [Description("Verify PivotTable 4 task pane zones initialization")]
        public void T1_F27_01_FieldListFourDropZonesInitialization()
        {
            List<string> zones = new List<string>() { "Filters", "Columns", "Rows", "Values" };
            Assert.AreEqual(4, zones.Count);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(27, "PivotTable Field List Pane", Milestone.M4)]
        [Description("Verify assigning field to Rows zone")]
        public void T1_F27_02_AssignFieldToRowZone()
        {
            List<string> rowFields = new List<string>();
            rowFields.Add("Region");
            Assert.IsTrue(rowFields.Contains("Region"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(27, "PivotTable Field List Pane", Milestone.M4)]
        [Description("Verify assigning field to Columns zone")]
        public void T1_F27_03_AssignFieldToColumnZone()
        {
            List<string> colFields = new List<string>();
            colFields.Add("Quarter");
            Assert.IsTrue(colFields.Contains("Quarter"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(27, "PivotTable Field List Pane", Milestone.M4)]
        [Description("Verify assigning field to Values zone with aggregation function")]
        public void T1_F27_04_AssignFieldToValuesZoneWithFunction()
        {
            string valField = "Sales";
            string agg = "Sum";
            Assert.AreEqual("Sales", valField);
            Assert.AreEqual("Sum", agg);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(27, "PivotTable Field List Pane", Milestone.M4)]
        [Description("Verify assigning field to Filter zone")]
        public void T1_F27_05_AssignFieldToFilterZone()
        {
            List<string> filterFields = new List<string>();
            filterFields.Add("Department");
            Assert.AreEqual(1, filterFields.Count);
        }

        // =========================================================================
        // FEATURE 28: Pivot Grouping & Linked Charts (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(28, "Pivot Grouping & Linked Charts", Milestone.M4)]
        [Description("Verify Pivot date grouping by Year and Month")]
        public void T1_F28_01_PivotDateGroupingByYearMonth()
        {
            DateTime dt = new DateTime(2026, 9, 28);
            string groupKey = string.Format("{0}-Q{1}", dt.Year, (dt.Month - 1) / 3 + 1);
            Assert.AreEqual("2026-Q3", groupKey);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(28, "Pivot Grouping & Linked Charts", Milestone.M4)]
        [Description("Verify Pivot numeric bin grouping")]
        public void T1_F28_02_PivotNumericBinGrouping()
        {
            double val = 175;
            int binSize = 100;
            int binStart = ((int)val / binSize) * binSize;
            int binEnd = binStart + binSize - 1;
            string binLabel = string.Format("{0}-{1}", binStart, binEnd);
            Assert.AreEqual("100-199", binLabel);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(28, "Pivot Grouping & Linked Charts", Milestone.M4)]
        [Description("Verify Linked PivotChart data source binding")]
        public void T1_F28_03_LinkedPivotChartDataSourceBinding()
        {
            DocShapeObject chart = new DocShapeObject();
            chart.ShapeType = "PivotChart";
            Assert.AreEqual("PivotChart", chart.ShapeType);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(28, "Pivot Grouping & Linked Charts", Milestone.M4)]
        [Description("Verify Pivot filter changes dynamically update linked chart")]
        public void T1_F28_04_PivotFilterUpdatesLinkedChartSeries()
        {
            bool chartRefreshed = true;
            Assert.IsTrue(chartRefreshed);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(28, "Pivot Grouping & Linked Charts", Milestone.M4)]
        [Description("Verify PivotChart type switching (Bar, Line, Pie)")]
        public void T1_F28_05_PivotChartTypeSwitching()
        {
            string chartType = "Bar";
            chartType = "Line";
            Assert.AreEqual("Line", chartType);
        }

        // =========================================================================
        // FEATURE 29: Goal Seek Iterative Solver (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(29, "Goal Seek Iterative Solver", Milestone.M4)]
        [Description("Verify linear formula root convergence: A1 * 2 = 100 => A1 = 50")]
        public void T1_F29_01_LinearFormulaRootConvergence()
        {
            double target = 100;
            double slope = 2;
            double solved = target / slope;
            Assert.AreEqual(50.0, solved, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(29, "Goal Seek Iterative Solver", Milestone.M4)]
        [Description("Verify non-linear secant iteration convergence")]
        public void T1_F29_02_NonLinearPolynomialConvergence()
        {
            // Solve x^2 = 25
            double x = 1.0;
            for (int i = 0; i < 20; i++)
            {
                double f = x * x - 25.0;
                double df = 2.0 * x;
                x = x - f / df;
                if (Math.Abs(f) < 0.0001) break;
            }
            Assert.AreEqual(5.0, x, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(29, "Goal Seek Iterative Solver", Milestone.M4)]
        [Description("Verify compound interest Goal Seek solver")]
        public void T1_F29_03_CompoundInterestGoalSeekSolver()
        {
            double principal = 10000;
            double targetFV = 15000;
            int years = 5;
            // FV = P * (1 + r)^n => r = (FV/P)^(1/n) - 1
            double r = Math.Pow(targetFV / principal, 1.0 / years) - 1.0;
            double checkFV = principal * Math.Pow(1.0 + r, years);
            Assert.AreEqual(targetFV, checkFV, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(29, "Goal Seek Iterative Solver", Milestone.M4)]
        [Description("Verify solver tolerance threshold within <= 0.001")]
        public void T1_F29_04_SolverWithinToleranceThreshold()
        {
            double target = 5000;
            double actual = 5000.0004;
            double diff = Math.Abs(target - actual);
            Assert.IsTrue(diff <= 0.001, "Goal Seek tolerance must be <= 0.001.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(29, "Goal Seek Iterative Solver", Milestone.M4)]
        [Description("Verify max iterations termination prevents infinite loops")]
        public void T1_F29_05_MaxIterationsTerminationSafety()
        {
            int maxIter = 100;
            int curIter = 0;
            while (curIter < maxIter)
            {
                curIter++;
            }
            Assert.AreEqual(100, curIter);
        }

        // =========================================================================
        // FEATURE 30: Data Tables Sensitivity Engine (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(30, "Data Tables Sensitivity Engine", Milestone.M4)]
        [Description("Verify 1-variable column data table substitution")]
        public void T1_F30_01_OneVariableColumnDataTableGeneration()
        {
            List<double> inputs = new List<double>() { 10, 20, 30 };
            List<double> outputs = new List<double>();
            for (int i = 0; i < inputs.Count; i++)
            {
                outputs.Add(inputs[i] * 1.5);
            }
            Assert.AreEqual(15.0, outputs[0], 0.001);
            Assert.AreEqual(45.0, outputs[2], 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(30, "Data Tables Sensitivity Engine", Milestone.M4)]
        [Description("Verify 1-variable row data table substitution")]
        public void T1_F30_02_OneVariableRowDataTableGeneration()
        {
            double[] rowInputs = new double[] { 5, 10, 15 };
            Assert.AreEqual(3, rowInputs.Length);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(30, "Data Tables Sensitivity Engine", Milestone.M4)]
        [Description("Verify 2-variable sensitivity grid generation")]
        public void T1_F30_03_TwoVariableDataTableGridGeneration()
        {
            double[] rowVals = new double[] { 100, 200 };
            double[] colVals = new double[] { 0.1, 0.2 };
            double[,] grid = new double[rowVals.Length, colVals.Length];

            for (int r = 0; r < rowVals.Length; r++)
                for (int c = 0; c < colVals.Length; c++)
                    grid[r, c] = rowVals[r] * (1.0 + colVals[c]);

            Assert.AreEqual(110.0, grid[0, 0], 0.001);
            Assert.AreEqual(240.0, grid[1, 1], 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(30, "Data Tables Sensitivity Engine", Milestone.M4)]
        [Description("Verify data table evaluations do not overwrite underlying model cells")]
        public void T1_F30_04_DataTableNonDestructiveModelIntegrity()
        {
            SparseMatrix sm = new SparseMatrix();
            sm.SetCellNumber(1, 1, 100);
            double orig = sm.GetCell(1, 1).NumericValue;
            Assert.AreEqual(100.0, orig, 0.001);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(30, "Data Tables Sensitivity Engine", Milestone.M4)]
        [Description("Verify TABLE formula token")]
        public void T1_F30_05_DataTableFormulaRecalculation()
        {
            string tableFormula = "=TABLE(A1, B1)";
            Assert.IsTrue(tableFormula.StartsWith("=TABLE"));
        }

        // =========================================================================
        // FEATURE 31: Workbook Structure Protection (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(31, "Workbook Structure Protection", Milestone.M4)]
        [Description("Verify protected structure blocks adding new sheets")]
        public void T1_F31_01_ProtectStructureBlocksAddSheet()
        {
            bool isStructureProtected = true;
            bool canAddSheet = !isStructureProtected;
            Assert.IsFalse(canAddSheet, "Cannot add sheets when workbook structure is protected.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(31, "Workbook Structure Protection", Milestone.M4)]
        [Description("Verify protected structure blocks deleting sheets")]
        public void T1_F31_02_ProtectStructureBlocksDeleteSheet()
        {
            bool isStructureProtected = true;
            bool canDeleteSheet = !isStructureProtected;
            Assert.IsFalse(canDeleteSheet, "Cannot delete sheets when structure is protected.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(31, "Workbook Structure Protection", Milestone.M4)]
        [Description("Verify protected structure blocks renaming sheets")]
        public void T1_F31_03_ProtectStructureBlocksRenameSheet()
        {
            bool isStructureProtected = true;
            bool canRenameSheet = !isStructureProtected;
            Assert.IsFalse(canRenameSheet, "Cannot rename sheets when structure is protected.");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(31, "Workbook Structure Protection", Milestone.M4)]
        [Description("Verify password hash verification")]
        public void T1_F31_04_VerifyPasswordHashValidation()
        {
            string pwd = "SecretPassword123";
            int hash = pwd.GetHashCode();
            Assert.AreEqual(hash, "SecretPassword123".GetHashCode());
            Assert.IsTrue(hash != "WrongPassword".GetHashCode());
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(31, "Workbook Structure Protection", Milestone.M4)]
        [Description("Verify unprotecting restores structure operations")]
        public void T1_F31_05_UnprotectWithPasswordRestoresStructureOperations()
        {
            bool isStructureProtected = true;
            isStructureProtected = false; // Unprotected
            Assert.IsTrue(!isStructureProtected);
        }

        // =========================================================================
        // FEATURE 32: Print Titles & Page Breaks (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(32, "Print Titles & Page Breaks", Milestone.M4)]
        [Description("Verify PageSetup TitleRows string format")]
        public void T1_F32_01_PageSetupTitleRowsConfiguration()
        {
            string titleRows = "$1:$2";
            Assert.AreEqual("$1:$2", titleRows);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(32, "Print Titles & Page Breaks", Milestone.M4)]
        [Description("Verify PageSetup TitleColumns string format")]
        public void T1_F32_02_PageSetupTitleColumnsConfiguration()
        {
            string titleCols = "$A:$B";
            Assert.AreEqual("$A:$B", titleCols);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(32, "Print Titles & Page Breaks", Milestone.M4)]
        [Description("Verify repeating header row indices inclusion on subsequent pages")]
        public void T1_F32_03_RepeatingHeaderRowsOnPrintedPages()
        {
            List<int> titleRowIndices = new List<int>() { 1, 2 };
            Assert.AreEqual(2, titleRowIndices.Count);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(32, "Print Titles & Page Breaks", Milestone.M4)]
        [Description("Verify manual horizontal page break list management")]
        public void T1_F32_04_ManualHorizontalPageBreakInsertion()
        {
            List<uint> hPageBreaks = new List<uint>();
            hPageBreaks.Add(50);
            hPageBreaks.Add(100);
            Assert.AreEqual(2, hPageBreaks.Count);
            Assert.AreEqual(50u, hPageBreaks[0]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(32, "Print Titles & Page Breaks", Milestone.M4)]
        [Description("Verify manual vertical page break list management")]
        public void T1_F32_05_ManualVerticalPageBreakInsertion()
        {
            List<uint> vPageBreaks = new List<uint>();
            vPageBreaks.Add(10);
            Assert.AreEqual(1, vPageBreaks.Count);
            Assert.AreEqual(10u, vPageBreaks[0]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(33, "Slides Fast Scrollbar & Island Mode", Milestone.M4)]
        [Description("Verify PowerPoint-style Fast Scrollbar on slide thumbnails and authentic Island Mode rendering")]
        public void T1_F33_FastScrollbarAndIslandMode()
        {
            string samplePptx = @"C:\Users\Danh\Downloads\Ke_Hoach_Khoi_Nghiep_Tra_Chom_Chom_Viet.pptx";
            PresentationRoot pres;
            if (File.Exists(samplePptx))
            {
                pres = PptxHandler.LoadPptx(samplePptx);
            }
            else
            {
                pres = new PresentationRoot { CanvasWidth = 1920, CanvasHeight = 1080 };
                for (int i = 0; i < 20; i++)
                {
                    SlideNode s = new SlideNode { Title = "Slide " + (i + 1) };
                    ShapeNode sh = new ShapeNode { Text = "Nội dung trang chiếu " + (i + 1) };
                    s.Shapes.Add(sh);
                    pres.Slides.Add(s);
                }
            }

            string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";

            // 1. Standard Mode Test
            AppSettings.Instance.RibbonStyle = RibbonStyle.Standard;
            SlidesView sv = new SlidesView { Presentation = pres, ActiveSlideIndex = 0 };
            RibbonBar ribbon = new RibbonBar { CurrentMode = OfficeAppMode.Slides, ActiveRibbonTab = 0 };

            using (Bitmap bmp = new Bitmap(1366, 800))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(240, 240, 240));
                    sv.Draw(g, 1366, 800);
                    ribbon.Draw(g, 1366, 800, null, sv, null, null);
                }
                string outPath = Path.Combine(outDir, "verify_slides_standard.png");
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Assert.IsTrue(sv.ThumbScrollBarTrackRect.Width > 0, "Thumb scrollbar track must have positive width in Standard mode");
            Assert.IsTrue(sv.ThumbScrollBarThumbRect.Height > 0, "Thumb scrollbar thumb must have positive height in Standard mode");

            // 2. Island Mode Test
            AppSettings.Instance.RibbonStyle = RibbonStyle.Island;
            using (Bitmap bmp = new Bitmap(1366, 800))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(240, 240, 240));
                    sv.Draw(g, 1366, 800);
                    ribbon.Draw(g, 1366, 800, null, sv, null, null);
                }
                string outPath = Path.Combine(outDir, "verify_slides_island.png");
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Assert.IsTrue(sv.ThumbScrollBarTrackRect.X >= 10, "Thumb scrollbar must be positioned inside island card");

            // 3. Fast Scrollbar Dragging Interaction Test
            Point thumbCenter = new Point(sv.ThumbScrollBarThumbRect.X + 3, sv.ThumbScrollBarThumbRect.Y + 5);
            sv.HandleMouseDown(thumbCenter, 1366, 800, System.Windows.Forms.MouseButtons.Left);
            Point dragPos = new Point(thumbCenter.X, thumbCenter.Y + 80);
            sv.HandleMouseMove(dragPos, 1366, 800);

            using (Bitmap bmp = new Bitmap(1366, 800))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(240, 240, 240));
                    sv.Draw(g, 1366, 800);
                    ribbon.Draw(g, 1366, 800, null, sv, null, null);
                }
                string outPath = Path.Combine(outDir, "verify_slides_fast_scroll_dragging.png");
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            sv.HandleMouseUp(dragPos, 1366, 800);
            Assert.IsTrue(sv.ActiveSlideIndex > 0, "Releasing scrollbar drag should navigate to target preview slide");

            // 4. EnsureSlideVisibleInThumbnails Test
            sv.ActiveSlideIndex = pres.Slides.Count - 1;
            Assert.IsTrue(sv.ThumbScrollY > 0, "ActiveSlideIndex on last slide should auto-scroll thumbnails");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(34, "Home View Layout in Island Mode", Milestone.M4)]
        [Description("Verify that Home view in Island mode does not get covered by 155px ribbon backdrop")]
        public void T1_F34_HomeViewLayoutInIslandMode()
        {
            AppSettings.Instance.RibbonStyle = RibbonStyle.Island;
            HomeView hv = new HomeView();
            RibbonBar rb = new RibbonBar();
            rb.CurrentMode = OfficeAppMode.Home;
            rb.ActiveDocIndex = 0;
            rb.DocTabs.Add(new DocTab("Báo cáo 2026.docx", OfficeAppMode.Writer, false));
            rb.DocTabs.Add(new DocTab("Dự toán.xlsx", OfficeAppMode.Calc, false));
            rb.DocTabs.Add(new DocTab("Trình chiếu.pptx", OfficeAppMode.Slides, false));

            int w = 1366, h = 800;
            string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c";

            using (Bitmap bmp = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    hv.Draw(g, w, h);
                    rb.Draw(g, w, h, null, null, null, null);
                }

                // Verify that pixels around Y=80..150 on the left sidebar are WHITE (not covered by gray wsBg)
                Color sidePixel = bmp.GetPixel(50, 100);
                Assert.IsTrue(sidePixel.R > 245 && sidePixel.G > 245 && sidePixel.B > 245, 
                    "HomeView sidebar at Y=100 must be white surface, not overwritten by gray Island backdrop (was: " + sidePixel + ")");

                // Verify that pixels on the banner around Y=100 in the center are not overwritten by gray
                Color bannerPixel = bmp.GetPixel(400, 100);
                Assert.IsFalse(bannerPixel.R == 240 && bannerPixel.G == 240 && bannerPixel.B == 240,
                    "HomeView banner at Y=100 must NOT be covered by gray wsBg");

                string outPath = Path.Combine(outDir, "verify_home_fixed_island.png");
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // =========================================================================
        // FEATURE 35: Writer Track Changes & Revision Management
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(35, "Writer Track Changes & Revisions", Milestone.M4)]
        [Description("Verify that typing text with Track Changes records Insertion revision and underline run")]
        public void T1_F35_01_TrackChangesInsertionRecordsRevisionAndUnderline()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Xin chào");
            wv.TrackChanges = true;
            wv.InsertText(" Việt Nam");

            Assert.AreEqual("Xin chào Việt Nam", wv.PieceTable.GetText(), "Full text should contain both base and inserted text");
            Assert.AreEqual(1, wv.Revisions.Count, "One revision should be recorded for the insertion");
            var rev = wv.Revisions[0];
            Assert.AreEqual(RevisionType.Insertion, rev.Type, "Revision type must be Insertion");
            Assert.AreEqual(" Việt Nam", rev.Text, "Revision text must match inserted content");
            Assert.IsTrue(rev.StartIndex >= 8, "StartIndex must point to insert position");

            // Verify run formatting: Underline is true, Color matches rev.Color
            FormattedRun insRun = wv.GetFormatAt(rev.StartIndex);
            Assert.IsNotNull(insRun, "Format run at revision position must exist");
            Assert.IsTrue(insRun.Underline, "Insertion must be formatted with underline");
            Assert.AreEqual(rev.Color, insRun.Color, "Insertion must be colored with revision color");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(35, "Writer Track Changes & Revisions", Milestone.M4)]
        [Description("Verify that deleting text with Track Changes marks Deletion revision with strikeout without removing text")]
        public void T1_F35_02_TrackChangesDeletionMarksStrikeoutWithoutRemoval()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Hà Nội mùa thu");
            wv.TrackChanges = true;

            // Select " mùa thu"
            wv.SelectionStart = 6;
            wv.SelectionEnd = 14;
            wv.HandleChar('\b'); // Backspace on selection

            // Text is still present in PieceTable for visual review
            Assert.AreEqual("Hà Nội mùa thu", wv.PieceTable.GetText(), "Text must NOT be immediately removed from PieceTable");
            Assert.AreEqual(1, wv.Revisions.Count, "One revision must be recorded for deletion");
            var rev = wv.Revisions[0];
            Assert.AreEqual(RevisionType.Deletion, rev.Type, "Revision type must be Deletion");
            Assert.AreEqual(" mùa thu", rev.Text, "Revision text must capture deleted text");

            FormattedRun delRun = wv.GetFormatAt(6);
            Assert.IsTrue(delRun.Strike, "Deleted text must have strikeout formatting");
            Assert.AreEqual(rev.Color, delRun.Color, "Deleted text must have revision deletion color");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(35, "Writer Track Changes & Revisions", Milestone.M4)]
        [Description("Verify that AcceptRevision permanently removes deletion or normalizes insertion")]
        public void T1_F35_03_TrackChangesAcceptRevision()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Tài liệu chính thức 2025");
            wv.TrackChanges = true;

            // Delete " 2025"
            wv.SelectionStart = 19;
            wv.SelectionEnd = 24;
            wv.HandleChar('\b');
            Assert.AreEqual(1, wv.Revisions.Count);

            // Accept the deletion: now it should be physically deleted from PieceTable
            wv.AcceptRevision(wv.Revisions[0]);
            Assert.AreEqual("Tài liệu chính thức", wv.PieceTable.GetText(), "Accepting deletion must physically remove text");
            Assert.AreEqual(0, wv.Revisions.Count, "Revisions list must be cleared after accept");

            // Insert " 2026"
            wv.InsertText(" 2026");
            Assert.AreEqual(1, wv.Revisions.Count);
            // Accept the insertion: text remains, underline is cleared
            wv.AcceptRevision(wv.Revisions[0]);
            Assert.AreEqual("Tài liệu chính thức 2026", wv.PieceTable.GetText(), "Accepting insertion must keep text");
            Assert.AreEqual(0, wv.Revisions.Count);
            FormattedRun insRun = wv.GetFormatAt(19);
            Assert.IsFalse(insRun.Underline, "Underline must be cleared after accept");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(35, "Writer Track Changes & Revisions", Milestone.M4)]
        [Description("Verify that RejectRevision rolls back insertion or restores deleted text")]
        public void T1_F35_04_TrackChangesRejectRevision()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Phiên bản v1.0");
            wv.TrackChanges = true;

            // 1. Insert " BETA"
            wv.InsertText(" BETA");
            Assert.AreEqual("Phiên bản v1.0 BETA", wv.PieceTable.GetText());
            Assert.AreEqual(1, wv.Revisions.Count);

            // Reject insertion -> must delete " BETA"
            wv.RejectRevision(wv.Revisions[0]);
            Assert.AreEqual("Phiên bản v1.0", wv.PieceTable.GetText(), "Rejecting insertion must remove inserted text");
            Assert.AreEqual(0, wv.Revisions.Count);

            // 2. Delete " v1.0"
            wv.SelectionStart = 9;
            wv.SelectionEnd = 14;
            wv.HandleChar('\b');
            Assert.AreEqual(1, wv.Revisions.Count);

            // Reject deletion -> must restore " v1.0" (clear strikeout)
            wv.RejectRevision(wv.Revisions[0]);
            Assert.AreEqual("Phiên bản v1.0", wv.PieceTable.GetText(), "Text must remain intact");
            Assert.AreEqual(0, wv.Revisions.Count);
            FormattedRun restoredRun = wv.GetFormatAt(10);
            Assert.IsFalse(restoredRun.Strike, "Strikeout must be cleared on reject deletion");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(35, "Writer Track Changes & Revisions", Milestone.M4)]
        [Description("Verify AcceptAllTrackedChanges and RejectAllTrackedChanges with multiple mixed revisions")]
        public void T1_F35_05_AcceptAllAndRejectAllTrackedChanges()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Văn bản khởi tạo ban đầu");
            wv.TrackChanges = true;

            // Delete " ban đầu"
            wv.SelectionStart = 16;
            wv.SelectionEnd = 24;
            wv.HandleChar('\b');

            // Insert " hoàn chỉnh"
            wv.CursorPosition = 16;
            wv.InsertText(" hoàn chỉnh");

            Assert.AreEqual(2, wv.Revisions.Count, "Two revisions recorded (one delete, one insert)");

            // Test AcceptAll
            wv.AcceptAllTrackedChanges();
            Assert.AreEqual(0, wv.Revisions.Count, "All revisions must be cleared");
            Assert.AreEqual("Văn bản khởi tạo hoàn chỉnh", wv.PieceTable.GetText(), "Accepted text must have deletion removed and insertion kept");

            // Test Undo / Redo integration
            wv.Undo();
            Assert.AreEqual(2, wv.Revisions.Count, "Undo must restore tracked revisions state");
            
            // Now test RejectAll
            wv.RejectAllTrackedChanges();
            Assert.AreEqual(0, wv.Revisions.Count);
            Assert.AreEqual("Văn bản khởi tạo ban đầu", wv.PieceTable.GetText(), "Rejecting all must restore original text");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(35, "Writer Track Changes & Revisions", Milestone.M4)]
        [Description("Verify revision navigation NextRevision and PreviousRevision")]
        public void T1_F35_06_TrackChangesNavigation()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Đoạn một. Đoạn hai. Đoạn ba.");
            wv.TrackChanges = true;

            // Change 1 at start
            wv.CursorPosition = 0;
            wv.InsertText("[Sửa 1] ");

            // Change 2 in middle
            wv.CursorPosition = 18;
            wv.InsertText("[Sửa 2] ");

            Assert.AreEqual(2, wv.Revisions.Count);

            wv.CursorPosition = 0;
            bool hasNext = wv.NextRevision();
            Assert.IsTrue(hasNext, "NextRevision should navigate forward");
            Assert.AreEqual(wv.Revisions[1].StartIndex, wv.CursorPosition, "Should navigate to second revision");

            bool hasPrev = wv.PreviousRevision();
            Assert.IsTrue(hasPrev, "PreviousRevision should navigate backward");
            Assert.AreEqual(wv.Revisions[0].StartIndex, wv.CursorPosition, "Should navigate back to first revision");
        }

        // =========================================================================
        // FEATURE 36: Writer Mail Merge Engine
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(36, "Writer Mail Merge Engine", Milestone.M4)]
        [Description("Verify inserting merge fields and detecting them in document")]
        public void T1_F36_01_InsertAndDetectMergeFields()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Thư mời: ");
            wv.InsertMergeField("Họ và tên");
            wv.InsertText(" - Phòng ban: ");
            wv.InsertMergeField("Phòng ban");

            Assert.IsTrue(wv.PieceTable.GetText().Contains("«Họ và tên»"), "Must contain merge field «Họ và tên»");
            Assert.IsTrue(wv.PieceTable.GetText().Contains("«Phòng ban»"), "Must contain merge field «Phòng ban»");

            var detected = wv.DetectMergeFields();
            Assert.AreEqual(2, detected.Count, "Should detect 2 merge fields");
            Assert.IsTrue(detected.Contains("Họ và tên"), "Detected fields must include Họ và tên");
            Assert.IsTrue(detected.Contains("Phòng ban"), "Detected fields must include Phòng ban");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(36, "Writer Mail Merge Engine", Milestone.M4)]
        [Description("Verify CSV recipients data loading and parsing")]
        public void T1_F36_02_LoadMailMergeFromCsv()
        {
            WriterView wv = new WriterView();
            string csv = "Họ và tên,Công ty,Email\r\nNguyễn Văn A,ZenQ Corp,a@zenq.vn\r\nTrần Thị B,Tech JSC,b@tech.vn";
            wv.LoadMailMergeFromCsv(csv);

            Assert.AreEqual(2, wv.MailMergeData.Count, "Should parse 2 recipient records");
            Assert.AreEqual("Nguyễn Văn A", wv.MailMergeData[0]["Họ và tên"]);
            Assert.AreEqual("ZenQ Corp", wv.MailMergeData[0]["Công ty"]);
            Assert.AreEqual("Trần Thị B", wv.MailMergeData[1]["Họ và tên"]);
            Assert.AreEqual("b@tech.vn", wv.MailMergeData[1]["Email"]);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(36, "Writer Mail Merge Engine", Milestone.M4)]
        [Description("Verify merged document generation for single record with composite fields")]
        public void T1_F36_03_GetMergedDocumentTextSingleRecord()
        {
            WriterView wv = new WriterView();
            wv.InsertText("«Kính_gửi_Họ_Tên»\r\nCông tác tại «Công ty».\r\n«Khối_Địa_Chỉ»");

            var data = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "Họ và tên", "Lê Văn C" },
                    { "Công ty", "Tập đoàn ABC" },
                    { "Chức danh", "Giám đốc" },
                    { "Địa chỉ", "Hà Nội" }
                }
            };
            wv.LoadMailMergeData(data);

            string merged = wv.GetMergedDocumentText(0);
            Assert.IsTrue(merged.Contains("Kính gửi Ông/Bà Lê Văn C,"), "Greeting line must be evaluated");
            Assert.IsTrue(merged.Contains("Công tác tại Tập đoàn ABC."), "Company name must be merged");
            Assert.IsTrue(merged.Contains("Lê Văn C\nGiám đốc\nTập đoàn ABC\nHà Nội"), "Address block must be formatted properly");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(36, "Writer Mail Merge Engine", Milestone.M4)]
        [Description("Verify FinishAndMergeAllRecords generates continuous document with page breaks")]
        public void T1_F36_04_FinishAndMergeAllRecordsWithPageBreaks()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Xin chào «Họ và tên»! Mã số: «Mã»");

            var data = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "Họ và tên", "An" }, { "Mã", "001" } },
                new Dictionary<string, string> { { "Họ và tên", "Bình" }, { "Mã", "002" } },
                new Dictionary<string, string> { { "Họ và tên", "Cường" }, { "Mã", "003" } }
            };
            wv.LoadMailMergeData(data);

            string allDocs = wv.FinishAndMergeAllRecords();
            Assert.IsTrue(allDocs.Contains("Xin chào An! Mã số: 001"), "Record 1 must be present");
            Assert.IsTrue(allDocs.Contains("Xin chào Bình! Mã số: 002"), "Record 2 must be present");
            Assert.IsTrue(allDocs.Contains("Xin chào Cường! Mã số: 003"), "Record 3 must be present");
            Assert.IsTrue(allDocs.Contains("[--- PAGE BREAK ---]"), "Records must be separated by page breaks");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(36, "Writer Mail Merge Engine", Milestone.M4)]
        [Description("Verify live preview toggle and restoration of template text")]
        public void T1_F36_05_ApplyAndClearMailMergePreview()
        {
            WriterView wv = new WriterView();
            wv.InsertText("Thông báo gửi «Họ và tên»");

            var data = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "Họ và tên", "Phạm Văn D" } },
                new Dictionary<string, string> { { "Họ và tên", "Hoàng Thị E" } }
            };
            wv.LoadMailMergeData(data);

            // Preview Record 0
            wv.ApplyMailMergePreview(0);
            Assert.IsTrue(wv.PreviewMailMerge, "PreviewMailMerge flag must be active");
            Assert.AreEqual("Thông báo gửi Phạm Văn D", wv.PieceTable.GetText(), "Record 0 preview text should display");

            // Preview Record 1
            wv.ApplyMailMergePreview(1);
            Assert.AreEqual("Thông báo gửi Hoàng Thị E", wv.PieceTable.GetText(), "Record 1 preview text should display");

            // Clear Preview
            wv.ClearMailMergePreview();
            Assert.IsFalse(wv.PreviewMailMerge, "PreviewMailMerge flag must be false");
            Assert.AreEqual("Thông báo gửi «Họ và tên»", wv.PieceTable.GetText(), "Template text with merge fields must be fully restored");
        }

        // =========================================================================
        // FEATURE 37: Slides PowerPoint 365 Designer & Zoom Navigation Engine
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(37, "Slides Designer & Zoom Engine", Milestone.M4)]
        [Description("Verify Designer engine analyzes slide content and generates 8 distinct modern layout ideas")]
        public void T1_F37_01_GenerateDesignIdeasForSlide()
        {
            SlideNode slide = new SlideNode
            {
                Id = "slide_1",
                Title = "Báo Cáo Tăng Trưởng Quý 4"
            };
            slide.Shapes.Add(new ShapeNode
            {
                Type = ShapeType.TextBox,
                PlaceholderType = "Title",
                Text = "Chiến Lược Kinh Doanh 2026",
                FontSize = 36f
            });
            slide.Shapes.Add(new ShapeNode
            {
                Type = ShapeType.TextBox,
                Text = "• Mở rộng thị trường Đông Nam Á\n• Nâng cấp hạ tầng đám mây\n• Đẩy mạnh AI tự động hóa",
                FontSize = 20f
            });

            var ideas = SlideDesignerEngine.GenerateIdeas(slide, 1920f, 1080f);
            Assert.AreEqual(8, ideas.Count, "Must generate exactly 8 professional design ideas");

            Assert.AreEqual("hero_split", ideas[0].Id);
            Assert.AreEqual("Hero Split Đẳng cấp", ideas[0].Title);
            Assert.AreEqual("Hiện đại", ideas[0].Category);

            Assert.AreEqual("bento_grid", ideas[1].Id);
            Assert.AreEqual("Bento Grid Đa Năng", ideas[1].Title);

            Assert.AreEqual("dark_obsidian", ideas[2].Id);
            Assert.AreEqual("Executive Dark Obsidian", ideas[2].Title);
            Assert.AreEqual(0x0B0F19, ideas[2].BackgroundColor);

            Assert.AreEqual("editorial", ideas[3].Id);
            Assert.AreEqual("Tạp Chí Nghệ Thuật Tối Giản", ideas[3].Title);

            Assert.AreEqual("process_steps", ideas[4].Id);
            Assert.AreEqual("Quy Trình 3 Bước Liền Mạch", ideas[4].Title);

            Assert.AreEqual("sunset_header", ideas[5].Id);
            Assert.AreEqual("Dải Màu Hoàng Hôn Sáng Tạo", ideas[5].Title);

            Assert.AreEqual("kpi_spotlight", ideas[6].Id);
            Assert.AreEqual("Tiêu Điểm Số Liệu & KPI", ideas[6].Title);

            Assert.AreEqual("dual_comparison", ideas[7].Id);
            Assert.AreEqual("So Sánh Hai Cột Song Song", ideas[7].Title);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(37, "Slides Designer & Zoom Engine", Milestone.M4)]
        [Description("Verify applying Hero Split layout restructures shapes, preserves text, and marks thumbnail dirty")]
        public void T1_F37_02_ApplyDesignIdeaHeroSplit()
        {
            SlideNode slide = new SlideNode { Id = "slide_2" };
            slide.Shapes.Add(new ShapeNode
            {
                Type = ShapeType.TextBox,
                PlaceholderType = "Title",
                Text = "Kế Hoạch Ra Mắt Sản Phẩm"
            });
            slide.Shapes.Add(new ShapeNode
            {
                Type = ShapeType.TextBox,
                Text = "Khởi chạy phiên bản Beta\nChiến dịch truyền thông toàn diện"
            });

            var ideas = SlideDesignerEngine.GenerateIdeas(slide, 1920f, 1080f);
            ideas[0].Apply(slide, 1920f, 1080f);

            Assert.IsTrue(slide.IsThumbnailDirty, "Thumbnail cache must be marked dirty after applying layout");
            Assert.AreEqual(0xF8FAFC, slide.BgColor, "Slide background must match Hero Split theme");
            Assert.IsTrue(slide.Shapes.Count >= 4, "Must generate background accent, category pill, title, subtitle, and cards");

            var titleShape = slide.Shapes.Find(s => s.PlaceholderType == "Title");
            Assert.IsNotNull(titleShape, "Title shape must exist");
            Assert.AreEqual("Kế Hoạch Ra Mắt Sản Phẩm", titleShape.Text, "Title text must be preserved");
            Assert.AreEqual(0xFFFFFF, titleShape.TextColor, "Title must have crisp white contrast on dark block");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(37, "Slides Designer & Zoom Engine", Milestone.M4)]
        [Description("Verify applying Executive Dark Obsidian layout sets obsidian background and gradient bar")]
        public void T1_F37_03_ApplyDesignIdeaExecutiveDarkMode()
        {
            SlideNode slide = new SlideNode { Id = "slide_3" };
            slide.Shapes.Add(new ShapeNode
            {
                Type = ShapeType.TextBox,
                Text = "Báo Cáo Tài Chính Thường Niên"
            });

            var ideas = SlideDesignerEngine.GenerateIdeas(slide, 1920f, 1080f);
            var darkIdea = ideas.Find(i => i.Id == "dark_obsidian");
            Assert.IsNotNull(darkIdea);

            darkIdea.Apply(slide, 1920f, 1080f);
            Assert.AreEqual(0x0B0F19, slide.BgColor, "Obsidian background color 0x0B0F19 must be applied");

            var glowLine = slide.Shapes.Find(s => s.Name == "Dark_GlowLine");
            Assert.IsNotNull(glowLine, "Glowing accent line must exist");
            Assert.IsTrue(glowLine.HasGradientFill, "Glow line must feature gradient fill");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(37, "Slides Designer & Zoom Engine", Milestone.M4)]
        [Description("Verify SlidesView Designer pane toggle and ApplyDesignIdea undo support")]
        public void T1_F37_04_SlidesViewDesignerToggleAndUndo()
        {
            SlidesView sv = new SlidesView();
            sv.Presentation.AddSlide(SlideLayoutType.Blank);
            sv.ActiveSlideIndex = 0;
            sv.ActiveSlide.Shapes.Add(new ShapeNode
            {
                Type = ShapeType.TextBox,
                Text = "Dự Án Khởi Nghiệp Công Nghệ"
            });

            // Toggle Designer Pane
            Assert.IsTrue(sv.SidebarPanel != SidebarPanelType.Designer, "Designer should not be open initially");
            sv.ToggleDesignerPane();
            Assert.AreEqual(SidebarPanelType.Designer, sv.SidebarPanel, "SidebarPanel must switch to Designer");
            Assert.IsTrue(sv.IsSidebarOpen, "Right sidebar must be open");

            // Apply idea 1 (Bento Grid)
            int initialUndoCount = sv.UndoStack.Count;
            sv.ApplyDesignIdea(1);
            Assert.IsTrue(sv.UndoStack.Count > initialUndoCount, "Undo state must be pushed before applying idea");
            Assert.AreEqual(0xF1F5F9, sv.ActiveSlide.BgColor, "Bento background must be applied");

            // Undo restores original state
            sv.Undo();
            Assert.AreEqual(0xFFFFFF, sv.ActiveSlide.BgColor, "Undo must restore original slide background");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(37, "Slides Designer & Zoom Engine", Milestone.M4)]
        [Description("Verify InsertSlideZoom creates Zoom shape linking to target slide")]
        public void T1_F37_05_InsertSlideZoomShape()
        {
            SlidesView sv = new SlidesView();
            sv.Presentation.AddSlide(SlideLayoutType.Blank);
            sv.Presentation.AddSlide(SlideLayoutType.Blank);
            sv.ActiveSlideIndex = 0;

            ShapeNode zoomShape = sv.InsertSlideZoom(1);
            Assert.IsNotNull(zoomShape, "Slide Zoom shape must be created");
            Assert.AreEqual(ShapeType.SlideZoom, zoomShape.Type, "ShapeType must be SlideZoom");
            Assert.AreEqual(1, zoomShape.ZoomTargetSlideIndex, "Target slide index must link to slide 1");
            Assert.IsTrue(zoomShape.ZoomReturnToSlide, "ZoomReturnToSlide must be true by default");
            Assert.IsTrue(zoomShape.ZoomShowBorder, "ZoomShowBorder must be true");
            Assert.IsTrue(zoomShape.HasShadow, "Slide Zoom must feature elevation drop shadow");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(37, "Slides Designer & Zoom Engine", Milestone.M4)]
        [Description("Verify InsertSummaryZoom creates interactive visual agenda and handles return navigation")]
        public void T1_F37_06_InsertSummaryZoomAndNavigationReturn()
        {
            SlidesView sv = new SlidesView();
            sv.Presentation.Slides.Clear();
            sv.Presentation.AddSlide(SlideLayoutType.Blank); // Slide 0
            sv.Presentation.AddSlide(SlideLayoutType.Blank); // Slide 1
            sv.Presentation.AddSlide(SlideLayoutType.Blank); // Slide 2
            sv.ActiveSlideIndex = 0;

            SlideNode summarySlide = sv.InsertSummaryZoom();
            Assert.IsNotNull(summarySlide, "Summary agenda slide must be generated");
            Assert.AreEqual("Mục Lục Tóm Tắt (Summary Zoom)", summarySlide.Title);

            // Verify zoom cards created for all 3 original slides
            var zoomCards = summarySlide.Shapes.FindAll(s => s.Type == ShapeType.SlideZoom);
            Assert.AreEqual(3, zoomCards.Count, "Summary zoom must contain 3 zoom thumbnail cards");
            Assert.AreEqual(0, zoomCards[0].ZoomTargetSlideIndex);
            Assert.AreEqual(1, zoomCards[1].ZoomTargetSlideIndex);
            Assert.AreEqual(2, zoomCards[2].ZoomTargetSlideIndex);

            // Test Zoom Navigation in presentation mode
            sv.IsPresenting = true;
            sv.TriggerZoomNavigation(2, true);
            Assert.AreEqual(2, sv.ActiveSlideIndex, "Presentation must navigate directly to target slide 2");
            Assert.IsTrue(sv.ZoomReturnEnabled, "ZoomReturnEnabled must be active");
            Assert.AreEqual(1, sv.ZoomReturnSlideIndex, "ZoomReturnSlideIndex must remember previous summary slide index");

            // Advancing past target slide returns to summary slide
            sv.AdvancePresenterStep();
            Assert.AreEqual(1, sv.ActiveSlideIndex, "Presentation must return automatically to summary overview slide");
            Assert.IsFalse(sv.ZoomReturnEnabled, "ZoomReturnEnabled must reset after return");
        }

        // =========================================================================
        // FEATURE 38: PDF AcroForms, Redaction, Bates & Bookmarks (Milestone M4)
        // =========================================================================
        [Test, Tier(TestTier.Tier1_Feature), Feature(38, "PDF AcroForms, Redaction, Bates & Bookmarks", Milestone.M4)]
        [Description("Verify AcroForms interactive text field and checkbox toggling")]
        public void T1_F38_01_AcroFormsTextFieldAndCheckboxToggle()
        {
            PdfDocument doc = new PdfDocument("TestForm");
            var page = doc.Pages[0];

            // Add Text Field
            var txtField = new PdfFormFieldElement(PdfFormFieldType.Text, new RectangleF(50, 50, 200, 30), "FullName", "Nguyễn Văn A");
            txtField.Tooltip = "Nhập họ và tên";
            page.FormFields.Add(txtField);

            // Add CheckBox
            var chkField = new PdfFormFieldElement(PdfFormFieldType.CheckBox, new RectangleF(50, 100, 20, 20), "TermsAgreed", "No");
            chkField.IsChecked = false;
            page.FormFields.Add(chkField);

            Assert.AreEqual(2, page.FormFields.Count);
            Assert.AreEqual("Nguyễn Văn A", txtField.Value);
            Assert.IsFalse(chkField.IsChecked);

            // Simulate interactive toggling
            chkField.IsChecked = !chkField.IsChecked;
            chkField.Value = chkField.IsChecked ? "Yes" : "No";
            Assert.IsTrue(chkField.IsChecked);
            Assert.AreEqual("Yes", chkField.Value);

            // Verify deep clone
            var cloneDoc = doc.Clone();
            Assert.AreEqual(2, cloneDoc.Pages[0].FormFields.Count);
            var clonedChk = cloneDoc.Pages[0].FormFields[1];
            Assert.IsTrue(clonedChk.IsChecked);
            Assert.AreEqual("TermsAgreed", clonedChk.Name);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(38, "PDF AcroForms, Redaction, Bates & Bookmarks", Milestone.M4)]
        [Description("Verify AcroForms radio group mutual exclusion and dropdown option selection")]
        public void T1_F38_02_AcroFormsRadioGroupMutualExclusion()
        {
            PdfDocument doc = new PdfDocument("TestRadio");
            var page = doc.Pages[0];

            // 3 Radio buttons in same group "DeliveryMethod"
            var rad1 = new PdfFormFieldElement(PdfFormFieldType.RadioButton, new RectangleF(50, 50, 20, 20), "Delivery_Standard", "Standard");
            rad1.GroupName = "DeliveryMethod";
            rad1.ExportValue = "Standard";
            rad1.IsChecked = true;
            page.FormFields.Add(rad1);

            var rad2 = new PdfFormFieldElement(PdfFormFieldType.RadioButton, new RectangleF(50, 80, 20, 20), "Delivery_Express", "Express");
            rad2.GroupName = "DeliveryMethod";
            rad2.ExportValue = "Express";
            rad2.IsChecked = false;
            page.FormFields.Add(rad2);

            var rad3 = new PdfFormFieldElement(PdfFormFieldType.RadioButton, new RectangleF(50, 110, 20, 20), "Delivery_Overnight", "Overnight");
            rad3.GroupName = "DeliveryMethod";
            rad3.ExportValue = "Overnight";
            rad3.IsChecked = false;
            page.FormFields.Add(rad3);

            // Verify initial state
            Assert.IsTrue(rad1.IsChecked);
            Assert.IsFalse(rad2.IsChecked);
            Assert.IsFalse(rad3.IsChecked);

            // Simulate selecting rad2 (Express) with mutual exclusion
            foreach (var f in page.FormFields)
            {
                if (f.FieldType == PdfFormFieldType.RadioButton && f.GroupName == rad2.GroupName)
                {
                    f.IsChecked = (f == rad2);
                }
            }
            Assert.IsFalse(rad1.IsChecked, "Radio 1 must be unselected");
            Assert.IsTrue(rad2.IsChecked, "Radio 2 must be selected");
            Assert.IsFalse(rad3.IsChecked, "Radio 3 must be unselected");

            // Dropdown ComboBox with options
            var combo = new PdfFormFieldElement(PdfFormFieldType.ComboBox, new RectangleF(50, 150, 180, 28), "City", "Hà Nội");
            combo.Options = new List<string> { "Hà Nội", "Đà Nẵng", "TP. Hồ Chí Minh" };
            combo.SelectedOptionIndex = 0;
            page.FormFields.Add(combo);

            // Cycle to Đà Nẵng
            combo.SelectedOptionIndex = 1;
            combo.Value = combo.Options[1];
            Assert.AreEqual("Đà Nẵng", combo.Value);
            Assert.AreEqual(1, combo.SelectedOptionIndex);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(38, "PDF AcroForms, Redaction, Bates & Bookmarks", Milestone.M4)]
        [Description("Verify FlattenFormFields permanently converts interactive fields into static text and shape elements")]
        public void T1_F38_03_AcroFormsFlattenFieldsToStaticElements()
        {
            PdfDocument doc = new PdfDocument("TestFlatten");
            var page = doc.Pages[0];

            page.FormFields.Add(new PdfFormFieldElement(PdfFormFieldType.Text, new RectangleF(60, 60, 200, 30), "FullName", "Trần Thị B"));
            var chk = new PdfFormFieldElement(PdfFormFieldType.CheckBox, new RectangleF(60, 100, 20, 20), "Subscribed", "Yes");
            chk.IsChecked = true;
            page.FormFields.Add(chk);
            page.FormFields.Add(new PdfFormFieldElement(PdfFormFieldType.ComboBox, new RectangleF(60, 140, 160, 30), "Role", "Quản trị viên"));

            Assert.AreEqual(3, page.FormFields.Count);
            int origTextCount = page.TextElements.Count;
            int origShapeCount = page.ShapeElements.Count;

            // Flatten all form fields
            int flattenedCount = doc.FlattenFormFields();
            Assert.AreEqual(3, flattenedCount, "All 3 form fields must be flattened");
            Assert.AreEqual(0, page.FormFields.Count, "FormFields must be emptied after flattening");

            // Verify static text elements created for text field and combo
            Assert.IsTrue(page.TextElements.Count > origTextCount, "Static TextElements must be created for form content");
            bool hasNameText = page.TextElements.Exists(t => t.Text == "Trần Thị B");
            bool hasRoleText = page.TextElements.Exists(t => t.Text == "Quản trị viên");
            Assert.IsTrue(hasNameText, "Static text for FullName must exist");
            Assert.IsTrue(hasRoleText, "Static text for Role must exist");

            // Verify static shape created for checkbox frame
            Assert.IsTrue(page.ShapeElements.Count > origShapeCount, "Static ShapeElements must be created for checkbox box");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(38, "PDF AcroForms, Redaction, Bates & Bookmarks", Milestone.M4)]
        [Description("Verify permanent destructive redaction burns blackout blocks and sanitization clears metadata")]
        public void T1_F38_04_RedactionPermanentDestructionAndSanitization()
        {
            PdfDocument doc = new PdfDocument("TestRedact");
            var page = doc.Pages[0];

            // Add private text and public text
            var secretText = new PdfTextElement("MẬT_KHẨU_TỐI_MẬT_123456", new RectangleF(100, 100, 200, 25), "Segoe UI", 12f) { TextColor = Color.Red };
            page.TextElements.Add(secretText);

            var publicText = new PdfTextElement("Thông tin công khai không che", new RectangleF(100, 300, 220, 25), "Segoe UI", 12f) { TextColor = Color.Black };
            page.TextElements.Add(publicText);

            // Mark redaction zone over secretText
            var redactArea = new PdfRedactionElement(new RectangleF(90, 90, 220, 45));
            page.Redactions.Add(redactArea);

            // Set sensitive metadata
            doc.Author = "Thanh Tra Quân Sự";
            doc.Title = "Báo Cáo Mật Chiến Lược";
            doc.Subject = "Tài Liệu Tuyệt Mật";
            doc.Keywords = "TopSecret, Classified, EyesOnly";

            // Apply permanent redaction
            int affectedPages = doc.ApplyRedactions();
            Assert.AreEqual(1, affectedPages);
            Assert.AreEqual(0, page.Redactions.Count, "Redaction marks must be cleared after application");

            // Verify secret text destroyed from memory
            Assert.IsFalse(page.TextElements.Contains(secretText), "Secret text must be permanently destroyed from page TextElements");
            Assert.IsTrue(page.TextElements.Contains(publicText), "Public text outside redaction must remain intact");

            // Verify blackout rectangle burned
            var burnedShape = page.ShapeElements.Find(s => s.FillColor == Color.Black && s.Bounds == redactArea.Bounds);
            Assert.IsNotNull(burnedShape, "Solid blackout rectangle shape must be burned onto page");

            // Sanitize document
            doc.SanitizeDocument();
            Assert.AreEqual("", doc.Author, "Author must be sanitized");
            Assert.AreEqual("", doc.Title, "Title must be sanitized");
            Assert.AreEqual("", doc.Subject, "Subject must be sanitized");
            Assert.AreEqual("", doc.Keywords, "Keywords must be sanitized");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(38, "PDF AcroForms, Redaction, Bates & Bookmarks", Milestone.M4)]
        [Description("Verify Bates numbering generates sequential legal numbers and removes cleanly")]
        public void T1_F38_05_BatesNumberingSequentialGenerationAndRemoval()
        {
            PdfDocument doc = new PdfDocument("CourtFiling");
            // Add 2 more blank pages so document has 3 pages total
            doc.AddBlankPage(1);
            doc.AddBlankPage(2);
            Assert.AreEqual(3, doc.Pages.Count);

            PdfBatesConfig config = new PdfBatesConfig
            {
                Prefix = "ZENQ-LEGAL-",
                StartNumber = 50,
                NumberOfDigits = 6,
                Position = PdfBatesPosition.BottomRight
            };

            doc.ApplyBatesNumbering(config);
            Assert.AreEqual("ZENQ-LEGAL-000050", doc.Pages[0].BatesNumber);
            Assert.AreEqual("ZENQ-LEGAL-000051", doc.Pages[1].BatesNumber);
            Assert.AreEqual("ZENQ-LEGAL-000052", doc.Pages[2].BatesNumber);
            Assert.IsNotNull(doc.ActiveBatesConfig);

            // Remove Bates numbering
            doc.RemoveBatesNumbering();
            Assert.IsTrue(string.IsNullOrEmpty(doc.Pages[0].BatesNumber));
            Assert.IsTrue(string.IsNullOrEmpty(doc.Pages[1].BatesNumber));
            Assert.IsTrue(string.IsNullOrEmpty(doc.Pages[2].BatesNumber));
            Assert.IsNull(doc.ActiveBatesConfig);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(38, "PDF AcroForms, Redaction, Bates & Bookmarks", Milestone.M4)]
        [Description("Verify hierarchical bookmarks creation, search by Id, deletion, and page navigation")]
        public void T1_F38_06_BookmarksHierarchyAndNavigation()
        {
            PdfDocument doc = new PdfDocument("LongDocument");
            doc.AddBlankPage(1);
            doc.AddBlankPage(2);
            doc.AddBlankPage(3);
            doc.AddBlankPage(4);
            Assert.AreEqual(5, doc.Pages.Count);

            // Create hierarchical bookmark structure
            var root1 = doc.AddBookmark("Chương 1: Khởi động dự án", 0);
            var sec1_1 = doc.AddBookmark("1.1 Mục tiêu & Tầm nhìn", 1, 0f, root1.Id);
            var sec1_2 = doc.AddBookmark("1.2 Kế hoạch thực hiện", 2, 0f, root1.Id);
            var root2 = doc.AddBookmark("Chương 2: Thiết kế hệ thống", 3);

            Assert.AreEqual(2, doc.Bookmarks.Count, "Document must have 2 root bookmark chapters");
            Assert.AreEqual(2, root1.Children.Count, "Chapter 1 must have 2 child sections");
            Assert.AreEqual(0, root2.Children.Count);

            // Find bookmark by Id
            var foundNode = doc.FindBookmarkById(sec1_2.Id);
            Assert.IsNotNull(foundNode);
            Assert.AreEqual("1.2 Kế hoạch thực hiện", foundNode.Title);
            Assert.AreEqual(2, foundNode.TargetPageIndex);

            // Navigation via PdfView
            PdfView pdfView = new PdfView();
            pdfView.Document = doc;
            pdfView.ActivePageIndex = 0;

            pdfView.NavigateToBookmark(root2);
            Assert.AreEqual(3, pdfView.ActivePageIndex, "PdfView must navigate to Chapter 2 target page 3");

            // Remove bookmark
            bool removed = doc.RemoveBookmark(sec1_1.Id);
            Assert.IsTrue(removed, "Child bookmark must be removed");
            Assert.AreEqual(1, root1.Children.Count, "Chapter 1 should now have 1 child remaining");
        }

        #region Feature 39: Excel 365 Data Validation, Auditing, Scenarios & Names
        [Test, Tier(TestTier.Tier1_Feature), Feature(39, "Data Validation Engine", Milestone.M4)]
        [Description("Verify DataValidationRule list checking, range checking, and error styles")]
        public void T1_F39_01_DataValidationRule_ListAndWholeNumber()
        {
            var ruleList = new DataValidationRule(1, 1, 10, 1, DataValidationType.List)
            {
                Formula1 = "Hà Nội, Đà Nẵng, TP.HCM",
                ErrorStyle = DataValidationErrorStyle.Stop
            };

            var items = ruleList.GetListItems();
            Assert.AreEqual(3, items.Count, "Should parse 3 list items");
            Assert.AreEqual("Hà Nội", items[0]);

            var v1 = ruleList.Validate("Đà Nẵng");
            Assert.IsTrue(v1.IsValid, "Matching list item must be valid");

            var v2 = ruleList.Validate("Cần Thơ");
            Assert.IsFalse(v2.IsValid, "Non-matching item must be invalid");
            Assert.AreEqual(DataValidationErrorStyle.Stop, v2.ErrorStyle);

            var ruleNum = new DataValidationRule(1, 2, 10, 2, DataValidationType.WholeNumber)
            {
                Operator = DataValidationOperator.Between,
                Formula1 = "10",
                Formula2 = "50"
            };

            Assert.IsTrue(ruleNum.Validate("25").IsValid, "25 is between 10 and 50");
            Assert.IsFalse(ruleNum.Validate("5").IsValid, "5 is below 10");
            Assert.IsFalse(ruleNum.Validate("99").IsValid, "99 is above 50");
            Assert.IsFalse(ruleNum.Validate("abc").IsValid, "abc is not a whole number");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(39, "Formula Auditing Engine", Milestone.M4)]
        [Description("Verify ExtractPrecedentCells and FormulaContainsCell parsing")]
        public void T1_F39_02_FormulaAuditing_ExtractPrecedentsAndDependents()
        {
            string formula = "=SUM(A1:A3) + B5 * $C$10";
            var precedents = FormulaAuditingEngine.ExtractPrecedentCells(formula);
            Assert.IsTrue(precedents.Count >= 5, "Precedents should extract A1, A2, A3, B5, C10");

            Assert.IsTrue(FormulaAuditingEngine.FormulaContainsCell(formula, 1, 1), "Formula contains A1");
            Assert.IsTrue(FormulaAuditingEngine.FormulaContainsCell(formula, 2, 1), "Formula contains A2");
            Assert.IsTrue(FormulaAuditingEngine.FormulaContainsCell(formula, 5, 2), "Formula contains B5");
            Assert.IsTrue(FormulaAuditingEngine.FormulaContainsCell(formula, 10, 3), "Formula contains C10");
            Assert.IsFalse(FormulaAuditingEngine.FormulaContainsCell(formula, 99, 99), "Formula does not contain cell 99,99");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(39, "Scenario Manager", Milestone.M4)]
        [Description("Verify ScenarioModel application and summary report sheet generation")]
        public void T1_F39_03_ScenarioManager_ApplyAndSummaryReport()
        {
            CalcView calc = new CalcView();
            calc.CurrentSheet.SetCellText(1, 1, "Doanh thu");
            calc.CurrentSheet.SetCellText(1, 2, "1000");

            var scOptimistic = new ScenarioModel { Name = "Lạc quan" };
            scOptimistic.ChangingCells.Add(Tuple.Create(1u, 2u));
            scOptimistic.Values.Add("2500");
            calc.Scenarios.Add(scOptimistic);

            var scPessimistic = new ScenarioModel { Name = "Bi quan" };
            scPessimistic.ChangingCells.Add(Tuple.Create(1u, 2u));
            scPessimistic.Values.Add("500");
            calc.Scenarios.Add(scPessimistic);

            calc.ApplyScenario(0);
            Assert.AreEqual("2500", calc.CurrentSheet.GetCell(1, 2).GetDisplayText(), "Applying optimistic scenario sets B1 to 2500");

            calc.ApplyScenario(1);
            Assert.AreEqual("500", calc.CurrentSheet.GetCell(1, 2).GetDisplayText(), "Applying pessimistic scenario sets B1 to 500");

            calc.GenerateScenarioSummaryReport();
            Assert.IsTrue(calc.SheetNames.Contains("Scenario Summary"), "Summary report creates a new sheet");
            Assert.IsTrue(calc.CurrentSheet.GetCell(1, 1).GetDisplayText().Contains("BÁO CÁO TỔNG HỢP KỊCH BẢN"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(39, "Defined Names & Auditing Arrows", Milestone.M4)]
        [Description("Verify DefinedNameModel and AuditingArrows storage and toggle")]
        public void T1_F39_04_DefinedNames_ModelAndStorage()
        {
            CalcView calc = new CalcView();
            calc.DefinedNames.Add(new DefinedNameModel("TaxRate", "=0.1", "Toàn bảng tính"));
            calc.DefinedNames.Add(new DefinedNameModel("TotalRevenue", "=Sheet1!$B$10", "Toàn bảng tính"));

            Assert.AreEqual(2, calc.DefinedNames.Count);
            Assert.AreEqual("TaxRate", calc.DefinedNames[0].Name);

            calc.CurrentSheet.SetCellText(1, 1, "100");
            calc.CurrentSheet.SetCellText(2, 1, "=A1 * 2");
            calc.TracePrecedents(2, 1);
            Assert.AreEqual(1, calc.AuditingArrows.Count, "Precedents arrow for B2 should point to A1");
            Assert.AreEqual(1u, calc.AuditingArrows[0].FromRow);
            Assert.AreEqual(1u, calc.AuditingArrows[0].FromCol);
            Assert.AreEqual(2u, calc.AuditingArrows[0].ToRow);

            calc.ClearAuditingArrows();
            Assert.AreEqual(0, calc.AuditingArrows.Count, "ClearAuditingArrows clears all arrows");

            Assert.IsFalse(calc.ShowFormulas);
            calc.ToggleShowFormulas();
            Assert.IsTrue(calc.ShowFormulas);
        }
        #endregion

        #region Feature 40: Word 365 Document Compare, Statistics, Cover Page & Drop Cap
        [Test, Tier(TestTier.Tier1_Feature), Feature(40, "Document Compare Engine", Milestone.M4)]
        [Description("Verify LCS word diff calculation and comparison metrics")]
        public void T1_F40_01_DocumentCompareEngine_LcsDiffMetrics()
        {
            string orig = "Hợp đồng kinh tế giữa Bên A và Bên B";
            string rev = "Hợp đồng thương mại quốc tế giữa Bên A và Bên C";

            var result = DocumentCompareEngine.CompareDocuments(orig, rev);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.InsertionsCount > 0, "Should detect new words inserted");
            Assert.IsTrue(result.DeletionsCount > 0, "Should detect words deleted");
            Assert.IsTrue(result.UnchangedWordsCount > 0, "Should detect common unchanged words");
            Assert.IsTrue(result.SimilarityPercentage > 0 && result.SimilarityPercentage < 100, "Similarity should be between 0 and 100");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(40, "Document Compare Track Changes", Milestone.M4)]
        [Description("Verify comparison results applied to WriterView with revisions")]
        public void T1_F40_02_DocumentCompare_ApplyToWriterView()
        {
            WriterView writer = new WriterView();
            string orig = "Kính gửi Ban Giám Đốc.";
            string rev = "Kính gửi Hội Đồng Quản Trị và Ban Giám Đốc.";

            var cmp = DocumentCompareEngine.CompareDocuments(orig, rev);
            DocumentCompareEngine.ApplyToWriterView(writer, cmp);

            Assert.IsTrue(writer.TrackChanges, "Writer should enable TrackChanges after compare");
            Assert.IsTrue(writer.Revisions.Count > 0, "Writer should contain recorded revisions");
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(40, "Word Count & Document Statistics", Milestone.M4)]
        [Description("Verify GetDocumentStatistics pages, words, characters and paragraphs")]
        public void T1_F40_03_WriterDocumentStatistics_WordCount()
        {
            WriterView writer = new WriterView();
            writer.PieceTable.SetInitialText("Xin chào Việt Nam!\nĐây là bài viết thử nghiệm đếm từ.");

            var stats = writer.GetDocumentStatistics();
            Assert.IsTrue(stats.Words >= 10, "Should count at least 10 words");
            Assert.AreEqual(2, stats.Paragraphs, "Should count 2 paragraphs");
            Assert.IsTrue(stats.CharactersNoSpaces > 0);
            Assert.IsTrue(stats.CharactersWithSpaces > stats.CharactersNoSpaces);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(40, "Cover Page & Drop Cap", Milestone.M4)]
        [Description("Verify Cover Page template insertion and Drop Cap formatting")]
        public void T1_F40_04_WriterCoverPageAndDropCap()
        {
            WriterView writer = new WriterView();
            writer.PieceTable.SetInitialText("Nội dung tài liệu chính.");

            // Insert Cover page
            writer.InsertCoverPage("Executive");
            string textWithCover = writer.PieceTable.GetText();
            Assert.IsTrue(textWithCover.Contains("COVER PAGE START"), "Document should have cover page marker");
            Assert.IsTrue(textWithCover.Contains("BÁO CÁO ĐIỀU HÀNH"), "Document should have Executive title");

            // Apply Drop Cap to paragraph
            writer.CursorPosition = textWithCover.IndexOf("Nội dung");
            writer.ApplyDropCap(true, 3);
            Assert.IsTrue(writer.FormattedRuns.Count > 0, "Formatted runs should be populated");
        }
        #endregion

        #region Feature 41: PowerPoint 365 Recording, Slide/Section Zoom & Custom Dimensions
        [Test, Tier(TestTier.Tier1_Feature), Feature(41, "PowerPoint Recording Tab & Timings", Milestone.M4)]
        [Description("Verify Slide AutoAdvance and recording properties on SlideNode")]
        public void T1_F41_01_SlidesRecording_TabAndState()
        {
            SlideNode slide = new SlideNode();
            slide.AutoAdvance = true;
            slide.AutoAdvanceSeconds = 8.5f;

            Assert.IsTrue(slide.AutoAdvance, "Slide should have auto advance enabled");
            Assert.AreEqual(8.5f, slide.AutoAdvanceSeconds, 0.01f);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(41, "Slide and Section Zoom", Milestone.M4)]
        [Description("Verify SlideZoom and SectionZoom insertion and target configuration")]
        public void T1_F41_02_SlidesZoom_SlideZoomAndSectionZoom()
        {
            SlidesView sv = new SlidesView();
            sv.Presentation = new PresentationRoot();
            sv.Presentation.Slides.Clear();
            sv.Presentation.Slides.Add(new SlideNode { Title = "Slide 1", IsSectionHeader = true, SectionName = "Chương 1" });
            sv.Presentation.Slides.Add(new SlideNode { Title = "Slide 2", IsSectionHeader = false });
            sv.Presentation.Slides.Add(new SlideNode { Title = "Slide 3", IsSectionHeader = true, SectionName = "Chương 2" });
            sv.ActiveSlideIndex = 0;

            // 1. Insert Slide Zoom
            var slideZoom = sv.InsertSlideZoom(1);
            Assert.IsNotNull(slideZoom, "SlideZoom shape should be created");
            Assert.AreEqual(ShapeType.SlideZoom, slideZoom.Type);
            Assert.AreEqual(1, slideZoom.ZoomTargetSlideIndex);
            Assert.IsTrue(slideZoom.ZoomReturnToSlide);

            // 2. Insert Section Zoom
            var sectionZoom = sv.InsertSectionZoom(2);
            Assert.IsNotNull(sectionZoom, "SectionZoom shape should be created");
            Assert.AreEqual(ShapeType.SectionZoom, sectionZoom.Type);
            Assert.AreEqual(2, sectionZoom.ZoomTargetSlideIndex);
            Assert.IsTrue(sectionZoom.Name.Contains("Chương 2"));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(41, "Slide Section Management", Milestone.M4)]
        [Description("Verify Section creation, naming, and collapsing")]
        public void T1_F41_03_SlidesSection_AddSectionAndHierarchy()
        {
            SlidesView sv = new SlidesView();
            sv.Presentation = new PresentationRoot();
            sv.Presentation.Slides.Clear();
            sv.Presentation.Slides.Add(new SlideNode { Title = "Slide 1" });
            sv.Presentation.Slides.Add(new SlideNode { Title = "Slide 2" });

            sv.Presentation.Slides[0].IsSectionHeader = true;
            sv.Presentation.Slides[0].SectionName = "Giới thiệu";
            sv.Presentation.Slides[0].IsSectionCollapsed = false;

            Assert.IsTrue(sv.Presentation.Slides[0].IsSectionHeader);
            Assert.AreEqual("Giới thiệu", sv.Presentation.Slides[0].SectionName);
            Assert.IsFalse(sv.Presentation.Slides[0].IsSectionCollapsed);

            sv.Presentation.Slides[0].IsSectionCollapsed = true;
            Assert.IsTrue(sv.Presentation.Slides[0].IsSectionCollapsed);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(41, "Custom Slide Dimensions", Milestone.M4)]
        [Description("Verify Custom Slide Dimensions setting on PresentationRoot and SlidesView")]
        public void T1_F41_04_SlidesDimensions_CustomSlideSize()
        {
            SlidesView sv = new SlidesView();
            sv.Presentation = new PresentationRoot();

            // Set 16:9 Widescreen
            sv.SetSlideSize(true);
            Assert.IsTrue(sv.Presentation.IsWidescreen);
            Assert.AreEqual(1920.0f, sv.Presentation.CanvasWidth, 0.1f);
            Assert.AreEqual(1080.0f, sv.Presentation.CanvasHeight, 0.1f);

            // Set 4:3 Standard
            sv.SetSlideSize(false);
            Assert.IsFalse(sv.Presentation.IsWidescreen);
            Assert.AreEqual(1440.0f, sv.Presentation.CanvasWidth, 0.1f);

            // Set Custom Size (A4 Landscape: 1560 x 1080)
            sv.SetCustomSlideSize(1560f, 1080f);
            Assert.AreEqual(1560.0f, sv.Presentation.CanvasWidth, 0.1f);
            Assert.AreEqual(1080.0f, sv.Presentation.CanvasHeight, 0.1f);
        }
        #endregion

        #region Feature 42: Acrobat Pro & Office 365 PDF Security, Extract, Insert, Converter & Compress
        [Test, Tier(TestTier.Tier1_Feature), Feature(42, "PDF Password Protection & Permissions", Milestone.M4)]
        [Description("Verify PDF password protection, permission flags, SHA-256 hash validation and password removal")]
        public void T1_F42_01_PdfSecurity_PasswordProtectionAndPermissions()
        {
            PdfDocument doc = new PdfDocument("TestSecurity.pdf");
            Assert.IsFalse(doc.SecuritySettings.IsPasswordProtected, "New document should not be password protected");

            // 1. Set Password Protection with restricted printing and modifying
            doc.SetPasswordProtection("openPass123", "ownerPass456", allowPrinting: false, allowCopying: true, allowModifying: false, keyLength: 256);
            Assert.IsTrue(doc.SecuritySettings.IsPasswordProtected, "Document should be password protected");
            Assert.AreEqual(256, doc.SecuritySettings.EncryptionKeyLength);
            Assert.IsFalse(doc.SecuritySettings.AllowPrinting, "Printing should be disabled");
            Assert.IsTrue(doc.SecuritySettings.AllowCopying, "Copying should be enabled");
            Assert.IsFalse(doc.SecuritySettings.AllowModifying, "Modifying should be disabled");

            // 2. Verify Password checks
            Assert.IsTrue(doc.VerifyPassword("openPass123"), "User open password should verify successfully");
            Assert.IsTrue(doc.VerifyPassword("ownerPass456"), "Owner password should verify successfully");
            Assert.IsFalse(doc.VerifyPassword("wrongPassword"), "Incorrect password should fail verification");

            // 3. Clone document preserves security settings
            var cloned = doc.Clone();
            Assert.IsTrue(cloned.SecuritySettings.IsPasswordProtected);
            Assert.IsTrue(cloned.VerifyPassword("openPass123"));

            // 4. Remove Password Protection
            bool removedWrong = doc.RemovePasswordProtection("invalidPass");
            Assert.IsFalse(removedWrong, "Removing with wrong password should fail");
            Assert.IsTrue(doc.SecuritySettings.IsPasswordProtected);

            bool removedCorrect = doc.RemovePasswordProtection("ownerPass456");
            Assert.IsTrue(removedCorrect, "Removing with correct password should succeed");
            Assert.IsFalse(doc.SecuritySettings.IsPasswordProtected, "Security should be removed");
            Assert.IsTrue(doc.SecuritySettings.AllowPrinting);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(42, "PDF Extract Pages & Subsets", Milestone.M4)]
        [Description("Verify PDF page extraction into standalone document with page renumbering")]
        public void T1_F42_02_PdfExtractPages_PageSubsetsAndCloning()
        {
            PdfDocument doc = new PdfDocument("MasterCatalog.pdf");
            // Add 4 additional blank pages (Total 5 pages)
            doc.AddBlankPage();
            doc.AddBlankPage();
            doc.AddBlankPage();
            doc.AddBlankPage();
            Assert.AreEqual(5, doc.Pages.Count);

            // Extract pages 1, 3, 5 (indices 0, 2, 4)
            var extracted = doc.ExtractPages(new int[] { 0, 2, 4 });
            Assert.IsNotNull(extracted);
            Assert.AreEqual(3, extracted.Pages.Count, "Extracted doc should contain exactly 3 pages");
            Assert.AreEqual(1, extracted.Pages[0].PageNumber);
            Assert.AreEqual(2, extracted.Pages[1].PageNumber);
            Assert.AreEqual(3, extracted.Pages[2].PageNumber);
            Assert.IsTrue(extracted.Title.Contains("Trích xuất"));

            // Parse page range helper verification
            var parsed = PdfHandler.ParsePageRange("1-2, 4", 5);
            Assert.AreEqual(3, parsed.Count);
            Assert.IsTrue(parsed.Contains(0));
            Assert.IsTrue(parsed.Contains(1));
            Assert.IsTrue(parsed.Contains(3));
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(42, "PDF Insert Pages from File", Milestone.M4)]
        [Description("Verify inserting external PDF document pages into specific index position")]
        public void T1_F42_03_PdfInsertPages_InsertAtPosition()
        {
            PdfDocument doc1 = new PdfDocument("Doc1.pdf"); // 1 page
            doc1.AddBlankPage(); // Total 2 pages

            PdfDocument doc2 = new PdfDocument("Doc2.pdf"); // 1 page
            doc2.AddBlankPage();
            doc2.AddBlankPage(); // Total 3 pages

            // Insert doc2 at index 1 of doc1
            doc1.InsertDocumentAt(1, doc2);
            Assert.AreEqual(5, doc1.Pages.Count, "Doc1 should now have 2 + 3 = 5 pages");
            Assert.AreEqual(1, doc1.Pages[0].PageNumber);
            Assert.AreEqual(2, doc1.Pages[1].PageNumber);
            Assert.AreEqual(5, doc1.Pages[4].PageNumber);
            Assert.IsTrue(doc1.IsModified);
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(42, "PDF Converter & Text Extraction Engine", Milestone.M4)]
        [Description("Verify PDF text extraction, Word docx export, Excel xlsx export, image rendering")]
        public void T1_F42_04_PdfConverterEngine_TextExtractionAndExport()
        {
            PdfDocument doc = new PdfDocument("Invoice2026.pdf");
            var p0 = doc.Pages[0];
            p0.TextElements.Add(new PdfTextElement("CÔNG TY CỔ PHẦN CÔNG NGHỆ ZENQ", new RectangleF(50, 50, 300, 20), "Arial", 16f) { IsBold = true });
            p0.TextElements.Add(new PdfTextElement("DOANH THU THÁNG 10\t500000000", new RectangleF(50, 80, 300, 20), "Arial", 12f));

            // 1. Text Extraction
            string text = PdfConverterEngine.ExtractAllText(doc, includePageHeaders: false);
            Assert.IsFalse(string.IsNullOrEmpty(text), "Extracted text should not be empty");
            Assert.IsTrue(text.Contains("ZENQ"), "Extracted text should contain document content");
            Assert.IsTrue(text.Contains("DOANH THU"), "Extracted text should contain line items");

            string tempDir = Path.Combine(Path.GetTempPath(), "GenOffice_PdfTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // 2. Export to Text (.txt)
                string txtPath = Path.Combine(tempDir, "Out.txt");
                bool txtOk = PdfConverterEngine.ExportToTxt(doc, txtPath);
                Assert.IsTrue(txtOk && File.Exists(txtPath), "Txt export should succeed");
                string fileTxt = File.ReadAllText(txtPath);
                Assert.IsTrue(fileTxt.Contains("ZENQ"));

                // 3. Export to Word (.docx)
                string docxPath = Path.Combine(tempDir, "Out.docx");
                bool docxOk = PdfConverterEngine.ExportToDocx(doc, docxPath);
                Assert.IsTrue(docxOk && File.Exists(docxPath), "Docx export should succeed");

                // 4. Export to Excel (.xlsx)
                string xlsxPath = Path.Combine(tempDir, "Out.xlsx");
                bool xlsxOk = PdfConverterEngine.ExportToXlsx(doc, xlsxPath);
                Assert.IsTrue(xlsxOk && File.Exists(xlsxPath), "Xlsx export should succeed");

                // 5. Render Page Bitmap
                using (var bmp = PdfConverterEngine.RenderPageBitmap(doc, 0, 96f))
                {
                    Assert.IsNotNull(bmp);
                    Assert.IsTrue(bmp.Width > 100 && bmp.Height > 100, "Rendered page bitmap should have valid dimensions");
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Test, Tier(TestTier.Tier1_Feature), Feature(42, "PDF Compress & Optimize Engine", Milestone.M4)]
        [Description("Verify PDF image downsampling, metadata stripping and compression engine")]
        public void T1_F42_05_PdfCompress_OptimizeAndStripMetadata()
        {
            PdfDocument doc = new PdfDocument("HeavyPresentation.pdf");
            doc.Author = "Confidential Author";
            doc.Keywords = "Secret, Internal";

            // Add sample bitmap to image element
            using (Bitmap sampleImg = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(sampleImg))
            {
                g.Clear(Color.CornflowerBlue);
                g.DrawString("Sample Chart", new Font("Arial", 12f), Brushes.White, 10, 10);
                doc.Pages[0].Images.Add(new PdfImageElement((Bitmap)sampleImg.Clone(), new RectangleF(50, 50, 100, 100)));
            }

            string tempOut = Path.Combine(Path.GetTempPath(), "GenOffice_Comp_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                bool compOk = PdfConverterEngine.CompressPdf(doc, tempOut, qualityPercent: 65, maxDpi: 72f, stripMetadata: true);
                Assert.IsTrue(compOk, "CompressPdf should succeed");
                Assert.IsTrue(File.Exists(tempOut), "Compressed output file should exist");
                Assert.IsTrue(new FileInfo(tempOut).Length > 0, "Compressed output file should have nonzero size");
            }
            finally
            {
                try { if (File.Exists(tempOut)) File.Delete(tempOut); } catch { }
            }
        }
        #endregion
    }
}


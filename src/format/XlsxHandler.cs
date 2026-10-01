using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Format
{
    public class XlsxHandler
    {
        public static bool LoadXlsxOrCsv(string filepath, CalcView calcView)
        {
            if (string.IsNullOrEmpty(filepath) || calcView == null) return false;

            if (filepath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                filepath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                filepath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
            {
                return LoadCsv(filepath, calcView);
            }
            else if (filepath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     filepath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return LoadXlsx(filepath, calcView);
            }

            return false;
        }

        public static void AutoFitPopulatedSheetColumns(SparseMatrix sheet, Dictionary<uint, int> colWidths, Dictionary<uint, int> rowHeights)
        {
            if (sheet == null) return;
            Dictionary<uint, int> maxColCharLens = new Dictionary<uint, int>();
            Dictionary<uint, int> maxRowLines = new Dictionary<uint, int>();

            foreach (var kvp in sheet.GetAllCells())
            {
                var pos = SparseMatrix.DecodeKey(kvp.Key);
                uint r = pos.Item1;
                uint c = pos.Item2;
                CellData cell = kvp.Value;
                if (cell == null) continue;

                string txt = !string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue;
                if (string.IsNullOrEmpty(txt)) continue;

                string[] lines = txt.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                int lineCount = Math.Max(1, lines.Length);
                if (lineCount > 1)
                {
                    if (!maxRowLines.ContainsKey(r) || maxRowLines[r] < lineCount)
                        maxRowLines[r] = lineCount;
                }

                int maxLen = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length > maxLen) maxLen = lines[i].Length;
                }

                if (!maxColCharLens.ContainsKey(c) || maxColCharLens[c] < maxLen)
                {
                    maxColCharLens[c] = maxLen;
                }
            }

            // AutoFit populated columns that don't have explicit custom width
            foreach (var kvp in maxColCharLens)
            {
                uint col = kvp.Key;
                int charLen = kvp.Value;
                if (!colWidths.ContainsKey(col))
                {
                    // Estimate width: 8.2px per character + 22px cell padding
                    int estW = Math.Max(75, (int)(charLen * 8.2 + 22));
                    estW = Math.Min(360, estW); // Cap at 360px so columns remain readable and manageable
                    colWidths[col] = estW;
                }
            }

            // If rows contain multiline text and no explicit row height, adapt row height
            foreach (var kvp in maxRowLines)
            {
                uint r = kvp.Key;
                int lCount = kvp.Value;
                if (!rowHeights.ContainsKey(r))
                {
                    int estH = Math.Min(120, lCount * 18 + 8);
                    if (estH > 24)
                    {
                        rowHeights[r] = estH;
                    }
                }
            }
        }

        public static bool LoadCsv(string filepath, CalcView calcView)
        {
            try
            {
                if (!File.Exists(filepath)) return false;

                calcView.Sheets.Clear();
                calcView.SheetNames.Clear();
                if (calcView.SheetStates == null) calcView.SheetStates = new List<CalcSheetState>();
                else calcView.SheetStates.Clear();

                SparseMatrix sheet = new SparseMatrix();
                calcView.Sheets.Add(sheet);
                calcView.SheetNames.Add(Path.GetFileNameWithoutExtension(filepath));

                string[] lines = File.ReadAllLines(filepath, Encoding.UTF8);
                char delimiter = filepath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';

                for (uint r = 0; r < lines.Length; r++)
                {
                    string line = lines[r];
                    string[] parts = SplitCsvLine(line, delimiter);
                    for (uint c = 0; c < parts.Length; c++)
                    {
                        string val = parts[c].Trim();
                        if (!string.IsNullOrEmpty(val))
                        {
                            sheet.SetCellText(r + 1, c + 1, val);
                        }
                    }
                }

                FormulaEngine.EvaluateSheet(sheet);

                Dictionary<uint, int> csvColWidths = new Dictionary<uint, int>();
                Dictionary<uint, int> csvRowHeights = new Dictionary<uint, int>();
                AutoFitPopulatedSheetColumns(sheet, csvColWidths, csvRowHeights);

                var st = new CalcSheetState();
                st.CustomColWidths = csvColWidths;
                st.CustomRowHeights = csvRowHeights;
                calcView.SheetStates.Add(st);

                calcView.CustomColWidths = new Dictionary<uint, int>(csvColWidths);
                calcView.CustomRowHeights = new Dictionary<uint, int>(csvRowHeights);
                calcView.ActiveSheetIndex = 0;
                calcView.LoadSheetState(0);

                calcView.SelectedRow = 1;
                calcView.SelectedCol = 1;
                calcView.SelEndRow = 1;
                calcView.SelEndCol = 1;
                calcView.ScrollRow = 1;
                calcView.ScrollCol = 1;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SaveXlsxOrCsv(string filepath, CalcView calcView)
        {
            if (string.IsNullOrEmpty(filepath) || calcView == null) return false;

            if (filepath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                filepath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                filepath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
            {
                return SaveCsv(filepath, calcView);
            }
            else
            {
                return SaveXlsx(filepath, calcView);
            }
        }

        public static bool SaveCsv(string filepath, CalcView calcView)
        {
            try
            {
                SparseMatrix sheet = calcView.CurrentSheet;
                if (sheet == null) return false;

                char delimiter = filepath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
                using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
                {
                    uint maxR = 1;
                    uint maxC = 1;
                    foreach (var kvp in sheet.GetAllCells())
                    {
                        var pos = SparseMatrix.DecodeKey(kvp.Key);
                        if (pos.Item1 > maxR) maxR = pos.Item1;
                        if (pos.Item2 > maxC) maxC = pos.Item2;
                    }

                    for (uint r = 1; r <= maxR; r++)
                    {
                        List<string> rowParts = new List<string>();
                        for (uint c = 1; c <= maxC; c++)
                        {
                            CellData cell = sheet.GetCell(r, c);
                            string val = (cell != null) ? (!string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue) : "";
                            if (val.Contains(delimiter.ToString()) || val.Contains("\"") || val.Contains("\n"))
                            {
                                val = "\"" + val.Replace("\"", "\"\"") + "\"";
                            }
                            rowParts.Add(val);
                        }
                        sw.WriteLine(string.Join(delimiter.ToString(), rowParts.ToArray()));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Structs for Style Management
        private class XlsxFont
        {
            public string Name = "Calibri";
            public float Size = 11f;
            public bool Bold = false;
            public bool Italic = false;
            public bool Underline = false;
            public string ColorRgb = "FF000000";

            public string GetKey()
            {
                return string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Name, Size, Bold, Italic, Underline, ColorRgb);
            }
        }

        private class XlsxFill
        {
            public string PatternType = "none";
            public string FgColorRgb = "";

            public string GetKey()
            {
                return string.Format("{0}|{1}", PatternType, FgColorRgb);
            }
        }

        private class XlsxBorderItem
        {
            public bool HasBorder = false;
            public string Style = "thin";
            public string ColorRgb = "FFCBD5E1";
        }

        private class XlsxBorder
        {
            public XlsxBorderItem Left = new XlsxBorderItem();
            public XlsxBorderItem Right = new XlsxBorderItem();
            public XlsxBorderItem Top = new XlsxBorderItem();
            public XlsxBorderItem Bottom = new XlsxBorderItem();
            public bool DiagonalDown = false;
            public bool DiagonalUp = false;
            public string DiagonalStyle = "thin";
            public string DiagonalColorRgb = "FF000000";

            public string GetKey()
            {
                return string.Format("L:{0}_{1}_{2}|R:{3}_{4}_{5}|T:{6}_{7}_{8}|B:{9}_{10}_{11}|Diag:{12}_{13}_{14}_{15}",
                    Left.HasBorder, Left.Style, Left.ColorRgb,
                    Right.HasBorder, Right.Style, Right.ColorRgb,
                    Top.HasBorder, Top.Style, Top.ColorRgb,
                    Bottom.HasBorder, Bottom.Style, Bottom.ColorRgb,
                    DiagonalDown, DiagonalUp, DiagonalStyle, DiagonalColorRgb);
            }
        }

        private class XlsxXf
        {
            public int FontId = 0;
            public int FillId = 0;
            public int BorderId = 0;
            public int NumFmtId = 0;
            public int HorizontalAlign = -1; // 0=left, 1=center, 2=right
            public int VerticalAlign = -1;   // 0=top, 1=center, 2=bottom
            public bool WrapText = false;

            public string GetKey()
            {
                return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}", FontId, FillId, BorderId, NumFmtId, HorizontalAlign, VerticalAlign, WrapText);
            }
        }
        #endregion

        public static bool LoadXlsx(string filepath, CalcView calcView)
        {
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath)) return false;
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return LoadXlsx(fs, calcView);
            }
        }

        public static bool LoadXlsx(Stream stream, CalcView calcView)
        {
            try
            {
                if (stream == null || calcView == null) return false;

                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
                {
                    // 1. Read Shared Strings (xl/sharedStrings.xml)
                    List<string> sharedStrings = new List<string>();
                    ZipArchiveEntry sstEntry = archive.GetEntry("xl/sharedStrings.xml") ?? archive.GetEntry("xl/SharedStrings.xml");
                    if (sstEntry != null)
                    {
                        using (Stream s = sstEntry.Open())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);
                            XmlNodeList siNodes = doc.GetElementsByTagName("si");
                            foreach (XmlNode si in siNodes)
                            {
                                XmlNodeList tNodes = si.SelectNodes(".//t", doc.CreateNavigator() != null ? new XmlNamespaceManager(doc.NameTable) : null);
                                StringBuilder sb = new StringBuilder();
                                if (tNodes != null && tNodes.Count > 0)
                                {
                                    foreach (XmlNode t in tNodes) sb.Append(t.InnerText);
                                }
                                else
                                {
                                    sb.Append(si.InnerText);
                                }
                                sharedStrings.Add(sb.ToString());
                            }
                        }
                    }

                    // 2. Read Styles (xl/styles.xml)
                    List<XlsxFont> fonts = new List<XlsxFont>();
                    List<XlsxFill> fills = new List<XlsxFill>();
                    List<XlsxBorder> borders = new List<XlsxBorder>();
                    List<XlsxXf> xfs = new List<XlsxXf>();

                    ZipArchiveEntry stylesEntry = archive.GetEntry("xl/styles.xml") ?? archive.GetEntry("xl/Styles.xml");
                    if (stylesEntry != null)
                    {
                        using (Stream s = stylesEntry.Open())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);

                            // Parse Fonts
                            XmlNodeList fontNodes = doc.GetElementsByTagName("font");
                            foreach (XmlNode fn in fontNodes)
                            {
                                XlsxFont f = new XlsxFont();
                                foreach (XmlNode ch in fn.ChildNodes)
                                {
                                    if (ch.LocalName == "name" && ch.Attributes != null && ch.Attributes["val"] != null) f.Name = ch.Attributes["val"].Value;
                                    else if (ch.LocalName == "sz" && ch.Attributes != null && ch.Attributes["val"] != null) float.TryParse(ch.Attributes["val"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out f.Size);
                                    else if (ch.LocalName == "b") f.Bold = true;
                                    else if (ch.LocalName == "i") f.Italic = true;
                                    else if (ch.LocalName == "u") f.Underline = true;
                                    else if (ch.LocalName == "color" && ch.Attributes != null)
                                    {
                                        if (ch.Attributes["rgb"] != null) f.ColorRgb = ch.Attributes["rgb"].Value;
                                    }
                                }
                                fonts.Add(f);
                            }

                            // Parse Fills
                            XmlNodeList fillNodes = doc.GetElementsByTagName("fill");
                            foreach (XmlNode fln in fillNodes)
                            {
                                XlsxFill fl = new XlsxFill();
                                foreach (XmlNode ch in fln.ChildNodes)
                                {
                                    if (ch.LocalName == "patternFill")
                                    {
                                        if (ch.Attributes != null && ch.Attributes["patternType"] != null) fl.PatternType = ch.Attributes["patternType"].Value;
                                        foreach (XmlNode pch in ch.ChildNodes)
                                        {
                                            if (pch.LocalName == "fgColor" && pch.Attributes != null && pch.Attributes["rgb"] != null)
                                            {
                                                fl.FgColorRgb = pch.Attributes["rgb"].Value;
                                            }
                                        }
                                    }
                                }
                                fills.Add(fl);
                            }

                            // Parse Borders
                            XmlNodeList borderNodes = doc.GetElementsByTagName("border");
                            foreach (XmlNode bn in borderNodes)
                            {
                                XlsxBorder b = new XlsxBorder();
                                if (bn.Attributes != null)
                                {
                                    if (bn.Attributes["diagonalDown"] != null) b.DiagonalDown = (bn.Attributes["diagonalDown"].Value == "1" || bn.Attributes["diagonalDown"].Value.ToLower() == "true");
                                    if (bn.Attributes["diagonalUp"] != null) b.DiagonalUp = (bn.Attributes["diagonalUp"].Value == "1" || bn.Attributes["diagonalUp"].Value.ToLower() == "true");
                                }
                                foreach (XmlNode ch in bn.ChildNodes)
                                {
                                    if (ch.LocalName == "left" && ch.Attributes != null && ch.Attributes["style"] != null) { b.Left.HasBorder = true; b.Left.Style = ch.Attributes["style"].Value; }
                                    else if (ch.LocalName == "right" && ch.Attributes != null && ch.Attributes["style"] != null) { b.Right.HasBorder = true; b.Right.Style = ch.Attributes["style"].Value; }
                                    else if (ch.LocalName == "top" && ch.Attributes != null && ch.Attributes["style"] != null) { b.Top.HasBorder = true; b.Top.Style = ch.Attributes["style"].Value; }
                                    else if (ch.LocalName == "bottom" && ch.Attributes != null && ch.Attributes["style"] != null) { b.Bottom.HasBorder = true; b.Bottom.Style = ch.Attributes["style"].Value; }
                                    else if (ch.LocalName == "diagonal" && ch.Attributes != null && ch.Attributes["style"] != null) { b.DiagonalStyle = ch.Attributes["style"].Value; }
                                }
                                borders.Add(b);
                            }

                            // Parse CellXfs
                            XmlNode cellXfsNode = doc.SelectSingleNode("//*[local-name()='cellXfs']");
                            if (cellXfsNode != null)
                            {
                                foreach (XmlNode xfNode in cellXfsNode.ChildNodes)
                                {
                                    if (xfNode.LocalName != "xf") continue;
                                    XlsxXf xf = new XlsxXf();
                                    if (xfNode.Attributes != null)
                                    {
                                        if (xfNode.Attributes["fontId"] != null) int.TryParse(xfNode.Attributes["fontId"].Value, out xf.FontId);
                                        if (xfNode.Attributes["fillId"] != null) int.TryParse(xfNode.Attributes["fillId"].Value, out xf.FillId);
                                        if (xfNode.Attributes["borderId"] != null) int.TryParse(xfNode.Attributes["borderId"].Value, out xf.BorderId);
                                        if (xfNode.Attributes["numFmtId"] != null) int.TryParse(xfNode.Attributes["numFmtId"].Value, out xf.NumFmtId);
                                    }
                                    foreach (XmlNode ch in xfNode.ChildNodes)
                                    {
                                        if (ch.LocalName == "alignment" && ch.Attributes != null)
                                        {
                                            if (ch.Attributes["horizontal"] != null)
                                            {
                                                string h = ch.Attributes["horizontal"].Value.ToLower();
                                                if (h == "center") xf.HorizontalAlign = 1;
                                                else if (h == "right") xf.HorizontalAlign = 2;
                                                else if (h == "left") xf.HorizontalAlign = 0;
                                            }
                                            if (ch.Attributes["vertical"] != null)
                                            {
                                                string v = ch.Attributes["vertical"].Value.ToLower();
                                                if (v == "center") xf.VerticalAlign = 1;
                                                else if (v == "bottom") xf.VerticalAlign = 2;
                                                else if (v == "top") xf.VerticalAlign = 0;
                                            }
                                            if (ch.Attributes["wrapText"] != null)
                                            {
                                                xf.WrapText = (ch.Attributes["wrapText"].Value == "1" || ch.Attributes["wrapText"].Value.ToLower() == "true");
                                            }
                                        }
                                    }
                                    xfs.Add(xf);
                                }
                            }
                        }
                    }

                    // 3. Read Workbook Sheet Names (xl/workbook.xml)
                    List<string> sheetNames = new List<string>();
                    ZipArchiveEntry wbEntry = archive.GetEntry("xl/workbook.xml") ?? archive.GetEntry("xl/Workbook.xml");
                    if (wbEntry != null)
                    {
                        using (Stream s = wbEntry.Open())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);
                            XmlNodeList sNodes = doc.GetElementsByTagName("sheet");
                            foreach (XmlNode sn in sNodes)
                            {
                                if (sn.Attributes != null && sn.Attributes["name"] != null)
                                {
                                    sheetNames.Add(sn.Attributes["name"].Value);
                                }
                            }
                        }
                    }

                    // 4. Read Worksheet XMLs (xl/worksheets/sheet*.xml)
                    List<SparseMatrix> loadedSheets = new List<SparseMatrix>();
                    List<string> finalSheetNames = new List<string>();
                    List<Dictionary<uint, int>> loadedColWidthsList = new List<Dictionary<uint, int>>();
                    List<Dictionary<uint, int>> loadedRowHeightsList = new List<Dictionary<uint, int>>();
                    List<int> loadedDefColWList = new List<int>();
                    List<int> loadedDefRowHList = new List<int>();
                    List<List<DocShapeObject>> loadedShapesList = new List<List<DocShapeObject>>();

                    int sheetIdx = 1;
                    while (true)
                    {
                        string entryPath = string.Format("xl/worksheets/sheet{0}.xml", sheetIdx);
                        ZipArchiveEntry wsEntry = archive.GetEntry(entryPath) ?? archive.GetEntry(entryPath.ToLower());
                        if (wsEntry == null && sheetIdx == 1)
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                                {
                                    wsEntry = entry;
                                    break;
                                }
                            }
                        }

                        if (wsEntry == null) break;

                        SparseMatrix sheet = new SparseMatrix();
                        Dictionary<uint, int> curSheetColWidths = new Dictionary<uint, int>();
                        Dictionary<uint, int> curSheetRowHeights = new Dictionary<uint, int>();
                        int curDefColW = 80;
                        int curDefRowH = 24;

                        using (Stream s = wsEntry.Open())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);

                            // Parse sheetFormatPr (defaultColWidth / defaultRowHeight)
                            XmlNodeList sfPrNodes = doc.GetElementsByTagName("sheetFormatPr");
                            if (sfPrNodes.Count > 0 && sfPrNodes[0].Attributes != null)
                            {
                                var defW = sfPrNodes[0].Attributes["defaultColWidth"];
                                var defH = sfPrNodes[0].Attributes["defaultRowHeight"];
                                if (defW != null)
                                {
                                    double dw;
                                    if (double.TryParse(defW.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out dw))
                                    {
                                        curDefColW = Math.Max(30, (int)Math.Round(dw * 7.5 + 5));
                                    }
                                }
                                if (defH != null)
                                {
                                    double dh;
                                    if (double.TryParse(defH.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out dh))
                                    {
                                        curDefRowH = Math.Max(18, (int)Math.Round(dh * 1.333));
                                    }
                                }
                            }

                            // Parse custom column widths
                            XmlNodeList colNodes = doc.GetElementsByTagName("col");
                            foreach (XmlNode colNode in colNodes)
                            {
                                if (colNode.Attributes == null) continue;
                                var minAttr = colNode.Attributes["min"];
                                var maxAttr = colNode.Attributes["max"];
                                var widthAttr = colNode.Attributes["width"];
                                if (minAttr != null && maxAttr != null && widthAttr != null)
                                {
                                    uint minC, maxC;
                                    double wChars;
                                    if (uint.TryParse(minAttr.Value, out minC) && uint.TryParse(maxAttr.Value, out maxC) && double.TryParse(widthAttr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out wChars))
                                    {
                                        int px = Math.Max(25, (int)Math.Round(wChars * 7.5 + 5));
                                        for (uint c = minC; c <= maxC; c++)
                                        {
                                            curSheetColWidths[c] = px;
                                        }
                                    }
                                }
                            }

                            XmlNodeList rowNodes = doc.GetElementsByTagName("row");
                            uint autoRowIdx = 1;
                            foreach (XmlNode rowNode in rowNodes)
                            {
                                uint rNum = autoRowIdx;
                                if (rowNode.Attributes != null)
                                {
                                    var rAttr = rowNode.Attributes["r"];
                                    if (rAttr != null && uint.TryParse(rAttr.Value, out rNum))
                                    {
                                        autoRowIdx = rNum;
                                    }
                                    var htAttr = rowNode.Attributes["ht"];
                                    if (htAttr != null)
                                    {
                                        double htPoints;
                                        if (double.TryParse(htAttr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out htPoints))
                                        {
                                            int pxH = Math.Max(16, (int)Math.Round(htPoints * 1.333));
                                            curSheetRowHeights[rNum] = pxH;
                                        }
                                    }
                                }
                                autoRowIdx++;

                                foreach (XmlNode cNode in rowNode.ChildNodes)
                                {
                                    if (cNode.LocalName != "c") continue;

                                    string cellRef = (cNode.Attributes != null && cNode.Attributes["r"] != null) ? cNode.Attributes["r"].Value : "";
                                    if (string.IsNullOrEmpty(cellRef)) continue;

                                    var pos = SparseMatrix.ParseCellAddress(cellRef);
                                    uint r = pos.Item1;
                                    uint c = pos.Item2;

                                    string cellType = (cNode.Attributes != null && cNode.Attributes["t"] != null) ? cNode.Attributes["t"].Value : "";

                                    XmlNode fNode = null;
                                    XmlNode vNode = null;
                                    XmlNode isNode = null;
                                    foreach (XmlNode child in cNode.ChildNodes)
                                    {
                                        if (child.LocalName == "f") fNode = child;
                                        else if (child.LocalName == "v") vNode = child;
                                        else if (child.LocalName == "is") isNode = child;
                                    }

                                    int sAttrId = 0;
                                    if (cNode.Attributes != null && cNode.Attributes["s"] != null)
                                    {
                                        int.TryParse(cNode.Attributes["s"].Value, out sAttrId);
                                    }

                                    if (fNode != null && !string.IsNullOrEmpty(fNode.InnerText))
                                    {
                                        sheet.SetCellText(r, c, "=" + fNode.InnerText);
                                    }
                                    else if (cellType == "s" && vNode != null)
                                    {
                                        int sstIdx;
                                        if (int.TryParse(vNode.InnerText, out sstIdx) && sstIdx >= 0 && sstIdx < sharedStrings.Count)
                                        {
                                            sheet.SetCellText(r, c, sharedStrings[sstIdx]);
                                        }
                                    }
                                    else if (isNode != null)
                                    {
                                        sheet.SetCellText(r, c, isNode.InnerText);
                                    }
                                    else if (vNode != null)
                                    {
                                        sheet.SetCellText(r, c, vNode.InnerText);
                                    }

                                    // Apply formatting from styles.xml
                                    if (sAttrId >= 0 && sAttrId < xfs.Count)
                                    {
                                        XlsxXf xf = xfs[sAttrId];
                                        CellData loadedCell = sheet.GetOrCreateCell(r, c);
                                        if (loadedCell.Format == null) loadedCell.Format = new CellFormat();

                                        // Apply Font
                                        if (xf.FontId >= 0 && xf.FontId < fonts.Count)
                                        {
                                            XlsxFont f = fonts[xf.FontId];
                                            loadedCell.Format.FontName = f.Name;
                                            loadedCell.Format.FontSize = f.Size;
                                            loadedCell.Format.Bold = f.Bold;
                                            loadedCell.Format.Italic = f.Italic;
                                            loadedCell.Format.Underline = f.Underline;
                                            if (!string.IsNullOrEmpty(f.ColorRgb) && f.ColorRgb.Length >= 6)
                                            {
                                                try { loadedCell.Format.TextColor = ParseArgbHex(f.ColorRgb); } catch { }
                                            }
                                        }

                                        // Apply Fill
                                        if (xf.FillId >= 0 && xf.FillId < fills.Count)
                                        {
                                            XlsxFill fl = fills[xf.FillId];
                                            if (!string.IsNullOrEmpty(fl.FgColorRgb) && fl.FgColorRgb.Length >= 6)
                                            {
                                                try { loadedCell.Format.BgColor = ParseArgbHex(fl.FgColorRgb); } catch { }
                                            }
                                        }

                                        // Apply Border
                                        if (xf.BorderId >= 0 && xf.BorderId < borders.Count)
                                        {
                                            XlsxBorder b = borders[xf.BorderId];
                                            loadedCell.Format.Borders.Top = b.Top.HasBorder;
                                            loadedCell.Format.Borders.Bottom = b.Bottom.HasBorder;
                                            loadedCell.Format.Borders.Left = b.Left.HasBorder;
                                            loadedCell.Format.Borders.Right = b.Right.HasBorder;
                                            loadedCell.Format.Borders.DiagonalDown = b.DiagonalDown;
                                            loadedCell.Format.Borders.DiagonalUp = b.DiagonalUp;
                                            if (b.DiagonalDown || b.DiagonalUp)
                                            {
                                                if (!string.IsNullOrEmpty(loadedCell.RawValue) && loadedCell.RawValue.Contains("\n"))
                                                {
                                                    loadedCell.Format.IsDiagonalHeader = true;
                                                    string[] lines = loadedCell.RawValue.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                                    if (lines.Length >= 2)
                                                    {
                                                        loadedCell.Format.DiagonalTopRightText = lines[0].Trim();
                                                        loadedCell.Format.DiagonalBottomLeftText = lines[1].Trim();
                                                    }
                                                }
                                            }
                                        }

                                        // Apply Alignment
                                        if (xf.HorizontalAlign >= 0) loadedCell.Format.Align = xf.HorizontalAlign;
                                        if (xf.VerticalAlign >= 0) loadedCell.Format.VAlign = xf.VerticalAlign;
                                        loadedCell.Format.WrapText = xf.WrapText;
                                        if (!loadedCell.Format.WrapText && !string.IsNullOrEmpty(loadedCell.RawValue) && (loadedCell.RawValue.Contains("\n") || loadedCell.RawValue.Contains("\r")))
                                        {
                                            loadedCell.Format.WrapText = true;
                                        }
                                    }
                                }
                            }

                            // Parse Merged Cells (<mergeCell ref="A1:C3"/>)
                            XmlNodeList mergeNodes = doc.GetElementsByTagName("mergeCell");
                            foreach (XmlNode mNode in mergeNodes)
                            {
                                if (mNode.Attributes == null || mNode.Attributes["ref"] == null) continue;
                                string mRef = mNode.Attributes["ref"].Value;
                                string[] parts = mRef.Split(':');
                                if (parts.Length == 2)
                                {
                                    var p1 = SparseMatrix.ParseCellAddress(parts[0]);
                                    var p2 = SparseMatrix.ParseCellAddress(parts[1]);
                                    sheet.MergeCells(p1.Item1, p1.Item2, p2.Item1, p2.Item2);
                                }
                            }
                        }

                        // Auto-fit any populated columns not specified in <col> tags (e.g. pandas or scraper exports)
                        AutoFitPopulatedSheetColumns(sheet, curSheetColWidths, curSheetRowHeights);

                        loadedSheets.Add(sheet);
                        string sName = (sheetIdx - 1 < sheetNames.Count) ? sheetNames[sheetIdx - 1] : ("Sheet" + sheetIdx);
                        finalSheetNames.Add(sName);
                        loadedColWidthsList.Add(curSheetColWidths);
                        loadedRowHeightsList.Add(curSheetRowHeights);
                        loadedDefColWList.Add(curDefColW);
                        loadedDefRowHList.Add(curDefRowH);

                        List<DocShapeObject> shList = LoadSheetDrawings(archive, sheetIdx, wsEntry != null ? wsEntry.FullName : null, curSheetColWidths, curSheetRowHeights, curDefColW, curDefRowH);
                        loadedShapesList.Add(shList);

                        sheetIdx++;
                    }

                    if (loadedSheets.Count > 0)
                    {
                        calcView.Sheets.Clear();
                        calcView.SheetNames.Clear();
                        if (calcView.SheetStates == null) calcView.SheetStates = new List<CalcSheetState>();
                        else calcView.SheetStates.Clear();

                        for (int i = 0; i < loadedSheets.Count; i++)
                        {
                            calcView.Sheets.Add(loadedSheets[i]);
                            calcView.SheetNames.Add(finalSheetNames[i]);
                            FormulaEngine.EvaluateSheet(loadedSheets[i]);

                            var st = new CalcSheetState();
                            st.CustomColWidths = loadedColWidthsList[i];
                            st.CustomRowHeights = loadedRowHeightsList[i];
                            st.Shapes = (i < loadedShapesList.Count && loadedShapesList[i] != null) ? loadedShapesList[i] : new List<DocShapeObject>();
                            calcView.SheetStates.Add(st);
                        }

                        if (loadedDefColWList.Count > 0 && loadedDefColWList[0] > 0) calcView.DefaultColWidth = loadedDefColWList[0];
                        if (loadedDefRowHList.Count > 0 && loadedDefRowHList[0] > 0) calcView.DefaultRowHeight = loadedDefRowHList[0];

                        calcView.LoadSheetState(0);

                        calcView.SelectedRow = 1;
                        calcView.SelectedCol = 1;
                        calcView.SelEndRow = 1;
                        calcView.SelEndCol = 1;
                        calcView.ScrollRow = 1;
                        calcView.ScrollCol = 1;

                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static bool SaveXlsx(string filepath, CalcView calcView)
        {
            try
            {
                if (calcView != null) calcView.SaveCurrentSheetState();

                if (File.Exists(filepath))
                {
                    try { File.Delete(filepath); } catch { }
                }

                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // 1. [Content_Types].xml
                    ZipArchiveEntry ctEntry = archive.CreateEntry("[Content_Types].xml");
                    using (StreamWriter sw = new StreamWriter(ctEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                        sw.WriteLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                        sw.WriteLine("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                        sw.WriteLine("  <Default Extension=\"png\" ContentType=\"image/png\"/>");
                        sw.WriteLine("  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
                        sw.WriteLine("  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
                        for (int i = 0; i < calcView.Sheets.Count; i++)
                        {
                            sw.WriteLine(string.Format("  <Override PartName=\"/xl/worksheets/sheet{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>", i + 1));
                        }
                        for (int s = 0; s < calcView.Sheets.Count; s++)
                        {
                            List<DocShapeObject> sShapes = GetSheetShapes(calcView, s);
                            if (sShapes != null && sShapes.Count > 0)
                            {
                                sw.WriteLine(string.Format("  <Override PartName=\"/xl/drawings/drawing{0}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawing+xml\"/>", s + 1));
                            }
                        }
                        sw.WriteLine("</Types>");
                    }

                    // 2. _rels/.rels
                    ZipArchiveEntry rootRelsEntry = archive.CreateEntry("_rels/.rels");
                    using (StreamWriter sw = new StreamWriter(rootRelsEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                        sw.WriteLine("  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>");
                        sw.WriteLine("</Relationships>");
                    }

                    // 3. xl/workbook.xml
                    ZipArchiveEntry wbEntry = archive.CreateEntry("xl/workbook.xml");
                    using (StreamWriter sw = new StreamWriter(wbEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                        sw.WriteLine("  <sheets>");
                        for (int i = 0; i < calcView.Sheets.Count; i++)
                        {
                            string sName = (i < calcView.SheetNames.Count) ? calcView.SheetNames[i] : ("Sheet" + (i + 1));
                            sw.WriteLine(string.Format("    <sheet name=\"{0}\" sheetId=\"{1}\" r:id=\"rId{2}\"/>", EscapeXml(sName), i + 1, i + 1));
                        }
                        sw.WriteLine("  </sheets>");
                        sw.WriteLine("</workbook>");
                    }

                    // 4. xl/_rels/workbook.xml.rels
                    ZipArchiveEntry wbRelsEntry = archive.CreateEntry("xl/_rels/workbook.xml.rels");
                    using (StreamWriter sw = new StreamWriter(wbRelsEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                        for (int i = 0; i < calcView.Sheets.Count; i++)
                        {
                            sw.WriteLine(string.Format("  <Relationship Id=\"rId{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{0}.xml\"/>", i + 1));
                        }
                        sw.WriteLine(string.Format("  <Relationship Id=\"rId{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>", calcView.Sheets.Count + 1));
                        sw.WriteLine("</Relationships>");
                    }

                    // Build Style Tables
                    List<XlsxFont> fonts = new List<XlsxFont>();
                    List<XlsxFill> fills = new List<XlsxFill>();
                    List<XlsxBorder> borders = new List<XlsxBorder>();
                    List<XlsxXf> xfs = new List<XlsxXf>();

                    // Default Font 0
                    fonts.Add(new XlsxFont { Name = "Calibri", Size = 11f, ColorRgb = "FF000000" });
                    // Default Fills 0 & 1
                    fills.Add(new XlsxFill { PatternType = "none" });
                    fills.Add(new XlsxFill { PatternType = "gray125" });
                    // Default Border 0
                    borders.Add(new XlsxBorder());
                    // Default Xf 0
                    xfs.Add(new XlsxXf { FontId = 0, FillId = 0, BorderId = 0, NumFmtId = 0 });

                    Dictionary<string, int> fontIndexMap = new Dictionary<string, int>();
                    fontIndexMap[fonts[0].GetKey()] = 0;

                    Dictionary<string, int> fillIndexMap = new Dictionary<string, int>();
                    fillIndexMap[fills[0].GetKey()] = 0;
                    fillIndexMap[fills[1].GetKey()] = 1;

                    Dictionary<string, int> borderIndexMap = new Dictionary<string, int>();
                    borderIndexMap[borders[0].GetKey()] = 0;

                    Dictionary<string, int> xfIndexMap = new Dictionary<string, int>();
                    xfIndexMap[xfs[0].GetKey()] = 0;

                    // Collect cell styles from sheets
                    Dictionary<CellData, int> cellStyleIds = new Dictionary<CellData, int>();
                    foreach (var sheet in calcView.Sheets)
                    {
                        foreach (var kvp in sheet.GetAllCells())
                        {
                            CellData cell = kvp.Value;
                            if (cell == null) continue;
                            CellFormat fmt = cell.Format ?? new CellFormat();

                            // Font
                            XlsxFont f = new XlsxFont
                            {
                                Name = !string.IsNullOrEmpty(fmt.FontName) ? fmt.FontName : "Calibri",
                                Size = fmt.FontSize > 0 ? fmt.FontSize : 11f,
                                Bold = fmt.Bold,
                                Italic = fmt.Italic,
                                Underline = fmt.Underline,
                                ColorRgb = fmt.TextColor != 0 ? ToArgbHex(Color.FromArgb(fmt.TextColor)) : "FF000000"
                            };
                            string fKey = f.GetKey();
                            int fId;
                            if (!fontIndexMap.TryGetValue(fKey, out fId))
                            {
                                fId = fonts.Count;
                                fonts.Add(f);
                                fontIndexMap[fKey] = fId;
                            }

                            // Fill
                            XlsxFill fl = new XlsxFill();
                            if (fmt.BgColor != unchecked((int)0xFFFFFFFF) && fmt.BgColor != 0)
                            {
                                fl.PatternType = "solid";
                                fl.FgColorRgb = ToArgbHex(Color.FromArgb(fmt.BgColor));
                            }
                            string flKey = fl.GetKey();
                            int flId;
                            if (!fillIndexMap.TryGetValue(flKey, out flId))
                            {
                                flId = fills.Count;
                                fills.Add(fl);
                                fillIndexMap[flKey] = flId;
                            }

                            // Border
                            XlsxBorder b = new XlsxBorder();
                            if (fmt.Borders != null)
                            {
                                var bd = fmt.Borders;
                                if (bd.Left) { b.Left.HasBorder = true; b.Left.Style = bd.LeftThickness > 1 ? "medium" : "thin"; b.Left.ColorRgb = bd.LeftColor != 0 ? ToArgbHex(Color.FromArgb(bd.LeftColor)) : "FF000000"; }
                                if (bd.Right) { b.Right.HasBorder = true; b.Right.Style = bd.RightThickness > 1 ? "medium" : "thin"; b.Right.ColorRgb = bd.RightColor != 0 ? ToArgbHex(Color.FromArgb(bd.RightColor)) : "FF000000"; }
                                if (bd.Top) { b.Top.HasBorder = true; b.Top.Style = bd.TopThickness > 1 ? "medium" : "thin"; b.Top.ColorRgb = bd.TopColor != 0 ? ToArgbHex(Color.FromArgb(bd.TopColor)) : "FF000000"; }
                                if (bd.Bottom) { b.Bottom.HasBorder = true; b.Bottom.Style = bd.BottomThickness > 1 ? "medium" : "thin"; b.Bottom.ColorRgb = bd.BottomColor != 0 ? ToArgbHex(Color.FromArgb(bd.BottomColor)) : "FF000000"; }
                                b.DiagonalDown = bd.DiagonalDown;
                                b.DiagonalUp = bd.DiagonalUp;
                                if (b.DiagonalDown || b.DiagonalUp)
                                {
                                    b.DiagonalStyle = bd.DiagonalThickness > 1 ? "medium" : "thin";
                                    b.DiagonalColorRgb = bd.DiagonalColor != 0 ? ToArgbHex(Color.FromArgb(bd.DiagonalColor)) : "FF000000";
                                }
                            }
                            string bKey = b.GetKey();
                            int bId;
                            if (!borderIndexMap.TryGetValue(bKey, out bId))
                            {
                                bId = borders.Count;
                                borders.Add(b);
                                borderIndexMap[bKey] = bId;
                            }

                            bool isDiagHeader = (fmt.IsDiagonalHeader || (fmt.Borders != null && (fmt.Borders.DiagonalDown || fmt.Borders.DiagonalUp)));

                            // Xf
                            XlsxXf xf = new XlsxXf
                            {
                                FontId = fId,
                                FillId = flId,
                                BorderId = bId,
                                NumFmtId = 0,
                                HorizontalAlign = fmt.Align,
                                VerticalAlign = fmt.VAlign,
                                WrapText = fmt.WrapText || isDiagHeader
                            };
                            string xfKey = xf.GetKey();
                            int xfId;
                            if (!xfIndexMap.TryGetValue(xfKey, out xfId))
                            {
                                xfId = xfs.Count;
                                xfs.Add(xf);
                                xfIndexMap[xfKey] = xfId;
                            }

                            cellStyleIds[cell] = xfId;
                        }
                    }

                    // 5. xl/styles.xml
                    ZipArchiveEntry sEntry = archive.CreateEntry("xl/styles.xml");
                    using (StreamWriter sw = new StreamWriter(sEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                        sw.WriteLine("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

                        // Fonts
                        sw.WriteLine(string.Format("  <fonts count=\"{0}\">", fonts.Count));
                        foreach (var f in fonts)
                        {
                            sw.WriteLine("    <font>");
                            if (f.Bold) sw.WriteLine("      <b/>");
                            if (f.Italic) sw.WriteLine("      <i/>");
                            if (f.Underline) sw.WriteLine("      <u/>");
                            sw.WriteLine(string.Format("      <sz val=\"{0}\"/>", f.Size.ToString(CultureInfo.InvariantCulture)));
                            sw.WriteLine(string.Format("      <color rgb=\"{0}\"/>", f.ColorRgb));
                            sw.WriteLine(string.Format("      <name val=\"{0}\"/>", EscapeXml(f.Name)));
                            sw.WriteLine("    </font>");
                        }
                        sw.WriteLine("  </fonts>");

                        // Fills
                        sw.WriteLine(string.Format("  <fills count=\"{0}\">", fills.Count));
                        foreach (var fl in fills)
                        {
                            sw.WriteLine("    <fill>");
                            if (fl.PatternType == "solid" && !string.IsNullOrEmpty(fl.FgColorRgb))
                            {
                                sw.WriteLine(string.Format("      <patternFill patternType=\"solid\"><fgColor rgb=\"{0}\"/><bgColor indexed=\"64\"/></patternFill>", fl.FgColorRgb));
                            }
                            else
                            {
                                sw.WriteLine(string.Format("      <patternFill patternType=\"{0}\"/>", fl.PatternType));
                            }
                            sw.WriteLine("    </fill>");
                        }
                        sw.WriteLine("  </fills>");

                        // Borders
                        sw.WriteLine(string.Format("  <borders count=\"{0}\">", borders.Count));
                        foreach (var b in borders)
                        {
                            string diagAttrs = "";
                            if (b.DiagonalDown) diagAttrs += " diagonalDown=\"1\"";
                            if (b.DiagonalUp) diagAttrs += " diagonalUp=\"1\"";
                            sw.WriteLine(string.Format("    <border{0}>", diagAttrs));

                            if (b.Left.HasBorder) sw.WriteLine(string.Format("      <left style=\"{0}\"><color rgb=\"{1}\"/></left>", b.Left.Style, b.Left.ColorRgb));
                            else sw.WriteLine("      <left/>");

                            if (b.Right.HasBorder) sw.WriteLine(string.Format("      <right style=\"{0}\"><color rgb=\"{1}\"/></right>", b.Right.Style, b.Right.ColorRgb));
                            else sw.WriteLine("      <right/>");

                            if (b.Top.HasBorder) sw.WriteLine(string.Format("      <top style=\"{0}\"><color rgb=\"{1}\"/></top>", b.Top.Style, b.Top.ColorRgb));
                            else sw.WriteLine("      <top/>");

                            if (b.Bottom.HasBorder) sw.WriteLine(string.Format("      <bottom style=\"{0}\"><color rgb=\"{1}\"/></bottom>", b.Bottom.Style, b.Bottom.ColorRgb));
                            else sw.WriteLine("      <bottom/>");

                            if (b.DiagonalDown || b.DiagonalUp)
                            {
                                sw.WriteLine(string.Format("      <diagonal style=\"{0}\"><color rgb=\"{1}\"/></diagonal>", b.DiagonalStyle, b.DiagonalColorRgb));
                            }
                            else
                            {
                                sw.WriteLine("      <diagonal/>");
                            }

                            sw.WriteLine("    </border>");
                        }
                        sw.WriteLine("  </borders>");

                        // CellStyleXfs
                        sw.WriteLine("  <cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");

                        // CellXfs
                        sw.WriteLine(string.Format("  <cellXfs count=\"{0}\">", xfs.Count));
                        foreach (var xf in xfs)
                        {
                            string alignXml = "";
                            string hAlign = xf.HorizontalAlign == 1 ? "center" : (xf.HorizontalAlign == 2 ? "right" : (xf.HorizontalAlign == 0 ? "left" : ""));
                            string vAlign = xf.VerticalAlign == 1 ? "center" : (xf.VerticalAlign == 2 ? "bottom" : (xf.VerticalAlign == 0 ? "top" : ""));
                            string wrapStr = xf.WrapText ? " wrapText=\"1\"" : "";

                            if (!string.IsNullOrEmpty(hAlign) || !string.IsNullOrEmpty(vAlign) || xf.WrapText)
                            {
                                string hAttr = !string.IsNullOrEmpty(hAlign) ? string.Format(" horizontal=\"{0}\"", hAlign) : "";
                                string vAttr = !string.IsNullOrEmpty(vAlign) ? string.Format(" vertical=\"{0}\"", vAlign) : "";
                                alignXml = string.Format("<alignment{0}{1}{2}/>", hAttr, vAttr, wrapStr);
                            }

                            string applyAttrs = string.Format(" applyFont=\"{0}\" applyFill=\"{1}\" applyBorder=\"{2}\" applyAlignment=\"{3}\"",
                                xf.FontId > 0 ? "1" : "0", xf.FillId > 0 ? "1" : "0", xf.BorderId > 0 ? "1" : "0", !string.IsNullOrEmpty(alignXml) ? "1" : "0");

                            if (!string.IsNullOrEmpty(alignXml))
                            {
                                sw.WriteLine(string.Format("    <xf numFmtId=\"{0}\" fontId=\"{1}\" fillId=\"{2}\" borderId=\"{3}\" xfId=\"0\"{4}>{5}</xf>",
                                    xf.NumFmtId, xf.FontId, xf.FillId, xf.BorderId, applyAttrs, alignXml));
                            }
                            else
                            {
                                sw.WriteLine(string.Format("    <xf numFmtId=\"{0}\" fontId=\"{1}\" fillId=\"{2}\" borderId=\"{3}\" xfId=\"0\"{4}/>",
                                    xf.NumFmtId, xf.FontId, xf.FillId, xf.BorderId, applyAttrs));
                            }
                        }
                        sw.WriteLine("  </cellXfs>");
                        sw.WriteLine("</styleSheet>");
                    }

                    // 6. xl/worksheets/sheet*.xml
                    for (int s = 0; s < calcView.Sheets.Count; s++)
                    {
                        SparseMatrix sheet = calcView.Sheets[s];
                        ZipArchiveEntry wsEntry = archive.CreateEntry(string.Format("xl/worksheets/sheet{0}.xml", s + 1));
                        using (StreamWriter sw = new StreamWriter(wsEntry.Open(), Encoding.UTF8))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

                            // Custom Column Widths
                            if (calcView.CustomColWidths != null && calcView.CustomColWidths.Count > 0)
                            {
                                sw.WriteLine("  <cols>");
                                foreach (var kvp in calcView.CustomColWidths)
                                {
                                    double wChars = Math.Max(1.0, (kvp.Value - 5) / 7.5);
                                    sw.WriteLine(string.Format("    <col min=\"{0}\" max=\"{0}\" width=\"{1}\" customWidth=\"1\"/>", kvp.Key, wChars.ToString("0.00", CultureInfo.InvariantCulture)));
                                }
                                sw.WriteLine("  </cols>");
                            }

                            sw.WriteLine("  <sheetData>");

                            // Group cells by row
                            Dictionary<uint, List<KeyValuePair<uint, CellData>>> rowMap = new Dictionary<uint, List<KeyValuePair<uint, CellData>>>();
                            foreach (var kvp in sheet.GetAllCells())
                            {
                                var pos = SparseMatrix.DecodeKey(kvp.Key);
                                uint r = pos.Item1;
                                uint c = pos.Item2;
                                if (!rowMap.ContainsKey(r)) rowMap[r] = new List<KeyValuePair<uint, CellData>>();
                                rowMap[r].Add(new KeyValuePair<uint, CellData>(c, kvp.Value));
                            }

                            List<uint> rows = new List<uint>(rowMap.Keys);
                            rows.Sort();

                            foreach (uint r in rows)
                            {
                                string htAttr = "";
                                if (calcView.CustomRowHeights != null && calcView.CustomRowHeights.ContainsKey(r))
                                {
                                    double htPt = calcView.CustomRowHeights[r] / 1.333;
                                    htAttr = string.Format(" ht=\"{0}\" customHeight=\"1\"", htPt.ToString("0.00", CultureInfo.InvariantCulture));
                                }

                                sw.WriteLine(string.Format("    <row r=\"{0}\"{1}>", r, htAttr));
                                var cols = rowMap[r];
                                cols.Sort((a, b) => a.Key.CompareTo(b.Key));

                                foreach (var colKvp in cols)
                                {
                                    uint c = colKvp.Key;
                                    CellData cell = colKvp.Value;
                                    string cellRef = SparseMatrix.ColIndexToName(c) + r;

                                    int sId = 0;
                                    if (cellStyleIds.ContainsKey(cell)) sId = cellStyleIds[cell];
                                    string styleAttr = string.Format(" s=\"{0}\"", sId);

                                    if (cell.Type == CellType.Formula && !string.IsNullOrEmpty(cell.FormulaStr))
                                    {
                                        string fClean = cell.FormulaStr.TrimStart('=');
                                        sw.WriteLine(string.Format("      <c r=\"{0}\"{1}><f>{2}</f><v>{3}</v></c>", cellRef, styleAttr, EscapeXml(fClean), cell.NumericValue.ToString(CultureInfo.InvariantCulture)));
                                    }
                                    else if (cell.Type == CellType.Number)
                                    {
                                        sw.WriteLine(string.Format("      <c r=\"{0}\"{1}><v>{2}</v></c>", cellRef, styleAttr, cell.NumericValue.ToString(CultureInfo.InvariantCulture)));
                                    }
                                    else if (!string.IsNullOrEmpty(cell.RawValue))
                                    {
                                        string exportText = cell.RawValue;
                                        if (cell.Format != null && (cell.Format.IsDiagonalHeader || (cell.Format.Borders != null && (cell.Format.Borders.DiagonalDown || cell.Format.Borders.DiagonalUp))))
                                        {
                                            string tr = !string.IsNullOrEmpty(cell.Format.DiagonalTopRightText) ? cell.Format.DiagonalTopRightText : "";
                                            string bl = !string.IsNullOrEmpty(cell.Format.DiagonalBottomLeftText) ? cell.Format.DiagonalBottomLeftText : "";
                                            if (!string.IsNullOrEmpty(tr) && !string.IsNullOrEmpty(bl))
                                            {
                                                int colW = calcView.GetColWidth(c);
                                                int spaces = Math.Max(12, colW / 10);
                                                exportText = new string(' ', spaces) + tr + "\n" + bl;
                                            }
                                        }
                                        sw.WriteLine(string.Format("      <c r=\"{0}\" t=\"inlineStr\"{1}><is><t xml:space=\"preserve\">{2}</t></is></c>", cellRef, styleAttr, EscapeXml(exportText)));
                                    }
                                }
                                sw.WriteLine("    </row>");
                            }

                            sw.WriteLine("  </sheetData>");

                            // Merged Cells Export
                            List<string> mergeRefs = new List<string>();
                            foreach (var kvp in sheet.GetAllCells())
                            {
                                CellData cData = kvp.Value;
                                if (cData != null && cData.Format != null && cData.Format.IsMergedParent && (cData.Format.MergeRowSpan > 1 || cData.Format.MergeColSpan > 1))
                                {
                                    var pos = SparseMatrix.DecodeKey(kvp.Key);
                                    uint r = pos.Item1;
                                    uint c = pos.Item2;
                                    uint endR = (uint)(r + cData.Format.MergeRowSpan - 1);
                                    uint endC = (uint)(c + cData.Format.MergeColSpan - 1);
                                    mergeRefs.Add(string.Format("{0}{1}:{2}{3}", SparseMatrix.ColIndexToName(c), r, SparseMatrix.ColIndexToName(endC), endR));
                                }
                            }

                            if (mergeRefs.Count > 0)
                            {
                                sw.WriteLine(string.Format("  <mergeCells count=\"{0}\">", mergeRefs.Count));
                                foreach (var mRef in mergeRefs)
                                {
                                    sw.WriteLine(string.Format("    <mergeCell ref=\"{0}\"/>", mRef));
                                }
                                sw.WriteLine("  </mergeCells>");
                            }

                            List<DocShapeObject> sShapes = GetSheetShapes(calcView, s);
                            if (sShapes != null && sShapes.Count > 0)
                            {
                                sw.WriteLine(string.Format("  <drawing r:id=\"rIdDrawing{0}\"/>", s + 1));
                            }
                            sw.WriteLine("</worksheet>");
                        }
                    }

                    // 7. DrawingML Export for Shapes & Pictures across all sheets
                    int nextImgId = 1;
                    for (int s = 0; s < calcView.Sheets.Count; s++)
                    {
                        List<DocShapeObject> sShapes = GetSheetShapes(calcView, s);
                        if (sShapes == null || sShapes.Count == 0) continue;

                        int sheetNum = s + 1;
                        ZipArchiveEntry sRelsEntry = archive.CreateEntry(string.Format("xl/worksheets/_rels/sheet{0}.xml.rels", sheetNum));
                        using (StreamWriter sw = new StreamWriter(sRelsEntry.Open(), Encoding.UTF8))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                            sw.WriteLine(string.Format("  <Relationship Id=\"rIdDrawing{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing{0}.xml\"/>", sheetNum));
                            sw.WriteLine("</Relationships>");
                        }

                        Dictionary<DocShapeObject, int> imageIds = new Dictionary<DocShapeObject, int>();
                        for (int shIdx = 0; shIdx < sShapes.Count; shIdx++)
                        {
                            DocShapeObject sh = sShapes[shIdx];
                            byte[] imgBytes = null;

                            if (sh.ShapeType == "Image")
                            {
                                if (sh.CachedImage != null)
                                {
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        sh.CachedImage.Save(ms, ImageFormat.Png);
                                        imgBytes = ms.ToArray();
                                    }
                                }
                                else if (!string.IsNullOrEmpty(sh.ImagePath) && File.Exists(sh.ImagePath))
                                {
                                    try { imgBytes = File.ReadAllBytes(sh.ImagePath); } catch { }
                                }
                            }
                            else if (sh.ShapeType == "Chart" || sh.ShapeType == "SmartArt" || !string.IsNullOrEmpty(sh.SmartArtType) || sh.ChartData != null)
                            {
                                int wPx = Math.Max(50, (sh.BaseWidth + sh.WidthDelta) * 2);
                                int hPx = Math.Max(50, (sh.BaseHeight + sh.HeightDelta) * 2);
                                using (Bitmap bmp = new Bitmap(wPx, hPx))
                                {
                                    using (Graphics g = Graphics.FromImage(bmp))
                                    {
                                        g.Clear(Color.Transparent);
                                        ShapeRenderer.DrawShape(g, sh, new Rectangle(0, 0, wPx, hPx), null, 2.0f);
                                    }
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        bmp.Save(ms, ImageFormat.Png);
                                        imgBytes = ms.ToArray();
                                    }
                                }
                            }

                            if (imgBytes != null && imgBytes.Length > 0)
                            {
                                imageIds[sh] = nextImgId;
                                ZipArchiveEntry imgEntry = archive.CreateEntry(string.Format("xl/media/image{0}.png", nextImgId));
                                using (Stream isStream = imgEntry.Open())
                                {
                                    isStream.Write(imgBytes, 0, imgBytes.Length);
                                }
                                nextImgId++;
                            }
                        }

                        ZipArchiveEntry dRelsEntry = archive.CreateEntry(string.Format("xl/drawings/_rels/drawing{0}.xml.rels", sheetNum));
                        using (StreamWriter sw = new StreamWriter(dRelsEntry.Open(), Encoding.UTF8))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                            foreach (var kvp in imageIds)
                            {
                                sw.WriteLine(string.Format("  <Relationship Id=\"rIdImg{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image{0}.png\"/>", kvp.Value));
                            }
                            sw.WriteLine("</Relationships>");
                        }

                        ZipArchiveEntry drawEntry = archive.CreateEntry(string.Format("xl/drawings/drawing{0}.xml", sheetNum));
                        using (StreamWriter sw = new StreamWriter(drawEntry.Open(), Encoding.UTF8))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                            sw.WriteLine("<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

                            Dictionary<uint, int> sColWidths = (calcView.SheetStates != null && s < calcView.SheetStates.Count && calcView.SheetStates[s] != null && calcView.SheetStates[s].CustomColWidths != null)
                                ? calcView.SheetStates[s].CustomColWidths : calcView.CustomColWidths;
                            Dictionary<uint, int> sRowHeights = (calcView.SheetStates != null && s < calcView.SheetStates.Count && calcView.SheetStates[s] != null && calcView.SheetStates[s].CustomRowHeights != null)
                                ? calcView.SheetStates[s].CustomRowHeights : calcView.CustomRowHeights;
                            int defColW = calcView.DefaultColWidth > 0 ? calcView.DefaultColWidth : 80;
                            int defRowH = calcView.DefaultRowHeight > 0 ? calcView.DefaultRowHeight : 24;

                            for (int i = 0; i < sShapes.Count; i++)
                            {
                                DocShapeObject sh = sShapes[i];
                                int shapeId = i + 1;

                                uint fromCol = 1;
                                int accW = 0;
                                while (fromCol < 250 && accW + GetColWidthForSheet(fromCol, sColWidths, defColW) <= sh.PageX)
                                {
                                    accW += GetColWidthForSheet(fromCol, sColWidths, defColW);
                                    fromCol++;
                                }
                                long colOff = (long)(sh.PageX - accW) * 9525;

                                uint fromRow = 1;
                                int accH = 0;
                                while (fromRow < 1000 && accH + GetRowHeightForSheet(fromRow, sRowHeights, defRowH) <= sh.PageY)
                                {
                                    accH += GetRowHeightForSheet(fromRow, sRowHeights, defRowH);
                                    fromRow++;
                                }
                                long rowOff = (long)(sh.PageY - accH) * 9525;

                                int wPx = Math.Max(10, sh.BaseWidth + sh.WidthDelta);
                                int hPx = Math.Max(10, sh.BaseHeight + sh.HeightDelta);
                                int endX = sh.PageX + wPx;
                                int endY = sh.PageY + hPx;

                                uint toCol = 1;
                                int toAccW = 0;
                                while (toCol < 250 && toAccW + GetColWidthForSheet(toCol, sColWidths, defColW) <= endX)
                                {
                                    toAccW += GetColWidthForSheet(toCol, sColWidths, defColW);
                                    toCol++;
                                }
                                long toColOff = (long)(endX - toAccW) * 9525;

                                uint toRow = 1;
                                int toAccH = 0;
                                while (toRow < 1000 && toAccH + GetRowHeightForSheet(toRow, sRowHeights, defRowH) <= endY)
                                {
                                    toAccH += GetRowHeightForSheet(toRow, sRowHeights, defRowH);
                                    toRow++;
                                }
                                long toRowOff = (long)(endY - toAccH) * 9525;

                                long extCx = (long)wPx * 9525;
                                long extCy = (long)hPx * 9525;

                                if ((sh.ShapeType == "Image" || sh.ShapeType == "Chart" || sh.ShapeType == "SmartArt" || !string.IsNullOrEmpty(sh.SmartArtType) || sh.ChartData != null) && imageIds.ContainsKey(sh))
                                {
                                    int imgId = imageIds[sh];
                                    sw.WriteLine("  <xdr:twoCellAnchor editAs=\"oneCell\">");
                                    sw.WriteLine("    <xdr:from>");
                                    sw.WriteLine(string.Format("      <xdr:col>{0}</xdr:col><xdr:colOff>{1}</xdr:colOff>", fromCol - 1, colOff));
                                    sw.WriteLine(string.Format("      <xdr:row>{0}</xdr:row><xdr:rowOff>{1}</xdr:rowOff>", fromRow - 1, rowOff));
                                    sw.WriteLine("    </xdr:from>");
                                    sw.WriteLine("    <xdr:to>");
                                    sw.WriteLine(string.Format("      <xdr:col>{0}</xdr:col><xdr:colOff>{1}</xdr:colOff>", toCol - 1, toColOff));
                                    sw.WriteLine(string.Format("      <xdr:row>{0}</xdr:row><xdr:rowOff>{1}</xdr:rowOff>", toRow - 1, toRowOff));
                                    sw.WriteLine("    </xdr:to>");
                                    sw.WriteLine("    <xdr:pic>");
                                    sw.WriteLine("      <xdr:nvPicPr>");
                                    if (sh.ShapeType == "Chart" || sh.ChartData != null)
                                    {
                                        string cType = (sh.ChartData != null && !string.IsNullOrEmpty(sh.ChartData.ChartType)) ? sh.ChartData.ChartType : "Column_Clustered";
                                        sw.WriteLine(string.Format("        <xdr:cNvPr id=\"{0}\" name=\"Chart {0}\" descr=\"Chart|{1}\"/>", shapeId, cType));
                                    }
                                    else
                                    {
                                        sw.WriteLine(string.Format("        <xdr:cNvPr id=\"{0}\" name=\"Picture {0}\" descr=\"{1}\"/>", shapeId, EscapeXml(sh.AltText ?? "")));
                                    }
                                    sw.WriteLine("        <xdr:cNvPicPr><a:picLocks noChangeAspect=\"1\"/></xdr:cNvPicPr>");
                                    sw.WriteLine("      </xdr:nvPicPr>");
                                    sw.WriteLine("      <xdr:blipFill>");
                                    sw.WriteLine(string.Format("        <a:blip r:embed=\"rIdImg{0}\"/>", imgId));
                                    sw.WriteLine("        <a:stretch><a:fillRect/></a:stretch>");
                                    sw.WriteLine("      </xdr:blipFill>");
                                    sw.WriteLine("      <xdr:spPr>");
                                    sw.WriteLine("        <a:xfrm>");
                                    sw.WriteLine("          <a:off x=\"0\" y=\"0\"/>");
                                    sw.WriteLine(string.Format("          <a:ext cx=\"{0}\" cy=\"{1}\"/>", extCx, extCy));
                                    sw.WriteLine("        </a:xfrm>");
                                    sw.WriteLine("        <a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
                                    sw.WriteLine("      </xdr:spPr>");
                                    sw.WriteLine("    </xdr:pic>");
                                    sw.WriteLine("    <xdr:clientData/>");
                                    sw.WriteLine("  </xdr:twoCellAnchor>");
                                }
                                else
                                {
                                    string geom = "rect";
                                    if (sh.ShapeType == "RoundedRectangle") geom = "roundRect";
                                    else if (sh.ShapeType == "Oval" || sh.ShapeType == "Circle") geom = "ellipse";
                                    else if (sh.ShapeType == "Triangle") geom = "triangle";
                                    else if (sh.ShapeType == "RightArrow") geom = "rightArrow";
                                    else if (sh.ShapeType == "Star5") geom = "star5";

                                    string fillHex = sh.FillColor.R.ToString("X2") + sh.FillColor.G.ToString("X2") + sh.FillColor.B.ToString("X2");
                                    string lineHex = sh.OutlineColor.R.ToString("X2") + sh.OutlineColor.G.ToString("X2") + sh.OutlineColor.B.ToString("X2");

                                    sw.WriteLine("  <xdr:twoCellAnchor editAs=\"oneCell\">");
                                    sw.WriteLine("    <xdr:from>");
                                    sw.WriteLine(string.Format("      <xdr:col>{0}</xdr:col><xdr:colOff>{1}</xdr:colOff>", fromCol - 1, colOff));
                                    sw.WriteLine(string.Format("      <xdr:row>{0}</xdr:row><xdr:rowOff>{1}</xdr:rowOff>", fromRow - 1, rowOff));
                                    sw.WriteLine("    </xdr:from>");
                                    sw.WriteLine("    <xdr:to>");
                                    sw.WriteLine(string.Format("      <xdr:col>{0}</xdr:col><xdr:colOff>{1}</xdr:colOff>", toCol - 1, toColOff));
                                    sw.WriteLine(string.Format("      <xdr:row>{0}</xdr:row><xdr:rowOff>{1}</xdr:rowOff>", toRow - 1, toRowOff));
                                    sw.WriteLine("    </xdr:to>");
                                    sw.WriteLine("    <xdr:sp macro=\"\" textlink=\"\">");
                                    sw.WriteLine("      <xdr:nvSpPr>");
                                    sw.WriteLine(string.Format("        <xdr:cNvPr id=\"{0}\" name=\"{1} {0}\"/>", shapeId, EscapeXml(sh.ShapeType)));
                                    sw.WriteLine("        <xdr:cNvSpPr/>");
                                    sw.WriteLine("      </xdr:nvSpPr>");
                                    sw.WriteLine("      <xdr:spPr>");
                                    sw.WriteLine("        <a:xfrm>");
                                    sw.WriteLine("          <a:off x=\"0\" y=\"0\"/>");
                                    sw.WriteLine(string.Format("          <a:ext cx=\"{0}\" cy=\"{1}\"/>", extCx, extCy));
                                    sw.WriteLine("        </a:xfrm>");
                                    sw.WriteLine(string.Format("        <a:prstGeom prst=\"{0}\"><a:avLst/></a:prstGeom>", geom));
                                    sw.WriteLine("        <a:solidFill>");
                                    sw.WriteLine(string.Format("          <a:srgbClr val=\"{0}\"/>", fillHex));
                                    sw.WriteLine("        </a:solidFill>");
                                    sw.WriteLine("        <a:ln w=\"12700\">");
                                    sw.WriteLine(string.Format("          <a:solidFill><a:srgbClr val=\"{0}\"/></a:solidFill>", lineHex));
                                    sw.WriteLine("        </a:ln>");
                                    sw.WriteLine("      </xdr:spPr>");
                                    if (!string.IsNullOrEmpty(sh.TextContent))
                                    {
                                        sw.WriteLine("      <xdr:txBody>");
                                        sw.WriteLine("        <a:bodyPr vert=\"horz\" lIns=\"91440\" tIns=\"45720\" rIns=\"91440\" bIns=\"45720\"/>");
                                        sw.WriteLine("        <a:lstStyle/>");
                                        string[] lines = sh.TextContent.Replace("\r\n", "\n").Split('\n');
                                        foreach (string line in lines)
                                        {
                                            sw.WriteLine("        <a:p>");
                                            sw.WriteLine("          <a:r>");
                                            sw.WriteLine("            <a:rPr lang=\"vi-VN\" sz=\"1100\"/>");
                                            sw.WriteLine(string.Format("            <a:t>{0}</a:t>", EscapeXml(line)));
                                            sw.WriteLine("          </a:r>");
                                            sw.WriteLine("        </a:p>");
                                        }
                                        sw.WriteLine("      </xdr:txBody>");
                                    }
                                    sw.WriteLine("    </xdr:sp>");
                                    sw.WriteLine("    <xdr:clientData/>");
                                    sw.WriteLine("  </xdr:twoCellAnchor>");
                                }
                            }
                            sw.WriteLine("</xdr:wsDr>");
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<DocShapeObject> GetSheetShapes(CalcView calcView, int s)
        {
            if (calcView == null) return null;
            if (calcView.SheetStates != null && s < calcView.SheetStates.Count && calcView.SheetStates[s] != null && calcView.SheetStates[s].Shapes != null)
            {
                return calcView.SheetStates[s].Shapes;
            }
            if (s == calcView.ActiveSheetIndex && calcView.Shapes != null)
            {
                return calcView.Shapes;
            }
            return null;
        }

        private static ZipArchiveEntry GetEntryCaseInsensitive(ZipArchive archive, string entryPath)
        {
            if (archive == null || string.IsNullOrEmpty(entryPath)) return null;
            ZipArchiveEntry entry = archive.GetEntry(entryPath);
            if (entry != null) return entry;

            string normalized = entryPath.Replace('\\', '/').TrimStart('/');
            entry = archive.GetEntry(normalized);
            if (entry != null) return entry;

            for (int i = 0; i < archive.Entries.Count; i++)
            {
                string fn = archive.Entries[i].FullName.Replace('\\', '/').TrimStart('/');
                if (string.Equals(fn, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return archive.Entries[i];
                }
            }
            return null;
        }

        private static string NormalizeOpenXmlPath(string target, string baseFolder)
        {
            if (string.IsNullOrEmpty(target)) return "";
            string t = target.Replace('\\', '/');
            if (t.StartsWith("/")) return t.Substring(1);

            string curDir = (baseFolder ?? "").Replace('\\', '/').TrimEnd('/');
            while (t.StartsWith("../"))
            {
                t = t.Substring(3);
                int lastSlash = curDir.LastIndexOf('/');
                if (lastSlash >= 0)
                {
                    curDir = curDir.Substring(0, lastSlash);
                }
                else
                {
                    curDir = "";
                }
            }

            if (!string.IsNullOrEmpty(curDir))
            {
                return curDir + "/" + t;
            }
            return t;
        }

        private static int GetColWidthForSheet(uint col, Dictionary<uint, int> colWidths, int defColW)
        {
            if (colWidths != null && colWidths.ContainsKey(col)) return colWidths[col];
            return defColW > 0 ? defColW : 80;
        }

        private static int GetRowHeightForSheet(uint row, Dictionary<uint, int> rowHeights, int defRowH)
        {
            if (rowHeights != null && rowHeights.ContainsKey(row)) return rowHeights[row];
            return defRowH > 0 ? defRowH : 24;
        }

        private static Color ParseColorHex(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length < 6) return defaultColor;
            try
            {
                if (hex.Length > 6) hex = hex.Substring(hex.Length - 6);
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return Color.FromArgb(255, r, g, b);
            }
            catch
            {
                return defaultColor;
            }
        }

        private static List<DocShapeObject> LoadSheetDrawings(
            ZipArchive archive, 
            int sheetIdx, 
            string wsEntryFullName,
            Dictionary<uint, int> colWidths, 
            Dictionary<uint, int> rowHeights, 
            int defColW, 
            int defRowH)
        {
            List<DocShapeObject> shapes = new List<DocShapeObject>();
            if (archive == null) return shapes;

            try
            {
                string drawingPartPath = null;
                string wsDir = !string.IsNullOrEmpty(wsEntryFullName) ? Path.GetDirectoryName(wsEntryFullName).Replace('\\', '/') : "xl/worksheets";
                string wsFileName = !string.IsNullOrEmpty(wsEntryFullName) ? Path.GetFileName(wsEntryFullName) : string.Format("sheet{0}.xml", sheetIdx);
                string wsRelsPath = wsDir + "/_rels/" + wsFileName + ".rels";

                ZipArchiveEntry wsRelsEntry = GetEntryCaseInsensitive(archive, wsRelsPath);
                if (wsRelsEntry == null)
                {
                    wsRelsEntry = GetEntryCaseInsensitive(archive, string.Format("xl/worksheets/_rels/sheet{0}.xml.rels", sheetIdx));
                }

                if (wsRelsEntry != null)
                {
                    using (Stream s = wsRelsEntry.Open())
                    {
                        XmlDocument relsDoc = new XmlDocument();
                        relsDoc.Load(s);
                        XmlNodeList relNodes = relsDoc.GetElementsByTagName("Relationship");
                        foreach (XmlNode rel in relNodes)
                        {
                            if (rel.Attributes == null) continue;
                            XmlAttribute typeAttr = rel.Attributes["Type"];
                            XmlAttribute targetAttr = rel.Attributes["Target"];
                            if (typeAttr != null && targetAttr != null && 
                                typeAttr.Value.EndsWith("/drawing", StringComparison.OrdinalIgnoreCase))
                            {
                                drawingPartPath = NormalizeOpenXmlPath(targetAttr.Value, wsDir + "/");
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(drawingPartPath))
                {
                    string fallbackPath = string.Format("xl/drawings/drawing{0}.xml", sheetIdx);
                    if (GetEntryCaseInsensitive(archive, fallbackPath) != null)
                    {
                        drawingPartPath = fallbackPath;
                    }
                }

                if (string.IsNullOrEmpty(drawingPartPath)) return shapes;

                ZipArchiveEntry drawEntry = GetEntryCaseInsensitive(archive, drawingPartPath);
                if (drawEntry == null) return shapes;

                string drawDir = Path.GetDirectoryName(drawingPartPath).Replace('\\', '/');
                string drawFileName = Path.GetFileName(drawingPartPath);
                string drawRelsPath = drawDir + "/_rels/" + drawFileName + ".rels";
                ZipArchiveEntry drawRelsEntry = GetEntryCaseInsensitive(archive, drawRelsPath);
                Dictionary<string, string> drawingRels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (drawRelsEntry != null)
                {
                    using (Stream s = drawRelsEntry.Open())
                    {
                        XmlDocument dRelsDoc = new XmlDocument();
                        dRelsDoc.Load(s);
                        XmlNodeList relNodes = dRelsDoc.GetElementsByTagName("Relationship");
                        foreach (XmlNode rel in relNodes)
                        {
                            if (rel.Attributes == null) continue;
                            XmlAttribute idAttr = rel.Attributes["Id"];
                            XmlAttribute targetAttr = rel.Attributes["Target"];
                            if (idAttr != null && targetAttr != null)
                            {
                                string normTarget = NormalizeOpenXmlPath(targetAttr.Value, drawDir + "/");
                                drawingRels[idAttr.Value] = normTarget;
                            }
                        }
                    }
                }

                using (Stream s = drawEntry.Open())
                {
                    XmlDocument drawDoc = new XmlDocument();
                    drawDoc.Load(s);

                    foreach (XmlNode rootNode in drawDoc.ChildNodes)
                    {
                        if (rootNode.LocalName != "wsDr") continue;

                        foreach (XmlNode anchorNode in rootNode.ChildNodes)
                        {
                            DocShapeObject sh = ParseAnchorNode(anchorNode, archive, drawingRels, colWidths, rowHeights, defColW, defRowH);
                            if (sh != null)
                            {
                                shapes.Add(sh);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return shapes;
        }

        private static DocShapeObject ParseAnchorNode(
            XmlNode anchorNode, 
            ZipArchive archive, 
            Dictionary<string, string> drawingRels, 
            Dictionary<uint, int> colWidths, 
            Dictionary<uint, int> rowHeights, 
            int defColW, 
            int defRowH)
        {
            if (anchorNode == null) return null;
            string localName = anchorNode.LocalName;
            if (localName != "twoCellAnchor" && localName != "oneCellAnchor" && localName != "absoluteAnchor")
            {
                return null;
            }

            int pageX = 0, pageY = 0, baseW = 100, baseH = 50;

            if (localName == "twoCellAnchor")
            {
                XmlNode fromNode = null, toNode = null;
                foreach (XmlNode ch in anchorNode.ChildNodes)
                {
                    if (ch.LocalName == "from") fromNode = ch;
                    else if (ch.LocalName == "to") toNode = ch;
                }

                if (fromNode != null && toNode != null)
                {
                    uint fromCol = 0, fromRow = 0, toCol = 0, toRow = 0;
                    long fromColOff = 0, fromRowOff = 0, toColOff = 0, toRowOff = 0;

                    foreach (XmlNode fch in fromNode.ChildNodes)
                    {
                        if (fch.LocalName == "col") uint.TryParse(fch.InnerText, out fromCol);
                        else if (fch.LocalName == "colOff") long.TryParse(fch.InnerText, out fromColOff);
                        else if (fch.LocalName == "row") uint.TryParse(fch.InnerText, out fromRow);
                        else if (fch.LocalName == "rowOff") long.TryParse(fch.InnerText, out fromRowOff);
                    }

                    foreach (XmlNode tch in toNode.ChildNodes)
                    {
                        if (tch.LocalName == "col") uint.TryParse(tch.InnerText, out toCol);
                        else if (tch.LocalName == "colOff") long.TryParse(tch.InnerText, out toColOff);
                        else if (tch.LocalName == "row") uint.TryParse(tch.InnerText, out toRow);
                        else if (tch.LocalName == "rowOff") long.TryParse(tch.InnerText, out toRowOff);
                    }

                    uint fromCol1 = fromCol + 1;
                    uint fromRow1 = fromRow + 1;
                    uint toCol1 = toCol + 1;
                    uint toRow1 = toRow + 1;

                    int startX = 0;
                    for (uint c = 1; c < fromCol1; c++) startX += GetColWidthForSheet(c, colWidths, defColW);
                    pageX = startX + (int)Math.Round((double)fromColOff / 9525.0);

                    int startY = 0;
                    for (uint r = 1; r < fromRow1; r++) startY += GetRowHeightForSheet(r, rowHeights, defRowH);
                    pageY = startY + (int)Math.Round((double)fromRowOff / 9525.0);

                    int endX = 0;
                    for (uint c = 1; c < toCol1; c++) endX += GetColWidthForSheet(c, colWidths, defColW);
                    endX += (int)Math.Round((double)toColOff / 9525.0);

                    int endY = 0;
                    for (uint r = 1; r < toRow1; r++) endY += GetRowHeightForSheet(r, rowHeights, defRowH);
                    endY += (int)Math.Round((double)toRowOff / 9525.0);

                    baseW = Math.Max(10, endX - pageX);
                    baseH = Math.Max(10, endY - pageY);
                }
            }
            else if (localName == "oneCellAnchor")
            {
                XmlNode fromNode = null, extNode = null;
                foreach (XmlNode ch in anchorNode.ChildNodes)
                {
                    if (ch.LocalName == "from") fromNode = ch;
                    else if (ch.LocalName == "ext") extNode = ch;
                }

                if (fromNode != null)
                {
                    uint fromCol = 0, fromRow = 0;
                    long fromColOff = 0, fromRowOff = 0;

                    foreach (XmlNode fch in fromNode.ChildNodes)
                    {
                        if (fch.LocalName == "col") uint.TryParse(fch.InnerText, out fromCol);
                        else if (fch.LocalName == "colOff") long.TryParse(fch.InnerText, out fromColOff);
                        else if (fch.LocalName == "row") uint.TryParse(fch.InnerText, out fromRow);
                        else if (fch.LocalName == "rowOff") long.TryParse(fch.InnerText, out fromRowOff);
                    }

                    uint fromCol1 = fromCol + 1;
                    uint fromRow1 = fromRow + 1;

                    int startX = 0;
                    for (uint c = 1; c < fromCol1; c++) startX += GetColWidthForSheet(c, colWidths, defColW);
                    pageX = startX + (int)Math.Round((double)fromColOff / 9525.0);

                    int startY = 0;
                    for (uint r = 1; r < fromRow1; r++) startY += GetRowHeightForSheet(r, rowHeights, defRowH);
                    pageY = startY + (int)Math.Round((double)fromRowOff / 9525.0);
                }

                if (extNode != null && extNode.Attributes != null)
                {
                    long cx = 0, cy = 0;
                    if (extNode.Attributes["cx"] != null) long.TryParse(extNode.Attributes["cx"].Value, out cx);
                    if (extNode.Attributes["cy"] != null) long.TryParse(extNode.Attributes["cy"].Value, out cy);
                    baseW = Math.Max(10, (int)Math.Round((double)cx / 9525.0));
                    baseH = Math.Max(10, (int)Math.Round((double)cy / 9525.0));
                }
            }
            else if (localName == "absoluteAnchor")
            {
                XmlNode posNode = null, extNode = null;
                foreach (XmlNode ch in anchorNode.ChildNodes)
                {
                    if (ch.LocalName == "pos") posNode = ch;
                    else if (ch.LocalName == "ext") extNode = ch;
                }

                if (posNode != null && posNode.Attributes != null)
                {
                    long x = 0, y = 0;
                    if (posNode.Attributes["x"] != null) long.TryParse(posNode.Attributes["x"].Value, out x);
                    if (posNode.Attributes["y"] != null) long.TryParse(posNode.Attributes["y"].Value, out y);
                    pageX = (int)Math.Round((double)x / 9525.0);
                    pageY = (int)Math.Round((double)y / 9525.0);
                }

                if (extNode != null && extNode.Attributes != null)
                {
                    long cx = 0, cy = 0;
                    if (extNode.Attributes["cx"] != null) long.TryParse(extNode.Attributes["cx"].Value, out cx);
                    if (extNode.Attributes["cy"] != null) long.TryParse(extNode.Attributes["cy"].Value, out cy);
                    baseW = Math.Max(10, (int)Math.Round((double)cx / 9525.0));
                    baseH = Math.Max(10, (int)Math.Round((double)cy / 9525.0));
                }
            }

            foreach (XmlNode ch in anchorNode.ChildNodes)
            {
                if (ch.LocalName == "pic")
                {
                    return ParsePictureElement(ch, archive, drawingRels, pageX, pageY, baseW, baseH);
                }
                else if (ch.LocalName == "sp")
                {
                    return ParseShapeElement(ch, pageX, pageY, baseW, baseH);
                }
                else if (ch.LocalName == "graphicFrame")
                {
                    return ParseGraphicFrameElement(ch, archive, drawingRels, pageX, pageY, baseW, baseH);
                }
            }

            return null;
        }

        private static DocShapeObject ParsePictureElement(
            XmlNode picNode, 
            ZipArchive archive, 
            Dictionary<string, string> drawingRels, 
            int pageX, 
            int pageY, 
            int baseW, 
            int baseH)
        {
            DocShapeObject sh = new DocShapeObject("Image", pageX, pageY, baseW, baseH);
            sh.FillColor = Color.Transparent;
            sh.OutlineColor = Color.Transparent;
            sh.OutlineWidth = 0f;

            string picName = "";
            string picDescr = "";

            XmlNode nvPicPr = picNode.SelectSingleNode(".//*[local-name()='nvPicPr']");
            if (nvPicPr != null)
            {
                XmlNode cNvPr = nvPicPr.SelectSingleNode(".//*[local-name()='cNvPr']");
                if (cNvPr != null && cNvPr.Attributes != null)
                {
                    if (cNvPr.Attributes["id"] != null) sh.Id = cNvPr.Attributes["id"].Value;
                    if (cNvPr.Attributes["name"] != null) picName = cNvPr.Attributes["name"].Value;
                    if (cNvPr.Attributes["descr"] != null) picDescr = cNvPr.Attributes["descr"].Value;
                    sh.CustomName = picName;
                    sh.AltText = picDescr;
                }
            }

            XmlNode blipNode = picNode.SelectSingleNode(".//*[local-name()='blip']");
            string embedId = null;
            if (blipNode != null && blipNode.Attributes != null)
            {
                foreach (XmlAttribute attr in blipNode.Attributes)
                {
                    if (attr.LocalName == "embed")
                    {
                        embedId = attr.Value;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(embedId) && drawingRels.ContainsKey(embedId))
            {
                string mediaPath = drawingRels[embedId];
                ZipArchiveEntry mediaEntry = GetEntryCaseInsensitive(archive, mediaPath);
                if (mediaEntry != null)
                {
                    try
                    {
                        using (Stream msStream = mediaEntry.Open())
                        using (MemoryStream ms = new MemoryStream())
                        {
                            msStream.CopyTo(ms);
                            ms.Position = 0;
                            using (Image tempImg = Image.FromStream(ms))
                            {
                                sh.CachedImage = new Bitmap(tempImg);
                            }
                        }
                    }
                    catch { }
                }
            }

            XmlNode xfrmNode = picNode.SelectSingleNode(".//*[local-name()='xfrm']");
            if (xfrmNode != null && xfrmNode.Attributes != null && xfrmNode.Attributes["rot"] != null)
            {
                long rot;
                if (long.TryParse(xfrmNode.Attributes["rot"].Value, out rot))
                {
                    sh.RotationAngle = (float)(rot / 60000.0);
                }
            }

            if (picName.StartsWith("Chart", StringComparison.OrdinalIgnoreCase) || 
                picDescr.StartsWith("Chart", StringComparison.OrdinalIgnoreCase))
            {
                sh.ShapeType = "Chart";
                string cType = !string.IsNullOrEmpty(picDescr) ? picDescr.Replace("Chart|", "") : "Column_Clustered";
                sh.ChartData = DocChartData.CreateDefault(cType);
                sh.TextContent = sh.ChartData.ChartType;
            }

            return sh;
        }

        private static DocShapeObject ParseShapeElement(XmlNode spNode, int pageX, int pageY, int baseW, int baseH)
        {
            DocShapeObject sh = new DocShapeObject("Rectangle", pageX, pageY, baseW, baseH);

            XmlNode nvSpPr = spNode.SelectSingleNode(".//*[local-name()='nvSpPr']");
            if (nvSpPr != null)
            {
                XmlNode cNvPr = nvSpPr.SelectSingleNode(".//*[local-name()='cNvPr']");
                if (cNvPr != null && cNvPr.Attributes != null)
                {
                    if (cNvPr.Attributes["id"] != null) sh.Id = cNvPr.Attributes["id"].Value;
                    if (cNvPr.Attributes["name"] != null) sh.CustomName = cNvPr.Attributes["name"].Value;
                }
            }

            XmlNode spPr = spNode.SelectSingleNode(".//*[local-name()='spPr']");
            if (spPr != null)
            {
                XmlNode prstGeom = spPr.SelectSingleNode(".//*[local-name()='prstGeom']");
                if (prstGeom != null && prstGeom.Attributes != null && prstGeom.Attributes["prst"] != null)
                {
                    string prst = prstGeom.Attributes["prst"].Value;
                    switch (prst)
                    {
                        case "rect": sh.ShapeType = "Rectangle"; break;
                        case "roundRect": sh.ShapeType = "RoundedRectangle"; break;
                        case "ellipse": sh.ShapeType = "Oval"; break;
                        case "triangle": sh.ShapeType = "Triangle"; break;
                        case "rtTriangle": sh.ShapeType = "RightTriangle"; break;
                        case "diamond": sh.ShapeType = "Diamond"; break;
                        case "parallelogram": sh.ShapeType = "Parallelogram"; break;
                        case "trapezoid": sh.ShapeType = "Trapezoid"; break;
                        case "hexagon": sh.ShapeType = "Hexagon"; break;
                        case "octagon": sh.ShapeType = "Octagon"; break;
                        case "star5": sh.ShapeType = "Star5"; break;
                        case "heart": sh.ShapeType = "Heart"; break;
                        case "rightArrow": sh.ShapeType = "RightArrow"; break;
                        case "leftArrow": sh.ShapeType = "LeftArrow"; break;
                        case "upArrow": sh.ShapeType = "UpArrow"; break;
                        case "downArrow": sh.ShapeType = "DownArrow"; break;
                        case "can": case "cylinder": sh.ShapeType = "Cylinder"; break;
                        case "cube": sh.ShapeType = "Cube"; break;
                        case "donut": sh.ShapeType = "Donut"; break;
                        case "line": sh.ShapeType = "Line"; break;
                        default: sh.ShapeType = prst; break;
                    }
                }

                XmlNode solidFill = spPr.SelectSingleNode("./*[local-name()='solidFill']");
                if (solidFill != null)
                {
                    XmlNode srgbClr = solidFill.SelectSingleNode("./*[local-name()='srgbClr']");
                    if (srgbClr != null && srgbClr.Attributes != null && srgbClr.Attributes["val"] != null)
                    {
                        sh.FillColor = ParseColorHex(srgbClr.Attributes["val"].Value, Color.FromArgb(43, 87, 154));
                    }
                }
                else if (spPr.SelectSingleNode("./*[local-name()='noFill']") != null)
                {
                    sh.FillColor = Color.Transparent;
                }

                XmlNode lnNode = spPr.SelectSingleNode("./*[local-name()='ln']");
                if (lnNode != null)
                {
                    if (lnNode.Attributes != null && lnNode.Attributes["w"] != null)
                    {
                        long lw;
                        if (long.TryParse(lnNode.Attributes["w"].Value, out lw))
                        {
                            sh.OutlineWidth = Math.Max(1f, (float)(lw / 9525.0));
                        }
                    }

                    XmlNode lnFill = lnNode.SelectSingleNode("./*[local-name()='solidFill']/*[local-name()='srgbClr']");
                    if (lnFill != null && lnFill.Attributes != null && lnFill.Attributes["val"] != null)
                    {
                        sh.OutlineColor = ParseColorHex(lnFill.Attributes["val"].Value, Color.FromArgb(30, 60, 110));
                    }

                    if (lnNode.SelectSingleNode("./*[local-name()='noFill']") != null)
                    {
                        sh.OutlineColor = Color.Transparent;
                        sh.OutlineWidth = 0f;
                    }
                }

                XmlNode xfrm = spPr.SelectSingleNode("./*[local-name()='xfrm']");
                if (xfrm != null && xfrm.Attributes != null && xfrm.Attributes["rot"] != null)
                {
                    long rot;
                    if (long.TryParse(xfrm.Attributes["rot"].Value, out rot))
                    {
                        sh.RotationAngle = (float)(rot / 60000.0);
                    }
                }
            }

            XmlNode txBody = spNode.SelectSingleNode(".//*[local-name()='txBody']");
            if (txBody != null)
            {
                StringBuilder sb = new StringBuilder();
                XmlNodeList pNodes = txBody.SelectNodes(".//*[local-name()='p']");
                if (pNodes != null)
                {
                    for (int pi = 0; pi < pNodes.Count; pi++)
                    {
                        XmlNode pNode = pNodes[pi];
                        XmlNodeList tNodes = pNode.SelectNodes(".//*[local-name()='t']");
                        if (tNodes != null)
                        {
                            for (int ti = 0; ti < tNodes.Count; ti++)
                            {
                                sb.Append(tNodes[ti].InnerText);
                            }
                        }
                        if (pi < pNodes.Count - 1) sb.Append("\n");

                        XmlNode pPr = pNode.SelectSingleNode("./*[local-name()='pPr']");
                        if (pPr != null && pPr.Attributes != null && pPr.Attributes["algn"] != null)
                        {
                            string algn = pPr.Attributes["algn"].Value;
                            if (algn == "ctr") sh.TextAlignment = StringAlignment.Center;
                            else if (algn == "r") sh.TextAlignment = StringAlignment.Far;
                            else if (algn == "l") sh.TextAlignment = StringAlignment.Near;
                        }
                    }
                }
                sh.TextContent = sb.ToString();

                XmlNode rPr = txBody.SelectSingleNode(".//*[local-name()='rPr']");
                if (rPr != null && rPr.Attributes != null)
                {
                    if (rPr.Attributes["sz"] != null)
                    {
                        float sz;
                        if (float.TryParse(rPr.Attributes["sz"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out sz))
                        {
                            sh.FontSize = sz / 100f;
                        }
                    }
                    if (rPr.Attributes["b"] != null) sh.Bold = (rPr.Attributes["b"].Value == "1" || rPr.Attributes["b"].Value.ToLower() == "true");
                    if (rPr.Attributes["i"] != null) sh.Italic = (rPr.Attributes["i"].Value == "1" || rPr.Attributes["i"].Value.ToLower() == "true");

                    XmlNode textClr = rPr.SelectSingleNode(".//*[local-name()='srgbClr']");
                    if (textClr != null && textClr.Attributes != null && textClr.Attributes["val"] != null)
                    {
                        sh.TextColor = ParseColorHex(textClr.Attributes["val"].Value, Color.White);
                    }
                }
            }

            return sh;
        }

        private static DocShapeObject ParseGraphicFrameElement(
            XmlNode gfNode, 
            ZipArchive archive, 
            Dictionary<string, string> drawingRels, 
            int pageX, 
            int pageY, 
            int baseW, 
            int baseH)
        {
            DocShapeObject sh = new DocShapeObject("Chart", pageX, pageY, baseW, baseH);
            sh.FillColor = Color.White;
            sh.OutlineColor = Color.FromArgb(209, 213, 219);
            sh.OutlineWidth = 1.0f;

            XmlNode nvGfPr = gfNode.SelectSingleNode(".//*[local-name()='nvGraphicFramePr']");
            if (nvGfPr != null)
            {
                XmlNode cNvPr = nvGfPr.SelectSingleNode(".//*[local-name()='cNvPr']");
                if (cNvPr != null && cNvPr.Attributes != null)
                {
                    if (cNvPr.Attributes["id"] != null) sh.Id = cNvPr.Attributes["id"].Value;
                    if (cNvPr.Attributes["name"] != null) sh.CustomName = cNvPr.Attributes["name"].Value;
                }
            }

            XmlNode graphicData = gfNode.SelectSingleNode(".//*[local-name()='graphicData']");
            if (graphicData != null && graphicData.Attributes != null && graphicData.Attributes["uri"] != null)
            {
                string uri = graphicData.Attributes["uri"].Value;

                if (uri.IndexOf("chart", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    XmlNode chartRefNode = graphicData.SelectSingleNode(".//*[local-name()='chart']");
                    string chartRelId = null;
                    if (chartRefNode != null && chartRefNode.Attributes != null)
                    {
                        foreach (XmlAttribute attr in chartRefNode.Attributes)
                        {
                            if (attr.LocalName == "id")
                            {
                                chartRelId = attr.Value;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(chartRelId) && drawingRels.ContainsKey(chartRelId))
                    {
                        string chartPath = drawingRels[chartRelId];
                        ZipArchiveEntry chartEntry = GetEntryCaseInsensitive(archive, chartPath);
                        if (chartEntry != null)
                        {
                            using (Stream cs = chartEntry.Open())
                            {
                                sh.ChartData = ParseChartXml(cs);
                            }
                        }
                    }

                    if (sh.ChartData == null)
                    {
                        sh.ChartData = DocChartData.CreateDefault("Column_Clustered");
                    }
                    sh.TextContent = sh.ChartData.ChartType;
                    return sh;
                }
                else if (uri.IndexOf("diagram", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sh.ShapeType = "SmartArt";
                    sh.SmartArtType = "Cycle_Radial";
                    sh.SmartArtTitle = "Sơ đồ";
                    sh.TextContent = "Mục 1|Mục 2|Mục 3";
                    return sh;
                }
            }

            sh.ChartData = DocChartData.CreateDefault("Column_Clustered");
            sh.TextContent = sh.ChartData.ChartType;
            return sh;
        }

        private static DocChartData ParseChartXml(Stream stream)
        {
            DocChartData data = new DocChartData();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);

                XmlNode titleNode = doc.SelectSingleNode(".//*[local-name()='title']");
                if (titleNode != null)
                {
                    XmlNode tNode = titleNode.SelectSingleNode(".//*[local-name()='t']");
                    if (tNode != null && !string.IsNullOrEmpty(tNode.InnerText))
                    {
                        data.Title = tNode.InnerText;
                    }
                    else
                    {
                        XmlNode vNode = titleNode.SelectSingleNode(".//*[local-name()='v']");
                        if (vNode != null && !string.IsNullOrEmpty(vNode.InnerText))
                        {
                            data.Title = vNode.InnerText;
                        }
                    }
                    data.ShowTitle = true;
                }

                XmlNode plotArea = doc.SelectSingleNode(".//*[local-name()='plotArea']");
                if (plotArea != null)
                {
                    XmlNode chartTypeNode = null;
                    foreach (XmlNode ch in plotArea.ChildNodes)
                    {
                        if (ch.LocalName.EndsWith("Chart", StringComparison.OrdinalIgnoreCase))
                        {
                            chartTypeNode = ch;
                            break;
                        }
                    }

                    if (chartTypeNode != null)
                    {
                        string localName = chartTypeNode.LocalName;
                        if (localName == "barChart")
                        {
                            string barDir = "col";
                            string grouping = "clustered";
                            XmlNode bdNode = chartTypeNode.SelectSingleNode("./*[local-name()='barDir']");
                            if (bdNode != null && bdNode.Attributes != null && bdNode.Attributes["val"] != null)
                                barDir = bdNode.Attributes["val"].Value;
                            XmlNode grpNode = chartTypeNode.SelectSingleNode("./*[local-name()='grouping']");
                            if (grpNode != null && grpNode.Attributes != null && grpNode.Attributes["val"] != null)
                                grouping = grpNode.Attributes["val"].Value;

                            if (barDir == "bar")
                                data.ChartType = (grouping == "stacked") ? "Bar_Stacked" : "Bar_Clustered";
                            else
                                data.ChartType = (grouping == "stacked") ? "Column_Stacked" : "Column_Clustered";
                        }
                        else if (localName == "lineChart") data.ChartType = "Line";
                        else if (localName == "pieChart") data.ChartType = "Pie";
                        else if (localName == "doughnutChart") data.ChartType = "Doughnut";
                        else if (localName == "areaChart") data.ChartType = "Area";
                        else if (localName == "radarChart") data.ChartType = "Radar";
                        else if (localName == "scatterChart") data.ChartType = "Scatter";
                        else data.ChartType = "Column_Clustered";

                        XmlNodeList serNodes = chartTypeNode.SelectNodes("./*[local-name()='ser']");
                        List<string> seriesNames = new List<string>();
                        List<string> categories = new List<string>();
                        Dictionary<int, List<double>> seriesValues = new Dictionary<int, List<double>>();

                        if (serNodes != null)
                        {
                            for (int s = 0; s < serNodes.Count; s++)
                            {
                                XmlNode serNode = serNodes[s];

                                string sName = "Chuỗi " + (s + 1);
                                XmlNode txNode = serNode.SelectSingleNode("./*[local-name()='tx']");
                                if (txNode != null)
                                {
                                    XmlNode vNode = txNode.SelectSingleNode(".//*[local-name()='v']");
                                    if (vNode != null && !string.IsNullOrEmpty(vNode.InnerText))
                                        sName = vNode.InnerText;
                                }
                                seriesNames.Add(sName);

                                if (s == 0)
                                {
                                    XmlNode catNode = serNode.SelectSingleNode("./*[local-name()='cat']");
                                    if (catNode != null)
                                    {
                                        XmlNodeList ptNodes = catNode.SelectNodes(".//*[local-name()='pt']");
                                        if (ptNodes != null)
                                        {
                                            foreach (XmlNode pt in ptNodes)
                                            {
                                                XmlNode vNode = pt.SelectSingleNode("./*[local-name()='v']");
                                                if (vNode != null) categories.Add(vNode.InnerText);
                                            }
                                        }
                                    }
                                }

                                List<double> valList = new List<double>();
                                XmlNode valNode = serNode.SelectSingleNode("./*[local-name()='val']");
                                if (valNode != null)
                                {
                                    XmlNodeList ptNodes = valNode.SelectNodes(".//*[local-name()='pt']");
                                    if (ptNodes != null)
                                    {
                                        foreach (XmlNode pt in ptNodes)
                                        {
                                            XmlNode vNode = pt.SelectSingleNode("./*[local-name()='v']");
                                            double dVal = 0.0;
                                            if (vNode != null)
                                            {
                                                double.TryParse(vNode.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out dVal);
                                            }
                                            valList.Add(dVal);
                                        }
                                    }
                                }
                                seriesValues[s] = valList;
                            }
                        }

                        int maxCatCount = 0;
                        foreach (var kvp in seriesValues)
                        {
                            if (kvp.Value.Count > maxCatCount) maxCatCount = kvp.Value.Count;
                        }
                        while (categories.Count < maxCatCount)
                        {
                            categories.Add("Danh mục " + (categories.Count + 1));
                        }

                        data.Categories = categories;
                        data.SeriesNames = seriesNames;

                        data.Values = new List<List<double>>();
                        for (int c = 0; c < categories.Count; c++)
                        {
                            List<double> row = new List<double>();
                            for (int s = 0; s < seriesNames.Count; s++)
                            {
                                double v = (seriesValues.ContainsKey(s) && c < seriesValues[s].Count) ? seriesValues[s][c] : 0.0;
                                row.Add(v);
                            }
                            data.Values.Add(row);
                        }

                        for (int s = 0; s < seriesNames.Count; s++) data.SeriesVisible.Add(true);
                        for (int c = 0; c < categories.Count; c++) data.CategoriesVisible.Add(true);
                    }
                }
            }
            catch
            {
                return DocChartData.CreateDefault("Column_Clustered");
            }

            return data;
        }

        private static string ToArgbHex(Color c)
        {
            return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
        }

        private static int ParseArgbHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return 0;
            if (hex.Length == 8)
            {
                int a = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int r = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                return Color.FromArgb(a, r, g, b).ToArgb();
            }
            else if (hex.Length == 6)
            {
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                return Color.FromArgb(255, r, g, b).ToArgb();
            }
            return 0;
        }

        private static string[] SplitCsvLine(string line, char delimiter)
        {
            List<string> parts = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    parts.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    sb.Append(c);
                }
            }
            parts.Add(sb.ToString());
            return parts.ToArray();
        }

        private static string EscapeXml(string input)
        {
            return (input ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("\r\n", "&#10;").Replace("\n", "&#10;");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using ZenQOffice.Core;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    /// <summary>
    /// OpenDocument Spreadsheet (.ods) reader and writer compatible with LibreOffice Calc and Microsoft Excel.
    /// </summary>
    public static class OdsHandler
    {
        public static bool LoadOds(CalcView calcView, string filePath)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(filePath))
                {
                    ZipArchiveEntry contentEntry = archive.GetEntry("content.xml");
                    if (contentEntry == null) return false;

                    using (Stream stream = contentEntry.Open())
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);

                        XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                        nsm.AddNamespace("office", "urn:oasis:names:tc:opendocument:xmlns:office:1.0");
                        nsm.AddNamespace("table", "urn:oasis:names:tc:opendocument:xmlns:table:1.0");
                        nsm.AddNamespace("text", "urn:oasis:names:tc:opendocument:xmlns:text:1.0");

                        XmlNodeList tables = doc.SelectNodes("//table:table", nsm);
                        if (tables != null && tables.Count > 0)
                        {
                            calcView.Sheets.Clear();
                            calcView.SheetNames.Clear();
                            if (calcView.SheetStates == null) calcView.SheetStates = new List<CalcSheetState>();
                            else calcView.SheetStates.Clear();

                            for (int t = 0; t < tables.Count; t++)
                            {
                                XmlNode tblNode = tables[t];
                                string sheetName = "Sheet" + (t + 1);
                                if (tblNode.Attributes != null && tblNode.Attributes["table:name"] != null)
                                {
                                    sheetName = tblNode.Attributes["table:name"].Value;
                                }

                                SparseMatrix sheet = new SparseMatrix();
                                XmlNodeList rows = tblNode.SelectNodes("table:table-row", nsm);

                                uint rowIdx = 1;
                                if (rows != null)
                                {
                                    foreach (XmlNode rowNode in rows)
                                    {
                                        uint repRow = 1;
                                        if (rowNode.Attributes != null && rowNode.Attributes["table:number-rows-repeated"] != null)
                                        {
                                            uint.TryParse(rowNode.Attributes["table:number-rows-repeated"].Value, out repRow);
                                            if (repRow > 100) repRow = 1; // Safeguard against trailing empty rows
                                        }

                                        for (uint rr = 0; rr < repRow; rr++)
                                        {
                                            XmlNodeList cells = rowNode.SelectNodes("table:table-cell", nsm);
                                            uint colIdx = 1;
                                            if (cells != null)
                                            {
                                                foreach (XmlNode cellNode in cells)
                                                {
                                                    uint repCol = 1;
                                                    if (cellNode.Attributes != null && cellNode.Attributes["table:number-columns-repeated"] != null)
                                                    {
                                                        uint.TryParse(cellNode.Attributes["table:number-columns-repeated"].Value, out repCol);
                                                        if (repCol > 100) repCol = 1; // Safeguard against trailing empty cells
                                                    }

                                                    string formula = null;
                                                    if (cellNode.Attributes != null && cellNode.Attributes["table:formula"] != null)
                                                    {
                                                        formula = cellNode.Attributes["table:formula"].Value;
                                                        // Convert ODF formula 'of:=SUM([.A1:.A5])' to standard Excel '=SUM(A1:A5)'
                                                        if (!string.IsNullOrEmpty(formula))
                                                        {
                                                            formula = formula.Replace("of:=", "=").Replace("[.", "").Replace(":.A", ":A").Replace("]", "");
                                                        }
                                                    }

                                                    string textVal = "";
                                                    XmlNode textNode = cellNode.SelectSingleNode("text:p", nsm);
                                                    if (textNode != null) textVal = textNode.InnerText;

                                                    string valType = cellNode.Attributes != null && cellNode.Attributes["office:value-type"] != null ? cellNode.Attributes["office:value-type"].Value : "";

                                                    for (uint rc = 0; rc < repCol; rc++)
                                                    {
                                                        if (!string.IsNullOrEmpty(formula))
                                                        {
                                                            sheet.SetCellFormula(rowIdx, colIdx, formula);
                                                        }
                                                        else if (valType == "float" || valType == "currency" || valType == "percentage")
                                                        {
                                                            string numValStr = cellNode.Attributes != null && cellNode.Attributes["office:value"] != null ? cellNode.Attributes["office:value"].Value : textVal;
                                                            double dVal;
                                                            if (double.TryParse(numValStr, NumberStyles.Any, CultureInfo.InvariantCulture, out dVal))
                                                            {
                                                                sheet.SetCellNumber(rowIdx, colIdx, dVal);
                                                            }
                                                            else
                                                            {
                                                                sheet.SetCellString(rowIdx, colIdx, textVal);
                                                            }
                                                        }
                                                        else if (!string.IsNullOrEmpty(textVal))
                                                        {
                                                            double num;
                                                            if (double.TryParse(textVal, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                                                            {
                                                                sheet.SetCellNumber(rowIdx, colIdx, num);
                                                            }
                                                            else
                                                            {
                                                                sheet.SetCellString(rowIdx, colIdx, textVal);
                                                            }
                                                        }
                                                        colIdx++;
                                                    }
                                                }
                                            }
                                            rowIdx++;
                                        }
                                    }
                                }

                                calcView.Sheets.Add(sheet);
                                calcView.SheetNames.Add(sheetName);
                                FormulaEngine.EvaluateSheet(sheet);

                                var st = new CalcSheetState();
                                st.CustomColWidths = new Dictionary<uint, int>();
                                st.CustomRowHeights = new Dictionary<uint, int>();
                                st.Shapes = new List<DocShapeObject>();
                                calcView.SheetStates.Add(st);
                            }

                            calcView.ActiveSheetIndex = 0;
                            calcView.Invalidate();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool SaveOds(CalcView calcView, string filePath)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath)) return false;

            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);

                using (ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Create))
                {
                    // 1. mimetype (must be uncompressed first entry in standard ODF)
                    ZipArchiveEntry mimeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                    using (StreamWriter sw = new StreamWriter(mimeEntry.Open(), Encoding.ASCII))
                    {
                        sw.Write("application/vnd.oasis.opendocument.spreadsheet");
                    }

                    // 2. META-INF/manifest.xml
                    ZipArchiveEntry manifestEntry = archive.CreateEntry("META-INF/manifest.xml");
                    using (StreamWriter sw = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\" manifest:version=\"1.2\">");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"/\" manifest:version=\"1.2\" manifest:media-type=\"application/vnd.oasis.opendocument.spreadsheet\"/>");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"content.xml\" manifest:media-type=\"text/xml\"/>");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"styles.xml\" manifest:media-type=\"text/xml\"/>");
                        sw.WriteLine("</manifest:manifest>");
                    }

                    // 3. content.xml
                    ZipArchiveEntry contentEntry = archive.CreateEntry("content.xml");
                    using (StreamWriter sw = new StreamWriter(contentEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<office:document-content xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\"");
                        sw.WriteLine("  xmlns:table=\"urn:oasis:names:tc:opendocument:xmlns:table:1.0\"");
                        sw.WriteLine("  xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\"");
                        sw.WriteLine("  xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\"");
                        sw.WriteLine("  xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\"");
                        sw.WriteLine("  office:version=\"1.2\">");
                        sw.WriteLine("  <office:body>");
                        sw.WriteLine("    <office:spreadsheet>");

                        for (int s = 0; s < calcView.Sheets.Count; s++)
                        {
                            SparseMatrix sheet = calcView.Sheets[s];
                            string sName = (s < calcView.SheetNames.Count) ? calcView.SheetNames[s] : ("Sheet" + (s + 1));
                            sw.WriteLine(string.Format("      <table:table table:name=\"{0}\">", EscapeXml(sName)));

                            uint maxR = Math.Max(1, sheet.GetMaxRow());
                            uint maxC = Math.Max(1, sheet.GetMaxCol());

                            for (uint r = 1; r <= maxR; r++)
                            {
                                sw.WriteLine("        <table:table-row>");
                                for (uint c = 1; c <= maxC; c++)
                                {
                                    CellData cell = sheet.GetCell(r, c);
                                    if (cell == null || (cell.Type == CellType.Empty && string.IsNullOrEmpty(cell.FormulaStr)))
                                    {
                                        sw.WriteLine("          <table:table-cell/>");
                                    }
                                    else if (!string.IsNullOrEmpty(cell.FormulaStr))
                                    {
                                        string odfFmt = cell.FormulaStr.Replace("=", "of:=");
                                        sw.WriteLine(string.Format("          <table:table-cell table:formula=\"{0}\"><text:p>{1}</text:p></table:table-cell>",
                                            EscapeXml(odfFmt), EscapeXml(cell.EvaluatedStr ?? "")));
                                    }
                                    else if (cell.Type == CellType.Number)
                                    {
                                        sw.WriteLine(string.Format("          <table:table-cell office:value-type=\"float\" office:value=\"{0}\"><text:p>{1}</text:p></table:table-cell>",
                                            cell.NumericValue.ToString(CultureInfo.InvariantCulture), EscapeXml(cell.EvaluatedStr ?? cell.NumericValue.ToString())));
                                    }
                                    else
                                    {
                                        sw.WriteLine(string.Format("          <table:table-cell office:value-type=\"string\"><text:p>{0}</text:p></table:table-cell>",
                                            EscapeXml(cell.RawValue ?? "")));
                                    }
                                }
                                sw.WriteLine("        </table:table-row>");
                            }
                            sw.WriteLine("      </table:table>");
                        }

                        sw.WriteLine("    </office:spreadsheet>");
                        sw.WriteLine("  </office:body>");
                        sw.WriteLine("</office:document-content>");
                    }

                    // 4. styles.xml
                    ZipArchiveEntry stylesEntry = archive.CreateEntry("styles.xml");
                    using (StreamWriter sw = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<office:document-styles xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\"");
                        sw.WriteLine("  xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\"");
                        sw.WriteLine("  xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\"");
                        sw.WriteLine("  office:version=\"1.2\">");
                        sw.WriteLine("  <office:styles>");
                        sw.WriteLine("    <style:default-style style:family=\"table-cell\">");
                        sw.WriteLine("      <style:text-properties fo:font-size=\"10pt\" style:font-name=\"Segoe UI\"/>");
                        sw.WriteLine("    </style:default-style>");
                        sw.WriteLine("  </office:styles>");
                        sw.WriteLine("</office:document-styles>");
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}

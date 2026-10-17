using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    /// <summary>
    /// OpenDocument Text (.odt) Export Engine.
    /// Standard OASIS OpenDocument v1.2 specification package:
    /// - Uncompressed 'mimetype' entry (application/vnd.oasis.opendocument.text)
    /// - META-INF/manifest.xml
    /// - styles.xml
    /// - content.xml with automatic styles, headings, paragraphs, formatted spans, and tables.
    /// Compatible with LibreOffice, Apache OpenOffice, Microsoft Word, and Google Docs.
    /// </summary>
    public static class OdtHandler
    {
        public static bool SaveOdt(
            string filePath,
            string textContent,
            List<FormattedRun> runs = null,
            Dictionary<int, int> paraAlignments = null,
            Dictionary<int, float> paraLineSpacings = null,
            Dictionary<int, float> paraSpacingsBefore = null,
            Dictionary<int, float> paraSpacingsAfter = null,
            Dictionary<int, float> paraFirstLineIndents = null,
            Dictionary<int, float> paraLeftIndents = null,
            Dictionary<int, float> paraRightIndents = null,
            List<ZenQOffice.UI.DocTableObject> tables = null)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);

                using (ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Create))
                {
                    // 1. mimetype (Must be uncompressed first entry in standard ODF)
                    ZipArchiveEntry mimeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                    using (StreamWriter sw = new StreamWriter(mimeEntry.Open(), Encoding.ASCII))
                    {
                        sw.Write("application/vnd.oasis.opendocument.text");
                    }

                    // 2. META-INF/manifest.xml
                    ZipArchiveEntry manifestEntry = archive.CreateEntry("META-INF/manifest.xml");
                    using (StreamWriter sw = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\" manifest:version=\"1.2\">");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"/\" manifest:version=\"1.2\" manifest:media-type=\"application/vnd.oasis.opendocument.text\"/>");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"content.xml\" manifest:media-type=\"text/xml\"/>");
                        sw.WriteLine("  <manifest:file-entry manifest:full-path=\"styles.xml\" manifest:media-type=\"text/xml\"/>");
                        sw.WriteLine("</manifest:manifest>");
                    }

                    // 3. styles.xml
                    ZipArchiveEntry stylesEntry = archive.CreateEntry("styles.xml");
                    using (StreamWriter sw = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<office:document-styles xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\"");
                        sw.WriteLine("  xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\"");
                        sw.WriteLine("  xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\"");
                        sw.WriteLine("  xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\"");
                        sw.WriteLine("  office:version=\"1.2\">");
                        sw.WriteLine("  <office:styles>");
                        sw.WriteLine("    <style:default-style style:family=\"paragraph\">");
                        sw.WriteLine("      <style:paragraph-properties fo:hyphenation-ladder-count=\"no-limit\"/>");
                        sw.WriteLine("      <style:text-properties style:font-name=\"Times New Roman\" fo:font-size=\"12pt\" fo:language=\"vi\" fo:country=\"VN\"/>");
                        sw.WriteLine("    </style:default-style>");
                        sw.WriteLine("  </office:styles>");
                        sw.WriteLine("</office:document-styles>");
                    }

                    // 4. content.xml
                    ZipArchiveEntry contentEntry = archive.CreateEntry("content.xml");
                    using (StreamWriter sw = new StreamWriter(contentEntry.Open(), Encoding.UTF8))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<office:document-content xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\"");
                        sw.WriteLine("  xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\"");
                        sw.WriteLine("  xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\"");
                        sw.WriteLine("  xmlns:table=\"urn:oasis:names:tc:opendocument:xmlns:table:1.0\"");
                        sw.WriteLine("  xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\"");
                        sw.WriteLine("  office:version=\"1.2\">");

                        // 4.1 Automatic Styles
                        sw.WriteLine("  <office:automatic-styles>");
                        sw.WriteLine("    <style:style style:name=\"P_Default\" style:family=\"paragraph\" style:parent-style-name=\"Standard\">");
                        sw.WriteLine("      <style:paragraph-properties fo:margin-top=\"0pt\" fo:margin-bottom=\"4pt\" fo:line-height=\"115%\"/>");
                        sw.WriteLine("      <style:text-properties style:font-name=\"Times New Roman\" fo:font-size=\"12pt\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"P_Center\" style:family=\"paragraph\" style:parent-style-name=\"Standard\">");
                        sw.WriteLine("      <style:paragraph-properties fo:text-align=\"center\" fo:margin-top=\"0pt\" fo:margin-bottom=\"4pt\" fo:line-height=\"115%\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"P_Right\" style:family=\"paragraph\" style:parent-style-name=\"Standard\">");
                        sw.WriteLine("      <style:paragraph-properties fo:text-align=\"end\" fo:margin-top=\"0pt\" fo:margin-bottom=\"4pt\" fo:line-height=\"115%\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"P_Justify\" style:family=\"paragraph\" style:parent-style-name=\"Standard\">");
                        sw.WriteLine("      <style:paragraph-properties fo:text-align=\"justify\" fo:margin-top=\"0pt\" fo:margin-bottom=\"4pt\" fo:line-height=\"115%\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"T_Bold\" style:family=\"text\">");
                        sw.WriteLine("      <style:text-properties fo:font-weight=\"bold\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"T_Italic\" style:family=\"text\">");
                        sw.WriteLine("      <style:text-properties fo:font-style=\"italic\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"T_BoldItalic\" style:family=\"text\">");
                        sw.WriteLine("      <style:text-properties fo:font-weight=\"bold\" fo:font-style=\"italic\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"Table1\" style:family=\"table\">");
                        sw.WriteLine("      <style:table-properties table:align=\"center\" fo:margin-top=\"6pt\" fo:margin-bottom=\"6pt\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"TableCell_Default\" style:family=\"table-cell\">");
                        sw.WriteLine("      <style:table-cell-properties fo:padding=\"4pt\" fo:border=\"0.5pt solid #000000\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("    <style:style style:name=\"TableCell_Header\" style:family=\"table-cell\">");
                        sw.WriteLine("      <style:table-cell-properties fo:background-color=\"#f1f5f9\" fo:padding=\"5pt\" fo:border=\"1pt solid #000000\"/>");
                        sw.WriteLine("    </style:style>");
                        sw.WriteLine("  </office:automatic-styles>");

                        // 4.2 Document Body
                        sw.WriteLine("  <office:body>");
                        sw.WriteLine("    <office:text>");

                        string[] paras = (textContent ?? "").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                        int charOffset = 0;

                        for (int pIdx = 0; pIdx < paras.Length; pIdx++)
                        {
                            string pStr = paras[pIdx];

                            // Page Break
                            if (pStr.Trim() == "[--- PAGE BREAK ---]" || pStr.Contains("TRANG TRẮNG MỚI"))
                            {
                                sw.WriteLine("      <text:p text:style-name=\"P_Default\"/>");
                                charOffset += pStr.Length + 1;
                                continue;
                            }

                            if (pStr.Trim() == "[--- COVER PAGE START ---]" || pStr.Trim() == "[--- COVER PAGE END ---]")
                            {
                                charOffset += pStr.Length + 1;
                                continue;
                            }

                            // Tables
                            if (!string.IsNullOrWhiteSpace(pStr) && pStr.Trim().StartsWith("|"))
                            {
                                List<string> tableLines = new List<string>();
                                int tblLen = 0;
                                while (pIdx < paras.Length && !string.IsNullOrWhiteSpace(paras[pIdx]) && paras[pIdx].Trim().StartsWith("|"))
                                {
                                    tableLines.Add(paras[pIdx]);
                                    tblLen += paras[pIdx].Length + 1;
                                    pIdx++;
                                }
                                pIdx--;

                                WriteOdtTable(sw, tableLines);
                                charOffset += tblLen;
                                continue;
                            }

                            // Alignment style
                            int pAlign = 0;
                            if (paraAlignments != null && paraAlignments.ContainsKey(charOffset))
                            {
                                pAlign = paraAlignments[charOffset];
                            }
                            string pStyle = "P_Default";
                            if (pAlign == 1) pStyle = "P_Center";
                            else if (pAlign == 2) pStyle = "P_Right";
                            else if (pAlign == 3) pStyle = "P_Justify";

                            sw.Write(string.Format("      <text:p text:style-name=\"{0}\">", pStyle));

                            if (string.IsNullOrEmpty(pStr))
                            {
                                sw.WriteLine("</text:p>");
                            }
                            else
                            {
                                int cur = 0;
                                while (cur < pStr.Length)
                                {
                                    FormattedRun fmt = (runs != null) ? GetFormatAtOffset(runs, charOffset + cur) : null;
                                    int runLen = 1;
                                    while (cur + runLen < pStr.Length && (runs == null || GetFormatAtOffset(runs, charOffset + cur + runLen) == fmt))
                                    {
                                        runLen++;
                                    }

                                    string runText = pStr.Substring(cur, runLen);
                                    string spanStyle = null;
                                    if (fmt != null && fmt.Bold && fmt.Italic) spanStyle = "T_BoldItalic";
                                    else if (fmt != null && fmt.Bold) spanStyle = "T_Bold";
                                    else if (fmt != null && fmt.Italic) spanStyle = "T_Italic";

                                    if (!string.IsNullOrEmpty(spanStyle))
                                    {
                                        sw.Write(string.Format("<text:span text:style-name=\"{0}\">{1}</text:span>", spanStyle, EscapeXml(runText)));
                                    }
                                    else
                                    {
                                        sw.Write(EscapeXml(runText));
                                    }

                                    cur += runLen;
                                }
                                sw.WriteLine("</text:p>");
                            }

                            charOffset += pStr.Length + 1;
                        }

                        sw.WriteLine("    </office:text>");
                        sw.WriteLine("  </office:body>");
                        sw.WriteLine("</office:document-content>");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveOdt Error: " + ex.Message);
                return false;
            }
        }

        private static void WriteOdtTable(StreamWriter sw, List<string> tableLines)
        {
            if (tableLines == null || tableLines.Count == 0) return;

            List<List<string>> rows = new List<List<string>>();
            foreach (var line in tableLines)
            {
                string tr = line.Trim();
                if (tr.Length < 2 || !tr.StartsWith("|")) continue;

                string stripped = tr.Replace("|", "").Replace("-", "").Replace(":", "").Trim();
                if (string.IsNullOrEmpty(stripped)) continue;

                string[] parts = tr.Split('|');
                List<string> rowCells = new List<string>();
                for (int i = 1; i < parts.Length - 1; i++)
                {
                    rowCells.Add(parts[i].Trim());
                }
                if (rowCells.Count > 0) rows.Add(rowCells);
            }

            if (rows.Count == 0) return;

            sw.WriteLine("      <table:table table:name=\"Table1\" table:style-name=\"Table1\">");
            int colCount = rows[0].Count;
            for (int c = 0; c < colCount; c++)
            {
                sw.WriteLine("        <table:table-column/>");
            }

            for (int r = 0; r < rows.Count; r++)
            {
                sw.WriteLine("        <table:table-row>");
                var row = rows[r];
                for (int c = 0; c < colCount; c++)
                {
                    string cellVal = (c < row.Count) ? row[c] : "";
                    string cStyle = (r == 0) ? "TableCell_Header" : "TableCell_Default";
                    sw.WriteLine(string.Format("          <table:table-cell table:style-name=\"{0}\" office:value-type=\"string\">", cStyle));
                    sw.Write("            <text:p text:style-name=\"P_Default\">");
                    if (r == 0) sw.Write(string.Format("<text:span text:style-name=\"T_Bold\">{0}</text:span>", EscapeXml(cellVal)));
                    else sw.Write(EscapeXml(cellVal));
                    sw.WriteLine("</text:p>");
                    sw.WriteLine("          </table:table-cell>");
                }
                sw.WriteLine("        </table:table-row>");
            }
            sw.WriteLine("      </table:table>");
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        private static FormattedRun GetFormatAtOffset(List<FormattedRun> runs, int offset)
        {
            if (runs == null || runs.Count == 0) return null;
            for (int i = runs.Count - 1; i >= 0; i--)
            {
                var r = runs[i];
                if (offset >= r.StartIndex && offset < r.StartIndex + r.Length)
                {
                    return r;
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    /// <summary>
    /// Genuine Rich Text Format (RTF 1.9.1 / MS Word standard) export engine.
    /// Supports full Unicode escaping (\uXXXX?), dynamic font table, dynamic color table,
    /// paragraph alignments, line spacings, space before/after, indents, formatting runs, and tables.
    /// </summary>
    public static class RtfHandler
    {
        public static bool SaveRtf(
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

                // 1. Build Font Table & Color Table from runs
                List<string> fontTable = new List<string> { "Times New Roman", "Arial", "Segoe UI", "Calibri", "Courier New" };
                List<Color> colorTable = new List<Color>();

                if (runs != null)
                {
                    foreach (var r in runs)
                    {
                        if (!string.IsNullOrEmpty(r.FontName) && !fontTable.Contains(r.FontName))
                        {
                            fontTable.Add(r.FontName);
                        }

                        if (r.Color != Color.Empty && r.Color != Color.Transparent && r.Color != Color.Black && !colorTable.Contains(r.Color))
                        {
                            colorTable.Add(r.Color);
                        }

                        if (r.Highlight != Color.Empty && r.Highlight != Color.Transparent && !colorTable.Contains(r.Highlight))
                        {
                            colorTable.Add(r.Highlight);
                        }
                    }
                }

                StringBuilder sb = new StringBuilder(64 * 1024);

                // 2. RTF Header
                sb.Append(@"{\rtf1\ansi\ansicpg1258\deff0\nouicompat");
                sb.AppendLine();

                // 2.1 Font Table
                sb.Append(@"{\fonttbl");
                for (int fi = 0; fi < fontTable.Count; fi++)
                {
                    sb.Append(@"{\f" + fi + @"\fnil\fcharset0 " + fontTable[fi] + ";}");
                }
                sb.Append("}");
                sb.AppendLine();

                // 2.2 Color Table (Index 0 is default / auto, indices 1+ match colorTable items)
                sb.Append(@"{\colortbl ;");
                for (int ci = 0; ci < colorTable.Count; ci++)
                {
                    Color c = colorTable[ci];
                    sb.Append(@"\red" + c.R + @"\green" + c.G + @"\blue" + c.B + ";");
                }
                sb.Append("}");
                sb.AppendLine();

                // 2.3 Document view & margins (default 1 inch = 1440 twips)
                sb.AppendLine(@"\viewkind4\uc1\margl1440\margr1440\margt1440\margb1440");

                // 3. Process Paragraphs
                string[] rawParas = (textContent ?? "").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                int charOffset = 0;

                for (int pIdx = 0; pIdx < rawParas.Length; pIdx++)
                {
                    string para = rawParas[pIdx];

                    // 3.1 Page Break
                    if (para.Trim() == "[--- PAGE BREAK ---]" || para.Contains("TRANG TRẮNG MỚI"))
                    {
                        sb.AppendLine(@"\page");
                        charOffset += para.Length + 1;
                        continue;
                    }

                    if (para.Trim() == "[--- COVER PAGE START ---]" || para.Trim() == "[--- COVER PAGE END ---]")
                    {
                        charOffset += para.Length + 1;
                        continue;
                    }

                    // 3.2 Tables
                    if (!string.IsNullOrWhiteSpace(para) && para.Trim().StartsWith("|"))
                    {
                        List<string> tableLines = new List<string>();
                        int tblLen = 0;
                        while (pIdx < rawParas.Length && !string.IsNullOrWhiteSpace(rawParas[pIdx]) && rawParas[pIdx].Trim().StartsWith("|"))
                        {
                            tableLines.Add(rawParas[pIdx]);
                            tblLen += rawParas[pIdx].Length + 1;
                            pIdx++;
                        }
                        pIdx--; // Step back for loop increment

                        WriteRtfTable(sb, tableLines, fontTable, colorTable);
                        charOffset += tblLen;
                        continue;
                    }

                    // 3.3 Regular Paragraph Reset & Properties
                    sb.Append(@"\pard");

                    // Alignment
                    int pAlign = 0; // 0: Left, 1: Center, 2: Right, 3: Justify
                    if (paraAlignments != null && paraAlignments.ContainsKey(charOffset))
                    {
                        pAlign = paraAlignments[charOffset];
                    }
                    if (pAlign == 1) sb.Append(@"\qc");
                    else if (pAlign == 2) sb.Append(@"\qr");
                    else if (pAlign == 3) sb.Append(@"\qj");
                    else sb.Append(@"\ql");

                    // Spacing Before / After
                    if (paraSpacingsBefore != null && paraSpacingsBefore.ContainsKey(charOffset))
                    {
                        int sbTwips = (int)(paraSpacingsBefore[charOffset] * 20);
                        if (sbTwips > 0) sb.Append(@"\sb" + sbTwips);
                    }
                    if (paraSpacingsAfter != null && paraSpacingsAfter.ContainsKey(charOffset))
                    {
                        int saTwips = (int)(paraSpacingsAfter[charOffset] * 20);
                        if (saTwips > 0) sb.Append(@"\sa" + saTwips);
                    }

                    // Line Spacing
                    float lSpacing = 1.15f;
                    if (paraLineSpacings != null && paraLineSpacings.ContainsKey(charOffset))
                    {
                        lSpacing = paraLineSpacings[charOffset];
                    }
                    int lineTwips = (int)(lSpacing * 240);
                    sb.Append(@"\sl" + lineTwips + @"\slmult1");

                    // Indentations
                    if (paraFirstLineIndents != null && paraFirstLineIndents.ContainsKey(charOffset))
                    {
                        int fiTwips = (int)(paraFirstLineIndents[charOffset] * 20);
                        if (fiTwips != 0) sb.Append(@"\fi" + fiTwips);
                    }
                    if (paraLeftIndents != null && paraLeftIndents.ContainsKey(charOffset))
                    {
                        int liTwips = (int)(paraLeftIndents[charOffset] * 20);
                        if (liTwips > 0) sb.Append(@"\li" + liTwips);
                    }
                    if (paraRightIndents != null && paraRightIndents.ContainsKey(charOffset))
                    {
                        int riTwips = (int)(paraRightIndents[charOffset] * 20);
                        if (riTwips > 0) sb.Append(@"\ri" + riTwips);
                    }

                    sb.Append(" ");

                    // 3.4 Runs in paragraph
                    if (string.IsNullOrEmpty(para))
                    {
                        sb.AppendLine(@"\par");
                    }
                    else
                    {
                        int runCursor = 0;
                        while (runCursor < para.Length)
                        {
                            FormattedRun fmt = (runs != null) ? GetFormatAtOffset(runs, charOffset + runCursor) : null;
                            int runLen = 1;
                            while (runCursor + runLen < para.Length && (runs == null || GetFormatAtOffset(runs, charOffset + runCursor + runLen) == fmt))
                            {
                                runLen++;
                            }

                            string runText = para.Substring(runCursor, runLen);
                            WriteRtfRun(sb, runText, fmt, fontTable, colorTable);
                            runCursor += runLen;
                        }
                        sb.AppendLine(@"\par");
                    }

                    charOffset += para.Length + 1;
                }

                // 4. Close RTF document
                sb.AppendLine("}");

                File.WriteAllText(filePath, sb.ToString(), Encoding.ASCII);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveRtf Error: " + ex.Message);
                return false;
            }
        }

        private static void WriteRtfRun(StringBuilder sb, string text, FormattedRun fmt, List<string> fontTable, List<Color> colorTable)
        {
            if (string.IsNullOrEmpty(text)) return;

            sb.Append("{");

            // Font
            string fontName = (fmt != null && !string.IsNullOrEmpty(fmt.FontName)) ? fmt.FontName : "Times New Roman";
            int fIdx = fontTable.IndexOf(fontName);
            if (fIdx < 0) fIdx = 0;
            sb.Append(@"\f" + fIdx);

            // Font size in half-points (12pt -> 24)
            int halfPoints = (fmt != null && fmt.FontSize > 0) ? (int)(fmt.FontSize * 2) : 24;
            sb.Append(@"\fs" + halfPoints);

            // Bold, Italic, Underline, Strike
            if (fmt != null && fmt.Bold) sb.Append(@"\b");
            if (fmt != null && fmt.Italic) sb.Append(@"\i");
            if (fmt != null && fmt.Underline) sb.Append(@"\ul");
            if (fmt != null && fmt.Strike) sb.Append(@"\strike");
            if (fmt != null && fmt.Subscript) sb.Append(@"\sub");
            if (fmt != null && fmt.Superscript) sb.Append(@"\super");

            // Text color
            if (fmt != null && fmt.Color != Color.Empty && fmt.Color != Color.Transparent && fmt.Color != Color.Black)
            {
                int cIdx = colorTable.IndexOf(fmt.Color);
                if (cIdx >= 0) sb.Append(@"\cf" + (cIdx + 1));
            }

            // Highlight color
            if (fmt != null && fmt.Highlight != Color.Empty && fmt.Highlight != Color.Transparent)
            {
                int hIdx = colorTable.IndexOf(fmt.Highlight);
                if (hIdx >= 0) sb.Append(@"\highlight" + (hIdx + 1));
            }

            sb.Append(" ");
            sb.Append(EscapeRtfText(text));
            sb.Append("}");
        }

        private static void WriteRtfTable(StringBuilder sb, List<string> tableLines, List<string> fontTable, List<Color> colorTable)
        {
            if (tableLines == null || tableLines.Count == 0) return;

            List<List<string>> rows = new List<List<string>>();
            foreach (var line in tableLines)
            {
                string tr = line.Trim();
                if (tr.Length < 2 || !tr.StartsWith("|")) continue;

                // Check separator row like "| --- | --- |"
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

            int colCount = rows[0].Count;
            int totalTwips = 9000;
            int colTwips = totalTwips / Math.Max(1, colCount);

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                sb.Append(@"\trowd\trgaph108\trleft0");

                // Cell boundary definitions
                for (int c = 0; c < colCount; c++)
                {
                    sb.Append(@"\clbrdrt\brdrs\brdrw10\clbrdrl\brdrs\brdrw10\clbrdrb\brdrs\brdrw10\clbrdrr\brdrs\brdrw10");
                    sb.Append(@"\cellx" + ((c + 1) * colTwips));
                }

                // Cell text contents
                for (int c = 0; c < colCount; c++)
                {
                    string cellVal = (c < row.Count) ? row[c] : "";
                    sb.Append(@"\pard\intbl ");
                    if (r == 0) sb.Append(@"\b ");
                    sb.Append(EscapeRtfText(cellVal));
                    if (r == 0) sb.Append(@"\b0 ");
                    sb.Append(@"\cell ");
                }

                sb.AppendLine(@"\row");
            }
            sb.AppendLine(@"\pard");
        }

        public static string EscapeRtfText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length * 2);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\\') sb.Append(@"\\");
                else if (c == '{') sb.Append(@"\{");
                else if (c == '}') sb.Append(@"\}");
                else if (c == '\t') sb.Append(@"\tab ");
                else if (c == '\n' || c == '\r') { /* Handled at paragraph level */ }
                else if (c > 127)
                {
                    // Official RTF Unicode character escape: \u<dec>?
                    sb.Append(@"\u" + ((int)c) + "?");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
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

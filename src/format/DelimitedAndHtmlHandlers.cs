using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Format
{
    /// <summary>
    /// Implements export and import routines for Web (HTML) and delimited table formats (TSV, PRN).
    /// </summary>
    public static class DelimitedAndHtmlHandlers
    {
        public static bool ExportHtml(CalcView calcView, string filePath, bool exportAllSheets = false)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath)) return false;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head>");
                sb.AppendLine("  <meta charset=\"utf-8\">");
                sb.AppendLine("  <title>ZenQ Calc Sheet Export</title>");
                sb.AppendLine("  <style>");
                sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 24px; }");
                sb.AppendLine("    table.zenq-table { border-collapse: collapse; width: 100%; margin-bottom: 30px; font-size: 13px; }");
                sb.AppendLine("    table.zenq-table th, table.zenq-table td { border: 1px solid #CBD5E1; padding: 8px 12px; }");
                sb.AppendLine("    table.zenq-table th { background-color: #E2E8F0; font-weight: bold; text-align: left; }");
                sb.AppendLine("    .text-num { text-align: right; }");
                sb.AppendLine("  </style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");

                int sheetsToExport = exportAllSheets ? calcView.Sheets.Count : 1;
                for (int s = 0; s < sheetsToExport; s++)
                {
                    SparseMatrix sheet = calcView.Sheets[s];
                    string sName = (s < calcView.SheetNames.Count) ? calcView.SheetNames[s] : ("Sheet" + (s + 1));
                    sb.AppendLine("  <h2>" + EscapeHtml(sName) + "</h2>");
                    sb.AppendLine("  <table class=\"zenq-table\">");

                    uint maxR = Math.Max(1, sheet.GetMaxRow());
                    uint maxC = Math.Max(1, sheet.GetMaxCol());

                    for (uint r = 1; r <= maxR; r++)
                    {
                        sb.AppendLine("    <tr>");
                        for (uint c = 1; c <= maxC; c++)
                        {
                            CellData cell = sheet.GetCell(r, c);
                            string txt = cell != null ? (!string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue) : "";
                            string tag = (r == 1) ? "th" : "td";
                            string cls = (cell != null && cell.Type == CellType.Number) ? " class=\"text-num\"" : "";
                            sb.AppendLine("      <" + tag + cls + ">" + EscapeHtml(txt ?? "") + "</" + tag + ">");
                        }
                        sb.AppendLine("    </tr>");
                    }
                    sb.AppendLine("  </table>");
                }

                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ExportTsv(CalcView calcView, string filePath)
        {
            if (calcView == null || string.IsNullOrEmpty(filePath) || calcView.Sheets.Count == 0) return false;
            try
            {
                SparseMatrix sheet = calcView.Sheets[0];
                uint maxR = sheet.GetMaxRow();
                uint maxC = sheet.GetMaxCol();

                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    for (uint r = 1; r <= maxR; r++)
                    {
                        List<string> rowVals = new List<string>();
                        for (uint c = 1; c <= maxC; c++)
                        {
                            CellData cell = sheet.GetCell(r, c);
                            string txt = cell != null ? (!string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue) : "";
                            if (txt.Contains("\t") || txt.Contains("\""))
                            {
                                txt = "\"" + txt.Replace("\"", "\"\"") + "\"";
                            }
                            rowVals.Add(txt);
                        }
                        sw.WriteLine(string.Join("\t", rowVals.ToArray()));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EscapeHtml(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}

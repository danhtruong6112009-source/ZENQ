using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ZenQOffice.Core
{
    public class FormulaEngine
    {
        public static List<SparseMatrix> WorkbookSheets = null;
        public static List<string> WorkbookSheetNames = null;

        public static void SetWorkbookContext(List<SparseMatrix> sheets, List<string> sheetNames)
        {
            WorkbookSheets = sheets;
            WorkbookSheetNames = sheetNames;
        }

        public static void EvaluateWorkbook(List<SparseMatrix> sheets, List<string> sheetNames)
        {
            SetWorkbookContext(sheets, sheetNames);
            if (sheets == null) return;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < sheets.Count; i++)
                {
                    EvaluateSheet(sheets[i]);
                }
            }
        }

        public static SparseMatrix ResolveSheet(string expr, SparseMatrix defaultSheet)
        {
            if (string.IsNullOrEmpty(expr) || WorkbookSheets == null || WorkbookSheetNames == null) return defaultSheet;
            string s = expr.Trim();
            int excl = -1;
            bool inQuotes = false;
            bool inSingleQuotes = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '"' && !inSingleQuotes) inQuotes = !inQuotes;
                else if (s[i] == '\'' && !inQuotes) inSingleQuotes = !inSingleQuotes;
                else if (s[i] == '!' && !inQuotes && !inSingleQuotes)
                {
                    excl = i;
                    break;
                }
            }
            if (excl <= 0) return defaultSheet;
            string sheetPart = s.Substring(0, excl).Trim();
            if (sheetPart.StartsWith("'") && sheetPart.EndsWith("'") && sheetPart.Length >= 2)
            {
                sheetPart = sheetPart.Substring(1, sheetPart.Length - 2).Replace("''", "'");
            }
            for (int i = 0; i < WorkbookSheetNames.Count && i < WorkbookSheets.Count; i++)
            {
                if (string.Equals(WorkbookSheetNames[i], sheetPart, StringComparison.OrdinalIgnoreCase))
                {
                    return WorkbookSheets[i] ?? defaultSheet;
                }
            }
            return defaultSheet;
        }

        private static bool IsSingleTopLevelFunctionCall(string expr, out string funcName, out string argsStr)
        {
            funcName = "";
            argsStr = "";
            if (string.IsNullOrEmpty(expr) || !expr.EndsWith(")")) return false;
            int openParen = expr.IndexOf('(');
            if (openParen <= 0) return false;

            string prefix = expr.Substring(0, openParen).Trim();
            if (string.IsNullOrEmpty(prefix)) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                char ch = prefix[i];
                if (i == 0 && !(char.IsLetter(ch) || ch == '_')) return false;
                if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_')) return false;
            }

            int depth = 0;
            bool inQuote = false;
            for (int i = openParen; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '"') inQuote = !inQuote;
                else if (!inQuote)
                {
                    if (c == '(') depth++;
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0 && i < expr.Length - 1)
                        {
                            return false;
                        }
                    }
                }
            }
            if (depth != 0) return false;

            funcName = prefix.ToUpperInvariant();
            if (funcName.StartsWith("_XLFN._XLWS.")) funcName = funcName.Substring(12);
            else if (funcName.StartsWith("_XLFN.")) funcName = funcName.Substring(6);
            argsStr = expr.Substring(openParen + 1, expr.Length - openParen - 2);
            return true;
        }

        public static void EvaluateSheet(SparseMatrix sheet)
        {
            if (sheet == null) return;
            var cells = new List<KeyValuePair<ulong, CellData>>(sheet.GetAllCells());
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var kvp in cells)
                {
                    CellData cell = kvp.Value;
                    if (cell.Type == CellType.Formula)
                    {
                        CellData evalRes = EvaluateFormula(cell.FormulaStr, sheet);
                        cell.NumericValue = evalRes.NumericValue;
                        cell.EvaluatedStr = evalRes.EvaluatedStr;
                    }
                    else if (cell.Type == CellType.Number)
                    {
                        cell.EvaluatedStr = FormatCellDisplay(cell);
                    }
                    else if (cell.Type == CellType.String)
                    {
                        cell.EvaluatedStr = cell.RawValue;
                    }
                }
            }
        }

        public static string FormatCellDisplay(CellData cell)
        {
            if (cell == null) return "";
            if (cell.Type == CellType.String) return cell.RawValue ?? "";
            if (cell.Type == CellType.Empty) return "";

            string fmt = cell.Format != null ? (cell.Format.NumberFormat ?? "General") : "General";
            int dec = cell.Format != null ? Math.Max(0, Math.Min(10, cell.Format.DecimalPlaces)) : 2;
            double val = cell.NumericValue;

            switch (fmt)
            {
                case "Currency":
                case "$":
                    return "$" + val.ToString("N" + dec, CultureInfo.InvariantCulture);

                case "VND":
                case "₫":
                    return val.ToString("N0", new CultureInfo("vi-VN")) + " ₫";

                case "Percent":
                case "%":
                    return (val * 100.0).ToString("N" + Math.Max(0, dec - 1), CultureInfo.InvariantCulture) + "%";

                case "Comma":
                case ",":
                    return val.ToString("N" + dec, CultureInfo.InvariantCulture);

                case "Decimals0":
                    return val.ToString("N0", CultureInfo.InvariantCulture);

                case "Decimals2":
                    return val.ToString("N2", CultureInfo.InvariantCulture);

                case "Number":
                    return val.ToString("F" + dec, CultureInfo.InvariantCulture);

                case "Scientific":
                    return val.ToString("E" + dec, CultureInfo.InvariantCulture);

                case "Date":
                    try
                    {
                        DateTime dt = DateTime.FromOADate(val);
                        return dt.ToString("dd/MM/yyyy");
                    }
                    catch
                    {
                        return val.ToString(CultureInfo.InvariantCulture);
                    }

                case "General":
                default:
                    if (Math.Abs(val - Math.Round(val)) < 0.0000001 && Math.Abs(val) < 1e12)
                    {
                        return ((long)Math.Round(val)).ToString(CultureInfo.InvariantCulture);
                    }
                    return val.ToString("G10", CultureInfo.InvariantCulture);
            }
        }

        public static CellData EvaluateFormula(string formula, SparseMatrix sheet)
        {
            CellData res = new CellData();
            res.Type = CellType.Formula;
            res.FormulaStr = formula;

            string expr = (formula ?? "").Trim();
            if (string.IsNullOrEmpty(expr) || expr[0] != '=')
            {
                res.SetString(formula);
                return res;
            }

            expr = expr.Substring(1).Trim();
            if (expr.StartsWith("_xlfn._xlws.", StringComparison.OrdinalIgnoreCase))
                expr = expr.Substring(12).Trim();
            else if (expr.StartsWith("_xlfn.", StringComparison.OrdinalIgnoreCase))
                expr = expr.Substring(6).Trim();

            if (IsCellReference(expr))
            {
                SparseMatrix refSheet = ResolveSheet(expr, sheet);
                var pos = SparseMatrix.ParseCellAddress(expr);
                CellData src = refSheet.GetCell(pos.Item1, pos.Item2);
                if (src != null)
                {
                    res.NumericValue = src.NumericValue;
                    res.EvaluatedStr = src.EvaluatedStr ?? src.RawValue;
                    res.RawValue = src.RawValue;
                    res.Type = src.Type;
                    return res;
                }
                res.SetNumber(0);
                res.EvaluatedStr = "0";
                return res;
            }

            string funcName;
            string argsStr;
            if (IsSingleTopLevelFunctionCall(expr, out funcName, out argsStr))
            {
                string[] args = SplitArgs(argsStr);

                // Math & Stats Functions
                if (funcName == "SUM")
                {
                    List<double> values = new List<double>();
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && cell.Type != CellType.Empty) values.Add(cell.NumericValue);
                            }
                        }
                        else
                        {
                            values.Add(ParseAndEvaluateNumeric(arg, sheet));
                        }
                    }
                    double sum = HardwareAsmEngine.FastSum(values.ToArray());
                    res.SetNumber(sum);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "AVERAGE" || funcName == "AVERAGEA")
                {
                    List<double> values = new List<double>();
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && cell.Type != CellType.Empty)
                                {
                                    values.Add(cell.NumericValue);
                                }
                            }
                        }
                        else
                        {
                            values.Add(ParseAndEvaluateNumeric(arg, sheet));
                        }
                    }
                    double avg = HardwareAsmEngine.FastAverage(values.ToArray());
                    res.SetNumber(avg);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "SUMPRODUCT")
                {
                    if (args.Length >= 2)
                    {
                        SparseMatrix sA = ResolveSheet(args[0], sheet);
                        SparseMatrix sB = ResolveSheet(args[1], sheet);
                        var rangeA = ParseRange(args[0]);
                        var rangeB = ParseRange(args[1]);
                        int n = Math.Min(rangeA.Count, rangeB.Count);
                        double[] arrA = new double[n];
                        double[] arrB = new double[n];
                        for (int i = 0; i < n; i++)
                        {
                            CellData cA = sA.GetCell(rangeA[i].Item1, rangeA[i].Item2);
                            CellData cB = sB.GetCell(rangeB[i].Item1, rangeB[i].Item2);
                            arrA[i] = (cA != null) ? cA.NumericValue : 0.0;
                            arrB[i] = (cB != null) ? cB.NumericValue : 0.0;
                        }
                        double dot = HardwareAsmEngine.FastDotProduct(arrA, arrB);
                        res.SetNumber(dot);
                        res.EvaluatedStr = FormatCellDisplay(res);
                        return res;
                    }
                }
                else if (funcName == "COUNT")
                {
                    int count = 0;
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && (cell.Type == CellType.Number || (cell.Type == CellType.Formula && !string.IsNullOrEmpty(cell.EvaluatedStr))))
                                    count++;
                            }
                        }
                        else
                        {
                            double nVal;
                            if (double.TryParse(arg, NumberStyles.Any, CultureInfo.InvariantCulture, out nVal) || double.TryParse(arg, out nVal))
                                count++;
                        }
                    }
                    res.SetNumber(count);
                    res.EvaluatedStr = count.ToString();
                    return res;
                }
                else if (funcName == "COUNTA")
                {
                    int count = 0;
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && (!string.IsNullOrEmpty(cell.RawValue) || !string.IsNullOrEmpty(cell.EvaluatedStr)))
                                    count++;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(arg)) count++;
                        }
                    }
                    res.SetNumber(count);
                    res.EvaluatedStr = count.ToString();
                    return res;
                }
                else if (funcName == "MIN" || funcName == "MINA")
                {
                    double minVal = double.MaxValue;
                    bool found = false;
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && cell.Type != CellType.Empty)
                                {
                                    minVal = Math.Min(minVal, cell.NumericValue);
                                    found = true;
                                }
                            }
                        }
                        else
                        {
                            minVal = Math.Min(minVal, ParseAndEvaluateNumeric(arg, sheet));
                            found = true;
                        }
                    }
                    res.SetNumber(found ? minVal : 0.0);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "MAX" || funcName == "MAXA")
                {
                    double maxVal = double.MinValue;
                    bool found = false;
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && cell.Type != CellType.Empty)
                                {
                                    maxVal = Math.Max(maxVal, cell.NumericValue);
                                    found = true;
                                }
                            }
                        }
                        else
                        {
                            maxVal = Math.Max(maxVal, ParseAndEvaluateNumeric(arg, sheet));
                            found = true;
                        }
                    }
                    res.SetNumber(found ? maxVal : 0.0);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "PRODUCT")
                {
                    double prod = 1.0;
                    bool found = false;
                    foreach (var arg in args)
                    {
                        SparseMatrix argSheet = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = argSheet.GetCell(cellPos.Item1, cellPos.Item2);
                                if (cell != null && cell.Type != CellType.Empty)
                                {
                                    prod *= cell.NumericValue;
                                    found = true;
                                }
                            }
                        }
                        else
                        {
                            prod *= ParseAndEvaluateNumeric(arg, sheet);
                            found = true;
                        }
                    }
                    res.SetNumber(found ? prod : 0.0);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "ROUND" || funcName == "ROUNDUP" || funcName == "ROUNDDOWN")
                {
                    double numVal = args.Length > 0 ? ParseAndEvaluateNumeric(args[0], sheet) : 0.0;
                    int numDigits = args.Length > 1 ? (int)ParseAndEvaluateNumeric(args[1], sheet) : 0;
                    double mult = Math.Pow(10, numDigits);
                    double roundRes = 0.0;
                    if (funcName == "ROUND") roundRes = Math.Round(numVal, Math.Max(0, numDigits));
                    else if (funcName == "ROUNDUP") roundRes = Math.Ceiling(numVal * mult) / mult;
                    else if (funcName == "ROUNDDOWN") roundRes = Math.Floor(numVal * mult) / mult;
                    res.SetNumber(roundRes);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "ABS")
                {
                    double numVal = args.Length > 0 ? Math.Abs(ParseAndEvaluateNumeric(args[0], sheet)) : 0.0;
                    res.SetNumber(numVal);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "SQRT")
                {
                    double numVal = args.Length > 0 ? Math.Sqrt(Math.Max(0, ParseAndEvaluateNumeric(args[0], sheet))) : 0.0;
                    res.SetNumber(numVal);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "POWER")
                {
                    double bVal = args.Length > 0 ? ParseAndEvaluateNumeric(args[0], sheet) : 0.0;
                    double pVal = args.Length > 1 ? ParseAndEvaluateNumeric(args[1], sheet) : 1.0;
                    res.SetNumber(Math.Pow(bVal, pVal));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }

                // Logical Functions
                else if (funcName == "IF")
                {
                    bool condition = EvaluateCondition(args.Length > 0 ? args[0] : "0", sheet);
                    string chosenBranch = condition ? (args.Length > 1 ? args[1] : "TRUE") : (args.Length > 2 ? args[2] : "FALSE");
                    SetResultFromToken(res, chosenBranch, sheet);
                    return res;
                }
                else if (funcName == "AND")
                {
                    bool allTrue = true;
                    foreach (var a in args)
                    {
                        if (!EvaluateCondition(a, sheet)) { allTrue = false; break; }
                    }
                    res.SetString(allTrue ? "TRUE" : "FALSE");
                    res.NumericValue = allTrue ? 1 : 0;
                    return res;
                }
                else if (funcName == "OR")
                {
                    bool anyTrue = false;
                    foreach (var a in args)
                    {
                        if (EvaluateCondition(a, sheet)) { anyTrue = true; break; }
                    }
                    res.SetString(anyTrue ? "TRUE" : "FALSE");
                    res.NumericValue = anyTrue ? 1 : 0;
                    return res;
                }
                else if (funcName == "NOT")
                {
                    bool cond = args.Length > 0 && EvaluateCondition(args[0], sheet);
                    res.SetString(!cond ? "TRUE" : "FALSE");
                    res.NumericValue = !cond ? 1 : 0;
                    return res;
                }
                else if (funcName == "CHOOSE" && args.Length >= 2)
                {
                    int idx = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    if (idx >= 1 && idx < args.Length)
                    {
                        SetResultFromToken(res, args[idx], sheet);
                    }
                    else
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }

                // Conditional Math & Stats Functions (SUMIF, COUNTIF, AVERAGEIF, etc.)
                else if (funcName == "COUNTIF" && args.Length >= 2)
                {
                    SparseMatrix sRange = ResolveSheet(args[0], sheet);
                    var range = ParseRange(args[0]);
                    string crit = args[1];
                    int matchCount = 0;
                    foreach (var cellPos in range)
                    {
                        CellData c = sRange.GetCell(cellPos.Item1, cellPos.Item2);
                        if (MatchesCriteria(c, crit, sheet)) matchCount++;
                    }
                    res.SetNumber(matchCount);
                    res.EvaluatedStr = matchCount.ToString();
                    return res;
                }
                else if (funcName == "SUMIF" && args.Length >= 2)
                {
                    SparseMatrix sCrit = ResolveSheet(args[0], sheet);
                    var critRange = ParseRange(args[0]);
                    string crit = args[1];
                    SparseMatrix sSum = args.Length >= 3 ? ResolveSheet(args[2], sheet) : sCrit;
                    var sumRange = args.Length >= 3 ? ParseRange(args[2]) : critRange;

                    double totalSum = 0.0;
                    int n = Math.Min(critRange.Count, sumRange.Count);
                    for (int i = 0; i < n; i++)
                    {
                        CellData cCrit = sCrit.GetCell(critRange[i].Item1, critRange[i].Item2);
                        if (MatchesCriteria(cCrit, crit, sheet))
                        {
                            CellData cSum = sSum.GetCell(sumRange[i].Item1, sumRange[i].Item2);
                            if (cSum != null) totalSum += cSum.NumericValue;
                        }
                    }
                    res.SetNumber(totalSum);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "AVERAGEIF" && args.Length >= 2)
                {
                    SparseMatrix sCrit = ResolveSheet(args[0], sheet);
                    var critRange = ParseRange(args[0]);
                    string crit = args[1];
                    SparseMatrix sAvg = args.Length >= 3 ? ResolveSheet(args[2], sheet) : sCrit;
                    var avgRange = args.Length >= 3 ? ParseRange(args[2]) : critRange;

                    double totalSum = 0.0;
                    int matchCount = 0;
                    int n = Math.Min(critRange.Count, avgRange.Count);
                    for (int i = 0; i < n; i++)
                    {
                        CellData cCrit = sCrit.GetCell(critRange[i].Item1, critRange[i].Item2);
                        if (MatchesCriteria(cCrit, crit, sheet))
                        {
                            CellData cAvg = sAvg.GetCell(avgRange[i].Item1, avgRange[i].Item2);
                            if (cAvg != null)
                            {
                                totalSum += cAvg.NumericValue;
                                matchCount++;
                            }
                        }
                    }
                    if (matchCount > 0)
                    {
                        res.SetNumber(totalSum / matchCount);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else
                    {
                        res.SetString("#DIV/0!");
                    }
                    return res;
                }
                else if (funcName == "MOD" && args.Length >= 2)
                {
                    double nVal = ParseAndEvaluateNumeric(args[0], sheet);
                    double dVal = ParseAndEvaluateNumeric(args[1], sheet);
                    if (Math.Abs(dVal) > 1e-12)
                    {
                        double m = nVal % dVal;
                        if ((m < 0 && dVal > 0) || (m > 0 && dVal < 0)) m += dVal;
                        res.SetNumber(m);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else
                    {
                        res.SetString("#DIV/0!");
                    }
                    return res;
                }
                else if (funcName == "INT" && args.Length >= 1)
                {
                    double nVal = ParseAndEvaluateNumeric(args[0], sheet);
                    res.SetNumber(Math.Floor(nVal));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "TRUNC" && args.Length >= 1)
                {
                    double nVal = ParseAndEvaluateNumeric(args[0], sheet);
                    int numDigits = args.Length > 1 ? (int)ParseAndEvaluateNumeric(args[1], sheet) : 0;
                    double mult = Math.Pow(10, numDigits);
                    double truncRes = Math.Truncate(nVal * mult) / mult;
                    res.SetNumber(truncRes);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "MEDIAN" && args.Length >= 1)
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count > 0)
                    {
                        vals.Sort();
                        double med = (vals.Count % 2 == 1) ? vals[vals.Count / 2] : (vals[vals.Count / 2 - 1] + vals[vals.Count / 2]) / 2.0;
                        res.SetNumber(med);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else
                    {
                        res.SetNumber(0);
                    }
                    return res;
                }
                else if (funcName == "LARGE" && args.Length >= 2)
                {
                    SparseMatrix sRange = ResolveSheet(args[0], sheet);
                    var r = ParseRange(args[0]);
                    int k = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    List<double> vals = new List<double>();
                    foreach (var p in r)
                    {
                        var c = sRange.GetCell(p.Item1, p.Item2);
                        if (c != null && (c.Type == CellType.Number || c.Type == CellType.Formula)) vals.Add(c.NumericValue);
                    }
                    vals.Sort((a, b) => b.CompareTo(a)); // Descending
                    if (k >= 1 && k <= vals.Count)
                    {
                        res.SetNumber(vals[k - 1]);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else
                    {
                        res.SetString("#NUM!");
                    }
                    return res;
                }
                else if (funcName == "SMALL" && args.Length >= 2)
                {
                    SparseMatrix sRange = ResolveSheet(args[0], sheet);
                    var r = ParseRange(args[0]);
                    int k = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    List<double> vals = new List<double>();
                    foreach (var p in r)
                    {
                        var c = sRange.GetCell(p.Item1, p.Item2);
                        if (c != null && (c.Type == CellType.Number || c.Type == CellType.Formula)) vals.Add(c.NumericValue);
                    }
                    vals.Sort(); // Ascending
                    if (k >= 1 && k <= vals.Count)
                    {
                        res.SetNumber(vals[k - 1]);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else
                    {
                        res.SetString("#NUM!");
                    }
                    return res;
                }
                else if (funcName == "PROPER" && args.Length >= 1)
                {
                    string text = GetStringValue(args[0], sheet);
                    res.SetString(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower()));
                    return res;
                }
                else if (funcName == "SUBSTITUTE" && args.Length >= 3)
                {
                    string text = GetStringValue(args[0], sheet);
                    string oldText = GetStringValue(args[1], sheet);
                    string newText = GetStringValue(args[2], sheet);
                    if (args.Length >= 4)
                    {
                        int instance = (int)ParseAndEvaluateNumeric(args[3], sheet);
                        int count = 0;
                        int idx = -1;
                        while ((idx = text.IndexOf(oldText, idx + 1, StringComparison.Ordinal)) >= 0)
                        {
                            count++;
                            if (count == instance)
                            {
                                text = text.Substring(0, idx) + newText + text.Substring(idx + oldText.Length);
                                break;
                            }
                        }
                    }
                    else
                    {
                        text = text.Replace(oldText, newText);
                    }
                    res.SetString(text);
                    return res;
                }
                else if (funcName == "EXACT" && args.Length >= 2)
                {
                    string s1 = GetStringValue(args[0], sheet);
                    string s2 = GetStringValue(args[1], sheet);
                    bool eq = string.Equals(s1, s2, StringComparison.Ordinal);
                    res.SetString(eq ? "TRUE" : "FALSE");
                    res.NumericValue = eq ? 1 : 0;
                    return res;
                }
                else if (funcName == "DATE" && args.Length >= 3)
                {
                    int y = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    int m = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    int d = (int)ParseAndEvaluateNumeric(args[2], sheet);
                    try
                    {
                        DateTime dt = new DateTime(y, m, d);
                        res.SetNumber(dt.ToOADate());
                        res.EvaluatedStr = dt.ToString("dd/MM/yyyy");
                    }
                    catch
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }
                else if (funcName == "TIME" && args.Length >= 3)
                {
                    int h = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    int m = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    int s = (int)ParseAndEvaluateNumeric(args[2], sheet);
                    double timeFrac = (h * 3600 + m * 60 + s) / 86400.0;
                    res.SetNumber(timeFrac);
                    res.EvaluatedStr = string.Format("{0:D2}:{1:D2}:{2:D2}", h % 24, m % 60, s % 60);
                    return res;
                }

                // Text Functions
                else if (funcName == "CONCATENATE" || funcName == "CONCAT")
                {
                    StringBuilder concatSb = new StringBuilder();
                    foreach (var arg in args)
                    {
                        string cleanArg = arg.Trim();
                        if (cleanArg.StartsWith("\"") && cleanArg.EndsWith("\"") && cleanArg.Length >= 2)
                        {
                            concatSb.Append(cleanArg.Substring(1, cleanArg.Length - 2));
                        }
                        else
                        {
                            var range = ParseRange(cleanArg);
                            foreach (var p in range)
                            {
                                CellData c = sheet.GetCell(p.Item1, p.Item2);
                                if (c != null) concatSb.Append(!string.IsNullOrEmpty(c.EvaluatedStr) ? c.EvaluatedStr : c.RawValue);
                            }
                        }
                    }
                    res.SetString(concatSb.ToString());
                    return res;
                }
                else if (funcName == "LEFT" || funcName == "RIGHT")
                {
                    string target = args.Length > 0 ? GetStringValue(args[0], sheet) : "";
                    int count = args.Length > 1 ? (int)ParseAndEvaluateNumeric(args[1], sheet) : 1;
                    count = Math.Max(0, Math.Min(target.Length, count));
                    string sub = (funcName == "LEFT") ? target.Substring(0, count) : target.Substring(target.Length - count);
                    res.SetString(sub);
                    return res;
                }
                else if (funcName == "MID")
                {
                    string target = args.Length > 0 ? GetStringValue(args[0], sheet) : "";
                    int start = args.Length > 1 ? (int)ParseAndEvaluateNumeric(args[1], sheet) - 1 : 0;
                    int length = args.Length > 2 ? (int)ParseAndEvaluateNumeric(args[2], sheet) : target.Length;
                    start = Math.Max(0, Math.Min(target.Length, start));
                    length = Math.Max(0, Math.Min(target.Length - start, length));
                    res.SetString(target.Substring(start, length));
                    return res;
                }
                else if (funcName == "LEN")
                {
                    string target = args.Length > 0 ? GetStringValue(args[0], sheet) : "";
                    res.SetNumber(target.Length);
                    res.EvaluatedStr = target.Length.ToString();
                    return res;
                }
                else if (funcName == "UPPER")
                {
                    res.SetString(args.Length > 0 ? GetStringValue(args[0], sheet).ToUpper() : "");
                    return res;
                }
                else if (funcName == "LOWER")
                {
                    res.SetString(args.Length > 0 ? GetStringValue(args[0], sheet).ToLower() : "");
                    return res;
                }
                else if (funcName == "TRIM")
                {
                    res.SetString(args.Length > 0 ? GetStringValue(args[0], sheet).Trim() : "");
                    return res;
                }

                // Date & Time Functions
                else if (funcName == "TODAY")
                {
                    DateTime now = DateTime.Today;
                    res.SetNumber(now.ToOADate());
                    res.EvaluatedStr = now.ToString("dd/MM/yyyy");
                    return res;
                }
                else if (funcName == "NOW")
                {
                    DateTime now = DateTime.Now;
                    res.SetNumber(now.ToOADate());
                    res.EvaluatedStr = now.ToString("dd/MM/yyyy HH:mm");
                    return res;
                }

                // Lookup Functions: VLOOKUP(lookup_val, table_range, col_index, [exact])
                else if (funcName == "VLOOKUP" && args.Length >= 3)
                {
                    string lookupKey = GetStringValue(args[0], sheet).Trim().ToUpper();
                    double lookupNum;
                    bool isLookupNum = double.TryParse(lookupKey, NumberStyles.Any, CultureInfo.InvariantCulture, out lookupNum);
                    SparseMatrix sTable = ResolveSheet(args[1], sheet);
                    var range = ParseRange(args[1]);
                    int targetColOffset = (int)ParseAndEvaluateNumeric(args[2], sheet) - 1;
                    bool approxMatch = args.Length >= 4 && (args[3].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase) || args[3].Trim() == "1");

                    if (range.Count > 0)
                    {
                        uint minR = range[0].Item1;
                        uint maxR = range[range.Count - 1].Item1;
                        uint minC = range[0].Item2;
                        uint bestApproxRow = 0;
                        double bestApproxVal = double.MinValue;

                        for (uint r = minR; r <= maxR; r++)
                        {
                            CellData keyCell = sTable.GetCell(r, minC);
                            string kVal = keyCell != null ? (!string.IsNullOrEmpty(keyCell.EvaluatedStr) ? keyCell.EvaluatedStr : keyCell.RawValue ?? "").Trim().ToUpper() : "";

                            if (kVal == lookupKey || (isLookupNum && keyCell != null && keyCell.Type != CellType.Empty && Math.Abs(keyCell.NumericValue - lookupNum) < 1e-9))
                            {
                                CellData matchCell = sTable.GetCell(r, (uint)(minC + targetColOffset));
                                if (matchCell != null)
                                {
                                    res.NumericValue = matchCell.NumericValue;
                                    res.EvaluatedStr = matchCell.EvaluatedStr ?? matchCell.RawValue;
                                    res.RawValue = matchCell.RawValue;
                                    res.Type = matchCell.Type;
                                    return res;
                                }
                            }
                            else if (approxMatch && isLookupNum && keyCell != null && keyCell.Type != CellType.Empty)
                            {
                                if (keyCell.NumericValue <= lookupNum && keyCell.NumericValue >= bestApproxVal)
                                {
                                    bestApproxVal = keyCell.NumericValue;
                                    bestApproxRow = r;
                                }
                            }
                        }

                        if (approxMatch && bestApproxRow > 0)
                        {
                            CellData matchCell = sTable.GetCell(bestApproxRow, (uint)(minC + targetColOffset));
                            if (matchCell != null)
                            {
                                res.NumericValue = matchCell.NumericValue;
                                res.EvaluatedStr = matchCell.EvaluatedStr ?? matchCell.RawValue;
                                res.RawValue = matchCell.RawValue;
                                res.Type = matchCell.Type;
                                return res;
                            }
                        }
                    }
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "HLOOKUP" && args.Length >= 3)
                {
                    string lookupKey = GetStringValue(args[0], sheet).Trim().ToUpper();
                    SparseMatrix sTable = ResolveSheet(args[1], sheet);
                    var range = ParseRange(args[1]);
                    int targetRowOffset = (int)ParseAndEvaluateNumeric(args[2], sheet) - 1;

                    if (range.Count > 0)
                    {
                        uint minR = range[0].Item1;
                        uint minC = range[0].Item2;
                        uint maxC = range[range.Count - 1].Item2;

                        for (uint c = minC; c <= maxC; c++)
                        {
                            CellData keyCell = sTable.GetCell(minR, c);
                            string kVal = keyCell != null ? (!string.IsNullOrEmpty(keyCell.EvaluatedStr) ? keyCell.EvaluatedStr : keyCell.RawValue ?? "").Trim().ToUpper() : "";

                            if (kVal == lookupKey)
                            {
                                CellData matchCell = sTable.GetCell((uint)(minR + targetRowOffset), c);
                                if (matchCell != null)
                                {
                                    res.NumericValue = matchCell.NumericValue;
                                    res.EvaluatedStr = matchCell.EvaluatedStr ?? matchCell.RawValue;
                                    res.RawValue = matchCell.RawValue;
                                    res.Type = matchCell.Type;
                                    return res;
                                }
                            }
                        }
                    }
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "XLOOKUP" && args.Length >= 3)
                {
                    string lookupKey = GetStringValue(args[0], sheet).Trim().ToUpper();
                    SparseMatrix sLookup = ResolveSheet(args[1], sheet);
                    SparseMatrix sRet = ResolveSheet(args[2], sheet);
                    var lookupRange = ParseRange(args[1]);
                    var returnRange = ParseRange(args[2]);

                    int n = Math.Min(lookupRange.Count, returnRange.Count);
                    for (int i = 0; i < n; i++)
                    {
                        CellData keyCell = sLookup.GetCell(lookupRange[i].Item1, lookupRange[i].Item2);
                        string kVal = keyCell != null ? (!string.IsNullOrEmpty(keyCell.EvaluatedStr) ? keyCell.EvaluatedStr : keyCell.RawValue ?? "").Trim().ToUpper() : "";

                        if (kVal == lookupKey)
                        {
                            CellData retCell = sRet.GetCell(returnRange[i].Item1, returnRange[i].Item2);
                            if (retCell != null)
                            {
                                res.NumericValue = retCell.NumericValue;
                                res.EvaluatedStr = retCell.EvaluatedStr ?? retCell.RawValue;
                                res.RawValue = retCell.RawValue;
                                res.Type = retCell.Type;
                                return res;
                            }
                        }
                    }
                    if (args.Length >= 4)
                    {
                        SetResultFromToken(res, args[3], sheet);
                        return res;
                    }
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "INDEX" && args.Length >= 2)
                {
                    SparseMatrix sRange = ResolveSheet(args[0], sheet);
                    var range = ParseRange(args[0]);
                    int rowIdx = (int)ParseAndEvaluateNumeric(args[1], sheet) - 1;
                    int colIdx = args.Length >= 3 ? (int)ParseAndEvaluateNumeric(args[2], sheet) - 1 : 0;

                    if (range.Count > 0)
                    {
                        uint minR = range[0].Item1;
                        uint minC = range[0].Item2;
                        CellData cell = sRange.GetCell((uint)(minR + Math.Max(0, rowIdx)), (uint)(minC + Math.Max(0, colIdx)));
                        if (cell != null)
                        {
                            res.NumericValue = cell.NumericValue;
                            res.EvaluatedStr = cell.EvaluatedStr ?? cell.RawValue;
                            res.RawValue = cell.RawValue;
                            res.Type = cell.Type;
                            return res;
                        }
                    }
                    res.SetString("#REF!");
                    return res;
                }
                else if (funcName == "MATCH" && args.Length >= 2)
                {
                    string lookupKey = GetStringValue(args[0], sheet).Trim().ToUpper();
                    SparseMatrix sRange = ResolveSheet(args[1], sheet);
                    var range = ParseRange(args[1]);

                    for (int i = 0; i < range.Count; i++)
                    {
                        CellData c = sRange.GetCell(range[i].Item1, range[i].Item2);
                        string val = c != null ? (!string.IsNullOrEmpty(c.EvaluatedStr) ? c.EvaluatedStr : c.RawValue ?? "").Trim().ToUpper() : "";
                        if (val == lookupKey)
                        {
                            res.SetNumber(i + 1);
                            res.EvaluatedStr = (i + 1).ToString();
                            return res;
                        }
                    }
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "OFFSET" && args.Length >= 3)
                {
                    SparseMatrix sBase = ResolveSheet(args[0], sheet);
                    var baseRange = ParseRange(args[0]);
                    if (baseRange.Count > 0)
                    {
                        int rOffset = (int)ParseAndEvaluateNumeric(args[1], sheet);
                        int cOffset = (int)ParseAndEvaluateNumeric(args[2], sheet);
                        uint targetR = (uint)Math.Max(1, (int)baseRange[0].Item1 + rOffset);
                        uint targetC = (uint)Math.Max(1, (int)baseRange[0].Item2 + cOffset);
                        CellData c = sBase.GetCell(targetR, targetC);
                        if (c != null)
                        {
                            res.NumericValue = c.NumericValue;
                            res.EvaluatedStr = c.EvaluatedStr ?? c.RawValue;
                            res.RawValue = c.RawValue;
                            res.Type = c.Type;
                            return res;
                        }
                    }
                    res.SetNumber(0);
                    return res;
                }
                else if (funcName == "INDIRECT" && args.Length >= 1)
                {
                    string refText = GetStringValue(args[0], sheet);
                    if (IsCellReference(refText))
                    {
                        SparseMatrix sRef = ResolveSheet(refText, sheet);
                        var pos = SparseMatrix.ParseCellAddress(refText);
                        CellData c = sRef.GetCell(pos.Item1, pos.Item2);
                        if (c != null)
                        {
                            res.NumericValue = c.NumericValue;
                            res.EvaluatedStr = c.EvaluatedStr ?? c.RawValue;
                            res.RawValue = c.RawValue;
                            res.Type = c.Type;
                            return res;
                        }
                    }
                    res.SetString("#REF!");
                    return res;
                }
                else if (funcName == "IFS")
                {
                    for (int i = 0; i < args.Length - 1; i += 2)
                    {
                        if (EvaluateCondition(args[i], sheet))
                        {
                            SetResultFromToken(res, args[i + 1], sheet);
                            return res;
                        }
                    }
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "XOR")
                {
                    int trueCount = 0;
                    foreach (var a in args)
                    {
                        if (EvaluateCondition(a, sheet)) trueCount++;
                    }
                    bool xorRes = (trueCount % 2 == 1);
                    res.SetString(xorRes ? "TRUE" : "FALSE");
                    res.NumericValue = xorRes ? 1 : 0;
                    return res;
                }
                else if (funcName == "SWITCH" && args.Length >= 3)
                {
                    string targetVal = GetStringValue(args[0], sheet).Trim().ToUpper();
                    for (int i = 1; i < args.Length - 1; i += 2)
                    {
                        string matchVal = GetStringValue(args[i], sheet).Trim().ToUpper();
                        if (targetVal == matchVal)
                        {
                            SetResultFromToken(res, args[i + 1], sheet);
                            return res;
                        }
                    }
                    if (args.Length % 2 == 0)
                    {
                        SetResultFromToken(res, args[args.Length - 1], sheet);
                        return res;
                    }
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "IFERROR" && args.Length >= 2)
                {
                    CellData evalFirst = EvaluateFormula("=" + args[0], sheet);
                    if (evalFirst.EvaluatedStr != null && (evalFirst.EvaluatedStr.StartsWith("#") || evalFirst.EvaluatedStr == "#N/A" || evalFirst.EvaluatedStr == "#VALUE!" || evalFirst.EvaluatedStr == "#REF!"))
                    {
                        SetResultFromToken(res, args[1], sheet);
                    }
                    else
                    {
                        res.Type = evalFirst.Type;
                        res.NumericValue = evalFirst.NumericValue;
                        res.EvaluatedStr = evalFirst.EvaluatedStr;
                        res.RawValue = evalFirst.RawValue;
                    }
                    return res;
                }
                else if (funcName == "TEXTJOIN" && args.Length >= 3)
                {
                    string delimiter = GetStringValue(args[0], sheet);
                    bool ignoreEmpty = args[1].Trim().ToUpper() != "FALSE" && args[1].Trim() != "0";
                    List<string> items = new List<string>();

                    for (int i = 2; i < args.Length; i++)
                    {
                        string cleanArg = args[i].Trim();
                        var range = ParseRange(cleanArg);
                        if (range.Count > 0)
                        {
                            foreach (var p in range)
                            {
                                CellData c = sheet.GetCell(p.Item1, p.Item2);
                                string sVal = (c != null) ? (!string.IsNullOrEmpty(c.EvaluatedStr) ? c.EvaluatedStr : c.RawValue) : "";
                                if (!ignoreEmpty || !string.IsNullOrEmpty(sVal)) items.Add(sVal);
                            }
                        }
                        else
                        {
                            string sVal = GetStringValue(cleanArg, sheet);
                            if (!ignoreEmpty || !string.IsNullOrEmpty(sVal)) items.Add(sVal);
                        }
                    }
                    res.SetString(string.Join(delimiter, items.ToArray()));
                    return res;
                }
                else if (funcName == "TEXT" && args.Length >= 2)
                {
                    double numVal = ParseAndEvaluateNumeric(args[0], sheet);
                    string fmt = GetStringValue(args[1], sheet);
                    try { res.SetString(numVal.ToString(fmt, CultureInfo.InvariantCulture)); }
                    catch { res.SetString(numVal.ToString(CultureInfo.InvariantCulture)); }
                    return res;
                }
                else if (funcName == "DATE" && args.Length >= 3)
                {
                    int y = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    int m = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    int d = (int)ParseAndEvaluateNumeric(args[2], sheet);
                    DateTime dt = new DateTime(Math.Max(1900, Math.Min(9999, y)), Math.Max(1, Math.Min(12, m)), Math.Max(1, Math.Min(31, d)));
                    res.SetNumber(dt.ToOADate());
                    res.EvaluatedStr = dt.ToString("dd/MM/yyyy");
                    return res;
                }
                else if (funcName == "DAY" && args.Length >= 1)
                {
                    double dVal = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(dVal);
                    res.SetNumber(dt.Day);
                    res.EvaluatedStr = dt.Day.ToString();
                    return res;
                }
                else if (funcName == "MONTH" && args.Length >= 1)
                {
                    double dVal = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(dVal);
                    res.SetNumber(dt.Month);
                    res.EvaluatedStr = dt.Month.ToString();
                    return res;
                }
                else if (funcName == "YEAR" && args.Length >= 1)
                {
                    double dVal = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(dVal);
                    res.SetNumber(dt.Year);
                    res.EvaluatedStr = dt.Year.ToString();
                    return res;
                }
                else if (funcName == "HOUR" && args.Length >= 1)
                {
                    double dVal = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(dVal);
                    res.SetNumber(dt.Hour);
                    res.EvaluatedStr = dt.Hour.ToString();
                    return res;
                }
                else if (funcName == "MINUTE" && args.Length >= 1)
                {
                    double dVal = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(dVal);
                    res.SetNumber(dt.Minute);
                    res.EvaluatedStr = dt.Minute.ToString();
                    return res;
                }
                else if (funcName == "SECOND" && args.Length >= 1)
                {
                    double dVal = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(dVal);
                    res.SetNumber(dt.Second);
                    res.EvaluatedStr = dt.Second.ToString();
                    return res;
                }
                else if (funcName == "ROW")
                {
                    if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                    {
                        var range = ParseRange(args[0]);
                        if (range.Count > 0)
                        {
                            res.SetNumber((double)range[0].Item1);
                            res.EvaluatedStr = range[0].Item1.ToString();
                            return res;
                        }
                    }
                    res.SetNumber(1.0);
                    res.EvaluatedStr = "1";
                    return res;
                }
                else if (funcName == "COLUMN")
                {
                    if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                    {
                        var range = ParseRange(args[0]);
                        if (range.Count > 0)
                        {
                            res.SetNumber((double)range[0].Item2);
                            res.EvaluatedStr = range[0].Item2.ToString();
                            return res;
                        }
                    }
                    res.SetNumber(1.0);
                    res.EvaluatedStr = "1";
                    return res;
                }
                else if (funcName == "IFNA" && args.Length >= 2)
                {
                    CellData valCell = GetCellReferenceOrData(args[0], sheet);
                    string errStr = valCell != null ? (valCell.EvaluatedStr ?? valCell.RawValue ?? "") : "";
                    if (errStr == "#N/A")
                    {
                        SetResultFromToken(res, args[1], sheet);
                    }
                    else
                    {
                        if (valCell != null)
                        {
                            res.NumericValue = valCell.NumericValue;
                            res.EvaluatedStr = valCell.EvaluatedStr ?? valCell.RawValue;
                            res.RawValue = valCell.RawValue;
                            res.Type = valCell.Type;
                        }
                    }
                    return res;
                }
                else if (funcName == "WEEKDAY" && args.Length >= 1)
                {
                    double oa = ParseAndEvaluateNumeric(args[0], sheet);
                    DateTime dt = DateTime.FromOADate(oa);
                    int returnType = args.Length >= 2 ? (int)ParseAndEvaluateNumeric(args[1], sheet) : 1;
                    int dayOfWeek;
                    if (returnType == 2)
                    {
                        dayOfWeek = (dt.DayOfWeek == DayOfWeek.Sunday) ? 7 : (int)dt.DayOfWeek;
                    }
                    else
                    {
                        dayOfWeek = (int)dt.DayOfWeek + 1;
                    }
                    res.SetNumber((double)dayOfWeek);
                    res.EvaluatedStr = dayOfWeek.ToString();
                    return res;
                }
                else if (funcName == "EDATE" && args.Length >= 2)
                {
                    double oa = ParseAndEvaluateNumeric(args[0], sheet);
                    int months = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    DateTime dt = DateTime.FromOADate(oa);
                    DateTime target = dt.AddMonths(months);
                    res.SetNumber(target.ToOADate());
                    res.EvaluatedStr = target.ToString("dd/MM/yyyy");
                    return res;
                }
                else if (funcName == "NETWORKDAYS" && args.Length >= 2)
                {
                    DateTime d1 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    DateTime d2 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[1], sheet));
                    if (d1 > d2) { DateTime tmp = d1; d1 = d2; d2 = tmp; }
                    int businessDays = 0;
                    for (DateTime cur = d1.Date; cur <= d2.Date; cur = cur.AddDays(1))
                    {
                        if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday)
                            businessDays++;
                    }
                    res.SetNumber((double)businessDays);
                    res.EvaluatedStr = businessDays.ToString();
                    return res;
                }
                else if (funcName == "DATEDIF" && args.Length >= 3)
                {
                    DateTime d1 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    DateTime d2 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[1], sheet));
                    string unit = GetStringValue(args[2], sheet).Trim().ToUpper();
                    if (d1 > d2)
                    {
                        res.SetString("#NUM!");
                        return res;
                    }
                    double diff = 0;
                    if (unit == "Y")
                    {
                        diff = d2.Year - d1.Year;
                        if (d2 < d1.AddYears((int)diff)) diff--;
                    }
                    else if (unit == "M")
                    {
                        diff = (d2.Year - d1.Year) * 12 + d2.Month - d1.Month;
                        if (d2.Day < d1.Day) diff--;
                    }
                    else
                    {
                        diff = (d2.Date - d1.Date).TotalDays;
                    }
                    res.SetNumber(diff);
                    res.EvaluatedStr = diff.ToString();
                    return res;
                }
                else if (funcName == "VALUE" && args.Length >= 1)
                {
                    string str = GetStringValue(args[0], sheet).Trim();
                    double v;
                    if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ||
                        double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
                    {
                        res.SetNumber(v);
                        res.EvaluatedStr = v.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }
                else if (funcName == "FIND" && args.Length >= 2)
                {
                    string findWhat = GetStringValue(args[0], sheet);
                    string withinText = GetStringValue(args[1], sheet);
                    int startIdx = args.Length >= 3 ? Math.Max(0, (int)ParseAndEvaluateNumeric(args[2], sheet) - 1) : 0;
                    int idx = withinText.IndexOf(findWhat, startIdx, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        res.SetNumber((double)(idx + 1));
                        res.EvaluatedStr = (idx + 1).ToString();
                    }
                    else
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }
                else if (funcName == "SEARCH" && args.Length >= 2)
                {
                    string findWhat = GetStringValue(args[0], sheet);
                    string withinText = GetStringValue(args[1], sheet);
                    int startIdx = args.Length >= 3 ? Math.Max(0, (int)ParseAndEvaluateNumeric(args[2], sheet) - 1) : 0;
                    int idx = withinText.IndexOf(findWhat, startIdx, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        res.SetNumber((double)(idx + 1));
                        res.EvaluatedStr = (idx + 1).ToString();
                    }
                    else
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }
                else if (funcName == "TEXTBEFORE" && args.Length >= 2)
                {
                    string text = GetStringValue(args[0], sheet);
                    string delim = GetStringValue(args[1], sheet);
                    int idx = text.IndexOf(delim, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        res.SetString(text.Substring(0, idx));
                    }
                    else
                    {
                        res.SetString("#N/A");
                    }
                    return res;
                }
                else if (funcName == "TEXTAFTER" && args.Length >= 2)
                {
                    string text = GetStringValue(args[0], sheet);
                    string delim = GetStringValue(args[1], sheet);
                    int idx = text.IndexOf(delim, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        res.SetString(text.Substring(idx + delim.Length));
                    }
                    else
                    {
                        res.SetString("#N/A");
                    }
                    return res;
                }
                else if (funcName == "CHAR" && args.Length >= 1)
                {
                    int code = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    if (code >= 1 && code <= 255)
                    {
                        res.SetString(((char)code).ToString());
                    }
                    else
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }
                else if (funcName == "CODE" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet);
                    if (!string.IsNullOrEmpty(s))
                    {
                        res.SetNumber((double)(int)s[0]);
                        res.EvaluatedStr = ((int)s[0]).ToString();
                    }
                    else
                    {
                        res.SetString("#VALUE!");
                    }
                    return res;
                }
                else if (funcName == "REPT" && args.Length >= 2)
                {
                    string s = GetStringValue(args[0], sheet);
                    double cnt = ParseAndEvaluateNumeric(args[1], sheet);
                    if (cnt < 0)
                    {
                        res.SetString("#VALUE!");
                        return res;
                    }
                    int rep = (int)cnt;
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < rep; i++) sb.Append(s);
                    res.SetString(sb.ToString());
                    return res;
                }
                else if (funcName == "RANDBETWEEN" && args.Length >= 2)
                {
                    double btm = ParseAndEvaluateNumeric(args[0], sheet);
                    double top = ParseAndEvaluateNumeric(args[1], sheet);
                    if (btm > top)
                    {
                        res.SetString("#NUM!");
                        return res;
                    }
                    Random rnd = new Random();
                    int rVal = rnd.Next((int)Math.Floor(btm), (int)Math.Floor(top) + 1);
                    res.SetNumber((double)rVal);
                    res.EvaluatedStr = rVal.ToString();
                    return res;
                }
                else if (funcName == "CEILING" && args.Length >= 2)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    double sig = ParseAndEvaluateNumeric(args[1], sheet);
                    if (sig == 0) { res.SetNumber(0); return res; }
                    double ceil = Math.Ceiling(num / sig) * sig;
                    res.SetNumber(ceil);
                    res.EvaluatedStr = ceil.ToString(CultureInfo.InvariantCulture);
                    return res;
                }
                else if (funcName == "FLOOR" && args.Length >= 2)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    double sig = ParseAndEvaluateNumeric(args[1], sheet);
                    if (sig == 0) { res.SetNumber(0); return res; }
                    double flr = Math.Floor(num / sig) * sig;
                    res.SetNumber(flr);
                    res.EvaluatedStr = flr.ToString(CultureInfo.InvariantCulture);
                    return res;
                }
                else if (funcName == "SIGN" && args.Length >= 1)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    double s = num > 0 ? 1.0 : (num < 0 ? -1.0 : 0.0);
                    res.SetNumber(s);
                    res.EvaluatedStr = s.ToString();
                    return res;
                }
                else if (funcName == "STDEV" || funcName == "STDEV.S")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count <= 1) { res.SetString("#DIV/0!"); return res; }
                    double avg = 0;
                    foreach (var v in vals) avg += v;
                    avg /= vals.Count;
                    double sumSq = 0;
                    foreach (var v in vals) sumSq += (v - avg) * (v - avg);
                    double stdev = Math.Sqrt(sumSq / (vals.Count - 1));
                    res.SetNumber(stdev);
                    res.EvaluatedStr = stdev.ToString("F3", CultureInfo.InvariantCulture);
                    return res;
                }
                else if (funcName == "VAR" || funcName == "VAR.S")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count <= 1) { res.SetString("#DIV/0!"); return res; }
                    double avg = 0;
                    foreach (var v in vals) avg += v;
                    avg /= vals.Count;
                    double sumSq = 0;
                    foreach (var v in vals) sumSq += (v - avg) * (v - avg);
                    double variance = sumSq / (vals.Count - 1);
                    res.SetNumber(variance);
                    res.EvaluatedStr = variance.ToString("F3", CultureInfo.InvariantCulture);
                    return res;
                }
                else if (funcName == "RANK" && args.Length >= 2)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    var range = ParseRange(args[1]);
                    int order = args.Length >= 3 ? (int)ParseAndEvaluateNumeric(args[2], sheet) : 0;
                    List<double> vals = new List<double>();
                    foreach (var p in range)
                    {
                        CellData c = sheet.GetCell(p.Item1, p.Item2);
                        if (c != null && c.Type == CellType.Number) vals.Add(c.NumericValue);
                    }
                    if (order == 0) vals.Sort((a, b) => b.CompareTo(a));
                    else vals.Sort();
                    int rank = 1;
                    for (int i = 0; i < vals.Count; i++)
                    {
                        if (Math.Abs(vals[i] - num) < 0.000001) { rank = i + 1; break; }
                    }
                    res.SetNumber((double)rank);
                    res.EvaluatedStr = rank.ToString();
                    return res;
                }
                else if (funcName == "MODE" || funcName == "MODE.SNGL")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    Dictionary<double, int> counts = new Dictionary<double, int>();
                    foreach (var v in vals)
                    {
                        if (!counts.ContainsKey(v)) counts[v] = 0;
                        counts[v]++;
                    }
                    double bestVal = 0;
                    int maxCount = 1;
                    foreach (var kvp in counts)
                    {
                        if (kvp.Value > maxCount) { maxCount = kvp.Value; bestVal = kvp.Key; }
                    }
                    if (maxCount <= 1) { res.SetString("#N/A"); return res; }
                    res.SetNumber(bestVal);
                    res.EvaluatedStr = bestVal.ToString();
                    return res;
                }
                else if (funcName == "ISNUMBER" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    bool isNum = (c != null && c.Type == CellType.Number);
                    res.SetNumber(isNum ? 1.0 : 0.0);
                    res.EvaluatedStr = isNum ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISTEXT" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    bool isTxt = (c != null && c.Type == CellType.String);
                    res.SetNumber(isTxt ? 1.0 : 0.0);
                    res.EvaluatedStr = isTxt ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISBLANK" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    bool isBlank = (c == null || c.Type == CellType.Empty || (string.IsNullOrEmpty(c.RawValue) && c.NumericValue == 0 && string.IsNullOrEmpty(c.EvaluatedStr)));
                    res.SetNumber(isBlank ? 1.0 : 0.0);
                    res.EvaluatedStr = isBlank ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISERROR" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    string val = c != null ? (c.EvaluatedStr ?? c.RawValue ?? "") : "";
                    bool isErr = val.StartsWith("#") && val.EndsWith("!");
                    res.SetNumber(isErr ? 1.0 : 0.0);
                    res.EvaluatedStr = isErr ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISNA" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    string val = c != null ? (c.EvaluatedStr ?? c.RawValue ?? "") : "";
                    bool isNa = val == "#N/A";
                    res.SetNumber(isNa ? 1.0 : 0.0);
                    res.EvaluatedStr = isNa ? "TRUE" : "FALSE";
                    return res;
                }
                // --- NEW STATISTICAL & CONDITIONAL AGGREGATION FUNCTIONS ---
                else if (funcName == "COUNTBLANK" && args.Length >= 1)
                {
                    int blankCount = 0;
                    foreach (var arg in args)
                    {
                        SparseMatrix sRange = ResolveSheet(arg, sheet);
                        var range = ParseRange(arg);
                        foreach (var p in range)
                        {
                            CellData c = sRange.GetCell(p.Item1, p.Item2);
                            if (c == null || c.Type == CellType.Empty || (string.IsNullOrEmpty(c.RawValue) && string.IsNullOrEmpty(c.EvaluatedStr)))
                                blankCount++;
                        }
                    }
                    res.SetNumber(blankCount);
                    res.EvaluatedStr = blankCount.ToString();
                    return res;
                }
                else if (funcName == "COUNTIFS" && args.Length >= 2)
                {
                    SparseMatrix sFirst = ResolveSheet(args[0], sheet);
                    var firstRange = ParseRange(args[0]);
                    int n = firstRange.Count;
                    int matchCount = 0;
                    for (int idx = 0; idx < n; idx++)
                    {
                        bool allMatch = true;
                        for (int pair = 0; pair + 1 < args.Length; pair += 2)
                        {
                            SparseMatrix sR = ResolveSheet(args[pair], sheet);
                            var rList = ParseRange(args[pair]);
                            if (idx >= rList.Count) { allMatch = false; break; }
                            CellData c = sR.GetCell(rList[idx].Item1, rList[idx].Item2);
                            if (!MatchesCriteria(c, args[pair + 1], sheet)) { allMatch = false; break; }
                        }
                        if (allMatch) matchCount++;
                    }
                    res.SetNumber(matchCount);
                    res.EvaluatedStr = matchCount.ToString();
                    return res;
                }
                else if (funcName == "SUMIFS" && args.Length >= 3)
                {
                    SparseMatrix sSum = ResolveSheet(args[0], sheet);
                    var sumRange = ParseRange(args[0]);
                    int n = sumRange.Count;
                    double total = 0.0;
                    for (int idx = 0; idx < n; idx++)
                    {
                        bool allMatch = true;
                        for (int pair = 1; pair + 1 < args.Length; pair += 2)
                        {
                            SparseMatrix sR = ResolveSheet(args[pair], sheet);
                            var rList = ParseRange(args[pair]);
                            if (idx >= rList.Count) { allMatch = false; break; }
                            CellData c = sR.GetCell(rList[idx].Item1, rList[idx].Item2);
                            if (!MatchesCriteria(c, args[pair + 1], sheet)) { allMatch = false; break; }
                        }
                        if (allMatch)
                        {
                            CellData cSum = sSum.GetCell(sumRange[idx].Item1, sumRange[idx].Item2);
                            if (cSum != null) total += cSum.NumericValue;
                        }
                    }
                    res.SetNumber(total);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "AVERAGEIFS" && args.Length >= 3)
                {
                    SparseMatrix sAvg = ResolveSheet(args[0], sheet);
                    var avgRange = ParseRange(args[0]);
                    int n = avgRange.Count;
                    double total = 0.0;
                    int matchCount = 0;
                    for (int idx = 0; idx < n; idx++)
                    {
                        bool allMatch = true;
                        for (int pair = 1; pair + 1 < args.Length; pair += 2)
                        {
                            SparseMatrix sR = ResolveSheet(args[pair], sheet);
                            var rList = ParseRange(args[pair]);
                            if (idx >= rList.Count) { allMatch = false; break; }
                            CellData c = sR.GetCell(rList[idx].Item1, rList[idx].Item2);
                            if (!MatchesCriteria(c, args[pair + 1], sheet)) { allMatch = false; break; }
                        }
                        if (allMatch)
                        {
                            CellData cAvg = sAvg.GetCell(avgRange[idx].Item1, avgRange[idx].Item2);
                            if (cAvg != null && cAvg.Type != CellType.Empty)
                            {
                                total += cAvg.NumericValue;
                                matchCount++;
                            }
                        }
                    }
                    if (matchCount > 0)
                    {
                        res.SetNumber(total / matchCount);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else
                    {
                        res.SetString("#DIV/0!");
                    }
                    return res;
                }
                else if ((funcName == "MAXIFS" || funcName == "MINIFS") && args.Length >= 3)
                {
                    SparseMatrix sTarget = ResolveSheet(args[0], sheet);
                    var targetRange = ParseRange(args[0]);
                    int n = targetRange.Count;
                    bool isMax = (funcName == "MAXIFS");
                    double best = isMax ? double.MinValue : double.MaxValue;
                    bool found = false;
                    for (int idx = 0; idx < n; idx++)
                    {
                        bool allMatch = true;
                        for (int pair = 1; pair + 1 < args.Length; pair += 2)
                        {
                            SparseMatrix sR = ResolveSheet(args[pair], sheet);
                            var rList = ParseRange(args[pair]);
                            if (idx >= rList.Count) { allMatch = false; break; }
                            CellData c = sR.GetCell(rList[idx].Item1, rList[idx].Item2);
                            if (!MatchesCriteria(c, args[pair + 1], sheet)) { allMatch = false; break; }
                        }
                        if (allMatch)
                        {
                            CellData cVal = sTarget.GetCell(targetRange[idx].Item1, targetRange[idx].Item2);
                            if (cVal != null && cVal.Type != CellType.Empty)
                            {
                                best = isMax ? Math.Max(best, cVal.NumericValue) : Math.Min(best, cVal.NumericValue);
                                found = true;
                            }
                        }
                    }
                    res.SetNumber(found ? best : 0.0);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "SUBTOTAL" || funcName == "AGGREGATE") && args.Length >= 2)
                {
                    int funcNum = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    int startArg = (funcName == "AGGREGATE" && args.Length >= 3) ? 2 : 1;
                    int opCode = funcNum > 100 ? funcNum - 100 : funcNum;
                    string[] subArgs = new string[args.Length - startArg];
                    Array.Copy(args, startArg, subArgs, 0, subArgs.Length);
                    string joined = string.Join(",", subArgs);
                    switch (opCode)
                    {
                        case 1: return EvaluateFormula("=AVERAGE(" + joined + ")", sheet);
                        case 2: return EvaluateFormula("=COUNT(" + joined + ")", sheet);
                        case 3: return EvaluateFormula("=COUNTA(" + joined + ")", sheet);
                        case 4: return EvaluateFormula("=MAX(" + joined + ")", sheet);
                        case 5: return EvaluateFormula("=MIN(" + joined + ")", sheet);
                        case 6: return EvaluateFormula("=PRODUCT(" + joined + ")", sheet);
                        case 7: return EvaluateFormula("=STDEV.S(" + joined + ")", sheet);
                        case 8: return EvaluateFormula("=STDEV.P(" + joined + ")", sheet);
                        case 9: return EvaluateFormula("=SUM(" + joined + ")", sheet);
                        case 10: return EvaluateFormula("=VAR.S(" + joined + ")", sheet);
                        case 11: return EvaluateFormula("=VAR.P(" + joined + ")", sheet);
                        case 12: return EvaluateFormula("=MEDIAN(" + joined + ")", sheet);
                        default: return EvaluateFormula("=SUM(" + joined + ")", sheet);
                    }
                }
                else if (funcName == "STDEV.P" || funcName == "STDEVP")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetString("#DIV/0!"); return res; }
                    double avg = 0;
                    foreach (var v in vals) avg += v;
                    avg /= vals.Count;
                    double sumSq = 0;
                    foreach (var v in vals) sumSq += (v - avg) * (v - avg);
                    double stdev = Math.Sqrt(sumSq / vals.Count);
                    res.SetNumber(stdev);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "VAR.P" || funcName == "VARP")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetString("#DIV/0!"); return res; }
                    double avg = 0;
                    foreach (var v in vals) avg += v;
                    avg /= vals.Count;
                    double sumSq = 0;
                    foreach (var v in vals) sumSq += (v - avg) * (v - avg);
                    double variance = sumSq / vals.Count;
                    res.SetNumber(variance);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "RANK.EQ" || funcName == "RANK.AVG") && args.Length >= 2)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    SparseMatrix sRange = ResolveSheet(args[1], sheet);
                    var range = ParseRange(args[1]);
                    int order = args.Length >= 3 ? (int)ParseAndEvaluateNumeric(args[2], sheet) : 0;
                    List<double> vals = new List<double>();
                    foreach (var p in range)
                    {
                        CellData c = sRange.GetCell(p.Item1, p.Item2);
                        if (c != null && (c.Type == CellType.Number || c.Type == CellType.Formula)) vals.Add(c.NumericValue);
                    }
                    if (order == 0) vals.Sort((a, b) => b.CompareTo(a));
                    else vals.Sort();
                    int firstRank = -1, lastRank = -1;
                    for (int i = 0; i < vals.Count; i++)
                    {
                        if (Math.Abs(vals[i] - num) < 0.000001)
                        {
                            if (firstRank < 0) firstRank = i + 1;
                            lastRank = i + 1;
                        }
                    }
                    if (firstRank < 0) { res.SetString("#N/A"); return res; }
                    double rankVal = (funcName == "RANK.AVG") ? (firstRank + lastRank) / 2.0 : firstRank;
                    res.SetNumber(rankVal);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "PERCENTILE" || funcName == "PERCENTILE.INC" || funcName == "PERCENTILE.EXC") && args.Length >= 2)
                {
                    List<double> vals = CollectNumericValues(new string[] { args[0] }, sheet);
                    double k = ParseAndEvaluateNumeric(args[1], sheet);
                    if (vals.Count == 0 || k < 0 || k > 1) { res.SetString("#NUM!"); return res; }
                    vals.Sort();
                    double pos = k * (vals.Count - 1);
                    int lower = (int)Math.Floor(pos);
                    int upper = (int)Math.Ceiling(pos);
                    double weight = pos - lower;
                    double pVal = vals[lower] + weight * (vals[Math.Min(vals.Count - 1, upper)] - vals[lower]);
                    res.SetNumber(pVal);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "QUARTILE" || funcName == "QUARTILE.INC" || funcName == "QUARTILE.EXC") && args.Length >= 2)
                {
                    int quart = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    if (quart < 0 || quart > 4) { res.SetString("#NUM!"); return res; }
                    return EvaluateFormula("=PERCENTILE(" + args[0] + "," + (quart * 0.25).ToString(CultureInfo.InvariantCulture) + ")", sheet);
                }
                else if (funcName == "GEOMEAN")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetString("#NUM!"); return res; }
                    double logSum = 0;
                    foreach (var v in vals)
                    {
                        if (v <= 0) { res.SetString("#NUM!"); return res; }
                        logSum += Math.Log(v);
                    }
                    res.SetNumber(Math.Exp(logSum / vals.Count));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "HARMEAN")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetString("#NUM!"); return res; }
                    double invSum = 0;
                    foreach (var v in vals)
                    {
                        if (v <= 0) { res.SetString("#NUM!"); return res; }
                        invSum += 1.0 / v;
                    }
                    res.SetNumber(vals.Count / invSum);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "AVEDEV")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetString("#NUM!"); return res; }
                    double avg = 0;
                    foreach (var v in vals) avg += v;
                    avg /= vals.Count;
                    double dev = 0;
                    foreach (var v in vals) dev += Math.Abs(v - avg);
                    res.SetNumber(dev / vals.Count);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "DEVSQ")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetNumber(0); return res; }
                    double avg = 0;
                    foreach (var v in vals) avg += v;
                    avg /= vals.Count;
                    double devSq = 0;
                    foreach (var v in vals) devSq += (v - avg) * (v - avg);
                    res.SetNumber(devSq);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "CORREL" || funcName == "PEARSON" || funcName == "COVARIANCE.P" || funcName == "COVARIANCE.S" || funcName == "SLOPE" || funcName == "INTERCEPT") && args.Length >= 2)
                {
                    List<double> yVals = CollectNumericValues(new string[] { args[0] }, sheet);
                    List<double> xVals = CollectNumericValues(new string[] { args[1] }, sheet);
                    int n = Math.Min(xVals.Count, yVals.Count);
                    if (n < 2) { res.SetString("#DIV/0!"); return res; }
                    double avgX = 0, avgY = 0;
                    for (int i = 0; i < n; i++) { avgX += xVals[i]; avgY += yVals[i]; }
                    avgX /= n; avgY /= n;
                    double sumXY = 0, sumX2 = 0, sumY2 = 0;
                    for (int i = 0; i < n; i++)
                    {
                        double dx = xVals[i] - avgX;
                        double dy = yVals[i] - avgY;
                        sumXY += dx * dy;
                        sumX2 += dx * dx;
                        sumY2 += dy * dy;
                    }
                    double outVal = 0;
                    if (funcName == "CORREL" || funcName == "PEARSON")
                        outVal = (sumX2 > 0 && sumY2 > 0) ? sumXY / Math.Sqrt(sumX2 * sumY2) : 0;
                    else if (funcName == "COVARIANCE.P")
                        outVal = sumXY / n;
                    else if (funcName == "COVARIANCE.S")
                        outVal = sumXY / (n - 1);
                    else if (funcName == "SLOPE")
                        outVal = sumX2 > 0 ? sumXY / sumX2 : 0;
                    else if (funcName == "INTERCEPT")
                    {
                        double slope = sumX2 > 0 ? sumXY / sumX2 : 0;
                        outVal = avgY - slope * avgX;
                    }
                    res.SetNumber(outVal);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "FORECAST" || funcName == "FORECAST.LINEAR") && args.Length >= 3)
                {
                    double xTarget = ParseAndEvaluateNumeric(args[0], sheet);
                    List<double> yVals = CollectNumericValues(new string[] { args[1] }, sheet);
                    List<double> xVals = CollectNumericValues(new string[] { args[2] }, sheet);
                    int n = Math.Min(xVals.Count, yVals.Count);
                    if (n < 2) { res.SetString("#DIV/0!"); return res; }
                    double avgX = 0, avgY = 0;
                    for (int i = 0; i < n; i++) { avgX += xVals[i]; avgY += yVals[i]; }
                    avgX /= n; avgY /= n;
                    double sumXY = 0, sumX2 = 0;
                    for (int i = 0; i < n; i++)
                    {
                        double dx = xVals[i] - avgX;
                        sumXY += dx * (yVals[i] - avgY);
                        sumX2 += dx * dx;
                    }
                    double slope = sumX2 > 0 ? sumXY / sumX2 : 0;
                    double intercept = avgY - slope * avgX;
                    res.SetNumber(intercept + slope * xTarget);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }

                // --- MATH & TRIGONOMETRY FUNCTIONS ---
                else if (funcName == "PI")
                {
                    res.SetNumber(Math.PI);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "RAND")
                {
                    double r = new Random(Guid.NewGuid().GetHashCode()).NextDouble();
                    res.SetNumber(r);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "EXP" && args.Length >= 1)
                {
                    res.SetNumber(Math.Exp(ParseAndEvaluateNumeric(args[0], sheet)));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "LN" && args.Length >= 1)
                {
                    double v = ParseAndEvaluateNumeric(args[0], sheet);
                    if (v <= 0) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(Math.Log(v));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "LOG" && args.Length >= 1)
                {
                    double v = ParseAndEvaluateNumeric(args[0], sheet);
                    double b = args.Length >= 2 ? ParseAndEvaluateNumeric(args[1], sheet) : 10.0;
                    if (v <= 0 || b <= 0 || Math.Abs(b - 1.0) < 1e-12) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(Math.Log(v, b));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "LOG10" && args.Length >= 1)
                {
                    double v = ParseAndEvaluateNumeric(args[0], sheet);
                    if (v <= 0) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(Math.Log10(v));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "SIN" || funcName == "COS" || funcName == "TAN" || funcName == "ASIN" || funcName == "ACOS" || funcName == "ATAN" || funcName == "SINH" || funcName == "COSH" || funcName == "TANH" || funcName == "RADIANS" || funcName == "DEGREES") && args.Length >= 1)
                {
                    double v = ParseAndEvaluateNumeric(args[0], sheet);
                    double r = 0;
                    if (funcName == "SIN") r = Math.Sin(v);
                    else if (funcName == "COS") r = Math.Cos(v);
                    else if (funcName == "TAN") r = Math.Tan(v);
                    else if (funcName == "ASIN") r = Math.Asin(v);
                    else if (funcName == "ACOS") r = Math.Acos(v);
                    else if (funcName == "ATAN") r = Math.Atan(v);
                    else if (funcName == "SINH") r = Math.Sinh(v);
                    else if (funcName == "COSH") r = Math.Cosh(v);
                    else if (funcName == "TANH") r = Math.Tanh(v);
                    else if (funcName == "RADIANS") r = v * Math.PI / 180.0;
                    else if (funcName == "DEGREES") r = v * 180.0 / Math.PI;
                    if (double.IsNaN(r) || double.IsInfinity(r)) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(r);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "ATAN2" && args.Length >= 2)
                {
                    double x = ParseAndEvaluateNumeric(args[0], sheet);
                    double y = ParseAndEvaluateNumeric(args[1], sheet);
                    res.SetNumber(Math.Atan2(y, x));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "COT" && args.Length >= 1)
                {
                    double t = Math.Tan(ParseAndEvaluateNumeric(args[0], sheet));
                    if (Math.Abs(t) < 1e-12) { res.SetString("#DIV/0!"); return res; }
                    res.SetNumber(1.0 / t);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "CSC" && args.Length >= 1)
                {
                    double s = Math.Sin(ParseAndEvaluateNumeric(args[0], sheet));
                    if (Math.Abs(s) < 1e-12) { res.SetString("#DIV/0!"); return res; }
                    res.SetNumber(1.0 / s);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "SEC" && args.Length >= 1)
                {
                    double c = Math.Cos(ParseAndEvaluateNumeric(args[0], sheet));
                    if (Math.Abs(c) < 1e-12) { res.SetString("#DIV/0!"); return res; }
                    res.SetNumber(1.0 / c);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "FACT" && args.Length >= 1)
                {
                    int n = (int)Math.Floor(ParseAndEvaluateNumeric(args[0], sheet));
                    if (n < 0 || n > 170) { res.SetString("#NUM!"); return res; }
                    double f = 1.0;
                    for (int i = 2; i <= n; i++) f *= i;
                    res.SetNumber(f);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "COMBIN" || funcName == "PERMUT") && args.Length >= 2)
                {
                    int n = (int)Math.Floor(ParseAndEvaluateNumeric(args[0], sheet));
                    int k = (int)Math.Floor(ParseAndEvaluateNumeric(args[1], sheet));
                    if (n < 0 || k < 0 || k > n) { res.SetString("#NUM!"); return res; }
                    double r = 1.0;
                    for (int i = 1; i <= k; i++)
                    {
                        r *= (n - i + 1);
                        if (funcName == "COMBIN") r /= i;
                    }
                    res.SetNumber(r);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "GCD" || funcName == "LCM")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    if (vals.Count == 0) { res.SetNumber(0); return res; }
                    long acc = (long)Math.Abs(Math.Floor(vals[0]));
                    for (int i = 1; i < vals.Count; i++)
                    {
                        long b = (long)Math.Abs(Math.Floor(vals[i]));
                        long x = acc, y = b;
                        while (y != 0) { long t = y; y = x % y; x = t; }
                        long gcd = x == 0 ? 1 : x;
                        acc = (funcName == "GCD") ? gcd : (acc == 0 || b == 0 ? 0 : (acc / gcd) * b);
                    }
                    res.SetNumber(acc);
                    res.EvaluatedStr = acc.ToString();
                    return res;
                }
                else if (funcName == "QUOTIENT" && args.Length >= 2)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    double den = ParseAndEvaluateNumeric(args[1], sheet);
                    if (Math.Abs(den) < 1e-12) { res.SetString("#DIV/0!"); return res; }
                    res.SetNumber(Math.Truncate(num / den));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "MROUND" && args.Length >= 2)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    double mult = ParseAndEvaluateNumeric(args[1], sheet);
                    if (Math.Abs(mult) < 1e-12) { res.SetNumber(0); return res; }
                    if ((num > 0 && mult < 0) || (num < 0 && mult > 0)) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(Math.Round(num / mult, MidpointRounding.AwayFromZero) * mult);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "EVEN" || funcName == "ODD") && args.Length >= 1)
                {
                    double v = ParseAndEvaluateNumeric(args[0], sheet);
                    double sign = v < 0 ? -1.0 : 1.0;
                    long ceil = (long)Math.Ceiling(Math.Abs(v));
                    if (funcName == "EVEN") { if (ceil % 2 != 0) ceil++; }
                    else { if (ceil % 2 == 0) ceil++; }
                    res.SetNumber(ceil * sign);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "SUMSQ")
                {
                    List<double> vals = CollectNumericValues(args, sheet);
                    double sq = 0;
                    foreach (var v in vals) sq += v * v;
                    res.SetNumber(sq);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "ROMAN" && args.Length >= 1)
                {
                    int n = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    if (n <= 0 || n >= 4000) { res.SetString("#VALUE!"); return res; }
                    int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
                    string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < values.Length; i++)
                    {
                        while (n >= values[i]) { n -= values[i]; sb.Append(numerals[i]); }
                    }
                    res.SetString(sb.ToString());
                    return res;
                }
                else if (funcName == "ARABIC" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet).Trim().ToUpperInvariant();
                    Dictionary<char, int> map = new Dictionary<char, int> { { 'I', 1 }, { 'V', 5 }, { 'X', 10 }, { 'L', 50 }, { 'C', 100 }, { 'D', 500 }, { 'M', 1000 } };
                    int total = 0;
                    for (int i = 0; i < s.Length; i++)
                    {
                        if (!map.ContainsKey(s[i])) { res.SetString("#VALUE!"); return res; }
                        int cur = map[s[i]];
                        int next = (i + 1 < s.Length && map.ContainsKey(s[i + 1])) ? map[s[i + 1]] : 0;
                        if (cur < next) total -= cur;
                        else total += cur;
                    }
                    res.SetNumber(total);
                    res.EvaluatedStr = total.ToString();
                    return res;
                }
                else if (funcName == "BASE" && args.Length >= 2)
                {
                    long num = (long)ParseAndEvaluateNumeric(args[0], sheet);
                    int radix = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    int minLen = args.Length >= 3 ? (int)ParseAndEvaluateNumeric(args[2], sheet) : 0;
                    if (radix < 2 || radix > 36 || num < 0) { res.SetString("#NUM!"); return res; }
                    const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    if (num == 0) { res.SetString("0".PadLeft(Math.Max(1, minLen), '0')); return res; }
                    StringBuilder sb = new StringBuilder();
                    while (num > 0) { sb.Insert(0, chars[(int)(num % radix)]); num /= radix; }
                    res.SetString(sb.ToString().PadLeft(Math.Max(sb.Length, minLen), '0'));
                    return res;
                }
                else if (funcName == "DECIMAL" && args.Length >= 2)
                {
                    string text = GetStringValue(args[0], sheet).Trim().ToUpperInvariant();
                    int radix = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    if (radix < 2 || radix > 36) { res.SetString("#NUM!"); return res; }
                    const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    long val = 0;
                    foreach (char c in text)
                    {
                        int d = chars.IndexOf(c);
                        if (d < 0 || d >= radix) { res.SetString("#NUM!"); return res; }
                        val = val * radix + d;
                    }
                    res.SetNumber(val);
                    res.EvaluatedStr = val.ToString();
                    return res;
                }

                // --- FINANCIAL FUNCTIONS (MS Excel Standard) ---
                else if (funcName == "PMT" && args.Length >= 3)
                {
                    double rate = ParseAndEvaluateNumeric(args[0], sheet);
                    double nper = ParseAndEvaluateNumeric(args[1], sheet);
                    double pv = ParseAndEvaluateNumeric(args[2], sheet);
                    double fv = args.Length >= 4 ? ParseAndEvaluateNumeric(args[3], sheet) : 0.0;
                    double type = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 0.0;
                    if (Math.Abs(nper) < 1e-12) { res.SetString("#NUM!"); return res; }
                    double pmt;
                    if (Math.Abs(rate) < 1e-12) pmt = -(pv + fv) / nper;
                    else
                    {
                        double factor = Math.Pow(1.0 + rate, nper);
                        pmt = -rate * (pv * factor + fv) / ((1.0 + rate * type) * (factor - 1.0));
                    }
                    res.SetNumber(pmt);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "IPMT" || funcName == "PPMT") && args.Length >= 4)
                {
                    double rate = ParseAndEvaluateNumeric(args[0], sheet);
                    double per = ParseAndEvaluateNumeric(args[1], sheet);
                    double nper = ParseAndEvaluateNumeric(args[2], sheet);
                    double pv = ParseAndEvaluateNumeric(args[3], sheet);
                    double fv = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 0.0;
                    double type = args.Length >= 6 ? ParseAndEvaluateNumeric(args[5], sheet) : 0.0;
                    if (per < 1 || per > nper || nper <= 0) { res.SetString("#NUM!"); return res; }
                    double pmt;
                    if (Math.Abs(rate) < 1e-12) pmt = -(pv + fv) / nper;
                    else
                    {
                        double factor = Math.Pow(1.0 + rate, nper);
                        pmt = -rate * (pv * factor + fv) / ((1.0 + rate * type) * (factor - 1.0));
                    }
                    double ipmt;
                    if (per == 1 && type != 0) ipmt = 0.0;
                    else
                    {
                        double n = (type != 0) ? per - 2 : per - 1;
                        double factorN = Math.Pow(1.0 + rate, n);
                        double fvBefore = -(pv * factorN + pmt * (1.0 + rate * type) * ((Math.Abs(rate) < 1e-12) ? n : (factorN - 1.0) / rate));
                        ipmt = fvBefore * rate;
                        if (type != 0) ipmt /= (1.0 + rate);
                    }
                    double result = (funcName == "IPMT") ? ipmt : (pmt - ipmt);
                    res.SetNumber(result);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "FV" && args.Length >= 3)
                {
                    double rate = ParseAndEvaluateNumeric(args[0], sheet);
                    double nper = ParseAndEvaluateNumeric(args[1], sheet);
                    double pmt = ParseAndEvaluateNumeric(args[2], sheet);
                    double pv = args.Length >= 4 ? ParseAndEvaluateNumeric(args[3], sheet) : 0.0;
                    double type = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 0.0;
                    double fv;
                    if (Math.Abs(rate) < 1e-12) fv = -(pv + pmt * nper);
                    else
                    {
                        double factor = Math.Pow(1.0 + rate, nper);
                        fv = -(pv * factor + pmt * (1.0 + rate * type) * (factor - 1.0) / rate);
                    }
                    res.SetNumber(fv);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "PV" && args.Length >= 3)
                {
                    double rate = ParseAndEvaluateNumeric(args[0], sheet);
                    double nper = ParseAndEvaluateNumeric(args[1], sheet);
                    double pmt = ParseAndEvaluateNumeric(args[2], sheet);
                    double fv = args.Length >= 4 ? ParseAndEvaluateNumeric(args[3], sheet) : 0.0;
                    double type = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 0.0;
                    double pv;
                    if (Math.Abs(rate) < 1e-12) pv = -(fv + pmt * nper);
                    else
                    {
                        double factor = Math.Pow(1.0 + rate, nper);
                        pv = -(fv + pmt * (1.0 + rate * type) * (factor - 1.0) / rate) / factor;
                    }
                    res.SetNumber(pv);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "NPER" && args.Length >= 3)
                {
                    double rate = ParseAndEvaluateNumeric(args[0], sheet);
                    double pmt = ParseAndEvaluateNumeric(args[1], sheet);
                    double pv = ParseAndEvaluateNumeric(args[2], sheet);
                    double fv = args.Length >= 4 ? ParseAndEvaluateNumeric(args[3], sheet) : 0.0;
                    double type = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 0.0;
                    double nper;
                    if (Math.Abs(rate) < 1e-12)
                    {
                        if (Math.Abs(pmt) < 1e-12) { res.SetString("#DIV/0!"); return res; }
                        nper = -(pv + fv) / pmt;
                    }
                    else
                    {
                        double pmtAdj = pmt * (1.0 + rate * type);
                        double num = pmtAdj - fv * rate;
                        double den = pv * rate + pmtAdj;
                        if (num / den <= 0) { res.SetString("#NUM!"); return res; }
                        nper = Math.Log(num / den) / Math.Log(1.0 + rate);
                    }
                    res.SetNumber(nper);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "NPV" && args.Length >= 2)
                {
                    double rate = ParseAndEvaluateNumeric(args[0], sheet);
                    string[] cashArgs = new string[args.Length - 1];
                    Array.Copy(args, 1, cashArgs, 0, cashArgs.Length);
                    List<double> flows = CollectNumericValues(cashArgs, sheet);
                    double npv = 0.0;
                    for (int i = 0; i < flows.Count; i++)
                    {
                        npv += flows[i] / Math.Pow(1.0 + rate, i + 1);
                    }
                    res.SetNumber(npv);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "IRR" && args.Length >= 1)
                {
                    List<double> flows = CollectNumericValues(new string[] { args[0] }, sheet);
                    double rate = args.Length >= 2 ? ParseAndEvaluateNumeric(args[1], sheet) : 0.1;
                    for (int iter = 0; iter < 100; iter++)
                    {
                        double fVal = 0.0, fDeriv = 0.0;
                        for (int i = 0; i < flows.Count; i++)
                        {
                            double p = Math.Pow(1.0 + rate, i);
                            fVal += flows[i] / p;
                            if (i > 0) fDeriv -= i * flows[i] / (p * (1.0 + rate));
                        }
                        if (Math.Abs(fVal) < 1e-7) break;
                        if (Math.Abs(fDeriv) < 1e-12) break;
                        rate -= fVal / fDeriv;
                    }
                    res.SetNumber(rate);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "RATE" && args.Length >= 3)
                {
                    double nper = ParseAndEvaluateNumeric(args[0], sheet);
                    double pmt = ParseAndEvaluateNumeric(args[1], sheet);
                    double pv = ParseAndEvaluateNumeric(args[2], sheet);
                    double fv = args.Length >= 4 ? ParseAndEvaluateNumeric(args[3], sheet) : 0.0;
                    double type = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 0.0;
                    double rate = args.Length >= 6 ? ParseAndEvaluateNumeric(args[5], sheet) : 0.1;
                    for (int iter = 0; iter < 100; iter++)
                    {
                        if (Math.Abs(rate) < 1e-10) rate = 1e-6;
                        double y = Math.Pow(1.0 + rate, nper);
                        double f = pv * y + pmt * (1.0 + rate * type) * (y - 1.0) / rate + fv;
                        if (Math.Abs(f) < 1e-7) break;
                        double dy = nper * Math.Pow(1.0 + rate, nper - 1.0);
                        double df = pv * dy + pmt * type * (y - 1.0) / rate + pmt * (1.0 + rate * type) * (dy * rate - (y - 1.0)) / (rate * rate);
                        if (Math.Abs(df) < 1e-12) break;
                        rate -= f / df;
                    }
                    res.SetNumber(rate);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "SLN" && args.Length >= 3)
                {
                    double cost = ParseAndEvaluateNumeric(args[0], sheet);
                    double salvage = ParseAndEvaluateNumeric(args[1], sheet);
                    double life = ParseAndEvaluateNumeric(args[2], sheet);
                    if (Math.Abs(life) < 1e-12) { res.SetString("#DIV/0!"); return res; }
                    res.SetNumber((cost - salvage) / life);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "SYD" && args.Length >= 4)
                {
                    double cost = ParseAndEvaluateNumeric(args[0], sheet);
                    double salvage = ParseAndEvaluateNumeric(args[1], sheet);
                    double life = ParseAndEvaluateNumeric(args[2], sheet);
                    double per = ParseAndEvaluateNumeric(args[3], sheet);
                    if (life <= 0 || per <= 0 || per > life) { res.SetString("#NUM!"); return res; }
                    double syd = ((cost - salvage) * (life - per + 1.0) * 2.0) / (life * (life + 1.0));
                    res.SetNumber(syd);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "DDB" && args.Length >= 4)
                {
                    double cost = ParseAndEvaluateNumeric(args[0], sheet);
                    double salvage = ParseAndEvaluateNumeric(args[1], sheet);
                    double life = ParseAndEvaluateNumeric(args[2], sheet);
                    double period = ParseAndEvaluateNumeric(args[3], sheet);
                    double factor = args.Length >= 5 ? ParseAndEvaluateNumeric(args[4], sheet) : 2.0;
                    if (cost < 0 || salvage < 0 || life <= 0 || period <= 0 || period > life || factor <= 0) { res.SetString("#NUM!"); return res; }
                    double bookVal = cost;
                    double depr = 0.0;
                    for (int p = 1; p <= (int)Math.Ceiling(period); p++)
                    {
                        depr = Math.Min(bookVal * (factor / life), Math.Max(0.0, bookVal - salvage));
                        bookVal -= depr;
                    }
                    res.SetNumber(depr);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "EFFECT" && args.Length >= 2)
                {
                    double nom = ParseAndEvaluateNumeric(args[0], sheet);
                    int npery = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    if (nom <= 0 || npery < 1) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(Math.Pow(1.0 + nom / npery, npery) - 1.0);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "NOMINAL" && args.Length >= 2)
                {
                    double eff = ParseAndEvaluateNumeric(args[0], sheet);
                    int npery = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    if (eff <= 0 || npery < 1) { res.SetString("#NUM!"); return res; }
                    res.SetNumber(npery * (Math.Pow(eff + 1.0, 1.0 / npery) - 1.0));
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }

                // --- DATE & TIME ADDITIONAL FUNCTIONS ---
                else if (funcName == "EOMONTH" && args.Length >= 2)
                {
                    DateTime dt = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    int months = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    DateTime target = dt.AddMonths(months);
                    DateTime eom = new DateTime(target.Year, target.Month, DateTime.DaysInMonth(target.Year, target.Month));
                    res.SetNumber(eom.ToOADate());
                    res.EvaluatedStr = eom.ToString("dd/MM/yyyy");
                    return res;
                }
                else if (funcName == "WORKDAY" && args.Length >= 2)
                {
                    DateTime cur = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet)).Date;
                    int days = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    int step = days >= 0 ? 1 : -1;
                    int rem = Math.Abs(days);
                    while (rem > 0)
                    {
                        cur = cur.AddDays(step);
                        if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday)
                            rem--;
                    }
                    res.SetNumber(cur.ToOADate());
                    res.EvaluatedStr = cur.ToString("dd/MM/yyyy");
                    return res;
                }
                else if (funcName == "DAYS" && args.Length >= 2)
                {
                    DateTime endDt = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    DateTime startDt = DateTime.FromOADate(ParseAndEvaluateNumeric(args[1], sheet));
                    double d = (endDt.Date - startDt.Date).TotalDays;
                    res.SetNumber(d);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "DAYS360" && args.Length >= 2)
                {
                    DateTime d1 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    DateTime d2 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[1], sheet));
                    int day1 = Math.Min(30, d1.Day);
                    int day2 = (d2.Day == 31 && day1 == 30) ? 30 : Math.Min(30, d2.Day);
                    int diff = (d2.Year - d1.Year) * 360 + (d2.Month - d1.Month) * 30 + (day2 - day1);
                    res.SetNumber(diff);
                    res.EvaluatedStr = diff.ToString();
                    return res;
                }
                else if (funcName == "YEARFRAC" && args.Length >= 2)
                {
                    DateTime d1 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    DateTime d2 = DateTime.FromOADate(ParseAndEvaluateNumeric(args[1], sheet));
                    double frac = Math.Abs((d2.Date - d1.Date).TotalDays) / 365.25;
                    res.SetNumber(frac);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if ((funcName == "WEEKNUM" || funcName == "ISOWEEKNUM") && args.Length >= 1)
                {
                    DateTime dt = DateTime.FromOADate(ParseAndEvaluateNumeric(args[0], sheet));
                    int wk = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    res.SetNumber(wk);
                    res.EvaluatedStr = wk.ToString();
                    return res;
                }
                else if (funcName == "DATEVALUE" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet);
                    DateTime dt;
                    if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt) ||
                        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        res.SetNumber(dt.Date.ToOADate());
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else res.SetString("#VALUE!");
                    return res;
                }
                else if (funcName == "TIMEVALUE" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet);
                    DateTime dt;
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        res.SetNumber(dt.TimeOfDay.TotalDays);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else res.SetString("#VALUE!");
                    return res;
                }

                // --- TEXT ADDITIONAL FUNCTIONS ---
                else if (funcName == "REPLACE" && args.Length >= 4)
                {
                    string oldText = GetStringValue(args[0], sheet);
                    int startNum = Math.Max(1, (int)ParseAndEvaluateNumeric(args[1], sheet)) - 1;
                    int numChars = Math.Max(0, (int)ParseAndEvaluateNumeric(args[2], sheet));
                    string newText = GetStringValue(args[3], sheet);
                    if (startNum > oldText.Length) startNum = oldText.Length;
                    if (startNum + numChars > oldText.Length) numChars = oldText.Length - startNum;
                    res.SetString(oldText.Substring(0, startNum) + newText + oldText.Substring(startNum + numChars));
                    return res;
                }
                else if (funcName == "CLEAN" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet);
                    StringBuilder sb = new StringBuilder();
                    foreach (char c in s) { if (c >= 32) sb.Append(c); }
                    res.SetString(sb.ToString());
                    return res;
                }
                else if (funcName == "FIXED" && args.Length >= 1)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    int dec = args.Length >= 2 ? Math.Max(0, (int)ParseAndEvaluateNumeric(args[1], sheet)) : 2;
                    bool noCommas = args.Length >= 3 && EvaluateCondition(args[2], sheet);
                    res.SetString(num.ToString((noCommas ? "F" : "N") + dec, CultureInfo.InvariantCulture));
                    return res;
                }
                else if (funcName == "DOLLAR" && args.Length >= 1)
                {
                    double num = ParseAndEvaluateNumeric(args[0], sheet);
                    int dec = args.Length >= 2 ? Math.Max(0, (int)ParseAndEvaluateNumeric(args[1], sheet)) : 2;
                    res.SetString("$" + num.ToString("N" + dec, CultureInfo.InvariantCulture));
                    return res;
                }
                else if (funcName == "UNICHAR" && args.Length >= 1)
                {
                    int code = (int)ParseAndEvaluateNumeric(args[0], sheet);
                    if (code < 1 || code > 0x10FFFF) { res.SetString("#VALUE!"); return res; }
                    res.SetString(char.ConvertFromUtf32(code));
                    return res;
                }
                else if (funcName == "UNICODE" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet);
                    if (string.IsNullOrEmpty(s)) { res.SetString("#VALUE!"); return res; }
                    int code = char.ConvertToUtf32(s, 0);
                    res.SetNumber(code);
                    res.EvaluatedStr = code.ToString();
                    return res;
                }
                else if (funcName == "NUMBERVALUE" && args.Length >= 1)
                {
                    string s = GetStringValue(args[0], sheet).Trim();
                    string decSep = args.Length >= 2 ? GetStringValue(args[1], sheet) : ".";
                    string grpSep = args.Length >= 3 ? GetStringValue(args[2], sheet) : ",";
                    if (!string.IsNullOrEmpty(grpSep)) s = s.Replace(grpSep, "");
                    if (!string.IsNullOrEmpty(decSep) && decSep != ".") s = s.Replace(decSep, ".");
                    double v;
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    {
                        res.SetNumber(v);
                        res.EvaluatedStr = FormatCellDisplay(res);
                    }
                    else res.SetString("#VALUE!");
                    return res;
                }
                else if (funcName == "T" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    res.SetString((c != null && c.Type == CellType.String) ? (c.EvaluatedStr ?? c.RawValue ?? "") : "");
                    return res;
                }
                else if (funcName == "N" && args.Length >= 1)
                {
                    double v = ParseAndEvaluateNumeric(args[0], sheet);
                    res.SetNumber(double.IsNaN(v) ? 0 : v);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }

                // --- LOOKUP & REFERENCE ADDITIONAL FUNCTIONS ---
                else if (funcName == "ROWS" && args.Length >= 1)
                {
                    var r = ParseRange(args[0]);
                    if (r.Count == 0) { res.SetNumber(1); res.EvaluatedStr = "1"; return res; }
                    uint minR = r[0].Item1, maxR = r[r.Count - 1].Item1;
                    uint cnt = maxR - minR + 1;
                    res.SetNumber(cnt);
                    res.EvaluatedStr = cnt.ToString();
                    return res;
                }
                else if (funcName == "COLUMNS" && args.Length >= 1)
                {
                    var r = ParseRange(args[0]);
                    if (r.Count == 0) { res.SetNumber(1); res.EvaluatedStr = "1"; return res; }
                    uint minC = r[0].Item2, maxC = r[r.Count - 1].Item2;
                    uint cnt = maxC - minC + 1;
                    res.SetNumber(cnt);
                    res.EvaluatedStr = cnt.ToString();
                    return res;
                }
                else if (funcName == "ADDRESS" && args.Length >= 2)
                {
                    uint r = (uint)Math.Max(1, (int)ParseAndEvaluateNumeric(args[0], sheet));
                    uint c = (uint)Math.Max(1, (int)ParseAndEvaluateNumeric(args[1], sheet));
                    int absNum = args.Length >= 3 ? (int)ParseAndEvaluateNumeric(args[2], sheet) : 1;
                    string colName = SparseMatrix.ColIndexToName(c);
                    string addr;
                    if (absNum == 2) addr = colName + "$" + r;
                    else if (absNum == 3) addr = "$" + colName + r;
                    else if (absNum == 4) addr = colName + r;
                    else addr = "$" + colName + "$" + r;
                    if (args.Length >= 5)
                    {
                        string sheetText = GetStringValue(args[4], sheet);
                        if (!string.IsNullOrEmpty(sheetText))
                            addr = (sheetText.Contains(" ") ? ("'" + sheetText + "'!") : (sheetText + "!")) + addr;
                    }
                    res.SetString(addr);
                    return res;
                }
                else if (funcName == "LOOKUP" && args.Length >= 2)
                {
                    if (args.Length >= 3)
                        return EvaluateFormula("=XLOOKUP(" + args[0] + "," + args[1] + "," + args[2] + ")", sheet);
                    return EvaluateFormula("=VLOOKUP(" + args[0] + "," + args[1] + ",1,TRUE)", sheet);
                }
                else if (funcName == "FORMULATEXT" && args.Length >= 1)
                {
                    SparseMatrix sRef = ResolveSheet(args[0], sheet);
                    var pos = SparseMatrix.ParseCellAddress(args[0]);
                    CellData c = sRef.GetCell(pos.Item1, pos.Item2);
                    if (c != null && c.Type == CellType.Formula && !string.IsNullOrEmpty(c.FormulaStr))
                        res.SetString(c.FormulaStr);
                    else
                        res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "HYPERLINK" && args.Length >= 1)
                {
                    string url = GetStringValue(args[0], sheet);
                    string label = args.Length >= 2 ? GetStringValue(args[1], sheet) : url;
                    res.SetString(label);
                    return res;
                }
                else if (funcName == "UNIQUE" && args.Length >= 1)
                {
                    SparseMatrix sRange = ResolveSheet(args[0], sheet);
                    var range = ParseRange(args[0]);
                    List<string> uniq = new List<string>();
                    foreach (var p in range)
                    {
                        CellData c = sRange.GetCell(p.Item1, p.Item2);
                        string v = c != null ? (c.EvaluatedStr ?? c.RawValue ?? "") : "";
                        if (!string.IsNullOrEmpty(v) && !uniq.Contains(v)) uniq.Add(v);
                    }
                    res.SetString(uniq.Count > 0 ? string.Join(", ", uniq.ToArray()) : "");
                    return res;
                }

                // --- LOGICAL & INFORMATION FUNCTIONS ---
                else if (funcName == "TRUE")
                {
                    res.SetString("TRUE");
                    res.NumericValue = 1;
                    return res;
                }
                else if (funcName == "FALSE")
                {
                    res.SetString("FALSE");
                    res.NumericValue = 0;
                    return res;
                }
                else if (funcName == "NA")
                {
                    res.SetString("#N/A");
                    return res;
                }
                else if (funcName == "ISERR" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    string val = c != null ? (c.EvaluatedStr ?? c.RawValue ?? "") : "";
                    bool isErr = val.StartsWith("#") && val != "#N/A";
                    res.SetNumber(isErr ? 1.0 : 0.0);
                    res.EvaluatedStr = isErr ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISLOGICAL" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    string val = c != null ? (c.EvaluatedStr ?? c.RawValue ?? "") : "";
                    bool isLog = val.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || val.Equals("FALSE", StringComparison.OrdinalIgnoreCase);
                    res.SetNumber(isLog ? 1.0 : 0.0);
                    res.EvaluatedStr = isLog ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISNONTEXT" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    bool isNonTxt = (c == null || c.Type != CellType.String);
                    res.SetNumber(isNonTxt ? 1.0 : 0.0);
                    res.EvaluatedStr = isNonTxt ? "TRUE" : "FALSE";
                    return res;
                }
                else if ((funcName == "ISEVEN" || funcName == "ISODD") && args.Length >= 1)
                {
                    long v = (long)Math.Truncate(ParseAndEvaluateNumeric(args[0], sheet));
                    bool even = (v % 2 == 0);
                    bool ans = (funcName == "ISEVEN") ? even : !even;
                    res.SetNumber(ans ? 1.0 : 0.0);
                    res.EvaluatedStr = ans ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "ISFORMULA" && args.Length >= 1)
                {
                    SparseMatrix sRef = ResolveSheet(args[0], sheet);
                    var pos = SparseMatrix.ParseCellAddress(args[0]);
                    CellData c = sRef.GetCell(pos.Item1, pos.Item2);
                    bool isForm = (c != null && c.Type == CellType.Formula);
                    res.SetNumber(isForm ? 1.0 : 0.0);
                    res.EvaluatedStr = isForm ? "TRUE" : "FALSE";
                    return res;
                }
                else if (funcName == "TYPE" && args.Length >= 1)
                {
                    CellData c = GetCellReferenceOrData(args[0], sheet);
                    int tCode = 1;
                    if (c != null)
                    {
                        string v = c.EvaluatedStr ?? c.RawValue ?? "";
                        if (v.StartsWith("#")) tCode = 16;
                        else if (v.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || v.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) tCode = 4;
                        else if (c.Type == CellType.String) tCode = 2;
                    }
                    res.SetNumber(tCode);
                    res.EvaluatedStr = tCode.ToString();
                    return res;
                }
            }

            // Check top-level string concatenation operator '&' (e.g. =A1 & " - " & B1)
            List<string> concatParts = SplitTopLevelByChar(expr, '&');
            if (concatParts.Count > 1)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var part in concatParts)
                {
                    CellData tmp = new CellData();
                    SetResultFromToken(tmp, part, sheet);
                    sb.Append(tmp.EvaluatedStr ?? tmp.RawValue ?? "");
                }
                res.SetString(sb.ToString());
                return res;
            }

            // Check top-level comparison operators (e.g. =A1>B1, =SUM(A1:A5)>=100)
            if (HasTopLevelComparison(expr))
            {
                bool cond = EvaluateCondition(expr, sheet);
                res.SetString(cond ? "TRUE" : "FALSE");
                res.NumericValue = cond ? 1.0 : 0.0;
                return res;
            }

            // General Arithmetic Evaluation (including nested functions, parentheses, %, ^, *, /, +, -)
            double evalVal = ParseAndEvaluateNumeric(expr, sheet);
            if (double.IsNaN(evalVal) || double.IsInfinity(evalVal))
            {
                res.SetString("#DIV/0!");
                res.NumericValue = 0;
            }
            else
            {
                res.SetNumber(evalVal);
                res.EvaluatedStr = FormatCellDisplay(res);
            }
            return res;
        }

        private static List<string> SplitTopLevelByChar(string expr, char sep)
        {
            List<string> parts = new List<string>();
            if (string.IsNullOrEmpty(expr)) return parts;
            int depth = 0;
            bool inQuote = false;
            StringBuilder cur = new StringBuilder();
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '"') inQuote = !inQuote;
                else if (!inQuote)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                    else if (c == sep && depth == 0)
                    {
                        parts.Add(cur.ToString().Trim());
                        cur.Clear();
                        continue;
                    }
                }
                cur.Append(c);
            }
            if (parts.Count > 0) parts.Add(cur.ToString().Trim());
            return parts;
        }

        private static bool HasTopLevelComparison(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return false;
            int depth = 0;
            bool inQuote = false;
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '"') inQuote = !inQuote;
                else if (!inQuote)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                    else if (depth == 0 && (c == '>' || c == '<' || c == '='))
                        return true;
                }
            }
            return false;
        }

        private static void SetResultFromToken(CellData res, string token, SparseMatrix sheet)
        {
            string clean = (token ?? "").Trim();
            if (string.IsNullOrEmpty(clean))
            {
                res.SetString("");
                return;
            }

            if (clean.StartsWith("\"") && clean.EndsWith("\"") && clean.Length >= 2)
            {
                res.SetString(clean.Substring(1, clean.Length - 2).Replace("\"\"", "\""));
                return;
            }

            string fName, fArgs;
            if (IsSingleTopLevelFunctionCall(clean, out fName, out fArgs))
            {
                CellData evalInner = EvaluateFormula("=" + clean, sheet);
                res.Type = evalInner.Type;
                res.NumericValue = evalInner.NumericValue;
                res.EvaluatedStr = evalInner.EvaluatedStr;
                res.RawValue = evalInner.RawValue;
                return;
            }

            double num;
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
            {
                res.SetNumber(num);
                res.EvaluatedStr = FormatCellDisplay(res);
                return;
            }

            if (IsCellReference(clean))
            {
                SparseMatrix refSheet = ResolveSheet(clean, sheet);
                var pos = SparseMatrix.ParseCellAddress(clean);
                CellData c = refSheet.GetCell(pos.Item1, pos.Item2);
                if (c != null)
                {
                    res.Type = c.Type;
                    res.NumericValue = c.NumericValue;
                    res.EvaluatedStr = c.EvaluatedStr ?? c.RawValue;
                    res.RawValue = c.RawValue;
                }
                else
                {
                    res.SetString("");
                }
                return;
            }

            if (clean.Contains("&") || clean.Contains("+") || clean.Contains("-") || clean.Contains("*") || clean.Contains("/") || clean.Contains("^") || clean.Contains("("))
            {
                CellData evalExpr = EvaluateFormula("=" + clean, sheet);
                res.Type = evalExpr.Type;
                res.NumericValue = evalExpr.NumericValue;
                res.EvaluatedStr = evalExpr.EvaluatedStr;
                res.RawValue = evalExpr.RawValue;
                return;
            }

            res.SetString(clean);
        }

        private static bool WildcardMatch(string text, string pattern)
        {
            string rx = "^" + System.Text.RegularExpressions.Regex.Escape(pattern ?? "")
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(text ?? "", rx, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static bool MatchesCriteria(CellData cell, string criteria, SparseMatrix sheet)
        {
            if (cell == null) return false;
            string crit = (criteria ?? "").Trim();
            if (crit.StartsWith("\"") && crit.EndsWith("\"") && crit.Length >= 2)
                crit = crit.Substring(1, crit.Length - 2);
            else if (IsCellReference(crit))
            {
                SparseMatrix sCritRef = ResolveSheet(crit, sheet);
                var pos = SparseMatrix.ParseCellAddress(crit);
                CellData refC = sCritRef.GetCell(pos.Item1, pos.Item2);
                crit = refC != null ? (!string.IsNullOrEmpty(refC.EvaluatedStr) ? refC.EvaluatedStr : refC.RawValue ?? "") : "";
            }

            string cellStr = (!string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue) ?? "";
            double cellNum = cell.NumericValue;

            string[] ops = new string[] { ">=", "<=", "<>", "!=", ">", "<", "=" };
            foreach (var op in ops)
            {
                if (crit.StartsWith(op))
                {
                    string target = crit.Substring(op.Length).Trim();
                    double targetNum;
                    if (double.TryParse(target, NumberStyles.Any, CultureInfo.InvariantCulture, out targetNum))
                    {
                        switch (op)
                        {
                            case ">=": return cellNum >= targetNum;
                            case "<=": return cellNum <= targetNum;
                            case ">": return cellNum > targetNum;
                            case "<": return cellNum < targetNum;
                            case "<>":
                            case "!=": return Math.Abs(cellNum - targetNum) > 0.000001;
                            case "=": return Math.Abs(cellNum - targetNum) < 0.000001;
                        }
                    }
                    else
                    {
                        if ((op == "=" || op == "<>" || op == "!=") && (target.Contains("*") || target.Contains("?")))
                        {
                            bool wm = WildcardMatch(cellStr, target);
                            return op == "=" ? wm : !wm;
                        }
                        int cmp = string.Compare(cellStr, target, StringComparison.OrdinalIgnoreCase);
                        switch (op)
                        {
                            case "=": return cmp == 0;
                            case "<>":
                            case "!=": return cmp != 0;
                            case ">": return cmp > 0;
                            case ">=": return cmp >= 0;
                            case "<": return cmp < 0;
                            case "<=": return cmp <= 0;
                        }
                    }
                }
            }

            double dCrit;
            if (double.TryParse(crit, NumberStyles.Any, CultureInfo.InvariantCulture, out dCrit))
            {
                return Math.Abs(cellNum - dCrit) < 0.000001;
            }

            if (crit.Contains("*") || crit.Contains("?"))
            {
                return WildcardMatch(cellStr, crit);
            }

            return string.Equals(cellStr, crit, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStringValue(string token, SparseMatrix sheet)
        {
            string clean = (token ?? "").Trim();
            if (clean.StartsWith("\"") && clean.EndsWith("\"") && clean.Length >= 2)
            {
                return clean.Substring(1, clean.Length - 2).Replace("\"\"", "\"");
            }
            if (IsCellReference(clean))
            {
                SparseMatrix refSheet = ResolveSheet(clean, sheet);
                var pos = SparseMatrix.ParseCellAddress(clean);
                CellData c = refSheet.GetCell(pos.Item1, pos.Item2);
                if (c != null) return (!string.IsNullOrEmpty(c.EvaluatedStr) ? c.EvaluatedStr : c.RawValue) ?? "";
                return "";
            }
            string fName, fArgs;
            if (IsSingleTopLevelFunctionCall(clean, out fName, out fArgs))
            {
                CellData eval = EvaluateFormula("=" + clean, sheet);
                return eval.EvaluatedStr ?? eval.RawValue ?? "";
            }
            return clean;
        }

        private static bool EvaluateCondition(string condStr, SparseMatrix sheet)
        {
            string clean = (condStr ?? "").Trim();
            if (string.IsNullOrEmpty(clean)) return false;
            if (clean.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
            if (clean.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return false;

            string fName, fArgs;
            if (IsSingleTopLevelFunctionCall(clean, out fName, out fArgs))
            {
                CellData eval = EvaluateFormula("=" + clean, sheet);
                if (eval != null)
                {
                    if (eval.EvaluatedStr != null && eval.EvaluatedStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
                    if (eval.EvaluatedStr != null && eval.EvaluatedStr.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return false;
                    return Math.Abs(eval.NumericValue) > 0.000001;
                }
            }

            string[] ops = new string[] { ">=", "<=", "<>", "!=", ">", "<", "=" };
            foreach (var op in ops)
            {
                int opIdx = FindTopLevelSubstring(clean, op);
                if (opIdx > 0)
                {
                    string leftStr = clean.Substring(0, opIdx).Trim();
                    string rightStr = clean.Substring(opIdx + op.Length).Trim();

                    bool leftIsString = leftStr.StartsWith("\"") && leftStr.EndsWith("\"");
                    bool rightIsString = rightStr.StartsWith("\"") && rightStr.EndsWith("\"");

                    if (leftIsString || rightIsString)
                    {
                        string leftVal = GetStringValue(leftStr, sheet);
                        string rightVal = GetStringValue(rightStr, sheet);
                        int cmp = string.Compare(leftVal, rightVal, StringComparison.OrdinalIgnoreCase);
                        switch (op)
                        {
                            case "=": return cmp == 0;
                            case "<>":
                            case "!=": return cmp != 0;
                            case ">": return cmp > 0;
                            case ">=": return cmp >= 0;
                            case "<": return cmp < 0;
                            case "<=": return cmp <= 0;
                        }
                    }
                    else
                    {
                        double leftNum = ParseAndEvaluateNumeric(leftStr, sheet);
                        double rightNum = ParseAndEvaluateNumeric(rightStr, sheet);

                        switch (op)
                        {
                            case ">=": return leftNum >= rightNum;
                            case "<=": return leftNum <= rightNum;
                            case "<>":
                            case "!=": return Math.Abs(leftNum - rightNum) > 0.000001;
                            case ">": return leftNum > rightNum;
                            case "<": return leftNum < rightNum;
                            case "=": return Math.Abs(leftNum - rightNum) < 0.000001;
                        }
                    }
                }
            }

            double singleNum = ParseAndEvaluateNumeric(clean, sheet);
            return Math.Abs(singleNum) > 0.000001;
        }

        private static int FindTopLevelSubstring(string str, string op)
        {
            int depth = 0;
            bool inQuote = false;
            for (int i = 0; i <= str.Length - op.Length; i++)
            {
                char c = str[i];
                if (c == '"') inQuote = !inQuote;
                else if (!inQuote)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                    else if (depth == 0 && string.CompareOrdinal(str, i, op, 0, op.Length) == 0)
                    {
                        if (op == ">" && i > 0 && str[i - 1] == '<') continue;
                        if (op == "=" && i > 0 && (str[i - 1] == '>' || str[i - 1] == '<' || str[i - 1] == '!')) continue;
                        if ((op == ">" || op == "<") && i + 1 < str.Length && (str[i + 1] == '=' || str[i + 1] == '>')) continue;
                        return i;
                    }
                }
            }
            return -1;
        }

        private static bool IsCellReference(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            string s = str.Trim().Replace("$", "");
            int excl = s.LastIndexOf('!');
            if (excl >= 0 && excl < s.Length - 1)
            {
                s = s.Substring(excl + 1).Trim();
            }
            int i = 0;
            while (i < s.Length && char.IsLetter(s[i])) i++;
            if (i == 0 || i > 3 || i == s.Length) return false;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            return (i == s.Length);
        }

        private static bool HasMatchingOuterParens(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 2 || s[0] != '(' || s[s.Length - 1] != ')') return false;
            int depth = 0;
            bool inQuote = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"') inQuote = !inQuote;
                else if (!inQuote)
                {
                    if (c == '(') depth++;
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0 && i < s.Length - 1) return false;
                    }
                }
            }
            return depth == 0;
        }

        public static double ParseAndEvaluateNumeric(string expr, SparseMatrix sheet)
        {
            string clean = (expr ?? "").Trim();
            if (string.IsNullOrEmpty(clean)) return 0.0;

            while (HasMatchingOuterParens(clean))
            {
                clean = clean.Substring(1, clean.Length - 2).Trim();
            }
            if (string.IsNullOrEmpty(clean)) return 0.0;

            // Binary Operations in reverse operator precedence: + / -, then * / /, then ^
            int plusMinus = FindLowestPrecedenceOp(clean, '+', '-');
            if (plusMinus > 0)
            {
                char op = clean[plusMinus];
                double left = ParseAndEvaluateNumeric(clean.Substring(0, plusMinus), sheet);
                double right = ParseAndEvaluateNumeric(clean.Substring(plusMinus + 1), sheet);
                return (op == '+') ? (left + right) : (left - right);
            }

            int mulDiv = FindLowestPrecedenceOp(clean, '*', '/');
            if (mulDiv > 0)
            {
                char op = clean[mulDiv];
                double left = ParseAndEvaluateNumeric(clean.Substring(0, mulDiv), sheet);
                double right = ParseAndEvaluateNumeric(clean.Substring(mulDiv + 1), sheet);
                return (op == '*') ? (left * right) : (Math.Abs(right) > 1e-12 ? (left / right) : double.NaN);
            }

            int powOp = FindLowestPrecedenceOp(clean, '^', '^');
            if (powOp > 0)
            {
                double left = ParseAndEvaluateNumeric(clean.Substring(0, powOp), sheet);
                double right = ParseAndEvaluateNumeric(clean.Substring(powOp + 1), sheet);
                return Math.Pow(left, right);
            }

            // Unary + or -
            if (clean[0] == '-')
                return -ParseAndEvaluateNumeric(clean.Substring(1), sheet);
            if (clean[0] == '+')
                return ParseAndEvaluateNumeric(clean.Substring(1), sheet);

            // Postfix percentage %
            if (clean.EndsWith("%") && clean.Length > 1)
                return ParseAndEvaluateNumeric(clean.Substring(0, clean.Length - 1), sheet) / 100.0;

            // Embedded Function Call (e.g. SUM(A1:A5) inside SUM(A1:A5)+AVERAGE(B1:B5))
            string fName, fArgs;
            if (IsSingleTopLevelFunctionCall(clean, out fName, out fArgs))
            {
                CellData fRes = EvaluateFormula("=" + clean, sheet);
                return fRes != null ? fRes.NumericValue : 0.0;
            }

            // Cell Reference (including cross-sheet Sheet2!A1)
            if (IsCellReference(clean))
            {
                SparseMatrix refSheet = ResolveSheet(clean, sheet);
                var pos = SparseMatrix.ParseCellAddress(clean);
                CellData cell = refSheet.GetCell(pos.Item1, pos.Item2);
                return cell != null ? cell.NumericValue : 0.0;
            }

            if (clean.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return 1.0;
            if (clean.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return 0.0;

            double val;
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val;
            if (double.TryParse(clean, out val)) return val;
            return 0.0;
        }

        private static int FindLowestPrecedenceOp(string str, char op1, char op2)
        {
            int depth = 0;
            bool inQuote = false;
            bool inSingleQuote = false;
            for (int i = str.Length - 1; i >= 1; i--)
            {
                char c = str[i];
                if (c == '"' && !inSingleQuote) inQuote = !inQuote;
                else if (c == '\'' && !inQuote) inSingleQuote = !inSingleQuote;
                else if (!inQuote && !inSingleQuote)
                {
                    if (c == ')') depth++;
                    else if (c == '(') depth--;
                    else if (depth == 0 && (c == op1 || c == op2))
                    {
                        int prevIdx = i - 1;
                        while (prevIdx >= 0 && str[prevIdx] == ' ') prevIdx--;
                        if (prevIdx < 0) continue;
                        char prev = str[prevIdx];
                        if ((c == '-' || c == '+') && (prev == '+' || prev == '-' || prev == '*' || prev == '/' || prev == '^' || prev == '(' || prev == '=' || prev == '<' || prev == '>'))
                            continue;
                        if ((c == '+' || c == '-') && (prev == 'E' || prev == 'e') && prevIdx > 0 && char.IsDigit(str[prevIdx - 1]))
                            continue;
                        return i;
                    }
                }
            }
            return -1;
        }

        private static bool IsPureLetters(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++) if (!char.IsLetter(s[i])) return false;
            return true;
        }

        private static bool IsPureDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++) if (!char.IsDigit(s[i])) return false;
            return true;
        }

        public static List<Tuple<uint, uint>> ParseRange(string rangeStr)
        {
            var res = new List<Tuple<uint, uint>>();
            string clean = (rangeStr ?? "").Trim().Replace("$", "");
            int excl = clean.LastIndexOf('!');
            if (excl >= 0 && excl < clean.Length - 1)
            {
                clean = clean.Substring(excl + 1).Trim();
            }
            int colonPos = clean.IndexOf(':');
            if (colonPos < 0)
            {
                if (IsCellReference(clean))
                {
                    res.Add(SparseMatrix.ParseCellAddress(clean));
                }
                return res;
            }

            string startAddr = clean.Substring(0, colonPos).Trim();
            string endAddr = clean.Substring(colonPos + 1).Trim();

            if (IsPureLetters(startAddr) && IsPureLetters(endAddr))
            {
                startAddr += "1";
                endAddr += "500";
            }
            else if (IsPureDigits(startAddr) && IsPureDigits(endAddr))
            {
                startAddr = "A" + startAddr;
                endAddr = "AZ" + endAddr;
            }

            var p1 = SparseMatrix.ParseCellAddress(startAddr);
            var p2 = SparseMatrix.ParseCellAddress(endAddr);

            uint minR = Math.Min(p1.Item1, p2.Item1);
            uint maxR = Math.Max(p1.Item1, p2.Item1);
            uint minC = Math.Min(p1.Item2, p2.Item2);
            uint maxC = Math.Max(p1.Item2, p2.Item2);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    res.Add(new Tuple<uint, uint>(r, c));
                }
            }
            return res;
        }

        public static string[] SplitArgs(string argsStr)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(argsStr)) return list.ToArray();
            StringBuilder current = new StringBuilder();
            int depth = 0;
            bool quote = false;

            foreach (char c in argsStr)
            {
                if (c == '"') quote = !quote;
                else if (c == '(' && !quote) depth++;
                else if (c == ')' && !quote) depth--;
                else if ((c == ',' || c == ';') && depth == 0 && !quote)
                {
                    list.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0 || argsStr.EndsWith(",") || argsStr.EndsWith(";"))
                list.Add(current.ToString().Trim());
            return list.ToArray();
        }

        private static List<double> CollectNumericValues(string[] args, SparseMatrix sheet)
        {
            List<double> values = new List<double>();
            foreach (var arg in args)
            {
                SparseMatrix sRange = ResolveSheet(arg, sheet);
                var range = ParseRange(arg);
                if (range.Count > 0)
                {
                    foreach (var p in range)
                    {
                        CellData c = sRange.GetCell(p.Item1, p.Item2);
                        if (c != null && (c.Type == CellType.Number || (c.Type == CellType.Formula && !string.IsNullOrEmpty(c.EvaluatedStr) && !c.EvaluatedStr.StartsWith("#"))))
                        {
                            values.Add(c.NumericValue);
                        }
                    }
                }
                else
                {
                    double d = ParseAndEvaluateNumeric(arg, sheet);
                    if (!double.IsNaN(d)) values.Add(d);
                }
            }
            return values;
        }

        private static CellData GetCellReferenceOrData(string token, SparseMatrix sheet)
        {
            if (string.IsNullOrEmpty(token)) return null;
            SparseMatrix sRange = ResolveSheet(token, sheet);
            var range = ParseRange(token);
            if (range.Count > 0)
            {
                return sRange.GetCell(range[0].Item1, range[0].Item2);
            }
            return EvaluateFormula("=" + token.Trim(), sheet);
        }
    }
}

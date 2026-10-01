using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GenOffice.Core
{
    public class FormulaEngine
    {
        public static void EvaluateSheet(SparseMatrix sheet)
        {
            if (sheet == null) return;
            var cells = new List<KeyValuePair<ulong, CellData>>(sheet.GetAllCells());
            foreach (var kvp in cells)
            {
                var pos = SparseMatrix.DecodeKey(kvp.Key);
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
            if (IsCellReference(expr))
            {
                var pos = SparseMatrix.ParseCellAddress(expr);
                CellData src = sheet.GetCell(pos.Item1, pos.Item2);
                if (src != null)
                {
                    res.NumericValue = src.NumericValue;
                    res.EvaluatedStr = src.EvaluatedStr ?? src.RawValue;
                    res.RawValue = src.RawValue;
                    res.Type = src.Type;
                    return res;
                }
            }

            int openParen = expr.IndexOf('(');
            int closeParen = expr.LastIndexOf(')');

            if (openParen > 0 && closeParen > openParen)
            {
                string funcName = expr.Substring(0, openParen).Trim().ToUpper();
                string argsStr = expr.Substring(openParen + 1, closeParen - openParen - 1);
                string[] args = SplitArgs(argsStr);

                // Math & Stats Functions
                if (funcName == "SUM")
                {
                    List<double> values = new List<double>();
                    foreach (var arg in args)
                    {
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                else if (funcName == "AVERAGE")
                {
                    List<double> values = new List<double>();
                    foreach (var arg in args)
                    {
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                        var rangeA = ParseRange(args[0]);
                        var rangeB = ParseRange(args[1]);
                        int n = Math.Min(rangeA.Count, rangeB.Count);
                        double[] arrA = new double[n];
                        double[] arrB = new double[n];
                        for (int i = 0; i < n; i++)
                        {
                            CellData cA = sheet.GetCell(rangeA[i].Item1, rangeA[i].Item2);
                            CellData cB = sheet.GetCell(rangeB[i].Item1, rangeB[i].Item2);
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
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                else if (funcName == "MIN")
                {
                    double minVal = double.MaxValue;
                    bool found = false;
                    foreach (var arg in args)
                    {
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                else if (funcName == "MAX")
                {
                    double maxVal = double.MinValue;
                    bool found = false;
                    foreach (var arg in args)
                    {
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                        var range = ParseRange(arg);
                        if (range.Count > 0)
                        {
                            foreach (var cellPos in range)
                            {
                                CellData cell = sheet.GetCell(cellPos.Item1, cellPos.Item2);
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
                    var range = ParseRange(args[0]);
                    string crit = args[1];
                    int matchCount = 0;
                    foreach (var cellPos in range)
                    {
                        CellData c = sheet.GetCell(cellPos.Item1, cellPos.Item2);
                        if (MatchesCriteria(c, crit, sheet)) matchCount++;
                    }
                    res.SetNumber(matchCount);
                    res.EvaluatedStr = matchCount.ToString();
                    return res;
                }
                else if (funcName == "SUMIF" && args.Length >= 2)
                {
                    var critRange = ParseRange(args[0]);
                    string crit = args[1];
                    var sumRange = args.Length >= 3 ? ParseRange(args[2]) : critRange;

                    double totalSum = 0.0;
                    int n = Math.Min(critRange.Count, sumRange.Count);
                    for (int i = 0; i < n; i++)
                    {
                        CellData cCrit = sheet.GetCell(critRange[i].Item1, critRange[i].Item2);
                        if (MatchesCriteria(cCrit, crit, sheet))
                        {
                            CellData cSum = sheet.GetCell(sumRange[i].Item1, sumRange[i].Item2);
                            if (cSum != null) totalSum += cSum.NumericValue;
                        }
                    }
                    res.SetNumber(totalSum);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return res;
                }
                else if (funcName == "AVERAGEIF" && args.Length >= 2)
                {
                    var critRange = ParseRange(args[0]);
                    string crit = args[1];
                    var avgRange = args.Length >= 3 ? ParseRange(args[2]) : critRange;

                    double totalSum = 0.0;
                    int matchCount = 0;
                    int n = Math.Min(critRange.Count, avgRange.Count);
                    for (int i = 0; i < n; i++)
                    {
                        CellData cCrit = sheet.GetCell(critRange[i].Item1, critRange[i].Item2);
                        if (MatchesCriteria(cCrit, crit, sheet))
                        {
                            CellData cAvg = sheet.GetCell(avgRange[i].Item1, avgRange[i].Item2);
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
                    List<double> vals = new List<double>();
                    foreach (var a in args)
                    {
                        var r = ParseRange(a);
                        if (r.Count > 0)
                        {
                            foreach (var p in r)
                            {
                                var c = sheet.GetCell(p.Item1, p.Item2);
                                if (c != null && c.Type == CellType.Number) vals.Add(c.NumericValue);
                            }
                        }
                        else
                        {
                            vals.Add(ParseAndEvaluateNumeric(a, sheet));
                        }
                    }
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
                    var r = ParseRange(args[0]);
                    int k = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    List<double> vals = new List<double>();
                    foreach (var p in r)
                    {
                        var c = sheet.GetCell(p.Item1, p.Item2);
                        if (c != null && c.Type == CellType.Number) vals.Add(c.NumericValue);
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
                    var r = ParseRange(args[0]);
                    int k = (int)ParseAndEvaluateNumeric(args[1], sheet);
                    List<double> vals = new List<double>();
                    foreach (var p in r)
                    {
                        var c = sheet.GetCell(p.Item1, p.Item2);
                        if (c != null && c.Type == CellType.Number) vals.Add(c.NumericValue);
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
                    var range = ParseRange(args[1]);
                    int targetColOffset = (int)ParseAndEvaluateNumeric(args[2], sheet) - 1;

                    if (range.Count > 0)
                    {
                        uint minR = range[0].Item1;
                        uint maxR = range[range.Count - 1].Item1;
                        uint minC = range[0].Item2;

                        for (uint r = minR; r <= maxR; r++)
                        {
                            CellData keyCell = sheet.GetCell(r, minC);
                            string kVal = keyCell != null ? (!string.IsNullOrEmpty(keyCell.EvaluatedStr) ? keyCell.EvaluatedStr : keyCell.RawValue).Trim().ToUpper() : "";

                            if (kVal == lookupKey)
                            {
                                CellData matchCell = sheet.GetCell(r, (uint)(minC + targetColOffset));
                                if (matchCell != null)
                                {
                                    res.NumericValue = matchCell.NumericValue;
                                    res.EvaluatedStr = matchCell.EvaluatedStr;
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
                else if (funcName == "HLOOKUP" && args.Length >= 3)
                {
                    string lookupKey = GetStringValue(args[0], sheet).Trim().ToUpper();
                    var range = ParseRange(args[1]);
                    int targetRowOffset = (int)ParseAndEvaluateNumeric(args[2], sheet) - 1;

                    if (range.Count > 0)
                    {
                        uint minR = range[0].Item1;
                        uint minC = range[0].Item2;
                        uint maxC = range[range.Count - 1].Item2;

                        for (uint c = minC; c <= maxC; c++)
                        {
                            CellData keyCell = sheet.GetCell(minR, c);
                            string kVal = keyCell != null ? (!string.IsNullOrEmpty(keyCell.EvaluatedStr) ? keyCell.EvaluatedStr : keyCell.RawValue).Trim().ToUpper() : "";

                            if (kVal == lookupKey)
                            {
                                CellData matchCell = sheet.GetCell((uint)(minR + targetRowOffset), c);
                                if (matchCell != null)
                                {
                                    res.NumericValue = matchCell.NumericValue;
                                    res.EvaluatedStr = matchCell.EvaluatedStr;
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
                    var lookupRange = ParseRange(args[1]);
                    var returnRange = ParseRange(args[2]);

                    int n = Math.Min(lookupRange.Count, returnRange.Count);
                    for (int i = 0; i < n; i++)
                    {
                        CellData keyCell = sheet.GetCell(lookupRange[i].Item1, lookupRange[i].Item2);
                        string kVal = keyCell != null ? (!string.IsNullOrEmpty(keyCell.EvaluatedStr) ? keyCell.EvaluatedStr : keyCell.RawValue).Trim().ToUpper() : "";

                        if (kVal == lookupKey)
                        {
                            CellData retCell = sheet.GetCell(returnRange[i].Item1, returnRange[i].Item2);
                            if (retCell != null)
                            {
                                res.NumericValue = retCell.NumericValue;
                                res.EvaluatedStr = retCell.EvaluatedStr;
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
                    var range = ParseRange(args[0]);
                    int rowIdx = (int)ParseAndEvaluateNumeric(args[1], sheet) - 1;
                    int colIdx = args.Length >= 3 ? (int)ParseAndEvaluateNumeric(args[2], sheet) - 1 : 0;

                    if (range.Count > 0)
                    {
                        uint minR = range[0].Item1;
                        uint minC = range[0].Item2;
                        CellData cell = sheet.GetCell((uint)(minR + Math.Max(0, rowIdx)), (uint)(minC + Math.Max(0, colIdx)));
                        if (cell != null)
                        {
                            res.NumericValue = cell.NumericValue;
                            res.EvaluatedStr = cell.EvaluatedStr;
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
                    var range = ParseRange(args[1]);

                    for (int i = 0; i < range.Count; i++)
                    {
                        CellData c = sheet.GetCell(range[i].Item1, range[i].Item2);
                        string val = c != null ? (!string.IsNullOrEmpty(c.EvaluatedStr) ? c.EvaluatedStr : c.RawValue).Trim().ToUpper() : "";
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
                    var baseRange = ParseRange(args[0]);
                    if (baseRange.Count > 0)
                    {
                        int rOffset = (int)ParseAndEvaluateNumeric(args[1], sheet);
                        int cOffset = (int)ParseAndEvaluateNumeric(args[2], sheet);
                        uint targetR = (uint)Math.Max(1, (int)baseRange[0].Item1 + rOffset);
                        uint targetC = (uint)Math.Max(1, (int)baseRange[0].Item2 + cOffset);
                        CellData c = sheet.GetCell(targetR, targetC);
                        if (c != null)
                        {
                            res.NumericValue = c.NumericValue;
                            res.EvaluatedStr = c.EvaluatedStr;
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
                        var pos = SparseMatrix.ParseCellAddress(refText);
                        CellData c = sheet.GetCell(pos.Item1, pos.Item2);
                        if (c != null)
                        {
                            res.NumericValue = c.NumericValue;
                            res.EvaluatedStr = c.EvaluatedStr;
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
            }

            // General Arithmetic Evaluation (e.g. =B2*C2 + D5)
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
                res.SetString(clean.Substring(1, clean.Length - 2));
                return;
            }

            // Tự động đệ quy đánh giá hàm lồng nhau (Nested Function Call e.g. IF(...), AVERAGE(...), SUM(...))
            if (clean.Contains("(") && clean.EndsWith(")"))
            {
                string formulaCall = clean.StartsWith("=") ? clean : ("=" + clean);
                CellData evalInner = EvaluateFormula(formulaCall, sheet);
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
                var pos = SparseMatrix.ParseCellAddress(clean);
                CellData c = sheet.GetCell(pos.Item1, pos.Item2);
                if (c != null)
                {
                    res.Type = c.Type;
                    res.NumericValue = c.NumericValue;
                    res.EvaluatedStr = c.EvaluatedStr;
                    res.RawValue = c.RawValue;
                }
                else
                {
                    res.SetString("");
                }
                return;
            }

            // Xử lý biểu thức toán học trong nhánh rẽ (ví dụ A1*0.1 hay B2+C2)
            if (clean.Contains("+") || clean.Contains("-") || clean.Contains("*") || clean.Contains("/") || clean.Contains("^"))
            {
                double dVal = ParseAndEvaluateNumeric(clean, sheet);
                if (!double.IsNaN(dVal) && !double.IsInfinity(dVal))
                {
                    res.SetNumber(dVal);
                    res.EvaluatedStr = FormatCellDisplay(res);
                    return;
                }
            }

            res.SetString(clean);
        }

        private static bool MatchesCriteria(CellData cell, string criteria, SparseMatrix sheet)
        {
            if (cell == null) return false;
            string crit = (criteria ?? "").Trim();
            if (crit.StartsWith("\"") && crit.EndsWith("\"") && crit.Length >= 2)
                crit = crit.Substring(1, crit.Length - 2);

            string cellStr = !string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : cell.RawValue;
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

            // So sánh trực tiếp
            double dCrit;
            if (double.TryParse(crit, NumberStyles.Any, CultureInfo.InvariantCulture, out dCrit))
            {
                return Math.Abs(cellNum - dCrit) < 0.000001;
            }

            return string.Equals(cellStr, crit, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStringValue(string token, SparseMatrix sheet)
        {
            string clean = (token ?? "").Trim();
            if (clean.StartsWith("\"") && clean.EndsWith("\"") && clean.Length >= 2)
            {
                return clean.Substring(1, clean.Length - 2);
            }
            if (IsCellReference(clean))
            {
                var pos = SparseMatrix.ParseCellAddress(clean);
                CellData c = sheet.GetCell(pos.Item1, pos.Item2);
                if (c != null) return !string.IsNullOrEmpty(c.EvaluatedStr) ? c.EvaluatedStr : c.RawValue;
                return "";
            }
            return clean;
        }

        private static bool EvaluateCondition(string condStr, SparseMatrix sheet)
        {
            string clean = (condStr ?? "").Trim();
            if (string.IsNullOrEmpty(clean)) return false;
            if (clean.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
            if (clean.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return false;

            if (clean.StartsWith("IS", StringComparison.OrdinalIgnoreCase) || clean.Contains("("))
            {
                CellData eval = EvaluateFormula(clean.StartsWith("=") ? clean : ("=" + clean), sheet);
                if (eval != null)
                {
                    if (eval.EvaluatedStr != null && eval.EvaluatedStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
                    if (eval.NumericValue > 0) return true;
                    return false;
                }
            }

            string[] ops = new string[] { ">=", "<=", "<>", "!=", ">", "<", "=" };

            foreach (var op in ops)
            {
                int opIdx = clean.IndexOf(op);
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
            if (i == 0 || i == s.Length) return false;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            return (i == s.Length);
        }

        public static double ParseAndEvaluateNumeric(string expr, SparseMatrix sheet)
        {
            string clean = (expr ?? "").Trim();
            if (string.IsNullOrEmpty(clean)) return 0.0;

            // Cell Reference
            if (IsCellReference(clean))
            {
                var pos = SparseMatrix.ParseCellAddress(clean);
                CellData cell = sheet.GetCell(pos.Item1, pos.Item2);
                return cell != null ? cell.NumericValue : 0.0;
            }

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

            double val;
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val;
            if (double.TryParse(clean, out val)) return val;
            return 0.0;
        }

        private static int FindLowestPrecedenceOp(string str, char op1, char op2)
        {
            int depth = 0;
            for (int i = str.Length - 1; i >= 1; i--)
            {
                char c = str[i];
                if (c == ')') depth++;
                else if (c == '(') depth--;
                else if (depth == 0 && (c == op1 || c == op2))
                {
                    // Avoid unary minus
                    if (c == '-' && (str[i - 1] == '+' || str[i - 1] == '-' || str[i - 1] == '*' || str[i - 1] == '/' || str[i - 1] == '^' || str[i - 1] == '('))
                        continue;
                    return i;
                }
            }
            return -1;
        }

        public static List<Tuple<uint, uint>> ParseRange(string rangeStr)
        {
            var res = new List<Tuple<uint, uint>>();
            string clean = (rangeStr ?? "").Trim().Replace("$", "");
            int colonPos = clean.IndexOf(':');
            if (colonPos < 0)
            {
                if (IsCellReference(clean))
                {
                    res.Add(SparseMatrix.ParseCellAddress(clean));
                }
                return res;
            }

            string startAddr = clean.Substring(0, colonPos);
            string endAddr = clean.Substring(colonPos + 1);

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
            string current = "";
            int depth = 0;
            bool quote = false;

            foreach (char c in argsStr)
            {
                if (c == '"') quote = !quote;
                else if (c == '(' && !quote) depth++;
                else if (c == ')' && !quote) depth--;
                else if (c == ',' && depth == 0 && !quote)
                {
                    list.Add(current.Trim());
                    current = "";
                    continue;
                }
                current += c;
            }
            if (!string.IsNullOrEmpty(current)) list.Add(current.Trim());
            return list.ToArray();
        }

        private static List<double> CollectNumericValues(string[] args, SparseMatrix sheet)
        {
            List<double> values = new List<double>();
            foreach (var arg in args)
            {
                var range = ParseRange(arg);
                if (range.Count > 0)
                {
                    foreach (var p in range)
                    {
                        CellData c = sheet.GetCell(p.Item1, p.Item2);
                        if (c != null && c.Type == CellType.Number)
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
            var range = ParseRange(token);
            if (range.Count > 0)
            {
                return sheet.GetCell(range[0].Item1, range[0].Item2);
            }
            return EvaluateFormula("=" + token.Trim(), sheet);
        }
    }
}

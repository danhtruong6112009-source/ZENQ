using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace ZenQOffice.Core
{
    public enum DataValidationType
    {
        AnyValue = 0,
        WholeNumber = 1,
        Decimal = 2,
        List = 3,
        Date = 4,
        Time = 5,
        TextLength = 6,
        Custom = 7
    }

    public enum DataValidationOperator
    {
        Between = 0,
        NotBetween = 1,
        Equal = 2,
        NotEqual = 3,
        GreaterThan = 4,
        LessThan = 5,
        GreaterThanOrEqual = 6,
        LessThanOrEqual = 7
    }

    public enum DataValidationErrorStyle
    {
        Stop = 0,        // Dừng lại - Không cho phép nhập giá trị sai
        Warning = 1,     // Cảnh báo - Hỏi người dùng có muốn tiếp tục hay không
        Information = 2  // Thông tin - Báo cho người dùng biết nhưng vẫn chấp nhận
    }

    public class DataValidationResult
    {
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
        public string ErrorTitle { get; set; } = "";
        public DataValidationErrorStyle ErrorStyle { get; set; } = DataValidationErrorStyle.Stop;
    }

    public class DataValidationRule
    {
        public string Id { get; set; }
        public uint StartRow { get; set; }
        public uint StartCol { get; set; }
        public uint EndRow { get; set; }
        public uint EndCol { get; set; }

        public DataValidationType ValidationType { get; set; } = DataValidationType.AnyValue;
        public DataValidationOperator Operator { get; set; } = DataValidationOperator.Between;

        public string Formula1 { get; set; } = "";
        public string Formula2 { get; set; } = "";

        public bool AllowBlank { get; set; } = true;
        public bool ShowDropdown { get; set; } = true;

        public bool ShowInputMessage { get; set; } = true;
        public string InputTitle { get; set; } = "";
        public string InputMessage { get; set; } = "";

        public bool ShowErrorMessage { get; set; } = true;
        public DataValidationErrorStyle ErrorStyle { get; set; } = DataValidationErrorStyle.Stop;
        public string ErrorTitle { get; set; } = "Giá trị không hợp lệ";
        public string ErrorMessage { get; set; } = "Giá trị bạn vừa nhập không đáp ứng các ràng buộc xác thực dữ liệu được thiết lập cho ô này.";

        public DataValidationRule()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        public DataValidationRule(uint startRow, uint startCol, uint endRow, uint endCol, DataValidationType type = DataValidationType.AnyValue)
        {
            Id = Guid.NewGuid().ToString("N");
            StartRow = Math.Min(startRow, endRow);
            StartCol = Math.Min(startCol, endCol);
            EndRow = Math.Max(startRow, endRow);
            EndCol = Math.Max(startCol, endCol);
            ValidationType = type;
        }

        public bool ContainsCell(uint row, uint col)
        {
            return row >= StartRow && row <= EndRow && col >= StartCol && col <= EndCol;
        }

        public List<string> GetListItems(SparseMatrix matrix = null)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(Formula1)) return list;

            string clean = Formula1.Trim();
            // Check if Formula1 is a range reference like "A1:A10" or "=$A$1:$A$10"
            if (clean.StartsWith("=")) clean = clean.Substring(1).Trim();

            if (clean.Contains(":") && matrix != null)
            {
                string[] parts = clean.Split(':');
                if (parts.Length == 2)
                {
                    uint r1, c1, r2, c2;
                    if (SparseMatrix.TryParseCellReference(parts[0], out r1, out c1) &&
                        SparseMatrix.TryParseCellReference(parts[1], out r2, out c2))
                    {
                        uint minR = Math.Min(r1, r2);
                        uint maxR = Math.Max(r1, r2);
                        uint minC = Math.Min(c1, c2);
                        uint maxC = Math.Max(c1, c2);

                        for (uint r = minR; r <= maxR; r++)
                        {
                            for (uint c = minC; c <= maxC; c++)
                            {
                                var cell = matrix.GetCell(r, c);
                                if (cell != null)
                                {
                                    string txt = cell.GetDisplayText();
                                    if (!string.IsNullOrEmpty(txt))
                                    {
                                        list.Add(txt);
                                    }
                                }
                            }
                        }
                        return list;
                    }
                }
            }

            // Otherwise, comma-separated list: "Apple, Banana, Orange" or "Hà Nội; Đà Nẵng; TP.HCM"
            char sep = clean.Contains(";") ? ';' : ',';
            string[] items = clean.Split(new char[] { sep }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                list.Add(item.Trim());
            }
            return list;
        }

        public DataValidationResult Validate(object rawValue, SparseMatrix matrix = null, uint cellRow = 0, uint cellCol = 0)
        {
            var res = new DataValidationResult
            {
                ErrorStyle = this.ErrorStyle,
                ErrorTitle = string.IsNullOrEmpty(this.ErrorTitle) ? "Giá trị không hợp lệ" : this.ErrorTitle,
                ErrorMessage = string.IsNullOrEmpty(this.ErrorMessage) ? "Giá trị nhập vào vi phạm quy tắc xác thực ô." : this.ErrorMessage
            };

            string strVal = rawValue != null ? rawValue.ToString() : "";
            if (string.IsNullOrEmpty(strVal))
            {
                res.IsValid = AllowBlank;
                return res;
            }

            if (ValidationType == DataValidationType.AnyValue)
            {
                res.IsValid = true;
                return res;
            }

            if (ValidationType == DataValidationType.List)
            {
                var items = GetListItems(matrix);
                if (items.Count == 0)
                {
                    res.IsValid = true;
                    return res;
                }

                bool found = false;
                foreach (var item in items)
                {
                    if (string.Equals(item.Trim(), strVal.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                res.IsValid = found;
                return res;
            }

            if (ValidationType == DataValidationType.WholeNumber)
            {
                long val;
                if (!long.TryParse(strVal.Trim(), out val))
                {
                    res.IsValid = false;
                    return res;
                }
                long f1 = 0, f2 = 0;
                long.TryParse(ResolveFormulaValue(Formula1, matrix), out f1);
                long.TryParse(ResolveFormulaValue(Formula2, matrix), out f2);
                res.IsValid = EvaluateNumericOperator(val, f1, f2, Operator);
                return res;
            }

            if (ValidationType == DataValidationType.Decimal)
            {
                double val;
                if (!double.TryParse(strVal.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                {
                    res.IsValid = false;
                    return res;
                }
                double f1 = 0.0, f2 = 0.0;
                double.TryParse(ResolveFormulaValue(Formula1, matrix).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out f1);
                double.TryParse(ResolveFormulaValue(Formula2, matrix).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out f2);
                res.IsValid = EvaluateNumericOperator(val, f1, f2, Operator);
                return res;
            }

            if (ValidationType == DataValidationType.TextLength)
            {
                int len = strVal.Length;
                int f1 = 0, f2 = 0;
                int.TryParse(ResolveFormulaValue(Formula1, matrix), out f1);
                int.TryParse(ResolveFormulaValue(Formula2, matrix), out f2);
                res.IsValid = EvaluateNumericOperator(len, f1, f2, Operator);
                return res;
            }

            if (ValidationType == DataValidationType.Date)
            {
                DateTime dt;
                if (!DateTime.TryParse(strVal.Trim(), out dt))
                {
                    res.IsValid = false;
                    return res;
                }
                DateTime d1, d2;
                DateTime.TryParse(ResolveFormulaValue(Formula1, matrix), out d1);
                DateTime.TryParse(ResolveFormulaValue(Formula2, matrix), out d2);
                res.IsValid = EvaluateDateOperator(dt, d1, d2, Operator);
                return res;
            }

            if (ValidationType == DataValidationType.Custom)
            {
                // Custom formula evaluation (e.g. "=A1>0" or "=ISNUMBER(...)")
                if (!string.IsNullOrEmpty(Formula1))
                {
                    string f = Formula1.Trim();
                    if (!f.StartsWith("=")) f = "=" + f;
                    var eval = FormulaEngine.EvaluateFormula(f, matrix ?? new SparseMatrix());
                    if (eval != null)
                    {
                        if (string.Equals(eval.EvaluatedStr, "TRUE", StringComparison.OrdinalIgnoreCase))
                        {
                            res.IsValid = true;
                            return res;
                        }
                        if (string.Equals(eval.EvaluatedStr, "FALSE", StringComparison.OrdinalIgnoreCase))
                        {
                            res.IsValid = false;
                            return res;
                        }
                        if (eval.Type == CellType.Number)
                        {
                            res.IsValid = eval.NumericValue != 0;
                            return res;
                        }
                        bool b;
                        if (bool.TryParse(eval.EvaluatedStr, out b))
                        {
                            res.IsValid = b;
                            return res;
                        }
                    }
                }
                res.IsValid = true;
                return res;
            }

            res.IsValid = true;
            return res;
        }

        private string ResolveFormulaValue(string f, SparseMatrix matrix)
        {
            if (string.IsNullOrEmpty(f)) return "0";
            string clean = f.Trim();
            if (clean.StartsWith("=")) clean = clean.Substring(1).Trim();

            uint r, c;
            if (matrix != null && SparseMatrix.TryParseCellReference(clean, out r, out c))
            {
                var cell = matrix.GetCell(r, c);
                if (cell != null) return cell.GetDisplayText();
            }
            return clean;
        }

        private static bool EvaluateNumericOperator(double val, double v1, double v2, DataValidationOperator op)
        {
            switch (op)
            {
                case DataValidationOperator.Between:
                    return val >= Math.Min(v1, v2) && val <= Math.Max(v1, v2);
                case DataValidationOperator.NotBetween:
                    return val < Math.Min(v1, v2) || val > Math.Max(v1, v2);
                case DataValidationOperator.Equal:
                    return Math.Abs(val - v1) < 0.0000001;
                case DataValidationOperator.NotEqual:
                    return Math.Abs(val - v1) >= 0.0000001;
                case DataValidationOperator.GreaterThan:
                    return val > v1;
                case DataValidationOperator.LessThan:
                    return val < v1;
                case DataValidationOperator.GreaterThanOrEqual:
                    return val >= v1;
                case DataValidationOperator.LessThanOrEqual:
                    return val <= v1;
                default:
                    return true;
            }
        }

        private static bool EvaluateDateOperator(DateTime val, DateTime d1, DateTime d2, DataValidationOperator op)
        {
            switch (op)
            {
                case DataValidationOperator.Between:
                    return val >= d1 && val <= d2;
                case DataValidationOperator.NotBetween:
                    return val < d1 || val > d2;
                case DataValidationOperator.Equal:
                    return val.Date == d1.Date;
                case DataValidationOperator.NotEqual:
                    return val.Date != d1.Date;
                case DataValidationOperator.GreaterThan:
                    return val > d1;
                case DataValidationOperator.LessThan:
                    return val < d1;
                case DataValidationOperator.GreaterThanOrEqual:
                    return val >= d1;
                case DataValidationOperator.LessThanOrEqual:
                    return val <= d1;
                default:
                    return true;
            }
        }

        public DataValidationRule Clone()
        {
            return new DataValidationRule(StartRow, StartCol, EndRow, EndCol, ValidationType)
            {
                Id = Guid.NewGuid().ToString("N"),
                Operator = this.Operator,
                Formula1 = this.Formula1,
                Formula2 = this.Formula2,
                AllowBlank = this.AllowBlank,
                ShowDropdown = this.ShowDropdown,
                ShowInputMessage = this.ShowInputMessage,
                InputTitle = this.InputTitle,
                InputMessage = this.InputMessage,
                ShowErrorMessage = this.ShowErrorMessage,
                ErrorStyle = this.ErrorStyle,
                ErrorTitle = this.ErrorTitle,
                ErrorMessage = this.ErrorMessage
            };
        }
    }

    #region Formula Auditing & Scenario Models
    public class FormulaAuditingArrow
    {
        public uint FromRow { get; set; }
        public uint FromCol { get; set; }
        public uint ToRow { get; set; }
        public uint ToCol { get; set; }
        public bool IsError { get; set; } = false;
        public Color ArrowColor { get; set; } = Color.FromArgb(37, 99, 235); // Blue for precedents/dependents, Red for error
    }

    public class ScenarioModel
    {
        public string Id { get; set; }
        public string Name { get; set; } = "Kịch bản 1";
        public string Comment { get; set; } = "";
        public List<Tuple<uint, uint>> ChangingCells { get; set; } = new List<Tuple<uint, uint>>();
        public List<string> Values { get; set; } = new List<string>();

        public ScenarioModel()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        public ScenarioModel Clone()
        {
            var sc = new ScenarioModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = this.Name,
                Comment = this.Comment,
                ChangingCells = new List<Tuple<uint, uint>>(this.ChangingCells),
                Values = new List<string>(this.Values)
            };
            return sc;
        }
    }

    public class DefinedNameModel
    {
        public string Name { get; set; } = "";
        public string RefersTo { get; set; } = "=Sheet1!$A$1";
        public string Scope { get; set; } = "Toàn bảng tính";
        public string Comment { get; set; } = "";

        public DefinedNameModel() { }
        public DefinedNameModel(string name, string refersTo, string scope = "Toàn bảng tính", string comment = "")
        {
            Name = name;
            RefersTo = refersTo;
            Scope = scope;
            Comment = comment;
        }
    }

    public static class FormulaAuditingEngine
    {
        private static readonly System.Text.RegularExpressions.Regex CellRefRegex = 
            new System.Text.RegularExpressions.Regex(@"\$?([A-Za-z]+)\$?(\d+)(?::\$?([A-Za-z]+)\$?(\d+))?", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static List<Tuple<uint, uint>> ExtractPrecedentCells(string formula)
        {
            var result = new List<Tuple<uint, uint>>();
            if (string.IsNullOrWhiteSpace(formula)) return result;

            var matches = CellRefRegex.Matches(formula);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                string c1Str = m.Groups[1].Value;
                uint r1;
                uint.TryParse(m.Groups[2].Value, out r1);
                uint c1 = SparseMatrix.ColNameToIndex(c1Str);

                if (m.Groups[3].Success && m.Groups[4].Success)
                {
                    string c2Str = m.Groups[3].Value;
                    uint r2;
                    uint.TryParse(m.Groups[4].Value, out r2);
                    uint c2 = SparseMatrix.ColNameToIndex(c2Str);

                    uint minR = Math.Min(r1, r2);
                    uint maxR = Math.Max(r1, r2);
                    uint minC = Math.Min(c1, c2);
                    uint maxC = Math.Max(c1, c2);

                    for (uint r = minR; r <= maxR; r++)
                    {
                        for (uint c = minC; c <= maxC; c++)
                        {
                            result.Add(Tuple.Create(r, c));
                        }
                    }
                }
                else
                {
                    if (r1 > 0 && c1 > 0)
                    {
                        result.Add(Tuple.Create(r1, c1));
                    }
                }
            }
            return result;
        }

        public static bool FormulaContainsCell(string formula, uint targetRow, uint targetCol)
        {
            var cells = ExtractPrecedentCells(formula);
            foreach (var cell in cells)
            {
                if (cell.Item1 == targetRow && cell.Item2 == targetCol) return true;
            }
            return false;
        }
    }
    #endregion
}

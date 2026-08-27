using System;
using System.Collections.Generic;
using System.Text;

namespace GenOffice.Core
{
    public enum CellType { Empty, String, Number, Formula, Boolean, Error }

    public class CellBorderSettings
    {
        public bool Top { get; set; }
        public bool Bottom { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public int Color { get; set; }
        public int Thickness { get; set; } // 1: Thin, 2: Medium, 3: Thick, 4: Double

        // Independent per-edge thickness and color for seamless additive layering ("kẽ chồng")
        public int TopThickness { get; set; }
        public int BottomThickness { get; set; }
        public int LeftThickness { get; set; }
        public int RightThickness { get; set; }
        public int TopColor { get; set; }
        public int BottomColor { get; set; }
        public int LeftColor { get; set; }
        public int RightColor { get; set; }

        // Diagonal Border Support (Đường kẻ chéo ô chia đôi tiêu đề 2 phần)
        public bool DiagonalDown { get; set; } // ╲ (Góc trên-trái xuống góc dưới-phải)
        public bool DiagonalUp { get; set; }   // ╱ (Góc dưới-trái lên góc trên-phải)
        public int DiagonalThickness { get; set; } // 1: Thin, 2: Medium, 3: Thick
        public int DiagonalColor { get; set; }

        public CellBorderSettings()
        {
            Top = false;
            Bottom = false;
            Left = false;
            Right = false;
            Color = unchecked((int)0xFF1E293B); // Dark Slate 800
            Thickness = 1;
            TopThickness = 1;
            BottomThickness = 1;
            LeftThickness = 1;
            RightThickness = 1;
            TopColor = 0;
            BottomColor = 0;
            LeftColor = 0;
            RightColor = 0;
            DiagonalDown = false;
            DiagonalUp = false;
            DiagonalThickness = 1;
            DiagonalColor = 0;
        }
    }

    public class CellFormat
    {
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public int TextColor { get; set; }
        public int BgColor { get; set; }
        public int Align { get; set; } // 0: Left, 1: Center, 2: Right
        public int VAlign { get; set; } // 0: Top, 1: Middle, 2: Bottom
        public bool WrapText { get; set; }
        public string NumberFormat { get; set; } // General, Number, Currency, Percent, Comma, Date, Text
        public int DecimalPlaces { get; set; }

        public CellBorderSettings Borders { get; set; }

        // Merge Cell Range Support
        public bool IsMergedParent { get; set; }
        public int MergeRowSpan { get; set; }
        public int MergeColSpan { get; set; }
        public bool IsMergedChild { get; set; }
        public uint MergedParentRow { get; set; }
        public uint MergedParentCol { get; set; }

        // Diagonal Split Header (Chia đôi ô tiêu đề góc bảng)
        public bool IsDiagonalHeader { get; set; }
        public string DiagonalTopRightText { get; set; }
        public string DiagonalBottomLeftText { get; set; }

        public CellFormat()
        {
            FontName = "Calibri";
            FontSize = 11.0f;
            Bold = false;
            Italic = false;
            Underline = false;
            Strikethrough = false;
            TextColor = unchecked((int)0xFF000000);
            BgColor = unchecked((int)0xFFFFFFFF);
            Align = 0;
            VAlign = 1;
            WrapText = false;
            NumberFormat = "General";
            DecimalPlaces = 2;
            Borders = new CellBorderSettings();
            IsMergedParent = false;
            MergeRowSpan = 1;
            MergeColSpan = 1;
            IsMergedChild = false;
            MergedParentRow = 0;
            MergedParentCol = 0;
            IsDiagonalHeader = false;
            DiagonalTopRightText = "";
            DiagonalBottomLeftText = "";
        }

        public CellFormat Clone()
        {
            CellFormat copy = new CellFormat();
            copy.FontName = FontName;
            copy.FontSize = FontSize;
            copy.Bold = Bold;
            copy.Italic = Italic;
            copy.Underline = Underline;
            copy.Strikethrough = Strikethrough;
            copy.TextColor = TextColor;
            copy.BgColor = BgColor;
            copy.Align = Align;
            copy.VAlign = VAlign;
            copy.WrapText = WrapText;
            copy.NumberFormat = NumberFormat;
            copy.DecimalPlaces = DecimalPlaces;
            copy.Borders.Top = Borders.Top;
            copy.Borders.Bottom = Borders.Bottom;
            copy.Borders.Left = Borders.Left;
            copy.Borders.Right = Borders.Right;
            copy.Borders.Color = Borders.Color;
            copy.Borders.Thickness = Borders.Thickness;
            copy.Borders.TopThickness = Borders.TopThickness;
            copy.Borders.BottomThickness = Borders.BottomThickness;
            copy.Borders.LeftThickness = Borders.LeftThickness;
            copy.Borders.RightThickness = Borders.RightThickness;
            copy.Borders.TopColor = Borders.TopColor;
            copy.Borders.BottomColor = Borders.BottomColor;
            copy.Borders.LeftColor = Borders.LeftColor;
            copy.Borders.RightColor = Borders.RightColor;
            copy.Borders.DiagonalDown = Borders.DiagonalDown;
            copy.Borders.DiagonalUp = Borders.DiagonalUp;
            copy.Borders.DiagonalThickness = Borders.DiagonalThickness;
            copy.Borders.DiagonalColor = Borders.DiagonalColor;
            copy.IsMergedParent = IsMergedParent;
            copy.MergeRowSpan = MergeRowSpan;
            copy.MergeColSpan = MergeColSpan;
            copy.IsMergedChild = IsMergedChild;
            copy.MergedParentRow = MergedParentRow;
            copy.MergedParentCol = MergedParentCol;
            copy.IsDiagonalHeader = IsDiagonalHeader;
            copy.DiagonalTopRightText = DiagonalTopRightText;
            copy.DiagonalBottomLeftText = DiagonalBottomLeftText;
            return copy;
        }
    }

    public class CellData
    {
        public CellType Type { get; set; }
        public string RawValue { get; set; }
        public string FormulaStr { get; set; }
        public double NumericValue { get; set; }
        public string EvaluatedStr { get; set; }
        public string Comment { get; set; }
        public string Hyperlink { get; set; }
        public CellFormat Format { get; set; }

        public CellData()
        {
            Type = CellType.Empty;
            RawValue = "";
            FormulaStr = "";
            NumericValue = 0.0;
            EvaluatedStr = "";
            Comment = "";
            Hyperlink = "";
            Format = new CellFormat();
        }

        public void SetString(string val)
        {
            Type = CellType.String;
            RawValue = val ?? "";
            FormulaStr = "";
            EvaluatedStr = RawValue;
            double num;
            if (double.TryParse(RawValue, out num))
            {
                NumericValue = num;
            }
        }

        public void SetNumber(double val)
        {
            Type = CellType.Number;
            NumericValue = val;
            RawValue = val.ToString();
            FormulaStr = "";
            EvaluatedStr = RawValue;
        }

        public void SetFormula(string fStr)
        {
            Type = CellType.Formula;
            FormulaStr = fStr ?? "";
            RawValue = FormulaStr;
        }

        public string GetDisplayText()
        {
            if (!string.IsNullOrEmpty(EvaluatedStr)) return EvaluatedStr;
            if (!string.IsNullOrEmpty(RawValue)) return RawValue;
            if (Type == CellType.Number) return NumericValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "";
        }

        public CellData Clone()
        {
            CellData copy = new CellData();
            copy.Type = this.Type;
            copy.RawValue = this.RawValue;
            copy.FormulaStr = this.FormulaStr;
            copy.NumericValue = this.NumericValue;
            copy.EvaluatedStr = this.EvaluatedStr;
            copy.Format = this.Format != null ? this.Format.Clone() : new CellFormat();
            return copy;
        }
    }

    public class SparseMatrix
    {
        private Dictionary<ulong, CellData> grid = new Dictionary<ulong, CellData>();

        public static bool TryParseCellReference(string refStr, out uint row, out uint col)
        {
            row = 1;
            col = 1;
            if (string.IsNullOrEmpty(refStr)) return false;
            string clean = refStr.Trim().ToUpper().Replace("$", "");
            int i = 0;
            while (i < clean.Length && char.IsLetter(clean[i])) i++;
            if (i == 0 || i == clean.Length) return false;

            string colLetters = clean.Substring(0, i);
            string rowDigits = clean.Substring(i);

            uint parsedRow;
            if (!uint.TryParse(rowDigits, out parsedRow) || parsedRow == 0) return false;
            row = parsedRow;

            uint parsedCol = 0;
            for (int c = 0; c < colLetters.Length; c++)
            {
                parsedCol = parsedCol * 26 + (uint)(colLetters[c] - 'A' + 1);
            }
            col = parsedCol;
            return true;
        }

        public static string GetCellReference(uint row, uint col)
        {
            StringBuilder sb = new StringBuilder();
            uint tempCol = col;
            while (tempCol > 0)
            {
                uint rem = (tempCol - 1) % 26;
                sb.Insert(0, (char)('A' + rem));
                tempCol = (tempCol - 1) / 26;
            }
            sb.Append(row);
            return sb.ToString();
        }

        public Dictionary<ulong, CellData> CloneGrid()
        {
            Dictionary<ulong, CellData> dict = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                dict[kvp.Key] = kvp.Value != null ? kvp.Value.Clone() : new CellData();
            }
            return dict;
        }

        public void RestoreGrid(Dictionary<ulong, CellData> snapshot)
        {
            grid.Clear();
            if (snapshot != null)
            {
                foreach (var kvp in snapshot)
                {
                    grid[kvp.Key] = kvp.Value != null ? kvp.Value.Clone() : new CellData();
                }
            }
        }

        public static ulong EncodeKey(uint row, uint col)
        {
            return (((ulong)row) << 32) | (ulong)col;
        }

        public static Tuple<uint, uint> DecodeKey(ulong key)
        {
            return new Tuple<uint, uint>((uint)(key >> 32), (uint)(key & 0xFFFFFFFF));
        }

        public bool HasCell(uint row, uint col)
        {
            return grid.ContainsKey(EncodeKey(row, col));
        }

        public CellData GetCell(uint row, uint col)
        {
            CellData data;
            if (grid.TryGetValue(EncodeKey(row, col), out data)) return data;
            return null;
        }

        public CellData GetOrCreateCell(uint row, uint col)
        {
            ulong key = EncodeKey(row, col);
            CellData data;
            if (!grid.TryGetValue(key, out data))
            {
                data = new CellData();
                grid[key] = data;
            }
            return data;
        }

        public void SetCell(uint row, uint col, CellData cell)
        {
            if (cell == null)
            {
                grid.Remove(EncodeKey(row, col));
            }
            else
            {
                grid[EncodeKey(row, col)] = cell;
            }
        }

        public void SetCellText(uint row, uint col, string text)
        {
            string clean = (text ?? "").Trim();
            if (string.IsNullOrEmpty(clean))
            {
                grid.Remove(EncodeKey(row, col));
                return;
            }

            CellData cell = GetOrCreateCell(row, col);
            if (clean.StartsWith("="))
            {
                cell.SetFormula(clean);
            }
            else
            {
                double num;
                if (double.TryParse(clean, out num))
                {
                    cell.SetNumber(num);
                }
                else
                {
                    cell.SetString(text);
                }
            }
        }

        public IEnumerable<KeyValuePair<ulong, CellData>> GetAllCells()
        {
            return grid;
        }

        public void ClearCell(uint row, uint col)
        {
            grid.Remove(EncodeKey(row, col));
        }

        public void MergeCells(uint r1, uint c1, uint r2, uint c2)
        {
            uint minR = Math.Min(r1, r2);
            uint maxR = Math.Max(r1, r2);
            uint minC = Math.Min(c1, c2);
            uint maxC = Math.Max(c1, c2);

            if (minR == maxR && minC == maxC) return;

            CellData master = GetOrCreateCell(minR, minC);
            master.Format.IsMergedParent = true;
            master.Format.MergeRowSpan = (int)(maxR - minR + 1);
            master.Format.MergeColSpan = (int)(maxC - minC + 1);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    if (r == minR && c == minC) continue;
                    CellData child = GetOrCreateCell(r, c);
                    child.Format.IsMergedChild = true;
                    child.Format.MergedParentRow = minR;
                    child.Format.MergedParentCol = minC;
                }
            }
        }

        public void UnmergeCells(uint r1, uint c1, uint r2, uint c2)
        {
            uint minR = Math.Min(r1, r2);
            uint maxR = Math.Max(r1, r2);
            uint minC = Math.Min(c1, c2);
            uint maxC = Math.Max(c1, c2);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = GetCell(r, c);
                    if (cell != null && cell.Format != null)
                    {
                        cell.Format.IsMergedParent = false;
                        cell.Format.MergeRowSpan = 1;
                        cell.Format.MergeColSpan = 1;
                        cell.Format.IsMergedChild = false;
                        cell.Format.MergedParentRow = 0;
                        cell.Format.MergedParentCol = 0;
                    }
                }
            }
        }

        public void ClearRange(uint r1, uint c1, uint r2, uint c2)
        {
            uint minR = Math.Min(r1, r2);
            uint maxR = Math.Max(r1, r2);
            uint minC = Math.Min(c1, c2);
            uint maxC = Math.Max(c1, c2);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    ClearCell(r, c);
                }
            }
        }

        public void InsertRows(uint startRow, uint count)
        {
            if (count == 0) return;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (r >= startRow)
                {
                    newGrid[EncodeKey(r + count, c)] = kvp.Value;
                }
                else
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void DeleteRows(uint startRow, uint count)
        {
            if (count == 0) return;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            uint endRow = startRow + count - 1;
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (r < startRow)
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
                else if (r > endRow)
                {
                    newGrid[EncodeKey(r - count, c)] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void InsertCols(uint startCol, uint count)
        {
            if (count == 0) return;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (c >= startCol)
                {
                    newGrid[EncodeKey(r, c + count)] = kvp.Value;
                }
                else
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void ShiftCellsRight(uint minR, uint minC, uint maxR, uint maxC)
        {
            uint count = maxC - minC + 1;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (r >= minR && r <= maxR && c >= minC)
                {
                    newGrid[EncodeKey(r, c + count)] = kvp.Value;
                }
                else
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void ShiftCellsDown(uint minR, uint minC, uint maxR, uint maxC)
        {
            uint count = maxR - minR + 1;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (c >= minC && c <= maxC && r >= minR)
                {
                    newGrid[EncodeKey(r + count, c)] = kvp.Value;
                }
                else
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void ShiftCellsLeft(uint minR, uint minC, uint maxR, uint maxC)
        {
            uint count = maxC - minC + 1;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (r >= minR && r <= maxR)
                {
                    if (c >= minC && c <= maxC)
                    {
                        // Cell within deleted area - removed
                    }
                    else if (c > maxC)
                    {
                        newGrid[EncodeKey(r, c - count)] = kvp.Value;
                    }
                    else
                    {
                        newGrid[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void ShiftCellsUp(uint minR, uint minC, uint maxR, uint maxC)
        {
            uint count = maxR - minR + 1;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (c >= minC && c <= maxC)
                {
                    if (r >= minR && r <= maxR)
                    {
                        // Cell within deleted area - removed
                    }
                    else if (r > maxR)
                    {
                        newGrid[EncodeKey(r - count, c)] = kvp.Value;
                    }
                    else
                    {
                        newGrid[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public void DeleteCols(uint startCol, uint count)
        {
            if (count == 0) return;
            Dictionary<ulong, CellData> newGrid = new Dictionary<ulong, CellData>();
            uint endCol = startCol + count - 1;
            foreach (var kvp in grid)
            {
                var coord = DecodeKey(kvp.Key);
                uint r = coord.Item1;
                uint c = coord.Item2;
                if (c < startCol)
                {
                    newGrid[kvp.Key] = kvp.Value;
                }
                else if (c > endCol)
                {
                    newGrid[EncodeKey(r, c - count)] = kvp.Value;
                }
            }
            grid = newGrid;
        }

        public static string ColIndexToName(uint col)
        {
            if (col == 0) return "A";
            StringBuilder sb = new StringBuilder();
            while (col > 0)
            {
                uint rem = (col - 1) % 26;
                sb.Insert(0, (char)('A' + rem));
                col = (col - 1) / 26;
            }
            return sb.ToString();
        }

        public static uint ColNameToIndex(string colName)
        {
            if (string.IsNullOrEmpty(colName)) return 1;
            uint res = 0;
            string upper = colName.ToUpper();
            for (int i = 0; i < upper.Length; i++)
            {
                if (upper[i] >= 'A' && upper[i] <= 'Z')
                {
                    res = res * 26 + (uint)(upper[i] - 'A' + 1);
                }
            }
            return Math.Max(1, res);
        }

        public static Tuple<uint, uint> ParseCellAddress(string addr)
        {
            string clean = (addr ?? "").Trim().Replace("$", "");
            if (string.IsNullOrEmpty(clean)) return new Tuple<uint, uint>(1, 1);

            int excl = clean.LastIndexOf('!');
            if (excl >= 0 && excl < clean.Length - 1)
            {
                clean = clean.Substring(excl + 1).Trim();
            }

            int split = 0;
            while (split < clean.Length && char.IsLetter(clean[split])) split++;

            string colStr = clean.Substring(0, split);
            string rowStr = clean.Substring(split);

            uint col = ColNameToIndex(colStr);
            uint row = 1;
            uint.TryParse(rowStr, out row);
            if (row == 0) row = 1;

            return new Tuple<uint, uint>(row, col);
        }
    }
}

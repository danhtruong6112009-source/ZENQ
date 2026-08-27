using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class CalcSelectionFormat
    {
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public int Align { get; set; } // 0: Left, 1: Center, 2: Right, -1: Mixed
        public int VAlign { get; set; } // 0: Top, 1: Middle, 2: Bottom, -1: Mixed
        public bool WrapText { get; set; }
        public bool IsMerged { get; set; }
        public string NumberFormat { get; set; }
        public int DecimalPlaces { get; set; }
        public int TextColor { get; set; }
        public int BgColor { get; set; }

        public CalcSelectionFormat()
        {
            FontName = "Calibri";
            FontSize = 11.0f;
            Bold = false;
            Italic = false;
            Underline = false;
            Align = 0;
            VAlign = 1;
            WrapText = false;
            IsMerged = false;
            NumberFormat = "General";
            DecimalPlaces = 2;
            TextColor = unchecked((int)0xFF000000);
            BgColor = unchecked((int)0xFFFFFFFF);
        }
    }

    public class FormulaSuggestion
    {
        public string Name { get; set; }
        public string Syntax { get; set; }
        public string Description { get; set; }

        public FormulaSuggestion(string name, string syntax, string desc)
        {
            Name = name;
            Syntax = syntax;
            Description = desc;
        }
    }

    public class SparklineInfo
    {
        public string Type { get; set; } // "Line", "Column", "WinLoss"
        public uint DataStartRow { get; set; }
        public uint DataStartCol { get; set; }
        public uint DataEndRow { get; set; }
        public uint DataEndCol { get; set; }

        public SparklineInfo()
        {
            Type = "Line";
        }
    }

    public class CalcUndoSnapshot
    {
        public Dictionary<ulong, CellData> GridSnapshot { get; set; }
        public int ActiveSheetIndex { get; set; }
        public uint SelectedRow { get; set; }
        public uint SelectedCol { get; set; }
        public uint SelEndRow { get; set; }
        public uint SelEndCol { get; set; }
        public Dictionary<uint, int> ColWidths { get; set; }
        public Dictionary<uint, int> RowHeights { get; set; }

        public CalcUndoSnapshot(SparseMatrix sheet, int activeSheet, uint selR, uint selC, uint selEndR, uint selEndC, Dictionary<uint, int> colW, Dictionary<uint, int> rowH)
        {
            GridSnapshot = sheet != null ? sheet.CloneGrid() : new Dictionary<ulong, CellData>();
            ActiveSheetIndex = activeSheet;
            SelectedRow = selR;
            SelectedCol = selC;
            SelEndRow = selEndR;
            SelEndCol = selEndC;
            ColWidths = colW != null ? new Dictionary<uint, int>(colW) : new Dictionary<uint, int>();
            RowHeights = rowH != null ? new Dictionary<uint, int>(rowH) : new Dictionary<uint, int>();
        }
    }

    public class CalcSheetState
    {
        public uint SelectedRow { get; set; }
        public uint SelectedCol { get; set; }
        public uint SelEndRow { get; set; }
        public uint SelEndCol { get; set; }
        public uint ScrollRow { get; set; }
        public uint ScrollCol { get; set; }
        public Dictionary<uint, int> CustomColWidths { get; set; }
        public Dictionary<uint, int> CustomRowHeights { get; set; }
        public List<DocShapeObject> Shapes { get; set; }
        public List<ExcelTableInfo> Tables { get; set; }
        public Color TabColor { get; set; }

        public CalcSheetState()
        {
            SelectedRow = 1;
            SelectedCol = 1;
            SelEndRow = 1;
            SelEndCol = 1;
            ScrollRow = 1;
            ScrollCol = 1;
            CustomColWidths = new Dictionary<uint, int>();
            CustomRowHeights = new Dictionary<uint, int>();
            Shapes = new List<DocShapeObject>();
            Tables = new List<ExcelTableInfo>();
            TabColor = Color.Empty;
        }
    }

    public class CalcView
    {
        public List<SparseMatrix> Sheets { get; set; }
        public List<string> SheetNames { get; set; }
        public List<CalcSheetState> SheetStates { get; set; }
        private int _activeSheetIndex;
        public int ActiveSheetIndex
        {
            get { return _activeSheetIndex; }
            set
            {
                if (value >= 0 && value < Sheets.Count)
                {
                    SwitchToSheet(value);
                }
            }
        }

        private Stack<CalcUndoSnapshot> undoStack = new Stack<CalcUndoSnapshot>();
        private Stack<CalcUndoSnapshot> redoStack = new Stack<CalcUndoSnapshot>();

        public bool CanUndo { get { return undoStack.Count > 0; } }
        public bool CanRedo { get { return redoStack.Count > 0; } }

        public SparseMatrix CurrentSheet
        {
            get
            {
                if (_activeSheetIndex >= 0 && _activeSheetIndex < Sheets.Count) return Sheets[_activeSheetIndex];
                return (Sheets != null && Sheets.Count > 0) ? Sheets[0] : null;
            }
        }

        public uint SelectedRow { get; set; }
        public uint SelectedCol { get; set; }
        public uint SelEndRow { get; set; }
        public uint SelEndCol { get; set; }
        public bool IsSelectingRange { get; set; }

        public uint ScrollRow { get; set; }
        public uint ScrollCol { get; set; }

        public Dictionary<uint, int> CustomColWidths { get; set; }
        public Dictionary<uint, int> CustomRowHeights { get; set; }

        public int DefaultColWidth { get; set; }
        public int DefaultRowHeight { get; set; }

        public bool IsEditing { get; set; }
        public string EditText { get; set; }
        public int EditCursorPos { get; set; }
        public int EditSelStart { get; set; }
        public int EditSelLength { get; set; }
        public int EditSelectionAnchor { get; set; }
        public bool IsDraggingTextSelection { get; set; }
        public Rectangle ActiveEditingCellRect { get; set; }

        // Column & Row Resize Dragging
        public bool IsResizingCol { get; set; }
        public bool IsResizingRow { get; set; }
        public uint ResizingColIndex { get; set; }
        public uint ResizingRowIndex { get; set; }
        public Point ResizeStartMouse { get; set; }
        public int ResizeStartSize { get; set; }

        // AutoFill Handle Dragging
        public bool IsDraggingFillHandle { get; set; }
        public uint FillStartRow { get; set; }
        public uint FillStartCol { get; set; }
        public uint FillEndRow { get; set; }
        public uint FillEndCol { get; set; }
        public uint FillDragTargetRow { get; set; }
        public uint FillDragTargetCol { get; set; }

        // Scrollbar Thumb Dragging
        public bool IsDraggingHorzScroll { get; set; }
        public bool IsDraggingVertScroll { get; set; }
        public int ScrollDragStartPos { get; set; }
        public uint ScrollDragStartVal { get; set; }

        // Freeze Panes
        public bool IsFreezeTopRow { get; set; }
        public bool IsFreezeFirstCol { get; set; }

        // View Mode & Zoom
        public int ViewMode { get; set; } // 0: Normal, 1: Page Layout, 2: Page Break Preview
        public float ZoomFactor { get; set; } // 1.0f = 100%
        public bool ShowGridlines { get; set; }
        public bool ShowHeadings { get; set; }
        public bool ShowFormulaBar { get; set; }

        // Format Painter
        public bool IsFormatPainterActive { get; set; }
        public CellFormat CopiedFormat { get; set; }

        // Hover Tracking
        public Point LastMousePos { get; set; }
        public bool IsHoveringCancel { get; set; }
        public bool IsHoveringEnter { get; set; }
        public bool IsHoveringFx { get; set; }
        public bool IsHoveringNameBox { get; set; }

        // Formula Suggestions List
        public List<FormulaSuggestion> AllFormulas { get; set; }
        public List<FormulaSuggestion> ActiveSuggestions { get; set; }
        public int SelectedSuggestionIndex { get; set; }

        // Shapes & Pictures (Drawing Objects)
        public List<DocShapeObject> Shapes { get; set; }
        public DocShapeObject SelectedShape { get; set; }
        public int SelectedShapeHandle { get; set; }
        public Point DragStartMouse { get; set; }
        public Point DragStartShapePos { get; set; }
        public Size DragStartShapeSize { get; set; }
        public Rectangle PlusBtnChartRect { get; set; }
        public Rectangle StyleBtnChartRect { get; set; }
        public Rectangle FilterBtnChartRect { get; set; }

        // Excel Tables & Filtering
        public List<ExcelTableInfo> Tables { get; set; }
        public HashSet<uint> HiddenRows { get; set; }

        // Full Column & Full Row Selection Dragging
        public bool IsSelectingFullCols { get; set; }
        public bool IsSelectingFullRows { get; set; }

        // Table Corner Resize Dragging
        public bool IsDraggingTableResize { get; set; }
        public ExcelTableInfo ResizingTable { get; set; }
        public uint TableResizeTargetRow { get; set; }
        public uint TableResizeTargetCol { get; set; }

        // Sparklines (Mini cell charts)
        public Dictionary<long, SparklineInfo> Sparklines { get; set; }

        // Page Layout & Print Setup
        public bool IsLandscape { get; set; }
        public string PaperSize { get; set; }
        public int MarginPreset { get; set; }
        public int ThemeIndex { get; set; }
        public bool HasPrintArea { get; set; }
        public uint PrintAreaStartRow { get; set; }
        public uint PrintAreaStartCol { get; set; }
        public uint PrintAreaEndRow { get; set; }
        public uint PrintAreaEndCol { get; set; }

        // Security & Header/Footer
        public bool IsSheetProtected { get; set; }
        public string ProtectPassword { get; set; }
        public string HeaderLeft { get; set; }
        public string HeaderCenter { get; set; }
        public string HeaderRight { get; set; }
        public string FooterLeft { get; set; }
        public string FooterCenter { get; set; }
        public string FooterRight { get; set; }

        #region Custom Excel Cursors (Down Arrow, Right Arrow, Diag Arrow)
        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

        private static Cursor _excelDownArrowCursor = null;
        private static Cursor _excelRightArrowCursor = null;
        private static Cursor _excelDiagArrowCursor = null;
        private static Cursor _excelCellSelectCursor = null;

        public static Cursor ExcelCellSelectCursor
        {
            get
            {
                if (_excelCellSelectCursor == null)
                {
                    try { _excelCellSelectCursor = CreateCellSelectCursor(); }
                    catch { _excelCellSelectCursor = Cursors.Cross; }
                }
                return _excelCellSelectCursor;
            }
        }

        public static Cursor ExcelDownArrowCursor
        {
            get
            {
                if (_excelDownArrowCursor == null)
                {
                    try { _excelDownArrowCursor = CreateDownArrowCursor(); }
                    catch { _excelDownArrowCursor = Cursors.PanSouth; }
                }
                return _excelDownArrowCursor;
            }
        }

        public static Cursor ExcelRightArrowCursor
        {
            get
            {
                if (_excelRightArrowCursor == null)
                {
                    try { _excelRightArrowCursor = CreateRightArrowCursor(); }
                    catch { _excelRightArrowCursor = Cursors.PanEast; }
                }
                return _excelRightArrowCursor;
            }
        }

        public static Cursor ExcelDiagArrowCursor
        {
            get
            {
                if (_excelDiagArrowCursor == null)
                {
                    try { _excelDiagArrowCursor = CreateDiagArrowCursor(); }
                    catch { _excelDiagArrowCursor = Cursors.PanSE; }
                }
                return _excelDiagArrowCursor;
            }
        }

        private static Cursor CreateDownArrowCursor()
        {
            using (Bitmap bmp = new Bitmap(24, 24))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Point[] head = new Point[] { new Point(5, 11), new Point(17, 11), new Point(11, 18) };
                    using (Pen w = new Pen(Color.White, 3f))
                    {
                        g.DrawLine(w, 11, 2, 11, 12);
                        g.DrawPolygon(w, head);
                    }
                    using (Pen b = new Pen(Color.Black, 2f))
                    {
                        g.DrawLine(b, 11, 2, 11, 12);
                    }
                    g.FillPolygon(Brushes.Black, head);
                }
                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 11, yHotspot = 18, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }

        private static Cursor CreateRightArrowCursor()
        {
            using (Bitmap bmp = new Bitmap(24, 24))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Point[] head = new Point[] { new Point(11, 5), new Point(11, 17), new Point(18, 11) };
                    using (Pen w = new Pen(Color.White, 3f))
                    {
                        g.DrawLine(w, 2, 11, 12, 11);
                        g.DrawPolygon(w, head);
                    }
                    using (Pen b = new Pen(Color.Black, 2f))
                    {
                        g.DrawLine(b, 2, 11, 12, 11);
                    }
                    g.FillPolygon(Brushes.Black, head);
                }
                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 18, yHotspot = 11, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }

        private static Cursor CreateDiagArrowCursor()
        {
            using (Bitmap bmp = new Bitmap(24, 24))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Point[] head = new Point[] { new Point(9, 17), new Point(17, 9), new Point(18, 18) };
                    using (Pen w = new Pen(Color.White, 3f))
                    {
                        g.DrawLine(w, 4, 4, 13, 13);
                        g.DrawPolygon(w, head);
                    }
                    using (Pen b = new Pen(Color.Black, 2f))
                    {
                        g.DrawLine(b, 4, 4, 13, 13);
                    }
                    g.FillPolygon(Brushes.Black, head);
                }
                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 18, yHotspot = 18, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }

        private static Cursor CreateCellSelectCursor()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Point[] crossPoints = new Point[] {
                        new Point(12, 6),
                        new Point(18, 6),
                        new Point(18, 12),
                        new Point(24, 12),
                        new Point(24, 18),
                        new Point(18, 18),
                        new Point(18, 24),
                        new Point(12, 24),
                        new Point(12, 18),
                        new Point(6, 18),
                        new Point(6, 12),
                        new Point(12, 12)
                    };
                    g.FillPolygon(Brushes.White, crossPoints);
                    using (Pen p = new Pen(Color.Black, 1.2f))
                    {
                        g.DrawPolygon(p, crossPoints);
                    }
                }
                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 15, yHotspot = 15, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }
        #endregion

        public CalcView()
        {
            Sheets = new List<SparseMatrix>();
            SheetNames = new List<string>();
            ActiveSheetIndex = 0;

            CustomColWidths = new Dictionary<uint, int>();
            CustomRowHeights = new Dictionary<uint, int>();

            Shapes = new List<DocShapeObject>();
            SelectedShape = null;
            SelectedShapeHandle = -1;
            Tables = new List<ExcelTableInfo>();
            HiddenRows = new HashSet<uint>();

            SelectedRow = 1;
            SelectedCol = 1;
            SelEndRow = 1;
            SelEndCol = 1;

            ScrollRow = 1;
            ScrollCol = 1;
            DefaultColWidth = 80;
            DefaultRowHeight = 24;
            IsEditing = false;
            EditText = "";
            EditCursorPos = 0;

            ShowGridlines = true;
            ShowHeadings = true;
            ShowFormulaBar = true;
            ZoomFactor = 1.0f;
            ViewMode = 0;

            Sparklines = new Dictionary<long, SparklineInfo>();
            IsLandscape = false;
            PaperSize = "A4";
            MarginPreset = 0;
            ThemeIndex = 0;
            IsSheetProtected = false;
            FooterCenter = "Trang &[Page]";

            InitFormulas();

            AddSheet("Sheet1");
            AddSheet("Sheet2");
            AddSheet("Sheet3");
        }

        private void InitFormulas()
        {
            AllFormulas = new List<FormulaSuggestion>
            {
                new FormulaSuggestion("SUM", "=SUM(number1, [number2], ...)", "Tính tổng tất cả các số trong một dải ô được chọn."),
                new FormulaSuggestion("AVERAGE", "=AVERAGE(number1, [number2], ...)", "Tính giá trị trung bình cộng của các đối số."),
                new FormulaSuggestion("COUNT", "=COUNT(value1, [value2], ...)", "Đếm số lượng ô có chứa giá trị số."),
                new FormulaSuggestion("MAX", "=MAX(number1, [number2], ...)", "Trả về giá trị lớn nhất trong tập hợp dữ liệu."),
                new FormulaSuggestion("MIN", "=MIN(number1, [number2], ...)", "Trả về giá trị nhỏ nhất trong tập hợp dữ liệu."),
                new FormulaSuggestion("IF", "=IF(logical_test, value_if_true, value_if_false)", "Kiểm tra điều kiện logic và trả về giá trị tương ứng."),
                new FormulaSuggestion("VLOOKUP", "=VLOOKUP(lookup_value, table_array, col_index, [range_lookup])", "Tìm kiếm một giá trị trong cột đầu tiên của bảng."),
                new FormulaSuggestion("CONCATENATE", "=CONCATENATE(text1, [text2], ...)", "Nối nhiều chuỗi văn bản lại thành một chuỗi duy nhất."),
                new FormulaSuggestion("ROUND", "=ROUND(number, num_digits)", "Làm tròn một số đến số chữ số thập phân đã định."),
                new FormulaSuggestion("PRODUCT", "=PRODUCT(number1, [number2], ...)", "Nhân tất cả các số được đưa ra làm đối số."),
                new FormulaSuggestion("TODAY", "=TODAY()", "Trả về ngày tháng hiện tại của hệ thống."),
                new FormulaSuggestion("NOW", "=NOW()", "Trả về ngày tháng và thời gian hiện tại của hệ thống.")
            };
            ActiveSuggestions = new List<FormulaSuggestion>();
            SelectedSuggestionIndex = 0;
        }

        public void SwitchToSheet(int newIndex)
        {
            if (newIndex >= 0 && newIndex < Sheets.Count)
            {
                SaveCurrentSheetState();
                _activeSheetIndex = newIndex;
                LoadSheetState(newIndex);
                IsEditing = false;
                CancelEdit();
            }
        }

        public void SaveCurrentSheetState()
        {
            if (SheetStates == null) SheetStates = new List<CalcSheetState>();
            while (SheetStates.Count < Sheets.Count)
            {
                SheetStates.Add(new CalcSheetState());
            }

            if (_activeSheetIndex >= 0 && _activeSheetIndex < SheetStates.Count)
            {
                var st = SheetStates[_activeSheetIndex];
                st.SelectedRow = SelectedRow;
                st.SelectedCol = SelectedCol;
                st.SelEndRow = SelEndRow;
                st.SelEndCol = SelEndCol;
                st.ScrollRow = ScrollRow;
                st.ScrollCol = ScrollCol;
                st.CustomColWidths = new Dictionary<uint, int>(CustomColWidths);
                st.CustomRowHeights = new Dictionary<uint, int>(CustomRowHeights);
                st.Shapes = Shapes != null ? new List<DocShapeObject>(Shapes) : new List<DocShapeObject>();
                st.Tables = Tables != null ? new List<ExcelTableInfo>(Tables) : new List<ExcelTableInfo>();
            }
        }

        public void LoadSheetState(int sheetIndex)
        {
            if (SheetStates == null) SheetStates = new List<CalcSheetState>();
            while (SheetStates.Count < Sheets.Count)
            {
                SheetStates.Add(new CalcSheetState());
            }

            if (sheetIndex >= 0 && sheetIndex < SheetStates.Count)
            {
                var st = SheetStates[sheetIndex];
                SelectedRow = Math.Max(1, st.SelectedRow);
                SelectedCol = Math.Max(1, st.SelectedCol);
                SelEndRow = Math.Max(1, st.SelEndRow);
                SelEndCol = Math.Max(1, st.SelEndCol);
                ScrollRow = Math.Max(1, st.ScrollRow);
                ScrollCol = Math.Max(1, st.ScrollCol);
                CustomColWidths = new Dictionary<uint, int>(st.CustomColWidths);
                CustomRowHeights = new Dictionary<uint, int>(st.CustomRowHeights);
                Shapes = st.Shapes != null ? new List<DocShapeObject>(st.Shapes) : new List<DocShapeObject>();
                Tables = st.Tables != null ? new List<ExcelTableInfo>(st.Tables) : new List<ExcelTableInfo>();
            }
        }

        public void AddSheet(string name = null)
        {
            if (string.IsNullOrEmpty(name)) name = "Sheet" + (SheetNames.Count + 1);
            SparseMatrix sheet = new SparseMatrix();
            Sheets.Add(sheet);
            SheetNames.Add(name);
            if (SheetStates == null) SheetStates = new List<CalcSheetState>();
            SheetStates.Add(new CalcSheetState());
        }

        public void DeleteSheet(int index)
        {
            if (Sheets.Count <= 1) return;
            if (index >= 0 && index < Sheets.Count)
            {
                SaveUndoState();
                Sheets.RemoveAt(index);
                SheetNames.RemoveAt(index);
                if (index < SheetStates.Count) SheetStates.RemoveAt(index);
                int nextIndex = Math.Max(0, Math.Min(index, Sheets.Count - 1));
                _activeSheetIndex = nextIndex;
                LoadSheetState(nextIndex);
            }
        }

        public void DuplicateSheet(int index)
        {
            if (index >= 0 && index < Sheets.Count)
            {
                SaveUndoState();
                SparseMatrix orig = Sheets[index];
                SparseMatrix clone = new SparseMatrix();
                foreach (var kvp in orig.GetAllCells())
                {
                    var pos = SparseMatrix.DecodeKey(kvp.Key);
                    CellData c = kvp.Value;
                    CellData cClone = new CellData
                    {
                        Type = c.Type,
                        RawValue = c.RawValue,
                        FormulaStr = c.FormulaStr,
                        NumericValue = c.NumericValue,
                        EvaluatedStr = c.EvaluatedStr,
                        Format = c.Format != null ? c.Format.Clone() : new CellFormat()
                    };
                    clone.SetCell(pos.Item1, pos.Item2, cClone);
                }
                string newName = SheetNames[index] + " (2)";
                Sheets.Insert(index + 1, clone);
                SheetNames.Insert(index + 1, newName);
                CalcSheetState cloneState = new CalcSheetState();
                if (index < SheetStates.Count)
                {
                    var origSt = SheetStates[index];
                    cloneState.SelectedRow = origSt.SelectedRow;
                    cloneState.SelectedCol = origSt.SelectedCol;
                    cloneState.SelEndRow = origSt.SelEndRow;
                    cloneState.SelEndCol = origSt.SelEndCol;
                    cloneState.CustomColWidths = new Dictionary<uint, int>(origSt.CustomColWidths);
                    cloneState.CustomRowHeights = new Dictionary<uint, int>(origSt.CustomRowHeights);
                }
                if (SheetStates == null) SheetStates = new List<CalcSheetState>();
                SheetStates.Insert(index + 1, cloneState);
                SwitchToSheet(index + 1);
            }
        }

        public void RenameSheet(int index, string newName)
        {
            if (index >= 0 && index < SheetNames.Count && !string.IsNullOrEmpty(newName))
            {
                SaveUndoState();
                SheetNames[index] = newName.Trim();
            }
        }

        public void PromptRenameSheet(int sheetIndex, Control parentControl = null)
        {
            if (sheetIndex < 0 || sheetIndex >= SheetNames.Count) return;
            using (Form prompt = new Form())
            {
                prompt.Width = 360;
                prompt.Height = 150;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Đổi Tên Sheet";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 15, Text = "Tên mới cho trang tính:" };
                TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 300, Text = SheetNames[sheetIndex] };
                Button confirmation = new Button() { Text = "OK", Left = 160, Width = 75, Top = 75, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Hủy", Left = 245, Width = 75, Top = 75, DialogResult = DialogResult.Cancel };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(cancel);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;
                prompt.CancelButton = cancel;

                if (prompt.ShowDialog(parentControl) == DialogResult.OK && !string.IsNullOrEmpty(textBox.Text))
                {
                    RenameSheet(sheetIndex, textBox.Text);
                    if (parentControl != null) parentControl.Invalidate();
                }
            }
        }

        public void ShowSheetTabContextMenu(int x, int y, int sheetIndex, Control parentControl)
        {
            if (sheetIndex < 0 || sheetIndex >= SheetNames.Count) return;
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Font = new Font("Segoe UI", 9f);

            cms.Items.Add("✏️ Đổi tên Sheet (Rename)...", null, (s, e) =>
            {
                PromptRenameSheet(sheetIndex, parentControl);
            });

            cms.Items.Add("➕ Chèn Sheet mới (Insert Sheet)", null, (s, e) =>
            {
                AddSheet("Sheet" + (SheetNames.Count + 1));
                SwitchToSheet(SheetNames.Count - 1);
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Items.Add("📋 Nhân bản Sheet (Duplicate Sheet)", null, (s, e) =>
            {
                DuplicateSheet(sheetIndex);
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Items.Add(new ToolStripSeparator());

            var delItem = cms.Items.Add("🗑️ Xóa Sheet (Delete Sheet)", null, (s, e) =>
            {
                if (Sheets.Count <= 1)
                {
                    MessageBox.Show("Bảng tính phải có ít nhất 1 sheet!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show(string.Format("Bạn có chắc chắn muốn xóa sheet \"{0}\"?", SheetNames[sheetIndex]), "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DeleteSheet(sheetIndex);
                    if (parentControl != null) parentControl.Invalidate();
                }
            });
            delItem.Enabled = (Sheets.Count > 1);

            cms.Items.Add(new ToolStripSeparator());

            var moveLeftItem = cms.Items.Add("⬅️ Di chuyển sang trái", null, (s, e) =>
            {
                if (sheetIndex > 0)
                {
                    SaveCurrentSheetState();
                    var sm = Sheets[sheetIndex];
                    var name = SheetNames[sheetIndex];
                    var st = SheetStates[sheetIndex];

                    Sheets.RemoveAt(sheetIndex);
                    SheetNames.RemoveAt(sheetIndex);
                    SheetStates.RemoveAt(sheetIndex);

                    Sheets.Insert(sheetIndex - 1, sm);
                    SheetNames.Insert(sheetIndex - 1, name);
                    SheetStates.Insert(sheetIndex - 1, st);

                    _activeSheetIndex = sheetIndex - 1;
                    LoadSheetState(_activeSheetIndex);
                    if (parentControl != null) parentControl.Invalidate();
                }
            });
            moveLeftItem.Enabled = (sheetIndex > 0);

            var moveRightItem = cms.Items.Add("➡️ Di chuyển sang phải", null, (s, e) =>
            {
                if (sheetIndex < Sheets.Count - 1)
                {
                    SaveCurrentSheetState();
                    var sm = Sheets[sheetIndex];
                    var name = SheetNames[sheetIndex];
                    var st = SheetStates[sheetIndex];

                    Sheets.RemoveAt(sheetIndex);
                    SheetNames.RemoveAt(sheetIndex);
                    SheetStates.RemoveAt(sheetIndex);

                    Sheets.Insert(sheetIndex + 1, sm);
                    SheetNames.Insert(sheetIndex + 1, name);
                    SheetStates.Insert(sheetIndex + 1, st);

                    _activeSheetIndex = sheetIndex + 1;
                    LoadSheetState(_activeSheetIndex);
                    if (parentControl != null) parentControl.Invalidate();
                }
            });
            moveRightItem.Enabled = (sheetIndex < Sheets.Count - 1);

            Point pt = (parentControl != null) ? parentControl.PointToScreen(new Point(x, y)) : new Point(x, y);
            cms.Show(pt);
        }

        public void PopulateSampleData(string docTitle)
        {
            Sheets.Clear();
            SheetNames.Clear();
            if (SheetStates == null) SheetStates = new List<CalcSheetState>();
            SheetStates.Clear();
            CustomColWidths.Clear();
            CustomRowHeights.Clear();
            AddSheet("Sheet1");
            AddSheet("Sheet2");
            AddSheet("Sheet3");
            _activeSheetIndex = 0;
            LoadSheetState(0);

            SelectedRow = 1;
            SelectedCol = 1;
            SelEndRow = 1;
            SelEndCol = 1;
            ScrollRow = 1;
            ScrollCol = 1;
            IsEditing = false;
            EditSelStart = 0;
            EditSelLength = 0;
            undoStack.Clear();
            redoStack.Clear();
        }

        public void SaveUndoState()
        {
            if (undoStack.Count >= 50)
            {
                var tempArr = undoStack.ToArray();
                undoStack.Clear();
                for (int i = tempArr.Length - 2; i >= 0; i--)
                {
                    undoStack.Push(tempArr[i]);
                }
            }
            undoStack.Push(new CalcUndoSnapshot(CurrentSheet, ActiveSheetIndex, SelectedRow, SelectedCol, SelEndRow, SelEndCol, CustomColWidths, CustomRowHeights));
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(new CalcUndoSnapshot(CurrentSheet, ActiveSheetIndex, SelectedRow, SelectedCol, SelEndRow, SelEndCol, CustomColWidths, CustomRowHeights));
                var prev = undoStack.Pop();
                if (prev != null)
                {
                    ActiveSheetIndex = prev.ActiveSheetIndex;
                    CurrentSheet.RestoreGrid(prev.GridSnapshot);
                    SelectedRow = prev.SelectedRow;
                    SelectedCol = prev.SelectedCol;
                    SelEndRow = prev.SelEndRow;
                    SelEndCol = prev.SelEndCol;
                    CustomColWidths = new Dictionary<uint, int>(prev.ColWidths);
                    CustomRowHeights = new Dictionary<uint, int>(prev.RowHeights);
                    IsEditing = false;
                    EditSelLength = 0;
                    FormulaEngine.EvaluateSheet(CurrentSheet);
                }
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(new CalcUndoSnapshot(CurrentSheet, ActiveSheetIndex, SelectedRow, SelectedCol, SelEndRow, SelEndCol, CustomColWidths, CustomRowHeights));
                var next = redoStack.Pop();
                if (next != null)
                {
                    ActiveSheetIndex = next.ActiveSheetIndex;
                    CurrentSheet.RestoreGrid(next.GridSnapshot);
                    SelectedRow = next.SelectedRow;
                    SelectedCol = next.SelectedCol;
                    SelEndRow = next.SelEndRow;
                    SelEndCol = next.SelEndCol;
                    CustomColWidths = new Dictionary<uint, int>(next.ColWidths);
                    CustomRowHeights = new Dictionary<uint, int>(next.RowHeights);
                    IsEditing = false;
                    EditSelLength = 0;
                    FormulaEngine.EvaluateSheet(CurrentSheet);
                }
            }
        }

        public int GetTextCharIndexAtX(int clickX, int cellX, Font font)
        {
            if (string.IsNullOrEmpty(EditText)) return 0;
            int relX = clickX - (cellX + 4);
            if (relX <= 0) return 0;
            using (Bitmap b = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(b))
            {
                for (int i = 1; i <= EditText.Length; i++)
                {
                    string sub = EditText.Substring(0, i);
                    SizeF sz = g.MeasureString(sub, font);
                    if (sz.Width >= relX)
                    {
                        string prevSub = EditText.Substring(0, i - 1);
                        SizeF prevSz = prevSub.Length > 0 ? g.MeasureString(prevSub, font) : SizeF.Empty;
                        if (Math.Abs(relX - prevSz.Width) < Math.Abs(relX - sz.Width)) return i - 1;
                        return i;
                    }
                }
            }
            return EditText.Length;
        }

        public void PopulateDemoData(SparseMatrix sheet)
        {
            sheet.SetCellText(1, 1, "BÁO CÁO DOANH THU & LỢI NHUẬN GENOFFICE Q3/2026");
            CellData titleCell = sheet.GetOrCreateCell(1, 1);
            titleCell.Format.Bold = true;
            titleCell.Format.FontSize = 13.0f;
            titleCell.Format.TextColor = unchecked((int)0xFF1E3A8A); // Blue 900
            titleCell.Format.IsMergedParent = true;
            titleCell.Format.MergeColSpan = 5;

            sheet.SetCellText(3, 1, "Mã SP");
            sheet.SetCellText(3, 2, "Tên Sản Phẩm");
            sheet.SetCellText(3, 3, "Số Lượng");
            sheet.SetCellText(3, 4, "Đơn Giá ($)");
            sheet.SetCellText(3, 5, "Thành Tiền ($)");

            sheet.SetCellText(4, 1, "SP-001");
            sheet.SetCellText(4, 2, "GenOffice Suite Pro 2026");
            sheet.SetCellText(4, 3, "250");
            sheet.SetCellText(4, 4, "120");
            sheet.SetCellText(4, 5, "=C4*D4");

            sheet.SetCellText(5, 1, "SP-002");
            sheet.SetCellText(5, 2, "GenWriter Cloud Enterprise");
            sheet.SetCellText(5, 3, "180");
            sheet.SetCellText(5, 4, "85");
            sheet.SetCellText(5, 5, "=C5*D5");

            sheet.SetCellText(6, 1, "SP-003");
            sheet.SetCellText(6, 2, "GenCalc Formula AI Add-in");
            sheet.SetCellText(6, 3, "320");
            sheet.SetCellText(6, 4, "45");
            sheet.SetCellText(6, 5, "=C6*D6");

            sheet.SetCellText(7, 1, "SP-004");
            sheet.SetCellText(7, 2, "GenSlides Morph 60FPS Plugin");
            sheet.SetCellText(7, 3, "150");
            sheet.SetCellText(7, 4, "60");
            sheet.SetCellText(7, 5, "=C7*D7");

            sheet.SetCellText(9, 4, "TỔNG DOANH THU:");
            sheet.SetCellText(9, 5, "=SUM(E4:E7)");

            sheet.SetCellText(10, 4, "TRUNG BÌNH:");
            sheet.SetCellText(10, 5, "=AVERAGE(E4:E7)");

            // Style Headers
            for (uint c = 1; c <= 5; c++)
            {
                CellData cell = sheet.GetOrCreateCell(3, c);
                cell.Format.Bold = true;
                cell.Format.BgColor = unchecked((int)0xFF107C41); // Excel Green
                cell.Format.TextColor = unchecked((int)0xFFFFFFFF);
                cell.Format.Align = (c >= 3) ? 2 : 0;
            }

            // Set Number Formats
            for (uint r = 4; r <= 7; r++)
            {
                CellData cellPrice = sheet.GetOrCreateCell(r, 4);
                cellPrice.Format.NumberFormat = "Currency";
                CellData cellTotal = sheet.GetOrCreateCell(r, 5);
                cellTotal.Format.NumberFormat = "Currency";
            }

            CellData sumLabel = sheet.GetOrCreateCell(9, 4);
            sumLabel.Format.Bold = true;
            sumLabel.Format.Align = 2;

            CellData sumVal = sheet.GetOrCreateCell(9, 5);
            sumVal.Format.Bold = true;
            sumVal.Format.NumberFormat = "Currency";
            sumVal.Format.BgColor = unchecked((int)0xFFDCFCE7); // Light green

            CellData avgLabel = sheet.GetOrCreateCell(10, 4);
            avgLabel.Format.Bold = true;
            avgLabel.Format.Align = 2;

            CellData avgVal = sheet.GetOrCreateCell(10, 5);
            avgVal.Format.Bold = true;
            avgVal.Format.NumberFormat = "Currency";

            FormulaEngine.EvaluateSheet(sheet);
        }

        public int GetColWidth(uint col)
        {
            if (CustomColWidths.ContainsKey(col)) return CustomColWidths[col];
            return DefaultColWidth;
        }

        public int GetRowHeight(uint row)
        {
            if (CustomRowHeights.ContainsKey(row)) return CustomRowHeights[row];
            return DefaultRowHeight;
        }

        public int GetColX(uint col)
        {
            int headerW = ShowHeadings ? 48 : 0;
            int x = headerW;
            for (uint c = ScrollCol; c < col; c++) x += GetColWidth(c);
            return x;
        }

        public int GetRowY(uint row)
        {
            int fBarY = 155;
            int startY = ShowFormulaBar ? (fBarY + 30) : fBarY;
            int headerH = ShowHeadings ? 26 : 0;
            int y = startY + headerH;
            for (uint r = ScrollRow; r < row; r++) y += GetRowHeight(r);
            return y;
        }

        public void StartEditingCell(string initialText = null)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }
            CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
            EditText = initialText != null ? initialText : (cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "");
            IsEditing = true;
            EditCursorPos = EditText.Length;
            EditSelStart = 0;
            EditSelLength = 0;
            EditSelectionAnchor = 0;
        }

        public void CommitCellEdit()
        {
            CommitEditAndMove(0, 0);
        }

        public void Draw(Graphics g, int width, int height)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int fBarY = 155;
            int fBarH = 30;

            // ==================== 1. Formula Bar & Name Box (Y: 155 to 185) ====================
            if (ShowFormulaBar)
            {
                g.FillRectangle(Brushes.White, 0, fBarY, width, fBarH);
                using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    g.DrawLine(bPen, 0, fBarY + fBarH - 1, width, fBarY + fBarH - 1);
                }

                // 1.1 Name Box (A1 / Range Address)
                Rectangle nameBox = new Rectangle(8, fBarY + 3, 75, 23);
                using (GraphicsPath nPath = CreateRoundedPath(nameBox, 3))
                {
                    g.FillPath(IsHoveringNameBox ? Brushes.WhiteSmoke : Brushes.White, nPath);
                    using (Pen nPen = new Pen(Color.FromArgb(203, 213, 225))) g.DrawPath(nPen, nPath);
                }

                using (Font boldFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (Font symFont = new Font("Segoe UI", 8f, FontStyle.Regular))
                {
                    string addr = SparseMatrix.ColIndexToName(SelectedCol) + SelectedRow;
                    if (SelectedRow != SelEndRow || SelectedCol != SelEndCol)
                    {
                        uint rCount = (uint)(Math.Abs((int)SelEndRow - (int)SelectedRow) + 1);
                        uint cCount = (uint)(Math.Abs((int)SelEndCol - (int)SelectedCol) + 1);
                        addr = string.Format("{0}R x {1}C", rCount, cCount);
                    }
                    g.DrawString(addr, boldFont, Brushes.Black, nameBox.X + 8, nameBox.Y + 4);
                    g.DrawString("▾", symFont, Brushes.DimGray, nameBox.Right - 14, nameBox.Y + 4);
                }

                // Vertical Divider
                g.DrawLine(Pens.LightGray, nameBox.Right + 6, fBarY + 5, nameBox.Right + 6, fBarY + fBarH - 5);

                // 1.2 Action Buttons: Cancel (✕), Enter (✓), fx
                int btnStartX = nameBox.Right + 12;

                // Cancel Button ✕
                Rectangle cancelBox = new Rectangle(btnStartX, fBarY + 3, 23, 23);
                if (IsHoveringCancel || IsEditing)
                {
                    using (GraphicsPath p = CreateRoundedPath(cancelBox, 3))
                    {
                        g.FillPath(IsHoveringCancel ? Brushes.MistyRose : Brushes.Transparent, p);
                        if (IsHoveringCancel) g.DrawPath(Pens.IndianRed, p);
                    }
                }
                using (Font xFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                {
                    g.DrawString("✕", xFont, IsEditing ? Brushes.Crimson : Brushes.Gray, cancelBox.X + 5, cancelBox.Y + 2);
                }

                // Enter Button ✓
                Rectangle enterBox = new Rectangle(btnStartX + 25, fBarY + 3, 23, 23);
                if (IsHoveringEnter || IsEditing)
                {
                    using (GraphicsPath p = CreateRoundedPath(enterBox, 3))
                    {
                        g.FillPath(IsHoveringEnter ? Brushes.Honeydew : Brushes.Transparent, p);
                        if (IsHoveringEnter) g.DrawPath(Pens.DarkSeaGreen, p);
                    }
                }
                using (Font vFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                {
                    g.DrawString("✓", vFont, IsEditing ? Brushes.Green : Brushes.Gray, enterBox.X + 4, enterBox.Y + 1);
                }

                // Insert Function Button fx
                Rectangle fxBox = new Rectangle(btnStartX + 50, fBarY + 3, 26, 23);
                if (IsHoveringFx)
                {
                    using (GraphicsPath p = CreateRoundedPath(fxBox, 3))
                    {
                        g.FillPath(Brushes.Honeydew, p);
                        g.DrawPath(Pens.MediumSeaGreen, p);
                    }
                }
                using (Font fxFont = new Font("Georgia", 10.5f, FontStyle.Bold | FontStyle.Italic))
                {
                    g.DrawString("fx", fxFont, Color.FromArgb(16, 124, 65).ToBrush(), fxBox.X + 4, fxBox.Y + 2);
                }

                // Vertical Divider
                g.DrawLine(Pens.LightGray, fxBox.Right + 6, fBarY + 5, fxBox.Right + 6, fBarY + fBarH - 5);

                // 1.3 Formula Input Box
                int fInputX = fxBox.Right + 12;
                int fInputW = width - fInputX - 10;
                Rectangle fBox = new Rectangle(fInputX, fBarY + 3, fInputW, 23);
                using (GraphicsPath fPath = CreateRoundedPath(fBox, 3))
                {
                    g.FillPath(Brushes.White, fPath);
                    using (Pen fPen = new Pen(IsEditing ? Color.FromArgb(16, 124, 65) : Color.FromArgb(203, 213, 225), IsEditing ? 1.5f : 1f))
                    {
                        g.DrawPath(fPen, fPath);
                    }
                }

                using (Font normFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                {
                    CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                    string fDisplay = IsEditing ? EditText : (cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "");
                    g.DrawString(fDisplay, normFont, Brushes.Black, fBox.X + 8, fBox.Y + 4);

                    if (IsEditing)
                    {
                        string subStr = fDisplay.Substring(0, Math.Min(fDisplay.Length, EditCursorPos));
                        SizeF txtSz = g.MeasureString(subStr, normFont);
                        float cX = fBox.X + 8 + txtSz.Width;
                        g.DrawLine(Pens.Black, cX, fBox.Y + 4, cX, fBox.Y + 19);
                    }
                }
            }

            // ==================== 2. Spreadsheet Grid Headers & Rows ====================
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = ShowFormulaBar ? (fBarY + fBarH) : fBarY;
            int vScrollW = 16;
            int bottomBarH = 56; // Sheet tabs (28px) + Status bar (28px)
            int gridW = width - vScrollW;
            int maxGridY = height - bottomBarH;

            // Top-Left Header Intersection (Select All Triangle)
            if (ShowHeadings)
            {
                Rectangle tlRect = new Rectangle(0, startY, headerW, headerH);
                using (SolidBrush bgB = new SolidBrush(Color.FromArgb(243, 244, 246))) g.FillRectangle(bgB, tlRect);
                using (Pen p = new Pen(Color.FromArgb(209, 213, 219))) g.DrawRectangle(p, tlRect);
                using (SolidBrush triB = new SolidBrush(Color.FromArgb(156, 163, 175)))
                {
                    g.FillPolygon(triB, new PointF[] {
                        new PointF(headerW - 6, startY + headerH - 6),
                        new PointF(headerW - 14, startY + headerH - 6),
                        new PointF(headerW - 6, startY + headerH - 14)
                    });
                }
            }

            // 2.1 Column Headers (A, B, C, D... Z, AA, AB...)
            int curX = headerW;
            uint colIdx = ScrollCol;

            uint minSelC = Math.Min(SelectedCol, SelEndCol);
            uint maxSelC = Math.Max(SelectedCol, SelEndCol);
            uint minSelR = Math.Min(SelectedRow, SelEndRow);
            uint maxSelR = Math.Max(SelectedRow, SelEndRow);

            using (Font hFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (Font hSelFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                while (curX < gridW)
                {
                    int cW = GetColWidth(colIdx);
                    Rectangle cRect = new Rectangle(curX, startY, cW, headerH);
                    bool isColSelected = (colIdx >= minSelC && colIdx <= maxSelC);

                    if (ShowHeadings)
                    {
                        if (isColSelected)
                        {
                            using (SolidBrush selB = new SolidBrush(Color.FromArgb(220, 252, 231))) g.FillRectangle(selB, cRect);
                            using (Pen selP = new Pen(Color.FromArgb(16, 124, 65), 2)) g.DrawLine(selP, curX, startY + headerH - 1, curX + cW, startY + headerH - 1);
                        }
                        else
                        {
                            using (SolidBrush bgB = new SolidBrush(Color.FromArgb(248, 250, 252))) g.FillRectangle(bgB, cRect);
                        }

                        using (Pen p = new Pen(Color.FromArgb(226, 232, 240))) g.DrawRectangle(p, cRect);

                        string colName = SparseMatrix.ColIndexToName(colIdx);
                        SizeF sz = g.MeasureString(colName, isColSelected ? hSelFont : hFont);
                        using (SolidBrush txtB = new SolidBrush(Color.FromArgb(71, 85, 105)))
                        {
                            g.DrawString(colName, isColSelected ? hSelFont : hFont, isColSelected ? Color.FromArgb(16, 124, 65).ToBrush() : txtB, curX + (cW - sz.Width) / 2, startY + 4);
                        }

                        // Split divider line
                        using (Pen divP = new Pen(Color.FromArgb(203, 213, 225)))
                        {
                            g.DrawLine(divP, curX + cW - 1, startY + 4, curX + cW - 1, startY + headerH - 4);
                        }
                    }

                    curX += cW;
                    colIdx++;
                }
            }

            // 2.2 Rows & Cells
            int curY = startY + headerH;
            uint rowIdx = ScrollRow;

            // Tracking active editing cell bounding box for in-place rendering
            Rectangle editingCellRect = Rectangle.Empty;

            while (curY < maxGridY)
            {
                if (HiddenRows != null && HiddenRows.Contains(rowIdx))
                {
                    rowIdx++;
                    continue;
                }
                int rH = GetRowHeight(rowIdx);
                bool isRowSelected = (rowIdx >= minSelR && rowIdx <= maxSelR);

                // Row Header
                if (ShowHeadings)
                {
                    Rectangle rRect = new Rectangle(0, curY, headerW, rH);
                    if (isRowSelected)
                    {
                        using (SolidBrush selB = new SolidBrush(Color.FromArgb(220, 252, 231))) g.FillRectangle(selB, rRect);
                        using (Pen selP = new Pen(Color.FromArgb(16, 124, 65), 2)) g.DrawLine(selP, headerW - 1, curY, headerW - 1, curY + rH);
                    }
                    else
                    {
                        using (SolidBrush bgB = new SolidBrush(Color.FromArgb(248, 250, 252))) g.FillRectangle(bgB, rRect);
                    }
                    using (Pen p = new Pen(Color.FromArgb(226, 232, 240))) g.DrawRectangle(p, rRect);

                    using (Font rFont = new Font("Segoe UI", 8.5f, isRowSelected ? FontStyle.Bold : FontStyle.Regular))
                    {
                        string rStr = rowIdx.ToString();
                        SizeF sz = g.MeasureString(rStr, rFont);
                        using (SolidBrush txtB = new SolidBrush(Color.FromArgb(71, 85, 105)))
                        {
                            g.DrawString(rStr, rFont, isRowSelected ? Color.FromArgb(16, 124, 65).ToBrush() : txtB, headerW - sz.Width - 6, curY + (rH - sz.Height) / 2);
                        }
                    }
                }

                // Render Cells across columns
                curX = headerW;
                colIdx = ScrollCol;

                while (curX < gridW)
                {
                    int cW = GetColWidth(colIdx);
                    CellData cell = CurrentSheet.GetCell(rowIdx, colIdx);

                    // Skip merged child cells (they are part of an enclosing merged cell)
                    if (cell != null && cell.Format != null && cell.Format.IsMergedChild)
                    {
                        curX += cW;
                        colIdx++;
                        continue;
                    }

                    // Calculate effective render width & height (spans multiple cells if merged parent)
                    int renderW = cW;
                    int renderH = rH;
                    if (cell != null && cell.Format != null && cell.Format.IsMergedParent)
                    {
                        int mColSpan = Math.Max(1, cell.Format.MergeColSpan);
                        int mRowSpan = Math.Max(1, cell.Format.MergeRowSpan);
                        renderW = 0;
                        for (uint mc = 0; mc < mColSpan; mc++) renderW += GetColWidth(colIdx + mc);
                        renderH = 0;
                        for (uint mr = 0; mr < mRowSpan; mr++) renderH += GetRowHeight(rowIdx + mr);
                    }

                    Rectangle cellRect = new Rectangle(curX, curY, renderW, renderH);

                    if (IsEditing && rowIdx == SelectedRow && colIdx == SelectedCol)
                    {
                        editingCellRect = cellRect;
                    }

                    // Background color
                    Color cellBg = Color.White;
                    if (cell != null && cell.Format != null && cell.Format.BgColor != unchecked((int)0xFFFFFFFF))
                    {
                        cellBg = Color.FromArgb(cell.Format.BgColor);
                    }

                    using (SolidBrush bgB = new SolidBrush(cellBg))
                    {
                        g.FillRectangle(bgB, cellRect);
                    }

                    // Grid line
                    if (ShowGridlines)
                    {
                        using (Pen gridPen = new Pen(Color.FromArgb(229, 231, 235)))
                        {
                            g.DrawRectangle(gridPen, cellRect);
                        }
                    }

                    // Custom Cell Borders (if any)
                    if (cell != null && cell.Format != null && cell.Format.Borders != null)
                    {
                        var b = cell.Format.Borders;
                        if (b.Top)
                        {
                            int tThick = b.TopThickness > 0 ? b.TopThickness : (b.Thickness > 0 ? b.Thickness : 1);
                            Color tCol = b.TopColor != 0 ? Color.FromArgb(b.TopColor) : (b.Color != 0 ? Color.FromArgb(b.Color) : Color.FromArgb(15, 23, 42));
                            DrawBorderSegment(g, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Top, tThick, tCol, true);
                        }
                        if (b.Bottom)
                        {
                            int bThick = b.BottomThickness > 0 ? b.BottomThickness : (b.Thickness > 0 ? b.Thickness : 1);
                            Color bCol = b.BottomColor != 0 ? Color.FromArgb(b.BottomColor) : (b.Color != 0 ? Color.FromArgb(b.Color) : Color.FromArgb(15, 23, 42));
                            DrawBorderSegment(g, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Bottom, bThick, bCol, true);
                        }
                        if (b.Left)
                        {
                            int lThick = b.LeftThickness > 0 ? b.LeftThickness : (b.Thickness > 0 ? b.Thickness : 1);
                            Color lCol = b.LeftColor != 0 ? Color.FromArgb(b.LeftColor) : (b.Color != 0 ? Color.FromArgb(b.Color) : Color.FromArgb(15, 23, 42));
                            DrawBorderSegment(g, cellRect.Left, cellRect.Top, cellRect.Left, cellRect.Bottom, lThick, lCol, false);
                        }
                        if (b.Right)
                        {
                            int rThick = b.RightThickness > 0 ? b.RightThickness : (b.Thickness > 0 ? b.Thickness : 1);
                            Color rCol = b.RightColor != 0 ? Color.FromArgb(b.RightColor) : (b.Color != 0 ? Color.FromArgb(b.Color) : Color.FromArgb(15, 23, 42));
                            DrawBorderSegment(g, cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom, rThick, rCol, false);
                        }
                        if (b.DiagonalDown)
                        {
                            int dThick = b.DiagonalThickness > 0 ? b.DiagonalThickness : 1;
                            Color dCol = b.DiagonalColor != 0 ? Color.FromArgb(b.DiagonalColor) : (b.Color != 0 ? Color.FromArgb(b.Color) : Color.FromArgb(15, 23, 42));
                            using (Pen dPen = new Pen(dCol, dThick))
                            {
                                g.DrawLine(dPen, cellRect.Left, cellRect.Top, cellRect.Right, cellRect.Bottom);
                            }
                        }
                        if (b.DiagonalUp)
                        {
                            int dThick = b.DiagonalThickness > 0 ? b.DiagonalThickness : 1;
                            Color dCol = b.DiagonalColor != 0 ? Color.FromArgb(b.DiagonalColor) : (b.Color != 0 ? Color.FromArgb(b.Color) : Color.FromArgb(15, 23, 42));
                            using (Pen dPen = new Pen(dCol, dThick))
                            {
                                g.DrawLine(dPen, cellRect.Left, cellRect.Bottom, cellRect.Right, cellRect.Top);
                            }
                        }
                    }

                    // Cell Text Content (if not currently editing this specific cell in-place)
                    if (!(IsEditing && rowIdx == SelectedRow && colIdx == SelectedCol))
                    {
                        if (cell != null && (!string.IsNullOrEmpty(cell.EvaluatedStr) || !string.IsNullOrEmpty(cell.RawValue) || (cell.Format != null && cell.Format.IsDiagonalHeader)))
                        {
                            string dispText = !string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : (cell.RawValue ?? "");
                            Color txtColor = (cell.Format != null && cell.Format.TextColor != 0) ? Color.FromArgb(cell.Format.TextColor) : Color.Black;

                            FontStyle fontStyle = FontStyle.Regular;
                            if (cell.Format != null)
                            {
                                if (cell.Format.Bold) fontStyle |= FontStyle.Bold;
                                if (cell.Format.Italic) fontStyle |= FontStyle.Italic;
                                if (cell.Format.Underline) fontStyle |= FontStyle.Underline;
                                if (cell.Format.Strikethrough) fontStyle |= FontStyle.Strikeout;
                            }

                            string fName = (cell.Format != null && !string.IsNullOrEmpty(cell.Format.FontName)) ? cell.Format.FontName : "Calibri";
                            float fSize = (cell.Format != null && cell.Format.FontSize > 0) ? cell.Format.FontSize : 11.0f;

                            using (Font font = new Font(fName, fSize * ZoomFactor, fontStyle))
                            using (SolidBrush txtB = new SolidBrush(txtColor))
                            {
                                bool isDiagCell = cell.Format != null && (cell.Format.IsDiagonalHeader || (cell.Format.Borders != null && (cell.Format.Borders.DiagonalDown || cell.Format.Borders.DiagonalUp)));
                                if (isDiagCell && (dispText.Contains("\n") || !string.IsNullOrEmpty(cell.Format.DiagonalTopRightText) || !string.IsNullOrEmpty(cell.Format.DiagonalBottomLeftText)))
                                {
                                    string trText = cell.Format.DiagonalTopRightText;
                                    string blText = cell.Format.DiagonalBottomLeftText;
                                    if (string.IsNullOrEmpty(trText) && string.IsNullOrEmpty(blText))
                                    {
                                        string[] lines = dispText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (lines.Length >= 2)
                                        {
                                            trText = lines[0].Trim();
                                            blText = lines[1].Trim();
                                        }
                                        else
                                        {
                                            trText = dispText;
                                        }
                                    }

                                    // Top-Right Text (Column Header)
                                    if (!string.IsNullOrEmpty(trText))
                                    {
                                        SizeF szTR = g.MeasureString(trText, font);
                                        float xTR = cellRect.Right - szTR.Width - 6;
                                        float yTR = cellRect.Top + 3;
                                        g.DrawString(trText, font, txtB, xTR, yTR);
                                    }

                                    // Bottom-Left Text (Row Header)
                                    if (!string.IsNullOrEmpty(blText))
                                    {
                                        SizeF szBL = g.MeasureString(blText, font);
                                        float xBL = cellRect.Left + 6;
                                        float yBL = cellRect.Bottom - szBL.Height - 3;
                                        g.DrawString(blText, font, txtB, xBL, yBL);
                                    }
                                }
                                else
                                {
                                    SizeF sz = g.MeasureString(dispText, font);
                                    float drawX = curX + 5;
                                    float drawY = curY + (renderH - sz.Height) / 2;

                                    if (cell.Format != null)
                                    {
                                        if (cell.Format.Align == 1) drawX = curX + (renderW - sz.Width) / 2; // Center
                                        else if (cell.Format.Align == 2) drawX = curX + renderW - sz.Width - 6; // Right

                                        if (cell.Format.VAlign == 0) drawY = curY + 2; // Top
                                        else if (cell.Format.VAlign == 2) drawY = curY + renderH - sz.Height - 2; // Bottom
                                    }

                                    RectangleF clipRect = new RectangleF(curX, curY, renderW, renderH);
                                    g.SetClip(clipRect);
                                    g.DrawString(dispText, font, txtB, drawX, drawY);
                                    g.ResetClip();
                                }
                            }
                        }
                    }

                    // Render Sparkline in Cell if exists
                    long sparkKey = ((long)rowIdx << 32) | (long)colIdx;
                    if (Sparklines != null && Sparklines.ContainsKey(sparkKey))
                    {
                        DrawSparklineCell(g, new Rectangle(curX, curY, cW, rH), Sparklines[sparkKey]);
                    }

                    // Draw Table Header Filter Dropdown Button [▾] and Table Corner Resize Handle ◢
                    if (Tables != null)
                    {
                        foreach (var tbl in Tables)
                        {
                            if (tbl.ShowFilterButtons && tbl.HasHeaders && rowIdx == tbl.StartRow && colIdx >= tbl.StartCol && colIdx <= tbl.EndCol)
                            {
                                int btnSize = 14;
                                Rectangle fBtnRect = new Rectangle(curX + cW - btnSize - 3, curY + rH - btnSize - 4, btnSize, btnSize);
                                tbl.HeaderFilterButtonBounds[colIdx] = fBtnRect;

                                using (SolidBrush fbBg = new SolidBrush(Color.FromArgb(240, 243, 248)))
                                using (Pen fbBorder = new Pen(Color.FromArgb(170, 185, 205)))
                                {
                                    g.FillRectangle(fbBg, fBtnRect);
                                    g.DrawRectangle(fbBorder, fBtnRect);
                                }
                                using (Font arrowFont = new Font("Segoe UI", 7f, FontStyle.Bold))
                                {
                                    g.DrawString("▾", arrowFont, Brushes.DimGray, fBtnRect.X + 1, fBtnRect.Y);
                                }
                            }

                            if (rowIdx == tbl.EndRow && colIdx == tbl.EndCol)
                            {
                                Rectangle cHandle = new Rectangle(curX + cW - 8, curY + rH - 8, 8, 8);
                                tbl.ResizeCornerBounds = cHandle;
                                using (SolidBrush cBr = new SolidBrush(Color.FromArgb(16, 124, 65)))
                                {
                                    g.FillPolygon(cBr, new Point[] {
                                        new Point(cHandle.Right, cHandle.Top + 2),
                                        new Point(cHandle.Right, cHandle.Bottom),
                                        new Point(cHandle.Left + 2, cHandle.Bottom)
                                    });
                                }
                            }
                        }
                    }

                    curX += cW;
                    colIdx++;
                }

                curY += rH;
                rowIdx++;
            }

            // ==================== 3. Excel Green Selection & AutoFill Handle ====================
            int selX1 = GetColScreenX(minSelC, headerW);
            int selY1 = GetRowScreenY(minSelR, startY, headerH);

            int selW = 0;
            for (uint c = minSelC; c <= maxSelC; c++) selW += GetColWidth(c);
            int selH = 0;
            for (uint r = minSelR; r <= maxSelR; r++) selH += GetRowHeight(r);

            Rectangle gridClipArea = new Rectangle(headerW, startY + headerH, width - headerW - vScrollW, maxGridY - (startY + headerH));
            GraphicsState gStateSel = g.Save();
            g.SetClip(gridClipArea);

            if (selW > 0 && selH > 0)
            {
                // Range Soft Green Overlay
                if (minSelR != maxSelR || minSelC != maxSelC)
                {
                    using (SolidBrush rangeFill = new SolidBrush(Color.FromArgb(30, 16, 124, 65)))
                    {
                        g.FillRectangle(rangeFill, selX1, selY1, selW, selH);
                    }
                }

                // Excel Deep Green Border
                using (Pen selPen = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    g.DrawRectangle(selPen, selX1, selY1, selW, selH);
                }

                // Excel Fill Handle (Square dot at bottom-right corner)
                int handleSize = 6;
                Rectangle handleRect = new Rectangle(selX1 + selW - handleSize / 2, selY1 + selH - handleSize / 2, handleSize, handleSize);
                g.FillRectangle(Color.FromArgb(16, 124, 65).ToBrush(), handleRect);
                g.DrawRectangle(Pens.White, handleRect);
            }
            g.Restore(gStateSel);

            // ==================== 3.1 In-Place Cell Editor ====================
            ActiveEditingCellRect = editingCellRect;
            if (IsEditing && !editingCellRect.IsEmpty)
            {
                g.FillRectangle(Brushes.White, editingCellRect);
                using (Pen ep = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    g.DrawRectangle(ep, editingCellRect);
                }
                using (Font edFont = new Font("Calibri", 11f * ZoomFactor, FontStyle.Regular))
                {
                    // Selection highlight in editing cell
                    if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                    {
                        string pre = EditText.Substring(0, EditSelStart);
                        string sel = EditText.Substring(EditSelStart, EditSelLength);
                        SizeF preSz = pre.Length > 0 ? g.MeasureString(pre, edFont) : SizeF.Empty;
                        SizeF selSz = g.MeasureString(sel, edFont);

                        float selX = editingCellRect.X + 4 + (pre.Length > 0 ? preSz.Width : 0);
                        RectangleF selBox = new RectangleF(selX, editingCellRect.Y + 3, selSz.Width, editingCellRect.Height - 6);
                        using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(180, 215, 255)))
                        {
                            g.FillRectangle(selBrush, selBox);
                        }
                    }

                    g.DrawString(EditText, edFont, Brushes.Black, editingCellRect.X + 4, editingCellRect.Y + 3);

                    if (EditSelLength == 0)
                    {
                        string sub = EditText.Substring(0, Math.Min(EditText.Length, EditCursorPos));
                        SizeF sz = sub.Length > 0 ? g.MeasureString(sub, edFont) : SizeF.Empty;
                        float curXPos = editingCellRect.X + 4 + (sub.Length > 0 ? sz.Width : 0);
                        g.DrawLine(Pens.Black, curXPos, editingCellRect.Y + 3, curXPos, editingCellRect.Y + editingCellRect.Height - 4);
                    }
                }

                // Floating Formula Suggestions Popup
                if (EditText.StartsWith("="))
                {
                    DrawFormulaSuggestions(g, editingCellRect.X, editingCellRect.Bottom + 2);
                }
            }

            // ==================== 3.2 AutoFill Dragging Preview Rectangle ====================
            if (IsDraggingFillHandle)
            {
                uint fMinR = Math.Min(minSelR, FillDragTargetRow);
                uint fMaxR = Math.Max(maxSelR, FillDragTargetRow);
                uint fMinC = Math.Min(minSelC, FillDragTargetCol);
                uint fMaxC = Math.Max(maxSelC, FillDragTargetCol);

                int fX1 = GetColScreenX(fMinC, headerW);
                int fY1 = GetRowScreenY(fMinR, startY, headerH);

                int fW = 0;
                for (uint c = fMinC; c <= fMaxC; c++) fW += GetColWidth(c);
                int fH = 0;
                for (uint r = fMinR; r <= fMaxR; r++) fH += GetRowHeight(r);

                GraphicsState gStateFill = g.Save();
                g.SetClip(gridClipArea);
                using (Pen dashPen = new Pen(Color.FromArgb(16, 124, 65), 1.5f))
                {
                    dashPen.DashStyle = DashStyle.Dot;
                    g.DrawRectangle(dashPen, fX1, fY1, fW, fH);
                }
                g.Restore(gStateFill);
            }

            // ==================== 3.2b Table Resize Dragging Preview Rectangle ====================
            if (IsDraggingTableResize && ResizingTable != null)
            {
                uint pStartR = ResizingTable.StartRow;
                uint pEndR = Math.Max(ResizingTable.EndRow, TableResizeTargetRow);
                uint pStartC = ResizingTable.StartCol;
                uint pEndC = Math.Max(ResizingTable.EndCol, TableResizeTargetCol);

                int pX1 = GetColScreenX(pStartC, headerW);
                int pY1 = GetRowScreenY(pStartR, startY, headerH);

                int pW = 0;
                for (uint c = pStartC; c <= pEndC; c++) pW += GetColWidth(c);
                int pH = 0;
                for (uint r = pStartR; r <= pEndR; r++) pH += GetRowHeight(r);

                GraphicsState gStateTbl = g.Save();
                g.SetClip(gridClipArea);
                using (Pen dashPen = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    dashPen.DashStyle = DashStyle.Dot;
                    g.DrawRectangle(dashPen, pX1, pY1, pW, pH);
                }
                g.Restore(gStateTbl);
            }

            // ==================== 3.3 Drawing Objects: Shapes & Pictures ====================
            if (Shapes != null && Shapes.Count > 0)
            {
                GraphicsState gsGrid = g.Save();
                Rectangle gridClip = new Rectangle(headerW, startY + headerH, width - headerW - vScrollW, maxGridY - (startY + headerH));
                g.SetClip(gridClip);

                foreach (var sh in Shapes)
                {
                    Rectangle sBounds = GetShapeScreenBounds(sh, headerW, startY, headerH);
                    ShapeRenderer.DrawShape(g, sh, sBounds, null, ZoomFactor);

                    if (sh == SelectedShape)
                    {
                        using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
                        {
                            g.DrawRectangle(selPen, sBounds);
                        }

                        Rectangle[] handles = GetHandleRectangles(sBounds);
                        for (int hIdx = 0; hIdx < 8; hIdx++)
                        {
                            Rectangle hr = handles[hIdx];
                            g.FillRectangle(Brushes.White, hr);
                            using (Pen hp = new Pen(Color.FromArgb(37, 99, 235), 1.2f))
                            {
                                g.DrawRectangle(hp, hr);
                            }
                        }

                        if (sh.ShapeType == "Chart" || sh.ChartData != null)
                        {
                            DrawChartFloatingButtons(g, sBounds);
                        }
                    }
                }

                g.Restore(gsGrid);
            }

            // ==================== 4. Vertical Scrollbar (Right Edge) ====================
            int vScrollX = width - vScrollW;
            int vScrollY = startY + headerH;
            int vScrollH = maxGridY - vScrollY;

            Rectangle vTrack = new Rectangle(vScrollX, vScrollY, vScrollW, vScrollH);
            using (SolidBrush trB = new SolidBrush(Color.FromArgb(243, 244, 246))) g.FillRectangle(trB, vTrack);
            g.DrawLine(Pens.LightGray, vScrollX, vScrollY, vScrollX, maxGridY);

            // Up Arrow ▲
            Rectangle upBtn = new Rectangle(vScrollX, vScrollY, vScrollW, 16);
            g.FillRectangle(Brushes.WhiteSmoke, upBtn);
            g.DrawRectangle(Pens.LightGray, upBtn);
            using (Font aFont = new Font("Segoe UI", 7f, FontStyle.Bold))
            {
                g.DrawString("▲", aFont, Brushes.DimGray, upBtn.X + 3, upBtn.Y + 2);
            }

            // Down Arrow ▼
            Rectangle dnBtn = new Rectangle(vScrollX, maxGridY - 16, vScrollW, 16);
            g.FillRectangle(Brushes.WhiteSmoke, dnBtn);
            g.DrawRectangle(Pens.LightGray, dnBtn);
            using (Font aFont = new Font("Segoe UI", 7f, FontStyle.Bold))
            {
                g.DrawString("▼", aFont, Brushes.DimGray, dnBtn.X + 3, dnBtn.Y + 2);
            }

            // Vertical Thumb
            int vTrackAvail = vScrollH - 32;
            int vThumbH = Math.Max(25, (int)(vTrackAvail * 0.25f));
            int vThumbY = vScrollY + 16 + (int)((ScrollRow - 1) / 100.0f * (vTrackAvail - vThumbH));
            vThumbY = Math.Max(vScrollY + 16, Math.Min(maxGridY - 16 - vThumbH, vThumbY));
            Rectangle vThumb = new Rectangle(vScrollX + 2, vThumbY, vScrollW - 4, vThumbH);
            using (GraphicsPath tp = CreateRoundedPath(vThumb, 3))
            {
                g.FillPath(Brushes.DarkGray, tp);
            }

            // ==================== 5. Bottom Sheet Tabs Bar & Horizontal Scrollbar ====================
            int tabY = height - 56;
            using (SolidBrush tabBgB = new SolidBrush(Color.FromArgb(243, 244, 246))) g.FillRectangle(tabBgB, 0, tabY, width, 28);
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240))) g.DrawLine(p, 0, tabY, width, tabY);

            // Sheet Navigation Buttons ◀ ▶
            using (Font navFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.DrawString("◀", navFont, Brushes.Gray, 8, tabY + 5);
                g.DrawString("▶", navFont, Brushes.Gray, 24, tabY + 5);
            }

            int tabX = 46;
            using (Font tabFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (Font activeTabFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                for (int s = 0; s < SheetNames.Count; s++)
                {
                    bool isActive = (s == ActiveSheetIndex);
                    string sName = SheetNames[s];
                    SizeF sz = g.MeasureString(sName, isActive ? activeTabFont : tabFont);
                    int tabW = (int)sz.Width + 24;

                    Rectangle tabRect = new Rectangle(tabX, tabY + 2, tabW, 25);
                    if (isActive)
                    {
                        g.FillRectangle(Brushes.White, tabRect);
                        using (Pen p = new Pen(Color.FromArgb(209, 213, 219)))
                        {
                            g.DrawLine(p, tabRect.Left, tabRect.Top, tabRect.Left, tabRect.Bottom);
                            g.DrawLine(p, tabRect.Right, tabRect.Top, tabRect.Right, tabRect.Bottom);
                        }
                        // Active Green Underline Bar
                        using (SolidBrush ab = new SolidBrush(Color.FromArgb(16, 124, 65)))
                        {
                            g.FillRectangle(ab, tabRect.Left, tabRect.Bottom - 3, tabRect.Width, 3);
                        }
                    }

                    using (SolidBrush inactTxtB = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    {
                        g.DrawString(sName, isActive ? activeTabFont : tabFont, isActive ? Color.FromArgb(16, 124, 65).ToBrush() : inactTxtB, tabX + 12, tabY + 5);
                    }
                    tabX += tabW + 2;
                }

                // Add Sheet Button (+)
                Rectangle addTabRect = new Rectangle(tabX + 4, tabY + 4, 20, 20);
                using (GraphicsPath addPath = CreateRoundedPath(addTabRect, 10))
                {
                    g.FillPath(Brushes.White, addPath);
                    using (Pen ap = new Pen(Color.FromArgb(203, 213, 225))) g.DrawPath(ap, addPath);
                }
                using (Font plusFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                {
                    g.DrawString("+", plusFont, Color.FromArgb(16, 124, 65).ToBrush(), addTabRect.X + 4, addTabRect.Y - 1);
                }
                tabX += 32;
            }

            // 5.1 Horizontal Scrollbar (from tabX + 15 to width - vScrollW - 5)
            int hScrollX = tabX + 15;
            int hScrollW = (width - vScrollW - 5) - hScrollX;
            if (hScrollW > 100)
            {
                Rectangle hTrack = new Rectangle(hScrollX, tabY + 4, hScrollW, 20);
                g.FillRectangle(Brushes.WhiteSmoke, hTrack);
                g.DrawRectangle(Pens.LightGray, hTrack);

                // Left Arrow ◀
                Rectangle lBtn = new Rectangle(hScrollX, tabY + 4, 16, 20);
                g.FillRectangle(Brushes.White, lBtn);
                g.DrawRectangle(Pens.LightGray, lBtn);
                using (Font aFont = new Font("Segoe UI", 7f, FontStyle.Bold))
                {
                    g.DrawString("◀", aFont, Brushes.DimGray, lBtn.X + 3, lBtn.Y + 3);
                }

                // Right Arrow ▶
                Rectangle rBtn = new Rectangle(hScrollX + hScrollW - 16, tabY + 4, 16, 20);
                g.FillRectangle(Brushes.White, rBtn);
                g.DrawRectangle(Pens.LightGray, rBtn);
                using (Font aFont = new Font("Segoe UI", 7f, FontStyle.Bold))
                {
                    g.DrawString("▶", aFont, Brushes.DimGray, rBtn.X + 4, rBtn.Y + 3);
                }

                // Horizontal Thumb
                int hTrackAvail = hScrollW - 32;
                int hThumbW = Math.Max(30, (int)(hTrackAvail * 0.3f));
                int hThumbX = hScrollX + 16 + (int)((ScrollCol - 1) / 50.0f * (hTrackAvail - hThumbW));
                hThumbX = Math.Max(hScrollX + 16, Math.Min(hScrollX + hScrollW - 16 - hThumbW, hThumbX));
                Rectangle hThumb = new Rectangle(hThumbX, tabY + 6, hThumbW, 16);
                using (GraphicsPath tp = CreateRoundedPath(hThumb, 3))
                {
                    g.FillPath(Brushes.DarkGray, tp);
                }
            }

            // ==================== 6. Excel Status Bar (Y: height - 28 to height) ====================
            int statY = height - 28;
            g.FillRectangle(Brushes.White, 0, statY, width, 28);
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240))) g.DrawLine(p, 0, statY, width, statY);

            using (Font statFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font statBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                // Left: Ready / Edit
                using (SolidBrush statB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString(IsEditing ? "Chỉnh sửa (Edit)" : "Sẵn sàng (Ready)", statFont, statB, 12, statY + 6);
                }

                // Calculation Metrics (Average, Count, Sum for selected range)
                double selSum = 0.0;
                double selCount = 0;
                double numCount = 0;

                for (uint r = minSelR; r <= maxSelR; r++)
                {
                    for (uint c = minSelC; c <= maxSelC; c++)
                    {
                        CellData cd = CurrentSheet.GetCell(r, c);
                        if (cd != null && (!string.IsNullOrEmpty(cd.RawValue) || !string.IsNullOrEmpty(cd.EvaluatedStr)))
                        {
                            selCount++;
                            if (cd.Type == CellType.Number || (cd.Type == CellType.Formula && cd.NumericValue != 0))
                            {
                                selSum += cd.NumericValue;
                                numCount++;
                            }
                        }
                    }
                }

                if (selCount > 1)
                {
                    string statMetrics = string.Format("Trung bình: {0:N2}    Đếm: {1}    Tổng: {2:N2}", (numCount > 0 ? (selSum / numCount) : 0), selCount, selSum);
                    SizeF mSz = g.MeasureString(statMetrics, statBold);
                    g.DrawString(statMetrics, statBold, Color.FromArgb(16, 124, 65).ToBrush(), width - mSz.Width - 160, statY + 6);
                }

                // Rightmost: View Mode buttons & Zoom Level
                using (SolidBrush zoomB = new SolidBrush(Color.FromArgb(51, 65, 85)))
                {
                    g.DrawString(string.Format("{0}%", (int)(ZoomFactor * 100)), statFont, zoomB, width - 50, statY + 6);
                }
            }
        }

        public void FilterFormulaSuggestions(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                ActiveSuggestions = new List<FormulaSuggestion>(AllFormulas);
            }
            else
            {
                string q = query.Trim().ToUpper();
                ActiveSuggestions = AllFormulas.FindAll(f => f.Name.StartsWith(q));
            }
            SelectedSuggestionIndex = 0;
        }

        private void DrawFormulaSuggestions(Graphics g, int x, int y)
        {
            string query = EditText.Length > 1 ? EditText.Substring(1).Trim().ToUpper() : "";
            FilterFormulaSuggestions(query);
            if (ActiveSuggestions.Count == 0) return;

            int popW = 280;
            int popH = Math.Min(6, ActiveSuggestions.Count) * 26 + 30;
            Rectangle popRect = new Rectangle(x, y, popW, popH);

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(250, 250, 250))) g.FillRectangle(bg, popRect);
            g.DrawRectangle(Pens.DarkSeaGreen, popRect);

            using (Font fFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font dFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                int itemY = y + 4;
                for (int i = 0; i < Math.Min(6, ActiveSuggestions.Count); i++)
                {
                    var sug = ActiveSuggestions[i];
                    Rectangle itemRect = new Rectangle(x + 2, itemY, popW - 4, 24);
                    if (i == SelectedSuggestionIndex)
                    {
                        g.FillRectangle(Brushes.LightGreen, itemRect);
                    }

                    g.DrawString(sug.Name, fFont, Brushes.Black, x + 6, itemY + 3);
                    g.DrawString(sug.Syntax, dFont, Brushes.DimGray, x + 80, itemY + 4);
                    itemY += 26;
                }

                if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < ActiveSuggestions.Count)
                {
                    g.DrawLine(Pens.LightGray, x, itemY, x + popW, itemY);
                    g.DrawString(ActiveSuggestions[SelectedSuggestionIndex].Description, dFont, Brushes.DarkSlateGray, x + 6, itemY + 4);
                }
            }
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ==================== Cursor Detection ====================

        public Cursor GetCursorForPosition(int x, int y, int width, int height)
        {
            int fBarY = 155;
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = ShowFormulaBar ? (fBarY + 30) : fBarY;

            // 0. Shape & Resize Handles Cursor
            if (SelectedShape != null)
            {
                int h;
                DocShapeObject hit = HitTestShape(x, y, headerW, startY, headerH, out h);
                if (hit == SelectedShape)
                {
                    if (h == 0 || h == 4) return Cursors.SizeNWSE;
                    if (h == 2 || h == 6) return Cursors.SizeNESW;
                    if (h == 1 || h == 5) return Cursors.SizeNS;
                    if (h == 3 || h == 7) return Cursors.SizeWE;
                    if (h == 8) return Cursors.SizeAll;
                    if (h == -1 && SelectedShape.ShapeType != "Image") return Cursors.IBeam;
                }
            }

            // 0b. Check Tables Interaction Cursors (Handle, Rims, Corner)
            if (Tables != null)
            {
                foreach (var tbl in Tables)
                {
                    if (tbl.ResizeCornerBounds.Contains(x, y)) return Cursors.SizeNWSE;

                    int tblLeft = headerW;
                    for (uint c = ScrollCol; c < tbl.StartCol; c++) tblLeft += GetColWidth(c);
                    int tblRight = tblLeft;
                    for (uint c = tbl.StartCol; c <= tbl.EndCol; c++) tblRight += GetColWidth(c);

                    int tblTop = startY + headerH;
                    for (uint r = ScrollRow; r < tbl.StartRow; r++) tblTop += GetRowHeight(r);
                    int tblBottom = tblTop;
                    for (uint r = tbl.StartRow; r <= tbl.EndRow; r++) tblBottom += GetRowHeight(r);

                    // Top-Left Corner Rim of Table
                    if (Math.Abs(x - tblLeft) <= 5 && Math.Abs(y - tblTop) <= 5)
                    {
                        return ExcelDiagArrowCursor;
                    }

                    // Top Edge / Rim of Table (3px near the top border of table header)
                    if (Math.Abs(y - tblTop) <= 4 && x >= tblLeft && x <= tblRight)
                    {
                        return ExcelDownArrowCursor;
                    }

                    // Left Edge / Rim of Table (3px near the left border of table first column)
                    if (Math.Abs(x - tblLeft) <= 4 && y >= tblTop && y <= tblBottom)
                    {
                        return ExcelRightArrowCursor;
                    }
                }
            }

            // 1. Column Header (Resize Column Cursor ↔ or Down Arrow Cursor ↓)
            if (ShowHeadings && y >= startY && y <= startY + headerH && x >= headerW)
            {
                int curX = headerW;
                uint cIdx = ScrollCol;
                while (curX < width - 16)
                {
                    int cW = GetColWidth(cIdx);
                    int divX = curX + cW;
                    if (Math.Abs(x - divX) <= 4)
                    {
                        return Cursors.VSplit;
                    }
                    curX += cW;
                    cIdx++;
                }
                return ExcelDownArrowCursor;
            }

            // 2. Row Header (Resize Row Cursor ↕ or Right Arrow Cursor →)
            if (ShowHeadings && x >= 0 && x <= headerW && y >= startY + headerH && y < height - 56)
            {
                int curY = startY + headerH;
                uint rIdx = ScrollRow;
                while (curY < height - 56)
                {
                    int rH = GetRowHeight(rIdx);
                    int divY = curY + rH;
                    if (Math.Abs(y - divY) <= 4)
                    {
                        return Cursors.HSplit;
                    }
                    curY += rH;
                    rIdx++;
                }
                return ExcelRightArrowCursor;
            }

            // 3. Fill Handle Square Hover (Excel Thin Solid Black Cross `+`)
            uint minSelC = Math.Min(SelectedCol, SelEndCol);
            uint maxSelC = Math.Max(SelectedCol, SelEndCol);
            uint minSelR = Math.Min(SelectedRow, SelEndRow);
            uint maxSelR = Math.Max(SelectedRow, SelEndRow);

            int selX1 = headerW;
            for (uint c = ScrollCol; c < minSelC; c++) selX1 += GetColWidth(c);
            int selY1 = startY + headerH;
            for (uint r = ScrollRow; r < minSelR; r++) selY1 += GetRowHeight(r);

            int selW = 0;
            for (uint c = minSelC; c <= maxSelC; c++) selW += GetColWidth(c);
            int selH = 0;
            for (uint r = minSelR; r <= maxSelR; r++) selH += GetRowHeight(r);

            int handleX = selX1 + selW;
            int handleY = selY1 + selH;
            if (Math.Abs(x - handleX) <= 5 && Math.Abs(y - handleY) <= 5)
            {
                return Cursors.Cross;
            }

            // 4. Formula Bar input box
            if (y >= fBarY + 3 && y <= fBarY + 26 && x >= 170)
            {
                return Cursors.IBeam;
            }

            // 5. Grid Cells Area (Iconic Excel White Thick Plus Cursor `+`)
            if (x >= headerW && x < width - 16 && y >= startY + headerH && y < height - 56)
            {
                return ExcelCellSelectCursor;
            }

            return Cursors.Default;
        }

        // ==================== Mouse Interaction ====================

        public bool HandleMouseDown(int x, int y, int width, int height, MouseButtons btn, Form parentForm = null)
        {
            if (btn != MouseButtons.Left) return false;

            int fBarY = 155;
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = ShowFormulaBar ? (fBarY + 30) : fBarY;
            int vScrollW = 16;
            int bottomBarH = 56;
            int maxGridY = height - bottomBarH;

            // 1. Check Vertical Scrollbar Clicks
            int vScrollX = width - vScrollW;
            if (x >= vScrollX && y >= startY + headerH && y <= maxGridY)
            {
                if (y <= startY + headerH + 16) // Up arrow
                {
                    if (ScrollRow > 1) ScrollRow--;
                }
                else if (y >= maxGridY - 16) // Down arrow
                {
                    ScrollRow++;
                }
                else // Thumb / Track
                {
                    IsDraggingVertScroll = true;
                    ScrollDragStartPos = y;
                    ScrollDragStartVal = ScrollRow;
                }
                return true;
            }

            // 2. Check Sheet Tabs & Horizontal Scrollbar Clicks (Y: height - 56 to height - 28)
            int tabY = height - 56;
            if (y >= tabY && y <= tabY + 28)
            {
                int tabX = 46;
                for (int s = 0; s < SheetNames.Count; s++)
                {
                    using (Font f = new Font("Segoe UI", 9f))
                    {
                        SizeF sz = SizeF.Empty;
                        using (Bitmap b = new Bitmap(1, 1))
                        using (Graphics g = Graphics.FromImage(b)) sz = g.MeasureString(SheetNames[s], f);
                        int tabW = (int)sz.Width + 24;
                        if (x >= tabX && x <= tabX + tabW)
                        {
                            SwitchToSheet(s);
                            return true;
                        }
                        tabX += tabW + 2;
                    }
                }

                if (x >= tabX + 4 && x <= tabX + 24) // Add Sheet (+)
                {
                    AddSheet("Sheet" + (SheetNames.Count + 1));
                    SwitchToSheet(SheetNames.Count - 1);
                    return true;
                }

                int hScrollX = tabX + 15;
                int hScrollW = (width - vScrollW - 5) - hScrollX;
                if (x >= hScrollX && x <= hScrollX + hScrollW)
                {
                    if (x <= hScrollX + 16) // Left arrow
                    {
                        if (ScrollCol > 1) ScrollCol--;
                    }
                    else if (x >= hScrollX + hScrollW - 16) // Right arrow
                    {
                        ScrollCol++;
                    }
                    else
                    {
                        IsDraggingHorzScroll = true;
                        ScrollDragStartPos = x;
                        ScrollDragStartVal = ScrollCol;
                    }
                    return true;
                }
            }

            // 3. Check Column Resize Divider MouseDown
            if (ShowHeadings && y >= startY && y <= startY + headerH && x >= headerW)
            {
                int curX = headerW;
                uint cIdx = ScrollCol;
                while (curX < width - vScrollW)
                {
                    int cW = GetColWidth(cIdx);
                    int divX = curX + cW;
                    if (Math.Abs(x - divX) <= 4)
                    {
                        IsResizingCol = true;
                        ResizingColIndex = cIdx;
                        ResizeStartMouse = new Point(x, y);
                        ResizeStartSize = cW;
                        return true;
                    }
                    curX += cW;
                    cIdx++;
                }
            }

            // 4. Check Row Resize Divider MouseDown
            if (ShowHeadings && x >= 0 && x <= headerW && y >= startY + headerH && y < maxGridY)
            {
                int curY = startY + headerH;
                uint rIdx = ScrollRow;
                while (curY < maxGridY)
                {
                    int rH = GetRowHeight(rIdx);
                    int divY = curY + rH;
                    if (Math.Abs(y - divY) <= 4)
                    {
                        IsResizingRow = true;
                        ResizingRowIndex = rIdx;
                        ResizeStartMouse = new Point(x, y);
                        ResizeStartSize = rH;
                        return true;
                    }
                    curY += rH;
                    rIdx++;
                }
            }

            // 3b. Column Header Click (Select Entire Column)
            if (ShowHeadings && y >= startY && y <= startY + headerH && x >= headerW)
            {
                uint cIdx = GetColAtX(x, headerW);
                IsSelectingFullCols = true;
                SelectedRow = 1;
                SelEndRow = 1000;
                SelectedCol = cIdx;
                SelEndCol = cIdx;
                return true;
            }

            // 4a. Row Header Click (Select Entire Row)
            if (ShowHeadings && x >= 0 && x <= headerW && y >= startY + headerH && y < maxGridY)
            {
                uint rIdx = GetRowAtY(y, startY + headerH);
                IsSelectingFullRows = true;
                SelectedCol = 1;
                SelEndCol = 250;
                SelectedRow = rIdx;
                SelEndRow = rIdx;
                return true;
            }

            // 4a0. Top-Left Select All Triangle Click
            if (ShowHeadings && x >= 0 && x <= headerW && y >= startY && y <= startY + headerH)
            {
                SelectedCol = 1;
                SelEndCol = 250;
                SelectedRow = 1;
                SelEndRow = 1000;
                return true;
            }

            // 4a1. Check Table Interactions (Corner Handle, Corner Rim, Top Rim, Left Rim)
            if (Tables != null)
            {
                foreach (var tbl in Tables)
                {
                    // Table Corner Handle MouseDown
                    if (tbl.ResizeCornerBounds.Contains(x, y))
                    {
                        IsDraggingTableResize = true;
                        ResizingTable = tbl;
                        TableResizeTargetRow = tbl.EndRow;
                        TableResizeTargetCol = tbl.EndCol;
                        return true;
                    }

                    int tblLeft = headerW;
                    for (uint c = ScrollCol; c < tbl.StartCol; c++) tblLeft += GetColWidth(c);
                    int tblRight = tblLeft;
                    for (uint c = tbl.StartCol; c <= tbl.EndCol; c++) tblRight += GetColWidth(c);

                    int tblTop = startY + headerH;
                    for (uint r = ScrollRow; r < tbl.StartRow; r++) tblTop += GetRowHeight(r);
                    int tblBottom = tblTop;
                    for (uint r = tbl.StartRow; r <= tbl.EndRow; r++) tblBottom += GetRowHeight(r);

                    // Top-Left Corner Rim of Table
                    if (Math.Abs(x - tblLeft) <= 5 && Math.Abs(y - tblTop) <= 5)
                    {
                        if (SelectedRow == tbl.StartRow && SelEndRow == tbl.EndRow && SelectedCol == tbl.StartCol && SelEndCol == tbl.EndCol)
                        {
                            SelectedRow = tbl.HasHeaders ? (tbl.StartRow + 1) : tbl.StartRow;
                            SelEndRow = tbl.EndRow;
                            SelectedCol = tbl.StartCol;
                            SelEndCol = tbl.EndCol;
                        }
                        else
                        {
                            SelectedRow = tbl.StartRow;
                            SelEndRow = tbl.EndRow;
                            SelectedCol = tbl.StartCol;
                            SelEndCol = tbl.EndCol;
                        }
                        return true;
                    }

                    // Top Edge / Rim of Table (Select column data or column with header)
                    if (Math.Abs(y - tblTop) <= 4 && x >= tblLeft && x <= tblRight)
                    {
                        uint cTarget = GetColAtX(x, headerW);
                        if (cTarget >= tbl.StartCol && cTarget <= tbl.EndCol)
                        {
                            if (SelectedCol == cTarget && SelEndCol == cTarget && SelectedRow == (tbl.HasHeaders ? (tbl.StartRow + 1) : tbl.StartRow) && SelEndRow == tbl.EndRow)
                            {
                                SelectedRow = tbl.StartRow;
                                SelEndRow = tbl.EndRow;
                                SelectedCol = cTarget;
                                SelEndCol = cTarget;
                            }
                            else
                            {
                                SelectedRow = tbl.HasHeaders ? (tbl.StartRow + 1) : tbl.StartRow;
                                SelEndRow = tbl.EndRow;
                                SelectedCol = cTarget;
                                SelEndCol = cTarget;
                            }
                            return true;
                        }
                    }

                    // Left Edge / Rim of Table (Select table row)
                    if (Math.Abs(x - tblLeft) <= 4 && y >= tblTop && y <= tblBottom)
                    {
                        uint rTarget = GetRowAtY(y, startY + headerH);
                        if (rTarget >= tbl.StartRow && rTarget <= tbl.EndRow)
                        {
                            SelectedRow = rTarget;
                            SelEndRow = rTarget;
                            SelectedCol = tbl.StartCol;
                            SelEndCol = tbl.EndCol;
                            return true;
                        }
                    }
                }
            }

            // 4b. Check Table Header Filter Button Clicks
            if (Tables != null)
            {
                foreach (var tbl in Tables)
                {
                    foreach (var btnKvp in tbl.HeaderFilterButtonBounds)
                    {
                        if (btnKvp.Value.Contains(x, y))
                        {
                            Point pt = (parentForm != null) ? parentForm.PointToScreen(new Point(btnKvp.Value.Left, btnKvp.Value.Bottom + 2)) : new Point(x, y);
                            TableColumnFilterPopupForm filterForm = new TableColumnFilterPopupForm(this, tbl, btnKvp.Key, pt);
                            filterForm.Show();
                            return true;
                        }
                    }
                }
            }

            // Check Click on Chart Floating Action Buttons
            if (SelectedShape != null && (SelectedShape.ShapeType == "Chart" || SelectedShape.ChartData != null))
            {
                DocChartData cd = SelectedShape.ChartData ?? DocChartData.CreateDefault(SelectedShape.TextContent);
                SelectedShape.ChartData = cd;

                if (!PlusBtnChartRect.IsEmpty && PlusBtnChartRect.Contains(x, y))
                {
                    Point pt = parentForm != null ? parentForm.PointToScreen(new Point(PlusBtnChartRect.Right + 4, PlusBtnChartRect.Top)) : new Point(PlusBtnChartRect.Right + 4, PlusBtnChartRect.Top);
                    ChartElementsPopupForm elemForm = new ChartElementsPopupForm(pt, cd, () => { if (parentForm != null) parentForm.Invalidate(); });
                    elemForm.Show();
                    return true;
                }
                else if (!StyleBtnChartRect.IsEmpty && StyleBtnChartRect.Contains(x, y))
                {
                    Point pt = parentForm != null ? parentForm.PointToScreen(new Point(StyleBtnChartRect.Right + 4, StyleBtnChartRect.Top)) : new Point(StyleBtnChartRect.Right + 4, StyleBtnChartRect.Top);
                    ChartStylesPopupForm styleForm = new ChartStylesPopupForm(pt, cd, () => { if (parentForm != null) parentForm.Invalidate(); });
                    styleForm.Show();
                    return true;
                }
                else if (!FilterBtnChartRect.IsEmpty && FilterBtnChartRect.Contains(x, y))
                {
                    Point pt = parentForm != null ? parentForm.PointToScreen(new Point(FilterBtnChartRect.Right + 4, FilterBtnChartRect.Top)) : new Point(FilterBtnChartRect.Right + 4, FilterBtnChartRect.Top);
                    ChartFiltersPopupForm filterForm = new ChartFiltersPopupForm(pt, cd, () => { if (parentForm != null) parentForm.Invalidate(); });
                    filterForm.Show();
                    return true;
                }
            }

            // 4c. Check Shape Hit-Test (Selection / Text Editing / Dragging / Resizing)
            int shHandle;
            DocShapeObject hitSh = HitTestShape(x, y, headerW, startY, headerH, out shHandle);
            if (hitSh != null)
            {
                if (Shapes != null)
                {
                    foreach (var s in Shapes) { if (s != hitSh) { s.IsSelected = false; s.IsEditingText = false; } }
                }
                SelectedShape = hitSh;
                SelectedShapeHandle = shHandle;
                hitSh.IsSelected = true;
                if (hitSh.ShapeType != "Image")
                {
                    hitSh.IsEditingText = true;
                }

                // Calculate cursor position inside shape text
                if (hitSh.IsEditingText)
                {
                    using (Bitmap tmpBmp = new Bitmap(1, 1))
                    using (Graphics gTmp = Graphics.FromImage(tmpBmp))
                    {
                        FontStyle fStyle = FontStyle.Regular;
                        if (hitSh.Bold) fStyle |= FontStyle.Bold;
                        if (hitSh.Italic) fStyle |= FontStyle.Italic;
                        using (Font txtF = new Font(hitSh.FontFamily ?? "Arial", (hitSh.FontSize > 0 ? hitSh.FontSize : 11f) * ZoomFactor, fStyle))
                        {
                            Rectangle sBounds = GetShapeScreenBounds(hitSh, headerW, startY, headerH);
                            Rectangle innerTb = new Rectangle(sBounds.X + 8, sBounds.Y + 6, Math.Max(10, sBounds.Width - 16), Math.Max(10, sBounds.Height - 12));
                            hitSh.TextCursorIndex = ShapeRenderer.GetCharIndexFromPoint(gTmp, hitSh, innerTb, txtF, ZoomFactor, x, y);
                        }
                    }
                }

                // Exit in-cell editing if active so keystrokes don't leak into background cells
                if (IsEditing)
                {
                    SaveUndoState();
                    CurrentSheet.SetCellText(SelectedRow, SelectedCol, EditText);
                    FormulaEngine.EvaluateSheet(CurrentSheet);
                    IsEditing = false;
                    EditSelLength = 0;
                }

                DragStartMouse = new Point(x, y);
                DragStartShapePos = new Point(hitSh.PageX, hitSh.PageY);
                DragStartShapeSize = new Size(hitSh.BaseWidth + hitSh.WidthDelta, hitSh.BaseHeight + hitSh.HeightDelta);
                if (parentForm != null) parentForm.Invalidate();
                return true;
            }
            else if (y >= startY + headerH && y <= maxGridY && x >= headerW && x <= width - vScrollW)
            {
                if (SelectedShape != null)
                {
                    SelectedShape.IsSelected = false;
                    SelectedShape.IsEditingText = false;
                }
                SelectedShape = null;
                SelectedShapeHandle = -1;
            }

            // 5. Check Fill Handle MouseDown
            uint minSelC = Math.Min(SelectedCol, SelEndCol);
            uint maxSelC = Math.Max(SelectedCol, SelEndCol);
            uint minSelR = Math.Min(SelectedRow, SelEndRow);
            uint maxSelR = Math.Max(SelectedRow, SelEndRow);

            int selX1 = GetColScreenX(minSelC, headerW);
            int selY1 = GetRowScreenY(minSelR, startY, headerH);

            int selW = 0;
            for (uint c = minSelC; c <= maxSelC; c++) selW += GetColWidth(c);
            int selH = 0;
            for (uint r = minSelR; r <= maxSelR; r++) selH += GetRowHeight(r);

            int handleX = selX1 + selW;
            int handleY = selY1 + selH;
            if (Math.Abs(x - handleX) <= 6 && Math.Abs(y - handleY) <= 6)
            {
                IsDraggingFillHandle = true;
                FillStartRow = minSelR;
                FillStartCol = minSelC;
                FillEndRow = maxSelR;
                FillEndCol = maxSelC;
                FillDragTargetRow = maxSelR;
                FillDragTargetCol = maxSelC;
                return true;
            }

            // 6. Check Grid Cell Selection MouseDown
            if (x > headerW && y > startY + headerH && y < maxGridY)
            {
                uint cIdx = GetColAtX(x, headerW);
                uint rIdx = GetRowAtY(y, startY + headerH);

                if (IsEditing && cIdx == SelectedCol && rIdx == SelectedRow)
                {
                    int cellLeft = headerW;
                    for (uint c = ScrollCol; c < SelectedCol; c++) cellLeft += GetColWidth(c);
                    using (Font edFont = new Font("Calibri", 11f * ZoomFactor, FontStyle.Regular))
                    {
                        int charIdx = GetTextCharIndexAtX(x, cellLeft, edFont);
                        EditCursorPos = charIdx;
                        EditSelectionAnchor = charIdx;
                        EditSelStart = charIdx;
                        EditSelLength = 0;
                        IsDraggingTextSelection = true;
                    }
                    return true;
                }

                if (IsEditing)
                {
                    SaveUndoState();
                    CurrentSheet.SetCellText(SelectedRow, SelectedCol, EditText);
                    FormulaEngine.EvaluateSheet(CurrentSheet);
                    IsEditing = false;
                    EditSelLength = 0;
                }

                SelectedCol = cIdx;
                SelectedRow = rIdx;
                SelEndCol = cIdx;
                SelEndRow = rIdx;
                IsSelectingRange = true;
                return false;
            }

            return false;
        }

        public void HandleMouseMove(int x, int y, int width, int height, MouseButtons btn)
        {
            LastMousePos = new Point(x, y);
            int fBarY = 155;
            int btnStartX = 8 + 75 + 12;

            IsHoveringCancel = (y >= fBarY + 3 && y <= fBarY + 26 && x >= btnStartX && x <= btnStartX + 23);
            IsHoveringEnter = (y >= fBarY + 3 && y <= fBarY + 26 && x >= btnStartX + 25 && x <= btnStartX + 48);
            IsHoveringFx = (y >= fBarY + 3 && y <= fBarY + 26 && x >= btnStartX + 50 && x <= btnStartX + 76);
            IsHoveringNameBox = (y >= fBarY + 3 && y <= fBarY + 26 && x >= 8 && x <= 83);

            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = ShowFormulaBar ? (fBarY + 30) : fBarY;

            // Handle Shape Dragging or Resizing
            if (SelectedShape != null && SelectedShapeHandle >= 0 && btn == MouseButtons.Left)
            {
                int dx = (int)((x - DragStartMouse.X) / ZoomFactor);
                int dy = (int)((y - DragStartMouse.Y) / ZoomFactor);

                if (SelectedShapeHandle == 8) // Move
                {
                    SelectedShape.PageX = Math.Max(0, DragStartShapePos.X + dx);
                    SelectedShape.PageY = Math.Max(0, DragStartShapePos.Y + dy);
                }
                else // Resize
                {
                    int newW = DragStartShapeSize.Width;
                    int newH = DragStartShapeSize.Height;
                    int newX = DragStartShapePos.X;
                    int newY = DragStartShapePos.Y;

                    if (SelectedShapeHandle == 4) // BR
                    {
                        newW = Math.Max(20, DragStartShapeSize.Width + dx);
                        newH = Math.Max(20, DragStartShapeSize.Height + dy);
                    }
                    else if (SelectedShapeHandle == 3) // MR
                    {
                        newW = Math.Max(20, DragStartShapeSize.Width + dx);
                    }
                    else if (SelectedShapeHandle == 5) // BC
                    {
                        newH = Math.Max(20, DragStartShapeSize.Height + dy);
                    }
                    else if (SelectedShapeHandle == 0) // TL
                    {
                        newW = Math.Max(20, DragStartShapeSize.Width - dx);
                        newH = Math.Max(20, DragStartShapeSize.Height - dy);
                        newX = DragStartShapePos.X + dx;
                        newY = DragStartShapePos.Y + dy;
                    }
                    else if (SelectedShapeHandle == 2) // TR
                    {
                        newW = Math.Max(20, DragStartShapeSize.Width + dx);
                        newH = Math.Max(20, DragStartShapeSize.Height - dy);
                        newY = DragStartShapePos.Y + dy;
                    }
                    else if (SelectedShapeHandle == 6) // BL
                    {
                        newW = Math.Max(20, DragStartShapeSize.Width - dx);
                        newH = Math.Max(20, DragStartShapeSize.Height + dy);
                        newX = DragStartShapePos.X + dx;
                    }
                    else if (SelectedShapeHandle == 1) // TC
                    {
                        newH = Math.Max(20, DragStartShapeSize.Height - dy);
                        newY = DragStartShapePos.Y + dy;
                    }
                    else if (SelectedShapeHandle == 7) // ML
                    {
                        newW = Math.Max(20, DragStartShapeSize.Width - dx);
                        newX = DragStartShapePos.X + dx;
                    }

                    SelectedShape.PageX = Math.Max(0, newX);
                    SelectedShape.PageY = Math.Max(0, newY);
                    SelectedShape.BaseWidth = newW;
                    SelectedShape.BaseHeight = newH;
                    SelectedShape.WidthDelta = 0;
                    SelectedShape.HeightDelta = 0;
                }
                return;
            }

            // Handle Text Dragging Selection inside active editing cell
            if (IsDraggingTextSelection && IsEditing && btn == MouseButtons.Left)
            {
                int cellLeft = headerW;
                for (uint c = ScrollCol; c < SelectedCol; c++) cellLeft += GetColWidth(c);
                using (Font edFont = new Font("Calibri", 11f * ZoomFactor, FontStyle.Regular))
                {
                    int charIdx = GetTextCharIndexAtX(x, cellLeft, edFont);
                    EditCursorPos = charIdx;
                    EditSelStart = Math.Min(EditSelectionAnchor, EditCursorPos);
                    EditSelLength = Math.Abs(EditCursorPos - EditSelectionAnchor);
                }
                return;
            }

            // Handle Column Resize Dragging
            if (IsResizingCol)
            {
                int newWidth = Math.Max(25, ResizeStartSize + (x - ResizeStartMouse.X));
                CustomColWidths[ResizingColIndex] = newWidth;
                return;
            }

            // Handle Row Resize Dragging
            if (IsResizingRow)
            {
                int newHeight = Math.Max(16, ResizeStartSize + (y - ResizeStartMouse.Y));
                CustomRowHeights[ResizingRowIndex] = newHeight;
                return;
            }

            // Handle Vertical Scroll Dragging
            if (IsDraggingVertScroll)
            {
                int deltaY = y - ScrollDragStartPos;
                int rowDelta = deltaY / 5;
                ScrollRow = (uint)Math.Max(1, (int)ScrollDragStartVal + rowDelta);
                return;
            }

            // Handle Horizontal Scroll Dragging
            if (IsDraggingHorzScroll)
            {
                int deltaX = x - ScrollDragStartPos;
                int colDelta = deltaX / 8;
                ScrollCol = (uint)Math.Max(1, (int)ScrollDragStartVal + colDelta);
                return;
            }

            // Handle Table Resize Handle Dragging
            if (IsDraggingTableResize && ResizingTable != null && btn == MouseButtons.Left)
            {
                uint curC = GetColAtX(x, headerW);
                uint curR = GetRowAtY(y, startY + headerH);
                int deltaCol = (int)curC - (int)ResizingTable.EndCol;
                int deltaRow = (int)curR - (int)ResizingTable.EndRow;

                // Nắm góc bảng: 1 là kéo ngang, 2 là kéo dọc, không vừa ngang vừa dọc
                if (deltaCol > 0 && deltaCol > deltaRow)
                {
                    // Kéo ngang: chỉ mở rộng thêm cột, giữ nguyên dòng cuối
                    TableResizeTargetCol = curC;
                    TableResizeTargetRow = ResizingTable.EndRow;
                }
                else if (deltaRow > 0 && deltaRow >= deltaCol)
                {
                    // Kéo dọc: chỉ mở rộng thêm dòng, giữ nguyên cột cuối
                    TableResizeTargetRow = curR;
                    TableResizeTargetCol = ResizingTable.EndCol;
                }
                else
                {
                    TableResizeTargetRow = ResizingTable.EndRow;
                    TableResizeTargetCol = ResizingTable.EndCol;
                }
                return;
            }

            // Handle Full Column Selection Dragging
            if (IsSelectingFullCols && btn == MouseButtons.Left)
            {
                SelEndCol = GetColAtX(x, headerW);
                return;
            }

            // Handle Full Row Selection Dragging
            if (IsSelectingFullRows && btn == MouseButtons.Left)
            {
                SelEndRow = GetRowAtY(y, startY + headerH);
                return;
            }

            // Handle Fill Handle Dragging
            if (IsDraggingFillHandle && btn == MouseButtons.Left)
            {
                uint curC = GetColAtX(x, headerW);
                uint curR = GetRowAtY(y, startY + headerH);
                int deltaCol = (int)curC - (int)FillEndCol;
                int deltaRow = (int)curR - (int)FillEndRow;

                // Nắm góc ô: 1 là kéo ngang, 2 là kéo dọc
                if (deltaCol > 0 && deltaCol > deltaRow)
                {
                    FillDragTargetCol = curC;
                    FillDragTargetRow = FillEndRow;
                }
                else if (deltaRow > 0 && deltaRow >= deltaCol)
                {
                    FillDragTargetRow = curR;
                    FillDragTargetCol = FillEndCol;
                }
                else
                {
                    FillDragTargetRow = FillEndRow;
                    FillDragTargetCol = FillEndCol;
                }
                return;
            }

            // Handle Marquee Range Selection Dragging (Chỉ quét khối chọn ô, không tự tạo hay mở rộng bảng)
            if (IsSelectingRange && btn == MouseButtons.Left)
            {
                SelEndCol = GetColAtX(x, headerW);
                SelEndRow = GetRowAtY(y, startY + headerH);
            }
        }

        public void HandleMouseUp(int x, int y, int width, int height, MouseButtons btn)
        {
            if (IsDraggingTextSelection) { IsDraggingTextSelection = false; }
            if (IsResizingCol) { IsResizingCol = false; }
            if (IsResizingRow) { IsResizingRow = false; }
            if (IsDraggingVertScroll) { IsDraggingVertScroll = false; }
            if (IsDraggingHorzScroll) { IsDraggingHorzScroll = false; }
            if (IsSelectingRange) { IsSelectingRange = false; }
            if (IsSelectingFullCols) { IsSelectingFullCols = false; }
            if (IsSelectingFullRows) { IsSelectingFullRows = false; }

            // Thả chuột khi kéo góc bảng: lúc này mới thực hiện mở rộng bảng
            if (IsDraggingTableResize && ResizingTable != null)
            {
                if (TableResizeTargetCol > ResizingTable.EndCol || TableResizeTargetRow > ResizingTable.EndRow)
                {
                    AutoExpandTable(ResizingTable, TableResizeTargetRow, TableResizeTargetCol);
                }
                IsDraggingTableResize = false;
                ResizingTable = null;
            }

            // Thả chuột khi kéo góc ô: lúc này mới thực hiện tự động điền / sao chép
            if (IsDraggingFillHandle)
            {
                IsDraggingFillHandle = false;
                ExecuteAutoFill();
            }

            SelectedShapeHandle = -1;
        }

        public void HandleDoubleClick(int x, int y, int width, int height)
        {
            int tabY = height - 56;
            if (y >= tabY && y <= tabY + 28)
            {
                int tabX = 46;
                for (int s = 0; s < SheetNames.Count; s++)
                {
                    using (Font f = new Font("Segoe UI", 9f))
                    {
                        SizeF sz = SizeF.Empty;
                        using (Bitmap b = new Bitmap(1, 1))
                        using (Graphics g = Graphics.FromImage(b)) sz = g.MeasureString(SheetNames[s], f);
                        int tabW = (int)sz.Width + 24;
                        if (x >= tabX && x <= tabX + tabW)
                        {
                            SwitchToSheet(s);
                            PromptRenameSheet(s, null);
                            return;
                        }
                        tabX += tabW + 2;
                    }
                }
            }

            int fBarY = 155;
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = ShowFormulaBar ? (fBarY + 30) : fBarY;

            int dummyHandle;
            DocShapeObject hitSh = HitTestShape(x, y, headerW, startY, headerH, out dummyHandle);
            if (hitSh != null)
            {
                SelectedShape = hitSh;
                if (hitSh.ShapeType == "SmartArt" || !string.IsNullOrEmpty(hitSh.SmartArtType))
                {
                    OpenSmartArtEditor(hitSh);
                    return;
                }
                if (hitSh.ShapeType == "Chart" || hitSh.ChartData != null)
                {
                    DocChartData cd = hitSh.ChartData ?? DocChartData.CreateDefault(hitSh.TextContent);
                    hitSh.ChartData = cd;

                    Rectangle sb = GetShapeScreenBounds(hitSh, headerW, startY, headerH);
                    int titleH = Math.Min(38, sb.Height / 3);
                    Rectangle titleZone = new Rectangle(sb.X, sb.Y, sb.Width, titleH);

                    if (titleZone.Contains(x, y))
                    {
                        // Double clicked on Chart Title -> Open Rename Form!
                        Point scrPt = new Point(sb.X + sb.Width / 4, sb.Y + 10);
                        ChartTitleRenameForm renameDlg = new ChartTitleRenameForm(cd, scrPt);
                        renameDlg.OnTitleChanged = (newTitle, showTitle) =>
                        {
                            cd.Title = newTitle;
                            cd.ShowTitle = showTitle;
                            hitSh.TextContent = newTitle;
                        };
                        renameDlg.ShowDialog();
                    }
                    else
                    {
                        OpenChartEditor(hitSh);
                    }
                    return;
                }
                if (hitSh.ShapeType != "Image")
                {
                    hitSh.IsEditingText = true;
                    hitSh.IsSelected = true;
                    if (hitSh.TextContent != null) hitSh.TextCursorIndex = hitSh.TextContent.Length;
                    IsEditing = false;
                }
                return;
            }

            if (x > headerW && y > startY + headerH && y < height - 56)
            {
                if (IsSheetProtected)
                {
                    ShowSheetProtectedWarning();
                    return;
                }

                uint cIdx = GetColAtX(x, headerW);
                uint rIdx = GetRowAtY(y, startY + headerH);

                SelectedCol = cIdx;
                SelectedRow = rIdx;
                SelEndCol = cIdx;
                SelEndRow = rIdx;

                CellData cell = CurrentSheet.GetCell(rIdx, cIdx);
                EditText = cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "";
                IsEditing = true;
                EditCursorPos = EditText.Length;
                EditSelectionAnchor = 0;
                EditSelStart = 0;
                EditSelLength = EditText.Length;
            }
        }

        public void OpenChartEditor(DocShapeObject shape)
        {
            if (shape == null) return;
            DocChartData cd = shape.ChartData ?? DocChartData.CreateDefault(shape.TextContent);
            shape.ChartData = cd;

            ChartDataExcelForm frm = new ChartDataExcelForm(cd);
            frm.OnDataChanged = (newData) =>
            {
                shape.ChartData = newData;
            };
            frm.Show();
        }

        public void ShowChartContextMenu(int x, int y, DocShapeObject shape, Control parentControl)
        {
            if (shape == null) return;
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Font = new Font("Segoe UI", 9f);

            cms.Items.Add("📊 Chỉnh sửa số liệu biểu đồ (Edit Data in Sheet)...", null, (s, e) =>
            {
                OpenChartEditor(shape);
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Items.Add("🔄 Thay đổi loại biểu đồ (Change Chart Type)...", null, (s, e) =>
            {
                DocChartData cd = shape.ChartData ?? DocChartData.CreateDefault(shape.TextContent);
                using (InsertChartForm dlg = new InsertChartForm(cd))
                {
                    if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedChartType))
                    {
                        cd.ChartType = dlg.SelectedChartType;
                        shape.ChartData = cd;
                        shape.TextContent = cd.ChartType;
                        if (parentControl != null) parentControl.Invalidate();
                    }
                }
            });

            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("🗑️ Xóa biểu đồ", null, (s, e) =>
            {
                Shapes.Remove(shape);
                if (SelectedShape == shape) SelectedShape = null;
                if (parentControl != null) parentControl.Invalidate();
            });

            Point pt = (parentControl != null) ? parentControl.PointToScreen(new Point(x, y)) : new Point(x, y);
            cms.Show(pt);
        }

        public void OpenSmartArtEditor(DocShapeObject shape)
        {
            if (shape == null) return;
            using (SmartArtEditorDialog dlg = new SmartArtEditorDialog(shape.SmartArtType, shape.TextContent, shape.StylePresetIndex, (newText, newColor) =>
            {
                shape.TextContent = newText;
                shape.StylePresetIndex = newColor;
            }))
            {
                dlg.ShowDialog();
            }
        }

        public void ShowSmartArtContextMenu(int x, int y, DocShapeObject shape, Control parentControl)
        {
            if (shape == null) return;
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Font = new Font("Segoe UI", 9f);

            cms.Items.Add("✏️ Chỉnh sửa nội dung chữ SmartArt...", null, (s, e) =>
            {
                OpenSmartArtEditor(shape);
                if (parentControl != null) parentControl.Invalidate();
            });

            ToolStripMenuItem colorMenu = new ToolStripMenuItem("🎨 Đổi màu sắc SmartArt (Color Palette)");
            colorMenu.DropDownItems.Add("🟠 Cam & Vàng (Office Warm)", null, (s, e) => { shape.StylePresetIndex = 0; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add("🔵 Xanh Lam Chuẩn (Office Blue)", null, (s, e) => { shape.StylePresetIndex = 1; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add("🌈 Đa sắc màu (Colorful Accent)", null, (s, e) => { shape.StylePresetIndex = 2; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add("🟢 Xanh Lục (Emerald Green)", null, (s, e) => { shape.StylePresetIndex = 3; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add("🟣 Tím & Hồng (Purple & Magenta)", null, (s, e) => { shape.StylePresetIndex = 4; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add("⚫ Đơn sắc (Monochrome Gray)", null, (s, e) => { shape.StylePresetIndex = 5; if (parentControl != null) parentControl.Invalidate(); });
            cms.Items.Add(colorMenu);

            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("🗑️ Xóa sơ đồ SmartArt", null, (s, e) =>
            {
                Shapes.Remove(shape);
                if (SelectedShape == shape) SelectedShape = null;
                if (parentControl != null) parentControl.Invalidate();
            });

            Point pt = (parentControl != null) ? parentControl.PointToScreen(new Point(x, y)) : new Point(x, y);
            cms.Show(pt);
        }

        public void HandleFormulaBarButtonClick(int x, int y)
        {
            int fBarY = 155;
            int btnStartX = 8 + 75 + 12;

            if (y >= fBarY && y <= fBarY + 28)
            {
                if (x >= btnStartX && x <= btnStartX + 26) { CancelEdit(); return; }
                if (x >= btnStartX + 28 && x <= btnStartX + 54) { CommitEditAndMove(0, 0); return; }
            }
        }

        public void HandleClick(int x, int y, int height)
        {
            HandleFormulaBarButtonClick(x, y);
        }

        public void HandleRightClick(int x, int y, Control parentControl, RibbonBar ribbon)
        {
            if (parentControl != null)
            {
                int tabY = parentControl.ClientSize.Height - 56;
                if (y >= tabY && y <= tabY + 28)
                {
                    int tabX = 46;
                    for (int s = 0; s < SheetNames.Count; s++)
                    {
                        using (Font f = new Font("Segoe UI", 9f))
                        {
                            SizeF sz = SizeF.Empty;
                            using (Bitmap b = new Bitmap(1, 1))
                            using (Graphics g = Graphics.FromImage(b)) sz = g.MeasureString(SheetNames[s], f);
                            int tabW = (int)sz.Width + 24;
                            if (x >= tabX && x <= tabX + tabW)
                            {
                                SwitchToSheet(s);
                                ShowSheetTabContextMenu(x, y, s, parentControl);
                                return;
                            }
                            tabX += tabW + 2;
                        }
                    }
                }
            }

            int fBarY = 155;
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = ShowFormulaBar ? (fBarY + 30) : fBarY;

            // Check Shape Right Click
            int dummyHandle;
            DocShapeObject hitSh = HitTestShape(x, y, headerW, startY, headerH, out dummyHandle);
            if (hitSh != null)
            {
                SelectedShape = hitSh;
                if (hitSh.ShapeType == "SmartArt" || !string.IsNullOrEmpty(hitSh.SmartArtType))
                {
                    ShowSmartArtContextMenu(x, y, hitSh, parentControl);
                    return;
                }
                if (hitSh.ShapeType == "Chart" || hitSh.ChartData != null)
                {
                    ShowChartContextMenu(x, y, hitSh, parentControl);
                    return;
                }
            }

            if (x > headerW && y > startY + headerH && y < parentControl.ClientSize.Height - 56)
            {
                uint cIdx = GetColAtX(x, headerW);
                uint rIdx = GetRowAtY(y, startY + headerH);

                uint minR = Math.Min(SelectedRow, SelEndRow);
                uint maxR = Math.Max(SelectedRow, SelEndRow);
                uint minC = Math.Min(SelectedCol, SelEndCol);
                uint maxC = Math.Max(SelectedCol, SelEndCol);

                // If right clicked outside currently selected range, select that single cell
                if (rIdx < minR || rIdx > maxR || cIdx < minC || cIdx > maxC)
                {
                    SelectedCol = cIdx;
                    SelectedRow = rIdx;
                    SelEndCol = cIdx;
                    SelEndRow = rIdx;
                }

                if (IsEditing)
                {
                    CommitEditAndMove(0, 0);
                }

                Form parentForm = parentControl as Form;
                CalcContextMenuHelper.Show(x, y, this, ribbon, parentForm);
            }
        }

        public uint GetColAtX(int x, int headerW)
        {
            if (x < headerW)
            {
                int runX = headerW;
                uint cIdx = ScrollCol;
                while (cIdx > 1)
                {
                    int w = GetColWidth(cIdx - 1);
                    runX -= w;
                    cIdx--;
                    if (x >= runX) return cIdx;
                }
                return 1;
            }
            else
            {
                int runX = headerW;
                uint cIdx = ScrollCol;
                while (true)
                {
                    int w = GetColWidth(cIdx);
                    if (x >= runX && x < runX + w) return cIdx;
                    runX += w;
                    cIdx++;
                    if (runX > 20000 || cIdx > 250) break; // Safety limit
                }
                return Math.Min((uint)250, cIdx);
            }
        }

        public uint GetRowAtY(int y, int headerTop)
        {
            if (y < headerTop)
            {
                int runY = headerTop;
                uint rIdx = ScrollRow;
                while (rIdx > 1)
                {
                    int h = GetRowHeight(rIdx - 1);
                    runY -= h;
                    rIdx--;
                    if (y >= runY) return rIdx;
                }
                return 1;
            }
            else
            {
                int runY = headerTop;
                uint rIdx = ScrollRow;
                while (true)
                {
                    int h = GetRowHeight(rIdx);
                    if (y >= runY && y < runY + h) return rIdx;
                    runY += h;
                    rIdx++;
                    if (runY > 20000 || rIdx > 1000) break; // Safety limit
                }
                return Math.Min((uint)1000, rIdx);
            }
        }

        // ==================== AutoFill Engine ====================

        public void ExecuteAutoFill()
        {
            SaveUndoState();
            uint srcMinR = FillStartRow;
            uint srcMaxR = FillEndRow;
            uint srcMinC = FillStartCol;
            uint srcMaxC = FillEndCol;

            uint tgtR = FillDragTargetRow;
            uint tgtC = FillDragTargetCol;

            // Fill Downwards
            if (tgtR > srcMaxR)
            {
                for (uint c = srcMinC; c <= srcMaxC; c++)
                {
                    List<CellData> srcList = new List<CellData>();
                    for (uint r = srcMinR; r <= srcMaxR; r++) srcList.Add(CurrentSheet.GetCell(r, c));

                    for (uint r = srcMaxR + 1; r <= tgtR; r++)
                    {
                        int srcOffset = (int)((r - srcMaxR - 1) % srcList.Count);
                        CellData src = srcList[srcOffset];
                        CellData tgt = CurrentSheet.GetOrCreateCell(r, c);

                        if (src != null)
                        {
                            tgt.Format = src.Format.Clone();
                            if (!string.IsNullOrEmpty(src.FormulaStr))
                            {
                                int rowDelta = (int)(r - (srcMinR + srcOffset));
                                tgt.FormulaStr = ShiftFormulaReferences(src.FormulaStr, rowDelta, 0);
                                tgt.Type = CellType.Formula;
                            }
                            else if (src.Type == CellType.Number)
                            {
                                double step = (srcList.Count > 1 && srcList[0] != null && srcList[srcList.Count - 1] != null && srcList[0].Type == CellType.Number && srcList[srcList.Count - 1].Type == CellType.Number)
                                    ? (srcList[srcList.Count - 1].NumericValue - srcList[0].NumericValue) / (srcList.Count - 1)
                                    : 1.0;
                                double newVal = srcList[srcList.Count - 1].NumericValue + step * (r - srcMaxR);
                                tgt.NumericValue = newVal;
                                tgt.RawValue = newVal.ToString(CultureInfo.InvariantCulture);
                                tgt.Type = CellType.Number;
                            }
                            else if (!string.IsNullOrEmpty(src.RawValue))
                            {
                                tgt.RawValue = ExtrapolateStringPattern(src.RawValue, (int)(r - srcMaxR));
                                tgt.Type = CellType.String;
                            }
                        }
                    }
                }
                SelEndRow = tgtR;
            }
            // Fill Rightwards
            else if (tgtC > srcMaxC)
            {
                for (uint r = srcMinR; r <= srcMaxR; r++)
                {
                    List<CellData> srcList = new List<CellData>();
                    for (uint c = srcMinC; c <= srcMaxC; c++) srcList.Add(CurrentSheet.GetCell(r, c));

                    for (uint c = srcMaxC + 1; c <= tgtC; c++)
                    {
                        int srcOffset = (int)((c - srcMaxC - 1) % srcList.Count);
                        CellData src = srcList[srcOffset];
                        CellData tgt = CurrentSheet.GetOrCreateCell(r, c);

                        if (src != null)
                        {
                            tgt.Format = src.Format.Clone();
                            if (!string.IsNullOrEmpty(src.FormulaStr))
                            {
                                int colDelta = (int)(c - (srcMinC + srcOffset));
                                tgt.FormulaStr = ShiftFormulaReferences(src.FormulaStr, 0, colDelta);
                                tgt.Type = CellType.Formula;
                            }
                            else if (src.Type == CellType.Number)
                            {
                                double step = (srcList.Count > 1 && srcList[0] != null && srcList[srcList.Count - 1] != null && srcList[0].Type == CellType.Number && srcList[srcList.Count - 1].Type == CellType.Number)
                                    ? (srcList[srcList.Count - 1].NumericValue - srcList[0].NumericValue) / (srcList.Count - 1)
                                    : 1.0;
                                double newVal = srcList[srcList.Count - 1].NumericValue + step * (c - srcMaxC);
                                tgt.NumericValue = newVal;
                                tgt.RawValue = newVal.ToString(CultureInfo.InvariantCulture);
                                tgt.Type = CellType.Number;
                            }
                            else if (!string.IsNullOrEmpty(src.RawValue))
                            {
                                tgt.RawValue = ExtrapolateStringPattern(src.RawValue, (int)(c - srcMaxC));
                                tgt.Type = CellType.String;
                            }
                        }
                    }
                }
                SelEndCol = tgtC;
            }

            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        private string ShiftFormulaReferences(string formula, int rowDelta, int colDelta)
        {
            if (string.IsNullOrEmpty(formula)) return "";
            return Regex.Replace(formula, @"([A-Za-z]+)([0-9]+)", new MatchEvaluator(match =>
            {
                string colStr = match.Groups[1].Value.ToUpper();
                string rowStr = match.Groups[2].Value;

                uint colIdx = SparseMatrix.ColNameToIndex(colStr);
                uint rowIdx = 1;
                uint.TryParse(rowStr, out rowIdx);
                if (rowIdx == 0) rowIdx = 1;

                uint newCol = (uint)Math.Max(1, (int)colIdx + colDelta);
                uint newRow = (uint)Math.Max(1, (int)rowIdx + rowDelta);

                return SparseMatrix.ColIndexToName(newCol) + newRow;
            }));
        }

        private string ExtrapolateStringPattern(string text, int step)
        {
            Match m = Regex.Match(text, @"^(.*?)(\d+)$");
            if (m.Success)
            {
                string prefix = m.Groups[1].Value;
                string numStr = m.Groups[2].Value;
                long val = long.Parse(numStr) + step;
                return prefix + val.ToString().PadLeft(numStr.Length, '0');
            }
            return text;
        }

        // ==================== Keyboard Interaction ====================

        public void HandleChar(char ch)
        {
            if (IsSheetProtected && ch != '\r' && ch != '\t')
            {
                ShowSheetProtectedWarning();
                return;
            }
            if (ch == '\r')
            {
                CommitEditAndMove(1, 0);
            }
            else if (ch == '\t')
            {
                if (IsEditing && EditText.StartsWith("=") && ActiveSuggestions.Count > 0)
                {
                    EditText = "=" + ActiveSuggestions[SelectedSuggestionIndex].Name + "(";
                    EditCursorPos = EditText.Length;
                    EditSelStart = 0;
                    EditSelLength = 0;
                }
                else
                {
                    CommitEditAndMove(0, 1);
                }
            }
            else if (ch == '\b')
            {
                if (!IsEditing)
                {
                    CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                    EditText = cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "";
                    IsEditing = true;
                    EditCursorPos = EditText.Length;
                    EditSelStart = 0;
                    EditSelLength = 0;
                }
                if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                {
                    EditText = EditText.Remove(EditSelStart, EditSelLength);
                    EditCursorPos = EditSelStart;
                    EditSelLength = 0;
                }
                else if (EditText.Length > 0 && EditCursorPos > 0)
                {
                    EditText = EditText.Remove(EditCursorPos - 1, 1);
                    EditCursorPos--;
                }
            }
            else if (ch >= 32)
            {
                if (!IsEditing)
                {
                    EditText = "";
                    IsEditing = true;
                    EditCursorPos = 0;
                    EditSelStart = 0;
                    EditSelLength = 0;
                }
                if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                {
                    EditText = EditText.Remove(EditSelStart, EditSelLength);
                    EditCursorPos = EditSelStart;
                    EditSelLength = 0;
                }
                EditText = EditText.Insert(EditCursorPos, ch.ToString());
                EditCursorPos++;
            }
        }

        public void CommitEditAndMove(int deltaRow, int deltaCol)
        {
            if (IsEditing)
            {
                SaveUndoState();
                CurrentSheet.SetCellText(SelectedRow, SelectedCol, EditText);
                FormulaEngine.EvaluateSheet(CurrentSheet);
                IsEditing = false;
                EditSelLength = 0;
            }
            SelectedRow = (uint)Math.Max(1, (int)SelectedRow + deltaRow);
            SelectedCol = (uint)Math.Max(1, (int)SelectedCol + deltaCol);
            SelEndRow = SelectedRow;
            SelEndCol = SelectedCol;
        }

        public void CancelEdit()
        {
            IsEditing = false;
            EditText = "";
            EditSelLength = 0;
        }

        public void HandleKeyDown(Keys key)
        {
            Keys rawKey = key & Keys.KeyCode;
            bool shift = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) || ((key & Keys.Shift) == Keys.Shift);
            bool ctrl = ((Control.ModifierKeys & Keys.Control) == Keys.Control) || ((key & Keys.Control) == Keys.Control);
            bool alt = ((Control.ModifierKeys & Keys.Alt) == Keys.Alt) || ((key & Keys.Alt) == Keys.Alt);

            // Shift + F3: Insert Function Dialog (fx)
            if (shift && rawKey == Keys.F3)
            {
                using (InsertFunctionDialog ifd = new InsertFunctionDialog(this))
                {
                    ifd.ShowDialog();
                }
                return;
            }

            // Alt + = : AutoSum (Σ)
            if (alt && (rawKey == Keys.Oemplus || rawKey == Keys.Add || rawKey == (Keys)187))
            {
                AutoSum("SUM");
                return;
            }

            // F9: Calculate Now
            if (rawKey == Keys.F9)
            {
                CalculateNow(true);
                return;
            }

            if (ctrl && rawKey == Keys.Z)
            {
                Undo();
                return;
            }
            if (ctrl && rawKey == Keys.Y)
            {
                Redo();
                return;
            }

            if (IsEditing)
            {
                if (ctrl && rawKey == Keys.A)
                {
                    EditSelStart = 0;
                    EditSelLength = EditText.Length;
                    EditCursorPos = EditText.Length;
                    return;
                }
                if (ctrl && rawKey == Keys.C)
                {
                    if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                    {
                        Clipboard.SetText(EditText.Substring(EditSelStart, EditSelLength));
                    }
                    return;
                }
                if (ctrl && rawKey == Keys.X)
                {
                    if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                    {
                        Clipboard.SetText(EditText.Substring(EditSelStart, EditSelLength));
                        EditText = EditText.Remove(EditSelStart, EditSelLength);
                        EditCursorPos = EditSelStart;
                        EditSelLength = 0;
                    }
                    return;
                }
                if (ctrl && rawKey == Keys.V)
                {
                    string clip = Clipboard.GetText() ?? "";
                    if (!string.IsNullOrEmpty(clip))
                    {
                        if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                        {
                            EditText = EditText.Remove(EditSelStart, EditSelLength);
                            EditCursorPos = EditSelStart;
                            EditSelLength = 0;
                        }
                        EditText = EditText.Insert(EditCursorPos, clip);
                        EditCursorPos += clip.Length;
                    }
                    return;
                }
                if (rawKey == Keys.Left)
                {
                    if (shift)
                    {
                        if (EditSelLength == 0) EditSelectionAnchor = EditCursorPos;
                        if (EditCursorPos > 0) EditCursorPos--;
                        EditSelStart = Math.Min(EditSelectionAnchor, EditCursorPos);
                        EditSelLength = Math.Abs(EditCursorPos - EditSelectionAnchor);
                    }
                    else
                    {
                        if (EditSelLength > 0) EditCursorPos = EditSelStart;
                        else if (EditCursorPos > 0) EditCursorPos--;
                        EditSelLength = 0;
                        EditSelectionAnchor = EditCursorPos;
                    }
                    return;
                }
                if (rawKey == Keys.Right)
                {
                    if (shift)
                    {
                        if (EditSelLength == 0) EditSelectionAnchor = EditCursorPos;
                        if (EditCursorPos < EditText.Length) EditCursorPos++;
                        EditSelStart = Math.Min(EditSelectionAnchor, EditCursorPos);
                        EditSelLength = Math.Abs(EditCursorPos - EditSelectionAnchor);
                    }
                    else
                    {
                        if (EditSelLength > 0) EditCursorPos = EditSelStart + EditSelLength;
                        else if (EditCursorPos < EditText.Length) EditCursorPos++;
                        EditSelLength = 0;
                        EditSelectionAnchor = EditCursorPos;
                    }
                    return;
                }
                if (rawKey == Keys.Home)
                {
                    if (shift)
                    {
                        if (EditSelLength == 0) EditSelectionAnchor = EditCursorPos;
                        EditCursorPos = 0;
                        EditSelStart = 0;
                        EditSelLength = EditSelectionAnchor;
                    }
                    else
                    {
                        EditCursorPos = 0;
                        EditSelLength = 0;
                        EditSelectionAnchor = 0;
                    }
                    return;
                }
                if (rawKey == Keys.End)
                {
                    if (shift)
                    {
                        if (EditSelLength == 0) EditSelectionAnchor = EditCursorPos;
                        EditCursorPos = EditText.Length;
                        EditSelStart = EditSelectionAnchor;
                        EditSelLength = EditText.Length - EditSelectionAnchor;
                    }
                    else
                    {
                        EditCursorPos = EditText.Length;
                        EditSelLength = 0;
                        EditSelectionAnchor = EditCursorPos;
                    }
                    return;
                }
                if (rawKey == Keys.Back)
                {
                    if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                    {
                        EditText = EditText.Remove(EditSelStart, EditSelLength);
                        EditCursorPos = EditSelStart;
                        EditSelLength = 0;
                    }
                    else if (EditText.Length > 0 && EditCursorPos > 0)
                    {
                        EditText = EditText.Remove(EditCursorPos - 1, 1);
                        EditCursorPos--;
                    }
                    return;
                }
                if (rawKey == Keys.Delete)
                {
                    if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelLength <= EditText.Length)
                    {
                        EditText = EditText.Remove(EditSelStart, EditSelLength);
                        EditCursorPos = EditSelStart;
                        EditSelLength = 0;
                    }
                    else if (EditCursorPos < EditText.Length)
                    {
                        EditText = EditText.Remove(EditCursorPos, 1);
                    }
                    return;
                }
                if (rawKey == Keys.Escape)
                {
                    CancelEdit();
                    return;
                }
                if (rawKey == Keys.Enter)
                {
                    CommitEditAndMove(1, 0);
                    return;
                }
                if (rawKey == Keys.Up && ActiveSuggestions.Count > 0)
                {
                    if (SelectedSuggestionIndex > 0) SelectedSuggestionIndex--;
                    return;
                }
                if (rawKey == Keys.Down && ActiveSuggestions.Count > 0)
                {
                    if (SelectedSuggestionIndex < ActiveSuggestions.Count - 1) SelectedSuggestionIndex++;
                    return;
                }
            }
            else
            {
                if (rawKey == Keys.F2)
                {
                    if (IsSheetProtected)
                    {
                        ShowSheetProtectedWarning();
                        return;
                    }
                    CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                    EditText = cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "";
                    IsEditing = true;
                    EditCursorPos = EditText.Length;
                    EditSelectionAnchor = 0;
                    EditSelStart = 0;
                    EditSelLength = EditText.Length;
                }
                else if (rawKey == Keys.Delete)
                {
                    if (SelectedShape != null)
                    {
                        Shapes.Remove(SelectedShape);
                        SelectedShape = null;
                        SelectedShapeHandle = -1;
                        return;
                    }
                    if (IsSheetProtected)
                    {
                        ShowSheetProtectedWarning();
                        return;
                    }
                    ClearSelectedCells();
                    FormulaEngine.EvaluateSheet(CurrentSheet);
                }
                else if (rawKey == Keys.Up && SelectedRow > 1) { CommitEditAndMove(-1, 0); }
                else if (rawKey == Keys.Down) { CommitEditAndMove(1, 0); }
                else if (rawKey == Keys.Left && SelectedCol > 1) { CommitEditAndMove(0, -1); }
                else if (rawKey == Keys.Right) { CommitEditAndMove(0, 1); }
            }
        }

        private void DrawBorderSegment(Graphics g, int x1, int y1, int x2, int y2, int thick, Color col, bool isHorz)
        {
            if (thick == 4) // Double line (Viền kép)
            {
                using (Pen bp = new Pen(col, 1f))
                {
                    if (isHorz)
                    {
                        g.DrawLine(bp, x1, y1 - 2, x2, y2 - 2);
                        g.DrawLine(bp, x1, y1, x2, y2);
                    }
                    else
                    {
                        g.DrawLine(bp, x1 - 2, y1, x2 - 2, y2);
                        g.DrawLine(bp, x1, y1, x2, y2);
                    }
                }
            }
            else
            {
                float penWidth = 1.0f;
                if (thick == 2) penWidth = 1.8f;
                else if (thick == 3) penWidth = 2.8f;
                using (Pen bp = new Pen(col, penWidth))
                {
                    g.DrawLine(bp, x1, y1, x2, y2);
                }
            }
        }

        // ==================== Formatting & Inspection Actions ====================

        public CalcSelectionFormat GetSelectionFormat()
        {
            CalcSelectionFormat res = new CalcSelectionFormat();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            bool first = true;
            bool mixedFont = false;
            bool mixedSize = false;
            bool allBold = true;
            bool allItalic = true;
            bool allUnderline = true;
            bool mixedAlign = false;
            bool mixedVAlign = false;
            bool allWrap = true;
            bool mixedNumFmt = false;

            string commonFont = null;
            float commonSize = -1;
            int commonAlign = -1;
            int commonVAlign = -1;
            string commonNumFmt = null;
            int commonDec = -1;
            int commonTxtColor = 0;
            int commonBgColor = unchecked((int)0xFFFFFFFF);

            int totalCells = (int)((maxR - minR + 1) * (maxC - minC + 1));
            bool isSingleCell = (totalCells == 1);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetCell(r, c);
                    CellFormat fmt = (cell != null && cell.Format != null) ? cell.Format : new CellFormat();

                    string fName = !string.IsNullOrEmpty(fmt.FontName) ? fmt.FontName : "Calibri";
                    float fSize = fmt.FontSize > 0 ? fmt.FontSize : 11.0f;
                    int align = fmt.Align;
                    int vAlign = fmt.VAlign;
                    string numFmt = !string.IsNullOrEmpty(fmt.NumberFormat) ? fmt.NumberFormat : "General";

                    if (first)
                    {
                        commonFont = fName;
                        commonSize = fSize;
                        commonAlign = align;
                        commonVAlign = vAlign;
                        commonNumFmt = numFmt;
                        commonDec = fmt.DecimalPlaces;
                        commonTxtColor = fmt.TextColor;
                        commonBgColor = fmt.BgColor;
                        first = false;
                    }
                    else
                    {
                        if (!string.Equals(commonFont, fName, StringComparison.OrdinalIgnoreCase)) mixedFont = true;
                        if (Math.Abs(commonSize - fSize) > 0.01f) mixedSize = true;
                        if (commonAlign != align) mixedAlign = true;
                        if (commonVAlign != vAlign) mixedVAlign = true;
                        if (!string.Equals(commonNumFmt, numFmt, StringComparison.OrdinalIgnoreCase)) mixedNumFmt = true;
                    }

                    if (!fmt.Bold) allBold = false;
                    if (!fmt.Italic) allItalic = false;
                    if (!fmt.Underline) allUnderline = false;
                    if (!fmt.WrapText) allWrap = false;
                }
            }

            res.FontName = mixedFont ? "" : (commonFont ?? "Calibri");
            res.FontSize = mixedSize ? 0f : (commonSize > 0 ? commonSize : 11.0f);
            res.Bold = allBold;
            res.Italic = allItalic;
            res.Underline = allUnderline;
            res.Align = mixedAlign ? -1 : commonAlign;
            res.VAlign = mixedVAlign ? -1 : commonVAlign;
            res.WrapText = allWrap;
            res.NumberFormat = mixedNumFmt ? "" : (commonNumFmt ?? "General");
            res.DecimalPlaces = commonDec >= 0 ? commonDec : 2;
            res.TextColor = commonTxtColor;
            res.BgColor = commonBgColor;

            // Merge status check
            CellData firstCell = CurrentSheet.GetCell(minR, minC);
            if (firstCell != null && firstCell.Format != null && firstCell.Format.IsMergedParent)
            {
                if (minR + firstCell.Format.MergeRowSpan - 1 == maxR && minC + firstCell.Format.MergeColSpan - 1 == maxC)
                {
                    res.IsMerged = true;
                }
                else if (isSingleCell)
                {
                    res.IsMerged = true;
                }
            }
            else if (firstCell != null && firstCell.Format != null && firstCell.Format.IsMergedChild)
            {
                res.IsMerged = true;
            }

            return res;
        }

        public void ApplyFontName(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return;
            SaveUndoState();
            ForEachSelectedCell(cell => cell.Format.FontName = fontName);
        }

        public void ApplyFontSize(float size)
        {
            if (size <= 0) return;
            SaveUndoState();
            ForEachSelectedCell(cell => cell.Format.FontSize = size);
        }

        public void IncreaseFontSize()
        {
            SaveUndoState();
            float[] stdSizes = new float[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            CalcSelectionFormat summary = GetSelectionFormat();
            float cur = summary.FontSize > 0 ? summary.FontSize : 11.0f;
            float next = 72f;
            for (int i = 0; i < stdSizes.Length; i++)
            {
                if (stdSizes[i] > cur + 0.1f)
                {
                    next = stdSizes[i];
                    break;
                }
            }
            ForEachSelectedCell(cell => cell.Format.FontSize = next);
        }

        public void DecreaseFontSize()
        {
            SaveUndoState();
            float[] stdSizes = new float[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            CalcSelectionFormat summary = GetSelectionFormat();
            float cur = summary.FontSize > 0 ? summary.FontSize : 11.0f;
            float prev = 8f;
            for (int i = stdSizes.Length - 1; i >= 0; i--)
            {
                if (stdSizes[i] < cur - 0.1f)
                {
                    prev = stdSizes[i];
                    break;
                }
            }
            ForEachSelectedCell(cell => cell.Format.FontSize = prev);
        }

        public void ToggleBold()
        {
            SaveUndoState();
            CalcSelectionFormat summary = GetSelectionFormat();
            bool target = !summary.Bold;
            ForEachSelectedCell(cell => cell.Format.Bold = target);
        }

        public void ToggleItalic()
        {
            SaveUndoState();
            CalcSelectionFormat summary = GetSelectionFormat();
            bool target = !summary.Italic;
            ForEachSelectedCell(cell => cell.Format.Italic = target);
        }

        public void ToggleUnderline()
        {
            SaveUndoState();
            CalcSelectionFormat summary = GetSelectionFormat();
            bool target = !summary.Underline;
            ForEachSelectedCell(cell => cell.Format.Underline = target);
        }

        public void SetSelectedCellBgColor(Color col)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => cell.Format.BgColor = col.ToArgb());
        }

        public void SetSelectedCellTextColor(Color col)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => cell.Format.TextColor = col.ToArgb());
        }

        public void SetSelectedCellAlign(int align, int vAlign = -1)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                if (align >= 0) cell.Format.Align = align;
                if (vAlign >= 0) cell.Format.VAlign = vAlign;
            });
        }

        public void ToggleWrapText()
        {
            SaveUndoState();
            CalcSelectionFormat summary = GetSelectionFormat();
            bool target = !summary.WrapText;
            ForEachSelectedCell(cell => cell.Format.WrapText = target);
        }

        public void MergeAndCenter()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            CalcSelectionFormat summary = GetSelectionFormat();
            if (summary.IsMerged)
            {
                // Unmerge
                for (uint r = minR; r <= maxR; r++)
                {
                    for (uint c = minC; c <= maxC; c++)
                    {
                        CellData cell = CurrentSheet.GetCell(r, c);
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
                return;
            }

            if (minR == maxR && minC == maxC) return; // Cannot merge 1 cell

            CellData master = CurrentSheet.GetOrCreateCell(minR, minC);
            master.Format.IsMergedParent = true;
            master.Format.MergeRowSpan = (int)(maxR - minR + 1);
            master.Format.MergeColSpan = (int)(maxC - minC + 1);
            master.Format.Align = 1; // Center

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    if (r == minR && c == minC) continue;
                    CellData child = CurrentSheet.GetOrCreateCell(r, c);
                    child.Format.IsMergedChild = true;
                    child.Format.MergedParentRow = minR;
                    child.Format.MergedParentCol = minC;
                }
            }
        }

        public void MergeAcross()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            if (minC == maxC) return;

            for (uint r = minR; r <= maxR; r++)
            {
                CurrentSheet.MergeCells(r, minC, r, maxC);
            }
        }

        public void MergeCells()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            if (minR == maxR && minC == maxC) return;

            CurrentSheet.MergeCells(minR, minC, maxR, maxC);
        }

        public void UnmergeCells()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            CurrentSheet.UnmergeCells(minR, minC, maxR, maxC);
        }

        public void ApplyDiagonalBorder(bool diagDown, bool diagUp, int thickness = 1, int colorArgb = 0)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                cell.Format.Borders.DiagonalDown = diagDown;
                cell.Format.Borders.DiagonalUp = diagUp;
                cell.Format.Borders.DiagonalThickness = thickness;
                cell.Format.Borders.DiagonalColor = colorArgb;
            });
        }

        public void OpenDiagonalHeaderDialog(Form parentForm)
        {
            CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
            using (DiagonalHeaderDialog dlg = new DiagonalHeaderDialog(cell))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    SaveUndoState();
                    if (dlg.IsClearRequested)
                    {
                        ForEachSelectedCell(c => {
                            c.Format.Borders.DiagonalDown = false;
                            c.Format.Borders.DiagonalUp = false;
                            c.Format.IsDiagonalHeader = false;
                            c.Format.DiagonalTopRightText = "";
                            c.Format.DiagonalBottomLeftText = "";
                        });
                    }
                    else
                    {
                        CellData target = CurrentSheet.GetOrCreateCell(SelectedRow, SelectedCol);
                        target.Format.Borders.DiagonalDown = dlg.IsDiagonalDown;
                        target.Format.Borders.DiagonalUp = dlg.IsDiagonalUp;
                        target.Format.Borders.DiagonalThickness = dlg.Thickness;
                        target.Format.Borders.DiagonalColor = dlg.LineColorArgb;
                        target.Format.IsDiagonalHeader = true;
                        target.Format.DiagonalTopRightText = dlg.TopRightText;
                        target.Format.DiagonalBottomLeftText = dlg.BottomLeftText;
                        target.RawValue = dlg.TopRightText + "\n" + dlg.BottomLeftText;
                        target.EvaluatedStr = target.RawValue;
                    }
                    if (parentForm != null) parentForm.Invalidate();
                }
            }
        }

        public void SetNumberFormat(string format, int dec = 2)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                cell.Format.NumberFormat = format;
                cell.Format.DecimalPlaces = dec;
                if (cell.Type == CellType.Number || cell.Type == CellType.Formula)
                {
                    cell.EvaluatedStr = FormulaEngine.FormatCellDisplay(cell);
                }
            });
        }

        public void AdjustDecimalPlaces(int delta)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                cell.Format.DecimalPlaces = Math.Max(0, Math.Min(10, cell.Format.DecimalPlaces + delta));
                if (cell.Type == CellType.Number || cell.Type == CellType.Formula)
                {
                    cell.EvaluatedStr = FormulaEngine.FormatCellDisplay(cell);
                }
            });
        }

        public void ApplyBorderPreset(int preset)
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                    var b = cell.Format.Borders;

                    switch (preset)
                    {
                        case 0: // Bottom Border (Viền dưới)
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 1; }
                            break;
                        case 1: // Top Border (Viền trên)
                            if (r == minR) { b.Top = true; b.TopThickness = 1; }
                            break;
                        case 2: // Left Border (Viền trái)
                            if (c == minC) { b.Left = true; b.LeftThickness = 1; }
                            break;
                        case 3: // Right Border (Viền phải)
                            if (c == maxC) { b.Right = true; b.RightThickness = 1; }
                            break;
                        case 4: // No Border (Không viền / Xóa kẻ)
                            b.Top = false;
                            b.Bottom = false;
                            b.Left = false;
                            b.Right = false;
                            break;
                        case 5: // All Borders (Tất cả viền)
                            b.Top = true; b.TopThickness = 1;
                            b.Bottom = true; b.BottomThickness = 1;
                            b.Left = true; b.LeftThickness = 1;
                            b.Right = true; b.RightThickness = 1;
                            break;
                        case 6: // Outside Borders (Viền bao ngoài)
                            if (r == minR) { b.Top = true; b.TopThickness = 1; }
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 1; }
                            if (c == minC) { b.Left = true; b.LeftThickness = 1; }
                            if (c == maxC) { b.Right = true; b.RightThickness = 1; }
                            break;
                        case 7: // Thick Outside Border (Viền ngoài đậm)
                            if (r == minR) { b.Top = true; b.TopThickness = 3; }
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 3; }
                            if (c == minC) { b.Left = true; b.LeftThickness = 3; }
                            if (c == maxC) { b.Right = true; b.RightThickness = 3; }
                            break;
                        case 8: // Bottom Double Border (Viền dưới kép)
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 4; }
                            break;
                        case 9: // Thick Bottom Border (Viền dưới đậm)
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 3; }
                            break;
                        case 10: // Top and Bottom Border (Viền trên và dưới)
                            if (r == minR) { b.Top = true; b.TopThickness = 1; }
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 1; }
                            break;
                        case 11: // Top and Thick Bottom Border (Viền trên và dưới đậm)
                            if (r == minR) { b.Top = true; b.TopThickness = 1; }
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 3; }
                            break;
                        case 12: // Top and Double Bottom Border (Viền trên và dưới kép)
                            if (r == minR) { b.Top = true; b.TopThickness = 1; }
                            if (r == maxR) { b.Bottom = true; b.BottomThickness = 4; }
                            break;
                    }
                }
            }
        }

        // ==================== Format Painter ====================
        public void CopyFormatting() { CopyFormat(); }
        public void PasteFormatting() { PasteFormat(); }

        public void CopyFormat()
        {
            CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
            if (cell != null && cell.Format != null)
            {
                CopiedFormat = cell.Format.Clone();
                IsFormatPainterActive = true;
            }
            else
            {
                CopiedFormat = new CellFormat();
                IsFormatPainterActive = true;
            }
        }

        public void PasteFormat()
        {
            if (CopiedFormat == null) return;
            SaveUndoState();
            ForEachSelectedCell(cell => {
                cell.Format.FontName = CopiedFormat.FontName;
                cell.Format.FontSize = CopiedFormat.FontSize;
                cell.Format.Bold = CopiedFormat.Bold;
                cell.Format.Italic = CopiedFormat.Italic;
                cell.Format.Underline = CopiedFormat.Underline;
                cell.Format.Strikethrough = CopiedFormat.Strikethrough;
                cell.Format.TextColor = CopiedFormat.TextColor;
                cell.Format.BgColor = CopiedFormat.BgColor;
                cell.Format.Align = CopiedFormat.Align;
                cell.Format.VAlign = CopiedFormat.VAlign;
                cell.Format.WrapText = CopiedFormat.WrapText;
                cell.Format.NumberFormat = CopiedFormat.NumberFormat;
                cell.Format.DecimalPlaces = CopiedFormat.DecimalPlaces;
                cell.Format.Borders = CopiedFormat.Borders != null ? new CellBorderSettings {
                    Top = CopiedFormat.Borders.Top, Bottom = CopiedFormat.Borders.Bottom,
                    Left = CopiedFormat.Borders.Left, Right = CopiedFormat.Borders.Right,
                    Color = CopiedFormat.Borders.Color, Thickness = CopiedFormat.Borders.Thickness,
                    TopThickness = CopiedFormat.Borders.TopThickness, BottomThickness = CopiedFormat.Borders.BottomThickness,
                    LeftThickness = CopiedFormat.Borders.LeftThickness, RightThickness = CopiedFormat.Borders.RightThickness,
                    TopColor = CopiedFormat.Borders.TopColor, BottomColor = CopiedFormat.Borders.BottomColor,
                    LeftColor = CopiedFormat.Borders.LeftColor, RightColor = CopiedFormat.Borders.RightColor
                } : new CellBorderSettings();
            });
            IsFormatPainterActive = false;
        }

        // ==================== Insert / Delete Rows & Columns ====================

        public void InsertRows(uint count = 1)
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            CurrentSheet.InsertRows(minR, count);
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void DeleteRows(uint count = 1)
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint delCount = Math.Max(count, maxR - minR + 1);
            CurrentSheet.DeleteRows(minR, delCount);
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void InsertCols(uint count = 1)
        {
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            CurrentSheet.InsertCols(minC, count);
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void DeleteCols(uint count = 1)
        {
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            uint delCount = Math.Max(count, maxC - minC + 1);
            CurrentSheet.DeleteCols(minC, delCount);
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        // ==================== Clear Actions ====================

        public void ClearAll()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            CurrentSheet.ClearRange(minR, minC, maxR, maxC);
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void ClearFormats()
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                cell.Format = new CellFormat();
            });
        }

        public void ClearContents()
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                cell.Type = CellType.Empty;
                cell.RawValue = "";
                cell.FormulaStr = "";
                cell.NumericValue = 0;
                cell.EvaluatedStr = "";
            });
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        // ==================== Table Styling & Design Operations ====================

        public bool IsSelectionInsideTable(out ExcelTableInfo activeTable)
        {
            activeTable = null;
            if (Tables == null || Tables.Count == 0) return false;
            foreach (var tbl in Tables)
            {
                if (tbl.IntersectsRange(SelectedRow, SelEndRow, SelectedCol, SelEndCol))
                {
                    activeTable = tbl;
                    return true;
                }
            }
            return false;
        }

        public void AutoExpandTable(ExcelTableInfo tbl, uint targetRow, uint targetCol)
        {
            SaveUndoState();
            uint oldEndCol = tbl.EndCol;
            uint oldEndRow = tbl.EndRow;

            // Expand columns if targetCol > tbl.EndCol (adds Column 2, 3, 4...)
            if (targetCol > tbl.EndCol)
            {
                for (uint c = oldEndCol + 1; c <= targetCol; c++)
                {
                    string colHeader = "Column" + (c - tbl.StartCol + 1);
                    if (tbl.HasHeaders)
                    {
                        CellData hCell = CurrentSheet.GetOrCreateCell(tbl.StartRow, c);
                        if (string.IsNullOrEmpty(hCell.RawValue))
                        {
                            hCell.RawValue = colHeader;
                            hCell.Type = CellType.String;
                            hCell.EvaluatedStr = colHeader;
                        }
                    }
                }
                tbl.EndCol = targetCol;
            }

            // Expand rows if targetRow > tbl.EndRow
            if (targetRow > tbl.EndRow)
            {
                tbl.EndRow = targetRow;
            }

            ApplyTableStyle(tbl, tbl.StyleIndex);
        }

        public void ApplyTableStyle(int styleIdx)
        {
            ExcelTableInfo activeTbl;
            if (IsSelectionInsideTable(out activeTbl) && activeTbl != null)
            {
                ApplyTableStyle(activeTbl, styleIdx);
                return;
            }

            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            ExcelTableInfo newTbl = new ExcelTableInfo("Table" + (Tables.Count + 1), minR, maxR, minC, maxC, true, styleIdx);
            Tables.Add(newTbl);
            ApplyTableStyle(newTbl, styleIdx);
        }

        public void ApplyTableStyle(ExcelTableInfo tbl, int styleIdx)
        {
            SaveUndoState();
            tbl.StyleIndex = styleIdx;

            int headerBg = unchecked((int)0xFF1E40AF); // Default Navy (Image 1 active style)
            int zebraBg = unchecked((int)0xFFEFF6FF);
            int headerTxt = unchecked((int)0xFFFFFFFF);

            if (styleIdx == 0) // Light / Plain Gray
            {
                headerBg = unchecked((int)0xFFF1F5F9);
                zebraBg = unchecked((int)0xFFF8FAFC);
                headerTxt = unchecked((int)0xFF0F172A);
            }
            else if (styleIdx == 1) // Medium Yellow / Gold
            {
                headerBg = unchecked((int)0xFFD97706);
                zebraBg = unchecked((int)0xFFFFFBEB);
            }
            else if (styleIdx == 2) // Medium Sky Blue
            {
                headerBg = unchecked((int)0xFF0284C7);
                zebraBg = unchecked((int)0xFFF0F9FF);
            }
            else if (styleIdx == 3) // Medium Green
            {
                headerBg = unchecked((int)0xFF16A34A);
                zebraBg = unchecked((int)0xFFF0FDF4);
            }
            else if (styleIdx == 4) // Medium Charcoal / Black
            {
                headerBg = unchecked((int)0xFF1F2937);
                zebraBg = unchecked((int)0xFFF3F4F6);
            }
            else if (styleIdx == 5) // Medium Navy Blue (Style 5 / Default)
            {
                headerBg = unchecked((int)0xFF1E40AF);
                zebraBg = unchecked((int)0xFFEFF6FF);
            }

            for (uint r = tbl.StartRow; r <= tbl.EndRow; r++)
            {
                bool isHeader = (r == tbl.StartRow && tbl.HasHeaders);
                bool isZebra = tbl.BandedRows && ((r - tbl.StartRow) % 2 == 1);
                bool isTotal = (tbl.HasTotalRow && r == tbl.EndRow);

                for (uint c = tbl.StartCol; c <= tbl.EndCol; c++)
                {
                    CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                    var b = cell.Format.Borders;
                    b.Top = true; b.TopThickness = 1;
                    b.Bottom = true; b.BottomThickness = isTotal ? 2 : 1;
                    b.Left = true; b.LeftThickness = 1;
                    b.Right = true; b.RightThickness = 1;

                    if (isHeader)
                    {
                        cell.Format.BgColor = headerBg;
                        cell.Format.TextColor = headerTxt;
                        cell.Format.Bold = true;
                        if (string.IsNullOrEmpty(cell.RawValue))
                        {
                            cell.RawValue = "Column" + (c - tbl.StartCol + 1);
                            cell.EvaluatedStr = cell.RawValue;
                        }
                    }
                    else if (isTotal)
                    {
                        cell.Format.Bold = true;
                        cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                        cell.Format.TextColor = unchecked((int)0xFF0F172A);
                        if (c == tbl.StartCol && string.IsNullOrEmpty(cell.RawValue))
                        {
                            cell.RawValue = "Total";
                            cell.EvaluatedStr = "Total";
                        }
                    }
                    else if (isZebra)
                    {
                        cell.Format.BgColor = zebraBg;
                        cell.Format.TextColor = unchecked((int)0xFF0F172A);
                    }
                    else
                    {
                        cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                        cell.Format.TextColor = unchecked((int)0xFF0F172A);
                    }

                    if (tbl.FirstColumn && c == tbl.StartCol && !isHeader) cell.Format.Bold = true;
                    if (tbl.LastColumn && c == tbl.EndCol && !isHeader) cell.Format.Bold = true;
                }
            }
        }

        public void ToggleTableTotalRow(ExcelTableInfo tbl)
        {
            SaveUndoState();
            if (tbl.HasTotalRow)
            {
                if (tbl.EndRow > tbl.StartRow + 1)
                {
                    for (uint c = tbl.StartCol; c <= tbl.EndCol; c++)
                    {
                        CurrentSheet.SetCellText(tbl.EndRow, c, "");
                    }
                    tbl.EndRow--;
                }
                tbl.HasTotalRow = false;
            }
            else
            {
                tbl.EndRow++;
                tbl.HasTotalRow = true;
                CurrentSheet.SetCellText(tbl.EndRow, tbl.StartCol, "Total");
                for (uint c = tbl.StartCol + 1; c <= tbl.EndCol; c++)
                {
                    double colSum = 0;
                    bool hasNum = false;
                    for (uint r = tbl.StartRow + (tbl.HasHeaders ? 1u : 0u); r < tbl.EndRow; r++)
                    {
                        CellData cd = CurrentSheet.GetCell(r, c);
                        if (cd != null && cd.Type == CellType.Number)
                        {
                            colSum += cd.NumericValue;
                            hasNum = true;
                        }
                    }
                    if (hasNum)
                    {
                        CellData totCell = CurrentSheet.GetOrCreateCell(tbl.EndRow, c);
                        totCell.SetNumber(colSum);
                    }
                }
            }
            ApplyTableStyle(tbl, tbl.StyleIndex);
        }

        public void ConvertTableToRange(ExcelTableInfo tbl)
        {
            SaveUndoState();
            if (Tables.Contains(tbl))
            {
                Tables.Remove(tbl);
            }
        }

        public void RemoveTableDuplicates(ExcelTableInfo tbl)
        {
            SaveUndoState();
            uint sR = tbl.HasHeaders ? (tbl.StartRow + 1) : tbl.StartRow;
            HashSet<string> seen = new HashSet<string>();
            List<uint> dupRows = new List<uint>();

            for (uint r = sR; r <= tbl.EndRow; r++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (uint c = tbl.StartCol; c <= tbl.EndCol; c++)
                {
                    CellData cd = CurrentSheet.GetCell(r, c);
                    sb.Append(cd != null ? cd.GetDisplayText() : "").Append("|");
                }
                string rowKey = sb.ToString();
                if (seen.Contains(rowKey)) dupRows.Add(r);
                else seen.Add(rowKey);
            }

            foreach (uint dr in dupRows)
            {
                for (uint c = tbl.StartCol; c <= tbl.EndCol; c++)
                {
                    CurrentSheet.SetCellText(dr, c, "");
                }
            }
        }

        public void ClearSelectedCells()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetCell(r, c);
                    if (cell != null)
                    {
                        cell.RawValue = "";
                        cell.NumericValue = 0;
                        cell.EvaluatedStr = "";
                        cell.FormulaStr = "";
                        cell.Type = CellType.Empty;
                    }
                }
            }
            RecomputeHiddenRows();
        }

        // ==================== Conditional Formatting ====================

        public void ApplyConditionalFormatting(int ruleType)
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            List<double> numbers = new List<double>();
            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetCell(r, c);
                    if (cell != null && (cell.Type == CellType.Number || cell.Type == CellType.Formula))
                    {
                        numbers.Add(cell.NumericValue);
                    }
                }
            }

            if (numbers.Count == 0) return;
            double minVal = double.MaxValue, maxVal = double.MinValue, sum = 0;
            for (int i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] < minVal) minVal = numbers[i];
                if (numbers[i] > maxVal) maxVal = numbers[i];
                sum += numbers[i];
            }
            double avg = sum / numbers.Count;

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                    if (cell.Type == CellType.Number || cell.Type == CellType.Formula)
                    {
                        double val = cell.NumericValue;
                        if (ruleType == 0) // Highlight Above Average (Green)
                        {
                            if (val >= avg) { cell.Format.BgColor = unchecked((int)0xFFDCFCE7); cell.Format.TextColor = unchecked((int)0xFF166534); }
                            else { cell.Format.BgColor = unchecked((int)0xFFFEE2E2); cell.Format.TextColor = unchecked((int)0xFF991B1B); }
                        }
                        else if (ruleType == 1) // Highlight Below Average (Red)
                        {
                            if (val < avg) { cell.Format.BgColor = unchecked((int)0xFFFEE2E2); cell.Format.TextColor = unchecked((int)0xFF991B1B); }
                        }
                        else if (ruleType == 2) // Heatmap Scale
                        {
                            double range = Math.Max(1.0, maxVal - minVal);
                            double frac = (val - minVal) / range;
                            if (frac >= 0.66) { cell.Format.BgColor = unchecked((int)0xFFDCFCE7); cell.Format.TextColor = unchecked((int)0xFF166534); }
                            else if (frac >= 0.33) { cell.Format.BgColor = unchecked((int)0xFFFEF3C7); cell.Format.TextColor = unchecked((int)0xFF92400E); }
                            else { cell.Format.BgColor = unchecked((int)0xFFFEE2E2); cell.Format.TextColor = unchecked((int)0xFF991B1B); }
                        }
                        else if (ruleType == 3) // Clear
                        {
                            cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                            cell.Format.TextColor = unchecked((int)0xFF000000);
                        }
                    }
                }
            }
        }

        // ==================== Fill Range ====================

        public void FillRange(int direction) // 0: Down, 1: Right, 2: Up, 3: Left
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            if (direction == 0 && maxR > minR) // Fill Down
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData src = CurrentSheet.GetCell(minR, c);
                    for (uint r = minR + 1; r <= maxR; r++)
                    {
                        CellData tgt = CurrentSheet.GetOrCreateCell(r, c);
                        if (src != null)
                        {
                            tgt.Type = src.Type;
                            tgt.RawValue = src.RawValue;
                            tgt.NumericValue = src.NumericValue;
                            tgt.EvaluatedStr = src.EvaluatedStr;
                            tgt.Format = src.Format != null ? src.Format.Clone() : new CellFormat();
                            if (src.Type == CellType.Formula)
                            {
                                tgt.FormulaStr = ShiftFormulaReferences(src.FormulaStr, (int)(r - minR), 0);
                            }
                        }
                    }
                }
            }
            else if (direction == 1 && maxC > minC) // Fill Right
            {
                for (uint r = minR; r <= maxR; r++)
                {
                    CellData src = CurrentSheet.GetCell(r, minC);
                    for (uint c = minC + 1; c <= maxC; c++)
                    {
                        CellData tgt = CurrentSheet.GetOrCreateCell(r, c);
                        if (src != null)
                        {
                            tgt.Type = src.Type;
                            tgt.RawValue = src.RawValue;
                            tgt.NumericValue = src.NumericValue;
                            tgt.EvaluatedStr = src.EvaluatedStr;
                            tgt.Format = src.Format != null ? src.Format.Clone() : new CellFormat();
                            if (src.Type == CellType.Formula)
                            {
                                tgt.FormulaStr = ShiftFormulaReferences(src.FormulaStr, 0, (int)(c - minC));
                            }
                        }
                    }
                }
            }
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void AutoSum(string func = "SUM")
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            // Case 1: Multi-cell selection (e.g. A1:A5 selected -> put sum in A6)
            if (minR != maxR || minC != maxC)
            {
                uint targetR = maxR + 1;
                for (uint c = minC; c <= maxC; c++)
                {
                    string rRange = string.Format("{0}{1}:{0}{2}", SparseMatrix.ColIndexToName(c), minR, maxR);
                    CurrentSheet.SetCellText(targetR, c, string.Format("={0}({1})", func, rRange));
                }
                SelectedRow = targetR;
                SelEndRow = targetR;
            }
            else
            {
                // Case 2: Single cell selected -> Heuristic Range Detection
                // 2a. Scan upwards
                uint curR = SelectedRow;
                uint curC = SelectedCol;
                uint scanTopR = curR;

                while (scanTopR > 1)
                {
                    CellData cellAbove = CurrentSheet.GetCell(scanTopR - 1, curC);
                    if (cellAbove != null && (cellAbove.Type == CellType.Number || cellAbove.Type == CellType.Formula))
                    {
                        scanTopR--;
                    }
                    else
                    {
                        break;
                    }
                }

                if (scanTopR < curR)
                {
                    // Found numeric range above!
                    string rRange = string.Format("{0}{1}:{0}{2}", SparseMatrix.ColIndexToName(curC), scanTopR, curR - 1);
                    CurrentSheet.SetCellText(curR, curC, string.Format("={0}({1})", func, rRange));
                }
                else
                {
                    // 2b. Scan leftwards
                    uint scanLeftC = curC;
                    while (scanLeftC > 1)
                    {
                        CellData cellLeft = CurrentSheet.GetCell(curR, scanLeftC - 1);
                        if (cellLeft != null && (cellLeft.Type == CellType.Number || cellLeft.Type == CellType.Formula))
                        {
                            scanLeftC--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (scanLeftC < curC)
                    {
                        string rRange = string.Format("{0}{1}:{2}{1}", SparseMatrix.ColIndexToName(scanLeftC), curR, SparseMatrix.ColIndexToName(curC - 1));
                        CurrentSheet.SetCellText(curR, curC, string.Format("={0}({1})", func, rRange));
                    }
                    else
                    {
                        // Default
                        CurrentSheet.SetCellText(curR, curC, string.Format("={0}()", func));
                    }
                }
            }

            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void CalculateNow(bool fullForce = false)
        {
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        public void GetContiguousDataRange(uint inRow, uint inCol, out uint minR, out uint maxR, out uint minC, out uint maxC)
        {
            minR = inRow;
            maxR = inRow;
            minC = inCol;
            maxC = inCol;

            while (minR > 1 && HasCellData(minR - 1, minC)) minR--;
            while (maxR < 5000 && HasCellData(maxR + 1, minC)) maxR++;
            while (minC > 1 && HasCellData(minR, minC - 1)) minC--;
            while (maxC < 500 && HasCellData(minR, maxC + 1)) maxC++;

            bool expanded = true;
            while (expanded)
            {
                expanded = false;
                for (uint r = minR; r <= maxR; r++)
                {
                    while (minC > 1 && HasCellData(r, minC - 1)) { minC--; expanded = true; }
                    while (maxC < 500 && HasCellData(r, maxC + 1)) { maxC++; expanded = true; }
                }
                for (uint c = minC; c <= maxC; c++)
                {
                    while (minR > 1 && HasCellData(minR - 1, c)) { minR--; expanded = true; }
                    while (maxR < 5000 && HasCellData(maxR + 1, c)) { maxR++; expanded = true; }
                }
            }
        }

        private bool HasCellData(uint r, uint c)
        {
            CellData cd = CurrentSheet.GetCell(r, c);
            return cd != null && (!string.IsNullOrEmpty(cd.RawValue) || !string.IsNullOrEmpty(cd.FormulaStr) || cd.NumericValue != 0);
        }

        public void ShowSheetProtectedWarning()
        {
            try { System.Media.SystemSounds.Beep.Play(); } catch { }
        }

        public void ToggleProtectSheet(Form parentForm)
        {
            if (!IsSheetProtected)
            {
                using (ProtectSheetDialog dlg = new ProtectSheetDialog(false))
                {
                    if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                    {
                        IsSheetProtected = true;
                        ProtectPassword = dlg.EnteredPassword;
                        MessageBox.Show("Đã bật chế độ Bảo vệ Sheet thành công!\nCác ô tính hiện đang ở chế độ chỉ đọc để tránh vô tình chỉnh sửa.", "Bảo Vệ Trang Tính", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(ProtectPassword))
                {
                    using (ProtectSheetDialog dlg = new ProtectSheetDialog(true))
                    {
                        if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                        {
                            if (dlg.EnteredPassword == ProtectPassword)
                            {
                                IsSheetProtected = false;
                                ProtectPassword = "";
                                MessageBox.Show("Mở khóa trang tính thành công! Bạn có thể tự do chỉnh sửa lại các ô.", "Mở Khóa Trang Tính", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Mật khẩu không chính xác! Không thể mở khóa trang tính.", "Lỗi Mật Khẩu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else
                {
                    IsSheetProtected = false;
                    MessageBox.Show("Đã mở khóa trang tính thành công! Bạn có thể tự do chỉnh sửa lại các ô.", "Mở Khóa Trang Tính", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            if (parentForm != null) parentForm.Invalidate();
        }

        public void RunSpellCheck(Form parentForm)
        {
            List<SpellCheckItem> items = new List<SpellCheckItem>();
            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "doan thu", "Doanh thu" },
                { "doanhtu", "Doanh thu" },
                { "tong cong", "Tổng cộng" },
                { "tongcong", "Tổng cộng" },
                { "loi nhuan", "Lợi nhuận" },
                { "loinhuan", "Lợi nhuận" },
                { "chi phi", "Chi phí" },
                { "chiphi", "Chi phí" },
                { "ngay thang", "Ngày tháng" },
                { "ngaythang", "Ngày tháng" },
                { "so luong", "Số lượng" },
                { "soluong", "Số lượng" },
                { "don gia", "Đơn giá" },
                { "dongia", "Đơn giá" },
                { "thanh tien", "Thành tiền" },
                { "thanhtien", "Thành tiền" },
                { "nhan vien", "Nhân viên" },
                { "nhanvien", "Nhân viên" },
                { "khach hang", "Khách hàng" },
                { "khachhang", "Khách hàng" },
                { "dia chi", "Địa chỉ" },
                { "diachi", "Địa chỉ" },
                { "dien thoai", "Điện thoại" },
                { "dienthoai", "Điện thoại" },
                { "ghi chu", "Ghi chú" },
                { "ghichu", "Ghi chú" }
            };

            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                CellData cell = kvp.Value;
                if (cell == null || string.IsNullOrEmpty(cell.RawValue) || cell.Type == CellType.Number || cell.Type == CellType.Formula) continue;

                string text = cell.RawValue.Trim();
                if (string.IsNullOrEmpty(text) || text.StartsWith("=")) continue;

                uint r = (uint)(kvp.Key >> 32);
                uint c = (uint)(kvp.Key & 0xFFFFFFFF);
                string cellCoord = string.Format("Ô {0}{1}", SparseMatrix.ColIndexToName(c), r);

                foreach (var dk in dict.Keys)
                {
                    if (string.Equals(text, dk, StringComparison.OrdinalIgnoreCase))
                    {
                        var scItem = new SpellCheckItem
                        {
                            Location = cellCoord,
                            OriginalText = text,
                            ProblemWord = text,
                            Reason = "Từ chưa có dấu chuẩn tiếng Việt",
                            ApplyCorrection = (newVal) =>
                            {
                                cell.RawValue = newVal;
                                cell.EvaluatedStr = newVal;
                            }
                        };
                        scItem.Suggestions.Add(dict[dk]);
                        items.Add(scItem);
                        break;
                    }
                }

                string[] words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int w = 0; w < words.Length - 1; w++)
                {
                    double dummy;
                    if (string.Equals(words[w], words[w + 1], StringComparison.OrdinalIgnoreCase) && words[w].Length > 1 && !double.TryParse(words[w], out dummy))
                    {
                        string repWord = words[w] + " " + words[w + 1];
                        string singleWord = words[w];
                        var scItem = new SpellCheckItem
                        {
                            Location = cellCoord,
                            OriginalText = text,
                            ProblemWord = repWord,
                            Reason = "Phát hiện từ lặp lại hai lần liên tiếp",
                            ApplyCorrection = (newVal) =>
                            {
                                cell.RawValue = newVal;
                                cell.EvaluatedStr = newVal;
                            }
                        };
                        scItem.Suggestions.Add(text.Replace(repWord, singleWord));
                        items.Add(scItem);
                        break;
                    }
                }

                foreach (var wStr in words)
                {
                    if (wStr.Length >= 3 && char.IsLower(wStr[0]) && char.IsUpper(wStr[1]))
                    {
                        string fixedWord = char.ToUpper(wStr[0]) + wStr.Substring(1).ToLower();
                        var scItem = new SpellCheckItem
                        {
                            Location = cellCoord,
                            OriginalText = text,
                            ProblemWord = wStr,
                            Reason = "Lỗi viết hoa bất thường (Caps Lock)",
                            ApplyCorrection = (newVal) =>
                            {
                                cell.RawValue = newVal;
                                cell.EvaluatedStr = newVal;
                            }
                        };
                        scItem.Suggestions.Add(text.Replace(wStr, fixedWord));
                        items.Add(scItem);
                        break;
                    }
                }
            }

            if (items.Count > 0)
            {
                using (SpellCheckDialog scDlg = new SpellCheckDialog(items, parentForm))
                {
                    scDlg.ShowDialog(parentForm);
                }
            }
            else
            {
                MessageBox.Show("Kiểm tra chính tả hoàn tất: Tất cả các ô văn bản trên trang tính đều hợp lệ!", "GenOffice Spelling", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void ToggleFilter()
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }

            if (Tables != null && Tables.Count > 0)
            {
                ExcelTableInfo insideTable;
                if (IsSelectionInsideTable(out insideTable) && insideTable != null)
                {
                    insideTable.ShowFilterButtons = !insideTable.ShowFilterButtons;
                    return;
                }
                else
                {
                    Tables[0].ShowFilterButtons = !Tables[0].ShowFilterButtons;
                    return;
                }
            }

            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            if (minR == maxR && minC == maxC)
            {
                GetContiguousDataRange(SelectedRow, SelectedCol, out minR, out maxR, out minC, out maxC);
            }

            if (maxR <= minR)
            {
                maxR = minR + 5;
                maxC = minC + 3;
            }

            string rStr = string.Format("{0}{1}:{2}{3}", SparseMatrix.ColIndexToName(minC), minR, SparseMatrix.ColIndexToName(maxC), maxR);
            CreateTableFromRange(rStr, true);
        }

        public void SortSelectedRange(bool ascending)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }

            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            if (minR == maxR && minC == maxC)
            {
                GetContiguousDataRange(SelectedRow, SelectedCol, out minR, out maxR, out minC, out maxC);
            }

            if (maxR <= minR) return;

            int sortKeyColIdx = (int)Math.Max(0, Math.Min(maxC - minC, SelectedCol - minC));

            bool hasHeader = false;
            CellData topCell = CurrentSheet.GetCell(minR, (uint)(minC + sortKeyColIdx));
            CellData nextCell = CurrentSheet.GetCell(minR + 1, (uint)(minC + sortKeyColIdx));
            if (topCell != null && nextCell != null)
            {
                if (topCell.Type == CellType.String && (nextCell.Type == CellType.Number || nextCell.Type == CellType.Formula))
                {
                    hasHeader = true;
                }
                else if (topCell.Format != null && topCell.Format.Bold && (nextCell.Format == null || !nextCell.Format.Bold))
                {
                    hasHeader = true;
                }
                else if (minR == 1 && topCell.Type == CellType.String)
                {
                    hasHeader = true;
                }
            }

            uint dataStartR = hasHeader ? (minR + 1) : minR;
            if (dataStartR >= maxR) dataStartR = minR;

            List<List<CellData>> rowData = new List<List<CellData>>();
            for (uint r = dataStartR; r <= maxR; r++)
            {
                List<CellData> rowCells = new List<CellData>();
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData orig = CurrentSheet.GetCell(r, c);
                    CellData clone = orig != null ? new CellData { Type = orig.Type, RawValue = orig.RawValue, FormulaStr = orig.FormulaStr, NumericValue = orig.NumericValue, EvaluatedStr = orig.EvaluatedStr, Format = orig.Format.Clone() } : new CellData();
                    rowCells.Add(clone);
                }
                rowData.Add(rowCells);
            }

            rowData.Sort((a, b) => {
                CellData cA = (sortKeyColIdx < a.Count) ? a[sortKeyColIdx] : new CellData();
                CellData cB = (sortKeyColIdx < b.Count) ? b[sortKeyColIdx] : new CellData();
                int cmp = 0;

                double numA = 0, numB = 0;
                bool isNumA = (cA.Type == CellType.Number) || double.TryParse(cA.RawValue ?? cA.EvaluatedStr, out numA);
                if (cA.Type == CellType.Number) numA = cA.NumericValue;
                else double.TryParse(cA.RawValue ?? cA.EvaluatedStr, out numA);

                bool isNumB = (cB.Type == CellType.Number) || double.TryParse(cB.RawValue ?? cB.EvaluatedStr, out numB);
                if (cB.Type == CellType.Number) numB = cB.NumericValue;
                else double.TryParse(cB.RawValue ?? cB.EvaluatedStr, out numB);

                if (isNumA && isNumB)
                {
                    cmp = numA.CompareTo(numB);
                }
                else
                {
                    string strA = cA.EvaluatedStr ?? cA.RawValue ?? "";
                    string strB = cB.EvaluatedStr ?? cB.RawValue ?? "";
                    cmp = string.Compare(strA, strB, StringComparison.CurrentCultureIgnoreCase);
                }
                return ascending ? cmp : -cmp;
            });

            for (int rIdx = 0; rIdx < rowData.Count; rIdx++)
            {
                uint r = (uint)(dataStartR + rIdx);
                for (int cIdx = 0; cIdx < rowData[rIdx].Count; cIdx++)
                {
                    uint c = (uint)(minC + cIdx);
                    CellData source = rowData[rIdx][cIdx];
                    if (source.Type != CellType.Empty && (!string.IsNullOrEmpty(source.RawValue) || !string.IsNullOrEmpty(source.FormulaStr) || source.NumericValue != 0))
                    {
                        CellData target = CurrentSheet.GetOrCreateCell(r, c);
                        target.Type = source.Type;
                        target.RawValue = source.RawValue;
                        target.FormulaStr = source.FormulaStr;
                        target.NumericValue = source.NumericValue;
                        target.EvaluatedStr = source.EvaluatedStr;
                        target.Format = source.Format;
                    }
                    else
                    {
                        CurrentSheet.ClearCell(r, c);
                    }
                }
            }
            FormulaEngine.EvaluateSheet(CurrentSheet);
        }

        private void ForEachSelectedCell(Action<CellData> action)
        {
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                    action(cell);
                }
            }
        }

        #region Excel Table, PivotTable & Shape Helpers
        public void CreateTableFromRange(string rangeStr, bool hasHeaders)
        {
            SaveUndoState();
            uint sRow = SelectedRow, eRow = SelEndRow, sCol = SelectedCol, eCol = SelEndCol;
            if (!string.IsNullOrEmpty(rangeStr))
            {
                string clean = rangeStr.TrimStart('=').Replace("$", "");
                string[] parts = clean.Split(':');
                if (parts.Length == 2)
                {
                    uint r1, c1, r2, c2;
                    if (SparseMatrix.TryParseCellReference(parts[0], out r1, out c1) &&
                        SparseMatrix.TryParseCellReference(parts[1], out r2, out c2))
                    {
                        sRow = Math.Min(r1, r2);
                        eRow = Math.Max(r1, r2);
                        sCol = Math.Min(c1, c2);
                        eCol = Math.Max(c1, c2);
                    }
                }
            }

            int headerBg = unchecked((int)0xFF1E40AF); // Microsoft Blue Table
            int zebraBg = unchecked((int)0xFFEFF6FF);

            for (uint r = sRow; r <= eRow; r++)
            {
                bool isHeader = (r == sRow && hasHeaders);
                bool isZebra = ((r - sRow) % 2 == 1);

                for (uint c = sCol; c <= eCol; c++)
                {
                    CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                    var b = cell.Format.Borders;
                    b.Top = true; b.TopThickness = 1;
                    b.Bottom = true; b.BottomThickness = 1;
                    b.Left = true; b.LeftThickness = 1;
                    b.Right = true; b.RightThickness = 1;

                    if (isHeader)
                    {
                        cell.Format.BgColor = headerBg;
                        cell.Format.TextColor = unchecked((int)0xFFFFFFFF);
                        cell.Format.Bold = true;
                        if (string.IsNullOrEmpty(cell.RawValue))
                        {
                            cell.RawValue = "Column" + (c - sCol + 1);
                            cell.EvaluatedStr = cell.RawValue;
                        }
                    }
                    else if (isZebra)
                    {
                        cell.Format.BgColor = zebraBg;
                    }
                    else
                    {
                        cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                    }
                }
            }

            string tableName = "Table" + (Tables.Count + 1);
            Tables.Add(new ExcelTableInfo(tableName, sRow, eRow, sCol, eCol, hasHeaders, 1));
        }

        public void CreatePivotTable(string sourceRange, bool newSheet, string destinationLoc)
        {
            SaveUndoState();
            SparseMatrix targetSheet = CurrentSheet;
            uint destRow = 3;
            uint destCol = 7; // G3

            if (newSheet)
            {
                string pSheetName = "PivotTable" + (Sheets.Count + 1);
                AddSheet(pSheetName);
                ActiveSheetIndex = Sheets.Count - 1;
                targetSheet = CurrentSheet;
                destRow = 2;
                destCol = 2; // B2
            }
            else if (!string.IsNullOrEmpty(destinationLoc))
            {
                string locClean = destinationLoc.Replace("$", "");
                string[] parts = locClean.Split('!');
                string cellPart = parts.Length > 1 ? parts[1] : parts[0];
                uint dr, dc;
                if (SparseMatrix.TryParseCellReference(cellPart, out dr, out dc))
                {
                    destRow = dr;
                    destCol = dc;
                }
            }

            int headerBg = unchecked((int)0xFF107C41); // Excel Pivot Green
            int subBg = unchecked((int)0xFFE2E8F0);

            targetSheet.SetCellText(destRow, destCol, "BÁO CÁO PIVOTTABLE TỔNG HỢP");
            CellData titleCell = targetSheet.GetOrCreateCell(destRow, destCol);
            titleCell.Format.Bold = true;
            titleCell.Format.FontSize = 11.5f;
            titleCell.Format.TextColor = unchecked((int)0xFF0F172A);

            targetSheet.SetCellText(destRow + 1, destCol, "Nhãn Hàng (Row Labels)");
            CellData h1 = targetSheet.GetOrCreateCell(destRow + 1, destCol);
            h1.Format.BgColor = headerBg;
            h1.Format.TextColor = unchecked((int)0xFFFFFFFF);
            h1.Format.Bold = true;

            targetSheet.SetCellText(destRow + 1, destCol + 1, "Tổng Giá Trị (Sum of Values)");
            CellData h2 = targetSheet.GetOrCreateCell(destRow + 1, destCol + 1);
            h2.Format.BgColor = headerBg;
            h2.Format.TextColor = unchecked((int)0xFFFFFFFF);
            h2.Format.Bold = true;

            uint sRow = 4, eRow = 9, sCol = 2, valCol = 5;
            Dictionary<string, double> pivotData = new Dictionary<string, double>();
            for (uint r = sRow; r <= eRow; r++)
            {
                CellData cKey = CurrentSheet.GetCell(r, sCol);
                CellData cVal = CurrentSheet.GetCell(r, valCol);
                string key = (cKey != null && !string.IsNullOrEmpty(cKey.GetDisplayText())) ? cKey.GetDisplayText() : ("Mục " + (r - sRow + 1));
                double val = (cVal != null && cVal.Type == CellType.Number) ? cVal.NumericValue : (100.0 * (r - sRow + 1));
                if (!pivotData.ContainsKey(key)) pivotData[key] = 0;
                pivotData[key] += val;
            }

            uint curR = destRow + 2;
            double grandTotal = 0;
            foreach (var kvp in pivotData)
            {
                targetSheet.SetCellText(curR, destCol, kvp.Key);
                CellData c1 = targetSheet.GetOrCreateCell(curR, destCol);
                c1.Format.Borders.Bottom = true;
                c1.Format.Borders.BottomThickness = 1;

                targetSheet.SetCellText(curR, destCol + 1, kvp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                CellData c2 = targetSheet.GetOrCreateCell(curR, destCol + 1);
                c2.Type = CellType.Number;
                c2.NumericValue = kvp.Value;
                c2.Format.NumberFormat = "Currency";
                c2.Format.Borders.Bottom = true;
                c2.Format.Borders.BottomThickness = 1;

                grandTotal += kvp.Value;
                curR++;
            }

            targetSheet.SetCellText(curR, destCol, "Tổng cộng (Grand Total)");
            CellData gt1 = targetSheet.GetOrCreateCell(curR, destCol);
            gt1.Format.Bold = true;
            gt1.Format.BgColor = subBg;
            gt1.Format.Borders.Top = true;
            gt1.Format.Borders.Bottom = true;

            targetSheet.SetCellText(curR, destCol + 1, grandTotal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            CellData gt2 = targetSheet.GetOrCreateCell(curR, destCol + 1);
            gt2.Type = CellType.Number;
            gt2.NumericValue = grandTotal;
            gt2.Format.Bold = true;
            gt2.Format.BgColor = subBg;
            gt2.Format.NumberFormat = "Currency";
            gt2.Format.Borders.Top = true;
            gt2.Format.Borders.Bottom = true;
        }

        public void SortTableColumn(ExcelTableInfo table, uint colIdx, bool ascending)
        {
            SaveUndoState();
            uint sRow = table.HasHeaders ? (table.StartRow + 1) : table.StartRow;
            uint eRow = table.EndRow;

            List<List<CellData>> rowDatas = new List<List<CellData>>();
            for (uint r = sRow; r <= eRow; r++)
            {
                List<CellData> rowCells = new List<CellData>();
                for (uint c = table.StartCol; c <= table.EndCol; c++)
                {
                    CellData orig = CurrentSheet.GetCell(r, c);
                    CellData clone = orig != null ? orig.Clone() : new CellData();
                    rowCells.Add(clone);
                }
                rowDatas.Add(rowCells);
            }

            int keyOffset = (int)(colIdx - table.StartCol);
            rowDatas.Sort(delegate(List<CellData> rA, List<CellData> rB) {
                string valA = (keyOffset >= 0 && keyOffset < rA.Count && rA[keyOffset] != null) ? rA[keyOffset].GetDisplayText() : "";
                string valB = (keyOffset >= 0 && keyOffset < rB.Count && rB[keyOffset] != null) ? rB[keyOffset].GetDisplayText() : "";

                double numA, numB;
                bool isNumA = double.TryParse(valA.Replace("$", "").Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numA);
                bool isNumB = double.TryParse(valB.Replace("$", "").Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numB);

                int cmp;
                if (isNumA && isNumB) cmp = numA.CompareTo(numB);
                else cmp = string.Compare(valA, valB, StringComparison.OrdinalIgnoreCase);

                return ascending ? cmp : -cmp;
            });

            for (int i = 0; i < rowDatas.Count; i++)
            {
                uint r = sRow + (uint)i;
                List<CellData> rowCells = rowDatas[i];
                for (int j = 0; j < rowCells.Count; j++)
                {
                    uint c = table.StartCol + (uint)j;
                    CellData src = rowCells[j];
                    CellData target = CurrentSheet.GetOrCreateCell(r, c);
                    target.RawValue = src.RawValue;
                    target.EvaluatedStr = src.EvaluatedStr;
                    target.Type = src.Type;
                    target.NumericValue = src.NumericValue;
                    target.FormulaStr = src.FormulaStr;
                    target.Format = src.Format;
                }
            }
        }

        public void ApplyColumnFilter(ExcelTableInfo table, uint colIdx, HashSet<string> allowedValues)
        {
            table.ColumnFilters[colIdx] = allowedValues;
            RecomputeHiddenRows();
        }

        public void ClearColumnFilter(ExcelTableInfo table, uint colIdx)
        {
            if (table.ColumnFilters.ContainsKey(colIdx))
            {
                table.ColumnFilters.Remove(colIdx);
            }
            RecomputeHiddenRows();
        }

        public void RecomputeHiddenRows()
        {
            HiddenRows.Clear();
            foreach (var tbl in Tables)
            {
                if (tbl.ColumnFilters.Count == 0) continue;
                uint sRow = tbl.HasHeaders ? (tbl.StartRow + 1) : tbl.StartRow;
                for (uint r = sRow; r <= tbl.EndRow; r++)
                {
                    foreach (var filterKvp in tbl.ColumnFilters)
                    {
                        uint c = filterKvp.Key;
                        HashSet<string> allowed = filterKvp.Value;
                        CellData cell = CurrentSheet.GetCell(r, c);
                        string val = (cell != null && !string.IsNullOrEmpty(cell.GetDisplayText())) ? cell.GetDisplayText() : "(Blanks)";
                        if (!allowed.Contains(val))
                        {
                            HiddenRows.Add(r);
                            break;
                        }
                    }
                }
            }
        }

        public void InsertShape(string shapeType)
        {
            int startX = 60;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            DocShapeObject newSh = new DocShapeObject(shapeType, startX + 20, startY + 20, 160, 100);
            newSh.FillColor = Color.FromArgb(43, 87, 154); // Classic Office Blue
            newSh.OutlineColor = Color.FromArgb(30, 60, 110);
            newSh.OutlineWidth = 1.5f;

            Shapes.Add(newSh);
            SelectedShape = newSh;
        }

        public void InsertPicture(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath)) return;

            int startX = 60;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            Image img = null;
            int imgW = 200;
            int imgH = 150;
            try
            {
                img = Image.FromFile(imagePath);
                imgW = Math.Min(400, Math.Max(60, img.Width));
                imgH = Math.Min(300, Math.Max(40, img.Height));
            }
            catch { }

            DocShapeObject newPic = new DocShapeObject("Image", startX + 20, startY + 20, imgW, imgH);
            newPic.ImagePath = imagePath;
            newPic.CachedImage = img;
            newPic.FillColor = Color.Transparent;
            newPic.OutlineColor = Color.Transparent;

            Shapes.Add(newPic);
            SelectedShape = newPic;
        }

        public void InsertSmartArt(string type = "Process_Linear", string name = "Quy trình")
        {
            int startX = 60;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            int saW = 440;
            int saH = 190;
            DocShapeObject newSh = new DocShapeObject("SmartArt", startX + 20, startY + 20, saW, saH);
            newSh.SmartArtType = type;
            newSh.SmartArtTitle = name;
            newSh.FillColor = Color.FromArgb(248, 250, 252);
            newSh.OutlineColor = Color.FromArgb(16, 124, 65); // Excel Green accent
            newSh.OutlineWidth = 1.5f;

            Shapes.Add(newSh);
            SelectedShape = newSh;
        }

        public void DrawChartFloatingButtons(Graphics g, Rectangle chartRect)
        {
            int btnSize = 22;
            int btnX = chartRect.Right + 6;
            int startBtnY = chartRect.Top + 2;

            PlusBtnChartRect = new Rectangle(btnX, startBtnY, btnSize, btnSize);
            StyleBtnChartRect = new Rectangle(btnX, startBtnY + 28, btnSize, btnSize);
            FilterBtnChartRect = new Rectangle(btnX, startBtnY + 56, btnSize, btnSize);

            // 1. [ + ] Chart Elements
            using (Pen borderPen = new Pen(Color.FromArgb(16, 124, 65), 1.2f))
            {
                g.FillRectangle(Brushes.White, PlusBtnChartRect);
                g.DrawRectangle(borderPen, PlusBtnChartRect);
                using (Pen plusP = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    g.DrawLine(plusP, PlusBtnChartRect.X + 6, PlusBtnChartRect.Y + 11, PlusBtnChartRect.X + 16, PlusBtnChartRect.Y + 11);
                    g.DrawLine(plusP, PlusBtnChartRect.X + 11, PlusBtnChartRect.Y + 6, PlusBtnChartRect.X + 11, PlusBtnChartRect.Y + 16);
                }
            }

            // 2. [ 🖌 ] Chart Styles
            using (Pen bPen = new Pen(Color.FromArgb(209, 213, 219), 1.2f))
            {
                g.FillRectangle(Brushes.White, StyleBtnChartRect);
                g.DrawRectangle(bPen, StyleBtnChartRect);
                using (SolidBrush bB = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    g.FillRectangle(bB, StyleBtnChartRect.X + 6, StyleBtnChartRect.Y + 5, 10, 6);
                    g.FillPolygon(bB, new Point[] { new Point(StyleBtnChartRect.X + 8, StyleBtnChartRect.Y + 11), new Point(StyleBtnChartRect.X + 14, StyleBtnChartRect.Y + 11), new Point(StyleBtnChartRect.X + 11, StyleBtnChartRect.Y + 16) });
                }
            }

            // 3. [ ⏚ ] Values Filter
            using (Pen fPen = new Pen(Color.FromArgb(209, 213, 219), 1.2f))
            {
                g.FillRectangle(Brushes.White, FilterBtnChartRect);
                g.DrawRectangle(fPen, FilterBtnChartRect);
                using (Pen fIconPen = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                {
                    g.DrawLine(fIconPen, FilterBtnChartRect.X + 5, FilterBtnChartRect.Y + 6, FilterBtnChartRect.X + 17, FilterBtnChartRect.Y + 6);
                    g.DrawLine(fIconPen, FilterBtnChartRect.X + 7, FilterBtnChartRect.Y + 10, FilterBtnChartRect.X + 15, FilterBtnChartRect.Y + 10);
                    g.DrawLine(fIconPen, FilterBtnChartRect.X + 9, FilterBtnChartRect.Y + 14, FilterBtnChartRect.X + 13, FilterBtnChartRect.Y + 14);
                }
            }
        }

        public DocChartData ExtractChartDataFromSelection()
        {
            if (CurrentSheet == null) return null;

            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            if (minR == maxR && minC == maxC)
            {
                CellData c = CurrentSheet.GetCell(minR, minC);
                if (c == null || string.IsNullOrEmpty(c.RawValue))
                {
                    return null;
                }

                // Auto-expand
                while (minR > 1 && CurrentSheet.GetCell(minR - 1, minC) != null && !string.IsNullOrEmpty(CurrentSheet.GetCell(minR - 1, minC).RawValue)) minR--;
                while (maxR < 500 && CurrentSheet.GetCell(maxR + 1, minC) != null && !string.IsNullOrEmpty(CurrentSheet.GetCell(maxR + 1, minC).RawValue)) maxR++;
                while (minC > 1 && CurrentSheet.GetCell(minR, minC - 1) != null && !string.IsNullOrEmpty(CurrentSheet.GetCell(minR, minC - 1).RawValue)) minC--;
                while (maxC < 100 && CurrentSheet.GetCell(minR, maxC + 1) != null && !string.IsNullOrEmpty(CurrentSheet.GetCell(minR, maxC + 1).RawValue)) maxC++;
            }

            // 1. Detect optional table/sheet title in top row
            string tableTitle = "";
            uint startRow = minR;
            CellData topCell = CurrentSheet.GetCell(minR, minC);
            if (topCell != null && !string.IsNullOrEmpty(topCell.RawValue))
            {
                bool isBannerTitle = true;
                for (uint c = minC + 1; c <= maxC; c++)
                {
                    CellData rc = CurrentSheet.GetCell(minR, c);
                    if (rc != null && !string.IsNullOrEmpty(rc.RawValue)) { isBannerTitle = false; break; }
                }
                if (isBannerTitle && maxR > minR + 1)
                {
                    tableTitle = topCell.RawValue.Trim();
                    startRow = minR + 1;
                    while (startRow <= maxR && (CurrentSheet.GetCell(startRow, minC) == null || string.IsNullOrEmpty(CurrentSheet.GetCell(startRow, minC).RawValue)))
                    {
                        startRow++;
                    }
                }
            }

            // 2. Find Header Row
            uint headerRow = startRow;
            uint dataStartRow = headerRow + 1;

            uint categoryCol = minC;
            List<uint> seriesCols = new List<uint>();
            for (uint c = minC + 1; c <= maxC; c++)
            {
                seriesCols.Add(c);
            }

            // If only 1 column selected, use it as series and row numbers as categories
            if (seriesCols.Count == 0)
            {
                seriesCols.Add(minC);
            }

            DocChartData data = new DocChartData();
            data.Title = !string.IsNullOrEmpty(tableTitle) ? tableTitle : "Biểu đồ Phân tích Dữ liệu";
            data.Categories = new List<string>();
            data.SeriesNames = new List<string>();
            data.Values = new List<List<double>>();

            // 3. Build Series Names
            foreach (uint col in seriesCols)
            {
                CellData hCell = CurrentSheet.GetCell(headerRow, col);
                string colName = (hCell != null && !string.IsNullOrEmpty(hCell.RawValue)) ? hCell.RawValue.Trim() : ("Cột " + col);
                string fullSeriesName = !string.IsNullOrEmpty(tableTitle) ? (tableTitle + " " + colName) : colName;
                data.SeriesNames.Add(fullSeriesName);
            }

            // 4. Build Categories & Data Values
            for (uint r = dataStartRow; r <= maxR; r++)
            {
                CellData catCell = CurrentSheet.GetCell(r, categoryCol);
                string catName = (catCell != null && !string.IsNullOrEmpty(catCell.RawValue)) ? catCell.RawValue.Trim() : "";

                bool hasAnyData = false;
                List<double> rowVals = new List<double>();
                foreach (uint col in seriesCols)
                {
                    CellData vCell = CurrentSheet.GetCell(r, col);
                    double num = 0;
                    if (vCell != null && !string.IsNullOrEmpty(vCell.RawValue))
                    {
                        hasAnyData = true;
                        string cleanStr = vCell.RawValue.Replace("$", "").Replace("đ", "").Replace(",", "").Trim();
                        double.TryParse(cleanStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out num);
                    }
                    rowVals.Add(num);
                }

                if (!string.IsNullOrEmpty(catName) || hasAnyData)
                {
                    if (string.IsNullOrEmpty(catName))
                    {
                        for (uint c = minC; c <= maxC; c++)
                        {
                            CellData oc = CurrentSheet.GetCell(r, c);
                            if (oc != null && !string.IsNullOrEmpty(oc.RawValue))
                            {
                                string cl = oc.RawValue.Trim();
                                double dummy;
                                if (!double.TryParse(cl.Replace("$", "").Replace("đ", "").Replace(",", "").Trim(), out dummy))
                                {
                                    catName = cl;
                                    break;
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(catName)) catName = "Mục " + (data.Categories.Count + 1);
                    data.Categories.Add(catName);
                    data.Values.Add(rowVals);
                }
            }

            if (data.Categories.Count == 0 || data.Values.Count == 0) return null;

            return data;
        }

        public void InsertChartDirectly(string chartTypeId, Form parentForm)
        {
            DocChartData data = ExtractChartDataFromSelection();
            if (data == null)
            {
                data = DocChartData.CreateDefault(chartTypeId);
            }
            data.ChartType = chartTypeId;
            InsertChartObject(data);
            if (parentForm != null) parentForm.Invalidate();
        }

        public void InsertChartFromDialog(Form parentForm, int defaultCategoryIndex = -1)
        {
            DocChartData data = ExtractChartDataFromSelection();
            if (data == null)
            {
                data = DocChartData.CreateDefault("Column_Clustered");
            }

            using (InsertChartForm chartDlg = new InsertChartForm(data, defaultCategoryIndex))
            {
                if (chartDlg.ShowDialog(parentForm) == DialogResult.OK && !string.IsNullOrEmpty(chartDlg.SelectedChartType))
                {
                    data.ChartType = chartDlg.SelectedChartType;
                    InsertChartObject(data);
                    if (parentForm != null) parentForm.Invalidate();
                }
            }
        }

        public void InsertChartObject(DocChartData chartData)
        {
            if (chartData == null) return;
            int startX = 60;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            DocShapeObject newSh = new DocShapeObject
            {
                ShapeType = "Chart",
                ChartData = chartData,
                TextContent = chartData.ChartType,
                BaseWidth = 480,
                BaseHeight = 280,
                PageX = startX + 20,
                PageY = startY + 20,
                FillColor = Color.White,
                OutlineColor = Color.FromArgb(209, 213, 219),
                OutlineWidth = 1.0f
            };

            Shapes.Add(newSh);
            SelectedShape = newSh;
        }

        public int GetColScreenX(uint targetCol, int headerW)
        {
            int x = headerW;
            if (targetCol >= ScrollCol)
            {
                for (uint c = ScrollCol; c < targetCol; c++) x += GetColWidth(c);
            }
            else
            {
                for (uint c = targetCol; c < ScrollCol; c++) x -= GetColWidth(c);
            }
            return x;
        }

        public int GetRowScreenY(uint targetRow, int startY, int headerH)
        {
            int y = startY + headerH;
            if (targetRow >= ScrollRow)
            {
                for (uint r = ScrollRow; r < targetRow; r++) y += GetRowHeight(r);
            }
            else
            {
                for (uint r = targetRow; r < ScrollRow; r++) y -= GetRowHeight(r);
            }
            return y;
        }

        public Rectangle GetShapeScreenBounds(DocShapeObject shape, int headerW, int startY, int headerH)
        {
            int scrollPixelX = 0;
            for (uint c = 1; c < ScrollCol; c++) scrollPixelX += GetColWidth(c);
            int scrollPixelY = 0;
            for (uint r = 1; r < ScrollRow; r++) scrollPixelY += GetRowHeight(r);

            int screenX = headerW + (int)((shape.PageX - scrollPixelX) * ZoomFactor);
            int screenY = startY + headerH + (int)((shape.PageY - scrollPixelY) * ZoomFactor);
            int screenW = (int)((shape.BaseWidth + shape.WidthDelta) * ZoomFactor);
            int screenH = (int)((shape.BaseHeight + shape.HeightDelta) * ZoomFactor);

            return new Rectangle(screenX, screenY, Math.Max(10, screenW), Math.Max(10, screenH));
        }

        public DocShapeObject HitTestShape(int mouseX, int mouseY, int headerW, int startY, int headerH, out int handle)
        {
            handle = -1;
            if (SelectedShape != null)
            {
                Rectangle sb = GetShapeScreenBounds(SelectedShape, headerW, startY, headerH);
                Rectangle[] handles = GetHandleRectangles(sb);
                for (int i = 0; i < 8; i++)
                {
                    if (handles[i].Contains(mouseX, mouseY))
                    {
                        handle = i;
                        return SelectedShape;
                    }
                }
                if (sb.Contains(mouseX, mouseY))
                {
                    bool nearBorder = (mouseX <= sb.X + 6 || mouseX >= sb.Right - 6 || mouseY <= sb.Y + 6 || mouseY >= sb.Bottom - 6);
                    if (nearBorder || SelectedShape.ShapeType == "Image" || (!SelectedShape.IsEditingText && SelectedShape.ShapeType != "TextBox"))
                    {
                        handle = 8;
                    }
                    else
                    {
                        handle = -1;
                    }
                    return SelectedShape;
                }
            }

            for (int i = Shapes.Count - 1; i >= 0; i--)
            {
                DocShapeObject sh = Shapes[i];
                Rectangle sb = GetShapeScreenBounds(sh, headerW, startY, headerH);
                if (sb.Contains(mouseX, mouseY))
                {
                    bool nearBorder = (mouseX <= sb.X + 6 || mouseX >= sb.Right - 6 || mouseY <= sb.Y + 6 || mouseY >= sb.Bottom - 6);
                    if (nearBorder || sh.ShapeType == "Image")
                    {
                        handle = 8;
                    }
                    else
                    {
                        handle = -1;
                    }
                    return sh;
                }
            }

            return null;
        }

        public Rectangle[] GetHandleRectangles(Rectangle bounds)
        {
            int hs = 8;
            return new Rectangle[]
            {
                new Rectangle(bounds.X - hs / 2, bounds.Y - hs / 2, hs, hs),
                new Rectangle(bounds.X + bounds.Width / 2 - hs / 2, bounds.Y - hs / 2, hs, hs),
                new Rectangle(bounds.Right - hs / 2, bounds.Y - hs / 2, hs, hs),
                new Rectangle(bounds.Right - hs / 2, bounds.Y + bounds.Height / 2 - hs / 2, hs, hs),
                new Rectangle(bounds.Right - hs / 2, bounds.Bottom - hs / 2, hs, hs),
                new Rectangle(bounds.X + bounds.Width / 2 - hs / 2, bounds.Bottom - hs / 2, hs, hs),
                new Rectangle(bounds.X - hs / 2, bounds.Bottom - hs / 2, hs, hs),
                new Rectangle(bounds.X - hs / 2, bounds.Y + bounds.Height / 2 - hs / 2, hs, hs)
            };
        }

        public void DrawSparklineCell(Graphics g, Rectangle cellRect, SparklineInfo spk)
        {
            if (spk == null) return;
            List<float> values = new List<float>();
            for (uint r = spk.DataStartRow; r <= spk.DataEndRow; r++)
            {
                for (uint c = spk.DataStartCol; c <= spk.DataEndCol; c++)
                {
                    CellData cd = CurrentSheet.GetCell(r, c);
                    if (cd != null && !string.IsNullOrEmpty(cd.RawValue))
                    {
                        float v;
                        if (float.TryParse(cd.RawValue.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                        {
                            values.Add(v);
                        }
                    }
                }
            }

            if (values.Count == 0) return;

            Rectangle padRect = new Rectangle(cellRect.X + 4, cellRect.Y + 3, cellRect.Width - 8, cellRect.Height - 6);
            if (padRect.Width < 8 || padRect.Height < 6) return;

            float minV = values[0];
            float maxV = values[0];
            foreach (var val in values)
            {
                if (val < minV) minV = val;
                if (val > maxV) maxV = val;
            }
            if (Math.Abs(maxV - minV) < 0.0001f) { maxV += 1; minV -= 1; }

            int n = values.Count;

            if (spk.Type == "Line")
            {
                PointF[] pts = new PointF[n];
                int minIdx = 0, maxIdx = 0;
                for (int i = 0; i < n; i++)
                {
                    float px = padRect.Left + (n == 1 ? padRect.Width / 2f : (i * (float)padRect.Width / (n - 1)));
                    float norm = (values[i] - minV) / (maxV - minV);
                    float py = padRect.Bottom - (norm * padRect.Height);
                    pts[i] = new PointF(px, py);
                    if (values[i] <= values[minIdx]) minIdx = i;
                    if (values[i] >= values[maxIdx]) maxIdx = i;
                }

                using (Pen linePen = new Pen(Color.FromArgb(37, 99, 235), 1.8f))
                {
                    if (n > 1) g.DrawLines(linePen, pts);
                }

                // Min / Max point markers
                g.FillEllipse(Brushes.Crimson, pts[maxIdx].X - 2.5f, pts[maxIdx].Y - 2.5f, 5, 5);
                g.FillEllipse(Brushes.Teal, pts[minIdx].X - 2.5f, pts[minIdx].Y - 2.5f, 5, 5);
            }
            else if (spk.Type == "Column")
            {
                float barW = Math.Max(2, (float)padRect.Width / n - 2);
                for (int i = 0; i < n; i++)
                {
                    float bx = padRect.Left + i * (float)padRect.Width / n + 1;
                    float v = values[i];
                    float zeroY = minV < 0 ? (padRect.Bottom - ((-minV) / (maxV - minV)) * padRect.Height) : padRect.Bottom;
                    float norm = (Math.Abs(v)) / (maxV - minV);
                    float barH = Math.Max(2, norm * padRect.Height);

                    if (v >= 0)
                    {
                        using (SolidBrush bBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                        {
                            g.FillRectangle(bBrush, bx, zeroY - barH, barW, barH);
                        }
                    }
                    else
                    {
                        using (SolidBrush rBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                        {
                            g.FillRectangle(rBrush, bx, zeroY, barW, barH);
                        }
                    }
                }
            }
            else // WinLoss
            {
                float barW = Math.Max(2, (float)padRect.Width / n - 2);
                float midY = padRect.Top + padRect.Height / 2f;
                float halfH = Math.Max(2, padRect.Height * 0.42f);
                for (int i = 0; i < n; i++)
                {
                    float bx = padRect.Left + i * (float)padRect.Width / n + 1;
                    if (values[i] >= 0)
                    {
                        using (SolidBrush gBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
                        {
                            g.FillRectangle(gBrush, bx, midY - halfH, barW, halfH);
                        }
                    }
                    else
                    {
                        using (SolidBrush rBrush = new SolidBrush(Color.FromArgb(239, 68, 68)))
                        {
                            g.FillRectangle(rBrush, bx, midY, barW, halfH);
                        }
                    }
                }
            }
        }

        public void InsertSparklineInteractive(string type, Form parentForm)
        {
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            string defaultDataRange = string.Format("{0}{1}:{2}{3}", SparseMatrix.ColIndexToName(minC), minR, SparseMatrix.ColIndexToName(maxC), maxR);
            string defaultLocRange = string.Format("{0}{1}", SparseMatrix.ColIndexToName(maxC + 1), minR);

            using (CreateSparklineForm dlg = new CreateSparklineForm(type, defaultDataRange, defaultLocRange))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    uint targetCol = maxC + 1;
                    for (uint r = minR; r <= maxR; r++)
                    {
                        long key = ((long)r << 32) | (long)targetCol;
                        if (Sparklines == null) Sparklines = new Dictionary<long, SparklineInfo>();
                        Sparklines[key] = new SparklineInfo
                        {
                            Type = dlg.SparklineType,
                            DataStartRow = r,
                            DataStartCol = minC,
                            DataEndRow = r,
                            DataEndCol = maxC
                        };
                    }
                    if (GetColWidth(targetCol) < 110) CustomColWidths[targetCol] = 110;
                    if (parentForm != null) parentForm.Invalidate();
                }
            }
        }

        public void InsertTextBox(Form parentForm)
        {
            int startX = 80;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            if (Shapes == null) Shapes = new List<DocShapeObject>();
            foreach (var s in Shapes) { s.IsSelected = false; s.IsEditingText = false; }

            DocShapeObject tb = new DocShapeObject
            {
                Id = "shape_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ShapeType = "TextBox",
                TextContent = "",
                PlaceholderText = "Nhập văn bản tại đây...",
                FontFamily = "Segoe UI",
                FontSize = 11f,
                TextColor = Color.Black,
                FillColor = Color.White,
                OutlineColor = Color.FromArgb(0, 120, 215),
                OutlineWidth = 1.5f,
                PageX = startX + 10,
                PageY = startY + 10,
                BaseWidth = 190,
                BaseHeight = 65,
                IsSelected = true,
                IsEditingText = true,
                Visible = true
            };
            Shapes.Add(tb);
            SelectedShape = tb;
            if (parentForm != null) parentForm.Invalidate();
        }

        public void InsertHyperlink(string text, string url, string tooltip = "")
        {
            if (string.IsNullOrEmpty(url)) return;
            SaveUndoState();
            CellData cell = CurrentSheet.GetOrCreateCell(SelectedRow, SelectedCol);
            cell.RawValue = !string.IsNullOrEmpty(text) ? text : url;
            cell.Hyperlink = url;
            cell.Format.Underline = true;
            cell.Format.TextColor = Color.FromArgb(5, 99, 193).ToArgb(); // Excel Hyperlink Blue
        }

        public void InsertWordArt(Form parentForm)
        {
            int startX = 80;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            if (Shapes == null) Shapes = new List<DocShapeObject>();
            foreach (var s in Shapes) { s.IsSelected = false; s.IsEditingText = false; }

            DocShapeObject wa = new DocShapeObject
            {
                Id = "shape_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ShapeType = "WordArt",
                TextContent = "GenOffice Calc",
                FillColor = Color.FromArgb(37, 99, 235),
                OutlineColor = Color.FromArgb(30, 58, 138),
                OutlineWidth = 1.0f,
                PageX = startX + 10,
                PageY = startY + 10,
                BaseWidth = 220,
                BaseHeight = 50,
                HasShadow = true,
                IsSelected = true,
                IsEditingText = true,
                Visible = true
            };
            Shapes.Add(wa);
            SelectedShape = wa;
            if (parentForm != null) parentForm.Invalidate();
        }

        public void InsertSlicer(Form parentForm)
        {
            int startX = 80;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            string colName = SparseMatrix.ColIndexToName(SelectedCol > 0 ? SelectedCol : 1);
            CellData headerCell = CurrentSheet.GetCell(1, SelectedCol > 0 ? SelectedCol : 1);
            if (headerCell != null && !string.IsNullOrEmpty(headerCell.RawValue)) colName = headerCell.RawValue;

            DocShapeObject sl = new DocShapeObject
            {
                Id = "shape_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ShapeType = "Slicer",
                CustomName = "Bộ Cắt Lát: " + colName,
                TextContent = colName,
                FillColor = Color.FromArgb(248, 250, 252),
                OutlineColor = Color.FromArgb(16, 124, 65),
                OutlineWidth = 1.5f,
                PageX = startX + 10,
                PageY = startY + 10,
                BaseWidth = 160,
                BaseHeight = 200,
                Visible = true
            };
            Shapes.Add(sl);
            SelectedShape = sl;
            if (parentForm != null) parentForm.Invalidate();
        }

        public void InsertTimeline(Form parentForm)
        {
            int startX = 80;
            int startY = 80;
            if (SelectedRow > 0 && SelectedCol > 0)
            {
                startX = 0;
                for (uint c = 1; c < SelectedCol; c++) startX += GetColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRowHeight(r);
            }

            DocShapeObject tl = new DocShapeObject
            {
                Id = "shape_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ShapeType = "Timeline",
                CustomName = "Dòng Thời Gian (Timeline)",
                TextContent = "Thời gian: 2026 - Q1 Q2 Q3 Q4",
                FillColor = Color.FromArgb(248, 250, 252),
                OutlineColor = Color.FromArgb(2, 132, 199),
                OutlineWidth = 1.5f,
                PageX = startX + 10,
                PageY = startY + 10,
                BaseWidth = 300,
                BaseHeight = 110,
                Visible = true
            };
            Shapes.Add(tl);
            SelectedShape = tl;
            if (parentForm != null) parentForm.Invalidate();
        }

        public void InsertPivotChart(Form parentForm)
        {
            InsertChartDirectly("PivotChart_Standard", parentForm);
        }

        public void InsertMapChart(Form parentForm)
        {
            InsertChartDirectly("FilledMap_Choropleth", parentForm);
        }

        public void Insert3DMapChart(Form parentForm)
        {
            InsertChartDirectly("3DMap_GlobeSpatial", parentForm);
        }

        public void RemoveDuplicates(Form parentForm)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }

            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            if (minR == maxR && minC == maxC)
            {
                GetContiguousDataRange(SelectedRow, SelectedCol, out minR, out maxR, out minC, out maxC);
            }

            if (maxR <= minR)
            {
                MessageBox.Show("Vui lòng chọn một vùng dữ liệu có nhiều dòng để xóa trùng lặp!", "Xóa Trùng Lặp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveUndoState();

            HashSet<string> seenRows = new HashSet<string>();
            List<uint> rowsToDelete = new List<uint>();

            for (uint r = minR + 1; r <= maxR; r++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cd = CurrentSheet.GetCell(r, c);
                    sb.Append(cd != null ? (cd.EvaluatedStr ?? cd.RawValue ?? "") : "");
                    sb.Append("|");
                }
                string rowSig = sb.ToString();
                if (seenRows.Contains(rowSig))
                {
                    rowsToDelete.Add(r);
                }
                else
                {
                    seenRows.Add(rowSig);
                }
            }

            if (rowsToDelete.Count == 0)
            {
                if (parentForm != null) MessageBox.Show("Không tìm thấy giá trị trùng lặp nào trong vùng dữ liệu đã chọn!", "Xóa Trùng Lặp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            for (int i = rowsToDelete.Count - 1; i >= 0; i--)
            {
                CurrentSheet.ShiftCellsUp(rowsToDelete[i], 1, 1000, 100);
            }

            FormulaEngine.EvaluateSheet(CurrentSheet);
            if (parentForm != null) MessageBox.Show(string.Format("Đã tìm thấy và xóa {0} dòng trùng lặp!\nGiữ lại {1} dòng dữ liệu duy nhất.", rowsToDelete.Count, seenRows.Count + 1), "Xóa Trùng Lặp Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (parentForm != null) parentForm.Invalidate();
        }

        public void SetPrintArea()
        {
            HasPrintArea = true;
            PrintAreaStartRow = Math.Min(SelectedRow, SelEndRow);
            PrintAreaStartCol = Math.Min(SelectedCol, SelEndCol);
            PrintAreaEndRow = Math.Max(SelectedRow, SelEndRow);
            PrintAreaEndCol = Math.Max(SelectedCol, SelEndCol);
        }

        public void ClearPrintArea()
        {
            HasPrintArea = false;
        }

        public void ApplyTheme(int themeIdx)
        {
            ThemeIndex = themeIdx;
        }

        public void SetMargins(int marginPreset)
        {
            MarginPreset = marginPreset;
        }

        public void SetOrientation(bool isLandscape)
        {
            IsLandscape = isLandscape;
        }

        public void SetPaperSize(string size)
        {
            PaperSize = size;
        }
        #endregion
    }

    internal static class ColorExtensions
    {
        public static SolidBrush ToBrush(this Color color)
        {
            return new SolidBrush(color);
        }
    }
}

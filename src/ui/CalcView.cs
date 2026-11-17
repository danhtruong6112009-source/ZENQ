using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
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
        public int Indent { get; set; }
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
            Indent = 0;
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
        public List<DocShapeObject> FloatingObjects
        {
            get { return Shapes; }
            set { Shapes = value; }
        }
        public List<ExcelTableInfo> Tables { get; set; }
        public Color TabColor { get; set; }
        public float ZoomFactor { get; set; } = 1.0f;

        public CalcSheetState()
        {
            SelectedRow = 1;
            SelectedCol = 1;
            SelEndRow = 1;
            SelEndCol = 1;
            ScrollRow = 1;
            ScrollCol = 1;
            ZoomFactor = 1.0f;
            CustomColWidths = new Dictionary<uint, int>();
            CustomRowHeights = new Dictionary<uint, int>();
            Shapes = new List<DocShapeObject>();
            Tables = new List<ExcelTableInfo>();
            TabColor = Color.Empty;
        }
    }

    public class CalcView
    {
        public Action InvalidateCallback;
        public void Invalidate()
        {
            if (InvalidateCallback != null) InvalidateCallback();
        }
        public List<SparseMatrix> Sheets { get; set; }
        public List<string> SheetNames { get; set; }
        public List<CalcSheetState> SheetStates { get; set; }
        private int _activeSheetIndex;
        public int ActiveSheetIndex
        {
            get { return _activeSheetIndex; }
            set
            {
                if (value >= 0 && Sheets != null && value < Sheets.Count)
                {
                    if (_activeSheetIndex != value)
                    {
                        SwitchToSheet(value);
                    }
                    else
                    {
                        LoadSheetState(value);
                    }
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

        public List<CollabPresenceInfo> RemoteCollabPresences { get; set; } = new List<CollabPresenceInfo>();

        public void RefreshAfterRemoteEdit()
        {
            RecalcAfterEdit(false);
        }

        private void DrawRemoteCollabCellOverlays(D2DGraphics g, int headerW, int startY, int headerH)
        {
            if (RemoteCollabPresences == null || RemoteCollabPresences.Count == 0) return;
            using Font tagFont = new Font("Segoe UI", 7.8f, FontStyle.Bold);
            foreach (var p in RemoteCollabPresences)
            {
                if (p == null || p.IsStale || p.SlideIndex != ActiveSheetIndex || string.IsNullOrEmpty(p.SelectedId)) continue;
                var parts = p.SelectedId.Split(':');
                if (parts.Length < 3) continue;
                if (!uint.TryParse(parts[1], out uint r) || !uint.TryParse(parts[2], out uint c)) continue;
                if (r < ScrollRow || c < ScrollCol) continue;

                int rx = GetColScreenX(c, headerW);
                int ry = GetRowScreenY(r, startY, headerH);
                int rw = GetColWidth(c);
                int rh = GetRowHeight(r);
                if (rx < headerW || ry < startY + headerH || rw <= 0 || rh <= 0) continue;

                Color peerCol = Color.FromArgb(37, 99, 235);
                try { peerCol = ColorTranslator.FromHtml(p.ColorHex); } catch { }

                using Pen pPen = new Pen(peerCol, 2.0f);
                g.DrawRectangle(pPen, rx + 1, ry + 1, Math.Max(2, rw - 2), Math.Max(2, rh - 2));

                string label = "@" + (p.Username ?? "peer");
                SizeF sz = g.MeasureString(label, tagFont);
                float tagW = sz.Width + 10f;
                float tagH = 16f;
                float tagY = Math.Max(startY + 2, ry - tagH + 1);
                RectangleF tagRect = new RectangleF(rx, tagY, tagW, tagH);
                using GraphicsPath gp = CreateRoundedPath(new Rectangle((int)tagRect.X, (int)tagRect.Y, (int)Math.Ceiling(tagRect.Width), (int)tagRect.Height), 4);
                using SolidBrush bg = new SolidBrush(peerCol);
                g.FillPath(bg, gp);
                g.DrawString(label, tagFont, Brushes.White, tagRect.X + 5f, tagRect.Y + 1.5f);
            }
        }

        // Excel 365 Smooth Focus Cell Glide & Sheet Tab Indicator Animation
        private bool _selAnimInitialized = false;
        private float _selAnimStartX, _selAnimStartY, _selAnimStartW, _selAnimStartH;
        private float _selAnimCurX, _selAnimCurY, _selAnimCurW, _selAnimCurH;
        private int _selAnimTargetX, _selAnimTargetY, _selAnimTargetW, _selAnimTargetH;
        private uint _selAnimLastScrollR, _selAnimLastScrollC;
        private int _selAnimLastSheet = -1;
        private float _selAnimLastZoom = 1f;
        private double _selAnimStartMs = -10000.0;
        private const double CalcFocusGlideDurationMs = 115.0;

        private bool _sheetTabIndInit = false;
        private int _sheetTabLastIdx = -1;
        private float _sheetTabCurX, _sheetTabCurW, _sheetTabStartX, _sheetTabStartW, _sheetTabTargetX, _sheetTabTargetW;
        private double _sheetTabStartMs = -10000.0;
        private const double CalcSheetTabDurationMs = 165.0;

        public bool UpdateAnimations()
        {
            double now = FluentMotion.NowMs;
            bool active = false;
            if (_selAnimInitialized && (now - _selAnimStartMs) >= 0 && (now - _selAnimStartMs) < CalcFocusGlideDurationMs)
                active = true;
            if (_sheetTabIndInit && (now - _sheetTabStartMs) >= 0 && (now - _sheetTabStartMs) < CalcSheetTabDurationMs)
                active = true;
            return active;
        }

        public Dictionary<uint, int> CustomColWidths { get; set; }
        public Dictionary<uint, int> CustomRowHeights { get; set; }

        public int DefaultColWidth { get; set; }
        public int DefaultRowHeight { get; set; }

        public Form ParentForm { get; set; }

        private List<Tuple<uint, uint, CellData>> _clipboardCells = new List<Tuple<uint, uint, CellData>>();
        private uint _clipRowCount = 0;
        private uint _clipColCount = 0;
        private bool _clipboardIsCut = false;
        private SparseMatrix _clipboardSourceSheet = null;
        private uint _clipSourceStartRow = 0;
        private uint _clipSourceStartCol = 0;

        public const uint CalcMaxRows = 1000;
        public const uint CalcMaxCols = 250;

        public class CalcSelRange
        {
            public uint StartRow { get; set; }
            public uint StartCol { get; set; }
            public uint EndRow { get; set; }
            public uint EndCol { get; set; }

            public uint MinRow => Math.Min(StartRow, EndRow);
            public uint MaxRow => Math.Max(StartRow, EndRow);
            public uint MinCol => Math.Min(StartCol, EndCol);
            public uint MaxCol => Math.Max(StartCol, EndCol);

            public CalcSelRange(uint r1, uint c1, uint r2, uint c2)
            {
                StartRow = r1;
                StartCol = c1;
                EndRow = r2;
                EndCol = c2;
            }

            public bool Contains(uint r, uint c)
            {
                return r >= MinRow && r <= MaxRow && c >= MinCol && c <= MaxCol;
            }
        }

        public List<CalcSelRange> AdditionalSelections { get; } = new List<CalcSelRange>();
        public bool AutoCalculation { get; set; } = true;

        public bool IsEditing { get; set; }
        public string EditText { get; set; }
        public int EditCursorPos { get; set; }
        public int EditSelStart { get; set; }
        public int EditSelLength { get; set; }
        public int EditSelectionAnchor { get; set; }
        public bool IsDraggingTextSelection { get; set; }
        public Rectangle ActiveEditingCellRect { get; set; }

        // Formula Point Mode & Cross-Sheet Reference Entry
        public int FormulaSourceSheetIndex { get; private set; } = -1;
        public uint FormulaSourceRow { get; private set; } = 1;
        public uint FormulaSourceCol { get; private set; } = 1;
        public bool IsFormulaPointMode { get; set; }
        public bool IsSelectingPointRange { get; set; }
        private int _pointInsertStart = -1;
        private int _pointInsertLength = 0;
        private uint _pointAnchorRow = 1;
        private uint _pointAnchorCol = 1;
        private uint _pointCursorRow = 1;
        private uint _pointCursorCol = 1;

        // Name Box Inline Editor & Hitboxes
        public bool IsEditingNameBox { get; set; }
        public string NameBoxEditText { get; set; } = "";
        public int NameBoxCursorPos { get; set; } = 0;
        private Rectangle _lastNameBoxRect = new Rectangle(8, 158, 75, 23);
        private Rectangle _lastCancelBtnRect = new Rectangle(95, 158, 23, 23);
        private Rectangle _lastEnterBtnRect = new Rectangle(120, 158, 23, 23);
        private Rectangle _lastFxBtnRect = new Rectangle(145, 158, 26, 23);
        private Rectangle _lastFormulaInputRect = new Rectangle(177, 158, 400, 23);

        private static readonly Color[] FormulaRefPalette = new Color[]
        {
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(220, 38, 38),
            Color.FromArgb(147, 51, 234),
            Color.FromArgb(5, 150, 105),
            Color.FromArgb(217, 119, 6),
            Color.FromArgb(13, 148, 136)
        };

        private class FormulaRefHighlight
        {
            public uint MinRow;
            public uint MaxRow;
            public uint MinCol;
            public uint MaxCol;
            public Color RefColor;
        }

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
        private float _zoomFactor = 1.0f;
        public float ZoomFactor
        {
            get { return _zoomFactor > 0 ? _zoomFactor : 1.0f; }
            set { _zoomFactor = Math.Max(0.10f, Math.Min(5.0f, value)); }
        }
        public bool ShowGridlines { get; set; }
        public bool ShowHeadings { get; set; }
        public bool ShowFormulaBar { get; set; }
        public bool IsFormulaBarExpanded { get; set; }
        public Rectangle FormulaBarChevronBounds { get; set; }
        public Rectangle ActiveCellDropdownButtonBounds { get; set; }
        public bool IsHoveringCellDropdown { get; set; }
        public bool IsHoveringFormulaChevron { get; set; }

        public int FormulaBarHeight
        {
            get { return ShowFormulaBar ? (IsFormulaBarExpanded ? 84 : 30) : 0; }
        }

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
        public Rectangle FormulaSuggestionsBounds { get; set; }

        // Caret screen coordinates for word suggestion popup positioning
        public int CurrentCaretScreenX { get; set; }
        public int CurrentCaretScreenY { get; set; }
        public int CurrentCaretScreenH { get; set; }

        // Cached drawing resources for smooth high-performance lag-free rendering
        private static readonly Pen _gridLinePen = new Pen(Color.FromArgb(229, 231, 235), 1f);
        private static readonly Pen _headerBorderPen = new Pen(Color.FromArgb(226, 232, 240), 1f);
        private static readonly SolidBrush _headerBgBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
        private static readonly SolidBrush _headerSelBgBrush = new SolidBrush(Color.FromArgb(220, 252, 231));
        private static readonly SolidBrush _headerTextBrush = new SolidBrush(Color.FromArgb(71, 85, 105));
        private static readonly SolidBrush _headerSelTextBrush = new SolidBrush(Color.FromArgb(16, 124, 65));
        private static readonly Pen _headerSelBorderPen = new Pen(Color.FromArgb(16, 124, 65), 2f);
        private static readonly Pen _dividerPen = new Pen(Color.FromArgb(203, 213, 225), 1f);
        private static readonly Font _headerFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        private static readonly Font _headerSelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        private static readonly Font _headerRowFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font _headerRowSelFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        private static readonly StringFormat _sfCenterCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat _sfFarCenter = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat _sfNearCenter = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat _sfNearNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

        private static readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, SolidBrush> _brushCache = new Dictionary<int, SolidBrush>();
        private static readonly StringFormat[,,] _cachedStringFormats;

        static CalcView()
        {
            _cachedStringFormats = new StringFormat[3, 3, 2];
            for (int h = 0; h < 3; h++)
            {
                for (int v = 0; v < 3; v++)
                {
                    for (int w = 0; w < 2; w++)
                    {
                        StringFormat sf = new StringFormat();
                        if (h == 1) sf.Alignment = StringAlignment.Center;
                        else if (h == 2) sf.Alignment = StringAlignment.Far;
                        else sf.Alignment = StringAlignment.Near;

                        if (v == 0) sf.LineAlignment = StringAlignment.Near;
                        else if (v == 2) sf.LineAlignment = StringAlignment.Far;
                        else sf.LineAlignment = StringAlignment.Center;

                        if (w == 0)
                        {
                            sf.FormatFlags = StringFormatFlags.NoWrap;
                            sf.Trimming = StringTrimming.EllipsisCharacter;
                        }
                        _cachedStringFormats[h, v, w] = sf;
                    }
                }
            }
        }

        public static Font GetCellFont(string fontName, float size, FontStyle style)
        {
            string key = string.Format("{0}_{1}_{2}", fontName, (int)(size * 10), (int)style);
            Font f;
            if (!_fontCache.TryGetValue(key, out f))
            {
                try { f = new Font(fontName, size, style); }
                catch { f = new Font("Segoe UI", size, style); }
                _fontCache[key] = f;
            }
            return f;
        }

        public static SolidBrush GetCachedBrush(Color color)
        {
            int argb = color.ToArgb();
            if (argb == -1) return (SolidBrush)Brushes.White;
            if (argb == unchecked((int)0xFF000000)) return (SolidBrush)Brushes.Black;
            SolidBrush b;
            if (!_brushCache.TryGetValue(argb, out b))
            {
                b = new SolidBrush(color);
                _brushCache[argb] = b;
            }
            return b;
        }

        public static StringFormat GetCachedStringFormat(int hAlign, int vAlign, bool wrapText)
        {
            int h = (hAlign >= 0 && hAlign <= 2) ? hAlign : 0;
            int v = (vAlign >= 0 && vAlign <= 2) ? vAlign : 1;
            int w = wrapText ? 1 : 0;
            return _cachedStringFormats[h, v, w];
        }

        // Excel 365 Data Validation, Auditing, Scenarios & Names
        public List<DataValidationRule> DataValidations { get; set; } = new List<DataValidationRule>();
        public bool CircleInvalidData { get; set; } = false;
        public List<FormulaAuditingArrow> AuditingArrows { get; set; } = new List<FormulaAuditingArrow>();
        public bool ShowFormulas { get; set; } = false;
        public List<ScenarioModel> Scenarios { get; set; } = new List<ScenarioModel>();
        public List<DefinedNameModel> DefinedNames { get; set; } = new List<DefinedNameModel>();

        // Shapes & Pictures (Drawing Objects)
        public List<DocShapeObject> Shapes { get; set; }
        public List<DocShapeObject> FloatingObjects
        {
            get { return Shapes; }
            set { Shapes = value; }
        }
        public DocShapeObject SelectedShape { get; set; }
        public int SelectedShapeHandle { get; set; }
        public Point DragStartMouse { get; set; }
        public Point DragStartShapePos { get; set; }
        public Size DragStartShapeSize { get; set; }
        public Rectangle PlusBtnChartRect { get; set; }
        public Rectangle StyleBtnChartRect { get; set; }
        public Rectangle FilterBtnChartRect { get; set; }
        private CalcFindReplaceDialog _findReplaceDlg;

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

        // Drag & Drop Cell / Range Move & Copy
        public bool IsDraggingCellMove { get; set; }
        public uint MoveSourceStartRow { get; set; }
        public uint MoveSourceEndRow { get; set; }
        public uint MoveSourceStartCol { get; set; }
        public uint MoveSourceEndCol { get; set; }
        public uint MoveTargetRow { get; set; }
        public uint MoveTargetCol { get; set; }
        public bool IsCopyMode { get; set; }

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
            FormulaEngine.SetWorkbookContext(Sheets, SheetNames);
        }

        private void InitFormulas()
        {
            AllFormulas = new List<FormulaSuggestion>
            {
                // Toán học & Thống kê cơ bản
                new FormulaSuggestion("SUM", "=SUM(number1, [number2], ...)", "Tính tổng tất cả các số trong một dải ô được chọn."),
                new FormulaSuggestion("AVERAGE", "=AVERAGE(number1, [number2], ...)", "Tính giá trị trung bình cộng của các đối số."),
                new FormulaSuggestion("COUNT", "=COUNT(value1, [value2], ...)", "Đếm số lượng ô có chứa giá trị số."),
                new FormulaSuggestion("COUNTA", "=COUNTA(value1, [value2], ...)", "Đếm số lượng ô không trống trong dải ô."),
                new FormulaSuggestion("COUNTBLANK", "=COUNTBLANK(range)", "Đếm số lượng ô trống trong dải ô."),
                new FormulaSuggestion("MAX", "=MAX(number1, [number2], ...)", "Trả về giá trị lớn nhất trong tập hợp dữ liệu."),
                new FormulaSuggestion("MIN", "=MIN(number1, [number2], ...)", "Trả về giá trị nhỏ nhất trong tập hợp dữ liệu."),
                new FormulaSuggestion("PRODUCT", "=PRODUCT(number1, [number2], ...)", "Nhân tất cả các số được đưa ra làm đối số."),
                new FormulaSuggestion("SUMPRODUCT", "=SUMPRODUCT(array1, [array2], ...)", "Nhân các thành phần tương ứng trong mảng và trả về tổng."),
                new FormulaSuggestion("SUBTOTAL", "=SUBTOTAL(function_num, ref1, ...)", "Trả về tổng phụ trong danh sách hoặc cơ sở dữ liệu (1-11 hoặc 101-111)."),
                new FormulaSuggestion("AGGREGATE", "=AGGREGATE(function_num, options, ref1, ...)", "Tính toán tổng hợp (SUM, AVERAGE, LARGE, SMALL...) bỏ qua lỗi/hàng ẩn."),
                // Thống kê có điều kiện (1 & nhiều điều kiện)
                new FormulaSuggestion("SUMIF", "=SUMIF(range, criteria, [sum_range])", "Tính tổng các ô thỏa mãn một điều kiện."),
                new FormulaSuggestion("SUMIFS", "=SUMIFS(sum_range, criteria_range1, criteria1, ...)", "Tính tổng các ô thỏa mãn nhiều điều kiện chuẩn Excel."),
                new FormulaSuggestion("COUNTIF", "=COUNTIF(range, criteria)", "Đếm số ô thỏa mãn một điều kiện."),
                new FormulaSuggestion("COUNTIFS", "=COUNTIFS(criteria_range1, criteria1, ...)", "Đếm số ô thỏa mãn nhiều điều kiện chuẩn Excel."),
                new FormulaSuggestion("AVERAGEIF", "=AVERAGEIF(range, criteria, [average_range])", "Tính trung bình cộng các ô thỏa mãn một điều kiện."),
                new FormulaSuggestion("AVERAGEIFS", "=AVERAGEIFS(average_range, criteria_range1, criteria1, ...)", "Tính trung bình cộng các ô thỏa mãn nhiều điều kiện."),
                new FormulaSuggestion("MAXIFS", "=MAXIFS(max_range, criteria_range1, criteria1, ...)", "Trả về giá trị lớn nhất trong các ô thỏa mãn nhiều điều kiện."),
                new FormulaSuggestion("MINIFS", "=MINIFS(min_range, criteria_range1, criteria1, ...)", "Trả về giá trị nhỏ nhất trong các ô thỏa mãn nhiều điều kiện."),
                // Thống kê nâng cao
                new FormulaSuggestion("MEDIAN", "=MEDIAN(number1, [number2], ...)", "Trả về số trung vị của các số đã cho."),
                new FormulaSuggestion("MODE", "=MODE(number1, [number2], ...)", "Trả về giá trị xuất hiện nhiều nhất trong tập dữ liệu."),
                new FormulaSuggestion("STDEV", "=STDEV(number1, [number2], ...)", "Ước tính độ lệch chuẩn dựa trên mẫu."),
                new FormulaSuggestion("STDEV.P", "=STDEV.P(number1, [number2], ...)", "Tính độ lệch chuẩn trên toàn bộ quần thể."),
                new FormulaSuggestion("VAR", "=VAR(number1, [number2], ...)", "Ước tính phương sai dựa trên mẫu."),
                new FormulaSuggestion("VAR.P", "=VAR.P(number1, [number2], ...)", "Tính phương sai trên toàn bộ quần thể."),
                new FormulaSuggestion("LARGE", "=LARGE(array, k)", "Trả về giá trị lớn thứ k trong tập dữ liệu."),
                new FormulaSuggestion("SMALL", "=SMALL(array, k)", "Trả về giá trị nhỏ thứ k trong tập dữ liệu."),
                new FormulaSuggestion("RANK", "=RANK(number, ref, [order])", "Trả về thứ hạng của một số trong danh sách."),
                new FormulaSuggestion("PERCENTILE", "=PERCENTILE(array, k)", "Trả về phân vị thứ k của các giá trị trong dải ô."),
                new FormulaSuggestion("QUARTILE", "=QUARTILE(array, quart)", "Trả về tứ phân vị của tập dữ liệu."),
                new FormulaSuggestion("CORREL", "=CORREL(array1, array2)", "Trả về hệ số tương quan giữa hai tập dữ liệu."),
                new FormulaSuggestion("FORECAST", "=FORECAST(x, known_y's, known_x's)", "Dự báo giá trị tương lai bằng hồi quy tuyến tính."),
                new FormulaSuggestion("SLOPE", "=SLOPE(known_y's, known_x's)", "Trả về độ dốc của đường hồi quy tuyến tính."),
                new FormulaSuggestion("INTERCEPT", "=INTERCEPT(known_y's, known_x's)", "Trả về giao điểm trục tung của đường hồi quy tuyến tính."),
                // Làm tròn & Toán học
                new FormulaSuggestion("ROUND", "=ROUND(number, num_digits)", "Làm tròn một số đến số chữ số thập phân đã định."),
                new FormulaSuggestion("ROUNDUP", "=ROUNDUP(number, num_digits)", "Làm tròn số ra xa số 0 (lên)."),
                new FormulaSuggestion("ROUNDDOWN", "=ROUNDDOWN(number, num_digits)", "Làm tròn số về gần số 0 (xuống)."),
                new FormulaSuggestion("MROUND", "=MROUND(number, multiple)", "Làm tròn số đến bội số gần nhất."),
                new FormulaSuggestion("CEILING", "=CEILING(number, significance)", "Làm tròn số lên đến bội số nguyên gần nhất."),
                new FormulaSuggestion("FLOOR", "=FLOOR(number, significance)", "Làm tròn số xuống đến bội số nguyên gần nhất."),
                new FormulaSuggestion("INT", "=INT(number)", "Làm tròn số xuống tới số nguyên gần nhất."),
                new FormulaSuggestion("TRUNC", "=TRUNC(number, [num_digits])", "Cắt cụt một số thành số nguyên hoặc số chữ số thập phân."),
                new FormulaSuggestion("ABS", "=ABS(number)", "Trả về giá trị tuyệt đối của một số."),
                new FormulaSuggestion("MOD", "=MOD(number, divisor)", "Trả về phần dư sau khi chia."),
                new FormulaSuggestion("QUOTIENT", "=QUOTIENT(numerator, denominator)", "Trả về phần nguyên của phép chia."),
                new FormulaSuggestion("POWER", "=POWER(number, power)", "Trả về kết quả của một số nâng lên lũy thừa."),
                new FormulaSuggestion("SQRT", "=SQRT(number)", "Trả về căn bậc hai của một số."),
                new FormulaSuggestion("EXP", "=EXP(number)", "Trả về cơ số e nâng lên lũy thừa của số đã cho."),
                new FormulaSuggestion("LN", "=LN(number)", "Trả về lô-ga-rít tự nhiên của một số."),
                new FormulaSuggestion("LOG", "=LOG(number, [base])", "Trả về lô-ga-rít của một số theo cơ số chỉ định."),
                new FormulaSuggestion("LOG10", "=LOG10(number)", "Trả về lô-ga-rít cơ số 10 của một số."),
                new FormulaSuggestion("FACT", "=FACT(number)", "Trả về giai thừa của một số."),
                new FormulaSuggestion("COMBIN", "=COMBIN(number, number_chosen)", "Trả về số tổ hợp chập k của n phần tử."),
                new FormulaSuggestion("PERMUT", "=PERMUT(number, number_chosen)", "Trả về số chỉnh hợp chập k của n phần tử."),
                new FormulaSuggestion("GCD", "=GCD(number1, [number2], ...)", "Trả về ước số chung lớn nhất."),
                new FormulaSuggestion("LCM", "=LCM(number1, [number2], ...)", "Trả về bội số chung nhỏ nhất."),
                new FormulaSuggestion("RAND", "=RAND()", "Trả về một số thực ngẫu nhiên từ 0 đến 1."),
                new FormulaSuggestion("RANDBETWEEN", "=RANDBETWEEN(bottom, top)", "Trả về số nguyên ngẫu nhiên nằm giữa hai số chỉ định."),
                new FormulaSuggestion("PI", "=PI()", "Trả về hằng số Pi (3.14159265358979)."),
                new FormulaSuggestion("SIN", "=SIN(number)", "Trả về sin của một góc (radian)."),
                new FormulaSuggestion("COS", "=COS(number)", "Trả về cosin của một góc (radian)."),
                new FormulaSuggestion("TAN", "=TAN(number)", "Trả về tang của một góc (radian)."),
                new FormulaSuggestion("RADIANS", "=RADIANS(angle)", "Chuyển đổi độ sang radian."),
                new FormulaSuggestion("DEGREES", "=DEGREES(angle)", "Chuyển đổi radian sang độ."),
                // Logic & Kiểm tra thông tin
                new FormulaSuggestion("IF", "=IF(logical_test, value_if_true, value_if_false)", "Kiểm tra điều kiện logic và trả về giá trị tương ứng."),
                new FormulaSuggestion("IFS", "=IFS(logical_test1, value1, ...)", "Kiểm tra nhiều điều kiện và trả về giá trị của điều kiện đúng đầu tiên."),
                new FormulaSuggestion("SWITCH", "=SWITCH(expression, val1, result1, ..., [default])", "So khớp biểu thức với danh sách giá trị và trả về kết quả tương ứng."),
                new FormulaSuggestion("AND", "=AND(logical1, [logical2], ...)", "Trả về TRUE nếu tất cả các đối số đều đúng."),
                new FormulaSuggestion("OR", "=OR(logical1, [logical2], ...)", "Trả về TRUE nếu bất kỳ đối số nào đúng."),
                new FormulaSuggestion("XOR", "=XOR(logical1, [logical2], ...)", "Trả về phép OR loại trừ của tất cả các đối số."),
                new FormulaSuggestion("NOT", "=NOT(logical)", "Đảo ngược giá trị logic của đối số."),
                new FormulaSuggestion("IFERROR", "=IFERROR(value, value_if_error)", "Trả về giá trị thay thế nếu công thức bị lỗi."),
                new FormulaSuggestion("IFNA", "=IFNA(value, value_if_na)", "Trả về giá trị thay thế nếu biểu thức trả về lỗi #N/A."),
                new FormulaSuggestion("ISBLANK", "=ISBLANK(value)", "Kiểm tra ô có trống hay không."),
                new FormulaSuggestion("ISNUMBER", "=ISNUMBER(value)", "Kiểm tra giá trị có phải là số hay không."),
                new FormulaSuggestion("ISTEXT", "=ISTEXT(value)", "Kiểm tra giá trị có phải là văn bản hay không."),
                new FormulaSuggestion("ISERROR", "=ISERROR(value)", "Kiểm tra giá trị có phải là lỗi bất kỳ hay không."),
                new FormulaSuggestion("ISNA", "=ISNA(value)", "Kiểm tra giá trị có phải là lỗi #N/A hay không."),
                new FormulaSuggestion("ISFORMULA", "=ISFORMULA(reference)", "Kiểm tra ô tham chiếu có chứa công thức hay không."),
                new FormulaSuggestion("ISEVEN", "=ISEVEN(number)", "Trả về TRUE nếu số chẵn."),
                new FormulaSuggestion("ISODD", "=ISODD(number)", "Trả về TRUE nếu số lẻ."),
                // Dò tìm & Tham chiếu
                new FormulaSuggestion("VLOOKUP", "=VLOOKUP(lookup_value, table_array, col_index, [range_lookup])", "Tìm kiếm một giá trị trong cột đầu tiên của bảng."),
                new FormulaSuggestion("HLOOKUP", "=HLOOKUP(lookup_value, table_array, row_index, [range_lookup])", "Tìm kiếm một giá trị trong hàng đầu tiên của bảng."),
                new FormulaSuggestion("XLOOKUP", "=XLOOKUP(lookup_value, lookup_array, return_array, [if_not_found])", "Tìm kiếm hiện đại theo dải ô hoặc mảng."),
                new FormulaSuggestion("LOOKUP", "=LOOKUP(lookup_value, lookup_vector, [result_vector])", "Tìm kiếm một giá trị trong một véc-tơ hoặc mảng."),
                new FormulaSuggestion("INDEX", "=INDEX(array, row_num, [column_num])", "Trả về giá trị tại giao điểm của hàng và cột trong mảng."),
                new FormulaSuggestion("MATCH", "=MATCH(lookup_value, lookup_array, [match_type])", "Trả về vị trí tương đối của một mục trong dải ô."),
                new FormulaSuggestion("CHOOSE", "=CHOOSE(index_num, value1, [value2], ...)", "Chọn một giá trị từ danh sách dựa trên số chỉ mục."),
                new FormulaSuggestion("INDIRECT", "=INDIRECT(ref_text)", "Trả về tham chiếu được chỉ định bởi một chuỗi văn bản."),
                new FormulaSuggestion("OFFSET", "=OFFSET(reference, rows, cols)", "Trả về giá trị ô dịch chuyển từ ô tham chiếu."),
                new FormulaSuggestion("ROW", "=ROW([reference])", "Trả về số thứ tự hàng của một tham chiếu."),
                new FormulaSuggestion("COLUMN", "=COLUMN([reference])", "Trả về số thứ tự cột của một tham chiếu."),
                new FormulaSuggestion("ROWS", "=ROWS(array)", "Trả về số lượng hàng trong một tham chiếu."),
                new FormulaSuggestion("COLUMNS", "=COLUMNS(array)", "Trả về số lượng cột trong một tham chiếu."),
                new FormulaSuggestion("ADDRESS", "=ADDRESS(row_num, column_num, [abs_num])", "Tạo địa chỉ ô dưới dạng văn bản từ số hàng và cột."),
                new FormulaSuggestion("FORMULATEXT", "=FORMULATEXT(reference)", "Trả về công thức dưới dạng chuỗi văn bản."),
                // Văn bản (Text)
                new FormulaSuggestion("CONCATENATE", "=CONCATENATE(text1, [text2], ...)", "Nối nhiều chuỗi văn bản lại thành một chuỗi duy nhất."),
                new FormulaSuggestion("CONCAT", "=CONCAT(text1, [text2], ...)", "Nối văn bản từ nhiều chuỗi hoặc dải ô."),
                new FormulaSuggestion("TEXTJOIN", "=TEXTJOIN(delimiter, ignore_empty, text1, ...)", "Nối nhiều chuỗi với ký tự phân cách chỉ định."),
                new FormulaSuggestion("LEFT", "=LEFT(text, [num_chars])", "Trích xuất các ký tự đầu tiên bên trái chuỗi."),
                new FormulaSuggestion("RIGHT", "=RIGHT(text, [num_chars])", "Trích xuất các ký tự cuối cùng bên phải chuỗi."),
                new FormulaSuggestion("MID", "=MID(text, start_num, num_chars)", "Trích xuất chuỗi con từ vị trí chỉ định."),
                new FormulaSuggestion("LEN", "=LEN(text)", "Trả về độ dài (số ký tự) của chuỗi."),
                new FormulaSuggestion("TRIM", "=TRIM(text)", "Loại bỏ khoảng trắng thừa khỏi chuỗi văn bản."),
                new FormulaSuggestion("CLEAN", "=CLEAN(text)", "Loại bỏ tất cả các ký tự không in được khỏi văn bản."),
                new FormulaSuggestion("UPPER", "=UPPER(text)", "Chuyển đổi văn bản sang chữ IN HOA."),
                new FormulaSuggestion("LOWER", "=LOWER(text)", "Chuyển đổi văn bản sang chữ thường."),
                new FormulaSuggestion("PROPER", "=PROPER(text)", "Viết hoa chữ cái đầu tiên của mỗi từ."),
                new FormulaSuggestion("SUBSTITUTE", "=SUBSTITUTE(text, old_text, new_text, [instance_num])", "Thay thế văn bản cũ bằng văn bản mới trong chuỗi."),
                new FormulaSuggestion("REPLACE", "=REPLACE(old_text, start_num, num_chars, new_text)", "Thay thế một phần của chuỗi văn bản dựa trên vị trí và số ký tự."),
                new FormulaSuggestion("FIND", "=FIND(find_text, within_text, [start_num])", "Tìm vị trí chuỗi con (phân biệt chữ hoa/thường)."),
                new FormulaSuggestion("SEARCH", "=SEARCH(find_text, within_text, [start_num])", "Tìm vị trí chuỗi con (không phân biệt hoa/thường, hỗ trợ * ?)."),
                new FormulaSuggestion("EXACT", "=EXACT(text1, text2)", "So sánh hai chuỗi có giống hệt nhau không."),
                new FormulaSuggestion("REPT", "=REPT(text, number_times)", "Lặp lại văn bản một số lần nhất định."),
                new FormulaSuggestion("VALUE", "=VALUE(text)", "Chuyển đổi chuỗi văn bản đại diện cho số thành số."),
                new FormulaSuggestion("TEXT", "=TEXT(value, format_text)", "Định dạng một số và chuyển đổi thành văn bản."),
                new FormulaSuggestion("FIXED", "=FIXED(number, [decimals], [no_commas])", "Làm tròn số và định dạng dưới dạng văn bản có dấu phẩy."),
                new FormulaSuggestion("CHAR", "=CHAR(number)", "Trả về ký tự theo mã số ASCII/ANSI."),
                new FormulaSuggestion("CODE", "=CODE(text)", "Trả về mã số của ký tự đầu tiên trong chuỗi."),
                // Ngày & Giờ (Date & Time)
                new FormulaSuggestion("TODAY", "=TODAY()", "Trả về ngày tháng hiện tại của hệ thống."),
                new FormulaSuggestion("NOW", "=NOW()", "Trả về ngày tháng và thời gian hiện tại của hệ thống."),
                new FormulaSuggestion("DATE", "=DATE(year, month, day)", "Tạo ngày hợp lệ từ Năm, Tháng, Ngày."),
                new FormulaSuggestion("TIME", "=TIME(hour, minute, second)", "Tạo thời gian hợp lệ từ Giờ, Phút, Giây."),
                new FormulaSuggestion("YEAR", "=YEAR(serial_number)", "Trả về năm của một ngày."),
                new FormulaSuggestion("MONTH", "=MONTH(serial_number)", "Trả về tháng của một ngày (1-12)."),
                new FormulaSuggestion("DAY", "=DAY(serial_number)", "Trả về ngày trong tháng (1-31)."),
                new FormulaSuggestion("HOUR", "=HOUR(serial_number)", "Trả về giờ (0-23)."),
                new FormulaSuggestion("MINUTE", "=MINUTE(serial_number)", "Trả về phút (0-59)."),
                new FormulaSuggestion("SECOND", "=SECOND(serial_number)", "Trả về giây (0-59)."),
                new FormulaSuggestion("WEEKDAY", "=WEEKDAY(serial_number, [return_type])", "Trả về thứ trong tuần của một ngày."),
                new FormulaSuggestion("WEEKNUM", "=WEEKNUM(serial_number, [return_type])", "Trả về số tuần trong năm."),
                new FormulaSuggestion("DATEDIF", "=DATEDIF(start_date, end_date, unit)", "Tính số ngày, tháng hoặc năm giữa hai ngày."),
                new FormulaSuggestion("DAYS", "=DAYS(end_date, start_date)", "Trả về số ngày giữa hai ngày."),
                new FormulaSuggestion("DAYS360", "=DAYS360(start_date, end_date, [method])", "Tính số ngày giữa hai ngày dựa trên năm 360 ngày."),
                new FormulaSuggestion("EDATE", "=EDATE(start_date, months)", "Trả về ngày cách ngày bắt đầu một số tháng."),
                new FormulaSuggestion("EOMONTH", "=EOMONTH(start_date, months)", "Trả về ngày cuối cùng của tháng trước hoặc sau một số tháng."),
                new FormulaSuggestion("WORKDAY", "=WORKDAY(start_date, days, [holidays])", "Trả về ngày làm việc trước hoặc sau một số ngày làm việc."),
                new FormulaSuggestion("NETWORKDAYS", "=NETWORKDAYS(start_date, end_date)", "Trả về số ngày làm việc trọn vẹn giữa hai ngày."),
                new FormulaSuggestion("YEARFRAC", "=YEARFRAC(start_date, end_date, [basis])", "Tính tỷ lệ năm giữa hai ngày."),
                // Tài chính (Financial)
                new FormulaSuggestion("PMT", "=PMT(rate, nper, pv, [fv], [type])", "Tính khoản thanh toán định kỳ cho khoản vay với lãi suất không đổi."),
                new FormulaSuggestion("IPMT", "=IPMT(rate, per, nper, pv, [fv], [type])", "Tính tiền lãi phải trả trong một kỳ của khoản đầu tư/vay."),
                new FormulaSuggestion("PPMT", "=PPMT(rate, per, nper, pv, [fv], [type])", "Tính khoản thanh toán vốn gốc trong một kỳ của khoản vay."),
                new FormulaSuggestion("PV", "=PV(rate, nper, pmt, [fv], [type])", "Tính giá trị hiện tại (Present Value) của khoản đầu tư."),
                new FormulaSuggestion("FV", "=FV(rate, nper, pmt, [pv], [type])", "Tính giá trị tương lai (Future Value) của khoản đầu tư."),
                new FormulaSuggestion("NPV", "=NPV(rate, value1, [value2], ...)", "Tính giá trị hiện tại ròng (Net Present Value) của dòng tiền."),
                new FormulaSuggestion("IRR", "=IRR(values, [guess])", "Tính tỷ suất hoàn vốn nội bộ (Internal Rate of Return)."),
                new FormulaSuggestion("RATE", "=RATE(nper, pmt, pv, [fv], [type], [guess])", "Tính lãi suất mỗi kỳ của một niên kim."),
                new FormulaSuggestion("NPER", "=NPER(rate, pmt, pv, [fv], [type])", "Tính tổng số kỳ thanh toán của một khoản vay/đầu tư."),
                new FormulaSuggestion("SLN", "=SLN(cost, salvage, life)", "Tính khấu hao đường thẳng của tài sản cho một kỳ."),
                new FormulaSuggestion("SYD", "=SYD(cost, salvage, life, per)", "Tính khấu hao theo tổng số năm sử dụng của tài sản."),
                new FormulaSuggestion("DDB", "=DDB(cost, salvage, life, period, [factor])", "Tính khấu hao theo số dư giảm dần kép."),
                new FormulaSuggestion("EFFECT", "=EFFECT(nominal_rate, npery)", "Tính lãi suất thực tế hàng năm."),
                new FormulaSuggestion("NOMINAL", "=NOMINAL(effect_rate, npery)", "Tính lãi suất danh nghĩa hàng năm.")
            };
            ActiveSuggestions = new List<FormulaSuggestion>();
            SelectedSuggestionIndex = 0;
        }

        public void SwitchToSheet(int newIndex)
        {
            if (newIndex >= 0 && newIndex < Sheets.Count)
            {
                if (IsEditing && !string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("="))
                {
                    if (FormulaSourceSheetIndex < 0)
                    {
                        FormulaSourceSheetIndex = _activeSheetIndex;
                        FormulaSourceRow = SelectedRow;
                        FormulaSourceCol = SelectedCol;
                    }
                    SaveCurrentSheetState();
                    _activeSheetIndex = newIndex;
                    LoadSheetState(newIndex);
                    IsFormulaPointMode = true;
                    _pointInsertStart = -1;
                    _pointInsertLength = 0;
                    FilterFormulaSuggestions();
                    return;
                }

                ClearAdditionalSelections();
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
                st.CustomColWidths = CustomColWidths != null ? new Dictionary<uint, int>(CustomColWidths) : new Dictionary<uint, int>();
                st.CustomRowHeights = CustomRowHeights != null ? new Dictionary<uint, int>(CustomRowHeights) : new Dictionary<uint, int>();
                st.Shapes = Shapes != null ? new List<DocShapeObject>(Shapes) : new List<DocShapeObject>();
                st.Tables = Tables != null ? new List<ExcelTableInfo>(Tables) : new List<ExcelTableInfo>();
                st.ZoomFactor = ZoomFactor;
            }
        }

        public void LoadSheetState(int sheetIndex)
        {
            FormulaEngine.SetWorkbookContext(Sheets, SheetNames);
            if (SheetStates == null) SheetStates = new List<CalcSheetState>();
            while (SheetStates.Count < Sheets.Count)
            {
                SheetStates.Add(new CalcSheetState());
            }

            if (sheetIndex >= 0 && sheetIndex < SheetStates.Count)
            {
                _activeSheetIndex = sheetIndex;
                var st = SheetStates[sheetIndex];
                SelectedRow = Math.Max(1, st.SelectedRow);
                SelectedCol = Math.Max(1, st.SelectedCol);
                SelEndRow = Math.Max(1, st.SelEndRow);
                SelEndCol = Math.Max(1, st.SelEndCol);
                ScrollRow = Math.Max(1, st.ScrollRow);
                ScrollCol = Math.Max(1, st.ScrollCol);
                ZoomFactor = st.ZoomFactor > 0 ? st.ZoomFactor : 1.0f;
                CustomColWidths = st.CustomColWidths != null ? new Dictionary<uint, int>(st.CustomColWidths) : new Dictionary<uint, int>();
                CustomRowHeights = st.CustomRowHeights != null ? new Dictionary<uint, int>(st.CustomRowHeights) : new Dictionary<uint, int>();
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
                    cloneState.ZoomFactor = origSt.ZoomFactor;
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
                prompt.Text = AppL10n.T("Đổi Tên Sheet", "Rename Sheet");
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 15, Text = AppL10n.T("Tên mới cho trang tính:", "New sheet name:") };
                TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 300, Text = SheetNames[sheetIndex] };
                Button confirmation = new Button() { Text = "OK", Left = 160, Width = 75, Top = 75, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = AppL10n.T("Hủy", "Cancel"), Left = 245, Width = 75, Top = 75, DialogResult = DialogResult.Cancel };
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
            ContextMenuStrip cms = new ZenQOffice.UI.FluentContextMenuStrip();
            cms.Font = new Font("Segoe UI", 9f);

            cms.Items.Add(AppL10n.T("✏️ Đổi tên Sheet (Rename)...", "✏️ Rename Sheet..."), null, (s, e) =>
            {
                PromptRenameSheet(sheetIndex, parentControl);
            });

            cms.Items.Add(AppL10n.T("➕ Chèn Sheet mới (Insert Sheet)", "➕ Insert Sheet"), null, (s, e) =>
            {
                AddSheet("Sheet" + (SheetNames.Count + 1));
                SwitchToSheet(SheetNames.Count - 1);
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Items.Add(AppL10n.T("📋 Nhân bản Sheet (Duplicate Sheet)", "📋 Duplicate Sheet"), null, (s, e) =>
            {
                DuplicateSheet(sheetIndex);
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Items.Add(new ToolStripSeparator());

            var delItem = cms.Items.Add(AppL10n.T("🗑️ Xóa Sheet (Delete Sheet)", "🗑️ Delete Sheet"), null, (s, e) =>
            {
                if (Sheets.Count <= 1)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Bảng tính phải có ít nhất 1 sheet!", "A workbook must contain at least one visible sheet!"), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T(string.Format("Bạn có chắc chắn muốn xóa sheet \"{0}\"?", SheetNames[sheetIndex]), string.Format("Are you sure you want to delete sheet \"{0}\"?", SheetNames[sheetIndex])), AppL10n.T("Xác nhận xóa", "Confirm Delete"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DeleteSheet(sheetIndex);
                    if (parentControl != null) parentControl.Invalidate();
                }
            });
            delItem.Enabled = (Sheets.Count > 1);

            cms.Items.Add(new ToolStripSeparator());

            var moveLeftItem = cms.Items.Add(AppL10n.T("⬅️ Di chuyển sang trái", "⬅️ Move Left"), null, (s, e) =>
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

            var moveRightItem = cms.Items.Add(AppL10n.T("➡️ Di chuyển sang phải", "➡️ Move Right"), null, (s, e) =>
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
                    RecalcAfterEdit();
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
                    RecalcAfterEdit();
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
            sheet.SetCellText(1, 1, "BÁO CÁO DOANH THU & LỢI NHUẬN ZENQ OFFICE Q3/2026");
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
            sheet.SetCellText(4, 2, "ZENQ OFFICE Suite Pro 2026");
            sheet.SetCellText(4, 3, "250");
            sheet.SetCellText(4, 4, "120");
            sheet.SetCellText(4, 5, "=C4*D4");

            sheet.SetCellText(5, 1, "SP-002");
            sheet.SetCellText(5, 2, "ZenQ Writer Cloud Enterprise");
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

        public int GetRawColWidth(uint col)
        {
            if (CustomColWidths != null && CustomColWidths.ContainsKey(col)) return CustomColWidths[col];
            return DefaultColWidth;
        }

        public int GetColWidth(uint col)
        {
            float z = ZoomFactor > 0 ? ZoomFactor : 1.0f;
            return Math.Max(1, (int)Math.Round(GetRawColWidth(col) * z));
        }

        public int GetRawRowHeight(uint row)
        {
            if (CustomRowHeights != null && CustomRowHeights.ContainsKey(row)) return CustomRowHeights[row];
            return AppSettings.Instance.TouchModeEnabled ? (DefaultRowHeight + 6) : DefaultRowHeight;
        }

        public int GetRowHeight(uint row)
        {
            float z = ZoomFactor > 0 ? ZoomFactor : 1.0f;
            return Math.Max(1, (int)Math.Round(GetRawRowHeight(row) * z));
        }

        public int GetColX(uint col)
        {
            int headerW = ShowHeadings ? 48 : 0;
            int x = headerW;
            if (col >= ScrollCol)
            {
                for (uint c = ScrollCol; c < col; c++) x += GetColWidth(c);
            }
            else
            {
                for (uint c = col; c < ScrollCol; c++) x -= GetColWidth(c);
            }
            return x;
        }

        public int GetRowY(uint row)
        {
            int fBarY = 155;
            int startY = ShowFormulaBar ? (fBarY + FormulaBarHeight) : fBarY;
            int headerH = ShowHeadings ? 26 : 0;
            int y = startY + headerH;
            if (row >= ScrollRow)
            {
                for (uint r = ScrollRow; r < row; r++) y += GetRowHeight(r);
            }
            else
            {
                for (uint r = row; r < ScrollRow; r++) y -= GetRowHeight(r);
            }
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

            if (initialText == null && AppSettings.Instance.TouchModeEnabled && AppSettings.Instance.AutoSummonTouchKeyboard)
            {
                PeripheralDeviceManager.ToggleTouchKeyboard();
            }
        }

        public void CommitCellEdit()
        {
            CommitEditAndMove(0, 0);
        }

        public void Draw(Graphics g, int width, int height)
        {
            using (D2DGraphics d2d = new D2DGraphics(g, width, height))
            {
                Draw(d2d, width, height);
            }
        }

        public void Draw(D2DGraphics g, int width, int height)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int fBarY = 155;
            int fBarH = IsFormulaBarExpanded ? 84 : 30;

            // Pre-fill entire Calc canvas background from formula bar to bottom to eliminate any unpainted D2D alpha=0 pixels
            g.FillRectangle(Brushes.White, 0, fBarY, width, Math.Max(0, height - fBarY));

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
                _lastNameBoxRect = nameBox;
                using (GraphicsPath nPath = CreateRoundedPath(nameBox, 3))
                {
                    g.FillPath(IsEditingNameBox ? Brushes.White : (IsHoveringNameBox ? Brushes.WhiteSmoke : Brushes.White), nPath);
                    using (Pen nPen = new Pen(IsEditingNameBox ? Color.FromArgb(16, 124, 65) : Color.FromArgb(203, 213, 225), IsEditingNameBox ? 1.5f : 1f))
                        g.DrawPath(nPen, nPath);
                }

                using (Font boldFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (Font symFont = new Font("Segoe UI", 8f, FontStyle.Regular))
                {
                    string addr = GetDisplayNameBoxText();
                    Region prevNbClip = g.Clip;
                    g.SetClip(new Rectangle(nameBox.X + 3, nameBox.Y + 2, nameBox.Width - 18, nameBox.Height - 4));
                    if (IsEditingNameBox)
                    {
                        string nbStr = NameBoxEditText ?? "";
                        g.DrawString(nbStr, boldFont, Brushes.Black, nameBox.X + 5, nameBox.Y + 4);
                        int cIdx = Math.Max(0, Math.Min(nbStr.Length, NameBoxCursorPos));
                        int cX = nameBox.X + 5 + (int)g.MeasureString(nbStr.Substring(0, cIdx), boldFont, 2000, StringFormat.GenericTypographic).Width;
                        g.DrawLine(Pens.Black, cX, nameBox.Y + 4, cX, nameBox.Bottom - 4);
                    }
                    else
                    {
                        g.DrawString(addr, boldFont, Brushes.Black, nameBox.X + 6, nameBox.Y + 4);
                    }
                    g.Clip = prevNbClip;
                    g.DrawString("▾", symFont, Brushes.DimGray, nameBox.Right - 14, nameBox.Y + 4);
                }

                // Vertical Divider
                g.DrawLine(Pens.LightGray, nameBox.Right + 6, fBarY + 5, nameBox.Right + 6, fBarY + 28);

                // 1.2 Action Buttons: Cancel (✕), Enter (✓), fx
                int btnStartX = nameBox.Right + 12;

                // Cancel Button ✕
                Rectangle cancelBox = new Rectangle(btnStartX, fBarY + 3, 23, 23);
                _lastCancelBtnRect = cancelBox;
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
                _lastEnterBtnRect = enterBox;
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
                _lastFxBtnRect = fxBox;
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
                g.DrawLine(Pens.LightGray, fxBox.Right + 6, fBarY + 5, fxBox.Right + 6, fBarY + 28);

                // 1.3 Formula Input Box & Expand/Collapse Chevron (Ctrl+Shift+U)
                int fChevronW = 24;
                int fInputX = fxBox.Right + 12;
                int fInputW = width - fInputX - 10 - fChevronW;
                int fInputH = IsFormulaBarExpanded ? (fBarH - 8) : 23;
                Rectangle fBox = new Rectangle(fInputX, fBarY + 3, fInputW, fInputH);
                _lastFormulaInputRect = fBox;
                using (GraphicsPath fPath = CreateRoundedPath(fBox, 3))
                {
                    g.FillPath(Brushes.White, fPath);
                    using (Pen fPen = new Pen(IsEditing ? Color.FromArgb(16, 124, 65) : Color.FromArgb(203, 213, 225), IsEditing ? 1.5f : 1f))
                    {
                        g.DrawPath(fPen, fPath);
                    }
                }

                // Expand/Collapse Chevron Button on right of Formula Bar
                Rectangle chevBox = new Rectangle(width - 10 - fChevronW + 2, fBarY + 3, fChevronW - 2, 23);
                FormulaBarChevronBounds = chevBox;
                using (GraphicsPath cPath = CreateRoundedPath(chevBox, 3))
                {
                    g.FillPath(IsHoveringFormulaChevron ? Brushes.WhiteSmoke : Brushes.White, cPath);
                    using (Pen cPen = new Pen(Color.FromArgb(203, 213, 225), 1f)) g.DrawPath(cPen, cPath);
                }
                using (Pen cPen = new Pen(Color.FromArgb(100, 116, 139), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    float cx = chevBox.X + chevBox.Width / 2f;
                    float cy = chevBox.Y + chevBox.Height / 2f;
                    if (IsFormulaBarExpanded)
                    {
                        // Up arrow ▴
                        g.DrawLine(cPen, cx - 4, cy + 2, cx, cy - 2);
                        g.DrawLine(cPen, cx, cy - 2, cx + 4, cy + 2);
                    }
                    else
                    {
                        // Down arrow ▾
                        g.DrawLine(cPen, cx - 4, cy - 2, cx, cy + 2);
                        g.DrawLine(cPen, cx, cy + 2, cx + 4, cy - 2);
                    }
                }

                using (Font normFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                {
                    CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                    string fDisplay = IsEditing ? EditText : (cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "");
                    if (!IsEditing && cell != null && cell.IsSpilledChild)
                    {
                        CellData originCell = CurrentSheet.GetCell(cell.SpillOriginRow, cell.SpillOriginCol);
                        if (originCell != null && !string.IsNullOrEmpty(originCell.FormulaStr))
                        {
                            fDisplay = originCell.FormulaStr;
                        }
                    }
                    Rectangle fTextBounds = new Rectangle(fBox.X + 8, fBox.Y + 4, fBox.Width - 16, fBox.Height - 8);
                    StringFormat fBarSf = IsFormulaBarExpanded ? _sfNearNear : _sfNearCenter;
                    g.DrawString(fDisplay, normFont, Brushes.Black, fTextBounds, fBarSf);

                    if (IsEditing)
                    {
                        string subStr = fDisplay.Substring(0, Math.Min(fDisplay.Length, EditCursorPos));
                        SizeF txtSz = g.MeasureString(subStr, normFont);
                        float cX = fBox.X + 8 + txtSz.Width;
                        g.DrawLine(Pens.Black, cX, fBox.Y + 4, cX, fBox.Y + 19);
                    }
                }
            }
            else
            {
                FormulaBarChevronBounds = Rectangle.Empty;
            }

            // ==================== 2. Spreadsheet Grid Headers & Rows ====================
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = fBarY + FormulaBarHeight;
            int vScrollW = 16;
            int bottomBarH = 56; // Sheet tabs (28px) + Status bar (28px)
            int gridW = width - vScrollW;
            int maxGridY = height - bottomBarH;

            // Top-Left Header Intersection (Select All Triangle) & Top-Right Header Corner (above vertical scrollbar)
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

                // Top-Right Header Corner (fills the 16x26px gap directly above vertical scrollbar)
                Rectangle trRect = new Rectangle(gridW, startY, vScrollW, headerH);
                g.FillRectangle(_headerBgBrush, trRect);
                g.DrawRectangle(_headerBorderPen, trRect);
            }

            // 2.1 Column Headers (A, B, C, D... Z, AA, AB...)
            int curX = headerW;
            uint colIdx = ScrollCol;

            uint minSelC = Math.Min(SelectedCol, SelEndCol);
            uint maxSelC = Math.Max(SelectedCol, SelEndCol);
            uint minSelR = Math.Min(SelectedRow, SelEndRow);
            uint maxSelR = Math.Max(SelectedRow, SelEndRow);

            if (ShowHeadings)
            {
                GraphicsState gsColHeaders = g.Save();
                Rectangle colHeaderClip = new Rectangle(headerW, startY, Math.Max(0, gridW - headerW), headerH);
                g.SetClip(colHeaderClip);

                while (curX < gridW)
                {
                    int cW = GetColWidth(colIdx);
                    Rectangle cRect = new Rectangle(curX, startY, cW, headerH);
                    bool isColSelected = IsColSelected(colIdx);

                    if (isColSelected)
                    {
                        g.FillRectangle(_headerSelBgBrush, cRect);
                        g.DrawLine(_headerSelBorderPen, curX, startY + headerH - 1, curX + cW, startY + headerH - 1);
                    }
                    else
                    {
                        g.FillRectangle(_headerBgBrush, cRect);
                    }

                    g.DrawRectangle(_headerBorderPen, cRect);

                    string colName = SparseMatrix.ColIndexToName(colIdx);
                    Rectangle colTextRect = new Rectangle(curX, startY, cW, headerH);
                    g.DrawString(colName, isColSelected ? _headerSelFont : _headerFont, isColSelected ? _headerSelTextBrush : _headerTextBrush, colTextRect, _sfCenterCenter);

                    // Split divider line
                    g.DrawLine(_dividerPen, curX + cW - 1, startY + 4, curX + cW - 1, startY + headerH - 4);

                    curX += cW;
                    colIdx++;
                }

                g.Restore(gsColHeaders);
            }

            // 2.2 Rows & Cells
            int curY = startY + headerH;
            uint rowIdx = ScrollRow;

            // Tracking active editing cell bounding box for in-place rendering
            Rectangle editingCellRect = Rectangle.Empty;

            // Pre-fill entire grid viewport to pure white to ensure no unpainted Direct2D alpha=0 pixels exist
            Rectangle gridClipArea = new Rectangle(headerW, startY + headerH, Math.Max(0, gridW - headerW), Math.Max(0, maxGridY - (startY + headerH)));
            g.FillRectangle(Brushes.White, gridClipArea);

            // Track which merged cells have already been rendered in this frame
            HashSet<long> renderedMergedCells = new HashSet<long>();

            while (curY < maxGridY)
            {
                if (HiddenRows != null && HiddenRows.Contains(rowIdx))
                {
                    rowIdx++;
                    continue;
                }
                int rH = GetRowHeight(rowIdx);
                bool isRowSelected = IsRowSelected(rowIdx);

                // Row Header
                if (ShowHeadings)
                {
                    Rectangle rRect = new Rectangle(0, curY, headerW, rH);
                    if (isRowSelected)
                    {
                        g.FillRectangle(_headerSelBgBrush, rRect);
                        g.DrawLine(_headerSelBorderPen, headerW - 1, curY, headerW - 1, curY + rH);
                    }
                    else
                    {
                        g.FillRectangle(_headerBgBrush, rRect);
                    }
                    g.DrawRectangle(_headerBorderPen, rRect);

                    string rStr = rowIdx.ToString();
                    Rectangle rowTextRect = new Rectangle(0, curY, headerW - 6, rH);
                    g.DrawString(rStr, isRowSelected ? _headerRowSelFont : _headerRowFont, isRowSelected ? _headerSelTextBrush : _headerTextBrush, rowTextRect, _sfFarCenter);
                }

                // Render Cells across columns
                curX = headerW;
                colIdx = ScrollCol;

                while (curX < gridW)
                {
                    int cW = GetColWidth(colIdx);
                    CellData cell = CurrentSheet.GetCell(rowIdx, colIdx);

                    // Check if this cell is part of a merged range
                    bool isMerged = cell != null && cell.Format != null && (cell.Format.IsMergedParent || cell.Format.IsMergedChild);
                    if (isMerged)
                    {
                        uint masterRow = cell.Format.IsMergedParent ? rowIdx : cell.Format.MergedParentRow;
                        uint masterCol = cell.Format.IsMergedParent ? colIdx : cell.Format.MergedParentCol;
                        long mKey = ((long)masterRow << 32) | (long)masterCol;

                        if (renderedMergedCells.Contains(mKey))
                        {
                            curX += cW;
                            colIdx++;
                            continue;
                        }

                        renderedMergedCells.Add(mKey);

                        CellData masterCell = cell.Format.IsMergedParent ? cell : (CurrentSheet.GetCell(masterRow, masterCol) ?? cell);
                        int mColSpan = (masterCell.Format != null && masterCell.Format.MergeColSpan > 0) ? masterCell.Format.MergeColSpan : 1;
                        int mRowSpan = (masterCell.Format != null && masterCell.Format.MergeRowSpan > 0) ? masterCell.Format.MergeRowSpan : 1;

                        int mX = GetColScreenX(masterCol, headerW);
                        int mY = GetRowScreenY(masterRow, startY, headerH);
                        int mW = 0;
                        for (uint mc = 0; mc < mColSpan; mc++) mW += GetColWidth(masterCol + mc);
                        int mH = 0;
                        for (uint mr = 0; mr < mRowSpan; mr++) mH += GetRowHeight(masterRow + mr);

                        Rectangle cellRect = new Rectangle(mX, mY, mW, mH);

                        if (IsEditing && rowIdx == SelectedRow && colIdx == SelectedCol)
                        {
                            editingCellRect = cellRect;
                        }

                        Color cellBg = Color.White;
                        if (masterCell.Format != null && masterCell.Format.BgColor != unchecked((int)0xFFFFFFFF) && masterCell.Format.BgColor != 0)
                        {
                            int bg = masterCell.Format.BgColor;
                            if ((bg & unchecked((int)0xFF000000)) == 0 && bg != 0) bg |= unchecked((int)0xFF000000);
                            cellBg = Color.FromArgb(bg);
                        }

                        g.PushClip(gridClipArea);

                        SolidBrush bgB = GetCachedBrush(cellBg);
                        g.FillRectangle(bgB, cellRect);

                        if (ShowGridlines)
                        {
                            g.DrawRectangle(_gridLinePen, cellRect);
                        }

                        if (masterCell.Format != null && masterCell.Format.Borders != null)
                        {
                            DrawCellBorders(g, masterCell.Format.Borders, cellRect);
                        }

                        if (!(IsEditing && rowIdx == SelectedRow && colIdx == SelectedCol))
                        {
                            Rectangle visibleMergedRect = Rectangle.Intersect(cellRect, gridClipArea);
                            if (visibleMergedRect.Width > 0 && visibleMergedRect.Height > 0)
                            {
                                DrawCellText(g, masterCell, cellRect, cellBg, visibleMergedRect);
                            }
                        }

                        g.PopClip();

                        curX += cW;
                        colIdx++;
                        continue;
                    }

                    // Normal Non-merged cell
                    Rectangle cellRectNorm = new Rectangle(curX, curY, cW, rH);

                    if (IsEditing && rowIdx == SelectedRow && colIdx == SelectedCol)
                    {
                        editingCellRect = cellRectNorm;
                    }

                    Color cellBgNorm = Color.White;
                    if (cell != null && cell.Format != null && cell.Format.BgColor != unchecked((int)0xFFFFFFFF) && cell.Format.BgColor != 0)
                    {
                        int bg = cell.Format.BgColor;
                        if ((bg & unchecked((int)0xFF000000)) == 0 && bg != 0) bg |= unchecked((int)0xFF000000);
                        cellBgNorm = Color.FromArgb(bg);
                    }

                    SolidBrush bgBNorm = GetCachedBrush(cellBgNorm);
                    g.FillRectangle(bgBNorm, cellRectNorm);

                    if (ShowGridlines)
                    {
                        g.DrawRectangle(_gridLinePen, cellRectNorm);
                    }

                    if (cell != null && cell.Format != null && cell.Format.Borders != null)
                    {
                        DrawCellBorders(g, cell.Format.Borders, cellRectNorm);
                    }

                    if (!(IsEditing && rowIdx == SelectedRow && colIdx == SelectedCol))
                    {
                        int textRenderW = cW;
                        bool wrapText = cell != null && cell.Format != null && cell.Format.WrapText;
                        if (!wrapText && (cell == null || cell.Format == null || cell.Format.Align == 0))
                        {
                            uint nextC = colIdx + 1;
                            while (curX + textRenderW < gridW)
                            {
                                CellData nextCell = CurrentSheet.GetCell(rowIdx, nextC);
                                bool hasContent = nextCell != null && (!string.IsNullOrEmpty(nextCell.EvaluatedStr) || !string.IsNullOrEmpty(nextCell.RawValue) || (nextCell.Format != null && nextCell.Format.BgColor != 0 && nextCell.Format.BgColor != unchecked((int)0xFFFFFFFF)));
                                if (hasContent) break;
                                textRenderW += GetColWidth(nextC);
                                nextC++;
                            }
                        }

                        Rectangle textClip = new Rectangle(curX, curY, textRenderW, rH);
                        DrawCellText(g, cell, cellRectNorm, cellBgNorm, textClip);
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

                    // Circle Invalid Data (Red oval)
                    if (CircleInvalidData)
                    {
                        var dvRule = GetDataValidationRule(rowIdx, colIdx);
                        if (dvRule != null)
                        {
                            var vRes = dvRule.Validate(cell != null ? cell.RawValue : "", CurrentSheet, rowIdx, colIdx);
                            if (!vRes.IsValid)
                            {
                                using (Pen circlePen = new Pen(Color.FromArgb(220, 38, 38), 1.5f))
                                {
                                    g.DrawEllipse(circlePen, curX + 2, curY + 2, cW - 4, rH - 4);
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

            GraphicsState gStateSel = g.Save();
            g.SetClip(gridClipArea);

            DrawAdditionalSelectionRanges(g, headerW, startY, headerH, width, height);
            DrawFormulaReferenceHighlights(g, headerW, startY, headerH, width, height);

            if (selW > 0 && selH > 0)
            {
                double animNow = FluentMotion.NowMs;
                bool resetSelGlide = !_selAnimInitialized || _selAnimLastScrollR != ScrollRow || _selAnimLastScrollC != ScrollCol ||
                    _selAnimLastSheet != ActiveSheetIndex || Math.Abs(_selAnimLastZoom - ZoomFactor) > 0.01f || IsSelectingRange ||
                    Math.Abs(selX1 - _selAnimCurX) > 700f || Math.Abs(selY1 - _selAnimCurY) > 500f;
                if (resetSelGlide)
                {
                    _selAnimInitialized = true;
                    _selAnimStartX = _selAnimCurX = _selAnimTargetX = selX1;
                    _selAnimStartY = _selAnimCurY = _selAnimTargetY = selY1;
                    _selAnimStartW = _selAnimCurW = _selAnimTargetW = selW;
                    _selAnimStartH = _selAnimCurH = _selAnimTargetH = selH;
                    _selAnimStartMs = -10000.0;
                }
                else if (selX1 != _selAnimTargetX || selY1 != _selAnimTargetY || selW != _selAnimTargetW || selH != _selAnimTargetH)
                {
                    _selAnimStartX = _selAnimCurX;
                    _selAnimStartY = _selAnimCurY;
                    _selAnimStartW = _selAnimCurW;
                    _selAnimStartH = _selAnimCurH;
                    _selAnimTargetX = selX1;
                    _selAnimTargetY = selY1;
                    _selAnimTargetW = selW;
                    _selAnimTargetH = selH;
                    _selAnimStartMs = animNow;
                }
                _selAnimLastScrollR = ScrollRow;
                _selAnimLastScrollC = ScrollCol;
                _selAnimLastSheet = ActiveSheetIndex;
                _selAnimLastZoom = ZoomFactor;

                double selElapsed = animNow - _selAnimStartMs;
                if (selElapsed >= 0 && selElapsed < CalcFocusGlideDurationMs)
                {
                    float e = FluentMotion.EaseOutQuint((float)(selElapsed / CalcFocusGlideDurationMs));
                    _selAnimCurX = FluentMotion.Lerp(_selAnimStartX, _selAnimTargetX, e);
                    _selAnimCurY = FluentMotion.Lerp(_selAnimStartY, _selAnimTargetY, e);
                    _selAnimCurW = FluentMotion.Lerp(_selAnimStartW, _selAnimTargetW, e);
                    _selAnimCurH = FluentMotion.Lerp(_selAnimStartH, _selAnimTargetH, e);
                }
                else
                {
                    _selAnimCurX = selX1;
                    _selAnimCurY = selY1;
                    _selAnimCurW = selW;
                    _selAnimCurH = selH;
                }

                float drawSelX = _selAnimCurX;
                float drawSelY = _selAnimCurY;
                float drawSelW = Math.Max(1f, _selAnimCurW);
                float drawSelH = Math.Max(1f, _selAnimCurH);

                // Range Soft Green Overlay
                if (minSelR != maxSelR || minSelC != maxSelC)
                {
                    using (SolidBrush rangeFill = new SolidBrush(Color.FromArgb(30, 16, 124, 65)))
                    {
                        g.FillRectangle(rangeFill, drawSelX, drawSelY, drawSelW, drawSelH);
                    }
                }

                // Excel Deep Green Border (Smooth 365 Glide)
                using (Pen selPen = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    g.DrawRectangle(selPen, drawSelX, drawSelY, drawSelW, drawSelH);
                }

                // ==================== Excel 365 Dynamic Array Spill Border ====================
                uint origR, origC;
                int spillRows, spillCols;
                if (CurrentSheet != null && CurrentSheet.IsCellSpilled(SelectedRow, SelectedCol, out origR, out origC, out spillRows, out spillCols))
                {
                    if (spillRows > 1 || spillCols > 1)
                    {
                        int spX = GetColScreenX(origC, headerW);
                        int spY = GetRowScreenY(origR, startY, headerH);
                        int spW = 0;
                        for (uint c = origC; c < origC + (uint)spillCols; c++) spW += GetColWidth(c);
                        int spH = 0;
                        for (uint r = origR; r < origR + (uint)spillRows; r++) spH += GetRowHeight(r);

                        if (spW > 0 && spH > 0)
                        {
                            using (Pen spillPen = new Pen(Color.FromArgb(17, 108, 228), 1.5f))
                            {
                                g.DrawRectangle(spillPen, spX, spY, spW, spH);
                            }
                        }
                    }
                }

                // Excel Fill Handle (Square dot at bottom-right corner - glides smoothly with focus box)
                int handleSize = AppSettings.Instance.TouchModeEnabled ? 10 : 6;
                RectangleF handleRect = new RectangleF(drawSelX + drawSelW - handleSize / 2f, drawSelY + drawSelH - handleSize / 2f, handleSize, handleSize);
                g.FillRectangle(Color.FromArgb(16, 124, 65).ToBrush(), handleRect);
                g.DrawRectangle(Pens.White, handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height);

                DrawRemoteCollabCellOverlays(g, headerW, startY, headerH);

                // Dropdown Content Viewer Chevron Button [ ▾ ] (when active cell has long text or newlines, or Data Validation List)
                CellData activeCell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                string activeText = activeCell != null ? (!string.IsNullOrEmpty(activeCell.EvaluatedStr) ? activeCell.EvaluatedStr : (!string.IsNullOrEmpty(activeCell.FormulaStr) ? activeCell.FormulaStr : (activeCell.RawValue ?? ""))) : "";
                var activeDvRule = GetDataValidationRule(SelectedRow, SelectedCol);
                bool hasDvList = activeDvRule != null && activeDvRule.ValidationType == DataValidationType.List && activeDvRule.ShowDropdown;
                bool isLongOrMultiline = activeText.Length > 20 || activeText.Contains("\n") || activeText.Contains("\r");
                if ((hasDvList || isLongOrMultiline) && minSelR == maxSelR && minSelC == maxSelC && selW >= 24 && selH >= 18)
                {
                    int btnSize = 16;
                    Rectangle dropBtn = new Rectangle(selX1 + selW - btnSize - 1, selY1 + selH - btnSize - 1, btnSize, btnSize);
                    ActiveCellDropdownButtonBounds = dropBtn;
                    using (SolidBrush bBg = new SolidBrush(IsHoveringCellDropdown ? Color.FromArgb(235, 245, 238) : Color.FromArgb(248, 250, 252)))
                    using (Pen pBdr = new Pen(Color.FromArgb(16, 124, 65), 1.2f))
                    {
                        g.FillRectangle(bBg, dropBtn);
                        g.DrawRectangle(pBdr, dropBtn);
                    }
                    using (Pen pChev = new Pen(Color.FromArgb(16, 124, 65), 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(pChev, dropBtn.X + 4.5f, dropBtn.Y + 6.5f, dropBtn.X + 8f, dropBtn.Y + 10.5f);
                        g.DrawLine(pChev, dropBtn.X + 8f, dropBtn.Y + 10.5f, dropBtn.X + 11.5f, dropBtn.Y + 6.5f);
                    }
                }
                else
                {
                    ActiveCellDropdownButtonBounds = Rectangle.Empty;
                }
            }

            // ==================== 3.0b Visual Feedback when Dragging Cell Range ====================
            if (IsDraggingCellMove && MoveTargetRow > 0 && MoveTargetCol > 0)
            {
                uint srcMinR = Math.Min(MoveSourceStartRow, MoveSourceEndRow);
                uint srcMaxR = Math.Max(MoveSourceStartRow, MoveSourceEndRow);
                uint srcMinC = Math.Min(MoveSourceStartCol, MoveSourceEndCol);
                uint srcMaxC = Math.Max(MoveSourceStartCol, MoveSourceEndCol);

                uint spanR = srcMaxR - srcMinR + 1;
                uint spanC = srcMaxC - srcMinC + 1;

                int dstX = GetColScreenX(MoveTargetCol, headerW);
                int dstY = GetRowScreenY(MoveTargetRow, startY, headerH);

                int dstW = 0;
                for (uint c = 0; c < spanC; c++) dstW += GetColWidth(MoveTargetCol + c);
                int dstH = 0;
                for (uint r = 0; r < spanR; r++) dstH += GetRowHeight(MoveTargetRow + r);

                if (dstW > 0 && dstH > 0)
                {
                    using (SolidBrush ghostB = new SolidBrush(Color.FromArgb(40, 16, 124, 65)))
                    {
                        g.FillRectangle(ghostB, dstX, dstY, dstW, dstH);
                    }

                    using (Pen ghostPen = new Pen(Color.FromArgb(16, 124, 65), 2f))
                    {
                        ghostPen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(ghostPen, dstX, dstY, dstW, dstH);
                    }

                    string targetRangeName = string.Format("{0}{1}", SparseMatrix.ColIndexToName(MoveTargetCol), MoveTargetRow);
                    if (spanR > 1 || spanC > 1)
                    {
                        string endRangeName = string.Format("{0}{1}", SparseMatrix.ColIndexToName(MoveTargetCol + spanC - 1), MoveTargetRow + spanR - 1);
                        targetRangeName = string.Format("{0}:{1}", targetRangeName, endRangeName);
                    }
                    string badgeText = IsCopyMode ? string.Format(AppL10n.T("➕ Sao chép tới [{0}]", "➕ Copy to [{0}]"), targetRangeName) : string.Format(AppL10n.T("✥ Dời tới [{0}]", "✥ Move to [{0}]"), targetRangeName);

                    using (Font badgeFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        SizeF bSz = g.MeasureString(badgeText, badgeFont);
                        int badgeX = dstX + 6;
                        int badgeY = Math.Max(startY + headerH + 2, dstY - 24);
                        Rectangle badgeRect = new Rectangle(badgeX, badgeY, (int)bSz.Width + 12, 20);

                        using (GraphicsPath bPath = CreateRoundedPath(badgeRect, 3))
                        {
                            using (SolidBrush bgB = new SolidBrush(Color.FromArgb(220, 15, 23, 42))) g.FillPath(bgB, bPath);
                            g.DrawPath(Pens.DarkSlateGray, bPath);
                        }
                        g.DrawString(badgeText, badgeFont, Brushes.White, badgeX + 6, badgeY + 2);
                    }
                }
            }
            g.Restore(gStateSel);

            // ==================== 3.1 In-Place Cell Editor ====================
            ActiveEditingCellRect = editingCellRect;
            bool isFormulaOnOtherSheet = IsEditing && FormulaSourceSheetIndex >= 0 && FormulaSourceSheetIndex != ActiveSheetIndex;
            if (isFormulaOnOtherSheet)
            {
                int tipY = fBarY + FormulaBarHeight + 4;
                if (ActiveSuggestions.Count > 0)
                {
                    DrawFormulaSuggestions(g, _lastFormulaInputRect.X, tipY);
                }
                else
                {
                    DrawFormulaScreenTip(g, _lastFormulaInputRect.X, tipY, width);
                }
            }
            else if (IsEditing && !editingCellRect.IsEmpty)
            {
                g.FillRectangle(Brushes.White, editingCellRect);
                using (Pen ep = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    g.DrawRectangle(ep, editingCellRect);
                }
                CellData editCell = CurrentSheet != null ? CurrentSheet.GetCell(SelectedRow, SelectedCol) : null;
                string edFName = (editCell != null && editCell.Format != null && !string.IsNullOrEmpty(editCell.Format.FontName)) ? editCell.Format.FontName : "Calibri";
                float edFSize = (editCell != null && editCell.Format != null && editCell.Format.FontSize > 0) ? editCell.Format.FontSize : 11.0f;
                FontStyle edFStyle = FontStyle.Regular;
                if (editCell != null && editCell.Format != null)
                {
                    if (editCell.Format.Bold) edFStyle |= FontStyle.Bold;
                    if (editCell.Format.Italic) edFStyle |= FontStyle.Italic;
                    if (editCell.Format.Underline) edFStyle |= FontStyle.Underline;
                }

                Font edFont = GetCellFont(edFName, edFSize * ZoomFactor, edFStyle);
                Rectangle clipBox = new Rectangle(editingCellRect.X + 2, editingCellRect.Y + 2, Math.Max(1, editingCellRect.Width - 4), Math.Max(1, editingCellRect.Height - 4));
                g.PushClip(clipBox);

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

                Rectangle edTextBounds = new Rectangle(editingCellRect.X + 4, editingCellRect.Y + 3, Math.Max(0, editingCellRect.Width - 8), editingCellRect.Height - 6);
                bool edWrap = (editCell != null && editCell.Format != null && editCell.Format.WrapText) || editingCellRect.Height >= 30 || EditText.Contains("\n") || EditText.Contains("\r");
                StringFormat edSf = GetCachedStringFormat(0, edWrap ? 0 : 1, edWrap);
                g.DrawString(EditText, edFont, Brushes.Black, edTextBounds, edSf);

                float curXPos = editingCellRect.X + 4;
                if (EditSelLength == 0)
                {
                    string sub = EditText.Substring(0, Math.Min(EditText.Length, EditCursorPos));
                    SizeF sz = sub.Length > 0 ? g.MeasureString(sub, edFont) : SizeF.Empty;
                    curXPos = editingCellRect.X + 4 + (sub.Length > 0 ? sz.Width : 0);
                    g.DrawLine(Pens.Black, curXPos, editingCellRect.Y + 3, curXPos, editingCellRect.Y + editingCellRect.Height - 4);
                }
                CurrentCaretScreenX = (int)curXPos;
                CurrentCaretScreenY = editingCellRect.Bottom + 2;
                CurrentCaretScreenH = editingCellRect.Height;

                g.PopClip();

                // Floating Formula Suggestions Popup & Live Parameter ScreenTip
                if (EditText.StartsWith("="))
                {
                    if (ActiveSuggestions.Count > 0)
                    {
                        DrawFormulaSuggestions(g, editingCellRect.X, editingCellRect.Bottom + 2);
                    }
                    else
                    {
                        DrawFormulaScreenTip(g, editingCellRect.X, editingCellRect.Bottom + 2, width);
                    }
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

            // ==================== 3.4 Excel 365 Formula Auditing Arrows ====================
            if (AuditingArrows != null && AuditingArrows.Count > 0)
            {
                GraphicsState gsAudit = g.Save();
                Rectangle gridClip = new Rectangle(headerW, startY + headerH, width - headerW - vScrollW, maxGridY - (startY + headerH));
                g.SetClip(gridClip);
                foreach (var arr in AuditingArrows)
                {
                    int x1 = GetColScreenX(arr.FromCol, headerW) + GetColWidth(arr.FromCol) / 2;
                    int y1 = GetRowScreenY(arr.FromRow, startY, headerH) + GetRowHeight(arr.FromRow) / 2;
                    int x2 = GetColScreenX(arr.ToCol, headerW) + GetColWidth(arr.ToCol) / 2;
                    int y2 = GetRowScreenY(arr.ToRow, startY, headerH) + GetRowHeight(arr.ToRow) / 2;

                    Color col = arr.IsError ? Color.FromArgb(220, 38, 38) : arr.ArrowColor;
                    using (Pen p = new Pen(col, 2.0f))
                    using (SolidBrush dotBr = new SolidBrush(col))
                    {
                        g.FillEllipse(dotBr, x1 - 3, y1 - 3, 6, 6);
                        p.CustomEndCap = new AdjustableArrowCap(5, 5, true);
                        g.DrawLine(p, x1, y1, x2, y2);
                    }
                }
                g.Restore(gsAudit);
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
            uint maxR = Math.Max(100, CurrentSheet != null ? CurrentSheet.GetMaxRow() + 30 : 100);
            float scrollProgress = Math.Min(1.0f, (ScrollRow - 1) / (float)maxR);
            int vThumbY = vScrollY + 16 + (int)(scrollProgress * (vTrackAvail - vThumbH));
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

            // Sheet Navigation Buttons (< >) using Windows 11 Fluent Icons
            FluentIconRenderer.DrawGlyph(g, "\uE76B", new Rectangle(4, tabY + 4, 20, 20), Color.FromArgb(100, 116, 139), 8.5f);
            FluentIconRenderer.DrawGlyph(g, "\uE76C", new Rectangle(24, tabY + 4, 20, 20), Color.FromArgb(100, 116, 139), 8.5f);

            int tabX = 46;
            Rectangle activeSheetTabRect = Rectangle.Empty;
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
                        activeSheetTabRect = tabRect;
                        g.FillRectangle(Brushes.White, tabRect);
                        using (Pen p = new Pen(Color.FromArgb(209, 213, 219)))
                        {
                            g.DrawLine(p, tabRect.Left, tabRect.Top, tabRect.Left, tabRect.Bottom);
                            g.DrawLine(p, tabRect.Right, tabRect.Top, tabRect.Right, tabRect.Bottom);
                        }
                    }

                    using (SolidBrush inactTxtB = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    {
                        g.DrawString(sName, isActive ? activeTabFont : tabFont, isActive ? Color.FromArgb(16, 124, 65).ToBrush() : inactTxtB, tabX + 12, tabY + 5);
                    }
                    tabX += tabW + 2;
                }

                if (!activeSheetTabRect.IsEmpty)
                {
                    double tabNow = FluentMotion.NowMs;
                    if (!_sheetTabIndInit)
                    {
                        _sheetTabIndInit = true;
                        _sheetTabLastIdx = ActiveSheetIndex;
                        _sheetTabStartX = _sheetTabCurX = _sheetTabTargetX = activeSheetTabRect.Left;
                        _sheetTabStartW = _sheetTabCurW = _sheetTabTargetW = activeSheetTabRect.Width;
                        _sheetTabStartMs = -10000.0;
                    }
                    else if (_sheetTabLastIdx != ActiveSheetIndex)
                    {
                        _sheetTabLastIdx = ActiveSheetIndex;
                        _sheetTabStartX = _sheetTabCurX;
                        _sheetTabStartW = _sheetTabCurW;
                        _sheetTabTargetX = activeSheetTabRect.Left;
                        _sheetTabTargetW = activeSheetTabRect.Width;
                        _sheetTabStartMs = tabNow;
                    }
                    else
                    {
                        _sheetTabTargetX = activeSheetTabRect.Left;
                        _sheetTabTargetW = activeSheetTabRect.Width;
                    }

                    double tabElapsed = tabNow - _sheetTabStartMs;
                    if (tabElapsed >= 0 && tabElapsed < CalcSheetTabDurationMs)
                    {
                        float e = FluentMotion.EaseOutQuint((float)(tabElapsed / CalcSheetTabDurationMs));
                        _sheetTabCurX = FluentMotion.Lerp(_sheetTabStartX, _sheetTabTargetX, e);
                        _sheetTabCurW = FluentMotion.Lerp(_sheetTabStartW, _sheetTabTargetW, e);
                    }
                    else
                    {
                        _sheetTabCurX = _sheetTabTargetX;
                        _sheetTabCurW = _sheetTabTargetW;
                    }

                    using (SolidBrush ab = new SolidBrush(Color.FromArgb(16, 124, 65)))
                    {
                        g.FillRectangle(ab, _sheetTabCurX, activeSheetTabRect.Bottom - 3f, _sheetTabCurW, 3f);
                    }
                }

                // Add Sheet Button (+)
                Rectangle addTabRect = new Rectangle(tabX + 4, tabY + 4, 20, 20);
                using (GraphicsPath addPath = CreateRoundedPath(addTabRect, 10))
                {
                    g.FillPath(Brushes.White, addPath);
                    using (Pen ap = new Pen(Color.FromArgb(203, 213, 225))) g.DrawPath(ap, addPath);
                }
                FluentIconRenderer.DrawGlyph(g, "\uE710", addTabRect, Color.FromArgb(16, 124, 65), 8.5f);
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

                // Left Arrow
                Rectangle lBtn = new Rectangle(hScrollX, tabY + 4, 16, 20);
                g.FillRectangle(Brushes.White, lBtn);
                g.DrawRectangle(Pens.LightGray, lBtn);
                FluentIconRenderer.DrawGlyph(g, "\uE76B", lBtn, Color.DimGray, 7.5f);

                // Right Arrow
                Rectangle rBtn = new Rectangle(hScrollX + hScrollW - 16, tabY + 4, 16, 20);
                g.FillRectangle(Brushes.White, rBtn);
                g.DrawRectangle(Pens.LightGray, rBtn);
                FluentIconRenderer.DrawGlyph(g, "\uE76C", rBtn, Color.DimGray, 7.5f);

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
                // Left: Ready / Point / Edit + Circular Reference + Manual Calc indicators
                string modeLabel = IsEditingNameBox
                    ? AppL10n.T("Nhập tên", "Enter Name")
                    : (IsEditing ? (IsFormulaPointMode ? AppL10n.T("Chọn điểm (Point)", "Point") : AppL10n.T("Chỉnh sửa", "Edit")) : AppL10n.T("Sẵn sàng", "Ready"));
                using (SolidBrush statB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString(modeLabel, statFont, statB, 12, statY + 6);
                }
                int badgeX = 128;
                if (!AutoCalculation)
                {
                    using (SolidBrush mBrush = new SolidBrush(Color.FromArgb(217, 119, 6)))
                        g.DrawString(AppL10n.T("Tính thủ công (F9)", "Manual Calc (F9)"), statBold, mBrush, badgeX, statY + 6);
                    badgeX += 130;
                }
                if (FormulaEngine.CircularReferenceDetected)
                {
                    string circMsg = AppL10n.T("Tham chiếu vòng: ", "Circular Ref: ") + (FormulaEngine.LastCircularReferenceCell ?? "");
                    using (SolidBrush cBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                        g.DrawString(circMsg, statBold, cBrush, badgeX, statY + 6);
                }

                // Calculation Metrics across all selected ranges (Average, Count, Sum)
                double selSum = 0.0;
                double selCount = 0;
                double numCount = 0;
                ComputeSelectionStatusMetrics(out selSum, out selCount, out numCount);

                if (selCount > 1)
                {
                    string statMetrics = string.Format(AppL10n.T("Trung bình: {0:N2}    Đếm: {1}    Tổng: {2:N2}", "Average: {0:N2}    Count: {1}    Sum: {2:N2}"), (numCount > 0 ? (selSum / numCount) : 0), selCount, selSum);
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

        public void FilterFormulaSuggestions(string query = null)
        {
            ActiveSuggestions.Clear();
            SelectedSuggestionIndex = 0;
            if (!IsEditing || string.IsNullOrEmpty(EditText) || !EditText.TrimStart().StartsWith("=")) return;

            int tokenStart, tokenLen;
            string prefix = GetFormulaTokenBeforeCursor(EditText, EditCursorPos, out tokenStart, out tokenLen);
            if (string.IsNullOrEmpty(prefix)) return;

            // Avoid suggesting functions inside sheet names ('Sheet1'!) or explicit cell-lock ($A1)
            if (tokenStart > 0 && (EditText[tokenStart - 1] == '$' || EditText[tokenStart - 1] == '\'')) return;
            // Pure digits (like =12) are numeric literals, not identifiers
            if (char.IsDigit(prefix[0])) return;

            if (AllFormulas != null)
            {
                foreach (var fs in AllFormulas)
                {
                    if (fs.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        ActiveSuggestions.Add(fs);
                    }
                }
            }

            if (DefinedNames != null)
            {
                foreach (var dn in DefinedNames)
                {
                    if (dn != null && !string.IsNullOrWhiteSpace(dn.Name) &&
                        dn.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        ActiveSuggestions.Add(new FormulaSuggestion(dn.Name, dn.Name + " → " + dn.RefersTo, AppL10n.T("Tên vùng đã định nghĩa (Defined Name)", "Defined Name Range")));
                    }
                }
            }

            if (Tables != null)
            {
                foreach (var tbl in Tables)
                {
                    if (tbl != null && !string.IsNullOrWhiteSpace(tbl.Name) &&
                        tbl.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        ActiveSuggestions.Add(new FormulaSuggestion(tbl.Name, tbl.Name + AppL10n.T("[Cột]", "[Column]"), AppL10n.T("Bảng dữ liệu có cấu trúc (Structured Table)", "Structured Data Table")));
                    }
                }
            }

            if (ActiveSuggestions.Count > 12)
            {
                ActiveSuggestions.RemoveRange(12, ActiveSuggestions.Count - 12);
            }
        }

        public void CommitFormulaSuggestion(FormulaSuggestion sug)
        {
            if (sug == null || string.IsNullOrEmpty(EditText)) return;
            int tokenStart, tokenLen;
            GetFormulaTokenBeforeCursor(EditText, EditCursorPos, out tokenStart, out tokenLen);
            bool isNamedOrTable = false;
            if (DefinedNames != null)
            {
                foreach (var dn in DefinedNames)
                {
                    if (dn != null && string.Equals(dn.Name, sug.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        isNamedOrTable = true;
                        break;
                    }
                }
            }
            if (!isNamedOrTable && Tables != null)
            {
                foreach (var tbl in Tables)
                {
                    if (tbl != null && string.Equals(tbl.Name, sug.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        isNamedOrTable = true;
                        break;
                    }
                }
            }

            string insertion = isNamedOrTable ? sug.Name : (sug.Name + "(");
            string before = EditText.Substring(0, tokenStart);
            string after = (tokenStart + tokenLen <= EditText.Length) ? EditText.Substring(tokenStart + tokenLen) : "";
            EditText = before + insertion + after;
            EditCursorPos = before.Length + insertion.Length;
            EditSelStart = EditCursorPos;
            EditSelLength = 0;
            IsFormulaPointMode = !isNamedOrTable;
            _pointInsertStart = -1;
            _pointInsertLength = 0;
            FilterFormulaSuggestions();
        }

        private void DrawFormulaSuggestions(D2DGraphics g, int x, int y)
        {
            string query = EditText.Length > 1 ? EditText.Substring(1).Trim().ToUpper() : "";
            FilterFormulaSuggestions(query);
            if (ActiveSuggestions == null || ActiveSuggestions.Count == 0)
            {
                FormulaSuggestionsBounds = Rectangle.Empty;
                return;
            }

            int popW = 320;
            int count = Math.Min(6, ActiveSuggestions.Count);
            int popH = count * 26 + 32;
            Rectangle popRect = new Rectangle(x, y, popW, popH);
            FormulaSuggestionsBounds = popRect;

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(255, 255, 255))) g.FillRectangle(bg, popRect);
            using (Pen p = new Pen(Color.FromArgb(16, 124, 65), 1.5f)) g.DrawRectangle(p, popRect);

            using (Font fFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font dFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (Font codeFont = new Font("Consolas", 8.5f, FontStyle.Regular))
            {
                int itemY = y + 3;
                for (int i = 0; i < count; i++)
                {
                    var sug = ActiveSuggestions[i];
                    Rectangle itemRect = new Rectangle(x + 2, itemY, popW - 4, 24);
                    if (i == SelectedSuggestionIndex)
                    {
                        using (SolidBrush selB = new SolidBrush(Color.FromArgb(220, 252, 231))) g.FillRectangle(selB, itemRect);
                        using (Pen selP = new Pen(Color.FromArgb(34, 197, 94))) g.DrawRectangle(selP, itemRect);
                        g.DrawString("fx", fFont, Color.FromArgb(16, 124, 65).ToBrush(), x + 6, itemY + 3);
                        g.DrawString(sug.Name, fFont, Brushes.Black, x + 28, itemY + 3);
                        g.DrawString(sug.Syntax, codeFont, Brushes.DimGray, x + 110, itemY + 4);
                    }
                    else
                    {
                        g.DrawString("fx", fFont, Brushes.Gray, x + 6, itemY + 3);
                        g.DrawString(sug.Name, fFont, Brushes.Black, x + 28, itemY + 3);
                        g.DrawString(sug.Syntax, codeFont, Brushes.DimGray, x + 110, itemY + 4);
                    }
                    itemY += 26;
                }

                if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < ActiveSuggestions.Count)
                {
                    g.DrawLine(Pens.LightGray, x + 4, itemY + 1, x + popW - 4, itemY + 1);
                    g.DrawString("💡 " + AppL10n.Tr(ActiveSuggestions[SelectedSuggestionIndex].Description), dFont, Brushes.DarkSlateGray, x + 6, itemY + 4);
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
            int startY = fBarY + FormulaBarHeight;

            // Check Active Cell Dropdown Reader or Formula Bar Chevron Hover
            if (!ActiveCellDropdownButtonBounds.IsEmpty && ActiveCellDropdownButtonBounds.Contains(x, y))
            {
                return Cursors.Hand;
            }
            if (!FormulaBarChevronBounds.IsEmpty && FormulaBarChevronBounds.Contains(x, y))
            {
                return Cursors.Hand;
            }

            // Check Formula Suggestions Hover (Hand Cursor)
            if (IsEditing && EditText != null && EditText.StartsWith("=") && ActiveSuggestions != null && ActiveSuggestions.Count > 0 && !FormulaSuggestionsBounds.IsEmpty && FormulaSuggestionsBounds.Contains(x, y))
            {
                return Cursors.Hand;
            }

            // 0a. Check In-Place Editing Cell & Formula Bar (Return IBeam text cursor)
            if (IsEditing)
            {
                if (y >= fBarY && y <= fBarY + FormulaBarHeight && x >= 170)
                {
                    return Cursors.IBeam;
                }

                int curEditX = headerW;
                for (uint c = ScrollCol; c < SelectedCol; c++) curEditX += GetColWidth(c);
                int curEditY = startY + headerH;
                for (uint r = ScrollRow; r < SelectedRow; r++) curEditY += GetRowHeight(r);
                int editCellW = GetColWidth(SelectedCol);
                int editCellH = GetRowHeight(SelectedRow);

                Rectangle editRect = new Rectangle(curEditX, curEditY, Math.Max(editCellW, 80), Math.Max(editCellH, 24));
                if (editRect.Contains(x, y))
                {
                    return Cursors.IBeam;
                }
            }

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

            int selX1 = GetColScreenX(minSelC, headerW);
            int selY1 = GetRowScreenY(minSelR, startY, headerH);

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

            // 3a. Selection Border Hover (Excel 4-Way Arrow Move Cursor `SizeAll`)
            if (selW > 0 && selH > 0)
            {
                Rectangle selRect = new Rectangle(selX1, selY1, selW, selH);
                bool onLeft = (Math.Abs(x - selRect.Left) <= 4 && y >= selRect.Top - 2 && y <= selRect.Bottom + 2);
                bool onRight = (Math.Abs(x - selRect.Right) <= 4 && y >= selRect.Top - 2 && y <= selRect.Bottom + 2);
                bool onTop = (Math.Abs(y - selRect.Top) <= 4 && x >= selRect.Left - 2 && x <= selRect.Right + 2);
                bool onBottom = (Math.Abs(y - selRect.Bottom) <= 4 && x >= selRect.Left - 2 && x <= selRect.Right + 2);

                if ((onLeft || onRight || onTop || onBottom) && !(Math.Abs(x - handleX) <= 6 && Math.Abs(y - handleY) <= 6))
                {
                    return Cursors.SizeAll;
                }
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

            // 0. Check Click on Floating Formula Suggestions Dropdown Item
            if (IsEditing && EditText != null && EditText.StartsWith("=") && ActiveSuggestions != null && ActiveSuggestions.Count > 0 && !FormulaSuggestionsBounds.IsEmpty && FormulaSuggestionsBounds.Contains(x, y))
            {
                int relY = y - (FormulaSuggestionsBounds.Y + 3);
                int clickedIdx = relY / 26;
                int maxCount = Math.Min(6, ActiveSuggestions.Count);
                if (clickedIdx >= 0 && clickedIdx < maxCount)
                {
                    SelectedSuggestionIndex = clickedIdx;
                    IsEditing = true;
                    CommitFormulaSuggestion(ActiveSuggestions[clickedIdx]);
                    FormulaSuggestionsBounds = Rectangle.Empty;
                    return true;
                }
                return true;
            }

            if (!FormulaBarChevronBounds.IsEmpty && FormulaBarChevronBounds.Contains(x, y))
            {
                IsFormulaBarExpanded = !IsFormulaBarExpanded;
                if (parentForm != null) parentForm.Invalidate();
                return true;
            }
            if (!ActiveCellDropdownButtonBounds.IsEmpty && ActiveCellDropdownButtonBounds.Contains(x, y))
            {
                OpenCellDropdownViewer(parentForm);
                return true;
            }

            int fBarY = 155;
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = fBarY + FormulaBarHeight;

            if (y >= fBarY && y < startY)
            {
                HandleFormulaBarButtonClick(x, y);
                return true;
            }
            if (IsEditingNameBox)
            {
                ExecuteGoToTarget(NameBoxEditText);
                IsEditingNameBox = false;
            }
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
                if (x >= 4 && x < 24)
                {
                    if (ActiveSheetIndex > 0) SwitchToSheet(ActiveSheetIndex - 1);
                    return true;
                }
                if (x >= 24 && x < 46)
                {
                    if (ActiveSheetIndex < SheetNames.Count - 1) SwitchToSheet(ActiveSheetIndex + 1);
                    return true;
                }

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
                        ResizeStartSize = GetRawColWidth(cIdx);
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
                        ResizeStartSize = GetRawRowHeight(rIdx);
                        return true;
                    }
                    curY += rH;
                    rIdx++;
                }
            }

            bool shiftMod = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool ctrlMod = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            // 3b. Column Header Click (Select Entire Column / Point / Shift / Ctrl)
            if (ShowHeadings && y >= startY && y <= startY + headerH && x >= headerW)
            {
                uint cIdx = GetColAtX(x, headerW);
                if (IsEditing && !string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("=") &&
                    (CanAcceptFormulaReferenceBeforeCursor(EditText, EditCursorPos) || IsFormulaPointMode || ctrlMod))
                {
                    BeginOrUpdatePointRange(1, cIdx, CalcMaxRows, cIdx, ctrlMod);
                    IsSelectingPointRange = false;
                    return true;
                }
                if (IsEditing)
                {
                    if (!CommitCurrentEditCell(true)) { if (parentForm != null) parentForm.Invalidate(); return true; }
                }

                IsSelectingFullCols = true;
                if (shiftMod)
                {
                    SelEndCol = cIdx;
                    SelectedRow = 1;
                    SelEndRow = CalcMaxRows;
                }
                else if (ctrlMod)
                {
                    CommitPrimarySelectionToAdditional();
                    SelectedCol = cIdx;
                    SelEndCol = cIdx;
                    SelectedRow = 1;
                    SelEndRow = CalcMaxRows;
                }
                else
                {
                    ClearAdditionalSelections();
                    SelectedRow = 1;
                    SelEndRow = CalcMaxRows;
                    SelectedCol = cIdx;
                    SelEndCol = cIdx;
                }
                return true;
            }

            // 4a. Row Header Click (Select Entire Row / Point / Shift / Ctrl)
            if (ShowHeadings && x >= 0 && x <= headerW && y >= startY + headerH && y < maxGridY)
            {
                uint rIdx = GetRowAtY(y, startY + headerH);
                if (IsEditing && !string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("=") &&
                    (CanAcceptFormulaReferenceBeforeCursor(EditText, EditCursorPos) || IsFormulaPointMode || ctrlMod))
                {
                    BeginOrUpdatePointRange(rIdx, 1, rIdx, CalcMaxCols, ctrlMod);
                    IsSelectingPointRange = false;
                    return true;
                }
                if (IsEditing)
                {
                    if (!CommitCurrentEditCell(true)) { if (parentForm != null) parentForm.Invalidate(); return true; }
                }

                IsSelectingFullRows = true;
                if (shiftMod)
                {
                    SelEndRow = rIdx;
                    SelectedCol = 1;
                    SelEndCol = CalcMaxCols;
                }
                else if (ctrlMod)
                {
                    CommitPrimarySelectionToAdditional();
                    SelectedRow = rIdx;
                    SelEndRow = rIdx;
                    SelectedCol = 1;
                    SelEndCol = CalcMaxCols;
                }
                else
                {
                    ClearAdditionalSelections();
                    SelectedCol = 1;
                    SelEndCol = CalcMaxCols;
                    SelectedRow = rIdx;
                    SelEndRow = rIdx;
                }
                return true;
            }

            // 4a0. Top-Left Select All Triangle Click
            if (ShowHeadings && x >= 0 && x <= headerW && y >= startY && y <= startY + headerH)
            {
                if (IsEditing)
                {
                    if (!CommitCurrentEditCell(true)) { if (parentForm != null) parentForm.Invalidate(); return true; }
                }
                ClearAdditionalSelections();
                SelectedCol = 1;
                SelEndCol = CalcMaxCols;
                SelectedRow = 1;
                SelEndRow = CalcMaxRows;
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
                    if (!CommitCurrentEditCell(true))
                    {
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
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

            // 5b. Check Selection Border MouseDown (Start Dragging to Move / Copy Range)
            if (selW > 0 && selH > 0)
            {
                Rectangle selRect = new Rectangle(selX1, selY1, selW, selH);
                bool onLeft = (Math.Abs(x - selRect.Left) <= 4 && y >= selRect.Top - 2 && y <= selRect.Bottom + 2);
                bool onRight = (Math.Abs(x - selRect.Right) <= 4 && y >= selRect.Top - 2 && y <= selRect.Bottom + 2);
                bool onTop = (Math.Abs(y - selRect.Top) <= 4 && x >= selRect.Left - 2 && x <= selRect.Right + 2);
                bool onBottom = (Math.Abs(y - selRect.Bottom) <= 4 && x >= selRect.Left - 2 && x <= selRect.Right + 2);

                if ((onLeft || onRight || onTop || onBottom) && !(Math.Abs(x - handleX) <= 6 && Math.Abs(y - handleY) <= 6))
                {
                    IsDraggingCellMove = true;
                    MoveSourceStartRow = minSelR;
                    MoveSourceEndRow = maxSelR;
                    MoveSourceStartCol = minSelC;
                    MoveSourceEndCol = maxSelC;
                    MoveTargetRow = minSelR;
                    MoveTargetCol = minSelC;
                    IsCopyMode = Control.ModifierKeys.HasFlag(Keys.Control);
                    return true;
                }
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

                // Point Mode: clicking cell during formula entry inserts/updates reference instead of closing edit
                if (IsEditing && !string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("=") &&
                    (CanAcceptFormulaReferenceBeforeCursor(EditText, EditCursorPos) || IsFormulaPointMode || (ctrlMod && !shiftMod)))
                {
                    if (shiftMod && _pointInsertStart >= 0)
                    {
                        _pointCursorRow = rIdx;
                        _pointCursorCol = cIdx;
                        BeginOrUpdatePointRange(_pointAnchorRow, _pointAnchorCol, rIdx, cIdx, false);
                    }
                    else
                    {
                        _pointAnchorRow = rIdx;
                        _pointAnchorCol = cIdx;
                        _pointCursorRow = rIdx;
                        _pointCursorCol = cIdx;
                        BeginOrUpdatePointRange(rIdx, cIdx, rIdx, cIdx, ctrlMod);
                    }
                    IsSelectingPointRange = true;
                    return true;
                }

                if (IsEditing)
                {
                    if (!CommitCurrentEditCell(true))
                    {
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                if (shiftMod)
                {
                    SelEndCol = cIdx;
                    SelEndRow = rIdx;
                    IsSelectingRange = true;
                    return true;
                }
                else if (ctrlMod)
                {
                    CommitPrimarySelectionToAdditional();
                    SelectedCol = cIdx;
                    SelectedRow = rIdx;
                    SelEndCol = cIdx;
                    SelEndRow = rIdx;
                    IsSelectingRange = true;
                    return true;
                }
                else
                {
                    ClearAdditionalSelections();
                    SelectedCol = cIdx;
                    SelectedRow = rIdx;
                    SelEndCol = cIdx;
                    SelEndRow = rIdx;
                    IsSelectingRange = true;
                    return false;
                }
            }

            return false;
        }

        public void HandleMouseMove(int x, int y, int width, int height, MouseButtons btn)
        {
            LastMousePos = new Point(x, y);

            // Highlight Formula Suggestions on Hover
            if (IsEditing && EditText != null && EditText.StartsWith("=") && ActiveSuggestions != null && ActiveSuggestions.Count > 0 && !FormulaSuggestionsBounds.IsEmpty && FormulaSuggestionsBounds.Contains(x, y))
            {
                int relY = y - (FormulaSuggestionsBounds.Y + 3);
                int hoverIdx = relY / 26;
                int maxCount = Math.Min(6, ActiveSuggestions.Count);
                if (hoverIdx >= 0 && hoverIdx < maxCount)
                {
                    SelectedSuggestionIndex = hoverIdx;
                }
                return;
            }
            int fBarY = 155;
            int btnStartX = 8 + 75 + 12;

            IsHoveringCancel = (y >= fBarY + 3 && y <= fBarY + 26 && x >= btnStartX && x <= btnStartX + 23);
            IsHoveringEnter = (y >= fBarY + 3 && y <= fBarY + 26 && x >= btnStartX + 25 && x <= btnStartX + 48);
            IsHoveringFx = (y >= fBarY + 3 && y <= fBarY + 26 && x >= btnStartX + 50 && x <= btnStartX + 76);
            IsHoveringNameBox = (y >= fBarY + 3 && y <= fBarY + 26 && x >= 8 && x <= 83);
            IsHoveringCellDropdown = (!ActiveCellDropdownButtonBounds.IsEmpty && ActiveCellDropdownButtonBounds.Contains(x, y));
            IsHoveringFormulaChevron = (!FormulaBarChevronBounds.IsEmpty && FormulaBarChevronBounds.Contains(x, y));

            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = fBarY + FormulaBarHeight;

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
                float z = ZoomFactor > 0 ? ZoomFactor : 1.0f;
                int delta = (int)Math.Round((x - ResizeStartMouse.X) / z);
                int newWidth = Math.Max(20, ResizeStartSize + delta);
                CustomColWidths[ResizingColIndex] = newWidth;
                return;
            }

            // Handle Row Resize Dragging
            if (IsResizingRow)
            {
                float z = ZoomFactor > 0 ? ZoomFactor : 1.0f;
                int delta = (int)Math.Round((y - ResizeStartMouse.Y) / z);
                int newHeight = Math.Max(12, ResizeStartSize + delta);
                CustomRowHeights[ResizingRowIndex] = newHeight;
                return;
            }

            // Handle Vertical Scroll Dragging
            if (IsDraggingVertScroll)
            {
                int deltaY = y - ScrollDragStartPos;
                uint maxR = Math.Max(100, CurrentSheet != null ? CurrentSheet.GetMaxRow() + 30 : 100);
                int vScrollH = Math.Max(50, (height - 56) - (startY + headerH));
                int vTrackAvail = Math.Max(1, vScrollH - 32 - Math.Max(25, (int)((vScrollH - 32) * 0.25f)));
                int rowDelta = (int)Math.Round((float)deltaY / vTrackAvail * maxR);
                ScrollRow = (uint)Math.Max(1, Math.Min(maxR, (int)ScrollDragStartVal + rowDelta));
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

            // Handle Dragging Cell Move / Copy
            if (IsDraggingCellMove && btn == MouseButtons.Left)
            {
                uint curC = GetColAtX(x, headerW);
                uint curR = GetRowAtY(y, startY + headerH);
                MoveTargetRow = Math.Max(1, curR);
                MoveTargetCol = Math.Max(1, curC);
                IsCopyMode = Control.ModifierKeys.HasFlag(Keys.Control);
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

            // Handle Fill Handle Dragging (all 4 directions: Down, Up, Right, Left)
            if (IsDraggingFillHandle && btn == MouseButtons.Left)
            {
                AutoScrollDuringDrag(x, y, width, height, startY + headerH, headerW);
                uint curC = GetColAtX(x, headerW);
                uint curR = GetRowAtY(y, startY + headerH);

                int downDist = curR > FillEndRow ? (int)(curR - FillEndRow) : 0;
                int upDist = curR < FillStartRow ? (int)(FillStartRow - curR) : 0;
                int rightDist = curC > FillEndCol ? (int)(curC - FillEndCol) : 0;
                int leftDist = curC < FillStartCol ? (int)(FillStartCol - curC) : 0;

                int maxVert = Math.Max(downDist, upDist);
                int maxHorz = Math.Max(rightDist, leftDist);

                if (maxHorz > maxVert && maxHorz > 0)
                {
                    FillDragTargetCol = curC;
                    FillDragTargetRow = FillEndRow;
                }
                else if (maxVert > 0)
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

            if (IsSelectingPointRange && btn == MouseButtons.Left)
            {
                AutoScrollDuringDrag(x, y, width, height, startY + headerH, headerW);
                uint curR = GetRowAtY(y, startY + headerH);
                uint curC = GetColAtX(x, headerW);
                if (curR != _pointCursorRow || curC != _pointCursorCol)
                {
                    _pointCursorRow = curR;
                    _pointCursorCol = curC;
                    BeginOrUpdatePointRange(_pointAnchorRow, _pointAnchorCol, curR, curC, false);
                }
                return;
            }

            // Handle Marquee Range Selection Dragging (Chỉ quét khối chọn ô, không tự tạo hay mở rộng bảng)
            if (IsSelectingRange && btn == MouseButtons.Left)
            {
                AutoScrollDuringDrag(x, y, width, height, startY + headerH, headerW);
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
            if (IsDraggingCellMove)
            {
                IsDraggingCellMove = false;
                if (MoveTargetRow > 0 && MoveTargetCol > 0 && (MoveTargetRow != MoveSourceStartRow || MoveTargetCol != MoveSourceStartCol))
                {
                    ExecuteMoveRange(MoveSourceStartRow, MoveSourceStartCol, MoveSourceEndRow, MoveSourceEndCol, MoveTargetRow, MoveTargetCol, IsCopyMode);
                }
            }
            if (IsSelectingRange) { IsSelectingRange = false; }
            if (IsSelectingPointRange) { IsSelectingPointRange = false; }
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
            int startY = fBarY + FormulaBarHeight;

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
                // Double-click Fill Handle -> auto-fill down matching adjacent column
                uint minSelR = Math.Min(SelectedRow, SelEndRow);
                uint maxSelR = Math.Max(SelectedRow, SelEndRow);
                uint minSelC = Math.Min(SelectedCol, SelEndCol);
                uint maxSelC = Math.Max(SelectedCol, SelEndCol);
                if (maxSelC >= ScrollCol && maxSelR >= ScrollRow)
                {
                    int handleX = headerW;
                    for (uint c = ScrollCol; c <= maxSelC; c++) handleX += GetColWidth(c);
                    int handleY = startY + headerH;
                    for (uint r = ScrollRow; r <= maxSelR; r++) handleY += GetRowHeight(r);
                    if (Math.Abs(x - handleX) <= 7 && Math.Abs(y - handleY) <= 7)
                    {
                        DoubleClickFillHandle();
                        return;
                    }
                }

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
                FormulaSourceSheetIndex = -1;
                IsFormulaPointMode = false;
                FilterFormulaSuggestions();
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
            ContextMenuStrip cms = new ZenQOffice.UI.FluentContextMenuStrip();
            cms.Font = new Font("Segoe UI", 9f);

            cms.Items.Add(AppL10n.T("📊 Chỉnh sửa số liệu biểu đồ...", "📊 Edit Chart Data..."), null, (s, e) =>
            {
                OpenChartEditor(shape);
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Items.Add(AppL10n.T("🔄 Thay đổi loại biểu đồ (Change Chart Type)...", "🔄 Change Chart Type..."), null, (s, e) =>
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
            cms.Items.Add(AppL10n.T("🗑️ Xóa biểu đồ", "🗑️ Delete Chart"), null, (s, e) =>
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
                this.Invalidate();
            }))
            {
                dlg.ShowDialog();
            }
            this.Invalidate();
        }

        public void ShowSmartArtContextMenu(int x, int y, DocShapeObject shape, Control parentControl)
        {
            if (shape == null) return;
            ContextMenuStrip cms = new ZenQOffice.UI.FluentContextMenuStrip();
            cms.Font = new Font("Segoe UI", 9f);

            cms.Items.Add(AppL10n.T("✏️ Chỉnh sửa nội dung chữ SmartArt...", "✏️ Edit SmartArt Text..."), null, (s, e) =>
            {
                OpenSmartArtEditor(shape);
                if (parentControl != null) parentControl.Invalidate();
            });

            ToolStripMenuItem colorMenu = new ToolStripMenuItem(AppL10n.T("🎨 Đổi màu sắc SmartArt (Color Palette)", "🎨 Change SmartArt Colors"));
            colorMenu.DropDownItems.Add(AppL10n.T("🌈 Đa sắc màu (Colorful Accent)", "🌈 Colorful Accent"), null, (s, e) => { shape.StylePresetIndex = 0; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add(AppL10n.T("🟠 Cam & Vàng (Office Warm)", "🟠 Office Warm (Orange & Gold)"), null, (s, e) => { shape.StylePresetIndex = 1; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add(AppL10n.T("🔵 Xanh Lam & Lục (Colorful Cool)", "🔵 Colorful Cool (Blue & Teal)"), null, (s, e) => { shape.StylePresetIndex = 2; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add(AppL10n.T("🔷 Xanh Lam Chuẩn (Office Blue)", "🔷 Office Blue"), null, (s, e) => { shape.StylePresetIndex = 3; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add(AppL10n.T("🟢 Xanh Lục (Emerald Green)", "🟢 Emerald Green"), null, (s, e) => { shape.StylePresetIndex = 4; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add(AppL10n.T("🟣 Tím & Hồng (Purple & Magenta)", "🟣 Purple & Magenta"), null, (s, e) => { shape.StylePresetIndex = 5; if (parentControl != null) parentControl.Invalidate(); });
            colorMenu.DropDownItems.Add(AppL10n.T("⚫ Đơn sắc (Monochrome Gray)", "⚫ Monochrome Gray"), null, (s, e) => { shape.StylePresetIndex = 6; if (parentControl != null) parentControl.Invalidate(); });
            cms.Items.Add(colorMenu);

            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(AppL10n.T("🗑️ Xóa sơ đồ SmartArt", "🗑️ Delete SmartArt"), null, (s, e) =>
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
            // Name Box click (dropdown arrow vs inline GoTo input)
            if (_lastNameBoxRect.Contains(x, y))
            {
                if (x >= _lastNameBoxRect.Right - 16)
                {
                    ShowNameBoxDropdownMenu();
                }
                else
                {
                    if (IsEditing) CommitCurrentEditCell(true);
                    IsEditingNameBox = true;
                    NameBoxEditText = GetDisplayNameBoxText();
                    if (NameBoxEditText.Contains("R ×") || NameBoxEditText.Contains("R x"))
                    {
                        NameBoxEditText = string.Format("{0}{1}:{2}{3}",
                            SparseMatrix.ColIndexToName(Math.Min(SelectedCol, SelEndCol)),
                            Math.Min(SelectedRow, SelEndRow),
                            SparseMatrix.ColIndexToName(Math.Max(SelectedCol, SelEndCol)),
                            Math.Max(SelectedRow, SelEndRow));
                    }
                    NameBoxCursorPos = NameBoxEditText.Length;
                }
                return;
            }

            if (IsEditingNameBox)
            {
                ExecuteGoToTarget(NameBoxEditText);
                IsEditingNameBox = false;
            }

            // Cancel (✕)
            if (_lastCancelBtnRect.Contains(x, y))
            {
                CancelEdit();
                return;
            }

            // Enter (✓)
            if (_lastEnterBtnRect.Contains(x, y))
            {
                if (IsEditing) CommitCurrentEditCell(true);
                return;
            }

            // Insert Function (fx)
            if (_lastFxBtnRect.Contains(x, y))
            {
                if (IsEditing && !string.IsNullOrEmpty(EditText) && !EditText.TrimStart().StartsWith("="))
                {
                    CommitCurrentEditCell(false);
                }
                using (InsertFunctionDialog ifd = new InsertFunctionDialog(this))
                {
                    ifd.ShowDialog();
                }
                return;
            }

            // Click Formula Input Area -> enter/focus Edit Mode & position caret
            if (!FormulaBarChevronBounds.IsEmpty && FormulaBarChevronBounds.Contains(x, y))
            {
                IsFormulaBarExpanded = !IsFormulaBarExpanded;
                return;
            }
            if (_lastFormulaInputRect.Contains(x, y) || x >= _lastFxBtnRect.Right)
            {
                if (!IsEditing)
                {
                    CellData cur = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                    string raw = (cur != null) ? (cur.Type == CellType.Formula ? cur.FormulaStr : cur.RawValue) : "";
                    StartEditingCell(raw ?? "");
                }
                IsFormulaPointMode = false;
                _pointInsertStart = -1;
                _pointInsertLength = 0;

                // Estimate caret index from X coordinate inside formula input box
                int relX = Math.Max(0, x - (_lastFormulaInputRect.X + 6));
                int estPos = (EditText ?? "").Length;
                using (Bitmap bmp = new Bitmap(1, 1))
                using (Graphics gTmp = Graphics.FromImage(bmp))
                using (Font fnt = new Font("Segoe UI", 9.5f))
                {
                    string s = EditText ?? "";
                    int bestIdx = s.Length;
                    float bestDiff = float.MaxValue;
                    for (int i = 0; i <= s.Length; i++)
                    {
                        float w = gTmp.MeasureString(s.Substring(0, i), fnt, 4000, StringFormat.GenericTypographic).Width;
                        float d = Math.Abs(w - relX);
                        if (d < bestDiff)
                        {
                            bestDiff = d;
                            bestIdx = i;
                        }
                    }
                    estPos = bestIdx;
                }
                EditCursorPos = estPos;
                EditSelStart = EditCursorPos;
                EditSelLength = 0;
                FilterFormulaSuggestions();
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

            uint srcRowCount = srcMaxR - srcMinR + 1;
            uint srcColCount = srcMaxC - srcMinC + 1;

            // Vertical Fill Down
            if (FillDragTargetRow > srcMaxR)
            {
                for (uint c = srcMinC; c <= srcMaxC; c++)
                {
                    var numericSeries = new List<double>();
                    bool isNumericCol = true;
                    for (uint r = srcMinR; r <= srcMaxR; r++)
                    {
                        CellData sc = CurrentSheet.GetCell(r, c);
                        if (sc != null && sc.Type == CellType.Number) numericSeries.Add(sc.NumericValue);
                        else { isNumericCol = false; break; }
                    }

                    double numStep = (numericSeries.Count >= 2) ? (numericSeries[1] - numericSeries[0]) : 1.0;

                    for (uint tgtR = srcMaxR + 1; tgtR <= FillDragTargetRow; tgtR++)
                    {
                        uint relIdx = (tgtR - srcMinR) % srcRowCount;
                        uint templateR = srcMinR + relIdx;
                        int rowOffset = (int)(tgtR - templateR);

                        CellData templateCell = CurrentSheet.GetCell(templateR, c);
                        if (templateCell == null) continue;

                        if (templateCell.Type == CellType.Formula && !string.IsNullOrEmpty(templateCell.FormulaStr))
                        {
                            string shiftedFormula = ShiftFormulaReferences(templateCell.FormulaStr, rowOffset, 0);
                            CurrentSheet.SetCellText(tgtR, c, shiftedFormula);
                        }
                        else if (isNumericCol && numericSeries.Count >= 1)
                        {
                            double nextVal = numericSeries[numericSeries.Count - 1] + numStep * (tgtR - srcMaxR);
                            CurrentSheet.SetCellText(tgtR, c, nextVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            string srcText = templateCell.RawValue ?? "";
                            string filledText = ExtrapolateStringPattern(srcText, (int)((tgtR - srcMinR) / srcRowCount));
                            CurrentSheet.SetCellText(tgtR, c, filledText);
                        }

                        if (templateCell.Format != null)
                        {
                            CellData targetCell = CurrentSheet.GetOrCreateCell(tgtR, c);
                            targetCell.Format = templateCell.Format.Clone();
                        }
                    }
                }
                SelectedRow = srcMinR;
                SelEndRow = FillDragTargetRow;
                SelectedCol = srcMinC;
                SelEndCol = srcMaxC;
            }
            // Vertical Fill Up
            else if (FillDragTargetRow < srcMinR && FillDragTargetRow >= 1)
            {
                for (uint c = srcMinC; c <= srcMaxC; c++)
                {
                    var numericSeries = new List<double>();
                    bool isNumericCol = true;
                    for (uint r = srcMinR; r <= srcMaxR; r++)
                    {
                        CellData sc = CurrentSheet.GetCell(r, c);
                        if (sc != null && sc.Type == CellType.Number) numericSeries.Add(sc.NumericValue);
                        else { isNumericCol = false; break; }
                    }

                    double numStep = (numericSeries.Count >= 2) ? (numericSeries[1] - numericSeries[0]) : 1.0;

                    for (uint tgtR = srcMinR - 1; tgtR >= FillDragTargetRow; tgtR--)
                    {
                        uint dist = srcMinR - tgtR;
                        uint relIdx = (srcRowCount - 1) - ((dist - 1) % srcRowCount);
                        uint templateR = srcMinR + relIdx;
                        int rowOffset = (int)tgtR - (int)templateR;

                        CellData templateCell = CurrentSheet.GetCell(templateR, c);
                        if (templateCell == null)
                        {
                            if (tgtR == FillDragTargetRow) break;
                            continue;
                        }

                        if (templateCell.Type == CellType.Formula && !string.IsNullOrEmpty(templateCell.FormulaStr))
                        {
                            string shiftedFormula = ShiftFormulaReferences(templateCell.FormulaStr, rowOffset, 0);
                            CurrentSheet.SetCellText(tgtR, c, shiftedFormula);
                        }
                        else if (isNumericCol && numericSeries.Count >= 1)
                        {
                            double prevVal = numericSeries[0] - numStep * dist;
                            CurrentSheet.SetCellText(tgtR, c, prevVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            string srcText = templateCell.RawValue ?? "";
                            int stepBack = -((int)((dist - 1) / srcRowCount) + 1);
                            string filledText = ExtrapolateStringPattern(srcText, stepBack);
                            CurrentSheet.SetCellText(tgtR, c, filledText);
                        }

                        if (templateCell.Format != null)
                        {
                            CellData targetCell = CurrentSheet.GetOrCreateCell(tgtR, c);
                            targetCell.Format = templateCell.Format.Clone();
                        }

                        if (tgtR == FillDragTargetRow) break;
                    }
                }
                SelectedRow = FillDragTargetRow;
                SelEndRow = srcMaxR;
                SelectedCol = srcMinC;
                SelEndCol = srcMaxC;
            }
            // Horizontal Fill Right
            else if (FillDragTargetCol > srcMaxC)
            {
                for (uint r = srcMinR; r <= srcMaxR; r++)
                {
                    var numericSeries = new List<double>();
                    bool isNumericRow = true;
                    for (uint c = srcMinC; c <= srcMaxC; c++)
                    {
                        CellData sc = CurrentSheet.GetCell(r, c);
                        if (sc != null && sc.Type == CellType.Number) numericSeries.Add(sc.NumericValue);
                        else { isNumericRow = false; break; }
                    }

                    double numStep = (numericSeries.Count >= 2) ? (numericSeries[1] - numericSeries[0]) : 1.0;

                    for (uint tgtC = srcMaxC + 1; tgtC <= FillDragTargetCol; tgtC++)
                    {
                        uint relIdx = (tgtC - srcMinC) % srcColCount;
                        uint templateC = srcMinC + relIdx;
                        int colOffset = (int)(tgtC - templateC);

                        CellData templateCell = CurrentSheet.GetCell(r, templateC);
                        if (templateCell == null) continue;

                        if (templateCell.Type == CellType.Formula && !string.IsNullOrEmpty(templateCell.FormulaStr))
                        {
                            string shiftedFormula = ShiftFormulaReferences(templateCell.FormulaStr, 0, colOffset);
                            CurrentSheet.SetCellText(r, tgtC, shiftedFormula);
                        }
                        else if (isNumericRow && numericSeries.Count >= 1)
                        {
                            double nextVal = numericSeries[numericSeries.Count - 1] + numStep * (tgtC - srcMaxC);
                            CurrentSheet.SetCellText(r, tgtC, nextVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            string srcText = templateCell.RawValue ?? "";
                            string filledText = ExtrapolateStringPattern(srcText, (int)((tgtC - srcMinC) / srcColCount));
                            CurrentSheet.SetCellText(r, tgtC, filledText);
                        }

                        if (templateCell.Format != null)
                        {
                            CellData targetCell = CurrentSheet.GetOrCreateCell(r, tgtC);
                            targetCell.Format = templateCell.Format.Clone();
                        }
                    }
                }
                SelectedRow = srcMinR;
                SelEndRow = srcMaxR;
                SelectedCol = srcMinC;
                SelEndCol = FillDragTargetCol;
            }
            // Horizontal Fill Left
            else if (FillDragTargetCol < srcMinC && FillDragTargetCol >= 1)
            {
                for (uint r = srcMinR; r <= srcMaxR; r++)
                {
                    var numericSeries = new List<double>();
                    bool isNumericRow = true;
                    for (uint c = srcMinC; c <= srcMaxC; c++)
                    {
                        CellData sc = CurrentSheet.GetCell(r, c);
                        if (sc != null && sc.Type == CellType.Number) numericSeries.Add(sc.NumericValue);
                        else { isNumericRow = false; break; }
                    }

                    double numStep = (numericSeries.Count >= 2) ? (numericSeries[1] - numericSeries[0]) : 1.0;

                    for (uint tgtC = srcMinC - 1; tgtC >= FillDragTargetCol; tgtC--)
                    {
                        uint dist = srcMinC - tgtC;
                        uint relIdx = (srcColCount - 1) - ((dist - 1) % srcColCount);
                        uint templateC = srcMinC + relIdx;
                        int colOffset = (int)tgtC - (int)templateC;

                        CellData templateCell = CurrentSheet.GetCell(r, templateC);
                        if (templateCell == null)
                        {
                            if (tgtC == FillDragTargetCol) break;
                            continue;
                        }

                        if (templateCell.Type == CellType.Formula && !string.IsNullOrEmpty(templateCell.FormulaStr))
                        {
                            string shiftedFormula = ShiftFormulaReferences(templateCell.FormulaStr, 0, colOffset);
                            CurrentSheet.SetCellText(r, tgtC, shiftedFormula);
                        }
                        else if (isNumericRow && numericSeries.Count >= 1)
                        {
                            double prevVal = numericSeries[0] - numStep * dist;
                            CurrentSheet.SetCellText(r, tgtC, prevVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            string srcText = templateCell.RawValue ?? "";
                            int stepBack = -((int)((dist - 1) / srcColCount) + 1);
                            string filledText = ExtrapolateStringPattern(srcText, stepBack);
                            CurrentSheet.SetCellText(r, tgtC, filledText);
                        }

                        if (templateCell.Format != null)
                        {
                            CellData targetCell = CurrentSheet.GetOrCreateCell(r, tgtC);
                            targetCell.Format = templateCell.Format.Clone();
                        }

                        if (tgtC == FillDragTargetCol) break;
                    }
                }
                SelectedRow = srcMinR;
                SelEndRow = srcMaxR;
                SelectedCol = FillDragTargetCol;
                SelEndCol = srcMaxC;
            }

            RecalcAfterEdit();
        }

        public string ShiftFormulaReferences(string formula, int rowOffset, int colOffset)
        {
            return AdjustFormulaOffset(formula, rowOffset, colOffset);
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
            if (IsEditingNameBox)
            {
                if (ch == '\r' || ch == '\n')
                {
                    ExecuteGoToTarget(NameBoxEditText);
                    IsEditingNameBox = false;
                }
                else if (ch == 27) // Escape
                {
                    IsEditingNameBox = false;
                }
                else if (ch == '\b' && NameBoxCursorPos > 0 && !string.IsNullOrEmpty(NameBoxEditText))
                {
                    int p = Math.Min(NameBoxCursorPos, NameBoxEditText.Length);
                    NameBoxEditText = NameBoxEditText.Remove(p - 1, 1);
                    NameBoxCursorPos = p - 1;
                }
                else if (!char.IsControl(ch))
                {
                    string nb = NameBoxEditText ?? "";
                    int pos = Math.Max(0, Math.Min(nb.Length, NameBoxCursorPos));
                    NameBoxEditText = nb.Insert(pos, ch.ToString());
                    NameBoxCursorPos = pos + 1;
                }
                return;
            }

            if (IsSheetProtected && ch != '\r' && ch != '\t')
            {
                ShowSheetProtectedWarning();
                return;
            }
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

            if (ch == '\r' || ch == '\n')
            {
                if (alt && IsEditing)
                {
                    return; // Handled in HandleKeyDown (Alt+Enter newline)
                }
                if (ctrl && IsEditing)
                {
                    return; // Handled in HandleKeyDown (Ctrl+Enter fill selection)
                }
                CommitEditAndMove(shift ? -1 : 1, 0);
            }
            else if (ch == '\t')
            {
                if (IsEditing && !string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("=") && ActiveSuggestions != null && ActiveSuggestions.Count > 0)
                {
                    CommitFormulaSuggestion(ActiveSuggestions[Math.Max(0, Math.Min(ActiveSuggestions.Count - 1, SelectedSuggestionIndex))]);
                }
                else
                {
                    CommitEditAndMove(0, shift ? -1 : 1);
                }
            }
            else if (ctrl)
            {
                return; // Ignore control characters when Ctrl is held
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
                _pointInsertStart = -1;
                _pointInsertLength = 0;
                FilterFormulaSuggestions(EditText);
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

                // Vietnamese Auto-Correction (Telex / VNI engine for Calc)
                if (VietnameseAutoCorrect.IsEnabled && !EditText.StartsWith("="))
                {
                    if (VietnameseAutoCorrect.IsWordDelimiter(ch))
                    {
                        string wordBefore = GetWordBeforePos(EditText, EditCursorPos);
                        string correctedWord;
                        if (!string.IsNullOrEmpty(wordBefore) && VietnameseAutoCorrect.TryCorrectWord(wordBefore, out correctedWord))
                        {
                            int wordStart = EditCursorPos - wordBefore.Length;
                            EditText = EditText.Remove(wordStart, wordBefore.Length).Insert(wordStart, correctedWord);
                            EditCursorPos = wordStart + correctedWord.Length;
                        }
                    }
                    else if (!char.IsControl(ch))
                    {
                        string wordBefore = GetWordBeforePos(EditText, EditCursorPos);
                        int charsToRemove;
                        string textToInsert;
                        if (!string.IsNullOrEmpty(wordBefore) && VietnameseAutoCorrect.TryProcessChar(wordBefore, ch, out charsToRemove, out textToInsert))
                        {
                            int removeStart = EditCursorPos - charsToRemove;
                            EditText = EditText.Remove(removeStart, charsToRemove).Insert(removeStart, textToInsert);
                            EditCursorPos = removeStart + textToInsert.Length;
                            return;
                        }
                    }
                }

                EditText = EditText.Insert(EditCursorPos, ch.ToString());
                EditCursorPos++;

                if (!string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("=") &&
                    ("+-*/^&=<>(),;:".IndexOf(ch) >= 0))
                {
                    IsFormulaPointMode = true;
                    _pointInsertStart = -1;
                    _pointInsertLength = 0;
                }
                else
                {
                    _pointInsertStart = -1;
                    _pointInsertLength = 0;
                    IsFormulaPointMode = false;
                }
                FilterFormulaSuggestions(EditText);
            }
        }

        private string GetWordBeforePos(string text, int pos)
        {
            if (string.IsNullOrEmpty(text) || pos <= 0 || pos > text.Length) return "";
            int start = pos - 1;
            while (start >= 0 && !VietnameseAutoCorrect.IsWordDelimiter(text[start]))
            {
                start--;
            }
            start++;
            if (start >= pos) return "";
            return text.Substring(start, pos - start);
        }

        public string GetCurrentWordBeforeCursor(out int wordStart)
        {
            if (!IsEditing || string.IsNullOrEmpty(EditText))
            {
                wordStart = -1;
                return "";
            }
            return VietnameseWordPredictor.GetWordBeforePos(EditText, EditCursorPos, out wordStart);
        }

        public void ReplaceWordAtCursor(string newWord)
        {
            if (!IsEditing || string.IsNullOrEmpty(EditText)) return;
            int wordStart;
            string wordBefore = VietnameseWordPredictor.GetWordBeforePos(EditText, EditCursorPos, out wordStart);
            if (!string.IsNullOrEmpty(wordBefore) && wordStart >= 0)
            {
                EditText = EditText.Remove(wordStart, wordBefore.Length).Insert(wordStart, newWord + " ");
                EditCursorPos = wordStart + newWord.Length + 1;
                EditSelLength = 0;
            }
        }

        public void CommitEditAndMove(int deltaRow, int deltaCol)
        {
            if (IsEditing)
            {
                SaveUndoState();
                string finalVal = EditText;
                if (VietnameseAutoCorrect.IsEnabled && !string.IsNullOrEmpty(finalVal) && !finalVal.StartsWith("="))
                {
                    finalVal = VietnameseAutoCorrect.CorrectText(finalVal);
                    EditText = finalVal;
                }

                // Chế độ Máy quét Mã vạch POS / Kho (Barcode Scanner Mode)
                if (AppSettings.Instance.CalcBarcodeScannerMode && deltaRow > 0 && !string.IsNullOrWhiteSpace(finalVal) && !finalVal.StartsWith("="))
                {
                    Func<uint, uint, string> readCellTxt = (rr, cc) =>
                    {
                        var cd = CurrentSheet.GetCell(rr, cc);
                        if (cd == null) return "";
                        return !string.IsNullOrEmpty(cd.EvaluatedStr) ? cd.EvaluatedStr : (cd.RawValue ?? "");
                    };

                    string code = finalVal.Trim();
                    uint existingRow = 0;
                    for (uint r = 1; r < SelectedRow; r++)
                    {
                        string prevCode = readCellTxt(r, SelectedCol).Trim();
                        if (string.Equals(prevCode, code, StringComparison.OrdinalIgnoreCase))
                        {
                            existingRow = r;
                            break;
                        }
                    }

                    string nowStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    if (existingRow > 0)
                    {
                        // Đã có mã vạch này trong danh sách -> Tự động cộng dồn Số lượng (+1) & cập nhật giờ quét!
                        double curQty = 0;
                        double.TryParse(readCellTxt(existingRow, SelectedCol + 1), out curQty);
                        CurrentSheet.SetCellText(existingRow, SelectedCol + 1, (curQty + 1).ToString());
                        CurrentSheet.SetCellText(existingRow, SelectedCol + 2, nowStamp);
                        CurrentSheet.SetCellText(SelectedRow, SelectedCol, "");
                        RecalcAfterEdit(true);
                        IsEditing = false;
                        EditText = "";
                        EditSelLength = 0;
                        EnsureCellVisible(existingRow, SelectedCol);
                        return;
                    }
                    else
                    {
                        // Mã vạch mới -> Ghi mã, điền Số lượng = 1 và Thời gian quét vào 2 cột kế bên
                        CurrentSheet.SetCellText(SelectedRow, SelectedCol, code);
                        if (string.IsNullOrWhiteSpace(readCellTxt(SelectedRow, SelectedCol + 1)))
                            CurrentSheet.SetCellText(SelectedRow, SelectedCol + 1, "1");
                        if (string.IsNullOrWhiteSpace(readCellTxt(SelectedRow, SelectedCol + 2)))
                            CurrentSheet.SetCellText(SelectedRow, SelectedCol + 2, nowStamp);
                    }
                }
                else
                {
                    if (!CommitCurrentEditCell(true)) return;
                }
            }
            SelectedRow = (uint)Math.Max(1, (int)SelectedRow + deltaRow);
            SelectedCol = (uint)Math.Max(1, (int)SelectedCol + deltaCol);
            SelEndRow = SelectedRow;
            SelEndCol = SelectedCol;
            EnsureCellVisible(SelectedRow, SelectedCol);
        }

        public void CancelEdit()
        {
            if (FormulaSourceSheetIndex >= 0 && FormulaSourceSheetIndex != ActiveSheetIndex && FormulaSourceSheetIndex < Sheets.Count)
            {
                SaveCurrentSheetState();
                _activeSheetIndex = FormulaSourceSheetIndex;
                LoadSheetState(FormulaSourceSheetIndex);
                SelectedRow = FormulaSourceRow;
                SelectedCol = FormulaSourceCol;
                SelEndRow = FormulaSourceRow;
                SelEndCol = FormulaSourceCol;
            }
            FormulaSourceSheetIndex = -1;
            IsFormulaPointMode = false;
            IsSelectingPointRange = false;
            _pointInsertStart = -1;
            _pointInsertLength = 0;
            IsEditing = false;
            EditText = "";
            EditSelLength = 0;
        }

        public void CycleFormulaRefAtCursor()
        {
            if (string.IsNullOrEmpty(EditText) || !EditText.TrimStart().StartsWith("=")) return;
            int pos = Math.Min(EditCursorPos, EditText.Length);

            var matches = System.Text.RegularExpressions.Regex.Matches(
                EditText,
                @"(?<![A-Za-z0-9_.])(\$?[A-Za-z]{1,3}\$?[0-9]{1,7}(?::\$?[A-Za-z]{1,3}\$?[0-9]{1,7})?)(?![A-Za-z0-9_(])");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (pos >= m.Index && pos <= m.Index + m.Length)
                {
                    string[] parts = m.Value.Split(':');
                    string cycledFirst = ToggleSingleRefAnchor(parts[0]);
                    int mode = DetectRefAnchorMode(cycledFirst);
                    string replacement = cycledFirst;
                    if (parts.Length == 2)
                    {
                        replacement += ":" + ApplyRefAnchorMode(parts[1], mode);
                    }

                    EditText = EditText.Substring(0, m.Index) + replacement + EditText.Substring(m.Index + m.Length);
                    EditCursorPos = m.Index + replacement.Length;
                    EditSelStart = EditCursorPos;
                    EditSelLength = 0;
                    _pointInsertStart = m.Index;
                    _pointInsertLength = replacement.Length;
                    break;
                }
            }
        }

        private static int DetectRefAnchorMode(string refToken)
        {
            var rm = System.Text.RegularExpressions.Regex.Match(refToken, @"^(\$?)([A-Za-z]+)(\$?)([0-9]+)$");
            if (!rm.Success) return 0;
            bool c = !string.IsNullOrEmpty(rm.Groups[1].Value);
            bool r = !string.IsNullOrEmpty(rm.Groups[3].Value);
            if (c && r) return 1;
            if (!c && r) return 2;
            if (c && !r) return 3;
            return 0;
        }

        private static string ApplyRefAnchorMode(string refToken, int mode)
        {
            var rm = System.Text.RegularExpressions.Regex.Match(refToken, @"^\$?([A-Za-z]+)\$?([0-9]+)$");
            if (!rm.Success) return refToken;
            string col = rm.Groups[1].Value;
            string row = rm.Groups[2].Value;
            switch (mode)
            {
                case 1: return "$" + col + "$" + row;
                case 2: return col + "$" + row;
                case 3: return "$" + col + row;
                default: return col + row;
            }
        }

        private static string ToggleSingleRefAnchor(string refToken)
        {
            int curMode = DetectRefAnchorMode(refToken);
            int nextMode = (curMode + 1) % 4;
            return ApplyRefAnchorMode(refToken, nextMode);
        }

        private void JumpToDataEdge(uint curR, uint curC, int dRow, int dCol, out uint targetR, out uint targetC)
        {
            targetR = curR;
            targetC = curC;
            if (CurrentSheet == null) return;

            uint maxSheetRow = 500;
            uint maxSheetCol = 52;
            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                if (kvp.Value != null && (!string.IsNullOrEmpty(kvp.Value.RawValue) || !string.IsNullOrEmpty(kvp.Value.FormulaStr)))
                {
                    var pos = SparseMatrix.DecodeKey(kvp.Key);
                    if (pos.Item1 > maxSheetRow) maxSheetRow = pos.Item1;
                    if (pos.Item2 > maxSheetCol) maxSheetCol = pos.Item2;
                }
            }

            Func<uint, uint, bool> hasData = (r, c) =>
            {
                CellData cd = CurrentSheet.GetCell(r, c);
                return cd != null && (!string.IsNullOrEmpty(cd.RawValue) || !string.IsNullOrEmpty(cd.FormulaStr));
            };

            int nr = (int)curR + dRow;
            int nc = (int)curC + dCol;
            if (nr < 1 || nc < 1 || nr > (int)maxSheetRow || nc > (int)maxSheetCol)
            {
                targetR = (uint)Math.Max(1, Math.Min((int)maxSheetRow, nr));
                targetC = (uint)Math.Max(1, Math.Min((int)maxSheetCol, nc));
                return;
            }

            bool curHas = hasData(curR, curC);
            bool nextHas = hasData((uint)nr, (uint)nc);

            if (curHas && nextHas)
            {
                int r = nr, c = nc;
                while (r + dRow >= 1 && r + dRow <= (int)maxSheetRow && c + dCol >= 1 && c + dCol <= (int)maxSheetCol && hasData((uint)(r + dRow), (uint)(c + dCol)))
                {
                    r += dRow;
                    c += dCol;
                }
                targetR = (uint)r;
                targetC = (uint)c;
            }
            else
            {
                int r = nr, c = nc;
                while (r >= 1 && r <= (int)maxSheetRow && c >= 1 && c <= (int)maxSheetCol && !hasData((uint)r, (uint)c))
                {
                    if (r + dRow < 1 || r + dRow > (int)maxSheetRow || c + dCol < 1 || c + dCol > (int)maxSheetCol)
                        break;
                    r += dRow;
                    c += dCol;
                }
                targetR = (uint)Math.Max(1, Math.Min((int)maxSheetRow, r));
                targetC = (uint)Math.Max(1, Math.Min((int)maxSheetCol, c));
            }
        }

        public void HandleKeyDown(Keys key)
        {
            Keys rawKey = key & Keys.KeyCode;
            bool shift = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) || ((key & Keys.Shift) == Keys.Shift);
            bool ctrl = ((Control.ModifierKeys & Keys.Control) == Keys.Control) || ((key & Keys.Control) == Keys.Control);
            bool alt = ((Control.ModifierKeys & Keys.Alt) == Keys.Alt) || ((key & Keys.Alt) == Keys.Alt);

            if (IsEditingNameBox)
            {
                if (rawKey == Keys.Enter)
                {
                    ExecuteGoToTarget(NameBoxEditText);
                    IsEditingNameBox = false;
                }
                else if (rawKey == Keys.Escape)
                {
                    IsEditingNameBox = false;
                }
                else if (rawKey == Keys.Back && NameBoxCursorPos > 0 && !string.IsNullOrEmpty(NameBoxEditText))
                {
                    int p = Math.Min(NameBoxCursorPos, NameBoxEditText.Length);
                    NameBoxEditText = NameBoxEditText.Remove(p - 1, 1);
                    NameBoxCursorPos = p - 1;
                }
                else if (rawKey == Keys.Left && NameBoxCursorPos > 0) NameBoxCursorPos--;
                else if (rawKey == Keys.Right && NameBoxCursorPos < (NameBoxEditText ?? "").Length) NameBoxCursorPos++;
                return;
            }

            // F5 or Ctrl + G : Go To / Go To Special
            if (rawKey == Keys.F5 || (ctrl && rawKey == Keys.G))
            {
                if (IsEditing) CommitCurrentEditCell(true);
                ShowGoToDialog(Form.ActiveForm);
                return;
            }

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

            // Ctrl + PageUp / PageDown: Switch sheets
            if (ctrl && (rawKey == Keys.Prior || rawKey == Keys.PageUp))
            {
                if (ActiveSheetIndex > 0) SwitchToSheet(ActiveSheetIndex - 1);
                return;
            }
            if (ctrl && (rawKey == Keys.Next || rawKey == Keys.PageDown))
            {
                if (ActiveSheetIndex < Sheets.Count - 1) SwitchToSheet(ActiveSheetIndex + 1);
                return;
            }

            if (ctrl && rawKey == Keys.F)
            {
                OpenFindReplaceDialog(Form.ActiveForm, false);
                return;
            }
            if (ctrl && rawKey == Keys.H)
            {
                OpenFindReplaceDialog(Form.ActiveForm, true);
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
                // F4 in Edit Mode: Toggle absolute/relative reference ($A$1 -> A$1 -> $A1 -> A1)
                if (rawKey == Keys.F4)
                {
                    CycleFormulaRefAtCursor();
                    return;
                }
                // Alt + Enter in Edit Mode: Insert line break inside cell and enable WrapText
                if (alt && rawKey == Keys.Enter)
                {
                    EditText = EditText.Insert(EditCursorPos, "\n");
                    EditCursorPos++;
                    CellData cd = CurrentSheet.GetOrCreateCell(SelectedRow, SelectedCol);
                    if (cd.Format == null) cd.Format = new CellFormat();
                    cd.Format.WrapText = true;
                    return;
                }
                // Ctrl + Enter in Edit Mode: Fill all selected cells with current EditText
                if (ctrl && rawKey == Keys.Enter)
                {
                    SaveUndoState();
                    string finalVal = EditText;
                    uint minR = Math.Min(SelectedRow, SelEndRow);
                    uint maxR = Math.Max(SelectedRow, SelEndRow);
                    uint minC = Math.Min(SelectedCol, SelEndCol);
                    uint maxC = Math.Max(SelectedCol, SelEndCol);
                    for (uint r = minR; r <= maxR; r++)
                    {
                        for (uint c = minC; c <= maxC; c++)
                        {
                            CurrentSheet.SetCellText(r, c, finalVal);
                        }
                    }
                    RecalcAfterEdit(true);
                    IsEditing = false;
                    EditSelLength = 0;
                    return;
                }
                if (ctrl && rawKey == Keys.A)
            {
                ClearAdditionalSelections();
                uint regMinR, regMaxR, regMinC, regMaxC;
                GetContiguousDataRange(SelectedRow, SelectedCol, out regMinR, out regMaxR, out regMinC, out regMaxC);
                bool hasRegion = (regMaxR > regMinR || regMaxC > regMinC) ||
                                 (CurrentSheet.GetCell(SelectedRow, SelectedCol) != null &&
                                  !string.IsNullOrEmpty(CurrentSheet.GetCell(SelectedRow, SelectedCol).RawValue));
                bool alreadyRegion = (Math.Min(SelectedRow, SelEndRow) == regMinR &&
                                      Math.Max(SelectedRow, SelEndRow) == regMaxR &&
                                      Math.Min(SelectedCol, SelEndCol) == regMinC &&
                                      Math.Max(SelectedCol, SelEndCol) == regMaxC);
                if (hasRegion && !alreadyRegion)
                {
                    SelectedRow = regMinR; SelEndRow = regMaxR;
                    SelectedCol = regMinC; SelEndCol = regMaxC;
                }
                else
                {
                    SelectedCol = 1; SelectedRow = 1;
                    SelEndCol = CalcMaxCols; SelEndRow = CalcMaxRows;
                }
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
                        if (EditSelLength > 0 && EditSelStart >= 0 && EditSelStart + EditSelStart <= EditText.Length && EditSelStart + EditSelLength <= EditText.Length)
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
                // Point Mode Keyboard Navigation (Left/Right/Up/Down arrows insert/move ref when at operator)
                if ((rawKey == Keys.Left || rawKey == Keys.Right || rawKey == Keys.Up || rawKey == Keys.Down) &&
                    !string.IsNullOrEmpty(EditText) && EditText.TrimStart().StartsWith("=") &&
                    (IsFormulaPointMode || CanAcceptFormulaReferenceBeforeCursor(EditText, EditCursorPos)))
                {
                    IsFormulaPointMode = true;
                    uint baseR = _pointInsertStart >= 0 ? _pointCursorRow : SelectedRow;
                    uint baseC = _pointInsertStart >= 0 ? _pointCursorCol : SelectedCol;
                    uint nextR = baseR;
                    uint nextC = baseC;
                    if (rawKey == Keys.Up && nextR > 1) nextR--;
                    else if (rawKey == Keys.Down && nextR < CalcMaxRows) nextR++;
                    else if (rawKey == Keys.Left && nextC > 1) nextC--;
                    else if (rawKey == Keys.Right && nextC < CalcMaxCols) nextC++;

                    if (shift && _pointInsertStart >= 0)
                    {
                        _pointCursorRow = nextR;
                        _pointCursorCol = nextC;
                        BeginOrUpdatePointRange(_pointAnchorRow, _pointAnchorCol, nextR, nextC, false);
                    }
                    else
                    {
                        _pointAnchorRow = nextR;
                        _pointAnchorCol = nextC;
                        _pointCursorRow = nextR;
                        _pointCursorCol = nextC;
                        BeginOrUpdatePointRange(nextR, nextC, nextR, nextC, false);
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
                    if (EditText.StartsWith("=") && !EditText.Contains("(") && ActiveSuggestions != null && ActiveSuggestions.Count > 0 && SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < ActiveSuggestions.Count)
                    {
                        EditText = "=" + ActiveSuggestions[SelectedSuggestionIndex].Name + "(";
                        EditCursorPos = EditText.Length;
                        EditSelStart = 0;
                        EditSelLength = 0;
                        FormulaSuggestionsBounds = Rectangle.Empty;
                        return;
                    }
                    CommitEditAndMove(shift ? -1 : 1, 0);
                    return;
                }
                if (rawKey == Keys.Tab)
                {
                    if (EditText.StartsWith("=") && !EditText.Contains("(") && ActiveSuggestions != null && ActiveSuggestions.Count > 0 && SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < ActiveSuggestions.Count)
                    {
                        EditText = "=" + ActiveSuggestions[SelectedSuggestionIndex].Name + "(";
                        EditCursorPos = EditText.Length;
                        EditSelStart = 0;
                        EditSelLength = 0;
                        FormulaSuggestionsBounds = Rectangle.Empty;
                        return;
                    }
                    CommitEditAndMove(0, shift ? -1 : 1);
                    return;
                }
                if (rawKey == Keys.Up && ActiveSuggestions != null && ActiveSuggestions.Count > 0)
                {
                    if (SelectedSuggestionIndex > 0) SelectedSuggestionIndex--;
                    return;
                }
                if (rawKey == Keys.Down && ActiveSuggestions != null && ActiveSuggestions.Count > 0)
                {
                    if (SelectedSuggestionIndex < ActiveSuggestions.Count - 1) SelectedSuggestionIndex++;
                    return;
                }
            }
            else
            {
                if (alt && rawKey == Keys.Down)
                {
                    OpenCellDropdownViewer(Form.ActiveForm);
                    return;
                }
                if (ctrl && shift && rawKey == Keys.U)
                {
                    IsFormulaBarExpanded = !IsFormulaBarExpanded;
                    if (Form.ActiveForm != null) Form.ActiveForm.Invalidate();
                    return;
                }

                // Clipboard & Flash Fill
                if (ctrl && rawKey == Keys.C) { Copy(); return; }
                if (ctrl && rawKey == Keys.X) { Cut(); return; }
                if (ctrl && rawKey == Keys.V) { Paste(); return; }
                if (ctrl && rawKey == Keys.E) { FlashFill(); return; }

                // Ctrl + D: Fill Down | Ctrl + R: Fill Right (MS Excel standard)
                if (ctrl && rawKey == Keys.D)
                {
                    if (SelectedRow == SelEndRow && SelectedRow > 1)
                    {
                        uint targetR = SelectedRow;
                        SelectedRow = targetR - 1;
                        SelEndRow = targetR;
                        FillRange(0);
                        SelectedRow = SelEndRow = targetR;
                    }
                    else
                    {
                        FillRange(0);
                    }
                    return;
                }
                if (ctrl && rawKey == Keys.R)
                {
                    if (SelectedCol == SelEndCol && SelectedCol > 1)
                    {
                        uint targetC = SelectedCol;
                        SelectedCol = targetC - 1;
                        SelEndCol = targetC;
                        FillRange(1);
                        SelectedCol = SelEndCol = targetC;
                    }
                    else
                    {
                        FillRange(1);
                    }
                    return;
                }

                // Ctrl + ; (Current Date) and Ctrl + Shift + ; (Current Time)
                if (ctrl && (rawKey == Keys.OemSemicolon || rawKey == (Keys)186))
                {
                    if (IsSheetProtected) { ShowSheetProtectedWarning(); return; }
                    SaveUndoState();
                    string stamp = shift ? DateTime.Now.ToString("HH:mm:ss") : DateTime.Today.ToString("dd/MM/yyyy");
                    CurrentSheet.SetCellText(SelectedRow, SelectedCol, stamp);
                    RecalcAfterEdit(true);
                    return;
                }

                // Formatting shortcuts: Ctrl+B, Ctrl+I, Ctrl+U, Ctrl+5 (Strikethrough)
                if (ctrl && !shift && rawKey == Keys.B) { ToggleBold(); return; }
                if (ctrl && !shift && rawKey == Keys.I) { ToggleItalic(); return; }
                if (ctrl && !shift && rawKey == Keys.U) { ToggleUnderline(); return; }
                if (ctrl && !shift && rawKey == Keys.D5)
                {
                    if (IsSheetProtected) { ShowSheetProtectedWarning(); return; }
                    SaveUndoState();
                    CellData firstCell = CurrentSheet.GetOrCreateCell(SelectedRow, SelectedCol);
                    bool newStrike = !(firstCell.Format != null && firstCell.Format.Strikethrough);
                    uint minR = Math.Min(SelectedRow, SelEndRow);
                    uint maxR = Math.Max(SelectedRow, SelEndRow);
                    uint minC = Math.Min(SelectedCol, SelEndCol);
                    uint maxC = Math.Max(SelectedCol, SelEndCol);
                    for (uint r = minR; r <= maxR; r++)
                    {
                        for (uint c = minC; c <= maxC; c++)
                        {
                            CellData cd = CurrentSheet.GetOrCreateCell(r, c);
                            if (cd.Format == null) cd.Format = new CellFormat();
                            cd.Format.Strikethrough = newStrike;
                        }
                    }
                    return;
                }

                // Format Cells: Ctrl+1 (Excel Standard Shortcut)
                if (ctrl && !shift && (rawKey == Keys.D1 || rawKey == Keys.NumPad1))
                {
                    using (var fcDlg = new FormatCellsDialogForm(this, 0))
                    {
                        if (fcDlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
                        {
                            this.Invalidate();
                        }
                    }
                    return;
                }

                // Number Format shortcuts: Ctrl+Shift+1 (! Number), Ctrl+Shift+3 (# Date), Ctrl+Shift+4 ($ Currency), Ctrl+Shift+5 (% Percent), Ctrl+Shift+~ (General)
                if (ctrl && shift && rawKey == Keys.D1) { SetNumberFormat("Comma", 2); return; }
                if (ctrl && shift && rawKey == Keys.D3) { SetNumberFormat("Date", 0); return; }
                if (ctrl && shift && rawKey == Keys.D4) { SetNumberFormat("Currency", 0); return; }
                if (ctrl && shift && rawKey == Keys.D5) { SetNumberFormat("Percent", 1); return; }
                if (ctrl && shift && rawKey == Keys.Oemtilde) { SetNumberFormat("General", 2); return; }

                // Table / AutoFilter shortcuts: Ctrl+T, Ctrl+L, Ctrl+Shift+L
                if ((ctrl && (rawKey == Keys.T || rawKey == Keys.L)) || (ctrl && shift && rawKey == Keys.L))
                {
                    ApplyTableStyle(5);
                    return;
                }

                // Insert / Delete Row/Column shortcuts: Ctrl++ and Ctrl+-
                if (ctrl && (rawKey == Keys.Oemplus || rawKey == Keys.Add))
                {
                    InsertRows(1);
                    return;
                }
                if (ctrl && (rawKey == Keys.OemMinus || rawKey == Keys.Subtract))
                {
                    DeleteRows(1);
                    return;
                }

                // Selection shortcuts: Ctrl+Space (Select Column), Shift+Space (Select Row), Ctrl+A (Select Region/Sheet)
                if (ctrl && !shift && rawKey == Keys.Space)
                {
                    SelectedRow = 1;
                    SelEndRow = 500;
                    return;
                }
                if (shift && !ctrl && rawKey == Keys.Space)
                {
                    SelectedCol = 1;
                    SelEndCol = 52;
                    return;
                }
                if (ctrl && rawKey == Keys.A)
                {
                    uint minR, maxR, minC, maxC;
                    GetContiguousDataRange(SelectedRow, SelectedCol, out minR, out maxR, out minC, out maxC);
                    uint curMinR = Math.Min(SelectedRow, SelEndRow);
                    uint curMaxR = Math.Max(SelectedRow, SelEndRow);
                    uint curMinC = Math.Min(SelectedCol, SelEndCol);
                    uint curMaxC = Math.Max(SelectedCol, SelEndCol);
                    if ((maxR > minR || maxC > minC) && (curMinR != minR || curMaxR != maxR || curMinC != minC || curMaxC != maxC))
                    {
                        SelectedRow = minR;
                        SelectedCol = minC;
                        SelEndRow = maxR;
                        SelEndCol = maxC;
                    }
                    else
                    {
                        SelectedRow = 1;
                        SelectedCol = 1;
                        SelEndRow = 500;
                        SelEndCol = 52;
                    }
                    return;
                }

                // Navigation: Home, Ctrl+Home, Ctrl+End, PageUp, PageDown
                if (rawKey == Keys.Home)
                {
                    if (ctrl)
                    {
                        if (shift) { SelEndRow = 1; SelEndCol = 1; }
                        else { SelectedRow = SelEndRow = 1; SelectedCol = SelEndCol = 1; }
                        EnsureCellVisible(1, 1);
                    }
                    else
                    {
                        if (shift) { SelEndCol = 1; }
                        else { SelectedCol = SelEndCol = 1; SelEndRow = SelectedRow; }
                        EnsureCellVisible(SelectedRow, 1);
                    }
                    return;
                }
                if (rawKey == Keys.End && ctrl)
                {
                    uint lastR = 1, lastC = 1;
                    foreach (var kvp in CurrentSheet.GetAllCells())
                    {
                        if (kvp.Value != null && (!string.IsNullOrEmpty(kvp.Value.RawValue) || !string.IsNullOrEmpty(kvp.Value.FormulaStr)))
                        {
                            var pos = SparseMatrix.DecodeKey(kvp.Key);
                            if (pos.Item1 > lastR) lastR = pos.Item1;
                            if (pos.Item2 > lastC) lastC = pos.Item2;
                        }
                    }
                    if (shift) { SelEndRow = lastR; SelEndCol = lastC; }
                    else { SelectedRow = SelEndRow = lastR; SelectedCol = SelEndCol = lastC; }
                    EnsureCellVisible(lastR, lastC);
                    return;
                }
                if (!ctrl && (rawKey == Keys.Prior || rawKey == Keys.PageUp))
                {
                    uint targetR = (uint)Math.Max(1, (int)SelectedRow - 20);
                    if (shift) SelEndRow = targetR;
                    else SelectedRow = SelEndRow = targetR;
                    EnsureCellVisible(targetR, SelectedCol);
                    return;
                }
                if (!ctrl && (rawKey == Keys.Next || rawKey == Keys.PageDown))
                {
                    uint targetR = SelectedRow + 20;
                    if (shift) SelEndRow = targetR;
                    else SelectedRow = SelEndRow = targetR;
                    EnsureCellVisible(targetR, SelectedCol);
                    return;
                }

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
                    EditSelLength = 0;
                    return;
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
                    RecalcAfterEdit(true);
                    return;
                }
                else if (rawKey == Keys.Enter)
                {
                    CommitEditAndMove(shift ? -1 : 1, 0);
                    return;
                }
                else if (rawKey == Keys.Tab)
                {
                    CommitEditAndMove(0, shift ? -1 : 1);
                    return;
                }
                else if (rawKey == Keys.Up || rawKey == Keys.Down || rawKey == Keys.Left || rawKey == Keys.Right)
                {
                    int dR = (rawKey == Keys.Up) ? -1 : (rawKey == Keys.Down ? 1 : 0);
                    int dC = (rawKey == Keys.Left) ? -1 : (rawKey == Keys.Right ? 1 : 0);

                    if (ctrl)
                    {
                        uint baseR = shift ? SelEndRow : SelectedRow;
                        uint baseC = shift ? SelEndCol : SelectedCol;
                        uint nextR, nextC;
                        JumpToDataEdge(baseR, baseC, dR, dC, out nextR, out nextC);
                        if (shift)
                        {
                            SelEndRow = nextR;
                            SelEndCol = nextC;
                        }
                        else
                        {
                            SelectedRow = SelEndRow = nextR;
                            SelectedCol = SelEndCol = nextC;
                        }
                        EnsureCellVisible(nextR, nextC);
                    }
                    else if (shift)
                    {
                        SelEndRow = (uint)Math.Max(1, (int)SelEndRow + dR);
                        SelEndCol = (uint)Math.Max(1, (int)SelEndCol + dC);
                        EnsureCellVisible(SelEndRow, SelEndCol);
                    }
                    else
                    {
                        CommitEditAndMove(dR, dC);
                    }
                }
            }
        }

        private void DrawBorderSegment(D2DGraphics g, int x1, int y1, int x2, int y2, int thick, Color col, bool isHorz)
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

        private void DrawCellBorders(D2DGraphics g, CellBorderSettings b, Rectangle cellRect)
        {
            if (b == null) return;
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

        private void DrawCellText(D2DGraphics g, CellData cell, Rectangle cellRect, Color cellBg, Rectangle textClipRect)
        {
            if (cell == null || (string.IsNullOrEmpty(cell.EvaluatedStr) && string.IsNullOrEmpty(cell.RawValue) && !(cell.Format != null && cell.Format.IsDiagonalHeader)))
                return;

            string dispText;
            if (ShowFormulas && !string.IsNullOrEmpty(cell.RawValue) && cell.RawValue.StartsWith("="))
            {
                dispText = cell.RawValue;
            }
            else
            {
                dispText = !string.IsNullOrEmpty(cell.EvaluatedStr) ? cell.EvaluatedStr : (cell.RawValue ?? "");
            }

            int tc = (cell.Format != null && cell.Format.TextColor != 0) ? cell.Format.TextColor : unchecked((int)0xFF000000);
            if ((tc & unchecked((int)0xFF000000)) == 0 && tc != 0) tc |= unchecked((int)0xFF000000);
            Color txtColor = Color.FromArgb(tc);

            if (cellBg.A > 0)
            {
                int diffR = Math.Abs(txtColor.R - cellBg.R);
                int diffG = Math.Abs(txtColor.G - cellBg.G);
                int diffB = Math.Abs(txtColor.B - cellBg.B);
                if (diffR + diffG + diffB < 60)
                {
                    int bgLum = (cellBg.R * 299 + cellBg.G * 587 + cellBg.B * 114) / 1000;
                    txtColor = bgLum > 130 ? Color.FromArgb(15, 23, 42) : Color.White;
                }
            }

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

            Font font = GetCellFont(fName, fSize * ZoomFactor, fontStyle);
            SolidBrush txtB = GetCachedBrush(txtColor);

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

                if (!string.IsNullOrEmpty(trText))
                {
                    SizeF szTR = g.MeasureString(trText, font);
                    float xTR = cellRect.Right - szTR.Width - 6;
                    float yTR = cellRect.Top + 3;
                    g.DrawString(trText, font, txtB, xTR, yTR);
                }

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
                float z = ZoomFactor > 0 ? ZoomFactor : 1.0f;
                bool isTallRow = cellRect.Height >= (int)Math.Round(30 * z);
                bool hasNewlines = dispText.Contains("\r") || dispText.Contains("\n");
                bool isExplicitWrap = cell.Format != null && cell.Format.WrapText;
                bool wrapText = isExplicitWrap || isTallRow || hasNewlines;

                int indentPx = (cell.Format != null && cell.Format.Indent > 0) ? (int)Math.Round(cell.Format.Indent * 10 * z) : 0;
                string textToDraw = dispText;
                if (!wrapText && (textToDraw.Contains("\r") || textToDraw.Contains("\n")))
                {
                    textToDraw = textToDraw.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                }

                int hAlign = (cell.Format != null) ? cell.Format.Align : 0;
                if (hAlign < 0 || hAlign > 2) hAlign = 0;
                int vAlign = (cell.Format != null) ? cell.Format.VAlign : 1;
                if (vAlign < 0 || vAlign > 2) vAlign = 1;

                StringFormat sf = GetCachedStringFormat(hAlign, vAlign, wrapText);

                int padX = Math.Max(2, (int)Math.Round(4 * z));
                int padY = wrapText ? Math.Max(1, (int)Math.Round(2 * z)) : 0;

                Rectangle textBounds = new Rectangle(
                    cellRect.Left + padX + indentPx,
                    cellRect.Top + padY,
                    Math.Max(0, cellRect.Width - (padX * 2) - indentPx),
                    wrapText ? Math.Max(0, cellRect.Height - (padY * 2)) : cellRect.Height
                );

                g.PushClip(textClipRect);
                g.DrawString(textToDraw, font, txtB, textBounds, sf);
                g.PopClip();
            }
        }

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

            // First cell for indent and merge check
            CellData firstCell = CurrentSheet.GetCell(minR, minC);
            res.Indent = (firstCell != null && firstCell.Format != null) ? firstCell.Format.Indent : 0;

            // Merge status check
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
            float next = Math.Min(1638f, cur + 4f);
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

        public void SetCellTextExplicit(uint row, uint col, string text)
        {
            SaveUndoState();
            CurrentSheet.SetCellText(row, col, text);
            RecalcAfterEdit();
        }

        public void OpenCellDropdownViewer(Form parentForm)
        {
            var dvRule = GetDataValidationRule(SelectedRow, SelectedCol);
            if (dvRule != null && dvRule.ValidationType == DataValidationType.List && dvRule.ShowDropdown)
            {
                var items = dvRule.GetListItems(CurrentSheet);
                if (items != null && items.Count > 0)
                {
                    ContextMenuStrip menu = new ZenQOffice.UI.FluentContextMenuStrip();
                    menu.Font = new Font("Segoe UI", 9.5f);
                    foreach (var item in items)
                    {
                        string val = item;
                        var mi = menu.Items.Add(val);
                        mi.Click += (s, e) =>
                        {
                            SetCellTextExplicit(SelectedRow, SelectedCol, val);
                            if (parentForm != null) parentForm.Invalidate();
                        };
                    }
                    int fBarY0 = 155;
                    int headerH0 = ShowHeadings ? 26 : 0;
                    int headerW0 = ShowHeadings ? 48 : 0;
                    int startY0 = fBarY0 + FormulaBarHeight;
                    int cellX0 = GetColScreenX(SelectedCol, headerW0);
                    int cellY0 = GetRowScreenY(SelectedRow, startY0, headerH0);
                    int cellH0 = GetRowHeight(SelectedRow);
                    Point pt0 = parentForm != null ? parentForm.PointToScreen(new Point(cellX0, cellY0 + cellH0 + 2)) : new Point(cellX0, cellY0 + cellH0 + 2);
                    menu.Show(pt0);
                    return;
                }
            }

            CellData cell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
            string txt = cell != null ? (!string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "")) : "";
            
            // Calculate screen anchor for active cell
            int fBarY = 155;
            int headerH = ShowHeadings ? 26 : 0;
            int headerW = ShowHeadings ? 48 : 0;
            int startY = fBarY + FormulaBarHeight;
            int cellX = GetColScreenX(SelectedCol, headerW);
            int cellY = GetRowScreenY(SelectedRow, startY, headerH);
            int cellW = GetColWidth(SelectedCol);
            int cellH = GetRowHeight(SelectedRow);

            Point pt = parentForm != null ? parentForm.PointToScreen(new Point(cellX, cellY + cellH + 2)) : new Point(cellX, cellY + cellH + 2);
            CellContentDropdownForm dlg = new CellContentDropdownForm(this, parentForm, pt, SelectedRow, SelectedCol, txt);
            dlg.Show();
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
            RecalcAfterEdit();
        }

        public void DeleteRows(uint count = 1)
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint delCount = Math.Max(count, maxR - minR + 1);
            CurrentSheet.DeleteRows(minR, delCount);
            RecalcAfterEdit();
        }

        public void InsertCols(uint count = 1)
        {
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            CurrentSheet.InsertCols(minC, count);
            RecalcAfterEdit();
        }

        public void DeleteCols(uint count = 1)
        {
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            uint delCount = Math.Max(count, maxC - minC + 1);
            CurrentSheet.DeleteCols(minC, delCount);
            RecalcAfterEdit();
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
            RecalcAfterEdit();
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
            RecalcAfterEdit();
        }

        // ==================== Indent Operations ====================

        public void IncreaseIndent()
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                if (cell.Format != null && cell.Format.Indent < 15)
                    cell.Format.Indent++;
            });
        }

        public void DecreaseIndent()
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                if (cell.Format != null && cell.Format.Indent > 0)
                    cell.Format.Indent--;
            });
        }

        // ==================== Cell Styles ====================

        public void ApplyCellStylePreset(int styleIdx)
        {
            SaveUndoState();
            ForEachSelectedCell(cell => {
                if (cell.Format == null) cell.Format = new CellFormat();
                switch (styleIdx)
                {
                    case 0: // Normal
                        cell.Format.FontName = "Calibri";
                        cell.Format.FontSize = 11.0f;
                        cell.Format.Bold = false;
                        cell.Format.Italic = false;
                        cell.Format.Underline = false;
                        cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                        cell.Format.TextColor = unchecked((int)0xFF000000);
                        cell.Format.Borders = new CellBorderSettings();
                        break;
                    case 1: // Good / Tốt (Xanh lá)
                        cell.Format.BgColor = unchecked((int)0xFFC6EFCE);
                        cell.Format.TextColor = unchecked((int)0xFF006100);
                        break;
                    case 2: // Bad / Kém (Đỏ)
                        cell.Format.BgColor = unchecked((int)0xFFFFC7CE);
                        cell.Format.TextColor = unchecked((int)0xFF9C0006);
                        break;
                    case 3: // Neutral / Trung tính (Vàng cam)
                        cell.Format.BgColor = unchecked((int)0xFFFFEB9C);
                        cell.Format.TextColor = unchecked((int)0xFF9C6500);
                        break;
                    case 4: // Heading 1 (Tiêu đề 1)
                        cell.Format.FontName = "Calibri";
                        cell.Format.FontSize = 15.0f;
                        cell.Format.Bold = true;
                        cell.Format.TextColor = unchecked((int)0xFF1F4E78);
                        cell.Format.Borders.Bottom = true;
                        cell.Format.Borders.BottomThickness = 2;
                        cell.Format.Borders.BottomColor = unchecked((int)0xFF5B9BD5);
                        break;
                    case 5: // Heading 2 (Tiêu đề 2)
                        cell.Format.FontName = "Calibri";
                        cell.Format.FontSize = 13.0f;
                        cell.Format.Bold = true;
                        cell.Format.TextColor = unchecked((int)0xFF1F4E78);
                        cell.Format.Borders.Bottom = true;
                        cell.Format.Borders.BottomThickness = 1;
                        cell.Format.Borders.BottomColor = unchecked((int)0xFF5B9BD5);
                        break;
                    case 6: // Total (Hàng Tổng)
                        cell.Format.Bold = true;
                        cell.Format.Borders.Top = true;
                        cell.Format.Borders.TopThickness = 1;
                        cell.Format.Borders.Bottom = true;
                        cell.Format.Borders.BottomThickness = 2;
                        break;
                    case 7: // Accent 1 (Xanh dương)
                        cell.Format.BgColor = unchecked((int)0xFF4472C4);
                        cell.Format.TextColor = unchecked((int)0xFFFFFFFF);
                        cell.Format.Bold = true;
                        break;
                    case 8: // Accent 2 (Cam)
                        cell.Format.BgColor = unchecked((int)0xFFED7D31);
                        cell.Format.TextColor = unchecked((int)0xFFFFFFFF);
                        cell.Format.Bold = true;
                        break;
                }
            });
        }

        // ==================== Cell Format (Row Height & Column Width) ====================

        public void SetSelectedRowsHeight(int height)
        {
            if (height < 10) height = 10;
            if (height > 400) height = 400;
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            for (uint r = minR; r <= maxR; r++)
            {
                CustomRowHeights[r] = height;
            }
        }

        public void SetSelectedColsWidth(int width)
        {
            if (width < 10) width = 10;
            if (width > 600) width = 600;
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            for (uint c = minC; c <= maxC; c++)
            {
                CustomColWidths[c] = width;
            }
        }

        public void AutoFitSelectedRows()
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            for (uint r = minR; r <= maxR; r++)
            {
                float maxH = DefaultRowHeight;
                foreach (var kvp in CurrentSheet.GetAllCells())
                {
                    var coord = SparseMatrix.DecodeKey(kvp.Key);
                    if (coord.Item1 == r)
                    {
                        CellData cell = kvp.Value;
                        if (cell != null && !string.IsNullOrEmpty(cell.RawValue))
                        {
                            float sz = (cell.Format != null && cell.Format.FontSize > 0) ? cell.Format.FontSize : 11.0f;
                            float needed = sz * 1.6f + 8;
                            if (needed > maxH) maxH = needed;
                        }
                    }
                }
                CustomRowHeights[r] = (int)Math.Ceiling(maxH);
            }
        }

        public void AutoFitSelectedCols()
        {
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            using (Bitmap dummyBmp = new Bitmap(1, 1))
            using (Graphics gMeasure = Graphics.FromImage(dummyBmp))
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    float maxW = DefaultColWidth;
                    foreach (var kvp in CurrentSheet.GetAllCells())
                    {
                        var coord = SparseMatrix.DecodeKey(kvp.Key);
                        if (coord.Item2 == c)
                        {
                            CellData cell = kvp.Value;
                            if (cell != null)
                            {
                                string txt = cell.EvaluatedStr ?? cell.RawValue;
                                if (!string.IsNullOrEmpty(txt))
                                {
                                    string fName = (cell.Format != null && !string.IsNullOrEmpty(cell.Format.FontName)) ? cell.Format.FontName : "Calibri";
                                    float fSize = (cell.Format != null && cell.Format.FontSize > 0) ? cell.Format.FontSize : 11.0f;
                                    FontStyle style = FontStyle.Regular;
                                    if (cell.Format != null)
                                    {
                                        if (cell.Format.Bold) style |= FontStyle.Bold;
                                        if (cell.Format.Italic) style |= FontStyle.Italic;
                                    }
                                    using (Font f = new Font(fName, fSize, style))
                                    {
                                        SizeF sz = gMeasure.MeasureString(txt, f);
                                        float needed = sz.Width + 18;
                                        if (needed > maxW) maxW = needed;
                                    }
                                }
                            }
                        }
                    }
                    CustomColWidths[c] = (int)Math.Min(400, Math.Ceiling(maxW));
                }
            }
        }

        public void HideSelectedRows(bool hide)
        {
            SaveUndoState();
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            for (uint r = minR; r <= maxR; r++)
            {
                if (hide) CustomRowHeights[r] = 0;
                else CustomRowHeights[r] = DefaultRowHeight;
            }
        }

        public void HideSelectedCols(bool hide)
        {
            SaveUndoState();
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);
            for (uint c = minC; c <= maxC; c++)
            {
                if (hide) CustomColWidths[c] = 0;
                else CustomColWidths[c] = DefaultColWidth;
            }
        }

        // ==================== Find & Replace ====================

        public void EnsureCellVisible(uint row, uint col)
        {
            if (row < ScrollRow) ScrollRow = row;
            else if (row > ScrollRow + 25) ScrollRow = Math.Max(1, row - 10);

            if (col < ScrollCol) ScrollCol = col;
            else if (col > ScrollCol + 15) ScrollCol = Math.Max(1, col - 5);
        }

        public bool FindNextCell(string searchText, bool matchCase = false)
        {
            if (string.IsNullOrEmpty(searchText)) return false;

            StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            List<ulong> keys = new List<ulong>();
            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                CellData cell = kvp.Value;
                if (cell != null)
                {
                    string txt = cell.EvaluatedStr ?? cell.RawValue;
                    if (!string.IsNullOrEmpty(txt) && txt.IndexOf(searchText, comp) >= 0)
                    {
                        keys.Add(kvp.Key);
                    }
                }
            }

            if (keys.Count == 0) return false;

            keys.Sort((k1, k2) =>
            {
                var c1 = SparseMatrix.DecodeKey(k1);
                var c2 = SparseMatrix.DecodeKey(k2);
                if (c1.Item1 != c2.Item1) return c1.Item1.CompareTo(c2.Item1);
                return c1.Item2.CompareTo(c2.Item2);
            });

            ulong curKey = SparseMatrix.EncodeKey(SelectedRow, SelectedCol);
            ulong chosenKey = keys[0];

            foreach (var k in keys)
            {
                if (k > curKey)
                {
                    chosenKey = k;
                    break;
                }
            }

            var targetCoord = SparseMatrix.DecodeKey(chosenKey);
            SelectedRow = targetCoord.Item1;
            SelectedCol = targetCoord.Item2;
            SelEndRow = targetCoord.Item1;
            SelEndCol = targetCoord.Item2;

            EnsureCellVisible(SelectedRow, SelectedCol);
            return true;
        }

        public int ReplaceAllInSheet(string findText, string replaceText, bool matchCase = false)
        {
            if (string.IsNullOrEmpty(findText)) return 0;
            SaveUndoState();
            StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int count = 0;
            List<ulong> keysToUpdate = new List<ulong>();

            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                CellData cell = kvp.Value;
                if (cell != null && !string.IsNullOrEmpty(cell.RawValue))
                {
                    if (cell.RawValue.IndexOf(findText, comp) >= 0)
                    {
                        keysToUpdate.Add(kvp.Key);
                    }
                }
            }

            foreach (var k in keysToUpdate)
            {
                var coord = SparseMatrix.DecodeKey(k);
                CellData cell = CurrentSheet.GetCell(coord.Item1, coord.Item2);
                if (cell != null && !string.IsNullOrEmpty(cell.RawValue))
                {
                    if (matchCase)
                    {
                        cell.RawValue = cell.RawValue.Replace(findText, replaceText ?? "");
                    }
                    else
                    {
                        cell.RawValue = Regex.Replace(
                            cell.RawValue,
                            Regex.Escape(findText),
                            replaceText ?? "",
                            RegexOptions.IgnoreCase
                        );
                    }
                    cell.EvaluatedStr = cell.RawValue;
                    count++;
                }
            }

            if (count > 0)
            {
                RecalcAfterEdit();
            }
            return count;
        }

        public void OpenFindReplaceDialog(Form parentForm, bool isReplace = false)
        {
            if (_findReplaceDlg == null || _findReplaceDlg.IsDisposed)
            {
                _findReplaceDlg = new CalcFindReplaceDialog(this, parentForm, isReplace);
                _findReplaceDlg.Show(parentForm);
            }
            else
            {
                _findReplaceDlg.SetMode(isReplace);
                _findReplaceDlg.BringToFront();
                _findReplaceDlg.Focus();
            }
        }

        public bool FindNextCellEx(string query, bool matchCase, bool matchEntire, bool byRows, bool lookInFormulas, bool searchBackwards, out string statusMsg)
        {
            if (string.IsNullOrEmpty(query) || CurrentSheet == null)
            {
                statusMsg = AppL10n.T("Vui lòng nhập nội dung cần tìm.", "Please enter text to find.");
                return false;
            }

            StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            List<Tuple<uint, uint>> matches = new List<Tuple<uint, uint>>();

            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                CellData cell = kvp.Value;
                if (cell == null) continue;

                string targetText = "";
                if (lookInFormulas)
                {
                    if (cell.Type == CellType.Formula)
                    {
                        targetText = cell.FormulaStr ?? cell.RawValue ?? "";
                    }
                    else
                    {
                        targetText = cell.RawValue ?? "";
                    }
                }
                else
                {
                    targetText = cell.GetDisplayText() ?? "";
                }

                bool matched = false;
                if (matchEntire)
                {
                    matched = string.Equals(targetText, query, comp);
                }
                else
                {
                    matched = (targetText.IndexOf(query, comp) >= 0);
                }

                if (matched)
                {
                    matches.Add(SparseMatrix.DecodeKey(kvp.Key));
                }
            }

            if (matches.Count == 0)
            {
                statusMsg = AppL10n.T("Không tìm thấy nội dung yêu cầu.", "No matching entries were found.");
                return false;
            }

            if (byRows)
            {
                matches.Sort(delegate(Tuple<uint, uint> a, Tuple<uint, uint> b)
                {
                    int cmp = a.Item1.CompareTo(b.Item1);
                    if (cmp != 0) return cmp;
                    return a.Item2.CompareTo(b.Item2);
                });
            }
            else
            {
                matches.Sort(delegate(Tuple<uint, uint> a, Tuple<uint, uint> b)
                {
                    int cmp = a.Item2.CompareTo(b.Item2);
                    if (cmp != 0) return cmp;
                    return a.Item1.CompareTo(b.Item1);
                });
            }

            uint curR = SelectedRow;
            uint curC = SelectedCol;
            Tuple<uint, uint> chosen = null;

            if (!searchBackwards)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    Tuple<uint, uint> m = matches[i];
                    if (byRows)
                    {
                        if (m.Item1 > curR || (m.Item1 == curR && m.Item2 > curC))
                        {
                            chosen = m;
                            break;
                        }
                    }
                    else
                    {
                        if (m.Item2 > curC || (m.Item2 == curC && m.Item1 > curR))
                        {
                            chosen = m;
                            break;
                        }
                    }
                }
                if (chosen == null)
                {
                    chosen = matches[0];
                }
            }
            else
            {
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Tuple<uint, uint> m = matches[i];
                    if (byRows)
                    {
                        if (m.Item1 < curR || (m.Item1 == curR && m.Item2 < curC))
                        {
                            chosen = m;
                            break;
                        }
                    }
                    else
                    {
                        if (m.Item2 < curC || (m.Item2 == curC && m.Item1 < curR))
                        {
                            chosen = m;
                            break;
                        }
                    }
                }
                if (chosen == null)
                {
                    chosen = matches[matches.Count - 1];
                }
            }

            SelectedRow = chosen.Item1;
            SelectedCol = chosen.Item2;
            SelEndRow = chosen.Item1;
            SelEndCol = chosen.Item2;

            EnsureCellVisible(SelectedRow, SelectedCol);
            statusMsg = AppL10n.T(string.Format("Đã tìm thấy tại {0} ({1} kết quả)", SparseMatrix.GetCellReference(SelectedRow, SelectedCol), matches.Count), string.Format("Found at {0} ({1} match(es))", SparseMatrix.GetCellReference(SelectedRow, SelectedCol), matches.Count));
            return true;
        }

        public int ReplaceCellEx(string find, string replace, bool matchCase, bool matchEntire, bool byRows, bool lookInFormulas)
        {
            if (string.IsNullOrEmpty(find) || CurrentSheet == null) return 0;
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return 0;
            }

            CellData curCell = CurrentSheet.GetCell(SelectedRow, SelectedCol);
            if (curCell == null) return 0;

            if (!lookInFormulas && curCell.Type == CellType.Formula)
            {
                // Formula safety! Cannot replace formula cell when looking in values
                return 0;
            }

            string targetText = "";
            if (lookInFormulas)
            {
                targetText = (curCell.Type == CellType.Formula) ? (curCell.FormulaStr ?? curCell.RawValue ?? "") : (curCell.RawValue ?? "");
            }
            else
            {
                targetText = curCell.GetDisplayText() ?? "";
            }

            StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            bool matched = matchEntire ? string.Equals(targetText, find, comp) : (targetText.IndexOf(find, comp) >= 0);
            if (!matched) return 0;

            SaveUndoState();
            string newText = "";
            if (matchEntire)
            {
                newText = replace ?? "";
            }
            else
            {
                if (matchCase)
                {
                    newText = targetText.Replace(find, replace ?? "");
                }
                else
                {
                    newText = Regex.Replace(targetText, Regex.Escape(find), replace ?? "", RegexOptions.IgnoreCase);
                }
            }

            if (lookInFormulas)
            {
                if (newText.StartsWith("="))
                {
                    curCell.Type = CellType.Formula;
                    curCell.FormulaStr = newText;
                    curCell.RawValue = newText;
                }
                else
                {
                    curCell.Type = CellType.String;
                    curCell.FormulaStr = null;
                    curCell.RawValue = newText;
                    curCell.EvaluatedStr = newText;
                }
            }
            else
            {
                double numVal;
                if (double.TryParse(newText, NumberStyles.Any, CultureInfo.InvariantCulture, out numVal))
                {
                    curCell.SetNumber(numVal);
                }
                else
                {
                    curCell.SetString(newText);
                }
            }

            RecalcAfterEdit();
            return 1;
        }

        public int ReplaceAllInSheetEx(string find, string replace, bool matchCase, bool matchEntire, bool lookInFormulas)
        {
            if (string.IsNullOrEmpty(find) || CurrentSheet == null) return 0;
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return 0;
            }

            SaveUndoState();
            StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int count = 0;
            List<Tuple<uint, uint>> targets = new List<Tuple<uint, uint>>();

            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                CellData cell = kvp.Value;
                if (cell == null) continue;

                if (!lookInFormulas && cell.Type == CellType.Formula)
                {
                    // Formula protection!
                    continue;
                }

                string targetText = "";
                if (lookInFormulas)
                {
                    targetText = (cell.Type == CellType.Formula) ? (cell.FormulaStr ?? cell.RawValue ?? "") : (cell.RawValue ?? "");
                }
                else
                {
                    targetText = cell.GetDisplayText() ?? "";
                }

                bool matched = matchEntire ? string.Equals(targetText, find, comp) : (targetText.IndexOf(find, comp) >= 0);
                if (matched)
                {
                    targets.Add(SparseMatrix.DecodeKey(kvp.Key));
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Tuple<uint, uint> coord = targets[i];
                CellData cell = CurrentSheet.GetCell(coord.Item1, coord.Item2);
                if (cell == null) continue;

                string targetText = lookInFormulas ?
                    ((cell.Type == CellType.Formula) ? (cell.FormulaStr ?? cell.RawValue ?? "") : (cell.RawValue ?? ""))
                    : (cell.GetDisplayText() ?? "");

                string newText = matchEntire ? (replace ?? "") :
                    (matchCase ? targetText.Replace(find, replace ?? "") : Regex.Replace(targetText, Regex.Escape(find), replace ?? "", RegexOptions.IgnoreCase));

                if (lookInFormulas)
                {
                    if (newText.StartsWith("="))
                    {
                        cell.Type = CellType.Formula;
                        cell.FormulaStr = newText;
                        cell.RawValue = newText;
                    }
                    else
                    {
                        cell.Type = CellType.String;
                        cell.FormulaStr = null;
                        cell.RawValue = newText;
                        cell.EvaluatedStr = newText;
                    }
                }
                else
                {
                    double numVal;
                    if (double.TryParse(newText, NumberStyles.Any, CultureInfo.InvariantCulture, out numVal))
                    {
                        cell.SetNumber(numVal);
                    }
                    else
                    {
                        cell.SetString(newText);
                    }
                }
                count++;
            }

            if (count > 0)
            {
                RecalcAfterEdit();
            }
            return count;
        }

        public List<Tuple<string, uint, uint, string>> FindAllCellsEx(string query, bool matchCase, bool matchEntire, bool lookInFormulas)
        {
            List<Tuple<string, uint, uint, string>> results = new List<Tuple<string, uint, uint, string>>();
            if (string.IsNullOrEmpty(query) || CurrentSheet == null) return results;

            string sheetName = (ActiveSheetIndex >= 0 && ActiveSheetIndex < SheetNames.Count) ? SheetNames[ActiveSheetIndex] : "Sheet1";
            StringComparison comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            List<Tuple<uint, uint, string>> matches = new List<Tuple<uint, uint, string>>();

            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                CellData cell = kvp.Value;
                if (cell == null) continue;

                string targetText = "";
                if (lookInFormulas)
                {
                    targetText = (cell.Type == CellType.Formula) ? (cell.FormulaStr ?? cell.RawValue ?? "") : (cell.RawValue ?? "");
                }
                else
                {
                    targetText = cell.GetDisplayText() ?? "";
                }

                bool matched = matchEntire ? string.Equals(targetText, query, comp) : (targetText.IndexOf(query, comp) >= 0);
                if (matched)
                {
                    var coord = SparseMatrix.DecodeKey(kvp.Key);
                    matches.Add(Tuple.Create(coord.Item1, coord.Item2, targetText));
                }
            }

            matches.Sort(delegate(Tuple<uint, uint, string> a, Tuple<uint, uint, string> b)
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                if (cmp != 0) return cmp;
                return a.Item2.CompareTo(b.Item2);
            });

            for (int i = 0; i < matches.Count; i++)
            {
                Tuple<uint, uint, string> m = matches[i];
                results.Add(Tuple.Create(sheetName, m.Item1, m.Item2, m.Item3));
            }

            return results;
        }

        public void OpenRemoveDuplicatesDialog(Form parentForm)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }
            CalcRemoveDuplicatesDialog dlg = new CalcRemoveDuplicatesDialog(this, parentForm);
            dlg.ShowDialog(parentForm);
        }

        public void OpenTextToColumnsDialog(Form parentForm)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }
            CalcTextToColumnsDialog dlg = new CalcTextToColumnsDialog(this, parentForm);
            dlg.ShowDialog(parentForm);
        }

        // ==================== Excel 365 Data Validation, Auditing, Scenarios & Names ====================
        public DataValidationRule GetDataValidationRule(uint row, uint col)
        {
            if (DataValidations == null || DataValidations.Count == 0) return null;
            for (int i = DataValidations.Count - 1; i >= 0; i--)
            {
                if (DataValidations[i].ContainsCell(row, col))
                    return DataValidations[i];
            }
            return null;
        }

        public DataValidationResult ValidateCellInput(uint row, uint col, object val)
        {
            var rule = GetDataValidationRule(row, col);
            if (rule == null) return new DataValidationResult { IsValid = true };
            return rule.Validate(val, CurrentSheet, row, col);
        }

        public bool CommitCurrentEditCell(bool preventOnStop = true)
        {
            if (!IsEditing) return true;
            string finalVal = EditText ?? "";

            // Excel 365 Data Validation Check
            var dvRule = GetDataValidationRule(SelectedRow, SelectedCol);
            if (dvRule != null)
            {
                var valRes = dvRule.Validate(finalVal, CurrentSheet, SelectedRow, SelectedCol);
                if (!valRes.IsValid && dvRule.ShowErrorMessage)
                {
                    if (dvRule.ErrorStyle == DataValidationErrorStyle.Stop)
                    {
                        ZenQOffice.UI.FluentMessageBox.Show(valRes.ErrorMessage, dvRule.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        if (preventOnStop) return false;
                    }
                    else if (dvRule.ErrorStyle == DataValidationErrorStyle.Warning)
                    {
                        var dr = ZenQOffice.UI.FluentMessageBox.Show(valRes.ErrorMessage + AppL10n.T("\n\nBạn có muốn tiếp tục áp dụng không?", "\n\nDo you want to continue?"), dvRule.ErrorTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr == DialogResult.No)
                        {
                            if (preventOnStop) return false;
                        }
                    }
                    else if (dvRule.ErrorStyle == DataValidationErrorStyle.Information)
                    {
                        ZenQOffice.UI.FluentMessageBox.Show(valRes.ErrorMessage, dvRule.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            if (FormulaSourceSheetIndex >= 0 && FormulaSourceSheetIndex != ActiveSheetIndex && FormulaSourceSheetIndex < Sheets.Count)
            {
                SaveCurrentSheetState();
                _activeSheetIndex = FormulaSourceSheetIndex;
                LoadSheetState(FormulaSourceSheetIndex);
                SelectedRow = FormulaSourceRow;
                SelectedCol = FormulaSourceCol;
                SelEndRow = FormulaSourceRow;
                SelEndCol = FormulaSourceCol;
            }
            FormulaSourceSheetIndex = -1;
            IsFormulaPointMode = false;
            IsSelectingPointRange = false;
            _pointInsertStart = -1;
            _pointInsertLength = 0;

            SaveUndoState();
            CurrentSheet.SetCellText(SelectedRow, SelectedCol, finalVal);
            RecalcAfterEdit(true);
            IsEditing = false;
            EditSelLength = 0;
            return true;
        }

        public void ShowDataValidationDialog(Form parentForm)
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

            DataValidationRule existing = GetDataValidationRule(SelectedRow, SelectedCol);
            using (var dlg = new DataValidationDialog(existing, minR, minC, maxR, maxC))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    if (dlg.IsCleared)
                    {
                        DataValidations.RemoveAll(r => r.ContainsCell(minR, minC));
                    }
                    else if (dlg.Rule != null)
                    {
                        DataValidations.RemoveAll(r => r.StartRow == minR && r.StartCol == minC && r.EndRow == maxR && r.EndCol == maxC);
                        DataValidations.Add(dlg.Rule);
                    }
                    if (parentForm != null) parentForm.Invalidate();
                }
            }
        }

        public void ToggleCircleInvalidData()
        {
            CircleInvalidData = !CircleInvalidData;
        }

        public void ClearValidationCircles()
        {
            CircleInvalidData = false;
        }

        public void TracePrecedents(uint row, uint col)
        {
            if (CurrentSheet == null) return;
            var cell = CurrentSheet.GetCell(row, col);
            if (cell == null) return;
            string formula = !string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "");
            if (!formula.StartsWith("=")) return;

            var precedents = FormulaAuditingEngine.ExtractPrecedentCells(formula);
            foreach (var prec in precedents)
            {
                AuditingArrows.Add(new FormulaAuditingArrow
                {
                    FromRow = prec.Item1,
                    FromCol = prec.Item2,
                    ToRow = row,
                    ToCol = col,
                    ArrowColor = Color.FromArgb(37, 99, 235)
                });
            }
        }

        public void TraceDependents(uint row, uint col)
        {
            if (CurrentSheet == null) return;
            foreach (var kvp in CurrentSheet.GetAllCells())
            {
                var cell = kvp.Value;
                if (cell == null) continue;
                string formula = !string.IsNullOrEmpty(cell.FormulaStr) ? cell.FormulaStr : (cell.RawValue ?? "");
                if (formula.StartsWith("=") && FormulaAuditingEngine.FormulaContainsCell(formula, row, col))
                {
                    var coord = SparseMatrix.DecodeKey(kvp.Key);
                    AuditingArrows.Add(new FormulaAuditingArrow
                    {
                        FromRow = row,
                        FromCol = col,
                        ToRow = coord.Item1,
                        ToCol = coord.Item2,
                        ArrowColor = Color.FromArgb(37, 99, 235)
                    });
                }
            }
        }

        public void ClearAuditingArrows()
        {
            AuditingArrows.Clear();
        }

        public void ToggleShowFormulas()
        {
            ShowFormulas = !ShowFormulas;
        }

        public void ApplyScenario(int index)
        {
            if (Scenarios == null || index < 0 || index >= Scenarios.Count) return;
            ApplyScenario(Scenarios[index]);
        }

        public void ApplyScenario(ScenarioModel sc)
        {
            if (sc == null || CurrentSheet == null) return;
            SaveUndoState();
            for (int i = 0; i < sc.ChangingCells.Count && i < sc.Values.Count; i++)
            {
                var cell = sc.ChangingCells[i];
                CurrentSheet.SetCellText(cell.Item1, cell.Item2, sc.Values[i]);
            }
            RecalcAfterEdit(true);
        }

        public void GenerateScenarioSummaryReport()
        {
            if (Scenarios == null || Scenarios.Count == 0) return;
            SaveUndoState();
            string summarySheetName = "Scenario Summary";
            int counter = 1;
            while (SheetNames.Contains(summarySheetName))
            {
                summarySheetName = "Scenario Summary " + (++counter);
            }
            AddSheet(summarySheetName);
            int sheetIdx = SheetNames.IndexOf(summarySheetName);
            if (sheetIdx >= 0) ActiveSheetIndex = sheetIdx;

            CurrentSheet.SetCellText(1, 1, "BÁO CÁO TỔNG HỢP KỊCH BẢN (SCENARIO SUMMARY REPORT)");
            var c1 = CurrentSheet.GetOrCreateCell(1, 1);
            c1.Format.Bold = true;
            c1.Format.FontSize = 14;

            CurrentSheet.SetCellText(3, 1, "Ô thay đổi (Changing Cells)");
            var cHeader = CurrentSheet.GetOrCreateCell(3, 1);
            cHeader.Format.Bold = true;
            cHeader.Format.BgColor = unchecked((int)0xFFE2E8F0);

            for (int i = 0; i < Scenarios.Count; i++)
            {
                CurrentSheet.SetCellText(3, (uint)(2 + i), Scenarios[i].Name);
                var scHeader = CurrentSheet.GetOrCreateCell(3, (uint)(2 + i));
                scHeader.Format.Bold = true;
                scHeader.Format.BgColor = unchecked((int)0xFFE2E8F0);
            }

            var allChanging = new List<Tuple<uint, uint>>();
            foreach (var sc in Scenarios)
            {
                foreach (var cell in sc.ChangingCells)
                {
                    bool exists = false;
                    foreach (var existing in allChanging)
                    {
                        if (existing.Item1 == cell.Item1 && existing.Item2 == cell.Item2) { exists = true; break; }
                    }
                    if (!exists) allChanging.Add(cell);
                }
            }

            uint curRow = 4;
            foreach (var cell in allChanging)
            {
                string addr = "$" + SparseMatrix.ColIndexToName(cell.Item2) + "$" + cell.Item1;
                CurrentSheet.SetCellText(curRow, 1, addr);
                for (int i = 0; i < Scenarios.Count; i++)
                {
                    var sc = Scenarios[i];
                    int idx = sc.ChangingCells.FindIndex(c => c.Item1 == cell.Item1 && c.Item2 == cell.Item2);
                    string val = idx >= 0 && idx < sc.Values.Count ? sc.Values[idx] : "";
                    CurrentSheet.SetCellText(curRow, (uint)(2 + i), val);
                }
                curRow++;
            }

            RecalcAfterEdit(true);
        }

        public void ShowScenarioManagerDialog(Form parentForm)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }
            using (var dlg = new ScenarioManagerDialog(this))
            {
                dlg.ShowDialog(parentForm);
                if (parentForm != null) parentForm.Invalidate();
            }
        }

        public void ShowNameManagerDialog(Form parentForm)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }
            using (var dlg = new NameManagerDialog(this))
            {
                dlg.ShowDialog(parentForm);
                if (parentForm != null) parentForm.Invalidate();
            }
        }

        public void ExecuteRemoveDuplicates(uint minR, uint maxR, uint minC, uint maxC, List<uint> checkedCols, bool hasHeaders, out int dupCount, out int uniqueCount)
        {
            dupCount = 0;
            uniqueCount = 0;
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }

            if (minR > maxR) { uint t = minR; minR = maxR; maxR = t; }
            if (minC > maxC) { uint t = minC; minC = maxC; maxC = t; }
            uint startR = hasHeaders ? (minR + 1) : minR;
            if (startR > maxR || checkedCols == null || checkedCols.Count == 0 || CurrentSheet == null)
            {
                return;
            }

            SaveUndoState();
            HashSet<string> seenSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<uint> rowsToDelete = new List<uint>();

            for (uint r = startR; r <= maxR; r++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < checkedCols.Count; i++)
                {
                    uint col = checkedCols[i];
                    CellData cell = CurrentSheet.GetCell(r, col);
                    string val = (cell != null) ? (cell.EvaluatedStr ?? cell.RawValue ?? "") : "";
                    sb.Append(val.Trim()).Append('\x1F');
                }
                string sig = sb.ToString();
                if (seenSignatures.Contains(sig))
                {
                    rowsToDelete.Add(r);
                }
                else
                {
                    seenSignatures.Add(sig);
                }
            }

            dupCount = rowsToDelete.Count;
            uniqueCount = seenSignatures.Count;
            if (dupCount == 0) return;

            rowsToDelete.Sort(delegate(uint a, uint b) { return b.CompareTo(a); });

            uint currentMaxR = maxR;
            for (int i = 0; i < rowsToDelete.Count; i++)
            {
                uint r = rowsToDelete[i];
                for (uint currR = r; currR < currentMaxR; currR++)
                {
                    for (uint c = minC; c <= maxC; c++)
                    {
                        CellData nextCell = CurrentSheet.GetCell(currR + 1, c);
                        if (nextCell != null)
                        {
                            CurrentSheet.SetCell(currR, c, nextCell.Clone());
                        }
                        else
                        {
                            CurrentSheet.ClearCell(currR, c);
                        }
                    }
                }
                for (uint c = minC; c <= maxC; c++)
                {
                    CurrentSheet.ClearCell(currentMaxR, c);
                }
                currentMaxR--;
            }
            SelEndRow = currentMaxR;

            RecalcAfterEdit();
        }

        public void ExecuteTextToColumns(uint srcCol, uint startRow, uint endRow, uint destCol, uint destRow, List<char> delimiters, bool consecutiveAsOne, char textQualifier, List<int> fixedWidths, bool isFixedWidth)
        {
            if (IsSheetProtected)
            {
                ShowSheetProtectedWarning();
                return;
            }
            if (CurrentSheet == null) return;

            if (startRow > endRow)
            {
                uint t = startRow;
                startRow = endRow;
                endRow = t;
            }

            SaveUndoState();

            for (uint r = startRow; r <= endRow; r++)
            {
                uint targetRow = destRow + (r - startRow);
                CellData cell = CurrentSheet.GetCell(r, srcCol);
                string line = (cell != null) ? (cell.RawValue ?? "") : "";

                List<string> tokens;
                if (isFixedWidth)
                {
                    tokens = CalcTextToColumnsDialog.ParseFixedWidthLine(line, fixedWidths);
                }
                else
                {
                    tokens = CalcTextToColumnsDialog.ParseDelimitedLine(line, delimiters, consecutiveAsOne, textQualifier);
                }

                for (int i = 0; i < tokens.Count; i++)
                {
                    uint targetCol = destCol + (uint)i;
                    string tok = (tokens[i] ?? "").Trim();
                    double numVal;
                    if (double.TryParse(tok, NumberStyles.Any, CultureInfo.InvariantCulture, out numVal))
                    {
                        CellData targetCell = CurrentSheet.GetOrCreateCell(targetRow, targetCol);
                        targetCell.SetNumber(numVal);
                    }
                    else
                    {
                        CurrentSheet.SetCellText(targetRow, targetCol, tok);
                    }
                }
            }

            RecalcAfterEdit();
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
            foreach (var rng in GetAllSelectedRanges())
            {
                uint maxR = Math.Min(rng.MaxRow, Math.Max(rng.MinRow, CurrentSheet.GetMaxRow()));
                uint maxC = Math.Min(rng.MaxCol, Math.Max(rng.MinCol, CurrentSheet.GetMaxCol()));
                for (uint r = rng.MinRow; r <= maxR; r++)
                {
                    for (uint c = rng.MinCol; c <= maxC; c++)
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
            }
            RecomputeHiddenRows();
            RecalcAfterEdit();
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

            if (ruleType == 3) // Clear Rules (All cells in selection)
            {
                for (uint r = minR; r <= maxR; r++)
                {
                    for (uint c = minC; c <= maxC; c++)
                    {
                        CellData cell = CurrentSheet.GetCell(r, c);
                        if (cell != null && cell.Format != null)
                        {
                            cell.Format.BgColor = unchecked((int)0xFFFFFFFF);
                            cell.Format.TextColor = unchecked((int)0xFF000000);
                        }
                    }
                }
                return;
            }

            if (ruleType == 5) // Highlight Duplicate Values (Yellow)
            {
                Dictionary<string, int> freq = new Dictionary<string, int>();
                for (uint r = minR; r <= maxR; r++)
                {
                    for (uint c = minC; c <= maxC; c++)
                    {
                        CellData cell = CurrentSheet.GetCell(r, c);
                        if (cell != null)
                        {
                            string val = cell.EvaluatedStr ?? cell.RawValue;
                            if (!string.IsNullOrEmpty(val))
                            {
                                int cnt = 0;
                                freq.TryGetValue(val, out cnt);
                                freq[val] = cnt + 1;
                            }
                        }
                    }
                }

                for (uint r = minR; r <= maxR; r++)
                {
                    for (uint c = minC; c <= maxC; c++)
                    {
                        CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                        string val = cell.EvaluatedStr ?? cell.RawValue;
                        if (!string.IsNullOrEmpty(val) && freq.ContainsKey(val) && freq[val] > 1)
                        {
                            cell.Format.BgColor = unchecked((int)0xFFFEF3C7);
                            cell.Format.TextColor = unchecked((int)0xFF92400E);
                        }
                    }
                }
                return;
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
                        else if (ruleType == 2) // Heatmap Scale (Red - Yellow - Green)
                        {
                            double range = Math.Max(1.0, maxVal - minVal);
                            double frac = (val - minVal) / range;
                            if (frac >= 0.66) { cell.Format.BgColor = unchecked((int)0xFFDCFCE7); cell.Format.TextColor = unchecked((int)0xFF166534); }
                            else if (frac >= 0.33) { cell.Format.BgColor = unchecked((int)0xFFFEF3C7); cell.Format.TextColor = unchecked((int)0xFF92400E); }
                            else { cell.Format.BgColor = unchecked((int)0xFFFEE2E2); cell.Format.TextColor = unchecked((int)0xFF991B1B); }
                        }
                        else if (ruleType == 4) // Data Bars (Blue gradient fill)
                        {
                            double range = Math.Max(1.0, maxVal - minVal);
                            double frac = (val - minVal) / range;
                            if (frac >= 0.75) { cell.Format.BgColor = unchecked((int)0xFF93C5FD); cell.Format.TextColor = unchecked((int)0xFF1E3A8A); }
                            else if (frac >= 0.50) { cell.Format.BgColor = unchecked((int)0xFFBFDBFE); cell.Format.TextColor = unchecked((int)0xFF1E3A8A); }
                            else if (frac >= 0.25) { cell.Format.BgColor = unchecked((int)0xFFDBEAFE); cell.Format.TextColor = unchecked((int)0xFF1E3A8A); }
                            else { cell.Format.BgColor = unchecked((int)0xFFEFF6FF); cell.Format.TextColor = unchecked((int)0xFF1E3A8A); }
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
            RecalcAfterEdit();
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

            RecalcAfterEdit();
        }

        public void SyncFormulaEngineMetadata()
        {
            Dictionary<string, string> namedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (DefinedNames != null)
            {
                foreach (var dn in DefinedNames)
                {
                    if (dn != null && !string.IsNullOrWhiteSpace(dn.Name) && !string.IsNullOrWhiteSpace(dn.RefersTo))
                    {
                        namedMap[dn.Name.Trim()] = dn.RefersTo.Trim();
                    }
                }
            }
            FormulaEngine.IsManualCalculation = !AutoCalculation;
            FormulaEngine.SetWorkbookContext(Sheets, SheetNames);
            FormulaEngine.SetWorkbookFormulaMetadata(namedMap, TryResolveStructuredReference);
        }

        public List<Tuple<uint, uint>> TryResolveStructuredReference(string expr, SparseMatrix targetSheet, uint currentRow)
        {
            if (string.IsNullOrWhiteSpace(expr) || Tables == null) return null;
            SparseMatrix sh = targetSheet ?? CurrentSheet;
            if (sh == null) return null;
            string s = expr.Trim();

            var m = System.Text.RegularExpressions.Regex.Match(s, @"^([A-Za-z_][A-Za-z0-9_.]*)?\[(.*)\]$");
            if (!m.Success) return null;

            string tblName = m.Groups[1].Value;
            string spec = m.Groups[2].Value.Trim();

            ExcelTableInfo targetTable = null;
            if (!string.IsNullOrEmpty(tblName))
            {
                foreach (var t in Tables)
                {
                    if (t != null && string.Equals(t.Name, tblName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetTable = t;
                        break;
                    }
                }
            }
            else
            {
                uint evalR = currentRow > 0 ? currentRow : SelectedRow;
                uint evalC = FormulaEngine.CurrentEvaluatingCol > 0 ? FormulaEngine.CurrentEvaluatingCol : SelectedCol;
                foreach (var t in Tables)
                {
                    if (t != null && evalR >= t.StartRow && evalR <= t.EndRow && evalC >= t.StartCol && evalC <= t.EndCol)
                    {
                        targetTable = t;
                        break;
                    }
                }
                if (targetTable == null)
                {
                    foreach (var t in Tables)
                    {
                        if (t != null && evalR >= t.StartRow && evalR <= t.EndRow)
                        {
                            targetTable = t;
                            break;
                        }
                    }
                }
            }

            if (targetTable == null) return null;

            uint dataStartR = targetTable.HasHeaders ? (targetTable.StartRow + 1) : targetTable.StartRow;
            uint dataEndR = targetTable.HasTotalRow && targetTable.EndRow > dataStartR ? (targetTable.EndRow - 1) : targetTable.EndRow;
            var coords = new List<Tuple<uint, uint>>();

            if (spec.Equals("#All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(spec))
            {
                for (uint r = targetTable.StartRow; r <= targetTable.EndRow; r++)
                    for (uint c = targetTable.StartCol; c <= targetTable.EndCol; c++)
                        coords.Add(Tuple.Create(r, c));
                return coords;
            }
            if (spec.Equals("#Data", StringComparison.OrdinalIgnoreCase))
            {
                for (uint r = dataStartR; r <= dataEndR; r++)
                    for (uint c = targetTable.StartCol; c <= targetTable.EndCol; c++)
                        coords.Add(Tuple.Create(r, c));
                return coords;
            }
            if (spec.Equals("#Headers", StringComparison.OrdinalIgnoreCase) && targetTable.HasHeaders)
            {
                for (uint c = targetTable.StartCol; c <= targetTable.EndCol; c++)
                    coords.Add(Tuple.Create(targetTable.StartRow, c));
                return coords;
            }
            if (spec.Equals("#Totals", StringComparison.OrdinalIgnoreCase) && targetTable.HasTotalRow)
            {
                for (uint c = targetTable.StartCol; c <= targetTable.EndCol; c++)
                    coords.Add(Tuple.Create(targetTable.EndRow, c));
                return coords;
            }

            bool currentRowOnly = spec.StartsWith("@");
            string colHeader = currentRowOnly ? spec.Substring(1).Trim() : spec;
            if (colHeader.StartsWith("[") && colHeader.EndsWith("]"))
                colHeader = colHeader.Substring(1, colHeader.Length - 2).Trim();

            for (uint c = targetTable.StartCol; c <= targetTable.EndCol; c++)
            {
                CellData hCell = sh.GetCell(targetTable.StartRow, c);
                string hText = hCell != null ? (hCell.EvaluatedStr ?? hCell.RawValue ?? "").Trim() : "";
                if (string.Equals(hText, colHeader, StringComparison.OrdinalIgnoreCase))
                {
                    if (currentRowOnly)
                    {
                        uint evalR = currentRow > 0 ? currentRow : SelectedRow;
                        coords.Add(Tuple.Create(evalR, c));
                    }
                    else
                    {
                        for (uint r = dataStartR; r <= dataEndR; r++)
                            coords.Add(Tuple.Create(r, c));
                    }
                    return coords;
                }
            }
            return null;
        }

        private void RecalcAfterEdit(bool singleCellChanged = false)
        {
            SyncFormulaEngineMetadata();
            if (AutoCalculation)
            {
                FormulaEngine.EvaluateWorkbook(Sheets, SheetNames);
            }
            else if (singleCellChanged && CurrentSheet != null)
            {
                CellData c = CurrentSheet.GetCell(SelectedRow, SelectedCol);
                if (c != null && c.Type == CellType.Formula && !string.IsNullOrEmpty(c.FormulaStr))
                {
                    FormulaEngine.CurrentEvaluatingRow = SelectedRow;
                    FormulaEngine.CurrentEvaluatingCol = SelectedCol;
                    try
                    {
                        CellData res = FormulaEngine.EvaluateFormula(c.FormulaStr, CurrentSheet);
                        c.EvaluatedStr = res.EvaluatedStr;
                        c.NumericValue = res.NumericValue;
                    }
                    finally
                    {
                        FormulaEngine.CurrentEvaluatingRow = 0;
                        FormulaEngine.CurrentEvaluatingCol = 0;
                    }
                }
            }
        }

        public void CalculateNow(bool fullForce = false)
        {
            SyncFormulaEngineMetadata();
            if (Sheets != null && Sheets.Count > 0)
            {
                FormulaEngine.EvaluateWorkbook(Sheets, SheetNames);
            }
            else if (CurrentSheet != null)
            {
                RecalcAfterEdit();
            }
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
                        ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Đã bật chế độ Bảo vệ Sheet thành công!\nCác ô tính hiện đang ở chế độ chỉ đọc để tránh vô tình chỉnh sửa.", "Sheet protection enabled!\nCells are now read-only to prevent accidental changes."), AppL10n.T("Bảo Vệ Trang Tính", "Protect Sheet"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Mở khóa trang tính thành công! Bạn có thể tự do chỉnh sửa lại các ô.", "Sheet unprotected! You can now freely edit cells."), AppL10n.T("Mở Khóa Trang Tính", "Unprotect Sheet"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Mật khẩu không chính xác! Không thể mở khóa trang tính.", "Incorrect password! Could not unprotect sheet."), AppL10n.T("Lỗi Mật Khẩu", "Password Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else
                {
                    IsSheetProtected = false;
                    ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Đã mở khóa trang tính thành công! Bạn có thể tự do chỉnh sửa lại các ô.", "Sheet unprotected! You can now freely edit cells."), AppL10n.T("Mở Khóa Trang Tính", "Unprotect Sheet"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                string cellCoord = string.Format(AppL10n.T("Ô {0}{1}", "Cell {0}{1}"), SparseMatrix.ColIndexToName(c), r);

                foreach (var dk in dict.Keys)
                {
                    if (string.Equals(text, dk, StringComparison.OrdinalIgnoreCase))
                    {
                        var scItem = new SpellCheckItem
                        {
                            Location = cellCoord,
                            OriginalText = text,
                            ProblemWord = text,
                            Reason = AppL10n.T("Từ chưa có dấu chuẩn tiếng Việt", "Missing Vietnamese diacritics"),
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
                            Reason = AppL10n.T("Phát hiện từ lặp lại hai lần liên tiếp", "Repeated consecutive word"),
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
                            Reason = AppL10n.T("Lỗi viết hoa bất thường (Caps Lock)", "Irregular capitalization (Caps Lock)"),
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

                if (VietnameseAutoCorrect.IsEnabled)
                {
                    foreach (var wStr in words)
                    {
                        string cleanW = wStr.Trim(new char[] { '.', ',', ';', ':', '!', '?', '(', ')', '"', '\'', '-', '–' });
                        string corrW;
                        if (!string.IsNullOrEmpty(cleanW) && VietnameseAutoCorrect.TryCorrectWord(cleanW, out corrW))
                        {
                            string wordToFix = cleanW;
                            var scItem = new SpellCheckItem
                            {
                                Location = cellCoord,
                                OriginalText = text,
                                ProblemWord = cleanW,
                                Reason = AppL10n.T("Lỗi gõ chính tả tiếng Việt (Telex / VNI)", "Vietnamese typing spelling error (Telex / VNI)"),
                                ApplyCorrection = (newVal) =>
                                {
                                    string updatedCell = (cell.RawValue ?? "").Replace(wordToFix, newVal);
                                    cell.RawValue = updatedCell;
                                    cell.EvaluatedStr = updatedCell;
                                }
                            };
                            scItem.Suggestions.Add(corrW);
                            items.Add(scItem);
                        }
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
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Kiểm tra chính tả hoàn tất: Tất cả các ô văn bản trên trang tính đều hợp lệ!", "Spelling check complete: all cells are valid!"), "ZenQ Spelling", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            RecalcAfterEdit();
        }

        public void ForEachSelectedCell(Action<CellData> action)
        {
            SaveUndoState();
            HashSet<ulong> visited = new HashSet<ulong>();
            foreach (var rng in GetAllSelectedRanges())
            {
                uint capR = rng.MaxRow;
                uint capC = rng.MaxCol;
                // Cap full-sheet/col/row formatting safely to used range or 100 rows/cols around data so formatting never hangs
                if (capR >= CalcMaxRows && CurrentSheet.GetMaxRow() < CalcMaxRows)
                    capR = Math.Max(rng.MinRow, Math.Max(50u, CurrentSheet.GetMaxRow()));
                if (capC >= CalcMaxCols && CurrentSheet.GetMaxCol() < CalcMaxCols)
                    capC = Math.Max(rng.MinCol, Math.Max(26u, CurrentSheet.GetMaxCol()));

                for (uint r = rng.MinRow; r <= capR; r++)
                {
                    for (uint c = rng.MinCol; c <= capC; c++)
                    {
                        ulong key = ((ulong)r << 32) | c;
                        if (!visited.Add(key)) continue;
                        CellData cell = CurrentSheet.GetOrCreateCell(r, c);
                        action(cell);
                    }
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
            }

            int saW = 480;
            int saH = 220;
            DocShapeObject newSh = new DocShapeObject("SmartArt", startX + 20, startY + 20, saW, saH);
            newSh.SmartArtType = type;
            newSh.SmartArtTitle = name;
            string[] defNodes = SmartArtRenderer.GetSmartArtNodeList(type, "");
            newSh.TextContent = (defNodes != null && defNodes.Length > 0) ? string.Join("|", defNodes) : "";
            newSh.FillColor = Color.FromArgb(248, 250, 252);
            newSh.OutlineColor = Color.FromArgb(16, 124, 65); // Excel Green accent
            newSh.OutlineWidth = 1.5f;

            Shapes.Add(newSh);
            SelectedShape = newSh;
        }

        public void DrawChartFloatingButtons(D2DGraphics g, Rectangle chartRect)
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
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

            float z = ZoomFactor > 0 ? ZoomFactor : 1.0f;
            int screenX = headerW + (int)Math.Round(shape.PageX * z) - scrollPixelX;
            int screenY = startY + headerH + (int)Math.Round(shape.PageY * z) - scrollPixelY;
            int screenW = (int)Math.Round((shape.BaseWidth + shape.WidthDelta) * z);
            int screenH = (int)Math.Round((shape.BaseHeight + shape.HeightDelta) * z);

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

        public void DrawSparklineCell(D2DGraphics g, Rectangle cellRect, SparklineInfo spk)
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
            }

            if (Shapes == null) Shapes = new List<DocShapeObject>();
            foreach (var s in Shapes) { s.IsSelected = false; s.IsEditingText = false; }

            DocShapeObject wa = new DocShapeObject
            {
                Id = "shape_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ShapeType = "WordArt",
                TextContent = "ZenQ Calc",
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
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
                for (uint c = 1; c < SelectedCol; c++) startX += GetRawColWidth(c);
                startY = 0;
                for (uint r = 1; r < SelectedRow; r++) startY += GetRawRowHeight(r);
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
            OpenRemoveDuplicatesDialog(parentForm);
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
        public void ExecuteMoveRange(uint srcStartR, uint srcStartC, uint srcEndR, uint srcEndC, uint dstStartR, uint dstStartC, bool copyOnly)
        {
            if (CurrentSheet == null) return;
            SaveUndoState();

            uint minR = Math.Min(srcStartR, srcEndR);
            uint maxR = Math.Max(srcStartR, srcEndR);
            uint minC = Math.Min(srcStartC, srcEndC);
            uint maxC = Math.Max(srcStartC, srcEndC);

            int rowSpan = (int)(maxR - minR + 1);
            int colSpan = (int)(maxC - minC + 1);

            int dRow = (int)dstStartR - (int)minR;
            int dCol = (int)dstStartC - (int)minC;

            // 1. Snapshot all source cells
            List<Tuple<uint, uint, CellData>> sourceCells = new List<Tuple<uint, uint, CellData>>();
            for (uint r = minR; r <= maxR; r++)
            {
                for (uint c = minC; c <= maxC; c++)
                {
                    CellData cell = CurrentSheet.GetCell(r, c);
                    if (cell != null)
                    {
                        sourceCells.Add(Tuple.Create(r, c, cell.Clone()));
                    }
                }
            }

            // 2. If moving (not copying), clear source cells
            if (!copyOnly)
            {
                for (uint r = minR; r <= maxR; r++)
                {
                    for (uint c = minC; c <= maxC; c++)
                    {
                        CurrentSheet.ClearCell(r, c);
                    }
                }
            }

            // 3. Paste into destination cells with adjusted coordinates & formulas
            foreach (var item in sourceCells)
            {
                uint targetR = (uint)(item.Item1 + dRow);
                uint targetC = (uint)(item.Item2 + dCol);
                CellData targetCell = CurrentSheet.GetOrCreateCell(targetR, targetC);

                CellData srcCell = item.Item3;
                targetCell.Type = srcCell.Type;
                targetCell.RawValue = srcCell.RawValue;
                targetCell.EvaluatedStr = srcCell.EvaluatedStr;
                targetCell.NumericValue = srcCell.NumericValue;
                targetCell.FormulaStr = AdjustFormulaOffset(srcCell.FormulaStr, dRow, dCol);
                targetCell.Format = srcCell.Format != null ? srcCell.Format.Clone() : null;
            }

            // 4. Update Selection to destination
            SelectedRow = dstStartR;
            SelectedCol = dstStartC;
            SelEndRow = (uint)(dstStartR + rowSpan - 1);
            SelEndCol = (uint)(dstStartC + colSpan - 1);

            // 5. Evaluate and refresh
            RecalcAfterEdit();
        }

        public static string AdjustFormulaOffset(string formula, int dRow, int dCol)
        {
            if (string.IsNullOrEmpty(formula) || !formula.StartsWith("=")) return formula;
            // Skip quoted string literals "..." and sheet names '...'!, respect $ anchors and skip LOG10 / ATAN2
            return System.Text.RegularExpressions.Regex.Replace(
                formula,
                @"""(?:[^""]|"""")*""|'(?:[^']|'')*'!|(?<![A-Za-z0-9_.])(\$?)([A-Za-z]{1,3})(\$?)([0-9]{1,7})(?![A-Za-z0-9_(])",
                m =>
                {
                    if (string.IsNullOrEmpty(m.Groups[2].Value)) return m.Value;
                    string cLock = m.Groups[1].Value;
                    string colName = m.Groups[2].Value;
                    string rLock = m.Groups[3].Value;
                    uint rowNum;
                    if (!uint.TryParse(m.Groups[4].Value, out rowNum) || rowNum == 0) return m.Value;

                    uint colIdx = SparseMatrix.ColNameToIndex(colName);
                    if (colIdx == 0 || colIdx > 16384 || rowNum > 1048576) return m.Value;

                    int newCol = string.IsNullOrEmpty(cLock) ? Math.Max(1, (int)colIdx + dCol) : (int)colIdx;
                    int newRow = string.IsNullOrEmpty(rLock) ? Math.Max(1, (int)rowNum + dRow) : (int)rowNum;
                    return cLock + SparseMatrix.ColIndexToName((uint)newCol) + rLock + newRow;
                });
        }

        /// <summary>
        /// Native vector PDF export honoring orientation, paper size, margins, gridlines, fonts, styling, 2D pagination, and floating objects.
        /// </summary>
        public bool ExportToPdf(string filePath)
        {
            return ExportToPdf(filePath, false);
        }

        public bool ExportToPdf(string filePath, bool exportAllSheets)
        {
            return ZenQOffice.Format.CalcPdfExporter.Export(this, filePath, exportAllSheets);
        }

        #region Clipboard & Paste Special

        public void Copy()
        {
            if (CurrentSheet == null) return;
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            _clipboardCells.Clear();
            _clipboardSourceSheet = CurrentSheet;
            _clipSourceStartRow = minR;
            _clipSourceStartCol = minC;
            _clipRowCount = maxR - minR + 1;
            _clipColCount = maxC - minC + 1;
            _clipboardIsCut = false;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (uint r = minR; r <= maxR; r++)
            {
                if (r > minR) sb.AppendLine();
                for (uint c = minC; c <= maxC; c++)
                {
                    if (c > minC) sb.Append('\t');
                    CellData cell = CurrentSheet.GetCell(r, c);
                    if (cell != null)
                    {
                        _clipboardCells.Add(Tuple.Create(r - minR, c - minC, cell.Clone()));
                        string text = cell.EvaluatedStr ?? cell.RawValue ?? "";
                        sb.Append(text);
                    }
                }
            }

            try
            {
                if (sb.Length > 0)
                {
                    Clipboard.SetText(sb.ToString());
                }
            }
            catch { }
        }

        public void Cut()
        {
            Copy();
            _clipboardIsCut = true;
        }

        public void Paste()
        {
            if (CurrentSheet == null) return;
            SaveUndoState();

            if (_clipboardCells != null && _clipboardCells.Count > 0)
            {
                uint dstR = SelectedRow;
                uint dstC = SelectedCol;
                int dRow = (int)dstR - (int)_clipSourceStartRow;
                int dCol = (int)dstC - (int)_clipSourceStartCol;

                if (_clipboardIsCut && _clipboardSourceSheet != null)
                {
                    for (uint r = _clipSourceStartRow; r < _clipSourceStartRow + _clipRowCount; r++)
                    {
                        for (uint c = _clipSourceStartCol; c < _clipSourceStartCol + _clipColCount; c++)
                        {
                            _clipboardSourceSheet.ClearCell(r, c);
                        }
                    }
                    _clipboardIsCut = false;
                }

                foreach (var item in _clipboardCells)
                {
                    uint targetR = dstR + item.Item1;
                    uint targetC = dstC + item.Item2;
                    CellData targetCell = CurrentSheet.GetOrCreateCell(targetR, targetC);
                    CellData srcCell = item.Item3;

                    targetCell.Type = srcCell.Type;
                    targetCell.RawValue = srcCell.RawValue;
                    targetCell.EvaluatedStr = srcCell.EvaluatedStr;
                    targetCell.NumericValue = srcCell.NumericValue;
                    targetCell.FormulaStr = AdjustFormulaOffset(srcCell.FormulaStr, dRow, dCol);
                    targetCell.Format = srcCell.Format != null ? srcCell.Format.Clone() : null;
                }

                SelEndRow = dstR + _clipRowCount - 1;
                SelEndCol = dstC + _clipColCount - 1;
            }
            else
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string clipText = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(clipText))
                        {
                            string[] lines = clipText.Replace("\r\n", "\n").Split('\n');
                            uint curR = SelectedRow;
                            uint maxTargetCol = SelectedCol;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i])) break;
                                string[] tokens = lines[i].Split('\t');
                                for (int j = 0; j < tokens.Length; j++)
                                {
                                    uint targetCol = SelectedCol + (uint)j;
                                    if (targetCol > maxTargetCol) maxTargetCol = targetCol;
                                    CurrentSheet.SetCellText(curR, targetCol, tokens[j]);
                                }
                                curR++;
                            }
                            SelEndRow = Math.Max(SelectedRow, curR - 1);
                            SelEndCol = maxTargetCol;
                        }
                    }
                }
                catch { }
            }

            RecalcAfterEdit();
            Invalidate();
            if (ParentForm != null) ParentForm.Invalidate();
        }

        public void PasteValues()
        {
            PasteSpecial(PasteSpecialType.Values, PasteOperation.None, false, false);
        }

        public void PasteFormulas()
        {
            PasteSpecial(PasteSpecialType.Formulas, PasteOperation.None, false, false);
        }

        public void PasteTranspose()
        {
            PasteSpecial(PasteSpecialType.All, PasteOperation.None, false, true);
        }

        public void PasteSpecial(PasteSpecialType type, PasteOperation op, bool skipBlanks, bool transpose)
        {
            if (CurrentSheet == null) return;
            SaveUndoState();

            uint dstR = SelectedRow;
            uint dstC = SelectedCol;

            if (_clipboardCells != null && _clipboardCells.Count > 0)
            {
                int dRow = (int)dstR - (int)_clipSourceStartRow;
                int dCol = (int)dstC - (int)_clipSourceStartCol;

                foreach (var item in _clipboardCells)
                {
                    uint relR = item.Item1;
                    uint relC = item.Item2;
                    uint targetR = transpose ? (dstR + relC) : (dstR + relR);
                    uint targetC = transpose ? (dstC + relR) : (dstC + relC);

                    CellData srcCell = item.Item3;
                    if (skipBlanks && (srcCell == null || string.IsNullOrEmpty(srcCell.RawValue)))
                        continue;

                    CellData targetCell = CurrentSheet.GetOrCreateCell(targetR, targetC);

                    if (op != PasteOperation.None)
                    {
                        double srcNum = !double.IsNaN(srcCell.NumericValue) ? srcCell.NumericValue : 0.0;
                        if (double.IsNaN(srcCell.NumericValue) && !string.IsNullOrEmpty(srcCell.RawValue))
                            double.TryParse(srcCell.RawValue, out srcNum);

                        double dstNum = !double.IsNaN(targetCell.NumericValue) ? targetCell.NumericValue : 0.0;
                        if (double.IsNaN(targetCell.NumericValue) && !string.IsNullOrEmpty(targetCell.RawValue))
                            double.TryParse(targetCell.RawValue, out dstNum);

                        double res = dstNum;
                        if (op == PasteOperation.Add) res = dstNum + srcNum;
                        else if (op == PasteOperation.Subtract) res = dstNum - srcNum;
                        else if (op == PasteOperation.Multiply) res = dstNum * srcNum;
                        else if (op == PasteOperation.Divide) res = (srcNum != 0) ? (dstNum / srcNum) : 0;

                        targetCell.Type = CellType.Number;
                        targetCell.NumericValue = res;
                        targetCell.RawValue = res.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        targetCell.FormulaStr = null;
                    }
                    else
                    {
                        switch (type)
                        {
                            case PasteSpecialType.Values:
                                targetCell.Type = srcCell.Type == CellType.Formula ? (double.IsNaN(srcCell.NumericValue) ? CellType.String : CellType.Number) : srcCell.Type;
                                targetCell.RawValue = srcCell.EvaluatedStr ?? srcCell.RawValue;
                                targetCell.NumericValue = srcCell.NumericValue;
                                targetCell.FormulaStr = null;
                                break;

                            case PasteSpecialType.Formulas:
                                targetCell.Type = srcCell.Type;
                                targetCell.FormulaStr = AdjustFormulaOffset(srcCell.FormulaStr, dRow, dCol);
                                targetCell.RawValue = srcCell.RawValue;
                                targetCell.NumericValue = srcCell.NumericValue;
                                break;

                            case PasteSpecialType.Formats:
                                if (srcCell.Format != null) targetCell.Format = srcCell.Format.Clone();
                                break;

                            case PasteSpecialType.AllExceptBorders:
                                targetCell.Type = srcCell.Type;
                                targetCell.RawValue = srcCell.RawValue;
                                targetCell.EvaluatedStr = srcCell.EvaluatedStr;
                                targetCell.NumericValue = srcCell.NumericValue;
                                targetCell.FormulaStr = AdjustFormulaOffset(srcCell.FormulaStr, dRow, dCol);
                                if (srcCell.Format != null)
                                {
                                    targetCell.Format = srcCell.Format.Clone();
                                    targetCell.Format.Borders = null;
                                }
                                break;

                            case PasteSpecialType.FormulasAndNumberFormats:
                                targetCell.Type = srcCell.Type;
                                targetCell.FormulaStr = AdjustFormulaOffset(srcCell.FormulaStr, dRow, dCol);
                                targetCell.RawValue = srcCell.RawValue;
                                targetCell.NumericValue = srcCell.NumericValue;
                                if (srcCell.Format != null)
                                {
                                    if (targetCell.Format == null) targetCell.Format = new CellFormat();
                                    targetCell.Format.NumberFormat = srcCell.Format.NumberFormat;
                                    targetCell.Format.DecimalPlaces = srcCell.Format.DecimalPlaces;
                                }
                                break;

                            case PasteSpecialType.ValuesAndNumberFormats:
                                targetCell.Type = srcCell.Type == CellType.Formula ? (double.IsNaN(srcCell.NumericValue) ? CellType.String : CellType.Number) : srcCell.Type;
                                targetCell.RawValue = srcCell.EvaluatedStr ?? srcCell.RawValue;
                                targetCell.NumericValue = srcCell.NumericValue;
                                targetCell.FormulaStr = null;
                                if (srcCell.Format != null)
                                {
                                    if (targetCell.Format == null) targetCell.Format = new CellFormat();
                                    targetCell.Format.NumberFormat = srcCell.Format.NumberFormat;
                                    targetCell.Format.DecimalPlaces = srcCell.Format.DecimalPlaces;
                                }
                                break;

                            case PasteSpecialType.ColumnWidths:
                                if (CustomColWidths != null && CustomColWidths.ContainsKey(_clipSourceStartCol + relC))
                                {
                                    int w = CustomColWidths[_clipSourceStartCol + relC];
                                    CustomColWidths[targetC] = w;
                                }
                                break;

                            default: // All
                                targetCell.Type = srcCell.Type;
                                targetCell.RawValue = srcCell.RawValue;
                                targetCell.EvaluatedStr = srcCell.EvaluatedStr;
                                targetCell.NumericValue = srcCell.NumericValue;
                                targetCell.FormulaStr = AdjustFormulaOffset(srcCell.FormulaStr, dRow, dCol);
                                targetCell.Format = srcCell.Format != null ? srcCell.Format.Clone() : null;
                                break;
                        }
                    }
                }
            }

            RecalcAfterEdit();
            Invalidate();
            if (ParentForm != null) ParentForm.Invalidate();
        }

        public void OpenPasteSpecialDialog(Form parentForm = null)
        {
            Form form = parentForm ?? ParentForm ?? Form.ActiveForm;
            using (CalcPasteSpecialDialog dlg = new CalcPasteSpecialDialog())
            {
                if (dlg.ShowDialog(form) == DialogResult.OK)
                {
                    PasteSpecial(dlg.SelectedType, dlg.SelectedOperation, dlg.SkipBlanks, dlg.Transpose);
                }
            }
        }

        public void OpenGoalSeekDialog(Form parentForm = null)
        {
            Form form = parentForm ?? ParentForm ?? Form.ActiveForm;
            using (CalcGoalSeekDialog dlg = new CalcGoalSeekDialog(this))
            {
                dlg.ShowDialog(form);
            }
        }

        public void FlashFill()
        {
            if (CurrentSheet == null || SelectedCol <= 1) return;
            SaveUndoState();

            uint targetCol = SelectedCol;
            uint sourceCol = SelectedCol - 1;
            uint startR = SelectedRow;

            uint exampleRow = startR;
            CellData exDestCell = CurrentSheet.GetCell(exampleRow, targetCol);
            CellData exSrcCell = CurrentSheet.GetCell(exampleRow, sourceCol);

            if (exDestCell == null || string.IsNullOrEmpty(exDestCell.RawValue))
            {
                if (exampleRow > 1)
                {
                    exampleRow--;
                    exDestCell = CurrentSheet.GetCell(exampleRow, targetCol);
                    exSrcCell = CurrentSheet.GetCell(exampleRow, sourceCol);
                }
            }

            if (exDestCell == null || exSrcCell == null ||
                string.IsNullOrEmpty(exDestCell.RawValue) || string.IsNullOrEmpty(exSrcCell.RawValue))
            {
                return;
            }

            string srcEx = exSrcCell.RawValue.Trim();
            string dstEx = exDestCell.RawValue.Trim();

            Func<string, string> rule = null;

            string[] srcParts = srcEx.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (srcParts.Length > 1)
            {
                if (string.Equals(srcParts[srcParts.Length - 1], dstEx, StringComparison.OrdinalIgnoreCase))
                {
                    rule = (s) =>
                    {
                        string[] p = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return (p.Length > 0) ? p[p.Length - 1] : "";
                    };
                }
                else if (string.Equals(srcParts[0], dstEx, StringComparison.OrdinalIgnoreCase))
                {
                    rule = (s) =>
                    {
                        string[] p = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return (p.Length > 0) ? p[0] : "";
                    };
                }
            }

            if (rule == null && srcEx.ToUpperInvariant() == dstEx)
            {
                rule = (s) => s.ToUpperInvariant();
            }
            else if (rule == null && srcEx.ToLowerInvariant() == dstEx)
            {
                rule = (s) => s.ToLowerInvariant();
            }
            else if (rule == null && srcEx.Contains("@"))
            {
                if (dstEx == srcEx.Substring(0, srcEx.IndexOf('@')))
                {
                    rule = (s) => { int at = s.IndexOf('@'); return at >= 0 ? s.Substring(0, at) : s; };
                }
                else if (dstEx == srcEx.Substring(srcEx.IndexOf('@') + 1))
                {
                    rule = (s) => { int at = s.IndexOf('@'); return at >= 0 ? s.Substring(at + 1) : s; };
                }
            }

            if (rule == null)
            {
                if (dstEx.StartsWith(srcEx))
                {
                    string suffix = dstEx.Substring(srcEx.Length);
                    rule = (s) => s + suffix;
                }
                else if (dstEx.EndsWith(srcEx))
                {
                    string prefix = dstEx.Substring(0, dstEx.Length - srcEx.Length);
                    rule = (s) => prefix + s;
                }
            }

            if (rule == null) return;

            uint fillStart = exampleRow + 1;
            for (uint r = fillStart; r < fillStart + 1000; r++)
            {
                CellData srcC = CurrentSheet.GetCell(r, sourceCol);
                if (srcC == null || string.IsNullOrEmpty(srcC.RawValue)) break;

                string generated = rule(srcC.RawValue.Trim());
                CurrentSheet.SetCellText(r, targetCol, generated);
            }

            RecalcAfterEdit();
            Invalidate();
            if (ParentForm != null) ParentForm.Invalidate();
        }

        #endregion

        #endregion

        // ==================== Multi-Range, Point Mode, ScreenTip & GoTo Helpers ====================

        public List<CalcSelRange> GetAllSelectedRanges()
        {
            var res = new List<CalcSelRange>();
            if (AdditionalSelections != null && AdditionalSelections.Count > 0)
            {
                res.AddRange(AdditionalSelections);
            }
            res.Add(new CalcSelRange(SelectedRow, SelectedCol, SelEndRow, SelEndCol));
            return res;
        }

        public void ClearAdditionalSelections()
        {
            if (AdditionalSelections != null) AdditionalSelections.Clear();
        }

        public void CommitPrimarySelectionToAdditional()
        {
            AdditionalSelections.Add(new CalcSelRange(SelectedRow, SelectedCol, SelEndRow, SelEndCol));
        }

        public bool IsColSelected(uint c)
        {
            foreach (var rng in GetAllSelectedRanges())
            {
                if (c >= rng.MinCol && c <= rng.MaxCol) return true;
            }
            return false;
        }

        public bool IsRowSelected(uint r)
        {
            foreach (var rng in GetAllSelectedRanges())
            {
                if (r >= rng.MinRow && r <= rng.MaxRow) return true;
            }
            return false;
        }

        private string GetDisplayNameBoxText()
        {
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            if (DefinedNames != null)
            {
                string curRangeRef = (minR == maxR && minC == maxC)
                    ? (SparseMatrix.ColIndexToName(minC) + minR)
                    : string.Format("{0}{1}:{2}{3}", SparseMatrix.ColIndexToName(minC), minR, SparseMatrix.ColIndexToName(maxC), maxR);
                foreach (var dn in DefinedNames)
                {
                    if (dn == null || string.IsNullOrWhiteSpace(dn.RefersTo)) continue;
                    string rt = dn.RefersTo.Trim().TrimStart('=').Replace("$", "");
                    if (string.Equals(rt, curRangeRef, StringComparison.OrdinalIgnoreCase))
                        return dn.Name;
                }
            }

            if (minR != maxR || minC != maxC)
            {
                return string.Format("{0}R x {1}C", (maxR - minR + 1), (maxC - minC + 1));
            }
            return SparseMatrix.ColIndexToName(SelectedCol) + SelectedRow;
        }

        private void ComputeSelectionStatusMetrics(out double selSum, out double selCount, out double numCount)
        {
            selSum = 0.0;
            selCount = 0;
            numCount = 0;
            if (CurrentSheet == null) return;
            HashSet<ulong> seen = new HashSet<ulong>();

            foreach (var rng in GetAllSelectedRanges())
            {
                uint capR = Math.Min(rng.MaxRow, Math.Max(rng.MinRow, CurrentSheet.GetMaxRow()));
                uint capC = Math.Min(rng.MaxCol, Math.Max(rng.MinCol, CurrentSheet.GetMaxCol()));
                for (uint r = rng.MinRow; r <= capR; r++)
                {
                    for (uint c = rng.MinCol; c <= capC; c++)
                    {
                        ulong key = ((ulong)r << 32) | c;
                        if (!seen.Add(key)) continue;
                        CellData cd = CurrentSheet.GetCell(r, c);
                        if (cd != null && (!string.IsNullOrEmpty(cd.RawValue) || !string.IsNullOrEmpty(cd.EvaluatedStr)))
                        {
                            selCount++;
                            double parsed;
                            if (cd.Type == CellType.Number)
                            {
                                selSum += cd.NumericValue;
                                numCount++;
                            }
                            else if (cd.Type == CellType.Formula && !FormulaEngine.IsExcelError(cd.EvaluatedStr))
                            {
                                if (cd.NumericValue != 0 || double.TryParse(cd.EvaluatedStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                                {
                                    selSum += cd.NumericValue;
                                    numCount++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AutoScrollDuringDrag(int x, int y, int width, int height, int gridTop, int gridLeft)
        {
            if (x > width - 20 && ScrollCol < CalcMaxCols) ScrollCol++;
            else if (x < gridLeft + 10 && ScrollCol > 1) ScrollCol--;
            if (y > height - 70 && ScrollRow < CalcMaxRows) ScrollRow++;
            else if (y < gridTop + 10 && ScrollRow > 1) ScrollRow--;
        }

        public static bool CanAcceptFormulaReferenceBeforeCursor(string text, int cursorPos)
        {
            if (string.IsNullOrEmpty(text) || !text.TrimStart().StartsWith("=")) return false;
            int idx = Math.Min(cursorPos, text.Length) - 1;
            while (idx >= 0 && text[idx] == ' ') idx--;
            if (idx < 0) return false;
            char ch = text[idx];
            return ch == '=' || ch == '(' || ch == ',' || ch == ';' || ch == '+' || ch == '-' ||
                   ch == '*' || ch == '/' || ch == '^' || ch == '&' || ch == '<' || ch == '>' ||
                   ch == ':' || ch == '{';
        }

        public static string FormatSheetPrefix(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName)) return "";
            bool needsQuote = false;
            for (int i = 0; i < sheetName.Length; i++)
            {
                char ch = sheetName[i];
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                {
                    needsQuote = true;
                    break;
                }
            }
            if (needsQuote || char.IsDigit(sheetName[0]))
            {
                return "'" + sheetName.Replace("'", "''") + "'!";
            }
            return sheetName + "!";
        }

        public void BeginOrUpdatePointRange(uint r1, uint c1, uint r2, uint c2, bool insertCommaSeparator)
        {
            if (FormulaSourceSheetIndex < 0)
            {
                FormulaSourceSheetIndex = ActiveSheetIndex;
                FormulaSourceRow = SelectedRow;
                FormulaSourceCol = SelectedCol;
            }

            uint minR = Math.Min(r1, r2);
            uint maxR = Math.Max(r1, r2);
            uint minC = Math.Min(c1, c2);
            uint maxC = Math.Max(c1, c2);

            string refStr = (minR == maxR && minC == maxC)
                ? (SparseMatrix.ColIndexToName(minC) + minR)
                : string.Format("{0}{1}:{2}{3}", SparseMatrix.ColIndexToName(minC), minR, SparseMatrix.ColIndexToName(maxC), maxR);

            if (ActiveSheetIndex != FormulaSourceSheetIndex && ActiveSheetIndex >= 0 && ActiveSheetIndex < SheetNames.Count)
            {
                refStr = FormatSheetPrefix(SheetNames[ActiveSheetIndex]) + refStr;
            }

            string cur = EditText ?? "=";
            int pos = Math.Max(0, Math.Min(cur.Length, EditCursorPos));

            if (insertCommaSeparator)
            {
                // Ctrl+Click starts a separate comma-separated argument in formula
                if (!CanAcceptFormulaReferenceBeforeCursor(cur, pos) && !cur.EndsWith(","))
                {
                    cur = cur.Insert(pos, ",");
                    pos++;
                }
                _pointInsertStart = -1;
                _pointInsertLength = 0;
            }

            if (_pointInsertStart >= 0 && _pointInsertStart + _pointInsertLength <= cur.Length)
            {
                cur = cur.Substring(0, _pointInsertStart) + refStr + cur.Substring(_pointInsertStart + _pointInsertLength);
                _pointInsertLength = refStr.Length;
                EditText = cur;
                EditCursorPos = _pointInsertStart + refStr.Length;
            }
            else
            {
                EditText = cur.Insert(pos, refStr);
                _pointInsertStart = pos;
                _pointInsertLength = refStr.Length;
                EditCursorPos = pos + refStr.Length;
            }

            EditSelStart = EditCursorPos;
            EditSelLength = 0;
            IsFormulaPointMode = true;
            FilterFormulaSuggestions();
        }

        public static string GetFormulaTokenBeforeCursor(string text, int cursorPos, out int tokenStart, out int tokenLen)
        {
            tokenStart = 0;
            tokenLen = 0;
            if (string.IsNullOrEmpty(text)) return "";
            int pos = Math.Max(0, Math.Min(text.Length, cursorPos));
            int start = pos;
            while (start > 0)
            {
                char ch = text[start - 1];
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.') start--;
                else break;
            }
            tokenStart = start;
            tokenLen = pos - start;
            return text.Substring(start, tokenLen);
        }

        private void GetActiveFunctionParameterContext(string formula, int cursorPos, out FormulaSuggestion meta, out int activeArgIndex)
        {
            meta = null;
            activeArgIndex = 0;
            if (string.IsNullOrEmpty(formula) || !formula.TrimStart().StartsWith("=") || AllFormulas == null) return;

            int pos = Math.Max(0, Math.Min(formula.Length, cursorPos));
            bool inStr = false;
            int depth = 0;
            int commasAtDepth0 = 0;
            var stack = new Stack<int>();
            int[] commasByOpenIdx = new int[pos + 1];

            for (int i = 0; i < pos; i++)
            {
                char ch = formula[i];
                if (ch == '"') { inStr = !inStr; continue; }
                if (inStr) continue;
                if (ch == '(')
                {
                    stack.Push(i);
                    commasByOpenIdx[i] = 0;
                    depth++;
                }
                else if (ch == ')')
                {
                    if (stack.Count > 0) stack.Pop();
                    if (depth > 0) depth--;
                }
                else if (ch == ',' && stack.Count > 0)
                {
                    commasByOpenIdx[stack.Peek()]++;
                }
            }

            if (stack.Count == 0) return;
            int openParenIdx = stack.Peek();
            commasAtDepth0 = commasByOpenIdx[openParenIdx];

            int fnEnd = openParenIdx;
            int fnStart = fnEnd;
            while (fnStart > 0 && (char.IsLetterOrDigit(formula[fnStart - 1]) || formula[fnStart - 1] == '_' || formula[fnStart - 1] == '.'))
            {
                fnStart--;
            }
            if (fnStart == fnEnd) return;
            string fnName = formula.Substring(fnStart, fnEnd - fnStart);

            foreach (var fs in AllFormulas)
            {
                if (string.Equals(fs.Name, fnName, StringComparison.OrdinalIgnoreCase))
                {
                    meta = fs;
                    activeArgIndex = commasAtDepth0;
                    return;
                }
            }
        }

        private void DrawFormulaScreenTip(D2DGraphics g, int x, int y, int maxWidth)
        {
            FormulaSuggestion meta;
            int activeArg;
            GetActiveFunctionParameterContext(EditText, EditCursorPos, out meta, out activeArg);
            if (meta == null || string.IsNullOrEmpty(meta.Syntax)) return;

            string syntax = meta.Syntax.TrimStart('=');
            int openIdx = syntax.IndexOf('(');
            int closeIdx = syntax.LastIndexOf(')');
            if (openIdx < 0 || closeIdx <= openIdx) return;

            string head = syntax.Substring(0, openIdx + 1);
            string inner = syntax.Substring(openIdx + 1, closeIdx - openIdx - 1);
            string[] args = inner.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            int clampedArg = Math.Min(activeArg, Math.Max(0, args.Length - 1));
            if (args.Length > 0 && args[args.Length - 1].Trim() == "..." && activeArg >= args.Length - 1)
            {
                clampedArg = args.Length - 1;
            }

            using (Font regFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font boldFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font descFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))
            {
                float curW = g.MeasureString(head, regFont, 2000, StringFormat.GenericTypographic).Width;
                for (int i = 0; i < args.Length; i++)
                {
                    string part = (i > 0 ? ", " : "") + args[i].Trim();
                    Font f = (i == clampedArg) ? boldFont : regFont;
                    curW += g.MeasureString(part, f, 2000, StringFormat.GenericTypographic).Width;
                }
                curW += g.MeasureString(")", regFont, 2000, StringFormat.GenericTypographic).Width;

                int boxW = Math.Max(220, Math.Min(maxWidth - x - 16, (int)curW + 24));
                int boxH = 42;
                int boxX = Math.Max(8, Math.Min(maxWidth - boxW - 10, x));
                Rectangle tipRect = new Rectangle(boxX, y, boxW, boxH);

                using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(24, 0, 0, 0)))
                    g.FillRectangle(shadowB, tipRect.X + 2, tipRect.Y + 2, tipRect.Width, tipRect.Height);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(255, 255, 250)))
                    g.FillRectangle(bg, tipRect);
                using (Pen borderP = new Pen(Color.FromArgb(203, 213, 225)))
                    g.DrawRectangle(borderP, tipRect);

                float drawX = tipRect.X + 10;
                float drawY = tipRect.Y + 5;
                g.DrawString(head, boldFont, Brushes.DimGray, drawX, drawY, StringFormat.GenericTypographic);
                drawX += g.MeasureString(head, boldFont, 2000, StringFormat.GenericTypographic).Width;

                for (int i = 0; i < args.Length; i++)
                {
                    string commaPrefix = (i > 0 ? ", " : "");
                    if (!string.IsNullOrEmpty(commaPrefix))
                    {
                        g.DrawString(commaPrefix, regFont, Brushes.Gray, drawX, drawY, StringFormat.GenericTypographic);
                        drawX += g.MeasureString(commaPrefix, regFont, 2000, StringFormat.GenericTypographic).Width;
                    }
                    string argTxt = args[i].Trim();
                    bool isCur = (i == clampedArg);
                    Font af = isCur ? boldFont : regFont;
                    using (SolidBrush ab = new SolidBrush(isCur ? Color.FromArgb(16, 124, 65) : Color.FromArgb(51, 65, 85)))
                    {
                        g.DrawString(argTxt, af, ab, drawX, drawY, StringFormat.GenericTypographic);
                    }
                    drawX += g.MeasureString(argTxt, af, 2000, StringFormat.GenericTypographic).Width;
                }
                g.DrawString(")", boldFont, Brushes.DimGray, drawX, drawY, StringFormat.GenericTypographic);

                using (SolidBrush db = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    Region oldClip = g.Clip;
                    g.SetClip(tipRect);
                    g.DrawString(AppL10n.Tr(meta.Description ?? ""), descFont, db, tipRect.X + 10, tipRect.Y + 23);
                    g.Clip = oldClip;
                }
            }
        }

        private List<FormulaRefHighlight> ParseFormulaHighlightRefs(string formula)
        {
            var result = new List<FormulaRefHighlight>();
            if (string.IsNullOrEmpty(formula) || !formula.TrimStart().StartsWith("=")) return result;

            string activeSheetName = (SheetNames != null && ActiveSheetIndex >= 0 && ActiveSheetIndex < SheetNames.Count)
                ? SheetNames[ActiveSheetIndex]
                : "";
            bool onSourceSheet = (FormulaSourceSheetIndex < 0 || ActiveSheetIndex == FormulaSourceSheetIndex);

            var matches = System.Text.RegularExpressions.Regex.Matches(
                formula,
                @"""(?:[^""]|"""")*""|(?:(?:'([^']+)'|([A-Za-z0-9_]+))!)?(?<![A-Za-z0-9_.])(\$?[A-Za-z]{1,3}\$?[0-9]{1,7}(?::\$?[A-Za-z]{1,3}\$?[0-9]{1,7})?)(?![A-Za-z0-9_(])");

            int colorIdx = 0;
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (m.Value.StartsWith("\"")) continue;
                string sheetTarget = !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : m.Groups[2].Value;
                if (!string.IsNullOrEmpty(sheetTarget))
                {
                    if (!string.Equals(sheetTarget, activeSheetName, StringComparison.OrdinalIgnoreCase)) continue;
                }
                else if (!onSourceSheet)
                {
                    continue;
                }

                string rawRef = m.Groups[3].Value.Replace("$", "");
                string[] parts = rawRef.Split(':');
                uint r1, c1, r2, c2;
                if (!ParseCellAddressParts(parts[0], out r1, out c1)) continue;
                if (parts.Length == 2)
                {
                    if (!ParseCellAddressParts(parts[1], out r2, out c2)) continue;
                }
                else
                {
                    r2 = r1;
                    c2 = c1;
                }

                result.Add(new FormulaRefHighlight
                {
                    MinRow = Math.Min(r1, r2),
                    MaxRow = Math.Max(r1, r2),
                    MinCol = Math.Min(c1, c2),
                    MaxCol = Math.Max(c1, c2),
                    RefColor = FormulaRefPalette[colorIdx % FormulaRefPalette.Length]
                });
                colorIdx++;
            }
            return result;
        }

        private static bool ParseCellAddressParts(string addr, out uint row, out uint col)
        {
            row = 0;
            col = 0;
            if (string.IsNullOrEmpty(addr)) return false;
            var m = System.Text.RegularExpressions.Regex.Match(addr.Trim(), @"^([A-Za-z]{1,3})([0-9]{1,7})$");
            if (!m.Success) return false;
            col = SparseMatrix.ColNameToIndex(m.Groups[1].Value);
            return uint.TryParse(m.Groups[2].Value, out row) && row >= 1 && col >= 1 && col <= 16384;
        }

        private void DrawAdditionalSelectionRanges(D2DGraphics g, int headerW, int startY, int headerH, int width, int height)
        {
            if (AdditionalSelections == null || AdditionalSelections.Count == 0) return;
            foreach (var rng in AdditionalSelections)
            {
                if (rng.MaxCol < ScrollCol || rng.MaxRow < ScrollRow) continue;
                Rectangle rect = ComputeRangeScreenRect(rng.MinRow, rng.MaxRow, rng.MinCol, rng.MaxCol, headerW, startY + headerH);
                if (rect.Width <= 0 || rect.Height <= 0) continue;
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(26, 16, 124, 65)))
                    g.FillRectangle(fill, rect);
                using (Pen border = new Pen(Color.FromArgb(16, 124, 65), 1.5f))
                    g.DrawRectangle(border, rect);
            }
        }

        private void DrawFormulaReferenceHighlights(D2DGraphics g, int headerW, int startY, int headerH, int width, int height)
        {
            if (!IsEditing || string.IsNullOrEmpty(EditText) || !EditText.TrimStart().StartsWith("=")) return;
            var refs = ParseFormulaHighlightRefs(EditText);
            foreach (var hr in refs)
            {
                if (hr.MaxCol < ScrollCol || hr.MaxRow < ScrollRow) continue;
                Rectangle rect = ComputeRangeScreenRect(hr.MinRow, hr.MaxRow, hr.MinCol, hr.MaxCol, headerW, startY + headerH);
                if (rect.Width <= 0 || rect.Height <= 0) continue;
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(22, hr.RefColor.R, hr.RefColor.G, hr.RefColor.B)))
                    g.FillRectangle(fill, rect);
                using (Pen border = new Pen(hr.RefColor, 1.8f))
                    g.DrawRectangle(border, rect);
                using (SolidBrush cornerB = new SolidBrush(hr.RefColor))
                {
                    g.FillRectangle(cornerB, rect.Left - 2, rect.Top - 2, 5, 5);
                    g.FillRectangle(cornerB, rect.Right - 3, rect.Top - 2, 5, 5);
                    g.FillRectangle(cornerB, rect.Left - 2, rect.Bottom - 3, 5, 5);
                    g.FillRectangle(cornerB, rect.Right - 3, rect.Bottom - 3, 5, 5);
                }
            }
        }

        private Rectangle ComputeRangeScreenRect(uint minR, uint maxR, uint minC, uint maxC, int gridLeft, int gridTop)
        {
            int x1 = gridLeft;
            for (uint c = ScrollCol; c < minC; c++) x1 += GetColWidth(c);
            int x2 = x1;
            uint startC = Math.Max(minC, ScrollCol);
            for (uint c = startC; c <= Math.Min(maxC, ScrollCol + 60); c++) x2 += GetColWidth(c);

            int y1 = gridTop;
            for (uint r = ScrollRow; r < minR; r++) y1 += GetRowHeight(r);
            int y2 = y1;
            uint startR = Math.Max(minR, ScrollRow);
            for (uint r = startR; r <= Math.Min(maxR, ScrollRow + 120); r++) y2 += GetRowHeight(r);

            return new Rectangle(x1, y1, Math.Max(1, x2 - x1), Math.Max(1, y2 - y1));
        }

        public void DoubleClickFillHandle()
        {
            if (CurrentSheet == null) return;
            uint minR = Math.Min(SelectedRow, SelEndRow);
            uint maxR = Math.Max(SelectedRow, SelEndRow);
            uint minC = Math.Min(SelectedCol, SelEndCol);
            uint maxC = Math.Max(SelectedCol, SelEndCol);

            uint refCol = 0;
            if (minC > 1)
            {
                CellData leftProbe = CurrentSheet.GetCell(maxR, minC - 1);
                CellData leftBelow = CurrentSheet.GetCell(maxR + 1, minC - 1);
                if ((leftProbe != null && !string.IsNullOrEmpty(leftProbe.RawValue)) ||
                    (leftBelow != null && !string.IsNullOrEmpty(leftBelow.RawValue)))
                {
                    refCol = minC - 1;
                }
            }
            if (refCol == 0 && maxC < CalcMaxCols)
            {
                CellData rightProbe = CurrentSheet.GetCell(maxR, maxC + 1);
                CellData rightBelow = CurrentSheet.GetCell(maxR + 1, maxC + 1);
                if ((rightProbe != null && !string.IsNullOrEmpty(rightProbe.RawValue)) ||
                    (rightBelow != null && !string.IsNullOrEmpty(rightBelow.RawValue)))
                {
                    refCol = maxC + 1;
                }
            }
            if (refCol == 0) return;

            uint endR = maxR;
            while (endR < CalcMaxRows)
            {
                CellData nextC = CurrentSheet.GetCell(endR + 1, refCol);
                if (nextC != null && (!string.IsNullOrEmpty(nextC.RawValue) || !string.IsNullOrEmpty(nextC.EvaluatedStr)))
                {
                    endR++;
                }
                else
                {
                    break;
                }
            }

            if (endR > maxR)
            {
                FillStartRow = minR;
                FillEndRow = maxR;
                FillStartCol = minC;
                FillEndCol = maxC;
                FillDragTargetRow = endR;
                FillDragTargetCol = maxC;
                ExecuteAutoFill();
            }
        }

        private void ShowNameBoxDropdownMenu()
        {
            ContextMenuStrip menu = new ZenQOffice.UI.FluentContextMenuStrip();
            if (DefinedNames != null && DefinedNames.Count > 0)
            {
                foreach (var dn in DefinedNames)
                {
                    if (dn == null || string.IsNullOrWhiteSpace(dn.Name)) continue;
                    string nm = dn.Name;
                    menu.Items.Add(nm + " (" + dn.RefersTo + ")", null, (s, e) => ExecuteGoToTarget(nm));
                }
            }
            if (Tables != null && Tables.Count > 0)
            {
                if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
                foreach (var tbl in Tables)
                {
                    if (tbl == null || string.IsNullOrWhiteSpace(tbl.Name)) continue;
                    string tn = tbl.Name;
                    menu.Items.Add(AppL10n.T("Bảng: ", "Table: ") + tn, null, (s, e) => ExecuteGoToTarget(tn));
                }
            }
            if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(AppL10n.T("Đi tới / Chọn đặc biệt (F5)...", "Go To / Go To Special (F5)..."), null, (s, e) => ShowGoToDialog(Form.ActiveForm));
            menu.Show(Cursor.Position);
        }

        public void ShowGoToDialog(IWin32Window owner = null)
        {
            using (CalcGoToDialog dlg = new CalcGoToDialog(this))
            {
                DialogResult dr = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    if (dlg.SpecialMode != CalcGoToSpecialMode.None)
                    {
                        ExecuteGoToSpecial(dlg.SpecialMode);
                    }
                    else if (!string.IsNullOrWhiteSpace(dlg.TargetReference))
                    {
                        ExecuteGoToTarget(dlg.TargetReference);
                    }
                }
            }
        }

        public bool ExecuteGoToTarget(string rawTarget)
        {
            if (string.IsNullOrWhiteSpace(rawTarget)) return false;
            string target = rawTarget.Trim();

            // Check DefinedNames
            if (DefinedNames != null)
            {
                foreach (var dn in DefinedNames)
                {
                    if (dn != null && string.Equals(dn.Name, target, StringComparison.OrdinalIgnoreCase))
                    {
                        target = (dn.RefersTo ?? "").Trim().TrimStart('=');
                        break;
                    }
                }
            }

            // Check Tables
            if (Tables != null)
            {
                foreach (var tbl in Tables)
                {
                    if (tbl != null && string.Equals(tbl.Name, target, StringComparison.OrdinalIgnoreCase))
                    {
                        ClearAdditionalSelections();
                        SelectedRow = tbl.StartRow;
                        SelectedCol = tbl.StartCol;
                        SelEndRow = tbl.EndRow;
                        SelEndCol = tbl.EndCol;
                        EnsureCellVisible(SelectedRow, SelectedCol);
                        return true;
                    }
                }
            }

            // Sheet!Ref
            int bangIdx = target.IndexOf('!');
            if (bangIdx > 0)
            {
                string shName = target.Substring(0, bangIdx).Trim().Trim('\'');
                target = target.Substring(bangIdx + 1).Trim();
                if (SheetNames != null)
                {
                    for (int i = 0; i < SheetNames.Count; i++)
                    {
                        if (string.Equals(SheetNames[i], shName, StringComparison.OrdinalIgnoreCase))
                        {
                            SwitchToSheet(i);
                            break;
                        }
                    }
                }
            }

            target = target.Replace("$", "");
            string[] parts = target.Split(':');
            uint r1, c1, r2, c2;
            if (!ParseCellAddressParts(parts[0], out r1, out c1)) return false;
            if (parts.Length == 2)
            {
                if (!ParseCellAddressParts(parts[1], out r2, out c2)) return false;
            }
            else
            {
                r2 = r1;
                c2 = c1;
            }

            ClearAdditionalSelections();
            SelectedRow = r1;
            SelectedCol = c1;
            SelEndRow = r2;
            SelEndCol = c2;
            EnsureCellVisible(SelectedRow, SelectedCol);
            return true;
        }

        public bool ExecuteGoToSpecial(CalcGoToSpecialMode mode)
        {
            if (CurrentSheet == null || mode == CalcGoToSpecialMode.None) return false;
            ClearAdditionalSelections();

            if (mode == CalcGoToSpecialMode.CurrentRegion)
            {
                uint minR, maxR, minC, maxC;
                GetContiguousDataRange(SelectedRow, SelectedCol, out minR, out maxR, out minC, out maxC);
                SelectedRow = minR; SelectedCol = minC;
                SelEndRow = maxR; SelEndCol = maxC;
                EnsureCellVisible(SelectedRow, SelectedCol);
                return true;
            }

            if (mode == CalcGoToSpecialMode.LastCell)
            {
                uint lr = Math.Max(1u, CurrentSheet.GetMaxRow());
                uint lc = Math.Max(1u, CurrentSheet.GetMaxCol());
                SelectedRow = lr; SelectedCol = lc;
                SelEndRow = lr; SelEndCol = lc;
                EnsureCellVisible(SelectedRow, SelectedCol);
                return true;
            }

            // Scan either current selection range (if >1 cell) or used range
            uint scanMinR = Math.Min(SelectedRow, SelEndRow);
            uint scanMaxR = Math.Max(SelectedRow, SelEndRow);
            uint scanMinC = Math.Min(SelectedCol, SelEndCol);
            uint scanMaxC = Math.Max(SelectedCol, SelEndCol);
            if (scanMinR == scanMaxR && scanMinC == scanMaxC)
            {
                scanMinR = 1; scanMinC = 1;
                scanMaxR = Math.Max(1u, CurrentSheet.GetMaxRow());
                scanMaxC = Math.Max(1u, CurrentSheet.GetMaxCol());
            }
            else
            {
                scanMaxR = Math.Min(scanMaxR, Math.Max(scanMinR, CurrentSheet.GetMaxRow() + 10));
                scanMaxC = Math.Min(scanMaxC, Math.Max(scanMinC, CurrentSheet.GetMaxCol() + 10));
            }

            var matched = new List<Tuple<uint, uint>>();
            for (uint r = scanMinR; r <= scanMaxR; r++)
            {
                for (uint c = scanMinC; c <= scanMaxC; c++)
                {
                    CellData cd = CurrentSheet.GetCell(r, c);
                    bool isMatch = false;
                    switch (mode)
                    {
                        case CalcGoToSpecialMode.Formulas:
                            isMatch = cd != null && cd.Type == CellType.Formula && !string.IsNullOrEmpty(cd.FormulaStr);
                            break;
                        case CalcGoToSpecialMode.Constants:
                            isMatch = cd != null && cd.Type != CellType.Empty && cd.Type != CellType.Formula && !string.IsNullOrEmpty(cd.RawValue);
                            break;
                        case CalcGoToSpecialMode.Blanks:
                            isMatch = cd == null || cd.Type == CellType.Empty || (string.IsNullOrEmpty(cd.RawValue) && string.IsNullOrEmpty(cd.EvaluatedStr));
                            break;
                        case CalcGoToSpecialMode.Errors:
                            isMatch = cd != null && FormulaEngine.IsExcelError(cd.EvaluatedStr);
                            break;
                    }
                    if (isMatch) matched.Add(Tuple.Create(r, c));
                }
            }

            if (matched.Count == 0) return false;
            SelectedRow = matched[0].Item1;
            SelectedCol = matched[0].Item2;
            SelEndRow = SelectedRow;
            SelEndCol = SelectedCol;
            for (int i = 1; i < matched.Count; i++)
            {
                AdditionalSelections.Add(new CalcSelRange(matched[i].Item1, matched[i].Item2, matched[i].Item1, matched[i].Item2));
            }
            EnsureCellVisible(SelectedRow, SelectedCol);
            return true;
        }
    }


    internal static class ColorExtensions
    {
        public static SolidBrush ToBrush(this Color color)
        {
            return new SolidBrush(color);
        }
    }
}

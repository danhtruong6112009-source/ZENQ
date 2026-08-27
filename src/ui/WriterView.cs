using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;

namespace GenOffice.UI
{
    public enum HeaderFooterPreset
    {
        None,
        Blank,
        ThreeColumns,
        Austin,
        Banded
    }

    public enum TabStopType
    {
        Left = 0,    // L
        Center = 1,  // ⊥
        Right = 2,   // ⅃
        Decimal = 3, // ⊥·
        Bar = 4      // │
    }

    public class TabStopInfo
    {
        public float PositionPt { get; set; }
        public TabStopType Type { get; set; }
        public int Leader { get; set; } // 0 = None, 1 = Dots, 2 = Hyphens, 3 = Underline

        public TabStopInfo()
        {
            Type = TabStopType.Left;
            Leader = 0;
        }

        public TabStopInfo(float posPt, TabStopType type, int leader = 0)
        {
            PositionPt = posPt;
            Type = type;
            Leader = leader;
        }
    }

    public class FootnoteItem
    {
        public int Id { get; set; }
        public int PageNumber { get; set; }
        public int AnchorChar { get; set; }
        public string Marker { get; set; }
        public string Text { get; set; }
        public bool IsEndnote { get; set; }

        public FootnoteItem()
        {
            Id = 1;
            PageNumber = 1;
            AnchorChar = 0;
            Marker = "1";
            Text = "";
            IsEndnote = false;
        }
    }

    public enum RulerUnit
    {
        Centimeters = 0,
        Inches = 1
    }

    public enum RulerDragHandle
    {
        None = 0,
        FirstLineIndent = 1,  // ▼ First Line Indent
        HangingIndent = 2,    // ▲ Hanging Indent
        LeftIndent = 3,       // ■ Left Indent (Both First Line + Hanging)
        RightIndent = 4,      // ▲ Right Indent
        LeftMargin = 5,       // Left page margin
        RightMargin = 6,      // Right page margin
        TabStop = 7           // Moving a Tab Stop
    }

    public class DocumentComment
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public DateTime CreatedTime { get; set; }
        public int RangeStart { get; set; }
        public int RangeLength { get; set; }
        public string SelectedText { get; set; }
        public string Content { get; set; }
        public bool IsResolved { get; set; }
        public List<string> Replies { get; set; }

        public DocumentComment()
        {
            Author = "User";
            CreatedTime = DateTime.Now;
            Replies = new List<string>();
        }
    }

    public class FormattedRun
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strike { get; set; }
        public Color Color { get; set; }
        public Color Highlight { get; set; }
        public bool Subscript { get; set; }
        public bool Superscript { get; set; }

        public FormattedRun(int start, int len, string font, float size, bool b, bool i, bool u, bool s, Color col, Color hl, bool sub = false, bool sup = false)
        {
            StartIndex = start;
            Length = len;
            FontName = font;
            FontSize = size;
            Bold = b;
            Italic = i;
            Underline = u;
            Strike = s;
            Color = col;
            Highlight = hl;
            Subscript = sub;
            Superscript = sup;
        }
    }

    public class WriterUndoSnapshot
    {
        public string Text { get; set; }
        public List<FormattedRun> FormattedRuns { get; set; }
        public int CursorPosition { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionEnd { get; set; }
        public Dictionary<int, int> ParagraphAlignments { get; set; }
        public Dictionary<int, float> ParagraphLineSpacings { get; set; }
        public Dictionary<int, float> ParagraphSpacingsBefore { get; set; }
        public Dictionary<int, float> ParagraphSpacingsAfter { get; set; }
        public Dictionary<int, ParagraphBorderInfo> ParagraphBorders { get; set; }

        public WriterUndoSnapshot(
            string text,
            List<FormattedRun> runs,
            int cursor,
            int selStart,
            int selEnd,
            Dictionary<int, int> pAlign = null,
            Dictionary<int, float> pLineSpacing = null,
            Dictionary<int, float> pBefore = null,
            Dictionary<int, float> pAfter = null,
            Dictionary<int, ParagraphBorderInfo> pBorders = null)
        {
            Text = text ?? "";
            CursorPosition = cursor;
            SelectionStart = selStart;
            SelectionEnd = selEnd;
            FormattedRuns = new List<FormattedRun>();
            if (runs != null)
            {
                foreach (var r in runs)
                {
                    FormattedRuns.Add(new FormattedRun(r.StartIndex, r.Length, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                }
            }
            ParagraphAlignments = pAlign != null ? new Dictionary<int, int>(pAlign) : new Dictionary<int, int>();
            ParagraphLineSpacings = pLineSpacing != null ? new Dictionary<int, float>(pLineSpacing) : new Dictionary<int, float>();
            ParagraphSpacingsBefore = pBefore != null ? new Dictionary<int, float>(pBefore) : new Dictionary<int, float>();
            ParagraphSpacingsAfter = pAfter != null ? new Dictionary<int, float>(pAfter) : new Dictionary<int, float>();
            ParagraphBorders = new Dictionary<int, ParagraphBorderInfo>();
            if (pBorders != null)
            {
                foreach (var kvp in pBorders)
                {
                    ParagraphBorders[kvp.Key] = kvp.Value.Clone();
                }
            }
        }
    }

    public class DocChartObject
    {
        public string Id { get; set; }
        public DocChartData Data { get; set; }
        public int CharIndex { get; set; }
        public int PageNumber { get; set; }
        public int PageX { get; set; }
        public int PageY { get; set; }
        public int WidthDelta { get; set; }
        public int HeightDelta { get; set; }
        public int BaseWidth { get; set; }
        public int BaseHeight { get; set; }
        public int WrapMode { get; set; } // 0=InLine, 1=Square, 2=Tight, 3=Through, 4=TopBottom, 5=Behind, 6=InFront
        public bool Visible { get; set; }
        public string CustomName { get; set; }
        public bool FixPosition { get; set; }
        public bool IsSelected { get; set; }
        public bool ShowLayoutOptions { get; set; }
        public bool ShowElementsMenu { get; set; }
        public bool ShowStylesMenu { get; set; }
        public bool ShowFilterMenu { get; set; }
        public int ColorPaletteIndex { get; set; }
        public int StyleThemeIndex { get; set; }
        public bool ShowTitle { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowDataLabels { get; set; }
        public bool ShowGridlines { get; set; }
        public Rectangle LastBounds { get; set; }

        public DocChartObject()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Data = DocChartData.CreateDefault("Column_Clustered");
            BaseWidth = 460;
            BaseHeight = 250;
            WrapMode = 0; // In-line default matching Word
            Visible = true;
            CustomName = "";
            FixPosition = false;
            PageNumber = 0;
            PageX = 65;
            PageY = 150;
            ShowTitle = true;
            ShowLegend = true;
            ShowDataLabels = false;
            ShowGridlines = true;
            ColorPaletteIndex = 0;
            StyleThemeIndex = 0;
            LastBounds = Rectangle.Empty;
        }
    }

    public class WriterView
    {
        public PieceTable PieceTable { get; set; }
        public DocumentAST DocAST { get; set; }

        public int CursorPosition { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionEnd { get; set; }
        public bool IsSelecting { get; set; }
        public bool CursorVisible { get; set; }
        public int ViewMode { get; set; } // 0 = Chế độ đọc (Read), 1 = Bố cục in (Print), 2 = Bố cục Web (Web)
        public int ReadModePageIndex { get; set; }
        public int CalculatedPageCount = 1;
        public bool ShowFormattingMarks { get; set; }

        public HeaderFooterPreset CurrentHeaderPreset = HeaderFooterPreset.None;
        public HeaderFooterPreset CurrentFooterPreset = HeaderFooterPreset.None;
        public bool IsEditingHeaderFooter = false;
        public bool IsEditingHeaderMode = true; // true = Header, false = Footer
        public string HeaderTitleText = "TIÊU ĐỀ ĐẦU TRANG";
        public bool IsHeaderTextSelected = true;

        public int HeaderBannerXOffset = 0;
        public int HeaderBannerYOffset = 0;
        public int HeaderBannerWidthDelta = 0;
        public int HeaderBannerHeightDelta = 0;
        public bool IsDraggingHeaderBanner = false;
        public bool IsResizingHeaderBanner = false;
        public int HeaderResizeHandleIndex = -1;
        public Point HeaderDragStartMouse;
        public bool ShowHeaderLayoutOptions = false;
        public int HeaderWrapMode = 4; // 0=InLine, 1=Square, 2=Tight, 3=Through, 4=TopBottom, 5=BehindText, 6=InFront
        public bool HeaderFixPosition = true;
        public int ShapeWrapMode = 1; // 0=InLine, 1=Square, 2=Tight, 3=Through, 4=TopBottom, 5=BehindText, 6=InFront
        public bool ShapeFixPosition = true;

        public string ThreeColHeaderLeft = "[Nhập tại đây]";
        public string ThreeColHeaderCenter = "[Nhập tại đây]";
        public string ThreeColHeaderRight = "[Nhập tại đây]";
        public int ActiveThreeColIndex = 0; // 0 = Left, 1 = Center, 2 = Right

        // Chart Subsystem
        public List<DocChartObject> DocumentCharts = new List<DocChartObject>();
        public DocChartObject ActiveChart = null;
        public ChartDataExcelForm ActiveExcelForm = null;
        public bool IsChartSelected { get { return ActiveChart != null; } }
        public bool IsDraggingChart = false;
        public int DragChartHandleIndex = -1;
        public Point ChartDragStartMouse;
        public Point ChartDragStartOffset;
        public Point ChartDragStartCursorOffset;
        public int ChartDragStartScrollY;
        public int ChartDragStartPageNumber;
        public Size ChartDragStartSize;
        public Rectangle ActiveChartLastBounds;
        public Rectangle LayoutBtnChartRect;
        public Rectangle PlusBtnChartRect;
        public Rectangle StyleBtnChartRect;
        public Rectangle FilterBtnChartRect;

        public List<FormattedRun> FormattedRuns { get; set; }
        public List<string> Bookmarks = new List<string>();
        public List<string> Footnotes = new List<string>();
        public List<FootnoteItem> DetailedFootnotes = new List<FootnoteItem>();
        public List<CitationSource> CitationSources = new List<CitationSource>();
        public string CurrentCitationStyle = "APA";
        public List<CaptionItem> CaptionsList = new List<CaptionItem>();
        public List<IndexEntry> IndexEntries = new List<IndexEntry>();
        public List<DocumentComment> Comments { get; set; }
        public int HoveredCommentId = -1;
        public int ActiveCommentId = -1;
        public int EditingReplyCommentId = -1;
        public string ReplyDraftText = "";
        public Dictionary<int, Rectangle> CommentBadgeHitRects = new Dictionary<int, Rectangle>();
        public Dictionary<int, Rectangle> CommentReplyBtnRects = new Dictionary<int, Rectangle>();
        public Dictionary<int, Rectangle> CommentResolveBtnRects = new Dictionary<int, Rectangle>();
        public Dictionary<int, Rectangle> CommentAnchorTextRects = new Dictionary<int, Rectangle>();
        public bool IsEditingEquation = false;
        private Point _mouseDownPoint = Point.Empty;
        private int _mouseDownCharIndex = 0;
        private bool _hasDraggedSelection = false;

        // Default typing format for newly typed characters when no text is selected
        public string TypingFont { get; set; }
        public float TypingFontSize { get; set; }
        public bool TypingBold { get; set; }
        public bool TypingItalic { get; set; }
        public bool TypingUnderline { get; set; }
        public bool TypingStrike { get; set; }
        public Color TypingColor { get; set; }
        public Color TypingHighlight { get; set; }

        public bool IsSelectObjectsMode { get; set; }

        // Format Painter Engine
        public bool IsFormatPainterActive { get; set; }
        public bool IsFormatPainterPersistent { get; set; }
        public TextStyleBuffer CopiedTextStyle { get; set; }
        public ShapeStyleBuffer CopiedShapeStyle { get; set; }

        public float SmoothScrollY = 0f;
        public float TargetScrollY = 0f;

        public int ScrollY
        {
            get { return (int)Math.Round(SmoothScrollY); }
            set { SmoothScrollY = value; TargetScrollY = value; }
        }

        public bool UpdateSmoothScroll()
        {
            if (Math.Abs(TargetScrollY - SmoothScrollY) > 0.5f)
            {
                SmoothScrollY += (TargetScrollY - SmoothScrollY) * 0.55f;
                return true;
            }
            else if (SmoothScrollY != TargetScrollY)
            {
                SmoothScrollY = TargetScrollY;
                return true;
            }
            return false;
        }

        public int TotalPageCount { get; set; }
        public int CurrentActivePage { get; set; }

        public float ZoomLevel { get; set; }
        public Dictionary<int, int> ParagraphAlignments = new Dictionary<int, int>();
        public Dictionary<int, Color> ParagraphBottomBorders = new Dictionary<int, Color>();
        public Dictionary<int, ParagraphBorderInfo> ParagraphBorders = new Dictionary<int, ParagraphBorderInfo>();
        public Dictionary<int, float> ParagraphLineSpacings = new Dictionary<int, float>();
        public Dictionary<int, float> ParagraphSpacingsBefore = new Dictionary<int, float>();
        public Dictionary<int, float> ParagraphSpacingsAfter = new Dictionary<int, float>();

        // MS Word Interactive Ruler & Indentation System
        public RulerUnit CurrentRulerUnit = RulerUnit.Centimeters;
        public TabStopType ActiveTabStopSelector = TabStopType.Left;
        public Dictionary<int, float> ParagraphFirstLineIndents = new Dictionary<int, float>(); // in pt (▼ First Line Indent)
        public Dictionary<int, float> ParagraphHangingIndents = new Dictionary<int, float>();   // in pt (▲ Hanging Indent)
        public Dictionary<int, float> ParagraphLeftIndents = new Dictionary<int, float>();      // in pt (■ Left Indent)
        public Dictionary<int, float> ParagraphRightIndents = new Dictionary<int, float>();     // in pt (▲ Right Indent)
        public Dictionary<int, List<TabStopInfo>> ParagraphTabStops = new Dictionary<int, List<TabStopInfo>>();

        // Ruler Hit Rectangles and Drag State
        public RulerDragHandle ActiveRulerDrag = RulerDragHandle.None;
        public int DraggedTabStopIndex = -1;
        public int ActiveRulerDragParaStart = 0;
        public Point RulerDragStartPoint = Point.Empty;
        public float RulerDragInitialValue = 0f;
        public float RulerDragInitialFirstLine = 0f;
        public float RulerDragInitialHanging = 0f;
        public int RulerGuideLineX = -1;
        public Rectangle RulerTabSelectorRect = Rectangle.Empty;
        public Rectangle FirstLineIndentHitRect = Rectangle.Empty;
        public Rectangle HangingIndentHitRect = Rectangle.Empty;
        public Rectangle LeftIndentHitRect = Rectangle.Empty;
        public Rectangle RightIndentHitRect = Rectangle.Empty;
        public List<Rectangle> TabStopHitRects = new List<Rectangle>();

        public float GetFirstLineIndent(int pStart)
        {
            return ParagraphFirstLineIndents.ContainsKey(pStart) ? ParagraphFirstLineIndents[pStart] : 0f;
        }

        public float GetHangingIndent(int pStart)
        {
            return ParagraphHangingIndents.ContainsKey(pStart) ? ParagraphHangingIndents[pStart] : 0f;
        }

        public float GetLeftIndent(int pStart)
        {
            return ParagraphLeftIndents.ContainsKey(pStart) ? ParagraphLeftIndents[pStart] : 0f;
        }

        public float GetRightIndent(int pStart)
        {
            return ParagraphRightIndents.ContainsKey(pStart) ? ParagraphRightIndents[pStart] : 0f;
        }

        public List<TabStopInfo> GetTabStops(int pStart)
        {
            if (!ParagraphTabStops.ContainsKey(pStart))
            {
                ParagraphTabStops[pStart] = new List<TabStopInfo>();
            }
            return ParagraphTabStops[pStart];
        }

        public int GetCurrentParagraphStart()
        {
            var pStarts = GetSelectedOrCurrentParagraphStarts();
            return (pStarts != null && pStarts.Count > 0) ? pStarts[0] : 0;
        }

        // MS Word Navigation Pane (Thanh Tìm Kiếm & Điều Hướng)
        public bool ShowNavigationPane = false;
        public int NavActiveTab = 2; // 0 = Headings (Tiêu đề), 1 = Pages (Trang), 2 = Results (Kết quả)
        public string NavSearchQuery = "";
        public bool NavSearchFocused = false;
        public int NavSearchResultIndex = -1;
        public int NavScrollY = 0;
        public Rectangle NavSearchBoxRect = Rectangle.Empty;
        public Rectangle NavCloseBtnRect = Rectangle.Empty;
        public Rectangle NavTabHeadingsRect = Rectangle.Empty;
        public Rectangle NavTabPagesRect = Rectangle.Empty;
        public Rectangle NavTabResultsRect = Rectangle.Empty;
        public Rectangle NavNextMatchBtnRect = Rectangle.Empty;
        public Rectangle NavPrevMatchBtnRect = Rectangle.Empty;
        public Rectangle NavClearQueryBtnRect = Rectangle.Empty;

        // MS Word Selection Pane (Ngăn Lựa Chọn Đối Tượng)
        public bool ShowSelectionPane = false;
        public int SelectionPaneScrollY = 0;
        public Rectangle SelectionPaneCloseBtnRect = Rectangle.Empty;
        public Rectangle SelectionPaneShowAllBtnRect = Rectangle.Empty;
        public Rectangle SelectionPaneHideAllBtnRect = Rectangle.Empty;
        public Rectangle SelectionPaneMoveUpBtnRect = Rectangle.Empty;
        public Rectangle SelectionPaneMoveDownBtnRect = Rectangle.Empty;
        public int SelectionPaneHoverIndex = -1;

        public struct SelectionPaneItemBounds
        {
            public object TargetObject;
            public Rectangle RowRect;
            public Rectangle EyeRect;
        }
        public List<SelectionPaneItemBounds> SelectionPaneItems = new List<SelectionPaneItemBounds>();

        public struct NavSearchResultItem
        {
            public int CharIndex;
            public int Length;
            public int PageNumber;
            public string SnippetBefore;
            public string MatchText;
            public string SnippetAfter;
            public Rectangle ClickRect;
        }
        public List<NavSearchResultItem> NavSearchResults = new List<NavSearchResultItem>();

        public struct NavHeadingItem
        {
            public int CharIndex;
            public int Level;
            public string Title;
            public int PageNumber;
            public Rectangle ClickRect;
        }
        public List<NavHeadingItem> NavHeadings = new List<NavHeadingItem>();

        public struct NavPageThumbnailItem
        {
            public int PageNumber;
            public Rectangle ClickRect;
        }
        public List<NavPageThumbnailItem> NavPageThumbnails = new List<NavPageThumbnailItem>();

        // Interactive Table Drag & Drop - MS Word style snap-to-gap
        public int DragTableIndex = -1;
        public bool IsDraggingMove = false;
        public bool IsDraggingResize = false;
        public Point DragStartMouse;
        public Dictionary<int, int> TablePageX = new Dictionary<int, int>();   // Horizontal position relative to pageX
        public Dictionary<int, int> TablePageY = new Dictionary<int, int>();   // Vertical position relative to startPageY (for Wrap Around tables)
        public Dictionary<int, int> TableWrapModes = new Dictionary<int, int>(); // 0=In-Line, 1=Square/Around (Bao quanh văn bản)
        public Dictionary<int, Rectangle> TableFloatingBounds = new Dictionary<int, Rectangle>();
        public Dictionary<int, Size> TableCustomSizes = new Dictionary<int, Size>();
        public List<Rectangle> MoveHandleRects = new List<Rectangle>();
        public List<Rectangle> ResizeHandleRects = new List<Rectangle>();

        // Snap-to-gap drag system (MS Word style)
        public struct ParagraphGap
        {
            public int ScreenY;             // Y position on screen of this gap
            public int CharIndex;           // Char index of insertion point (start of next paragraph)
            public int VirtualEntersNeeded; // Number of \n's to append if dropped at this virtual gap
        }
        public List<ParagraphGap> DocParagraphGaps = new List<ParagraphGap>();
        // TableCharRanges[tblIdx] = { charStart, charLen }
        public Dictionary<int, int[]> TableCharRanges = new Dictionary<int, int[]>();
        public int DragPreviewGapIndex = -1;   // Which gap index to show dashed preview box at
        public int DragPreviewTableW = 0;      // Width of table being dragged (for preview box)
        public int DragPreviewTableH = 0;      // Height of table being dragged (for preview box)
        public int DragPreviewX = -1;          // X position of dashed preview box (moves along X)
        public int DragStartMouseRelativeX = 0; // Mouse X offset relative to tableX when move drag started
        public int DragStartMouseRelativeY = 0; // Mouse Y offset relative to tableY when move drag started
        public Point TableDragStartMouse = Point.Empty;
        public Point TableDragStartOffset = Point.Empty;
        public int CurrentRenderTextX = 0;     // Exact document textX recorded during Draw()
        public Point HoveredMousePoint = new Point(-999, -999); // Mouse hover position for dynamic + button rendering
        // Per-table custom column widths & row heights (individual cell sizing)
        public Dictionary<int, List<int>> TableColWidths = new Dictionary<int, List<int>>();
        public Dictionary<int, List<int>> TableRowHeights = new Dictionary<int, List<int>>();

        // Inner Gridline Dragging
        public bool IsDraggingColGridline = false;
        public bool IsDraggingRowGridline = false;
        public int DragGridlineTableIndex = -1;
        public int DragGridlineColIndex = -1;
        public int DragGridlineRowIndex = -1;
        public Point DragGridlineInitialMouse;
        public int DragGridlineInitialWidth1;
        public int DragGridlineInitialWidth2;
        public int DragGridlineInitialHeight;

        // Smooth Table Corner Resizing
        public Point DragResizeInitialMouse;
        public int DragResizeInitialW;
        public int DragResizeInitialH;
        public List<int> DragResizeInitialColWidths = new List<int>();
        public List<int> DragResizeInitialRowHeights = new List<int>();

        // MS Word '+' Insert Button Hits
        public struct InsertButtonHit
        {
            public Rectangle Rect;
            public int TableIndex;
            public bool IsRowInsert; // true for inserting row, false for inserting column
            public int InsertIndex;  // index to insert after
        }
        public List<InsertButtonHit> InsertButtonHits = new List<InsertButtonHit>();

        // Table Wrap Mode & Layout Options State
        public struct TableWrapBtnHit
        {
            public Rectangle Rect;
            public int TableIndex;
        }
        public List<TableWrapBtnHit> TableWrapBtnHits = new List<TableWrapBtnHit>();
        public int ActiveTableLayoutOptionsIndex = -1; // Chỉ số bảng đang mở menu Tùy chọn bố trí
        public Dictionary<int, bool> TableFixPositions = new Dictionary<int, bool>(); // Cố định vị trí trên trang

        private Stack<WriterUndoSnapshot> undoStack = new Stack<WriterUndoSnapshot>();
        private Stack<WriterUndoSnapshot> redoStack = new Stack<WriterUndoSnapshot>();

        // Cached rendered line info for up/down arrow key navigation
        public struct RenderedLineInfo
        {
            public int StartCharIndex;
            public int Length;
            public int ScreenY;
            public int ScreenH;
            public int ScreenX; // left edge of text in this line
        }
        public List<RenderedLineInfo> RenderedLines = new List<RenderedLineInfo>();
        public int LastCursorScreenX = -1; // remembered X for up/down navigation

        public WriterView()
        {
            PieceTable = new PieceTable();
            DocAST = new DocumentAST();
            FormattedRuns = new List<FormattedRun>();
            Comments = new List<DocumentComment>();

            CursorPosition = 0;
            SelectionStart = 0;
            SelectionEnd = 0;
            IsSelecting = false;
            CursorVisible = true;
            ScrollY = 0;
            ZoomLevel = 1.0f;
            ViewMode = 1; // 1 = Bố cục in (Print Layout) MẶC ĐỊNH HÀNG ĐẦU KHI MỞ
            ReadModePageIndex = 0;

            // Default font set to Times New Roman
            TypingFont = "Times New Roman";
            TypingFontSize = 11.0f;
            TypingBold = false;
            TypingItalic = false;
            TypingUnderline = false;
            TypingStrike = false;
            TypingColor = Color.Black;
            TypingHighlight = Color.Transparent;

            PieceTable.SetInitialText("");
            CursorPosition = 0;
            FormattedRuns.Clear();
        }

        public void UpdateRibbonState(RibbonBar ribbon)
        {
            if (ribbon == null) return;

            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);

            if (selMin != selMax && selMin >= 0 && selMax <= PieceTable.Length)
            {
                bool allBold = true, allItalic = true, allUnderline = true, allStrike = true;
                string commonFont = null;
                float commonSize = -1;
                bool mixedFont = false;
                bool mixedSize = false;
                Color firstColor = Color.Black;
                Color firstHL = Color.Transparent;
                bool firstSet = false;

                for (int r = 0; r < FormattedRuns.Count; r++)
                {
                    FormattedRun run = FormattedRuns[r];
                    int rEnd = run.StartIndex + run.Length;

                    if (rEnd > selMin && run.StartIndex < selMax)
                    {
                        if (!firstSet)
                        {
                            commonFont = run.FontName;
                            commonSize = run.FontSize;
                            firstColor = run.Color;
                            firstHL = run.Highlight;
                            firstSet = true;
                        }
                        else
                        {
                            if (!string.Equals(commonFont, run.FontName, StringComparison.OrdinalIgnoreCase)) mixedFont = true;
                            if (Math.Abs(commonSize - run.FontSize) > 0.01f) mixedSize = true;
                        }

                        if (!run.Bold) allBold = false;
                        if (!run.Italic) allItalic = false;
                        if (!run.Underline) allUnderline = false;
                        if (!run.Strike) allStrike = false;
                    }
                }

                ribbon.SelectedFont = mixedFont ? "" : (commonFont ?? TypingFont);
                ribbon.FontSize = mixedSize ? 0f : ((commonSize > 0) ? commonSize : TypingFontSize);
                ribbon.IsBold = allBold;
                ribbon.IsItalic = allItalic;
                ribbon.IsUnderline = allUnderline;
                ribbon.IsStrikethrough = allStrike;
                ribbon.CurrentFontColor = firstColor;
                ribbon.CurrentHighlightColor = firstHL;

                // Sync paragraph alignment
                var pStarts = GetSelectedOrCurrentParagraphStarts();
                int commonAlign = -1;
                bool mixedAlign = false;
                bool firstP = true;
                foreach (int pStart in pStarts)
                {
                    int pAlign = ParagraphAlignments.ContainsKey(pStart) ? ParagraphAlignments[pStart] : 0;
                    if (firstP) { commonAlign = pAlign; firstP = false; }
                    else if (commonAlign != pAlign) { mixedAlign = true; }
                }
                ribbon.Alignment = mixedAlign ? -1 : commonAlign;
            }
            else
            {
                int pos = Math.Max(0, CursorPosition > 0 ? CursorPosition - 1 : 0);
                FormattedRun fmt = GetFormatAt(pos);
                ribbon.SelectedFont = fmt.FontName;
                ribbon.FontSize = fmt.FontSize;
                ribbon.IsBold = fmt.Bold;
                ribbon.IsItalic = fmt.Italic;
                ribbon.IsUnderline = fmt.Underline;
                ribbon.IsStrikethrough = fmt.Strike;
                ribbon.IsSubscript = fmt.Subscript;
                ribbon.IsSuperscript = fmt.Superscript;
                ribbon.CurrentFontColor = fmt.Color;
                var pStartsSingle = GetSelectedOrCurrentParagraphStarts();
                int curP = pStartsSingle.Count > 0 ? pStartsSingle[0] : 0;
                ribbon.Alignment = ParagraphAlignments.ContainsKey(curP) ? ParagraphAlignments[curP] : 0;
            }
        }

        public void ApplyFontToSelectionOrTyping(string fontName)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, fontName, -1, null, null, null, null, null, null);
            }
            else
            {
                TypingFont = fontName;
            }
        }

        public void ApplyFontSizeToSelectionOrTyping(float fontSize)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, fontSize, null, null, null, null, null, null);
            }
            else
            {
                TypingFontSize = fontSize;
            }
        }

        public void ApplyBoldToSelectionOrTyping(bool bold)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, bold, null, null, null, null, null);
            }
            else
            {
                TypingBold = bold;
            }
        }

        public void ApplyItalicToSelectionOrTyping(bool italic)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, italic, null, null, null, null);
            }
            else
            {
                TypingItalic = italic;
            }
        }

        public void ApplyUnderlineToSelectionOrTyping(bool underline)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, underline, null, null, null);
            }
            else
            {
                TypingUnderline = underline;
            }
        }

        public void ApplyStrikeToSelectionOrTyping(bool strike)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, null, strike, null, null);
            }
            else
            {
                TypingStrike = strike;
            }
        }

        public void ApplyFontColorToSelectionOrTyping(Color col)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, null, null, col, null);
            }
            else
            {
                TypingColor = col;
            }
        }

        public void ApplyHighlightToSelectionOrTyping(Color hl)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, null, null, null, hl);
            }
            else
            {
                TypingHighlight = hl;
            }
        }
        public void ApplySubscriptToSelectionOrTyping(bool sub)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, null, null, null, null, sub, false);
            }
            else
            {
                TypingSubscript = sub;
                if (sub) TypingSuperscript = false;
            }
        }

        public void ApplySuperscriptToSelectionOrTyping(bool sup)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, null, null, null, null, false, sup);
            }
            else
            {
                TypingSuperscript = sup;
                if (sup) TypingSubscript = false;
            }
        }

        public bool TypingSubscript { get; set; }
        public bool TypingSuperscript { get; set; }

        public void AdjustFormattedRunsOnDelete(int delStart, int delLen)
        {
            if (delLen <= 0 || FormattedRuns.Count == 0) return;
            int delEnd = delStart + delLen;

            List<FormattedRun> newRuns = new List<FormattedRun>();
            foreach (var r in FormattedRuns)
            {
                int rStart = r.StartIndex;
                int rLen = r.Length;
                int rEnd = rStart + rLen;

                if (rEnd <= delStart)
                {
                    newRuns.Add(r);
                }
                else if (rStart >= delEnd)
                {
                    newRuns.Add(new FormattedRun(rStart - delLen, rLen, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                }
                else
                {
                    int keepBefore = Math.Max(0, delStart - rStart);
                    int keepAfter = Math.Max(0, rEnd - delEnd);
                    int newLen = keepBefore + keepAfter;

                    if (newLen > 0)
                    {
                        int newStart = (rStart < delStart) ? rStart : delStart;
                        newRuns.Add(new FormattedRun(newStart, newLen, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                    }
                }
            }
            FormattedRuns = newRuns;

            if (ParagraphAlignments != null && ParagraphAlignments.Count > 0)
            {
                Dictionary<int, int> newAligns = new Dictionary<int, int>();
                foreach (var kvp in ParagraphAlignments)
                {
                    if (kvp.Key >= delStart && kvp.Key < delEnd) continue;
                    else if (kvp.Key >= delEnd) newAligns[kvp.Key - delLen] = kvp.Value;
                    else newAligns[kvp.Key] = kvp.Value;
                }
                ParagraphAlignments = newAligns;
            }

            if (ParagraphBottomBorders != null && ParagraphBottomBorders.Count > 0)
            {
                Dictionary<int, Color> newBorders = new Dictionary<int, Color>();
                foreach (var kvp in ParagraphBottomBorders)
                {
                    if (kvp.Key >= delStart && kvp.Key < delEnd) continue; // Paragraph deleted -> border deleted!
                    else if (kvp.Key >= delEnd) newBorders[kvp.Key - delLen] = kvp.Value;
                    else newBorders[kvp.Key] = kvp.Value;
                }
                ParagraphBottomBorders = newBorders;
            }

            if (ParagraphBorders != null && ParagraphBorders.Count > 0)
            {
                Dictionary<int, ParagraphBorderInfo> newBorders = new Dictionary<int, ParagraphBorderInfo>();
                foreach (var kvp in ParagraphBorders)
                {
                    if (kvp.Key >= delStart && kvp.Key < delEnd) continue;
                    else if (kvp.Key >= delEnd) newBorders[kvp.Key - delLen] = kvp.Value;
                    else newBorders[kvp.Key] = kvp.Value;
                }
                ParagraphBorders = newBorders;
            }

            if (ParagraphLineSpacings != null && ParagraphLineSpacings.Count > 0)
            {
                Dictionary<int, float> newSpacings = new Dictionary<int, float>();
                foreach (var kvp in ParagraphLineSpacings)
                {
                    if (kvp.Key >= delStart && kvp.Key < delEnd) continue;
                    else if (kvp.Key >= delEnd) newSpacings[kvp.Key - delLen] = kvp.Value;
                    else newSpacings[kvp.Key] = kvp.Value;
                }
                ParagraphLineSpacings = newSpacings;
            }

            if (ParagraphSpacingsBefore != null && ParagraphSpacingsBefore.Count > 0)
            {
                Dictionary<int, float> newBefores = new Dictionary<int, float>();
                foreach (var kvp in ParagraphSpacingsBefore)
                {
                    if (kvp.Key >= delStart && kvp.Key < delEnd) continue;
                    else if (kvp.Key >= delEnd) newBefores[kvp.Key - delLen] = kvp.Value;
                    else newBefores[kvp.Key] = kvp.Value;
                }
                ParagraphSpacingsBefore = newBefores;
            }

            if (ParagraphSpacingsAfter != null && ParagraphSpacingsAfter.Count > 0)
            {
                Dictionary<int, float> newAfters = new Dictionary<int, float>();
                foreach (var kvp in ParagraphSpacingsAfter)
                {
                    if (kvp.Key >= delStart && kvp.Key < delEnd) continue;
                    else if (kvp.Key >= delEnd) newAfters[kvp.Key - delLen] = kvp.Value;
                    else newAfters[kvp.Key] = kvp.Value;
                }
                ParagraphSpacingsAfter = newAfters;
            }
        }

        public void AdjustFormattedRunsOnInsert(int insStart, int insLen, FormattedRun inheritFrom = null)
        {
            if (insLen <= 0) return;

            if (ParagraphAlignments != null && ParagraphAlignments.Count > 0)
            {
                Dictionary<int, int> newAligns = new Dictionary<int, int>();
                foreach (var kvp in ParagraphAlignments)
                {
                    if (kvp.Key >= insStart) newAligns[kvp.Key + insLen] = kvp.Value;
                    else newAligns[kvp.Key] = kvp.Value;
                }
                ParagraphAlignments = newAligns;
            }

            if (ParagraphBottomBorders != null && ParagraphBottomBorders.Count > 0)
            {
                Dictionary<int, Color> newBorders = new Dictionary<int, Color>();
                foreach (var kvp in ParagraphBottomBorders)
                {
                    if (kvp.Key >= insStart) newBorders[kvp.Key + insLen] = kvp.Value;
                    else newBorders[kvp.Key] = kvp.Value;
                }
                ParagraphBottomBorders = newBorders;
            }

            if (ParagraphBorders != null && ParagraphBorders.Count > 0)
            {
                Dictionary<int, ParagraphBorderInfo> newBorders = new Dictionary<int, ParagraphBorderInfo>();
                foreach (var kvp in ParagraphBorders)
                {
                    if (kvp.Key >= insStart) newBorders[kvp.Key + insLen] = kvp.Value;
                    else newBorders[kvp.Key] = kvp.Value;
                }
                ParagraphBorders = newBorders;
            }

            if (ParagraphLineSpacings != null && ParagraphLineSpacings.Count > 0)
            {
                Dictionary<int, float> newSpacings = new Dictionary<int, float>();
                foreach (var kvp in ParagraphLineSpacings)
                {
                    if (kvp.Key >= insStart) newSpacings[kvp.Key + insLen] = kvp.Value;
                    else newSpacings[kvp.Key] = kvp.Value;
                }
                ParagraphLineSpacings = newSpacings;
            }

            if (ParagraphSpacingsBefore != null && ParagraphSpacingsBefore.Count > 0)
            {
                Dictionary<int, float> newBefores = new Dictionary<int, float>();
                foreach (var kvp in ParagraphSpacingsBefore)
                {
                    if (kvp.Key >= insStart) newBefores[kvp.Key + insLen] = kvp.Value;
                    else newBefores[kvp.Key] = kvp.Value;
                }
                ParagraphSpacingsBefore = newBefores;
            }

            if (ParagraphSpacingsAfter != null && ParagraphSpacingsAfter.Count > 0)
            {
                Dictionary<int, float> newAfters = new Dictionary<int, float>();
                foreach (var kvp in ParagraphSpacingsAfter)
                {
                    if (kvp.Key >= insStart) newAfters[kvp.Key + insLen] = kvp.Value;
                    else newAfters[kvp.Key] = kvp.Value;
                }
                ParagraphSpacingsAfter = newAfters;
            }

            bool expandedExisting = false;
            List<FormattedRun> newRuns = new List<FormattedRun>();

            foreach (var r in FormattedRuns)
            {
                int rEnd = r.StartIndex + r.Length;
                if (insStart > r.StartIndex && insStart <= rEnd)
                {
                    newRuns.Add(new FormattedRun(r.StartIndex, r.Length + insLen, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                    expandedExisting = true;
                }
                else if (r.StartIndex >= insStart)
                {
                    newRuns.Add(new FormattedRun(r.StartIndex + insLen, r.Length, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                }
                else
                {
                    newRuns.Add(r);
                }
            }

            if (!expandedExisting)
            {
                FormattedRun fmt = inheritFrom ?? GetFormatAt(Math.Max(0, insStart - 1));
                newRuns.Add(new FormattedRun(insStart, insLen, fmt.FontName, fmt.FontSize, fmt.Bold, fmt.Italic, fmt.Underline, fmt.Strike, fmt.Color, fmt.Highlight, fmt.Subscript, fmt.Superscript));
            }

            FormattedRuns = newRuns;
        }

        public void ApplyFormatToSelection(string font, float size, bool? bold, bool? italic, bool? underline, bool? strike, Color? col, Color? hl, bool? sub = null, bool? sup = null)
        {
            int min = Math.Min(SelectionStart, SelectionEnd);
            int len = Math.Abs(SelectionEnd - SelectionStart);
            if (len > 0 && min >= 0)
            {
                ApplyFormatToRange(min, len, font, size, bold, italic, underline, strike, col, hl, sub, sup);
            }
        }

        public void ApplyFormatToRange(int start, int len, string font, float size, bool? bold, bool? italic, bool? underline, bool? strike, Color? col, Color? hl, bool? sub = null, bool? sup = null)
        {
            if (len <= 0) return;
            int end = start + len;

            FormattedRun curFormat = GetFormatAt(start);
            FormattedRun newRun = new FormattedRun(
                start, len,
                font ?? curFormat.FontName,
                size > 0 ? size : curFormat.FontSize,
                bold.HasValue ? bold.Value : curFormat.Bold,
                italic.HasValue ? italic.Value : curFormat.Italic,
                underline.HasValue ? underline.Value : curFormat.Underline,
                strike.HasValue ? strike.Value : curFormat.Strike,
                col.HasValue ? col.Value : curFormat.Color,
                hl.HasValue ? hl.Value : curFormat.Highlight,
                sub.HasValue ? sub.Value : curFormat.Subscript,
                sup.HasValue ? sup.Value : curFormat.Superscript
            );

            List<FormattedRun> updatedRuns = new List<FormattedRun>();
            foreach (var r in FormattedRuns)
            {
                int rEnd = r.StartIndex + r.Length;
                if (rEnd <= start || r.StartIndex >= end)
                {
                    updatedRuns.Add(r);
                }
                else
                {
                    if (r.StartIndex < start)
                    {
                        updatedRuns.Add(new FormattedRun(r.StartIndex, start - r.StartIndex, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                    }
                    if (rEnd > end)
                    {
                        updatedRuns.Add(new FormattedRun(end, rEnd - end, r.FontName, r.FontSize, r.Bold, r.Italic, r.Underline, r.Strike, r.Color, r.Highlight, r.Subscript, r.Superscript));
                    }
                }
            }
            updatedRuns.Add(newRun);
            FormattedRuns = updatedRuns;
        }

        public void CopyFormatting()
        {
            if (ActiveShape != null)
            {
                CopiedShapeStyle = new ShapeStyleBuffer
                {
                    FillColor = ActiveShape.FillColor,
                    OutlineColor = ActiveShape.OutlineColor,
                    OutlineWidth = ActiveShape.OutlineWidth,
                    PictureTransparency = ActiveShape.PictureTransparency,
                    WrapMode = ActiveShape.WrapMode,
                    PictureStylePreset = ActiveShape.PictureStylePreset,
                    RotationAngle = ActiveShape.RotationAngle,
                    PictureFlipHorizontal = ActiveShape.PictureFlipHorizontal,
                    PictureFlipVertical = ActiveShape.PictureFlipVertical
                };
                CopiedTextStyle = null;
            }
            else
            {
                int targetChar = SelectionStart != SelectionEnd ? Math.Min(SelectionStart, SelectionEnd) : CursorPosition;
                FormattedRun fmt = GetFormatAt(targetChar);
                CopiedTextStyle = new TextStyleBuffer
                {
                    FontName = fmt.FontName,
                    FontSize = fmt.FontSize,
                    Bold = fmt.Bold,
                    Italic = fmt.Italic,
                    Underline = fmt.Underline,
                    Strike = fmt.Strike,
                    Color = fmt.Color,
                    Highlight = fmt.Highlight,
                    Subscript = fmt.Subscript,
                    Superscript = fmt.Superscript
                };
                CopiedShapeStyle = null;
            }
            IsFormatPainterActive = true;
        }

        public void PasteFormatting()
        {
            if (CopiedShapeStyle != null && ActiveShape != null)
            {
                SaveUndoState();
                ActiveShape.FillColor = CopiedShapeStyle.FillColor;
                ActiveShape.OutlineColor = CopiedShapeStyle.OutlineColor;
                ActiveShape.OutlineWidth = CopiedShapeStyle.OutlineWidth;
                ActiveShape.PictureTransparency = CopiedShapeStyle.PictureTransparency;
                ActiveShape.WrapMode = CopiedShapeStyle.WrapMode;
                ActiveShape.PictureStylePreset = CopiedShapeStyle.PictureStylePreset;
                ActiveShape.RotationAngle = CopiedShapeStyle.RotationAngle;
                ActiveShape.PictureFlipHorizontal = CopiedShapeStyle.PictureFlipHorizontal;
                ActiveShape.PictureFlipVertical = CopiedShapeStyle.PictureFlipVertical;
                SyncShapeTagInDocument(ActiveShape);
            }
            else if (CopiedTextStyle != null)
            {
                SaveUndoState();
                int start = Math.Min(SelectionStart, SelectionEnd);
                int end = Math.Max(SelectionStart, SelectionEnd);
                int len = end - start;
                if (len > 0)
                {
                    ApplyFormatToRange(start, len,
                        CopiedTextStyle.FontName,
                        CopiedTextStyle.FontSize,
                        CopiedTextStyle.Bold,
                        CopiedTextStyle.Italic,
                        CopiedTextStyle.Underline,
                        CopiedTextStyle.Strike,
                        CopiedTextStyle.Color,
                        CopiedTextStyle.Highlight,
                        CopiedTextStyle.Subscript,
                        CopiedTextStyle.Superscript);
                }
                else
                {
                    TypingFont = CopiedTextStyle.FontName;
                    TypingFontSize = CopiedTextStyle.FontSize;
                    TypingBold = CopiedTextStyle.Bold;
                    TypingItalic = CopiedTextStyle.Italic;
                    TypingUnderline = CopiedTextStyle.Underline;
                    TypingStrike = CopiedTextStyle.Strike;
                    TypingColor = CopiedTextStyle.Color;
                    TypingHighlight = CopiedTextStyle.Highlight;
                }
            }
            if (!IsFormatPainterPersistent)
            {
                IsFormatPainterActive = false;
            }
        }

        public void ToggleFormatPainter(bool persistent = false)
        {
            if (IsFormatPainterActive)
            {
                IsFormatPainterActive = false;
                IsFormatPainterPersistent = false;
            }
            else
            {
                CopyFormatting();
                IsFormatPainterPersistent = persistent;
            }
        }

        public void CancelFormatPainter()
        {
            IsFormatPainterActive = false;
            IsFormatPainterPersistent = false;
        }

        public FormattedRun GetFormatAt(int charIndex)
        {
            for (int r = FormattedRuns.Count - 1; r >= 0; r--)
            {
                var run = FormattedRuns[r];
                if (charIndex >= run.StartIndex && charIndex < run.StartIndex + run.Length)
                {
                    return run;
                }
            }
            return new FormattedRun(charIndex, 1, "Times New Roman", 11.0f, false, false, false, false, Color.Black, Color.Transparent);
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
            undoStack.Push(new WriterUndoSnapshot(
                PieceTable.GetText(),
                FormattedRuns,
                CursorPosition,
                SelectionStart,
                SelectionEnd,
                ParagraphAlignments,
                ParagraphLineSpacings,
                ParagraphSpacingsBefore,
                ParagraphSpacingsAfter,
                ParagraphBorders));
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(new WriterUndoSnapshot(
                    PieceTable.GetText(),
                    FormattedRuns,
                    CursorPosition,
                    SelectionStart,
                    SelectionEnd,
                    ParagraphAlignments,
                    ParagraphLineSpacings,
                    ParagraphSpacingsBefore,
                    ParagraphSpacingsAfter,
                    ParagraphBorders));
                var prev = undoStack.Pop();
                PieceTable.SetInitialText(prev.Text);
                FormattedRuns = prev.FormattedRuns;
                CursorPosition = Math.Min(prev.CursorPosition, PieceTable.Length);
                SelectionStart = Math.Min(prev.SelectionStart, PieceTable.Length);
                SelectionEnd = Math.Min(prev.SelectionEnd, PieceTable.Length);
                ParagraphAlignments = prev.ParagraphAlignments != null ? new Dictionary<int, int>(prev.ParagraphAlignments) : new Dictionary<int, int>();
                ParagraphLineSpacings = prev.ParagraphLineSpacings != null ? new Dictionary<int, float>(prev.ParagraphLineSpacings) : new Dictionary<int, float>();
                ParagraphSpacingsBefore = prev.ParagraphSpacingsBefore != null ? new Dictionary<int, float>(prev.ParagraphSpacingsBefore) : new Dictionary<int, float>();
                ParagraphSpacingsAfter = prev.ParagraphSpacingsAfter != null ? new Dictionary<int, float>(prev.ParagraphSpacingsAfter) : new Dictionary<int, float>();
                ParagraphBorders = prev.ParagraphBorders != null ? new Dictionary<int, ParagraphBorderInfo>(prev.ParagraphBorders) : new Dictionary<int, ParagraphBorderInfo>();
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(new WriterUndoSnapshot(
                    PieceTable.GetText(),
                    FormattedRuns,
                    CursorPosition,
                    SelectionStart,
                    SelectionEnd,
                    ParagraphAlignments,
                    ParagraphLineSpacings,
                    ParagraphSpacingsBefore,
                    ParagraphSpacingsAfter,
                    ParagraphBorders));
                var next = redoStack.Pop();
                PieceTable.SetInitialText(next.Text);
                FormattedRuns = next.FormattedRuns;
                CursorPosition = Math.Min(next.CursorPosition, PieceTable.Length);
                SelectionStart = Math.Min(next.SelectionStart, PieceTable.Length);
                SelectionEnd = Math.Min(next.SelectionEnd, PieceTable.Length);
                ParagraphAlignments = next.ParagraphAlignments != null ? new Dictionary<int, int>(next.ParagraphAlignments) : new Dictionary<int, int>();
                ParagraphLineSpacings = next.ParagraphLineSpacings != null ? new Dictionary<int, float>(next.ParagraphLineSpacings) : new Dictionary<int, float>();
                ParagraphSpacingsBefore = next.ParagraphSpacingsBefore != null ? new Dictionary<int, float>(next.ParagraphSpacingsBefore) : new Dictionary<int, float>();
                ParagraphSpacingsAfter = next.ParagraphSpacingsAfter != null ? new Dictionary<int, float>(next.ParagraphSpacingsAfter) : new Dictionary<int, float>();
                ParagraphBorders = next.ParagraphBorders != null ? new Dictionary<int, ParagraphBorderInfo>(next.ParagraphBorders) : new Dictionary<int, ParagraphBorderInfo>();
            }
        }

        public void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = PieceTable != null ? PieceTable.Length : 0;
            CursorPosition = SelectionEnd;
        }

        public void SelectAllTextWithSimilarFormatting()
        {
            if (PieceTable == null || PieceTable.Length == 0) return;
            string text = PieceTable.GetText();
            if (string.IsNullOrEmpty(text)) return;

            int cur = Math.Min(SelectionStart, SelectionEnd);
            if (cur < 0 || cur >= text.Length) cur = CursorPosition;
            if (cur < 0) cur = 0;

            FormattedRun matchRun = null;
            if (FormattedRuns != null)
            {
                foreach (var r in FormattedRuns)
                {
                    if (cur >= r.StartIndex && cur < r.StartIndex + r.Length)
                    {
                        matchRun = r;
                        break;
                    }
                }
            }

            if (matchRun != null)
            {
                SelectionStart = matchRun.StartIndex;
                SelectionEnd = matchRun.StartIndex + matchRun.Length;
                CursorPosition = SelectionEnd;
            }
            else
            {
                SelectAll();
            }
        }

        public int TablePreviewCols = 0;
        public int TablePreviewRows = 0;

        public void SetTableInsertPreview(int cols, int rows)
        {
            TablePreviewCols = cols;
            TablePreviewRows = rows;
        }

        public void ClearTableInsertPreview()
        {
            TablePreviewCols = 0;
            TablePreviewRows = 0;
        }

        public void InsertTable(int cols, int rows)
        {
            if (cols <= 0) cols = 3;
            if (rows <= 0) rows = 3;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine();

            // Table Header Line with Clean Empty Cells
            sb.Append("|");
            for (int c = 1; c <= cols; c++)
            {
                sb.Append("  |");
            }
            sb.AppendLine();

            // Table Header Divider Line
            sb.Append("|");
            for (int c = 1; c <= cols; c++)
            {
                sb.Append("---|");
            }
            sb.AppendLine();

            // Table Rows with Clean Empty Cells
            for (int r = 1; r <= rows; r++)
            {
                sb.Append("|");
                for (int c = 1; c <= cols; c++)
                {
                    sb.Append("  |");
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            InsertTextWithAutoFormat(sb.ToString());
        }

        public string HeaderText = "";
        public string FooterText = "";
        public bool ShowPageNumbers = false;

        public void ToggleHeader(string text)
        {
            if (string.IsNullOrEmpty(HeaderText)) HeaderText = string.IsNullOrEmpty(text) ? "Tiêu đề đầu trang" : text;
            else HeaderText = "";
        }

        public void GetCursorPageCoordinates(out int pageNum, out int relativePageX, out int relativePageY)
        {
            pageNum = 0;
            relativePageX = 72;
            relativePageY = 72;

            int stdH = 1123;
            int pageHWithGap = (int)(stdH * ZoomLevel) + 30;

            if (RenderedLines.Count > 0)
            {
                RenderedLineInfo bestLine = RenderedLines[0];
                for (int l = 0; l < RenderedLines.Count; l++)
                {
                    var rl = RenderedLines[l];
                    if (CursorPosition >= rl.StartCharIndex && CursorPosition <= rl.StartCharIndex + rl.Length)
                    {
                        bestLine = rl;
                        break;
                    }
                    if (rl.StartCharIndex <= CursorPosition)
                    {
                        bestLine = rl;
                    }
                }

                int absDocY = bestLine.ScreenY + ScrollY - 195;
                if (absDocY >= 0)
                {
                    pageNum = Math.Max(0, absDocY / pageHWithGap);
                    int pLocalY = absDocY % pageHWithGap;
                    relativePageY = Math.Max(72, (int)(pLocalY / Math.Max(0.2f, ZoomLevel)));
                    relativePageX = 72;
                }
            }
        }

        public void DeleteActiveShape()
        {
            if (ActiveShape == null) return;
            SaveUndoState();
            string shapeId = ActiveShape.Id;
            DocumentShapes.Remove(ActiveShape);
            ActiveShape = null;

            string fullText = PieceTable.GetText();
            string tagPrefix = "![SHAPE:ID=" + shapeId;
            int idx = fullText.IndexOf(tagPrefix);
            if (idx >= 0)
            {
                int lineStart = fullText.LastIndexOf('\n', Math.Max(0, idx - 1));
                if (lineStart < 0) lineStart = 0; else lineStart += 1;
                int lineEnd = fullText.IndexOf('\n', idx);
                if (lineEnd < 0) lineEnd = fullText.Length; else lineEnd += 1;

                int len = lineEnd - lineStart;
                if (len > 0)
                {
                    PieceTable.Remove(lineStart, len);
                    AdjustFormattedRunsOnDelete(lineStart, len);
                    CursorPosition = Math.Min(lineStart, PieceTable.Length);
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                }
            }
        }

        public void DeleteActiveChart()
        {
            if (ActiveChart == null) return;
            SaveUndoState();
            string chartId = ActiveChart.Id;
            DocumentCharts.Remove(ActiveChart);
            if (ActiveExcelForm != null && !ActiveExcelForm.IsDisposed)
            {
                ActiveExcelForm.Close();
                ActiveExcelForm = null;
            }
            ActiveChart = null;

            string fullText = PieceTable.GetText();
            string tagPrefix = "![CHART:ID=" + chartId;
            int idx = fullText.IndexOf(tagPrefix);
            if (idx >= 0)
            {
                int lineStart = fullText.LastIndexOf('\n', Math.Max(0, idx - 1));
                if (lineStart < 0) lineStart = 0; else lineStart += 1;
                int lineEnd = fullText.IndexOf('\n', idx);
                if (lineEnd < 0) lineEnd = fullText.Length; else lineEnd += 1;

                int len = lineEnd - lineStart;
                if (len > 0)
                {
                    PieceTable.Remove(lineStart, len);
                    AdjustFormattedRunsOnDelete(lineStart, len);
                    CursorPosition = Math.Min(lineStart, PieceTable.Length);
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                }
            }
        }

        public void ToggleFooter(string text)
        {
            if (string.IsNullOrEmpty(FooterText)) FooterText = string.IsNullOrEmpty(text) ? "Chân trang GenOffice" : text;
            else FooterText = "";
        }

        public void TogglePageNumbers()
        {
            ShowPageNumbers = !ShowPageNumbers;
        }

        public void InsertPageBreak()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- PAGE BREAK ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertColumnBreak()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- COLUMN BREAK ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertTextWrappingBreak()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- TEXT WRAPPING BREAK ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertSectionBreakNextPage()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- SECTION BREAK (NEXT PAGE) ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertSectionBreakContinuous()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- SECTION BREAK (CONTINUOUS) ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertSectionBreakEvenPage()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- SECTION BREAK (EVEN PAGE) ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertSectionBreakOddPage()
        {
            SaveUndoState();
            if (SelectionStart != SelectionEnd) DeleteSelection();
            string tag = "\n[--- SECTION BREAK (ODD PAGE) ---]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertImage(string imagePath)
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);

            int imgW = 280;
            int imgH = 180;
            Image loadedImg = null;
            try
            {
                if (System.IO.File.Exists(imagePath))
                {
                    using (Image img = Image.FromFile(imagePath))
                    {
                        loadedImg = new Bitmap(img);
                        float ratio = (float)img.Width / Math.Max(1, img.Height);
                        if (ratio >= 1.0f)
                        {
                            imgW = Math.Min(480, Math.Max(120, img.Width));
                            imgH = (int)(imgW / ratio);
                        }
                        else
                        {
                            imgH = Math.Min(380, Math.Max(120, img.Height));
                            imgW = (int)(imgH * ratio);
                        }
                    }
                }
            }
            catch { }

            DocShapeObject sh = new DocShapeObject("Image", curX, curY, imgW, imgH);
            sh.ImagePath = imagePath;
            sh.CachedImage = loadedImg;
            sh.PageNumber = curPage;
            sh.PageX = curX;
            sh.PageY = curY;
            sh.FillColor = Color.Transparent;
            sh.OutlineColor = Color.Transparent;
            sh.OutlineWidth = 0;
            sh.WrapMode = 1; // Default to Square (Ngắt dòng vuông)

            DocumentShapes.Add(sh);
            foreach (var s in DocumentShapes) s.IsSelected = false;
            ActiveShape = sh;
            sh.IsSelected = true;
            if (ActiveChart != null) { ActiveChart.IsSelected = false; ActiveChart = null; }
            IsEditingHeaderFooter = false;

            string tag = string.Format("\n![SHAPE:ID={0}:TYPE=Image:PATH={1}:X={2}:Y={3}:W={4}:H={5}:WRAP={6}:PAGE={7}:STYLE=0]\n",
                sh.Id, imagePath, curX, curY, imgW, imgH, sh.WrapMode, curPage);
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void SyncShapeTagInDocument(DocShapeObject shape)
        {
            if (shape == null || string.IsNullOrEmpty(shape.Id)) return;
            string text = PieceTable.GetText();
            string search = "![SHAPE:ID=" + shape.Id + ":";
            int idx = text.IndexOf(search);
            if (idx >= 0)
            {
                int endIdx = text.IndexOf(']', idx);
                if (endIdx > idx)
                {
                    string oldTag = text.Substring(idx, endIdx - idx + 1);
                    int w = Math.Max(20, shape.BaseWidth + shape.WidthDelta);
                    int h = Math.Max(15, shape.BaseHeight + shape.HeightDelta);
                    string newTag = string.Format("![SHAPE:ID={0}:TYPE={1}:PATH={2}:X={3}:Y={4}:W={5}:H={6}:WRAP={7}:PAGE={8}:STYLE={9}]",
                        shape.Id, shape.ShapeType, shape.ImagePath ?? "", shape.PageX, shape.PageY, w, h, shape.WrapMode, shape.PageNumber, shape.PictureStylePreset);

                    if (oldTag != newTag)
                    {
                        PieceTable.Remove(idx, oldTag.Length);
                        PieceTable.Insert(idx, newTag);
                    }
                }
            }
        }

        public List<DocShapeObject> DocumentShapes = new List<DocShapeObject>();
        public List<DocShapeObject> SelectedShapes = new List<DocShapeObject>();
        public List<DocShapeObject> LastUngroupedShapes = new List<DocShapeObject>();
        public bool AlignToMargin = true;
        public bool AlignToPage = false;
        public bool UseAlignmentGuides = true;

        public DocShapeObject ActiveShape = null;
        public bool IsShapeSelected { get { return ActiveShape != null || (SelectedShapes != null && SelectedShapes.Count > 0); } }
        public bool IsDraggingShape = false;
        public int DragShapeHandleIndex = -1; // -1=Body, 0..7=8 handles, 8=rotation
        public Point ShapeDragStartMouse;
        public Point ShapeDragStartOffset;
        public Point ShapeDragStartCursorOffset;
        public int ShapeDragStartScrollY;
        public int ShapeDragStartPageNumber;
        public Size ShapeDragStartSize;

        public bool GroupSelectedShapes()
        {
            var shapesToGroup = new List<DocShapeObject>();
            if (SelectedShapes != null && SelectedShapes.Count >= 2)
            {
                shapesToGroup.AddRange(SelectedShapes);
            }
            else if (ActiveShape != null && DocumentShapes.Count >= 2)
            {
                shapesToGroup.AddRange(DocumentShapes);
            }

            if (shapesToGroup.Count < 2) return false;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            int pageNum = shapesToGroup[0].PageNumber;

            foreach (var s in shapesToGroup)
            {
                minX = Math.Min(minX, s.PageX);
                minY = Math.Min(minY, s.PageY);
                maxX = Math.Max(maxX, s.PageX + s.BaseWidth + s.WidthDelta);
                maxY = Math.Max(maxY, s.PageY + s.BaseHeight + s.HeightDelta);
            }

            DocShapeObject grp = new DocShapeObject();
            grp.ShapeType = "Group";
            grp.PageNumber = pageNum;
            grp.PageX = minX;
            grp.PageY = minY;
            grp.BaseWidth = Math.Max(20, maxX - minX);
            grp.BaseHeight = Math.Max(20, maxY - minY);
            grp.CustomName = "Nhóm " + (DocumentShapes.FindAll(delegate(DocShapeObject x) { return x.ShapeType == "Group"; }).Count + 1);

            foreach (var s in shapesToGroup)
            {
                s.PageX -= minX;
                s.PageY -= minY;
                s.IsSelected = false;
                grp.GroupChildren.Add(s);
                DocumentShapes.Remove(s);
            }

            DocumentShapes.Add(grp);
            ActiveShape = grp;
            grp.IsSelected = true;
            SelectedShapes.Clear();
            SelectedShapes.Add(grp);
            return true;
        }

        public bool UngroupSelectedShape()
        {
            if (ActiveShape == null || ActiveShape.ShapeType != "Group") return false;

            var grp = ActiveShape;
            LastUngroupedShapes.Clear();

            foreach (var child in grp.GroupChildren)
            {
                child.PageX += grp.PageX;
                child.PageY += grp.PageY;
                child.PageNumber = grp.PageNumber;
                child.IsSelected = true;
                DocumentShapes.Add(child);
                LastUngroupedShapes.Add(child);
            }

            DocumentShapes.Remove(grp);
            ActiveShape = LastUngroupedShapes.Count > 0 ? LastUngroupedShapes[0] : null;
            SelectedShapes.Clear();
            SelectedShapes.AddRange(LastUngroupedShapes);
            return true;
        }

        public bool RegroupSelectedShapes()
        {
            if (LastUngroupedShapes == null || LastUngroupedShapes.Count < 2) return false;
            SelectedShapes.Clear();
            foreach (var s in LastUngroupedShapes)
            {
                if (DocumentShapes.Contains(s))
                {
                    s.IsSelected = true;
                    SelectedShapes.Add(s);
                }
            }
            if (SelectedShapes.Count >= 2)
            {
                return GroupSelectedShapes();
            }
            return false;
        }

        public void AlignShapes(string alignType, RibbonBar ribbon)
        {
            var targets = (SelectedShapes != null && SelectedShapes.Count > 0) ? SelectedShapes : (ActiveShape != null ? new List<DocShapeObject> { ActiveShape } : null);
            if (targets == null || targets.Count == 0) return;

            int pageW = ribbon != null && ribbon.IsLandscape ? 1123 : 794;
            int pageH = ribbon != null && ribbon.IsLandscape ? 794 : 1123;
            int leftMargin = ribbon != null ? (int)(ribbon.GetEffectiveLeftMarginInches() * 96f) : 96;
            int rightMargin = ribbon != null ? (int)(ribbon.GetEffectiveRightMarginInches() * 96f) : 96;
            int topMargin = ribbon != null ? (int)(ribbon.GetEffectiveTopMarginInches() * 96f) : 96;
            int bottomMargin = ribbon != null ? (int)(ribbon.GetEffectiveBottomMarginInches() * 96f) : 96;

            if (targets.Count > 1 && !AlignToPage && !AlignToMargin)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (var s in targets)
                {
                    minX = Math.Min(minX, s.PageX);
                    minY = Math.Min(minY, s.PageY);
                    maxX = Math.Max(maxX, s.PageX + s.BaseWidth + s.WidthDelta);
                    maxY = Math.Max(maxY, s.PageY + s.BaseHeight + s.HeightDelta);
                }

                foreach (var s in targets)
                {
                    int sw = s.BaseWidth + s.WidthDelta;
                    int sh = s.BaseHeight + s.HeightDelta;
                    switch (alignType)
                    {
                        case "Left": s.PageX = minX; break;
                        case "Center": s.PageX = minX + (maxX - minX - sw) / 2; break;
                        case "Right": s.PageX = maxX - sw; break;
                        case "Top": s.PageY = minY; break;
                        case "Middle": s.PageY = minY + (maxY - minY - sh) / 2; break;
                        case "Bottom": s.PageY = maxY - sh; break;
                    }
                }
            }
            else
            {
                int targetLeft = AlignToMargin ? leftMargin : 0;
                int targetRight = AlignToMargin ? (pageW - rightMargin) : pageW;
                int targetTop = AlignToMargin ? topMargin : 0;
                int targetBottom = AlignToMargin ? (pageH - bottomMargin) : pageH;

                foreach (var s in targets)
                {
                    int sw = s.BaseWidth + s.WidthDelta;
                    int sh = s.BaseHeight + s.HeightDelta;
                    switch (alignType)
                    {
                        case "Left": s.PageX = targetLeft; break;
                        case "Center": s.PageX = targetLeft + (targetRight - targetLeft - sw) / 2; break;
                        case "Right": s.PageX = targetRight - sw; break;
                        case "Top": s.PageY = targetTop; break;
                        case "Middle": s.PageY = targetTop + (targetBottom - targetTop - sh) / 2; break;
                        case "Bottom": s.PageY = targetBottom - sh; break;
                    }
                }
            }
        }

        public void DistributeShapes(string direction)
        {
            var targets = (SelectedShapes != null && SelectedShapes.Count >= 3) ? new List<DocShapeObject>(SelectedShapes) : null;
            if (targets == null || targets.Count < 3) return;

            if (direction == "Horizontal")
            {
                targets.Sort(delegate(DocShapeObject a, DocShapeObject b) { return a.PageX.CompareTo(b.PageX); });
                int totalSpan = (targets[targets.Count - 1].PageX + targets[targets.Count - 1].BaseWidth + targets[targets.Count - 1].WidthDelta) - targets[0].PageX;
                int totalShapeWidth = 0;
                foreach (var s in targets) totalShapeWidth += (s.BaseWidth + s.WidthDelta);
                int totalGap = totalSpan - totalShapeWidth;
                int gapPerItem = totalGap / (targets.Count - 1);

                int curX = targets[0].PageX;
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].PageX = curX;
                    curX += (targets[i].BaseWidth + targets[i].WidthDelta) + gapPerItem;
                }
            }
            else if (direction == "Vertical")
            {
                targets.Sort(delegate(DocShapeObject a, DocShapeObject b) { return a.PageY.CompareTo(b.PageY); });
                int totalSpan = (targets[targets.Count - 1].PageY + targets[targets.Count - 1].BaseHeight + targets[targets.Count - 1].HeightDelta) - targets[0].PageY;
                int totalShapeHeight = 0;
                foreach (var s in targets) totalShapeHeight += (s.BaseHeight + s.HeightDelta);
                int totalGap = totalSpan - totalShapeHeight;
                int gapPerItem = totalGap / (targets.Count - 1);

                int curY = targets[0].PageY;
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].PageY = curY;
                    curY += (targets[i].BaseHeight + targets[i].HeightDelta) + gapPerItem;
                }
            }
        }

        private int _previewOrigPageX;
        private int _previewOrigPageY;
        private int _previewOrigWrapMode;
        private bool _hasPreviewBackup = false;

        public void BeginShapeLivePreview()
        {
            if (ActiveShape != null && !_hasPreviewBackup)
            {
                _previewOrigPageX = ActiveShape.PageX;
                _previewOrigPageY = ActiveShape.PageY;
                _previewOrigWrapMode = ActiveShape.WrapMode;
                _hasPreviewBackup = true;
            }
        }

        public void CancelShapeLivePreview()
        {
            if (_hasPreviewBackup && ActiveShape != null)
            {
                ActiveShape.PageX = _previewOrigPageX;
                ActiveShape.PageY = _previewOrigPageY;
                ActiveShape.WrapMode = _previewOrigWrapMode;
                _hasPreviewBackup = false;
            }
        }

        public void CommitShapeLivePreview()
        {
            _hasPreviewBackup = false;
        }

        public void PreviewPositionShape(int posIndex, RibbonBar ribbon)
        {
            if (ActiveShape == null) return;
            if (!_hasPreviewBackup) BeginShapeLivePreview();
            if (posIndex == 0)
            {
                ActiveShape.WrapMode = 0;
            }
            else
            {
                PositionShape(posIndex - 1, ribbon);
            }
        }

        public void PreviewWrapMode(int wrapMode)
        {
            if (ActiveShape == null) return;
            if (!_hasPreviewBackup) BeginShapeLivePreview();
            ActiveShape.WrapMode = wrapMode;
        }

        public void PositionShape(int posIndex, RibbonBar ribbon)
        {
            if (ActiveShape == null) return;
            int pageW = ribbon != null && ribbon.IsLandscape ? 1123 : 794;
            int pageH = ribbon != null && ribbon.IsLandscape ? 794 : 1123;
            int leftMargin = ribbon != null ? (int)(ribbon.GetEffectiveLeftMarginInches() * 96f) : 96;
            int rightMargin = ribbon != null ? (int)(ribbon.GetEffectiveRightMarginInches() * 96f) : 96;
            int topMargin = ribbon != null ? (int)(ribbon.GetEffectiveTopMarginInches() * 96f) : 96;
            int bottomMargin = ribbon != null ? (int)(ribbon.GetEffectiveBottomMarginInches() * 96f) : 96;

            int sw = ActiveShape.BaseWidth + ActiveShape.WidthDelta;
            int sh = ActiveShape.BaseHeight + ActiveShape.HeightDelta;

            int left = leftMargin;
            int centerH = leftMargin + (pageW - leftMargin - rightMargin - sw) / 2;
            int right = pageW - rightMargin - sw;

            int top = topMargin;
            int middleV = topMargin + (pageH - topMargin - bottomMargin - sh) / 2;
            int bottom = pageH - bottomMargin - sh;

            ActiveShape.WrapMode = 1; // Square wrap mode

            switch (posIndex)
            {
                case 0: ActiveShape.PageX = left; ActiveShape.PageY = top; break; // Top Left
                case 1: ActiveShape.PageX = centerH; ActiveShape.PageY = top; break; // Top Center
                case 2: ActiveShape.PageX = right; ActiveShape.PageY = top; break; // Top Right
                case 3: ActiveShape.PageX = left; ActiveShape.PageY = middleV; break; // Middle Left
                case 4: ActiveShape.PageX = centerH; ActiveShape.PageY = middleV; break; // Middle Center
                case 5: ActiveShape.PageX = right; ActiveShape.PageY = middleV; break; // Middle Right
                case 6: ActiveShape.PageX = left; ActiveShape.PageY = bottom; break; // Bottom Left
                case 7: ActiveShape.PageX = centerH; ActiveShape.PageY = bottom; break; // Bottom Center
                case 8: ActiveShape.PageX = right; ActiveShape.PageY = bottom; break; // Bottom Right
            }
        }

        public void RotateShape(float deltaAngle)
        {
            var targets = (SelectedShapes != null && SelectedShapes.Count > 0) ? SelectedShapes : (ActiveShape != null ? new List<DocShapeObject> { ActiveShape } : null);
            if (targets == null) return;
            foreach (var s in targets)
            {
                s.RotationAngle = (s.RotationAngle + deltaAngle + 360f) % 360f;
            }
        }

        public void FlipShape(bool horizontal)
        {
            var targets = (SelectedShapes != null && SelectedShapes.Count > 0) ? SelectedShapes : (ActiveShape != null ? new List<DocShapeObject> { ActiveShape } : null);
            if (targets == null) return;
            foreach (var s in targets)
            {
                if (horizontal) s.PictureFlipHorizontal = !s.PictureFlipHorizontal;
                else s.PictureFlipVertical = !s.PictureFlipVertical;
            }
        }

        public int GetMaxScrollY(int clientHeight, RibbonBar ribbon)
        {
            int stdH = ribbon != null && ribbon.IsLandscape ? 794 : 1123;
            int pageH = (int)(stdH * ZoomLevel);
            int pageHWithGap = pageH + 30;
            int bottomBuffer = 65; // Khoảng đệm xám 65px ở cuối trang giống Microsoft Word
            int totalDocHeight = 195 + Math.Max(1, TotalPageCount) * pageHWithGap - 30 + bottomBuffer;
            int viewHeight = clientHeight - 26; // Trừ thanh trạng thái 26px
            return Math.Max(0, totalDocHeight - viewHeight);
        }

        public void UpdateShapeDragPosition(int mouseX, int mouseY, int canvasWidth, RibbonBar ribbon)
        {
            if (ActiveShape == null || !IsDraggingShape || DragShapeHandleIndex != -1) return;

            int stdH = ribbon != null && ribbon.IsLandscape ? 794 : 1123;
            int pageDocH = stdH + (int)(30 / Math.Max(0.2f, ZoomLevel));

            int dx = (int)((mouseX - ShapeDragStartMouse.X) / Math.Max(0.2f, ZoomLevel));
            int dy = (int)((mouseY - ShapeDragStartMouse.Y) / Math.Max(0.2f, ZoomLevel));
            int dScroll = (int)((ScrollY - ShapeDragStartScrollY) / Math.Max(0.2f, ZoomLevel));

            int baseAbsDocY = ShapeDragStartPageNumber * pageDocH + ShapeDragStartOffset.Y;
            int currentAbsDocY = Math.Max(0, baseAbsDocY + dy + dScroll);

            ActiveShape.PageNumber = currentAbsDocY / pageDocH;
            ActiveShape.PageX = ShapeDragStartOffset.X + dx;
            ActiveShape.PageY = currentAbsDocY % pageDocH;

            // Tự động tạo thêm trang mới khi kéo đối tượng xuống các trang tiếp theo
            if (ActiveShape.PageNumber + 1 > CalculatedPageCount)
            {
                CalculatedPageCount = ActiveShape.PageNumber + 1;
                TotalPageCount = CalculatedPageCount;
            }
        }

        public void UpdateChartDragPosition(int mouseX, int mouseY, int canvasWidth, RibbonBar ribbon)
        {
            if (ActiveChart == null || !IsDraggingChart || DragChartHandleIndex != -1) return;

            int stdH = ribbon != null && ribbon.IsLandscape ? 794 : 1123;
            int pageDocH = stdH + (int)(30 / Math.Max(0.2f, ZoomLevel));

            int dx = (int)((mouseX - ChartDragStartMouse.X) / Math.Max(0.2f, ZoomLevel));
            int dy = (int)((mouseY - ChartDragStartMouse.Y) / Math.Max(0.2f, ZoomLevel));
            int dScroll = (int)((ScrollY - ChartDragStartScrollY) / Math.Max(0.2f, ZoomLevel));

            int baseAbsDocY = ChartDragStartPageNumber * pageDocH + ChartDragStartOffset.Y;
            int currentAbsDocY = Math.Max(0, baseAbsDocY + dy + dScroll);

            ActiveChart.PageNumber = currentAbsDocY / pageDocH;
            ActiveChart.PageX = ChartDragStartOffset.X + dx;
            ActiveChart.PageY = currentAbsDocY % pageDocH;

            // Tự động tạo thêm trang mới khi kéo biểu đồ xuống các trang tiếp theo
            if (ActiveChart.PageNumber + 1 > CalculatedPageCount)
            {
                CalculatedPageCount = ActiveChart.PageNumber + 1;
                TotalPageCount = CalculatedPageCount;
            }
        }

        public void InsertShape(string shapeType)
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);
            DocShapeObject sh = new DocShapeObject(shapeType, curX, curY, 180, 110);
            sh.PageNumber = curPage;
            sh.PageX = curX;
            sh.PageY = curY;
            DocumentShapes.Add(sh);
            foreach (var s in DocumentShapes) s.IsSelected = false;
            ActiveShape = sh;
            sh.IsSelected = true;
            if (ActiveChart != null) { ActiveChart.IsSelected = false; ActiveChart = null; }
            IsEditingHeaderFooter = false;

            string tag = "\n![SHAPE:ID=" + sh.Id + ":TYPE=" + shapeType + "]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public DocShapeObject GetOrCreateDocShape(string shapeId, string shapeType)
        {
            foreach (var sh in DocumentShapes)
            {
                if (sh.Id == shapeId) return sh;
            }
            DocShapeObject newSh = new DocShapeObject(shapeType, 80, 160, 180, 110);
            if (!string.IsNullOrEmpty(shapeId)) newSh.Id = shapeId;
            DocumentShapes.Add(newSh);
            return newSh;
        }

        public DocChartObject GetOrCreateChart(string chartId, string chartType)
        {
            foreach (var ch in DocumentCharts)
            {
                if (ch.Id == chartId) return ch;
            }
            DocChartObject newCh = new DocChartObject();
            if (!string.IsNullOrEmpty(chartId)) newCh.Id = chartId;
            newCh.Data = DocChartData.CreateDefault(chartType);
            DocumentCharts.Add(newCh);
            return newCh;
        }

        public void InsertChart(string chartType)
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);
            DocChartObject chart = new DocChartObject();
            chart.PageNumber = curPage;
            chart.PageX = curX;
            chart.PageY = curY;
            chart.Data = DocChartData.CreateDefault(chartType);
            DocumentCharts.Add(chart);
            ActiveChart = chart;
            chart.IsSelected = true;
            if (ActiveShape != null) { ActiveShape.IsSelected = false; ActiveShape = null; }

            string tag = "\n![CHART:ID=" + chart.Id + ":TYPE=" + chartType + "]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;

            OpenChartExcelEditor(chart);
        }

        public void OpenChartExcelEditor(DocChartObject chart)
        {
            if (chart == null) return;
            if (ActiveExcelForm != null && !ActiveExcelForm.IsDisposed)
            {
                ActiveExcelForm.Close();
            }
            ActiveExcelForm = new ChartDataExcelForm(chart.Data);
            ActiveExcelForm.OnDataChanged = (newData) =>
            {
                chart.Data = newData;
            };
            ActiveExcelForm.Show();
        }

        public void InsertNewComment(string initialText = "")
        {
            int selStart = 0;
            int selLen = 0;
            string selSnippet = "";

            if (SelectionStart != SelectionEnd)
            {
                selStart = Math.Min(SelectionStart, SelectionEnd);
                selLen = Math.Abs(SelectionEnd - SelectionStart);
                string full = PieceTable.GetText();
                if (selStart >= 0 && selStart + selLen <= full.Length)
                {
                    selSnippet = full.Substring(selStart, selLen);
                }
            }
            else
            {
                selStart = CursorPosition;
                selLen = 0;
                string full = PieceTable.GetText();
                if (CursorPosition >= 0 && CursorPosition < full.Length)
                {
                    selSnippet = full.Substring(CursorPosition, Math.Min(30, full.Length - CursorPosition));
                }
            }

            using (NewCommentDialogForm dlg = new NewCommentDialogForm(selSnippet))
            {
                if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.CommentText))
                {
                    SaveUndoState();
                    DocumentComment c = new DocumentComment
                    {
                        Id = Comments.Count + 1,
                        Author = Environment.UserName,
                        CreatedTime = DateTime.Now,
                        RangeStart = selStart,
                        RangeLength = Math.Max(1, selLen),
                        SelectedText = selSnippet,
                        Content = dlg.CommentText,
                        IsResolved = false
                    };
                    Comments.Add(c);
                    if (selLen > 0)
                    {
                        ApplyFormatToRange(selStart, Math.Max(1, selLen), null, 0, null, null, null, null, null, Color.FromArgb(233, 213, 255));
                    }
                }
            }
        }

        public void InsertNoteBox()
        {
            SaveUndoState();
            string tag = "\n![NOTEBOX:Nhập nội dung ghi chú tại đây...]\n";
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertSymbol(string sym)
        {
            if (ActiveShape != null && ActiveShape.IsEditingText)
            {
                SaveUndoState();
                int cur = Math.Max(0, Math.Min(ActiveShape.TextCursorIndex, ActiveShape.TextContent.Length));
                ActiveShape.TextContent = ActiveShape.TextContent.Insert(cur, sym);
                ActiveShape.TextCursorIndex = cur + sym.Length;
                return;
            }
            SaveUndoState();
            InsertText(sym);
        }

        public void InsertEquation(string formula = "")
        {
            SaveUndoState();
            string eqStr = string.IsNullOrEmpty(formula) ? " [ Nhập công thức vào đây ] " : " " + formula + " ";
            int insertPos = CursorPosition;
            PieceTable.Insert(insertPos, eqStr);
            FormattedRuns.Add(new FormattedRun(insertPos, eqStr.Length, "Cambria Math", Math.Max(12.0f, TypingFontSize * 1.1f), false, true, false, false, Color.Navy, Color.FromArgb(235, 243, 255)));
            CursorPosition += eqStr.Length;
            SelectionStart = insertPos;
            SelectionEnd = CursorPosition;
            IsEditingEquation = true;
        }

        public string GetSelectedText()
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                string full = PieceTable.GetText();
                if (min >= 0 && min + len <= full.Length)
                {
                    return full.Substring(min, len);
                }
            }
            return "";
        }

        public void InsertText(string text)
        {
            SaveUndoState();
            DeleteSelection();
            int insertPos = CursorPosition;
            PieceTable.Insert(insertPos, text);
            FormattedRuns.Add(new FormattedRun(insertPos, text.Length, TypingFont, TypingFontSize, TypingBold, TypingItalic, TypingUnderline, TypingStrike, TypingColor, TypingHighlight));
            CursorPosition += text.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertHyperlink(string text, string url, string tooltip = "")
        {
            SaveUndoState();
            DeleteSelection();
            int insertPos = CursorPosition;
            string linkText = string.IsNullOrEmpty(text) ? url : text;
            PieceTable.Insert(insertPos, linkText);
            FormattedRuns.Add(new FormattedRun(insertPos, linkText.Length, TypingFont, TypingFontSize, false, false, true, false, Color.FromArgb(0, 102, 204), Color.Transparent));
            CursorPosition += linkText.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertFootnote(string initialText = null, bool promptDialog = true)
        {
            if (promptDialog && initialText == null)
            {
                using (InsertFootnoteForm dlg = new InsertFootnoteForm(this, false))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        InsertFootnoteWithParams(dlg.NoteMarker, dlg.NoteText, dlg.IsEndnote);
                    }
                }
                return;
            }

            int curPg = CurrentActivePage > 0 ? CurrentActivePage : 1;
            int fnNum = 1;
            if (DetailedFootnotes != null)
            {
                foreach (var fn in DetailedFootnotes)
                {
                    if (!fn.IsEndnote && fn.PageNumber == curPg) fnNum++;
                }
            }
            string fnSuperscript = GetSuperscriptNumber(fnNum);
            InsertFootnoteWithParams(fnSuperscript, initialText ?? "", false);
        }

        public void InsertEndnote(string initialText = null, bool promptDialog = true)
        {
            if (promptDialog && initialText == null)
            {
                using (InsertFootnoteForm dlg = new InsertFootnoteForm(this, true))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        InsertFootnoteWithParams(dlg.NoteMarker, dlg.NoteText, dlg.IsEndnote);
                    }
                }
                return;
            }

            int enNum = 1;
            if (DetailedFootnotes != null)
            {
                foreach (var fn in DetailedFootnotes)
                {
                    if (fn.IsEndnote) enNum++;
                }
            }
            string enMarker = "[" + ToRomanNumeral(enNum).ToLower() + "]";
            InsertFootnoteWithParams(enMarker, initialText ?? "", true);
        }

        public void InsertFootnoteWithParams(string marker, string noteText, bool isEndnote)
        {
            SaveUndoState();
            int curPg = CurrentActivePage > 0 ? CurrentActivePage : 1;
            int anchor = CursorPosition;

            // Chèn duy nhất ký hiệu (marker) vào văn bản tại vị trí con trỏ
            InsertText(marker);

            if (DetailedFootnotes == null) DetailedFootnotes = new List<FootnoteItem>();
            if (Footnotes == null) Footnotes = new List<string>();

            FootnoteItem item = new FootnoteItem
            {
                Id = DetailedFootnotes.Count + 1,
                PageNumber = curPg,
                AnchorChar = anchor,
                Marker = marker,
                Text = noteText ?? "",
                IsEndnote = isEndnote
            };
            DetailedFootnotes.Add(item);
            Footnotes.Add(string.Format("{0} {1}: {2}", isEndnote ? "Endnote" : "Footnote (Trang " + curPg + ")", marker, noteText));
        }

        public void GoToNextFootnote(bool isEndnote = false, bool reverse = false)
        {
            if (DetailedFootnotes == null || DetailedFootnotes.Count == 0)
            {
                MessageBox.Show("Không tìm thấy chú thích (Footnote / Endnote) nào trong tài liệu!", "GenOffice Chú thích", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<FootnoteItem> matching = new List<FootnoteItem>();
            foreach (var fn in DetailedFootnotes)
            {
                if (fn.IsEndnote == isEndnote) matching.Add(fn);
            }

            if (matching.Count == 0)
            {
                MessageBox.Show(string.Format("Không có {0} nào trong tài liệu!", isEndnote ? "chú thích cuối tài liệu (Endnote)" : "chú thích chân trang (Footnote)"), "GenOffice Chú thích", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            matching.Sort((a, b) => a.AnchorChar.CompareTo(b.AnchorChar));

            FootnoteItem target = null;
            if (!reverse)
            {
                foreach (var fn in matching)
                {
                    if (fn.AnchorChar > CursorPosition)
                    {
                        target = fn;
                        break;
                    }
                }
                if (target == null) target = matching[0];
            }
            else
            {
                for (int i = matching.Count - 1; i >= 0; i--)
                {
                    if (matching[i].AnchorChar < CursorPosition)
                    {
                        target = matching[i];
                        break;
                    }
                }
                if (target == null) target = matching[matching.Count - 1];
            }

            CursorPosition = target.AnchorChar;
            SelectionStart = target.AnchorChar;
            SelectionEnd = target.AnchorChar + (target.Marker != null ? target.Marker.Length : 1);
            CurrentActivePage = target.PageNumber;
            ScrollY = Math.Max(0, (target.PageNumber - 1) * 1150);
        }

        public void ShowFootnotesList()
        {
            using (InsertFootnoteForm dlg = new InsertFootnoteForm(this, false))
            {
                dlg.ShowDialog();
            }
        }

        private string GetSuperscriptNumber(int num)
        {
            string[] supers = new string[] { "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹" };
            string s = num.ToString();
            string res = "";
            foreach (char c in s)
            {
                if (c >= '0' && c <= '9') res += supers[c - '0'];
                else res += c;
            }
            return res;
        }

        private string ToRomanNumeral(int number)
        {
            if (number < 1) return "i";
            if (number >= 1000) return "m" + ToRomanNumeral(number - 1000);
            if (number >= 900) return "cm" + ToRomanNumeral(number - 900);
            if (number >= 500) return "d" + ToRomanNumeral(number - 500);
            if (number >= 400) return "cd" + ToRomanNumeral(number - 400);
            if (number >= 100) return "c" + ToRomanNumeral(number - 100);
            if (number >= 90) return "xc" + ToRomanNumeral(number - 90);
            if (number >= 50) return "l" + ToRomanNumeral(number - 50);
            if (number >= 40) return "xl" + ToRomanNumeral(number - 40);
            if (number >= 10) return "x" + ToRomanNumeral(number - 10);
            if (number >= 9) return "ix" + ToRomanNumeral(number - 9);
            if (number >= 5) return "v" + ToRomanNumeral(number - 5);
            if (number >= 4) return "iv" + ToRomanNumeral(number - 4);
            if (number >= 1) return "i" + ToRomanNumeral(number - 1);
            return "i";
        }

        public List<TableOfContentsEntry> ExtractDetailedHeadings()
        {
            List<TableOfContentsEntry> result = new List<TableOfContentsEntry>();
            string fullText = PieceTable.GetText();
            if (string.IsNullOrEmpty(fullText)) return result;

            string[] paragraphs = fullText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int runningChar = 0;
            foreach (var pStr in paragraphs)
            {
                string trimmed = pStr.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                int level = 0;
                string title = trimmed;

                if (trimmed.StartsWith("### "))
                {
                    level = 3;
                    title = trimmed.Substring(4).Trim();
                }
                else if (trimmed.StartsWith("## "))
                {
                    level = 2;
                    title = trimmed.Substring(3).Trim();
                }
                else if (trimmed.StartsWith("# "))
                {
                    level = 1;
                    title = trimmed.Substring(2).Trim();
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\d+\.\d+\.\s*"))
                {
                    level = 3;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\d+\.\s*"))
                {
                    level = 2;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^(\d+\.|[I|V|X]+\.)\s*"))
                {
                    level = 1;
                }
                else
                {
                    FormattedRun fr = GetFormatAt(runningChar);
                    if (fr.Bold && trimmed.Length > 0 && trimmed.Length < 120 && !trimmed.StartsWith("|") && !trimmed.StartsWith("!["))
                    {
                        if (fr.FontSize >= 16) level = 1;
                        else if (fr.FontSize >= 13) level = 2;
                        else level = 3;
                    }
                }

                if (level > 0 && !string.IsNullOrEmpty(title))
                {
                    int linesCount = fullText.Substring(0, Math.Min(runningChar, fullText.Length)).Split('\n').Length;
                    int estPage = Math.Max(1, (linesCount / 38) + 1);

                    result.Add(new TableOfContentsEntry(level, title, estPage, runningChar));
                }

                runningChar += pStr.Length + 1;
            }
            return result;
        }

        public List<string> ExtractHeadings()
        {
            var detailed = ExtractDetailedHeadings();
            List<string> list = new List<string>();
            foreach (var d in detailed) list.Add(d.Title);
            return list;
        }

        public void UpdateTableOfContents()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int startTOC = fullText.IndexOf("================== MỤC LỤC TỰ ĐỘNG ==================");
            int endTOC = fullText.IndexOf("=====================================================");

            var headings = ExtractDetailedHeadings();
            if (headings.Count == 0)
            {
                MessageBox.Show("Chưa có đề mục (Heading) nào trong văn bản.\nVui lòng định dạng Heading 1, Heading 2 trước khi cập nhật Mục lục!", "Mục Lục Tự Động", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("================== MỤC LỤC TỰ ĐỘNG ==================");
            sb.AppendLine();
            foreach (var entry in headings)
            {
                string indent = entry.Level == 2 ? "   " : (entry.Level >= 3 ? "      " : "");
                string displayTitle = indent + entry.Title;
                int totalLen = 52;
                int padLen = Math.Max(3, totalLen - displayTitle.Length);
                string dots = new string('.', padLen);
                sb.AppendLine(string.Format("{0} {1} Trang {2}", displayTitle, dots, entry.Page));
            }
            sb.AppendLine();
            sb.Append("=====================================================");

            string newTOC = sb.ToString();

            if (startTOC >= 0 && endTOC > startTOC)
            {
                int totalEnd = endTOC + "=====================================================".Length;
                PieceTable.Remove(startTOC, totalEnd - startTOC);
                PieceTable.Insert(startTOC, newTOC);
                AdjustFormattedRunsOnDelete(startTOC, totalEnd - startTOC);
                AdjustFormattedRunsOnInsert(startTOC, newTOC.Length);
                MessageBox.Show(string.Format("Đã cập nhật mục lục thành công với {0} đề mục!", headings.Count), "Mục Lục Tự Động", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                InsertText("\n" + newTOC + "\n");
                MessageBox.Show(string.Format("Đã tạo bảng mục lục mới với {0} đề mục!", headings.Count), "Mục Lục Tự Động", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public List<string> ExtractFiguresAndTables()
        {
            List<string> list = new List<string>();
            if (CaptionsList != null && CaptionsList.Count > 0)
            {
                foreach (var c in CaptionsList)
                {
                    list.Add(c.GetFullCaption());
                }
            }
            if (DocumentShapes != null)
            {
                int figIdx = 1;
                foreach (var shp in DocumentShapes)
                {
                    if (shp.ShapeType == "Image" || shp.ShapeType == "Chart" || shp.ShapeType == "Rectangle")
                    {
                        string desc = !string.IsNullOrEmpty(shp.TextContent) ? shp.TextContent.Trim() : (shp.ShapeType + " " + figIdx);
                        if (desc.Length > 30) desc = desc.Substring(0, 30) + "...";
                        list.Add(string.Format("Hình {0}: {1}", figIdx++, desc));
                    }
                }
            }
            return list;
        }

        public void InsertTableOfFigures()
        {
            using (InsertTableOfFiguresForm figDlg = new InsertTableOfFiguresForm(this))
            {
                if (figDlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(figDlg.GeneratedTableOfFigures))
                {
                    InsertText("\n" + figDlg.GeneratedTableOfFigures + "\n");
                }
            }
        }

        public void InsertIndexTable()
        {
            using (InsertIndexForm idxDlg = new InsertIndexForm(this))
            {
                if (idxDlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(idxDlg.GeneratedIndex))
                {
                    InsertText("\n" + idxDlg.GeneratedIndex + "\n");
                }
            }
        }

        public void UpdateTableOfFigures()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int startFig = fullText.IndexOf("================== DANH MỤC HÌNH ẢNH ==================");
            int endFig = fullText.IndexOf("=========================================================");
            using (InsertTableOfFiguresForm figDlg = new InsertTableOfFiguresForm(this))
            {
                if (figDlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(figDlg.GeneratedTableOfFigures))
                {
                    if (startFig >= 0 && endFig > startFig)
                    {
                        int totalEnd = endFig + "=========================================================".Length;
                        PieceTable.Remove(startFig, totalEnd - startFig);
                        PieceTable.Insert(startFig, figDlg.GeneratedTableOfFigures);
                        AdjustFormattedRunsOnDelete(startFig, totalEnd - startFig);
                        AdjustFormattedRunsOnInsert(startFig, figDlg.GeneratedTableOfFigures.Length);
                        MessageBox.Show("Đã cập nhật danh mục hình ảnh thành công!", "Danh Mục Hình Ảnh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        InsertText("\n" + figDlg.GeneratedTableOfFigures + "\n");
                    }
                }
            }
        }

        public void UpdateIndex()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int startIdx = fullText.IndexOf("================== CHỈ MỤC TỪ KHÓA (INDEX) ==================");
            int endIdx = fullText.IndexOf("==============================================================");
            using (InsertIndexForm idxDlg = new InsertIndexForm(this))
            {
                if (idxDlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(idxDlg.GeneratedIndex))
                {
                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        int totalEnd = endIdx + "==============================================================".Length;
                        PieceTable.Remove(startIdx, totalEnd - startIdx);
                        PieceTable.Insert(startIdx, idxDlg.GeneratedIndex);
                        AdjustFormattedRunsOnDelete(startIdx, totalEnd - startIdx);
                        AdjustFormattedRunsOnInsert(startIdx, idxDlg.GeneratedIndex.Length);
                        MessageBox.Show("Đã cập nhật bảng chỉ mục thành công!", "Chỉ Mục Từ Khóa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        InsertText("\n" + idxDlg.GeneratedIndex + "\n");
                    }
                }
            }
        }

        public void PromoteHeading()
        {
            SaveUndoState();
            var fr = GetFormatAt(CursorPosition);
            if (fr.FontSize >= 16)
            {
                ApplyHeadingStyle(0); // Normal
            }
            else if (fr.FontSize >= 13)
            {
                ApplyHeadingStyle(1); // Heading 1
            }
            else
            {
                ApplyHeadingStyle(2); // Heading 2
            }
        }

        public void DemoteHeading()
        {
            SaveUndoState();
            var fr = GetFormatAt(CursorPosition);
            if (fr.FontSize >= 16)
            {
                ApplyHeadingStyle(2); // Heading 2
            }
            else if (fr.FontSize >= 13)
            {
                ApplyHeadingStyle(3); // Heading 3
            }
            else
            {
                ApplyHeadingStyle(0); // Normal
            }
        }

        public void InsertCitationFromSource(CitationSource source)
        {
            if (source == null) return;
            SaveUndoState();
            string cit = source.GetFormattedCitation(CurrentCitationStyle);
            InsertText(" " + cit + " ");
        }

        public void InsertBibliography()
        {
            SaveUndoState();
            if (CitationSources == null || CitationSources.Count == 0)
            {
                MessageBox.Show("Chưa có nguồn tài liệu tham khảo nào trong danh mục.\nVui lòng thêm nguồn trong 'Quản lý nguồn' trước khi chèn thư mục!", "Thư Mục Tài Liệu Tham Khảo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================== TÀI LIỆU THAM KHẢO ==================");
            sb.AppendLine();

            List<CitationSource> sorted = new List<CitationSource>(CitationSources);
            if (CurrentCitationStyle == "APA" || CurrentCitationStyle == "Harvard")
            {
                sorted.Sort((a, b) => string.Compare(a.Author, b.Author, StringComparison.CurrentCultureIgnoreCase));
            }

            int idx = 1;
            foreach (var src in sorted)
            {
                sb.AppendLine(src.GetFormattedBibliography(CurrentCitationStyle, idx++));
            }

            sb.AppendLine();
            sb.AppendLine("=========================================================");
            sb.AppendLine();

            InsertText(sb.ToString());
        }

        public void InsertCaption(string labelType, string title, bool isBelow = true)
        {
            SaveUndoState();
            if (CaptionsList == null) CaptionsList = new List<CaptionItem>();

            int count = 1;
            foreach (var c in CaptionsList)
            {
                if (c.LabelType == labelType) count++;
            }

            CaptionItem item = new CaptionItem
            {
                Id = "CAP_" + DateTime.Now.Ticks.ToString().Substring(10),
                LabelType = labelType,
                Number = count,
                Title = title,
                AnchorChar = CursorPosition,
                PageNumber = Math.Max(1, CurrentActivePage),
                IsBelow = isBelow
            };
            CaptionsList.Add(item);

            string capText = item.GetFullCaption();
            InsertText("\n" + capText + "\n");
        }

        public void MarkIndexEntry(string mainEntry, string subEntry, bool markAll = false)
        {
            SaveUndoState();
            string fullTag = string.IsNullOrEmpty(subEntry)
                ? string.Format(" {{XE \"{0}\"}} ", mainEntry)
                : string.Format(" {{XE \"{0}:{1}\"}} ", mainEntry, subEntry);

            if (markAll)
            {
                string full = PieceTable.GetText();
                int count = 0;
                int idx = 0;
                while (idx < full.Length)
                {
                    int found = full.IndexOf(mainEntry, idx, StringComparison.CurrentCultureIgnoreCase);
                    if (found < 0) break;
                    int insertAt = found + mainEntry.Length;
                    PieceTable.Insert(insertAt, fullTag);
                    AdjustFormattedRunsOnInsert(insertAt, fullTag.Length);
                    full = PieceTable.GetText();
                    idx = insertAt + fullTag.Length;
                    count++;
                }
                MessageBox.Show(string.Format("Đã đánh dấu tất cả {0} vị trí xuất hiện của '{1}'!", count > 0 ? count : 1, mainEntry), "Đánh Dấu Mục", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                InsertText(fullTag);
                MessageBox.Show(string.Format("Đã đánh dấu mục '{0}' tại vị trí con trỏ!", mainEntry), "Đánh Dấu Mục", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void InsertOnlineVideo(string title, string url)
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);
            string shapeId = "Video_" + DateTime.Now.Ticks;
            DocShapeObject videoShape = new DocShapeObject
            {
                Id = shapeId,
                ShapeType = "Rectangle",
                BaseWidth = 320,
                BaseHeight = 180,
                WrapMode = 0, // In-Line
                PageNumber = curPage,
                PageX = curX,
                PageY = curY,
                FillColor = Color.FromArgb(20, 25, 35),
                OutlineColor = Color.FromArgb(204, 0, 0),
                OutlineWidth = 2.0f,
                TextContent = "▶  " + title + "\n\n[Nhấp đúp để phát video trên Trình duyệt]",
                TextColor = Color.White,
                FontSize = 10f,
                Bold = true,
                TextAlignment = StringAlignment.Center,
                TextLineAlignment = StringAlignment.Center
            };
            DocumentShapes.Add(videoShape);
            ActiveShape = videoShape;
            videoShape.IsSelected = true;

            string tag = string.Format("\n![SHAPE:ID={0}:TYPE=Rectangle]\n", shapeId);
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertSmartArt(string type, string name)
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);
            string shapeId = "SmartArt_" + DateTime.Now.Ticks;

            DocShapeObject smartArtShape = new DocShapeObject
            {
                Id = shapeId,
                ShapeType = "SmartArt",
                SmartArtType = type,
                SmartArtTitle = name,
                BaseWidth = 440,
                BaseHeight = 190,
                WrapMode = 0,
                PageNumber = curPage,
                PageX = curX,
                PageY = curY,
                FillColor = Color.FromArgb(248, 250, 252),
                OutlineColor = Color.FromArgb(0, 120, 215),
                OutlineWidth = 1.5f
            };
            DocumentShapes.Add(smartArtShape);
            ActiveShape = smartArtShape;
            smartArtShape.IsSelected = true;

            string tag = string.Format("\n![SHAPE:ID={0}:TYPE=SmartArt]\n", shapeId);
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertWordArt(string text, int styleIndex)
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);
            string shapeId = "WordArt_" + DateTime.Now.Ticks;
            Color[] colors = new Color[] { Color.Black, Color.FromArgb(0, 114, 198), Color.FromArgb(216, 59, 1), Color.FromArgb(16, 124, 65), Color.FromArgb(112, 48, 160), Color.DarkOrange, Color.FromArgb(0, 120, 215), Color.DimGray, Color.Crimson, Color.Teal, Color.DarkGoldenrod, Color.MediumPurple, Color.ForestGreen, Color.DodgerBlue, Color.Tomato };
            Color c = (styleIndex >= 0 && styleIndex < colors.Length) ? colors[styleIndex] : Color.FromArgb(0, 114, 198);

            DocShapeObject wordArtShape = new DocShapeObject
            {
                Id = shapeId,
                ShapeType = "RoundedRectangle",
                BaseWidth = 280,
                BaseHeight = 70,
                WrapMode = 1, // Floating
                PageNumber = curPage,
                PageX = curX,
                PageY = curY,
                FillColor = Color.FromArgb(248, 250, 252),
                OutlineColor = c,
                OutlineWidth = 2.5f,
                TextContent = text,
                TextColor = c,
                FontSize = 14f,
                Bold = true,
                TextAlignment = StringAlignment.Center,
                TextLineAlignment = StringAlignment.Center
            };
            DocumentShapes.Add(wordArtShape);
            ActiveShape = wordArtShape;
            wordArtShape.IsSelected = true;

            string tag = string.Format("\n![SHAPE:ID={0}:TYPE=RoundedRectangle]\n", shapeId);
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertTextBoxShape()
        {
            SaveUndoState();
            int curPage, curX, curY;
            GetCursorPageCoordinates(out curPage, out curX, out curY);
            string shapeId = "TextBox_" + DateTime.Now.Ticks;
            DocShapeObject tbShape = new DocShapeObject
            {
                Id = shapeId,
                ShapeType = "TextBox",
                BaseWidth = 220,
                BaseHeight = 100,
                WrapMode = 1, // Floating
                PageNumber = curPage,
                PageX = curX,
                PageY = curY,
                FillColor = Color.White,
                OutlineColor = Color.FromArgb(0, 120, 215),
                OutlineWidth = 1.5f,
                TextContent = "",
                PlaceholderText = "Nhập văn bản vào đây...",
                TextColor = Color.Black,
                FontSize = 11f,
                Bold = false,
                TextAlignment = StringAlignment.Near,
                TextLineAlignment = StringAlignment.Near
            };
            DocumentShapes.Add(tbShape);
            foreach (var s in DocumentShapes) s.IsSelected = false;
            ActiveShape = tbShape;
            tbShape.IsSelected = true;
            if (ActiveChart != null) { ActiveChart.IsSelected = false; ActiveChart = null; }
            IsEditingHeaderFooter = false;

            string tag = string.Format("\n![SHAPE:ID={0}:TYPE=TextBox]\n", shapeId);
            PieceTable.Insert(CursorPosition, tag);
            AdjustFormattedRunsOnInsert(CursorPosition, tag.Length);
            CursorPosition += tag.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public bool LoadDocumentFile(string filepath, RibbonBar ribbon = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath)) return false;
                string ext = Path.GetExtension(filepath).ToLower();

                if (ext == ".docx")
                {
                    DocxMetadata meta;
                    if (DocxHandler.LoadDocxFile(filepath, out meta))
                    {
                        SaveUndoState();
                        PieceTable = new PieceTable(meta.TextContent ?? "");
                        FormattedRuns.Clear();
                        if (meta.FormattedRuns != null)
                        {
                            FormattedRuns.AddRange(meta.FormattedRuns);
                        }
                        ParagraphAlignments.Clear();
                        if (meta.ParagraphAlignments != null)
                        {
                            foreach (var kvp in meta.ParagraphAlignments) ParagraphAlignments[kvp.Key] = kvp.Value;
                        }
                        ParagraphBorders.Clear();
                        if (meta.ParagraphBorders != null)
                        {
                            foreach (var kvp in meta.ParagraphBorders) ParagraphBorders[kvp.Key] = kvp.Value.Clone();
                        }
                        ParagraphLineSpacings.Clear();
                        if (meta.ParagraphLineSpacings != null)
                        {
                            foreach (var kvp in meta.ParagraphLineSpacings) ParagraphLineSpacings[kvp.Key] = kvp.Value;
                        }
                        ParagraphSpacingsBefore.Clear();
                        if (meta.ParagraphSpacingsBefore != null)
                        {
                            foreach (var kvp in meta.ParagraphSpacingsBefore) ParagraphSpacingsBefore[kvp.Key] = kvp.Value;
                        }
                        ParagraphSpacingsAfter.Clear();
                        if (meta.ParagraphSpacingsAfter != null)
                        {
                            foreach (var kvp in meta.ParagraphSpacingsAfter) ParagraphSpacingsAfter[kvp.Key] = kvp.Value;
                        }

                        if (!string.IsNullOrEmpty(meta.HeaderText))
                        {
                            HeaderText = meta.HeaderText;
                            HeaderTitleText = meta.HeaderText;
                            CurrentHeaderPreset = HeaderFooterPreset.Austin;
                        }
                        if (!string.IsNullOrEmpty(meta.FooterText))
                        {
                            FooterText = meta.FooterText;
                        }
                        ShowPageNumbers = meta.ShowPageNumbers;

                        if (ribbon != null)
                        {
                            ribbon.IsLandscape = meta.IsLandscape;
                            ribbon.MarginMode = meta.MarginMode;
                        }

                        if (FormattedRuns.Count > 0)
                        {
                            TypingFont = FormattedRuns[0].FontName;
                            TypingFontSize = FormattedRuns[0].FontSize;
                            if (ribbon != null)
                            {
                                ribbon.SelectedFont = TypingFont;
                                ribbon.FontSize = TypingFontSize;
                            }
                        }
                        DocumentShapes.Clear();
                        DocumentCharts.Clear();
                        CursorPosition = 0;
                        SelectionStart = 0;
                        SelectionEnd = 0;
                        ScrollY = 0;
                        TargetScrollY = 0;
                        return true;
                    }
                }
                else
                {
                    string text = File.ReadAllText(filepath, Encoding.UTF8);
                    SaveUndoState();
                    PieceTable = new PieceTable(text);
                    FormattedRuns.Clear();
                    FormattedRuns.Add(new FormattedRun(0, text.Length, TypingFont, TypingFontSize, false, false, false, false, Color.Black, Color.Transparent));
                    ParagraphAlignments.Clear();
                    ParagraphBorders.Clear();
                    ParagraphLineSpacings.Clear();
                    ParagraphSpacingsBefore.Clear();
                    ParagraphSpacingsAfter.Clear();
                    DocumentShapes.Clear();
                    DocumentCharts.Clear();
                    CursorPosition = 0;
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    ScrollY = 0;
                    TargetScrollY = 0;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở tệp: " + ex.Message, "Lỗi mở tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool SaveDocumentFile(string filepath, RibbonBar ribbon = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filepath)) return false;
                string ext = Path.GetExtension(filepath).ToLower();

                if (ext == ".docx")
                {
                    string fullText = PieceTable.GetText();
                    bool isLandscape = ribbon != null ? ribbon.IsLandscape : false;
                    int marginMode = ribbon != null ? ribbon.MarginMode : 0;
                    string headerText = HeaderText ?? "";
                    string footerText = FooterText ?? "";
                    bool showPageNumbers = ShowPageNumbers;

                    return DocxHandler.SaveDocxFile(
                        filepath,
                        fullText,
                        FormattedRuns,
                        isLandscape,
                        marginMode,
                        headerText,
                        footerText,
                        showPageNumbers,
                        DocumentShapes,
                        DocumentCharts,
                        ParagraphAlignments,
                        ParagraphLineSpacings,
                        ParagraphSpacingsBefore,
                        ParagraphSpacingsAfter,
                        ParagraphBorders);
                }
                else
                {
                    File.WriteAllText(filepath, PieceTable.GetText(), Encoding.UTF8);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu tệp: " + ex.Message, "Lỗi lưu tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public List<int> GetSelectedOrCurrentParagraphStarts()
        {
            List<int> result = new List<int>();
            string fullText = PieceTable.GetText();
            if (string.IsNullOrEmpty(fullText))
            {
                result.Add(0);
                return result;
            }

            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);

            int curPStart = 0;
            string[] paras = fullText.Split('\n');
            for (int p = 0; p < paras.Length; p++)
            {
                int pEnd = curPStart + paras[p].Length; // end of paragraph text

                if (hasSelection)
                {
                    // Paragraph overlaps if selection intersects [curPStart, pEnd]
                    // If paragraph is non-empty: strictly overlaps if selMax > curPStart && selMin < pEnd
                    // If selection covers the newline at the end: selMax > curPStart && selMin <= pEnd
                    bool overlaps = (paras[p].Length > 0)
                        ? (selMax > curPStart && selMin < pEnd)
                        : (selMax > curPStart && selMin <= curPStart);

                    // Special case: if user selected across lines including the whole line
                    if (!overlaps && selMin <= curPStart && selMax >= pEnd && paras[p].Length > 0)
                    {
                        overlaps = true;
                    }

                    if (overlaps)
                    {
                        result.Add(curPStart);
                    }
                }
                else
                {
                    // Caret is inside this paragraph
                    if (p == paras.Length - 1)
                    {
                        if (CursorPosition >= curPStart)
                        {
                            result.Add(curPStart);
                        }
                    }
                    else
                    {
                        // Inside paragraph text (before newline)
                        if (CursorPosition >= curPStart && CursorPosition <= pEnd)
                        {
                            result.Add(curPStart);
                        }
                    }
                }

                curPStart = pEnd + 1;
            }

            if (result.Count == 0 && paras.Length > 0)
            {
                result.Add(0);
            }
            return result;
        }

        public void ApplyThemeToDocument(WriterThemeItem theme, RibbonBar ribbon)
        {
            if (theme == null || ribbon == null) return;
            SaveUndoState();

            ribbon.ActiveTheme = theme.Name;
            ribbon.ThemeFontHeading = theme.HeadingFont;
            ribbon.ThemeFontBody = theme.BodyFont;
            ribbon.ThemeColorPalette = theme.Palette;
            ribbon.SelectedFont = theme.BodyFont;
            ribbon.CurrentFontColor = Color.Black;

            Color accent = (theme.Palette != null && theme.Palette.Length > 0) ? theme.Palette[0] : Color.FromArgb(43, 87, 154);
            ribbon.PageBorderColor = accent;

            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            if (isSelectAll)
            {
                for (int r = 0; r < FormattedRuns.Count; r++)
                {
                    var run = FormattedRuns[r];
                    if (run.FontSize >= 14.0f || run.Bold)
                    {
                        run.FontName = theme.HeadingFont;
                        if (run.Color != Color.Transparent && run.Color != Color.Black)
                        {
                            run.Color = accent;
                        }
                    }
                    else
                    {
                        run.FontName = theme.BodyFont;
                    }
                }
                TypingFont = theme.BodyFont;
            }
            else if (hasSelection)
            {
                ApplyFormatToRange(selMin, selMax - selMin, theme.BodyFont, -1, null, null, null, null, null, null);
            }
            else
            {
                var targetPStarts = GetSelectedOrCurrentParagraphStarts();
                string[] paras = fullText.Split('\n');
                int curP = 0;
                for (int i = 0; i < paras.Length; i++)
                {
                    int pEnd = curP + paras[i].Length;
                    if (targetPStarts.Contains(curP))
                    {
                        ApplyFormatToRange(curP, paras[i].Length, theme.BodyFont, -1, null, null, null, null, null, null);
                    }
                    curP = pEnd + 1;
                }
            }
        }

        public void ApplyStyleSetToDocument(int styleSetIndex, RibbonBar ribbon)
        {
            if (ribbon == null) return;
            SaveUndoState();
            ribbon.ActiveStyleSetIndex = styleSetIndex;

            Color accent = (ribbon.ThemeColorPalette != null && ribbon.ThemeColorPalette.Length > 0)
                ? ribbon.ThemeColorPalette[0]
                : Color.FromArgb(43, 87, 154);

            string hFont = ribbon.ThemeFontHeading ?? "Calibri Light";
            string bFont = ribbon.ThemeFontBody ?? "Calibri";
            float ls = 1.15f;
            float beforePt = 0f;
            float afterPt = 8f;

            switch (styleSetIndex)
            {
                case 0: // Chuẩn Word (Default Blue)
                    ls = 1.15f; beforePt = 0f; afterPt = 8f;
                    break;
                case 1: // Hiện đại / Tối giản (Modern Minimalist)
                    hFont = "Segoe UI Semibold"; bFont = "Segoe UI";
                    ls = 1.25f; beforePt = 2f; afterPt = 10f;
                    accent = Color.FromArgb(31, 41, 55);
                    break;
                case 2: // Cổ điển (Classic Elegance)
                    hFont = "Georgia"; bFont = "Garamond";
                    ls = 1.15f; beforePt = 0f; afterPt = 6f;
                    accent = Color.FromArgb(120, 53, 15);
                    break;
                case 3: // Trang trọng / Báo cáo (Formal Executive)
                    hFont = "Times New Roman"; bFont = "Times New Roman";
                    ls = 1.20f; beforePt = 4f; afterPt = 8f;
                    accent = Color.FromArgb(5, 150, 105);
                    break;
            }

            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            var targetPStarts = GetSelectedOrCurrentParagraphStarts();

            if (isSelectAll)
            {
                ribbon.LineSpacingFactor = ls;
                ribbon.ParagraphSpacingBefore = beforePt;
                ribbon.ParagraphSpacingAfter = afterPt;

                ParagraphLineSpacings.Clear();
                ParagraphSpacingsBefore.Clear();
                ParagraphSpacingsAfter.Clear();

                for (int r = 0; r < FormattedRuns.Count; r++)
                {
                    var run = FormattedRuns[r];
                    if (run.FontSize >= 14.0f || run.Bold)
                    {
                        run.FontName = hFont;
                        if (run.Color != Color.Transparent && run.Color != Color.Black)
                        {
                            run.Color = accent;
                        }
                    }
                    else
                    {
                        run.FontName = bFont;
                    }
                }
                TypingFont = bFont;
            }
            else
            {
                foreach (int pStart in targetPStarts)
                {
                    ParagraphLineSpacings[pStart] = ls;
                    ParagraphSpacingsBefore[pStart] = beforePt;
                    ParagraphSpacingsAfter[pStart] = afterPt;
                }

                if (hasSelection)
                {
                    ApplyFormatToRange(selMin, selMax - selMin, bFont, -1, null, null, null, null, null, null);
                }
                else
                {
                    string[] paras = fullText.Split('\n');
                    int curP = 0;
                    for (int i = 0; i < paras.Length; i++)
                    {
                        int pEnd = curP + paras[i].Length;
                        if (targetPStarts.Contains(curP))
                        {
                            ApplyFormatToRange(curP, paras[i].Length, bFont, -1, null, null, null, null, null, null);
                        }
                        curP = pEnd + 1;
                    }
                }
            }
        }

        public void ApplyColorPaletteToDocument(ThemeColorPaletteItem palette, RibbonBar ribbon)
        {
            if (palette == null || ribbon == null) return;
            SaveUndoState();

            ribbon.ThemeColorPalette = palette.Colors;
            ribbon.ThemeColorPaletteName = palette.Name;
            Color accent = (palette.Colors != null && palette.Colors.Length > 0) ? palette.Colors[0] : Color.FromArgb(43, 87, 154);
            ribbon.PageBorderColor = accent;

            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            if (isSelectAll)
            {
                for (int r = 0; r < FormattedRuns.Count; r++)
                {
                    var run = FormattedRuns[r];
                    if ((run.FontSize >= 14.0f || run.Bold) && run.Color != Color.Black && run.Color != Color.Transparent)
                    {
                        run.Color = accent;
                    }
                }
            }
            else if (hasSelection)
            {
                ApplyFormatToRange(selMin, selMax - selMin, null, -1, null, null, null, null, accent, null);
            }
            else
            {
                var targetPStarts = GetSelectedOrCurrentParagraphStarts();
                string[] paras = fullText.Split('\n');
                int curP = 0;
                for (int i = 0; i < paras.Length; i++)
                {
                    int pEnd = curP + paras[i].Length;
                    if (targetPStarts.Contains(curP))
                    {
                        ApplyFormatToRange(curP, paras[i].Length, null, -1, null, null, null, null, accent, null);
                    }
                    curP = pEnd + 1;
                }
            }
        }

        public void ApplyFontPairToDocument(ThemeFontPairItem fontPair, RibbonBar ribbon)
        {
            if (fontPair == null || ribbon == null) return;
            SaveUndoState();

            ribbon.ThemeFontHeading = fontPair.HeadingFont;
            ribbon.ThemeFontBody = fontPair.BodyFont;
            ribbon.SelectedFont = fontPair.BodyFont;

            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            if (isSelectAll)
            {
                for (int r = 0; r < FormattedRuns.Count; r++)
                {
                    var run = FormattedRuns[r];
                    if (run.FontSize >= 14.0f || run.Bold)
                    {
                        run.FontName = fontPair.HeadingFont;
                    }
                    else
                    {
                        run.FontName = fontPair.BodyFont;
                    }
                }
                TypingFont = fontPair.BodyFont;
            }
            else if (hasSelection)
            {
                ApplyFormatToRange(selMin, selMax - selMin, fontPair.BodyFont, -1, null, null, null, null, null, null);
            }
            else
            {
                var targetPStarts = GetSelectedOrCurrentParagraphStarts();
                string[] paras = fullText.Split('\n');
                int curP = 0;
                for (int i = 0; i < paras.Length; i++)
                {
                    int pEnd = curP + paras[i].Length;
                    if (targetPStarts.Contains(curP))
                    {
                        ApplyFormatToRange(curP, paras[i].Length, fontPair.BodyFont, -1, null, null, null, null, null, null);
                    }
                    curP = pEnd + 1;
                }
            }
        }

        public void ApplyParagraphSpacingPreset(ParagraphSpacingItem spacing, RibbonBar ribbon)
        {
            if (spacing == null || ribbon == null) return;
            SaveUndoState();

            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            if (isSelectAll)
            {
                ParagraphLineSpacings.Clear();
                ParagraphSpacingsBefore.Clear();
                ParagraphSpacingsAfter.Clear();

                ribbon.ParagraphSpacingPreset = spacing.Name;
                ribbon.LineSpacingFactor = spacing.LineSpacingFactor;
                ribbon.ParagraphSpacingBefore = spacing.BeforePt;
                ribbon.ParagraphSpacingAfter = spacing.AfterPt;
            }
            else
            {
                foreach (int pStart in targetPStarts)
                {
                    ParagraphLineSpacings[pStart] = spacing.LineSpacingFactor;
                    ParagraphSpacingsBefore[pStart] = spacing.BeforePt;
                    ParagraphSpacingsAfter[pStart] = spacing.AfterPt;
                }
            }
        }

        public void ApplyCustomParagraphDefaults(string fontName, float fontSize, Color fontColor, int align, float beforePt, float afterPt, float lineSpacingFactor, RibbonBar ribbon)
        {
            SaveUndoState();

            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            if (isSelectAll)
            {
                ParagraphLineSpacings.Clear();
                ParagraphSpacingsBefore.Clear();
                ParagraphSpacingsAfter.Clear();
                ParagraphAlignments.Clear();

                if (ribbon != null)
                {
                    ribbon.SelectedFont = fontName;
                    ribbon.FontSize = fontSize;
                    ribbon.CurrentFontColor = fontColor;
                    ribbon.LineSpacingFactor = lineSpacingFactor;
                    ribbon.ParagraphSpacingBefore = beforePt;
                    ribbon.ParagraphSpacingAfter = afterPt;
                    ribbon.Alignment = align;
                }

                TypingFont = fontName;
                TypingFontSize = fontSize;
            }
            else
            {
                foreach (int pStart in targetPStarts)
                {
                    ParagraphLineSpacings[pStart] = lineSpacingFactor;
                    ParagraphSpacingsBefore[pStart] = beforePt;
                    ParagraphSpacingsAfter[pStart] = afterPt;
                    ParagraphAlignments[pStart] = align;
                }

                if (hasSelection)
                {
                    ApplyFormatToRange(selMin, selMax - selMin, fontName, fontSize, null, null, null, null, fontColor, null);
                }
                else
                {
                    string[] paras = fullText.Split('\n');
                    int curP = 0;
                    for (int i = 0; i < paras.Length; i++)
                    {
                        int pEnd = curP + paras[i].Length;
                        if (targetPStarts.Contains(curP))
                        {
                            ApplyFormatToRange(curP, paras[i].Length, fontName, fontSize, null, null, null, null, fontColor, null);
                        }
                        curP = pEnd + 1;
                    }
                }
            }
        }

        public void ApplyParagraphBorderPreset(int borderType, Color? col = null, float size = 1.0f)
        {
            SaveUndoState();
            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            Color borderCol = col ?? Color.Black;

            foreach (int pStart in targetPStarts)
            {
                if (borderType == 4) // No Border
                {
                    if (ParagraphBorders.ContainsKey(pStart)) ParagraphBorders.Remove(pStart);
                    continue;
                }

                ParagraphBorderInfo info = ParagraphBorders.ContainsKey(pStart) ? ParagraphBorders[pStart].Clone() : new ParagraphBorderInfo();
                info.Color = borderCol;
                info.SizePt = size;

                switch (borderType)
                {
                    case 0: // Bottom Border
                        info.Bottom = true;
                        break;
                    case 1: // Top Border
                        info.Top = true;
                        break;
                    case 2: // Left Border
                        info.Left = true;
                        break;
                    case 3: // Right Border
                        info.Right = true;
                        break;
                    case 5: // All Borders
                    case 6: // Outside Borders (Box)
                        info.Top = true;
                        info.Bottom = true;
                        info.Left = true;
                        info.Right = true;
                        info.IsHorizontalLine = false;
                        break;
                    case 7: // Inside Borders
                    case 8: // Inside Horizontal
                        info.Bottom = true;
                        break;
                    case 9: // Inside Vertical
                        info.Left = true;
                        info.Right = true;
                        break;
                    case 10: // Diagonal Down
                    case 11: // Diagonal Up
                        info.Bottom = true;
                        break;
                    case 13: // Horizontal Line
                        info.IsHorizontalLine = true;
                        info.Bottom = true;
                        break;
                }
                ParagraphBorders[pStart] = info;
            }
        }

        public int CurrentAlignment = 0; // 0=Left, 1=Center, 2=Right, 3=Justified

        public void ApplyAlignmentToSelectionOrParagraph(int align)
        {
            SaveUndoState();
            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);
            bool isSelectAll = (hasSelection && selMin == 0 && selMax >= fullText.Length && fullText.Length > 0);

            if (isSelectAll)
            {
                ParagraphAlignments.Clear();
                CurrentAlignment = align;
            }
            else
            {
                foreach (int pStart in targetPStarts)
                {
                    ParagraphAlignments[pStart] = align;
                }
            }
        }

        public int CurrentCaseMode = 0; // 0 = UPPERCASE, 1 = Sentence case, 2 = lowercase, 3 = Title Case

        public void ChangeCaseCycle()
        {
            SaveUndoState();
            string full = PieceTable.GetText();
            int min = Math.Min(SelectionStart, SelectionEnd);
            int len = Math.Abs(SelectionEnd - SelectionStart);

            int targetStart = min;
            int targetLen = len;

            if (targetLen == 0)
            {
                int curPos = CursorPosition;
                int lineStart = (curPos > 0 && full.Length >= curPos) ? full.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
                if (lineStart < 0) lineStart = 0;
                int lineEnd = full.IndexOf('\n', lineStart);
                if (lineEnd < 0) lineEnd = full.Length;

                targetStart = lineStart;
                targetLen = lineEnd - lineStart;
            }

            if (targetLen > 0 && targetStart >= 0 && targetStart + targetLen <= full.Length)
            {
                string target = full.Substring(targetStart, targetLen);
                string res = target;

                CurrentCaseMode = (CurrentCaseMode + 1) % 4;

                if (CurrentCaseMode == 0)
                {
                    res = target.ToUpper();
                }
                else if (CurrentCaseMode == 1)
                {
                    res = ToSentenceCase(target);
                }
                else if (CurrentCaseMode == 2)
                {
                    res = target.ToLower();
                }
                else if (CurrentCaseMode == 3)
                {
                    res = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(target.ToLower());
                }

                PieceTable.Remove(targetStart, targetLen);
                PieceTable.Insert(targetStart, res);
                SelectionStart = targetStart;
                SelectionEnd = targetStart + res.Length;
                CursorPosition = SelectionEnd;
            }
        }

        public string ToSentenceCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            char[] chars = input.ToLower().ToCharArray();
            bool capitalizeNext = true;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsLetter(c))
                {
                    if (capitalizeNext)
                    {
                        chars[i] = char.ToUpper(c);
                        capitalizeNext = false;
                    }
                }
                else if (c == '.' || c == '?' || c == '!' || c == '\n')
                {
                    capitalizeNext = true;
                }
            }
            return new string(chars);
        }

        public void ClearFormatting()
        {
            SaveUndoState();
            int min = Math.Min(SelectionStart, SelectionEnd);
            int len = Math.Abs(SelectionEnd - SelectionStart);

            TypingFont = "Times New Roman";
            TypingFontSize = 11.0f;
            TypingBold = false;
            TypingItalic = false;
            TypingUnderline = false;
            TypingStrike = false;
            TypingColor = Color.Black;
            TypingHighlight = Color.Transparent;
            TypingSubscript = false;
            TypingSuperscript = false;

            if (len > 0 && min >= 0)
            {
                ApplyFormatToRange(min, len, "Times New Roman", 11.0f, false, false, false, false, Color.Black, Color.Transparent);
            }
        }

        public void ToggleBullets()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;
            int lineStart = (curPos > 0 && fullText.Length >= curPos) ? fullText.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
            if (lineStart < 0) lineStart = 0;
            int lineEnd = fullText.IndexOf('\n', lineStart);
            if (lineEnd < 0) lineEnd = fullText.Length;

            string lineText = fullText.Substring(lineStart, lineEnd - lineStart);
            if (lineText.StartsWith("\t•\t") || lineText.StartsWith("•\t") || lineText.StartsWith("•   "))
            {
                int prefixLen = lineText.StartsWith("\t•\t") ? 3 : 2;
                PieceTable.Remove(lineStart, prefixLen);
                AdjustFormattedRunsOnDelete(lineStart, prefixLen);
                CursorPosition = Math.Max(lineStart, CursorPosition - prefixLen);
            }
            else
            {
                string bulletStr = "\t•\t";
                PieceTable.Insert(lineStart, bulletStr);
                AdjustFormattedRunsOnInsert(lineStart, bulletStr.Length);
                CursorPosition += bulletStr.Length;
            }
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void ToggleNumbering()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;
            int lineStart = (curPos > 0 && fullText.Length >= curPos) ? fullText.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
            if (lineStart < 0) lineStart = 0;
            int lineEnd = fullText.IndexOf('\n', lineStart);
            if (lineEnd < 0) lineEnd = fullText.Length;

            string lineText = fullText.Substring(lineStart, lineEnd - lineStart);
            if (System.Text.RegularExpressions.Regex.IsMatch(lineText, @"^\t*\d+\.[\t\s]"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(lineText, @"^\t*\d+\.[\t\s]");
                PieceTable.Remove(lineStart, match.Length);
                AdjustFormattedRunsOnDelete(lineStart, match.Length);
                CursorPosition = Math.Max(lineStart, CursorPosition - match.Length);
            }
            else
            {
                string numStr = "\t1.\t";
                PieceTable.Insert(lineStart, numStr);
                AdjustFormattedRunsOnInsert(lineStart, numStr.Length);
                CursorPosition += numStr.Length;
            }
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void ApplyBulletStyle(string bullet)
        {
            if (string.IsNullOrEmpty(bullet) || bullet == "none")
            {
                RemoveListPrefixFromSelection();
                return;
            }
            if (bullet == "level")
            {
                IndentOrInsertTab();
                return;
            }

            SaveUndoState();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            if (selMin == selMax)
            {
                selMin = CursorPosition;
                selMax = CursorPosition;
            }

            int pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
            if (pStart < 0) pStart = 0;
            int pEnd = fullText.IndexOf('\n', selMax);
            if (pEnd < 0) pEnd = fullText.Length;

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] lines = sub.Split('\n');
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string clean = RemovePrefix(lines[i]);
                sb.Append("\t" + bullet + "\t" + clean);
                if (i < lines.Length - 1) sb.Append("\n");
            }

            string newSub = sb.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);

            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        public void ApplyNumberingStyle(int formatIndex)
        {
            if (formatIndex == 0)
            {
                RemoveListPrefixFromSelection();
                return;
            }

            SaveUndoState();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            if (selMin == selMax)
            {
                selMin = CursorPosition;
                selMax = CursorPosition;
            }

            int pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
            if (pStart < 0) pStart = 0;
            int pEnd = fullText.IndexOf('\n', selMax);
            if (pEnd < 0) pEnd = fullText.Length;

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] lines = sub.Split('\n');
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string clean = RemovePrefix(lines[i]);
                string numTag = GetNumberingTag(i + 1, formatIndex);
                sb.Append("\t" + numTag + "\t" + clean);
                if (i < lines.Length - 1) sb.Append("\n");
            }

            string newSub = sb.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);

            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        public void ApplyMultilevelStyle(int styleIndex)
        {
            if (styleIndex == 0)
            {
                RemoveListPrefixFromSelection();
                return;
            }

            SaveUndoState();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            if (selMin == selMax)
            {
                selMin = CursorPosition;
                selMax = CursorPosition;
            }

            int pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
            if (pStart < 0) pStart = 0;
            int pEnd = fullText.IndexOf('\n', selMax);
            if (pEnd < 0) pEnd = fullText.Length;

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] lines = sub.Split('\n');
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int tabCount = 0;
                while (tabCount < line.Length && line[tabCount] == '\t') tabCount++;
                string clean = RemovePrefix(line);

                string tag = (i + 1).ToString() + ".";
                if (styleIndex == 1) // 1) a) i)
                {
                    if (tabCount == 0) tag = (i + 1).ToString() + ")";
                    else if (tabCount == 1) tag = ToAlpha(i + 1, false) + ")";
                    else tag = ToRoman(i + 1).ToLower() + ")";
                }
                else if (styleIndex == 2) // 1. 1.1. 1.1.1.
                {
                    if (tabCount == 0) tag = (i + 1).ToString() + ".";
                    else if (tabCount == 1) tag = "1." + (i + 1).ToString() + ".";
                    else tag = "1.1." + (i + 1).ToString() + ".";
                }
                else if (styleIndex == 3) // ❖ ➢ ■
                {
                    if (tabCount == 0) tag = "❖";
                    else if (tabCount == 1) tag = "➢";
                    else tag = "■";
                }
                else if (styleIndex == 4) // Article I. Section 1.01 (a)
                {
                    if (tabCount == 0) tag = "Article " + ToRoman(i + 1) + ".";
                    else if (tabCount == 1) tag = "Section 1.0" + (i + 1).ToString();
                    else tag = "(" + ToAlpha(i + 1, false) + ")";
                }

                sb.Append(new string('\t', tabCount) + "\t" + tag + "\t" + clean);
                if (i < lines.Length - 1) sb.Append("\n");
            }

            string newSub = sb.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);

            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        private void RemoveListPrefixFromSelection()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            if (selMin == selMax)
            {
                selMin = CursorPosition;
                selMax = CursorPosition;
            }

            int pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
            if (pStart < 0) pStart = 0;
            int pEnd = fullText.IndexOf('\n', selMax);
            if (pEnd < 0) pEnd = fullText.Length;

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] lines = sub.Split('\n');
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(RemovePrefix(lines[i]));
                if (i < lines.Length - 1) sb.Append("\n");
            }

            string newSub = sb.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);

            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        private static string RemovePrefix(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";
            string trimmed = line.TrimStart('\t', ' ');
            if (trimmed.StartsWith("●") || trimmed.StartsWith("○") || trimmed.StartsWith("■") || trimmed.StartsWith("✤") ||
                trimmed.StartsWith("❖") || trimmed.StartsWith("➢") || trimmed.StartsWith("✓") || trimmed.StartsWith("•"))
            {
                trimmed = trimmed.Substring(1).TrimStart('\t', ' ');
                return trimmed;
            }
            var m = System.Text.RegularExpressions.Regex.Match(trimmed, @"^([0-9]+|[a-zA-Z]+|[ivxlcdmIVXLCDM]+)[\.\)][\t\s]+");
            if (m.Success)
            {
                trimmed = trimmed.Substring(m.Length).TrimStart('\t', ' ');
                return trimmed;
            }
            return line.TrimStart('\t');
        }

        private static string GetNumberingTag(int num, int formatIndex)
        {
            switch (formatIndex)
            {
                case 1: return num.ToString() + ".";
                case 2: return num.ToString() + ")";
                case 3: return ToRoman(num) + ".";
                case 4: return ToAlpha(num, true) + ".";
                case 5: return ToAlpha(num, false) + ")";
                case 6: return ToAlpha(num, false) + ".";
                case 7: return ToRoman(num).ToLower() + ".";
                default: return num.ToString() + ".";
            }
        }

        private static string ToRoman(int number)
        {
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            return string.Empty;
        }

        private static string ToAlpha(int number, bool uppercase)
        {
            if (number < 1) number = 1;
            char c = (char)((uppercase ? 'A' : 'a') + ((number - 1) % 26));
            return c.ToString();
        }

        #region Complete Word Shortcuts Helper Methods
        public void IncreaseFontSize(float delta = 1f)
        {
            float target = (TypingFontSize > 0 ? TypingFontSize : 12f) + delta;
            ApplyFontSizeToSelectionOrTyping(target);
        }

        public void DecreaseFontSize(float delta = 1f)
        {
            float target = Math.Max(4f, (TypingFontSize > 0 ? TypingFontSize : 12f) - delta);
            ApplyFontSizeToSelectionOrTyping(target);
        }

        private static readonly float[] StandardFontSizes = new float[] { 8f, 9f, 10f, 11f, 12f, 14f, 16f, 18f, 20f, 22f, 24f, 26f, 28f, 36f, 48f, 72f };

        public void IncreaseFontSizePreset()
        {
            float cur = TypingFontSize > 0 ? TypingFontSize : 12f;
            float next = cur + 2f;
            for (int i = 0; i < StandardFontSizes.Length; i++)
            {
                if (StandardFontSizes[i] > cur)
                {
                    next = StandardFontSizes[i];
                    break;
                }
            }
            ApplyFontSizeToSelectionOrTyping(next);
        }

        public void DecreaseFontSizePreset()
        {
            float cur = TypingFontSize > 0 ? TypingFontSize : 12f;
            float prev = Math.Max(4f, cur - 2f);
            for (int i = StandardFontSizes.Length - 1; i >= 0; i--)
            {
                if (StandardFontSizes[i] < cur)
                {
                    prev = StandardFontSizes[i];
                    break;
                }
            }
            ApplyFontSizeToSelectionOrTyping(prev);
        }

        public void ToggleSubscript()
        {
            ApplySubscriptToSelectionOrTyping(!TypingSubscript);
        }

        public void ToggleSuperscript()
        {
            ApplySuperscriptToSelectionOrTyping(!TypingSuperscript);
        }

        public void ToggleDoubleUnderline()
        {
            ApplyUnderlineToSelectionOrTyping(!TypingUnderline);
        }

        public void ToggleWordsUnderline()
        {
            ApplyUnderlineToSelectionOrTyping(!TypingUnderline);
        }

        public void ApplyAllCaps()
        {
            SaveUndoState();
            string full = PieceTable.GetText();
            int min = Math.Min(SelectionStart, SelectionEnd);
            int len = Math.Abs(SelectionEnd - SelectionStart);
            if (len > 0 && min >= 0 && min + len <= full.Length)
            {
                string target = full.Substring(min, len);
                string upper = target.ToUpper();
                PieceTable.Remove(min, len);
                PieceTable.Insert(min, upper);
                SelectionStart = min;
                SelectionEnd = min + upper.Length;
                CursorPosition = SelectionEnd;
            }
        }

        public void ToggleSpaceBeforeParagraph()
        {
            SaveUndoState();
            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            foreach (int pStart in targetPStarts)
            {
                if (ParagraphSpacingsBefore.ContainsKey(pStart) && ParagraphSpacingsBefore[pStart] > 0)
                {
                    ParagraphSpacingsBefore[pStart] = 0f;
                }
                else
                {
                    ParagraphSpacingsBefore[pStart] = 12f;
                }
            }
        }

        public void SetHangingIndent(bool increase)
        {
            SaveUndoState();
            if (increase) IndentOrInsertTab();
            else Outdent();
        }

        public void ClearParagraphFormatting()
        {
            SaveUndoState();
            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            foreach (int pStart in targetPStarts)
            {
                ParagraphAlignments[pStart] = 0;
                ParagraphLineSpacings[pStart] = 1.0f;
                ParagraphSpacingsBefore[pStart] = 0f;
                ParagraphSpacingsAfter[pStart] = 0f;
            }
        }

        public void InsertCurrentDate()
        {
            InsertText(DateTime.Now.ToString("dd/MM/yyyy"));
        }

        public void InsertCurrentTime()
        {
            InsertText(DateTime.Now.ToString("HH:mm:ss"));
        }

        public void InsertSoftLineBreak()
        {
            SaveUndoState();
            DeleteSelection();
            int curPos = CursorPosition;
            PieceTable.Insert(curPos, "\n");
            FormattedRuns.Add(new FormattedRun(curPos, 1, TypingFont, TypingFontSize, TypingBold, TypingItalic, TypingUnderline, TypingStrike, TypingColor, TypingHighlight, TypingSubscript, TypingSuperscript));
            CursorPosition++;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public int F8SelectionStage = 0; // 0=Off, 1=Active, 2=Word, 3=Sentence, 4=Paragraph, 5=All

        public void ExtendSelectionF8()
        {
            F8SelectionStage++;
            if (F8SelectionStage > 5) F8SelectionStage = 2;

            string full = PieceTable.GetText();
            if (string.IsNullOrEmpty(full)) return;
            int cur = Math.Max(0, Math.Min(CursorPosition, full.Length));

            switch (F8SelectionStage)
            {
                case 1:
                case 2: // Select Word
                    {
                        int wStart = cur;
                        while (wStart > 0 && char.IsLetterOrDigit(full[wStart - 1])) wStart--;
                        int wEnd = cur;
                        while (wEnd < full.Length && char.IsLetterOrDigit(full[wEnd])) wEnd++;
                        SelectionStart = wStart;
                        SelectionEnd = wEnd;
                        CursorPosition = wEnd;
                        break;
                    }
                case 3: // Select Sentence
                    {
                        int sStart = cur;
                        while (sStart > 0 && full[sStart - 1] != '.' && full[sStart - 1] != '?' && full[sStart - 1] != '!' && full[sStart - 1] != '\n') sStart--;
                        int sEnd = cur;
                        while (sEnd < full.Length && full[sEnd] != '.' && full[sEnd] != '?' && full[sEnd] != '!' && full[sEnd] != '\n') sEnd++;
                        if (sEnd < full.Length && (full[sEnd] == '.' || full[sEnd] == '?' || full[sEnd] == '!')) sEnd++;
                        SelectionStart = sStart;
                        SelectionEnd = sEnd;
                        CursorPosition = sEnd;
                        break;
                    }
                case 4: // Select Paragraph
                    {
                        int pStart = (cur > 0) ? full.LastIndexOf('\n', cur - 1) + 1 : 0;
                        if (pStart < 0) pStart = 0;
                        int pEnd = full.IndexOf('\n', cur);
                        if (pEnd < 0) pEnd = full.Length;
                        SelectionStart = pStart;
                        SelectionEnd = pEnd;
                        CursorPosition = pEnd;
                        break;
                    }
                case 5: // Select All
                    {
                        SelectAll();
                        break;
                    }
            }
        }
        #endregion

        public void SortSelectedParagraphs(bool ascending, string sortType, bool hasHeader)
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            if (string.IsNullOrEmpty(fullText)) return;

            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool entireDoc = (selMin == selMax);

            int pStart = 0;
            int pEnd = fullText.Length;
            if (!entireDoc)
            {
                pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
                if (pStart < 0) pStart = 0;
                pEnd = fullText.IndexOf('\n', selMax);
                if (pEnd < 0) pEnd = fullText.Length;
            }

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] rawLines = sub.Split('\n');
            if (rawLines.Length <= 1) return;

            List<string> linesToSort = new List<string>();
            string headerLine = null;
            int startIdx = 0;

            if (hasHeader && rawLines.Length > 1)
            {
                headerLine = rawLines[0];
                startIdx = 1;
            }

            for (int i = startIdx; i < rawLines.Length; i++)
            {
                linesToSort.Add(rawLines[i]);
            }

            linesToSort.Sort((a, b) =>
            {
                int cmp = 0;
                if (sortType != null && (sortType.Contains("Số") || sortType.Contains("Number")))
                {
                    double na = 0, nb = 0;
                    string sa = System.Text.RegularExpressions.Regex.Match(a, @"[-+]?[0-9]*\.?[0-9]+").Value;
                    string sb = System.Text.RegularExpressions.Regex.Match(b, @"[-+]?[0-9]*\.?[0-9]+").Value;
                    double.TryParse(sa, out na);
                    double.TryParse(sb, out nb);
                    cmp = na.CompareTo(nb);
                }
                else if (sortType != null && (sortType.Contains("Ngày") || sortType.Contains("Date")))
                {
                    DateTime da = DateTime.MinValue, db = DateTime.MinValue;
                    DateTime.TryParse(a, out da);
                    DateTime.TryParse(b, out db);
                    cmp = da.CompareTo(db);
                }
                else
                {
                    cmp = string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
                }
                return ascending ? cmp : -cmp;
            });

            StringBuilder res = new StringBuilder();
            if (headerLine != null)
            {
                res.Append(headerLine);
                res.Append("\n");
            }
            for (int i = 0; i < linesToSort.Count; i++)
            {
                res.Append(linesToSort[i]);
                if (i < linesToSort.Count - 1) res.Append("\n");
            }

            string newSub = res.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);

            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        public bool FindNext(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            string text = PieceTable.GetText();
            int startSearch = Math.Max(0, CursorPosition);

            int idx = text.IndexOf(query, startSearch, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) idx = text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                SelectionStart = idx;
                SelectionEnd = idx + query.Length;
                CursorPosition = SelectionEnd;
                return true;
            }
            return false;
        }

        public void ReplaceCurrent(string query, string replacement)
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                string full = PieceTable.GetText();
                if (min >= 0 && min + len <= full.Length)
                {
                    string selectedStr = full.Substring(min, len);
                    if (selectedStr.Equals(query, StringComparison.OrdinalIgnoreCase))
                    {
                        PieceTable.Remove(min, len);
                        PieceTable.Insert(min, replacement);
                        SelectionStart = min;
                        SelectionEnd = min + replacement.Length;
                        CursorPosition = SelectionEnd;
                    }
                }
            }
            FindNext(query);
        }

        public int ReplaceAll(string query, string replacement)
        {
            if (string.IsNullOrEmpty(query)) return 0;
            string text = PieceTable.GetText();
            int count = 0;

            while (text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                PieceTable.Remove(idx, query.Length);
                PieceTable.Insert(idx, replacement);
                count++;
                text = PieceTable.GetText();
            }

            CursorPosition = 0;
            SelectionStart = 0;
            SelectionEnd = 0;
            return count;
        }

        public void PerformNavSearch()
        {
            NavSearchResults.Clear();
            NavHeadings.Clear();
            NavPageThumbnails.Clear();

            string fullText = PieceTable.GetText();
            string[] paragraphs = fullText.Split('\n');

            // 1. Scan Headings in document
            int runningChar = 0;
            for (int p = 0; p < paragraphs.Length; p++)
            {
                string pStr = paragraphs[p];
                string trimmed = pStr.Trim();
                int pg = Math.Max(1, (runningChar / 1800) + 1);

                bool isHeading = false;
                int level = 1;
                string title = trimmed;

                if (trimmed.StartsWith("#"))
                {
                    isHeading = true;
                    if (trimmed.StartsWith("###")) { level = 3; title = trimmed.TrimStart('#').Trim(); }
                    else if (trimmed.StartsWith("##")) { level = 2; title = trimmed.TrimStart('#').Trim(); }
                    else { level = 1; title = trimmed.TrimStart('#').Trim(); }
                }
                else if (trimmed.StartsWith("PHẦN ") || trimmed.StartsWith("CHƯƠNG ") || trimmed.StartsWith("BÀI ") || trimmed.StartsWith("MỤC "))
                {
                    isHeading = true;
                    level = 1;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^(\d+(\.\d+)*\.|[I|V|X]+\.)\s*"))
                {
                    isHeading = true;
                    int dotCount = 0;
                    for (int d = 0; d < trimmed.Length && trimmed[d] != ' '; d++) if (trimmed[d] == '.') dotCount++;
                    level = Math.Max(1, Math.Min(3, dotCount));
                }
                else
                {
                    FormattedRun fr = GetFormatAt(runningChar);
                    if (fr.Bold && trimmed.Length > 0 && trimmed.Length < 100 && !trimmed.StartsWith("|") && !trimmed.StartsWith("!["))
                    {
                        isHeading = true;
                        level = fr.FontSize >= 13.0f ? 1 : 2;
                    }
                }

                if (isHeading && !string.IsNullOrEmpty(title))
                {
                    NavHeadings.Add(new NavHeadingItem
                    {
                        CharIndex = runningChar,
                        Level = level,
                        Title = title,
                        PageNumber = pg,
                        ClickRect = Rectangle.Empty
                    });
                }

                runningChar += pStr.Length + 1;
            }

            // 2. Scan Search Query Matches
            if (!string.IsNullOrEmpty(NavSearchQuery))
            {
                int qLen = NavSearchQuery.Length;
                int searchIdx = 0;
                while (searchIdx < fullText.Length)
                {
                    int found = fullText.IndexOf(NavSearchQuery, searchIdx, StringComparison.OrdinalIgnoreCase);
                    if (found < 0) break;

                    int matchPg = 1;
                    foreach (var line in RenderedLines)
                    {
                        if (found >= line.StartCharIndex && found <= line.StartCharIndex + line.Length)
                        {
                            matchPg = Math.Max(1, (line.ScreenY - 195 + (int)ScrollY) / (int)(842 * ZoomLevel + 30) + 1);
                            break;
                        }
                    }

                    int sBefore = Math.Max(0, found - 25);
                    int sBeforeLen = found - sBefore;
                    string snipBefore = (sBefore > 0 ? "..." : "") + fullText.Substring(sBefore, sBeforeLen).Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');

                    string matchTxt = fullText.Substring(found, qLen);

                    int sAfter = found + qLen;
                    int sAfterLen = Math.Min(35, fullText.Length - sAfter);
                    string snipAfter = fullText.Substring(sAfter, sAfterLen).Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ') + (sAfter + sAfterLen < fullText.Length ? "..." : "");

                    NavSearchResults.Add(new NavSearchResultItem
                    {
                        CharIndex = found,
                        Length = qLen,
                        PageNumber = matchPg,
                        SnippetBefore = snipBefore,
                        MatchText = matchTxt,
                        SnippetAfter = snipAfter,
                        ClickRect = Rectangle.Empty
                    });

                    searchIdx = found + Math.Max(1, qLen);
                }
            }

            if (NavSearchResults.Count > 0)
            {
                if (NavSearchResultIndex < 0 || NavSearchResultIndex >= NavSearchResults.Count)
                    NavSearchResultIndex = 0;
            }
            else
            {
                NavSearchResultIndex = -1;
            }
        }

        public void JumpToNavMatch(int index)
        {
            if (index < 0 || index >= NavSearchResults.Count) return;
            NavSearchResultIndex = index;
            var item = NavSearchResults[index];

            SelectionStart = item.CharIndex;
            SelectionEnd = item.CharIndex + item.Length;
            CursorPosition = item.CharIndex;

            foreach (var line in RenderedLines)
            {
                if (item.CharIndex >= line.StartCharIndex && item.CharIndex <= line.StartCharIndex + line.Length)
                {
                    int docY = line.ScreenY - 195 + (int)ScrollY;
                    TargetScrollY = Math.Max(0, docY - 250);
                    UpdateSmoothScroll();
                    break;
                }
            }
        }

        public void SelectNextNavMatch()
        {
            if (NavSearchResults.Count == 0) return;
            int nextIdx = (NavSearchResultIndex + 1) % NavSearchResults.Count;
            JumpToNavMatch(nextIdx);
        }

        public void SelectPrevNavMatch()
        {
            if (NavSearchResults.Count == 0) return;
            int prevIdx = (NavSearchResultIndex - 1 + NavSearchResults.Count) % NavSearchResults.Count;
            JumpToNavMatch(prevIdx);
        }

        public void JumpToNavHeading(int charIndex)
        {
            CursorPosition = charIndex;
            SelectionStart = charIndex;
            SelectionEnd = charIndex;

            foreach (var line in RenderedLines)
            {
                if (charIndex >= line.StartCharIndex && charIndex <= line.StartCharIndex + line.Length)
                {
                    int docY = line.ScreenY - 195 + (int)ScrollY;
                    TargetScrollY = Math.Max(0, docY - 200);
                    UpdateSmoothScroll();
                    break;
                }
            }
        }

        public void JumpToNavPage(int pageNumber)
        {
            int stdH = 842;
            int pageH = (int)(stdH * ZoomLevel);
            TargetScrollY = Math.Max(0, (pageNumber - 1) * (pageH + 30));
            UpdateSmoothScroll();
        }

        public bool HandleNavMouseDown(int x, int y, int canvasW, int canvasH)
        {
            if (!ShowNavigationPane || x > 280 || y < 155 || y > canvasH - 26)
            {
                NavSearchFocused = false;
                return false;
            }

            // 1. Close Button ✕
            if (NavCloseBtnRect.Contains(x, y))
            {
                ShowNavigationPane = false;
                NavSearchFocused = false;
                return true;
            }

            // 2. Search Box
            if (NavSearchBoxRect.Contains(x, y))
            {
                if (NavClearQueryBtnRect.Contains(x, y) && !string.IsNullOrEmpty(NavSearchQuery))
                {
                    NavSearchQuery = "";
                    PerformNavSearch();
                }
                else
                {
                    NavSearchFocused = true;
                }
                return true;
            }

            NavSearchFocused = false;

            // 3. Tab Navigation (Headings, Pages, Results)
            if (NavTabHeadingsRect.Contains(x, y))
            {
                NavActiveTab = 0;
                PerformNavSearch();
                return true;
            }
            if (NavTabPagesRect.Contains(x, y))
            {
                NavActiveTab = 1;
                return true;
            }
            if (NavTabResultsRect.Contains(x, y))
            {
                NavActiveTab = 2;
                return true;
            }

            // 4. Previous / Next Match Buttons
            if (NavPrevMatchBtnRect.Contains(x, y))
            {
                SelectPrevNavMatch();
                return true;
            }
            if (NavNextMatchBtnRect.Contains(x, y))
            {
                SelectNextNavMatch();
                return true;
            }

            // 5. Results List Items (Tab 2)
            if (NavActiveTab == 2)
            {
                for (int i = 0; i < NavSearchResults.Count; i++)
                {
                    if (NavSearchResults[i].ClickRect.Contains(x, y))
                    {
                        JumpToNavMatch(i);
                        return true;
                    }
                }
            }

            // 6. Pages Thumbnail Cards (Tab 1)
            if (NavActiveTab == 1)
            {
                for (int i = 0; i < NavPageThumbnails.Count; i++)
                {
                    if (NavPageThumbnails[i].ClickRect.Contains(x, y))
                    {
                        JumpToNavPage(NavPageThumbnails[i].PageNumber);
                        return true;
                    }
                }
            }

            // 7. Headings Items (Tab 0)
            if (NavActiveTab == 0)
            {
                for (int i = 0; i < NavHeadings.Count; i++)
                {
                    if (NavHeadings[i].ClickRect.Contains(x, y))
                    {
                        JumpToNavHeading(NavHeadings[i].CharIndex);
                        return true;
                    }
                }
            }

            return true;
        }

        public void DrawNavigationPane(Graphics g, int width, int height, RibbonBar ribbon)
        {
            int paneW = 280;
            int paneTop = 155;
            int paneH = height - 155 - 26;
            if (paneH <= 50) return;

            Rectangle paneRect = new Rectangle(0, paneTop, paneW, paneH);

            // 1. Background and Right Border
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(251, 251, 251)))
            using (Pen borderPen = new Pen(Color.FromArgb(225, 223, 221), 1.0f))
            {
                g.FillRectangle(bgBrush, paneRect);
                g.DrawLine(borderPen, paneW, paneTop, paneW, paneTop + paneH);
            }

            // 2. Header Bar: Title "Điều hướng", Chevron ▾, and Close ✕
            using (Font titleFont = new Font("Segoe UI", 13.5f, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(16, 110, 190))) // Word Signature Blue
            using (Font iconFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush iconBrush = new SolidBrush(Color.FromArgb(96, 94, 92)))
            {
                g.DrawString("Điều hướng", titleFont, titleBrush, 14, paneTop + 12);
                g.DrawString("▾", iconFont, iconBrush, paneW - 48, paneTop + 16);

                NavCloseBtnRect = new Rectangle(paneW - 28, paneTop + 14, 18, 18);
                g.DrawString("✕", iconFont, iconBrush, NavCloseBtnRect.X + 2, NavCloseBtnRect.Y);
            }

            // 3. Search Box
            NavSearchBoxRect = new Rectangle(14, paneTop + 48, paneW - 28, 26);
            using (SolidBrush boxBg = new SolidBrush(Color.White))
            using (Pen boxBorder = new Pen(NavSearchFocused ? Color.FromArgb(0, 90, 158) : Color.FromArgb(138, 136, 134), NavSearchFocused ? 1.5f : 1.0f))
            using (Font searchFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(32, 31, 30)))
            using (SolidBrush placeholderBrush = new SolidBrush(Color.FromArgb(138, 136, 134)))
            {
                g.FillRectangle(boxBg, NavSearchBoxRect);
                g.DrawRectangle(boxBorder, NavSearchBoxRect);

                if (string.IsNullOrEmpty(NavSearchQuery))
                {
                    if (!NavSearchFocused)
                    {
                        g.DrawString("Tìm kiếm tài liệu...", searchFont, placeholderBrush, 20, NavSearchBoxRect.Y + 4);
                    }
                    else
                    {
                        if (DateTime.Now.Millisecond % 1000 < 500)
                        {
                            using (Pen caretPen = new Pen(Color.FromArgb(0, 90, 158), 1.5f))
                            {
                                g.DrawLine(caretPen, 20, NavSearchBoxRect.Y + 4, 20, NavSearchBoxRect.Y + 22);
                            }
                        }
                    }
                }
                else
                {
                    g.DrawString(NavSearchQuery, searchFont, textBrush, 20, NavSearchBoxRect.Y + 4);
                    if (NavSearchFocused && (DateTime.Now.Millisecond % 1000 < 500))
                    {
                        SizeF qSz = g.MeasureString(NavSearchQuery, searchFont);
                        using (Pen caretPen = new Pen(Color.FromArgb(0, 90, 158), 1.5f))
                        {
                            g.DrawLine(caretPen, 20 + qSz.Width, NavSearchBoxRect.Y + 4, 20 + qSz.Width, NavSearchBoxRect.Y + 22);
                        }
                    }

                    // Clear ✕ Button
                    NavClearQueryBtnRect = new Rectangle(NavSearchBoxRect.Right - 36, NavSearchBoxRect.Y + 4, 16, 16);
                    using (Font clrFont = new Font("Segoe UI", 8.0f))
                    {
                        g.DrawString("✕", clrFont, placeholderBrush, NavClearQueryBtnRect.X + 2, NavClearQueryBtnRect.Y);
                    }
                }

                // Magnifier Icon 🔍▾
                using (Font magFont = new Font("Segoe UI Symbol", 9.0f))
                {
                    g.DrawString("🔍▾", magFont, placeholderBrush, NavSearchBoxRect.Right - 22, NavSearchBoxRect.Y + 3);
                }
            }

            // 4. Tab Navigation (Tiêu đề | Trang | Kết quả)
            int tabY = paneTop + 84;
            NavTabHeadingsRect = new Rectangle(14, tabY, 60, 26);
            NavTabPagesRect = new Rectangle(82, tabY, 48, 26);
            NavTabResultsRect = new Rectangle(138, tabY, 68, 26);

            using (Font activeTabFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font tabFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush activeBlue = new SolidBrush(Color.FromArgb(0, 90, 158)))
            using (SolidBrush inactiveText = new SolidBrush(Color.FromArgb(96, 94, 92)))
            {
                // Tab 0: Tiêu đề
                g.DrawString("Tiêu đề", NavActiveTab == 0 ? activeTabFont : tabFont, NavActiveTab == 0 ? activeBlue : inactiveText, NavTabHeadingsRect.X, NavTabHeadingsRect.Y + 3);
                if (NavActiveTab == 0) g.FillRectangle(activeBlue, NavTabHeadingsRect.X, NavTabHeadingsRect.Y + 24, NavTabHeadingsRect.Width - 4, 3);

                // Tab 1: Trang
                g.DrawString("Trang", NavActiveTab == 1 ? activeTabFont : tabFont, NavActiveTab == 1 ? activeBlue : inactiveText, NavTabPagesRect.X, NavTabPagesRect.Y + 3);
                if (NavActiveTab == 1) g.FillRectangle(activeBlue, NavTabPagesRect.X, NavTabPagesRect.Y + 24, NavTabPagesRect.Width - 4, 3);

                // Tab 2: Kết quả
                g.DrawString("Kết quả", NavActiveTab == 2 ? activeTabFont : tabFont, NavActiveTab == 2 ? activeBlue : inactiveText, NavTabResultsRect.X, NavTabResultsRect.Y + 3);
                if (NavActiveTab == 2) g.FillRectangle(activeBlue, NavTabResultsRect.X, NavTabResultsRect.Y + 24, NavTabResultsRect.Width - 4, 3);
            }

            // 5. Tab Content Area
            int contentY = paneTop + 120;
            int availContentH = paneH - 125;
            Rectangle contentClip = new Rectangle(0, contentY, paneW, availContentH);

            var oldClip = g.Clip;
            g.SetClip(contentClip);

            try
            {
                if (NavActiveTab == 2) // Kết quả (Results)
                {
                    if (string.IsNullOrEmpty(NavSearchQuery))
                    {
                        // Help / Guide prompt in Vietnamese
                        using (Font guideFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                        using (SolidBrush guideBrush = new SolidBrush(Color.FromArgb(96, 94, 92)))
                        {
                            Rectangle guideRect1 = new Rectangle(14, contentY + 10, paneW - 28, 60);
                            g.DrawString("Văn bản, hình ảnh, chú thích... Có thể tìm kiếm hầu như mọi thứ trong tài liệu.", guideFont, guideBrush, guideRect1);

                            Rectangle guideRect2 = new Rectangle(14, contentY + 75, paneW - 28, 60);
                            g.DrawString("Dùng hộp tìm kiếm để nhập văn bản hoặc kính lúp để chọn nội dung khác.", guideFont, guideBrush, guideRect2);
                        }
                    }
                    else
                    {
                        // Summary header: "{N} kết quả" with Next/Prev buttons
                        using (Font countFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                        using (Font btnFont = new Font("Segoe UI Symbol", 8.0f))
                        using (SolidBrush cBrush = new SolidBrush(Color.FromArgb(50, 49, 48)))
                        using (Pen btnPen = new Pen(Color.FromArgb(209, 208, 207)))
                        {
                            string countStr = NavSearchResults.Count + " kết quả";
                            g.DrawString(countStr, countFont, cBrush, 14, contentY + 2);

                            NavPrevMatchBtnRect = new Rectangle(paneW - 58, contentY, 20, 20);
                            NavNextMatchBtnRect = new Rectangle(paneW - 34, contentY, 20, 20);

                            g.FillRectangle(Brushes.White, NavPrevMatchBtnRect);
                            g.DrawRectangle(btnPen, NavPrevMatchBtnRect);
                            g.DrawString("▲", btnFont, cBrush, NavPrevMatchBtnRect.X + 3, NavPrevMatchBtnRect.Y + 2);

                            g.FillRectangle(Brushes.White, NavNextMatchBtnRect);
                            g.DrawRectangle(btnPen, NavNextMatchBtnRect);
                            g.DrawString("▼", btnFont, cBrush, NavNextMatchBtnRect.X + 3, NavNextMatchBtnRect.Y + 2);
                        }

                        // Render result cards
                        int cardY = contentY + 28;
                        for (int i = 0; i < NavSearchResults.Count; i++)
                        {
                            var res = NavSearchResults[i];
                            Rectangle cardRect = new Rectangle(14, cardY, paneW - 28, 56);
                            res.ClickRect = cardRect;
                            NavSearchResults[i] = res;

                            bool isSelected = (i == NavSearchResultIndex);
                            if (isSelected)
                            {
                                using (SolidBrush selBg = new SolidBrush(Color.FromArgb(237, 235, 233)))
                                using (SolidBrush accentBar = new SolidBrush(Color.FromArgb(0, 90, 158)))
                                {
                                    g.FillRectangle(selBg, cardRect);
                                    g.FillRectangle(accentBar, cardRect.X, cardRect.Y, 3, cardRect.Height);
                                }
                            }
                            else
                            {
                                using (Pen cardBorder = new Pen(Color.FromArgb(243, 242, 241)))
                                {
                                    g.DrawRectangle(cardBorder, cardRect);
                                }
                            }

                            // Page number badge
                            using (Font pgFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                            using (SolidBrush pgBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
                            {
                                g.DrawString("Trang " + res.PageNumber, pgFont, pgBrush, cardRect.Right - 50, cardRect.Y + 4);
                            }

                            // Snippet
                            using (Font snipFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                            using (Font matchFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                            using (SolidBrush snipBrush = new SolidBrush(Color.FromArgb(96, 94, 92)))
                            using (SolidBrush matchBrush = new SolidBrush(Color.FromArgb(0, 90, 158)))
                            using (SolidBrush hlBg = new SolidBrush(Color.FromArgb(255, 244, 206)))
                            {
                                float sX = cardRect.X + 8;
                                float sY = cardRect.Y + 18;

                                SizeF beforeSz = g.MeasureString(res.SnippetBefore, snipFont);
                                g.DrawString(res.SnippetBefore, snipFont, snipBrush, sX, sY);
                                sX += beforeSz.Width;

                                SizeF matchSz = g.MeasureString(res.MatchText, matchFont);
                                g.FillRectangle(hlBg, sX, sY, matchSz.Width, matchSz.Height);
                                g.DrawString(res.MatchText, matchFont, matchBrush, sX, sY);
                                sX += matchSz.Width;

                                g.DrawString(res.SnippetAfter, snipFont, snipBrush, sX, sY);
                            }

                            cardY += 62;
                        }
                    }
                }
                else if (NavActiveTab == 1) // Pages
                {
                    NavPageThumbnails.Clear();
                    int cardW = 105;
                    int cardH = 135;
                    int pX = 18;
                    int pY = contentY + 10;

                    for (int pg = 1; pg <= TotalPageCount; pg++)
                    {
                        Rectangle pageCardRect = new Rectangle(pX, pY, cardW, cardH);
                        NavPageThumbnails.Add(new NavPageThumbnailItem { PageNumber = pg, ClickRect = pageCardRect });

                        bool isCur = (pg == CurrentActivePage);

                        g.FillRectangle(Brushes.White, pageCardRect);
                        using (Pen cardPen = new Pen(isCur ? Color.FromArgb(0, 90, 158) : Color.FromArgb(209, 208, 207), isCur ? 2.0f : 1.0f))
                        {
                            g.DrawRectangle(cardPen, pageCardRect);
                        }

                        // Mini text sketch lines inside page thumbnail
                        using (Pen linePen = new Pen(Color.FromArgb(220, 220, 220), 2))
                        {
                            for (int l = 0; l < 6; l++)
                            {
                                int lY = pageCardRect.Y + 15 + l * 14;
                                int lW = (l % 2 == 0) ? cardW - 30 : cardW - 45;
                                g.DrawLine(linePen, pageCardRect.X + 15, lY, pageCardRect.X + 15 + lW, lY);
                            }
                        }

                        // Page label
                        using (Font pgFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                        using (SolidBrush pBrush = new SolidBrush(Color.FromArgb(50, 49, 48)))
                        {
                            g.DrawString(pg.ToString(), pgFont, pBrush, pageCardRect.X + (cardW / 2) - 4, pageCardRect.Bottom + 4);
                        }

                        pX += cardW + 18;
                        if (pX + cardW > paneW)
                        {
                            pX = 18;
                            pY += cardH + 28;
                        }
                    }
                }
                else if (NavActiveTab == 0) // Headings
                {
                    if (NavHeadings.Count == 0)
                    {
                        using (Font guideFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                        using (SolidBrush guideBrush = new SolidBrush(Color.FromArgb(96, 94, 92)))
                        {
                            Rectangle guideRect = new Rectangle(14, contentY + 10, paneW - 28, 100);
                            g.DrawString("Tài liệu chưa có tiêu đề (Headings).\n\nBạn có thể áp dụng các kiểu Tiêu đề (Heading 1, Heading 2...) từ thanh Ribbon để tạo mục lục tự động.", guideFont, guideBrush, guideRect);
                        }
                    }
                    else
                    {
                        int hY = contentY + 5;
                        using (Font hFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                        using (Font hBoldFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                        using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(32, 31, 30)))
                        using (SolidBrush arrBrush = new SolidBrush(Color.FromArgb(96, 94, 92)))
                        {
                            for (int i = 0; i < NavHeadings.Count; i++)
                            {
                                var hItem = NavHeadings[i];
                                int indent = (hItem.Level - 1) * 14;
                                Rectangle hRect = new Rectangle(14 + indent, hY, paneW - 28 - indent, 26);
                                hItem.ClickRect = hRect;
                                NavHeadings[i] = hItem;

                                g.DrawString("▶", hFont, arrBrush, hRect.X, hRect.Y + 3);
                                g.DrawString(hItem.Title, hItem.Level == 1 ? hBoldFont : hFont, hBrush, hRect.X + 16, hRect.Y + 3);

                                hY += 28;
                            }
                        }
                    }
                }
            }
            finally
            {
                g.Clip = oldClip;
            }
        }

        // ==================== MS WORD SELECTION PANE (NGĂN LỰA CHỌN) ====================
        public string GetShapeDisplayName(DocShapeObject shape)
        {
            if (shape == null) return "Hình vẽ";
            if (!string.IsNullOrEmpty(shape.CustomName)) return shape.CustomName;
            int idx = DocumentShapes.IndexOf(shape) + 1;
            if (shape.ShapeType == "Image") return "Hình ảnh " + idx;
            if (shape.ShapeType == "TextBox") return "Hộp văn bản " + idx;
            if (shape.ShapeType == "Rectangle") return "Hình chữ nhật " + idx;
            if (shape.ShapeType == "RoundedRectangle") return "Chữ nhật bo tròn " + idx;
            if (shape.ShapeType == "Oval") return "Hình oval " + idx;
            if (shape.ShapeType == "RightArrow") return "Mũi tên " + idx;
            if (shape.ShapeType == "Heart") return "Hình trái tim " + idx;
            if (shape.ShapeType == "Star5") return "Ngôi sao " + idx;
            if (shape.ShapeType == "SmileFace") return "Mặt cười " + idx;
            return shape.ShapeType + " " + idx;
        }

        public string GetChartDisplayName(DocChartObject chart)
        {
            if (chart == null) return "Biểu đồ";
            if (!string.IsNullOrEmpty(chart.CustomName)) return chart.CustomName;
            int idx = DocumentCharts.IndexOf(chart) + 1;
            return "Biểu đồ " + idx;
        }

        public void MoveShapeUp(DocShapeObject shape)
        {
            if (shape == null) return;
            int idx = DocumentShapes.IndexOf(shape);
            if (idx < DocumentShapes.Count - 1)
            {
                DocumentShapes.RemoveAt(idx);
                DocumentShapes.Insert(idx + 1, shape);
            }
        }

        public void MoveShapeDown(DocShapeObject shape)
        {
            if (shape == null) return;
            int idx = DocumentShapes.IndexOf(shape);
            if (idx > 0)
            {
                DocumentShapes.RemoveAt(idx);
                DocumentShapes.Insert(idx - 1, shape);
            }
        }

        public void MoveChartUp(DocChartObject chart)
        {
            if (chart == null) return;
            int idx = DocumentCharts.IndexOf(chart);
            if (idx < DocumentCharts.Count - 1)
            {
                DocumentCharts.RemoveAt(idx);
                DocumentCharts.Insert(idx + 1, chart);
            }
        }

        public void MoveChartDown(DocChartObject chart)
        {
            if (chart == null) return;
            int idx = DocumentCharts.IndexOf(chart);
            if (idx > 0)
            {
                DocumentCharts.RemoveAt(idx);
                DocumentCharts.Insert(idx - 1, chart);
            }
        }

        public void ShowAllObjects()
        {
            foreach (var sh in DocumentShapes) sh.Visible = true;
            foreach (var ch in DocumentCharts) ch.Visible = true;
        }

        public void HideAllObjects()
        {
            foreach (var sh in DocumentShapes) sh.Visible = false;
            foreach (var ch in DocumentCharts) ch.Visible = false;
        }

        public bool HandleSelectionPaneMouseDown(int x, int y, int canvasW, int canvasH)
        {
            int paneW = 260;
            int paneTop = 155;
            int paneH = canvasH - 155 - 26;
            int paneX = canvasW - paneW;

            if (!ShowSelectionPane || x < paneX || y < paneTop || y > canvasH - 26)
            {
                return false;
            }

            // 1. Nút Đóng ✕
            if (SelectionPaneCloseBtnRect.Contains(x, y))
            {
                ShowSelectionPane = false;
                return true;
            }

            // 2. Nút Hiện tất cả (Show All)
            if (SelectionPaneShowAllBtnRect.Contains(x, y))
            {
                ShowAllObjects();
                return true;
            }

            // 3. Nút Ẩn tất cả (Hide All)
            if (SelectionPaneHideAllBtnRect.Contains(x, y))
            {
                HideAllObjects();
                return true;
            }

            // 4. Nút ▲ Lên trước (Move Up / Forward)
            if (SelectionPaneMoveUpBtnRect.Contains(x, y))
            {
                if (ActiveShape != null) MoveShapeUp(ActiveShape);
                else if (ActiveChart != null) MoveChartUp(ActiveChart);
                return true;
            }

            // 5. Nút ▼ Về sau (Move Down / Backward)
            if (SelectionPaneMoveDownBtnRect.Contains(x, y))
            {
                if (ActiveShape != null) MoveShapeDown(ActiveShape);
                else if (ActiveChart != null) MoveChartDown(ActiveChart);
                return true;
            }

            // 6. Danh sách các đối tượng
            for (int i = 0; i < SelectionPaneItems.Count; i++)
            {
                var item = SelectionPaneItems[i];

                // Click vào icon con mắt 👁 -> Toggle Ẩn / Hiện
                if (item.EyeRect.Contains(x, y))
                {
                    if (item.TargetObject is DocShapeObject)
                    {
                        var s = (DocShapeObject)item.TargetObject;
                        s.Visible = !s.Visible;
                    }
                    else if (item.TargetObject is DocChartObject)
                    {
                        var c = (DocChartObject)item.TargetObject;
                        c.Visible = !c.Visible;
                    }
                    return true;
                }

                // Click vào hàng -> Chọn đối tượng tương ứng
                if (item.RowRect.Contains(x, y))
                {
                    foreach (var s in DocumentShapes) s.IsSelected = false;
                    foreach (var c in DocumentCharts) c.IsSelected = false;

                    if (item.TargetObject is DocShapeObject)
                    {
                        var s = (DocShapeObject)item.TargetObject;
                        ActiveShape = s;
                        s.IsSelected = true;
                        ActiveChart = null;
                    }
                    else if (item.TargetObject is DocChartObject)
                    {
                        var c = (DocChartObject)item.TargetObject;
                        ActiveChart = c;
                        c.IsSelected = true;
                        ActiveShape = null;
                    }
                    return true;
                }
            }

            return true;
        }

        public void HandleSelectionPaneMouseMove(int x, int y, int canvasW, int canvasH)
        {
            int paneW = 260;
            int paneX = canvasW - paneW;
            if (!ShowSelectionPane || x < paneX)
            {
                SelectionPaneHoverIndex = -1;
                return;
            }

            for (int i = 0; i < SelectionPaneItems.Count; i++)
            {
                if (SelectionPaneItems[i].RowRect.Contains(x, y))
                {
                    SelectionPaneHoverIndex = i;
                    return;
                }
            }
            SelectionPaneHoverIndex = -1;
        }

        public void HandleSelectionPaneMouseWheel(int delta)
        {
            int maxScroll = Math.Max(0, (DocumentShapes.Count + DocumentCharts.Count) * 28 - 200);
            SelectionPaneScrollY = Math.Max(0, Math.Min(maxScroll, SelectionPaneScrollY - (delta > 0 ? 30 : -30)));
        }

        public void DrawSelectionPane(Graphics g, int width, int height, RibbonBar ribbon)
        {
            int paneW = 260;
            int paneTop = 155;
            int paneH = height - 155 - 26;
            if (paneH <= 50) return;

            int paneX = width - paneW;
            Rectangle paneRect = new Rectangle(paneX, paneTop, paneW, paneH);

            // 1. Nền và Viền trái
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(248, 249, 250)))
            using (Pen borderPen = new Pen(Color.FromArgb(218, 220, 224), 1.0f))
            {
                g.FillRectangle(bgBrush, paneRect);
                g.DrawLine(borderPen, paneX, paneTop, paneX, paneTop + paneH);
            }

            // 2. Thanh tiêu đề: Title "Selection", Chevron ▾, and Close ✕
            using (Font titleFont = new Font("Segoe UI", 12.5f, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(16, 110, 190))) // Word Signature Blue
            using (Font iconFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush iconBrush = new SolidBrush(Color.FromArgb(96, 94, 92)))
            {
                g.DrawString("Selection", titleFont, titleBrush, paneX + 14, paneTop + 10);
                g.DrawString("▾", iconFont, iconBrush, paneX + 88, paneTop + 14);

                SelectionPaneCloseBtnRect = new Rectangle(paneX + paneW - 28, paneTop + 12, 20, 20);
                g.DrawString("✕", iconFont, iconBrush, SelectionPaneCloseBtnRect.X + 3, SelectionPaneCloseBtnRect.Y + 1);
            }

            // 3. Hàng nút thao tác: [Hiện tất cả] [Ẩn tất cả] [▲] [▼]
            SelectionPaneShowAllBtnRect = new Rectangle(paneX + 10, paneTop + 42, 74, 24);
            SelectionPaneHideAllBtnRect = new Rectangle(paneX + 88, paneTop + 42, 74, 24);
            SelectionPaneMoveUpBtnRect = new Rectangle(paneX + paneW - 54, paneTop + 42, 20, 24);
            SelectionPaneMoveDownBtnRect = new Rectangle(paneX + paneW - 30, paneTop + 42, 20, 24);

            using (Font btnFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Pen btnBorderPen = new Pen(Color.FromArgb(170, 170, 170), 1.0f))
            using (SolidBrush btnBg = new SolidBrush(Color.White))
            using (SolidBrush btnTextBrush = new SolidBrush(Color.FromArgb(32, 31, 30)))
            {
                // Show All
                g.FillRectangle(btnBg, SelectionPaneShowAllBtnRect);
                g.DrawRectangle(btnBorderPen, SelectionPaneShowAllBtnRect);
                g.DrawString("Hiện tất cả", btnFont, btnTextBrush, SelectionPaneShowAllBtnRect.X + 8, SelectionPaneShowAllBtnRect.Y + 4);

                // Hide All
                g.FillRectangle(btnBg, SelectionPaneHideAllBtnRect);
                g.DrawRectangle(btnBorderPen, SelectionPaneHideAllBtnRect);
                g.DrawString("Ẩn tất cả", btnFont, btnTextBrush, SelectionPaneHideAllBtnRect.X + 11, SelectionPaneHideAllBtnRect.Y + 4);

                // Move Up ▲
                g.FillRectangle(btnBg, SelectionPaneMoveUpBtnRect);
                g.DrawRectangle(btnBorderPen, SelectionPaneMoveUpBtnRect);
                g.DrawString("▲", btnFont, Brushes.DimGray, SelectionPaneMoveUpBtnRect.X + 3, SelectionPaneMoveUpBtnRect.Y + 4);

                // Move Down ▼
                g.FillRectangle(btnBg, SelectionPaneMoveDownBtnRect);
                g.DrawRectangle(btnBorderPen, SelectionPaneMoveDownBtnRect);
                g.DrawString("▼", btnFont, Brushes.DimGray, SelectionPaneMoveDownBtnRect.X + 3, SelectionPaneMoveDownBtnRect.Y + 4);
            }

            // 4. Danh sách các đối tượng hình vẽ (Shapes, Images, Charts)
            int listY = paneTop + 74;
            int availListH = paneH - 78;
            Rectangle listClip = new Rectangle(paneX, listY, paneW, availListH);

            var oldClip = g.Clip;
            g.SetClip(listClip);

            SelectionPaneItems.Clear();

            try
            {
                int totalObjects = DocumentShapes.Count + DocumentCharts.Count;
                if (totalObjects == 0)
                {
                    using (Font emptyFont = new Font("Segoe UI", 8.5f, FontStyle.Italic))
                    using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(128, 128, 128)))
                    {
                        Rectangle emptyRect = new Rectangle(paneX + 14, listY + 30, paneW - 28, 80);
                        g.DrawString("Tài liệu chưa có đối tượng hình vẽ nào.\n\n(Vào tab Chèn để thêm Hình ảnh, Hình dạng hoặc Biểu đồ)", emptyFont, emptyBrush, emptyRect);
                    }
                }
                else
                {
                    int rowIndex = 0;
                    using (Font regularFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                    using (Font boldFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    using (SolidBrush selRowBg = new SolidBrush(Color.FromArgb(204, 232, 255)))
                    using (Pen selRowBorder = new Pen(Color.FromArgb(120, 180, 240), 1.0f))
                    using (SolidBrush hoverRowBg = new SolidBrush(Color.FromArgb(238, 244, 250)))
                    using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(32, 31, 30)))
                    {
                        // 1. Shapes (Hiển thị theo thứ tự Z-order từ trên cùng xuống dưới)
                        for (int i = DocumentShapes.Count - 1; i >= 0; i--)
                        {
                            var sh = DocumentShapes[i];
                            int rowY = listY + rowIndex * 28 - SelectionPaneScrollY;
                            Rectangle rowRect = new Rectangle(paneX + 6, rowY, paneW - 12, 26);
                            Rectangle eyeRect = new Rectangle(paneX + paneW - 32, rowY + 3, 20, 20);

                            SelectionPaneItems.Add(new SelectionPaneItemBounds { TargetObject = sh, RowRect = rowRect, EyeRect = eyeRect });

                            if (sh.IsSelected)
                            {
                                g.FillRectangle(selRowBg, rowRect);
                                g.DrawRectangle(selRowBorder, rowRect);
                            }
                            else if (SelectionPaneHoverIndex == rowIndex)
                            {
                                g.FillRectangle(hoverRowBg, rowRect);
                            }

                            // Vẽ Mini Icon theo loại hình
                            DrawShapeMiniIcon(g, paneX + 12, rowY + 5, sh.ShapeType);

                            // Tên đối tượng
                            string name = GetShapeDisplayName(sh);
                            g.DrawString(name, sh.IsSelected ? boldFont : regularFont, textBrush, paneX + 32, rowY + 5);

                            // Icon con mắt 👁
                            DrawEyeVector(g, eyeRect.X, eyeRect.Y, sh.Visible);

                            rowIndex++;
                        }

                        // 2. Charts
                        for (int i = DocumentCharts.Count - 1; i >= 0; i--)
                        {
                            var ch = DocumentCharts[i];
                            int rowY = listY + rowIndex * 28 - SelectionPaneScrollY;
                            Rectangle rowRect = new Rectangle(paneX + 6, rowY, paneW - 12, 26);
                            Rectangle eyeRect = new Rectangle(paneX + paneW - 32, rowY + 3, 20, 20);

                            SelectionPaneItems.Add(new SelectionPaneItemBounds { TargetObject = ch, RowRect = rowRect, EyeRect = eyeRect });

                            if (ch.IsSelected)
                            {
                                g.FillRectangle(selRowBg, rowRect);
                                g.DrawRectangle(selRowBorder, rowRect);
                            }
                            else if (SelectionPaneHoverIndex == rowIndex)
                            {
                                g.FillRectangle(hoverRowBg, rowRect);
                            }

                            // Mini Chart Icon
                            DrawChartMiniIcon(g, paneX + 12, rowY + 5);

                            // Tên biểu đồ
                            string name = GetChartDisplayName(ch);
                            g.DrawString(name, ch.IsSelected ? boldFont : regularFont, textBrush, paneX + 32, rowY + 5);

                            // Icon con mắt 👁
                            DrawEyeVector(g, eyeRect.X, eyeRect.Y, ch.Visible);

                            rowIndex++;
                        }
                    }
                }
            }
            finally
            {
                g.Clip = oldClip;
            }
        }

        private void DrawShapeMiniIcon(Graphics g, int x, int y, string shapeType)
        {
            using (Pen iconPen = new Pen(Color.FromArgb(43, 87, 154), 1.2f))
            {
                if (shapeType == "Image")
                {
                    g.DrawRectangle(iconPen, x, y + 1, 14, 12);
                    using (SolidBrush dot = new SolidBrush(Color.FromArgb(245, 158, 11)))
                        g.FillEllipse(dot, x + 3, y + 3, 3, 3);
                    g.DrawLine(iconPen, x + 2, y + 11, x + 6, y + 7);
                    g.DrawLine(iconPen, x + 6, y + 7, x + 10, y + 11);
                }
                else if (shapeType == "TextBox")
                {
                    g.DrawRectangle(iconPen, x, y + 1, 14, 12);
                    using (Font f = new Font("Segoe UI", 7.0f, FontStyle.Bold))
                        g.DrawString("A", f, Brushes.Navy, x + 2, y);
                }
                else if (shapeType == "Oval")
                {
                    g.DrawEllipse(iconPen, x, y + 1, 14, 12);
                }
                else
                {
                    g.DrawRectangle(iconPen, x, y + 1, 14, 12);
                }
            }
        }

        private void DrawChartMiniIcon(Graphics g, int x, int y)
        {
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(43, 87, 154)))
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(217, 119, 6)))
            using (SolidBrush b3 = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                g.FillRectangle(b1, x + 1, y + 6, 3, 7);
                g.FillRectangle(b2, x + 5, y + 2, 3, 11);
                g.FillRectangle(b3, x + 9, y + 4, 3, 9);
            }
        }

        private void DrawEyeVector(Graphics g, int x, int y, bool isVisible)
        {
            using (Pen pen = new Pen(isVisible ? Color.FromArgb(70, 70, 70) : Color.FromArgb(160, 160, 160), 1.3f))
            {
                // Hình mắt (Almond shape)
                g.DrawArc(pen, x + 2, y + 5, 14, 9, 180, 180);
                g.DrawArc(pen, x + 2, y + 3, 14, 9, 0, 180);

                if (isVisible)
                {
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(43, 87, 154)))
                    {
                        g.FillEllipse(pb, x + 7, y + 6, 4, 4);
                    }
                }
                else
                {
                    using (Pen slashPen = new Pen(Color.FromArgb(220, 38, 38), 1.5f))
                    {
                        g.DrawLine(slashPen, x + 2, y + 4, x + 16, y + 14);
                    }
                }
            }
        }

        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();
        private static Font GetCachedFont(string fontName, float size, FontStyle style)
        {
            if (string.IsNullOrEmpty(fontName)) fontName = "Times New Roman";
            int sizeInt = (int)(size * 10.0f);
            string key = fontName + "_" + sizeInt + "_" + (int)style;
            Font f;
            if (!FontCache.TryGetValue(key, out f))
            {
                try
                {
                    f = new Font(fontName, size, style);
                }
                catch
                {
                    f = new Font("Segoe UI", size, style);
                }
                FontCache[key] = f;
            }
            return f;
        }

        private float MeasureSubstringWidth(Graphics g, string lineText, int lineStartIndex, int subStart, int subLen)
        {
            if (subLen <= 0 || string.IsNullOrEmpty(lineText)) return 0;

            float totalW = 0;
            int i = subStart;
            while (i < subStart + subLen && i < lineText.Length)
            {
                FormattedRun fmt = GetFormatAt(lineStartIndex + i);
                int runLen = 1;
                while (i + runLen < subStart + subLen && i + runLen < lineText.Length && GetFormatAt(lineStartIndex + i + runLen) == fmt)
                {
                    runLen++;
                }

                string runStr = lineText.Substring(i, runLen);
                FontStyle style = FontStyle.Regular;
                if (fmt.Bold) style |= FontStyle.Bold;
                if (fmt.Italic) style |= FontStyle.Italic;
                if (fmt.Underline) style |= FontStyle.Underline;
                if (fmt.Strike) style |= FontStyle.Strikeout;

                float effectiveFontSize = fmt.FontSize;
                if (fmt.Subscript || fmt.Superscript) effectiveFontSize = fmt.FontSize * 0.72f;

                Font runFont = GetCachedFont(fmt.FontName, effectiveFontSize * ZoomLevel, style);
                SizeF sz = MeasureStringWithTabs(g, runStr, runFont, ZoomLevel);
                totalW += sz.Width;

                i += runLen;
            }
            return totalW;
        }

        private void FillRoundedRectangle(Graphics g, Brush brush, RectangleF rect, float radius)
        {
            if (radius <= 0)
            {
                g.FillRectangle(brush, rect);
                return;
            }
            using (GraphicsPath path = new GraphicsPath())
            {
                float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        private void DrawRoundedRectangle(Graphics g, Pen pen, RectangleF rect, float radius)
        {
            if (radius <= 0)
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                return;
            }
            using (GraphicsPath path = new GraphicsPath())
            {
                float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }

        private struct WrappedLine
        {
            public string Text;
            public int StartIndex;
            public int Length;
            public bool IsLastInPara;
            public float MaxFontSize;
        }

        public struct WrapLineInfo
        {
            public string Text;
            public int StartIndex;
            public int Length;
            public float StartX;
            public float Width;
            public int LineY;
            public int LineHeight;
            public float MaxFontSize;
            public bool IsLastInPara;
        }

        public struct LineSlot
        {
            public float StartX;
            public float Width;
            public int LineY;
            public int LineHeight;
            public int PageIndex;
        }

        private List<LineSlot> GetLineSlotsForY(ref int currentY, int lineH, int textX, int maxTextW, int startPageY, ref int currentPage, int pageH, int marginY)
        {
            List<LineSlot> slots = new List<LineSlot>();

            // 1. Top and Bottom (WrapMode == 4): Nhảy hoàn toàn xuống dưới đáy của đối tượng
            bool jumped = true;
            while (jumped)
            {
                jumped = false;
                foreach (var sh in DocumentShapes)
                {
                    if (sh.WrapMode == 4 && !sh.LastBounds.IsEmpty)
                    {
                        if (currentY + lineH > sh.LastBounds.Top - 2 && currentY < sh.LastBounds.Bottom + 2)
                        {
                            currentY = sh.LastBounds.Bottom + Math.Max(6, (int)(10 * ZoomLevel));
                            jumped = true;
                            break;
                        }
                    }
                }
                foreach (var ch in DocumentCharts)
                {
                    if (ch.WrapMode == 4 && !ch.LastBounds.IsEmpty)
                    {
                        if (currentY + lineH > ch.LastBounds.Top - 2 && currentY < ch.LastBounds.Bottom + 2)
                        {
                            currentY = ch.LastBounds.Bottom + Math.Max(6, (int)(10 * ZoomLevel));
                            jumped = true;
                            break;
                        }
                    }
                }
                foreach (var kvp in TableFloatingBounds)
                {
                    int tIdx = kvp.Key;
                    int tWrap = TableWrapModes.ContainsKey(tIdx) ? TableWrapModes[tIdx] : 1;
                    if (tWrap == 4)
                    {
                        Rectangle tb = kvp.Value;
                        if (!tb.IsEmpty && currentY + lineH > tb.Top - 2 && currentY < tb.Bottom + 2)
                        {
                            currentY = tb.Bottom + Math.Max(6, (int)(10 * ZoomLevel));
                            jumped = true;
                            break;
                        }
                    }
                }
            }

            if (currentY + lineH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
            {
                currentPage++;
                currentY = startPageY + currentPage * (pageH + 30) + marginY;
            }

            // 2. Square (1), Tight (2), Through (3): Tính toán các khe trống Trái & Phải
            Rectangle sideObstacle = Rectangle.Empty;
            int gap = Math.Max(12, (int)(16 * ZoomLevel));

            foreach (var sh in DocumentShapes)
            {
                if ((sh.WrapMode == 1 || sh.WrapMode == 2 || sh.WrapMode == 3) && !sh.LastBounds.IsEmpty)
                {
                    if (currentY + lineH > sh.LastBounds.Top - 2 && currentY < sh.LastBounds.Bottom + 2)
                    {
                        sideObstacle = sh.LastBounds;
                        break;
                    }
                }
            }
            if (sideObstacle.IsEmpty)
            {
                foreach (var ch in DocumentCharts)
                {
                    if ((ch.WrapMode == 1 || ch.WrapMode == 2 || ch.WrapMode == 3) && !ch.LastBounds.IsEmpty)
                    {
                        if (currentY + lineH > ch.LastBounds.Top - 2 && currentY < ch.LastBounds.Bottom + 2)
                        {
                            sideObstacle = ch.LastBounds;
                            break;
                        }
                    }
                }
            }
            if (sideObstacle.IsEmpty)
            {
                foreach (var kvp in TableFloatingBounds)
                {
                    int tIdx = kvp.Key;
                    int tWrap = TableWrapModes.ContainsKey(tIdx) ? TableWrapModes[tIdx] : 1;
                    if (tWrap == 1 || tWrap == 2 || tWrap == 3)
                    {
                        Rectangle tb = kvp.Value;
                        if (!tb.IsEmpty && currentY + lineH > tb.Top - 2 && currentY < tb.Bottom + 2)
                        {
                            sideObstacle = tb;
                            break;
                        }
                    }
                }
            }

            if (sideObstacle.IsEmpty)
            {
                slots.Add(new LineSlot { StartX = textX, Width = maxTextW, LineY = currentY, LineHeight = lineH, PageIndex = currentPage });
                return slots;
            }

            int minUsable = (int)(45 * ZoomLevel);
            int leftW = sideObstacle.Left - textX - gap;
            int rightStart = sideObstacle.Right + gap;
            int rightW = (textX + maxTextW) - rightStart;

            if (leftW >= minUsable)
            {
                slots.Add(new LineSlot { StartX = textX, Width = leftW, LineY = currentY, LineHeight = lineH, PageIndex = currentPage });
            }
            if (rightW >= minUsable)
            {
                slots.Add(new LineSlot { StartX = rightStart, Width = rightW, LineY = currentY, LineHeight = lineH, PageIndex = currentPage });
            }

            if (slots.Count == 0)
            {
                currentY = sideObstacle.Bottom + gap;
                if (currentY + lineH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                {
                    currentPage++;
                    currentY = startPageY + currentPage * (pageH + 30) + marginY;
                }
                slots.Add(new LineSlot { StartX = textX, Width = maxTextW, LineY = currentY, LineHeight = lineH, PageIndex = currentPage });
            }

            return slots;
        }

        private List<WrapLineInfo> LayoutParagraphLines(string para, int paraStartIndex, ref int currentY, ref int currentPage, int startPageY, int pageH, int marginY, int textX, int maxTextW, Graphics g, RibbonBar ribbon)
        {
            float pBefore = (ParagraphSpacingsBefore.ContainsKey(paraStartIndex)) ? ParagraphSpacingsBefore[paraStartIndex] : (ribbon != null ? ribbon.ParagraphSpacingBefore : 0f);
            if (pBefore > 0)
            {
                currentY += (int)(pBefore * ZoomLevel);
            }

            float pLineSpacing = (ParagraphLineSpacings.ContainsKey(paraStartIndex)) ? ParagraphLineSpacings[paraStartIndex] : (ribbon != null ? ribbon.LineSpacingFactor : 1.15f);
            float pAfter = (ParagraphSpacingsAfter.ContainsKey(paraStartIndex)) ? ParagraphSpacingsAfter[paraStartIndex] : (ribbon != null ? ribbon.ParagraphSpacingAfter : 0f);

            float pFirstLine = GetFirstLineIndent(paraStartIndex);
            float pHanging = GetHangingIndent(paraStartIndex);
            float pLeft = GetLeftIndent(paraStartIndex);
            float pRight = GetRightIndent(paraStartIndex);

            List<WrapLineInfo> result = new List<WrapLineInfo>();
            if (string.IsNullOrEmpty(para))
            {
                int lH = (int)(Math.Max(26.0f * ZoomLevel, TypingFontSize * 1.4f * ZoomLevel) * pLineSpacing);
                int effStartX = textX + (int)((pLeft + pFirstLine) * ZoomLevel);
                int effMaxW = Math.Max(40, maxTextW - (int)((pLeft + pFirstLine + pRight) * ZoomLevel));
                var slots = GetLineSlotsForY(ref currentY, lH, effStartX, effMaxW, startPageY, ref currentPage, pageH, marginY);
                var slot = slots[0];

                result.Add(new WrapLineInfo
                {
                    Text = "",
                    StartIndex = paraStartIndex,
                    Length = 0,
                    StartX = slot.StartX,
                    Width = slot.Width,
                    LineY = currentY,
                    LineHeight = lH,
                    MaxFontSize = TypingFontSize,
                    IsLastInPara = true
                });
                currentY += lH;
                if (pAfter > 0)
                {
                    currentY += (int)(pAfter * ZoomLevel);
                }
                return result;
            }

            // 1. Fast tokenization into word tokens with exact character offsets
            List<string> tokens = new List<string>();
            List<int> tokenStarts = new List<int>();
            int pPos = 0;
            while (pPos < para.Length)
            {
                int tStart = pPos;
                while (pPos < para.Length && para[pPos] != ' ' && para[pPos] != '\t')
                {
                    pPos++;
                }
                while (pPos < para.Length && (para[pPos] == ' ' || para[pPos] == '\t'))
                {
                    pPos++;
                }
                if (pPos == tStart) pPos++;
                tokens.Add(para.Substring(tStart, pPos - tStart));
                tokenStarts.Add(paraStartIndex + tStart);
            }

            int tokenIdx = 0;
            while (tokenIdx < tokens.Count)
            {
                // Find max font size in this line
                float lineMaxFS = 11.0f;
                int peekTokens = Math.Min(tokens.Count - tokenIdx, 10);
                for (int k = 0; k < peekTokens; k++)
                {
                    FormattedRun fr = GetFormatAt(tokenStarts[tokenIdx + k]);
                    if (fr.FontSize > lineMaxFS) lineMaxFS = fr.FontSize;
                }

                int lH = (int)(Math.Max(26.0f * ZoomLevel, lineMaxFS * 1.4f * ZoomLevel) * pLineSpacing);
                
                // First line uses (pLeft + pFirstLine), following lines use (pLeft + pHanging)
                int effStartX = (result.Count == 0) ? textX + (int)((pLeft + pFirstLine) * ZoomLevel) : textX + (int)((pLeft + pHanging) * ZoomLevel);
                int effMaxW = (result.Count == 0) ? Math.Max(40, maxTextW - (int)((pLeft + pFirstLine + pRight) * ZoomLevel)) : Math.Max(40, maxTextW - (int)((pLeft + pHanging + pRight) * ZoomLevel));

                var slots = GetLineSlotsForY(ref currentY, lH, effStartX, effMaxW, startPageY, ref currentPage, pageH, marginY);

                foreach (var slot in slots)
                {
                    if (tokenIdx >= tokens.Count) break;

                    int currentLineCharStart = tokenStarts[tokenIdx];
                    StringBuilder lineSb = new StringBuilder();
                    float curLineWidth = 0;
                    int firstTokenIdx = tokenIdx;

                    while (tokenIdx < tokens.Count)
                    {
                        string tok = tokens[tokenIdx];
                        float tokW = MeasureSubstringWidth(g, tok, tokenStarts[tokenIdx], 0, tok.Length);

                        if (curLineWidth + tokW > slot.Width && tokenIdx > firstTokenIdx)
                        {
                            break;
                        }

                        lineSb.Append(tok);
                        curLineWidth += tokW;
                        tokenIdx++;
                    }

                    string lineText = lineSb.ToString();
                    bool isLast = (tokenIdx >= tokens.Count);

                    result.Add(new WrapLineInfo
                    {
                        Text = lineText,
                        StartIndex = currentLineCharStart,
                        Length = lineText.Length,
                        StartX = slot.StartX,
                        Width = slot.Width,
                        LineY = slot.LineY,
                        LineHeight = lH,
                        MaxFontSize = lineMaxFS,
                        IsLastInPara = isLast
                    });
                }

                currentY += lH;
            }

            if (pAfter > 0)
            {
                currentY += (int)(pAfter * ZoomLevel);
            }

            return result;
        }

        private List<WrappedLine> WrapParagraph(string para, Graphics g, int maxWidth, int paraStartIndex)
        {
            List<WrappedLine> lines = new List<WrappedLine>();
            if (string.IsNullOrEmpty(para))
            {
                lines.Add(new WrappedLine { Text = "", StartIndex = paraStartIndex, Length = 0, IsLastInPara = true, MaxFontSize = TypingFontSize });
                return lines;
            }

            string[] words = para.Split(' ');
            string currentLine = "";
            int currentLineStart = paraStartIndex;
            float maxFS = 11.0f;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                int wordStart = currentLineStart + (string.IsNullOrEmpty(currentLine) ? 0 : currentLine.Length + 1);
                FormattedRun wordFormat = GetFormatAt(wordStart);
                maxFS = Math.Max(maxFS, wordFormat.FontSize);

                FontStyle style = FontStyle.Regular;
                if (wordFormat.Bold) style |= FontStyle.Bold;
                if (wordFormat.Italic) style |= FontStyle.Italic;

                using (Font wordFont = new Font(wordFormat.FontName, wordFormat.FontSize * ZoomLevel, style))
                {
                    string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    SizeF size = g.MeasureString(testLine, wordFont, Point.Empty, StringFormat.GenericTypographic);

                    if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(new WrappedLine { Text = currentLine, StartIndex = currentLineStart, Length = currentLine.Length, IsLastInPara = false, MaxFontSize = maxFS });
                        currentLineStart += currentLine.Length + 1;
                        currentLine = word;
                        maxFS = wordFormat.FontSize;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(new WrappedLine { Text = currentLine, StartIndex = currentLineStart, Length = currentLine.Length, IsLastInPara = true, MaxFontSize = maxFS });
            }

            return lines;
        }

        public void Copy()
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                string full = PieceTable.GetText();
                if (min >= 0 && min + len <= full.Length)
                {
                    string sub = full.Substring(min, len);
                    Clipboard.SetText(sub);
                }
            }
        }

        public void Cut()
        {
            if (SelectionStart != SelectionEnd)
            {
                Copy();
                DeleteSelection();
            }
        }

        public void GetPageLayoutMetrics(int canvasW, RibbonBar ribbon, out int pageX, out int pageW, out int pageH, out int marginX, out int rightMarginX, out int marginY)
        {
            int stdW = (ribbon != null && ribbon.IsLandscape) ? 1123 : 794; // Standard A4 (8.27" x 11.69" at 96 DPI)
            int stdH = (ribbon != null && ribbon.IsLandscape) ? 794 : 1123;
            if (ViewMode == 0) { stdW = 750; } // Read mode
            else if (ViewMode == 2) { stdW = Math.Max(700, (int)((canvasW - 80) / Math.Max(0.2f, ZoomLevel))); } // Web mode

            pageW = (int)(stdW * ZoomLevel);
            pageH = (int)(stdH * ZoomLevel);

            float leftMarginInches = ribbon != null ? ribbon.GetEffectiveLeftMarginInches() : 1.0f;
            float rightMarginInches = ribbon != null ? ribbon.GetEffectiveRightMarginInches() : 1.0f;
            float topMarginInches = ribbon != null ? ribbon.GetEffectiveTopMarginInches() : 1.0f;

            marginX = (int)(leftMarginInches * 96 * ZoomLevel);
            rightMarginX = (int)(rightMarginInches * 96 * ZoomLevel);
            marginY = (int)(topMarginInches * 96 * ZoomLevel);

            int navOffset = ShowNavigationPane ? 280 : 0;
            int selPaneOffset = ShowSelectionPane ? 260 : 0;
            int effectiveCanvasW = canvasW - navOffset - selPaneOffset;
            pageX = navOffset + Math.Max(40, (effectiveCanvasW - pageW) / 2);
        }

        public void DrawInteractiveRuler(Graphics g, int pageX, int pageW, int marginX, int rightMarginX, int canvasW, int canvasH, RibbonBar ribbon)
        {
            int rulerH = 24;
            int rulerY = 156; // Fixed directly below Ribbon (Y: 156..180), pinned at top, NOT moving with ScrollY!
            int curPStart = GetCurrentParagraphStart();

            // Background strip across the entire window behind ruler to hide any scrolled page content
            using (SolidBrush barBg = new SolidBrush(Color.FromArgb(240, 242, 245)))
            {
                g.FillRectangle(barBg, 0, rulerY, canvasW, rulerH);
            }
            using (Pen bottomLine = new Pen(Color.FromArgb(205, 210, 220)))
            {
                g.DrawLine(bottomLine, 0, rulerY + rulerH, canvasW, rulerY + rulerH);
            }

            // 1. Tab Selector Button on the Far Left Corner (Ô vuông chọn kiểu Tab Stop)
            int tabBtnW = 24;
            int tabBtnX = Math.Max(4, pageX - tabBtnW - 4);
            RulerTabSelectorRect = new Rectangle(tabBtnX, rulerY, tabBtnW, rulerH);

            using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.FillRectangle(btnBg, RulerTabSelectorRect);
            }
            using (Pen borderPen = new Pen(Color.FromArgb(190, 198, 210)))
            {
                g.DrawRectangle(borderPen, RulerTabSelectorRect);
            }

            // Draw Active Tab Stop Symbol inside the Tab Selector Button
            DrawTabGlyph(g, tabBtnX + tabBtnW / 2, rulerY + rulerH / 2, ActiveTabStopSelector, Color.FromArgb(30, 41, 59), 1.8f);

            // 2. Base Ruler Panel (Mặt thước ngang)
            Rectangle fullRulerRect = new Rectangle(pageX, rulerY, pageW, rulerH);
            int bodyW = pageW - marginX - rightMarginX;

            // 2.1. Left Margin Grey Zone
            using (SolidBrush greyBrush = new SolidBrush(Color.FromArgb(226, 230, 237)))
            {
                g.FillRectangle(greyBrush, pageX, rulerY, marginX, rulerH);
            }

            // 2.2. Middle Editable White Zone
            using (SolidBrush whiteBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(whiteBrush, pageX + marginX, rulerY, bodyW, rulerH);
            }

            // 2.3. Right Margin Grey Zone
            using (SolidBrush greyBrush = new SolidBrush(Color.FromArgb(226, 230, 237)))
            {
                g.FillRectangle(greyBrush, pageX + pageW - rightMarginX, rulerY, rightMarginX, rulerH);
            }

            // Border lines for Ruler
            using (Pen rBorderPen = new Pen(Color.FromArgb(195, 202, 214)))
            {
                g.DrawRectangle(rBorderPen, fullRulerRect);
                g.DrawLine(rBorderPen, pageX + marginX, rulerY, pageX + marginX, rulerY + rulerH);
                g.DrawLine(rBorderPen, pageX + pageW - rightMarginX, rulerY, pageX + pageW - rightMarginX, rulerY + rulerH);
            }

            // 3. Tick Marks & Numbers (Centimeters / Inches)
            using (Font numFont = new Font("Segoe UI", 7.0f, FontStyle.Regular))
            using (SolidBrush numBrush = new SolidBrush(Color.FromArgb(70, 80, 95)))
            using (Pen tickPen = new Pen(Color.FromArgb(135, 145, 160)))
            using (Pen minorTickPen = new Pen(Color.FromArgb(185, 192, 202)))
            {
                if (CurrentRulerUnit == RulerUnit.Centimeters)
                {
                    // 1 cm = pageW / 21.0f (since A4 width = 21.0 cm)
                    float pxPerCm = pageW / 21.0f;

                    // Positive numbers starting from 0 at left margin
                    int cmIndex = 1;
                    float curX = pageX + marginX + pxPerCm;
                    while (curX < pageX + pageW - 6)
                    {
                        // Minor ticks every 2mm
                        for (int sub = 1; sub <= 4; sub++)
                        {
                            float subX = curX - pxPerCm + sub * (pxPerCm / 5.0f);
                            if (subX >= pageX + marginX && subX < pageX + pageW)
                            {
                                int subH = (sub == 2 || sub == 3) ? 5 : 3;
                                g.DrawLine(minorTickPen, subX, rulerY + rulerH, subX, rulerY + rulerH - subH);
                            }
                        }

                        // Half-cm tick
                        float halfX = curX - pxPerCm * 0.5f;
                        if (halfX >= pageX + marginX && halfX < pageX + pageW)
                        {
                            g.DrawLine(tickPen, halfX, rulerY + rulerH, halfX, rulerY + rulerH - 6);
                        }

                        // Major cm tick
                        g.DrawLine(tickPen, curX, rulerY + rulerH, curX, rulerY + rulerH - 9);

                        // Number label
                        string numStr = cmIndex.ToString();
                        SizeF sz = g.MeasureString(numStr, numFont);
                        g.DrawString(numStr, numFont, numBrush, curX - sz.Width / 2f, rulerY + 3);

                        cmIndex++;
                        curX += pxPerCm;
                    }

                    // Negative ticks on left margin grey area
                    int negCm = 1;
                    float negX = pageX + marginX - pxPerCm;
                    while (negX > pageX + 6)
                    {
                        // Minor ticks
                        for (int sub = 1; sub <= 4; sub++)
                        {
                            float subX = negX + sub * (pxPerCm / 5.0f);
                            if (subX >= pageX && subX < pageX + marginX)
                            {
                                g.DrawLine(minorTickPen, subX, rulerY + rulerH, subX, rulerY + rulerH - 3);
                            }
                        }
                        g.DrawLine(tickPen, negX, rulerY + rulerH, negX, rulerY + rulerH - 8);
                        string numStr = negCm.ToString();
                        SizeF sz = g.MeasureString(numStr, numFont);
                        g.DrawString(numStr, numFont, numBrush, negX - sz.Width / 2f, rulerY + 3);

                        negCm++;
                        negX -= pxPerCm;
                    }
                }
                else
                {
                    // Inches Mode (A4 width = 8.27 inches)
                    float pxPerInch = pageW / 8.27f;
                    int inIndex = 1;
                    float curX = pageX + marginX + pxPerInch;
                    while (curX < pageX + pageW - 6)
                    {
                        // 1/4 inch ticks
                        for (int sub = 1; sub <= 3; sub++)
                        {
                            float subX = curX - pxPerInch + sub * (pxPerInch / 4.0f);
                            int subH = (sub == 2) ? 6 : 4;
                            g.DrawLine(minorTickPen, subX, rulerY + rulerH, subX, rulerY + rulerH - subH);
                        }
                        g.DrawLine(tickPen, curX, rulerY + rulerH, curX, rulerY + rulerH - 9);
                        string numStr = inIndex.ToString();
                        SizeF sz = g.MeasureString(numStr, numFont);
                        g.DrawString(numStr, numFont, numBrush, curX - sz.Width / 2f, rulerY + 3);
                        inIndex++;
                        curX += pxPerInch;
                    }
                }
            }

            // 4. Tab Stops Glyphs on the Ruler
            TabStopHitRects.Clear();
            var tabStops = GetTabStops(curPStart);
            if (tabStops != null)
            {
                for (int i = 0; i < tabStops.Count; i++)
                {
                    var ts = tabStops[i];
                    int tsX = pageX + marginX + (int)(ts.PositionPt * ZoomLevel);
                    Rectangle tsRect = new Rectangle(tsX - 5, rulerY + rulerH - 12, 10, 12);
                    TabStopHitRects.Add(tsRect);
                    DrawTabGlyph(g, tsX, rulerY + rulerH - 6, ts.Type, Color.FromArgb(15, 23, 42), 1.8f);
                }
            }

            // 5. The 4 Geometric Indentation Markers (4 Mấu Kéo Đoạn Văn)
            float pFirstLine = GetFirstLineIndent(curPStart) * ZoomLevel;
            float pHanging = GetHangingIndent(curPStart) * ZoomLevel;
            float pLeft = GetLeftIndent(curPStart) * ZoomLevel;
            float pRight = GetRightIndent(curPStart) * ZoomLevel;

            int leftBaseX = pageX + marginX + (int)pLeft;
            int flX = leftBaseX + (int)pFirstLine;
            int hgX = leftBaseX + (int)pHanging;
            int rX = pageX + pageW - rightMarginX - (int)pRight;

            // (1) ▼ First Line Indent (Mấu tam giác chúc xuống ở đỉnh trên bên trái)
            FirstLineIndentHitRect = new Rectangle(flX - 6, rulerY, 12, 10);
            PointF[] flTri = new PointF[]
            {
                new PointF(flX - 5f, rulerY + 1),
                new PointF(flX + 5f, rulerY + 1),
                new PointF(flX, rulerY + 9)
            };
            using (SolidBrush mBrush = new SolidBrush(Color.FromArgb(90, 100, 115)))
            using (Pen mPen = new Pen(Color.FromArgb(35, 45, 60), 1.2f))
            {
                g.FillPolygon(mBrush, flTri);
                g.DrawPolygon(mPen, flTri);
            }

            // (2) ▲ Hanging Indent (Mấu tam giác chúc lên ở giữa bên trái)
            HangingIndentHitRect = new Rectangle(hgX - 6, rulerY + 9, 12, 9);
            PointF[] hgTri = new PointF[]
            {
                new PointF(hgX, rulerY + 9),
                new PointF(hgX - 5f, rulerY + 17),
                new PointF(hgX + 5f, rulerY + 17)
            };
            using (SolidBrush mBrush = new SolidBrush(Color.FromArgb(90, 100, 115)))
            using (Pen mPen = new Pen(Color.FromArgb(35, 45, 60), 1.2f))
            {
                g.FillPolygon(mBrush, hgTri);
                g.DrawPolygon(mPen, hgTri);
            }

            // (3) ■ Left Indent (Khối hình chữ nhật ở đáy bên trái)
            LeftIndentHitRect = new Rectangle(hgX - 5, rulerY + 17, 10, 6);
            using (SolidBrush mBrush = new SolidBrush(Color.FromArgb(90, 100, 115)))
            using (Pen mPen = new Pen(Color.FromArgb(35, 45, 60), 1.2f))
            {
                g.FillRectangle(mBrush, LeftIndentHitRect);
                g.DrawRectangle(mPen, LeftIndentHitRect);
            }

            // (4) ▲ Right Indent (Mấu tam giác chúc lên bên phải)
            RightIndentHitRect = new Rectangle(rX - 6, rulerY + 8, 12, 15);
            PointF[] rTri = new PointF[]
            {
                new PointF(rX, rulerY + 8),
                new PointF(rX - 5f, rulerY + 22),
                new PointF(rX + 5f, rulerY + 22)
            };
            using (SolidBrush mBrush = new SolidBrush(Color.FromArgb(90, 100, 115)))
            using (Pen mPen = new Pen(Color.FromArgb(35, 45, 60), 1.2f))
            {
                g.FillPolygon(mBrush, rTri);
                g.DrawPolygon(mPen, rTri);
            }

            // 6. Vertical Alignment Guide Line while Dragging
            if (RulerGuideLineX >= 0)
            {
                using (Pen guidePen = new Pen(Color.FromArgb(180, 0, 120, 215), 1.0f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLine(guidePen, RulerGuideLineX, rulerY + rulerH, RulerGuideLineX, canvasH - 30);
                }
            }
        }

        private void DrawTabGlyph(Graphics g, float cx, float cy, TabStopType type, Color col, float penWidth)
        {
            using (Pen p = new Pen(col, penWidth))
            using (SolidBrush b = new SolidBrush(col))
            {
                switch (type)
                {
                    case TabStopType.Left: // L shape: vertical bar at cx, bottom foot extends right to cx + 5
                        g.DrawLine(p, cx, cy - 5, cx, cy + 3);
                        g.DrawLine(p, cx, cy + 3, cx + 5, cy + 3);
                        break;
                    case TabStopType.Center: // Inverted T (⊥): horizontal bar cx - 4 to cx + 4, vertical stem at cx
                        g.DrawLine(p, cx - 4, cy + 3, cx + 4, cy + 3);
                        g.DrawLine(p, cx, cy + 3, cx, cy - 5);
                        break;
                    case TabStopType.Right: // Reverse L (⅃): vertical bar at cx, bottom foot extends left to cx - 5
                        g.DrawLine(p, cx, cy - 5, cx, cy + 3);
                        g.DrawLine(p, cx, cy + 3, cx - 5, cy + 3);
                        break;
                    case TabStopType.Decimal: // Inverted T with dot (⊥·)
                        g.DrawLine(p, cx - 4, cy + 3, cx + 3, cy + 3);
                        g.DrawLine(p, cx, cy + 3, cx, cy - 5);
                        g.FillEllipse(b, cx + 4.5f, cy + 1.5f, 2.5f, 2.5f);
                        break;
                    case TabStopType.Bar: // Vertical bar (│)
                        g.DrawLine(p, cx, cy - 5, cx, cy + 5);
                        break;
                }
            }
        }

        public void Paste()
        {
            if (Clipboard.ContainsText())
            {
                string clip = Clipboard.GetText();
                InsertText(clip);
            }
        }

        private void DeleteSelection()
        {
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                PieceTable.Remove(min, len);
                AdjustFormattedRunsOnDelete(min, len);
                CursorPosition = min;
                SelectionStart = min;
                SelectionEnd = min;
            }
        }

        public void Draw(Graphics g, int width, int height, RibbonBar ribbon)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int canvasW = width;

            // 1. Workspace Background
            Color wsBg = Color.FromArgb(229, 229, 229);
            if (ViewMode == 0) wsBg = Color.FromArgb(37, 37, 37); // MS Word Dark Reading Mode Background (#252525)
            else if (ViewMode == 2) wsBg = Color.FromArgb(240, 240, 240); // Web mode clean bg

            using (SolidBrush bgBrush = new SolidBrush(wsBg))
            {
                g.FillRectangle(bgBrush, 0, 155, canvasW, height - 183);
            }

            // 2. Compute Paper Bounds based on ViewMode (Read, Print, Web)
            int pageX, pageW, pageH, marginX, rightMarginX, marginY;
            GetPageLayoutMetrics(canvasW, ribbon, out pageX, out pageW, out pageH, out marginX, out rightMarginX, out marginY);

            if (CurrentHeaderPreset != HeaderFooterPreset.None || IsEditingHeaderFooter || !string.IsNullOrEmpty(HeaderText))
            {
                if (CurrentHeaderPreset != HeaderFooterPreset.Banded || HeaderBannerYOffset <= 10)
                {
                    marginY = Math.Max(marginY, (int)(44 * ZoomLevel));
                }
            }
            int maxTextW = pageW - marginX - rightMarginX;
            int startPageY = (ribbon != null && ribbon.ShowRuler ? 190 : 165) - ScrollY;

            string fullText = PieceTable.GetText();
            int textX = pageX + marginX;
            CurrentRenderTextX = textX;
            int currentY = startPageY + marginY;

            string[] paragraphs = fullText.Split('\n');
            int paraStartIndex = 0;

            int cursorX = textX;
            int cursorY = currentY;
            int cursorH = (int)(20 * ZoomLevel);
            int currentPage = 0;

            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            bool hasSelection = (selMin != selMax);

            MoveHandleRects.Clear();
            ResizeHandleRects.Clear();
            DocParagraphGaps.Clear();
            InsertButtonHits.Clear();
            RenderedLines.Clear();
            TableCharRanges.Clear();
            int currentTableCount = 0;

            // Single-Pass High Performance Engine: Calculate necessary pages including floating shapes/charts!
            int maxNeededPages = 1;
            foreach (var sh in DocumentShapes)
            {
                if (sh.Visible && sh.PageNumber + 1 > maxNeededPages) maxNeededPages = sh.PageNumber + 1;
            }
            foreach (var ch in DocumentCharts)
            {
                if (ch.Visible && ch.PageNumber + 1 > maxNeededPages) maxNeededPages = ch.PageNumber + 1;
            }
            int pageCount = Math.Max(maxNeededPages, CalculatedPageCount);
            TotalPageCount = pageCount;
            CurrentActivePage = Math.Max(1, Math.Min(pageCount, (int)((ScrollY + (height / 2)) / (pageH + 30)) + 1));
            CalculatedPageCount = pageCount;
            if (ViewMode == 0)
            {
                // MS Word Read Mode (📖): Render 2 Side-by-Side Pages or Centered Book Page
                DrawPageCanvas(g, pageX, startPageY, pageW, pageH, ribbon, ReadModePageIndex + 1, pageCount);

                // Draw Semi-Transparent Page Navigation Arrow Buttons (◀ and ▶)
                int arrowY = 155 + (height - 183) / 2;
                
                // Left Arrow (◀)
                Rectangle leftBtn = new Rectangle(20, arrowY - 25, 45, 45);
                using (SolidBrush circleBg = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                {
                    g.FillEllipse(circleBg, leftBtn);
                }
                g.DrawEllipse(Pens.Gray, leftBtn);
                using (Font arrowFont = new Font("Segoe UI Symbol", 13.0f, FontStyle.Bold))
                {
                    g.DrawString("◀", arrowFont, Brushes.White, leftBtn.X + 11, leftBtn.Y + 10);
                }

                // Right Arrow (▶)
                Rectangle rightBtn = new Rectangle(canvasW - 65, arrowY - 25, 45, 45);
                using (SolidBrush circleBg = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                {
                    g.FillEllipse(circleBg, rightBtn);
                }
                g.DrawEllipse(Pens.Gray, rightBtn);
                using (Font arrowFont = new Font("Segoe UI Symbol", 13.0f, FontStyle.Bold))
                {
                    g.DrawString("▶", arrowFont, Brushes.White, rightBtn.X + 13, rightBtn.Y + 10);
                }
            }
            else
            {
                for (int pg = 0; pg < pageCount; pg++)
                {
                    DrawPageCanvas(g, pageX, startPageY + pg * (pageH + 30), pageW, pageH, ribbon, pg + 1, pageCount);
                }
            }

            // Pre-pass: Scan all Shape and Chart tags from paragraphs to register/update DocShapeObject & DocChartObject before text layout!
            for (int pScan = 0; pScan < paragraphs.Length; pScan++)
            {
                string pStr = paragraphs[pScan];
                if (pStr.StartsWith("![SHAPE:") || pStr.StartsWith("![IMAGE:") || (pStr.StartsWith("![IMAGE](") && pStr.EndsWith(")")))
                {
                    string sId = "", sType = "Rectangle", sPath = "";
                    int tX = -1, tY = -1, tW = -1, tH = -1, tWrap = -1, tPage = -1, tStyle = -1;
                    DocShapeObject.ParseShapeTag(pStr, out sId, out sType, out sPath, out tX, out tY, out tW, out tH, out tWrap, out tPage, out tStyle);
                    if (string.IsNullOrEmpty(sId)) sId = "Shape_" + pScan;
                    DocShapeObject sObj = GetOrCreateDocShape(sId, sType);
                    if (!IsDraggingShape)
                    {
                        if (tX >= 0) sObj.PageX = tX;
                        if (tY >= 0) sObj.PageY = tY;
                        if (tW > 0) sObj.BaseWidth = tW;
                        if (tH > 0) sObj.BaseHeight = tH;
                        if (tWrap >= 0) sObj.WrapMode = tWrap;
                        if (tPage >= 0) sObj.PageNumber = tPage;
                        if (tStyle >= 0) sObj.PictureStylePreset = tStyle;
                        if (!string.IsNullOrEmpty(sPath)) sObj.ImagePath = sPath;
                    }
                }
            }

            // Pre-calculate LastBounds for all Floating charts (WrapMode != 0) before text layout!
            foreach (var ch in DocumentCharts)
            {
                if (ch.WrapMode != 0)
                {
                    int cW = Math.Max((int)(200 * ZoomLevel), (int)((ch.BaseWidth + ch.WidthDelta) * ZoomLevel));
                    int cH = Math.Max((int)(120 * ZoomLevel), (int)((ch.BaseHeight + ch.HeightDelta) * ZoomLevel));
                    int pTopY = startPageY + ch.PageNumber * (pageH + 30) + marginY;
                    int cX = pageX + (int)(ch.PageX * ZoomLevel);
                    int cY = pTopY + (int)(ch.PageY * ZoomLevel);
                    ch.LastBounds = new Rectangle(cX, cY, cW, cH);
                }
                else
                {
                    ch.LastBounds = Rectangle.Empty;
                }
            }

            // Pre-calculate LastBounds for all Floating shapes (WrapMode != 0) before text layout!
            foreach (var sh in DocumentShapes)
            {
                if (sh.WrapMode != 0)
                {
                    int sW = Math.Max((int)(20 * ZoomLevel), (int)((sh.BaseWidth + sh.WidthDelta) * ZoomLevel));
                    int sH = Math.Max((int)(15 * ZoomLevel), (int)((sh.BaseHeight + sh.HeightDelta) * ZoomLevel));
                    int pTopY = startPageY + sh.PageNumber * (pageH + 30) + marginY;
                    int sX = pageX + (int)(sh.PageX * ZoomLevel);
                    int sY = pTopY + (int)(sh.PageY * ZoomLevel);
                    sh.LastBounds = new Rectangle(sX, sY, sW, sH);
                }
                else
                {
                    sh.LastBounds = Rectangle.Empty;
                }
            }

            // PASS 1: Render Behind-Text Shapes & Charts (WrapMode == 5) beneath text
            foreach (var sh in DocumentShapes)
            {
                if (sh.WrapMode == 5 && !sh.LastBounds.IsEmpty)
                {
                    ShapeRenderer.DrawShape(g, sh, sh.LastBounds, ribbon, ZoomLevel);
                }
            }
            foreach (var ch in DocumentCharts)
            {
                if (ch.WrapMode == 5 && !ch.LastBounds.IsEmpty)
                {
                    DrawDocumentChart(g, ch, ch.LastBounds.X, ch.LastBounds.Y, ch.LastBounds.Width, ch.LastBounds.Height, ribbon);
                }
            }

            DocParagraphGaps.Add(new ParagraphGap { ScreenY = currentY, CharIndex = 0, VirtualEntersNeeded = 0 });

            using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(180, 213, 254)))
            {
                for (int p = 0; p < paragraphs.Length; p++)
                {
                    string para = paragraphs[p];

                    // 1. All Break Handlers (Page & Section Breaks)
                    string pTrim = para.Trim();
                    if (pTrim == "[--- PAGE BREAK ---]" || para.Contains("TRANG TRẮNG MỚI"))
                    {
                        if (ShowFormattingMarks)
                        {
                            int gapMidY = startPageY + currentPage * (pageH + 30) + pageH + 15;
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                SizeF lbSz = g.MeasureString("····························································································· Ngắt trang (Page Break) ·····························································································", pFont);
                                int lbX = pageX + (pageW - (int)lbSz.Width) / 2;
                                g.DrawString("····························································································· Ngắt trang (Page Break) ·····························································································", pFont, Brushes.DimGray, lbX, gapMidY - (int)(lbSz.Height / 2));
                            }
                        }
                        currentPage++;
                        currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }
                    else if (pTrim == "[--- COLUMN BREAK ---]")
                    {
                        if (ShowFormattingMarks)
                        {
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                g.DrawString("············································································· Ngắt cột (Column Break) ·············································································", pFont, Brushes.DimGray, pageX + marginX, currentY);
                            }
                        }
                        currentPage++;
                        currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }
                    else if (pTrim == "[--- TEXT WRAPPING BREAK ---]")
                    {
                        if (ShowFormattingMarks)
                        {
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                g.DrawString("····················································· Ngắt bao quanh đối tượng (Text Wrapping Break) ·····················································", pFont, Brushes.DimGray, pageX + marginX, currentY);
                            }
                        }
                        currentY += (int)(18 * ZoomLevel);
                        foreach (var sh in DocumentShapes)
                        {
                            if (sh != null && sh.PageNumber == currentPage)
                            {
                                int sBot = startPageY + currentPage * (pageH + 30) + sh.PageY + (sh.BaseHeight + sh.HeightDelta);
                                if (sBot > currentY) currentY = sBot + 12;
                            }
                        }
                        paraStartIndex += para.Length + 1;
                        continue;
                    }
                    else if (pTrim == "[--- SECTION BREAK (NEXT PAGE) ---]")
                    {
                        if (ShowFormattingMarks)
                        {
                            int gapMidY = startPageY + currentPage * (pageH + 30) + pageH + 15;
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                SizeF lbSz = g.MeasureString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Trang tiếp theo / Next Page) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont);
                                int lbX = pageX + (pageW - (int)lbSz.Width) / 2;
                                g.DrawString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Trang tiếp theo / Next Page) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont, Brushes.DarkSlateGray, lbX, gapMidY - (int)(lbSz.Height / 2));
                            }
                        }
                        currentPage++;
                        currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }
                    else if (pTrim == "[--- SECTION BREAK (CONTINUOUS) ---]")
                    {
                        if (ShowFormattingMarks)
                        {
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                g.DrawString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Liên tục / Continuous) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont, Brushes.DarkSlateGray, pageX + marginX, currentY);
                            }
                            currentY += (int)(20 * ZoomLevel);
                        }
                        paraStartIndex += para.Length + 1;
                        continue;
                    }
                    else if (pTrim == "[--- SECTION BREAK (EVEN PAGE) ---]")
                    {
                        if (ShowFormattingMarks)
                        {
                            int gapMidY = startPageY + currentPage * (pageH + 30) + pageH + 15;
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                SizeF lbSz = g.MeasureString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Trang chẵn / Even Page) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont);
                                int lbX = pageX + (pageW - (int)lbSz.Width) / 2;
                                g.DrawString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Trang chẵn / Even Page) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont, Brushes.DarkSlateGray, lbX, gapMidY - (int)(lbSz.Height / 2));
                            }
                        }
                        currentPage++;
                        if ((currentPage + 1) % 2 != 0) currentPage++;
                        currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }
                    else if (pTrim == "[--- SECTION BREAK (ODD PAGE) ---]")
                    {
                        if (ShowFormattingMarks)
                        {
                            int gapMidY = startPageY + currentPage * (pageH + 30) + pageH + 15;
                            using (Font pFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Italic))
                            {
                                SizeF lbSz = g.MeasureString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Trang lẻ / Odd Page) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont);
                                int lbX = pageX + (pageW - (int)lbSz.Width) / 2;
                                g.DrawString(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: Ngắt phân đoạn (Trang lẻ / Odd Page) ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::", pFont, Brushes.DarkSlateGray, lbX, gapMidY - (int)(lbSz.Height / 2));
                            }
                        }
                        currentPage++;
                        if ((currentPage + 1) % 2 == 0) currentPage++;
                        currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }

                    if (para.Trim() == "[--- COVER PAGE START ---]" || para.Trim() == "[--- COVER PAGE END ---]")
                    {
                        paraStartIndex += para.Length + 1;
                        continue; // Hide markers
                    }

                    // 2. Real Vector Shape & Image Handler
                    if (para.StartsWith("![SHAPE:") || para.StartsWith("![IMAGE:") || (para.StartsWith("![IMAGE](") && para.EndsWith(")")) || para.Contains("HÌNH KHỐI VECTOR") || para.Contains("HÌNH ÁNH DỰ ÁN"))
                    {
                        string shapeId = "";
                        string shapeType = "Rectangle";
                        string imgPath = "";
                        int tagX = -1, tagY = -1, tagW = -1, tagH = -1, tagWrap = -1, tagPage = -1, tagStyle = -1;

                        DocShapeObject.ParseShapeTag(para, out shapeId, out shapeType, out imgPath, out tagX, out tagY, out tagW, out tagH, out tagWrap, out tagPage, out tagStyle);

                        if (string.IsNullOrEmpty(shapeId)) shapeId = "Shape_" + p;
                        DocShapeObject shapeObj = GetOrCreateDocShape(shapeId, shapeType);

                        if (!IsDraggingShape)
                        {
                            if (tagX >= 0) shapeObj.PageX = tagX;
                            if (tagY >= 0) shapeObj.PageY = tagY;
                            if (tagW > 0) shapeObj.BaseWidth = tagW;
                            if (tagH > 0) shapeObj.BaseHeight = tagH;
                            if (tagWrap >= 0) shapeObj.WrapMode = tagWrap;
                            if (tagPage >= 0) shapeObj.PageNumber = tagPage;
                            if (tagStyle >= 0) shapeObj.PictureStylePreset = tagStyle;
                        }

                        if (!string.IsNullOrEmpty(imgPath))
                        {
                            shapeObj.ImagePath = imgPath;
                            if (shapeObj.CachedImage == null && System.IO.File.Exists(imgPath))
                            {
                                try
                                {
                                    using (var temp = Image.FromFile(imgPath))
                                    {
                                        shapeObj.CachedImage = new Bitmap(temp);
                                        if (tagW <= 0 && (shapeObj.BaseWidth <= 180 || shapeObj.BaseHeight <= 110))
                                        {
                                            float ratio = (float)shapeObj.CachedImage.Width / Math.Max(1, shapeObj.CachedImage.Height);
                                            if (ratio >= 1.0f)
                                            {
                                                shapeObj.BaseWidth = Math.Min(480, Math.Max(150, shapeObj.CachedImage.Width));
                                                shapeObj.BaseHeight = (int)(shapeObj.BaseWidth / ratio);
                                            }
                                            else
                                            {
                                                shapeObj.BaseHeight = Math.Min(350, Math.Max(150, shapeObj.CachedImage.Height));
                                                shapeObj.BaseWidth = (int)(shapeObj.BaseHeight * ratio);
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }

                        int sW = Math.Max((int)(20 * ZoomLevel), (int)((shapeObj.BaseWidth + shapeObj.WidthDelta) * ZoomLevel));
                        int sH = Math.Max((int)(15 * ZoomLevel), (int)((shapeObj.BaseHeight + shapeObj.HeightDelta) * ZoomLevel));

                        if (shapeObj.WrapMode == 0) // In-Line with text
                        {
                            if (currentY + sH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                            {
                                currentPage++;
                                currentY = startPageY + currentPage * (pageH + 30) + marginY;
                            }

                            int shapeX = textX + (maxTextW - sW) / 2;
                            int shapeY = currentY;
                            shapeObj.LastBounds = new Rectangle(shapeX, shapeY, sW, sH);

                            ShapeRenderer.DrawShape(g, shapeObj, shapeObj.LastBounds, ribbon, ZoomLevel);
                            currentY += sH + 16;
                        }
                        else if (shapeObj.WrapMode != 5 && shapeObj.WrapMode != 6) // Square, Tight, Through, TopBottom
                        {
                            int pTopY = startPageY + shapeObj.PageNumber * (pageH + 30) + marginY;
                            int shapeX = pageX + (int)(shapeObj.PageX * ZoomLevel);
                            int shapeY = pTopY + (int)(shapeObj.PageY * ZoomLevel);
                            shapeObj.LastBounds = new Rectangle(shapeX, shapeY, sW, sH);

                            ShapeRenderer.DrawShape(g, shapeObj, shapeObj.LastBounds, ribbon, ZoomLevel);
                        }

                        paraStartIndex += para.Length + 1;
                        continue;
                    }

                    // 4. Real Interactive Multi-Series Chart Handler
                    if (para.StartsWith("![CHART:") || para.Contains("BIỂU ĐỒ DOANH THU") || para.Contains("BIỂU ĐỒ SỐ LIỆU"))
                    {
                        string chartId = "";
                        string chartType = "Column_Clustered";
                        if (para.StartsWith("![CHART:"))
                        {
                            string inner = para.Substring(8).TrimEnd(']', '\r', '\n');
                            string[] parts = inner.Split(':');
                            foreach (var pItem in parts)
                            {
                                if (pItem.StartsWith("ID=")) chartId = pItem.Substring(3);
                                if (pItem.StartsWith("TYPE=")) chartType = pItem.Substring(5);
                            }
                        }
                        if (string.IsNullOrEmpty(chartId)) chartId = "Chart_" + p;
                        DocChartObject chartObj = GetOrCreateChart(chartId, chartType);
                        chartObj.CharIndex = paraStartIndex;

                        int chartW = Math.Max((int)(200 * ZoomLevel), (int)((chartObj.BaseWidth + chartObj.WidthDelta) * ZoomLevel));
                        int chartH = Math.Max((int)(120 * ZoomLevel), (int)((chartObj.BaseHeight + chartObj.HeightDelta) * ZoomLevel));

                        if (chartObj.WrapMode == 0) // Cùng dòng với văn bản (In-Line)
                        {
                            if (currentY + chartH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                            {
                                currentPage++;
                                currentY = startPageY + currentPage * (pageH + 30) + marginY;
                            }

                            int chartX = textX + (maxTextW - chartW) / 2;
                            int chartY = currentY;
                            chartObj.LastBounds = new Rectangle(chartX, chartY, chartW, chartH);

                            DrawDocumentChart(g, chartObj, chartX, chartY, chartW, chartH, ribbon);
                            currentY += chartH + 16;
                        }
                        else if (chartObj.WrapMode != 5 && chartObj.WrapMode != 6) // Square, Tight, Through, Top/Bottom
                        {
                            int pTopY = startPageY + chartObj.PageNumber * (pageH + 30) + marginY;
                            int chartX = pageX + (int)(chartObj.PageX * ZoomLevel);
                            int chartY = pTopY + (int)(chartObj.PageY * ZoomLevel);
                            chartObj.LastBounds = new Rectangle(chartX, chartY, chartW, chartH);

                            DrawDocumentChart(g, chartObj, chartX, chartY, chartW, chartH, ribbon);
                        }

                        paraStartIndex += para.Length + 1;
                        continue;
                    }

                    // 5. Real Callout Note Box Handler
                    if (para.StartsWith("![NOTEBOX:") || para.Contains("KHUNG GHI CHÚ CHÍNH THỨC"))
                    {
                        string noteTxt = para.Contains("![NOTEBOX:") ? para.Substring(10).TrimEnd(']', '\r', '\n') : "Nhập nội dung ghi chú tại đây...";
                        int noteW = maxTextW;
                        int noteH = (int)(60 * ZoomLevel);

                        if (currentY + noteH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                        {
                            currentPage++;
                            currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        }

                        using (SolidBrush nBg = new SolidBrush(Color.FromArgb(240, 244, 249)))
                        using (SolidBrush barBrush = new SolidBrush(Color.FromArgb(11, 87, 208)))
                        using (Font nFont = new Font("Segoe UI", 9.5f * ZoomLevel, FontStyle.Regular))
                        using (Font boldFont = new Font("Segoe UI", 9.5f * ZoomLevel, FontStyle.Bold))
                        {
                            g.FillRectangle(nBg, textX, currentY, noteW, noteH);
                            g.FillRectangle(barBrush, textX, currentY, (int)(5 * ZoomLevel), noteH);
                            g.DrawString("💡 GHI CHÚ", boldFont, Brushes.DarkBlue, textX + 15, currentY + 10);
                            
                            StringFormat sf = StringFormat.GenericTypographic;
                            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                            g.DrawString(noteTxt, nFont, Brushes.Black, textX + 15, currentY + 32, sf);

                            if (!hasSelection && CursorPosition >= paraStartIndex && CursorPosition <= paraStartIndex + para.Length)
                            {
                                int relCursor = CursorPosition - paraStartIndex;
                                if (relCursor >= 10 && relCursor <= 10 + noteTxt.Length)
                                {
                                    string beforeCursor = para.Substring(10, relCursor - 10);
                                    float cX = textX + 15 + g.MeasureString(beforeCursor, nFont, Point.Empty, sf).Width;
                                    cursorX = (int)cX;
                                    cursorY = currentY + 32;
                                    cursorH = (int)(nFont.Height);
                                }
                            }
                        }

                        currentY += noteH + 16;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }

                    // 5.5. Real Horizontal Divider Line Handler (Bottom border / hr from Word)
                    if (para.StartsWith("![DIVIDER:") || para.StartsWith("![HR:") || para == "---" || para == "***")
                    {
                        int divW = maxTextW;
                        int divH = (int)(14 * ZoomLevel);
                        if (currentY + divH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                        {
                            currentPage++;
                            currentY = startPageY + currentPage * (pageH + 30) + marginY;
                        }

                        Color divColor = Color.FromArgb(27, 54, 93); // #1B365D Navy Blue
                        if (para.Contains("COLOR=#"))
                        {
                            try
                            {
                                int cIdx = para.IndexOf("COLOR=#") + 7;
                                string hex = para.Substring(cIdx, 6);
                                int rC = Convert.ToInt32(hex.Substring(0, 2), 16);
                                int gC = Convert.ToInt32(hex.Substring(2, 2), 16);
                                int bC = Convert.ToInt32(hex.Substring(4, 2), 16);
                                divColor = Color.FromArgb(rC, gC, bC);
                            }
                            catch { }
                        }

                        using (Pen divPen = new Pen(divColor, Math.Max(1.5f, 1.5f * ZoomLevel)))
                        {
                            int lineY = currentY + (int)(6 * ZoomLevel);
                            g.DrawLine(divPen, textX, lineY, textX + divW, lineY);
                        }

                        currentY += divH;
                        paraStartIndex += para.Length + 1;
                        continue;
                    }

                    // Visual MS Word Table Engine: Render '|' lines as real GDI+ tables matching Image 2!
                    if (!string.IsNullOrWhiteSpace(para) && para.Trim().StartsWith("|"))
                    {
                        List<string> tableLines = new List<string>();
                        int tblParaStart = paraStartIndex;
                        int tblTotalLen = 0;

                        while (p < paragraphs.Length && !string.IsNullOrWhiteSpace(paragraphs[p]) && paragraphs[p].Trim().StartsWith("|"))
                        {
                            tableLines.Add(paragraphs[p]);
                            tblTotalLen += paragraphs[p].Length + 1;
                            p++;
                        }
                        p--; // Adjust for loop increment

                        // Parse clean rows (ignoring divider lines like |---|)
                        List<List<string>> tableRows = new List<List<string>>();
                        foreach (string tLine in tableLines)
                        {
                            if (tLine.Contains("---") || tLine.Contains("===")) continue;
                            string[] rawCells = tLine.Split('|');
                            List<string> rowCells = new List<string>();
                            for (int c = 1; c < rawCells.Length - 1; c++)
                            {
                                rowCells.Add(rawCells[c].Trim());
                            }
                            if (rowCells.Count > 0) tableRows.Add(rowCells);
                        }

                        if (tableRows.Count > 0)
                        {
                            int tblIdx = currentTableCount++;
                            int numRows = tableRows.Count;
                            int numCols = 0;
                            foreach (var r in tableRows) numCols = Math.Max(numCols, r.Count);
                            if (numCols <= 0) numCols = 1;

                            // 🌟 Special Callout / Quote Box Handler: 1x1 Table in Word is rendered as a clean Shaded Box with Left Blue Accent!
                            if (numRows == 1 && numCols == 1)
                            {
                                string cellTxt = tableRows[0][0];
                                int boxW = maxTextW;
                                using (Font calloutFont = new Font("Times New Roman", 11.0f * ZoomLevel, FontStyle.Italic))
                                {
                                    SizeF txtSz = g.MeasureString(cellTxt, calloutFont, boxW - (int)(32 * ZoomLevel), SafeStringFormat);
                                    int boxH = Math.Max((int)(38 * ZoomLevel), (int)txtSz.Height + (int)(20 * ZoomLevel));

                                    if (currentY + boxH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                                    {
                                        currentPage++;
                                        currentY = startPageY + currentPage * (pageH + 30) + marginY;
                                    }

                                    int boxX = textX;
                                    int boxY = currentY;

                                    if (boxY + boxH >= 155 && boxY <= height - 26)
                                    {
                                        using (SolidBrush boxBg = new SolidBrush(Color.FromArgb(242, 244, 248)))
                                        using (SolidBrush barBrush = new SolidBrush(Color.FromArgb(15, 76, 129)))
                                        {
                                            g.FillRectangle(boxBg, boxX, boxY, boxW, boxH);
                                            g.FillRectangle(barBrush, boxX, boxY, (int)(4.5f * ZoomLevel), boxH);
                                            RectangleF textRect = new RectangleF(boxX + (int)(16 * ZoomLevel), boxY + (int)(10 * ZoomLevel), boxW - (int)(32 * ZoomLevel), txtSz.Height + (int)(4 * ZoomLevel));
                                            g.DrawString(cellTxt, calloutFont, Brushes.Black, textRect, SafeStringFormat);
                                        }
                                    }

                                    if (CursorPosition >= tblParaStart && CursorPosition <= tblParaStart + tblTotalLen)
                                    {
                                        cursorX = boxX + (int)(16 * ZoomLevel);
                                        cursorY = boxY + (int)(10 * ZoomLevel);
                                        cursorH = (int)(18 * ZoomLevel);
                                    }

                                    currentY += boxH + 16;
                                    paraStartIndex += tblTotalLen;
                                    continue;
                                }
                            }

                            // Initialize or validate per-column widths
                            if (!TableColWidths.ContainsKey(tblIdx) || TableColWidths[tblIdx].Count != numCols)
                            {
                                int defaultColW = Math.Max(30, maxTextW / numCols);
                                if (TableCustomSizes.ContainsKey(tblIdx))
                                {
                                    defaultColW = Math.Max(20, TableCustomSizes[tblIdx].Width / numCols);
                                }
                                List<int> colWList = new List<int>();
                                for (int c = 0; c < numCols; c++) colWList.Add(defaultColW);
                                TableColWidths[tblIdx] = colWList;
                            }

                            // Initialize or validate per-row heights
                            if (!TableRowHeights.ContainsKey(tblIdx) || TableRowHeights[tblIdx].Count != numRows)
                            {
                                int defaultRowH = (int)(28.0f * ZoomLevel);
                                if (TableCustomSizes.ContainsKey(tblIdx))
                                {
                                    defaultRowH = Math.Max(15, TableCustomSizes[tblIdx].Height / numRows);
                                }
                                List<int> rowHList = new List<int>();
                                for (int r = 0; r < numRows; r++) rowHList.Add(defaultRowH);
                                TableRowHeights[tblIdx] = rowHList;
                            }

                            int tWrap = TableWrapModes.ContainsKey(tblIdx) ? TableWrapModes[tblIdx] : 0;
                            int tableX = pageX + (TablePageX.ContainsKey(tblIdx) ? TablePageX[tblIdx] : marginX);
                            int maxPaperRight = pageX + pageW;
                            int maxAvailW = maxPaperRight - tableX;

                            // Calculate column X positions
                            List<int> colXPos = new List<int>();
                            int runX = tableX;
                            for (int c = 0; c < numCols; c++)
                            {
                                colXPos.Add(runX);
                                runX += TableColWidths[tblIdx][c];
                            }
                            int tableW = runX - tableX;

                            // Calculate row Y positions
                            int tableY = (tWrap == 1 && TablePageY.ContainsKey(tblIdx)) ? (startPageY + TablePageY[tblIdx]) : currentY;
                            List<int> rowYPos = new List<int>();
                            int runY = tableY;
                            for (int r = 0; r < numRows; r++)
                            {
                                rowYPos.Add(runY);
                                runY += TableRowHeights[tblIdx][r];
                            }
                            int tableH = runY - tableY;

                            // Page overflow: table falls on next page (chỉ áp dụng cho In-Line table)
                            if (tWrap == 0 && currentY + tableH > startPageY + currentPage * (pageH + 30) + pageH - marginY)
                            {
                                currentPage++;
                                currentY = startPageY + currentPage * (pageH + 30) + marginY;
                                tableY = currentY;
                                rowYPos.Clear();
                                runY = tableY;
                                for (int r = 0; r < numRows; r++)
                                {
                                    rowYPos.Add(runY);
                                    runY += TableRowHeights[tblIdx][r];
                                }
                            }

                            if (tWrap == 1)
                            {
                                TableFloatingBounds[tblIdx] = new Rectangle(tableX, tableY, tableW, tableH);
                            }
                            else
                            {
                                TableFloatingBounds.Remove(tblIdx);
                            }

                            // Hit-test handles for move/resize (resize handle on right edge)
                            Rectangle moveHitRect = new Rectangle(tableX - 25, tableY - 25, 35, 35);
                            Rectangle resizeHitRect = new Rectangle(tableX + tableW - 15, tableY + tableH - 15, 30, 30);
                            MoveHandleRects.Add(moveHitRect);
                            ResizeHandleRects.Add(resizeHitRect);

                            // Record char range for snap-on-drop
                            TableCharRanges[tblIdx] = new int[] { tblParaStart, tblTotalLen };

                            bool thisTableDragging = (IsDraggingMove && DragTableIndex == tblIdx);

                            // Draw Move Handle (✢)
                            using (SolidBrush handleBg = new SolidBrush(thisTableDragging ? Color.FromArgb(180, 245, 245, 245) : Color.FromArgb(245, 245, 245)))
                            using (Pen handlePen = new Pen(Color.FromArgb(160, 160, 160), 1))
                            using (Font handleFont = new Font("Segoe UI Symbol", 8.0f * ZoomLevel))
                            {
                                g.FillRectangle(handleBg, tableX - 16, tableY - 14, 15, 15);
                                g.DrawRectangle(handlePen, tableX - 16, tableY - 14, 15, 15);
                                g.DrawString("✢", handleFont, Brushes.DimGray, tableX - 15, tableY - 15);
                            }

                            // Render MS Word '+' Insert Row and Column Buttons ONLY when hovered near boundary
                            int hX = HoveredMousePoint.X;
                            int hY = HoveredMousePoint.Y;

                            using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(0, 102, 204)))
                            using (Font plusFont = new Font("Segoe UI", 8.5f * ZoomLevel, FontStyle.Bold))
                            {
                                // Column Insert '+' Buttons at top boundary of columns
                                if (hY >= tableY - 35 && hY <= tableY + 25)
                                {
                                    for (int c = 0; c < numCols; c++)
                                    {
                                        int bX = colXPos[c] + TableColWidths[tblIdx][c];
                                        if (Math.Abs(hX - bX) <= 15)
                                        {
                                            Rectangle colBtnRect = new Rectangle(bX - 8, tableY - 20, 16, 16);
                                            InsertButtonHits.Add(new InsertButtonHit { Rect = colBtnRect, TableIndex = tblIdx, IsRowInsert = false, InsertIndex = c + 1 });

                                            g.FillEllipse(btnBg, colBtnRect);
                                            g.DrawString("+", plusFont, Brushes.White, colBtnRect.X + 3, colBtnRect.Y + 1);
                                        }
                                    }
                                }

                                // Row Insert '+' Buttons at left boundary of rows
                                if (hX >= tableX - 35 && hX <= tableX + 25)
                                {
                                    for (int r = 0; r < numRows; r++)
                                    {
                                        int bY = rowYPos[r] + TableRowHeights[tblIdx][r];
                                        if (Math.Abs(hY - bY) <= 15)
                                        {
                                            Rectangle rowBtnRect = new Rectangle(tableX - 20, bY - 8, 16, 16);
                                            InsertButtonHits.Add(new InsertButtonHit { Rect = rowBtnRect, TableIndex = tblIdx, IsRowInsert = true, InsertIndex = r + 1 });

                                            g.FillEllipse(btnBg, rowBtnRect);
                                            g.DrawString("+", plusFont, Brushes.White, rowBtnRect.X + 3, rowBtnRect.Y + 1);
                                        }
                                    }
                                }
                            }

                            // Draw Cells (Modern Word-style table rendering)
                            using (SolidBrush headerBrush = new SolidBrush(thisTableDragging ? Color.FromArgb(180, 37, 99, 235) : Color.FromArgb(37, 99, 235)))
                            using (SolidBrush evenCellBrush = new SolidBrush(thisTableDragging ? Color.FromArgb(80, 248, 250, 252) : Color.FromArgb(248, 250, 252)))
                            using (SolidBrush oddCellBrush = new SolidBrush(thisTableDragging ? Color.FromArgb(80, 255, 255, 255) : Color.White))
                            using (Pen gridPen = new Pen(thisTableDragging ? Color.FromArgb(120, 203, 213, 225) : Color.FromArgb(203, 213, 225), 1.0f))
                            using (Font headerFont = new Font("Segoe UI", 10.0f * ZoomLevel, FontStyle.Bold))
                            using (Font cellFont = new Font("Segoe UI", 10.0f * ZoomLevel, FontStyle.Regular))
                            {
                                List<string> cleanDataLines = new List<string>();
                                List<int> cleanLineStarts = new List<int>();
                                int lStart = tblParaStart;
                                foreach (string tLine in tableLines)
                                {
                                    if (!tLine.Contains("---") && !tLine.Contains("==="))
                                    {
                                        cleanDataLines.Add(tLine);
                                        cleanLineStarts.Add(lStart);
                                    }
                                    lStart += tLine.Length + 1;
                                }

                                for (int r = 0; r < numRows; r++)
                                {
                                    string lineStr = (r < cleanDataLines.Count) ? cleanDataLines[r] : "";
                                    int rowLineStart = (r < cleanLineStarts.Count) ? cleanLineStarts[r] : 0;
                                    int cellY = rowYPos[r];
                                    int cellH = TableRowHeights[tblIdx][r];
                                    bool isHeaderRow = (r == 0);

                                    for (int c = 0; c < numCols; c++)
                                    {
                                        int cellX = colXPos[c];
                                        int cellW = TableColWidths[tblIdx][c];
                                        Rectangle cellRect = new Rectangle(cellX, cellY, cellW, cellH);

                                        // Fill Solid Opaque Background
                                        if (isHeaderRow) g.FillRectangle(headerBrush, cellRect);
                                        else if (r % 2 == 0) g.FillRectangle(evenCellBrush, cellRect);
                                        else g.FillRectangle(oddCellBrush, cellRect);

                                        g.DrawRectangle(gridPen, cellRect);

                                        string cellTxt = (r < tableRows.Count && c < tableRows[r].Count) ? tableRows[r][c] : "";
                                        if (!string.IsNullOrEmpty(cellTxt))
                                        {
                                            Brush tBrush = isHeaderRow ? Brushes.White : Brushes.Black;
                                            Font fToUse = isHeaderRow ? headerFont : cellFont;

                                            decimal dummyD;
                                            bool isNum = !isHeaderRow && c > 0 && decimal.TryParse(cellTxt.Replace(",", "").Replace("%", "").Trim(), out dummyD);

                                            if (isHeaderRow)
                                            {
                                                SizeF sz = g.MeasureString(cellTxt, fToUse);
                                                float tX = cellX + Math.Max(4, (cellW - sz.Width) / 2);
                                                float tY = cellY + Math.Max(2, (cellH - sz.Height) / 2);
                                                g.DrawString(cellTxt, fToUse, tBrush, tX, tY);
                                            }
                                            else if (isNum)
                                            {
                                                SizeF sz = g.MeasureString(cellTxt, fToUse);
                                                float tX = cellX + cellW - sz.Width - 8;
                                                float tY = cellY + Math.Max(2, (cellH - sz.Height) / 2);
                                                g.DrawString(cellTxt, fToUse, tBrush, tX, tY);
                                            }
                                            else
                                            {
                                                SizeF sz = g.MeasureString(cellTxt, fToUse);
                                                float tY = cellY + Math.Max(2, (cellH - sz.Height) / 2);
                                                g.DrawString(cellTxt, fToUse, tBrush, cellX + 8, tY);
                                            }
                                        }

                                        // Compute In-Cell Cursor Position with exact 1-to-1 row alignment
                                        if (lineStr.Contains("|"))
                                        {
                                            int pipeCount = 0;
                                            int cStartIdx = -1;
                                            int cEndIdx = -1;
                                            for (int k = 0; k < lineStr.Length; k++)
                                            {
                                                if (lineStr[k] == '|')
                                                {
                                                    pipeCount++;
                                                    if (pipeCount == c + 1) cStartIdx = rowLineStart + k + 1;
                                                    else if (pipeCount == c + 2) { cEndIdx = rowLineStart + k; break; }
                                                }
                                            }
                                            if (cStartIdx >= 0)
                                            {
                                                if (cEndIdx < 0) cEndIdx = rowLineStart + lineStr.Length;
                                                if (CursorPosition >= cStartIdx && CursorPosition <= cEndIdx + 1)
                                                {
                                                    int off = Math.Max(0, CursorPosition - cStartIdx);
                                                    string pre = cellTxt.Substring(0, Math.Min(off, cellTxt.Length));
                                                    SizeF preSz = g.MeasureString(pre, cellFont, Point.Empty, StringFormat.GenericTypographic);
                                                    cursorX = cellX + 6 + (int)preSz.Width;
                                                    cursorY = cellY + 5;
                                                    cursorH = (int)(18.0f * ZoomLevel);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Draw Table Layout Options Floating Button (Top-Right)
                            Rectangle wrapBtnRect = new Rectangle(tableX + tableW + 6, tableY, 22, 22);
                            TableWrapBtnHits.Add(new TableWrapBtnHit { Rect = wrapBtnRect, TableIndex = tblIdx });
                            bool isWrapOpen = (ActiveTableLayoutOptionsIndex == tblIdx);
                            using (SolidBrush btnBg = new SolidBrush(isWrapOpen ? Color.FromArgb(215, 235, 255) : Color.White))
                            using (Pen btnBorder = new Pen(isWrapOpen ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), 1))
                            {
                                g.FillRectangle(btnBg, wrapBtnRect);
                                g.DrawRectangle(btnBorder, wrapBtnRect);
                            }
                            using (Pen archPen = new Pen(Color.FromArgb(0, 102, 204), 2.2f))
                            using (Pen linePen = new Pen(Color.FromArgb(0, 120, 215), 1.4f))
                            {
                                g.DrawLine(linePen, wrapBtnRect.X + 4, wrapBtnRect.Y + 5, wrapBtnRect.X + 18, wrapBtnRect.Y + 5);
                                ShapeRenderer.DrawVectorArch(g, archPen, wrapBtnRect.X + 5, wrapBtnRect.Y + 8, 12, 10);
                                g.DrawLine(linePen, wrapBtnRect.X + 4, wrapBtnRect.Y + 18, wrapBtnRect.X + 18, wrapBtnRect.Y + 18);
                            }

                            // Draw Resize Handle (Bottom-Right)
                            int handleSz = (int)(9.0f * ZoomLevel);
                            Rectangle handleRect = new Rectangle(tableX + tableW - handleSz / 2, tableY + tableH - handleSz / 2, handleSz, handleSz);
                            using (SolidBrush hBrush = new SolidBrush(Color.White))
                            using (Pen handlePen = new Pen(Color.FromArgb(0, 102, 204), 1.5f))
                            {
                                g.FillRectangle(hBrush, handleRect);
                                g.DrawRectangle(handlePen, handleRect);
                            }

                            // Text Flow: natural doc position, always advance by tableH
                            currentY += tableH + 16;
                            // Track gap AFTER this table
                            DocParagraphGaps.Add(new ParagraphGap { ScreenY = currentY, CharIndex = paraStartIndex + tblTotalLen, VirtualEntersNeeded = 0 });
                            paraStartIndex += tblTotalLen;
                            continue;
                        }
                    }

                    var wrappedLines = LayoutParagraphLines(para, paraStartIndex, ref currentY, ref currentPage, startPageY, pageH, marginY, textX, maxTextW, g, ribbon);

                    foreach (var line in wrappedLines)
                    {
                        int dynamicLineHeight = line.LineHeight;
                        float lineStartX = line.StartX;
                        bool isLineVisible = (line.LineY + dynamicLineHeight >= 155 && line.LineY <= height - 26);

                        // Calculate Alignment Shift (Left, Center, Right) within available line width
                        float lineW = MeasureSubstringWidth(g, line.Text, line.StartIndex, 0, line.Text.Length);
                        int pAlign = ribbon.Alignment;
                        if (ParagraphAlignments.ContainsKey(paraStartIndex))
                        {
                            pAlign = ParagraphAlignments[paraStartIndex];
                        }

                        if (pAlign == 1) // Center
                        {
                            lineStartX = line.StartX + Math.Max(0, (line.Width - lineW) / 2.0f);
                        }
                        else if (pAlign == 2) // Align Right
                        {
                            lineStartX = line.StartX + Math.Max(0, line.Width - lineW);
                        }

                        // Style-Aware Pixel-Exact Selection Highlight Box
                        if (hasSelection && selMax > line.StartIndex && selMin < line.StartIndex + line.Length && isLineVisible)
                        {
                            int sIdx = Math.Max(line.StartIndex, selMin) - line.StartIndex;
                            int eIdx = Math.Min(line.StartIndex + line.Length, selMax) - line.StartIndex;

                            float preW = MeasureSubstringWidth(g, line.Text, line.StartIndex, 0, sIdx);
                            float selW = MeasureSubstringWidth(g, line.Text, line.StartIndex, sIdx, eIdx - sIdx);

                            float textH = (line.MaxFontSize * 1.45f * ZoomLevel) + 2.0f * ZoomLevel;
                            float selY = line.LineY - 1.0f * ZoomLevel;
                            RectangleF selBoxRect = new RectangleF(lineStartX + preW, selY, Math.Max(5.0f, selW), textH);

                            using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(95, 147, 197, 253)))
                            using (Pen sBorderPen = new Pen(Color.FromArgb(160, 59, 130, 246), 1.0f))
                            {
                                FillRoundedRectangle(g, sBrush, selBoxRect, 3.5f * ZoomLevel);
                                DrawRoundedRectangle(g, sBorderPen, selBoxRect, 3.5f * ZoomLevel);
                            }
                        }

                        // Draw line formatted runs
                        float curX = lineStartX;
                        int i = 0;
                        while (i < line.Text.Length)
                        {
                            FormattedRun fmt = GetFormatAt(line.StartIndex + i);
                            int runLen = 1;
                            while (i + runLen < line.Text.Length && GetFormatAt(line.StartIndex + i + runLen) == fmt)
                            {
                                runLen++;
                            }

                            string runStr = line.Text.Substring(i, runLen);

                            FontStyle style = FontStyle.Regular;
                            if (fmt.Bold) style |= FontStyle.Bold;
                            if (fmt.Italic) style |= FontStyle.Italic;
                            if (fmt.Underline) style |= FontStyle.Underline;
                            if (fmt.Strike) style |= FontStyle.Strikeout;

                            float effectiveFontSize = fmt.FontSize;
                            bool isSub = fmt.Subscript;
                            bool isSup = fmt.Superscript;
                            float drawY = line.LineY;

                            if (isSub)
                            {
                                effectiveFontSize = fmt.FontSize * 0.72f;
                                drawY = line.LineY + (line.MaxFontSize * 0.45f * ZoomLevel);
                            }
                            else if (isSup)
                            {
                                effectiveFontSize = fmt.FontSize * 0.72f;
                                drawY = line.LineY - (line.MaxFontSize * 0.12f * ZoomLevel);
                            }
                            else
                            {
                                // Baseline alignment: căn chỉnh chân chữ (baseline) cho các font chữ khác kích thước cùng hàng
                                if (line.MaxFontSize > effectiveFontSize)
                                {
                                    drawY = line.LineY + (line.MaxFontSize - effectiveFontSize) * ZoomLevel * 0.85f;
                                }
                            }

                            Font runFont = GetCachedFont(fmt.FontName, effectiveFontSize * ZoomLevel, style);
                            SizeF runSize = MeasureStringWithTabs(g, runStr, runFont, ZoomLevel);
                            float runW = runSize.Width;

                            if (isLineVisible)
                            {
                                if (fmt.Highlight != Color.Transparent && !hasSelection)
                                {
                                    using (SolidBrush hlBrush = new SolidBrush(fmt.Highlight))
                                    {
                                        g.FillRectangle(hlBrush, curX, drawY, runW, dynamicLineHeight);
                                    }
                                }

                                using (SolidBrush runBrush = new SolidBrush(fmt.Color))
                                {
                                    DrawStringWithTabs(g, runStr, runFont, runBrush, curX, drawY, ZoomLevel);
                                }
                            }

                            if (CursorPosition >= line.StartIndex + i && CursorPosition <= line.StartIndex + i + runLen)
                            {
                                int offset = CursorPosition - (line.StartIndex + i);
                                string pre = runStr.Substring(0, Math.Min(offset, runStr.Length));
                                SizeF preSz = MeasureStringWithTabs(g, pre, runFont, ZoomLevel);
                                cursorX = (int)(curX + preSz.Width);
                                cursorY = (int)(drawY - 1.0f * ZoomLevel);
                                cursorH = (int)((effectiveFontSize * 1.45f * ZoomLevel) + 2.0f * ZoomLevel);
                            }

                            curX += runW;
                            i += runLen;
                        }

                        if (CursorPosition == line.StartIndex + line.Length)
                        {
                            cursorX = (int)curX;
                            cursorY = line.LineY;
                        }

                        if (ShowFormattingMarks && isLineVisible && line.IsLastInPara)
                        {
                            using (Font pilcrowFont = new Font("Segoe UI", Math.Max(8.0f, line.MaxFontSize * 0.9f * ZoomLevel), FontStyle.Regular))
                            using (SolidBrush pilcrowBrush = new SolidBrush(Color.FromArgb(120, 140, 165)))
                            {
                                g.DrawString("¶", pilcrowFont, pilcrowBrush, curX + 2.0f * ZoomLevel, line.LineY, SafeStringFormat);
                            }
                        }

                        RenderedLines.Add(new RenderedLineInfo {
                            StartCharIndex = line.StartIndex,
                            Length = line.Length,
                            ScreenY = line.LineY,
                            ScreenH = dynamicLineHeight,
                            ScreenX = (int)lineStartX
                        });
                    }

                    // Draw Paragraph Borders (Top, Bottom, Left, Right / Box / Horizontal Line)
                    if (ParagraphBorders != null && ParagraphBorders.ContainsKey(paraStartIndex) && wrappedLines.Count > 0)
                    {
                        ParagraphBorderInfo bdr = ParagraphBorders[paraStartIndex];
                        if (bdr != null && bdr.HasAnyBorder)
                        {
                            int paraTopY = wrappedLines[0].LineY;
                            int paraBottomY = wrappedLines[wrappedLines.Count - 1].LineY + wrappedLines[wrappedLines.Count - 1].LineHeight;
                            float penThickness = Math.Max(1.2f, bdr.SizePt * 1.2f * ZoomLevel);
                            using (Pen bPen = new Pen(bdr.Color, penThickness))
                            {
                                if (bdr.IsHorizontalLine)
                                {
                                    int hLineY = paraBottomY + (int)(2 * ZoomLevel);
                                    g.DrawLine(bPen, textX, hLineY, textX + maxTextW, hLineY);
                                }
                                else
                                {
                                    int padY = (int)(2 * ZoomLevel);
                                    int topY = paraTopY - padY;
                                    int botY = paraBottomY + padY;

                                    if (bdr.Top) g.DrawLine(bPen, textX, topY, textX + maxTextW, topY);
                                    if (bdr.Bottom) g.DrawLine(bPen, textX, botY, textX + maxTextW, botY);
                                    if (bdr.Left) g.DrawLine(bPen, textX, topY, textX, botY);
                                    if (bdr.Right) g.DrawLine(bPen, textX + maxTextW, topY, textX + maxTextW, botY);
                                }
                            }
                        }
                    }
                    else if (ParagraphBottomBorders != null && ParagraphBottomBorders.ContainsKey(paraStartIndex))
                    {
                        Color borderCol = ParagraphBottomBorders[paraStartIndex];
                        int bdrY = currentY + (int)(3 * ZoomLevel);
                        using (Pen bPen = new Pen(borderCol, Math.Max(1.5f, 1.5f * ZoomLevel)))
                        {
                            g.DrawLine(bPen, textX, bdrY, textX + maxTextW, bdrY);
                        }
                        currentY += (int)(10 * ZoomLevel);
                    }

                    paraStartIndex += para.Length + 1;
                    DocParagraphGaps.Add(new ParagraphGap { ScreenY = currentY, CharIndex = paraStartIndex, VirtualEntersNeeded = 0 });
                }
            }

            // Generate virtual gaps for empty space below document content & next page
            int defaultLineH = (int)(Math.Max(26.0f * ZoomLevel, 11.0f * 1.4f * ZoomLevel) * ribbon.LineSpacingFactor);
            if (defaultLineH < 15) defaultLineH = 20;

            int fillY = currentY + defaultLineH;
            int fillPage = currentPage;
            int vEnters = 1;

            // Fill empty space on current page and 1 extra page below
            while (fillPage <= currentPage + 1)
            {
                int pageMaxY = startPageY + fillPage * (pageH + 30) + pageH - marginY;
                if (fillY + defaultLineH > pageMaxY)
                {
                    fillPage++;
                    fillY = startPageY + fillPage * (pageH + 30) + marginY;
                }

                DocParagraphGaps.Add(new ParagraphGap
                {
                    ScreenY = fillY,
                    CharIndex = fullText.Length,
                    VirtualEntersNeeded = vEnters
                });

                fillY += defaultLineH;
                vEnters++;
            }

            int endEstimatedPage = Math.Max(0, (currentY - startPageY) / (pageH + 30));
            int finalPages = Math.Max(1, Math.Max(currentPage + 1, endEstimatedPage + 1));
            foreach (var sh in DocumentShapes)
            {
                if (sh.PageNumber + 1 > finalPages) finalPages = sh.PageNumber + 1;
            }
            foreach (var ch in DocumentCharts)
            {
                if (ch.PageNumber + 1 > finalPages) finalPages = ch.PageNumber + 1;
            }
            CalculatedPageCount = finalPages;
            TotalPageCount = finalPages;
            CurrentActivePage = Math.Max(1, Math.Min(finalPages, (int)((ScrollY + (height / 2)) / (pageH + 30)) + 1));

            // Draw End of Document Marker in Read Mode (ViewMode == 0)
            if (ViewMode == 0)
            {
                using (Font endFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                using (SolidBrush sqBrush = new SolidBrush(Color.FromArgb(0, 120, 212)))
                {
                    g.DrawString("End of document ", endFont, Brushes.Gray, textX, currentY + 25);
                    g.FillRectangle(sqBrush, textX + 105, currentY + 30, 8, 8);
                }
            }

            // PASS 3: Render In-Front-Of-Text Shapes & Charts (WrapMode == 6) over the text
            foreach (var sh in DocumentShapes)
            {
                if (sh.Visible && sh.WrapMode == 6 && !sh.LastBounds.IsEmpty)
                {
                    ShapeRenderer.DrawShape(g, sh, sh.LastBounds, ribbon, ZoomLevel);
                }
            }
            foreach (var ch in DocumentCharts)
            {
                if (ch.Visible && ch.WrapMode == 6 && !ch.LastBounds.IsEmpty)
                {
                    DrawDocumentChart(g, ch, ch.LastBounds.X, ch.LastBounds.Y, ch.LastBounds.Width, ch.LastBounds.Height, ribbon);
                }
            }

            // 5. Draw Blinking Caret Bar (Thicker 3px Bold Caret with rounded caps)
            if (CursorVisible && ActiveShape == null && ActiveChart == null)
            {
                float cursorW = Math.Max(2.8f, 2.8f * ZoomLevel);
                using (Pen cursorPen = new Pen(Color.FromArgb(29, 78, 216), cursorW)) // Vibrant Navy Blue (#1D4ED8)
                {
                    cursorPen.StartCap = LineCap.Round;
                    cursorPen.EndCap = LineCap.Round;
                    g.DrawLine(cursorPen, cursorX, cursorY + 1, cursorX, cursorY + cursorH - 1);
                }
            }



            // 6.5. IN FRONT OF TEXT: Draw Banner On Top of Document Text (Che phủ hoàn toàn văn bản bên dưới)
            if (IsEditingHeaderFooter && CurrentHeaderPreset == HeaderFooterPreset.Banded && HeaderWrapMode == 6)
            {
                DrawBandedHeaderBanner(g, pageX, startPageY, pageW, pageH, ribbon);
            }

            // 7. TOPMOST OVERLAY: Render Layout Options Popup Dialog if opened (Đảm bảo nằm trên mọi lớp chữ và canvas)
            if (IsEditingHeaderFooter && CurrentHeaderPreset == HeaderFooterPreset.Banded && ShowHeaderLayoutOptions)
            {
                int leftM_pop = (int)(ribbon.GetEffectiveLeftMarginInches() * 54.0f * ZoomLevel);
                int rightM_pop = (int)(ribbon.GetEffectiveRightMarginInches() * 54.0f * ZoomLevel);
                int bX_pop = pageX + leftM_pop + (int)(HeaderBannerXOffset * ZoomLevel);
                int bW_pop = Math.Max((int)(50 * ZoomLevel), pageW - leftM_pop - rightM_pop + (int)(HeaderBannerWidthDelta * ZoomLevel));
                int bY_pop = startPageY + (int)(10 * ZoomLevel) + (int)(HeaderBannerYOffset * ZoomLevel);

                Rectangle wrapBtnRect = new Rectangle(bX_pop + bW_pop + 4, bY_pop, 22, 22);

                int popW = 185;
                int popH = 250;
                int popX = wrapBtnRect.Right + 8;
                int popY = wrapBtnRect.Top - 5;

                g.FillRectangle(Brushes.White, popX, popY, popW, popH);
                using (Pen popBorder = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                {
                    g.DrawRectangle(popBorder, popX, popY, popW, popH);

                    // Left pointer notch triangle pointing to wrap button
                    Point[] pointerPts = new Point[]
                    {
                        new Point(popX, popY + 12),
                        new Point(popX - 6, popY + 18),
                        new Point(popX, popY + 24)
                    };
                    g.FillPolygon(Brushes.White, pointerPts);
                    g.DrawLine(popBorder, popX, popY + 12, popX - 6, popY + 18);
                    g.DrawLine(popBorder, popX - 6, popY + 18, popX, popY + 24);
                }

                using (Font headerFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (Font sectionFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
                using (Font itemFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    // Title & Close Button
                    g.DrawString("Tùy chọn bố trí", headerFont, new SolidBrush(Color.FromArgb(0, 95, 184)), popX + 12, popY + 10);
                    using (Pen closePen = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                    {
                        g.DrawLine(closePen, popX + popW - 20, popY + 12, popX + popW - 10, popY + 22);
                        g.DrawLine(closePen, popX + popW - 10, popY + 12, popX + popW - 20, popY + 22);
                    }

                    // Section 1: Cùng dòng với văn bản (In Line with Text)
                    Rectangle sec1Bar = new Rectangle(popX + 10, popY + 36, popW - 20, 18);
                    using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241)))
                    {
                        g.FillRectangle(secBarBrush, sec1Bar);
                    }
                    g.DrawString("Cùng dòng với văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec1Bar.X + 6, sec1Bar.Y + 2);

                    Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                    DrawVectorWrapCard(g, 0, inlineCard, HeaderWrapMode == 0);

                    // Section 2: Ngắt dòng văn bản (With Text Wrapping)
                    Rectangle sec2Bar = new Rectangle(popX + 10, popY + 98, popW - 20, 18);
                    using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241)))
                    {
                        g.FillRectangle(secBarBrush, sec2Bar);
                    }
                    g.DrawString("Ngắt dòng văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec2Bar.X + 6, sec2Bar.Y + 2);

                    // 6 Vector Wrapping Cards (3 cols x 2 rows)
                    for (int r = 0; r < 2; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int wIdx = r == 0 ? (1 + c) : (4 + c);
                            Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                            DrawVectorWrapCard(g, wIdx, cardBox, HeaderWrapMode == wIdx);
                        }
                    }

                    // Vector Radio 1: Di chuyển cùng văn bản
                    int r1Y = popY + 204;
                    DrawVectorRadioButton(g, popX + 14, r1Y, !HeaderFixPosition, "Di chuyển cùng văn bản", itemFont);
                    DrawVectorInfoIcon(g, popX + popW - 26, r1Y + 2);

                    // Vector Radio 2: Cố định vị trí trên trang
                    int r2Y = popY + 228;
                    DrawVectorRadioButton(g, popX + 14, r2Y, HeaderFixPosition, "Cố định vị trí trên trang", itemFont);
                    DrawVectorInfoIcon(g, popX + popW - 26, r2Y + 2);
                }
            }

            // 7.5. TOPMOST OVERLAYS: Render Shape & Chart Popup Dialogs on top of all text and pages
            if (ActiveShape != null && ActiveShape.ShowLayoutOptions && !ActiveShape.LastBounds.IsEmpty)
            {
                ShapeRenderer.DrawLayoutOptionsPopup(g, ActiveShape, ActiveShape.LastBounds, ZoomLevel);
            }

            if (ActiveChart != null)
            {
                if (ActiveChart.ShowLayoutOptions) DrawChartWrapMenu(g, ActiveChart);
                if (ActiveChart.ShowElementsMenu) DrawChartElementsMenu(g, ActiveChart);
                if (ActiveChart.ShowStylesMenu) DrawChartStylesMenu(g, ActiveChart);
            }

            // 7.8. TABLE LAYOUT OPTIONS POPUP (Tùy chọn bố trí Wrap Text cho Bảng)
            if (ActiveTableLayoutOptionsIndex >= 0 && ActiveTableLayoutOptionsIndex < MoveHandleRects.Count)
            {
                DrawTableLayoutOptionsPopup(g, ActiveTableLayoutOptionsIndex, width, ZoomLevel);
            }

            // 7.9. RENDER DOCUMENT COMMENTS & RIGHT MARGIN MARKUP CALLOUTS
            DrawDocumentComments(g, pageX, pageW, startPageY, pageH, finalPages, ribbon);

            // 8. MS WORD NAVIGATION PANE (Tìm kiếm & Điều hướng)
            if (ShowNavigationPane)
            {
                DrawNavigationPane(g, width, height, ribbon);
            }

            // 9. MS WORD SELECTION PANE (Ngăn Lựa Chọn)
            if (ShowSelectionPane)
            {
                DrawSelectionPane(g, width, height, ribbon);
            }

            // 10. INTERACTIVE TOP RULER (Cố định ở đỉnh, luôn vẽ đè lên trên trang khi cuộn)
            if (ribbon != null && ribbon.ShowRuler)
            {
                DrawInteractiveRuler(g, pageX, pageW, marginX, rightMarginX, width, height, ribbon);
            }
        }

        private void DrawDocumentComments(Graphics g, int pageX, int pageW, int startPageY, int pageH, int pageCount, RibbonBar ribbon)
        {
            if (Comments == null || Comments.Count == 0) return;

            CommentBadgeHitRects.Clear();
            CommentReplyBtnRects.Clear();
            CommentResolveBtnRects.Clear();
            CommentAnchorTextRects.Clear();

            // Right markup separator line
            int markupSepX = pageX + pageW - 135;
            using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawLine(sepPen, markupSepX, startPageY + 10, markupSepX, startPageY + pageCount * (pageH + 30) - 10);
            }

            int calloutX = markupSepX + 12;

            for (int ci = 0; ci < Comments.Count; ci++)
            {
                DocumentComment c = Comments[ci];
                if (c == null) continue;

                // Match line position from RenderedLines
                int lineY = startPageY + 40;
                int lineH = (int)(22 * ZoomLevel);
                int lineX = pageX + (int)(ribbon.GetEffectiveLeftMarginInches() * 72 * ZoomLevel);
                int charOffset = 0;

                for (int rl = 0; rl < RenderedLines.Count; rl++)
                {
                    RenderedLineInfo rLine = RenderedLines[rl];
                    if (c.RangeStart >= rLine.StartCharIndex && c.RangeStart <= rLine.StartCharIndex + rLine.Length)
                    {
                        lineY = rLine.ScreenY;
                        lineH = rLine.ScreenH;
                        lineX = rLine.ScreenX;
                        charOffset = Math.Max(0, c.RangeStart - rLine.StartCharIndex);
                        break;
                    }
                }

                int anchorX = lineX + (int)(charOffset * 7.2f * ZoomLevel);
                int anchorW = Math.Max((int)(35 * ZoomLevel), (int)(c.RangeLength * 7.5f * ZoomLevel));
                Rectangle anchorBox = new Rectangle(anchorX, lineY, anchorW, lineH);
                CommentAnchorTextRects[c.Id] = anchorBox;

                int calloutY = lineY - 2;
                bool isHovered = (HoveredCommentId == c.Id || ActiveCommentId == c.Id || EditingReplyCommentId == c.Id);

                string authorInitial = !string.IsNullOrEmpty(c.Author) ? c.Author.Substring(0, 1).ToUpper() : "D";
                string authorName = !string.IsNullOrEmpty(c.Author) ? c.Author : "Danh";

                // Calculate relative time string
                TimeSpan elapsed = DateTime.Now - c.CreatedTime;
                string timeStr = "A few seconds ago";
                if (elapsed.TotalMinutes >= 60) timeStr = ((int)elapsed.TotalHours).ToString() + " hours ago";
                else if (elapsed.TotalMinutes >= 1) timeStr = ((int)elapsed.TotalMinutes).ToString() + " minutes ago";

                if (isHovered)
                {
                    // ================= 2. EXPANDED / HOVERED STATE (Image 2) =================
                    // A. Highlight and red border on text
                    using (SolidBrush pinkBrush = new SolidBrush(Color.FromArgb(254, 226, 226)))
                    using (Pen redPen = new Pen(Color.FromArgb(220, 38, 38), 1.2f))
                    {
                        g.FillRectangle(pinkBrush, anchorBox);
                        g.DrawRectangle(redPen, anchorBox);

                        // Red leader connecting line from text box to comment card
                        g.DrawLine(redPen, anchorBox.Right, anchorBox.Top + 1, calloutX, calloutY);
                    }

                    // Calculate dynamic card height
                    int replyCount = (c.Replies != null) ? c.Replies.Count : 0;
                    bool isReplying = (EditingReplyCommentId == c.Id);
                    int cardH = 78 + (replyCount * 32) + (isReplying ? 42 : 0);

                    // B. Expanded Comment Card
                    Rectangle cardBox = new Rectangle(calloutX, calloutY, 230, cardH);
                    g.FillRectangle(Brushes.White, cardBox);
                    using (Pen pCard = new Pen(Color.FromArgb(220, 38, 38), 1.2f))
                    {
                        g.DrawRectangle(pCard, cardBox);
                    }

                    int curCardY = calloutY + 6;

                    // Main Comment Header
                    Rectangle avaRect = new Rectangle(calloutX + 8, curCardY + 2, 20, 20);
                    using (SolidBrush avaBg = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (Font avaFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        g.FillEllipse(avaBg, avaRect);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(authorInitial, avaFont, Brushes.White, new RectangleF(avaRect.X, avaRect.Y - 1, avaRect.Width, avaRect.Height), sf);
                        }
                    }

                    using (Font authFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                    using (Font timeFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
                    {
                        g.DrawString(authorName, authFont, new SolidBrush(Color.FromArgb(15, 23, 42)), calloutX + 32, curCardY);
                        SizeF aSz = g.MeasureString(authorName, authFont);
                        g.DrawString(timeStr, timeFont, new SolidBrush(Color.FromArgb(100, 116, 139)), calloutX + 34 + (int)aSz.Width, curCardY + 1);
                    }

                    curCardY += 20;

                    // Main Comment Content
                    using (Font contentFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                    {
                        string cText = string.IsNullOrEmpty(c.Content) ? "(Không có nội dung)" : c.Content;
                        if (cText.Length > 45) cText = cText.Substring(0, 42) + "...";
                        g.DrawString(cText, contentFont, new SolidBrush(Color.FromArgb(51, 65, 85)), calloutX + 32, curCardY);
                    }

                    curCardY += 22;

                    // Render Existing Replies (nếu có)
                    if (c.Replies != null && c.Replies.Count > 0)
                    {
                        for (int ri = 0; ri < c.Replies.Count; ri++)
                        {
                            using (Pen pSubDiv = new Pen(Color.FromArgb(241, 245, 249), 1f))
                            {
                                g.DrawLine(pSubDiv, calloutX + 16, curCardY, calloutX + 224, curCardY);
                            }
                            curCardY += 4;

                            Rectangle rAvaRect = new Rectangle(calloutX + 18, curCardY + 2, 16, 16);
                            using (SolidBrush rAvaBg = new SolidBrush(Color.FromArgb(37, 99, 235)))
                            using (Font rAvaFont = new Font("Segoe UI", 7.0f, FontStyle.Bold))
                            {
                                g.FillEllipse(rAvaBg, rAvaRect);
                                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                                {
                                    g.DrawString("U", rAvaFont, Brushes.White, new RectangleF(rAvaRect.X, rAvaRect.Y - 1, rAvaRect.Width, rAvaRect.Height), sf);
                                }
                            }

                            using (Font repFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
                            {
                                string repTxt = c.Replies[ri];
                                if (repTxt.Length > 38) repTxt = repTxt.Substring(0, 35) + "...";
                                g.DrawString(repTxt, repFont, new SolidBrush(Color.FromArgb(30, 41, 59)), calloutX + 38, curCardY + 2);
                            }
                            curCardY += 24;
                        }
                    }

                    // Render Inline Reply Box (khi đang gõ phản hồi)
                    if (isReplying)
                    {
                        using (Pen pSubDiv = new Pen(Color.FromArgb(241, 245, 249), 1f))
                        {
                            g.DrawLine(pSubDiv, calloutX + 8, curCardY, calloutX + 222, curCardY);
                        }
                        curCardY += 4;

                        Rectangle rAvaRect = new Rectangle(calloutX + 10, curCardY + 2, 18, 18);
                        using (SolidBrush rAvaBg = new SolidBrush(Color.FromArgb(220, 38, 38)))
                        using (Font rAvaFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
                        {
                            g.FillEllipse(rAvaBg, rAvaRect);
                            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            {
                                g.DrawString(authorInitial, rAvaFont, Brushes.White, new RectangleF(rAvaRect.X, rAvaRect.Y - 1, rAvaRect.Width, rAvaRect.Height), sf);
                            }
                        }

                        using (Font authFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        using (Font timeFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))
                        {
                            g.DrawString(authorName, authFont, new SolidBrush(Color.FromArgb(15, 23, 42)), calloutX + 32, curCardY);
                            SizeF aSz = g.MeasureString(authorName, authFont);
                            g.DrawString("A few seconds ago", timeFont, new SolidBrush(Color.FromArgb(100, 116, 139)), calloutX + 34 + (int)aSz.Width, curCardY + 1);
                        }

                        curCardY += 18;

                        using (Font inFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                        {
                            string draft = ReplyDraftText + "|";
                            g.DrawString(draft, inFont, new SolidBrush(Color.FromArgb(30, 41, 59)), calloutX + 32, curCardY);
                        }

                        curCardY += 20;
                    }

                    // Divider line before bottom actions
                    using (Pen pDiv = new Pen(Color.FromArgb(241, 245, 249), 1f))
                    {
                        g.DrawLine(pDiv, calloutX + 4, cardBox.Bottom - 26, calloutX + 226, cardBox.Bottom - 26);
                    }

                    // Bottom Action buttons: Reply & Resolve / Reopen
                    Rectangle replyRect = new Rectangle(calloutX + 102, cardBox.Bottom - 24, 58, 22);
                    Rectangle resolveRect = new Rectangle(calloutX + 162, cardBox.Bottom - 24, 64, 22);
                    CommentReplyBtnRects[c.Id] = replyRect;
                    CommentResolveBtnRects[c.Id] = resolveRect;

                    using (Font actFont = new Font("Segoe UI", 8.2f, FontStyle.Regular))
                    {
                        // Purple 💬↩ Reply
                        DrawReplyVectorIcon(g, replyRect.X + 2, replyRect.Y + 3, Color.FromArgb(124, 58, 237));
                        g.DrawString("Reply", actFont, new SolidBrush(Color.FromArgb(30, 64, 175)), replyRect.X + 18, replyRect.Y + 2);

                        // Green 💬✔ Resolve / Reopen
                        string resLabel = c.IsResolved ? "Reopen" : "Resolve";
                        DrawResolveVectorIcon(g, resolveRect.X + 2, resolveRect.Y + 3, Color.FromArgb(22, 163, 74));
                        g.DrawString(resLabel, actFont, new SolidBrush(Color.FromArgb(30, 64, 175)), resolveRect.X + 18, resolveRect.Y + 2);
                    }

                    CommentBadgeHitRects[c.Id] = cardBox;
                }
                else
                {
                    // ================= 1. NORMAL / COLLAPSED STATE (Image 1) =================
                    Rectangle avaRect = new Rectangle(calloutX, calloutY, 20, 20);
                    using (SolidBrush avaBg = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (Font avaFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        g.FillEllipse(avaBg, avaRect);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(authorInitial, avaFont, Brushes.White, new RectangleF(avaRect.X, avaRect.Y - 1, avaRect.Width, avaRect.Height), sf);
                        }
                    }

                    using (Font authFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                    {
                        g.DrawString(authorName, authFont, new SolidBrush(Color.FromArgb(15, 23, 42)), calloutX + 26, calloutY + 1);
                    }

                    CommentBadgeHitRects[c.Id] = new Rectangle(calloutX, calloutY, 120, 24);
                }
            }
        }

        private void DrawReplyVectorIcon(Graphics g, int x, int y, Color col)
        {
            using (Pen pBubble = new Pen(Color.FromArgb(100, 116, 139), 1.1f))
            using (Pen pArrow = new Pen(col, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                PointF[] bub = new PointF[] {
                    new PointF(x, y),
                    new PointF(x + 13, y),
                    new PointF(x + 13, y + 9),
                    new PointF(x + 6, y + 9),
                    new PointF(x + 3, y + 13),
                    new PointF(x + 3, y + 9),
                    new PointF(x, y + 9)
                };
                g.DrawPolygon(pBubble, bub);
                g.DrawArc(pArrow, x + 4, y + 2, 5, 5, 270, 180);
                g.DrawLine(pArrow, x + 5, y + 2, x + 3, y + 4.5f);
                g.DrawLine(pArrow, x + 3, y + 4.5f, x + 5, y + 7);
            }
        }

        private void DrawResolveVectorIcon(Graphics g, int x, int y, Color col)
        {
            using (Pen pBubble = new Pen(Color.FromArgb(100, 116, 139), 1.1f))
            using (Pen pCheck = new Pen(col, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                PointF[] bub = new PointF[] {
                    new PointF(x, y),
                    new PointF(x + 13, y),
                    new PointF(x + 13, y + 9),
                    new PointF(x + 6, y + 9),
                    new PointF(x + 3, y + 13),
                    new PointF(x + 3, y + 9),
                    new PointF(x, y + 9)
                };
                g.DrawPolygon(pBubble, bub);
                g.DrawLine(pCheck, x + 3, y + 5, x + 6, y + 8);
                g.DrawLine(pCheck, x + 6, y + 8, x + 11, y + 2.5f);
            }
        }

        private void DrawPageCanvas(Graphics g, int x, int y, int w, int h, RibbonBar ribbon, int pageNumber = 1, int totalPages = 1)
        {
            g.FillRectangle(Brushes.DarkGray, x + 5, y + 5, w, h);

            Color pageBg = (ribbon != null) ? ribbon.PageBgColor : Color.White;
            using (SolidBrush bgBrush = new SolidBrush(pageBg))
            {
                g.FillRectangle(bgBrush, x, y, w, h);
            }
            g.DrawRectangle(Pens.LightGray, x, y, w, h);

            // Real Header & Footer Presets Rendering (Blank, Three Columns, Austin, Banded)
            if (CurrentHeaderPreset != HeaderFooterPreset.None || IsEditingHeaderFooter || !string.IsNullOrEmpty(HeaderText))
            {
                if (IsEditingHeaderFooter)
                {
                    // Dashed boundary line & Tag box [ Đầu trang ] ONLY WHEN EDITING!
                    int dashY = y + (int)(45 * ZoomLevel);
                    using (Pen dashPen = new Pen(Color.FromArgb(160, 160, 160), 1.0f))
                    {
                        dashPen.DashStyle = DashStyle.Dash;
                        g.DrawLine(dashPen, x, dashY, x + w, dashY);
                    }

                    Rectangle tagRect = new Rectangle(x + (int)(5 * ZoomLevel), dashY - (int)(13 * ZoomLevel), (int)(70 * ZoomLevel), (int)(18 * ZoomLevel));
                    g.FillRectangle(Brushes.White, tagRect);
                    g.DrawRectangle(Pens.LightGray, tagRect);
                    using (Font tagFont = new Font("Segoe UI", 8.0f))
                    {
                        g.DrawString("Đầu trang", tagFont, Brushes.DimGray, tagRect.X + 6, tagRect.Y + 1);
                    }
                }

                // Unified Header Text resolution across all presets
                string hActiveStr = !string.IsNullOrEmpty(HeaderTitleText) ? HeaderTitleText : HeaderText;

                int leftM = (int)((ribbon != null ? ribbon.GetEffectiveLeftMarginInches() : 1.0f) * 54.0f * ZoomLevel);
                int rightM = (int)((ribbon != null ? ribbon.GetEffectiveRightMarginInches() : 1.0f) * 54.0f * ZoomLevel);

                if (CurrentHeaderPreset == HeaderFooterPreset.Banded)
                {
                    if (HeaderWrapMode != 6)
                    {
                        DrawBandedHeaderBanner(g, x, y, w, h, ribbon);
                    }
                }
                else if (CurrentHeaderPreset == HeaderFooterPreset.Austin)
                {
                    string aStr = string.IsNullOrEmpty(hActiveStr) ? "[Tiêu đề tài liệu]" : hActiveStr;
                    using (Font aFont = new Font("Segoe UI", 9.5f * ZoomLevel, FontStyle.Bold))
                    {
                        SizeF sz = g.MeasureString(aStr, aFont);
                        float txtX = x + leftM;
                        float txtY = y + (int)(18 * ZoomLevel);

                        if (IsEditingHeaderFooter)
                        {
                            // Text box container frame
                            Rectangle textFrame = new Rectangle((int)txtX - 4, (int)txtY - 2, (int)Math.Max(150 * ZoomLevel, sz.Width + 12), (int)sz.Height + 4);
                            using (Pen dashPen = new Pen(Color.SteelBlue) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            {
                                g.DrawRectangle(dashPen, textFrame);
                            }

                            // Quét khối / Bôi đen background highlight
                            if (IsEditingHeaderMode && IsHeaderTextSelected)
                            {
                                using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(197, 217, 241)))
                                {
                                    g.FillRectangle(selBrush, txtX, txtY, sz.Width, sz.Height);
                                }
                            }
                        }

                        g.DrawString(aStr, aFont, Brushes.DarkBlue, txtX, txtY);
                        g.DrawLine(Pens.LightGray, x + leftM, y + (int)(36 * ZoomLevel), x + w - rightM, y + (int)(36 * ZoomLevel));

                        if (IsEditingHeaderFooter && IsEditingHeaderMode && !IsHeaderTextSelected && CursorVisible)
                        {
                            float caretX = txtX + sz.Width + 2;
                            float caretY = txtY;
                            using (Pen blueCaret = new Pen(Color.DarkBlue, 2.0f))
                            {
                                g.DrawLine(blueCaret, caretX, caretY, caretX, caretY + (int)(16 * ZoomLevel));
                            }
                        }
                    }
                }
                else if (CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                {
                    string cStr1 = string.IsNullOrEmpty(ThreeColHeaderLeft) ? "[Nhập tại đây]" : ThreeColHeaderLeft;
                    string cStr2 = string.IsNullOrEmpty(ThreeColHeaderCenter) ? "[Nhập tại đây]" : ThreeColHeaderCenter;
                    string cStr3 = string.IsNullOrEmpty(ThreeColHeaderRight) ? "[Nhập tại đây]" : ThreeColHeaderRight;

                    using (Font tFont = new Font("Segoe UI", 9.0f * ZoomLevel, FontStyle.Regular))
                    {
                        SizeF sz1 = g.MeasureString(cStr1, tFont);
                        SizeF sz2 = g.MeasureString(cStr2, tFont);
                        SizeF sz3 = g.MeasureString(cStr3, tFont);

                        float x1 = x + leftM;
                        float x2 = x + (w - sz2.Width) / 2;
                        float x3 = x + w - rightM - sz3.Width;
                        float txtY = y + (int)(20 * ZoomLevel);

                        if (IsEditingHeaderFooter)
                        {
                            using (Pen dashPen = new Pen(Color.LightGray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            {
                                g.DrawRectangle(dashPen, (int)x1 - 4, (int)txtY - 2, (int)Math.Max(90 * ZoomLevel, sz1.Width + 8), (int)sz1.Height + 4);
                                g.DrawRectangle(dashPen, (int)x2 - 4, (int)txtY - 2, (int)Math.Max(90 * ZoomLevel, sz2.Width + 8), (int)sz2.Height + 4);
                                g.DrawRectangle(dashPen, (int)x3 - 4, (int)txtY - 2, (int)Math.Max(90 * ZoomLevel, sz3.Width + 8), (int)sz3.Height + 4);
                            }

                            if (IsEditingHeaderMode && IsHeaderTextSelected)
                            {
                                using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(197, 217, 241)))
                                {
                                    if (ActiveThreeColIndex == 0) g.FillRectangle(selBrush, x1, txtY, sz1.Width, sz1.Height);
                                    else if (ActiveThreeColIndex == 1) g.FillRectangle(selBrush, x2, txtY, sz2.Width, sz2.Height);
                                    else if (ActiveThreeColIndex == 2) g.FillRectangle(selBrush, x3, txtY, sz3.Width, sz3.Height);
                                }
                            }
                        }

                        g.DrawString(cStr1, tFont, ActiveThreeColIndex == 0 && IsEditingHeaderFooter ? Brushes.Navy : Brushes.DimGray, x1, txtY);
                        g.DrawString(cStr2, tFont, ActiveThreeColIndex == 1 && IsEditingHeaderFooter ? Brushes.Navy : Brushes.DimGray, x2, txtY);
                        g.DrawString(cStr3, tFont, ActiveThreeColIndex == 2 && IsEditingHeaderFooter ? Brushes.Navy : Brushes.DimGray, x3, txtY);

                        if (IsEditingHeaderFooter && IsEditingHeaderMode && !IsHeaderTextSelected && CursorVisible)
                        {
                            float caretX = 0;
                            if (ActiveThreeColIndex == 0) caretX = x1 + sz1.Width + 2;
                            else if (ActiveThreeColIndex == 1) caretX = x2 + sz2.Width + 2;
                            else if (ActiveThreeColIndex == 2) caretX = x3 + sz3.Width + 2;

                            using (Pen blackCaret = new Pen(Color.Navy, 2.0f))
                            {
                                g.DrawLine(blackCaret, caretX, txtY, caretX, txtY + (int)(16 * ZoomLevel));
                            }
                        }
                    }
                }
                else if (CurrentHeaderPreset == HeaderFooterPreset.Blank || !string.IsNullOrEmpty(hActiveStr))
                {
                    string hStr = string.IsNullOrEmpty(hActiveStr) ? "[Nhập tại đây]" : hActiveStr;
                    using (Font hFont = new Font("Segoe UI", 9.0f * ZoomLevel, FontStyle.Regular))
                    {
                        float txtX = x + (int)(40 * ZoomLevel);
                        float txtY = y + (int)(20 * ZoomLevel);
                        SizeF sz = g.MeasureString(hStr, hFont);

                        if (IsEditingHeaderFooter)
                        {
                            using (Pen dashPen = new Pen(Color.LightGray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            {
                                g.DrawRectangle(dashPen, (int)txtX - 4, (int)txtY - 2, (int)Math.Max(120 * ZoomLevel, sz.Width + 8), (int)sz.Height + 4);
                            }

                            if (IsEditingHeaderMode && IsHeaderTextSelected)
                            {
                                using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(197, 217, 241)))
                                {
                                    g.FillRectangle(selBrush, txtX, txtY, sz.Width, sz.Height);
                                }
                            }
                        }

                        g.DrawString(hStr, hFont, Brushes.DimGray, txtX, txtY);

                        if (IsEditingHeaderFooter && IsEditingHeaderMode && !IsHeaderTextSelected && CursorVisible)
                        {
                            float caretX = txtX + sz.Width + 2;
                            using (Pen blackCaret = new Pen(Color.Black, 2.0f))
                            {
                                g.DrawLine(blackCaret, caretX, txtY, caretX, txtY + (int)(16 * ZoomLevel));
                            }
                        }
                    }
                }
            }

            // Render Footnotes for this page
            if (DetailedFootnotes != null && DetailedFootnotes.Count > 0)
            {
                List<FootnoteItem> pageFootnotes = new List<FootnoteItem>();
                foreach (var fn in DetailedFootnotes)
                {
                    if (!fn.IsEndnote && fn.PageNumber == pageNumber)
                    {
                        pageFootnotes.Add(fn);
                    }
                }

                if (pageFootnotes.Count > 0)
                {
                    int fnBottomY = y + h - (int)(55 * ZoomLevel);
                    int fnLineY = fnBottomY - (int)(pageFootnotes.Count * 18 * ZoomLevel) - (int)(8 * ZoomLevel);
                    int leftMargin = (int)((ribbon != null ? ribbon.GetEffectiveLeftMarginInches() : 1.0f) * 54.0f * ZoomLevel);
                    int fnX = x + Math.Max(20, leftMargin);

                    // 1. Footnote Separator Line (~5cm / 150px)
                    using (Pen fnSepPen = new Pen(Color.FromArgb(160, 174, 192), 1.2f))
                    {
                        g.DrawLine(fnSepPen, fnX, fnLineY, fnX + (int)(150 * ZoomLevel), fnLineY);
                    }

                    // 2. Footnote list items
                    int curFnY = fnLineY + (int)(6 * ZoomLevel);
                    using (Font fnFont = new Font("Segoe UI", 8.5f * ZoomLevel, FontStyle.Regular))
                    using (SolidBrush fnBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    {
                        foreach (var pfn in pageFootnotes)
                        {
                            string fnStr = string.Format("{0} {1}", pfn.Marker, pfn.Text);
                            g.DrawString(fnStr, fnFont, fnBrush, fnX, curFnY);
                            curFnY += (int)(18 * ZoomLevel);
                        }
                    }
                }

                // Render Endnotes on the last page of document
                if (pageNumber == totalPages)
                {
                    List<FootnoteItem> endnotes = new List<FootnoteItem>();
                    foreach (var fn in DetailedFootnotes)
                    {
                        if (fn.IsEndnote)
                        {
                            endnotes.Add(fn);
                        }
                    }

                    if (endnotes.Count > 0)
                    {
                        int leftMargin = (int)((ribbon != null ? ribbon.GetEffectiveLeftMarginInches() : 1.0f) * 54.0f * ZoomLevel);
                        int rightMargin = (int)((ribbon != null ? ribbon.GetEffectiveRightMarginInches() : 1.0f) * 54.0f * ZoomLevel);
                        int enX = x + Math.Max(20, leftMargin);
                        int enBottomY = y + h - (int)(58 * ZoomLevel);
                        int enLineY = enBottomY - (int)(endnotes.Count * 18 * ZoomLevel) - (int)(20 * ZoomLevel);

                        // Endnotes Section Separator Line
                        using (Pen enSepPen = new Pen(Color.FromArgb(99, 102, 241), 1.5f))
                        {
                            g.DrawLine(enSepPen, enX, enLineY, x + w - Math.Max(20, rightMargin), enLineY);
                        }

                        using (Font hdrFont = new Font("Segoe UI", 7.5f * ZoomLevel, FontStyle.Bold))
                        using (SolidBrush hdrBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
                        {
                            g.DrawString("CHÚ THÍCH CUỐI TÀI LIỆU (ENDNOTES)", hdrFont, hdrBrush, enX, enLineY - (int)(14 * ZoomLevel));
                        }

                        int curEnY = enLineY + (int)(6 * ZoomLevel);
                        using (Font enFont = new Font("Segoe UI", 8.5f * ZoomLevel, FontStyle.Regular))
                        using (SolidBrush enBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
                        {
                            foreach (var pen in endnotes)
                            {
                                string enStr = string.Format("{0} {1}", pen.Marker, pen.Text);
                                g.DrawString(enStr, enFont, enBrush, enX, curEnY);
                                curEnY += (int)(18 * ZoomLevel);
                            }
                        }
                    }
                }
            }

            // Real Footer rendering
            if (CurrentFooterPreset != HeaderFooterPreset.None || ShowPageNumbers || !string.IsNullOrEmpty(FooterText))
            {
                if (IsEditingHeaderFooter)
                {
                    // Dashed boundary line & Tag box [ Chân trang ] ONLY WHEN EDITING!
                    using (Pen dashPen = new Pen(Color.FromArgb(160, 160, 160), 1))
                    {
                        dashPen.DashStyle = DashStyle.Dash;
                        g.DrawLine(dashPen, x, y + h - 45, x + w, y + h - 45);
                    }

                    Rectangle fTagRect = new Rectangle(x + 5, y + h - 45, 70, 18);
                    g.FillRectangle(Brushes.White, fTagRect);
                    g.DrawRectangle(Pens.LightGray, fTagRect);
                    using (Font tagFont = new Font("Segoe UI", 8.0f))
                    {
                        g.DrawString("Chân trang", tagFont, Brushes.DimGray, fTagRect.X + 6, fTagRect.Y + 1);
                    }
                }

                if (CurrentFooterPreset == HeaderFooterPreset.Banded || ShowPageNumbers)
                {
                    string pgStr = string.Format("{0}", pageNumber);
                    using (Font fFont = new Font("Segoe UI", 9.0f * ZoomLevel, FontStyle.Bold))
                    {
                        SizeF sz = g.MeasureString(pgStr, fFont);
                        g.DrawString(pgStr, fFont, Brushes.DarkBlue, x + (w - sz.Width) / 2, y + h - 30);
                    }
                }
                else if (CurrentFooterPreset == HeaderFooterPreset.Austin)
                {
                    using (Font fFont = new Font("Segoe UI", 9.0f * ZoomLevel, FontStyle.Bold))
                    {
                        g.DrawString("PAGE " + pageNumber, fFont, Brushes.DarkBlue, x + 40, y + h - 30);
                    }
                }
                else if (CurrentFooterPreset == HeaderFooterPreset.ThreeColumns)
                {
                    using (Font tFont = new Font("Segoe UI", 9.0f * ZoomLevel, FontStyle.Regular))
                    {
                        g.DrawString("[Type here]", tFont, Brushes.DimGray, x + 40, y + h - 30);
                        g.DrawString("[Type here]", tFont, Brushes.DimGray, x + w / 2 - 30, y + h - 30);
                        g.DrawString("[Type here]", tFont, Brushes.DimGray, x + w - 100, y + h - 30);
                    }
                }
                else if (CurrentFooterPreset == HeaderFooterPreset.Blank || !string.IsNullOrEmpty(FooterText))
                {
                    string fStr = string.IsNullOrEmpty(FooterText) ? "[Type here]" : FooterText;
                    using (Font fFont = new Font("Segoe UI", 9.0f * ZoomLevel, FontStyle.Regular))
                    {
                        g.DrawString(fStr, fFont, Brushes.DimGray, x + 40, y + h - 30);
                    }
                }
            }

            if (ribbon != null && ribbon.HasPageBorder)
            {
                using (Pen borderPen = new Pen(ribbon.PageBorderColor, Math.Max(1.0f, ribbon.PageBorderWidth * ZoomLevel)))
                {
                    if (ribbon.PageBorderStyleType == 2) // Shadow
                    {
                        using (SolidBrush shB = new SolidBrush(Color.FromArgb(100, 120, 120, 120)))
                        {
                            g.FillRectangle(shB, x + 16, y + h - 10, w - 20, 5);
                            g.FillRectangle(shB, x + w - 10, y + 16, 5, h - 20);
                        }
                    }
                    g.DrawRectangle(borderPen, x + 8, y + 8, w - 16, h - 16);
                    if (ribbon.PageBorderStyleType == 3 || ribbon.PageBorderStyleType == 4) // 3-D / Double
                    {
                        g.DrawRectangle(borderPen, x + 12, y + 12, w - 24, h - 24);
                    }
                }
            }

            if (ribbon != null && !string.IsNullOrEmpty(ribbon.WatermarkText))
            {
                GraphicsState state = g.Save();
                g.SetClip(new Rectangle(x, y, w, h));

                float baseFontSize = (ribbon.WatermarkIsDiagonal ? 50.0f : 44.0f) * ZoomLevel;
                float maxAvailableW = ribbon.WatermarkIsDiagonal ? (float)Math.Sqrt(w * w + h * h) * 0.72f : (w * 0.82f);

                using (Font testFont = new Font("Arial", baseFontSize, FontStyle.Bold))
                {
                    SizeF measuredSz = g.MeasureString(ribbon.WatermarkText, testFont);
                    float finalFontSize = baseFontSize;
                    if (measuredSz.Width > maxAvailableW && measuredSz.Width > 0)
                    {
                        finalFontSize = Math.Max(12.0f * ZoomLevel, baseFontSize * (maxAvailableW / measuredSz.Width));
                    }

                    using (Font wmFont = new Font("Arial", finalFontSize, FontStyle.Bold))
                    using (SolidBrush wmBrush = new SolidBrush(Color.FromArgb(38, 120, 120, 120)))
                    {
                        g.TranslateTransform(x + w / 2f, y + h / 2f);
                        if (ribbon.WatermarkIsDiagonal)
                        {
                            g.RotateTransform(-45f);
                        }
                        SizeF sz = g.MeasureString(ribbon.WatermarkText, wmFont);
                        g.DrawString(ribbon.WatermarkText, wmFont, wmBrush, -sz.Width / 2f, -sz.Height / 2f);
                    }
                }
                g.Restore(state);
            }

            // Live Ghost Table Insert Preview Overlay
            if (TablePreviewCols > 0 && TablePreviewRows > 0)
            {
                int cols = TablePreviewCols;
                int rows = TablePreviewRows;

                int leftM = (int)(ribbon.GetEffectiveLeftMarginInches() * 72.0f * ZoomLevel);
                int rightM = (int)(ribbon.GetEffectiveRightMarginInches() * 72.0f * ZoomLevel);
                int topM = (int)(ribbon.GetEffectiveTopMarginInches() * 72.0f * ZoomLevel);

                float tw = w - leftM - rightM;
                float th = Math.Min(h * 0.40f, rows * (28f * ZoomLevel));
                float tx = x + leftM;
                float ty = y + topM + (int)(90 * ZoomLevel);

                float cellW = tw / cols;
                float cellH = th / rows;

                using (SolidBrush headerTopBrush = new SolidBrush(Color.FromArgb(170, 37, 99, 235)))
                using (SolidBrush rowOddBrush = new SolidBrush(Color.FromArgb(120, 219, 234, 254)))
                using (SolidBrush rowEvenBrush = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                using (Pen gridPen = new Pen(Color.FromArgb(200, 37, 99, 235), 1.2f))
                using (Pen outerPen = new Pen(Color.FromArgb(240, 29, 78, 216), 2.0f))
                {
                    g.FillRectangle(headerTopBrush, tx, ty, tw, cellH);
                    for (int r = 1; r < rows; r++)
                    {
                        SolidBrush b = (r % 2 == 1) ? rowOddBrush : rowEvenBrush;
                        g.FillRectangle(b, tx, ty + r * cellH, tw, cellH);
                    }
                    for (int r = 0; r <= rows; r++)
                    {
                        g.DrawLine(gridPen, tx, ty + r * cellH, tx + tw, ty + r * cellH);
                    }
                    for (int c = 0; c <= cols; c++)
                    {
                        g.DrawLine(gridPen, tx + c * cellW, ty, tx + c * cellW, ty + th);
                    }
                    g.DrawRectangle(outerPen, tx, ty, tw, th);
                }
            }
        }

        private void DrawBandedHeaderBanner(Graphics g, int x, int y, int w, int h, RibbonBar ribbon)
        {
            int leftM = (int)(ribbon.GetEffectiveLeftMarginInches() * 54.0f * ZoomLevel);
            int rightM = (int)(ribbon.GetEffectiveRightMarginInches() * 54.0f * ZoomLevel);
            int bX = x + leftM + (int)(HeaderBannerXOffset * ZoomLevel);
            int bW = Math.Max((int)(50 * ZoomLevel), w - leftM - rightM + (int)(HeaderBannerWidthDelta * ZoomLevel));
            int bH = Math.Max((int)(16 * ZoomLevel), (int)(28 * ZoomLevel) + (int)(HeaderBannerHeightDelta * ZoomLevel));
            int bY = y + (int)(10 * ZoomLevel) + (int)(HeaderBannerYOffset * ZoomLevel);
            Rectangle bannerRect = new Rectangle(bX, bY, bW, bH);

            // 100% Solid Opaque Blue Banner (Màu đặc nguyên bản, phẳng và không shadow)
            using (SolidBrush bannerBrush = new SolidBrush(Color.FromArgb(67, 120, 195)))
            {
                g.FillRectangle(bannerBrush, bannerRect);
            }

            string hActiveStr = !string.IsNullOrEmpty(HeaderTitleText) ? HeaderTitleText : HeaderText;
            string tStr = string.IsNullOrEmpty(hActiveStr) ? "TIÊU ĐỀ ĐẦU TRANG" : hActiveStr;
            using (Font titleFont = new Font("Segoe UI", 10.0f * ZoomLevel, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(tStr, titleFont);
                float txtX = bannerRect.X + (bannerRect.Width - sz.Width) / 2;
                float txtY = bannerRect.Y + (bannerRect.Height - sz.Height) / 2;

                if (IsEditingHeaderFooter)
                {
                    // Draw Text Box Container Frame around text inside banner
                    Rectangle textFrame = new Rectangle((int)txtX - 6, (int)txtY - 2, (int)sz.Width + 12, (int)sz.Height + 4);
                    using (Pen dashPen = new Pen(Color.FromArgb(200, 255, 255, 255)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        g.DrawRectangle(dashPen, textFrame);
                    }

                    // Quét khối / bôi đen text selection background if selected
                    if (IsEditingHeaderMode && IsHeaderTextSelected)
                    {
                        using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(140, 0, 45, 125)))
                        {
                            g.FillRectangle(selBrush, txtX, txtY, sz.Width, sz.Height);
                        }
                    }
                }

                g.DrawString(tStr, titleFont, Brushes.White, txtX, txtY);

                // Blinking Caret Indicator in Header Banner (only when not auto-selected)
                if (IsEditingHeaderFooter && IsEditingHeaderMode && !IsHeaderTextSelected && CursorVisible)
                {
                    float caretX = txtX + sz.Width + 2;
                    using (Pen whiteCaret = new Pen(Color.White, 2.0f))
                    {
                        g.DrawLine(whiteCaret, caretX, txtY, caretX, txtY + sz.Height);
                    }
                }
            }

            // Selection handle dots & Title tag & Layout button
            if (IsEditingHeaderFooter)
            {
                g.DrawRectangle(Pens.White, bannerRect);
                Rectangle handleTag = new Rectangle(bannerRect.X + bannerRect.Width / 2 - 30, bannerRect.Y - 14, 60, 14);
                g.FillRectangle(Brushes.White, handleTag);
                g.DrawRectangle(Pens.Gray, handleTag);
                using (Font hfTagFont = new Font("Segoe UI", 7.5f))
                {
                    g.DrawString(":: Tiêu đề", hfTagFont, Brushes.DimGray, handleTag.X + 4, handleTag.Y);
                }

                // 8 White Circular Selection Handles around banner
                Point[] handlePts = new Point[]
                {
                    new Point(bannerRect.Left, bannerRect.Top),
                    new Point(bannerRect.Left + bannerRect.Width / 2, bannerRect.Top),
                    new Point(bannerRect.Right, bannerRect.Top),
                    new Point(bannerRect.Left, bannerRect.Top + bannerRect.Height / 2),
                    new Point(bannerRect.Right, bannerRect.Top + bannerRect.Height / 2),
                    new Point(bannerRect.Left, bannerRect.Bottom),
                    new Point(bannerRect.Left + bannerRect.Width / 2, bannerRect.Bottom),
                    new Point(bannerRect.Right, bannerRect.Bottom)
                };
                for (int hIdx = 0; hIdx < 8; hIdx++)
                {
                    Rectangle handleCircle = new Rectangle(handlePts[hIdx].X - 4, handlePts[hIdx].Y - 4, 8, 8);
                    g.FillEllipse(Brushes.White, handleCircle);
                    g.DrawEllipse(Pens.RoyalBlue, handleCircle);
                }

                // Top rotation handle circular icon 🔄
                Rectangle rotateHandle = new Rectangle(bannerRect.Left + bannerRect.Width / 2 - 8, bannerRect.Top - 26, 16, 16);
                g.FillEllipse(Brushes.White, rotateHandle);
                g.DrawEllipse(Pens.DimGray, rotateHandle);
                using (Font rFont = new Font("Segoe UI Symbol", 8f))
                {
                    g.DrawString("🔄", rFont, Brushes.DarkSlateGray, rotateHandle.X + 1, rotateHandle.Y + 1);
                }

                // Layout Options Button [ ⌒ ] (Top-Right)
                Rectangle wrapBtnRect = new Rectangle(bannerRect.Right + 4, bannerRect.Top, 22, 22);
                using (SolidBrush wBtnBg = new SolidBrush(Color.FromArgb(245, 247, 250)))
                using (Pen wBtnPen = new Pen(Color.FromArgb(180, 195, 210), 1.0f))
                {
                    g.FillRectangle(wBtnBg, wrapBtnRect);
                    g.DrawRectangle(wBtnPen, wrapBtnRect);
                    g.DrawLine(Pens.DimGray, wrapBtnRect.X + 3, wrapBtnRect.Y + 4, wrapBtnRect.X + 19, wrapBtnRect.Y + 4);
                    g.DrawArc(new Pen(Color.SteelBlue, 1.5f), wrapBtnRect.X + 5, wrapBtnRect.Y + 6, 12, 10, 180, 180);
                    g.DrawLine(Pens.DimGray, wrapBtnRect.X + 3, wrapBtnRect.Y + 17, wrapBtnRect.X + 19, wrapBtnRect.Y + 17);
                }

                // Smart Alignment Green Guide Lines (Đường gióng căn chỉnh màu xanh lá cây chuẩn MS Word)
                if (IsDraggingHeaderBanner || IsResizingHeaderBanner)
                {
                    using (Pen greenGuidePen = new Pen(Color.FromArgb(34, 197, 94), 1.2f))
                    {
                        int leftMarginLineX = x + leftM;
                        int rightMarginLineX = x + w - rightM;
                        int pageCenterX = x + w / 2;
                        int topGuidelineY = y + (int)(45 * ZoomLevel);

                        // Left margin guide line
                        if (Math.Abs(bannerRect.Left - leftMarginLineX) <= 3)
                        {
                            g.DrawLine(greenGuidePen, leftMarginLineX, y, leftMarginLineX, y + h);
                        }

                        // Center alignment guide line
                        if (Math.Abs((bannerRect.Left + bannerRect.Right) / 2 - pageCenterX) <= 3)
                        {
                            g.DrawLine(greenGuidePen, pageCenterX, y, pageCenterX, y + h);
                        }

                        // Right margin guide line
                        if (Math.Abs(bannerRect.Right - rightMarginLineX) <= 3)
                        {
                            g.DrawLine(greenGuidePen, rightMarginLineX, y, rightMarginLineX, y + h);
                        }

                        // Top header boundary guide line
                        if (Math.Abs(bannerRect.Top - (y + (int)(10 * ZoomLevel))) <= 3 || Math.Abs(bannerRect.Bottom - topGuidelineY) <= 3)
                        {
                            g.DrawLine(greenGuidePen, x, topGuidelineY, x + w, topGuidelineY);
                        }
                    }
                }
            }
        }

        private void DrawVectorArch(Graphics g, Pen archPen, float x, float y, float w, float h)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(x, y, w, h, 180, 180);
                path.AddLine(x + w, y + h / 2, x + w, y + h);
                path.AddLine(x, y + h / 2, x, y + h);
                g.DrawPath(archPen, path);
            }
        }

        private void DrawVectorWrapCard(Graphics g, int wrapMode, Rectangle box, bool isSelected)
        {
            if (isSelected)
            {
                using (SolidBrush selBg = new SolidBrush(Color.FromArgb(215, 220, 225)))
                {
                    g.FillRectangle(selBg, box);
                }
                using (Pen selBorder = new Pen(Color.FromArgb(120, 125, 130), 1.2f))
                {
                    g.DrawRectangle(selBorder, box);
                }
            }
            else
            {
                using (Pen borderPen = new Pen(Color.FromArgb(218, 224, 230), 1.0f))
                {
                    g.DrawRectangle(borderPen, box);
                }
            }

            using (Pen bluePen = new Pen(Color.FromArgb(0, 120, 215), 1.6f))
            using (Pen darkArchPen = new Pen(Color.FromArgb(40, 40, 40), 3.0f))
            using (Pen ghostArchPen = new Pen(Color.FromArgb(180, 185, 192), 3.0f))
            {
                float archX = box.X + (box.Width - 14) / 2.0f;
                float archY = box.Y + (box.Height - 12) / 2.0f + 1;

                if (wrapMode == 0) // In Line With Text
                {
                    float ax = box.X + 6;
                    float ay = box.Y + 12;
                    g.DrawLine(bluePen, box.X + 5, box.Y + 7, box.X + 22, box.Y + 7);
                    DrawVectorArch(g, darkArchPen, ax, ay, 13, 12);
                    g.DrawLine(bluePen, box.X + 22, box.Y + 17, box.X + 36, box.Y + 17);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 26, box.X + 36, box.Y + 26);
                }
                else if (wrapMode == 1) // Square (Vuông)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 11, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 11, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 2) // Tight (Chặt)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 12, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 30, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 11, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 3) // Through (Xuyên qua)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 13, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 29, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 11, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 4) // Top and Bottom (Trên và Dưới)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 5) // Behind Text (Dưới văn bản)
                {
                    DrawVectorArch(g, ghostArchPen, archX, archY, 13, 12);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                }
                else if (wrapMode == 6) // In Front of Text (Trước văn bản)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
            }
        }

        private void DrawVectorRadioButton(Graphics g, int x, int y, bool isChecked, string label, Font font)
        {
            Rectangle rCircle = new Rectangle(x, y + 2, 12, 12);
            using (Pen rPen = new Pen(isChecked ? Color.FromArgb(0, 120, 215) : Color.FromArgb(140, 145, 150), 1.2f))
            {
                g.DrawEllipse(rPen, rCircle);
                if (isChecked)
                {
                    using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    {
                        g.FillEllipse(dotBrush, x + 3, y + 5, 6, 6);
                    }
                }
            }
            g.DrawString(label, font, isChecked ? new SolidBrush(Color.FromArgb(20, 20, 20)) : new SolidBrush(Color.FromArgb(70, 70, 70)), x + 16, y);
        }

        private void DrawVectorInfoIcon(Graphics g, int x, int y)
        {
            Rectangle infoCircle = new Rectangle(x, y, 11, 11);
            using (Pen infoPen = new Pen(Color.FromArgb(0, 120, 215), 1.0f))
            using (Font iFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            {
                g.DrawEllipse(infoPen, infoCircle);
                g.DrawString("i", iFont, new SolidBrush(Color.FromArgb(0, 120, 215)), x + 2.5f, y - 0.5f);
            }
        }

        public void DrawDocumentChart(Graphics g, DocChartObject chart, int chartX, int chartY, int chartW, int chartH, RibbonBar ribbon)
        {
            chart.LastBounds = new Rectangle(chartX, chartY, chartW, chartH);
            ActiveChartLastBounds = chart.LastBounds;

            Color bgColor = Color.White;
            Color borderColor = Color.FromArgb(218, 224, 233);
            Color textCol = Color.FromArgb(40, 40, 40);
            Color gridCol = Color.FromArgb(235, 238, 242);

            if (chart.StyleThemeIndex == 1) // Dark Slate Theme
            {
                bgColor = Color.FromArgb(30, 41, 59);
                borderColor = Color.FromArgb(51, 65, 85);
                textCol = Color.White;
                gridCol = Color.FromArgb(51, 65, 85);
            }
            else if (chart.StyleThemeIndex == 2) // Soft Modern Gray
            {
                bgColor = Color.FromArgb(248, 250, 252);
                borderColor = Color.FromArgb(203, 213, 225);
                textCol = Color.FromArgb(30, 41, 59);
                gridCol = Color.FromArgb(226, 232, 240);
            }

            using (SolidBrush bgB = new SolidBrush(bgColor))
            using (Pen borderP = new Pen(borderColor, 1.2f))
            {
                g.FillRectangle(bgB, chartX, chartY, chartW, chartH);
                g.DrawRectangle(borderP, chartX, chartY, chartW, chartH);
            }

            // Title Box
            int titleH = 0;
            if (chart.ShowTitle && !string.IsNullOrEmpty(chart.Data.Title))
            {
                using (Font tFont = new Font("Segoe UI", 10.5f * ZoomLevel, FontStyle.Bold))
                using (SolidBrush tBrush = new SolidBrush(textCol))
                {
                    SizeF tSz = g.MeasureString(chart.Data.Title, tFont);
                    float tx = chartX + (chartW - tSz.Width) / 2;
                    float ty = chartY + (int)(8 * ZoomLevel);
                    g.DrawString(chart.Data.Title, tFont, tBrush, tx, ty);
                    titleH = (int)(tSz.Height + 10 * ZoomLevel);

                    if (chart.IsSelected)
                    {
                        Rectangle tBox = new Rectangle((int)tx - 4, (int)ty - 2, (int)tSz.Width + 8, (int)tSz.Height + 4);
                        using (Pen dashPen = new Pen(Color.FromArgb(120, 180, 240)) { DashStyle = DashStyle.Dash })
                        {
                            g.DrawRectangle(dashPen, tBox);
                        }
                    }
                }
            }
            else
            {
                titleH = (int)(12 * ZoomLevel);
            }

            // Color Palettes
            Color[][] palettes = new Color[][]
            {
                new Color[] { Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0), Color.FromArgb(112, 173, 71), Color.FromArgb(91, 155, 213) },
                new Color[] { Color.FromArgb(112, 48, 160), Color.FromArgb(0, 176, 240), Color.FromArgb(0, 176, 80), Color.FromArgb(255, 192, 0), Color.FromArgb(192, 0, 0), Color.FromArgb(237, 125, 49) },
                new Color[] { Color.FromArgb(31, 78, 120), Color.FromArgb(47, 85, 151), Color.FromArgb(65, 113, 156), Color.FromArgb(142, 169, 219), Color.FromArgb(180, 198, 231), Color.FromArgb(217, 226, 243) }
            };
            Color[] activePalette = palettes[Math.Min(palettes.Length - 1, Math.Max(0, chart.ColorPaletteIndex))];

            // Determine Data Min / Max
            double maxVal = 1.0;
            if (chart.Data.Values != null && chart.Data.Values.Count > 0)
            {
                foreach (var row in chart.Data.Values)
                {
                    if (row == null) continue;
                    foreach (var v in row) { if (v > maxVal) maxVal = v; }
                }
            }
            if (maxVal <= 5.0) maxVal = 5.0;
            else if (maxVal <= 10.0) maxVal = 10.0;
            else if (maxVal <= 25.0) maxVal = 25.0;
            else if (maxVal <= 50.0) maxVal = 50.0;
            else if (maxVal <= 100.0) maxVal = 100.0;
            else maxVal = Math.Ceiling(maxVal / 50.0) * 50.0;

            int legendH = chart.ShowLegend ? (int)(28 * ZoomLevel) : (int)(10 * ZoomLevel);
            int plotX = chartX + (int)(42 * ZoomLevel);
            int plotY = chartY + titleH;
            int plotW = chartW - (int)(60 * ZoomLevel);
            int plotH = chartH - titleH - legendH - (int)(18 * ZoomLevel);

            if (plotW > 50 && plotH > 30)
            {
                if (chart.Data.ChartType == "Pie" || chart.Data.ChartType == "Doughnut")
                {
                    int pieSize = Math.Min(plotW, plotH);
                    Rectangle pieRect = new Rectangle(plotX + (plotW - pieSize) / 2, plotY + (plotH - pieSize) / 2, pieSize, pieSize);

                    double totalPie = 0;
                    if (chart.Data.Values != null)
                    {
                        foreach (var row in chart.Data.Values)
                        {
                            if (row != null && row.Count > 0) totalPie += Math.Max(0, row[0]);
                        }
                    }
                    if (totalPie <= 0) totalPie = 1.0;

                    float startAngle = 0f;
                    int numSlices = chart.Data.Categories != null ? chart.Data.Categories.Count : 0;
                    for (int k = 0; k < numSlices; k++)
                    {
                        double val = (chart.Data.Values != null && k < chart.Data.Values.Count && chart.Data.Values[k].Count > 0) ? Math.Max(0, chart.Data.Values[k][0]) : 1.0;
                        float sweep = (float)(val / totalPie * 360.0);
                        if (sweep > 0)
                        {
                            Color sliceCol = activePalette[k % activePalette.Length];
                            using (SolidBrush sBrush = new SolidBrush(sliceCol))
                            using (Pen whiteBorder = new Pen(bgColor, 1.5f))
                            {
                                g.FillPie(sBrush, pieRect, startAngle, sweep);
                                g.DrawPie(whiteBorder, pieRect, startAngle, sweep);
                            }
                            startAngle += sweep;
                        }
                    }

                    if (chart.Data.ChartType == "Doughnut")
                    {
                        int holeSize = (int)(pieSize * 0.48f);
                        Rectangle holeRect = new Rectangle(pieRect.X + (pieSize - holeSize) / 2, pieRect.Y + (pieSize - holeSize) / 2, holeSize, holeSize);
                        using (SolidBrush holeB = new SolidBrush(bgColor))
                        using (Pen hPen = new Pen(borderColor, 1f))
                        {
                            g.FillEllipse(holeB, holeRect);
                            g.DrawEllipse(hPen, holeRect);
                        }
                    }
                }
                else // Cartesian Charts
                {
                    using (Pen gridP = new Pen(gridCol, 1f))
                    using (Font axFont = new Font("Segoe UI", 7.5f * ZoomLevel, FontStyle.Regular))
                    using (SolidBrush axBrush = new SolidBrush(textCol))
                    {
                        for (int gr = 0; gr <= 5; gr++)
                        {
                            int gy = plotY + (int)(gr * (plotH / 5.0f));
                            if (chart.ShowGridlines)
                            {
                                g.DrawLine(gridP, plotX, gy, plotX + plotW, gy);
                            }
                            double yVal = maxVal * (5 - gr) / 5.0;
                            string yStr = yVal >= 10 ? yVal.ToString("0") : yVal.ToString("0.#");
                            SizeF ySz = g.MeasureString(yStr, axFont);
                            g.DrawString(yStr, axFont, axBrush, plotX - ySz.Width - 4, gy - ySz.Height / 2);
                        }

                        using (Pen axLinePen = new Pen(Color.FromArgb(160, 160, 160), 1f))
                        {
                            g.DrawLine(axLinePen, plotX, plotY + plotH, plotX + plotW, plotY + plotH);
                            g.DrawLine(axLinePen, plotX, plotY, plotX, plotY + plotH);
                        }
                    }

                    int numCats = chart.Data.Categories != null ? chart.Data.Categories.Count : 0;
                    int numSeries = chart.Data.SeriesNames != null ? chart.Data.SeriesNames.Count : 0;

                    if (numCats > 0 && numSeries > 0)
                    {
                        float catW = (float)plotW / numCats;

                        if (chart.Data.ChartType == "Line")
                        {
                            for (int s = 0; s < numSeries; s++)
                            {
                                Color serCol = activePalette[s % activePalette.Length];
                                List<PointF> pts = new List<PointF>();
                                for (int c = 0; c < numCats; c++)
                                {
                                    double val = (c < chart.Data.Values.Count && s < chart.Data.Values[c].Count) ? chart.Data.Values[c][s] : 0;
                                    float px = plotX + c * catW + catW / 2.0f;
                                    float py = plotY + plotH - (float)(val / maxVal * plotH);
                                    pts.Add(new PointF(px, py));
                                }

                                if (pts.Count > 1)
                                {
                                    using (Pen linePen = new Pen(serCol, 2.5f * ZoomLevel))
                                    {
                                        g.DrawLines(linePen, pts.ToArray());
                                    }
                                }
                                foreach (var pt in pts)
                                {
                                    using (SolidBrush dotBrush = new SolidBrush(serCol))
                                    {
                                        g.FillEllipse(dotBrush, pt.X - 4 * ZoomLevel, pt.Y - 4 * ZoomLevel, 8 * ZoomLevel, 8 * ZoomLevel);
                                        g.FillEllipse(Brushes.White, pt.X - 2 * ZoomLevel, pt.Y - 2 * ZoomLevel, 4 * ZoomLevel, 4 * ZoomLevel);
                                    }
                                }
                            }
                        }
                        else if (chart.Data.ChartType == "Area")
                        {
                            for (int s = 0; s < numSeries; s++)
                            {
                                Color serCol = activePalette[s % activePalette.Length];
                                List<PointF> pts = new List<PointF>();
                                pts.Add(new PointF(plotX, plotY + plotH));
                                for (int c = 0; c < numCats; c++)
                                {
                                    double val = (c < chart.Data.Values.Count && s < chart.Data.Values[c].Count) ? chart.Data.Values[c][s] : 0;
                                    float px = plotX + c * catW + catW / 2.0f;
                                    float py = plotY + plotH - (float)(val / maxVal * plotH);
                                    pts.Add(new PointF(px, py));
                                }
                                pts.Add(new PointF(plotX + plotW, plotY + plotH));

                                using (SolidBrush areaB = new SolidBrush(Color.FromArgb(120, serCol.R, serCol.G, serCol.B)))
                                using (Pen areaP = new Pen(serCol, 2.0f * ZoomLevel))
                                {
                                    g.FillPolygon(areaB, pts.ToArray());
                                    if (pts.Count > 2)
                                    {
                                        PointF[] linePts = new PointF[numCats];
                                        for (int k = 0; k < numCats; k++) linePts[k] = pts[k + 1];
                                        g.DrawLines(areaP, linePts);
                                    }
                                }
                            }
                        }
                        else // Column Charts (Clustered / Stacked / 100%)
                        {
                            float totalBarGroupW = catW * 0.75f;
                            float barW = totalBarGroupW / numSeries;

                            for (int c = 0; c < numCats; c++)
                            {
                                float groupStartX = plotX + c * catW + (catW - totalBarGroupW) / 2.0f;

                                for (int s = 0; s < numSeries; s++)
                                {
                                    double val = (c < chart.Data.Values.Count && s < chart.Data.Values[c].Count) ? chart.Data.Values[c][s] : 0;
                                    float bH = (float)(val / maxVal * plotH);
                                    float bx = groupStartX + s * barW;
                                    float by = plotY + plotH - bH;

                                    Color serCol = activePalette[s % activePalette.Length];
                                    using (SolidBrush barB = new SolidBrush(serCol))
                                    using (Pen whiteBorder = new Pen(Color.FromArgb(240, 240, 240), 1f))
                                    {
                                        g.FillRectangle(barB, bx, by, Math.Max(2, barW - 1), bH);
                                        g.DrawRectangle(whiteBorder, bx, by, Math.Max(2, barW - 1), bH);
                                    }

                                    if (chart.ShowDataLabels && bH > 14)
                                    {
                                        using (Font dlFont = new Font("Segoe UI", 7.0f * ZoomLevel, FontStyle.Bold))
                                        using (SolidBrush dlBrush = new SolidBrush(textCol))
                                        {
                                            string dStr = val >= 10 ? val.ToString("0") : val.ToString("0.#");
                                            SizeF dSz = g.MeasureString(dStr, dlFont);
                                            g.DrawString(dStr, dlFont, dlBrush, bx + (barW - dSz.Width) / 2, by - dSz.Height - 1);
                                        }
                                    }
                                }
                            }
                        }

                        using (Font catFont = new Font("Segoe UI", 8.0f * ZoomLevel, FontStyle.Regular))
                        using (SolidBrush catBrush = new SolidBrush(textCol))
                        {
                            for (int c = 0; c < numCats; c++)
                            {
                                string cName = chart.Data.Categories[c];
                                SizeF cSz = g.MeasureString(cName, catFont);
                                float cx = plotX + c * catW + (catW - cSz.Width) / 2.0f;
                                float cy = plotY + plotH + (int)(5 * ZoomLevel);
                                g.DrawString(cName, catFont, catBrush, cx, cy);
                            }
                        }
                    }
                }
            }

            // Legend at Bottom
            if (chart.ShowLegend && chart.Data.SeriesNames != null && chart.Data.SeriesNames.Count > 0)
            {
                using (Font lFont = new Font("Segoe UI", 8.5f * ZoomLevel, FontStyle.Regular))
                using (SolidBrush lBrush = new SolidBrush(textCol))
                {
                    int legY = chartY + chartH - (int)(22 * ZoomLevel);
                    float lx = chartX + (int)(20 * ZoomLevel);

                    int numLegs = (chart.Data.ChartType == "Pie" || chart.Data.ChartType == "Doughnut") ? (chart.Data.Categories != null ? chart.Data.Categories.Count : 0) : chart.Data.SeriesNames.Count;

                    for (int s = 0; s < numLegs; s++)
                    {
                        string sName = (chart.Data.ChartType == "Pie" || chart.Data.ChartType == "Doughnut") ? chart.Data.Categories[s] : chart.Data.SeriesNames[s];
                        Color col = activePalette[s % activePalette.Length];
                        using (SolidBrush cb = new SolidBrush(col))
                        {
                            g.FillRectangle(cb, lx, legY + (int)(2 * ZoomLevel), (int)(10 * ZoomLevel), (int)(10 * ZoomLevel));
                        }
                        g.DrawString(sName, lFont, lBrush, lx + (int)(14 * ZoomLevel), legY);
                        SizeF lSz = g.MeasureString(sName, lFont);
                        lx += lSz.Width + (int)(22 * ZoomLevel);
                        if (lx > chartX + chartW - (int)(30 * ZoomLevel)) break;
                    }
                }
            }

            // Selection Frame & 8 Handles & 4 Floating Action Buttons
            if (chart.IsSelected)
            {
                using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                {
                    g.DrawRectangle(selPen, chartX - 1, chartY - 1, chartW + 2, chartH + 2);
                }

                int hs = (int)(7 * ZoomLevel);
                Point[] handlePts = new Point[]
                {
                    new Point(chartX - hs/2, chartY - hs/2),
                    new Point(chartX + chartW/2 - hs/2, chartY - hs/2),
                    new Point(chartX + chartW - hs/2, chartY - hs/2),
                    new Point(chartX + chartW - hs/2, chartY + chartH/2 - hs/2),
                    new Point(chartX + chartW - hs/2, chartY + chartH - hs/2),
                    new Point(chartX + chartW/2 - hs/2, chartY + chartH - hs/2),
                    new Point(chartX - hs/2, chartY + chartH - hs/2),
                    new Point(chartX - hs/2, chartY + chartH/2 - hs/2)
                };

                using (SolidBrush hBrush = new SolidBrush(Color.White))
                using (Pen hPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                {
                    foreach (var hp in handlePts)
                    {
                        g.FillRectangle(hBrush, hp.X, hp.Y, hs, hs);
                        g.DrawRectangle(hPen, hp.X, hp.Y, hs, hs);
                    }
                }

                DrawChartFloatingButtons(g, chart, new Rectangle(chartX, chartY, chartW, chartH));
            }
        }

        private void DrawChartFloatingButtons(Graphics g, DocChartObject chart, Rectangle chartRect)
        {
            int btnSize = 22;
            int btnX = chartRect.Right + 6;
            int startBtnY = chartRect.Top + 2;

            LayoutBtnChartRect = new Rectangle(btnX, startBtnY, btnSize, btnSize);
            PlusBtnChartRect = new Rectangle(btnX, startBtnY + 28, btnSize, btnSize);
            StyleBtnChartRect = new Rectangle(btnX, startBtnY + 56, btnSize, btnSize);
            FilterBtnChartRect = new Rectangle(btnX, startBtnY + 84, btnSize, btnSize);

            using (Pen borderPen = new Pen(Color.FromArgb(180, 200, 220), 1.2f))
            {
                // 1. [ ⌒ ] Layout Options
                g.FillRectangle(chart.ShowLayoutOptions ? new SolidBrush(Color.FromArgb(215, 235, 255)) : Brushes.White, LayoutBtnChartRect);
                g.DrawRectangle(borderPen, LayoutBtnChartRect);
                using (Pen archP = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                {
                    DrawVectorArch(g, archP, LayoutBtnChartRect.X + 4, LayoutBtnChartRect.Y + 5, 14, 11);
                }

                // 2. [ + ] Chart Elements
                g.FillRectangle(chart.ShowElementsMenu ? new SolidBrush(Color.FromArgb(215, 235, 255)) : Brushes.White, PlusBtnChartRect);
                g.DrawRectangle(borderPen, PlusBtnChartRect);
                using (Pen plusP = new Pen(Color.FromArgb(16, 124, 65), 2f))
                {
                    g.DrawLine(plusP, PlusBtnChartRect.X + 6, PlusBtnChartRect.Y + 11, PlusBtnChartRect.X + 16, PlusBtnChartRect.Y + 11);
                    g.DrawLine(plusP, PlusBtnChartRect.X + 11, PlusBtnChartRect.Y + 6, PlusBtnChartRect.X + 11, PlusBtnChartRect.Y + 16);
                }

                // 3. [ 🖌 ] Chart Styles
                g.FillRectangle(chart.ShowStylesMenu ? new SolidBrush(Color.FromArgb(215, 235, 255)) : Brushes.White, StyleBtnChartRect);
                g.DrawRectangle(borderPen, StyleBtnChartRect);
                using (SolidBrush bB = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    g.FillRectangle(bB, StyleBtnChartRect.X + 6, StyleBtnChartRect.Y + 5, 10, 6);
                    g.FillPolygon(bB, new Point[] { new Point(StyleBtnChartRect.X + 8, StyleBtnChartRect.Y + 11), new Point(StyleBtnChartRect.X + 14, StyleBtnChartRect.Y + 11), new Point(StyleBtnChartRect.X + 11, StyleBtnChartRect.Y + 16) });
                }

                // 4. [ ⏚ ] Values Filter
                g.FillRectangle(chart.ShowFilterMenu ? new SolidBrush(Color.FromArgb(215, 235, 255)) : Brushes.White, FilterBtnChartRect);
                g.DrawRectangle(borderPen, FilterBtnChartRect);
                using (Pen fPen = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                {
                    g.DrawLine(fPen, FilterBtnChartRect.X + 5, FilterBtnChartRect.Y + 6, FilterBtnChartRect.X + 17, FilterBtnChartRect.Y + 6);
                    g.DrawLine(fPen, FilterBtnChartRect.X + 7, FilterBtnChartRect.Y + 10, FilterBtnChartRect.X + 15, FilterBtnChartRect.Y + 10);
                    g.DrawLine(fPen, FilterBtnChartRect.X + 9, FilterBtnChartRect.Y + 14, FilterBtnChartRect.X + 13, FilterBtnChartRect.Y + 14);
                }
            }
        }

        private void DrawChartWrapMenu(Graphics g, DocChartObject chart)
        {
            if (LayoutBtnChartRect.IsEmpty) return;
            int popW = 185;
            int popH = 275;
            int popX = LayoutBtnChartRect.Right + 8;
            int popY = LayoutBtnChartRect.Top - 5;

            g.FillRectangle(Brushes.White, popX, popY, popW, popH);
            using (Pen popBorder = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
            {
                g.DrawRectangle(popBorder, popX, popY, popW, popH);
                Point[] pointerPts = new Point[] { new Point(popX, popY + 12), new Point(popX - 6, popY + 18), new Point(popX, popY + 24) };
                g.FillPolygon(Brushes.White, pointerPts);
                g.DrawLine(popBorder, popX, popY + 12, popX - 6, popY + 18);
                g.DrawLine(popBorder, popX - 6, popY + 18, popX, popY + 24);
            }

            using (Font headerFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font sectionFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            using (Font itemFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font linkFont = new Font("Segoe UI", 8.5f, FontStyle.Underline))
            {
                g.DrawString("Tùy chọn bố trí", headerFont, new SolidBrush(Color.FromArgb(0, 95, 184)), popX + 12, popY + 10);
                using (Pen closePen = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                {
                    g.DrawLine(closePen, popX + popW - 20, popY + 12, popX + popW - 10, popY + 22);
                    g.DrawLine(closePen, popX + popW - 10, popY + 12, popX + popW - 20, popY + 22);
                }

                Rectangle sec1Bar = new Rectangle(popX + 10, popY + 36, popW - 20, 18);
                using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241))) g.FillRectangle(secBarBrush, sec1Bar);
                g.DrawString("Cùng dòng với văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec1Bar.X + 6, sec1Bar.Y + 2);

                Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                DrawVectorWrapCard(g, 0, inlineCard, chart.WrapMode == 0);

                Rectangle sec2Bar = new Rectangle(popX + 10, popY + 98, popW - 20, 18);
                using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241))) g.FillRectangle(secBarBrush, sec2Bar);
                g.DrawString("Ngắt dòng văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec2Bar.X + 6, sec2Bar.Y + 2);

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int wIdx = r == 0 ? (1 + c) : (4 + c);
                        Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                        DrawVectorWrapCard(g, wIdx, cardBox, chart.WrapMode == wIdx);
                    }
                }

                int r1Y = popY + 202;
                DrawVectorRadioButton(g, popX + 14, r1Y, !chart.FixPosition, "Di chuyển cùng văn bản", itemFont);
                DrawVectorInfoIcon(g, popX + popW - 26, r1Y + 2);

                int r2Y = popY + 226;
                DrawVectorRadioButton(g, popX + 14, r2Y, chart.FixPosition, "Cố định vị trí trên trang", itemFont);
                DrawVectorInfoIcon(g, popX + popW - 26, r2Y + 2);

                using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(0, 95, 184)))
                {
                    g.DrawString("Xem thêm...", linkFont, linkBrush, popX + popW - 85, popY + 252);
                }
            }
        }

        private void DrawChartElementsMenu(Graphics g, DocChartObject chart)
        {
            if (PlusBtnChartRect.IsEmpty) return;
            int popW = 160;
            int popH = 150;
            int popX = PlusBtnChartRect.Right + 8;
            int popY = PlusBtnChartRect.Top - 5;

            g.FillRectangle(Brushes.White, popX, popY, popW, popH);
            using (Pen popBorder = new Pen(Color.FromArgb(16, 124, 65), 1.2f))
            {
                g.DrawRectangle(popBorder, popX, popY, popW, popH);
                Point[] pointerPts = new Point[] { new Point(popX, popY + 12), new Point(popX - 6, popY + 18), new Point(popX, popY + 24) };
                g.FillPolygon(Brushes.White, pointerPts);
                g.DrawLine(popBorder, popX, popY + 12, popX - 6, popY + 18);
                g.DrawLine(popBorder, popX - 6, popY + 18, popX, popY + 24);
            }

            using (Font titleF = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font itemF = new Font("Segoe UI", 8.5f))
            {
                g.DrawString("Thành phần Biểu đồ", titleF, new SolidBrush(Color.FromArgb(16, 124, 65)), popX + 10, popY + 8);
                g.DrawLine(Pens.LightGray, popX + 10, popY + 26, popX + popW - 10, popY + 26);

                string[] items = new string[] { "Trục tọa độ", "Tiêu đề biểu đồ", "Nhãn dữ liệu", "Đường lưới", "Chú giải" };
                bool[] states = new bool[] { true, chart.ShowTitle, chart.ShowDataLabels, chart.ShowGridlines, chart.ShowLegend };

                for (int i = 0; i < 5; i++)
                {
                    int iy = popY + 32 + i * 22;
                    Rectangle chkBox = new Rectangle(popX + 12, iy + 2, 13, 13);
                    g.DrawRectangle(Pens.Gray, chkBox);
                    if (states[i])
                    {
                        using (SolidBrush cb = new SolidBrush(Color.FromArgb(16, 124, 65))) g.FillRectangle(cb, chkBox);
                        using (Pen checkP = new Pen(Color.White, 1.8f))
                        {
                            g.DrawLine(checkP, chkBox.X + 2, chkBox.Y + 6, chkBox.X + 5, chkBox.Y + 9);
                            g.DrawLine(checkP, chkBox.X + 5, chkBox.Y + 9, chkBox.X + 10, chkBox.Y + 3);
                        }
                    }
                    g.DrawString(items[i], itemF, Brushes.Black, popX + 30, iy);
                }
            }
        }

        private void DrawChartStylesMenu(Graphics g, DocChartObject chart)
        {
            if (StyleBtnChartRect.IsEmpty) return;
            int popW = 180;
            int popH = 160;
            int popX = StyleBtnChartRect.Right + 8;
            int popY = StyleBtnChartRect.Top - 5;

            g.FillRectangle(Brushes.White, popX, popY, popW, popH);
            using (Pen popBorder = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
            {
                g.DrawRectangle(popBorder, popX, popY, popW, popH);
                Point[] pointerPts = new Point[] { new Point(popX, popY + 12), new Point(popX - 6, popY + 18), new Point(popX, popY + 24) };
                g.FillPolygon(Brushes.White, pointerPts);
                g.DrawLine(popBorder, popX, popY + 12, popX - 6, popY + 18);
                g.DrawLine(popBorder, popX - 6, popY + 18, popX, popY + 24);
            }

            using (Font titleF = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font itemF = new Font("Segoe UI", 8.0f))
            {
                g.DrawString("Kiểu dáng & Màu sắc", titleF, new SolidBrush(Color.FromArgb(0, 120, 215)), popX + 10, popY + 8);
                g.DrawLine(Pens.LightGray, popX + 10, popY + 26, popX + popW - 10, popY + 26);

                string[] thNames = new string[] { "Sáng cổ điển", "Nền tối", "Xám tinh tế" };
                for (int t = 0; t < 3; t++)
                {
                    Rectangle box = new Rectangle(popX + 10 + t * 54, popY + 34, 48, 40);
                    Color thBg = t == 1 ? Color.FromArgb(30, 41, 59) : (t == 2 ? Color.FromArgb(245, 247, 250) : Color.White);
                    using (SolidBrush b = new SolidBrush(thBg)) g.FillRectangle(b, box);
                    g.DrawRectangle(chart.StyleThemeIndex == t ? new Pen(Color.FromArgb(0, 120, 215), 2f) : Pens.LightGray, box);
                    g.FillRectangle(Brushes.SteelBlue, box.X + 6, box.Y + 12, 6, 16);
                    g.FillRectangle(Brushes.DarkOrange, box.X + 14, box.Y + 8, 6, 20);
                    g.FillRectangle(Brushes.Gray, box.X + 22, box.Y + 14, 6, 14);
                }

                g.DrawString("Bảng màu sắc:", itemF, Brushes.DimGray, popX + 10, popY + 82);
                Color[][] pals = new Color[][]
                {
                    new Color[] { Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0) },
                    new Color[] { Color.FromArgb(112, 48, 160), Color.FromArgb(0, 176, 240), Color.FromArgb(0, 176, 80), Color.FromArgb(255, 192, 0) },
                    new Color[] { Color.FromArgb(31, 78, 120), Color.FromArgb(47, 85, 151), Color.FromArgb(65, 113, 156), Color.FromArgb(142, 169, 219) }
                };

                for (int p = 0; p < 3; p++)
                {
                    Rectangle pBox = new Rectangle(popX + 10, popY + 100 + p * 18, popW - 20, 15);
                    if (chart.ColorPaletteIndex == p)
                    {
                        using (SolidBrush selB = new SolidBrush(Color.FromArgb(225, 240, 255))) g.FillRectangle(selB, pBox);
                        g.DrawRectangle(new Pen(Color.FromArgb(0, 120, 215), 1.2f), pBox);
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        using (SolidBrush cB = new SolidBrush(pals[p][k])) g.FillRectangle(cB, pBox.X + 6 + k * 36, pBox.Y + 3, 28, 9);
                    }
                }
            }
        }

        public void DrawTableLayoutOptionsPopup(Graphics g, int tableIdx, int canvasWidth, float zoomLevel)
        {
            if (tableIdx < 0 || tableIdx >= MoveHandleRects.Count) return;
            int tX = MoveHandleRects[tableIdx].Left + 25;
            int tY = MoveHandleRects[tableIdx].Top + 25;
            int tW = 200;
            if (tableIdx < ResizeHandleRects.Count) tW = (ResizeHandleRects[tableIdx].Left + 15) - tX;

            Rectangle wrapBtn = new Rectangle(tX + tW + 6, tY, 22, 22);
            int popW = 195;
            int popH = 280;
            int popX = wrapBtn.Right + 8;
            int popY = wrapBtn.Top - 5;

            if (popX + popW > canvasWidth - 15) popX = wrapBtn.Left - popW - 8;

            Rectangle popRect = new Rectangle(popX, popY, popW, popH);

            g.FillRectangle(Brushes.White, popX, popY, popW, popH);
            using (Pen popBorder = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
            {
                g.DrawRectangle(popBorder, popX, popY, popW, popH);
                Point[] pointerPts = new Point[] { new Point(popX, popY + 12), new Point(popX - 6, popY + 18), new Point(popX, popY + 24) };
                g.FillPolygon(Brushes.White, pointerPts);
                g.DrawLine(popBorder, popX, popY + 12, popX - 6, popY + 18);
                g.DrawLine(popBorder, popX - 6, popY + 18, popX, popY + 24);
            }

            int curWrap = TableWrapModes.ContainsKey(tableIdx) ? TableWrapModes[tableIdx] : 0;
            bool fixPos = TableFixPositions.ContainsKey(tableIdx) && TableFixPositions[tableIdx];

            using (Font headerFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font sectionFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            using (Font itemFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font linkFont = new Font("Segoe UI", 8.5f, FontStyle.Underline))
            {
                g.DrawString("Tùy chọn bố trí", headerFont, new SolidBrush(Color.FromArgb(0, 95, 184)), popX + 12, popY + 10);
                using (Pen closePen = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                {
                    g.DrawLine(closePen, popX + popW - 20, popY + 12, popX + popW - 10, popY + 22);
                    g.DrawLine(closePen, popX + popW - 10, popY + 12, popX + popW - 20, popY + 22);
                }

                // Section 1: In-Line
                Rectangle sec1Bar = new Rectangle(popX + 10, popY + 36, popW - 20, 18);
                using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241))) g.FillRectangle(secBarBrush, sec1Bar);
                g.DrawString("Cùng dòng với văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec1Bar.X + 6, sec1Bar.Y + 2);

                Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                ShapeRenderer.DrawVectorWrapCard(g, 0, inlineCard, curWrap == 0);

                // Section 2: Wrap Text (6 cards)
                Rectangle sec2Bar = new Rectangle(popX + 10, popY + 98, popW - 20, 18);
                using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241))) g.FillRectangle(secBarBrush, sec2Bar);
                g.DrawString("Ngắt dòng văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec2Bar.X + 6, sec2Bar.Y + 2);

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int wIdx = r == 0 ? (1 + c) : (4 + c);
                        Rectangle cardBox = new Rectangle(popX + 14 + c * 54, popY + 120 + r * 38, 44, 34);
                        ShapeRenderer.DrawVectorWrapCard(g, wIdx, cardBox, curWrap == wIdx);
                    }
                }

                int r1Y = popY + 202;
                ShapeRenderer.DrawVectorRadioButton(g, popX + 14, r1Y, !fixPos, "Di chuyển cùng văn bản", itemFont);
                ShapeRenderer.DrawVectorInfoIcon(g, popX + popW - 26, r1Y + 2);

                int r2Y = popY + 226;
                ShapeRenderer.DrawVectorRadioButton(g, popX + 14, r2Y, fixPos, "Cố định vị trí trên trang", itemFont);
                ShapeRenderer.DrawVectorInfoIcon(g, popX + popW - 26, r2Y + 2);

                // See more... link
                using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(0, 95, 184)))
                {
                    g.DrawString("Xem thêm...", linkFont, linkBrush, popX + popW - 85, popY + 254);
                }
            }
        }

        public void HandleMouseDown(int mouseX, int mouseY, int canvasWidth, Graphics g, RibbonBar ribbon)
        {
            int pageX, pageW, pageH, marginX, rightMarginX, marginY;
            GetPageLayoutMetrics(canvasWidth, ribbon, out pageX, out pageW, out pageH, out marginX, out rightMarginX, out marginY);
            int pageY = (ribbon != null && ribbon.ShowRuler ? 190 : 165) - ScrollY;

            int leftM_m = marginX;
            int rightM_m = rightMarginX;
            int topM_m = marginY;

            // =========================================================================
            // -1. RULER & INDENTATION HANDLES HIT TESTS (Fixed at Y: 156..180)
            // =========================================================================
            if (ribbon != null && ribbon.ShowRuler && mouseY >= 155 && mouseY <= 180)
            {
                if (RulerTabSelectorRect.Contains(mouseX, mouseY))
                {
                    ActiveTabStopSelector = (TabStopType)(((int)ActiveTabStopSelector + 1) % 5);
                    return;
                }

                int curPStart = GetCurrentParagraphStart();
                ActiveRulerDragParaStart = curPStart;

                // Check First Line Indent (▼)
                if (FirstLineIndentHitRect.Contains(mouseX, mouseY))
                {
                    ActiveRulerDrag = RulerDragHandle.FirstLineIndent;
                    RulerDragStartPoint = new Point(mouseX, mouseY);
                    RulerDragInitialValue = GetFirstLineIndent(curPStart);
                    RulerGuideLineX = mouseX;
                    return;
                }

                // Check Left Indent (■ - bottom rectangle)
                if (LeftIndentHitRect.Contains(mouseX, mouseY))
                {
                    ActiveRulerDrag = RulerDragHandle.LeftIndent;
                    RulerDragStartPoint = new Point(mouseX, mouseY);
                    RulerDragInitialValue = GetLeftIndent(curPStart);
                    RulerDragInitialFirstLine = GetFirstLineIndent(curPStart);
                    RulerDragInitialHanging = GetHangingIndent(curPStart);
                    RulerGuideLineX = mouseX;
                    return;
                }

                // Check Hanging Indent (▲ - middle triangle)
                if (HangingIndentHitRect.Contains(mouseX, mouseY))
                {
                    ActiveRulerDrag = RulerDragHandle.HangingIndent;
                    RulerDragStartPoint = new Point(mouseX, mouseY);
                    RulerDragInitialValue = GetHangingIndent(curPStart);
                    RulerGuideLineX = mouseX;
                    return;
                }

                // Check Right Indent (▲ on the right)
                if (RightIndentHitRect.Contains(mouseX, mouseY))
                {
                    ActiveRulerDrag = RulerDragHandle.RightIndent;
                    RulerDragStartPoint = new Point(mouseX, mouseY);
                    RulerDragInitialValue = GetRightIndent(curPStart);
                    RulerGuideLineX = mouseX;
                    return;
                }

                // Check existing Tab Stops
                if (TabStopHitRects != null && TabStopHitRects.Count > 0)
                {
                    var tabStops = GetTabStops(curPStart);
                    for (int tIdx = 0; tIdx < TabStopHitRects.Count; tIdx++)
                    {
                        if (TabStopHitRects[tIdx].Contains(mouseX, mouseY))
                        {
                            ActiveRulerDrag = RulerDragHandle.TabStop;
                            DraggedTabStopIndex = tIdx;
                            RulerDragStartPoint = new Point(mouseX, mouseY);
                            RulerDragInitialValue = tabStops[tIdx].PositionPt;
                            RulerGuideLineX = mouseX;
                            return;
                        }
                    }
                }

                // Click on Ruler White Area -> Add New Tab Stop!
                int rulerY = 156;
                Rectangle rulerWhiteArea = new Rectangle(pageX + marginX, rulerY, pageW - marginX - rightMarginX, 24);

                if (rulerWhiteArea.Contains(mouseX, mouseY))
                {
                    float clickedPt = (mouseX - (pageX + marginX)) / ZoomLevel;
                    var tList = GetTabStops(curPStart);
                    tList.Add(new TabStopInfo(clickedPt, ActiveTabStopSelector));
                    ActiveRulerDrag = RulerDragHandle.TabStop;
                    DraggedTabStopIndex = tList.Count - 1;
                    RulerDragStartPoint = new Point(mouseX, mouseY);
                    RulerDragInitialValue = clickedPt;
                    RulerGuideLineX = mouseX;
                    return;
                }
                return; // Consumed click on ruler
            }

            // =========================================================================
            // 0. COMMENT HIT TESTS (Reply, Resolve, Expand/Collapse Card, Anchor click)
            // =========================================================================
            if (Comments != null && Comments.Count > 0)
            {
                bool clickedComment = false;

                foreach (var kvp in CommentReplyBtnRects)
                {
                    if (kvp.Value.Contains(mouseX, mouseY))
                    {
                        int cId = kvp.Key;
                        ActiveCommentId = cId;
                        EditingReplyCommentId = cId;
                        ReplyDraftText = "";
                        clickedComment = true;
                        return;
                    }
                }

                foreach (var kvp in CommentResolveBtnRects)
                {
                    if (kvp.Value.Contains(mouseX, mouseY))
                    {
                        int cId = kvp.Key;
                        for (int i = 0; i < Comments.Count; i++)
                        {
                            if (Comments[i].Id == cId)
                            {
                                Comments[i].IsResolved = !Comments[i].IsResolved;
                                break;
                            }
                        }
                        clickedComment = true;
                        return;
                    }
                }

                foreach (var kvp in CommentBadgeHitRects)
                {
                    if (kvp.Value.Contains(mouseX, mouseY))
                    {
                        ActiveCommentId = (ActiveCommentId == kvp.Key) ? -1 : kvp.Key;
                        if (ActiveCommentId != kvp.Key) EditingReplyCommentId = -1;
                        clickedComment = true;
                        return;
                    }
                }

                foreach (var kvp in CommentAnchorTextRects)
                {
                    if (kvp.Value.Contains(mouseX, mouseY))
                    {
                        ActiveCommentId = (ActiveCommentId == kvp.Key) ? -1 : kvp.Key;
                        if (ActiveCommentId != kvp.Key) EditingReplyCommentId = -1;
                        clickedComment = true;
                        return;
                    }
                }

                // Nếu click ra ngoài bất kỳ thẻ comment nào -> tự động đóng/ẩn và hủy chế độ reply
                if (!clickedComment)
                {
                    ActiveCommentId = -1;
                    EditingReplyCommentId = -1;
                    ReplyDraftText = "";
                }
            }

            // =========================================================================
            // 0.1 TABLE HIT TESTS: Luôn ưu tiên xử lý kéo/chỉnh Bảng trước mọi vùng khác
            // =========================================================================
            // 0.0 Check Table Layout Options Popup Dialog Click (nếu đang mở)
            if (ActiveTableLayoutOptionsIndex >= 0 && ActiveTableLayoutOptionsIndex < MoveHandleRects.Count)
            {
                int tIdx = ActiveTableLayoutOptionsIndex;
                int tX = MoveHandleRects[tIdx].Left + 25;
                int tY = MoveHandleRects[tIdx].Top + 25;
                int tW = 200;
                if (tIdx < ResizeHandleRects.Count) tW = (ResizeHandleRects[tIdx].Left + 15) - tX;

                Rectangle wrapBtn = new Rectangle(tX + tW + 6, tY, 22, 22);
                int popW = 195;
                int popH = 280;
                int popX = wrapBtn.Right + 8;
                int popY = wrapBtn.Top - 5;
                if (popX + popW > canvasWidth - 15) popX = wrapBtn.Left - popW - 8;
                Rectangle popRect = new Rectangle(popX, popY, popW, popH);

                if (popRect.Contains(mouseX, mouseY))
                {
                    // 1. Close button
                    Rectangle closeBtn = new Rectangle(popX + popW - 24, popY + 6, 20, 20);
                    if (closeBtn.Contains(mouseX, mouseY))
                    {
                        ActiveTableLayoutOptionsIndex = -1;
                        return;
                    }

                    // 2. In-Line box
                    Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                    if (inlineCard.Contains(mouseX, mouseY))
                    {
                        TableWrapModes[tIdx] = 0;
                        TableFloatingBounds.Remove(tIdx);
                        return;
                    }

                    // 3. 6 Wrap boxes (Square, Tight, Through, TopBottom, Behind, InFront)
                    for (int r = 0; r < 2; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int wIdx = r == 0 ? (1 + c) : (4 + c);
                            Rectangle cardBox = new Rectangle(popX + 14 + c * 54, popY + 120 + r * 38, 44, 34);
                            if (cardBox.Contains(mouseX, mouseY))
                            {
                                TableWrapModes[tIdx] = wIdx;
                                return;
                            }
                        }
                    }

                    // 4. Radio 1 (Di chuyển cùng văn bản)
                    Rectangle r1Box = new Rectangle(popX + 14, popY + 200, popW - 28, 22);
                    if (r1Box.Contains(mouseX, mouseY))
                    {
                        TableFixPositions[tIdx] = false;
                        return;
                    }

                    // 5. Radio 2 (Cố định vị trí trên trang)
                    Rectangle r2Box = new Rectangle(popX + 14, popY + 224, popW - 28, 22);
                    if (r2Box.Contains(mouseX, mouseY))
                    {
                        TableFixPositions[tIdx] = true;
                        return;
                    }

                    return;
                }
                else if (!wrapBtn.Contains(mouseX, mouseY))
                {
                    ActiveTableLayoutOptionsIndex = -1;
                }
            }

            // 0.1 Check Table Wrap Mode Button Click (Mở/đóng Popup Tùy chọn bố trí)
            for (int i = 0; i < TableWrapBtnHits.Count; i++)
            {
                if (TableWrapBtnHits[i].Rect.Contains(mouseX, mouseY))
                {
                    int tIdx = TableWrapBtnHits[i].TableIndex;
                    ActiveTableLayoutOptionsIndex = (ActiveTableLayoutOptionsIndex == tIdx) ? -1 : tIdx;
                    return;
                }
            }

            // 0.2 Check MS Word '+' Insert Button Hits (Insert Row or Column)
            for (int i = 0; i < InsertButtonHits.Count; i++)
            {
                if (InsertButtonHits[i].Rect.Contains(mouseX, mouseY))
                {
                    var hit = InsertButtonHits[i];
                    if (hit.IsRowInsert)
                        InsertTableRow(hit.TableIndex, hit.InsertIndex);
                    else
                        InsertTableColumn(hit.TableIndex, hit.InsertIndex);
                    return;
                }
            }

            // 0.3 Check Table Move Handle Click (Top-Left ✢ Icon)
            for (int t = 0; t < MoveHandleRects.Count; t++)
            {
                if (MoveHandleRects[t].Contains(mouseX, mouseY))
                {
                    DragTableIndex = t;
                    IsDraggingMove = true;
                    TableWrapModes[t] = 1; // Tự động chuyển sang Wrap Around để di chuyển tự do 100% như hình ảnh!

                    TableDragStartMouse = new Point(mouseX, mouseY);
                    int sPageY = 195 - ScrollY;
                    int defaultPageX = (int)(ribbon.GetEffectiveLeftMarginInches() * 54.0f * ZoomLevel);
                    int curPageX = TablePageX.ContainsKey(t) ? TablePageX[t] : defaultPageX;
                    int curPageY = TablePageY.ContainsKey(t) ? TablePageY[t] : (MoveHandleRects[t].Top + 25 - sPageY);

                    TableDragStartOffset = new Point(curPageX, curPageY);
                    TablePageX[t] = curPageX;
                    TablePageY[t] = curPageY;
                    return;
                }
            }

            // 0.4 Check Table Resize Handle Click (Bottom-Right ⇲ Icon)
            for (int t = 0; t < ResizeHandleRects.Count; t++)
            {
                if (ResizeHandleRects[t].Contains(mouseX, mouseY))
                {
                    DragTableIndex = t;
                    IsDraggingResize = true;
                    DragResizeInitialMouse = new Point(mouseX, mouseY);
                    DragResizeInitialW = 0;
                    DragResizeInitialH = 0;
                    DragResizeInitialColWidths.Clear();
                    DragResizeInitialRowHeights.Clear();

                    if (TableColWidths.ContainsKey(t))
                    {
                        DragResizeInitialColWidths = new List<int>(TableColWidths[t]);
                        foreach (int w in TableColWidths[t]) DragResizeInitialW += w;
                    }
                    if (TableRowHeights.ContainsKey(t))
                    {
                        DragResizeInitialRowHeights = new List<int>(TableRowHeights[t]);
                        foreach (int h in TableRowHeights[t]) DragResizeInitialH += h;
                    }
                    return;
                }
            }

            // 0.5 Check Table Internal Column Gridline Dragging
            foreach (var kvp in TableColWidths)
            {
                int tIdx = kvp.Key;
                var colWidths = kvp.Value;
                if (tIdx < MoveHandleRects.Count)
                {
                    int tX = MoveHandleRects[tIdx].Left + 25;
                    int tY = MoveHandleRects[tIdx].Top + 25;
                    int tH = 100;
                    if (tIdx < ResizeHandleRects.Count) tH = (ResizeHandleRects[tIdx].Top + 10) - tY;

                    int curX = tX;
                    for (int c = 0; c < colWidths.Count; c++)
                    {
                        curX += colWidths[c];
                        Rectangle lineHit = new Rectangle(curX - 6, tY, 12, tH);
                        if (lineHit.Contains(mouseX, mouseY))
                        {
                            IsDraggingColGridline = true;
                            DragGridlineTableIndex = tIdx;
                            DragGridlineColIndex = c;
                            DragGridlineInitialMouse = new Point(mouseX, mouseY);
                            DragGridlineInitialWidth1 = colWidths[c];
                            DragGridlineInitialWidth2 = (c < colWidths.Count - 1) ? colWidths[c + 1] : 0;
                            return;
                        }
                    }
                }
            }

            // 0.6 Check Table Internal Row Gridline Dragging
            foreach (var kvp in TableRowHeights)
            {
                int tIdx = kvp.Key;
                var rowHeights = kvp.Value;
                if (tIdx < MoveHandleRects.Count)
                {
                    int tX = MoveHandleRects[tIdx].Left + 25;
                    int tY = MoveHandleRects[tIdx].Top + 25;
                    int tW = 200;
                    if (tIdx < ResizeHandleRects.Count) tW = (ResizeHandleRects[tIdx].Left + 10) - tX;

                    int curY = tY;
                    for (int r = 0; r < rowHeights.Count; r++)
                    {
                        curY += rowHeights[r];
                        Rectangle lineHit = new Rectangle(tX, curY - 6, tW, 12);
                        if (lineHit.Contains(mouseX, mouseY))
                        {
                            IsDraggingRowGridline = true;
                            DragGridlineTableIndex = tIdx;
                            DragGridlineRowIndex = r;
                            DragGridlineInitialMouse = new Point(mouseX, mouseY);
                            DragGridlineInitialHeight = rowHeights[r];
                            return;
                        }
                    }
                }
            }

            if (IsEditingHeaderFooter && CurrentHeaderPreset == HeaderFooterPreset.Banded)
            {
                int bX = pageX + leftM_m + (int)(HeaderBannerXOffset * ZoomLevel);
                int bW = Math.Max((int)(50 * ZoomLevel), pageW - leftM_m - rightM_m + (int)(HeaderBannerWidthDelta * ZoomLevel));
                int bH = Math.Max((int)(16 * ZoomLevel), (int)(28 * ZoomLevel) + (int)(HeaderBannerHeightDelta * ZoomLevel));
                int bY = pageY + (int)(10 * ZoomLevel) + (int)(HeaderBannerYOffset * ZoomLevel);
                Rectangle bannerRect = new Rectangle(bX, bY, bW, bH);

                Point[] handlePts = new Point[]
                {
                    new Point(bannerRect.Left, bannerRect.Top),
                    new Point(bannerRect.Left + bannerRect.Width / 2, bannerRect.Top),
                    new Point(bannerRect.Right, bannerRect.Top),
                    new Point(bannerRect.Left, bannerRect.Top + bannerRect.Height / 2),
                    new Point(bannerRect.Right, bannerRect.Top + bannerRect.Height / 2),
                    new Point(bannerRect.Left, bannerRect.Bottom),
                    new Point(bannerRect.Left + bannerRect.Width / 2, bannerRect.Bottom),
                    new Point(bannerRect.Right, bannerRect.Bottom)
                };

                for (int hIdx = 0; hIdx < 8; hIdx++)
                {
                    Rectangle handleCircle = new Rectangle(handlePts[hIdx].X - 8, handlePts[hIdx].Y - 8, 16, 16);
                    if (handleCircle.Contains(mouseX, mouseY))
                    {
                        IsResizingHeaderBanner = true;
                        HeaderResizeHandleIndex = hIdx;
                        HeaderDragStartMouse = new Point(mouseX, mouseY);
                        return;
                    }
                }

                // Check Layout Options button and popup
                Rectangle wrapBtnRect = new Rectangle(bannerRect.Right + 4, bannerRect.Top, 22, 22);
                if (wrapBtnRect.Contains(mouseX, mouseY))
                {
                    ShowHeaderLayoutOptions = !ShowHeaderLayoutOptions;
                    return;
                }

                if (ShowHeaderLayoutOptions)
                {
                    int popW = 185;
                    int popH = 250;
                    int popX = wrapBtnRect.Right + 8;
                    int popY = wrapBtnRect.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        // 1. Close button
                        Rectangle closeBtn = new Rectangle(popX + popW - 24, popY + 6, 20, 20);
                        if (closeBtn.Contains(mouseX, mouseY))
                        {
                            ShowHeaderLayoutOptions = false;
                            return;
                        }

                        // 2. In-Line box
                        Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                        if (inlineCard.Contains(mouseX, mouseY))
                        {
                            HeaderWrapMode = 0;
                            return;
                        }

                        // 3. 6 Wrap boxes
                        for (int r = 0; r < 2; r++)
                        {
                            for (int c = 0; c < 3; c++)
                            {
                                int wIdx = r == 0 ? (1 + c) : (4 + c);
                                Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                                if (cardBox.Contains(mouseX, mouseY))
                                {
                                    HeaderWrapMode = wIdx;
                                    return;
                                }
                            }
                        }

                        // 4. Radio 1 (Di chuyển cùng văn bản)
                        Rectangle r1Box = new Rectangle(popX + 14, popY + 200, popW - 28, 22);
                        if (r1Box.Contains(mouseX, mouseY))
                        {
                            HeaderFixPosition = false;
                            return;
                        }

                        // 5. Radio 2 (Cố định vị trí trên trang)
                        Rectangle r2Box = new Rectangle(popX + 14, popY + 224, popW - 28, 22);
                        if (r2Box.Contains(mouseX, mouseY))
                        {
                            HeaderFixPosition = true;
                            return;
                        }

                        return;
                    }
                    else
                    {
                        ShowHeaderLayoutOptions = false;
                    }
                }

                Rectangle handleTag = new Rectangle(bannerRect.X + bannerRect.Width / 2 - 30, bannerRect.Y - 16, 60, 16);
                Rectangle rotateHandle = new Rectangle(bannerRect.Left + bannerRect.Width / 2 - 10, bannerRect.Top - 30, 20, 20);
                if (handleTag.Contains(mouseX, mouseY) || rotateHandle.Contains(mouseX, mouseY))
                {
                    IsDraggingHeaderBanner = true;
                    HeaderDragStartMouse = new Point(mouseX, mouseY);
                    return;
                }

                string tStr = string.IsNullOrEmpty(HeaderTitleText) ? "TIÊU ĐỀ ĐẦU TRANG" : HeaderTitleText;
                using (Font titleFont = new Font("Segoe UI", 10.0f * ZoomLevel, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(tStr, titleFont);
                    float txtX = bannerRect.X + (bannerRect.Width - sz.Width) / 2;
                    float txtY = bannerRect.Y + (bannerRect.Height - sz.Height) / 2;
                    Rectangle textFrame = new Rectangle((int)txtX - 6, (int)txtY - 2, (int)sz.Width + 12, (int)sz.Height + 4);

                    if (textFrame.Contains(mouseX, mouseY))
                    {
                        IsEditingHeaderFooter = true;
                        IsEditingHeaderMode = true;
                        IsHeaderTextSelected = true;
                        if (ribbon != null) ribbon.ActiveRibbonTab = 6;
                        return;
                    }
                }

                if (bannerRect.Contains(mouseX, mouseY))
                {
                    IsDraggingHeaderBanner = true;
                    HeaderDragStartMouse = new Point(mouseX, mouseY);
                    return;
                }
            }

            // Chỉ xử lý phân vùng Header/Footer khi đang ở chế độ chỉnh sửa Header/Footer
            if (IsEditingHeaderFooter)
            {
                Rectangle headerZone = new Rectangle(pageX, pageY, pageW, (int)(60 * ZoomLevel));
                Rectangle footerZone = new Rectangle(pageX, pageY + pageH - (int)(60 * ZoomLevel), pageW, (int)(60 * ZoomLevel));

                if (headerZone.Contains(mouseX, mouseY))
                {
                    IsEditingHeaderMode = true;
                    IsHeaderTextSelected = true;
                    if (CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                    {
                        if (mouseX >= pageX + pageW * 2 / 3) ActiveThreeColIndex = 2;
                        else if (mouseX >= pageX + pageW / 3) ActiveThreeColIndex = 1;
                        else ActiveThreeColIndex = 0;
                    }
                    if (ribbon != null) ribbon.ActiveRibbonTab = 6;
                    return;
                }
                if (footerZone.Contains(mouseX, mouseY))
                {
                    IsEditingHeaderMode = false;
                    if (ribbon != null) ribbon.ActiveRibbonTab = 6;
                    return;
                }
            }

            if (ViewMode == 0)
            {
                // Check Left Arrow Hit (◀)
                if (mouseX <= 70)
                {
                    ReadModePageIndex = Math.Max(0, ReadModePageIndex - 1);
                    return;
                }
                // Check Right Arrow Hit (▶)
                if (mouseX >= canvasWidth - 70)
                {
                    ReadModePageIndex++;
                    return;
                }
            }



            // Check Chart Floating Buttons Hits
            if (ActiveChart != null)
            {
                if (!LayoutBtnChartRect.IsEmpty && LayoutBtnChartRect.Contains(mouseX, mouseY))
                {
                    ActiveChart.ShowLayoutOptions = !ActiveChart.ShowLayoutOptions;
                    ActiveChart.ShowElementsMenu = false;
                    ActiveChart.ShowStylesMenu = false;
                    ActiveChart.ShowFilterMenu = false;
                    return;
                }
                if (!PlusBtnChartRect.IsEmpty && PlusBtnChartRect.Contains(mouseX, mouseY))
                {
                    ActiveChart.ShowElementsMenu = !ActiveChart.ShowElementsMenu;
                    ActiveChart.ShowLayoutOptions = false;
                    ActiveChart.ShowStylesMenu = false;
                    ActiveChart.ShowFilterMenu = false;
                    return;
                }
                if (!StyleBtnChartRect.IsEmpty && StyleBtnChartRect.Contains(mouseX, mouseY))
                {
                    ActiveChart.ShowStylesMenu = !ActiveChart.ShowStylesMenu;
                    ActiveChart.ShowLayoutOptions = false;
                    ActiveChart.ShowElementsMenu = false;
                    ActiveChart.ShowFilterMenu = false;
                    return;
                }
                if (!FilterBtnChartRect.IsEmpty && FilterBtnChartRect.Contains(mouseX, mouseY))
                {
                    OpenChartExcelEditor(ActiveChart);
                    return;
                }

                // Check Chart Popups clicks
                if (ActiveChart.ShowLayoutOptions && !LayoutBtnChartRect.IsEmpty)
                {
                    int popW = 185; int popH = 275;
                    int popX = LayoutBtnChartRect.Right + 8; int popY = LayoutBtnChartRect.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        Rectangle closeBtn = new Rectangle(popX + popW - 24, popY + 6, 20, 20);
                        if (closeBtn.Contains(mouseX, mouseY)) { ActiveChart.ShowLayoutOptions = false; return; }

                        Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                        if (inlineCard.Contains(mouseX, mouseY)) { ActiveChart.WrapMode = 0; return; }

                        for (int r = 0; r < 2; r++)
                        {
                            for (int c = 0; c < 3; c++)
                            {
                                int wIdx = r == 0 ? (1 + c) : (4 + c);
                                Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                                if (cardBox.Contains(mouseX, mouseY))
                                {
                                    if (ActiveChart.WrapMode == 0 && wIdx != 0)
                                    {
                                        int pX_cur = Math.Max(0, (int)((ActiveChart.LastBounds.X - pageX) / ZoomLevel));
                                        int pY_cur = Math.Max(0, (int)((ActiveChart.LastBounds.Y - (pageY + (int)(45 * ZoomLevel))) / ZoomLevel));
                                        ActiveChart.PageX = pX_cur > 0 ? pX_cur : 65;
                                        ActiveChart.PageY = pY_cur > 0 ? pY_cur : 150;
                                    }
                                    ActiveChart.WrapMode = wIdx;
                                    return;
                                }
                            }
                        }

                        if (new Rectangle(popX + 14, popY + 202, popW - 28, 20).Contains(mouseX, mouseY)) { ActiveChart.FixPosition = false; return; }
                        if (new Rectangle(popX + 14, popY + 226, popW - 28, 20).Contains(mouseX, mouseY)) { ActiveChart.FixPosition = true; return; }
                        if (new Rectangle(popX + popW - 90, popY + 248, 80, 24).Contains(mouseX, mouseY))
                        {
                            MessageBox.Show("Chế độ ngắt dòng biểu đồ hiện tại: " + (ActiveChart.WrapMode == 0 ? "Cùng dòng với văn bản" : (ActiveChart.WrapMode == 1 ? "Vuông (Square)" : (ActiveChart.WrapMode == 2 ? "Chặt (Tight)" : (ActiveChart.WrapMode == 3 ? "Xuyên qua (Through)" : (ActiveChart.WrapMode == 4 ? "Trên và Dưới (Top/Bottom)" : (ActiveChart.WrapMode == 5 ? "Dưới văn bản (Behind)" : "Trước văn bản (In Front)")))))) + "\n\nBạn có thể nhấp và kéo trực tiếp biểu đồ đến bất kỳ vị trí nào trên trang tài liệu.", "Tùy chọn bố trí Biểu đồ GenOffice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        return;
                    }
                }

                if (ActiveChart.ShowElementsMenu && !PlusBtnChartRect.IsEmpty)
                {
                    int popW = 160; int popH = 150;
                    int popX = PlusBtnChartRect.Right + 8; int popY = PlusBtnChartRect.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int iy = popY + 32 + i * 22;
                            Rectangle chkBox = new Rectangle(popX + 10, iy, popW - 20, 20);
                            if (chkBox.Contains(mouseX, mouseY))
                            {
                                if (i == 1) ActiveChart.ShowTitle = !ActiveChart.ShowTitle;
                                else if (i == 2) ActiveChart.ShowDataLabels = !ActiveChart.ShowDataLabels;
                                else if (i == 3) ActiveChart.ShowGridlines = !ActiveChart.ShowGridlines;
                                else if (i == 4) ActiveChart.ShowLegend = !ActiveChart.ShowLegend;
                                return;
                            }
                        }
                        return;
                    }
                }

                if (ActiveChart.ShowStylesMenu && !StyleBtnChartRect.IsEmpty)
                {
                    int popW = 180; int popH = 160;
                    int popX = StyleBtnChartRect.Right + 8; int popY = StyleBtnChartRect.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        for (int t = 0; t < 3; t++)
                        {
                            Rectangle box = new Rectangle(popX + 10 + t * 54, popY + 34, 48, 40);
                            if (box.Contains(mouseX, mouseY)) { ActiveChart.StyleThemeIndex = t; return; }
                        }
                        for (int p = 0; p < 3; p++)
                        {
                            Rectangle pBox = new Rectangle(popX + 10, popY + 100 + p * 18, popW - 20, 15);
                            if (pBox.Contains(mouseX, mouseY)) { ActiveChart.ColorPaletteIndex = p; return; }
                        }
                        return;
                    }
                }

                // Check 8 Handles on ActiveChart
                int hs = (int)(7 * ZoomLevel);
                int cX = ActiveChartLastBounds.X; int cY = ActiveChartLastBounds.Y;
                int cW = ActiveChartLastBounds.Width; int cH = ActiveChartLastBounds.Height;
                Point[] chartHandles = new Point[]
                {
                    new Point(cX - hs/2, cY - hs/2),
                    new Point(cX + cW/2 - hs/2, cY - hs/2),
                    new Point(cX + cW - hs/2, cY - hs/2),
                    new Point(cX + cW - hs/2, cY + cH/2 - hs/2),
                    new Point(cX + cW - hs/2, cY + cH - hs/2),
                    new Point(cX + cW/2 - hs/2, cY + cH - hs/2),
                    new Point(cX - hs/2, cY + cH - hs/2),
                    new Point(cX - hs/2, cY + cH/2 - hs/2)
                };
                for (int h = 0; h < 8; h++)
                {
                    Rectangle hRect = new Rectangle(chartHandles[h].X - 4, chartHandles[h].Y - 4, hs + 8, hs + 8);
                    if (hRect.Contains(mouseX, mouseY))
                    {
                        IsDraggingChart = false;
                        DragChartHandleIndex = h;
                        ChartDragStartMouse = new Point(mouseX, mouseY);
                        ChartDragStartSize = new Size(ActiveChart.BaseWidth + ActiveChart.WidthDelta, ActiveChart.BaseHeight + ActiveChart.HeightDelta);
                        ChartDragStartOffset = new Point(ActiveChart.PageX, ActiveChart.PageY);
                        return;
                    }
                }
            }

            // Check Hit on any chart in DocumentCharts
            DocChartObject hitChart = null;
            foreach (var ch in DocumentCharts)
            {
                if (!ch.LastBounds.IsEmpty && ch.LastBounds.Contains(mouseX, mouseY))
                {
                    hitChart = ch;
                    break;
                }
            }

            // Check Hit on ActiveShape Handles & Layout options
            if (ActiveShape != null && !ActiveShape.LastBounds.IsEmpty)
            {
                Rectangle sBounds = ActiveShape.LastBounds;
                int hs = (int)(7 * ZoomLevel);

                // Check Layout Options button
                Rectangle wrapBtn = new Rectangle(sBounds.Right + 5, sBounds.Top, 22, 22);
                if (wrapBtn.Contains(mouseX, mouseY))
                {
                    ActiveShape.ShowLayoutOptions = !ActiveShape.ShowLayoutOptions;
                    return;
                }

                // Check Layout Options popup clicks
                if (ActiveShape.ShowLayoutOptions)
                {
                    int popW = 185; int popH = 275;
                    int popX = wrapBtn.Right + 8; int popY = wrapBtn.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        Rectangle closeBtn = new Rectangle(popX + popW - 24, popY + 6, 20, 20);
                        if (closeBtn.Contains(mouseX, mouseY)) { ActiveShape.ShowLayoutOptions = false; return; }

                        Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                        if (inlineCard.Contains(mouseX, mouseY)) { ActiveShape.WrapMode = 0; SyncShapeTagInDocument(ActiveShape); return; }

                        for (int r = 0; r < 2; r++)
                        {
                            for (int c = 0; c < 3; c++)
                            {
                                int wIdx = r == 0 ? (1 + c) : (4 + c);
                                Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                                if (cardBox.Contains(mouseX, mouseY))
                                {
                                    if (ActiveShape.WrapMode == 0 && wIdx != 0)
                                    {
                                        int pX_cur = Math.Max(0, (int)((ActiveShape.LastBounds.X - pageX) / ZoomLevel));
                                        int pY_cur = Math.Max(0, (int)((ActiveShape.LastBounds.Y - (pageY + (int)(45 * ZoomLevel))) / ZoomLevel));
                                        ActiveShape.PageX = pX_cur > 0 ? pX_cur : 80;
                                        ActiveShape.PageY = pY_cur > 0 ? pY_cur : 160;
                                    }
                                    ActiveShape.WrapMode = wIdx;
                                    SyncShapeTagInDocument(ActiveShape);
                                    return;
                                }
                            }
                        }

                        if (new Rectangle(popX + 14, popY + 202, popW - 28, 20).Contains(mouseX, mouseY)) { ActiveShape.FixPosition = false; SyncShapeTagInDocument(ActiveShape); return; }
                        if (new Rectangle(popX + 14, popY + 226, popW - 28, 20).Contains(mouseX, mouseY)) { ActiveShape.FixPosition = true; SyncShapeTagInDocument(ActiveShape); return; }
                        if (new Rectangle(popX + popW - 90, popY + 248, 80, 24).Contains(mouseX, mouseY))
                        {
                            MessageBox.Show("Chế độ ngắt dòng hiện tại: " + (ActiveShape.WrapMode == 0 ? "Cùng dòng với văn bản" : (ActiveShape.WrapMode == 1 ? "Vuông (Square)" : (ActiveShape.WrapMode == 2 ? "Chặt (Tight)" : (ActiveShape.WrapMode == 3 ? "Xuyên qua (Through)" : (ActiveShape.WrapMode == 4 ? "Trên và Dưới (Top/Bottom)" : (ActiveShape.WrapMode == 5 ? "Dưới văn bản (Behind)" : "Trước văn bản (In Front)")))))) + "\n\nBạn có thể nhấp và kéo trực tiếp đối tượng đến bất kỳ vị trí nào trên trang tài liệu.", "Tùy chọn bố trí GenOffice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        return;
                    }
                }

                // Check Rotation handle (circle at top)
                int rotY = sBounds.Top - (int)(22 * ZoomLevel);
                int rotX = sBounds.Left + sBounds.Width / 2;
                Rectangle rotRect = new Rectangle(rotX - hs / 2 - 4, rotY - 4, hs + 8, hs + 8);
                if (rotRect.Contains(mouseX, mouseY))
                {
                    ActiveShape.RotationAngle = (ActiveShape.RotationAngle + 45f) % 360f;
                    return;
                }

                // Check 8 Handles on ActiveShape
                Point[] sHandles = new Point[]
                {
                    new Point(sBounds.Left - hs/2, sBounds.Top - hs/2),
                    new Point(sBounds.Left + sBounds.Width/2 - hs/2, sBounds.Top - hs/2),
                    new Point(sBounds.Right - hs/2, sBounds.Top - hs/2),
                    new Point(sBounds.Right - hs/2, sBounds.Top + sBounds.Height/2 - hs/2),
                    new Point(sBounds.Right - hs/2, sBounds.Bottom - hs/2),
                    new Point(sBounds.Left + sBounds.Width/2 - hs/2, sBounds.Bottom - hs/2),
                    new Point(sBounds.Left - hs/2, sBounds.Bottom - hs/2),
                    new Point(sBounds.Left - hs/2, sBounds.Top + sBounds.Height/2 - hs/2)
                };
                for (int h = 0; h < 8; h++)
                {
                    Rectangle hRect = new Rectangle(sHandles[h].X - 4, sHandles[h].Y - 4, hs + 8, hs + 8);
                    if (hRect.Contains(mouseX, mouseY))
                    {
                        IsDraggingShape = false;
                        DragShapeHandleIndex = h;
                        ShapeDragStartMouse = new Point(mouseX, mouseY);
                        ShapeDragStartSize = new Size(ActiveShape.BaseWidth + ActiveShape.WidthDelta, ActiveShape.BaseHeight + ActiveShape.HeightDelta);
                        ShapeDragStartOffset = new Point(ActiveShape.PageX, ActiveShape.PageY);
                        return;
                    }
                }
            }

            // Check Hit on any shape in DocumentShapes
            DocShapeObject hitShape = null;
            foreach (var sh in DocumentShapes)
            {
                if (!sh.LastBounds.IsEmpty && sh.LastBounds.Contains(mouseX, mouseY))
                {
                    hitShape = sh;
                    break;
                }
            }

            if (hitShape != null)
            {
                foreach (var sh in DocumentShapes) sh.IsSelected = false;
                if (ActiveChart != null) { ActiveChart.IsSelected = false; ActiveChart = null; }
                ActiveShape = hitShape;
                hitShape.IsSelected = true;

                int borderThickness = Math.Max(6, (int)(8 * ZoomLevel));
                Rectangle innerTextBounds = new Rectangle(hitShape.LastBounds.X + borderThickness, hitShape.LastBounds.Y + borderThickness, Math.Max(10, hitShape.LastBounds.Width - borderThickness * 2), Math.Max(10, hitShape.LastBounds.Height - borderThickness * 2));

                if (hitShape.ShapeType != "Image" && innerTextBounds.Contains(mouseX, mouseY))
                {
                    // Clicked in Text area -> Position text cursor inside shape, DO NOT drag
                    IsDraggingShape = false;
                    DragShapeHandleIndex = -1;
                    using (Font txtFont = new Font(hitShape.FontFamily, hitShape.FontSize * ZoomLevel, hitShape.Bold ? FontStyle.Bold : FontStyle.Regular))
                    {
                        hitShape.TextCursorIndex = ShapeRenderer.GetCharIndexFromPoint(g, hitShape, innerTextBounds, txtFont, ZoomLevel, mouseX, mouseY);
                    }
                }
                else
                {
                    // Clicked on Border -> Drag shape
                    IsDraggingShape = true;
                    DragShapeHandleIndex = -1;
                    ShapeDragStartMouse = new Point(mouseX, mouseY);
                    ShapeDragStartScrollY = ScrollY;

                    if (hitShape.WrapMode == 0)
                    {
                        hitShape.WrapMode = 1;
                        int pTopY = pageY + topM_m;
                        hitShape.PageX = (int)((hitShape.LastBounds.X - pageX) / Math.Max(0.2f, ZoomLevel));
                        hitShape.PageY = (int)((hitShape.LastBounds.Y - pTopY) / Math.Max(0.2f, ZoomLevel));
                        hitShape.PageNumber = 0;
                    }
                    ShapeDragStartOffset = new Point(hitShape.PageX, hitShape.PageY);
                    ShapeDragStartPageNumber = hitShape.PageNumber;
                }

                if (ribbon != null) ribbon.ActiveRibbonTab = 6; // Chuyển sang Tab "Định dạng Hình dạng"
                return;
            }
            else
            {
                if (ActiveShape != null)
                {
                    ActiveShape.IsSelected = false;
                    ActiveShape.ShowLayoutOptions = false;
                    ActiveShape = null;
                }
            }

            if (hitChart != null)
            {
                foreach (var ch in DocumentCharts) ch.IsSelected = false;
                ActiveChart = hitChart;
                hitChart.IsSelected = true;
                IsDraggingChart = true;
                DragChartHandleIndex = -1;
                ChartDragStartMouse = new Point(mouseX, mouseY);
                ChartDragStartScrollY = ScrollY;

                if (hitChart.WrapMode == 0)
                {
                    hitChart.WrapMode = 1; // Auto convert to Square when dragged
                    int pTopY = pageY + topM_m;
                    hitChart.PageX = (int)((hitChart.LastBounds.X - pageX) / Math.Max(0.2f, ZoomLevel));
                    hitChart.PageY = (int)((hitChart.LastBounds.Y - pTopY) / Math.Max(0.2f, ZoomLevel));
                    hitChart.PageNumber = 0;
                }
                ChartDragStartOffset = new Point(hitChart.PageX, hitChart.PageY);
                ChartDragStartPageNumber = hitChart.PageNumber;
                if (ribbon != null) ribbon.ActiveRibbonTab = 6;
                return;
            }
            else
            {
                if (ActiveChart != null)
                {
                    ActiveChart.IsSelected = false;
                    ActiveChart.ShowLayoutOptions = false;
                    ActiveChart.ShowElementsMenu = false;
                    ActiveChart.ShowStylesMenu = false;
                    ActiveChart = null;
                    if (ribbon != null && ribbon.ActiveRibbonTab == 6) ribbon.ActiveRibbonTab = 0; // Trở về tab Trang đầu
                }
            }



            if (IsEditingHeaderFooter)
            {
                IsEditingHeaderFooter = false;
                if (ribbon != null && ribbon.ActiveRibbonTab == 6) ribbon.ActiveRibbonTab = 0;
            }

            int index = GetCharIndexFromPoint(mouseX, mouseY, canvasWidth, g, ribbon);

            // Kiểm tra điều hướng Hyperlink khi Click / Ctrl+Click trên dòng Mục Lục (TOC)
            if ((Control.ModifierKeys & Keys.Control) != 0 || IsInTableOfContentsLine(index))
            {
                if (TryNavigateTableOfContents(index))
                {
                    UpdateRibbonState(ribbon);
                    return;
                }
            }

            _mouseDownPoint = new Point(mouseX, mouseY);
            _mouseDownCharIndex = index;
            _hasDraggedSelection = false;

            WriterFloatingMiniToolbarForm.CloseCurrent();

            CursorPosition = index;
            SelectionStart = index;
            SelectionEnd = index;
            IsSelecting = true;
            UpdateRibbonState(ribbon);
        }

        private bool IsInTableOfContentsLine(int charIdx)
        {
            string full = PieceTable.GetText();
            if (string.IsNullOrEmpty(full) || charIdx < 0 || charIdx >= full.Length) return false;
            int lStart = full.LastIndexOf('\n', Math.Max(0, charIdx - 1)) + 1;
            int lEnd = full.IndexOf('\n', charIdx);
            if (lEnd < 0) lEnd = full.Length;
            string line = full.Substring(lStart, lEnd - lStart).Trim();
            return (line.Contains("...") || line.Contains("---")) && line.Contains("Trang ");
        }

        private bool TryNavigateTableOfContents(int charIdx)
        {
            string full = PieceTable.GetText();
            if (string.IsNullOrEmpty(full) || charIdx < 0 || charIdx >= full.Length) return false;
            int lStart = full.LastIndexOf('\n', Math.Max(0, charIdx - 1)) + 1;
            int lEnd = full.IndexOf('\n', charIdx);
            if (lEnd < 0) lEnd = full.Length;
            string clickedLine = full.Substring(lStart, lEnd - lStart).Trim();

            if ((clickedLine.Contains("...") || clickedLine.Contains("---")) && clickedLine.Contains("Trang "))
            {
                int dotIdx = clickedLine.IndexOfAny(new char[] { '.', '-', '_' });
                if (dotIdx > 0)
                {
                    string headingTitle = clickedLine.Substring(0, dotIdx).Trim();
                    if (!string.IsNullOrEmpty(headingTitle))
                    {
                        int targetPos = full.IndexOf(headingTitle, 0, StringComparison.CurrentCultureIgnoreCase);
                        if (targetPos >= lStart && targetPos <= lEnd)
                        {
                            targetPos = full.IndexOf(headingTitle, lEnd, StringComparison.CurrentCultureIgnoreCase);
                        }
                        if (targetPos >= 0)
                        {
                            CursorPosition = targetPos;
                            SelectionStart = targetPos;
                            SelectionEnd = targetPos + headingTitle.Length;
                            int targetPage = Math.Max(1, (full.Substring(0, targetPos).Split('\n').Length / 38) + 1);
                            ScrollY = Math.Max(0, (targetPage - 1) * 1150);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void ApplyParagraphStyle(int styleIdx)
        {
            SaveUndoState();
            int minIdx = Math.Min(SelectionStart, SelectionEnd);
            string text = PieceTable.GetText();
            
            // Find paragraph bounds
            int pStart = 0;
            int pEnd = text.Length;
            for (int i = minIdx - 1; i >= 0; i--)
            {
                if (text[i] == '\n') { pStart = i + 1; break; }
            }
            for (int i = minIdx; i < text.Length; i++)
            {
                if (text[i] == '\n') { pEnd = i; break; }
            }

            int pLen = pEnd - pStart;
            if (pLen <= 0) return;

            string font = "Times New Roman";
            float size = 11.0f;
            bool bold = false;
            Color color = Color.Black;

            if (styleIdx == 0) // Normal
            {
                font = "Times New Roman"; size = 11.0f; bold = false; color = Color.Black;
            }
            else if (styleIdx == 1) // Heading 1
            {
                font = "Arial"; size = 18.0f; bold = true; color = Color.FromArgb(43, 87, 154);
            }
            else if (styleIdx == 2) // Heading 2
            {
                font = "Arial"; size = 14.0f; bold = true; color = Color.FromArgb(60, 100, 160);
            }
            else if (styleIdx == 3) // Title
            {
                font = "Segoe UI"; size = 24.0f; bold = true; color = Color.Black;
            }
            else if (styleIdx == 4) // Subtitle
            {
                font = "Segoe UI"; size = 14.0f; bold = false; color = Color.DimGray;
            }

            ApplyFormatToRange(pStart, pLen, font, size, bold, false, false, false, color, Color.Transparent);
            
            // Set typing format so cursor picks it up
            TypingFont = font;
            TypingFontSize = size;
            TypingBold = bold;
            TypingColor = color;
            TypingItalic = false;
            TypingUnderline = false;
            TypingStrike = false;
        }

        public void HandleMouseMove(int mouseX, int mouseY, int canvasWidth, Graphics g, RibbonBar ribbon)
        {
            HoveredMousePoint = new Point(mouseX, mouseY);

            // =========================================================================
            // -1. RULER DRAG IN PROGRESS
            // =========================================================================
            if (ActiveRulerDrag != RulerDragHandle.None)
            {
                Cursor.Current = Cursors.SizeWE;
                float delta = (mouseX - RulerDragStartPoint.X) / ZoomLevel;
                int pStart = ActiveRulerDragParaStart;
                float curLeft = GetLeftIndent(pStart);

                if (ActiveRulerDrag == RulerDragHandle.FirstLineIndent)
                {
                    ParagraphFirstLineIndents[pStart] = Math.Max(-curLeft, RulerDragInitialValue + delta);
                }
                else if (ActiveRulerDrag == RulerDragHandle.HangingIndent)
                {
                    ParagraphHangingIndents[pStart] = Math.Max(-curLeft, RulerDragInitialValue + delta);
                }
                else if (ActiveRulerDrag == RulerDragHandle.LeftIndent)
                {
                    ParagraphLeftIndents[pStart] = Math.Max(0, RulerDragInitialValue + delta);
                }
                else if (ActiveRulerDrag == RulerDragHandle.RightIndent)
                {
                    ParagraphRightIndents[pStart] = Math.Max(0, RulerDragInitialValue - delta);
                }
                else if (ActiveRulerDrag == RulerDragHandle.TabStop)
                {
                    var tList = GetTabStops(pStart);
                    if (DraggedTabStopIndex >= 0 && DraggedTabStopIndex < tList.Count)
                    {
                        tList[DraggedTabStopIndex].PositionPt = Math.Max(0, RulerDragInitialValue + delta);
                    }
                }

                RulerGuideLineX = mouseX;
                return;
            }

            // 0. Nếu chuột đang ở thanh Ribbon / Title bar hoặc thước kẻ phía trên -> kiểm tra hover thước kẻ
            if (ribbon != null && ribbon.ShowRuler && mouseY >= 155 && mouseY <= 195)
            {
                if (FirstLineIndentHitRect.Contains(mouseX, mouseY) || HangingIndentHitRect.Contains(mouseX, mouseY) ||
                    LeftIndentHitRect.Contains(mouseX, mouseY) || RightIndentHitRect.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.SizeWE;
                    return;
                }
                if (RulerTabSelectorRect.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.Hand;
                    return;
                }
            }

            if (mouseY < 155 || (ribbon != null && !ribbon.ShowRuler && mouseY < 155) || (ribbon != null && ribbon.ShowRuler && mouseY < 185))
            {
                Cursor.Current = Cursors.Default;
                return;
            }

            // =========================================================================
            // 1. ACTIVE DRAG / RESIZE OPERATIONS (Phải thực thi đầu tiên khi đang kéo)
            // =========================================================================
            if (IsDraggingShape && ActiveShape != null && DragShapeHandleIndex == -1)
            {
                Cursor.Current = Cursors.SizeAll;
                UpdateShapeDragPosition(mouseX, mouseY, canvasWidth, ribbon);
                return;
            }

            if (DragShapeHandleIndex >= 0 && ActiveShape != null)
            {
                int dx = (int)((mouseX - ShapeDragStartMouse.X) / ZoomLevel);
                int dy = (int)((mouseY - ShapeDragStartMouse.Y) / ZoomLevel);
                float aspect = (float)ShapeDragStartSize.Width / Math.Max(1, ShapeDragStartSize.Height);

                // Corner Handles (0=NW, 2=NE, 4=SE, 6=SW) maintain aspect ratio (no distortion / kéo vuông góc không méo hình)
                if (DragShapeHandleIndex == 4) // Bottom-Right (SE)
                {
                    float diag = (dx + dy * aspect) / 2.0f;
                    int newW = Math.Max(40, (int)(ShapeDragStartSize.Width + diag));
                    int newH = Math.Max(30, (int)(newW / aspect));
                    ActiveShape.WidthDelta = newW - ActiveShape.BaseWidth;
                    ActiveShape.HeightDelta = newH - ActiveShape.BaseHeight;
                }
                else if (DragShapeHandleIndex == 2) // Top-Right (NE)
                {
                    float diag = (dx - dy * aspect) / 2.0f;
                    int newW = Math.Max(40, (int)(ShapeDragStartSize.Width + diag));
                    int newH = Math.Max(30, (int)(newW / aspect));
                    ActiveShape.WidthDelta = newW - ActiveShape.BaseWidth;
                    ActiveShape.HeightDelta = newH - ActiveShape.BaseHeight;
                    ActiveShape.PageY = ShapeDragStartOffset.Y + (ShapeDragStartSize.Height - newH);
                }
                else if (DragShapeHandleIndex == 0) // Top-Left (NW)
                {
                    float diag = (-dx - dy * aspect) / 2.0f;
                    int newW = Math.Max(40, (int)(ShapeDragStartSize.Width + diag));
                    int newH = Math.Max(30, (int)(newW / aspect));
                    ActiveShape.WidthDelta = newW - ActiveShape.BaseWidth;
                    ActiveShape.HeightDelta = newH - ActiveShape.BaseHeight;
                    ActiveShape.PageX = ShapeDragStartOffset.X + (ShapeDragStartSize.Width - newW);
                    ActiveShape.PageY = ShapeDragStartOffset.Y + (ShapeDragStartSize.Height - newH);
                }
                else if (DragShapeHandleIndex == 6) // Bottom-Left (SW)
                {
                    float diag = (-dx + dy * aspect) / 2.0f;
                    int newW = Math.Max(40, (int)(ShapeDragStartSize.Width + diag));
                    int newH = Math.Max(30, (int)(newW / aspect));
                    ActiveShape.WidthDelta = newW - ActiveShape.BaseWidth;
                    ActiveShape.HeightDelta = newH - ActiveShape.BaseHeight;
                    ActiveShape.PageX = ShapeDragStartOffset.X + (ShapeDragStartSize.Width - newW);
                }
                // Edge Handles (1=N, 3=E, 5=S, 7=W) stretch single dimension
                else if (DragShapeHandleIndex == 3) // Right (E)
                {
                    int newW = Math.Max(40, ShapeDragStartSize.Width + dx);
                    ActiveShape.WidthDelta = newW - ActiveShape.BaseWidth;
                }
                else if (DragShapeHandleIndex == 7) // Left (W)
                {
                    int newW = Math.Max(40, ShapeDragStartSize.Width - dx);
                    ActiveShape.WidthDelta = newW - ActiveShape.BaseWidth;
                    ActiveShape.PageX = ShapeDragStartOffset.X + (ShapeDragStartSize.Width - newW);
                }
                else if (DragShapeHandleIndex == 5) // Bottom (S)
                {
                    int newH = Math.Max(30, ShapeDragStartSize.Height + dy);
                    ActiveShape.HeightDelta = newH - ActiveShape.BaseHeight;
                }
                else if (DragShapeHandleIndex == 1) // Top (N)
                {
                    int newH = Math.Max(30, ShapeDragStartSize.Height - dy);
                    ActiveShape.HeightDelta = newH - ActiveShape.BaseHeight;
                    ActiveShape.PageY = ShapeDragStartOffset.Y + (ShapeDragStartSize.Height - newH);
                }

                Cursor[] hCursors = new Cursor[]
                {
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
                };
                if (DragShapeHandleIndex >= 0 && DragShapeHandleIndex < 8)
                {
                    Cursor.Current = hCursors[DragShapeHandleIndex];
                }
                return;
            }

            if (IsDraggingChart && ActiveChart != null && DragChartHandleIndex == -1)
            {
                Cursor.Current = Cursors.SizeAll;
                UpdateChartDragPosition(mouseX, mouseY, canvasWidth, ribbon);
                return;
            }

            if (DragChartHandleIndex >= 0 && ActiveChart != null)
            {
                int dx = (int)((mouseX - ChartDragStartMouse.X) / ZoomLevel);
                int dy = (int)((mouseY - ChartDragStartMouse.Y) / ZoomLevel);

                // Right handles: 2 (NE), 3 (E), 4 (SE)
                if (DragChartHandleIndex == 2 || DragChartHandleIndex == 3 || DragChartHandleIndex == 4)
                {
                    ActiveChart.WidthDelta = Math.Max(-200, ChartDragStartSize.Width + dx - ActiveChart.BaseWidth);
                }
                // Left handles: 0 (NW), 6 (SW), 7 (W)
                else if (DragChartHandleIndex == 0 || DragChartHandleIndex == 6 || DragChartHandleIndex == 7)
                {
                    int newW = Math.Max(200, ChartDragStartSize.Width - dx);
                    ActiveChart.WidthDelta = newW - ActiveChart.BaseWidth;
                    ActiveChart.PageX = ChartDragStartOffset.X + (ChartDragStartSize.Width - newW);
                }

                // Bottom handles: 4 (SE), 5 (S), 6 (SW)
                if (DragChartHandleIndex == 4 || DragChartHandleIndex == 5 || DragChartHandleIndex == 6)
                {
                    ActiveChart.HeightDelta = Math.Max(-150, ChartDragStartSize.Height + dy - ActiveChart.BaseHeight);
                }
                // Top handles: 0 (NW), 1 (N), 2 (NE)
                else if (DragChartHandleIndex == 0 || DragChartHandleIndex == 1 || DragChartHandleIndex == 2)
                {
                    int newH = Math.Max(120, ChartDragStartSize.Height - dy);
                    ActiveChart.HeightDelta = newH - ActiveChart.BaseHeight;
                    ActiveChart.PageY = ChartDragStartOffset.Y + (ChartDragStartSize.Height - newH);
                }

                Cursor[] hCursors = new Cursor[]
                {
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
                };
                if (DragChartHandleIndex >= 0 && DragChartHandleIndex < 8)
                {
                    Cursor.Current = hCursors[DragChartHandleIndex];
                }
                return;
            }

            if (IsDraggingHeaderBanner)
            {
                Cursor.Current = Cursors.SizeAll;
                HeaderBannerXOffset += (int)((mouseX - HeaderDragStartMouse.X) / ZoomLevel);
                HeaderBannerYOffset += (int)((mouseY - HeaderDragStartMouse.Y) / ZoomLevel);

                if (Math.Abs(HeaderBannerXOffset) <= 4) HeaderBannerXOffset = 0;
                if (Math.Abs(HeaderBannerYOffset) <= 4) HeaderBannerYOffset = 0;

                int centerDelta = HeaderBannerXOffset + HeaderBannerWidthDelta / 2;
                if (Math.Abs(centerDelta) <= 4) HeaderBannerXOffset -= centerDelta;

                HeaderBannerWidthDelta = Math.Max(-500, Math.Min(500, HeaderBannerWidthDelta));
                HeaderBannerHeightDelta = Math.Max(-20, Math.Min(300, HeaderBannerHeightDelta));
                HeaderBannerXOffset = Math.Max(-300, Math.Min(300, HeaderBannerXOffset));
                HeaderBannerYOffset = Math.Max(-100, Math.Min(300, HeaderBannerYOffset));

                HeaderDragStartMouse = new Point(mouseX, mouseY);
                return;
            }

            if (IsResizingHeaderBanner)
            {
                int hIdx = HeaderResizeHandleIndex;
                if (hIdx == 0 || hIdx == 7) Cursor.Current = Cursors.SizeNWSE;
                else if (hIdx == 1 || hIdx == 6) Cursor.Current = Cursors.SizeNS;
                else if (hIdx == 2 || hIdx == 5) Cursor.Current = Cursors.SizeNESW;
                else if (hIdx == 3 || hIdx == 4) Cursor.Current = Cursors.SizeWE;

                int dx = (int)((mouseX - HeaderDragStartMouse.X) / ZoomLevel);
                int dy = (int)((mouseY - HeaderDragStartMouse.Y) / ZoomLevel);
                if (HeaderResizeHandleIndex == 2 || HeaderResizeHandleIndex == 4 || HeaderResizeHandleIndex == 7)
                    HeaderBannerWidthDelta += dx;
                else if (HeaderResizeHandleIndex == 0 || HeaderResizeHandleIndex == 3 || HeaderResizeHandleIndex == 5)
                {
                    HeaderBannerWidthDelta -= dx;
                    HeaderBannerXOffset += dx;
                }

                if (HeaderResizeHandleIndex == 5 || HeaderResizeHandleIndex == 6 || HeaderResizeHandleIndex == 7)
                    HeaderBannerHeightDelta += dy;
                else if (HeaderResizeHandleIndex == 0 || HeaderResizeHandleIndex == 1 || HeaderResizeHandleIndex == 2)
                {
                    HeaderBannerHeightDelta -= dy;
                    HeaderBannerYOffset += dy;
                }

                if (Math.Abs(HeaderBannerXOffset) <= 4) HeaderBannerXOffset = 0;
                if (Math.Abs(HeaderBannerWidthDelta) <= 4) HeaderBannerWidthDelta = 0;

                HeaderBannerWidthDelta = Math.Max(-500, Math.Min(500, HeaderBannerWidthDelta));
                HeaderBannerHeightDelta = Math.Max(-20, Math.Min(300, HeaderBannerHeightDelta));
                HeaderBannerXOffset = Math.Max(-300, Math.Min(300, HeaderBannerXOffset));
                HeaderBannerYOffset = Math.Max(-100, Math.Min(300, HeaderBannerYOffset));

                HeaderDragStartMouse = new Point(mouseX, mouseY);
                return;
            }

            // =========================================================================
            // 2. HOVER CURSOR CHECKS (Chỉ kiểm tra khi người dùng không kéo)
            // =========================================================================

            // 1. Check Hover Cursors on Active Shape (Handles, Rotation, Layout Button, Body)
            if (ActiveShape != null && !ActiveShape.LastBounds.IsEmpty)
            {
                Rectangle sBounds = ActiveShape.LastBounds;
                int hs = (int)(7 * ZoomLevel);

                // Check Layout Options button
                Rectangle wrapBtn = new Rectangle(sBounds.Right + 5, sBounds.Top, 22, 22);
                if (wrapBtn.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.Hand;
                    return;
                }

                // Check Layout Options Popup (if open)
                if (ActiveShape.ShowLayoutOptions)
                {
                    int popW = 185; int popH = 250;
                    int popX = wrapBtn.Right + 8; int popY = wrapBtn.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        Rectangle closeBtn = new Rectangle(popX + popW - 24, popY + 6, 20, 20);
                        Rectangle inCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                        if (closeBtn.Contains(mouseX, mouseY) || inCard.Contains(mouseX, mouseY))
                        {
                            Cursor.Current = Cursors.Hand;
                            return;
                        }
                        for (int r = 0; r < 2; r++)
                        {
                            for (int c = 0; c < 3; c++)
                            {
                                Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                                if (cardBox.Contains(mouseX, mouseY))
                                {
                                    Cursor.Current = Cursors.Hand;
                                    return;
                                }
                            }
                        }
                        Cursor.Current = Cursors.Default;
                        return;
                    }
                }

                // Check Rotation handle (circle at top)
                int rotY = sBounds.Top - (int)(22 * ZoomLevel);
                int rotX = sBounds.Left + sBounds.Width / 2;
                Rectangle rotRect = new Rectangle(rotX - hs / 2 - 4, rotY - 4, hs + 8, hs + 8);
                if (rotRect.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.Hand;
                    return;
                }

                // Check 8 Handles on ActiveShape
                Point[] sHandles = new Point[]
                {
                    new Point(sBounds.Left - hs/2, sBounds.Top - hs/2),
                    new Point(sBounds.Left + sBounds.Width/2 - hs/2, sBounds.Top - hs/2),
                    new Point(sBounds.Right - hs/2, sBounds.Top - hs/2),
                    new Point(sBounds.Right - hs/2, sBounds.Top + sBounds.Height/2 - hs/2),
                    new Point(sBounds.Right - hs/2, sBounds.Bottom - hs/2),
                    new Point(sBounds.Left + sBounds.Width/2 - hs/2, sBounds.Bottom - hs/2),
                    new Point(sBounds.Left - hs/2, sBounds.Bottom - hs/2),
                    new Point(sBounds.Left - hs/2, sBounds.Top + sBounds.Height/2 - hs/2)
                };
                Cursor[] hCursors = new Cursor[]
                {
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
                };
                for (int h = 0; h < 8; h++)
                {
                    Rectangle hRect = new Rectangle(sHandles[h].X - 4, sHandles[h].Y - 4, hs + 8, hs + 8);
                    if (hRect.Contains(mouseX, mouseY))
                    {
                        Cursor.Current = hCursors[h];
                        return;
                    }
                }

                int borderThickness = Math.Max(6, (int)(8 * ZoomLevel));
                Rectangle innerRect = new Rectangle(sBounds.X + borderThickness, sBounds.Y + borderThickness, Math.Max(0, sBounds.Width - borderThickness * 2), Math.Max(0, sBounds.Height - borderThickness * 2));
                if (innerRect.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.IBeam;
                    return;
                }

                if (sBounds.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.SizeAll;
                    return;
                }
            }

            // Check Hover on any shape in DocumentShapes
            foreach (var sh in DocumentShapes)
            {
                if (!sh.LastBounds.IsEmpty && sh.LastBounds.Contains(mouseX, mouseY))
                {
                    int borderThickness = Math.Max(6, (int)(8 * ZoomLevel));
                    Rectangle innerRect = new Rectangle(sh.LastBounds.X + borderThickness, sh.LastBounds.Y + borderThickness, Math.Max(0, sh.LastBounds.Width - borderThickness * 2), Math.Max(0, sh.LastBounds.Height - borderThickness * 2));
                    if (innerRect.Contains(mouseX, mouseY))
                    {
                        Cursor.Current = Cursors.IBeam;
                        return;
                    }

                    Cursor.Current = Cursors.SizeAll;
                    return;
                }
            }

            // 2. Check Hover Cursors on Active Chart (Handles, Floating Buttons, Body)
            if (ActiveChart != null && !ActiveChart.LastBounds.IsEmpty)
            {
                Rectangle cBounds = ActiveChart.LastBounds;
                int hs = (int)(7 * ZoomLevel);

                if (!LayoutBtnChartRect.IsEmpty && LayoutBtnChartRect.Contains(mouseX, mouseY)) { Cursor.Current = Cursors.Hand; return; }
                if (!PlusBtnChartRect.IsEmpty && PlusBtnChartRect.Contains(mouseX, mouseY)) { Cursor.Current = Cursors.Hand; return; }
                if (!StyleBtnChartRect.IsEmpty && StyleBtnChartRect.Contains(mouseX, mouseY)) { Cursor.Current = Cursors.Hand; return; }
                if (!FilterBtnChartRect.IsEmpty && FilterBtnChartRect.Contains(mouseX, mouseY)) { Cursor.Current = Cursors.Hand; return; }

                if (ActiveChart.ShowLayoutOptions && !LayoutBtnChartRect.IsEmpty)
                {
                    int popW = 185; int popH = 250;
                    int popX = LayoutBtnChartRect.Right + 8; int popY = LayoutBtnChartRect.Top - 5;
                    Rectangle popRect = new Rectangle(popX, popY, popW, popH);
                    if (popRect.Contains(mouseX, mouseY))
                    {
                        Cursor.Current = Cursors.Hand;
                        return;
                    }
                }

                Point[] cHandles = new Point[]
                {
                    new Point(cBounds.Left - hs/2, cBounds.Top - hs/2),
                    new Point(cBounds.Left + cBounds.Width/2 - hs/2, cBounds.Top - hs/2),
                    new Point(cBounds.Right - hs/2, cBounds.Top - hs/2),
                    new Point(cBounds.Right - hs/2, cBounds.Top + cBounds.Height/2 - hs/2),
                    new Point(cBounds.Right - hs/2, cBounds.Bottom - hs/2),
                    new Point(cBounds.Left + cBounds.Width/2 - hs/2, cBounds.Bottom - hs/2),
                    new Point(cBounds.Left - hs/2, cBounds.Bottom - hs/2),
                    new Point(cBounds.Left - hs/2, cBounds.Top + cBounds.Height/2 - hs/2)
                };
                Cursor[] hCursors = new Cursor[]
                {
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
                    Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
                };
                for (int h = 0; h < 8; h++)
                {
                    Rectangle hRect = new Rectangle(cHandles[h].X - 4, cHandles[h].Y - 4, hs + 8, hs + 8);
                    if (hRect.Contains(mouseX, mouseY))
                    {
                        Cursor.Current = hCursors[h];
                        return;
                    }
                }

                if (cBounds.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.SizeAll;
                    return;
                }
            }

            // Check Hover on any chart in DocumentCharts
            foreach (var ch in DocumentCharts)
            {
                if (!ch.LastBounds.IsEmpty && ch.LastBounds.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.SizeAll;
                    return;
                }
            }

            if (IsEditingHeaderFooter && CurrentHeaderPreset == HeaderFooterPreset.Banded)
            {
                int stdW = ribbon != null && ribbon.IsLandscape ? 842 : 595;
                int stdH = ribbon != null && ribbon.IsLandscape ? 595 : 842;
                if (ViewMode == 0) { stdW = 750; }
                else if (ViewMode == 2) { stdW = Math.Max(700, (int)((canvasWidth - 80) / Math.Max(0.2f, ZoomLevel))); }

                int pageW = (int)(stdW * ZoomLevel);
                int pageH = (int)(stdH * ZoomLevel);
                int pageX = Math.Max(40, (canvasWidth - pageW) / 2);
                int pageY = 195 - ScrollY;

                int leftM_m = ribbon != null ? (int)(ribbon.GetEffectiveLeftMarginInches() * 54.0f * ZoomLevel) : (int)(45 * ZoomLevel);
                int rightM_m = ribbon != null ? (int)(ribbon.GetEffectiveRightMarginInches() * 54.0f * ZoomLevel) : (int)(45 * ZoomLevel);

                int bX = pageX + leftM_m + (int)(HeaderBannerXOffset * ZoomLevel);
                int bW = Math.Max((int)(50 * ZoomLevel), pageW - leftM_m - rightM_m + (int)(HeaderBannerWidthDelta * ZoomLevel));
                int bH = Math.Max((int)(16 * ZoomLevel), (int)(28 * ZoomLevel) + (int)(HeaderBannerHeightDelta * ZoomLevel));
                int bY = pageY + (int)(10 * ZoomLevel) + (int)(HeaderBannerYOffset * ZoomLevel);
                Rectangle bannerRect = new Rectangle(bX, bY, bW, bH);

                Point[] handlePts = new Point[]
                {
                    new Point(bannerRect.Left, bannerRect.Top),
                    new Point(bannerRect.Left + bannerRect.Width / 2, bannerRect.Top),
                    new Point(bannerRect.Right, bannerRect.Top),
                    new Point(bannerRect.Left, bannerRect.Top + bannerRect.Height / 2),
                    new Point(bannerRect.Right, bannerRect.Top + bannerRect.Height / 2),
                    new Point(bannerRect.Left, bannerRect.Bottom),
                    new Point(bannerRect.Left + bannerRect.Width / 2, bannerRect.Bottom),
                    new Point(bannerRect.Right, bannerRect.Bottom)
                };

                for (int hIdx = 0; hIdx < 8; hIdx++)
                {
                    Rectangle handleCircle = new Rectangle(handlePts[hIdx].X - 8, handlePts[hIdx].Y - 8, 16, 16);
                    if (handleCircle.Contains(mouseX, mouseY))
                    {
                        if (hIdx == 0 || hIdx == 7) Cursor.Current = Cursors.SizeNWSE;
                        else if (hIdx == 1 || hIdx == 6) Cursor.Current = Cursors.SizeNS;
                        else if (hIdx == 2 || hIdx == 5) Cursor.Current = Cursors.SizeNESW;
                        else if (hIdx == 3 || hIdx == 4) Cursor.Current = Cursors.SizeWE;
                        return;
                    }
                }

                Rectangle handleTag = new Rectangle(bannerRect.X + bannerRect.Width / 2 - 30, bannerRect.Y - 16, 60, 16);
                if (handleTag.Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.SizeAll;
                    return;
                }

                if (bannerRect.Contains(mouseX, mouseY))
                {
                    string tStr = string.IsNullOrEmpty(HeaderTitleText) ? "TIÊU ĐỀ ĐẦU TRANG" : HeaderTitleText;
                    using (Font titleFont = new Font("Segoe UI", 10.0f * ZoomLevel, FontStyle.Bold))
                    {
                        SizeF sz = g.MeasureString(tStr, titleFont);
                        float txtX = bannerRect.X + (bannerRect.Width - sz.Width) / 2;
                        float txtY = bannerRect.Y + (bannerRect.Height - sz.Height) / 2;
                        Rectangle textFrame = new Rectangle((int)txtX - 6, (int)txtY - 2, (int)sz.Width + 12, (int)sz.Height + 4);

                        if (textFrame.Contains(mouseX, mouseY))
                        {
                            Cursor.Current = Cursors.IBeam;
                        }
                        else
                        {
                            Cursor.Current = Cursors.SizeAll;
                        }
                        return;
                    }
                }
            }



            if (IsDraggingMove && DragTableIndex >= 0)
            {
                Cursor.Current = Cursors.SizeAll;
                int dx = (int)((mouseX - TableDragStartMouse.X) / Math.Max(0.2f, ZoomLevel));
                int dy = (int)((mouseY - TableDragStartMouse.Y) / Math.Max(0.2f, ZoomLevel));

                // Di chuyển tự do toàn diện tới bất kỳ vị trí nào trên trang (trái, phải, lề mép) 100% mượt mà như hình ảnh
                TablePageX[DragTableIndex] = TableDragStartOffset.X + dx;
                TablePageY[DragTableIndex] = TableDragStartOffset.Y + dy;
                return;
            }

            if (IsDraggingResize && DragTableIndex >= 0)
            {
                Cursor.Current = Cursors.SizeNWSE;
                int totalDx = mouseX - DragResizeInitialMouse.X;
                int totalDy = mouseY - DragResizeInitialMouse.Y;

                if (TableColWidths.ContainsKey(DragTableIndex) && TableRowHeights.ContainsKey(DragTableIndex))
                {
                    var colList = TableColWidths[DragTableIndex];
                    var rowList = TableRowHeights[DragTableIndex];
                    int numCols = colList.Count;
                    int numRows = rowList.Count;

                    if (numCols > 0 && numRows > 0 && DragResizeInitialW > 0 && DragResizeInitialH > 0)
                    {
                        // Khóa tỉ lệ khung hình (Aspect Ratio) đồng dạng theo đường chéo giống như hình ảnh/khung vẽ
                        float aspect = (float)DragResizeInitialW / Math.Max(1, DragResizeInitialH);
                        float diag = (totalDx + totalDy * aspect) / 2.0f;
                        int targetW = Math.Max(numCols * 20, (int)(DragResizeInitialW + diag));
                        int targetH = Math.Max(numRows * 15, (int)(targetW / aspect));

                        int assignedW = 0;
                        for (int c = 0; c < numCols; c++)
                        {
                            if (c == numCols - 1)
                            {
                                colList[c] = Math.Max(15, targetW - assignedW);
                            }
                            else
                            {
                                int initW = (c < DragResizeInitialColWidths.Count) ? DragResizeInitialColWidths[c] : (DragResizeInitialW / numCols);
                                int colW = Math.Max(15, (int)Math.Round((double)initW * targetW / DragResizeInitialW));
                                colList[c] = colW;
                                assignedW += colW;
                            }
                        }

                        int assignedH = 0;
                        for (int r = 0; r < numRows; r++)
                        {
                            if (r == numRows - 1)
                            {
                                rowList[r] = Math.Max(15, targetH - assignedH);
                            }
                            else
                            {
                                int initH = (r < DragResizeInitialRowHeights.Count) ? DragResizeInitialRowHeights[r] : (DragResizeInitialH / numRows);
                                int rowH = Math.Max(15, (int)Math.Round((double)initH * targetH / DragResizeInitialH));
                                rowList[r] = rowH;
                                assignedH += rowH;
                            }
                        }
                    }
                }
                return;
            }

            if (IsDraggingColGridline && DragGridlineTableIndex >= 0 && DragGridlineColIndex >= 0)
            {
                Cursor.Current = Cursors.VSplit;
                int totalDx = mouseX - DragGridlineInitialMouse.X;
                if (TableColWidths.ContainsKey(DragGridlineTableIndex))
                {
                    var list = TableColWidths[DragGridlineTableIndex];
                    int cIdx = DragGridlineColIndex;
                    if (cIdx >= 0 && cIdx < list.Count - 1)
                    {
                        int newW1 = Math.Max(20, DragGridlineInitialWidth1 + totalDx);
                        int delta = newW1 - DragGridlineInitialWidth1;
                        if (DragGridlineInitialWidth2 - delta >= 20)
                        {
                            list[cIdx] = newW1;
                            list[cIdx + 1] = DragGridlineInitialWidth2 - delta;
                        }
                    }
                    else if (cIdx == list.Count - 1) // Cho phép kéo nới rộng/thu hẹp cột cuối cùng
                    {
                        list[cIdx] = Math.Max(20, DragGridlineInitialWidth1 + totalDx);
                    }
                }
                return;
            }

            if (IsDraggingRowGridline && DragGridlineTableIndex >= 0 && DragGridlineRowIndex >= 0)
            {
                Cursor.Current = Cursors.HSplit;
                int totalDy = mouseY - DragGridlineInitialMouse.Y;
                if (TableRowHeights.ContainsKey(DragGridlineTableIndex))
                {
                    var list = TableRowHeights[DragGridlineTableIndex];
                    int rIdx = DragGridlineRowIndex;
                    if (rIdx >= 0 && rIdx < list.Count)
                    {
                        list[rIdx] = Math.Max(15, DragGridlineInitialHeight + totalDy);
                    }
                }
                return;
            }

            // Update hover cursors for handles and gridlines
            bool hoverSet = false;
            for (int t = 0; t < MoveHandleRects.Count; t++)
            {
                if (MoveHandleRects[t].Contains(mouseX, mouseY))
                {
                    Cursor.Current = Cursors.SizeAll;
                    hoverSet = true;
                    break;
                }
            }
            if (!hoverSet)
            {
                for (int t = 0; t < ResizeHandleRects.Count; t++)
                {
                    if (ResizeHandleRects[t].Contains(mouseX, mouseY))
                    {
                        Cursor.Current = Cursors.SizeNWSE;
                        hoverSet = true;
                        break;
                    }
                }
            }
            if (!hoverSet)
            {
                foreach (var kvp in TableColWidths)
                {
                    int tIdx = kvp.Key;
                    var colWidths = kvp.Value;
                    if (tIdx < MoveHandleRects.Count)
                    {
                        int tX = MoveHandleRects[tIdx].Left + 25;
                        int tY = MoveHandleRects[tIdx].Top + 25;
                        int tH = 100;
                        if (tIdx < ResizeHandleRects.Count) tH = (ResizeHandleRects[tIdx].Top + 10) - tY;

                        int curX = tX;
                        for (int c = 0; c < colWidths.Count; c++)
                        {
                            curX += colWidths[c];
                            Rectangle lineHit = new Rectangle(curX - 6, tY, 12, tH);
                            if (lineHit.Contains(mouseX, mouseY))
                            {
                                Cursor.Current = Cursors.VSplit;
                                hoverSet = true;
                                break;
                            }
                        }
                    }
                    if (hoverSet) break;
                }
            }
            if (!hoverSet)
            {
                foreach (var kvp in TableRowHeights)
                {
                    int tIdx = kvp.Key;
                    var rowHeights = kvp.Value;
                    if (tIdx < MoveHandleRects.Count)
                    {
                        int tX = MoveHandleRects[tIdx].Left + 25;
                        int tY = MoveHandleRects[tIdx].Top + 25;
                        int tW = 200;
                        if (tIdx < ResizeHandleRects.Count) tW = (ResizeHandleRects[tIdx].Left + 10) - tX;

                        int curY = tY;
                        for (int r = 0; r < rowHeights.Count; r++)
                        {
                            curY += rowHeights[r];
                            Rectangle lineHit = new Rectangle(tX, curY - 6, tW, 12);
                            if (lineHit.Contains(mouseX, mouseY))
                            {
                                Cursor.Current = Cursors.HSplit;
                                hoverSet = true;
                                break;
                            }
                        }
                    }
                    if (hoverSet) break;
                }
            }

            if (!hoverSet && ActiveChart != null && !ActiveChartLastBounds.IsEmpty)
            {
                // Floating buttons
                if ((!LayoutBtnChartRect.IsEmpty && LayoutBtnChartRect.Contains(mouseX, mouseY)) ||
                    (!PlusBtnChartRect.IsEmpty && PlusBtnChartRect.Contains(mouseX, mouseY)) ||
                    (!StyleBtnChartRect.IsEmpty && StyleBtnChartRect.Contains(mouseX, mouseY)) ||
                    (!FilterBtnChartRect.IsEmpty && FilterBtnChartRect.Contains(mouseX, mouseY)))
                {
                    Cursor.Current = Cursors.Hand;
                    hoverSet = true;
                }
                else
                {
                    // 8 Handles
                    int hs = (int)(7 * ZoomLevel);
                    int cX = ActiveChartLastBounds.X; int cY = ActiveChartLastBounds.Y;
                    int cW = ActiveChartLastBounds.Width; int cH = ActiveChartLastBounds.Height;
                    Point[] chartHandles = new Point[]
                    {
                        new Point(cX - hs/2, cY - hs/2),
                        new Point(cX + cW/2 - hs/2, cY - hs/2),
                        new Point(cX + cW - hs/2, cY - hs/2),
                        new Point(cX + cW - hs/2, cY + cH/2 - hs/2),
                        new Point(cX + cW - hs/2, cY + cH - hs/2),
                        new Point(cX + cW/2 - hs/2, cY + cH - hs/2),
                        new Point(cX - hs/2, cY + cH - hs/2),
                        new Point(cX - hs/2, cY + cH/2 - hs/2)
                    };
                    Cursor[] hCursors = new Cursor[]
                    {
                        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
                        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
                    };

                    for (int h = 0; h < 8; h++)
                    {
                        Rectangle hRect = new Rectangle(chartHandles[h].X - 4, chartHandles[h].Y - 4, hs + 8, hs + 8);
                        if (hRect.Contains(mouseX, mouseY))
                        {
                            Cursor.Current = hCursors[h];
                            hoverSet = true;
                            break;
                        }
                    }

                    if (!hoverSet && ActiveChartLastBounds.Contains(mouseX, mouseY))
                    {
                        Cursor.Current = Cursors.SizeAll;
                        hoverSet = true;
                    }
                }
            }

            if (!hoverSet && Comments != null && Comments.Count > 0)
            {
                foreach (var kvp in CommentBadgeHitRects)
                {
                    if (kvp.Value.Contains(mouseX, mouseY))
                    {
                        HoveredCommentId = kvp.Key;
                        Cursor.Current = Cursors.Hand;
                        hoverSet = true;
                        break;
                    }
                }
                if (!hoverSet)
                {
                    foreach (var kvp in CommentAnchorTextRects)
                    {
                        if (kvp.Value.Contains(mouseX, mouseY))
                        {
                            HoveredCommentId = kvp.Key;
                            Cursor.Current = Cursors.Hand;
                            hoverSet = true;
                            break;
                        }
                    }
                }
                if (!hoverSet && HoveredCommentId != -1)
                {
                    HoveredCommentId = -1;
                }
            }

            bool isInsideAnyPage = false;
            if (!hoverSet)
            {
                int stdW = ribbon != null && ribbon.IsLandscape ? 842 : 595;
                int stdH = ribbon != null && ribbon.IsLandscape ? 595 : 842;
                if (ViewMode == 0) { stdW = 750; }
                else if (ViewMode == 2) { stdW = Math.Max(700, (int)((canvasWidth - 80) / Math.Max(0.2f, ZoomLevel))); }

                int pageW = (int)(stdW * ZoomLevel);
                int pageH = (int)(stdH * ZoomLevel);
                int pageX = Math.Max(40, (canvasWidth - pageW) / 2);
                int startPageY = 195 - ScrollY;

                for (int pg = 0; pg < Math.Max(1, CalculatedPageCount); pg++)
                {
                    int pY = startPageY + pg * (pageH + 30);
                    Rectangle pageRect = new Rectangle(pageX, pY, pageW, pageH);
                    if (pageRect.Contains(mouseX, mouseY))
                    {
                        isInsideAnyPage = true;
                        break;
                    }
                }

                if (isInsideAnyPage)
                {
                    Cursor.Current = Cursors.IBeam;
                }
                else
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            if (IsSelecting)
            {
                int moveDist = Math.Abs(mouseX - _mouseDownPoint.X) + Math.Abs(mouseY - _mouseDownPoint.Y);
                if (moveDist >= 8)
                {
                    _hasDraggedSelection = true;
                    int index = GetCharIndexFromPoint(mouseX, mouseY, canvasWidth, g, ribbon);
                    SelectionEnd = index;
                    CursorPosition = index;
                    UpdateRibbonState(ribbon);
                    WriterFloatingMiniToolbarForm.HideToolbar();
                }
            }
            else if (SelectionStart != SelectionEnd && Math.Abs(SelectionEnd - SelectionStart) > 0 && _hasDraggedSelection)
            {
                // Mini-Toolbar hover visibility tracking (keep fixed position, do NOT move toolbar)
                Point screenMouse = Cursor.Position;
                if (!WriterFloatingMiniToolbarPopup.IsMouseOverToolbar(screenMouse))
                {
                    if (WriterFloatingMiniToolbarPopup.CanAutoDismiss())
                    {
                        if (!isInsideAnyPage || !WriterFloatingMiniToolbarPopup.IsNearToolbar(screenMouse, 140))
                        {
                            // Mouse moved away (approx 3 cm / 140px) or out of the white page -> Auto dismiss!
                            WriterFloatingMiniToolbarPopup.CloseCurrent();
                        }
                        else
                        {
                            WriterFloatingMiniToolbarPopup.ShowToolbar();
                        }
                    }
                }
            }
            else
            {
                WriterFloatingMiniToolbarPopup.CloseCurrent();
            }
        }

        public void HandleDoubleClick(int mouseX, int mouseY, int canvasWidth, Graphics g, RibbonBar ribbon)
        {
            foreach (var sh in DocumentShapes)
            {
                if (!sh.LastBounds.IsEmpty && sh.LastBounds.Contains(mouseX, mouseY))
                {
                    ActiveShape = sh;
                    sh.IsSelected = true;
                    if (sh.ShapeType == "SmartArt" || !string.IsNullOrEmpty(sh.SmartArtType))
                    {
                        OpenSmartArtEditor(sh);
                        return;
                    }
                }
            }

            foreach (var ch in DocumentCharts)
            {
                if (!ch.LastBounds.IsEmpty && ch.LastBounds.Contains(mouseX, mouseY))
                {
                    ActiveChart = ch;
                    ch.IsSelected = true;
                    if (ribbon != null) ribbon.ActiveRibbonTab = 6;

                    DocChartData cd = ch.Data ?? DocChartData.CreateDefault("Column_Clustered");
                    ch.Data = cd;

                    int titleH = (int)(38 * ZoomLevel);
                    Rectangle titleZone = new Rectangle(ch.LastBounds.X, ch.LastBounds.Y, ch.LastBounds.Width, titleH);
                    if (titleZone.Contains(mouseX, mouseY))
                    {
                        // Double clicked on Chart Title -> Open Rename Form!
                        Point scrPt = new Point(ch.LastBounds.X + ch.LastBounds.Width / 4, ch.LastBounds.Y + 10);
                        ChartTitleRenameForm renameDlg = new ChartTitleRenameForm(cd, scrPt);
                        renameDlg.OnTitleChanged = (newTitle, showTitle) =>
                        {
                            cd.Title = newTitle;
                            cd.ShowTitle = showTitle;
                        };
                        renameDlg.ShowDialog();
                    }
                    else
                    {
                        OpenChartExcelEditor(ch);
                    }
                    return;
                }
            }

            int stdW = ribbon != null && ribbon.IsLandscape ? 842 : 595;
            int stdH = ribbon != null && ribbon.IsLandscape ? 595 : 842;
            if (ViewMode == 0) { stdW = 750; }
            else if (ViewMode == 2) { stdW = Math.Max(700, (int)((canvasWidth - 80) / Math.Max(0.2f, ZoomLevel))); }

            int pageW = (int)(stdW * ZoomLevel);
            int pageH = (int)(stdH * ZoomLevel);
            int pageX = Math.Max(40, (canvasWidth - pageW) / 2);
            int pageY = 195 - ScrollY;

            Rectangle headerZone = new Rectangle(pageX, pageY, pageW, (int)(60 * ZoomLevel));
            Rectangle footerZone = new Rectangle(pageX, pageY + pageH - (int)(60 * ZoomLevel), pageW, (int)(60 * ZoomLevel));

            if (headerZone.Contains(mouseX, mouseY))
            {
                IsEditingHeaderFooter = true;
                IsEditingHeaderMode = true;
                IsHeaderTextSelected = true;
                if (CurrentHeaderPreset == HeaderFooterPreset.None) CurrentHeaderPreset = HeaderFooterPreset.Banded;
                if (ribbon != null) ribbon.ActiveRibbonTab = 6;
            }
            else if (footerZone.Contains(mouseX, mouseY))
            {
                IsEditingHeaderFooter = true;
                IsEditingHeaderMode = false;
                if (CurrentFooterPreset == HeaderFooterPreset.None) CurrentFooterPreset = HeaderFooterPreset.Banded;
                if (ribbon != null) ribbon.ActiveRibbonTab = 6;
            }
            else if (mouseX >= pageX && mouseX <= pageX + pageW)
            {
                // MS Word Click-and-Type: double click in empty page area below text or in line gap to write
                string fullText = PieceTable.GetText();
                if (RenderedLines.Count > 0)
                {
                    var lastLine = RenderedLines[RenderedLines.Count - 1];
                    if (mouseY > lastLine.ScreenY + lastLine.ScreenH)
                    {
                        int distY = mouseY - (lastLine.ScreenY + lastLine.ScreenH);
                        int lineH = Math.Max(20, (int)(24 * ZoomLevel));
                        int entersNeeded = Math.Max(1, distY / lineH);
                        string enters = new string('\n', entersNeeded);

                        int insPos = fullText.Length;
                        PieceTable.Insert(insPos, enters);
                        AdjustFormattedRunsOnInsert(insPos, enters.Length);
                        CursorPosition = insPos + enters.Length;
                        SelectionStart = CursorPosition;
                        SelectionEnd = CursorPosition;
                        return;
                    }
                }

                int charIdx = GetCharIndexFromPoint(mouseX, mouseY, canvasWidth, g, ribbon);
                string txt = PieceTable.GetText();
                if (txt != null && txt.Length > 0 && charIdx >= 0 && charIdx <= txt.Length)
                {
                    int wStart = Math.Min(charIdx, txt.Length > 0 ? txt.Length - 1 : 0);
                    while (wStart > 0 && !char.IsWhiteSpace(txt[wStart - 1])) wStart--;
                    int wEnd = Math.Min(charIdx, txt.Length);
                    while (wEnd < txt.Length && !char.IsWhiteSpace(txt[wEnd])) wEnd++;

                    if (wEnd > wStart)
                    {
                        SelectionStart = wStart;
                        SelectionEnd = wEnd;
                        CursorPosition = wEnd;
                        _hasDraggedSelection = true;
                        Form parentForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
                        Point screenPt = parentForm != null ? parentForm.PointToScreen(new Point(mouseX, mouseY)) : new Point(mouseX, mouseY);
                        WriterFloatingMiniToolbarForm.ShowMiniToolbar(screenPt.X, screenPt.Y, this, ribbon, parentForm);
                        return;
                    }
                }
                CursorPosition = charIdx;
                SelectionStart = charIdx;
                SelectionEnd = charIdx;
                _hasDraggedSelection = false;
                WriterFloatingMiniToolbarForm.CloseCurrent();
            }
        }

        public void HandleMouseUp(int mouseX = 0, int mouseY = 0, int canvasWidth = 0, Graphics g = null, RibbonBar ribbon = null)
        {
            if (ActiveRulerDrag != RulerDragHandle.None)
            {
                int pStart = ActiveRulerDragParaStart;
                if (ActiveRulerDrag == RulerDragHandle.TabStop)
                {
                    int rulerY = 156;
                    // If dragged vertically outside the ruler (above or below), delete the tab stop (MS Word standard behavior)
                    if (mouseY < rulerY - 15 || mouseY > rulerY + 40)
                    {
                        var tList = GetTabStops(pStart);
                        if (DraggedTabStopIndex >= 0 && DraggedTabStopIndex < tList.Count)
                        {
                            tList.RemoveAt(DraggedTabStopIndex);
                        }
                    }
                }
                ActiveRulerDrag = RulerDragHandle.None;
                DraggedTabStopIndex = -1;
                RulerGuideLineX = -1;
                SaveUndoState();
            }

            if (ActiveShape != null)
            {
                if (ActiveShape.WidthDelta != 0 || ActiveShape.HeightDelta != 0)
                {
                    ActiveShape.BaseWidth = Math.Max(20, ActiveShape.BaseWidth + ActiveShape.WidthDelta);
                    ActiveShape.BaseHeight = Math.Max(15, ActiveShape.BaseHeight + ActiveShape.HeightDelta);
                    ActiveShape.WidthDelta = 0;
                    ActiveShape.HeightDelta = 0;
                }
                SyncShapeTagInDocument(ActiveShape);
            }

            IsDraggingHeaderBanner = false;
            IsResizingHeaderBanner = false;
            IsDraggingChart = false;
            DragChartHandleIndex = -1;
            IsDraggingShape = false;
            DragShapeHandleIndex = -1;



            if (IsDraggingMove && DragTableIndex >= 0)
            {
                IsDraggingMove = false;
                DragTableIndex = -1;
            }
            IsSelecting = false;
            IsDraggingMove = false;
            IsDraggingResize = false;
            IsDraggingColGridline = false;
            IsDraggingRowGridline = false;
            DragTableIndex = -1;
            DragPreviewGapIndex = -1;
            DragPreviewX = -1;
            DragGridlineTableIndex = -1;
            DragGridlineColIndex = -1;
            DragGridlineRowIndex = -1;

            if (!_hasDraggedSelection || SelectionStart == SelectionEnd)
            {
                // Single click on document -> clear selection, move cursor to clicked position, close mini toolbar!
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
                _hasDraggedSelection = false;
                WriterFloatingMiniToolbarForm.CloseCurrent();
            }
            else if (SelectionStart != SelectionEnd && Math.Abs(SelectionEnd - SelectionStart) > 0 && _hasDraggedSelection)
            {
                // Genuine drag selection -> show mini toolbar!
                Form parentForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
                Point screenPt = parentForm != null ? parentForm.PointToScreen(new Point(mouseX, mouseY)) : new Point(mouseX, mouseY);
                WriterFloatingMiniToolbarForm.ShowMiniToolbar(screenPt.X, screenPt.Y, this, ribbon, parentForm);
            }

            if (IsFormatPainterActive)
            {
                PasteFormatting();
            }
        }

        private void InsertTableRow(int tableIdx, int insertAtRow)
        {
            if (!TableCharRanges.ContainsKey(tableIdx)) return;
            int[] tRange = TableCharRanges[tableIdx];
            int tblStart = tRange[0];
            int tblLen   = tRange[1];

            string fullText = PieceTable.GetText();
            if (tblStart < 0 || tblStart + tblLen > fullText.Length) return;

            string tableText = fullText.Substring(tblStart, tblLen);
            string[] lines = tableText.Split('\n');
            List<string> newLines = new List<string>();

            int numCols = 1;
            if (TableColWidths.ContainsKey(tableIdx)) numCols = TableColWidths[tableIdx].Count;

            StringBuilder sbRow = new StringBuilder("|");
            for (int c = 0; c < numCols; c++) sbRow.Append("   |");
            string newRowStr = sbRow.ToString();

            int dataRowCounter = 0;
            bool inserted = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (!string.IsNullOrWhiteSpace(l) && l.Trim().StartsWith("|") && !l.Contains("---") && !l.Contains("==="))
                {
                    if (dataRowCounter == insertAtRow && !inserted)
                    {
                        newLines.Add(newRowStr);
                        inserted = true;
                    }
                    dataRowCounter++;
                }
                newLines.Add(l);
            }
            if (!inserted)
            {
                newLines.Add(newRowStr);
            }

            string newTableText = string.Join("\n", newLines.ToArray());
            string newDocText = fullText.Substring(0, tblStart) + newTableText + fullText.Substring(tblStart + tblLen);

            TableRowHeights.Remove(tableIdx);
            PieceTable.SetInitialText(newDocText);
        }

        private void InsertTableColumn(int tableIdx, int insertAtCol)
        {
            if (!TableCharRanges.ContainsKey(tableIdx)) return;
            int[] tRange = TableCharRanges[tableIdx];
            int tblStart = tRange[0];
            int tblLen   = tRange[1];

            string fullText = PieceTable.GetText();
            if (tblStart < 0 || tblStart + tblLen > fullText.Length) return;

            string tableText = fullText.Substring(tblStart, tblLen);
            string[] lines = tableText.Split('\n');
            List<string> newLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (!string.IsNullOrWhiteSpace(l) && l.Trim().StartsWith("|"))
                {
                    if (l.Contains("---") || l.Contains("==="))
                    {
                        string[] parts = l.Split('|');
                        List<string> newParts = new List<string>(parts);
                        int insIdx = Math.Min(newParts.Count - 1, Math.Max(1, insertAtCol + 1));
                        newParts.Insert(insIdx, "---");
                        newLines.Add(string.Join("|", newParts.ToArray()));
                    }
                    else
                    {
                        string[] cells = l.Split('|');
                        List<string> newCells = new List<string>(cells);
                        int insIdx = Math.Min(newCells.Count - 1, Math.Max(1, insertAtCol + 1));
                        newCells.Insert(insIdx, "   ");
                        newLines.Add(string.Join("|", newCells.ToArray()));
                    }
                }
                else
                {
                    newLines.Add(l);
                }
            }

            string newTableText = string.Join("\n", newLines.ToArray());
            string newDocText = fullText.Substring(0, tblStart) + newTableText + fullText.Substring(tblStart + tblLen);

            TableColWidths.Remove(tableIdx);
            PieceTable.SetInitialText(newDocText);
        }

        public void DeleteTableRow(int tableIdx, int rowIdx)
        {
            if (!TableCharRanges.ContainsKey(tableIdx)) return;
            int[] tRange = TableCharRanges[tableIdx];
            int tblStart = tRange[0];
            int tblLen   = tRange[1];

            string fullText = PieceTable.GetText();
            if (tblStart < 0 || tblStart + tblLen > fullText.Length) return;

            string tableText = fullText.Substring(tblStart, tblLen);
            string[] lines = tableText.Split('\n');
            List<string> newLines = new List<string>();

            int dataRowCounter = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (!string.IsNullOrWhiteSpace(l) && l.Trim().StartsWith("|") && !l.Contains("---") && !l.Contains("==="))
                {
                    if (dataRowCounter == rowIdx)
                    {
                        dataRowCounter++;
                        continue; // Bỏ qua để xóa hàng này
                    }
                    dataRowCounter++;
                }
                newLines.Add(l);
            }

            string newTableText = string.Join("\n", newLines.ToArray());
            string newDocText = fullText.Substring(0, tblStart) + newTableText + fullText.Substring(tblStart + tblLen);

            TableRowHeights.Remove(tableIdx);
            SaveUndoState();
            PieceTable.SetInitialText(newDocText);
        }

        public void DeleteTableColumn(int tableIdx, int colIdx)
        {
            if (!TableCharRanges.ContainsKey(tableIdx)) return;
            int[] tRange = TableCharRanges[tableIdx];
            int tblStart = tRange[0];
            int tblLen   = tRange[1];

            string fullText = PieceTable.GetText();
            if (tblStart < 0 || tblStart + tblLen > fullText.Length) return;

            string tableText = fullText.Substring(tblStart, tblLen);
            string[] lines = tableText.Split('\n');
            List<string> newLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (!string.IsNullOrWhiteSpace(l) && l.Trim().StartsWith("|"))
                {
                    string[] cells = l.Split('|');
                    List<string> keepCells = new List<string>();
                    for (int c = 1; c < cells.Length - 1; c++)
                    {
                        if (c - 1 != colIdx) keepCells.Add(cells[c]);
                    }
                    if (keepCells.Count > 0)
                    {
                        newLines.Add("| " + string.Join(" | ", keepCells.ToArray()) + " |");
                    }
                }
                else
                {
                    newLines.Add(l);
                }
            }

            string newTableText = string.Join("\n", newLines.ToArray());
            string newDocText = fullText.Substring(0, tblStart) + newTableText + fullText.Substring(tblStart + tblLen);

            if (TableColWidths.ContainsKey(tableIdx) && colIdx >= 0 && colIdx < TableColWidths[tableIdx].Count)
            {
                TableColWidths[tableIdx].RemoveAt(colIdx);
            }
            SaveUndoState();
            PieceTable.SetInitialText(newDocText);
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
                DocumentShapes.Remove(shape);
                if (ActiveShape == shape) ActiveShape = null;
                if (parentControl != null) parentControl.Invalidate();
            });

            cms.Show(parentControl, new Point(x, y));
        }

        public void HandleRightClick(int mouseX, int mouseY, Control parentControl, RibbonBar ribbon)
        {
            // Kiểm tra nhấp chuột phải vào SmartArt / Shape
            foreach (var sh in DocumentShapes)
            {
                if (!sh.LastBounds.IsEmpty && sh.LastBounds.Contains(mouseX, mouseY))
                {
                    ActiveShape = sh;
                    sh.IsSelected = true;
                    if (sh.ShapeType == "SmartArt" || !string.IsNullOrEmpty(sh.SmartArtType))
                    {
                        ShowSmartArtContextMenu(mouseX, mouseY, sh, parentControl);
                        return;
                    }
                }
            }

            // Kiểm tra nhấp chuột phải vào bảng
            for (int t = 0; t < MoveHandleRects.Count; t++)
            {
                int tX = MoveHandleRects[t].Left + 25;
                int tY = MoveHandleRects[t].Top + 25;
                int tW = (t < ResizeHandleRects.Count) ? (ResizeHandleRects[t].Left + 10 - tX) : 200;
                int tH = (t < ResizeHandleRects.Count) ? (ResizeHandleRects[t].Top + 10 - tY) : 100;
                Rectangle tBounds = new Rectangle(tX, tY, tW, tH);

                if (tBounds.Contains(mouseX, mouseY))
                {
                    int tblIdx = t;
                    int curWrap = TableWrapModes.ContainsKey(tblIdx) ? TableWrapModes[tblIdx] : 0;

                    ContextMenuStrip cms = new ContextMenuStrip();
                    cms.Items.Add("➕ Chèn hàng phía dưới", null, (s, e) => { InsertTableRow(tblIdx, 999); parentControl.Invalidate(); });
                    cms.Items.Add("➕ Chèn hàng phía trên", null, (s, e) => { InsertTableRow(tblIdx, 0); parentControl.Invalidate(); });
                    cms.Items.Add(new ToolStripSeparator());
                    cms.Items.Add("➕ Chèn cột bên phải", null, (s, e) => { InsertTableColumn(tblIdx, 999); parentControl.Invalidate(); });
                    cms.Items.Add("➕ Chèn cột bên trái", null, (s, e) => { InsertTableColumn(tblIdx, 0); parentControl.Invalidate(); });
                    cms.Items.Add(new ToolStripSeparator());
                    cms.Items.Add("🗑️ Xóa hàng này", null, (s, e) => { DeleteTableRow(tblIdx, 0); parentControl.Invalidate(); });
                    cms.Items.Add("🗑️ Xóa cột này", null, (s, e) => { DeleteTableColumn(tblIdx, 0); parentControl.Invalidate(); });
                    cms.Items.Add(new ToolStripSeparator());

                    ToolStripMenuItem wrapMenu = new ToolStripMenuItem("🗖 Ngắt dòng văn bản (Wrap Text)");
                    var itm0 = wrapMenu.DropDownItems.Add("📄 Cùng dòng với văn bản (In-Line)", null, (s, e) => { TableWrapModes[tblIdx] = 0; TableFloatingBounds.Remove(tblIdx); parentControl.Invalidate(); });
                    var itm1 = wrapMenu.DropDownItems.Add("🔲 Vuông (Square)", null, (s, e) => { TableWrapModes[tblIdx] = 1; parentControl.Invalidate(); });
                    var itm2 = wrapMenu.DropDownItems.Add("🗖 Chặt (Tight)", null, (s, e) => { TableWrapModes[tblIdx] = 2; parentControl.Invalidate(); });
                    var itm3 = wrapMenu.DropDownItems.Add("🔲 Xuyên qua (Through)", null, (s, e) => { TableWrapModes[tblIdx] = 3; parentControl.Invalidate(); });
                    var itm4 = wrapMenu.DropDownItems.Add("↕️ Trên và Dưới (Top and Bottom)", null, (s, e) => { TableWrapModes[tblIdx] = 4; parentControl.Invalidate(); });
                    var itm5 = wrapMenu.DropDownItems.Add("🔽 Dưới văn bản (Behind Text)", null, (s, e) => { TableWrapModes[tblIdx] = 5; parentControl.Invalidate(); });
                    var itm6 = wrapMenu.DropDownItems.Add("🔼 Trước văn bản (In Front of Text)", null, (s, e) => { TableWrapModes[tblIdx] = 6; parentControl.Invalidate(); });

                    if (curWrap == 0) ((ToolStripMenuItem)itm0).Checked = true;
                    else if (curWrap == 1) ((ToolStripMenuItem)itm1).Checked = true;
                    else if (curWrap == 2) ((ToolStripMenuItem)itm2).Checked = true;
                    else if (curWrap == 3) ((ToolStripMenuItem)itm3).Checked = true;
                    else if (curWrap == 4) ((ToolStripMenuItem)itm4).Checked = true;
                    else if (curWrap == 5) ((ToolStripMenuItem)itm5).Checked = true;
                    else if (curWrap == 6) ((ToolStripMenuItem)itm6).Checked = true;

                    cms.Items.Add(wrapMenu);

                    cms.Show(parentControl, new Point(mouseX, mouseY));
                    return;
                }
            }

            Form parentForm = parentControl as Form;
            WriterContextMenuHelper.Show(mouseX, mouseY, this, ribbon, parentForm);
        }

        public void CopySelection()
        {
            string txt = GetSelectedText();
            if (!string.IsNullOrEmpty(txt))
            {
                Clipboard.SetText(txt);
            }
        }

        public void CutSelection()
        {
            string txt = GetSelectedText();
            if (!string.IsNullOrEmpty(txt))
            {
                Clipboard.SetText(txt);
                DeleteSelection();
            }
        }

        public void PasteText()
        {
            if (Clipboard.ContainsText())
            {
                string txt = Clipboard.GetText();
                if (SelectionStart != SelectionEnd)
                {
                    DeleteSelection();
                }
                SaveUndoState();
                PieceTable.Insert(CursorPosition, txt);
                AdjustFormattedRunsOnInsert(CursorPosition, txt.Length);
                CursorPosition += txt.Length;
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
            }
        }

        public void PastePlainText()
        {
            PasteText();
        }

        public void ApplyHeadingStyle(int styleIdx)
        {
            SaveUndoState();
            string fName = "Times New Roman";
            float fSize = 12f;
            bool bold = false;
            Color col = Color.Black;

            switch (styleIdx)
            {
                case 0: // Normal
                    fName = "Times New Roman"; fSize = 12f; bold = false; col = Color.Black;
                    break;
                case 1: // Heading 1
                    fName = "Segoe UI Semibold"; fSize = 18f; bold = true; col = Color.FromArgb(43, 87, 154);
                    break;
                case 2: // Heading 2
                    fName = "Segoe UI Semibold"; fSize = 14f; bold = true; col = Color.FromArgb(43, 87, 154);
                    break;
                case 3: // Title
                    fName = "Segoe UI"; fSize = 24f; bold = true; col = Color.FromArgb(31, 41, 55);
                    break;
                case 4: // Subtitle
                    fName = "Segoe UI"; fSize = 13f; bold = false; col = Color.FromArgb(107, 114, 128);
                    break;
            }

            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, fName, fSize, bold, null, null, null, col, null);
            }
            else
            {
                TypingFont = fName;
                TypingFontSize = fSize;
                TypingBold = bold;
                TypingColor = col;
            }
        }

        public void IncreaseIndent()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;
            int lineStart = (curPos > 0 && fullText.Length >= curPos) ? fullText.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
            if (lineStart < 0) lineStart = 0;
            PieceTable.Insert(lineStart, "\t");
            AdjustFormattedRunsOnInsert(lineStart, 1);
            CursorPosition += 1;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void SetLineSpacing(float factor)
        {
            SaveUndoState();
            var targetPStarts = GetSelectedOrCurrentParagraphStarts();
            foreach (int pStart in targetPStarts)
            {
                ParagraphLineSpacings[pStart] = factor;
            }
        }

        public void InsertHyperlink(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            SaveUndoState();
            if (SelectionStart != SelectionEnd)
            {
                int min = Math.Min(SelectionStart, SelectionEnd);
                int len = Math.Abs(SelectionEnd - SelectionStart);
                ApplyFormatToRange(min, len, null, -1, null, null, true, null, Color.FromArgb(16, 110, 190), null);
            }
            else
            {
                PieceTable.Insert(CursorPosition, url);
                ApplyFormatToRange(CursorPosition, url.Length, null, -1, null, null, true, null, Color.FromArgb(16, 110, 190), null);
                AdjustFormattedRunsOnInsert(CursorPosition, url.Length);
                CursorPosition += url.Length;
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
            }
        }

        // Move table markdown text to a different paragraph gap position
        private void SnapTableToGap(int tableIdx, int gapIdx)
        {
            SaveUndoState();
            if (!TableCharRanges.ContainsKey(tableIdx)) return;
            if (gapIdx < 0 || gapIdx >= DocParagraphGaps.Count) return;

            int[] tRange = TableCharRanges[tableIdx];
            int tblStart = tRange[0];
            int tblLen   = tRange[1];

            ParagraphGap targetGap = DocParagraphGaps[gapIdx];
            int targetCharIndex = targetGap.CharIndex;
            int entersNeeded = targetGap.VirtualEntersNeeded;

            string fullText = PieceTable.GetText();
            if (tblStart < 0 || tblStart + tblLen > fullText.Length) return;

            string tableText = fullText.Substring(tblStart, tblLen);
            string before    = fullText.Substring(0, tblStart);
            string after     = fullText.Substring(tblStart + tblLen);
            string withoutTable = before + after;

            int insertAt = targetCharIndex;
            if (entersNeeded > 0)
            {
                // Appending newlines to place table into empty space
                string extraEnters = new string('\n', entersNeeded);
                if (!withoutTable.EndsWith("\n") && withoutTable.Length > 0)
                {
                    withoutTable += "\n";
                }
                withoutTable += extraEnters;
                insertAt = withoutTable.Length;
            }
            else
            {
                // Adjust target index now that table is removed
                if (insertAt > tblStart + tblLen)       insertAt -= tblLen;
                else if (insertAt > tblStart)            insertAt  = tblStart;
                insertAt = Math.Max(0, Math.Min(insertAt, withoutTable.Length));
            }

            string newText = withoutTable.Substring(0, insertAt) + tableText + withoutTable.Substring(insertAt);

            // Rebuild PieceTable with new text order
            int shift = insertAt - tblStart;   // how much char indices shifted
            PieceTable.SetInitialText(newText);

            // Shift FormattedRuns so formatting stays attached to the right characters
            // Simple approach: shift runs that fell inside the old table range to the new position
            for (int ri = 0; ri < FormattedRuns.Count; ri++)
            {
                int s = FormattedRuns[ri].StartIndex;
                if (s >= tblStart && s < tblStart + tblLen)
                {
                    FormattedRuns[ri].StartIndex = s + shift;
                }
                else if (s >= tblStart + tblLen && insertAt <= tblStart)
                {
                    FormattedRuns[ri].StartIndex = s - tblLen;
                }
                else if (s < tblStart && insertAt >= tblStart + tblLen)
                {
                    FormattedRuns[ri].StartIndex = s;
                }
            }

            // Reset cursor to after the moved table
            CursorPosition = insertAt + tblLen;
            SelectionStart = CursorPosition;
            SelectionEnd   = CursorPosition;
        }

        public bool TryDeleteElementAtCursor()
        {
            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;
            if (curPos < 0 || curPos > fullText.Length) return false;

            // 1. Try deleting table at cursor
            foreach (var kvp in TableCharRanges)
            {
                int tIdx = kvp.Key;
                int tblStart = kvp.Value[0];
                int tblLen   = kvp.Value[1];

                if (curPos >= Math.Max(0, tblStart - 1) && curPos <= tblStart + tblLen + 1)
                {
                    SaveUndoState();
                    string before = fullText.Substring(0, tblStart);
                    string after  = fullText.Substring(tblStart + tblLen);

                    while (before.EndsWith("\n\n")) before = before.Substring(0, before.Length - 1);
                    while (after.StartsWith("\n\n")) after = after.Substring(1);

                    string newText = before + after;
                    TableCharRanges.Remove(tIdx);
                    TableColWidths.Remove(tIdx);
                    TableRowHeights.Remove(tIdx);
                    TablePageX.Remove(tIdx);
                    TableCustomSizes.Remove(tIdx);

                    PieceTable.SetInitialText(newText);
                    CursorPosition = Math.Min(tblStart, PieceTable.Length);
                    SelectionStart = CursorPosition;
                    SelectionEnd   = CursorPosition;
                    return true;
                }
            }

            // 2. Try deleting special tags (Image, Shape, Chart, NoteBox, PageBreak) at cursor line
            int lineStart = (curPos > 0 && fullText.Length >= curPos) ? fullText.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
            int lineEnd = fullText.IndexOf('\n', curPos);
            if (lineEnd < 0) lineEnd = fullText.Length;

            int lineLen = lineEnd - lineStart;
            if (lineLen > 0)
            {
                string lineStr = fullText.Substring(lineStart, lineLen).Trim();
                if (lineStr.StartsWith("![IMAGE]") || lineStr.StartsWith("![SHAPE:") || lineStr.StartsWith("![CHART:") || lineStr.StartsWith("![NOTEBOX:") || lineStr.Contains("PAGE BREAK") || lineStr.Contains("TRANG TRẮNG MỚI") || lineStr.Contains("HÌNH ÁNH DỰ ÁN") || lineStr.Contains("HÌNH KHỐI VECTOR") || lineStr.Contains("BIỂU ĐỒ DOANH THU") || lineStr.Contains("KHUNG GHI CHÚ"))
                {
                    SaveUndoState();
                    if (lineStr.StartsWith("![SHAPE:"))
                    {
                        int idIdx = lineStr.IndexOf("ID=");
                        if (idIdx >= 0)
                        {
                            string shId = lineStr.Substring(idIdx + 3).Split(':', ']')[0];
                            DocumentShapes.RemoveAll(s => s.Id == shId);
                            if (ActiveShape != null && ActiveShape.Id == shId) ActiveShape = null;
                        }
                    }
                    else if (lineStr.StartsWith("![CHART:"))
                    {
                        int idIdx = lineStr.IndexOf("ID=");
                        if (idIdx >= 0)
                        {
                            string chId = lineStr.Substring(idIdx + 3).Split(':', ']')[0];
                            DocumentCharts.RemoveAll(c => c.Id == chId);
                            if (ActiveChart != null && ActiveChart.Id == chId) ActiveChart = null;
                        }
                    }

                    string before = fullText.Substring(0, lineStart);
                    string after = (lineEnd < fullText.Length) ? fullText.Substring(lineEnd + 1) : "";
                    PieceTable.SetInitialText(before + after);
                    AdjustFormattedRunsOnDelete(lineStart, (lineEnd + 1 <= fullText.Length ? lineEnd + 1 - lineStart : lineEnd - lineStart));
                    CursorPosition = Math.Min(lineStart, PieceTable.Length);
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    return true;
                }
            }

            return false;
        }

        public int GetCharIndexFromPoint(int mouseX, int mouseY, int canvasWidth, Graphics g, RibbonBar ribbon)
        {
            string fullText = PieceTable.GetText();
            if (RenderedLines.Count == 0 || string.IsNullOrEmpty(fullText)) return 0;

            // 1. Check direct hit on rendered lines or between-line vertical gaps
            for (int l = 0; l < RenderedLines.Count; l++)
            {
                var rLine = RenderedLines[l];
                bool isLast = (l == RenderedLines.Count - 1);
                int lineBottom = isLast ? (rLine.ScreenY + rLine.ScreenH) : RenderedLines[l + 1].ScreenY;

                if (mouseY >= rLine.ScreenY && mouseY < lineBottom)
                {
                    if (rLine.StartCharIndex >= fullText.Length) return fullText.Length;
                    int safeLen = Math.Min(rLine.Length, fullText.Length - rLine.StartCharIndex);
                    string lineText = fullText.Substring(rLine.StartCharIndex, safeLen);

                    float relX = mouseX - rLine.ScreenX;
                    if (relX <= 0) return rLine.StartCharIndex;

                    int low = 0;
                    int high = lineText.Length;
                    while (low < high)
                    {
                        int mid = (low + high) / 2;
                        float w = MeasureSubstringWidth(g, lineText, rLine.StartCharIndex, 0, mid);
                        if (w < relX) low = mid + 1;
                        else high = mid;
                    }
                    if (low > 0)
                    {
                        float wLow = MeasureSubstringWidth(g, lineText, rLine.StartCharIndex, 0, low);
                        float wPrev = MeasureSubstringWidth(g, lineText, rLine.StartCharIndex, 0, low - 1);
                        if (Math.Abs(relX - wPrev) < Math.Abs(relX - wLow)) return rLine.StartCharIndex + low - 1;
                    }
                    return rLine.StartCharIndex + low;
                }
            }

            // 2. Clicked above first rendered line
            if (RenderedLines.Count > 0 && mouseY < RenderedLines[0].ScreenY)
            {
                return RenderedLines[0].StartCharIndex;
            }

            // 3. Clicked below last rendered line
            if (RenderedLines.Count > 0 && mouseY >= RenderedLines[RenderedLines.Count - 1].ScreenY + RenderedLines[RenderedLines.Count - 1].ScreenH)
            {
                var last = RenderedLines[RenderedLines.Count - 1];
                return Math.Min(fullText.Length, last.StartCharIndex + last.Length);
            }

            return 0;
        }

        private static readonly StringFormat SafeStringFormat = CreateSafeStringFormat();
        private static StringFormat CreateSafeStringFormat()
        {
            StringFormat sf = (StringFormat)StringFormat.GenericTypographic.Clone();
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            return sf;
        }

        private SizeF MeasureStringWithTabs(Graphics g, string str, Font font, float zoomLevel)
        {
            if (string.IsNullOrEmpty(str)) return SizeF.Empty;
            float totalW = 0;
            float maxH = font.Height;

            string[] parts = str.Split('\t');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    SizeF sz = g.MeasureString(parts[i], font, Point.Empty, SafeStringFormat);
                    totalW += sz.Width;
                    if (sz.Height > maxH) maxH = sz.Height;
                }
                if (i < parts.Length - 1)
                {
                    totalW += 36.0f * zoomLevel; // Standard Tab Stop Width (36px * Zoom)
                }
            }
            return new SizeF(totalW, maxH);
        }

        private void DrawStringWithTabs(Graphics g, string str, Font font, Brush brush, float x, float y, float zoomLevel)
        {
            if (string.IsNullOrEmpty(str)) return;

            string[] parts = str.Split('\t');
            float curX = x;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    g.DrawString(parts[i], font, brush, curX, y, SafeStringFormat);

                    if (ShowFormattingMarks && parts[i].Contains(" "))
                    {
                        using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(120, 140, 165)))
                        {
                            float wordX = curX;
                            for (int c = 0; c < parts[i].Length; c++)
                            {
                                if (parts[i][c] == ' ')
                                {
                                    float dotX = wordX + 2.0f * zoomLevel;
                                    float dotY = y + font.Height * 0.45f;
                                    float dotSize = Math.Max(2.0f, 2.0f * zoomLevel);
                                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                                }
                                string charStr = parts[i].Substring(c, 1);
                                SizeF charSz = g.MeasureString(charStr, font, Point.Empty, SafeStringFormat);
                                wordX += charSz.Width;
                            }
                        }
                    }

                    SizeF sz = g.MeasureString(parts[i], font, Point.Empty, SafeStringFormat);
                    curX += sz.Width;
                }
                if (i < parts.Length - 1)
                {
                    if (ShowFormattingMarks)
                    {
                        using (Pen arrPen = new Pen(Color.FromArgb(120, 140, 165), 1.2f * zoomLevel) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        {
                            float tabArrowY = y + font.Height * 0.48f;
                            float tabArrowStartX = curX + 4.0f * zoomLevel;
                            float tabArrowEndX = curX + 22.0f * zoomLevel;
                            g.DrawLine(arrPen, tabArrowStartX, tabArrowY, tabArrowEndX, tabArrowY);
                            g.DrawLine(arrPen, tabArrowEndX - 4.0f * zoomLevel, tabArrowY - 3.0f * zoomLevel, tabArrowEndX, tabArrowY);
                            g.DrawLine(arrPen, tabArrowEndX - 4.0f * zoomLevel, tabArrowY + 3.0f * zoomLevel, tabArrowEndX, tabArrowY);
                        }
                    }
                    curX += 36.0f * zoomLevel; // Advance by 36px Tab Stop
                }
            }
        }

        public void IndentRight()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            if (selMin == selMax) { selMin = CursorPosition; selMax = CursorPosition; }

            int pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
            if (pStart < 0) pStart = 0;
            int pEnd = fullText.IndexOf('\n', selMax);
            if (pEnd < 0) pEnd = fullText.Length;

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] lines = sub.Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append("\t" + lines[i]);
                if (i < lines.Length - 1) sb.Append("\n");
            }
            string newSub = sb.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);
            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        public void IndentLeft()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int selMin = Math.Min(SelectionStart, SelectionEnd);
            int selMax = Math.Max(SelectionStart, SelectionEnd);
            if (selMin == selMax) { selMin = CursorPosition; selMax = CursorPosition; }

            int pStart = (selMin > 0 && fullText.Length >= selMin) ? fullText.LastIndexOf('\n', Math.Max(0, selMin - 1)) + 1 : 0;
            if (pStart < 0) pStart = 0;
            int pEnd = fullText.IndexOf('\n', selMax);
            if (pEnd < 0) pEnd = fullText.Length;

            string sub = fullText.Substring(pStart, pEnd - pStart);
            string[] lines = sub.Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("\t")) line = line.Substring(1);
                else if (line.StartsWith("    ")) line = line.Substring(4);
                sb.Append(line);
                if (i < lines.Length - 1) sb.Append("\n");
            }
            string newSub = sb.ToString();
            PieceTable.Remove(pStart, sub.Length);
            AdjustFormattedRunsOnDelete(pStart, sub.Length);
            PieceTable.Insert(pStart, newSub);
            AdjustFormattedRunsOnInsert(pStart, newSub.Length);
            SelectionStart = pStart;
            SelectionEnd = pStart + newSub.Length;
            CursorPosition = SelectionEnd;
        }

        public void IndentOrInsertTab()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;

            // Kiểm tra xem con trỏ có đang nằm trong một bảng hay không
            foreach (var kvp in TableCharRanges)
            {
                int tblIdx = kvp.Key;
                int tblStart = kvp.Value[0];
                int tblLen = kvp.Value[1];
                if (curPos >= tblStart && curPos <= tblStart + tblLen)
                {
                    int tblEnd = tblStart + tblLen;
                    int nextPipe = fullText.IndexOf('|', curPos);
                    // Nếu còn ô tiếp theo trong bảng
                    if (nextPipe >= 0 && nextPipe < tblEnd - 2)
                    {
                        CursorPosition = nextPipe + 1;
                        if (CursorPosition < fullText.Length && fullText[CursorPosition] == ' ') CursorPosition++;
                        SelectionStart = CursorPosition;
                        SelectionEnd = CursorPosition;
                        return;
                    }
                    else
                    {
                        // Đã ở ô cuối cùng của bảng -> Tự động thêm một hàng mới vào cuối bảng!
                        int numRows = TableRowHeights.ContainsKey(tblIdx) ? TableRowHeights[tblIdx].Count : 1;
                        InsertTableRow(tblIdx, numRows);
                        string updatedText = PieceTable.GetText();
                        if (TableCharRanges.ContainsKey(tblIdx))
                        {
                            int newTblStart = TableCharRanges[tblIdx][0];
                            int newTblLen = TableCharRanges[tblIdx][1];
                            int lastLineStart = updatedText.LastIndexOf('\n', Math.Min(updatedText.Length - 1, newTblStart + newTblLen - 1));
                            if (lastLineStart >= 0)
                            {
                                int firstPipe = updatedText.IndexOf('|', lastLineStart);
                                if (firstPipe >= 0)
                                {
                                    CursorPosition = Math.Min(updatedText.Length, firstPipe + 2);
                                    SelectionStart = CursorPosition;
                                    SelectionEnd = CursorPosition;
                                    return;
                                }
                            }
                        }
                        return;
                    }
                }
            }

            DeleteSelection();
            curPos = CursorPosition;
            PieceTable.Insert(curPos, "\t");
            FormattedRuns.Add(new FormattedRun(curPos, 1, TypingFont, TypingFontSize, TypingBold, TypingItalic, TypingUnderline, TypingStrike, TypingColor, TypingHighlight, TypingSubscript, TypingSuperscript));
            CursorPosition++;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void Outdent()
        {
            SaveUndoState();
            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;
            int lineStart = (curPos > 0) ? fullText.LastIndexOf('\n', curPos - 1) + 1 : 0;
            int checkLen = Math.Min(4, fullText.Length - lineStart);
            if (checkLen > 0)
            {
                string sub = fullText.Substring(lineStart, checkLen);
                int spacesToRemove = 0;
                foreach (char c in sub)
                {
                    if (c == ' ' || c == '\t') spacesToRemove++;
                    else break;
                }
                if (spacesToRemove > 0)
                {
                    PieceTable.Remove(lineStart, spacesToRemove);
                    CursorPosition = Math.Max(lineStart, CursorPosition - spacesToRemove);
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                }
            }
        }

        private bool TryHandleSmartListBackspace()
        {
            if (CursorPosition <= 0) return false;

            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;

            int lineStart = (curPos > 0 && fullText.Length >= curPos) ? fullText.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
            if (lineStart < 0) lineStart = 0;

            string lineUpToCursor = (curPos >= lineStart && curPos <= fullText.Length) ? fullText.Substring(lineStart, curPos - lineStart) : "";

            // 1. Backspace #1: If cursor is right after list prefix like "\t1.\t" or "1.\t" or "•\t" or "\t•\t"
            var matchPrefix = System.Text.RegularExpressions.Regex.Match(lineUpToCursor, @"^(\t*)(\u2022|\*|-|\d+\.)([\t\s]+)$");
            if (matchPrefix.Success)
            {
                SaveUndoState();
                string tabsBefore = matchPrefix.Groups[1].Value;
                int prefixLen = matchPrefix.Length;

                PieceTable.Remove(lineStart, prefixLen);
                AdjustFormattedRunsOnDelete(lineStart, prefixLen);

                if (tabsBefore.Length > 0)
                {
                    PieceTable.Insert(lineStart, tabsBefore);
                    AdjustFormattedRunsOnInsert(lineStart, tabsBefore.Length);
                    CursorPosition = lineStart + tabsBefore.Length;
                }
                else
                {
                    CursorPosition = lineStart;
                }

                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
                return true;
            }

            // 2. Backspace #2: If cursor is right after leading Tab "\t" or "    " at line start
            if (lineUpToCursor == "\t" || lineUpToCursor == "    ")
            {
                SaveUndoState();
                PieceTable.Remove(lineStart, lineUpToCursor.Length);
                AdjustFormattedRunsOnDelete(lineStart, lineUpToCursor.Length);
                CursorPosition = lineStart;
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
                return true;
            }

            return false;
        }

        public void InsertTextWithAutoFormat(string text)
        {
            SaveUndoState();
            DeleteSelection();

            string fullText = PieceTable.GetText();
            int curPos = CursorPosition;
            int lineStart = (curPos > 0 && fullText.Length >= curPos) ? fullText.LastIndexOf('\n', Math.Max(0, curPos - 1)) + 1 : 0;
            if (lineStart < 0) lineStart = 0;
            string currentLineUpToCursor = (curPos >= lineStart && curPos <= fullText.Length) ? fullText.Substring(lineStart, curPos - lineStart) : "";

            // 1. AutoFormat Bullet "- " or "* " when Space or Tab is typed
            if ((text == " " || text == "\t") && (currentLineUpToCursor == "-" || currentLineUpToCursor == "*"))
            {
                PieceTable.Remove(lineStart, currentLineUpToCursor.Length);
                AdjustFormattedRunsOnDelete(lineStart, currentLineUpToCursor.Length);
                string bulletStr = "\t•\t";
                PieceTable.Insert(lineStart, bulletStr);
                AdjustFormattedRunsOnInsert(lineStart, bulletStr.Length);
                CursorPosition = lineStart + bulletStr.Length;
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
                return;
            }

            // 2. AutoFormat Numbered List "1. " when Space or Tab is typed
            if ((text == " " || text == "\t") && System.Text.RegularExpressions.Regex.IsMatch(currentLineUpToCursor, @"^\t*\d+\.$"))
            {
                string indent = currentLineUpToCursor.StartsWith("\t") ? "" : "\t";
                string numStr = indent + currentLineUpToCursor + "\t";
                PieceTable.Remove(lineStart, currentLineUpToCursor.Length);
                AdjustFormattedRunsOnDelete(lineStart, currentLineUpToCursor.Length);
                PieceTable.Insert(lineStart, numStr);
                AdjustFormattedRunsOnInsert(lineStart, numStr.Length);
                CursorPosition = lineStart + numStr.Length;
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
                return;
            }

            // 3. Auto-Continue Bullets or Numbers on Enter Key (\n)
            if (text == "\n")
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(currentLineUpToCursor, @"^\t*(\u2022|\*|-|\d+\.)[\t\s]*$"))
                {
                    PieceTable.Remove(lineStart, currentLineUpToCursor.Length);
                    AdjustFormattedRunsOnDelete(lineStart, currentLineUpToCursor.Length);
                    CursorPosition = lineStart;
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    return;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(currentLineUpToCursor, @"^(\t*)\u2022[\t\s]"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(currentLineUpToCursor, @"^(\t*)");
                    string indent = match.Groups[1].Value;
                    string autoBullet = "\n" + indent + "•\t";
                    PieceTable.Insert(curPos, autoBullet);
                    AdjustFormattedRunsOnInsert(curPos, autoBullet.Length);
                    CursorPosition = curPos + autoBullet.Length;
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    return;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(currentLineUpToCursor, @"^(\t*)(\d+)\.[\t\s]"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(currentLineUpToCursor, @"^(\t*)(\d+)\.");
                    string indent = match.Groups[1].Value;
                    int currentNum = int.Parse(match.Groups[2].Value);
                    string autoNum = "\n" + indent + (currentNum + 1) + ".\t";
                    PieceTable.Insert(curPos, autoNum);
                    AdjustFormattedRunsOnInsert(curPos, autoNum.Length);
                    CursorPosition = curPos + autoNum.Length;
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    return;
                }
            }

            // 4. Auto-Capitalize initial letter of sentence (at start of line or after ". ", "? ", "! ")
            if (text.Length == 1 && char.IsLower(text[0]))
            {
                string trimmed = currentLineUpToCursor.TrimEnd('\t', ' ');
                if (string.IsNullOrEmpty(trimmed) ||
                    trimmed.EndsWith(".") ||
                    trimmed.EndsWith("?") ||
                    trimmed.EndsWith("!"))
                {
                    text = text.ToUpper();
                }
            }

            PieceTable.Insert(curPos, text);
            AdjustFormattedRunsOnInsert(curPos, text.Length);
            CursorPosition += text.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void HandleChar(char ch)
        {
            if (EditingReplyCommentId != -1)
            {
                if (ch == '\r' || ch == '\n')
                {
                    for (int i = 0; i < Comments.Count; i++)
                    {
                        if (Comments[i].Id == EditingReplyCommentId)
                        {
                            if (!string.IsNullOrEmpty(ReplyDraftText.Trim()))
                            {
                                Comments[i].Replies.Add(Environment.UserName + ": " + ReplyDraftText.Trim());
                            }
                            break;
                        }
                    }
                    EditingReplyCommentId = -1;
                    ReplyDraftText = "";
                    return;
                }
                else if (ch == '\b')
                {
                    if (ReplyDraftText.Length > 0)
                    {
                        ReplyDraftText = ReplyDraftText.Substring(0, ReplyDraftText.Length - 1);
                    }
                    return;
                }
                else if (ch >= 32 || ch == '\t')
                {
                    ReplyDraftText += ch;
                    return;
                }
            }

            if (ActiveShape != null || ActiveChart != null) return;

            if (ch == '\r') ch = '\n';
            if (ch == '\b')
            {
                if (SelectionStart != SelectionEnd)
                {
                    SaveUndoState();
                    DeleteSelection();
                }
                else if (TryDeleteElementAtCursor())
                {
                    // Table deleted cleanly
                }
                else if (TryHandleSmartListBackspace())
                {
                    // 3-stage List Backspace handled!
                }
                else if (CursorPosition > 0)
                {
                    SaveUndoState();
                    PieceTable.Remove(CursorPosition - 1, 1);
                    AdjustFormattedRunsOnDelete(CursorPosition - 1, 1);
                    CursorPosition--;
                }
            }
            else if (ch >= 32 || ch == '\n' || ch == '\t')
            {
                InsertTextWithAutoFormat(ch.ToString());
            }
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void InsertBlankPage()
        {
            SaveUndoState();
            DeleteSelection();
            string bp = "\n[--- PAGE BREAK ---]\n[--- PAGE BREAK ---]\n";
            PieceTable.Insert(CursorPosition, bp);
            CursorPosition += bp.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void RemoveCoverPage()
        {
            string text = PieceTable.GetText();
            int startIdx = text.IndexOf("[--- COVER PAGE START ---]");
            int endIdx = text.IndexOf("[--- COVER PAGE END ---]\n[--- PAGE BREAK ---]\n");

            if (startIdx >= 0 && endIdx > startIdx)
            {
                SaveUndoState();
                int removeLen = (endIdx + 46) - startIdx;
                PieceTable.Remove(startIdx, removeLen);
                AdjustFormattedRunsOnDelete(startIdx, removeLen);
                
                if (CursorPosition > startIdx)
                {
                    CursorPosition = Math.Max(0, CursorPosition - removeLen);
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                }
            }
        }

        public void InsertCoverPage(string templateName)
        {
            RemoveCoverPage();
            SaveUndoState();
            
            string cp = "[--- COVER PAGE START ---]\n" +
                        "![SHAPE:RECT:0,0,842,1200:#1a365d]\n" + // Background
                        "![SHAPE:RECT:0,0,842,200:#2b6cb0]\n" + // Top Bar
                        "\n\n\n\n\n\n[Document Title]\n\n[Subtitle]\n\n\n\n\n\n\n\n\n[Author]\n[Date]\n" +
                        "[--- COVER PAGE END ---]\n[--- PAGE BREAK ---]\n";

            PieceTable.Insert(0, cp);

            int titleIdx = cp.IndexOf("[Document Title]");
            int subIdx = cp.IndexOf("[Subtitle]");
            int authorIdx = cp.IndexOf("[Author]");
            int dateIdx = cp.IndexOf("[Date]");

            ApplyFormatToRange(titleIdx, 16, "Segoe UI", 48.0f, true, false, false, false, Color.White, Color.Transparent);
            ApplyFormatToRange(subIdx, 10, "Segoe UI", 24.0f, false, false, false, false, Color.LightGray, Color.Transparent);
            ApplyFormatToRange(authorIdx, 8, "Segoe UI", 16.0f, false, true, false, false, Color.White, Color.Transparent);
            ApplyFormatToRange(dateIdx, 6, "Segoe UI", 16.0f, false, false, false, false, Color.LightGray, Color.Transparent);

            CursorPosition += cp.Length;
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        public void HandleKeyDown(Keys key, RibbonBar ribbon = null)
        {
            if (ActiveShape != null || ActiveChart != null) return;

            bool isShift = (Control.ModifierKeys & Keys.Shift) != 0;
            bool isCtrl = (Control.ModifierKeys & Keys.Control) != 0;
            string text = PieceTable.GetText();

            if (key == Keys.Left)
            {
                if (isCtrl)
                {
                    // Di chuyển qua từng từ (Ctrl + Left)
                    int pos = Math.Max(0, Math.Min(CursorPosition, text.Length));
                    if (pos > 0)
                    {
                        pos--;
                        while (pos > 0 && char.IsWhiteSpace(text[pos])) pos--;
                        while (pos > 0 && !char.IsWhiteSpace(text[pos - 1])) pos--;
                    }
                    CursorPosition = pos;
                }
                else
                {
                    // Di chuyển từng ký tự (Left / Shift + Left)
                    if (!isShift && SelectionStart != SelectionEnd)
                    {
                        CursorPosition = Math.Min(SelectionStart, SelectionEnd);
                    }
                    else if (CursorPosition > 0)
                    {
                        CursorPosition--;
                    }
                }

                if (isShift)
                {
                    SelectionEnd = CursorPosition;
                }
                else
                {
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    F8SelectionStage = 0;
                }
                LastCursorScreenX = -1;
            }
            else if (key == Keys.Right)
            {
                if (isCtrl)
                {
                    // Di chuyển qua từng từ (Ctrl + Right)
                    int pos = Math.Max(0, Math.Min(CursorPosition, text.Length));
                    if (pos < text.Length)
                    {
                        while (pos < text.Length && !char.IsWhiteSpace(text[pos])) pos++;
                        while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
                    }
                    CursorPosition = pos;
                }
                else
                {
                    // Di chuyển từng ký tự (Right / Shift + Right)
                    if (!isShift && SelectionStart != SelectionEnd)
                    {
                        CursorPosition = Math.Max(SelectionStart, SelectionEnd);
                    }
                    else if (CursorPosition < PieceTable.Length)
                    {
                        CursorPosition++;
                    }
                }

                if (isShift)
                {
                    SelectionEnd = CursorPosition;
                }
                else
                {
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    F8SelectionStage = 0;
                }
                LastCursorScreenX = -1;
            }
            else if (key == Keys.Home)
            {
                if (isCtrl)
                {
                    // Về đầu tài liệu (Ctrl + Home)
                    CursorPosition = 0;
                }
                else
                {
                    // Về đầu dòng (Home)
                    int pos = CursorPosition - 1;
                    while (pos >= 0 && text.Length > pos && text[pos] != '\n') pos--;
                    CursorPosition = pos + 1;
                }

                if (isShift)
                {
                    SelectionEnd = CursorPosition;
                }
                else
                {
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    F8SelectionStage = 0;
                }
                LastCursorScreenX = -1;
            }
            else if (key == Keys.End)
            {
                if (isCtrl)
                {
                    // Về cuối tài liệu (Ctrl + End)
                    CursorPosition = PieceTable.Length;
                }
                else
                {
                    // Về cuối dòng (End)
                    int pos = CursorPosition;
                    while (pos < text.Length && text[pos] != '\n') pos++;
                    CursorPosition = pos;
                }

                if (isShift)
                {
                    SelectionEnd = CursorPosition;
                }
                else
                {
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;
                    F8SelectionStage = 0;
                }
                LastCursorScreenX = -1;
            }
            else if (key == Keys.Up || key == Keys.Down)
            {
                if (isCtrl)
                {
                    // Nhảy đầu đoạn trước/sau (Ctrl + Up / Ctrl + Down)
                    if (key == Keys.Up)
                    {
                        int pos = Math.Max(0, CursorPosition - 1);
                        int pStart = (pos > 0 && text.Length >= pos) ? text.LastIndexOf('\n', Math.Max(0, pos - 1)) + 1 : 0;
                        CursorPosition = Math.Max(0, pStart);
                    }
                    else
                    {
                        int pNext = text.IndexOf('\n', CursorPosition);
                        CursorPosition = (pNext >= 0) ? Math.Min(text.Length, pNext + 1) : text.Length;
                    }

                    if (isShift)
                    {
                        SelectionEnd = CursorPosition;
                    }
                    else
                    {
                        SelectionStart = CursorPosition;
                        SelectionEnd = CursorPosition;
                        F8SelectionStage = 0;
                    }
                    LastCursorScreenX = -1;
                }
                else
                {
                    // Điều hướng theo dòng (Up / Down)
                    int curLineIdx = -1;
                    for (int li = 0; li < RenderedLines.Count; li++)
                    {
                        var rl = RenderedLines[li];
                        if (CursorPosition >= rl.StartCharIndex && CursorPosition <= rl.StartCharIndex + rl.Length)
                        {
                            curLineIdx = li;
                            break;
                        }
                    }

                    if (curLineIdx >= 0)
                    {
                        if (LastCursorScreenX < 0)
                        {
                            var curLine = RenderedLines[curLineIdx];
                            int offsetInLine = CursorPosition - curLine.StartCharIndex;
                            float estimatedX = curLine.ScreenX;
                            if (curLine.Length > 0)
                            {
                                estimatedX += (float)offsetInLine / (float)curLine.Length * (float)Math.Max(1, 200);
                            }
                            LastCursorScreenX = (int)estimatedX;
                        }

                        int targetLineIdx = key == Keys.Up ? curLineIdx - 1 : curLineIdx + 1;
                        if (targetLineIdx >= 0 && targetLineIdx < RenderedLines.Count)
                        {
                            var targetLine = RenderedLines[targetLineIdx];
                            int targetLen = targetLine.Length;
                            int relX = LastCursorScreenX - targetLine.ScreenX;
                            if (relX < 0) relX = 0;
                            int approxOffset = 0;
                            if (targetLen > 0 && relX > 0)
                            {
                                approxOffset = Math.Min(targetLen, (int)((float)relX / 200.0f * targetLen));
                            }
                            CursorPosition = targetLine.StartCharIndex + approxOffset;
                        }
                        else if (key == Keys.Up && curLineIdx == 0)
                        {
                            CursorPosition = 0;
                        }
                        else if (key == Keys.Down && curLineIdx == RenderedLines.Count - 1)
                        {
                            CursorPosition = PieceTable.Length;
                        }

                        if (isShift)
                        {
                            SelectionEnd = CursorPosition;
                        }
                        else
                        {
                            SelectionStart = CursorPosition;
                            SelectionEnd = CursorPosition;
                            F8SelectionStage = 0;
                        }
                    }
                }
            }
            else if (key == Keys.F8)
            {
                ExtendSelectionF8();
            }
            else if (key == Keys.Escape)
            {
                F8SelectionStage = 0;
                SelectionStart = CursorPosition;
                SelectionEnd = CursorPosition;
            }
            else if (key == Keys.Delete)
            {
                if (SelectionStart != SelectionEnd)
                {
                    SaveUndoState();
                    DeleteSelection();
                }
                else if (TryDeleteElementAtCursor())
                {
                    // Table deleted cleanly
                }
                else if (CursorPosition < PieceTable.Length)
                {
                    SaveUndoState();
                    PieceTable.Remove(CursorPosition, 1);
                    AdjustFormattedRunsOnDelete(CursorPosition, 1);
                }
                LastCursorScreenX = -1;
            }
            if (ribbon != null) UpdateRibbonState(ribbon);
        }
    }
}

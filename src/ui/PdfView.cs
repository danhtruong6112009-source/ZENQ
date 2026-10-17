using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ZenQOffice.Core;
using ZenQOffice.Format;

namespace ZenQOffice.UI
{
    public class PdfSearchMatch
    {
        public int PageIndex;
        public RectangleF Bounds;
        public string Text;
    }

    public class PdfView
    {
        public PdfDocument Document { get; set; }
        public int ActivePageIndex { get; set; }
        public float ZoomLevel { get; set; }
        public float ScrollY { get; set; }
        public float TargetScrollY { get; set; }
        public float ScrollX { get; set; }
        private int _lastViewWidth = 1000;
        private int _lastViewHeight = 800;
        private int _lastTopOffset = 155;
        private int _lastStatusH = 26;

        public bool ShowThumbnailsSidebar { get; set; }
        public int SidebarWidth { get; set; }
        public float SidebarScrollY { get; set; }
        public float TargetSidebarScrollY { get; set; }

        // Main Viewport Scrollbar
        public bool IsDraggingMainScrollThumb { get; set; }
        public float DragStartMainScrollY { get; set; }
        public float DragStartMainMouseY { get; set; }
        public RectangleF MainScrollbarTrackRect { get; set; }
        public RectangleF MainScrollbarThumbRect { get; set; }
        public bool IsMainScrollbarHovered { get; set; }

        // Sidebar Scrollbar
        public bool IsDraggingSidebarScrollThumb { get; set; }
        public float DragStartSidebarScrollY { get; set; }
        public float DragStartSidebarMouseY { get; set; }
        public RectangleF SidebarScrollbarTrackRect { get; set; }
        public RectangleF SidebarScrollbarThumbRect { get; set; }
        public bool IsSidebarScrollbarHovered { get; set; }

        // Selected Tool:
        // 0: Select/Hand, 1: Add Text, 2: Edit Text, 3: Highlight, 4: Underline, 5: Strikethrough,
        // 6: Sticky Note, 7: Freehand Pen, 8: Rectangle Shape, 9: Line Shape, 10: Stamp, 11: Signature,
        // 12: Form Fill, 13: Add Form Text, 14: Add Form CheckBox, 15: Add Form Radio, 16: Add Form Dropdown, 17: Mark Redaction
        private int _selectedTool;
        public int SelectedTool
        {
            get { return _selectedTool; }
            set
            {
                _selectedTool = value;
                if (_selectedTool != 0 && _selectedTool != 12)
                {
                    // Tự động bỏ chọn các hình khối / hộp chữ / chữ ký khi chuyển sang công cụ vẽ
                    SelectedShape = null;
                    SelectedText = null;
                    SelectedSignature = null;
                    SelectedAnnotation = null;
                    SelectedFormField = null;
                    SelectedRedaction = null;
                    if (EditingText != null) CommitInlineEdit();
                    if (EditingFormField != null) CommitFormFieldEdit();
                }
            }
        }
        public Color ActiveColor { get; set; }
        public float ActiveStrokeWidth { get; set; }
        public string ActiveStampText { get; set; }

        // Form Fields (AcroForms) & Redaction
        public bool HighlightFormFields { get; set; } = true;
        public PdfFormFieldElement SelectedFormField { get; set; }
        public PdfFormFieldElement EditingFormField { get; set; }
        public string EditingFormFieldBuffer { get; set; } = "";
        public PdfRedactionElement SelectedRedaction { get; set; }

        // Sidebar View Mode (0: Thumbnails, 1: Bookmarks Outline)
        public int SidebarViewMode { get; set; } = 0;
        public PdfBookmarkNode SelectedBookmark { get; set; }
        public RectangleF SidebarTabThumbnailsRect { get; set; }
        public RectangleF SidebarTabBookmarksRect { get; set; }
        public RectangleF BtnAddBookmarkRect { get; set; }

        // Inline Text Editing State
        public PdfTextElement EditingText { get; set; }
        public int EditingPageIndex { get; set; }
        public string EditingBuffer { get; set; }
        public int CursorPosition { get; set; }
        public bool ShowCursor { get; set; }

        // Freehand Drawing in progress
        public bool IsDrawingFreehand { get; set; }
        public List<PointF> CurrentFreehandPoints { get; set; }
        public int DrawingPageIndex { get; set; }

        // Selection & Dragging Objects
        public PdfTextElement SelectedText { get; set; }
        public PdfAnnotationElement SelectedAnnotation { get; set; }
        public PdfSignatureElement SelectedSignature { get; set; }
        public PdfShapeElement SelectedShape { get; set; }
        public bool IsDraggingObject { get; set; }
        public PointF DragStartMouse { get; set; }
        public RectangleF DragStartBounds { get; set; }

        // Text box resize handles
        public bool IsResizingHandle { get; set; }
        public int DraggingHandleIndex { get; set; }
        public RectangleF DragStartBoxBounds { get; set; }
        public string ActiveFontName { get; set; }
        public float ActiveFontSize { get; set; }

        // Highlight/Box Selection Drag
        public bool IsBoxSelecting { get; set; }
        public PointF BoxSelectStart { get; set; }
        public PointF BoxSelectCurrent { get; set; }

        // Search in PDF (Ctrl + F)
        public bool ShowSearchBar { get; set; }
        public string SearchQuery { get; set; }
        public int SearchMatchCount { get; set; }
        public int CurrentSearchIndex { get; set; }
        public bool IsSearchInputFocused { get; set; }
        public List<PdfSearchMatch> SearchMatches { get; set; }
        public Color ActiveStampColor { get; set; }

        // Hover tracking
        public int HoverSidebarPageIndex { get; set; }
        public int HoverSidebarBtnIndex { get; set; } // 0: Rotate, 1: Delete

        // Trạng thái Quét Hiệu Ứng AI OCR Ma Thuật
        public bool IsOcrScanning { get; private set; }
        public float OcrProgress { get; private set; }
        public int OcrTargetPageIndex { get; private set; }
        public string OcrStatusMessage { get; set; }
        private System.Windows.Forms.Timer OcrTimer;
        private List<PdfTextElement> OcrResultElements;
        private bool IsOcrBackgroundCompleted;
        private Form OcrParentForm;

        public PdfView()
        {
            Document = new PdfDocument("Tài liệu PDF Mới.pdf");
            ActivePageIndex = 0;
            ZoomLevel = 1.0f;
            ScrollY = 0;
            TargetScrollY = 0;
            ScrollX = 0;

            ShowThumbnailsSidebar = true;
            SidebarWidth = 200;
            SidebarScrollY = 0;
            TargetSidebarScrollY = 0;

            SelectedTool = 0;
            ActiveColor = Color.FromArgb(220, 38, 38); // Crimson Red
            ActiveStrokeWidth = 2.5f;
            ActiveStampText = "APPROVED";
            ActiveStampColor = Color.FromArgb(16, 185, 129); // Xanh lục mặc định cho dấu duyệt

            EditingText = null;
            EditingPageIndex = 0;
            EditingBuffer = "";
            CursorPosition = 0;
            ShowCursor = true;

            IsDrawingFreehand = false;
            CurrentFreehandPoints = new List<PointF>();
            DrawingPageIndex = 0;

            SelectedText = null;
            SelectedAnnotation = null;
            SelectedSignature = null;
            SelectedShape = null;
            IsResizingHandle = false;
            DraggingHandleIndex = -1;
            ActiveFontName = "Segoe UI";
            ActiveFontSize = 12f;
            IsDraggingObject = false;

            IsBoxSelecting = false;
            ShowSearchBar = false;
            SearchQuery = "";
            SearchMatchCount = 0;
            CurrentSearchIndex = 0;
            IsSearchInputFocused = false;
            SearchMatches = new List<PdfSearchMatch>();

            HoverSidebarPageIndex = -1;
            HoverSidebarBtnIndex = -1;
        }

        // Undo / Redo State Management
        private readonly Stack<PdfDocument> _undoStack = new Stack<PdfDocument>();
        private readonly Stack<PdfDocument> _redoStack = new Stack<PdfDocument>();
        private bool _hasSavedUndoForCurrentEdit = false;

        public void PushUndoState(string actionName = "")
        {
            if (Document != null)
            {
                _undoStack.Push(Document.Clone());
                _redoStack.Clear();
                if (_undoStack.Count > 50)
                {
                    var arr = _undoStack.ToArray();
                    _undoStack.Clear();
                    for (int i = arr.Length - 2; i >= 0; i--)
                    {
                        _undoStack.Push(arr[i]);
                    }
                }
            }
        }

        public bool Undo()
        {
            if (_undoStack.Count > 0 && Document != null)
            {
                if (EditingText != null)
                {
                    EditingText = null;
                    EditingBuffer = "";
                }
                SelectedText = null;
                SelectedAnnotation = null;
                SelectedShape = null;
                SelectedSignature = null;
                _hasSavedUndoForCurrentEdit = false;

                _redoStack.Push(Document.Clone());
                Document = _undoStack.Pop();
                if (ActivePageIndex >= Document.Pages.Count)
                {
                    ActivePageIndex = Math.Max(0, Document.Pages.Count - 1);
                }
                InvalidateAllCaches();
                return true;
            }
            return false;
        }

        public bool Redo()
        {
            if (_redoStack.Count > 0 && Document != null)
            {
                if (EditingText != null)
                {
                    EditingText = null;
                    EditingBuffer = "";
                }
                SelectedText = null;
                SelectedAnnotation = null;
                SelectedShape = null;
                SelectedSignature = null;
                _hasSavedUndoForCurrentEdit = false;

                _undoStack.Push(Document.Clone());
                Document = _redoStack.Pop();
                if (ActivePageIndex >= Document.Pages.Count)
                {
                    ActivePageIndex = Math.Max(0, Document.Pages.Count - 1);
                }
                InvalidateAllCaches();
                return true;
            }
            return false;
        }

        public void InvalidateAllCaches()
        {
            if (Document != null && Document.Pages != null)
            {
                foreach (var page in Document.Pages)
                {
                    page.InvalidateCache();
                }
            }
        }

        #region Smooth Scrolling & Geometry Calculations
        public bool UpdateSmoothScroll()
        {
            bool moved = false;
            if (Math.Abs(TargetScrollY - ScrollY) > 0.5f)
            {
                ScrollY += (TargetScrollY - ScrollY) * 0.55f;
                if (Math.Abs(TargetScrollY - ScrollY) < 0.5f) ScrollY = TargetScrollY;
                moved = true;
            }
            else if (ScrollY != TargetScrollY)
            {
                ScrollY = TargetScrollY;
                // moved = false → snap xong, không cần redraw thêm
            }

            if (Math.Abs(TargetSidebarScrollY - SidebarScrollY) > 0.5f)
            {
                SidebarScrollY += (TargetSidebarScrollY - SidebarScrollY) * 0.55f;
                if (Math.Abs(TargetSidebarScrollY - SidebarScrollY) < 0.5f) SidebarScrollY = TargetSidebarScrollY;
                moved = true;
            }
            else if (SidebarScrollY != TargetSidebarScrollY)
            {
                SidebarScrollY = TargetSidebarScrollY;
                // moved = false → snap xong
            }

            return moved;
        }

        public void UpdateActivePageFromScroll(int viewWidth, int viewHeight, int topOffset = 155, int statusH = 26)
        {
            if (Document == null || Document.Pages == null || Document.Pages.Count == 0) return;

            int viewH = viewHeight - topOffset - statusH;
            if (viewH <= 0) return;

            float maxVisibleArea = -1;
            int bestPageIdx = -1;

            for (int i = 0; i < Document.Pages.Count; i++)
            {
                RectangleF pb = GetPageScreenBounds(i, viewWidth, viewHeight, topOffset);
                float visibleTop = Math.Max(topOffset, pb.Top);
                float visibleBottom = Math.Min(viewHeight - statusH, pb.Bottom);
                float visibleH = Math.Max(0, visibleBottom - visibleTop);

                if (visibleH > maxVisibleArea)
                {
                    maxVisibleArea = visibleH;
                    bestPageIdx = i;
                }
            }

            if (bestPageIdx >= 0 && bestPageIdx != ActivePageIndex)
            {
                ActivePageIndex = bestPageIdx;
                EnsureSidebarThumbnailVisible(bestPageIdx, viewHeight, topOffset, statusH);
            }
        }

        public void EnsureSidebarThumbnailVisible(int pageIndex, int viewHeight, int topOffset = 155, int statusH = 26)
        {
            if (!ShowThumbnailsSidebar || Document == null || Document.Pages == null || pageIndex < 0 || pageIndex >= Document.Pages.Count) return;

            int viewH = viewHeight - topOffset - statusH - 36;
            if (viewH <= 0) return;

            int thumbH = 198;
            int itemH = thumbH + 34;
            int thumbTop = pageIndex * itemH;
            int thumbBottom = thumbTop + itemH;

            float curTop = TargetSidebarScrollY;
            float curBottom = curTop + viewH;

            if (thumbTop < curTop)
            {
                TargetSidebarScrollY = Math.Max(0, thumbTop - 10);
                SidebarScrollY = TargetSidebarScrollY;
            }
            else if (thumbBottom > curBottom)
            {
                int maxScroll = GetMaxSidebarScrollY(viewHeight - topOffset - statusH - 36);
                TargetSidebarScrollY = Math.Min(maxScroll, thumbBottom - viewH + 10);
                SidebarScrollY = TargetSidebarScrollY;
            }
        }

        public int GetMaxScrollY(int viewHeight)
        {
            if (Document == null || Document.Pages.Count == 0) return 0;
            float totalH = 40f;
            foreach (var page in Document.Pages)
            {
                totalH += (page.Height * ZoomLevel) + 30f;
            }
            return Math.Max(0, (int)(totalH - viewHeight + 200));
        }

        public int GetMaxSidebarScrollY(int viewHeight)
        {
            if (Document == null || Document.Pages.Count == 0) return 0;
            int totalH = 44 + Document.Pages.Count * (198 + 34) + 60;
            return Math.Max(0, totalH - viewHeight);
        }

        public RectangleF GetPageScreenBounds(int pageIndex, int viewWidth, int viewHeight, int topOffset = 155)
        {
            if (Document == null || pageIndex < 0 || pageIndex >= Document.Pages.Count)
                return RectangleF.Empty;

            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int canvasAreaW = viewWidth - sideOffset - 14; // Trừ khoảng scrollbar bên phải

            float yAcc = topOffset + 30f - ScrollY;
            for (int i = 0; i < pageIndex; i++)
            {
                yAcc += (Document.Pages[i].Height * ZoomLevel) + 30f;
            }

            var page = Document.Pages[pageIndex];
            float renderW = page.Width * ZoomLevel;
            float renderH = page.Height * ZoomLevel;

            // Căn giữa theo chiều ngang trong vùng canvas
            float pageX = sideOffset + Math.Max(30f, (canvasAreaW - renderW) / 2f) - ScrollX;

            return new RectangleF(pageX, yAcc, renderW, renderH);
        }

        public int GetPageIndexAtPoint(int x, int y, int viewWidth, int viewHeight, int topOffset = 155)
        {
            if (Document == null) return -1;
            for (int i = 0; i < Document.Pages.Count; i++)
            {
                RectangleF bounds = GetPageScreenBounds(i, viewWidth, viewHeight, topOffset);
                if (bounds.Contains(x, y)) return i;
            }
            return -1;
        }

        public PointF ScreenToPagePoint(PointF screenPt, int pageIndex, int viewWidth, int viewHeight, int topOffset = 155)
        {
            RectangleF pageBounds = GetPageScreenBounds(pageIndex, viewWidth, viewHeight, topOffset);
            if (pageBounds.IsEmpty || ZoomLevel <= 0) return PointF.Empty;

            float pageX = (screenPt.X - pageBounds.X) / ZoomLevel;
            float pageY = (screenPt.Y - pageBounds.Y) / ZoomLevel;

            return new PointF(pageX, pageY);
        }

        public PointF PageToScreenPoint(PointF pagePt, int pageIndex, int viewWidth, int viewHeight, int topOffset = 155)
        {
            RectangleF pageBounds = GetPageScreenBounds(pageIndex, viewWidth, viewHeight, topOffset);
            if (pageBounds.IsEmpty) return PointF.Empty;

            float screenX = pageBounds.X + (pagePt.X * ZoomLevel);
            float screenY = pageBounds.Y + (pagePt.Y * ZoomLevel);

            return new PointF(screenX, screenY);
        }
        #endregion

        #region Main Render Pipeline
        public void Draw(Graphics g, int width, int height, RibbonBar ribbon, int topOffset = 155, int statusH = 26)
        {
            _lastViewWidth = width;
            _lastViewHeight = height;
            _lastTopOffset = topOffset;
            _lastStatusH = statusH;

            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int viewH = height - topOffset - statusH;
            if (viewH <= 0) return;

            // 1. Nền canvas làm việc hiện đại
            Rectangle canvasRect = new Rectangle(0, topOffset, width, viewH);
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(241, 245, 249))) // Slate-100
            {
                g.FillRectangle(bgBrush, canvasRect);
            }

            // 2. Vẽ Thanh Thu Nhỏ Trang (Page Thumbnails Sidebar)
            if (ShowThumbnailsSidebar)
            {
                DrawThumbnailsSidebar(g, width, height, topOffset, viewH);
            }

            // 3. Vùng Viewport Vẽ Các Trang PDF
            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int canvasW = width - sideOffset - 14;
            Rectangle mainViewport = new Rectangle(sideOffset, topOffset, canvasW, viewH);
            GraphicsState gState = g.Save();
            g.SetClip(mainViewport);

            if (Document != null && Document.Pages.Count > 0)
            {
                for (int i = 0; i < Document.Pages.Count; i++)
                {
                    RectangleF pageScreenBounds = GetPageScreenBounds(i, width, height, topOffset);

                    // Chỉ vẽ các trang đang nằm trong tầm nhìn (Frustum Culling)
                    if (pageScreenBounds.Bottom >= topOffset - 150 && pageScreenBounds.Top <= height + 150)
                    {
                        DrawSinglePdfPage(g, i, pageScreenBounds);
                    }
                    else
                    {
                        // Tự động giải phóng cache của trang nằm xa tầm nhìn để tiết kiệm RAM & chống lag khi zoom lớn
                        if (Document.Pages[i].RenderCache != null && (pageScreenBounds.Bottom < topOffset - 1000 || pageScreenBounds.Top > height + 1000))
                        {
                            try { Document.Pages[i].RenderCache.Dispose(); } catch { }
                            Document.Pages[i].RenderCache = null;
                        }
                    }
                }

                // Vẽ nét vẽ tay tự do đang kéo (In-progress Ink Drawing)
                if (IsDrawingFreehand && CurrentFreehandPoints != null && CurrentFreehandPoints.Count > 1)
                {
                    RectangleF pBounds = GetPageScreenBounds(DrawingPageIndex, width, height, topOffset);
                    using (Pen inkPen = new Pen(ActiveColor, ActiveStrokeWidth * ZoomLevel) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        PointF p0 = new PointF(pBounds.X + CurrentFreehandPoints[0].X * ZoomLevel, pBounds.Y + CurrentFreehandPoints[0].Y * ZoomLevel);
                        for (int k = 1; k < CurrentFreehandPoints.Count; k++)
                        {
                            PointF pk = new PointF(pBounds.X + CurrentFreehandPoints[k].X * ZoomLevel, pBounds.Y + CurrentFreehandPoints[k].Y * ZoomLevel);
                            g.DrawLine(inkPen, p0, pk);
                            p0 = pk;
                        }
                    }
                }

                // Vẽ khung chọn vùng (Box Selection Dragging for Highlight / Shape)
                if (IsBoxSelecting)
                {
                    float minX = Math.Min(BoxSelectStart.X, BoxSelectCurrent.X);
                    float minY = Math.Min(BoxSelectStart.Y, BoxSelectCurrent.Y);
                    float boxW = Math.Abs(BoxSelectCurrent.X - BoxSelectStart.X);
                    float boxH = Math.Abs(BoxSelectCurrent.Y - BoxSelectStart.Y);
                    RectangleF selRect = new RectangleF(minX, minY, boxW, boxH);

                    Color fillCol = (SelectedTool == 3) ? Color.FromArgb(60, 250, 204, 21) : Color.FromArgb(40, 59, 130, 246);
                    Color strokeCol = (SelectedTool == 3) ? Color.FromArgb(234, 179, 8) : Color.FromArgb(37, 99, 235);
                    using (SolidBrush sb = new SolidBrush(fillCol))
                    using (Pen sp = new Pen(strokeCol, 1.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.FillRectangle(sb, selRect);
                        g.DrawRectangle(sp, selRect.X, selRect.Y, selRect.Width, selRect.Height);
                    }
                }
            }

            g.Restore(gState);

            // 4. Vẽ Thanh Cuộn Dọc Chính (Main Vertical ScrollBar)
            DrawMainScrollBar(g, width, topOffset, viewH);

            // 5. Thanh Tìm Kiếm Trực Tiếp Floating (Ctrl + F Search Bar)
            if (ShowSearchBar)
            {
                DrawFloatingSearchBar(g, width, topOffset);
            }

            // 6. Hiệu Ứng Quét AI OCR Ma Thuật (Lớp phủ trắng mờ + Cây quét laser phát sáng nằm ngang)
            if (IsOcrScanning)
            {
                DrawOcrMagicOverlay(g, width, height, topOffset, statusH);
            }

            // 7. Thanh Công Cụ Khung Bầu Liquid Glass (Floating Liquid Glass Color Palette)
            // Hiển thị ở góc dưới khi chọn Cây Bút (Tool 7), Khung Vuông (Tool 8), hoặc khi đang chọn hình khối
            if (SelectedTool == 7 || SelectedTool == 8 || SelectedShape != null)
            {
                DrawLiquidGlassPalette(g, width, height, statusH);
            }
        }

        private void DrawThumbnailsSidebar(Graphics g, int width, int height, int topOffset, int viewH)
        {
            Rectangle sideRect = new Rectangle(0, topOffset, SidebarWidth, viewH);

            // Nền Sidebar
            using (SolidBrush sideBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen sideBorder = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
            {
                g.FillRectangle(sideBg, sideRect);
                g.DrawLine(sideBorder, SidebarWidth, topOffset, SidebarWidth, topOffset + viewH);
            }

            // Header Sidebar: 2 Tabs [📑 Trang] & [🔖 Mục lục]
            int tabW = SidebarWidth / 2;
            SidebarTabThumbnailsRect = new RectangleF(0, topOffset, tabW, 34);
            SidebarTabBookmarksRect = new RectangleF(tabW, topOffset, SidebarWidth - tabW, 34);

            using (Font tabFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (SolidBrush activeBg = new SolidBrush(Color.White))
            using (SolidBrush inactiveBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (SolidBrush activeTxt = new SolidBrush(Color.FromArgb(30, 58, 138)))
            using (SolidBrush inactiveTxt = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (Pen borderP = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                // Tab 0: Trang
                g.FillRectangle(SidebarViewMode == 0 ? activeBg : inactiveBg, SidebarTabThumbnailsRect);
                g.DrawRectangle(borderP, SidebarTabThumbnailsRect.X, SidebarTabThumbnailsRect.Y, SidebarTabThumbnailsRect.Width, SidebarTabThumbnailsRect.Height);
                g.DrawString($"📑 Trang ({Document?.Pages?.Count ?? 0})", tabFont, SidebarViewMode == 0 ? activeTxt : inactiveTxt, SidebarTabThumbnailsRect, sf);

                // Tab 1: Mục lục (Bookmarks)
                g.FillRectangle(SidebarViewMode == 1 ? activeBg : inactiveBg, SidebarTabBookmarksRect);
                g.DrawRectangle(borderP, SidebarTabBookmarksRect.X, SidebarTabBookmarksRect.Y, SidebarTabBookmarksRect.Width, SidebarTabBookmarksRect.Height);
                g.DrawString($"🔖 Mục lục ({Document?.Bookmarks?.Count ?? 0})", tabFont, SidebarViewMode == 1 ? activeTxt : inactiveTxt, SidebarTabBookmarksRect, sf);
            }

            if (SidebarViewMode == 1)
            {
                DrawBookmarksSidebar(g, width, height, topOffset, viewH);
                return;
            }

            // Vùng cuộn danh sách ảnh thu nhỏ các trang
            Rectangle sideListRect = new Rectangle(0, topOffset + 36, SidebarWidth - 8, viewH - 36);
            GraphicsState sState = g.Save();
            g.SetClip(sideListRect);

            if (Document != null && Document.Pages.Count > 0)
            {
                int thumbW = 140;
                int thumbH = 198;
                int startY = topOffset + 44;

                for (int i = 0; i < Document.Pages.Count; i++)
                {
                    int thumbX = (SidebarWidth - 8 - thumbW) / 2;
                    int thumbY = startY - (int)SidebarScrollY + i * (thumbH + 34);

                    if (thumbY + thumbH >= topOffset + 36 && thumbY <= topOffset + viewH)
                    {
                        Rectangle thumbRect = new Rectangle(thumbX, thumbY, thumbW, thumbH);
                        bool isCurrent = (i == ActivePageIndex);
                        bool isHover = (i == HoverSidebarPageIndex);

                        // Đổ bóng Thumbnail
                        using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(isCurrent ? 40 : 20, 0, 0, 0)))
                        {
                            g.FillRectangle(shadowB, thumbRect.X + 2, thumbRect.Y + 2, thumbRect.Width, thumbRect.Height);
                        }

                        // Nền trang thu nhỏ
                        using (SolidBrush pBg = new SolidBrush(Color.White))
                        using (Pen pBorder = new Pen(isCurrent ? Color.FromArgb(220, 38, 38) : (isHover ? Color.FromArgb(148, 163, 184) : Color.FromArgb(203, 213, 225)), isCurrent ? 2.5f : 1.0f))
                        {
                            g.FillRectangle(pBg, thumbRect);

                            // Vẽ phác thảo nội dung trang bên trong thumbnail (có bộ đệm Cache)
                            DrawMiniPageContent(g, Document.Pages[i], thumbRect);

                            g.DrawRectangle(pBorder, thumbRect);
                        }

                        // Nhãn số trang
                        string pageNumStr = "Trang " + (i + 1);
                        using (Font pFont = new Font("Segoe UI", 8.5f, isCurrent ? FontStyle.Bold : FontStyle.Regular))
                        using (SolidBrush pTxt = new SolidBrush(isCurrent ? Color.FromArgb(220, 38, 38) : Color.FromArgb(71, 85, 105)))
                        {
                            g.DrawString(pageNumStr, pFont, pTxt, thumbX + 46, thumbY + thumbH + 6);
                        }

                        // Nút thao tác nhanh khi Hover trang thumbnail: Xoay & Xóa
                        if (isHover)
                        {
                            Rectangle rotBtn = new Rectangle(thumbRect.Right - 38, thumbY + 4, 16, 16);
                            Rectangle delBtn = new Rectangle(thumbRect.Right - 18, thumbY + 4, 16, 16);
                            using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                            {
                                g.FillRectangle(btnBg, rotBtn);
                                g.FillRectangle(btnBg, delBtn);
                            }
                            g.DrawString("⟳", new Font("Segoe UI", 8.5f, FontStyle.Bold), Brushes.DarkBlue, rotBtn.X + 1, rotBtn.Y);
                            g.DrawString("×", new Font("Segoe UI", 9.5f, FontStyle.Bold), Brushes.Red, delBtn.X + 2, delBtn.Y - 1);
                        }
                    }
                }
            }

            g.Restore(sState);

            // Vẽ Thanh Cuộn Dọc cho Sidebar
            DrawSidebarScrollBar(g, topOffset + 36, viewH - 36);
        }

        private void DrawSidebarScrollBar(Graphics g, int topY, int height)
        {
            int maxScroll = GetMaxSidebarScrollY(height);
            if (maxScroll <= 0)
            {
                SidebarScrollbarTrackRect = RectangleF.Empty;
                SidebarScrollbarThumbRect = RectangleF.Empty;
                return;
            }

            float trackX = SidebarWidth - 7f;
            float trackW = 5f;
            SidebarScrollbarTrackRect = new RectangleF(trackX, topY, trackW, height);

            float visibleFrac = (float)height / (height + maxScroll);
            float thumbH = Math.Max(24f, visibleFrac * height);
            float scrollFrac = Math.Max(0f, Math.Min(1f, SidebarScrollY / maxScroll));
            float thumbY = topY + scrollFrac * (height - thumbH);

            SidebarScrollbarThumbRect = new RectangleF(trackX, thumbY, trackW, thumbH);

            Color thumbColor = IsDraggingSidebarScrollThumb ? Color.FromArgb(148, 163, 184) :
                              (IsSidebarScrollbarHovered ? Color.FromArgb(203, 213, 225) : Color.FromArgb(226, 232, 240));

            using (SolidBrush sb = new SolidBrush(thumbColor))
            using (GraphicsPath path = LiquidGlassHelper.CreateRoundedRectanglePath(SidebarScrollbarThumbRect, 2.5f))
            {
                g.FillPath(sb, path);
            }
        }

        private void DrawMainScrollBar(Graphics g, int width, int topOffset, int viewH)
        {
            int maxScroll = GetMaxScrollY(viewH);
            if (maxScroll <= 0)
            {
                MainScrollbarTrackRect = RectangleF.Empty;
                MainScrollbarThumbRect = RectangleF.Empty;
                return;
            }

            float trackX = width - 12f;
            float trackW = 10f;
            MainScrollbarTrackRect = new RectangleF(trackX, topOffset, trackW, viewH);

            // Vẽ nền track thanh cuộn
            using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.FillRectangle(trackBg, MainScrollbarTrackRect);
            }

            float visibleFrac = (float)viewH / (viewH + maxScroll);
            float thumbH = Math.Max(32f, visibleFrac * viewH);
            float scrollFrac = Math.Max(0f, Math.Min(1f, ScrollY / maxScroll));
            float thumbY = topOffset + scrollFrac * (viewH - thumbH);

            MainScrollbarThumbRect = new RectangleF(trackX + 1f, thumbY, trackW - 2f, thumbH);

            Color thumbColor = IsDraggingMainScrollThumb ? Color.FromArgb(100, 116, 139) :
                              (IsMainScrollbarHovered ? Color.FromArgb(148, 163, 184) : Color.FromArgb(203, 213, 225));

            using (SolidBrush sb = new SolidBrush(thumbColor))
            using (GraphicsPath path = LiquidGlassHelper.CreateRoundedRectanglePath(MainScrollbarThumbRect, 4f))
            {
                g.FillPath(sb, path);
            }
        }

        private void DrawMiniPageContent(Graphics g, PdfPageModel page, Rectangle thumbRect)
        {
            if (page == null) return;

            // Dùng ThumbnailCache để tăng tốc độ cuộn Sidebar lên 60+ FPS
            if (page.ThumbnailCache != null && page.ThumbnailCache.Width == thumbRect.Width && page.ThumbnailCache.Height == thumbRect.Height)
            {
                g.DrawImage(page.ThumbnailCache, thumbRect);
                return;
            }

            try
            {
                if (page.ThumbnailCache != null)
                {
                    try { page.ThumbnailCache.Dispose(); } catch { }
                    page.ThumbnailCache = null;
                }

                if (Document != null && Document.UnderlyingPdfiumDoc != null && page.PageNumber - 1 >= 0 && page.PageNumber - 1 < Document.UnderlyingPdfiumDoc.PageCount)
                {
                    try
                    {
                        Image miniNative = Document.UnderlyingPdfiumDoc.Render(page.PageNumber - 1, thumbRect.Width, thumbRect.Height, 96f, 96f, PdfiumViewer.PdfRenderFlags.Annotations | PdfiumViewer.PdfRenderFlags.LcdText);
                        if (miniNative != null)
                        {
                            page.ThumbnailCache = new Bitmap(miniNative);
                            miniNative.Dispose();
                            g.DrawImage(page.ThumbnailCache, thumbRect);
                            return;
                        }
                    }
                    catch { }
                }

                Bitmap miniBmp = new Bitmap(thumbRect.Width, thumbRect.Height);
                using (Graphics mg = Graphics.FromImage(miniBmp))
                {
                    mg.SmoothingMode = SmoothingMode.HighSpeed;
                    mg.Clear(page.BackgroundColor);

                    float scaleX = (float)thumbRect.Width / page.Width;
                    float scaleY = (float)thumbRect.Height / page.Height;

                    using (SolidBrush lineBrush = new SolidBrush(Color.FromArgb(190, 205, 220)))
                    {
                        foreach (var txt in page.TextElements)
                        {
                            float tx = txt.Bounds.X * scaleX;
                            float ty = txt.Bounds.Y * scaleY;
                            float tw = Math.Min(thumbRect.Width - 10, txt.Bounds.Width * scaleX);
                            float th = Math.Max(2f, txt.Bounds.Height * scaleY * 0.7f);
                            mg.FillRectangle(lineBrush, tx, ty, tw, th);
                        }
                    }
                }

                page.ThumbnailCache = miniBmp;
                g.DrawImage(miniBmp, thumbRect);
            }
            catch
            {
            }
        }

        private void DrawSinglePdfPage(Graphics g, int pageIndex, RectangleF bounds)
        {
            PdfPageModel page = Document.Pages[pageIndex];

            // 1. Kiểm tra Cache để đạt 60+ FPS mượt mà tuyệt đối khi cuộn/kéo
            int targetW = Math.Max(10, (int)Math.Round(page.Width * ZoomLevel));
            int targetH = Math.Max(10, (int)Math.Round(page.Height * ZoomLevel));

            if (page.RenderCache == null || Math.Abs(page.CachedZoomLevel - ZoomLevel) > 0.005f || page.RenderCache.Width != targetW || page.RenderCache.Height != targetH)
            {
                RenderPageToCache(page, pageIndex, targetW, targetH);
            }

            if (page.RenderCache != null)
            {
                InterpolationMode oldInterp = g.InterpolationMode;
                PixelOffsetMode oldOffset = g.PixelOffsetMode;
                SmoothingMode oldSmooth = g.SmoothingMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(page.RenderCache, (int)Math.Round(bounds.X), (int)Math.Round(bounds.Y), targetW, targetH);

                g.InterpolationMode = oldInterp;
                g.PixelOffsetMode = oldOffset;
                g.SmoothingMode = oldSmooth;
            }
            else if (page.NativePageImage != null)
            {
                g.DrawImage(page.NativePageImage, (int)Math.Round(bounds.X), (int)Math.Round(bounds.Y), targetW, targetH);
            }
            else
            {
                using (SolidBrush pageBg = new SolidBrush(page.BackgroundColor))
                {
                    g.FillRectangle(pageBg, bounds);
                }
            }

            // 2. Viền trang phẳng, sắc nét (Flat clean page border)
            using (Pen pageBorder = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawRectangle(pageBorder, (int)Math.Round(bounds.X), (int)Math.Round(bounds.Y), targetW, targetH);
            }

            // 3. Vẽ các lớp tương tác trực tiếp trên nền trang gốc (Shapes, Annotations, Chữ ký, Inks)
            GraphicsState preState = g.Save();
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Watermark
            if (Document.GlobalWatermark != null && Document.GlobalWatermark.IsEnabled && !string.IsNullOrEmpty(Document.GlobalWatermark.Text))
            {
                DrawPageWatermark(g, bounds, Document.GlobalWatermark);
            }

            // Hình ảnh thêm mới
            foreach (var img in page.ImageElements)
            {
                DrawImageElement(g, bounds, img);
            }

            // Hình khối thêm mới
            foreach (var shp in page.ShapeElements)
            {
                DrawShape(g, bounds, shp);
            }

            // Văn bản thêm mới
            for (int tIdx = 0; tIdx < page.TextElements.Count; tIdx++)
            {
                DrawTextElement(g, bounds, page.TextElements[tIdx]);
            }

            // Chữ ký điện tử
            foreach (var sig in page.Signatures)
            {
                DrawSignature(g, bounds, sig);
            }

            // Chú thích Annotations (Highlight, Bút vẽ, Ghi chú, Con dấu)
            foreach (var ann in page.Annotations)
            {
                DrawAnnotation(g, bounds, ann);
            }

            // Nét vẽ tay tự do đang vẽ trực tiếp
            if (IsDrawingFreehand && DrawingPageIndex == pageIndex && CurrentFreehandPoints != null && CurrentFreehandPoints.Count > 1)
            {
                float strokeW = (ActiveStrokeWidth <= 0 ? 2.5f : ActiveStrokeWidth) * ZoomLevel;
                using (Pen pen = new Pen(ActiveColor, strokeW) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    PointF[] screenPts = new PointF[CurrentFreehandPoints.Count];
                    for (int pt = 0; pt < CurrentFreehandPoints.Count; pt++)
                    {
                        screenPts[pt] = new PointF(bounds.X + CurrentFreehandPoints[pt].X * ZoomLevel, bounds.Y + CurrentFreehandPoints[pt].Y * ZoomLevel);
                    }
                    g.DrawLines(pen, screenPts);
                }
            }

            // Biểu mẫu tương tác Form Fields (AcroForms)
            if (page.FormFields != null)
            {
                foreach (var ff in page.FormFields)
                {
                    DrawFormField(g, bounds, ff, (EditingFormField == ff));
                }
            }

            // Vùng kiểm duyệt & xóa thông tin mật (Redactions)
            if (page.Redactions != null)
            {
                foreach (var red in page.Redactions)
                {
                    DrawRedaction(g, bounds, red);
                }
            }

            // Đánh số hồ sơ Bates
            if (!string.IsNullOrEmpty(page.BatesNumber))
            {
                DrawBatesNumber(g, bounds, page);
            }

            // Hộp kéo chọn đánh dấu Redaction trực tiếp
            if (IsBoxSelecting && SelectedTool == 17 && DrawingPageIndex == pageIndex)
            {
                float rx = Math.Min(BoxSelectStart.X, BoxSelectCurrent.X) * ZoomLevel;
                float ry = Math.Min(BoxSelectStart.Y, BoxSelectCurrent.Y) * ZoomLevel;
                float rw = Math.Abs(BoxSelectCurrent.X - BoxSelectStart.X) * ZoomLevel;
                float rh = Math.Abs(BoxSelectCurrent.Y - BoxSelectStart.Y) * ZoomLevel;
                RectangleF dragRect = new RectangleF(bounds.X + rx, bounds.Y + ry, rw, rh);

                using (SolidBrush fillB = new SolidBrush(Color.FromArgb(50, 239, 68, 68)))
                using (Pen borderP = new Pen(Color.FromArgb(239, 68, 68), 2f) { DashStyle = DashStyle.Dash })
                {
                    g.FillRectangle(fillB, dragRect);
                    g.DrawRectangle(borderP, dragRect.X, dragRect.Y, dragRect.Width, dragRect.Height);
                }
            }

            g.Restore(preState);

            // 4. Viền trang
            using (Pen pageBorder = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawRectangle(pageBorder, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            // 5. Hộp soạn thảo văn bản trực tiếp
            if (EditingText != null && EditingPageIndex == pageIndex)
            {
                DrawInlineTextEditor(g, bounds, EditingText);
            }

            // 6. Bôi sáng từ khóa tìm kiếm (Search Matches)
            if (ShowSearchBar && !string.IsNullOrEmpty(SearchQuery))
            {
                DrawSearchHighlights(g, bounds, page);
            }
        }

        private void RenderPageToCache(PdfPageModel page, int pageIndex, int targetW, int targetH)
        {
            try
            {
                if (page.RenderCache != null)
                {
                    try { page.RenderCache.Dispose(); } catch { }
                    page.RenderCache = null;
                }

                // 1. Nếu tài liệu nạp từ tệp PDF: Render bằng Native Chromium Engine (Pdfium) 100% chuẩn xác font, vector, math, layout với LcdText sắc nét
                if (Document != null && Document.UnderlyingPdfiumDoc != null && pageIndex >= 0 && pageIndex < Document.UnderlyingPdfiumDoc.PageCount)
                {
                    try
                    {
                        Image nativeImg = Document.UnderlyingPdfiumDoc.Render(pageIndex, targetW, targetH, 96f, 96f, PdfiumViewer.PdfRenderFlags.Annotations | PdfiumViewer.PdfRenderFlags.LcdText);
                        if (nativeImg != null)
                        {
                            page.RenderCache = new Bitmap(nativeImg);
                            nativeImg.Dispose();
                            page.CachedZoomLevel = ZoomLevel;
                            return;
                        }
                    }
                    catch { }
                }

                // 2. Fallback: Trang trắng tạo mới trong GenOffice
                Bitmap pageBmp = new Bitmap(targetW, targetH);
                using (Graphics bg = Graphics.FromImage(pageBmp))
                {
                    bg.SmoothingMode = SmoothingMode.HighQuality;
                    bg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    bg.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    RectangleF localBounds = new RectangleF(0, 0, targetW, targetH);

                    using (SolidBrush pageBg = new SolidBrush(page.BackgroundColor))
                    {
                        bg.FillRectangle(pageBg, localBounds);
                    }
                }

                page.RenderCache = pageBmp;
                page.CachedZoomLevel = ZoomLevel;
            }
            catch
            {
            }
        }

        private void DrawImageElement(Graphics g, RectangleF pageBounds, PdfImageElement img)
        {
            if (img == null || img.ImageData == null) return;
            float screenX = pageBounds.X + (img.Bounds.X * ZoomLevel);
            float screenY = pageBounds.Y + (img.Bounds.Y * ZoomLevel);
            float screenW = img.Bounds.Width * ZoomLevel;
            float screenH = img.Bounds.Height * ZoomLevel;
            RectangleF screenRect = new RectangleF(screenX, screenY, screenW, screenH);

            g.DrawImage(img.ImageData, screenRect);

            if (SelectedShape != null && SelectedShape.Bounds == img.Bounds)
            {
                using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(selPen, screenRect.X - 2, screenRect.Y - 2, screenRect.Width + 4, screenRect.Height + 4);
                }
            }
        }

        private void DrawPageWatermark(Graphics g, RectangleF pageBounds, PdfWatermark watermark)
        {
            GraphicsState gWs = g.Save();
            g.TranslateTransform(pageBounds.X + pageBounds.Width / 2f, pageBounds.Y + pageBounds.Height / 2f);
            g.RotateTransform(watermark.AngleDegrees);

            using (Font wmFont = new Font("Segoe UI", watermark.FontSize * ZoomLevel, FontStyle.Bold))
            using (SolidBrush wmBrush = new SolidBrush(Color.FromArgb((int)(watermark.Opacity * 255), watermark.Color)))
            {
                SizeF sz = g.MeasureString(watermark.Text, wmFont);
                g.DrawString(watermark.Text, wmFont, wmBrush, -sz.Width / 2f, -sz.Height / 2f);
            }
            g.Restore(gWs);
        }

        private void DrawTextElement(Graphics g, RectangleF pageBounds, PdfTextElement txt)
        {
            if (txt == null) return;

            float screenX = pageBounds.X + (txt.Bounds.X * ZoomLevel);
            float screenY = pageBounds.Y + (txt.Bounds.Y * ZoomLevel);
            float screenW = Math.Max(16f, txt.Bounds.Width * ZoomLevel);
            float screenH = Math.Max(12f, txt.Bounds.Height * ZoomLevel);
            RectangleF screenRect = new RectangleF(screenX, screenY, screenW, screenH);

            // Xử lý dòng văn bản trích xuất từ PDF gốc
            if (txt.IsDetectedFromPdf)
            {
                // Nếu chưa sửa nội dung (vẫn giữ nguyên văn bản gốc của PDF):
                if (txt.Text == txt.OriginalText)
                {
                    // Nếu đang bật công cụ Sửa chữ (Tool 2), hiển thị viền chấm gợi ý màu xanh để người dùng nhận biết nhấp sửa được
                    if (SelectedTool == 2 && SelectedText != txt && EditingText != txt)
                    {
                        using (Pen hintPen = new Pen(Color.FromArgb(90, 59, 130, 246), 1.0f) { DashStyle = DashStyle.Dot })
                        {
                            g.DrawRectangle(hintPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
                        }
                    }

                    // Nếu ô đang được chọn (SelectedText == txt) nhưng chưa sửa:
                    // Tuyệt đối KHÔNG che nền trắng đục! Chỉ vẽ viền chọn và các chốt vuông
                    if (SelectedText == txt && EditingText != txt)
                    {
                        using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
                        {
                            g.DrawRectangle(selPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
                        }
                        DrawResizeHandles(g, screenRect);
                    }

                    return; // Chữ gốc đã được Pdfium vẽ sắc nét trên nền
                }

                // Nếu người dùng đã sửa đổi chữ khác với nguyên bản PDF (txt.Text != txt.OriginalText):
                // Lúc này che phủ dòng chữ cũ ở vị trí gốc bằng màu nền chuẩn đã được trích xuất (hoặc màu trắng)
                RectangleF origScreenRect = new RectangleF(
                    pageBounds.X + (txt.OriginalBounds.X * ZoomLevel),
                    pageBounds.Y + (txt.OriginalBounds.Y * ZoomLevel),
                    Math.Max(16f, txt.OriginalBounds.Width * ZoomLevel),
                    Math.Max(12f, txt.OriginalBounds.Height * ZoomLevel)
                );
                Color coverCol = (txt.DetectedBackgroundColor != Color.Transparent && txt.DetectedBackgroundColor.A > 0)
                    ? txt.DetectedBackgroundColor
                    : Color.White;
                using (SolidBrush coverBrush = new SolidBrush(coverCol))
                {
                    g.FillRectangle(coverBrush, origScreenRect.X - 1, origScreenRect.Y - 1, origScreenRect.Width + 2, origScreenRect.Height + 2);
                }
            }

            // 1. Vẽ Màu Nền Ô (Fill Color)
            if (txt.BackgroundColor != Color.Transparent && txt.BackgroundColor.A > 0)
            {
                using (SolidBrush bgBrush = new SolidBrush(txt.BackgroundColor))
                {
                    g.FillRectangle(bgBrush, screenRect);
                }
            }

            // 2. Vẽ Viền Khung (Border Color)
            if (txt.BorderColor != Color.Transparent && txt.BorderColor.A > 0 && txt.BorderWidth > 0)
            {
                float bWidth = Math.Max(1f, txt.BorderWidth * ZoomLevel);
                using (Pen borderPen = new Pen(txt.BorderColor, bWidth))
                {
                    g.DrawRectangle(borderPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
                }
            }

            // 3. Nội dung chữ
            if (!string.IsNullOrEmpty(txt.Text))
            {
                FontStyle fs = FontStyle.Regular;
                if (txt.IsBold) fs |= FontStyle.Bold;
                if (txt.IsItalic) fs |= FontStyle.Italic;
                if (txt.IsUnderline) fs |= FontStyle.Underline;

                float scaledFontSize = Math.Max(6f, txt.FontSize * ZoomLevel);
                string fontToUse = string.IsNullOrEmpty(txt.FontName) ? "Times New Roman" : txt.FontName;
                using (Font f = new Font(fontToUse, scaledFontSize, fs, GraphicsUnit.Pixel))
                using (SolidBrush b = new SolidBrush(txt.TextColor))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = txt.Alignment;
                    sf.LineAlignment = StringAlignment.Near;
                    sf.Trimming = StringTrimming.Word;
                    RectangleF textInnerRect = new RectangleF(screenRect.X + 2, screenRect.Y + 2, Math.Max(10f, screenRect.Width - 4), Math.Max(10f, screenRect.Height - 4));
                    g.DrawString(txt.Text, f, b, textInnerRect, sf);
                }
            }

            // 4. Nếu đang được chọn (Selected) nhưng không đang trong chế độ soạn thảo (Editing)
            if (SelectedText == txt && EditingText != txt)
            {
                using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(selPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
                }
                DrawResizeHandles(g, screenRect);
            }
        }

        private void DrawResizeHandles(Graphics g, RectangleF rect)
        {
            PointF[] handles = new PointF[] {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Left + rect.Width / 2f, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Top + rect.Height / 2f),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left + rect.Width / 2f, rect.Bottom),
                new PointF(rect.Left, rect.Bottom),
                new PointF(rect.Left, rect.Top + rect.Height / 2f)
            };

            float hs = 8f; // Chốt vuông kích thước 8px nổi bật, dễ nắm kéo
            using (SolidBrush handleBrush = new SolidBrush(Color.White))
            using (Pen handlePen = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
            {
                for (int h = 0; h < handles.Length; h++)
                {
                    g.FillRectangle(handleBrush, handles[h].X - hs / 2f, handles[h].Y - hs / 2f, hs, hs);
                    g.DrawRectangle(handlePen, handles[h].X - hs / 2f, handles[h].Y - hs / 2f, hs, hs);
                }
            }
        }

        public int HitTestResizeHandles(RectangleF rect, PointF pt, float hitRadius = 10f)
        {
            PointF[] handles = new PointF[] {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Left + rect.Width / 2f, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Top + rect.Height / 2f),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left + rect.Width / 2f, rect.Bottom),
                new PointF(rect.Left, rect.Bottom),
                new PointF(rect.Left, rect.Top + rect.Height / 2f)
            };

            for (int h = 0; h < handles.Length; h++)
            {
                RectangleF hRect = new RectangleF(handles[h].X - hitRadius, handles[h].Y - hitRadius, hitRadius * 2, hitRadius * 2);
                if (hRect.Contains(pt)) return h;
            }
            return -1;
        }

        public Cursor GetResizeHandleCursor(int handleIndex)
        {
            switch (handleIndex)
            {
                case 0: // Top-Left
                case 4: // Bottom-Right
                    return Cursors.SizeNWSE;
                case 1: // Top-Middle
                case 5: // Bottom-Middle
                    return Cursors.SizeNS;
                case 2: // Top-Right
                case 6: // Bottom-Left
                    return Cursors.SizeNESW;
                case 3: // Middle-Right
                case 7: // Middle-Left
                    return Cursors.SizeWE;
                default:
                    return Cursors.Default;
            }
        }

        private void DrawInlineTextEditor(Graphics g, RectangleF pageBounds, PdfTextElement txt)
        {
            float screenX = pageBounds.X + (txt.Bounds.X * ZoomLevel);
            float screenY = pageBounds.Y + (txt.Bounds.Y * ZoomLevel);
            float screenW = Math.Max(16f, txt.Bounds.Width * ZoomLevel);
            float screenH = Math.Max(12f, txt.Bounds.Height * ZoomLevel);
            RectangleF screenRect = new RectangleF(screenX, screenY, screenW, screenH);

            bool isModified = (txt.IsDetectedFromPdf && EditingBuffer != txt.OriginalText) || (!txt.IsDetectedFromPdf && !string.IsNullOrEmpty(EditingBuffer));

            // Chỉ che phủ dòng chữ cũ trên ảnh nền PDF KHI VÀ CHỈ KHI người dùng thực sự thay đổi nội dung chữ
            if (txt.IsDetectedFromPdf && isModified)
            {
                RectangleF origScreenRect = new RectangleF(
                    pageBounds.X + (txt.OriginalBounds.X * ZoomLevel),
                    pageBounds.Y + (txt.OriginalBounds.Y * ZoomLevel),
                    Math.Max(16f, txt.OriginalBounds.Width * ZoomLevel),
                    Math.Max(12f, txt.OriginalBounds.Height * ZoomLevel)
                );
                Color coverCol = (txt.DetectedBackgroundColor != Color.Transparent && txt.DetectedBackgroundColor.A > 0)
                    ? txt.DetectedBackgroundColor
                    : Color.White;
                using (SolidBrush coverB = new SolidBrush(coverCol))
                {
                    g.FillRectangle(coverB, origScreenRect.X - 1, origScreenRect.Y - 1, origScreenRect.Width + 2, origScreenRect.Height + 2);
                }
            }

            // 1. Nền hộp soạn thảo: Mặc định trong suốt 100%, chỉ vẽ khi người dùng chọn màu nền khác
            // HOẶC khi đã gõ sửa chữ và có DetectedBackgroundColor (để che chữ ảnh scan/PDF cũ bên dưới):
            Color fillBg = Color.Transparent;
            if (txt.BackgroundColor != Color.Transparent && txt.BackgroundColor.A > 0)
            {
                fillBg = txt.BackgroundColor;
            }
            else if (txt.DetectedBackgroundColor != Color.Transparent && txt.DetectedBackgroundColor.A > 0 && EditingBuffer != txt.OriginalText)
            {
                fillBg = txt.DetectedBackgroundColor;
            }

            if (fillBg != Color.Transparent && fillBg.A > 0)
            {
                using (SolidBrush editBg = new SolidBrush(fillBg))
                {
                    g.FillRectangle(editBg, screenRect);
                }
            }

            // 2. Viền tùy chỉnh của người dùng (nếu có)
            if (txt.BorderColor != Color.Transparent && txt.BorderColor.A > 0 && txt.BorderWidth > 0)
            {
                float bWidth = Math.Max(1f, txt.BorderWidth * ZoomLevel);
                using (Pen borderPen = new Pen(txt.BorderColor, bWidth))
                {
                    g.DrawRectangle(borderPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
                }
            }

            // 3. Viền xanh đứt nét biểu thị ô đang được chọn/soạn thảo (không có nền trắng đục)
            using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
            {
                g.DrawRectangle(selPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
            }

            // 4. Vẽ 8 chốt vuông co giãn kích thước (Resize Handles)
            DrawResizeHandles(g, screenRect);

            float scaledFontSize = Math.Max(6f, txt.FontSize * ZoomLevel);
            FontStyle fs = FontStyle.Regular;
            if (txt.IsBold) fs |= FontStyle.Bold;
            if (txt.IsItalic) fs |= FontStyle.Italic;
            if (txt.IsUnderline) fs |= FontStyle.Underline;

            string fontToUse = string.IsNullOrEmpty(txt.FontName) ? "Times New Roman" : txt.FontName;

            using (Font f = new Font(fontToUse, scaledFontSize, fs, GraphicsUnit.Pixel))
            {
                // Nếu là Placeholder và chưa gõ gì -> Hiển thị chữ mờ ảo "Nhập văn bản..." và con trỏ ở đầu
                if (txt.IsPlaceholder && string.IsNullOrEmpty(EditingBuffer))
                {
                    using (Font ghostFont = new Font(f.FontFamily, scaledFontSize, FontStyle.Italic, GraphicsUnit.Pixel))
                    using (SolidBrush ghostBrush = new SolidBrush(Color.FromArgb(160, 174, 192)))
                    {
                        g.DrawString("Nhập văn bản...", ghostFont, ghostBrush, screenRect.X + 2, screenRect.Y + 2);
                    }

                    if (ShowCursor)
                    {
                        using (Pen cPen = new Pen(Color.Black, 1.8f))
                        {
                            g.DrawLine(cPen, screenRect.X + 2, screenRect.Y + 2, screenRect.X + 2, Math.Min(screenRect.Bottom - 2, screenRect.Y + 2 + scaledFontSize * 1.25f));
                        }
                    }
                }
                else
                {
                    // Màu con trỏ phù hợp với màu chữ và màu nền
                    Color curColor = (txt.TextColor == Color.White || (txt.DetectedBackgroundColor.R < 100 && txt.DetectedBackgroundColor.G < 100 && txt.DetectedBackgroundColor.B < 150))
                        ? Color.White
                        : Color.FromArgb(37, 99, 235);

                    // Nếu là chữ phát hiện từ PDF và CHƯA sửa đổi:
                    // Chữ gốc đã được PDFium vẽ sắc nét trên nền, KHÔNG VẼ ĐÈ GDI+ để tránh bị lệch/phóng to!
                    // Chỉ vẽ con trỏ nhấp nháy!
                    if (txt.IsDetectedFromPdf && !isModified)
                    {
                        if (ShowCursor)
                        {
                            string beforeCursor = (EditingBuffer != null && CursorPosition <= EditingBuffer.Length) ? EditingBuffer.Substring(0, CursorPosition) : "";
                            SizeF sz = g.MeasureString(beforeCursor, f, new PointF(0, 0), StringFormat.GenericTypographic);
                            float curX = screenRect.X + 2 + sz.Width;
                            using (Pen cPen = new Pen(curColor, 1.8f))
                            {
                                g.DrawLine(cPen, curX, screenRect.Y + 2, curX, Math.Min(screenRect.Bottom - 2, screenRect.Y + 2 + scaledFontSize * 1.25f));
                            }
                        }
                    }
                    else
                    {
                        // Người dùng đã sửa đổi chữ hoặc thêm chữ mới: Vẽ nội dung mới với chế độ wrap tự nhiên
                        using (SolidBrush b = new SolidBrush(txt.TextColor))
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = txt.Alignment;
                            sf.LineAlignment = StringAlignment.Near;
                            sf.Trimming = StringTrimming.Word;
                            RectangleF textInner = new RectangleF(screenRect.X + 2, screenRect.Y + 2, Math.Max(10f, screenRect.Width - 4), Math.Max(10f, screenRect.Height - 4));
                            g.DrawString(EditingBuffer ?? "", f, b, textInner, sf);

                            if (ShowCursor)
                            {
                                float curX = screenRect.X + 2;
                                float curY = screenRect.Y + 2;
                                float curH = Math.Min(screenRect.Height, scaledFontSize * 1.25f);

                                if (!string.IsNullOrEmpty(EditingBuffer) && CursorPosition > 0)
                                {
                                    try
                                    {
                                        int checkPos = Math.Min(CursorPosition, EditingBuffer.Length);
                                        int lastNl = EditingBuffer.LastIndexOf('\n', Math.Max(0, checkPos - 1));
                                        int lineStart = (lastNl >= 0) ? lastNl + 1 : 0;
                                        int numLinesBefore = 0;
                                        for (int ci = 0; ci < checkPos; ci++) if (EditingBuffer[ci] == '\n') numLinesBefore++;

                                        string lineBefore = EditingBuffer.Substring(lineStart, checkPos - lineStart);
                                        SizeF lsz = g.MeasureString(lineBefore, f, new PointF(0, 0), StringFormat.GenericTypographic);
                                        curX = screenRect.X + 2 + lsz.Width;
                                        curY = screenRect.Y + 2 + numLinesBefore * (scaledFontSize * 1.2f);
                                    }
                                    catch
                                    {
                                        string beforeCursor = (EditingBuffer != null && CursorPosition <= EditingBuffer.Length) ? EditingBuffer.Substring(0, CursorPosition) : "";
                                        SizeF sz = g.MeasureString(beforeCursor, f, new PointF(0, 0), StringFormat.GenericTypographic);
                                        curX = screenRect.X + 2 + sz.Width;
                                    }
                                }

                                using (Pen cPen = new Pen(curColor, 1.8f))
                                {
                                    g.DrawLine(cPen, curX, curY, curX, Math.Min(screenRect.Bottom - 2, curY + curH));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawAnnotation(Graphics g, RectangleF pageBounds, PdfAnnotationElement ann)
        {
            if (ann == null) return;

            if (ann.AnnotationType == PdfAnnotationType.Highlight)
            {
                float hx = pageBounds.X + (ann.Bounds.X * ZoomLevel);
                float hy = pageBounds.Y + (ann.Bounds.Y * ZoomLevel);
                float hw = ann.Bounds.Width * ZoomLevel;
                float hh = ann.Bounds.Height * ZoomLevel;
                RectangleF hRect = new RectangleF(hx, hy, hw, hh);

                using (SolidBrush hb = new SolidBrush(Color.FromArgb((int)(ann.Opacity * 255), ann.Color)))
                {
                    g.FillRectangle(hb, hRect);
                }
            }
            else if (ann.AnnotationType == PdfAnnotationType.Underline)
            {
                float ux = pageBounds.X + (ann.Bounds.X * ZoomLevel);
                float uy = pageBounds.Y + (ann.Bounds.Bottom * ZoomLevel);
                float uw = ann.Bounds.Width * ZoomLevel;
                using (Pen up = new Pen(ann.Color, ann.StrokeWidth * ZoomLevel))
                {
                    g.DrawLine(up, ux, uy, ux + uw, uy);
                }
            }
            else if (ann.AnnotationType == PdfAnnotationType.Strikethrough)
            {
                float sx = pageBounds.X + (ann.Bounds.X * ZoomLevel);
                float sy = pageBounds.Y + ((ann.Bounds.Y + ann.Bounds.Height / 2f) * ZoomLevel);
                float sw = ann.Bounds.Width * ZoomLevel;
                using (Pen sp = new Pen(ann.Color, ann.StrokeWidth * ZoomLevel))
                {
                    g.DrawLine(sp, sx, sy, sx + sw, sy);
                }
            }
            else if (ann.AnnotationType == PdfAnnotationType.StickyNote)
            {
                float nx = pageBounds.X + (ann.Bounds.X * ZoomLevel);
                float ny = pageBounds.Y + (ann.Bounds.Y * ZoomLevel);
                RectangleF noteRect = new RectangleF(nx, ny, 24 * ZoomLevel, 24 * ZoomLevel);

                using (SolidBrush nb = new SolidBrush(Color.FromArgb(250, 204, 21))) // Yellow note icon
                using (Pen np = new Pen(Color.FromArgb(202, 138, 4), 1.2f))
                {
                    g.FillRectangle(nb, noteRect);
                    g.DrawRectangle(np, noteRect.X, noteRect.Y, noteRect.Width, noteRect.Height);
                    g.DrawString("✎", new Font("Segoe UI", 10f * ZoomLevel, FontStyle.Bold), Brushes.Brown, noteRect.X + 3, noteRect.Y + 2);
                }
            }
            else if (ann.AnnotationType == PdfAnnotationType.InkDrawing && ann.InkPoints != null && ann.InkPoints.Count > 1)
            {
                using (Pen inkPen = new Pen(ann.Color, ann.StrokeWidth * ZoomLevel) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    PointF p0 = new PointF(pageBounds.X + ann.InkPoints[0].X * ZoomLevel, pageBounds.Y + ann.InkPoints[0].Y * ZoomLevel);
                    for (int k = 1; k < ann.InkPoints.Count; k++)
                    {
                        PointF pk = new PointF(pageBounds.X + ann.InkPoints[k].X * ZoomLevel, pageBounds.Y + ann.InkPoints[k].Y * ZoomLevel);
                        g.DrawLine(inkPen, p0, pk);
                        p0 = pk;
                    }
                }
            }
            else if (ann.AnnotationType == PdfAnnotationType.Stamp)
            {
                float sx = pageBounds.X + (ann.Bounds.X * ZoomLevel);
                float sy = pageBounds.Y + (ann.Bounds.Y * ZoomLevel);
                float sw = ann.Bounds.Width * ZoomLevel;
                float sh = ann.Bounds.Height * ZoomLevel;
                RectangleF sRect = new RectangleF(sx, sy, sw, sh);

                using (SolidBrush sb = new SolidBrush(Color.FromArgb(28, ann.Color)))
                using (Pen sp = new Pen(ann.Color, 2.2f * ZoomLevel))
                using (Font stampFont = new Font("Segoe UI", 13f * ZoomLevel, FontStyle.Bold))
                using (SolidBrush stxt = new SolidBrush(ann.Color))
                {
                    g.FillRectangle(sb, sRect);
                    g.DrawRectangle(sp, sRect.X, sRect.Y, sRect.Width, sRect.Height);
                    // Đường viền kép bên trong tạo nét con dấu chuẩn công văn
                    using (Pen innerP = new Pen(ann.Color, 1.2f * ZoomLevel))
                    {
                        g.DrawRectangle(innerP, sRect.X + 3.5f * ZoomLevel, sRect.Y + 3.5f * ZoomLevel, Math.Max(2f, sRect.Width - 7f * ZoomLevel), Math.Max(2f, sRect.Height - 7f * ZoomLevel));
                    }
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(ann.StampText, stampFont, stxt, sRect, sf);
                }

                if (SelectedTool == 0 && SelectedAnnotation == ann)
                {
                    using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawRectangle(selPen, sRect.X - 1, sRect.Y - 1, sRect.Width + 2, sRect.Height + 2);
                    }
                    DrawResizeHandles(g, sRect);
                }
            }
        }

        private void DrawShape(Graphics g, RectangleF pageBounds, PdfShapeElement shp)
        {
            if (shp == null) return;
            float sx = pageBounds.X + (shp.Bounds.X * ZoomLevel);
            float sy = pageBounds.Y + (shp.Bounds.Y * ZoomLevel);
            float sw = shp.Bounds.Width * ZoomLevel;
            float sh = shp.Bounds.Height * ZoomLevel;
            RectangleF sRect = new RectangleF(sx, sy, sw, sh);

            if (shp.FillColor != Color.Transparent && shp.FillColor.A > 0)
            {
                using (SolidBrush brush = new SolidBrush(shp.FillColor))
                {
                    if (shp.ShapeType == PdfShapeType.Rectangle) g.FillRectangle(brush, sRect);
                    else if (shp.ShapeType == PdfShapeType.Ellipse) g.FillEllipse(brush, sRect);
                }
            }

            if (shp.StrokeColor != Color.Transparent && shp.StrokeColor.A > 0 && shp.StrokeWidth > 0)
            {
                using (Pen pen = new Pen(shp.StrokeColor, Math.Max(1f, shp.StrokeWidth * ZoomLevel)))
                {
                    if (shp.ShapeType == PdfShapeType.Rectangle) g.DrawRectangle(pen, sRect.X, sRect.Y, sRect.Width, sRect.Height);
                    else if (shp.ShapeType == PdfShapeType.Ellipse) g.DrawEllipse(pen, sRect);
                    else if (shp.ShapeType == PdfShapeType.Line) g.DrawLine(pen, sRect.X, sRect.Y, sRect.Right, sRect.Bottom);
                }
            }

            // Nếu hình khối đang được chọn và đang ở công cụ Con trỏ (Tool 0): Vẽ viền chọn đứt nét màu xanh và 8 chốt vuông co giãn ở 4 góc và 4 cạnh
            if (SelectedTool == 0 && SelectedShape == shp)
            {
                using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(selPen, sRect.X - 1, sRect.Y - 1, sRect.Width + 2, sRect.Height + 2);
                }
                DrawResizeHandles(g, sRect);
            }
        }

        private void DrawFormField(Graphics g, RectangleF pageBounds, PdfFormFieldElement field, bool isEditing)
        {
            if (field == null) return;
            float sx = pageBounds.X + (field.Bounds.X * ZoomLevel);
            float sy = pageBounds.Y + (field.Bounds.Y * ZoomLevel);
            float sw = field.Bounds.Width * ZoomLevel;
            float sh = field.Bounds.Height * ZoomLevel;
            RectangleF r = new RectangleF(sx, sy, sw, sh);

            if (field.FieldType == PdfFormFieldType.CheckBox)
            {
                using (SolidBrush bgB = new SolidBrush(field.BackgroundColor))
                using (Pen bPen = new Pen(field.BorderColor, Math.Max(1f, field.BorderWidth * ZoomLevel)))
                {
                    g.FillRectangle(bgB, r);
                    g.DrawRectangle(bPen, r.X, r.Y, r.Width, r.Height);
                }
                if (field.IsChecked)
                {
                    using (Pen chkPen = new Pen(Color.FromArgb(37, 99, 235), Math.Max(2f, 2.5f * ZoomLevel)))
                    {
                        chkPen.StartCap = LineCap.Round;
                        chkPen.EndCap = LineCap.Round;
                        PointF p1 = new PointF(r.X + r.Width * 0.22f, r.Y + r.Height * 0.52f);
                        PointF p2 = new PointF(r.X + r.Width * 0.44f, r.Y + r.Height * 0.76f);
                        PointF p3 = new PointF(r.X + r.Width * 0.80f, r.Y + r.Height * 0.24f);
                        g.DrawLine(chkPen, p1, p2);
                        g.DrawLine(chkPen, p2, p3);
                    }
                }
            }
            else if (field.FieldType == PdfFormFieldType.RadioButton)
            {
                using (SolidBrush bgB = new SolidBrush(field.BackgroundColor))
                using (Pen bPen = new Pen(field.BorderColor, Math.Max(1f, field.BorderWidth * ZoomLevel)))
                {
                    g.FillEllipse(bgB, r);
                    g.DrawEllipse(bPen, r);
                }
                if (field.IsChecked)
                {
                    float dotInset = r.Width * 0.25f;
                    RectangleF dotR = new RectangleF(r.X + dotInset, r.Y + dotInset, r.Width - 2 * dotInset, r.Height - 2 * dotInset);
                    using (SolidBrush dotB = new SolidBrush(field.TextColor))
                    {
                        g.FillEllipse(dotB, dotR);
                    }
                }
            }
            else if (field.FieldType == PdfFormFieldType.ComboBox)
            {
                Color bgCol = (HighlightFormFields && string.IsNullOrEmpty(field.Value))
                    ? Color.FromArgb(235, 244, 255)
                    : field.BackgroundColor;
                Color borderCol = (HighlightFormFields && string.IsNullOrEmpty(field.Value))
                    ? Color.FromArgb(59, 130, 246)
                    : field.BorderColor;

                using (SolidBrush bgB = new SolidBrush(bgCol))
                using (Pen bPen = new Pen(borderCol, Math.Max(1f, field.BorderWidth * ZoomLevel)))
                {
                    g.FillRectangle(bgB, r);
                    g.DrawRectangle(bPen, r.X, r.Y, r.Width, r.Height);
                }

                // Down arrow
                float arrowX = r.Right - 16 * ZoomLevel;
                float arrowY = r.Y + r.Height / 2f - 2 * ZoomLevel;
                using (SolidBrush aBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    PointF[] arrow = new PointF[]
                    {
                        new PointF(arrowX, arrowY),
                        new PointF(arrowX + 8 * ZoomLevel, arrowY),
                        new PointF(arrowX + 4 * ZoomLevel, arrowY + 5 * ZoomLevel)
                    };
                    g.FillPolygon(aBrush, arrow);
                }

                string displayText = !string.IsNullOrEmpty(field.Value) ? field.Value : (field.Options != null && field.Options.Count > 0 ? field.Options[0] : field.FieldName);
                using (Font f = new Font(field.FontName, Math.Max(7f, field.FontSize * ZoomLevel)))
                using (SolidBrush tB = new SolidBrush(string.IsNullOrEmpty(field.Value) ? Color.Gray : field.TextColor))
                {
                    RectangleF textR = new RectangleF(r.X + 4 * ZoomLevel, r.Y + 2 * ZoomLevel, r.Width - 22 * ZoomLevel, r.Height - 4 * ZoomLevel);
                    g.DrawString(displayText, f, tB, textR);
                }
            }
            else // Text or Button
            {
                Color bgCol = (HighlightFormFields && string.IsNullOrEmpty(field.Value) && !isEditing)
                    ? Color.FromArgb(235, 244, 255)
                    : field.BackgroundColor;
                Color borderCol = (HighlightFormFields && string.IsNullOrEmpty(field.Value) && !isEditing)
                    ? Color.FromArgb(59, 130, 246)
                    : field.BorderColor;

                using (SolidBrush bgB = new SolidBrush(bgCol))
                using (Pen bPen = new Pen(borderCol, Math.Max(1f, field.BorderWidth * ZoomLevel)))
                {
                    g.FillRectangle(bgB, r);
                    g.DrawRectangle(bPen, r.X, r.Y, r.Width, r.Height);
                }

                string displayText = isEditing ? EditingFormFieldBuffer : (!string.IsNullOrEmpty(field.Value) ? field.Value : field.DefaultValue);
                if (string.IsNullOrEmpty(displayText) && !isEditing)
                {
                    displayText = field.FieldName;
                }

                using (Font f = new Font(field.FontName, Math.Max(7f, field.FontSize * ZoomLevel)))
                using (SolidBrush tB = new SolidBrush((string.IsNullOrEmpty(field.Value) && !isEditing) ? Color.FromArgb(148, 163, 184) : field.TextColor))
                {
                    RectangleF textR = new RectangleF(r.X + 4 * ZoomLevel, r.Y + 2 * ZoomLevel, r.Width - 8 * ZoomLevel, r.Height - 4 * ZoomLevel);
                    g.DrawString(displayText, f, tB, textR);

                    if (isEditing && ShowCursor)
                    {
                        SizeF sz = g.MeasureString(displayText ?? "", f);
                        float cursorX = Math.Min(textR.Right - 2, textR.X + sz.Width);
                        using (Pen curPen = new Pen(Color.FromArgb(15, 23, 42), 1.5f))
                        {
                            g.DrawLine(curPen, cursorX, textR.Y + 1, cursorX, textR.Y + textR.Height - 2);
                        }
                    }
                }
            }
        }

        private void DrawRedaction(Graphics g, RectangleF pageBounds, PdfRedactionElement red)
        {
            if (red == null) return;
            float sx = pageBounds.X + (red.Bounds.X * ZoomLevel);
            float sy = pageBounds.Y + (red.Bounds.Y * ZoomLevel);
            float sw = red.Bounds.Width * ZoomLevel;
            float sh = red.Bounds.Height * ZoomLevel;
            RectangleF r = new RectangleF(sx, sy, sw, sh);

            if (red.IsApplied)
            {
                using (SolidBrush b = new SolidBrush(red.FillColor))
                {
                    g.FillRectangle(b, r);
                }
                if (!string.IsNullOrEmpty(red.OverlayText))
                {
                    using (Font f = new Font("Segoe UI", Math.Max(8f, 10f * ZoomLevel), FontStyle.Bold))
                    using (SolidBrush tb = new SolidBrush(red.TextColor))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString(red.OverlayText, f, tb, r, sf);
                    }
                }
            }
            else
            {
                // Marked state preview
                using (SolidBrush transRed = new SolidBrush(Color.FromArgb(45, 239, 68, 68)))
                {
                    g.FillRectangle(transRed, r);
                }
                using (HatchBrush hb = new HatchBrush(HatchStyle.ForwardDiagonal, Color.FromArgb(140, 239, 68, 68), Color.Transparent))
                {
                    g.FillRectangle(hb, r);
                }
                using (Pen p = new Pen(Color.FromArgb(239, 68, 68), Math.Max(1.5f, 2f * ZoomLevel)) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
                }

                // Badge overlay
                string badge = string.IsNullOrEmpty(red.OverlayText) ? "ĐÃ ĐÁNH DẤU XÓA MẬT" : $"ĐÃ ĐÁNH DẤU: {red.OverlayText}";
                using (Font badgeFont = new Font("Segoe UI", Math.Max(7f, 8f * ZoomLevel), FontStyle.Bold))
                using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(220, 38, 38)))
                using (SolidBrush badgeFg = new SolidBrush(Color.White))
                {
                    SizeF bSz = g.MeasureString(badge, badgeFont);
                    RectangleF badgeRect = new RectangleF(r.X, Math.Max(pageBounds.Y, r.Y - bSz.Height - 2), bSz.Width + 6, bSz.Height + 2);
                    g.FillRectangle(badgeBg, badgeRect);
                    g.DrawString(badge, badgeFont, badgeFg, badgeRect.X + 3, badgeRect.Y + 1);
                }
            }
        }

        private void DrawBatesNumber(Graphics g, RectangleF pageBounds, PdfPageModel page)
        {
            if (Document == null || Document.ActiveBatesConfig == null || string.IsNullOrEmpty(page.BatesNumber)) return;
            var cfg = Document.ActiveBatesConfig;

            using (Font bf = new Font(cfg.FontName, Math.Max(7f, cfg.FontSize * ZoomLevel), FontStyle.Regular))
            using (SolidBrush bb = new SolidBrush(cfg.TextColor))
            {
                SizeF sz = g.MeasureString(page.BatesNumber, bf);
                float mx = cfg.MarginX * ZoomLevel;
                float my = cfg.MarginY * ZoomLevel;
                float bx = pageBounds.X + mx;
                float by = pageBounds.Y + my;

                switch (cfg.Position)
                {
                    case PdfBatesPosition.TopLeft:
                        bx = pageBounds.X + mx;
                        by = pageBounds.Y + my;
                        break;
                    case PdfBatesPosition.TopCenter:
                        bx = pageBounds.X + (pageBounds.Width - sz.Width) / 2f;
                        by = pageBounds.Y + my;
                        break;
                    case PdfBatesPosition.TopRight:
                        bx = pageBounds.Right - mx - sz.Width;
                        by = pageBounds.Y + my;
                        break;
                    case PdfBatesPosition.BottomLeft:
                        bx = pageBounds.X + mx;
                        by = pageBounds.Bottom - my - sz.Height;
                        break;
                    case PdfBatesPosition.BottomCenter:
                        bx = pageBounds.X + (pageBounds.Width - sz.Width) / 2f;
                        by = pageBounds.Bottom - my - sz.Height;
                        break;
                    case PdfBatesPosition.BottomRight:
                        bx = pageBounds.Right - mx - sz.Width;
                        by = pageBounds.Bottom - my - sz.Height;
                        break;
                }

                g.DrawString(page.BatesNumber, bf, bb, bx, by);
            }
        }

        private void DrawBookmarksSidebar(Graphics g, int width, int height, int topOffset, int viewH)
        {
            Rectangle sideListRect = new Rectangle(0, topOffset + 36, SidebarWidth - 8, viewH - 36);
            GraphicsState sState = g.Save();
            g.SetClip(sideListRect);

            // Button Thêm dấu trang (+ Thêm Dấu Trang)
            Rectangle btnAddRect = new Rectangle(12, topOffset + 44, SidebarWidth - 32, 28);
            BtnAddBookmarkRect = btnAddRect;
            using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(238, 242, 255)))
            using (Pen btnBorder = new Pen(Color.FromArgb(99, 102, 241), 1f))
            using (Font btnFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush btnTxt = new SolidBrush(Color.FromArgb(79, 70, 229)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.FillRectangle(btnBg, btnAddRect);
                g.DrawRectangle(btnBorder, btnAddRect);
                g.DrawString("🔖 + Thêm Dấu Trang", btnFont, btnTxt, btnAddRect, sf);
            }

            int curY = topOffset + 80;
            if (Document != null && Document.Bookmarks != null && Document.Bookmarks.Count > 0)
            {
                DrawBookmarkNodesRecursive(g, Document.Bookmarks, 0, ref curY, topOffset, viewH);
            }
            else
            {
                using (Font hintFont = new Font("Segoe UI", 8.5f, FontStyle.Italic))
                using (SolidBrush hintBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    Rectangle hintRect = new Rectangle(12, curY + 20, SidebarWidth - 32, 60);
                    g.DrawString("Chưa có dấu trang mục lục.\nNhấp '+ Thêm Dấu Trang' để tạo.", hintFont, hintBrush, hintRect, sf);
                }
            }

            g.Restore(sState);
        }

        private void DrawBookmarkNodesRecursive(Graphics g, List<PdfBookmarkNode> nodes, int indent, ref int curY, int topOffset, int viewH)
        {
            if (nodes == null) return;
            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (Font pageFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush titleB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush pageB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                foreach (var node in nodes)
                {
                    if (curY >= topOffset + 36 && curY <= topOffset + viewH)
                    {
                        Rectangle itemRect = new Rectangle(10 + indent * 16, curY, SidebarWidth - 28 - indent * 16, 26);
                        bool isSel = (SelectedBookmark == node);
                        if (isSel)
                        {
                            using (SolidBrush selB = new SolidBrush(Color.FromArgb(224, 231, 255)))
                            {
                                g.FillRectangle(selB, itemRect);
                            }
                        }

                        // Bookmark icon
                        g.DrawString("🔖", titleFont, Brushes.Navy, itemRect.X, itemRect.Y + 2);
                        // Title
                        g.DrawString(node.Title, titleFont, titleB, itemRect.X + 20, itemRect.Y + 3);
                        // Page pill
                        string pageStr = $"Trang {node.TargetPageIndex + 1}";
                        g.DrawString(pageStr, pageFont, pageB, itemRect.Right - 50, itemRect.Y + 5);
                    }
                    curY += 28;

                    if (node.Children != null && node.Children.Count > 0 && node.IsExpanded)
                    {
                        DrawBookmarkNodesRecursive(g, node.Children, indent + 1, ref curY, topOffset, viewH);
                    }
                }
            }
        }

        private PdfBookmarkNode HitTestBookmarkNode(List<PdfBookmarkNode> nodes, int x, int y, int indent, ref int curY)
        {
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                Rectangle itemRect = new Rectangle(10 + indent * 16, curY, SidebarWidth - 28 - indent * 16, 26);
                if (itemRect.Contains(x, y)) return node;
                curY += 28;
                if (node.Children != null && node.Children.Count > 0 && node.IsExpanded)
                {
                    var found = HitTestBookmarkNode(node.Children, x, y, indent + 1, ref curY);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public static SizeF CalculateSignatureAutoBounds(string signerName, string signDate, string reason, Image sigImage)
        {
            using (Bitmap tmpBmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(tmpBmp))
            using (Font titleF = new Font("Segoe UI", 9.8f, FontStyle.Bold))
            using (Font detailF = new Font("Segoe UI", 8.2f, FontStyle.Regular))
            {
                string titleStr = "ĐÃ KÝ ĐIỆN TỬ BỞI: " + (signerName ?? "");
                string dateStr = "Thời gian: " + (signDate ?? "");
                string reasonStr = "Mục đích: " + (reason ?? "");

                SizeF szTitle = g.MeasureString(titleStr, titleF);
                SizeF szDate = g.MeasureString(dateStr, detailF);
                SizeF szReason = g.MeasureString(reasonStr, detailF);

                float maxTxtW = Math.Max(szTitle.Width, Math.Max(szDate.Width, szReason.Width));
                float imgW = (sigImage != null) ? 68f : 0f;
                // Cột bên trái: Biểu tượng khiên 24px + lề 8px + khoảng cách 6px = 38px
                // Đệm an toàn bên phải: 22px để chữ không bao giờ chạm hoặc vượt khung
                float autoW = 42f + maxTxtW + imgW + 24f;
                float autoH = Math.Max(64f, 8f + szTitle.Height + 4f + szDate.Height + 4f + szReason.Height + 10f);

                return new SizeF((float)Math.Ceiling(Math.Max(180f, autoW)), (float)Math.Ceiling(autoH));
            }
        }

        private void DrawSignature(Graphics g, RectangleF pageBounds, PdfSignatureElement sig)
        {
            if (sig == null) return;

            // Tự động điều chỉnh kích thước khung theo nội dung chữ nếu khung chưa đủ rộng
            SizeF reqSize = CalculateSignatureAutoBounds(sig.SignerName, sig.SignDate, sig.Reason, sig.SignatureImage);
            if (sig.Bounds.Width < reqSize.Width || sig.Bounds.Height < reqSize.Height)
            {
                sig.Bounds = new RectangleF(
                    sig.Bounds.X, 
                    sig.Bounds.Y, 
                    Math.Max(sig.Bounds.Width, reqSize.Width), 
                    Math.Max(sig.Bounds.Height, reqSize.Height)
                );
            }

            float sx = pageBounds.X + (sig.Bounds.X * ZoomLevel);
            float sy = pageBounds.Y + (sig.Bounds.Y * ZoomLevel);
            float sw = sig.Bounds.Width * ZoomLevel;
            float sh = sig.Bounds.Height * ZoomLevel;
            RectangleF sRect = new RectangleF(sx, sy, sw, sh);

            Color certColor = sig.SignatureColor != Color.Empty ? sig.SignatureColor : Color.FromArgb(30, 58, 138);

            // Khung chứng thực chữ ký điện tử sắc nét
            using (SolidBrush sigBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen sigBorder = new Pen(certColor, 1.8f * ZoomLevel))
            {
                g.FillRectangle(sigBg, sRect);
                g.DrawRectangle(sigBorder, sRect.X, sRect.Y, sRect.Width, sRect.Height);
            }

            // Biểu tượng con dấu / huy hiệu chứng nhận
            DrawVectorShieldCheck(g, sRect.X + 8 * ZoomLevel, sRect.Y + 8 * ZoomLevel, 24 * ZoomLevel, certColor);

            // Ảnh chữ ký cá nhân hoặc nét vẽ ký tay (nếu có đính kèm)
            float txtLeft = sRect.X + 38 * ZoomLevel;
            if (sig.SignatureImage != null)
            {
                float imgW = Math.Min(90 * ZoomLevel, sRect.Width * 0.40f);
                float imgH = Math.Max(10f, sRect.Height - 14 * ZoomLevel);
                RectangleF imgR = new RectangleF(sRect.Right - imgW - 8 * ZoomLevel, sRect.Y + 7 * ZoomLevel, imgW, imgH);
                g.DrawImage(sig.SignatureImage, imgR);
            }
            else if (sig.SignatureStrokes != null && sig.SignatureStrokes.Count > 0)
            {
                float imgW = Math.Min(90 * ZoomLevel, sRect.Width * 0.40f);
                float imgH = Math.Max(10f, sRect.Height - 14 * ZoomLevel);
                RectangleF imgR = new RectangleF(sRect.Right - imgW - 8 * ZoomLevel, sRect.Y + 7 * ZoomLevel, imgW, imgH);

                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var strk in sig.SignatureStrokes)
                {
                    foreach (var pt in strk)
                    {
                        if (pt.X < minX) minX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y > maxY) maxY = pt.Y;
                    }
                }
                float strokeW = maxX - minX;
                float strokeH = maxY - minY;
                if (strokeW > 0 && strokeH > 0)
                {
                    float scale = Math.Min((imgR.Width - 4) / strokeW, (imgR.Height - 4) / strokeH);
                    using (Pen inkPen = new Pen(certColor, 2f * ZoomLevel) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        foreach (var strk in sig.SignatureStrokes)
                        {
                            if (strk.Count > 1)
                            {
                                PointF[] pts = new PointF[strk.Count];
                                for (int p = 0; p < strk.Count; p++)
                                {
                                    pts[p] = new PointF(imgR.X + 2 + (strk[p].X - minX) * scale, imgR.Y + 2 + (strk[p].Y - minY) * scale);
                                }
                                g.DrawLines(inkPen, pts);
                            }
                        }
                    }
                }
            }

            using (Font titleF = new Font("Segoe UI", 9.8f * ZoomLevel, FontStyle.Bold))
            using (Font detailF = new Font("Segoe UI", 8.2f * ZoomLevel, FontStyle.Regular))
            using (SolidBrush titleTxt = new SolidBrush(certColor))
            using (SolidBrush grayTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawString("ĐÃ KÝ ĐIỆN TỬ BỞI: " + sig.SignerName, titleF, titleTxt, txtLeft, sRect.Y + 8 * ZoomLevel);
                g.DrawString("Thời gian: " + sig.SignDate, detailF, grayTxt, txtLeft, sRect.Y + 28 * ZoomLevel);
                g.DrawString("Mục đích: " + sig.Reason, detailF, grayTxt, txtLeft, sRect.Y + 45 * ZoomLevel);
            }

            // Nếu chữ ký đang được chọn và đang ở công cụ Con trỏ: Vẽ 8 chốt vuông co giãn kích thước
            if (SelectedTool == 0 && SelectedSignature == sig)
            {
                using (Pen selPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(selPen, sRect.X - 1, sRect.Y - 1, sRect.Width + 2, sRect.Height + 2);
                }
                DrawResizeHandles(g, sRect);
            }
        }

        private void DrawVectorShieldCheck(Graphics g, float x, float y, float size, Color color)
        {
            float s = size / 24f;
            using (Pen p = new Pen(color, 2.0f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (SolidBrush b = new SolidBrush(Color.FromArgb(40, color)))
            {
                PointF[] shield = new PointF[] {
                    new PointF(x + 12 * s, y + 2 * s),
                    new PointF(x + 21 * s, y + 5 * s),
                    new PointF(x + 21 * s, y + 12 * s),
                    new PointF(x + 12 * s, y + 21 * s),
                    new PointF(x + 3 * s, y + 12 * s),
                    new PointF(x + 3 * s, y + 5 * s)
                };
                g.FillPolygon(b, shield);
                g.DrawPolygon(p, shield);

                // Dấu tích Checkmark
                g.DrawLine(p, x + 8 * s, y + 12 * s, x + 11 * s, y + 15 * s);
                g.DrawLine(p, x + 11 * s, y + 15 * s, x + 16 * s, y + 8 * s);
            }
        }

        private void DrawSearchHighlights(Graphics g, RectangleF pageBounds, PdfPageModel page)
        {
            if (string.IsNullOrEmpty(SearchQuery)) return;
            string qLower = SearchQuery.ToLower();

            // 1. Tô màu danh sách SearchMatches nếu có
            if (SearchMatches != null && SearchMatches.Count > 0)
            {
                for (int mIdx = 0; mIdx < SearchMatches.Count; mIdx++)
                {
                    var m = SearchMatches[mIdx];
                    if (m.PageIndex == page.PageNumber - 1)
                    {
                        float tx = pageBounds.X + (m.Bounds.X * ZoomLevel);
                        float ty = pageBounds.Y + (m.Bounds.Y * ZoomLevel);
                        float tw = m.Bounds.Width * ZoomLevel;
                        float th = m.Bounds.Height * ZoomLevel;
                        RectangleF mRect = new RectangleF(tx, ty, tw, th);

                        bool isCurrent = (mIdx == CurrentSearchIndex);
                        Color fillC = isCurrent ? Color.FromArgb(210, 249, 115, 22) : Color.FromArgb(160, 250, 204, 21);
                        Color borderC = isCurrent ? Color.FromArgb(194, 65, 12) : Color.FromArgb(234, 179, 8);

                        using (SolidBrush matchBrush = new SolidBrush(fillC))
                        using (Pen matchPen = new Pen(borderC, isCurrent ? 2.2f : 1.2f))
                        {
                            g.FillRectangle(matchBrush, mRect);
                            g.DrawRectangle(matchPen, mRect.X, mRect.Y, mRect.Width, mRect.Height);
                        }
                    }
                }
                return;
            }

            // 2. Fallback trực tiếp trên page.TextElements
            using (SolidBrush matchBrush = new SolidBrush(Color.FromArgb(160, 250, 204, 21)))
            using (Pen matchPen = new Pen(Color.FromArgb(234, 179, 8), 1.2f))
            {
                foreach (var txt in page.TextElements)
                {
                    if (txt.Text != null && txt.Text.ToLower().Contains(qLower))
                    {
                        float tx = pageBounds.X + (txt.Bounds.X * ZoomLevel);
                        float ty = pageBounds.Y + (txt.Bounds.Y * ZoomLevel);
                        float tw = txt.Bounds.Width * ZoomLevel;
                        float th = txt.Bounds.Height * ZoomLevel;
                        RectangleF mRect = new RectangleF(tx, ty, tw, th);

                        g.FillRectangle(matchBrush, mRect);
                        g.DrawRectangle(matchPen, mRect.X, mRect.Y, mRect.Width, mRect.Height);
                    }
                }
            }
        }

        private void DrawFloatingSearchBar(Graphics g, int width, int topOffset)
        {
            Rectangle barRect = new Rectangle(width - 360, topOffset + 12, 340, 42);
            using (GraphicsPath bp = LiquidGlassHelper.CreateRoundedRectanglePath(new RectangleF(barRect.X, barRect.Y, barRect.Width, barRect.Height), 12f))
            using (SolidBrush bBg = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
            using (Pen bPen = new Pen(IsSearchInputFocused ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225), 1.5f))
            {
                g.FillPath(bBg, bp);
                g.DrawPath(bPen, bp);
            }

            using (Font sFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (Font countFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font btnFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                // Biểu tượng kính lúp
                g.DrawString("🔍", sFont, Brushes.Gray, barRect.X + 8, barRect.Y + 11);

                // Khung nhập từ khóa tìm kiếm
                Rectangle inputRect = new Rectangle(barRect.X + 34, barRect.Y + 11, 160, 20);
                string displayTxt = string.IsNullOrEmpty(SearchQuery) ? "Tìm kiếm trong PDF..." : SearchQuery;
                Brush txtBrush = string.IsNullOrEmpty(SearchQuery) ? Brushes.Gray : Brushes.Black;
                g.DrawString(displayTxt, sFont, txtBrush, inputRect.X, inputRect.Y);

                // Con trỏ nhấp nháy khi đang tập trung gõ
                if (IsSearchInputFocused && ShowCursor)
                {
                    SizeF qSz = g.MeasureString(displayTxt, sFont);
                    float curX = inputRect.X + (string.IsNullOrEmpty(SearchQuery) ? 0 : qSz.Width - 4);
                    using (Pen cPen = new Pen(Color.Black, 1.5f))
                    {
                        g.DrawLine(cPen, curX, inputRect.Y + 1, curX, inputRect.Y + 18);
                    }
                }

                // Nhãn số kết quả tìm kiếm
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    string matchStr = SearchMatchCount > 0
                        ? string.Format("{0}/{1}", CurrentSearchIndex + 1, SearchMatchCount)
                        : "0";
                    Brush cBrush = SearchMatchCount > 0 ? Brushes.DarkSlateBlue : Brushes.Crimson;
                    g.DrawString(matchStr, countFont, cBrush, barRect.Right - 122, barRect.Y + 13);
                }

                // Nút tìm kết quả trước ▲
                Rectangle prevBtn = new Rectangle(barRect.Right - 82, barRect.Y + 8, 22, 24);
                using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillRectangle(btnBg, prevBtn);
                    g.DrawRectangle(Pens.LightGray, prevBtn);
                    g.DrawString("▲", new Font("Segoe UI", 7.5f, FontStyle.Bold), Brushes.DimGray, prevBtn.X + 4, prevBtn.Y + 5);
                }

                // Nút tìm kết quả sau ▼
                Rectangle nextBtn = new Rectangle(barRect.Right - 56, barRect.Y + 8, 22, 24);
                using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillRectangle(btnBg, nextBtn);
                    g.DrawRectangle(Pens.LightGray, nextBtn);
                    g.DrawString("▼", new Font("Segoe UI", 7.5f, FontStyle.Bold), Brushes.DimGray, nextBtn.X + 4, nextBtn.Y + 5);
                }

                // Nút đóng tìm kiếm ✕ (Close Button)
                Rectangle closeBtn = new Rectangle(barRect.Right - 30, barRect.Y + 8, 22, 24);
                using (SolidBrush cBg = new SolidBrush(Color.FromArgb(254, 226, 226)))
                {
                    g.FillRectangle(cBg, closeBtn);
                    g.DrawRectangle(Pens.LightPink, closeBtn);
                    g.DrawString("✕", btnFont, Brushes.Crimson, closeBtn.X + 4, closeBtn.Y + 3);
                }
            }
        }
        #endregion

        #region Floating Liquid Glass Color Palette (Thanh chọn màu khung bầu Liquid Glass góc dưới)
        public static readonly Color[] LiquidSwatches = new Color[] {
            Color.FromArgb(239, 68, 68),  // Đỏ tươi
            Color.FromArgb(59, 130, 246),  // Xanh dương
            Color.FromArgb(16, 185, 129),  // Xanh lá
            Color.FromArgb(245, 158, 11),  // Vàng nghệ
            Color.FromArgb(139, 92, 246),  // Tím
            Color.FromArgb(15, 23, 42)     // Đen than
        };

        public RectangleF GetLiquidGlassPaletteRect(int width, int height, int statusH = 26)
        {
            int pillW = 346;
            int pillH = 46;
            int leftMargin = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int availW = width - leftMargin;
            float pillX = leftMargin + (availW - pillW) / 2f;
            float pillY = height - statusH - pillH - 16;
            return new RectangleF(pillX, pillY, pillW, pillH);
        }

        private void DrawLiquidGlassPalette(Graphics g, int width, int height, int statusH = 26)
        {
            RectangleF r = GetLiquidGlassPaletteRect(width, height, statusH);
            float cornerRadius = 23f; // Khung bầu tròn hai đầu (capsule pill shape)

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Lớp đổ bóng mềm Liquid Shadow (Ambient drop shadow)
            using (GraphicsPath shadowPath = CreateRoundedRectPath(new RectangleF(r.X, r.Y + 3, r.Width, r.Height), cornerRadius))
            using (PathGradientBrush pgb = new PathGradientBrush(shadowPath))
            {
                pgb.CenterColor = Color.FromArgb(45, 15, 23, 42);
                pgb.SurroundColors = new Color[] { Color.Transparent };
                g.FillPath(pgb, shadowPath);
            }

            // 2. Lớp kính lỏng bán trong suốt (Frosted Liquid Glass Gradient)
            using (GraphicsPath glassPath = CreateRoundedRectPath(r, cornerRadius))
            {
                using (LinearGradientBrush glassBrush = new LinearGradientBrush(
                    r,
                    Color.FromArgb(235, 255, 255, 255), // Trắng trong mờ phía trên
                    Color.FromArgb(195, 241, 245, 249), // Sáng bóng xanh mờ phía dưới
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(glassBrush, glassPath);
                }

                // Vệt phản chiếu ánh sáng trên vòm kính (Glass Specular Gloss Sheen)
                RectangleF sheenRect = new RectangleF(r.X + 8, r.Y + 2, r.Width - 16, r.Height * 0.45f);
                using (GraphicsPath sheenPath = CreateRoundedRectPath(sheenRect, 14f))
                using (LinearGradientBrush sheenBrush = new LinearGradientBrush(
                    sheenRect,
                    Color.FromArgb(160, 255, 255, 255),
                    Color.FromArgb(20, 255, 255, 255),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(sheenBrush, sheenPath);
                }

                // 3. Viền kính phát sáng khúc xạ (Liquid Glass Refraction Border)
                using (Pen borderPen = new Pen(Color.FromArgb(220, 255, 255, 255), 1.4f))
                {
                    g.DrawPath(borderPen, glassPath);
                }
            }

            // 4. Biểu tượng công cụ đang chọn (Cây bút ✏️ hoặc Khung vuông 🔲)
            float iconX = r.X + 14;
            float iconY = r.Y + 11;
            if (SelectedTool == 7) // Cây bút (Bút vẽ tay)
            {
                using (Pen pPen = new Pen(Color.FromArgb(71, 85, 105), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                using (SolidBrush tipB = new SolidBrush(ActiveColor))
                {
                    g.DrawLine(pPen, iconX + 4, iconY + 18, iconX + 16, iconY + 6);
                    g.DrawLine(pPen, iconX + 16, iconY + 6, iconX + 20, iconY + 10);
                    g.DrawLine(pPen, iconX + 20, iconY + 10, iconX + 8, iconY + 22);
                    g.DrawLine(pPen, iconX + 8, iconY + 22, iconX + 4, iconY + 18);
                    PointF[] tip = new PointF[] {
                        new PointF(iconX + 4, iconY + 18),
                        new PointF(iconX + 1, iconY + 23),
                        new PointF(iconX + 8, iconY + 22)
                    };
                    g.FillPolygon(tipB, tip);
                }
            }
            else // Khung vuông
            {
                using (Pen rectP = new Pen(ActiveColor, 2f))
                using (SolidBrush fillB = new SolidBrush(Color.FromArgb(40, ActiveColor)))
                {
                    g.FillRectangle(fillB, iconX + 3, iconY + 4, 18, 16);
                    g.DrawRectangle(rectP, iconX + 3, iconY + 4, 18, 16);
                }
            }

            // Vạch phân cách mờ (Divider)
            using (Pen divPen = new Pen(Color.FromArgb(120, 203, 213, 225), 1f))
            {
                g.DrawLine(divPen, r.X + 44, r.Y + 10, r.X + 44, r.Y + 36);
            }

            // 5. Dãy các ô chấm màu tròn phổ biến
            for (int i = 0; i < LiquidSwatches.Length; i++)
            {
                float dotX = r.X + 54 + (i * 28);
                float dotY = r.Y + 12;
                Color col = LiquidSwatches[i];

                using (SolidBrush dotShadow = new SolidBrush(Color.FromArgb(40, col)))
                {
                    g.FillEllipse(dotShadow, dotX, dotY + 1.5f, 22, 22);
                }

                using (SolidBrush dotB = new SolidBrush(col))
                {
                    g.FillEllipse(dotB, dotX, dotY, 22, 22);
                }

                using (Pen dotRim = new Pen(Color.FromArgb(100, 255, 255, 255), 1f))
                {
                    g.DrawEllipse(dotRim, dotX + 1, dotY + 1, 20, 20);
                }

                if (ActiveColor.ToArgb() == col.ToArgb())
                {
                    using (Pen activeRing = new Pen(Color.FromArgb(37, 99, 235), 2.2f))
                    using (Pen whiteGap = new Pen(Color.White, 1.2f))
                    {
                        g.DrawEllipse(whiteGap, dotX - 2, dotY - 2, 26, 26);
                        g.DrawEllipse(activeRing, dotX - 3.5f, dotY - 3.5f, 29, 29);
                    }
                }
            }

            // 6. Nút Bảng Màu Tùy Biến (Custom Color Palette - 4 chấm đa sắc)
            float moreBtnX = r.X + 54 + (LiquidSwatches.Length * 28) + 4;
            float moreBtnY = r.Y + 12;
            using (SolidBrush moreBg = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
            using (Pen moreBorder = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
            {
                g.FillEllipse(moreBg, moreBtnX, moreBtnY, 22, 22);
                g.DrawEllipse(moreBorder, moreBtnX, moreBtnY, 22, 22);

                g.FillEllipse(Brushes.Crimson, moreBtnX + 4, moreBtnY + 4, 6, 6);
                g.FillEllipse(Brushes.DodgerBlue, moreBtnX + 12, moreBtnY + 4, 6, 6);
                g.FillEllipse(Brushes.Gold, moreBtnX + 4, moreBtnY + 12, 6, 6);
                g.FillEllipse(Brushes.MediumPurple, moreBtnX + 12, moreBtnY + 12, 6, 6);
            }

            // Vạch phân cách thứ 2
            using (Pen divPen = new Pen(Color.FromArgb(120, 203, 213, 225), 1f))
            {
                g.DrawLine(divPen, moreBtnX + 30, r.Y + 10, moreBtnX + 30, r.Y + 36);
            }

            // 7. Chọn cỡ nét bút (Nét mỏng 2.5pt vs Nét đậm 5pt)
            float s1X = moreBtnX + 38;
            bool isS1 = (ActiveStrokeWidth <= 3f);
            using (SolidBrush s1Bg = new SolidBrush(isS1 ? Color.FromArgb(219, 234, 254) : Color.Transparent))
            using (Pen s1P = new Pen(isS1 ? Color.FromArgb(37, 99, 235) : Color.FromArgb(148, 163, 184), 1.2f))
            {
                g.FillEllipse(s1Bg, s1X, r.Y + 13, 20, 20);
                if (isS1) g.DrawEllipse(s1P, s1X, r.Y + 13, 20, 20);
                using (SolidBrush dotPenB = new SolidBrush(isS1 ? Color.FromArgb(30, 58, 138) : Color.FromArgb(100, 116, 139)))
                {
                    g.FillEllipse(dotPenB, s1X + 8, r.Y + 21, 4, 4);
                }
            }

            float s2X = s1X + 24;
            bool isS2 = (ActiveStrokeWidth > 3f);
            using (SolidBrush s2Bg = new SolidBrush(isS2 ? Color.FromArgb(219, 234, 254) : Color.Transparent))
            using (Pen s2P = new Pen(isS2 ? Color.FromArgb(37, 99, 235) : Color.FromArgb(148, 163, 184), 1.2f))
            {
                g.FillEllipse(s2Bg, s2X, r.Y + 13, 20, 20);
                if (isS2) g.DrawEllipse(s2P, s2X, r.Y + 13, 20, 20);
                using (SolidBrush dotPenB = new SolidBrush(isS2 ? Color.FromArgb(30, 58, 138) : Color.FromArgb(100, 116, 139)))
                {
                    g.FillEllipse(dotPenB, s2X + 6, r.Y + 19, 8, 8);
                }
            }
        }

        private GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            if (rect.Width < diameter) diameter = rect.Width;
            if (rect.Height < diameter) diameter = rect.Height;

            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
        #endregion

        #region PDF Text Auto Detection & Smart Extraction
        public int AutoDetectTextForPage(int pageIdx = -1)
        {
            if (Document == null) return 0;
            ZenQOffice.Format.PdfHandler.AutoDetectAndExtractText(Document, pageIdx);
            InvalidateAllCaches();
            if (pageIdx >= 0 && pageIdx < Document.Pages.Count)
                return Document.Pages[pageIdx].TextElements.Count;
            int total = 0;
            for (int i = 0; i < Document.Pages.Count; i++) total += Document.Pages[i].TextElements.Count;
            return total;
        }

        public bool HandleDoubleClick(int x, int y, int width, int height, int topOffset = 155)
        {
            if (Document == null || ActivePageIndex < 0 || ActivePageIndex >= Document.Pages.Count) return false;
            int pageIdx = GetPageIndexAtPoint(x, y, width, height, topOffset);
            if (pageIdx >= 0)
            {
                ActivePageIndex = pageIdx;
                PointF pagePt = ScreenToPagePoint(new PointF(x, y), pageIdx, width, height, topOffset);
                var page = Document.Pages[pageIdx];
                if (page.TextElements.Count == 0 && Document.UnderlyingPdfiumDoc != null)
                {
                    AutoDetectTextForPage(pageIdx);
                }
                for (int t = page.TextElements.Count - 1; t >= 0; t--)
                {
                    var targetTxt = page.TextElements[t];
                    if (targetTxt.Bounds.Contains(pagePt))
                    {
                        // Chữ tự thêm sau (!targetTxt.IsDetectedFromPdf): cho phép tự sửa trực tiếp
                        // Chữ có sẵn từ PDF gốc: CHỈ cho sửa khi đang bật nút "Sửa chữ" (SelectedTool == 2)
                        bool canEdit = (!targetTxt.IsDetectedFromPdf) || (SelectedTool == 2);
                        if (canEdit)
                        {
                            StartInlineEdit(pageIdx, targetTxt, x, width, height);
                            return true;
                        }
                    }
                }
                for (int s = page.Signatures.Count - 1; s >= 0; s--)
                {
                    var sig = page.Signatures[s];
                    if (sig.Bounds.Contains(pagePt))
                    {
                        using (var dlg = new PdfSignatureInputDialog(sig.SignerName, sig.Reason, sig.SignatureColor))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK)
                            {
                                PushUndoState("Sửa chữ ký điện tử");
                                sig.SignerName = dlg.SignerName;
                                sig.SignDate = dlg.SignDate;
                                sig.Reason = dlg.Reason;
                                sig.SignatureColor = dlg.SignatureColor;
                                if (dlg.SignatureImage != null) sig.SignatureImage = dlg.SignatureImage;
                                if (dlg.SignatureStrokes != null) sig.SignatureStrokes = new List<List<PointF>>(dlg.SignatureStrokes);
                                page.InvalidateCache();
                                Document.IsModified = true;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region User Interaction Handlers
        public bool HandleMouseDown(int x, int y, int width, int height, MouseButtons button, Form parentForm)
        {
            int topOffset = 155;
            int viewH = height - topOffset - 26;

            // 0. Kiểm tra nhấp trên Floating Search Bar
            if (ShowSearchBar)
            {
                Rectangle barRect = new Rectangle(width - 360, topOffset + 12, 340, 42);
                if (barRect.Contains(x, y))
                {
                    // Nút Đóng (✕)
                    Rectangle closeBtn = new Rectangle(barRect.Right - 30, barRect.Y + 8, 22, 24);
                    if (closeBtn.Contains(x, y))
                    {
                        ShowSearchBar = false;
                        IsSearchInputFocused = false;
                        SearchQuery = "";
                        SearchMatchCount = 0;
                        if (SearchMatches != null) SearchMatches.Clear();
                        InvalidateAllCaches();
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    // Nút Trước (▲)
                    Rectangle prevBtn = new Rectangle(barRect.Right - 82, barRect.Y + 8, 22, 24);
                    if (prevBtn.Contains(x, y))
                    {
                        NavigateSearchMatch(-1, width, height, topOffset);
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    // Nút Sau (▼)
                    Rectangle nextBtn = new Rectangle(barRect.Right - 56, barRect.Y + 8, 22, 24);
                    if (nextBtn.Contains(x, y))
                    {
                        NavigateSearchMatch(1, width, height, topOffset);
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    // Ô nhập
                    Rectangle inputRect = new Rectangle(barRect.X + 34, barRect.Y + 6, 175, 30);
                    if (inputRect.Contains(x, y))
                    {
                        IsSearchInputFocused = true;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    return true;
                }
                else
                {
                    IsSearchInputFocused = false;
                }
            }

            // 0. Kiểm tra tương tác với Thanh Khung Bầu Liquid Glass (Floating Liquid Glass Palette)
            if (SelectedTool == 7 || SelectedTool == 8 || SelectedShape != null)
            {
                RectangleF pillRect = GetLiquidGlassPaletteRect(width, height, 26);
                if (pillRect.Contains(x, y))
                {
                    // a. Kiểm tra các chấm màu tròn
                    for (int i = 0; i < LiquidSwatches.Length; i++)
                    {
                        float dotX = pillRect.X + 54 + (i * 28);
                        float dotY = pillRect.Y + 12;
                        RectangleF dotRect = new RectangleF(dotX, dotY, 22, 22);
                        if (dotRect.Contains(x, y))
                        {
                            ActiveColor = LiquidSwatches[i];
                            if (SelectedShape != null)
                            {
                                PushUndoState("Đổi màu hình");
                                SelectedShape.StrokeColor = ActiveColor;
                                if (ActivePageIndex < Document.Pages.Count)
                                    Document.Pages[ActivePageIndex].InvalidateCache();
                                Document.IsModified = true;
                            }
                            if (parentForm != null) parentForm.Invalidate();
                            return true;
                        }
                    }

                    // b. Nút mở Bảng màu đầy đủ (+)
                    float moreBtnX = pillRect.X + 54 + (LiquidSwatches.Length * 28) + 4;
                    RectangleF moreRect = new RectangleF(moreBtnX, pillRect.Y + 12, 22, 22);
                    if (moreRect.Contains(x, y))
                    {
                        Point scrPt = parentForm != null
                            ? parentForm.PointToScreen(new Point((int)moreRect.X, (int)moreRect.Y - 260))
                            : new Point((int)moreRect.X, (int)moreRect.Y);
                        ColorPaletteForm palette = new ColorPaletteForm(scrPt, false);
                        palette.OnColorSelected += (c) =>
                        {
                            ActiveColor = c;
                            if (SelectedShape != null)
                            {
                                PushUndoState("Đổi màu hình");
                                SelectedShape.StrokeColor = c;
                                if (ActivePageIndex < Document.Pages.Count)
                                    Document.Pages[ActivePageIndex].InvalidateCache();
                                Document.IsModified = true;
                            }
                            if (parentForm != null) parentForm.Invalidate();
                        };
                        palette.Show(parentForm);
                        return true;
                    }

                    // c. Chọn cỡ nét bút (2.5pt vs 5pt)
                    float s1X = moreBtnX + 38;
                    RectangleF s1Rect = new RectangleF(s1X, pillRect.Y + 13, 20, 20);
                    if (s1Rect.Contains(x, y))
                    {
                        ActiveStrokeWidth = 2.5f;
                        if (SelectedShape != null) { SelectedShape.StrokeWidth = 2.5f; Document.IsModified = true; }
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    float s2X = s1X + 24;
                    RectangleF s2Rect = new RectangleF(s2X, pillRect.Y + 13, 20, 20);
                    if (s2Rect.Contains(x, y))
                    {
                        ActiveStrokeWidth = 5.0f;
                        if (SelectedShape != null) { SelectedShape.StrokeWidth = 5.0f; Document.IsModified = true; }
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    return true; // Ngăn bấm xuyên qua thanh liquid glass xuống trang canvas
                }
            }

            // 1. Kiểm tra kéo thanh cuộn chính (Main ScrollBar)
            if (MainScrollbarThumbRect.Contains(x, y))
            {
                IsDraggingMainScrollThumb = true;
                DragStartMainMouseY = y;
                DragStartMainScrollY = ScrollY;
                return true;
            }
            else if (MainScrollbarTrackRect.Contains(x, y))
            {
                // Nhấp vào track -> Nhảy scroll trực tiếp
                float trackH = MainScrollbarTrackRect.Height - MainScrollbarThumbRect.Height;
                if (trackH > 0)
                {
                    float frac = (y - topOffset - MainScrollbarThumbRect.Height / 2f) / trackH;
                    frac = Math.Max(0f, Math.Min(1f, frac));
                    TargetScrollY = frac * GetMaxScrollY(viewH);
                    ScrollY = TargetScrollY;
                    UpdateActivePageFromScroll(width, height, topOffset, 26);
                }
                return true;
            }

            // 2. Kiểm tra nhấp chuột trên Sidebar Thumbnails & Bookmarks
            if (ShowThumbnailsSidebar && x <= SidebarWidth)
            {
                // Nhấp vào 2 Tab [📑 Trang] vs [🔖 Mục lục] trên đỉnh Sidebar
                if (y >= topOffset && y <= topOffset + 36)
                {
                    if (x < SidebarWidth / 2)
                    {
                        SidebarViewMode = 0;
                    }
                    else
                    {
                        SidebarViewMode = 1;
                    }
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Nếu đang ở Tab Mục lục (Bookmarks)
                if (SidebarViewMode == 1)
                {
                    // Nút + Thêm Dấu Trang
                    if (BtnAddBookmarkRect.Contains(x, y))
                    {
                        PushUndoState("Thêm dấu trang");
                        AddBookmark($"Dấu trang - Trang {ActivePageIndex + 1}", ActivePageIndex, ScrollY);
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }

                    // Nhấp vào danh sách Dấu trang
                    int curY = topOffset + 80;
                    if (Document != null && Document.Bookmarks != null)
                    {
                        PdfBookmarkNode clickedBm = HitTestBookmarkNode(Document.Bookmarks, x, y, 0, ref curY);
                        if (clickedBm != null)
                        {
                            SelectedBookmark = clickedBm;
                            NavigateToBookmark(clickedBm);
                            if (parentForm != null) parentForm.Invalidate();
                            return true;
                        }
                    }
                    return true;
                }

                // Kiểm tra kéo thanh cuộn Sidebar
                if (SidebarScrollbarThumbRect.Contains(x, y))
                {
                    IsDraggingSidebarScrollThumb = true;
                    DragStartSidebarMouseY = y;
                    DragStartSidebarScrollY = SidebarScrollY;
                    return true;
                }
                else if (SidebarScrollbarTrackRect.Contains(x, y))
                {
                    float trackH = SidebarScrollbarTrackRect.Height - SidebarScrollbarThumbRect.Height;
                    if (trackH > 0)
                    {
                        float frac = (y - (topOffset + 36) - SidebarScrollbarThumbRect.Height / 2f) / trackH;
                        frac = Math.Max(0f, Math.Min(1f, frac));
                        TargetSidebarScrollY = frac * GetMaxSidebarScrollY(viewH - 36);
                        SidebarScrollY = TargetSidebarScrollY;
                    }
                    return true;
                }

                int startY = topOffset + 44;
                int thumbW = 140;
                int thumbH = 198;

                for (int i = 0; i < Document.Pages.Count; i++)
                {
                    int thumbX = (SidebarWidth - 8 - thumbW) / 2;
                    int thumbY = startY - (int)SidebarScrollY + i * (thumbH + 34);
                    Rectangle thumbRect = new Rectangle(thumbX, thumbY, thumbW, thumbH);

                    if (thumbRect.Contains(x, y))
                    {
                        // Kiểm tra nút Xoay 90°
                        Rectangle rotBtn = new Rectangle(thumbRect.Right - 38, thumbY + 4, 16, 16);
                        if (rotBtn.Contains(x, y))
                        {
                            PushUndoState("Xoay trang 90°");
                            Document.Pages[i].RotateClockwise();
                            Document.Pages[i].InvalidateCache();
                            Document.IsModified = true;
                            if (parentForm != null) parentForm.Invalidate();
                            return true;
                        }

                        // Kiểm tra nút Xóa Trang
                        Rectangle delBtn = new Rectangle(thumbRect.Right - 18, thumbY + 4, 16, 16);
                        if (delBtn.Contains(x, y))
                        {
                            if (Document.Pages.Count > 1)
                            {
                                PushUndoState("Xóa trang");
                                Document.DeletePage(i);
                                if (ActivePageIndex >= Document.Pages.Count) ActivePageIndex = Document.Pages.Count - 1;
                                if (parentForm != null) parentForm.Invalidate();
                            }
                            return true;
                        }

                        // Chọn trang và cuộn đến trang đó
                        ActivePageIndex = i;
                        ScrollToPage(i, width, height);
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }
                return true;
            }

            // 3. Ưu tiên trên Canvas: Kiểm tra nhấp vào 8 chốt điều chỉnh kích thước của Hộp chữ (Resize Handles)
            // (Chỉ áp dụng ở chế độ con trỏ thông thường, không chặn khi đang dùng nút "Sửa chữ" Tool 2)
            PdfTextElement activeTxt = SelectedText ?? EditingText;
            if (SelectedTool != 2 && activeTxt != null && ActivePageIndex >= 0 && ActivePageIndex < Document.Pages.Count)
            {
                RectangleF pBounds = GetPageScreenBounds(ActivePageIndex, width, height, topOffset);
                float sX = pBounds.X + (activeTxt.Bounds.X * ZoomLevel);
                float sY = pBounds.Y + (activeTxt.Bounds.Y * ZoomLevel);
                float sW = Math.Max(20f, activeTxt.Bounds.Width * ZoomLevel);
                float sH = Math.Max(15f, activeTxt.Bounds.Height * ZoomLevel);
                RectangleF selScreenRect = new RectangleF(sX, sY, sW, sH);

                PointF[] handles = new PointF[] {
                    new PointF(selScreenRect.Left, selScreenRect.Top),
                    new PointF(selScreenRect.Left + selScreenRect.Width / 2f, selScreenRect.Top),
                    new PointF(selScreenRect.Right, selScreenRect.Top),
                    new PointF(selScreenRect.Right, selScreenRect.Top + selScreenRect.Height / 2f),
                    new PointF(selScreenRect.Right, selScreenRect.Bottom),
                    new PointF(selScreenRect.Left + selScreenRect.Width / 2f, selScreenRect.Bottom),
                    new PointF(selScreenRect.Left, selScreenRect.Bottom),
                    new PointF(selScreenRect.Left, selScreenRect.Top + selScreenRect.Height / 2f)
                };

                for (int h = 0; h < handles.Length; h++)
                {
                    // Vùng bấm nhạy 10px quanh mỗi chốt để người dùng nắm và kéo cực kỳ dễ dàng và chuẩn xác
                    if (Math.Abs(x - handles[h].X) <= 10 && Math.Abs(y - handles[h].Y) <= 10)
                    {
                        SelectedText = activeTxt;
                        PushUndoState("Kéo kích thước hộp chữ");
                        IsResizingHandle = true;
                        DraggingHandleIndex = h;
                        DragStartMouse = new PointF(x, y);
                        DragStartBoxBounds = activeTxt.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                // Nếu đang soạn thảo văn bản và nhấp vào bên trong thân hộp chữ -> giữ nguyên để soạn thảo
                if (EditingText == activeTxt && selScreenRect.Contains(x, y))
                {
                    return true;
                }
            }

            // 4. Tương tác theo từng trang trên Canvas
            int pageIdx = GetPageIndexAtPoint(x, y, width, height, topOffset);
            if (pageIdx >= 0)
            {
                ActivePageIndex = pageIdx;
                PointF pagePt = ScreenToPagePoint(new PointF(x, y), pageIdx, width, height, topOffset);
                PdfPageModel page = Document.Pages[pageIdx];

                // Kiểm tra tương tác Biểu mẫu (AcroForms Form Filling)
                if (SelectedTool == 12 || SelectedTool == 0)
                {
                    if (page.FormFields != null && page.FormFields.Count > 0)
                    {
                        for (int f = page.FormFields.Count - 1; f >= 0; f--)
                        {
                            var ff = page.FormFields[f];
                            if (ff.Bounds.Contains(pagePt))
                            {
                                SelectedFormField = ff;
                                if (ff.FieldType == PdfFormFieldType.CheckBox)
                                {
                                    PushUndoState("Đánh dấu hộp kiểm");
                                    ff.IsChecked = !ff.IsChecked;
                                    page.InvalidateCache();
                                    Document.IsModified = true;
                                    if (parentForm != null) parentForm.Invalidate();
                                    return true;
                                }
                                else if (ff.FieldType == PdfFormFieldType.RadioButton)
                                {
                                    PushUndoState("Chọn nút radio");
                                    foreach (var other in page.FormFields)
                                    {
                                        if (other.FieldType == PdfFormFieldType.RadioButton && other.GroupName == ff.GroupName)
                                        {
                                            other.IsChecked = false;
                                        }
                                    }
                                    ff.IsChecked = true;
                                    page.InvalidateCache();
                                    Document.IsModified = true;
                                    if (parentForm != null) parentForm.Invalidate();
                                    return true;
                                }
                                else if (ff.FieldType == PdfFormFieldType.ComboBox)
                                {
                                    PushUndoState("Chọn danh sách");
                                    if (ff.Options != null && ff.Options.Count > 0)
                                    {
                                        int curIdx = ff.Options.IndexOf(ff.Value);
                                        int nextIdx = (curIdx + 1) % ff.Options.Count;
                                        ff.Value = ff.Options[nextIdx];
                                    }
                                    page.InvalidateCache();
                                    Document.IsModified = true;
                                    if (parentForm != null) parentForm.Invalidate();
                                    return true;
                                }
                                else if (ff.FieldType == PdfFormFieldType.Text)
                                {
                                    if (EditingFormField != null && EditingFormField != ff) CommitFormFieldEdit();
                                    EditingFormField = ff;
                                    EditingFormFieldBuffer = ff.Value ?? "";
                                    if (parentForm != null) parentForm.Invalidate();
                                    return true;
                                }
                                return true;
                            }
                        }
                    }
                }

                // Nếu nhấp ra ngoài form field khi đang soạn thảo form field
                if (EditingFormField != null)
                {
                    CommitFormFieldEdit();
                }

                // Thêm ô văn bản biểu mẫu (Tool 13)
                if (SelectedTool == 13)
                {
                    PushUndoState("Thêm ô văn bản biểu mẫu");
                    var ff = new PdfFormFieldElement(PdfFormFieldType.Text, new RectangleF(pagePt.X, pagePt.Y, 160, 26), "text_" + (page.FormFields.Count + 1));
                    page.FormFields.Add(ff);
                    page.InvalidateCache();
                    Document.IsModified = true;
                    SelectedFormField = ff;
                    EditingFormField = ff;
                    EditingFormFieldBuffer = "";
                    SelectedTool = 12;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Thêm hộp kiểm biểu mẫu (Tool 14)
                if (SelectedTool == 14)
                {
                    PushUndoState("Thêm hộp kiểm biểu mẫu");
                    var ff = new PdfFormFieldElement(PdfFormFieldType.CheckBox, new RectangleF(pagePt.X, pagePt.Y, 18, 18), "chk_" + (page.FormFields.Count + 1));
                    page.FormFields.Add(ff);
                    page.InvalidateCache();
                    Document.IsModified = true;
                    SelectedFormField = ff;
                    SelectedTool = 12;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Thêm nút radio biểu mẫu (Tool 15)
                if (SelectedTool == 15)
                {
                    PushUndoState("Thêm nút radio biểu mẫu");
                    var ff = new PdfFormFieldElement(PdfFormFieldType.RadioButton, new RectangleF(pagePt.X, pagePt.Y, 18, 18), "radio_" + (page.FormFields.Count + 1)) { GroupName = "Group1" };
                    page.FormFields.Add(ff);
                    page.InvalidateCache();
                    Document.IsModified = true;
                    SelectedFormField = ff;
                    SelectedTool = 12;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Thêm menu thả biểu mẫu (Tool 16)
                if (SelectedTool == 16)
                {
                    PushUndoState("Thêm menu thả biểu mẫu");
                    var ff = new PdfFormFieldElement(PdfFormFieldType.ComboBox, new RectangleF(pagePt.X, pagePt.Y, 160, 26), "combo_" + (page.FormFields.Count + 1))
                    {
                        Options = new List<string> { "Lựa chọn A", "Lựa chọn B", "Lựa chọn C" },
                        Value = "Lựa chọn A"
                    };
                    page.FormFields.Add(ff);
                    page.InvalidateCache();
                    Document.IsModified = true;
                    SelectedFormField = ff;
                    SelectedTool = 12;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Đánh dấu xóa mật Redaction (Tool 17)
                if (SelectedTool == 17)
                {
                    IsBoxSelecting = true;
                    BoxSelectStart = pagePt;
                    BoxSelectCurrent = pagePt;
                    DrawingPageIndex = pageIdx;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Nếu đang dùng công cụ Thêm Chữ (Add Text - Tool 1)
                if (SelectedTool == 1)
                {
                    PushUndoState("Thêm hộp chữ");
                    var newTxt = new PdfTextElement("", new RectangleF(pagePt.X, pagePt.Y, 200, 32), "Segoe UI", 12f);
                    newTxt.IsPlaceholder = true;
                    newTxt.BorderColor = Color.Transparent;
                    newTxt.BorderWidth = 0f;
                    newTxt.BackgroundColor = Color.Transparent;
                    page.TextElements.Add(newTxt);
                    page.InvalidateCache();
                    Document.IsModified = true;
                    SelectedText = newTxt;
                    StartInlineEdit(pageIdx, newTxt);
                    SelectedTool = 0; // Quay về con trỏ sau khi đặt hộp chữ
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Nếu đang dùng công cụ Sửa chữ (Edit Text - Tool 2)
                if (SelectedTool == 2)
                {
                    if (page.TextElements.Count == 0 && Document.UnderlyingPdfiumDoc != null)
                    {
                        AutoDetectTextForPage(pageIdx);
                    }
                    for (int t = page.TextElements.Count - 1; t >= 0; t--)
                    {
                        if (page.TextElements[t].Bounds.Contains(pagePt))
                        {
                            StartInlineEdit(pageIdx, page.TextElements[t], x, width, height);
                            if (parentForm != null) parentForm.Invalidate();
                            return true;
                        }
                    }
                }

                RectangleF pageBounds = GetPageScreenBounds(pageIdx, width, height, topOffset);

                // 1. Kiểm tra bấm vào 8 chốt vuông co giãn kích thước (Resize Handles - chỉ ở chế độ Con trỏ Tool 0)
                if (SelectedTool == 0 && SelectedShape != null)
                {
                    RectangleF sRect = new RectangleF(pageBounds.X + SelectedShape.Bounds.X * ZoomLevel, pageBounds.Y + SelectedShape.Bounds.Y * ZoomLevel, SelectedShape.Bounds.Width * ZoomLevel, SelectedShape.Bounds.Height * ZoomLevel);
                    int hIdx = HitTestResizeHandles(sRect, new PointF(x, y));
                    if (hIdx >= 0)
                    {
                        PushUndoState("Đổi kích thước hình");
                        IsResizingHandle = true;
                        DraggingHandleIndex = hIdx;
                        DragStartMouse = new PointF(x, y);
                        DragStartBoxBounds = SelectedShape.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }
                if (SelectedTool == 0 && SelectedSignature != null)
                {
                    RectangleF sRect = new RectangleF(pageBounds.X + SelectedSignature.Bounds.X * ZoomLevel, pageBounds.Y + SelectedSignature.Bounds.Y * ZoomLevel, SelectedSignature.Bounds.Width * ZoomLevel, SelectedSignature.Bounds.Height * ZoomLevel);
                    int hIdx = HitTestResizeHandles(sRect, new PointF(x, y));
                    if (hIdx >= 0)
                    {
                        PushUndoState("Đổi kích thước chữ ký");
                        IsResizingHandle = true;
                        DraggingHandleIndex = hIdx;
                        DragStartMouse = new PointF(x, y);
                        DragStartBoxBounds = SelectedSignature.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }
                if (SelectedTool == 0 && SelectedAnnotation != null && SelectedAnnotation.AnnotationType == PdfAnnotationType.Stamp)
                {
                    RectangleF sRect = new RectangleF(pageBounds.X + SelectedAnnotation.Bounds.X * ZoomLevel, pageBounds.Y + SelectedAnnotation.Bounds.Y * ZoomLevel, SelectedAnnotation.Bounds.Width * ZoomLevel, SelectedAnnotation.Bounds.Height * ZoomLevel);
                    int hIdx = HitTestResizeHandles(sRect, new PointF(x, y));
                    if (hIdx >= 0)
                    {
                        PushUndoState("Đổi kích thước con dấu");
                        IsResizingHandle = true;
                        DraggingHandleIndex = hIdx;
                        DragStartMouse = new PointF(x, y);
                        DragStartBoxBounds = SelectedAnnotation.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }
                if (SelectedTool == 0 && SelectedText != null)
                {
                    RectangleF sRect = new RectangleF(pageBounds.X + SelectedText.Bounds.X * ZoomLevel, pageBounds.Y + SelectedText.Bounds.Y * ZoomLevel, SelectedText.Bounds.Width * ZoomLevel, SelectedText.Bounds.Height * ZoomLevel);
                    int hIdx = HitTestResizeHandles(sRect, new PointF(x, y));
                    if (hIdx >= 0)
                    {
                        PushUndoState("Đổi kích thước hộp chữ");
                        IsResizingHandle = true;
                        DraggingHandleIndex = hIdx;
                        DragStartMouse = new PointF(x, y);
                        DragStartBoxBounds = SelectedText.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                // Nếu đang dùng công cụ Bút Dạ Quang Highlight (3) hoặc Khung Hình Chữ Nhật (8)
                if (SelectedTool == 3 || SelectedTool == 8)
                {
                    SelectedShape = null;
                    SelectedText = null;
                    SelectedSignature = null;
                    SelectedAnnotation = null;
                    IsBoxSelecting = true;
                    BoxSelectStart = new PointF(x, y);
                    BoxSelectCurrent = new PointF(x, y);
                    return true;
                }

                // Nếu đang dùng công cụ Bút Vẽ Tay Tự Do (Freehand Pen - 7)
                if (SelectedTool == 7)
                {
                    SelectedShape = null;
                    SelectedText = null;
                    SelectedSignature = null;
                    SelectedAnnotation = null;
                    IsDrawingFreehand = true;
                    DrawingPageIndex = pageIdx;
                    CurrentFreehandPoints = new List<PointF> { pagePt };
                    return true;
                }

                // Nếu đang dùng công cụ Đóng Dấu Mộc (Stamp - 10)
                if (SelectedTool == 10)
                {
                    PushUndoState("Đóng dấu mộc");
                    Color sCol = ActiveStampColor != Color.Empty ? ActiveStampColor : ActiveColor;
                    string sTxt = !string.IsNullOrEmpty(ActiveStampText) ? ActiveStampText : "ĐÃ DUYỆT";
                    float stampW = 120f;
                    float stampH = 40f;
                    using (Bitmap tb = new Bitmap(1, 1))
                    using (Graphics tg = Graphics.FromImage(tb))
                    using (Font sf = new Font("Segoe UI", 13f, FontStyle.Bold))
                    {
                        SizeF sz = tg.MeasureString(sTxt, sf);
                        stampW = Math.Max(120f, (float)Math.Ceiling(sz.Width + 28f));
                        stampH = Math.Max(40f, (float)Math.Ceiling(sz.Height + 16f));
                    }
                    var stampAnn = new PdfAnnotationElement(PdfAnnotationType.Stamp, new RectangleF(pagePt.X - stampW / 2f, pagePt.Y - stampH / 2f, stampW, stampH), sCol);
                    stampAnn.StampText = sTxt;
                    page.Annotations.Add(stampAnn);
                    SelectedAnnotation = stampAnn;
                    SelectedShape = null;
                    SelectedText = null;
                    SelectedSignature = null;
                    page.InvalidateCache();
                    Document.IsModified = true;
                    SelectedTool = 0;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // Nếu đang dùng công cụ Ký Tên Điện Tử (Signature - 11)
                if (SelectedTool == 11)
                {
                    using (var dlg = new PdfSignatureInputDialog())
                    {
                        if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                        {
                            PushUndoState("Chèn chữ ký điện tử");
                            SizeF reqSize = CalculateSignatureAutoBounds(dlg.SignerName, dlg.SignDate, dlg.Reason, dlg.SignatureImage);
                            var sig = new PdfSignatureElement(new RectangleF(pagePt.X - reqSize.Width / 2f, pagePt.Y - reqSize.Height / 2f, reqSize.Width, reqSize.Height), dlg.SignerName);
                            sig.SignDate = dlg.SignDate;
                            sig.Reason = dlg.Reason;
                            sig.SignatureColor = dlg.SignatureColor;
                            if (dlg.SignatureImage != null) sig.SignatureImage = dlg.SignatureImage;
                            if (dlg.SignatureStrokes != null) sig.SignatureStrokes = new List<List<PointF>>(dlg.SignatureStrokes);
                            page.Signatures.Add(sig);
                            SelectedSignature = sig;
                            SelectedShape = null;
                            SelectedText = null;
                            SelectedAnnotation = null;
                            page.InvalidateCache();
                            Document.IsModified = true;
                        }
                    }
                    SelectedTool = 0;
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }

                // 2. Kiểm tra nhấp vào Khung Hình Khối (ShapeElements)
                for (int sh = page.ShapeElements.Count - 1; sh >= 0; sh--)
                {
                    var targetShp = page.ShapeElements[sh];
                    if (targetShp.Bounds.Contains(pagePt))
                    {
                        if (EditingText != null) CommitInlineEdit();
                        SelectedShape = targetShp;
                        SelectedText = null;
                        SelectedSignature = null;
                        SelectedAnnotation = null;
                        PushUndoState("Di chuyển hình");
                        IsDraggingObject = true;
                        DragStartMouse = new PointF(x, y);
                        DragStartBounds = SelectedShape.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                // 3. Kiểm tra nhấp vào Chữ Ký (Signatures)
                for (int s = page.Signatures.Count - 1; s >= 0; s--)
                {
                    if (page.Signatures[s].Bounds.Contains(pagePt))
                    {
                        if (EditingText != null) CommitInlineEdit();
                        SelectedSignature = page.Signatures[s];
                        SelectedShape = null;
                        SelectedText = null;
                        SelectedAnnotation = null;
                        PushUndoState("Di chuyển chữ ký");
                        IsDraggingObject = true;
                        DragStartMouse = new PointF(x, y);
                        DragStartBounds = SelectedSignature.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                // 4. Kiểm tra nhấp vào Con Dấu (Annotations Stamp)
                for (int a = page.Annotations.Count - 1; a >= 0; a--)
                {
                    var targetAnn = page.Annotations[a];
                    if (targetAnn.AnnotationType == PdfAnnotationType.Stamp && targetAnn.Bounds.Contains(pagePt))
                    {
                        if (EditingText != null) CommitInlineEdit();
                        SelectedAnnotation = targetAnn;
                        SelectedShape = null;
                        SelectedText = null;
                        SelectedSignature = null;
                        PushUndoState("Di chuyển con dấu");
                        IsDraggingObject = true;
                        DragStartMouse = new PointF(x, y);
                        DragStartBounds = SelectedAnnotation.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                // 5. Kiểm tra nhấp vào Text Element
                if (page.TextElements.Count == 0 && Document.UnderlyingPdfiumDoc != null)
                {
                    AutoDetectTextForPage(pageIdx);
                }
                for (int t = page.TextElements.Count - 1; t >= 0; t--)
                {
                    var targetTxt = page.TextElements[t];
                    if (targetTxt.Bounds.Contains(pagePt))
                    {
                        // Chỉ tương tác với chữ nếu là Hộp chữ tự tạo, hoặc khi đang chọn công cụ Sửa chữ (SelectedTool == 2)
                        // Tuyệt đối không chọn/kéo chữ gốc khi đang dùng con trỏ đọc bình thường (SelectedTool == 0)
                        bool canInteract = (!targetTxt.IsDetectedFromPdf) || (SelectedTool == 2);
                        if (!canInteract) continue;

                        if (EditingText != null && EditingText != targetTxt)
                        {
                            CommitInlineEdit();
                        }
                        SelectedText = targetTxt;
                        SelectedShape = null;
                        SelectedSignature = null;
                        SelectedAnnotation = null;
                        // Chữ tự thêm sau (!targetTxt.IsDetectedFromPdf): cho phép tự sửa trực tiếp
                        // Chữ có sẵn từ PDF: chỉ sửa khi đang chọn công cụ Sửa chữ (SelectedTool == 2)
                        if (!targetTxt.IsDetectedFromPdf && EditingText != targetTxt)
                        {
                            StartInlineEdit(pageIdx, targetTxt, x, width, height);
                        }
                        else if (EditingText == SelectedText)
                        {
                            RepositionCursor(pageIdx, EditingText, x, width, height);
                        }
                        PushUndoState("Di chuyển hộp chữ");
                        IsDraggingObject = true;
                        DragStartMouse = new PointF(x, y);
                        DragStartBounds = SelectedText.Bounds;
                        if (parentForm != null) parentForm.Invalidate();
                        return true;
                    }
                }

                // Bỏ chọn đối tượng khi nhấp vào vùng trắng trên trang
                bool hadSelectionOnPage = (SelectedText != null || EditingText != null || SelectedAnnotation != null || SelectedSignature != null || SelectedShape != null);
                if (EditingText != null)
                {
                    CommitInlineEdit();
                }
                SelectedText = null;
                SelectedAnnotation = null;
                SelectedSignature = null;
                SelectedShape = null;
                if (hadSelectionOnPage && parentForm != null) parentForm.Invalidate();
                return true;
            }

            // 5. Nếu nhấp vào vùng canvas bên ngoài trang (ngoài lề trang nhưng không thuộc sidebar/ribbon/status bar)
            int sbW = ShowThumbnailsSidebar ? SidebarWidth : 0;
            if (x > sbW && y >= topOffset && y <= height - 26)
            {
                bool hadSelectionOutside = (SelectedText != null || EditingText != null || SelectedAnnotation != null || SelectedSignature != null || SelectedShape != null);
                if (EditingText != null)
                {
                    CommitInlineEdit();
                }
                SelectedText = null;
                SelectedAnnotation = null;
                SelectedSignature = null;
                SelectedShape = null;
                if (hadSelectionOutside && parentForm != null) parentForm.Invalidate();
                return true;
            }

            return false;
        }

        public bool HandleMouseMove(int x, int y, int width, int height, MouseButtons button)
        {
            int topOffset = 155;
            int viewH = height - topOffset - 26;

            // Xử lý kéo thanh cuộn chính
            if (IsDraggingMainScrollThumb)
            {
                float dy = y - DragStartMainMouseY;
                float trackH = MainScrollbarTrackRect.Height - MainScrollbarThumbRect.Height;
                if (trackH > 0)
                {
                    float scrollFrac = dy / trackH;
                    TargetScrollY = Math.Max(0, Math.Min(GetMaxScrollY(viewH), DragStartMainScrollY + scrollFrac * GetMaxScrollY(viewH)));
                    ScrollY = TargetScrollY;
                    UpdateActivePageFromScroll(width, height, topOffset, 26);
                }
                return true;
            }

            // Xử lý kéo thanh cuộn Sidebar
            if (IsDraggingSidebarScrollThumb)
            {
                float dy = y - DragStartSidebarMouseY;
                float trackH = SidebarScrollbarTrackRect.Height - SidebarScrollbarThumbRect.Height;
                if (trackH > 0)
                {
                    float scrollFrac = dy / trackH;
                    TargetSidebarScrollY = Math.Max(0, Math.Min(GetMaxSidebarScrollY(viewH - 36), DragStartSidebarScrollY + scrollFrac * GetMaxSidebarScrollY(viewH - 36)));
                    SidebarScrollY = TargetSidebarScrollY;
                }
                return true;
            }

            // Hover trên Scrollbars
            IsMainScrollbarHovered = MainScrollbarThumbRect.Contains(x, y);
            IsSidebarScrollbarHovered = SidebarScrollbarThumbRect.Contains(x, y);

            // Hover trên Sidebar
            if (ShowThumbnailsSidebar && x <= SidebarWidth)
            {
                int startY = topOffset + 44;
                int thumbW = 140;
                int thumbH = 198;
                HoverSidebarPageIndex = -1;

                if (Document != null)
                {
                    for (int i = 0; i < Document.Pages.Count; i++)
                    {
                        int thumbX = (SidebarWidth - 8 - thumbW) / 2;
                        int thumbY = startY - (int)SidebarScrollY + i * (thumbH + 34);
                        Rectangle thumbRect = new Rectangle(thumbX, thumbY, thumbW, thumbH);
                        if (thumbRect.Contains(x, y))
                        {
                            HoverSidebarPageIndex = i;
                            break;
                        }
                    }
                }
                return true;
            }

            // Kéo vẽ tay tự do
            if (IsDrawingFreehand && button == MouseButtons.Left)
            {
                PointF pagePt = ScreenToPagePoint(new PointF(x, y), DrawingPageIndex, width, height, topOffset);
                if (!pagePt.IsEmpty)
                {
                    CurrentFreehandPoints.Add(pagePt);
                    return true;
                }
            }

            // Kéo quét vùng chọn Box Selection
            if (IsBoxSelecting && button == MouseButtons.Left)
            {
                BoxSelectCurrent = new PointF(x, y);
                return true;
            }

            // Kéo co giãn kích thước qua 8 chốt vuông (Interactive 8-Handle Resizing)
            if (IsResizingHandle && (button == MouseButtons.Left || Control.MouseButtons == MouseButtons.Left))
            {
                float dx = (x - DragStartMouse.X) / ZoomLevel;
                float dy = (y - DragStartMouse.Y) / ZoomLevel;

                float origL = DragStartBoxBounds.Left;
                float origT = DragStartBoxBounds.Top;
                float origR = DragStartBoxBounds.Right;
                float origB = DragStartBoxBounds.Bottom;

                RectangleF newBounds = DragStartBoxBounds;

                switch (DraggingHandleIndex)
                {
                    case 0: // Top-Left
                        newBounds = new RectangleF(origR - Math.Max(10f, DragStartBoxBounds.Width - dx),
                                                  origB - Math.Max(10f, DragStartBoxBounds.Height - dy),
                                                  Math.Max(10f, DragStartBoxBounds.Width - dx),
                                                  Math.Max(10f, DragStartBoxBounds.Height - dy));
                        break;
                    case 1: // Top-Middle
                        newBounds = new RectangleF(DragStartBoxBounds.X,
                                                  origB - Math.Max(10f, DragStartBoxBounds.Height - dy),
                                                  DragStartBoxBounds.Width,
                                                  Math.Max(10f, DragStartBoxBounds.Height - dy));
                        break;
                    case 2: // Top-Right
                        newBounds = new RectangleF(DragStartBoxBounds.X,
                                                  origB - Math.Max(10f, DragStartBoxBounds.Height - dy),
                                                  Math.Max(10f, DragStartBoxBounds.Width + dx),
                                                  Math.Max(10f, DragStartBoxBounds.Height - dy));
                        break;
                    case 3: // Middle-Right
                        newBounds = new RectangleF(DragStartBoxBounds.X, DragStartBoxBounds.Y,
                                                  Math.Max(10f, DragStartBoxBounds.Width + dx),
                                                  DragStartBoxBounds.Height);
                        break;
                    case 4: // Bottom-Right
                        newBounds = new RectangleF(DragStartBoxBounds.X, DragStartBoxBounds.Y,
                                                  Math.Max(10f, DragStartBoxBounds.Width + dx),
                                                  Math.Max(10f, DragStartBoxBounds.Height + dy));
                        break;
                    case 5: // Bottom-Middle
                        newBounds = new RectangleF(DragStartBoxBounds.X, DragStartBoxBounds.Y,
                                                  DragStartBoxBounds.Width,
                                                  Math.Max(10f, DragStartBoxBounds.Height + dy));
                        break;
                    case 6: // Bottom-Left
                        newBounds = new RectangleF(origR - Math.Max(10f, DragStartBoxBounds.Width - dx),
                                                  DragStartBoxBounds.Y,
                                                  Math.Max(10f, DragStartBoxBounds.Width - dx),
                                                  Math.Max(10f, DragStartBoxBounds.Height + dy));
                        break;
                    case 7: // Middle-Left
                        newBounds = new RectangleF(origR - Math.Max(10f, DragStartBoxBounds.Width - dx),
                                                  DragStartBoxBounds.Y,
                                                  Math.Max(10f, DragStartBoxBounds.Width - dx),
                                                  DragStartBoxBounds.Height);
                        break;
                }

                if (SelectedShape != null)
                {
                    SelectedShape.Bounds = newBounds;
                }
                else if (SelectedText != null)
                {
                    SelectedText.Bounds = newBounds;
                }
                else if (SelectedSignature != null)
                {
                    SizeF minSigSz = CalculateSignatureAutoBounds(SelectedSignature.SignerName, SelectedSignature.SignDate, SelectedSignature.Reason, SelectedSignature.SignatureImage);
                    newBounds = new RectangleF(newBounds.X, newBounds.Y, Math.Max(minSigSz.Width, newBounds.Width), Math.Max(minSigSz.Height, newBounds.Height));
                    SelectedSignature.Bounds = newBounds;
                }
                else if (SelectedAnnotation != null)
                {
                    if (SelectedAnnotation.AnnotationType == PdfAnnotationType.Stamp)
                    {
                        using (Bitmap tb = new Bitmap(1, 1))
                        using (Graphics tg = Graphics.FromImage(tb))
                        using (Font sf = new Font("Segoe UI", 13f, FontStyle.Bold))
                        {
                            SizeF sz = tg.MeasureString(SelectedAnnotation.StampText ?? "ĐÃ DUYỆT", sf);
                            newBounds = new RectangleF(newBounds.X, newBounds.Y, Math.Max(sz.Width + 24f, newBounds.Width), Math.Max(sz.Height + 16f, newBounds.Height));
                        }
                    }
                    SelectedAnnotation.Bounds = newBounds;
                }

                Document.IsModified = true;
                if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                return true;
            }

            // Kéo di chuyển vị trí đối tượng đang chọn (Shape, Text, Signature, Annotation)
            if (IsDraggingObject && button == MouseButtons.Left)
            {
                float dx = (x - DragStartMouse.X) / ZoomLevel;
                float dy = (y - DragStartMouse.Y) / ZoomLevel;

                if (SelectedShape != null)
                {
                    SelectedShape.Bounds = new RectangleF(DragStartBounds.X + dx, DragStartBounds.Y + dy, DragStartBounds.Width, DragStartBounds.Height);
                    Document.IsModified = true;
                    if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                    return true;
                }
                if (SelectedText != null)
                {
                    SelectedText.Bounds = new RectangleF(DragStartBounds.X + dx, DragStartBounds.Y + dy, DragStartBounds.Width, DragStartBounds.Height);
                    Document.IsModified = true;
                    if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                    return true;
                }
                if (SelectedSignature != null)
                {
                    SelectedSignature.Bounds = new RectangleF(DragStartBounds.X + dx, DragStartBounds.Y + dy, DragStartBounds.Width, DragStartBounds.Height);
                    Document.IsModified = true;
                    if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                    return true;
                }
                if (SelectedAnnotation != null)
                {
                    SelectedAnnotation.Bounds = new RectangleF(DragStartBounds.X + dx, DragStartBounds.Y + dy, DragStartBounds.Width, DragStartBounds.Height);
                    Document.IsModified = true;
                    if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                    return true;
                }
            }

            return false;
        }

        public bool HandleMouseUp(int x, int y, int width, int height, MouseButtons button)
        {
            int topOffset = 155;

            IsDraggingMainScrollThumb = false;
            IsDraggingSidebarScrollThumb = false;
            IsResizingHandle = false;
            DraggingHandleIndex = -1;

            // Kết thúc vẽ tay tự do -> lưu thành Annotation InkDrawing
            if (IsDrawingFreehand)
            {
                if (CurrentFreehandPoints != null && CurrentFreehandPoints.Count > 1 && DrawingPageIndex < Document.Pages.Count)
                {
                    PushUndoState("Vẽ tay tự do");
                    var inkAnn = new PdfAnnotationElement(PdfAnnotationType.InkDrawing, RectangleF.Empty, ActiveColor);
                    inkAnn.InkPoints = new List<PointF>(CurrentFreehandPoints);
                    inkAnn.StrokeWidth = ActiveStrokeWidth;
                    Document.Pages[DrawingPageIndex].Annotations.Add(inkAnn);
                    Document.Pages[DrawingPageIndex].InvalidateCache();
                    Document.IsModified = true;
                }
                IsDrawingFreehand = false;
                CurrentFreehandPoints.Clear();
                return true;
            }

            // Kết thúc kéo chọn vùng Box Selection (Highlight hoặc Rectangle Shape)
            if (IsBoxSelecting)
            {
                int pageIdx = GetPageIndexAtPoint(x, y, width, height, topOffset);
                if (pageIdx >= 0 && pageIdx < Document.Pages.Count)
                {
                    PointF p1 = ScreenToPagePoint(BoxSelectStart, pageIdx, width, height, topOffset);
                    PointF p2 = ScreenToPagePoint(new PointF(x, y), pageIdx, width, height, topOffset);

                    float minX = Math.Min(p1.X, p2.X);
                    float minY = Math.Min(p1.Y, p2.Y);
                    float bw = Math.Abs(p2.X - p1.X);
                    float bh = Math.Abs(p2.Y - p1.Y);

                    if (bw > 5 && bh > 5)
                    {
                        if (SelectedTool == 3) // Highlight
                        {
                            PushUndoState("Tô màu dạ quang");
                            var hAnn = new PdfAnnotationElement(PdfAnnotationType.Highlight, new RectangleF(minX, minY, bw, bh), ActiveColor);
                            Document.Pages[pageIdx].Annotations.Add(hAnn);
                            Document.Pages[pageIdx].InvalidateCache();
                            Document.IsModified = true;
                            SelectedAnnotation = hAnn;
                            SelectedTool = 0;
                        }
                        else if (SelectedTool == 8) // Rectangle Shape
                        {
                            PushUndoState("Vẽ hình chữ nhật");
                            var shp = new PdfShapeElement(PdfShapeType.Rectangle, new RectangleF(minX, minY, bw, bh));
                            shp.StrokeColor = ActiveColor;
                            Document.Pages[pageIdx].ShapeElements.Add(shp);
                            Document.Pages[pageIdx].InvalidateCache();
                            Document.IsModified = true;
                            SelectedShape = shp;
                            SelectedTool = 0;
                        }
                        else if (SelectedTool == 17) // Mark Redaction
                        {
                            PushUndoState("Đánh dấu xóa mật");
                            var red = new PdfRedactionElement(new RectangleF(minX, minY, bw, bh), "BẢO MẬT");
                            Document.Pages[pageIdx].Redactions.Add(red);
                            Document.Pages[pageIdx].InvalidateCache();
                            Document.IsModified = true;
                            SelectedRedaction = red;
                            SelectedTool = 0;
                        }
                    }
                }
                IsBoxSelecting = false;
                return true;
            }

            if (IsDraggingObject)
            {
                IsDraggingObject = false;
                return true;
            }

            return false;
        }

        public void StartInlineEdit(int pageIdx, PdfTextElement txt, int clickScreenX = -1, int viewWidth = 1000, int viewHeight = 1000)
        {
            if (txt == null) return;
            _hasSavedUndoForCurrentEdit = false;
            EditingText = txt;
            EditingPageIndex = pageIdx;
            SelectedText = txt;
            if (!string.IsNullOrEmpty(txt.FontName)) ActiveFontName = txt.FontName;
            if (txt.FontSize > 0) ActiveFontSize = txt.FontSize;
            ActiveColor = txt.TextColor;

            if (txt.IsPlaceholder)
            {
                EditingBuffer = "";
                CursorPosition = 0; // Con trỏ ở đầu!
            }
            else
            {
                EditingBuffer = txt.Text ?? "";
                CursorPosition = EditingBuffer.Length;

                if (clickScreenX >= 0)
                {
                    RepositionCursor(pageIdx, txt, clickScreenX, viewWidth, viewHeight);
                }
            }
            ShowCursor = true;
        }

        public void RepositionCursor(int pageIdx, PdfTextElement txt, int clickScreenX, int viewWidth, int viewHeight)
        {
            if (txt == null || string.IsNullOrEmpty(EditingBuffer) || clickScreenX < 0 || Document == null || pageIdx >= Document.Pages.Count) return;
            try
            {
                RectangleF pBounds = GetPageScreenBounds(pageIdx, viewWidth, viewHeight, 155);
                float textScreenX = pBounds.X + (txt.Bounds.X * ZoomLevel) + 2;
                float relX = clickScreenX - textScreenX;

                if (relX <= 0)
                {
                    CursorPosition = 0;
                    return;
                }

                float scaledFs = Math.Max(6f, txt.FontSize * ZoomLevel);
                FontStyle fs = FontStyle.Regular;
                if (txt.IsBold) fs |= FontStyle.Bold;
                if (txt.IsItalic) fs |= FontStyle.Italic;
                string fName = string.IsNullOrEmpty(txt.FontName) ? "Times New Roman" : txt.FontName;

                using (Bitmap tmp = new Bitmap(1, 1))
                using (Graphics tg = Graphics.FromImage(tmp))
                using (Font f = new Font(fName, scaledFs, fs, GraphicsUnit.Pixel))
                {
                    int bestIdx = 0;
                    float bestDist = float.MaxValue;
                    for (int i = 0; i <= EditingBuffer.Length; i++)
                    {
                        string sub = EditingBuffer.Substring(0, i);
                        SizeF sz = tg.MeasureString(sub, f, new PointF(0, 0), StringFormat.GenericTypographic);
                        float dist = Math.Abs(sz.Width - relX);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIdx = i;
                        }
                    }
                    CursorPosition = bestIdx;
                }
            }
            catch { }
        }

        public bool HandleDoubleClick(int x, int y, int width, int height)
        {
            int topOffset = 155;
            int pageIdx = GetPageIndexAtPoint(x, y, width, height, topOffset);
            if (pageIdx >= 0 && pageIdx < Document.Pages.Count)
            {
                PointF pagePt = ScreenToPagePoint(new PointF(x, y), pageIdx, width, height, topOffset);
                var page = Document.Pages[pageIdx];

                for (int t = page.TextElements.Count - 1; t >= 0; t--)
                {
                    if (page.TextElements[t].Bounds.Contains(pagePt))
                    {
                        StartInlineEdit(pageIdx, page.TextElements[t], x, width, height);
                        return true;
                    }
                }
            }
            return false;
        }

        public void HandleKeyDown(Keys keyCode)
        {
            if (EditingFormField != null)
            {
                if (keyCode == Keys.Escape || keyCode == Keys.Enter)
                {
                    CommitFormFieldEdit();
                    return;
                }
                if (keyCode == Keys.Back)
                {
                    if (!string.IsNullOrEmpty(EditingFormFieldBuffer))
                    {
                        EditingFormFieldBuffer = EditingFormFieldBuffer.Substring(0, EditingFormFieldBuffer.Length - 1);
                        EditingFormField.Value = EditingFormFieldBuffer;
                        if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                        Document.IsModified = true;
                    }
                    return;
                }
            }

            if (ShowSearchBar)
            {
                if (keyCode == Keys.Escape)
                {
                    ShowSearchBar = false;
                    IsSearchInputFocused = false;
                    SearchQuery = "";
                    SearchMatchCount = 0;
                    if (SearchMatches != null) SearchMatches.Clear();
                    InvalidateAllCaches();
                    return;
                }
                if (IsSearchInputFocused)
                {
                    if (keyCode == Keys.Back)
                    {
                        if (!string.IsNullOrEmpty(SearchQuery))
                        {
                            SearchQuery = SearchQuery.Substring(0, SearchQuery.Length - 1);
                            ExecuteSearch(SearchQuery);
                        }
                        return;
                    }
                    if (keyCode == Keys.Enter || keyCode == Keys.Down)
                    {
                        NavigateSearchMatch(1);
                        return;
                    }
                    if (keyCode == Keys.Up)
                    {
                        NavigateSearchMatch(-1);
                        return;
                    }
                }
            }

            bool isCtrl = (Control.ModifierKeys & Keys.Control) != 0;
            bool isShift = (Control.ModifierKeys & Keys.Shift) != 0;

            // Xử lý các tổ hợp phím tắt Ctrl
            if (isCtrl)
            {
                if (keyCode == Keys.Z)
                {
                    Undo();
                    return;
                }
                if (keyCode == Keys.Y)
                {
                    Redo();
                    return;
                }
                if (keyCode == Keys.C)
                {
                    if (EditingText != null)
                    {
                        if (!string.IsNullOrEmpty(EditingBuffer)) Clipboard.SetText(EditingBuffer);
                    }
                    else if (SelectedText != null)
                    {
                        if (!string.IsNullOrEmpty(SelectedText.Text)) Clipboard.SetText(SelectedText.Text);
                    }
                    return;
                }
                if (keyCode == Keys.V)
                {
                    if (Clipboard.ContainsText())
                    {
                        string clipText = Clipboard.GetText();
                        if (EditingText != null)
                        {
                            if (EditingText.IsPlaceholder)
                            {
                                EditingText.IsPlaceholder = false;
                                EditingBuffer = "";
                                CursorPosition = 0;
                            }
                            int cur = Math.Max(0, Math.Min(CursorPosition, EditingBuffer.Length));
                            EditingBuffer = EditingBuffer.Insert(cur, clipText);
                            CursorPosition = cur + clipText.Length;
                            EditingText.Text = EditingBuffer;
                        }
                        else if (ActivePageIndex < Document.Pages.Count)
                        {
                            PushUndoState("Dán hộp chữ");
                            var pastedTxt = new PdfTextElement(clipText, new RectangleF(60, 60, Math.Max(160, clipText.Length * 9), 32), "Segoe UI", 12f);
                            Document.Pages[ActivePageIndex].TextElements.Add(pastedTxt);
                            SelectedText = pastedTxt;
                            Document.Pages[ActivePageIndex].InvalidateCache();
                        }
                        Document.IsModified = true;
                    }
                    return;
                }
                if (keyCode == Keys.B)
                {
                    if (SelectedText != null) { SelectedText.IsBold = !SelectedText.IsBold; Document.IsModified = true; }
                    return;
                }
                if (keyCode == Keys.I)
                {
                    if (SelectedText != null) { SelectedText.IsItalic = !SelectedText.IsItalic; Document.IsModified = true; }
                    return;
                }
                if (keyCode == Keys.U)
                {
                    if (SelectedText != null) { SelectedText.IsUnderline = !SelectedText.IsUnderline; Document.IsModified = true; }
                    return;
                }
                if (keyCode == Keys.D)
                {
                    if (SelectedText != null && ActivePageIndex < Document.Pages.Count)
                    {
                        PushUndoState("Nhân bản hộp chữ");
                        var dup = SelectedText.Clone();
                        dup.Bounds = new RectangleF(dup.Bounds.X + 20, dup.Bounds.Y + 20, dup.Bounds.Width, dup.Bounds.Height);
                        Document.Pages[ActivePageIndex].TextElements.Add(dup);
                        SelectedText = dup;
                        Document.Pages[ActivePageIndex].InvalidateCache();
                        Document.IsModified = true;
                    }
                    return;
                }
            }

            if (EditingText != null)
            {
                if (keyCode == Keys.Enter)
                {
                    CommitInlineEdit();
                }
                else if (keyCode == Keys.Escape)
                {
                    CommitInlineEdit();
                    SelectedText = null;
                }
                else if (Control.ModifierKeys == Keys.Control && keyCode == Keys.A)
                {
                    CursorPosition = EditingBuffer.Length;
                }
                else if (Control.ModifierKeys == Keys.Control && keyCode == Keys.C)
                {
                    try { if (!string.IsNullOrEmpty(EditingBuffer)) Clipboard.SetText(EditingBuffer); } catch { }
                }
                else if (Control.ModifierKeys == Keys.Control && keyCode == Keys.V)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            string pasteTxt = Clipboard.GetText();
                            if (!string.IsNullOrEmpty(pasteTxt))
                            {
                                if (EditingBuffer == null) EditingBuffer = "";
                                int cur = Math.Max(0, Math.Min(CursorPosition, EditingBuffer.Length));
                                EditingBuffer = EditingBuffer.Insert(cur, pasteTxt);
                                CursorPosition = cur + pasteTxt.Length;
                                EditingText.Text = EditingBuffer;
                                Document.IsModified = true;
                            }
                        }
                    }
                    catch { }
                }
                else if (keyCode == Keys.Delete)
                {
                    if (EditingText.IsPlaceholder)
                    {
                        EditingText.IsPlaceholder = false;
                        EditingBuffer = "";
                        CursorPosition = 0;
                    }
                    else if (CursorPosition < EditingBuffer.Length)
                    {
                        EditingBuffer = EditingBuffer.Remove(CursorPosition, 1);
                        EditingText.Text = EditingBuffer;
                        Document.IsModified = true;
                    }
                }
                else if (keyCode == Keys.Left && CursorPosition > 0)
                {
                    CursorPosition--;
                }
                else if (keyCode == Keys.Right && CursorPosition < EditingBuffer.Length)
                {
                    CursorPosition++;
                }
                else if (keyCode == Keys.Home)
                {
                    CursorPosition = 0;
                }
                else if (keyCode == Keys.End)
                {
                    CursorPosition = EditingBuffer.Length;
                }
            }
            else
            {
                if (keyCode == Keys.Delete)
                {
                    if (SelectedText != null && ActivePageIndex < Document.Pages.Count)
                    {
                        PushUndoState("Xóa hộp chữ");
                        Document.Pages[ActivePageIndex].TextElements.Remove(SelectedText);
                        Document.Pages[ActivePageIndex].InvalidateCache();
                        SelectedText = null;
                        Document.IsModified = true;
                    }
                    else if (SelectedShape != null && ActivePageIndex < Document.Pages.Count)
                    {
                        PushUndoState("Xóa hình khối");
                        Document.Pages[ActivePageIndex].ShapeElements.Remove(SelectedShape);
                        Document.Pages[ActivePageIndex].InvalidateCache();
                        SelectedShape = null;
                        Document.IsModified = true;
                    }
                    else if (SelectedSignature != null && ActivePageIndex < Document.Pages.Count)
                    {
                        PushUndoState("Xóa chữ ký");
                        Document.Pages[ActivePageIndex].Signatures.Remove(SelectedSignature);
                        Document.Pages[ActivePageIndex].InvalidateCache();
                        SelectedSignature = null;
                        Document.IsModified = true;
                    }
                    else if (SelectedAnnotation != null && ActivePageIndex < Document.Pages.Count)
                    {
                        PushUndoState("Xóa con dấu / chú thích");
                        Document.Pages[ActivePageIndex].Annotations.Remove(SelectedAnnotation);
                        Document.Pages[ActivePageIndex].InvalidateCache();
                        SelectedAnnotation = null;
                        Document.IsModified = true;
                    }
                }
                else if (keyCode == Keys.Escape)
                {
                    SelectedText = null;
                    SelectedShape = null;
                    SelectedSignature = null;
                    SelectedAnnotation = null;
                }
            }
        }

        public void HandleKeyPress(char keyChar)
        {
            if (ShowSearchBar && IsSearchInputFocused)
            {
                if (!char.IsControl(keyChar))
                {
                    SearchQuery += keyChar;
                    ExecuteSearch(SearchQuery);
                    return;
                }
                if (keyChar == '\b')
                {
                    if (!string.IsNullOrEmpty(SearchQuery))
                    {
                        SearchQuery = SearchQuery.Substring(0, SearchQuery.Length - 1);
                        ExecuteSearch(SearchQuery);
                    }
                    return;
                }
                if (keyChar == '\r' || keyChar == '\n')
                {
                    NavigateSearchMatch(1);
                    return;
                }
            }

            if (EditingFormField != null)
            {
                if (keyChar == '\r' || keyChar == '\n')
                {
                    CommitFormFieldEdit();
                    return;
                }
                if (keyChar == '\b')
                {
                    if (!string.IsNullOrEmpty(EditingFormFieldBuffer))
                    {
                        EditingFormFieldBuffer = EditingFormFieldBuffer.Substring(0, EditingFormFieldBuffer.Length - 1);
                        EditingFormField.Value = EditingFormFieldBuffer;
                        if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                        Document.IsModified = true;
                    }
                    return;
                }
                if (!char.IsControl(keyChar))
                {
                    EditingFormFieldBuffer += keyChar;
                    EditingFormField.Value = EditingFormFieldBuffer;
                    if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                    Document.IsModified = true;
                    return;
                }
            }

            if (EditingText != null)
            {
                if (!char.IsControl(keyChar) || keyChar == '\b' || keyChar == 22 || keyChar == '\x16')
                {
                    if (!_hasSavedUndoForCurrentEdit)
                    {
                        PushUndoState("Sửa văn bản");
                        _hasSavedUndoForCurrentEdit = true;
                    }
                }

                if (EditingText.IsPlaceholder)
                {
                    EditingText.IsPlaceholder = false;
                    EditingBuffer = "";
                    CursorPosition = 0;
                }

                if (keyChar == '\r' || keyChar == '\n')
                {
                    CommitInlineEdit();
                    return;
                }

                // Xử lý phím Backspace '\b' (0x08) từ bàn phím hoặc bộ gõ tiếng Việt (UniKey / EVKey Telex / VNI)
                if (keyChar == '\b')
                {
                    if (EditingBuffer != null && EditingBuffer.Length > 0 && CursorPosition > 0)
                    {
                        EditingBuffer = EditingBuffer.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                        EditingText.Text = EditingBuffer;
                        Document.IsModified = true;
                    }
                    return;
                }

                // Xử lý phím tắt dán Ctrl+V (0x16) khi bộ gõ tiếng Việt dùng chế độ Clipboard
                if (keyChar == 22 || keyChar == '\x16')
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            string pasteTxt = Clipboard.GetText();
                            if (!string.IsNullOrEmpty(pasteTxt))
                            {
                                if (EditingBuffer == null) EditingBuffer = "";
                                int cur = Math.Max(0, Math.Min(CursorPosition, EditingBuffer.Length));
                                EditingBuffer = EditingBuffer.Insert(cur, pasteTxt);
                                CursorPosition = cur + pasteTxt.Length;
                                EditingText.Text = EditingBuffer;
                                Document.IsModified = true;
                            }
                        }
                    }
                    catch { }
                    return;
                }

                if (!char.IsControl(keyChar))
                {
                    if (EditingBuffer == null) EditingBuffer = "";
                    int cur = Math.Max(0, Math.Min(CursorPosition, EditingBuffer.Length));
                    EditingBuffer = EditingBuffer.Insert(cur, keyChar.ToString());
                    CursorPosition = cur + 1;
                    EditingText.Text = EditingBuffer;
                    Document.IsModified = true;
                }
            }
        }

        public void CommitInlineEdit()
        {
            if (EditingText != null)
            {
                EditingText.Text = EditingBuffer;
                if (EditingText.IsPlaceholder && string.IsNullOrEmpty(EditingBuffer))
                {
                    // Nếu là placeholder nhưng người dùng không gõ gì và bỏ chọn -> xóa placeholder rỗng
                    if (EditingPageIndex < Document.Pages.Count)
                    {
                        Document.Pages[EditingPageIndex].TextElements.Remove(EditingText);
                    }
                }
                else
                {
                    EditingText.IsPlaceholder = false;
                }

                if (EditingPageIndex < Document.Pages.Count)
                {
                    Document.Pages[EditingPageIndex].InvalidateCache();
                }
                Document.IsModified = true;
                EditingText = null;
                EditingBuffer = "";
            }
        }

        public Cursor GetCursorAtPoint(int x, int y, int width, int height, int topOffset)
        {
            // 0. Con trỏ trên thanh Khung Bầu Liquid Glass
            if (SelectedTool == 7 || SelectedTool == 8 || SelectedShape != null)
            {
                RectangleF pillRect = GetLiquidGlassPaletteRect(width, height, 26);
                if (pillRect.Contains(x, y)) return Cursors.Hand;
            }

            if (IsResizingHandle)
            {
                switch (DraggingHandleIndex)
                {
                    case 0:
                    case 4: return Cursors.SizeNWSE;
                    case 2:
                    case 6: return Cursors.SizeNESW;
                    case 1:
                    case 5: return Cursors.SizeNS;
                    case 3:
                    case 7: return Cursors.SizeWE;
                }
            }

            // Kiểm tra 8 chốt vuông của Khung vuông đang chọn
            if (SelectedTool == 0 && SelectedShape != null && ActivePageIndex >= 0 && ActivePageIndex < Document.Pages.Count)
            {
                RectangleF pageBounds = GetPageScreenBounds(ActivePageIndex, width, height, topOffset);
                float sX = pageBounds.X + (SelectedShape.Bounds.X * ZoomLevel);
                float sY = pageBounds.Y + (SelectedShape.Bounds.Y * ZoomLevel);
                float sW = SelectedShape.Bounds.Width * ZoomLevel;
                float sH = SelectedShape.Bounds.Height * ZoomLevel;
                RectangleF sRect = new RectangleF(sX, sY, sW, sH);

                int hIdx = HitTestResizeHandles(sRect, new PointF(x, y));
                if (hIdx >= 0) return GetResizeHandleCursor(hIdx);

                if (sRect.Contains(x, y)) return Cursors.SizeAll;
            }

            PdfTextElement activeTxt = SelectedText ?? EditingText;
            if (SelectedTool == 0 && activeTxt != null && ActivePageIndex >= 0 && ActivePageIndex < Document.Pages.Count)
            {
                RectangleF pageBounds = GetPageScreenBounds(ActivePageIndex, width, height, topOffset);
                float screenX = pageBounds.X + (activeTxt.Bounds.X * ZoomLevel);
                float screenY = pageBounds.Y + (activeTxt.Bounds.Y * ZoomLevel);
                float screenW = Math.Max(20f, activeTxt.Bounds.Width * ZoomLevel);
                float screenH = Math.Max(15f, activeTxt.Bounds.Height * ZoomLevel);
                RectangleF selScreenRect = new RectangleF(screenX, screenY, screenW, screenH);

                PointF[] handles = new PointF[] {
                    new PointF(selScreenRect.Left, selScreenRect.Top),
                    new PointF(selScreenRect.Left + selScreenRect.Width / 2f, selScreenRect.Top),
                    new PointF(selScreenRect.Right, selScreenRect.Top),
                    new PointF(selScreenRect.Right, selScreenRect.Top + selScreenRect.Height / 2f),
                    new PointF(selScreenRect.Right, selScreenRect.Bottom),
                    new PointF(selScreenRect.Left + selScreenRect.Width / 2f, selScreenRect.Bottom),
                    new PointF(selScreenRect.Left, selScreenRect.Bottom),
                    new PointF(selScreenRect.Left, selScreenRect.Top + selScreenRect.Height / 2f)
                };

                for (int h = 0; h < handles.Length; h++)
                {
                    if (Math.Abs(x - handles[h].X) <= 10 && Math.Abs(y - handles[h].Y) <= 10)
                    {
                        switch (h)
                        {
                            case 0:
                            case 4: return Cursors.SizeNWSE;
                            case 2:
                            case 6: return Cursors.SizeNESW;
                            case 1:
                            case 5: return Cursors.SizeNS;
                            case 3:
                            case 7: return Cursors.SizeWE;
                        }
                    }
                }

                if (selScreenRect.Contains(x, y))
                {
                    if (EditingText == activeTxt) return Cursors.IBeam;
                    return Cursors.SizeAll;
                }
            }

            if (SelectedTool == 1 || SelectedTool == 2) return Cursors.IBeam;
            if (SelectedTool == 7 || SelectedTool == 8 || SelectedTool == 3) return Cursors.Cross;
            return Cursors.Default;
        }

        public void ScrollToPage(int pageIndex, int viewWidth, int viewHeight)
        {
            if (Document == null || pageIndex < 0 || pageIndex >= Document.Pages.Count) return;
            float yAcc = 0;
            for (int i = 0; i < pageIndex; i++)
            {
                yAcc += (Document.Pages[i].Height * ZoomLevel) + 30f;
            }
            int vH = Math.Max(100, viewHeight - _lastTopOffset - _lastStatusH);
            int maxScroll = GetMaxScrollY(vH);
            TargetScrollY = Math.Max(0, Math.Min(maxScroll, yAcc));
            ScrollY = TargetScrollY;
        }

        public void ExecuteSearch(string query)
        {
            SearchQuery = query;
            if (SearchMatches == null) SearchMatches = new List<PdfSearchMatch>();
            SearchMatches.Clear();
            CurrentSearchIndex = 0;
            if (string.IsNullOrEmpty(query) || Document == null)
            {
                SearchMatchCount = 0;
                InvalidateAllCaches();
                return;
            }

            // Đảm bảo văn bản các trang đã được trích xuất
            for (int p = 0; p < Document.Pages.Count; p++)
            {
                if (Document.Pages[p].TextElements.Count == 0 && Document.UnderlyingPdfiumDoc != null)
                {
                    AutoDetectTextForPage(p);
                }
            }

            string qLower = query.ToLower();
            for (int p = 0; p < Document.Pages.Count; p++)
            {
                var page = Document.Pages[p];
                for (int t = 0; t < page.TextElements.Count; t++)
                {
                    var txt = page.TextElements[t];
                    if (txt.Text != null && txt.Text.ToLower().Contains(qLower))
                    {
                        SearchMatches.Add(new PdfSearchMatch { PageIndex = p, Bounds = txt.Bounds, Text = txt.Text });
                    }
                }
            }

            SearchMatchCount = SearchMatches.Count;
            if (SearchMatchCount > 0)
            {
                ScrollToSearchMatch(0);
            }
            InvalidateAllCaches();
        }

        public void NavigateSearchMatch(int dir, int width = 1000, int height = 800, int topOffset = 155)
        {
            if (SearchMatches == null || SearchMatches.Count == 0) return;
            CurrentSearchIndex = (CurrentSearchIndex + dir + SearchMatches.Count) % SearchMatches.Count;
            ScrollToSearchMatch(CurrentSearchIndex, width, height, topOffset);
            InvalidateAllCaches();
        }

        public void ScrollToSearchMatch(int index, int width = 1000, int height = 800, int topOffset = 155)
        {
            if (SearchMatches == null || index < 0 || index >= SearchMatches.Count) return;
            var match = SearchMatches[index];
            ActivePageIndex = match.PageIndex;
            ScrollToPage(match.PageIndex, width, height);
        }
        #endregion

        #region Smart Zoom Algorithms (Fit Width, Fit Height, Fit Best, Actual Size, Anchored Zoom)
        public void ZoomAt(Point anchorScreenPt, float newZoom, int viewWidth = 0, int viewHeight = 0, int topOffset = 155, int statusH = 26)
        {
            if (Document == null || Document.Pages == null || Document.Pages.Count == 0) return;

            newZoom = Math.Max(0.20f, Math.Min(4.00f, (float)Math.Round(newZoom, 2)));
            float oldZoom = ZoomLevel;
            if (Math.Abs(newZoom - oldZoom) < 0.001f) return;

            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;

            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int canvasAreaW = Math.Max(100, viewWidth - sideOffset - 14);
            int viewH = Math.Max(100, viewHeight - topOffset - statusH);

            // If anchorScreenPt is out of the page viewport (e.g. in sidebar, ribbon or status bar), anchor to center of viewport
            if (anchorScreenPt.X < sideOffset || anchorScreenPt.X > viewWidth - 14 ||
                anchorScreenPt.Y < topOffset || anchorScreenPt.Y > viewHeight - statusH)
            {
                anchorScreenPt = new Point(sideOffset + canvasAreaW / 2, topOffset + viewH / 2);
            }

            // 1. Identify which page and unscaled page coordinates are under anchorScreenPt before zoom
            int targetPageIdx = -1;
            float targetLogicalY = 0f;
            float targetLogicalX = 0f;

            for (int i = 0; i < Document.Pages.Count; i++)
            {
                RectangleF pb = GetPageScreenBounds(i, viewWidth, viewHeight, topOffset);
                if (anchorScreenPt.Y >= pb.Top && anchorScreenPt.Y <= pb.Bottom + 30f)
                {
                    targetPageIdx = i;
                    targetLogicalY = Math.Max(0f, Math.Min(Document.Pages[i].Height, (anchorScreenPt.Y - pb.Top) / Math.Max(0.01f, oldZoom)));
                    targetLogicalX = Math.Max(0f, Math.Min(Document.Pages[i].Width, (anchorScreenPt.X - pb.Left) / Math.Max(0.01f, oldZoom)));
                    break;
                }
            }

            if (targetPageIdx < 0)
            {
                RectangleF firstPb = GetPageScreenBounds(0, viewWidth, viewHeight, topOffset);
                if (anchorScreenPt.Y < firstPb.Top)
                {
                    targetPageIdx = 0;
                    targetLogicalY = 0f;
                    targetLogicalX = Math.Max(0f, (anchorScreenPt.X - firstPb.Left) / Math.Max(0.01f, oldZoom));
                }
                else
                {
                    targetPageIdx = Math.Max(0, Math.Min(Document.Pages.Count - 1, ActivePageIndex));
                    RectangleF pb = GetPageScreenBounds(targetPageIdx, viewWidth, viewHeight, topOffset);
                    targetLogicalY = Math.Max(0f, Math.Min(Document.Pages[targetPageIdx].Height, (anchorScreenPt.Y - pb.Top) / Math.Max(0.01f, oldZoom)));
                    targetLogicalX = Math.Max(0f, Math.Min(Document.Pages[targetPageIdx].Width, (anchorScreenPt.X - pb.Left) / Math.Max(0.01f, oldZoom)));
                }
            }

            // 2. Apply new zoom level
            ZoomLevel = newZoom;
            InvalidateAllCaches();

            // 3. Compute new document Y position for targetPageIdx with new zoom
            float newPageDocY = topOffset + 30f;
            for (int i = 0; i < targetPageIdx; i++)
            {
                newPageDocY += (Document.Pages[i].Height * newZoom) + 30f;
            }

            // Screen position of the anchor point remains unchanged:
            // anchorScreenPt.Y = newPageDocY + (targetLogicalY * newZoom) - newScrollY
            float targetDocY = newPageDocY + (targetLogicalY * newZoom);
            float newScrollY = targetDocY - anchorScreenPt.Y;

            int maxScrollY = GetMaxScrollY(viewH);
            TargetScrollY = Math.Max(0, Math.Min(maxScrollY, newScrollY));
            ScrollY = TargetScrollY;

            // 4. Compute horizontal scroll adjustment
            var targetPage = Document.Pages[targetPageIdx];
            float newRenderW = targetPage.Width * newZoom;
            if (newRenderW > canvasAreaW)
            {
                float newPageScreenLeftBeforeScrollX = sideOffset + 30f;
                float targetDocX = newPageScreenLeftBeforeScrollX + (targetLogicalX * newZoom);
                float newScrollX = targetDocX - anchorScreenPt.X;
                float maxScrollX = Math.Max(0f, newRenderW - canvasAreaW + 60f);
                ScrollX = Math.Max(0f, Math.Min(maxScrollX, newScrollX));
            }
            else
            {
                ScrollX = 0f;
            }

            // 5. Update active page index to match current viewport
            UpdateActivePageFromScroll(viewWidth, viewHeight, topOffset, statusH);
        }

        public void ZoomFitWidth(int viewWidth = 0, int viewHeight = 0, int topOffset = 155, int statusH = 26)
        {
            if (Document == null || Document.Pages.Count == 0) return;
            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;

            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            float availableW = Math.Max(100, viewWidth - sideOffset - 60);

            int targetPage = (ActivePageIndex >= 0 && ActivePageIndex < Document.Pages.Count) ? ActivePageIndex : 0;
            var activePage = Document.Pages[targetPage];

            float targetZoom = availableW / activePage.Width;
            ZoomLevel = Math.Max(0.2f, Math.Min(4.0f, (float)Math.Round(targetZoom, 2)));
            ScrollX = 0f;
            ScrollToPage(targetPage, viewWidth, viewHeight);
            ScrollY = TargetScrollY;
            InvalidateAllCaches();
        }

        public void ZoomFitHeight(int viewWidth = 0, int viewHeight = 0, int topOffset = 155, int statusH = 26)
        {
            if (Document == null || Document.Pages.Count == 0) return;
            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;

            float availableH = Math.Max(100, viewHeight - topOffset - statusH - 60);

            int targetPage = (ActivePageIndex >= 0 && ActivePageIndex < Document.Pages.Count) ? ActivePageIndex : 0;
            var activePage = Document.Pages[targetPage];

            float targetZoom = availableH / activePage.Height;
            ZoomLevel = Math.Max(0.2f, Math.Min(4.0f, (float)Math.Round(targetZoom, 2)));
            ScrollX = 0f;
            ScrollToPage(targetPage, viewWidth, viewHeight);
            ScrollY = TargetScrollY;
            InvalidateAllCaches();
        }

        public void ZoomFitBest(int viewWidth = 0, int viewHeight = 0, int topOffset = 155, int statusH = 26)
        {
            if (Document == null || Document.Pages.Count == 0) return;
            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;

            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            float availableW = Math.Max(100, viewWidth - sideOffset - 60);
            float availableH = Math.Max(100, viewHeight - topOffset - statusH - 60);

            int targetPage = (ActivePageIndex >= 0 && ActivePageIndex < Document.Pages.Count) ? ActivePageIndex : 0;
            var activePage = Document.Pages[targetPage];

            float zw = availableW / activePage.Width;
            float zh = availableH / activePage.Height;
            float targetZoom = Math.Min(zw, zh);

            ZoomLevel = Math.Max(0.2f, Math.Min(4.0f, (float)Math.Round(targetZoom, 2)));
            ScrollX = 0f;
            ScrollToPage(targetPage, viewWidth, viewHeight);
            ScrollY = TargetScrollY;
            InvalidateAllCaches();
        }

        public void ZoomActualSize(int viewWidth = 0, int viewHeight = 0)
        {
            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;
            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int canvasAreaW = Math.Max(100, viewWidth - sideOffset - 14);
            int viewH = Math.Max(100, viewHeight - _lastTopOffset - _lastStatusH);
            Point centerPt = new Point(sideOffset + canvasAreaW / 2, _lastTopOffset + viewH / 2);
            ZoomAt(centerPt, 1.0f, viewWidth, viewHeight, _lastTopOffset, _lastStatusH);
        }

        public void ZoomIn(int viewWidth = 0, int viewHeight = 0)
        {
            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;
            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int canvasAreaW = Math.Max(100, viewWidth - sideOffset - 14);
            int viewH = Math.Max(100, viewHeight - _lastTopOffset - _lastStatusH);
            Point centerPt = new Point(sideOffset + canvasAreaW / 2, _lastTopOffset + viewH / 2);
            ZoomAt(centerPt, (float)Math.Round(ZoomLevel + 0.15f, 2), viewWidth, viewHeight, _lastTopOffset, _lastStatusH);
        }

        public void ZoomOut(int viewWidth = 0, int viewHeight = 0)
        {
            if (viewWidth <= 0) viewWidth = _lastViewWidth > 0 ? _lastViewWidth : 1000;
            if (viewHeight <= 0) viewHeight = _lastViewHeight > 0 ? _lastViewHeight : 800;
            int sideOffset = ShowThumbnailsSidebar ? SidebarWidth : 0;
            int canvasAreaW = Math.Max(100, viewWidth - sideOffset - 14);
            int viewH = Math.Max(100, viewHeight - _lastTopOffset - _lastStatusH);
            Point centerPt = new Point(sideOffset + canvasAreaW / 2, _lastTopOffset + viewH / 2);
            ZoomAt(centerPt, (float)Math.Round(ZoomLevel - 0.15f, 2), viewWidth, viewHeight, _lastTopOffset, _lastStatusH);
        }
        #endregion

        #region Interactive PDF Processing Dialogs (Rotate, Delete, Merge, Split)
        public bool PromptRotateSpecificPage(Form parentForm)
        {
            if (Document == null || Document.Pages.Count == 0) return false;

            using (Form dlg = new Form())
            {
                dlg.Text = "Xoay Trang PDF - ZENQ PDF Engine";
                dlg.Size = new Size(380, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

                Label lblScope = new Label { Text = "Phạm vi xoay:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                RadioButton rbCurrent = new RadioButton { Text = "Trang hiện tại (Trang " + (ActivePageIndex + 1) + ")", Location = new Point(30, 48), AutoSize = true, Checked = true };
                RadioButton rbSpecific = new RadioButton { Text = "Trang số:", Location = new Point(30, 76), AutoSize = true };
                NumericUpDown numPage = new NumericUpDown { Location = new Point(120, 74), Width = 70, Minimum = 1, Maximum = Document.Pages.Count, Value = ActivePageIndex + 1 };
                RadioButton rbAll = new RadioButton { Text = "Tất cả các trang (" + Document.Pages.Count + " trang)", Location = new Point(30, 104), AutoSize = true };

                Label lblAngle = new Label { Text = "Góc xoay:", Location = new Point(210, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                ComboBox cbAngle = new ComboBox { Location = new Point(210, 48), Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
                cbAngle.Items.AddRange(new object[] { "90° Xuôi kim", "180° Đảo ngược", "270° Ngược kim" });
                cbAngle.SelectedIndex = 0;

                Button btnOk = new Button { Text = "Xoay trang", Location = new Point(160, 150), Width = 100, Height = 32, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Hủy bỏ", Location = new Point(270, 150), Width = 80, Height = 32, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };

                dlg.Controls.AddRange(new Control[] { lblScope, rbCurrent, rbSpecific, numPage, rbAll, lblAngle, cbAngle, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    int angle = 90;
                    if (cbAngle.SelectedIndex == 1) angle = 180;
                    else if (cbAngle.SelectedIndex == 2) angle = 270;

                    if (rbCurrent.Checked)
                    {
                        Document.RotatePage(ActivePageIndex, angle);
                    }
                    else if (rbSpecific.Checked)
                    {
                        int p = (int)numPage.Value - 1;
                        Document.RotatePage(p, angle);
                        ActivePageIndex = p;
                    }
                    else if (rbAll.Checked)
                    {
                        Document.RotateAllPages(angle);
                    }

                    InvalidateAllCaches();
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }
            }
            return false;
        }

        public bool PromptDeleteSpecificPage(Form parentForm)
        {
            if (Document == null || Document.Pages.Count == 0) return false;
            if (Document.Pages.Count <= 1)
            {
                MessageBox.Show("Không thể xóa trang duy nhất còn lại của tài liệu PDF.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            using (Form dlg = new Form())
            {
                dlg.Text = "Xóa Trang PDF - ZENQ PDF Engine";
                dlg.Size = new Size(360, 200);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

                Label lbl = new Label { Text = "Nhập số trang cần xóa (1 - " + Document.Pages.Count + "):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                NumericUpDown numPage = new NumericUpDown { Location = new Point(24, 52), Width = 100, Minimum = 1, Maximum = Document.Pages.Count, Value = ActivePageIndex + 1 };
                Label lblInfo = new Label { Text = "Hành động này sẽ xóa vĩnh viễn trang được chọn khỏi tài liệu.", Location = new Point(20, 85), Width = 310, Height = 36, ForeColor = Color.FromArgb(100, 116, 139) };

                Button btnDel = new Button { Text = "Xóa trang", Location = new Point(140, 120), Width = 100, Height = 32, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Hủy bỏ", Location = new Point(250, 120), Width = 80, Height = 32, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };

                dlg.Controls.AddRange(new Control[] { lbl, numPage, lblInfo, btnDel, btnCancel });
                dlg.AcceptButton = btnDel;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    int targetPageIdx = (int)numPage.Value - 1;
                    if (targetPageIdx >= 0 && targetPageIdx < Document.Pages.Count)
                    {
                        Document.DeletePage(targetPageIdx);
                        if (ActivePageIndex >= Document.Pages.Count) ActivePageIndex = Document.Pages.Count - 1;
                        InvalidateAllCaches();
                        if (parentForm != null) parentForm.Invalidate();
                        MessageBox.Show("Đã xóa trang " + (targetPageIdx + 1) + " thành công!", "ZENQ PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool PromptMergePdfs(Form parentForm, Action<PdfDocument, string> onMerged = null)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn nhiều tệp PDF cần ghép nối (Thuật toán Merge PDF)";
                ofd.Filter = "Tệp PDF (*.pdf)|*.pdf";
                ofd.Multiselect = true;

                if (ofd.ShowDialog(parentForm) == DialogResult.OK)
                {
                    if (ofd.FileNames.Length < 1) return false;

                    try
                    {
                        using (SaveFileDialog sfd = new SaveFileDialog())
                        {
                            sfd.Title = "Lưu tệp PDF hợp nhất mới";
                            sfd.Filter = "Tệp PDF (*.pdf)|*.pdf";
                            sfd.FileName = "Tài liệu PDF Hợp nhất.pdf";

                            if (sfd.ShowDialog(parentForm) == DialogResult.OK)
                            {
                                PdfDocument mergedDoc = PdfHandler.MergePdfs(ofd.FileNames, sfd.FileName);
                                if (mergedDoc != null)
                                {
                                    this.Document = mergedDoc;
                                    this.ActivePageIndex = 0;
                                    InvalidateAllCaches();
                                    if (parentForm != null) parentForm.Invalidate();

                                    if (onMerged != null)
                                    {
                                        onMerged(mergedDoc, sfd.FileName);
                                    }

                                    MessageBox.Show("Đã ghép nối " + ofd.FileNames.Length + " tệp PDF thành công!\nTệp lưu tại: " + sfd.FileName, "ZENQ PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi ghép tệp PDF: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return false;
        }

        public bool PromptSplitPdf(Form parentForm, Action<PdfDocument, string> onSplit = null)
        {
            if (Document == null || Document.Pages.Count == 0) return false;

            using (Form dlg = new Form())
            {
                dlg.Text = "Tách Tệp PDF - ZENQ PDF Engine";
                dlg.Size = new Size(420, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

                Label lbl = new Label { Text = "Nhập dải trang muốn trích xuất (Tổng số trang: " + Document.Pages.Count + "):", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                TextBox txtRange = new TextBox { Location = new Point(24, 46), Width = 355, Text = "1-" + Document.Pages.Count };
                Label lblHelp = new Label { Text = "Ví dụ cú pháp hợp lệ: 1-3, 5, 7-10 hoặc 2, 4, 6", Location = new Point(20, 80), Width = 360, AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139) };

                Button btnSplit = new Button { Text = "Tách PDF...", Location = new Point(195, 145), Width = 105, Height = 32, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Hủy bỏ", Location = new Point(310, 145), Width = 70, Height = 32, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };

                dlg.Controls.AddRange(new Control[] { lbl, txtRange, lblHelp, btnSplit, btnCancel });
                dlg.AcceptButton = btnSplit;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    string range = txtRange.Text.Trim();
                    if (string.IsNullOrEmpty(range)) range = "1-" + Document.Pages.Count;

                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Title = "Lưu tệp PDF đã tách";
                        sfd.Filter = "Tệp PDF (*.pdf)|*.pdf";
                        sfd.FileName = "Tách - " + Document.Title;

                        if (sfd.ShowDialog(parentForm) == DialogResult.OK)
                        {
                            try
                            {
                                PdfDocument splitDoc = PdfHandler.SplitPdf(Document, range, sfd.FileName);
                                if (splitDoc != null)
                                {
                                    if (onSplit != null)
                                    {
                                        onSplit(splitDoc, sfd.FileName);
                                    }
                                    MessageBox.Show("Đã tách " + splitDoc.Pages.Count + " trang PDF và lưu thành công tại:\n" + sfd.FileName, "ZENQ PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Lỗi khi tách PDF: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region AI OCR Magic Scanning Animation & Recognition
        public void StartOcrScan(Form parentForm = null)
        {
            if (Document == null || Document.Pages.Count == 0 || IsOcrScanning) return;

            OcrParentForm = parentForm;
            OcrTargetPageIndex = Math.Max(0, Math.Min(ActivePageIndex, Document.Pages.Count - 1));
            IsOcrScanning = true;
            OcrProgress = 0.0f;
            IsOcrBackgroundCompleted = false;
            OcrResultElements = null;
            OcrStatusMessage = "✨ Khởi động AI OCR...";

            // Bắt đầu tác vụ nhận dạng ngầm
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    var results = OcrHandler.RecognizePage(Document, OcrTargetPageIndex);
                    OcrResultElements = results;
                }
                catch { }
                finally
                {
                    IsOcrBackgroundCompleted = true;
                }
            });

            // Khởi động Timer hoạt họa quét ma thuật 60 FPS (~3.0 giây)
            if (OcrTimer != null)
            {
                OcrTimer.Stop();
                OcrTimer.Dispose();
                OcrTimer = null;
            }

            OcrTimer = new System.Windows.Forms.Timer();
            OcrTimer.Interval = 16; // ~60 FPS
            OcrTimer.Tick += (s, e) =>
            {
                if (!IsOcrScanning)
                {
                    if (OcrTimer != null) { OcrTimer.Stop(); OcrTimer.Dispose(); OcrTimer = null; }
                    return;
                }

                // Tiến trình quét từ từ trong ~2.8 đến 3.2 giây
                if (OcrProgress < 0.88f)
                {
                    OcrProgress += 0.0055f;
                }
                else if (OcrProgress < 1.0f)
                {
                    if (IsOcrBackgroundCompleted)
                    {
                        OcrProgress += 0.018f;
                    }
                    else
                    {
                        OcrProgress += 0.001f;
                    }
                }

                if (OcrProgress >= 1.0f && IsOcrBackgroundCompleted)
                {
                    if (OcrTimer != null) { OcrTimer.Stop(); OcrTimer.Dispose(); OcrTimer = null; }
                    IsOcrScanning = false;

                    if (OcrResultElements != null && OcrResultElements.Count > 0)
                    {
                        PushUndoState("Nhận diện văn bản AI OCR");
                        var page = Document.Pages[OcrTargetPageIndex];
                        page.TextElements.Clear();
                        foreach (var el in OcrResultElements)
                        {
                            page.TextElements.Add(el);
                        }
                        SelectedText = page.TextElements[0];
                        SelectedTool = 0;
                        Document.IsModified = true;
                        page.InvalidateCache();
                    }

                    if (OcrParentForm != null) OcrParentForm.Invalidate();
                    return;
                }

                // Cập nhật câu trạng thái hiển thị
                if (OcrProgress < 0.25f)
                    OcrStatusMessage = "✨ Đang quét phân tích bố cục quang học...";
                else if (OcrProgress < 0.65f)
                    OcrStatusMessage = "⚡ AI đang trích xuất các khối văn bản...";
                else if (OcrProgress < 0.88f)
                    OcrStatusMessage = "🎨 Đang lấy mẫu màu sắc & phông chữ...";
                else
                    OcrStatusMessage = "✨ Hoàn tất! Đang đưa khối chữ vào trang...";

                if (OcrParentForm != null) OcrParentForm.Invalidate();
            };
            OcrTimer.Start();
        }

        private void DrawOcrMagicOverlay(Graphics g, int width, int height, int topOffset, int statusH)
        {
            int viewH = height - topOffset - statusH;
            if (viewH <= 0) return;

            // 1. Lớp phủ màu trắng mờ Frosted Acrylic (Che toàn bộ màn hình bên dưới, TUYỆT ĐỐI KHÔNG che Ribbon)
            Rectangle overlayRect = new Rectangle(0, topOffset, width, viewH);
            using (SolidBrush frostBrush = new SolidBrush(Color.FromArgb(195, 252, 253, 255)))
            {
                g.FillRectangle(frostBrush, overlayRect);
            }

            RectangleF pBounds = GetPageScreenBounds(OcrTargetPageIndex, width, height, topOffset);
            if (pBounds.IsEmpty)
            {
                pBounds = new RectangleF(width * 0.15f, topOffset + 20, width * 0.70f, viewH - 40);
            }

            // Tọa độ Y của cây quét ma thuật nằm ngang
            float beamY = pBounds.Top + (pBounds.Height * Math.Min(1.0f, Math.Max(0.0f, OcrProgress)));

            float beamLeft = Math.Max(10f, pBounds.Left - 30f);
            float beamRight = Math.Min(width - 24f, pBounds.Right + 30f);
            float beamW = Math.Max(50f, beamRight - beamLeft);

            // 2. Vùng hào quang lan tỏa (Glow Curtain) phía trên và dưới cây quét
            float curtainH = 45f;
            RectangleF upperCurtain = new RectangleF(beamLeft, beamY - curtainH, beamW, curtainH);
            if (upperCurtain.Height > 0 && upperCurtain.Width > 0)
            {
                using (LinearGradientBrush upGlow = new LinearGradientBrush(
                    new PointF(beamLeft, beamY - curtainH),
                    new PointF(beamLeft, beamY),
                    Color.FromArgb(0, 6, 182, 212),
                    Color.FromArgb(50, 6, 182, 212)))
                {
                    g.FillRectangle(upGlow, upperCurtain);
                }
            }

            RectangleF lowerCurtain = new RectangleF(beamLeft, beamY, beamW, curtainH);
            if (lowerCurtain.Height > 0 && lowerCurtain.Width > 0)
            {
                using (LinearGradientBrush downGlow = new LinearGradientBrush(
                    new PointF(beamLeft, beamY),
                    new PointF(beamLeft, beamY + curtainH),
                    Color.FromArgb(50, 168, 85, 247),
                    Color.FromArgb(0, 168, 85, 247)))
                {
                    g.FillRectangle(downGlow, lowerCurtain);
                }
            }

            // 3. Cây quét ma thuật nằm ngang phát sáng rực rỡ (Glowing Horizontal Laser Wand)
            // Lớp phát sáng viền Neon
            using (LinearGradientBrush neonBar = new LinearGradientBrush(
                new PointF(beamLeft, beamY),
                new PointF(beamRight, beamY),
                Color.FromArgb(6, 182, 212),
                Color.FromArgb(217, 70, 239)
            ))
            {
                ColorBlend cb = new ColorBlend(4);
                cb.Colors = new Color[] {
                    Color.FromArgb(0, 6, 182, 212),
                    Color.FromArgb(220, 6, 182, 212),
                    Color.FromArgb(230, 217, 70, 239),
                    Color.FromArgb(0, 217, 70, 239)
                };
                cb.Positions = new float[] { 0.0f, 0.15f, 0.85f, 1.0f };
                neonBar.InterpolationColors = cb;

                using (Pen neonPen = new Pen(neonBar, 5.0f))
                {
                    g.DrawLine(neonPen, beamLeft, beamY, beamRight, beamY);
                }
            }

            // Lõi laser trắng tinh khiết ở giữa
            using (Pen corePen = new Pen(Color.White, 2.0f))
            {
                g.DrawLine(corePen, beamLeft + 15, beamY, beamRight - 15, beamY);
            }

            // 4. Các hạt ma thuật lấp lánh (Sparkling Stars) trôi theo cây quét
            for (int s = 0; s < 10; s++)
            {
                float frac = (s + 0.5f) / 10f;
                float starX = beamLeft + beamW * frac + (float)Math.Sin(OcrProgress * 25f + s * 1.5f) * 18f;
                float starY = beamY + (float)Math.Cos(OcrProgress * 20f + s * 2.1f) * 14f;
                float starSz = 3.5f + (float)Math.Sin(OcrProgress * 15f + s) * 1.5f;

                using (SolidBrush starB = new SolidBrush(s % 2 == 0 ? Color.FromArgb(240, 255, 255, 255) : Color.FromArgb(220, 103, 232, 249)))
                {
                    PointF[] starPts = new PointF[] {
                        new PointF(starX, starY - starSz),
                        new PointF(starX + starSz * 0.4f, starY),
                        new PointF(starX, starY + starSz),
                        new PointF(starX - starSz * 0.4f, starY)
                    };
                    g.FillPolygon(starB, starPts);
                }
            }

            // 5. Khối thông báo kính mờ nổi (Floating Glass Capsule) hiển thị phần trăm & trạng thái AI
            float capW = 380f;
            float capH = 50f;
            float capX = pBounds.X + (pBounds.Width - capW) / 2f;
            capX = Math.Max(20f, Math.Min(width - capW - 20f, capX));
            float capY = Math.Max(topOffset + 25f, Math.Min(height - statusH - capH - 25f, beamY - capH - 18f));

            RectangleF capRect = new RectangleF(capX, capY, capW, capH);
            using (GraphicsPath capPath = LiquidGlassHelper.CreateRoundedRectanglePath(capRect, 14f))
            {
                using (SolidBrush capBg = new SolidBrush(Color.FromArgb(235, 15, 23, 42)))
                {
                    g.FillPath(capBg, capPath);
                }
                using (Pen capBorder = new Pen(Color.FromArgb(180, 6, 182, 212), 1.8f))
                {
                    g.DrawPath(capBorder, capPath);
                }
            }

            // Biểu tượng & Nội dung bên trong Capsule
            using (Font statusFont = new Font("Segoe UI", 9.8f, FontStyle.Bold))
            using (Font pctFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (SolidBrush txtBrush = new SolidBrush(Color.White))
            using (SolidBrush pctBrush = new SolidBrush(Color.FromArgb(34, 211, 238)))
            {
                g.DrawString(OcrStatusMessage, statusFont, txtBrush, capX + 16, capY + 12);
                int pctVal = Math.Min(100, (int)(OcrProgress * 100));
                string pctStr = pctVal + "%";
                SizeF pSz = g.MeasureString(pctStr, pctFont);
                g.DrawString(pctStr, pctFont, pctBrush, capX + capW - pSz.Width - 16, capY + 11);
            }

            // Thanh tiến trình nhỏ đáy Capsule
            float pTrackW = capW - 32;
            float pFillW = pTrackW * Math.Min(1.0f, Math.Max(0.0f, OcrProgress));
            using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
            {
                g.FillRectangle(trackBg, capX + 16, capY + capH - 8, pTrackW, 3);
            }
            using (LinearGradientBrush fillBg = new LinearGradientBrush(
                new PointF(capX + 16, capY + capH - 8),
                new PointF(capX + 16 + pTrackW, capY + capH - 8),
                Color.FromArgb(6, 182, 212),
                Color.FromArgb(217, 70, 239)))
            {
                g.FillRectangle(fillBg, capX + 16, capY + capH - 8, pFillW, 3);
            }
        }
        #endregion

        #region AcroForms, Redaction, Bates Numbering & Bookmarks Management
        public void CommitFormFieldEdit()
        {
            if (EditingFormField != null)
            {
                EditingFormField.Value = EditingFormFieldBuffer;
                EditingFormField = null;
                EditingFormFieldBuffer = "";
                if (ActivePageIndex < Document.Pages.Count) Document.Pages[ActivePageIndex].InvalidateCache();
                Document.IsModified = true;
            }
        }

        public PdfFormFieldElement AddFormField(int pageIndex, PdfFormFieldType type, RectangleF bounds, string fieldName = "")
        {
            if (Document == null || pageIndex < 0 || pageIndex >= Document.Pages.Count) return null;
            PushUndoState("Thêm trường biểu mẫu");
            var ff = new PdfFormFieldElement(type, bounds, fieldName);
            Document.Pages[pageIndex].FormFields.Add(ff);
            Document.Pages[pageIndex].InvalidateCache();
            Document.IsModified = true;
            SelectedFormField = ff;
            return ff;
        }

        public int FlattenFormFields()
        {
            if (Document == null) return 0;
            PushUndoState("Làm phẳng biểu mẫu");
            int count = Document.FlattenFormFields();
            SelectedFormField = null;
            EditingFormField = null;
            EditingFormFieldBuffer = "";
            InvalidateAllCaches();
            return count;
        }

        public PdfRedactionElement MarkRedaction(int pageIndex, RectangleF bounds, string overlayText = "BẢO MẬT")
        {
            if (Document == null || pageIndex < 0 || pageIndex >= Document.Pages.Count) return null;
            PushUndoState("Đánh dấu xóa mật");
            var red = new PdfRedactionElement(bounds, overlayText);
            Document.Pages[pageIndex].Redactions.Add(red);
            Document.Pages[pageIndex].InvalidateCache();
            Document.IsModified = true;
            SelectedRedaction = red;
            return red;
        }

        public int ApplyRedactions()
        {
            if (Document == null) return 0;
            PushUndoState("Áp dụng xóa mật vĩnh viễn");
            int count = Document.ApplyRedactions();
            SelectedRedaction = null;
            InvalidateAllCaches();
            return count;
        }

        public void SanitizeDocument()
        {
            if (Document == null) return;
            PushUndoState("Làm sạch tài liệu");
            Document.SanitizeDocument();
            InvalidateAllCaches();
        }

        public void ApplyBatesNumbering(PdfBatesConfig config)
        {
            if (Document == null || config == null) return;
            PushUndoState("Đánh số Bates");
            Document.ApplyBatesNumbering(config);
            InvalidateAllCaches();
        }

        public void RemoveBatesNumbering()
        {
            if (Document == null) return;
            PushUndoState("Xóa số Bates");
            Document.RemoveBatesNumbering();
            InvalidateAllCaches();
        }

        public PdfBookmarkNode AddBookmark(string title, int pageIndex = -1, float scrollY = 0f, string parentId = null)
        {
            if (Document == null) return null;
            int pIdx = (pageIndex >= 0) ? pageIndex : ActivePageIndex;
            PushUndoState("Thêm dấu trang");
            var bm = Document.AddBookmark(title, pIdx, scrollY, parentId);
            SelectedBookmark = bm;
            return bm;
        }

        public void NavigateToBookmark(PdfBookmarkNode node)
        {
            if (node == null || Document == null) return;
            if (node.TargetPageIndex >= 0 && node.TargetPageIndex < Document.Pages.Count)
            {
                ActivePageIndex = node.TargetPageIndex;
                ScrollToPage(node.TargetPageIndex, 1000, 800);
            }
        }

        public bool PromptApplyBatesNumbering(Form parentForm)
        {
            if (Document == null || Document.Pages.Count == 0) return false;

            using (Form dlg = new Form())
            {
                dlg.Text = "Đánh Số Bates Pháp Lý - ZENQ PDF Legal Engine";
                dlg.Size = new Size(380, 310);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

                Label lblPrefix = new Label { Text = "Tiền tố (Prefix):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                TextBox txtPrefix = new TextBox { Text = "CONFIDENTIAL-", Location = new Point(20, 45), Width = 150 };

                Label lblStart = new Label { Text = "Số bắt đầu:", Location = new Point(190, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                NumericUpDown numStart = new NumericUpDown { Location = new Point(190, 45), Width = 80, Minimum = 1, Maximum = 1000000, Value = 1 };

                Label lblDigits = new Label { Text = "Số chữ số (Digits):", Location = new Point(20, 85), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                NumericUpDown numDigits = new NumericUpDown { Location = new Point(20, 110), Width = 80, Minimum = 1, Maximum = 12, Value = 6 };

                Label lblPos = new Label { Text = "Vị trí đánh số:", Location = new Point(140, 85), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                ComboBox cbPos = new ComboBox { Location = new Point(140, 110), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
                cbPos.Items.AddRange(new object[] { "Góc dưới cùng bên phải", "Góc dưới cùng ở giữa", "Góc dưới cùng bên trái", "Góc trên cùng bên phải", "Góc trên cùng ở giữa", "Góc trên cùng bên trái" });
                cbPos.SelectedIndex = 0;

                Label lblFont = new Label { Text = "Cỡ chữ: 10 pt Courier | Màu mực: Đen", Location = new Point(20, 155), AutoSize = true, ForeColor = Color.DarkSlateGray };

                Button btnOk = new Button { Text = "Đánh số", Location = new Point(160, 210), Width = 100, Height = 32, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Hủy bỏ", Location = new Point(270, 210), Width = 80, Height = 32, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };

                dlg.Controls.AddRange(new Control[] { lblPrefix, txtPrefix, lblStart, numStart, lblDigits, numDigits, lblPos, cbPos, lblFont, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    PdfBatesPosition pos = PdfBatesPosition.BottomRight;
                    switch (cbPos.SelectedIndex)
                    {
                        case 0: pos = PdfBatesPosition.BottomRight; break;
                        case 1: pos = PdfBatesPosition.BottomCenter; break;
                        case 2: pos = PdfBatesPosition.BottomLeft; break;
                        case 3: pos = PdfBatesPosition.TopRight; break;
                        case 4: pos = PdfBatesPosition.TopCenter; break;
                        case 5: pos = PdfBatesPosition.TopLeft; break;
                    }

                    var config = new PdfBatesConfig
                    {
                        Prefix = txtPrefix.Text,
                        StartNumber = (int)numStart.Value,
                        NumberOfDigits = (int)numDigits.Value,
                        Position = pos,
                        FontSize = 10f
                    };
                    ApplyBatesNumbering(config);
                    return true;
                }
            }
            return false;
        }

        public bool PromptAddBookmark(Form parentForm)
        {
            if (Document == null || Document.Pages.Count == 0) return false;

            using (Form dlg = new Form())
            {
                dlg.Text = "Thêm Dấu Trang / Mục Lục - ZENQ PDF";
                dlg.Size = new Size(360, 180);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

                Label lblTitle = new Label { Text = "Tiêu đề mục lục (Trang " + (ActivePageIndex + 1) + "):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                TextBox txtTitle = new TextBox { Text = "Trang " + (ActivePageIndex + 1), Location = new Point(20, 48), Width = 300 };

                Button btnOk = new Button { Text = "Thêm", Location = new Point(140, 90), Width = 90, Height = 30, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Hủy", Location = new Point(240, 90), Width = 80, Height = 30, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };

                dlg.Controls.AddRange(new Control[] { lblTitle, txtTitle, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    string title = txtTitle.Text.Trim();
                    if (string.IsNullOrEmpty(title)) title = "Trang " + (ActivePageIndex + 1);
                    AddBookmark(title, ActivePageIndex);
                    ShowThumbnailsSidebar = true;
                    SidebarViewMode = 1;
                    return true;
                }
            }
            return false;
        }

        public bool PromptSetPasswordSecurity(Form parentForm)
        {
            if (Document == null) Document = new PdfDocument();

            using (var dlg = new PdfSecurityDialog(Document.SecuritySettings))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    if (dlg.RemoveSecurityRequested)
                    {
                        PushUndoState("Gỡ mật khẩu bảo mật PDF");
                        Document.RemovePasswordProtection(dlg.OpenPassword ?? "");
                        MessageBox.Show(parentForm, "Đã gỡ bỏ toàn bộ mật khẩu bảo vệ và hạn chế thao tác trên tài liệu!", "Bảo mật ZenQ PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (dlg.IsProtected)
                    {
                        PushUndoState("Thiết lập mật khẩu bảo mật PDF");
                        Document.SetPasswordProtection(dlg.OpenPassword, dlg.PermissionPassword, dlg.AllowPrinting, dlg.AllowCopying, dlg.AllowModifying, dlg.KeyLength);
                        MessageBox.Show(parentForm, "Đã thiết lập mật khẩu mã hóa thành công theo chuẩn " + dlg.KeyLength + "-bit AES!", "Bảo mật ZenQ PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (parentForm != null) parentForm.Invalidate();
                    return true;
                }
            }
            return false;
        }

        public bool PromptExtractPages(Form parentForm, Action<PdfDocument, string> onExtracted = null)
        {
            if (Document == null || Document.Pages == null || Document.Pages.Count == 0) return false;

            using (var dlg = new PdfExtractPagesDialog(Document.Pages.Count, ActivePageIndex + 1))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                {
                    if (dlg.SelectedPageIndices == null || dlg.SelectedPageIndices.Count == 0) return false;

                    string extractedTitle = "Trích xuất - " + (string.IsNullOrEmpty(Document.Title) ? "Tài liệu" : Document.Title);
                    var extractedDoc = Document.ExtractPages(dlg.SelectedPageIndices, extractedTitle);

                    if (dlg.DeleteAfterExtract)
                    {
                        PushUndoState("Trích xuất và xóa trang");
                        var sortedDesc = dlg.SelectedPageIndices.OrderByDescending(x => x).ToList();
                        foreach (int idx in sortedDesc)
                        {
                            if (Document.Pages.Count > 1)
                            {
                                Document.DeletePage(idx);
                            }
                        }
                        ActivePageIndex = Math.Min(ActivePageIndex, Document.Pages.Count - 1);
                        InvalidateAllCaches();
                        if (parentForm != null) parentForm.Invalidate();
                    }

                    if (dlg.SaveDirectlyToFile && !string.IsNullOrEmpty(dlg.OutputFilePath))
                    {
                        bool saved = PdfHandler.SavePdf(extractedDoc, dlg.OutputFilePath);
                        if (saved)
                        {
                            MessageBox.Show(parentForm, "Đã trích xuất " + dlg.SelectedPageIndices.Count + " trang và lưu thành công vào:\n" + dlg.OutputFilePath, "Trích xuất Trang PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        if (onExtracted != null)
                        {
                            onExtracted(extractedDoc, extractedDoc.Title);
                        }
                        else
                        {
                            MessageBox.Show(parentForm, "Đã trích xuất thành công " + dlg.SelectedPageIndices.Count + " trang!", "Trích xuất Trang PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool PromptInsertPagesFromFile(Form parentForm)
        {
            if (Document == null) Document = new PdfDocument();

            using (var dlg = new PdfInsertPagesDialog(Document.Pages.Count, ActivePageIndex + 1))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedFilePath))
                {
                    try
                    {
                        var externalDoc = PdfHandler.LoadPdf(dlg.SelectedFilePath);
                        if (externalDoc == null || externalDoc.Pages == null || externalDoc.Pages.Count == 0)
                        {
                            MessageBox.Show(parentForm, "Không thể đọc nội dung từ tệp PDF đã chọn!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        PushUndoState("Chèn trang từ tệp PDF");
                        Document.InsertDocumentAt(dlg.InsertTargetIndex, externalDoc);
                        ActivePageIndex = Math.Min(dlg.InsertTargetIndex, Document.Pages.Count - 1);
                        InvalidateAllCaches();
                        if (parentForm != null) parentForm.Invalidate();

                        MessageBox.Show(parentForm, "Đã chèn thành công " + externalDoc.Pages.Count + " trang từ:\n" + Path.GetFileName(dlg.SelectedFilePath), "Chèn Trang PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(parentForm, "Lỗi khi chèn trang: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return false;
        }

        public bool PromptCompressPdf(Form parentForm)
        {
            if (Document == null || Document.Pages == null || Document.Pages.Count == 0) return false;

            using (var dlg = new PdfCompressDialog(Document))
            {
                if (dlg.ShowDialog(parentForm) == DialogResult.OK && !string.IsNullOrEmpty(dlg.OutputFilePath))
                {
                    long originalBytes = 0;
                    if (!string.IsNullOrEmpty(Document.FilePath) && File.Exists(Document.FilePath))
                    {
                        originalBytes = new FileInfo(Document.FilePath).Length;
                    }

                    bool ok = PdfConverterEngine.CompressPdf(Document, dlg.OutputFilePath, dlg.TargetQualityPercent, dlg.TargetMaxDpi, dlg.StripMetadata);
                    if (ok)
                    {
                        long compressedBytes = File.Exists(dlg.OutputFilePath) ? new FileInfo(dlg.OutputFilePath).Length : 0;
                        string msg = "Tối ưu hóa và nén tài liệu PDF thành công!";
                        if (originalBytes > 0 && compressedBytes > 0)
                        {
                            double savedPct = Math.Max(0, (1.0 - ((double)compressedBytes / originalBytes)) * 100);
                            msg += $"\n\n- Dung lượng gốc: {originalBytes / 1024:N0} KB\n- Dung lượng nén: {compressedBytes / 1024:N0} KB\n- Tiết kiệm: {savedPct:0.#}%";
                        }
                        msg += $"\n\nTệp đã lưu tại:\n{dlg.OutputFilePath}";
                        MessageBox.Show(parentForm, msg, "Nén & Tối ưu hóa PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(parentForm, "Không thể hoàn tất nén tài liệu PDF!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return false;
        }

        public bool PromptExportWord(Form parentForm)
        {
            if (Document == null) return false;
            using (var sfd = new SaveFileDialog { Filter = "Tài liệu Word (*.docx)|*.docx", Title = "Xuất PDF sang Word (.docx)" })
            {
                string defName = !string.IsNullOrEmpty(Document.FilePath) ? Path.GetFileNameWithoutExtension(Document.FilePath) : "Document";
                sfd.FileName = defName + ".docx";
                if (sfd.ShowDialog(parentForm) == DialogResult.OK)
                {
                    bool ok = PdfConverterEngine.ExportToDocx(Document, sfd.FileName);
                    if (ok)
                    {
                        var res = MessageBox.Show(parentForm, "Đã xuất tài liệu sang Microsoft Word (.docx) thành công!\n\nBạn có muốn mở tệp ngay bây giờ?", "Xuất Word 365", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool PromptExportExcel(Form parentForm)
        {
            if (Document == null) return false;
            using (var sfd = new SaveFileDialog { Filter = "Bảng tính Excel (*.xlsx)|*.xlsx", Title = "Xuất PDF sang Excel (.xlsx)" })
            {
                string defName = !string.IsNullOrEmpty(Document.FilePath) ? Path.GetFileNameWithoutExtension(Document.FilePath) : "Document";
                sfd.FileName = defName + ".xlsx";
                if (sfd.ShowDialog(parentForm) == DialogResult.OK)
                {
                    bool ok = PdfConverterEngine.ExportToXlsx(Document, sfd.FileName);
                    if (ok)
                    {
                        var res = MessageBox.Show(parentForm, "Đã xuất bảng biểu sang Microsoft Excel (.xlsx) thành công!\n\nBạn có muốn mở tệp ngay bây giờ?", "Xuất Excel 365", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool PromptExportImages(Form parentForm)
        {
            if (Document == null) return false;
            using (var fbd = new FolderBrowserDialog { Description = "Chọn thư mục lưu hình ảnh từng trang PDF" })
            {
                if (fbd.ShowDialog(parentForm) == DialogResult.OK)
                {
                    var list = PdfConverterEngine.ExportToImages(Document, fbd.SelectedPath, "PNG", 150);
                    if (list.Count > 0)
                    {
                        MessageBox.Show(parentForm, $"Đã xuất thành công {list.Count} trang thành hình ảnh PNG tại:\n{fbd.SelectedPath}", "Xuất Hình Ảnh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool PromptExtractAllText(Form parentForm)
        {
            if (Document == null) return false;
            string text = PdfConverterEngine.ExtractAllText(Document, includePageHeaders: true);
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show(parentForm, "Tài liệu PDF không chứa nội dung văn bản có thể trích xuất.", "Trích xuất Văn bản", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try { Clipboard.SetText(text); } catch { }

            var res = MessageBox.Show(parentForm, $"Đã trích xuất toàn bộ văn bản ({text.Length:N0} ký tự) và sao chép vào Clipboard!\n\nBạn có muốn lưu nội dung này ra tệp văn bản thuần (.txt) không?", "Trích xuất Văn bản", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes)
            {
                using (var sfd = new SaveFileDialog { Filter = "Tệp văn bản (*.txt)|*.txt", Title = "Lưu văn bản trích xuất" })
                {
                    string defName = !string.IsNullOrEmpty(Document.FilePath) ? Path.GetFileNameWithoutExtension(Document.FilePath) : "Document";
                    sfd.FileName = defName + "_Text.txt";
                    if (sfd.ShowDialog(parentForm) == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, text, Encoding.UTF8);
                    }
                }
            }
            return true;
        }
        #endregion
    }
}

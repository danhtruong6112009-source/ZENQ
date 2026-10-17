using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;
using ZenQOffice.Format;

namespace ZenQOffice.UI
{
    public class PhotoView : IDisposable
    {
        public PhotoDocument Document { get; set; }

        // Navigation & Canvas Transform
        public float Zoom { get; set; } = 1.0f;
        public float PanX { get; set; } = 0f;
        public float PanY { get; set; } = 0f;
        public bool IsPanning { get; set; } = false;
        private Point _lastPanPoint = Point.Empty;

        // Active Tool
        public PhotoTool SelectedTool { get; set; } = PhotoTool.Move;

        // Tool Properties
        public Color ForegroundColor { get; set; } = Color.FromArgb(37, 99, 235);
        public Color BackgroundColor { get; set; } = Color.White;
        public float BrushSize { get; set; } = 18f;
        public float BrushOpacity { get; set; } = 1.0f;
        public float BrushHardness { get; set; } = 0.85f;
        public VectorShapeType ActiveShapeType { get; set; } = VectorShapeType.Rectangle;
        public float ShapeStrokeWidth { get; set; } = 2.0f;
        public float ShapeCornerRadius { get; set; } = 0.0f;
        public int ActiveShapePointCount { get; set; } = 5;
        public Color ShapeFillColor { get; set; } = Color.FromArgb(59, 130, 246);
        public Color ShapeStrokeColor { get; set; } = Color.FromArgb(37, 99, 235);
        public VectorFillType ShapeFillType { get; set; } = VectorFillType.Solid;
        public Color ShapeFillColor2 { get; set; } = Color.FromArgb(147, 197, 253);
        public float ShapeFillGradientAngle { get; set; } = 0f;
        public List<PhotoGradientStop> ShapeGradientStops { get; set; } = new List<PhotoGradientStop>();
        public VectorStrokeType ShapeStrokeType { get; set; } = VectorStrokeType.Solid;
        public DashStyle ShapeStrokeDashStyle { get; set; } = DashStyle.Solid;
        public LineJoin ShapeStrokeLineJoin { get; set; } = LineJoin.Round;
        public LineCap ShapeStrokeLineCap { get; set; } = LineCap.Round;
        private List<PointF> _dragInitialPathPoints = null;
        private List<VectorSubPath> _dragInitialSubPaths = null;

        // Active Text Tool State
        public string ActiveFontName { get; set; } = "Segoe UI";
        public float ActiveFontSize { get; set; } = 42f;
        public bool ActiveFontBold { get; set; } = true;
        public bool ActiveFontItalic { get; set; } = false;

        // Interactive States
        private bool _isDrawing = false;
        private PointF _lastBrushPt = PointF.Empty;
        private PointF _dragStartPt = PointF.Empty;
        private PointF _currentDragPt = PointF.Empty;
        // Top Tool Controls Bar Interactive State
        private readonly List<(Rectangle Rect, Action Action)> _topBarClickables = new List<(Rectangle, Action)>();
        private readonly List<(Rectangle Rect, Action Action)> _inspectorClickables = new List<(Rectangle, Action)>();
        private bool _isDraggingBrushSizeSlider = false;
        private Rectangle _brushSizeTrackRect = Rectangle.Empty;
        private bool _isDraggingRadiusSlider = false;
        private Rectangle _radiusTrackRect = Rectangle.Empty;
        private bool _isDraggingFontSizeSlider = false;
        private Rectangle _fontSizeTrackRect = Rectangle.Empty;

        private int _selectedTransformHandle = -1; // 0..7 resize handles, 8 = rotation handle, 9 = drag body
        private RectangleF _dragInitialBounds = RectangleF.Empty;
        private float _dragInitialRotation = 0f;

        // Node Tool State (Inkscape-standard edit paths by nodes)
        private enum NodeDragTarget
        {
            None = 0,
            AnchorNode,
            InHandle,
            OutHandle,
            Segment,
            Marquee
        }

        private bool _isNodeToolDragging = false;
        private NodeDragTarget _nodeDragTarget = NodeDragTarget.None;
        private VectorPathNode _activeDragNode = null;
        private VectorPathNode _pendingSingleSelectNode = null;
        private readonly Dictionary<VectorPathNode, PointF> _nodeDragInitialPoints = new Dictionary<VectorPathNode, PointF>();
        private readonly Dictionary<VectorPathNode, PointF> _nodeDragInitialInHandles = new Dictionary<VectorPathNode, PointF>();
        private readonly Dictionary<VectorPathNode, PointF> _nodeDragInitialOutHandles = new Dictionary<VectorPathNode, PointF>();
        private bool _nodeUnitIsMm = false;
        private const float PxPerMm = 96.0f / 25.4f;

        // Multi-Layer Dragging & Inkscape Selection
        private readonly Dictionary<PhotoLayer, RectangleF> _multiDragInitialBounds = new Dictionary<PhotoLayer, RectangleF>();
        private readonly Dictionary<PhotoLayer, float> _multiDragInitialRotations = new Dictionary<PhotoLayer, float>();
        private readonly Dictionary<PhotoLayer, List<PointF>> _multiDragInitialPoints = new Dictionary<PhotoLayer, List<PointF>>();
        private readonly Dictionary<PhotoLayer, List<VectorSubPath>> _multiDragInitialSubPaths = new Dictionary<PhotoLayer, List<VectorSubPath>>();

        // Interactive Inkscape Bezier Pen Tool State
        private PointF _penHoverPt = PointF.Empty;
        private bool _isPenDraggingHandle = false;

        // Direct Segment Bending State (Inkscape curve dragging)
        private int _dragSegmentSubPathIndex = -1;
        private int _dragSegmentIndex = -1;

        // Snapping & Alignment Guides
        public bool SnapToGrid { get; set; } = false;
        public float GridSize { get; set; } = 20f;
        public bool SnapToObjects { get; set; } = true;
        public bool ShowGrid { get; set; } = false;
        public bool ShowRulers { get; set; } = true;
        public const int RulerThickness = 18;
        public List<float> HorizontalGuides { get; } = new List<float>();
        public List<float> VerticalGuides { get; } = new List<float>();
        public bool IsSpaceHeld { get; set; } = false;
        private bool _isDraggingRulerGuideH = false;
        private bool _isDraggingRulerGuideV = false;
        private float _currentDraggingGuidePos = 0f;
        public PointF CurrentMouseCanvasPoint { get; set; } = PointF.Empty;
        private readonly List<(PointF Start, PointF End, Color Col)> _activeSmartGuides = new List<(PointF, PointF, Color)>();

        // Crop Tool State
        public RectangleF CropRect { get; set; } = RectangleF.Empty;
        public bool IsCropping { get; set; } = false;
        private int _selectedCropHandle = -1;

        // Right Inspector Tabs (0: Lớp, 1: Bộ lọc & Màu, 2: Thuộc tính)
        public int InspectorActiveTab { get; set; } = 0;
        private int _hoverLayerIndex = -1;
        private int _hoverToolIndex = -1;
        private int _hoverFilterIndex = -1;
        private int _layerScrollY = 0;

        // Workspace Scrollbar State (Thanh cuộn ngang & dọc chuẩn Slide)
        public Rectangle HScrollBarTrackRect { get; private set; } = Rectangle.Empty;
        public Rectangle HScrollBarThumbRect { get; private set; } = Rectangle.Empty;
        public Rectangle VScrollBarTrackRect { get; private set; } = Rectangle.Empty;
        public Rectangle VScrollBarThumbRect { get; private set; } = Rectangle.Empty;
        public bool IsDraggingHScrollBar { get; private set; } = false;
        public bool IsDraggingVScrollBar { get; private set; } = false;
        public bool IsHoverHScrollBarThumb { get; private set; } = false;
        public bool IsHoverVScrollBarThumb { get; private set; } = false;
        private int _scrollBarDragStartMouseX = 0;
        private int _scrollBarDragStartMouseY = 0;
        private float _scrollBarDragStartPanX = 0f;
        private float _scrollBarDragStartPanY = 0f;

        // Layout Constants
        public const int LeftToolbarWidth = 52;
        public const int RightInspectorWidth = 290;
        public const int TopOptionsBarHeight = 44;
        public const int RibbonHeight = 155;
        public const int BottomStatusHeight = 26;

        public PhotoView()
        {
            Document = new PhotoDocument(1920, 1080, "Thiết kế Mới 1.svg");
        }

        public PhotoView(PhotoDocument doc)
        {
            Document = doc ?? new PhotoDocument(1920, 1080, "Thiết kế Mới 1.svg");
        }

        #region Coordinate Transformations
        public PointF ScreenToCanvas(float sx, float sy, int viewW, int viewH)
        {
            float canvasScreenX = LeftToolbarWidth + PanX;
            float canvasScreenY = RibbonHeight + TopOptionsBarHeight + PanY;

            float cx = (sx - canvasScreenX) / Zoom;
            float cy = (sy - canvasScreenY) / Zoom;
            return new PointF(cx, cy);
        }

        public PointF CanvasToScreen(float cx, float cy, int viewW, int viewH)
        {
            float canvasScreenX = LeftToolbarWidth + PanX;
            float canvasScreenY = RibbonHeight + TopOptionsBarHeight + PanY;

            float sx = canvasScreenX + cx * Zoom;
            float sy = canvasScreenY + cy * Zoom;
            return new PointF(sx, sy);
        }

        public RectangleF GetCanvasScreenBounds(int viewW, int viewH)
        {
            if (Document == null) return RectangleF.Empty;
            float canvasScreenX = LeftToolbarWidth + PanX;
            float canvasScreenY = RibbonHeight + TopOptionsBarHeight + PanY;
            return new RectangleF(canvasScreenX, canvasScreenY, Document.Width * Zoom, Document.Height * Zoom);
        }

        public void CenterCanvas(int viewW, int viewH)
        {
            if (Document == null) return;
            float availW = Math.Max(200, viewW - LeftToolbarWidth - RightInspectorWidth);
            float availH = Math.Max(200, viewH - RibbonHeight - TopOptionsBarHeight - BottomStatusHeight);

            // Fit zoom
            float fitZoomX = (availW - 80f) / Document.Width;
            float fitZoomY = (availH - 80f) / Document.Height;
            Zoom = Math.Max(0.1f, Math.Min(1.0f, Math.Min(fitZoomX, fitZoomY)));

            float renderedW = Document.Width * Zoom;
            float renderedH = Document.Height * Zoom;

            PanX = (availW - renderedW) / 2.0f;
            PanY = (availH - renderedH) / 2.0f;
        }

        public void GetScrollLimits(int width, int height, out float minPanX, out float maxPanX, out float minPanY, out float maxPanY)
        {
            if (Document == null)
            {
                minPanX = maxPanX = minPanY = maxPanY = 0f;
                return;
            }

            float availW = Math.Max(100f, width - LeftToolbarWidth - RightInspectorWidth);
            float availH = Math.Max(100f, height - RibbonHeight - TopOptionsBarHeight - BottomStatusHeight);
            float docRenderedW = Document.Width * Zoom;
            float docRenderedH = Document.Height * Zoom;

            float marginX = Math.Max(40f, availW * 0.12f);
            float marginY = Math.Max(40f, availH * 0.12f);

            if (docRenderedW + marginX * 2 > availW)
            {
                minPanX = availW - docRenderedW - marginX;
                maxPanX = marginX;
            }
            else
            {
                float center = (availW - docRenderedW) / 2f;
                minPanX = center;
                maxPanX = center;
            }

            if (docRenderedH + marginY * 2 > availH)
            {
                minPanY = availH - docRenderedH - marginY;
                maxPanY = marginY;
            }
            else
            {
                float center = (availH - docRenderedH) / 2f;
                minPanY = center;
                maxPanY = center;
            }
        }

        public void ClampPan(int width, int height)
        {
            GetScrollLimits(width, height, out float minX, out float maxX, out float minY, out float maxY);
            PanX = Math.Max(minX, Math.Min(maxX, PanX));
            PanY = Math.Max(minY, Math.Min(maxY, PanY));
        }
        #endregion

        #region 100% PURE Direct2D Rendering Loop
        public void Draw(D2DGraphics g, int width, int height, RibbonBar ribbon)
        {
            if (Document == null) return;
            if (PanX == 0 && PanY == 0 && Zoom == 1.0f)
            {
                CenterCanvas(width, height);
            }

            // 1. Draw Viewport Background (Clean Light Studio Slate)
            Rectangle viewportRect = new Rectangle(LeftToolbarWidth, RibbonHeight + TopOptionsBarHeight, width - LeftToolbarWidth - RightInspectorWidth, height - RibbonHeight - TopOptionsBarHeight - BottomStatusHeight);
            using (SolidBrush vpBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.FillRectangle(vpBrush, viewportRect);
            }

            // Clip viewport to avoid drawing outside the canvas work area
            g.SetClip(viewportRect);

            // 2. Draw Artboard Drop Shadow & Checkerboard Canvas
            RectangleF artboardRect = GetCanvasScreenBounds(width, height);

            // Checkerboard Transparency Pattern on Artboard
            DrawCheckerboardGrid(g, artboardRect, 16f * Zoom);

            // 3. Render Canvas Layers with Direct2D GPU Acceleration
            GraphicsState canvasState = g.Save();
            g.TranslateTransform(artboardRect.X, artboardRect.Y);
            g.ScaleTransform(Zoom, Zoom);

            // Render Document Layers Bottom-to-Top
            for (int i = 0; i < Document.Layers.Count; i++)
            {
                PhotoLayer layer = Document.Layers[i];
                if (!layer.IsVisible || layer.Opacity <= 0.001f) continue;
                RenderLayerD2D(g, layer);
            }

            // Draw Interactive Vector Shape Preview during Dragging
            if (_isDrawing && IsShapeTool(SelectedTool))
            {
                RectangleF previewBounds = NormalizeRect(_dragStartPt, _currentDragPt);
                if (previewBounds.Width > 1 || previewBounds.Height > 1)
                {
                    VectorShapeType pType = GetShapeTypeForTool(SelectedTool);
                    PhotoLayer previewLayer = new PhotoLayer
                    {
                        Type = LayerType.VectorShape,
                        ShapeType = pType,
                        Bounds = previewBounds,
                        FillType = ShapeFillType,
                        FillColor = ShapeFillColor,
                        FillColor2 = ShapeFillColor2,
                        FillGradientAngle = ShapeFillGradientAngle,
                        GradientStops = ShapeGradientStops?.Select(s => s.Clone()).ToList() ?? new List<PhotoGradientStop>(),
                        StrokeType = ShapeStrokeType,
                        StrokeColor = ShapeStrokeColor,
                        StrokeWidth = ShapeStrokeWidth,
                        StrokeDashStyle = ShapeStrokeDashStyle,
                        StrokeLineJoin = ShapeStrokeLineJoin,
                        StrokeLineCap = ShapeStrokeLineCap,
                        CornerRadius = ShapeCornerRadius,
                        ShapePointCount = ActiveShapePointCount,
                        Opacity = 0.85f
                    };
                    RenderLayerD2D(g, previewLayer);
                }
            }

            // Draw Interactive Gradient Vector Line
            if (_isDrawing && SelectedTool == PhotoTool.Gradient)
            {
                using (Pen gPen = new Pen(Color.FromArgb(59, 130, 246), 2.0f / Zoom))
                using (SolidBrush gBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
                using (SolidBrush wBrush = new SolidBrush(Color.White))
                {
                    gPen.DashStyle = DashStyle.Dash;
                    g.DrawLine(gPen, _dragStartPt.X, _dragStartPt.Y, _currentDragPt.X, _currentDragPt.Y);
                    float r = 5.0f / Zoom;
                    g.FillEllipse(wBrush, _dragStartPt.X - r, _dragStartPt.Y - r, r * 2, r * 2);
                    g.DrawEllipse(gPen, _dragStartPt.X - r, _dragStartPt.Y - r, r * 2, r * 2);
                    g.FillEllipse(gBrush, _currentDragPt.X - r, _currentDragPt.Y - r, r * 2, r * 2);
                }
            }

            // Draw Interactive Marquee Selection Preview
            if (_isDrawing && SelectedTool == PhotoTool.Marquee)
            {
                RectangleF marqueeBounds = NormalizeRect(_dragStartPt, _currentDragPt);
                using (Pen marqueePen = new Pen(Color.FromArgb(51, 65, 85), 1.0f / Zoom))
                {
                    marqueePen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(marqueePen, marqueeBounds.X, marqueeBounds.Y, marqueeBounds.Width, marqueeBounds.Height);
                }
            }

            g.Restore(canvasState);

            // 4. Render Grid & Smart Guides
            if (ShowGrid)
            {
                using var gridPen = new Pen(Color.FromArgb(226, 232, 240), 0.8f);
                for (float gx = 0; gx <= Document.Width; gx += GridSize)
                {
                    PointF p1 = CanvasToScreen(gx, 0, width, height);
                    PointF p2 = CanvasToScreen(gx, Document.Height, width, height);
                    g.DrawLine(gridPen, p1.X, p1.Y, p2.X, p2.Y);
                }
                for (float gy = 0; gy <= Document.Height; gy += GridSize)
                {
                    PointF p1 = CanvasToScreen(0, gy, width, height);
                    PointF p2 = CanvasToScreen(Document.Width, gy, width, height);
                    g.DrawLine(gridPen, p1.X, p1.Y, p2.X, p2.Y);
                }
            }

            if (_activeSmartGuides != null && _activeSmartGuides.Count > 0)
            {
                using var guidePen = new Pen(Color.FromArgb(236, 72, 153), 1.2f) { DashStyle = DashStyle.Dash };
                foreach (var gLine in _activeSmartGuides)
                {
                    PointF p1 = CanvasToScreen(gLine.Start.X, gLine.Start.Y, width, height);
                    PointF p2 = CanvasToScreen(gLine.End.X, gLine.End.Y, width, height);
                    g.DrawLine(guidePen, p1.X, p1.Y, p2.X, p2.Y);
                }
            }

            // 4b. Render User Measurement Guides (Inkscape/Photoshop Guidelines)
            if (ShowRulers)
            {
                using var userGuidePen = new Pen(Color.FromArgb(0, 180, 240), 1.2f) { DashStyle = DashStyle.Dash };
                foreach (var gy in HorizontalGuides)
                {
                    PointF p = CanvasToScreen(0, gy, width, height);
                    g.DrawLine(userGuidePen, LeftToolbarWidth, p.Y, width - RightInspectorWidth, p.Y);
                }
                foreach (var gx in VerticalGuides)
                {
                    PointF p = CanvasToScreen(gx, 0, width, height);
                    g.DrawLine(userGuidePen, p.X, RibbonHeight + TopOptionsBarHeight, p.X, height - BottomStatusHeight);
                }
                if (_isDraggingRulerGuideH)
                {
                    PointF p = CanvasToScreen(0, _currentDraggingGuidePos, width, height);
                    g.DrawLine(userGuidePen, LeftToolbarWidth, p.Y, width - RightInspectorWidth, p.Y);
                }
                if (_isDraggingRulerGuideV)
                {
                    PointF p = CanvasToScreen(_currentDraggingGuidePos, 0, width, height);
                    g.DrawLine(userGuidePen, p.X, RibbonHeight + TopOptionsBarHeight, p.X, height - BottomStatusHeight);
                }
            }

            // 5. Render Active Layer Transform Handles & Multi-Selection Boxes
            if ((SelectedTool == PhotoTool.Move || SelectedTool == PhotoTool.Rectangle) && Document.ActiveLayer != null)
            {
                if (Document.SelectedLayers.Count > 1)
                {
                    foreach (var sel in Document.SelectedLayers)
                    {
                        DrawTransformBoundingBox(g, sel, width, height, isSecondary: true);
                    }
                    DrawCombinedTransformBoundingBox(g, width, height);
                }
                else
                {
                    DrawTransformBoundingBox(g, Document.ActiveLayer, width, height, isSecondary: false);
                }
            }
            else if (SelectedTool == PhotoTool.NodeTool && Document.ActiveLayer != null)
            {
                DrawNodeToolCanvasOverlay(g, Document.ActiveLayer, width, height);
            }
            else if (SelectedTool == PhotoTool.Pen && Document.ActiveLayer != null && Document.ActiveLayer.ShapeType == VectorShapeType.Path)
            {
                var curPath = Document.ActiveLayer;
                curPath.EnsureNodesInitialized();

                if (curPath.SubPaths != null && curPath.SubPaths.Count > 0)
                {
                    var lastSp = curPath.SubPaths[curPath.SubPaths.Count - 1];
                    if (lastSp.Nodes != null && lastSp.Nodes.Count > 0)
                    {
                        using (Pen penArm = new Pen(Color.FromArgb(99, 102, 241), 1f))
                        using (SolidBrush knobB = new SolidBrush(Color.FromArgb(59, 130, 246)))
                        using (Pen knobP = new Pen(Color.FromArgb(15, 23, 42), 1f))
                        using (SolidBrush nodeB = new SolidBrush(Color.White))
                        using (Pen nodeP = new Pen(Color.FromArgb(15, 23, 42), 1.2f))
                        {
                            for (int ni = 0; ni < lastSp.Nodes.Count; ni++)
                            {
                                var n = lastSp.Nodes[ni];
                                PointF sPt = CanvasToScreen(n.Point.X, n.Point.Y, width, height);

                                if (n.HasInHandle)
                                {
                                    PointF sIn = CanvasToScreen(n.InHandle.X, n.InHandle.Y, width, height);
                                    g.DrawLine(penArm, sPt.X, sPt.Y, sIn.X, sIn.Y);
                                    g.FillEllipse(knobB, sIn.X - 3, sIn.Y - 3, 6, 6);
                                    g.DrawEllipse(knobP, sIn.X - 3, sIn.Y - 3, 6, 6);
                                }
                                if (n.HasOutHandle)
                                {
                                    PointF sOut = CanvasToScreen(n.OutHandle.X, n.OutHandle.Y, width, height);
                                    g.DrawLine(penArm, sPt.X, sPt.Y, sOut.X, sOut.Y);
                                    g.FillEllipse(knobB, sOut.X - 3, sOut.Y - 3, 6, 6);
                                    g.DrawEllipse(knobP, sOut.X - 3, sOut.Y - 3, 6, 6);
                                }

                                g.FillRectangle(nodeB, sPt.X - 3.5f, sPt.Y - 3.5f, 7, 7);
                                g.DrawRectangle(nodeP, sPt.X - 3.5f, sPt.Y - 3.5f, 7, 7);
                            }
                        }

                        // Rubberband preview line from last node to mouse cursor
                        if (!_penHoverPt.IsEmpty && !lastSp.IsClosed)
                        {
                            var lastNode = lastSp.Nodes[lastSp.Nodes.Count - 1];
                            PointF sLast = CanvasToScreen(lastNode.Point.X, lastNode.Point.Y, width, height);
                            PointF sMouse = CanvasToScreen(_penHoverPt.X, _penHoverPt.Y, width, height);

                            using (Pen rubberPen = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
                            {
                                rubberPen.DashStyle = DashStyle.Dash;
                                g.DrawLine(rubberPen, sLast.X, sLast.Y, sMouse.X, sMouse.Y);
                            }

                            // Close-path snap indicator when near start node
                            if (lastSp.Nodes.Count > 1)
                            {
                                var firstNode = lastSp.Nodes[0];
                                float distSq = (_penHoverPt.X - firstNode.Point.X) * (_penHoverPt.X - firstNode.Point.X) +
                                               (_penHoverPt.Y - firstNode.Point.Y) * (_penHoverPt.Y - firstNode.Point.Y);
                                float snapTol = 12f / Math.Max(0.1f, Zoom);
                                if (distSq <= snapTol * snapTol)
                                {
                                    PointF sFirst = CanvasToScreen(firstNode.Point.X, firstNode.Point.Y, width, height);
                                    using (Pen snapPen = new Pen(Color.FromArgb(16, 185, 129), 2f))
                                    {
                                        g.DrawEllipse(snapPen, sFirst.X - 8, sFirst.Y - 8, 16, 16);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 5. Draw Interactive Crop Overlay if Active
            if (SelectedTool == PhotoTool.Crop && IsCropping)
            {
                DrawCropOverlay(g, artboardRect, width, height);
            }

            // 6. Draw Brush / Eraser Cursor Overlay (Live Circle following Mouse)
            if (SelectedTool == PhotoTool.Brush || SelectedTool == PhotoTool.Eraser)
            {
                PointF screenPt = CanvasToScreen(_currentDragPt.X, _currentDragPt.Y, width, height);
                if (viewportRect.Contains((int)screenPt.X, (int)screenPt.Y))
                {
                    float screenRadius = (BrushSize * Zoom) / 2.0f;
                    using (Pen brushPen = new Pen(Color.FromArgb(180, 15, 23, 42), 1.2f))
                    {
                        g.DrawEllipse(brushPen, screenPt.X - screenRadius, screenPt.Y - screenRadius, screenRadius * 2, screenRadius * 2);
                    }
                }
            }

            g.ResetClip();

            // 6b. Render Workspace Scrollbars (Thanh cuộn ngang & dọc chuẩn Slide)
            DrawWorkspaceScrollbars(g, width, height);

            // 7. Render Modern Light UI Controls
            DrawTopOptionsBar(g, width);
            DrawLeftToolbar(g, height);
            DrawRightInspector(g, width, height);
            DrawBottomColorPalette(g, width, height);
            DrawRulers(g, width, height);
        }

        private void DrawRulers(D2DGraphics g, int width, int height)
        {
            if (!ShowRulers || Document == null) return;

            int rulerTop = RibbonHeight + TopOptionsBarHeight;
            int rulerLeft = LeftToolbarWidth;
            int availW = width - LeftToolbarWidth - RightInspectorWidth;
            int availH = height - rulerTop - BottomStatusHeight;

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            using (Pen tickPen = new Pen(Color.FromArgb(148, 163, 184), 1f))
            using (Pen cursorPen = new Pen(Color.FromArgb(239, 68, 68), 1.2f))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (Font numFont = new Font("Segoe UI", 6.8f, FontStyle.Regular))
            {
                // 1. Top Ruler
                Rectangle topRulerRect = new Rectangle(rulerLeft, rulerTop, availW, RulerThickness);
                g.FillRectangle(bgBrush, topRulerRect);
                g.DrawLine(borderPen, rulerLeft, rulerTop + RulerThickness, rulerLeft + availW, rulerTop + RulerThickness);

                // 2. Left Ruler
                Rectangle leftRulerRect = new Rectangle(rulerLeft, rulerTop + RulerThickness, RulerThickness, availH - RulerThickness);
                g.FillRectangle(bgBrush, leftRulerRect);
                g.DrawLine(borderPen, rulerLeft + RulerThickness, rulerTop + RulerThickness, rulerLeft + RulerThickness, rulerTop + availH);

                // 3. Corner Box (Intersection)
                Rectangle cornerRect = new Rectangle(rulerLeft, rulerTop, RulerThickness, RulerThickness);
                using (SolidBrush cornerBrush = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    g.FillRectangle(cornerBrush, cornerRect);
                    g.DrawRectangle(borderPen, cornerRect);
                }

                // Ticks interval calculation
                float step = 100f;
                if (Zoom >= 2.5f) step = 20f;
                else if (Zoom >= 1.2f) step = 50f;
                else if (Zoom >= 0.5f) step = 100f;
                else step = 200f;

                // Draw Top Ruler Ticks & Numbers
                float minCanvasX = ScreenToCanvas(rulerLeft + RulerThickness, rulerTop, width, height).X;
                float maxCanvasX = ScreenToCanvas(rulerLeft + availW, rulerTop, width, height).X;
                float startX = (float)Math.Floor(minCanvasX / step) * step;

                for (float cx = startX; cx <= maxCanvasX; cx += step)
                {
                    PointF sp = CanvasToScreen(cx, 0, width, height);
                    if (sp.X >= rulerLeft + RulerThickness && sp.X <= rulerLeft + availW)
                    {
                        g.DrawLine(tickPen, sp.X, rulerTop + RulerThickness - 7, sp.X, rulerTop + RulerThickness);
                        g.DrawString(((int)cx).ToString(), numFont, textBrush, sp.X + 2, rulerTop + 1);
                    }
                    float midX = cx + step / 2f;
                    PointF midSp = CanvasToScreen(midX, 0, width, height);
                    if (midSp.X >= rulerLeft + RulerThickness && midSp.X <= rulerLeft + availW)
                    {
                        g.DrawLine(tickPen, midSp.X, rulerTop + RulerThickness - 4, midSp.X, rulerTop + RulerThickness);
                    }
                }

                // Draw Left Ruler Ticks & Numbers
                float minCanvasY = ScreenToCanvas(rulerLeft, rulerTop + RulerThickness, width, height).Y;
                float maxCanvasY = ScreenToCanvas(rulerLeft, rulerTop + availH, width, height).Y;
                float startY = (float)Math.Floor(minCanvasY / step) * step;

                for (float cy = startY; cy <= maxCanvasY; cy += step)
                {
                    PointF sp = CanvasToScreen(0, cy, width, height);
                    if (sp.Y >= rulerTop + RulerThickness && sp.Y <= rulerTop + availH)
                    {
                        g.DrawLine(tickPen, rulerLeft + RulerThickness - 7, sp.Y, rulerLeft + RulerThickness, sp.Y);
                        string numStr = ((int)cy).ToString();
                        if (numStr.Length > 0)
                        {
                            g.DrawString(numStr[0].ToString(), numFont, textBrush, rulerLeft + 2, sp.Y + 1);
                        }
                    }
                    float midY = cy + step / 2f;
                    PointF midSp = CanvasToScreen(0, midY, width, height);
                    if (midSp.Y >= rulerTop + RulerThickness && midSp.Y <= rulerTop + availH)
                    {
                        g.DrawLine(tickPen, rulerLeft + RulerThickness - 4, midSp.Y, rulerLeft + RulerThickness, midSp.Y);
                    }
                }

                // Cursor tracking ticks on rulers
                if (CurrentMouseCanvasPoint != PointF.Empty)
                {
                    PointF mouseScr = CanvasToScreen(CurrentMouseCanvasPoint.X, CurrentMouseCanvasPoint.Y, width, height);
                    if (mouseScr.X >= rulerLeft + RulerThickness && mouseScr.X <= rulerLeft + availW)
                    {
                        g.DrawLine(cursorPen, mouseScr.X, rulerTop, mouseScr.X, rulerTop + RulerThickness);
                    }
                    if (mouseScr.Y >= rulerTop + RulerThickness && mouseScr.Y <= rulerTop + availH)
                    {
                        g.DrawLine(cursorPen, rulerLeft, mouseScr.Y, rulerLeft + RulerThickness, mouseScr.Y);
                    }
                }
            }
        }

        private void DrawCheckerboardGrid(D2DGraphics g, RectangleF rect, float cellSize)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            using (SolidBrush whiteB = new SolidBrush(Color.White))
            using (SolidBrush grayB = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.FillRectangle(whiteB, rect);
                cellSize = Math.Max(8f, Math.Min(32f, cellSize));
                int cols = (int)Math.Ceiling(rect.Width / cellSize);
                int rows = (int)Math.Ceiling(rect.Height / cellSize);

                g.SetClip(rect);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if ((r + c) % 2 == 1)
                        {
                            g.FillRectangle(grayB, rect.X + c * cellSize, rect.Y + r * cellSize, cellSize, cellSize);
                        }
                    }
                }
                g.ResetClip();
            }

            // Subtle Artboard Border
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private void RenderLayerD2D(D2DGraphics g, PhotoLayer l)
        {
            GraphicsState state = g.Save();

            if (l.Rotation != 0f)
            {
                PointF center = new PointF(l.Bounds.X + l.Bounds.Width / 2f, l.Bounds.Y + l.Bounds.Height / 2f);
                g.TranslateTransform(center.X, center.Y);
                g.RotateTransform(l.Rotation);
                g.TranslateTransform(-center.X, -center.Y);
            }

            // Clip Mask
            if (l.ClipMask != null)
            {
                using (GraphicsPath clipGp = l.ClipMask.BuildGraphicsPath())
                {
                    g.SetClip(clipGp, CombineMode.Intersect);
                }
            }

            if (l.Type == LayerType.Raster && l.Image != null)
            {
                lock (l.Image)
                {
                    g.DrawImage(l.Image, l.Bounds, l.Opacity);
                }
            }
            else if (l.Type == LayerType.VectorShape)
            {
                int alpha = (int)(l.Opacity * 255f);
                Color fill = Color.FromArgb((int)(alpha * (l.FillColor.A / 255f)), l.FillColor.R, l.FillColor.G, l.FillColor.B);
                Color fill2 = Color.FromArgb((int)(alpha * (l.FillColor2.A / 255f)), l.FillColor2.R, l.FillColor2.G, l.FillColor2.B);
                Color stroke = Color.FromArgb((int)(alpha * (l.StrokeColor.A / 255f)), l.StrokeColor.R, l.StrokeColor.G, l.StrokeColor.B);

                using (GraphicsPath path = l.BuildGraphicsPath())
                {
                    if (l.HasShadow)
                    {
                        using (GraphicsPath sPath = (GraphicsPath)path.Clone())
                        using (Matrix sm = new Matrix())
                        {
                            sm.Translate(4f, 4f);
                            sPath.Transform(sm);
                            using (SolidBrush shadowB = new SolidBrush(Color.FromArgb((int)(alpha * 0.25f), 0, 0, 0)))
                            {
                                g.FillPath(shadowB, sPath);
                            }
                        }
                    }

                    // Fill rendering
                    if (l.FillType == VectorFillType.Solid && fill.A > 0)
                    {
                        using (SolidBrush fillB = new SolidBrush(fill))
                        {
                            g.FillPath(fillB, path);
                        }
                    }
                    else if (l.FillType == VectorFillType.LinearGradient && (fill.A > 0 || fill2.A > 0))
                    {
                        RectangleF b = l.Bounds;
                        if (b.Width > 1 && b.Height > 1)
                        {
                            using (LinearGradientBrush lgb = new LinearGradientBrush(b, fill, fill2, l.FillGradientAngle))
                            {
                                if (l.GradientStops != null && l.GradientStops.Count >= 2)
                                {
                                    var sorted = l.GradientStops.OrderBy(s => s.Position).ToList();
                                    ColorBlend cb = new ColorBlend(sorted.Count);
                                    cb.Colors = sorted.Select(s => Color.FromArgb((int)(alpha * (s.Color.A / 255f)), s.Color.R, s.Color.G, s.Color.B)).ToArray();
                                    cb.Positions = sorted.Select(s => Math.Max(0f, Math.Min(1f, s.Position))).ToArray();
                                    cb.Positions[0] = 0f;
                                    cb.Positions[cb.Positions.Length - 1] = 1f;
                                    lgb.InterpolationColors = cb;
                                }
                                g.FillPath(lgb, path);
                            }
                        }
                    }
                    else if (l.FillType == VectorFillType.RadialGradient && (fill.A > 0 || fill2.A > 0))
                    {
                        RectangleF b = l.Bounds;
                        if (b.Width > 1 && b.Height > 1 && path.PointCount >= 2)
                        {
                            using (PathGradientBrush pgb = new PathGradientBrush(path))
                            {
                                if (l.GradientStops != null && l.GradientStops.Count >= 2)
                                {
                                    var sorted = l.GradientStops.OrderBy(s => s.Position).ToList();
                                    ColorBlend cb = new ColorBlend(sorted.Count);
                                    cb.Colors = sorted.Select(s => Color.FromArgb((int)(alpha * (s.Color.A / 255f)), s.Color.R, s.Color.G, s.Color.B)).ToArray();
                                    cb.Positions = sorted.Select(s => Math.Max(0f, Math.Min(1f, s.Position))).ToArray();
                                    cb.Positions[0] = 0f;
                                    cb.Positions[cb.Positions.Length - 1] = 1f;
                                    pgb.InterpolationColors = cb;
                                }
                                else
                                {
                                    pgb.CenterColor = fill;
                                    pgb.SurroundColors = new Color[] { fill2 };
                                }
                                g.FillPath(pgb, path);
                            }
                        }
                    }

                    // Stroke rendering
                    if (l.StrokeType != VectorStrokeType.None && stroke.A > 0 && l.StrokeWidth > 0)
                    {
                        using (Pen strokeP = new Pen(stroke, l.StrokeWidth))
                        {
                            strokeP.DashStyle = l.StrokeDashStyle;
                            strokeP.LineJoin = l.StrokeLineJoin;
                            strokeP.StartCap = l.StrokeLineCap;
                            strokeP.EndCap = l.StrokeLineCap;
                            g.DrawPath(strokeP, path);
                        }
                    }
                }
            }
            else if (l.Type == LayerType.Text && !string.IsNullOrEmpty(l.TextContent))
            {
                int alpha = (int)(l.Opacity * 255f);
                Color col = Color.FromArgb((int)(alpha * (l.TextColor.A / 255f)), l.TextColor.R, l.TextColor.G, l.TextColor.B);
                using (Font f = new Font(l.FontName ?? "Segoe UI", Math.Max(6f, l.FontSize), l.FontStyle))
                using (SolidBrush tb = new SolidBrush(col))
                {
                    if (l.IsTextOnPath && l.TextPathSubPaths != null && l.TextPathSubPaths.Count > 0)
                    {
                        RenderTextOnPathD2D(g, l, f, tb);
                    }
                    else
                    {
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
                        {
                            g.DrawString(l.TextContent, f, tb, l.Bounds, sf);
                        }
                    }
                }
            }

            g.Restore(state);
        }

        private void RenderTextOnPathD2D(D2DGraphics g, PhotoLayer l, Font font, Brush brush)
        {
            if (l.TextPathSubPaths == null || l.TextPathSubPaths.Count == 0 || string.IsNullOrEmpty(l.TextContent)) return;

            var sp = l.TextPathSubPaths[0];
            VectorPathModel.SampleSubPath(sp, 400, out var samplePoints, out var cumDistances);
            if (samplePoints.Count < 2 || cumDistances.Count < 2) return;

            float totalPathLength = cumDistances[cumDistances.Count - 1];
            if (totalPathLength <= 1f) return;

            string text = l.TextContent;
            float currentDist = 0f;

            for (int chIdx = 0; chIdx < text.Length; chIdx++)
            {
                string chStr = text[chIdx].ToString();
                SizeF charSize = g.MeasureString(chStr, font);
                float charW = Math.Max(3f, charSize.Width * 0.72f);
                float midDist = currentDist + charW / 2f;

                if (midDist > totalPathLength)
                {
                    if (!sp.IsClosed) break;
                    midDist %= totalPathLength;
                }

                int k = 0;
                while (k < cumDistances.Count - 2 && cumDistances[k + 1] < midDist)
                {
                    k++;
                }

                float d0 = cumDistances[k];
                float d1 = cumDistances[k + 1];
                float segLen = d1 - d0;
                float t = (segLen > 0.0001f) ? (midDist - d0) / segLen : 0f;

                PointF p0 = samplePoints[k];
                PointF p1 = samplePoints[k + 1];
                PointF pt = new PointF(p0.X + (p1.X - p0.X) * t, p0.Y + (p1.Y - p0.Y) * t);

                float dx = p1.X - p0.X;
                float dy = p1.Y - p0.Y;
                if (Math.Abs(dx) < 0.0001f && Math.Abs(dy) < 0.0001f && k + 2 < samplePoints.Count)
                {
                    dx = samplePoints[k + 2].X - p0.X;
                    dy = samplePoints[k + 2].Y - p0.Y;
                }
                float angleDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);

                GraphicsState chState = g.Save();
                g.TranslateTransform(pt.X, pt.Y);
                g.RotateTransform(angleDeg);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far })
                {
                    g.DrawString(chStr, font, brush, 0, 0, sf);
                }
                g.Restore(chState);

                currentDist += charW;
            }
        }

        public RectangleF GetCombinedSelectionBounds()
        {
            if (Document == null || Document.SelectedLayers.Count == 0) return RectangleF.Empty;
            var layers = Document.SelectedLayers;
            if (layers.Count == 1) return layers[0].Bounds;

            float minX = layers.Min(l => l.Bounds.Left);
            float minY = layers.Min(l => l.Bounds.Top);
            float maxX = layers.Max(l => l.Bounds.Right);
            float maxY = layers.Max(l => l.Bounds.Bottom);
            return new RectangleF(minX, minY, Math.Max(2f, maxX - minX), Math.Max(2f, maxY - minY));
        }

        private int CheckTransformHandles(int x, int y, int viewW, int viewH)
        {
            if (Document.ActiveLayer == null) return -1;

            if (Document.SelectedLayers.Count > 1)
            {
                RectangleF comb = GetCombinedSelectionBounds();
                PointF pTL = CanvasToScreen(comb.X, comb.Y, viewW, viewH);
                PointF pBR = CanvasToScreen(comb.Right, comb.Bottom, viewW, viewH);
                RectangleF sRectComb = new RectangleF(pTL.X, pTL.Y, pBR.X - pTL.X, pBR.Y - pTL.Y);

                PointF[] combHandles = GetHandlePoints(sRectComb);
                float hSz = 12f;
                for (int i = 0; i < 8; i++)
                {
                    RectangleF hRect = new RectangleF(combHandles[i].X - hSz / 2f, combHandles[i].Y - hSz / 2f, hSz, hSz);
                    if (hRect.Contains(x, y)) return i;
                }

                if (SelectedTool == PhotoTool.Move)
                {
                    PointF rotPin = new PointF(sRectComb.X + sRectComb.Width / 2f, sRectComb.Y - 18f);
                    RectangleF rotRect = new RectangleF(rotPin.X - 8f, rotPin.Y - 8f, 16f, 16f);
                    if (rotRect.Contains(x, y)) return 8;
                }

                PointF canvasPt = ScreenToCanvas(x, y, viewW, viewH);
                if (comb.Contains(canvasPt)) return 9; // Move body

                return -1;
            }

            PhotoLayer layer = Document.ActiveLayer;

            float mx = x;
            float my = y;
            if (layer.Rotation != 0f)
            {
                PointF center = CanvasToScreen(layer.Bounds.X + layer.Bounds.Width / 2f, layer.Bounds.Y + layer.Bounds.Height / 2f, viewW, viewH);
                float angle = -layer.Rotation * (float)Math.PI / 180f;
                float dx = mx - center.X;
                float dy = my - center.Y;
                mx = center.X + (float)(dx * Math.Cos(angle) - dy * Math.Sin(angle));
                my = center.Y + (float)(dx * Math.Sin(angle) + dy * Math.Cos(angle));
            }

            PointF pTopLeft = CanvasToScreen(layer.Bounds.X, layer.Bounds.Y, viewW, viewH);
            PointF pBottomRight = CanvasToScreen(layer.Bounds.Right, layer.Bounds.Bottom, viewW, viewH);
            RectangleF sRect = new RectangleF(pTopLeft.X, pTopLeft.Y, pBottomRight.X - pTopLeft.X, pBottomRight.Y - pTopLeft.Y);

            // Kiểm tra núm tròn bo góc trước (Ưu tiên tuyệt đối khi là hình chữ nhật - Chuẩn Inkscape)
            bool isRectShape = (layer.Type == LayerType.VectorShape && 
                (layer.ShapeType == VectorShapeType.Rectangle || layer.ShapeType == VectorShapeType.RoundedRect));
            if (isRectShape)
            {
                float radPx = layer.CornerRadius * Zoom;
                float maxR = Math.Min(sRect.Width, sRect.Height) / 2f;
                radPx = Math.Max(0f, Math.Min(maxR, radPx));

                // Handle 10: Núm tròn cạnh phải (Ry)
                PointF roundPinY = new PointF(sRect.Right, sRect.Top + radPx);
                RectangleF rHitY = new RectangleF(roundPinY.X - 10f, roundPinY.Y - 10f, 20f, 20f);
                if (rHitY.Contains(mx, my)) return 10;

                // Handle 11: Núm tròn cạnh trên (Rx)
                PointF roundPinX = new PointF(sRect.Right - radPx, sRect.Top);
                RectangleF rHitX = new RectangleF(roundPinX.X - 10f, roundPinX.Y - 10f, 20f, 20f);
                if (rHitX.Contains(mx, my)) return 11;
            }

            PointF[] handles = GetHandlePoints(sRect);
            float hSize = 12f; // larger hit area
            for (int i = 0; i < 8; i++)
            {
                if (isRectShape && i == 2) continue; // nhường chỗ cho núm tròn bo góc
                RectangleF hRect = new RectangleF(handles[i].X - hSize / 2f, handles[i].Y - hSize / 2f, hSize, hSize);
                if (hRect.Contains(mx, my)) return i;
            }
            
            // Rotation handle (chỉ khi chọn công cụ di chuyển)
            if (SelectedTool == PhotoTool.Move)
            {
                PointF rotPin = new PointF(sRect.X + sRect.Width / 2f, sRect.Y - 18f);
                RectangleF rotRect = new RectangleF(rotPin.X - 8f, rotPin.Y - 8f, 16f, 16f);
                if (rotRect.Contains(mx, my)) return 8;
            }

            return -1;
        }

        private void DrawCombinedTransformBoundingBox(D2DGraphics g, int viewW, int viewH)
        {
            RectangleF comb = GetCombinedSelectionBounds();
            if (comb.IsEmpty) return;

            PointF pTopLeft = CanvasToScreen(comb.X, comb.Y, viewW, viewH);
            PointF pBottomRight = CanvasToScreen(comb.Right, comb.Bottom, viewW, viewH);
            RectangleF sRect = new RectangleF(pTopLeft.X, pTopLeft.Y, pBottomRight.X - pTopLeft.X, pBottomRight.Y - pTopLeft.Y);

            using (Pen boxPen = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
            {
                g.DrawRectangle(boxPen, sRect.X, sRect.Y, sRect.Width, sRect.Height);
            }

            float cx = sRect.X + sRect.Width / 2f;
            float cy = sRect.Y + sRect.Height / 2f;
            using (Pen xPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
            {
                g.DrawLine(xPen, cx - 4, cy - 4, cx + 4, cy + 4);
                g.DrawLine(xPen, cx - 4, cy + 4, cx + 4, cy - 4);
            }

            float hSize = 7f;
            PointF[] handles = GetHandlePoints(sRect);
            using (SolidBrush hBrush = new SolidBrush(Color.White))
            using (Pen hPen = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
            {
                for (int i = 0; i < 8; i++)
                {
                    RectangleF hRect = new RectangleF(handles[i].X - hSize / 2f, handles[i].Y - hSize / 2f, hSize, hSize);
                    g.FillRectangle(hBrush, hRect);
                    g.DrawRectangle(hPen, hRect.X, hRect.Y, hRect.Width, hRect.Height);
                }

                if (SelectedTool == PhotoTool.Move)
                {
                    PointF rotPin = new PointF(sRect.X + sRect.Width / 2f, sRect.Y - 18f);
                    PointF topMid = new PointF(sRect.X + sRect.Width / 2f, sRect.Y);
                    using (Pen stemPen = new Pen(Color.FromArgb(59, 130, 246), 1.0f))
                    {
                        stemPen.DashStyle = DashStyle.Dot;
                        g.DrawLine(stemPen, topMid.X, topMid.Y, rotPin.X, rotPin.Y);
                    }
                    RectangleF rotRect = new RectangleF(rotPin.X - 4f, rotPin.Y - 4f, 8f, 8f);
                    g.FillEllipse(hBrush, rotRect);
                    g.DrawEllipse(hPen, rotRect);
                }
            }
        }

        private void DrawTransformBoundingBox(D2DGraphics g, PhotoLayer layer, int viewW, int viewH, bool isSecondary = false)
        {
            GraphicsState state = g.Save();

            if (layer.Rotation != 0f)
            {
                PointF center = CanvasToScreen(layer.Bounds.X + layer.Bounds.Width / 2f, layer.Bounds.Y + layer.Bounds.Height / 2f, viewW, viewH);
                g.TranslateTransform(center.X, center.Y);
                g.RotateTransform(layer.Rotation);
                g.TranslateTransform(-center.X, -center.Y);
            }

            PointF pTopLeft = CanvasToScreen(layer.Bounds.X, layer.Bounds.Y, viewW, viewH);
            PointF pBottomRight = CanvasToScreen(layer.Bounds.Right, layer.Bounds.Bottom, viewW, viewH);

            RectangleF sRect = new RectangleF(pTopLeft.X, pTopLeft.Y, pBottomRight.X - pTopLeft.X, pBottomRight.Y - pTopLeft.Y);

            if (isSecondary)
            {
                using (Pen secPen = new Pen(Color.FromArgb(99, 102, 241), 1.2f))
                {
                    secPen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(secPen, sRect.X, sRect.Y, sRect.Width, sRect.Height);
                }
                g.Restore(state);
                return;
            }

            bool isRectShape = (layer.Type == LayerType.VectorShape && 
                (layer.ShapeType == VectorShapeType.Rectangle || layer.ShapeType == VectorShapeType.RoundedRect));

            // Bounding box outline: Nét đứt màu xanh dương chuẩn Inkscape khi là hình chữ nhật (Xem ảnh 1)
            using (Pen boxPen = new Pen(isRectShape ? Color.FromArgb(59, 130, 246) : Color.FromArgb(51, 65, 85), 1.0f))
            {
                if (isRectShape) boxPen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(boxPen, sRect.X, sRect.Y, sRect.Width, sRect.Height);
            }

            // Dấu tâm X ở giữa đối tượng (Chuẩn Inkscape - Xem ảnh 1)
            float cx = sRect.X + sRect.Width / 2f;
            float cy = sRect.Y + sRect.Height / 2f;
            using (Pen xPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
            {
                g.DrawLine(xPen, cx - 4, cy - 4, cx + 4, cy + 4);
                g.DrawLine(xPen, cx - 4, cy + 4, cx + 4, cy - 4);
            }

            // 8 Resize Handles: Các núm vuông vắn màu trắng viền đen xám
            float hSize = 7f;
            PointF[] handles = GetHandlePoints(sRect);
            using (SolidBrush hBrush = new SolidBrush(Color.White))
            using (Pen hPen = new Pen(Color.FromArgb(51, 65, 85), 1.0f))
            {
                for (int i = 0; i < 8; i++)
                {
                    if (isRectShape && i == 2) continue; // góc trên-phải nhường chỗ cho núm tròn bo góc
                    RectangleF hRect = new RectangleF(handles[i].X - hSize / 2f, handles[i].Y - hSize / 2f, hSize, hSize);
                    g.FillRectangle(hBrush, hRect);
                    g.DrawRectangle(hPen, hRect.X, hRect.Y, hRect.Width, hRect.Height);
                }

                // Điểm xoay vuông vắn phía trên (chỉ hiện khi dùng công cụ Con trỏ chọn)
                if (SelectedTool == PhotoTool.Move)
                {
                    PointF rotPin = new PointF(sRect.X + sRect.Width / 2f, sRect.Y - 18f);
                    g.DrawLine(hPen, sRect.X + sRect.Width / 2f, sRect.Y, rotPin.X, rotPin.Y);
                    RectangleF rotRect = new RectangleF(rotPin.X - 3.5f, rotPin.Y - 3.5f, 7f, 7f);
                    g.FillRectangle(hBrush, rotRect);
                    g.DrawRectangle(hPen, rotRect.X, rotRect.Y, rotRect.Width, rotRect.Height);
                }
            }

            // NÚM TRÒN BO GÓC TƯƠNG TÁC TRỰC TIẾP TRÊN HÌNH (CHUẨN INKSCAPE - ẢNH 1)
            if (isRectShape)
            {
                float radPx = layer.CornerRadius * Zoom;
                float maxR = Math.Min(sRect.Width, sRect.Height) / 2f;
                radPx = Math.Max(0f, Math.Min(maxR, radPx));

                PointF roundPinY = new PointF(sRect.Right, sRect.Top + radPx);
                PointF roundPinX = new PointF(sRect.Right - radPx, sRect.Top);

                // Đường chỉ dẫn mờ khi bo góc > 0
                if (radPx > 0)
                {
                    using (Pen guidePen = new Pen(Color.FromArgb(160, 59, 130, 246), 1f))
                    {
                        guidePen.DashStyle = DashStyle.Dot;
                        g.DrawLine(guidePen, sRect.Right, sRect.Top, roundPinY.X, roundPinY.Y);
                        g.DrawLine(guidePen, sRect.Right, sRect.Top, roundPinX.X, roundPinX.Y);
                    }
                }

                // Núm tròn cạnh trên (Rx)
                RectangleF circleRectX = new RectangleF(roundPinX.X - 5f, roundPinX.Y - 5f, 10f, 10f);
                using (SolidBrush wb = new SolidBrush(Color.White))
                using (Pen cp = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
                {
                    g.FillEllipse(wb, circleRectX);
                    g.DrawEllipse(cp, circleRectX);
                }

                // Núm tròn kéo bo góc cạnh phải (Ry) - Màu xanh dương nổi bật chuẩn ảnh 1 Inkscape
                RectangleF circleRectY = new RectangleF(roundPinY.X - 6f, roundPinY.Y - 6f, 12f, 12f);
                using (SolidBrush blueB = new SolidBrush(Color.FromArgb(37, 99, 235)))
                using (Pen whiteP = new Pen(Color.White, 1.4f))
                using (Pen borderP = new Pen(Color.FromArgb(30, 41, 59), 1.0f))
                {
                    g.FillEllipse(blueB, circleRectY);
                    g.DrawEllipse(whiteP, circleRectY);
                    g.DrawEllipse(borderP, circleRectY.X - 0.5f, circleRectY.Y - 0.5f, circleRectY.Width + 1f, circleRectY.Height + 1f);
                }
            }

            g.Restore(state);
        }

        private PointF[] GetHandlePoints(RectangleF r)
        {
            return new PointF[]
            {
                new PointF(r.Left, r.Top),                   // 0: NW
                new PointF(r.Left + r.Width / 2f, r.Top),    // 1: N
                new PointF(r.Right, r.Top),                  // 2: NE
                new PointF(r.Right, r.Top + r.Height / 2f),  // 3: E
                new PointF(r.Right, r.Bottom),               // 4: SE
                new PointF(r.Left + r.Width / 2f, r.Bottom), // 5: S
                new PointF(r.Left, r.Bottom),                // 6: SW
                new PointF(r.Left, r.Top + r.Height / 2f)    // 7: W
            };
        }

        private void DrawCropOverlay(D2DGraphics g, RectangleF artboard, int viewW, int viewH)
        {
            PointF c1 = CanvasToScreen(CropRect.X, CropRect.Y, viewW, viewH);
            PointF c2 = CanvasToScreen(CropRect.Right, CropRect.Bottom, viewW, viewH);
            RectangleF screenCrop = new RectangleF(c1.X, c1.Y, c2.X - c1.X, c2.Y - c1.Y);

            // Dim outside region
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                // Top
                g.FillRectangle(dimBrush, artboard.X, artboard.Y, artboard.Width, screenCrop.Y - artboard.Y);
                // Bottom
                g.FillRectangle(dimBrush, artboard.X, screenCrop.Bottom, artboard.Width, artboard.Bottom - screenCrop.Bottom);
                // Left
                g.FillRectangle(dimBrush, artboard.X, screenCrop.Y, screenCrop.X - artboard.X, screenCrop.Height);
                // Right
                g.FillRectangle(dimBrush, screenCrop.Right, screenCrop.Y, artboard.Right - screenCrop.Right, screenCrop.Height);
            }

            using (Pen cropPen = new Pen(Color.White, 2f))
            {
                cropPen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(cropPen, screenCrop.X, screenCrop.Y, screenCrop.Width, screenCrop.Height);
            }
        }
        #endregion

        #region Modern Light Theme UI Controls
        
        #region Tool Controls Bar Helpers
        private void DrawD2DSlider(D2DGraphics g, Rectangle trackRect, float min, float max, float currentVal)
        {
            float norm = Math.Max(0f, Math.Min(1f, (currentVal - min) / (max - min)));
            int fillW = (int)(trackRect.Width * norm);
            int thumbX = trackRect.X + fillW;
            int thumbY = trackRect.Y + trackRect.Height / 2;
            Rectangle thumbRect = new Rectangle(thumbX - 6, thumbY - 6, 12, 12);

            using (SolidBrush bgB = new SolidBrush(Color.FromArgb(226, 232, 240)))
            using (SolidBrush fillB = new SolidBrush(Color.FromArgb(99, 102, 241)))
            using (SolidBrush thumbB = new SolidBrush(Color.White))
            using (Pen thumbP = new Pen(Color.FromArgb(79, 70, 229), 2f))
            {
                g.FillRoundedRectangle(bgB, trackRect, 3f);
                if (fillW > 0)
                {
                    Rectangle fillRect = new Rectangle(trackRect.X, trackRect.Y, fillW, trackRect.Height);
                    g.FillRoundedRectangle(fillB, fillRect, 3f);
                }
                g.FillEllipse(thumbB, thumbRect);
                g.DrawEllipse(thumbP, thumbRect);
            }
        }

        private void UpdateBrushSizeFromSlider(int mouseX)
        {
            if (_brushSizeTrackRect.Width <= 0) return;
            float ratio = Math.Max(0f, Math.Min(1f, (float)(mouseX - _brushSizeTrackRect.X) / _brushSizeTrackRect.Width));
            BrushSize = Math.Max(1f, (float)Math.Round(1f + ratio * 149f));
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.ActiveLayer.StrokeWidth = BrushSize;
            }
        }

        private void UpdateRadiusFromSlider(int mouseX)
        {
            if (_radiusTrackRect.Width <= 0) return;
            float ratio = Math.Max(0f, Math.Min(1f, (float)(mouseX - _radiusTrackRect.X) / _radiusTrackRect.Width));
            ShapeCornerRadius = (float)Math.Round(ratio * 80f);
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.ActiveLayer.CornerRadius = ShapeCornerRadius;
                if (ShapeCornerRadius > 0 && Document.ActiveLayer.ShapeType == VectorShapeType.Rectangle)
                    Document.ActiveLayer.ShapeType = VectorShapeType.RoundedRect;
                else if (ShapeCornerRadius <= 0 && Document.ActiveLayer.ShapeType == VectorShapeType.RoundedRect)
                    Document.ActiveLayer.ShapeType = VectorShapeType.Rectangle;
            }
        }

        private void UpdateFontSizeFromSlider(int mouseX)
        {
            if (_fontSizeTrackRect.Width <= 0) return;
            float ratio = Math.Max(0f, Math.Min(1f, (float)(mouseX - _fontSizeTrackRect.X) / _fontSizeTrackRect.Width));
            ActiveFontSize = Math.Max(8f, (float)Math.Round(8f + ratio * 112f));
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text)
            {
                Document.ActiveLayer.FontSize = ActiveFontSize;
            }
        }

        private void DrawTopBarButton(D2DGraphics g, int x, int y, int w, int h, string text, Action action, bool isActive = false)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            _topBarClickables.Add((r, action));

            Color bgColor = isActive ? Color.FromArgb(238, 242, 255) : Color.FromArgb(241, 245, 249);
            Color borderColor = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(203, 213, 225);
            Color textColor = isActive ? Color.FromArgb(79, 70, 229) : Color.FromArgb(30, 41, 59);

            using (SolidBrush b = new SolidBrush(bgColor))
            using (Pen p = new Pen(borderColor, 1f))
            using (Font font = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(textColor))
            {
                g.FillRoundedRectangle(b, r, 5f);
                g.DrawRoundedRectangle(p, r, 5f);
                var sz = g.MeasureString(text, font);
                g.DrawString(text, font, tb, x + (w - sz.Width) / 2f, y + (h - sz.Height) / 2f);
            }
        }

        private void DrawTopBarGlyphButton(D2DGraphics g, int x, int y, int w, int h, char glyph, Action action)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            _topBarClickables.Add((r, action));

            using (SolidBrush b = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillRoundedRectangle(b, r, 5f);
                g.DrawRoundedRectangle(p, r, 5f);
                Rectangle gBox = new Rectangle(x + (w - 14) / 2, y + (h - 14) / 2, 14, 14);
                FluentIconRenderer.DrawGlyphD2D(g, glyph, gBox, Color.FromArgb(71, 85, 105), 12f);
            }
        }

        private void DrawTopBarNodeIconButton(D2DGraphics g, int x, int y, string symbol, Action action, bool isActive = false, int w = 32)
        {
            int h = 26;
            Rectangle r = new Rectangle(x, y, w, h);
            _topBarClickables.Add((r, action));

            Color bgColor = isActive ? Color.FromArgb(224, 231, 255) : Color.FromArgb(241, 245, 249);
            Color borderColor = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(203, 213, 225);
            Color textColor = isActive ? Color.FromArgb(67, 56, 202) : Color.FromArgb(30, 41, 59);

            using (SolidBrush b = new SolidBrush(bgColor))
            using (Pen p = new Pen(borderColor, 1f))
            using (Font font = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(textColor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.FillRoundedRectangle(b, r, 4f);
                g.DrawRoundedRectangle(p, r, 4f);
                g.DrawString(symbol, font, tb, r, sf);
            }
        }

        private void DrawTopBarColorSwatch(D2DGraphics g, int x, int y, int size, Color color, Action onClick)
        {
            DrawTopBarColorSwatch(g, x, y, size, size, color, onClick);
        }

        private void DrawTopBarColorSwatch(D2DGraphics g, int x, int y, int w, int h, Color color, Action onClick)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            _topBarClickables.Add((r, onClick));

            using (Pen borderP = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
            {
                if (color.A == 0)
                {
                    using (SolidBrush wb = new SolidBrush(Color.White))
                    using (Pen redSlash = new Pen(Color.FromArgb(239, 68, 68), 1.8f))
                    {
                        g.FillRoundedRectangle(wb, r, 4f);
                        g.DrawRoundedRectangle(borderP, r, 4f);
                        g.DrawLine(redSlash, r.Left + 3, r.Bottom - 3, r.Right - 3, r.Top + 3);
                    }
                }
                else
                {
                    using (SolidBrush cb = new SolidBrush(color))
                    {
                        g.FillRoundedRectangle(cb, r, 4f);
                        g.DrawRoundedRectangle(borderP, r, 4f);
                    }
                }
            }
        }

        private int DrawTopBarValueBox(D2DGraphics g, int x, int y, string label, string valStr, Action onDec, Action onInc, Action onEdit = null, int boxWidth = 80)
        {
            using (Font lblFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (Font valFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            using (SolidBrush lblB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush valB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush boxBg = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                if (!string.IsNullOrEmpty(label))
                {
                    g.DrawString(label, lblFont, lblB, x, y + 4);
                    x += (int)g.MeasureString(label, lblFont).Width + 4;
                }

                int h = 26;
                Rectangle boxRect = new Rectangle(x, y, boxWidth, h);
                g.FillRoundedRectangle(boxBg, boxRect, 4f);
                g.DrawRoundedRectangle(borderPen, boxRect, 4f);

                Rectangle valRect = new Rectangle(boxRect.X + 2, boxRect.Y, boxWidth - 31, h);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    g.DrawString(valStr, valFont, valB, valRect, sf);
                }
                if (onEdit != null)
                {
                    _topBarClickables.Add((valRect, onEdit));
                }

                int btnW = 14;
                Rectangle decRect = new Rectangle(boxRect.Right - 29, boxRect.Y + 2, btnW, h - 4);
                _topBarClickables.Add((decRect, onDec));

                Rectangle incRect = new Rectangle(boxRect.Right - 14, boxRect.Y + 2, btnW, h - 4);
                _topBarClickables.Add((incRect, onInc));

                using (SolidBrush btnBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen btnBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                using (Font stepFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillRoundedRectangle(btnBg, decRect, 3f);
                    g.DrawRoundedRectangle(btnBorder, decRect, 3f);
                    g.DrawString("-", stepFont, valB, decRect, csf);

                    g.FillRoundedRectangle(btnBg, incRect, 3f);
                    g.DrawRoundedRectangle(btnBorder, incRect, 3f);
                    g.DrawString("+", stepFont, valB, incRect, csf);
                }

                return boxRect.Right + 8;
            }
        }

        private float? ShowNumberInputDialog(string title, float currentVal)
        {
            using (Form f = new Form()
            {
                Text = title,
                Size = new Size(280, 140),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false
            })
            {
                TextBox tb = new TextBox()
                {
                    Text = currentVal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                    Font = new Font("Segoe UI", 11f),
                    Location = new Point(20, 16),
                    Width = 220
                };
                Button btnOk = new Button()
                {
                    Text = "Xác nhận",
                    Location = new Point(120, 56),
                    Size = new Size(120, 32),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Click += (s, e) => { f.DialogResult = DialogResult.OK; };
                f.Controls.Add(tb);
                f.Controls.Add(btnOk);
                f.AcceptButton = btnOk;
                if (f.ShowDialog() == DialogResult.OK)
                {
                    string raw = tb.Text.Trim().Replace(',', '.');
                    if (float.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float res))
                    {
                        return res;
                    }
                }
                return null;
            }
        }

        private bool IsShapeTool(PhotoTool t) =>
            t == PhotoTool.Shape || t == PhotoTool.Rectangle || t == PhotoTool.Ellipse || t == PhotoTool.Star;

        private VectorShapeType GetShapeTypeForTool(PhotoTool t)
        {
            if (t == PhotoTool.Rectangle)
                return (ShapeCornerRadius > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
            if (t == PhotoTool.Ellipse)
                return VectorShapeType.Ellipse;
            if (t == PhotoTool.Star)
                return ActiveShapeType;
            return ActiveShapeType;
        }

        private void DrawTopBarSeparator(D2DGraphics g, ref int curX)
        {
            curX += 4;
            using (Pen sepPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawLine(sepPen, curX, RibbonHeight + 11, curX, RibbonHeight + 33);
            }
            curX += 8;
        }

        private void DrawNodeToolCanvasOverlay(D2DGraphics g, PhotoLayer layer, int viewW, int viewH)
        {
            if (layer == null) return;
            layer.EnsureNodesInitialized();
            if (layer.SubPaths == null || layer.SubPaths.Count == 0) return;

            // 1. Đường tay đòn và núm tròn tiếp tuyến cho các neo ĐANG ĐƯỢC CHỌN (Chuẩn Inkscape - Ảnh 2 & 3)
            using (Pen armPen = new Pen(Color.FromArgb(99, 102, 241), 1.1f))            // Màu tím/xanh nhạt chuẩn Inkscape
            using (SolidBrush knobFill = new SolidBrush(Color.FromArgb(37, 99, 235)))    // Núm tròn xanh dương rực rỡ
            using (Pen knobBorder = new Pen(Color.FromArgb(15, 23, 42), 1.2f))           // Viền đen rõ nét
            {
                foreach (var sp in layer.SubPaths)
                {
                    if (sp.Nodes == null) continue;
                    for (int i = 0; i < sp.Nodes.Count; i++)
                    {
                        var node = sp.Nodes[i];
                        if (!node.IsSelected) continue;

                        PointF anchorScreen = CanvasToScreen(node.Point.X, node.Point.Y, viewW, viewH);

                        // Tay đòn InHandle
                        if (node.HasInHandle && (Math.Abs(node.InHandle.X - node.Point.X) > 0.5f || Math.Abs(node.InHandle.Y - node.Point.Y) > 0.5f))
                        {
                            PointF inScreen = CanvasToScreen(node.InHandle.X, node.InHandle.Y, viewW, viewH);
                            g.DrawLine(armPen, anchorScreen.X, anchorScreen.Y, inScreen.X, inScreen.Y);

                            RectangleF knobRect = new RectangleF(inScreen.X - 4f, inScreen.Y - 4f, 8f, 8f);
                            g.FillEllipse(knobFill, knobRect);
                            g.DrawEllipse(knobBorder, knobRect);
                        }

                        // Tay đòn OutHandle
                        if (node.HasOutHandle && (Math.Abs(node.OutHandle.X - node.Point.X) > 0.5f || Math.Abs(node.OutHandle.Y - node.Point.Y) > 0.5f))
                        {
                            PointF outScreen = CanvasToScreen(node.OutHandle.X, node.OutHandle.Y, viewW, viewH);
                            g.DrawLine(armPen, anchorScreen.X, anchorScreen.Y, outScreen.X, outScreen.Y);

                            RectangleF knobRect = new RectangleF(outScreen.X - 4f, outScreen.Y - 4f, 8f, 8f);
                            g.FillEllipse(knobFill, knobRect);
                            g.DrawEllipse(knobBorder, knobRect);
                        }
                    }
                }
            }

            // 2. Vẽ tất cả các điểm neo (Anchor Nodes) trên mọi SubPath (Chuẩn Inkscape - Ảnh 2 & 3)
            using (SolidBrush unselectedFill = new SolidBrush(Color.White))
            using (Pen unselectedBorder = new Pen(Color.FromArgb(15, 23, 42), 1.3f))     // Viền đen rõ nét
            using (SolidBrush selectedFill = new SolidBrush(Color.FromArgb(37, 99, 235))) // Nền xanh Inkscape
            using (Pen selectedBorder = new Pen(Color.FromArgb(15, 23, 42), 1.5f))       // Viền đen đậm
            {
                foreach (var sp in layer.SubPaths)
                {
                    if (sp.Nodes == null) continue;
                    for (int i = 0; i < sp.Nodes.Count; i++)
                    {
                        var node = sp.Nodes[i];
                        PointF p = CanvasToScreen(node.Point.X, node.Point.Y, viewW, viewH);

                        if (node.IsSelected)
                        {
                            // Neo đang được chọn: Hình vuông màu xanh viền đen nổi bật (Chuẩn Inkscape - Ảnh 3)
                            RectangleF sq = new RectangleF(p.X - 4.5f, p.Y - 4.5f, 9f, 9f);
                            g.FillRectangle(selectedFill, sq);
                            g.DrawRectangle(selectedBorder, sq.X, sq.Y, sq.Width, sq.Height);
                        }
                        else
                        {
                            // Neo chưa chọn:
                            if (node.NodeType == VectorNodeType.Corner)
                            {
                                // Góc nhọn: Hình quả trám / thoi trắng viền đen (Chuẩn Inkscape - Ảnh 3)
                                PointF[] diamond = new PointF[]
                                {
                                    new PointF(p.X, p.Y - 4.5f),
                                    new PointF(p.X + 4.5f, p.Y),
                                    new PointF(p.X, p.Y + 4.5f),
                                    new PointF(p.X - 4.5f, p.Y)
                                };
                                g.FillPolygon(unselectedFill, diamond);
                                g.DrawPolygon(unselectedBorder, diamond);
                            }
                            else
                            {
                                // Neo uốn mượt: Hình vuông trắng viền đen (Chuẩn Inkscape - Ảnh 3)
                                RectangleF sq = new RectangleF(p.X - 4f, p.Y - 4f, 8f, 8f);
                                g.FillRectangle(unselectedFill, sq);
                                g.DrawRectangle(unselectedBorder, sq.X, sq.Y, sq.Width, sq.Height);
                            }
                        }
                    }
                }
            }

            // 3. Vẽ khung Marquee box khi đang quét chuột trên vùng trống
            if (_isNodeToolDragging && _nodeDragTarget == NodeDragTarget.Marquee)
            {
                PointF sStart = CanvasToScreen(_dragStartPt.X, _dragStartPt.Y, viewW, viewH);
                PointF sCur = CanvasToScreen(_currentDragPt.X, _currentDragPt.Y, viewW, viewH);
                RectangleF marqueeBox = NormalizeRect(sStart, sCur);

                using (SolidBrush mFill = new SolidBrush(Color.FromArgb(38, 59, 130, 246))) // Xanh trong suốt 15%
                using (Pen mPen = new Pen(Color.FromArgb(59, 130, 246), 1.0f))
                {
                    mPen.DashStyle = DashStyle.Dash;
                    g.FillRectangle(mFill, marqueeBox);
                    g.DrawRectangle(mPen, marqueeBox.X, marqueeBox.Y, marqueeBox.Width, marqueeBox.Height);
                }
            }
        }

        private void HandleNodeToolMouseDown(PointF canvasPt, int screenX, int screenY, MouseButtons button, Keys modifiers, int viewW, int viewH)
        {
            if (button != MouseButtons.Left) return;

            _dragStartPt = canvasPt;
            _currentDragPt = canvasPt;
            _activeDragNode = null;
            _nodeDragInitialPoints.Clear();
            _nodeDragInitialInHandles.Clear();
            _nodeDragInitialOutHandles.Clear();

            float hitToleranceCanvas = 8.0f / Zoom;
            bool isShift = (modifiers & Keys.Shift) == Keys.Shift;

            // 1. ƯU TIÊN 1: Hit-test các núm tròn tiếp tuyến (InHandle / OutHandle) của các điểm neo ĐANG ĐƯỢC CHỌN
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                var layer = Document.ActiveLayer;
                layer.EnsureNodesInitialized();

                if (layer.SubPaths != null)
                {
                    // 1. ƯU TIÊN 1: Hit-test các núm tròn tiếp tuyến (InHandle / OutHandle) trên mọi SubPath
                    foreach (var sp in layer.SubPaths)
                    {
                        if (sp.Nodes == null) continue;
                        for (int i = 0; i < sp.Nodes.Count; i++)
                        {
                            var node = sp.Nodes[i];
                            if (!node.IsSelected) continue;

                            // Kiểm tra In-Handle
                            if (node.HasInHandle && Math.Abs(canvasPt.X - node.InHandle.X) <= hitToleranceCanvas && Math.Abs(canvasPt.Y - node.InHandle.Y) <= hitToleranceCanvas)
                            {
                                Document.SaveUndoSnapshot();
                                _nodeDragTarget = NodeDragTarget.InHandle;
                                _activeDragNode = node;
                                _isNodeToolDragging = true;
                                return;
                            }

                            // Kiểm tra Out-Handle
                            if (node.HasOutHandle && Math.Abs(canvasPt.X - node.OutHandle.X) <= hitToleranceCanvas && Math.Abs(canvasPt.Y - node.OutHandle.Y) <= hitToleranceCanvas)
                            {
                                Document.SaveUndoSnapshot();
                                _nodeDragTarget = NodeDragTarget.OutHandle;
                                _activeDragNode = node;
                                _isNodeToolDragging = true;
                                return;
                            }
                        }
                    }

                    // 2. ƯU TIÊN 2: Hit-test các điểm neo hình vuông (Anchor Nodes) trên mọi SubPath
                    foreach (var sp in layer.SubPaths)
                    {
                        if (sp.Nodes == null) continue;
                        for (int i = 0; i < sp.Nodes.Count; i++)
                        {
                            var node = sp.Nodes[i];
                            if (Math.Abs(canvasPt.X - node.Point.X) <= hitToleranceCanvas && Math.Abs(canvasPt.Y - node.Point.Y) <= hitToleranceCanvas)
                            {
                                if (isShift)
                                {
                                    node.IsSelected = !node.IsSelected;
                                }
                                else
                                {
                                    if (!node.IsSelected)
                                    {
                                        foreach (var sub in layer.SubPaths)
                                        {
                                            if (sub.Nodes != null)
                                            {
                                                foreach (var n in sub.Nodes) n.IsSelected = false;
                                            }
                                        }
                                        node.IsSelected = true;
                                        _pendingSingleSelectNode = null;
                                    }
                                    else
                                    {
                                        _pendingSingleSelectNode = node;
                                    }
                                }

                                Document.SaveUndoSnapshot();
                                _nodeDragTarget = NodeDragTarget.AnchorNode;
                                _activeDragNode = node;
                                _isNodeToolDragging = true;

                                // Lưu tọa độ ban đầu của tất cả các neo đang được chọn trên mọi SubPath để di chuyển đồng bộ
                                foreach (var sub in layer.SubPaths)
                                {
                                    if (sub.Nodes != null)
                                    {
                                        foreach (var n in sub.Nodes)
                                        {
                                            if (n.IsSelected)
                                            {
                                                _nodeDragInitialPoints[n] = n.Point;
                                                _nodeDragInitialInHandles[n] = n.InHandle;
                                                _nodeDragInitialOutHandles[n] = n.OutHandle;
                                            }
                                        }
                                    }
                                }
                                return;
                            }
                        }
                    }

                    // 3. ƯU TIÊN 3: Hit-test thân đoạn cong / thẳng trên mọi SubPath (Kéo uốn cong trực tiếp - Chuẩn Inkscape)
                    int hitSubPathIdx;
                    int hitSegIdx = VectorPathModel.HitTestSegment(layer, canvasPt, hitToleranceCanvas, out hitSubPathIdx);
                    if (hitSegIdx >= 0 && hitSubPathIdx >= 0 && hitSubPathIdx < layer.SubPaths.Count)
                    {
                        var hitSubPath = layer.SubPaths[hitSubPathIdx];
                        if (hitSubPath.Nodes != null && hitSubPath.Nodes.Count > 1)
                        {
                            if (!isShift)
                            {
                                foreach (var sub in layer.SubPaths)
                                {
                                    if (sub.Nodes != null)
                                    {
                                        foreach (var n in sub.Nodes) n.IsSelected = false;
                                    }
                                }
                            }
                            var subNodes = hitSubPath.Nodes;
                            int nextIdx = (hitSegIdx + 1) % subNodes.Count;
                            subNodes[hitSegIdx].IsSelected = true;
                            subNodes[nextIdx].IsSelected = true;

                            Document.SaveUndoSnapshot();
                            _nodeDragTarget = NodeDragTarget.Segment;
                            _dragSegmentSubPathIndex = hitSubPathIdx;
                            _dragSegmentIndex = hitSegIdx;
                            _isNodeToolDragging = true;

                            var nA = subNodes[hitSegIdx];
                            var nB = subNodes[nextIdx];

                            if (!nA.HasOutHandle)
                            {
                                nA.OutHandle = new PointF(nA.Point.X + (nB.Point.X - nA.Point.X) / 3f, nA.Point.Y + (nB.Point.Y - nA.Point.Y) / 3f);
                                nA.HasOutHandle = true;
                            }
                            if (!nB.HasInHandle)
                            {
                                nB.InHandle = new PointF(nB.Point.X - (nB.Point.X - nA.Point.X) / 3f, nB.Point.Y - (nB.Point.Y - nA.Point.Y) / 3f);
                                nB.HasInHandle = true;
                            }

                            _nodeDragInitialOutHandles[nA] = nA.OutHandle;
                            _nodeDragInitialInHandles[nB] = nB.InHandle;
                            return;
                        }
                    }
                }
            }

            // 4. ƯU TIÊN 4: Hit-test đối tượng hình khối vector khác trên canvas
            int hitOtherLayer = HitTestVectorLayerAt(canvasPt, hitToleranceCanvas);
            if (hitOtherLayer >= 0)
            {
                Document.ActiveLayerIndex = hitOtherLayer;
                var l = Document.ActiveLayer;
                l.EnsureNodesInitialized();
                if (l.SubPaths != null)
                {
                    foreach (var sp in l.SubPaths)
                    {
                        if (sp.Nodes != null)
                        {
                            foreach (var n in sp.Nodes) n.IsSelected = false;
                        }
                    }
                }
                return;
            }

            // 5. ƯU TIÊN 5: Vùng trống canvas -> Quét khung Marquee đa chọn
            if (!isShift && Document.ActiveLayer != null && Document.ActiveLayer.SubPaths != null)
            {
                foreach (var sp in Document.ActiveLayer.SubPaths)
                {
                    if (sp.Nodes != null)
                    {
                        foreach (var n in sp.Nodes) n.IsSelected = false;
                    }
                }
            }
            _nodeDragTarget = NodeDragTarget.Marquee;
            _isNodeToolDragging = true;
        }

        private void HandleNodeToolMouseMove(PointF canvasPt, int screenX, int screenY, MouseButtons button, Keys modifiers, int viewW, int viewH)
        {
            if (!_isNodeToolDragging || button != MouseButtons.Left || Document.ActiveLayer == null) return;

            var layer = Document.ActiveLayer;
            float dx = canvasPt.X - _dragStartPt.X;
            float dy = canvasPt.Y - _dragStartPt.Y;

            if (_pendingSingleSelectNode != null && (Math.Abs(dx) > 3f / Zoom || Math.Abs(dy) > 3f / Zoom))
            {
                _pendingSingleSelectNode = null;
            }

            switch (_nodeDragTarget)
            {
                case NodeDragTarget.AnchorNode:
                    foreach (var kv in _nodeDragInitialPoints)
                    {
                        var n = kv.Key;
                        PointF origPt = kv.Value;
                        n.Point = new PointF(origPt.X + dx, origPt.Y + dy);

                        if (_nodeDragInitialInHandles.TryGetValue(n, out PointF origIn))
                        {
                            n.InHandle = new PointF(origIn.X + dx, origIn.Y + dy);
                        }
                        if (_nodeDragInitialOutHandles.TryGetValue(n, out PointF origOut))
                        {
                            n.OutHandle = new PointF(origOut.X + dx, origOut.Y + dy);
                        }
                    }
                    VectorPathModel.UpdateAutoSmoothHandles(layer);
                    VectorPathModel.UpdateLayerBounds(layer);
                    break;

                case NodeDragTarget.InHandle:
                    if (_activeDragNode != null)
                    {
                        _activeDragNode.InHandle = canvasPt;
                        _activeDragNode.HasInHandle = true;
                        VectorPathModel.SynchronizeOppositeHandle(_activeDragNode, isDraggingInHandle: true);
                        VectorPathModel.UpdateLayerBounds(layer);
                    }
                    break;

                case NodeDragTarget.OutHandle:
                    if (_activeDragNode != null)
                    {
                        _activeDragNode.OutHandle = canvasPt;
                        _activeDragNode.HasOutHandle = true;
                        VectorPathModel.SynchronizeOppositeHandle(_activeDragNode, isDraggingInHandle: false);
                        VectorPathModel.UpdateLayerBounds(layer);
                    }
                    break;

                case NodeDragTarget.Segment:
                    if (layer.SubPaths != null && _dragSegmentSubPathIndex >= 0 && _dragSegmentSubPathIndex < layer.SubPaths.Count)
                    {
                        var subPath = layer.SubPaths[_dragSegmentSubPathIndex];
                        if (subPath.Nodes != null && _dragSegmentIndex >= 0 && _dragSegmentIndex < subPath.Nodes.Count)
                        {
                            var nA = subPath.Nodes[_dragSegmentIndex];
                            int nextIdx = (_dragSegmentIndex + 1) % subPath.Nodes.Count;
                            var nB = subPath.Nodes[nextIdx];

                            if (_nodeDragInitialOutHandles.TryGetValue(nA, out PointF origOutA))
                            {
                                nA.OutHandle = new PointF(origOutA.X + dx, origOutA.Y + dy);
                                nA.HasOutHandle = true;
                            }
                            if (_nodeDragInitialInHandles.TryGetValue(nB, out PointF origInB))
                            {
                                nB.InHandle = new PointF(origInB.X + dx, origInB.Y + dy);
                                nB.HasInHandle = true;
                            }
                            VectorPathModel.UpdateLayerBounds(layer);
                        }
                    }
                    break;

                case NodeDragTarget.Marquee:
                    _currentDragPt = canvasPt;
                    RectangleF marqueeBounds = NormalizeRect(_dragStartPt, _currentDragPt);
                    if (layer.SubPaths != null)
                    {
                        bool isShift = (modifiers & Keys.Shift) == Keys.Shift;
                        foreach (var sp in layer.SubPaths)
                        {
                            if (sp.Nodes == null) continue;
                            foreach (var n in sp.Nodes)
                            {
                                if (marqueeBounds.Contains(n.Point))
                                {
                                    n.IsSelected = true;
                                }
                                else if (!isShift)
                                {
                                    n.IsSelected = false;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void HandleNodeToolMouseUp(PointF canvasPt, int screenX, int screenY, MouseButtons button, int viewW, int viewH)
        {
            if (!_isNodeToolDragging) return;

            if (_pendingSingleSelectNode != null && Document.ActiveLayer != null && Document.ActiveLayer.SubPaths != null)
            {
                foreach (var sp in Document.ActiveLayer.SubPaths)
                {
                    if (sp.Nodes != null)
                    {
                        foreach (var n in sp.Nodes) n.IsSelected = (n == _pendingSingleSelectNode);
                    }
                }
                _pendingSingleSelectNode = null;
            }

            if (Document.ActiveLayer != null)
            {
                var layer = Document.ActiveLayer;
                VectorPathModel.UpdateLayerBounds(layer);
                if (layer.SubPaths != null && layer.SubPaths.Count > 0)
                {
                    var allPts = new List<PointF>();
                    foreach (var sp in layer.SubPaths)
                    {
                        if (sp.Nodes != null)
                        {
                            foreach (var n in sp.Nodes) allPts.Add(n.Point);
                        }
                    }
                    layer.PathPoints = allPts;
                }
            }

            _isNodeToolDragging = false;
            _nodeDragTarget = NodeDragTarget.None;
            _dragSegmentSubPathIndex = -1;
            _dragSegmentIndex = -1;
            _activeDragNode = null;
            _pendingSingleSelectNode = null;
            _nodeDragInitialPoints.Clear();
            _nodeDragInitialInHandles.Clear();
            _nodeDragInitialOutHandles.Clear();
        }
        #endregion

        private void DrawTopOptionsBar(D2DGraphics g, int width)
        {
            _topBarClickables.Clear();
            _brushSizeTrackRect = Rectangle.Empty;
            _radiusTrackRect = Rectangle.Empty;
            _fontSizeTrackRect = Rectangle.Empty;

            Rectangle topBarRect = new Rectangle(LeftToolbarWidth, RibbonHeight, width - LeftToolbarWidth - RightInspectorWidth, TopOptionsBarHeight);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen sep = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(bg, topBarRect);
                g.DrawLine(sep, topBarRect.X, topBarRect.Bottom, topBarRect.Right, topBarRect.Bottom);
            }

            int curX = LeftToolbarWidth + 14;
            using (Font lblFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (Font valFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush lblB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush valB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                if (SelectedTool == PhotoTool.Rectangle)
                {
                    var l = (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape) ? Document.ActiveLayer : null;
                    float curW = (l != null) ? l.Bounds.Width : 150f;
                    float curH = (l != null) ? l.Bounds.Height : 100f;
                    float curRx = (l != null) ? l.CornerRadius : ShapeCornerRadius;
                    float curRy = (l != null) ? l.CornerRadius : ShapeCornerRadius;
                    float curSw = (l != null) ? l.StrokeWidth : ShapeStrokeWidth;
                    Color curFill = (l != null) ? l.FillColor : ShapeFillColor;
                    Color curStroke = (l != null) ? l.StrokeColor : ShapeStrokeColor;

                    // 1. Nhãn chính chuẩn Inkscape: "Thay đổi:"
                    g.DrawString("Thay đổi:", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Thay đổi:", valFont).Width + 8;

                    // 2. Chiều rộng R: [154.800] - +
                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "R:", curW.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float nw = Math.Max(5f, curW - 5f);
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, nw, l.Bounds.Height); }
                    }, () => {
                        float nw = curW + 5f;
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, nw, l.Bounds.Height); }
                    }, () => {
                        float? v = ShowNumberInputDialog("Chiều rộng (R)", curW);
                        if (v.HasValue && v.Value > 0 && l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, v.Value, l.Bounds.Height); }
                    }, 82);

                    // 3. Chiều cao C: [162.000] - +
                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "C:", curH.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float nh = Math.Max(5f, curH - 5f);
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, nh); }
                    }, () => {
                        float nh = curH + 5f;
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, nh); }
                    }, () => {
                        float? v = ShowNumberInputDialog("Chiều cao (C)", curH);
                        if (v.HasValue && v.Value > 0 && l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, v.Value); }
                    }, 82);

                    // 4. Bán kính bo góc ngang Rx: [0.000] - +
                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Rx:", curRx.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float maxR = (l != null) ? Math.Min(l.Bounds.Width, l.Bounds.Height) / 2f : 100f;
                        float nextR = Math.Max(0f, Math.Min(maxR, curRx - 2f));
                        ShapeCornerRadius = nextR;
                        if (l != null) {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = nextR;
                            l.ShapeType = (nextR > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
                        }
                    }, () => {
                        float maxR = (l != null) ? Math.Min(l.Bounds.Width, l.Bounds.Height) / 2f : 100f;
                        float nextR = Math.Max(0f, Math.Min(maxR, curRx + 2f));
                        ShapeCornerRadius = nextR;
                        if (l != null) {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = nextR;
                            l.ShapeType = (nextR > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
                        }
                    }, () => {
                        float? v = ShowNumberInputDialog("Bán kính góc ngang (Rx)", curRx);
                        if (v.HasValue) {
                            float maxR = (l != null) ? Math.Min(l.Bounds.Width, l.Bounds.Height) / 2f : 100f;
                            float nextR = Math.Max(0f, Math.Min(maxR, v.Value));
                            ShapeCornerRadius = nextR;
                            if (l != null) {
                                Document.SaveUndoSnapshot();
                                l.CornerRadius = nextR;
                                l.ShapeType = (nextR > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
                            }
                        }
                    }, 82);

                    // 5. Bán kính bo góc dọc Ry: [35.400] - +
                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Ry:", curRy.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float maxR = (l != null) ? Math.Min(l.Bounds.Width, l.Bounds.Height) / 2f : 100f;
                        float nextR = Math.Max(0f, Math.Min(maxR, curRy - 2f));
                        ShapeCornerRadius = nextR;
                        if (l != null) {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = nextR;
                            l.ShapeType = (nextR > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
                        }
                    }, () => {
                        float maxR = (l != null) ? Math.Min(l.Bounds.Width, l.Bounds.Height) / 2f : 100f;
                        float nextR = Math.Max(0f, Math.Min(maxR, curRy + 2f));
                        ShapeCornerRadius = nextR;
                        if (l != null) {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = nextR;
                            l.ShapeType = (nextR > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
                        }
                    }, () => {
                        float? v = ShowNumberInputDialog("Bán kính góc dọc (Ry)", curRy);
                        if (v.HasValue) {
                            float maxR = (l != null) ? Math.Min(l.Bounds.Width, l.Bounds.Height) / 2f : 100f;
                            float nextR = Math.Max(0f, Math.Min(maxR, v.Value));
                            ShapeCornerRadius = nextR;
                            if (l != null) {
                                Document.SaveUndoSnapshot();
                                l.CornerRadius = nextR;
                                l.ShapeType = (nextR > 0) ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle;
                            }
                        }
                    }, 82);

                    // 6. Đơn vị px
                    g.DrawString("px", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("px", lblFont).Width + 8;

                    // 7. Nút icon ⌐ (Làm các góc thành góc nhọn 90° - Chuẩn Inkscape ảnh 2)
                    Rectangle sharpBtn = new Rectangle(curX, RibbonHeight + 9, 30, 26);
                    _topBarClickables.Add((sharpBtn, () => {
                        ShapeCornerRadius = 0f;
                        if (l != null)
                        {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = 0f;
                            l.ShapeType = VectorShapeType.Rectangle;
                        }
                    }));
                    bool isSharp = (curRx == 0f);
                    using (SolidBrush bBg = new SolidBrush(isSharp ? Color.FromArgb(238, 242, 255) : Color.FromArgb(241, 245, 249)))
                    using (Pen bBorder = new Pen(isSharp ? Color.FromArgb(99, 102, 241) : Color.FromArgb(203, 213, 225), 1f))
                    using (Pen glyphPen = new Pen(isSharp ? Color.FromArgb(79, 70, 229) : Color.FromArgb(51, 65, 85), 2f))
                    {
                        g.FillRoundedRectangle(bBg, sharpBtn, 4f);
                        g.DrawRoundedRectangle(bBorder, sharpBtn, 4f);
                        // Vẽ góc nhọn ⌐
                        int gx = sharpBtn.X + 9;
                        int gy = sharpBtn.Y + 8;
                        g.DrawLine(glyphPen, gx, gy, gx + 11, gy);
                        g.DrawLine(glyphPen, gx + 11, gy, gx + 11, gy + 11);
                    }
                    curX = sharpBtn.Right + 12;

                    // 8. Độ dày viền: [2.0] - +
                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Độ dày:", curSw.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float nsw = Math.Max(0.5f, curSw - 0.5f);
                        SetStrokeWidth(nsw);
                    }, () => {
                        float nsw = Math.Min(50f, curSw + 0.5f);
                        SetStrokeWidth(nsw);
                    }, () => {
                        float? v = ShowNumberInputDialog("Độ dày viền", curSw);
                        if (v.HasValue && v.Value > 0) SetStrokeWidth(v.Value);
                    }, 82);

                    // 9. Màu Tô
                    g.DrawString("Tô:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Tô:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, curFill, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curFill))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetFillColor(dlg.SelectedColor);
                        }
                    });
                    curX += 34;

                    // 10. Màu Nét
                    g.DrawString("Nét:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Nét:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, curStroke, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curStroke))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetStrokeColor(dlg.SelectedColor);
                        }
                    });
                    curX += 34;
                }
                else if (SelectedTool == PhotoTool.Move)
                {
                    var l = Document.ActiveLayer;
                    float curX_l = (l != null) ? l.Bounds.X : 0f;
                    float curY_l = (l != null) ? l.Bounds.Y : 0f;
                    float curW = (l != null) ? l.Bounds.Width : 0f;
                    float curH = (l != null) ? l.Bounds.Height : 0f;
                    float curRot = (l != null) ? l.Rotation : 0f;
                    Color curFill = (l != null && l.Type == LayerType.VectorShape) ? l.FillColor : ShapeFillColor;
                    Color curStroke = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeColor : ShapeStrokeColor;

                    g.DrawString("Đối tượng:", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Đối tượng:", valFont).Width + 8;

                    if (l != null)
                    {
                        curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "X:", curX_l.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(curX_l - 5f, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height);
                        }, () => {
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(curX_l + 5f, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height);
                        }, () => {
                            float? v = ShowNumberInputDialog("Tọa độ X", curX_l);
                            if (v.HasValue) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(v.Value, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height); }
                        }, 80);

                        curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Y:", curY_l.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, curY_l - 5f, l.Bounds.Width, l.Bounds.Height);
                        }, () => {
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, curY_l + 5f, l.Bounds.Width, l.Bounds.Height);
                        }, () => {
                            float? v = ShowNumberInputDialog("Tọa độ Y", curY_l);
                            if (v.HasValue) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, v.Value, l.Bounds.Width, l.Bounds.Height); }
                        }, 80);

                        curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "R:", curW.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                            float nw = Math.Max(5f, curW - 5f);
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, nw, l.Bounds.Height);
                        }, () => {
                            float nw = curW + 5f;
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, nw, l.Bounds.Height);
                        }, () => {
                            float? v = ShowNumberInputDialog("Chiều rộng (R)", curW);
                            if (v.HasValue && v.Value > 0) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, v.Value, l.Bounds.Height); }
                        }, 80);

                        curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "C:", curH.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                            float nh = Math.Max(5f, curH - 5f);
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, nh);
                        }, () => {
                            float nh = curH + 5f;
                            Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, nh);
                        }, () => {
                            float? v = ShowNumberInputDialog("Chiều cao (C)", curH);
                            if (v.HasValue && v.Value > 0) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, v.Value); }
                        }, 80);

                        g.DrawString("px", lblFont, lblB, curX, RibbonHeight + 13);
                        curX += (int)g.MeasureString("px", lblFont).Width + 8;

                        curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Góc:", $"{curRot:0}°", () => {
                            Document.SaveUndoSnapshot(); l.Rotation = (l.Rotation - 5f + 360f) % 360f;
                        }, () => {
                            Document.SaveUndoSnapshot(); l.Rotation = (l.Rotation + 5f) % 360f;
                        }, null, 56);

                        DrawTopBarButton(g, curX, RibbonHeight + 10, 52, 24, "↶ -90°", () => {
                            Document.SaveUndoSnapshot(); l.Rotation = (l.Rotation - 90f + 360f) % 360f;
                        });
                        curX += 56;

                        DrawTopBarButton(g, curX, RibbonHeight + 10, 52, 24, "↷ +90°", () => {
                            Document.SaveUndoSnapshot(); l.Rotation = (l.Rotation + 90f) % 360f;
                        });
                        curX += 56;

                        DrawTopBarButton(g, curX, RibbonHeight + 10, 48, 24, "⇈ Lên", () => {
                            Document.MoveLayerUp();
                        });
                        curX += 52;

                        DrawTopBarButton(g, curX, RibbonHeight + 10, 56, 24, "⇊ Xuống", () => {
                            Document.MoveLayerDown();
                        });
                        curX += 60;

                        if (l.Type == LayerType.VectorShape)
                        {
                            g.DrawString("Tô:", lblFont, lblB, curX, RibbonHeight + 13);
                            curX += (int)g.MeasureString("Tô:", lblFont).Width + 5;
                            DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 24, 24, curFill, () => {
                                using (var dlg = new PhotoColorPickerDialogForm(curFill))
                                {
                                    if (dlg.ShowDialog() == DialogResult.OK) SetFillColor(dlg.SelectedColor);
                                }
                            });
                            curX += 32;

                            g.DrawString("Nét:", lblFont, lblB, curX, RibbonHeight + 13);
                            curX += (int)g.MeasureString("Nét:", lblFont).Width + 5;
                            DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 24, 24, curStroke, () => {
                                using (var dlg = new PhotoColorPickerDialogForm(curStroke))
                                {
                                    if (dlg.ShowDialog() == DialogResult.OK) SetStrokeColor(dlg.SelectedColor);
                                }
                            });
                            curX += 32;
                        }
                    }
                    else
                    {
                        g.DrawString("Chưa chọn đối tượng nào", lblFont, lblB, curX, RibbonHeight + 13);
                        curX += 160;
                    }
                }
                else if (SelectedTool == PhotoTool.Star)
                {
                    var l = (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape && (Document.ActiveLayer.ShapeType == VectorShapeType.Star || Document.ActiveLayer.ShapeType == VectorShapeType.Polygon)) ? Document.ActiveLayer : null;
                    bool isStar = (l != null) ? (l.ShapeType == VectorShapeType.Star) : (ActiveShapeType == VectorShapeType.Star);
                    int curPts = (l != null) ? l.ShapePointCount : ActiveShapePointCount;
                    float curSw = (l != null) ? l.StrokeWidth : ShapeStrokeWidth;
                    Color curFill = (l != null) ? l.FillColor : ShapeFillColor;
                    Color curStroke = (l != null) ? l.StrokeColor : ShapeStrokeColor;

                    DrawTopBarButton(g, curX, RibbonHeight + 10, 72, 24, "★ Ngôi sao", () => {
                        ActiveShapeType = VectorShapeType.Star;
                        if (l != null) { Document.SaveUndoSnapshot(); l.ShapeType = VectorShapeType.Star; }
                    }, isStar);
                    curX += 76;

                    DrawTopBarButton(g, curX, RibbonHeight + 10, 68, 24, "⬡ Đa giác", () => {
                        ActiveShapeType = VectorShapeType.Polygon;
                        if (l != null) { Document.SaveUndoSnapshot(); l.ShapeType = VectorShapeType.Polygon; }
                    }, !isStar);
                    curX += 74;

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Số đỉnh:", $"{curPts}", () => {
                        int nextP = Math.Max(3, curPts - 1);
                        ActiveShapePointCount = nextP;
                        if (l != null) { Document.SaveUndoSnapshot(); l.ShapePointCount = nextP; }
                    }, () => {
                        int nextP = Math.Min(50, curPts + 1);
                        ActiveShapePointCount = nextP;
                        if (l != null) { Document.SaveUndoSnapshot(); l.ShapePointCount = nextP; }
                    }, () => {
                        float? v = ShowNumberInputDialog("Số đỉnh", curPts);
                        if (v.HasValue && v.Value >= 3) {
                            int np = (int)Math.Max(3, Math.Min(50, v.Value));
                            ActiveShapePointCount = np;
                            if (l != null) { Document.SaveUndoSnapshot(); l.ShapePointCount = np; }
                        }
                    }, 58);

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Độ dày:", curSw.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        SetStrokeWidth(Math.Max(0.5f, curSw - 0.5f));
                    }, () => {
                        SetStrokeWidth(Math.Min(50f, curSw + 0.5f));
                    }, null, 58);

                    g.DrawString("Tô:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Tô:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, curFill, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curFill))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetFillColor(dlg.SelectedColor);
                        }
                    });
                    curX += 34;

                    g.DrawString("Nét:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Nét:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, curStroke, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curStroke))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetStrokeColor(dlg.SelectedColor);
                        }
                    });
                    curX += 34;
                }
                else if (SelectedTool == PhotoTool.Ellipse)
                {
                    var l = (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape && Document.ActiveLayer.ShapeType == VectorShapeType.Ellipse) ? Document.ActiveLayer : null;
                    float curW = (l != null) ? l.Bounds.Width : 120f;
                    float curH = (l != null) ? l.Bounds.Height : 120f;
                    float curSw = (l != null) ? l.StrokeWidth : ShapeStrokeWidth;
                    Color curFill = (l != null) ? l.FillColor : ShapeFillColor;
                    Color curStroke = (l != null) ? l.StrokeColor : ShapeStrokeColor;

                    g.DrawString("Hình elip:", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Hình elip:", valFont).Width + 8;

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "R:", curW.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float nw = Math.Max(5f, curW - 5f);
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, nw, l.Bounds.Height); }
                    }, () => {
                        float nw = curW + 5f;
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, nw, l.Bounds.Height); }
                    }, null, 82);

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "C:", curH.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        float nh = Math.Max(5f, curH - 5f);
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, nh); }
                    }, () => {
                        float nh = curH + 5f;
                        if (l != null) { Document.SaveUndoSnapshot(); l.Bounds = new RectangleF(l.Bounds.X, l.Bounds.Y, l.Bounds.Width, nh); }
                    }, null, 82);

                    g.DrawString("px", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("px", lblFont).Width + 8;

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Độ dày:", curSw.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), () => {
                        SetStrokeWidth(Math.Max(0.5f, curSw - 0.5f));
                    }, () => {
                        SetStrokeWidth(Math.Min(50f, curSw + 0.5f));
                    }, null, 80);

                    g.DrawString("Tô:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Tô:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, curFill, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curFill))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetFillColor(dlg.SelectedColor);
                        }
                    });
                    curX += 34;

                    g.DrawString("Nét:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Nét:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, curStroke, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curStroke))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetStrokeColor(dlg.SelectedColor);
                        }
                    });
                    curX += 34;
                }
                else if (SelectedTool == PhotoTool.Brush || SelectedTool == PhotoTool.Pen)
                {
                    string title = (SelectedTool == PhotoTool.Pen) ? "Bút vẽ Bezier" : "Bút vẽ tự do";
                    g.DrawString(title, valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString(title, valFont).Width + 14;

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Cỡ nét:", $"{(int)BrushSize} px", () => {
                        BrushSize = Math.Max(1f, BrushSize - 1f);
                        if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape) Document.ActiveLayer.StrokeWidth = BrushSize;
                    }, () => {
                        BrushSize = Math.Min(150f, BrushSize + 1f);
                        if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape) Document.ActiveLayer.StrokeWidth = BrushSize;
                    }, null, 86);

                    // Thanh kéo pixel tự do
                    _brushSizeTrackRect = new Rectangle(curX, RibbonHeight + 19, 90, 6);
                    DrawD2DSlider(g, _brushSizeTrackRect, 1f, 150f, BrushSize);
                    curX += 100;

                    // Phím chọn nhanh pixel
                    int[] presets = new int[] { 1, 3, 5, 10, 20, 40 };
                    foreach (var pv in presets)
                    {
                        int pVal = pv;
                        bool isCur = ((int)BrushSize == pVal);
                        DrawTopBarButton(g, curX, RibbonHeight + 10, 42, 24, $"{pVal}px", () => {
                            BrushSize = pVal;
                            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape) Document.ActiveLayer.StrokeWidth = BrushSize;
                        }, isCur);
                        curX += 46;
                    }
                    curX += 6;

                    // Màu nét vẽ
                    g.DrawString("Màu nét:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Màu nét:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, ForegroundColor, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(ForegroundColor))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK)
                            {
                                ForegroundColor = dlg.SelectedColor;
                                if (Document.ActiveLayer != null)
                                {
                                    Document.SaveUndoSnapshot();
                                    if (Document.ActiveLayer.Type == LayerType.VectorShape) Document.ActiveLayer.StrokeColor = dlg.SelectedColor;
                                }
                            }
                        }
                    });
                    curX += 34;
                }
                else if (SelectedTool == PhotoTool.NodeTool)
                {
                    g.DrawString("Neo:", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Neo:", valFont).Width + 8;

                    var activeL = Document.ActiveLayer;
                    bool hasLayer = (activeL != null && activeL.Type == LayerType.VectorShape);
                    if (hasLayer) activeL.EnsureNodesInitialized();

                    var selNodes = new List<VectorPathNode>();
                    if (hasLayer && activeL.SubPaths != null)
                    {
                        foreach (var sp in activeL.SubPaths)
                        {
                            if (sp.Nodes != null)
                            {
                                selNodes.AddRange(sp.Nodes.FindAll(n => n.IsSelected));
                            }
                        }
                    }
                    int selCount = selNodes.Count;

                    // Nhóm 1: Thao tác neo (6 nút biểu tượng 28x26 chuẩn Inkscape)
                    // Nhóm 1: Thao tác neo (6 nút chuẩn Inkscape với nhãn rõ ràng)
                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "+ Neo", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.InsertNodeAtMidpoint(Document.ActiveLayer);
                        }
                    }, false, 46);
                    curX += 50;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "− Neo", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.DeleteSelectedNodes(Document.ActiveLayer);
                        }
                    }, false, 46);
                    curX += 50;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "⧉ Nối", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.JoinSelectedNodes(Document.ActiveLayer);
                        }
                    }, false, 46);
                    curX += 50;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "✂ Tách", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.BreakPathAtSelectedNodes(Document.ActiveLayer);
                        }
                    }, false, 48);
                    curX += 52;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "⌢ Nối đầu", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.ConnectEndpoints(Document.ActiveLayer);
                        }
                    }, false, 60);
                    curX += 64;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "✕ Xóa đoạn", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.DeleteSelectedSegment(Document.ActiveLayer);
                        }
                    }, false, 68);
                    curX += 72;

                    DrawTopBarSeparator(g, ref curX);

                    // Nhóm 2: Kiểu điểm neo (4 kiểu chuẩn Inkscape)
                    bool isCornerActive = selCount > 0 && selNodes.TrueForAll(n => n.NodeType == VectorNodeType.Corner);
                    bool isSmoothActive = selCount > 0 && selNodes.TrueForAll(n => n.NodeType == VectorNodeType.Smooth);
                    bool isSymmActive = selCount > 0 && selNodes.TrueForAll(n => n.NodeType == VectorNodeType.Symmetric);
                    bool isAutoActive = selCount > 0 && selNodes.TrueForAll(n => n.NodeType == VectorNodeType.AutoSmooth);

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "⌟ Góc", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.SetSelectedNodesType(Document.ActiveLayer, VectorNodeType.Corner);
                        }
                    }, isCornerActive, 46);
                    curX += 50;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "〜 Mượt", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.SetSelectedNodesType(Document.ActiveLayer, VectorNodeType.Smooth);
                        }
                    }, isSmoothActive, 52);
                    curX += 56;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "⚬ Đ.xứng", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.SetSelectedNodesType(Document.ActiveLayer, VectorNodeType.Symmetric);
                        }
                    }, isSymmActive, 56);
                    curX += 60;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "✦ Tự uốn", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.SetSelectedNodesType(Document.ActiveLayer, VectorNodeType.AutoSmooth);
                        }
                    }, isAutoActive, 56);
                    curX += 60;

                    DrawTopBarSeparator(g, ref curX);

                    // Nhóm 3: Đoạn vẽ (Thẳng / Cong)
                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "╱ Thẳng", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.ConvertSegmentToLine(Document.ActiveLayer);
                        }
                    }, false, 52);
                    curX += 56;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "⌒ Cong", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.ConvertSegmentToCurve(Document.ActiveLayer);
                        }
                    }, false, 50);
                    curX += 54;

                    DrawTopBarSeparator(g, ref curX);

                    // Nhóm 4: Chuyển đổi đối tượng (Object to Path / Stroke to Path)
                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "⎔ Ra Path", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.ObjectToPath(Document.ActiveLayer);
                        }
                    }, false, 60);
                    curX += 64;

                    DrawTopBarNodeIconButton(g, curX, RibbonHeight + 8, "║ Viền Path", () => {
                        if (Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.StrokeToPath(Document.ActiveLayer);
                        }
                    }, false, 66);
                    curX += 70;

                    DrawTopBarSeparator(g, ref curX);

                    // Nhóm 5: Tọa độ & Đơn vị đo
                    float avgX = 0f, avgY = 0f;
                    if (selCount > 0) {
                        foreach (var sn in selNodes) { avgX += sn.Point.X; avgY += sn.Point.Y; }
                        avgX /= selCount;
                        avgY /= selCount;
                    }
                    float displayX = _nodeUnitIsMm ? (avgX / PxPerMm) : avgX;
                    float displayY = _nodeUnitIsMm ? (avgY / PxPerMm) : avgY;
                    string strX = (selCount > 0) ? displayX.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "--";
                    string strY = (selCount > 0) ? displayY.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "--";
                    float stepAmount = _nodeUnitIsMm ? PxPerMm : 1.0f;
                    string unitLabel = _nodeUnitIsMm ? "mm" : "px";

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "X:", strX, () => {
                        if (selCount > 0 && Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.MoveSelectedNodes(Document.ActiveLayer, -stepAmount, 0f);
                        }
                    }, () => {
                        if (selCount > 0 && Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.MoveSelectedNodes(Document.ActiveLayer, stepAmount, 0f);
                        }
                    }, () => {
                        if (selCount > 0 && Document.ActiveLayer != null) {
                            float? v = ShowNumberInputDialog($"Nhập tọa độ X ({unitLabel})", displayX);
                            if (v.HasValue) {
                                float targetX = _nodeUnitIsMm ? (v.Value * PxPerMm) : v.Value;
                                float deltaX = targetX - avgX;
                                Document.SaveUndoSnapshot();
                                VectorPathModel.MoveSelectedNodes(Document.ActiveLayer, deltaX, 0f);
                            }
                        }
                    }, 82);

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Y:", strY, () => {
                        if (selCount > 0 && Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.MoveSelectedNodes(Document.ActiveLayer, 0f, -stepAmount);
                        }
                    }, () => {
                        if (selCount > 0 && Document.ActiveLayer != null) {
                            Document.SaveUndoSnapshot();
                            VectorPathModel.MoveSelectedNodes(Document.ActiveLayer, 0f, stepAmount);
                        }
                    }, () => {
                        if (selCount > 0 && Document.ActiveLayer != null) {
                            float? v = ShowNumberInputDialog($"Nhập tọa độ Y ({unitLabel})", displayY);
                            if (v.HasValue) {
                                float targetY = _nodeUnitIsMm ? (v.Value * PxPerMm) : v.Value;
                                float deltaY = targetY - avgY;
                                Document.SaveUndoSnapshot();
                                VectorPathModel.MoveSelectedNodes(Document.ActiveLayer, 0f, deltaY);
                            }
                        }
                    }, 82);

                    string unitBtnText = _nodeUnitIsMm ? "mm" : "px";
                    DrawTopBarButton(g, curX, RibbonHeight + 9, 38, 26, unitBtnText, () => {
                        _nodeUnitIsMm = !_nodeUnitIsMm;
                    }, _nodeUnitIsMm);
                    curX += 44;
                }
                else if (SelectedTool == PhotoTool.Text)
                {
                    g.DrawString("Văn bản", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Văn bản", valFont).Width + 14;

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Cỡ chữ:", $"{(int)ActiveFontSize}", () => {
                        ActiveFontSize = Math.Max(8f, ActiveFontSize - 2f);
                        if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text) Document.ActiveLayer.FontSize = ActiveFontSize;
                    }, () => {
                        ActiveFontSize = Math.Min(150f, ActiveFontSize + 2f);
                        if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text) Document.ActiveLayer.FontSize = ActiveFontSize;
                    }, null, 58);

                    _fontSizeTrackRect = new Rectangle(curX, RibbonHeight + 19, 90, 6);
                    DrawD2DSlider(g, _fontSizeTrackRect, 8f, 120f, ActiveFontSize);
                    curX += 100;

                    g.DrawString("Màu chữ:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Màu chữ:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, ForegroundColor, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(ForegroundColor)) {
                            if (dlg.ShowDialog() == DialogResult.OK) {
                                ForegroundColor = dlg.SelectedColor;
                                if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text) Document.ActiveLayer.TextColor = dlg.SelectedColor;
                            }
                        }
                    });
                    curX += 34;
                }
                else if (SelectedTool == PhotoTool.Eraser)
                {
                    g.DrawString("Tẩy xóa", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Tẩy xóa", valFont).Width + 14;

                    curX = DrawTopBarValueBox(g, curX, RibbonHeight + 9, "Cỡ tẩy:", $"{(int)BrushSize} px", () => {
                        BrushSize = Math.Max(1f, BrushSize - 1f);
                    }, () => {
                        BrushSize = Math.Min(150f, BrushSize + 1f);
                    }, null, 86);

                    _brushSizeTrackRect = new Rectangle(curX, RibbonHeight + 19, 90, 6);
                    DrawD2DSlider(g, _brushSizeTrackRect, 1f, 150f, BrushSize);
                    curX += 100;
                }
                else if (SelectedTool == PhotoTool.Bucket)
                {
                    g.DrawString("Đổ màu", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Đổ màu", valFont).Width + 14;

                    g.DrawString("Màu đổ:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Màu đổ:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, ForegroundColor, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(ForegroundColor)) {
                            if (dlg.ShowDialog() == DialogResult.OK) ForegroundColor = dlg.SelectedColor;
                        }
                    });
                    curX += 34;
                }
                else if (SelectedTool == PhotoTool.Gradient)
                {
                    g.DrawString("Dải màu", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Dải màu", valFont).Width + 14;

                    DrawTopBarButton(g, curX, RibbonHeight + 10, 84, 24, "Tuyến tính", () => SetFillType(VectorFillType.LinearGradient), ShapeFillType == VectorFillType.LinearGradient);
                    curX += 90;
                    DrawTopBarButton(g, curX, RibbonHeight + 10, 76, 24, "Tỏa tròn", () => SetFillType(VectorFillType.RadialGradient), ShapeFillType == VectorFillType.RadialGradient);
                    curX += 82;
                    DrawTopBarButton(g, curX, RibbonHeight + 10, 140, 24, "Chỉnh Dải Màu & Alpha...", () => OpenGradientEditor(), false);
                    curX += 146;
                }
                else if (SelectedTool == PhotoTool.Eyedropper)
                {
                    g.DrawString("Hút màu", valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString("Hút màu", valFont).Width + 14;

                    g.DrawString("Màu mẫu:", lblFont, lblB, curX, RibbonHeight + 13);
                    curX += (int)g.MeasureString("Màu mẫu:", lblFont).Width + 5;
                    DrawTopBarColorSwatch(g, curX, RibbonHeight + 10, 26, 24, ForegroundColor, null);
                    curX += 34;
                }
                else
                {
                    string generic = $"Công cụ: {SelectedTool}";
                    g.DrawString(generic, valFont, valB, curX, RibbonHeight + 12);
                    curX += (int)g.MeasureString(generic, valFont).Width + 14;
                }

                // Quick Zoom Controls (Right aligned, safely padded)
                int zoomX = width - RightInspectorWidth - 130;
                if (zoomX > curX + 10)
                {
                    string zoomPct = $"{(int)(Zoom * 100)}%";
                    g.DrawString(zoomPct, valFont, valB, zoomX, RibbonHeight + 12);

                    DrawTopBarGlyphButton(g, zoomX + 45, RibbonHeight + 8, 26, 26, '', () => {
                        Zoom = Math.Max(0.1f, Zoom * 0.8f);
                    });
                    DrawTopBarGlyphButton(g, zoomX + 75, RibbonHeight + 8, 26, 26, '', () => {
                        Zoom = Math.Min(10f, Zoom * 1.25f);
                    });
                }
            }
        }


        private void DrawLeftToolbar(D2DGraphics g, int height)
        {
            Rectangle tbRect = new Rectangle(0, RibbonHeight, LeftToolbarWidth, height - RibbonHeight);
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen sep = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(bg, tbRect);
                g.DrawLine(sep, tbRect.Right, tbRect.Y, tbRect.Right, tbRect.Bottom);
            }

            PhotoTool[] tools = new PhotoTool[]
            {
                PhotoTool.Move,
                PhotoTool.NodeTool,
                PhotoTool.Rectangle,
                PhotoTool.Ellipse,
                PhotoTool.Star,
                PhotoTool.Pen,
                PhotoTool.Brush,
                PhotoTool.Eraser,
                PhotoTool.Text,
                PhotoTool.Bucket,
                PhotoTool.Eyedropper,
                PhotoTool.Gradient,
                PhotoTool.Hand
            };

            int startY = RibbonHeight + 12;
            for (int i = 0; i < tools.Length; i++)
            {
                PhotoTool t = tools[i];
                bool isSelected = (SelectedTool == t);
                Rectangle toolBtnRect = new Rectangle(6, startY + i * 40, 40, 36);

                if (isSelected)
                {
                    using (SolidBrush activeB = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    using (Pen activeP = new Pen(Color.FromArgb(99, 102, 241), 1.5f))
                    {
                        g.FillRoundedRectangle(activeB, toolBtnRect, 8f);
                        g.DrawRoundedRectangle(activeP, toolBtnRect, 8f);
                    }
                }
                else if (_hoverToolIndex == i)
                {
                    using (SolidBrush hoverB = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillRoundedRectangle(hoverB, toolBtnRect, 8f);
                    }
                }

                Color iconCol = isSelected ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139);
                DrawToolGlyph(g, t, toolBtnRect, iconCol);
            }

            // Foreground / Background Swatches at bottom
            int swY = height - 85;
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
            {
                using (SolidBrush bgB = new SolidBrush(BackgroundColor))
                {
                    g.FillRectangle(bgB, 18, swY + 12, 22, 22);
                    g.DrawRectangle(borderPen, 18, swY + 12, 22, 22);
                }

                using (SolidBrush fgB = new SolidBrush(ForegroundColor))
                {
                    g.FillRectangle(fgB, 10, swY + 4, 22, 22);
                    g.DrawRectangle(borderPen, 10, swY + 4, 22, 22);
                }
            }
        }

        private void DrawToolGlyph(D2DGraphics g, PhotoTool tool, Rectangle r, Color col)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            using (Pen p = new Pen(col, 1.6f))
            using (SolidBrush b = new SolidBrush(col))
            using (SolidBrush whiteB = new SolidBrush(Color.White))
            {
                p.LineJoin = LineJoin.Round;
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;

                switch (tool)
                {
                    case PhotoTool.Move:
                        PointF[] arrow = new PointF[]
                        {
                            new PointF(cx - 7, cy - 8),
                            new PointF(cx - 7, cy + 7),
                            new PointF(cx - 3, cy + 3),
                            new PointF(cx + 2, cy + 9),
                            new PointF(cx + 5, cy + 7),
                            new PointF(cx, cy + 2),
                            new PointF(cx + 6, cy + 2)
                        };
                        g.FillPolygon(b, arrow);
                        break;

                    case PhotoTool.NodeTool:
                        using (Pen hLine = new Pen(col, 1.2f))
                        {
                            g.DrawLine(hLine, cx - 6, cy + 4, cx + 5, cy - 6);
                        }
                        g.FillEllipse(b, cx + 4, cy - 7, 4, 4);
                        g.FillRectangle(whiteB, cx - 8, cy + 2, 6, 6);
                        g.DrawRectangle(p, cx - 8, cy + 2, 6, 6);
                        PointF[] nodeArrow = new PointF[]
                        {
                            new PointF(cx + 1, cy - 1),
                            new PointF(cx + 1, cy + 8),
                            new PointF(cx + 4, cy + 5),
                            new PointF(cx + 7, cy + 5)
                        };
                        g.FillPolygon(b, nodeArrow);
                        break;

                    case PhotoTool.Rectangle:
                        Rectangle rectBox = new Rectangle(cx - 8, cy - 6, 16, 12);
                        g.DrawRectangle(p, rectBox);
                        break;

                    case PhotoTool.Ellipse:
                        Rectangle elBox = new Rectangle(cx - 7, cy - 7, 14, 14);
                        g.DrawEllipse(p, elBox);
                        break;

                    case PhotoTool.Star:
                        PointF[] starPts = new PointF[10];
                        for (int i = 0; i < 10; i++)
                        {
                            double a = -Math.PI / 2 + i * Math.PI / 5;
                            float rad = (i % 2 == 0) ? 8f : 3.8f;
                            starPts[i] = new PointF((float)(cx + rad * Math.Cos(a)), (float)(cy + rad * Math.Sin(a)));
                        }
                        g.DrawPolygon(p, starPts);
                        break;

                    case PhotoTool.Pen:
                        PointF[] nib = new PointF[]
                        {
                            new PointF(cx - 4, cy - 6),
                            new PointF(cx + 4, cy - 6),
                            new PointF(cx + 6, cy),
                            new PointF(cx, cy + 7),
                            new PointF(cx - 6, cy)
                        };
                        g.FillPolygon(b, nib);
                        using (Pen nibSplit = new Pen(Color.White, 1.2f))
                        {
                            g.DrawLine(nibSplit, cx, cy - 3, cx, cy + 5);
                        }
                        break;

                    case PhotoTool.Brush:
                        PointF[] brushPts = new PointF[]
                        {
                            new PointF(cx - 5, cy + 6),
                            new PointF(cx + 5, cy - 6),
                            new PointF(cx + 7, cy - 4),
                            new PointF(cx - 3, cy + 8)
                        };
                        g.FillPolygon(b, brushPts);
                        using (Pen strokePen = new Pen(col, 2f))
                        {
                            g.DrawArc(strokePen, cx - 7, cy + 3, 10, 6, 0, 180);
                        }
                        break;

                    case PhotoTool.Eraser:
                        PointF[] eraserBody = new PointF[]
                        {
                            new PointF(cx - 6, cy - 2),
                            new PointF(cx + 2, cy - 8),
                            new PointF(cx + 7, cy - 3),
                            new PointF(cx - 1, cy + 5)
                        };
                        g.DrawPolygon(p, eraserBody);
                        using (Pen eraseLine = new Pen(col, 1.2f))
                        {
                            g.DrawLine(eraseLine, cx - 2, cy, cx + 3, cy - 5);
                        }
                        break;

                    case PhotoTool.Text:
                        using (Font af = new Font("Segoe UI", 12f, FontStyle.Bold))
                        {
                            var sz = g.MeasureString("A", af);
                            g.DrawString("A", af, b, cx - sz.Width / 2f + 1, cy - sz.Height / 2f - 1);
                        }
                        break;

                    case PhotoTool.Bucket:
                        PointF[] bkt = new PointF[]
                        {
                            new PointF(cx - 5, cy - 3),
                            new PointF(cx + 3, cy - 7),
                            new PointF(cx + 7, cy + 1),
                            new PointF(cx - 1, cy + 5)
                        };
                        g.DrawPolygon(p, bkt);
                        g.FillEllipse(b, cx + 4, cy + 4, 3, 4);
                        break;

                    case PhotoTool.Eyedropper:
                        using (Pen dropPen = new Pen(col, 1.6f))
                        {
                            g.DrawLine(dropPen, cx + 5, cy - 6, cx - 3, cy + 3);
                            g.DrawLine(dropPen, cx - 3, cy + 3, cx - 6, cy + 7);
                            g.DrawEllipse(dropPen, cx + 2, cy - 7, 5, 5);
                        }
                        break;

                    case PhotoTool.Gradient:
                        Rectangle gSquare = new Rectangle(cx - 6, cy - 6, 13, 13);
                        g.DrawRectangle(p, gSquare);
                        using (LinearGradientBrush lgb = new LinearGradientBrush(gSquare, Color.Transparent, col, 45f))
                        {
                            g.FillRectangle(lgb, new Rectangle(cx - 5, cy - 5, 11, 11));
                        }
                        break;

                    case PhotoTool.Hand:
                        FluentIconRenderer.DrawGlyphD2D(g, '\uE927', new Rectangle(cx - 7, cy - 7, 14, 14), col, 14f);
                        break;

                    default:
                        Rectangle defRect = new Rectangle(cx - 6, cy - 6, 12, 12);
                        g.DrawRectangle(p, defRect);
                        break;
                }
            }
        }

        
        #region Right Inspector - Fill & Stroke & Layers (Inkscape Standard)
        private void DrawRightInspector(D2DGraphics g, int width, int height)
        {
            _inspectorClickables.Clear();
            Rectangle inspRect = new Rectangle(width - RightInspectorWidth, RibbonHeight, RightInspectorWidth, height - RibbonHeight);
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen sep = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(bg, inspRect);
                g.DrawLine(sep, inspRect.X, inspRect.Y, inspRect.X, inspRect.Bottom);
            }

            // 3 Tabs Thống Nhất Chuẩn Thiết Kế Đồ Hoạ: Lớp, Tô & Nét, Thuộc tính
            string[] tabs = new string[] { "Lớp", "Tô & Nét", "Thuộc tính" };
            int tabW = RightInspectorWidth / tabs.Length;
            for (int t = 0; t < tabs.Length; t++)
            {
                Rectangle tabRect = new Rectangle(inspRect.X + t * tabW, RibbonHeight, tabW, 40);
                bool isActive = (InspectorActiveTab == t);

                if (isActive)
                {
                    using (Pen activeP = new Pen(Color.FromArgb(99, 102, 241), 2.5f))
                    {
                        g.DrawLine(activeP, tabRect.X + 6, tabRect.Bottom - 1.25f, tabRect.Right - 6, tabRect.Bottom - 1.25f);
                    }
                }

                using (Font tf = new Font("Segoe UI", 8.5f, isActive ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(100, 116, 139)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(tabs[t], tf, tb, tabRect, sf);
                }
            }

            int contentY = RibbonHeight + 48;
            int availH = height - contentY - BottomStatusHeight;

            switch (InspectorActiveTab)
            {
                case 0:
                    DrawLayersTab(g, inspRect.X, contentY, RightInspectorWidth, availH);
                    break;
                case 1:
                    DrawFillAndStrokeTab(g, inspRect.X, contentY, RightInspectorWidth);
                    break;
                case 2:
                    DrawPropertiesTab(g, inspRect.X, contentY, RightInspectorWidth);
                    break;
            }
        }

        private void DrawLayersTab(D2DGraphics g, int x, int y, int w, int h)
        {
            // Action Buttons
            int btnY = y;
            DrawInspButton(g, x + 10, btnY, 52, 28, "+ Thêm", () => {
                ContextMenuStrip menu = new ContextMenuStrip();
                var itemRect = new ToolStripMenuItem("Thêm hình chữ nhật vector");
                itemRect.Click += (s, e) => Document?.AddVectorShape(VectorShapeType.Rectangle, new RectangleF(150, 150, 200, 150), ShapeFillColor, ShapeStrokeColor, ShapeStrokeWidth, ShapeCornerRadius, ActiveShapePointCount);
                var itemEllipse = new ToolStripMenuItem("Thêm hình tròn / Elip vector");
                itemEllipse.Click += (s, e) => Document?.AddVectorShape(VectorShapeType.Ellipse, new RectangleF(150, 150, 180, 180), ShapeFillColor, ShapeStrokeColor, ShapeStrokeWidth, ShapeCornerRadius, ActiveShapePointCount);
                var itemText = new ToolStripMenuItem("Thêm văn bản nghệ thuật");
                itemText.Click += (s, e) => Document?.AddText("ZenQ Vector Text", 150, 150, ActiveFontSize, ForegroundColor);
                var itemImg = new ToolStripMenuItem("🖼️ Chèn hình ảnh (Chuẩn Inkscape)...");
                itemImg.Click += (s, e) => PromptInsertImage();
                menu.Items.Add(itemRect);
                menu.Items.Add(itemEllipse);
                menu.Items.Add(itemText);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(itemImg);
                menu.Show(Cursor.Position);
            });
            DrawInspButton(g, x + 66, btnY, 64, 28, "Nhân bản", () => {
                Document.DuplicateActiveLayer();
            });
            DrawInspButton(g, x + 134, btnY, 46, 28, "Xóa", () => {
                Document.DeleteActiveLayer();
            });
            DrawInspButton(g, x + 184, btnY, 44, 28, "▲ Lên", () => {
                Document.MoveLayerUp();
            });
            DrawInspButton(g, x + 232, btnY, 48, 28, "▼ Xuống", () => {
                Document.MoveLayerDown();
            });

            // Layer Cards List
            int cardStartY = btnY + 38;
            int cardH = 50;
            for (int i = Document.Layers.Count - 1; i >= 0; i--)
            {
                var layer = Document.Layers[i];
                bool isSelected = (Document.ActiveLayerIndex == i);
                Rectangle cardRect = new Rectangle(x + 10, cardStartY, w - 20, cardH - 6);

                using (SolidBrush bgB = new SolidBrush(isSelected ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252)))
                using (Pen bP = new Pen(isSelected ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), isSelected ? 1.5f : 1f))
                {
                    g.FillRoundedRectangle(bgB, cardRect, 6f);
                    g.DrawRoundedRectangle(bP, cardRect, 6f);
                }

                // Visibility Eye
                Rectangle eyeRect = new Rectangle(cardRect.X + 8, cardRect.Y + 12, 20, 20);
                char eyeGlyph = layer.IsVisible ? '\uE7B3' : '\uED1A';
                FluentIconRenderer.DrawGlyphD2D(g, eyeGlyph, eyeRect, layer.IsVisible ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225), 13f);

                // Thumbnail icon
                Rectangle thumbRect = new Rectangle(cardRect.X + 34, cardRect.Y + 10, 24, 24);
                using (Pen tp = new Pen(Color.FromArgb(148, 163, 184), 1f))
                {
                    g.DrawRoundedRectangle(tp, thumbRect, 4f);
                    if (layer.Type == LayerType.VectorShape)
                    {
                        using (SolidBrush fb = new SolidBrush(layer.FillColor.A > 0 ? layer.FillColor : Color.FromArgb(203, 213, 225)))
                            g.FillRectangle(fb, thumbRect.X + 4, thumbRect.Y + 4, 16, 16);
                    }
                    else
                    {
                        FluentIconRenderer.DrawGlyphD2D(g, '\uE8D2', thumbRect, Color.FromArgb(71, 85, 105), 12f);
                    }
                }

                // Name & Type
                using (Font nameFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (Font typeFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (SolidBrush nameB = new SolidBrush(Color.FromArgb(30, 41, 59)))
                using (SolidBrush typeB = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString(layer.Name, nameFont, nameB, cardRect.X + 66, cardRect.Y + 7);
                    string tStr = layer.ShapeType switch
                    {
                        VectorShapeType.Rectangle => "Hình chữ nhật",
                        VectorShapeType.RoundedRect => "Chữ nhật bo góc",
                        VectorShapeType.Ellipse => "Hình tròn / Elip",
                        VectorShapeType.Star => "Ngôi sao",
                        VectorShapeType.Polygon => "Đa giác",
                        VectorShapeType.Spiral => "Đường xoắn ốc",
                        VectorShapeType.Arrow => "Mũi tên vector",
                        VectorShapeType.Heart => "Trái tim",
                        VectorShapeType.Line => "Đoạn thẳng",
                        VectorShapeType.Path => "Đường vẽ Bezier",
                        _ => "Lớp đồ họa"
                    };
                    g.DrawString(tStr, typeFont, typeB, cardRect.X + 66, cardRect.Y + 23);
                }

                cardStartY += cardH;
            }
        }

        private void DrawFillTab(D2DGraphics g, int x, int y, int w)
        {
            var l = Document.ActiveLayer;
            VectorFillType curType = (l != null && l.Type == LayerType.VectorShape) ? l.FillType : ShapeFillType;
            Color curCol = (l != null && l.Type == LayerType.VectorShape) ? l.FillColor : ShapeFillColor;
            Color curCol2 = (l != null && l.Type == LayerType.VectorShape) ? l.FillColor2 : ShapeFillColor2;

            using (Font titleF = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font lblF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush lb = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString("CHẾ ĐỘ TÔ MÀU", titleF, tb, x + 12, y);

                // 4 Compact Mode Icons at Corner (Chuẩn Inkscape)
                int btnY = y + 24;
                DrawModeIconButton(g, x + 12, btnY, 26, 0, () => SetFillType(VectorFillType.None), curType == VectorFillType.None);
                DrawModeIconButton(g, x + 44, btnY, 26, 1, () => SetFillType(VectorFillType.Solid), curType == VectorFillType.Solid);
                DrawModeIconButton(g, x + 76, btnY, 26, 2, () => SetFillType(VectorFillType.LinearGradient), curType == VectorFillType.LinearGradient);
                DrawModeIconButton(g, x + 108, btnY, 26, 3, () => SetFillType(VectorFillType.RadialGradient), curType == VectorFillType.RadialGradient);

                string modeLabel = curType switch
                {
                    VectorFillType.None => "Không tô màu",
                    VectorFillType.Solid => "Màu đơn sắc",
                    VectorFillType.LinearGradient => "Dải màu tuyến tính",
                    VectorFillType.RadialGradient => "Dải màu tỏa tròn",
                    _ => "Màu đơn sắc"
                };
                g.DrawString(modeLabel, lblF, tb, x + 144, btnY + 4);

                int secY = btnY + 40;
                if (curType == VectorFillType.Solid || curType == VectorFillType.None)
                {
                    g.DrawString("MÀU SẮC CHÍNH", titleF, tb, x + 12, secY);
                    secY += 24;

                    // Large current color preview swatch
                    Rectangle curSwatch = new Rectangle(x + 12, secY, 44, 28);
                    _inspectorClickables.Add((curSwatch, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curCol))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK)
                                SetFillColor(dlg.SelectedColor);
                        }
                    }));
                    using (SolidBrush fb = new SolidBrush(curCol))
                    using (Pen sp = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    {
                        g.FillRoundedRectangle(fb, curSwatch, 4f);
                        g.DrawRoundedRectangle(sp, curSwatch, 4f);
                    }

                    string hexStr = $"HEX: #{curCol.R:X2}{curCol.G:X2}{curCol.B:X2}";
                    g.DrawString(hexStr, lblF, tb, x + 64, secY + 6);

                    secY += 40;
                    g.DrawString("BẢNG MÀU CHỌN NHANH", titleF, tb, x + 12, secY);
                    secY += 24;
                    DrawQuickPalette(g, x + 12, secY, (c) => SetFillColor(c));
                }
                else if (curType == VectorFillType.LinearGradient || curType == VectorFillType.RadialGradient)
                {
                    g.DrawString("MÀU BẮT ĐẦU VÀ KẾT THÚC", titleF, tb, x + 12, secY);
                    secY += 24;

                    // Swatch 1
                    Rectangle s1 = new Rectangle(x + 12, secY, 40, 28);
                    _inspectorClickables.Add((s1, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curCol))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetFillColor(dlg.SelectedColor);
                        }
                    }));
                    using (SolidBrush fb = new SolidBrush(curCol))
                    using (Pen sp = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    {
                        g.FillRoundedRectangle(fb, s1, 4f);
                        g.DrawRoundedRectangle(sp, s1, 4f);
                    }
                    g.DrawString("Màu 1", lblF, tb, x + 56, secY + 6);

                    // Swatch 2
                    Rectangle s2 = new Rectangle(x + 130, secY, 40, 28);
                    _inspectorClickables.Add((s2, () => {
                        using (var dlg = new PhotoColorPickerDialogForm(curCol2))
                        {
                            if (dlg.ShowDialog() == DialogResult.OK) SetFillColor2(dlg.SelectedColor);
                        }
                    }));
                    using (SolidBrush fb2 = new SolidBrush(curCol2))
                    using (Pen sp = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    {
                        g.FillRoundedRectangle(fb2, s2, 4f);
                        g.DrawRoundedRectangle(sp, s2, 4f);
                    }
                    g.DrawString("Màu 2", lblF, tb, x + 174, secY + 6);

                    secY += 40;
                    g.DrawString("GÓC DẢI MÀU", titleF, tb, x + 12, secY);
                    secY += 24;
                    int[] angles = new int[] { 0, 45, 90, 180 };
                    for (int a = 0; a < angles.Length; a++)
                    {
                        int ang = angles[a];
                        DrawInspButton(g, x + 12 + a * 64, secY, 56, 26, $"{ang}°", () => {
                            SetGradientAngle(ang);
                        });
                    }

                    secY += 38;
                    DrawInspButton(g, x + 12, secY, 240, 28, "Chỉnh Gradient & Alpha nhiều màu...", () => {
                        OpenGradientEditor();
                    });

                    secY += 38;
                    g.DrawString("BẢNG MÀU CHỌN NHANH", titleF, tb, x + 12, secY);
                    secY += 24;
                    DrawQuickPalette(g, x + 12, secY, (c) => SetFillColor(c));
                }
            }
        }

        private void DrawStrokePaintTab(D2DGraphics g, int x, int y, int w)
        {
            var l = Document.ActiveLayer;
            VectorStrokeType curType = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeType : ShapeStrokeType;
            Color curCol = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeColor : ShapeStrokeColor;

            using (Font titleF = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font lblF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("CHẾ ĐỘ NÉT VIỀN", titleF, tb, x + 12, y);

                int btnY = y + 24;
                DrawModeIconButton(g, x + 12, btnY, 26, 0, () => SetStrokeType(VectorStrokeType.None), curType == VectorStrokeType.None);
                DrawModeIconButton(g, x + 44, btnY, 26, 1, () => SetStrokeType(VectorStrokeType.Solid), curType == VectorStrokeType.Solid);
                DrawModeIconButton(g, x + 76, btnY, 26, 2, () => SetStrokeType(VectorStrokeType.LinearGradient), curType == VectorStrokeType.LinearGradient);

                string strokeLabel = curType switch
                {
                    VectorStrokeType.None => "Không có viền nét",
                    VectorStrokeType.Solid => "Viền nét đơn sắc",
                    VectorStrokeType.LinearGradient => "Viền dải chuyển màu",
                    _ => "Viền nét đơn sắc"
                };
                g.DrawString(strokeLabel, lblF, tb, x + 114, btnY + 4);

                int secY = btnY + 44;
                g.DrawString("MÀU SẮC VIỀN", titleF, tb, x + 12, secY);
                secY += 24;

                Rectangle curSwatch = new Rectangle(x + 12, secY, 44, 28);
                _inspectorClickables.Add((curSwatch, () => {
                    using (var dlg = new PhotoColorPickerDialogForm(curCol))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK) SetStrokeColor(dlg.SelectedColor);
                    }
                }));
                using (SolidBrush sb = new SolidBrush(curCol))
                using (Pen sp = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                {
                    g.FillRoundedRectangle(sb, curSwatch, 4f);
                    g.DrawRoundedRectangle(sp, curSwatch, 4f);
                }

                string hexStr = $"HEX: #{curCol.R:X2}{curCol.G:X2}{curCol.B:X2}";
                g.DrawString(hexStr, lblF, tb, x + 64, secY + 6);

                secY += 40;
                g.DrawString("BẢNG MÀU CHỌN NHANH", titleF, tb, x + 12, secY);
                secY += 24;
                DrawQuickPalette(g, x + 12, secY, (c) => SetStrokeColor(c));
            }
        }

        private void DrawStrokeStyleTab(D2DGraphics g, int x, int y, int w)
        {
            var l = Document.ActiveLayer;
            float sw = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeWidth : ShapeStrokeWidth;
            DashStyle ds = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeDashStyle : ShapeStrokeDashStyle;
            LineJoin lj = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeLineJoin : ShapeStrokeLineJoin;
            LineCap lc = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeLineCap : ShapeStrokeLineCap;

            using (Font titleF = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font lblF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("ĐỘ DÀY NÉT VIỀN", titleF, tb, x + 12, y);

                int rowY = y + 24;
                DrawInspButton(g, x + 12, rowY, 28, 28, "-", () => {
                    SetStrokeWidth(Math.Max(0.5f, sw - 0.5f));
                });

                string valStr = $"{sw:0.0} px";
                g.DrawString(valStr, titleF, tb, x + 48, rowY + 5);

                DrawInspButton(g, x + 104, rowY, 28, 28, "+", () => {
                    SetStrokeWidth(Math.Min(50f, sw + 0.5f));
                });

                // Presets
                float[] swPresets = new float[] { 1f, 2f, 4f, 8f, 16f };
                for (int i = 0; i < swPresets.Length; i++)
                {
                    float pVal = swPresets[i];
                    DrawInspButton(g, x + 140 + i * 26, rowY, 24, 28, $"{(int)pVal}", () => {
                        SetStrokeWidth(pVal);
                    }, Math.Abs(sw - pVal) < 0.1f);
                }

                // Kiểu nét đứt
                int sec2Y = rowY + 44;
                g.DrawString("KIỂU NÉT ĐỨT (DASHES)", titleF, tb, x + 12, sec2Y);
                int dashY = sec2Y + 24;
                DrawInspButton(g, x + 12, dashY, 62, 28, "Nét liền", () => SetDashStyle(DashStyle.Solid), ds == DashStyle.Solid);
                DrawInspButton(g, x + 78, dashY, 62, 28, "Nét đứt", () => SetDashStyle(DashStyle.Dash), ds == DashStyle.Dash);
                DrawInspButton(g, x + 144, dashY, 62, 28, "Chấm bi", () => SetDashStyle(DashStyle.Dot), ds == DashStyle.Dot);
                DrawInspButton(g, x + 210, dashY, 66, 28, "Gạch chấm", () => SetDashStyle(DashStyle.DashDot), ds == DashStyle.DashDot);

                // Nối góc
                int sec3Y = dashY + 44;
                g.DrawString("NỐI GÓC VIỀN (JOIN)", titleF, tb, x + 12, sec3Y);
                int joinY = sec3Y + 24;
                DrawInspButton(g, x + 12, joinY, 82, 28, "Góc nhọn", () => SetLineJoin(LineJoin.Miter), lj == LineJoin.Miter);
                DrawInspButton(g, x + 98, joinY, 82, 28, "Bo tròn", () => SetLineJoin(LineJoin.Round), lj == LineJoin.Round);
                DrawInspButton(g, x + 184, joinY, 82, 28, "Vát góc", () => SetLineJoin(LineJoin.Bevel), lj == LineJoin.Bevel);

                // Đầu mút
                int sec4Y = joinY + 44;
                g.DrawString("ĐẦU MÚT VIỀN (CAP)", titleF, tb, x + 12, sec4Y);
                int capY = sec4Y + 24;
                DrawInspButton(g, x + 12, capY, 82, 28, "Đầu phẳng", () => SetLineCap(LineCap.Flat), lc == LineCap.Flat);
                DrawInspButton(g, x + 98, capY, 82, 28, "Đầu tròn", () => SetLineCap(LineCap.Round), lc == LineCap.Round);
                DrawInspButton(g, x + 184, capY, 82, 28, "Đầu vuông", () => SetLineCap(LineCap.Square), lc == LineCap.Square);
            }
        }

        private int _subTabFillStroke = 0; // 0: Tô, 1: Nét, 2: Kiểu nét

        private void DrawFillAndStrokeTab(D2DGraphics g, int x, int y, int w)
        {
            // 3 Sub-tabs: [ Tô màu ]  [ Màu viền ]  [ Kiểu viền ]
            int subW = (w - 24) / 3;
            DrawCornerPillButton(g, x + 12, y, subW - 4, 26, "Tô màu", () => _subTabFillStroke = 0, _subTabFillStroke == 0);
            DrawCornerPillButton(g, x + 12 + subW, y, subW - 4, 26, "Màu viền", () => _subTabFillStroke = 1, _subTabFillStroke == 1);
            DrawCornerPillButton(g, x + 12 + subW * 2, y, subW - 4, 26, "Kiểu viền", () => _subTabFillStroke = 2, _subTabFillStroke == 2);

            int subContentY = y + 36;
            if (_subTabFillStroke == 0) DrawFillTab(g, x, subContentY, w);
            else if (_subTabFillStroke == 1) DrawStrokePaintTab(g, x, subContentY, w);
            else DrawStrokeStyleTab(g, x, subContentY, w);
        }

        private void DrawCornerPillButton(D2DGraphics g, int x, int y, int w, int h, string text, Action action, bool isActive)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            _inspectorClickables.Add((r, action));

            Color bg = isActive ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252);
            Color border = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240);
            Color textCol = isActive ? Color.FromArgb(79, 70, 229) : Color.FromArgb(71, 85, 105);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, isActive ? 1.5f : 1f))
            using (Font f = new Font("Segoe UI", 8.2f, isActive ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(textCol))
            {
                g.FillRoundedRectangle(b, r, 4f);
                g.DrawRoundedRectangle(p, r, 4f);
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, tb, r.X + (r.Width - sz.Width) / 2f, r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        private void DrawPropertiesTab(D2DGraphics g, int x, int y, int w)
        {
            var l = Document.ActiveLayer;
            using (Font titleF = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font lblF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush grayB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                if (l == null)
                {
                    g.DrawString("Chưa chọn đối tượng nào", lblF, grayB, x + 16, y + 20);
                    return;
                }

                // 1. Kích thước & Vị trí
                g.DrawString("KÍCH THƯỚC & VỊ TRÍ", titleF, tb, x + 12, y);
                int rY = y + 22;
                string geomStr = $"Rộng: {l.Bounds.Width:0} px   |   Cao: {l.Bounds.Height:0} px\nX: {l.Bounds.X:0} px   |   Y: {l.Bounds.Y:0} px";
                g.DrawString(geomStr, lblF, grayB, x + 12, rY);

                // 2. Góc xoay
                int sec2Y = rY + 44;
                g.DrawString("GÓC XOAY ĐỐI TƯỢNG", titleF, tb, x + 12, sec2Y);
                int rotY = sec2Y + 22;
                string rotVal = $"{l.Rotation:0.0}°";
                g.DrawString(rotVal, titleF, tb, x + 12, rotY + 5);

                DrawInspButton(g, x + 70, rotY, 46, 26, "0°", () => {
                    Document.SaveUndoSnapshot();
                    l.Rotation = 0f;
                }, l.Rotation == 0f);

                DrawInspButton(g, x + 120, rotY, 52, 26, "+90°", () => {
                    Document.SaveUndoSnapshot();
                    l.Rotation = (l.Rotation + 90f) % 360f;
                });

                DrawInspButton(g, x + 176, rotY, 52, 26, "-90°", () => {
                    Document.SaveUndoSnapshot();
                    l.Rotation = (l.Rotation - 90f + 360f) % 360f;
                });

                DrawInspButton(g, x + 232, rotY, 52, 26, "180°", () => {
                    Document.SaveUndoSnapshot();
                    l.Rotation = (l.Rotation + 180f) % 360f;
                });

                int nextSecY = rotY + 38;

                // 3. Thuộc tính hình vector
                if (l.Type == LayerType.VectorShape)
                {
                    if (l.ShapeType == VectorShapeType.Rectangle || l.ShapeType == VectorShapeType.RoundedRect)
                    {
                        g.DrawString("BO GÓC CHỮ NHẬT", titleF, tb, x + 12, nextSecY);
                        int crY = nextSecY + 22;

                        DrawInspButton(g, x + 12, crY, 28, 26, "-", () => {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = Math.Max(0f, l.CornerRadius - 2f);
                            ShapeCornerRadius = l.CornerRadius;
                            if (l.CornerRadius > 0) l.ShapeType = VectorShapeType.RoundedRect;
                            else l.ShapeType = VectorShapeType.Rectangle;
                        });

                        string crStr = $"{l.CornerRadius:0} px";
                        g.DrawString(crStr, titleF, tb, x + 48, crY + 5);

                        DrawInspButton(g, x + 96, crY, 28, 26, "+", () => {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = Math.Min(100f, l.CornerRadius + 2f);
                            ShapeCornerRadius = l.CornerRadius;
                            l.ShapeType = VectorShapeType.RoundedRect;
                        });

                        DrawInspButton(g, x + 132, crY, 70, 26, "Góc nhọn", () => {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = 0f;
                            ShapeCornerRadius = 0f;
                            l.ShapeType = VectorShapeType.Rectangle;
                        }, l.CornerRadius == 0f);

                        DrawInspButton(g, x + 206, crY, 76, 26, "Bo tròn 16", () => {
                            Document.SaveUndoSnapshot();
                            l.CornerRadius = 16f;
                            ShapeCornerRadius = 16f;
                            l.ShapeType = VectorShapeType.RoundedRect;
                        }, l.CornerRadius == 16f);

                        nextSecY = crY + 38;
                    }
                    else if (l.ShapeType == VectorShapeType.Star || l.ShapeType == VectorShapeType.Polygon)
                    {
                        string shapeLabel = (l.ShapeType == VectorShapeType.Star) ? "SỐ CÁNH HÌNH SAO" : "SỐ CẠNH ĐA GIÁC";
                        g.DrawString(shapeLabel, titleF, tb, x + 12, nextSecY);
                        int ptY = nextSecY + 22;

                        DrawInspButton(g, x + 12, ptY, 28, 26, "-", () => {
                            Document.SaveUndoSnapshot();
                            l.ShapePointCount = Math.Max(3, l.ShapePointCount - 1);
                            ActiveShapePointCount = l.ShapePointCount;
                        });

                        string ptStr = $"{l.ShapePointCount}";
                        g.DrawString(ptStr, titleF, tb, x + 48, ptY + 5);

                        DrawInspButton(g, x + 80, ptY, 28, 26, "+", () => {
                            Document.SaveUndoSnapshot();
                            l.ShapePointCount = Math.Min(30, l.ShapePointCount + 1);
                            ActiveShapePointCount = l.ShapePointCount;
                        });

                        int[] presets = (l.ShapeType == VectorShapeType.Star) ? new int[] { 4, 5, 6, 8 } : new int[] { 3, 5, 6, 8 };
                        for (int pi = 0; pi < presets.Length; pi++)
                        {
                            int pVal = presets[pi];
                            DrawInspButton(g, x + 116 + pi * 32, ptY, 28, 26, $"{pVal}", () => {
                                Document.SaveUndoSnapshot();
                                l.ShapePointCount = pVal;
                                ActiveShapePointCount = pVal;
                            }, l.ShapePointCount == pVal);
                        }

                        nextSecY = ptY + 38;
                    }

                    // Đổ bóng
                    g.DrawString("HIỆU ỨNG ĐỔ BÓNG", titleF, tb, x + 12, nextSecY);
                    int shdY = nextSecY + 22;
                    DrawInspButton(g, x + 12, shdY, 110, 26, l.HasShadow ? "Bật đổ bóng" : "Tắt đổ bóng", () => {
                        Document.SaveUndoSnapshot();
                        l.HasShadow = !l.HasShadow;
                    }, l.HasShadow);

                    nextSecY = shdY + 38;
                }

                // 4. Căn gióng
                g.DrawString("CĂN GIÓNG ĐỐI TƯỢNG", titleF, tb, x + 12, nextSecY);
                int alY = nextSecY + 22;
                DrawInspButton(g, x + 12, alY, 78, 26, "Trái", () => {
                    Document.SaveUndoSnapshot();
                    l.Bounds = new RectangleF(0, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height);
                });
                DrawInspButton(g, x + 94, alY, 78, 26, "Giữa ngang", () => {
                    Document.SaveUndoSnapshot();
                    l.Bounds = new RectangleF((Document.Width - l.Bounds.Width) / 2f, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height);
                });
                DrawInspButton(g, x + 176, alY, 78, 26, "Phải", () => {
                    Document.SaveUndoSnapshot();
                    l.Bounds = new RectangleF(Document.Width - l.Bounds.Width, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height);
                });

                int al2Y = alY + 32;
                DrawInspButton(g, x + 12, al2Y, 78, 26, "Trên", () => {
                    Document.SaveUndoSnapshot();
                    l.Bounds = new RectangleF(l.Bounds.X, 0, l.Bounds.Width, l.Bounds.Height);
                });
                DrawInspButton(g, x + 94, al2Y, 78, 26, "Giữa dọc", () => {
                    Document.SaveUndoSnapshot();
                    l.Bounds = new RectangleF(l.Bounds.X, (Document.Height - l.Bounds.Height) / 2f, l.Bounds.Width, l.Bounds.Height);
                });
                DrawInspButton(g, x + 176, al2Y, 78, 26, "Dưới", () => {
                    Document.SaveUndoSnapshot();
                    l.Bounds = new RectangleF(l.Bounds.X, Document.Height - l.Bounds.Height, l.Bounds.Width, l.Bounds.Height);
                });
            }
        }

        private void DrawModeIconButton(D2DGraphics g, int x, int y, int size, int modeIdx, Action action, bool isActive)
        {
            Rectangle r = new Rectangle(x, y, size, size);
            if (action != null) _inspectorClickables.Add((r, action));

            Color bg = isActive ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252);
            Color border = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(203, 213, 225);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, isActive ? 1.5f : 1f))
            {
                g.FillRoundedRectangle(b, r, 4f);
                g.DrawRoundedRectangle(p, r, 4f);

                Rectangle inner = new Rectangle(x + 4, y + 4, size - 8, size - 8);
                if (modeIdx == 0) // No Paint (X)
                {
                    using (Pen redP = new Pen(Color.FromArgb(239, 68, 68), 1.8f))
                    {
                        g.DrawLine(redP, inner.Left + 1, inner.Bottom - 1, inner.Right - 1, inner.Top + 1);
                    }
                }
                else if (modeIdx == 1) // Solid Color
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        g.FillRectangle(sb, inner);
                    }
                }
                else if (modeIdx == 2) // Linear Gradient
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(inner, Color.FromArgb(203, 213, 225), Color.FromArgb(71, 85, 105), 0f))
                    {
                        g.FillRectangle(lgb, inner);
                    }
                }
                else if (modeIdx == 3) // Radial Gradient
                {
                    using (Pen circleP = new Pen(Color.FromArgb(71, 85, 105), 1.5f))
                    using (SolidBrush centerB = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        g.DrawEllipse(circleP, inner);
                        g.FillEllipse(centerB, x + size / 2 - 2, y + size / 2 - 2, 4, 4);
                    }
                }
            }
        }

        private void DrawQuickPalette(D2DGraphics g, int startX, int startY, Action<Color> onSelect)
        {
            Color[] colors = new Color[]
            {
                Color.FromArgb(239, 68, 68), Color.FromArgb(249, 115, 22), Color.FromArgb(245, 158, 11),
                Color.FromArgb(234, 179, 8), Color.FromArgb(132, 204, 22), Color.FromArgb(34, 197, 94),
                Color.FromArgb(16, 185, 129), Color.FromArgb(20, 184, 166), Color.FromArgb(6, 182, 212),
                Color.FromArgb(14, 165, 233), Color.FromArgb(59, 130, 246), Color.FromArgb(99, 102, 241),
                Color.FromArgb(139, 92, 246), Color.FromArgb(168, 85, 247), Color.FromArgb(217, 70, 239),
                Color.FromArgb(236, 72, 153), Color.FromArgb(244, 63, 94), Color.FromArgb(15, 23, 42),
                Color.FromArgb(71, 85, 105), Color.FromArgb(148, 163, 184), Color.FromArgb(203, 213, 225),
                Color.FromArgb(241, 245, 249), Color.FromArgb(255, 255, 255), Color.Transparent
            };

            int cols = 6;
            int sz = 38;
            int gap = 6;
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];
                int row = i / cols;
                int col = i % cols;
                Rectangle r = new Rectangle(startX + col * (sz + gap), startY + row * (sz + gap), sz, sz);

                _inspectorClickables.Add((r, () => onSelect(c)));

                using (Pen bp = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    if (c.A == 0)
                    {
                        using (SolidBrush wb = new SolidBrush(Color.White))
                        using (Pen redSlash = new Pen(Color.FromArgb(239, 68, 68), 2f))
                        {
                            g.FillRoundedRectangle(wb, r, 4f);
                            g.DrawRoundedRectangle(bp, r, 4f);
                            g.DrawLine(redSlash, r.Left + 4, r.Bottom - 4, r.Right - 4, r.Top + 4);
                        }
                    }
                    else
                    {
                        using (SolidBrush cb = new SolidBrush(c))
                        {
                            g.FillRoundedRectangle(cb, r, 4f);
                            g.DrawRoundedRectangle(bp, r, 4f);
                        }
                    }
                }
            }
        }

        private void DrawInspButton(D2DGraphics g, int x, int y, int w, int h, string text, Action action, bool isActive = false)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            _inspectorClickables.Add((r, action));

            Color bgColor = isActive ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252);
            Color borderColor = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240);
            Color textColor = isActive ? Color.FromArgb(79, 70, 229) : Color.FromArgb(30, 41, 59);

            using (SolidBrush b = new SolidBrush(bgColor))
            using (Pen p = new Pen(borderColor, isActive ? 1.5f : 1f))
            using (Font font = new Font("Segoe UI", 8f, isActive ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(textColor))
            {
                g.FillRoundedRectangle(b, r, 5f);
                g.DrawRoundedRectangle(p, r, 5f);
                var sz = g.MeasureString(text, font);
                g.DrawString(text, font, tb, x + (w - sz.Width) / 2f, y + (h - sz.Height) / 2f);
            }
        }

        #region Vector Property Mutators
        private void SetFillType(VectorFillType type)
        {
            ShapeFillType = type;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.FillType = type;
            }
        }

        private void SetFillColor(Color c)
        {
            ShapeFillColor = c;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.FillColor = c;
                if (Document.ActiveLayer.FillType != VectorFillType.LinearGradient && 
                    Document.ActiveLayer.FillType != VectorFillType.RadialGradient)
                {
                    Document.ActiveLayer.FillType = (c.A == 0) ? VectorFillType.None : VectorFillType.Solid;
                }
                else if (Document.ActiveLayer.GradientStops != null && Document.ActiveLayer.GradientStops.Count > 0)
                {
                    Document.ActiveLayer.GradientStops[0].Color = c;
                }
            }
            else
            {
                if (ShapeFillType != VectorFillType.LinearGradient && ShapeFillType != VectorFillType.RadialGradient)
                {
                    ShapeFillType = (c.A == 0) ? VectorFillType.None : VectorFillType.Solid;
                }
                else if (ShapeGradientStops != null && ShapeGradientStops.Count > 0)
                {
                    ShapeGradientStops[0].Color = c;
                }
            }
        }

        private void SetFillColor2(Color c)
        {
            ShapeFillColor2 = c;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.FillColor2 = c;
                if (Document.ActiveLayer.GradientStops != null && Document.ActiveLayer.GradientStops.Count >= 2)
                {
                    Document.ActiveLayer.GradientStops[Document.ActiveLayer.GradientStops.Count - 1].Color = c;
                }
            }
            else
            {
                if (ShapeGradientStops != null && ShapeGradientStops.Count >= 2)
                {
                    ShapeGradientStops[ShapeGradientStops.Count - 1].Color = c;
                }
            }
        }

        public void OpenGradientEditor(IWin32Window parent = null)
        {
            var l = Document.ActiveLayer;
            VectorFillType curType = (l != null && l.Type == LayerType.VectorShape) ? l.FillType : ShapeFillType;
            if (curType != VectorFillType.LinearGradient && curType != VectorFillType.RadialGradient)
            {
                curType = VectorFillType.LinearGradient;
            }
            float curAngle = (l != null && l.Type == LayerType.VectorShape) ? l.FillGradientAngle : ShapeFillGradientAngle;
            Color c1 = (l != null && l.Type == LayerType.VectorShape) ? l.FillColor : ShapeFillColor;
            Color c2 = (l != null && l.Type == LayerType.VectorShape) ? l.FillColor2 : ShapeFillColor2;
            List<PhotoGradientStop> stops = (l != null && l.Type == LayerType.VectorShape && l.GradientStops != null && l.GradientStops.Count >= 2)
                ? l.GradientStops
                : ShapeGradientStops;

            using (var dlg = new GradientEditorDialogForm(curType, curAngle, c1, c2, stops))
            {
                DialogResult res = (parent != null) ? dlg.ShowDialog(parent) : dlg.ShowDialog();
                if (res == DialogResult.OK)
                {
                    ShapeFillType = dlg.SelectedFillType;
                    ShapeFillGradientAngle = dlg.SelectedAngle;
                    ShapeGradientStops = dlg.SelectedStops.Select(s => s.Clone()).ToList();
                    if (ShapeGradientStops.Count >= 2)
                    {
                        ShapeFillColor = ShapeGradientStops[0].Color;
                        ShapeFillColor2 = ShapeGradientStops[ShapeGradientStops.Count - 1].Color;
                    }

                    if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
                    {
                        Document.SaveUndoSnapshot();
                        Document.ActiveLayer.FillType = dlg.SelectedFillType;
                        Document.ActiveLayer.FillGradientAngle = dlg.SelectedAngle;
                        Document.ActiveLayer.GradientStops = dlg.SelectedStops.Select(s => s.Clone()).ToList();
                        if (Document.ActiveLayer.GradientStops.Count >= 2)
                        {
                            Document.ActiveLayer.FillColor = Document.ActiveLayer.GradientStops[0].Color;
                            Document.ActiveLayer.FillColor2 = Document.ActiveLayer.GradientStops[Document.ActiveLayer.GradientStops.Count - 1].Color;
                        }
                    }
                }
            }
        }

        private void SetGradientAngle(float angle)
        {
            ShapeFillGradientAngle = angle;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.FillGradientAngle = angle;
            }
        }

        private void SetStrokeType(VectorStrokeType type)
        {
            ShapeStrokeType = type;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.StrokeType = type;
            }
        }

        private void SetStrokeColor(Color c)
        {
            ShapeStrokeColor = c;
            ShapeStrokeType = (c.A == 0) ? VectorStrokeType.None : VectorStrokeType.Solid;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.StrokeColor = c;
                Document.ActiveLayer.StrokeType = (c.A == 0) ? VectorStrokeType.None : VectorStrokeType.Solid;
            }
        }

        private void SetStrokeWidth(float w)
        {
            ShapeStrokeWidth = w;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.StrokeWidth = w;
            }
        }

        private void SetDashStyle(DashStyle ds)
        {
            ShapeStrokeDashStyle = ds;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.StrokeDashStyle = ds;
            }
        }

        private void SetLineJoin(LineJoin lj)
        {
            ShapeStrokeLineJoin = lj;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.StrokeLineJoin = lj;
            }
        }

        private void SetLineCap(LineCap lc)
        {
            ShapeStrokeLineCap = lc;
            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.SaveUndoSnapshot();
                Document.ActiveLayer.StrokeLineCap = lc;
            }
        }
        #endregion
        #endregion

private void DrawSmallButtonWithGlyph(D2DGraphics g, int x, int y, int w, int h, char glyph, string text)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (Font f = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.FillRoundedRectangle(b, r, 6f);
                g.DrawRoundedRectangle(p, r, 6f);

                Rectangle gBox = new Rectangle(x + 5, y + (h - 14) / 2, 14, 14);
                FluentIconRenderer.DrawGlyphD2D(g, glyph, gBox, Color.FromArgb(71, 85, 105), 11f);

                g.DrawString(text, f, tb, x + 21, y + (h - 16) / 2);
            }
        }

        private void DrawSmallButton(D2DGraphics g, int x, int y, int w, int h, string text)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (Font f = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(51, 65, 85)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.FillRoundedRectangle(b, r, 6f);
                g.DrawRoundedRectangle(p, r, 6f);
                g.DrawString(text, f, tb, r, sf);
            }
        }

        private void DrawFilterPresetButton(D2DGraphics g, Rectangle r, char glyph, string text)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (Font f = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillRoundedRectangle(b, r, 6f);
                g.DrawRoundedRectangle(p, r, 6f);
                Rectangle gBox = new Rectangle(r.X + 8, r.Y + (r.Height - 16) / 2, 16, 16);
                FluentIconRenderer.DrawGlyphD2D(g, glyph, gBox, Color.FromArgb(71, 85, 105), 13f);
                g.DrawString(text, f, tb, r.X + 28, r.Y + (r.Height - 16) / 2);
            }
        }

        private void DrawStatusBar(D2DGraphics g, int width, int height)
        {
            Rectangle statusRect = new Rectangle(0, height - BottomStatusHeight, width, BottomStatusHeight);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen sep = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (Font sf = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.FillRectangle(bg, statusRect);
                g.DrawLine(sep, 0, statusRect.Y, width, statusRect.Y);

                string statusText = $"Kích thước Canvas: {Document.Width} × {Document.Height} px  |  Độ phóng: {(int)(Zoom * 100)}%  |  Lớp hiện tại: {Document.ActiveLayer?.Name ?? "None"}  |  Hệ màu: RGB 32-bit  |  Hỗ trợ PSD";
                g.DrawString(statusText, sf, sb, LeftToolbarWidth + 12, statusRect.Y + 5);
            }
        }
        #endregion

        #region User Interaction Handlers (Mouse & Keyboard)
        
        private void HandleTopBarMouseDown(int x, int y, MouseButtons button)
        {
            if (button != MouseButtons.Left) return;

            if ((SelectedTool == PhotoTool.Brush || SelectedTool == PhotoTool.Pen || SelectedTool == PhotoTool.Eraser) && _brushSizeTrackRect.Contains(x, y))
            {
                _isDraggingBrushSizeSlider = true;
                UpdateBrushSizeFromSlider(x);
                return;
            }
            if ((SelectedTool == PhotoTool.Rectangle || SelectedTool == PhotoTool.Shape) && _radiusTrackRect.Contains(x, y))
            {
                _isDraggingRadiusSlider = true;
                UpdateRadiusFromSlider(x);
                return;
            }
            if (SelectedTool == PhotoTool.Text && _fontSizeTrackRect.Contains(x, y))
            {
                _isDraggingFontSizeSlider = true;
                UpdateFontSizeFromSlider(x);
                return;
            }

            for (int i = _topBarClickables.Count - 1; i >= 0; i--)
            {
                if (_topBarClickables[i].Rect.Contains(x, y))
                {
                    _topBarClickables[i].Action?.Invoke();
                    return;
                }
            }
        }

        public void HandleMouseDown(int x, int y, MouseButtons button, Keys modifiers, int viewW, int viewH)
        {
            // 0. Check Horizontal & Vertical Workspace Scrollbars Click
            if (!HScrollBarThumbRect.IsEmpty && HScrollBarThumbRect.Contains(x, y))
            {
                IsDraggingHScrollBar = true;
                _scrollBarDragStartMouseX = x;
                _scrollBarDragStartPanX = PanX;
                return;
            }
            if (!HScrollBarTrackRect.IsEmpty && HScrollBarTrackRect.Contains(x, y))
            {
                GetScrollLimits(viewW, viewH, out float minX, out float maxX, out float minY, out float maxY);
                float availW = Math.Max(100f, viewW - LeftToolbarWidth - RightInspectorWidth);
                if (x < HScrollBarThumbRect.X)
                    PanX = Math.Min(maxX, PanX + availW * 0.75f);
                else
                    PanX = Math.Max(minX, PanX - availW * 0.75f);
                ClampPan(viewW, viewH);
                return;
            }

            if (!VScrollBarThumbRect.IsEmpty && VScrollBarThumbRect.Contains(x, y))
            {
                IsDraggingVScrollBar = true;
                _scrollBarDragStartMouseY = y;
                _scrollBarDragStartPanY = PanY;
                return;
            }
            if (!VScrollBarTrackRect.IsEmpty && VScrollBarTrackRect.Contains(x, y))
            {
                GetScrollLimits(viewW, viewH, out float minX, out float maxX, out float minY, out float maxY);
                float availH = Math.Max(100f, viewH - RibbonHeight - TopOptionsBarHeight - BottomStatusHeight);
                if (y < VScrollBarThumbRect.Y)
                    PanY = Math.Min(maxY, PanY + availH * 0.75f);
                else
                    PanY = Math.Max(minY, PanY - availH * 0.75f);
                ClampPan(viewW, viewH);
                return;
            }

            // 1. Check Left Toolbar Clicks
            if (x < LeftToolbarWidth && y >= RibbonHeight)
            {
                int toolIdx = (y - RibbonHeight - 12) / 40;
                PhotoTool[] tools = new PhotoTool[]
                {
                    PhotoTool.Move,
                    PhotoTool.NodeTool,
                    PhotoTool.Rectangle,
                    PhotoTool.Ellipse,
                    PhotoTool.Star,
                    PhotoTool.Pen,
                    PhotoTool.Brush,
                    PhotoTool.Eraser,
                    PhotoTool.Text,
                    PhotoTool.Bucket,
                    PhotoTool.Eyedropper,
                    PhotoTool.Gradient,
                    PhotoTool.Hand
                };
                if (toolIdx >= 0 && toolIdx < tools.Length)
                {
                    SelectedTool = tools[toolIdx];
                    if (SelectedTool == PhotoTool.NodeTool)
                    {
                        ActivateNodeTool();
                    }
                    else if (SelectedTool == PhotoTool.Rectangle) { ActiveShapeType = VectorShapeType.Rectangle; }
                    else if (SelectedTool == PhotoTool.Ellipse) { ActiveShapeType = VectorShapeType.Ellipse; }
                    else if (SelectedTool == PhotoTool.Star) { ActiveShapeType = VectorShapeType.Star; }
                    return;
                }

                // Check Color Swatches click
                int swY = RibbonHeight + 12 + tools.Length * 40 + 16;
                Rectangle fgRect = new Rectangle(10, swY + 4, 22, 22);
                Rectangle bgRect = new Rectangle(18, swY + 12, 22, 22);
                if (fgRect.Contains(x, y))
                {
                    using (var dlg = new PhotoColorPickerDialogForm(ForegroundColor))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            ForegroundColor = dlg.SelectedColor;
                            if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
                            {
                                Document.SaveUndoSnapshot();
                                Document.ActiveLayer.FillColor = dlg.SelectedColor;
                            }
                        }
                    }
                    return;
                }
                if (bgRect.Contains(x, y))
                {
                    using (var dlg = new PhotoColorPickerDialogForm(BackgroundColor))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            BackgroundColor = dlg.SelectedColor;
                        }
                    }
                    return;
                }
                return;
            }

                        // 2. Check Top Options Bar Clicks
            if (x >= LeftToolbarWidth && x < viewW - RightInspectorWidth && y >= RibbonHeight && y < RibbonHeight + TopOptionsBarHeight)
            {
                HandleTopBarMouseDown(x, y, button);
                return;
            }

            // 3. Check Right Inspector Clicks
            if (x >= viewW - RightInspectorWidth && y >= RibbonHeight)
            {
                HandleRightInspectorMouseDown(x, y, viewW, viewH);
                return;
            }

            // 4. Check Bottom Color Palette Clicks (Góc dãy màu & Các ô màu)
            if (x >= LeftToolbarWidth && x < viewW - RightInspectorWidth && y >= viewH - BottomStatusHeight)
            {
                for (int b = _bottomBarClickables.Count - 1; b >= 0; b--)
                {
                    if (_bottomBarClickables[b].Rect.Contains(x, y))
                    {
                        _bottomBarClickables[b].Action?.Invoke();
                        return;
                    }
                }

                for (int s = _bottomPaletteSwatches.Count - 1; s >= 0; s--)
                {
                    if (_bottomPaletteSwatches[s].Rect.Contains(x, y))
                    {
                        Color c = _bottomPaletteSwatches[s].Color;
                        if (button == MouseButtons.Right || (modifiers & Keys.Shift) == Keys.Shift || _bottomColorTargetIsStroke)
                        {
                            SetStrokeColor(c);
                        }
                        else
                        {
                            SetFillColor(c);
                        }
                        return;
                    }
                }
                return;
            }

            // Check Ruler Clicks (Drag out new guidelines)
            if (ShowRulers)
            {
                int rulerStartY = RibbonHeight + TopOptionsBarHeight;
                // Top Ruler (Drag Horizontal Guide)
                if (x >= LeftToolbarWidth + RulerThickness && x < viewW - RightInspectorWidth && y >= rulerStartY && y < rulerStartY + RulerThickness)
                {
                    _isDraggingRulerGuideH = true;
                    _currentDraggingGuidePos = ScreenToCanvas(x, y, viewW, viewH).Y;
                    return;
                }
                // Left Ruler (Drag Vertical Guide)
                if (x >= LeftToolbarWidth && x < LeftToolbarWidth + RulerThickness && y >= rulerStartY + RulerThickness && y < viewH - BottomStatusHeight)
                {
                    _isDraggingRulerGuideV = true;
                    _currentDraggingGuidePos = ScreenToCanvas(x, y, viewW, viewH).X;
                    return;
                }
            }

            // 5. Middle Mouse Button, Hand Tool, or Spacebar held: Pan Canvas
            if (button == MouseButtons.Middle || SelectedTool == PhotoTool.Hand || (modifiers & Keys.Space) == Keys.Space || IsSpaceHeld)
            {
                IsPanning = true;
                _lastPanPoint = new Point(x, y);
                return;
            }

            // 4. Canvas Work Area Interactions
            PointF canvasPt = ScreenToCanvas(x, y, viewW, viewH);
            _dragStartPt = canvasPt;
            _currentDragPt = canvasPt;

            // Kiểm tra tương tác với các handle biến đổi (Bo góc, Thay đổi kích thước, Xoay)
            if (Document.ActiveLayer != null && (IsShapeTool(SelectedTool) || SelectedTool == PhotoTool.Move))
            {
                int hitH = CheckTransformHandles(x, y, viewW, viewH);
                if (hitH >= 0)
                {
                    _selectedTransformHandle = hitH;
                    Document.SaveUndoSnapshot();
                    _dragInitialBounds = Document.ActiveLayer.Bounds;
                    _dragInitialRotation = Document.ActiveLayer.Rotation;
                    if (Document.ActiveLayer.PathPoints != null)
                    {
                        _dragInitialPathPoints = new List<PointF>(Document.ActiveLayer.PathPoints);
                    }
                    if (Document.ActiveLayer.SubPaths != null)
                    {
                        _dragInitialSubPaths = Document.ActiveLayer.SubPaths.Select(sp => sp.Clone()).ToList();
                    }
                    _isDrawing = true;
                    return;
                }
            }

            if (IsShapeTool(SelectedTool))
            {
                // Nếu click vào một hình vector khác trên canvas thì chọn nó
                for (int i = Document.Layers.Count - 1; i >= 0; i--)
                {
                    var l = Document.Layers[i];
                    if (l.Type == LayerType.VectorShape && l.Bounds.Contains(canvasPt))
                    {
                        Document.ActiveLayerIndex = i;
                        ShapeCornerRadius = l.CornerRadius;
                        ShapeFillColor = l.FillColor;
                        ShapeFillColor2 = l.FillColor2;
                        ShapeFillType = l.FillType;
                        ShapeFillGradientAngle = l.FillGradientAngle;
                        ShapeStrokeColor = l.StrokeColor;
                        ShapeStrokeWidth = l.StrokeWidth;
                        if (l.GradientStops != null && l.GradientStops.Count > 0)
                        {
                            ShapeGradientStops = l.GradientStops.Select(s => s.Clone()).ToList();
                        }
                        _isDrawing = false;
                        return;
                    }
                }
                // Nếu click vào vùng trống, bắt đầu vẽ hình vector mới
                _selectedTransformHandle = -1;
                _isDrawing = true;
                return;
            }
            else if (SelectedTool == PhotoTool.Gradient)
            {
                _selectedTransformHandle = -1;
                _isDrawing = true;
                return;
            }
            else if (SelectedTool == PhotoTool.NodeTool)
            {
                HandleNodeToolMouseDown(canvasPt, x, y, button, modifiers, viewW, viewH);
                return;
            }
            else if (SelectedTool == PhotoTool.Brush || SelectedTool == PhotoTool.Eraser)
            {
                if (Document.ActiveLayer == null || Document.ActiveLayer.Type != LayerType.Raster)
                {
                    Document.AddRasterLayer("Lớp vẽ mới");
                }
                Document.SaveUndoSnapshot();
                _lastBrushPt = canvasPt;
                PaintBrushDot(canvasPt);
                _isDrawing = true;
            }
            else if (SelectedTool == PhotoTool.Bucket)
            {
                SelectLayerAt(canvasPt);
                Document.SaveUndoSnapshot();
                if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
                {
                    Document.ActiveLayer.FillColor = ForegroundColor;
                }
                else if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text)
                {
                    Document.ActiveLayer.TextColor = ForegroundColor;
                }
                else
                {
                    if (Document.ActiveLayer == null || Document.ActiveLayer.Type != LayerType.Raster)
                    {
                        Document.AddRasterLayer("Lớp màu đổ");
                    }
                    FloodFillActiveLayer(canvasPt, ForegroundColor);
                }
                _isDrawing = true;
            }
            else if (SelectedTool == PhotoTool.Eyedropper)
            {
                SampleCanvasColor(canvasPt);
                _isDrawing = false;
            }
            else if (SelectedTool == PhotoTool.Text)
            {
                SelectLayerAt(canvasPt);
                if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text)
                {
                    Document.SaveUndoSnapshot();
                    Document.ActiveLayer.TextContent = ShowTextInputDialog(Document.ActiveLayer.TextContent);
                }
                else
                {
                    string newTxt = ShowTextInputDialog("Nhập văn bản tại đây");
                    if (!string.IsNullOrWhiteSpace(newTxt))
                    {
                        Document.AddText(newTxt, canvasPt.X, canvasPt.Y, ActiveFontSize, ForegroundColor);
                    }
                }
                SelectedTool = PhotoTool.Move;
                _isDrawing = false;
            }
            else if (SelectedTool == PhotoTool.Pen)
            {
                if (Document.ActiveLayer == null || Document.ActiveLayer.Type != LayerType.VectorShape || Document.ActiveLayer.ShapeType != VectorShapeType.Path)
                {
                    Document.AddVectorShape(VectorShapeType.Path, new RectangleF(canvasPt.X, canvasPt.Y, 1, 1), ShapeFillColor, ShapeStrokeColor, ShapeStrokeWidth);
                    Document.ActiveLayer.PathPoints.Add(canvasPt);
                    if (Document.ActiveLayer.SubPaths == null) Document.ActiveLayer.SubPaths = new List<VectorSubPath>();
                    if (Document.ActiveLayer.SubPaths.Count == 0) Document.ActiveLayer.SubPaths.Add(new VectorSubPath(false));
                    var n = new VectorPathNode(canvasPt, VectorNodeType.Corner);
                    Document.ActiveLayer.SubPaths[0].Nodes.Add(n);
                    _activeDragNode = n;
                }
                else
                {
                    var curLayer = Document.ActiveLayer;
                    curLayer.EnsureNodesInitialized();
                    var sp = (curLayer.SubPaths != null && curLayer.SubPaths.Count > 0) ? curLayer.SubPaths[curLayer.SubPaths.Count - 1] : null;

                    // Nếu click gần điểm đầu tiên của phân đoạn -> Đóng đường dẫn (Close Path)!
                    if (sp != null && sp.Nodes != null && sp.Nodes.Count > 1 && !sp.IsClosed)
                    {
                        var firstNode = sp.Nodes[0];
                        float distSq = (canvasPt.X - firstNode.Point.X) * (canvasPt.X - firstNode.Point.X) +
                                       (canvasPt.Y - firstNode.Point.Y) * (canvasPt.Y - firstNode.Point.Y);
                        float snapTol = 12f / Math.Max(0.1f, Zoom);
                        if (distSq <= snapTol * snapTol)
                        {
                            Document.SaveUndoSnapshot();
                            sp.IsClosed = true;
                            VectorPathModel.UpdateLayerBounds(curLayer);
                            _isDrawing = false;
                            _isPenDraggingHandle = false;
                            _activeDragNode = null;
                            return;
                        }
                    }

                    Document.SaveUndoSnapshot();
                    curLayer.PathPoints.Add(canvasPt);
                    if (curLayer.SubPaths == null) curLayer.SubPaths = new List<VectorSubPath>();
                    if (curLayer.SubPaths.Count == 0) curLayer.SubPaths.Add(new VectorSubPath(false));
                    var n = new VectorPathNode(canvasPt, VectorNodeType.Corner);
                    curLayer.SubPaths[curLayer.SubPaths.Count - 1].Nodes.Add(n);
                    _activeDragNode = n;
                    VectorPathModel.UpdateLayerBounds(curLayer);
                }
                _isDrawing = true;
                _isPenDraggingHandle = true;
                _dragStartPt = canvasPt;
            }
            else if (SelectedTool == PhotoTool.Move)
            {
                _selectedTransformHandle = CheckTransformHandles(x, y, viewW, viewH);
                if (_selectedTransformHandle < 0)
                {
                    bool isShift = (modifiers & Keys.Shift) == Keys.Shift;
                    SelectLayerAt(canvasPt, isShift);
                    if (Document.ActiveLayer != null) _selectedTransformHandle = 9;
                }

                if (Document.ActiveLayer != null)
                {
                    Document.SaveUndoSnapshot();
                    if (Document.SelectedLayers.Count > 1)
                    {
                        _dragInitialBounds = GetCombinedSelectionBounds();
                    }
                    else
                    {
                        _dragInitialBounds = Document.ActiveLayer.Bounds;
                    }
                    _dragInitialRotation = Document.ActiveLayer.Rotation;
                    if (Document.ActiveLayer.PathPoints != null)
                    {
                        _dragInitialPathPoints = new List<PointF>(Document.ActiveLayer.PathPoints);
                    }
                    if (Document.ActiveLayer.SubPaths != null)
                    {
                        _dragInitialSubPaths = Document.ActiveLayer.SubPaths.Select(sp => sp.Clone()).ToList();
                    }

                    _multiDragInitialBounds.Clear();
                    _multiDragInitialRotations.Clear();
                    _multiDragInitialPoints.Clear();
                    _multiDragInitialSubPaths.Clear();
                    foreach (var sel in Document.SelectedLayers)
                    {
                        _multiDragInitialBounds[sel] = sel.Bounds;
                        _multiDragInitialRotations[sel] = sel.Rotation;
                        if (sel.PathPoints != null) _multiDragInitialPoints[sel] = new List<PointF>(sel.PathPoints);
                        if (sel.SubPaths != null) _multiDragInitialSubPaths[sel] = sel.SubPaths.Select(sp => sp.Clone()).ToList();
                    }
                }
                _isDrawing = true;
            }
            else if (SelectedTool == PhotoTool.Crop)
            {
                if (!IsCropping)
                {
                    IsCropping = true;
                    CropRect = new RectangleF(Document.Width * 0.1f, Document.Height * 0.1f, Document.Width * 0.8f, Document.Height * 0.8f);
                }
                _isDrawing = true;
            }
        }

        public void HandleMouseMove(int x, int y, MouseButtons button, Keys modifiers, int viewW, int viewH)
        {
            if (IsDraggingHScrollBar)
            {
                int deltaX = x - _scrollBarDragStartMouseX;
                int trackW = HScrollBarTrackRect.Width;
                int thumbW = HScrollBarThumbRect.Width;
                int availTravel = trackW - thumbW;
                if (availTravel > 0)
                {
                    GetScrollLimits(viewW, viewH, out float minX, out float maxX, out float minY, out float maxY);
                    float scrollSpan = maxX - minX;
                    PanX = _scrollBarDragStartPanX - (deltaX / (float)availTravel) * scrollSpan;
                    ClampPan(viewW, viewH);
                }
                return;
            }

            if (IsDraggingVScrollBar)
            {
                int deltaY = y - _scrollBarDragStartMouseY;
                int trackH = VScrollBarTrackRect.Height;
                int thumbH = VScrollBarThumbRect.Height;
                int availTravel = trackH - thumbH;
                if (availTravel > 0)
                {
                    GetScrollLimits(viewW, viewH, out float minX, out float maxX, out float minY, out float maxY);
                    float scrollSpan = maxY - minY;
                    PanY = _scrollBarDragStartPanY - (deltaY / (float)availTravel) * scrollSpan;
                    ClampPan(viewW, viewH);
                }
                return;
            }

            IsHoverHScrollBarThumb = !HScrollBarThumbRect.IsEmpty && HScrollBarThumbRect.Contains(x, y);
            IsHoverVScrollBarThumb = !VScrollBarThumbRect.IsEmpty && VScrollBarThumbRect.Contains(x, y);

            if (_isDraggingBrushSizeSlider)
            {
                UpdateBrushSizeFromSlider(x);
                return;
            }
            if (_isDraggingRadiusSlider)
            {
                UpdateRadiusFromSlider(x);
                return;
            }
            if (_isDraggingFontSizeSlider)
            {
                UpdateFontSizeFromSlider(x);
                return;
            }

            PointF canvasPt = ScreenToCanvas(x, y, viewW, viewH);
            CurrentMouseCanvasPoint = canvasPt;
            _currentDragPt = canvasPt;
            _penHoverPt = canvasPt;

            if (_isDraggingRulerGuideH)
            {
                _currentDraggingGuidePos = canvasPt.Y;
                return;
            }
            if (_isDraggingRulerGuideV)
            {
                _currentDraggingGuidePos = canvasPt.X;
                return;
            }

            if (IsPanning)
            {
                PanX += (x - _lastPanPoint.X);
                PanY += (y - _lastPanPoint.Y);
                _lastPanPoint = new Point(x, y);
                return;
            }

            if (SelectedTool == PhotoTool.NodeTool && _isNodeToolDragging)
            {
                HandleNodeToolMouseMove(canvasPt, x, y, button, modifiers, viewW, viewH);
                return;
            }

            if (_isDrawing && button == MouseButtons.Left)
            {
                if (SelectedTool == PhotoTool.Pen && _isPenDraggingHandle && _activeDragNode != null)
                {
                    float dx = canvasPt.X - _dragStartPt.X;
                    float dy = canvasPt.Y - _dragStartPt.Y;
                    if (Math.Abs(dx) > 1f / Zoom || Math.Abs(dy) > 1f / Zoom)
                    {
                        _activeDragNode.OutHandle = canvasPt;
                        _activeDragNode.HasOutHandle = true;
                        _activeDragNode.InHandle = new PointF(2 * _activeDragNode.Point.X - canvasPt.X, 2 * _activeDragNode.Point.Y - canvasPt.Y);
                        _activeDragNode.HasInHandle = true;
                        _activeDragNode.NodeType = VectorNodeType.Smooth;
                        if (Document.ActiveLayer != null) VectorPathModel.UpdateLayerBounds(Document.ActiveLayer);
                    }
                    return;
                }

                if (SelectedTool == PhotoTool.Brush || SelectedTool == PhotoTool.Eraser)
                {
                    PaintBrushLine(_lastBrushPt, canvasPt);
                    _lastBrushPt = canvasPt;
                }
                else if (Document.ActiveLayer != null && (_selectedTransformHandle == 10 || _selectedTransformHandle == 11))
                {
                    var layer = Document.ActiveLayer;
                    RectangleF b = _dragInitialBounds;
                    float maxR = Math.Min(b.Width, b.Height) / 2f;

                    PointF center = new PointF(b.X + b.Width / 2f, b.Y + b.Height / 2f);
                    PointF localPt = canvasPt;
                    if (layer.Rotation != 0f)
                    {
                        float rad = -layer.Rotation * (float)Math.PI / 180f;
                        float dX = canvasPt.X - center.X;
                        float dY = canvasPt.Y - center.Y;
                        localPt = new PointF(
                            center.X + (float)(dX * Math.Cos(rad) - dY * Math.Sin(rad)),
                            center.Y + (float)(dX * Math.Sin(rad) + dY * Math.Cos(rad))
                        );
                    }

                    float newR = 0f;
                    if (_selectedTransformHandle == 10)
                    {
                        // Kéo dọc theo cạnh phải (Ry) - Chuẩn Inkscape
                        float localY = localPt.Y - b.Top;
                        newR = Math.Max(0f, Math.Min(maxR, localY));
                    }
                    else
                    {
                        // Kéo ngang theo cạnh trên (Rx) - Chuẩn Inkscape
                        float localX = b.Right - localPt.X;
                        newR = Math.Max(0f, Math.Min(maxR, localX));
                    }

                    layer.CornerRadius = (float)Math.Round(newR, 1);
                    ShapeCornerRadius = layer.CornerRadius;
                    if (layer.CornerRadius > 0)
                        layer.ShapeType = VectorShapeType.RoundedRect;
                    else
                        layer.ShapeType = VectorShapeType.Rectangle;
                }
                else if (Document.ActiveLayer != null && _selectedTransformHandle >= 0)
                {
                    float dx = canvasPt.X - _dragStartPt.X;
                    float dy = canvasPt.Y - _dragStartPt.Y;
                    RectangleF b = _dragInitialBounds;

                    if (_selectedTransformHandle == 9) // Move
                    {
                        if (SnapToGrid && GridSize > 0f)
                        {
                            float targetX = (float)Math.Round((b.X + dx) / GridSize) * GridSize;
                            float targetY = (float)Math.Round((b.Y + dy) / GridSize) * GridSize;
                            dx = targetX - b.X;
                            dy = targetY - b.Y;
                        }

                        _activeSmartGuides.Clear();
                        if (SnapToObjects)
                        {
                            float snapTol = 6f / Math.Max(0.1f, Zoom);
                            RectangleF movingBox = new RectangleF(b.X + dx, b.Y + dy, b.Width, b.Height);
                            float[] testX = new float[] { movingBox.Left, movingBox.Left + movingBox.Width / 2f, movingBox.Right };
                            float[] testY = new float[] { movingBox.Top, movingBox.Top + movingBox.Height / 2f, movingBox.Bottom };

                            var targetXList = new List<float> { 0f, Document.Width / 2f, Document.Width };
                            var targetYList = new List<float> { 0f, Document.Height / 2f, Document.Height };

                            foreach (var lyr in Document.Layers)
                            {
                                if (!lyr.IsVisible || Document.SelectedLayers.Contains(lyr)) continue;
                                targetXList.Add(lyr.Bounds.Left);
                                targetXList.Add(lyr.Bounds.Left + lyr.Bounds.Width / 2f);
                                targetXList.Add(lyr.Bounds.Right);
                                targetYList.Add(lyr.Bounds.Top);
                                targetYList.Add(lyr.Bounds.Top + lyr.Bounds.Height / 2f);
                                targetYList.Add(lyr.Bounds.Bottom);
                            }

                            // Best X snap
                            float bestDiffX = float.MaxValue;
                            float snapDeltaX = 0f;
                            float guideX = 0f;
                            foreach (float tx in testX)
                            {
                                foreach (float target in targetXList)
                                {
                                    float diff = Math.Abs(tx - target);
                                    if (diff < snapTol && diff < bestDiffX)
                                    {
                                        bestDiffX = diff;
                                        snapDeltaX = target - tx;
                                        guideX = target;
                                    }
                                }
                            }
                            if (bestDiffX < snapTol)
                            {
                                dx += snapDeltaX;
                                _activeSmartGuides.Add((new PointF(guideX, 0f), new PointF(guideX, Document.Height), Color.FromArgb(236, 72, 153)));
                            }

                            // Best Y snap
                            float bestDiffY = float.MaxValue;
                            float snapDeltaY = 0f;
                            float guideY = 0f;
                            foreach (float ty in testY)
                            {
                                foreach (float target in targetYList)
                                {
                                    float diff = Math.Abs(ty - target);
                                    if (diff < snapTol && diff < bestDiffY)
                                    {
                                        bestDiffY = diff;
                                        snapDeltaY = target - ty;
                                        guideY = target;
                                    }
                                }
                            }
                            if (bestDiffY < snapTol)
                            {
                                dy += snapDeltaY;
                                _activeSmartGuides.Add((new PointF(0f, guideY), new PointF(Document.Width, guideY), Color.FromArgb(236, 72, 153)));
                            }
                        }

                        if (Document.SelectedLayers.Count > 1)
                        {
                            foreach (var sel in Document.SelectedLayers)
                            {
                                if (_multiDragInitialBounds.TryGetValue(sel, out var origB))
                                {
                                    sel.Bounds = new RectangleF(origB.X + dx, origB.Y + dy, origB.Width, origB.Height);
                                }
                                if (_multiDragInitialPoints.TryGetValue(sel, out var origPts) && sel.PathPoints != null && sel.PathPoints.Count == origPts.Count)
                                {
                                    for (int pIdx = 0; pIdx < origPts.Count; pIdx++)
                                    {
                                        sel.PathPoints[pIdx] = new PointF(origPts[pIdx].X + dx, origPts[pIdx].Y + dy);
                                    }
                                }
                                if (_multiDragInitialSubPaths.TryGetValue(sel, out var origSubs) && sel.SubPaths != null && sel.SubPaths.Count == origSubs.Count)
                                {
                                    for (int spIdx = 0; spIdx < origSubs.Count; spIdx++)
                                    {
                                        var initSp = origSubs[spIdx];
                                        var curSp = sel.SubPaths[spIdx];
                                        if (initSp.Nodes != null && curSp.Nodes != null && initSp.Nodes.Count == curSp.Nodes.Count)
                                        {
                                            for (int nIdx = 0; nIdx < initSp.Nodes.Count; nIdx++)
                                            {
                                                var initN = initSp.Nodes[nIdx];
                                                var curN = curSp.Nodes[nIdx];
                                                curN.Point = new PointF(initN.Point.X + dx, initN.Point.Y + dy);
                                                curN.InHandle = new PointF(initN.InHandle.X + dx, initN.InHandle.Y + dy);
                                                curN.OutHandle = new PointF(initN.OutHandle.X + dx, initN.OutHandle.Y + dy);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Document.ActiveLayer.Bounds = new RectangleF(b.X + dx, b.Y + dy, b.Width, b.Height);
                            if (_dragInitialPathPoints != null && Document.ActiveLayer.PathPoints != null && Document.ActiveLayer.PathPoints.Count == _dragInitialPathPoints.Count)
                            {
                                for (int pIdx = 0; pIdx < _dragInitialPathPoints.Count; pIdx++)
                                {
                                    Document.ActiveLayer.PathPoints[pIdx] = new PointF(_dragInitialPathPoints[pIdx].X + dx, _dragInitialPathPoints[pIdx].Y + dy);
                                }
                            }
                            if (_dragInitialSubPaths != null && Document.ActiveLayer.SubPaths != null && Document.ActiveLayer.SubPaths.Count == _dragInitialSubPaths.Count)
                            {
                                for (int spIdx = 0; spIdx < _dragInitialSubPaths.Count; spIdx++)
                                {
                                    var initSp = _dragInitialSubPaths[spIdx];
                                    var curSp = Document.ActiveLayer.SubPaths[spIdx];
                                    if (initSp.Nodes != null && curSp.Nodes != null && initSp.Nodes.Count == curSp.Nodes.Count)
                                    {
                                        for (int nIdx = 0; nIdx < initSp.Nodes.Count; nIdx++)
                                        {
                                            var initN = initSp.Nodes[nIdx];
                                            var curN = curSp.Nodes[nIdx];
                                            curN.Point = new PointF(initN.Point.X + dx, initN.Point.Y + dy);
                                            curN.InHandle = new PointF(initN.InHandle.X + dx, initN.InHandle.Y + dy);
                                            curN.OutHandle = new PointF(initN.OutHandle.X + dx, initN.OutHandle.Y + dy);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (_selectedTransformHandle == 8) // Rotate
                    {
                        PointF center = new PointF(b.X + b.Width / 2f, b.Y + b.Height / 2f);
                        float angleStart = (float)Math.Atan2(_dragStartPt.Y - center.Y, _dragStartPt.X - center.X);
                        float angleEnd = (float)Math.Atan2(canvasPt.Y - center.Y, canvasPt.X - center.X);
                        float angleDiff = (angleEnd - angleStart) * 180f / (float)Math.PI;

                        if (Document.SelectedLayers.Count > 1)
                        {
                            float rad = angleDiff * (float)Math.PI / 180f;
                            float cosR = (float)Math.Cos(rad);
                            float sinR = (float)Math.Sin(rad);

                            foreach (var sel in Document.SelectedLayers)
                            {
                                if (_multiDragInitialBounds.TryGetValue(sel, out var origB) &&
                                    _multiDragInitialRotations.TryGetValue(sel, out var origRot))
                                {
                                    PointF lyrCenter0 = new PointF(origB.X + origB.Width / 2f, origB.Y + origB.Height / 2f);
                                    float dcX = lyrCenter0.X - center.X;
                                    float dcY = lyrCenter0.Y - center.Y;
                                    float newCenterCx = center.X + dcX * cosR - dcY * sinR;
                                    float newCenterCy = center.Y + dcX * sinR + dcY * cosR;

                                    sel.Bounds = new RectangleF(newCenterCx - origB.Width / 2f, newCenterCy - origB.Height / 2f, origB.Width, origB.Height);
                                    sel.Rotation = (origRot + angleDiff + 360f) % 360f;
                                }
                            }
                        }
                        else
                        {
                            Document.ActiveLayer.Rotation = (_dragInitialRotation + angleDiff + 360f) % 360f;
                        }
                    }
                    else if (_selectedTransformHandle >= 0 && _selectedTransformHandle <= 7) // Resize
                    {
                        if (Document.ActiveLayer.Rotation != 0f && Document.SelectedLayers.Count <= 1)
                        {
                            float angle = -Document.ActiveLayer.Rotation * (float)Math.PI / 180f;
                            float localDx = (float)(dx * Math.Cos(angle) - dy * Math.Sin(angle));
                            float localDy = (float)(dx * Math.Sin(angle) + dy * Math.Cos(angle));
                            dx = localDx;
                            dy = localDy;
                        }
                        float newX = b.X, newY = b.Y, newW = b.Width, newH = b.Height;
                        if (_selectedTransformHandle == 0 || _selectedTransformHandle == 6 || _selectedTransformHandle == 7) // Left
                        {
                            newX = b.X + dx;
                            newW = b.Width - dx;
                        }
                        if (_selectedTransformHandle == 0 || _selectedTransformHandle == 1 || _selectedTransformHandle == 2) // Top
                        {
                            newY = b.Y + dy;
                            newH = b.Height - dy;
                        }
                        if (_selectedTransformHandle == 2 || _selectedTransformHandle == 3 || _selectedTransformHandle == 4) // Right
                        {
                            newW = b.Width + dx;
                        }
                        if (_selectedTransformHandle == 4 || _selectedTransformHandle == 5 || _selectedTransformHandle == 6) // Bottom
                        {
                            newH = b.Height + dy;
                        }
                        
                        if (newW < 2f) { newX = b.X + b.Width - 2f; newW = 2f; }
                        if (newH < 2f) { newY = b.Y + b.Height - 2f; newH = 2f; }

                        if (Document.SelectedLayers.Count > 1)
                        {
                            if (b.Width > 0f && b.Height > 0f)
                            {
                                float sx = newW / b.Width;
                                float sy = newH / b.Height;
                                foreach (var sel in Document.SelectedLayers)
                                {
                                    if (_multiDragInitialBounds.TryGetValue(sel, out var origB))
                                    {
                                        float relX = origB.X - b.X;
                                        float relY = origB.Y - b.Y;
                                        float lyrNewX = newX + relX * sx;
                                        float lyrNewY = newY + relY * sy;
                                        float lyrNewW = Math.Max(2f, origB.Width * sx);
                                        float lyrNewH = Math.Max(2f, origB.Height * sy);
                                        sel.Bounds = new RectangleF(lyrNewX, lyrNewY, lyrNewW, lyrNewH);

                                        if (_multiDragInitialPoints.TryGetValue(sel, out var origPts) && sel.PathPoints != null && sel.PathPoints.Count == origPts.Count)
                                        {
                                            for (int pIdx = 0; pIdx < origPts.Count; pIdx++)
                                            {
                                                sel.PathPoints[pIdx] = new PointF(
                                                    newX + (origPts[pIdx].X - b.X) * sx,
                                                    newY + (origPts[pIdx].Y - b.Y) * sy
                                                );
                                            }
                                        }

                                        if (_multiDragInitialSubPaths.TryGetValue(sel, out var origSubs) && sel.SubPaths != null && sel.SubPaths.Count == origSubs.Count)
                                        {
                                            for (int spIdx = 0; spIdx < origSubs.Count; spIdx++)
                                            {
                                                var initSp = origSubs[spIdx];
                                                var curSp = sel.SubPaths[spIdx];
                                                if (initSp.Nodes != null && curSp.Nodes != null && initSp.Nodes.Count == curSp.Nodes.Count)
                                                {
                                                    for (int nIdx = 0; nIdx < initSp.Nodes.Count; nIdx++)
                                                    {
                                                        var initN = initSp.Nodes[nIdx];
                                                        var curN = curSp.Nodes[nIdx];
                                                        curN.Point = new PointF(newX + (initN.Point.X - b.X) * sx, newY + (initN.Point.Y - b.Y) * sy);
                                                        curN.InHandle = new PointF(newX + (initN.InHandle.X - b.X) * sx, newY + (initN.InHandle.Y - b.Y) * sy);
                                                        curN.OutHandle = new PointF(newX + (initN.OutHandle.X - b.X) * sx, newY + (initN.OutHandle.Y - b.Y) * sy);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Document.ActiveLayer.Bounds = new RectangleF(newX, newY, newW, newH);

                            if (b.Width > 0f && b.Height > 0f)
                            {
                                float sx = newW / b.Width;
                                float sy = newH / b.Height;
                                if (_dragInitialPathPoints != null && Document.ActiveLayer.PathPoints != null && Document.ActiveLayer.PathPoints.Count == _dragInitialPathPoints.Count)
                                {
                                    for (int pIdx = 0; pIdx < _dragInitialPathPoints.Count; pIdx++)
                                    {
                                        float relX = _dragInitialPathPoints[pIdx].X - b.X;
                                        float relY = _dragInitialPathPoints[pIdx].Y - b.Y;
                                        Document.ActiveLayer.PathPoints[pIdx] = new PointF(newX + relX * sx, newY + relY * sy);
                                    }
                                }
                                if (_dragInitialSubPaths != null && Document.ActiveLayer.SubPaths != null && Document.ActiveLayer.SubPaths.Count == _dragInitialSubPaths.Count)
                                {
                                    for (int spIdx = 0; spIdx < _dragInitialSubPaths.Count; spIdx++)
                                    {
                                        var initSp = _dragInitialSubPaths[spIdx];
                                        var curSp = Document.ActiveLayer.SubPaths[spIdx];
                                        if (initSp.Nodes != null && curSp.Nodes != null && initSp.Nodes.Count == curSp.Nodes.Count)
                                        {
                                            for (int nIdx = 0; nIdx < initSp.Nodes.Count; nIdx++)
                                            {
                                                var initN = initSp.Nodes[nIdx];
                                                var curN = curSp.Nodes[nIdx];
                                                curN.Point = new PointF(newX + (initN.Point.X - b.X) * sx, newY + (initN.Point.Y - b.Y) * sy);
                                                curN.InHandle = new PointF(newX + (initN.InHandle.X - b.X) * sx, newY + (initN.InHandle.Y - b.Y) * sy);
                                                curN.OutHandle = new PointF(newX + (initN.OutHandle.X - b.X) * sx, newY + (initN.OutHandle.Y - b.Y) * sy);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void HandleMouseUp(int x, int y, MouseButtons button, int viewW, int viewH)
        {
            if (IsDraggingHScrollBar || IsDraggingVScrollBar)
            {
                IsDraggingHScrollBar = false;
                IsDraggingVScrollBar = false;
                return;
            }

            if (_isPenDraggingHandle)
            {
                _isPenDraggingHandle = false;
                _activeDragNode = null;
                return;
            }

            if (_isDraggingBrushSizeSlider || _isDraggingRadiusSlider || _isDraggingFontSizeSlider)
            {
                _isDraggingBrushSizeSlider = false;
                _isDraggingRadiusSlider = false;
                _isDraggingFontSizeSlider = false;
                return;
            }

            if (_isDraggingRulerGuideH)
            {
                PointF pt = ScreenToCanvas(x, y, viewW, viewH);
                if (pt.Y >= 0 && pt.Y <= Document.Height)
                {
                    HorizontalGuides.Add((float)Math.Round(pt.Y));
                }
                _isDraggingRulerGuideH = false;
                return;
            }
            if (_isDraggingRulerGuideV)
            {
                PointF pt = ScreenToCanvas(x, y, viewW, viewH);
                if (pt.X >= 0 && pt.X <= Document.Width)
                {
                    VerticalGuides.Add((float)Math.Round(pt.X));
                }
                _isDraggingRulerGuideV = false;
                return;
            }

            if (IsPanning)
            {
                IsPanning = false;
                return;
            }

            if (SelectedTool == PhotoTool.NodeTool && _isNodeToolDragging)
            {
                PointF canvasPt = ScreenToCanvas(x, y, viewW, viewH);
                HandleNodeToolMouseUp(canvasPt, x, y, button, viewW, viewH);
                return;
            }

            if (_isDrawing)
            {
                if (_selectedTransformHandle >= 0)
                {
                    _selectedTransformHandle = -1;
                    _isDrawing = false;
                    _multiDragInitialBounds.Clear();
                    _multiDragInitialRotations.Clear();
                    _multiDragInitialPoints.Clear();
                    _multiDragInitialSubPaths.Clear();
                    _activeSmartGuides.Clear();
                    return;
                }
                PointF canvasPt = ScreenToCanvas(x, y, viewW, viewH);
                if (IsShapeTool(SelectedTool))
                {
                    RectangleF finalBounds = NormalizeRect(_dragStartPt, canvasPt);
                    if (finalBounds.Width <= 3 && finalBounds.Height <= 3)
                    {
                        float defSize = 120f;
                        finalBounds = new RectangleF(canvasPt.X - defSize / 2f, canvasPt.Y - defSize / 2f, defSize, defSize);
                    }
                    VectorShapeType pType = GetShapeTypeForTool(SelectedTool);
                    Document.AddVectorShape(
                        pType,
                        finalBounds,
                        ShapeFillColor,
                        ShapeStrokeColor,
                        ShapeStrokeWidth,
                        ShapeCornerRadius,
                        ActiveShapePointCount,
                        ShapeFillType,
                        ShapeFillColor2,
                        ShapeFillGradientAngle,
                        ShapeStrokeType,
                        ShapeStrokeDashStyle,
                        ShapeStrokeLineJoin,
                        ShapeStrokeLineCap,
                        ShapeGradientStops);
                }
                else if (SelectedTool == PhotoTool.Gradient)
                {
                    float dx = canvasPt.X - _dragStartPt.X;
                    float dy = canvasPt.Y - _dragStartPt.Y;
                    if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
                    {
                        float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
                        if (angle < 0) angle += 360f;
                        ShapeFillGradientAngle = angle;
                        if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
                        {
                            Document.SaveUndoSnapshot();
                            Document.ActiveLayer.FillGradientAngle = angle;
                            if (Document.ActiveLayer.FillType != VectorFillType.LinearGradient && 
                                Document.ActiveLayer.FillType != VectorFillType.RadialGradient)
                            {
                                Document.ActiveLayer.FillType = VectorFillType.LinearGradient;
                            }
                        }
                    }
                }
                else if (SelectedTool == PhotoTool.Marquee)
                {
                    RectangleF marqueeBounds = NormalizeRect(_dragStartPt, canvasPt);
                    if (marqueeBounds.Width > 5 && marqueeBounds.Height > 5)
                    {
                        Document.ActiveLayerIndex = -1;
                        for (int i = Document.Layers.Count - 1; i >= 0; i--)
                        {
                            if (Document.Layers[i].Bounds.IntersectsWith(marqueeBounds) && Document.Layers[i].Name != "Lớp nền")
                            {
                                Document.ActiveLayerIndex = i;
                                break;
                            }
                        }
                    }
                    SelectedTool = PhotoTool.Move;
                }
                _isDrawing = false;
            }
        }

        public void ActivateNodeTool()
        {
            SelectedTool = PhotoTool.NodeTool;
            if (Document != null && Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
            {
                Document.ActiveLayer.EnsureNodesInitialized();
                if (Document.ActiveLayer.SubPaths != null)
                {
                    foreach (var sp in Document.ActiveLayer.SubPaths)
                    {
                        if (sp.Nodes != null)
                        {
                            foreach (var n in sp.Nodes) n.IsSelected = false;
                        }
                    }
                }
            }
        }

        public void HandleDoubleClick(int x, int y, MouseButtons button, Keys modifiers, int viewW, int viewH)
        {
            PointF canvasPt = ScreenToCanvas(x, y, viewW, viewH);
            float tol = 12f / Math.Max(0.1f, Zoom);

            if (SelectedTool == PhotoTool.NodeTool)
            {
                if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.VectorShape)
                {
                    var layer = Document.ActiveLayer;
                    layer.EnsureNodesInitialized();

                    // 1. Double click vào điểm neo -> XÓA ĐIỂM NEO ĐÓ
                    if (layer.SubPaths != null)
                    {
                        foreach (var sp in layer.SubPaths)
                        {
                            if (sp.Nodes == null) continue;
                            for (int i = 0; i < sp.Nodes.Count; i++)
                            {
                                var n = sp.Nodes[i];
                                if (Math.Abs(canvasPt.X - n.Point.X) <= tol && Math.Abs(canvasPt.Y - n.Point.Y) <= tol)
                                {
                                    Document.SaveUndoSnapshot();
                                    foreach (var s in layer.SubPaths) { if (s.Nodes != null) foreach (var node in s.Nodes) node.IsSelected = false; }
                                    n.IsSelected = true;
                                    VectorPathModel.DeleteSelectedNodes(layer);
                                    return;
                                }
                            }
                        }
                    }

                    // 2. Double click vào phân đoạn đường vẽ -> CHÈN ĐIỂM NEO MỚI TẠI VỊ TRÍ CHUỘT
                    int subIdx;
                    int segIdx = VectorPathModel.HitTestSegment(layer, canvasPt, tol * 1.5f, out subIdx);
                    if (segIdx >= 0 && subIdx >= 0 && layer.SubPaths != null && subIdx < layer.SubPaths.Count)
                    {
                        Document.SaveUndoSnapshot();
                        bool ok = VectorPathModel.InsertNodeAt(layer, canvasPt);
                        if (ok) return;
                    }
                }

                // 3. Double click vào hình khối vector khác trên canvas -> Chọn và hiện toàn bộ neo
                int otherIdx = HitTestVectorLayerAt(canvasPt, tol);
                if (otherIdx >= 0)
                {
                    Document.ActiveLayerIndex = otherIdx;
                    ActivateNodeTool();
                    return;
                }
            }
            else
            {
                // Khi đang ở công cụ Chọn hoặc công cụ khác:
                // Double click vào hình vector -> Tự động chuyển ngay sang công cụ Chỉnh neo và hiện toàn bộ neo!
                int vecIdx = HitTestVectorLayerAt(canvasPt, tol);
                if (vecIdx >= 0)
                {
                    Document.ActiveLayerIndex = vecIdx;
                    ActivateNodeTool();
                    return;
                }

                // Double click vào văn bản -> Mở hộp thoại sửa chữ
                if (Document.ActiveLayer != null && Document.ActiveLayer.Type == LayerType.Text && Document.ActiveLayer.Bounds.Contains(canvasPt))
                {
                    Document.SaveUndoSnapshot();
                    Document.ActiveLayer.TextContent = ShowTextInputDialog(Document.ActiveLayer.TextContent);
                    return;
                }
            }
        }

        public void HandleMouseWheel(int x, int y, int delta, Keys modifiers, int viewW, int viewH)
        {
            float availW = Math.Max(100f, viewW - LeftToolbarWidth - RightInspectorWidth);
            float availH = Math.Max(100f, viewH - RibbonHeight - TopOptionsBarHeight - BottomStatusHeight);

            if ((modifiers & Keys.Control) == Keys.Control)
            {
                // Stable Inkscape-style zoom centered exactly at viewport center (stable, deterministic)
                float oldZoom = Zoom;
                float zoomFactor = (delta > 0) ? 1.15f : (1f / 1.15f);
                Zoom = Math.Max(0.05f, Math.Min(32.0f, Zoom * zoomFactor));

                float vcX = availW / 2f;
                float vcY = availH / 2f;
                PanX = vcX - (vcX - PanX) * (Zoom / oldZoom);
                PanY = vcY - (vcY - PanY) * (Zoom / oldZoom);
                ClampPan(viewW, viewH);
            }
            else if ((modifiers & Keys.Shift) == Keys.Shift)
            {
                // Standard horizontal scroll / pan (clamped)
                PanX += (delta > 0 ? 90f : -90f);
                ClampPan(viewW, viewH);
            }
            else
            {
                // Standard vertical scroll / pan (clamped)
                PanY += (delta > 0 ? 90f : -90f);
                ClampPan(viewW, viewH);
            }
        }

        public Cursor GetCursorAtPoint(int x, int y, int viewW, int viewH)
        {
            if (IsDraggingHScrollBar || IsDraggingVScrollBar ||
                (!HScrollBarThumbRect.IsEmpty && HScrollBarThumbRect.Contains(x, y)) ||
                (!VScrollBarThumbRect.IsEmpty && VScrollBarThumbRect.Contains(x, y)))
            {
                return Cursors.Default;
            }

            // If in toolbar, inspector, top option bar, or status bar -> Default
            if (x < 56 || x > viewW - RightInspectorWidth || y < RibbonHeight + TopOptionsBarHeight || y > viewH - BottomStatusHeight)
            {
                return Cursors.Default;
            }

            if (IsPanning || SelectedTool == PhotoTool.Hand || (Control.ModifierKeys & Keys.Space) == Keys.Space || IsSpaceHeld)
            {
                return Cursors.Hand;
            }

            // Check transform handles if an object is selected
            if (Document != null && Document.ActiveLayer != null && (SelectedTool == PhotoTool.Move || IsShapeTool(SelectedTool)))
            {
                int hit = CheckTransformHandles(x, y, viewW, viewH);
                switch (hit)
                {
                    case 0: return Cursors.SizeNWSE; // Top-Left
                    case 1: return Cursors.SizeNS;   // Top-Mid
                    case 2: return Cursors.SizeNESW; // Top-Right
                    case 3: return Cursors.SizeWE;   // Mid-Right
                    case 4: return Cursors.SizeNWSE; // Bottom-Right
                    case 5: return Cursors.SizeNS;   // Bottom-Mid
                    case 6: return Cursors.SizeNESW; // Bottom-Left
                    case 7: return Cursors.SizeWE;   // Mid-Left
                    case 8: return Cursors.Hand;     // Rotate handle
                    case 9: return (SelectedTool == PhotoTool.Move) ? Cursors.SizeAll : Cursors.Default;  // Move body
                    case 10: return Cursors.SizeNS;  // Round pin Y
                    case 11: return Cursors.SizeWE;  // Round pin X
                }
            }

            switch (SelectedTool)
            {
                case PhotoTool.Rectangle:
                case PhotoTool.Ellipse:
                case PhotoTool.Star:
                case PhotoTool.Shape:
                case PhotoTool.Pen:
                case PhotoTool.Brush:
                case PhotoTool.Eraser:
                case PhotoTool.Marquee:
                case PhotoTool.Crop:
                case PhotoTool.Gradient:
                    return Cursors.Cross;
                case PhotoTool.Text:
                    return Cursors.IBeam;
                case PhotoTool.Eyedropper:
                case PhotoTool.Bucket:
                case PhotoTool.Hand:
                    return Cursors.Hand;
                case PhotoTool.NodeTool:
                    return Cursors.Cross;
                case PhotoTool.Move:
                default:
                    return Cursors.Default;
            }
        }

        public void HandleKeyDown(Keys key, Keys modifiers)
        {
            if (Document == null) return;

            if ((modifiers & Keys.Control) == Keys.Control)
            {
                if (key == Keys.Z) Document.Undo();
                else if (key == Keys.Y) Document.Redo();
                else if (key == Keys.C) Document.CopySelected();
                else if (key == Keys.X) Document.CutSelected();
                else if (key == Keys.V) Document.Paste();
                else if (key == Keys.D)
                {
                    if (IsCropping) { IsCropping = false; CropRect = RectangleF.Empty; }
                    else if (Document.SelectedLayers.Count > 1) { Document.SelectedLayerIds.Clear(); }
                    else Document.DuplicateSelectedLayers();
                }
                else if (key == Keys.J) Document.DuplicateSelectedLayers();
                else if (key == Keys.G)
                {
                    if ((modifiers & Keys.Shift) == Keys.Shift) Document.UngroupSelectedLayers();
                    else Document.GroupSelectedLayers();
                }
                else if (key == Keys.U) Document.UngroupSelectedLayers();
                else if (key == Keys.T) SelectedTool = PhotoTool.Move;
                else if (key == Keys.R) ShowRulers = !ShowRulers;
                else if (key == Keys.D0 || key == Keys.NumPad0) CenterCanvas(1920, 1080);
                else if (key == Keys.D1 || key == Keys.NumPad1) Zoom = 1.0f;
                else if (key == Keys.OemOpenBrackets)
                {
                    if ((modifiers & Keys.Shift) == Keys.Shift) Document.SendActiveLayerToBack();
                    else Document.MoveLayerDown();
                }
                else if (key == Keys.OemCloseBrackets)
                {
                    if ((modifiers & Keys.Shift) == Keys.Shift) Document.BringActiveLayerToFront();
                    else Document.MoveLayerUp();
                }
                else if ((modifiers & Keys.Shift) == Keys.Shift && (key == Keys.D9 || key == Keys.OemOpenBrackets)) Document.InsetActivePath();
                else if ((modifiers & Keys.Shift) == Keys.Shift && (key == Keys.D0 || key == Keys.OemCloseBrackets)) Document.OutsetActivePath();
                else if (key == Keys.Oemcomma) Document.InsetActivePath();
                else if (key == Keys.OemPeriod) Document.OutsetActivePath();
                else if (key == Keys.E) Document.MergeDown();
                else if (key == Keys.A)
                {
                    if (SelectedTool == PhotoTool.NodeTool && Document.ActiveLayer != null && Document.ActiveLayer.SubPaths != null)
                    {
                        foreach (var sp in Document.ActiveLayer.SubPaths)
                        {
                            if (sp.Nodes != null)
                            {
                                foreach (var n in sp.Nodes) n.IsSelected = true;
                            }
                        }
                    }
                    else
                    {
                        Document.SelectAllLayers();
                    }
                }
                // Inkscape Đại số đường vẽ (Path Booleans) hoặc Thu phóng
                else if (key == Keys.Oemplus || key == Keys.Add)
                {
                    if ((modifiers & Keys.Shift) == 0 && (modifiers & Keys.Alt) == 0 && Document.SelectedLayers.Count <= 1)
                    {
                        Zoom = Math.Min(32.0f, Zoom * 1.25f);
                    }
                    else
                    {
                        Document.ApplyBooleanOperation(BooleanOpType.Union);
                    }
                }
                else if (key == Keys.OemMinus || key == Keys.Subtract)
                {
                    if ((modifiers & Keys.Shift) == 0 && (modifiers & Keys.Alt) == 0 && Document.SelectedLayers.Count <= 1)
                    {
                        Zoom = Math.Max(0.05f, Zoom / 1.25f);
                    }
                    else
                    {
                        Document.ApplyBooleanOperation(BooleanOpType.Difference);
                    }
                }
                else if ((modifiers & Keys.Shift) == Keys.Shift && (key == Keys.D8 || key == Keys.Multiply))
                {
                    Document.ApplyBooleanOperation(BooleanOpType.Intersection);
                }
                else if ((modifiers & Keys.Shift) == Keys.Shift && key == Keys.D6)
                {
                    Document.ApplyBooleanOperation(BooleanOpType.Exclusion);
                }
                else if (key == Keys.Oem2 || key == Keys.Divide || key == Keys.OemQuestion)
                {
                    Document.ApplyBooleanOperation(BooleanOpType.Division);
                }
                else if (key == Keys.K)
                {
                    if ((modifiers & Keys.Shift) == Keys.Shift) Document.BreakApartActivePath();
                    else Document.CombineSelectedPaths();
                }
                else if (key == Keys.L) Document.SimplifyActivePath();
                else if (key == Keys.OemQuotes || key == Keys.Oem7) ShowGrid = !ShowGrid;
                else if ((modifiers & Keys.Shift) == Keys.Shift && key == Keys.C) Document.ObjectToPathActive();
                else if ((modifiers & Keys.Alt) == Keys.Alt && key == Keys.C) Document.StrokeToPathActive();
            }
            else
            {
                // Phím chuyển đổi bắt dính Inkscape (%)
                if ((modifiers & Keys.Shift) == Keys.Shift && key == Keys.D5) SnapToObjects = !SnapToObjects;
                // Phím xếp lớp Inkscape (Z-order)
                else if (key == Keys.Home) Document.BringActiveLayerToFront();
                else if (key == Keys.End) Document.SendActiveLayerToBack();
                else if (key == Keys.PageUp) Document.MoveLayerUp();
                else if (key == Keys.PageDown) Document.MoveLayerDown();
                // Phím Lật Inkscape (Flip)
                else if (key == Keys.H && SelectedTool != PhotoTool.Text) Document.FlipActive(true);
                else if ((modifiers & Keys.Shift) == Keys.Shift && key == Keys.V && SelectedTool != PhotoTool.Text) Document.FlipActive(false);
                // Các công cụ tiêu chuẩn Photoshop/Illustrator
                else if (key == Keys.V) SelectedTool = PhotoTool.Move;
                else if (key == Keys.A) ActivateNodeTool();
                else if (key == Keys.M) SelectedTool = PhotoTool.Marquee;
                else if (key == Keys.B) SelectedTool = PhotoTool.Brush;
                else if (key == Keys.E) SelectedTool = PhotoTool.Eraser;
                else if (key == Keys.N) ActivateNodeTool();
                else if (key == Keys.G) SelectedTool = PhotoTool.Bucket;
                else if (key == Keys.T) SelectedTool = PhotoTool.Text;
                else if (key == Keys.U) SelectedTool = PhotoTool.Shape;
                else if (key == Keys.I) SelectedTool = PhotoTool.Eyedropper;
                else if (key == Keys.C)
                {
                    SelectedTool = PhotoTool.Crop;
                    IsCropping = true;
                    CropRect = new RectangleF(Document.Width * 0.05f, Document.Height * 0.05f, Document.Width * 0.9f, Document.Height * 0.9f);
                }
                else if (key == Keys.P) SelectedTool = PhotoTool.Pen;
                else if (key == Keys.H) SelectedTool = PhotoTool.Hand;
                else if (key == Keys.Z) Zoom = Math.Min(32.0f, Zoom * 1.25f);
                // Dịch chuyển Nudge bằng phím mũi tên (1px mặc định, 10px nếu giữ Shift)
                else if (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down)
                {
                    float step = ((modifiers & Keys.Shift) == Keys.Shift) ? 10f : 1f;
                    float dx = (key == Keys.Left) ? -step : (key == Keys.Right ? step : 0f);
                    float dy = (key == Keys.Up) ? -step : (key == Keys.Down ? step : 0f);

                    if (SelectedTool == PhotoTool.NodeTool && Document.ActiveLayer != null && Document.ActiveLayer.SubPaths != null)
                    {
                        Document.SaveUndoSnapshot();
                        foreach (var sp in Document.ActiveLayer.SubPaths)
                        {
                            if (sp.Nodes != null)
                            {
                                foreach (var n in sp.Nodes)
                                {
                                    if (n.IsSelected)
                                    {
                                        n.Point = new PointF(n.Point.X + dx, n.Point.Y + dy);
                                        n.InHandle = new PointF(n.InHandle.X + dx, n.InHandle.Y + dy);
                                        n.OutHandle = new PointF(n.OutHandle.X + dx, n.OutHandle.Y + dy);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var selected = Document.SelectedLayers;
                        if (selected.Count == 0 && Document.ActiveLayer != null) selected = new List<PhotoLayer> { Document.ActiveLayer };
                        if (selected.Count > 0)
                        {
                            Document.SaveUndoSnapshot();
                            foreach (var l in selected)
                            {
                                var b = l.Bounds;
                                l.Bounds = new RectangleF(b.X + dx, b.Y + dy, b.Width, b.Height);
                                if (l.SubPaths != null)
                                {
                                    foreach (var sp in l.SubPaths)
                                    {
                                        if (sp.Nodes != null)
                                        {
                                            foreach (var n in sp.Nodes)
                                            {
                                                n.Point = new PointF(n.Point.X + dx, n.Point.Y + dy);
                                                n.InHandle = new PointF(n.InHandle.X + dx, n.InHandle.Y + dy);
                                                n.OutHandle = new PointF(n.OutHandle.X + dx, n.OutHandle.Y + dy);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (key == Keys.Delete || key == Keys.Back)
                {
                    bool hasSelectedNode = false;
                    if (SelectedTool == PhotoTool.NodeTool && Document.ActiveLayer != null && Document.ActiveLayer.SubPaths != null)
                    {
                        foreach (var sp in Document.ActiveLayer.SubPaths)
                        {
                            if (sp.Nodes != null && sp.Nodes.Exists(n => n.IsSelected))
                            {
                                hasSelectedNode = true;
                                break;
                            }
                        }
                    }

                    if (hasSelectedNode)
                    {
                        Document.SaveUndoSnapshot();
                        VectorPathModel.DeleteSelectedNodes(Document.ActiveLayer);
                        return;
                    }
                    else
                    {
                        Document.DeleteActiveLayer();
                    }
                }
                else if (key == Keys.Escape)
                {
                    if (IsCropping)
                    {
                        IsCropping = false;
                        CropRect = RectangleF.Empty;
                    }
                    else if (SelectedTool == PhotoTool.NodeTool && Document.ActiveLayer != null && Document.ActiveLayer.SubPaths != null)
                    {
                        foreach (var sp in Document.ActiveLayer.SubPaths)
                        {
                            if (sp.Nodes != null)
                            {
                                foreach (var n in sp.Nodes) n.IsSelected = false;
                            }
                        }
                    }
                    else
                    {
                        Document.SelectedLayerIds.Clear();
                    }
                }
                else if (key == Keys.Oem4) { BrushSize = Math.Max(1f, BrushSize - 5f); ActiveFontSize = Math.Max(8f, ActiveFontSize - 2f); } // [ (Decrease size)
                else if (key == Keys.Oem6) { BrushSize = Math.Min(500f, BrushSize + 5f); ActiveFontSize = Math.Min(200f, ActiveFontSize + 2f); } // ] (Increase size)
                else if (key == Keys.Enter && SelectedTool == PhotoTool.Crop && IsCropping)
                {
                    Document.CropCanvas(Rectangle.Round(CropRect));
                    IsCropping = false;
                }
            }
        }

                private void HandleRightInspectorMouseDown(int x, int y, int viewW, int viewH)
        {
            int inspX = viewW - RightInspectorWidth;

            // Check Tab Headers (3 Tabs: Lớp, Tô & Nét, Thuộc tính)
            if (y >= RibbonHeight && y <= RibbonHeight + 40)
            {
                int tabW = RightInspectorWidth / 3;
                InspectorActiveTab = Math.Max(0, Math.Min(2, (x - inspX) / tabW));
                return;
            }

            // Check layer selection in Tab 0
            if (InspectorActiveTab == 0)
            {
                int cardStartY = RibbonHeight + 48 + 38;
                int cardH = 50;
                for (int i = Document.Layers.Count - 1; i >= 0; i--)
                {
                    Rectangle cardRect = new Rectangle(inspX + 10, cardStartY, RightInspectorWidth - 20, cardH - 6);
                    if (cardRect.Contains(x, y))
                    {
                        Rectangle eyeRect = new Rectangle(cardRect.X + 8, cardRect.Y + 12, 20, 20);
                        if (eyeRect.Contains(x, y))
                        {
                            Document.Layers[i].IsVisible = !Document.Layers[i].IsVisible;
                        }
                        else
                        {
                            Document.ActiveLayerIndex = i;
                        }
                        return;
                    }
                    cardStartY += cardH;
                }
            }

            // Check all registered inspector clickables (buttons, swatches, presets)
            for (int i = _inspectorClickables.Count - 1; i >= 0; i--)
            {
                if (_inspectorClickables[i].Rect.Contains(x, y))
                {
                    _inspectorClickables[i].Action?.Invoke();
                    return;
                }
            }
        }

        private void PaintBrushDot(PointF pt)
        {
            if (Document.ActiveLayer == null || Document.ActiveLayer.Image == null) return;
            lock (Document.ActiveLayer.Image)
            {
                using (Graphics g = Graphics.FromImage(Document.ActiveLayer.Image))
                {
                    Color c = (SelectedTool == PhotoTool.Eraser) ? Color.Transparent : Color.FromArgb((int)(BrushOpacity * 255f), ForegroundColor);
                    if (SelectedTool == PhotoTool.Eraser)
                    {
                        g.CompositingMode = CompositingMode.SourceCopy;
                        using (SolidBrush b = new SolidBrush(Color.Transparent))
                        {
                            g.FillEllipse(b, pt.X - BrushSize / 2f, pt.Y - BrushSize / 2f, BrushSize, BrushSize);
                        }
                    }
                    else
                    {
                        using (SolidBrush b = new SolidBrush(c))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.FillEllipse(b, pt.X - BrushSize / 2f, pt.Y - BrushSize / 2f, BrushSize, BrushSize);
                        }
                    }
                }
            }
        }

        private void PaintBrushLine(PointF p1, PointF p2)
        {
            if (Document.ActiveLayer == null || Document.ActiveLayer.Image == null) return;
            lock (Document.ActiveLayer.Image)
            {
                using (Graphics g = Graphics.FromImage(Document.ActiveLayer.Image))
                {
                    if (SelectedTool == PhotoTool.Eraser)
                    {
                        g.CompositingMode = CompositingMode.SourceCopy;
                        using (Pen p = new Pen(Color.Transparent, BrushSize))
                        {
                            p.StartCap = LineCap.Round;
                            p.EndCap = LineCap.Round;
                            g.DrawLine(p, p1, p2);
                        }
                    }
                    else
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        Color c = Color.FromArgb((int)(BrushOpacity * 255f), ForegroundColor);
                        using (Pen p = new Pen(c, BrushSize))
                        {
                            p.StartCap = LineCap.Round;
                            p.EndCap = LineCap.Round;
                            g.DrawLine(p, p1, p2);
                        }
                    }
                }
            }
        }

        private void FloodFillActiveLayer(PointF pt, Color fillCol)
        {
            if (Document.ActiveLayer == null || Document.ActiveLayer.Image == null) return;
            using (Graphics g = Graphics.FromImage(Document.ActiveLayer.Image))
            using (SolidBrush b = new SolidBrush(fillCol))
            {
                g.FillRectangle(b, 0, 0, Document.ActiveLayer.Image.Width, Document.ActiveLayer.Image.Height);
            }
        }

        private void SampleCanvasColor(PointF pt)
        {
            if (pt.X < 0 || pt.Y < 0 || pt.X >= Document.Width || pt.Y >= Document.Height) return;
            using (Bitmap comp = Document.RenderComposite())
            {
                int px = Math.Max(0, Math.Min(comp.Width - 1, (int)pt.X));
                int py = Math.Max(0, Math.Min(comp.Height - 1, (int)pt.Y));
                ForegroundColor = comp.GetPixel(px, py);
            }
        }

        public int HitTestVectorLayerAt(PointF canvasPt, float hitTolerance)
        {
            if (Document == null || Document.Layers == null) return -1;
            for (int i = Document.Layers.Count - 1; i >= 0; i--)
            {
                var layer = Document.Layers[i];
                if (!layer.IsVisible || layer.Name == "Lớp nền") continue;
                if (layer.Type != LayerType.VectorShape) continue;

                // 1. Kiểm tra bao quát trước với dung sai
                RectangleF expandedBounds = layer.Bounds;
                float pad = hitTolerance + Math.Max(2f, layer.StrokeWidth / 2f + 2f);
                expandedBounds.Inflate(pad, pad);
                if (!expandedBounds.Contains(canvasPt)) continue;

                // 2. Nếu đã có SubPaths
                if (layer.SubPaths != null && layer.SubPaths.Count > 0)
                {
                    int hitSeg = VectorPathModel.HitTestSegment(layer, canvasPt, hitTolerance, out int subIdx);
                    if (hitSeg >= 0) return i;

                    if (layer.FillType != VectorFillType.None)
                    {
                        using (GraphicsPath gp = layer.BuildGraphicsPath())
                        {
                            if (gp != null && gp.IsVisible(canvasPt)) return i;
                        }
                    }
                }
                else
                {
                    // Nếu là hình khối chưa sinh SubPaths
                    using (GraphicsPath gp = layer.BuildGraphicsPath())
                    {
                        if (gp != null)
                        {
                            if (layer.FillType != VectorFillType.None && gp.IsVisible(canvasPt)) return i;

                            using (Pen p = new Pen(Color.Black, Math.Max(hitTolerance * 2, layer.StrokeWidth + 4)))
                            {
                                if (gp.IsOutlineVisible(canvasPt, p)) return i;
                            }
                        }
                    }
                    if (layer.FillType != VectorFillType.None && layer.Bounds.Contains(canvasPt)) return i;
                }
            }
            return -1;
        }

        private void SelectLayerAt(PointF pt, bool addToSelection = false)
        {
            if (Document == null || Document.Layers == null) return;
            float tol = 6f / Math.Max(0.1f, Zoom);
            int vecIdx = HitTestVectorLayerAt(pt, tol);
            if (vecIdx >= 0)
            {
                Document.SelectLayer(Document.Layers[vecIdx], addToSelection);
                return;
            }

            for (int i = Document.Layers.Count - 1; i >= 0; i--)
            {
                if (Document.Layers[i].IsVisible && Document.Layers[i].Bounds.Contains(pt) && Document.Layers[i].Name != "Lớp nền")
                {
                    Document.SelectLayer(Document.Layers[i], addToSelection);
                    return;
                }
            }
            if (!addToSelection)
            {
                Document.ClearSelection();
                Document.ActiveLayerIndex = -1;
            }
        }

        private void ApplyPresetFilter(string name)
        {
            switch (name)
            {
                case "bw": Document.ApplyGrayscale(); break;
                case "vintage": Document.ApplySepia(); break;
                case "vivid": Document.ApplyBrightnessContrast(0.05f, 1.35f); break;
                case "invert": Document.ApplyInvert(); break;
                case "vignette": Document.ApplyVignette(0.7f); break;
                case "blur": Document.ApplyBrightnessContrast(-0.05f, 0.95f); break;
            }
        }

        private RectangleF NormalizeRect(PointF p1, PointF p2)
        {
            float x = Math.Min(p1.X, p2.X);
            float y = Math.Min(p1.Y, p2.Y);
            float w = Math.Abs(p1.X - p2.X);
            float h = Math.Abs(p1.Y - p2.Y);
            return new RectangleF(x, y, w, h);
        }
        #endregion

        public void Dispose()
        {
            if (Document != null)
            {
                Document.Dispose();
                Document = null;
            }
        }
        private string ShowTextInputDialog(string initialText)
        {
            using (Form f = new Form() { Text = "Chỉnh sửa Văn bản", Size = new Size(500, 300), StartPosition = FormStartPosition.CenterParent, ShowIcon = false })
            {
                TextBox tb = new TextBox() { Multiline = true, Dock = DockStyle.Fill, Text = initialText, Font = new Font("Segoe UI", 12f) };
                Button btnOk = new Button() { Text = "Xác nhận", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Click += (s, e) => { f.DialogResult = DialogResult.OK; };
                f.Controls.Add(tb);
                f.Controls.Add(btnOk);
                f.AcceptButton = btnOk;
                if (f.ShowDialog() == DialogResult.OK) return tb.Text;
                return initialText;
            }
        }
    
        #region Inkscape Bottom Color Palette Bar
        private readonly List<(Rectangle Rect, Color Color)> _bottomPaletteSwatches = new List<(Rectangle, Color)>();
        private readonly List<(Rectangle Rect, Action Action)> _bottomBarClickables = new List<(Rectangle, Action)>();
        private bool _bottomColorTargetIsStroke = false;

        private void DrawBottomColorPalette(D2DGraphics g, int width, int height)
        {
            _bottomPaletteSwatches.Clear();
            _bottomBarClickables.Clear();
            int pX = LeftToolbarWidth;
            int pY = height - BottomStatusHeight;
            int pW = width - LeftToolbarWidth - RightInspectorWidth;
            int pH = BottomStatusHeight;

            Rectangle barRect = new Rectangle(pX, pY, pW, pH);
            using (SolidBrush bgB = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen sep = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(bgB, barRect);
                g.DrawLine(sep, barRect.X, barRect.Y, barRect.Right, barRect.Y);
            }

            int curX = pX + 8;

            // 1. Selector Tô / Nét (Chuẩn góc thanh màu Inkscape)
            Rectangle btnTo = new Rectangle(curX, pY + 7, 30, 22);
            _bottomBarClickables.Add((btnTo, () => _bottomColorTargetIsStroke = false));
            bool isFillActive = !_bottomColorTargetIsStroke;
            DrawCornerPill(g, btnTo, "Tô", isFillActive);
            curX += 32;

            Rectangle btnNet = new Rectangle(curX, pY + 7, 32, 22);
            _bottomBarClickables.Add((btnNet, () => _bottomColorTargetIsStroke = true));
            bool isStrokeActive = _bottomColorTargetIsStroke;
            DrawCornerPill(g, btnNet, "Nét", isStrokeActive);
            curX += 36;

            // Lấy trạng thái từ layer active hoặc thuộc tính mặc định
            var l = Document.ActiveLayer;
            VectorFillType activeFillType = (l != null && l.Type == LayerType.VectorShape) ? l.FillType : ShapeFillType;
            VectorStrokeType activeStrokeType = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeType : ShapeStrokeType;
            Color curFillColor = (l != null && l.Type == LayerType.VectorShape) ? l.FillColor : ShapeFillColor;
            Color curStrokeColor = (l != null && l.Type == LayerType.VectorShape) ? l.StrokeColor : ShapeStrokeColor;

            // 2. 4 NÚT CHẾ ĐỘ NGAY GÓC DÃY MÀU (Chuẩn thiết kế Inkscape/Illustrator)
            // Mode 0: Không màu [X]
            bool isMode0 = isFillActive ? (activeFillType == VectorFillType.None) : (activeStrokeType == VectorStrokeType.None);
            Rectangle m0 = new Rectangle(curX, pY + 7, 22, 22);
            _bottomBarClickables.Add((m0, () => {
                if (_bottomColorTargetIsStroke) SetStrokeType(VectorStrokeType.None);
                else SetFillType(VectorFillType.None);
            }));
            DrawModeIconButton(g, m0.X, m0.Y, 22, 0, null, isMode0);
            curX += 25;

            // Mode 1: Màu đơn sắc [■]
            bool isMode1 = isFillActive ? (activeFillType == VectorFillType.Solid) : (activeStrokeType == VectorStrokeType.Solid);
            Rectangle m1 = new Rectangle(curX, pY + 7, 22, 22);
            _bottomBarClickables.Add((m1, () => {
                if (_bottomColorTargetIsStroke) SetStrokeType(VectorStrokeType.Solid);
                else SetFillType(VectorFillType.Solid);
            }));
            DrawModeIconButton(g, m1.X, m1.Y, 22, 1, null, isMode1);
            curX += 25;

            // Mode 2: Dải chuyển màu [▨]
            bool isMode2 = isFillActive ? (activeFillType == VectorFillType.LinearGradient) : (activeStrokeType == VectorStrokeType.LinearGradient);
            Rectangle m2 = new Rectangle(curX, pY + 7, 22, 22);
            _bottomBarClickables.Add((m2, () => {
                if (_bottomColorTargetIsStroke) SetStrokeType(VectorStrokeType.LinearGradient);
                else SetFillType(VectorFillType.LinearGradient);
            }));
            DrawModeIconButton(g, m2.X, m2.Y, 22, 2, null, isMode2);
            curX += 25;

            // Mode 3: Tỏa tròn [◎]
            bool isMode3 = isFillActive && (activeFillType == VectorFillType.RadialGradient);
            Rectangle m3 = new Rectangle(curX, pY + 7, 22, 22);
            _bottomBarClickables.Add((m3, () => {
                if (!_bottomColorTargetIsStroke) SetFillType(VectorFillType.RadialGradient);
            }));
            DrawModeIconButton(g, m3.X, m3.Y, 22, 3, null, isMode3);
            curX += 28;

            // 3. Ô màu hiện tại (Click để mở hộp thoại chọn màu mượt mà)
            Rectangle curPrevRect = new Rectangle(curX, pY + 7, 24, 22);
            Color activeCol = _bottomColorTargetIsStroke ? curStrokeColor : curFillColor;
            bool activeIsNone = _bottomColorTargetIsStroke ? (activeStrokeType == VectorStrokeType.None) : (activeFillType == VectorFillType.None);

            _bottomBarClickables.Add((curPrevRect, () => {
                using (var dlg = new PhotoColorPickerDialogForm(activeCol))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (_bottomColorTargetIsStroke) SetStrokeColor(dlg.SelectedColor);
                        else SetFillColor(dlg.SelectedColor);
                    }
                }
            }));

            using (Pen bp = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            {
                if (activeIsNone)
                {
                    using (SolidBrush wb = new SolidBrush(Color.White))
                    using (Pen rx = new Pen(Color.FromArgb(239, 68, 68), 2f))
                    {
                        g.FillRoundedRectangle(wb, curPrevRect, 3f);
                        g.DrawRoundedRectangle(bp, curPrevRect, 3f);
                        g.DrawLine(rx, curPrevRect.Left + 2, curPrevRect.Bottom - 2, curPrevRect.Right - 2, curPrevRect.Top + 2);
                    }
                }
                else
                {
                    using (SolidBrush cb = new SolidBrush(activeCol))
                    {
                        g.FillRoundedRectangle(cb, curPrevRect, 3f);
                        g.DrawRoundedRectangle(bp, curPrevRect, 3f);
                    }
                }
            }
            curX += 30;

            // Đường phân cách dọc
            using (Pen sepPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawLine(sepPen, curX, pY + 6, curX, pY + 26);
            }
            curX += 8;

            // 4. Dãy ô màu đầy đủ (Hơn 30 màu phong phú)
            Color[] swatches = new Color[]
            {
                Color.FromArgb(0, 0, 0), Color.FromArgb(30, 41, 59), Color.FromArgb(71, 85, 105),
                Color.FromArgb(100, 116, 139), Color.FromArgb(148, 163, 184), Color.FromArgb(203, 213, 225),
                Color.FromArgb(241, 245, 249), Color.FromArgb(255, 255, 255),
                Color.FromArgb(239, 68, 68), Color.FromArgb(220, 38, 38), Color.FromArgb(185, 28, 28),
                Color.FromArgb(249, 115, 22), Color.FromArgb(234, 88, 12), Color.FromArgb(245, 158, 11),
                Color.FromArgb(234, 179, 8), Color.FromArgb(202, 138, 4), Color.FromArgb(132, 204, 22),
                Color.FromArgb(34, 197, 94), Color.FromArgb(22, 163, 74), Color.FromArgb(16, 185, 129),
                Color.FromArgb(13, 148, 136), Color.FromArgb(6, 182, 212), Color.FromArgb(14, 165, 233),
                Color.FromArgb(2, 132, 199), Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235),
                Color.FromArgb(29, 78, 216), Color.FromArgb(99, 102, 241), Color.FromArgb(79, 70, 229),
                Color.FromArgb(139, 92, 246), Color.FromArgb(124, 58, 237), Color.FromArgb(168, 85, 247),
                Color.FromArgb(217, 70, 239), Color.FromArgb(236, 72, 153), Color.FromArgb(244, 63, 94),
                Color.FromArgb(120, 53, 15), Color.FromArgb(67, 20, 7)
            };

            int swW = 16;
            int swH = 20;
            for (int s = 0; s < swatches.Length; s++)
            {
                if (curX + swW > barRect.Right - 8) break;
                Color col = swatches[s];
                Rectangle sRect = new Rectangle(curX, pY + 8, swW, swH);
                _bottomPaletteSwatches.Add((sRect, col));

                using (SolidBrush sb = new SolidBrush(col))
                using (Pen bp = new Pen(Color.FromArgb(203, 213, 225), 1f))
                {
                    g.FillRectangle(sb, sRect);
                    g.DrawRectangle(bp, sRect);
                }
                curX += swW + 3;
            }
        }

        private void DrawCornerPill(D2DGraphics g, Rectangle r, string text, bool isActive)
        {
            Color bg = isActive ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252);
            Color border = isActive ? Color.FromArgb(99, 102, 241) : Color.FromArgb(203, 213, 225);
            Color textCol = isActive ? Color.FromArgb(79, 70, 229) : Color.FromArgb(71, 85, 105);

            using (SolidBrush b = new SolidBrush(bg))
            using (Pen p = new Pen(border, isActive ? 1.5f : 1f))
            using (Font f = new Font("Segoe UI", 7.8f, isActive ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(textCol))
            {
                g.FillRoundedRectangle(b, r, 3f);
                g.DrawRoundedRectangle(p, r, 3f);
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, tb, r.X + (r.Width - sz.Width) / 2f, r.Y + (r.Height - sz.Height) / 2f);
            }
        }
        #endregion

        #region Inkscape Image & Vector Import
        public void PromptInsertImage(IWin32Window owner = null)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chèn hình ảnh vào đồ họa - Chuẩn Inkscape";
                ofd.Filter = "Tất cả tệp hình ảnh & Vector (*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.svg)|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.svg|Đồ họa Vector SVG (*.svg)|*.svg|Hình ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tất cả tệp (*.*)|*.*";
                if (ofd.ShowDialog(owner) == DialogResult.OK)
                {
                    InsertImageFromFile(ofd.FileName, owner);
                }
            }
        }

        public void InsertImageFromFile(string filePath, IWin32Window owner = null)
        {
            if (Document == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            try
            {
                Bitmap bmp = null;
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".svg")
                {
                    string svgContent = File.ReadAllText(filePath);
                    bmp = ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(svgContent, Document.Width, Document.Height)
                       ?? ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(filePath, Document.Width, Document.Height);
                }
                else
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var orig = Image.FromStream(stream))
                    {
                        bmp = new Bitmap(orig);
                    }
                }

                if (bmp == null)
                {
                    MessageBox.Show(owner, "Không thể đọc định dạng hình ảnh này.", "Lỗi tải ảnh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var dlg = new PhotoInsertImageDialogForm(filePath, bmp, Document.Width, Document.Height))
                {
                    if (dlg.ShowDialog(owner) == DialogResult.OK)
                    {
                        float targetW = bmp.Width;
                        float targetH = bmp.Height;

                        if (dlg.SizeMode == ImageSizeMode.FitCanvas)
                        {
                            float maxW = Document.Width * 0.8f;
                            float maxH = Document.Height * 0.8f;
                            float scale = Math.Min(maxW / bmp.Width, maxH / bmp.Height);
                            if (scale < 1.0f || !dlg.LockAspectRatio)
                            {
                                targetW = bmp.Width * scale;
                                targetH = bmp.Height * scale;
                            }
                        }

                        float posX = Math.Max(0f, (Document.Width - targetW) / 2f);
                        float posY = Math.Max(0f, (Document.Height - targetH) / 2f);

                        Document.SaveUndoSnapshot();
                        var newLayer = new PhotoLayer
                        {
                            Name = Path.GetFileName(filePath),
                            Type = LayerType.Raster,
                            Bounds = new RectangleF(posX, posY, targetW, targetH),
                            Image = bmp,
                            ImagePath = (dlg.ImportMode == ImageImportMode.Link) ? filePath : null,
                            IsLinkedImage = (dlg.ImportMode == ImageImportMode.Link),
                            Opacity = 1.0f,
                            IsVisible = true
                        };

                        Document.Layers.Add(newLayer);
                        Document.ActiveLayerIndex = Document.Layers.Count - 1;
                    }
                    else
                    {
                        bmp.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Lỗi khi chèn ảnh: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Workspace Scrollbars Drawing
        private void DrawWorkspaceScrollbars(D2DGraphics g, int width, int height)
        {
            if (Document == null)
            {
                HScrollBarTrackRect = Rectangle.Empty;
                HScrollBarThumbRect = Rectangle.Empty;
                VScrollBarTrackRect = Rectangle.Empty;
                VScrollBarThumbRect = Rectangle.Empty;
                return;
            }

            int vX = LeftToolbarWidth;
            int vY = RibbonHeight + TopOptionsBarHeight;
            int vW = Math.Max(100, width - LeftToolbarWidth - RightInspectorWidth);
            int vH = Math.Max(100, height - RibbonHeight - TopOptionsBarHeight - BottomStatusHeight);

            float docW = Document.Width * Zoom;
            float docH = Document.Height * Zoom;

            GetScrollLimits(width, height, out float minX, out float maxX, out float minY, out float maxY);

            bool hasHScroll = (docW + 30f > vW) || (maxX > minX + 5f);
            bool hasVScroll = (docH + 30f > vH) || (maxY > minY + 5f);

            int sbSize = 14;

            if (hasHScroll)
            {
                int hTrackW = hasVScroll ? (vW - sbSize) : vW;
                int hTrackX = vX;
                int hTrackY = vY + vH - sbSize;
                HScrollBarTrackRect = new Rectangle(hTrackX, hTrackY, hTrackW, sbSize);

                using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                using (Pen trackBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillRectangle(trackBg, HScrollBarTrackRect);
                    g.DrawLine(trackBorder, hTrackX, hTrackY, hTrackX + hTrackW, hTrackY);
                }

                float totalSpanX = maxX - minX + vW;
                int thumbW = Math.Max(36, (int)((vW / Math.Max(1f, totalSpanX)) * hTrackW));
                thumbW = Math.Min(thumbW, hTrackW);

                float fracX = (maxX > minX) ? (maxX - PanX) / (maxX - minX) : 0f;
                fracX = Math.Max(0f, Math.Min(1f, fracX));
                int thumbX = hTrackX + (int)(fracX * (hTrackW - thumbW));
                HScrollBarThumbRect = new Rectangle(thumbX + 2, hTrackY + 2, Math.Max(16, thumbW - 4), sbSize - 4);

                Color thumbCol = IsDraggingHScrollBar ? Color.FromArgb(100, 116, 139) :
                                 (IsHoverHScrollBarThumb ? Color.FromArgb(148, 163, 184) : Color.FromArgb(188, 195, 206));
                using (SolidBrush thumbBrush = new SolidBrush(thumbCol))
                {
                    FillRoundedScrollThumb(g, thumbBrush, HScrollBarThumbRect);
                }
            }
            else
            {
                HScrollBarTrackRect = Rectangle.Empty;
                HScrollBarThumbRect = Rectangle.Empty;
            }

            if (hasVScroll)
            {
                int vTrackH = hasHScroll ? (vH - sbSize) : vH;
                int vTrackX = vX + vW - sbSize;
                int vTrackY = vY;
                VScrollBarTrackRect = new Rectangle(vTrackX, vTrackY, sbSize, vTrackH);

                using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                using (Pen trackBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.FillRectangle(trackBg, VScrollBarTrackRect);
                    g.DrawLine(trackBorder, vTrackX, vTrackY, vTrackX, vTrackY + vTrackH);
                }

                float totalSpanY = maxY - minY + vH;
                int thumbH = Math.Max(36, (int)((vH / Math.Max(1f, totalSpanY)) * vTrackH));
                thumbH = Math.Min(thumbH, vTrackH);

                float fracY = (maxY > minY) ? (maxY - PanY) / (maxY - minY) : 0f;
                fracY = Math.Max(0f, Math.Min(1f, fracY));
                int thumbY = vTrackY + (int)(fracY * (vTrackH - thumbH));
                VScrollBarThumbRect = new Rectangle(vTrackX + 2, thumbY + 2, sbSize - 4, Math.Max(16, thumbH - 4));

                Color thumbCol = IsDraggingVScrollBar ? Color.FromArgb(100, 116, 139) :
                                 (IsHoverVScrollBarThumb ? Color.FromArgb(148, 163, 184) : Color.FromArgb(188, 195, 206));
                using (SolidBrush thumbBrush = new SolidBrush(thumbCol))
                {
                    FillRoundedScrollThumb(g, thumbBrush, VScrollBarThumbRect);
                }
            }
            else
            {
                VScrollBarTrackRect = Rectangle.Empty;
                VScrollBarThumbRect = Rectangle.Empty;
            }

            if (hasHScroll && hasVScroll)
            {
                Rectangle cornerRect = new Rectangle(vX + vW - sbSize, vY + vH - sbSize, sbSize, sbSize);
                using (SolidBrush cornerBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                {
                    g.FillRectangle(cornerBg, cornerRect);
                }
            }
        }

        private void FillRoundedScrollThumb(D2DGraphics g, SolidBrush brush, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            float r = Math.Min(rect.Width, rect.Height) / 2f;
            r = Math.Min(r, 4f);
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                float d = r * 2f;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }
        #endregion
        #endregion

    }
}

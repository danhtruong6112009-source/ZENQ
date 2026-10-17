using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using ZenQOffice.Format;

namespace ZenQOffice.UI
{
    public enum LayerType
    {
        Raster,
        VectorShape,
        Text
    }

    public enum LayerBlendMode
    {
        Normal,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        Add,
        Difference
    }

    
    public enum VectorFillType
    {
        None,
        Solid,
        LinearGradient,
        RadialGradient
    }

    public class PhotoGradientStop
    {
        public Color Color { get; set; } = Color.White;
        public float Position { get; set; } = 0f; // 0.0 to 1.0

        public PhotoGradientStop() { }
        public PhotoGradientStop(Color color, float position)
        {
            Color = color;
            Position = position;
        }

        public PhotoGradientStop Clone()
        {
            return new PhotoGradientStop(Color, Position);
        }
    }

    public enum VectorStrokeType
    {
        None,
        Solid,
        LinearGradient
    }

    public enum VectorShapeType
    {
        Rectangle,
        RoundedRect,
        Ellipse,
        Star,
        Polygon,
        Arrow,
        Heart,
        Line,
        Spiral,
        Path
    }

    public enum PhotoTool
    {
        Move,
        NodeTool,
        Rectangle,
        Ellipse,
        Star,
        Pen,
        Brush,
        Eraser,
        Text,
        Bucket,
        Eyedropper,
        Gradient,
        Hand,
        Crop,
        Marquee,
        Shape
    }

    public class PhotoLayer : IDisposable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? GroupId { get; set; } = null;
        public PhotoLayer ClipMask { get; set; } = null;
        public bool IsTextOnPath { get; set; } = false;
        public List<VectorSubPath> TextPathSubPaths { get; set; } = null;
        public string Name { get; set; } = "Lớp mới";
        public List<PointF> PathPoints { get; set; } = new List<PointF>();
        public bool IsVisible { get; set; } = true;
        public float Opacity { get; set; } = 1.0f; // 0.0 to 1.0
        public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
        public LayerType Type { get; set; } = LayerType.Raster;

        public RectangleF Bounds { get; set; }
        public float Rotation { get; set; } = 0f;

        // Raster Layer Pixel Buffer
        public Bitmap Image { get; set; }
        public string ImagePath { get; set; }
        public bool IsLinkedImage { get; set; } = false;

        // Vector Shape Properties
        public VectorShapeType ShapeType { get; set; } = VectorShapeType.Rectangle;
        public VectorFillType FillType { get; set; } = VectorFillType.Solid;
        public Color FillColor { get; set; } = Color.FromArgb(59, 130, 246);
        public Color FillColor2 { get; set; } = Color.FromArgb(147, 197, 253);
        public float FillGradientAngle { get; set; } = 0f;
        public List<PhotoGradientStop> GradientStops { get; set; } = new List<PhotoGradientStop>();

        public VectorStrokeType StrokeType { get; set; } = VectorStrokeType.Solid;
        public Color StrokeColor { get; set; } = Color.FromArgb(29, 78, 216);
        public float StrokeWidth { get; set; } = 2.0f;
        public DashStyle StrokeDashStyle { get; set; } = DashStyle.Solid;
        public LineJoin StrokeLineJoin { get; set; } = LineJoin.Round;
        public LineCap StrokeLineCap { get; set; } = LineCap.Round;
        public float CornerRadius { get; set; } = 0.0f;
        public int ShapePointCount { get; set; } = 5;
        public bool HasShadow { get; set; } = false;
        public bool DropShadow { get => HasShadow; set => HasShadow = value; }

        // Vector SubPaths and Nodes
        public List<VectorSubPath> SubPaths { get; set; } = new List<VectorSubPath>();

        public List<VectorPathNode> Nodes
        {
            get
            {
                if (SubPaths == null) SubPaths = new List<VectorSubPath>();
                if (SubPaths.Count == 0) SubPaths.Add(new VectorSubPath(true));
                return SubPaths[0].Nodes;
            }
            set
            {
                if (SubPaths == null) SubPaths = new List<VectorSubPath>();
                if (SubPaths.Count == 0) SubPaths.Add(new VectorSubPath(true));
                SubPaths[0].Nodes = value ?? new List<VectorPathNode>();
            }
        }

        public bool IsClosedPath
        {
            get => SubPaths != null && SubPaths.Count > 0 && SubPaths[0].IsClosed;
            set
            {
                if (SubPaths == null) SubPaths = new List<VectorSubPath>();
                if (SubPaths.Count == 0) SubPaths.Add(new VectorSubPath(value));
                else SubPaths[0].IsClosed = value;
            }
        }

        public void EnsureNodesInitialized()
        {
            if (SubPaths == null) SubPaths = new List<VectorSubPath>();
            bool hasAnyNodes = SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Count > 0);
            if (!hasAnyNodes)
            {
                if (PathPoints != null && PathPoints.Count > 0)
                {
                    var sp = new VectorSubPath(false);
                    for (int i = 0; i < PathPoints.Count; i++)
                    {
                        sp.Nodes.Add(new VectorPathNode(PathPoints[i], VectorNodeType.Corner));
                    }
                    SubPaths.Add(sp);
                    ShapeType = VectorShapeType.Path;
                }
                else
                {
                    SubPaths = VectorPathModel.CreateSubPathsFromShape(ShapeType, Bounds, CornerRadius, ShapePointCount);
                    ShapeType = VectorShapeType.Path;
                }
            }
        }

        public GraphicsPath BuildGraphicsPath()
        {
            if (SubPaths != null && SubPaths.Count > 0 && SubPaths.Any(sp => sp.Nodes != null && sp.Nodes.Count > 0))
            {
                GraphicsPath gp = new GraphicsPath();
                foreach (var sp in SubPaths)
                {
                    sp.AppendToGraphicsPath(gp);
                }
                return gp;
            }
            else if (PathPoints != null && PathPoints.Count > 1)
            {
                GraphicsPath gp = new GraphicsPath();
                gp.AddLines(PathPoints.ToArray());
                return gp;
            }
            else
            {
                return PhotoDocument.CreateShapePath(ShapeType, Bounds, CornerRadius, ShapePointCount);
            }
        }

        // Text Properties
        public string TextContent { get; set; } = "Văn bản";
        public string Text { get => TextContent; set => TextContent = value; }
        public string FontName { get; set; } = "Segoe UI";
        public string FontFamily { get => FontName; set => FontName = value; }
        public float FontSize { get; set; } = 36.0f;
        public FontStyle FontStyle { get; set; } = FontStyle.Bold;
        public Color TextColor { get; set; } = Color.FromArgb(30, 41, 59);

        public PhotoLayer Clone()
        {
            PhotoLayer copy = new PhotoLayer
            {
                Id = Guid.NewGuid(),
                Name = Name + " (Sao chép)",
                IsVisible = IsVisible,
                Opacity = Opacity,
                BlendMode = BlendMode,
                Type = Type,
                Bounds = Bounds,
                Rotation = Rotation,
                ShapeType = ShapeType,
                FillType = FillType,
                FillColor = FillColor,
                FillColor2 = FillColor2,
                FillGradientAngle = FillGradientAngle,
                GradientStops = GradientStops?.Select(s => s.Clone()).ToList() ?? new List<PhotoGradientStop>(),
                StrokeType = StrokeType,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                StrokeDashStyle = StrokeDashStyle,
                StrokeLineJoin = StrokeLineJoin,
                StrokeLineCap = StrokeLineCap,
                CornerRadius = CornerRadius,
                ShapePointCount = ShapePointCount,
                HasShadow = HasShadow,
                TextContent = TextContent,
                FontName = FontName,
                FontSize = FontSize,
                FontStyle = FontStyle,
                TextColor = TextColor,
                ImagePath = ImagePath,
                IsLinkedImage = IsLinkedImage,
                GroupId = GroupId,
                ClipMask = ClipMask?.Clone(),
                IsTextOnPath = IsTextOnPath,
                TextPathSubPaths = TextPathSubPaths?.Select(sp => sp.Clone()).ToList()
            };

            // Deep copy SubPaths & Nodes
            if (SubPaths != null)
            {
                copy.SubPaths = new List<VectorSubPath>(SubPaths.Count);
                foreach (var sp in SubPaths)
                {
                    copy.SubPaths.Add(sp.Clone());
                }
            }

            // Sao chép PathPoints
            if (PathPoints != null)
            {
                copy.PathPoints = new List<PointF>(PathPoints);
            }

            if (Image != null)
            {
                copy.Image = new Bitmap(Image);
            }

            return copy;
        }

        public void EnsureBitmapSize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            if (Image == null)
            {
                Image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(Image))
                {
                    g.Clear(Color.Transparent);
                }
                Bounds = new RectangleF(0, 0, width, height);
            }
            else if (Image.Width < width || Image.Height < height)
            {
                int newW = Math.Max(Image.Width, width);
                int newH = Math.Max(Image.Height, height);
                Bitmap expanded = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(expanded))
                {
                    g.Clear(Color.Transparent);
                    g.DrawImage(Image, 0, 0);
                }
                Image.Dispose();
                Image = expanded;
            }
        }

        public void Dispose()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
        }
    }

    public class PhotoDocumentSnapshot
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<PhotoLayer> Layers { get; set; } = new List<PhotoLayer>();
        public int ActiveLayerIndex { get; set; }
        public List<Guid> SelectedLayerIds { get; set; } = new List<Guid>();
    }

    public class PhotoDocument : IDisposable
    {
        public string Title { get; set; } = "Thiết kế Mới 1.svg";
        public string FilePath { get; set; }
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public Color BackgroundColor { get; set; } = Color.White;
        public List<PhotoLayer> Layers { get; set; } = new List<PhotoLayer>();
        public int ActiveLayerIndex { get; set; } = 0;
        public HashSet<Guid> SelectedLayerIds { get; } = new HashSet<Guid>();

        public Stack<PhotoDocumentSnapshot> UndoStack { get; } = new Stack<PhotoDocumentSnapshot>();
        public Stack<PhotoDocumentSnapshot> RedoStack { get; } = new Stack<PhotoDocumentSnapshot>();

        public PhotoLayer ActiveLayer
        {
            get
            {
                if (ActiveLayerIndex >= 0 && ActiveLayerIndex < Layers.Count)
                    return Layers[ActiveLayerIndex];
                return null;
            }
        }

        public List<PhotoLayer> SelectedLayers
        {
            get
            {
                if (SelectedLayerIds.Count > 0)
                {
                    var list = Layers.Where(l => SelectedLayerIds.Contains(l.Id)).ToList();
                    if (list.Count > 0) return list;
                }
                if (ActiveLayer != null) return new List<PhotoLayer> { ActiveLayer };
                return new List<PhotoLayer>();
            }
        }

        public void ClearSelection()
        {
            SelectedLayerIds.Clear();
        }

        public void SelectLayer(PhotoLayer layer, bool addToSelection = false)
        {
            if (layer == null) return;
            int idx = Layers.IndexOf(layer);
            if (idx < 0) return;

            var toProcess = new List<PhotoLayer> { layer };
            if (layer.GroupId.HasValue)
            {
                var groupMembers = Layers.Where(l => l.GroupId == layer.GroupId).ToList();
                if (groupMembers.Count > 0) toProcess = groupMembers;
            }

            if (addToSelection)
            {
                bool isPrimarySelected = SelectedLayerIds.Contains(layer.Id);
                if (isPrimarySelected)
                {
                    foreach (var m in toProcess) SelectedLayerIds.Remove(m.Id);
                    if (ActiveLayerIndex == idx)
                    {
                        var rem = Layers.FirstOrDefault(l => SelectedLayerIds.Contains(l.Id));
                        if (rem != null) ActiveLayerIndex = Layers.IndexOf(rem);
                    }
                }
                else
                {
                    foreach (var m in toProcess) SelectedLayerIds.Add(m.Id);
                    ActiveLayerIndex = idx;
                }
            }
            else
            {
                SelectedLayerIds.Clear();
                foreach (var m in toProcess) SelectedLayerIds.Add(m.Id);
                ActiveLayerIndex = idx;
            }
        }

        public void SelectAllLayers()
        {
            SelectedLayerIds.Clear();
            for (int i = 0; i < Layers.Count; i++)
            {
                if (i == 0 && Layers.Count > 1 && Layers[0].Name == "Lớp nền") continue;
                SelectedLayerIds.Add(Layers[i].Id);
            }
            if (Layers.Count > 1) ActiveLayerIndex = Layers.Count - 1;
        }

        public PhotoDocument()
        {
            InitDefault();
        }

        public PhotoDocument(int width, int height, string title = "Thiết kế Mới 1.svg")
        {
            Width = Math.Max(100, width);
            Height = Math.Max(100, height);
            Title = title;
            InitDefault();
        }

        private void InitDefault()
        {
            Layers.Clear();

            // Background Layer (Khởi tạo canvas trắng tinh, trống trơn)
            PhotoLayer bg = new PhotoLayer
            {
                Name = "Lớp nền",
                Type = LayerType.Raster,
                Bounds = new RectangleF(0, 0, Width, Height),
                Image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb)
            };
            using (Graphics g = Graphics.FromImage(bg.Image))
            {
                g.Clear(Color.White);
            }
            Layers.Add(bg);

            ActiveLayerIndex = 0;
        }

        public void SaveUndoSnapshot()
        {
            var snap = new PhotoDocumentSnapshot
            {
                Width = Width,
                Height = Height,
                ActiveLayerIndex = ActiveLayerIndex,
                SelectedLayerIds = new List<Guid>(SelectedLayerIds)
            };
            foreach (var l in Layers)
            {
                snap.Layers.Add(l.Clone());
            }

            UndoStack.Push(snap);
            if (UndoStack.Count > 30)
            {
                // Discard oldest
                var list = new List<PhotoDocumentSnapshot>(UndoStack);
                list.RemoveAt(list.Count - 1);
                UndoStack.Clear();
                for (int i = list.Count - 1; i >= 0; i--) UndoStack.Push(list[i]);
            }
            RedoStack.Clear();
        }

        public bool Undo()
        {
            if (UndoStack.Count == 0) return false;

            // Save redo snapshot
            var current = new PhotoDocumentSnapshot
            {
                Width = Width,
                Height = Height,
                ActiveLayerIndex = ActiveLayerIndex,
                SelectedLayerIds = new List<Guid>(SelectedLayerIds)
            };
            foreach (var l in Layers) current.Layers.Add(l.Clone());
            RedoStack.Push(current);

            var snap = UndoStack.Pop();
            RestoreFromSnapshot(snap);
            return true;
        }

        public bool Redo()
        {
            if (RedoStack.Count == 0) return false;

            var current = new PhotoDocumentSnapshot
            {
                Width = Width,
                Height = Height,
                ActiveLayerIndex = ActiveLayerIndex,
                SelectedLayerIds = new List<Guid>(SelectedLayerIds)
            };
            foreach (var l in Layers) current.Layers.Add(l.Clone());
            UndoStack.Push(current);

            var snap = RedoStack.Pop();
            RestoreFromSnapshot(snap);
            return true;
        }

        private void RestoreFromSnapshot(PhotoDocumentSnapshot snap)
        {
            foreach (var l in Layers) l.Dispose();
            Layers.Clear();

            Width = snap.Width;
            Height = snap.Height;
            foreach (var l in snap.Layers)
            {
                Layers.Add(l.Clone());
            }
            ActiveLayerIndex = Math.Max(0, Math.Min(Layers.Count - 1, snap.ActiveLayerIndex));
            SelectedLayerIds.Clear();
            if (snap.SelectedLayerIds != null)
            {
                foreach (var id in snap.SelectedLayerIds) SelectedLayerIds.Add(id);
            }
        }

        public PhotoLayer AddRasterLayer(string name = null, Bitmap bitmap = null, float x = 0, float y = 0)
        {
            SaveUndoSnapshot();
            int w = (bitmap != null) ? bitmap.Width : Width;
            int h = (bitmap != null) ? bitmap.Height : Height;
            string lName = name ?? ("Lớp " + (Layers.Count + 1));

            PhotoLayer layer = new PhotoLayer
            {
                Name = lName,
                Type = LayerType.Raster,
                Bounds = new RectangleF(x, y, w, h),
                Image = (bitmap != null) ? new Bitmap(bitmap) : new Bitmap(Width, Height, PixelFormat.Format32bppArgb)
            };

            if (bitmap == null)
            {
                using (Graphics g = Graphics.FromImage(layer.Image))
                {
                    g.Clear(Color.Transparent);
                }
            }

            Layers.Add(layer);
            ActiveLayerIndex = Layers.Count - 1;
            return layer;
        }

        public PhotoLayer AddVectorShape(
            VectorShapeType shapeType,
            RectangleF bounds,
            Color fill,
            Color stroke,
            float strokeWidth = 2.0f,
            float cornerRadius = 0f,
            int pointCount = 5,
            VectorFillType fillType = VectorFillType.Solid,
            Color? fill2 = null,
            float gradientAngle = 0f,
            VectorStrokeType strokeType = VectorStrokeType.Solid,
            DashStyle dashStyle = DashStyle.Solid,
            LineJoin lineJoin = LineJoin.Round,
            LineCap lineCap = LineCap.Round,
            List<PhotoGradientStop> gradientStops = null)
        {
            SaveUndoSnapshot();
            string name = shapeType switch
            {
                VectorShapeType.Rectangle => "Hình chữ nhật",
                VectorShapeType.RoundedRect => "Chữ nhật bo góc",
                VectorShapeType.Ellipse => "Hình tròn / Elip",
                VectorShapeType.Star => "Ngôi sao",
                VectorShapeType.Polygon => "Đa giác",
                VectorShapeType.Arrow => "Mũi tên",
                VectorShapeType.Heart => "Trái tim",
                VectorShapeType.Line => "Đoạn thẳng",
                _ => "Hình vector"
            };

            PhotoLayer layer = new PhotoLayer
            {
                Name = name + " " + (Layers.Count + 1),
                Type = LayerType.VectorShape,
                ShapeType = shapeType,
                Bounds = bounds,
                FillType = fillType,
                FillColor = fill,
                FillColor2 = fill2 ?? Color.FromArgb(147, 197, 253),
                FillGradientAngle = gradientAngle,
                GradientStops = gradientStops?.Select(s => s.Clone()).ToList() ?? new List<PhotoGradientStop>(),
                StrokeType = strokeType,
                StrokeColor = stroke,
                StrokeWidth = strokeWidth,
                StrokeDashStyle = dashStyle,
                StrokeLineJoin = lineJoin,
                StrokeLineCap = lineCap,
                CornerRadius = cornerRadius,
                ShapePointCount = pointCount
            };

            Layers.Add(layer);
            ActiveLayerIndex = Layers.Count - 1;
            return layer;
        }

        public PhotoLayer AddText(string text, float x, float y, float fontSize = 36f, Color? color = null)
        {
            SaveUndoSnapshot();
            PhotoLayer layer = new PhotoLayer
            {
                Name = "Văn bản " + (Layers.Count + 1),
                Type = LayerType.Text,
                TextContent = text,
                FontSize = fontSize,
                TextColor = color ?? Color.FromArgb(30, 41, 59),
                Bounds = new RectangleF(x, y, Math.Max(200, text.Length * fontSize * 0.7f), fontSize * 1.5f)
            };

            Layers.Add(layer);
            ActiveLayerIndex = Layers.Count - 1;
            return layer;
        }

        public void DuplicateActiveLayer()
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            PhotoLayer clone = ActiveLayer.Clone();
            clone.Name = ActiveLayer.Name + " (Sao chép)";
            clone.Bounds = new RectangleF(ActiveLayer.Bounds.X + 20, ActiveLayer.Bounds.Y + 20, ActiveLayer.Bounds.Width, ActiveLayer.Bounds.Height);
            Layers.Insert(ActiveLayerIndex + 1, clone);
            ActiveLayerIndex++;
        }

        public void DeleteActiveLayer()
        {
            if (ActiveLayer == null || Layers.Count <= 1) return;
            SaveUndoSnapshot();
            ActiveLayer.Dispose();
            Layers.RemoveAt(ActiveLayerIndex);
            if (ActiveLayerIndex >= Layers.Count) ActiveLayerIndex = Layers.Count - 1;
        }

        public void MoveLayerUp()
        {
            if (ActiveLayerIndex < Layers.Count - 1)
            {
                SaveUndoSnapshot();
                var tmp = Layers[ActiveLayerIndex];
                Layers[ActiveLayerIndex] = Layers[ActiveLayerIndex + 1];
                Layers[ActiveLayerIndex + 1] = tmp;
                ActiveLayerIndex++;
            }
        }

        public void MoveLayerDown()
        {
            if (ActiveLayerIndex > 0)
            {
                SaveUndoSnapshot();
                var tmp = Layers[ActiveLayerIndex];
                Layers[ActiveLayerIndex] = Layers[ActiveLayerIndex - 1];
                Layers[ActiveLayerIndex - 1] = tmp;
                ActiveLayerIndex--;
            }
        }

        public void BringActiveLayerToFront()
        {
            if (ActiveLayerIndex < Layers.Count - 1)
            {
                SaveUndoSnapshot();
                var l = Layers[ActiveLayerIndex];
                Layers.RemoveAt(ActiveLayerIndex);
                Layers.Add(l);
                ActiveLayerIndex = Layers.Count - 1;
            }
        }

        public void SendActiveLayerToBack()
        {
            int targetIndex = (Layers.Count > 1 && Layers[0].Name == "Lớp nền") ? 1 : 0;
            if (ActiveLayerIndex > targetIndex)
            {
                SaveUndoSnapshot();
                var l = Layers[ActiveLayerIndex];
                Layers.RemoveAt(ActiveLayerIndex);
                Layers.Insert(targetIndex, l);
                ActiveLayerIndex = targetIndex;
            }
        }

        // ==========================================
        // THAO TÁC ĐẠI SỐ ĐƯỜNG VẼ & BIẾN ĐỔI INKSCAPE
        // ==========================================

        public bool ApplyBooleanOperation(BooleanOpType op)
        {
            var selected = SelectedLayers.Where(l => l.Type == LayerType.VectorShape).ToList();
            PhotoLayer bottom = null;
            PhotoLayer top = null;

            if (selected.Count >= 2)
            {
                int idx1 = Layers.IndexOf(selected[0]);
                int idx2 = Layers.IndexOf(selected[1]);
                if (idx1 < idx2) { bottom = selected[0]; top = selected[1]; }
                else { bottom = selected[1]; top = selected[0]; }
            }
            else if (ActiveLayer != null && ActiveLayer.Type == LayerType.VectorShape)
            {
                top = ActiveLayer;
                for (int i = ActiveLayerIndex - 1; i >= 0; i--)
                {
                    if (Layers[i].Type == LayerType.VectorShape)
                    {
                        bottom = Layers[i];
                        break;
                    }
                }
            }

            if (bottom == null || top == null) return false;

            if (!VectorPathModel.ExecuteBoolean(bottom, top, op, out var resA, out var resB))
            {
                return false;
            }

            SaveUndoSnapshot();

            bottom.Rotation = 0f;
            if (op == BooleanOpType.Division)
            {
                bottom.SubPaths = resA ?? new List<VectorSubPath>();
                bottom.ShapeType = VectorShapeType.Path;
                VectorPathModel.UpdateLayerBounds(bottom);

                if (resB != null && resB.Count > 0)
                {
                    PhotoLayer piece = new PhotoLayer
                    {
                        Name = bottom.Name + " (Phần cắt)",
                        Type = LayerType.VectorShape,
                        ShapeType = VectorShapeType.Path,
                        FillColor = bottom.FillColor,
                        FillColor2 = bottom.FillColor2,
                        FillType = bottom.FillType,
                        FillGradientAngle = bottom.FillGradientAngle,
                        StrokeColor = bottom.StrokeColor,
                        StrokeWidth = bottom.StrokeWidth,
                        StrokeDashStyle = bottom.StrokeDashStyle,
                        StrokeLineJoin = bottom.StrokeLineJoin,
                        StrokeLineCap = bottom.StrokeLineCap,
                        StrokeType = bottom.StrokeType,
                        Opacity = bottom.Opacity,
                        SubPaths = resB
                    };
                    VectorPathModel.UpdateLayerBounds(piece);
                    int insertIdx = Layers.IndexOf(bottom) + 1;
                    if (insertIdx > Layers.Count) insertIdx = Layers.Count;
                    Layers.Insert(insertIdx, piece);
                }

                SelectedLayerIds.Remove(top.Id);
                Layers.Remove(top);
                top.Dispose();
                ActiveLayerIndex = Layers.IndexOf(bottom);
                SelectedLayerIds.Clear();
                SelectedLayerIds.Add(bottom.Id);
            }
            else
            {
                bottom.SubPaths = resA ?? new List<VectorSubPath>();
                bottom.ShapeType = VectorShapeType.Path;
                VectorPathModel.UpdateLayerBounds(bottom);

                SelectedLayerIds.Remove(top.Id);
                Layers.Remove(top);
                top.Dispose();
                ActiveLayerIndex = Layers.IndexOf(bottom);
                SelectedLayerIds.Clear();
                SelectedLayerIds.Add(bottom.Id);
            }

            return true;
        }

        public bool CombineSelectedPaths()
        {
            var selected = SelectedLayers.Where(l => l.Type == LayerType.VectorShape).ToList();
            if (selected.Count < 2 && ActiveLayerIndex > 0 && Layers[ActiveLayerIndex - 1].Type == LayerType.VectorShape)
            {
                selected = new List<PhotoLayer> { Layers[ActiveLayerIndex - 1], ActiveLayer };
            }
            if (selected.Count < 2) return false;
            return VectorPathModel.CombineLayers(this, selected);
        }

        public bool BreakApartActivePath()
        {
            if (ActiveLayer == null) return false;
            return VectorPathModel.BreakApartLayer(this, ActiveLayer);
        }

        public bool ObjectToPathActive()
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.VectorShape) return false;
            SaveUndoSnapshot();
            bool ok = VectorPathModel.ObjectToPath(ActiveLayer);
            if (ok)
            {
                ActiveLayer.ShapeType = VectorShapeType.Path;
                VectorPathModel.UpdateLayerBounds(ActiveLayer);
            }
            return ok;
        }

        public bool StrokeToPathActive()
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.VectorShape) return false;
            SaveUndoSnapshot();
            bool ok = VectorPathModel.StrokeToPath(ActiveLayer);
            if (ok)
            {
                ActiveLayer.ShapeType = VectorShapeType.Path;
                VectorPathModel.UpdateLayerBounds(ActiveLayer);
            }
            return ok;
        }

        public void ReverseActivePath()
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.VectorShape) return;
            SaveUndoSnapshot();
            VectorPathModel.ReversePath(ActiveLayer);
        }

        public bool SimplifyActivePath(float tolerance = 2.0f)
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.VectorShape) return false;
            SaveUndoSnapshot();
            bool ok = VectorPathModel.SimplifyPath(ActiveLayer, tolerance);
            if (ok)
            {
                ActiveLayer.ShapeType = VectorShapeType.Path;
                VectorPathModel.UpdateLayerBounds(ActiveLayer);
            }
            return ok;
        }

        public bool InsetActivePath(float delta = 3.0f)
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.VectorShape) return false;
            SaveUndoSnapshot();
            return VectorPathModel.ApplyOffset(ActiveLayer, -Math.Abs(delta));
        }

        public bool OutsetActivePath(float delta = 3.0f)
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.VectorShape) return false;
            SaveUndoSnapshot();
            return VectorPathModel.ApplyOffset(ActiveLayer, Math.Abs(delta));
        }

        public void GroupSelectedLayers()
        {
            var selected = SelectedLayers;
            if (selected.Count < 2) return;
            SaveUndoSnapshot();
            Guid newGid = Guid.NewGuid();
            foreach (var lyr in selected)
            {
                lyr.GroupId = newGid;
            }
        }

        public void UngroupSelectedLayers()
        {
            var selected = SelectedLayers;
            if (selected.Count == 0) return;
            SaveUndoSnapshot();
            var gidsToClear = selected.Where(l => l.GroupId.HasValue).Select(l => l.GroupId.Value).Distinct().ToList();
            foreach (var lyr in Layers)
            {
                if (lyr.GroupId.HasValue && gidsToClear.Contains(lyr.GroupId.Value))
                {
                    lyr.GroupId = null;
                }
            }
        }

        public void DuplicateSelectedLayers()
        {
            var selected = SelectedLayers;
            if (selected.Count == 0 && ActiveLayer != null) selected = new List<PhotoLayer> { ActiveLayer };
            if (selected.Count == 0) return;

            SaveUndoSnapshot();
            var newSelectedIds = new HashSet<Guid>();
            foreach (var lyr in selected)
            {
                var copy = lyr.Clone();
                copy.Name = lyr.Name + " (Bản sao)";
                copy.Bounds = new RectangleF(lyr.Bounds.X + 15f, lyr.Bounds.Y + 15f, lyr.Bounds.Width, lyr.Bounds.Height);
                int origIdx = Layers.IndexOf(lyr);
                Layers.Insert(origIdx + 1, copy);
                newSelectedIds.Add(copy.Id);
            }
            SelectedLayerIds.Clear();
            foreach (var id in newSelectedIds) SelectedLayerIds.Add(id);
            if (Layers.Count > 0)
            {
                var lastNew = Layers.FirstOrDefault(l => newSelectedIds.Contains(l.Id));
                if (lastNew != null) ActiveLayerIndex = Layers.IndexOf(lastNew);
            }
        }

        private static readonly List<PhotoLayer> _layerClipboard = new List<PhotoLayer>();

        public void CopySelected()
        {
            var selected = SelectedLayers;
            if (selected.Count == 0 && ActiveLayer != null) selected = new List<PhotoLayer> { ActiveLayer };
            if (selected.Count == 0) return;

            _layerClipboard.Clear();
            foreach (var l in selected)
            {
                _layerClipboard.Add(l.Clone());
            }

            try
            {
                var firstImg = selected.FirstOrDefault(l => l.Image != null);
                if (firstImg?.Image != null)
                {
                    Clipboard.SetImage(firstImg.Image);
                }
                else
                {
                    var firstTxt = selected.FirstOrDefault(l => !string.IsNullOrEmpty(l.TextContent));
                    if (firstTxt != null)
                    {
                        Clipboard.SetText(firstTxt.TextContent);
                    }
                }
            }
            catch { }
        }

        public void CutSelected()
        {
            CopySelected();
            DeleteActiveLayer();
        }

        public void Paste()
        {
            SaveUndoSnapshot();
            if (_layerClipboard.Count > 0)
            {
                SelectedLayerIds.Clear();
                foreach (var item in _layerClipboard)
                {
                    var clone = item.Clone();
                    clone.Name = item.Name + " (Dán)";
                    clone.Bounds = new RectangleF(clone.Bounds.X + 20f, clone.Bounds.Y + 20f, clone.Bounds.Width, clone.Bounds.Height);
                    if (clone.SubPaths != null)
                    {
                        foreach (var sp in clone.SubPaths)
                        {
                            if (sp.Nodes != null)
                            {
                                foreach (var n in sp.Nodes)
                                {
                                    n.Point = new PointF(n.Point.X + 20f, n.Point.Y + 20f);
                                    n.InHandle = new PointF(n.InHandle.X + 20f, n.InHandle.Y + 20f);
                                    n.OutHandle = new PointF(n.OutHandle.X + 20f, n.OutHandle.Y + 20f);
                                }
                            }
                        }
                    }
                    Layers.Add(clone);
                    SelectedLayerIds.Add(clone.Id);
                    ActiveLayerIndex = Layers.Count - 1;
                }
                return;
            }

            try
            {
                if (Clipboard.ContainsImage())
                {
                    Image clipImg = Clipboard.GetImage();
                    if (clipImg != null)
                    {
                        var imgLayer = new PhotoLayer
                        {
                            Name = "Ảnh Dán",
                            Type = LayerType.Raster,
                            Bounds = new RectangleF(0, 0, clipImg.Width, clipImg.Height),
                            Image = new Bitmap(clipImg)
                        };
                        Layers.Add(imgLayer);
                        ActiveLayerIndex = Layers.Count - 1;
                    }
                }
                else if (Clipboard.ContainsText())
                {
                    string txt = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        AddText(txt, Math.Max(50, Width / 4), Math.Max(50, Height / 4), 36f, Color.FromArgb(30, 41, 59));
                    }
                }
            }
            catch { }
        }

        public bool SetClipForSelected()
        {
            var selected = SelectedLayers;
            if (selected.Count < 2) return false;
            SaveUndoSnapshot();
            var bottom = selected[0];
            var top = selected[1];
            bottom.ClipMask = top.Clone();
            Layers.Remove(top);
            SelectedLayerIds.Clear();
            SelectedLayerIds.Add(bottom.Id);
            ActiveLayerIndex = Layers.IndexOf(bottom);
            return true;
        }

        public bool ReleaseClipActive()
        {
            if (ActiveLayer == null || ActiveLayer.ClipMask == null) return false;
            SaveUndoSnapshot();
            var mask = ActiveLayer.ClipMask;
            ActiveLayer.ClipMask = null;
            int idx = Layers.IndexOf(ActiveLayer);
            Layers.Insert(idx + 1, mask);
            SelectedLayerIds.Clear();
            SelectedLayerIds.Add(mask.Id);
            ActiveLayerIndex = idx + 1;
            return true;
        }

        public bool PutTextOnPath()
        {
            var selected = SelectedLayers;
            if (selected.Count < 2) return false;
            var textLayer = selected.FirstOrDefault(l => l.Type == LayerType.Text);
            var pathLayer = selected.FirstOrDefault(l => l.Type == LayerType.VectorShape);
            if (textLayer == null || pathLayer == null) return false;

            pathLayer.EnsureNodesInitialized();
            if (pathLayer.SubPaths == null || pathLayer.SubPaths.Count == 0) return false;

            SaveUndoSnapshot();
            textLayer.IsTextOnPath = true;
            textLayer.TextPathSubPaths = pathLayer.SubPaths.Select(sp => sp.Clone()).ToList();
            textLayer.Bounds = pathLayer.Bounds;
            return true;
        }

        public bool RemoveTextFromPath()
        {
            if (ActiveLayer == null || ActiveLayer.Type != LayerType.Text || !ActiveLayer.IsTextOnPath) return false;
            SaveUndoSnapshot();
            ActiveLayer.IsTextOnPath = false;
            ActiveLayer.TextPathSubPaths = null;
            return true;
        }

        public void AlignSelected(AlignType alignType)
        {
            var selected = SelectedLayers;
            if (selected.Count == 0 && ActiveLayer != null) selected = new List<PhotoLayer> { ActiveLayer };
            if (selected.Count == 0) return;

            SaveUndoSnapshot();
            if (selected.Count == 1)
            {
                var l = selected[0];
                var b = l.Bounds;
                float newX = b.X;
                float newY = b.Y;
                switch (alignType)
                {
                    case AlignType.Left: newX = 0f; break;
                    case AlignType.CenterH: newX = (Width - b.Width) / 2f; break;
                    case AlignType.Right: newX = Width - b.Width; break;
                    case AlignType.Top: newY = 0f; break;
                    case AlignType.MiddleV: newY = (Height - b.Height) / 2f; break;
                    case AlignType.Bottom: newY = Height - b.Height; break;
                }
                float dx = newX - b.X;
                float dy = newY - b.Y;
                l.Bounds = new RectangleF(newX, newY, b.Width, b.Height);
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
            else
            {
                VectorPathModel.AlignLayers(selected, alignType);
            }
        }

        public void DistributeSelected(DistributeType distType)
        {
            var selected = SelectedLayers;
            if (selected.Count < 3) return;
            SaveUndoSnapshot();
            VectorPathModel.DistributeLayers(selected, distType);
        }

        public void FlipActive(bool horizontal)
        {
            var selected = SelectedLayers;
            if (selected.Count == 0) return;
            SaveUndoSnapshot();
            foreach (var l in selected)
            {
                VectorPathModel.FlipLayer(l, horizontal);
            }
        }

        public void RotateActive90(bool clockwise)
        {
            var selected = SelectedLayers;
            if (selected.Count == 0) return;
            SaveUndoSnapshot();
            foreach (var l in selected)
            {
                VectorPathModel.RotateLayer90(l, clockwise);
            }
        }

        public void RotateActive180()
        {
            RotateActive90(true);
            RotateActive90(true);
        }

        public void MergeDown()
        {
            if (ActiveLayerIndex <= 0) return;
            SaveUndoSnapshot();

            PhotoLayer topLayer = Layers[ActiveLayerIndex];
            PhotoLayer bottomLayer = Layers[ActiveLayerIndex - 1];

            // Render bottom + top together into bottom's raster bitmap
            bottomLayer.EnsureBitmapSize(Width, Height);
            using (Graphics g = Graphics.FromImage(bottomLayer.Image))
            {
                RenderSingleLayerToGdi(g, topLayer);
            }
            bottomLayer.Type = LayerType.Raster;
            bottomLayer.Name = bottomLayer.Name + " + " + topLayer.Name;

            topLayer.Dispose();
            Layers.RemoveAt(ActiveLayerIndex);
            ActiveLayerIndex--;
        }

        public void FlattenImage()
        {
            if (Layers.Count <= 1) return;
            SaveUndoSnapshot();

            Bitmap composite = RenderComposite();
            foreach (var l in Layers) l.Dispose();
            Layers.Clear();

            PhotoLayer flat = new PhotoLayer
            {
                Name = "Hình đã gộp (Flattened)",
                Type = LayerType.Raster,
                Bounds = new RectangleF(0, 0, Width, Height),
                Image = composite
            };
            Layers.Add(flat);
            ActiveLayerIndex = 0;
        }

        public Bitmap RenderComposite(int targetW = 0, int targetH = 0)
        {
            int outW = targetW > 0 ? targetW : Width;
            int outH = targetH > 0 ? targetH : Height;

            Bitmap composite = new Bitmap(outW, outH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(composite))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (targetW > 0 && targetH > 0 && (targetW != Width || targetH != Height))
                {
                    g.ScaleTransform((float)targetW / Width, (float)targetH / Height);
                }

                g.Clear(BackgroundColor);

                for (int i = 0; i < Layers.Count; i++)
                {
                    PhotoLayer l = Layers[i];
                    if (!l.IsVisible) continue;
                    RenderSingleLayerToGdi(g, l);
                }
            }

            return composite;
        }

        public static void RenderSingleLayerToGdi(Graphics g, PhotoLayer l)
        {
            if (!l.IsVisible || l.Opacity <= 0.01f) return;

            GraphicsState state = g.Save();

            // Transform
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
                using (ImageAttributes attr = new ImageAttributes())
                {
                    if (l.Opacity < 0.999f)
                    {
                        ColorMatrix cm = new ColorMatrix();
                        cm.Matrix33 = l.Opacity;
                        attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    }
                    g.DrawImage(l.Image, new Rectangle((int)l.Bounds.X, (int)l.Bounds.Y, (int)l.Bounds.Width, (int)l.Bounds.Height), 0, 0, l.Image.Width, l.Image.Height, GraphicsUnit.Pixel, attr);
                }
            }
            else if (l.Type == LayerType.VectorShape)
            {
                RenderVectorShape(g, l);
            }
            else if (l.Type == LayerType.Text)
            {
                RenderTextLayer(g, l);
            }

            g.Restore(state);
        }

        private static void RenderVectorShape(Graphics g, PhotoLayer l)
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

                if (l.FillType == VectorFillType.LinearGradient && (fill.A > 0 || fill2.A > 0))
                {
                    if (l.Bounds.Width > 1 && l.Bounds.Height > 1)
                    {
                        using (LinearGradientBrush lgb = new LinearGradientBrush(l.Bounds, fill, fill2, l.FillGradientAngle))
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
                    if (l.Bounds.Width > 1 && l.Bounds.Height > 1 && path.PointCount >= 2)
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
                else if (fill.A > 0)
                {
                    using (SolidBrush fillB = new SolidBrush(fill))
                    {
                        g.FillPath(fillB, path);
                    }
                }

                if (stroke.A > 0 && l.StrokeWidth > 0 && l.StrokeType != VectorStrokeType.None)
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

        private static void RenderTextLayer(Graphics g, PhotoLayer l)
        {
            if (string.IsNullOrEmpty(l.TextContent)) return;
            int alpha = (int)(l.Opacity * 255f);
            Color col = Color.FromArgb((int)(alpha * (l.TextColor.A / 255f)), l.TextColor.R, l.TextColor.G, l.TextColor.B);

            using (Font f = new Font(l.FontName ?? "Segoe UI", Math.Max(6f, l.FontSize), l.FontStyle))
            using (SolidBrush b = new SolidBrush(col))
            {
                if (l.IsTextOnPath && l.TextPathSubPaths != null && l.TextPathSubPaths.Count > 0)
                {
                    RenderTextOnPathGdi(g, l, f, b);
                }
                else
                {
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
                    {
                        g.DrawString(l.TextContent, f, b, l.Bounds, sf);
                    }
                }
            }
        }

        public static void RenderTextOnPathGdi(Graphics g, PhotoLayer l, Font font, Brush brush)
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

        public static GraphicsPath CreateShapePath(VectorShapeType type, RectangleF r, float radius = 12f, int pointCount = 5)
        {
            GraphicsPath path = new GraphicsPath();
            float w = Math.Max(2f, r.Width);
            float h = Math.Max(2f, r.Height);

            switch (type)
            {
                case VectorShapeType.Rectangle:
                    if (radius > 0)
                    {
                        float rad = Math.Max(0.5f, Math.Min(radius, Math.Min(w / 2f, h / 2f)));
                        path.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
                        path.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
                        path.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
                        path.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
                        path.CloseFigure();
                    }
                    else
                    {
                        path.AddRectangle(r);
                    }
                    break;

                case VectorShapeType.RoundedRect:
                    float rRad = Math.Max(0.5f, Math.Min(radius, Math.Min(w / 2f, h / 2f)));
                    path.AddArc(r.X, r.Y, rRad * 2, rRad * 2, 180, 90);
                    path.AddArc(r.Right - rRad * 2, r.Y, rRad * 2, rRad * 2, 270, 90);
                    path.AddArc(r.Right - rRad * 2, r.Bottom - rRad * 2, rRad * 2, rRad * 2, 0, 90);
                    path.AddArc(r.X, r.Bottom - rRad * 2, rRad * 2, rRad * 2, 90, 90);
                    path.CloseFigure();
                    break;

                case VectorShapeType.Ellipse:
                    path.AddEllipse(r);
                    break;

                case VectorShapeType.Star:
                    int stars = Math.Max(3, pointCount);
                    PointF[] pts = new PointF[stars * 2];
                    float cx = r.X + w / 2f;
                    float cy = r.Y + h / 2f;
                    float rx = w / 2f;
                    float ry = h / 2f;
                    float irx = rx * 0.45f;
                    float iry = ry * 0.45f;
                    for (int i = 0; i < stars * 2; i++)
                    {
                        double angle = -Math.PI / 2 + i * (Math.PI / stars);
                        float crx = (i % 2 == 0) ? rx : irx;
                        float cry = (i % 2 == 0) ? ry : iry;
                        pts[i] = new PointF((float)(cx + crx * Math.Cos(angle)), (float)(cy + cry * Math.Sin(angle)));
                    }
                    path.AddPolygon(pts);
                    break;

                case VectorShapeType.Polygon:
                    int sides = Math.Max(3, pointCount);
                    PointF[] poly = new PointF[sides];
                    float pcx = r.X + w / 2f;
                    float pcy = r.Y + h / 2f;
                    for (int i = 0; i < sides; i++)
                    {
                        double a = -Math.PI / 2 + i * (Math.PI * 2 / sides);
                        poly[i] = new PointF((float)(pcx + (w / 2f) * Math.Cos(a)), (float)(pcy + (h / 2f) * Math.Sin(a)));
                    }
                    path.AddPolygon(poly);
                    break;

                case VectorShapeType.Arrow:
                    float stemTop = r.Y + h * 0.3f;
                    float stemBot = r.Y + h * 0.7f;
                    float headX = r.X + w * 0.65f;
                    PointF[] arrowPts = new PointF[]
                    {
                        new PointF(r.X, stemTop),
                        new PointF(headX, stemTop),
                        new PointF(headX, r.Y),
                        new PointF(r.Right, r.Y + h / 2f),
                        new PointF(headX, r.Bottom),
                        new PointF(headX, stemBot),
                        new PointF(r.X, stemBot)
                    };
                    path.AddPolygon(arrowPts);
                    break;

                case VectorShapeType.Heart:
                    path.AddBezier(r.X + w / 2f, r.Y + h * 0.35f, r.X + w * 0.25f, r.Y, r.X, r.Y + h * 0.25f, r.X, r.Y + h * 0.55f);
                    path.AddBezier(r.X, r.Y + h * 0.55f, r.X, r.Y + h * 0.75f, r.X + w * 0.35f, r.Bottom, r.X + w / 2f, r.Bottom);
                    path.AddBezier(r.X + w / 2f, r.Bottom, r.Right - w * 0.35f, r.Bottom, r.Right, r.Y + h * 0.75f, r.Right, r.Y + h * 0.55f);
                    path.AddBezier(r.Right, r.Y + h * 0.55f, r.Right, r.Y + h * 0.25f, r.Right - w * 0.25f, r.Y, r.X + w / 2f, r.Y + h * 0.35f);
                    path.CloseFigure();
                    break;

                case VectorShapeType.Line:
                    path.AddLine(r.X, r.Y, r.Right, r.Bottom);
                    break;

                case VectorShapeType.Spiral:
                    int totalSpiralPts = 72;
                    PointF[] spiralPts = new PointF[totalSpiralPts];
                    float spCx = r.X + w / 2f;
                    float spCy = r.Y + h / 2f;
                    float spMaxR = Math.Min(w, h) / 2f;
                    for (int i = 0; i < totalSpiralPts; i++)
                    {
                        float theta = i * (float)(Math.PI * 6) / (totalSpiralPts - 1);
                        float rad = (theta / (float)(Math.PI * 6)) * spMaxR;
                        spiralPts[i] = new PointF(
                            spCx + rad * (float)Math.Cos(theta),
                            spCy + rad * (float)Math.Sin(theta)
                        );
                    }
                    path.AddLines(spiralPts);
                    break;
            }

            return path;
        }

        #region Filters & Pixel Manipulations
        public void ApplyBrightnessContrast(float brightness, float contrast)
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);

            Bitmap bmp = ActiveLayer.Image;
            Bitmap adjusted = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(adjusted))
            using (ImageAttributes attr = new ImageAttributes())
            {
                // Contrast formula: c = contrast; t = (1 - c) / 2
                float c = contrast;
                float t = (1.0f - c) / 2.0f;
                float b = brightness;

                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { c, 0, 0, 0, 0 },
                    new float[] { 0, c, 0, 0, 0 },
                    new float[] { 0, 0, c, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { t + b, t + b, t + b, 0, 1 }
                });

                attr.SetColorMatrix(cm);
                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
            }

            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = adjusted;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplyGrayscale()
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);

            Bitmap bmp = ActiveLayer.Image;
            Bitmap adjusted = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(adjusted))
            using (ImageAttributes attr = new ImageAttributes())
            {
                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
                attr.SetColorMatrix(cm);
                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
            }

            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = adjusted;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplySepia()
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);

            Bitmap bmp = ActiveLayer.Image;
            Bitmap adjusted = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(adjusted))
            using (ImageAttributes attr = new ImageAttributes())
            {
                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { 0.393f, 0.349f, 0.272f, 0, 0 },
                    new float[] { 0.769f, 0.686f, 0.534f, 0, 0 },
                    new float[] { 0.189f, 0.168f, 0.131f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
                attr.SetColorMatrix(cm);
                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
            }

            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = adjusted;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplyInvert()
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);

            Bitmap bmp = ActiveLayer.Image;
            Bitmap adjusted = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(adjusted))
            using (ImageAttributes attr = new ImageAttributes())
            {
                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { -1, 0, 0, 0, 0 },
                    new float[] { 0, -1, 0, 0, 0 },
                    new float[] { 0, 0, -1, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 1, 1, 1, 0, 1 }
                });
                attr.SetColorMatrix(cm);
                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
            }

            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = adjusted;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplyVignette(float intensity = 0.5f)
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);

            using (Graphics g = Graphics.FromImage(ActiveLayer.Image))
            using (GraphicsPath vp = new GraphicsPath())
            {
                vp.AddEllipse(0, 0, Width, Height);
                using (PathGradientBrush pgb = new PathGradientBrush(vp))
                {
                    pgb.CenterColor = Color.Transparent;
                    pgb.SurroundColors = new Color[] { Color.FromArgb((int)(intensity * 200), 0, 0, 0) };
                    g.FillRectangle(pgb, 0, 0, Width, Height);
                }
            }
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplyBlur(int radius = 3)
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);
            Bitmap src = ActiveLayer.Image;
            Bitmap blurred = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            int dw = Math.Max(1, src.Width / Math.Max(2, radius * 2));
            int dh = Math.Max(1, src.Height / Math.Max(2, radius * 2));
            using (Bitmap small = new Bitmap(dw, dh, PixelFormat.Format32bppArgb))
            {
                using (Graphics g1 = Graphics.FromImage(small))
                {
                    g1.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g1.DrawImage(src, 0, 0, dw, dh);
                }
                using (Graphics g2 = Graphics.FromImage(blurred))
                {
                    g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g2.DrawImage(small, 0, 0, src.Width, src.Height);
                }
            }
            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = blurred;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplySharpen()
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);
            Bitmap src = ActiveLayer.Image;
            Bitmap sharp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            int w = src.Width, h = src.Height;
            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = sharp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* pSrc = (byte*)srcData.Scan0;
                    byte* pDst = (byte*)dstData.Scan0;
                    int stride = srcData.Stride;
                    for (int y = 1; y < h - 1; y++)
                    {
                        for (int x = 1; x < w - 1; x++)
                        {
                            int idx = y * stride + x * 4;
                            for (int c = 0; c < 3; c++)
                            {
                                int center = pSrc[idx + c];
                                int top = pSrc[(y - 1) * stride + x * 4 + c];
                                int bottom = pSrc[(y + 1) * stride + x * 4 + c];
                                int left = pSrc[y * stride + (x - 1) * 4 + c];
                                int right = pSrc[y * stride + (x + 1) * 4 + c];
                                int val = 5 * center - (top + bottom + left + right);
                                pDst[idx + c] = (byte)Math.Max(0, Math.Min(255, val));
                            }
                            pDst[idx + 3] = pSrc[idx + 3];
                        }
                    }
                }
            }
            finally
            {
                src.UnlockBits(srcData);
                sharp.UnlockBits(dstData);
            }
            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = sharp;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ApplyHueSaturation(float hueShift, float satScale, float lightShift)
        {
            ApplyHueSaturation(satScale, lightShift);
        }

        public void ApplyHueSaturation(float satScale, float lightShift)
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.EnsureBitmapSize(Width, Height);
            Bitmap bmp = ActiveLayer.Image;
            Bitmap adjusted = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(adjusted))
            using (ImageAttributes attr = new ImageAttributes())
            {
                float s = satScale;
                float rw = 0.3086f * (1.0f - s);
                float gw = 0.6094f * (1.0f - s);
                float bw = 0.0820f * (1.0f - s);
                float l = lightShift;
                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { rw + s, rw, rw, 0, 0 },
                    new float[] { gw, gw + s, gw, 0, 0 },
                    new float[] { bw, bw, bw + s, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { l, l, l, 0, 1 }
                });
                attr.SetColorMatrix(cm);
                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
            }
            ActiveLayer.Image.Dispose();
            ActiveLayer.Image = adjusted;
            ActiveLayer.Type = LayerType.Raster;
        }

        public void ToggleActiveLayerShadow()
        {
            if (ActiveLayer == null) return;
            SaveUndoSnapshot();
            ActiveLayer.HasShadow = !ActiveLayer.HasShadow;
        }

        public void ResizeCanvas(int newW, int newH, bool centerContent = true)
        {
            if (newW <= 10 || newH <= 10) return;
            SaveUndoSnapshot();
            float dx = centerContent ? (newW - Width) / 2f : 0f;
            float dy = centerContent ? (newH - Height) / 2f : 0f;

            foreach (var l in Layers)
            {
                if (l.Type == LayerType.Raster && l.Image != null)
                {
                    Bitmap newBmp = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(newBmp))
                    {
                        g.Clear(Color.Transparent);
                        g.DrawImage(l.Image, dx, dy);
                    }
                    l.Image.Dispose();
                    l.Image = newBmp;
                }
                l.Bounds = new RectangleF(l.Bounds.X + dx, l.Bounds.Y + dy, l.Bounds.Width, l.Bounds.Height);
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
            Width = newW;
            Height = newH;
        }

        public void CropCanvas(Rectangle cropRect)
        {
            if (cropRect.Width <= 10 || cropRect.Height <= 10) return;
            SaveUndoSnapshot();

            int newW = cropRect.Width;
            int newH = cropRect.Height;

            foreach (var l in Layers)
            {
                if (l.Type == LayerType.Raster && l.Image != null)
                {
                    Bitmap newBmp = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(newBmp))
                    {
                        g.Clear(Color.Transparent);
                        g.DrawImage(l.Image, -cropRect.X, -cropRect.Y);
                    }
                    l.Image.Dispose();
                    l.Image = newBmp;
                }
                l.Bounds = new RectangleF(l.Bounds.X - cropRect.X, l.Bounds.Y - cropRect.Y, l.Bounds.Width, l.Bounds.Height);
            }

            Width = newW;
            Height = newH;
        }
        #endregion

        #region Open & Save
        public static PhotoDocument LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".svg")
            {
                try
                {
                    string svgContent = File.ReadAllText(filePath, Encoding.UTF8);
                    int docW = 1920;
                    int docH = 1080;
                    try
                    {
                        var xdoc = XDocument.Parse(svgContent);
                        var root = xdoc.Root;
                        if (root != null)
                        {
                            string wStr = root.Attribute("width")?.Value;
                            string hStr = root.Attribute("height")?.Value;
                            if (!string.IsNullOrEmpty(wStr) && float.TryParse(wStr.Replace("px", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float pwRoot) && pwRoot > 0)
                                docW = (int)Math.Ceiling(pwRoot);
                            if (!string.IsNullOrEmpty(hStr) && float.TryParse(hStr.Replace("px", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float phRoot) && phRoot > 0)
                                docH = (int)Math.Ceiling(phRoot);

                            if (docW <= 0 || docH <= 0 || (docW == 1920 && docH == 1080))
                            {
                                string vb = root.Attribute("viewBox")?.Value;
                                if (!string.IsNullOrEmpty(vb))
                                {
                                    var parts = vb.Split(new char[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 4 && float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float pw) &&
                                        float.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float ph) && pw > 0 && ph > 0)
                                    {
                                        docW = (int)Math.Ceiling(pw);
                                        docH = (int)Math.Ceiling(ph);
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    PhotoDocument doc = new PhotoDocument(docW, docH, Path.GetFileName(filePath));
                    doc.FilePath = filePath;
                    doc.Layers.Clear();

                    // Khởi tạo Lớp nền chuẩn để đảm bảo độ tương phản đồ họa (tránh canvas bị tối/đen)
                    PhotoLayer bg = new PhotoLayer
                    {
                        Name = "Lớp nền",
                        Type = LayerType.Raster,
                        Bounds = new RectangleF(0, 0, docW, docH),
                        Image = new Bitmap(docW, docH, PixelFormat.Format32bppArgb)
                    };
                    using (Graphics g = Graphics.FromImage(bg.Image))
                    {
                        g.Clear(Color.White);
                    }
                    doc.Layers.Add(bg);

                    Bitmap svgBmp = ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(svgContent, docW, docH);
                    if (svgBmp == null)
                    {
                        svgBmp = ZenQOffice.Core.SvgRasterizer.RenderSvgToBitmap(filePath, docW, docH);
                    }

                    if (svgBmp != null)
                    {
                        doc.Width = svgBmp.Width;
                        doc.Height = svgBmp.Height;

                        if (bg.Image == null || bg.Image.Width != svgBmp.Width || bg.Image.Height != svgBmp.Height)
                        {
                            bg.Image?.Dispose();
                            bg.Image = new Bitmap(svgBmp.Width, svgBmp.Height, PixelFormat.Format32bppArgb);
                            using (Graphics g = Graphics.FromImage(bg.Image)) g.Clear(Color.White);
                        }
                        bg.Bounds = new RectangleF(0, 0, svgBmp.Width, svgBmp.Height);

                        PhotoLayer svgLayer = new PhotoLayer
                        {
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            Type = LayerType.Raster,
                            Bounds = new RectangleF(0, 0, svgBmp.Width, svgBmp.Height),
                            Image = svgBmp,
                            Opacity = 1.0f,
                            IsVisible = true
                        };
                        doc.Layers.Add(svgLayer);
                        doc.ActiveLayerIndex = 1;
                    }
                    else
                    {
                        doc.InitDefault();
                    }

                    return doc;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Load SVG error: " + ex.Message);
                    return null;
                }
            }
            else if (ext == ".psd")
            {
                PsdDocumentData psdData = PsdHandler.LoadPsd(filePath);
                if (psdData == null) return null;

                PhotoDocument doc = new PhotoDocument(psdData.Width, psdData.Height, Path.GetFileName(filePath));
                doc.FilePath = filePath;
                doc.Layers.Clear();

                if (psdData.Layers.Count > 0)
                {
                    foreach (var pl in psdData.Layers)
                    {
                        PhotoLayer layer = new PhotoLayer
                        {
                            Name = pl.Name,
                            Type = LayerType.Raster,
                            IsVisible = pl.IsVisible,
                            Opacity = pl.Opacity / 255.0f,
                            Bounds = new RectangleF(pl.Left, pl.Top, pl.Width, pl.Height),
                            Image = pl.Bitmap
                        };
                        doc.Layers.Add(layer);
                    }
                }
                else if (psdData.CompositeImage != null)
                {
                    PhotoLayer compositeLayer = new PhotoLayer
                    {
                        Name = "Hình tổng hợp (Composite)",
                        Type = LayerType.Raster,
                        Bounds = new RectangleF(0, 0, psdData.Width, psdData.Height),
                        Image = psdData.CompositeImage
                    };
                    doc.Layers.Add(compositeLayer);
                }

                doc.ActiveLayerIndex = Math.Max(0, doc.Layers.Count - 1);
                return doc;
            }
            else if (ext == ".svg")
            {
                return ImportFromSvg(filePath);
            }
            else
            {
                // Standard image file (PNG, JPG, BMP, TIFF, WebP, GIF)
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (Image rawImg = Image.FromStream(fs))
                    {
                        int w = rawImg.Width;
                        int h = rawImg.Height;
                        PhotoDocument doc = new PhotoDocument(w, h, Path.GetFileName(filePath));
                        doc.FilePath = filePath;
                        doc.Layers.Clear();

                        Bitmap bmp = new Bitmap(rawImg);
                        PhotoLayer layer = new PhotoLayer
                        {
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            Type = LayerType.Raster,
                            Bounds = new RectangleF(0, 0, w, h),
                            Image = bmp
                        };
                        doc.Layers.Add(layer);
                        doc.ActiveLayerIndex = 0;
                        return doc;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Load image error: " + ex.Message);
                    return null;
                }
            }
        }

        public bool SaveToFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".svg")
                {
                    return ExportToSvg(filePath);
                }

                Bitmap composite = RenderComposite();

                if (ext == ".psd")
                {
                    bool ok = PsdHandler.SavePsd(filePath, composite);
                    composite.Dispose();
                    if (ok)
                    {
                        FilePath = filePath;
                        Title = Path.GetFileName(filePath);
                    }
                    return ok;
                }
                else if (ext == ".png")
                {
                    composite.Save(filePath, ImageFormat.Png);
                }
                else if (ext == ".jpg" || ext == ".jpeg")
                {
                    composite.Save(filePath, ImageFormat.Jpeg);
                }
                else if (ext == ".bmp")
                {
                    composite.Save(filePath, ImageFormat.Bmp);
                }
                else if (ext == ".tiff" || ext == ".tif")
                {
                    composite.Save(filePath, ImageFormat.Tiff);
                }
                else if (ext == ".pdf")
                {
                    var pdfDoc = new ZenQOffice.Core.PdfDocument(Path.GetFileName(filePath));
                    pdfDoc.Pages.Clear();
                    float pdfW = Width > 0 ? Width : 1920f;
                    float pdfH = Height > 0 ? Height : 1080f;
                    var page = new ZenQOffice.Core.PdfPageModel(1, pdfW, pdfH);
                    page.ImageElements.Add(new ZenQOffice.Core.PdfImageElement((Bitmap)composite.Clone(), new RectangleF(0, 0, pdfW, pdfH)));
                    pdfDoc.Pages.Add(page);
                    bool ok = ZenQOffice.Format.PdfHandler.SavePdf(pdfDoc, filePath);
                    composite.Dispose();
                    if (ok)
                    {
                        FilePath = filePath;
                        Title = Path.GetFileName(filePath);
                    }
                    return ok;
                }
                else
                {
                    // Default to PNG
                    composite.Save(filePath, ImageFormat.Png);
                }

                composite.Dispose();
                FilePath = filePath;
                Title = Path.GetFileName(filePath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save image error: " + ex.Message);
                return false;
            }
        }

        public bool ExportToSvg(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
                sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"{Width}\" height=\"{Height}\" viewBox=\"0 0 {Width} {Height}\">");

                // 1. Defs Block: Gradients, ClipPaths, TextPaths
                sb.AppendLine("  <defs>");
                for (int i = 0; i < Layers.Count; i++)
                {
                    var l = Layers[i];
                    if (!l.IsVisible) continue;

                    // Gradients
                    if (l.Type == LayerType.VectorShape)
                    {
                        if (l.FillType == VectorFillType.LinearGradient)
                        {
                            double rad = l.FillGradientAngle * Math.PI / 180.0;
                            double x1 = 50.0 - 50.0 * Math.Cos(rad);
                            double y1 = 50.0 - 50.0 * Math.Sin(rad);
                            double x2 = 50.0 + 50.0 * Math.Cos(rad);
                            double y2 = 50.0 + 50.0 * Math.Sin(rad);
                            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "    <linearGradient id=\"grad_{0}\" x1=\"{1:0.#}%\" y1=\"{2:0.#}%\" x2=\"{3:0.#}%\" y2=\"{4:0.#}%\">",
                                i, x1, y1, x2, y2));
                            if (l.GradientStops != null && l.GradientStops.Count >= 2)
                            {
                                foreach (var st in l.GradientStops.OrderBy(s => s.Position))
                                {
                                    sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "      <stop offset=\"{0:0.#}%\" stop-color=\"{1}\" stop-opacity=\"{2:0.##}\" />",
                                        st.Position * 100.0, ColorToHex(st.Color), (st.Color.A / 255.0) * l.Opacity));
                                }
                            }
                            else
                            {
                                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "      <stop offset=\"0%\" stop-color=\"{0}\" stop-opacity=\"{1:0.##}\" />",
                                    ColorToHex(l.FillColor), (l.FillColor.A / 255.0) * l.Opacity));
                                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "      <stop offset=\"100%\" stop-color=\"{0}\" stop-opacity=\"{1:0.##}\" />",
                                    ColorToHex(l.FillColor2), (l.FillColor2.A / 255.0) * l.Opacity));
                            }
                            sb.AppendLine("    </linearGradient>");
                        }
                        else if (l.FillType == VectorFillType.RadialGradient)
                        {
                            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "    <radialGradient id=\"grad_{0}\" cx=\"50%\" cy=\"50%\" r=\"50%\" fx=\"50%\" fy=\"50%\">", i));
                            if (l.GradientStops != null && l.GradientStops.Count >= 2)
                            {
                                foreach (var st in l.GradientStops.OrderBy(s => s.Position))
                                {
                                    sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "      <stop offset=\"{0:0.#}%\" stop-color=\"{1}\" stop-opacity=\"{2:0.##}\" />",
                                        st.Position * 100.0, ColorToHex(st.Color), (st.Color.A / 255.0) * l.Opacity));
                                }
                            }
                            else
                            {
                                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "      <stop offset=\"0%\" stop-color=\"{0}\" stop-opacity=\"{1:0.##}\" />",
                                    ColorToHex(l.FillColor), (l.FillColor.A / 255.0) * l.Opacity));
                                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "      <stop offset=\"100%\" stop-color=\"{0}\" stop-opacity=\"{1:0.##}\" />",
                                    ColorToHex(l.FillColor2), (l.FillColor2.A / 255.0) * l.Opacity));
                            }
                            sb.AppendLine("    </radialGradient>");
                        }
                    }

                    // Clip Mask
                    if (l.ClipMask != null)
                    {
                        l.ClipMask.EnsureNodesInitialized();
                        string clipD = BuildSvgPathData(l.ClipMask);
                        if (!string.IsNullOrEmpty(clipD))
                        {
                            sb.AppendLine(string.Format("    <clipPath id=\"clip_{0}\">", i));
                            sb.AppendLine(string.Format("      <path d=\"{0}\" />", clipD));
                            sb.AppendLine("    </clipPath>");
                        }
                    }

                    // Text on Path
                    if (l.Type == LayerType.Text && l.IsTextOnPath && l.TextPathSubPaths != null && l.TextPathSubPaths.Count > 0)
                    {
                        string tpD = BuildSvgPathData(l.TextPathSubPaths);
                        if (!string.IsNullOrEmpty(tpD))
                        {
                            sb.AppendLine(string.Format("    <path id=\"tp_{0}\" d=\"{1}\" fill=\"none\" />", i, tpD));
                        }
                    }
                }
                sb.AppendLine("  </defs>");

                if (BackgroundColor != Color.Transparent && BackgroundColor.A > 0)
                {
                    string bgHex = ColorToHex(BackgroundColor);
                    sb.AppendLine($"  <rect width=\"{Width}\" height=\"{Height}\" fill=\"{bgHex}\" />");
                }

                for (int i = 0; i < Layers.Count; i++)
                {
                    var l = Layers[i];
                    if (!l.IsVisible) continue;
                    float op = l.Opacity;
                    string opAttr = (op < 0.999f) ? $" opacity=\"{op.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\"" : "";
                    string clipAttr = (l.ClipMask != null) ? $" clip-path=\"url(#clip_{i})\"" : "";

                    string transformAttr = "";
                    if (l.Rotation != 0f)
                    {
                        float cx = l.Bounds.X + l.Bounds.Width / 2f;
                        float cy = l.Bounds.Y + l.Bounds.Height / 2f;
                        transformAttr = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            " transform=\"rotate({0:0.##}, {1:0.##}, {2:0.##})\"", l.Rotation, cx, cy);
                    }

                    bool hasWrapperG = !string.IsNullOrEmpty(transformAttr) || !string.IsNullOrEmpty(clipAttr);
                    if (hasWrapperG)
                    {
                        sb.AppendLine($"  <g{transformAttr}{clipAttr}>");
                    }

                    if (l.Type == LayerType.VectorShape)
                    {
                        l.EnsureNodesInitialized();
                        string fillAttr;
                        if (l.FillType == VectorFillType.LinearGradient || l.FillType == VectorFillType.RadialGradient)
                        {
                            fillAttr = $"fill=\"url(#grad_{i})\"";
                        }
                        else if (l.FillType == VectorFillType.Solid && l.FillColor.A > 0)
                        {
                            fillAttr = $"fill=\"{ColorToHex(l.FillColor)}\"";
                            if (l.FillColor.A < 255)
                            {
                                fillAttr += $" fill-opacity=\"{(l.FillColor.A / 255f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\"";
                            }
                        }
                        else
                        {
                            fillAttr = "fill=\"none\"";
                        }

                        string strokeAttr = "stroke=\"none\"";
                        if (l.StrokeWidth > 0 && l.StrokeColor.A > 0 && l.StrokeType != VectorStrokeType.None)
                        {
                            string strokeHex = ColorToHex(l.StrokeColor);
                            string sw = l.StrokeWidth.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                            string cap = l.StrokeLineCap.ToString().ToLowerInvariant();
                            string join = l.StrokeLineJoin.ToString().ToLowerInvariant();
                            string dash = "";
                            if (l.StrokeDashStyle == DashStyle.Dash) dash = " stroke-dasharray=\"6,4\"";
                            else if (l.StrokeDashStyle == DashStyle.Dot) dash = " stroke-dasharray=\"2,2\"";
                            else if (l.StrokeDashStyle == DashStyle.DashDot) dash = " stroke-dasharray=\"6,3,2,3\"";

                            strokeAttr = $"stroke=\"{strokeHex}\" stroke-width=\"{sw}\" stroke-linecap=\"{cap}\" stroke-linejoin=\"{join}\"{dash}";
                            if (l.StrokeColor.A < 255)
                            {
                                strokeAttr += $" stroke-opacity=\"{(l.StrokeColor.A / 255f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\"";
                            }
                        }

                        string pathD = BuildSvgPathData(l);
                        if (!string.IsNullOrEmpty(pathD))
                        {
                            sb.AppendLine($"    <path d=\"{pathD}\" {fillAttr} {strokeAttr}{opAttr} />");
                        }
                    }
                    else if (l.Type == LayerType.Text)
                    {
                        string fillHex = ColorToHex(l.TextColor);
                        string escapedText = System.Security.SecurityElement.Escape(l.TextContent ?? "");
                        string fontFam = l.FontName ?? "Segoe UI";
                        string fontStyleAttr = "";
                        if ((l.FontStyle & FontStyle.Bold) == FontStyle.Bold) fontStyleAttr += " font-weight=\"bold\"";
                        if ((l.FontStyle & FontStyle.Italic) == FontStyle.Italic) fontStyleAttr += " font-style=\"italic\"";

                        if (l.IsTextOnPath && l.TextPathSubPaths != null && l.TextPathSubPaths.Count > 0)
                        {
                            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "    <text font-family=\"{0}\" font-size=\"{1:0.##}\" fill=\"{2}\"{3}{4}><textPath href=\"#tp_{5}\">{6}</textPath></text>",
                                fontFam, l.FontSize, fillHex, fontStyleAttr, opAttr, i, escapedText));
                        }
                        else
                        {
                            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "    <text x=\"{0:0.##}\" y=\"{1:0.##}\" font-family=\"{2}\" font-size=\"{3:0.##}\" fill=\"{4}\"{5}{6}>{7}</text>",
                                l.Bounds.X, l.Bounds.Y + l.FontSize, fontFam, l.FontSize, fillHex, fontStyleAttr, opAttr, escapedText));
                        }
                    }
                    else if (l.Image != null)
                    {
                        if (l.IsLinkedImage && !string.IsNullOrEmpty(l.ImagePath))
                        {
                            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "    <image x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{3:0.##}\" href=\"{4}\"{5} />",
                                l.Bounds.X, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height, System.Security.SecurityElement.Escape(l.ImagePath), opAttr));
                        }
                        else
                        {
                            using (var ms = new MemoryStream())
                            {
                                l.Image.Save(ms, ImageFormat.Png);
                                string b64 = Convert.ToBase64String(ms.ToArray());
                                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "    <image x=\"{0:0.##}\" y=\"{1:0.##}\" width=\"{2:0.##}\" height=\"{3:0.##}\" href=\"data:image/png;base64,{4}\"{5} />",
                                    l.Bounds.X, l.Bounds.Y, l.Bounds.Width, l.Bounds.Height, b64, opAttr));
                            }
                        }
                    }

                    if (hasWrapperG)
                    {
                        sb.AppendLine("  </g>");
                    }
                }

                sb.AppendLine("</svg>");
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                FilePath = filePath;
                Title = Path.GetFileName(filePath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ExportToSvg error: " + ex.Message);
                return false;
            }
        }

        public static string BuildSvgPathData(PhotoLayer l)
        {
            if (l == null) return "";
            l.EnsureNodesInitialized();
            return BuildSvgPathData(l.SubPaths);
        }

        public static string BuildSvgPathData(List<VectorSubPath> subpaths)
        {
            if (subpaths == null || subpaths.Count == 0) return "";
            var pathData = new StringBuilder();

            foreach (var sp in subpaths)
            {
                if (sp.Nodes == null || sp.Nodes.Count == 0) continue;
                var n0 = sp.Nodes[0];
                pathData.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "M {0:0.##},{1:0.##} ", n0.Point.X, n0.Point.Y));

                int count = sp.Nodes.Count;
                int segCount = sp.IsClosed ? count : count - 1;
                for (int i = 0; i < segCount; i++)
                {
                    var n1 = sp.Nodes[i];
                    var n2 = sp.Nodes[(i + 1) % count];
                    if (!n1.HasOutHandle && !n2.HasInHandle)
                    {
                        pathData.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "L {0:0.##},{1:0.##} ", n2.Point.X, n2.Point.Y));
                    }
                    else
                    {
                        PointF p1 = n1.HasOutHandle ? n1.OutHandle : new PointF(n1.Point.X + (n2.Point.X - n1.Point.X) / 3f, n1.Point.Y + (n2.Point.Y - n1.Point.Y) / 3f);
                        PointF p2 = n2.HasInHandle ? n2.InHandle : new PointF(n2.Point.X - (n2.Point.X - n1.Point.X) / 3f, n2.Point.Y - (n2.Point.Y - n1.Point.Y) / 3f);
                        pathData.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "C {0:0.##},{1:0.##} {2:0.##},{3:0.##} {4:0.##},{5:0.##} ", p1.X, p1.Y, p2.X, p2.Y, n2.Point.X, n2.Point.Y));
                    }
                }
                if (sp.IsClosed) pathData.Append("Z ");
            }

            return pathData.ToString().TrimEnd();
        }

        private static string ColorToHex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        public static PhotoDocument ImportFromSvg(string filePath)
        {
            try
            {
                var xdoc = XDocument.Load(filePath);
                var svgRoot = xdoc.Root;
                if (svgRoot == null || svgRoot.Name.LocalName.ToLowerInvariant() != "svg") return null;

                float docW = 1920f;
                float docH = 1080f;

                string wAttr = svgRoot.Attribute("width")?.Value;
                string hAttr = svgRoot.Attribute("height")?.Value;
                string vbAttr = svgRoot.Attribute("viewBox")?.Value;

                if (!string.IsNullOrEmpty(vbAttr))
                {
                    var parts = vbAttr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        if (float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vbw)) docW = vbw;
                        if (float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vbh)) docH = vbh;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(wAttr)) float.TryParse(wAttr.Replace("px", "").Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out docW);
                    if (!string.IsNullOrEmpty(hAttr)) float.TryParse(hAttr.Replace("px", "").Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out docH);
                }

                if (docW < 10) docW = 800;
                if (docH < 10) docH = 600;

                PhotoDocument doc = new PhotoDocument((int)docW, (int)docH, Path.GetFileName(filePath));
                doc.FilePath = filePath;
                doc.Layers.Clear();

                var elements = svgRoot.Descendants().Where(e =>
                {
                    string name = e.Name.LocalName.ToLowerInvariant();
                    return name is "path" or "rect" or "circle" or "ellipse" or "line" or "polygon" or "polyline" or "text" or "image";
                });

                foreach (var el in elements)
                {
                    string name = el.Name.LocalName.ToLowerInvariant();
                    Color fill = ParseSvgColor(GetSvgAttrOrStyle(el, "fill"), Color.Black);
                    Color stroke = ParseSvgColor(GetSvgAttrOrStyle(el, "stroke"), Color.Transparent);
                    float strokeW = ParseSvgFloat(GetSvgAttrOrStyle(el, "stroke-width"), 0f);
                    float op = ParseSvgFloat(GetSvgAttrOrStyle(el, "opacity"), 1f);

                    if (name == "path")
                    {
                        string d = el.Attribute("d")?.Value;
                        if (!string.IsNullOrEmpty(d))
                        {
                            var subs = ParseSvgPath(d);
                            if (subs.Count > 0)
                            {
                                PhotoLayer layer = new PhotoLayer
                                {
                                    Name = el.Attribute("id")?.Value ?? "Đường vẽ SVG",
                                    Type = LayerType.VectorShape,
                                    ShapeType = VectorShapeType.Path,
                                    FillColor = fill,
                                    FillType = fill.A > 0 ? VectorFillType.Solid : VectorFillType.None,
                                    StrokeColor = stroke,
                                    StrokeWidth = strokeW,
                                    StrokeType = (strokeW > 0 && stroke.A > 0) ? VectorStrokeType.Solid : VectorStrokeType.None,
                                    Opacity = op,
                                    SubPaths = subs
                                };
                                VectorPathModel.UpdateLayerBounds(layer);
                                doc.Layers.Add(layer);
                            }
                        }
                    }
                    else if (name == "rect")
                    {
                        float rx = ParseSvgFloat(el.Attribute("x")?.Value, 0f);
                        float ry = ParseSvgFloat(el.Attribute("y")?.Value, 0f);
                        float rw = ParseSvgFloat(el.Attribute("width")?.Value, 100f);
                        float rh = ParseSvgFloat(el.Attribute("height")?.Value, 100f);
                        float cornerR = ParseSvgFloat(el.Attribute("rx")?.Value, 0f);

                        PhotoLayer layer = new PhotoLayer
                        {
                            Name = el.Attribute("id")?.Value ?? "Hình chữ nhật SVG",
                            Type = LayerType.VectorShape,
                            ShapeType = cornerR > 0 ? VectorShapeType.RoundedRect : VectorShapeType.Rectangle,
                            Bounds = new RectangleF(rx, ry, rw, rh),
                            CornerRadius = cornerR,
                            FillColor = fill,
                            FillType = fill.A > 0 ? VectorFillType.Solid : VectorFillType.None,
                            StrokeColor = stroke,
                            StrokeWidth = strokeW,
                            StrokeType = (strokeW > 0 && stroke.A > 0) ? VectorStrokeType.Solid : VectorStrokeType.None,
                            Opacity = op
                        };
                        layer.EnsureNodesInitialized();
                        doc.Layers.Add(layer);
                    }
                    else if (name is "circle" or "ellipse")
                    {
                        float cx = ParseSvgFloat(el.Attribute("cx")?.Value, 0f);
                        float cy = ParseSvgFloat(el.Attribute("cy")?.Value, 0f);
                        float rx = name == "circle" ? ParseSvgFloat(el.Attribute("r")?.Value, 50f) : ParseSvgFloat(el.Attribute("rx")?.Value, 50f);
                        float ry = name == "circle" ? rx : ParseSvgFloat(el.Attribute("ry")?.Value, 50f);

                        PhotoLayer layer = new PhotoLayer
                        {
                            Name = el.Attribute("id")?.Value ?? "Hình tròn SVG",
                            Type = LayerType.VectorShape,
                            ShapeType = VectorShapeType.Ellipse,
                            Bounds = new RectangleF(cx - rx, cy - ry, rx * 2f, ry * 2f),
                            FillColor = fill,
                            FillType = fill.A > 0 ? VectorFillType.Solid : VectorFillType.None,
                            StrokeColor = stroke,
                            StrokeWidth = strokeW,
                            StrokeType = (strokeW > 0 && stroke.A > 0) ? VectorStrokeType.Solid : VectorStrokeType.None,
                            Opacity = op
                        };
                        layer.EnsureNodesInitialized();
                        doc.Layers.Add(layer);
                    }
                    else if (name == "text")
                    {
                        float tx = ParseSvgFloat(el.Attribute("x")?.Value, 0f);
                        float ty = ParseSvgFloat(el.Attribute("y")?.Value, 0f);
                        float fsz = ParseSvgFloat(GetSvgAttrOrStyle(el, "font-size"), 18f);
                        string ffamily = GetSvgAttrOrStyle(el, "font-family") ?? "Segoe UI";
                        string content = el.Value;

                        PhotoLayer layer = new PhotoLayer
                        {
                            Name = "Chữ SVG: " + (content.Length > 10 ? content.Substring(0, 10) + "..." : content),
                            Type = LayerType.Text,
                            Text = content,
                            FontFamily = ffamily,
                            FontSize = fsz,
                            TextColor = fill,
                            Bounds = new RectangleF(tx, Math.Max(0, ty - fsz), 300, fsz * 1.5f),
                            Opacity = op
                        };
                        doc.Layers.Add(layer);
                    }
                }

                if (doc.Layers.Count == 0)
                {
                    doc.Layers.Add(new PhotoLayer { Name = "Lớp 1", Type = LayerType.Raster, Bounds = new RectangleF(0, 0, docW, docH) });
                }

                doc.ActiveLayerIndex = doc.Layers.Count - 1;
                return doc;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ImportFromSvg error: " + ex.Message);
                return null;
            }
        }

        public static List<VectorSubPath> ParseSvgPath(string d)
        {
            var subPaths = new List<VectorSubPath>();
            if (string.IsNullOrWhiteSpace(d)) return subPaths;

            VectorSubPath currentSubPath = null;
            PointF currentPt = PointF.Empty;
            PointF startPt = PointF.Empty;
            PointF lastCubicControl = PointF.Empty;

            var matches = System.Text.RegularExpressions.Regex.Matches(d, @"([a-zA-Z])|([-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?)");
            int idx = 0;
            char curCmd = 'M';

            float ReadFloat()
            {
                if (idx < matches.Count && !matches[idx].Groups[1].Success)
                {
                    float.TryParse(matches[idx].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val);
                    idx++;
                    return val;
                }
                return 0f;
            }

            bool HasMoreNumbers()
            {
                return idx < matches.Count && !matches[idx].Groups[1].Success;
            }

            while (idx < matches.Count)
            {
                if (matches[idx].Groups[1].Success)
                {
                    curCmd = matches[idx].Value[0];
                    idx++;
                }

                bool isRel = char.IsLower(curCmd);
                char cmdUpper = char.ToUpperInvariant(curCmd);

                switch (cmdUpper)
                {
                    case 'M':
                        float mx = ReadFloat();
                        float my = ReadFloat();
                        if (isRel) { mx += currentPt.X; my += currentPt.Y; }
                        currentPt = new PointF(mx, my);
                        startPt = currentPt;
                        currentSubPath = new VectorSubPath(false);
                        currentSubPath.Nodes.Add(new VectorPathNode(currentPt, VectorNodeType.Corner));
                        subPaths.Add(currentSubPath);
                        lastCubicControl = currentPt;
                        curCmd = isRel ? 'l' : 'L';
                        break;

                    case 'L':
                        while (HasMoreNumbers())
                        {
                            float lx = ReadFloat();
                            float ly = ReadFloat();
                            if (isRel) { lx += currentPt.X; ly += currentPt.Y; }
                            currentPt = new PointF(lx, ly);
                            if (currentSubPath == null)
                            {
                                currentSubPath = new VectorSubPath(false);
                                subPaths.Add(currentSubPath);
                            }
                            currentSubPath.Nodes.Add(new VectorPathNode(currentPt, VectorNodeType.Corner));
                            lastCubicControl = currentPt;
                        }
                        break;

                    case 'H':
                        while (HasMoreNumbers())
                        {
                            float hx = ReadFloat();
                            if (isRel) hx += currentPt.X;
                            currentPt = new PointF(hx, currentPt.Y);
                            if (currentSubPath != null) currentSubPath.Nodes.Add(new VectorPathNode(currentPt, VectorNodeType.Corner));
                            lastCubicControl = currentPt;
                        }
                        break;

                    case 'V':
                        while (HasMoreNumbers())
                        {
                            float vy = ReadFloat();
                            if (isRel) vy += currentPt.Y;
                            currentPt = new PointF(currentPt.X, vy);
                            if (currentSubPath != null) currentSubPath.Nodes.Add(new VectorPathNode(currentPt, VectorNodeType.Corner));
                            lastCubicControl = currentPt;
                        }
                        break;

                    case 'C':
                        while (HasMoreNumbers())
                        {
                            float x1 = ReadFloat(); float y1 = ReadFloat();
                            float x2 = ReadFloat(); float y2 = ReadFloat();
                            float x = ReadFloat(); float y = ReadFloat();
                            if (isRel)
                            {
                                x1 += currentPt.X; y1 += currentPt.Y;
                                x2 += currentPt.X; y2 += currentPt.Y;
                                x += currentPt.X; y += currentPt.Y;
                            }
                            if (currentSubPath != null && currentSubPath.Nodes.Count > 0)
                            {
                                var prevNode = currentSubPath.Nodes[currentSubPath.Nodes.Count - 1];
                                prevNode.OutHandle = new PointF(x1, y1);
                                prevNode.HasOutHandle = true;
                                prevNode.NodeType = VectorNodeType.Smooth;

                                var newNode = new VectorPathNode(new PointF(x, y), VectorNodeType.Smooth);
                                newNode.InHandle = new PointF(x2, y2);
                                newNode.HasInHandle = true;
                                currentSubPath.Nodes.Add(newNode);
                            }
                            currentPt = new PointF(x, y);
                            lastCubicControl = new PointF(x2, y2);
                        }
                        break;

                    case 'S':
                        while (HasMoreNumbers())
                        {
                            float x2 = ReadFloat(); float y2 = ReadFloat();
                            float x = ReadFloat(); float y = ReadFloat();
                            if (isRel)
                            {
                                x2 += currentPt.X; y2 += currentPt.Y;
                                x += currentPt.X; y += currentPt.Y;
                            }
                            PointF x1 = new PointF(2 * currentPt.X - lastCubicControl.X, 2 * currentPt.Y - lastCubicControl.Y);
                            if (currentSubPath != null && currentSubPath.Nodes.Count > 0)
                            {
                                var prevNode = currentSubPath.Nodes[currentSubPath.Nodes.Count - 1];
                                prevNode.OutHandle = x1;
                                prevNode.HasOutHandle = true;
                                prevNode.NodeType = VectorNodeType.Smooth;

                                var newNode = new VectorPathNode(new PointF(x, y), VectorNodeType.Smooth);
                                newNode.InHandle = new PointF(x2, y2);
                                newNode.HasInHandle = true;
                                currentSubPath.Nodes.Add(newNode);
                            }
                            currentPt = new PointF(x, y);
                            lastCubicControl = new PointF(x2, y2);
                        }
                        break;

                    case 'Z':
                        if (currentSubPath != null)
                        {
                            currentSubPath.IsClosed = true;
                            currentPt = startPt;
                        }
                        break;

                    default:
                        idx++;
                        break;
                }
            }
            return subPaths;
        }

        private static string GetSvgAttrOrStyle(XElement el, string name)
        {
            var attr = el.Attribute(name)?.Value;
            if (!string.IsNullOrEmpty(attr)) return attr;
            var style = el.Attribute("style")?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                var declarations = style.Split(';');
                foreach (var dec in declarations)
                {
                    var kv = dec.Split(':');
                    if (kv.Length == 2 && kv[0].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return kv[1].Trim();
                    }
                }
            }
            return null;
        }

        private static float ParseSvgFloat(string val, float defVal)
        {
            if (string.IsNullOrWhiteSpace(val)) return defVal;
            val = val.Replace("px", "").Trim();
            if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                return f;
            return defVal;
        }

        private static Color ParseSvgColor(string val, Color defColor)
        {
            if (string.IsNullOrWhiteSpace(val) || val.Equals("none", StringComparison.OrdinalIgnoreCase)) return Color.Transparent;
            try
            {
                if (val.StartsWith("#"))
                {
                    string hex = val.Substring(1);
                    if (hex.Length == 3)
                    {
                        hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                    }
                    if (hex.Length == 6)
                    {
                        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        return Color.FromArgb(255, r, g, b);
                    }
                }
                else if (val.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    int open = val.IndexOf('(');
                    int close = val.IndexOf(')');
                    if (open >= 0 && close > open)
                    {
                        var parts = val.Substring(open + 1, close - open - 1).Split(',');
                        if (parts.Length >= 3)
                        {
                            int r = int.Parse(parts[0].Trim());
                            int g = int.Parse(parts[1].Trim());
                            int b = int.Parse(parts[2].Trim());
                            return Color.FromArgb(255, r, g, b);
                        }
                    }
                }
                var named = Color.FromName(val);
                if (named.IsKnownColor) return named;
            }
            catch { }
            return defColor;
        }
        #endregion

        public void Dispose()
        {
            foreach (var l in Layers)
            {
                l.Dispose();
            }
            Layers.Clear();
            UndoStack.Clear();
            RedoStack.Clear();
        }
    }
}

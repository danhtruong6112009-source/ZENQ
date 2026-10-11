using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

        // Vector Shape Properties
        public VectorShapeType ShapeType { get; set; } = VectorShapeType.Rectangle;
        public VectorFillType FillType { get; set; } = VectorFillType.Solid;
        public Color FillColor { get; set; } = Color.FromArgb(59, 130, 246);
        public Color FillColor2 { get; set; } = Color.FromArgb(147, 197, 253);
        public float FillGradientAngle { get; set; } = 0f;

        public VectorStrokeType StrokeType { get; set; } = VectorStrokeType.Solid;
        public Color StrokeColor { get; set; } = Color.FromArgb(29, 78, 216);
        public float StrokeWidth { get; set; } = 2.0f;
        public DashStyle StrokeDashStyle { get; set; } = DashStyle.Solid;
        public LineJoin StrokeLineJoin { get; set; } = LineJoin.Round;
        public LineCap StrokeLineCap { get; set; } = LineCap.Round;
        public float CornerRadius { get; set; } = 0.0f;
        public int ShapePointCount { get; set; } = 5;
        public bool HasShadow { get; set; } = false;

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
        public string FontName { get; set; } = "Segoe UI";
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
                TextColor = TextColor
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
    }

    public class PhotoDocument : IDisposable
    {
        public string Title { get; set; } = "Thiết kế Mới 1.psd";
        public string FilePath { get; set; }
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public Color BackgroundColor { get; set; } = Color.White;
        public List<PhotoLayer> Layers { get; set; } = new List<PhotoLayer>();
        public int ActiveLayerIndex { get; set; } = 0;

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

        public PhotoDocument()
        {
            InitDefault();
        }

        public PhotoDocument(int width, int height, string title = "Thiết kế Mới 1.psd")
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
                ActiveLayerIndex = ActiveLayerIndex
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
                ActiveLayerIndex = ActiveLayerIndex
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
                ActiveLayerIndex = ActiveLayerIndex
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
            LineCap lineCap = LineCap.Round)
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

                if (fill.A > 0)
                {
                    using (SolidBrush fillB = new SolidBrush(fill))
                    {
                        g.FillPath(fillB, path);
                    }
                }

                if (stroke.A > 0 && l.StrokeWidth > 0)
                {
                    using (Pen strokeP = new Pen(stroke, l.StrokeWidth))
                    {
                        strokeP.LineJoin = LineJoin.Round;
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
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
            {
                g.DrawString(l.TextContent, f, b, l.Bounds, sf);
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

            if (ext == ".psd")
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

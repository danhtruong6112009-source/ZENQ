using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GenOffice.Core
{
    public enum PdfElementType
    {
        Text,
        Image,
        Shape,
        Annotation,
        Signature,
        FormField
    }

    public enum PdfAnnotationType
    {
        Highlight,
        Underline,
        Strikethrough,
        StickyNote,
        InkDrawing,
        Stamp
    }

    public enum PdfShapeType
    {
        Rectangle,
        Ellipse,
        Line,
        Arrow,
        Freehand
    }

    public class PdfTextElement
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public RectangleF Bounds { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderline { get; set; }
        public StringAlignment Alignment { get; set; }
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }
        public float BorderWidth { get; set; }
        public bool IsPlaceholder { get; set; }
        public bool IsDetectedFromPdf { get; set; }
        public string OriginalText { get; set; }
        public RectangleF OriginalBounds { get; set; }
        public Color DetectedBackgroundColor { get; set; }

        public PdfTextElement(string text, RectangleF bounds, string fontName = "Segoe UI", float fontSize = 12f)
        {
            Id = Guid.NewGuid().ToString("N");
            Text = text;
            Bounds = bounds;
            OriginalBounds = bounds;
            FontName = string.IsNullOrEmpty(fontName) ? "Segoe UI" : fontName;
            FontSize = fontSize <= 0 ? 12f : fontSize;
            TextColor = Color.Black;
            DetectedBackgroundColor = Color.Transparent;
            IsBold = false;
            IsItalic = false;
            IsUnderline = false;
            Alignment = StringAlignment.Near;
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderWidth = 0f;
            IsPlaceholder = false;
            IsDetectedFromPdf = false;
            OriginalText = text;
        }

        public PdfTextElement Clone()
        {
            return new PdfTextElement(Text, Bounds, FontName, FontSize)
            {
                TextColor = this.TextColor,
                IsBold = this.IsBold,
                IsItalic = this.IsItalic,
                IsUnderline = this.IsUnderline,
                Alignment = this.Alignment,
                BackgroundColor = this.BackgroundColor,
                BorderColor = this.BorderColor,
                BorderWidth = this.BorderWidth,
                IsPlaceholder = this.IsPlaceholder,
                IsDetectedFromPdf = this.IsDetectedFromPdf,
                OriginalText = this.OriginalText,
                OriginalBounds = this.OriginalBounds,
                DetectedBackgroundColor = this.DetectedBackgroundColor
            };
        }
    }

    public class PdfImageElement
    {
        public string Id { get; set; }
        public RectangleF Bounds { get; set; }
        public Image ImageData { get; set; }
        public float Opacity { get; set; }
        public float Rotation { get; set; }

        public PdfImageElement(Image image, RectangleF bounds)
        {
            Id = Guid.NewGuid().ToString("N");
            ImageData = image;
            Bounds = bounds;
            Opacity = 1.0f;
            Rotation = 0f;
        }

        public PdfImageElement Clone()
        {
            return new PdfImageElement(ImageData != null ? (Image)ImageData.Clone() : null, Bounds)
            {
                Opacity = this.Opacity,
                Rotation = this.Rotation
            };
        }
    }

    public class PdfShapeElement
    {
        public string Id { get; set; }
        public PdfShapeType ShapeType { get; set; }
        public RectangleF Bounds { get; set; }
        public Color FillColor { get; set; }
        public Color StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public List<PointF> Points { get; set; }

        public PdfShapeElement(PdfShapeType type, RectangleF bounds)
        {
            Id = Guid.NewGuid().ToString("N");
            ShapeType = type;
            Bounds = bounds;
            FillColor = Color.Transparent;
            StrokeColor = Color.FromArgb(220, 38, 38);
            StrokeWidth = 2.0f;
            Points = new List<PointF>();
        }

        public PdfShapeElement Clone()
        {
            var shape = new PdfShapeElement(ShapeType, Bounds)
            {
                FillColor = this.FillColor,
                StrokeColor = this.StrokeColor,
                StrokeWidth = this.StrokeWidth
            };
            if (Points != null) shape.Points = new List<PointF>(Points);
            return shape;
        }
    }

    public class PdfAnnotationElement
    {
        public string Id { get; set; }
        public PdfAnnotationType AnnotationType { get; set; }
        public RectangleF Bounds { get; set; }
        public Color Color { get; set; }
        public float Opacity { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PointF> InkPoints { get; set; }
        public float StrokeWidth { get; set; }
        public string StampText { get; set; }

        public PdfAnnotationElement(PdfAnnotationType type, RectangleF bounds, Color color)
        {
            Id = Guid.NewGuid().ToString("N");
            AnnotationType = type;
            Bounds = bounds;
            Color = color;
            Opacity = (type == PdfAnnotationType.Highlight) ? 0.35f : 1.0f;
            Content = "";
            Author = "Người dùng GenOffice";
            CreatedAt = DateTime.Now;
            InkPoints = new List<PointF>();
            StrokeWidth = 2.5f;
            StampText = "";
        }

        public PdfAnnotationElement Clone()
        {
            var ann = new PdfAnnotationElement(AnnotationType, Bounds, Color)
            {
                Opacity = this.Opacity,
                Content = this.Content,
                Author = this.Author,
                CreatedAt = this.CreatedAt,
                StrokeWidth = this.StrokeWidth,
                StampText = this.StampText
            };
            if (InkPoints != null) ann.InkPoints = new List<PointF>(InkPoints);
            return ann;
        }
    }

    public class PdfSignatureElement
    {
        public string Id { get; set; }
        public RectangleF Bounds { get; set; }
        public Image SignatureImage { get; set; }
        public string SignerName { get; set; }
        public string SignDate { get; set; }
        public string Reason { get; set; }
        public Color SignatureColor { get; set; }
        public List<List<PointF>> SignatureStrokes { get; set; }

        public PdfSignatureElement(RectangleF bounds, string signerName = "GenOffice Signer")
        {
            Id = Guid.NewGuid().ToString("N");
            Bounds = bounds;
            SignerName = signerName;
            SignDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            Reason = "Đã phê duyệt tài liệu";
            SignatureColor = Color.FromArgb(30, 58, 138); // Deep Royal Blue
            SignatureStrokes = new List<List<PointF>>();
        }

        public PdfSignatureElement Clone()
        {
            var sig = new PdfSignatureElement(Bounds, SignerName)
            {
                SignatureImage = SignatureImage != null ? (Image)SignatureImage.Clone() : null,
                SignDate = this.SignDate,
                Reason = this.Reason,
                SignatureColor = this.SignatureColor
            };
            if (SignatureStrokes != null)
            {
                sig.SignatureStrokes = new List<List<PointF>>();
                foreach (var stroke in SignatureStrokes)
                {
                    sig.SignatureStrokes.Add(new List<PointF>(stroke));
                }
            }
            return sig;
        }
    }

    public class PdfWatermark
    {
        public string Text { get; set; }
        public Color Color { get; set; }
        public float Opacity { get; set; }
        public float AngleDegrees { get; set; }
        public float FontSize { get; set; }
        public bool IsEnabled { get; set; }

        public PdfWatermark(string text = "BẢO MẬT")
        {
            Text = text;
            Color = Color.FromArgb(220, 38, 38);
            Opacity = 0.18f;
            AngleDegrees = -45f;
            FontSize = 54f;
            IsEnabled = false;
        }

        public PdfWatermark Clone()
        {
            return new PdfWatermark(Text)
            {
                Color = this.Color,
                Opacity = this.Opacity,
                AngleDegrees = this.AngleDegrees,
                FontSize = this.FontSize,
                IsEnabled = this.IsEnabled
            };
        }
    }

    public class PdfPageModel
    {
        public string PageId { get; set; }
        public int PageNumber { get; set; }
        public float Width { get; set; }  // Standard A4 width: 595.28 pt (approx 8.27 in)
        public float Height { get; set; } // Standard A4 height: 841.89 pt (approx 11.69 in)
        public int Rotation { get; set; } // 0, 90, 180, 270 degrees
        public Color BackgroundColor { get; set; }

        public List<PdfTextElement> TextElements { get; set; }
        public List<PdfImageElement> ImageElements { get; set; }
        public List<PdfShapeElement> ShapeElements { get; set; }
        public List<PdfAnnotationElement> Annotations { get; set; }
        public List<PdfSignatureElement> Signatures { get; set; }

        // High-Performance Bitmap Caches for 60+ FPS Smooth Scrolling
        public Bitmap RenderCache { get; set; }
        public float CachedZoomLevel { get; set; }
        public Bitmap ThumbnailCache { get; set; }
        public Image NativePageImage { get; set; }

        public void InvalidateCache()
        {
            if (RenderCache != null)
            {
                try { RenderCache.Dispose(); } catch { }
                RenderCache = null;
            }
            if (ThumbnailCache != null)
            {
                try { ThumbnailCache.Dispose(); } catch { }
                ThumbnailCache = null;
            }
        }

        public PdfPageModel(int pageNumber = 1, float width = 595.28f, float height = 841.89f)
        {
            PageId = Guid.NewGuid().ToString("N");
            PageNumber = pageNumber;
            Width = width;
            Height = height;
            Rotation = 0;
            BackgroundColor = Color.White;

            TextElements = new List<PdfTextElement>();
            ImageElements = new List<PdfImageElement>();
            ShapeElements = new List<PdfShapeElement>();
            Annotations = new List<PdfAnnotationElement>();
            Signatures = new List<PdfSignatureElement>();
        }

        public PdfPageModel Clone()
        {
            var page = new PdfPageModel(PageNumber, Width, Height)
            {
                Rotation = this.Rotation,
                BackgroundColor = this.BackgroundColor
            };

            foreach (var t in TextElements) page.TextElements.Add(t.Clone());
            foreach (var img in ImageElements) page.ImageElements.Add(img.Clone());
            foreach (var shp in ShapeElements) page.ShapeElements.Add(shp.Clone());
            foreach (var ann in Annotations) page.Annotations.Add(ann.Clone());
            foreach (var sig in Signatures) page.Signatures.Add(sig.Clone());

            return page;
        }

        public void RotateClockwise()
        {
            Rotation = (Rotation + 90) % 360;
            InvalidateCache();
        }

        public void RotateCounterClockwise()
        {
            Rotation = (Rotation + 270) % 360;
            InvalidateCache();
        }
    }

    public class PdfDocument : IDisposable
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string Author { get; set; }
        public string Creator { get; set; }
        public string Subject { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public List<PdfPageModel> Pages { get; set; }
        public PdfWatermark GlobalWatermark { get; set; }
        public bool IsModified { get; set; }
        public string PrimaryFontName { get; set; }
        public PdfiumViewer.PdfDocument UnderlyingPdfiumDoc { get; set; }

        public void InvalidateAllCaches()
        {
            if (Pages != null)
            {
                foreach (var p in Pages)
                {
                    p.InvalidateCache();
                }
            }
        }

        public void Dispose()
        {
            if (UnderlyingPdfiumDoc != null)
            {
                try { UnderlyingPdfiumDoc.Dispose(); } catch { }
                UnderlyingPdfiumDoc = null;
            }
            InvalidateAllCaches();
        }

        public PdfDocument Clone()
        {
            var newDoc = new PdfDocument(this.Title)
            {
                FilePath = this.FilePath,
                Author = this.Author,
                Creator = this.Creator,
                Subject = this.Subject,
                CreatedDate = this.CreatedDate,
                ModifiedDate = this.ModifiedDate,
                UnderlyingPdfiumDoc = this.UnderlyingPdfiumDoc,
                GlobalWatermark = this.GlobalWatermark != null ? this.GlobalWatermark.Clone() : new PdfWatermark(),
                IsModified = this.IsModified,
                PrimaryFontName = this.PrimaryFontName
            };
            newDoc.Pages.Clear();
            foreach (var p in this.Pages)
            {
                newDoc.Pages.Add(p.Clone());
            }
            return newDoc;
        }

        public PdfDocument(string title = "Tài liệu PDF Mới.pdf")
        {
            Title = title;
            FilePath = "";
            PrimaryFontName = "Times New Roman";
            Author = "Người dùng GenOffice";
            Creator = "GenOffice Master Suite PDF Engine";
            Subject = "Tài liệu PDF";
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            Pages = new List<PdfPageModel>();
            GlobalWatermark = new PdfWatermark();
            IsModified = false;
            UnderlyingPdfiumDoc = null;

            // Khởi tạo trang đầu tiên chuẩn A4
            Pages.Add(new PdfPageModel(1));
        }

        public void AddBlankPage(int? insertIndex = null, float width = 595.28f, float height = 841.89f)
        {
            int targetIdx = insertIndex.HasValue ? insertIndex.Value : Pages.Count;
            if (targetIdx < 0) targetIdx = 0;
            if (targetIdx > Pages.Count) targetIdx = Pages.Count;

            var newPage = new PdfPageModel(targetIdx + 1, width, height);
            Pages.Insert(targetIdx, newPage);
            RenumberPages();
            IsModified = true;
        }

        public bool DeletePage(int pageIndex)
        {
            if (Pages.Count <= 1 || pageIndex < 0 || pageIndex >= Pages.Count) return false;
            Pages.RemoveAt(pageIndex);
            RenumberPages();
            IsModified = true;
            return true;
        }

        public void MovePage(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Pages.Count || toIndex < 0 || toIndex >= Pages.Count || fromIndex == toIndex) return;
            var page = Pages[fromIndex];
            Pages.RemoveAt(fromIndex);
            Pages.Insert(toIndex, page);
            RenumberPages();
            IsModified = true;
        }

        public void RenumberPages()
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].PageNumber = i + 1;
            }
        }

        public bool RotatePage(int pageIndex, int angleDelta = 90)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count) return false;
            Pages[pageIndex].Rotation = (Pages[pageIndex].Rotation + angleDelta) % 360;
            if (Pages[pageIndex].Rotation < 0) Pages[pageIndex].Rotation += 360;
            Pages[pageIndex].InvalidateCache();
            IsModified = true;
            return true;
        }

        public void RotateAllPages(int angleDelta = 90)
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].Rotation = (Pages[i].Rotation + angleDelta) % 360;
                if (Pages[i].Rotation < 0) Pages[i].Rotation += 360;
                Pages[i].InvalidateCache();
            }
            IsModified = true;
        }

        public bool DuplicatePage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count) return false;
            var clonedPage = Pages[pageIndex].Clone();
            Pages.Insert(pageIndex + 1, clonedPage);
            RenumberPages();
            IsModified = true;
            return true;
        }

        public void AppendDocument(PdfDocument otherDoc)
        {
            if (otherDoc == null || otherDoc.Pages == null || otherDoc.Pages.Count == 0) return;
            foreach (var p in otherDoc.Pages)
            {
                Pages.Add(p.Clone());
            }
            RenumberPages();
            IsModified = true;
        }

        public void InsertDocumentAt(int targetIndex, PdfDocument otherDoc)
        {
            if (otherDoc == null || otherDoc.Pages == null || otherDoc.Pages.Count == 0) return;
            int insertIdx = Math.Max(0, Math.Min(Pages.Count, targetIndex));
            for (int i = 0; i < otherDoc.Pages.Count; i++)
            {
                Pages.Insert(insertIdx + i, otherDoc.Pages[i].Clone());
            }
            RenumberPages();
            IsModified = true;
        }

        public PdfDocument ExtractPages(IEnumerable<int> pageIndices, string newTitle = "")
        {
            string title = string.IsNullOrEmpty(newTitle) ? "Trích xuất - " + Title : newTitle;
            var newDoc = new PdfDocument(title)
            {
                Author = this.Author,
                Creator = this.Creator,
                Subject = this.Subject,
                GlobalWatermark = this.GlobalWatermark.Clone(),
                IsModified = true
            };
            newDoc.Pages.Clear();

            foreach (int idx in pageIndices)
            {
                if (idx >= 0 && idx < Pages.Count)
                {
                    newDoc.Pages.Add(Pages[idx].Clone());
                }
            }

            if (newDoc.Pages.Count == 0 && Pages.Count > 0)
            {
                newDoc.Pages.Add(Pages[0].Clone());
            }

            newDoc.RenumberPages();
            return newDoc;
        }
    }
}

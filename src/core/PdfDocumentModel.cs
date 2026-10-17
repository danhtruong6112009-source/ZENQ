using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZenQOffice.Core
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
        Stamp,
        Redaction
    }

    public enum PdfFormFieldType
    {
        Text,
        CheckBox,
        RadioButton,
        ComboBox,
        Button
    }

    public enum PdfBatesPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
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
            Author = "Người dùng ZENQ OFFICE";
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

        public PdfSignatureElement(RectangleF bounds, string signerName = "ZENQ Signer")
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

    public class PdfSecuritySettings
    {
        public bool IsPasswordProtected { get; set; } = false;
        public string UserPasswordHash { get; set; } = "";
        public string OwnerPasswordHash { get; set; } = "";
        public bool AllowPrinting { get; set; } = true;
        public bool AllowCopying { get; set; } = true;
        public bool AllowModifying { get; set; } = true;
        public int EncryptionKeyLength { get; set; } = 128;

        public static string ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public bool VerifyUserPassword(string password)
        {
            if (!IsPasswordProtected || string.IsNullOrEmpty(UserPasswordHash)) return true;
            return UserPasswordHash.Equals(ComputeHash(password), StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyOwnerPassword(string password)
        {
            if (!IsPasswordProtected || string.IsNullOrEmpty(OwnerPasswordHash)) return true;
            return OwnerPasswordHash.Equals(ComputeHash(password), StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyAnyPassword(string password)
        {
            if (!IsPasswordProtected) return true;
            string h = ComputeHash(password);
            if (!string.IsNullOrEmpty(UserPasswordHash) && UserPasswordHash.Equals(h, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(OwnerPasswordHash) && OwnerPasswordHash.Equals(h, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public PdfSecuritySettings Clone()
        {
            return new PdfSecuritySettings
            {
                IsPasswordProtected = this.IsPasswordProtected,
                UserPasswordHash = this.UserPasswordHash,
                OwnerPasswordHash = this.OwnerPasswordHash,
                AllowPrinting = this.AllowPrinting,
                AllowCopying = this.AllowCopying,
                AllowModifying = this.AllowModifying,
                EncryptionKeyLength = this.EncryptionKeyLength
            };
        }
    }

    public class PdfFormFieldElement
    {
        public string Id { get; set; }
        public PdfFormFieldType FieldType { get; set; }
        public string FieldName { get; set; }
        public string Name { get => FieldName; set => FieldName = value; }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
        public RectangleF Bounds { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsRequired { get; set; }
        public bool IsChecked { get; set; }
        public string GroupName { get; set; }
        public string ExportValue { get; set; } = "";
        public string Tooltip { get; set; } = "";
        public int SelectedOptionIndex { get; set; } = 0;
        public List<string> Options { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }
        public float BorderWidth { get; set; }
        public StringAlignment Alignment { get; set; }
        public bool IsMultiline { get; set; }

        public PdfFormFieldElement(PdfFormFieldType type, RectangleF bounds, string fieldName = "", string value = "")
        {
            Id = Guid.NewGuid().ToString("N");
            FieldType = type;
            Bounds = bounds;
            FieldName = string.IsNullOrEmpty(fieldName) ? $"field_{type}_{Id.Substring(0, 6)}" : fieldName;
            Value = value ?? "";
            DefaultValue = Value;
            IsReadOnly = false;
            IsRequired = false;
            IsChecked = false;
            GroupName = "";
            ExportValue = "";
            Tooltip = "";
            SelectedOptionIndex = 0;
            Options = new List<string>();
            FontName = "Segoe UI";
            FontSize = (type == PdfFormFieldType.CheckBox || type == PdfFormFieldType.RadioButton) ? 14f : 11f;
            TextColor = Color.FromArgb(15, 23, 42);
            BackgroundColor = (type == PdfFormFieldType.CheckBox || type == PdfFormFieldType.RadioButton) ? Color.White : Color.FromArgb(248, 250, 252);
            BorderColor = Color.FromArgb(148, 163, 184);
            BorderWidth = 1.2f;
            Alignment = StringAlignment.Near;
            IsMultiline = false;
        }

        public PdfFormFieldElement Clone()
        {
            var elem = new PdfFormFieldElement(FieldType, Bounds, FieldName, Value)
            {
                Id = this.Id,
                DefaultValue = this.DefaultValue,
                IsReadOnly = this.IsReadOnly,
                IsRequired = this.IsRequired,
                IsChecked = this.IsChecked,
                GroupName = this.GroupName,
                ExportValue = this.ExportValue,
                Tooltip = this.Tooltip,
                SelectedOptionIndex = this.SelectedOptionIndex,
                FontName = this.FontName,
                FontSize = this.FontSize,
                TextColor = this.TextColor,
                BackgroundColor = this.BackgroundColor,
                BorderColor = this.BorderColor,
                BorderWidth = this.BorderWidth,
                Alignment = this.Alignment,
                IsMultiline = this.IsMultiline
            };
            if (Options != null) elem.Options = new List<string>(Options);
            return elem;
        }
    }

    public class PdfRedactionElement
    {
        public string Id { get; set; }
        public RectangleF Bounds { get; set; }
        public string OverlayText { get; set; }
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public Color TextColor { get; set; }
        public bool IsApplied { get; set; }
        public DateTime MarkedAt { get; set; }

        public PdfRedactionElement(RectangleF bounds, string overlayText = "BẢO MẬT")
        {
            Id = Guid.NewGuid().ToString("N");
            Bounds = bounds;
            OverlayText = overlayText;
            FillColor = Color.Black;
            BorderColor = Color.FromArgb(239, 68, 68);
            TextColor = Color.White;
            IsApplied = false;
            MarkedAt = DateTime.Now;
        }

        public PdfRedactionElement Clone()
        {
            return new PdfRedactionElement(Bounds, OverlayText)
            {
                Id = this.Id,
                FillColor = this.FillColor,
                BorderColor = this.BorderColor,
                TextColor = this.TextColor,
                IsApplied = this.IsApplied,
                MarkedAt = this.MarkedAt
            };
        }
    }

    public class PdfBatesConfig
    {
        public string Prefix { get; set; } = "ZENQ-";
        public string Suffix { get; set; } = "";
        public int StartNumber { get; set; } = 1;
        public int NumberOfDigits { get; set; } = 6;
        public PdfBatesPosition Position { get; set; } = PdfBatesPosition.BottomRight;
        public string FontName { get; set; } = "Segoe UI";
        public float FontSize { get; set; } = 9f;
        public Color TextColor { get; set; } = Color.FromArgb(71, 85, 105);
        public float MarginX { get; set; } = 36f;
        public float MarginY { get; set; } = 28f;
        public int StartPageIndex { get; set; } = 0;
        public int EndPageIndex { get; set; } = -1;

        public string FormatNumber(int pageOffset)
        {
            int currentNum = StartNumber + pageOffset;
            string numStr = currentNum.ToString().PadLeft(NumberOfDigits, '0');
            return $"{Prefix}{numStr}{Suffix}";
        }

        public PdfBatesConfig Clone()
        {
            return new PdfBatesConfig
            {
                Prefix = this.Prefix,
                Suffix = this.Suffix,
                StartNumber = this.StartNumber,
                NumberOfDigits = this.NumberOfDigits,
                Position = this.Position,
                FontName = this.FontName,
                FontSize = this.FontSize,
                TextColor = this.TextColor,
                MarginX = this.MarginX,
                MarginY = this.MarginY,
                StartPageIndex = this.StartPageIndex,
                EndPageIndex = this.EndPageIndex
            };
        }
    }

    public class PdfBookmarkNode
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int TargetPageIndex { get; set; }
        public float TargetScrollY { get; set; }
        public int Level { get; set; }
        public bool IsExpanded { get; set; }
        public List<PdfBookmarkNode> Children { get; set; }

        public PdfBookmarkNode(string title, int pageIndex, float scrollY = 0f, int level = 0)
        {
            Id = Guid.NewGuid().ToString("N");
            Title = title;
            TargetPageIndex = pageIndex;
            TargetScrollY = scrollY;
            Level = level;
            IsExpanded = true;
            Children = new List<PdfBookmarkNode>();
        }

        public PdfBookmarkNode Clone()
        {
            var node = new PdfBookmarkNode(Title, TargetPageIndex, TargetScrollY, Level)
            {
                Id = this.Id,
                IsExpanded = this.IsExpanded
            };
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    node.Children.Add(child.Clone());
                }
            }
            return node;
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
        public List<PdfImageElement> Images { get => ImageElements; set => ImageElements = value; }
        public List<PdfShapeElement> ShapeElements { get; set; }
        public List<PdfAnnotationElement> Annotations { get; set; }
        public List<PdfSignatureElement> Signatures { get; set; }
        public List<PdfFormFieldElement> FormFields { get; set; }
        public List<PdfRedactionElement> Redactions { get; set; }
        public string BatesNumber { get; set; }

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
            FormFields = new List<PdfFormFieldElement>();
            Redactions = new List<PdfRedactionElement>();
            BatesNumber = null;
        }

        public PdfPageModel Clone()
        {
            var page = new PdfPageModel(PageNumber, Width, Height)
            {
                Rotation = this.Rotation,
                BackgroundColor = this.BackgroundColor,
                BatesNumber = this.BatesNumber
            };

            foreach (var t in TextElements) page.TextElements.Add(t.Clone());
            foreach (var img in ImageElements) page.ImageElements.Add(img.Clone());
            foreach (var shp in ShapeElements) page.ShapeElements.Add(shp.Clone());
            foreach (var ann in Annotations) page.Annotations.Add(ann.Clone());
            foreach (var sig in Signatures) page.Signatures.Add(sig.Clone());
            foreach (var ff in FormFields) page.FormFields.Add(ff.Clone());
            foreach (var red in Redactions) page.Redactions.Add(red.Clone());

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
        public string Keywords { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public List<PdfPageModel> Pages { get; set; }
        public PdfWatermark GlobalWatermark { get; set; }
        public List<PdfBookmarkNode> Bookmarks { get; set; }
        public PdfBatesConfig ActiveBatesConfig { get; set; }
        public PdfSecuritySettings SecuritySettings { get; set; } = new PdfSecuritySettings();
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
                Keywords = this.Keywords,
                CreatedDate = this.CreatedDate,
                ModifiedDate = this.ModifiedDate,
                UnderlyingPdfiumDoc = this.UnderlyingPdfiumDoc,
                GlobalWatermark = this.GlobalWatermark != null ? this.GlobalWatermark.Clone() : new PdfWatermark(),
                SecuritySettings = this.SecuritySettings != null ? this.SecuritySettings.Clone() : new PdfSecuritySettings(),
                IsModified = this.IsModified,
                PrimaryFontName = this.PrimaryFontName,
                ActiveBatesConfig = this.ActiveBatesConfig != null ? this.ActiveBatesConfig.Clone() : null
            };
            newDoc.Pages.Clear();
            foreach (var p in this.Pages)
            {
                newDoc.Pages.Add(p.Clone());
            }
            if (this.Bookmarks != null)
            {
                newDoc.Bookmarks = new List<PdfBookmarkNode>();
                foreach (var bm in this.Bookmarks) newDoc.Bookmarks.Add(bm.Clone());
            }
            return newDoc;
        }

        public void SetPasswordProtection(string userPassword, string ownerPassword = null, bool allowPrinting = true, bool allowCopying = true, bool allowModifying = true, int keyLength = 128)
        {
            if (SecuritySettings == null) SecuritySettings = new PdfSecuritySettings();
            SecuritySettings.IsPasswordProtected = true;
            SecuritySettings.UserPasswordHash = !string.IsNullOrEmpty(userPassword) ? PdfSecuritySettings.ComputeHash(userPassword) : "";
            SecuritySettings.OwnerPasswordHash = !string.IsNullOrEmpty(ownerPassword) ? PdfSecuritySettings.ComputeHash(ownerPassword) : SecuritySettings.UserPasswordHash;
            SecuritySettings.AllowPrinting = allowPrinting;
            SecuritySettings.AllowCopying = allowCopying;
            SecuritySettings.AllowModifying = allowModifying;
            SecuritySettings.EncryptionKeyLength = keyLength;
            IsModified = true;
        }

        public bool RemovePasswordProtection(string password)
        {
            if (SecuritySettings == null || !SecuritySettings.IsPasswordProtected) return true;
            if (SecuritySettings.VerifyAnyPassword(password))
            {
                SecuritySettings.IsPasswordProtected = false;
                SecuritySettings.UserPasswordHash = "";
                SecuritySettings.OwnerPasswordHash = "";
                SecuritySettings.AllowPrinting = true;
                SecuritySettings.AllowCopying = true;
                SecuritySettings.AllowModifying = true;
                IsModified = true;
                return true;
            }
            return false;
        }

        public bool VerifyPassword(string password)
        {
            if (SecuritySettings == null || !SecuritySettings.IsPasswordProtected) return true;
            return SecuritySettings.VerifyAnyPassword(password);
        }

        public PdfDocument(string title = "Tài liệu PDF Mới.pdf")
        {
            Title = title;
            FilePath = "";
            PrimaryFontName = "Times New Roman";
            Author = "Người dùng ZENQ OFFICE";
            Creator = "ZENQ OFFICE Suite PDF Engine";
            Subject = "Tài liệu PDF";
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            Pages = new List<PdfPageModel>();
            GlobalWatermark = new PdfWatermark();
            SecuritySettings = new PdfSecuritySettings();
            Bookmarks = new List<PdfBookmarkNode>();
            ActiveBatesConfig = null;
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

        public int FlattenFormFields()
        {
            if (Pages == null) return 0;
            int totalFlattened = 0;
            foreach (var page in Pages)
            {
                if (page.FormFields == null || page.FormFields.Count == 0) continue;
                totalFlattened += page.FormFields.Count;
                foreach (var ff in page.FormFields)
                {
                    if (ff.FieldType == PdfFormFieldType.Text || ff.FieldType == PdfFormFieldType.ComboBox)
                    {
                        string displayText = !string.IsNullOrEmpty(ff.Value) ? ff.Value : ff.DefaultValue;
                        if (!string.IsNullOrEmpty(displayText))
                        {
                            var textElem = new PdfTextElement(displayText, ff.Bounds, ff.FontName, ff.FontSize)
                            {
                                TextColor = ff.TextColor,
                                Alignment = ff.Alignment,
                                BackgroundColor = ff.BackgroundColor,
                                BorderColor = ff.BorderColor,
                                BorderWidth = ff.BorderWidth
                            };
                            page.TextElements.Add(textElem);
                        }
                    }
                    else if (ff.FieldType == PdfFormFieldType.CheckBox)
                    {
                        var shp = new PdfShapeElement(PdfShapeType.Rectangle, ff.Bounds)
                        {
                            StrokeColor = ff.BorderColor,
                            StrokeWidth = ff.BorderWidth > 0 ? ff.BorderWidth : 1.5f,
                            FillColor = ff.BackgroundColor
                        };
                        page.ShapeElements.Add(shp);
                        if (ff.IsChecked)
                        {
                            var checkText = new PdfTextElement("✓", ff.Bounds, "Segoe UI", ff.FontSize > 0 ? ff.FontSize : 14f)
                            {
                                TextColor = ff.TextColor,
                                Alignment = StringAlignment.Center
                            };
                            page.TextElements.Add(checkText);
                        }
                    }
                    else if (ff.FieldType == PdfFormFieldType.RadioButton)
                    {
                        var shp = new PdfShapeElement(PdfShapeType.Ellipse, ff.Bounds)
                        {
                            StrokeColor = ff.BorderColor,
                            StrokeWidth = ff.BorderWidth > 0 ? ff.BorderWidth : 1.5f,
                            FillColor = ff.BackgroundColor
                        };
                        page.ShapeElements.Add(shp);
                        if (ff.IsChecked)
                        {
                            float dotInset = ff.Bounds.Width * 0.25f;
                            var dotRect = new RectangleF(ff.Bounds.X + dotInset, ff.Bounds.Y + dotInset, ff.Bounds.Width - 2 * dotInset, ff.Bounds.Height - 2 * dotInset);
                            var dotShp = new PdfShapeElement(PdfShapeType.Ellipse, dotRect)
                            {
                                StrokeColor = Color.Transparent,
                                FillColor = ff.TextColor
                            };
                            page.ShapeElements.Add(dotShp);
                        }
                    }
                }
                page.FormFields.Clear();
                page.InvalidateCache();
            }
            IsModified = true;
            return totalFlattened;
        }

        public int ApplyRedactions()
        {
            if (Pages == null) return 0;
            int totalApplied = 0;
            foreach (var page in Pages)
            {
                if (page.Redactions == null || page.Redactions.Count == 0) continue;
                foreach (var red in page.Redactions)
                {
                    if (red.IsApplied) continue;

                    // 1. Destructive permanent removal of text elements intersecting bounds
                    page.TextElements.RemoveAll(t => red.Bounds.IntersectsWith(t.Bounds));

                    // 2. Destructive removal of images completely contained inside bounds
                    page.ImageElements.RemoveAll(img => red.Bounds.Contains(img.Bounds));

                    // 3. Destructive removal of annotations intersecting bounds
                    page.Annotations.RemoveAll(ann => red.Bounds.IntersectsWith(ann.Bounds));

                    // 4. Destructive removal of form fields intersecting bounds
                    page.FormFields.RemoveAll(ff => red.Bounds.IntersectsWith(ff.Bounds));

                    // 5. Destructive removal of signatures intersecting bounds
                    page.Signatures.RemoveAll(sig => red.Bounds.IntersectsWith(sig.Bounds));

                    // 6. Burn permanent opaque blackout rectangle into ShapeElements
                    var blackoutShape = new PdfShapeElement(PdfShapeType.Rectangle, red.Bounds)
                    {
                        FillColor = red.FillColor,
                        StrokeColor = red.FillColor,
                        StrokeWidth = 0f
                    };
                    page.ShapeElements.Add(blackoutShape);

                    if (!string.IsNullOrEmpty(red.OverlayText))
                    {
                        var overlayTxt = new PdfTextElement(red.OverlayText, red.Bounds, "Segoe UI", 10f)
                        {
                            TextColor = red.TextColor,
                            Alignment = StringAlignment.Center,
                            IsBold = true
                        };
                        page.TextElements.Add(overlayTxt);
                    }

                    red.IsApplied = true;
                    totalApplied++;
                }
                page.Redactions.Clear();
                page.InvalidateCache();
            }
            IsModified = true;
            return totalApplied;
        }

        public void SanitizeDocument()
        {
            Author = "";
            Creator = "";
            Subject = "";
            Keywords = "";
            Title = "";
            ModifiedDate = DateTime.Now;
            if (Pages != null)
            {
                foreach (var page in Pages)
                {
                    if (page.Annotations != null)
                    {
                        page.Annotations.RemoveAll(a => a.AnnotationType == PdfAnnotationType.StickyNote);
                    }
                    page.InvalidateCache();
                }
            }
            IsModified = true;
        }

        public void ApplyBatesNumbering(PdfBatesConfig config)
        {
            if (Pages == null || config == null) return;
            ActiveBatesConfig = config;
            int startIdx = Math.Max(0, config.StartPageIndex);
            int endIdx = (config.EndPageIndex >= 0 && config.EndPageIndex < Pages.Count) ? config.EndPageIndex : Pages.Count - 1;

            int pageOffset = 0;
            for (int i = 0; i < Pages.Count; i++)
            {
                if (i >= startIdx && i <= endIdx)
                {
                    Pages[i].BatesNumber = config.FormatNumber(pageOffset);
                    pageOffset++;
                }
                else
                {
                    Pages[i].BatesNumber = null;
                }
                Pages[i].InvalidateCache();
            }
            IsModified = true;
        }

        public void RemoveBatesNumbering()
        {
            if (Pages == null) return;
            ActiveBatesConfig = null;
            foreach (var page in Pages)
            {
                page.BatesNumber = null;
                page.InvalidateCache();
            }
            IsModified = true;
        }

        public PdfBookmarkNode AddBookmark(string title, int pageIndex, float scrollY = 0f, string parentId = null)
        {
            if (Bookmarks == null) Bookmarks = new List<PdfBookmarkNode>();
            if (string.IsNullOrEmpty(parentId))
            {
                var bm = new PdfBookmarkNode(title, pageIndex, scrollY, 0);
                Bookmarks.Add(bm);
                IsModified = true;
                return bm;
            }
            else
            {
                var parent = FindBookmarkById(Bookmarks, parentId);
                if (parent != null)
                {
                    var bm = new PdfBookmarkNode(title, pageIndex, scrollY, parent.Level + 1);
                    parent.Children.Add(bm);
                    IsModified = true;
                    return bm;
                }
                else
                {
                    var bm = new PdfBookmarkNode(title, pageIndex, scrollY, 0);
                    Bookmarks.Add(bm);
                    IsModified = true;
                    return bm;
                }
            }
        }

        public PdfBookmarkNode FindBookmarkById(string id)
        {
            return FindBookmarkById(Bookmarks, id);
        }

        public static PdfBookmarkNode FindBookmarkById(List<PdfBookmarkNode> list, string id)
        {
            if (list == null) return null;
            foreach (var item in list)
            {
                if (item.Id == id) return item;
                var found = FindBookmarkById(item.Children, id);
                if (found != null) return found;
            }
            return null;
        }

        public bool RemoveBookmark(string bookmarkId)
        {
            if (Bookmarks == null) return false;
            bool removed = RemoveBookmarkFromList(Bookmarks, bookmarkId);
            if (removed) IsModified = true;
            return removed;
        }

        private static bool RemoveBookmarkFromList(List<PdfBookmarkNode> list, string id)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id == id)
                {
                    list.RemoveAt(i);
                    return true;
                }
                if (RemoveBookmarkFromList(list[i].Children, id)) return true;
            }
            return false;
        }
    }
}

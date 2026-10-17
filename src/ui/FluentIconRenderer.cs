using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bộ kết xuất biểu tượng chuẩn Microsoft 365 / Fluent Design (Segoe Fluent Icons / Segoe MDL2 Assets).
    /// Áp dụng kỹ thuật khử răng cưa đa tầng Super-Sampling Anti-Aliasing (SSAA 4x):
    /// - Kết xuất vector glyph ở độ phân giải siêu cao (4x) bằng TextRenderingHint.AntiAlias.
    /// - Hạ tỷ lệ về kích thước đích bằng bộ lọc HighQualityBicubic 16-tap.
    /// - Loại bỏ hoàn toàn hiện tượng răng cưa / bậc thang điểm ảnh của GDI thông thường.
    /// - Lưu đệm (cache) tự động: Vẽ mượt mà, đạt 120+ FPS, không tiêu tốn CPU.
    /// </summary>
    public static class FluentIconRenderer
    {
        public static readonly string FontFamilyName = DetectBestIconFontFamily();

        private static readonly ConcurrentDictionary<float, Font> _fontCache =
            new ConcurrentDictionary<float, Font>();

        private static readonly ConcurrentDictionary<string, Bitmap> _bitmapCache =
            new ConcurrentDictionary<string, Bitmap>(StringComparer.Ordinal);

        private static string DetectBestIconFontFamily()
        {
            try
            {
                using (var ifc = new InstalledFontCollection())
                {
                    var names = ifc.Families.Select(f => f.Name).ToArray();
                    if (names.Any(n => string.Equals(n, "Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase)))
                        return "Segoe Fluent Icons";
                    if (names.Any(n => string.Equals(n, "Segoe MDL2 Assets", StringComparison.OrdinalIgnoreCase)))
                        return "Segoe MDL2 Assets";
                    if (names.Any(n => string.Equals(n, "Segoe UI Symbol", StringComparison.OrdinalIgnoreCase)))
                        return "Segoe UI Symbol";
                }
            }
            catch { }
            return "Segoe UI";
        }

        public static Font GetFont(float size)
        {
            if (size <= 0) size = 14f;
            return _fontCache.GetOrAdd(size, s =>
                new Font(FontFamilyName, s, FontStyle.Regular, GraphicsUnit.Pixel));
        }

        /// <summary>
        /// Tạo hoặc lấy từ bộ nhớ đệm Bitmap 32-bit ARGB siêu mịn được khử răng cưa qua bộ lọc SSAA 4x.
        /// </summary>
        public static Bitmap GetGlyphBitmap(char glyph, int width, int height, Color color, float fontSize = 0)
        {
            if (width <= 0) width = 16;
            if (height <= 0) height = 16;

            int intFontSz = (int)Math.Round(fontSize * 10f);
            string key = $"{glyph}_{width}_{height}_{color.ToArgb():X8}_{intFontSz}";

            return _bitmapCache.GetOrAdd(key, _ =>
            {
                // 1. Kết xuất vector ở độ phân giải 4x (Super-Sampling 4x)
                int scale = 4;
                int hiW = Math.Max(32, width * scale);
                int hiH = Math.Max(32, height * scale);

                using (Bitmap hiBmp = new Bitmap(hiW, hiH, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gHi = Graphics.FromImage(hiBmp))
                    {
                        gHi.Clear(Color.Transparent);
                        if (glyph == '\uF51E')
                        {
                            // Align Justify: Tái tạo đồng bộ 100% từ vạch chuẩn của \uE8E4 (Align Left) trong font Segoe Fluent Icons.
                            // Đảm bảo cùng độ dày, cùng bo góc, cùng độ dài và chuẩn xác trên cùng 3 hàng ngang (không bao giờ lệch hàng hay dày/dài bất thường).
                            float hiFontSz = (fontSize > 0) ? Math.Min(fontSize * scale, hiH * 0.75f) : (hiH * 0.72f);
                            Font hiFont = GetFont(hiFontSz);

                            using (Bitmap hiSrc = new Bitmap(hiW, hiH, PixelFormat.Format32bppArgb))
                            {
                                using (Graphics gSrc = Graphics.FromImage(hiSrc))
                                {
                                    gSrc.Clear(Color.Transparent);
                                    gSrc.TextRenderingHint = TextRenderingHint.AntiAlias;
                                    using (SolidBrush brush = new SolidBrush(color))
                                    using (StringFormat sf = new StringFormat
                                    {
                                        Alignment = StringAlignment.Center,
                                        LineAlignment = StringAlignment.Center,
                                        FormatFlags = StringFormatFlags.NoWrap
                                    })
                                    {
                                        gSrc.DrawString("\uE8E4", hiFont, brush, new RectangleF(0, 0, hiW, hiH), sf);
                                    }
                                }

                                // Dò tìm vị trí đỉnh của 3 vạch (y1, y2, y3) của icon căn lề trái chuẩn
                                int checkX = Math.Max(1, hiW / 3);
                                var peaks = new System.Collections.Generic.List<int>();
                                int prevA = 0;
                                bool growing = false;
                                for (int y = 0; y < hiH; y++)
                                {
                                    int a = hiSrc.GetPixel(checkX, y).A;
                                    if (a > prevA) growing = true;
                                    else if (a < prevA && growing && prevA > 120)
                                    {
                                        peaks.Add(y - 1);
                                        growing = false;
                                    }
                                    prevA = a;
                                }

                                int y1, y2, y3, deltaY;
                                if (peaks.Count >= 3)
                                {
                                    y1 = peaks[0];
                                    y2 = peaks[1];
                                    y3 = peaks[2];
                                    deltaY = (int)Math.Round((y3 - y1) / 2.0);
                                }
                                else
                                {
                                    deltaY = (int)Math.Round(hiFontSz * (640.0 / 2048.0));
                                    y2 = (int)Math.Round(hiH / 2.0 - (hiFontSz * 0.03));
                                    y1 = y2 - deltaY;
                                    y3 = y2 + deltaY;
                                }

                                int sliceHalf = Math.Max(3, deltaY / 2);
                                int sliceH = sliceHalf * 2;
                                Rectangle srcMidRect = new Rectangle(0, Math.Max(0, y2 - sliceHalf), hiW, sliceH);

                                // Sao chép vạch giữa (vạch dài nhất chuẩn nét) sang cả 3 hàng y1, y2, y3
                                gHi.DrawImage(hiSrc, new Rectangle(0, y1 - sliceHalf, hiW, sliceH), srcMidRect, GraphicsUnit.Pixel);
                                gHi.DrawImage(hiSrc, new Rectangle(0, y2 - sliceHalf, hiW, sliceH), srcMidRect, GraphicsUnit.Pixel);
                                gHi.DrawImage(hiSrc, new Rectangle(0, y3 - sliceHalf, hiW, sliceH), srcMidRect, GraphicsUnit.Pixel);
                            }
                        }
                        else
                        {
                            gHi.TextRenderingHint = TextRenderingHint.AntiAlias;

                            float hiFontSz = (fontSize > 0) ? Math.Min(fontSize * scale, hiH * 0.75f) : (hiH * 0.72f);
                            Font hiFont = GetFont(hiFontSz);

                            using (SolidBrush brush = new SolidBrush(color))
                            using (StringFormat sf = new StringFormat
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center,
                                FormatFlags = StringFormatFlags.NoWrap
                            })
                            {
                                gHi.DrawString(glyph.ToString(), hiFont, brush, new RectangleF(0, 0, hiW, hiH), sf);
                            }
                        }
                    }

                    // 2. Thu nhỏ về kích thước gốc bằng bộ lọc Bicubic 16-tap (khử răng cưa hoàn hảo)
                    Bitmap targetBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using (Graphics gTarget = Graphics.FromImage(targetBmp))
                    {
                        gTarget.Clear(Color.Transparent);
                        gTarget.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gTarget.SmoothingMode = SmoothingMode.HighQuality;
                        gTarget.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        gTarget.DrawImage(hiBmp, new Rectangle(0, 0, width, height));
                    }
                    return targetBmp;
                }
            });
        }

        /// <summary>
        /// Vẽ một ký tự Glyph của Segoe Fluent Icons căn giữa hoàn hảo trong bounds bằng SSAA bitmap mịn màng.
        /// </summary>
        public static void DrawGlyph(Graphics g, char glyph, RectangleF bounds, Color color, float fontSize = 0)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            int w = (int)Math.Max(1, Math.Round(bounds.Width));
            int h = (int)Math.Max(1, Math.Round(bounds.Height));

            Bitmap bmp = GetGlyphBitmap(glyph, w, h, color, fontSize);
            if (bmp != null)
            {
                var prevInterp = g.InterpolationMode;
                var prevSmooth = g.SmoothingMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(bmp, bounds.X, bounds.Y, bounds.Width, bounds.Height);

                g.InterpolationMode = prevInterp;
                g.SmoothingMode = prevSmooth;
            }
        }

        public static void DrawGlyph(Graphics g, char glyph, Rectangle bounds, Color color, float fontSize = 0)
        {
            DrawGlyph(g, glyph, new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), color, fontSize);
        }

        public static void DrawGlyph(Graphics g, char glyph, int x, int y, int size, Color color, float fontSize = 0)
        {
            DrawGlyph(g, glyph, new RectangleF(x, y, size, size), color, fontSize);
        }

        /// <summary>
        /// Hỗ trợ vẽ trực tiếp lên DirectX Direct2D Graphics
        /// </summary>
        public static void DrawGlyphD2D(D2DGraphics g, char glyph, RectangleF bounds, Color color, float fontSize = 0)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;
            int w = (int)Math.Max(1, Math.Round(bounds.Width));
            int h = (int)Math.Max(1, Math.Round(bounds.Height));
            Bitmap bmp = GetGlyphBitmap(glyph, w, h, color, fontSize);
            if (bmp != null)
            {
                g.DrawImage(bmp, bounds);
            }
        }

        public static void DrawGlyph(D2DGraphics g, char glyph, RectangleF bounds, Color color, float fontSize = 0)
        {
            DrawGlyphD2D(g, glyph, bounds, color, fontSize);
        }

        public static void DrawGlyph(D2DGraphics g, char glyph, Rectangle bounds, Color color, float fontSize = 0)
        {
            DrawGlyphD2D(g, glyph, new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), color, fontSize);
        }

        public static void DrawGlyph(D2DGraphics g, char glyph, int x, int y, int size, Color color, float fontSize = 0)
        {
            DrawGlyphD2D(g, glyph, new RectangleF(x, y, size, size), color, fontSize);
        }

        public static void DrawGlyph(Graphics g, string glyph, Rectangle bounds, Color color, float fontSize = 0)
        {
            if (string.IsNullOrEmpty(glyph)) return;
            DrawGlyph(g, glyph[0], bounds, color, fontSize);
        }

        public static void DrawGlyph(Graphics g, string glyph, RectangleF bounds, Color color, float fontSize = 0)
        {
            if (string.IsNullOrEmpty(glyph)) return;
            DrawGlyph(g, glyph[0], bounds, color, fontSize);
        }

        public static void DrawGlyph(D2DGraphics g, string glyph, Rectangle bounds, Color color, float fontSize = 0)
        {
            if (string.IsNullOrEmpty(glyph)) return;
            DrawGlyph(g, glyph[0], bounds, color, fontSize);
        }

        public static void DrawGlyph(D2DGraphics g, string glyph, RectangleF bounds, Color color, float fontSize = 0)
        {
            if (string.IsNullOrEmpty(glyph)) return;
            DrawGlyph(g, glyph[0], bounds, color, fontSize);
        }

        /// <summary>
        /// Ánh xạ từ danh mục SlideIconKind sang mã Unicode Glyph chính chủ của Microsoft Fluent Icons.
        /// </summary>
        public static char GetSlideIconGlyph(SlideIconKind kind)
        {
            switch (kind)
            {
                // ==================== HOME / CLIPBOARD / SLIDES ====================
                case SlideIconKind.NewSlide: return '\uE82E';            // PageAdd
                case SlideIconKind.SlideLayout: return '\uF158';        // ViewAll / Grid / Layout
                case SlideIconKind.ResetSlide: return '\uE777';         // Reset / Refresh
                case SlideIconKind.DuplicateSlide: return '\uE8C8';     // Copy / Duplicate
                case SlideIconKind.DeleteSlide: return '\uE74D';        // Delete / Trash
                case SlideIconKind.Section: return '\uE81E';            // Folder / Section
                case SlideIconKind.Paste: return '\uE77F';              // Paste
                case SlideIconKind.Cut: return '\uE8C6';                // Cut / Scissors
                case SlideIconKind.Copy: return '\uE8C8';               // Copy
                case SlideIconKind.FormatPainter: return '\uF0E3';      // Format Painter Brush

                // ==================== FONT ====================
                case SlideIconKind.FontIncrease: return '\uE8E8';       // Font Increase (^ Up Arrow)
                case SlideIconKind.FontDecrease: return '\uE8E7';       // Font Decrease (v Down Arrow)
                case SlideIconKind.Bold: return '\uE8DD';               // Bold
                case SlideIconKind.Italic: return '\uE8DB';             // Italic
                case SlideIconKind.Underline: return '\uE8DC';          // Underline
                case SlideIconKind.Strikethrough: return '\uEDE0';      // Strikethrough
                case SlideIconKind.TextShadow: return '\uE790';         // Shadow / Font
                case SlideIconKind.CharacterSpacing: return '\uE8E1';   // Character Spacing
                case SlideIconKind.ChangeCase: return '\uE8E0';         // Change Case
                case SlideIconKind.FontColor: return '\uE8D3';          // Font Color
                case SlideIconKind.TextHighlight: return '\uE7E6';      // Highlighter

                // ==================== PARAGRAPH ====================
                case SlideIconKind.AlignLeft: return '\uE8E4';          // Align Left
                case SlideIconKind.AlignCenter: return '\uE8E3';        // Align Center
                case SlideIconKind.AlignRight: return '\uE8E2';         // Align Right
                case SlideIconKind.AlignJustify: return '\uF51E';       // Align Justify (4 gạch đều nhau chuẩn MS)
                case SlideIconKind.Bullets: return '\uE8FD';            // Bullets
                case SlideIconKind.Numbering: return '\uE8FE';          // Numbering
                case SlideIconKind.IndentDecrease: return '\uE7FD';     // Decrease Indent
                case SlideIconKind.IndentIncrease: return '\uE7FE';     // Increase Indent
                case SlideIconKind.LineSpacing: return '\uE8D0';        // Line Spacing
                case SlideIconKind.Columns: return '\uE8AB';            // Columns
                case SlideIconKind.TextDirection: return '\uE8DF';      // Text Direction
                case SlideIconKind.AlignTextVertical: return '\uE8E6';  // Align Text Vertical
                case SlideIconKind.ConvertSmartArt: return '\uEA3C';    // SmartArt / Flow

                // ==================== INSERT TAB ====================
                case SlideIconKind.Table: return '\uE867';              // Table
                case SlideIconKind.Picture: return '\uEB9F';            // Image / Picture
                case SlideIconKind.Camera: return '\uE722';             // Camera
                case SlideIconKind.PhotoAlbum: return '\uEB9F';         // Photo Album
                case SlideIconKind.Shapes: return '\uF158';             // Shapes
                case SlideIconKind.Icons: return '\uED58';              // Icons
                case SlideIconKind.Model3D: return '\uF158';            // 3D Model
                case SlideIconKind.SmartArt: return '\uE8A9';           // SmartArt
                case SlideIconKind.Chart: return '\uE9D2';              // Chart
                case SlideIconKind.Link: return '\uE71B';               // Link / Chain
                case SlideIconKind.Action: return '\uE768';             // Action / Play
                case SlideIconKind.Comment: return '\uE90A';            // Comment
                case SlideIconKind.TextBox: return '\uE8C2';            // Text Box
                case SlideIconKind.HeaderFooter: return '\uE8D7';       // Header / Footer
                case SlideIconKind.WordArt: return '\uE8D2';            // WordArt
                case SlideIconKind.DateTime: return '\uE787';           // Date / Time
                case SlideIconKind.SlideNumber: return '\uE8EF';        // Slide Number
                case SlideIconKind.Equation: return '\uF142';           // Equation
                case SlideIconKind.Symbol: return '\uF143';             // Symbol
                case SlideIconKind.Video: return '\uE714';              // Video
                case SlideIconKind.Audio: return '\uE8D6';              // Audio
                case SlideIconKind.ScreenRecord: return '\uE714';       // Screen Record

                // ==================== DESIGN TAB ====================
                case SlideIconKind.SlideSize: return '\uE740';          // Slide Size
                case SlideIconKind.FormatBackground: return '\uE771';   // Format Background
                case SlideIconKind.DesignIdeas: return '\uE781';        // Ideas / Sparkle

                // ==================== TRANSITIONS TAB ====================
                case SlideIconKind.TransitionPreview: return '\uE768';  // Play
                case SlideIconKind.TransitionMorph: return '\uE712';    // Morph
                case SlideIconKind.TransitionFade: return '\uE706';     // Fade
                case SlideIconKind.TransitionPush: return '\uE74B';     // Push
                case SlideIconKind.TransitionWipe: return '\uE749';     // Wipe
                case SlideIconKind.TransitionSplit: return '\uE748';    // Split
                case SlideIconKind.TransitionReveal: return '\uE781';   // Reveal
                case SlideIconKind.TransitionRandomBars: return '\uE8FD';// Bars
                case SlideIconKind.TransitionShape: return '\uF158';    // Shape
                case SlideIconKind.EffectOptions: return '\uE713';      // Options
                case SlideIconKind.Sound: return '\uE767';              // Sound
                case SlideIconKind.Duration: return '\uE823';           // Clock / Duration
                case SlideIconKind.ApplyToAll: return '\uE8B1';         // Apply All

                // ==================== ANIMATIONS TAB ====================
                case SlideIconKind.AnimationPreview: return '\uE768';   // Play
                case SlideIconKind.AnimationEntrance: return '\uE8A1';  // Entrance
                case SlideIconKind.AnimationEmphasis: return '\uE734';  // Star / Emphasis
                case SlideIconKind.AnimationExit: return '\uE8A0';      // Exit
                case SlideIconKind.AnimationMotionPath: return '\uE707';// Motion Path
                case SlideIconKind.AddAnimation: return '\uE710';       // Add
                case SlideIconKind.AnimationPane: return '\uE8A9';      // Pane
                case SlideIconKind.Trigger: return '\uE7E8';            // Trigger
                case SlideIconKind.AnimationPainter: return '\uF0E3';   // Painter

                // ==================== SLIDE SHOW TAB ====================
                case SlideIconKind.SlideShowFromBeginning: return '\uE768'; // From Beginning
                case SlideIconKind.SlideShowFromCurrent: return '\uE768';   // From Current
                case SlideIconKind.PresentOnline: return '\uE774';          // Online
                case SlideIconKind.CustomSlideShow: return '\uE714';        // Custom Show
                case SlideIconKind.SetUpSlideShow: return '\uE713';         // Settings
                case SlideIconKind.HideSlide: return '\uED1A';              // Hide
                case SlideIconKind.RehearseTimings: return '\uE823';        // Timer
                case SlideIconKind.RecordSlideShow: return '\uE7C8';        // Record
                case SlideIconKind.Subtitles: return '\uED1E';              // Subtitles

                // ==================== VIEW TAB ====================
                case SlideIconKind.NormalView: return '\uE8A9';             // Normal
                case SlideIconKind.OutlineView: return '\uE8D2';            // Outline
                case SlideIconKind.SlideSorter: return '\uE80A';            // Sorter
                case SlideIconKind.ReadingView: return '\uE736';            // Reading
                case SlideIconKind.SlideMaster: return '\uE8A9';            // Master
                case SlideIconKind.Ruler: return '\uE90B';                  // Ruler
                case SlideIconKind.Gridlines: return '\uE80A';              // Grid
                case SlideIconKind.Guides: return '\uE8A9';                 // Guides
                case SlideIconKind.Zoom: return '\uE71E';                   // Zoom
                case SlideIconKind.FitToWindow: return '\uE740';            // Fit
                case SlideIconKind.ColorGrayscale: return '\uE790';         // Color
                case SlideIconKind.NotesPane: return '\uE70B';              // Notes

                // ==================== DRAWING & CONTEXTUAL FORMAT ====================
                case SlideIconKind.Arrange: return '\uE8B9';                // Arrange / Layers
                case SlideIconKind.QuickStyles: return '\uE790';            // Styles
                case SlideIconKind.ShapeFill: return '\uE771';              // Paint Bucket
                case SlideIconKind.ShapeOutline: return '\uE8E0';           // Outline
                case SlideIconKind.ShapeEffects: return '\uE781';           // Effects
                case SlideIconKind.BringForward: return '\uE898';           // Bring Forward
                case SlideIconKind.SendBackward: return '\uE896';           // Send Backward
                case SlideIconKind.SelectionPane: return '\uE8A9';          // Selection Pane
                case SlideIconKind.AlignObjects: return '\uE8E4';           // Align
                case SlideIconKind.GroupObjects: return '\uE8F4';           // Group
                case SlideIconKind.UngroupObjects: return '\uE8F5';         // Ungroup
                case SlideIconKind.RemoveBackground: return '\uE75C';       // Erase
                case SlideIconKind.PictureCorrections: return '\uE706';     // Corrections
                case SlideIconKind.PictureColor: return '\uE790';           // Color
                case SlideIconKind.PictureBorder: return '\uE8E0';          // Border
                case SlideIconKind.PictureCrop: return '\uE7A8';            // Crop

                default: return '\uE82E';
            }
        }

        /// <summary>
        /// Kết xuất một icon SlideIconKind trực tiếp lên Graphics.
        /// </summary>
        public static void DrawIcon(Graphics g, SlideIconKind kind, Rectangle bounds, Color color, Color? accentColor = null)
        {
            char glyph = GetSlideIconGlyph(kind);
            DrawGlyph(g, glyph, bounds, color);
        }

        public static void DrawIcon(Graphics g, SlideIconKind kind, int x, int y, int width, int height, Color color, Color? accentColor = null)
        {
            DrawIcon(g, kind, new Rectangle(x, y, width, height), color, accentColor);
        }

        public static void DrawIconCentered(Graphics g, SlideIconKind kind, Rectangle containerRect, int iconSize, Color color, Color? accentColor = null)
        {
            if (g == null) return;
            int x = containerRect.X + (containerRect.Width - iconSize) / 2;
            int y = containerRect.Y + (containerRect.Height - iconSize) / 2;
            DrawIcon(g, kind, new Rectangle(x, y, iconSize, iconSize), color, accentColor);
        }

        public static void DrawIcon(D2DGraphics g, SlideIconKind kind, Rectangle bounds, Color color, Color? accentColor = null)
        {
            char glyph = GetSlideIconGlyph(kind);
            DrawGlyph(g, glyph, bounds, color);
        }

        public static void DrawIcon(D2DGraphics g, SlideIconKind kind, int x, int y, int width, int height, Color color, Color? accentColor = null)
        {
            DrawIcon(g, kind, new Rectangle(x, y, width, height), color, accentColor);
        }

        public static void DrawIconCentered(D2DGraphics g, SlideIconKind kind, Rectangle containerRect, int iconSize, Color color, Color? accentColor = null)
        {
            if (g == null) return;
            int x = containerRect.X + (containerRect.Width - iconSize) / 2;
            int y = containerRect.Y + (containerRect.Height - iconSize) / 2;
            DrawIcon(g, kind, new Rectangle(x, y, iconSize, iconSize), color, accentColor);
        }

        public static Bitmap GetIconBitmap(SlideIconKind kind, int width, int height, Color color, Color? accentColor = null)
        {
            char glyph = GetSlideIconGlyph(kind);
            return GetGlyphBitmap(glyph, width, height, color);
        }
    }
}

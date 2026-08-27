using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using GenOffice.UI;

namespace GenOffice.Core
{
    public enum ShapeType
    {
        TextBox,
        Rectangle,
        RoundedRectangle,
        Ellipse,
        Triangle,
        Diamond,
        Star,
        Arrow,
        Line,
        Image,
        Video,
        Table,
        Chart,
        Hexagon,
        Heart,
        Callout,
        Chevron,
        RightTriangle,
        Parallelogram,
        Trapezoid,
        Octagon,
        Pentagon,
        Cloud,
        BlockArrowRight,
        BlockArrowLeft,
        BlockArrowUp,
        BlockArrowDown,
        Lightning,
        Sun,
        Moon,
        Smiley,
        Donut,
        Cross,
        Cube,
        Can,
        CustomPath,
        // Cây cỏ & Tự nhiên (Botanical & Nature)
        Leaf,
        Clover,
        Flower,
        Sprout,
        // Hình học & Huy hiệu sang trọng (Luxury Geometric & Badges)
        LuxuryHexagon,
        Gem,
        Shield,
        Banner,
        Starburst,
        Arch
    }

    public enum CropHandleType
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        PanImage
    }

    public enum MergeShapeOperation
    {
        Union,
        Combine,
        Fragment,
        Intersect,
        Subtract
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Justify
    }

    public enum VerticalTextAlignment
    {
        Top,
        Middle,
        Bottom
    }

    public enum ShapeAlignType
    {
        Left,
        Center,
        Right,
        Top,
        Middle,
        Bottom
    }

    public enum SlideLayoutType
    {
        TitleSlide,
        TitleAndContent,
        SectionHeader,
        TwoContent,
        Comparison,
        TitleOnly,
        Blank,
        ContentWithCaption,
        PictureWithCaption
    }

    public enum TransitionType
    {
        None,
        Morph,
        Fade,
        Push,
        Wipe,
        Split,
        Reveal,
        Cut,
        RandomBars,
        Wheel,
        Shape,
        Uncover,
        Cover,
        Flash,
        FallOver,
        Curtains,
        Wind,
        Prestige,
        Fracture,
        Crush,
        PeelOff,
        PageCurl,
        Airplane,
        Origami,
        Dissolve,
        Checker,
        Blinds,
        Clock,
        Ripple,
        Honeycomb,
        Glitter,
        Vortex,
        Shred,
        Switch,
        Flip,
        Gallery,
        Cube,
        Doors,
        Box,
        Comb,
        Zoom,
        Pan,
        FerrisWheel,
        Conveyor,
        Rotate,
        Window,
        Orbit,
        FlyThrough,
        // 10 Hiệu ứng Chuyển cảnh Độc quyền GenOffice (GenOffice Exclusive Transitions)
        GenOffice_CinematicPrism,
        GenOffice_LiquidFluid,
        GenOffice_MatrixRain,
        GenOffice_NeonPulse,
        GenOffice_PortalWarp,
        GenOffice_Supernova,
        GenOffice_HologramGlitch,
        GenOffice_Kaleidoscope,
        GenOffice_Starlight,
        GenOffice_GoldenRibbon,
        GenOffice_CanvaFlow
    }

    public enum MorphType
    {
        Objects,
        Words,
        Characters
    }

    public enum TransitionDirection
    {
        FromLeft,
        FromRight,
        FromTop,
        FromBottom,
        Smooth,
        In,
        Out,
        Horizontal,
        Vertical
    }

    public enum AnimationType
    {
        None,
        // Entrance (Green - Xuất hiện)
        Appear,
        FadeIn,
        FlyIn,
        FloatIn,
        SplitIn,
        WipeIn,
        ShapeIn,
        WheelIn,
        RandomBarsIn,
        GrowAndTurn,
        ZoomIn,
        Swivel,
        Bounce,

        // Emphasis (Yellow/Orange - Nhấn mạnh)
        Pulse,
        ColorPulse,
        Teeter,
        Spin,
        GrowShrink,
        Desaturate,
        Darken,
        Lighten,
        Transparency,
        ObjectColor,
        ComplementaryColor,
        LineColor,
        FillColor,
        BrushColor,
        FontColor,
        Underline,
        BoldFlash,
        BoldReveal,
        Wave,

        // Exit (Red - Thoát)
        Disappear,
        FadeOut,
        FlyOut,
        FloatOut,
        SplitOut,
        WipeOut,
        ShapeOut,
        WheelOut,
        RandomBarsOut,
        ShrinkAndTurn,
        ZoomOut,
        SwivelOut,
        BounceOut,

        // Motion Paths (Đường chuyển động)
        MotionLine,
        MotionArc,
        MotionTurn,
        MotionShape,
        MotionLoop,
        MotionCustom,

        // 10 Hiệu ứng Hoạt họa Độc quyền GenOffice (GenOffice Exclusive Animations)
        GenOffice_NeonPulse,
        GenOffice_LiquidMorph,
        GenOffice_Cinematic3DFlip,
        GenOffice_GlitchHologram,
        GenOffice_KineticSnap,
        GenOffice_VortexSpiral,
        GenOffice_OrigamiUnfold,
        GenOffice_StarlightAssembly,
        GenOffice_ElasticJellyPop,
        GenOffice_GoldenRibbonSwirl
    }

    public enum AnimationTrigger
    {
        OnClick,
        WithPrevious,
        AfterPrevious
    }

    public enum ImpressViewMode
    {
        Normal,
        Outline,
        Notes,
        SlideSorter,
        SlideMaster,
        ReadingView
    }

    public enum VideoStartMode
    {
        InClickSequence,
        Automatically,
        WhenClickedOn
    }

    public enum VideoVolumeLevel
    {
        Mute,
        Low,
        Medium,
        High
    }

    public class Transform2D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Rotation { get; set; }
        public int ZIndex { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }

        public float Right { get { return X + Width; } }
        public float Bottom { get { return Y + Height; } }

        public Transform2D(float x = 100f, float y = 100f, float w = 300f, float h = 200f, float rot = 0.0f)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            Rotation = rot;
            ZIndex = 0;
            FlipHorizontal = false;
            FlipVertical = false;
        }

        public RectangleF GetBoundingBox()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        public PointF GetCenter()
        {
            return new PointF(X + Width / 2.0f, Y + Height / 2.0f);
        }

        public Transform2D Clone()
        {
            return new Transform2D(X, Y, Width, Height, Rotation)
            {
                ZIndex = ZIndex,
                FlipHorizontal = FlipHorizontal,
                FlipVertical = FlipVertical
            };
        }
    }

    public class ShapeNode
    {
        public string Id { get; set; }
        public string MorphName { get; set; }
        public string Name { get; set; }
        public ShapeType Type { get; set; }
        public DocChartData ChartData { get; set; }
        public string SmartArtType { get; set; }
        public string SmartArtTitle { get; set; }
        public Transform2D Transform { get; set; }

        // Text Properties
        public string Text { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public bool HasTextShadow { get; set; }
        public TextAlignment Alignment { get; set; }
        public VerticalTextAlignment VerticalAlignment { get; set; }
        public int TextColor { get; set; }
        public float LineSpacing { get; set; }

        // Fill & Line Properties
        public int FillColor { get; set; }
        public bool HasGradientFill { get; set; }
        public int GradientColor2 { get; set; }
        public float GradientAngle { get; set; }
        public float FillOpacity { get; set; }
        public int StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public DashStyle DashStyle { get; set; }

        // Effect Properties
        public bool HasShadow { get; set; }
        public int ShadowColor { get; set; }
        public float ShadowOffsetX { get; set; }
        public float ShadowOffsetY { get; set; }
        public float ShadowBlur { get; set; }

        // Media / Metadata / Placeholder
        public string ImagePath { get; set; }
        public Image CachedImage { get; set; }
        public GraphicsPath CustomPath { get; set; }
        public float PathWidth { get; set; }
        public float PathHeight { get; set; }
        public string PlaceholderType { get; set; } // "Title", "Subtitle", "Body", "ThemeDecoration", "None"
        public bool IsPlaceholder { get; set; }
        public bool IsDecorative { get; set; }

        // Picture Format Properties
        public float ImageBrightness { get; set; } // -1.0f to 1.0f (default 0)
        public float ImageContrast { get; set; }   // 0.0f to 3.0f (default 1.0f)
        public float ImageSaturation { get; set; } // 0.0f to 4.0f (default 1.0f)
        public string ImageRecolor { get; set; }   // "None", "Grayscale", "Sepia", "Washout", "Blue", "Orange", "Green", "Purple"
        public int PictureStyleIndex { get; set; } // 0: None, 1: Simple White Frame, 2: Matte Dual, 3: Metal Frame, 4: Drop Shadow, 5: Soft Edge, 6: Double Frame, 7: Beveled Oval, 8: Rounded Bevel
        public float CropLeft { get; set; }
        public float CropTop { get; set; }
        public float CropRight { get; set; }
        public float CropBottom { get; set; }
        public string CropShapeType { get; set; } // "Rectangle", "RoundedRectangle", "Oval", "Triangle", "Diamond", "Hexagon", "Heart", "Star5", "Cloud", "ArrowRight", etc.
        public string AltText { get; set; }
        public float OriginalWidth { get; set; }
        public float OriginalHeight { get; set; }

        // Table Properties
        public int TableColumns { get; set; }
        public int TableRows { get; set; }
        public List<List<string>> TableData { get; set; }
        public bool TableHeaderRow { get; set; }
        public bool TableBandedRows { get; set; }
        public int TableSelectedCellRow { get; set; }
        public int TableSelectedCellCol { get; set; }
        public int TableSelectedEndRow { get; set; }
        public int TableSelectedEndCol { get; set; }
        public Dictionary<string, int> TableCellFillColors { get; set; }
        public Dictionary<string, int> TableCellTextColors { get; set; }
        public Dictionary<string, bool> TableCellBolds { get; set; }

        // Video Properties
        public string VideoPath { get; set; }
        public float VideoFadeInDuration { get; set; } // seconds, e.g. 0.00
        public float VideoFadeOutDuration { get; set; } // seconds, e.g. 0.00
        public VideoStartMode VideoStartMode { get; set; }
        public bool VideoPlayFullScreen { get; set; }
        public bool VideoHideWhileNotPlaying { get; set; }
        public bool VideoLoopUntilStopped { get; set; }
        public bool VideoRewindAfterPlaying { get; set; }
        public VideoVolumeLevel VideoVolume { get; set; }
        public string VideoVolumeLevel { get; set; }
        public List<float> VideoBookmarks { get; set; } // list of bookmark timestamps in seconds
        public float VideoTrimStart { get; set; }
        public float VideoTrimEnd { get; set; }
        public string VideoPosterFramePath { get; set; }
        public int VideoStyleIndex { get; set; } // 0: None, 1: Simple Frame, 2: Rounded Soft, 3: Metal Double, 4: Drop Shadow, 5: Beveled Oval, 6: Reflection, 7: Soft Ellipse
        public string VideoCaptionPath { get; set; }
        public string VideoCorrections { get; set; } // "Normal", "Bright", "HighContrast", etc.
        public string VideoColorFilter { get; set; } // "None", "Grayscale", "Sepia", "Warm", "Cool"

        public bool IsVideoPlaying { get; set; }
        public float VideoCurrentTime { get; set; }
        public float VideoTotalTime { get; set; }

        // Interaction State
        public bool Selected { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEditingText { get; set; }
        public int CaretPosition { get; set; }

        public string GetDisplayName(int index)
        {
            if (!string.IsNullOrEmpty(Name) && !Name.StartsWith("Hình ")) return Name;
            if (!string.IsNullOrEmpty(PlaceholderType))
            {
                if (PlaceholderType == "Title") return "Tiêu đề " + (index + 1);
                if (PlaceholderType == "Subtitle") return "Tiêu đề phụ " + (index + 1);
                if (PlaceholderType == "Body") return "Nội dung " + (index + 1);
            }
            if (!string.IsNullOrEmpty(Text))
            {
                string trimmed = Text.Trim().Replace("\r", "").Replace("\n", " ");
                if (trimmed.Length > 22) trimmed = trimmed.Substring(0, 20) + "...";
                if (!string.IsNullOrEmpty(trimmed) && trimmed != "Nhấp để thêm văn bản") return trimmed;
            }
            switch (Type)
            {
                case ShapeType.TextBox: return "Hộp văn bản " + (index + 1);
                case ShapeType.Rectangle: return "Hình chữ nhật " + (index + 1);
                case ShapeType.RoundedRectangle: return "Chữ nhật bo góc " + (index + 1);
                case ShapeType.Ellipse: return "Hình tròn / Elip " + (index + 1);
                case ShapeType.Triangle: return "Hình tam giác " + (index + 1);
                case ShapeType.Diamond: return "Hình thoi " + (index + 1);
                case ShapeType.Star: return "Ngôi sao " + (index + 1);
                case ShapeType.Heart: return "Trái tim " + (index + 1);
                case ShapeType.Hexagon: return "Hình lục giác " + (index + 1);
                case ShapeType.Arrow: return "Mũi tên " + (index + 1);
                case ShapeType.Callout: return "Hộp thoại " + (index + 1);
                case ShapeType.Image: return "Hình ảnh " + (index + 1);
                case ShapeType.Table: return "Bảng dữ liệu " + (index + 1);
                default: return "Hình vẽ " + (index + 1);
            }
        }

        public ShapeNode()
        {
            Id = Guid.NewGuid().ToString("N");
            MorphName = "";
            Name = "Hình " + Id.Substring(0, 4);
            Type = ShapeType.Rectangle;
            IsVisible = true;
            Transform = new Transform2D(100f, 100f, 300f, 200f);
            Text = "";
            FontName = "Segoe UI";
            FontSize = 18.0f;
            Bold = false;
            Italic = false;
            Underline = false;
            Strikethrough = false;
            HasTextShadow = false;
            Alignment = TextAlignment.Left;
            VerticalAlignment = VerticalTextAlignment.Middle;
            TextColor = 0x222222;
            LineSpacing = 1.2f;

            FillColor = 0xF3F4F6;
            HasGradientFill = false;
            GradientColor2 = 0xE5E7EB;
            GradientAngle = 90.0f;
            FillOpacity = 1.0f;
            StrokeColor = 0xD24726;
            StrokeWidth = 2.0f;
            DashStyle = DashStyle.Solid;

            HasShadow = false;
            ShadowColor = 0x40000000;
            ShadowOffsetX = 4.0f;
            ShadowOffsetY = 4.0f;
            ShadowBlur = 8.0f;

            ImagePath = "";
            CachedImage = null;
            CustomPath = null;
            PathWidth = 100f;
            PathHeight = 100f;
            PlaceholderType = "None";
            IsPlaceholder = false;
            TableColumns = 0;
            TableRows = 0;
            TableData = null;
            TableHeaderRow = true;
            TableBandedRows = true;
            TableSelectedCellRow = -1;
            TableSelectedCellCol = -1;
            TableSelectedEndRow = -1;
            TableSelectedEndCol = -1;
            TableCellFillColors = new Dictionary<string, int>();
            TableCellTextColors = new Dictionary<string, int>();
            TableCellBolds = new Dictionary<string, bool>();
            ImageBrightness = 0.0f;
            ImageContrast = 1.0f;
            ImageSaturation = 1.0f;
            ImageRecolor = "None";
            PictureStyleIndex = 0;
            CropLeft = 0f;
            CropTop = 0f;
            CropRight = 0f;
            CropBottom = 0f;
            CropShapeType = "Rectangle";
            AltText = "";
            OriginalWidth = 0f;
            OriginalHeight = 0f;
            Selected = false;
            IsEditingText = false;
            CaretPosition = 0;
            VideoTotalTime = 150f;
            VideoCurrentTime = 0f;
            IsVideoPlaying = false;
        }

        public ShapeNode Clone()
        {
            List<List<string>> clonedTableData = null;
            if (TableData != null)
            {
                clonedTableData = new List<List<string>>();
                foreach (var row in TableData)
                {
                    clonedTableData.Add(new List<string>(row));
                }
            }

            Dictionary<string, int> clonedFills = new Dictionary<string, int>();
            if (TableCellFillColors != null)
            {
                foreach (var kvp in TableCellFillColors) clonedFills[kvp.Key] = kvp.Value;
            }

            Dictionary<string, int> clonedTexts = new Dictionary<string, int>();
            if (TableCellTextColors != null)
            {
                foreach (var kvp in TableCellTextColors) clonedTexts[kvp.Key] = kvp.Value;
            }

            Dictionary<string, bool> clonedBolds = new Dictionary<string, bool>();
            if (TableCellBolds != null)
            {
                foreach (var kvp in TableCellBolds) clonedBolds[kvp.Key] = kvp.Value;
            }

            return new ShapeNode
            {
                Id = Id,
                MorphName = MorphName,
                Name = Name,
                Type = Type,
                IsVisible = IsVisible,
                Transform = Transform.Clone(),
                Text = Text,
                FontName = FontName,
                FontSize = FontSize,
                Bold = Bold,
                Italic = Italic,
                Underline = Underline,
                Strikethrough = Strikethrough,
                HasTextShadow = HasTextShadow,
                Alignment = Alignment,
                VerticalAlignment = VerticalAlignment,
                TextColor = TextColor,
                LineSpacing = LineSpacing,
                FillColor = FillColor,
                HasGradientFill = HasGradientFill,
                GradientColor2 = GradientColor2,
                GradientAngle = GradientAngle,
                FillOpacity = FillOpacity,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                DashStyle = DashStyle,
                HasShadow = HasShadow,
                ShadowColor = ShadowColor,
                ShadowOffsetX = ShadowOffsetX,
                ShadowOffsetY = ShadowOffsetY,
                ShadowBlur = ShadowBlur,
                ImagePath = ImagePath,
                CachedImage = (CachedImage != null) ? new Bitmap(CachedImage) : null,
                CustomPath = (CustomPath != null) ? (GraphicsPath)CustomPath.Clone() : null,
                PathWidth = PathWidth,
                PathHeight = PathHeight,
                PlaceholderType = PlaceholderType,
                IsPlaceholder = IsPlaceholder,
                IsDecorative = IsDecorative,
                ImageBrightness = ImageBrightness,
                ImageContrast = ImageContrast,
                ImageSaturation = ImageSaturation,
                ImageRecolor = ImageRecolor,
                PictureStyleIndex = PictureStyleIndex,
                CropLeft = CropLeft,
                CropTop = CropTop,
                CropRight = CropRight,
                CropBottom = CropBottom,
                CropShapeType = CropShapeType ?? "Rectangle",
                AltText = AltText,
                OriginalWidth = OriginalWidth,
                OriginalHeight = OriginalHeight,
                VideoPath = VideoPath,
                VideoFadeInDuration = VideoFadeInDuration,
                VideoFadeOutDuration = VideoFadeOutDuration,
                VideoStartMode = VideoStartMode,
                VideoPlayFullScreen = VideoPlayFullScreen,
                VideoHideWhileNotPlaying = VideoHideWhileNotPlaying,
                VideoLoopUntilStopped = VideoLoopUntilStopped,
                VideoRewindAfterPlaying = VideoRewindAfterPlaying,
                VideoVolume = VideoVolume,
                VideoVolumeLevel = VideoVolumeLevel,
                VideoBookmarks = (VideoBookmarks != null) ? new List<float>(VideoBookmarks) : null,
                VideoTrimStart = VideoTrimStart,
                VideoTrimEnd = VideoTrimEnd,
                VideoPosterFramePath = VideoPosterFramePath,
                VideoStyleIndex = VideoStyleIndex,
                VideoCaptionPath = VideoCaptionPath,
                VideoCorrections = VideoCorrections,
                VideoColorFilter = VideoColorFilter,
                TableColumns = TableColumns,
                TableRows = TableRows,
                TableData = clonedTableData,
                TableHeaderRow = TableHeaderRow,
                TableBandedRows = TableBandedRows,
                TableSelectedCellRow = TableSelectedCellRow,
                TableSelectedCellCol = TableSelectedCellCol,
                TableSelectedEndRow = TableSelectedEndRow,
                TableSelectedEndCol = TableSelectedEndCol,
                TableCellFillColors = clonedFills,
                TableCellTextColors = clonedTexts,
                TableCellBolds = clonedBolds,
                Selected = false,
                IsEditingText = false
            };
        }
    }

    public enum BackgroundFillType
    {
        Solid,
        Gradient,
        Picture,
        Pattern
    }

    public enum GradientType
    {
        Linear,
        Radial,
        Rectangular,
        Path
    }

    public enum PatternFillStyle
    {
        Grid5Percent,
        Grid10Percent,
        HorizontalLight,
        VerticalLight,
        DiagonalUpLight,
        DiagonalDownLight,
        Crosshatch,
        ZigZag,
        Brick,
        Dots,
        Checker,
        Waves
    }

    public class GradientStop
    {
        public int Color { get; set; } // 0xRRGGBB
        public float Position { get; set; } // 0.0f to 1.0f
        public float Transparency { get; set; } // 0.0f to 1.0f
        public float Brightness { get; set; } // -1.0f to 1.0f

        public GradientStop()
        {
            Color = 0xFFFFFF;
            Position = 0.0f;
            Transparency = 0.0f;
            Brightness = 0.0f;
        }

        public GradientStop(int color, float position, float transparency = 0.0f, float brightness = 0.0f)
        {
            Color = color;
            Position = position;
            Transparency = transparency;
            Brightness = brightness;
        }

        public GradientStop Clone()
        {
            return new GradientStop(Color, Position, Transparency, Brightness);
        }
    }

    public class PresetGradient
    {
        public string Name { get; set; }
        public List<GradientStop> Stops { get; set; }
        public float Angle { get; set; }

        public PresetGradient(string name, float angle, params GradientStop[] stops)
        {
            Name = name;
            Angle = angle;
            Stops = new List<GradientStop>(stops);
        }

        public static List<PresetGradient> GetPresets()
        {
            return new List<PresetGradient>
            {
                new PresetGradient("Bạc Kim loại (Titanium)", 90f, new GradientStop(0xFFFFFF, 0.0f), new GradientStop(0xE2E8F0, 0.5f), new GradientStop(0x94A3B8, 1.0f)),
                new PresetGradient("Hoàng hôn Ấm áp (Warm Sunset)", 90f, new GradientStop(0xFFF176, 0.0f), new GradientStop(0xFFB74D, 0.45f), new GradientStop(0xF06292, 1.0f)),
                new PresetGradient("Đại dương Xanh (Ocean Breeze)", 90f, new GradientStop(0xE0F2FE, 0.0f), new GradientStop(0x38BDF8, 0.5f), new GradientStop(0x0284C7, 1.0f)),
                new PresetGradient("Rừng Nhiệt đới (Emerald)", 90f, new GradientStop(0xECFDF5, 0.0f), new GradientStop(0x34D399, 0.5f), new GradientStop(0x059669, 1.0f)),
                new PresetGradient("Hoàng gia Tím (Royal Purple)", 90f, new GradientStop(0xFAF5FF, 0.0f), new GradientStop(0xC084FC, 0.5f), new GradientStop(0x7E22CE, 1.0f)),
                new PresetGradient("Cyber Neon (Neon Glow)", 90f, new GradientStop(0x0F172A, 0.0f), new GradientStop(0x1E293B, 0.4f), new GradientStop(0x06B6D4, 1.0f)),
                new PresetGradient("Vàng Champagne (Golden Hour)", 90f, new GradientStop(0xFFFBEB, 0.0f), new GradientStop(0xFDE68A, 0.5f), new GradientStop(0xF59E0B, 1.0f)),
                new PresetGradient("Hoa Anh đào (Sakura Rose)", 90f, new GradientStop(0xFFF1F2, 0.0f), new GradientStop(0xFDA4AF, 0.5f), new GradientStop(0xF43F5E, 1.0f)),
                new PresetGradient("Đêm Huyền bí (Midnight Blue)", 90f, new GradientStop(0x1E1B4B, 0.0f), new GradientStop(0x312E81, 0.5f), new GradientStop(0x4338CA, 1.0f)),
                new PresetGradient("Băng Tuyết Bạc hà (Mint Frost)", 90f, new GradientStop(0xF0FDFA, 0.0f), new GradientStop(0x99F6E4, 0.5f), new GradientStop(0x14B8A6, 1.0f)),
                new PresetGradient("Hồng Pastel (Soft Coral)", 90f, new GradientStop(0xFFF7ED, 0.0f), new GradientStop(0xFFEDD5, 0.5f), new GradientStop(0xFB923C, 1.0f)),
                new PresetGradient("Khói Núi (Charcoal Mist)", 90f, new GradientStop(0xF8FAFC, 0.0f), new GradientStop(0xE2E8F0, 0.5f), new GradientStop(0x64748B, 1.0f))
            };
        }
    }

    public enum SidebarPanelType
    {
        None,
        Properties,
        SlideTransitions,
        CustomAnimations,
        MasterSlides,
        Navigator,
        FormatShape,
        AnimationPane,
        FormatBackground,
        SelectionPane
    }

    public enum NeonGlowTarget
    {
        Both,
        ShapeOnly,
        TextOnly
    }

    public enum NeonColorMode
    {
        Monochrome,
        DualTone,
        Rainbow
    }

    public class ShapeAnimation
    {
        public string Id { get; set; }
        public string ShapeId { get; set; }
        public AnimationType Type { get; set; }
        public AnimationTrigger Trigger { get; set; }
        public float Duration { get; set; }
        public float Delay { get; set; }
        public TransitionDirection Direction { get; set; }
        public int OrderIndex { get; set; }

        // Motion Path Custom Points & Endpoint Drag Adjustments
        public List<PointF> CustomPoints { get; set; }
        public float EndOffsetX { get; set; }
        public float EndOffsetY { get; set; }
        public bool HasCustomEndOffset { get; set; }

        // Neon Pulse Specific Customizable Parameters
        public NeonGlowTarget NeonTarget { get; set; }
        public int NeonColor { get; set; } // 0 = Auto / Inherit
        public int NeonSecondaryColor { get; set; }
        public NeonColorMode NeonColorMode { get; set; }
        public bool NeonInfiniteLoop { get; set; }
        public int NeonRepeatCount { get; set; }
        public float NeonBloomRadius { get; set; }
        public float NeonIntensity { get; set; }
        public bool NeonCornerFlares { get; set; }

        public bool IsEntrance { get { return (Type >= AnimationType.Appear && Type <= AnimationType.Bounce) || IsExclusive; } }
        public bool IsEmphasis { get { return Type >= AnimationType.Pulse && Type <= AnimationType.Wave; } }
        public bool IsExit { get { return Type >= AnimationType.Disappear && Type <= AnimationType.BounceOut; } }
        public bool IsMotionPath { get { return Type >= AnimationType.MotionLine && Type <= AnimationType.MotionCustom; } }
        public bool IsExclusive { get { return Type >= AnimationType.GenOffice_NeonPulse && Type <= AnimationType.GenOffice_GoldenRibbonSwirl; } }

        public static bool HasEffectOptions(AnimationType type)
        {
            switch (type)
            {
                // Standard PowerPoint Entrance with directions
                case AnimationType.FlyIn:
                case AnimationType.FloatIn:
                case AnimationType.SplitIn:
                case AnimationType.WipeIn:
                case AnimationType.ShapeIn:
                case AnimationType.WheelIn:
                case AnimationType.RandomBarsIn:
                case AnimationType.ZoomIn:
                // Standard PowerPoint Emphasis with options
                case AnimationType.Spin:
                case AnimationType.GrowShrink:
                // Standard PowerPoint Exit with directions
                case AnimationType.FlyOut:
                case AnimationType.FloatOut:
                case AnimationType.SplitOut:
                case AnimationType.WipeOut:
                case AnimationType.ShapeOut:
                case AnimationType.WheelOut:
                case AnimationType.RandomBarsOut:
                case AnimationType.ZoomOut:
                // Motion Paths with directional presets
                case AnimationType.MotionLine:
                case AnimationType.MotionArc:
                case AnimationType.MotionTurn:
                case AnimationType.MotionShape:
                case AnimationType.MotionLoop:
                // Exclusive animations that support direction variations
                case AnimationType.GenOffice_KineticSnap:
                case AnimationType.GenOffice_GoldenRibbonSwirl:
                case AnimationType.GenOffice_Cinematic3DFlip:
                case AnimationType.GenOffice_OrigamiUnfold:
                case AnimationType.GenOffice_GlitchHologram:
                case AnimationType.GenOffice_LiquidMorph:
                case AnimationType.GenOffice_VortexSpiral:
                case AnimationType.GenOffice_NeonPulse:
                    return true;

                // In-place effects (Appear, Fade, Pulse, Bounce, Swivel, Starlight, Jelly, etc.) have no direction options in PowerPoint
                default:
                    return false;
            }
        }

        public bool SupportsEffectOptions { get { return HasEffectOptions(Type); } }

        public ShapeAnimation()
        {
            Id = Guid.NewGuid().ToString("N");
            ShapeId = "";
            Type = AnimationType.FadeIn;
            Trigger = AnimationTrigger.OnClick;
            Duration = 0.5f;
            Delay = 0.0f;
            Direction = TransitionDirection.Smooth;
            OrderIndex = 0;
            CustomPoints = new List<PointF>();
            EndOffsetX = 0f;
            EndOffsetY = 0f;
            HasCustomEndOffset = false;

            // Default Neon Pulse settings
            NeonTarget = NeonGlowTarget.Both;
            NeonColor = 0;
            NeonSecondaryColor = 0;
            NeonColorMode = NeonColorMode.Monochrome;
            NeonInfiniteLoop = false;
            NeonRepeatCount = 1;
            NeonBloomRadius = 28.0f;
            NeonIntensity = 1.0f;
            NeonCornerFlares = true;
        }

        public ShapeAnimation Clone()
        {
            return new ShapeAnimation
            {
                Id = Guid.NewGuid().ToString("N"),
                ShapeId = ShapeId,
                Type = Type,
                Trigger = Trigger,
                Duration = Duration,
                Delay = Delay,
                Direction = Direction,
                OrderIndex = OrderIndex,
                CustomPoints = CustomPoints != null ? new List<PointF>(CustomPoints) : new List<PointF>(),
                EndOffsetX = EndOffsetX,
                EndOffsetY = EndOffsetY,
                HasCustomEndOffset = HasCustomEndOffset,
                NeonTarget = NeonTarget,
                NeonColor = NeonColor,
                NeonSecondaryColor = NeonSecondaryColor,
                NeonColorMode = NeonColorMode,
                NeonInfiniteLoop = NeonInfiniteLoop,
                NeonRepeatCount = NeonRepeatCount,
                NeonBloomRadius = NeonBloomRadius,
                NeonIntensity = NeonIntensity,
                NeonCornerFlares = NeonCornerFlares
            };
        }
    }

    public class SlideNode
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int BgColor { get; set; }
        public bool IsGradientBg { get; set; }
        public int BgColor2 { get; set; }
        public float BgGradientAngle { get; set; }
        public GradientType BgGradientType { get; set; }
        public List<GradientStop> BgGradientStops { get; set; }
        public int SelectedGradientStopIndex { get; set; }

        public PatternFillStyle BgPatternStyle { get; set; }
        public int BgPatternForeColor { get; set; }
        public int BgPatternBackColor { get; set; }

        public string BgImagePath { get; set; }
        public Image CachedBgImage { get; set; }
        public BackgroundFillType BgFillType { get; set; }
        public float BgTransparency { get; set; }
        public bool HideBackgroundGraphics { get; set; }

        public SlideLayoutType Layout { get; set; }
        public string SpeakerNotes { get; set; }
        public string Notes { get { return SpeakerNotes; } set { SpeakerNotes = value; } }
        public bool IsHidden { get; set; }
        public string SectionName { get; set; }
        public bool IsSectionHeader { get; set; }
        public bool IsSectionCollapsed { get; set; }

        // Transition Settings (Office 2016+)
        public TransitionType Transition { get; set; }
        public float TransitionDuration { get; set; }
        public TransitionDirection TransitionDirection { get; set; }
        public MorphType MorphMode { get; set; }
        public bool AdvanceOnClick { get; set; }
        public bool AutoAdvance { get; set; }
        public float AutoAdvanceSeconds { get; set; }
        public string TransitionSound { get; set; }

        public List<ShapeNode> Shapes { get; set; }
        public List<ShapeAnimation> Animations { get; set; }

        public SlideNode()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "Slide không tên";
            BgColor = 0xFFFFFF;
            IsGradientBg = false;
            BgColor2 = 0xF3F4F6;
            BgGradientAngle = 90.0f;
            BgGradientType = GradientType.Linear;
            SelectedGradientStopIndex = 0;
            BgGradientStops = new List<GradientStop>
            {
                new GradientStop(0xFFFFFF, 0.0f),
                new GradientStop(0xF1F5F9, 0.5f),
                new GradientStop(0xE2E8F0, 1.0f)
            };

            BgPatternStyle = PatternFillStyle.Grid5Percent;
            BgPatternForeColor = 0xCBD5E1;
            BgPatternBackColor = 0xFFFFFF;

            BgImagePath = "";
            CachedBgImage = null;
            BgFillType = BackgroundFillType.Solid;
            BgTransparency = 0.0f;
            HideBackgroundGraphics = false;

            Layout = SlideLayoutType.TitleAndContent;
            SpeakerNotes = "";
            IsHidden = false;
            SectionName = "";
            IsSectionHeader = false;
            IsSectionCollapsed = false;

            Transition = TransitionType.Fade;
            TransitionDuration = 1.0f;
            TransitionDirection = TransitionDirection.Smooth;
            MorphMode = MorphType.Objects;
            AdvanceOnClick = true;
            AutoAdvance = false;
            AutoAdvanceSeconds = 5.0f;
            TransitionSound = "[No Sound]";

            Shapes = new List<ShapeNode>();
            Animations = new List<ShapeAnimation>();
        }

        public void AddShape(ShapeNode shape)
        {
            shape.Transform.ZIndex = Shapes.Count;
            Shapes.Add(shape);
        }

        public void RemoveShape(ShapeNode shape)
        {
            Shapes.Remove(shape);
            ReindexZ();
        }

        public void BringToFront(ShapeNode shape)
        {
            if (Shapes.Remove(shape))
            {
                Shapes.Add(shape);
                ReindexZ();
            }
        }

        public void SendToBack(ShapeNode shape)
        {
            if (Shapes.Remove(shape))
            {
                Shapes.Insert(0, shape);
                ReindexZ();
            }
        }

        public void ReindexZ()
        {
            for (int i = 0; i < Shapes.Count; i++)
            {
                Shapes[i].Transform.ZIndex = i;
            }
        }

        public SlideNode Clone()
        {
            SlideNode copy = new SlideNode
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = Title + " (Bản sao)",
                BgColor = BgColor,
                IsGradientBg = IsGradientBg,
                BgColor2 = BgColor2,
                BgGradientAngle = BgGradientAngle,
                BgGradientType = BgGradientType,
                SelectedGradientStopIndex = SelectedGradientStopIndex,
                BgPatternStyle = BgPatternStyle,
                BgPatternForeColor = BgPatternForeColor,
                BgPatternBackColor = BgPatternBackColor,
                BgImagePath = BgImagePath,
                CachedBgImage = CachedBgImage,
                BgFillType = BgFillType,
                BgTransparency = BgTransparency,
                HideBackgroundGraphics = HideBackgroundGraphics,
                Layout = Layout,
                SpeakerNotes = SpeakerNotes,
                IsHidden = IsHidden,
                SectionName = SectionName,
                IsSectionHeader = IsSectionHeader,
                IsSectionCollapsed = IsSectionCollapsed,
                Transition = Transition,
                TransitionDuration = TransitionDuration,
                TransitionDirection = TransitionDirection,
                MorphMode = MorphMode,
                AdvanceOnClick = AdvanceOnClick,
                AutoAdvance = AutoAdvance,
                AutoAdvanceSeconds = AutoAdvanceSeconds,
                TransitionSound = TransitionSound
            };
            copy.BgGradientStops = new List<GradientStop>();
            if (BgGradientStops != null)
            {
                foreach (GradientStop gs in BgGradientStops)
                {
                    copy.BgGradientStops.Add(gs.Clone());
                }
            }
            foreach (ShapeNode s in Shapes)
            {
                copy.Shapes.Add(s.Clone());
            }
            foreach (ShapeAnimation a in Animations)
            {
                copy.Animations.Add(a.Clone());
            }
            return copy;
        }
    }

    public class PresentationTheme
    {
        public string Name { get; set; }
        public int PrimaryColor { get; set; }
        public int AccentColor { get; set; }
        public int DarkColor { get; set; }
        public int LightColor { get; set; }
        public int LightColor2 { get; set; }
        public string TitleFont { get; set; }
        public string BodyFont { get; set; }
        public bool IsGradient { get; set; }
        public float GradientAngle { get; set; }
        public string Category { get; set; }
        public List<GradientStop> Stops { get; set; }

        public PresentationTheme(string name, int primary, int accent, int dark, int light, int light2, string titleFont = "Segoe UI", string bodyFont = "Segoe UI", bool isGradient = true, float angle = 90f, string category = "Chuyên nghiệp")
        {
            Name = name;
            PrimaryColor = primary;
            AccentColor = accent;
            DarkColor = dark;
            LightColor = light;
            LightColor2 = light2;
            TitleFont = titleFont;
            BodyFont = bodyFont;
            IsGradient = isGradient;
            GradientAngle = angle;
            Category = category;

            Stops = new List<GradientStop>
            {
                new GradientStop(light, 0.0f),
                new GradientStop(light2, 1.0f)
            };
        }
    }

    public class PresentationRoot
    {
        public float CanvasWidth { get; set; }
        public float CanvasHeight { get; set; }
        public string Title { get; set; }
        public List<SlideNode> Slides { get; set; }
        public List<SlideNode> MasterSlides { get; set; }
        public int ThemePrimaryColor { get; set; }
        public string CurrentTheme { get; set; }
        public int CurrentVariantIndex { get; set; }
        public bool IsWidescreen { get; set; }

        public List<PresentationTheme> Themes { get; set; }

        public PresentationRoot()
        {
            CanvasWidth = 1920.0f;
            CanvasHeight = 1080.0f;
            Title = "Presentation1.pptx";
            Slides = new List<SlideNode>();
            MasterSlides = new List<SlideNode>();
            ThemePrimaryColor = 0x2563EB;
            CurrentTheme = "Blank";
            CurrentVariantIndex = 0;
            IsWidescreen = true;

            Themes = new List<PresentationTheme>
            {
                // 1. Nhóm Cơ Bản & Doanh nghiệp
                new PresentationTheme("Blank", 0x2563EB, 0x64748B, 0x0F172A, 0xFFFFFF, 0xFFFFFF, "Segoe UI", "Segoe UI", false, 90f, "Cơ bản"),
                new PresentationTheme("Office", 0xD24726, 0x106EBE, 0x1F2937, 0xFFFFFF, 0xF8FAFC, "Segoe UI", "Segoe UI", true, 90f, "Chuyên nghiệp"),
                new PresentationTheme("Wisp", 0x0D9488, 0x059669, 0x134E4A, 0xF0FDFA, 0xCCFBF1, "Segoe UI Semibold", "Segoe UI", true, 90f, "Chuyên nghiệp"),
                new PresentationTheme("Celestial", 0x2563EB, 0x06B6D4, 0x0F172A, 0xF8FAFC, 0xE0F2FE, "Calibri Light", "Calibri", true, 90f, "Công nghệ"),
                new PresentationTheme("Slate", 0x4F46E5, 0x7C3AED, 0x1E1B4B, 0xF5F3FF, 0xEDE9FE, "Segoe UI", "Segoe UI", true, 90f, "Thanh lịch"),
                
                // 2. Nhóm Lát Cắt & Hình Học / Slices & Geometry
                new PresentationTheme("Facet", 0x65A30D, 0x15803D, 0x14532D, 0xFFFFFF, 0xFFFFFF, "Trebuchet MS", "Segoe UI", false, 90f, "Hình học"),
                new PresentationTheme("Slice", 0xDC2626, 0x0D9488, 0x0F172A, 0xFFFFFF, 0xF8FAFC, "Century Gothic", "Segoe UI", false, 90f, "Hình học"),
                new PresentationTheme("Bauhaus", 0xDC2626, 0x2563EB, 0x111827, 0xFFFFFF, 0xF3F4F6, "Arial Black", "Segoe UI", true, 90f, "Tối giản"),
                new PresentationTheme("Ion", 0xEA580C, 0xF59E0B, 0x262626, 0xFFFBEB, 0xFEF3C7, "Century Gothic", "Segoe UI", true, 90f, "Sáng tạo"),

                // 3. Nhóm Hoa Lá Hẹ & Tự Nhiên / Botanical
                new PresentationTheme("Organic", 0x991B1B, 0x585E4B, 0x283020, 0xF5F7ED, 0xEAEED9, "Georgia", "Calibri", true, 90f, "Hoa lá hẹ"),
                new PresentationTheme("Floral Pastel", 0xE11D48, 0x10B981, 0x881337, 0xFFF1F2, 0xFFE4E6, "Palatino Linotype", "Segoe UI", true, 90f, "Hoa lá hẹ"),
                new PresentationTheme("Damask", 0x9333EA, 0xDB2777, 0x581C87, 0xFDF4FF, 0xFAE8FF, "Garamond", "Calibri", true, 90f, "Hoàng gia"),
                
                // 4. Nhóm Sóng Lụa & Đột Phá Cao Cấp
                new PresentationTheme("Cyber Wave", 0x06B6D4, 0xDC2626, 0xF8FAFC, 0x000000, 0x05070E, "Consolas", "Segoe UI", false, 90f, "Sóng lụa tối"),
                new PresentationTheme("Cyber Neon", 0x06B6D4, 0xA855F7, 0xF8FAFC, 0x0F172A, 0x1E293B, "Consolas", "Segoe UI", true, 90f, "Công nghệ tối"),
                new PresentationTheme("Gold Luxury", 0xD97706, 0xB45309, 0xF8FAFC, 0x18181B, 0x27272A, "Times New Roman", "Georgia", true, 90f, "Sang trọng"),
                new PresentationTheme("Watercolor", 0x0284C7, 0xF472B6, 0x0C4A6E, 0xF0F9FF, 0xE0F2FE, "Segoe UI Light", "Segoe UI", true, 90f, "Nghệ thuật"),
                new PresentationTheme("Warm Sunset", 0xF97316, 0xEAB308, 0x431407, 0xFFF7ED, 0xFED7AA, "Trebuchet MS", "Segoe UI", true, 90f, "Hoàng hôn")
            };

            CreateDefaultPresentation();
        }

        public void SetSlideSize(bool widescreen)
        {
            IsWidescreen = widescreen;
            if (widescreen)
            {
                CanvasWidth = 1920.0f;
                CanvasHeight = 1080.0f;
            }
            else
            {
                CanvasWidth = 1440.0f;
                CanvasHeight = 1080.0f;
            }
        }

        public void CreateDefaultPresentation()
        {
            Slides.Clear();
            MasterSlides.Clear();

            // Slide Master 1 (Clean Blank)
            SlideNode master1 = new SlideNode
            {
                Title = "Default PowerPoint Master",
                BgColor = 0xFFFFFF
            };
            MasterSlides.Add(master1);

            // Slide 1: Clean Title Slide (Khung Tiêu đề & Phụ đề chuẩn PowerPoint)
            SlideNode slide1 = new SlideNode
            {
                Title = "Slide 1",
                Layout = SlideLayoutType.TitleSlide,
                BgColor = 0xFFFFFF,
                Transition = TransitionType.Fade,
                TransitionDuration = 0.5f
            };

            ShapeNode mainTitle = new ShapeNode
            {
                Id = "title_shape_" + DateTime.Now.Ticks,
                Name = "Title",
                Type = ShapeType.TextBox,
                Transform = new Transform2D(200f, 300f, 1520f, 160f),
                Text = "Nhấp để thêm tiêu đề",
                FontName = "Segoe UI",
                FontSize = 44.0f,
                Bold = true,
                Alignment = TextAlignment.Center,
                TextColor = 0x111111,
                FillColor = 0x00FFFFFF,
                StrokeWidth = 0f,
                PlaceholderType = "Title",
                IsPlaceholder = true
            };

            ShapeNode subTitle = new ShapeNode
            {
                Id = "sub_shape_" + DateTime.Now.Ticks,
                Name = "Subtitle",
                Type = ShapeType.TextBox,
                Transform = new Transform2D(280f, 490f, 1360f, 100f),
                Text = "Nhấp để thêm phụ đề",
                FontName = "Segoe UI",
                FontSize = 24.0f,
                Alignment = TextAlignment.Center,
                TextColor = 0x666666,
                FillColor = 0x00FFFFFF,
                StrokeWidth = 0f,
                PlaceholderType = "Subtitle",
                IsPlaceholder = true
            };

            slide1.AddShape(mainTitle);
            slide1.AddShape(subTitle);
            Slides.Add(slide1);
        }

        public SlideNode AddSlide(SlideLayoutType layout = SlideLayoutType.TitleAndContent)
        {
            SlideNode slide = new SlideNode
            {
                Title = "Slide " + (Slides.Count + 1),
                Layout = layout,
                Transition = TransitionType.Fade,
                TransitionDuration = 1.0f
            };

            ShapeNode titleShape = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(100f, 80f, 1720f, 120f),
                Text = "Tiêu Đề Slide " + (Slides.Count + 1),
                FontName = "Segoe UI",
                FontSize = 36.0f,
                Bold = true,
                TextColor = 0x111111,
                FillColor = 0x00FFFFFF,
                StrokeWidth = 0f,
                PlaceholderType = "Title",
                IsPlaceholder = true
            };
            slide.AddShape(titleShape);

            if (layout == SlideLayoutType.TitleAndContent)
            {
                ShapeNode contentShape = new ShapeNode
                {
                    Type = ShapeType.TextBox,
                    Transform = new Transform2D(100f, 240f, 1720f, 720f),
                    Text = "Nhấp vào đây để thêm nội dung thuyết trình...",
                    FontName = "Segoe UI",
                    FontSize = 22.0f,
                    TextColor = 0x666666,
                    FillColor = 0x00FFFFFF,
                    StrokeWidth = 0f,
                    PlaceholderType = "Body",
                    IsPlaceholder = true
                };
                slide.AddShape(contentShape);
            }

            Slides.Add(slide);
            return slide;
        }

        public void DuplicateSlide(int index)
        {
            if (index >= 0 && index < Slides.Count)
            {
                SlideNode copy = Slides[index].Clone();
                Slides.Insert(index + 1, copy);
            }
        }

        public void DeleteSlide(int index)
        {
            if (index >= 0 && index < Slides.Count && Slides.Count > 1)
            {
                Slides.RemoveAt(index);
            }
        }

        public void MoveSlide(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < Slides.Count && toIndex >= 0 && toIndex < Slides.Count && fromIndex != toIndex)
            {
                SlideNode target = Slides[fromIndex];
                Slides.RemoveAt(fromIndex);
                Slides.Insert(toIndex, target);
            }
        }

        public PresentationRoot Clone()
        {
            PresentationRoot copy = new PresentationRoot
            {
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                Title = Title,
                ThemePrimaryColor = ThemePrimaryColor,
                CurrentTheme = CurrentTheme,
                CurrentVariantIndex = CurrentVariantIndex,
                IsWidescreen = IsWidescreen
            };
            copy.Slides = new List<SlideNode>();
            if (Slides != null)
            {
                foreach (var s in Slides)
                {
                    copy.Slides.Add(s.Clone());
                }
            }
            copy.MasterSlides = new List<SlideNode>();
            if (MasterSlides != null)
            {
                foreach (var ms in MasterSlides)
                {
                    copy.MasterSlides.Add(ms.Clone());
                }
            }
            return copy;
        }
    }

    public class BgRemovalStroke
    {
        public bool IsKeep { get; set; }
        public List<PointF> Points { get; set; }
        public float BrushRadius { get; set; }

        public BgRemovalStroke()
        {
            Points = new List<PointF>();
            BrushRadius = 14f;
            IsKeep = true;
        }
    }
}


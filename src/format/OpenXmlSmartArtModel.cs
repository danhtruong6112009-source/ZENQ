using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ZenQOffice.UI;

namespace ZenQOffice.Format
{
    /// <summary>
    /// Model strongly-typed biểu diễn một nút trong cấu trúc SmartArt của Microsoft Office (OpenXML dgm:pt).
    /// </summary>
    public class OpenXmlSmartArtNode
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public Color FillColor { get; set; }
        public Color OutlineColor { get; set; }
        public Color TextColor { get; set; }
        public string ShapeType { get; set; }
        public float ScaleFactor { get; set; }
        public int Level { get; set; }

        public OpenXmlSmartArtNode()
        {
            Id = Guid.NewGuid().ToString("N");
            ShapeType = "RoundedRectangle";
            ScaleFactor = 1.0f;
            Level = 0;
            FillColor = Color.FromArgb(43, 87, 154);
            OutlineColor = Color.FromArgb(30, 60, 110);
            TextColor = Color.White;
            Text = "[Text]";
        }

        public OpenXmlSmartArtNode(string text, Color fill) : this()
        {
            Text = text;
            FillColor = fill;
            OutlineColor = ControlPaint.Dark(fill, 0.15f);
        }
    }

    /// <summary>
    /// Precision unit conversion formulas between OpenXML EMUs, DXA/Twips, Eighths of pt, Points, and Pixels.
    /// </summary>
    public static class OpenXmlUnitConverter
    {
        public const float EmuPerPoint = 12700.0f;
        public const float EmuPerPixel = 9525.0f;
        public const float TwipsPerPoint = 20.0f;
        public const float TwipsPerPixel = 15.0f;
        public const float BorderEighthsPerPoint = 8.0f;

        public static float EmuToPoints(long emu)
        {
            return emu / EmuPerPoint;
        }

        public static long PointsToEmu(float pt)
        {
            return (long)Math.Round(pt * EmuPerPoint);
        }

        public static float EmuToPixels(long emu)
        {
            return emu / EmuPerPixel;
        }

        public static long PixelsToEmu(float px)
        {
            return (long)Math.Round(px * EmuPerPixel);
        }

        public static float TwipsToPoints(int twips)
        {
            return twips / TwipsPerPoint;
        }

        public static int PointsToTwips(float pt)
        {
            return (int)Math.Round(pt * TwipsPerPoint);
        }

        public static float TwipsToPixels(int twips)
        {
            return twips / TwipsPerPixel;
        }

        public static int PixelsToTwips(float px)
        {
            return (int)Math.Round(px * TwipsPerPixel);
        }

        public static float BorderSzToPoints(int sz)
        {
            return sz / BorderEighthsPerPoint;
        }

        public static int PointsToBorderSz(float pt)
        {
            return (int)Math.Round(pt * BorderEighthsPerPoint);
        }

        public static float DrawingMLAngleToDegrees(long angleUnits)
        {
            return angleUnits / 60000.0f;
        }

        public static long DegreesToDrawingMLAngle(float deg)
        {
            return (long)Math.Round(deg * 60000.0f);
        }
    }

    /// <summary>
    /// Quản lý dữ liệu SmartArt theo kiến trúc 4 tầng OpenXML: DataModel, Layout, Colors và Styles.
    /// Giúp chuyển đổi linh hoạt bố cục, bảng màu và hiệu ứng mà không bao giờ mất chữ của người dùng.
    /// </summary>
    public class OpenXmlSmartArtModel
    {
        public string LayoutType { get; set; }
        public string Title { get; set; }
        public int VisualStylePreset { get; set; }
        public int ColorPaletteIndex { get; set; }
        public List<OpenXmlSmartArtNode> Nodes { get; set; }

        public Rectangle OriginalBoundsEmu { get; set; }
        public bool AspectRatioLock { get; set; }
        public Color[] ResolvedThemePalette { get; set; }

        public OpenXmlSmartArtModel()
        {
            LayoutType = "Cycle_Radial";
            Title = "SmartArt Diagram";
            VisualStylePreset = 0;
            ColorPaletteIndex = 0;
            Nodes = new List<OpenXmlSmartArtNode>();
            OriginalBoundsEmu = Rectangle.Empty;
            AspectRatioLock = false;
            ResolvedThemePalette = null;
        }

        public static bool IsCycleOrRadialLayout(string layoutType)
        {
            if (string.IsNullOrEmpty(layoutType)) return false;
            return layoutType.StartsWith("Cycle", StringComparison.OrdinalIgnoreCase) ||
                   layoutType.IndexOf("Radial", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   layoutType.IndexOf("Ring", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   layoutType.IndexOf("Pie", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Nạp dữ liệu từ DocShapeObject vào model hiện tại.
        /// </summary>
        public void LoadFromDocShapeObject(DocShapeObject shape)
        {
            PopulateFromDocShapeObject(shape);
        }

        /// <summary>
        /// Nạp dữ liệu từ DocShapeObject vào model hiện tại.
        /// </summary>
        public void PopulateFromDocShapeObject(DocShapeObject shape)
        {
            if (shape == null) return;
            this.LayoutType = !string.IsNullOrEmpty(shape.SmartArtType) ? shape.SmartArtType : "Cycle_Radial";
            this.Title = shape.SmartArtTitle ?? "SmartArt";
            this.VisualStylePreset = shape.VisualStylePreset;
            this.ColorPaletteIndex = shape.StylePresetIndex;
            this.OriginalBoundsEmu = shape.OriginalBoundsEmu;
            this.ResolvedThemePalette = shape.ResolvedThemePalette;
            this.AspectRatioLock = shape.AspectRatioLock || IsCycleOrRadialLayout(this.LayoutType);

            this.Nodes.Clear();

            string[] defaultNodeTexts = SmartArtRenderer.GetSmartArtNodeList(this.LayoutType, shape.TextContent);
            int nodeCount = Math.Max(defaultNodeTexts != null ? defaultNodeTexts.Length : 0, 
                                     shape.SubShapeRelativeBounds != null ? shape.SubShapeRelativeBounds.Count : 0);
            if (nodeCount <= 0) nodeCount = 4;

            for (int i = 0; i < nodeCount; i++)
            {
                var node = new OpenXmlSmartArtNode();
                node.Id = i.ToString();
                
                // 1. Text: Ưu tiên lấy text thực tế đang sửa qua GetNodeText
                string curTxt = shape.GetNodeText(i);
                if (!string.IsNullOrEmpty(curTxt))
                    node.Text = curTxt;
                else if (defaultNodeTexts != null && i < defaultNodeTexts.Length)
                    node.Text = defaultNodeTexts[i];
                else
                    node.Text = "[Text]";

                // 2. Color
                if (shape.NodeColors != null && shape.NodeColors.ContainsKey(i))
                    node.FillColor = shape.NodeColors[i];
                else if (shape.DefaultNodeColors != null && shape.DefaultNodeColors.ContainsKey(i))
                    node.FillColor = shape.DefaultNodeColors[i];
                else
                    node.FillColor = Color.FromArgb(43, 87, 154);

                // 3. Shape Type
                if (shape.NodeShapeTypes != null && shape.NodeShapeTypes.ContainsKey(i))
                    node.ShapeType = shape.NodeShapeTypes[i];

                // 4. Outline Color
                if (shape.NodeOutlineColors != null && shape.NodeOutlineColors.ContainsKey(i))
                    node.OutlineColor = shape.NodeOutlineColors[i];
                else
                    node.OutlineColor = ControlPaint.Dark(node.FillColor, 0.15f);

                // 5. Text Color
                if (shape.NodeTextColors != null && shape.NodeTextColors.ContainsKey(i))
                    node.TextColor = shape.NodeTextColors[i];
                else
                    node.TextColor = (this.VisualStylePreset == 4) ? node.FillColor : Color.White;

                // 6. Scale
                if (shape.NodeScaleDeltas != null && shape.NodeScaleDeltas.ContainsKey(i))
                    node.ScaleFactor = 1.0f + shape.NodeScaleDeltas[i];

                this.Nodes.Add(node);
            }
        }

        /// <summary>
        /// Tạo OpenXmlSmartArtModel từ một đối tượng DocShapeObject hiện tại.
        /// </summary>
        public static OpenXmlSmartArtModel FromDocShapeObject(DocShapeObject shape)
        {
            if (shape == null) return null;
            var model = new OpenXmlSmartArtModel();
            model.PopulateFromDocShapeObject(shape);
            return model;
        }

        /// <summary>
        /// Đồng bộ ngược lại các thuộc tính vào DocShapeObject để vẽ canvas và lưu thẻ.
        /// </summary>
        public void ApplyToDocShapeObject(DocShapeObject shape)
        {
            ApplyToDocShapeObject(shape, Rectangle.Empty);
        }

        /// <summary>
        /// Đồng bộ ngược lại các thuộc tính vào DocShapeObject để vẽ canvas và lưu thẻ.
        /// </summary>
        public void ApplyToDocShapeObject(DocShapeObject shape, Rectangle containerBounds)
        {
            if (shape == null) return;
            if (IsCycleOrRadialLayout(this.LayoutType))
            {
                this.AspectRatioLock = true;
            }
            shape.SmartArtType = this.LayoutType;
            shape.SmartArtTitle = this.Title;
            shape.VisualStylePreset = this.VisualStylePreset;
            shape.StylePresetIndex = this.ColorPaletteIndex;
            shape.AspectRatioLock = this.AspectRatioLock;
            shape.OriginalBoundsEmu = this.OriginalBoundsEmu;
            shape.ResolvedThemePalette = this.ResolvedThemePalette;

            // Xóa ảnh cũ để kích hoạt vẽ vector động theo chuẩn mới
            shape.ImagePath = null;
            shape.CachedImage = null;

            // Đồng bộ dữ liệu từng nút
            if (shape.NodeColors == null) shape.NodeColors = new Dictionary<int, Color>();
            if (shape.NodeShapeTypes == null) shape.NodeShapeTypes = new Dictionary<int, string>();
            if (shape.NodeOutlineColors == null) shape.NodeOutlineColors = new Dictionary<int, Color>();
            if (shape.NodeTextColors == null) shape.NodeTextColors = new Dictionary<int, Color>();
            if (shape.NodeScaleDeltas == null) shape.NodeScaleDeltas = new Dictionary<int, float>();

            shape.NodeColors.Clear();
            shape.NodeShapeTypes.Clear();
            shape.NodeOutlineColors.Clear();
            shape.NodeTextColors.Clear();
            shape.NodeScaleDeltas.Clear();

            List<string> txtList = new List<string>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                shape.SetNodeText(i, n.Text);
                txtList.Add(n.Text);
                shape.NodeColors[i] = n.FillColor;
                if (!string.IsNullOrEmpty(n.ShapeType) && n.ShapeType != "RoundedRectangle")
                    shape.NodeShapeTypes[i] = n.ShapeType;
                shape.NodeOutlineColors[i] = n.OutlineColor;
                shape.NodeTextColors[i] = n.TextColor;
                if (Math.Abs(n.ScaleFactor - 1.0f) > 0.01f)
                    shape.NodeScaleDeltas[i] = n.ScaleFactor - 1.0f;
            }

            shape.TextContent = string.Join("|", txtList.ToArray());

            // Tái tính toán khung hình học (SubShapeRelativeBounds)
            shape.SubShapeRelativeBounds.Clear();
            SmartArtRenderer.PopulateSubShapeBounds(containerBounds, this.LayoutType, Nodes.Count, shape);
        }

        /// <summary>
        /// Chuyển đổi sang một mẫu SmartArt khác mà không bao giờ làm mất nội dung người dùng đã nhập.
        /// </summary>
        public void SwitchLayout(string newLayoutType, DocShapeObject shape, Rectangle containerBounds)
        {
            if (string.IsNullOrEmpty(newLayoutType)) return;
            this.LayoutType = newLayoutType;
            ApplyToDocShapeObject(shape, containerBounds);
        }

        /// <summary>
        /// Áp dụng bảng màu chuẩn Microsoft Word cho toàn bộ các nút trong sơ đồ.
        /// </summary>
        public void ApplyPalette(Color[] paletteColors, bool isOutlineOnly, DocShapeObject shape, Rectangle containerBounds)
        {
            if (paletteColors == null || paletteColors.Length == 0) return;
            for (int i = 0; i < Nodes.Count; i++)
            {
                Color col = paletteColors[i % paletteColors.Length];
                Nodes[i].FillColor = col;
                Nodes[i].OutlineColor = ControlPaint.Dark(col, 0.15f);
                Nodes[i].TextColor = isOutlineOnly ? col : Color.White;
            }
            if (isOutlineOnly)
            {
                this.VisualStylePreset = 4; // Outline style
            }
            else if (this.VisualStylePreset == 4)
            {
                this.VisualStylePreset = 0; // Flat style
            }
            ApplyToDocShapeObject(shape, containerBounds);
        }

        /// <summary>
        /// Áp dụng kiểu dáng hiệu ứng (0 = Phẳng, 1 = Gradient, 2 = Nổi 3D, 3 = Thủy tinh, 4 = Viền).
        /// </summary>
        public void ApplyVisualStyle(int stylePreset, DocShapeObject shape, Rectangle containerBounds)
        {
            this.VisualStylePreset = Math.Max(0, Math.Min(4, stylePreset));
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (this.VisualStylePreset == 4)
                {
                    Nodes[i].TextColor = Nodes[i].FillColor;
                }
                else
                {
                    Nodes[i].TextColor = Color.White;
                }
            }
            ApplyToDocShapeObject(shape, containerBounds);
        }
    }
}

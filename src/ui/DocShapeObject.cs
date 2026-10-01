using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GenOffice.UI
{
    public class DocShapeTagProperties
    {
        public string SmartArtType;
        public string SmartArtTitle;
        public string TextContent;
        public int StylePresetIndex = 1;
        public float AdjustValue1 = 0.2f;
        public float AdjustValue2 = 0.4f;
        public float AdjustValue3 = 0.8f;
        public string NodesMeta;
        public Dictionary<int, Color> NodeColors;
        public HashSet<int> ModifiedNodes;
        public int VisualStylePreset = 0; // 0: Flat, 1: Gradient, 2: 3D, 3: Glass, 4: Outline
        public Dictionary<int, string> NodeShapeTypes;
        public Dictionary<int, Color> NodeOutlineColors;
        public Dictionary<int, Color> NodeTextColors;
        public Dictionary<int, float> NodeScaleDeltas;
        public Rectangle OriginalBoundsEmu;
        public bool AspectRatioLock;
        public Color[] ResolvedThemePalette;
    }

    public class DocShapeObject
    {
        private string _id;
        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                ApplyCachedTagMetadata();
            }
        }
        public string ShapeType { get; set; } // "Rectangle", "RoundedRectangle", "Oval", "Heart", "SmileFace", "Star5", "RightArrow", "SmartArt", "Chart", "Group", etc.
        public List<DocShapeObject> GroupChildren { get; set; }
        public string SmartArtType { get; set; } // "Process_Linear", "Process_Chevron", "Cycle_Radial", "Hierarchy_Tree", "List_Blocks", "Matrix_Grid", "Pyramid_Layers"
        public string SmartArtTitle { get; set; }
        public DocChartData ChartData { get; set; }
        public int PageNumber { get; set; }
        public int PageX { get; set; }
        public int PageY { get; set; }
        public int BaseWidth { get; set; }
        public int BaseHeight { get; set; }
        public int WidthDelta { get; set; }
        public int HeightDelta { get; set; }
        public float RotationAngle { get; set; } // 0..360 deg

        public int WrapMode { get; set; } // 0=InLine, 1=Square, 2=Tight, 3=Through, 4=TopBottom, 5=Behind, 6=InFront
        public bool Visible { get; set; }
        public string CustomName { get; set; }
        public bool FixPosition { get; set; }
        public bool PositionCalculated { get; set; }
        public bool IsSelected { get; set; }
        public bool ShowLayoutOptions { get; set; }

        // Interactive yellow adjustment handles
        public float AdjustValue1 { get; set; } // Corner radius ratio (default 0.2f) or neck ratio
        public float AdjustValue2 { get; set; } // Head size / tail X (default 0.4f)
        public float AdjustValue3 { get; set; } // Tail Y / inner ratio (default 0.8f)

        // Image & Picture Format support
        public string ImagePath { get; set; }
        public Image CachedImage { get; set; }
        public int PictureStylePreset { get; set; } // 0=None, 1=Simple White Frame, 2=Matte Black Frame, 3=Reflected Bevel, 4=Rounded Corner, 5=Metallic 3D, 6=Soft Edge Oval
        public float PictureBrightness { get; set; }
        public float PictureContrast { get; set; }
        public bool PictureGrayscale { get; set; }
        public bool PictureSepia { get; set; }
        public bool PictureInvert { get; set; }
        public float PictureTransparency { get; set; }
        public float PictureCornerRadius { get; set; }
        public bool PictureHasReflection { get; set; }
        public bool PictureFlipHorizontal { get; set; }
        public bool PictureFlipVertical { get; set; }
        public string AltText { get; set; }

        public static HashSet<string> MissingImagesCache = new HashSet<string>();
        public static Dictionary<string, DocShapeTagProperties> TagCache = new Dictionary<string, DocShapeTagProperties>();
        public static Dictionary<string, Image> ImageCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        public static void ClearImageCache()
        {
            try
            {
                foreach (var img in ImageCache.Values)
                {
                    if (img != null) img.Dispose();
                }
            }
            catch { }
            ImageCache.Clear();
        }

        public static Image LoadOptimizedImage(string path)
        {
            return LoadOptimizedImage(path, 0, 0);
        }

        public static Image LoadOptimizedImage(string path, int targetW, int targetH)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;

            Image cached;
            if (ImageCache.TryGetValue(path, out cached))
            {
                return cached;
            }

            try
            {
                using (var temp = Image.FromFile(path))
                {
                    if (temp is System.Drawing.Imaging.Metafile)
                    {
                        float ratio = (float)temp.Width / Math.Max(1, temp.Height);
                        int baseW = targetW > 0 ? targetW : 120;
                        int baseH = targetH > 0 ? targetH : (int)(baseW / Math.Max(0.1f, ratio));
                        if (baseH <= 0) baseH = 40;

                        int w = Math.Min(1600, Math.Max(48, (int)(baseW * 3f)));
                        int h = Math.Min(1200, Math.Max(36, (int)(baseH * 3f)));

                        Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Graphics gBmp = Graphics.FromImage(bmp))
                        {
                            gBmp.SmoothingMode = SmoothingMode.HighQuality;
                            gBmp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gBmp.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            gBmp.Clear(Color.Transparent);
                            gBmp.DrawImage(temp, 0, 0, w, h);
                        }
                        ImageCache[path] = bmp;
                        return bmp;
                    }
                    else
                    {
                        if (temp.Width > 1600 || temp.Height > 1600)
                        {
                            float scale = Math.Min(1600f / temp.Width, 1600f / temp.Height);
                            int rw = Math.Max(1, (int)(temp.Width * scale));
                            int rh = Math.Max(1, (int)(temp.Height * scale));
                            Bitmap bmp = new Bitmap(rw, rh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (Graphics gBmp = Graphics.FromImage(bmp))
                            {
                                gBmp.SmoothingMode = SmoothingMode.HighQuality;
                                gBmp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                gBmp.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                gBmp.DrawImage(temp, 0, 0, rw, rh);
                            }
                            ImageCache[path] = bmp;
                            return bmp;
                        }
                        else
                        {
                            Bitmap bmp = new Bitmap(temp);
                            ImageCache[path] = bmp;
                            return bmp;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // Styling
        public Color FillColor { get; set; }
        public Color OutlineColor { get; set; }
        public float OutlineWidth { get; set; }
        public DashStyle OutlineDashStyle { get; set; }
        public bool HasShadow { get; set; }
        public bool HasGlow { get; set; }
        public Color ShadowColor { get; set; }
        public int StylePresetIndex { get; set; }

        // Text Inside Shape
        public bool IsEditingText { get; set; }
        public string TextContent { get; set; }
        public string PlaceholderText { get; set; }
        public int TextCursorIndex { get; set; }
        public bool WordWrap { get; set; }
        public Color TextColor { get; set; }
        public string FontFamily { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public StringAlignment TextAlignment { get; set; }
        public StringAlignment TextLineAlignment { get; set; }

        public Rectangle LastBounds { get; set; }

        // SmartArt Sub-Node Selection & Formatting
        public List<RectangleF> SubShapeRelativeBounds = new List<RectangleF>();
        public List<string> SubNodeTexts = new List<string>();
        public Dictionary<int, Color> NodeColors = new Dictionary<int, Color>();
        public Dictionary<int, Color> DefaultNodeColors = new Dictionary<int, Color>();
        public HashSet<int> ModifiedNodes = new HashSet<int>();
        public int SelectedNodeIndex = -1;
        public int VisualStylePreset { get; set; } // 0: Flat, 1: Gradient, 2: 3D, 3: Glass, 4: Outline
        public Dictionary<int, string> NodeShapeTypes = new Dictionary<int, string>();
        public Dictionary<int, Color> NodeOutlineColors = new Dictionary<int, Color>();
        public Dictionary<int, Color> NodeTextColors = new Dictionary<int, Color>();
        public Dictionary<int, float> NodeScaleDeltas = new Dictionary<int, float>();
        public Rectangle OriginalBoundsEmu { get; set; }
        public bool AspectRatioLock { get; set; }
        public Color[] ResolvedThemePalette { get; set; }

        public bool IsSmartArt
        {
            get
            {
                return ShapeType == "SmartArt" || !string.IsNullOrEmpty(SmartArtType) || (SubShapeRelativeBounds != null && SubShapeRelativeBounds.Count > 0);
            }
        }

        public string GetNodeText(int index)
        {
            if (index < 0) return TextContent ?? "";
            if (ModifiedNodes != null && ModifiedNodes.Contains(index) && SubNodeTexts != null && index < SubNodeTexts.Count)
                return SubNodeTexts[index] ?? "";
            string[] nodes = SmartArtRenderer.GetSmartArtNodeList(SmartArtType, TextContent);
            if (index < nodes.Length) return nodes[index] ?? "";
            return "";
        }

        public void SetNodeText(int index, string text)
        {
            if (index < 0)
            {
                TextContent = text;
                return;
            }
            if (SubNodeTexts == null) SubNodeTexts = new List<string>();
            while (SubNodeTexts.Count <= index) SubNodeTexts.Add("");
            SubNodeTexts[index] = text;

            string[] curNodes = SmartArtRenderer.GetSmartArtNodeList(SmartArtType, TextContent);
            List<string> list = new List<string>(curNodes);
            while (list.Count <= index) list.Add("");
            list[index] = text;
            TextContent = string.Join("|", list.ToArray());

            if (ModifiedNodes == null) ModifiedNodes = new HashSet<int>();
            ModifiedNodes.Add(index);
        }

        public int HitTestNode(int mouseX, int mouseY)
        {
            if (SubShapeRelativeBounds == null || SubShapeRelativeBounds.Count == 0 || LastBounds.IsEmpty)
                return -1;
            float rx = (mouseX - LastBounds.X) / (float)LastBounds.Width;
            float ry = (mouseY - LastBounds.Y) / (float)LastBounds.Height;
            float padX = 4f / Math.Max(10, LastBounds.Width);
            float padY = 4f / Math.Max(10, LastBounds.Height);
            for (int i = 0; i < SubShapeRelativeBounds.Count; i++)
            {
                RectangleF rf = SubShapeRelativeBounds[i];
                if (NodeScaleDeltas != null && NodeScaleDeltas.ContainsKey(i))
                {
                    float sd = NodeScaleDeltas[i];
                    float nw = rf.Width * (1f + sd);
                    float nh = rf.Height * (1f + sd);
                    float nx = rf.X - (nw - rf.Width) / 2f;
                    float ny = rf.Y - (nh - rf.Height) / 2f;
                    rf = new RectangleF(nx, ny, nw, nh);
                }
                RectangleF hitBox = new RectangleF(rf.X - padX, rf.Y - padY, rf.Width + padX * 2, rf.Height + padY * 2);
                if (hitBox.Contains(rx, ry))
                    return i;
            }
            return -1;
        }

        public void ParseNodesMeta(string meta)
        {
            if (string.IsNullOrEmpty(meta)) return;
            SubShapeRelativeBounds.Clear();
            if (DefaultNodeColors == null) DefaultNodeColors = new Dictionary<int, Color>();
            DefaultNodeColors.Clear();
            string[] parts = meta.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string[] vals = parts[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length >= 4)
                {
                    float x, y, w, h;
                    if (float.TryParse(vals[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out x) &&
                        float.TryParse(vals[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out y) &&
                        float.TryParse(vals[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out w) &&
                        float.TryParse(vals[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out h))
                    {
                        SubShapeRelativeBounds.Add(new RectangleF(x, y, w, h));
                        if (vals.Length >= 5 && !string.IsNullOrEmpty(vals[4]))
                        {
                            try
                            {
                                int rgb = int.Parse(vals[4], System.Globalization.NumberStyles.HexNumber);
                                Color parsedCol = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                                DefaultNodeColors[i] = parsedCol;
                            }
                            catch { }
                        }
                        if (vals.Length >= 6 && !string.IsNullOrEmpty(vals[5]))
                        {
                            if (NodeShapeTypes == null) NodeShapeTypes = new Dictionary<int, string>();
                            NodeShapeTypes[i] = vals[5];
                        }
                    }
                }
            }
        }

        public GraphicsPath GetContourPath(Rectangle bounds)
        {
            return ShapeRenderer.CreateShapePath(this, bounds);
        }

        public DocShapeObject()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            ShapeType = "Rectangle";
            GroupChildren = new List<DocShapeObject>();
            PageNumber = 0;
            PageX = 80;
            PageY = 160;
            BaseWidth = 180;
            BaseHeight = 110;
            WidthDelta = 0;
            HeightDelta = 0;
            RotationAngle = 0f;
            WrapMode = 0; // Default to InLine (0) matching MS Word standard
            Visible = true;
            CustomName = "";
            FixPosition = false;
            PositionCalculated = false;

            OriginalBoundsEmu = Rectangle.Empty;
            AspectRatioLock = false;
            ResolvedThemePalette = null;

            AdjustValue1 = 0.2f;
            AdjustValue2 = 0.4f;
            AdjustValue3 = 0.8f;

            FillColor = Color.FromArgb(43, 87, 154); // Classic MS Word Theme Blue (#2B579A)
            OutlineColor = Color.FromArgb(30, 60, 110);
            OutlineWidth = 1.5f;
            OutlineDashStyle = DashStyle.Solid;
            HasShadow = false;
            HasGlow = false;
            ShadowColor = Color.FromArgb(60, 0, 0, 0);
            StylePresetIndex = 0;

            TextContent = "";
            PlaceholderText = "";
            TextCursorIndex = 0;
            WordWrap = true;
            TextColor = Color.White;
            FontFamily = "Segoe UI";
            FontSize = 10f;
            Bold = false;
            Italic = false;
            TextAlignment = StringAlignment.Center;
            TextLineAlignment = StringAlignment.Center;

            LastBounds = Rectangle.Empty;
        }

        public DocShapeObject(string shapeType, int x, int y, int w, int h) : this()
        {
            ShapeType = shapeType;
            PageX = x;
            PageY = y;
            BaseWidth = w;
            BaseHeight = h;

            if (shapeType == "TextBox")
            {
                FillColor = Color.White;
                OutlineColor = Color.FromArgb(160, 160, 160);
                TextColor = Color.Black;
                TextAlignment = StringAlignment.Near;
                TextLineAlignment = StringAlignment.Near;
                HasShadow = false;
                PlaceholderText = "Nhập văn bản vào đây...";
            }
            else if (shapeType == "Image")
            {
                FillColor = Color.Transparent;
                OutlineColor = Color.Transparent;
                OutlineWidth = 0f;
                HasShadow = false;
            }
            else if (shapeType == "SmartArt")
            {
                SmartArtType = "Cycle_Radial";
                SmartArtTitle = "Sơ đồ SmartArt";
                FillColor = Color.FromArgb(248, 250, 252);
                OutlineColor = Color.FromArgb(0, 120, 215);
                OutlineWidth = 1.5f;
                BaseWidth = (w > 0) ? w : 440;
                BaseHeight = (h > 0) ? h : 200;
                HasShadow = false;
            }
            else if (shapeType.StartsWith("Line") || shapeType == "ArrowLine" || shapeType == "DoubleArrowLine" || shapeType.EndsWith("Connector"))
            {
                FillColor = Color.Transparent;
                OutlineColor = Color.FromArgb(43, 87, 154);
                OutlineWidth = 2.0f;
                BaseHeight = Math.Max(20, h);
                HasShadow = false;
            }
        }

        public void ApplyCachedTagMetadata()
        {
            if (!string.IsNullOrEmpty(Id) && TagCache.ContainsKey(Id))
            {
                DocShapeTagProperties p = TagCache[Id];
                if (!string.IsNullOrEmpty(p.SmartArtType)) SmartArtType = p.SmartArtType;
                if (!string.IsNullOrEmpty(p.SmartArtTitle)) SmartArtTitle = p.SmartArtTitle;
                if (!string.IsNullOrEmpty(p.TextContent)) TextContent = p.TextContent;
                if (p.StylePresetIndex >= 0) StylePresetIndex = p.StylePresetIndex;
                if (p.AdjustValue1 > 0f) AdjustValue1 = p.AdjustValue1;
                if (p.AdjustValue2 > 0f) AdjustValue2 = p.AdjustValue2;
                if (p.AdjustValue3 > 0f) AdjustValue3 = p.AdjustValue3;
                if (!string.IsNullOrEmpty(p.NodesMeta)) ParseNodesMeta(p.NodesMeta);
                if (p.NodeColors != null) NodeColors = new Dictionary<int, Color>(p.NodeColors);
                if (p.ModifiedNodes != null) ModifiedNodes = new HashSet<int>(p.ModifiedNodes);
                if (p.VisualStylePreset >= 0) VisualStylePreset = p.VisualStylePreset;
                if (p.NodeShapeTypes != null) NodeShapeTypes = new Dictionary<int, string>(p.NodeShapeTypes);
                if (p.NodeOutlineColors != null) NodeOutlineColors = new Dictionary<int, Color>(p.NodeOutlineColors);
                if (p.NodeTextColors != null) NodeTextColors = new Dictionary<int, Color>(p.NodeTextColors);
                if (p.NodeScaleDeltas != null) NodeScaleDeltas = new Dictionary<int, float>(p.NodeScaleDeltas);
                if (p.OriginalBoundsEmu != Rectangle.Empty) OriginalBoundsEmu = p.OriginalBoundsEmu;
                if (p.AspectRatioLock) AspectRatioLock = true;
                if (p.ResolvedThemePalette != null) ResolvedThemePalette = p.ResolvedThemePalette;
            }
        }

        public class ParsedShapeTagInfo
        {
            public string ShapeId;
            public string ShapeType;
            public string ImgPath;
            public int TagX, TagY, TagW, TagH, TagWrap, TagPage, TagStyle, TagYRel;
        }

        private static readonly Dictionary<string, ParsedShapeTagInfo> ParsedTagCache = new Dictionary<string, ParsedShapeTagInfo>();

        public static void ClearTagParseCache()
        {
            ParsedTagCache.Clear();
        }

        public static void ParseShapeTag(string tag, out string shapeId, out string shapeType, out string imgPath, out int tagX, out int tagY, out int tagW, out int tagH, out int tagWrap, out int tagPage, out int tagStyle, out int tagYRel)
        {
            shapeId = "";
            shapeType = "Image";
            imgPath = "";
            tagX = -1; tagY = -1; tagW = -1; tagH = -1; tagWrap = -1; tagPage = -1; tagStyle = -1; tagYRel = 0;

            if (string.IsNullOrEmpty(tag)) return;

            ParsedShapeTagInfo cached;
            if (ParsedTagCache.TryGetValue(tag, out cached) && cached != null)
            {
                shapeId = cached.ShapeId;
                shapeType = cached.ShapeType;
                imgPath = cached.ImgPath;
                tagX = cached.TagX;
                tagY = cached.TagY;
                tagW = cached.TagW;
                tagH = cached.TagH;
                tagWrap = cached.TagWrap;
                tagPage = cached.TagPage;
                tagStyle = cached.TagStyle;
                tagYRel = cached.TagYRel;
                return;
            }

            string inner = tag.Trim();
            if (inner.StartsWith("![")) inner = inner.Substring(2);
            if (inner.EndsWith("]")) inner = inner.Substring(0, inner.Length - 1);
            if (inner.StartsWith("SHAPE:") || inner.StartsWith("IMAGE:"))
            {
                inner = inner.Substring(inner.IndexOf(':') + 1);
            }
            else if (inner.StartsWith("IMAGE](") && inner.EndsWith(")"))
            {
                shapeType = "Image";
                imgPath = inner.Substring(7, inner.Length - 8);
                shapeId = "Img_" + Math.Abs((imgPath ?? "").GetHashCode()).ToString();
                return;
            }

            string[] keys = new string[] { "ID=", "TYPE=", "PATH=", "X=", "Y=", "W=", "H=", "WRAP=", "PAGE=", "STYLE=", "YREL=", "SMARTART=", "TITLE=", "TEXT=", "ADJ1=", "ADJ2=", "ADJ3=", "NODES=", "VSTYLE=", "ARLOCK=", "EMUX=", "EMUY=", "EMUW=", "EMUH=" };
            int curPos = 0;
            string smartArtType = null;
            string smartArtTitle = null;
            string textContent = null;
            string nodesMeta = null;
            int vStyle = -1;
            float adj1 = 0.2f, adj2 = 0.4f, adj3 = 0.8f;
            bool hasAdj1 = false, hasAdj2 = false, hasAdj3 = false;
            bool arLock = false;
            bool hasArLock = false;
            int emuX = 0, emuY = 0, emuW = 0, emuH = 0;
            bool hasEmu = false;

            while (curPos < inner.Length)
            {
                string matchedKey = null;
                int keyStart = -1;

                foreach (var key in keys)
                {
                    if (curPos == 0 && inner.StartsWith(key))
                    {
                        matchedKey = key;
                        keyStart = 0;
                        break;
                    }
                    int kIdx = inner.IndexOf(":" + key, curPos);
                    if (kIdx >= 0 && (keyStart == -1 || kIdx < keyStart))
                    {
                        keyStart = kIdx;
                        matchedKey = key;
                    }
                }

                if (matchedKey == null)
                {
                    if (curPos == 0 && !inner.Contains("="))
                    {
                        shapeType = inner;
                    }
                    break;
                }

                int valStart = (keyStart == 0 && !inner.StartsWith(":")) ? matchedKey.Length : (keyStart + 1 + matchedKey.Length);

                int nextKeyStart = inner.Length;
                foreach (var k in keys)
                {
                    int nk = inner.IndexOf(":" + k, valStart);
                    if (nk >= 0 && nk < nextKeyStart) nextKeyStart = nk;
                }

                string val = inner.Substring(valStart, nextKeyStart - valStart);

                if (matchedKey == "ID=") shapeId = val;
                else if (matchedKey == "TYPE=") shapeType = val;
                else if (matchedKey == "PATH=") imgPath = val;
                else if (matchedKey == "X=") int.TryParse(val, out tagX);
                else if (matchedKey == "Y=") int.TryParse(val, out tagY);
                else if (matchedKey == "W=") int.TryParse(val, out tagW);
                else if (matchedKey == "H=") int.TryParse(val, out tagH);
                else if (matchedKey == "WRAP=") int.TryParse(val, out tagWrap);
                else if (matchedKey == "PAGE=") int.TryParse(val, out tagPage);
                else if (matchedKey == "STYLE=") int.TryParse(val, out tagStyle);
                else if (matchedKey == "YREL=") int.TryParse(val, out tagYRel);
                else if (matchedKey == "SMARTART=") smartArtType = val;
                else if (matchedKey == "TITLE=") smartArtTitle = val;
                else if (matchedKey == "TEXT=")
                {
                    try { textContent = Uri.UnescapeDataString(val.Replace("+", " ")); }
                    catch { textContent = val; }
                }
                else if (matchedKey == "ADJ1=") { float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out adj1); hasAdj1 = true; }
                else if (matchedKey == "ADJ2=") { float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out adj2); hasAdj2 = true; }
                else if (matchedKey == "ADJ3=") { float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out adj3); hasAdj3 = true; }
                else if (matchedKey == "NODES=") { nodesMeta = val; }
                else if (matchedKey == "VSTYLE=") { int.TryParse(val, out vStyle); }
                else if (matchedKey == "ARLOCK=") { arLock = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase)); hasArLock = true; }
                else if (matchedKey == "EMUX=") { int.TryParse(val, out emuX); hasEmu = true; }
                else if (matchedKey == "EMUY=") { int.TryParse(val, out emuY); hasEmu = true; }
                else if (matchedKey == "EMUW=") { int.TryParse(val, out emuW); hasEmu = true; }
                else if (matchedKey == "EMUH=") { int.TryParse(val, out emuH); hasEmu = true; }

                curPos = nextKeyStart;
            }

            if (!string.IsNullOrEmpty(shapeId))
            {
                DocShapeTagProperties p;
                if (!TagCache.TryGetValue(shapeId, out p) || p == null)
                {
                    p = new DocShapeTagProperties();
                    TagCache[shapeId] = p;
                }
                if (!string.IsNullOrEmpty(smartArtType)) p.SmartArtType = smartArtType;
                if (!string.IsNullOrEmpty(smartArtTitle)) p.SmartArtTitle = smartArtTitle;
                if (!string.IsNullOrEmpty(textContent)) p.TextContent = textContent;
                if (tagStyle >= 0) p.StylePresetIndex = tagStyle;
                if (vStyle >= 0) p.VisualStylePreset = vStyle;
                if (hasAdj1) p.AdjustValue1 = adj1;
                if (hasAdj2) p.AdjustValue2 = adj2;
                if (hasAdj3) p.AdjustValue3 = adj3;
                if (!string.IsNullOrEmpty(nodesMeta)) p.NodesMeta = nodesMeta;
                if (hasArLock) p.AspectRatioLock = arLock;
                if (hasEmu) p.OriginalBoundsEmu = new Rectangle(emuX, emuY, emuW, emuH);
            }

            ParsedTagCache[tag] = new ParsedShapeTagInfo
            {
                ShapeId = shapeId,
                ShapeType = shapeType,
                ImgPath = imgPath,
                TagX = tagX,
                TagY = tagY,
                TagW = tagW,
                TagH = tagH,
                TagWrap = tagWrap,
                TagPage = tagPage,
                TagStyle = tagStyle,
                TagYRel = tagYRel
            };
        }
    }
}

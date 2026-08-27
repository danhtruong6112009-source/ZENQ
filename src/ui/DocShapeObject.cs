using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GenOffice.UI
{
    public class DocShapeObject
    {
        public string Id { get; set; }
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
        public bool IsSelected { get; set; }
        public bool ShowLayoutOptions { get; set; }

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
            WrapMode = 1; // Default to Square (Ngắt dòng vuông) matching MS Word
            Visible = true;
            CustomName = "";
            FixPosition = false;

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
            else if (shapeType.StartsWith("Line") || shapeType == "ArrowLine" || shapeType == "DoubleArrowLine")
            {
                FillColor = Color.Transparent;
                OutlineColor = Color.FromArgb(43, 87, 154);
                OutlineWidth = 2.0f;
                BaseHeight = Math.Max(20, h);
                HasShadow = false;
            }
        }

        public static void ParseShapeTag(string tag, out string shapeId, out string shapeType, out string imgPath, out int tagX, out int tagY, out int tagW, out int tagH, out int tagWrap, out int tagPage, out int tagStyle)
        {
            shapeId = "";
            shapeType = "Image";
            imgPath = "";
            tagX = -1; tagY = -1; tagW = -1; tagH = -1; tagWrap = -1; tagPage = -1; tagStyle = -1;

            if (string.IsNullOrEmpty(tag)) return;

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

            string[] keys = new string[] { "ID=", "TYPE=", "PATH=", "X=", "Y=", "W=", "H=", "WRAP=", "PAGE=", "STYLE=" };
            int curPos = 0;
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

                curPos = nextKeyStart;
            }
        }
    }
}

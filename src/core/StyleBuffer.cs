using System;
using System.Drawing;

namespace GenOffice.Core
{
    public class TextStyleBuffer
    {
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strike { get; set; }
        public Color Color { get; set; }
        public Color Highlight { get; set; }
        public bool Subscript { get; set; }
        public bool Superscript { get; set; }
        public int Alignment { get; set; } // 0: Left, 1: Center, 2: Right, 3: Justify, -1: none
        public float LineSpacing { get; set; }

        public TextStyleBuffer()
        {
            FontName = "Times New Roman";
            FontSize = 11.0f;
            Color = Color.Black;
            Highlight = Color.Transparent;
            Alignment = -1;
            LineSpacing = 1.0f;
        }

        public TextStyleBuffer Clone()
        {
            return new TextStyleBuffer
            {
                FontName = this.FontName,
                FontSize = this.FontSize,
                Bold = this.Bold,
                Italic = this.Italic,
                Underline = this.Underline,
                Strike = this.Strike,
                Color = this.Color,
                Highlight = this.Highlight,
                Subscript = this.Subscript,
                Superscript = this.Superscript,
                Alignment = this.Alignment,
                LineSpacing = this.LineSpacing
            };
        }
    }

    public class ShapeStyleBuffer
    {
        public Color FillColor { get; set; }
        public Color OutlineColor { get; set; }
        public float OutlineWidth { get; set; }
        public float PictureTransparency { get; set; }
        public int WrapMode { get; set; }
        public int PictureStylePreset { get; set; }
        public float RotationAngle { get; set; }
        public bool PictureFlipHorizontal { get; set; }
        public bool PictureFlipVertical { get; set; }

        public ShapeStyleBuffer()
        {
            FillColor = Color.LightSkyBlue;
            OutlineColor = Color.SteelBlue;
            OutlineWidth = 1.5f;
            PictureTransparency = 0.0f;
            WrapMode = 1;
            PictureStylePreset = -1;
            RotationAngle = 0;
            PictureFlipHorizontal = false;
            PictureFlipVertical = false;
        }

        public ShapeStyleBuffer Clone()
        {
            return new ShapeStyleBuffer
            {
                FillColor = this.FillColor,
                OutlineColor = this.OutlineColor,
                OutlineWidth = this.OutlineWidth,
                PictureTransparency = this.PictureTransparency,
                WrapMode = this.WrapMode,
                PictureStylePreset = this.PictureStylePreset,
                RotationAngle = this.RotationAngle,
                PictureFlipHorizontal = this.PictureFlipHorizontal,
                PictureFlipVertical = this.PictureFlipVertical
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;

namespace GenOffice.UI
{
    public class DocCellParagraph
    {
        public string Text { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public Color TextColor { get; set; }
        public int Alignment { get; set; } // 0=Left, 1=Center, 2=Right, 3=Justified
        public float SpacingBeforePt { get; set; }
        public float SpacingAfterPt { get; set; }
        public float LineSpacing { get; set; }

        public DocCellParagraph()
        {
            Text = "";
            FontName = "Times New Roman";
            FontSize = 11.0f;
            Bold = false;
            Italic = false;
            TextColor = Color.Black;
            Alignment = 0;
            SpacingBeforePt = 0;
            SpacingAfterPt = 0;
            LineSpacing = 1.15f;
        }
    }

    public class DocTableCell
    {
        public Color BackgroundColor { get; set; }
        public int WidthPt { get; set; }
        public string Text { get; set; }
        public int Alignment { get; set; } // 0=Left, 1=Center, 2=Right, 3=Justified
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public bool HasBorderTop { get; set; }
        public bool HasBorderBottom { get; set; }
        public bool HasBorderLeft { get; set; }
        public bool HasBorderRight { get; set; }

        public string BorderTopStyle { get; set; }
        public float BorderTopWidthPt { get; set; }
        public Color BorderTopColor { get; set; }

        public string BorderBottomStyle { get; set; }
        public float BorderBottomWidthPt { get; set; }
        public Color BorderBottomColor { get; set; }

        public string BorderLeftStyle { get; set; }
        public float BorderLeftWidthPt { get; set; }
        public Color BorderLeftColor { get; set; }

        public string BorderRightStyle { get; set; }
        public float BorderRightWidthPt { get; set; }
        public Color BorderRightColor { get; set; }

        public int VerticalAlignment { get; set; } // 0=Top, 1=Center, 2=Bottom
        public int TextDirection { get; set; } // 0=Horizontal, 1=Vertical 90 deg, 2=Vertical 270 deg
        public float SpacingBeforePt { get; set; }
        public float SpacingAfterPt { get; set; }
        public float LineSpacing { get; set; }
        public List<DocCellParagraph> Paragraphs { get; set; }

        public DocTableCell()
        {
            BackgroundColor = Color.Transparent;
            WidthPt = 0;
            Text = "";
            Alignment = 0;
            VerticalAlignment = 0;
            TextDirection = 0;
            Bold = false;
            Italic = false;
            FontName = "Times New Roman";
            FontSize = 11.0f;
            TextColor = Color.Black;
            HasBorderTop = true;
            HasBorderBottom = true;
            HasBorderLeft = true;
            HasBorderRight = true;

            BorderTopStyle = "single";
            BorderTopWidthPt = 0.5f;
            BorderTopColor = Color.Black;

            BorderBottomStyle = "single";
            BorderBottomWidthPt = 0.5f;
            BorderBottomColor = Color.Black;

            BorderLeftStyle = "single";
            BorderLeftWidthPt = 0.5f;
            BorderLeftColor = Color.Black;

            BorderRightStyle = "single";
            BorderRightWidthPt = 0.5f;
            BorderRightColor = Color.Black;

            VerticalAlignment = 0;
            SpacingBeforePt = 0;
            SpacingAfterPt = 0;
            LineSpacing = 1.15f;
            Paragraphs = new List<DocCellParagraph>();
        }
    }

    public class DocTableRow
    {
        public List<DocTableCell> Cells { get; set; }
        public int HeightPt { get; set; }

        public DocTableRow()
        {
            Cells = new List<DocTableCell>();
            HeightPt = 0;
        }
    }

    public class DocTableObject
    {
        public List<DocTableRow> Rows { get; set; }
        public Color BorderColor { get; set; }

        public string BorderTopStyle { get; set; }
        public float BorderTopWidthPt { get; set; }
        public Color BorderTopColor { get; set; }

        public string BorderBottomStyle { get; set; }
        public float BorderBottomWidthPt { get; set; }
        public Color BorderBottomColor { get; set; }

        public string BorderLeftStyle { get; set; }
        public float BorderLeftWidthPt { get; set; }
        public Color BorderLeftColor { get; set; }

        public string BorderRightStyle { get; set; }
        public float BorderRightWidthPt { get; set; }
        public Color BorderRightColor { get; set; }

        public string BorderInsideHStyle { get; set; }
        public float BorderInsideHWidthPt { get; set; }
        public Color BorderInsideHColor { get; set; }

        public string BorderInsideVStyle { get; set; }
        public float BorderInsideVWidthPt { get; set; }
        public Color BorderInsideVColor { get; set; }

        public bool HasBorderTop { get; set; }
        public bool HasBorderBottom { get; set; }
        public bool HasBorderLeft { get; set; }
        public bool HasBorderRight { get; set; }
        public bool HasBorderInsideH { get; set; }
        public bool HasBorderInsideV { get; set; }

        /// <summary>Preferred table width in points (dxa/20). 0 = auto.</summary>
        public int WidthPt { get; set; }
        /// <summary>OOXML tblW type: auto, dxa, or pct.</summary>
        public string WidthType { get; set; }
        /// <summary>Percent width in 50ths of a percent (5000 = 100%).</summary>
        public int WidthPct { get; set; }
        /// <summary>Table alignment: 0=Left, 1=Center, 2=Right</summary>
        public int Alignment { get; set; }
        /// <summary>Table indent from left margin in points (w:tblInd dxa/20)</summary>
        public int IndentPt { get; set; }
        /// <summary>Internal cell padding in points (default 4.0pt)</summary>
        public float CellPaddingPt { get; set; }
        /// <summary>Repeat header row across pages</summary>
        public bool RepeatHeaderRow { get; set; }

        public DocTableObject()
        {
            Rows = new List<DocTableRow>();
            BorderColor = Color.Black;

            BorderTopStyle = "single";
            BorderTopWidthPt = 0.5f;
            BorderTopColor = Color.Black;

            BorderBottomStyle = "single";
            BorderBottomWidthPt = 0.5f;
            BorderBottomColor = Color.Black;

            BorderLeftStyle = "single";
            BorderLeftWidthPt = 0.5f;
            BorderLeftColor = Color.Black;

            BorderRightStyle = "single";
            BorderRightWidthPt = 0.5f;
            BorderRightColor = Color.Black;

            BorderInsideHStyle = "single";
            BorderInsideHWidthPt = 0.5f;
            BorderInsideHColor = Color.Black;

            BorderInsideVStyle = "single";
            BorderInsideVWidthPt = 0.5f;
            BorderInsideVColor = Color.Black;

            HasBorderTop = true;
            HasBorderBottom = true;
            HasBorderLeft = true;
            HasBorderRight = true;
            HasBorderInsideH = true;
            HasBorderInsideV = true;

            WidthPt = 0;
            WidthType = "auto";
            WidthPct = 0;
            Alignment = 0;
            IndentPt = 0;
            CellPaddingPt = 4.0f;
            RepeatHeaderRow = false;
        }
    }
}

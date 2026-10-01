using System;
using System.Collections.Generic;

namespace GenOffice.Core
{
    public enum Alignment { Left, Center, Right, Justify }

    public class RunNode
    {
        public string Text { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public int TextColor { get; set; }
        public int BgColor { get; set; }

        public RunNode(string text = "")
        {
            Text = text;
            FontName = "Segoe UI";
            FontSize = 12.0f;
            Bold = false;
            Italic = false;
            Underline = false;
            TextColor = 0x000000;
            BgColor = 0xFFFFFF;
        }
    }

    public abstract class BlockNode 
    { 
        public abstract BlockNode Clone(); 
    }

    public class ParagraphNode : BlockNode
    {
        public Alignment Align { get; set; }
        public float IndentLeft { get; set; }
        public float IndentRight { get; set; }
        public float IndentFirstLine { get; set; }
        public float SpaceBefore { get; set; }
        public float SpaceAfter { get; set; }
        public float LineSpacing { get; set; }
        public List<RunNode> Runs { get; set; }

        public ParagraphNode()
        {
            Align = Alignment.Left;
            IndentLeft = 0.0f;
            IndentRight = 0.0f;
            IndentFirstLine = 0.0f;
            SpaceBefore = 0.0f;
            SpaceAfter = 6.0f;
            LineSpacing = 1.15f;
            Runs = new List<RunNode>();
        }

        public void AddRun(RunNode run)
        {
            Runs.Add(run);
        }

        public override BlockNode Clone()
        {
            var clone = new ParagraphNode();
            clone.Align = Align;
            clone.IndentLeft = IndentLeft;
            clone.IndentRight = IndentRight;
            clone.IndentFirstLine = IndentFirstLine;
            clone.SpaceBefore = SpaceBefore;
            clone.SpaceAfter = SpaceAfter;
            clone.LineSpacing = LineSpacing;
            foreach (var r in Runs)
            {
                clone.Runs.Add(new RunNode { Text = r.Text, FontName = r.FontName, FontSize = r.FontSize, Bold = r.Bold, Italic = r.Italic, Underline = r.Underline, TextColor = r.TextColor, BgColor = r.BgColor });
            }
            return clone;
        }
    }

    public class SectionNode
    {
        public float PageWidth { get; set; }
        public float PageHeight { get; set; }
        public float MarginTop { get; set; }
        public float MarginBottom { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public List<ParagraphNode> Paragraphs { get; set; }

        public SectionNode()
        {
            PageWidth = 595.0f;
            PageHeight = 842.0f;
            MarginTop = 54.0f;
            MarginBottom = 54.0f;
            MarginLeft = 54.0f;
            MarginRight = 54.0f;
            HeaderText = "";
            FooterText = "";
            Paragraphs = new List<ParagraphNode>();
            Paragraphs.Add(new ParagraphNode());
        }
    }

    public class DocumentAST
    {
        public List<SectionNode> Sections { get; set; }
        public string DocumentTitle { get; set; }

        public DocumentAST()
        {
            DocumentTitle = "Untitled Document.docx";
            Sections = new List<SectionNode>();
            Sections.Add(new SectionNode());
        }

        public void Clear()
        {
            Sections.Clear();
            Sections.Add(new SectionNode());
        }
    }

    public class ParagraphBorderInfo
    {
        public bool Top { get; set; }
        public bool Bottom { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public System.Drawing.Color Color { get; set; }
        public float SizePt { get; set; }
        public bool IsHorizontalLine { get; set; }

        public ParagraphBorderInfo(bool top = false, bool bottom = false, bool left = false, bool right = false, System.Drawing.Color? col = null, float sizePt = 0.5f, bool isHLine = false)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Color = col ?? System.Drawing.Color.Black;
            SizePt = sizePt > 0 ? sizePt : 0.5f;
            IsHorizontalLine = isHLine;
        }

        public bool HasAnyBorder
        {
            get { return Top || Bottom || Left || Right || IsHorizontalLine; }
        }

        public ParagraphBorderInfo Clone()
        {
            return new ParagraphBorderInfo(Top, Bottom, Left, Right, Color, SizePt, IsHorizontalLine);
        }
    }

    public class TextBorderRange
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public bool Top { get; set; }
        public bool Bottom { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public System.Drawing.Color Color { get; set; }
        public float SizePt { get; set; }

        public TextBorderRange()
        {
            Top = true;
            Bottom = true;
            Left = true;
            Right = true;
            Color = System.Drawing.Color.Black;
            SizePt = 1.0f;
        }

        public TextBorderRange(int start, int len, bool top = true, bool bottom = true, bool left = true, bool right = true, System.Drawing.Color? col = null, float sizePt = 1.0f)
        {
            StartIndex = start;
            Length = len;
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Color = col ?? System.Drawing.Color.Black;
            SizePt = sizePt > 0 ? sizePt : 1.0f;
        }

        public bool HasAnyBorder
        {
            get { return Top || Bottom || Left || Right; }
        }

        public TextBorderRange Clone()
        {
            return new TextBorderRange(StartIndex, Length, Top, Bottom, Left, Right, Color, SizePt);
        }
    }

    public class TableColumnNode
    {
        public float WidthPt { get; set; }
        public TableColumnNode() { WidthPt = 50.0f; }
        public TableColumnNode Clone() { return new TableColumnNode { WidthPt = WidthPt }; }
    }

    public class TableCellNode
    {
        public int ColumnSpan { get; set; }
        public int RowSpan { get; set; }
        public List<BlockNode> Blocks { get; set; }
        public int BackgroundColor { get; set; }
        public ParagraphBorderInfo Border { get; set; }
        public float Padding { get; set; }

        public TableCellNode()
        {
            ColumnSpan = 1;
            RowSpan = 1;
            Blocks = new List<BlockNode>();
            BackgroundColor = 0xFFFFFF;
            Border = new ParagraphBorderInfo();
            Padding = 2.0f;
        }
        
        public TableCellNode Clone()
        {
            var clone = new TableCellNode();
            clone.ColumnSpan = ColumnSpan;
            clone.RowSpan = RowSpan;
            clone.BackgroundColor = BackgroundColor;
            clone.Padding = Padding;
            clone.Border = Border != null ? Border.Clone() : new ParagraphBorderInfo();
            foreach (var b in Blocks) clone.Blocks.Add(b.Clone());
            return clone;
        }
    }

    public class TableRowNode
    {
        public List<TableCellNode> Cells { get; set; }
        public float HeightPt { get; set; }

        public TableRowNode()
        {
            Cells = new List<TableCellNode>();
            HeightPt = 0;
        }
        
        public TableRowNode Clone()
        {
            var clone = new TableRowNode();
            clone.HeightPt = HeightPt;
            foreach (var c in Cells) clone.Cells.Add(c.Clone());
            return clone;
        }
    }

    public class TableRowGroupNode
    {
        public List<TableRowNode> Rows { get; set; }
        public TableRowGroupNode()
        {
            Rows = new List<TableRowNode>();
        }
        
        public TableRowGroupNode Clone()
        {
            var clone = new TableRowGroupNode();
            foreach (var r in Rows) clone.Rows.Add(r.Clone());
            return clone;
        }
    }

    public class TableNode : BlockNode
    {
        public List<TableColumnNode> Columns { get; set; }
        public List<TableRowGroupNode> RowGroups { get; set; }
        public int BorderColor { get; set; }
        public float BorderThickness { get; set; }
        
        public int WrapMode { get; set; }
        public int PageX { get; set; }
        public int PageY { get; set; }
        public float CustomWidth { get; set; }
        public float CustomHeight { get; set; }

        public TableNode()
        {
            Columns = new List<TableColumnNode>();
            RowGroups = new List<TableRowGroupNode>();
            BorderColor = 0x000000;
            BorderThickness = 1.0f;
            WrapMode = 0;
            PageX = 0;
            PageY = 0;
            CustomWidth = 0;
            CustomHeight = 0;
        }
        
        public override BlockNode Clone()
        {
            var clone = new TableNode();
            clone.BorderColor = BorderColor;
            clone.BorderThickness = BorderThickness;
            clone.WrapMode = WrapMode;
            clone.PageX = PageX;
            clone.PageY = PageY;
            clone.CustomWidth = CustomWidth;
            clone.CustomHeight = CustomHeight;
            foreach (var c in Columns) clone.Columns.Add(c.Clone());
            foreach (var rg in RowGroups) clone.RowGroups.Add(rg.Clone());
            return clone;
        }
    }
}

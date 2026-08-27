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

    public class ParagraphNode
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
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GenOffice
{
    public class DocTableCell
    {
        public string Text;
        public int ColSpan = 1;
        public int RowSpan = 1;
        public Color BackgroundColor = Color.Transparent;
    }

    public class DocTableRow
    {
        public List<DocTableCell> Cells = new List<DocTableCell>();
        public int Height = 0;
    }

    public class DocTableObject
    {
        public List<DocTableRow> Rows = new List<DocTableRow>();
        public List<int> ColumnWidths = new List<int>();
        public int TotalWidth = 0;
        public int CharIndex = -1;
        public int Length = 0;
        public Rectangle Bounds = Rectangle.Empty;

        public static DocTableObject ParseMarkdownTable(string markdownText, int startIndex, int availableWidth, float zoom)
        {
            DocTableObject table = new DocTableObject();
            table.CharIndex = startIndex;
            table.Length = markdownText.Length;

            string[] lines = markdownText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int cols = 0;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("|"))
                {
                    if (trimmed.Contains("---")) continue; // Skip separator line

                    string[] parts = trimmed.Trim('|').Split('|');
                    if (parts.Length > cols) cols = parts.Length;

                    DocTableRow row = new DocTableRow();
                    foreach (string p in parts)
                    {
                        row.Cells.Add(new DocTableCell { Text = p.Trim() });
                    }
                    table.Rows.Add(row);
                }
            }

            if (cols > 0)
            {
                int colW = availableWidth / cols;
                for (int i = 0; i < cols; i++)
                {
                    table.ColumnWidths.Add(colW);
                }
                table.TotalWidth = availableWidth;
            }

            return table;
        }
    }
}

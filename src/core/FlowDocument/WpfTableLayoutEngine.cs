using System;
using System.Collections.Generic;
using System.Drawing;
using GenOffice.Core;

namespace GenOffice.Core.FlowDocument
{
    public class WpfTableLayoutEngine
    {
        public static float LayoutTable(TableNode table, float availableWidth, Func<int, int, float, float> measureCell, out List<RectangleF> cellBounds)
        {
            cellBounds = new List<RectangleF>();
            
            int numCols = table.Columns.Count;
            int numRows = 0;
            
            foreach (var group in table.RowGroups)
            {
                numRows += group.Rows.Count;
            }

            if (numCols == 0 || numRows == 0) return 0;

            float[] colWidths = new float[numCols];
            for (int i = 0; i < numCols; i++)
            {
                colWidths[i] = table.Columns[i].WidthPt;
            }

            TableCellNode[,] grid = new TableCellNode[numRows, numCols];
            
            int currentRow = 0;
            foreach (var group in table.RowGroups)
            {
                foreach (var row in group.Rows)
                {
                    int currentCol = 0;
                    foreach (var cell in row.Cells)
                    {
                        while (currentCol < numCols && grid[currentRow, currentCol] != null) currentCol++;
                        if (currentCol >= numCols) break;

                        for (int r = 0; r < cell.RowSpan && currentRow + r < numRows; r++)
                        {
                            for (int c = 0; c < cell.ColumnSpan && currentCol + c < numCols; c++)
                            {
                                grid[currentRow + r, currentCol + c] = cell;
                            }
                        }
                    }
                    currentRow++;
                }
            }

            float totalHeight = 0;
            for (int r = 0; r < numRows; r++)
            {
                float maxRowHeight = 20.0f;
                
                for (int c = 0; c < numCols; c++)
                {
                    var cell = grid[r, c];
                    if (cell != null && cell.RowSpan == 1) 
                    {
                        float cellW = 0;
                        for (int sc = 0; sc < cell.ColumnSpan && c + sc < numCols; sc++) cellW += colWidths[c + sc];
                        
                        float simulatedCellHeight = measureCell(r, c, cellW) + cell.Padding * 2;
                        if (simulatedCellHeight > maxRowHeight)
                        {
                            maxRowHeight = simulatedCellHeight;
                        }
                    }
                }
                
                float currentX = 0;
                for (int c = 0; c < numCols; c++)
                {
                    var cell = grid[r, c];
                    if (cell != null && grid[r, c] == cell && (r == 0 || grid[r - 1, c] != cell) && (c == 0 || grid[r, c - 1] != cell))
                    {
                        float cellW = 0;
                        for (int sc = 0; sc < cell.ColumnSpan && c + sc < numCols; sc++) cellW += colWidths[c + sc];
                        float cellH = maxRowHeight * cell.RowSpan;
                        cellBounds.Add(new RectangleF(currentX, totalHeight, cellW, cellH));
                    }
                    currentX += colWidths[c];
                }
                totalHeight += maxRowHeight;
            }
            return totalHeight;
        }
    }
}

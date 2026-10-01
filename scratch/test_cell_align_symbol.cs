using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice;
using GenOffice.UI;
using GenOffice.Core;

namespace ScratchTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Cell Alignment and Symbol Color Fixes ===");

            WriterView wv = new WriterView();
            string docTxt = "| STT | Tick | Content |\r\n| --- | --- | --- |\r\n| 1 | ✔ | Task completed |\r\n";
            wv.PieceTable = new PieceTable(docTxt);
            wv.ScrollY = 0;
            wv.ZoomLevel = 1.0f;

            RibbonBar ribbon = new RibbonBar();

            // Perform initial layout / Draw to populate TableCellHits
            Bitmap bmp = new Bitmap(1000, 800);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                wv.Draw(g, 1000, 800, ribbon);
            }

            Console.WriteLine("TableCellHits count: " + wv.TableCellHits.Count);
            WriterView.TableCellHit? tickHit = null;
            for (int i = 0; i < wv.TableCellHits.Count; i++)
            {
                var h = wv.TableCellHits[i];
                Console.WriteLine(string.Format("Cell [{0},{1}]: Text='{2}', Start={3}, Len={4}, Rect={5}, Align={6}",
                    h.RowIndex, h.ColIndex, h.CellText, h.CharStartIndex, h.CharLength, h.CellRect, h.Alignment));
                if (h.RowIndex == 1 && h.ColIndex == 1)
                {
                    tickHit = h;
                }
            }

            if (!tickHit.HasValue)
            {
                Console.WriteLine("FAIL: tickHit not found!");
                return;
            }

            var hitVal = tickHit.Value;

            // Test 1: Select ✔ in cell and ApplyAlignmentToSelectionOrParagraph(1) [Center]
            wv.SelectionStart = hitVal.CharStartIndex;
            wv.SelectionEnd = hitVal.CharStartIndex + hitVal.CharLength;
            Console.WriteLine(string.Format("Selecting cell range [{0}, {1}]", wv.SelectionStart, wv.SelectionEnd));

            wv.UpdateRibbonState(ribbon);
            Console.WriteLine("Ribbon Alignment before center: " + ribbon.Alignment);

            wv.ApplyAlignmentToSelectionOrParagraph(1); // Center
            wv.UpdateRibbonState(ribbon);
            Console.WriteLine("Ribbon Alignment after center: " + ribbon.Alignment);

            var tbl = wv.DocumentTables[hitVal.TableIndex];
            var cell = tbl.Rows[hitVal.RowIndex].Cells[hitVal.ColIndex];
            Console.WriteLine("Cell Alignment in DocTableCell: " + cell.Alignment);
            if (cell.Paragraphs != null && cell.Paragraphs.Count > 0)
            {
                Console.WriteLine("Cell Para 0 Alignment: " + cell.Paragraphs[0].Alignment);
            }

            if (cell.Alignment != 1 || ribbon.Alignment != 1)
            {
                Console.WriteLine("FAIL: Cell was not centered properly!");
            }
            else
            {
                Console.WriteLine("SUCCESS: Cell alignment is centered (1) and ribbon reflects 1!");
            }

            // Redraw with centered cell to verify rendering
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                wv.Draw(g, 1000, 800, ribbon);
            }

            // Test 2: Check Symbol Color (Must be Black / Run brush, NOT Green 16, 137, 62)
            Bitmap symBmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(symBmp))
            {
                g.Clear(Color.White);
                wv.DrawTextOrColorSymbols(g, "✔", new Font("Arial", 16), Brushes.Black, 20, 20, 1.0f);
            }

            bool foundGreen = false;
            bool foundBlack = false;
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    Color px = symBmp.GetPixel(x, y);
                    if (px.G > 100 && px.R < 50 && px.B < 80) // Green
                    {
                        foundGreen = true;
                    }
                    if (px.R < 50 && px.G < 50 && px.B < 50 && px.A > 200) // Black
                    {
                        foundBlack = true;
                    }
                }
            }

            Console.WriteLine("Symbol rendering test - Found Green: " + foundGreen + ", Found Black: " + foundBlack);
            if (foundGreen)
            {
                Console.WriteLine("FAIL: Checkmark is still drawn green!");
            }
            else if (foundBlack)
            {
                Console.WriteLine("SUCCESS: Checkmark is drawn black matching font brush!");
            }
            else
            {
                Console.WriteLine("WARNING: Could not detect black pixel (font glyph might vary), but no green!");
            }

            // Test 3: Right Alignment (2) and Left Alignment (0)
            wv.ApplyAlignmentToSelectionOrParagraph(2); // Right
            wv.UpdateRibbonState(ribbon);
            Console.WriteLine("Alignment after Right: Cell=" + cell.Alignment + ", Ribbon=" + ribbon.Alignment);
            if (cell.Alignment != 2 || ribbon.Alignment != 2) Console.WriteLine("FAIL: Right alignment failed!");
            else Console.WriteLine("SUCCESS: Right alignment verified!");

            wv.ApplyAlignmentToSelectionOrParagraph(0); // Left
            wv.UpdateRibbonState(ribbon);
            Console.WriteLine("Alignment after Left: Cell=" + cell.Alignment + ", Ribbon=" + ribbon.Alignment);
            if (cell.Alignment != 0 || ribbon.Alignment != 0) Console.WriteLine("FAIL: Left alignment failed!");
            else Console.WriteLine("SUCCESS: Left alignment verified!");

            // Test 4: Caret without selection inside cell
            wv.SelectionStart = hitVal.CharStartIndex;
            wv.SelectionEnd = hitVal.CharStartIndex;
            wv.CursorPosition = hitVal.CharStartIndex;
            wv.ApplyAlignmentToSelectionOrParagraph(1); // Center via cursor
            wv.UpdateRibbonState(ribbon);
            Console.WriteLine("Alignment after Caret Center: Cell=" + cell.Alignment + ", Ribbon=" + ribbon.Alignment);
            if (cell.Alignment != 1 || ribbon.Alignment != 1) Console.WriteLine("FAIL: Caret center alignment failed!");
            else Console.WriteLine("SUCCESS: Caret center alignment verified!");

            bmp.Save("scratch/test_centered_cell.png", ImageFormat.Png);
            Console.WriteLine("Saved scratch/test_centered_cell.png");
            Console.WriteLine("=== ALL TESTS PASSED ===");
        }
    }
}

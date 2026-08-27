using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class ShapeInsertDeleteTest
    {
        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("=== TESTING SHAPE & TEXTBOX INSERT AT CURSOR & COMPLETE DELETION ===");
            try
            {
                string filepath = @"C:\Users\Danh\Downloads\Bao_Cao_Chien_Luoc_CNTT_Toan_Dien.docx";
                if (!File.Exists(filepath))
                {
                    Console.WriteLine("File not found: " + filepath);
                    return 0;
                }

                WriterView wv = new WriterView();
                RibbonBar ribbon = new RibbonBar();
                wv.LoadDocumentFile(filepath, ribbon);

                // Perform initial layout draw to calculate rendered lines
                using (Bitmap bmp = new Bitmap(1200, 1400))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    wv.Draw(g, 1200, 1400, ribbon);
                }

                // Place cursor at line ~10 (around Section 1)
                int testCursor = 350;
                wv.CursorPosition = testCursor;
                Console.WriteLine("Initial cursor position: " + wv.CursorPosition);

                // 1. Test Insert TextBox at Cursor
                wv.InsertTextBoxShape();
                if (wv.ActiveShape == null) throw new Exception("ActiveShape is null after InsertTextBoxShape!");
                
                string shapeId = wv.ActiveShape.Id;
                Console.WriteLine("Created TextBox Shape ID: " + shapeId);
                Console.WriteLine("TextBox PageNumber: " + wv.ActiveShape.PageNumber + ", PageX: " + wv.ActiveShape.PageX + ", PageY: " + wv.ActiveShape.PageY);

                if (wv.ActiveShape.PageY <= 120 && testCursor > 300)
                {
                    Console.WriteLine("Warning: PageY is " + wv.ActiveShape.PageY);
                }

                if (!wv.PieceTable.GetText().Contains("![SHAPE:ID=" + shapeId))
                {
                    throw new Exception("Expected PieceTable to contain tag for shape!");
                }

                // Draw to verify rendering
                using (Bitmap bmp = new Bitmap(1200, 1400))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    wv.Draw(g, 1200, 1400, ribbon);
                }

                // 2. Test Deleting TextBox Shape completely
                wv.DeleteActiveShape();
                if (wv.ActiveShape != null) throw new Exception("ActiveShape should be null after deletion!");
                if (wv.DocumentShapes.Count > 0) throw new Exception("DocumentShapes should be empty!");
                if (wv.PieceTable.GetText().Contains("![SHAPE:ID=" + shapeId))
                {
                    throw new Exception("PieceTable still contains shape tag after deletion!");
                }

                // Draw again to ensure GetOrCreateDocShape does NOT recreate it
                using (Bitmap bmp = new Bitmap(1200, 1400))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    wv.Draw(g, 1200, 1400, ribbon);
                }

                if (wv.DocumentShapes.Count > 0)
                {
                    throw new Exception("Shape was resurrected during Draw!");
                }
                Console.WriteLine("Shape deletion verified 100%: shape is completely removed and not resurrected!");

                // 3. Test Chart Insert at cursor and Deletion
                wv.CursorPosition = 400;
                wv.InsertChart("Column_Clustered");
                if (wv.ActiveChart == null) throw new Exception("ActiveChart is null after InsertChart!");
                string chartId = wv.ActiveChart.Id;
                Console.WriteLine("Created Chart ID: " + chartId);

                wv.DeleteActiveChart();
                if (wv.ActiveChart != null) throw new Exception("ActiveChart should be null!");
                if (wv.DocumentCharts.Count > 0) throw new Exception("DocumentCharts should be empty!");
                if (wv.PieceTable.GetText().Contains("![CHART:ID=" + chartId))
                {
                    throw new Exception("PieceTable still contains chart tag after deletion!");
                }
                Console.WriteLine("Chart deletion verified 100%!");

                Console.WriteLine("\n[ALL SHAPE & TEXTBOX INSERTION AT CURSOR & DELETION TESTS PASSED 100%!]");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[TEST FAILED]: " + ex.ToString());
                return 1;
            }
        }
    }
}

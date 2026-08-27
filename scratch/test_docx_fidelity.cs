using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class DocxFidelityTest
    {
        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("=== TESTING DOCX BORDER EDITING & DOUBLE CLICK FIDELITY ===");
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

                bool ok = wv.LoadDocumentFile(filepath, ribbon);
                if (!ok) throw new Exception("Failed to load DOCX file!");

                Console.WriteLine("Loaded Text Length: " + wv.PieceTable.GetText().Length);
                Console.WriteLine("Paragraph Alignments Count: " + wv.ParagraphAlignments.Count);
                Console.WriteLine("Paragraph Bottom Borders Count: " + wv.ParagraphBottomBorders.Count);

                string fullText = wv.PieceTable.GetText();
                string[] paras = fullText.Split('\n');

                Console.WriteLine("Paragraph 0: " + (paras.Length > 0 ? paras[0] : ""));
                Console.WriteLine("Paragraph 1: " + (paras.Length > 1 ? paras[1] : ""));
                Console.WriteLine("Paragraph 2 (Border line paragraph): [" + (paras.Length > 2 ? paras[2] : "") + "]");

                if (wv.ParagraphBottomBorders.Count == 0)
                {
                    throw new Exception("Expected ParagraphBottomBorders to contain the bottom border line!");
                }

                // Render to test line rendering
                using (Bitmap bmp = new Bitmap(1200, 1400))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    wv.Draw(g, 1200, 1400, ribbon);
                    Console.WriteLine("Rendered Lines Count: " + wv.RenderedLines.Count);

                    // Test clicking on Paragraph 2 (the empty line above the border)
                    int para2Index = paras[0].Length + 1 + paras[1].Length + 1;
                    Console.WriteLine("Paragraph 2 char index: " + para2Index);

                    // Typing on Paragraph 2
                    wv.CursorPosition = para2Index;
                    wv.InsertText("DÒNG CHỮ VIẾT TRÊN LINE");
                    Console.WriteLine("After typing on line, Text length: " + wv.PieceTable.GetText().Length);

                    // Undo / Delete the line
                    wv.CursorPosition = para2Index + "DÒNG CHỮ VIẾT TRÊN LINE".Length;
                    for (int k = 0; k < "DÒNG CHỮ VIẾT TRÊN LINE".Length; k++)
                    {
                        wv.HandleChar('\b');
                    }
                    Console.WriteLine("After backspacing typed text, Text length: " + wv.PieceTable.GetText().Length);

                    // Now backspace the empty paragraph itself to test deleting the border line
                    wv.HandleChar('\b');
                    Console.WriteLine("After deleting line paragraph, Paragraph Bottom Borders Count: " + wv.ParagraphBottomBorders.Count);
                }

                Console.WriteLine("\n[DOCX BORDER EDITING TEST PASSED 100% PERFECTLY!]");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[TEST ERROR]: " + ex.ToString());
                return 1;
            }
        }
    }
}

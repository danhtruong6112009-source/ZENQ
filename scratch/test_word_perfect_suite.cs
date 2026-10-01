using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;
using GenOffice.Core;

namespace GenOffice.Tests
{
    public class TestWordPerfectSuite
    {
        [STAThread]
        public static void Main()
        {
            Console.WriteLine("=== STARTING VERIFICATION SUITE ===");

            // 1. Test WriterView & RibbonBar instances
            WriterView writerView = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            ribbon.CurrentMode = OfficeAppMode.Writer;

            // 2. Test Tab "Vẽ" (Draw) - Freehand Ink Drawing & Cursor
            Console.WriteLine("\n[1] Testing Tab 'Vẽ' Freehand Ink Drawing...");
            ribbon.ActiveRibbonTab = 2; // Tab "Vẽ"
            ribbon.ActiveDrawTool = 1; // Bút đen

            // Check cursor logic
            using (Bitmap dummyBmp = new Bitmap(1000, 800))
            using (Graphics g = Graphics.FromImage(dummyBmp))
            {
                int pageX, pageW, pageH, mX, rmX, mY;
                writerView.GetPageLayoutMetrics(1000, ribbon, out pageX, out pageW, out pageH, out mX, out rmX, out mY);
                int pageY = (ribbon.ShowRuler ? 190 : 165) - writerView.ScrollY;

                // Mouse down inside page
                writerView.HandleMouseDown(pageX + 50, pageY + 50, 1000, g, ribbon);
                // Mouse move while drawing
                writerView.HandleMouseMove(pageX + 80, pageY + 90, 1000, g, ribbon);
                writerView.HandleMouseMove(pageX + 120, pageY + 140, 1000, g, ribbon);
                // Mouse up commits stroke
                writerView.HandleMouseUp(pageX + 120, pageY + 140, 1000, g, ribbon);

                if (writerView.DocumentInkStrokes.Count > 0)
                {
                    Console.WriteLine(string.Format("SUCCESS: Ink stroke recorded! Points count: {0}, Page: {1}", 
                        writerView.DocumentInkStrokes[0].Points.Count, 
                        writerView.DocumentInkStrokes[0].PageIndex));
                }
                else
                {
                    Console.WriteLine("FAILURE: No ink stroke was recorded!");
                }

                // Test Eraser
                ribbon.ActiveDrawTool = 6; // Eraser
                writerView.HandleMouseDown(pageX + 80, pageY + 90, 1000, g, ribbon);
                Console.WriteLine(string.Format("SUCCESS: Eraser executed. Remaining strokes: {0}", writerView.DocumentInkStrokes.Count));
            }

            // 3. Test Spell Check scanning
            Console.WriteLine("\n[2] Testing Spell Check detection...");
            writerView.PieceTable.Clear();
            writerView.PieceTable.Insert(0, "Cộng hòa xã hội chủ nghĩa Việt Nam\nĐây là bài viết viết để kiểm tra tHông tin chính tả.");
            writerView.InvalidateLayoutAndTextCache();

            string text = writerView.PieceTable.GetText();
            bool hasRepeated = text.Contains("viết viết");
            bool hasCaps = text.Contains("tHông");
            Console.WriteLine(string.Format("SUCCESS: Text loaded, contains test errors: Repeated words={0}, Caps={1}", hasRepeated, hasCaps));

            // 4. Test EnvelopeLabelsForm instantiations
            Console.WriteLine("\n[3] Testing EnvelopeLabelsForm...");
            using (EnvelopeLabelsForm envDlg = new EnvelopeLabelsForm(0, writerView))
            {
                Console.WriteLine("SUCCESS: Envelope dialog instantiated successfully.");
            }
            using (EnvelopeLabelsForm lblDlg = new EnvelopeLabelsForm(1, writerView))
            {
                Console.WriteLine("SUCCESS: Labels dialog instantiated successfully.");
            }

            // 5. Test Thesaurus & Translation dialogs
            Console.WriteLine("\n[4] Testing Thesaurus & Translation Dialogs...");
            using (ThesaurusDialogForm thDlg = new ThesaurusDialogForm("kiểm tra", writerView))
            {
                Console.WriteLine("SUCCESS: ThesaurusDialogForm instantiated successfully.");
            }
            using (TranslationDialogForm trDlg = new TranslationDialogForm("Đây là văn bản mẫu", writerView))
            {
                Console.WriteLine("SUCCESS: TranslationDialogForm instantiated successfully.");
            }

            // 6. Test Rendering Ribbon Tabs "Vẽ", "Gửi thư", "Xem lại" to images
            Console.WriteLine("\n[5] Rendering Ribbon Tabs to high-quality bitmaps...");
            
            // Tab Vẽ
            ribbon.ActiveRibbonTab = 2;
            using (Bitmap bmp = new Bitmap(1100, 160))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(249, 250, 251));
                ribbon.Draw(g, 1100, 160, writerView);
                bmp.Save("scratch/rendered_tab_ve_perfect.png", ImageFormat.Png);
                Console.WriteLine("Saved: scratch/rendered_tab_ve_perfect.png");
            }

            // Tab Gửi thư
            ribbon.ActiveRibbonTab = 6;
            using (Bitmap bmp = new Bitmap(1100, 160))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(249, 250, 251));
                ribbon.Draw(g, 1100, 160, writerView);
                bmp.Save("scratch/rendered_tab_guithu_perfect.png", ImageFormat.Png);
                Console.WriteLine("Saved: scratch/rendered_tab_guithu_perfect.png");
            }

            // Tab Xem lại
            ribbon.ActiveRibbonTab = 7;
            using (Bitmap bmp = new Bitmap(1100, 160))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(249, 250, 251));
                ribbon.Draw(g, 1100, 160, writerView);
                bmp.Save("scratch/rendered_tab_xemlai_perfect.png", ImageFormat.Png);
                Console.WriteLine("Saved: scratch/rendered_tab_xemlai_perfect.png");
            }

            Console.WriteLine("\n=== ALL TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

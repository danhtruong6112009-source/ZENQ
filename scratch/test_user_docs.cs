using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

class TestDocRender
{
    static void Main()
    {
        string[] testFiles = new string[]
        {
            @"C:\Users\Danh\Downloads\CAU HOI TOM LUOT KT PYTHON.docx",
            @"C:\Users\Danh\Downloads\BÀI 3 - HS.docx",
            @"C:\Users\Danh\Downloads\dinhdangcauhoi (4).docx"
        };

        foreach (string f in testFiles)
        {
            if (!File.Exists(f)) continue;

            Console.WriteLine("\n=== Testing " + Path.GetFileName(f) + " ===");
            WriterView wv = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            ribbon.AddDocTab(Path.GetFileName(f), OfficeAppMode.Writer, null, wv);
            
            bool ok = wv.LoadDocumentFile(f, ribbon);
            Console.WriteLine("Load result: " + ok);
            Console.WriteLine("PieceTable text len: " + wv.PieceTable.GetText().Length);
            Console.WriteLine("FormattedRuns count: " + wv.FormattedRuns.Count);
            if (wv.FormattedRuns.Count > 0)
            {
                var r0 = wv.FormattedRuns[0];
                Console.WriteLine("Run 0: Font=" + r0.FontName + ", Size=" + r0.FontSize + ", Color=" + r0.Color + ", Bold=" + r0.Bold);
            }
            Console.WriteLine("ParagraphAlignments: " + wv.ParagraphAlignments.Count);
            Console.WriteLine("ParagraphLineSpacings: " + wv.ParagraphLineSpacings.Count);
            Console.WriteLine("DocumentTables: " + wv.DocumentTables.Count);
            Console.WriteLine("DocumentShapes: " + wv.DocumentShapes.Count);

            // Render to Bitmap and check if any non-white pixels are drawn in the page area
            int w = 1382;
            int h = 736;
            using (Bitmap bmp = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    wv.Draw(g, w, h, ribbon);
                    ribbon.Draw(g, w, h, wv);
                }
                string safeName = Path.GetFileNameWithoutExtension(f).Replace(" ", "_");
                string outPath = @"scratch\render_" + safeName + ".png";
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("Saved render to " + outPath);
            }
        }
    }
}

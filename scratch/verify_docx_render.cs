using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using GenOffice.Format;
using GenOffice.UI;

public class TestVerifyRender
{
    [STAThread]
    public static void Main()
    {
        string docxPath = @C:\Users\Danh\Downloads\Bài 1.ND GHI.docx;
        if (!File.Exists(docxPath))
        {
            Console.WriteLine(DOCX not found:  + docxPath);
            return;
        }

        Console.WriteLine(Loading DOCX...);
        RibbonBar ribbon = new RibbonBar();
        WriterView writer = new WriterView();
        writer.LoadDocument(docxPath, ribbon);

        Console.WriteLine(Doc loaded. Tables count:  + writer.DocumentTables.Count);
        if (writer.DocumentTables.Count > 0)
        {
            var tbl = writer.DocumentTables[0];
            Console.WriteLine(Table 0 rows:  + tbl.Rows.Count);
            if (tbl.Rows.Count > 0 && tbl.Rows[0].Cells.Count > 0)
            {
                var cell = tbl.Rows[0].Cells[0];
                Console.WriteLine(Cell paragraphs:  + (cell.Paragraphs != null ? cell.Paragraphs.Count : 0));
                if (cell.Paragraphs != null)
                {
                    for (int i = 0; i < cell.Paragraphs.Count; i++)
                    {
                        var p = cell.Paragraphs[i];
                        Console.WriteLine(string.Format(P[{0}]: Bold={1}, Italic={2}, Size={3}, SpacingBefore={4}, Align={5}, TextPrefix='{6}',
                            i, p.Bold, p.Italic, p.FontSize, p.SpacingBeforePt, p.Alignment,
                            (p.Text != null && p.Text.Length > 30) ? p.Text.Substring(0, 30) : p.Text));
                    }
                }
            }
        }

        // Test Render at 180% zoom
        writer.ZoomLevel = 1.8f;
        int canvasW = 1600;
        int canvasH = 2400;

        int pageX, pageW, pageH, marginX, rightMarginX, marginY;
        writer.GetPageLayoutMetrics(canvasW, ribbon, out pageX, out pageW, out pageH, out marginX, out rightMarginX, out marginY);
        Console.WriteLine(string.Format(Metrics at 180%: pageW={0}, pageH={1}, marginX={2}, rightMarginX={3}, pageW, pageH, marginX, rightMarginX));

        writer.ScrollY = (int)(pageH + 30);

        using (Bitmap bmp = new Bitmap(canvasW, canvasH))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(243, 243, 243));
                writer.Draw(g, canvasW, canvasH, ribbon);
            }

            string outPath = @scratch\verified_page2_render.png;
            bmp.Save(outPath, ImageFormat.Png);
            Console.WriteLine(Saved render to:  + outPath);
        }
    }
}

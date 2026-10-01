using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

namespace TestRender
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            RibbonBar rb = new RibbonBar();
            string path1 = @"C:\Users\Danh\Desktop\quy\Bang_Thu_Chi_Quy_Lop_1Trang.docx";
            if (File.Exists(path1))
            {
                DocxMetadata meta;
                if (DocxHandler.LoadDocxFile(path1, out meta))
                {
                    WriterView wv = new WriterView();
                    wv.PieceTable.SetInitialText(meta.TextContent);
                    wv.FormattedRuns = meta.FormattedRuns;
                    wv.DocumentTables = meta.Tables;
                    
                    Bitmap bmp = new Bitmap(1000, 1400);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(240, 240, 240));
                        wv.Draw(g, 1000, 1400, rb);
                    }
                    bmp.Save(@"scratch\rendered_quy_lop.png", ImageFormat.Png);
                    Console.WriteLine("Saved rendered_quy_lop.png successfully!");
                }
            }

            string path2 = @"C:\Users\Danh\Downloads\Bài 1 - Liên hợp quốc.docx";
            if (File.Exists(path2))
            {
                DocxMetadata meta2;
                if (DocxHandler.LoadDocxFile(path2, out meta2))
                {
                    WriterView wv2 = new WriterView();
                    wv2.PieceTable.SetInitialText(meta2.TextContent);
                    wv2.FormattedRuns = meta2.FormattedRuns;
                    wv2.DocumentTables = meta2.Tables;

                    Bitmap bmp2 = new Bitmap(1000, 1400);
                    using (Graphics g = Graphics.FromImage(bmp2))
                    {
                        g.Clear(Color.FromArgb(240, 240, 240));
                        wv2.Draw(g, 1000, 1400, rb);
                    }
                    bmp2.Save(@"scratch\rendered_history.png", ImageFormat.Png);
                    Console.WriteLine("Saved rendered_history.png successfully!");
                }
            }
        }
    }
}

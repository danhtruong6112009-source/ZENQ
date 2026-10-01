using System;
using System.Drawing;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

public class DiagPara3
{
    public static void Main()
    {
        string docxPath = @C:\Users\Danh\Downloads\Bài 1.ND GHI.docx;
        DocxMetadata meta;
        DocxHandler.LoadDocxFile(docxPath, out meta);
        var p = meta.Tables[0].Rows[0].Cells[0].Paragraphs[2];
        Console.WriteLine(P2 text:  + p.Text);
        
        using (Bitmap bmp = new Bitmap(2000, 100))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            float zoom = 1.8f;
            int pageW = (int)Math.Round(595.275f * zoom);
            int marginX = (int)Math.Round(56.7f * zoom);
            int rightMarginX = (int)Math.Round(56.7f * zoom);
            int maxTextW = pageW - marginX - rightMarginX; // 867 px
            int padX = (int)Math.Max(2f, 2.5f * zoom); // 4 px -> availW = 867 - 8 = 859 px
            float availW = maxTextW - padX * 2;
            Console.WriteLine(string.Format(pageW={0}, maxTextW={1}, padX={2}, availW={3}, pageW, maxTextW, padX, availW));

            float pSize = 11.0f * zoom;
            using (Font f = new Font(Times New Roman, pSize, FontStyle.Italic))
            {
                var sf = StringFormat.GenericTypographic;
                string l1_word = Trước thảm hoạ do tập đoàn Pol Pot – Ieng Sary gây ra, đáp lại lời kêu gọi của Mặt trận Đoàn kết dân tộc;
                SizeF s1 = g.MeasureString(l1_word, f, Point.Empty, sf);
                Console.WriteLine(string.Format(Word L1 width={0:F2} vs availW={1:F2}, diff={2:F2}, s1.Width, availW, s1.Width - availW));
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Tests
{
    class ExportPreview
    {
        [STAThread]
        static void Main(string[] args)
        {
            string filepath = @"C:\Users\Danh\Downloads\Bao_Cao_Chien_Luoc_CNTT_Toan_Dien.docx";
            WriterView wv = new WriterView();
            RibbonBar ribbon = new RibbonBar();
            wv.LoadDocumentFile(filepath, ribbon);

            int imgW = 1000;
            int imgH = 1350;
            wv.ScrollY = 0;
            using (Bitmap bmp = new Bitmap(imgW, imgH))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(240, 240, 240));
                    wv.Draw(g, imgW, imgH, ribbon);
                }
                bmp.Save(@"scratch\preview_page1.png", ImageFormat.Png);
            }
            Console.WriteLine("Saved scratch\\preview_page1.png");
            Console.WriteLine("Saved scratch\\preview_page2.png");
        }
    }
}

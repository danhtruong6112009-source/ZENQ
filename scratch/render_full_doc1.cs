using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Format;
using GenOffice.UI;

namespace GenOffice.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string docPath = @"C:\Users\Danh\Desktop\Doc1.docx";
                RibbonBar ribbon = new RibbonBar();
                Form dummyParent = new Form();
                dummyParent.Size = new Size(1200, 900);

                WriterView view = new WriterView();
                view.ParentControl = dummyParent;
                view.LoadDocumentFile(docPath, ribbon);

                int w = 1100;
                int h = 2200;
                Bitmap bmp = new Bitmap(w, h);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(240, 242, 245));
                    view.Draw(g, w, h, ribbon);
                }

                string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\doc1_full_page_render.png";
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();
                Console.WriteLine("Saved doc1 full render to: " + outPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.ToString());
            }
        }
    }
}

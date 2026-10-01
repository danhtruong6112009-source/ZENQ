using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class FindCrash
{
    [STAThread]
    public static void Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            string file = @"C:\Users\Danh\Desktop\CHƯƠNG I- BÀI 1. GÓC LƯỢNG GIÁC- CTST- TRƯỜNG THPT SỐ 4 TPLC.docx";
            
            var writer = new WriterView();
            var ribbon = new RibbonBar();
            writer.LoadDocumentFile(file, ribbon);
            
            using (Bitmap bmp = new Bitmap(1920, 1080))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    writer.Draw(g, 1920, 1080, ribbon);
                }
            }
            Console.WriteLine("Draw completed without exceptions!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception in Draw: " + ex.ToString());
        }
    }
}

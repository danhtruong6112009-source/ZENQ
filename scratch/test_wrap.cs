using System;
using System.Drawing;

public class TestWrap
{
    public static void Main()
    {
        string text = "Bài 1: Mệnh đề logic toán học";
        using (Bitmap bmp = new Bitmap(500, 500))
        using (Graphics g = Graphics.FromImage(bmp))
        using (Font f = new Font("Times New Roman", 14f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (StringFormat sf = new StringFormat())
        {
            sf.Trimming = StringTrimming.Word;

            // Chiều rộng 200 (1 dòng)
            SizeF sz1 = g.MeasureString(text, f, 200, sf);
            Console.WriteLine(string.Format("Width=200 -> Measured: W={0:0.0}, H={1:0.0}", sz1.Width, sz1.Height));

            // Chiều rộng 100 (kéo khung vô -> tự xuống 2 dòng)
            SizeF sz2 = g.MeasureString(text, f, 100, sf);
            Console.WriteLine(string.Format("Width=100 -> Measured: W={0:0.0}, H={1:0.0}", sz2.Width, sz2.Height));

            // Chiều rộng 60 (kéo khung vô nữa -> tự xuống 3 dòng)
            SizeF sz3 = g.MeasureString(text, f, 60, sf);
            Console.WriteLine(string.Format("Width=60  -> Measured: W={0:0.0}, H={1:0.0}", sz3.Width, sz3.Height));
        }
    }
}

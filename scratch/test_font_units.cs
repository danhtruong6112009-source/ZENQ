using System;
using System.Drawing;

public class TestFontUnits
{
    public static void Main()
    {
        using (Bitmap bmp = new Bitmap(500, 100))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            string text = "CHƯƠNG I: MỆNH ĐỀ VÀ TẬP HỢP";
            // Font default (GraphicsUnit.Point)
            using (Font fPoint = new Font("Times New Roman", 16f))
            {
                SizeF szP = g.MeasureString(text, fPoint, new PointF(0, 0), StringFormat.GenericTypographic);
                Console.WriteLine("Font with GraphicsUnit.Point (default): Width = " + szP.Width);
            }

            // Font with GraphicsUnit.Pixel
            using (Font fPixel = new Font("Times New Roman", 16f, GraphicsUnit.Pixel))
            {
                SizeF szPx = g.MeasureString(text, fPixel, new PointF(0, 0), StringFormat.GenericTypographic);
                Console.WriteLine("Font with GraphicsUnit.Pixel: Width = " + szPx.Width);
            }
        }
    }
}

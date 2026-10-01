using System;
using System.Drawing;
using System.Drawing.Drawing2D;

class TestGdiLargeCoords
{
    static void Main()
    {
        using (Bitmap bmp = new Bitmap(500, 500))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            float pw = 2915606f;
            float ph = 1218762f;
            float baseW = 500f;
            float baseH = 200f;

            g.ScaleTransform(baseW / pw, baseH / ph);
            try
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(new RectangleF(0, 0, pw, ph), Color.Red, Color.Blue, 0f))
                {
                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        gp.AddRectangle(new RectangleF(0, 0, pw, ph));
                        g.FillPath(lgb, gp);
                    }
                }
                g.ResetTransform();
                Color cMid = bmp.GetPixel(250, 100);
                Color cLeft = bmp.GetPixel(10, 100);
                Color cRight = bmp.GetPixel(490, 100);
                Console.WriteLine(string.Format("Left: R={0},G={1},B={2}", cLeft.R, cLeft.G, cLeft.B));
                Console.WriteLine(string.Format("Mid:  R={0},G={1},B={2}", cMid.R, cMid.G, cMid.B));
                Console.WriteLine(string.Format("Right:R={0},G={1},B={2}", cRight.R, cRight.G, cRight.B));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }
}

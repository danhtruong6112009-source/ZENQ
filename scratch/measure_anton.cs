using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using GenOffice.Core;

class MeasureAnton
{
    static void Main()
    {
        using (Bitmap bmp = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            float pt = 275.99f;
            float px = pt * (96.0f / 72.0f); // 367.9867 px
            using (Font fReg = SlideFontManager.CreateFont("Anton", px, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font fBold = SlideFontManager.CreateFont("Anton", px, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF szReg = g.MeasureString("QUIZ TIME", fReg, PointF.Empty, StringFormat.GenericTypographic);
                SizeF szBold = g.MeasureString("QUIZ TIME", fBold, PointF.Empty, StringFormat.GenericTypographic);
                SizeF szQReg = g.MeasureString("Q", fReg, PointF.Empty, StringFormat.GenericTypographic);
                SizeF szQBold = g.MeasureString("Q", fBold, PointF.Empty, StringFormat.GenericTypographic);
                Console.WriteLine("Anton Regular: " + szReg.Width + " x " + szReg.Height + " (Q: " + szQReg.Width + ")");
                Console.WriteLine("Anton Bold:    " + szBold.Width + " x " + szBold.Height + " (Q: " + szQBold.Width + ")");
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;

class CropTab
{
    static void Main()
    {
        string p = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\.user_uploaded\media_1790314574420.png";
        using (Bitmap img = new Bitmap(p))
        using (Bitmap crop = new Bitmap(350, 60))
        using (Graphics g = Graphics.FromImage(crop))
        {
            // The tab bar is at top around y: 25..65, x: 180..500
            g.DrawImage(img, new Rectangle(0, 0, 350, 60), new Rectangle(180, 25, 350, 60), GraphicsUnit.Pixel);
            crop.Save(@"scratch\tab_crop.png", ImageFormat.Png);
        }
    }
}

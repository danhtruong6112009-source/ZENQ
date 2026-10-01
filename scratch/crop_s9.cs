using System;
using System.Drawing;
using System.Drawing.Imaging;

class CropSlide9
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            Rectangle rect = new Rectangle(200, 200, 1500, 500);
            using (Bitmap cropMs = bMs.Clone(rect, bMs.PixelFormat))
            using (Bitmap cropGen = bGen.Clone(rect, bGen.PixelFormat))
            {
                cropMs.Save(@"scratch\crop_s9_ms.png", ImageFormat.Png);
                cropGen.Save(@"scratch\crop_s9_gen.png", ImageFormat.Png);
            }
        }
        Console.WriteLine("Cropped Slide 9 comparison saved.");
    }
}

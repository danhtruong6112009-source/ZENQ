using System;
using System.Drawing;
using System.Drawing.Imaging;

class CropS8
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide8.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide8.png"))
        {
            Rectangle r = new Rectangle(100, 450, 400, 300);
            using (Bitmap cMs = bMs.Clone(r, bMs.PixelFormat))
            using (Bitmap cGen = bGen.Clone(r, bGen.PixelFormat))
            {
                cMs.Save(@"scratch\s8_crop_ms.png", ImageFormat.Png);
                cGen.Save(@"scratch\s8_crop_gen.png", ImageFormat.Png);
            }
        }
        Console.WriteLine("Saved s8 crops.");
    }
}

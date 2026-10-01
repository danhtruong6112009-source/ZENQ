using System;
using System.Drawing;
using System.Drawing.Imaging;

class SaveQCrop
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide9.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide9.png"))
        {
            Rectangle r = new Rectangle(50, 180, 500, 440);
            using (Bitmap cMs = bMs.Clone(r, bMs.PixelFormat))
            using (Bitmap cGen = bGen.Clone(r, bGen.PixelFormat))
            {
                cMs.Save(@"scratch\q_ms.png", ImageFormat.Png);
                cGen.Save(@"scratch\q_gen.png", ImageFormat.Png);
            }
        }
        Console.WriteLine("Saved q_ms.png and q_gen.png");
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;

class InspectSlide5
{
    static void Main()
    {
        using (Bitmap bMs = new Bitmap(@"scratch\ms365_slide5.png"))
        using (Bitmap bGen = new Bitmap(@"scratch\genoffice_slide5.png"))
        {
            // Crop upper left: X: 0..1000, Y: 0..600
            Rectangle r1 = new Rectangle(0, 0, 1000, 600);
            using (Bitmap cMs = bMs.Clone(r1, bMs.PixelFormat))
            using (Bitmap cGen = bGen.Clone(r1, bGen.PixelFormat))
            {
                cMs.Save(@"scratch\s5_topleft_ms.png", ImageFormat.Png);
                cGen.Save(@"scratch\s5_topleft_gen.png", ImageFormat.Png);
            }

            // Crop bottom right: X: 900..1920, Y: 400..1080
            Rectangle r2 = new Rectangle(900, 400, 1020, 680);
            using (Bitmap cMs2 = bMs.Clone(r2, bMs.PixelFormat))
            using (Bitmap cGen2 = bGen.Clone(r2, bGen.PixelFormat))
            {
                cMs2.Save(@"scratch\s5_botright_ms.png", ImageFormat.Png);
                cGen2.Save(@"scratch\s5_botright_gen.png", ImageFormat.Png);
            }
        }
        Console.WriteLine("Saved Slide 5 crops.");
    }
}

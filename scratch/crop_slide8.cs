using System;
using System.Drawing;

class Program
{
    static void Main()
    {
        using (var refBmp = new Bitmap(@"c:\Users\Danh\Desktop\genofficepy\ms365_ref\slide8.png"))
        using (var genBmp = new Bitmap(@"c:\Users\Danh\Desktop\genofficepy\scratch\audit_slide8.png"))
        {
            // Crop around TextBox 13: X=30..850, Y=30..130
            Rectangle cropRect = new Rectangle(20, 20, 900, 150);
            using (var cropRef = refBmp.Clone(cropRect, refBmp.PixelFormat))
            using (var cropGen = genBmp.Clone(cropRect, genBmp.PixelFormat))
            {
                cropRef.Save(@"c:\Users\Danh\Desktop\genofficepy\scratch\crop_slide8_ref.png");
                cropGen.Save(@"c:\Users\Danh\Desktop\genofficepy\scratch\crop_slide8_gen.png");
                Console.WriteLine("Saved crop images");
            }
        }
    }
}

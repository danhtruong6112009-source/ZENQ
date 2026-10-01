using System;
using GenOffice.Core;
using GenOffice.Format;

class CheckShape4
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");
        SlideNode s8 = pres.Slides[7];
        ShapeNode sh = s8.Shapes[4];
        Console.WriteLine("Shape 4: Name=" + sh.Name + ", Type=" + sh.Type);
        Console.WriteLine("  ImagePath: " + sh.ImagePath);
        Console.WriteLine("  CachedImage: " + (sh.CachedImage != null ? (sh.CachedImage.Width + "x" + sh.CachedImage.Height) : "null"));
        Console.WriteLine("  CustomPath: " + (sh.CustomPath != null));
    }
}

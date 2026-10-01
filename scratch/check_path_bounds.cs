using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using GenOffice.Core;
using GenOffice.Format;

class CheckCustomPathBounds
{
    static void Main()
    {
        PresentationRoot pres = PptxHandler.LoadPptx(@"C:\Users\Danh\Desktop\bài 2.pptx");

        // Slide 5, Freeform 9
        SlideNode s5 = pres.Slides[4];
        ShapeNode f9 = s5.Shapes[5];
        Console.WriteLine("Slide 5 - Freeform 9:");
        Console.WriteLine("  Transform: X=" + f9.Transform.X + " Y=" + f9.Transform.Y + " W=" + f9.Transform.Width + " H=" + f9.Transform.Height);
        Console.WriteLine("  PathWidth=" + f9.PathWidth + " PathHeight=" + f9.PathHeight);
        if (f9.CustomPath != null)
        {
            RectangleF bounds = f9.CustomPath.GetBounds();
            Console.WriteLine(string.Format("  CustomPath Bounds: X={0} Y={1} W={2} H={3}", bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }

        // Slide 8, Freeform 5
        SlideNode s8 = pres.Slides[7];
        ShapeNode f5 = s8.Shapes[2];
        Console.WriteLine("\nSlide 8 - Freeform 5:");
        Console.WriteLine("  Transform: X=" + f5.Transform.X + " Y=" + f5.Transform.Y + " W=" + f5.Transform.Width + " H=" + f5.Transform.Height);
        Console.WriteLine("  PathWidth=" + f5.PathWidth + " PathHeight=" + f5.PathHeight);
        if (f5.CustomPath != null)
        {
            RectangleF bounds = f5.CustomPath.GetBounds();
            Console.WriteLine(string.Format("  CustomPath Bounds: X={0} Y={1} W={2} H={3}", bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }
    }
}

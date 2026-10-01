using System;
using System.IO;
using GenOffice.Format;
using GenOffice.Core;

namespace TestLoad
{
    class Program
    {
        static void Main()
        {
            string path = @"C:\Users\Danh\Downloads\Lesson 1 - GETTING STARTED.pptx";
            PresentationRoot pres = PptxHandler.LoadPptx(path);
            Console.WriteLine("Slides count: " + pres.Slides.Count);
            Console.WriteLine("CanvasWidth: " + pres.CanvasWidth + ", CanvasHeight: " + pres.CanvasHeight + ", IsWidescreen: " + pres.IsWidescreen);
            for (int s = 0; s < Math.Min(3, pres.Slides.Count); s++)
            {
                var slide = pres.Slides[s];
                Console.WriteLine($"\n--- Slide {s + 1} (Shapes: {slide.Shapes.Count}) ---");
                for (int i = 0; i < slide.Shapes.Count; i++)
                {
                    var sh = slide.Shapes[i];
                    Console.WriteLine($"Shape {i}: Name='{sh.Name}', Type={sh.Type}, Rect=({sh.Transform.X}, {sh.Transform.Y}, {sh.Transform.Width}, {sh.Transform.Height}), HasImg={(sh.CachedImage != null ? sh.CachedImage.Width + "x" + sh.CachedImage.Height : "null")}, FillRect=({sh.FillRectLeft},{sh.FillRectTop},{sh.FillRectRight},{sh.FillRectBottom}), Crop=({sh.CropLeft},{sh.CropTop},{sh.CropRight},{sh.CropBottom})");
                }
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using GenOffice.Core;
using GenOffice.UI;

namespace TestRunner
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Testing SlidesView shape render...");
            SlidesView sv = new SlidesView();
            PresentationDoc doc = new PresentationDoc();
            SlideNode slide = new SlideNode();
            doc.Slides.Add(slide);
            sv.Presentation = doc;
            sv.ActiveSlideIndex = 0;

            ShapeNode shape = new ShapeNode
            {
                Type = ShapeType.Rectangle,
                Transform = new Transform2D(100, 100, 300, 150),
                FillColor = unchecked((int)0xFF4472C4),
                StrokeColor = unchecked((int)0xFF203864),
                StrokeWidth = 1.5f
            };
            slide.AddShape(shape);
            sv.SelectedShapeIndex = 0;

            using (Bitmap bmp = new Bitmap(800, 600))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                sv.DrawShape(g, 50, 50, 1.0f, shape, true);
                bmp.Save("scratch/test_render_result.png");
                Console.WriteLine("Saved scratch/test_render_result.png");
            }
        }
    }
}

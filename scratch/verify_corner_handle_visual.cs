using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using GenOffice.Core;
using GenOffice.UI;

namespace VisualVerification
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Rendering visual verification for corner rounding handle and multi-dir resize...");

            SlidesView sv = new SlidesView();
            PresentationDoc doc = new PresentationDoc();
            SlideNode slide = new SlideNode();
            doc.Slides.Add(slide);
            sv.Presentation = doc;
            sv.ActiveSlideIndex = 0;

            // Shape 1: Standard Blue Rectangle (just like user's screenshot)
            ShapeNode rectShape = new ShapeNode
            {
                Type = ShapeType.Rectangle,
                Transform = new Transform2D(60, 80, 280, 160),
                FillColor = unchecked((int)0xFF4472C4),
                StrokeColor = unchecked((int)0xFF203864),
                StrokeWidth = 1.5f,
                CornerRadius = 0f
            };
            slide.AddShape(rectShape);

            // Shape 2: Rounded Rectangle with 25px radius
            ShapeNode roundedShape = new ShapeNode
            {
                Type = ShapeType.RoundedRectangle,
                Transform = new Transform2D(420, 80, 280, 160),
                FillColor = unchecked((int)0xFF10B981), // Emerald green
                StrokeColor = unchecked((int)0xFF047857),
                StrokeWidth = 1.5f,
                CornerRadius = 25f
            };
            slide.AddShape(roundedShape);

            // Shape 3: TextBox with rounded corners
            ShapeNode tbShape = new ShapeNode
            {
                Type = ShapeType.TextBox,
                Transform = new Transform2D(60, 300, 280, 100),
                FillColor = unchecked((int)0xFFF8FAFC),
                StrokeColor = unchecked((int)0xFF6366F1),
                StrokeWidth = 2.0f,
                CornerRadius = 15f,
                Text = "Hộp văn bản bo góc",
                TextColor = unchecked((int)0xFF1E293B),
                FontSize = 14f
            };
            slide.AddShape(tbShape);

            using (Bitmap bmp = new Bitmap(800, 500))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(241, 245, 249));

                // Draw slide white background
                Rectangle slideRect = new Rectangle(40, 40, 720, 420);
                g.FillRectangle(Brushes.White, slideRect);
                using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                {
                    g.DrawRectangle(borderPen, slideRect);
                }

                // Render shape 2 (unselected)
                sv.DrawShape(g, 40, 40, 1.0f, roundedShape, false);

                // Render shape 3 (unselected)
                sv.DrawShape(g, 40, 40, 1.0f, tbShape, false);

                // Select Shape 1 (Rectangle like user) and render with handles
                sv.SelectedShapeIndex = 0;
                sv.DrawShape(g, 40, 40, 1.0f, rectShape, true);

                string outPath = "scratch/verified_corner_knob_and_resize.png";
                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("Saved visual verification image to: " + outPath);
            }
        }
    }
}

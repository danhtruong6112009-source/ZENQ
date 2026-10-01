using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.UI;

namespace VerifyShapeText
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var presentation = new PresentationNode();
            var slide = new SlideNode();
            presentation.Slides.Add(slide);

            // 1. Create a Parallelogram like the user did in media_1790679630319.png
            var paraShape = new ShapeNode
            {
                Type = ShapeType.Parallelogram,
                Transform = new Transform2D(100, 100, 300, 200),
                FillColor = 0x4472C4,
                StrokeColor = 0x203864,
                StrokeWidth = 1.5f,
                Text = "Văn bản hình bình hành căn giữa chuẩn Office 365"
            };
            slide.Shapes.Add(paraShape);

            // 2. Create a Trapezoid
            var trapShape = new ShapeNode
            {
                Type = ShapeType.Trapezoid,
                Transform = new Transform2D(450, 100, 300, 200),
                FillColor = 0x2563EB,
                StrokeColor = 0x1D4ED8,
                StrokeWidth = 1.5f,
                Text = "Hình thang căn giữa và không bị tràn viền"
            };
            slide.Shapes.Add(trapShape);

            var slidesView = new SlidesView();
            slidesView.Presentation = presentation;
            slidesView.ActiveSlideIndex = 0;

            using (var bmp = new Bitmap(960, 540))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(240, 240, 240));
                slidesView.Draw(g, 960, 540);
                string outPath = Path.Combine(Directory.GetCurrentDirectory(), "scratch/verify_shapes_text_result.png");
                bmp.Save(outPath, ImageFormat.Png);
                string artifactDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c");
                bmp.Save(Path.Combine(artifactDir, "verify_shapes_text_result.png"), ImageFormat.Png);
                Console.WriteLine("Saved: " + outPath);
            }
        }
    }
}

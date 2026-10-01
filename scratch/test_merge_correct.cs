using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class TestMergeShapesCorrect
{
    static void Main()
    {
        string scratchDir = "scratch";
        string outPptx = Path.Combine(scratchDir, "test_merge_shapes.pptx");

        PresentationRoot pres = new PresentationRoot();
        pres.Slides.Clear();
        pres.CanvasWidth = 1920;
        pres.CanvasHeight = 1080;
        SlideNode slide = new SlideNode { Id = "s1" };
        pres.Slides.Add(slide);

        // Shape 1: Rectangle at (300, 300, 400, 300) with multi-stop alpha gradient
        ShapeNode s1 = new ShapeNode
        {
            Name = "Rect1",
            Type = ShapeType.Rectangle,
            Transform = new Transform2D(300, 300, 400, 300),
            HasGradientFill = true,
            GradientAngle = 45f,
            GradientStops = new System.Collections.Generic.List<GradientStop>
            {
                new GradientStop(0xFF0055, 0.0f, 0.0f),
                new GradientStop(0x00A2FF, 0.5f, 0.2f),
                new GradientStop(0x00FF88, 1.0f, 0.0f)
            },
            StrokeColor = 0xFFFFFF,
            StrokeWidth = 3f
        };

        // Shape 2: Ellipse at (500, 400, 400, 400)
        ShapeNode s2 = new ShapeNode
        {
            Name = "Circle2",
            Type = ShapeType.Ellipse,
            Transform = new Transform2D(500, 400, 400, 400),
            FillColor = 0x00AAFF,
            StrokeColor = 0xFFFFFF,
            StrokeWidth = 2f
        };

        slide.AddShape(s1);
        slide.AddShape(s2);

        SlidesView sv = new SlidesView { Presentation = pres };
        sv.ActiveSlideIndex = 0;
        sv.SelectedShapeIndices.Clear();
        sv.SelectedShapeIndices.Add(0); // Select s1
        sv.SelectedShapeIndices.Add(1); // Select s2

        Console.WriteLine("[1] Executing MergeShapes(Union)...");
        sv.MergeShapes(MergeShapeOperation.Union);

        Console.WriteLine("  Slide shapes after union: " + slide.Shapes.Count);
        ShapeNode merged = slide.Shapes[0];
        Console.WriteLine("  Merged shape name: " + merged.Name + " Type: " + merged.Type);
        Console.WriteLine(string.Format("  Merged bounds: X={0} Y={1} W={2} H={3} PathW={4} PathH={5}",
            merged.Transform.X, merged.Transform.Y, merged.Transform.Width, merged.Transform.Height,
            merged.PathWidth, merged.PathHeight));
        Console.WriteLine("  HasGrad: " + merged.HasGradientFill + " Stops: " + (merged.GradientStops != null ? merged.GradientStops.Count : 0));

        Console.WriteLine("\n[2] Saving to PPTX: " + outPptx);
        PptxHandler.SavePptx(outPptx, pres);
        Console.WriteLine("  Saved successfully! File size: " + new FileInfo(outPptx).Length + " bytes");

        Console.WriteLine("\n[3] Reloading merged shape PPTX in GenOffice...");
        PresentationRoot loadedPres = PptxHandler.LoadPptx(outPptx);
        Console.WriteLine("  Loaded slides: " + loadedPres.Slides.Count);
        ShapeNode reloadedShape = loadedPres.Slides[0].Shapes[0];
        Console.WriteLine("  Reloaded shape type: " + reloadedShape.Type + " CustomPath: " + (reloadedShape.CustomPath != null));
        Console.WriteLine(string.Format("  Reloaded bounds: X={0} Y={1} W={2} H={3} PathW={4} PathH={5}",
            reloadedShape.Transform.X, reloadedShape.Transform.Y, reloadedShape.Transform.Width, reloadedShape.Transform.Height,
            reloadedShape.PathWidth, reloadedShape.PathHeight));
        Console.WriteLine("  HasGrad: " + reloadedShape.HasGradientFill + " Stops: " + (reloadedShape.GradientStops != null ? reloadedShape.GradientStops.Count : 0));

        Console.WriteLine("\n[4] Verifying with Microsoft PowerPoint 365 COM...");
        try
        {
            Type pptType = Type.GetTypeFromProgID("PowerPoint.Application");
            if (pptType != null)
            {
                dynamic ppt = Activator.CreateInstance(pptType);
                dynamic presCom = ppt.Presentations.Open(Path.GetFullPath(outPptx), 1, 0, 0);
                Console.WriteLine("  MS 365 opened merged PPTX with 0 errors! Slides: " + presCom.Slides.Count);
                string outPng = Path.Combine(scratchDir, "ms365_merged_union.png");
                presCom.Slides[1].Export(Path.GetFullPath(outPng), "PNG", 1920, 1080);
                presCom.Close();
                ppt.Quit();
                Console.WriteLine("  MS 365 successfully exported merged slide to: " + outPng);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  MS 365 COM error: " + ex.Message);
        }
    }
}

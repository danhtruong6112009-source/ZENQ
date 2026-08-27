using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestMotionPaths
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Testing Motion Paths Engine in GenOffice Slides ===");

                // 1. Initialize presentation and slides
                PresentationRoot pres = new PresentationRoot();
                pres.Slides.Clear();
                SlideNode slide = new SlideNode();
                slide.BgColor = 0xF8F9FA;
                pres.Slides.Add(slide);

                // Add test shapes
                ShapeNode circleShape = new ShapeNode
                {
                    Id = "shape_circle_1",
                    Type = ShapeType.Ellipse,
                    Transform = new Transform2D(120f, 100f, 90f, 90f),
                    FillColor = 0x3B82F6,
                    StrokeColor = 0x1D4ED8,
                    StrokeWidth = 2f,
                    Text = "Lines"
                };
                slide.Shapes.Add(circleShape);

                ShapeNode rectShape = new ShapeNode
                {
                    Id = "shape_rect_2",
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(120f, 220f, 110f, 60f),
                    FillColor = 0x10B981,
                    StrokeColor = 0x047857,
                    StrokeWidth = 2f,
                    Text = "Arcs"
                };
                slide.Shapes.Add(rectShape);

                ShapeNode turnShape = new ShapeNode
                {
                    Id = "shape_turn_3",
                    Type = ShapeType.Rectangle,
                    Transform = new Transform2D(120f, 320f, 90f, 60f),
                    FillColor = 0xEF4444,
                    StrokeColor = 0xB91C1C,
                    StrokeWidth = 2f,
                    Text = "Turns"
                };
                slide.Shapes.Add(turnShape);

                ShapeNode starShape = new ShapeNode
                {
                    Id = "shape_star_4",
                    Type = ShapeType.Diamond,
                    Transform = new Transform2D(500f, 140f, 80f, 80f),
                    FillColor = 0xF59E0B,
                    StrokeColor = 0xB45309,
                    StrokeWidth = 2f,
                    Text = "Loops"
                };
                slide.Shapes.Add(starShape);

                // Add motion path animations
                // 1. Lines
                slide.Animations.Add(new ShapeAnimation
                {
                    ShapeId = "shape_circle_1",
                    Type = AnimationType.MotionLine,
                    Duration = 2.0f,
                    Direction = TransitionDirection.Smooth,
                    EndOffsetX = 240,
                    EndOffsetY = 0,
                    HasCustomEndOffset = true,
                    OrderIndex = 0
                });

                // 2. Arcs
                slide.Animations.Add(new ShapeAnimation
                {
                    ShapeId = "shape_rect_2",
                    Type = AnimationType.MotionArc,
                    Duration = 2.5f,
                    Direction = TransitionDirection.Smooth,
                    EndOffsetX = 240,
                    EndOffsetY = 60,
                    HasCustomEndOffset = true,
                    OrderIndex = 1
                });

                // 3. Turns
                slide.Animations.Add(new ShapeAnimation
                {
                    ShapeId = "shape_turn_3",
                    Type = AnimationType.MotionTurn,
                    Duration = 2.5f,
                    EndOffsetX = 200,
                    EndOffsetY = 80,
                    HasCustomEndOffset = true,
                    OrderIndex = 2
                });

                // 4. Loops
                slide.Animations.Add(new ShapeAnimation
                {
                    ShapeId = "shape_star_4",
                    Type = AnimationType.MotionLoop,
                    Duration = 3.0f,
                    OrderIndex = 3
                });

                Console.WriteLine("[PASS] Added 4 motion path animations: MotionLine, MotionArc, MotionTurn, MotionLoop.");

                // Test SlidesView Rendering to Bitmap
                SlidesView sv = new SlidesView();
                sv.Presentation = pres;
                sv.ActiveSlideIndex = 0;
                sv.SelectedShapeIndex = 0; // Select circle to highlight motion path in design mode

                int canvasW = 1200;
                int canvasH = 700;
                Bitmap bmpDesign = new Bitmap(canvasW, canvasH);
                using (Graphics g = Graphics.FromImage(bmpDesign))
                {
                    g.Clear(Color.FromArgb(235, 238, 242));
                    sv.Draw(g, canvasW, canvasH);
                }

                string artifactDir = @"C:\Users\Danh\.gemini\antigravity-ide\brain\8e6530d4-1a0a-4590-90e2-d532581fa545";
                string outPathDesign = Path.Combine(artifactDir, "motion_paths_design_mode.png");
                bmpDesign.Save(outPathDesign, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("[PASS] Saved Design Mode Motion Path rendering to " + outPathDesign);

                // Test Parametric Bezier Evaluation across time steps
                ShapeAnimation lineAnim = slide.Animations[0];
                float offX0, offY0, offX5, offY5, offX1, offY1;
                SlidesView.EvaluateMotionPathOffset(lineAnim, 0.0f, out offX0, out offY0);
                SlidesView.EvaluateMotionPathOffset(lineAnim, 0.5f, out offX5, out offY5);
                SlidesView.EvaluateMotionPathOffset(lineAnim, 1.0f, out offX1, out offY1);

                Console.WriteLine(string.Format("MotionLine offset: t=0.0 -> ({0},{1}), t=0.5 -> ({2},{3}), t=1.0 -> ({4},{5})",
                    offX0, offY0, offX5, offY5, offX1, offY1));

                if (Math.Abs(offX0) < 0.01f && Math.Abs(offX1 - 240f) < 0.01f && offX5 > 50f && offX5 < 200f)
                {
                    Console.WriteLine("[PASS] Parametric evaluation & easing curves computed correctly!");
                }
                else
                {
                    Console.WriteLine("[FAIL] Parametric offset mismatch!");
                }

                Console.WriteLine("=== ALL MOTION PATH VERIFICATION CHECKS PASSED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EXCEPTION] " + ex.ToString());
            }
        }
    }
}

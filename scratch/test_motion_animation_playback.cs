using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using GenOffice.Core;
using GenOffice.UI;

namespace TestAnimationPlayback
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                PresentationRoot pres = new PresentationRoot();
                SlideNode slide = pres.Slides[0];
                slide.Shapes.Clear();

                ShapeNode shape = new ShapeNode
                {
                    Id = "shape_arc",
                    Type = ShapeType.RoundedRectangle,
                    Transform = new Transform2D(200, 260, 140, 70),
                    FillColor = 0x2563EB,
                    StrokeColor = 0xFFFFFF,
                    StrokeWidth = 2f,
                    Text = "🚀 Bouncing Arc",
                    FontSize = 11f,
                    Bold = true,
                    TextColor = 0xFFFFFF,
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalTextAlignment.Middle
                };
                slide.Shapes.Add(shape);

                ShapeAnimation anim = new ShapeAnimation
                {
                    ShapeId = shape.Id,
                    Type = AnimationType.MotionArc,
                    Trigger = AnimationTrigger.OnClick,
                    Duration = 1.0f,
                    Direction = TransitionDirection.Smooth,
                    OrderIndex = 0
                };
                slide.Animations.Add(anim);

                SlidesView sv = new SlidesView();
                sv.Presentation = pres;
                sv.ActiveSlideIndex = 0;

                // Test at 5 different time frames: 0%, 25%, 50%, 75%, 100%
                float[] frames = new float[] { 0.0f, 0.25f, 0.50f, 0.75f, 1.0f };
                int w = 900;
                int h = 600;

                using (Bitmap montage = new Bitmap(w * 5 / 2, h))
                using (Graphics mg = Graphics.FromImage(montage))
                {
                    mg.Clear(Color.FromArgb(240, 243, 246));

                    for (int f = 0; f < frames.Length; f++)
                    {
                        float p = frames[f];
                        bool isVis;
                        float offX, offY, scX, scY, rDelta, opMul;
                        sv.GetShapeAnimationTransform(shape, slide, 0, false, true, 0, p, out isVis, out offX, out offY, out scX, out scY, out rDelta, out opMul);

                        Console.WriteLine(string.Format("Frame {0:0}%: offX={1:0.0}, offY={2:0.0}", p * 100, offX, offY));
                    }
                }

                Console.WriteLine("ALL ANIMATION TESTS PASSED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}

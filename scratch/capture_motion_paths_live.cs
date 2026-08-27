using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestLiveMotionPaths
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

                string[] titles = new string[] { "Lines (Đường thẳng)", "Arcs (Cung tròn)", "Turns (Góc cua)", "Shapes (Hình học)", "Loops (Vòng lặp 8)", "Custom Path (S-Curve)" };
                AnimationType[] animTypes = new AnimationType[] { AnimationType.MotionLine, AnimationType.MotionArc, AnimationType.MotionTurn, AnimationType.MotionShape, AnimationType.MotionLoop, AnimationType.MotionCustom };
                int[] colors = new int[] { 0x2563EB, 0x0D9488, 0xEA580C, 0x7C3AED, 0xDB2777, 0x059669 };

                for (int i = 0; i < 6; i++)
                {
                    int col = i % 3;
                    int row = i / 3;
                    float sx = 80 + col * 280;
                    float sy = 70 + row * 220;

                    ShapeNode shape = new ShapeNode
                    {
                        Id = "shape_" + (i + 1),
                        Type = ShapeType.RoundedRectangle,
                        Transform = new Transform2D(sx, sy, 110, 60),
                        FillColor = colors[i],
                        StrokeColor = 0xFFFFFF,
                        StrokeWidth = 1.5f,
                        Text = titles[i],
                        FontSize = 9.5f,
                        Bold = true,
                        TextColor = 0xFFFFFF,
                        Alignment = TextAlignment.Center,
                        VerticalAlignment = VerticalTextAlignment.Middle
                    };
                    slide.Shapes.Add(shape);

                    ShapeAnimation anim = new ShapeAnimation
                    {
                        ShapeId = shape.Id,
                        Type = animTypes[i],
                        Trigger = AnimationTrigger.OnClick,
                        Duration = 1.2f,
                        Direction = TransitionDirection.Smooth,
                        OrderIndex = i
                    };
                    slide.Animations.Add(anim);
                }

                SlidesView sv = new SlidesView();
                sv.Presentation = pres;
                sv.ActiveSlideIndex = 0;
                sv.SelectedShapeIndex = 0;

                int w = 1100;
                int h = 720;
                using (Bitmap bmp = new Bitmap(w, h))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    sv.Draw(g, w, h);

                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\slides_motion_paths_live.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SAVED DESIGN MODE: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }
    }
}

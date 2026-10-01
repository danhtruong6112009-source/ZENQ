using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class CompareMergedRender
{
    static void Main()
    {
        string scratchDir = "scratch";
        string pptx = Path.Combine(scratchDir, "test_merge_shapes.pptx");
        string msPng = Path.Combine(scratchDir, "ms365_merged_union.png");
        string genPng = Path.Combine(scratchDir, "genoffice_merged_union.png");

        PresentationRoot pres = PptxHandler.LoadPptx(pptx);
        using (Bitmap bmp = new Bitmap(1920, 1080))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            SlidesView sv = new SlidesView { Presentation = pres };
            sv.DrawSlideBackground(g, new Rectangle(0, 0, 1920, 1080), pres.Slides[0]);
            foreach (ShapeNode sh in pres.Slides[0].Shapes)
            {
                sv.DrawShape(g, 0, 0, 1.0f, sh, false);
            }
            bmp.Save(genPng, ImageFormat.Png);
        }

        // Compare genPng with msPng
        using (Bitmap bMs = new Bitmap(msPng))
        using (Bitmap bGen = new Bitmap(genPng))
        {
            int diffCount = 0;
            int totalChecked = 0;
            for (int y = 0; y < 1080; y += 2)
            {
                for (int x = 0; x < 1920; x += 2)
                {
                    totalChecked++;
                    Color c1 = bMs.GetPixel(x, y);
                    Color c2 = bGen.GetPixel(x, y);
                    if (Math.Abs(c1.R - c2.R) > 20 || Math.Abs(c1.G - c2.G) > 20 || Math.Abs(c1.B - c2.B) > 20)
                    {
                        diffCount++;
                    }
                }
            }
            Console.WriteLine(string.Format("Merged Shape Diff: {0} / {1} ({2:F2}%)",
                diffCount, totalChecked, (diffCount * 100.0 / totalChecked)));

            // Side by Side
            using (Bitmap cmp = new Bitmap(1920 * 2, 1080))
            using (Graphics gCmp = Graphics.FromImage(cmp))
            {
                gCmp.DrawImage(bMs, 0, 0, 1920, 1080);
                gCmp.DrawImage(bGen, 1920, 0, 1920, 1080);
                cmp.Save(Path.Combine(scratchDir, "cmp_merged_union.png"), ImageFormat.Png);
            }
            Console.WriteLine("Saved: cmp_merged_union.png");
        }
    }
}

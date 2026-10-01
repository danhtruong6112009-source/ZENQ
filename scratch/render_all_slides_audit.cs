using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class Program
{
    [STAThread]
    static void Main()
    {
        PresentationRoot pres;
        bool ok = PptxHandler.LoadPptx(@"c:\Users\Danh\Desktop\bài 2.pptx", out pres);
        if (!ok || pres == null)
        {
            Console.WriteLine("[FAIL] Could not load pptx");
            return;
        }

        SlidesView sv = new SlidesView();
        sv.Presentation = pres;
        sv.IsPresenting = true;
        sv.IsPresenterViewMode = false;

        int w = 1920;
        int h = 1080;

        for (int i = 0; i < 10; i++)
        {
            sv.ActiveSlideIndex = i;
            string outPath = string.Format(@"c:\Users\Danh\Desktop\genofficepy\scratch\audit_slide{0}.png", i + 1);
            using (Bitmap bmp = new Bitmap(w, h))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                sv.Draw(g, w, h);
                bmp.Save(outPath);
            }
            Console.WriteLine(string.Format("[PASS] Rendered slide {0}", i + 1));

            // Create side-by-side comparison image: Left = MS 365 Reference, Right = GenOffice Render
            string refPath = string.Format(@"c:\Users\Danh\Desktop\genofficepy\ms365_ref\slide{0}.png", i + 1);
            if (File.Exists(refPath))
            {
                string cmpPath = string.Format(@"c:\Users\Danh\Desktop\genofficepy\scratch\cmp_slide{0}.png", i + 1);
                using (Bitmap refBmp = new Bitmap(refPath))
                using (Bitmap genBmp = new Bitmap(outPath))
                using (Bitmap cmpBmp = new Bitmap(1920, 540)) // Half-height side-by-side
                using (Graphics gCmp = Graphics.FromImage(cmpBmp))
                {
                    gCmp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gCmp.DrawImage(refBmp, new Rectangle(0, 0, 960, 540));
                    gCmp.DrawImage(genBmp, new Rectangle(960, 0, 960, 540));
                    using (Pen divider = new Pen(Color.Red, 2f))
                    {
                        gCmp.DrawLine(divider, 960, 0, 960, 540);
                    }
                    using (Font labelFont = new Font("Arial", 12f, FontStyle.Bold))
                    using (SolidBrush bgB = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    using (SolidBrush textB = new SolidBrush(Color.White))
                    {
                        gCmp.FillRectangle(bgB, 10, 10, 200, 30);
                        gCmp.DrawString("MS 365 Reference", labelFont, textB, 15, 15);

                        gCmp.FillRectangle(bgB, 970, 10, 200, 30);
                        gCmp.DrawString("GenOffice Render", labelFont, textB, 975, 15);
                    }
                    cmpBmp.Save(cmpPath);
                    Console.WriteLine(string.Format("[PASS] Created comparison cmp_slide{0}.png", i + 1));
                }
            }
        }
        Console.WriteLine("=== ALL 10 SLIDES RENDERED AND COMPARED ===");
    }
}

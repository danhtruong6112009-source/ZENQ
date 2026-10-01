using System;
using System.IO;
using System.Drawing;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

class Program
{
    [STAThread]
    static void Main()
    {
        string srcPath = @"c:\Users\Danh\Desktop\bài 2.pptx";
        string outPath = @"c:\Users\Danh\Desktop\genofficepy\scratch\test_roundtrip.pptx";

        Console.WriteLine("Step 1: Loading original PPTX...");
        PresentationRoot pres1;
        bool ok1 = PptxHandler.LoadPptx(srcPath, out pres1);
        if (!ok1 || pres1 == null)
        {
            Console.WriteLine("[FAIL] Failed to load source PPTX!");
            return;
        }
        Console.WriteLine(string.Format("[PASS] Original loaded successfully: {0} slides", pres1.Slides.Count));

        Console.WriteLine("Step 2: Exporting to new PPTX: " + outPath);
        if (File.Exists(outPath)) File.Delete(outPath);
        bool okSave = PptxHandler.SavePptx(outPath, pres1);
        if (!okSave || !File.Exists(outPath))
        {
            Console.WriteLine("[FAIL] Failed to save PPTX!");
            return;
        }
        FileInfo fi = new FileInfo(outPath);
        Console.WriteLine(string.Format("[PASS] Saved successfully: {0:N0} bytes", fi.Length));

        Console.WriteLine("Step 3: Loading exported PPTX back...");
        PresentationRoot pres2;
        bool ok2 = PptxHandler.LoadPptx(outPath, out pres2);
        if (!ok2 || pres2 == null)
        {
            Console.WriteLine("[FAIL] Failed to reload exported PPTX!");
            return;
        }
        Console.WriteLine(string.Format("[PASS] Reloaded successfully: {0} slides", pres2.Slides.Count));

        // Compare slide and shape counts
        int totalDiffs = 0;
        for (int i = 0; i < Math.Min(pres1.Slides.Count, pres2.Slides.Count); i++)
        {
            var s1 = pres1.Slides[i];
            var s2 = pres2.Slides[i];
            if (s1.Shapes.Count != s2.Shapes.Count)
            {
                Console.WriteLine(string.Format("Slide {0} shape count mismatch: orig={1}, reloaded={2}", i + 1, s1.Shapes.Count, s2.Shapes.Count));
                totalDiffs++;
            }
        }
        Console.WriteLine(string.Format("Total slide shape count differences: {0}", totalDiffs));

        // Render Slide 3 and Slide 5 from reloaded presentation
        SlidesView sv = new SlidesView();
        sv.Presentation = pres2;

        int w = 1600;
        int h = 900;
        sv.ActiveSlideIndex = 2; // Slide 3
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(240, 240, 240));
            sv.Draw(g, w, h);
            bmp.Save(@"c:\Users\Danh\Desktop\genofficepy\scratch\roundtrip_slide3.png");
            Console.WriteLine("[PASS] Saved roundtrip_slide3.png");
        }

        sv.ActiveSlideIndex = 4; // Slide 5
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(240, 240, 240));
            sv.Draw(g, w, h);
            bmp.Save(@"c:\Users\Danh\Desktop\genofficepy\scratch\roundtrip_slide5.png");
            Console.WriteLine("[PASS] Saved roundtrip_slide5.png");
        }

        Console.WriteLine("=== ROUNDTRIP VERIFICATION COMPLETE ===");
    }
}

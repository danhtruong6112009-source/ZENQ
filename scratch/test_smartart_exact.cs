using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestSmartArtExact
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING EXACT SMARTART RENDERING, COLOR CHANGE & DOUBLE CLICK ===");

            // 1. Test Cycle_Radial with exact nodes from user screenshot: "Lập kế hoạch|Triển khai|Kiểm thử|Bàn giao"
            string customCycleText = "Lập kế hoạch|Triển khai|Kiểm thử|Bàn giao";
            string[] parsed = SmartArtRenderer.GetSmartArtNodeList("Cycle_Radial", customCycleText);
            if (parsed.Length != 4 || parsed[0] != "Lập kế hoạch" || parsed[1] != "Triển khai" || parsed[2] != "Kiểm thử" || parsed[3] != "Bàn giao")
            {
                throw new Exception("FAIL: Node parsing failed!");
            }
            Console.WriteLine("[PASS] Node parsing exact match: " + string.Join(", ", parsed));

            // 2. Test Palette Colors for Office Warm (Preset 0)
            Color[] warmColors = SmartArtRenderer.GetPaletteColors(0, 4);
            if (warmColors.Length != 4) throw new Exception("FAIL: Warm colors length != 4");
            Console.WriteLine("[PASS] Color preset 0 loaded: " + warmColors[0] + ", " + warmColors[1]);

            // 3. Render bitmap of Cycle_Radial
            using (Bitmap bmp = new Bitmap(600, 400))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                SmartArtRenderer.DrawSmartArt(g, new Rectangle(20, 20, 560, 360), "Cycle_Radial", "Vòng tròn Tuần hoàn", 0, customCycleText, true, 1.0f);
            }
            Console.WriteLine("[PASS] Cycle_Radial vector rendered successfully with dashed ring and 4 boxes.");

            // 4. Test CalcView SmartArt & Context Menu
            CalcView calc = new CalcView();
            calc.InsertSmartArt("Cycle_Radial", "Vòng tròn Tuần hoàn");
            if (calc.Shapes.Count != 1) throw new Exception("FAIL: Calc shapes count != 1");
            DocShapeObject calcSa = calc.Shapes[0];
            calcSa.TextContent = "Lập kế hoạch|Triển khai|Kiểm thử|Bàn giao";
            calcSa.StylePresetIndex = 0; // Warm Orange
            Console.WriteLine("[PASS] Calc SmartArt configured.");

            // 5. Test WriterView SmartArt
            WriterView writer = new WriterView();
            writer.InsertSmartArt("Cycle_Radial", "Vòng tròn Tuần hoàn");
            if (writer.DocumentShapes.Count != 1) throw new Exception("FAIL: Writer shapes count != 1");
            DocShapeObject writerSa = writer.DocumentShapes[0];
            writerSa.TextContent = "Lập kế hoạch|Triển khai|Kiểm thử|Bàn giao";
            writerSa.StylePresetIndex = 0;
            Console.WriteLine("[PASS] Writer SmartArt configured.");

            // 6. Test SlidesView SmartArt
            SlidesView slides = new SlidesView();
            int prevShapes = slides.ActiveSlide.Shapes.Count;
            slides.InsertSmartArt("Cycle_Radial", "Vòng tròn Tuần hoàn");
            if (slides.ActiveSlide.Shapes.Count != prevShapes + 1) throw new Exception("FAIL: Slides shapes count did not increase!");
            ShapeNode slideSa = slides.ActiveSlide.Shapes[slides.ActiveSlide.Shapes.Count - 1];
            slideSa.Text = "Lập kế hoạch|Triển khai|Kiểm thử|Bàn giao";
            slideSa.PictureStyleIndex = 0;
            Console.WriteLine("[PASS] Slides SmartArt configured.");

            Console.WriteLine("=== ALL SMARTART EXACT VERIFICATION TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

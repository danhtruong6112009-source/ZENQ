using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestSmartArtDrawing
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING VECTOR GRAPHICAL RENDERING FOR ALL SMARTART TYPES ===");

            string[] types = new string[] {
                "Process_Linear",
                "Process_Chevron",
                "Cycle_Radial",
                "Hierarchy_Tree",
                "List_Blocks",
                "Matrix_Grid",
                "Pyramid_Layers"
            };

            // 1. Test SmartArtRenderer on Bitmap
            foreach (var t in types)
            {
                using (Bitmap bmp = new Bitmap(600, 300))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    SmartArtRenderer.DrawSmartArt(g, new Rectangle(10, 10, 580, 280), t, "Test " + t, Color.Blue, Color.Navy, "", true, 1.0f);
                }
                Console.WriteLine("[PASS] Rendered SmartArt type: " + t);
            }

            // 2. Test CalcView SmartArt Integration
            CalcView calc = new CalcView();
            calc.InsertSmartArt("Process_Chevron", "Lộ trình Chuyển đổi");
            if (calc.Shapes.Count < 1) throw new Exception("Calc shape count < 1");
            using (Bitmap b = new Bitmap(1000, 600))
            using (Graphics g = Graphics.FromImage(b))
            {
                calc.Draw(g, 1000, 600);
            }
            Console.WriteLine("[PASS] CalcView rendered Process_Chevron SmartArt shape successfully.");

            // 3. Test WriterView SmartArt Integration
            WriterView writer = new WriterView();
            writer.InsertSmartArt("Hierarchy_Tree", "Sơ đồ Cây Tổ chức");
            if (writer.DocumentShapes.Count < 1) throw new Exception("Writer shape count < 1");
            using (Bitmap b = new Bitmap(1000, 600))
            using (Graphics g = Graphics.FromImage(b))
            {
                RibbonBar ribbon = new RibbonBar();
                writer.Draw(g, 1000, 600, ribbon);
            }
            Console.WriteLine("[PASS] WriterView rendered Hierarchy_Tree SmartArt shape successfully.");

            // 4. Test SlidesView SmartArt Integration
            SlidesView slides = new SlidesView();
            int prevCount = slides.ActiveSlide.Shapes.Count;
            slides.InsertSmartArt("Cycle_Radial", "Chu trình Tuần hoàn");
            if (slides.ActiveSlide.Shapes.Count != prevCount + 1) throw new Exception("Slides shape count did not increase by 1!");
            using (Bitmap b = new Bitmap(1000, 600))
            using (Graphics g = Graphics.FromImage(b))
            {
                slides.Draw(g, 1000, 600);
            }
            Console.WriteLine("[PASS] SlidesView rendered Cycle_Radial SmartArt shape successfully.");

            Console.WriteLine("=== ALL SMARTART VECTOR DRAWING TESTS PASSED! ===");
        }
    }
}

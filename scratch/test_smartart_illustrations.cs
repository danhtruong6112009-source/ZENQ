using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestSmartArtIllustrations
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING SMARTART & ILLUSTRATIONS IN CALC & WRITER ===");

            // 1. Test Calc SmartArt Insertion
            CalcView calc = new CalcView();
            calc.InsertSmartArt("Process_Linear", "Quy trình Tuyến tính");
            if (calc.Shapes.Count != 1)
            {
                throw new Exception("FAIL: Calc failed to insert SmartArt shape!");
            }
            DocShapeObject saCalc = calc.Shapes[0];
            Console.WriteLine("[PASS] Calc SmartArt created: Text=" + saCalc.TextContent.Substring(0, 30) + "...");

            // 2. Test Writer SmartArt Insertion
            WriterView writer = new WriterView();
            writer.InsertSmartArt("Cycle_Radial", "Vòng tròn Tuần hoàn");
            if (writer.DocumentShapes.Count != 1)
            {
                throw new Exception("FAIL: Writer failed to insert SmartArt shape!");
            }
            DocShapeObject saWriter = writer.DocumentShapes[0];
            Console.WriteLine("[PASS] Writer SmartArt created: Text=" + saWriter.TextContent.Substring(0, 30) + "...");

            // 3. Test SmartArtGalleryForm instance
            using (SmartArtGalleryForm saDlg = new SmartArtGalleryForm())
            {
                if (saDlg == null) throw new Exception("FAIL: SmartArtGalleryForm is null!");
            }
            Console.WriteLine("[PASS] SmartArtGalleryForm initialized successfully.");

            Console.WriteLine("=== ALL SMARTART & ILLUSTRATION TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}

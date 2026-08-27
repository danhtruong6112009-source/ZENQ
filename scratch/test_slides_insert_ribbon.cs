using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace TestSlidesInsertRibbon
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Testing PowerPoint Insert Ribbon Redesign & Layout ===");

                RibbonBar ribbon = new RibbonBar();
                ribbon.CurrentMode = OfficeAppMode.Slides;
                ribbon.ActiveRibbonTab = 1; // Tab Chèn (Insert)

                SlidesView sv = new SlidesView();

                // 1. Test Drawing Ribbon
                using (Bitmap bmp = new Bitmap(1300, 160))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    ribbon.Draw(g, 1300, 160, null, sv, null, null);
                    Console.WriteLine("RibbonBar DrawPowerPointTabInsert rendered successfully without errors!");
                }

                // 2. Test Slide Mới click (X: 30, Y: 80)
                int initSlides = sv.Presentation.Slides.Count;
                ribbon.HandleClick(30, 80, null, null, sv, null, null);
                Console.WriteLine(string.Format("Added Slide via Ribbon: Before={0}, After={1}", initSlides, sv.Presentation.Slides.Count));
                bool addSlidePass = (sv.Presentation.Slides.Count == initSlides + 1);

                // 3. Test Hộp chữ click (X: 720, Y: 80)
                ribbon.HandleClick(720, 80, null, null, sv, null, null);
                Console.WriteLine("Hộp chữ draw mode: " + sv.PendingShapeType + " (IsDrawing: " + sv.IsDrawingShapeMode + ")");
                bool textBoxPass = (sv.IsDrawingShapeMode && sv.PendingShapeType == ShapeType.TextBox);

                // 4. Test Đầu & Chân trang click (X: 770, Y: 80)
                int initShapes = sv.ActiveSlide.Shapes.Count;
                ribbon.HandleClick(770, 80, null, null, sv, null, null);
                Console.WriteLine(string.Format("Inserted Header/Footer: Shapes Before={0}, After={1}", initShapes, sv.ActiveSlide.Shapes.Count));
                bool hfPass = (sv.ActiveSlide.Shapes.Count == initShapes + 1 && sv.ActiveSlide.Shapes[sv.ActiveSlide.Shapes.Count - 1].Name == "Chân trang (Footer)");

                // 5. Test Bình luận click (X: 650, Y: 80)
                initShapes = sv.ActiveSlide.Shapes.Count;
                ribbon.HandleClick(650, 80, null, null, sv, null, null);
                Console.WriteLine(string.Format("Inserted Comment: Shapes Before={0}, After={1}", initShapes, sv.ActiveSlide.Shapes.Count));
                bool cmtPass = (sv.ActiveSlide.Shapes.Count == initShapes + 1 && sv.ActiveSlide.Shapes[sv.ActiveSlide.Shapes.Count - 1].Name == "Bình luận");

                if (addSlidePass && textBoxPass && hfPass && cmtPass)
                {
                    Console.WriteLine("\n=== ALL_SLIDES_INSERT_RIBBON_TESTS_PASSED_SUCCESSFULLY ===");
                }
                else
                {
                    Console.WriteLine("\n=== TEST_FAILED ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST_ERROR: " + ex.ToString());
            }
        }
    }
}

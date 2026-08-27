using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class ImageShapeKeyIsolationTest
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== TESTING IMAGE AS SHAPE & KEYBOARD ISOLATION ===");

            WriterView writer = new WriterView();
            int initialTextLen = writer.PieceTable.Length;
            Console.WriteLine("Initial text length: " + initialTextLen);

            // 1. Test InsertImage creates DocShapeObject
            string dummyImg = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_img.png");
            using (Bitmap bmp = new Bitmap(100, 100))
            {
                using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Red);
                bmp.Save(dummyImg, System.Drawing.Imaging.ImageFormat.Png);
            }

            writer.InsertImage(dummyImg);
            if (writer.ActiveShape == null || writer.ActiveShape.ShapeType != "Image")
            {
                throw new Exception("InsertImage did not create an ActiveShape with ShapeType == 'Image'!");
            }
            Console.WriteLine("Image shape created successfully. ID: " + writer.ActiveShape.Id + ", WrapMode: " + writer.ActiveShape.WrapMode);

            // 2. Test typing is isolated (does not write onto document page when Image is selected)
            int lenBefore = writer.PieceTable.Length;
            writer.HandleChar('A');
            writer.HandleChar('B');
            writer.HandleChar('\b');
            if (writer.PieceTable.Length != lenBefore)
            {
                throw new Exception("Typing leaked into document page while Image was selected!");
            }
            Console.WriteLine("Typing isolation verified: 0 characters leaked to document page.");

            // 3. Test DeleteActiveShape removes image
            writer.DeleteActiveShape();
            if (writer.ActiveShape != null)
            {
                throw new Exception("ActiveShape was not cleared after DeleteActiveShape!");
            }
            Console.WriteLine("Image shape deleted cleanly.");

            // 4. Test Chart isolation
            writer.InsertChart("Column_Clustered");
            if (writer.ActiveChart == null)
            {
                throw new Exception("ActiveChart was not created!");
            }
            Console.WriteLine("Chart created successfully. ID: " + writer.ActiveChart.Id);

            lenBefore = writer.PieceTable.Length;
            writer.HandleChar('X');
            writer.HandleChar('Y');
            writer.HandleKeyDown(Keys.Left);
            if (writer.PieceTable.Length != lenBefore)
            {
                throw new Exception("Typing leaked into document page while Chart was selected!");
            }
            Console.WriteLine("Chart typing isolation verified: 0 characters leaked to document page.");

            writer.DeleteActiveChart();
            if (writer.ActiveChart != null)
            {
                throw new Exception("ActiveChart was not cleared after DeleteActiveChart!");
            }
            Console.WriteLine("Chart deleted cleanly.");

            Console.WriteLine("\n[ALL IMAGE AS SHAPE & KEYBOARD ISOLATION TESTS PASSED 100%!]");
        }
    }
}

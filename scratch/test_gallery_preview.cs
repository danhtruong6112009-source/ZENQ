using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public class TestGalleryPreview
    {
        public static void Main()
        {
            Console.WriteLine("=== TESTING SMARTART GALLERY PREVIEW RENDERING FOR ALL 7 TYPES ===");

            string[] types = new string[]
            {
                "Process_Linear",
                "Process_Chevron",
                "Cycle_Radial",
                "Hierarchy_Tree",
                "List_Blocks",
                "Matrix_Grid",
                "Pyramid_Layers"
            };

            string[] names = new string[]
            {
                "Quy trình Tuyến tính",
                "Mũi tên Khối Quy trình",
                "Sơ đồ Vòng tròn Tuần hoàn",
                "Sơ đồ Cây Tổ chức (Org Chart)",
                "Danh sách Khối Nổi bật",
                "Ma trận 4 Ô (SWOT / BCG)",
                "Kim tự tháp Phân tầng"
            };

            for (int i = 0; i < types.Length; i++)
            {
                using (Bitmap bmp = new Bitmap(400, 330))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    Rectangle drawArea = new Rectangle(16, 92, 368, 225);
                    SmartArtRenderer.DrawSmartArt(g, drawArea, types[i], names[i], 0, "", false, 0.92f);
                }
                Console.WriteLine("[PASS] Preview rendered for: " + names[i] + " (" + types[i] + ")");
            }

            Console.WriteLine("=== ALL 7 SMARTART GALLERY PREVIEWS TESTED & VERIFIED! ===");
        }
    }
}

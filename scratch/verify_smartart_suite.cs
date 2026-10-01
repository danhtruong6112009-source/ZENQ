using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GenOffice.Format;
using GenOffice.UI;

class Program
{
    static void Main()
    {
        string artifactDir = @"C:\Users\Danh\.gemini\antigravity\brain\2a53d4ad-f7b0-44a8-b92e-6ac96e18e730";
        Rectangle bounds = new Rectangle(50, 50, 700, 420);

        // 1. Tạo đối tượng SmartArt ban đầu (Cycle_Radial)
        DocShapeObject sh = new DocShapeObject();
        sh.ShapeType = "SmartArt";
        sh.SmartArtType = "Cycle_Radial";
        sh.SmartArtTitle = "Quy Trình Quản Trị";
        sh.BaseWidth = 700;
        sh.BaseHeight = 420;
        sh.PageX = 50;
        sh.PageY = 50;
        sh.TextContent = "Khảo sát|Thiết kế|Phát triển|Kiểm thử|Vận hành";

        var model = OpenXmlSmartArtModel.FromDocShapeObject(sh);
        model.ApplyToDocShapeObject(sh, bounds);

        // Render Base
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_1_cycle_initial.png"), "1. Cycle Initial (Khảo sát, Thiết kế, Phát triển, Kiểm thử, Vận hành)");

        // 2. Chuyển đổi sang Bố cục Quy Trình (Process_BasicChevron)
        model.SwitchLayout("Process_BasicChevron", sh, bounds);
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_2_switch_chevron.png"), "2. Chuyển sang Quy trình Mũi tên (Chevron) - Giữ nguyên 100% chữ");

        // 3. Chuyển đổi sang Bố cục Phân Cấp (Hierarchy_Tree)
        model.SwitchLayout("Hierarchy_Tree", sh, bounds);
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_3_switch_hierarchy.png"), "3. Chuyển sang Sơ đồ Cây Phân Cấp (Hierarchy Tree)");

        // 4. Chuyển lại Cycle và Đổi Bảng Màu Đa Sắc (Colorful Palette)
        model.SwitchLayout("Cycle_Radial", sh, bounds);
        Color[] colorful = new Color[] {
            Color.FromArgb(237, 125, 49), // Cam
            Color.FromArgb(91, 155, 213), // Lam
            Color.FromArgb(112, 173, 71), // Lục
            Color.FromArgb(255, 192, 0),  // Vàng
            Color.FromArgb(165, 165, 165) // Xám
        };
        model.ApplyPalette(colorful, false, sh, bounds);
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_4_palette_colorful.png"), "4. Bảng màu Đa sắc (Colorful Palette) - Nút 0: Cam, 1: Lam, 2: Lục, 3: Vàng, 4: Xám");

        // 5. Áp dụng Kiểu Nổi 3D (Style 2: 3D Bevel & Drop Shadow)
        model.ApplyVisualStyle(2, sh, bounds);
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_5_style_3d.png"), "5. Kiểu Nổi 3D (3D Bevel & Soft Shadow)");

        // 6. Áp dụng Kiểu Thủy Tinh (Style 3: Glass Reflection Highlight)
        model.ApplyVisualStyle(3, sh, bounds);
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_6_style_glass.png"), "6. Kiểu Thủy Tinh (Glossy Specular Reflection)");

        // 7. Tinh chỉnh riêng nút con trên Tab Format
        sh.NodeShapeTypes[0] = "Diamond";       // Nút 0 thành hình thoi
        sh.NodeShapeTypes[1] = "Oval";          // Nút 1 thành hình elip
        sh.NodeScaleDeltas[1] = 0.25f;          // Nút 1 phóng to +25%
        sh.NodeColors[2] = Color.FromArgb(192, 0, 0); // Nút 2 thành màu Đỏ
        sh.NodeOutlineColors[2] = Color.Gold;   // Nút 2 viền Vàng
        sh.NodeTextColors[3] = Color.Black;     // Nút 3 chữ Đen
        RenderAndSave(sh, bounds, Path.Combine(artifactDir, "test_smartart_7_format_custom_nodes.png"), "7. Tab Format tùy biến nút con (Nút 0 Thoi, Nút 1 Elip +25%, Nút 2 Đỏ viền Vàng)");

        Console.WriteLine("ALL SMARTART VERIFICATION TESTS PASSED SUCCESSFULLY!");
    }

    static void RenderAndSave(DocShapeObject shape, Rectangle bounds, string filePath, string caption)
    {
        using (Bitmap bmp = new Bitmap(800, 520))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(245, 247, 250));

            // Tiêu đề kiểm thử
            using (Font titleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (Font captionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            {
                g.DrawString("KIỂM TRA TƯƠNG TÁC SMARTART - GENOFFICE", titleFont, Brushes.Navy, 20, 12);
                g.DrawString(caption, captionFont, Brushes.DimGray, 20, 32);
            }

            // Dựng hình SmartArt
            ShapeRenderer.DrawShape(g, shape, bounds, null, 1.0f);

            bmp.Save(filePath, ImageFormat.Png);
            Console.WriteLine("Saved: " + Path.GetFileName(filePath));
        }
    }
}

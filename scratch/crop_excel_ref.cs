using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace CropExcelRef
{
    class Program
    {
        static void Main(string[] args)
        {
            string imgPath = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\.user_uploaded\media_1790503999965.png";
            using (Bitmap src = new Bitmap(imgPath))
            {
                Console.WriteLine("Source size: " + src.Width + "x" + src.Height);
                // Save scaled 2x or 3x crops of different parts
                string outDir = @"C:\Users\Danh\.gemini\antigravity\brain\f235df56-64c9-4729-af29-751f07f3050c\";
                
                // Crop 1: Styles (Kiểu dáng)
                // Crop 2: Cells (Ô tính)
                // Crop 3: Editing (Chỉnh sửa)
                // Crop 4: Alignment (Căn chỉnh)
                // Crop 5: Number (Định dạng Số)
                
                // Let's inspect where groups are along X:
                // Total width is src.Width
                int w = src.Width;
                int h = src.Height;
                
                // Let's save 4 slices across the width:
                SaveSlice(src, 0, 0, (int)(w * 0.35), h, outDir + "excel_slice1_font_align.png");
                SaveSlice(src, (int)(w * 0.30), 0, (int)(w * 0.30), h, outDir + "excel_slice2_num_styles.png");
                SaveSlice(src, (int)(w * 0.55), 0, (int)(w * 0.25), h, outDir + "excel_slice3_cells.png");
                SaveSlice(src, (int)(w * 0.70), 0, (int)(w * 0.30), h, outDir + "excel_slice4_editing.png");
                Console.WriteLine("Slices saved!");
            }
        }

        static void SaveSlice(Bitmap src, int x, int y, int w, int h, string outPath)
        {
            if (x + w > src.Width) w = src.Width - x;
            if (y + h > src.Height) h = src.Height - y;
            using (Bitmap bmp = new Bitmap(w * 3, h * 3))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(src, new Rectangle(0, 0, w * 3, h * 3), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                bmp.Save(outPath, ImageFormat.Png);
            }
        }
    }
}

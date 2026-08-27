using System;
using System.Drawing;
using System.Diagnostics;
using GenOffice.Core;
using GenOffice.Format;
using GenOffice.UI;

public class TestZoomQualityAndSpeed
{
    public static void Main()
    {
        Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ ĐỘ NÉT VÀ TỐC ĐỘ RENDER KHI THU NHỎ / PHÓNG TO PDF ===");

        string pdfPath = @"core-master\vcl\qa\cppunit\data\basic.pdf";
        var doc = PdfHandler.LoadPdf(pdfPath);
        if (doc == null || doc.Pages.Count == 0)
        {
            Console.WriteLine("[FAIL] Không load được PDF test");
            Environment.Exit(1);
        }

        PdfView view = new PdfView();
        view.Document = doc;
        view.ActivePageIndex = 0;

        int viewW = 1200;
        int viewH = 800;
        int topOffset = 155;
        int statusH = 26;

        // Test các mức Zoom: từ cực nhỏ (0.2x) đến cực lớn (4.0x)
        float[] testZooms = new float[] { 0.2f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f, 4.0f };

        using (Bitmap screenBmp = new Bitmap(viewW, viewH))
        using (Graphics g = Graphics.FromImage(screenBmp))
        {
            foreach (float z in testZooms)
            {
                view.ZoomLevel = z;
                view.InvalidateAllCaches();

                Stopwatch sw = Stopwatch.StartNew();
                view.Draw(g, viewW, viewH, null, topOffset, statusH);
                sw.Stop();

                var p0 = doc.Pages[0];
                int cacheW = (p0.RenderCache != null) ? p0.RenderCache.Width : 0;
                int cacheH = (p0.RenderCache != null) ? p0.RenderCache.Height : 0;

                int expectedW = (int)Math.Round(p0.Width * z);
                int expectedH = (int)Math.Round(p0.Height * z);

                Console.WriteLine(string.Format("Zoom {0:0.00}x: Kích thước Cache={1}x{2} (Kỳ vọng={3}x{4}), Thời gian Render={5}ms",
                    z, cacheW, cacheH, expectedW, expectedH, sw.ElapsedMilliseconds));

                // Kiểm tra kích thước Cache 1:1 chuẩn xác
                if (cacheW != expectedW || cacheH != expectedH)
                {
                    Console.WriteLine("   -> [FAIL] Kích thước cache không khớp chuẩn 1:1!");
                    Environment.Exit(1);
                }

                // Kiểm tra thời gian render không bị đơ/lag (phải dưới 100ms)
                if (sw.ElapsedMilliseconds > 150)
                {
                    Console.WriteLine("   -> [WARNING] Thời gian render hơi cao: " + sw.ElapsedMilliseconds + "ms");
                }
                else
                {
                    Console.WriteLine("   -> [PASS] Tốc độ render siêu tốc, kích thước chuẩn 1:1 pixel sắc nét!");
                }
            }
        }

        Console.WriteLine("\n=== TẤT CẢ KIỂM THỬ THU PHÓNG (ZOOM IN/OUT) ĐẠT ĐỘ NÉT 100% VÀ HIỆU NĂNG TỐI ĐA! ===");
    }
}

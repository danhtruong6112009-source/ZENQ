using System;
using System.Diagnostics;
using System.Drawing;
using PdfiumViewer;

public class BenchmarkFastColor
{
    public static void Main()
    {
        string pdfPath = @"C:\Users\Danh\Desktop\Toan10_TrongTam_GhiChu_KNTT.pdf";
        var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
        var pSize = doc.PageSizes[0];
        using (var bmp = (Bitmap)doc.Render(0, (int)pSize.Width, (int)pSize.Height, 96, 96, false))
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 61; i++)
            {
                Color bg, fg;
                FastSampleColors(bmp, 50, 100 + i * 10, 400, 15, out bg, out fg);
            }
            sw.Stop();
            Console.WriteLine("FastSampleColors for 61 lines: " + sw.ElapsedMilliseconds + " ms");
        }
    }

    public static void FastSampleColors(Bitmap bmp, int x, int y, int w, int h, out Color bgColor, out Color fgColor)
    {
        bgColor = Color.White;
        fgColor = Color.Black;
        if (bmp == null) return;

        try
        {
            int sx = Math.Max(1, Math.Min(bmp.Width - 2, x + 2));
            int sy = Math.Max(1, Math.Min(bmp.Height - 2, y - 2));
            bgColor = bmp.GetPixel(sx, sy);

            int maxDiff = 0;
            int stepX = Math.Max(5, w / 12);
            int stepY = Math.Max(3, h / 3);
            int endY = Math.Min(bmp.Height - 1, y + h);
            int endX = Math.Min(bmp.Width - 1, x + w);

            for (int py = Math.Max(0, y); py < endY; py += stepY)
            {
                for (int px = Math.Max(0, x); px < endX; px += stepX)
                {
                    Color c = bmp.GetPixel(px, py);
                    int diff = Math.Abs(c.R - bgColor.R) + Math.Abs(c.G - bgColor.G) + Math.Abs(c.B - bgColor.B);
                    if (diff > maxDiff && diff > 100)
                    {
                        maxDiff = diff;
                        fgColor = c;
                    }
                }
            }

            if (fgColor.R > 210 && fgColor.G > 210 && fgColor.B > 210) fgColor = Color.White;
            else if (fgColor.R < 60 && fgColor.G < 60 && fgColor.B < 60) fgColor = Color.Black;
        }
        catch { }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TestHelp
{
    class Program
    {
        static void Main()
        {
            try
            {
                int width = 1200;
                int height = 180;
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Light gradient background
                    using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.FromArgb(238, 242, 255), Color.FromArgb(243, 244, 246), 45f))
                    {
                        g.FillRectangle(bg, 0, 0, width, height);
                    }

                    int marginX = Math.Max(32, (width - 1200) / 2);
                    int contentW = width - marginX * 2;
                    float dockY = 50;

                    // Draw Mock Dock Capsule on Left
                    RectangleF dockRect = new RectangleF(marginX + 84, dockY, 586, 42);
                    using (GraphicsPath dp = CreateRoundedRectanglePath(dockRect, 21f))
                    using (LinearGradientBrush db = new LinearGradientBrush(dockRect, Color.FromArgb(145, 255, 255, 255), Color.FromArgb(95, 240, 245, 255), 90f))
                    using (Pen dpPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f))
                    {
                        g.FillPath(db, dp);
                        g.DrawPath(dpPen, dp);
                    }

                    // Draw Help Button on Right (Opposite side) - Normal state
                    RectangleF helpRect = new RectangleF(marginX + contentW - 42, dockY, 42, 42);
                    using (GraphicsPath hp = CreateRoundedRectanglePath(helpRect, 21f))
                    using (LinearGradientBrush db = new LinearGradientBrush(helpRect, Color.FromArgb(145, 255, 255, 255), Color.FromArgb(95, 240, 245, 255), 90f))
                    using (Pen dpPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f))
                    {
                        g.FillPath(db, hp);
                        g.DrawPath(dpPen, hp);
                    }
                    DrawVectorQuestionIcon(g, helpRect.X + helpRect.Width / 2f, helpRect.Y + helpRect.Height / 2f, 20f, Color.FromArgb(37, 99, 235));

                    // Draw Hover Help Button next to it for comparison
                    RectangleF helpHover = new RectangleF(marginX + contentW - 96, dockY, 42, 42);
                    using (GraphicsPath hp = CreateRoundedRectanglePath(helpHover, 21f))
                    using (LinearGradientBrush hbg = new LinearGradientBrush(helpHover, Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), 90f))
                    using (Pen hBorder = new Pen(Color.FromArgb(147, 197, 253), 1.5f))
                    {
                        g.FillPath(hbg, hp);
                        g.DrawPath(hBorder, hp);
                    }
                    DrawVectorQuestionIcon(g, helpHover.X + helpHover.Width / 2f, helpHover.Y + helpHover.Height / 2f, 20f, Color.White);

                    string outPath = @"C:\Users\Danh\Desktop\genofficepy\scratch\test_help_icon.png";
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("SAVED: " + outPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.ToString());
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawVectorQuestionIcon(Graphics g, float cx, float cy, float size, Color color)
        {
            float s = size / 20f;
            using (Pen p = new Pen(color, 2.2f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (SolidBrush b = new SolidBrush(color))
            {
                g.DrawArc(p, cx - 4.5f * s, cy - 7.5f * s, 9f * s, 8f * s, 180, 200);

                using (GraphicsPath stem = new GraphicsPath())
                {
                    stem.AddBezier(
                        new PointF(cx + 4.5f * s, cy - 3.5f * s),
                        new PointF(cx + 4.0f * s, cy + 0.5f * s),
                        new PointF(cx, cy + 1.0f * s),
                        new PointF(cx, cy + 3.5f * s)
                    );
                    g.DrawPath(p, stem);
                }

                float dotR = 1.4f * s;
                g.FillEllipse(b, cx - dotR, cy + 6.5f * s - dotR, dotR * 2f, dotR * 2f);
            }
        }
    }
}

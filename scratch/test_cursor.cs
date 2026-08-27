using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GenOffice.Tests
{
    public class TestCursor
    {
        [StructLayout(LayoutKind.Sequential)]
        struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

        public static Cursor CreateDownArrowCursor()
        {
            using (Bitmap bmp = new Bitmap(24, 24))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.None;
                    g.Clear(Color.Transparent);

                    // Draw sleek black down arrow with white border (Image 2 style)
                    // Shaft: x = 10..13, y = 2..13
                    // Head: triangle from (6, 13) to (17, 13) to (11, 20)
                    Point[] headPoints = new Point[] {
                        new Point(5, 11),
                        new Point(17, 11),
                        new Point(11, 18)
                    };

                    using (Pen whitePen = new Pen(Color.White, 2f))
                    {
                        g.DrawLine(whitePen, 11, 2, 11, 12);
                        g.DrawPolygon(whitePen, headPoints);
                    }

                    using (Pen blackPen = new Pen(Color.Black, 1.5f))
                    {
                        g.DrawLine(blackPen, 11, 2, 11, 12);
                    }
                    g.FillPolygon(Brushes.Black, headPoints);
                }

                ICONINFO info = new ICONINFO();
                info.fIcon = false;
                info.xHotspot = 11;
                info.yHotspot = 18;
                info.hbmMask = bmp.GetHbitmap();
                info.hbmColor = bmp.GetHbitmap();
                IntPtr hCursor = CreateIconIndirect(ref info);
                return new Cursor(hCursor);
            }
        }

        public static void Main()
        {
            Cursor c = CreateDownArrowCursor();
            Console.WriteLine("Cursor created successfully! Handle = " + c.Handle);
        }
    }
}

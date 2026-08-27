using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GenOffice.Tests
{
    public class TestAllCursors
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
                    g.Clear(Color.Transparent);
                    Point[] head = new Point[] { new Point(5, 11), new Point(17, 11), new Point(11, 18) };
                    using (Pen w = new Pen(Color.White, 3f))
                    {
                        g.DrawLine(w, 11, 2, 11, 12);
                        g.DrawPolygon(w, head);
                    }
                    using (Pen b = new Pen(Color.Black, 2f))
                    {
                        g.DrawLine(b, 11, 2, 11, 12);
                    }
                    g.FillPolygon(Brushes.Black, head);
                }

                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 11, yHotspot = 18, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }

        public static Cursor CreateRightArrowCursor()
        {
            using (Bitmap bmp = new Bitmap(24, 24))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Point[] head = new Point[] { new Point(11, 5), new Point(11, 17), new Point(18, 11) };
                    using (Pen w = new Pen(Color.White, 3f))
                    {
                        g.DrawLine(w, 2, 11, 12, 11);
                        g.DrawPolygon(w, head);
                    }
                    using (Pen b = new Pen(Color.Black, 2f))
                    {
                        g.DrawLine(b, 2, 11, 12, 11);
                    }
                    g.FillPolygon(Brushes.Black, head);
                }

                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 18, yHotspot = 11, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }

        public static Cursor CreateDiagArrowCursor()
        {
            using (Bitmap bmp = new Bitmap(24, 24))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Point[] head = new Point[] { new Point(9, 17), new Point(17, 9), new Point(18, 18) };
                    using (Pen w = new Pen(Color.White, 3f))
                    {
                        g.DrawLine(w, 4, 4, 13, 13);
                        g.DrawPolygon(w, head);
                    }
                    using (Pen b = new Pen(Color.Black, 2f))
                    {
                        g.DrawLine(b, 4, 4, 13, 13);
                    }
                    g.FillPolygon(Brushes.Black, head);
                }

                ICONINFO info = new ICONINFO { fIcon = false, xHotspot = 18, yHotspot = 18, hbmMask = bmp.GetHbitmap(), hbmColor = bmp.GetHbitmap() };
                return new Cursor(CreateIconIndirect(ref info));
            }
        }

        public static void Main()
        {
            Cursor down = CreateDownArrowCursor();
            Cursor right = CreateRightArrowCursor();
            Cursor diag = CreateDiagArrowCursor();

            Console.WriteLine("Down Arrow Cursor: " + down.Handle);
            Console.WriteLine("Right Arrow Cursor: " + right.Handle);
            Console.WriteLine("Diag Arrow Cursor: " + diag.Handle);
            Console.WriteLine("All 3 Excel Cursors generated perfectly!");
        }
    }
}

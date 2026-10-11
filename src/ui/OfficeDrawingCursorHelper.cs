using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Helper to generate high-precision, high-contrast vector cursors for Drawing tools (Pen, Highlighter, Eraser)
    /// replacing the standard generic '+' crosshair with beautiful, authentic tool icons.
    /// </summary>
    public static class OfficeDrawingCursorHelper
    {
        private static readonly Dictionary<int, Cursor> _penCache = new Dictionary<int, Cursor>();
        private static readonly Dictionary<int, Cursor> _highlighterCache = new Dictionary<int, Cursor>();
        private static Cursor _eraserCursor = null;

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetIconInfo(IntPtr hIcon, ref ICONINFO pIconInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO pIconInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private static Cursor CreateCursorFromBitmap(Bitmap bmp, int hotX, int hotY)
        {
            IntPtr hIcon = bmp.GetHicon();
            ICONINFO info = new ICONINFO();
            if (GetIconInfo(hIcon, ref info))
            {
                info.xHotspot = hotX;
                info.yHotspot = hotY;
                info.fIcon = false; // False = Cursor (not icon)
                IntPtr hCursor = CreateIconIndirect(ref info);

                if (info.hbmColor != IntPtr.Zero) DeleteObject(info.hbmColor);
                if (info.hbmMask != IntPtr.Zero) DeleteObject(info.hbmMask);
                DestroyIcon(hIcon);

                if (hCursor != IntPtr.Zero)
                    return new Cursor(hCursor);
            }
            return new Cursor(hIcon);
        }

        /// <summary>
        /// Gets a precision stylus/pen cursor matching the active ink color. Hotspot is at the sharp nib tip (1, 30).
        /// </summary>
        public static Cursor GetPenCursor(Color inkColor)
        {
            int key = inkColor.ToArgb();
            if (_penCache.TryGetValue(key, out var cached)) return cached;

            using (Bitmap bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                PointF tip = new PointF(1f, 30f);
                PointF w1 = new PointF(4.5f, 26.5f);
                PointF w2 = new PointF(7.5f, 29.5f);

                PointF b1 = new PointF(10f, 21f);
                PointF b2 = new PointF(15f, 26f);

                PointF f1 = new PointF(22f, 9f);
                PointF f2 = new PointF(27f, 14f);

                PointF top1 = new PointF(26f, 5f);
                PointF top2 = new PointF(29f, 8f);

                PointF topTip = new PointF(29f, 2f);
                PointF topTip2 = new PointF(31f, 4f);

                PointF[] contour = new PointF[] { tip, b1, f1, top1, topTip, topTip2, top2, f2, b2, tip };

                // 1. High contrast outer white glow/halo for visibility on dark/light documents
                using (Pen whitePen = new Pen(Color.FromArgb(235, 255, 255, 255), 3.5f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawPolygon(whitePen, contour);
                }

                // 2. Stylus End Cap (silver/metallic)
                using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    g.FillPolygon(capBrush, new PointF[] { top1, topTip, topTip2, top2 });
                }

                // 3. Stylus Ferrule band
                using (SolidBrush bandBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.FillPolygon(bandBrush, new PointF[] { f1, top1, top2, f2 });
                }

                // 4. Stylus Pen Body (filled with active ink color)
                using (SolidBrush bodyBrush = new SolidBrush(inkColor))
                {
                    g.FillPolygon(bodyBrush, new PointF[] { b1, f1, f2, b2 });
                }
                // Subtle highlight shine on pen body
                using (SolidBrush shineBrush = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                {
                    PointF m1 = new PointF((b1.X + b2.X) * 0.5f, (b1.Y + b2.Y) * 0.5f);
                    PointF m2 = new PointF((f1.X + f2.X) * 0.5f, (f1.Y + f2.Y) * 0.5f);
                    g.FillPolygon(shineBrush, new PointF[] { b1, f1, m2, m1 });
                }

                // 5. Stylus Nib Cone
                using (SolidBrush coneBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillPolygon(coneBrush, new PointF[] { w1, b1, b2, w2, tip });
                }

                // 6. Fine Nib Ink Point
                using (SolidBrush nibBrush = new SolidBrush(inkColor.A < 200 ? Color.FromArgb(30, 41, 59) : inkColor))
                {
                    g.FillPolygon(nibBrush, new PointF[] { tip, w1, w2 });
                }

                // 7. Crisp dark boundary outline
                using (Pen darkPen = new Pen(Color.FromArgb(30, 41, 59), 1.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawPolygon(darkPen, contour);
                    g.DrawLine(darkPen, b1, b2);
                    g.DrawLine(darkPen, f1, f2);
                }

                Cursor c = CreateCursorFromBitmap(bmp, 1, 30);
                _penCache[key] = c;
                return c;
            }
        }

        /// <summary>
        /// Gets an authentic chisel-tip highlighter cursor matching the active fluorescent color. Hotspot is at the chisel writing edge (2, 29).
        /// </summary>
        public static Cursor GetHighlighterCursor(Color inkColor)
        {
            int key = inkColor.ToArgb();
            if (_highlighterCache.TryGetValue(key, out var cached)) return cached;

            using (Bitmap bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Chisel tip angled cut
                PointF t1 = new PointF(1.5f, 29.5f);
                PointF t2 = new PointF(7.0f, 25.5f);
                PointF t3 = new PointF(9.5f, 29.0f);
                PointF t4 = new PointF(4.0f, 31.0f);

                // Collar
                PointF c1 = new PointF(8f, 21f);
                PointF c2 = new PointF(15f, 27f);

                // Marker Barrel
                PointF b1 = new PointF(19f, 8f);
                PointF b2 = new PointF(27f, 15f);

                // Cap end
                PointF top1 = new PointF(23f, 3f);
                PointF top2 = new PointF(30f, 10f);

                PointF[] contour = new PointF[] { t1, t2, c1, b1, top1, top2, b2, c2, t3, t4, t1 };

                // 1. High contrast white glow
                using (Pen whitePen = new Pen(Color.FromArgb(235, 255, 255, 255), 3.5f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawPolygon(whitePen, contour);
                }

                // 2. Chisel felt nib (vibrant highlighter color)
                using (SolidBrush tipBrush = new SolidBrush(inkColor))
                {
                    g.FillPolygon(tipBrush, new PointF[] { t1, t2, c1, c2, t3, t4 });
                }

                // 3. Collar (dark grey plastic)
                using (SolidBrush colBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    PointF cm1 = new PointF((c1.X + b1.X) * 0.35f + c1.X * 0.65f, (c1.Y + b1.Y) * 0.35f + c1.Y * 0.65f);
                    PointF cm2 = new PointF((c2.X + b2.X) * 0.35f + c2.X * 0.65f, (c2.Y + b2.Y) * 0.35f + c2.Y * 0.65f);
                    g.FillPolygon(colBrush, new PointF[] { c1, cm1, cm2, c2 });
                }

                // 4. Marker body
                using (SolidBrush bodyBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPolygon(bodyBrush, new PointF[] { c1, b1, b2, c2 });
                }
                // Marker color band
                using (SolidBrush bandBrush = new SolidBrush(inkColor))
                {
                    PointF m1 = new PointF((c1.X + b1.X) * 0.5f, (c1.Y + b1.Y) * 0.5f);
                    PointF m2 = new PointF((c2.X + b2.X) * 0.5f, (c2.Y + b2.Y) * 0.5f);
                    g.FillPolygon(bandBrush, new PointF[] { m1, b1, b2, m2 });
                }

                // 5. Cap top
                using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.FillPolygon(capBrush, new PointF[] { b1, top1, top2, b2 });
                }

                // 6. Dark outline
                using (Pen darkPen = new Pen(Color.FromArgb(15, 23, 42), 1.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawPolygon(darkPen, contour);
                    g.DrawLine(darkPen, t1, t2);
                    g.DrawLine(darkPen, c1, c2);
                    g.DrawLine(darkPen, b1, b2);
                }

                Cursor c = CreateCursorFromBitmap(bmp, 2, 29);
                _highlighterCache[key] = c;
                return c;
            }
        }

        /// <summary>
        /// Gets an authentic rubber block eraser cursor. Hotspot is at the bottom-left erasing corner (4, 28).
        /// </summary>
        public static Cursor GetEraserCursor()
        {
            if (_eraserCursor != null) return _eraserCursor;

            using (Bitmap bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Eraser bottom active rubber face
                PointF p1 = new PointF(4f, 28f);
                PointF p2 = new PointF(13f, 29f);
                PointF p3 = new PointF(21f, 20f);
                PointF p4 = new PointF(12f, 19f);

                // Top rubber end
                PointF top1 = new PointF(8f, 12f);
                PointF top2 = new PointF(17f, 13f);
                PointF top3 = new PointF(25f, 4f);
                PointF top4 = new PointF(16f, 3f);

                PointF[] contour = new PointF[] { p1, p2, p3, top3, top4, top1, p1 };

                // 1. White glow
                using (Pen whitePen = new Pen(Color.FromArgb(235, 255, 255, 255), 3.5f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawPolygon(whitePen, contour);
                }

                // 2. White clean rubber end
                using (SolidBrush rubBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPolygon(rubBrush, new PointF[] { p1, p2, p3, p4 });
                }

                // 3. Eraser sleeve (ZenQ Royal Blue brand sleeve)
                using (SolidBrush sleeveBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    g.FillPolygon(sleeveBrush, new PointF[] { p4, p3, top3, top4 });
                }

                // 4. Bevel side (darker contrast)
                using (SolidBrush bevelBrush = new SolidBrush(Color.FromArgb(191, 219, 254)))
                {
                    g.FillPolygon(bevelBrush, new PointF[] { p1, p4, top4, top1 });
                }

                // 5. Crisp dark outline
                using (Pen darkPen = new Pen(Color.FromArgb(15, 23, 42), 1.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawPolygon(darkPen, contour);
                    g.DrawLine(darkPen, p4, p3);
                    g.DrawLine(darkPen, top4, top1);
                }

                _eraserCursor = CreateCursorFromBitmap(bmp, 4, 28);
                return _eraserCursor;
            }
        }
    }
}

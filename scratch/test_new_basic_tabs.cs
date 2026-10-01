using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GenOffice.UI;

class TestNewBasicTabs
{
    [STAThread]
    static void Main()
    {
        int w = 1200, ribbonTop = 36;
        int homeTabWidth = 110;
        int docAreaX = homeTabWidth;
        int tabWidth = 160;

        using (Bitmap bmp = new Bitmap(w, 200))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.FromArgb(248, 250, 252));

            // Tab bar base
            Rectangle topBarRect = new Rectangle(0, 0, w, ribbonTop);
            using (SolidBrush tabStripBg = new SolidBrush(Color.FromArgb(241, 244, 249)))
            {
                g.FillRectangle(tabStripBg, topBarRect);
            }
            using (Pen btmPen = new Pen(Color.FromArgb(218, 224, 233), 1.0f))
            {
                g.DrawLine(btmPen, 0, ribbonTop - 0.5f, w, ribbonTop - 0.5f);
            }

            // Ribbon header under tab bar
            int headerHeight = 28;
            using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen headerBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRectangle(headerBrush, 0, ribbonTop, w, headerHeight);
                g.DrawLine(headerBorder, 0, ribbonTop + headerHeight, w, ribbonTop + headerHeight);
            }

            // Tabs definition
            StringFormat ellipsisFormat = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
                LineAlignment = StringAlignment.Center
            };

            using (Font tabFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (Font tabActiveFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Pen dividerPen = new Pen(Color.FromArgb(218, 224, 233), 1.0f))
            {
                // Tab 0: Trang chính (Inactive in this test)
                Rectangle homeRect = new Rectangle(0, 0, homeTabWidth, ribbonTop);
                using (SolidBrush idleBg = new SolidBrush(Color.FromArgb(238, 242, 247)))
                {
                    g.FillRectangle(idleBg, homeRect);
                }
                g.DrawLine(dividerPen, homeRect.Right - 0.5f, 0, homeRect.Right - 0.5f, ribbonTop);
                g.DrawLine(dividerPen, homeRect.X, ribbonTop - 0.5f, homeRect.Right, ribbonTop - 0.5f);

                // Draw home icon
                DrawHomeIcon(g, homeRect.X + 12, (ribbonTop - 16) / 2, 16, Color.FromArgb(148, 163, 184));
                Rectangle homeTextRect = new Rectangle(homeRect.X + 32, 0, homeRect.Width - 36, ribbonTop);
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    g.DrawString("Trang chính", tabFont, textBrush, homeTextRect, ellipsisFormat);
                }

                // Tab 1: Active Writer Tab
                int x1 = docAreaX;
                Rectangle tab1Rect = new Rectangle(x1, 0, tabWidth, ribbonTop);
                using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillRectangle(activeBg, tab1Rect.X, tab1Rect.Y, tab1Rect.Width, tab1Rect.Height + 1);
                }
                using (Pen accentPen = new Pen(Color.FromArgb(37, 99, 235), 2.5f))
                {
                    g.DrawLine(accentPen, tab1Rect.X, 1.25f, tab1Rect.Right, 1.25f);
                }
                g.DrawLine(dividerPen, tab1Rect.X - 0.5f, 0, tab1Rect.X - 0.5f, ribbonTop);
                g.DrawLine(dividerPen, tab1Rect.Right - 0.5f, 0, tab1Rect.Right - 0.5f, ribbonTop);

                DrawDocIcon(g, tab1Rect.X + 10, (ribbonTop - 16) / 2, 16, Color.FromArgb(37, 99, 235));
                Rectangle t1TextRect = new Rectangle(tab1Rect.X + 30, 0, tab1Rect.Width - 54, ribbonTop);
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString("Báo cáo tháng 9.docx", tabActiveFont, textBrush, t1TextRect, ellipsisFormat);
                }
                // Close button active idle
                Rectangle c1Rect = new Rectangle(tab1Rect.Right - 22, (ribbonTop - 16) / 2, 16, 16);
                DrawCloseCross(g, c1Rect.X + 8, c1Rect.Y + 8, 8, Color.FromArgb(100, 116, 139));

                // Tab 2: Inactive Calc Tab (with Hover on Close Button to demonstrate NO RED)
                int x2 = x1 + tabWidth;
                Rectangle tab2Rect = new Rectangle(x2, 0, tabWidth, ribbonTop);
                using (SolidBrush idleBg = new SolidBrush(Color.FromArgb(238, 242, 247)))
                {
                    g.FillRectangle(idleBg, tab2Rect);
                }
                g.DrawLine(dividerPen, tab2Rect.Right - 0.5f, 0, tab2Rect.Right - 0.5f, ribbonTop);
                g.DrawLine(dividerPen, tab2Rect.X, ribbonTop - 0.5f, tab2Rect.Right, ribbonTop - 0.5f);

                DrawDocIcon(g, tab2Rect.X + 10, (ribbonTop - 16) / 2, 16, Color.FromArgb(5, 150, 105));
                Rectangle t2TextRect = new Rectangle(tab2Rect.X + 30, 0, tab2Rect.Width - 54, ribbonTop);
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    g.DrawString("Bảng chấm công.xlsx", tabFont, textBrush, t2TextRect, ellipsisFormat);
                }
                // Hovered Close button: subtle gray box, dark charcoal 'x', NO RED
                Rectangle c2Rect = new Rectangle(tab2Rect.Right - 22, (ribbonTop - 16) / 2, 16, 16);
                using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(220, 226, 235)))
                {
                    g.FillRectangle(hoverBg, c2Rect);
                }
                DrawCloseCross(g, c2Rect.X + 8, c2Rect.Y + 8, 8, Color.FromArgb(30, 41, 59));

                // Tab 3: Inactive Slides Tab
                int x3 = x2 + tabWidth;
                Rectangle tab3Rect = new Rectangle(x3, 0, tabWidth, ribbonTop);
                using (SolidBrush idleBg = new SolidBrush(Color.FromArgb(238, 242, 247)))
                {
                    g.FillRectangle(idleBg, tab3Rect);
                }
                g.DrawLine(dividerPen, tab3Rect.Right - 0.5f, 0, tab3Rect.Right - 0.5f, ribbonTop);
                g.DrawLine(dividerPen, tab3Rect.X, ribbonTop - 0.5f, tab3Rect.Right, ribbonTop - 0.5f);

                DrawDocIcon(g, tab3Rect.X + 10, (ribbonTop - 16) / 2, 16, Color.FromArgb(234, 88, 12));
                Rectangle t3TextRect = new Rectangle(tab3Rect.X + 30, 0, tab3Rect.Width - 54, ribbonTop);
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    g.DrawString("Kế hoạch.pptx", tabFont, textBrush, t3TextRect, ellipsisFormat);
                }
                Rectangle c3Rect = new Rectangle(tab3Rect.Right - 22, (ribbonTop - 16) / 2, 16, 16);
                DrawCloseCross(g, c3Rect.X + 8, c3Rect.Y + 8, 8, Color.FromArgb(148, 163, 184));

                // Add button (+)
                int xAdd = x3 + tabWidth;
                Rectangle addRect = new Rectangle(xAdd + 4, (ribbonTop - 22) / 2, 22, 22);
                DrawPlus(g, addRect.X + 11, addRect.Y + 11, 10, Color.FromArgb(100, 116, 139));
            }

            bmp.Save("scratch\\test_prototype_tabs.png", ImageFormat.Png);
        }
        Console.WriteLine("Saved: scratch\\test_prototype_tabs.png");
    }

    static void DrawCloseCross(Graphics g, float cx, float cy, float size, Color col)
    {
        float h = size / 2f;
        using (Pen p = new Pen(col, 1.0f))
        {
            g.DrawLine(p, cx - h, cy - h, cx + h, cy + h);
            g.DrawLine(p, cx + h, cy - h, cx - h, cy + h);
        }
    }

    static void DrawPlus(Graphics g, float cx, float cy, float size, Color col)
    {
        float h = size / 2f;
        using (Pen p = new Pen(col, 1.0f))
        {
            g.DrawLine(p, cx - h, cy, cx + h, cy);
            g.DrawLine(p, cx, cy - h, cx, cy + h);
        }
    }

    static void DrawHomeIcon(Graphics g, float x, float y, float size, Color col)
    {
        using (Pen p = new Pen(col, 1.3f))
        {
            PointF[] roof = new PointF[] { new PointF(x, y + 7), new PointF(x + 8, y + 1), new PointF(x + 16, y + 7) };
            g.DrawLines(p, roof);
            g.DrawRectangle(p, x + 2.5f, y + 7, 11f, 8f);
        }
    }

    static void DrawDocIcon(Graphics g, float x, float y, float size, Color col)
    {
        using (SolidBrush b = new SolidBrush(col))
        {
            g.FillRectangle(b, x + 2, y + 1, 12, 14);
        }
        using (SolidBrush b = new SolidBrush(Color.White))
        {
            g.FillRectangle(b, x + 4, y + 5, 8, 1.5f);
            g.FillRectangle(b, x + 4, y + 8, 8, 1.5f);
            g.FillRectangle(b, x + 4, y + 11, 5, 1.5f);
        }
    }
}

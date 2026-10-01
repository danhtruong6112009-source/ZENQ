using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class TestWordIconsPerfect
{
    public static void Main()
    {
        int width = 900;
        int height = 160;
        using (Bitmap bmp = new Bitmap(width, height))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(249, 250, 251)); // Ribbon background
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Render Font group row 2
            int fx = 30;
            // B, I, U, ab, x2, x^2
            DrawInteractiveButton(g, new Rectangle(fx, 96, 22, 22), false);
            DrawBold(g, fx, 96);
            fx += 24;

            DrawInteractiveButton(g, new Rectangle(fx, 96, 22, 22), false);
            DrawItalic(g, fx, 96);
            fx += 24;

            DrawInteractiveButton(g, new Rectangle(fx, 96, 26, 22), false);
            DrawUnderline(g, fx, 96);
            fx += 28;

            DrawInteractiveButton(g, new Rectangle(fx, 96, 22, 22), false);
            DrawStrikethrough(g, fx, 96);
            fx += 24;

            DrawInteractiveButton(g, new Rectangle(fx, 96, 22, 22), false);
            DrawSubscript(g, fx, 96);
            fx += 24;

            DrawInteractiveButton(g, new Rectangle(fx, 96, 22, 22), false);
            DrawSuperscript(g, fx, 96);
            fx += 24;

            // Text Effects A▾
            Rectangle fxRect = new Rectangle(fx, 96, 28, 22);
            DrawInteractiveButton(g, fxRect, false);
            DrawWordTextEffects(g, fx, 96);
            fx += 30;

            // Highlighter
            Rectangle hlRect = new Rectangle(fx, 96, 28, 22);
            DrawInteractiveButton(g, hlRect, false);
            DrawWordHighlighter(g, fx, 96, Color.FromArgb(255, 235, 59));
            fx += 30;

            // Font Color
            Rectangle fcRect = new Rectangle(fx, 96, 28, 22);
            DrawInteractiveButton(g, fcRect, false);
            DrawWordFontColor(g, fx, 96, Color.FromArgb(220, 38, 38));
            fx += 35;

            // Divider between Font & Paragraph
            g.DrawLine(new Pen(Color.FromArgb(215, 218, 223), 1f), fx, 68, fx, 122);
            fx += 10;

            // Paragraph Top Row:
            int px = fx;
            DrawInteractiveButton(g, new Rectangle(px, 68, 28, 22), false);
            DrawWordBullets(g, px, 68);
            px += 30;

            DrawInteractiveButton(g, new Rectangle(px, 68, 28, 22), false);
            DrawWordNumbering(g, px, 68);
            px += 30;

            DrawInteractiveButton(g, new Rectangle(px, 68, 28, 22), false);
            DrawWordMultilevel(g, px, 68);
            px += 30;

            // Subgroup divider
            g.DrawLine(new Pen(Color.FromArgb(225, 228, 233), 1f), px + 2, 70, px + 2, 88);
            px += 7;

            // Dec Indent
            DrawInteractiveButton(g, new Rectangle(px, 68, 22, 22), false);
            DrawWordOutdent(g, px, 68);
            px += 24;

            // Inc Indent
            DrawInteractiveButton(g, new Rectangle(px, 68, 22, 22), false);
            DrawWordIndent(g, px, 68);
            px += 24;

            // Subgroup divider
            g.DrawLine(new Pen(Color.FromArgb(225, 228, 233), 1f), px + 2, 70, px + 2, 88);
            px += 7;

            // Sort A-Z
            DrawInteractiveButton(g, new Rectangle(px, 68, 22, 22), false);
            DrawWordSort(g, px, 68);
            px += 24;

            // Pilcrow ¶
            DrawInteractiveButton(g, new Rectangle(px, 68, 22, 22), false);
            DrawWordPilcrow(g, px, 68);

            // Paragraph Bottom Row:
            px = fx;
            for (int a = 0; a < 4; a++)
            {
                Rectangle aRect = new Rectangle(px, 96, 22, 22);
                bool isActive = (a == 0); // Align Left active
                DrawInteractiveButton(g, aRect, isActive);
                DrawWordAlignment(g, px, 96, a);
                px += 23;
            }

            // Subgroup divider
            g.DrawLine(new Pen(Color.FromArgb(225, 228, 233), 1f), px + 2, 98, px + 2, 116);
            px += 7;

            // Line Spacing
            Rectangle lsRect = new Rectangle(px, 96, 28, 22);
            DrawInteractiveButton(g, lsRect, false);
            DrawWordLineSpacing(g, px, 96);
            px += 30;

            // Subgroup divider
            g.DrawLine(new Pen(Color.FromArgb(225, 228, 233), 1f), px + 2, 98, px + 2, 116);
            px += 7;

            // Shading (Paint Bucket)
            Rectangle shadeRect = new Rectangle(px, 96, 28, 22);
            DrawInteractiveButton(g, shadeRect, false);
            DrawWordPaintBucket(g, px, 96, Color.Transparent);
            px += 30;

            // Borders
            Rectangle bdrRect = new Rectangle(px, 96, 28, 22);
            DrawInteractiveButton(g, bdrRect, true); // Active borders
            DrawWordBorders(g, px, 96);

            bmp.Save("scratch\\test_word_icons_perfect.png", ImageFormat.Png);
        }

        // 3x magnified
        using (Bitmap src = new Bitmap("scratch\\test_word_icons_perfect.png"))
        using (Bitmap mag = new Bitmap(src.Width * 3, src.Height * 3))
        using (Graphics g = Graphics.FromImage(mag))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(src, 0, 0, mag.Width, mag.Height);
            mag.Save("scratch\\test_word_icons_perfect_3x.png", ImageFormat.Png);
        }

        Console.WriteLine("Test completed successfully.");
    }

    private static void DrawInteractiveButton(Graphics g, Rectangle rect, bool isActive)
    {
        if (isActive)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(226, 228, 232)))
                g.FillRectangle(b, rect);
            using (Pen p = new Pen(Color.FromArgb(175, 178, 184), 1.0f))
                g.DrawRectangle(p, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }
    }

    private static void DrawDropdownChevron(Graphics g, float x, float y, Color color)
    {
        using (Pen p = new Pen(color, 1.1f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            g.DrawLine(p, x, y, x + 2.5f, y + 2.5f);
            g.DrawLine(p, x + 2.5f, y + 2.5f, x + 5f, y);
        }
    }

    private static void DrawBold(Graphics g, float x, float y)
    {
        using (Font f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        {
            g.DrawString("B", f, b, x + 4, y + 2);
        }
    }

    private static void DrawItalic(Graphics g, float x, float y)
    {
        using (Font f = new Font("Georgia", 9.5f, FontStyle.Italic | FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        {
            g.DrawString("I", f, b, x + 5, y + 2);
        }
    }

    private static void DrawUnderline(Graphics g, float x, float y)
    {
        using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Regular))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        using (Pen p = new Pen(Color.FromArgb(38, 38, 42), 1.15f))
        {
            g.DrawString("U", f, b, x + 2, y + 1);
            g.DrawLine(p, x + 2, y + 15, x + 12, y + 15);
            DrawDropdownChevron(g, x + 15, y + 9, Color.FromArgb(80, 80, 80));
        }
    }

    private static void DrawStrikethrough(Graphics g, float x, float y)
    {
        using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        using (Pen p = new Pen(Color.FromArgb(38, 38, 42), 1.15f))
        {
            g.DrawString("ab", f, b, x + 1, y + 2);
            g.DrawLine(p, x + 1, y + 9, x + 16, y + 9);
        }
    }

    private static void DrawSubscript(Graphics g, float x, float y)
    {
        using (Font fX = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        using (Font f2 = new Font("Segoe UI", 6.0f, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(16, 110, 190)))
        {
            g.DrawString("x", fX, b, x + 1, y + 2);
            g.DrawString("2", f2, bBlue, x + 8, y + 8);
        }
    }

    private static void DrawSuperscript(Graphics g, float x, float y)
    {
        using (Font fX = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        using (Font f2 = new Font("Segoe UI", 6.0f, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(16, 110, 190)))
        {
            g.DrawString("x", fX, b, x + 1, y + 4);
            g.DrawString("2", f2, bBlue, x + 8, y + 1);
        }
    }

    private static void DrawWordTextEffects(Graphics g, float x, float y)
    {
        using (Font f = new Font("Segoe UI", 10.0f, FontStyle.Bold))
        using (SolidBrush bGlow = new SolidBrush(Color.FromArgb(170, 205, 245)))
        using (SolidBrush bWhite = new SolidBrush(Color.White))
        using (Pen pBlue = new Pen(Color.FromArgb(16, 110, 190), 1.1f))
        {
            // Soft sky blue outer glow
            g.DrawString("A", f, bGlow, x + 3.0f, y + 1.5f);
            // Crisp white body
            g.DrawString("A", f, bWhite, x + 2.0f, y + 1.0f);
            // Sharp royal blue outline
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddString("A", f.FontFamily, (int)FontStyle.Bold, 13.5f, new PointF(x + 1.5f, y + 0.5f), StringFormat.GenericDefault);
                g.DrawPath(pBlue, gp);
            }
            DrawDropdownChevron(g, x + 16, y + 9, Color.FromArgb(80, 80, 80));
        }
    }

    private static void DrawWordHighlighter(Graphics g, float x, float y, Color col)
    {
        using (Pen pOutline = new Pen(Color.FromArgb(38, 38, 42), 1.0f) { LineJoin = LineJoin.Miter })
        using (SolidBrush bBody = new SolidBrush(Color.FromArgb(245, 245, 245)))
        using (SolidBrush bCollar = new SolidBrush(Color.FromArgb(160, 160, 160)))
        using (SolidBrush bNib = new SolidBrush(col))
        using (SolidBrush bBar = new SolidBrush(col))
        {
            PointF[] barrel = new PointF[] {
                new PointF(x + 5.5f, y + 6.5f),
                new PointF(x + 10.5f, y + 1.5f),
                new PointF(x + 13.5f, y + 4.5f),
                new PointF(x + 8.5f, y + 9.5f)
            };
            g.FillPolygon(bBody, barrel);
            g.DrawPolygon(pOutline, barrel);

            PointF[] collar = new PointF[] {
                new PointF(x + 4.5f, y + 7.5f),
                new PointF(x + 5.5f, y + 6.5f),
                new PointF(x + 8.5f, y + 9.5f),
                new PointF(x + 7.5f, y + 10.5f)
            };
            g.FillPolygon(bCollar, collar);
            g.DrawPolygon(pOutline, collar);

            PointF[] nib = new PointF[] {
                new PointF(x + 2.0f, y + 10.0f),
                new PointF(x + 4.5f, y + 7.5f),
                new PointF(x + 7.5f, y + 10.5f),
                new PointF(x + 4.0f, y + 12.5f)
            };
            g.FillPolygon(bNib, nib);
            g.DrawPolygon(pOutline, nib);

            DrawDropdownChevron(g, x + 17, y + 8, Color.FromArgb(80, 80, 80));
            g.FillRectangle(bBar, x + 1, y + 15, 14, 3);
        }
    }

    private static void DrawWordFontColor(Graphics g, float x, float y, Color col)
    {
        using (Font f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        using (SolidBrush bBar = new SolidBrush(col))
        {
            g.DrawString("A", f, b, x + 3, y + 1);
            DrawDropdownChevron(g, x + 17, y + 8, Color.FromArgb(80, 80, 80));
            g.FillRectangle(bBar, x + 1, y + 15, 14, 3);
        }
    }

    private static void DrawWordBullets(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (SolidBrush bDot = new SolidBrush(Color.FromArgb(16, 110, 190)))
        {
            float[] ys = { y + 3.5f, y + 7.5f, y + 11.5f };
            foreach (float dy in ys)
            {
                g.FillRectangle(bDot, x + 1f, dy - 1f, 2.5f, 2.5f);
                g.DrawLine(p, x + 5.5f, dy, x + 14.5f, dy);
            }
            DrawDropdownChevron(g, x + 17, y + 8, Color.FromArgb(80, 80, 80));
        }
    }

    private static void DrawWordNumbering(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (Pen pNum = new Pen(Color.FromArgb(16, 110, 190), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            // "1"
            g.DrawLine(pNum, x + 2.5f, y + 2.0f, x + 2.5f, y + 5.5f);
            g.DrawLine(pNum, x + 1.2f, y + 3.0f, x + 2.5f, y + 2.0f);
            g.DrawLine(p, x + 6.0f, y + 3.8f, x + 14.5f, y + 3.8f);

            // "2"
            g.DrawLine(pNum, x + 1.0f, y + 7.0f, x + 3.5f, y + 7.0f);
            g.DrawLine(pNum, x + 3.5f, y + 7.0f, x + 1.0f, y + 10.0f);
            g.DrawLine(pNum, x + 1.0f, y + 10.0f, x + 3.5f, y + 10.0f);
            g.DrawLine(p, x + 6.0f, y + 8.5f, x + 14.5f, y + 8.5f);

            // "3"
            g.DrawLine(pNum, x + 1.0f, y + 11.5f, x + 3.5f, y + 11.5f);
            g.DrawLine(pNum, x + 2.0f, y + 13.0f, x + 3.5f, y + 13.0f);
            g.DrawLine(pNum, x + 1.0f, y + 14.5f, x + 3.5f, y + 14.5f);
            g.DrawLine(pNum, x + 3.5f, y + 11.5f, x + 3.5f, y + 14.5f);
            g.DrawLine(p, x + 6.0f, y + 13.0f, x + 14.5f, y + 13.0f);

            DrawDropdownChevron(g, x + 17, y + 8, Color.FromArgb(80, 80, 80));
        }
    }

    private static void DrawWordMultilevel(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (Pen pNum = new Pen(Color.FromArgb(16, 110, 190), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            // Level 1: "1" + line
            g.DrawLine(pNum, x + 2.0f, y + 2.0f, x + 2.0f, y + 5.0f);
            g.DrawLine(pNum, x + 1.0f, y + 3.0f, x + 2.0f, y + 2.0f);
            g.DrawLine(p, x + 5.0f, y + 3.5f, x + 14.5f, y + 3.5f);

            // Level 2: "a" + line (indented)
            g.DrawLine(pNum, x + 4.5f, y + 7.5f, x + 6.5f, y + 7.5f);
            g.DrawLine(pNum, x + 6.5f, y + 7.5f, x + 6.5f, y + 10.0f);
            g.DrawLine(p, x + 8.5f, y + 8.5f, x + 14.5f, y + 8.5f);

            // Level 3: "i" + line (further indented)
            g.DrawLine(pNum, x + 7.5f, y + 12.0f, x + 7.5f, y + 14.5f);
            g.FillRectangle(new SolidBrush(Color.FromArgb(16, 110, 190)), x + 7.0f, y + 10.5f, 1.2f, 1.0f);
            g.DrawLine(p, x + 10.5f, y + 13.0f, x + 14.5f, y + 13.0f);

            DrawDropdownChevron(g, x + 17, y + 8, Color.FromArgb(80, 80, 80));
        }
    }

    private static void DrawWordOutdent(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (Pen pArrow = new Pen(Color.FromArgb(16, 110, 190), 1.25f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            g.DrawLine(pArrow, x + 4f, y + 4.5f, x + 1.5f, y + 6.5f);
            g.DrawLine(pArrow, x + 1.5f, y + 6.5f, x + 4f, y + 8.5f);
            g.DrawLine(pArrow, x + 1.5f, y + 6.5f, x + 5.5f, y + 6.5f);

            g.DrawLine(p, x + 7.5f, y + 2.5f, x + 16.5f, y + 2.5f);
            g.DrawLine(p, x + 7.5f, y + 5.5f, x + 16.5f, y + 5.5f);
            g.DrawLine(p, x + 7.5f, y + 8.5f, x + 16.5f, y + 8.5f);
            g.DrawLine(p, x + 7.5f, y + 11.5f, x + 16.5f, y + 11.5f);
        }
    }

    private static void DrawWordIndent(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (Pen pArrow = new Pen(Color.FromArgb(16, 110, 190), 1.25f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            g.DrawLine(pArrow, x + 3f, y + 4.5f, x + 5.5f, y + 6.5f);
            g.DrawLine(pArrow, x + 5.5f, y + 6.5f, x + 3f, y + 8.5f);
            g.DrawLine(pArrow, x + 1.5f, y + 6.5f, x + 5.5f, y + 6.5f);

            g.DrawLine(p, x + 7.5f, y + 2.5f, x + 16.5f, y + 2.5f);
            g.DrawLine(p, x + 7.5f, y + 5.5f, x + 16.5f, y + 5.5f);
            g.DrawLine(p, x + 7.5f, y + 8.5f, x + 16.5f, y + 8.5f);
            g.DrawLine(p, x + 7.5f, y + 11.5f, x + 16.5f, y + 11.5f);
        }
    }

    private static void DrawWordSort(Graphics g, float x, float y)
    {
        using (Font fA = new Font("Segoe UI", 6.0f, FontStyle.Bold))
        using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(16, 110, 190)))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 70, 75)))
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            g.DrawString("A", fA, bBlue, x - 1f, y - 1f);
            g.DrawString("Z", fA, b, x - 1f, y + 6f);
            g.DrawLine(p, x + 10f, y + 1f, x + 10f, y + 12f);
            g.DrawLine(p, x + 8f, y + 9.5f, x + 10f, y + 12f);
            g.DrawLine(p, x + 12f, y + 9.5f, x + 10f, y + 12f);
        }
    }

    private static void DrawWordPilcrow(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(38, 38, 42), 1.15f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 42)))
        {
            g.DrawLine(p, x + 2f, y + 2f, x + 11f, y + 2f);
            g.DrawLine(p, x + 8f, y + 2f, x + 8f, y + 13f);
            g.DrawLine(p, x + 10.5f, y + 2f, x + 10.5f, y + 13f);
            using (GraphicsPath loop = new GraphicsPath())
            {
                loop.AddArc(x + 2f, y + 2f, 6.5f, 6.5f, 90, 180);
                loop.CloseFigure();
                g.FillPath(b, loop);
            }
        }
    }

    private static void DrawWordAlignment(Graphics g, float x, float y, int alignType)
    {
        using (Pen pen = new Pen(Color.FromArgb(45, 45, 48), 1.1f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            float ox = x + 4.5f;
            float oy = y + 5.0f;

            if (alignType == 0) // Left
            {
                g.DrawLine(pen, ox, oy, ox + 13f, oy);
                g.DrawLine(pen, ox, oy + 3.0f, ox + 7.5f, oy + 3.0f);
                g.DrawLine(pen, ox, oy + 6.0f, ox + 13f, oy + 6.0f);
                g.DrawLine(pen, ox, oy + 9.0f, ox + 9.5f, oy + 9.0f);
            }
            else if (alignType == 1) // Center
            {
                g.DrawLine(pen, ox, oy, ox + 13f, oy);
                g.DrawLine(pen, ox + 2.75f, oy + 3.0f, ox + 10.25f, oy + 3.0f);
                g.DrawLine(pen, ox, oy + 6.0f, ox + 13f, oy + 6.0f);
                g.DrawLine(pen, ox + 1.75f, oy + 9.0f, ox + 11.25f, oy + 9.0f);
            }
            else if (alignType == 2) // Right
            {
                g.DrawLine(pen, ox, oy, ox + 13f, oy);
                g.DrawLine(pen, ox + 5.5f, oy + 3.0f, ox + 13f, oy + 3.0f);
                g.DrawLine(pen, ox, oy + 6.0f, ox + 13f, oy + 6.0f);
                g.DrawLine(pen, ox + 3.5f, oy + 9.0f, ox + 13f, oy + 9.0f);
            }
            else if (alignType == 3) // Justify
            {
                g.DrawLine(pen, ox, oy, ox + 13f, oy);
                g.DrawLine(pen, ox, oy + 3.0f, ox + 13f, oy + 3.0f);
                g.DrawLine(pen, ox, oy + 6.0f, ox + 13f, oy + 6.0f);
                g.DrawLine(pen, ox, oy + 9.0f, ox + 13f, oy + 9.0f);
            }
        }
    }

    private static void DrawWordLineSpacing(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(70, 70, 75), 1.1f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        using (Pen pArrow = new Pen(Color.FromArgb(16, 110, 190), 1.2f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
        {
            g.DrawLine(pArrow, x + 2.5f, y + 2f, x + 2.5f, y + 12f);
            g.DrawLine(pArrow, x + 1f, y + 4f, x + 2.5f, y + 2f);
            g.DrawLine(pArrow, x + 4f, y + 4f, x + 2.5f, y + 2f);
            g.DrawLine(pArrow, x + 1f, y + 10f, x + 2.5f, y + 12f);
            g.DrawLine(pArrow, x + 4f, y + 10f, x + 2.5f, y + 12f);

            g.DrawLine(p, x + 6.5f, y + 2.5f, x + 14.5f, y + 2.5f);
            g.DrawLine(p, x + 6.5f, y + 5.5f, x + 14.5f, y + 5.5f);
            g.DrawLine(p, x + 6.5f, y + 8.5f, x + 14.5f, y + 8.5f);
            g.DrawLine(p, x + 6.5f, y + 11.5f, x + 14.5f, y + 11.5f);

            DrawDropdownChevron(g, x + 17f, y + 7f, Color.FromArgb(80, 80, 80));
        }
    }

    private static void DrawWordPaintBucket(Graphics g, float x, float y, Color col)
    {
        // Authentic Word bucket:
        // Cylinder tilted 45° with upright handle and blue paint pouring stream
        using (Pen pOutline = new Pen(Color.FromArgb(45, 45, 48), 1.1f) { LineJoin = LineJoin.Round })
        using (SolidBrush bBody = new SolidBrush(Color.FromArgb(245, 245, 245)))
        using (Pen pStream = new Pen(Color.FromArgb(16, 110, 190), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (SolidBrush bPaint = new SolidBrush(Color.FromArgb(16, 110, 190)))
        {
            // Tilted bucket body
            PointF[] bucket = new PointF[] {
                new PointF(x + 3.5f, y + 5.5f),
                new PointF(x + 7.5f, y + 1.5f),
                new PointF(x + 11.5f, y + 5.5f),
                new PointF(x + 7.5f, y + 9.5f)
            };
            g.FillPolygon(bBody, bucket);
            g.DrawPolygon(pOutline, bucket);

            // Handle arch
            using (Pen pHandle = new Pen(Color.FromArgb(110, 110, 115), 1.0f))
            {
                g.DrawArc(pHandle, x + 6.0f, y - 1.0f, 4.5f, 4.0f, 180, 180);
            }

            // Blue paint stream pouring down from right spout to bar
            g.DrawBezier(pStream,
                new PointF(x + 10.5f, y + 5.0f),
                new PointF(x + 12.5f, y + 6.5f),
                new PointF(x + 12.5f, y + 9.5f),
                new PointF(x + 11.5f, y + 13.5f));

            DrawDropdownChevron(g, x + 16f, y + 7f, Color.FromArgb(80, 80, 80));

            // Horizontal color bar underneath
            if (col != Color.Transparent && col.A > 0)
            {
                using (SolidBrush bBar = new SolidBrush(col))
                    g.FillRectangle(bBar, x + 1, y + 15, 14, 3);
            }
            else
            {
                using (Pen pBar = new Pen(Color.FromArgb(165, 168, 172), 0.8f))
                {
                    g.FillRectangle(Brushes.White, x + 1, y + 15, 14, 3);
                    g.DrawRectangle(pBar, x + 1, y + 15, 14, 3);
                }
            }
        }
    }

    private static void DrawWordBorders(Graphics g, float x, float y)
    {
        using (Pen p = new Pen(Color.FromArgb(45, 45, 48), 1.1f))
        using (Pen pDashed = new Pen(Color.FromArgb(160, 160, 160), 1.0f) { DashStyle = DashStyle.Dot })
        {
            g.DrawRectangle(p, x + 2.5f, y + 2.5f, 11f, 11f);
            g.DrawLine(pDashed, x + 8f, y + 2.5f, x + 8f, y + 13.5f);
            g.DrawLine(pDashed, x + 2.5f, y + 8f, x + 13.5f, y + 8f);

            DrawDropdownChevron(g, x + 16f, y + 7f, Color.FromArgb(80, 80, 80));
        }
    }
}

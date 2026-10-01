using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class PageColorDropDownForm : Form
    {
        public Action<Color> OnColorSelected;
        public Action OnNoColorSelected;
        public Action OnMoreColorsClicked;

        private int HoverSection = 0; // 1: No Color, 2: Theme Main, 3: Theme Shade, 4: Standard, 5: More Colors, 6: Fill Effects
        private int HoverRow = -1;
        private int HoverCol = -1;

        // Theme colors palette (10 columns x 6 rows)
        private Color[,] ThemeColors;
        // Standard colors (10 colors)
        private Color[] StandardColors;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        public PageColorDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location;
            this.Size = new Size(228, 270);
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            InitPalette();

            this.MouseMove += (s, e) =>
            {
                int prevSec = HoverSection;
                int prevR = HoverRow;
                int prevC = HoverCol;

                HitTest(e.X, e.Y, out HoverSection, out HoverCol, out HoverRow);

                if (prevSec != HoverSection || prevR != HoverRow || prevC != HoverCol)
                {
                    Invalidate();
                }
            };

            this.MouseLeave += (s, e) =>
            {
                HoverSection = 0;
                HoverRow = -1;
                HoverCol = -1;
                Invalidate();
            };

            this.Deactivate += (s, e) => this.Close();

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) this.Close();
            };
        }

        private void InitPalette()
        {
            StandardColors = new Color[] {
                Color.FromArgb(192, 0, 0),     // Dark Red
                Color.FromArgb(255, 0, 0),     // Red
                Color.FromArgb(255, 192, 0),   // Orange / Gold
                Color.FromArgb(255, 255, 0),   // Yellow
                Color.FromArgb(146, 208, 80),  // Light Green
                Color.FromArgb(0, 176, 80),    // Green
                Color.FromArgb(0, 176, 240),   // Light Blue
                Color.FromArgb(0, 112, 192),   // Blue
                Color.FromArgb(0, 32, 96),     // Dark Blue
                Color.FromArgb(112, 48, 160)   // Purple
            };

            Color[] topRow = new Color[] {
                Color.White, Color.Black, Color.FromArgb(231, 230, 230), Color.FromArgb(68, 84, 106),
                Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165),
                Color.FromArgb(255, 192, 0), Color.FromArgb(91, 155, 213), Color.FromArgb(112, 173, 71)
            };

            ThemeColors = new Color[6, 10];
            for (int c = 0; c < 10; c++) ThemeColors[0, c] = topRow[c];

            // 5 Shade rows matching standard Word tint/shade algorithm
            for (int c = 0; c < 10; c++)
            {
                Color baseC = topRow[c];
                ThemeColors[1, c] = BlendColor(baseC, Color.White, 0.8f);
                ThemeColors[2, c] = BlendColor(baseC, Color.White, 0.6f);
                ThemeColors[3, c] = BlendColor(baseC, Color.White, 0.4f);
                ThemeColors[4, c] = BlendColor(baseC, Color.Black, 0.25f);
                ThemeColors[5, c] = BlendColor(baseC, Color.Black, 0.5f);
            }
        }

        private Color BlendColor(Color c1, Color c2, float factor)
        {
            int r = (int)(c1.R * (1 - factor) + c2.R * factor);
            int g = (int)(c1.G * (1 - factor) + c2.G * factor);
            int b = (int)(c1.B * (1 - factor) + c2.B * factor);
            return Color.FromArgb(Math.Min(255, Math.Max(0, r)), Math.Min(255, Math.Max(0, g)), Math.Min(255, Math.Max(0, b)));
        }

        private void HitTest(int x, int y, out int sec, out int col, out int row)
        {
            sec = 0;
            col = -1;
            row = -1;

            int curY = 6;
            // 1. No Color top button
            Rectangle noColorRect = new Rectangle(6, curY, Width - 12, 26);
            if (noColorRect.Contains(x, y))
            {
                sec = 1;
                return;
            }
            curY += 28 + 8 + 20;

            int startGridX = 14;
            int cellW = 17;
            int gapGridX = 3;

            // 2. Theme Main (Row 0)
            if (y >= curY && y <= curY + 17)
            {
                for (int c = 0; c < 10; c++)
                {
                    int cx = startGridX + c * (cellW + gapGridX);
                    if (x >= cx && x <= cx + cellW)
                    {
                        sec = 2;
                        col = c;
                        return;
                    }
                }
            }
            curY += 17 + 4;

            // 3. Theme Shades (5 seamless rows, height=65)
            if (y >= curY && y <= curY + 65)
            {
                int r = (y - curY) / 13;
                if (r > 4) r = 4;
                for (int c = 0; c < 10; c++)
                {
                    int cx = startGridX + c * (cellW + gapGridX);
                    if (x >= cx && x <= cx + cellW)
                    {
                        sec = 3;
                        row = r;
                        col = c;
                        return;
                    }
                }
            }
            curY += 65 + 10 + 1 + 6 + 20;

            // 4. Standard Colors
            if (y >= curY && y <= curY + 17)
            {
                for (int c = 0; c < 10; c++)
                {
                    int cx = startGridX + c * (cellW + gapGridX);
                    if (x >= cx && x <= cx + cellW)
                    {
                        sec = 4;
                        col = c;
                        return;
                    }
                }
            }
            curY += 17 + 10 + 1 + 4;

            // 5. More Colors
            Rectangle moreRect = new Rectangle(6, curY, Width - 12, 26);
            if (moreRect.Contains(x, y))
            {
                sec = 5;
                return;
            }
            curY += 27;

            // 6. Fill Effects
            Rectangle fillEffRect = new Rectangle(6, curY, Width - 12, 26);
            if (fillEffRect.Contains(x, y))
            {
                sec = 6;
                return;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(195, 202, 212), 1.0f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            int curY = 6;

            // 1. No Color Button
            Rectangle noColorRect = new Rectangle(6, curY, Width - 12, 26);
            if (HoverSection == 1)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, noColorRect);
                    g.DrawRectangle(hp, noColorRect.X, noColorRect.Y, noColorRect.Width - 1, noColorRect.Height - 1);
                }
            }

            int swatchX = 14;
            int swatchY = curY + 4;
            using (SolidBrush sb = new SolidBrush(Color.White))
            using (Pen sp = new Pen(Color.FromArgb(180, 185, 195), 1.0f))
            {
                g.FillRectangle(sb, swatchX, swatchY, 18, 18);
                g.DrawRectangle(sp, swatchX, swatchY, 18, 18);
            }
            using (Pen redSlash = new Pen(Color.FromArgb(220, 38, 38), 1.5f))
            {
                g.DrawLine(redSlash, swatchX + 2, swatchY + 16, swatchX + 16, swatchY + 2);
            }
            using (Font txtFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Kh\u00f4ng m\u00e0u (No Color)", txtFont, tb, swatchX + 28, curY + 4);
            }
            curY += 28 + 8;

            // 2. Theme Colors Header
            using (Font headerFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush hb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Theme Colors", headerFont, hb, 14, curY);
            }
            curY += 20;

            // 3. Theme Base Row (Row 0 - Squares)
            int startGridX = 14;
            int cellW = 17;
            int gapGridX = 3;

            for (int c = 0; c < 10; c++)
            {
                int cx = startGridX + c * (cellW + gapGridX);
                Rectangle cellRect = new Rectangle(cx, curY, cellW, 17);

                using (SolidBrush cb = new SolidBrush(ThemeColors[0, c]))
                {
                    g.FillRectangle(cb, cellRect);
                }

                bool isHovered = (HoverSection == 2 && HoverCol == c);
                using (Pen cp = new Pen(isHovered ? Color.FromArgb(245, 158, 11) : Color.FromArgb(205, 210, 218), isHovered ? 1.6f : 1.0f))
                {
                    g.DrawRectangle(cp, cellRect.X, cellRect.Y, cellRect.Width - 1, cellRect.Height - 1);
                }
            }
            curY += 17 + 4;

            // 4. Theme Shades (Seamless Solid Vertical Columns)
            for (int c = 0; c < 10; c++)
            {
                int cx = startGridX + c * (cellW + gapGridX);

                for (int r = 0; r < 5; r++)
                {
                    int cy = curY + r * 13;
                    Rectangle shadeRect = new Rectangle(cx, cy, cellW, 13);
                    using (SolidBrush cb = new SolidBrush(ThemeColors[r + 1, c]))
                    {
                        g.FillRectangle(cb, shadeRect);
                    }
                }

                // One subtle outer border enclosing the 5 shades
                using (Pen stripPen = new Pen(Color.FromArgb(215, 220, 228), 1.0f))
                {
                    g.DrawRectangle(stripPen, cx, curY, cellW - 1, 65 - 1);
                }

                // Highlight hovered shade
                if (HoverSection == 3 && HoverCol == c && HoverRow >= 0 && HoverRow < 5)
                {
                    int hovY = curY + HoverRow * 13;
                    using (Pen hovPen = new Pen(Color.FromArgb(245, 158, 11), 1.6f))
                    {
                        g.DrawRectangle(hovPen, cx - 1, hovY - 1, cellW + 1, 14);
                    }
                }
            }
            curY += 65 + 10;

            // Divider Line
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawLine(divPen, 10, curY, Width - 10, curY);
            }
            curY += 6;

            // 5. Standard Colors
            using (Font headerFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush hb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Standard Colors", headerFont, hb, 14, curY);
            }
            curY += 20;

            for (int c = 0; c < 10; c++)
            {
                int cx = startGridX + c * (cellW + gapGridX);
                Rectangle cellRect = new Rectangle(cx, curY, cellW, 17);

                using (SolidBrush cb = new SolidBrush(StandardColors[c]))
                {
                    g.FillRectangle(cb, cellRect);
                }

                bool isHovered = (HoverSection == 4 && HoverCol == c);
                using (Pen cp = new Pen(isHovered ? Color.FromArgb(245, 158, 11) : Color.FromArgb(205, 210, 218), isHovered ? 1.6f : 1.0f))
                {
                    g.DrawRectangle(cp, cellRect.X, cellRect.Y, cellRect.Width - 1, cellRect.Height - 1);
                }
            }
            curY += 17 + 10;

            // Divider Line
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawLine(divPen, 10, curY, Width - 10, curY);
            }
            curY += 4;

            // 6. More Colors Button
            Rectangle moreBtnRect = new Rectangle(6, curY, Width - 12, 26);
            if (HoverSection == 5)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, moreBtnRect);
                    g.DrawRectangle(hp, moreBtnRect.X, moreBtnRect.Y, moreBtnRect.Width - 1, moreBtnRect.Height - 1);
                }
            }

            DrawArtistPaletteIcon(g, 15, curY + 5);
            using (Font itemFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("More Colors...", itemFont, tb, 42, curY + 4);
            }
            curY += 27;

            // 7. Fill Effects Button
            Rectangle fillBtnRect = new Rectangle(6, curY, Width - 12, 26);
            if (HoverSection == 6)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, fillBtnRect);
                    g.DrawRectangle(hp, fillBtnRect.X, fillBtnRect.Y, fillBtnRect.Width - 1, fillBtnRect.Height - 1);
                }
            }

            DrawGradientIcon(g, 15, curY + 5);
            using (Font itemFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Fill Effects...", itemFont, tb, 42, curY + 4);
            }
        }

        private void DrawArtistPaletteIcon(Graphics g, int x, int y)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddBezier(x + 2, y + 6, x + 3, y + 1, x + 13, y + 1, x + 15, y + 6);
            path.AddBezier(x + 15, y + 6, x + 16, y + 11, x + 12, y + 15, x + 7, y + 15);
            path.AddBezier(x + 7, y + 15, x + 4, y + 15, x + 5, y + 11, x + 4, y + 10);
            path.AddBezier(x + 4, y + 10, x + 1, y + 9, x + 1, y + 7, x + 2, y + 6);
            path.CloseFigure();

            using (SolidBrush pb = new SolidBrush(Color.FromArgb(252, 252, 252)))
            {
                g.FillPath(pb, path);
            }
            using (Pen pPen = new Pen(Color.FromArgb(55, 65, 81), 1.1f))
            {
                g.DrawPath(pPen, path);
            }

            using (SolidBrush hb = new SolidBrush(Color.White))
            using (Pen hp = new Pen(Color.FromArgb(75, 85, 99), 0.9f))
            {
                g.FillEllipse(hb, x + 9f, y + 9f, 3.5f, 4f);
                g.DrawEllipse(hp, x + 9f, y + 9f, 3.5f, 4f);
            }

            using (SolidBrush redDab = new SolidBrush(Color.FromArgb(220, 38, 38)))
            using (SolidBrush yellowDab = new SolidBrush(Color.FromArgb(245, 158, 11)))
            using (SolidBrush greenDab = new SolidBrush(Color.FromArgb(16, 185, 129)))
            using (SolidBrush blueDab = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                g.FillEllipse(redDab, x + 4.2f, y + 4.2f, 2.8f, 2.8f);
                g.FillEllipse(yellowDab, x + 8.2f, y + 2.8f, 2.8f, 2.8f);
                g.FillEllipse(greenDab, x + 12.0f, y + 4.8f, 2.8f, 2.8f);
                g.FillEllipse(blueDab, x + 5.5f, y + 9.5f, 2.8f, 2.8f);
            }
        }

        private void DrawGradientIcon(Graphics g, int x, int y)
        {
            Rectangle rect = new Rectangle(x, y + 1, 16, 16);
            using (LinearGradientBrush lgb = new LinearGradientBrush(rect, Color.White, Color.FromArgb(140, 150, 165), 45f))
            {
                g.FillRectangle(lgb, rect);
            }
            using (Pen bp = new Pen(Color.FromArgb(100, 110, 125), 1.0f))
            {
                g.DrawRectangle(bp, rect);
                g.DrawLine(bp, rect.Left, rect.Bottom, rect.Right, rect.Top);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int sec, col, row;
            HitTest(e.X, e.Y, out sec, out col, out row);

            if (sec == 1) // No Color
            {
                if (OnNoColorSelected != null) OnNoColorSelected();
                Close();
            }
            else if (sec == 2) // Theme Main
            {
                if (col >= 0 && col < 10)
                {
                    if (OnColorSelected != null) OnColorSelected(ThemeColors[0, col]);
                    Close();
                }
            }
            else if (sec == 3) // Theme Shade
            {
                if (row >= 0 && row < 5 && col >= 0 && col < 10)
                {
                    if (OnColorSelected != null) OnColorSelected(ThemeColors[row + 1, col]);
                    Close();
                }
            }
            else if (sec == 4) // Standard Colors
            {
                if (col >= 0 && col < 10)
                {
                    if (OnColorSelected != null) OnColorSelected(StandardColors[col]);
                    Close();
                }
            }
            else if (sec == 5) // More Colors
            {
                if (OnMoreColorsClicked != null) OnMoreColorsClicked();
                Close();
            }
            else if (sec == 6) // Fill Effects
            {
                if (OnColorSelected != null) OnColorSelected(Color.FromArgb(250, 248, 240));
                Close();
            }
        }
    }
}

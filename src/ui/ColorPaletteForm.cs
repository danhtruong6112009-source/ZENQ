using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public enum ColorPaletteMode
    {
        Text,       // Word Text/Font Color: "Automatic" at top, More Colors..., Gradient >
        Fill,       // Word Fill/Shape Color: "No Color" at top, More Colors..., Eyedropper
        Highlight,  // Word Text Highlight Color: 15 highlight color grid, "No Color" at bottom, Stop Highlighting
        PageColor   // Word Page Color: "No Color" at top, More Colors..., Fill Effects...
    }

    public class ColorPaletteForm : Form
    {
        public Color SelectedColor { get; private set; }
        public event Action<Color> OnColorSelected;

        public ColorPaletteMode Mode { get; set; }

        private bool isSubDialogOpening = false;
        private Color _currentColor;
        private bool _allowNoColor;
        private bool _isHighlight;

        // Custom colors persistence for ColorDialog (Define Custom Colors >>)
        private static int[] _savedCustomColors = null;

        // Interactive hover states
        // HoverSection: 0 = None, 1 = TopAction, 2 = ThemeMain, 3 = ThemeShade, 4 = Standard, 5 = MoreColors, 6 = Gradient, 7 = Eyedropper, 10 = Highlight, 11 = StopHighlight
        private int _hoverSection = 0;
        private int _hoverCol = -1;
        private int _hoverRow = -1;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        public static Color GetPixelColorAt(int x, int y)
        {
            IntPtr desk = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(desk, x, y);
            ReleaseDC(IntPtr.Zero, desk);
            return Color.FromArgb((int)(pixel & 0x000000FF),
                                  (int)((pixel & 0x0000FF00) >> 8),
                                  (int)((pixel & 0x00FF0000) >> 16));
        }

        // Word Official Theme Colors (10 Columns)
        private static readonly Color[] ThemeMainColors = new Color[]
        {
            Color.FromArgb(255, 255, 255), // White (Background 1)
            Color.FromArgb(0, 0, 0),       // Black (Text 1)
            Color.FromArgb(231, 230, 230), // Light Gray (Background 2)
            Color.FromArgb(68, 84, 106),   // Dark Slate (Text 2)
            Color.FromArgb(68, 114, 196),  // Accent 1 (Blue)
            Color.FromArgb(237, 125, 49),  // Accent 2 (Orange)
            Color.FromArgb(165, 165, 165), // Accent 3 (Gray)
            Color.FromArgb(255, 192, 0),   // Accent 4 (Gold / Yellow)
            Color.FromArgb(91, 155, 213),  // Accent 5 (Light Blue)
            Color.FromArgb(112, 173, 71)   // Accent 6 (Green)
        };

        // 5 Shade / Tint variations for each of the 10 theme columns [5 rows, 10 cols]
        private static readonly Color[,] ThemeShades = new Color[5, 10]
        {
            // Row 1: Tint 80% / Shade 20%
            {
                Color.FromArgb(242, 242, 242), Color.FromArgb(127, 127, 127), Color.FromArgb(208, 206, 206), Color.FromArgb(214, 220, 228), Color.FromArgb(217, 225, 242),
                Color.FromArgb(251, 228, 213), Color.FromArgb(237, 237, 237), Color.FromArgb(255, 242, 204), Color.FromArgb(222, 235, 246), Color.FromArgb(226, 239, 218)
            },
            // Row 2: Tint 60% / Shade 40%
            {
                Color.FromArgb(217, 217, 217), Color.FromArgb(89, 89, 89), Color.FromArgb(174, 171, 171), Color.FromArgb(172, 185, 202), Color.FromArgb(180, 198, 231),
                Color.FromArgb(248, 202, 172), Color.FromArgb(219, 219, 219), Color.FromArgb(255, 229, 153), Color.FromArgb(189, 215, 238), Color.FromArgb(198, 224, 179)
            },
            // Row 3: Tint 40% / Shade 60%
            {
                Color.FromArgb(191, 191, 191), Color.FromArgb(63, 63, 63), Color.FromArgb(117, 113, 113), Color.FromArgb(132, 151, 176), Color.FromArgb(142, 170, 219),
                Color.FromArgb(244, 176, 132), Color.FromArgb(201, 201, 201), Color.FromArgb(255, 217, 102), Color.FromArgb(155, 194, 229), Color.FromArgb(168, 208, 141)
            },
            // Row 4: Shade 25% / Darker
            {
                Color.FromArgb(166, 166, 166), Color.FromArgb(38, 38, 38), Color.FromArgb(58, 56, 56), Color.FromArgb(47, 63, 86), Color.FromArgb(47, 84, 150),
                Color.FromArgb(198, 89, 17), Color.FromArgb(123, 123, 123), Color.FromArgb(191, 143, 0), Color.FromArgb(46, 117, 181), Color.FromArgb(84, 130, 53)
            },
            // Row 5: Shade 50% / Darkest
            {
                Color.FromArgb(128, 128, 128), Color.FromArgb(12, 12, 12), Color.FromArgb(22, 22, 22), Color.FromArgb(31, 42, 56), Color.FromArgb(31, 56, 100),
                Color.FromArgb(131, 59, 7), Color.FromArgb(82, 82, 82), Color.FromArgb(127, 96, 0), Color.FromArgb(30, 78, 120), Color.FromArgb(55, 86, 35)
            }
        };

        // Word Official Standard Colors (10 Colors)
        private static readonly Color[] StandardColors = new Color[]
        {
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

        // Word 15 Standard Highlight Colors (3 rows x 5 cols)
        private static readonly Color[] HighlightColors = new Color[]
        {
            Color.FromArgb(255, 255, 0),   // Yellow
            Color.FromArgb(0, 255, 0),     // Bright Green
            Color.FromArgb(0, 255, 255),   // Turquoise / Cyan
            Color.FromArgb(255, 0, 255),   // Pink / Magenta
            Color.FromArgb(0, 0, 255),     // Blue
            Color.FromArgb(255, 0, 0),     // Red
            Color.FromArgb(0, 0, 128),     // Dark Blue
            Color.FromArgb(0, 128, 128),   // Teal
            Color.FromArgb(0, 128, 0),     // Green
            Color.FromArgb(128, 0, 128),   // Violet
            Color.FromArgb(128, 0, 0),     // Dark Red
            Color.FromArgb(128, 128, 0),   // Dark Yellow
            Color.FromArgb(128, 128, 128), // Gray 50%
            Color.FromArgb(192, 192, 192), // Gray 25%
            Color.FromArgb(0, 0, 0)        // Black
        };

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        public ColorPaletteForm(Point screenLocation, bool isHighlight)
            : this(screenLocation, isHighlight ? ColorPaletteMode.Highlight : ColorPaletteMode.Text, Color.Empty)
        {
        }

        public ColorPaletteForm(Point screenLocation, Color currentColor = default(Color), bool allowNoColor = true, bool isHighlight = false)
            : this(screenLocation, isHighlight ? ColorPaletteMode.Highlight : (allowNoColor && currentColor != default(Color) ? ColorPaletteMode.Fill : ColorPaletteMode.Text), currentColor, allowNoColor)
        {
        }

        public ColorPaletteForm(Point screenLocation, ColorPaletteMode mode, Color currentColor = default(Color), bool allowNoColor = true)
        {
            Mode = mode;
            _currentColor = currentColor;
            _allowNoColor = allowNoColor;
            _isHighlight = (mode == ColorPaletteMode.Highlight);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int desiredW = 228;
            int desiredH;

            if (Mode == ColorPaletteMode.Highlight)
            {
                desiredH = 160;
            }
            else
            {
                // Word standard height: Top(28) + Theme(114) + Divider(11) + Standard(43) + Divider(11) + Bottom(88) = 295
                desiredH = 295;
            }

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            if (targetY + desiredH > wa.Bottom)
            {
                int aboveY = targetY - desiredH - 30;
                if (aboveY >= wa.Top)
                {
                    targetY = aboveY;
                }
                else
                {
                    targetY = Math.Max(wa.Top + 5, wa.Bottom - desiredH - 5);
                    desiredH = Math.Min(desiredH, wa.Bottom - targetY - 5);
                }
            }
            if (targetX + desiredW > wa.Right) targetX = wa.Right - desiredW - 5;
            if (targetX < wa.Left) targetX = wa.Left + 5;

            Location = new Point(targetX, targetY);
            Size = new Size(desiredW, desiredH);

            Deactivate += (s, e) =>
            {
                if (!isSubDialogOpening) Close();
            };

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int oldSec = _hoverSection;
            int oldCol = _hoverCol;
            int oldRow = _hoverRow;

            HitTest(e.X, e.Y, out _hoverSection, out _hoverCol, out _hoverRow);

            if (oldSec != _hoverSection || oldCol != _hoverCol || oldRow != _hoverRow)
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverSection = 0;
            _hoverCol = -1;
            _hoverRow = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int sec, col, row;
            HitTest(e.X, e.Y, out sec, out col, out row);

            if (sec == 1) // Top action (Automatic or No Color)
            {
                if (Mode == ColorPaletteMode.Text)
                {
                    SelectColorAndClose(Color.Black);
                }
                else
                {
                    SelectColorAndClose(Color.Transparent);
                }
            }
            else if (sec == 2) // Theme Main
            {
                if (col >= 0 && col < 10)
                {
                    SelectColorAndClose(ThemeMainColors[col]);
                }
            }
            else if (sec == 3) // Theme Shade
            {
                if (row >= 0 && row < 5 && col >= 0 && col < 10)
                {
                    SelectColorAndClose(ThemeShades[row, col]);
                }
            }
            else if (sec == 4) // Standard Colors
            {
                if (col >= 0 && col < 10)
                {
                    SelectColorAndClose(StandardColors[col]);
                }
            }
            else if (sec == 5) // More Colors...
            {
                OpenMoreColorsDialog();
            }
            else if (sec == 6) // Gradient >
            {
                Color baseC = (_currentColor != Color.Empty && _currentColor != Color.Transparent) ? _currentColor : Color.FromArgb(43, 87, 154);
                SelectColorAndClose(baseC);
            }
            else if (sec == 7) // Eyedropper
            {
                StartEyedropperMode();
            }
            else if (sec == 10) // Highlight Color
            {
                if (col >= 0 && col < HighlightColors.Length)
                {
                    SelectColorAndClose(HighlightColors[col]);
                }
            }
            else if (sec == 11) // Stop Highlighting
            {
                SelectColorAndClose(Color.Transparent);
            }
        }

        private void HitTest(int x, int y, out int section, out int col, out int row)
        {
            section = 0;
            col = -1;
            row = -1;

            if (Mode == ColorPaletteMode.Highlight)
            {
                int startX = 14;
                int startY = 10;
                int cellW = 34;
                int cellH = 24;
                int gapX = 4;
                int gapY = 4;

                for (int i = 0; i < HighlightColors.Length; i++)
                {
                    int r = i / 5;
                    int c = i % 5;
                    int cx = startX + c * (cellW + gapX);
                    int cy = startY + r * (cellH + gapY);
                    if (x >= cx && x <= cx + cellW && y >= cy && y <= cy + cellH)
                    {
                        section = 10;
                        col = i;
                        return;
                    }
                }

                int noColY = startY + 3 * (cellH + gapY) + 6;
                if (y >= noColY && y <= noColY + 24 && x >= 6 && x <= Width - 6)
                {
                    section = 1; // No Color
                    return;
                }

                int stopY = noColY + 26;
                if (y >= stopY && y <= stopY + 24 && x >= 6 && x <= Width - 6)
                {
                    section = 11; // Stop Highlighting
                    return;
                }
                return;
            }

            int curY = 6;

            // 1. Top action button (Automatic or No Color)
            Rectangle topBtnRect = new Rectangle(6, curY, Width - 12, 26);
            if (topBtnRect.Contains(x, y))
            {
                section = 1;
                return;
            }
            curY += 28 + 8 + 20;

            // 2. Theme Main Row (10 cols, cellW=17, gap=3)
            int startGridX = 14;
            int cellWidth = 17;
            int gapGridX = 3;

            if (y >= curY && y <= curY + 17)
            {
                for (int c = 0; c < 10; c++)
                {
                    int cx = startGridX + c * (cellWidth + gapGridX);
                    if (x >= cx && x <= cx + cellWidth)
                    {
                        section = 2;
                        col = c;
                        return;
                    }
                }
            }
            curY += 17 + 4;

            // 3. Theme Shades (5 rows stacked seamlessly without vertical gaps, total height = 65)
            if (y >= curY && y <= curY + 65)
            {
                int r = (y - curY) / 13;
                if (r > 4) r = 4;
                for (int c = 0; c < 10; c++)
                {
                    int cx = startGridX + c * (cellWidth + gapGridX);
                    if (x >= cx && x <= cx + cellWidth)
                    {
                        section = 3;
                        row = r;
                        col = c;
                        return;
                    }
                }
            }
            curY += 65 + 10 + 1 + 6 + 20;

            // 4. Standard Colors Row (cellW=17, cellH=17)
            if (y >= curY && y <= curY + 17)
            {
                for (int c = 0; c < 10; c++)
                {
                    int cx = startGridX + c * (cellWidth + gapGridX);
                    if (x >= cx && x <= cx + cellWidth)
                    {
                        section = 4;
                        col = c;
                        return;
                    }
                }
            }
            curY += 17 + 10 + 1 + 4;

            // 5. More Colors...
            Rectangle moreRect = new Rectangle(6, curY, Width - 12, 26);
            if (moreRect.Contains(x, y))
            {
                section = 5;
                return;
            }
            curY += 27;

            // 6. Gradient >
            Rectangle gradRect = new Rectangle(6, curY, Width - 12, 26);
            if (gradRect.Contains(x, y))
            {
                section = 6;
                return;
            }
            curY += 27;

            // 7. Eyedropper
            Rectangle eyeRect = new Rectangle(6, curY, Width - 12, 26);
            if (eyeRect.Contains(x, y))
            {
                section = 7;
                return;
            }
        }

        private void SelectColorAndClose(Color col)
        {
            SelectedColor = col;
            if (OnColorSelected != null) OnColorSelected(col);
            Close();
        }

        private void OpenMoreColorsDialog()
        {
            isSubDialogOpening = true;
            using (ColorDialog cd = new ColorDialog())
            {
                cd.FullOpen = true;
                cd.AnyColor = true;
                if (_currentColor != Color.Empty && _currentColor != Color.Transparent)
                {
                    cd.Color = _currentColor;
                }
                if (_savedCustomColors != null)
                {
                    cd.CustomColors = _savedCustomColors;
                }

                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    _savedCustomColors = cd.CustomColors;
                    SelectColorAndClose(cd.Color);
                }
                else
                {
                    isSubDialogOpening = false;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Outer 1px clean border
            using (Pen borderPen = new Pen(Color.FromArgb(195, 202, 212), 1.0f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            if (Mode == ColorPaletteMode.Highlight)
            {
                PaintHighlightMode(g);
                return;
            }

            int curY = 6;

            // ==================== 1. TOP ACTION BUTTON (AUTOMATIC / NO COLOR) ====================
            Rectangle topBtnRect = new Rectangle(6, curY, Width - 12, 26);
            bool isTopHovered = (_hoverSection == 1);
            if (isTopHovered)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, topBtnRect);
                    g.DrawRectangle(hp, topBtnRect.X, topBtnRect.Y, topBtnRect.Width - 1, topBtnRect.Height - 1);
                }
            }

            int swatchX = 14;
            int swatchY = curY + 4;

            if (Mode == ColorPaletteMode.Text)
            {
                // Word Automatic Swatch: Pure black square (18x18)
                using (SolidBrush sb = new SolidBrush(Color.Black))
                using (Pen sp = new Pen(Color.FromArgb(100, 105, 115), 1.0f))
                {
                    g.FillRectangle(sb, swatchX, swatchY, 18, 18);
                    g.DrawRectangle(sp, swatchX, swatchY, 18, 18);
                }
                using (Font txtFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.DrawString("Automatic", txtFont, tb, swatchX + 28, curY + 4);
                }
            }
            else
            {
                // Word No Color Swatch: White square with thin red diagonal slash (18x18)
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
                    g.DrawString("Kh\u00f4ng c\u00f3 m\u00e0u (No Color)", txtFont, tb, swatchX + 28, curY + 4);
                }
            }

            curY += 28 + 8;

            // ==================== 2. THEME COLORS HEADER ====================
            using (Font headerFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush hb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Theme Colors", headerFont, hb, 14, curY);
            }
            curY += 20;

            // ==================== 3. THEME BASE COLORS (ROW 0 - SQUARES) ====================
            int startGridX = 14;
            int cellW = 17;
            int gapGridX = 3;

            for (int c = 0; c < 10; c++)
            {
                int cx = startGridX + c * (cellW + gapGridX);
                Rectangle cellRect = new Rectangle(cx, curY, cellW, 17);

                using (SolidBrush cb = new SolidBrush(ThemeMainColors[c]))
                {
                    g.FillRectangle(cb, cellRect);
                }

                bool isHovered = (_hoverSection == 2 && _hoverCol == c);
                using (Pen cp = new Pen(isHovered ? Color.FromArgb(245, 158, 11) : Color.FromArgb(205, 210, 218), isHovered ? 1.6f : 1.0f))
                {
                    g.DrawRectangle(cp, cellRect.X, cellRect.Y, cellRect.Width - 1, cellRect.Height - 1);
                }
            }
            curY += 17 + 4;

            // ==================== 4. THEME SHADES (SEAMLESS SOLID VERTICAL STRIPS) ====================
            // Exact Word standard: 5 shades stacked seamlessly without any horizontal divider lines
            for (int c = 0; c < 10; c++)
            {
                int cx = startGridX + c * (cellW + gapGridX);

                // 1. Fill the 5 stacked shades without inner borders
                for (int r = 0; r < 5; r++)
                {
                    int cy = curY + r * 13;
                    Rectangle shadeRect = new Rectangle(cx, cy, cellW, 13);
                    using (SolidBrush cb = new SolidBrush(ThemeShades[r, c]))
                    {
                        g.FillRectangle(cb, shadeRect);
                    }
                }

                // 2. Draw ONE clean outer border enclosing the whole vertical column strip
                using (Pen stripPen = new Pen(Color.FromArgb(215, 220, 228), 1.0f))
                {
                    g.DrawRectangle(stripPen, cx, curY, cellW - 1, 65 - 1);
                }

                // 3. Highlight ONLY the specific hovered shade with an amber border
                if (_hoverSection == 3 && _hoverCol == c && _hoverRow >= 0 && _hoverRow < 5)
                {
                    int hovY = curY + _hoverRow * 13;
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

            // ==================== 5. STANDARD COLORS ====================
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

                bool isHovered = (_hoverSection == 4 && _hoverCol == c);
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

            // ==================== 6. MORE COLORS... (ARTIST PALETTE ICON) ====================
            Rectangle moreBtnRect = new Rectangle(6, curY, Width - 12, 26);
            bool isMoreHovered = (_hoverSection == 5);
            if (isMoreHovered)
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

            // ==================== 7. GRADIENT > (GRADIENT ICON) ====================
            Rectangle gradBtnRect = new Rectangle(6, curY, Width - 12, 26);
            bool isGradHovered = (_hoverSection == 6);
            if (isGradHovered)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, gradBtnRect);
                    g.DrawRectangle(hp, gradBtnRect.X, gradBtnRect.Y, gradBtnRect.Width - 1, gradBtnRect.Height - 1);
                }
            }

            DrawGradientIcon(g, 15, curY + 5);
            using (Font itemFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Gradient", itemFont, tb, 42, curY + 4);
                g.DrawString(">", itemFont, Brushes.Gray, Width - 20, curY + 4);
            }
            curY += 27;

            // ==================== 8. EYEDROPPER (VECTOR PIPETTE ICON) ====================
            Rectangle eyeBtnRect = new Rectangle(6, curY, Width - 12, 26);
            bool isEyeHovered = (_hoverSection == 7);
            if (isEyeHovered)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, eyeBtnRect);
                    g.DrawRectangle(hp, eyeBtnRect.X, eyeBtnRect.Y, eyeBtnRect.Width - 1, eyeBtnRect.Height - 1);
                }
            }

            DrawEyedropperIcon(g, 15, curY + 5);
            using (Font itemFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Eyedropper", itemFont, tb, 42, curY + 4);
            }
        }

        private void PaintHighlightMode(Graphics g)
        {
            int startX = 14;
            int startY = 10;
            int cellW = 34;
            int cellH = 24;
            int gapX = 4;
            int gapY = 4;

            for (int i = 0; i < HighlightColors.Length; i++)
            {
                int r = i / 5;
                int c = i % 5;
                int cx = startX + c * (cellW + gapX);
                int cy = startY + r * (cellH + gapY);
                Rectangle cellRect = new Rectangle(cx, cy, cellW, cellH);

                using (SolidBrush b = new SolidBrush(HighlightColors[i]))
                {
                    g.FillRectangle(b, cellRect);
                }

                bool isHovered = (_hoverSection == 10 && _hoverCol == i);
                using (Pen p = new Pen(isHovered ? Color.FromArgb(245, 158, 11) : Color.FromArgb(200, 208, 218), isHovered ? 1.6f : 1.0f))
                {
                    g.DrawRectangle(p, cellRect.X, cellRect.Y, cellRect.Width - 1, cellRect.Height - 1);
                }
            }

            int noColY = startY + 3 * (cellH + gapY) + 6;
            Rectangle noColorBtn = new Rectangle(6, noColY, Width - 12, 24);
            if (_hoverSection == 1)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, noColorBtn);
                    g.DrawRectangle(hp, noColorBtn.X, noColorBtn.Y, noColorBtn.Width - 1, noColorBtn.Height - 1);
                }
            }

            using (SolidBrush sb = new SolidBrush(Color.White))
            using (Pen sp = new Pen(Color.FromArgb(180, 185, 195), 1.0f))
            {
                g.FillRectangle(sb, 14, noColY + 4, 16, 16);
                g.DrawRectangle(sp, 14, noColY + 4, 16, 16);
            }
            using (Pen redSlash = new Pen(Color.FromArgb(220, 38, 38), 1.5f))
            {
                g.DrawLine(redSlash, 14 + 2, noColY + 4 + 14, 14 + 14, noColY + 4 + 2);
            }
            using (Font itemFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("No Color", itemFont, tb, 42, noColY + 3);
            }

            int stopY = noColY + 26;
            Rectangle stopBtn = new Rectangle(6, stopY, Width - 12, 24);
            if (_hoverSection == 11)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(237, 243, 251)))
                using (Pen hp = new Pen(Color.FromArgb(185, 209, 234), 1.0f))
                {
                    g.FillRectangle(hb, stopBtn);
                    g.DrawRectangle(hp, stopBtn.X, stopBtn.Y, stopBtn.Width - 1, stopBtn.Height - 1);
                }
            }
            using (Font itemFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Stop Highlighting", itemFont, tb, 14, stopY + 3);
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

        private void DrawEyedropperIcon(Graphics g, int x, int y)
        {
            GraphicsState gs = g.Save();
            g.TranslateTransform(x + 8, y + 8);
            g.RotateTransform(45f);

            // Rubber bulb at top (y from -8 to -4)
            using (SolidBrush bulbBrush = new SolidBrush(Color.FromArgb(50, 58, 70)))
            using (Pen darkPen = new Pen(Color.FromArgb(30, 38, 50), 1.0f))
            {
                GraphicsPath bulb = new GraphicsPath();
                bulb.AddArc(-3f, -8f, 6f, 5f, 180, 180);
                bulb.AddLine(3f, -5.5f, 2.2f, -4f);
                bulb.AddLine(2.2f, -4f, -2.2f, -4f);
                bulb.AddLine(-2.2f, -4f, -3f, -5.5f);
                bulb.CloseFigure();
                g.FillPath(bulbBrush, bulb);
                g.DrawPath(darkPen, bulb);
            }

            // Metal collar (y from -4 to -2.5)
            using (SolidBrush metal = new SolidBrush(Color.FromArgb(180, 190, 205)))
            using (Pen metalPen = new Pen(Color.FromArgb(120, 130, 145), 0.8f))
            {
                g.FillRectangle(metal, -2.5f, -4f, 5f, 1.8f);
                g.DrawRectangle(metalPen, -2.5f, -4f, 5f, 1.8f);
            }

            // Glass body (y from -2.2 to 3.5)
            using (SolidBrush glass = new SolidBrush(Color.FromArgb(235, 243, 250)))
            using (Pen glassPen = new Pen(Color.FromArgb(70, 85, 105), 1.0f))
            {
                g.FillRectangle(glass, -2f, -2.2f, 4f, 5.7f);

                // Ink liquid inside bottom of glass body
                using (SolidBrush ink = new SolidBrush(Color.FromArgb(0, 120, 212)))
                {
                    g.FillRectangle(ink, -1.5f, 0.5f, 3f, 3f);
                }

                g.DrawRectangle(glassPen, -2f, -2.2f, 4f, 5.7f);

                // Specular reflection streak on glass
                using (Pen specPen = new Pen(Color.White, 0.8f))
                {
                    g.DrawLine(specPen, -1.2f, -1.5f, -1.2f, 2.5f);
                }
            }

            // Tapered glass nozzle / tip (y from 3.5 to 7.5)
            using (SolidBrush tipGlass = new SolidBrush(Color.FromArgb(0, 120, 212)))
            using (Pen tipPen = new Pen(Color.FromArgb(70, 85, 105), 1.0f))
            {
                PointF[] tip = new PointF[] {
                    new PointF(-2f, 3.5f),
                    new PointF(2f, 3.5f),
                    new PointF(0.8f, 6.5f),
                    new PointF(-0.8f, 6.5f)
                };
                g.FillPolygon(tipGlass, tip);
                g.DrawPolygon(tipPen, tip);

                // Needle orifice
                g.FillRectangle(Brushes.White, -0.6f, 6.5f, 1.2f, 1.2f);
                g.DrawRectangle(tipPen, -0.6f, 6.5f, 1.2f, 1.2f);
            }

            g.Restore(gs);

            // Tiny ink drop suspended at bottom-left
            using (SolidBrush drop = new SolidBrush(Color.FromArgb(0, 120, 212)))
            {
                g.FillEllipse(drop, x + 2.5f, y + 13.5f, 2.5f, 2.5f);
            }
        }

        private void StartEyedropperMode()
        {
            Hide();
            Form overlay = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                WindowState = FormWindowState.Maximized,
                TopMost = true,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Opacity = 0.01,
                Cursor = Cursors.Cross
            };

            overlay.MouseDown += (s, e) =>
            {
                Point screenPt = Cursor.Position;
                Color picked = GetPixelColorAt(screenPt.X, screenPt.Y);
                overlay.Close();
                overlay.Dispose();
                SelectColorAndClose(picked);
            };

            overlay.KeyDown += (s, e) =>
            {
                overlay.Close();
                overlay.Dispose();
                Close();
            };

            overlay.Show();
        }
    }
}

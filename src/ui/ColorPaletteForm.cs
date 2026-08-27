using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ColorPaletteForm : Form
    {
        public Color SelectedColor { get; private set; }
        public event Action<Color> OnColorSelected;

        private bool isSubDialogOpening = false;
        private Color _currentColor;
        private bool _allowNoColor;
        private bool _isHighlight;

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

        // PowerPoint Theme Colors (10 Columns)
        private static readonly Color[] ThemeMainColors = new Color[]
        {
            Color.FromArgb(255, 255, 255), // White
            Color.FromArgb(0, 0, 0),       // Black
            Color.FromArgb(231, 230, 230), // Light Gray
            Color.FromArgb(68, 84, 106),   // Dark Slate (Theme 1)
            Color.FromArgb(68, 114, 196),  // Blue (Theme 2)
            Color.FromArgb(237, 125, 49),  // Orange (Theme 3)
            Color.FromArgb(165, 165, 165), // Gray (Theme 4)
            Color.FromArgb(255, 192, 0),   // Gold / Yellow (Theme 5)
            Color.FromArgb(91, 155, 213),  // Light Blue (Theme 6)
            Color.FromArgb(112, 173, 71)   // Green (Theme 7)
        };

        // 5 Shade variations for each of the 10 theme columns [5 rows, 10 cols]
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

        // PowerPoint Standard Colors (10 Colors)
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

        public ColorPaletteForm(Point screenLocation, bool isHighlight)
            : this(screenLocation, Color.Empty, !isHighlight, isHighlight)
        {
        }

        public ColorPaletteForm(Point screenLocation, Color currentColor = default(Color), bool allowNoColor = true, bool isHighlight = false)
        {
            _currentColor = currentColor;
            _allowNoColor = allowNoColor;
            _isHighlight = isHighlight;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int desiredW = 226;
            int desiredH = isHighlight ? 140 : (allowNoColor ? 310 : 280);
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
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            int curY = 6;

            // 1. Tùy chọn "Không màu (No Color) / Tự động (Automatic)"
            if (allowNoColor || isHighlight)
            {
                Panel pnlNoColor = new Panel
                {
                    Location = new Point(10, curY),
                    Size = new Size(206, 24),
                    BackColor = Color.White,
                    Cursor = Cursors.Hand
                };
                pnlNoColor.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Box with red diagonal slash
                    Rectangle box = new Rectangle(4, 3, 16, 16);
                    g.DrawRectangle(Pens.Silver, box);
                    if (isHighlight || allowNoColor)
                    {
                        using (Pen redSlash = new Pen(Color.FromArgb(239, 68, 68), 1.5f))
                        {
                            g.DrawLine(redSlash, box.Left + 2, box.Bottom - 2, box.Right - 2, box.Top + 2);
                        }
                    }

                    using (Font f = new Font("Segoe UI", 9.0f))
                    {
                        g.DrawString(isHighlight ? "Không màu (No Color)" : "Không có màu (No Color)", f, Brushes.Black, 26, 2);
                    }
                };
                pnlNoColor.MouseEnter += (s, e) => { pnlNoColor.BackColor = Color.FromArgb(240, 245, 255); };
                pnlNoColor.MouseLeave += (s, e) => { pnlNoColor.BackColor = Color.White; };
                pnlNoColor.Click += (s, e) =>
                {
                    SelectedColor = Color.Transparent;
                    if (OnColorSelected != null) OnColorSelected(SelectedColor);
                    Close();
                };
                pnlMain.Controls.Add(pnlNoColor);
                curY += 28;
            }

            if (isHighlight)
            {
                // Simple highlight colors grid
                AddHighlightGrid(pnlMain, ref curY);
            }
            else
            {
                // 2. Theme Colors Header
                Label lblTheme = new Label
                {
                    Text = "Theme Colors",
                    Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Location = new Point(10, curY),
                    AutoSize = true
                };
                pnlMain.Controls.Add(lblTheme);
                curY += 20;

                // 3. Theme Main Row (10 Colors)
                int cellW = 18;
                int cellH = 16;
                int gap = 2;
                int startX = 12;

                for (int col = 0; col < 10; col++)
                {
                    Color c = ThemeMainColors[col];
                    Panel pCell = CreateColorTile(c, startX + col * (cellW + gap), curY, cellW, cellH);
                    pnlMain.Controls.Add(pCell);
                }
                curY += cellH + 4;

                // 4. Theme Shades Grid (5 Rows x 10 Cols)
                for (int row = 0; row < 5; row++)
                {
                    for (int col = 0; col < 10; col++)
                    {
                        Color c = ThemeShades[row, col];
                        Panel pCell = CreateColorTile(c, startX + col * (cellW + gap), curY + row * (cellH + 1), cellW, cellH);
                        pnlMain.Controls.Add(pCell);
                    }
                }
                curY += 5 * (cellH + 1) + 8;

                // 5. Divider Line
                Panel div1 = new Panel { Location = new Point(10, curY), Size = new Size(206, 1), BackColor = Color.FromArgb(226, 232, 240) };
                pnlMain.Controls.Add(div1);
                curY += 6;

                // 6. Standard Colors Header
                Label lblStandard = new Label
                {
                    Text = "Standard Colors",
                    Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Location = new Point(10, curY),
                    AutoSize = true
                };
                pnlMain.Controls.Add(lblStandard);
                curY += 20;

                // 7. Standard Colors Row (10 Colors)
                for (int col = 0; col < 10; col++)
                {
                    Color c = StandardColors[col];
                    Panel pCell = CreateColorTile(c, startX + col * (cellW + gap), curY, cellW, cellH);
                    pnlMain.Controls.Add(pCell);
                }
                curY += cellH + 8;

                // 8. Divider Line
                Panel div2 = new Panel { Location = new Point(10, curY), Size = new Size(206, 1), BackColor = Color.FromArgb(226, 232, 240) };
                pnlMain.Controls.Add(div2);
                // 10. Menu Item: Eyedropper (Hút màu)
                Panel pnlEye = new Panel { Location = new Point(10, curY), Size = new Size(206, 24), BackColor = Color.White, Cursor = Cursors.Hand };
                pnlEye.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Dropper pipette icon
                    using (Pen p = new Pen(Color.FromArgb(70, 80, 95), 1.5f))
                    {
                        PointF[] pipette = new PointF[] {
                            new PointF(15, 3), new PointF(18, 6), new PointF(12, 12), new PointF(7, 17), new PointF(5, 17), new PointF(5, 15), new PointF(10, 10)
                        };
                        g.DrawPolygon(p, pipette);
                    }

                    using (Font f = new Font("Segoe UI", 9.0f))
                    {
                        g.DrawString("Eyedropper", f, Brushes.Black, 26, 2);
                    }
                };
                pnlEye.MouseEnter += (s, e) => { pnlEye.BackColor = Color.FromArgb(240, 245, 255); };
                pnlEye.MouseLeave += (s, e) => { pnlEye.BackColor = Color.White; };
                pnlEye.Click += (s, e) =>
                {
                    StartEyedropperMode();
                };
                pnlMain.Controls.Add(pnlEye);
                curY += 28;
            }

            Controls.Add(pnlMain);
            Deactivate += (s, e) =>
            {
                if (!isSubDialogOpening) Close();
            };
        }

        private void AddHighlightGrid(Panel parent, ref int curY)
        {
            Color[] hlColors = new Color[] {
                Color.Yellow, Color.Lime, Color.Cyan, Color.Magenta, Color.Blue,
                Color.Red, Color.DarkBlue, Color.Teal, Color.Green, Color.Purple
            };

            int cellW = 18;
            int cellH = 18;
            int gap = 2;
            int startX = 12;

            for (int i = 0; i < hlColors.Length; i++)
            {
                Panel pCell = CreateColorTile(hlColors[i], startX + i * (cellW + gap), curY, cellW, cellH);
                parent.Controls.Add(pCell);
            }
            curY += cellH + 8;
        }

        private Panel CreateColorTile(Color col, int x, int y, int w, int h)
        {
            Panel p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = col,
                Cursor = Cursors.Hand
            };

            bool isSelected = (_currentColor.ToArgb() == col.ToArgb());

            p.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                if (isSelected)
                {
                    using (Pen activePen = new Pen(Color.FromArgb(237, 125, 49), 2.0f))
                    {
                        g.DrawRectangle(activePen, 0, 0, w - 1, h - 1);
                    }
                }
                else
                {
                    using (Pen borderPen = new Pen(Color.FromArgb(215, 220, 228), 1.0f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
                    }
                }
            };

            p.MouseEnter += (s, e) =>
            {
                using (Graphics g = p.CreateGraphics())
                using (Pen hoverPen = new Pen(Color.FromArgb(245, 158, 11), 2.0f))
                {
                    g.DrawRectangle(hoverPen, 0, 0, w - 1, h - 1);
                }
            };

            p.MouseLeave += (s, e) => p.Invalidate();

            p.Click += (s, e) =>
            {
                SelectedColor = col;
                if (OnColorSelected != null) OnColorSelected(SelectedColor);
                Close();
            };

            return p;
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
                SelectedColor = picked;
                if (OnColorSelected != null) OnColorSelected(SelectedColor);
                Close();
            };

            overlay.KeyDown += (s, e) =>
            {
                overlay.Close();
                overlay.Dispose();
                Close();
            };

            overlay.Show();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 185, 195), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

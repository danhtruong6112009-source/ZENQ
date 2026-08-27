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

        private int HoverRow = -1;
        private int HoverCol = -1;
        private int HoverAction = -1; // 0: No Color, 1: More Colors, 2: Fill Effects

        // Theme colors palette (10 columns x 6 rows)
        private Color[,] ThemeColors;
        // Standard colors (10 colors)
        private Color[] StandardColors;

        public PageColorDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location;
            this.Size = new Size(240, 310);
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            InitPalette();

            this.MouseMove += (s, e) =>
            {
                int prevR = HoverRow;
                int prevC = HoverCol;
                int prevA = HoverAction;

                HoverRow = -1;
                HoverCol = -1;
                HoverAction = -1;

                // Theme Colors Grid: X: 15..215, Y: 32..136 (10 cols x 6 rows, cell 18x16)
                if (e.X >= 15 && e.X <= 215 && e.Y >= 32 && e.Y <= 136)
                {
                    HoverCol = (e.X - 15) / 20;
                    HoverRow = (e.Y - 32) / 17;
                    if (HoverCol > 9) HoverCol = 9;
                    if (HoverRow > 5) HoverRow = 5;
                }
                // Standard Colors: X: 15..215, Y: 162..180 (10 cols, cell 18x18)
                else if (e.X >= 15 && e.X <= 215 && e.Y >= 162 && e.Y <= 180)
                {
                    HoverCol = (e.X - 15) / 20;
                    HoverRow = 10; // flag for standard
                    if (HoverCol > 9) HoverCol = 9;
                }
                // Actions below
                else if (e.Y >= 195 && e.Y <= 225) HoverAction = 0; // No Color
                else if (e.Y >= 230 && e.Y <= 258) HoverAction = 1; // More Colors
                else if (e.Y >= 260 && e.Y <= 288) HoverAction = 2; // Fill Effects

                if (prevR != HoverRow || prevC != HoverCol || prevA != HoverAction) Invalidate();
            };

            this.Deactivate += (s, e) => this.Close();
        }

        private void InitPalette()
        {
            StandardColors = new Color[] {
                Color.FromArgb(192, 0, 0), Color.FromArgb(255, 0, 0), Color.FromArgb(255, 192, 0),
                Color.FromArgb(255, 255, 0), Color.FromArgb(146, 208, 80), Color.FromArgb(0, 176, 80),
                Color.FromArgb(0, 176, 240), Color.FromArgb(0, 112, 192), Color.FromArgb(0, 32, 96),
                Color.FromArgb(112, 48, 160)
            };

            Color[] topRow = new Color[] {
                Color.White, Color.Black, Color.FromArgb(231, 230, 230), Color.FromArgb(68, 84, 106),
                Color.FromArgb(91, 155, 213), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165),
                Color.FromArgb(255, 192, 0), Color.FromArgb(68, 114, 196), Color.FromArgb(112, 173, 71)
            };

            ThemeColors = new Color[6, 10];
            for (int c = 0; c < 10; c++) ThemeColors[0, c] = topRow[c];

            // Generated lighter and darker tints
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(200, 205, 215), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            using (Font groupFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font itemFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            {
                // 1. Theme Colors
                g.DrawString("Màu chủ đề (Theme Colors)", groupFont, Brushes.Black, 15, 10);

                int cellW = 18;
                int cellH = 14;

                for (int r = 0; r < 6; r++)
                {
                    int yPos = 32 + r * 17;
                    if (r == 1) yPos += 2; // small gap after top row
                    for (int c = 0; c < 10; c++)
                    {
                        int xPos = 15 + c * 20;
                        Rectangle rect = new Rectangle(xPos, yPos, cellW, cellH);
                        using (SolidBrush b = new SolidBrush(ThemeColors[r, c])) g.FillRectangle(b, rect);
                        using (Pen p = new Pen(HoverRow == r && HoverCol == c ? Color.Orange : Color.FromArgb(215, 215, 215), HoverRow == r && HoverCol == c ? 1.5f : 1f))
                            g.DrawRectangle(p, rect);
                    }
                }

                // 2. Standard Colors
                g.DrawString("Màu tiêu chuẩn (Standard Colors)", groupFont, Brushes.Black, 15, 142);
                for (int c = 0; c < 10; c++)
                {
                    int xPos = 15 + c * 20;
                    Rectangle rect = new Rectangle(xPos, 162, cellW, 16);
                    using (SolidBrush b = new SolidBrush(StandardColors[c])) g.FillRectangle(b, rect);
                    using (Pen p = new Pen(HoverRow == 10 && HoverCol == c ? Color.Orange : Color.FromArgb(215, 215, 215), HoverRow == 10 && HoverCol == c ? 1.5f : 1f))
                        g.DrawRectangle(p, rect);
                }

                // Divider line
                g.DrawLine(Pens.LightGray, 0, 188, Width, 188);

                // 3. No Color (Không màu)
                Rectangle noColorBox = new Rectangle(15, 198, 22, 22);
                using (SolidBrush hb = new SolidBrush(HoverAction == 0 ? Color.FromArgb(235, 243, 255) : Color.White))
                {
                    g.FillRectangle(hb, 4, 196, Width - 8, 26);
                }
                g.DrawRectangle(Pens.Gray, noColorBox);
                using (Pen redSlash = new Pen(Color.Red, 1.5f))
                {
                    g.DrawLine(redSlash, noColorBox.Left, noColorBox.Bottom, noColorBox.Right, noColorBox.Top);
                }
                g.DrawString("Không màu (No Color)", itemFont, Brushes.Black, 44, 199);

                // 4. More Colors
                using (SolidBrush hb = new SolidBrush(HoverAction == 1 ? Color.FromArgb(235, 243, 255) : Color.White))
                {
                    g.FillRectangle(hb, 4, 230, Width - 8, 26);
                }
                g.DrawString("🎨  Thêm màu khác (More Colors)...", itemFont, Brushes.Black, 15, 234);

                // 5. Fill Effects
                using (SolidBrush hb = new SolidBrush(HoverAction == 2 ? Color.FromArgb(235, 243, 255) : Color.White))
                {
                    g.FillRectangle(hb, 4, 260, Width - 8, 26);
                }
                g.DrawString("✨  Hiệu ứng nền (Fill Effects)...", itemFont, Brushes.Black, 15, 264);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Theme colors click
            if (HoverRow >= 0 && HoverRow < 6 && HoverCol >= 0 && HoverCol < 10)
            {
                if (OnColorSelected != null) OnColorSelected(ThemeColors[HoverRow, HoverCol]);
                this.Close();
                return;
            }

            // Standard colors click
            if (HoverRow == 10 && HoverCol >= 0 && HoverCol < 10)
            {
                if (OnColorSelected != null) OnColorSelected(StandardColors[HoverCol]);
                this.Close();
                return;
            }

            // No color click
            if (HoverAction == 0)
            {
                if (OnNoColorSelected != null) OnNoColorSelected();
                this.Close();
                return;
            }

            // More colors click
            if (HoverAction == 1)
            {
                if (OnMoreColorsClicked != null) OnMoreColorsClicked();
                this.Close();
                return;
            }

            // Fill effects click
            if (HoverAction == 2)
            {
                if (OnColorSelected != null) OnColorSelected(Color.FromArgb(250, 248, 240));
                this.Close();
                return;
            }
        }
    }
}

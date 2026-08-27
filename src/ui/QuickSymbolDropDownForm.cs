using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class QuickSymbolDropDownForm : Form
    {
        public Action<string> OnSymbolSelected;
        public Action OnMoreSymbolsClicked;

        private string[] QuickSymbols = new string[] {
            "€", "£", "¥", "©", "®",
            "™", "±", "≠", "≤", "≥",
            "÷", "×", "∞", "µ", "α",
            "β", "π", "Ω", "∑", "☺"
        };

        private int HoverCol = -1;
        private int HoverRow = -1;
        private bool HoverMore = false;

        public QuickSymbolDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(185, 195);

            Rectangle screen = Screen.FromPoint(location).WorkingArea;
            if (location.X + this.Width > screen.Right)
            {
                location.X = screen.Right - this.Width - 10;
            }
            if (location.Y + this.Height > screen.Bottom)
            {
                location.Y = screen.Bottom - this.Height - 10;
            }
            this.Location = location;

            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            this.MouseMove += (s, e) =>
            {
                int prevR = HoverRow;
                int prevC = HoverCol;
                bool prevM = HoverMore;

                HoverRow = -1;
                HoverCol = -1;
                HoverMore = false;

                if (e.X >= 10 && e.X <= 175 && e.Y >= 10 && e.Y <= 150)
                {
                    HoverCol = (e.X - 10) / 33;
                    HoverRow = (e.Y - 10) / 35;
                    if (HoverCol > 4) HoverCol = 4;
                    if (HoverRow > 3) HoverRow = 3;
                }
                else if (e.Y >= 155 && e.Y <= 188)
                {
                    HoverMore = true;
                }

                if (prevR != HoverRow || prevC != HoverCol || prevM != HoverMore) Invalidate();
            };

            this.Deactivate += (s, e) => this.Close();
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

            using (Font symFont = new Font("Segoe UI", 12.0f, FontStyle.Regular))
            using (Font btnFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            {
                // 5 columns x 4 rows
                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        int idx = r * 5 + c;
                        if (idx >= QuickSymbols.Length) break;

                        int x = 10 + c * 33;
                        int y = 10 + r * 35;
                        Rectangle rect = new Rectangle(x, y, 31, 32);

                        bool isHover = (HoverRow == r && HoverCol == c);
                        if (isHover)
                        {
                            using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255)))
                            {
                                g.FillRectangle(hb, rect);
                            }
                            using (Pen hp = new Pen(Color.FromArgb(68, 114, 196), 1.2f))
                            {
                                g.DrawRectangle(hp, rect);
                            }
                        }

                        SizeF sz = g.MeasureString(QuickSymbols[idx], symFont);
                        g.DrawString(QuickSymbols[idx], symFont, Brushes.Black, x + (rect.Width - sz.Width) / 2, y + (rect.Height - sz.Height) / 2);
                    }
                }

                // Divider line
                g.DrawLine(Pens.LightGray, 0, 152, Width, 152);

                // More Symbols Row
                Rectangle moreRect = new Rectangle(4, 156, Width - 8, 30);
                if (HoverMore)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255)))
                    {
                        g.FillRectangle(hb, moreRect);
                    }
                }

                using (Font omFont = new Font("Segoe UI", 11.0f, FontStyle.Bold))
                {
                    g.DrawString("Ω", omFont, Brushes.Navy, 12, 160);
                }
                g.DrawString("Thêm Ký hiệu (More Symbols)...", btnFont, Brushes.Black, 32, 163);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (HoverRow >= 0 && HoverRow < 4 && HoverCol >= 0 && HoverCol < 5)
            {
                int idx = HoverRow * 5 + HoverCol;
                if (idx < QuickSymbols.Length)
                {
                    if (OnSymbolSelected != null) OnSymbolSelected(QuickSymbols[idx]);
                    this.Close();
                    return;
                }
            }

            if (HoverMore)
            {
                this.Close();
                if (OnMoreSymbolsClicked != null) OnMoreSymbolsClicked();
                return;
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Microsoft 365 Authentic Quick Symbol Dropdown Popup
    /// Rendered entirely via Direct2D GPU (D2DGraphics) with DirectWrite ClearType for razor-sharp symbols.
    /// </summary>
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

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (D2DGraphics g = new D2DGraphics(e.Graphics, Width, Height))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Background
                using (SolidBrush bg = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bg, 0, 0, Width, Height);
                }

                // Border
                using (Pen borderPen = new Pen(Color.FromArgb(204, 208, 214), 1.0f))
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
                                using (Pen hp = new Pen(Color.FromArgb(68, 114, 196), 1.0f))
                                {
                                    g.DrawRectangle(hp, rect);
                                }
                            }

                            SizeF sz = g.MeasureString(QuickSymbols[idx], symFont);
                            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                            {
                                g.DrawString(QuickSymbols[idx], symFont, textBrush, x + (rect.Width - sz.Width) / 2, y + (rect.Height - sz.Height) / 2);
                            }
                        }
                    }

                    // Divider line
                    using (Pen divPen = new Pen(Color.FromArgb(226, 230, 235), 1.0f))
                    {
                        g.DrawLine(divPen, 1, 152, Width - 2, 152);
                    }

                    // More Symbols Row
                    Rectangle moreRect = new Rectangle(4, 156, Width - 8, 32);
                    if (HoverMore)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 244, 254)))
                        {
                            g.FillRectangle(hb, moreRect);
                        }
                    }

                    using (Font omFont = new Font("Segoe UI", 11.0f, FontStyle.Bold))
                    using (SolidBrush omBrush = new SolidBrush(Color.FromArgb(0, 95, 184)))
                    using (SolidBrush moreTextBrush = new SolidBrush(Color.FromArgb(20, 20, 20)))
                    {
                        g.DrawString("Ω", omFont, omBrush, 12, 161);
                        g.DrawString("Thêm Ký hiệu (More Symbols)...", btnFont, moreTextBrush, 32, 163);
                    }
                }
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

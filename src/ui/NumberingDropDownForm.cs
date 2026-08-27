using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class NumberingDropDownForm : Form
    {
        private int _hoverBoxIndex = -1;
        private int _hoverBottomIndex = -1;
        private readonly Action<int> _callback;

        public static readonly string[] NumberingFormatNames = new string[]
        {
            "None", "1.", "1)", "I.", "A.", "a)", "a.", "i."
        };

        public NumberingDropDownForm(Point screenPos, Action<int> callback)
        {
            _callback = callback;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int w = 210;
            int h = 275;

            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            Deactivate += (s, e) => { Close(); };
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int oldBox = _hoverBoxIndex;
            int oldBottom = _hoverBottomIndex;

            _hoverBoxIndex = -1;
            _hoverBottomIndex = -1;

            // Check Grid Boxes (3 columns, 3 rows, X: 12..196, Y: 28..196)
            int boxW = 58, boxH = 52, startX = 10, startY = 28;
            for (int i = 0; i < 8; i++)
            {
                int r = i / 3;
                int c = i % 3;
                Rectangle rBox = new Rectangle(startX + c * (boxW + 6), startY + r * (boxH + 6), boxW, boxH);
                if (rBox.Contains(e.Location))
                {
                    _hoverBoxIndex = i;
                    break;
                }
            }

            // Check Bottom Links (Y: 204 to 275)
            if (e.Y >= 204 && e.Y < 228) _hoverBottomIndex = 0; // Change List Level
            else if (e.Y >= 228 && e.Y < 252) _hoverBottomIndex = 1; // Define New Number Format...
            else if (e.Y >= 252 && e.Y <= 275) _hoverBottomIndex = 2; // Set Numbering Value...

            if (oldBox != _hoverBoxIndex || oldBottom != _hoverBottomIndex)
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverBoxIndex = -1;
            _hoverBottomIndex = -1;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_hoverBoxIndex >= 0 && _hoverBoxIndex < NumberingFormatNames.Length)
            {
                int chosen = _hoverBoxIndex;
                Close();
                if (_callback != null) _callback(chosen);
            }
            else if (_hoverBottomIndex >= 0)
            {
                int chosen = _hoverBottomIndex == 0 ? 1 : 1; // Default to 1. 2. 3.
                Close();
                if (_callback != null) _callback(chosen);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Form Border
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Title
            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Numbering Library", titleFont, titleBrush, 10, 8);
            }

            // Grid Boxes
            int boxW = 58, boxH = 52, startX = 10, startY = 28;
            using (Font boxFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
            {
                for (int i = 0; i < 8; i++)
                {
                    int r = i / 3;
                    int c = i % 3;
                    Rectangle rBox = new Rectangle(startX + c * (boxW + 6), startY + r * (boxH + 6), boxW, boxH);

                    bool isHover = (_hoverBoxIndex == i);
                    if (isHover)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(239, 246, 255)))
                        using (Pen hp = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
                        {
                            g.FillRectangle(hb, rBox);
                            g.DrawRectangle(hp, rBox);
                        }
                    }
                    else
                    {
                        using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1))
                        {
                            g.DrawRectangle(p, rBox);
                        }
                    }

                    // Render Preview Lines
                    if (i == 0) // None
                    {
                        using (SolidBrush bNone = new SolidBrush(Color.FromArgb(30, 41, 59)))
                        {
                            SizeF sz = g.MeasureString("None", boxFont);
                            g.DrawString("None", boxFont, bNone, rBox.X + (rBox.Width - sz.Width) / 2, rBox.Y + (rBox.Height - sz.Height) / 2);
                        }
                    }
                    else
                    {
                        string[] p1 = new string[3];
                        if (i == 1) p1 = new string[] { "1.", "2.", "3." };
                        else if (i == 2) p1 = new string[] { "1)", "2)", "3)" };
                        else if (i == 3) p1 = new string[] { "I.", "II.", "III." };
                        else if (i == 4) p1 = new string[] { "A.", "B.", "C." };
                        else if (i == 5) p1 = new string[] { "a)", "b)", "c)" };
                        else if (i == 6) p1 = new string[] { "a.", "b.", "c." };
                        else if (i == 7) p1 = new string[] { "i.", "ii.", "iii." };

                        for (int k = 0; k < 3; k++)
                        {
                            float yk = rBox.Y + 6 + k * 14;
                            g.DrawString(p1[k], boxFont, Brushes.Black, rBox.X + 4, yk - 1);
                            SizeF numSz = g.MeasureString(p1[k], boxFont);
                            float lx = rBox.X + 6 + numSz.Width;
                            float rx = rBox.Right - 6;
                            if (rx > lx)
                            {
                                g.DrawLine(linePen, lx, yk + 6, rx, yk + 6);
                            }
                        }
                    }
                }
            }

            // Divider line
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                g.DrawLine(divPen, 8, 202, Width - 8, 202);
            }

            // Bottom Links
            using (Font linkFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            {
                // Item 1: Change List Level
                Rectangle link1 = new Rectangle(6, 205, Width - 12, 22);
                if (_hoverBottomIndex == 0)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillRectangle(hb, link1);
                    }
                }
                using (Pen arrPen = new Pen(Color.FromArgb(71, 85, 105), 1.2f))
                {
                    g.DrawLine(arrPen, 14, 216, 22, 216);
                    g.DrawLine(arrPen, 19, 213, 22, 216);
                    g.DrawLine(arrPen, 19, 219, 22, 216);
                }
                g.DrawString("Change List Level", linkFont, Brushes.Black, 28, 208);
                using (Pen chevPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
                {
                    g.DrawLine(chevPen, Width - 18, 212, Width - 14, 216);
                    g.DrawLine(chevPen, Width - 14, 216, Width - 18, 220);
                }

                // Item 2: Define New Number Format...
                Rectangle link2 = new Rectangle(6, 228, Width - 12, 22);
                if (_hoverBottomIndex == 1)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillRectangle(hb, link2);
                    }
                }
                g.DrawString("Define New Number Format...", linkFont, Brushes.Black, 28, 231);

                // Item 3: Set Numbering Value...
                Rectangle link3 = new Rectangle(6, 251, Width - 12, 22);
                if (_hoverBottomIndex == 2)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillRectangle(hb, link3);
                    }
                }
                g.DrawString("Set Numbering Value...", linkFont, Brushes.Black, 28, 254);
            }
        }
    }
}

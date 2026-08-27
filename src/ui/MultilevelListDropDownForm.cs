using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class MultilevelListDropDownForm : Form
    {
        private int _hoverBoxIndex = -1;
        private int _hoverBottomIndex = -1;
        private readonly Action<int> _callback;

        public MultilevelListDropDownForm(Point screenPos, Action<int> callback)
        {
            _callback = callback;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int w = 220;
            int h = 340;

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

            // Current List box: Y: 22..68
            Rectangle curBox = new Rectangle(10, 24, 60, 48);
            if (curBox.Contains(e.Location))
            {
                _hoverBoxIndex = 1;
            }

            // List Library boxes: 3 columns, 3 rows (Y: 96..260)
            int boxW = 60, boxH = 48, startX = 10, startY = 96;
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

            // Bottom links (Y: 268 to 340)
            if (e.Y >= 268 && e.Y < 292) _hoverBottomIndex = 0; // Change List Level
            else if (e.Y >= 292 && e.Y < 316) _hoverBottomIndex = 1; // Define New Multilevel List...
            else if (e.Y >= 316 && e.Y <= 340) _hoverBottomIndex = 2; // Define New List Style...

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
            if (_hoverBoxIndex >= 0)
            {
                int chosen = _hoverBoxIndex;
                Close();
                if (_callback != null) _callback(chosen);
            }
            else if (_hoverBottomIndex >= 0)
            {
                Close();
                if (_callback != null) _callback(1);
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

            using (Font sectionFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font boxFont = new Font("Segoe UI", 7.0f, FontStyle.Regular))
            using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
            {
                // 1. Current List Section
                g.DrawString("Current List", sectionFont, Brushes.Black, 10, 6);
                Rectangle curBox = new Rectangle(10, 24, 60, 48);
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(p, curBox);
                }
                DrawListPreview(g, curBox, boxFont, linePen, 1);

                // 2. List Library Section
                using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    g.DrawLine(divPen, 8, 78, Width - 8, 78);
                }
                g.DrawString("List Library", sectionFont, Brushes.Black, 10, 82);

                int boxW = 60, boxH = 48, startX = 10, startY = 100;
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

                    DrawListPreview(g, rBox, boxFont, linePen, i);
                }

                // Divider line
                using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    g.DrawLine(divPen, 8, 266, Width - 8, 266);
                }

                // Bottom Links
                using (Font linkFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
                {
                    // Item 1: Change List Level
                    Rectangle link1 = new Rectangle(6, 269, Width - 12, 22);
                    if (_hoverBottomIndex == 0)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                            g.FillRectangle(hb, link1);
                    }
                    using (Pen arrPen = new Pen(Color.FromArgb(71, 85, 105), 1.2f))
                    {
                        g.DrawLine(arrPen, 14, 280, 22, 280);
                        g.DrawLine(arrPen, 19, 277, 22, 280);
                        g.DrawLine(arrPen, 19, 283, 22, 280);
                    }
                    g.DrawString("Change List Level", linkFont, Brushes.Black, 28, 272);
                    using (Pen chevPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
                    {
                        g.DrawLine(chevPen, Width - 18, 276, Width - 14, 280);
                        g.DrawLine(chevPen, Width - 14, 280, Width - 18, 284);
                    }

                    // Item 2: Define New Multilevel List...
                    Rectangle link2 = new Rectangle(6, 292, Width - 12, 22);
                    if (_hoverBottomIndex == 1)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                            g.FillRectangle(hb, link2);
                    }
                    g.DrawString("Define New Multilevel List...", linkFont, Brushes.Black, 28, 295);

                    // Item 3: Define New List Style...
                    Rectangle link3 = new Rectangle(6, 315, Width - 12, 22);
                    if (_hoverBottomIndex == 2)
                    {
                        using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                            g.FillRectangle(hb, link3);
                    }
                    g.DrawString("Define New List Style...", linkFont, Brushes.Black, 28, 318);
                }
            }
        }

        private void DrawListPreview(Graphics g, Rectangle rBox, Font boxFont, Pen linePen, int index)
        {
            if (index == 0) // None
            {
                using (SolidBrush bNone = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    SizeF sz = g.MeasureString("None", boxFont);
                    g.DrawString("None", boxFont, bNone, rBox.X + (rBox.Width - sz.Width) / 2, rBox.Y + (rBox.Height - sz.Height) / 2);
                }
                return;
            }

            string[] p = new string[3];
            int[] ind = new int[] { 2, 7, 12 };
            if (index == 1) p = new string[] { "1)", "a)", "i)" };
            else if (index == 2) p = new string[] { "1.", "1.1.", "1.1.1." };
            else if (index == 3) p = new string[] { "❖", "➢", "■" };
            else if (index == 4) p = new string[] { "Art. I", "Sec. 1", "(a)" };
            else if (index == 5) p = new string[] { "1 H1", "1.1 H2", "1.1.1" };
            else if (index == 6) p = new string[] { "I. H1", "A. H2", "1. H3" };
            else if (index == 7) p = new string[] { "Ch. 1", "H2", "H3" };

            for (int k = 0; k < 3; k++)
            {
                float yk = rBox.Y + 4 + k * 13;
                float xk = rBox.X + ind[k];
                g.DrawString(p[k], boxFont, Brushes.Black, xk, yk);
                SizeF sz = g.MeasureString(p[k], boxFont);
                float lx = xk + sz.Width + 2;
                float rx = rBox.Right - 4;
                if (rx > lx)
                {
                    g.DrawLine(linePen, lx, yk + 6, rx, yk + 6);
                }
            }
        }
    }
}

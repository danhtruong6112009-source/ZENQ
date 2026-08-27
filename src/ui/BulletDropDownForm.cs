using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class BulletDropDownForm : Form
    {
        private int _hoverBoxIndex = -1;
        private int _hoverBottomIndex = -1;
        private readonly Action<string> _callback;

        public static readonly string[] BulletSymbols = new string[]
        {
            "none", "●", "○", "■", "✤", "❖", "➢", "✓"
        };

        public BulletDropDownForm(Point screenPos, Action<string> callback)
        {
            _callback = callback;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int w = 240;
            int h = 185;

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

            // Check Grid Boxes (2 rows of 4, X: 12..228, Y: 28..124)
            int boxW = 50, boxH = 45, startX = 12, startY = 28;
            for (int i = 0; i < 8; i++)
            {
                int r = i / 4;
                int c = i % 4;
                Rectangle rBox = new Rectangle(startX + c * (boxW + 4), startY + r * (boxH + 4), boxW, boxH);
                if (rBox.Contains(e.Location))
                {
                    _hoverBoxIndex = i;
                    break;
                }
            }

            // Check Bottom Links (Y: 130 to 180)
            if (e.Y >= 130 && e.Y < 155) _hoverBottomIndex = 0; // Change List Level
            else if (e.Y >= 155 && e.Y <= 180) _hoverBottomIndex = 1; // Define New Bullet

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
            if (_hoverBoxIndex >= 0 && _hoverBoxIndex < BulletSymbols.Length)
            {
                string chosen = BulletSymbols[_hoverBoxIndex];
                Close();
                if (_callback != null) _callback(chosen);
            }
            else if (_hoverBottomIndex == 0)
            {
                // Change List Level (cycles level or tab indent)
                Close();
                if (_callback != null) _callback("level");
            }
            else if (_hoverBottomIndex == 1)
            {
                // Define New Bullet default to solid circle
                Close();
                if (_callback != null) _callback("●");
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
                g.DrawString("Bullet Library", titleFont, titleBrush, 12, 8);
            }

            // Grid Boxes
            int boxW = 50, boxH = 45, startX = 12, startY = 28;
            for (int i = 0; i < 8; i++)
            {
                int r = i / 4;
                int c = i % 4;
                Rectangle rBox = new Rectangle(startX + c * (boxW + 4), startY + r * (boxH + 4), boxW, boxH);

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

                // Render Bullet Symbol
                int cx = rBox.X + rBox.Width / 2;
                int cy = rBox.Y + rBox.Height / 2;

                if (i == 0) // None
                {
                    using (Font fNone = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                    using (SolidBrush bNone = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    {
                        SizeF sz = g.MeasureString("None", fNone);
                        g.DrawString("None", fNone, bNone, cx - sz.Width / 2, cy - sz.Height / 2);
                    }
                }
                else if (i == 1) // Solid Circle ●
                {
                    using (SolidBrush b = new SolidBrush(Color.Black))
                    {
                        g.FillEllipse(b, cx - 4, cy - 4, 9, 9);
                    }
                }
                else if (i == 2) // Hollow Circle ○
                {
                    using (Pen p = new Pen(Color.Black, 1.6f))
                    {
                        g.DrawEllipse(p, cx - 4.5f, cy - 4.5f, 9, 9);
                    }
                }
                else if (i == 3) // Solid Square ■
                {
                    using (SolidBrush b = new SolidBrush(Color.Black))
                    {
                        g.FillRectangle(b, cx - 4, cy - 4, 8, 8);
                    }
                }
                else if (i == 4) // 4-Color Quad Cross ✤
                {
                    using (SolidBrush bBlue = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    using (SolidBrush bRed = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    using (SolidBrush bYellow = new SolidBrush(Color.FromArgb(234, 179, 8)))
                    using (SolidBrush bGreen = new SolidBrush(Color.FromArgb(22, 163, 74)))
                    {
                        g.FillRectangle(bRed, cx - 5, cy - 5, 4, 4);
                        g.FillRectangle(bBlue, cx + 1, cy - 5, 4, 4);
                        g.FillRectangle(bYellow, cx - 5, cy + 1, 4, 4);
                        g.FillRectangle(bGreen, cx + 1, cy + 1, 4, 4);
                    }
                }
                else if (i == 5) // Diamond ❖ (4 small diamonds)
                {
                    using (SolidBrush b = new SolidBrush(Color.Black))
                    {
                        DrawSmallDiamond(g, b, cx, cy - 4, 3);
                        DrawSmallDiamond(g, b, cx, cy + 4, 3);
                        DrawSmallDiamond(g, b, cx - 4, cy, 3);
                        DrawSmallDiamond(g, b, cx + 4, cy, 3);
                    }
                }
                else if (i == 6) // Arrowhead ➢
                {
                    using (SolidBrush b = new SolidBrush(Color.Black))
                    {
                        PointF[] arrow = new PointF[]
                        {
                            new PointF(cx - 5, cy - 6),
                            new PointF(cx + 6, cy),
                            new PointF(cx - 5, cy + 6),
                            new PointF(cx - 2, cy)
                        };
                        g.FillPolygon(b, arrow);
                    }
                }
                else if (i == 7) // Checkmark ✓
                {
                    using (Pen p = new Pen(Color.Black, 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(p, cx - 5, cy, cx - 2, cy + 5);
                        g.DrawLine(p, cx - 2, cy + 5, cx + 6, cy - 5);
                    }
                }
            }

            // Divider line
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                g.DrawLine(divPen, 8, 128, Width - 8, 128);
            }

            // Bottom Links
            using (Font linkFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            {
                // Item 1: Change List Level
                Rectangle link1 = new Rectangle(8, 131, Width - 16, 23);
                if (_hoverBottomIndex == 0)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillRectangle(hb, link1);
                    }
                }
                using (Pen arrPen = new Pen(Color.FromArgb(71, 85, 105), 1.2f))
                {
                    g.DrawLine(arrPen, 16, 142, 24, 142);
                    g.DrawLine(arrPen, 21, 139, 24, 142);
                    g.DrawLine(arrPen, 21, 145, 24, 142);
                }
                g.DrawString("Change List Level", linkFont, Brushes.Black, 30, 134);
                using (Pen chevPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
                {
                    g.DrawLine(chevPen, Width - 20, 138, Width - 16, 142);
                    g.DrawLine(chevPen, Width - 16, 142, Width - 20, 146);
                }

                // Item 2: Define New Bullet...
                Rectangle link2 = new Rectangle(8, 155, Width - 16, 23);
                if (_hoverBottomIndex == 1)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    {
                        g.FillRectangle(hb, link2);
                    }
                }
                g.DrawString("Define New Bullet...", linkFont, Brushes.Black, 30, 158);
            }
        }

        private void DrawSmallDiamond(Graphics g, Brush brush, float cx, float cy, float r)
        {
            PointF[] pts = new PointF[]
            {
                new PointF(cx, cy - r),
                new PointF(cx + r, cy),
                new PointF(cx, cy + r),
                new PointF(cx - r, cy)
            };
            g.FillPolygon(brush, pts);
        }
    }
}

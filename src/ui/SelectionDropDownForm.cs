using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class SelectionDropDownForm : Form
    {
        private int _hoverIndex = -1;
        private readonly Action<int> _callback;

        private readonly string[] _itemTitles = new string[]
        {
            "Chọn tất cả (Select All)",
            "Chọn đối tượng (Select Objects)",
            "Chọn tất cả văn bản có định dạng tương tự",
            "Ngăn chọn... (Selection Pane...)"
        };

        public SelectionDropDownForm(Point screenPos, Action<int> callback)
        {
            _callback = callback;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int w = 310;
            int h = 150;

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
            int itemH = 34;
            int hit = (e.Y - 6) / itemH;
            if (hit >= 0 && hit < _itemTitles.Length)
            {
                if (_hoverIndex != hit)
                {
                    _hoverIndex = hit;
                    Invalidate();
                }
            }
            else
            {
                if (_hoverIndex != -1)
                {
                    _hoverIndex = -1;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int itemH = 34;
            int hit = (e.Y - 6) / itemH;
            if (hit >= 0 && hit < _itemTitles.Length)
            {
                int chosen = hit;
                Close();
                if (_callback != null)
                {
                    _callback(chosen);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Outer Border with Soft Shadow Feel
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            int itemH = 34;
            int startY = 6;

            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            {
                for (int i = 0; i < _itemTitles.Length; i++)
                {
                    Rectangle itemRect = new Rectangle(4, startY + i * itemH, Width - 8, itemH - 2);

                    if (i == _hoverIndex)
                    {
                        using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        using (Pen hPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                        {
                            FillRoundedRect(g, hBrush, itemRect, 3);
                            DrawRoundedRect(g, hPen, itemRect, 3);
                        }
                    }

                    // Draw Item Icon
                    int iconX = itemRect.X + 8;
                    int iconY = itemRect.Y + (itemRect.Height - 16) / 2;

                    if (i == 0)
                    {
                        // 1. Select All Icon (Doc + Cursor)
                        DrawSelectAllIcon(g, iconX, iconY);
                    }
                    else if (i == 1)
                    {
                        // 2. Select Objects Icon (Cursor Arrow)
                        DrawSelectPointerIcon(g, iconX, iconY);
                    }
                    else if (i == 2)
                    {
                        // 3. Similar Formatting Icon (Text cursor / lines)
                        DrawSimilarFormattingIcon(g, iconX, iconY);
                    }
                    else if (i == 3)
                    {
                        // 4. Selection Pane Icon (Layered panels + cursor)
                        DrawSelectionPaneMenuIcon(g, iconX, iconY);
                    }

                    // Text
                    int textX = itemRect.X + 32;
                    int textY = itemRect.Y + (itemRect.Height - 16) / 2;
                    g.DrawString(_itemTitles[i], titleFont, Brushes.Black, textX, textY);
                }
            }
        }

        private void DrawSelectAllIcon(Graphics g, int x, int y)
        {
            using (Pen docPen = new Pen(Color.FromArgb(10, 102, 194), 1.2f))
            using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
            using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Pen arrowPen = new Pen(Color.White, 1.0f))
            {
                // Document Box
                g.DrawRectangle(docPen, x + 1, y + 1, 10, 13);
                // Lines in doc
                g.DrawLine(linePen, x + 3, y + 4, x + 8, y + 4);
                g.DrawLine(linePen, x + 3, y + 7, x + 8, y + 7);
                g.DrawLine(linePen, x + 3, y + 10, x + 6, y + 10);

                // Small arrow pointer on right
                PointF[] ptr = new PointF[] {
                    new PointF(x + 7, y + 6),
                    new PointF(x + 7, y + 15),
                    new PointF(x + 10, y + 12),
                    new PointF(x + 14, y + 14),
                    new PointF(x + 15, y + 12),
                    new PointF(x + 11, y + 10),
                    new PointF(x + 13, y + 8)
                };
                g.FillPolygon(arrowBrush, ptr);
                g.DrawPolygon(arrowPen, ptr);
            }
        }

        private void DrawSelectPointerIcon(Graphics g, int x, int y)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Pen p = new Pen(Color.White, 1.0f))
            {
                PointF[] ptr = new PointF[] {
                    new PointF(x + 3, y + 1),
                    new PointF(x + 3, y + 13),
                    new PointF(x + 6.5f, y + 10),
                    new PointF(x + 11.5f, y + 14),
                    new PointF(x + 13, y + 12.5f),
                    new PointF(x + 8.5f, y + 8.5f),
                    new PointF(x + 12, y + 6)
                };
                g.FillPolygon(b, ptr);
                g.DrawPolygon(p, ptr);
            }
        }

        private void DrawSimilarFormattingIcon(Graphics g, int x, int y)
        {
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (SolidBrush b = new SolidBrush(Color.FromArgb(16, 110, 190)))
            {
                g.DrawLine(p, x + 2, y + 4, x + 14, y + 4);
                g.DrawLine(p, x + 2, y + 8, x + 12, y + 8);
                g.DrawLine(p, x + 2, y + 12, x + 8, y + 12);
                g.FillEllipse(b, x + 12, y + 10, 3, 3);
            }
        }

        private void DrawSelectionPaneMenuIcon(Graphics g, int x, int y)
        {
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(245, 158, 11)))
            using (Pen goldPen = new Pen(Color.FromArgb(217, 119, 6), 1.0f))
            using (Pen backPen = new Pen(Color.FromArgb(100, 116, 139), 1.2f))
            using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Pen arrowPen = new Pen(Color.White, 1.0f))
            {
                // Top-right Gold Rectangle
                Rectangle r1 = new Rectangle(x + 5, y + 1, 9, 8);
                g.FillRectangle(gold, r1);
                g.DrawRectangle(goldPen, r1);

                // Bottom-left Outline Rectangle
                g.DrawRectangle(backPen, x + 1, y + 5, 9, 8);

                // Small arrow pointer
                PointF[] ptr = new PointF[] {
                    new PointF(x + 7, y + 6),
                    new PointF(x + 7, y + 15),
                    new PointF(x + 9.5f, y + 12),
                    new PointF(x + 13, y + 14),
                    new PointF(x + 14, y + 12.5f),
                    new PointF(x + 10.5f, y + 10.5f),
                    new PointF(x + 12, y + 8.5f)
                };
                g.FillPolygon(arrowBrush, ptr);
                g.DrawPolygon(arrowPen, ptr);
            }
        }

        private void FillRoundedRect(Graphics g, Brush brush, Rectangle r, int radius)
        {
            using (GraphicsPath path = CreateRoundedRectPath(r, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private void DrawRoundedRect(Graphics g, Pen pen, Rectangle r, int radius)
        {
            using (GraphicsPath path = CreateRoundedRectPath(r, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

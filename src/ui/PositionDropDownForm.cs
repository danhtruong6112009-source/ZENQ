using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class PositionDropDownForm : Form
    {
        private int _hoverIndex = -1; // 0=InLine, 1..9=3x3 grid
        private DoubleBufferedPosPanel _contentPanel;
        private DoubleBufferedPosPanel _bottomPanel;
        private bool _committed = false;

        public event Action<int> OnPositionSelected; // 0=InLine, 1..9 for 9 positions
        public event Action<int> OnPreviewPosition;
        public event Action OnCancelPreview;
        public event Action OnMoreLayoutOptionsClicked;

        public class DoubleBufferedPosPanel : Panel
        {
            public DoubleBufferedPosPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }

        public PositionDropDownForm(Point screenPos, int currentPosIndex)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            int w = 280;
            int h = 390;
            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            _bottomPanel = new DoubleBufferedPosPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand
            };
            _bottomPanel.Paint += DrawBottomPanel;
            _bottomPanel.MouseEnter += (s, e) => { _bottomPanel.BackColor = Color.FromArgb(226, 232, 240); _bottomPanel.Invalidate(); };
            _bottomPanel.MouseLeave += (s, e) => { _bottomPanel.BackColor = Color.FromArgb(248, 250, 252); _bottomPanel.Invalidate(); };
            _bottomPanel.Click += (s, e) =>
            {
                _committed = true;
                Close();
                if (OnMoreLayoutOptionsClicked != null) OnMoreLayoutOptionsClicked();
            };
            Controls.Add(_bottomPanel);

            _contentPanel = new DoubleBufferedPosPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _contentPanel.Paint += DrawContent;
            _contentPanel.MouseMove += (s, e) =>
            {
                int newHover = HitTest(e.X, e.Y);
                if (newHover != _hoverIndex)
                {
                    _hoverIndex = newHover;
                    _contentPanel.Invalidate();
                    if (_hoverIndex >= 0)
                    {
                        if (OnPreviewPosition != null) OnPreviewPosition(_hoverIndex);
                    }
                    else
                    {
                        if (!_committed && OnCancelPreview != null) OnCancelPreview();
                    }
                }
            };
            _contentPanel.MouseLeave += (s, e) =>
            {
                if (_hoverIndex != -1)
                {
                    _hoverIndex = -1;
                    _contentPanel.Invalidate();
                    if (!_committed && OnCancelPreview != null) OnCancelPreview();
                }
            };
            _contentPanel.Click += (s, e) =>
            {
                Point pt = _contentPanel.PointToClient(Cursor.Position);
                int hit = HitTest(pt.X, pt.Y);
                if (hit >= 0)
                {
                    _committed = true;
                    Close();
                    if (OnPositionSelected != null) OnPositionSelected(hit);
                }
            };
            Controls.Add(_contentPanel);
        }

        private int HitTest(int mx, int my)
        {
            // InLine box (14, 28, 56, 60)
            if (mx >= 14 && mx <= 70 && my >= 28 && my <= 88) return 0;

            // 3x3 Grid: startX=14, startY=118, itemW=80, itemH=70, gap=6
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int gx = 14 + c * 86;
                    int gy = 118 + r * 74;
                    if (mx >= gx && mx <= gx + 80 && my >= gy && my <= gy + 70)
                    {
                        return 1 + r * 3 + c;
                    }
                }
            }
            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 180, 180), 1.0f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void DrawContent(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (Font headFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Brush headBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                // 1. In Line with Text
                g.DrawString("Cùng dòng văn bản (In Line with Text)", headFont, headBrush, 14, 8);
                Rectangle inLineRect = new Rectangle(14, 28, 56, 60);
                DrawPageThumbnail(g, inLineRect, -1, -1, _hoverIndex == 0, false);

                // Separator
                g.DrawLine(sepPen, 14, 94, _contentPanel.Width - 14, 94);

                // 2. With Text Wrapping
                g.DrawString("Ngắt dòng quanh trang (With Text Wrapping)", headFont, headBrush, 14, 100);

                // 3x3 Grid
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int idx = 1 + r * 3 + c;
                        int gx = 14 + c * 86;
                        int gy = 118 + r * 74;
                        Rectangle gridRect = new Rectangle(gx, gy, 80, 70);
                        DrawPageThumbnail(g, gridRect, r, c, _hoverIndex == idx, true);
                    }
                }
            }
        }

        private void DrawPageThumbnail(Graphics g, Rectangle bounds, int row, int col, bool isHover, bool isWrapped)
        {
            // Hover background
            if (isHover)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(239, 246, 255)))
                {
                    g.FillRectangle(hb, bounds);
                }
                using (Pen hp = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
                {
                    g.DrawRectangle(hp, bounds);
                }
            }

            // Page paper
            int pw = bounds.Width - 28;
            int ph = bounds.Height - 16;
            int px = bounds.X + (bounds.Width - pw) / 2;
            int py = bounds.Y + (bounds.Height - ph) / 2;

            using (SolidBrush pb = new SolidBrush(Color.White))
            {
                g.FillRectangle(pb, px, py, pw, ph);
            }
            using (Pen borderP = new Pen(Color.FromArgb(71, 85, 105), 1.0f))
            {
                g.DrawRectangle(borderP, px, py, pw, ph);
            }

            // Picture box size inside thumbnail
            int picW = 20;
            int picH = 14;
            int picX = px + 4;
            int picY = py + 4;

            if (!isWrapped)
            {
                // In line with text: center picture vertically between text lines
                picX = px + (pw - picW) / 2;
                picY = py + (ph - picH) / 2;

                using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
                {
                    g.DrawLine(linePen, px + 3, py + 5, px + pw - 3, py + 5);
                    g.DrawLine(linePen, px + 3, py + 9, px + pw - 3, py + 9);
                    g.DrawLine(linePen, px + 3, py + ph - 9, px + pw - 3, py + ph - 9);
                    g.DrawLine(linePen, px + 3, py + ph - 5, px + pw - 3, py + ph - 5);
                }
            }
            else
            {
                // 3x3 positions
                if (col == 0) picX = px + 3;
                else if (col == 1) picX = px + (pw - picW) / 2;
                else if (col == 2) picX = px + pw - picW - 3;

                if (row == 0) picY = py + 3;
                else if (row == 1) picY = py + (ph - picH) / 2;
                else if (row == 2) picY = py + ph - picH - 3;

                // Draw surrounding simulated text lines
                using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
                {
                    for (int ly = py + 4; ly <= py + ph - 4; ly += 4)
                    {
                        if (ly >= picY && ly <= picY + picH)
                        {
                            if (col == 0) // Pic on Left -> Text on Right
                            {
                                g.DrawLine(linePen, picX + picW + 3, ly, px + pw - 3, ly);
                            }
                            else if (col == 1) // Pic in Center -> Text on Left & Right
                            {
                                g.DrawLine(linePen, px + 3, ly, picX - 2, ly);
                                g.DrawLine(linePen, picX + picW + 2, ly, px + pw - 3, ly);
                            }
                            else if (col == 2) // Pic on Right -> Text on Left
                            {
                                g.DrawLine(linePen, px + 3, ly, picX - 3, ly);
                            }
                        }
                        else
                        {
                            // Full line across
                            g.DrawLine(linePen, px + 3, ly, px + pw - 3, ly);
                        }
                    }
                }
            }

            // Draw Picture Icon (Blue frame with mountains silhouette & orange sun)
            using (SolidBrush imgBg = new SolidBrush(Color.FromArgb(239, 246, 255)))
            using (Pen imgBorder = new Pen(Color.FromArgb(59, 130, 246), 1.0f))
            {
                g.FillRectangle(imgBg, picX, picY, picW, picH);
                g.DrawRectangle(imgBorder, picX, picY, picW, picH);
            }
            // Sun (orange dot)
            using (SolidBrush sunB = new SolidBrush(Color.FromArgb(245, 158, 11)))
            {
                g.FillEllipse(sunB, picX + picW - 6, picY + 2, 3, 3);
            }
            // Mountain (blue triangle)
            using (SolidBrush mtnB = new SolidBrush(Color.FromArgb(59, 130, 246)))
            {
                Point[] poly = new Point[] {
                    new Point(picX + 2, picY + picH - 1),
                    new Point(picX + 7, picY + 5),
                    new Point(picX + 13, picY + picH - 1)
                };
                g.FillPolygon(mtnB, poly);
            }
        }

        private void DrawBottomPanel(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen sep = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawLine(sep, 0, 0, _bottomPanel.Width, 0);
            }
            using (Pen p = new Pen(Color.FromArgb(2, 132, 199), 1.2f))
            {
                g.DrawRectangle(p, 12, 11, 14, 14);
                g.DrawLine(p, 8, 15, 12, 15);
                g.DrawLine(p, 8, 21, 12, 21);
                g.DrawLine(p, 26, 15, 30, 15);
                g.DrawLine(p, 26, 21, 30, 21);
            }
            using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Brush b = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Tùy chọn bố cục khác (More Layout Options)...", f, b, 34, 9);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (!_committed && OnCancelPreview != null) OnCancelPreview();
            Close();
        }
    }
}

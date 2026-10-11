using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class WrapTextDropDownForm : Form
    {
        private int _hoverIndex = -1;
        private int _currentWrapMode = -1;
        private DoubleBufferedWrapPanel _listPanel;
        private DoubleBufferedWrapPanel _bottomPanel;
        private bool _committed = false;

        private string[] _wrapTitles = new string[]
        {
            "Cùng dòng với văn bản (In Line with Text)",
            "Khung vuông (Square)",
            "Chặt chẽ (Tight)",
            "Xuyên qua (Through)",
            "Trên và Dưới (Top and Bottom)",
            "Sau văn bản (Behind Text)",
            "Trước văn bản (In Front of Text)"
        };

        public event Action<int> OnWrapModeSelected;
        public event Action<int> OnPreviewWrapMode;
        public event Action OnCancelPreview;
        public event Action OnMoreLayoutOptionsClicked;

        public class DoubleBufferedWrapPanel : Panel
        {
            public DoubleBufferedWrapPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }

        public WrapTextDropDownForm(Point screenPos, int currentMode)
        {
            _currentWrapMode = currentMode;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            int w = 275;
            int itemH = 34;
            int h = _wrapTitles.Length * itemH + 42;

            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            _bottomPanel = new DoubleBufferedWrapPanel
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

            _listPanel = new DoubleBufferedWrapPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _listPanel.Paint += DrawListPanel;
            _listPanel.MouseMove += (s, e) =>
            {
                int idx = e.Y / itemH;
                if (idx >= 0 && idx < _wrapTitles.Length)
                {
                    if (_hoverIndex != idx)
                    {
                        _hoverIndex = idx;
                        _listPanel.Invalidate();
                        if (OnPreviewWrapMode != null) OnPreviewWrapMode(idx);
                    }
                }
                else
                {
                    if (_hoverIndex != -1)
                    {
                        _hoverIndex = -1;
                        _listPanel.Invalidate();
                    }
                }
            };
            _listPanel.MouseLeave += (s, e) =>
            {
                if (_hoverIndex != -1)
                {
                    _hoverIndex = -1;
                    _listPanel.Invalidate();
                    if (!_committed && OnCancelPreview != null) OnCancelPreview();
                }
            };
            _listPanel.Click += (s, e) =>
            {
                Point pt = _listPanel.PointToClient(Cursor.Position);
                int idx = pt.Y / itemH;
                if (idx >= 0 && idx < _wrapTitles.Length)
                {
                    _committed = true;
                    Close();
                    if (OnWrapModeSelected != null) OnWrapModeSelected(idx);
                }
            };
            Controls.Add(_listPanel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 180, 180), 1.0f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void DrawListPanel(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int itemH = 34;
            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (SolidBrush selBg = new SolidBrush(Color.FromArgb(224, 242, 254)))
            using (Pen selPen = new Pen(Color.FromArgb(2, 132, 199), 1.0f))
            {
                for (int i = 0; i < _wrapTitles.Length; i++)
                {
                    int itemY = i * itemH;
                    Rectangle itemRect = new Rectangle(4, itemY + 2, _listPanel.Width - 8, itemH - 4);
                    bool isHover = (_hoverIndex == i);
                    bool isSel = (_currentWrapMode == i);

                    if (isHover)
                    {
                        g.FillRectangle(hoverBg, itemRect);
                    }
                    else if (isSel)
                    {
                        g.FillRectangle(selBg, itemRect);
                        g.DrawRectangle(selPen, itemRect);
                    }

                    // Draw 24x24 Vector Icon at X=10, Y=itemY+5
                    Rectangle iconRect = new Rectangle(10, itemY + 5, 24, 24);
                    DrawWrapIcon(g, iconRect, i);

                    // Text
                    g.DrawString(_wrapTitles[i], titleFont, textBrush, 42, itemY + 8);
                }
            }
        }

        private void DrawWrapIcon(Graphics g, Rectangle r, int mode)
        {
            int ix = r.X;
            int iy = r.Y;
            int iw = r.Width;
            int ih = r.Height;

            using (Pen bluePen = new Pen(Color.FromArgb(2, 132, 199), 1.2f))
            using (Pen arcPen = new Pen(mode == 5 ? Color.FromArgb(148, 163, 184) : Color.FromArgb(15, 23, 42), 2.2f))
            {
                arcPen.StartCap = LineCap.Round;
                arcPen.EndCap = LineCap.Round;

                if (mode == 0) // In line with text
                {
                    g.DrawLine(bluePen, ix + 2, iy + 6, ix + iw - 2, iy + 6);
                    g.DrawArc(arcPen, ix + 5, iy + 9, 14, 12, 180, 180);
                    g.DrawLine(bluePen, ix + 2, iy + 18, ix + iw - 2, iy + 18);
                }
                else if (mode == 1) // Square
                {
                    g.DrawLine(bluePen, ix + 2, iy + 4, ix + iw - 2, iy + 4);
                    g.DrawLine(bluePen, ix + 2, iy + 10, ix + 6, iy + 10);
                    g.DrawLine(bluePen, ix + iw - 6, iy + 10, ix + iw - 2, iy + 10);
                    g.DrawLine(bluePen, ix + 2, iy + 16, ix + 6, iy + 16);
                    g.DrawLine(bluePen, ix + iw - 6, iy + 16, ix + iw - 2, iy + 16);
                    g.DrawArc(arcPen, ix + 6, iy + 8, 12, 12, 180, 180);
                    g.DrawLine(bluePen, ix + 2, iy + 21, ix + iw - 2, iy + 21);
                }
                else if (mode == 2 || mode == 3) // Tight / Through
                {
                    g.DrawLine(bluePen, ix + 2, iy + 4, ix + iw - 2, iy + 4);
                    g.DrawLine(bluePen, ix + 2, iy + 9, ix + 7, iy + 9);
                    g.DrawLine(bluePen, ix + iw - 7, iy + 9, ix + iw - 2, iy + 9);
                    g.DrawLine(bluePen, ix + 2, iy + 15, ix + 7, iy + 15);
                    g.DrawLine(bluePen, ix + iw - 7, iy + 15, ix + iw - 2, iy + 15);
                    g.DrawArc(arcPen, ix + 5, iy + 8, 14, 13, 180, 180);
                    g.DrawLine(bluePen, ix + 2, iy + 20, ix + iw - 2, iy + 20);
                }
                else if (mode == 4) // Top and Bottom
                {
                    g.DrawLine(bluePen, ix + 2, iy + 4, ix + iw - 2, iy + 4);
                    g.DrawArc(arcPen, ix + 5, iy + 8, 14, 12, 180, 180);
                    g.DrawLine(bluePen, ix + 2, iy + 20, ix + iw - 2, iy + 20);
                }
                else if (mode == 5) // Behind Text
                {
                    g.DrawArc(arcPen, ix + 5, iy + 8, 14, 12, 180, 180);
                    g.DrawLine(bluePen, ix + 2, iy + 6, ix + iw - 2, iy + 6);
                    g.DrawLine(bluePen, ix + 2, iy + 12, ix + iw - 2, iy + 12);
                    g.DrawLine(bluePen, ix + 2, iy + 18, ix + iw - 2, iy + 18);
                }
                else if (mode == 6) // In Front of Text
                {
                    g.DrawLine(bluePen, ix + 2, iy + 6, ix + iw - 2, iy + 6);
                    g.DrawLine(bluePen, ix + 2, iy + 12, ix + iw - 2, iy + 12);
                    g.DrawLine(bluePen, ix + 2, iy + 18, ix + iw - 2, iy + 18);
                    g.DrawArc(arcPen, ix + 5, iy + 8, 14, 12, 180, 180);
                }
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

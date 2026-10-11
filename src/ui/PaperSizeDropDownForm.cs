using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class PaperSizeItem
    {
        public string Name { get; set; }
        public string Dimensions { get; set; }
        public float WidthInches { get; set; }
        public float HeightInches { get; set; }

        public PaperSizeItem(string name, string dim, float w, float h)
        {
            Name = name;
            Dimensions = dim;
            WidthInches = w;
            HeightInches = h;
        }
    }

    public class PaperSizeDropDownForm : Form
    {
        private List<PaperSizeItem> _items;
        private string _selectedName;
        private int _hoverIndex = -1;
        private int _scrollY = 0;
        private int _itemHeight = 52;
        private VScrollBar _scrollBar;
        private DoubleBufferedSizePanel _listPanel;
        private DoubleBufferedSizePanel _bottomPanel;

        public event Action<PaperSizeItem> OnPaperSizeSelected;
        public event Action OnMorePaperSizesClicked;

        public class DoubleBufferedSizePanel : Panel
        {
            public DoubleBufferedSizePanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }

        public PaperSizeDropDownForm(Point screenPos, string currentPaperSize)
        {
            _selectedName = string.IsNullOrEmpty(currentPaperSize) ? "A4" : currentPaperSize;

            _items = new List<PaperSizeItem>
            {
                new PaperSizeItem("Letter", "8.5\" x 11\"", 8.5f, 11.0f),
                new PaperSizeItem("Tabloid", "11\" x 17\"", 11.0f, 17.0f),
                new PaperSizeItem("Legal", "8.5\" x 14\"", 8.5f, 14.0f),
                new PaperSizeItem("Statement", "5.5\" x 8.5\"", 5.5f, 8.5f),
                new PaperSizeItem("Executive", "7.25\" x 10.5\"", 7.25f, 10.5f),
                new PaperSizeItem("A3", "11.69\" x 16.54\"", 11.69f, 16.54f),
                new PaperSizeItem("A4", "8.27\" x 11.69\"", 8.27f, 11.69f),
                new PaperSizeItem("A5", "5.83\" x 8.27\"", 5.83f, 8.27f),
                new PaperSizeItem("B4 (JIS)", "10.12\" x 14.33\"", 10.12f, 14.33f),
                new PaperSizeItem("B5 (JIS)", "7.17\" x 10.12\"", 7.17f, 10.12f)
            };

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            int w = 240;
            int h = 400;
            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            _bottomPanel = new DoubleBufferedSizePanel
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
                Close();
                if (OnMorePaperSizesClicked != null) OnMorePaperSizesClicked();
            };
            Controls.Add(_bottomPanel);

            int visibleListH = h - 36;
            int totalContentH = _items.Count * _itemHeight + 10;
            int maxScroll = Math.Max(0, totalContentH - visibleListH);

            _scrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Width = 16,
                Minimum = 0,
                Maximum = maxScroll + 20,
                LargeChange = 20,
                SmallChange = _itemHeight
            };
            _scrollBar.Scroll += (s, e) =>
            {
                _scrollY = _scrollBar.Value;
                _listPanel.Invalidate();
            };
            Controls.Add(_scrollBar);

            _listPanel = new DoubleBufferedSizePanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _listPanel.Paint += DrawListPanel;
            _listPanel.MouseMove += (s, e) =>
            {
                int idx = (e.Y + _scrollY) / _itemHeight;
                if (idx >= 0 && idx < _items.Count)
                {
                    if (_hoverIndex != idx)
                    {
                        _hoverIndex = idx;
                        _listPanel.Invalidate();
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
                }
            };
            _listPanel.MouseWheel += (s, e) =>
            {
                int delta = (e.Delta / 120) * _itemHeight;
                int newVal = _scrollBar.Value - delta;
                newVal = Math.Max(0, Math.Min(_scrollBar.Maximum - _scrollBar.LargeChange + 1, newVal));
                _scrollBar.Value = newVal;
                _scrollY = newVal;
                _listPanel.Invalidate();
            };
            _listPanel.Click += (s, e) =>
            {
                Point pt = _listPanel.PointToClient(Cursor.Position);
                int idx = (pt.Y + _scrollY) / _itemHeight;
                if (idx >= 0 && idx < _items.Count)
                {
                    var item = _items[idx];
                    Close();
                    if (OnPaperSizeSelected != null) OnPaperSizeSelected(item);
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

            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font dimFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Brush dimBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (Pen outlinePen = new Pen(Color.FromArgb(71, 85, 105), 1.2f))
            using (SolidBrush selBgBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
            using (SolidBrush hoverBgBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    int itemY = i * _itemHeight - _scrollY;
                    if (itemY + _itemHeight < 0 || itemY > _listPanel.Height) continue;

                    Rectangle itemRect = new Rectangle(4, itemY + 2, _listPanel.Width - 8, _itemHeight - 4);
                    bool isSelected = _items[i].Name.Equals(_selectedName, StringComparison.OrdinalIgnoreCase);
                    bool isHover = (_hoverIndex == i);

                    if (isHover)
                    {
                        g.FillRectangle(hoverBgBrush, itemRect);
                    }

                    // Thumbnail Box (32x36) at X=10, Y=itemY+8
                    Rectangle thumbBox = new Rectangle(10, itemY + 8, 32, 36);
                    if (isSelected)
                    {
                        g.FillRectangle(selBgBrush, thumbBox);
                        using (Pen selPen = new Pen(Color.FromArgb(51, 65, 85), 1.0f))
                        {
                            g.DrawRectangle(selPen, thumbBox);
                        }
                    }

                    // Draw miniature page proportional rectangle
                    float maxPaperDim = 17.0f;
                    int pw = Math.Max(12, (int)(22 * (_items[i].WidthInches / maxPaperDim * 1.5f)));
                    int ph = Math.Max(16, (int)(28 * (_items[i].HeightInches / maxPaperDim * 1.5f)));
                    pw = Math.Min(22, pw);
                    ph = Math.Min(28, ph);
                    int px = thumbBox.X + (thumbBox.Width - pw) / 2;
                    int py = thumbBox.Y + (thumbBox.Height - ph) / 2;

                    using (SolidBrush paperBg = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(paperBg, px, py, pw, ph);
                    }
                    g.DrawRectangle(outlinePen, px, py, pw, ph);

                    // Text labels
                    g.DrawString(_items[i].Name, titleFont, textBrush, 50, itemY + 10);
                    g.DrawString(_items[i].Dimensions, dimFont, dimBrush, 50, itemY + 28);
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
            using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Brush b = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Thêm khổ giấy khác (More Paper Sizes)...", f, b, 12, 9);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}

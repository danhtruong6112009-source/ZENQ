using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ColumnPresetItem
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public int ColumnCount { get; set; }
        public string PresetType { get; set; } // "One", "Two", "Three", "Left", "Right"

        public ColumnPresetItem(string name, string title, int count, string type)
        {
            Name = name;
            Title = title;
            ColumnCount = count;
            PresetType = type;
        }
    }

    public class ColumnsDropDownForm : Form
    {
        private List<ColumnPresetItem> _items;
        private string _selectedPreset;
        private int _hoverIndex = -1;
        private int _itemHeight = 46;
        private DoubleBufferedColPanel _listPanel;
        private DoubleBufferedColPanel _bottomPanel;

        public event Action<ColumnPresetItem> OnColumnPresetSelected;
        public event Action OnMoreColumnsClicked;

        public class DoubleBufferedColPanel : Panel
        {
            public DoubleBufferedColPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }

        public ColumnsDropDownForm(Point screenPos, string currentPreset)
        {
            _selectedPreset = string.IsNullOrEmpty(currentPreset) ? "One" : currentPreset;

            _items = new List<ColumnPresetItem>
            {
                new ColumnPresetItem("One", "Một cột (One)", 1, "One"),
                new ColumnPresetItem("Two", "Hai cột (Two)", 2, "Two"),
                new ColumnPresetItem("Three", "Ba cột (Three)", 3, "Three"),
                new ColumnPresetItem("Left", "Cột trái nhỏ (Left)", 2, "Left"),
                new ColumnPresetItem("Right", "Cột phải nhỏ (Right)", 2, "Right")
            };

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            int w = 220;
            int h = _items.Count * _itemHeight + 42;
            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            _bottomPanel = new DoubleBufferedColPanel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand
            };
            _bottomPanel.Paint += DrawBottomPanel;
            _bottomPanel.MouseEnter += (s, e) => { _bottomPanel.BackColor = Color.FromArgb(226, 232, 240); _bottomPanel.Invalidate(); };
            _bottomPanel.MouseLeave += (s, e) => { _bottomPanel.BackColor = Color.FromArgb(248, 250, 252); _bottomPanel.Invalidate(); };
            _bottomPanel.Click += (s, e) =>
            {
                Close();
                if (OnMoreColumnsClicked != null) OnMoreColumnsClicked();
            };
            Controls.Add(_bottomPanel);

            _listPanel = new DoubleBufferedColPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _listPanel.Paint += DrawListPanel;
            _listPanel.MouseMove += (s, e) =>
            {
                int idx = e.Y / _itemHeight;
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
            _listPanel.Click += (s, e) =>
            {
                Point pt = _listPanel.PointToClient(Cursor.Position);
                int idx = pt.Y / _itemHeight;
                if (idx >= 0 && idx < _items.Count)
                {
                    var item = _items[idx];
                    Close();
                    if (OnColumnPresetSelected != null) OnColumnPresetSelected(item);
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

            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Pen outlinePen = new Pen(Color.FromArgb(71, 85, 105), 1.0f))
            using (Pen linePen = new Pen(Color.FromArgb(2, 132, 199), 1.2f))
            using (SolidBrush selBgBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
            using (SolidBrush hoverBgBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    int itemY = i * _itemHeight;
                    Rectangle itemRect = new Rectangle(4, itemY + 2, _listPanel.Width - 8, _itemHeight - 4);
                    bool isSelected = _items[i].PresetType.Equals(_selectedPreset, StringComparison.OrdinalIgnoreCase);
                    bool isHover = (_hoverIndex == i);

                    if (isHover)
                    {
                        g.FillRectangle(hoverBgBrush, itemRect);
                    }

                    // Thumbnail Box (28x34) at X=12, Y=itemY+6
                    Rectangle thumbBox = new Rectangle(12, itemY + 6, 28, 34);
                    if (isSelected)
                    {
                        g.FillRectangle(selBgBrush, thumbBox);
                        using (Pen selPen = new Pen(Color.FromArgb(51, 65, 85), 1.0f))
                        {
                            g.DrawRectangle(selPen, thumbBox);
                        }
                    }

                    int px = thumbBox.X + 3;
                    int py = thumbBox.Y + 3;
                    int pw = 22;
                    int ph = 28;

                    using (SolidBrush paperBg = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(paperBg, px, py, pw, ph);
                    }
                    g.DrawRectangle(outlinePen, px, py, pw, ph);

                    // Draw blue column lines inside miniature paper
                    string pType = _items[i].PresetType;
                    if (pType == "One")
                    {
                        for (int ly = py + 4; ly <= py + ph - 4; ly += 4)
                            g.DrawLine(linePen, px + 3, ly, px + pw - 3, ly);
                    }
                    else if (pType == "Two")
                    {
                        int mid = px + pw / 2;
                        for (int ly = py + 4; ly <= py + ph - 4; ly += 4)
                        {
                            g.DrawLine(linePen, px + 2, ly, mid - 2, ly);
                            g.DrawLine(linePen, mid + 2, ly, px + pw - 2, ly);
                        }
                    }
                    else if (pType == "Three")
                    {
                        int cW = (pw - 6) / 3;
                        for (int ly = py + 4; ly <= py + ph - 4; ly += 4)
                        {
                            g.DrawLine(linePen, px + 2, ly, px + 2 + cW - 1, ly);
                            g.DrawLine(linePen, px + 2 + cW + 1, ly, px + 2 + 2 * cW, ly);
                            g.DrawLine(linePen, px + 2 + 2 * cW + 2, ly, px + pw - 2, ly);
                        }
                    }
                    else if (pType == "Left")
                    {
                        int split = px + 7;
                        for (int ly = py + 4; ly <= py + ph - 4; ly += 4)
                        {
                            g.DrawLine(linePen, px + 2, ly, split, ly);
                            g.DrawLine(linePen, split + 3, ly, px + pw - 2, ly);
                        }
                    }
                    else if (pType == "Right")
                    {
                        int split = px + pw - 9;
                        for (int ly = py + 4; ly <= py + ph - 4; ly += 4)
                        {
                            g.DrawLine(linePen, px + 2, ly, split - 3, ly);
                            g.DrawLine(linePen, split, ly, px + pw - 2, ly);
                        }
                    }

                    // Text label
                    g.DrawString(_items[i].Name, titleFont, textBrush, 48, itemY + 13);
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
                g.DrawLine(p, 12, 14, 18, 14);
                g.DrawLine(p, 20, 14, 26, 14);
                g.DrawLine(p, 12, 18, 18, 18);
                g.DrawLine(p, 20, 18, 26, 18);
                g.DrawLine(p, 12, 22, 18, 22);
                g.DrawLine(p, 20, 22, 26, 22);
            }
            using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Brush b = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Thêm tùy chọn cột (More Columns)...", f, b, 34, 10);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}

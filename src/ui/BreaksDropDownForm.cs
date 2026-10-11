using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class BreakItem
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string BreakType { get; set; } // "Page", "Column", "TextWrapping", "NextPage", "Continuous", "EvenPage", "OddPage"
        public bool IsSectionHeader { get; set; }

        public BreakItem(string title, string subtitle, string breakType)
        {
            Title = title;
            Subtitle = subtitle;
            BreakType = breakType;
            IsSectionHeader = false;
        }

        public static BreakItem CreateHeader(string headerTitle)
        {
            return new BreakItem(headerTitle, "", "") { IsSectionHeader = true };
        }
    }

    public class BreaksDropDownForm : Form
    {
        private List<BreakItem> _items;
        private int _hoverIndex = -1;
        private DoubleBufferedBreakPanel _listPanel;

        public event Action<string> OnBreakSelected;

        public class DoubleBufferedBreakPanel : Panel
        {
            public DoubleBufferedBreakPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }

        public BreaksDropDownForm(Point screenPos)
        {
            _items = new List<BreakItem>
            {
                BreakItem.CreateHeader("Ngắt trang (Page Breaks)"),
                new BreakItem("Trang (Page)", "Đánh dấu điểm kết thúc một trang và bắt đầu trang tiếp theo.", "Page"),
                new BreakItem("Cột (Column)", "Văn bản sau dấu ngắt cột sẽ bắt đầu ở cột tiếp theo.", "Column"),
                new BreakItem("Ngắt văn bản (Text Wrapping)", "Tách biệt văn bản xung quanh các đối tượng hình ảnh.", "TextWrapping"),

                BreakItem.CreateHeader("Ngắt đoạn (Section Breaks)"),
                new BreakItem("Trang tiếp theo (Next Page)", "Chèn ngắt đoạn và bắt đầu đoạn mới trên trang tiếp theo.", "NextPage"),
                new BreakItem("Liên tục (Continuous)", "Chèn ngắt đoạn và bắt đầu đoạn mới trên cùng trang.", "Continuous"),
                new BreakItem("Trang chẵn (Even Page)", "Chèn ngắt đoạn và bắt đầu đoạn mới trên trang chẵn tiếp theo.", "EvenPage"),
                new BreakItem("Trang lẻ (Odd Page)", "Chèn ngắt đoạn và bắt đầu đoạn mới trên trang lẻ tiếp theo.", "OddPage")
            };

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int w = 310;
            int h = 480;
            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            _listPanel = new DoubleBufferedBreakPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _listPanel.Paint += DrawListPanel;
            _listPanel.MouseMove += (s, e) =>
            {
                int hit = GetItemAt(e.Y);
                if (hit != _hoverIndex)
                {
                    _hoverIndex = hit;
                    _listPanel.Invalidate();
                }
            };
            _listPanel.MouseLeave += (s, e) =>
            {
                _hoverIndex = -1;
                _listPanel.Invalidate();
            };
            _listPanel.Click += (s, e) =>
            {
                Point pt = _listPanel.PointToClient(Cursor.Position);
                int hit = GetItemAt(pt.Y);
                if (hit >= 0 && hit < _items.Count && !_items[hit].IsSectionHeader)
                {
                    string bType = _items[hit].BreakType;
                    Close();
                    if (OnBreakSelected != null) OnBreakSelected(bType);
                }
            };
            Controls.Add(_listPanel);
        }

        private int GetItemAt(int mouseY)
        {
            int curY = 4;
            for (int i = 0; i < _items.Count; i++)
            {
                int itemH = _items[i].IsSectionHeader ? 28 : 48;
                if (mouseY >= curY && mouseY <= curY + itemH)
                {
                    return _items[i].IsSectionHeader ? -1 : i;
                }
                curY += itemH;
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

        private void DrawListPanel(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int curY = 4;
            using (Font headFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 8.0f, FontStyle.Regular))
            using (Brush headBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Brush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    int itemH = item.IsSectionHeader ? 28 : 48;

                    if (item.IsSectionHeader)
                    {
                        if (i > 0)
                        {
                            g.DrawLine(sepPen, 10, curY, _listPanel.Width - 10, curY);
                        }
                        g.DrawString(item.Title, headFont, headBrush, 12, curY + 6);
                    }
                    else
                    {
                        bool isHover = (_hoverIndex == i);
                        if (isHover)
                        {
                            g.FillRectangle(hoverBg, 4, curY, _listPanel.Width - 8, itemH);
                        }

                        // Draw rich vector icon for this break type (32x38 box at X=12, Y=curY+5)
                        Rectangle iconBox = new Rectangle(12, curY + 5, 32, 38);
                        DrawBreakIcon(g, iconBox, item.BreakType);

                        // Draw Title & Subtitle with word wrap
                        g.DrawString(item.Title, titleFont, titleBrush, 52, curY + 4);
                        Rectangle subRect = new Rectangle(52, curY + 20, _listPanel.Width - 60, 26);
                        g.DrawString(item.Subtitle, subFont, subBrush, subRect);
                    }

                    curY += itemH;
                }
            }
        }

        private void DrawBreakIcon(Graphics g, Rectangle box, string type)
        {
            int bx = box.X + 2;
            int by = box.Y + 2;
            int bw = 26;
            int bh = 34;

            using (SolidBrush paperBg = new SolidBrush(Color.White))
            using (Pen paperPen = new Pen(Color.FromArgb(71, 85, 105), 1.0f))
            using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
            using (Pen redPen = new Pen(Color.FromArgb(220, 38, 38), 1.2f))
            using (Pen cyanPen = new Pen(Color.FromArgb(2, 132, 199), 1.4f))
            using (Font numFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            using (Brush numBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                if (type == "Page")
                {
                    // Split page horizontally in middle with cyan arrow/separator
                    g.FillRectangle(paperBg, bx, by, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by, bw, bh / 2 - 2);

                    g.FillRectangle(paperBg, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by + bh / 2 + 2, bw, bh / 2 - 2);

                    // Cyan split indicator
                    g.DrawLine(cyanPen, bx - 3, by + bh / 2, bx + bw + 3, by + bh / 2);
                    Point[] arrow = new Point[] { new Point(bx - 3, by + bh / 2 - 2), new Point(bx, by + bh / 2), new Point(bx - 3, by + bh / 2 + 2) };
                    g.DrawLines(cyanPen, arrow);
                }
                else if (type == "Column")
                {
                    // Page with 2 columns, red arrow jumping from col 1 to col 2
                    g.FillRectangle(paperBg, bx, by, bw, bh);
                    g.DrawRectangle(paperPen, bx, by, bw, bh);

                    int mid = bx + bw / 2;
                    for (int ly = by + 4; ly <= by + bh - 6; ly += 4)
                    {
                        g.DrawLine(linePen, bx + 3, ly, mid - 2, ly);
                        g.DrawLine(linePen, mid + 2, ly, bx + bw - 3, ly);
                    }
                    // Red arrow
                    g.DrawLine(redPen, bx + 3, by + bh - 6, mid - 2, by + bh - 6);
                    g.DrawLine(redPen, mid - 2, by + bh - 6, mid - 2, by + 4);
                    g.DrawLine(redPen, mid - 2, by + 4, mid + 5, by + 4);
                    // Red arrowhead
                    g.DrawLine(redPen, mid + 3, by + 2, mid + 5, by + 4);
                    g.DrawLine(redPen, mid + 3, by + 6, mid + 5, by + 4);
                }
                else if (type == "TextWrapping")
                {
                    // Page with image box and red wrap arrow
                    g.FillRectangle(paperBg, bx, by, bw, bh);
                    g.DrawRectangle(paperPen, bx, by, bw, bh);

                    g.DrawRectangle(paperPen, bx + 3, by + 6, 9, 9);
                    g.DrawLine(linePen, bx + 14, by + 8, bx + bw - 3, by + 8);
                    g.DrawLine(linePen, bx + 14, by + 12, bx + bw - 3, by + 12);

                    for (int ly = by + 18; ly <= by + bh - 6; ly += 4)
                    {
                        g.DrawLine(linePen, bx + 3, ly, bx + bw - 3, ly);
                    }

                    // Red wrap arrow
                    g.DrawLine(redPen, bx + 15, by + 12, bx + 17, by + 12);
                    g.DrawLine(redPen, bx + 17, by + 12, bx + 17, by + 16);
                    g.DrawLine(redPen, bx + 17, by + 16, bx + 13, by + 16);
                    g.DrawLine(redPen, bx + 14, by + 14, bx + 12, by + 16);
                    g.DrawLine(redPen, bx + 14, by + 18, bx + 12, by + 16);
                }
                else if (type == "NextPage")
                {
                    // 2 pages: Top page with black lines, bottom page with red lines
                    g.FillRectangle(paperBg, bx, by, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by, bw, bh / 2 - 2);
                    g.DrawLine(linePen, bx + 3, by + 4, bx + bw - 3, by + 4);
                    g.DrawLine(linePen, bx + 3, by + 8, bx + bw - 3, by + 8);

                    g.FillRectangle(paperBg, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    g.DrawLine(redPen, bx + 3, by + bh / 2 + 5, bx + bw - 3, by + bh / 2 + 5);
                    g.DrawLine(redPen, bx + 3, by + bh / 2 + 9, bx + bw - 3, by + bh / 2 + 9);

                    // Section break chevron
                    g.DrawLine(cyanPen, bx - 3, by + bh / 2, bx - 1, by + bh / 2 + 2);
                    g.DrawLine(cyanPen, bx - 3, by + bh / 2 + 4, bx - 1, by + bh / 2 + 2);
                }
                else if (type == "Continuous")
                {
                    // 1 Page: Top black lines, Section break chevron, Bottom red lines
                    g.FillRectangle(paperBg, bx, by, bw, bh);
                    g.DrawRectangle(paperPen, bx, by, bw, bh);

                    g.DrawLine(linePen, bx + 3, by + 4, bx + bw - 3, by + 4);
                    g.DrawLine(linePen, bx + 3, by + 8, bx + bw - 3, by + 8);
                    g.DrawLine(linePen, bx + 3, by + 12, bx + bw - 3, by + 12);

                    // Red section marker
                    g.DrawLine(redPen, bx + 1, by + 16, bx + 4, by + 18);
                    g.DrawLine(redPen, bx + 1, by + 20, bx + 4, by + 18);

                    g.DrawLine(redPen, bx + 5, by + 18, bx + bw - 3, by + 18);
                    g.DrawLine(redPen, bx + 3, by + 22, bx + bw - 3, by + 22);
                    g.DrawLine(redPen, bx + 3, by + 26, bx + bw - 3, by + 26);
                }
                else if (type == "EvenPage")
                {
                    // 2 Pages: Top = 2 =, Bottom = 4 =
                    g.FillRectangle(paperBg, bx, by, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by, bw, bh / 2 - 2);
                    g.DrawString("= 2 =", numFont, numBrush, bx + 3, by + 2);

                    g.FillRectangle(paperBg, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    using (Brush redB = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.DrawString("= 4 =", numFont, redB, bx + 3, by + bh / 2 + 2);
                    }
                    g.DrawLine(redPen, bx + 3, by + bh / 2 + 10, bx + bw - 3, by + bh / 2 + 10);
                }
                else if (type == "OddPage")
                {
                    // 2 Pages: Top = 1 =, Bottom = 3 =
                    g.FillRectangle(paperBg, bx, by, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by, bw, bh / 2 - 2);
                    g.DrawString("= 1 =", numFont, numBrush, bx + 3, by + 2);

                    g.FillRectangle(paperBg, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    g.DrawRectangle(paperPen, bx, by + bh / 2 + 2, bw, bh / 2 - 2);
                    using (Brush redB = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.DrawString("= 3 =", numFont, redB, bx + 3, by + bh / 2 + 2);
                    }
                    g.DrawLine(redPen, bx + 3, by + bh / 2 + 10, bx + bw - 3, by + bh / 2 + 10);
                }
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}

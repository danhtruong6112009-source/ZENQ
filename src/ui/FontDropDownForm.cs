using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class FontDropDownForm : Form
    {
        private TextBox txtSearch;
        private Panel topPanel;
        private FontListControl listControl;
        private Color _accentColor;
        private bool _isSearchFocused = false;
        private bool _isClearHover = false;
        private List<string> allFonts = new List<string>();

        public string SelectedFontName { get; private set; }
        public event Action<string> OnFontSelected;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        public FontDropDownForm(string currentFont, Point screenLocation, IEnumerable<string> fontList = null, Color? accentColor = null)
        {
            _accentColor = accentColor ?? Color.FromArgb(16, 110, 190);
            SelectedFontName = !string.IsNullOrEmpty(currentFont) ? currentFont : "Calibri";

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int desiredW = 260;
            // 42px search header + 4px pad + 9 items of 32px + 4px pad = 338px
            int desiredH = 42 + 4 + (9 * 32) + 4;

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            // If overflowing bottom edge, flip above trigger point
            if (targetY + desiredH > wa.Bottom)
            {
                int aboveY = targetY - desiredH - 28;
                if (aboveY >= wa.Top)
                {
                    targetY = aboveY;
                }
                else
                {
                    targetY = Math.Max(wa.Top + 5, wa.Bottom - desiredH - 5);
                    desiredH = Math.Min(desiredH, wa.Bottom - targetY - 5);
                }
            }
            if (targetX + desiredW > wa.Right) targetX = wa.Right - desiredW - 5;
            if (targetX < wa.Left) targetX = wa.Left + 5;

            Location = new Point(targetX, targetY);
            Size = new Size(desiredW, desiredH);

            // Populate system font collection
            if (fontList != null)
            {
                foreach (string f in fontList)
                {
                    if (!string.IsNullOrWhiteSpace(f) && !allFonts.Contains(f))
                    {
                        allFonts.Add(f);
                    }
                }
            }

            if (allFonts.Count == 0)
            {
                allFonts.AddRange(new string[] {
                    "Calibri",
                    "Times New Roman",
                    "Arial",
                    "Arial Black",
                    "Bahnschrift",
                    "Cambria",
                    "Comic Sans MS",
                    "Courier New",
                    "Georgia",
                    "Impact",
                    "Segoe UI",
                    "Segoe Script",
                    "Tahoma",
                    "Trebuchet MS",
                    "Verdana"
                });
            }

            // ==================== 1. Modern Search Header (42px) ====================
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.None,
                Location = new Point(32, 12),
                Width = desiredW - 62,
                Height = 18
            };

            try
            {
                SendMessage(txtSearch.Handle, 0x1501, 1, "Tìm phông chữ...");
            }
            catch { }

            txtSearch.GotFocus += (s, e) =>
            {
                _isSearchFocused = true;
                topPanel.Invalidate();
            };

            txtSearch.LostFocus += (s, e) =>
            {
                _isSearchFocused = false;
                topPanel.Invalidate();
            };

            txtSearch.TextChanged += (s, e) =>
            {
                FilterList(txtSearch.Text);
                topPanel.Invalidate();
            };

            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down)
                {
                    listControl.SelectNext();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    listControl.SelectPrevious();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    SelectCurrentFontAndClose();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };

            topPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Separator line at bottom of search header
                using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawLine(sepPen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
                }

                // Search pill: x=7, y=6, w=Width - 14, h=30
                Rectangle pillR = new Rectangle(7, 6, topPanel.Width - 14, 30);
                using (SolidBrush pillBg = new SolidBrush(Color.White))
                {
                    FillPill(g, pillBg, pillR, 4f);
                }
                Color borderCol = _isSearchFocused ? _accentColor : Color.FromArgb(203, 213, 225);
                using (Pen pillBorder = new Pen(borderCol, _isSearchFocused ? 1.5f : 1.0f))
                {
                    DrawPill(g, pillBorder, pillR, 4f);
                }

                // Vector search icon 🔍
                using (Pen iconPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
                {
                    g.DrawEllipse(iconPen, 15, 12, 9, 9);
                    g.DrawLine(iconPen, 22.5f, 19.5f, 26f, 23f);
                }

                // Clear button (✕) when search has text
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    Rectangle clearR = new Rectangle(topPanel.Width - 28, 12, 16, 16);
                    if (_isClearHover)
                    {
                        using (SolidBrush hovB = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        {
                            FillPill(g, hovB, clearR, 8f);
                        }
                    }
                    using (Pen xPen = new Pen(Color.FromArgb(100, 116, 139), 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(xPen, clearR.X + 4, clearR.Y + 4, clearR.Right - 4, clearR.Bottom - 4);
                        g.DrawLine(xPen, clearR.Right - 4, clearR.Y + 4, clearR.X + 4, clearR.Bottom - 4);
                    }
                }
            };

            topPanel.MouseMove += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    Rectangle clearR = new Rectangle(topPanel.Width - 28, 12, 16, 16);
                    bool hover = clearR.Contains(e.Location);
                    if (hover != _isClearHover)
                    {
                        _isClearHover = hover;
                        topPanel.Invalidate();
                    }
                }
            };

            topPanel.MouseLeave += (s, e) =>
            {
                if (_isClearHover)
                {
                    _isClearHover = false;
                    topPanel.Invalidate();
                }
            };

            topPanel.MouseDown += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    Rectangle clearR = new Rectangle(topPanel.Width - 28, 12, 16, 16);
                    if (clearR.Contains(e.Location))
                    {
                        txtSearch.Text = "";
                        txtSearch.Focus();
                    }
                }
            };

            topPanel.Controls.Add(txtSearch);

            // ==================== 2. Custom Font List ====================
            listControl = new FontListControl(allFonts, SelectedFontName, _accentColor)
            {
                Dock = DockStyle.Fill
            };

            listControl.OnFontClicked += (font) =>
            {
                SelectedFontName = font;
                if (OnFontSelected != null) OnFontSelected(font);
                Close();
            };

            Deactivate += (s, e) => Close();

            Controls.Add(listControl);
            Controls.Add(topPanel);
        }

        private void FilterList(string filter)
        {
            listControl.ApplyFilter(filter);
        }

        private void SelectCurrentFontAndClose()
        {
            string chosen = listControl.GetActiveOrHighlightedFont();
            if (!string.IsNullOrEmpty(chosen))
            {
                SelectedFontName = chosen;
                if (OnFontSelected != null) OnFontSelected(chosen);
                Close();
            }
        }

        private static void FillPill(Graphics g, Brush b, Rectangle r, float radius)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                float d = radius * 2;
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                g.FillPath(b, p);
            }
        }

        private static void DrawPill(Graphics g, Pen pen, Rectangle r, float radius)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                float d = radius * 2;
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                g.DrawPath(pen, p);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    /// <summary>
    /// Custom-drawn, double-buffered font picker list with authentic typography rendering,
    /// smart caching, interactive hover, and smooth Fluent scrollbar.
    /// </summary>
    public class FontListControl : Control
    {
        private const int ITEM_HEIGHT = 32;
        private static readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _masterList;
        private List<string> _filteredList = new List<string>();
        private string _activeFont;
        private Color _accentColor;
        private int _hoverIndex = -1;
        private int _selectedIndex = 0;
        private int _scrollY = 0;
        private bool _isDraggingScroll = false;
        private int _dragStartY = 0;
        private int _dragStartScrollY = 0;

        public event Action<string> OnFontClicked;

        public FontListControl(List<string> masterList, string activeFont, Color accentColor)
        {
            _masterList = masterList ?? new List<string>();
            _activeFont = activeFont ?? "Calibri";
            _accentColor = accentColor;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
            Cursor = Cursors.Default;

            ApplyFilter("");
        }

        public void ApplyFilter(string query)
        {
            _filteredList.Clear();
            string clean = (query ?? "").Trim().ToLower();

            for (int i = 0; i < _masterList.Count; i++)
            {
                string fn = _masterList[i];
                if (string.IsNullOrEmpty(clean) || fn.ToLower().Contains(clean))
                {
                    _filteredList.Add(fn);
                }
            }

            _selectedIndex = GetFontIndex(_activeFont);
            if (_selectedIndex < 0 && _filteredList.Count > 0) _selectedIndex = 0;

            // Center selected font in viewport
            if (_selectedIndex >= 0)
            {
                int visibleBefore = 2;
                _scrollY = Math.Max(0, (_selectedIndex - visibleBefore) * ITEM_HEIGHT);
                int maxS = GetMaxScroll();
                if (_scrollY > maxS) _scrollY = maxS;
            }
            else
            {
                _scrollY = 0;
            }

            _hoverIndex = -1;
            Invalidate();
        }

        public void SelectNext()
        {
            if (_filteredList.Count == 0) return;
            _selectedIndex = Math.Min(_filteredList.Count - 1, _selectedIndex + 1);
            EnsureIndexVisible(_selectedIndex);
            Invalidate();
        }

        public void SelectPrevious()
        {
            if (_filteredList.Count == 0) return;
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
            EnsureIndexVisible(_selectedIndex);
            Invalidate();
        }

        public string GetActiveOrHighlightedFont()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _filteredList.Count)
            {
                return CleanFamilyName(_filteredList[_selectedIndex]);
            }
            if (_filteredList.Count > 0)
            {
                return CleanFamilyName(_filteredList[0]);
            }
            return _activeFont;
        }

        private void EnsureIndexVisible(int idx)
        {
            int itemY = idx * ITEM_HEIGHT;
            if (itemY < _scrollY)
            {
                _scrollY = itemY;
            }
            else if (itemY + ITEM_HEIGHT > _scrollY + Height)
            {
                _scrollY = itemY + ITEM_HEIGHT - Height;
            }
            int maxS = GetMaxScroll();
            if (_scrollY > maxS) _scrollY = maxS;
            if (_scrollY < 0) _scrollY = 0;
        }

        private int GetFontIndex(string fontName)
        {
            string cleanTarget = CleanFamilyName(fontName);
            for (int i = 0; i < _filteredList.Count; i++)
            {
                if (string.Equals(CleanFamilyName(_filteredList[i]), cleanTarget, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static string CleanFamilyName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace("(Headings)", "").Replace("(Body)", "").Trim();
        }

        private int GetTotalHeight()
        {
            return _filteredList.Count * ITEM_HEIGHT;
        }

        private int GetMaxScroll()
        {
            return Math.Max(0, GetTotalHeight() - Height);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int maxS = GetMaxScroll();
            if (maxS <= 0) return;

            int delta = (e.Delta / 120) * (ITEM_HEIGHT * 2);
            _scrollY = Math.Max(0, Math.Min(maxS, _scrollY - delta));
            UpdateHover(e.Location);
            Invalidate();
        }

        private void UpdateHover(Point pt)
        {
            if (pt.X > Width - 10)
            {
                if (_hoverIndex != -1) { _hoverIndex = -1; Invalidate(); }
                return;
            }

            int virtualY = pt.Y - 4 + _scrollY;
            int newIdx = virtualY / ITEM_HEIGHT;
            if (newIdx >= 0 && newIdx < _filteredList.Count)
            {
                if (_hoverIndex != newIdx)
                {
                    _hoverIndex = newIdx;
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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDraggingScroll)
            {
                int maxS = GetMaxScroll();
                if (maxS > 0)
                {
                    int trackH = Height - 6;
                    int thumbH = Math.Max(24, (Height * trackH) / GetTotalHeight());
                    int scrollableTrack = trackH - thumbH;
                    if (scrollableTrack > 0)
                    {
                        int deltaY = e.Y - _dragStartY;
                        int newScroll = _dragStartScrollY + (deltaY * maxS) / scrollableTrack;
                        _scrollY = Math.Max(0, Math.Min(maxS, newScroll));
                        Invalidate();
                    }
                }
                return;
            }

            UpdateHover(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_isDraggingScroll && _hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                // Check if scrollbar clicked
                if (e.X >= Width - 10)
                {
                    _isDraggingScroll = true;
                    _dragStartY = e.Y;
                    _dragStartScrollY = _scrollY;
                    return;
                }

                int virtualY = e.Y - 4 + _scrollY;
                int idx = virtualY / ITEM_HEIGHT;
                if (idx >= 0 && idx < _filteredList.Count)
                {
                    _selectedIndex = idx;
                    string clickedFont = CleanFamilyName(_filteredList[idx]);
                    if (OnFontClicked != null)
                    {
                        OnFontClicked(clickedFont);
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingScroll = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            e.Graphics.SetClip(ClientRectangle);

            int activeIdx = GetFontIndex(_activeFont);
            int topY = 4 - _scrollY;

            Color selBgColor = Color.FromArgb(24, _accentColor.R, _accentColor.G, _accentColor.B);
            Color selBorderColor = Color.FromArgb(120, _accentColor.R, _accentColor.G, _accentColor.B);

            using (SolidBrush darkText = new SolidBrush(Color.FromArgb(31, 41, 55)))
            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (SolidBrush selBg = new SolidBrush(selBgColor))
            using (Pen selBorder = new Pen(selBorderColor, 1.0f))
            using (SolidBrush accentBrush = new SolidBrush(_accentColor))
            using (Pen checkPen = new Pen(_accentColor, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                for (int i = 0; i < _filteredList.Count; i++)
                {
                    int itemY = topY + (i * ITEM_HEIGHT);
                    if (itemY + ITEM_HEIGHT <= 0 || itemY >= Height) continue;

                    Rectangle itemR = new Rectangle(5, itemY + 2, Width - 16, ITEM_HEIGHT - 4);
                    bool isActive = (i == activeIdx);
                    bool isKeyboardSelected = (i == _selectedIndex);
                    bool isHover = (i == _hoverIndex);

                    if (isActive)
                    {
                        FillPill(g, selBg, itemR, 4f);
                        DrawPill(g, selBorder, itemR, 4f);

                        // Vector checkmark ✓ on the left
                        float cx = itemR.X + 8f;
                        float cy = itemR.Y + itemR.Height / 2f;
                        g.DrawLine(checkPen, cx, cy, cx + 2.8f, cy + 3.2f);
                        g.DrawLine(checkPen, cx + 2.8f, cy + 3.2f, cx + 7.8f, cy - 3.2f);

                        string fName = _filteredList[i];
                        Font previewF = GetOrCreateFont(CleanFamilyName(fName), 10.5f);
                        g.DrawString(fName, previewF, accentBrush, itemR.X + 24, itemR.Y + 4);
                    }
                    else if (isHover || isKeyboardSelected)
                    {
                        FillPill(g, hoverBg, itemR, 4f);
                        string fName = _filteredList[i];
                        Font previewF = GetOrCreateFont(CleanFamilyName(fName), 10.5f);
                        g.DrawString(fName, previewF, darkText, itemR.X + 24, itemR.Y + 4);
                    }
                    else
                    {
                        string fName = _filteredList[i];
                        Font previewF = GetOrCreateFont(CleanFamilyName(fName), 10.5f);
                        g.DrawString(fName, previewF, darkText, itemR.X + 24, itemR.Y + 4);
                    }
                }
            }

            // Slim custom scrollbar on the right
            int maxScroll = GetMaxScroll();
            if (maxScroll > 0)
            {
                int trackH = Height - 6;
                int thumbH = Math.Max(28, (Height * trackH) / GetTotalHeight());
                int thumbY = 3 + (_scrollY * (trackH - thumbH)) / maxScroll;

                Rectangle thumbR = new Rectangle(Width - 6, thumbY, 3, thumbH);
                Color thumbCol = _isDraggingScroll ? Color.FromArgb(100, 116, 139) : Color.FromArgb(203, 213, 225);
                using (SolidBrush tb = new SolidBrush(thumbCol))
                {
                    FillPill(g, tb, thumbR, 1.5f);
                }
            }
        }

        private static Font GetOrCreateFont(string familyName, float size)
        {
            string key = familyName + "_" + size.ToString("0.#");
            Font f;
            if (_fontCache.TryGetValue(key, out f))
            {
                return f;
            }

            try
            {
                f = new Font(familyName, size, FontStyle.Regular);
            }
            catch
            {
                try
                {
                    f = new Font(familyName, size, FontStyle.Bold);
                }
                catch
                {
                    f = new Font("Segoe UI", size, FontStyle.Regular);
                }
            }

            _fontCache[key] = f;
            return f;
        }

        private static void FillPill(Graphics g, Brush b, Rectangle r, float radius)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                float d = radius * 2;
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                g.FillPath(b, p);
            }
        }

        private static void DrawPill(Graphics g, Pen pen, Rectangle r, float radius)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                float d = radius * 2;
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                g.DrawPath(pen, p);
            }
        }
    }
}

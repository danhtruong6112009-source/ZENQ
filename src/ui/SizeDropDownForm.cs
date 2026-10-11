using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class SizeDropDownForm : Form
    {
        private TextBox txtCustomSize;
        private Panel topPanel;
        private SizeListControl listControl;
        private Color _accentColor;
        private bool _isInputFocused = false;
        private bool _isBtnIncHover = false;
        private bool _isBtnDecHover = false;

        public float SelectedFontSize { get; private set; }
        public event Action<float> OnSizeSelected;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        public SizeDropDownForm(float currentSize, Point screenLocation, Color? accentColor = null)
        {
            _accentColor = accentColor ?? Color.FromArgb(16, 110, 190);
            SelectedFontSize = currentSize > 0 ? currentSize : 11f;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int desiredW = 84;
            // 38px header + exactly 9 items of 26px = 272px for zero clipped items
            int desiredH = 38 + (9 * 26);
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

            // ==================== 1. Top Header Pill (38px) ====================
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            txtCustomSize = new TextBox
            {
                Text = SelectedFontSize.ToString("0.#"),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.None,
                Location = new Point(24, 10),
                Width = 34,
                Height = 18,
                TextAlign = HorizontalAlignment.Center
            };

            txtCustomSize.GotFocus += (s, e) =>
            {
                _isInputFocused = true;
                txtCustomSize.SelectAll();
                topPanel.Invalidate();
            };

            txtCustomSize.LostFocus += (s, e) =>
            {
                _isInputFocused = false;
                topPanel.Invalidate();
            };

            txtCustomSize.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ApplyCustomTextAndClose();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Close();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    IncrementSize(1f);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    IncrementSize(-1f);
                    e.Handled = true;
                }
            };

            topPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Separator line below header
                using (Pen sepPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawLine(sepPen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
                }

                // Header pill: x=6, y=6, w=topPanel.Width - 12, h=26
                Rectangle pillR = new Rectangle(6, 6, topPanel.Width - 12, 26);
                using (SolidBrush pillBg = new SolidBrush(Color.White))
                {
                    FillPill(g, pillBg, pillR, 3.5f);
                }
                Color borderCol = _isInputFocused ? _accentColor : Color.FromArgb(203, 213, 225);
                using (Pen pillBorder = new Pen(borderCol, _isInputFocused ? 1.5f : 1.0f))
                {
                    DrawPill(g, pillBorder, pillR, 3.5f);
                }

                // "pt" badge
                using (Font badgeF = new Font("Segoe UI", 7.8f, FontStyle.Bold))
                using (SolidBrush badgeB = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("pt", badgeF, badgeB, 10, 11);
                }

                // Divider line before steppers
                int btnW = 18;
                int divX = pillR.Right - btnW;
                using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawLine(divPen, divX, 8, divX, pillR.Bottom - 3);
                }

                // Steppers micro-buttons
                int btnH = 13;
                Rectangle incR = new Rectangle(divX, 6, btnW, btnH);
                Rectangle decR = new Rectangle(divX, 19, btnW, btnH);

                if (_isBtnIncHover)
                {
                    using (SolidBrush hovB = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        g.FillRectangle(hovB, incR);
                }
                if (_isBtnDecHover)
                {
                    using (SolidBrush hovB = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        g.FillRectangle(hovB, decR);
                }

                using (Pen arrPen = new Pen(Color.FromArgb(100, 116, 139), 1.2f) { LineJoin = LineJoin.Round })
                {
                    // Up arrow ▲
                    float cx = incR.X + btnW / 2f;
                    float cy = incR.Y + btnH / 2f + 1;
                    g.DrawLine(arrPen, cx - 3f, cy + 1.5f, cx, cy - 1.5f);
                    g.DrawLine(arrPen, cx, cy - 1.5f, cx + 3f, cy + 1.5f);

                    // Down arrow ▼
                    cy = decR.Y + btnH / 2f - 1;
                    g.DrawLine(arrPen, cx - 3f, cy - 1.5f, cx, cy + 1.5f);
                    g.DrawLine(arrPen, cx, cy + 1.5f, cx + 3f, cy - 1.5f);
                }
            };

            topPanel.MouseMove += (s, e) =>
            {
                int btnW = 18;
                int divX = topPanel.Width - 6 - btnW;
                bool newInc = (e.X >= divX && e.X <= topPanel.Width - 6 && e.Y >= 6 && e.Y < 19);
                bool newDec = (e.X >= divX && e.X <= topPanel.Width - 6 && e.Y >= 19 && e.Y <= 32);
                if (newInc != _isBtnIncHover || newDec != _isBtnDecHover)
                {
                    _isBtnIncHover = newInc;
                    _isBtnDecHover = newDec;
                    topPanel.Invalidate();
                }
            };

            topPanel.MouseLeave += (s, e) =>
            {
                if (_isBtnIncHover || _isBtnDecHover)
                {
                    _isBtnIncHover = false;
                    _isBtnDecHover = false;
                    topPanel.Invalidate();
                }
            };

            topPanel.MouseDown += (s, e) =>
            {
                int btnW = 18;
                int divX = topPanel.Width - 6 - btnW;
                if (e.X >= divX && e.X <= topPanel.Width - 6)
                {
                    if (e.Y >= 6 && e.Y < 19)
                    {
                        IncrementSize(1f);
                    }
                    else if (e.Y >= 19 && e.Y <= 32)
                    {
                        IncrementSize(-1f);
                    }
                }
            };

            topPanel.Controls.Add(txtCustomSize);

            // ==================== 2. Custom-Painted Scrollable List ====================
            listControl = new SizeListControl(SelectedFontSize, _accentColor)
            {
                Dock = DockStyle.Fill
            };

            listControl.OnSizeClicked += (sz) =>
            {
                SelectedFontSize = sz;
                if (OnSizeSelected != null) OnSizeSelected(sz);
                Close();
            };

            Deactivate += (s, e) => Close();

            Controls.Add(listControl);
            Controls.Add(topPanel);
        }

        private void IncrementSize(float delta)
        {
            float cur;
            if (!float.TryParse(txtCustomSize.Text, out cur)) cur = SelectedFontSize;
            float next = cur + delta;
            if (next < 1f) next = 1f;
            if (next > 144f) next = 144f;
            SelectedFontSize = next;
            txtCustomSize.Text = next.ToString("0.#");
            txtCustomSize.SelectAll();
            listControl.SetCurrentSize(next);
            if (OnSizeSelected != null) OnSizeSelected(next);
        }

        private void ApplyCustomTextAndClose()
        {
            float parsed;
            if (float.TryParse(txtCustomSize.Text, out parsed) && parsed > 0 && parsed <= 500)
            {
                SelectedFontSize = parsed;
                if (OnSizeSelected != null) OnSizeSelected(SelectedFontSize);
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
    /// Sleek, custom-drawn scrollable font size list with interactive hover, active checkmarks, and thin Fluent scrollbar.
    /// </summary>
    public class SizeListControl : Control
    {
        private readonly float[] _sizes = new float[] {
            8f, 9f, 10f, 10.5f, 11f, 12f, 14f, 16f, 18f, 20f, 22f, 24f, 26f, 28f, 36f, 48f, 72f
        };

        private const int ITEM_HEIGHT = 26;
        private float _currentSize;
        private Color _accentColor;
        private int _hoverIndex = -1;
        private int _scrollY = 0;
        private bool _isDraggingScroll = false;
        private int _dragStartY = 0;
        private int _dragStartScrollY = 0;

        public event Action<float> OnSizeClicked;

        public SizeListControl(float currentSize, Color accentColor)
        {
            _currentSize = currentSize;
            _accentColor = accentColor;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
            Cursor = Cursors.Default;

            // Scroll so selected item is cleanly in view without clipping top/bottom
            int selIdx = GetSelectedIndex();
            if (selIdx >= 0)
            {
                int visibleBefore = 2;
                _scrollY = Math.Max(0, (selIdx - visibleBefore) * ITEM_HEIGHT);
            }
        }

        public void SetCurrentSize(float sz)
        {
            _currentSize = sz;
            int selIdx = GetSelectedIndex();
            if (selIdx >= 0)
            {
                int visibleBefore = 2;
                _scrollY = Math.Max(0, (selIdx - visibleBefore) * ITEM_HEIGHT);
                int maxS = GetMaxScroll();
                if (_scrollY > maxS) _scrollY = maxS;
            }
            Invalidate();
        }

        private int GetSelectedIndex()
        {
            for (int i = 0; i < _sizes.Length; i++)
            {
                if (Math.Abs(_sizes[i] - _currentSize) < 0.05f) return i;
            }
            return -1;
        }

        private int GetTotalHeight()
        {
            return _sizes.Length * ITEM_HEIGHT;
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
            if (pt.X > Width - 8)
            {
                if (_hoverIndex != -1) { _hoverIndex = -1; Invalidate(); }
                return;
            }

            int virtualY = pt.Y + _scrollY;
            int newIdx = virtualY / ITEM_HEIGHT;
            if (newIdx >= 0 && newIdx < _sizes.Length)
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
                if (e.X >= Width - 8)
                {
                    _isDraggingScroll = true;
                    _dragStartY = e.Y;
                    _dragStartScrollY = _scrollY;
                    return;
                }

                int virtualY = e.Y + _scrollY;
                int idx = virtualY / ITEM_HEIGHT;
                if (idx >= 0 && idx < _sizes.Length)
                {
                    if (OnSizeClicked != null)
                    {
                        OnSizeClicked(_sizes[idx]);
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

            int selIdx = GetSelectedIndex();
            int topY = 0 - _scrollY;

            Color selBgColor = Color.FromArgb(24, _accentColor.R, _accentColor.G, _accentColor.B);
            Color selBorderColor = Color.FromArgb(120, _accentColor.R, _accentColor.G, _accentColor.B);

            using (Font regFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (Font boldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush darkText = new SolidBrush(Color.FromArgb(31, 41, 55)))
            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (SolidBrush selBg = new SolidBrush(selBgColor))
            using (Pen selBorder = new Pen(selBorderColor, 1.0f))
            using (SolidBrush accentBrush = new SolidBrush(_accentColor))
            using (Pen checkPen = new Pen(_accentColor, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                for (int i = 0; i < _sizes.Length; i++)
                {
                    int itemY = topY + (i * ITEM_HEIGHT);
                    if (itemY + ITEM_HEIGHT <= 0 || itemY >= Height) continue;

                    Rectangle itemR = new Rectangle(4, itemY + 1, Width - 12, ITEM_HEIGHT - 2);
                    bool isSelected = (i == selIdx);
                    bool isHover = (i == _hoverIndex);

                    if (isSelected)
                    {
                        FillPill(g, selBg, itemR, 3.5f);
                        DrawPill(g, selBorder, itemR, 3.5f);

                        // Draw vector checkmark ✓
                        float cx = itemR.X + 7.5f;
                        float cy = itemR.Y + itemR.Height / 2f;
                        g.DrawLine(checkPen, cx, cy, cx + 2.8f, cy + 3.2f);
                        g.DrawLine(checkPen, cx + 2.8f, cy + 3.2f, cx + 7.8f, cy - 3.2f);

                        // Size string in bold accent
                        string str = _sizes[i].ToString("0.#");
                        g.DrawString(str, boldFont, accentBrush, itemR.X + 22, itemR.Y + 3);
                    }
                    else if (isHover)
                    {
                        FillPill(g, hoverBg, itemR, 3.5f);
                        string str = _sizes[i].ToString("0.#");
                        g.DrawString(str, regFont, darkText, itemR.X + 22, itemR.Y + 3);
                    }
                    else
                    {
                        string str = _sizes[i].ToString("0.#");
                        g.DrawString(str, regFont, darkText, itemR.X + 22, itemR.Y + 3);
                    }
                }
            }

            // Draw thin custom scrollbar on the right
            int maxScroll = GetMaxScroll();
            if (maxScroll > 0)
            {
                int trackH = Height - 6;
                int thumbH = Math.Max(24, (Height * trackH) / GetTotalHeight());
                int thumbY = 3 + (_scrollY * (trackH - thumbH)) / maxScroll;

                Rectangle thumbR = new Rectangle(Width - 5, thumbY, 3, thumbH);
                Color thumbCol = _isDraggingScroll ? Color.FromArgb(100, 116, 139) : Color.FromArgb(203, 213, 225);
                using (SolidBrush tb = new SolidBrush(thumbCol))
                {
                    FillPill(g, tb, thumbR, 1.5f);
                }
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
    }
}

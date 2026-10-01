using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    #region Owner-Drawn Vector Gallery Forms

    /// <summary>
    /// Visual Style Gallery Popup matching Microsoft Word Style Cards
    /// </summary>
    public class WriterStylesGalleryForm : Form
    {
        private WriterView _writerView;
        private RibbonBar _ribbon;
        private Form _parentForm;
        private int _hoverIndex = -1;

        private struct StyleItem
        {
            public string Name;
            public string Subtext;
            public Font PreviewFont;
            public Color TextColor;
            public int Level;
        }

        private StyleItem[] _styles;

        public WriterStylesGalleryForm(Point screenLocation, WriterView writerView, RibbonBar ribbon, Form parentForm)
        {
            _writerView = writerView;
            _ribbon = ribbon;
            _parentForm = parentForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _styles = new StyleItem[]
            {
                new StyleItem { Name = "Normal", Subtext = "Văn bản chuẩn", PreviewFont = new Font("Segoe UI", 10f, FontStyle.Regular), TextColor = Color.Black, Level = 0 },
                new StyleItem { Name = "No Spacing", Subtext = "Không dãn cách dòng", PreviewFont = new Font("Segoe UI", 10f, FontStyle.Regular), TextColor = Color.Black, Level = 6 },
                new StyleItem { Name = "Heading 1", Subtext = "Tiêu đề cấp 1", PreviewFont = new Font("Segoe UI Semibold", 13f, FontStyle.Bold), TextColor = Color.FromArgb(43, 87, 154), Level = 1 },
                new StyleItem { Name = "Heading 2", Subtext = "Tiêu đề cấp 2", PreviewFont = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold), TextColor = Color.FromArgb(43, 87, 154), Level = 2 },
                new StyleItem { Name = "Heading 3", Subtext = "Tiêu đề cấp 3", PreviewFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), TextColor = Color.FromArgb(31, 78, 121), Level = 3 },
                new StyleItem { Name = "Title", Subtext = "Tiêu đề tài liệu lớn", PreviewFont = new Font("Segoe UI", 14f, FontStyle.Bold), TextColor = Color.FromArgb(43, 87, 154), Level = 4 },
                new StyleItem { Name = "Subtitle", Subtext = "Tiêu đề phụ / Diễn giải", PreviewFont = new Font("Segoe UI", 10.5f, FontStyle.Italic), TextColor = Color.FromArgb(107, 114, 128), Level = 5 }
            };

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int desiredW = 280;
            int desiredH = 34 + _styles.Length * 46 + 8;
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            if (targetY + desiredH > wa.Bottom)
            {
                int aboveY = targetY - desiredH - 30;
                if (aboveY >= wa.Top) targetY = aboveY;
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
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = _hoverIndex;
            _hoverIndex = -1;
            int startY = 32;
            for (int i = 0; i < _styles.Length; i++)
            {
                Rectangle cardRect = new Rectangle(6, startY + i * 46, Width - 12, 42);
                if (cardRect.Contains(e.Location))
                {
                    _hoverIndex = i;
                    break;
                }
            }
            if (prev != _hoverIndex) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _hoverIndex >= 0 && _hoverIndex < _styles.Length)
            {
                string sName = _styles[_hoverIndex].Name;
                if (_writerView != null)
                {
                    _writerView.ApplyQuickStyle(sName, _ribbon);
                    if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon);
                    if (_parentForm != null) _parentForm.Invalidate();
                }
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border & Header
            using (Pen borderPen = new Pen(Color.FromArgb(209, 213, 219), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            using (SolidBrush hdrBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
            {
                g.FillRectangle(hdrBg, 1, 1, Width - 2, 28);
            }

            using (Font fHdr = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.DrawString("Style Gallery (Mẫu phong cách)", fHdr, Brushes.DimGray, 8, 6);
            }

            int startY = 32;
            for (int i = 0; i < _styles.Length; i++)
            {
                Rectangle cardRect = new Rectangle(6, startY + i * 46, Width - 12, 42);
                bool isHover = (_hoverIndex == i);

                if (isHover)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 243, 252))) g.FillRectangle(b, cardRect);
                    using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.2f)) g.DrawRectangle(p, cardRect);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(250, 250, 250))) g.FillRectangle(b, cardRect);
                    using (Pen p = new Pen(Color.FromArgb(229, 231, 235), 1f)) g.DrawRectangle(p, cardRect);
                }

                // Vector typography sample box
                Rectangle sampleBox = new Rectangle(cardRect.Left + 4, cardRect.Top + 4, 75, cardRect.Height - 8);
                using (SolidBrush sb = new SolidBrush(Color.White)) g.FillRectangle(sb, sampleBox);
                using (Pen sp = new Pen(Color.FromArgb(209, 213, 219), 1f)) g.DrawRectangle(sp, sampleBox);

                using (SolidBrush textBrush = new SolidBrush(_styles[i].TextColor))
                {
                    TextRenderer.DrawText(g, "AaBbCc", _styles[i].PreviewFont, sampleBox, _styles[i].TextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Style Name & Subtext
                using (Font fTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (Font fSub = new Font("Segoe UI", 8f, FontStyle.Regular))
                {
                    g.DrawString(_styles[i].Name, fTitle, Brushes.Black, cardRect.Left + 86, cardRect.Top + 4);
                    g.DrawString(_styles[i].Subtext, fSub, Brushes.Gray, cardRect.Left + 86, cardRect.Top + 22);
                }
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }

    /// <summary>
    /// Visual Vector Bullet Library Popup
    /// </summary>
    public class WriterBulletsGalleryForm : Form
    {
        private WriterView _writerView;
        private Form _parentForm;
        private int _hoverIdx = -1;

        public WriterBulletsGalleryForm(Point screenLocation, WriterView writerView, Form parentForm)
        {
            _writerView = writerView;
            _parentForm = parentForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int desiredW = 190;
            int desiredH = 145;
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            if (targetY + desiredH > wa.Bottom) targetY = Math.Max(wa.Top + 5, targetY - desiredH - 30);
            if (targetX + desiredW > wa.Right) targetX = wa.Right - desiredW - 5;
            if (targetX < wa.Left) targetX = wa.Left + 5;

            Location = new Point(targetX, targetY);
            Size = new Size(desiredW, desiredH);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = _hoverIdx;
            _hoverIdx = -1;
            int startX = 8, startY = 30;
            for (int i = 0; i < 6; i++)
            {
                int r = i / 3;
                int c = i % 3;
                Rectangle tile = new Rectangle(startX + c * 58, startY + r * 52, 52, 46);
                if (tile.Contains(e.Location))
                {
                    _hoverIdx = i;
                    break;
                }
            }
            if (prev != _hoverIdx) Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _hoverIdx >= 0)
            {
                if (_writerView != null)
                {
                    _writerView.ToggleBullets();
                    if (_parentForm != null) _parentForm.Invalidate();
                }
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen borderPen = new Pen(Color.FromArgb(209, 213, 219), 1f))
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            using (SolidBrush hdrBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                g.FillRectangle(hdrBg, 1, 1, Width - 2, 24);

            using (Font fHdr = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                g.DrawString("Bullet Library (Dấu đầu dòng)", fHdr, Brushes.DimGray, 6, 4);

            int startX = 8, startY = 30;
            for (int i = 0; i < 6; i++)
            {
                int r = i / 3;
                int c = i % 3;
                Rectangle tile = new Rectangle(startX + c * 58, startY + r * 52, 52, 46);
                bool isHov = (_hoverIdx == i);

                if (isHov)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 243, 252))) g.FillRectangle(b, tile);
                    using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.2f)) g.DrawRectangle(p, tile);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(250, 250, 250))) g.FillRectangle(b, tile);
                    using (Pen p = new Pen(Color.FromArgb(229, 231, 235), 1f)) g.DrawRectangle(p, tile);
                }

                int cx = tile.X + 14;
                int cy = tile.Y + tile.Height / 2;

                using (Pen pLine = new Pen(Color.FromArgb(156, 163, 175), 1.2f))
                {
                    g.DrawLine(pLine, cx + 10, cy - 6, tile.Right - 8, cy - 6);
                    g.DrawLine(pLine, cx + 10, cy, tile.Right - 8, cy);
                    g.DrawLine(pLine, cx + 10, cy + 6, tile.Right - 8, cy + 6);
                }

                // Vector Bullet Shape
                switch (i)
                {
                    case 0: // Solid Circle ●
                        g.FillEllipse(Brushes.Black, cx - 4, cy - 4, 8, 8);
                        break;
                    case 1: // Hollow Circle ○
                        using (Pen p = new Pen(Color.Black, 1.5f)) g.DrawEllipse(p, cx - 4, cy - 4, 8, 8);
                        break;
                    case 2: // Solid Square ■
                        g.FillRectangle(Brushes.Black, cx - 4, cy - 4, 8, 8);
                        break;
                    case 3: // Diamond ◆
                        Point[] dPts = new Point[] { new Point(cx, cy - 5), new Point(cx + 5, cy), new Point(cx, cy + 5), new Point(cx - 5, cy) };
                        g.FillPolygon(Brushes.Black, dPts);
                        break;
                    case 4: // Arrow ➔
                        using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.6f))
                        {
                            g.DrawLine(p, cx - 5, cy, cx + 3, cy);
                            g.DrawLine(p, cx, cy - 3, cx + 4, cy);
                            g.DrawLine(p, cx, cy + 3, cx + 4, cy);
                        }
                        break;
                    case 5: // Checkmark ✓
                        using (Pen p = new Pen(Color.FromArgb(16, 124, 65), 1.8f))
                        {
                            g.DrawLine(p, cx - 4, cy, cx - 1, cy + 3);
                            g.DrawLine(p, cx - 1, cy + 3, cx + 5, cy - 4);
                        }
                        break;
                }
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }

    /// <summary>
    /// Visual Vector Numbering Library Popup
    /// </summary>
    public class WriterNumberingGalleryForm : Form
    {
        private WriterView _writerView;
        private Form _parentForm;
        private int _hoverIdx = -1;

        private string[] _numSamples = new string[] { "1.", "1)", "A.", "a.", "a)", "i." };

        public WriterNumberingGalleryForm(Point screenLocation, WriterView writerView, Form parentForm)
        {
            _writerView = writerView;
            _parentForm = parentForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int desiredW = 190;
            int desiredH = 145;
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            if (targetY + desiredH > wa.Bottom) targetY = Math.Max(wa.Top + 5, targetY - desiredH - 30);
            if (targetX + desiredW > wa.Right) targetX = wa.Right - desiredW - 5;
            if (targetX < wa.Left) targetX = wa.Left + 5;

            Location = new Point(targetX, targetY);
            Size = new Size(desiredW, desiredH);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = _hoverIdx;
            _hoverIdx = -1;
            int startX = 8, startY = 30;
            for (int i = 0; i < 6; i++)
            {
                int r = i / 3;
                int c = i % 3;
                Rectangle tile = new Rectangle(startX + c * 58, startY + r * 52, 52, 46);
                if (tile.Contains(e.Location))
                {
                    _hoverIdx = i;
                    break;
                }
            }
            if (prev != _hoverIdx) Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _hoverIdx >= 0)
            {
                if (_writerView != null)
                {
                    _writerView.ToggleNumbering();
                    if (_parentForm != null) _parentForm.Invalidate();
                }
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen borderPen = new Pen(Color.FromArgb(209, 213, 219), 1f))
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            using (SolidBrush hdrBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                g.FillRectangle(hdrBg, 1, 1, Width - 2, 24);

            using (Font fHdr = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                g.DrawString("Numbering Library (Đánh số)", fHdr, Brushes.DimGray, 6, 4);

            int startX = 8, startY = 30;
            for (int i = 0; i < 6; i++)
            {
                int r = i / 3;
                int c = i % 3;
                Rectangle tile = new Rectangle(startX + c * 58, startY + r * 52, 52, 46);
                bool isHov = (_hoverIdx == i);

                if (isHov)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 243, 252))) g.FillRectangle(b, tile);
                    using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.2f)) g.DrawRectangle(p, tile);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(250, 250, 250))) g.FillRectangle(b, tile);
                    using (Pen p = new Pen(Color.FromArgb(229, 231, 235), 1f)) g.DrawRectangle(p, tile);
                }

                using (Font fNum = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                {
                    g.DrawString(_numSamples[i], fNum, Brushes.Navy, tile.X + 3, tile.Y + 12);
                }

                int cx = tile.X + 16;
                int cy = tile.Y + tile.Height / 2;
                using (Pen pLine = new Pen(Color.FromArgb(156, 163, 175), 1.2f))
                {
                    g.DrawLine(pLine, cx + 5, cy - 6, tile.Right - 6, cy - 6);
                    g.DrawLine(pLine, cx + 5, cy, tile.Right - 6, cy);
                    g.DrawLine(pLine, cx + 5, cy + 6, tile.Right - 6, cy + 6);
                }
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }

    #endregion

    public class WriterMiniToolbarControl : UserControl
    {
        private WriterView _writerView;
        private RibbonBar _ribbon;
        private Form _parentForm;
        private WriterFloatingMiniToolbarPopup _floatingParentPopup;

        private int _hoveredBtn = -1;
        private Rectangle[] _btnRects = new Rectangle[16];

        public WriterMiniToolbarControl(WriterView writerView, RibbonBar ribbon, Form parentForm, WriterFloatingMiniToolbarPopup floatingParentPopup = null)
        {
            _writerView = writerView;
            _ribbon = ribbon;
            _parentForm = parentForm;
            _floatingParentPopup = floatingParentPopup;

            this.DoubleBuffered = true;
            this.Size = new Size(346, 58);
            this.BackColor = Color.FromArgb(249, 250, 251);
            this.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);

            ComputeLayout();
        }

        private void ComputeLayout()
        {
            // Row 1 (Y: 3, Height: 24)
            _btnRects[0] = new Rectangle(4, 3, 102, 24);  // Font Family
            _btnRects[1] = new Rectangle(110, 3, 40, 24); // Font Size
            _btnRects[2] = new Rectangle(154, 3, 22, 24); // A^
            _btnRects[3] = new Rectangle(178, 3, 22, 24); // Av
            _btnRects[4] = new Rectangle(202, 3, 24, 24); // 🧹 Format Painter / Clear
            _btnRects[5] = new Rectangle(228, 3, 48, 24); // Styles ▾
            _btnRects[6] = new Rectangle(280, 3, 62, 24); // 💬+ New Comment

            // Row 2 (Y: 30, Height: 24)
            _btnRects[7]  = new Rectangle(4, 30, 24, 24);  // B
            _btnRects[8]  = new Rectangle(30, 30, 24, 24); // I
            _btnRects[9]  = new Rectangle(56, 30, 24, 24); // U
            _btnRects[10] = new Rectangle(84, 30, 32, 24); // Highlight ✏️ ▾
            _btnRects[11] = new Rectangle(118, 30, 32, 24); // Font Color A ▾
            _btnRects[12] = new Rectangle(152, 30, 30, 24); // Bullets •≡ ▾
            _btnRects[13] = new Rectangle(184, 30, 30, 24); // Numbering 123≡ ▾
            _btnRects[14] = new Rectangle(216, 30, 30, 24); // Align Left/Center/Right ≡ ▾
            _btnRects[15] = new Rectangle(248, 30, 30, 24); // Indent ⇤ ⇥
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = _hoveredBtn;
            _hoveredBtn = -1;
            for (int i = 0; i < _btnRects.Length; i++)
            {
                if (_btnRects[i].Contains(e.Location))
                {
                    _hoveredBtn = i;
                    break;
                }
            }
            if (prev != _hoveredBtn) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredBtn = -1;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string curFont = _ribbon != null ? _ribbon.SelectedFont : "Times New Roman";
            float curSize = _ribbon != null ? _ribbon.FontSize : 12f;
            bool isB = _ribbon != null ? _ribbon.IsBold : false;
            bool isI = _ribbon != null ? _ribbon.IsItalic : false;
            bool isU = _ribbon != null ? _ribbon.IsUnderline : false;

            using (Pen borderPen = new Pen(Color.FromArgb(229, 231, 235)))
            {
                g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
                g.DrawLine(borderPen, 277, 4, 277, 26);
            }

            for (int i = 0; i < _btnRects.Length; i++)
            {
                Rectangle r = _btnRects[i];
                bool isHover = (_hoveredBtn == i);
                bool isActive = false;

                if (i == 7 && isB) isActive = true;
                if (i == 8 && isI) isActive = true;
                if (i == 9 && isU) isActive = true;

                if (isActive)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(198, 224, 252))) g.FillRectangle(b, r);
                    using (Pen p = new Pen(Color.FromArgb(16, 110, 190))) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
                }
                else if (isHover)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(237, 243, 250))) g.FillRectangle(b, r);
                    using (Pen p = new Pen(Color.FromArgb(204, 228, 247))) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
                }

                DrawButtonContent(g, i, r, curFont, curSize, isHover, isActive);
            }
        }

        private void DrawButtonContent(Graphics g, int idx, Rectangle r, string curFont, float curSize, bool isHover, bool isActive)
        {
            Color iconCol = Color.FromArgb(51, 51, 51);
            using (Font fBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font fRegular = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font fSmall = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            using (Pen pen = new Pen(iconCol, 1.2f))
            {
                switch (idx)
                {
                    case 0: // Font Combo
                        using (SolidBrush boxBg = new SolidBrush(Color.White))
                        using (Pen boxPen = new Pen(Color.FromArgb(209, 213, 219)))
                        {
                            g.FillRectangle(boxBg, r.X, r.Y + 2, r.Width, r.Height - 4);
                            g.DrawRectangle(boxPen, r.X, r.Y + 2, r.Width - 1, r.Height - 5);
                        }
                        TextRenderer.DrawText(g, curFont ?? "", fRegular, new Rectangle(r.X + 2, r.Y + 3, r.Width - 14, r.Height - 6), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        DrawArrowDown(g, r.Right - 10, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 1: // Font Size Combo
                        string sizeText = curSize > 0 ? curSize.ToString() : "";
                        using (SolidBrush boxBg = new SolidBrush(Color.White))
                        using (Pen boxPen = new Pen(Color.FromArgb(209, 213, 219)))
                        {
                            g.FillRectangle(boxBg, r.X, r.Y + 2, r.Width, r.Height - 4);
                            g.DrawRectangle(boxPen, r.X, r.Y + 2, r.Width - 1, r.Height - 5);
                        }
                        TextRenderer.DrawText(g, sizeText, fRegular, new Rectangle(r.X + 2, r.Y + 3, r.Width - 12, r.Height - 6), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        DrawArrowDown(g, r.Right - 9, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 2: // A^ (Increase font)
                        TextRenderer.DrawText(g, "A", fBold, new Rectangle(r.X, r.Y + 2, 13, r.Height - 4), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        DrawArrowUp(g, r.X + 14, r.Y + 7, Color.FromArgb(16, 124, 65));
                        break;

                    case 3: // Av (Decrease font)
                        TextRenderer.DrawText(g, "A", fRegular, new Rectangle(r.X, r.Y + 3, 12, r.Height - 5), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        DrawArrowDown(g, r.X + 13, r.Y + 13, Color.FromArgb(239, 68, 68));
                        break;

                    case 4: // 🧹 Format Painter / Clear formatting
                        DrawBrushIcon(g, r.X + 4, r.Y + 4, 16);
                        break;

                    case 5: // Styles ▾ (A with blue glow + Styles)
                        using (Font fStyles = new Font("Segoe UI", 9f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "A", fStyles, new Rectangle(r.X + 1, r.Y + 2, 14, 18), Color.FromArgb(16, 110, 190), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        }
                        using (Font fStyleLabel = new Font("Segoe UI", 7f, FontStyle.Regular))
                        {
                            TextRenderer.DrawText(g, "Styles", fStyleLabel, new Rectangle(r.X + 14, r.Y + 3, 24, 18), Color.FromArgb(55, 65, 81), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        }
                        DrawArrowDown(g, r.Right - 6, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 6: // 💬+ New Comment
                        DrawCommentIcon(g, r.X + 2, r.Y + 3, 16);
                        using (Font fComment = new Font("Segoe UI", 6.5f, FontStyle.Regular))
                        {
                            g.DrawString("New", fComment, Brushes.Black, r.X + 22, r.Y + 2);
                            g.DrawString("Comment", fComment, Brushes.Black, r.X + 20, r.Y + 11);
                        }
                        break;

                    case 7: // B
                        TextRenderer.DrawText(g, "B", fBold, r, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        break;

                    case 8: // I
                        using (Font fItalic = new Font("Segoe UI", 9f, FontStyle.Italic | FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "I", fItalic, r, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                        break;

                    case 9: // U
                        using (Font fUnder = new Font("Segoe UI", 9f, FontStyle.Underline | FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "U", fUnder, r, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                        break;

                    case 10: // Highlight ✏️ ▾
                        DrawPencilIcon(g, r.X + 5, r.Y + 3, 14);
                        Color hlCol = (_ribbon != null && _ribbon.CurrentHighlightColor != Color.Empty && _ribbon.CurrentHighlightColor != Color.Transparent) ? _ribbon.CurrentHighlightColor : Color.Yellow;
                        using (SolidBrush b = new SolidBrush(hlCol)) g.FillRectangle(b, r.X + 4, r.Bottom - 5, 16, 3);
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 11: // Font Color A ▾
                        TextRenderer.DrawText(g, "A", fBold, new Rectangle(r.X + 2, r.Y + 1, 18, 16), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        Color fontCol = (_ribbon != null && _ribbon.CurrentFontColor != Color.Empty && _ribbon.CurrentFontColor != Color.Transparent) ? _ribbon.CurrentFontColor : Color.Red;
                        using (SolidBrush b = new SolidBrush(fontCol)) g.FillRectangle(b, r.X + 4, r.Bottom - 5, 16, 3);
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 12: // Bullets •≡ ▾
                        g.FillEllipse(Brushes.Black, r.X + 4, r.Y + 6, 3, 3);
                        g.DrawLine(pen, r.X + 9, r.Y + 7, r.X + 18, r.Y + 7);
                        g.FillEllipse(Brushes.Black, r.X + 4, r.Y + 11, 3, 3);
                        g.DrawLine(pen, r.X + 9, r.Y + 12, r.X + 18, r.Y + 12);
                        g.FillEllipse(Brushes.Black, r.X + 4, r.Y + 16, 3, 3);
                        g.DrawLine(pen, r.X + 9, r.Y + 17, r.X + 18, r.Y + 17);
                        DrawArrowDown(g, r.Right - 6, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 13: // Numbering 123≡ ▾
                        g.DrawString("1.", fSmall, Brushes.Black, r.X + 1, r.Y + 2);
                        g.DrawLine(pen, r.X + 11, r.Y + 6, r.X + 18, r.Y + 6);
                        g.DrawString("2.", fSmall, Brushes.Black, r.X + 1, r.Y + 9);
                        g.DrawLine(pen, r.X + 11, r.Y + 13, r.X + 18, r.Y + 13);
                        DrawArrowDown(g, r.Right - 6, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 14: // Align ≡ ▾
                        for (int line = 0; line < 3; line++)
                        {
                            int ly = r.Y + 6 + line * 4;
                            g.DrawLine(pen, r.X + 4, ly, r.X + 18, ly);
                        }
                        DrawArrowDown(g, r.Right - 6, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 15: // Indent ⇤ ⇥
                        using (Pen pIndent = new Pen(Color.FromArgb(59, 130, 246), 1.2f))
                        {
                            pIndent.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2.5f, 2.5f);
                            g.DrawLine(pIndent, r.X + 4, r.Y + 12, r.X + 18, r.Y + 12);
                            g.DrawLine(pen, r.X + 4, r.Y + 6, r.X + 22, r.Y + 6);
                            g.DrawLine(pen, r.X + 4, r.Y + 18, r.X + 22, r.Y + 18);
                        }
                        break;
                }
            }
        }

        private void DrawArrowDown(Graphics g, int x, int y, Color col)
        {
            Point[] pts = new Point[] { new Point(x - 3, y - 1), new Point(x + 3, y - 1), new Point(x, y + 2) };
            using (SolidBrush b = new SolidBrush(col)) g.FillPolygon(b, pts);
        }

        private void DrawArrowUp(Graphics g, int x, int y, Color col)
        {
            Point[] pts = new Point[] { new Point(x, y - 3), new Point(x - 3, y + 1), new Point(x + 3, y + 1) };
            using (SolidBrush b = new SolidBrush(col)) g.FillPolygon(b, pts);
        }

        private void DrawBrushIcon(Graphics g, int x, int y, int size)
        {
            using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.3f))
            {
                g.DrawRectangle(p, x + 2, y + 2, size - 4, 6);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillRectangle(b, x + 4, y + 8, size - 8, 4);
                g.DrawLine(p, x + size / 2, y + 8, x + size / 2, y + size);
            }
        }

        private void DrawPencilIcon(Graphics g, int x, int y, int size)
        {
            using (Pen p = new Pen(Color.FromArgb(55, 65, 81), 1.2f))
            {
                Point[] pts = new Point[] { new Point(x + 3, y + size - 4), new Point(x + size - 4, y + 2), new Point(x + size - 2, y + 4), new Point(x + 5, y + size - 2) };
                g.DrawPolygon(p, pts);
            }
        }

        private void DrawCommentIcon(Graphics g, int x, int y, int size)
        {
            using (Pen p = new Pen(Color.FromArgb(16, 124, 65), 1.2f))
            {
                Point[] b = new Point[] { new Point(x + 2, y + 2), new Point(x + size - 2, y + 2), new Point(x + size - 2, y + 10), new Point(x + 6, y + 10), new Point(x + 3, y + 14), new Point(x + 4, y + 10), new Point(x + 2, y + 10) };
                g.DrawPolygon(p, b);
                using (Pen pPlus = new Pen(Color.FromArgb(16, 124, 65), 1.3f))
                {
                    g.DrawLine(pPlus, x + 8, y + 4, x + 8, y + 8);
                    g.DrawLine(pPlus, x + 6, y + 6, x + 10, y + 6);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _writerView == null) return;

            for (int i = 0; i < _btnRects.Length; i++)
            {
                if (_btnRects[i].Contains(e.Location))
                {
                    HandleButtonClick(i);
                    break;
                }
            }
        }

        private void HandleButtonClick(int idx)
        {
            if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = true;

            switch (idx)
            {
                case 0: // Font
                    Point fontPt = this.PointToScreen(new Point(_btnRects[0].Left, _btnRects[0].Bottom));
                    string curF = _ribbon != null ? _ribbon.SelectedFont : "Times New Roman";
                    FontDropDownForm fDlg = new FontDropDownForm(!string.IsNullOrEmpty(curF) ? curF : "Times New Roman", fontPt, _ribbon != null ? _ribbon.AvailableSystemFonts : null, Color.FromArgb(16, 110, 190));
                    fDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    fDlg.OnFontSelected += (font) =>
                    {
                        if (_ribbon != null) _ribbon.SelectedFont = font;
                        _writerView.ApplyFontToSelectionOrTyping(font);
                        _writerView.UpdateRibbonState(_ribbon);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    fDlg.Show();
                    break;

                case 1: // Font Size
                    Point szPt = this.PointToScreen(new Point(_btnRects[1].Left, _btnRects[1].Bottom));
                    float curS = _ribbon != null && _ribbon.FontSize > 0 ? _ribbon.FontSize : 12f;
                    SizeDropDownForm szDlg = new SizeDropDownForm(curS, szPt, Color.FromArgb(16, 110, 190));
                    szDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    szDlg.OnSizeSelected += (sz) =>
                    {
                        if (_ribbon != null) _ribbon.FontSize = sz;
                        _writerView.ApplyFontSizeToSelectionOrTyping(sz);
                        _writerView.UpdateRibbonState(_ribbon);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    szDlg.Show();
                    break;

                case 2: // Increase font (A^)
                    if (_ribbon != null)
                    {
                        _ribbon.FontSize = Math.Min(72, _ribbon.FontSize + 1);
                        _writerView.ApplyFontSizeToSelectionOrTyping(_ribbon.FontSize);
                        _writerView.UpdateRibbonState(_ribbon);
                    }
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 3: // Decrease font (Av)
                    if (_ribbon != null)
                    {
                        _ribbon.FontSize = Math.Max(6, _ribbon.FontSize - 1);
                        _writerView.ApplyFontSizeToSelectionOrTyping(_ribbon.FontSize);
                        _writerView.UpdateRibbonState(_ribbon);
                    }
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 4: // 🧹 Clear Formatting / Format Painter
                    _writerView.ClearFormatting();
                    if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon);
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 5: // Styles ▾ (Visual Style Gallery)
                    Point stPt = this.PointToScreen(new Point(_btnRects[5].Left, _btnRects[5].Bottom));
                    WriterStylesGalleryForm stDlg = new WriterStylesGalleryForm(stPt, _writerView, _ribbon, _parentForm);
                    stDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    stDlg.Show();
                    break;

                case 6: // 💬+ New Comment
                    string comment = CalcContextMenuHelper.PromptInput("Nhập nội dung ghi chú cho đoạn văn bản đã chọn:", "New Comment", "");
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    if (!string.IsNullOrEmpty(comment))
                    {
                        _writerView.ApplyHighlightToSelectionOrTyping(Color.FromArgb(254, 240, 138));
                        MessageBox.Show("Đã thêm ghi chú: \"" + comment + "\"", "New Comment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (_parentForm != null) _parentForm.Invalidate();
                    }
                    break;

                case 7: // B
                    if (_ribbon != null)
                    {
                        _ribbon.IsBold = !_ribbon.IsBold;
                        _writerView.ApplyBoldToSelectionOrTyping(_ribbon.IsBold);
                        _writerView.UpdateRibbonState(_ribbon);
                    }
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 8: // I
                    if (_ribbon != null)
                    {
                        _ribbon.IsItalic = !_ribbon.IsItalic;
                        _writerView.ApplyItalicToSelectionOrTyping(_ribbon.IsItalic);
                        _writerView.UpdateRibbonState(_ribbon);
                    }
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 9: // U
                    if (_ribbon != null)
                    {
                        _ribbon.IsUnderline = !_ribbon.IsUnderline;
                        _writerView.ApplyUnderlineToSelectionOrTyping(_ribbon.IsUnderline);
                        _writerView.UpdateRibbonState(_ribbon);
                    }
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 10: // Highlight ✏️ ▾
                    Point hlPt = this.PointToScreen(new Point(_btnRects[10].Left, _btnRects[10].Bottom));
                    ColorPaletteForm hlDlg = new ColorPaletteForm(hlPt, _ribbon != null ? _ribbon.CurrentHighlightColor : Color.Yellow, true, true);
                    hlDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    hlDlg.OnColorSelected += (c) =>
                    {
                        if (_ribbon != null) _ribbon.CurrentHighlightColor = c;
                        _writerView.ApplyHighlightToSelectionOrTyping(c);
                        if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    hlDlg.Show();
                    break;

                case 11: // Font Color A ▾
                    Point fcPt = this.PointToScreen(new Point(_btnRects[11].Left, _btnRects[11].Bottom));
                    ColorPaletteForm fcDlg = new ColorPaletteForm(fcPt, _ribbon != null ? _ribbon.CurrentFontColor : Color.Black, false, false);
                    fcDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    fcDlg.OnColorSelected += (c) =>
                    {
                        if (_ribbon != null) _ribbon.CurrentFontColor = c;
                        _writerView.ApplyFontColorToSelectionOrTyping(c);
                        if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    fcDlg.Show();
                    break;

                case 12: // Bullets •≡ ▾ (Visual Bullet Library)
                    Point bPt = this.PointToScreen(new Point(_btnRects[12].Left, _btnRects[12].Bottom));
                    WriterBulletsGalleryForm bDlg = new WriterBulletsGalleryForm(bPt, _writerView, _parentForm);
                    bDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    bDlg.Show();
                    break;

                case 13: // Numbering 123≡ ▾ (Visual Numbering Library)
                    Point nPt = this.PointToScreen(new Point(_btnRects[13].Left, _btnRects[13].Bottom));
                    WriterNumberingGalleryForm nDlg = new WriterNumberingGalleryForm(nPt, _writerView, _parentForm);
                    nDlg.FormClosed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    nDlg.Show();
                    break;

                case 14: // Align ≡ ▾
                    ContextMenuStrip aMenu = new ContextMenuStrip();
                    aMenu.Items.Add("Căn trái (Left: Ctrl+L)", null, (s, e) => { _writerView.ApplyAlignmentToSelectionOrParagraph(0); if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Items.Add("Căn giữa (Center: Ctrl+E)", null, (s, e) => { _writerView.ApplyAlignmentToSelectionOrParagraph(1); if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Items.Add("Căn phải (Right: Ctrl+R)", null, (s, e) => { _writerView.ApplyAlignmentToSelectionOrParagraph(2); if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Items.Add("Căn đều 2 bên (Justify: Ctrl+J)", null, (s, e) => { _writerView.ApplyAlignmentToSelectionOrParagraph(3); if (_ribbon != null) _writerView.UpdateRibbonState(_ribbon); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Closed += (s, e) => { if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false; };
                    aMenu.Show(this.PointToScreen(new Point(_btnRects[14].Left, _btnRects[14].Bottom)));
                    break;

                case 15: // Indent
                    _writerView.IncreaseIndent();
                    if (_floatingParentPopup != null) _floatingParentPopup.IsSubMenuOpen = false;
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;
            }
        }
    }

    public class WriterFloatingMiniToolbarPopup : ToolStripDropDown
    {
        private static WriterFloatingMiniToolbarPopup _currentInstance;
        private static Rectangle _screenBounds = Rectangle.Empty;
        private static DateTime _openedTime = DateTime.MinValue;
        private WriterMiniToolbarControl _toolbarControl;

        public bool IsSubMenuOpen { get; set; }

        public static bool IsOpen
        {
            get { return _currentInstance != null && !_currentInstance.IsDisposed && _currentInstance.Visible; }
        }

        public static void ShowMiniToolbar(int screenX, int screenY, WriterView writerView, RibbonBar ribbon, Control parentControl)
        {
            // Floating toolbar on left click is permanently disabled per user request.
            // Mini-toolbar is now exclusively embedded inside the right-click ContextMenu.
            CloseCurrent();
        }

        public static void CloseCurrent()
        {
            if (_currentInstance != null && !_currentInstance.IsDisposed)
            {
                try
                {
                    _currentInstance.Close();
                    _currentInstance.Dispose();
                }
                catch { }
                _currentInstance = null;
                _screenBounds = Rectangle.Empty;
            }
        }

        public static void HideToolbar()
        {
            if (_currentInstance != null && !_currentInstance.IsDisposed && !_currentInstance.IsSubMenuOpen)
            {
                _currentInstance.Visible = false;
            }
        }

        public static void ShowToolbar()
        {
            if (_currentInstance != null && !_currentInstance.IsDisposed)
            {
                _currentInstance.Visible = true;
            }
        }

        public static bool IsMouseOverToolbar(Point screenPt)
        {
            if (_currentInstance != null && !_currentInstance.IsDisposed && _currentInstance.Visible)
            {
                return _screenBounds.Contains(screenPt);
            }
            return false;
        }

        public static bool IsNearToolbar(Point screenPt, int thresholdPx = 140)
        {
            if (_currentInstance != null && !_currentInstance.IsDisposed && _currentInstance.Visible)
            {
                Form mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
                if (mainForm != null && mainForm.IsHandleCreated && !mainForm.IsDisposed)
                {
                    Point clientPt = mainForm.PointToClient(screenPt);
                    if (clientPt.Y <= 158 || clientPt.Y >= mainForm.ClientSize.Height - 26) return false;
                }

                Rectangle expanded = new Rectangle(_screenBounds.X - thresholdPx, _screenBounds.Y - thresholdPx, _screenBounds.Width + thresholdPx * 2, _screenBounds.Height + thresholdPx * 2);
                return expanded.Contains(screenPt);
            }
            return false;
        }

        public static bool CanAutoDismiss()
        {
            return (DateTime.Now - _openedTime).TotalMilliseconds > 350;
        }

        public WriterFloatingMiniToolbarPopup(WriterView writerView, RibbonBar ribbon, Control parentControl)
        {
            this.AutoClose = true;
            this.Margin = Padding.Empty;
            this.Padding = Padding.Empty;
            this.DropShadowEnabled = true;
            this.BackColor = Color.FromArgb(249, 250, 251);

            this.Closed += (s, e) =>
            {
                if (_currentInstance == this)
                {
                    _currentInstance = null;
                    _screenBounds = Rectangle.Empty;
                }
            };

            this.Closing += (s, e) =>
            {
                if (IsSubMenuOpen)
                {
                    e.Cancel = true;
                    return;
                }
            };

            _toolbarControl = new WriterMiniToolbarControl(writerView, ribbon, parentControl as Form, this);
            ToolStripControlHost host = new ToolStripControlHost(_toolbarControl);
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.Size = _toolbarControl.Size;
            this.Items.Add(host);
        }
    }

    public class WriterFloatingMiniToolbarForm
    {
        public static bool IsOpen { get { return WriterFloatingMiniToolbarPopup.IsOpen; } }
        public static void ShowMiniToolbar(int screenX, int screenY, WriterView writerView, RibbonBar ribbon, Form parentForm)
        {
            WriterFloatingMiniToolbarPopup.ShowMiniToolbar(screenX, screenY, writerView, ribbon, parentForm);
        }
        public static void CloseCurrent() { WriterFloatingMiniToolbarPopup.CloseCurrent(); }
        public static void HideToolbar() { WriterFloatingMiniToolbarPopup.HideToolbar(); }
        public static void ShowToolbar() { WriterFloatingMiniToolbarPopup.ShowToolbar(); }
        public static bool IsMouseOverToolbar(Point screenPt) { return WriterFloatingMiniToolbarPopup.IsMouseOverToolbar(screenPt); }
        public static bool IsNearToolbar(Point screenPt, int thresholdPx = 115) { return WriterFloatingMiniToolbarPopup.IsNearToolbar(screenPt, thresholdPx); }
    }

    public static class WriterContextMenuHelper
    {
        public static void Show(int screenX, int screenY, WriterView writerView, RibbonBar ribbon, Form parentForm)
        {
            if (writerView == null) return;

            WriterFloatingMiniToolbarForm.CloseCurrent();

            ContextMenuStrip cms = new ContextMenuStrip();
            cms.ShowImageMargin = true;
            cms.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            cms.Renderer = new ExcelMenuRenderer();

            // 1. Mini-Toolbar Host
            WriterMiniToolbarControl toolbar = new WriterMiniToolbarControl(writerView, ribbon, parentForm, null);
            ToolStripControlHost host = new ToolStripControlHost(toolbar);
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.Size = toolbar.Size;
            cms.Items.Add(host);
            cms.Items.Add(new ToolStripSeparator());

            string selText = writerView.GetSelectedText();

            // 2. Cut (Ctrl+X)
            cms.Items.Add(CreateMenuItem("Cut", CreateCutIcon(), (s, e) =>
            {
                writerView.CutSelection();
                if (parentForm != null) parentForm.Invalidate();
            }));

            // 3. Copy (Ctrl+C)
            cms.Items.Add(CreateMenuItem("Copy", CreateCopyIcon(), (s, e) =>
            {
                writerView.CopySelection();
            }));

            // 4. Paste Options
            ToolStripMenuItem pasteMenu = new ToolStripMenuItem("Paste Options:");
            pasteMenu.Image = CreatePasteIcon();
            pasteMenu.DropDownItems.Add("Keep Source Formatting (K)", CreatePasteIcon(), (s, e) =>
            {
                writerView.PasteText();
                if (parentForm != null) parentForm.Invalidate();
            });
            pasteMenu.DropDownItems.Add("Merge Formatting (M)", CreatePasteIcon(), (s, e) =>
            {
                writerView.PasteText();
                if (parentForm != null) parentForm.Invalidate();
            });
            pasteMenu.DropDownItems.Add("Keep Text Only (T)", CreatePasteTextOnlyIcon(), (s, e) =>
            {
                writerView.PastePlainText();
                if (parentForm != null) parentForm.Invalidate();
            });
            cms.Items.Add(pasteMenu);

            cms.Items.Add(new ToolStripSeparator());

            // 5. Font... (Ctrl+D)
            cms.Items.Add(CreateMenuItem("Font...", CreateFontIcon(), (s, e) =>
            {
                Point fontPt = parentForm != null ? parentForm.PointToScreen(new Point(screenX, screenY)) : new Point(screenX, screenY);
                string curF = ribbon != null ? ribbon.SelectedFont : "Times New Roman";
                FontDropDownForm fDlg = new FontDropDownForm(!string.IsNullOrEmpty(curF) ? curF : "Times New Roman", fontPt, ribbon != null ? ribbon.AvailableSystemFonts : null, Color.FromArgb(16, 110, 190));
                fDlg.OnFontSelected += (font) =>
                {
                    if (ribbon != null) ribbon.SelectedFont = font;
                    writerView.ApplyFontToSelectionOrTyping(font);
                    writerView.UpdateRibbonState(ribbon);
                    if (parentForm != null) parentForm.Invalidate();
                };
                fDlg.Show();
            }));

            // 6. Paragraph...
            cms.Items.Add(CreateMenuItem("Paragraph...", CreateParagraphIcon(), (s, e) =>
            {
                ContextMenuStrip pMenu = new ContextMenuStrip();
                pMenu.Items.Add("Căn trái (Left: Ctrl+L)", null, (ps, pe) => { writerView.ApplyAlignmentToSelectionOrParagraph(0); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add("Căn giữa (Center: Ctrl+E)", null, (ps, pe) => { writerView.ApplyAlignmentToSelectionOrParagraph(1); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add("Căn phải (Right: Ctrl+R)", null, (ps, pe) => { writerView.ApplyAlignmentToSelectionOrParagraph(2); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add("Căn đều 2 bên (Justify: Ctrl+J)", null, (ps, pe) => { writerView.ApplyAlignmentToSelectionOrParagraph(3); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add(new ToolStripSeparator());
                pMenu.Items.Add("Khoảng cách dòng 1.0 (Single)", null, (ps, pe) => { writerView.SetLineSpacing(1.0f); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add("Khoảng cách dòng 1.15 (Standard)", null, (ps, pe) => { writerView.SetLineSpacing(1.15f); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add("Khoảng cách dòng 1.5 (1.5 lines)", null, (ps, pe) => { writerView.SetLineSpacing(1.5f); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Items.Add("Khoảng cách dòng 2.0 (Double)", null, (ps, pe) => { writerView.SetLineSpacing(2.0f); if (parentForm != null) parentForm.Invalidate(); });
                pMenu.Show(parentForm, new Point(screenX, screenY));
            }));

            cms.Items.Add(new ToolStripSeparator());

            // 7. Search "[Selected Text]"
            string searchLabel = !string.IsNullOrEmpty(selText)
                ? ("Search \"" + (selText.Length > 20 ? selText.Substring(0, 18) + "..." : selText) + "\"")
                : "Smart Lookup";
            cms.Items.Add(CreateMenuItem(searchLabel, CreateSearchIcon(), (s, e) =>
            {
                if (!string.IsNullOrEmpty(selText))
                {
                    try { System.Diagnostics.Process.Start("https://www.google.com/search?q=" + Uri.EscapeDataString(selText)); } catch { }
                }
            }));

            // 8. Synonyms >
            ToolStripMenuItem synMenu = new ToolStripMenuItem("Synonyms");
            synMenu.DropDownItems.Add("Tra cứu từ điển (Thesaurus)...", null, (s, e) =>
            {
                if (!string.IsNullOrEmpty(selText))
                {
                    try { System.Diagnostics.Process.Start("https://dict.laban.vn/find?type=1&query=" + Uri.EscapeDataString(selText)); } catch { }
                }
            });
            cms.Items.Add(synMenu);

            // 9. Translate
            cms.Items.Add(CreateMenuItem("Translate", CreateTranslateIcon(), (s, e) =>
            {
                if (!string.IsNullOrEmpty(selText))
                {
                    try { System.Diagnostics.Process.Start("https://translate.google.com/?sl=auto&tl=vi&text=" + Uri.EscapeDataString(selText) + "&op=translate"); } catch { }
                }
            }));

            // 10. Link
            cms.Items.Add(CreateMenuItem("Link", CreateLinkIcon(), (s, e) =>
            {
                string url = CalcContextMenuHelper.PromptInput("Nhập địa chỉ liên kết (URL):", "Insert Hyperlink", "https://");
                if (!string.IsNullOrEmpty(url))
                {
                    writerView.InsertHyperlink(url);
                    if (parentForm != null) parentForm.Invalidate();
                }
            }));

            cms.Show(parentForm, new Point(screenX, screenY));
        }

        private static ToolStripMenuItem CreateMenuItem(string text, Image icon, EventHandler onClick)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text, icon, onClick);
            return item;
        }

        #region Vector Icon Generators
        private static Image CreateCutIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 1.3f))
                {
                    g.DrawEllipse(p, 2, 2, 4, 4);
                    g.DrawEllipse(p, 2, 10, 4, 4);
                    g.DrawLine(p, 5, 4, 13, 12);
                    g.DrawLine(p, 5, 12, 13, 4);
                }
            }
            return bmp;
        }

        private static Image CreateCopyIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(75, 85, 99), 1.2f))
                {
                    g.DrawRectangle(p, 4, 2, 9, 10);
                    g.DrawRectangle(p, 2, 4, 9, 10);
                }
            }
            return bmp;
        }

        private static Image CreatePasteIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(217, 119, 6), 1.2f))
                {
                    g.DrawRectangle(p, 3, 4, 10, 11);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillRectangle(b, 5, 2, 6, 3);
                    using (SolidBrush wb = new SolidBrush(Color.FromArgb(59, 130, 246))) g.FillRectangle(wb, 5, 7, 6, 5);
                }
            }
            return bmp;
        }

        private static Image CreatePasteTextOnlyIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(217, 119, 6), 1.2f))
                {
                    g.DrawRectangle(p, 3, 4, 10, 11);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillRectangle(b, 5, 2, 6, 3);
                    using (Font f = new Font("Segoe UI", 6f, FontStyle.Bold))
                    {
                        g.DrawString("A", f, Brushes.Black, 4, 6);
                    }
                }
            }
            return bmp;
        }

        private static Image CreateFontIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                {
                    g.DrawString("A", f, Brushes.DarkSlateGray, 2, 1);
                }
            }
            return bmp;
        }

        private static Image CreateParagraphIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(75, 85, 99), 1.3f))
                {
                    g.DrawLine(p, 2, 4, 14, 4);
                    g.DrawLine(p, 5, 8, 14, 8);
                    g.DrawLine(p, 2, 12, 14, 12);
                }
            }
            return bmp;
        }

        private static Image CreateSearchIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(2, 132, 199), 1.4f))
                {
                    g.DrawEllipse(p, 2, 2, 8, 8);
                    g.DrawLine(p, 8, 8, 14, 14);
                }
            }
            return bmp;
        }

        private static Image CreateTranslateIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Font f = new Font("Segoe UI", 6.5f, FontStyle.Bold))
                {
                    g.DrawString("a", f, Brushes.Teal, 1, 1);
                    g.DrawString("あ", f, Brushes.Navy, 6, 6);
                }
            }
            return bmp;
        }

        private static Image CreateLinkIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(59, 130, 246), 1.4f))
                {
                    g.DrawArc(p, 2, 6, 6, 6, 90, 180);
                    g.DrawArc(p, 8, 4, 6, 6, 270, 180);
                    g.DrawLine(p, 5, 8, 11, 8);
                }
            }
            return bmp;
        }
        #endregion
    }
}

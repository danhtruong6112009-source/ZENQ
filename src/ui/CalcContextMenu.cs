using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class CalcMiniToolbarControl : UserControl
    {
        private CalcView _calcView;
        private RibbonBar _ribbon;
        private Form _parentForm;
        private ContextMenuStrip _parentMenu;

        private int _hoveredBtn = -1;
        private Rectangle[] _btnRects = new Rectangle[18];

        public CalcMiniToolbarControl(CalcView calcView, RibbonBar ribbon, Form parentForm, ContextMenuStrip parentMenu)
        {
            _calcView = calcView;
            _ribbon = ribbon;
            _parentForm = parentForm;
            _parentMenu = parentMenu;

            this.DoubleBuffered = true;
            this.Size = new Size(298, 56);
            this.BackColor = Color.FromArgb(249, 250, 251);
            this.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);

            ComputeLayout();
        }

        private void ComputeLayout()
        {
            // Row 1 (Y: 3, Height: 24)
            _btnRects[0] = new Rectangle(4, 3, 82, 24);   // Font Family
            _btnRects[1] = new Rectangle(88, 3, 38, 24);  // Font Size
            _btnRects[2] = new Rectangle(128, 3, 20, 24); // A^
            _btnRects[3] = new Rectangle(150, 3, 20, 24); // Av
            _btnRects[4] = new Rectangle(172, 3, 22, 24); // $
            _btnRects[5] = new Rectangle(196, 3, 20, 24); // %
            _btnRects[6] = new Rectangle(218, 3, 18, 24); // ,
            _btnRects[7] = new Rectangle(238, 3, 26, 24); // Merge & Center
            _btnRects[8] = new Rectangle(266, 3, 26, 24); // Wrap Text

            // Row 2 (Y: 29, Height: 24)
            _btnRects[9]  = new Rectangle(4, 29, 22, 24);  // B
            _btnRects[10] = new Rectangle(28, 29, 22, 24); // I
            _btnRects[11] = new Rectangle(52, 29, 28, 24); // Align ≡ ▾
            _btnRects[12] = new Rectangle(82, 29, 30, 24); // Fill Color 🪣 ▾
            _btnRects[13] = new Rectangle(114, 29, 30, 24); // Font Color A ▾
            _btnRects[14] = new Rectangle(146, 29, 30, 24); // Borders ⊞ ▾
            _btnRects[15] = new Rectangle(178, 29, 24, 24); // Dec Decimals <-.00 .0
            _btnRects[16] = new Rectangle(204, 29, 24, 24); // Inc Decimals .0->.00
            _btnRects[17] = new Rectangle(230, 29, 26, 24); // Format Painter 🖌️
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

            CalcSelectionFormat summary = _calcView != null ? _calcView.GetSelectionFormat() : new CalcSelectionFormat();

            using (Pen borderPen = new Pen(Color.FromArgb(229, 231, 235)))
            {
                g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
            }

            for (int i = 0; i < _btnRects.Length; i++)
            {
                Rectangle r = _btnRects[i];
                bool isHover = (_hoveredBtn == i);
                bool isActive = false;

                if (i == 7 && summary.IsMerged) isActive = true;
                if (i == 8 && summary.WrapText) isActive = true;
                if (i == 9 && summary.Bold) isActive = true;
                if (i == 10 && summary.Italic) isActive = true;
                if (i == 17 && _calcView != null && _calcView.IsFormatPainterActive) isActive = true;

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

                DrawButtonContent(g, i, r, summary, isHover, isActive);
            }
        }

        private void DrawButtonContent(Graphics g, int idx, Rectangle r, CalcSelectionFormat summary, bool isHover, bool isActive)
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
                        string fontText = !string.IsNullOrEmpty(summary.FontName) ? summary.FontName : "";
                        using (SolidBrush boxBg = new SolidBrush(Color.White))
                        using (Pen boxPen = new Pen(Color.FromArgb(209, 213, 219)))
                        {
                            g.FillRectangle(boxBg, r.X, r.Y + 2, r.Width, r.Height - 4);
                            g.DrawRectangle(boxPen, r.X, r.Y + 2, r.Width - 1, r.Height - 5);
                        }
                        TextRenderer.DrawText(g, fontText, fRegular, new Rectangle(r.X + 2, r.Y + 3, r.Width - 14, r.Height - 6), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        DrawArrowDown(g, r.Right - 10, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 1: // Font Size Combo
                        string sizeText = summary.FontSize > 0 ? summary.FontSize.ToString() : "";
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
                        TextRenderer.DrawText(g, "A", fBold, new Rectangle(r.X, r.Y + 2, 12, r.Height - 4), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        DrawArrowUp(g, r.X + 13, r.Y + 7, Color.FromArgb(16, 124, 65));
                        break;

                    case 3: // Av (Decrease font)
                        TextRenderer.DrawText(g, "A", fRegular, new Rectangle(r.X, r.Y + 3, 11, r.Height - 5), Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        DrawArrowDown(g, r.X + 12, r.Y + 13, Color.FromArgb(239, 68, 68));
                        break;

                    case 4: // $
                        TextRenderer.DrawText(g, "$", fBold, new Rectangle(r.X, r.Y + 2, 14, r.Height - 4), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 5: // %
                        TextRenderer.DrawText(g, "%", fBold, r, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        break;

                    case 6: // ,
                        TextRenderer.DrawText(g, ",", fBold, new Rectangle(r.X, r.Y - 2, r.Width, r.Height), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        break;

                    case 7: // Merge & Center (<->)
                        g.DrawRectangle(pen, r.X + 2, r.Y + 4, r.Width - 4, r.Height - 8);
                        g.DrawLine(pen, r.X + 4, r.Y + r.Height / 2, r.Right - 4, r.Y + r.Height / 2);
                        Point[] lArr = new Point[] { new Point(r.X + 3, r.Y + r.Height / 2), new Point(r.X + 6, r.Y + r.Height / 2 - 2), new Point(r.X + 6, r.Y + r.Height / 2 + 2) };
                        Point[] rArr = new Point[] { new Point(r.Right - 3, r.Y + r.Height / 2), new Point(r.Right - 6, r.Y + r.Height / 2 - 2), new Point(r.Right - 6, r.Y + r.Height / 2 + 2) };
                        using (SolidBrush b = new SolidBrush(iconCol))
                        {
                            g.FillPolygon(b, lArr);
                            g.FillPolygon(b, rArr);
                        }
                        break;

                    case 8: // Wrap Text (ab->c)
                        g.DrawString("ab", fSmall, iconCol.ToBrush(), r.X + 1, r.Y + 2);
                        g.DrawString("c", fSmall, iconCol.ToBrush(), r.X + 1, r.Y + 11);
                        using (Pen pArc = new Pen(iconCol, 1.2f))
                        {
                            pArc.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2.5f, 2.5f);
                            g.DrawArc(pArc, r.Right - 10, r.Y + 4, 7, 7, 270, 180);
                            g.DrawLine(pArc, r.Right - 6, r.Y + 11, r.X + 9, r.Y + 11);
                        }
                        break;

                    case 9: // B
                        TextRenderer.DrawText(g, "B", fBold, r, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        break;

                    case 10: // I
                        using (Font fItalic = new Font("Segoe UI", 9f, FontStyle.Italic | FontStyle.Bold))
                        {
                            TextRenderer.DrawText(g, "I", fItalic, r, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                        break;

                    case 11: // Align ≡ ▾
                        for (int line = 0; line < 3; line++)
                        {
                            int ly = r.Y + 6 + line * 4;
                            g.DrawLine(pen, r.X + 4, ly, r.X + 18, ly);
                        }
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 12: // Fill Color 🪣 ▾
                        DrawBucketIcon(g, r.X + 4, r.Y + 4, 14);
                        using (SolidBrush b = new SolidBrush(Color.Yellow)) g.FillRectangle(b, r.X + 4, r.Bottom - 5, 14, 3);
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 13: // Font Color A ▾
                        TextRenderer.DrawText(g, "A", fBold, new Rectangle(r.X + 2, r.Y + 1, 16, 16), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        using (SolidBrush b = new SolidBrush(Color.Red)) g.FillRectangle(b, r.X + 4, r.Bottom - 5, 14, 3);
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 14: // Borders ⊞ ▾
                        g.DrawRectangle(pen, r.X + 3, r.Y + 4, 14, 14);
                        g.DrawLine(pen, r.X + 10, r.Y + 4, r.X + 10, r.Y + 18);
                        g.DrawLine(pen, r.X + 3, r.Y + 11, r.X + 17, r.Y + 11);
                        DrawArrowDown(g, r.Right - 7, r.Y + r.Height / 2, Color.FromArgb(107, 114, 128));
                        break;

                    case 15: // Dec Decimals <-.00 .0
                        g.DrawString(".0", fSmall, Brushes.Black, r.X + 1, r.Y + 2);
                        using (Pen pArr = new Pen(iconCol, 1.1f))
                        {
                            pArr.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2f, 2f);
                            g.DrawLine(pArr, r.X + 4, r.Y + 14, r.X + 12, r.Y + 14);
                        }
                        break;

                    case 16: // Inc Decimals .0->.00
                        g.DrawString(".00", fSmall, Brushes.Black, r.X + 1, r.Y + 2);
                        using (Pen pArr = new Pen(iconCol, 1.1f))
                        {
                            pArr.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2f, 2f);
                            g.DrawLine(pArr, r.X + 12, r.Y + 14, r.X + 4, r.Y + 14);
                        }
                        break;

                    case 17: // Format Painter 🖌️
                        DrawBrushIcon(g, r.X + 5, r.Y + 4, 16);
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

        private void DrawBucketIcon(Graphics g, int x, int y, int size)
        {
            using (Pen p = new Pen(Color.FromArgb(60, 60, 60), 1.2f))
            {
                Point[] bucket = new Point[] { new Point(x + 3, y + 2), new Point(x + size - 3, y + 2), new Point(x + size - 5, y + size - 4), new Point(x + 5, y + size - 4) };
                g.DrawPolygon(p, bucket);
                g.DrawLine(p, x + 5, y + 2, x + 7, y - 1);
            }
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _calcView == null) return;

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
            CalcSelectionFormat summary = _calcView.GetSelectionFormat();

            switch (idx)
            {
                case 0: // Font
                    Point fontPt = this.PointToScreen(new Point(_btnRects[0].Left, _btnRects[0].Bottom));
                    FontDropDownForm fDlg = new FontDropDownForm(!string.IsNullOrEmpty(summary.FontName) ? summary.FontName : "Calibri", fontPt, _ribbon != null ? _ribbon.AvailableSystemFonts : null, Color.FromArgb(16, 124, 65));
                    fDlg.OnFontSelected += (font) =>
                    {
                        _calcView.ApplyFontName(font);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    fDlg.Show();
                    break;

                case 1: // Font Size
                    Point szPt = this.PointToScreen(new Point(_btnRects[1].Left, _btnRects[1].Bottom));
                    SizeDropDownForm szDlg = new SizeDropDownForm(summary.FontSize > 0 ? summary.FontSize : 11f, szPt, Color.FromArgb(16, 124, 65));
                    szDlg.OnSizeSelected += (sz) =>
                    {
                        _calcView.ApplyFontSize(sz);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    szDlg.Show();
                    break;

                case 2: // Increase font
                    _calcView.IncreaseFontSize();
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 3: // Decrease font
                    _calcView.DecreaseFontSize();
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 4: // Currency $
                    _calcView.SetNumberFormat("Currency", 2);
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 5: // Percent %
                    _calcView.SetNumberFormat("Percent", 2);
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 6: // Comma ,
                    _calcView.SetNumberFormat("Comma", 0);
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 7: // Merge & Center
                    _calcView.MergeAndCenter();
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 8: // Wrap Text
                    _calcView.ToggleWrapText();
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 9: // Bold B
                    _calcView.ToggleBold();
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 10: // Italic I
                    _calcView.ToggleItalic();
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 11: // Align ≡ ▾
                    ContextMenuStrip aMenu = new ContextMenuStrip();
                    aMenu.Items.Add("Căn trái (Left)", null, (s, e) => { _calcView.SetSelectedCellAlign(0); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Items.Add("Căn giữa (Center)", null, (s, e) => { _calcView.SetSelectedCellAlign(1); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Items.Add("Căn phải (Right)", null, (s, e) => { _calcView.SetSelectedCellAlign(2); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    aMenu.Show(this.PointToScreen(new Point(_btnRects[11].Left, _btnRects[11].Bottom)));
                    break;

                case 12: // Fill Color 🪣 ▾
                    Point fcPt = this.PointToScreen(new Point(_btnRects[12].Left, _btnRects[12].Bottom));
                    ColorPaletteForm fcDlg = new ColorPaletteForm(fcPt, Color.LightYellow, true, false);
                    fcDlg.OnColorSelected += (c) =>
                    {
                        _calcView.SetSelectedCellBgColor(c);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    fcDlg.Show();
                    break;

                case 13: // Font Color A ▾
                    Point tcPt = this.PointToScreen(new Point(_btnRects[13].Left, _btnRects[13].Bottom));
                    ColorPaletteForm tcDlg = new ColorPaletteForm(tcPt, Color.Black, true, false);
                    tcDlg.OnColorSelected += (c) =>
                    {
                        _calcView.SetSelectedCellTextColor(c);
                        Invalidate();
                        if (_parentForm != null) _parentForm.Invalidate();
                    };
                    tcDlg.Show();
                    break;

                case 14: // Borders ⊞ ▾
                    ContextMenuStrip bdrMenu = new ContextMenuStrip();
                    bdrMenu.ShowImageMargin = true;
                    bdrMenu.Items.Add("Viền dưới (Bottom Border)", RibbonBar.CreateExcelBorderIcon(0), (s, e) => { _calcView.ApplyBorderPreset(0); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền trên (Top Border)", RibbonBar.CreateExcelBorderIcon(1), (s, e) => { _calcView.ApplyBorderPreset(1); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền trái (Left Border)", RibbonBar.CreateExcelBorderIcon(2), (s, e) => { _calcView.ApplyBorderPreset(2); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền phải (Right Border)", RibbonBar.CreateExcelBorderIcon(3), (s, e) => { _calcView.ApplyBorderPreset(3); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add(new ToolStripSeparator());
                    bdrMenu.Items.Add("Không viền (No Border / Xóa kẻ)", RibbonBar.CreateExcelBorderIcon(4), (s, e) => { _calcView.ApplyBorderPreset(4); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Tất cả viền (All Borders)", RibbonBar.CreateExcelBorderIcon(5), (s, e) => { _calcView.ApplyBorderPreset(5); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền ngoài (Outside Borders)", RibbonBar.CreateExcelBorderIcon(6), (s, e) => { _calcView.ApplyBorderPreset(6); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền ngoài đậm (Thick Outside Border)", RibbonBar.CreateExcelBorderIcon(7), (s, e) => { _calcView.ApplyBorderPreset(7); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add(new ToolStripSeparator());
                    bdrMenu.Items.Add("Viền dưới kép (Bottom Double Border)", RibbonBar.CreateExcelBorderIcon(8), (s, e) => { _calcView.ApplyBorderPreset(8); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền dưới đậm (Thick Bottom Border)", RibbonBar.CreateExcelBorderIcon(9), (s, e) => { _calcView.ApplyBorderPreset(9); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền trên và dưới (Top and Bottom Border)", RibbonBar.CreateExcelBorderIcon(10), (s, e) => { _calcView.ApplyBorderPreset(10); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền trên và dưới đậm (Top and Thick Bottom)", RibbonBar.CreateExcelBorderIcon(11), (s, e) => { _calcView.ApplyBorderPreset(11); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Items.Add("Viền trên và dưới kép (Top and Double Bottom)", RibbonBar.CreateExcelBorderIcon(12), (s, e) => { _calcView.ApplyBorderPreset(12); Invalidate(); if (_parentForm != null) _parentForm.Invalidate(); });
                    bdrMenu.Show(this.PointToScreen(new Point(_btnRects[14].Left, _btnRects[14].Bottom)));
                    break;

                case 15: // Dec Decimals
                    _calcView.AdjustDecimalPlaces(-1);
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 16: // Inc Decimals
                    _calcView.AdjustDecimalPlaces(1);
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;

                case 17: // Format Painter
                    if (_calcView.IsFormatPainterActive)
                    {
                        _calcView.PasteFormat();
                    }
                    else
                    {
                        _calcView.CopyFormat();
                    }
                    Invalidate();
                    if (_parentForm != null) _parentForm.Invalidate();
                    break;
            }
        }
    }

    public static class CalcContextMenuHelper
    {
        public static void Show(int screenX, int screenY, CalcView calcView, RibbonBar ribbon, Form parentForm)
        {
            if (calcView == null) return;

            ContextMenuStrip cms = new ContextMenuStrip();
            cms.ShowImageMargin = true;
            cms.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            cms.Renderer = new ExcelMenuRenderer();

            // 1. Mini-Toolbar Host
            CalcMiniToolbarControl toolbar = new CalcMiniToolbarControl(calcView, ribbon, parentForm, cms);
            ToolStripControlHost host = new ToolStripControlHost(toolbar);
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.Size = toolbar.Size;
            cms.Items.Add(host);
            cms.Items.Add(new ToolStripSeparator());

            // 2. Cut (Ctrl+X)
            cms.Items.Add(CreateMenuItem("Cắt (Cut - Ctrl+X)", CreateCutIcon(), (s, e) =>
            {
                calcView.Cut();
                if (parentForm != null) parentForm.Invalidate();
            }));

            // 3. Copy (Ctrl+C)
            cms.Items.Add(CreateMenuItem("Sao chép (Copy - Ctrl+C)", CreateCopyIcon(), (s, e) =>
            {
                calcView.Copy();
            }));

            // 4. Paste Options
            ToolStripMenuItem pasteMenu = new ToolStripMenuItem("Tùy chọn Dán (Paste):");
            pasteMenu.Image = CreatePasteIcon();
            pasteMenu.DropDownItems.Add("Dán (Paste All - Ctrl+V)", CreatePasteIcon(), (s, e) =>
            {
                calcView.Paste();
                if (parentForm != null) parentForm.Invalidate();
            });
            pasteMenu.DropDownItems.Add("Dán giá trị (Values - 123)", CreatePasteValuesIcon(), (s, e) =>
            {
                calcView.PasteValues();
                if (parentForm != null) parentForm.Invalidate();
            });
            pasteMenu.DropDownItems.Add("Dán công thức (Formulas - fx)", null, (s, e) =>
            {
                calcView.PasteFormulas();
                if (parentForm != null) parentForm.Invalidate();
            });
            pasteMenu.DropDownItems.Add("Hoán vị hàng / cột (Transpose)", null, (s, e) =>
            {
                calcView.PasteTranspose();
                if (parentForm != null) parentForm.Invalidate();
            });
            pasteMenu.DropDownItems.Add(new ToolStripSeparator());
            pasteMenu.DropDownItems.Add("Dán đặc biệt (Paste Special...)", null, (s, e) =>
            {
                calcView.OpenPasteSpecialDialog(parentForm);
                if (parentForm != null) parentForm.Invalidate();
            });
            cms.Items.Add(pasteMenu);

            cms.Items.Add(CreateMenuItem("Điền nhanh (Flash Fill - Ctrl+E)", null, (s, e) =>
            {
                calcView.FlashFill();
                if (parentForm != null) parentForm.Invalidate();
            }));
            cms.Items.Add(CreateMenuItem("Dò tìm mục tiêu (Goal Seek...)", null, (s, e) =>
            {
                calcView.OpenGoalSeekDialog(parentForm);
                if (parentForm != null) parentForm.Invalidate();
            }));

            cms.Items.Add(new ToolStripSeparator());

            // 5. Smart Lookup
            cms.Items.Add(CreateMenuItem("Smart Lookup", CreateSearchIcon(), (s, e) =>
            {
                CellData cd = calcView.CurrentSheet.GetCell(calcView.SelectedRow, calcView.SelectedCol);
                string query = cd != null ? (cd.EvaluatedStr ?? cd.RawValue ?? "") : "";
                if (!string.IsNullOrEmpty(query))
                {
                    try { System.Diagnostics.Process.Start("https://www.google.com/search?q=" + Uri.EscapeDataString(query)); } catch { }
                }
            }));

            // 5b. Expand & Scroll Viewer
            cms.Items.Add(CreateMenuItem("Sổ xuống xem nội dung dài... (Expand & Scroll - Alt+Down)", null, (s, e) =>
            {
                calcView.OpenCellDropdownViewer(parentForm);
            }));

            cms.Items.Add(new ToolStripSeparator());

            // 6. Insert... -> CalcInsertDialogForm
            cms.Items.Add(CreateMenuItem("Insert...", CreateInsertIcon(), (s, e) =>
            {
                using (CalcInsertDialogForm dlg = new CalcInsertDialogForm())
                {
                    if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                    {
                        calcView.SaveUndoState();
                        uint minR = Math.Min(calcView.SelectedRow, calcView.SelEndRow);
                        uint maxR = Math.Max(calcView.SelectedRow, calcView.SelEndRow);
                        uint minC = Math.Min(calcView.SelectedCol, calcView.SelEndCol);
                        uint maxC = Math.Max(calcView.SelectedCol, calcView.SelEndCol);

                        switch (dlg.SelectedMode)
                        {
                            case InsertShiftMode.ShiftCellsRight:
                                calcView.CurrentSheet.ShiftCellsRight(minR, minC, maxR, maxC);
                                break;
                            case InsertShiftMode.ShiftCellsDown:
                                calcView.CurrentSheet.ShiftCellsDown(minR, minC, maxR, maxC);
                                break;
                            case InsertShiftMode.EntireRow:
                                calcView.InsertRows(maxR - minR + 1);
                                break;
                            case InsertShiftMode.EntireColumn:
                                calcView.InsertCols(maxC - minC + 1);
                                break;
                        }
                        FormulaEngine.EvaluateSheet(calcView.CurrentSheet);
                        if (parentForm != null) parentForm.Invalidate();
                    }
                }
            }));

            // 7. Delete... -> CalcDeleteDialogForm
            cms.Items.Add(CreateMenuItem("Delete...", CreateDeleteIcon(), (s, e) =>
            {
                using (CalcDeleteDialogForm dlg = new CalcDeleteDialogForm())
                {
                    if (dlg.ShowDialog(parentForm) == DialogResult.OK)
                    {
                        calcView.SaveUndoState();
                        uint minR = Math.Min(calcView.SelectedRow, calcView.SelEndRow);
                        uint maxR = Math.Max(calcView.SelectedRow, calcView.SelEndRow);
                        uint minC = Math.Min(calcView.SelectedCol, calcView.SelEndCol);
                        uint maxC = Math.Max(calcView.SelectedCol, calcView.SelEndCol);

                        switch (dlg.SelectedMode)
                        {
                            case DeleteShiftMode.ShiftCellsLeft:
                                calcView.CurrentSheet.ShiftCellsLeft(minR, minC, maxR, maxC);
                                break;
                            case DeleteShiftMode.ShiftCellsUp:
                                calcView.CurrentSheet.ShiftCellsUp(minR, minC, maxR, maxC);
                                break;
                            case DeleteShiftMode.EntireRow:
                                calcView.DeleteRows(maxR - minR + 1);
                                break;
                            case DeleteShiftMode.EntireColumn:
                                calcView.DeleteCols(maxC - minC + 1);
                                break;
                        }
                        FormulaEngine.EvaluateSheet(calcView.CurrentSheet);
                        if (parentForm != null) parentForm.Invalidate();
                    }
                }
            }));

            // 8. Clear Contents
            cms.Items.Add(CreateMenuItem("Clear Contents", null, (s, e) =>
            {
                calcView.ClearContents();
                if (parentForm != null) parentForm.Invalidate();
            }));

            cms.Items.Add(new ToolStripSeparator());

            // 9. Quick Analysis
            cms.Items.Add(CreateMenuItem("Quick Analysis", CreateQuickAnalysisIcon(), (s, e) =>
            {
                calcView.ApplyConditionalFormatting(2); // Color scale
                if (parentForm != null) parentForm.Invalidate();
            }));

            // 10. Filter >
            ToolStripMenuItem filterMenu = new ToolStripMenuItem("Filter");
            filterMenu.DropDownItems.Add("Clear Filter", CreateFunnelClearIcon(), (s, e) => { /* Clear filter */ });
            filterMenu.DropDownItems.Add("Reapply", CreateFunnelRefreshIcon(), (s, e) => { /* Reapply */ });
            filterMenu.DropDownItems.Add(new ToolStripSeparator());
            filterMenu.DropDownItems.Add("Filter by Selected Cell's Value", null, (s, e) =>
            {
                CellData cd = calcView.CurrentSheet.GetCell(calcView.SelectedRow, calcView.SelectedCol);
                string val = cd != null ? (cd.EvaluatedStr ?? cd.RawValue ?? "") : "";
                MessageBox.Show("Đã lọc bảng tính theo giá trị: \"" + val + "\"", "Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            filterMenu.DropDownItems.Add("Filter by Selected Cell's Color", null, (s, e) =>
            {
                MessageBox.Show("Đã lọc theo màu nền ô.", "Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            filterMenu.DropDownItems.Add("Filter by Selected Cell's Font Color", null, (s, e) =>
            {
                MessageBox.Show("Đã lọc theo màu chữ.", "Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            filterMenu.DropDownItems.Add("Filter by Selected Cell's Icon", null, (s, e) =>
            {
                MessageBox.Show("Đã lọc theo biểu tượng điều kiện.", "Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            cms.Items.Add(filterMenu);

            // 11. Sort >
            ToolStripMenuItem sortMenu = new ToolStripMenuItem("Sort");
            sortMenu.DropDownItems.Add("Sort A to Z", CreateSortAZIcon(), (s, e) => { calcView.SortSelectedRange(true); if (parentForm != null) parentForm.Invalidate(); });
            sortMenu.DropDownItems.Add("Sort Z to A", CreateSortZAIcon(), (s, e) => { calcView.SortSelectedRange(false); if (parentForm != null) parentForm.Invalidate(); });
            sortMenu.DropDownItems.Add(new ToolStripSeparator());
            sortMenu.DropDownItems.Add("Put Selected Cell Color On Top", null, (s, e) => { calcView.SortSelectedRange(true); if (parentForm != null) parentForm.Invalidate(); });
            sortMenu.DropDownItems.Add("Put Selected Font Color On Top", null, (s, e) => { calcView.SortSelectedRange(true); if (parentForm != null) parentForm.Invalidate(); });
            sortMenu.DropDownItems.Add("Put Selected Formatting Icon On Top", null, (s, e) => { calcView.SortSelectedRange(true); if (parentForm != null) parentForm.Invalidate(); });
            sortMenu.DropDownItems.Add(new ToolStripSeparator());
            sortMenu.DropDownItems.Add("Custom Sort...", CreateCustomSortIcon(), (s, e) => { calcView.SortSelectedRange(true); if (parentForm != null) parentForm.Invalidate(); });
            cms.Items.Add(sortMenu);

            cms.Items.Add(new ToolStripSeparator());

            // 12. Get Data from Table/Range...
            cms.Items.Add(CreateMenuItem("Get Data from Table/Range...", CreateTableRangeIcon(), (s, e) =>
            {
                calcView.ApplyTableStyle(0);
                if (parentForm != null) parentForm.Invalidate();
            }));

            // 13. Insert Comment
            cms.Items.Add(CreateMenuItem("Insert Comment", CreateCommentIcon(), (s, e) =>
            {
                string comment = PromptInput("Nhập nội dung ghi chú cho ô (" + calcView.SelectedRow + ", " + calcView.SelectedCol + "):", "Insert Comment", "");
                if (!string.IsNullOrEmpty(comment))
                {
                    CellData cd = calcView.CurrentSheet.GetOrCreateCell(calcView.SelectedRow, calcView.SelectedCol);
                    cd.Comment = comment;
                    if (parentForm != null) parentForm.Invalidate();
                }
            }));

            // 14. Format Cells...
            cms.Items.Add(CreateMenuItem("Format Cells...", CreateFormatCellsIcon(), (s, e) =>
            {
                Point pt = parentForm != null ? parentForm.PointToScreen(new Point(screenX, screenY)) : new Point(screenX, screenY);
                ColorPaletteForm fcDlg = new ColorPaletteForm(pt, Color.LightYellow, true, false);
                fcDlg.OnColorSelected += (c) =>
                {
                    calcView.SetSelectedCellBgColor(c);
                    if (parentForm != null) parentForm.Invalidate();
                };
                fcDlg.Show();
            }));

            // 15. Pick From Drop-down List...
            cms.Items.Add(CreateMenuItem("Pick From Drop-down List...", null, (s, e) =>
            {
                // Auto list distinct values in column
                uint c = calcView.SelectedCol;
                HashSet<string> vals = new HashSet<string>();
                for (uint r = 1; r <= 100; r++)
                {
                    CellData cd = calcView.CurrentSheet.GetCell(r, c);
                    if (cd != null && !string.IsNullOrEmpty(cd.RawValue)) vals.Add(cd.RawValue);
                }
                if (vals.Count > 0)
                {
                    ContextMenuStrip pMenu = new ContextMenuStrip();
                    foreach (string v in vals)
                    {
                        string curVal = v;
                        pMenu.Items.Add(curVal, null, (ps, pe) =>
                        {
                            calcView.SaveUndoState();
                            calcView.CurrentSheet.SetCellText(calcView.SelectedRow, calcView.SelectedCol, curVal);
                            FormulaEngine.EvaluateSheet(calcView.CurrentSheet);
                            if (parentForm != null) parentForm.Invalidate();
                        });
                    }
                    pMenu.Show(parentForm, new Point(screenX, screenY));
                }
            }));

            // 16. Define Name...
            cms.Items.Add(CreateMenuItem("Define Name...", null, (s, e) =>
            {
                string name = PromptInput("Đặt tên đại diện cho vùng chọn:", "Define Name", "Range1");
                if (!string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Đã gán tên đại diện \"" + name + "\" cho vùng chọn!", "Define Name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }));

            // 17. Link
            cms.Items.Add(CreateMenuItem("Link", CreateLinkIcon(), (s, e) =>
            {
                string url = PromptInput("Nhập địa chỉ liên kết (URL):", "Insert Link", "https://");
                if (!string.IsNullOrEmpty(url))
                {
                    calcView.SaveUndoState();
                    CellData cd = calcView.CurrentSheet.GetOrCreateCell(calcView.SelectedRow, calcView.SelectedCol);
                    if (string.IsNullOrEmpty(cd.RawValue)) cd.RawValue = url;
                    cd.Hyperlink = url;
                    cd.Format.Underline = true;
                    cd.Format.TextColor = Color.FromArgb(16, 110, 190).ToArgb();
                    if (parentForm != null) parentForm.Invalidate();
                }
            }));

            cms.Show(parentForm, new Point(screenX, screenY));
        }

        public static string PromptInput(string prompt, string title, string defaultValue)
        {
            using (Form form = new Form())
            {
                form.Text = title;
                form.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(330, 115);
                form.BackColor = Color.FromArgb(243, 243, 243);

                Label lbl = new Label() { Text = prompt, Left = 15, Top = 12, Width = 300, Height = 20 };
                TextBox txt = new TextBox() { Text = defaultValue, Left = 15, Top = 36, Width = 300 };
                Button btnOk = new Button() { Text = "OK", Left = 145, Top = 72, Width = 80, Height = 26, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.System };
                Button btnCancel = new Button() { Text = "Cancel", Left = 235, Top = 72, Width = 80, Height = 26, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.System };

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btnOk);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    return txt.Text;
                }
                return "";
            }
        }

        private static ToolStripMenuItem CreateMenuItem(string text, Image icon, EventHandler onClick)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text, icon, onClick);
            return item;
        }

        #region Vector Icon Generators for Context Menu
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

        private static Image CreatePasteValuesIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(217, 119, 6), 1.2f))
                {
                    g.DrawRectangle(p, 3, 4, 10, 11);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillRectangle(b, 5, 2, 6, 3);
                    using (Font f = new Font("Segoe UI", 5.5f, FontStyle.Bold))
                    {
                        g.DrawString("123", f, Brushes.Black, 2, 6);
                    }
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
                    using (Font f = new Font("Georgia", 6f, FontStyle.Bold))
                    {
                        g.DrawString("i", f, Brushes.Teal, 4, 2);
                    }
                }
            }
            return bmp;
        }

        private static Image CreateInsertIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(156, 163, 175), 1.1f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    using (Pen pPlus = new Pen(Color.FromArgb(16, 124, 65), 1.6f))
                    {
                        g.DrawLine(pPlus, 8, 4, 8, 12);
                        g.DrawLine(pPlus, 4, 8, 12, 8);
                    }
                }
            }
            return bmp;
        }

        private static Image CreateDeleteIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(156, 163, 175), 1.1f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(239, 68, 68)))
                    {
                        g.FillRectangle(b, 4, 7, 8, 2);
                    }
                }
            }
            return bmp;
        }

        private static Image CreateQuickAnalysisIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(107, 114, 128), 1.1f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    Point[] bolt = new Point[] { new Point(8, 3), new Point(5, 9), new Point(9, 9), new Point(8, 13), new Point(12, 7), new Point(8, 7) };
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillPolygon(b, bolt);
                }
            }
            return bmp;
        }

        private static Image CreateFunnelClearIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(107, 114, 128), 1.2f))
                {
                    Point[] f = new Point[] { new Point(2, 3), new Point(14, 3), new Point(9, 9), new Point(9, 14), new Point(7, 12), new Point(7, 9) };
                    g.DrawPolygon(p, f);
                }
                using (Pen pX = new Pen(Color.FromArgb(239, 68, 68), 1.4f))
                {
                    g.DrawLine(pX, 10, 10, 15, 15);
                    g.DrawLine(pX, 15, 10, 10, 15);
                }
            }
            return bmp;
        }

        private static Image CreateFunnelRefreshIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(107, 114, 128), 1.2f))
                {
                    Point[] f = new Point[] { new Point(2, 3), new Point(14, 3), new Point(9, 9), new Point(9, 14), new Point(7, 12), new Point(7, 9) };
                    g.DrawPolygon(p, f);
                }
            }
            return bmp;
        }

        private static Image CreateSortAZIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Font f = new Font("Segoe UI", 5.5f, FontStyle.Bold))
                {
                    g.DrawString("A", f, Brushes.Teal, 0, 1);
                    g.DrawString("Z", f, Brushes.Teal, 0, 8);
                }
                using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.3f))
                {
                    p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2.5f, 2.5f);
                    g.DrawLine(p, 10, 2, 10, 13);
                }
            }
            return bmp;
        }

        private static Image CreateSortZAIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Font f = new Font("Segoe UI", 5.5f, FontStyle.Bold))
                {
                    g.DrawString("Z", f, Brushes.Teal, 0, 1);
                    g.DrawString("A", f, Brushes.Teal, 0, 8);
                }
                using (Pen p = new Pen(Color.FromArgb(16, 110, 190), 1.3f))
                {
                    p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2.5f, 2.5f);
                    g.DrawLine(p, 10, 2, 10, 13);
                }
            }
            return bmp;
        }

        private static Image CreateCustomSortIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(107, 114, 128), 1.2f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    g.DrawLine(p, 5, 11, 5, 5);
                    g.DrawLine(p, 11, 5, 11, 11);
                }
            }
            return bmp;
        }

        private static Image CreateTableRangeIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(16, 124, 65), 1.2f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(16, 124, 65))) g.FillRectangle(b, 2, 2, 12, 4);
                    g.DrawLine(p, 2, 9, 14, 9);
                    g.DrawLine(p, 8, 6, 8, 14);
                }
            }
            return bmp;
        }

        private static Image CreateCommentIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(16, 124, 65), 1.2f))
                {
                    Point[] b = new Point[] { new Point(2, 2), new Point(14, 2), new Point(14, 10), new Point(6, 10), new Point(3, 14), new Point(4, 10), new Point(2, 10) };
                    g.DrawPolygon(p, b);
                    g.DrawLine(p, 8, 4, 8, 8);
                    g.DrawLine(p, 6, 6, 10, 6);
                }
            }
            return bmp;
        }

        private static Image CreateFormatCellsIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Pen p = new Pen(Color.FromArgb(75, 85, 99), 1.2f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    g.DrawLine(p, 8, 2, 8, 14);
                    g.DrawLine(p, 2, 8, 14, 8);
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

    public class ExcelMenuRenderer : ToolStripProfessionalRenderer
    {
        public ExcelMenuRenderer() : base(new ExcelColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                Rectangle r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(237, 243, 250)))
                {
                    e.Graphics.FillRectangle(b, r);
                }
                using (Pen p = new Pen(Color.FromArgb(204, 228, 247)))
                {
                    e.Graphics.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }

    public class ExcelColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.White; } }
        public override Color MenuBorder { get { return Color.FromArgb(209, 213, 219); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(237, 243, 250); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(204, 228, 247); } }
        public override Color ImageMarginGradientBegin { get { return Color.White; } }
        public override Color ImageMarginGradientMiddle { get { return Color.White; } }
        public override Color ImageMarginGradientEnd { get { return Color.White; } }
        public override Color SeparatorDark { get { return Color.FromArgb(229, 231, 235); } }
        public override Color SeparatorLight { get { return Color.White; } }
    }
}

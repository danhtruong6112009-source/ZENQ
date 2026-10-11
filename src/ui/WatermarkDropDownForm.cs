using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class WatermarkDropDownForm : Form
    {
        public Action<string, bool> OnWatermarkSelected; // text, isDiagonal
        public Action OnCustomWatermarkClicked;
        public Action OnRemoveWatermarkClicked;

        private int HoveredIndex = -1;

        public WatermarkDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location;
            this.Size = new Size(390, 405);
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            this.MouseMove += (s, e) =>
            {
                int prev = HoveredIndex;
                int tileW = 110;
                int tileH = 135;

                // Row 1: Tiles 0, 1, 2
                if (e.Y >= 36 && e.Y <= 36 + tileH)
                {
                    if (e.X >= 15 && e.X <= 15 + tileW) HoveredIndex = 0; // BẢO MẬT 1
                    else if (e.X >= 135 && e.X <= 135 + tileW) HoveredIndex = 1; // BẢO MẬT 2
                    else if (e.X >= 255 && e.X <= 255 + tileW) HoveredIndex = 2; // KHÔNG COPY 1
                    else HoveredIndex = -1;
                }
                // Row 2: Tile 3
                else if (e.Y >= 180 && e.Y <= 180 + tileH)
                {
                    if (e.X >= 15 && e.X <= 15 + tileW) HoveredIndex = 3; // KHÔNG COPY 2
                    else HoveredIndex = -1;
                }
                // Actions below
                else if (e.Y >= 335 && e.Y <= 365) HoveredIndex = 4; // Tùy chỉnh
                else if (e.Y >= 368 && e.Y <= 398) HoveredIndex = 5; // Xóa
                else HoveredIndex = -1;

                if (prev != HoveredIndex) Invalidate();
            };

            this.Deactivate += (s, e) =>
            {
                if (!_isClosing)
                {
                    _isClosing = true;
                    this.Close();
                }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(200, 205, 215), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            using (Font groupFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font tileLabelFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (Font itemFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            {
                g.DrawString("Bảo mật (Confidential)", groupFont, Brushes.Black, 15, 12);

                int tileW = 110;
                int tileH = 135;

                // 1. BẢO MẬT 1 (CONFIDENTIAL 1 - chéo)
                DrawWatermarkTile(g, 15, 36, tileW, tileH, "BẢO MẬT", true, "BẢO MẬT 1", tileLabelFont, HoveredIndex == 0);

                // 2. BẢO MẬT 2 (CONFIDENTIAL 2 - ngang)
                DrawWatermarkTile(g, 135, 36, tileW, tileH, "BẢO MẬT", false, "BẢO MẬT 2", tileLabelFont, HoveredIndex == 1);

                // 3. KHÔNG SAO CHÉP 1 (DO NOT COPY 1 - chéo)
                DrawWatermarkTile(g, 255, 36, tileW, tileH, "KHÔNG SAO CHÉP", true, "KHÔNG SAO CHÉP 1", tileLabelFont, HoveredIndex == 2);

                // 4. KHÔNG SAO CHÉP 2 (DO NOT COPY 2 - ngang)
                DrawWatermarkTile(g, 15, 180, tileW, tileH, "KHÔNG SAO CHÉP", false, "KHÔNG SAO CHÉP 2", tileLabelFont, HoveredIndex == 3);

                // Bottom actions
                int actionY = 335;
                g.DrawLine(Pens.LightGray, 0, actionY - 6, Width, actionY - 6);

                DrawActionRow(g, 0, actionY, Width, 30, "📄  Tùy chỉnh Hình mờ (Custom Watermark)...", itemFont, HoveredIndex == 4);
                actionY += 33;
                DrawActionRow(g, 0, actionY, Width, 30, "❌  Xóa Hình mờ (Remove Watermark)", itemFont, HoveredIndex == 5);
            }
        }

        private void DrawActionRow(Graphics g, int x, int y, int w, int h, string text, Font font, bool isHovered)
        {
            Rectangle rowRect = new Rectangle(x + 4, y, w - 8, h);
            if (isHovered)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(235, 243, 255)))
                {
                    g.FillRectangle(hb, rowRect);
                }
            }
            g.DrawString(text, font, Brushes.Black, x + 16, y + 5);
        }

        private void DrawWatermarkTile(Graphics g, int x, int y, int w, int h, string wmText, bool isDiagonal, string label, Font labelFont, bool isHovered)
        {
            Rectangle paperRect = new Rectangle(x, y, w, h - 22);

            // Paper BG
            using (SolidBrush paperBg = new SolidBrush(isHovered ? Color.FromArgb(240, 245, 255) : Color.FromArgb(250, 250, 250)))
            {
                g.FillRectangle(paperBg, paperRect);
            }
            using (Pen p = new Pen(isHovered ? Color.FromArgb(68, 114, 196) : Color.FromArgb(180, 185, 195), isHovered ? 1.5f : 1f))
            {
                g.DrawRectangle(p, paperRect);
            }

            // Draw watermark text inside miniature paper
            GraphicsState state = g.Save();
            g.SetClip(paperRect);
            using (Font f = new Font("Arial", 8.0f, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(100, 160, 160, 160)))
            {
                g.TranslateTransform(paperRect.X + paperRect.Width / 2f, paperRect.Y + paperRect.Height / 2f);
                if (isDiagonal)
                {
                    g.RotateTransform(-45f);
                }
                SizeF sz = g.MeasureString(wmText, f);
                g.DrawString(wmText, f, b, -sz.Width / 2f, -sz.Height / 2f);
            }
            g.Restore(state);

            // Label beneath paper
            SizeF lsz = g.MeasureString(label, labelFont);
            g.DrawString(label, labelFont, Brushes.Black, x + (w - lsz.Width) / 2, y + h - 18);
        }

        private bool _isClosing = false;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int tileW = 110;
            int tileH = 135;

            // 1. CONFIDENTIAL 1
            if (e.X >= 15 && e.X <= 15 + tileW && e.Y >= 36 && e.Y <= 36 + tileH)
            {
                var cb = OnWatermarkSelected;
                var owner = this.Owner;
                if (!_isClosing) { _isClosing = true; this.Close(); }
                if (cb != null)
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        try { owner.BeginInvoke(new Action(() => cb("BẢO MẬT", true))); } catch { cb("BẢO MẬT", true); }
                    }
                    else { cb("BẢO MẬT", true); }
                }
                return;
            }

            // 2. CONFIDENTIAL 2
            if (e.X >= 135 && e.X <= 135 + tileW && e.Y >= 36 && e.Y <= 36 + tileH)
            {
                var cb = OnWatermarkSelected;
                var owner = this.Owner;
                if (!_isClosing) { _isClosing = true; this.Close(); }
                if (cb != null)
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        try { owner.BeginInvoke(new Action(() => cb("BẢO MẬT", false))); } catch { cb("BẢO MẬT", false); }
                    }
                    else { cb("BẢO MẬT", false); }
                }
                return;
            }

            // 3. DO NOT COPY 1
            if (e.X >= 255 && e.X <= 255 + tileW && e.Y >= 36 && e.Y <= 36 + tileH)
            {
                var cb = OnWatermarkSelected;
                var owner = this.Owner;
                if (!_isClosing) { _isClosing = true; this.Close(); }
                if (cb != null)
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        try { owner.BeginInvoke(new Action(() => cb("KHÔNG SAO CHÉP", true))); } catch { cb("KHÔNG SAO CHÉP", true); }
                    }
                    else { cb("KHÔNG SAO CHÉP", true); }
                }
                return;
            }

            // 4. DO NOT COPY 2
            if (e.X >= 15 && e.X <= 15 + tileW && e.Y >= 180 && e.Y <= 180 + tileH)
            {
                var cb = OnWatermarkSelected;
                var owner = this.Owner;
                if (!_isClosing) { _isClosing = true; this.Close(); }
                if (cb != null)
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        try { owner.BeginInvoke(new Action(() => cb("KHÔNG SAO CHÉP", false))); } catch { cb("KHÔNG SAO CHÉP", false); }
                    }
                    else { cb("KHÔNG SAO CHÉP", false); }
                }
                return;
            }

            // Custom Watermark
            if (e.Y >= 335 && e.Y <= 365)
            {
                var cb = OnCustomWatermarkClicked;
                var owner = this.Owner;
                if (!_isClosing) { _isClosing = true; this.Hide(); this.Close(); }
                if (cb != null)
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        try { owner.BeginInvoke(cb); } catch { cb(); }
                    }
                    else { cb(); }
                }
                return;
            }

            // Remove Watermark
            if (e.Y >= 368 && e.Y <= 398)
            {
                var cb = OnRemoveWatermarkClicked;
                var owner = this.Owner;
                if (!_isClosing) { _isClosing = true; this.Hide(); this.Close(); }
                if (cb != null)
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        try { owner.BeginInvoke(cb); } catch { cb(); }
                    }
                    else { cb(); }
                }
                return;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: không cướp tiêu điểm kích hoạt của cửa sổ chính
                return cp;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (this.Owner != null && !this.Owner.IsDisposed)
            {
                try
                {
                    this.Owner.Activate();
                    this.Owner.BringToFront();
                }
                catch { }
            }
        }
    }
}

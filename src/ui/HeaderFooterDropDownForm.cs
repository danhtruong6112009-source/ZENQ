using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class HeaderFooterDropDownForm : Form
    {
        public Action<HeaderFooterPreset> OnPresetSelected;
        public Action OnEditClicked;
        public Action OnRemoveClicked;

        private bool IsHeaderMode = true;
        private int HoveredIndex = -1;

        public HeaderFooterDropDownForm(Point location, bool isHeader)
        {
            this.IsHeaderMode = isHeader;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location;
            this.Size = new Size(380, 530);
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            this.MouseMove += (s, e) =>
            {
                int prev = HoveredIndex;
                int cardY = 38;
                int cardH = 72;
                if (e.X >= 16 && e.X <= 364 && e.Y >= cardY && e.Y <= cardY + cardH) HoveredIndex = 0;
                else if (e.X >= 16 && e.X <= 364 && e.Y >= cardY + 88 && e.Y <= cardY + 88 + cardH) HoveredIndex = 1;
                else if (e.X >= 16 && e.X <= 364 && e.Y >= cardY + 176 && e.Y <= cardY + 176 + cardH) HoveredIndex = 2;
                else if (e.X >= 16 && e.X <= 364 && e.Y >= cardY + 264 && e.Y <= cardY + 264 + cardH) HoveredIndex = 3;
                else if (e.Y >= 400 && e.Y <= 428) HoveredIndex = 4;
                else if (e.Y >= 430 && e.Y <= 458) HoveredIndex = 5;
                else if (e.Y >= 460 && e.Y <= 488) HoveredIndex = 6;
                else if (e.Y >= 490 && e.Y <= 518) HoveredIndex = 7;
                else HoveredIndex = -1;

                if (prev != HoveredIndex) Invalidate();
            };

            this.Deactivate += (s, e) => this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Outer border with subtle shadow tone
            using (Pen borderPen = new Pen(Color.FromArgb(200, 205, 215), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            using (Font titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font nameFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (Font itemFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Font previewFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                g.DrawString("Mẫu tích hợp sẵn (Built-in)", titleFont, Brushes.Navy, 16, 12);

                int cardY = 38;
                int cardH = 72;
                int cardW = 348;

                // 1. Blank Preset
                DrawPresetCard(g, 16, cardY, cardW, cardH, "Trống (Blank)", (g1, r) =>
                {
                    g1.DrawString("[Nhập văn bản vào đây]", previewFont, Brushes.DimGray, r.X + 25, r.Y + 16);
                }, nameFont, HoveredIndex == 0);

                // 2. Blank (Three Columns) Preset
                cardY += 88;
                DrawPresetCard(g, 16, cardY, cardW, cardH, "Trống (Ba cột)", (g1, r) =>
                {
                    g1.DrawString("[Nhập đây]", previewFont, Brushes.DimGray, r.X + 10, r.Y + 16);
                    g1.DrawString("[Nhập đây]", previewFont, Brushes.DimGray, r.X + 130, r.Y + 16);
                    g1.DrawString("[Nhập đây]", previewFont, Brushes.DimGray, r.X + 240, r.Y + 16);
                }, nameFont, HoveredIndex == 1);

                // 3. Austin Preset
                cardY += 88;
                DrawPresetCard(g, 16, cardY, cardW, cardH, "Austin", (g1, r) =>
                {
                    if (IsHeaderMode)
                    {
                        g1.DrawString("[Tiêu đề tài liệu]", previewFont, Brushes.DarkBlue, r.X + 25, r.Y + 10);
                        using (Pen p = new Pen(Color.FromArgb(68, 114, 196), 1.5f))
                        {
                            g1.DrawLine(p, r.X + 10, r.Y + 30, r.X + r.Width - 10, r.Y + 30);
                        }
                    }
                    else
                    {
                        using (Pen p = new Pen(Color.FromArgb(68, 114, 196), 1.5f))
                        {
                            g1.DrawLine(p, r.X + 10, r.Y + 10, r.X + r.Width - 10, r.Y + 10);
                        }
                        g1.DrawString("TRANG 1", previewFont, Brushes.DarkBlue, r.X + 25, r.Y + 18);
                    }
                }, nameFont, HoveredIndex == 2);

                // 4. Banded Preset
                cardY += 88;
                DrawPresetCard(g, 16, cardY, cardW, cardH, "Dải màu (Banded)", (g1, r) =>
                {
                    Rectangle banner = new Rectangle(r.X + 30, r.Y + 10, r.Width - 60, 22);
                    using (SolidBrush blueB = new SolidBrush(Color.FromArgb(43, 87, 154)))
                    {
                        g1.FillRectangle(blueB, banner);
                    }
                    string bannerTxt = IsHeaderMode ? "[TIÊU ĐỀ TÀI LIỆU]" : "1";
                    SizeF sz = g1.MeasureString(bannerTxt, previewFont);
                    g1.DrawString(bannerTxt, previewFont, Brushes.White, banner.X + (banner.Width - sz.Width) / 2, banner.Y + (banner.Height - sz.Height) / 2);
                }, nameFont, HoveredIndex == 3);

                // Bottom Action Items
                int actionY = 400;
                g.DrawLine(Pens.LightGray, 0, actionY - 6, Width, actionY - 6);

                DrawActionRow(g, 0, actionY, Width, 28, "🌐  Thêm " + (IsHeaderMode ? "Đầu trang" : "Chân trang") + " từ ZenQOffice.com", itemFont, HoveredIndex == 4);
                actionY += 28;
                DrawActionRow(g, 0, actionY, Width, 28, "✏️  Chỉnh sửa " + (IsHeaderMode ? "Đầu trang" : "Chân trang"), itemFont, HoveredIndex == 5);
                actionY += 28;
                DrawActionRow(g, 0, actionY, Width, 28, "❌  Xóa " + (IsHeaderMode ? "Đầu trang" : "Chân trang"), itemFont, HoveredIndex == 6);
                actionY += 28;
                DrawActionRow(g, 0, actionY, Width, 28, "💾  Lưu vùng chọn vào Thư viện...", itemFont, HoveredIndex == 7);
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
            g.DrawString(text, font, Brushes.Black, x + 16, y + 4);
        }

        private void DrawPresetCard(Graphics g, int x, int y, int w, int h, string title, Action<Graphics, Rectangle> drawPreview, Font nameFont, bool isHovered)
        {
            g.DrawString(title, nameFont, Brushes.Black, x, y);

            Rectangle previewRect = new Rectangle(x, y + 20, w, h - 22);
            using (SolidBrush cardBg = new SolidBrush(isHovered ? Color.FromArgb(240, 245, 255) : Color.FromArgb(250, 250, 250)))
            {
                g.FillRectangle(cardBg, previewRect);
            }
            using (Pen cardPen = new Pen(isHovered ? Color.FromArgb(68, 114, 196) : Color.FromArgb(215, 220, 228), isHovered ? 1.5f : 1f))
            {
                g.DrawRectangle(cardPen, previewRect);
            }

            drawPreview(g, previewRect);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int cardY = 38;
            int cardH = 72;

            // 1. Blank
            if (e.X >= 16 && e.X <= 364 && e.Y >= cardY && e.Y <= cardY + cardH)
            {
                if (OnPresetSelected != null) OnPresetSelected(HeaderFooterPreset.Blank);
                this.Close();
                return;
            }

            // 2. Blank (Three Columns)
            cardY += 88;
            if (e.X >= 16 && e.X <= 364 && e.Y >= cardY && e.Y <= cardY + cardH)
            {
                if (OnPresetSelected != null) OnPresetSelected(HeaderFooterPreset.ThreeColumns);
                this.Close();
                return;
            }

            // 3. Austin
            cardY += 88;
            if (e.X >= 16 && e.X <= 364 && e.Y >= cardY && e.Y <= cardY + cardH)
            {
                if (OnPresetSelected != null) OnPresetSelected(HeaderFooterPreset.Austin);
                this.Close();
                return;
            }

            // 4. Banded
            cardY += 88;
            if (e.X >= 16 && e.X <= 364 && e.Y >= cardY && e.Y <= cardY + cardH)
            {
                if (OnPresetSelected != null) OnPresetSelected(HeaderFooterPreset.Banded);
                this.Close();
                return;
            }

            // Edit Header / Edit Footer (y: 428..458)
            if (e.Y >= 428 && e.Y <= 458)
            {
                if (OnEditClicked != null) OnEditClicked();
                this.Close();
                return;
            }

            // Remove Header / Remove Footer (y: 458..488)
            if (e.Y >= 458 && e.Y <= 488)
            {
                if (OnRemoveClicked != null) OnRemoveClicked();
                this.Close();
                return;
            }
        }
    }
}

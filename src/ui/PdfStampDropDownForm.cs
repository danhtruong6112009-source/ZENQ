using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class PdfStampDropDownForm : Form
    {
        public Action<string, Color> OnPresetStampSelected;
        public Action<Image> OnCompanyStampImageSelected;
        public Action OnCustomTextStampRequested;

        private int HoveredIndex = -1;
        private bool _isClosing = false;

        private static readonly string[] StampLabels = new string[] {
            "ĐÃ DUYỆT",
            "BẢO MẬT",
            "KHẨN CẤP",
            "HOÀN THÀNH",
            "BẢN SAO"
        };

        private static readonly Color[] StampColors = new Color[] {
            Color.FromArgb(16, 185, 129), // Xanh lục
            Color.FromArgb(220, 38, 38),  // Đỏ
            Color.FromArgb(234, 88, 12),  // Cam đỏ
            Color.FromArgb(37, 99, 235),  // Xanh dương
            Color.FromArgb(107, 114, 128) // Xám
        };

        public PdfStampDropDownForm(Point location, Action<string, Color> onPresetSelected, Action<Image> onImageSelected)
            : this(location)
        {
            this.OnPresetStampSelected = onPresetSelected;
            this.OnCompanyStampImageSelected = onImageSelected;
        }

        public PdfStampDropDownForm(Point location)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location;
            this.Size = new Size(260, 290);
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            this.MouseMove += (s, e) =>
            {
                int prev = HoveredIndex;
                int itemH = 34;
                int startY = 32;

                if (e.Y >= startY && e.Y < startY + (StampLabels.Length * itemH))
                {
                    HoveredIndex = (e.Y - startY) / itemH;
                }
                else if (e.Y >= 210 && e.Y <= 245)
                {
                    HoveredIndex = 5; // Tải con dấu công ty
                }
                else if (e.Y >= 248 && e.Y <= 283)
                {
                    HoveredIndex = 6; // Tự nhập chữ
                }
                else
                {
                    HoveredIndex = -1;
                }

                if (prev != HoveredIndex) Invalidate();
            };

            this.MouseClick += (s, e) =>
            {
                var owner = this.Owner;
                if (HoveredIndex >= 0 && HoveredIndex < StampLabels.Length)
                {
                    string txt = StampLabels[HoveredIndex];
                    Color c = StampColors[HoveredIndex];
                    if (!_isClosing) { _isClosing = true; this.Close(); }
                    if (OnPresetStampSelected != null)
                    {
                        if (owner != null && !owner.IsDisposed)
                        {
                            try { owner.BeginInvoke(new Action(() => OnPresetStampSelected(txt, c))); } catch { OnPresetStampSelected(txt, c); }
                        }
                        else { OnPresetStampSelected(txt, c); }
                    }
                }
                else if (HoveredIndex == 5)
                {
                    if (!_isClosing) { _isClosing = true; this.Close(); }
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Chọn hình ảnh con dấu công ty (PNG/JPG)";
                        ofd.Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Tất cả tệp (*.*)|*.*";
                        if (ofd.ShowDialog(owner != null && !owner.IsDisposed ? owner : null) == DialogResult.OK)
                        {
                            try
                            {
                                Image img = Image.FromFile(ofd.FileName);
                                if (OnCompanyStampImageSelected != null) OnCompanyStampImageSelected(img);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Không thể nạp tệp ảnh con dấu: " + ex.Message, "Lỗi con dấu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else if (HoveredIndex == 6)
                {
                    var cb = OnCustomTextStampRequested;
                    if (!_isClosing) { _isClosing = true; this.Close(); }
                    if (cb != null)
                    {
                        if (owner != null && !owner.IsDisposed)
                        {
                            try { owner.BeginInvoke(cb); } catch { cb(); }
                        }
                        else { cb(); }
                    }
                }
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
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header
            using (Font hFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                g.DrawString("CON DẤU VĂN PHÒNG CÓ SẴN", hFont, Brushes.Gray, 12, 10);
            }

            int itemH = 34;
            int startY = 32;

            using (Font itemFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                for (int i = 0; i < StampLabels.Length; i++)
                {
                    int iy = startY + (i * itemH);
                    bool isHov = (HoveredIndex == i);

                    if (isHov)
                    {
                        using (SolidBrush hovBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        {
                            g.FillRectangle(hovBrush, 1, iy, Width - 2, itemH);
                        }
                    }

                    // Stamp preview box
                    Rectangle stampBox = new Rectangle(12, iy + 4, 115, 25);
                    Color sc = StampColors[i];
                    using (SolidBrush sbg = new SolidBrush(Color.FromArgb(20, sc)))
                    using (Pen sp = new Pen(sc, 1.4f))
                    using (SolidBrush stxt = new SolidBrush(sc))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.FillRectangle(sbg, stampBox);
                        g.DrawRectangle(sp, stampBox);
                        g.DrawString(StampLabels[i], itemFont, stxt, stampBox, sf);
                    }
                }
            }

            // Separator
            g.DrawLine(Pens.LightGray, 8, 206, Width - 8, 206);

            // Action 1: Upload Company Stamp Image
            using (Font actFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                bool hovComp = (HoveredIndex == 5);
                if (hovComp)
                {
                    using (SolidBrush hovB = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    {
                        g.FillRectangle(hovB, 1, 210, Width - 2, 34);
                    }
                }

                // Vector Company Icon
                using (Pen p = new Pen(hovComp ? Color.DarkBlue : Color.FromArgb(71, 85, 105), 1.5f))
                {
                    g.DrawRectangle(p, 14, 218, 14, 16);
                    g.DrawLine(p, 17, 222, 21, 222);
                    g.DrawLine(p, 17, 226, 21, 226);
                    g.DrawLine(p, 17, 230, 21, 230);
                }
                g.DrawString("Tải con dấu công ty...", actFont, hovComp ? Brushes.DarkBlue : darkBrush, 36, 218);

                // Action 2: Custom Text Stamp
                bool hovCust = (HoveredIndex == 6);
                if (hovCust)
                {
                    using (SolidBrush hovB = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    {
                        g.FillRectangle(hovB, 1, 248, Width - 2, 34);
                    }
                }

                // Vector Pen/Edit Icon
                using (Pen p = new Pen(hovCust ? Color.DarkBlue : Color.FromArgb(71, 85, 105), 1.5f))
                {
                    g.DrawLine(p, 14, 268, 24, 258);
                    g.DrawLine(p, 24, 258, 27, 261);
                    g.DrawLine(p, 27, 261, 17, 271);
                    g.DrawLine(p, 17, 271, 14, 268);
                }
                g.DrawString("Nhập chữ con dấu tùy chỉnh...", actFont, hovCust ? Brushes.DarkBlue : darkBrush, 36, 256);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: không chiếm tiêu điểm kích hoạt của cửa sổ chính
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
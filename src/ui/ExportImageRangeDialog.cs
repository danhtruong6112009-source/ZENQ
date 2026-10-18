using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ZenQOffice.Format;

namespace ZenQOffice.UI
{
    public class ExportImageRangeDialog : Form
    {
        public List<int> SelectedIndices { get; private set; } = new List<int>();
        public bool IsAllSelected => rdoAll != null && rdoAll.Checked;
        public bool IsCurrentSelected => rdoCurrent != null && rdoCurrent.Checked;
        public bool IsCustomSelected => rdoCustom != null && rdoCustom.Checked;

        private readonly bool isSlide;
        private readonly int totalItems;
        private readonly int currentItemIndex;

        private RadioButton rdoAll;
        private RadioButton rdoCurrent;
        private RadioButton rdoCustom;
        private TextBox txtRange;
        private Label lblRangeHint;
        private Button btnExport;
        private Button btnCancel;

        public ExportImageRangeDialog(bool isSlide, int totalItems, int currentItemIndex = 0, string customTitle = null)
        {
            this.isSlide = isSlide;
            this.totalItems = Math.Max(1, totalItems);
            this.currentItemIndex = Math.Max(0, Math.Min(this.totalItems - 1, currentItemIndex));

            this.Text = customTitle ?? (isSlide ? "Tùy chọn Xuất Ảnh Slide - Export Slides" : "Tùy chọn Xuất Ảnh Trang - Export Pages");
            this.Size = new Size(500, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildUI();
        }

        private void BuildUI()
        {
            // 1. Header Panel with Fluent modern gradient
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = isSlide ? Color.FromArgb(216, 59, 1) : Color.FromArgb(16, 185, 129)
            };
            pnlHeader.Paint += (s, e) =>
            {
                Color c1 = isSlide ? Color.FromArgb(196, 43, 28) : Color.FromArgb(5, 150, 105);
                Color c2 = isSlide ? Color.FromArgb(234, 88, 12) : Color.FromArgb(16, 185, 129);
                using (var brush = new LinearGradientBrush(pnlHeader.ClientRectangle, c1, c2, LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }

                char iconGlyph = isSlide ? '\uEB9F' : '\uEDE1'; // Photo vs Document
                FluentIconRenderer.DrawGlyph(e.Graphics, iconGlyph, new Rectangle(16, 14, 22, 22), Color.White, 18f);

                using (Font titleF = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font subF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    string titleText = isSlide ? "Tùy chọn Xuất Ảnh Slide" : "Tùy chọn Xuất Ảnh Trang";
                    string subText = isSlide
                        ? $"Bản trình chiếu có {totalItems} slide. Chọn các slide bạn muốn xuất thành tệp ảnh."
                        : $"Tài liệu hiện tại có {totalItems} trang. Chọn các trang bạn muốn xuất thành tệp ảnh.";

                    e.Graphics.DrawString(titleText, titleF, Brushes.White, 44, 12);
                    e.Graphics.DrawString(subText, subF, Brushes.WhiteSmoke, 44, 38);
                }
            };
            this.Controls.Add(pnlHeader);

            // 2. Body Panel
            Panel pnlBody = new Panel
            {
                Location = new Point(16, 75),
                Size = new Size(452, 190)
            };
            this.Controls.Add(pnlBody);

            GroupBox grpRange = new GroupBox
            {
                Text = isSlide ? "Phạm vi Slide cần xuất" : "Phạm vi Trang cần xuất",
                Location = new Point(4, 4),
                Size = new Size(444, 180),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpRange);

            // Option 1: All
            rdoAll = new RadioButton
            {
                Text = isSlide ? $"Tất cả các slide ({totalItems} slide)" : $"Tất cả các trang ({totalItems} trang)",
                Location = new Point(18, 28),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            grpRange.Controls.Add(rdoAll);

            // Option 2: Current
            rdoCurrent = new RadioButton
            {
                Text = isSlide ? $"Chỉ slide hiện tại (Slide {currentItemIndex + 1})" : $"Chỉ trang hiện tại (Trang {currentItemIndex + 1})",
                Location = new Point(18, 62),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            grpRange.Controls.Add(rdoCurrent);

            // Option 3: Custom Range
            rdoCustom = new RadioButton
            {
                Text = isSlide ? "Phạm vi slide cụ thể:" : "Phạm vi trang cụ thể:",
                Location = new Point(18, 98),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            grpRange.Controls.Add(rdoCustom);

            txtRange = new TextBox
            {
                Location = new Point(185, 96),
                Width = 140,
                Text = $"1-{Math.Min(3, totalItems)}",
                Enabled = false,
                Font = new Font("Segoe UI", 9.5f)
            };
            grpRange.Controls.Add(txtRange);

            lblRangeHint = new Label
            {
                Text = "Ví dụ: 1-3, 5, 8-10",
                Location = new Point(332, 99),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5f)
            };
            grpRange.Controls.Add(lblRangeHint);

            Label lblNote = new Label
            {
                Text = isSlide
                    ? "• Nếu chọn 1 slide, ảnh sẽ lưu đúng tên tệp đã chỉ định.\n• Nếu chọn nhiều slide, các ảnh sẽ được đánh số theo từng slide (ví dụ: TenTep_Slide_1.png)."
                    : "• Nếu chọn 1 trang, ảnh sẽ lưu đúng tên tệp đã chỉ định.\n• Nếu chọn nhiều trang, các ảnh sẽ được đánh số theo từng trang (ví dụ: TenTep_Trang_1.png).",
                Location = new Point(18, 130),
                Size = new Size(410, 42),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular)
            };
            grpRange.Controls.Add(lblNote);

            rdoCustom.CheckedChanged += (s, e) =>
            {
                txtRange.Enabled = rdoCustom.Checked;
                if (rdoCustom.Checked)
                {
                    txtRange.Focus();
                    txtRange.SelectAll();
                }
            };

            // 3. Bottom Button Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(pnlBottom);

            btnExport = new Button
            {
                Text = "Xuất ảnh",
                Size = new Size(110, 34),
                Location = new Point(245, 9),
                BackColor = isSlide ? Color.FromArgb(216, 59, 1) : Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE73E', 13, 13, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            pnlBottom.Controls.Add(btnExport);

            btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Size = new Size(95, 34),
                Location = new Point(365, 9),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 11, 11, Color.FromArgb(71, 85, 105)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlBottom.Controls.Add(btnCancel);

            this.AcceptButton = btnExport;
            this.CancelButton = btnCancel;
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SelectedIndices.Clear();

            if (rdoAll.Checked)
            {
                SelectedIndices = Enumerable.Range(0, totalItems).ToList();
            }
            else if (rdoCurrent.Checked)
            {
                SelectedIndices.Add(currentItemIndex);
            }
            else if (rdoCustom.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtRange.Text))
                {
                    MessageBox.Show(this, isSlide ? "Vui lòng nhập phạm vi slide cần xuất!" : "Vui lòng nhập phạm vi trang cần xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRange.Focus();
                    return;
                }

                SelectedIndices = PdfHandler.ParsePageRange(txtRange.Text, totalItems);
                if (SelectedIndices == null || SelectedIndices.Count == 0)
                {
                    MessageBox.Show(this, isSlide ? "Phạm vi slide không hợp lệ hoặc nằm ngoài số lượng slide trong bản trình chiếu!" : "Phạm vi trang không hợp lệ hoặc nằm ngoài số lượng trang trong tài liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRange.Focus();
                    return;
                }
            }

            if (SelectedIndices.Count == 0)
            {
                MessageBox.Show(this, "Không có mục nào được chọn để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Format;

namespace ZenQOffice.UI
{
    public class PdfExtractPagesDialog : Form
    {
        public List<int> SelectedPageIndices { get; private set; } = new List<int>();
        public bool DeleteAfterExtract { get; private set; } = false;
        public bool SaveDirectlyToFile { get; private set; } = false;
        public string OutputFilePath { get; private set; } = "";

        private int totalPages;
        private int currentPage;

        private RadioButton rdoCurrent;
        private RadioButton rdoRange;
        private TextBox txtRange;
        private RadioButton rdoOdd;
        private RadioButton rdoEven;

        private CheckBox chkDeleteOriginal;
        private RadioButton rdoOpenNew;
        private RadioButton rdoSaveDirect;
        private TextBox txtSavePath;
        private Button btnBrowseSave;

        private Button btnExtract;
        private Button btnCancel;

        public PdfExtractPagesDialog(int totalPages, int currentPage)
        {
            this.totalPages = Math.Max(1, totalPages);
            this.currentPage = Math.Max(1, Math.Min(currentPage, this.totalPages));

            this.Text = "Trích xuất Trang PDF - Extract Pages";
            this.Size = new Size(520, 500);
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
            // Header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(16, 185, 129)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(pnlHeader.ClientRectangle, Color.FromArgb(5, 150, 105), Color.FromArgb(16, 185, 129), LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uEDE1', new Rectangle(16, 14, 22, 22), Color.White, 18f);
                using (Font titleF = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font subF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("Trích xuất Trang Tài liệu (Extract Pages)", titleF, Brushes.White, 44, 12);
                    e.Graphics.DrawString($"Tài liệu hiện tại có {totalPages} trang. Chọn các trang cần tách thành tệp mới.", subF, Brushes.WhiteSmoke, 44, 38);
                }
            };
            this.Controls.Add(pnlHeader);

            Panel pnlBody = new Panel
            {
                Location = new Point(16, 75),
                Size = new Size(472, 330)
            };
            this.Controls.Add(pnlBody);

            // Group 1: Phạm vi trang
            GroupBox grpPages = new GroupBox
            {
                Text = "Phạm vi Trang cần Trích xuất",
                Location = new Point(4, 4),
                Size = new Size(464, 155),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpPages);

            rdoCurrent = new RadioButton
            {
                Text = $"Trang hiện tại (Trang {currentPage})",
                Location = new Point(16, 24),
                AutoSize = true,
                Checked = true
            };
            grpPages.Controls.Add(rdoCurrent);

            rdoRange = new RadioButton
            {
                Text = "Dải trang chỉ định:",
                Location = new Point(16, 52),
                AutoSize = true
            };
            grpPages.Controls.Add(rdoRange);

            txtRange = new TextBox
            {
                Location = new Point(155, 49),
                Width = 180,
                Text = $"1-{Math.Min(3, totalPages)}",
                Enabled = false
            };
            grpPages.Controls.Add(txtRange);

            Label lblRangeHint = new Label
            {
                Text = "Ví dụ: 1-3, 5, 8-10",
                Location = new Point(345, 52),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.2f)
            };
            grpPages.Controls.Add(lblRangeHint);

            rdoOdd = new RadioButton
            {
                Text = "Tất cả các trang lẻ (1, 3, 5, ...)",
                Location = new Point(16, 82),
                AutoSize = true
            };
            grpPages.Controls.Add(rdoOdd);

            rdoEven = new RadioButton
            {
                Text = "Tất cả các trang chẵn (2, 4, 6, ...)",
                Location = new Point(16, 110),
                AutoSize = true
            };
            grpPages.Controls.Add(rdoEven);

            rdoRange.CheckedChanged += (s, e) => { txtRange.Enabled = rdoRange.Checked; if (rdoRange.Checked) txtRange.Focus(); };

            // Group 2: Tùy chọn xử lý
            GroupBox grpOptions = new GroupBox
            {
                Text = "Tùy chọn Đầu ra & Lưu trữ",
                Location = new Point(4, 165),
                Size = new Size(464, 155),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpOptions);

            chkDeleteOriginal = new CheckBox
            {
                Text = "Xóa các trang này khỏi tài liệu gốc sau khi trích xuất",
                Location = new Point(16, 24),
                AutoSize = true,
                ForeColor = Color.FromArgb(185, 28, 28)
            };
            grpOptions.Controls.Add(chkDeleteOriginal);

            rdoOpenNew = new RadioButton
            {
                Text = "Mở các trang đã trích xuất trong tài liệu mới",
                Location = new Point(16, 54),
                AutoSize = true,
                Checked = true
            };
            grpOptions.Controls.Add(rdoOpenNew);

            rdoSaveDirect = new RadioButton
            {
                Text = "Lưu trực tiếp vào tệp PDF:",
                Location = new Point(16, 84),
                AutoSize = true
            };
            grpOptions.Controls.Add(rdoSaveDirect);

            txtSavePath = new TextBox
            {
                Location = new Point(36, 112),
                Width = 320,
                Enabled = false
            };
            grpOptions.Controls.Add(txtSavePath);

            btnBrowseSave = new Button
            {
                Text = "Duyệt...",
                Location = new Point(356, 110),
                Size = new Size(88, 28),
                Enabled = false,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE838', 13, 13, Color.FromArgb(71, 85, 105)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnBrowseSave.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnBrowseSave.Click += (s, e) =>
            {
                using (var sfd = new SaveFileDialog { Filter = "Tệp PDF (*.pdf)|*.pdf", Title = "Chọn vị trí lưu tệp PDF trích xuất" })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        txtSavePath.Text = sfd.FileName;
                    }
                }
            };
            grpOptions.Controls.Add(btnBrowseSave);

            rdoSaveDirect.CheckedChanged += (s, e) =>
            {
                txtSavePath.Enabled = rdoSaveDirect.Checked;
                btnBrowseSave.Enabled = rdoSaveDirect.Checked;
            };

            // Bottom
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(pnlBottom);

            btnExtract = new Button
            {
                Text = "Trích xuất",
                Size = new Size(130, 34),
                Location = new Point(230, 10),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Image = FluentIconRenderer.GetGlyphBitmap('\uEDE1', 14, 14, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnExtract.FlatAppearance.BorderSize = 0;
            btnExtract.Click += BtnExtract_Click;
            pnlBottom.Controls.Add(btnExtract);

            btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Size = new Size(95, 34),
                Location = new Point(370, 10),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 12, 12, Color.FromArgb(71, 85, 105)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlBottom.Controls.Add(btnCancel);
        }

        private void BtnExtract_Click(object sender, EventArgs e)
        {
            SelectedPageIndices.Clear();

            if (rdoCurrent.Checked)
            {
                SelectedPageIndices.Add(currentPage - 1);
            }
            else if (rdoRange.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtRange.Text))
                {
                    MessageBox.Show(this, "Vui lòng nhập dải trang cần trích xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRange.Focus();
                    return;
                }
                SelectedPageIndices = PdfHandler.ParsePageRange(txtRange.Text, totalPages);
                if (SelectedPageIndices.Count == 0)
                {
                    MessageBox.Show(this, "Dải trang không hợp lệ hoặc nằm ngoài phạm vi tài liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRange.Focus();
                    return;
                }
            }
            else if (rdoOdd.Checked)
            {
                for (int i = 0; i < totalPages; i += 2) SelectedPageIndices.Add(i);
            }
            else if (rdoEven.Checked)
            {
                for (int i = 1; i < totalPages; i += 2) SelectedPageIndices.Add(i);
            }

            if (SelectedPageIndices.Count == 0)
            {
                MessageBox.Show(this, "Không có trang nào được chọn để trích xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rdoSaveDirect.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtSavePath.Text))
                {
                    MessageBox.Show(this, "Vui lòng chọn đường dẫn tệp để lưu các trang trích xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnBrowseSave.PerformClick();
                    if (string.IsNullOrWhiteSpace(txtSavePath.Text)) return;
                }
                SaveDirectlyToFile = true;
                OutputFilePath = txtSavePath.Text;
            }
            else
            {
                SaveDirectlyToFile = false;
            }

            DeleteAfterExtract = chkDeleteOriginal.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

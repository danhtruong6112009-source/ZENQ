using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Format;

namespace ZenQOffice.UI
{
    public class PdfInsertPagesDialog : Form
    {
        public string SelectedFilePath { get; private set; } = "";
        public int InsertTargetIndex { get; private set; } = 0;

        private int totalPages;
        private int currentPage;

        private TextBox txtFile;
        private Button btnBrowse;
        private Label lblFileInfo;

        private RadioButton rdoBeforeFirst;
        private RadioButton rdoAfterLast;
        private RadioButton rdoBeforeCurrent;
        private RadioButton rdoAfterCurrent;

        private Button btnInsert;
        private Button btnCancel;

        public PdfInsertPagesDialog(int totalPages, int currentPage)
        {
            this.totalPages = Math.Max(1, totalPages);
            this.currentPage = Math.Max(1, Math.Min(currentPage, this.totalPages));

            this.Text = "Chèn Trang từ Tệp PDF - Insert Pages from File";
            this.Size = new Size(520, 460);
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
                BackColor = Color.FromArgb(37, 99, 235)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(pnlHeader.ClientRectangle, Color.FromArgb(29, 78, 216), Color.FromArgb(37, 99, 235), LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE710', new Rectangle(16, 14, 22, 22), Color.White, 18f);
                using (Font titleF = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font subF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("Chèn Trang từ Tệp PDF (Insert Pages)", titleF, Brushes.White, 44, 12);
                    e.Graphics.DrawString($"Tài liệu hiện tại có {totalPages} trang. Chọn tệp PDF và vị trí cần ghép vào.", subF, Brushes.WhiteSmoke, 44, 38);
                }
            };
            this.Controls.Add(pnlHeader);

            Panel pnlBody = new Panel
            {
                Location = new Point(16, 75),
                Size = new Size(472, 290)
            };
            this.Controls.Add(pnlBody);

            // Group 1: Chọn tệp
            GroupBox grpFile = new GroupBox
            {
                Text = "Tệp PDF Cần Chèn",
                Location = new Point(4, 4),
                Size = new Size(464, 105),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpFile);

            Label lblPath = new Label { Text = "Đường dẫn tệp:", Location = new Point(16, 26), AutoSize = true };
            grpFile.Controls.Add(lblPath);

            txtFile = new TextBox
            {
                Location = new Point(16, 48),
                Width = 345,
                ReadOnly = true,
                BackColor = Color.White
            };
            grpFile.Controls.Add(txtFile);

            btnBrowse = new Button
            {
                Text = "Duyệt...",
                Location = new Point(366, 46),
                Size = new Size(88, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE838', 13, 13, Color.FromArgb(71, 85, 105)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnBrowse.Click += BtnBrowse_Click;
            grpFile.Controls.Add(btnBrowse);

            lblFileInfo = new Label
            {
                Text = "Chưa chọn tệp PDF nào.",
                Location = new Point(16, 78),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f)
            };
            grpFile.Controls.Add(lblFileInfo);

            // Group 2: Vị trí chèn
            GroupBox grpLoc = new GroupBox
            {
                Text = "Vị trí Chèn vào Tài liệu",
                Location = new Point(4, 115),
                Size = new Size(464, 165),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpLoc);

            rdoBeforeFirst = new RadioButton
            {
                Text = "Trước trang đầu tiên (Đầu tài liệu)",
                Location = new Point(16, 26),
                AutoSize = true
            };
            grpLoc.Controls.Add(rdoBeforeFirst);

            rdoAfterLast = new RadioButton
            {
                Text = "Sau trang cuối cùng (Cuối tài liệu)",
                Location = new Point(16, 56),
                AutoSize = true,
                Checked = true
            };
            grpLoc.Controls.Add(rdoAfterLast);

            rdoBeforeCurrent = new RadioButton
            {
                Text = $"Trước trang hiện tại (Trước trang {currentPage})",
                Location = new Point(16, 86),
                AutoSize = true
            };
            grpLoc.Controls.Add(rdoBeforeCurrent);

            rdoAfterCurrent = new RadioButton
            {
                Text = $"Sau trang hiện tại (Sau trang {currentPage})",
                Location = new Point(16, 116),
                AutoSize = true
            };
            grpLoc.Controls.Add(rdoAfterCurrent);

            // Bottom
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(pnlBottom);

            btnInsert = new Button
            {
                Text = "Chèn Trang",
                Size = new Size(130, 34),
                Location = new Point(230, 10),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE710', 14, 14, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnInsert.FlatAppearance.BorderSize = 0;
            btnInsert.Click += BtnInsert_Click;
            pnlBottom.Controls.Add(btnInsert);

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

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Tệp PDF (*.pdf)|*.pdf", Title = "Chọn tệp PDF cần chèn" })
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    txtFile.Text = ofd.FileName;
                    try
                    {
                        var infoDoc = PdfHandler.LoadPdf(ofd.FileName);
                        int pCount = (infoDoc != null && infoDoc.Pages != null) ? infoDoc.Pages.Count : 1;
                        long sizeKb = new FileInfo(ofd.FileName).Length / 1024;
                        lblFileInfo.Text = $"Tệp hợp lệ: {pCount} trang ({sizeKb:N0} KB)";
                        lblFileInfo.ForeColor = Color.FromArgb(16, 185, 129);
                    }
                    catch
                    {
                        lblFileInfo.Text = "Tệp PDF đã chọn.";
                        lblFileInfo.ForeColor = Color.Gray;
                    }
                }
            }
        }

        private void BtnInsert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFile.Text) || !File.Exists(txtFile.Text))
            {
                MessageBox.Show(this, "Vui lòng chọn một tệp PDF hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnBrowse.PerformClick();
                return;
            }

            SelectedFilePath = txtFile.Text;

            if (rdoBeforeFirst.Checked)
            {
                InsertTargetIndex = 0;
            }
            else if (rdoAfterLast.Checked)
            {
                InsertTargetIndex = totalPages;
            }
            else if (rdoBeforeCurrent.Checked)
            {
                InsertTargetIndex = currentPage - 1;
            }
            else if (rdoAfterCurrent.Checked)
            {
                InsertTargetIndex = currentPage;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

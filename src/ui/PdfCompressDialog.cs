using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class PdfCompressDialog : Form
    {
        public int TargetQualityPercent { get; private set; } = 70;
        public float TargetMaxDpi { get; private set; } = 150f;
        public bool StripMetadata { get; private set; } = true;
        public string OutputFilePath { get; private set; } = "";

        private RadioButton rdoStandard;
        private RadioButton rdoMax;
        private RadioButton rdoHigh;
        private RadioButton rdoCustom;
        private NumericUpDown numDpi;
        private NumericUpDown numQuality;
        private CheckBox chkStripMetadata;

        private TextBox txtOutput;
        private Button btnBrowse;
        private Button btnCompress;
        private Button btnCancel;

        public PdfCompressDialog(PdfDocument doc)
        {
            this.Text = AppL10n.T("Nén & Tối ưu hóa Dung lượng PDF - Compress & Optimize PDF", "Compress & Optimize PDF");
            this.Size = new Size(530, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildUI(doc);
        }

        private void BuildUI(PdfDocument doc)
        {
            // Header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(14, 165, 233)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(pnlHeader.ClientRectangle, Color.FromArgb(2, 132, 199), Color.FromArgb(14, 165, 233), LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE8B7', new Rectangle(16, 14, 22, 22), Color.White, 18f);
                using (Font titleF = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font subF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString(AppL10n.T("Nén & Tối ưu hóa Dung lượng PDF", "Compress & Optimize PDF"), titleF, Brushes.White, 44, 12);
                    int pCount = (doc != null && doc.Pages != null) ? doc.Pages.Count : 1;
                    e.Graphics.DrawString(string.Format(AppL10n.T("Tài liệu gồm {0} trang. Giảm dung lượng tệp thông minh mà vẫn giữ độ sắc nét.", "Document has {0} page(s). Smart compression reduces size while preserving clarity."), pCount), subF, Brushes.WhiteSmoke, 44, 38);
                }
            };
            this.Controls.Add(pnlHeader);

            Panel pnlBody = new Panel
            {
                Location = new Point(16, 75),
                Size = new Size(482, 350)
            };
            this.Controls.Add(pnlBody);

            // Group 1: Mức độ nén
            GroupBox grpPreset = new GroupBox
            {
                Text = AppL10n.T("Cấp độ Nén (Compression Presets)", "Compression Presets"),
                Location = new Point(4, 4),
                Size = new Size(474, 190),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpPreset);

            rdoStandard = new RadioButton
            {
                Text = AppL10n.T("Tiêu chuẩn (150 DPI, Chất lượng 70%) - Khuyên dùng cho chia sẻ & lưu trữ", "Standard (150 DPI, 70% quality) - Recommended for sharing & archive"),
                Location = new Point(16, 26),
                AutoSize = true,
                Checked = true
            };
            grpPreset.Controls.Add(rdoStandard);

            rdoMax = new RadioButton
            {
                Text = AppL10n.T("Tối đa (72 DPI, Chất lượng 50%) - Dung lượng nhỏ nhất, phù hợp email/web", "Maximum (72 DPI, 50% quality) - Smallest size for email/web"),
                Location = new Point(16, 56),
                AutoSize = true
            };
            grpPreset.Controls.Add(rdoMax);

            rdoHigh = new RadioButton
            {
                Text = AppL10n.T("Chất lượng cao (300 DPI, Chất lượng 85%) - Giữ độ nét cao phục vụ in ấn", "High Quality (300 DPI, 85% quality) - Print-ready sharpness"),
                Location = new Point(16, 86),
                AutoSize = true
            };
            grpPreset.Controls.Add(rdoHigh);

            rdoCustom = new RadioButton
            {
                Text = AppL10n.T("Tùy biến tham số DPI và tỷ lệ nén ảnh:", "Custom DPI and image quality:"),
                Location = new Point(16, 116),
                AutoSize = true
            };
            grpPreset.Controls.Add(rdoCustom);

            Label lblDpi = new Label { Text = AppL10n.T("DPI tối đa:", "Max DPI:"), Location = new Point(40, 148), AutoSize = true };
            grpPreset.Controls.Add(lblDpi);

            numDpi = new NumericUpDown
            {
                Location = new Point(110, 145),
                Width = 70,
                Minimum = 50,
                Maximum = 600,
                Value = 150,
                Enabled = false
            };
            grpPreset.Controls.Add(numDpi);

            Label lblQuality = new Label { Text = AppL10n.T("Chất lượng ảnh (%):", "Image quality (%):"), Location = new Point(200, 148), AutoSize = true };
            grpPreset.Controls.Add(lblQuality);

            numQuality = new NumericUpDown
            {
                Location = new Point(325, 145),
                Width = 70,
                Minimum = 10,
                Maximum = 100,
                Value = 70,
                Enabled = false
            };
            grpPreset.Controls.Add(numQuality);

            rdoCustom.CheckedChanged += (s, e) =>
            {
                numDpi.Enabled = rdoCustom.Checked;
                numQuality.Enabled = rdoCustom.Checked;
            };

            // Group 2: Tùy chọn & Tệp đầu ra
            GroupBox grpOut = new GroupBox
            {
                Text = AppL10n.T("Tùy chọn Bổ sung & Đường dẫn Lưu", "Additional Options & Output"),
                Location = new Point(4, 200),
                Size = new Size(474, 140),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpOut);

            chkStripMetadata = new CheckBox
            {
                Text = AppL10n.T("Lược bỏ Siêu dữ liệu dư thừa & Cache bản dựng", "Strip redundant metadata & build cache"),
                Location = new Point(16, 24),
                AutoSize = true,
                Checked = true
            };
            grpOut.Controls.Add(chkStripMetadata);

            Label lblSave = new Label { Text = AppL10n.T("Lưu tệp nén tại:", "Save compressed file to:"), Location = new Point(16, 56), AutoSize = true };
            grpOut.Controls.Add(lblSave);

            string defaultSavePath = (doc != null && !string.IsNullOrEmpty(doc.FilePath))
                ? Path.Combine(Path.GetDirectoryName(doc.FilePath), Path.GetFileNameWithoutExtension(doc.FilePath) + "_Compressed.pdf")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Document_Compressed.pdf");

            txtOutput = new TextBox
            {
                Location = new Point(16, 80),
                Width = 350,
                Text = defaultSavePath
            };
            grpOut.Controls.Add(txtOutput);

            btnBrowse = new Button
            {
                Text = AppL10n.T("Duyệt...", "Browse..."),
                Location = new Point(374, 78),
                Size = new Size(90, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE838', 13, 13, Color.FromArgb(71, 85, 105)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnBrowse.Click += (s, e) =>
            {
                using (var sfd = new SaveFileDialog { Filter = AppL10n.T("Tệp PDF (*.pdf)|*.pdf", "PDF Files (*.pdf)|*.pdf"), Title = AppL10n.T("Chọn vị trí lưu tệp PDF đã nén", "Save Compressed PDF"), FileName = Path.GetFileName(txtOutput.Text) })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        txtOutput.Text = sfd.FileName;
                    }
                }
            };
            grpOut.Controls.Add(btnBrowse);

            // Bottom
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(pnlBottom);

            btnCompress = new Button
            {
                Text = AppL10n.T("Bắt đầu Nén", "Compress"),
                Size = new Size(130, 34),
                Location = new Point(245, 10),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8B7', 14, 14, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnCompress.FlatAppearance.BorderSize = 0;
            btnCompress.Click += BtnCompress_Click;
            pnlBottom.Controls.Add(btnCompress);

            btnCancel = new Button
            {
                Text = AppL10n.T("Hủy bỏ", "Cancel"),
                Size = new Size(95, 34),
                Location = new Point(385, 10),
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

        private void BtnCompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Vui lòng chọn đường dẫn để lưu tệp PDF đã nén!", "Please select a destination path to save the compressed PDF!"), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnBrowse.PerformClick();
                return;
            }

            if (rdoStandard.Checked)
            {
                TargetMaxDpi = 150f;
                TargetQualityPercent = 70;
            }
            else if (rdoMax.Checked)
            {
                TargetMaxDpi = 72f;
                TargetQualityPercent = 50;
            }
            else if (rdoHigh.Checked)
            {
                TargetMaxDpi = 300f;
                TargetQualityPercent = 85;
            }
            else if (rdoCustom.Checked)
            {
                TargetMaxDpi = (float)numDpi.Value;
                TargetQualityPercent = (int)numQuality.Value;
            }

            StripMetadata = chkStripMetadata.Checked;
            OutputFilePath = txtOutput.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

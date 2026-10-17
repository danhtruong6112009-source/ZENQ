using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Gửi Tài liệu qua Email chuẩn Microsoft Office 365.
    /// Hỗ trợ tạo tệp đính kèm sạch sẽ (DOCX/XLSX/PPTX hoặc PDF)
    /// và khởi chạy ứng dụng thư mặc định (Outlook, Thunderbird, Windows Mail)
    /// hoặc gửi trực tiếp qua MAPI / mailto.
    /// </summary>
    public class SendEmailDialogForm : Form
    {
        private readonly string _documentTitle;
        private readonly string _currentFilePath;
        private readonly Func<string, bool> _saveTempCopyAction;
        private readonly Func<string, bool> _exportPdfAction;

        private TextBox _txtTo;
        private TextBox _txtCc;
        private TextBox _txtSubject;
        private TextBox _txtBody;
        private RadioButton _radOriginal;
        private RadioButton _radPdf;
        private Label _lblAttachmentInfo;
        private Button _btnSend;
        private Button _btnClose;
        private Button _btnOpenFolder;

        public SendEmailDialogForm(string docTitle, string currentFilePath, Func<string, bool> saveTempCopyAction = null, Func<string, bool> exportPdfAction = null)
        {
            _documentTitle = !string.IsNullOrEmpty(docTitle) ? docTitle : "Tai_Lieu_ZenQOffice";
            _currentFilePath = currentFilePath;
            _saveTempCopyAction = saveTempCopyAction;
            _exportPdfAction = exportPdfAction;

            InitializeComponent();
            UpdateAttachmentLabel();
        }

        private void InitializeComponent()
        {
            this.Text = "Chia sẻ & Gửi Thư điện tử (Send by Email)";
            this.Size = new Size(540, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Top Header Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(16, 110, 190)
            };
            topPanel.Paint += (s, e) =>
            {
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE715', new Rectangle(16, 14, 24, 24), Color.White, 20f);
                using (Font hFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("Gửi tài liệu qua Email", hFont, Brushes.White, 48, 8);
                    e.Graphics.DrawString("Đính kèm tệp tài liệu và soạn thư trực tiếp trong ứng dụng Mail", subFont, Brushes.WhiteSmoke, 48, 32);
                }
            };
            this.Controls.Add(topPanel);

            int curY = 68;

            // To
            Label lblTo = new Label { Text = "Gửi tới (To):", Location = new Point(18, curY + 2), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            this.Controls.Add(lblTo);

            _txtTo = new TextBox { Location = new Point(130, curY), Size = new Size(376, 24), PlaceholderText = "nhap_email_nguoi_nhan@example.com" };
            this.Controls.Add(_txtTo);
            curY += 34;

            // CC
            Label lblCc = new Label { Text = "Đồng gửi (CC):", Location = new Point(18, curY + 2), AutoSize = true };
            this.Controls.Add(lblCc);

            _txtCc = new TextBox { Location = new Point(130, curY), Size = new Size(376, 24) };
            this.Controls.Add(_txtCc);
            curY += 34;

            // Subject
            Label lblSubject = new Label { Text = "Tiêu đề (Subject):", Location = new Point(18, curY + 2), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            this.Controls.Add(lblSubject);

            _txtSubject = new TextBox { Location = new Point(130, curY), Size = new Size(376, 24), Text = "Tài liệu: " + _documentTitle };
            this.Controls.Add(_txtSubject);
            curY += 34;

            // Format Options
            GroupBox grpFormat = new GroupBox { Text = "Định dạng tệp đính kèm", Location = new Point(18, curY), Size = new Size(488, 62) };
            _radOriginal = new RadioButton { Text = "Tệp gốc tài liệu (" + GetDefaultExtension() + ")", Location = new Point(16, 24), AutoSize = true, Checked = true };
            _radOriginal.CheckedChanged += (s, e) => UpdateAttachmentLabel();
            grpFormat.Controls.Add(_radOriginal);

            _radPdf = new RadioButton { Text = "Tệp định dạng PDF (.pdf)", Location = new Point(240, 24), AutoSize = true };
            _radPdf.CheckedChanged += (s, e) => UpdateAttachmentLabel();
            grpFormat.Controls.Add(_radPdf);
            this.Controls.Add(grpFormat);
            curY += 70;

            // Body
            Label lblBody = new Label { Text = "Nội dung thư (Message):", Location = new Point(18, curY), AutoSize = true };
            this.Controls.Add(lblBody);
            curY += 22;

            _txtBody = new TextBox
            {
                Location = new Point(18, curY),
                Size = new Size(488, 110),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "Kính gửi quý anh/chị,\r\n\r\nTôi xin gửi tài liệu đính kèm \"" + _documentTitle + "\" để phục vụ công việc.\r\n\r\nTrân trọng,\r\nZenQ Office User"
            };
            this.Controls.Add(_txtBody);
            curY += 118;

            // Attachment Status
            _lblAttachmentInfo = new Label
            {
                Location = new Point(18, curY),
                Size = new Size(488, 36),
                ForeColor = Color.FromArgb(70, 70, 70),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };
            this.Controls.Add(_lblAttachmentInfo);
            curY += 40;

            // Bottom Buttons
            _btnSend = new Button
            {
                Text = "Mở ứng dụng Mail & Đính kèm",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE715', 16, 16, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(18, curY),
                Size = new Size(240, 32),
                BackColor = Color.FromArgb(16, 110, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += (s, e) => DispatchEmail();
            this.Controls.Add(_btnSend);

            _btnOpenFolder = new Button
            {
                Text = "Thư mục đính kèm",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE838', 15, 15, Color.Black),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(266, curY),
                Size = new Size(140, 32),
                BackColor = Color.FromArgb(235, 236, 238),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnOpenFolder.FlatAppearance.BorderColor = Color.LightGray;
            _btnOpenFolder.Click += (s, e) => OpenAttachmentFolder();
            this.Controls.Add(_btnOpenFolder);

            _btnClose = new Button
            {
                Text = "Đóng",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 13, 13, Color.Black),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(414, curY),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(235, 236, 238),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.LightGray;
            _btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(_btnClose);
        }

        private string GetDefaultExtension()
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                string ext = Path.GetExtension(_currentFilePath);
                if (!string.IsNullOrEmpty(ext)) return ext;
            }
            return ".docx";
        }

        private void UpdateAttachmentLabel()
        {
            string ext = _radPdf.Checked ? ".pdf" : GetDefaultExtension();
            string fileName = Path.GetFileNameWithoutExtension(_documentTitle) + ext;
            _lblAttachmentInfo.Text = "Tệp đính kèm sẽ được chuẩn bị: " + fileName + "\nỨng dụng sẽ tự động khởi chạy Outlook, Thunderbird hoặc Windows Mail.";
        }

        private string PrepareAttachmentFile()
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOffice_MailAttachments");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                string baseName = Path.GetFileNameWithoutExtension(_documentTitle);
                if (string.IsNullOrEmpty(baseName)) baseName = "Tai_Lieu";

                if (_radPdf.Checked)
                {
                    string targetPdf = Path.Combine(tempDir, baseName + ".pdf");
                    if (_exportPdfAction != null)
                    {
                        if (_exportPdfAction(targetPdf) && File.Exists(targetPdf)) return targetPdf;
                    }
                    if (File.Exists(_currentFilePath) && _currentFilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(_currentFilePath, targetPdf, true);
                        return targetPdf;
                    }
                }
                else
                {
                    string ext = GetDefaultExtension();
                    string targetDoc = Path.Combine(tempDir, baseName + ext);
                    if (_saveTempCopyAction != null)
                    {
                        if (_saveTempCopyAction(targetDoc) && File.Exists(targetDoc)) return targetDoc;
                    }
                    if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                    {
                        File.Copy(_currentFilePath, targetDoc, true);
                        return targetDoc;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SendEmail] PrepareAttachmentFile error: " + ex.Message);
            }

            return !string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath) ? _currentFilePath : null;
        }

        private void DispatchEmail()
        {
            string attachmentPath = PrepareAttachmentFile();

            string to = _txtTo.Text.Trim();
            string cc = _txtCc.Text.Trim();
            string subject = _txtSubject.Text.Trim();
            string body = _txtBody.Text.Trim();

            // Nếu có tệp đính kèm, thêm thông tin tệp vào cuối body để người nhận dễ theo dõi
            if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
            {
                try
                {
                    // Copy tệp vào Clipboard để người dùng có thể Ctrl+V ngay vào cửa sổ mail nếu ứng dụng mail yêu cầu kéo thả
                    var files = new System.Collections.Specialized.StringCollection { attachmentPath };
                    Clipboard.SetFileDropList(files);
                }
                catch { }
            }

            // Xây dựng URI mailto chuẩn RFC 2368
            StringBuilder mailto = new StringBuilder("mailto:");
            if (!string.IsNullOrEmpty(to)) mailto.Append(Uri.EscapeDataString(to));

            List<string> queryParams = new List<string>();
            if (!string.IsNullOrEmpty(cc)) queryParams.Add("cc=" + Uri.EscapeDataString(cc));
            if (!string.IsNullOrEmpty(subject)) queryParams.Add("subject=" + Uri.EscapeDataString(subject));
            if (!string.IsNullOrEmpty(body)) queryParams.Add("body=" + Uri.EscapeDataString(body));

            if (queryParams.Count > 0)
            {
                mailto.Append("?").Append(string.Join("&", queryParams));
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = mailto.ToString(),
                    UseShellExecute = true
                };
                Process.Start(psi);

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    MessageBox.Show(this,
                        "Cửa sổ soạn thư đã được mở thành công!\n\nTệp đính kèm đã được chuẩn bị tại:\n" + attachmentPath + "\n(Đã sao chép sẵn vào Clipboard, bạn có thể dán tệp trực tiếp bằng Ctrl+V).",
                        "ZenQ Office Email", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Không thể mở ứng dụng gửi thư mặc định: " + ex.Message + "\n\nTệp đính kèm đã sẵn sàng tại: " + (attachmentPath ?? "Chưa lưu"),
                    "Lỗi gửi thư", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenAttachmentFolder()
        {
            string attachmentPath = PrepareAttachmentFile();
            if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + attachmentPath + "\"",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
            else
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ZenQOffice_MailAttachments");
                if (Directory.Exists(tempDir))
                {
                    Process.Start(new ProcessStartInfo { FileName = tempDir, UseShellExecute = true });
                }
            }
        }
    }
}

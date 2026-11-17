using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class ExtensionPublishDialogForm : Form
    {
        private TextBox _txtName;
        private TextBox _txtVersion;
        private ComboBox _cboCategory;
        private static readonly string[] CategoryKeys = new string[] { "Thiết kế", "Ảnh", "Chữ", "Dữ liệu", "Khác" };
        private TextBox _txtDesc;
        private TextBox _txtFilePath;
        private Button _btnBrowse;
        private Button _btnPublish;
        private Button _btnCancel;
        private Label _lblAuthorInfo;
        private Label _lblStatus;

        public ZenQExtensionItem CreatedItem { get; private set; }
        public string SelectedZqePath { get; private set; }

        public ExtensionPublishDialogForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = AppL10n.T("Đăng tải tiện ích - ZenQ Extension Hub", "Publish Extension - ZenQ Extension Hub");
            this.Size = new Size(520, 510);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            var user = DatabaseAuthService.CurrentUser;
            string authorName = user != null ? (user.FullName ?? user.Username) : AppL10n.T("Chưa đăng nhập", "Not signed in");
            int authorId = user != null ? user.Id : 0;

            int pad = 24;
            int curY = 18;

            Label lblHeader = new Label
            {
                Text = AppL10n.T("Đăng tải Tiện ích lên Cloud", "Publish Extension to Cloud"),
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(pad, curY),
                AutoSize = true
            };
            this.Controls.Add(lblHeader);
            curY += 32;

            _lblAuthorInfo = new Label
            {
                Text = $"👤 {AppL10n.T("Tác giả", "Author")}: {authorName}   |   🆔 ID: #{authorId}   |   {AppL10n.T("Vai trò", "Role")}: {user?.RoleDisplayName ?? AppL10n.T("Thành viên", "Member")}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(79, 70, 229),
                Location = new Point(pad, curY),
                AutoSize = true
            };
            this.Controls.Add(_lblAuthorInfo);
            curY += 28;

            // Separator
            Panel sep = new Panel
            {
                Location = new Point(pad, curY),
                Size = new Size(this.ClientSize.Width - pad * 2, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };
            this.Controls.Add(sep);
            curY += 14;

            // Tên tiện ích
            Label lblName = new Label { Text = AppL10n.T("Tên tiện ích mở rộng:", "Extension Name:"), Location = new Point(pad, curY), AutoSize = true, ForeColor = Color.FromArgb(51, 65, 85) };
            this.Controls.Add(lblName);
            curY += 22;

            _txtName = new TextBox
            {
                Location = new Point(pad, curY),
                Size = new Size(this.ClientSize.Width - pad * 2, 26),
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = AppL10n.T("VD: Chuyển đổi định dạng, Cọ nghệ thuật...", "e.g. Format Converter, Art Brush...")
            };
            this.Controls.Add(_txtName);
            curY += 34;

            // Phiên bản & Danh mục
            int halfW = (this.ClientSize.Width - pad * 2 - 12) / 2;

            Label lblVer = new Label { Text = AppL10n.T("Phiên bản:", "Version:"), Location = new Point(pad, curY), AutoSize = true, ForeColor = Color.FromArgb(51, 65, 85) };
            this.Controls.Add(lblVer);

            Label lblCat = new Label { Text = AppL10n.T("Danh mục:", "Category:"), Location = new Point(pad + halfW + 12, curY), AutoSize = true, ForeColor = Color.FromArgb(51, 65, 85) };
            this.Controls.Add(lblCat);
            curY += 22;

            _txtVersion = new TextBox
            {
                Text = "1.0.0",
                Location = new Point(pad, curY),
                Size = new Size(halfW, 26),
                Font = new Font("Segoe UI", 10f)
            };
            this.Controls.Add(_txtVersion);

            _cboCategory = new ComboBox
            {
                Location = new Point(pad + halfW + 12, curY),
                Size = new Size(halfW, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f)
            };
            _cboCategory.Items.AddRange(new object[] { AppL10n.T("Thiết kế", "Design"), AppL10n.T("Ảnh", "Photo"), AppL10n.T("Chữ", "Text"), AppL10n.T("Dữ liệu", "Data"), AppL10n.T("Khác", "Other") });
            _cboCategory.SelectedIndex = 0;
            this.Controls.Add(_cboCategory);
            curY += 34;

            // Tệp gói tiện ích
            Label lblFile = new Label { Text = AppL10n.T("Tệp tiện ích (tùy chọn - để trống nếu tạo gói cơ bản):", "Extension package file (optional - leave blank for basic package):"), Location = new Point(pad, curY), AutoSize = true, ForeColor = Color.FromArgb(51, 65, 85) };
            this.Controls.Add(lblFile);
            curY += 22;

            _txtFilePath = new TextBox
            {
                Location = new Point(pad, curY),
                Size = new Size(this.ClientSize.Width - pad * 2 - 90, 26),
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = AppL10n.T("Đường dẫn tệp tiện ích trên máy tính...", "Local path to extension package...")
            };
            this.Controls.Add(_txtFilePath);

            _btnBrowse = new Button
            {
                Text = AppL10n.T("Duyệt...", "Browse..."),
                Location = new Point(this.ClientSize.Width - pad - 80, curY - 1),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat
            };
            _btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnBrowse.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = AppL10n.T("Gói tiện ích ZenQ (*.zqe)|*.zqe|Tệp ZIP (*.zip)|*.zip|Tất cả tệp (*.*)|*.*", "ZenQ Extension (*.zqe)|*.zqe|ZIP Archives (*.zip)|*.zip|All Files (*.*)|*.*"),
                    Title = AppL10n.T("Chọn tệp tiện ích mở rộng", "Select Extension Package")
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtFilePath.Text = ofd.FileName;
                    if (string.IsNullOrWhiteSpace(_txtName.Text))
                    {
                        _txtName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                }
            };
            this.Controls.Add(_btnBrowse);
            curY += 34;

            // Mô tả
            Label lblDesc = new Label { Text = AppL10n.T("Mô tả tiện ích:", "Description:"), Location = new Point(pad, curY), AutoSize = true, ForeColor = Color.FromArgb(51, 65, 85) };
            this.Controls.Add(lblDesc);
            curY += 22;

            _txtDesc = new TextBox
            {
                Location = new Point(pad, curY),
                Size = new Size(this.ClientSize.Width - pad * 2, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = AppL10n.T("Giới thiệu ngắn gọn các tính năng nổi bật của tiện ích mở rộng này...", "Briefly describe the key features of this extension...")
            };
            this.Controls.Add(_txtDesc);
            curY += 80;

            // Trạng thái
            _lblStatus = new Label
            {
                Text = "",
                Location = new Point(pad, curY),
                Size = new Size(this.ClientSize.Width - pad * 2, 20),
                ForeColor = Color.FromArgb(220, 38, 38)
            };
            this.Controls.Add(_lblStatus);
            curY += 24;

            // Buttons
            _btnPublish = new Button
            {
                Text = AppL10n.T("✨ Đăng tải", "✨ Publish"),
                Location = new Point(this.ClientSize.Width - pad - 140, curY),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPublish.FlatAppearance.BorderSize = 0;
            _btnPublish.Click += BtnPublish_Click;
            this.Controls.Add(_btnPublish);

            _btnCancel = new Button
            {
                Text = AppL10n.T("Hủy bỏ", "Cancel"),
                Location = new Point(this.ClientSize.Width - pad - 140 - 90, curY),
                Size = new Size(80, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(_btnCancel);
        }

        private async void BtnPublish_Click(object sender, EventArgs e)
        {
            string name = _txtName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                _lblStatus.Text = AppL10n.T("Vui lòng nhập tên tiện ích.", "Please enter an extension name.");
                _txtName.Focus();
                return;
            }

            var user = DatabaseAuthService.CurrentUser;
            if (user == null)
            {
                _lblStatus.Text = AppL10n.T("Vui lòng đăng nhập để thực hiện thao tác.", "Please sign in to publish.");
                return;
            }

            if (user.Ban)
            {
                _lblStatus.Text = AppL10n.T("Tài khoản của bạn đã bị khóa. Không thể đăng tải.", "Your account is locked and cannot publish.");
                return;
            }

            string zqePath = _txtFilePath.Text?.Trim();
            if (!string.IsNullOrEmpty(zqePath) && !File.Exists(zqePath))
            {
                _lblStatus.Text = AppL10n.T("Tệp .zqe không tồn tại.", "The .zqe file does not exist.");
                return;
            }

            _btnPublish.Enabled = false;
            _btnCancel.Enabled = false;
            _btnBrowse.Enabled = false;
            _lblStatus.ForeColor = Color.FromArgb(79, 70, 229);
            _lblStatus.Text = AppL10n.T("Đang đóng gói và đăng tải lên Cloud...", "Packaging and uploading to Cloud...");

            var meta = new ZenQExtensionItem
            {
                id = "zenq.ext." + Guid.NewGuid().ToString("N").Substring(0, 8),
                name = name,
                version = string.IsNullOrWhiteSpace(_txtVersion.Text) ? "1.0.0" : _txtVersion.Text.Trim(),
                category = (_cboCategory.SelectedIndex >= 0 && _cboCategory.SelectedIndex < CategoryKeys.Length) ? CategoryKeys[_cboCategory.SelectedIndex] : CategoryKeys[0],
                description = _txtDesc.Text?.Trim() ?? "",
                authorId = user.Id,
                authorName = user.FullName ?? user.Username,
                actionId = "custom",
                isCustom = true
            };

            var (success, error) = await ZenQExtensionManager.PublishExtensionAsync(meta, zqePath);

            if (success)
            {
                CreatedItem = meta;
                SelectedZqePath = zqePath;
                ZenQOffice.UI.FluentMessageBox.Show(this, string.Format(AppL10n.T("Đã đăng tải thành công tiện ích '{0}' lên ZenQ Extension Hub!", "Successfully published extension '{0}' to ZenQ Extension Hub!"), meta.name), AppL10n.T("Đăng tải thành công", "Published"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _btnPublish.Enabled = true;
                _btnCancel.Enabled = true;
                _btnBrowse.Enabled = true;
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = AppL10n.T("Thất bại: ", "Failed: ") + error;
            }
        }
    }
}

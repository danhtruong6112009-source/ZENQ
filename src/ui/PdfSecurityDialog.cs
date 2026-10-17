using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class PdfSecurityDialog : Form
    {
        public bool IsProtected { get; private set; }
        public string OpenPassword { get; private set; }
        public string PermissionPassword { get; private set; }
        public bool AllowPrinting { get; private set; } = true;
        public bool AllowCopying { get; private set; } = true;
        public bool AllowModifying { get; private set; } = true;
        public int KeyLength { get; private set; } = 128;
        public bool RemoveSecurityRequested { get; private set; } = false;

        private CheckBox chkOpenPass;
        private TextBox txtOpenPass;
        private TextBox txtConfirmOpenPass;

        private CheckBox chkPermPass;
        private TextBox txtPermPass;
        private TextBox txtConfirmPermPass;
        private CheckBox chkPrint;
        private CheckBox chkCopy;
        private CheckBox chkModify;
        private ComboBox cboKeyLength;

        private Button btnApply;
        private Button btnRemove;
        private Button btnCancel;

        public PdfSecurityDialog(PdfSecuritySettings currentSettings = null)
        {
            this.Text = "Bảo mật & Mã hóa PDF (Acrobat Security Standard)";
            this.Size = new Size(540, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildUI(currentSettings);
        }

        private void BuildUI(PdfSecuritySettings settings)
        {
            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(220, 38, 38)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(pnlHeader.ClientRectangle, Color.FromArgb(185, 28, 28), Color.FromArgb(220, 38, 38), LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE72E', new Rectangle(16, 14, 22, 22), Color.White, 18f);
                using (Font titleF = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font subF = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("Thiết lập Mật khẩu & Quyền hạn Bảo mật", titleF, Brushes.White, 44, 12);
                    e.Graphics.DrawString("Mã hóa tài liệu PDF theo chuẩn công nghiệp Acrobat Pro 128-bit / 256-bit AES", subF, Brushes.WhiteSmoke, 44, 38);
                }
            };
            this.Controls.Add(pnlHeader);

            Panel pnlBody = new Panel
            {
                Location = new Point(16, 75),
                Size = new Size(492, 430),
                AutoScroll = true
            };
            this.Controls.Add(pnlBody);

            int curY = 4;

            // Group 1: Mật khẩu mở tệp
            GroupBox grpOpen = new GroupBox
            {
                Text = "Mật khẩu Mở Tài liệu (Document Open Password)",
                Location = new Point(4, curY),
                Size = new Size(480, 115),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpOpen);

            chkOpenPass = new CheckBox
            {
                Text = "Yêu cầu mật khẩu để mở tài liệu",
                Location = new Point(14, 22),
                AutoSize = true,
                Checked = settings != null && settings.IsPasswordProtected && !string.IsNullOrEmpty(settings.UserPasswordHash)
            };
            grpOpen.Controls.Add(chkOpenPass);

            Label lblOpen = new Label { Text = "Mật khẩu mở:", Location = new Point(14, 52), AutoSize = true };
            grpOpen.Controls.Add(lblOpen);

            txtOpenPass = new TextBox
            {
                Location = new Point(120, 49),
                Width = 140,
                PasswordChar = '●',
                Enabled = chkOpenPass.Checked
            };
            grpOpen.Controls.Add(txtOpenPass);

            Label lblConfirmOpen = new Label { Text = "Xác nhận:", Location = new Point(275, 52), AutoSize = true };
            grpOpen.Controls.Add(lblConfirmOpen);

            txtConfirmOpenPass = new TextBox
            {
                Location = new Point(340, 49),
                Width = 125,
                PasswordChar = '●',
                Enabled = chkOpenPass.Checked
            };
            grpOpen.Controls.Add(txtConfirmOpenPass);

            Label lblOpenHint = new Label
            {
                Text = "Người nhận bắt buộc phải nhập mật khẩu này để xem nội dung PDF.",
                Location = new Point(14, 82),
                Size = new Size(450, 22),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.2f)
            };
            grpOpen.Controls.Add(lblOpenHint);

            chkOpenPass.CheckedChanged += (s, e) =>
            {
                txtOpenPass.Enabled = chkOpenPass.Checked;
                txtConfirmOpenPass.Enabled = chkOpenPass.Checked;
                if (chkOpenPass.Checked) txtOpenPass.Focus();
            };

            curY += 125;

            // Group 2: Quyền hạn & Giới hạn thao tác
            GroupBox grpPerm = new GroupBox
            {
                Text = "Quyền hạn & Mật khẩu Quản trị (Permissions & Restrictions)",
                Location = new Point(4, curY),
                Size = new Size(480, 185),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpPerm);

            chkPermPass = new CheckBox
            {
                Text = "Bật mật khẩu quyền hạn để hạn chế in ấn và sao chép",
                Location = new Point(14, 22),
                AutoSize = true,
                Checked = settings != null && (!settings.AllowPrinting || !settings.AllowCopying || !settings.AllowModifying || !string.IsNullOrEmpty(settings.OwnerPasswordHash))
            };
            grpPerm.Controls.Add(chkPermPass);

            Label lblPerm = new Label { Text = "Mật khẩu quyền:", Location = new Point(14, 52), AutoSize = true };
            grpPerm.Controls.Add(lblPerm);

            txtPermPass = new TextBox
            {
                Location = new Point(120, 49),
                Width = 140,
                PasswordChar = '●',
                Enabled = chkPermPass.Checked
            };
            grpPerm.Controls.Add(txtPermPass);

            Label lblConfirmPerm = new Label { Text = "Xác nhận:", Location = new Point(275, 52), AutoSize = true };
            grpPerm.Controls.Add(lblConfirmPerm);

            txtConfirmPermPass = new TextBox
            {
                Location = new Point(340, 49),
                Width = 125,
                PasswordChar = '●',
                Enabled = chkPermPass.Checked
            };
            grpPerm.Controls.Add(txtConfirmPermPass);

            chkPrint = new CheckBox
            {
                Text = "Cho phép In ấn tài liệu (Printing)",
                Location = new Point(14, 82),
                AutoSize = true,
                Checked = settings == null || settings.AllowPrinting,
                Enabled = chkPermPass.Checked
            };
            grpPerm.Controls.Add(chkPrint);

            chkCopy = new CheckBox
            {
                Text = "Cho phép Sao chép nội dung văn bản & hình ảnh (Copying)",
                Location = new Point(14, 110),
                AutoSize = true,
                Checked = settings == null || settings.AllowCopying,
                Enabled = chkPermPass.Checked
            };
            grpPerm.Controls.Add(chkCopy);

            chkModify = new CheckBox
            {
                Text = "Cho phép Chỉnh sửa và thay đổi cấu trúc trang (Modifying)",
                Location = new Point(14, 138),
                AutoSize = true,
                Checked = settings == null || settings.AllowModifying,
                Enabled = chkPermPass.Checked
            };
            grpPerm.Controls.Add(chkModify);

            chkPermPass.CheckedChanged += (s, e) =>
            {
                txtPermPass.Enabled = chkPermPass.Checked;
                txtConfirmPermPass.Enabled = chkPermPass.Checked;
                chkPrint.Enabled = chkPermPass.Checked;
                chkCopy.Enabled = chkPermPass.Checked;
                chkModify.Enabled = chkPermPass.Checked;
                if (chkPermPass.Checked) txtPermPass.Focus();
            };

            curY += 195;

            // Group 3: Chuẩn mã hóa
            GroupBox grpEnc = new GroupBox
            {
                Text = "Mức độ Mã hóa (Encryption Level)",
                Location = new Point(4, curY),
                Size = new Size(480, 70),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(grpEnc);

            Label lblEnc = new Label { Text = "Thuật toán:", Location = new Point(14, 28), AutoSize = true };
            grpEnc.Controls.Add(lblEnc);

            cboKeyLength = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, 24),
                Width = 345
            };
            cboKeyLength.Items.Add("128-bit AES (Chuẩn Adobe Reader / Acrobat 7.0+)");
            cboKeyLength.Items.Add("256-bit AES (Mức độ bảo mật quân sự / Acrobat 9.0+)");
            cboKeyLength.SelectedIndex = (settings != null && settings.EncryptionKeyLength == 256) ? 1 : 0;
            grpEnc.Controls.Add(cboKeyLength);

            // Bottom Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(pnlBottom);

            btnApply = new Button
            {
                Text = "Áp dụng",
                Size = new Size(115, 34),
                Location = new Point(145, 10),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE72E', 13, 13, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;
            pnlBottom.Controls.Add(btnApply);

            bool isCurrentlyProtected = (settings != null && settings.IsPasswordProtected);
            btnRemove = new Button
            {
                Text = "Gỡ Mật khẩu",
                Size = new Size(125, 34),
                Location = new Point(270, 10),
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(30, 58, 138),
                FlatStyle = FlatStyle.Flat,
                Visible = isCurrentlyProtected,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE785', 13, 13, Color.FromArgb(30, 58, 138)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            btnRemove.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnRemove.Click += (s, e) =>
            {
                RemoveSecurityRequested = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlBottom.Controls.Add(btnRemove);

            btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Size = new Size(95, 34),
                Location = new Point(isCurrentlyProtected ? 405 : 270, 10),
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

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (chkOpenPass.Checked)
            {
                if (string.IsNullOrEmpty(txtOpenPass.Text))
                {
                    MessageBox.Show(this, "Vui lòng nhập mật khẩu mở tài liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtOpenPass.Focus();
                    return;
                }
                if (txtOpenPass.Text != txtConfirmOpenPass.Text)
                {
                    MessageBox.Show(this, "Mật khẩu mở tài liệu xác nhận không khớp!", "Lỗi xác nhận", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmOpenPass.Focus();
                    return;
                }
            }

            if (chkPermPass.Checked)
            {
                if (string.IsNullOrEmpty(txtPermPass.Text))
                {
                    MessageBox.Show(this, "Vui lòng nhập mật khẩu quyền hạn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPermPass.Focus();
                    return;
                }
                if (txtPermPass.Text != txtConfirmPermPass.Text)
                {
                    MessageBox.Show(this, "Mật khẩu quyền hạn xác nhận không khớp!", "Lỗi xác nhận", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPermPass.Focus();
                    return;
                }
            }

            if (!chkOpenPass.Checked && !chkPermPass.Checked)
            {
                MessageBox.Show(this, "Vui lòng chọn ít nhất một hình thức bảo mật (Mật khẩu mở hoặc Mật khẩu quyền hạn)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            IsProtected = true;
            OpenPassword = chkOpenPass.Checked ? txtOpenPass.Text : "";
            PermissionPassword = chkPermPass.Checked ? txtPermPass.Text : "";
            AllowPrinting = chkPermPass.Checked ? chkPrint.Checked : true;
            AllowCopying = chkPermPass.Checked ? chkCopy.Checked : true;
            AllowModifying = chkPermPass.Checked ? chkModify.Checked : true;
            KeyLength = (cboKeyLength.SelectedIndex == 1) ? 256 : 128;
            RemoveSecurityRequested = false;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

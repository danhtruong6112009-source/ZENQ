using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class ProtectSheetDialog : Form
    {
        private TextBox txtPassword;
        private CheckBox chkSelectLocked;
        private CheckBox chkSelectUnlocked;
        private CheckBox chkFormatCells;
        private CheckBox chkSort;
        private Button btnOk;
        private Button btnCancel;

        public string EnteredPassword { get { return txtPassword != null ? txtPassword.Text : ""; } }

        public ProtectSheetDialog(bool isAlreadyProtected = false)
        {
            InitializeUI(isAlreadyProtected);
        }

        private void InitializeUI(bool isAlreadyProtected)
        {
            this.Text = isAlreadyProtected ? "Mở Khóa Trang Tính" : "Bảo Vệ Trang Tính";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(380, isAlreadyProtected ? 190 : 310);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(16, 124, 65)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Font hFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 8f, FontStyle.Regular))
                {
                    string hText = isAlreadyProtected ? "🔓 Mở Khóa Trang Tính (Unprotect Sheet)" : "🔒 Bảo Vệ Trang Tính (Protect Sheet)";
                    string sText = isAlreadyProtected ? "Nhập mật khẩu để cho phép chỉnh sửa lại trang tính" : "Bảo vệ trang tính và khóa nội dung ô chống sửa đổi";
                    e.Graphics.DrawString(hText, hFont, Brushes.White, 14, 8);
                    e.Graphics.DrawString(sText, subFont, Brushes.WhiteSmoke, 16, 28);
                }
            };
            this.Controls.Add(pnlHeader);

            int curY = 62;

            Label lblPwd = new Label
            {
                Text = "Mật khẩu (tùy chọn):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, curY),
                AutoSize = true
            };
            this.Controls.Add(lblPwd);
            curY += 22;

            txtPassword = new TextBox
            {
                Location = new Point(16, curY),
                Size = new Size(332, 24),
                PasswordChar = '●',
                Font = new Font("Segoe UI", 10f, FontStyle.Regular)
            };
            this.Controls.Add(txtPassword);
            curY += 32;

            if (!isAlreadyProtected)
            {
                Label lblOptions = new Label
                {
                    Text = "Cho phép người dùng trang tính này:",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Location = new Point(16, curY),
                    AutoSize = true
                };
                this.Controls.Add(lblOptions);
                curY += 20;

                chkSelectLocked = new CheckBox { Text = "Chọn các ô bị khóa", Checked = true, Location = new Point(20, curY), AutoSize = true };
                this.Controls.Add(chkSelectLocked);
                curY += 22;

                chkSelectUnlocked = new CheckBox { Text = "Chọn các ô chưa khóa", Checked = true, Location = new Point(20, curY), AutoSize = true };
                this.Controls.Add(chkSelectUnlocked);
                curY += 22;

                chkFormatCells = new CheckBox { Text = "Định dạng ô & phông chữ", Checked = false, Location = new Point(20, curY), AutoSize = true };
                this.Controls.Add(chkFormatCells);
                curY += 22;

                chkSort = new CheckBox { Text = "Sắp xếp dữ liệu", Checked = false, Location = new Point(20, curY), AutoSize = true };
                this.Controls.Add(chkSort);
                curY += 26;
            }

            // Bottom Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlBottom.Paint += (s, e) => e.Graphics.DrawLine(Pens.LightGray, 0, 0, pnlBottom.Width, 0);

            btnOk = new Button
            {
                Text = "Đồng ý",
                DialogResult = DialogResult.OK,
                Size = new Size(86, 30),
                Location = new Point(176, 8),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            pnlBottom.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Size = new Size(76, 30),
                Location = new Point(272, 8),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            pnlBottom.Controls.Add(btnCancel);

            this.Controls.Add(pnlBottom);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}

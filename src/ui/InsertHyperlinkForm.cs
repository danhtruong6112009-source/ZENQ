using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class InsertHyperlinkForm : Form
    {
        public string DisplayText { get; private set; }
        public string TargetUrl { get; private set; }
        public string ScreenTip { get; private set; }

        private TextBox txtText;
        private TextBox txtUrl;
        private TextBox txtScreenTip;
        private Button btnOk;
        private Button btnCancel;

        public InsertHyperlinkForm(string selectedText = "", string existingUrl = "")
        {
            this.Text = "Chèn Liên kết (Insert Hyperlink)";
            this.Size = new Size(480, 290);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            int curY = 18;

            // 1. Văn bản hiển thị
            Label lblText = new Label { Text = "Văn bản hiển thị:", Location = new Point(20, curY), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblText);
            curY += 22;

            txtText = new TextBox { Text = string.IsNullOrEmpty(selectedText) ? "Nhấp vào đây để xem chi tiết" : selectedText, Location = new Point(20, curY), Width = 425 };
            this.Controls.Add(txtText);
            curY += 34;

            // 2. Địa chỉ Web / Tệp
            Label lblUrl = new Label { Text = "Địa chỉ Web / Đường dẫn (URL):", Location = new Point(20, curY), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblUrl);
            curY += 22;

            txtUrl = new TextBox { Text = string.IsNullOrEmpty(existingUrl) ? "https://" : existingUrl, Location = new Point(20, curY), Width = 425 };
            this.Controls.Add(txtUrl);
            curY += 34;

            // 3. Gợi ý màn hình
            Label lblTip = new Label { Text = "Gợi ý màn hình (ScreenTip - tùy chọn):", Location = new Point(20, curY), AutoSize = true };
            this.Controls.Add(lblTip);
            curY += 22;

            txtScreenTip = new TextBox { Location = new Point(20, curY), Width = 425 };
            this.Controls.Add(txtScreenTip);
            curY += 40;

            // Buttons
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            btnOk = new Button
            {
                Text = "Chèn (OK)",
                Location = new Point(275, 10),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                DisplayText = txtText.Text.Trim();
                TargetUrl = txtUrl.Text.Trim();
                ScreenTip = txtScreenTip.Text.Trim();
                if (string.IsNullOrEmpty(DisplayText)) DisplayText = TargetUrl;
                if (string.IsNullOrEmpty(TargetUrl)) TargetUrl = "https://genoffice.vn";
            };
            bottomPanel.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(368, 10),
                Size = new Size(75, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}

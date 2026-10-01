using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class InsertOnlineVideoForm : Form
    {
        public string VideoTitle { get; private set; }
        public string VideoUrl { get; private set; }

        private TextBox txtTitle;
        private TextBox txtUrl;
        private Button btnOk;
        private Button btnCancel;

        public InsertOnlineVideoForm()
        {
            this.Text = "Chèn Video Trực Tuyến (Insert Online Video)";
            this.Size = new Size(500, 270);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            int curY = 18;

            Label lblHeading = new Label
            {
                Text = "Nhập liên kết hoặc mã nhúng video từ YouTube, Vimeo hoặc trang web:",
                Location = new Point(20, curY),
                Size = new Size(450, 36),
                ForeColor = Color.FromArgb(40, 50, 60)
            };
            this.Controls.Add(lblHeading);
            curY += 40;

            Label lblTitle = new Label { Text = "Tiêu đề video:", Location = new Point(20, curY), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblTitle);
            curY += 22;

            txtTitle = new TextBox { Text = "Video Giới Thiệu ZENQ OFFICE", Location = new Point(20, curY), Width = 445 };
            this.Controls.Add(txtTitle);
            curY += 32;

            Label lblUrl = new Label { Text = "Địa chỉ Video (URL):", Location = new Point(20, curY), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblUrl);
            curY += 22;

            txtUrl = new TextBox { Text = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", Location = new Point(20, curY), Width = 445 };
            this.Controls.Add(txtUrl);
            curY += 36;

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            btnOk = new Button
            {
                Text = "Chèn Video",
                Location = new Point(285, 10),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                VideoTitle = txtTitle.Text.Trim();
                VideoUrl = txtUrl.Text.Trim();
                if (string.IsNullOrEmpty(VideoTitle)) VideoTitle = "Video Trực Tuyến";
                if (string.IsNullOrEmpty(VideoUrl)) VideoUrl = "https://youtube.com";
            };
            bottomPanel.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(388, 10),
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

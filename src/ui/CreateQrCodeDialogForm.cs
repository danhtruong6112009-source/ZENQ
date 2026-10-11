using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class CreateQrCodeDialogForm : Form
    {
        private TextBox txtInput;
        private PictureBox picPreview;
        private Button btnInsert;
        private Button btnCancel;
        public Bitmap GeneratedQrBitmap { get; private set; }
        public string QrTextResult { get; private set; }

        public CreateQrCodeDialogForm(string initialText = "https://zenqoffice.com")
        {
            Text = "Tạo mã QR - Tiện ích mở rộng";
            Size = new Size(420, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            Label lblTitle = new Label
            {
                Text = "Nhập liên kết Website hoặc nội dung văn bản:",
                Location = new Point(24, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55)
            };
            Controls.Add(lblTitle);

            txtInput = new TextBox
            {
                Location = new Point(24, 50),
                Size = new Size(356, 26),
                Text = initialText
            };
            txtInput.TextChanged += (s, e) => UpdatePreview();
            Controls.Add(txtInput);

            picPreview = new PictureBox
            {
                Location = new Point(100, 95),
                Size = new Size(200, 200),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            Controls.Add(picPreview);

            btnInsert = new Button
            {
                Text = "Chèn vào trang chiếu",
                Location = new Point(180, 380),
                Size = new Size(200, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnInsert.FlatAppearance.BorderSize = 0;
            btnInsert.Click += (s, e) =>
            {
                QrTextResult = txtInput.Text.Trim();
                GeneratedQrBitmap = QrCodeGenerator.GenerateQrBitmap(QrTextResult, 400);
            };
            Controls.Add(btnInsert);

            btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(90, 380),
                Size = new Size(80, 36),
                BackColor = Color.FromArgb(229, 231, 235),
                ForeColor = Color.FromArgb(55, 65, 81),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            Controls.Add(btnCancel);

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            string txt = string.IsNullOrEmpty(txtInput.Text) ? "https://zenqoffice.com" : txtInput.Text;
            var old = picPreview.Image;
            picPreview.Image = QrCodeGenerator.GenerateQrBitmap(txt, 200);
            if (old != null) old.Dispose();
        }
    }
}

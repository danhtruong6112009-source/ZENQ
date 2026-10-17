using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public enum ImageImportMode
    {
        Embed, // Nhúng trực tiếp vào tệp SVG
        Link   // Liên kết đến tệp bên ngoài
    }

    public enum ImageSizeMode
    {
        FitCanvas,
        Original1to1
    }

    /// <summary>
    /// Hộp thoại Chèn Hình Ảnh vào Tài liệu Đồ Họa Vector chuẩn Inkscape / SVG.
    /// Cho phép người dùng tùy chọn: Nhúng ảnh trực tiếp (Embed) hoặc Liên kết tệp ngoài (Link),
    /// kèm chế độ tự động co giãn vừa khung vẽ hoặc giữ nguyên tỷ lệ 1:1.
    /// </summary>
    public class PhotoInsertImageDialogForm : Form
    {
        public ImageImportMode ImportMode { get; private set; } = ImageImportMode.Embed;
        public ImageSizeMode SizeMode { get; private set; } = ImageSizeMode.FitCanvas;
        public bool LockAspectRatio { get; private set; } = true;

        private RadioButton rbEmbed;
        private RadioButton rbLink;
        private RadioButton rbFit;
        private RadioButton rbOriginal;
        private CheckBox chkLockRatio;
        private PictureBox picPreview;
        private Label lblInfo;

        public PhotoInsertImageDialogForm(string imagePath, Image img, int canvasW, int canvasH)
        {
            InitializeComponent(imagePath, img, canvasW, canvasH);
        }

        private void InitializeComponent(string imagePath, Image img, int canvasW, int canvasH)
        {
            this.Text = "Nhúng hình ảnh vào tệp Đồ họa Vector (Chuẩn Inkscape)";
            this.Size = new Size(580, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // 1. Header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 12, 20, 10)
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "🖼️ Chèn hình ảnh vào không gian Vector SVG",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(18, 12)
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "Tùy chọn phương thức lưu trữ hình ảnh và kích thước hiển thị trên khung vẽ.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(20, 36)
            };
            pnlHeader.Controls.Add(lblSub);

            Panel divHeader = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlHeader.Controls.Add(divHeader);

            // 2. Bottom Action Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 12, 16, 12)
            };
            this.Controls.Add(pnlBottom);

            Panel divBottom = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlBottom.Controls.Add(divBottom);

            Button btnOK = new Button
            {
                Text = "Đồng ý (Chèn ảnh)",
                Size = new Size(150, 34),
                BackColor = Color.FromArgb(99, 102, 241), // Indigo 500
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Location = new Point(this.ClientSize.Width - 170, 13);
            btnOK.Click += (s, e) =>
            {
                ImportMode = rbEmbed.Checked ? ImageImportMode.Embed : ImageImportMode.Link;
                SizeMode = rbFit.Checked ? ImageSizeMode.FitCanvas : ImageSizeMode.Original1to1;
                LockAspectRatio = chkLockRatio.Checked;
            };
            pnlBottom.Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Size = new Size(95, 34),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Location = new Point(btnOK.Left - 105, 13);
            pnlBottom.Controls.Add(btnCancel);

            // 3. Main Body
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 10),
                AutoScroll = true
            };
            this.Controls.Add(pnlBody);

            // Preview Thumbnail & Info Card
            Panel pnlCard = new Panel
            {
                Location = new Point(20, 12),
                Size = new Size(520, 90),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };
            pnlBody.Controls.Add(pnlCard);

            picPreview = new PictureBox
            {
                Location = new Point(12, 10),
                Size = new Size(70, 70),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Image = img
            };
            pnlCard.Controls.Add(picPreview);

            string fileName = Path.GetFileName(imagePath);
            long fileBytes = 0;
            try { fileBytes = new FileInfo(imagePath).Length; } catch { }
            string sizeStr = fileBytes > 1024 * 1024 ? $"{fileBytes / (1024f * 1024f):0.##} MB" : $"{fileBytes / 1024f:0.##} KB";

            lblInfo = new Label
            {
                Text = $"Tệp: {fileName}\nĐộ phân giải gốc: {img?.Width ?? 0} × {img?.Height ?? 0} px\nDung lượng: {sizeStr}  |  Khung vẽ hiện tại: {canvasW} × {canvasH} px",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true,
                Location = new Point(94, 16)
            };
            pnlCard.Controls.Add(lblInfo);

            // Group 1: Chế độ nạp ảnh (Inkscape: Embed vs Link)
            GroupBox grpMode = new GroupBox
            {
                Text = "Phương thức lưu ảnh trong tài liệu Vector",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 114),
                Size = new Size(520, 130)
            };
            pnlBody.Controls.Add(grpMode);

            rbEmbed = new RadioButton
            {
                Text = "Nhúng ảnh trực tiếp vào tài liệu (Embed image) - [Khuyên dùng]",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 24),
                Size = new Size(480, 24),
                Checked = true
            };
            grpMode.Controls.Add(rbEmbed);

            Label lblEmbedDesc = new Label
            {
                Text = "Dữ liệu hình ảnh được nhúng trực tiếp vào tệp SVG/ZenQ. Tài liệu tự chứa đầy đủ dữ liệu ảnh, chia sẻ hoặc mang sang máy khác không bao giờ bị mất.",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(34, 48),
                Size = new Size(470, 30)
            };
            grpMode.Controls.Add(lblEmbedDesc);

            rbLink = new RadioButton
            {
                Text = "Liên kết đến tệp ảnh bên ngoài (Link image to external file)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(16, 80),
                Size = new Size(480, 22)
            };
            grpMode.Controls.Add(rbLink);

            Label lblLinkDesc = new Label
            {
                Text = "Chỉ lưu đường dẫn liên kết đến tệp trên máy tính. Kích thước file tài liệu nhỏ hơn nhưng cần giữ tệp ảnh gốc.",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(34, 102),
                Size = new Size(470, 24)
            };
            grpMode.Controls.Add(lblLinkDesc);

            // Group 2: Kích thước & Tỷ lệ hiển thị
            GroupBox grpSize = new GroupBox
            {
                Text = "Kích thước & Tỷ lệ khi đưa vào khung vẽ",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 254),
                Size = new Size(520, 115)
            };
            pnlBody.Controls.Add(grpSize);

            rbFit = new RadioButton
            {
                Text = "Tự động co giãn vừa vặn khung vẽ (Fit to canvas) - Giữ gọn gàng",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 24),
                Size = new Size(480, 22),
                Checked = true
            };
            grpSize.Controls.Add(rbFit);

            rbOriginal = new RadioButton
            {
                Text = $"Giữ kích thước pixel gốc ({img?.Width ?? 0} × {img?.Height ?? 0} px, 1:1)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 50),
                Size = new Size(480, 22)
            };
            grpSize.Controls.Add(rbOriginal);

            chkLockRatio = new CheckBox
            {
                Text = "🔒 Khóa tỷ lệ khung hình khi kéo dãn (Lock Aspect Ratio)",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(16, 80),
                Size = new Size(480, 22),
                Checked = true
            };
            grpSize.Controls.Add(chkLockRatio);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}

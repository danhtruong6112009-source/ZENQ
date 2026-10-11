using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class CustomWatermarkDialogForm : Form
    {
        public Action<string, bool, Color, bool> OnApplyTextWatermark; // text, isDiagonal, color, isSemitransparent
        public Action<Image, float, bool> OnApplyPictureWatermark; // image, scale (0 = auto), washout
        public Action OnRemoveWatermark;

        private Image SelectedPictureImage = null;
        private string SelectedPicturePath = null;

        private RadioButton rbNoWatermark;
        private RadioButton rbPictureWatermark;
        private Button btnSelectPicture;
        private ComboBox cbScale;
        private CheckBox chkWashout;

        private RadioButton rbTextWatermark;
        private ComboBox cbLanguage;
        private ComboBox cbText;
        private ComboBox cbFont;
        private ComboBox cbSize;
        private Button btnColor;
        private CheckBox chkSemitransparent;
        private RadioButton rbDiagonal;
        private RadioButton rbHorizontal;

        private Button btnApply;
        private Button btnOK;
        private Button btnCancel;

        private Color SelectedColor = Color.FromArgb(160, 160, 160);

        public CustomWatermarkDialogForm(string currentText, bool currentDiagonal)
        {
            this.Text = "In Hình mờ (Printed Watermark)";
            this.ClientSize = new Size(530, 430);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.DoubleBuffered = true;

            InitControls(currentText, currentDiagonal);
        }

        private void InitControls(string currentText, bool currentDiagonal)
        {
            // 1. Radio: No Watermark
            rbNoWatermark = new RadioButton
            {
                Text = "Không có hình mờ (No watermark)",
                Location = new Point(24, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                Checked = string.IsNullOrEmpty(currentText)
            };
            rbNoWatermark.CheckedChanged += (s, e) => UpdateEnabledStates();
            this.Controls.Add(rbNoWatermark);

            // 2. Radio: Picture Watermark
            rbPictureWatermark = new RadioButton
            {
                Text = "Hình ảnh mờ (Picture watermark)",
                Location = new Point(24, 46),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                Checked = false
            };
            rbPictureWatermark.CheckedChanged += (s, e) => UpdateEnabledStates();
            this.Controls.Add(rbPictureWatermark);

            btnSelectPicture = new Button
            {
                Text = "Chọn hình ảnh (Select Picture)...",
                Location = new Point(48, 72),
                Size = new Size(200, 26),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 8.8f)
            };
            btnSelectPicture.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Tệp hình ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            SelectedPictureImage = Image.FromFile(ofd.FileName);
                            SelectedPicturePath = ofd.FileName;
                            btnSelectPicture.Text = System.IO.Path.GetFileName(ofd.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Không thể nạp tệp hình ảnh: " + ex.Message, "Lỗi hình ảnh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            this.Controls.Add(btnSelectPicture);

            Label lblScale = new Label { Text = "Tỉ lệ (Scale):", Location = new Point(48, 106), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblScale);

            cbScale = new ComboBox
            {
                Location = new Point(140, 103),
                Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cbScale.Items.AddRange(new object[] { "Tự động (Auto)", "500%", "200%", "150%", "100%", "50%" });
            cbScale.SelectedIndex = 0;
            this.Controls.Add(cbScale);

            chkWashout = new CheckBox
            {
                Text = "Làm mờ (Washout)",
                Location = new Point(290, 105),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Checked = true
            };
            this.Controls.Add(chkWashout);

            // 3. Radio: Text Watermark
            rbTextWatermark = new RadioButton
            {
                Text = "Chữ hình mờ (Text watermark)",
                Location = new Point(24, 140),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                Checked = !string.IsNullOrEmpty(currentText)
            };
            rbTextWatermark.CheckedChanged += (s, e) => UpdateEnabledStates();
            this.Controls.Add(rbTextWatermark);

            // Language
            Label lblLang = new Label { Text = "Ngôn ngữ (Language):", Location = new Point(48, 171), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblLang);

            cbLanguage = new ComboBox { Location = new Point(185, 168), Size = new Size(305, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cbLanguage.Items.AddRange(new object[] { "Tiếng Việt (Việt Nam)", "English (United States)" });
            cbLanguage.SelectedIndex = 0;
            this.Controls.Add(cbLanguage);

            // Text
            Label lblText = new Label { Text = "Văn bản (Text):", Location = new Point(48, 203), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblText);

            cbText = new ComboBox { Location = new Point(185, 200), Size = new Size(305, 24), Font = new Font("Segoe UI", 9) };
            cbText.Items.AddRange(new object[] { "BẢO MẬT", "KHÔNG SAO CHÉP", "BẢN THẢO", "TUYỆT MẬT", "CONFIDENTIAL", "DO NOT COPY", "DRAFT", "ORIGINAL", "SAMPLE", "ASAP" });
            cbText.Text = !string.IsNullOrEmpty(currentText) ? currentText : "BẢO MẬT";
            this.Controls.Add(cbText);

            // Font
            Label lblFont = new Label { Text = "Phông chữ (Font):", Location = new Point(48, 235), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblFont);

            cbFont = new ComboBox { Location = new Point(185, 232), Size = new Size(305, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cbFont.Items.AddRange(new object[] { "Calibri", "Arial", "Times New Roman", "Segoe UI", "Tahoma", "Verdana" });
            cbFont.SelectedIndex = 1; // Arial
            this.Controls.Add(cbFont);

            // Size
            Label lblSize = new Label { Text = "Cỡ chữ (Size):", Location = new Point(48, 267), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblSize);

            cbSize = new ComboBox { Location = new Point(185, 264), Size = new Size(130, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cbSize.Items.AddRange(new object[] { "Tự động (Auto)", "36", "44", "48", "54", "66", "72", "80", "96" });
            cbSize.SelectedIndex = 0;
            this.Controls.Add(cbSize);

            // Color & Semitransparent
            Label lblColor = new Label { Text = "Màu sắc (Color):", Location = new Point(48, 299), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblColor);

            btnColor = new Button
            {
                Text = "Tự động (Xám)",
                Location = new Point(185, 296),
                Size = new Size(130, 26),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 8.8f)
            };
            btnColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = SelectedColor;
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        SelectedColor = cd.Color;
                        btnColor.Text = "Đã chọn màu";
                    }
                }
            };
            this.Controls.Add(btnColor);

            chkSemitransparent = new CheckBox
            {
                Text = "Bán trong suốt (Semitransparent)",
                Location = new Point(325, 299),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                Checked = true
            };
            this.Controls.Add(chkSemitransparent);

            // Layout (Diagonal / Horizontal)
            Label lblLayout = new Label { Text = "Bố cục (Layout):", Location = new Point(48, 332), AutoSize = true, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblLayout);

            rbDiagonal = new RadioButton
            {
                Text = "Chéo (Diagonal)",
                Location = new Point(185, 330),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Checked = currentDiagonal
            };
            this.Controls.Add(rbDiagonal);

            rbHorizontal = new RadioButton
            {
                Text = "Ngang (Horizontal)",
                Location = new Point(320, 330),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Checked = !currentDiagonal
            };
            this.Controls.Add(rbHorizontal);

            // Action Buttons (Apply, OK, Cancel) placed with clean vertical clearance at Y = 380
            btnApply = new Button
            {
                Text = "Áp dụng (Apply)",
                Location = new Point(205, 380),
                Size = new Size(110, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 9)
            };
            btnApply.Click += (s, e) => ApplySettings();
            this.Controls.Add(btnApply);

            btnOK = new Button
            {
                Text = "Đồng ý (OK)",
                Location = new Point(325, 380),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(43, 87, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnOK.Click += (s, e) =>
            {
                ApplySettings();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(435, 380),
                Size = new Size(80, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 9),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            UpdateEnabledStates();
        }

        private void UpdateEnabledStates()
        {
            if (rbPictureWatermark == null || rbTextWatermark == null) return;

            bool isPic = rbPictureWatermark.Checked;
            if (btnSelectPicture != null) btnSelectPicture.Enabled = isPic;
            if (cbScale != null) cbScale.Enabled = isPic;
            if (chkWashout != null) chkWashout.Enabled = isPic;

            bool isTxt = rbTextWatermark.Checked;
            if (cbLanguage != null) cbLanguage.Enabled = isTxt;
            if (cbText != null) cbText.Enabled = isTxt;
            if (cbFont != null) cbFont.Enabled = isTxt;
            if (cbSize != null) cbSize.Enabled = isTxt;
            if (btnColor != null) btnColor.Enabled = isTxt;
            if (chkSemitransparent != null) chkSemitransparent.Enabled = isTxt;
            if (rbDiagonal != null) rbDiagonal.Enabled = isTxt;
            if (rbHorizontal != null) rbHorizontal.Enabled = isTxt;
        }

        private void ApplySettings()
        {
            if (rbNoWatermark != null && rbNoWatermark.Checked)
            {
                if (OnRemoveWatermark != null) OnRemoveWatermark();
            }
            else if (rbTextWatermark != null && rbTextWatermark.Checked)
            {
                string txt = (cbText != null && cbText.Text != null) ? cbText.Text.Trim() : "BẢO MẬT";
                bool isDiag = (rbDiagonal != null) ? rbDiagonal.Checked : true;
                bool isSemi = (chkSemitransparent != null) ? chkSemitransparent.Checked : true;
                if (OnApplyTextWatermark != null)
                {
                    OnApplyTextWatermark(txt, isDiag, SelectedColor, isSemi);
                }
            }
            else if (rbPictureWatermark != null && rbPictureWatermark.Checked)
            {
                if (SelectedPictureImage != null && OnApplyPictureWatermark != null)
                {
                    float scale = 0f; // Auto
                    if (cbScale != null)
                    {
                        if (cbScale.SelectedIndex == 1) scale = 5.0f;
                        else if (cbScale.SelectedIndex == 2) scale = 2.0f;
                        else if (cbScale.SelectedIndex == 3) scale = 1.5f;
                        else if (cbScale.SelectedIndex == 4) scale = 1.0f;
                        else if (cbScale.SelectedIndex == 5) scale = 0.5f;
                    }
                    bool washout = (chkWashout != null) ? chkWashout.Checked : true;
                    OnApplyPictureWatermark(SelectedPictureImage, scale, washout);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (this.Owner != null && !this.Owner.IsDisposed)
            {
                try
                {
                    this.Owner.Activate();
                    this.Owner.BringToFront();
                }
                catch { }
            }
        }
    }
}

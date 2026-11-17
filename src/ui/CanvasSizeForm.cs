using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class CanvasSizeForm : Form
    {
        public int CanvasWidth { get; private set; }
        public int CanvasHeight { get; private set; }
        public int NewWidth => CanvasWidth;
        public int NewHeight => CanvasHeight;
        public bool CenterContent { get; private set; } = true;

        private NumericUpDown numWidth;
        private NumericUpDown numHeight;
        private ComboBox cboPresets;
        private CheckBox chkCenter;

        public CanvasSizeForm(int currentWidth, int currentHeight)
        {
            CanvasWidth = currentWidth;
            CanvasHeight = currentHeight;

            this.Text = AppL10n.T("Kích Thước Khung Vẽ (Canvas Size)", "Canvas Size");
            this.Size = new Size(420, 310);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f);

            // Title
            Label lblHeader = new Label
            {
                Text = AppL10n.T("Thay đổi kích thước Khung vẽ thiết kế", "Resize Design Canvas"),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 16),
                AutoSize = true
            };
            this.Controls.Add(lblHeader);

            // Preset Label & ComboBox
            Label lblPreset = new Label
            {
                Text = AppL10n.T("Khung mẫu sẵn:", "Preset:"),
                Location = new Point(20, 52),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(lblPreset);

            cboPresets = new ZenQOffice.UI.FluentComboBox
            {
                Location = new Point(150, 48),
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboPresets.Items.AddRange(new object[]
            {
                AppL10n.T("Tùy chỉnh", "Custom"),
                "1920 × 1080 px (Full HD 16:9)",
                AppL10n.T("1080 × 1080 px (Vuông 1:1 - Post)", "1080 × 1080 px (Square 1:1 - Post)"),
                "1280 × 720 px (HD 16:9)",
                "3840 × 2160 px (4K UHD)",
                "1080 × 1920 px (Story / Reel 9:16)",
                "1200 × 630 px (Facebook Share)",
                AppL10n.T("2480 × 3508 px (Khổ A4 In ấn 300 DPI)", "2480 × 3508 px (A4 Print 300 DPI)")
            });
            cboPresets.SelectedIndex = 0;
            cboPresets.SelectedIndexChanged += CboPresets_SelectedIndexChanged;
            this.Controls.Add(cboPresets);

            // Width
            Label lblW = new Label
            {
                Text = AppL10n.T("Chiều rộng (px):", "Width (px):"),
                Location = new Point(20, 92),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(lblW);

            numWidth = new NumericUpDown
            {
                Location = new Point(150, 88),
                Width = 140,
                Minimum = 50,
                Maximum = 16000,
                Value = Math.Max(50, Math.Min(16000, currentWidth))
            };
            this.Controls.Add(numWidth);

            // Height
            Label lblH = new Label
            {
                Text = AppL10n.T("Chiều cao (px):", "Height (px):"),
                Location = new Point(20, 130),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(lblH);

            numHeight = new NumericUpDown
            {
                Location = new Point(150, 126),
                Width = 140,
                Minimum = 50,
                Maximum = 16000,
                Value = Math.Max(50, Math.Min(16000, currentHeight))
            };
            this.Controls.Add(numHeight);

            // Center Content Checkbox
            chkCenter = new CheckBox
            {
                Text = AppL10n.T("Căn giữa các đối tượng vào khung mới", "Center existing objects on canvas"),
                Location = new Point(22, 168),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            this.Controls.Add(chkCenter);

            // Action Buttons
            Button btnCancel = new SettingsActionButton
            {
                Text = AppL10n.T("Hủy bỏ", "Cancel"),
                Location = new Point(290, 220),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(241, 245, 249),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;

            Button btnOk = new SettingsActionButton
            {
                Text = AppL10n.T("Đồng ý", "OK"),
                Location = new Point(190, 220),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                CanvasWidth = (int)numWidth.Value;
                CanvasHeight = (int)numHeight.Value;
                CenterContent = chkCenter.Checked;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;
        }

        private void CboPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboPresets.SelectedIndex)
            {
                case 1: numWidth.Value = 1920; numHeight.Value = 1080; break;
                case 2: numWidth.Value = 1080; numHeight.Value = 1080; break;
                case 3: numWidth.Value = 1280; numHeight.Value = 720; break;
                case 4: numWidth.Value = 3840; numHeight.Value = 2160; break;
                case 5: numWidth.Value = 1080; numHeight.Value = 1920; break;
                case 6: numWidth.Value = 1200; numHeight.Value = 630; break;
                case 7: numWidth.Value = 2480; numHeight.Value = 3508; break;
            }
        }
    }
}

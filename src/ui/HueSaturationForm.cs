using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class HueSaturationForm : Form
    {
        public float HueShift { get; private set; } = 0.0f;
        public float SaturationScale { get; private set; } = 1.0f;
        public float SaturationShift => SaturationScale;
        public float LightnessShift { get; private set; } = 0.0f;

        private TrackBar tbSaturation;
        private TrackBar tbLightness;
        private Label lblSatVal;
        private Label lblLightVal;

        public HueSaturationForm()
        {
            this.Text = "Cân Bằng Màu Sắc & Sắc Độ (Hue / Saturation)";
            this.Size = new Size(420, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f);

            // Header
            Label lblHeader = new Label
            {
                Text = "Điều chỉnh độ bão hòa và độ sáng ảnh",
                Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 16),
                AutoSize = true
            };
            this.Controls.Add(lblHeader);

            // Saturation Trackbar
            Label lblSat = new Label
            {
                Text = "Độ bão hòa (Saturation):",
                Location = new Point(20, 56),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(lblSat);

            lblSatVal = new Label
            {
                Text = "100%",
                Location = new Point(345, 56),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            this.Controls.Add(lblSatVal);

            tbSaturation = new TrackBar
            {
                Location = new Point(20, 80),
                Width = 360,
                Minimum = 0,
                Maximum = 200,
                Value = 100,
                TickFrequency = 20
            };
            tbSaturation.ValueChanged += (s, e) =>
            {
                lblSatVal.Text = tbSaturation.Value + "%";
                SaturationScale = tbSaturation.Value / 100.0f;
            };
            this.Controls.Add(tbSaturation);

            // Lightness Trackbar
            Label lblLight = new Label
            {
                Text = "Độ sáng (Lightness):",
                Location = new Point(20, 126),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(lblLight);

            lblLightVal = new Label
            {
                Text = "0",
                Location = new Point(345, 126),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            this.Controls.Add(lblLightVal);

            tbLightness = new TrackBar
            {
                Location = new Point(20, 150),
                Width = 360,
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 20
            };
            tbLightness.ValueChanged += (s, e) =>
            {
                lblLightVal.Text = (tbLightness.Value > 0 ? "+" : "") + tbLightness.Value;
                LightnessShift = tbLightness.Value / 255.0f;
            };
            this.Controls.Add(tbLightness);

            // Buttons
            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(290, 185),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(241, 245, 249),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;

            Button btnOk = new Button
            {
                Text = "Áp dụng",
                Location = new Point(190, 185),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                SaturationScale = tbSaturation.Value / 100.0f;
                LightnessShift = tbLightness.Value / 255.0f;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;
        }
    }
}

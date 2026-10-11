using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class CreateSparklineForm : Form
    {
        public string SparklineType { get; private set; }
        public string DataRangeText { get; private set; }
        public string LocationRangeText { get; private set; }

        private ComboBox cboType;
        private TextBox txtDataRange;
        private TextBox txtLocationRange;

        public CreateSparklineForm(string defaultType, string defaultDataRange, string defaultLocRange)
        {
            SparklineType = defaultType;
            this.Text = "Tạo Đồ Thị Nhỏ (Create Sparklines)";
            this.Size = new Size(460, 260);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI(defaultType, defaultDataRange, defaultLocRange);
        }

        private void BuildUI(string defaultType, string defaultDataRange, string defaultLocRange)
        {
            Label lblType = new Label { Text = "Loại đồ thị nhỏ (Type):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblType);

            cboType = new ComboBox { Location = new Point(180, 17), Size = new Size(235, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboType.Items.AddRange(new object[] { "Đường (Line)", "Cột (Column)", "Thắng/Thua (Win/Loss)" });
            if (defaultType == "Column") cboType.SelectedIndex = 1;
            else if (defaultType == "WinLoss") cboType.SelectedIndex = 2;
            else cboType.SelectedIndex = 0;
            this.Controls.Add(cboType);

            Label lblData = new Label { Text = "Vùng dữ liệu (Data Range):", Location = new Point(20, 60), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblData);

            txtDataRange = new TextBox { Location = new Point(180, 57), Size = new Size(235, 24), Text = defaultDataRange ?? "A1:D1" };
            this.Controls.Add(txtDataRange);

            Label lblLoc = new Label { Text = "Vị trí đặt (Location Range):", Location = new Point(20, 100), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblLoc);

            txtLocationRange = new TextBox { Location = new Point(180, 97), Size = new Size(235, 24), Text = defaultLocRange ?? "E1" };
            this.Controls.Add(txtLocationRange);

            // Bottom Buttons
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOK = new Button
            {
                Text = "Đồng ý",
                Location = new Point(230, 10),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) =>
            {
                if (cboType.SelectedIndex == 1) SparklineType = "Column";
                else if (cboType.SelectedIndex == 2) SparklineType = "WinLoss";
                else SparklineType = "Line";

                DataRangeText = txtDataRange.Text.Trim();
                LocationRangeText = txtLocationRange.Text.Trim();
            };
            bottomPanel.Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(335, 10),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}

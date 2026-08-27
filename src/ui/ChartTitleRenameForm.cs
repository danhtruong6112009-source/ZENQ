using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ChartTitleRenameForm : Form
    {
        private TextBox txtTitle;
        private CheckBox chkShowTitle;
        private Button btnOk;
        private Button btnCancel;
        private DocChartData _chartData;
        public Action<string, bool> OnTitleChanged;

        public ChartTitleRenameForm(DocChartData chartData, Point? screenPos = null)
        {
            _chartData = chartData;
            InitializeUI(screenPos);
        }

        private void InitializeUI(Point? screenPos)
        {
            this.Text = "Đổi Tiêu Đề Biểu Đồ";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = screenPos.HasValue ? FormStartPosition.Manual : FormStartPosition.CenterParent;
            if (screenPos.HasValue)
            {
                this.Location = screenPos.Value;
            }
            this.Size = new Size(420, 260);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.FromArgb(14, 116, 144)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Font hFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("📊 Đổi Tiêu Đề Biểu Đồ", hFont, Brushes.White, 16, 10);
                    e.Graphics.DrawString("Nhập tên hoặc chỉnh sửa tiêu đề hiển thị trên biểu đồ", subFont, Brushes.WhiteSmoke, 18, 32);
                }
            };
            this.Controls.Add(pnlHeader);

            // Body Panel
            Panel pnlBody = new Panel
            {
                Location = new Point(16, 66),
                Size = new Size(372, 110),
                BackColor = Color.Transparent
            };

            Label lblTitle = new Label
            {
                Text = "Tên / Tiêu đề biểu đồ:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(0, 4),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblTitle);

            txtTitle = new TextBox
            {
                Location = new Point(0, 26),
                Size = new Size(370, 26),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                Text = _chartData != null ? (_chartData.Title ?? "") : "Tiêu đề Biểu đồ"
            };
            txtTitle.SelectAll();
            pnlBody.Controls.Add(txtTitle);

            chkShowTitle = new CheckBox
            {
                Text = "Hiển thị tiêu đề trên biểu đồ",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(2, 60),
                AutoSize = true,
                Checked = _chartData != null ? _chartData.ShowTitle : true
            };
            pnlBody.Controls.Add(chkShowTitle);

            // Quick Preset Tags
            Label lblPresets = new Label
            {
                Text = "Gợi ý nhanh:",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(2, 86),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblPresets);

            string[] presets = new string[] { "Doanh Thu", "Tăng Trưởng", "Báo Cáo", "Thị Phần" };
            int tagX = 80;
            foreach (var preset in presets)
            {
                LinkLabel lnk = new LinkLabel
                {
                    Text = preset,
                    Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                    LinkColor = Color.FromArgb(14, 116, 144),
                    Location = new Point(tagX, 86),
                    AutoSize = true
                };
                string pText = preset;
                lnk.LinkClicked += (s, e) =>
                {
                    txtTitle.Text = pText;
                    txtTitle.Focus();
                    txtTitle.SelectAll();
                };
                pnlBody.Controls.Add(lnk);
                tagX += preset.Length * 7 + 22;
            }

            this.Controls.Add(pnlBody);

            // Bottom Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlBottom.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(Pens.LightGray, 0, 0, pnlBottom.Width, 0);
            };

            btnOk = new Button
            {
                Text = "Áp dụng",
                DialogResult = DialogResult.OK,
                Size = new Size(95, 32),
                Location = new Point(200, 10),
                BackColor = Color.FromArgb(14, 116, 144),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                string newTitle = txtTitle.Text.Trim();
                bool showTitle = chkShowTitle.Checked;
                if (_chartData != null)
                {
                    _chartData.Title = newTitle;
                    _chartData.ShowTitle = showTitle;
                }
                if (OnTitleChanged != null) OnTitleChanged(newTitle, showTitle);
                this.Close();
            };
            pnlBottom.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 32),
                Location = new Point(305, 10),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => { this.Close(); };
            pnlBottom.Controls.Add(btnCancel);

            this.Controls.Add(pnlBottom);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}

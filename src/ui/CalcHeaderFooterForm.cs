using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class CalcHeaderFooterForm : Form
    {
        private CalcView _calcView;
        private TextBox txtHeaderLeft;
        private TextBox txtHeaderCenter;
        private TextBox txtHeaderRight;
        private TextBox txtFooterLeft;
        private TextBox txtFooterCenter;
        private TextBox txtFooterRight;

        public CalcHeaderFooterForm(CalcView calcView)
        {
            _calcView = calcView;
            this.Text = "Thiết Lập Đầu Trang & Chân Trang (Header & Footer)";
            this.Size = new Size(560, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            // Header Group
            GroupBox grpHeader = new GroupBox
            {
                Text = "Đầu trang (Header):",
                Location = new Point(20, 15),
                Size = new Size(505, 125),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(grpHeader);

            Label lblHL = new Label { Text = "Phần bên trái:", Location = new Point(15, 25), Size = new Size(150, 18), Font = new Font("Segoe UI", 8.5f) };
            Label lblHC = new Label { Text = "Phần ở giữa:", Location = new Point(175, 25), Size = new Size(150, 18), Font = new Font("Segoe UI", 8.5f) };
            Label lblHR = new Label { Text = "Phần bên phải:", Location = new Point(335, 25), Size = new Size(150, 18), Font = new Font("Segoe UI", 8.5f) };
            grpHeader.Controls.AddRange(new Control[] { lblHL, lblHC, lblHR });

            txtHeaderLeft = new TextBox { Location = new Point(15, 48), Size = new Size(150, 60), Multiline = true, Font = new Font("Segoe UI", 9.0f) };
            txtHeaderCenter = new TextBox { Location = new Point(175, 48), Size = new Size(150, 60), Multiline = true, Font = new Font("Segoe UI", 9.0f) };
            txtHeaderRight = new TextBox { Location = new Point(335, 48), Size = new Size(150, 60), Multiline = true, Font = new Font("Segoe UI", 9.0f) };
            grpHeader.Controls.AddRange(new Control[] { txtHeaderLeft, txtHeaderCenter, txtHeaderRight });

            // Footer Group
            GroupBox grpFooter = new GroupBox
            {
                Text = "Chân trang (Footer):",
                Location = new Point(20, 155),
                Size = new Size(505, 125),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(grpFooter);

            Label lblFL = new Label { Text = "Phần bên trái:", Location = new Point(15, 25), Size = new Size(150, 18), Font = new Font("Segoe UI", 8.5f) };
            Label lblFC = new Label { Text = "Phần ở giữa:", Location = new Point(175, 25), Size = new Size(150, 18), Font = new Font("Segoe UI", 8.5f) };
            Label lblFR = new Label { Text = "Phần bên phải:", Location = new Point(335, 25), Size = new Size(150, 18), Font = new Font("Segoe UI", 8.5f) };
            grpFooter.Controls.AddRange(new Control[] { lblFL, lblFC, lblFR });

            txtFooterLeft = new TextBox { Location = new Point(15, 48), Size = new Size(150, 60), Multiline = true, Font = new Font("Segoe UI", 9.0f) };
            txtFooterCenter = new TextBox { Location = new Point(175, 48), Size = new Size(150, 60), Multiline = true, Font = new Font("Segoe UI", 9.0f) };
            txtFooterRight = new TextBox { Location = new Point(335, 48), Size = new Size(150, 60), Multiline = true, Font = new Font("Segoe UI", 9.0f) };
            grpFooter.Controls.AddRange(new Control[] { txtFooterLeft, txtFooterCenter, txtFooterRight });

            // Quick Preset Buttons
            FlowLayoutPanel pnlPresets = new FlowLayoutPanel { Location = new Point(20, 290), Size = new Size(505, 32) };
            this.Controls.Add(pnlPresets);

            Button btnAddPageNum = new Button { Text = "+ Số trang &[Page]", AutoSize = true, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddPageNum.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnAddPageNum.Click += (s, e) => { txtFooterCenter.Text = "Trang &[Page] / &[Pages]"; };
            pnlPresets.Controls.Add(btnAddPageNum);

            Button btnAddDate = new Button { Text = "+ Ngày &[Date]", AutoSize = true, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddDate.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnAddDate.Click += (s, e) => { txtHeaderRight.Text = "&[Date]"; };
            pnlPresets.Controls.Add(btnAddDate);

            Button btnAddSheet = new Button { Text = "+ Tên Sheet &[Tab]", AutoSize = true, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddSheet.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnAddSheet.Click += (s, e) => { txtHeaderLeft.Text = "&[Tab]"; };
            pnlPresets.Controls.Add(btnAddSheet);

            // Bottom Buttons
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOK = new Button
            {
                Text = "Đồng ý",
                Location = new Point(330, 10),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) => SaveData();
            bottomPanel.Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(435, 10),
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

        private void LoadData()
        {
            if (_calcView != null)
            {
                txtHeaderLeft.Text = _calcView.HeaderLeft ?? "";
                txtHeaderCenter.Text = _calcView.HeaderCenter ?? "";
                txtHeaderRight.Text = _calcView.HeaderRight ?? "";
                txtFooterLeft.Text = _calcView.FooterLeft ?? "";
                txtFooterCenter.Text = _calcView.FooterCenter ?? "Trang &[Page]";
                txtFooterRight.Text = _calcView.FooterRight ?? "";
            }
        }

        private void SaveData()
        {
            if (_calcView != null)
            {
                _calcView.HeaderLeft = txtHeaderLeft.Text.Trim();
                _calcView.HeaderCenter = txtHeaderCenter.Text.Trim();
                _calcView.HeaderRight = txtHeaderRight.Text.Trim();
                _calcView.FooterLeft = txtFooterLeft.Text.Trim();
                _calcView.FooterCenter = txtFooterCenter.Text.Trim();
                _calcView.FooterRight = txtFooterRight.Text.Trim();
            }
        }
    }
}

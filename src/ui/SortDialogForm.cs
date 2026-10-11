using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class SortDialogForm : Form
    {
        private ComboBox _cboSortBy;
        private ComboBox _cboType;
        private RadioButton _radAscending;
        private RadioButton _radDescending;
        private RadioButton _radNoHeader;
        private RadioButton _radHeader;
        private Button _btnOk;
        private Button _btnCancel;

        public bool Ascending { get { return _radAscending.Checked; } }
        public string SortType { get { return _cboType.SelectedItem != null ? _cboType.SelectedItem.ToString() : "Văn bản (Text)"; } }
        public bool HasHeader { get { return _radHeader.Checked; } }

        public event Action<bool, string, bool> OnSortExecute;

        public SortDialogForm()
        {
            Text = "Sắp xếp văn bản (Sort Text)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 240);
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            InitializeControls();
        }

        private void InitializeControls()
        {
            // 1. GroupBox: Sort by
            GroupBox grpSort = new GroupBox
            {
                Text = "Sắp xếp theo (Sort by)",
                Location = new Point(16, 12),
                Size = new Size(348, 120),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            Label lblSortBy = new Label { Text = "Đối tượng:", Location = new Point(14, 26), AutoSize = true };
            _cboSortBy = new ComboBox
            {
                Location = new Point(80, 23),
                Size = new Size(130, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboSortBy.Items.AddRange(new object[] { "Đoạn văn", "Tiêu đề", "Cột (Field 1)" });
            _cboSortBy.SelectedIndex = 0;

            Label lblType = new Label { Text = "Kiểu dữ liệu:", Location = new Point(14, 56), AutoSize = true };
            _cboType = new ComboBox
            {
                Location = new Point(80, 53),
                Size = new Size(130, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboType.Items.AddRange(new object[] { "Văn bản (Text)", "Số (Number)", "Ngày tháng (Date)" });
            _cboType.SelectedIndex = 0;

            _radAscending = new RadioButton
            {
                Text = "Tăng dần (A → Z, 0 → 9)",
                Location = new Point(220, 24),
                Size = new Size(122, 22),
                Checked = true
            };
            _radDescending = new RadioButton
            {
                Text = "Giảm dần (Z → A, 9 → 0)",
                Location = new Point(220, 54),
                Size = new Size(122, 22)
            };

            grpSort.Controls.Add(lblSortBy);
            grpSort.Controls.Add(_cboSortBy);
            grpSort.Controls.Add(lblType);
            grpSort.Controls.Add(_cboType);
            grpSort.Controls.Add(_radAscending);
            grpSort.Controls.Add(_radDescending);
            Controls.Add(grpSort);

            // 2. GroupBox: My list has
            GroupBox grpHeader = new GroupBox
            {
                Text = "Danh sách của tôi (My list has)",
                Location = new Point(16, 138),
                Size = new Size(348, 52),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            _radNoHeader = new RadioButton
            {
                Text = "Không có dòng tiêu đề",
                Location = new Point(16, 20),
                Size = new Size(150, 22),
                Checked = true
            };
            _radHeader = new RadioButton
            {
                Text = "Có dòng tiêu đề (Header row)",
                Location = new Point(170, 20),
                Size = new Size(170, 22)
            };

            grpHeader.Controls.Add(_radNoHeader);
            grpHeader.Controls.Add(_radHeader);
            Controls.Add(grpHeader);

            // 3. Action Buttons
            _btnOk = new Button
            {
                Text = "Sắp xếp (OK)",
                Location = new Point(172, 200),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(16, 110, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) =>
            {
                if (OnSortExecute != null)
                {
                    OnSortExecute(Ascending, SortType, HasHeader);
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(274, 200),
                Size = new Size(90, 28),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class CustomTableForm : Form
    {
        public int Columns { get; set; }
        public int Rows { get; set; }

        private NumericUpDown numCols;
        private NumericUpDown numRows;

        public CustomTableForm()
        {
            Columns = 5;
            Rows = 5;

            Text = "Insert Table (Chèn bảng tùy chỉnh)";
            Size = new Size(320, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(243, 242, 241);

            Label lblCols = new Label { Text = "Number of columns (Số cột):", Location = new Point(20, 25), AutoSize = true };
            numCols = new NumericUpDown { Location = new Point(200, 22), Size = new Size(80, 24), Minimum = 1, Maximum = 20, Value = 5 };

            Label lblRows = new Label { Text = "Number of rows (Số hàng):", Location = new Point(20, 65), AutoSize = true };
            numRows = new NumericUpDown { Location = new Point(200, 62), Size = new Size(80, 24), Minimum = 1, Maximum = 50, Value = 5 };

            Button btnOK = new Button { Text = "OK (Đồng ý)", DialogResult = DialogResult.OK, Location = new Point(115, 115), Size = new Size(85, 28), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnOK.Click += (s, e) =>
            {
                Columns = (int)numCols.Value;
                Rows = (int)numRows.Value;
            };

            Button btnCancel = new Button { Text = "Cancel (Hủy)", DialogResult = DialogResult.Cancel, Location = new Point(205, 115), Size = new Size(85, 28) };

            Controls.Add(lblCols);
            Controls.Add(numCols);
            Controls.Add(lblRows);
            Controls.Add(numRows);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
}

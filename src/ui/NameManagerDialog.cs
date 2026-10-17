using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class NameManagerDialog : Form
    {
        private CalcView calcView;
        private ListView lvNames;

        public NameManagerDialog(CalcView view)
        {
            this.calcView = view;
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "Trình quản lý Tên (Name Manager)";
            this.Size = new Size(580, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            Button btnNew = new Button { Text = "Mới...", Location = new Point(16, 16), Width = 90, Height = 28, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White };
            btnNew.Click += BtnNew_Click;

            Button btnEdit = new Button { Text = "Chỉnh sửa...", Location = new Point(114, 16), Width = 95, Height = 28 };
            btnEdit.Click += BtnEdit_Click;

            Button btnDelete = new Button { Text = "Xóa", Location = new Point(216, 16), Width = 80, Height = 28 };
            btnDelete.Click += BtnDelete_Click;

            Button btnClose = new Button { Text = "Đóng", Location = new Point(470, 16), Width = 80, Height = 28 };
            btnClose.Click += (s, e) => this.Close();

            lvNames = new ListView
            {
                Location = new Point(16, 56),
                Width = 534,
                Height = 310,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvNames.Columns.Add("Tên (Name)", 130);
            lvNames.Columns.Add("Giá trị / Tham chiếu (Refers To)", 180);
            lvNames.Columns.Add("Phạm vi (Scope)", 110);
            lvNames.Columns.Add("Ghi chú (Comment)", 100);

            this.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDelete, btnClose, lvNames });
        }

        private void RefreshList()
        {
            lvNames.Items.Clear();
            if (calcView != null && calcView.DefinedNames != null)
            {
                foreach (var dn in calcView.DefinedNames)
                {
                    var lvi = new ListViewItem(dn.Name);
                    lvi.SubItems.Add(dn.RefersTo);
                    lvi.SubItems.Add(dn.Scope);
                    lvi.SubItems.Add(dn.Comment);
                    lvi.Tag = dn;
                    lvNames.Items.Add(lvi);
                }
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            using (Form editDlg = new Form())
            {
                editDlg.Text = "Tên mới (New Name)";
                editDlg.Size = new Size(380, 260);
                editDlg.StartPosition = FormStartPosition.CenterParent;
                editDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                editDlg.MaximizeBox = false; editDlg.MinimizeBox = false;

                Label lblN = new Label { Text = "Tên vùng (Name):", Location = new Point(16, 16), AutoSize = true };
                TextBox txtN = new TextBox { Text = "VungDuLieu1", Location = new Point(16, 38), Width = 330 };

                Label lblS = new Label { Text = "Phạm vi (Scope):", Location = new Point(16, 70), AutoSize = true };
                ComboBox cboS = new ComboBox { Location = new Point(16, 92), Width = 330, DropDownStyle = ComboBoxStyle.DropDownList };
                cboS.Items.Add("Toàn bảng tính (Workbook)");
                cboS.SelectedIndex = 0;

                Label lblR = new Label { Text = "Tham chiếu tới (Refers to):", Location = new Point(16, 124), AutoSize = true };
                string curAddr = "=" + (calcView.CurrentSheet != null ? "Sheet1" : "") + "!$" + SparseMatrix.ColIndexToName(calcView.SelectedCol) + "$" + calcView.SelectedRow;
                TextBox txtR = new TextBox { Text = curAddr, Location = new Point(16, 146), Width = 330 };

                Button btnOk = new Button { Text = "OK", Location = new Point(180, 182), Width = 80, Height = 28, DialogResult = DialogResult.OK };
                Button btnCan = new Button { Text = "Hủy", Location = new Point(266, 182), Width = 80, Height = 28, DialogResult = DialogResult.Cancel };
                editDlg.Controls.AddRange(new Control[] { lblN, txtN, lblS, cboS, lblR, txtR, btnOk, btnCan });

                if (editDlg.ShowDialog(this) == DialogResult.OK)
                {
                    calcView.DefinedNames.Add(new DefinedNameModel(txtN.Text.Trim(), txtR.Text.Trim()));
                    RefreshList();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (lvNames.SelectedItems.Count == 0) return;
            var dn = lvNames.SelectedItems[0].Tag as DefinedNameModel;
            if (dn == null) return;

            using (Form editDlg = new Form())
            {
                editDlg.Text = "Chỉnh sửa Tên";
                editDlg.Size = new Size(380, 240);
                editDlg.StartPosition = FormStartPosition.CenterParent;
                editDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                editDlg.MaximizeBox = false; editDlg.MinimizeBox = false;

                Label lblN = new Label { Text = "Tên vùng (Name):", Location = new Point(16, 16), AutoSize = true };
                TextBox txtN = new TextBox { Text = dn.Name, Location = new Point(16, 38), Width = 330 };

                Label lblR = new Label { Text = "Tham chiếu tới (Refers to):", Location = new Point(16, 70), AutoSize = true };
                TextBox txtR = new TextBox { Text = dn.RefersTo, Location = new Point(16, 92), Width = 330 };

                Button btnOk = new Button { Text = "OK", Location = new Point(180, 150), Width = 80, Height = 28, DialogResult = DialogResult.OK };
                Button btnCan = new Button { Text = "Hủy", Location = new Point(266, 150), Width = 80, Height = 28, DialogResult = DialogResult.Cancel };
                editDlg.Controls.AddRange(new Control[] { lblN, txtN, lblR, txtR, btnOk, btnCan });

                if (editDlg.ShowDialog(this) == DialogResult.OK)
                {
                    dn.Name = txtN.Text.Trim();
                    dn.RefersTo = txtR.Text.Trim();
                    RefreshList();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lvNames.SelectedItems.Count == 0) return;
            var dn = lvNames.SelectedItems[0].Tag as DefinedNameModel;
            if (dn != null && MessageBox.Show($"Bạn có chắc chắn muốn xóa tên \"{dn.Name}\"?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                calcView.DefinedNames.Remove(dn);
                RefreshList();
            }
        }
    }
}

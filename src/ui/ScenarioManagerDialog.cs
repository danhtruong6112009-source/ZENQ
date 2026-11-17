using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class ScenarioManagerDialog : Form
    {
        private CalcView calcView;
        private ListBox lstScenarios;
        private TextBox txtComment;
        private Label lblChangingCells;

        public ScenarioManagerDialog(CalcView view)
        {
            this.calcView = view;
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = AppL10n.T("Quản lý kịch bản (Scenario Manager)", "Scenario Manager");
            this.Size = new Size(520, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            Label lblTitle = new Label { Text = AppL10n.T("Các kịch bản hiện có:", "Scenarios:"), Location = new Point(16, 14), AutoSize = true };
            lstScenarios = new ZenQOffice.UI.FluentListBox { Location = new Point(16, 36), Width = 340, Height = 160 };
            lstScenarios.SelectedIndexChanged += LstScenarios_SelectedIndexChanged;

            Button btnShow = new Button { Text = AppL10n.T("Hiển thị", "Show"), Location = new Point(370, 36), Width = 115, Height = 28, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White };
            btnShow.Click += BtnShow_Click;

            Button btnAdd = new Button { Text = AppL10n.T("Thêm...", "Add..."), Location = new Point(370, 70), Width = 115, Height = 28 };
            btnAdd.Click += BtnAdd_Click;

            Button btnDelete = new Button { Text = AppL10n.T("Xóa", "Delete"), Location = new Point(370, 104), Width = 115, Height = 28 };
            btnDelete.Click += BtnDelete_Click;

            Button btnSummary = new Button { Text = AppL10n.T("Tóm tắt...", "Summary..."), Location = new Point(370, 138), Width = 115, Height = 28 };
            btnSummary.Click += BtnSummary_Click;

            Button btnClose = new Button { Text = AppL10n.T("Đóng", "Close"), Location = new Point(370, 172), Width = 115, Height = 28 };
            btnClose.Click += (s, e) => this.Close();

            lblChangingCells = new Label { Text = AppL10n.T("Các ô thay đổi: (Chưa chọn)", "Changing cells: (None)"), Location = new Point(16, 206), Width = 340, AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };
            Label lblCmt = new Label { Text = AppL10n.T("Ghi chú:", "Comment:"), Location = new Point(16, 232), AutoSize = true };
            txtComment = new TextBox { Location = new Point(16, 252), Width = 340, Height = 70, Multiline = true, ReadOnly = true, BackColor = Color.White };

            this.Controls.AddRange(new Control[] {
                lblTitle, lstScenarios, btnShow, btnAdd, btnDelete, btnSummary, btnClose,
                lblChangingCells, lblCmt, txtComment
            });
        }

        private void RefreshList()
        {
            lstScenarios.Items.Clear();
            if (calcView != null && calcView.Scenarios != null)
            {
                foreach (var sc in calcView.Scenarios)
                {
                    lstScenarios.Items.Add(sc.Name);
                }
            }
            if (lstScenarios.Items.Count > 0)
            {
                lstScenarios.SelectedIndex = 0;
            }
            else
            {
                lblChangingCells.Text = AppL10n.T("Các ô thay đổi: (Không có)", "Changing cells: (None)");
                txtComment.Text = "";
            }
        }

        private void LstScenarios_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstScenarios.SelectedIndex;
            if (idx >= 0 && idx < calcView.Scenarios.Count)
            {
                var sc = calcView.Scenarios[idx];
                txtComment.Text = sc.Comment ?? "";

                List<string> cellNames = new List<string>();
                foreach (var tuple in sc.ChangingCells)
                {
                    cellNames.Add(SparseMatrix.ColIndexToName(tuple.Item2) + tuple.Item1);
                }
                lblChangingCells.Text = AppL10n.T("Các ô thay đổi: ", "Changing cells: ") + string.Join(", ", cellNames);
            }
        }

        private void BtnShow_Click(object sender, EventArgs e)
        {
            int idx = lstScenarios.SelectedIndex;
            if (idx >= 0 && idx < calcView.Scenarios.Count)
            {
                calcView.ApplyScenario(idx);
                ZenQOffice.UI.FluentMessageBox.Show(string.Format(AppL10n.T("Đã áp dụng kịch bản \"{0}\" vào bảng tính thành công!", "Applied scenario \"{0}\" successfully!"), calcView.Scenarios[idx].Name), AppL10n.T("Thông báo", "Scenario Manager"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (Form addDlg = new Form())
            {
                addDlg.Text = AppL10n.T("Thêm kịch bản mới", "Add Scenario");
                addDlg.Size = new Size(380, 260);
                addDlg.StartPosition = FormStartPosition.CenterParent;
                addDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                addDlg.MaximizeBox = false; addDlg.MinimizeBox = false;

                Label lblN = new Label { Text = AppL10n.T("Tên kịch bản:", "Scenario name:"), Location = new Point(16, 16), AutoSize = true };
                TextBox txtN = new TextBox { Text = AppL10n.T("Kịch bản ", "Scenario ") + (calcView.Scenarios.Count + 1), Location = new Point(16, 38), Width = 330 };

                Label lblC = new Label { Text = AppL10n.T("Các ô thay đổi (ví dụ: B2, B3):", "Changing cells (e.g. B2, B3):"), Location = new Point(16, 70), AutoSize = true };
                string curAddr = SparseMatrix.ColIndexToName(calcView.SelectedCol) + calcView.SelectedRow;
                TextBox txtC = new TextBox { Text = curAddr, Location = new Point(16, 92), Width = 330 };

                Label lblV = new Label { Text = AppL10n.T("Giá trị gán mới (cách nhau bởi dấu phẩy):", "New values (comma-separated):"), Location = new Point(16, 124), AutoSize = true };
                TextBox txtV = new TextBox { Text = "100", Location = new Point(16, 146), Width = 330 };

                Button btnOk = new Button { Text = "OK", Location = new Point(180, 182), Width = 80, Height = 28, DialogResult = DialogResult.OK };
                Button btnCan = new Button { Text = AppL10n.T("Hủy", "Cancel"), Location = new Point(266, 182), Width = 80, Height = 28, DialogResult = DialogResult.Cancel };
                addDlg.Controls.AddRange(new Control[] { lblN, txtN, lblC, txtC, lblV, txtV, btnOk, btnCan });

                if (addDlg.ShowDialog(this) == DialogResult.OK)
                {
                    var sc = new ScenarioModel { Name = txtN.Text.Trim() };
                    string[] cParts = txtC.Text.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in cParts)
                    {
                        uint r, c;
                        if (SparseMatrix.TryParseCellReference(p.Trim(), out r, out c))
                        {
                            sc.ChangingCells.Add(Tuple.Create(r, c));
                        }
                    }
                    string[] vParts = txtV.Text.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var v in vParts) sc.Values.Add(v.Trim());

                    calcView.Scenarios.Add(sc);
                    RefreshList();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            int idx = lstScenarios.SelectedIndex;
            if (idx >= 0 && idx < calcView.Scenarios.Count)
            {
                if (ZenQOffice.UI.FluentMessageBox.Show(string.Format(AppL10n.T("Bạn có chắc chắn muốn xóa kịch bản \"{0}\"?", "Delete scenario \"{0}\"?"), calcView.Scenarios[idx].Name), AppL10n.T("Xác nhận", "Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    calcView.Scenarios.RemoveAt(idx);
                    RefreshList();
                }
            }
        }

        private void BtnSummary_Click(object sender, EventArgs e)
        {
            if (calcView.Scenarios.Count == 0)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Chưa có kịch bản nào để tạo tóm tắt.", "No scenarios available to summarize."), AppL10n.T("Thông báo", "Scenario Manager"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            calcView.GenerateScenarioSummaryReport();
            ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Đã tạo trang báo cáo Tóm tắt Kịch bản (Scenario Summary) thành công!", "Created Scenario Summary sheet successfully!"), AppL10n.T("Thông báo", "Scenario Manager"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}

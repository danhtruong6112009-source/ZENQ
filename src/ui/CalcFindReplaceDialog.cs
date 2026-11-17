using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class CalcFindReplaceDialog : Form
    {
        private CalcView _calcView;
        private Form _parentForm;

        private TabControl tabControl;
        private TabPage tabFind;
        private TabPage tabReplace;

        private TextBox txtFind;
        private TextBox txtReplace;
        private Label lblReplace;
        private CheckBox chkMatchCase;
        private CheckBox chkMatchEntire;
        private ComboBox cboSearchBy;
        private ComboBox cboLookIn;

        private Button btnFindNext;
        private Button btnFindPrev;
        private Button btnFindAll;
        private Button btnReplace;
        private Button btnReplaceAll;
        private Button btnClose;

        private ListView lvResults;
        private Label lblStatus;

        public CalcFindReplaceDialog(CalcView calcView, Form parentForm, bool isReplace)
        {
            _calcView = calcView;
            _parentForm = parentForm;

            InitializeComponent();
            SetMode(isReplace);
        }

        public void SetMode(bool isReplace)
        {
            if (tabControl != null)
            {
                tabControl.SelectedTab = isReplace ? tabReplace : tabFind;
            }
            UpdateControlVisibility(isReplace);
            if (txtFind != null)
            {
                txtFind.Focus();
                txtFind.SelectAll();
            }
        }

        private void InitializeComponent()
        {
            this.Text = AppL10n.T("Tìm kiếm & Thay thế - ZenQ Calc", "Find and Replace - ZenQ Calc");
            this.Size = new Size(540, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(243, 242, 241);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            this.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Hide();
                    e.Handled = true;
                }
            };

            tabControl = new ZenQOffice.UI.FluentTabControl();
            tabControl.Location = new Point(12, 10);
            tabControl.Size = new Size(500, 30);
            tabFind = new TabPage(AppL10n.T("Tìm kiếm (Find)", "Find"));
            tabReplace = new TabPage(AppL10n.T("Thay thế (Replace)", "Replace"));
            tabControl.TabPages.Add(tabFind);
            tabControl.TabPages.Add(tabReplace);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            this.Controls.Add(tabControl);

            Label lblFind = new Label();
            lblFind.Text = AppL10n.T("Nội dung tìm kiếm:", "Find what:");
            lblFind.Location = new Point(15, 52);
            lblFind.AutoSize = true;
            this.Controls.Add(lblFind);

            txtFind = new TextBox();
            txtFind.Location = new Point(140, 49);
            txtFind.Size = new Size(250, 25);
            txtFind.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    DoFindNext();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            this.Controls.Add(txtFind);

            lblReplace = new Label();
            lblReplace.Text = AppL10n.T("Thay thế bằng:", "Replace with:");
            lblReplace.Location = new Point(15, 85);
            lblReplace.AutoSize = true;
            this.Controls.Add(lblReplace);

            txtReplace = new TextBox();
            txtReplace.Location = new Point(140, 82);
            txtReplace.Size = new Size(250, 25);
            txtReplace.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    DoReplace();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            this.Controls.Add(txtReplace);

            chkMatchCase = new CheckBox();
            chkMatchCase.Text = AppL10n.T("Khớp chữ hoa/thường (Match case)", "Match case");
            chkMatchCase.Location = new Point(15, 120);
            chkMatchCase.AutoSize = true;
            this.Controls.Add(chkMatchCase);

            chkMatchEntire = new CheckBox();
            chkMatchEntire.Text = AppL10n.T("Khớp toàn bộ ô (Match entire cell)", "Match entire cell contents");
            chkMatchEntire.Location = new Point(15, 145);
            chkMatchEntire.AutoSize = true;
            this.Controls.Add(chkMatchEntire);

            Label lblSearchBy = new Label();
            lblSearchBy.Text = AppL10n.T("Tìm theo:", "Search:");
            lblSearchBy.Location = new Point(275, 122);
            lblSearchBy.AutoSize = true;
            this.Controls.Add(lblSearchBy);

            cboSearchBy = new ZenQOffice.UI.FluentComboBox();
            cboSearchBy.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSearchBy.Items.AddRange(new object[] { AppL10n.T("Theo Hàng (By Rows)", "By Rows"), AppL10n.T("Theo Cột (By Columns)", "By Columns") });
            cboSearchBy.SelectedIndex = 0;
            cboSearchBy.Location = new Point(350, 118);
            cboSearchBy.Size = new Size(160, 23);
            this.Controls.Add(cboSearchBy);

            Label lblLookIn = new Label();
            lblLookIn.Text = AppL10n.T("Tìm trong:", "Look in:");
            lblLookIn.Location = new Point(275, 147);
            lblLookIn.AutoSize = true;
            this.Controls.Add(lblLookIn);

            cboLookIn = new ZenQOffice.UI.FluentComboBox();
            cboLookIn.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLookIn.Items.AddRange(new object[] { AppL10n.T("Giá trị (Values)", "Values"), AppL10n.T("Công thức (Formulas)", "Formulas") });
            cboLookIn.SelectedIndex = 0;
            cboLookIn.Location = new Point(350, 144);
            cboLookIn.Size = new Size(160, 23);
            this.Controls.Add(cboLookIn);

            // Action Buttons
            btnFindNext = new Button();
            btnFindNext.Text = AppL10n.T("Tìm tiếp", "Find Next");
            btnFindNext.Location = new Point(405, 47);
            btnFindNext.Size = new Size(105, 28);
            btnFindNext.Click += delegate(object sender, EventArgs e) { DoFindNext(); };
            this.Controls.Add(btnFindNext);

            btnFindPrev = new Button();
            btnFindPrev.Text = AppL10n.T("Tìm trước", "Find Previous");
            btnFindPrev.Location = new Point(405, 80);
            btnFindPrev.Size = new Size(105, 28);
            btnFindPrev.Click += delegate(object sender, EventArgs e) { DoFindPrev(); };
            this.Controls.Add(btnFindPrev);

            btnReplace = new Button();
            btnReplace.Text = AppL10n.T("Thay thế", "Replace");
            btnReplace.Location = new Point(15, 180);
            btnReplace.Size = new Size(95, 30);
            btnReplace.Click += delegate(object sender, EventArgs e) { DoReplace(); };
            this.Controls.Add(btnReplace);

            btnReplaceAll = new Button();
            btnReplaceAll.Text = AppL10n.T("Thay thế tất cả", "Replace All");
            btnReplaceAll.Location = new Point(118, 180);
            btnReplaceAll.Size = new Size(110, 30);
            btnReplaceAll.Click += delegate(object sender, EventArgs e) { DoReplaceAll(); };
            this.Controls.Add(btnReplaceAll);

            btnFindAll = new Button();
            btnFindAll.Text = AppL10n.T("Tìm tất cả", "Find All");
            btnFindAll.Location = new Point(236, 180);
            btnFindAll.Size = new Size(95, 30);
            btnFindAll.Click += delegate(object sender, EventArgs e) { DoFindAll(); };
            this.Controls.Add(btnFindAll);

            btnClose = new Button();
            btnClose.Text = AppL10n.T("Đóng", "Close");
            btnClose.Location = new Point(415, 180);
            btnClose.Size = new Size(95, 30);
            btnClose.Click += delegate(object sender, EventArgs e) { this.Hide(); };
            this.Controls.Add(btnClose);

            // Results List
            lvResults = new ListView();
            lvResults.View = View.Details;
            lvResults.FullRowSelect = true;
            lvResults.GridLines = true;
            lvResults.Location = new Point(15, 220);
            lvResults.Size = new Size(495, 180);
            lvResults.Columns.Add(AppL10n.T("Bảng tính", "Sheet"), 80);
            lvResults.Columns.Add(AppL10n.T("Ô", "Cell"), 60);
            lvResults.Columns.Add(AppL10n.T("Nội dung", "Value"), 330);
            lvResults.SelectedIndexChanged += LvResults_SelectedIndexChanged;
            this.Controls.Add(lvResults);

            lblStatus = new Label();
            lblStatus.Text = AppL10n.T("Sẵn sàng tìm kiếm.", "Ready to search.");
            lblStatus.Location = new Point(15, 410);
            lblStatus.AutoSize = true;
            this.Controls.Add(lblStatus);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlVisibility(tabControl.SelectedTab == tabReplace);
        }

        private void UpdateControlVisibility(bool isReplace)
        {
            if (lblReplace != null) lblReplace.Visible = isReplace;
            if (txtReplace != null) txtReplace.Visible = isReplace;
            if (btnReplace != null) btnReplace.Visible = isReplace;
            if (btnReplaceAll != null) btnReplaceAll.Visible = isReplace;
        }

        private void DoFindNext()
        {
            string q = txtFind.Text;
            if (string.IsNullOrEmpty(q)) return;

            string statusMsg;
            bool found = _calcView.FindNextCellEx(q, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboSearchBy.SelectedIndex == 0, cboLookIn.SelectedIndex == 1, false, out statusMsg);

            lblStatus.Text = statusMsg;
            if (!found)
            {
                ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Không tìm thấy nội dung: \"", "No match found for: \"") + q + "\"", AppL10n.T("Tìm kiếm", "Find"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoFindPrev()
        {
            string q = txtFind.Text;
            if (string.IsNullOrEmpty(q)) return;

            string statusMsg;
            bool found = _calcView.FindNextCellEx(q, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboSearchBy.SelectedIndex == 0, cboLookIn.SelectedIndex == 1, true, out statusMsg);

            lblStatus.Text = statusMsg;
            if (!found)
            {
                ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Không tìm thấy nội dung: \"", "No match found for: \"") + q + "\"", AppL10n.T("Tìm kiếm", "Find"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoReplace()
        {
            string find = txtFind.Text;
            if (string.IsNullOrEmpty(find)) return;
            string repl = txtReplace.Text ?? "";

            int rep = _calcView.ReplaceCellEx(find, repl, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboSearchBy.SelectedIndex == 0, cboLookIn.SelectedIndex == 1);

            if (rep > 0)
            {
                lblStatus.Text = AppL10n.T("Đã thay thế ô hiện tại.", "Replaced current cell.");
                DoFindNext();
            }
            else
            {
                DoFindNext();
            }
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoReplaceAll()
        {
            string find = txtFind.Text;
            if (string.IsNullOrEmpty(find)) return;
            string repl = txtReplace.Text ?? "";

            int count = _calcView.ReplaceAllInSheetEx(find, repl, chkMatchCase.Checked, chkMatchEntire.Checked,
                cboLookIn.SelectedIndex == 1);

            lblStatus.Text = string.Format(AppL10n.T("Đã hoàn tất thay thế {0} vị trí.", "Completed {0} replacement(s)."), count);
            ZenQOffice.UI.FluentMessageBox.Show(this, string.Format(AppL10n.T("ZenQ Calc đã hoàn thành việc tìm kiếm và thực hiện {0} lượt thay thế.", "ZenQ Calc finished searching and made {0} replacement(s)."), count),
                AppL10n.T("Thay thế tất cả", "Replace All"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (_parentForm != null) _parentForm.Invalidate();
        }

        private void DoFindAll()
        {
            string q = txtFind.Text;
            if (string.IsNullOrEmpty(q)) return;

            lvResults.Items.Clear();
            List<Tuple<string, uint, uint, string>> matches = _calcView.FindAllCellsEx(q, chkMatchCase.Checked, chkMatchEntire.Checked, cboLookIn.SelectedIndex == 1);

            for (int i = 0; i < matches.Count; i++)
            {
                Tuple<string, uint, uint, string> m = matches[i];
                ListViewItem lvi = new ListViewItem(m.Item1);
                lvi.SubItems.Add(SparseMatrix.GetCellReference(m.Item2, m.Item3));
                lvi.SubItems.Add(m.Item4);
                lvi.Tag = Tuple.Create(m.Item2, m.Item3);
                lvResults.Items.Add(lvi);
            }

            lblStatus.Text = string.Format(AppL10n.T("Đã tìm thấy {0} kết quả phù hợp.", "Found {0} matching cell(s)."), matches.Count);
        }

        private void LvResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count > 0)
            {
                Tuple<uint, uint> tag = lvResults.SelectedItems[0].Tag as Tuple<uint, uint>;
                if (tag != null)
                {
                    _calcView.SelectedRow = tag.Item1;
                    _calcView.SelectedCol = tag.Item2;
                    _calcView.SelEndRow = tag.Item1;
                    _calcView.SelEndCol = tag.Item2;
                    _calcView.EnsureCellVisible(tag.Item1, tag.Item2);
                    if (_parentForm != null) _parentForm.Invalidate();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class CalcRemoveDuplicatesDialog : Form
    {
        private CalcView _calcView;
        private Form _parentForm;

        private uint _minR;
        private uint _maxR;
        private uint _minC;
        private uint _maxC;

        private Label lblRange;
        private CheckBox chkHasHeaders;
        private CheckedListBox clbColumns;
        private Button btnSelectAll;
        private Button btnUnselectAll;
        private Button btnOK;
        private Button btnCancel;

        public CalcRemoveDuplicatesDialog(CalcView calcView, Form parentForm)
        {
            _calcView = calcView;
            _parentForm = parentForm;

            DetermineRange();
            InitializeComponent();
            PopulateColumns();
        }

        private void DetermineRange()
        {
            _minR = Math.Min(_calcView.SelectedRow, _calcView.SelEndRow);
            _maxR = Math.Max(_calcView.SelectedRow, _calcView.SelEndRow);
            _minC = Math.Min(_calcView.SelectedCol, _calcView.SelEndCol);
            _maxC = Math.Max(_calcView.SelectedCol, _calcView.SelEndCol);

            if (_minR == _maxR && _minC == _maxC)
            {
                _calcView.GetContiguousDataRange(_calcView.SelectedRow, _calcView.SelectedCol,
                    out _minR, out _maxR, out _minC, out _maxC);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Xóa Trùng Lặp (Remove Duplicates)";
            this.Size = new Size(380, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(243, 242, 241);

            Label lblPrompt = new Label();
            lblPrompt.Text = "Để xóa các dòng trùng lặp, hãy chọn một hoặc nhiều cột:";
            lblPrompt.Location = new Point(15, 12);
            lblPrompt.Size = new Size(340, 32);
            this.Controls.Add(lblPrompt);

            lblRange = new Label();
            string refStart = SparseMatrix.GetCellReference(_minR, _minC);
            string refEnd = SparseMatrix.GetCellReference(_maxR, _maxC);
            lblRange.Text = "Vùng dữ liệu: " + refStart + ":" + refEnd;
            lblRange.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblRange.Location = new Point(15, 45);
            lblRange.AutoSize = true;
            this.Controls.Add(lblRange);

            chkHasHeaders = new CheckBox();
            chkHasHeaders.Text = "Dữ liệu có hàng tiêu đề (My data has headers)";
            chkHasHeaders.Location = new Point(15, 72);
            chkHasHeaders.AutoSize = true;
            chkHasHeaders.Checked = DetectHeaderHeuristic();
            chkHasHeaders.CheckedChanged += delegate(object sender, EventArgs e) { PopulateColumns(); };
            this.Controls.Add(chkHasHeaders);

            btnSelectAll = new Button();
            btnSelectAll.Text = "Chọn tất cả";
            btnSelectAll.Location = new Point(15, 100);
            btnSelectAll.Size = new Size(95, 26);
            btnSelectAll.Click += delegate(object sender, EventArgs e) { SetAllColumnsChecked(true); };
            this.Controls.Add(btnSelectAll);

            btnUnselectAll = new Button();
            btnUnselectAll.Text = "Bỏ chọn tất cả";
            btnUnselectAll.Location = new Point(115, 100);
            btnUnselectAll.Size = new Size(105, 26);
            btnUnselectAll.Click += delegate(object sender, EventArgs e) { SetAllColumnsChecked(false); };
            this.Controls.Add(btnUnselectAll);

            clbColumns = new CheckedListBox();
            clbColumns.Location = new Point(15, 132);
            clbColumns.Size = new Size(335, 190);
            clbColumns.CheckOnClick = true;
            this.Controls.Add(clbColumns);

            btnOK = new Button();
            btnOK.Text = "Xóa trùng lặp";
            btnOK.Location = new Point(145, 335);
            btnOK.Size = new Size(110, 32);
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button();
            btnCancel.Text = "Hủy";
            btnCancel.Location = new Point(265, 335);
            btnCancel.Size = new Size(85, 32);
            btnCancel.Click += delegate(object sender, EventArgs e) { this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private bool DetectHeaderHeuristic()
        {
            if (_maxR <= _minR) return false;
            SparseMatrix sheet = _calcView.CurrentSheet;
            if (sheet == null) return false;

            int stringCountRow1 = 0;
            int numCountRow2 = 0;
            for (uint c = _minC; c <= _maxC; c++)
            {
                CellData c1 = sheet.GetCell(_minR, c);
                if (c1 != null && c1.Type == CellType.String && !string.IsNullOrEmpty(c1.RawValue))
                {
                    stringCountRow1++;
                }
                CellData c2 = sheet.GetCell(_minR + 1, c);
                if (c2 != null && (c2.Type == CellType.Number || c2.Type == CellType.Formula))
                {
                    numCountRow2++;
                }
            }
            return (stringCountRow1 > 0 && numCountRow2 > 0) || stringCountRow1 >= ((_maxC - _minC + 1) / 2);
        }

        private void PopulateColumns()
        {
            clbColumns.Items.Clear();
            SparseMatrix sheet = _calcView.CurrentSheet;

            for (uint c = _minC; c <= _maxC; c++)
            {
                string colLetter = SparseMatrix.ColIndexToName(c);
                string text = "Cột " + colLetter;
                if (chkHasHeaders.Checked && sheet != null)
                {
                    CellData headerCell = sheet.GetCell(_minR, c);
                    string headerVal = (headerCell != null) ? (headerCell.EvaluatedStr ?? headerCell.RawValue ?? "") : "";
                    if (!string.IsNullOrEmpty(headerVal))
                    {
                        text += ": " + headerVal;
                    }
                }
                clbColumns.Items.Add(new ColumnItem(c, text), true);
            }
        }

        private void SetAllColumnsChecked(bool isChecked)
        {
            for (int i = 0; i < clbColumns.Items.Count; i++)
            {
                clbColumns.SetItemChecked(i, isChecked);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            List<uint> checkedCols = new List<uint>();
            for (int i = 0; i < clbColumns.CheckedItems.Count; i++)
            {
                ColumnItem item = clbColumns.CheckedItems[i] as ColumnItem;
                if (item != null) checkedCols.Add(item.ColIndex);
            }

            if (checkedCols.Count == 0)
            {
                MessageBox.Show(this, "Vui lòng chọn ít nhất một cột để kiểm tra trùng lặp!",
                    "Xóa Trùng Lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int dupCount;
            int uniqueCount;
            _calcView.ExecuteRemoveDuplicates(_minR, _maxR, _minC, _maxC, checkedCols,
                chkHasHeaders.Checked, out dupCount, out uniqueCount);

            MessageBox.Show(this, string.Format("Đã tìm thấy và xóa {0} dòng trùng lặp!\nGiữ lại {1} dòng dữ liệu duy nhất.",
                dupCount, uniqueCount), "Xóa Trùng Lặp Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (_parentForm != null) _parentForm.Invalidate();
            this.Close();
        }

        private class ColumnItem
        {
            public uint ColIndex { get; private set; }
            public string DisplayText { get; private set; }

            public ColumnItem(uint colIndex, string text)
            {
                ColIndex = colIndex;
                DisplayText = text;
            }

            public override string ToString()
            {
                return DisplayText;
            }
        }
    }
}

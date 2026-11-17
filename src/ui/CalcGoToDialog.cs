using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public enum CalcGoToSpecialMode
    {
        None = 0,
        Formulas = 1,
        Constants = 2,
        Blanks = 3,
        Errors = 4,
        CurrentRegion = 5,
        LastCell = 6
    }

    /// <summary>
    /// Fluent Excel-style Go To and Go To Special dialog (F5 / Ctrl+G).
    /// </summary>
    public class CalcGoToDialog : Form
    {
        private readonly CalcView _calcView;
        private ListBox lstTargets;
        private TextBox txtReference;
        private RadioButton rbAddress;
        private RadioButton rbFormulas;
        private RadioButton rbConstants;
        private RadioButton rbBlanks;
        private RadioButton rbErrors;
        private RadioButton rbCurrentRegion;
        private RadioButton rbLastCell;
        private Button btnOk;
        private Button btnCancel;

        public string TargetReference { get; private set; } = "";
        public CalcGoToSpecialMode SpecialMode { get; private set; } = CalcGoToSpecialMode.None;

        public CalcGoToDialog(CalcView calcView)
        {
            _calcView = calcView;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = AppL10n.T("Đi tới / Chọn đặc biệt (Go To - F5 / Ctrl+G)", "Go To (F5 / Ctrl+G)");
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(410, 345);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            Label lblList = new Label
            {
                Text = AppL10n.T("Tên vùng / Bảng dữ liệu đã đặt:", "Go to:"),
                Location = new Point(16, 14),
                AutoSize = true
            };
            this.Controls.Add(lblList);

            lstTargets = new ListBox
            {
                Location = new Point(16, 36),
                Size = new Size(185, 178),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            if (_calcView != null)
            {
                if (_calcView.DefinedNames != null)
                {
                    foreach (var dn in _calcView.DefinedNames)
                    {
                        if (dn != null && !string.IsNullOrWhiteSpace(dn.Name))
                        {
                            lstTargets.Items.Add(dn.Name);
                        }
                    }
                }
                if (_calcView.Tables != null)
                {
                    foreach (var tbl in _calcView.Tables)
                    {
                        if (tbl != null && !string.IsNullOrWhiteSpace(tbl.Name))
                        {
                            lstTargets.Items.Add(tbl.Name);
                        }
                    }
                }
            }
            lstTargets.SelectedIndexChanged += (s, e) =>
            {
                if (lstTargets.SelectedItem != null)
                {
                    txtReference.Text = lstTargets.SelectedItem.ToString();
                    rbAddress.Checked = true;
                }
            };
            lstTargets.DoubleClick += (s, e) =>
            {
                if (lstTargets.SelectedItem != null)
                {
                    txtReference.Text = lstTargets.SelectedItem.ToString();
                    rbAddress.Checked = true;
                    AcceptAndClose();
                }
            };
            this.Controls.Add(lstTargets);

            GroupBox grpSpecial = new GroupBox
            {
                Text = AppL10n.T("Chọn đặc biệt (Go To Special)", "Go To Special"),
                Location = new Point(214, 14),
                Size = new Size(180, 200)
            };
            rbAddress = new RadioButton { Text = AppL10n.T("Địa chỉ / Tên tham chiếu", "Reference / Address"), Location = new Point(12, 24), AutoSize = true, Checked = true };
            rbFormulas = new RadioButton { Text = AppL10n.T("Ô chứa Công thức (=)", "Formulas (=)"), Location = new Point(12, 50), AutoSize = true };
            rbConstants = new RadioButton { Text = AppL10n.T("Ô Hằng số / Văn bản", "Constants / Text"), Location = new Point(12, 76), AutoSize = true };
            rbBlanks = new RadioButton { Text = AppL10n.T("Ô Trống (Blanks)", "Blanks"), Location = new Point(12, 102), AutoSize = true };
            rbErrors = new RadioButton { Text = AppL10n.T("Ô Báo lỗi (#...)", "Errors (#...)"), Location = new Point(12, 128), AutoSize = true };
            rbCurrentRegion = new RadioButton { Text = AppL10n.T("Vùng dữ liệu hiện tại", "Current region"), Location = new Point(12, 154), AutoSize = true };
            rbLastCell = new RadioButton { Text = AppL10n.T("Ô dữ liệu cuối cùng", "Last cell"), Location = new Point(12, 176), AutoSize = true };

            grpSpecial.Controls.AddRange(new Control[]
            {
                rbAddress, rbFormulas, rbConstants, rbBlanks, rbErrors, rbCurrentRegion, rbLastCell
            });
            this.Controls.Add(grpSpecial);

            Label lblRef = new Label
            {
                Text = AppL10n.T("Tham chiếu (VD: A1, B2:D10, 'Sheet2'!A1 hoặc Tên vùng):", "Reference (e.g. A1, B2:D10, 'Sheet2'!A1, or Name):"),
                Location = new Point(16, 226),
                AutoSize = true
            };
            this.Controls.Add(lblRef);

            txtReference = new TextBox
            {
                Location = new Point(16, 248),
                Size = new Size(378, 24),
                Text = _calcView != null ? SparseMatrix.ColIndexToName(_calcView.SelectedCol) + _calcView.SelectedRow : "A1"
            };
            txtReference.TextChanged += (s, e) =>
            {
                if (txtReference.Focused) rbAddress.Checked = true;
            };
            this.Controls.Add(txtReference);

            btnOk = new Button
            {
                Text = AppL10n.T("Đi tới / Chọn", "OK"),
                Location = new Point(214, 294),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => AcceptAndClose();
            this.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = AppL10n.T("Hủy", "Cancel"),
                Location = new Point(314, 294),
                Size = new Size(80, 32),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void AcceptAndClose()
        {
            if (rbFormulas.Checked) SpecialMode = CalcGoToSpecialMode.Formulas;
            else if (rbConstants.Checked) SpecialMode = CalcGoToSpecialMode.Constants;
            else if (rbBlanks.Checked) SpecialMode = CalcGoToSpecialMode.Blanks;
            else if (rbErrors.Checked) SpecialMode = CalcGoToSpecialMode.Errors;
            else if (rbCurrentRegion.Checked) SpecialMode = CalcGoToSpecialMode.CurrentRegion;
            else if (rbLastCell.Checked) SpecialMode = CalcGoToSpecialMode.LastCell;
            else SpecialMode = CalcGoToSpecialMode.None;

            TargetReference = (txtReference.Text ?? "").Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

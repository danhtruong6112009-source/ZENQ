using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class CalcGoalSeekDialog : Form
    {
        private TextBox txtSetCell;
        private TextBox txtToValue;
        private TextBox txtChangingCell;
        private Button btnOk;
        private Button btnCancel;
        private CalcView _calcView;

        public CalcGoalSeekDialog(CalcView calcView)
        {
            _calcView = calcView;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Goal Seek (Dò tìm mục tiêu)";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(330, 185);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            Label lblSetCell = new Label();
            lblSetCell.Text = "Ô đặt giá trị (Set cell):";
            lblSetCell.Location = new Point(16, 20);
            lblSetCell.AutoSize = true;
            this.Controls.Add(lblSetCell);

            string defaultSetCell = "";
            if (_calcView != null && _calcView.CurrentSheet != null)
            {
                defaultSetCell = string.Format("${0}${1}",
                    SparseMatrix.ColIndexToName(_calcView.SelectedCol),
                    _calcView.SelectedRow);
            }

            txtSetCell = new TextBox();
            txtSetCell.Text = defaultSetCell;
            txtSetCell.Location = new Point(160, 17);
            txtSetCell.Size = new Size(140, 23);
            this.Controls.Add(txtSetCell);

            Label lblToVal = new Label();
            lblToVal.Text = "Giá trị đích (To value):";
            lblToVal.Location = new Point(16, 56);
            lblToVal.AutoSize = true;
            this.Controls.Add(lblToVal);

            txtToValue = new TextBox();
            txtToValue.Location = new Point(160, 53);
            txtToValue.Size = new Size(140, 23);
            this.Controls.Add(txtToValue);

            Label lblChgCell = new Label();
            lblChgCell.Text = "Bằng cách đổi ô (By changing):";
            lblChgCell.Location = new Point(16, 92);
            lblChgCell.AutoSize = true;
            this.Controls.Add(lblChgCell);

            txtChangingCell = new TextBox();
            txtChangingCell.Location = new Point(160, 89);
            txtChangingCell.Size = new Size(140, 23);
            this.Controls.Add(txtChangingCell);

            btnOk = new Button();
            btnOk.Text = "Đồng ý";
            btnOk.Location = new Point(136, 138);
            btnOk.Size = new Size(80, 28);
            btnOk.Click += (s, e) => ExecuteGoalSeek();
            this.Controls.Add(btnOk);

            btnCancel = new Button();
            btnCancel.Text = "Hủy";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(222, 138);
            btnCancel.Size = new Size(80, 28);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void ExecuteGoalSeek()
        {
            if (_calcView == null || _calcView.CurrentSheet == null) return;

            string setRef = (txtSetCell.Text ?? "").Replace("$", "").Trim();
            string chgRef = (txtChangingCell.Text ?? "").Replace("$", "").Trim();
            string toValStr = (txtToValue.Text ?? "").Trim();

            uint setRow, setCol;
            if (!SparseMatrix.TryParseCellReference(setRef, out setRow, out setCol))
            {
                MessageBox.Show("Ô đặt giá trị không hợp lệ. Vui lòng nhập tọa độ như A1 hoặc $A$1.", "Goal Seek", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            uint chgRow, chgCol;
            if (!SparseMatrix.TryParseCellReference(chgRef, out chgRow, out chgCol))
            {
                MessageBox.Show("Ô cần thay đổi không hợp lệ. Vui lòng nhập tọa độ như B2 hoặc $B$2.", "Goal Seek", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double targetVal;
            if (!double.TryParse(toValStr, NumberStyles.Float, CultureInfo.InvariantCulture, out targetVal) &&
                !double.TryParse(toValStr, NumberStyles.Float, CultureInfo.CurrentCulture, out targetVal))
            {
                MessageBox.Show("Giá trị mục tiêu phải là một số hợp lệ.", "Goal Seek", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CellData setCell = _calcView.CurrentSheet.GetCell(setRow, setCol);
            if (setCell == null || string.IsNullOrEmpty(setCell.FormulaStr))
            {
                MessageBox.Show("Ô đặt giá trị (" + setRef + ") phải chứa một công thức.", "Goal Seek", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save undo state before modifying sheet
            _calcView.SaveUndoState();

            CellData chgCell = _calcView.CurrentSheet.GetOrCreateCell(chgRow, chgCol);
            double origVal = chgCell.NumericValue;
            if (double.IsNaN(origVal)) origVal = 0.0;

            // Secant method solver
            double x0 = origVal;
            double x1 = (Math.Abs(origVal) > 0.001) ? origVal * 1.05 : 1.0;
            if (Math.Abs(x1 - x0) < 1e-6) x1 = x0 + 1.0;

            // Evaluate at x0
            SetCellNum(chgCell, x0);
            FormulaEngine.EvaluateSheet(_calcView.CurrentSheet);
            double y0 = GetCurrentVal(setCell) - targetVal;

            if (Math.Abs(y0) <= 0.001)
            {
                ShowResultDialog(setRef, targetVal, targetVal, origVal, chgCell);
                return;
            }

            // Evaluate at x1
            SetCellNum(chgCell, x1);
            FormulaEngine.EvaluateSheet(_calcView.CurrentSheet);
            double y1 = GetCurrentVal(setCell) - targetVal;

            bool converged = false;
            int maxIter = 100;
            double xNext = x1;

            for (int iter = 0; iter < maxIter; iter++)
            {
                if (Math.Abs(y1) <= 0.001)
                {
                    converged = true;
                    xNext = x1;
                    break;
                }

                double dy = y1 - y0;
                if (Math.Abs(dy) < 1e-12)
                {
                    // Gradient flat or stuck, perturb
                    xNext = x1 + 0.1;
                }
                else
                {
                    xNext = x1 - y1 * (x1 - x0) / dy;
                }

                x0 = x1;
                y0 = y1;
                x1 = xNext;

                SetCellNum(chgCell, x1);
                FormulaEngine.EvaluateSheet(_calcView.CurrentSheet);
                y1 = GetCurrentVal(setCell) - targetVal;
            }

            if (!converged && Math.Abs(y1) <= 0.001)
            {
                converged = true;
                xNext = x1;
            }

            double finalActual = GetCurrentVal(setCell);
            ShowResultDialog(setRef, targetVal, finalActual, origVal, chgCell);
        }

        private void SetCellNum(CellData cell, double val)
        {
            cell.Type = CellType.Number;
            cell.NumericValue = val;
            cell.RawValue = val.ToString("G15", CultureInfo.InvariantCulture);
            cell.FormulaStr = null;
        }

        private double GetCurrentVal(CellData cell)
        {
            if (cell == null) return 0.0;
            if (!double.IsNaN(cell.NumericValue)) return cell.NumericValue;
            double v;
            if (double.TryParse(cell.EvaluatedStr ?? cell.RawValue ?? "", NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                return v;
            return 0.0;
        }

        private void ShowResultDialog(string setRef, double targetVal, double actualVal, double origVal, CellData chgCell)
        {
            string msg = string.Format(
                "Dò tìm mục tiêu cho ô {0} đã hoàn thành.\n\n" +
                "Giá trị mục tiêu: {1:N2}\n" +
                "Giá trị hiện tại: {2:N2}\n\n" +
                "Nhấn OK để giữ giá trị đã giải, hoặc Cancel để hoàn tác về giá trị ban đầu.",
                setRef, targetVal, actualVal);

            DialogResult dr = MessageBox.Show(msg, "Trạng thái Dò tìm Mục tiêu", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (dr == DialogResult.Cancel)
            {
                SetCellNum(chgCell, origVal);
                FormulaEngine.EvaluateSheet(_calcView.CurrentSheet);
            }

            if (_calcView.ParentForm != null) _calcView.ParentForm.Invalidate();
            this.Close();
        }
    }
}

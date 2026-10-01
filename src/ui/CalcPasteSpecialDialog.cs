using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public enum PasteSpecialType
    {
        All,
        Formulas,
        Values,
        Formats,
        Comments,
        AllExceptBorders,
        ColumnWidths,
        FormulasAndNumberFormats,
        ValuesAndNumberFormats
    }

    public enum PasteOperation
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public class CalcPasteSpecialDialog : Form
    {
        private RadioButton rbAll;
        private RadioButton rbFormulas;
        private RadioButton rbValues;
        private RadioButton rbFormats;
        private RadioButton rbComments;
        private RadioButton rbAllExceptBorders;
        private RadioButton rbColumnWidths;
        private RadioButton rbFormulasAndNumberFormats;
        private RadioButton rbValuesAndNumberFormats;

        private RadioButton rbOpNone;
        private RadioButton rbOpAdd;
        private RadioButton rbOpSubtract;
        private RadioButton rbOpMultiply;
        private RadioButton rbOpDivide;

        private CheckBox chkSkipBlanks;
        private CheckBox chkTranspose;

        private Button btnOk;
        private Button btnCancel;

        public PasteSpecialType SelectedType { get; private set; }
        public PasteOperation SelectedOperation { get; private set; }
        public bool SkipBlanks { get; private set; }
        public bool Transpose { get; private set; }

        public CalcPasteSpecialDialog()
        {
            SelectedType = PasteSpecialType.All;
            SelectedOperation = PasteOperation.None;
            SkipBlanks = false;
            Transpose = false;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Paste Special (Dán đặc biệt)";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(420, 370);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Group 1: Paste Options
            GroupBox gbPaste = new GroupBox();
            gbPaste.Text = "Dán (Paste)";
            gbPaste.Location = new Point(16, 12);
            gbPaste.Size = new Size(230, 260);

            rbAll = new RadioButton { Text = "Tất cả (All)", Location = new Point(14, 22), AutoSize = true, Checked = true };
            rbFormulas = new RadioButton { Text = "Công thức (Formulas)", Location = new Point(14, 46), AutoSize = true };
            rbValues = new RadioButton { Text = "Giá trị (Values)", Location = new Point(14, 70), AutoSize = true };
            rbFormats = new RadioButton { Text = "Định dạng (Formats)", Location = new Point(14, 94), AutoSize = true };
            rbComments = new RadioButton { Text = "Ghi chú (Comments)", Location = new Point(14, 118), AutoSize = true };
            rbAllExceptBorders = new RadioButton { Text = "Trừ đường viền (All except borders)", Location = new Point(14, 142), AutoSize = true };
            rbColumnWidths = new RadioButton { Text = "Độ rộng cột (Column widths)", Location = new Point(14, 166), AutoSize = true };
            rbFormulasAndNumberFormats = new RadioButton { Text = "Công thức & Số (Formulas & Numbers)", Location = new Point(14, 190), AutoSize = true };
            rbValuesAndNumberFormats = new RadioButton { Text = "Giá trị & Số (Values & Numbers)", Location = new Point(14, 214), AutoSize = true };

            gbPaste.Controls.AddRange(new Control[] {
                rbAll, rbFormulas, rbValues, rbFormats, rbComments,
                rbAllExceptBorders, rbColumnWidths, rbFormulasAndNumberFormats, rbValuesAndNumberFormats
            });
            this.Controls.Add(gbPaste);

            // Group 2: Operation
            GroupBox gbOp = new GroupBox();
            gbOp.Text = "Phép tính (Operation)";
            gbOp.Location = new Point(256, 12);
            gbOp.Size = new Size(150, 165);

            rbOpNone = new RadioButton { Text = "Không (None)", Location = new Point(14, 22), AutoSize = true, Checked = true };
            rbOpAdd = new RadioButton { Text = "Cộng (Add)", Location = new Point(14, 48), AutoSize = true };
            rbOpSubtract = new RadioButton { Text = "Trừ (Subtract)", Location = new Point(14, 74), AutoSize = true };
            rbOpMultiply = new RadioButton { Text = "Nhân (Multiply)", Location = new Point(14, 100), AutoSize = true };
            rbOpDivide = new RadioButton { Text = "Chia (Divide)", Location = new Point(14, 126), AutoSize = true };

            gbOp.Controls.AddRange(new Control[] { rbOpNone, rbOpAdd, rbOpSubtract, rbOpMultiply, rbOpDivide });
            this.Controls.Add(gbOp);

            // Checkboxes
            chkSkipBlanks = new CheckBox();
            chkSkipBlanks.Text = "Bỏ qua ô trống (Skip blanks)";
            chkSkipBlanks.Location = new Point(20, 282);
            chkSkipBlanks.AutoSize = true;
            this.Controls.Add(chkSkipBlanks);

            chkTranspose = new CheckBox();
            chkTranspose.Text = "Hoán vị hàng / cột (Transpose)";
            chkTranspose.Location = new Point(20, 306);
            chkTranspose.AutoSize = true;
            this.Controls.Add(chkTranspose);

            // Buttons
            btnOk = new Button();
            btnOk.Text = "Đồng ý";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(236, 328);
            btnOk.Size = new Size(80, 28);
            btnOk.Click += (s, e) =>
            {
                if (rbFormulas.Checked) SelectedType = PasteSpecialType.Formulas;
                else if (rbValues.Checked) SelectedType = PasteSpecialType.Values;
                else if (rbFormats.Checked) SelectedType = PasteSpecialType.Formats;
                else if (rbComments.Checked) SelectedType = PasteSpecialType.Comments;
                else if (rbAllExceptBorders.Checked) SelectedType = PasteSpecialType.AllExceptBorders;
                else if (rbColumnWidths.Checked) SelectedType = PasteSpecialType.ColumnWidths;
                else if (rbFormulasAndNumberFormats.Checked) SelectedType = PasteSpecialType.FormulasAndNumberFormats;
                else if (rbValuesAndNumberFormats.Checked) SelectedType = PasteSpecialType.ValuesAndNumberFormats;
                else SelectedType = PasteSpecialType.All;

                if (rbOpAdd.Checked) SelectedOperation = PasteOperation.Add;
                else if (rbOpSubtract.Checked) SelectedOperation = PasteOperation.Subtract;
                else if (rbOpMultiply.Checked) SelectedOperation = PasteOperation.Multiply;
                else if (rbOpDivide.Checked) SelectedOperation = PasteOperation.Divide;
                else SelectedOperation = PasteOperation.None;

                SkipBlanks = chkSkipBlanks.Checked;
                Transpose = chkTranspose.Checked;

                this.Close();
            };
            this.Controls.Add(btnOk);

            btnCancel = new Button();
            btnCancel.Text = "Hủy";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(324, 328);
            btnCancel.Size = new Size(80, 28);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public enum InsertShiftMode
    {
        ShiftCellsRight,
        ShiftCellsDown,
        EntireRow,
        EntireColumn
    }

    public enum DeleteShiftMode
    {
        ShiftCellsLeft,
        ShiftCellsUp,
        EntireRow,
        EntireColumn
    }

    public class CalcInsertDialogForm : Form
    {
        private RadioButton rbShiftRight;
        private RadioButton rbShiftDown;
        private RadioButton rbEntireRow;
        private RadioButton rbEntireCol;
        private Button btnOK;
        private Button btnCancel;
        private GroupBox gbInsert;

        public InsertShiftMode SelectedMode
        {
            get
            {
                if (rbShiftDown.Checked) return InsertShiftMode.ShiftCellsDown;
                if (rbEntireRow.Checked) return InsertShiftMode.EntireRow;
                if (rbEntireCol.Checked) return InsertShiftMode.EntireColumn;
                return InsertShiftMode.ShiftCellsRight;
            }
        }

        public CalcInsertDialogForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Insert";
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(218, 185);
            this.BackColor = Color.FromArgb(243, 243, 243);

            gbInsert = new GroupBox();
            gbInsert.Text = "Insert";
            gbInsert.Location = new Point(12, 10);
            gbInsert.Size = new Size(194, 126);
            gbInsert.BackColor = Color.Transparent;

            rbShiftRight = new RadioButton();
            rbShiftRight.Text = "Shift cells right";
            rbShiftRight.Location = new Point(15, 20);
            rbShiftRight.Size = new Size(160, 22);
            rbShiftRight.Checked = true;

            rbShiftDown = new RadioButton();
            rbShiftDown.Text = "Shift cells down";
            rbShiftDown.Location = new Point(15, 44);
            rbShiftDown.Size = new Size(160, 22);

            rbEntireRow = new RadioButton();
            rbEntireRow.Text = "Entire row";
            rbEntireRow.Location = new Point(15, 68);
            rbEntireRow.Size = new Size(160, 22);

            rbEntireCol = new RadioButton();
            rbEntireCol.Text = "Entire column";
            rbEntireCol.Location = new Point(15, 92);
            rbEntireCol.Size = new Size(160, 22);

            gbInsert.Controls.Add(rbShiftRight);
            gbInsert.Controls.Add(rbShiftDown);
            gbInsert.Controls.Add(rbEntireRow);
            gbInsert.Controls.Add(rbEntireCol);

            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Location = new Point(16, 144);
            btnOK.Size = new Size(88, 28);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.BackColor = Color.FromArgb(250, 250, 250);
            btnOK.FlatStyle = FlatStyle.System;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(114, 144);
            btnCancel.Size = new Size(88, 28);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.BackColor = Color.FromArgb(250, 250, 250);
            btnCancel.FlatStyle = FlatStyle.System;

            this.Controls.Add(gbInsert);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }

    public class CalcDeleteDialogForm : Form
    {
        private RadioButton rbShiftLeft;
        private RadioButton rbShiftUp;
        private RadioButton rbEntireRow;
        private RadioButton rbEntireCol;
        private Button btnOK;
        private Button btnCancel;
        private GroupBox gbDelete;

        public DeleteShiftMode SelectedMode
        {
            get
            {
                if (rbShiftUp.Checked) return DeleteShiftMode.ShiftCellsUp;
                if (rbEntireRow.Checked) return DeleteShiftMode.EntireRow;
                if (rbEntireCol.Checked) return DeleteShiftMode.EntireColumn;
                return DeleteShiftMode.ShiftCellsLeft;
            }
        }

        public CalcDeleteDialogForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Delete";
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(218, 185);
            this.BackColor = Color.FromArgb(243, 243, 243);

            gbDelete = new GroupBox();
            gbDelete.Text = "Delete";
            gbDelete.Location = new Point(12, 10);
            gbDelete.Size = new Size(194, 126);
            gbDelete.BackColor = Color.Transparent;

            rbShiftLeft = new RadioButton();
            rbShiftLeft.Text = "Shift cells left";
            rbShiftLeft.Location = new Point(15, 20);
            rbShiftLeft.Size = new Size(160, 22);
            rbShiftLeft.Checked = true;

            rbShiftUp = new RadioButton();
            rbShiftUp.Text = "Shift cells up";
            rbShiftUp.Location = new Point(15, 44);
            rbShiftUp.Size = new Size(160, 22);

            rbEntireRow = new RadioButton();
            rbEntireRow.Text = "Entire row";
            rbEntireRow.Location = new Point(15, 68);
            rbEntireRow.Size = new Size(160, 22);

            rbEntireCol = new RadioButton();
            rbEntireCol.Text = "Entire column";
            rbEntireCol.Location = new Point(15, 92);
            rbEntireCol.Size = new Size(160, 22);

            gbDelete.Controls.Add(rbShiftLeft);
            gbDelete.Controls.Add(rbShiftUp);
            gbDelete.Controls.Add(rbEntireRow);
            gbDelete.Controls.Add(rbEntireCol);

            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Location = new Point(16, 144);
            btnOK.Size = new Size(88, 28);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.BackColor = Color.FromArgb(250, 250, 250);
            btnOK.FlatStyle = FlatStyle.System;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(114, 144);
            btnCancel.Size = new Size(88, 28);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.BackColor = Color.FromArgb(250, 250, 250);
            btnCancel.FlatStyle = FlatStyle.System;

            this.Controls.Add(gbDelete);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}

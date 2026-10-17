using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class DataValidationDialog : Form
    {
        public DataValidationRule Rule { get; private set; }
        public bool IsCleared { get; private set; } = false;

        private TabControl tabControl;
        private TabPage tabSettings;
        private TabPage tabInputMessage;
        private TabPage tabErrorAlert;

        // Settings tab controls
        private ComboBox cboAllow;
        private ComboBox cboOperator;
        private TextBox txtFormula1;
        private TextBox txtFormula2;
        private Label lblFormula1;
        private Label lblFormula2;
        private CheckBox chkIgnoreBlank;
        private CheckBox chkInCellDropdown;

        // Input message tab controls
        private CheckBox chkShowInputMessage;
        private TextBox txtInputTitle;
        private TextBox txtInputMessage;

        // Error alert tab controls
        private CheckBox chkShowErrorMessage;
        private ComboBox cboErrorStyle;
        private TextBox txtErrorTitle;
        private TextBox txtErrorMessage;

        public DataValidationDialog(DataValidationRule existingRule = null)
        {
            Rule = existingRule != null ? existingRule.Clone() : new DataValidationRule();
            InitializeComponent();
            LoadRuleData();
        }

        public DataValidationDialog(DataValidationRule existingRule, uint startRow, uint startCol, uint endRow, uint endCol)
        {
            Rule = existingRule != null ? existingRule.Clone() : new DataValidationRule(startRow, startCol, endRow, endCol);
            InitializeComponent();
            LoadRuleData();
        }

        private void InitializeComponent()
        {
            this.Text = "Xác thực dữ liệu (Data Validation)";
            this.Size = new Size(460, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            tabControl = new TabControl { Dock = DockStyle.Top, Height = 320 };
            tabSettings = new TabPage("Cài đặt");
            tabInputMessage = new TabPage("Thông báo nhập");
            tabErrorAlert = new TabPage("Cảnh báo lỗi");
            tabControl.TabPages.AddRange(new TabPage[] { tabSettings, tabInputMessage, tabErrorAlert });

            // ==================== TAB 1: Cài đặt ====================
            Label lblAllow = new Label { Text = "Cho phép (Allow):", Location = new Point(16, 16), AutoSize = true };
            cboAllow = new ComboBox { Location = new Point(16, 38), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboAllow.Items.AddRange(new object[] {
                "Bất kỳ giá trị nào (Any)",
                "Số nguyên (Whole number)",
                "Số thập phân (Decimal)",
                "Danh sách (List)",
                "Ngày tháng (Date)",
                "Độ dài văn bản (Text length)",
                "Tùy chỉnh (Custom Formula)"
            });

            chkIgnoreBlank = new CheckBox { Text = "Bỏ qua ô trống", Location = new Point(220, 38), AutoSize = true, Checked = true };
            chkInCellDropdown = new CheckBox { Text = "Danh sách thả xuống trong ô", Location = new Point(220, 68), AutoSize = true, Checked = true };

            Label lblData = new Label { Text = "Dữ liệu (Data):", Location = new Point(16, 75), AutoSize = true };
            cboOperator = new ComboBox { Location = new Point(16, 96), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboOperator.Items.AddRange(new object[] {
                "Ở giữa (between)",
                "Không ở giữa (not between)",
                "Bằng (equal to)",
                "Không bằng (not equal to)",
                "Lớn hơn (greater than)",
                "Nhỏ hơn (less than)",
                "Lớn hơn hoặc bằng (>=)",
                "Nhỏ hơn hoặc bằng (<=)"
            });

            lblFormula1 = new Label { Text = "Giá trị tối thiểu / Nguồn:", Location = new Point(16, 135), AutoSize = true };
            txtFormula1 = new TextBox { Location = new Point(16, 156), Width = 380 };

            lblFormula2 = new Label { Text = "Giá trị tối đa:", Location = new Point(16, 195), AutoSize = true };
            txtFormula2 = new TextBox { Location = new Point(16, 216), Width = 380 };

            tabSettings.Controls.AddRange(new Control[] {
                lblAllow, cboAllow, chkIgnoreBlank, chkInCellDropdown,
                lblData, cboOperator, lblFormula1, txtFormula1, lblFormula2, txtFormula2
            });

            cboAllow.SelectedIndexChanged += (s, e) => UpdateSettingsUI();
            cboOperator.SelectedIndexChanged += (s, e) => UpdateOperatorUI();

            // ==================== TAB 2: Thông báo nhập ====================
            chkShowInputMessage = new CheckBox { Text = "Hiển thị thông báo khi chọn ô này", Location = new Point(16, 16), AutoSize = true, Checked = true };
            Label lblInTitle = new Label { Text = "Tiêu đề:", Location = new Point(16, 50), AutoSize = true };
            txtInputTitle = new TextBox { Location = new Point(16, 72), Width = 380 };
            Label lblInMsg = new Label { Text = "Nội dung thông báo nhập:", Location = new Point(16, 110), AutoSize = true };
            txtInputMessage = new TextBox { Location = new Point(16, 132), Width = 380, Height = 100, Multiline = true };
            tabInputMessage.Controls.AddRange(new Control[] { chkShowInputMessage, lblInTitle, txtInputTitle, lblInMsg, txtInputMessage });

            // ==================== TAB 3: Cảnh báo lỗi ====================
            chkShowErrorMessage = new CheckBox { Text = "Hiển thị cảnh báo lỗi khi nhập dữ liệu không hợp lệ", Location = new Point(16, 16), AutoSize = true, Checked = true };
            Label lblStyle = new Label { Text = "Kiểu cảnh báo (Style):", Location = new Point(16, 50), AutoSize = true };
            cboErrorStyle = new ComboBox { Location = new Point(16, 72), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboErrorStyle.Items.AddRange(new object[] { "⛔ Dừng lại (Stop)", "⚠️ Cảnh báo (Warning)", "ℹ️ Thông tin (Information)" });

            Label lblErrTitle = new Label { Text = "Tiêu đề:", Location = new Point(180, 50), AutoSize = true };
            txtErrorTitle = new TextBox { Location = new Point(180, 72), Width = 216 };
            Label lblErrMsg = new Label { Text = "Thông báo lỗi:", Location = new Point(16, 110), AutoSize = true };
            txtErrorMessage = new TextBox { Location = new Point(16, 132), Width = 380, Height = 100, Multiline = true };
            tabErrorAlert.Controls.AddRange(new Control[] { chkShowErrorMessage, lblStyle, cboErrorStyle, lblErrTitle, txtErrorTitle, lblErrMsg, txtErrorMessage });

            // Bottom Buttons
            Button btnClearAll = new Button { Text = "Xóa tất cả", Location = new Point(16, 336), Width = 90, Height = 30 };
            btnClearAll.Click += (s, e) =>
            {
                IsCleared = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnOk = new Button { Text = "OK", Location = new Point(245, 336), Width = 85, Height = 30, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White };
            btnOk.Click += (s, e) =>
            {
                SaveRuleData();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnCancel = new Button { Text = "Hủy bỏ", Location = new Point(340, 336), Width = 85, Height = 30 };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { tabControl, btnClearAll, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void LoadRuleData()
        {
            switch (Rule.ValidationType)
            {
                case DataValidationType.AnyValue: cboAllow.SelectedIndex = 0; break;
                case DataValidationType.WholeNumber: cboAllow.SelectedIndex = 1; break;
                case DataValidationType.Decimal: cboAllow.SelectedIndex = 2; break;
                case DataValidationType.List: cboAllow.SelectedIndex = 3; break;
                case DataValidationType.Date: cboAllow.SelectedIndex = 4; break;
                case DataValidationType.TextLength: cboAllow.SelectedIndex = 5; break;
                case DataValidationType.Custom: cboAllow.SelectedIndex = 6; break;
                default: cboAllow.SelectedIndex = 0; break;
            }

            cboOperator.SelectedIndex = Math.Max(0, Math.Min(7, (int)Rule.Operator));
            txtFormula1.Text = Rule.Formula1 ?? "";
            txtFormula2.Text = Rule.Formula2 ?? "";
            chkIgnoreBlank.Checked = Rule.AllowBlank;
            chkInCellDropdown.Checked = Rule.ShowDropdown;

            chkShowInputMessage.Checked = Rule.ShowInputMessage;
            txtInputTitle.Text = Rule.InputTitle ?? "";
            txtInputMessage.Text = Rule.InputMessage ?? "";

            chkShowErrorMessage.Checked = Rule.ShowErrorMessage;
            cboErrorStyle.SelectedIndex = Math.Max(0, Math.Min(2, (int)Rule.ErrorStyle));
            txtErrorTitle.Text = Rule.ErrorTitle ?? "Giá trị không hợp lệ";
            txtErrorMessage.Text = Rule.ErrorMessage ?? "Giá trị bạn vừa nhập không đáp ứng các ràng buộc của ô này.";

            UpdateSettingsUI();
        }

        private void SaveRuleData()
        {
            switch (cboAllow.SelectedIndex)
            {
                case 0: Rule.ValidationType = DataValidationType.AnyValue; break;
                case 1: Rule.ValidationType = DataValidationType.WholeNumber; break;
                case 2: Rule.ValidationType = DataValidationType.Decimal; break;
                case 3: Rule.ValidationType = DataValidationType.List; break;
                case 4: Rule.ValidationType = DataValidationType.Date; break;
                case 5: Rule.ValidationType = DataValidationType.TextLength; break;
                case 6: Rule.ValidationType = DataValidationType.Custom; break;
                default: Rule.ValidationType = DataValidationType.AnyValue; break;
            }

            Rule.Operator = (DataValidationOperator)Math.Max(0, cboOperator.SelectedIndex);
            Rule.Formula1 = txtFormula1.Text.Trim();
            Rule.Formula2 = txtFormula2.Text.Trim();
            Rule.AllowBlank = chkIgnoreBlank.Checked;
            Rule.ShowDropdown = chkInCellDropdown.Checked;

            Rule.ShowInputMessage = chkShowInputMessage.Checked;
            Rule.InputTitle = txtInputTitle.Text.Trim();
            Rule.InputMessage = txtInputMessage.Text.Trim();

            Rule.ShowErrorMessage = chkShowErrorMessage.Checked;
            Rule.ErrorStyle = (DataValidationErrorStyle)Math.Max(0, cboErrorStyle.SelectedIndex);
            Rule.ErrorTitle = txtErrorTitle.Text.Trim();
            Rule.ErrorMessage = txtErrorMessage.Text.Trim();
        }

        private void UpdateSettingsUI()
        {
            int idx = cboAllow.SelectedIndex;
            if (idx == 0) // Any value
            {
                cboOperator.Enabled = false;
                txtFormula1.Enabled = false;
                txtFormula2.Enabled = false;
                chkInCellDropdown.Enabled = false;
            }
            else if (idx == 3) // List
            {
                cboOperator.Enabled = false;
                chkInCellDropdown.Enabled = true;
                lblFormula1.Text = "Nguồn dữ liệu (vd: A1:A10 hoặc Táo, Cam, Chuối):";
                txtFormula1.Enabled = true;
                lblFormula2.Visible = false;
                txtFormula2.Visible = false;
            }
            else if (idx == 6) // Custom
            {
                cboOperator.Enabled = false;
                chkInCellDropdown.Enabled = false;
                lblFormula1.Text = "Công thức (Formula, vd: =A1>0):";
                txtFormula1.Enabled = true;
                lblFormula2.Visible = false;
                txtFormula2.Visible = false;
            }
            else // Numbers, Dates, Length
            {
                cboOperator.Enabled = true;
                chkInCellDropdown.Enabled = false;
                lblFormula1.Text = (cboOperator.SelectedIndex <= 1) ? "Giá trị tối thiểu:" : "Giá trị:";
                txtFormula1.Enabled = true;
                UpdateOperatorUI();
            }
        }

        private void UpdateOperatorUI()
        {
            int idx = cboAllow.SelectedIndex;
            if (idx == 3 || idx == 6 || idx == 0) return;

            bool isBetween = (cboOperator.SelectedIndex == 0 || cboOperator.SelectedIndex == 1);
            lblFormula2.Visible = isBetween;
            txtFormula2.Visible = isBetween;
            lblFormula1.Text = isBetween ? "Giá trị tối thiểu:" : "Giá trị:";
        }
    }
}

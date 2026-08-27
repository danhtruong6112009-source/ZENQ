using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    #region Excel Table Data Model & Information
    public class ExcelTableInfo
    {
        public string Name { get; set; }
        public uint StartRow { get; set; }
        public uint EndRow { get; set; }
        public uint StartCol { get; set; }
        public uint EndCol { get; set; }
        public bool HasHeaders { get; set; }
        public bool HasTotalRow { get; set; }
        public bool BandedRows { get; set; }
        public bool FirstColumn { get; set; }
        public bool LastColumn { get; set; }
        public bool BandedColumns { get; set; }
        public bool ShowFilterButtons { get; set; }
        public int StyleIndex { get; set; }
        public Dictionary<uint, HashSet<string>> ColumnFilters { get; set; }
        public Dictionary<uint, Rectangle> HeaderFilterButtonBounds { get; set; }
        public Rectangle ResizeCornerBounds { get; set; }

        public ExcelTableInfo(string name, uint sRow, uint eRow, uint sCol, uint eCol, bool hasHeaders = true, int styleIdx = 1)
        {
            Name = !string.IsNullOrEmpty(name) ? name : "Table1";
            StartRow = Math.Min(sRow, eRow);
            EndRow = Math.Max(sRow, eRow);
            StartCol = Math.Min(sCol, eCol);
            EndCol = Math.Max(sCol, eCol);
            HasHeaders = hasHeaders;
            HasTotalRow = false;
            BandedRows = true;
            FirstColumn = false;
            LastColumn = false;
            BandedColumns = false;
            ShowFilterButtons = true;
            StyleIndex = styleIdx;
            ColumnFilters = new Dictionary<uint, HashSet<string>>();
            HeaderFilterButtonBounds = new Dictionary<uint, Rectangle>();
            ResizeCornerBounds = Rectangle.Empty;
        }

        public bool ContainsCell(uint r, uint c)
        {
            return r >= StartRow && r <= EndRow && c >= StartCol && c <= EndCol;
        }

        public bool IntersectsRange(uint sR, uint eR, uint sC, uint eC)
        {
            uint minR = Math.Min(sR, eR);
            uint maxR = Math.Max(sR, eR);
            uint minC = Math.Min(sC, eC);
            uint maxC = Math.Max(sC, eC);
            return !(maxR < StartRow || minR > EndRow || maxC < StartCol || minC > EndCol);
        }
    }
    #endregion

    #region Create Table Dialog (Ảnh 3)
    public class CreateTableDialogForm : Form
    {
        private TextBox txtRange;
        private CheckBox chkHeaders;
        private Button btnOk;
        private Button btnCancel;
        private CalcView _calcView;

        public CreateTableDialogForm(CalcView calcView)
        {
            _calcView = calcView;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Create Table";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(340, 165);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            Label lblPrompt = new Label();
            lblPrompt.Text = "Where is the data for your table?";
            lblPrompt.Location = new Point(20, 18);
            lblPrompt.AutoSize = true;
            lblPrompt.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Controls.Add(lblPrompt);

            // Tọa độ vùng chọn hiện hành
            uint minR = Math.Min(_calcView.SelectedRow, _calcView.SelEndRow);
            uint maxR = Math.Max(_calcView.SelectedRow, _calcView.SelEndRow);
            uint minC = Math.Min(_calcView.SelectedCol, _calcView.SelEndCol);
            uint maxC = Math.Max(_calcView.SelectedCol, _calcView.SelEndCol);
            string rangeStr = string.Format("=${0}${1}:${2}${3}",
                SparseMatrix.ColIndexToName(minC), minR,
                SparseMatrix.ColIndexToName(maxC), maxR);

            txtRange = new TextBox();
            txtRange.Text = rangeStr;
            txtRange.Location = new Point(22, 45);
            txtRange.Size = new Size(255, 25);
            this.Controls.Add(txtRange);

            Button btnSelect = new Button();
            btnSelect.Text = "⬆";
            btnSelect.Location = new Point(282, 44);
            btnSelect.Size = new Size(34, 27);
            btnSelect.FlatStyle = FlatStyle.System;
            this.Controls.Add(btnSelect);

            chkHeaders = new CheckBox();
            chkHeaders.Text = "My table has headers";
            chkHeaders.Checked = true;
            chkHeaders.Location = new Point(22, 82);
            chkHeaders.AutoSize = true;
            this.Controls.Add(chkHeaders);

            btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(148, 120);
            btnOk.Size = new Size(80, 28);
            btnOk.Click += (s, e) => ApplyCreateTable();
            this.Controls.Add(btnOk);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(236, 120);
            btnCancel.Size = new Size(80, 28);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void ApplyCreateTable()
        {
            if (_calcView == null) return;
            _calcView.CreateTableFromRange(txtRange.Text, chkHeaders.Checked);
            this.Close();
        }
    }
    #endregion

    #region Create PivotTable Dialog (Ảnh 1 & Ảnh 2)
    public class CreatePivotTableDialogForm : Form
    {
        private RadioButton rbRange;
        private TextBox txtRange;
        private RadioButton rbExternal;
        private Button btnChooseConn;
        private Label lblConnName;
        private RadioButton rbDataModel;

        private RadioButton rbNewWorksheet;
        private RadioButton rbExistingWorksheet;
        private TextBox txtLocation;
        private CheckBox chkAddDataModel;

        private Button btnOk;
        private Button btnCancel;
        private CalcView _calcView;

        public CreatePivotTableDialogForm(CalcView calcView)
        {
            _calcView = calcView;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Create PivotTable";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(420, 390);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Group 1: Choose data
            Label lblGroup1 = new Label();
            lblGroup1.Text = "Choose the data that you want to analyze";
            lblGroup1.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblGroup1.Location = new Point(16, 14);
            lblGroup1.AutoSize = true;
            this.Controls.Add(lblGroup1);

            rbRange = new RadioButton();
            rbRange.Text = "Select a table or range";
            rbRange.Checked = true;
            rbRange.Location = new Point(26, 38);
            rbRange.AutoSize = true;
            this.Controls.Add(rbRange);

            Label lblRange = new Label();
            lblRange.Text = "Table/Range:";
            lblRange.Location = new Point(48, 65);
            lblRange.AutoSize = true;
            this.Controls.Add(lblRange);

            uint minR = Math.Min(_calcView.SelectedRow, _calcView.SelEndRow);
            uint maxR = Math.Max(_calcView.SelectedRow, _calcView.SelEndRow);
            uint minC = Math.Min(_calcView.SelectedCol, _calcView.SelEndCol);
            uint maxC = Math.Max(_calcView.SelectedCol, _calcView.SelEndCol);
            string curSheet = (_calcView.ActiveSheetIndex < _calcView.SheetNames.Count) ? _calcView.SheetNames[_calcView.ActiveSheetIndex] : "Sheet1";
            string rangeStr = string.Format("{0}!${1}${2}:${3}${4}", curSheet,
                SparseMatrix.ColIndexToName(minC), minR,
                SparseMatrix.ColIndexToName(maxC), maxR);

            txtRange = new TextBox();
            txtRange.Text = rangeStr;
            txtRange.Location = new Point(130, 62);
            txtRange.Size = new Size(230, 23);
            this.Controls.Add(txtRange);

            Button btnUpRange = new Button();
            btnUpRange.Text = "⬆";
            btnUpRange.Location = new Point(366, 61);
            btnUpRange.Size = new Size(32, 25);
            this.Controls.Add(btnUpRange);

            rbExternal = new RadioButton();
            rbExternal.Text = "Use an external data source";
            rbExternal.Location = new Point(26, 96);
            rbExternal.AutoSize = true;
            this.Controls.Add(rbExternal);

            btnChooseConn = new Button();
            btnChooseConn.Text = "Choose Connection...";
            btnChooseConn.Location = new Point(48, 120);
            btnChooseConn.Size = new Size(150, 26);
            btnChooseConn.Click += (s, e) => {
                MessageBox.Show("Kết nối nguồn dữ liệu bên ngoài (SQL Server, OData, Power BI, CSV Data Model).", "Choose Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            this.Controls.Add(btnChooseConn);

            lblConnName = new Label();
            lblConnName.Text = "Connection name:";
            lblConnName.ForeColor = Color.Gray;
            lblConnName.Location = new Point(48, 150);
            lblConnName.AutoSize = true;
            this.Controls.Add(lblConnName);

            rbDataModel = new RadioButton();
            rbDataModel.Text = "Use this workbook's Data Model";
            rbDataModel.Location = new Point(26, 172);
            rbDataModel.AutoSize = true;
            this.Controls.Add(rbDataModel);

            // Divider line
            Label div1 = new Label();
            div1.BorderStyle = BorderStyle.Fixed3D;
            div1.Location = new Point(16, 202);
            div1.Size = new Size(385, 2);
            this.Controls.Add(div1);

            // Group 2: Where to place
            Label lblGroup2 = new Label();
            lblGroup2.Text = "Choose where you want the PivotTable report to be placed";
            lblGroup2.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblGroup2.Location = new Point(16, 212);
            lblGroup2.AutoSize = true;
            this.Controls.Add(lblGroup2);

            rbNewWorksheet = new RadioButton();
            rbNewWorksheet.Text = "New Worksheet";
            rbNewWorksheet.Location = new Point(26, 236);
            rbNewWorksheet.AutoSize = true;
            this.Controls.Add(rbNewWorksheet);

            rbExistingWorksheet = new RadioButton();
            rbExistingWorksheet.Text = "Existing Worksheet";
            rbExistingWorksheet.Checked = true;
            rbExistingWorksheet.Location = new Point(26, 260);
            rbExistingWorksheet.AutoSize = true;
            this.Controls.Add(rbExistingWorksheet);

            Label lblLoc = new Label();
            lblLoc.Text = "Location:";
            lblLoc.Location = new Point(48, 288);
            lblLoc.AutoSize = true;
            this.Controls.Add(lblLoc);

            txtLocation = new TextBox();
            txtLocation.Text = string.Format("{0}!$G$3", curSheet);
            txtLocation.Location = new Point(130, 285);
            txtLocation.Size = new Size(230, 23);
            this.Controls.Add(txtLocation);

            Button btnUpLoc = new Button();
            btnUpLoc.Text = "⬆";
            btnUpLoc.Location = new Point(366, 284);
            btnUpLoc.Size = new Size(32, 25);
            this.Controls.Add(btnUpLoc);

            // Group 3: Multiple tables
            chkAddDataModel = new CheckBox();
            chkAddDataModel.Text = "Add this data to the Data Model";
            chkAddDataModel.Location = new Point(26, 320);
            chkAddDataModel.AutoSize = true;
            this.Controls.Add(chkAddDataModel);

            btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(232, 350);
            btnOk.Size = new Size(80, 28);
            btnOk.Click += (s, e) => ApplyCreatePivotTable();
            this.Controls.Add(btnOk);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(320, 350);
            btnCancel.Size = new Size(80, 28);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void ApplyCreatePivotTable()
        {
            if (_calcView == null) return;
            _calcView.CreatePivotTable(txtRange.Text, rbNewWorksheet.Checked, txtLocation.Text);
            this.Close();
        }
    }
    #endregion

    #region Table Column Filter & Sort Dropdown Popup Form (Ảnh 4)
    public class TableColumnFilterPopupForm : Form
    {
        private CalcView _calcView;
        private ExcelTableInfo _table;
        private uint _colIndex;
        private string _colName;
        private CheckedListBox clbValues;
        private TextBox txtSearch;
        private List<string> _allDistinctValues;

        public TableColumnFilterPopupForm(CalcView calcView, ExcelTableInfo table, uint colIndex, Point screenLocation)
        {
            _calcView = calcView;
            _table = table;
            _colIndex = colIndex;
            InitUI(screenLocation);
        }

        private void InitUI(Point screenLocation)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = screenLocation;
            this.Size = new Size(240, 350);
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            _colName = _calcView.CurrentSheet.GetCell(_table.StartRow, _colIndex) != null ?
                _calcView.CurrentSheet.GetCell(_table.StartRow, _colIndex).GetDisplayText() : ("Column" + _colIndex);

            // Panel viền ngoài
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(mainPanel);

            int y = 6;

            // 1. Sort A to Z
            Button btnSortAsc = CreateMenuItem("A↓  Sort A to Z", y);
            btnSortAsc.Click += (s, e) => {
                _calcView.SortTableColumn(_table, _colIndex, true);
                this.Close();
            };
            mainPanel.Controls.Add(btnSortAsc);
            y += 26;

            // 2. Sort Z to A
            Button btnSortDesc = CreateMenuItem("Z↓  Sort Z to A", y);
            btnSortDesc.Click += (s, e) => {
                _calcView.SortTableColumn(_table, _colIndex, false);
                this.Close();
            };
            mainPanel.Controls.Add(btnSortDesc);
            y += 26;

            // Divider
            Panel d1 = new Panel { Height = 1, BackColor = Color.FromArgb(226, 232, 240), Location = new Point(6, y + 2), Width = 224 };
            mainPanel.Controls.Add(d1);
            y += 6;

            // 3. Clear Filter
            Button btnClearFilter = CreateMenuItem("✕  Clear Filter From \"" + _colName + "\"", y);
            btnClearFilter.Click += (s, e) => {
                _calcView.ClearColumnFilter(_table, _colIndex);
                this.Close();
            };
            mainPanel.Controls.Add(btnClearFilter);
            y += 26;

            // Divider
            Panel d2 = new Panel { Height = 1, BackColor = Color.FromArgb(226, 232, 240), Location = new Point(6, y + 2), Width = 224 };
            mainPanel.Controls.Add(d2);
            y += 6;

            // 4. Search Box
            Panel pnlSearch = new Panel { Location = new Point(10, y), Size = new Size(216, 24), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            txtSearch = new TextBox { Location = new Point(2, 2), Size = new Size(185, 20), BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 8.5f) };
            txtSearch.TextChanged += (s, e) => FilterCheckList();
            Label lblMag = new Label { Text = "🔍", Location = new Point(190, 2), AutoSize = true, Font = new Font("Segoe UI", 8f) };
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(lblMag);
            mainPanel.Controls.Add(pnlSearch);
            y += 28;

            // 5. CheckedListBox of Values
            _allDistinctValues = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            for (uint r = _table.StartRow + 1; r <= _table.EndRow; r++)
            {
                CellData cell = _calcView.CurrentSheet.GetCell(r, _colIndex);
                string val = (cell != null && !string.IsNullOrEmpty(cell.GetDisplayText())) ? cell.GetDisplayText() : "(Blanks)";
                if (seen.Add(val))
                {
                    _allDistinctValues.Add(val);
                }
            }
            _allDistinctValues.Sort();

            clbValues = new CheckedListBox();
            clbValues.Location = new Point(10, y);
            clbValues.Size = new Size(216, 140);
            clbValues.BorderStyle = BorderStyle.FixedSingle;
            clbValues.Font = new Font("Segoe UI", 8.5f);
            clbValues.CheckOnClick = true;
            mainPanel.Controls.Add(clbValues);

            PopulateCheckList("");

            y += 146;

            // 6. Action Buttons: OK & Cancel
            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Font = new Font("Segoe UI", 8.5f);
            btnOk.Location = new Point(72, y);
            btnOk.Size = new Size(72, 26);
            btnOk.Click += (s, e) => {
                ApplyFilter();
                this.Close();
            };
            mainPanel.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Font = new Font("Segoe UI", 8.5f);
            btnCancel.Location = new Point(152, y);
            btnCancel.Size = new Size(72, 26);
            btnCancel.Click += (s, e) => this.Close();
            mainPanel.Controls.Add(btnCancel);
        }

        private Button CreateMenuItem(string text, int top)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Location = new Point(4, top);
            btn.Size = new Size(230, 24);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 242, 246);
            btn.Font = new Font("Segoe UI", 8.8f);
            return btn;
        }

        private void PopulateCheckList(string filterText)
        {
            clbValues.Items.Clear();
            clbValues.Items.Add("(Select All)", true);

            HashSet<string> currentActive = null;
            if (_table.ColumnFilters.ContainsKey(_colIndex))
            {
                currentActive = _table.ColumnFilters[_colIndex];
            }

            foreach (string val in _allDistinctValues)
            {
                if (string.IsNullOrEmpty(filterText) || val.ToLower().Contains(filterText.ToLower()))
                {
                    bool isChecked = (currentActive == null || currentActive.Contains(val));
                    clbValues.Items.Add(val, isChecked);
                }
            }

            clbValues.ItemCheck += (s, e) => {
                if (e.Index == 0)
                {
                    bool chk = (e.NewValue == CheckState.Checked);
                    for (int i = 1; i < clbValues.Items.Count; i++)
                    {
                        clbValues.SetItemChecked(i, chk);
                    }
                }
            };
        }

        private void FilterCheckList()
        {
            PopulateCheckList(txtSearch.Text.Trim());
        }

        private void ApplyFilter()
        {
            HashSet<string> selected = new HashSet<string>();
            for (int i = 1; i < clbValues.Items.Count; i++)
            {
                if (clbValues.GetItemChecked(i))
                {
                    selected.Add(clbValues.Items[i].ToString());
                }
            }

            _calcView.ApplyColumnFilter(_table, _colIndex, selected);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Close();
        }
    }
    #endregion
}

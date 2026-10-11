using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class CalcTextToColumnsDialog : Form
    {
        private CalcView _calcView;
        private Form _parentForm;

        private uint _srcCol;
        private uint _startRow;
        private uint _endRow;

        private RadioButton radDelimited;
        private RadioButton radFixedWidth;
        private GroupBox grpDelim;
        private GroupBox grpFixed;
        private CheckBox chkTab;
        private CheckBox chkSemicolon;
        private CheckBox chkComma;
        private CheckBox chkSpace;
        private CheckBox chkOther;
        private TextBox txtOther;
        private CheckBox chkConsecutive;
        private ComboBox cboQualifier;
        private TextBox txtBreakPositions;
        private TextBox txtDestination;
        private DataGridView dgvPreview;
        private Button btnOK;
        private Button btnCancel;

        public CalcTextToColumnsDialog(CalcView calcView, Form parentForm)
        {
            _calcView = calcView;
            _parentForm = parentForm;

            _srcCol = Math.Min(_calcView.SelectedCol, _calcView.SelEndCol);
            _startRow = Math.Min(_calcView.SelectedRow, _calcView.SelEndRow);
            _endRow = Math.Max(_calcView.SelectedRow, _calcView.SelEndRow);

            InitializeComponent();
            UpdateLivePreview();
        }

        private void InitializeComponent()
        {
            this.Text = "Chuyển Văn Bản Thành Cột (Convert Text to Columns)";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(243, 242, 241);

            GroupBox grpMode = new GroupBox();
            grpMode.Text = "Kiểu dữ liệu gốc";
            grpMode.Location = new Point(15, 10);
            grpMode.Size = new Size(535, 65);

            radDelimited = new RadioButton();
            radDelimited.Text = "Phân tách bằng ký tự (Delimited - Tab, phẩy, chấm phẩy, v.v.)";
            radDelimited.Location = new Point(15, 20);
            radDelimited.AutoSize = true;
            radDelimited.Checked = true;
            radDelimited.CheckedChanged += delegate(object sender, EventArgs e) { ToggleMode(); UpdateLivePreview(); };
            grpMode.Controls.Add(radDelimited);

            radFixedWidth = new RadioButton();
            radFixedWidth.Text = "Độ rộng cố định (Fixed width - Các cột canh đều theo vị trí ký tự)";
            radFixedWidth.Location = new Point(15, 42);
            radFixedWidth.AutoSize = true;
            grpMode.Controls.Add(radFixedWidth);
            this.Controls.Add(grpMode);

            // Delimiters Group
            grpDelim = new GroupBox();
            grpDelim.Text = "Ký tự phân cách (Delimiters)";
            grpDelim.Location = new Point(15, 80);
            grpDelim.Size = new Size(535, 98);

            chkTab = new CheckBox();
            chkTab.Text = "Tab";
            chkTab.Location = new Point(15, 20);
            chkTab.AutoSize = true;
            chkTab.Checked = true;
            chkTab.CheckedChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(chkTab);

            chkSemicolon = new CheckBox();
            chkSemicolon.Text = "Chấm phẩy (;)";
            chkSemicolon.Location = new Point(110, 20);
            chkSemicolon.AutoSize = true;
            chkSemicolon.CheckedChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(chkSemicolon);

            chkComma = new CheckBox();
            chkComma.Text = "Dấu phẩy (,)";
            chkComma.Location = new Point(230, 20);
            chkComma.AutoSize = true;
            chkComma.CheckedChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(chkComma);

            chkSpace = new CheckBox();
            chkSpace.Text = "Khoảng trắng";
            chkSpace.Location = new Point(340, 20);
            chkSpace.AutoSize = true;
            chkSpace.CheckedChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(chkSpace);

            chkOther = new CheckBox();
            chkOther.Text = "Khác:";
            chkOther.Location = new Point(15, 45);
            chkOther.AutoSize = true;
            chkOther.CheckedChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(chkOther);

            txtOther = new TextBox();
            txtOther.Location = new Point(70, 43);
            txtOther.Size = new Size(30, 22);
            txtOther.MaxLength = 2;
            txtOther.TextChanged += delegate(object sender, EventArgs e)
            {
                chkOther.Checked = !string.IsNullOrEmpty(txtOther.Text);
                UpdateLivePreview();
            };
            grpDelim.Controls.Add(txtOther);

            chkConsecutive = new CheckBox();
            chkConsecutive.Text = "Coi các dấu phân cách liên tiếp là một";
            chkConsecutive.Location = new Point(150, 45);
            chkConsecutive.AutoSize = true;
            chkConsecutive.Checked = true;
            chkConsecutive.CheckedChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(chkConsecutive);

            Label lblQual = new Label();
            lblQual.Text = "Ký tự định danh văn bản:";
            lblQual.Location = new Point(15, 72);
            lblQual.AutoSize = true;
            grpDelim.Controls.Add(lblQual);

            cboQualifier = new ComboBox();
            cboQualifier.DropDownStyle = ComboBoxStyle.DropDownList;
            cboQualifier.Location = new Point(160, 69);
            cboQualifier.Size = new Size(90, 22);
            cboQualifier.Items.AddRange(new object[] { "\"", "'", "{Không có}" });
            cboQualifier.SelectedIndex = 0;
            cboQualifier.SelectedIndexChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpDelim.Controls.Add(cboQualifier);

            this.Controls.Add(grpDelim);

            // Fixed Width Box
            grpFixed = new GroupBox();
            grpFixed.Text = "Độ rộng cố định (Fixed Width Breakpoints)";
            grpFixed.Location = new Point(15, 80);
            grpFixed.Size = new Size(535, 98);
            grpFixed.Visible = false;

            Label lblFixedHelp = new Label();
            lblFixedHelp.Text = "Nhập danh sách các vị trí cắt cột (phân cách bằng dấu phẩy, VD: 10, 25, 40):";
            lblFixedHelp.Location = new Point(15, 25);
            lblFixedHelp.AutoSize = true;
            grpFixed.Controls.Add(lblFixedHelp);

            txtBreakPositions = new TextBox();
            txtBreakPositions.Location = new Point(15, 52);
            txtBreakPositions.Size = new Size(400, 24);
            txtBreakPositions.TextChanged += delegate(object sender, EventArgs e) { UpdateLivePreview(); };
            grpFixed.Controls.Add(txtBreakPositions);

            this.Controls.Add(grpFixed);

            // Destination
            Label lblDest = new Label();
            lblDest.Text = "Ô đích (Destination):";
            lblDest.Location = new Point(15, 188);
            lblDest.AutoSize = true;
            this.Controls.Add(lblDest);

            txtDestination = new TextBox();
            txtDestination.Location = new Point(150, 185);
            txtDestination.Size = new Size(120, 24);
            txtDestination.Text = "$" + SparseMatrix.ColIndexToName(_srcCol) + "$" + _startRow;
            this.Controls.Add(txtDestination);

            // Live Preview Grid
            Label lblPrev = new Label();
            lblPrev.Text = "Xem trước dữ liệu phân tách (Data Preview):";
            lblPrev.Location = new Point(15, 218);
            lblPrev.AutoSize = true;
            this.Controls.Add(lblPrev);

            dgvPreview = new DataGridView();
            dgvPreview.Location = new Point(15, 238);
            dgvPreview.Size = new Size(535, 185);
            dgvPreview.ReadOnly = true;
            dgvPreview.AllowUserToAddRows = false;
            dgvPreview.AllowUserToDeleteRows = false;
            dgvPreview.AllowUserToResizeRows = false;
            dgvPreview.RowHeadersVisible = false;
            dgvPreview.BackgroundColor = Color.White;
            dgvPreview.BorderStyle = BorderStyle.Fixed3D;
            this.Controls.Add(dgvPreview);

            btnOK = new Button();
            btnOK.Text = "Thực hiện (Finish)";
            btnOK.Location = new Point(320, 435);
            btnOK.Size = new Size(130, 32);
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button();
            btnCancel.Text = "Hủy";
            btnCancel.Location = new Point(460, 435);
            btnCancel.Size = new Size(90, 32);
            btnCancel.Click += delegate(object sender, EventArgs e) { this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private void ToggleMode()
        {
            bool isDelim = radDelimited.Checked;
            grpDelim.Visible = isDelim;
            grpFixed.Visible = !isDelim;
        }

        private List<char> GetActiveDelimiters()
        {
            List<char> delims = new List<char>();
            if (chkTab.Checked) delims.Add('\t');
            if (chkSemicolon.Checked) delims.Add(';');
            if (chkComma.Checked) delims.Add(',');
            if (chkSpace.Checked) delims.Add(' ');
            if (chkOther.Checked && !string.IsNullOrEmpty(txtOther.Text))
            {
                delims.Add(txtOther.Text[0]);
            }
            if (delims.Count == 0) delims.Add('\t');
            return delims;
        }

        private char GetTextQualifier()
        {
            if (cboQualifier.SelectedIndex == 0) return '"';
            if (cboQualifier.SelectedIndex == 1) return '\'';
            return '\0';
        }

        private List<int> GetBreakPositions()
        {
            List<int> list = new List<int>();
            if (string.IsNullOrEmpty(txtBreakPositions.Text)) return list;
            string[] parts = txtBreakPositions.Text.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val) && val > 0)
                {
                    if (!list.Contains(val)) list.Add(val);
                }
            }
            list.Sort();
            return list;
        }

        private void UpdateLivePreview()
        {
            dgvPreview.Columns.Clear();
            dgvPreview.Rows.Clear();

            SparseMatrix sheet = _calcView.CurrentSheet;
            if (sheet == null) return;

            uint previewLimit = Math.Min(_endRow, _startRow + 10);
            List<List<string>> rowTokens = new List<List<string>>();
            int maxCols = 1;

            bool isDelim = radDelimited.Checked;
            List<char> delims = GetActiveDelimiters();
            char qual = GetTextQualifier();
            List<int> breaks = GetBreakPositions();

            for (uint r = _startRow; r <= previewLimit; r++)
            {
                CellData cell = sheet.GetCell(r, _srcCol);
                string text = (cell != null) ? (cell.RawValue ?? "") : "";
                List<string> tokens;
                if (isDelim)
                {
                    tokens = ParseDelimitedLine(text, delims, chkConsecutive.Checked, qual);
                }
                else
                {
                    tokens = ParseFixedWidthLine(text, breaks);
                }
                rowTokens.Add(tokens);
                if (tokens.Count > maxCols) maxCols = tokens.Count;
            }

            for (int c = 0; c < maxCols; c++)
            {
                dgvPreview.Columns.Add("Col" + c, "Cột " + (c + 1));
            }

            for (int r = 0; r < rowTokens.Count; r++)
            {
                List<string> toks = rowTokens[r];
                object[] rowVals = new object[maxCols];
                for (int c = 0; c < maxCols; c++)
                {
                    rowVals[c] = (c < toks.Count) ? toks[c] : "";
                }
                dgvPreview.Rows.Add(rowVals);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Tuple<uint, uint> dest = SparseMatrix.ParseCellAddress(txtDestination.Text);
            uint destR = (dest != null) ? dest.Item1 : _startRow;
            uint destC = (dest != null) ? dest.Item2 : _srcCol;

            // Check overwrite collision
            bool hasCollision = false;
            SparseMatrix sheet = _calcView.CurrentSheet;
            uint rowSpan = _endRow - _startRow;
            for (uint r = 0; r <= rowSpan; r++)
            {
                for (uint c = 1; c < (uint)dgvPreview.Columns.Count; c++)
                {
                    uint targetR = destR + r;
                    uint targetC = destC + c;
                    if (targetC == _srcCol && targetR >= _startRow && targetR <= _endRow) continue;
                    CellData existing = sheet.GetCell(targetR, targetC);
                    if (existing != null && !string.IsNullOrEmpty(existing.RawValue))
                    {
                        hasCollision = true;
                        break;
                    }
                }
                if (hasCollision) break;
            }

            if (hasCollision)
            {
                DialogResult dr = MessageBox.Show(this,
                    "Vùng dữ liệu đích đã có sẵn dữ liệu. Bạn có muốn ghi đè lên các ô này không?",
                    "Cảnh báo ghi đè", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) return;
            }

            _calcView.ExecuteTextToColumns(_srcCol, _startRow, _endRow, destC, destR,
                GetActiveDelimiters(), chkConsecutive.Checked, GetTextQualifier(),
                GetBreakPositions(), !radDelimited.Checked);

            if (_parentForm != null) _parentForm.Invalidate();
            this.Close();
        }

        public static List<string> ParseDelimitedLine(string line, List<char> delimiters, bool consecutiveAsOne, char textQualifier)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                tokens.Add("");
                return tokens;
            }

            System.Text.StringBuilder current = new System.Text.StringBuilder();
            bool insideQuotes = false;
            bool hasQualifier = (textQualifier != '\0' && textQualifier != ' ');

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (hasQualifier && ch == textQualifier)
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == textQualifier)
                    {
                        current.Append(ch);
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (!insideQuotes && delimiters.Contains(ch))
                {
                    if (consecutiveAsOne)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        while (i + 1 < line.Length && delimiters.Contains(line[i + 1]))
                        {
                            i++;
                        }
                    }
                    else
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            tokens.Add(current.ToString());
            return tokens;
        }

        public static List<string> ParseFixedWidthLine(string line, List<int> breaks)
        {
            List<string> tokens = new List<string>();
            if (line == null) line = "";
            int lastPos = 0;
            for (int i = 0; i < breaks.Count; i++)
            {
                int p = breaks[i];
                if (p > lastPos)
                {
                    if (lastPos < line.Length)
                    {
                        int len = Math.Min(p - lastPos, line.Length - lastPos);
                        tokens.Add(line.Substring(lastPos, len));
                    }
                    else
                    {
                        tokens.Add("");
                    }
                    lastPos = p;
                }
            }
            if (lastPos < line.Length)
            {
                tokens.Add(line.Substring(lastPos));
            }
            else if (tokens.Count == 0)
            {
                tokens.Add(line);
            }
            return tokens;
        }
    }
}

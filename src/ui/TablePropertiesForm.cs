using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Word 365 Table Properties dialog with 5 tabs: Table, Row, Column, Cell, Alt Text.
    /// </summary>
    public class TablePropertiesForm : Form
    {
        private readonly WriterView _writerView;
        private readonly int _tableIndex;
        private readonly int _rowIndex;
        private readonly int _colIndex;

        private RadioButton _rdoAlignLeft;
        private RadioButton _rdoAlignCenter;
        private RadioButton _rdoAlignRight;
        private RadioButton _rdoWrapNone;
        private RadioButton _rdoWrapAround;

        private CheckBox _chkSpecifyRowHeight;
        private NumericUpDown _numRowHeightPt;
        private CheckBox _chkAllowRowBreak;
        private CheckBox _chkRepeatHeader;

        private CheckBox _chkSpecifyColWidth;
        private NumericUpDown _numColWidthIn;

        private RadioButton _rdoCellTop;
        private RadioButton _rdoCellCenter;
        private RadioButton _rdoCellBottom;
        private ComboBox _cboCellMargins;

        private TextBox _txtAltTitle;
        private TextBox _txtAltDesc;

        public TablePropertiesForm(WriterView writerView, int tableIndex, int rowIndex, int colIndex, int initialTab = 0)
        {
            _writerView = writerView;
            _tableIndex = tableIndex;
            _rowIndex = rowIndex;
            _colIndex = colIndex;

            BuildUI(initialTab);
            LoadCurrentValues();
        }

        private void BuildUI(int initialTab)
        {
            Text = AppL10n.T("Thuộc tính bảng (Table Properties)", "Table Properties");
            Size = new Size(460, 445);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 246, 248);
            Font = new Font("Segoe UI", 9f);

            TabControl tabs = new ZenQOffice.UI.FluentTabControl
            {
                Location = new Point(12, 12),
                Size = new Size(422, 344)
            };

            TabPage tabTable = new TabPage(AppL10n.T("Bảng", "Table")) { BackColor = Color.White };
            TabPage tabRow = new TabPage(AppL10n.T("Hàng", "Row")) { BackColor = Color.White };
            TabPage tabCol = new TabPage(AppL10n.T("Cột", "Column")) { BackColor = Color.White };
            TabPage tabCell = new TabPage(AppL10n.T("Ô", "Cell")) { BackColor = Color.White };
            TabPage tabAlt = new TabPage(AppL10n.T("Văn bản thay thế", "Alt Text")) { BackColor = Color.White };

            BuildTableTab(tabTable);
            BuildRowTab(tabRow);
            BuildColumnTab(tabCol);
            BuildCellTab(tabCell);
            BuildAltTextTab(tabAlt);

            tabs.TabPages.AddRange(new TabPage[] { tabTable, tabRow, tabCol, tabCell, tabAlt });
            if (initialTab >= 0 && initialTab < tabs.TabPages.Count) tabs.SelectedIndex = initialTab;
            Controls.Add(tabs);

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(268, 368),
                Size = new Size(80, 27),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => ApplyProperties();
            Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = AppL10n.T("Hủy", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(354, 368),
                Size = new Size(80, 27),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(196, 200, 208);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void BuildTableTab(TabPage tab)
        {
            GroupBox grpAlign = new GroupBox { Text = AppL10n.T("Căn chỉnh bảng (Alignment)", "Alignment"), Location = new Point(14, 14), Size = new Size(388, 76) };
            _rdoAlignLeft = new RadioButton { Text = AppL10n.T("Trái (Left)", "Left"), Location = new Point(18, 30), AutoSize = true, Checked = true };
            _rdoAlignCenter = new RadioButton { Text = AppL10n.T("Giữa (Center)", "Center"), Location = new Point(136, 30), AutoSize = true };
            _rdoAlignRight = new RadioButton { Text = AppL10n.T("Phải (Right)", "Right"), Location = new Point(264, 30), AutoSize = true };
            grpAlign.Controls.AddRange(new Control[] { _rdoAlignLeft, _rdoAlignCenter, _rdoAlignRight });
            tab.Controls.Add(grpAlign);

            GroupBox grpWrap = new GroupBox { Text = AppL10n.T("Ngắt dòng văn bản (Text wrapping)", "Text wrapping"), Location = new Point(14, 102), Size = new Size(388, 76) };
            _rdoWrapNone = new RadioButton { Text = AppL10n.T("Không (None - Cùng dòng)", "None"), Location = new Point(18, 30), AutoSize = true, Checked = true };
            _rdoWrapAround = new RadioButton { Text = AppL10n.T("Bao quanh (Around)", "Around"), Location = new Point(210, 30), AutoSize = true };
            grpWrap.Controls.AddRange(new Control[] { _rdoWrapNone, _rdoWrapAround });
            tab.Controls.Add(grpWrap);

            GroupBox grpFit = new GroupBox { Text = AppL10n.T("Thao tác nhanh kích thước (Quick Sizing)", "Quick Sizing"), Location = new Point(14, 190), Size = new Size(388, 96) };
            Button btnAutoContents = new Button { Text = AppL10n.T("Tự khớp nội dung", "AutoFit Contents"), Location = new Point(18, 28), Size = new Size(168, 27), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAutoContents.Click += (s, e) => { _writerView?.AutoFitTableToContents(); };
            Button btnAutoWindow = new Button { Text = AppL10n.T("Tự khớp trang giấy", "AutoFit Window"), Location = new Point(202, 28), Size = new Size(168, 27), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAutoWindow.Click += (s, e) => { _writerView?.AutoFitTableToWindow(); };
            Button btnDistCols = new Button { Text = AppL10n.T("Chia đều cột", "Distribute Columns"), Location = new Point(18, 60), Size = new Size(168, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDistCols.Click += (s, e) => { _writerView?.DistributeTableColumns(); };
            Button btnDistRows = new Button { Text = AppL10n.T("Chia đều hàng", "Distribute Rows"), Location = new Point(202, 60), Size = new Size(168, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDistRows.Click += (s, e) => { _writerView?.DistributeTableRows(); };
            grpFit.Controls.AddRange(new Control[] { btnAutoContents, btnAutoWindow, btnDistCols, btnDistRows });
            tab.Controls.Add(grpFit);
        }

        private void BuildRowTab(TabPage tab)
        {
            GroupBox grpSize = new GroupBox { Text = AppL10n.T("Kích thước hàng ", "Row ") + (_rowIndex + 1) + AppL10n.T(" (Size)", " Size"), Location = new Point(14, 14), Size = new Size(388, 86) };
            _chkSpecifyRowHeight = new CheckBox { Text = AppL10n.T("Chỉ định chiều cao (Specify height - pt):", "Specify height (pt):"), Location = new Point(16, 30), AutoSize = true };
            _numRowHeightPt = new NumericUpDown { Location = new Point(282, 28), Size = new Size(86, 23), DecimalPlaces = 0, Minimum = 12, Maximum = 500, Value = 28, Enabled = false };
            _chkSpecifyRowHeight.CheckedChanged += (s, e) => { _numRowHeightPt.Enabled = _chkSpecifyRowHeight.Checked; };
            grpSize.Controls.AddRange(new Control[] { _chkSpecifyRowHeight, _numRowHeightPt });
            tab.Controls.Add(grpSize);

            GroupBox grpOpt = new GroupBox { Text = AppL10n.T("Tùy chọn hàng (Options)", "Options"), Location = new Point(14, 112), Size = new Size(388, 100) };
            _chkAllowRowBreak = new CheckBox { Text = AppL10n.T("Cho phép hàng ngắt qua trang (Allow row to break across pages)", "Allow row to break across pages"), Location = new Point(16, 26), AutoSize = true, Checked = true };
            _chkRepeatHeader = new CheckBox { Text = AppL10n.T("Lặp lại làm hàng tiêu đề ở đầu mỗi trang (Repeat header row)", "Repeat as header row at the top of each page"), Location = new Point(16, 56), AutoSize = true };
            grpOpt.Controls.AddRange(new Control[] { _chkAllowRowBreak, _chkRepeatHeader });
            tab.Controls.Add(grpOpt);
        }

        private void BuildColumnTab(TabPage tab)
        {
            GroupBox grpCol = new GroupBox { Text = AppL10n.T("Kích thước cột ", "Column ") + (_colIndex + 1) + AppL10n.T(" (Size)", " Size"), Location = new Point(14, 14), Size = new Size(388, 100) };
            _chkSpecifyColWidth = new CheckBox { Text = AppL10n.T("Độ rộng ưu tiên (Preferred width - inch):", "Preferred width (in):"), Location = new Point(16, 34), AutoSize = true };
            _numColWidthIn = new NumericUpDown { Location = new Point(282, 32), Size = new Size(86, 23), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.2m, Maximum = 15m, Value = 1.50m, Enabled = false };
            _chkSpecifyColWidth.CheckedChanged += (s, e) => { _numColWidthIn.Enabled = _chkSpecifyColWidth.Checked; };
            grpCol.Controls.AddRange(new Control[] { _chkSpecifyColWidth, _numColWidthIn });
            tab.Controls.Add(grpCol);
        }

        private void BuildCellTab(TabPage tab)
        {
            GroupBox grpVAlign = new GroupBox { Text = AppL10n.T("Căn lề dọc trong ô (Vertical alignment)", "Vertical alignment"), Location = new Point(14, 14), Size = new Size(388, 86) };
            _rdoCellTop = new RadioButton { Text = AppL10n.T("Trên (Top)", "Top"), Location = new Point(18, 32), AutoSize = true, Checked = true };
            _rdoCellCenter = new RadioButton { Text = AppL10n.T("Giữa (Center)", "Center"), Location = new Point(140, 32), AutoSize = true };
            _rdoCellBottom = new RadioButton { Text = AppL10n.T("Dưới (Bottom)", "Bottom"), Location = new Point(268, 32), AutoSize = true };
            grpVAlign.Controls.AddRange(new Control[] { _rdoCellTop, _rdoCellCenter, _rdoCellBottom });
            tab.Controls.Add(grpVAlign);

            GroupBox grpMarg = new GroupBox { Text = AppL10n.T("Lề ô bảng (Cell margins)", "Cell margins"), Location = new Point(14, 112), Size = new Size(388, 80) };
            grpMarg.Controls.Add(new Label { Text = AppL10n.T("Khoảng đệm lề ô:", "Cell padding:"), Location = new Point(18, 32), AutoSize = true });
            _cboCellMargins = new ZenQOffice.UI.FluentComboBox { Location = new Point(150, 28), Size = new Size(216, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboCellMargins.Items.AddRange(new object[] { AppL10n.T("Tiêu chuẩn (4 pt)", "Standard (4 pt)"), AppL10n.T("Không lề (0 pt)", "None (0 pt)"), AppL10n.T("Rộng rãi (8 pt)", "Wide (8 pt)") });
            _cboCellMargins.SelectedIndex = 0;
            grpMarg.Controls.Add(_cboCellMargins);
            tab.Controls.Add(grpMarg);
        }

        private void BuildAltTextTab(TabPage tab)
        {
            tab.Controls.Add(new Label { Text = AppL10n.T("Tiêu đề (Title):", "Title:"), Location = new Point(16, 16), AutoSize = true });
            _txtAltTitle = new TextBox { Location = new Point(16, 36), Size = new Size(382, 24) };
            tab.Controls.Add(_txtAltTitle);

            tab.Controls.Add(new Label { Text = AppL10n.T("Mô tả (Description):", "Description:"), Location = new Point(16, 74), AutoSize = true });
            _txtAltDesc = new TextBox { Location = new Point(16, 94), Size = new Size(382, 160), Multiline = true, ScrollBars = ScrollBars.Vertical };
            tab.Controls.Add(_txtAltDesc);
        }

        private void LoadCurrentValues()
        {
            if (_writerView == null) return;
            if (_tableIndex >= 0 && _tableIndex < _writerView.DocumentTables.Count)
            {
                var tbl = _writerView.DocumentTables[_tableIndex];
                if (tbl != null)
                {
                    _rdoAlignLeft.Checked = (tbl.Alignment == 0);
                    _rdoAlignCenter.Checked = (tbl.Alignment == 1);
                    _rdoAlignRight.Checked = (tbl.Alignment == 2);
                    if (_rowIndex >= 0 && _rowIndex < tbl.Rows.Count)
                    {
                        _chkRepeatHeader.Checked = tbl.Rows[_rowIndex].RepeatHeader || (tbl.RepeatHeaderRow && _rowIndex == 0);
                        if (_colIndex >= 0 && _colIndex < tbl.Rows[_rowIndex].Cells.Count)
                        {
                            var cell = tbl.Rows[_rowIndex].Cells[_colIndex];
                            if (cell.VerticalAlignment == 1) _rdoCellCenter.Checked = true;
                            else if (cell.VerticalAlignment == 2) _rdoCellBottom.Checked = true;
                            else _rdoCellTop.Checked = true;
                        }
                    }
                }
            }

            int wrap = _writerView.TableWrapModes.ContainsKey(_tableIndex) ? _writerView.TableWrapModes[_tableIndex] : 0;
            _rdoWrapAround.Checked = (wrap == 1);
            _rdoWrapNone.Checked = (wrap == 0);

            if (_writerView.TableColWidths.ContainsKey(_tableIndex) && _colIndex >= 0 && _colIndex < _writerView.TableColWidths[_tableIndex].Count)
            {
                int wPx = _writerView.TableColWidths[_tableIndex][_colIndex];
                if (wPx > 0) _numColWidthIn.Value = Math.Max(0.2m, Math.Min(15m, (decimal)Math.Round(wPx / 96.0, 2)));
            }
            if (_writerView.TableRowHeights.ContainsKey(_tableIndex) && _rowIndex >= 0 && _rowIndex < _writerView.TableRowHeights[_tableIndex].Count)
            {
                int hPx = _writerView.TableRowHeights[_tableIndex][_rowIndex];
                if (hPx > 0) _numRowHeightPt.Value = Math.Max(12m, Math.Min(500m, (decimal)Math.Round(hPx * 0.75)));
            }
        }

        private void ApplyProperties()
        {
            if (_writerView == null) return;
            _writerView.SaveUndoState();

            int align = _rdoAlignCenter.Checked ? 1 : (_rdoAlignRight.Checked ? 2 : 0);
            _writerView.SetActiveTableAlignment(align);
            _writerView.TableWrapModes[_tableIndex] = _rdoWrapAround.Checked ? 1 : 0;

            if (_tableIndex >= 0 && _tableIndex < _writerView.DocumentTables.Count)
            {
                var tbl = _writerView.DocumentTables[_tableIndex];
                if (tbl != null && _rowIndex >= 0 && _rowIndex < tbl.Rows.Count)
                {
                    tbl.Rows[_rowIndex].RepeatHeader = _chkRepeatHeader.Checked;
                    if (_rowIndex == 0) tbl.RepeatHeaderRow = _chkRepeatHeader.Checked;
                    if (_colIndex >= 0 && _colIndex < tbl.Rows[_rowIndex].Cells.Count)
                    {
                        var cell = tbl.Rows[_rowIndex].Cells[_colIndex];
                        cell.VerticalAlignment = _rdoCellCenter.Checked ? 1 : (_rdoCellBottom.Checked ? 2 : 0);
                    }
                }
            }

            if (_chkSpecifyColWidth.Checked && _writerView.TableColWidths.ContainsKey(_tableIndex) && _colIndex >= 0 && _colIndex < _writerView.TableColWidths[_tableIndex].Count)
            {
                _writerView.TableColWidths[_tableIndex][_colIndex] = Math.Max(20, (int)Math.Round((float)_numColWidthIn.Value * 96f));
            }
            if (_chkSpecifyRowHeight.Checked && _writerView.TableRowHeights.ContainsKey(_tableIndex) && _rowIndex >= 0 && _rowIndex < _writerView.TableRowHeights[_tableIndex].Count)
            {
                _writerView.TableRowHeights[_tableIndex][_rowIndex] = Math.Max(16, (int)Math.Round((float)_numRowHeightPt.Value / 0.75f));
            }

            if (_cboCellMargins.SelectedIndex > 0)
            {
                _writerView.SetTableCellMargins(_cboCellMargins.SelectedIndex);
            }

            _writerView.InvalidateLayoutAndTextCache();
            if (_writerView.InvalidateCallback != null) _writerView.InvalidateCallback();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class CrossReferenceForm : Form
    {
        public string ReferenceText { get; private set; }

        private ComboBox cboRefType;
        private ComboBox cboInsertAs;
        private ListBox lstItems;
        private Button btnInsert;
        private Button btnClose;
        private WriterView _writerView;

        public CrossReferenceForm(WriterView writerView = null)
        {
            _writerView = writerView;

            this.Text = AppL10n.T("Tham Chiếu Chéo (Cross-reference)", "Cross-reference");
            this.Size = new Size(460, 370);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            Label lblType = new Label { Text = AppL10n.T("Loại tham chiếu:", "Reference type:"), Location = new Point(18, 15), AutoSize = true };
            this.Controls.Add(lblType);

            cboRefType = new ZenQOffice.UI.FluentComboBox { Location = new Point(18, 36), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cboRefType.Items.AddRange(new object[] { AppL10n.T("Tiêu đề (Heading)", "Heading"), AppL10n.T("Dấu trang (Bookmark)", "Bookmark"), AppL10n.T("Chú thích cuối trang (Footnote)", "Footnote"), AppL10n.T("Hình ảnh / Bảng (Figure/Table)", "Figure / Table") });
            cboRefType.SelectedIndex = 0;
            this.Controls.Add(cboRefType);

            Label lblInsertAs = new Label { Text = AppL10n.T("Chèn tham chiếu tới:", "Insert reference to:"), Location = new Point(235, 15), AutoSize = true };
            this.Controls.Add(lblInsertAs);

            cboInsertAs = new ZenQOffice.UI.FluentComboBox { Location = new Point(235, 36), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            cboInsertAs.Items.AddRange(new object[] { AppL10n.T("Văn bản tiêu đề / nội dung", "Heading / caption text"), AppL10n.T("Số trang", "Page number"), AppL10n.T("Số thứ tự mục", "Item number"), AppL10n.T("Trên / Dưới", "Above / Below") });
            cboInsertAs.SelectedIndex = 0;
            this.Controls.Add(cboInsertAs);

            Label lblList = new Label { Text = AppL10n.T("Chọn mục trong tài liệu:", "For which item:"), Location = new Point(18, 75), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblList);

            lstItems = new ZenQOffice.UI.FluentListBox { Location = new Point(18, 98), Size = new Size(407, 170), BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(lstItems);

            cboRefType.SelectedIndexChanged += (s, e) =>
            {
                RefreshItemsList();
            };

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            btnInsert = new Button
            {
                Text = AppL10n.T("Chèn (Insert)", "Insert"),
                Location = new Point(245, 10),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnInsert.FlatAppearance.BorderSize = 0;
            btnInsert.Click += (s, e) =>
            {
                if (lstItems.SelectedItem == null || (lstItems.SelectedItem.ToString().StartsWith("(") && lstItems.SelectedItem.ToString().EndsWith(")")))
                {
                    ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Vui lòng chọn một mục hợp lệ trong tài liệu để chèn tham chiếu!", "Please select a valid item to insert!"), AppL10n.T("Tham Chiếu Chéo", "Cross-reference"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                string selItem = lstItems.SelectedItem.ToString();
                string asType = cboInsertAs.SelectedItem != null ? cboInsertAs.SelectedItem.ToString() : "Văn bản";
                if (cboInsertAs.SelectedIndex == 1 || asType.Contains("Số trang") || asType.Contains("Page number"))
                {
                    ReferenceText = AppL10n.T(" (Trang 1) ", " (Page 1) ");
                }
                else
                {
                    ReferenceText = " (" + selItem + ") ";
                }
            };
            bottomPanel.Controls.Add(btnInsert);

            btnClose = new Button
            {
                Text = AppL10n.T("Đóng", "Close"),
                Location = new Point(352, 10),
                Size = new Size(75, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnClose);

            this.AcceptButton = btnInsert;
            this.CancelButton = btnClose;

            RefreshItemsList();
        }

        private void RefreshItemsList()
        {
            lstItems.Items.Clear();
            if (_writerView == null)
            {
                lstItems.Items.Add(AppL10n.T("(Không có mục nào trong tài liệu)", "(No items in document)"));
                btnInsert.Enabled = false;
                return;
            }

            if (cboRefType.SelectedIndex == 0) // Headings
            {
                var headings = _writerView.ExtractHeadings();
                if (headings != null && headings.Count > 0)
                {
                    foreach (var h in headings) lstItems.Items.Add(h);
                }
                else
                {
                    lstItems.Items.Add(AppL10n.T("(Chưa có tiêu đề Heading nào trong tài liệu)", "(No headings found in document)"));
                }
            }
            else if (cboRefType.SelectedIndex == 1) // Bookmarks
            {
                if (_writerView.Bookmarks != null && _writerView.Bookmarks.Count > 0)
                {
                    foreach (var b in _writerView.Bookmarks) lstItems.Items.Add(b);
                }
                else
                {
                    lstItems.Items.Add(AppL10n.T("(Chưa có dấu trang Bookmark nào trong tài liệu)", "(No bookmarks found in document)"));
                }
            }
            else if (cboRefType.SelectedIndex == 2) // Footnotes
            {
                if (_writerView.Footnotes != null && _writerView.Footnotes.Count > 0)
                {
                    foreach (var fn in _writerView.Footnotes) lstItems.Items.Add(fn);
                }
                else
                {
                    lstItems.Items.Add(AppL10n.T("(Chưa có chú thích Footnote nào trong tài liệu)", "(No footnotes found in document)"));
                }
            }
            else // Figures / Tables
            {
                var figs = _writerView.ExtractFiguresAndTables();
                if (figs != null && figs.Count > 0)
                {
                    foreach (var fig in figs) lstItems.Items.Add(fig);
                }
                else
                {
                    lstItems.Items.Add(AppL10n.T("(Chưa có bảng biểu hay hình ảnh nào trong tài liệu)", "(No tables or figures found in document)"));
                }
            }

            bool hasValidItem = lstItems.Items.Count > 0 && !lstItems.Items[0].ToString().StartsWith("(");
            if (hasValidItem)
            {
                lstItems.SelectedIndex = 0;
                btnInsert.Enabled = true;
            }
            else
            {
                btnInsert.Enabled = false;
            }
        }
    }
}

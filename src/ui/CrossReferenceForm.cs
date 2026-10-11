using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

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

            this.Text = "Tham Chiếu Chéo (Cross-reference)";
            this.Size = new Size(460, 370);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            Label lblType = new Label { Text = "Loại tham chiếu:", Location = new Point(18, 15), AutoSize = true };
            this.Controls.Add(lblType);

            cboRefType = new ComboBox { Location = new Point(18, 36), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cboRefType.Items.AddRange(new object[] { "Tiêu đề (Heading)", "Dấu trang (Bookmark)", "Chú thích cuối trang (Footnote)", "Hình ảnh / Bảng (Figure/Table)" });
            cboRefType.SelectedIndex = 0;
            this.Controls.Add(cboRefType);

            Label lblInsertAs = new Label { Text = "Chèn tham chiếu tới:", Location = new Point(235, 15), AutoSize = true };
            this.Controls.Add(lblInsertAs);

            cboInsertAs = new ComboBox { Location = new Point(235, 36), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            cboInsertAs.Items.AddRange(new object[] { "Văn bản tiêu đề / nội dung", "Số trang", "Số thứ tự mục", "Trên / Dưới" });
            cboInsertAs.SelectedIndex = 0;
            this.Controls.Add(cboInsertAs);

            Label lblList = new Label { Text = "Chọn mục trong tài liệu:", Location = new Point(18, 75), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblList);

            lstItems = new ListBox { Location = new Point(18, 98), Size = new Size(407, 170), BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(lstItems);

            cboRefType.SelectedIndexChanged += (s, e) =>
            {
                RefreshItemsList();
            };

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            btnInsert = new Button
            {
                Text = "Chèn (Insert)",
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
                    MessageBox.Show("Vui lòng chọn một mục hợp lệ trong tài liệu để chèn tham chiếu!", "Tham Chiếu Chéo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                string selItem = lstItems.SelectedItem.ToString();
                string asType = cboInsertAs.SelectedItem != null ? cboInsertAs.SelectedItem.ToString() : "Văn bản";
                if (asType.Contains("Số trang"))
                {
                    ReferenceText = " (Trang 1) ";
                }
                else
                {
                    ReferenceText = " (" + selItem + ") ";
                }
            };
            bottomPanel.Controls.Add(btnInsert);

            btnClose = new Button
            {
                Text = "Đóng",
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
                lstItems.Items.Add("(Không có mục nào trong tài liệu)");
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
                    lstItems.Items.Add("(Chưa có tiêu đề Heading nào trong tài liệu)");
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
                    lstItems.Items.Add("(Chưa có dấu trang Bookmark nào trong tài liệu)");
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
                    lstItems.Items.Add("(Chưa có chú thích Footnote nào trong tài liệu)");
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
                    lstItems.Items.Add("(Chưa có bảng biểu hay hình ảnh nào trong tài liệu)");
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

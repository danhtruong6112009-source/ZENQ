using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class IndexEntry
    {
        public string MainEntry { get; set; }
        public string SubEntry { get; set; }
        public List<int> Pages { get; set; }

        public IndexEntry()
        {
            MainEntry = "";
            SubEntry = "";
            Pages = new List<int>();
        }

        public string GetFullKey()
        {
            if (string.IsNullOrEmpty(SubEntry)) return MainEntry;
            return MainEntry + " : " + SubEntry;
        }
    }

    public class MarkIndexEntryForm : Form
    {
        public string MainEntryText { get; private set; }
        public string SubEntryText { get; private set; }
        public bool MarkAllOccurrences { get; private set; }

        public string MarkedEntryField
        {
            get
            {
                if (string.IsNullOrEmpty(SubEntryText))
                    return " {XE \"" + MainEntryText + "\"} ";
                return " {XE \"" + MainEntryText + ":" + SubEntryText + "\"} ";
            }
        }

        private TextBox txtMainEntry;
        private TextBox txtSubEntry;
        private RadioButton rdoCurrentPage;
        private WriterView _writerView;

        public MarkIndexEntryForm(WriterView writerView = null, string initialSelectedText = "")
        {
            _writerView = writerView;

            this.Text = AppL10n.T("Đánh Dấu Mục Tra Cứu (Mark Index Entry)", "Mark Index Entry");
            this.Size = new Size(450, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI(initialSelectedText);
        }

        private void BuildUI(string initialText)
        {
            Label lblMain = new Label { Text = AppL10n.T("Mục tra cứu chính (Main entry):", "Main entry:"), Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblMain);

            txtMainEntry = new TextBox { Location = new Point(20, 36), Size = new Size(395, 24) };
            txtMainEntry.Text = !string.IsNullOrEmpty(initialText) ? initialText.Trim() : "";
            this.Controls.Add(txtMainEntry);

            Label lblSub = new Label { Text = AppL10n.T("Mục phụ (Subentry - tùy chọn):", "Subentry (optional):"), Location = new Point(20, 72), AutoSize = true };
            this.Controls.Add(lblSub);

            txtSubEntry = new TextBox { Location = new Point(20, 93), Size = new Size(395, 24) };
            this.Controls.Add(txtSubEntry);

            GroupBox grpOpt = new GroupBox
            {
                Text = AppL10n.T("Tùy chọn trang:", "Options:"),
                Location = new Point(20, 130),
                Size = new Size(395, 55),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            this.Controls.Add(grpOpt);

            rdoCurrentPage = new RadioButton { Text = AppL10n.T("Trang hiện tại (Current page)", "Current page"), Location = new Point(15, 22), AutoSize = true, Checked = true };
            grpOpt.Controls.Add(rdoCurrentPage);

            // Bottom buttons
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnMark = new Button
            {
                Text = AppL10n.T("Đánh dấu", "Mark"),
                Location = new Point(145, 10),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnMark.FlatAppearance.BorderSize = 0;
            btnMark.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMainEntry.Text.Trim()))
                {
                    ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Vui lòng nhập mục tra cứu chính!", "Please enter a main entry!"), AppL10n.T("Đánh dấu mục", "Mark Index Entry"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                MainEntryText = txtMainEntry.Text.Trim();
                SubEntryText = txtSubEntry.Text.Trim();
                MarkAllOccurrences = false;
            };
            bottomPanel.Controls.Add(btnMark);

            Button btnMarkAll = new Button
            {
                Text = AppL10n.T("Đánh dấu tất cả", "Mark All"),
                Location = new Point(238, 10),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnMarkAll.FlatAppearance.BorderSize = 0;
            btnMarkAll.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMainEntry.Text.Trim()))
                {
                    ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Vui lòng nhập mục tra cứu chính!", "Please enter a main entry!"), AppL10n.T("Đánh dấu mục", "Mark Index Entry"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                MainEntryText = txtMainEntry.Text.Trim();
                SubEntryText = txtSubEntry.Text.Trim();
                MarkAllOccurrences = true;
            };
            bottomPanel.Controls.Add(btnMarkAll);

            Button btnClose = new Button
            {
                Text = AppL10n.T("Đóng", "Close"),
                Location = new Point(356, 10),
                Size = new Size(60, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnClose);

            this.AcceptButton = btnMark;
            this.CancelButton = btnClose;
        }
    }
}

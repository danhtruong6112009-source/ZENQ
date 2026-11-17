using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class TableOfContentsEntry
    {
        public int Level { get; set; }
        public string Title { get; set; }
        public int Page { get; set; }
        public int CharPosition { get; set; }

        public TableOfContentsEntry()
        {
            Level = 1;
            Title = "";
            Page = 1;
            CharPosition = 0;
        }

        public TableOfContentsEntry(int level, string title, int page, int charPos)
        {
            Level = level;
            Title = title;
            Page = page;
            CharPosition = charPos;
        }
    }

    public class TableOfContentsForm : Form
    {
        public string GeneratedTOC { get; private set; }

        private ListBox lstPresets;
        private CheckBox chkShowPageNumbers;
        private CheckBox chkRightAlign;
        private ComboBox cboTabLeader;
        private List<TableOfContentsEntry> _tocEntries;

        public TableOfContentsForm(WriterView writerView = null, List<TableOfContentsEntry> entries = null)
        {
            if (entries != null)
            {
                _tocEntries = entries;
            }
            else if (writerView != null)
            {
                _tocEntries = writerView.ExtractDetailedHeadings();
            }
            else
            {
                _tocEntries = new List<TableOfContentsEntry>();
            }

            this.Text = AppL10n.T("Bảng Mục Lục Tự Động (Table of Contents)", "Table of Contents");
            this.Size = new Size(500, 370);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            Label lblPresets = new Label { Text = AppL10n.T("Chọn kiểu dáng Mục lục:", "Select Table of Contents style:"), Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblPresets);

            lstPresets = new ZenQOffice.UI.FluentListBox { Location = new Point(20, 38), Size = new Size(445, 110), BorderStyle = BorderStyle.FixedSingle };
            lstPresets.Items.Add(AppL10n.T("1. Mục lục Cổ điển (Đường chấm dẫn ................. Trang 1)", "1. Classic Table of Contents (Dots ................. Page 1)"));
            lstPresets.Items.Add(AppL10n.T("2. Mục lục Hiện đại (Đường gạch nối ---------------- Trang 1)", "2. Modern Table of Contents (Dashes -------------- Page 1)"));
            lstPresets.Items.Add(AppL10n.T("3. Mục lục Tối giản (Khoảng trắng trống            Trang 1)", "3. Minimal Table of Contents (Spaces              Page 1)"));
            lstPresets.SelectedIndex = 0;
            this.Controls.Add(lstPresets);

            chkShowPageNumbers = new CheckBox { Text = AppL10n.T("Hiển thị số trang (Show page numbers)", "Show page numbers"), Location = new Point(20, 160), AutoSize = true, Checked = true };
            this.Controls.Add(chkShowPageNumbers);

            chkRightAlign = new CheckBox { Text = AppL10n.T("Căn lề phải số trang (Right align page numbers)", "Right align page numbers"), Location = new Point(20, 185), AutoSize = true, Checked = true };
            this.Controls.Add(chkRightAlign);

            Label lblLeader = new Label { Text = AppL10n.T("Đường dẫn tab (Tab leader):", "Tab leader:"), Location = new Point(20, 215), AutoSize = true };
            this.Controls.Add(lblLeader);

            cboTabLeader = new ZenQOffice.UI.FluentComboBox { Location = new Point(190, 212), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboTabLeader.Items.AddRange(new object[] { AppL10n.T(".................... (Chấm)", ".................... (Dots)"), AppL10n.T("-------------------- (Gạch)", "-------------------- (Dashes)"), AppL10n.T("____________________ (Gạch dưới)", "____________________ (Underline)"), AppL10n.T("(Không có)", "(None)") });
            cboTabLeader.SelectedIndex = 0;
            this.Controls.Add(cboTabLeader);

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = AppL10n.T("Tạo Mục Lục", "Insert Table of Contents"),
                Location = new Point(280, 10),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (_tocEntries == null || _tocEntries.Count == 0)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Chưa tìm thấy tiêu đề (Heading) nào trong tài liệu.\n\nVui lòng định dạng các đề mục trong bài viết bằng kiểu Heading 1, Heading 2 hoặc số thứ tự (1., 1.1...) trước khi tạo Mục lục tự động.", "No headings found in this document.\n\nPlease format headings with Heading 1, Heading 2, or numbered styles (1., 1.1...) first!"), AppL10n.T("Mục Lục Tự Động", "Table of Contents"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                string leaderChar = ".";
                if (cboTabLeader.SelectedIndex == 1) leaderChar = "-";
                else if (cboTabLeader.SelectedIndex == 2) leaderChar = "_";
                else if (cboTabLeader.SelectedIndex == 3) leaderChar = " ";

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine();
                sb.AppendLine(AppL10n.T("================== MỤC LỤC TỰ ĐỘNG ==================", "================== TABLE OF CONTENTS =================="));
                sb.AppendLine();

                foreach (var entry in _tocEntries)
                {
                    string indent = entry.Level == 2 ? "   " : (entry.Level >= 3 ? "      " : "");
                    string displayTitle = indent + entry.Title;
                    int totalLen = 52;
                    int padLen = Math.Max(3, totalLen - displayTitle.Length);
                    string dots = new string(leaderChar[0], padLen);

                    if (chkShowPageNumbers.Checked)
                    {
                        sb.AppendLine(string.Format("{0} {1} Trang {2}", displayTitle, dots, entry.Page));
                    }
                    else
                    {
                        sb.AppendLine(displayTitle);
                    }
                }
                sb.AppendLine();
                sb.AppendLine("=====================================================");
                sb.AppendLine();

                GeneratedTOC = sb.ToString();
            };
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = AppL10n.T("Hủy", "Cancel"),
                Location = new Point(398, 10),
                Size = new Size(75, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}

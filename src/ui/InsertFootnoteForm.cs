using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class InsertFootnoteForm : Form
    {
        public bool IsEndnote { get; private set; }
        public string NoteMarker { get; private set; }
        public string NoteText { get; private set; }

        private RadioButton rdoFootnote;
        private RadioButton rdoEndnote;
        private ComboBox cboNumberFormat;
        private TextBox txtCustomMark;
        private TextBox txtNoteContent;
        private ListBox lstExistingNotes;
        private WriterView _writerView;

        public InsertFootnoteForm(WriterView writerView, bool defaultEndnote = false)
        {
            _writerView = writerView;
            this.Text = "Chèn Chú Thích (Footnote & Endnote)";
            this.Size = new Size(540, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI(defaultEndnote);
            LoadExistingNotes();
        }

        private void BuildUI(bool defaultEndnote)
        {
            // 1. Group Vị trí
            GroupBox grpLocation = new GroupBox
            {
                Text = "Vị trí đặt chú thích (Location):",
                Location = new Point(20, 15),
                Size = new Size(485, 80),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(grpLocation);

            rdoFootnote = new RadioButton
            {
                Text = "Chú thích chân trang (Footnote) - Chân mỗi trang",
                Location = new Point(20, 24),
                AutoSize = true,
                Checked = !defaultEndnote,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            rdoFootnote.CheckedChanged += (s, e) => UpdateFormatPresets();
            grpLocation.Controls.Add(rdoFootnote);

            rdoEndnote = new RadioButton
            {
                Text = "Chú thích cuối tài liệu (Endnote) - Cuối toàn bộ tài liệu",
                Location = new Point(20, 48),
                AutoSize = true,
                Checked = defaultEndnote,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            rdoEndnote.CheckedChanged += (s, e) => UpdateFormatPresets();
            grpLocation.Controls.Add(rdoEndnote);

            // 2. Group Định dạng ký hiệu
            GroupBox grpFormat = new GroupBox
            {
                Text = "Định dạng ký hiệu đánh số (Format):",
                Location = new Point(20, 102),
                Size = new Size(485, 95),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(grpFormat);

            Label lblFmt = new Label { Text = "Định dạng số:", Location = new Point(20, 26), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            grpFormat.Controls.Add(lblFmt);

            cboNumberFormat = new ComboBox
            {
                Location = new Point(130, 23),
                Size = new Size(160, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.0f)
            };
            cboNumberFormat.Items.AddRange(new object[] {
                "1, 2, 3, ... (Số thường)",
                "i, ii, iii, ... (La Mã thường)",
                "I, II, III, ... (La Mã hoa)",
                "a, b, c, ... (Chữ cái thường)",
                "A, B, C, ... (Chữ cái hoa)",
                "*, †, ‡, § (Ký hiệu đặc biệt)"
            });
            cboNumberFormat.SelectedIndex = defaultEndnote ? 1 : 0;
            grpFormat.Controls.Add(cboNumberFormat);

            Label lblCustom = new Label { Text = "Ký hiệu tự nhập:", Location = new Point(20, 58), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            grpFormat.Controls.Add(lblCustom);

            txtCustomMark = new TextBox { Location = new Point(130, 56), Size = new Size(160, 24), Font = new Font("Segoe UI", 9.0f) };
            grpFormat.Controls.Add(txtCustomMark);

            // 3. Group Nội dung chú thích
            GroupBox grpContent = new GroupBox
            {
                Text = "Nội dung chú thích (Footnote Text):",
                Location = new Point(20, 204),
                Size = new Size(485, 110),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(grpContent);

            txtNoteContent = new TextBox
            {
                Location = new Point(15, 24),
                Size = new Size(455, 75),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            grpContent.Controls.Add(txtNoteContent);

            // 4. Danh sách chú thích hiện tại
            Label lblList = new Label { Text = "Danh sách các chú thích hiện có trong tài liệu:", Location = new Point(20, 320), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), ForeColor = Color.DimGray };
            this.Controls.Add(lblList);

            lstExistingNotes = new ListBox
            {
                Location = new Point(20, 340),
                Size = new Size(485, 45),
                Font = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(lstExistingNotes);

            // 5. Bottom Panel
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnInsert = new Button
            {
                Text = "Chèn",
                Location = new Point(310, 10),
                Size = new Size(95, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnInsert.FlatAppearance.BorderSize = 0;
            btnInsert.Click += (s, e) =>
            {
                IsEndnote = rdoEndnote.Checked;
                NoteText = txtNoteContent.Text.Trim();
                NoteMarker = ResolveMarker();
            };
            bottomPanel.Controls.Add(btnInsert);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(415, 10),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnInsert;
            this.CancelButton = btnCancel;
        }

        private void UpdateFormatPresets()
        {
            if (rdoEndnote.Checked)
            {
                if (cboNumberFormat.SelectedIndex == 0) cboNumberFormat.SelectedIndex = 1; // i, ii, iii
            }
            else
            {
                if (cboNumberFormat.SelectedIndex == 1) cboNumberFormat.SelectedIndex = 0; // 1, 2, 3
            }
        }

        private string ResolveMarker()
        {
            if (!string.IsNullOrEmpty(txtCustomMark.Text.Trim()))
            {
                return txtCustomMark.Text.Trim();
            }

            int count = 1;
            if (_writerView != null && _writerView.DetailedFootnotes != null)
            {
                foreach (var fn in _writerView.DetailedFootnotes)
                {
                    if (fn.IsEndnote == rdoEndnote.Checked) count++;
                }
            }

            switch (cboNumberFormat.SelectedIndex)
            {
                case 1: // i, ii, iii
                    return "[" + ToRomanNumeral(count).ToLower() + "]";
                case 2: // I, II, III
                    return "[" + ToRomanNumeral(count).ToUpper() + "]";
                case 3: // a, b, c
                    return ((char)('a' + (count - 1) % 26)).ToString();
                case 4: // A, B, C
                    return ((char)('A' + (count - 1) % 26)).ToString();
                case 5: // Special symbols
                    string[] syms = new string[] { "*", "†", "‡", "§", "**", "††" };
                    return syms[(count - 1) % syms.Length];
                default: // 1, 2, 3
                    return GetSuperscriptNumber(count);
            }
        }

        private string GetSuperscriptNumber(int num)
        {
            string s = num.ToString();
            char[] supDigits = new char[] { '⁰', '¹', '²', '³', '⁴', '⁵', '⁶', '⁷', '⁸', '⁹' };
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (c >= '0' && c <= '9') sb.Append(supDigits[c - '0']);
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private string ToRomanNumeral(int number)
        {
            if (number <= 0) return "1";
            int[] values = new int[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] numerals = new string[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                while (number >= values[i])
                {
                    number -= values[i];
                    result.Append(numerals[i]);
                }
            }
            return result.ToString();
        }

        private void LoadExistingNotes()
        {
            lstExistingNotes.Items.Clear();
            if (_writerView != null && _writerView.DetailedFootnotes != null && _writerView.DetailedFootnotes.Count > 0)
            {
                foreach (var fn in _writerView.DetailedFootnotes)
                {
                    string type = fn.IsEndnote ? "Endnote" : "Footnote (Trang " + fn.PageNumber + ")";
                    string previewTxt = !string.IsNullOrEmpty(fn.Text) ? fn.Text : "(Chưa có nội dung)";
                    lstExistingNotes.Items.Add(string.Format("{0} {1}: {2}", type, fn.Marker, previewTxt));
                }
            }
            else
            {
                lstExistingNotes.Items.Add("(Chưa có chú thích nào trong tài liệu)");
            }
        }
    }
}

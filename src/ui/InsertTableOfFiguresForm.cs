using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class InsertTableOfFiguresForm : Form
    {
        public string GeneratedTableOfFigures { get; private set; }

        private ComboBox cboLabelFilter;
        private CheckBox chkShowPageNumbers;
        private CheckBox chkRightAlign;
        private ComboBox cboTabLeader;
        private ListBox lstPreview;
        private WriterView _writerView;

        public InsertTableOfFiguresForm(WriterView writerView = null)
        {
            _writerView = writerView;

            this.Text = "Chèn Bảng Danh Mục Hình Ảnh / Bảng Biểu (Table of Figures)";
            this.Size = new Size(520, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI();
            UpdatePreview();
        }

        private void BuildUI()
        {
            Label lblLabel = new Label { Text = "Nhãn phụ đề (Caption label):", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblLabel);

            cboLabelFilter = new ComboBox { Location = new Point(200, 14), Width = 285, DropDownStyle = ComboBoxStyle.DropDownList };
            cboLabelFilter.Items.AddRange(new object[] { "Tất cả nhãn (Hình ảnh & Bảng biểu)", "Chỉ Hình ảnh (Figure)", "Chỉ Bảng biểu (Table)", "Chỉ Phương trình (Equation)" });
            cboLabelFilter.SelectedIndex = 0;
            cboLabelFilter.SelectedIndexChanged += (s, e) => UpdatePreview();
            this.Controls.Add(cboLabelFilter);

            Label lblPrevTitle = new Label { Text = "Xem trước danh mục hình ảnh / bảng biểu:", Location = new Point(20, 52), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            this.Controls.Add(lblPrevTitle);

            lstPreview = new ListBox { Location = new Point(20, 72), Size = new Size(465, 130), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 8.5f) };
            this.Controls.Add(lstPreview);

            chkShowPageNumbers = new CheckBox { Text = "Hiển thị số trang (Show page numbers)", Location = new Point(20, 216), AutoSize = true, Checked = true };
            chkShowPageNumbers.CheckedChanged += (s, e) => UpdatePreview();
            this.Controls.Add(chkShowPageNumbers);

            chkRightAlign = new CheckBox { Text = "Căn lề phải số trang (Right align page numbers)", Location = new Point(20, 242), AutoSize = true, Checked = true };
            chkRightAlign.CheckedChanged += (s, e) => UpdatePreview();
            this.Controls.Add(chkRightAlign);

            Label lblLeader = new Label { Text = "Đường dẫn tab (Tab leader):", Location = new Point(20, 275), AutoSize = true };
            this.Controls.Add(lblLeader);

            cboTabLeader = new ComboBox { Location = new Point(200, 272), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboTabLeader.Items.AddRange(new object[] { ".................... (Chấm)", "-------------------- (Gạch)", "____________________ (Gạch dưới)", "(Không có)" });
            cboTabLeader.SelectedIndex = 0;
            cboTabLeader.SelectedIndexChanged += (s, e) => UpdatePreview();
            this.Controls.Add(cboTabLeader);

            // Bottom action panel
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn Danh Mục",
                Location = new Point(275, 10),
                Size = new Size(125, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(410, 10),
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

        private List<CaptionItem> GetFilteredCaptions()
        {
            List<CaptionItem> list = new List<CaptionItem>();
            if (_writerView != null && _writerView.CaptionsList != null)
            {
                foreach (var cap in _writerView.CaptionsList)
                {
                    if (cboLabelFilter.SelectedIndex == 1 && cap.LabelType != "Hình") continue;
                    if (cboLabelFilter.SelectedIndex == 2 && cap.LabelType != "Bảng") continue;
                    if (cboLabelFilter.SelectedIndex == 3 && cap.LabelType != "Phương trình") continue;
                    list.Add(cap);
                }
            }

            // If empty, extract any shapes or tables dynamically
            if (list.Count == 0 && _writerView != null)
            {
                var dynFigs = _writerView.ExtractFiguresAndTables();
                int idx = 1;
                foreach (var df in dynFigs)
                {
                    string lType = df.StartsWith("Bảng") ? "Bảng" : "Hình";
                    if (cboLabelFilter.SelectedIndex == 1 && lType != "Hình") continue;
                    if (cboLabelFilter.SelectedIndex == 2 && lType != "Bảng") continue;
                    list.Add(new CaptionItem
                    {
                        LabelType = lType,
                        Number = idx++,
                        Title = df,
                        PageNumber = 1
                    });
                }
            }
            return list;
        }

        private void UpdatePreview()
        {
            lstPreview.Items.Clear();
            var caps = GetFilteredCaptions();

            string leaderChar = ".";
            if (cboTabLeader.SelectedIndex == 1) leaderChar = "-";
            else if (cboTabLeader.SelectedIndex == 2) leaderChar = "_";
            else if (cboTabLeader.SelectedIndex == 3) leaderChar = " ";

            if (caps.Count == 0)
            {
                lstPreview.Items.Add("(Chưa có hình ảnh/bảng biểu nào được tạo phụ đề trong tài liệu)");
                return;
            }

            foreach (var c in caps)
            {
                string tit = c.GetFullCaption();
                int totalLen = 46;
                int padLen = Math.Max(3, totalLen - tit.Length);
                string dots = new string(leaderChar[0], padLen);
                if (chkShowPageNumbers.Checked)
                {
                    lstPreview.Items.Add(string.Format("{0} {1} Trang {2}", tit, dots, c.PageNumber));
                }
                else
                {
                    lstPreview.Items.Add(tit);
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var caps = GetFilteredCaptions();
            if (caps.Count == 0)
            {
                MessageBox.Show("Không tìm thấy hình ảnh hoặc bảng biểu nào để tạo danh mục.\n\nVui lòng thêm hình ảnh hoặc bảng biểu và gán Phụ đề trước!", "Danh mục Hình ảnh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.None;
                return;
            }

            string leaderChar = ".";
            if (cboTabLeader.SelectedIndex == 1) leaderChar = "-";
            else if (cboTabLeader.SelectedIndex == 2) leaderChar = "_";
            else if (cboTabLeader.SelectedIndex == 3) leaderChar = " ";

            string headerTitle = "DANH MỤC HÌNH ẢNH & BẢNG BIỂU";
            if (cboLabelFilter.SelectedIndex == 1) headerTitle = "DANH MỤC HÌNH ẢNH";
            else if (cboLabelFilter.SelectedIndex == 2) headerTitle = "DANH MỤC BẢNG BIỂU";
            else if (cboLabelFilter.SelectedIndex == 3) headerTitle = "DANH MỤC PHƯƠNG TRÌNH";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================== " + headerTitle + " ==================");
            sb.AppendLine();

            foreach (var c in caps)
            {
                string tit = c.GetFullCaption();
                int totalLen = 52;
                int padLen = Math.Max(3, totalLen - tit.Length);
                string dots = new string(leaderChar[0], padLen);
                if (chkShowPageNumbers.Checked)
                {
                    sb.AppendLine(string.Format("{0} {1} Trang {2}", tit, dots, c.PageNumber));
                }
                else
                {
                    sb.AppendLine(tit);
                }
            }

            sb.AppendLine();
            sb.AppendLine("=========================================================");
            sb.AppendLine();

            GeneratedTableOfFigures = sb.ToString();
        }
    }
}

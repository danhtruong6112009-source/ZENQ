using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class InsertIndexForm : Form
    {
        public string GeneratedIndex { get; private set; }

        private ComboBox cboColumns;
        private ComboBox cboTabLeader;
        private CheckBox chkRightAlign;
        private ListBox lstPreview;
        private WriterView _writerView;

        public InsertIndexForm(WriterView writerView = null)
        {
            _writerView = writerView;

            this.Text = "Chèn Bảng Chỉ Mục Tra Cứu (Insert Index)";
            this.Size = new Size(500, 390);
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
            Label lblCols = new Label { Text = "Số cột hiển thị (Columns):", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblCols);

            cboColumns = new ComboBox { Location = new Point(200, 14), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboColumns.Items.AddRange(new object[] { "2 Cột (Khuyên dùng)", "3 Cột", "1 Cột" });
            cboColumns.SelectedIndex = 0;
            this.Controls.Add(cboColumns);

            Label lblPrev = new Label { Text = "Xem trước bảng chỉ mục theo thứ tự A-Z:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            this.Controls.Add(lblPrev);

            lstPreview = new ListBox { Location = new Point(20, 70), Size = new Size(445, 120), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 8.5f) };
            this.Controls.Add(lstPreview);

            chkRightAlign = new CheckBox { Text = "Căn lề phải số trang (Right align page numbers)", Location = new Point(20, 204), AutoSize = true, Checked = true };
            chkRightAlign.CheckedChanged += (s, e) => UpdatePreview();
            this.Controls.Add(chkRightAlign);

            Label lblLeader = new Label { Text = "Đường dẫn tab (Tab leader):", Location = new Point(20, 235), AutoSize = true };
            this.Controls.Add(lblLeader);

            cboTabLeader = new ComboBox { Location = new Point(200, 232), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            cboTabLeader.Items.AddRange(new object[] { ".................... (Chấm)", "-------------------- (Gạch)", "(Không có)" });
            cboTabLeader.SelectedIndex = 0;
            cboTabLeader.SelectedIndexChanged += (s, e) => UpdatePreview();
            this.Controls.Add(cboTabLeader);

            // Bottom action panel
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn Chỉ Mục",
                Location = new Point(265, 10),
                Size = new Size(115, 28),
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
                Location = new Point(390, 10),
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

        private SortedDictionary<string, List<int>> ScanIndexEntries()
        {
            SortedDictionary<string, List<int>> map = new SortedDictionary<string, List<int>>(StringComparer.CurrentCultureIgnoreCase);

            if (_writerView != null)
            {
                string text = _writerView.PieceTable != null ? _writerView.PieceTable.GetText() : "";
                if (!string.IsNullOrEmpty(text))
                {
                    // Scan {XE "..."} tags
                    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\{XE\s*""([^""]+)""\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var matches = regex.Matches(text);
                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        if (m.Groups.Count > 1)
                        {
                            string term = m.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(term))
                            {
                                int pageNum = Math.Max(1, _writerView.CurrentActivePage);
                                if (!map.ContainsKey(term)) map[term] = new List<int>();
                                if (!map[term].Contains(pageNum)) map[term].Add(pageNum);
                            }
                        }
                    }
                }

                // Also check _writerView.IndexEntries
                if (_writerView.IndexEntries != null)
                {
                    foreach (var ie in _writerView.IndexEntries)
                    {
                        string key = ie.GetFullKey();
                        if (!string.IsNullOrEmpty(key))
                        {
                            if (!map.ContainsKey(key)) map[key] = new List<int>();
                            if (ie.Pages != null)
                            {
                                foreach (var p in ie.Pages)
                                {
                                    if (!map[key].Contains(p)) map[key].Add(p);
                                }
                            }
                            if (map[key].Count == 0) map[key].Add(1);
                        }
                    }
                }
            }

            return map;
        }

        private void UpdatePreview()
        {
            lstPreview.Items.Clear();
            var map = ScanIndexEntries();

            string leaderChar = ".";
            if (cboTabLeader.SelectedIndex == 1) leaderChar = "-";
            else if (cboTabLeader.SelectedIndex == 2) leaderChar = " ";

            if (map.Count == 0)
            {
                lstPreview.Items.Add("(Chưa có mục nào được đánh dấu {XE} trong tài liệu)");
                lstPreview.Items.Add("• A");
                lstPreview.Items.Add("  Thuật toán cơ bản .................... Trang 1");
                lstPreview.Items.Add("• B");
                lstPreview.Items.Add("  Biến toàn cục ........................ Trang 1, 2");
                return;
            }

            char lastGroup = '\0';
            foreach (var kvp in map)
            {
                char firstChar = char.ToUpper(kvp.Key[0]);
                if (firstChar != lastGroup)
                {
                    lastGroup = firstChar;
                    lstPreview.Items.Add(string.Format("• {0}", lastGroup));
                }

                string pagesStr = string.Join(", ", kvp.Value.ConvertAll(new Converter<int, string>(x => x.ToString())).ToArray());
                int totalLen = 42;
                int padLen = Math.Max(3, totalLen - kvp.Key.Length);
                string dots = new string(leaderChar[0], padLen);

                if (chkRightAlign.Checked)
                {
                    lstPreview.Items.Add(string.Format("  {0} {1} Trang {2}", kvp.Key, dots, pagesStr));
                }
                else
                {
                    lstPreview.Items.Add(string.Format("  {0}, Trang {1}", kvp.Key, pagesStr));
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var map = ScanIndexEntries();
            if (map.Count == 0)
            {
                MessageBox.Show("Chưa tìm thấy từ khóa nào được đánh dấu tra cứu trong tài liệu.\n\nVui lòng bôi đen từ khóa và chọn 'Đánh dấu mục' (Alt + Shift + X) trước khi chèn bảng chỉ mục!", "Chèn Chỉ Mục", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.None;
                return;
            }

            string leaderChar = ".";
            if (cboTabLeader.SelectedIndex == 1) leaderChar = "-";
            else if (cboTabLeader.SelectedIndex == 2) leaderChar = " ";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================== CHỈ MỤC TÀI LIỆU (INDEX) ==================");
            sb.AppendLine();

            char lastGroup = '\0';
            foreach (var kvp in map)
            {
                char firstChar = char.ToUpper(kvp.Key[0]);
                if (firstChar != lastGroup)
                {
                    lastGroup = firstChar;
                    sb.AppendLine(string.Format("• {0}", lastGroup));
                }

                string pagesStr = string.Join(", ", kvp.Value.ConvertAll(new Converter<int, string>(x => x.ToString())).ToArray());
                int totalLen = 50;
                int padLen = Math.Max(3, totalLen - kvp.Key.Length);
                string dots = new string(leaderChar[0], padLen);

                if (chkRightAlign.Checked)
                {
                    sb.AppendLine(string.Format("  {0} {1} Trang {2}", kvp.Key, dots, pagesStr));
                }
                else
                {
                    sb.AppendLine(string.Format("  {0}, Trang {1}", kvp.Key, pagesStr));
                }
            }

            sb.AppendLine();
            sb.AppendLine("==============================================================");
            sb.AppendLine();

            GeneratedIndex = sb.ToString();
        }
    }
}

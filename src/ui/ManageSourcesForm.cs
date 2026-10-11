using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class CitationSource
    {
        public string Id { get; set; }
        public string SourceType { get; set; } // Sách (Book), Tạp chí (Journal), Kỷ yếu (Conference), Trang web (Website), Báo cáo (Report)
        public string Author { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public string Publisher { get; set; }
        public string City { get; set; }
        public string JournalName { get; set; }
        public string Volume { get; set; }
        public string Pages { get; set; }
        public string Url { get; set; }

        public CitationSource()
        {
            Id = "SRC_" + DateTime.Now.Ticks.ToString().Substring(10);
            SourceType = "Sách (Book)";
            Author = "";
            Title = "";
            Year = DateTime.Now.Year;
            Publisher = "";
            City = "";
            JournalName = "";
            Volume = "";
            Pages = "";
            Url = "";
        }

        public string GetFormattedCitation(string style)
        {
            string firstAuthor = !string.IsNullOrEmpty(Author) ? Author.Split(',')[0].Trim() : "Khuyết danh";
            int y = Year > 0 ? Year : DateTime.Now.Year;

            if (style == "IEEE")
            {
                return "[1]";
            }
            else if (style == "MLA")
            {
                return string.Format("({0} {1})", firstAuthor, !string.IsNullOrEmpty(Pages) ? Pages : "1");
            }
            else if (style == "Chicago")
            {
                return string.Format("({0} {1})", firstAuthor, y);
            }
            else // APA / Harvard default
            {
                return string.Format("({0}, {1})", firstAuthor, y);
            }
        }

        public string GetFormattedBibliography(string style, int index = 1)
        {
            string auth = !string.IsNullOrEmpty(Author) ? Author : "Khuyết danh";
            string tit = !string.IsNullOrEmpty(Title) ? Title : "Tài liệu không tiêu đề";
            int y = Year > 0 ? Year : DateTime.Now.Year;
            string pub = !string.IsNullOrEmpty(Publisher) ? Publisher : "NXB";

            if (style == "IEEE")
            {
                return string.Format("[{0}] {1}, \"{2},\" {3}, {4}.", index, auth, tit, pub, y);
            }
            else if (style == "MLA")
            {
                return string.Format("{0}. \"{1}.\" {2}, {3}.", auth, tit, pub, y);
            }
            else if (style == "Chicago")
            {
                return string.Format("{0}. {1}. {2}: {3}.", auth, y, tit, pub);
            }
            else // APA 7th
            {
                return string.Format("{0} ({1}). {2}. {3}.", auth, y, tit, pub);
            }
        }
    }

    public class ManageSourcesForm : Form
    {
        private WriterView _writerView;
        private ListBox lstSources;
        private TextBox txtSearch;
        private Label lblPreviewCit;
        private Label lblPreviewBib;
        private ComboBox cboPreviewStyle;

        private ComboBox cboType;
        private TextBox txtAuthor;
        private TextBox txtTitle;
        private TextBox txtYear;
        private TextBox txtPublisher;
        private TextBox txtCity;
        private TextBox txtJournal;
        private TextBox txtPages;
        private Button btnSaveSource;
        private Button btnNewSource;
        private Button btnDeleteSource;

        private CitationSource _selectedSource = null;

        public ManageSourcesForm(WriterView writerView)
        {
            _writerView = writerView;

            this.Text = "Quản Lý Nguồn Trích Dẫn (Manage Sources)";
            this.Size = new Size(780, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            BuildUI();
            LoadSourceList();
        }

        private void BuildUI()
        {
            // Left panel: List and search
            Label lblSearch = new Label { Text = "Tìm kiếm nguồn:", Location = new Point(16, 12), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            this.Controls.Add(lblSearch);

            txtSearch = new TextBox { Location = new Point(16, 32), Size = new Size(260, 24) };
            txtSearch.TextChanged += (s, e) => FilterSourceList();
            this.Controls.Add(txtSearch);

            Label lblList = new Label { Text = "Danh mục tài liệu nguồn:", Location = new Point(16, 64), AutoSize = true };
            this.Controls.Add(lblList);

            lstSources = new ListBox { Location = new Point(16, 86), Size = new Size(260, 310), BorderStyle = BorderStyle.FixedSingle };
            lstSources.SelectedIndexChanged += LstSources_SelectedIndexChanged;
            this.Controls.Add(lstSources);

            btnNewSource = new Button
            {
                Text = "+ Thêm mới",
                Location = new Point(16, 404),
                Size = new Size(125, 30),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNewSource.FlatAppearance.BorderSize = 0;
            btnNewSource.Click += (s, e) => ResetFieldsForNew();
            this.Controls.Add(btnNewSource);

            btnDeleteSource = new Button
            {
                Text = "Xóa nguồn",
                Location = new Point(151, 404),
                Size = new Size(125, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDeleteSource.FlatAppearance.BorderSize = 0;
            btnDeleteSource.Click += BtnDeleteSource_Click;
            this.Controls.Add(btnDeleteSource);

            // Right panel: Editor
            GroupBox grpEdit = new GroupBox
            {
                Text = "Thông tin chi tiết tài liệu",
                Location = new Point(292, 12),
                Size = new Size(460, 384),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            this.Controls.Add(grpEdit);

            int y = 26;
            AddEditorRow(grpEdit, "Loại nguồn:", ref y, out cboType);
            cboType.Items.AddRange(new object[] { "Sách (Book)", "Bài báo tạp chí (Journal Article)", "Kỷ yếu hội thảo (Conference)", "Trang web (Website)", "Báo cáo khoa học (Report)" });
            cboType.SelectedIndex = 0;

            AddEditorRow(grpEdit, "Tác giả:", ref y, out txtAuthor);
            AddEditorRow(grpEdit, "Tựa đề:", ref y, out txtTitle);
            AddEditorRow(grpEdit, "Năm xuất bản:", ref y, out txtYear);
            AddEditorRow(grpEdit, "Nhà xuất bản / Tổ chức:", ref y, out txtPublisher);
            AddEditorRow(grpEdit, "Thành phố:", ref y, out txtCity);
            AddEditorRow(grpEdit, "Tên tạp chí / Hội thảo:", ref y, out txtJournal);
            AddEditorRow(grpEdit, "Trang trích dẫn (ví dụ: 12-25):", ref y, out txtPages);

            btnSaveSource = new Button
            {
                Text = "Lưu / Cập Nhật Nguồn",
                Location = new Point(170, y + 6),
                Size = new Size(270, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSaveSource.FlatAppearance.BorderSize = 0;
            btnSaveSource.Click += BtnSaveSource_Click;
            grpEdit.Controls.Add(btnSaveSource);

            // Bottom preview panel
            GroupBox grpPreview = new GroupBox
            {
                Text = "Xem trước trích dẫn & thư mục",
                Location = new Point(292, 400),
                Size = new Size(460, 100),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            this.Controls.Add(grpPreview);

            Label lblStyle = new Label { Text = "Kiểu:", Location = new Point(12, 22), AutoSize = true };
            grpPreview.Controls.Add(lblStyle);

            cboPreviewStyle = new ComboBox { Location = new Point(50, 19), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            cboPreviewStyle.Items.AddRange(new object[] { "APA 7th", "IEEE", "MLA 9th", "Chicago", "Harvard" });
            cboPreviewStyle.SelectedIndex = 0;
            cboPreviewStyle.SelectedIndexChanged += (s, e) => UpdatePreview();
            grpPreview.Controls.Add(cboPreviewStyle);

            lblPreviewCit = new Label { Text = "Trích dẫn: (Tác giả, Năm)", Location = new Point(170, 22), Size = new Size(275, 20), Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), ForeColor = Color.FromArgb(43, 87, 154) };
            grpPreview.Controls.Add(lblPreviewCit);

            lblPreviewBib = new Label { Text = "Thư mục: Tác giả (Năm). Tựa đề. NXB.", Location = new Point(12, 48), Size = new Size(435, 45), Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), ForeColor = Color.FromArgb(51, 65, 85) };
            grpPreview.Controls.Add(lblPreviewBib);

            // Dialog bottom
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(670, 8),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnClose);
            this.CancelButton = btnClose;
        }

        private void AddEditorRow(GroupBox grp, string label, ref int y, out TextBox tb)
        {
            Label lbl = new Label { Text = label, Location = new Point(12, y + 2), Size = new Size(150, 20), Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            grp.Controls.Add(lbl);
            tb = new TextBox { Location = new Point(170, y), Size = new Size(270, 22), Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            grp.Controls.Add(tb);
            y += 28;
        }

        private void AddEditorRow(GroupBox grp, string label, ref int y, out ComboBox cb)
        {
            Label lbl = new Label { Text = label, Location = new Point(12, y + 2), Size = new Size(150, 20), Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            grp.Controls.Add(lbl);
            cb = new ComboBox { Location = new Point(170, y), Size = new Size(270, 22), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            grp.Controls.Add(cb);
            y += 28;
        }

        private void LoadSourceList()
        {
            lstSources.Items.Clear();
            if (_writerView != null && _writerView.CitationSources != null)
            {
                foreach (var src in _writerView.CitationSources)
                {
                    string display = string.Format("{0} ({1}) - {2}",
                        !string.IsNullOrEmpty(src.Author) ? src.Author : "Khuyết danh",
                        src.Year,
                        !string.IsNullOrEmpty(src.Title) ? src.Title : "Không tiêu đề");
                    lstSources.Items.Add(display);
                }
            }
            if (lstSources.Items.Count > 0)
            {
                lstSources.SelectedIndex = 0;
            }
            else
            {
                ResetFieldsForNew();
            }
        }

        private void FilterSourceList()
        {
            string kw = txtSearch.Text.Trim().ToLower();
            lstSources.Items.Clear();
            if (_writerView != null && _writerView.CitationSources != null)
            {
                foreach (var src in _writerView.CitationSources)
                {
                    string display = string.Format("{0} ({1}) - {2}",
                        !string.IsNullOrEmpty(src.Author) ? src.Author : "Khuyết danh",
                        src.Year,
                        !string.IsNullOrEmpty(src.Title) ? src.Title : "Không tiêu đề");
                    if (string.IsNullOrEmpty(kw) || display.ToLower().Contains(kw))
                    {
                        lstSources.Items.Add(display);
                    }
                }
            }
        }

        private void LstSources_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstSources.SelectedIndex < 0 || _writerView == null || _writerView.CitationSources == null) return;
            int idx = lstSources.SelectedIndex;
            if (idx >= 0 && idx < _writerView.CitationSources.Count)
            {
                _selectedSource = _writerView.CitationSources[idx];
                cboType.SelectedItem = _selectedSource.SourceType;
                txtAuthor.Text = _selectedSource.Author;
                txtTitle.Text = _selectedSource.Title;
                txtYear.Text = _selectedSource.Year.ToString();
                txtPublisher.Text = _selectedSource.Publisher;
                txtCity.Text = _selectedSource.City;
                txtJournal.Text = _selectedSource.JournalName;
                txtPages.Text = _selectedSource.Pages;
                UpdatePreview();
            }
        }

        private void ResetFieldsForNew()
        {
            _selectedSource = null;
            lstSources.SelectedIndex = -1;
            cboType.SelectedIndex = 0;
            txtAuthor.Text = "";
            txtTitle.Text = "";
            txtYear.Text = DateTime.Now.Year.ToString();
            txtPublisher.Text = "";
            txtCity.Text = "";
            txtJournal.Text = "";
            txtPages.Text = "";
            lblPreviewCit.Text = "Trích dẫn: (Tác giả, Năm)";
            lblPreviewBib.Text = "Thư mục: Tác giả (Năm). Tựa đề. NXB.";
            txtAuthor.Focus();
        }

        private void BtnSaveSource_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtAuthor.Text.Trim()) && string.IsNullOrEmpty(txtTitle.Text.Trim()))
            {
                MessageBox.Show("Vui lòng nhập Tác giả hoặc Tựa đề của nguồn trích dẫn!", "Quản lý nguồn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int yr;
            if (!int.TryParse(txtYear.Text.Trim(), out yr)) yr = DateTime.Now.Year;

            if (_selectedSource == null)
            {
                _selectedSource = new CitationSource();
                if (_writerView != null)
                {
                    if (_writerView.CitationSources == null) _writerView.CitationSources = new List<CitationSource>();
                    _writerView.CitationSources.Add(_selectedSource);
                }
            }

            _selectedSource.SourceType = cboType.SelectedItem != null ? cboType.SelectedItem.ToString() : "Sách (Book)";
            _selectedSource.Author = txtAuthor.Text.Trim();
            _selectedSource.Title = txtTitle.Text.Trim();
            _selectedSource.Year = yr;
            _selectedSource.Publisher = txtPublisher.Text.Trim();
            _selectedSource.City = txtCity.Text.Trim();
            _selectedSource.JournalName = txtJournal.Text.Trim();
            _selectedSource.Pages = txtPages.Text.Trim();

            LoadSourceList();
            UpdatePreview();
            MessageBox.Show("Đã lưu nguồn tài liệu tham khảo thành công!", "Quản lý nguồn", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDeleteSource_Click(object sender, EventArgs e)
        {
            if (_selectedSource == null || _writerView == null || _writerView.CitationSources == null) return;
            if (MessageBox.Show("Bạn có chắc chắn muốn xóa nguồn này khỏi danh sách?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _writerView.CitationSources.Remove(_selectedSource);
                LoadSourceList();
                ResetFieldsForNew();
            }
        }

        private void UpdatePreview()
        {
            if (_selectedSource == null)
            {
                int y;
                if (!int.TryParse(txtYear.Text, out y)) y = 2024;
                _selectedSource = new CitationSource
                {
                    Author = !string.IsNullOrEmpty(txtAuthor.Text) ? txtAuthor.Text : "Nguyễn Văn A",
                    Title = !string.IsNullOrEmpty(txtTitle.Text) ? txtTitle.Text : "Giáo trình Khoa học",
                    Year = y,
                    Publisher = !string.IsNullOrEmpty(txtPublisher.Text) ? txtPublisher.Text : "NXB Đại Học"
                };
            }
            string style = cboPreviewStyle.SelectedItem != null ? cboPreviewStyle.SelectedItem.ToString() : "APA 7th";
            if (style.StartsWith("APA")) style = "APA";
            else if (style.StartsWith("MLA")) style = "MLA";
            else if (style.StartsWith("IEEE")) style = "IEEE";
            else if (style.StartsWith("Chicago")) style = "Chicago";
            else style = "Harvard";

            lblPreviewCit.Text = "Trích dẫn: " + _selectedSource.GetFormattedCitation(style);
            lblPreviewBib.Text = "Thư mục: " + _selectedSource.GetFormattedBibliography(style, 1);
        }
    }
}

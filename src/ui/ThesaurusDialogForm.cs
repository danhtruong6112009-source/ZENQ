using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ThesaurusDialogForm : Form
    {
        private TextBox txtSearch;
        private Button btnSearch;
        private ListBox lstResults;
        private Button btnInsert;
        private Button btnClose;
        private Label lblHeader;
        private Label lblCount;
        private WriterView _writerView;

        private static Dictionary<string, List<string>> _synonymDict = null;

        public ThesaurusDialogForm(string initialWord, WriterView writerView)
        {
            _writerView = writerView;
            InitDictionary();
            InitializeComponent();

            if (!string.IsNullOrEmpty(initialWord))
            {
                txtSearch.Text = initialWord.Trim();
                PerformSearch(txtSearch.Text);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Từ điển đồng nghĩa (Thesaurus)";
            this.Size = new Size(380, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Top Header Panel
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 52;
            topPanel.BackColor = Color.FromArgb(16, 110, 190);
            topPanel.Paint += (s, e) =>
            {
                using (Font hf = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (Font sub = new Font("Segoe UI", 8.2f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("📖 Từ điển đồng nghĩa", hf, Brushes.White, 14, 8);
                    e.Graphics.DrawString("Gợi ý từ ngữ đồng nghĩa và mở rộng vốn từ cho văn bản", sub, Brushes.WhiteSmoke, 16, 30);
                }
            };
            this.Controls.Add(topPanel);

            // Search box
            Label lblSearch = new Label();
            lblSearch.Text = "Tra cứu từ:";
            lblSearch.Location = new Point(14, 64);
            lblSearch.AutoSize = true;
            lblSearch.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblSearch.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblSearch);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(14, 86);
            txtSearch.Size = new Size(250, 26);
            txtSearch.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformSearch(txtSearch.Text);
                }
            };
            this.Controls.Add(txtSearch);

            btnSearch = new Button();
            btnSearch.Text = "Tìm kiếm";
            btnSearch.Location = new Point(270, 84);
            btnSearch.Size = new Size(82, 28);
            btnSearch.BackColor = Color.FromArgb(16, 110, 190);
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => PerformSearch(txtSearch.Text);
            this.Controls.Add(btnSearch);

            lblHeader = new Label();
            lblHeader.Text = "Danh sách từ đồng nghĩa / tương đương:";
            lblHeader.Location = new Point(14, 122);
            lblHeader.AutoSize = true;
            lblHeader.ForeColor = Color.FromArgb(96, 94, 92);
            this.Controls.Add(lblHeader);

            lstResults = new ListBox();
            lstResults.Location = new Point(14, 144);
            lstResults.Size = new Size(338, 230);
            lstResults.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lstResults.ItemHeight = 22;
            lstResults.DoubleClick += (s, e) => ApplySelectedWord();
            this.Controls.Add(lstResults);

            lblCount = new Label();
            lblCount.Location = new Point(14, 380);
            lblCount.AutoSize = true;
            lblCount.Font = new Font("Segoe UI", 8f, FontStyle.Italic);
            lblCount.ForeColor = Color.Gray;
            this.Controls.Add(lblCount);

            // Bottom Buttons
            btnInsert = new Button();
            btnInsert.Text = "Chèn vào tài liệu";
            btnInsert.Location = new Point(144, 404);
            btnInsert.Size = new Size(120, 30);
            btnInsert.BackColor = Color.FromArgb(16, 110, 190);
            btnInsert.ForeColor = Color.White;
            btnInsert.FlatStyle = FlatStyle.Flat;
            btnInsert.FlatAppearance.BorderSize = 0;
            btnInsert.Click += (s, e) => ApplySelectedWord();
            this.Controls.Add(btnInsert);

            btnClose = new Button();
            btnClose.Text = "Đóng";
            btnClose.Location = new Point(272, 404);
            btnClose.Size = new Size(80, 30);
            btnClose.BackColor = Color.FromArgb(235, 236, 238);
            btnClose.ForeColor = Color.FromArgb(50, 49, 48);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = Color.LightGray;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void PerformSearch(string query)
        {
            lstResults.Items.Clear();
            if (string.IsNullOrEmpty(query))
            {
                lblCount.Text = "Vui lòng nhập từ cần tra cứu.";
                return;
            }

            string key = query.Trim().ToLowerInvariant();
            List<string> found = null;

            if (_synonymDict.ContainsKey(key))
            {
                found = _synonymDict[key];
            }
            else
            {
                // Fuzzy search / prefix search
                foreach (var kvp in _synonymDict)
                {
                    if (kvp.Key.Contains(key) || key.Contains(kvp.Key))
                    {
                        found = kvp.Value;
                        break;
                    }
                }
            }

            if (found != null && found.Count > 0)
            {
                foreach (var item in found)
                {
                    lstResults.Items.Add(item);
                }
                lstResults.SelectedIndex = 0;
                lblCount.Text = string.Format("Tìm thấy {0} gợi ý đồng nghĩa.", found.Count);
            }
            else
            {
                // Smart fallback extensions
                lstResults.Items.Add(query + " (ngữ cảnh trang trọng)");
                lstResults.Items.Add(query + " (diễn đạt mở rộng)");
                lstResults.Items.Add(query + " tương ứng");
                lblCount.Text = "Không có trong từ điển chuẩn. Đã tạo gợi ý ngữ cảnh.";
            }
        }

        private void ApplySelectedWord()
        {
            if (lstResults.SelectedItem == null || _writerView == null) return;
            string word = lstResults.SelectedItem.ToString();
            int parenIdx = word.IndexOf('(');
            if (parenIdx > 0) word = word.Substring(0, parenIdx).Trim();

            _writerView.ReplaceWordAtCursor(word);
            this.Close();
        }

        private static void InitDictionary()
        {
            if (_synonymDict != null) return;
            _synonymDict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Vietnamese
                { "phát triển", new List<string> { "tiến bộ", "vươn lên", "mở rộng", "tăng trưởng", "thăng tiến", "hoàn thiện" } },
                { "đẹp", new List<string> { "xinh đẹp", "diễm lệ", "mỹ miều", "lộng lẫy", "hài hòa", "tươi tắn" } },
                { "nhanh", new List<string> { "mau chóng", "chóng vánh", "tốc hành", "hỏa tốc", "khẩn trương", "thần tốc" } },
                { "thành công", new List<string> { "thắng lợi", "thành tựu", "đạt kết quả", "hoàn thành tốt", "viên mãn" } },
                { "quan trọng", new List<string> { "thiết yếu", "cốt lõi", "then chốt", "chủ chốt", "trọng đại", "trọng yếu" } },
                { "học tập", new List<string> { "nghiên cứu", "trau dồi", "rèn luyện", "học hỏi", "tiếp thu", "lĩnh hội" } },
                { "công việc", new List<string> { "nhiệm vụ", "công tác", "trách nhiệm", "sứ mệnh", "trọng trách" } },
                { "tài liệu", new List<string> { "văn bản", "hồ sơ", "giấy tờ", "ấn phẩm", "tập lưu" } },
                { "báo cáo", new List<string> { "thông báo", "trình bày", "tường trình", "tổng kết", "biên bản" } },
                { "doanh nghiệp", new List<string> { "công ty", "tổ chức", "đơn vị", "tập đoàn", "cơ sở" } },
                { "thị trường", new List<string> { "thương trường", "ngành hàng", "phân khúc", "sàn giao dịch" } },
                { "xây dựng", new List<string> { "thiết lập", "hình thành", "kiến tạo", "tạo lập", "phát triển" } },
                { "thực hiện", new List<string> { "tiến hành", "triển khai", "áp dụng", "thi hành", "vận hành" } },
                { "giúp", new List<string> { "hỗ trợ", "trợ giúp", "tương trợ", "tiếp sức", "tạo điều kiện" } },
                { "tốt", new List<string> { "xuất sắc", "hiệu quả", "ưu việt", "chất lượng", "hoàn hảo" } },
                { "khó", new List<string> { "phức tạp", "thử thách", "gian nan", "trở ngại", "chông gai" } },
                { "mới", new List<string> { "tân tiến", "hiện đại", "đột phá", "sáng tạo", "mới mẻ" } },
                { "thay đổi", new List<string> { "chuyển biến", "cải cách", "đổi mới", "biến chuyển", "cải tiến" } },
                { "giải quyết", new List<string> { "xử lý", "tháo gỡ", "khắc phục", "dàn xếp", "hoàn tất" } },
                { "kế hoạch", new List<string> { "phương án", "chiến lược", "dự án", "đề án", "lộ trình" } },

                // English
                { "good", new List<string> { "excellent", "superior", "fine", "valuable", "positive", "beneficial" } },
                { "great", new List<string> { "grand", "immense", "fantastic", "remarkable", "extraordinary" } },
                { "important", new List<string> { "crucial", "essential", "significant", "critical", "vital" } },
                { "fast", new List<string> { "quick", "rapid", "swift", "speedy", "brisk" } },
                { "make", new List<string> { "create", "produce", "generate", "build", "form" } },
                { "document", new List<string> { "file", "record", "paper", "report", "text" } },
                { "change", new List<string> { "modify", "alter", "transform", "adjust", "revise" } },
                { "help", new List<string> { "assist", "support", "aid", "facilitate", "serve" } },
                { "work", new List<string> { "task", "job", "labor", "duty", "assignment" } },
                { "new", new List<string> { "modern", "novel", "fresh", "innovative", "recent" } }
            };
        }
    }
}

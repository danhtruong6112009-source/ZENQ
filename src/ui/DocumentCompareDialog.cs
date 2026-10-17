using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class DocumentCompareDialog : Form
    {
        private WriterView _writerView;
        private TextBox _txtOriginalPath;
        private TextBox _txtRevisedPath;
        private Button _btnBrowseOrig;
        private Button _btnBrowseRev;
        private Button _btnCompare;
        private Button _btnApply;
        private Button _btnClose;

        private GroupBox _grpResults;
        private Label _lblSimilarity;
        private Label _lblInsertions;
        private Label _lblDeletions;
        private Label _lblUnchanged;
        private ProgressBar _prgSimilarity;

        private DocumentCompareResult _lastResult;

        public DocumentCompareDialog(WriterView writerView)
        {
            _writerView = writerView;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "So sánh tài liệu (Document Compare & Legal Blackline)";
            this.Size = new Size(540, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // Original Document Section
            Label lblOrig = new Label { Text = "Tài liệu gốc (Original Document):", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _txtOriginalPath = new TextBox { Location = new Point(20, 38), Width = 380, Text = "[Tài liệu đang mở hiện tại]" };
            _btnBrowseOrig = new Button { Text = "Duyệt...", Location = new Point(410, 37), Width = 95, Height = 25 };
            _btnBrowseOrig.Click += (s, e) => BrowseFile(_txtOriginalPath);

            // Revised Document Section
            Label lblRev = new Label { Text = "Tài liệu sửa đổi (Revised Document):", Location = new Point(20, 75), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _txtRevisedPath = new TextBox { Location = new Point(20, 97), Width = 380 };
            _btnBrowseRev = new Button { Text = "Duyệt...", Location = new Point(410, 96), Width = 95, Height = 25 };
            _btnBrowseRev.Click += (s, e) => BrowseFile(_txtRevisedPath);

            // Compare Action Button
            _btnCompare = new Button
            {
                Text = "⚡ Tiến hành So sánh",
                Location = new Point(20, 134),
                Width = 485,
                Height = 32,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCompare.Click += BtnCompare_Click;

            // Results GroupBox
            _grpResults = new GroupBox
            {
                Text = "Kết quả phân tích chênh lệch (Diff Metrics)",
                Location = new Point(20, 180),
                Width = 485,
                Height = 145,
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            _lblSimilarity = new Label { Text = "Độ tương đồng: -- %", Location = new Point(20, 28), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            _prgSimilarity = new ProgressBar { Location = new Point(20, 50), Width = 445, Height = 18, Value = 0, Maximum = 100 };

            _lblInsertions = new Label { Text = "Nội dung thêm mới: 0 từ", Location = new Point(20, 80), AutoSize = true, ForeColor = Color.FromArgb(16, 185, 129), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _lblDeletions = new Label { Text = "Nội dung bị xóa: 0 từ", Location = new Point(200, 80), AutoSize = true, ForeColor = Color.FromArgb(239, 68, 68), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _lblUnchanged = new Label { Text = "Nội dung giữ nguyên: 0 từ", Location = new Point(20, 110), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };

            _grpResults.Controls.AddRange(new Control[] { _lblSimilarity, _prgSimilarity, _lblInsertions, _lblDeletions, _lblUnchanged });

            // Bottom Buttons
            _btnApply = new Button
            {
                Text = "Áp dụng vào Văn bản (Track Changes)",
                Location = new Point(20, 338),
                Width = 260,
                Height = 32,
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            _btnApply.Click += BtnApply_Click;

            _btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(415, 338),
                Width = 90,
                Height = 32
            };
            _btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                lblOrig, _txtOriginalPath, _btnBrowseOrig,
                lblRev, _txtRevisedPath, _btnBrowseRev,
                _btnCompare, _grpResults, _btnApply, _btnClose
            });
        }

        private void BrowseFile(TextBox targetBox)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Tệp văn bản (*.docx;*.odt;*.txt;*.rtf;*.md)|*.docx;*.odt;*.txt;*.rtf;*.md|Tất cả tệp (*.*)|*.*";
                ofd.Title = "Chọn tệp văn bản để so sánh";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    targetBox.Text = ofd.FileName;
                }
            }
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            string origText = "";
            string revText = "";

            // Read original
            if (_txtOriginalPath.Text.Contains("[Tài liệu đang mở hiện tại]"))
            {
                origText = _writerView != null && _writerView.PieceTable != null ? _writerView.PieceTable.GetText() : "";
            }
            else if (File.Exists(_txtOriginalPath.Text))
            {
                origText = ReadFileContent(_txtOriginalPath.Text);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn tài liệu gốc hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Read revised
            if (File.Exists(_txtRevisedPath.Text))
            {
                revText = ReadFileContent(_txtRevisedPath.Text);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn tài liệu sửa đổi cần so sánh.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor = Cursors.WaitCursor;
            try
            {
                _lastResult = DocumentCompareEngine.CompareDocuments(origText, revText);
                _lblSimilarity.Text = $"Độ tương đồng: {_lastResult.SimilarityPercentage}%";
                _prgSimilarity.Value = Math.Max(0, Math.Min(100, (int)_lastResult.SimilarityPercentage));
                _lblInsertions.Text = $"Nội dung thêm mới: +{_lastResult.InsertionsCount} từ";
                _lblDeletions.Text = $"Nội dung bị xóa: -{_lastResult.DeletionsCount} từ";
                _lblUnchanged.Text = $"Nội dung giữ nguyên: {_lastResult.UnchangedWordsCount} từ";
                _btnApply.Enabled = true;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (_lastResult != null && _writerView != null)
            {
                DocumentCompareEngine.ApplyToWriterView(_writerView, _lastResult);
                MessageBox.Show("Đã nạp toàn bộ các thay đổi so sánh vào chế độ Theo dõi sửa đổi (Track Changes) của tài liệu!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private static string ReadFileContent(string filePath)
        {
            try
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".txt" || ext == ".md" || ext == ".csv")
                {
                    return File.ReadAllText(filePath);
                }
                // Try reading file text
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }
    }
}

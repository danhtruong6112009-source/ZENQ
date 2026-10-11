using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class SpellCheckItem
    {
        public string Location { get; set; } // e.g. "Ô B4" or "Đoạn 2"
        public string OriginalText { get; set; }
        public string ProblemWord { get; set; }
        public string Reason { get; set; }
        public List<string> Suggestions { get; set; }
        public Action<string> ApplyCorrection { get; set; }

        public SpellCheckItem()
        {
            Suggestions = new List<string>();
        }
    }

    public class SpellCheckDialog : Form
    {
        private Label lblLocation;
        private Label lblReason;
        private TextBox txtOriginal;
        private TextBox txtChangeTo;
        private ListBox lstSuggestions;
        private Button btnIgnore;
        private Button btnChange;
        private Button btnChangeAll;
        private Button btnClose;

        private List<SpellCheckItem> _items;
        private int _currentIndex = 0;
        private Form _parentForm;

        public SpellCheckDialog(List<SpellCheckItem> items, Form parentForm = null)
        {
            _items = items != null ? items : new List<SpellCheckItem>();
            _parentForm = parentForm;
            InitializeUI();
            LoadCurrentItem();
        }

        private void InitializeUI()
        {
            this.Text = "Kiểm Tra Chính Tả (Spelling)";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(480, 360);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(14, 116, 144)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Font hFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 8f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("✓ Kiểm Tra Chính Tả & Ngữ Pháp", hFont, Brushes.White, 14, 8);
                    e.Graphics.DrawString("Phát hiện và sửa lỗi từ ngữ, lặp từ, định dạng trong bảng tính", subFont, Brushes.WhiteSmoke, 16, 28);
                }
            };
            this.Controls.Add(pnlHeader);

            lblLocation = new Label
            {
                Text = "Vị trí:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 116, 144),
                Location = new Point(16, 60),
                AutoSize = true
            };
            this.Controls.Add(lblLocation);

            lblReason = new Label
            {
                Text = "Lỗi:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(16, 80),
                AutoSize = true
            };
            this.Controls.Add(lblReason);

            Label lblNotDict = new Label
            {
                Text = "Từ / Câu gốc:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 104),
                AutoSize = true
            };
            this.Controls.Add(lblNotDict);

            txtOriginal = new TextBox
            {
                Location = new Point(16, 124),
                Size = new Size(320, 24),
                ReadOnly = true,
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            this.Controls.Add(txtOriginal);

            Label lblChange = new Label
            {
                Text = "Thay đổi thành:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 154),
                AutoSize = true
            };
            this.Controls.Add(lblChange);

            txtChangeTo = new TextBox
            {
                Location = new Point(16, 174),
                Size = new Size(320, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            this.Controls.Add(txtChangeTo);

            Label lblSugg = new Label
            {
                Text = "Gợi ý chuẩn:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 204),
                AutoSize = true
            };
            this.Controls.Add(lblSugg);

            lstSuggestions = new ListBox
            {
                Location = new Point(16, 224),
                Size = new Size(320, 80),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            lstSuggestions.SelectedIndexChanged += (s, e) =>
            {
                if (lstSuggestions.SelectedItem != null)
                {
                    txtChangeTo.Text = lstSuggestions.SelectedItem.ToString();
                }
            };
            this.Controls.Add(lstSuggestions);

            // Side Action Buttons
            int btnX = 350;
            btnIgnore = new Button
            {
                Text = "Bỏ qua",
                Size = new Size(100, 28),
                Location = new Point(btnX, 122),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnIgnore.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnIgnore.Click += (s, e) => { NextItem(); };
            this.Controls.Add(btnIgnore);

            btnChange = new Button
            {
                Text = "Thay đổi",
                Size = new Size(100, 28),
                Location = new Point(btnX, 156),
                BackColor = Color.FromArgb(14, 116, 144),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChange.FlatAppearance.BorderSize = 0;
            btnChange.Click += (s, e) =>
            {
                ApplyCurrentCorrection(txtChangeTo.Text.Trim());
                NextItem();
            };
            this.Controls.Add(btnChange);

            btnChangeAll = new Button
            {
                Text = "Đổi tất cả",
                Size = new Size(100, 28),
                Location = new Point(btnX, 190),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChangeAll.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnChangeAll.Click += (s, e) =>
            {
                string repText = txtChangeTo.Text.Trim();
                string probWord = _items[_currentIndex].ProblemWord;
                for (int k = _currentIndex; k < _items.Count; k++)
                {
                    if (_items[k].ProblemWord == probWord)
                    {
                        if (_items[k].ApplyCorrection != null) _items[k].ApplyCorrection(repText);
                    }
                }
                NextItem();
            };
            this.Controls.Add(btnChangeAll);

            btnClose = new Button
            {
                Text = "Đóng",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 28),
                Location = new Point(btnX, 276),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClose.Click += (s, e) => { this.Close(); };
            this.Controls.Add(btnClose);
        }

        private void LoadCurrentItem()
        {
            if (_items == null || _items.Count == 0 || _currentIndex >= _items.Count)
            {
                MessageBox.Show("Kiểm tra chính tả hoàn tất! Không còn lỗi nào khác trên trang tính.", "Chính Tả Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                return;
            }

            var item = _items[_currentIndex];
            lblLocation.Text = string.Format("Vị trí: {0} ({1}/{2})", item.Location, _currentIndex + 1, _items.Count);
            lblReason.Text = "Lỗi: " + item.Reason;
            txtOriginal.Text = item.OriginalText;

            lstSuggestions.Items.Clear();
            if (item.Suggestions != null && item.Suggestions.Count > 0)
            {
                foreach (var sugg in item.Suggestions) lstSuggestions.Items.Add(sugg);
                lstSuggestions.SelectedIndex = 0;
                txtChangeTo.Text = item.Suggestions[0];
            }
            else
            {
                txtChangeTo.Text = item.OriginalText;
            }
            txtChangeTo.SelectAll();
        }

        private void ApplyCurrentCorrection(string newText)
        {
            if (_currentIndex < _items.Count)
            {
                var item = _items[_currentIndex];
                if (item.ApplyCorrection != null)
                {
                    item.ApplyCorrection(newText);
                }
                if (_parentForm != null) _parentForm.Invalidate();
            }
        }

        private void NextItem()
        {
            _currentIndex++;
            LoadCurrentItem();
        }
    }
}

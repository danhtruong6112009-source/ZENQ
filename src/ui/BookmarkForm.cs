using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class BookmarkForm : Form
    {
        public string AddedBookmarkName { get; private set; }
        public string TargetBookmarkToGo { get; private set; }

        private TextBox txtName;
        private ListBox lstBookmarks;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnGoTo;
        private Button btnClose;
        private WriterView _writerView;

        public BookmarkForm(WriterView writerView = null, List<string> existingBookmarks = null)
        {
            _writerView = writerView;

            this.Text = "Đánh Dấu Trang (Bookmark)";
            this.Size = new Size(380, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            Label lblName = new Label { Text = "Tên dấu trang (Bookmark name):", Location = new Point(18, 15), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblName);

            txtName = new TextBox { Location = new Point(18, 38), Width = 230 };
            this.Controls.Add(txtName);

            btnAdd = new Button { Text = "Thêm", Location = new Point(260, 36), Size = new Size(88, 26), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                string bName = txtName.Text.Trim();
                if (!string.IsNullOrEmpty(bName))
                {
                    AddedBookmarkName = bName;
                    if (!lstBookmarks.Items.Contains(bName))
                    {
                        lstBookmarks.Items.Add(bName);
                    }
                    if (_writerView != null && !_writerView.Bookmarks.Contains(bName))
                    {
                        _writerView.Bookmarks.Add(bName);
                    }
                    txtName.Clear();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập tên cho dấu trang!", "Dấu trang", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            this.Controls.Add(btnAdd);

            Label lblList = new Label { Text = "Danh sách dấu trang hiện có:", Location = new Point(18, 75), AutoSize = true };
            this.Controls.Add(lblList);

            lstBookmarks = new ListBox { Location = new Point(18, 98), Size = new Size(230, 180), BorderStyle = BorderStyle.FixedSingle };
            if (existingBookmarks != null)
            {
                foreach (var b in existingBookmarks) lstBookmarks.Items.Add(b);
            }
            else if (_writerView != null && _writerView.Bookmarks != null)
            {
                foreach (var b in _writerView.Bookmarks) lstBookmarks.Items.Add(b);
            }
            this.Controls.Add(lstBookmarks);

            btnDelete = new Button { Text = "Xóa", Location = new Point(260, 98), Size = new Size(88, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelete.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            btnDelete.Click += (s, e) =>
            {
                if (lstBookmarks.SelectedIndex >= 0)
                {
                    string delName = lstBookmarks.SelectedItem.ToString();
                    lstBookmarks.Items.RemoveAt(lstBookmarks.SelectedIndex);
                    if (_writerView != null && _writerView.Bookmarks.Contains(delName))
                    {
                        _writerView.Bookmarks.Remove(delName);
                    }
                }
            };
            this.Controls.Add(btnDelete);

            btnGoTo = new Button { Text = "Đi tới (Go to)", Location = new Point(260, 134), Size = new Size(88, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGoTo.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            btnGoTo.Click += (s, e) =>
            {
                if (lstBookmarks.SelectedItem != null)
                {
                    TargetBookmarkToGo = lstBookmarks.SelectedItem.ToString();
                    this.DialogResult = DialogResult.Yes;
                    this.Close();
                }
            };
            this.Controls.Add(btnGoTo);

            btnClose = new Button { Text = "Đóng", Location = new Point(260, 252), Size = new Size(88, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            this.Controls.Add(btnClose);

            this.AcceptButton = btnAdd;
            this.CancelButton = btnClose;
        }
    }
}

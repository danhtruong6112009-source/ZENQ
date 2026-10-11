using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice
{
    public class NewCommentDialogForm : Form
    {
        public string CommentText { get; private set; }
        private TextBox _txtComment;
        private string _selectedSnippet;

        public NewCommentDialogForm(string selectedSnippet = "")
        {
            _selectedSnippet = selectedSnippet;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Thêm Bình luận (New Comment)";
            this.Size = new Size(420, 290);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                // Green plus badge
                using (Pen p = new Pen(Color.FromArgb(22, 163, 74), 2f))
                {
                    e.Graphics.DrawLine(p, 16, 22, 26, 22);
                    e.Graphics.DrawLine(p, 21, 17, 21, 27);
                }
                using (Font hf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                {
                    e.Graphics.DrawString("New Comment (Thêm bình luận)", hf, new SolidBrush(Color.FromArgb(15, 23, 42)), 32, 12);
                }
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, 43, pnlHeader.Width, 43);
            };
            this.Controls.Add(pnlHeader);

            // Selected Text Snippet box (if any)
            int contentTop = 52;
            if (!string.IsNullOrEmpty(_selectedSnippet))
            {
                Panel pnlSnippet = new Panel
                {
                    Location = new Point(16, contentTop),
                    Size = new Size(372, 38),
                    BackColor = Color.FromArgb(243, 232, 255)
                };
                pnlSnippet.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                    using (SolidBrush barBrush = new SolidBrush(Color.FromArgb(147, 51, 234)))
                    {
                        e.Graphics.FillRectangle(barBrush, 0, 0, 4, pnlSnippet.Height);
                    }
                    string displayTxt = _selectedSnippet.Length > 50 ? _selectedSnippet.Substring(0, 47) + "..." : _selectedSnippet;
                    using (Font f = new Font("Segoe UI", 8.2f, FontStyle.Italic))
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(88, 28, 135)))
                    {
                        e.Graphics.DrawString("\"" + displayTxt + "\"", f, tb, 10, 11);
                    }
                };
                this.Controls.Add(pnlSnippet);
                contentTop += 44;
            }

            // Author & Time label
            Label lblAuthor = new Label
            {
                Text = "Tác giả: " + Environment.UserName + " • " + DateTime.Now.ToString("HH:mm - dd/MM/yyyy"),
                Location = new Point(16, contentTop),
                Size = new Size(372, 18),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular)
            };
            this.Controls.Add(lblAuthor);
            contentTop += 22;

            // Comment text box
            _txtComment = new TextBox
            {
                Location = new Point(16, contentTop),
                Size = new Size(372, 75),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(_txtComment);

            // Bottom Buttons
            Button btnPost = new Button
            {
                Text = "Đăng (Post)",
                Size = new Size(95, 30),
                Location = new Point(195, 210),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPost.FlatAppearance.BorderSize = 0;
            btnPost.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtComment.Text))
                {
                    MessageBox.Show("Vui lòng nhập nội dung bình luận!", "ZENQ OFFICE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                CommentText = _txtComment.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnPost);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(80, 30),
                Location = new Point(298, 210),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnPost;
            this.CancelButton = btnCancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtComment.Focus();
        }
    }
}

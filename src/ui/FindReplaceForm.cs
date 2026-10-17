using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class FindReplaceForm : Form
    {
        private TextBox txtFind;
        private TextBox txtReplace;
        private Button btnFindNext;
        private Button btnReplace;
        private Button btnReplaceAll;
        private Button btnClose;

        public event Action<string> OnFindNext;
        public event Action<string, string> OnReplace;
        public event Action<string, string> OnReplaceAll;

        public FindReplaceForm()
        {
            Text = "Tìm kiếm & Thay thế - ZenQ Writer";
            Size = new Size(460, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(243, 242, 241);
            Font = new Font("Segoe UI", 9f);

            Label lblFind = new Label { Text = "Nội dung tìm kiếm:", Location = new Point(15, 20), AutoSize = true };
            txtFind = new TextBox { Location = new Point(135, 18), Width = 285 };

            Label lblReplace = new Label { Text = "Thay thế bằng:", Location = new Point(15, 55), AutoSize = true };
            txtReplace = new TextBox { Location = new Point(135, 53), Width = 285 };

            btnFindNext = new Button
            {
                Text = "Tìm tiếp",
                Location = new Point(15, 105),
                Width = 92,
                Height = 30,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE721', 13, 13, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnReplace = new Button
            {
                Text = "Thay thế",
                Location = new Point(115, 105),
                Width = 95,
                Height = 30,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8AC', 13, 13, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnReplaceAll = new Button
            {
                Text = "Thay tất cả",
                Location = new Point(218, 105),
                Width = 110,
                Height = 30,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8EE', 13, 13, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(336, 105),
                Width = 84,
                Height = 30,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 12, 12, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(3, 0, 3, 0)
            };

            btnFindNext.Click += (s, e) => { if (!string.IsNullOrEmpty(txtFind.Text) && OnFindNext != null) OnFindNext(txtFind.Text); };
            btnReplace.Click += (s, e) => { if (!string.IsNullOrEmpty(txtFind.Text) && OnReplace != null) OnReplace(txtFind.Text, txtReplace.Text); };
            btnReplaceAll.Click += (s, e) => { if (!string.IsNullOrEmpty(txtFind.Text) && OnReplaceAll != null) OnReplaceAll(txtFind.Text, txtReplace.Text); };
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblFind);
            Controls.Add(txtFind);
            Controls.Add(lblReplace);
            Controls.Add(txtReplace);
            Controls.Add(btnFindNext);
            Controls.Add(btnReplace);
            Controls.Add(btnReplaceAll);
            Controls.Add(btnClose);
        }
    }
}

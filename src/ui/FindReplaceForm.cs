using System;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
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
            Size = new Size(420, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(243, 242, 241);

            Label lblFind = new Label { Text = "Nội dung tìm kiếm:", Location = new Point(15, 20), AutoSize = true };
            txtFind = new TextBox { Location = new Point(130, 18), Width = 250 };

            Label lblReplace = new Label { Text = "Thay thế bằng:", Location = new Point(15, 55), AutoSize = true };
            txtReplace = new TextBox { Location = new Point(130, 53), Width = 250 };

            btnFindNext = new Button { Text = "Tìm tiếp", Location = new Point(15, 105), Width = 85, Height = 30 };
            btnReplace = new Button { Text = "Thay thế", Location = new Point(110, 105), Width = 85, Height = 30 };
            btnReplaceAll = new Button { Text = "Thay thế tất cả", Location = new Point(205, 105), Width = 100, Height = 30 };
            btnClose = new Button { Text = "Đóng", Location = new Point(315, 105), Width = 70, Height = 30 };

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

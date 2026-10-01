using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice
{
    public class WordCountForm : Form
    {
        public WordCountForm(int pages, int words, int charsNoSpaces, int charsWithSpaces, int paragraphs, int lines)
        {
            this.Text = "Thống kê số từ (Word Count)";
            this.Size = new Size(360, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 8;
            mainPanel.Padding = new Padding(24, 20, 24, 16);
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            AddStatRow(mainPanel, "Số trang (Pages):", pages.ToString("N0"), 0);
            AddStatRow(mainPanel, "Số từ (Words):", words.ToString("N0"), 1);
            AddStatRow(mainPanel, "Ký tự (không khoảng trắng):", charsNoSpaces.ToString("N0"), 2);
            AddStatRow(mainPanel, "Ký tự (có khoảng trắng):", charsWithSpaces.ToString("N0"), 3);
            AddStatRow(mainPanel, "Số đoạn văn (Paragraphs):", paragraphs.ToString("N0"), 4);
            AddStatRow(mainPanel, "Số dòng (Lines):", lines.ToString("N0"), 5);

            CheckBox chkInclude = new CheckBox();
            chkInclude.Text = "Tính cả hộp văn bản và chú thích";
            chkInclude.Checked = true;
            chkInclude.AutoSize = true;
            chkInclude.ForeColor = Color.FromArgb(71, 85, 105);
            mainPanel.SetColumnSpan(chkInclude, 2);
            mainPanel.Controls.Add(chkInclude, 0, 6);

            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Fill;
            Button btnClose = new Button();
            btnClose.Text = "Đóng";
            btnClose.Size = new Size(88, 30);
            btnClose.Location = new Point(bottomPanel.Width - 90, 2);
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnClose.BackColor = Color.FromArgb(37, 99, 235);
            btnClose.ForeColor = Color.White;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => { this.Close(); };
            bottomPanel.Controls.Add(btnClose);

            mainPanel.SetColumnSpan(bottomPanel, 2);
            mainPanel.Controls.Add(bottomPanel, 0, 7);

            this.Controls.Add(mainPanel);
            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;
        }

        private void AddStatRow(TableLayoutPanel panel, string label, string value, int row)
        {
            Label lblName = new Label();
            lblName.Text = label;
            lblName.AutoSize = true;
            lblName.Dock = DockStyle.Fill;
            lblName.TextAlign = ContentAlignment.MiddleLeft;
            lblName.ForeColor = Color.FromArgb(51, 65, 85);
            lblName.Padding = new Padding(0, 4, 0, 4);

            Label lblVal = new Label();
            lblVal.Text = value;
            lblVal.AutoSize = true;
            lblVal.Dock = DockStyle.Fill;
            lblVal.TextAlign = ContentAlignment.MiddleRight;
            lblVal.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            lblVal.ForeColor = Color.FromArgb(15, 23, 42);
            lblVal.Padding = new Padding(0, 4, 0, 4);

            panel.Controls.Add(lblName, 0, row);
            panel.Controls.Add(lblVal, 1, row);
        }
    }
}

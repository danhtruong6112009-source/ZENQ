using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public struct DocumentStatistics
    {
        public int Pages;
        public int Words;
        public int CharactersNoSpaces;
        public int CharactersWithSpaces;
        public int Paragraphs;
        public int Lines;
    }

    public class WordCountDialog : Form
    {
        public WordCountDialog(DocumentStatistics stats)
        {
            InitializeComponent(stats);
        }

        private void InitializeComponent(DocumentStatistics stats)
        {
            this.Text = "Đếm từ (Word Count)";
            this.Size = new Size(360, 310);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            Label lblHeading = new Label
            {
                Text = "Thống kê tài liệu:",
                Location = new Point(20, 16),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true
            };

            int startY = 48;
            int stepY = 25;

            string[,] rows = new string[,]
            {
                { "Trang (Pages):", stats.Pages.ToString("N0") },
                { "Từ (Words):", stats.Words.ToString("N0") },
                { "Ký tự (không dấu cách):", stats.CharactersNoSpaces.ToString("N0") },
                { "Ký tự (có dấu cách):", stats.CharactersWithSpaces.ToString("N0") },
                { "Đoạn văn (Paragraphs):", stats.Paragraphs.ToString("N0") },
                { "Dòng (Lines):", stats.Lines.ToString("N0") }
            };

            for (int i = 0; i < 6; i++)
            {
                Label lblName = new Label
                {
                    Text = rows[i, 0],
                    Location = new Point(24, startY + i * stepY),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(71, 85, 105)
                };
                Label lblVal = new Label
                {
                    Text = rows[i, 1],
                    Location = new Point(240, startY + i * stepY),
                    Width = 80,
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42)
                };
                this.Controls.Add(lblName);
                this.Controls.Add(lblVal);
            }

            CheckBox chkIncludeNotes = new CheckBox
            {
                Text = "Bao gồm cả hộp văn bản và chú thích",
                Location = new Point(24, startY + 6 * stepY + 6),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            Button btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(235, startY + 7 * stepY + 14),
                Width = 85,
                Height = 28,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White
            };

            this.Controls.AddRange(new Control[] { lblHeading, chkIncludeNotes, btnClose });
            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;
        }
    }
}

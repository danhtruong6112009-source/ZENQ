using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

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
            this.Text = AppL10n.T("Đếm từ (Word Count)", "Word Count");
            this.Size = new Size(360, 310);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            Label lblHeading = new Label
            {
                Text = AppL10n.T("Thống kê tài liệu:", "Statistics:"),
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
                { AppL10n.T("Từ (Words):", "Words:"), stats.Words.ToString("N0") },
                { AppL10n.T("Ký tự (không dấu cách):", "Characters (no spaces):"), stats.CharactersNoSpaces.ToString("N0") },
                { AppL10n.T("Ký tự (có dấu cách):", "Characters (with spaces):"), stats.CharactersWithSpaces.ToString("N0") },
                { AppL10n.T("Đoạn văn (Paragraphs):", "Paragraphs:"), stats.Paragraphs.ToString("N0") },
                { AppL10n.T("Dòng (Lines):", "Lines:"), stats.Lines.ToString("N0") }
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
                Text = AppL10n.T("Bao gồm cả hộp văn bản và chú thích", "Include textboxes, footnotes and endnotes"),
                Location = new Point(24, startY + 6 * stepY + 6),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            Button btnClose = new Button
            {
                Text = AppL10n.T("Đóng", "Close"),
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

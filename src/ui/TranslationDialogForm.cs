using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class TranslationDialogForm : Form
    {
        private TextBox txtSource;
        private TextBox txtTarget;
        private ComboBox cboTargetLang;
        private Button btnTranslate;
        private Button btnInsert;
        private Button btnClose;
        private WriterView _writerView;

        public TranslationDialogForm(string selectedText, WriterView writerView)
        {
            _writerView = writerView;
            InitializeComponent();

            if (!string.IsNullOrEmpty(selectedText))
            {
                txtSource.Text = selectedText.Trim();
                PerformTranslation();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Dịch thuật (Translator)";
            this.Size = new Size(460, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Header Panel
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 52;
            topPanel.BackColor = Color.FromArgb(16, 110, 190);
            topPanel.Paint += (s, e) =>
            {
                using (Font hf = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (Font sub = new Font("Segoe UI", 8.2f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("🌐 Dịch thuật tài liệu", hf, Brushes.White, 14, 8);
                    e.Graphics.DrawString("Chuyển ngữ văn bản đa ngôn ngữ trực tiếp trong ZENQ OFFICE", sub, Brushes.WhiteSmoke, 16, 30);
                }
            };
            this.Controls.Add(topPanel);

            // Source Text
            Label lblSource = new Label();
            lblSource.Text = "Văn bản gốc (Tự động nhận diện ngôn ngữ):";
            lblSource.Location = new Point(14, 62);
            lblSource.AutoSize = true;
            lblSource.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblSource.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblSource);

            txtSource = new TextBox();
            txtSource.Multiline = true;
            txtSource.ScrollBars = ScrollBars.Vertical;
            txtSource.Location = new Point(14, 84);
            txtSource.Size = new Size(416, 90);
            txtSource.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Controls.Add(txtSource);

            // Target Language selector
            Label lblTarget = new Label();
            lblTarget.Text = "Dịch sang ngôn ngữ:";
            lblTarget.Location = new Point(14, 184);
            lblTarget.AutoSize = true;
            lblTarget.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblTarget.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblTarget);

            cboTargetLang = new ComboBox();
            cboTargetLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTargetLang.Location = new Point(160, 182);
            cboTargetLang.Size = new Size(160, 26);
            cboTargetLang.Items.AddRange(new object[] {
                "Tiếng Việt",
                "English (US)",
                "Français (Pháp)",
                "Deutsch (Đức)",
                "日本語 (Nhật)",
                "中文 (Trung)"
            });
            cboTargetLang.SelectedIndex = 1; // Default English
            cboTargetLang.SelectedIndexChanged += (s, e) => PerformTranslation();
            this.Controls.Add(cboTargetLang);

            btnTranslate = new Button();
            btnTranslate.Text = "Dịch ngay";
            btnTranslate.Location = new Point(330, 180);
            btnTranslate.Size = new Size(100, 28);
            btnTranslate.BackColor = Color.FromArgb(16, 110, 190);
            btnTranslate.ForeColor = Color.White;
            btnTranslate.FlatStyle = FlatStyle.Flat;
            btnTranslate.FlatAppearance.BorderSize = 0;
            btnTranslate.Click += (s, e) => PerformTranslation();
            this.Controls.Add(btnTranslate);

            // Target Text
            Label lblResult = new Label();
            lblResult.Text = "Bản dịch kết quả:";
            lblResult.Location = new Point(14, 218);
            lblResult.AutoSize = true;
            lblResult.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblResult.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblResult);

            txtTarget = new TextBox();
            txtTarget.Multiline = true;
            txtTarget.ScrollBars = ScrollBars.Vertical;
            txtTarget.Location = new Point(14, 240);
            txtTarget.Size = new Size(416, 100);
            txtTarget.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Controls.Add(txtTarget);

            // Buttons
            btnInsert = new Button();
            btnInsert.Text = "Chèn vào tài liệu";
            btnInsert.Location = new Point(220, 356);
            btnInsert.Size = new Size(124, 32);
            btnInsert.BackColor = Color.FromArgb(16, 110, 190);
            btnInsert.ForeColor = Color.White;
            btnInsert.FlatStyle = FlatStyle.Flat;
            btnInsert.FlatAppearance.BorderSize = 0;
            btnInsert.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtTarget.Text) && _writerView != null)
                {
                    _writerView.InsertText(txtTarget.Text);
                }
                this.Close();
            };
            this.Controls.Add(btnInsert);

            btnClose = new Button();
            btnClose.Text = "Đóng";
            btnClose.Location = new Point(352, 356);
            btnClose.Size = new Size(78, 32);
            btnClose.BackColor = Color.FromArgb(235, 236, 238);
            btnClose.ForeColor = Color.FromArgb(50, 49, 48);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = Color.LightGray;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void PerformTranslation()
        {
            string src = txtSource.Text;
            if (string.IsNullOrEmpty(src))
            {
                txtTarget.Text = "";
                return;
            }

            int targetLang = cboTargetLang.SelectedIndex;
            // Target: 0=VI, 1=EN, 2=FR, 3=DE, 4=JA, 5=ZH
            txtTarget.Text = TranslateString(src, targetLang);
        }

        private string TranslateString(string src, int targetLang)
        {
            // Built-in offline phrases and dictionary
            Dictionary<string, string[]> dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "xin chào", new string[] { "xin chào", "Hello", "Bonjour", "Hallo", "こんにちは", "你好" } },
                { "báo cáo", new string[] { "báo cáo", "Report", "Rapport", "Bericht", "報告書", "报告" } },
                { "hợp đồng", new string[] { "hợp đồng", "Contract", "Contrat", "Vertrag", "契約", "合同" } },
                { "tài liệu", new string[] { "tài liệu", "Document", "Document", "Dokument", "書類", "文档" } },
                { "công ty", new string[] { "công ty", "Company", "Société", "Firma", "会社", "公司" } },
                { "tổng giám đốc", new string[] { "tổng giám đốc", "General Director", "Directeur Général", "Generaldirektor", "社長", "总经理" } },
                { "kế hoạch phát triển", new string[] { "kế hoạch phát triển", "Development Plan", "Plan de développement", "Entwicklungsplan", "開発計画", "发展规划" } },
                { "cảm ơn", new string[] { "cảm ơn", "Thank you", "Merci", "Danke", "ありがとうございます", "谢谢" } },
                { "quyết định", new string[] { "quyết định", "Decision", "Décision", "Entscheidung", "決定", "决定" } },
                { "biên bản cuộc họp", new string[] { "biên bản cuộc họp", "Meeting Minutes", "Procès-verbal de réunion", "Sitzungsprotokoll", "会議議事録", "会议纪要" } },

                // English source
                { "hello", new string[] { "Xin chào", "Hello", "Bonjour", "Hallo", "こんにちは", "你好" } },
                { "report", new string[] { "Báo cáo", "Report", "Rapport", "Bericht", "報告書", "报告" } },
                { "contract", new string[] { "Hợp đồng", "Contract", "Contrat", "Vertrag", "契約", "合同" } },
                { "document", new string[] { "Tài liệu", "Document", "Document", "Dokument", "書類", "文档" } },
                { "thank you", new string[] { "Cảm ơn bạn", "Thank you", "Merci", "Danke", "ありがとうございます", "谢谢" } }
            };

            string trimmed = src.Trim();
            if (dict.ContainsKey(trimmed))
            {
                string[] res = dict[trimmed];
                if (targetLang >= 0 && targetLang < res.Length) return res[targetLang];
            }

            // Word-by-word substitution fallback
            string[] words = src.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> translatedWords = new List<string>();
            foreach (var w in words)
            {
                string cleanW = w.Trim().ToLowerInvariant();
                if (dict.ContainsKey(cleanW))
                {
                    string[] r = dict[cleanW];
                    translatedWords.Add(r[Math.Min(r.Length - 1, targetLang)]);
                }
                else
                {
                    translatedWords.Add(w);
                }
            }

            return string.Join(" ", translatedWords.ToArray());
        }
    }
}

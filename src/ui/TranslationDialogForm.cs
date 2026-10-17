using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Dịch thuật Tài liệu (Document Translator) chuẩn Microsoft Office 365.
    /// Tích hợp dịch máy thông minh Neural AI trực tuyến qua Google Translate
    /// kèm bộ từ điển dự phòng ngoại tuyến mượt mà.
    /// </summary>
    public class TranslationDialogForm : Form
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private TextBox txtSource;
        private TextBox txtTarget;
        private ComboBox cboTargetLang;
        private Button btnTranslate;
        private Button btnSwap;
        private Button btnInsert;
        private Button btnClose;
        private Label lblStatus;
        private WriterView _writerView;

        private readonly string[] _langCodes = new string[] {
            "vi",    // 0: Tiếng Việt
            "en",    // 1: English (US)
            "fr",    // 2: Français (Pháp)
            "de",    // 3: Deutsch (Đức)
            "ja",    // 4: 日本語 (Nhật)
            "zh-CN", // 5: 中文 (Trung)
            "ko",    // 6: 한국어 (Hàn)
            "es",    // 7: Español (Tây Ban Nha)
            "ru"     // 8: Русский (Nga)
        };

        public TranslationDialogForm(string selectedText, WriterView writerView)
        {
            _writerView = writerView;
            InitializeComponent();

            if (!string.IsNullOrEmpty(selectedText))
            {
                txtSource.Text = selectedText.Trim();
                _ = PerformTranslationAsync();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Dịch thuật Đa ngôn ngữ (Translator - Office 365)";
            this.Size = new Size(520, 520);
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
            topPanel.Height = 56;
            topPanel.BackColor = Color.FromArgb(16, 110, 190);
            topPanel.Paint += (s, e) =>
            {
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE774', new Rectangle(14, 14, 24, 24), Color.White, 20f);
                using (Font hf = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (Font sub = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("Dịch thuật Trí tuệ Nhân tạo (Office 365 AI Translator)", hf, Brushes.White, 46, 8);
                    e.Graphics.DrawString("Chuyển ngữ đa chiều toàn vẹn ngữ cảnh văn bản cho Writer, Calc và Slides", sub, Brushes.WhiteSmoke, 46, 32);
                }
            };
            this.Controls.Add(topPanel);

            // Source Text Label
            Label lblSource = new Label();
            lblSource.Text = "Văn bản gốc (Tự động nhận diện ngôn ngữ nguồn):";
            lblSource.Location = new Point(14, 66);
            lblSource.AutoSize = true;
            lblSource.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblSource.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblSource);

            txtSource = new TextBox();
            txtSource.Multiline = true;
            txtSource.ScrollBars = ScrollBars.Vertical;
            txtSource.Location = new Point(14, 88);
            txtSource.Size = new Size(476, 100);
            txtSource.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Controls.Add(txtSource);

            // Target Language selector Bar
            Label lblTarget = new Label();
            lblTarget.Text = "Dịch sang:";
            lblTarget.Location = new Point(14, 200);
            lblTarget.AutoSize = true;
            lblTarget.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblTarget.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblTarget);

            cboTargetLang = new ComboBox();
            cboTargetLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTargetLang.Location = new Point(88, 196);
            cboTargetLang.Size = new Size(170, 26);
            cboTargetLang.Items.AddRange(new object[] {
                "Tiếng Việt (Vietnamese)",
                "English (Tiếng Anh - US)",
                "Français (Tiếng Pháp)",
                "Deutsch (Tiếng Đức)",
                "日本語 (Tiếng Nhật)",
                "中文 (Tiếng Trung)",
                "한국어 (Tiếng Hàn)",
                "Español (Tây Ban Nha)",
                "Русский (Tiếng Nga)"
            });
            cboTargetLang.SelectedIndex = 1; // Mặc định dịch sang English
            cboTargetLang.SelectedIndexChanged += (s, e) => _ = PerformTranslationAsync();
            this.Controls.Add(cboTargetLang);

            // Swap button
            btnSwap = new Button();
            btnSwap.Text = "Hoán đổi";
            btnSwap.Image = FluentIconRenderer.GetGlyphBitmap('\uE8AB', 14, 14, Color.FromArgb(50, 49, 48));
            btnSwap.ImageAlign = ContentAlignment.MiddleLeft;
            btnSwap.TextAlign = ContentAlignment.MiddleCenter;
            btnSwap.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnSwap.Padding = new Padding(4, 0, 4, 0);
            btnSwap.Location = new Point(266, 195);
            btnSwap.Size = new Size(95, 28);
            btnSwap.BackColor = Color.White;
            btnSwap.FlatStyle = FlatStyle.Flat;
            btnSwap.FlatAppearance.BorderColor = Color.LightGray;
            btnSwap.Click += (s, e) => SwapLanguagesAndText();
            this.Controls.Add(btnSwap);

            btnTranslate = new Button();
            btnTranslate.Text = "Dịch ngay";
            btnTranslate.Image = FluentIconRenderer.GetGlyphBitmap('\uE774', 14, 14, Color.White);
            btnTranslate.ImageAlign = ContentAlignment.MiddleLeft;
            btnTranslate.TextAlign = ContentAlignment.MiddleCenter;
            btnTranslate.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnTranslate.Padding = new Padding(6, 0, 6, 0);
            btnTranslate.Location = new Point(368, 195);
            btnTranslate.Size = new Size(122, 28);
            btnTranslate.BackColor = Color.FromArgb(16, 110, 190);
            btnTranslate.ForeColor = Color.White;
            btnTranslate.FlatStyle = FlatStyle.Flat;
            btnTranslate.FlatAppearance.BorderSize = 0;
            btnTranslate.Click += (s, e) => _ = PerformTranslationAsync();
            this.Controls.Add(btnTranslate);

            // Target Text Label
            Label lblResult = new Label();
            lblResult.Text = "Bản dịch kết quả (Neural AI Translation):";
            lblResult.Location = new Point(14, 234);
            lblResult.AutoSize = true;
            lblResult.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblResult.ForeColor = Color.FromArgb(50, 49, 48);
            this.Controls.Add(lblResult);

            txtTarget = new TextBox();
            txtTarget.Multiline = true;
            txtTarget.ScrollBars = ScrollBars.Vertical;
            txtTarget.Location = new Point(14, 256);
            txtTarget.Size = new Size(476, 120);
            txtTarget.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Controls.Add(txtTarget);

            // Status label
            lblStatus = new Label();
            lblStatus.Text = "Sẵn sàng dịch thuật.";
            lblStatus.Location = new Point(14, 384);
            lblStatus.Size = new Size(476, 22);
            lblStatus.ForeColor = Color.FromArgb(100, 100, 100);
            lblStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            this.Controls.Add(lblStatus);

            // Buttons
            btnInsert = new Button();
            btnInsert.Text = "Chèn vào tài liệu";
            btnInsert.Image = FluentIconRenderer.GetGlyphBitmap('\uE73E', 15, 15, Color.White);
            btnInsert.ImageAlign = ContentAlignment.MiddleLeft;
            btnInsert.TextAlign = ContentAlignment.MiddleCenter;
            btnInsert.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnInsert.Padding = new Padding(6, 0, 6, 0);
            btnInsert.Location = new Point(236, 426);
            btnInsert.Size = new Size(146, 34);
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
            btnClose.Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 13, 13, Color.FromArgb(50, 49, 48));
            btnClose.ImageAlign = ContentAlignment.MiddleLeft;
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnClose.Padding = new Padding(6, 0, 6, 0);
            btnClose.Location = new Point(392, 426);
            btnClose.Size = new Size(98, 34);
            btnClose.BackColor = Color.FromArgb(235, 236, 238);
            btnClose.ForeColor = Color.FromArgb(50, 49, 48);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = Color.LightGray;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void SwapLanguagesAndText()
        {
            string temp = txtSource.Text;
            txtSource.Text = txtTarget.Text;
            txtTarget.Text = temp;
            if (cboTargetLang.SelectedIndex == 0) cboTargetLang.SelectedIndex = 1;
            else if (cboTargetLang.SelectedIndex == 1) cboTargetLang.SelectedIndex = 0;
            _ = PerformTranslationAsync();
        }

        private async Task PerformTranslationAsync()
        {
            string src = txtSource.Text;
            if (string.IsNullOrWhiteSpace(src))
            {
                txtTarget.Text = "";
                lblStatus.Text = "Vui lòng nhập văn bản cần dịch.";
                lblStatus.ForeColor = Color.Gray;
                return;
            }

            int targetIdx = cboTargetLang.SelectedIndex;
            if (targetIdx < 0 || targetIdx >= _langCodes.Length) targetIdx = 1;
            string targetLangCode = _langCodes[targetIdx];

            lblStatus.Text = "Đang kết nối máy chủ dịch thuật AI trực tuyến...";
            lblStatus.ForeColor = Color.Navy;
            btnTranslate.Enabled = false;

            try
            {
                var (result, provider) = await TranslateOnlineAsync(src, targetLangCode);
                if (!string.IsNullOrEmpty(result))
                {
                    txtTarget.Text = result;
                    lblStatus.Text = string.Equals(provider, "MyMemory", StringComparison.OrdinalIgnoreCase)
                        ? "Đã dịch thành công qua MyMemory Neural Translation."
                        : "Đã dịch thành công qua Google Neural Machine Translation.";
                    lblStatus.ForeColor = Color.DarkGreen;
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[TranslationDialog] Online translation failed: " + ex.Message);
            }
            finally
            {
                btnTranslate.Enabled = true;
            }

            // Fallback to offline dictionary
            bool isConnected = false;
            try { isConnected = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable(); } catch { }

            if (!isConnected)
            {
                lblStatus.Text = "Không có kết nối internet, đang sử dụng từ điển dịch nhanh cục bộ.";
            }
            else
            {
                lblStatus.Text = "Máy chủ dịch thuật phản hồi chậm, đang sử dụng từ điển dịch nhanh cục bộ.";
            }
            lblStatus.ForeColor = Color.DarkGoldenrod;
            txtTarget.Text = TranslateStringOffline(src, targetIdx);
        }

        private async Task<(string Result, string Provider)> TranslateOnlineAsync(string text, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return ("", "None");

            // Chunk if text is very long (> 2500 characters) to avoid server payload/URL limits
            if (text.Length > 2500)
            {
                var chunks = SplitIntoParagraphChunks(text, 2000);
                var translatedChunks = new List<string>();
                string finalProvider = "Google";
                foreach (var chunk in chunks)
                {
                    var (tr, prov) = await TranslateSingleChunkAsync(chunk, targetLang);
                    translatedChunks.Add(!string.IsNullOrEmpty(tr) ? tr : chunk);
                    if (!string.IsNullOrEmpty(prov)) finalProvider = prov;
                }
                return (string.Join("\n\n", translatedChunks), finalProvider);
            }

            return await TranslateSingleChunkAsync(text, targetLang);
        }

        private List<string> SplitIntoParagraphChunks(string text, int maxChunkSize)
        {
            var result = new List<string>();
            string[] paras = text.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();

            foreach (var p in paras)
            {
                if (sb.Length > 0 && sb.Length + p.Length + 2 > maxChunkSize)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }

                if (p.Length > maxChunkSize)
                {
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    result.Add(p);
                }
                else
                {
                    if (sb.Length > 0) sb.Append("\n\n");
                    sb.Append(p);
                }
            }

            if (sb.Length > 0) result.Add(sb.ToString());
            return result;
        }

        private async Task<(string Result, string Provider)> TranslateSingleChunkAsync(string text, string targetLang)
        {
            // Tier 1: Google Translate API (clients5.google.com via POST form)
            try
            {
                string url = "https://clients5.google.com/translate_a/t?client=dict-chrome-ex";
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("sl", "auto"),
                    new KeyValuePair<string, string>("tl", targetLang),
                    new KeyValuePair<string, string>("q", text)
                });

                using (var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = formContent })
                {
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    var resp = await _httpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        string parsed = ParseGoogleChromeDictResponse(json);
                        if (!string.IsNullOrEmpty(parsed)) return (parsed, "Google");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[TranslationDialog] Tier 1 Google POST error: " + ex.Message);
            }

            // Tier 2: Google Translate API fallback (translate.googleapis.com / translate.google.com single POST)
            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=at&sl=auto&tl={targetLang}&dt=t";
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("q", text)
                });

                using (var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = formContent })
                {
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    var resp = await _httpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        string parsed = ParseGoogleSingleResponse(json);
                        if (!string.IsNullOrEmpty(parsed)) return (parsed, "Google");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[TranslationDialog] Tier 2 Google single error: " + ex.Message);
            }

            // Tier 3: MyMemory API fallback (free, neural machine translation)
            try
            {
                string enc = Uri.EscapeDataString(text.Length > 500 ? text.Substring(0, 500) : text);
                string url = $"https://api.mymemory.translated.net/get?q={enc}&langpair=autodetect|{targetLang}";

                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    var resp = await _httpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("responseData", out var respData) &&
                                respData.TryGetProperty("translatedText", out var trText))
                            {
                                string res = trText.GetString();
                                if (!string.IsNullOrEmpty(res)) return (res, "MyMemory");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[TranslationDialog] Tier 3 MyMemory error: " + ex.Message);
            }

            return (null, "None");
        }

        private static string ParseGoogleChromeDictResponse(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var first = root[0];
                        if (first.ValueKind == JsonValueKind.Array && first.GetArrayLength() > 0)
                        {
                            return first[0].GetString();
                        }
                        if (first.ValueKind == JsonValueKind.String)
                        {
                            return first.GetString();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static string ParseGoogleSingleResponse(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        JsonElement sentences = root[0];
                        if (sentences.ValueKind == JsonValueKind.Array)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (JsonElement sentence in sentences.EnumerateArray())
                            {
                                if (sentence.ValueKind == JsonValueKind.Array && sentence.GetArrayLength() > 0)
                                {
                                    string part = sentence[0].GetString();
                                    if (!string.IsNullOrEmpty(part)) sb.Append(part);
                                }
                            }
                            if (sb.Length > 0) return sb.ToString();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string TranslateStringOffline(string src, int targetLang)
        {
            Dictionary<string, string[]> dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "xin chào", new string[] { "xin chào", "Hello", "Bonjour", "Hallo", "こんにちは", "你好", "안녕하세요", "Hola", "Здравствуйте" } },
                { "báo cáo", new string[] { "báo cáo", "Report", "Rapport", "Bericht", "報告書", "报告", "보고서", "Informe", "Отчет" } },
                { "hợp đồng", new string[] { "hợp đồng", "Contract", "Contrat", "Vertrag", "契約", "合同", "계약", "Contrato", "Договор" } },
                { "tài liệu", new string[] { "tài liệu", "Document", "Document", "Dokument", "書類", "文档", "문서", "Documento", "Документ" } },
                { "công ty", new string[] { "công ty", "Company", "Société", "Firma", "会社", "公司", "회사", "Empresa", "Компания" } },
                { "tổng giám đốc", new string[] { "tổng giám đốc", "General Director", "Directeur Général", "Generaldirektor", "社長", "总经理", "대표이사", "Director General", "Генеральный директор" } },
                { "kế hoạch phát triển", new string[] { "kế hoạch phát triển", "Development Plan", "Plan de développement", "Entwicklungsplan", "開発計画", "发展规划", "개발 계획", "Plan de desarrollo", "План развития" } },
                { "cảm ơn", new string[] { "cảm ơn", "Thank you", "Merci", "Danke", "ありがとうございます", "谢谢", "감사합니다", "Gracias", "Спасибо" } },
                { "quyết định", new string[] { "quyết định", "Decision", "Décision", "Entscheidung", "決定", "决定", "결정", "Decisión", "Решение" } },
                { "biên bản cuộc họp", new string[] { "biên bản cuộc họp", "Meeting Minutes", "Procès-verbal de réunion", "Sitzungsprotokoll", "会議議事録", "会议纪要", "회의록", "Acta de reunión", "Протокол собрания" } },

                // English source
                { "hello", new string[] { "Xin chào", "Hello", "Bonjour", "Hallo", "こんにちは", "你好", "안녕하세요", "Hola", "Здравствуйте" } },
                { "report", new string[] { "Báo cáo", "Report", "Rapport", "Bericht", "報告書", "报告", "보고서", "Informe", "Отчет" } },
                { "contract", new string[] { "Hợp đồng", "Contract", "Contrat", "Vertrag", "契約", "合同", "계약", "Contrato", "Договор" } },
                { "document", new string[] { "Tài liệu", "Document", "Document", "Dokument", "書類", "文档", "문서", "Documento", "Документ" } },
                { "thank you", new string[] { "Cảm ơn bạn", "Thank you", "Merci", "Danke", "ありがとうございます", "谢谢", "감사합니다", "Gracias", "Спасибо" } }
            };

            string trimmed = src.Trim();
            if (dict.ContainsKey(trimmed))
            {
                string[] res = dict[trimmed];
                if (targetLang >= 0 && targetLang < res.Length) return res[targetLang];
            }

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

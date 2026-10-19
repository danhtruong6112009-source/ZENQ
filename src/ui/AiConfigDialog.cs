using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Cửa sổ Cấu hình AI Copilot Đa nền tảng cho ZenQOffice.
    /// Cho phép tùy chỉnh Nhà cung cấp (Google AI Studio, Groq, OpenAI, DeepSeek, Ollama, Custom),
    /// Tên Model, Chuỗi API Key, Endpoint, và Kiểm tra kết nối trực tiếp.
    /// </summary>
    public class AiConfigDialog : Form
    {
        private ComboBox cboProvider;
        private ComboBox cboModel;
        private TextBox txtApiKey;
        private Button btnToggleKey;
        private TextBox txtEndpoint;
        private TextBox txtSystemPrompt;
        private TrackBar trkTemp;
        private Label lblTempValue;
        private Label lblStatus;
        private Button btnTest;
        private Button btnSave;
        private Button btnClose;

        public AiConfig Config { get; private set; }

        public AiConfigDialog()
        {
            Config = AiConfig.Load();
            InitializeComponent();
            LoadConfigToUi();
        }

        private void InitializeComponent()
        {
            this.Text = "Cấu hình & Thiết lập Trợ lý AI - ZENQ AI";
            this.Size = new Size(680, 680);
            this.MinimumSize = new Size(680, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24)
            };
            this.Controls.Add(pnlMain);

            int curY = 16;

            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Location = new Point(20, curY),
                Size = new Size(620, 70),
                BackColor = Color.White
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                using (GraphicsPath gp = CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, pnlHeader.Width - 1f, pnlHeader.Height - 1f), 12f))
                using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawPath(bPen, gp);
                }
                using (SolidBrush barB = new SolidBrush(Color.FromArgb(99, 102, 241)))
                {
                    e.Graphics.FillRectangle(barB, 0, 0, 6, pnlHeader.Height);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE781', new RectangleF(16, 12, 22, 22), Color.FromArgb(99, 102, 241), 18f);
            };

            Label lblTitle = new Label
            {
                Text = "Thiết lập Trí tuệ Nhân tạo - ZENQ AI Engine",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(44, 12),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "Hỗ trợ Google AI Studio (Gemini 3.7/3.6/3.5/2.5), Groq Cloud, OpenAI, DeepSeek, Ollama & Custom API",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(44, 38),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSub);

            pnlMain.Controls.Add(pnlHeader);
            curY += 85;

            // 2. Settings Group Card
            Panel pnlCard = new Panel
            {
                Location = new Point(20, curY),
                Size = new Size(620, 410),
                BackColor = Color.White
            };
            pnlCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                using (GraphicsPath gp = CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, pnlCard.Width - 1f, pnlCard.Height - 1f), 12f))
                using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawPath(bPen, gp);
                }
            };

            int formY = 16;

            // Field: Provider
            Label lblProv = new Label { Text = "Nhà cung cấp AI (Provider):", Location = new Point(20, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold) };
            pnlCard.Controls.Add(lblProv);
            formY += 24;

            cboProvider = new ComboBox
            {
                Location = new Point(20, formY),
                Size = new Size(580, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f)
            };
            foreach (AiProviderType p in Enum.GetValues(typeof(AiProviderType)))
            {
                cboProvider.Items.Add(AiConfig.GetProviderDisplayName(p));
            }
            cboProvider.SelectedIndexChanged += CboProvider_SelectedIndexChanged;
            pnlCard.Controls.Add(cboProvider);
            formY += 42;

            // Field: Model Name
            Label lblModel = new Label { Text = "Tên Model (Model Name):", Location = new Point(20, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold) };
            pnlCard.Controls.Add(lblModel);
            formY += 24;

            cboModel = new ComboBox
            {
                Location = new Point(20, formY),
                Size = new Size(580, 30),
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10f)
            };
            pnlCard.Controls.Add(cboModel);
            formY += 42;

            // Field: API Key
            Label lblKey = new Label { Text = "Chuỗi API Key (Mật mã API):", Location = new Point(20, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold) };
            pnlCard.Controls.Add(lblKey);
            formY += 24;

            txtApiKey = new TextBox
            {
                Location = new Point(20, formY),
                Size = new Size(520, 28),
                Font = new Font("Segoe UI", 10f),
                UseSystemPasswordChar = true
            };
            pnlCard.Controls.Add(txtApiKey);

            btnToggleKey = new Button
            {
                Location = new Point(545, formY - 1),
                Size = new Size(55, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE7B3', 16, 16, Color.FromArgb(71, 85, 105)),
                Cursor = Cursors.Hand
            };
            btnToggleKey.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnToggleKey.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggleKey.Image = FluentIconRenderer.GetGlyphBitmap(
                    txtApiKey.UseSystemPasswordChar ? '\uE7B3' : '\uE890', 16, 16, Color.FromArgb(71, 85, 105));
            };
            pnlCard.Controls.Add(btnToggleKey);
            formY += 42;

            // Field: Custom Endpoint
            Label lblEp = new Label { Text = "Đường dẫn Endpoint URL (Tùy chọn / Mặc định):", Location = new Point(20, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold) };
            pnlCard.Controls.Add(lblEp);
            formY += 24;

            txtEndpoint = new TextBox
            {
                Location = new Point(20, formY),
                Size = new Size(580, 28),
                Font = new Font("Segoe UI", 9.5f)
            };
            pnlCard.Controls.Add(txtEndpoint);
            formY += 42;

            // Field: Temperature
            Label lblTemp = new Label { Text = "Độ sáng tạo (Temperature):", Location = new Point(20, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold) };
            pnlCard.Controls.Add(lblTemp);

            lblTempValue = new Label { Text = "0.7", Location = new Point(560, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold), ForeColor = Color.FromArgb(99, 102, 241) };
            pnlCard.Controls.Add(lblTempValue);
            formY += 24;

            trkTemp = new TrackBar
            {
                Location = new Point(15, formY),
                Size = new Size(590, 45),
                Minimum = 0,
                Maximum = 15,
                Value = 7,
                TickFrequency = 1,
                BackColor = Color.White
            };
            trkTemp.ValueChanged += (s, e) =>
            {
                float val = trkTemp.Value / 10.0f;
                lblTempValue.Text = val.ToString("0.0");
            };
            pnlCard.Controls.Add(trkTemp);
            formY += 44;

            // Field: System Prompt
            Label lblSys = new Label { Text = "Chỉ dẫn hệ thống (System Prompt):", Location = new Point(20, formY), AutoSize = true, Font = new Font("Segoe UI", 9.2f, FontStyle.Bold) };
            pnlCard.Controls.Add(lblSys);
            formY += 24;

            txtSystemPrompt = new TextBox
            {
                Location = new Point(20, formY),
                Size = new Size(580, 50),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9f)
            };
            pnlCard.Controls.Add(txtSystemPrompt);

            pnlMain.Controls.Add(pnlCard);
            curY += 425;

            // 3. Status Bar
            lblStatus = new Label
            {
                Text = "Trạng thái: Chưa kiểm tra kết nối",
                Location = new Point(22, curY),
                Size = new Size(618, 30),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlMain.Controls.Add(lblStatus);
            curY += 36;

            // 4. Action Buttons Footer
            Panel pnlButtons = new Panel
            {
                Location = new Point(20, curY),
                Size = new Size(620, 45)
            };

            btnTest = new Button
            {
                Text = "Kiểm tra kết nối",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE945', 16, 16, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(0, 0),
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnTest.Click += BtnTest_Click;
            pnlButtons.Controls.Add(btnTest);

            btnSave = new Button
            {
                Text = "Lưu & Kích hoạt",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE74E', 16, 16, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(340, 0),
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            pnlButtons.Controls.Add(btnSave);

            btnClose = new Button
            {
                Text = "Đóng",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 14, 14, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(510, 0),
                Size = new Size(110, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlButtons.Controls.Add(btnClose);

            pnlMain.Controls.Add(pnlButtons);
        }

        private void LoadConfigToUi()
        {
            if (Config == null) Config = AiConfig.Load();

            var activeProf = Config.GetActiveProfile();
            if (activeProf != null)
            {
                int pIdx = (int)activeProf.Provider;
                if (pIdx >= 0 && pIdx < cboProvider.Items.Count)
                {
                    cboProvider.SelectedIndex = pIdx;
                }
                else
                {
                    cboProvider.SelectedIndex = 0;
                }

                UpdateModelList();
                string modelToSelect = activeProf.SelectedModel ?? "";
                if (activeProf.Provider == AiProviderType.GoogleAIStudio && modelToSelect == "gemini-3.1-flash")
                {
                    modelToSelect = "gemini-3.1-flash-lite";
                }
                if (!string.IsNullOrEmpty(modelToSelect))
                {
                    if (!cboModel.Items.Contains(modelToSelect))
                    {
                        cboModel.Items.Insert(0, modelToSelect);
                    }
                    cboModel.Text = modelToSelect;
                }
                txtApiKey.Text = activeProf.ApiKey ?? "";
                txtEndpoint.Text = activeProf.CustomEndpoint ?? "";
                trkTemp.Value = Math.Max(0, Math.Min(15, (int)(activeProf.Temperature * 10)));
                lblTempValue.Text = (trkTemp.Value / 10.0f).ToString("0.0");
                txtSystemPrompt.Text = Config.SystemInstructions ?? "";
            }
            else
            {
                int pIdx = (int)Config.Provider;
                if (pIdx >= 0 && pIdx < cboProvider.Items.Count)
                {
                    cboProvider.SelectedIndex = pIdx;
                }
                else
                {
                    cboProvider.SelectedIndex = 0;
                }

                UpdateModelList();
                string modelToSelect = Config.ModelName ?? "";
                if (Config.Provider == AiProviderType.GoogleAIStudio && modelToSelect == "gemini-3.1-flash")
                {
                    modelToSelect = "gemini-3.1-flash-lite";
                }
                cboModel.Text = modelToSelect;
                txtApiKey.Text = Config.ApiKey ?? "";
                txtEndpoint.Text = Config.CustomEndpoint ?? "";
                trkTemp.Value = Math.Max(0, Math.Min(15, (int)(Config.Temperature * 10)));
                lblTempValue.Text = (trkTemp.Value / 10.0f).ToString("0.0");
                txtSystemPrompt.Text = Config.SystemInstructions ?? "";
            }
        }

        private void UpdateModelList()
        {
            int pIdx = cboProvider.SelectedIndex;
            if (pIdx < 0) return;
            AiProviderType prov = (AiProviderType)pIdx;

            string curText = cboModel.Text;
            cboModel.Items.Clear();
            List<string> models = AiConfig.GetRecommendedModels(prov);
            foreach (var m in models)
            {
                cboModel.Items.Add(m);
            }

            if (!string.IsNullOrEmpty(curText) && models.Contains(curText))
            {
                cboModel.Text = curText;
            }
            else if (models.Count > 0)
            {
                cboModel.SelectedIndex = 0;
            }
        }

        private void CboProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            int pIdx = cboProvider.SelectedIndex;
            if (pIdx < 0) return;
            AiProviderType prov = (AiProviderType)pIdx;

            UpdateModelList();
            if (prov == AiProviderType.GoogleAIStudio)
            {
                txtEndpoint.Text = "https://generativelanguage.googleapis.com/v1beta";
            }
            else if (prov == AiProviderType.Groq)
            {
                txtEndpoint.Text = "https://api.groq.com/openai/v1";
            }
            else if (prov == AiProviderType.OpenAI)
            {
                txtEndpoint.Text = "https://api.openai.com/v1";
            }
            else if (prov == AiProviderType.DeepSeek)
            {
                txtEndpoint.Text = "https://api.deepseek.com/v1";
            }
            else if (prov == AiProviderType.Ollama)
            {
                txtEndpoint.Text = "http://localhost:11434/v1";
            }
            else if (prov == AiProviderType.OpenRouter)
            {
                txtEndpoint.Text = "https://openrouter.ai/api/v1";
            }
        }

        private void ReadUiToConfig()
        {
            AiProviderType prov = (AiProviderType)Math.Max(0, cboProvider.SelectedIndex);
            string model = cboModel.Text.Trim();
            if (prov == AiProviderType.GoogleAIStudio && model == "gemini-3.1-flash")
            {
                model = "gemini-3.1-flash-lite";
            }
            string apiKey = txtApiKey.Text.Trim();
            string endpoint = txtEndpoint.Text.Trim();
            float temp = trkTemp.Value / 10.0f;
            string sysPrompt = txtSystemPrompt.Text.Trim();

            Config.Provider = prov;
            Config.ModelName = model;
            Config.ApiKey = apiKey;
            Config.CustomEndpoint = endpoint;
            Config.Temperature = temp;
            Config.SystemInstructions = sysPrompt;

            var activeProf = Config.GetActiveProfile();
            if (activeProf == null && !string.IsNullOrWhiteSpace(apiKey))
            {
                activeProf = new AiKeyProfile(AiConfig.GetSimpleProviderName(prov), prov, apiKey, model);
                Config.Profiles.Add(activeProf);
                Config.ActiveProfileId = activeProf.Id;
            }

            if (activeProf != null)
            {
                activeProf.Provider = prov;
                activeProf.ApiKey = apiKey;
                activeProf.SelectedModel = model;
                activeProf.CustomEndpoint = endpoint;
                activeProf.Temperature = temp;

                if (activeProf.SavedModels == null) activeProf.SavedModels = new List<string>();
                if (!string.IsNullOrEmpty(model) && !activeProf.SavedModels.Contains(model))
                {
                    activeProf.SavedModels.Insert(0, model);
                }
                // Sanitize obsolete models
                activeProf.SavedModels.RemoveAll(m => m == "gemini-3.1-flash" || (prov == AiProviderType.GoogleAIStudio && (m.StartsWith("gemini-1.") || m.StartsWith("gemini-2.") || m == "gemini-2.5-flash" || m == "gemini-2.5-pro")));
                if (activeProf.SavedModels.Count == 0 && !string.IsNullOrEmpty(model))
                {
                    activeProf.SavedModels.Add(model);
                }
            }
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            ReadUiToConfig();
            lblStatus.Text = "Đang gửi yêu cầu kiểm tra kết nối tới " + Config.ModelName + "...";
            lblStatus.ForeColor = Color.FromArgb(202, 138, 4);
            btnTest.Enabled = false;

            AiService.SendPromptAsync(Config, "System check", "Xin chào! Trả lời 'OK' nếu kết nối hoạt động.", "", resp =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    btnTest.Enabled = true;
                    if (resp.Success)
                    {
                        lblStatus.Text = "Kết nối thành công! Phản hồi sau " + resp.DurationMs + " ms từ " + resp.ModelUsed;
                        lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                    }
                    else
                    {
                        lblStatus.Text = "Kết nối thất bại: " + resp.ErrorMessage;
                        lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    }
                }));
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ReadUiToConfig();
            Config.Save();
            if (AppForm.Instance != null)
            {
                AppForm.Instance.RefreshAiChatProfileSelectors();
                if (AppForm.Instance.AiSidebar != null)
                {
                    AppForm.Instance.AiSidebar.ReloadProfilesAndModels();
                }
            }
            MessageBox.Show("Đã lưu thiết lập Trợ lý AI thành công!\nBạn có thể sử dụng ngay trên tất cả các thẻ làm việc.", "ZENQ AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

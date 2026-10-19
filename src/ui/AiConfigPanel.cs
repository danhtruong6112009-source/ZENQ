using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bảng điều khiển cấu hình AI Liquid Glass căn chỉnh Pixel-Perfect trên màn hình chính (Tab Thiết lập AI).
    /// 
    /// HỖ TRỢ 2 CHẾ ĐỘ THIẾT LẬP:
    /// 1. ⚡ ĐƠN GIẢN (Simple Mode):
    ///    - Chỉ 1 ô nhập API Key duy nhất.
    ///    - Hệ thống tự động phân tích chuỗi Key để nhận diện Nhà cung cấp (Google Gemini, Groq, OpenAI, DeepSeek, OpenRouter, Ollama).
    ///    - Tự động nạp toàn bộ danh sách Model tối ưu dạng Chips để chọn 1 chạm + hỗ trợ tự gõ model tùy ý.
    /// 2. ⚙️ NÂNG CAO (Advanced Mode):
    ///    - Quản lý đa Profile (Bộ lưu Key), chọn Provider thủ công, chỉnh Độ sáng tạo (Temperature), Chỉ dẫn hệ thống (System Prompt).
    /// </summary>
    public class AiConfigPanel : UserControl
    {
        private AppForm _appForm;
        private AiConfig _config;
        private AiKeyProfile _currentProfile; // null nếu chưa có profile nào
        private bool _isBinding = true;

        // Chế độ Thiết lập: true = ⚡ Đơn giản, false = ⚙️ Nâng cao
        private bool _isSimpleMode = true;
        private RectangleF _modeSwitcherRect;
        private RectangleF _simpleTabRect;
        private RectangleF _advancedTabRect;

        // Profile Management Bar (Chế độ Nâng cao)
        private GlassComboInput cboProfiles;
        private GlassVectorButton btnAddProfile;
        private GlassVectorButton btnDeleteProfile;

        // Details Form
        private GlassTextBox txtKeyName;
        private GlassComboInput cboProvider;
        private GlassComboInput cboModel;
        private GlassTextBox txtApiKey;
        private GlassVectorButton btnToggleKey;
        private GlassTextBox txtSystemPrompt;
        private GlassSlider trkTemp;
        private GlassVectorButton btnTest;
        private GlassVectorButton btnSave;

        // Chip paint data
        private List<Rectangle> _chipRects = new List<Rectangle>();
        private List<Rectangle> _chipXRects = new List<Rectangle>();

        private string _statusText = "";
        private Color _statusColor = Color.FromArgb(71, 85, 105);

        public AiConfigPanel(AppForm appForm)
        {
            _appForm = appForm;
            _config = AiConfig.Load();
            _currentProfile = _config.GetActiveProfile(); // có thể null

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.Font = new Font("Segoe UI", 9.5f);

            InitializeComponent();
            ReloadProfilesToUi();

            // Nếu đã có profile với key, kích hoạt nhận diện model ngay
            if (_currentProfile != null && !string.IsNullOrEmpty(_currentProfile.ApiKey))
            {
                AiProviderType detected = DetectProviderFromApiKey(_currentProfile.ApiKey);
                if ((int)_currentProfile.Provider >= 0) detected = _currentProfile.Provider;
                cboProvider.SelectedIndex = (int)detected;
            }
        }

        private void InitializeComponent()
        {
            this.AutoScroll = false;
            this.Padding = new Padding(28, 16, 28, 24);

            // ==========================================
            // HÀNG 1: CHỌN KEY (Trái) & TEMPERATURE (Phải)
            // ==========================================
            cboProfiles = new GlassComboInput
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                PlaceholderText = "Chọn bộ lưu API Key..."
            };
            cboProfiles.SelectedIndexChanged += (s, e) =>
            {
                if (_isBinding) return;
                if (cboProfiles.SelectedIndex >= 0 && cboProfiles.SelectedIndex < _config.Profiles.Count)
                {
                    _config.ActiveProfileId = _config.Profiles[cboProfiles.SelectedIndex].Id;
                    _currentProfile = _config.GetActiveProfile();
                    BindProfileToUi(_currentProfile);
                    if (_appForm != null) _appForm.RefreshAiChatProfileSelectors();
                }
            };
            this.Controls.Add(cboProfiles);

            btnAddProfile = new GlassVectorButton
            {
                Text = "Thêm Key",
                IsPrimary = false,
                FluentIcon = "/add"
            };
            btnAddProfile.Click += (s, e) => AddNewKeyProfile();
            this.Controls.Add(btnAddProfile);

            btnDeleteProfile = new GlassVectorButton
            {
                Text = "",
                IsPrimary = false,
                FluentIcon = "/trash"
            };
            btnDeleteProfile.Click += (s, e) => DeleteCurrentKeyProfile();
            this.Controls.Add(btnDeleteProfile);

            trkTemp = new GlassSlider
            {
                Value = 0.7f
            };
            trkTemp.ValueChanged += (s, e) =>
            {
                Invalidate();
            };
            this.Controls.Add(trkTemp);

            // ==========================================
            // HÀNG 2: TÊN BỘ LƯU (Trái) & SYSTEM PROMPT (Phải)
            // ==========================================
            txtKeyName = new GlassTextBox
            {
                PlaceholderText = "Tên bộ lưu (ví dụ: Google Gemini, OpenAI GPT-4o...)"
            };
            this.Controls.Add(txtKeyName);

            txtSystemPrompt = new GlassTextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Nhập hướng dẫn vai trò, ngữ cảnh và phong cách phản hồi cho AI..."
            };
            this.Controls.Add(txtSystemPrompt);

            // ==========================================
            // HÀNG 3: NHÀ CUNG CẤP AI
            // ==========================================
            cboProvider = new GlassComboInput
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                PlaceholderText = "Chọn nhà cung cấp AI..."
            };
            foreach (AiProviderType p in Enum.GetValues(typeof(AiProviderType)))
            {
                if ((int)p >= 0)
                {
                    cboProvider.Items.Add(AiConfig.GetProviderDisplayName(p));
                }
            }
            cboProvider.SelectedIndexChanged += (s, e) =>
            {
                if (_isBinding) return;
                OnProviderChanged();
            };
            this.Controls.Add(cboProvider);

            // ==========================================
            // HÀNG 4: MÔ HÌNH AI (Trái)
            // ==========================================
            cboModel = new GlassComboInput
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                PlaceholderText = "Chọn hoặc gõ tên model rồi nhấn Enter..."
            };
            cboModel.SelectedIndexChanged += (s, e) =>
            {
                if (_isBinding) return;
                if (cboModel.SelectedIndex >= 0)
                {
                    string picked = cboModel.Items[cboModel.SelectedIndex].ToString();
                    if (!string.IsNullOrEmpty(picked) && !picked.StartsWith("⏳") && !picked.StartsWith("⚠️"))
                    {
                        EnsureProfileExistsForSimpleMode();
                        if (_currentProfile != null)
                        {
                            _currentProfile.AddModel(picked);
                            _currentProfile.SelectedModel = picked;
                        }
                        _isBinding = true;
                        cboModel.SelectedIndex = -1;
                        cboModel.Text = "";
                        _isBinding = false;
                        Invalidate();
                    }
                }
            };
            cboModel.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
                {
                    string typed = cboModel.Text != null ? cboModel.Text.Trim() : "";
                    if (!string.IsNullOrEmpty(typed) && !typed.StartsWith("⏳") && !typed.StartsWith("⚠️"))
                    {
                        EnsureProfileExistsForSimpleMode();
                        if (_currentProfile != null)
                        {
                            _currentProfile.AddModel(typed);
                            _currentProfile.SelectedModel = typed;
                        }
                        _isBinding = true;
                        cboModel.SelectedIndex = -1;
                        cboModel.Text = "";
                        _isBinding = false;
                        Invalidate();
                    }
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            };
            this.Controls.Add(cboModel);

            // ==========================================
            // HÀNG 5: CHUỖI API KEY (Trái) & STATUS (Phải)
            // ==========================================
            txtApiKey = new GlassTextBox
            {
                UseSystemPasswordChar = true,
                PlaceholderText = "Dán chuỗi API Key của bạn vào đây..."
            };
            txtApiKey.TextChanged += (s, e) =>
            {
                if (_isBinding) return;
                if (_isSimpleMode)
                {
                    EnsureProfileExistsForSimpleMode();
                    AiProviderType detected = DetectProviderFromApiKey(txtApiKey.Text);
                    if (cboProvider.SelectedIndex != (int)detected)
                    {
                        cboProvider.SelectedIndex = (int)detected;
                    }
                    if (_currentProfile != null)
                    {
                        _currentProfile.ApiKey = txtApiKey.Text.Trim();
                        _currentProfile.Provider = detected;
                    }
                    Invalidate();
                }
            };
            this.Controls.Add(txtApiKey);

            btnToggleKey = new GlassVectorButton
            {
                Text = "",
                IsPrimary = false,
                FluentIcon = txtApiKey.UseSystemPasswordChar ? "/hide" : "/eye"
            };
            btnToggleKey.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggleKey.FluentIcon = txtApiKey.UseSystemPasswordChar ? "/hide" : "/eye";
                btnToggleKey.Invalidate();
            };
            this.Controls.Add(btnToggleKey);

            // ==========================================
            // HÀNG NÚT BẤM DƯỚI CÙNG (PIXEL-PERFECT ALIGNED)
            // ==========================================
            btnTest = new GlassVectorButton
            {
                Text = "Kiểm tra kết nối",
                IsPrimary = false,
                FluentIcon = "/test"
            };
            btnTest.Click += (s, e) => RunConnectionTest();
            this.Controls.Add(btnTest);

            btnSave = new GlassVectorButton
            {
                Text = "Lưu cấu hình",
                IsPrimary = true,
                FluentIcon = "/save"
            };
            btnSave.Click += (s, e) => SaveConfig();
            this.Controls.Add(btnSave);

            RelayoutControls();
        }

        public static AiProviderType DetectProviderFromApiKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return AiProviderType.GoogleAIStudio;
            string k = key.Trim();

            if (k.StartsWith("gsk_", StringComparison.OrdinalIgnoreCase))
                return AiProviderType.Groq;

            if (k.StartsWith("sk-or-v1-", StringComparison.OrdinalIgnoreCase))
                return AiProviderType.OpenRouter;

            if (k.StartsWith("AIzaSy", StringComparison.OrdinalIgnoreCase) || k.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase))
                return AiProviderType.GoogleAIStudio;

            if (k.StartsWith("ds-", StringComparison.OrdinalIgnoreCase))
                return AiProviderType.DeepSeek;

            if (k.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || k.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || k.Contains("11434"))
                return AiProviderType.Ollama;

            if (k.StartsWith("sk-proj-", StringComparison.OrdinalIgnoreCase) || k.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
                return AiProviderType.OpenAI;

            if (k.Length >= 35 && k.Length <= 45 && !k.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
                return AiProviderType.GoogleAIStudio;

            return AiProviderType.GoogleAIStudio;
        }

        private void EnsureProfileExistsForSimpleMode()
        {
            if (_currentProfile == null)
            {
                if (_config.Profiles.Count > 0)
                {
                    _currentProfile = _config.Profiles[0];
                    _config.ActiveProfileId = _currentProfile.Id;
                }
                else
                {
                    _currentProfile = _config.CreateDraftProfile();
                    _currentProfile.Name = "AI Key Mặc định";
                    _config.ActiveProfileId = _currentProfile.Id;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int padLeft = 28;
            int padTop = 26;
            int colGap = 36;
            int totalW = this.ClientSize.Width - padLeft * 2;
            int colW = Math.Max(320, (totalW - colGap) / 2);
            int leftColX = padLeft;
            int rightColX = leftColX + colW + colGap;

            using (Font headerTitleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (Font headerSubFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Font titleFont = new Font("Segoe UI", 9.8f, FontStyle.Bold))
            using (Font valueFont = new Font("Segoe UI", 9.8f, FontStyle.Bold))
            using (Font statusFont = new Font("Segoe UI", 9.2f, FontStyle.Italic))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush labelBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush valueBrush = new SolidBrush(Color.FromArgb(79, 70, 229)))
            using (SolidBrush statusBrush = new SolidBrush(_statusColor))
            {
                // ==========================================
                // 1. THANH TIÊU ĐỀ & CÂY TRƯỢT CHUYỂN CHẾ ĐỘ
                // ==========================================
                string modeTitle = _isSimpleMode ? "Thiết lập AI Nhanh" : "Thiết lập AI Nâng cao";
                string modeSub = _isSimpleMode ? "Chỉ cần dán API Key, hệ thống tự động phát hiện Nhà cung cấp và nạp toàn bộ danh sách Model."
                                               : "Quản lý nhiều bộ lưu API Key, tùy chỉnh độ sáng tạo và chỉ dẫn hệ thống.";

                // Huy hiệu Thử nghiệm nằm giữa đường viền khung kính và nền bên phải (Y = 14 là mép trên khung kính)
                HomeView.DrawFloatingCardBorderBadge(g, this.ClientSize.Width - padLeft - 118f, 14f);

                g.DrawString(modeTitle, headerTitleFont, textBrush, leftColX, padTop + 2);
                g.DrawString(modeSub, headerSubFont, subBrush, leftColX, padTop + 24);

                // Vẽ Cây trượt Switcher (Segmented Pill Slider)
                if (_modeSwitcherRect.Width > 0 && _modeSwitcherRect.Height > 0)
                {
                    using (GraphicsPath trackPath = RoundRect(_modeSwitcherRect, 16f))
                    using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(235, 240, 248)))
                    using (Pen trackPen = new Pen(Color.FromArgb(210, 220, 235), 1.2f))
                    {
                        g.FillPath(trackBrush, trackPath);
                        g.DrawPath(trackPen, trackPath);
                    }

                    RectangleF activeIndRect = _isSimpleMode ? _simpleTabRect : _advancedTabRect;
                    RectangleF shadowRect = new RectangleF(activeIndRect.X, activeIndRect.Y + 1.5f, activeIndRect.Width, activeIndRect.Height);
                    using (GraphicsPath sp = RoundRect(shadowRect, 14f))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(25, 0, 0, 0)))
                    {
                        g.FillPath(sb, sp);
                    }

                    using (GraphicsPath indPath = RoundRect(activeIndRect, 14f))
                    using (LinearGradientBrush indBrush = new LinearGradientBrush(activeIndRect, Color.FromArgb(255, 255, 255), Color.FromArgb(235, 245, 255), 90f))
                    using (Pen indPen = new Pen(Color.FromArgb(245, 255, 255), 1.4f))
                    {
                        g.FillPath(indBrush, indPath);
                        g.DrawPath(indPen, indPath);
                    }

                    // Nhãn chữ trên 2 tab
                    using (Font tabBoldFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                    using (Font tabRegFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                    {
                        using (SolidBrush actBrush = new SolidBrush(Color.FromArgb(67, 56, 202)))
                        using (SolidBrush inactBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                        {
                            // Tab Đơn giản
                            string txtSimple = "Đơn giản";
                            SizeF szS = g.MeasureString(txtSimple, _isSimpleMode ? tabBoldFont : tabRegFont);
                            g.DrawString(txtSimple, _isSimpleMode ? tabBoldFont : tabRegFont, _isSimpleMode ? actBrush : inactBrush,
                                         _simpleTabRect.X + (_simpleTabRect.Width - szS.Width) / 2f,
                                         _simpleTabRect.Y + (_simpleTabRect.Height - szS.Height) / 2f);

                            // Tab Nâng cao
                            string txtAdv = "Nâng cao";
                            SizeF szA = g.MeasureString(txtAdv, !_isSimpleMode ? tabBoldFont : tabRegFont);
                            g.DrawString(txtAdv, !_isSimpleMode ? tabBoldFont : tabRegFont, !_isSimpleMode ? actBrush : inactBrush,
                                         _advancedTabRect.X + (_advancedTabRect.Width - szA.Width) / 2f,
                                         _advancedTabRect.Y + (_advancedTabRect.Height - szA.Height) / 2f);
                        }
                    }
                }

                // ==========================================
                // 2. NỘI DUNG THEO TỪNG CHẾ ĐỘ
                // ==========================================
                if (_isSimpleMode)
                {
                    int sRow1Y = padTop + 54;
                    int ctlH = 36;

                    // Cột Trái: Ô nhập API Key
                    g.DrawString("Chuỗi API Key:", titleFont, textBrush, leftColX, sRow1Y);

                    // Huy hiệu Nhận diện Nhà cung cấp (Smart Detection Badge)
                    AiProviderType currentProv = (cboProvider.SelectedIndex >= 0) ? (AiProviderType)cboProvider.SelectedIndex : DetectProviderFromApiKey(txtApiKey.Text);
                    string keyStr = txtApiKey.Text != null ? txtApiKey.Text.Trim() : "";

                    RectangleF badgeRect = new RectangleF(leftColX, sRow1Y + 26 + ctlH + 12, colW, 40);
                    using (GraphicsPath bp = RoundRect(badgeRect, 14f))
                    {
                        if (!string.IsNullOrEmpty(keyStr))
                        {
                            using (LinearGradientBrush bb = new LinearGradientBrush(badgeRect, Color.FromArgb(238, 242, 255), Color.FromArgb(224, 231, 255), 90f))
                            using (Pen bPen = new Pen(Color.FromArgb(165, 180, 252), 1.2f))
                            {
                                g.FillPath(bb, bp);
                                g.DrawPath(bPen, bp);
                            }

                            string provDisplayName = AiConfig.GetProviderDisplayName(currentProv);
                            string badgeTitle = "Đã nhận diện: " + provDisplayName;
                            if (currentProv == AiProviderType.Groq) badgeTitle = "Đã nhận diện: Groq Cloud";
                            else if (currentProv == AiProviderType.GoogleAIStudio) badgeTitle = "Đã nhận diện: Google AI Studio";
                            else if (currentProv == AiProviderType.OpenAI) badgeTitle = "Đã nhận diện: OpenAI";
                            else if (currentProv == AiProviderType.DeepSeek) badgeTitle = "Đã nhận diện: DeepSeek";
                            else if (currentProv == AiProviderType.OpenRouter) badgeTitle = "Đã nhận diện: OpenRouter";
                            else if (currentProv == AiProviderType.Ollama) badgeTitle = "Đã nhận diện: Ollama Local";

                            using (Font bf = new Font("Segoe UI", 9.6f, FontStyle.Bold))
                            using (SolidBrush bbText = new SolidBrush(Color.FromArgb(67, 56, 202)))
                            {
                                g.DrawString(badgeTitle, bf, bbText, badgeRect.X + 14, badgeRect.Y + (badgeRect.Height - 18) / 2f);
                            }
                        }
                        else
                        {
                            using (SolidBrush bb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                            using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                            {
                                g.FillPath(bb, bp);
                                g.DrawPath(bPen, bp);
                            }

                            using (Font bf = new Font("Segoe UI", 9.0f, FontStyle.Italic))
                            using (SolidBrush bbText = new SolidBrush(Color.FromArgb(148, 163, 184)))
                            {
                                g.DrawString("Nhập hoặc dán API Key để tự động nhận diện nhà cung cấp", bf, bbText, badgeRect.X + 14, badgeRect.Y + (badgeRect.Height - 16) / 2f);
                            }
                        }
                    }

                    // Cột Phải: Ô chọn / Gõ Model
                    g.DrawString("Chọn Mô hình AI:", titleFont, textBrush, rightColX, sRow1Y);

                    // Render Model Chips bên phải
                    _chipRects.Clear();
                    _chipXRects.Clear();

                    int chipsStartY = sRow1Y + 26 + ctlH + 12;
                    int curX = rightColX;
                    int curY = chipsStartY;
                    int chipH = 28;
                    int xBtnW = 20;
                    int hPad = 10;
                    int gap = 6;
                    int maxRight = rightColX + colW;

                    var models = (_currentProfile != null) ? (_currentProfile.SavedModels ?? new List<string>()) : new List<string>();
                    if (models.Count == 0 && (int)currentProv >= 0)
                    {
                        models = AiConfig.GetRecommendedModels(currentProv);
                    }

                    if (models.Count > 0)
                    {
                        using (Font f = new Font("Segoe UI", 8.5f))
                        using (Font fb = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            foreach (string m in models)
                            {
                                bool isSel = (_currentProfile != null && m == _currentProfile.SelectedModel);
                                SizeF sz = g.MeasureString(m, isSel ? fb : f);
                                int chipW = (int)sz.Width + hPad * 2 + xBtnW;

                                if (curX + chipW > maxRight && curX > rightColX)
                                {
                                    curX = rightColX;
                                    curY += chipH + 6;
                                }

                                RectangleF cr = new RectangleF(curX, curY, chipW, chipH);
                                _chipRects.Add(new Rectangle(curX, curY, chipW, chipH));
                                _chipXRects.Add(new Rectangle(curX + chipW - xBtnW, curY, xBtnW, chipH));

                                Color bg = isSel ? Color.FromArgb(79, 70, 229) : Color.FromArgb(224, 231, 255);
                                using (GraphicsPath gp = RoundRect(cr, 12f))
                                using (SolidBrush bgBrush = new SolidBrush(bg))
                                {
                                    g.FillPath(bgBrush, gp);
                                }

                                if (!isSel)
                                {
                                    using (GraphicsPath gp = RoundRect(cr, 12f))
                                    using (Pen border = new Pen(Color.FromArgb(165, 180, 252), 1f))
                                    {
                                        g.DrawPath(border, gp);
                                    }
                                }

                                Color fg = isSel ? Color.White : Color.FromArgb(55, 48, 163);
                                using (SolidBrush fgBrush = new SolidBrush(fg))
                                {
                                    g.DrawString(m, isSel ? fb : f, fgBrush, curX + hPad, curY + (chipH - sz.Height) / 2f);
                                }

                                Color xCol = isSel ? Color.FromArgb(220, 220, 255) : Color.FromArgb(99, 102, 241);
                                using (Font xf = new Font("Segoe UI", 8f, FontStyle.Bold))
                                using (SolidBrush xBrush = new SolidBrush(xCol))
                                {
                                    g.DrawString("×", xf, xBrush, curX + chipW - xBtnW + 1, curY + (chipH - 14) / 2f);
                                }

                                curX += chipW + gap;
                            }
                        }
                    }

                    // Status Text dưới nút bấm
                    int statusY = sRow1Y + 26 + ctlH + 28 + 48 + 48;
                    if (!string.IsNullOrEmpty(_statusText))
                    {
                        g.DrawString(_statusText, statusFont, statusBrush, new RectangleF(leftColX, statusY, totalW, 40));
                    }
                }
                else
                {
                    // Chế độ Nâng cao (Advanced Mode Layout)
                    int rowSpacing = 68;
                    int row1Y = padTop + 48;
                    int row2Y = row1Y + rowSpacing;
                    int row3Y = row2Y + rowSpacing;
                    int row4Y = row3Y + rowSpacing;

                    bool isOllama = (cboProvider != null && cboProvider.SelectedIndex == (int)AiProviderType.Ollama);

                    g.DrawString("Bộ lưu API Key:", titleFont, textBrush, leftColX, row1Y);

                    if (_config.Profiles.Count == 0 || _currentProfile == null)
                    {
                        using (Font hintFont = new Font("Segoe UI", 10.5f, FontStyle.Italic))
                        using (SolidBrush hintBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                        {
                            g.DrawString("Chưa có cấu hình nào. Bấm \"+ Thêm Key\" để bắt đầu.", hintFont, hintBrush, leftColX, row2Y + 30);
                        }
                    }
                    else
                    {
                        g.DrawString("Tên bộ lưu:", titleFont, labelBrush, leftColX, row2Y);
                        g.DrawString("Nhà cung cấp AI:", titleFont, labelBrush, leftColX, row3Y);
                        if (!isOllama) g.DrawString("Chuỗi API Key:", titleFont, labelBrush, leftColX, row4Y);
                    }

                    g.DrawString("Độ sáng tạo:", titleFont, labelBrush, rightColX, row1Y);
                    float tempVal = trkTemp != null ? trkTemp.Value : 0.7f;
                    g.DrawString(tempVal.ToString("0.0"), valueFont, valueBrush, rightColX + colW - 36, row1Y);

                    g.DrawString("Chỉ dẫn hệ thống:", titleFont, labelBrush, rightColX, row2Y);

                    int modelLabelY = row2Y + 24 + 110 + 8;
                    g.DrawString(isOllama ? "Mô hình Ollama trên máy:" : "Mô hình AI:", titleFont, labelBrush, rightColX, modelLabelY);

                    // VẼ MODEL CHIPS TRỰC TIẾP
                    _chipRects.Clear();
                    _chipXRects.Clear();

                    int chipsStartY = modelLabelY + 24 + 34 + 8;
                    int curX = rightColX;
                    int curY = chipsStartY;
                    int chipH = 28;
                    int xBtnW = 20;
                    int hPad = 10;
                    int gap = 6;
                    int maxRight = rightColX + colW;

                    var models = (_currentProfile != null) ? (_currentProfile.SavedModels ?? new List<string>()) : new List<string>();

                    if (models.Count == 0)
                    {
                        using (Font hintFont = new Font("Segoe UI", 8.5f, FontStyle.Italic))
                        using (SolidBrush hintBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                        {
                            g.DrawString("Chưa có model nào. Chọn hoặc gõ tên rồi nhấn Enter ↑", hintFont, hintBrush, rightColX, chipsStartY + 4);
                        }
                        curY += 28;
                    }
                    else
                    {
                        using (Font f = new Font("Segoe UI", 8.5f))
                        using (Font fb = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            foreach (string m in models)
                            {
                                bool isSel = (_currentProfile != null && m == _currentProfile.SelectedModel);
                                SizeF sz = g.MeasureString(m, isSel ? fb : f);
                                int chipW = (int)sz.Width + hPad * 2 + xBtnW;

                                if (curX + chipW > maxRight && curX > rightColX)
                                {
                                    curX = rightColX;
                                    curY += chipH + 6;
                                }

                                RectangleF cr = new RectangleF(curX, curY, chipW, chipH);
                                _chipRects.Add(new Rectangle(curX, curY, chipW, chipH));
                                _chipXRects.Add(new Rectangle(curX + chipW - xBtnW, curY, xBtnW, chipH));

                                Color bg = isSel ? Color.FromArgb(79, 70, 229) : Color.FromArgb(224, 231, 255);
                                using (GraphicsPath gp = RoundRect(cr, 12f))
                                using (SolidBrush bgBrush = new SolidBrush(bg))
                                {
                                    g.FillPath(bgBrush, gp);
                                }

                                if (!isSel)
                                {
                                    using (GraphicsPath gp = RoundRect(cr, 12f))
                                    using (Pen border = new Pen(Color.FromArgb(165, 180, 252), 1f))
                                    {
                                        g.DrawPath(border, gp);
                                    }
                                }

                                Color fg = isSel ? Color.White : Color.FromArgb(55, 48, 163);
                                using (SolidBrush fgBrush = new SolidBrush(fg))
                                {
                                    g.DrawString(m, isSel ? fb : f, fgBrush, curX + hPad, curY + (chipH - sz.Height) / 2f);
                                }

                                Color xCol = isSel ? Color.FromArgb(220, 220, 255) : Color.FromArgb(99, 102, 241);
                                using (Font xf = new Font("Segoe UI", 8f, FontStyle.Bold))
                                using (SolidBrush xBrush = new SolidBrush(xCol))
                                {
                                    g.DrawString("×", xf, xBrush, curX + chipW - xBtnW + 1, curY + (chipH - 14) / 2f);
                                }

                                curX += chipW + gap;
                            }
                        }
                    }

                    int statusY = curY + chipH + 10;
                    if (!string.IsNullOrEmpty(_statusText))
                    {
                        g.DrawString(_statusText, statusFont, statusBrush, new RectangleF(rightColX, statusY, colW, 56));
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 1. Kiểm tra bấm vào Mode Switcher Tab
            if (_simpleTabRect.Contains(e.Location))
            {
                if (!_isSimpleMode)
                {
                    _isSimpleMode = true;
                    EnsureProfileExistsForSimpleMode();
                    if (_currentProfile != null && !string.IsNullOrEmpty(_currentProfile.ApiKey))
                    {
                        AiProviderType detected = DetectProviderFromApiKey(_currentProfile.ApiKey);
                        if (cboProvider.SelectedIndex != (int)detected)
                        {
                            cboProvider.SelectedIndex = (int)detected;
                        }
                    }
                    RelayoutControls();
                    Invalidate();
                }
                return;
            }
            if (_advancedTabRect.Contains(e.Location))
            {
                if (_isSimpleMode)
                {
                    _isSimpleMode = false;
                    RelayoutControls();
                    Invalidate();
                }
                return;
            }

            // 2. Kiểm tra bấm vào Model Chips
            var models = (_currentProfile != null) ? (_currentProfile.SavedModels ?? new List<string>()) : new List<string>();
            if (_isSimpleMode && models.Count == 0 && cboProvider.SelectedIndex >= 0)
            {
                models = AiConfig.GetRecommendedModels((AiProviderType)cboProvider.SelectedIndex);
            }

            for (int i = 0; i < _chipXRects.Count && i < models.Count; i++)
            {
                if (_chipXRects[i].Contains(e.Location))
                {
                    string removed = models[i];
                    if (_currentProfile != null && _currentProfile.SavedModels != null)
                    {
                        _currentProfile.SavedModels.Remove(removed);
                        if (_currentProfile.SelectedModel == removed)
                            _currentProfile.SelectedModel = _currentProfile.SavedModels.Count > 0 ? _currentProfile.SavedModels[0] : "";
                    }
                    Invalidate();
                    return;
                }
                if (_chipRects[i].Contains(e.Location))
                {
                    if (!models[i].StartsWith("⏳") && !models[i].StartsWith("⚠️"))
                    {
                        EnsureProfileExistsForSimpleMode();
                        if (_currentProfile != null)
                        {
                            _currentProfile.SelectedModel = models[i];
                            if (_currentProfile.SavedModels == null) _currentProfile.SavedModels = new List<string>();
                            if (!_currentProfile.SavedModels.Contains(models[i])) _currentProfile.SavedModels.Add(models[i]);
                        }
                        Invalidate();
                    }
                    return;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_simpleTabRect.Contains(e.Location) || _advancedTabRect.Contains(e.Location))
            {
                this.Cursor = Cursors.Hand;
                return;
            }

            bool overChip = false;
            for (int i = 0; i < _chipXRects.Count; i++)
            {
                if (_chipXRects[i].Contains(e.Location) || (i < _chipRects.Count && _chipRects[i].Contains(e.Location)))
                {
                    overChip = true;
                    break;
                }
            }
            this.Cursor = overChip ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RelayoutControls();
            Invalidate();
        }

        private void RelayoutControls()
        {
            if (cboProfiles == null) return;

            int padLeft = 28;
            int padTop = 26;
            int colGap = 36;
            int totalW = this.ClientSize.Width - padLeft * 2;
            int colW = Math.Max(320, (totalW - colGap) / 2);
            int leftColX = padLeft;
            int rightColX = leftColX + colW + colGap;

            // Kích thước thanh chuyển chế độ (Mode Switcher)
            int switchW = 230;
            int switchH = 32;
            _modeSwitcherRect = new RectangleF(this.ClientSize.Width - padLeft - switchW, padTop + 2, switchW, switchH);
            _simpleTabRect = new RectangleF(_modeSwitcherRect.X + 2, _modeSwitcherRect.Y + 2, switchW / 2f - 2, switchH - 4);
            _advancedTabRect = new RectangleF(_modeSwitcherRect.X + switchW / 2f, _modeSwitcherRect.Y + 2, switchW / 2f - 2, switchH - 4);

            if (_isSimpleMode)
            {
                // Ẩn các trường nâng cao
                cboProfiles.Visible = false;
                btnAddProfile.Visible = false;
                btnDeleteProfile.Visible = false;
                txtKeyName.Visible = false;
                cboProvider.Visible = false;
                trkTemp.Visible = false;
                txtSystemPrompt.Visible = false;

                // Hiện các trường đơn giản
                txtApiKey.Visible = true;
                btnToggleKey.Visible = true;
                cboModel.Visible = true;
                btnTest.Visible = true;
                btnSave.Visible = true;

                int sRow1Y = padTop + 54;
                int ctlH = 36;

                txtApiKey.Location = new Point(leftColX, sRow1Y + 26);
                txtApiKey.Size = new Size(colW - 50, ctlH);
                btnToggleKey.Location = new Point(leftColX + colW - 44, sRow1Y + 26);
                btnToggleKey.Size = new Size(44, ctlH);

                int btnY = sRow1Y + 26 + ctlH + 12 + 40 + 16; // đặt dưới badge nhận diện
                int halfLeftBtnW = (colW - 16) / 2;
                btnTest.Location = new Point(leftColX, btnY);
                btnTest.Size = new Size(halfLeftBtnW, 42);
                btnSave.Location = new Point(leftColX + halfLeftBtnW + 16, btnY);
                btnSave.Size = new Size(halfLeftBtnW, 42);

                cboModel.Location = new Point(rightColX, sRow1Y + 26);
                cboModel.Size = new Size(colW, ctlH);
            }
            else
            {
                // Hiện các trường nâng cao
                cboProfiles.Visible = true;
                btnAddProfile.Visible = true;
                btnDeleteProfile.Visible = true;
                trkTemp.Visible = true;
                txtSystemPrompt.Visible = true;

                bool hasProfile = (_currentProfile != null);
                bool isOllama = (hasProfile && cboProvider != null && cboProvider.SelectedIndex == (int)AiProviderType.Ollama);

                txtKeyName.Visible = hasProfile;
                cboProvider.Visible = hasProfile;
                cboModel.Visible = hasProfile;
                txtApiKey.Visible = hasProfile && !isOllama;
                btnToggleKey.Visible = hasProfile && !isOllama;
                btnTest.Visible = hasProfile;
                btnSave.Visible = hasProfile;
                btnDeleteProfile.Visible = hasProfile;

                int rowSpacing = 68;
                int row1Y = padTop + 48;
                int ctl1Y = row1Y + 24;
                cboProfiles.Location = new Point(leftColX, ctl1Y);
                cboProfiles.Size = new Size(Math.Max(140, colW - 175), 34);
                btnAddProfile.Location = new Point(cboProfiles.Right + 10, ctl1Y);
                btnAddProfile.Size = new Size(115, 34);
                btnDeleteProfile.Location = new Point(btnAddProfile.Right + 6, ctl1Y);
                btnDeleteProfile.Size = new Size(44, 34);

                if (hasProfile)
                {
                    int row2Y = row1Y + rowSpacing;
                    txtKeyName.Location = new Point(leftColX, row2Y + 24);
                    txtKeyName.Size = new Size(colW, 34);

                    int row3Y = row2Y + rowSpacing;
                    cboProvider.Location = new Point(leftColX, row3Y + 24);
                    cboProvider.Size = new Size(colW, 34);

                    if (!isOllama)
                    {
                        int row4Y = row3Y + rowSpacing;
                        int ctl4Y = row4Y + 24;
                        txtApiKey.Location = new Point(leftColX, ctl4Y);
                        txtApiKey.Size = new Size(colW - 50, 34);
                        btnToggleKey.Location = new Point(leftColX + colW - 44, ctl4Y);
                        btnToggleKey.Size = new Size(44, 34);

                        int btnY = row4Y + 70;
                        int halfLeftBtnW = (colW - 16) / 2;
                        btnTest.Location = new Point(leftColX, btnY);
                        btnTest.Size = new Size(halfLeftBtnW, 42);
                        btnSave.Location = new Point(leftColX + halfLeftBtnW + 16, btnY);
                        btnSave.Size = new Size(halfLeftBtnW, 42);
                    }
                    else
                    {
                        int btnY = row3Y + 70;
                        int halfLeftBtnW = (colW - 16) / 2;
                        btnTest.Location = new Point(leftColX, btnY);
                        btnTest.Size = new Size(halfLeftBtnW, 42);
                        btnSave.Location = new Point(leftColX + halfLeftBtnW + 16, btnY);
                        btnSave.Size = new Size(halfLeftBtnW, 42);
                    }
                }

                trkTemp.Location = new Point(rightColX, ctl1Y + 3);
                trkTemp.Size = new Size(colW, 28);

                int sysPromptH = 110;
                txtSystemPrompt.Location = new Point(rightColX, row1Y + rowSpacing + 24);
                txtSystemPrompt.Size = new Size(colW, sysPromptH);

                int modelY = row1Y + rowSpacing + 24 + sysPromptH + 8;
                int modelLabelH = 24;
                cboModel.Location = new Point(rightColX, modelY + modelLabelH);
                cboModel.Size = new Size(colW, 34);
            }
        }

        private GraphicsPath RoundRect(RectangleF r, float radius)
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, radius, radius, 180, 90);
            gp.AddArc(r.Width + r.X - radius, r.Y, radius, radius, 270, 90);
            gp.AddArc(r.Width + r.X - radius, r.Height + r.Y - radius, radius, radius, 0, 90);
            gp.AddArc(r.X, r.Height + r.Y - radius, radius, radius, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        public void ReloadProfilesToUi()
        {
            _isBinding = true;
            _config = AiConfig.Load();
            _currentProfile = _config.GetActiveProfile(); // có thể null

            cboProfiles.Items.Clear();
            int selIdx = -1;
            for (int i = 0; i < _config.Profiles.Count; i++)
            {
                var p = _config.Profiles[i];
                string displayName = !string.IsNullOrEmpty(p.Name) ? p.Name : "(Chưa đặt tên)";
                cboProfiles.Items.Add(displayName);
                if (_currentProfile != null && p.Id == _config.ActiveProfileId) selIdx = i;
            }
            if (cboProfiles.Items.Count > 0 && selIdx >= 0)
            {
                cboProfiles.SelectedIndex = selIdx;
            }
            else
            {
                cboProfiles.SelectedIndex = -1;
            }

            txtSystemPrompt.Text = _config.SystemInstructions ?? "";

            if (_currentProfile != null)
            {
                BindProfileToUi(_currentProfile);
            }
            else
            {
                ClearFormFields();
            }
        }

        private void ClearFormFields()
        {
            _isBinding = true;
            txtKeyName.Text = "";
            cboProvider.SelectedIndex = -1;
            cboModel.Items.Clear();
            cboModel.Text = "";
            txtApiKey.Text = "";
            trkTemp.Value = 0.7f;
            _statusText = "";
            _isBinding = false;
            RelayoutControls();
            Invalidate();
        }

        private void SafeBeginInvoke(Action action)
        {
            if (this.IsDisposed) return;
            if (this.IsHandleCreated)
            {
                try { this.BeginInvoke(action); } catch { }
            }
            else
            {
                EventHandler handler = null;
                handler = (s, e) =>
                {
                    this.HandleCreated -= handler;
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        try { this.BeginInvoke(action); } catch { }
                    }
                };
                this.HandleCreated += handler;
            }
        }

        private void BindProfileToUi(AiKeyProfile profile)
        {
            if (profile == null) return;
            _isBinding = true;

            txtKeyName.Text = profile.Name ?? "";
            
            if ((int)profile.Provider >= 0 && (int)profile.Provider < cboProvider.Items.Count)
            {
                cboProvider.SelectedIndex = (int)profile.Provider;
                PopulateModelsForProvider(profile.Provider, profile.SelectedModel);
            }
            else
            {
                cboProvider.SelectedIndex = -1;
                cboModel.Items.Clear();
                cboModel.Text = "";
            }

            cboModel.SelectedIndex = -1;
            cboModel.Text = "";
            RebuildModelChips();

            txtApiKey.Text = profile.ApiKey ?? "";
            trkTemp.Value = Math.Max(0.0f, Math.Min(2.0f, profile.Temperature));

            _isBinding = false;
            RelayoutControls();
            Invalidate();
        }

        private void SetStatus(string text, Color color)
        {
            _statusText = text;
            _statusColor = color;
            Invalidate();
        }

        private void OnProviderChanged()
        {
            if (cboProvider.SelectedIndex < 0)
            {
                cboModel.Items.Clear();
                cboModel.Text = "";
                RelayoutControls();
                Invalidate();
                return;
            }

            if (_currentProfile != null)
            {
                _currentProfile.Provider = (AiProviderType)cboProvider.SelectedIndex;
                _currentProfile.SavedModels.Clear();
                _currentProfile.SelectedModel = "";
            }

            AiProviderType prov = (AiProviderType)cboProvider.SelectedIndex;
            PopulateModelsForProvider(prov);
            RebuildModelChips();
            if (prov != AiProviderType.Ollama)
            {
                cboModel.Text = "";
                SetStatus("Nhấn 'Kiểm tra kết nối' để kiểm tra chuỗi API Key và đo độ trễ phản hồi.", Color.FromArgb(71, 85, 105));
            }
            RelayoutControls();
            Invalidate();
        }

        private void PopulateModelsForProvider(AiProviderType prov, string modelToSelect = null)
        {
            cboModel.Items.Clear();

            if (prov == AiProviderType.Ollama)
            {
                var recommendedOllama = AiConfig.GetRecommendedModels(AiProviderType.Ollama);
                foreach (var r in recommendedOllama)
                {
                    cboModel.Items.Add(r);
                }
                cboModel.SelectedIndex = -1;
                cboModel.Text = "";

                SetStatus("Đang quét danh sách các model Ollama cục bộ có sẵn trên máy...", Color.FromArgb(79, 70, 229));

                AiService.ScanLocalOllamaModelsAsync((models, err) =>
                {
                    SafeBeginInvoke(() =>
                    {
                        if (cboProvider.SelectedIndex != (int)AiProviderType.Ollama) return;

                        if (models != null && models.Count > 0)
                        {
                            cboModel.Items.Clear();
                            foreach (var m in models)
                            {
                                cboModel.Items.Add(m);
                            }
                            SetStatus(string.Format("Đã phát hiện {0} model Ollama trên máy sẵn sàng chạy offline: {1}", models.Count, string.Join(", ", models.ToArray())), Color.FromArgb(22, 163, 74));
                        }
                        else
                        {
                            SetStatus("Chưa thấy model nào đang chạy. Bạn có thể chọn deepseek-r1:7b hoặc gõ tên model rồi bấm Enter.", Color.FromArgb(217, 119, 6));
                        }
                        RebuildModelChips();
                    });
                });
            }
            else if ((int)prov >= 0)
            {
                var list = AiConfig.GetRecommendedModels(prov);
                foreach (var m in list)
                {
                    cboModel.Items.Add(m);
                }
                if (!string.IsNullOrEmpty(modelToSelect))
                {
                    int found = cboModel.Items.IndexOf(modelToSelect);
                    if (found >= 0)
                    {
                        cboModel.SelectedIndex = found;
                        cboModel.Text = modelToSelect;
                    }
                    else
                    {
                        cboModel.Text = modelToSelect;
                    }
                }
                else
                {
                    cboModel.SelectedIndex = -1;
                    cboModel.Text = "";
                }
            }
        }

        private void RebuildModelChips()
        {
            Invalidate();
        }

        private void CollectFieldsToProfile()
        {
            EnsureProfileExistsForSimpleMode();
            if (_currentProfile == null) return;

            if (_isSimpleMode)
            {
                AiProviderType detected = (cboProvider.SelectedIndex >= 0) ? (AiProviderType)cboProvider.SelectedIndex : DetectProviderFromApiKey(txtApiKey.Text);
                _currentProfile.Provider = detected;
                if (string.IsNullOrEmpty(_currentProfile.Name) || _currentProfile.Name == "(Chưa đặt tên)")
                {
                    _currentProfile.Name = AiConfig.GetProviderDisplayName(detected);
                }
                _currentProfile.ApiKey = (txtApiKey.Text ?? "").Trim();
                _currentProfile.Temperature = 0.7f;
            }
            else
            {
                _currentProfile.Name = (txtKeyName.Text ?? "").Trim();
                if (cboProvider.SelectedIndex >= 0)
                {
                    _currentProfile.Provider = (AiProviderType)cboProvider.SelectedIndex;
                }
                _currentProfile.ApiKey = (txtApiKey.Text ?? "").Trim();
                _currentProfile.Temperature = trkTemp.Value;
                _config.SystemInstructions = (txtSystemPrompt.Text ?? "").Trim();
            }

            if (_currentProfile.SavedModels == null) _currentProfile.SavedModels = new List<string>();

            if (cboModel.SelectedIndex >= 0)
            {
                string pickedModel = cboModel.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(pickedModel) && !pickedModel.StartsWith("⏳") && !pickedModel.StartsWith("⚠️"))
                {
                    if (_currentProfile.Provider == AiProviderType.GoogleAIStudio && pickedModel == "gemini-3.1-flash") pickedModel = "gemini-3.1-flash-lite";
                    _currentProfile.SelectedModel = pickedModel;
                    if (!_currentProfile.SavedModels.Contains(pickedModel)) _currentProfile.SavedModels.Add(pickedModel);
                }
            }
            else
            {
                string typedModel = cboModel.Text != null ? cboModel.Text.Trim() : "";
                if (!string.IsNullOrEmpty(typedModel) && !typedModel.StartsWith("⏳") && !typedModel.StartsWith("⚠️"))
                {
                    if (_currentProfile.Provider == AiProviderType.GoogleAIStudio && typedModel == "gemini-3.1-flash") typedModel = "gemini-3.1-flash-lite";
                    _currentProfile.SelectedModel = typedModel;
                    if (!_currentProfile.SavedModels.Contains(typedModel))
                    {
                        _currentProfile.SavedModels.Add(typedModel);
                    }
                }
            }

            if (_currentProfile.Provider == AiProviderType.GoogleAIStudio)
            {
                if (_currentProfile.SelectedModel == "gemini-3.1-flash") _currentProfile.SelectedModel = "gemini-3.1-flash-lite";
                for (int si = 0; si < _currentProfile.SavedModels.Count; si++)
                {
                    if (_currentProfile.SavedModels[si] == "gemini-3.1-flash") _currentProfile.SavedModels[si] = "gemini-3.1-flash-lite";
                }
                _currentProfile.SavedModels.RemoveAll(m => m.StartsWith("gemini-1.") || m.StartsWith("gemini-2.") || m == "gemini-2.5-flash" || m == "gemini-2.5-pro");
                List<string> cleanList = new List<string>();
                foreach (var m in _currentProfile.SavedModels)
                {
                    if (!cleanList.Contains(m)) cleanList.Add(m);
                }
                _currentProfile.SavedModels = cleanList;
            }

            if (string.IsNullOrWhiteSpace(_currentProfile.SelectedModel) && _currentProfile.SavedModels.Count > 0)
            {
                _currentProfile.SelectedModel = _currentProfile.SavedModels[0];
            }
        }

        private void AddNewKeyProfile()
        {
            if (_currentProfile != null) CollectFieldsToProfile();

            var newProf = _config.CreateDraftProfile();
            _currentProfile = newProf;

            _isBinding = true;
            cboProfiles.Items.Add("(Chưa đặt tên)");
            cboProfiles.SelectedIndex = cboProfiles.Items.Count - 1;
            _isBinding = false;

            ClearFormFields();
            _currentProfile = newProf;
            BindProfileToUi(newProf);

            SetStatus("Hãy điền đầy đủ thông tin rồi bấm \"Lưu cấu hình\".", Color.FromArgb(79, 70, 229));
        }

        private void DeleteCurrentKeyProfile()
        {
            if (_currentProfile == null)
            {
                MessageBox.Show("Không có cấu hình nào để xóa.", "ZENQ AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = MessageBox.Show(
                string.Format("Bạn có chắc chắn muốn xóa cấu hình '{0}'?",
                    !string.IsNullOrEmpty(_currentProfile.Name) ? _currentProfile.Name : "(Chưa đặt tên)"),
                "Xác nhận xóa Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialog == DialogResult.Yes)
            {
                _config.RemoveProfile(_currentProfile.Id);
                _config.Save();
                ReloadProfilesToUi();
                SetStatus("Đã xóa cấu hình Key thành công.", Color.FromArgb(220, 38, 38));
            }
        }

        private void RunConnectionTest()
        {
            EnsureProfileExistsForSimpleMode();
            if (_currentProfile == null)
            {
                SetStatus("Chưa có cấu hình nào. Hãy nhập API Key trước.", Color.FromArgb(220, 38, 38));
                return;
            }

            CollectFieldsToProfile();

            if ((int)_currentProfile.Provider < 0)
            {
                SetStatus("Chưa xác định Nhà cung cấp AI.", Color.FromArgb(220, 38, 38));
                return;
            }
            if (string.IsNullOrWhiteSpace(_currentProfile.SelectedModel) && _currentProfile.SavedModels.Count == 0)
            {
                SetStatus("Chưa chọn Model AI nào.", Color.FromArgb(220, 38, 38));
                return;
            }
            if (string.IsNullOrWhiteSpace(_currentProfile.SelectedModel) && _currentProfile.SavedModels.Count > 0)
            {
                _currentProfile.SelectedModel = _currentProfile.SavedModels[0];
            }

            SetStatus("Đang gửi yêu cầu kiểm tra kết nối tới " + _currentProfile.SelectedModel + "...", Color.FromArgb(79, 70, 229));
            btnTest.Enabled = false;

            AiService.TestConnectionAsync(_config, (success, latencyMs, message) =>
            {
                if (this.IsDisposed) return;
                this.BeginInvoke(new Action(() =>
                {
                    btnTest.Enabled = true;
                    if (success)
                    {
                        SetStatus(string.Format("Kết nối thành công! Phản hồi sau {0} ms từ {1}.", latencyMs, _currentProfile.SelectedModel), Color.FromArgb(22, 163, 74));
                    }
                    else
                    {
                        SetStatus(message, Color.FromArgb(220, 38, 38));
                    }
                }));
            });
        }

        private void SaveConfig(bool showPrompt = true)
        {
            EnsureProfileExistsForSimpleMode();
            if (_currentProfile == null)
            {
                MessageBox.Show("Không có cấu hình nào để lưu.\nHãy nhập API Key trước.", "ZENQ AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CollectFieldsToProfile();

            if (string.IsNullOrWhiteSpace(_currentProfile.SelectedModel) && _currentProfile.SavedModels.Count > 0)
            {
                _currentProfile.SelectedModel = _currentProfile.SavedModels[0];
            }

            var errors = _currentProfile.Validate();
            if (errors.Count > 0)
            {
                string errorMsg = "Không thể lưu! Vui lòng khắc phục các lỗi sau:\n\n";
                for (int i = 0; i < errors.Count; i++)
                {
                    errorMsg += "• " + errors[i] + "\n";
                }
                MessageBox.Show(errorMsg, "Lỗi cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus(string.Join(" | ", errors.ToArray()), Color.FromArgb(220, 38, 38));
                return;
            }

            _config.ActiveProfileId = _currentProfile.Id;
            _config.Save();

            if (_appForm != null)
            {
                _appForm.ReloadAiSidebarConfig();
            }

            _isBinding = true;
            cboProfiles.Items.Clear();
            for (int i = 0; i < _config.Profiles.Count; i++)
            {
                var p = _config.Profiles[i];
                cboProfiles.Items.Add(!string.IsNullOrEmpty(p.Name) ? p.Name : "(Chưa đặt tên)");
                if (p.Id == _currentProfile.Id)
                {
                    cboProfiles.SelectedIndex = i;
                }
            }
            _isBinding = false;

            if (showPrompt)
            {
                SetStatus("Đã lưu thiết lập thành công! Model: " + _currentProfile.SelectedModel, Color.FromArgb(22, 163, 74));
                MessageBox.Show("Đã lưu thiết lập AI thành công!\nBạn có thể sử dụng ngay trên tất cả các thẻ làm việc.", "ZENQ AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (_appForm != null)
            {
                _appForm.RefreshAiChatProfileSelectors();
                _appForm.Invalidate();
            }
        }
    }
}

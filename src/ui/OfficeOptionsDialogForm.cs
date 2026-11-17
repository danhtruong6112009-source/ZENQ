using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class OfficeOptionsDialogForm : Form
    {
        private static readonly Color AccentBlue = Color.FromArgb(15, 108, 189);
        private static readonly Color AccentBlueHover = Color.FromArgb(17, 94, 163);
        private static readonly Color AccentSoftBg = Color.FromArgb(235, 243, 252);
        private static readonly Color AccentSoftBorder = Color.FromArgb(180, 214, 250);
        private static readonly Color CanvasBg = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(220, 226, 234);
        private static readonly Color InkPrimary = Color.FromArgb(24, 34, 48);
        private static readonly Color InkSecondary = Color.FromArgb(100, 116, 139);

        private readonly AppForm _appForm;

        // Giao diện & Chủ đề
        private FluentRadioRow _rbRibbonIsland;
        private FluentRadioRow _rbRibbonStandard;
        private FluentRadioRow _rbThemeLight;
        private FluentRadioRow _rbThemeDark;
        private FluentRadioRow _rbLangVi;
        private FluentRadioRow _rbLangEn;

        // Lưu trữ & Tự động lưu
        private FluentToggleRow _chkAutoSave;
        private ComboBox _cboAutoSaveInterval;

        // Soạn thảo & Hiển thị
        private FluentToggleRow _chkShowRuler;
        private FluentToggleRow _chkSmoothScroll;
        private FluentToggleRow _chkFormattingMarks;
        private FluentToggleRow _chkWordPrediction;
        private FluentToggleRow _chkAutoCorrectVi;
        private ComboBox _cboDefaultZoom;

        // AI
        private FluentToggleRow _chkAiCopilot;
        private ComboBox _cboAiProvider;

        // Mặc định & Lối tắt
        private FluentToggleRow _chkRegOnStartup;

        // Navigation
        private FluentNavRail _navRail;
        private Panel[] _contentPanels;

        public OfficeOptionsDialogForm(AppForm appForm = null)
        {
            _appForm = appForm;
            InitializeComponent();
            LoadCurrentSettings();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FluentUi.RoundWindow(this);
        }

        public void SelectPage(int index)
        {
            if (_navRail != null && index >= 0 && index < _contentPanels.Length)
            {
                _navRail.SelectedIndex = index;
            }
        }

        private void InitializeComponent()
        {
            this.Text = AppL10n.T("Tùy chọn - ZenQ Office 6.0", "Options - ZenQ Office 6.0");
            int workH = Screen.PrimaryScreen?.WorkingArea.Height ?? 768;
            int targetH = Math.Min(640, Math.Max(540, workH - 28));
            this.Size = new Size(824, targetH);
            this.MinimumSize = new Size(760, 520);
            this.StartPosition = (_appForm != null) ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = CanvasBg;
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            this.DoubleBuffered = true;

            try { this.Icon = ZenQOfficeLogoRenderer.CreateAppIcon(32); } catch { }

            // 1. HEADER, FOOTER, LEFT RAIL & CONTENT (Add Fill -> Left -> Bottom -> Top so Top/Bottom span full width)
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White
            };
            headerPanel.Paint += (s, e) =>
            {
                using var d2d = new D2DGraphics(e.Graphics, headerPanel.Width, headerPanel.Height);
                d2d.Clear(Color.White);

                using (SolidBrush topStrip = new SolidBrush(AccentBlue))
                    d2d.FillRectangle(topStrip, 0, 0, headerPanel.Width, 3);

                using (Pen divPen = new Pen(Color.FromArgb(228, 233, 240), 1f))
                    d2d.DrawLine(divPen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);

                RectangleF badgeRect = new RectangleF(20f, 15f, 40f, 40f);
                using (SolidBrush bFill = new SolidBrush(AccentSoftBg))
                using (Pen bBorder = new Pen(AccentSoftBorder, 1f))
                {
                    d2d.FillRoundedRectangle(bFill, badgeRect, 10f);
                    d2d.DrawRoundedRectangle(bBorder, badgeRect, 10f);
                }

                FluentIconRenderer.DrawGlyph(d2d, '\uE713', badgeRect, AccentBlue, 20f);

                using (Font titleF = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold))
                using (SolidBrush titleB = new SolidBrush(InkPrimary))
                {
                    d2d.DrawString(AppL10n.T("Tùy chọn hệ thống & Thiết lập ứng dụng", "System Options & Application Settings"), titleF, titleB, new RectangleF(72f, 13f, headerPanel.Width - 92f, 24f));
                }

                using (Font subF = new Font("Segoe UI", 8.75f, FontStyle.Regular))
                using (SolidBrush subB = new SolidBrush(InkSecondary))
                {
                    d2d.DrawString(AppL10n.T("Tùy biến giao diện thanh công cụ, tự động lưu, soạn thảo và trợ lý AI", "Customize Ribbon appearance, auto-save, editing, and AI Assistant"), subF, subB, new RectangleF(72f, 36f, headerPanel.Width - 92f, 20f));
                }
            };

            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Width = this.ClientSize.Width,
                BackColor = Color.White
            };
            bottomPanel.Paint += (s, e) =>
            {
                using var d2d = new D2DGraphics(e.Graphics, bottomPanel.Width, bottomPanel.Height);
                d2d.Clear(Color.White);
                using (Pen divPen = new Pen(Color.FromArgb(228, 233, 240), 1f))
                    d2d.DrawLine(divPen, 0, 0, bottomPanel.Width, 0);
            };

            FluentActionButton btnSave = new FluentActionButton
            {
                Text = AppL10n.T("Lưu & Áp dụng", "Save & Apply"),
                Glyph = '\uE74E',
                Variant = ButtonVariant.Primary,
                Size = new Size(146, 34),
                Location = new Point(this.ClientSize.Width - 282, 12),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => SaveSettings();
            bottomPanel.Controls.Add(btnSave);

            FluentActionButton btnCancel = new FluentActionButton
            {
                Text = AppL10n.T("Hủy bỏ", "Cancel"),
                Glyph = '\uE711',
                Variant = ButtonVariant.Secondary,
                Size = new Size(106, 34),
                Location = new Point(this.ClientSize.Width - 126, 12),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            bottomPanel.Controls.Add(btnCancel);

            _navRail = new FluentNavRail
            {
                Dock = DockStyle.Left,
                Width = 216
            };
            _navRail.AddItem('\uE790', AppL10n.T("Giao diện & Chủ đề", "Appearance & Theme"));
            _navRail.AddItem('\uE74E', AppL10n.T("Lưu trữ & Tự động lưu", "Save & Auto-Save"));
            _navRail.AddItem('\uE70F', AppL10n.T("Soạn thảo & Hiển thị", "Editing & Display"));
            _navRail.AddItem('\uE945', AppL10n.T("Trí tuệ nhân tạo (AI)", "AI Assistant"));
            _navRail.AddItem('\uE71D', AppL10n.T("Mặc định & Lối tắt", "Defaults & Shortcuts"));
            _navRail.AddItem('\uE765', AppL10n.T("Phím tắt & Trợ giúp", "Shortcuts & Help"));

            Panel contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CanvasBg
            };

            // Add Fill -> Left -> Bottom -> Top so Header and Footer span full dialog width
            this.Controls.Add(contentHost);
            this.Controls.Add(_navRail);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(headerPanel);

            _contentPanels = new Panel[6];
            for (int i = 0; i < 6; i++)
            {
                _contentPanels[i] = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = CanvasBg,
                    AutoScroll = true,
                    Visible = (i == 0),
                    Padding = new Padding(22, 16, 22, 16)
                };
                contentHost.Controls.Add(_contentPanels[i]);
            }

            _navRail.SelectedIndexChanged += (s, e) =>
            {
                int sel = _navRail.SelectedIndex;
                for (int i = 0; i < _contentPanels.Length; i++)
                {
                    _contentPanels[i].Visible = (i == sel);
                }
            };

            const int cardW = 528;
            BuildTabAppearance(_contentPanels[0], cardW);
            BuildTabSave(_contentPanels[1], cardW);
            BuildTabEditor(_contentPanels[2], cardW);
            BuildTabAi(_contentPanels[3], cardW);
            BuildTabDefaultApps(_contentPanels[4], cardW);
            BuildTabHelp(_contentPanels[5], cardW);
        }

        private void BuildTabAppearance(Panel p, int cardW)
        {
            int curY = 14;
            p.Controls.Add(new FluentD2DLabel(AppL10n.T("Thiết lập Giao diện & Chủ đề", "Appearance & Theme"), new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold), InkPrimary, 22, curY, cardW, 28));
            curY += 36;

            // Card 1: Ribbon Style
            var cardRibbon = new FluentSettingsCard(AppL10n.T("Kiểu thanh công cụ Ribbon", "Ribbon Style"), 22, curY, cardW, 108);
            var rbGroupRibbon = new List<FluentRadioRow>();
            _rbRibbonIsland = new FluentRadioRow(AppL10n.T("Dạng Đảo (Floating Island Ribbon)", "Floating Island Ribbon"), AppL10n.T("Thiết kế bo góc, nổi khối hiện đại", "Modern rounded floating toolbar"), 14, 40, cardW - 28, rbGroupRibbon);
            _rbRibbonStandard = new FluentRadioRow(AppL10n.T("Tiêu chuẩn (Classic Full-Width Ribbon)", "Classic Full-Width Ribbon"), AppL10n.T("Tràn viền cổ điển quen thuộc", "Traditional edge-to-edge layout"), 14, 72, cardW - 28, rbGroupRibbon);
            _rbRibbonIsland.Checked = true;
            cardRibbon.Controls.AddRange(new Control[] { _rbRibbonIsland, _rbRibbonStandard });
            p.Controls.Add(cardRibbon);
            curY += 120;

            // Card 2: Theme
            var cardTheme = new FluentSettingsCard(AppL10n.T("Chủ đề màu sắc (Theme)", "Color Theme"), 22, curY, cardW, 108);
            var rbGroupTheme = new List<FluentRadioRow>();
            _rbThemeLight = new FluentRadioRow(AppL10n.T("Giao diện Sáng (Light)", "Light Theme"), AppL10n.T("Thanh lịch, tương phản rõ nét ban ngày", "Clean high-contrast daytime look"), 14, 40, cardW - 28, rbGroupTheme);
            _rbThemeDark = new FluentRadioRow(AppL10n.T("Giao diện Tối (Dark)", "Dark Theme"), AppL10n.T("Dịu mắt, phù hợp làm việc ban đêm", "Comfortable for low-light environments"), 14, 72, cardW - 28, rbGroupTheme);
            _rbThemeLight.Checked = true;
            cardTheme.Controls.AddRange(new Control[] { _rbThemeLight, _rbThemeDark });
            p.Controls.Add(cardTheme);
            curY += 120;

            // Card 3: Language
            var cardLang = new FluentSettingsCard(AppL10n.T("Ngôn ngữ hiển thị (Language)", "Display Language"), 22, curY, cardW, 84);
            var rbGroupLang = new List<FluentRadioRow>();
            _rbLangVi = new FluentRadioRow(AppL10n.T("Tiếng Việt (Mặc định)", "Tiếng Việt (Vietnamese)"), null, 14, 40, 220, rbGroupLang);
            _rbLangEn = new FluentRadioRow("English (US)", null, 250, 40, 200, rbGroupLang);
            _rbLangVi.Checked = true;
            cardLang.Controls.AddRange(new Control[] { _rbLangVi, _rbLangEn });
            p.Controls.Add(cardLang);
        }

        private void BuildTabSave(Panel p, int cardW)
        {
            int curY = 14;
            p.Controls.Add(new FluentD2DLabel(AppL10n.T(AppL10n.T("Lưu trữ & Tự động lưu", "Save & Auto-Save"), "Save & Auto-Save"), new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold), InkPrimary, 22, curY, cardW, 28));
            curY += 36;

            var cardSave = new FluentSettingsCard(AppL10n.T("Cơ chế bảo vệ dữ liệu Auto-Save", "Auto-Save Protection"), 22, curY, cardW, 168);

            _chkAutoSave = new FluentToggleRow(AppL10n.T("Bật chế độ tự động lưu ngầm định kỳ (tránh mất dữ liệu)", "Enable periodic background Auto-Save"), 14, 40, cardW - 28, true);
            cardSave.Controls.Add(_chkAutoSave);

            cardSave.Controls.Add(new FluentD2DLabel(AppL10n.T("Khoảng thời gian tự động lưu:", "Auto-save interval:"), new Font("Segoe UI", 9.25f, FontStyle.Regular), InkPrimary, 16, 80, 210, 26));

            _cboAutoSaveInterval = new FluentComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(230, 78),
                Size = new Size(180, 28),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            _cboAutoSaveInterval.Items.AddRange(new object[] { AppL10n.T("1 phút (Khuyến nghị)", "1 minute (Recommended)"), AppL10n.T("3 phút", "3 minutes"), AppL10n.T("5 phút", "5 minutes"), AppL10n.T("10 phút", "10 minutes") });
            _cboAutoSaveInterval.SelectedIndex = 0;
            cardSave.Controls.Add(_cboAutoSaveInterval);

            cardSave.Controls.Add(new FluentD2DLabel(
                AppL10n.T("Thư mục lưu trữ an toàn: %AppData%\\GenOffice\\autosave\\\nCác bản nháp sẽ được tự động phục hồi nếu xảy ra sự cố tắt máy đột ngột.", "Auto-save folder: %AppData%\\GenOffice\\autosave\\\nUnsaved drafts are automatically recovered after unexpected shutdowns."),
                new Font("Segoe UI", 8.5f, FontStyle.Regular), InkSecondary, 16, 118, cardW - 32, 38));

            p.Controls.Add(cardSave);
        }

        private void BuildTabEditor(Panel p, int cardW)
        {
            int curY = 14;
            p.Controls.Add(new FluentD2DLabel(AppL10n.T(AppL10n.T("Soạn thảo & Hiển thị", "Editing & Display"), "Editing & Display"), new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold), InkPrimary, 22, curY, cardW, 28));
            curY += 36;

            var cardDisp = new FluentSettingsCard(AppL10n.T("Tùy chọn hiển thị & Hỗ trợ gõ", "Display & Typing Options"), 22, curY, cardW, 268);

            _chkShowRuler = new FluentToggleRow(AppL10n.T("Hiển thị thước kẻ căn lề (Ruler) trên văn bản", "Show ruler in Writer documents"), 14, 40, cardW - 28, true);
            _chkSmoothScroll = new FluentToggleRow(AppL10n.T("Bật cuộn trang mượt mà 60 FPS (Smooth Scrolling)", "Enable 60 FPS Smooth Scrolling"), 14, 74, cardW - 28, true);
            _chkFormattingMarks = new FluentToggleRow(AppL10n.T("Hiển thị ký tự định dạng ẩn (Paragraph marks ¶, spaces)", "Show formatting marks (¶, spaces)"), 14, 108, cardW - 28, false);
            _chkWordPrediction = new FluentToggleRow(AppL10n.T("Bật gợi ý từ ngữ thông minh (IDE-Style Word Suggestions)", "Enable smart word suggestions"), 14, 142, cardW - 28, false);
            _chkAutoCorrectVi = new FluentToggleRow(AppL10n.T("Tự động hỗ trợ gõ tiếng Việt Telex / VNI chuẩn xác", "Enable Vietnamese Telex / VNI auto-correction"), 14, 176, cardW - 28, false);

            cardDisp.Controls.Add(new FluentD2DLabel(AppL10n.T("Mức thu phóng mặc định:", "Default zoom level:"), new Font("Segoe UI", 9.25f, FontStyle.Regular), InkPrimary, 16, 220, 170, 26));
            _cboDefaultZoom = new FluentComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(192, 218),
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            _cboDefaultZoom.Items.AddRange(new object[] { "100%", "125%", "150%" });
            _cboDefaultZoom.SelectedIndex = 0;

            cardDisp.Controls.AddRange(new Control[] { _chkShowRuler, _chkSmoothScroll, _chkFormattingMarks, _chkWordPrediction, _chkAutoCorrectVi, _cboDefaultZoom });
            p.Controls.Add(cardDisp);
        }

        private void BuildTabAi(Panel p, int cardW)
        {
            int curY = 14;
            p.Controls.Add(new FluentD2DLabel(AppL10n.T("Trí tuệ nhân tạo NextGen AI Copilot", "NextGen AI Copilot"), new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold), InkPrimary, 22, curY, cardW, 28));
            curY += 36;

            var cardAi = new FluentSettingsCard(AppL10n.T("Cấu hình AI Copilot", "AI Copilot Settings"), 22, curY, cardW, 184);

            _chkAiCopilot = new FluentToggleRow(AppL10n.T("Kích hoạt trợ lý ảo NextGen AI Copilot trong toàn bộ ứng dụng", "Enable NextGen AI Copilot across all apps"), 14, 40, cardW - 28, true);
            cardAi.Controls.Add(_chkAiCopilot);

            cardAi.Controls.Add(new FluentD2DLabel(AppL10n.T("Mô hình AI mặc định:", "Default AI model:"), new Font("Segoe UI", 9.25f, FontStyle.Regular), InkPrimary, 16, 82, 160, 26));

            _cboAiProvider = new FluentComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(182, 80),
                Size = new Size(216, 28),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            _cboAiProvider.Items.AddRange(new object[] { "Google Gemini Pro", "OpenAI GPT-4o", "DeepSeek-V3", "Ollama Local (Offline)" });
            _cboAiProvider.SelectedIndex = 0;
            cardAi.Controls.Add(_cboAiProvider);

            FluentActionButton btnApiConfig = new FluentActionButton
            {
                Text = AppL10n.T("Cấu hình Khóa API & Máy chủ AI...", "Configure API Keys & AI Providers..."),
                Glyph = '\uE8D7',
                Variant = ButtonVariant.Tinted,
                Location = new Point(16, 126),
                Size = new Size(276, 36),
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold)
            };
            btnApiConfig.Click += (s, e) =>
            {
                using (var dlg = new AiConfigDialog()) dlg.ShowDialog(this);
            };
            cardAi.Controls.Add(btnApiConfig);

            p.Controls.Add(cardAi);
        }

        private void BuildTabDefaultApps(Panel p, int cardW)
        {
            int curY = 14;
            p.Controls.Add(new FluentD2DLabel(AppL10n.T("Ứng dụng Mặc định & Lối tắt Desktop", "Default Apps & Desktop Shortcuts"), new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold), InkPrimary, 22, curY, cardW, 28));
            curY += 36;

            // Card 1: Windows File Association (fits completely with zero clipping)
            var cardAssoc = new FluentSettingsCard(AppL10n.T("Ứng dụng mặc định khi mở file (Windows File Association)", "Default File Associations"), 22, curY, cardW, 206);

            cardAssoc.Controls.Add(new FluentD2DLabel(
                AppL10n.T("Đăng ký ZenQ Office làm ứng dụng mặc định trong Windows. Khi nhấn đúp vào tệp:\n• Word (.docx, .doc, .rtf)  →  Mở ZenQ Writer riêng biệt (không tab mẹ)\n• Excel (.xlsx, .xls, .csv)  →  Mở ZenQ Calc riêng biệt (không tab mẹ)\n• PowerPoint (.pptx, .ppt)  →  Mở ZenQ Slides riêng biệt (không tab mẹ)\n• PDF (.pdf)  →  Mở ZenQ PDF riêng biệt (không tab mẹ)", "Set ZenQ Office as default handler in Windows. Double-clicking files opens:\n• Word (.docx, .doc, .rtf)  →  ZenQ Writer standalone\n• Excel (.xlsx, .xls, .csv)  →  ZenQ Calc standalone\n• PowerPoint (.pptx, .ppt)  →  ZenQ Slides standalone\n• PDF (.pdf)  →  ZenQ PDF standalone"),
                new Font("Segoe UI", 8.5f, FontStyle.Regular),
                InkSecondary,
                16, 38, cardW - 32, 82));

            _chkRegOnStartup = new FluentToggleRow(AppL10n.T("Tự động duy trì đăng ký định dạng tệp với Windows mỗi khi khởi động", "Keep Windows file associations registered on startup"), 14, 124, cardW - 28, false);
            cardAssoc.Controls.Add(_chkRegOnStartup);

            FluentActionButton btnSetDefault = new FluentActionButton
            {
                Text = AppL10n.T("Đặt ZenQ Office làm mặc định ngay", "Set ZenQ Office as Default Now"),
                Glyph = '\uE71B',
                Variant = ButtonVariant.Primary,
                Location = new Point(16, 160),
                Size = new Size(270, 34),
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold)
            };
            btnSetDefault.Click += (s, e) =>
            {
                try
                {
                    FileAssociationManager.RegisterAllFileAssociations();
                    FluentMessageBox.Show(this, AppL10n.T("Đã đăng ký ZenQ Office làm ứng dụng mặc định cho Word, Excel, PowerPoint và PDF thành công!", "Registered ZenQ Office as default app for Word, Excel, PowerPoint, and PDF!"), "Đăng ký mặc định thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    FluentMessageBox.Show(this, AppL10n.T("Không thể đăng ký: ", "Registration error: ") + ex.Message, AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            cardAssoc.Controls.Add(btnSetDefault);

            FluentActionButton btnOpenWinSettings = new FluentActionButton
            {
                Text = AppL10n.T("Mở Cài đặt Windows...", "Open Windows Settings..."),
                Glyph = '\uE713',
                Variant = ButtonVariant.Secondary,
                Location = new Point(296, 160),
                Size = new Size(192, 34),
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular)
            };
            btnOpenWinSettings.Click += (s, e) => FileAssociationManager.OpenWindowsDefaultAppsSettings();
            cardAssoc.Controls.Add(btnOpenWinSettings);

            p.Controls.Add(cardAssoc);
            curY += 216;

            // Card 2: Desktop Shortcuts (fits completely with zero clipping)
            var cardDesktop = new FluentSettingsCard(AppL10n.T("Biểu tượng lối tắt ngoài Màn hình chính (Desktop Shortcuts)", "Desktop Shortcuts"), 22, curY, cardW, 174);

            cardDesktop.Controls.Add(new FluentD2DLabel(
                AppL10n.T("• ZenQ Office (App mẹ): Mở Hub trung tâm đầy đủ Trang chủ và chia tab đa tài liệu.\n• App nhánh (Writer, Calc, Slides, PDF): Mở thẳng từng ứng dụng riêng biệt, không có tab mẹ.", "• ZenQ Office (Hub): Opens the main suite hub with Home & tabs.\n• Standalone apps (Writer, Calc, Slides, PDF): Launch each editor directly."),
                new Font("Segoe UI", 8.5f, FontStyle.Regular),
                InkSecondary,
                16, 38, cardW - 32, 38));

            FluentActionButton btnCreateAll = new FluentActionButton
            {
                Text = AppL10n.T("Tạo ĐẦY ĐỦ lối tắt ra Desktop (App mẹ + 4 App nhánh)", "Create ALL Desktop Shortcuts (Hub + 4 Apps)"),
                Glyph = '\uE718',
                Variant = ButtonVariant.Primary,
                Location = new Point(16, 84),
                Size = new Size(cardW - 32, 34),
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold)
            };
            btnCreateAll.Click += (s, e) =>
            {
                try
                {
                    DesktopShortcutManager.CreateAllShortcuts();
                    FluentMessageBox.Show(this, AppL10n.T("Đã tạo đầy đủ 5 lối tắt ngoài Desktop (ZenQ Office, Writer, Calc, Slides, PDF)!", "Created all 5 Desktop shortcuts (ZenQ Office, Writer, Calc, Slides, PDF)!"), AppL10n.T("Tạo lối tắt thành công", "Shortcuts Created"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    FluentMessageBox.Show(this, AppL10n.T("Không thể tạo lối tắt: ", "Could not create shortcuts: ") + ex.Message, AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            cardDesktop.Controls.Add(btnCreateAll);

            int halfBtnW = (cardW - 44) / 2;
            FluentActionButton btnCreateHubOnly = new FluentActionButton
            {
                Text = AppL10n.T("Chỉ tạo icon App mẹ (ZenQ Office)", "Hub Icon Only (ZenQ Office)"),
                Glyph = '\uE80F',
                Variant = ButtonVariant.Secondary,
                Location = new Point(16, 126),
                Size = new Size(halfBtnW, 34),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            btnCreateHubOnly.Click += (s, e) =>
            {
                try
                {
                    DesktopShortcutManager.CreateMainHubShortcut();
                    FluentMessageBox.Show(this, AppL10n.T("Đã tạo lối tắt 'ZenQ Office' (App mẹ) ra Desktop!", "Created 'ZenQ Office' shortcut on Desktop!"), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    FluentMessageBox.Show(this, AppL10n.T("Không thể tạo lối tắt: ", "Could not create shortcuts: ") + ex.Message, AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            cardDesktop.Controls.Add(btnCreateHubOnly);

            FluentActionButton btnCreateBranchesOnly = new FluentActionButton
            {
                Text = AppL10n.T("Chỉ tạo 4 icon App nhánh riêng", "Create 4 Standalone App Icons"),
                Glyph = '\uE838',
                Variant = ButtonVariant.Secondary,
                Location = new Point(28 + halfBtnW, 126),
                Size = new Size(halfBtnW, 34),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            btnCreateBranchesOnly.Click += (s, e) =>
            {
                try
                {
                    DesktopShortcutManager.CreateAllBranchShortcuts();
                    FluentMessageBox.Show(this, AppL10n.T("Đã tạo 4 lối tắt App nhánh (Writer, Calc, Slides, PDF) ra Desktop!", "Created 4 app shortcuts (Writer, Calc, Slides, PDF) on Desktop!"), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    FluentMessageBox.Show(this, AppL10n.T("Không thể tạo lối tắt: ", "Could not create shortcuts: ") + ex.Message, AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            cardDesktop.Controls.Add(btnCreateBranchesOnly);

            p.Controls.Add(cardDesktop);
        }

        private void BuildTabHelp(Panel p, int cardW)
        {
            int curY = 14;
            p.Controls.Add(new FluentD2DLabel(AppL10n.T("Phím tắt nhanh & Thông tin Suite", "Keyboard Shortcuts & About"), new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold), InkPrimary, 22, curY, cardW, 28));
            curY += 36;

            var cardShortcuts = new FluentSettingsCard(AppL10n.T("Tra cứu phím tắt bàn phím", "Keyboard Shortcuts Reference"), 22, curY, cardW, 120);
            cardShortcuts.Controls.Add(new FluentD2DLabel(
                AppL10n.T("Tra cứu danh mục toàn bộ phím tắt Microsoft Word / Excel / PowerPoint chuẩn quốc tế.", "Browse all Microsoft Word, Excel, and PowerPoint compatible shortcuts."),
                new Font("Segoe UI", 8.75f, FontStyle.Regular), InkSecondary, 16, 40, cardW - 32, 24));

            FluentActionButton btnOpenShortcuts = new FluentActionButton
            {
                Text = AppL10n.T("Mở Bảng Tổng Hợp Phím Tắt", "Open Shortcuts Reference"),
                Glyph = '\uE765',
                Variant = ButtonVariant.Tinted,
                Location = new Point(16, 72),
                Size = new Size(256, 34),
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold)
            };
            btnOpenShortcuts.Click += (s, e) =>
            {
                using (var sf = new ShortcutsHelpForm()) sf.ShowDialog(this);
            };
            cardShortcuts.Controls.Add(btnOpenShortcuts);
            p.Controls.Add(cardShortcuts);
            curY += 132;

            var cardAbout = new FluentSettingsCard(AppL10n.T("Thông tin bản quyền & Đơn vị phát triển", "About & Credits"), 22, curY, cardW, 114);
            cardAbout.Controls.Add(new FluentD2DLabel(
                AppL10n.T("ZENQ OFFICE - PHIÊN BẢN 6.0 (BUILD 2026)\nPhát triển bởi: NEXTGENEDUSPACE STUDIO • Trương Nguyễn Thế Danh\nTối ưu cho Windows 10/11 với hiệu năng kết xuất Direct2D & DirectWrite.", "ZENQ OFFICE - VERSION 6.0 (BUILD 2026)\nDeveloped by: NEXTGENEDUSPACE STUDIO • Truong Nguyen The Danh\nOptimized for Windows 10/11 with Direct2D & DirectWrite rendering."),
                new Font("Segoe UI", 8.75f, FontStyle.Regular), InkSecondary, 16, 42, cardW - 32, 58));
            p.Controls.Add(cardAbout);
        }

        private void LoadCurrentSettings()
        {
            var s = AppSettings.Instance;

            if (s.RibbonStyle == RibbonStyle.Standard) _rbRibbonStandard.Checked = true;
            else _rbRibbonIsland.Checked = true;

            if (s.Theme == AppTheme.Dark) _rbThemeDark.Checked = true;
            else _rbThemeLight.Checked = true;

            if (s.Language == AppLanguage.English) _rbLangEn.Checked = true;
            else _rbLangVi.Checked = true;

            _chkAutoSave.Checked = s.AutoSaveEnabled;
            if (s.AutoSaveIntervalSeconds <= 60) _cboAutoSaveInterval.SelectedIndex = 0;
            else if (s.AutoSaveIntervalSeconds <= 180) _cboAutoSaveInterval.SelectedIndex = 1;
            else if (s.AutoSaveIntervalSeconds <= 300) _cboAutoSaveInterval.SelectedIndex = 2;
            else _cboAutoSaveInterval.SelectedIndex = 3;

            _chkShowRuler.Checked = s.ShowRuler;
            _chkSmoothScroll.Checked = s.EnableSmoothScroll;
            _chkFormattingMarks.Checked = s.ShowFormattingMarks;
            _chkWordPrediction.Checked = s.WordPredictionEnabled;
            _chkAutoCorrectVi.Checked = s.AutoCorrectVietnamese;

            if (s.DefaultZoom >= 1.45f) _cboDefaultZoom.SelectedIndex = 2;
            else if (s.DefaultZoom >= 1.20f) _cboDefaultZoom.SelectedIndex = 1;
            else _cboDefaultZoom.SelectedIndex = 0;

            _chkAiCopilot.Checked = s.AiCopilotEnabled;
            _cboAiProvider.SelectedIndex = Math.Max(0, Math.Min(3, s.SelectedAiProvider));

            _chkRegOnStartup.Checked = s.RegisterFileAssociationsOnStartup;
        }

        private void SaveSettings()
        {
            var s = AppSettings.Instance;

            s.RibbonStyle = _rbRibbonStandard.Checked ? RibbonStyle.Standard : RibbonStyle.Island;
            s.Theme = _rbThemeDark.Checked ? AppTheme.Dark : AppTheme.Light;
            s.Language = _rbLangEn.Checked ? AppLanguage.English : AppLanguage.Vietnamese;

            s.AutoSaveEnabled = _chkAutoSave.Checked;
            switch (_cboAutoSaveInterval.SelectedIndex)
            {
                case 1: s.AutoSaveIntervalSeconds = 180; break;
                case 2: s.AutoSaveIntervalSeconds = 300; break;
                case 3: s.AutoSaveIntervalSeconds = 600; break;
                default: s.AutoSaveIntervalSeconds = 60; break;
            }

            s.ShowRuler = _chkShowRuler.Checked;
            s.EnableSmoothScroll = _chkSmoothScroll.Checked;
            s.ShowFormattingMarks = _chkFormattingMarks.Checked;
            s.WordPredictionEnabled = _chkWordPrediction.Checked;
            s.AutoCorrectVietnamese = _chkAutoCorrectVi.Checked;

            switch (_cboDefaultZoom.SelectedIndex)
            {
                case 1: s.DefaultZoom = 1.25f; break;
                case 2: s.DefaultZoom = 1.50f; break;
                default: s.DefaultZoom = 1.0f; break;
            }

            s.AiCopilotEnabled = _chkAiCopilot.Checked;
            s.SelectedAiProvider = _cboAiProvider.SelectedIndex;
            s.RegisterFileAssociationsOnStartup = _chkRegOnStartup.Checked;

            if (s.RegisterFileAssociationsOnStartup)
            {
                try { FileAssociationManager.RegisterAllFileAssociations(); } catch { }
            }

            s.Save();
            s.NotifyChanged();

            if (_appForm != null)
            {
                _appForm.StatusText = AppL10n.T("Đã lưu và áp dụng cài đặt ZenQ Office thành công!", "Saved and applied ZenQ Office settings!");
                _appForm.Invalidate();
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // ==================== PURE DIRECT2D / DIRECTWRITE CONTROLS ====================

        private enum ButtonVariant { Primary, Secondary, Tinted }

        private sealed class FluentD2DLabel : Control
        {
            public FluentD2DLabel(string text, Font font, Color color, int x, int y, int w, int h)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
                Text = text;
                Font = font;
                ForeColor = color;
                Location = new Point(x, y);
                Size = new Size(w, h);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Color bg = Parent?.BackColor ?? Color.White;
                if (bg == Color.Transparent) bg = Color.White;
                using var d2d = new D2DGraphics(e.Graphics, Width, Height);
                d2d.Clear(bg);
                using var brush = new SolidBrush(ForeColor);
                using var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                d2d.DrawString(Text ?? "", Font, brush, new RectangleF(0, 0, Width, Height), sf);
            }
        }

        private sealed class FluentSettingsCard : Panel
        {
            private readonly string _title;

            public FluentSettingsCard(string title, int x, int y, int w, int h)
            {
                _title = title;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Location = new Point(x, y);
                Size = new Size(w, h);
                BackColor = CardBg;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var d2d = new D2DGraphics(e.Graphics, Width, Height);
                d2d.Clear(Parent?.BackColor ?? CanvasBg);

                RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                using (SolidBrush fill = new SolidBrush(CardBg))
                using (Pen border = new Pen(CardBorder, 1f))
                {
                    d2d.FillRoundedRectangle(fill, rect, 8f);
                    d2d.DrawRoundedRectangle(border, rect, 8f);
                }

                if (!string.IsNullOrEmpty(_title))
                {
                    using (Font hf = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold))
                    using (SolidBrush hb = new SolidBrush(InkPrimary))
                    {
                        d2d.DrawString(_title, hf, hb, new RectangleF(16f, 8f, Width - 32f, 22f));
                    }
                    using (Pen sep = new Pen(Color.FromArgb(241, 245, 249), 1f))
                    {
                        d2d.DrawLine(sep, 14f, 32f, Width - 14f, 32f);
                    }
                }
            }
        }

        private sealed class FluentToggleRow : Control
        {
            private bool _checked;
            private bool _hovered;

            public bool Checked
            {
                get => _checked;
                set
                {
                    if (_checked != value)
                    {
                        _checked = value;
                        Invalidate();
                        CheckedChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CheckedChanged;

            public FluentToggleRow(string text, int x, int y, int w, bool initialChecked)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Text = text;
                Location = new Point(x, y);
                Size = new Size(w, 28);
                _checked = initialChecked;
                Cursor = Cursors.Hand;
                BackColor = CardBg;
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            }

            protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var d2d = new D2DGraphics(e.Graphics, Width, Height);
                d2d.Clear(CardBg);

                if (_hovered)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(246, 248, 252)))
                        d2d.FillRoundedRectangle(hb, new RectangleF(0, 0, Width - 1, Height - 1), 6f);
                }

                // Windows 11 Fluent Toggle Switch Pill on left
                float trackW = 34f, trackH = 18f;
                float trackX = 4f, trackY = (Height - trackH) / 2f;
                RectangleF trackRect = new RectangleF(trackX, trackY, trackW, trackH);

                if (_checked)
                {
                    using (SolidBrush tb = new SolidBrush(_hovered ? AccentBlueHover : AccentBlue))
                        d2d.FillRoundedRectangle(tb, trackRect, trackH / 2f);
                    using (SolidBrush knob = new SolidBrush(Color.White))
                        d2d.FillEllipse(knob, new RectangleF(trackX + trackW - 14.5f, trackY + 3.5f, 11f, 11f));
                }
                else
                {
                    using (SolidBrush tb = new SolidBrush(Color.White))
                    using (Pen tp = new Pen(Color.FromArgb(130, 140, 154), 1.3f))
                    {
                        d2d.FillRoundedRectangle(tb, trackRect, trackH / 2f);
                        d2d.DrawRoundedRectangle(tp, trackRect, trackH / 2f);
                    }
                    using (SolidBrush knob = new SolidBrush(Color.FromArgb(95, 106, 122)))
                        d2d.FillEllipse(knob, new RectangleF(trackX + 3.5f, trackY + 3.5f, 11f, 11f));
                }

                using (SolidBrush txtB = new SolidBrush(InkPrimary))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    d2d.DrawString(Text ?? "", Font, txtB, new RectangleF(46f, 0f, Width - 50f, Height), sf);
                }
            }
        }

        private sealed class FluentRadioRow : Control
        {
            private readonly string _subtitle;
            private readonly List<FluentRadioRow> _group;
            private bool _checked;
            private bool _hovered;

            public bool Checked
            {
                get => _checked;
                set
                {
                    if (value && _group != null)
                    {
                        foreach (var r in _group)
                        {
                            if (r != this && r._checked)
                            {
                                r._checked = false;
                                r.Invalidate();
                            }
                        }
                    }
                    if (_checked != value)
                    {
                        _checked = value;
                        Invalidate();
                        CheckedChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CheckedChanged;

            public FluentRadioRow(string title, string subtitle, int x, int y, int w, List<FluentRadioRow> group)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Text = title;
                _subtitle = subtitle;
                _group = group;
                _group?.Add(this);
                Location = new Point(x, y);
                Size = new Size(w, string.IsNullOrEmpty(subtitle) ? 28 : 30);
                Cursor = Cursors.Hand;
                BackColor = CardBg;
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            }

            protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnClick(EventArgs e) { Checked = true; base.OnClick(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                SizeF measuredTitle = SizeF.Empty;
                if (!string.IsNullOrEmpty(_subtitle))
                {
                    using (Font tfMeasure = new Font("Segoe UI", 9.0f, _checked ? FontStyle.Bold : FontStyle.Regular))
                    {
                        measuredTitle = e.Graphics.MeasureString(Text ?? "", tfMeasure);
                    }
                }

                using var d2d = new D2DGraphics(e.Graphics, Width, Height);
                d2d.Clear(CardBg);

                if (_hovered)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(246, 248, 252)))
                        d2d.FillRoundedRectangle(hb, new RectangleF(0, 0, Width - 1, Height - 1), 6f);
                }

                float rSize = 17f;
                float rx = 4f, ry = (Height - rSize) / 2f;
                RectangleF ring = new RectangleF(rx, ry, rSize, rSize);

                if (_checked)
                {
                    using (SolidBrush fill = new SolidBrush(AccentBlue))
                        d2d.FillEllipse(fill, ring);
                    using (SolidBrush innerDot = new SolidBrush(Color.White))
                        d2d.FillEllipse(innerDot, new RectangleF(rx + 5f, ry + 5f, 7f, 7f));
                }
                else
                {
                    using (SolidBrush fill = new SolidBrush(Color.White))
                    using (Pen pen = new Pen(Color.FromArgb(148, 163, 184), 1.3f))
                    {
                        d2d.FillEllipse(fill, ring);
                        d2d.DrawEllipse(pen, ring);
                    }
                }

                using (SolidBrush titleB = new SolidBrush(InkPrimary))
                using (Font tf = new Font("Segoe UI", 9.0f, _checked ? FontStyle.Bold : FontStyle.Regular))
                {
                    if (string.IsNullOrEmpty(_subtitle))
                    {
                        using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                            d2d.DrawString(Text ?? "", tf, titleB, new RectangleF(28f, 0f, Width - 32f, Height), sf);
                    }
                    else
                    {
                        d2d.DrawString(Text ?? "", tf, titleB, new RectangleF(28f, (Height - 18f) / 2f, measuredTitle.Width + 6f, 20f));
                        using (Font sfFont = new Font("Segoe UI", 8.25f, FontStyle.Regular))
                        using (SolidBrush subB = new SolidBrush(InkSecondary))
                        using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                        {
                            float sx = 28f + measuredTitle.Width + 8f;
                            if (sx < Width - 20f)
                                d2d.DrawString("— " + _subtitle, sfFont, subB, new RectangleF(sx, 0f, Width - sx - 4f, Height), sf);
                        }
                    }
                }
            }
        }

        private sealed class FluentActionButton : Control
        {
            public char Glyph { get; set; }
            public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;
            private bool _hovered;
            private bool _pressed;

            public FluentActionButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                SizeF txtSize = e.Graphics.MeasureString(Text ?? "", Font);

                using var d2d = new D2DGraphics(e.Graphics, Width, Height);
                d2d.Clear(Parent?.BackColor ?? Color.White);

                Color bg, border, fg;
                if (Variant == ButtonVariant.Primary)
                {
                    bg = _pressed ? Color.FromArgb(12, 82, 146) : (_hovered ? AccentBlueHover : AccentBlue);
                    border = bg;
                    fg = Color.White;
                }
                else if (Variant == ButtonVariant.Tinted)
                {
                    bg = _pressed ? Color.FromArgb(215, 232, 250) : (_hovered ? Color.FromArgb(225, 238, 252) : AccentSoftBg);
                    border = AccentSoftBorder;
                    fg = AccentBlue;
                }
                else
                {
                    bg = _pressed ? Color.FromArgb(236, 240, 245) : (_hovered ? Color.FromArgb(248, 250, 252) : Color.White);
                    border = Color.FromArgb(203, 213, 225);
                    fg = InkPrimary;
                }

                RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                using (SolidBrush fill = new SolidBrush(bg))
                using (Pen pen = new Pen(border, 1f))
                {
                    d2d.FillRoundedRectangle(fill, rect, 6f);
                    if (Variant != ButtonVariant.Primary) d2d.DrawRoundedRectangle(pen, rect, 6f);
                }

                bool hasGlyph = Glyph != '\0';
                float contentW = (hasGlyph ? 20f : 0f) + txtSize.Width;
                float startX = Math.Max(8f, (Width - contentW) / 2f);

                if (hasGlyph)
                {
                    FluentIconRenderer.DrawGlyph(d2d, Glyph, new RectangleF(startX, 0f, 18f, Height), fg, 13f);
                    startX += 21f;
                }

                using (SolidBrush txtB = new SolidBrush(fg))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    d2d.DrawString(Text ?? "", Font, txtB, new RectangleF(startX, 0f, Width - startX - 6f, Height), sf);
                }
            }
        }

        private sealed class FluentNavRail : Control
        {
            private readonly List<(char Glyph, string Label)> _items = new();
            private int _selectedIndex = 0;
            private int _hoveredIndex = -1;
            private const int TopPad = 14;
            private const int RowH = 38;
            private const int GapY = 4;

            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    if (value >= 0 && value < _items.Count && _selectedIndex != value)
                    {
                        _selectedIndex = value;
                        Invalidate();
                        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler SelectedIndexChanged;

            public FluentNavRail()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                BackColor = CanvasBg;
            }

            public void AddItem(char glyph, string label) => _items.Add((glyph, label));

            private int HitTestIndex(int y)
            {
                int rel = y - TopPad;
                if (rel < 0) return -1;
                int stride = RowH + GapY;
                int idx = rel / stride;
                if (idx >= _items.Count) return -1;
                if ((rel % stride) > RowH) return -1;
                return idx;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int idx = HitTestIndex(e.Y);
                if (idx != _hoveredIndex)
                {
                    _hoveredIndex = idx;
                    Invalidate();
                }
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hoveredIndex = -1;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    int idx = HitTestIndex(e.Y);
                    if (idx >= 0) SelectedIndex = idx;
                }
                base.OnMouseDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var d2d = new D2DGraphics(e.Graphics, Width, Height);
                d2d.Clear(CanvasBg);

                using (Pen rightDiv = new Pen(Color.FromArgb(228, 233, 240), 1f))
                    d2d.DrawLine(rightDiv, Width - 1, 0, Width - 1, Height);

                for (int i = 0; i < _items.Count; i++)
                {
                    int y = TopPad + i * (RowH + GapY);
                    RectangleF pillRect = new RectangleF(10f, y, Width - 20f, RowH);
                    bool sel = (i == _selectedIndex);
                    bool hov = (i == _hoveredIndex);

                    if (sel || hov)
                    {
                        Color pillBg = sel ? Color.FromArgb(232, 241, 252) : Color.FromArgb(235, 239, 245);
                        using (SolidBrush pb = new SolidBrush(pillBg))
                            d2d.FillRoundedRectangle(pb, pillRect, 6f);
                    }

                    if (sel)
                    {
                        using (SolidBrush barB = new SolidBrush(AccentBlue))
                            d2d.FillRoundedRectangle(barB, new RectangleF(10f, y + 9f, 3.5f, RowH - 18f), 2f);
                    }

                    Color fg = sel ? AccentBlue : Color.FromArgb(51, 65, 85);
                    FluentIconRenderer.DrawGlyph(d2d, _items[i].Glyph, new RectangleF(20f, y, 20f, RowH), fg, 15f);

                    using (Font f = new Font("Segoe UI", 9.25f, sel ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush tb = new SolidBrush(fg))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                    {
                        d2d.DrawString(_items[i].Label, f, tb, new RectangleF(46f, y, Width - 58f, RowH), sf);
                    }
                }
            }
        }
    }
}

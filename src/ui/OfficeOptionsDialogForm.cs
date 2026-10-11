using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class OfficeOptionsDialogForm : Form
    {
        private readonly AppForm _appForm;

        // UI Controls - Giao diện
        private RadioButton _rbRibbonIsland;
        private RadioButton _rbRibbonStandard;
        private RadioButton _rbThemeLight;
        private RadioButton _rbThemeDark;
        private RadioButton _rbLangVi;
        private RadioButton _rbLangEn;

        // UI Controls - Lưu trữ
        private CheckBox _chkAutoSave;
        private ComboBox _cboAutoSaveInterval;

        // UI Controls - Soạn thảo & Hiển thị
        private CheckBox _chkShowRuler;
        private CheckBox _chkSmoothScroll;
        private CheckBox _chkFormattingMarks;
        private CheckBox _chkWordPrediction;
        private CheckBox _chkAutoCorrectVi;
        private ComboBox _cboDefaultZoom;

        // UI Controls - AI
        private CheckBox _chkAiCopilot;
        private ComboBox _cboAiProvider;

        // Navigation Panels
        private ListBox _navListBox;
        private Panel[] _contentPanels;

        public OfficeOptionsDialogForm(AppForm appForm)
        {
            _appForm = appForm;
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Tùy chọn - ZENQ OFFICE Suite 3.5";
            this.Size = new Size(740, 530);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

            // 1. Header Banner
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(37, 99, 235) // Modern Royal Blue
            };
            this.Controls.Add(headerPanel);

            Label lblHeaderTitle = new Label
            {
                Text = "⚙ TÙY CHỌN HỆ THỐNG VÀ THIẾT LẬP ỨNG DỤNG",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 10),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblHeaderTitle);

            Label lblHeaderSub = new Label
            {
                Text = "Tùy biến giao diện thanh công cụ, cơ chế tự động lưu, hiển thị và trợ lý AI NextGen",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(219, 234, 254),
                Location = new Point(21, 34),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblHeaderSub);

            // 2. Bottom Actions Panel
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            this.Controls.Add(bottomPanel);

            Button btnSave = new Button
            {
                Text = "Lưu & Áp dụng",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 36),
                Location = new Point(bottomPanel.Width - 275, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveSettings();
            bottomPanel.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 36),
                Location = new Point(bottomPanel.Width - 130, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnCancel);

            // 3. Left Navigation Rail
            _navListBox = new ListBox
            {
                Dock = DockStyle.Left,
                Width = 190,
                Font = new Font("Segoe UI", 10.0f, FontStyle.Regular),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.None,
                ItemHeight = 36,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _navListBox.Items.Add("🎨 Giao diện & Chủ đề");
            _navListBox.Items.Add("💾 Lưu trữ & Tự động lưu");
            _navListBox.Items.Add("📐 Soạn thảo & Hiển thị");
            _navListBox.Items.Add("🤖 Trí tuệ nhân tạo (AI)");
            _navListBox.Items.Add("⌨ Phím tắt & Trợ giúp");
            _navListBox.SelectedIndex = 0;

            _navListBox.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) != 0;
                Color bg = isSelected ? Color.FromArgb(224, 231, 255) : Color.FromArgb(248, 250, 252);
                Color fg = isSelected ? Color.FromArgb(37, 99, 235) : Color.FromArgb(51, 65, 85);
                using (SolidBrush b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);
                if (isSelected)
                {
                    using (SolidBrush bar = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    {
                        e.Graphics.FillRectangle(bar, e.Bounds.X, e.Bounds.Y + 4, 4, e.Bounds.Height - 8);
                    }
                }
                using (SolidBrush txtBr = new SolidBrush(fg))
                using (Font itemF = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                {
                    e.Graphics.DrawString(_navListBox.Items[e.Index].ToString(), itemF, txtBr, e.Bounds.X + 16, e.Bounds.Y + 8);
                }
            };

            this.Controls.Add(_navListBox);

            // 4. Content Area
            Panel contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            this.Controls.Add(contentHost);
            contentHost.BringToFront();

            _contentPanels = new Panel[5];
            for (int i = 0; i < 5; i++)
            {
                _contentPanels[i] = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    AutoScroll = true,
                    Visible = (i == 0)
                };
                contentHost.Controls.Add(_contentPanels[i]);
            }

            _navListBox.SelectedIndexChanged += (s, e) =>
            {
                int sel = _navListBox.SelectedIndex;
                for (int i = 0; i < _contentPanels.Length; i++)
                {
                    _contentPanels[i].Visible = (i == sel);
                }
            };

            BuildTabAppearance(_contentPanels[0]);
            BuildTabSave(_contentPanels[1]);
            BuildTabEditor(_contentPanels[2]);
            BuildTabAi(_contentPanels[3]);
            BuildTabHelp(_contentPanels[4]);
        }

        private void BuildTabAppearance(Panel p)
        {
            int curY = 16;

            Label lblTitle = new Label { Text = "Thiết lập Giao diện & Chủ đề", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, curY), AutoSize = true };
            p.Controls.Add(lblTitle);
            curY += 36;

            // Group 1: Ribbon Style
            GroupBox grpRibbon = new GroupBox { Text = "Kiểu thanh công cụ Ribbon", Location = new Point(10, curY), Size = new Size(470, 85), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _rbRibbonIsland = new RadioButton { Text = "Dạng Đảo (Floating Island Ribbon - Thiết kế hiện đại bo góc, nổi khối)", Location = new Point(16, 26), Size = new Size(440, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            _rbRibbonStandard = new RadioButton { Text = "Tiêu chuẩn (Classic Full-Width Ribbon - Tràn viền cổ điển chuẩn Office)", Location = new Point(16, 52), Size = new Size(440, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            grpRibbon.Controls.Add(_rbRibbonIsland);
            grpRibbon.Controls.Add(_rbRibbonStandard);
            p.Controls.Add(grpRibbon);
            curY += 95;

            // Group 2: Theme
            GroupBox grpTheme = new GroupBox { Text = "Chủ đề màu sắc (Theme)", Location = new Point(10, curY), Size = new Size(470, 85), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _rbThemeLight = new RadioButton { Text = "Giao diện Sáng (Light - Thanh lịch, sáng sủa)", Location = new Point(16, 26), Size = new Size(440, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            _rbThemeDark = new RadioButton { Text = "Giao diện Tối (Dark - Dịu mắt, làm việc ban đêm)", Location = new Point(16, 52), Size = new Size(440, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            grpTheme.Controls.Add(_rbThemeLight);
            grpTheme.Controls.Add(_rbThemeDark);
            p.Controls.Add(grpTheme);
            curY += 95;

            // Group 3: Language
            GroupBox grpLang = new GroupBox { Text = "Ngôn ngữ hiển thị (Language)", Location = new Point(10, curY), Size = new Size(470, 75), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _rbLangVi = new RadioButton { Text = "Tiếng Việt (Mặc định)", Location = new Point(16, 26), Size = new Size(180, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            _rbLangEn = new RadioButton { Text = "English (US)", Location = new Point(210, 26), Size = new Size(180, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            grpLang.Controls.Add(_rbLangVi);
            grpLang.Controls.Add(_rbLangEn);
            p.Controls.Add(grpLang);
        }

        private void BuildTabSave(Panel p)
        {
            int curY = 16;
            Label lblTitle = new Label { Text = "Thiết lập Lưu trữ & Tự động lưu", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, curY), AutoSize = true };
            p.Controls.Add(lblTitle);
            curY += 36;

            GroupBox grpSave = new GroupBox { Text = "Cơ chế bảo vệ dữ liệu Auto-Save", Location = new Point(10, curY), Size = new Size(470, 160), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };

            _chkAutoSave = new CheckBox { Text = "Bật chế độ tự động lưu ngầm định kỳ (Tránh mất mát dữ liệu)", Location = new Point(16, 30), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            grpSave.Controls.Add(_chkAutoSave);

            Label lblInt = new Label { Text = "Khoảng thời gian tự động lưu:", Location = new Point(16, 68), Size = new Size(190, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            grpSave.Controls.Add(lblInt);

            _cboAutoSaveInterval = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(210, 65), Size = new Size(150, 26), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            _cboAutoSaveInterval.Items.AddRange(new object[] { "1 phút (Khuyến nghị)", "3 phút", "5 phút", "10 phút" });
            _cboAutoSaveInterval.SelectedIndex = 0;
            grpSave.Controls.Add(_cboAutoSaveInterval);

            Label lblPath = new Label
            {
                Text = "Thư mục lưu trữ an toàn: %AppData%\\GenOffice\\autosave\\\nCác bản nháp sẽ được tự động phục hồi nếu sự cố tắt máy xảy ra.",
                Location = new Point(16, 105),
                Size = new Size(430, 42),
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            grpSave.Controls.Add(lblPath);

            p.Controls.Add(grpSave);
        }

        private void BuildTabEditor(Panel p)
        {
            int curY = 16;
            Label lblTitle = new Label { Text = "Thiết lập Soạn thảo & Hiển thị", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, curY), AutoSize = true };
            p.Controls.Add(lblTitle);
            curY += 36;

            GroupBox grpDisp = new GroupBox { Text = "Tùy chọn hiển thị & Tương tác", Location = new Point(10, curY), Size = new Size(470, 260), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };

            _chkShowRuler = new CheckBox { Text = "Hiển thị thước kẻ căn lề (Ruler) trên văn bản", Location = new Point(16, 28), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            _chkSmoothScroll = new CheckBox { Text = "Bật cuộn trang siêu mượt mà 60 FPS (Smooth Scrolling)", Location = new Point(16, 58), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            _chkFormattingMarks = new CheckBox { Text = "Hiển thị ký tự định dạng ẩn (Paragraph marks ¶, spaces)", Location = new Point(16, 88), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            _chkWordPrediction = new CheckBox { Text = "Bật hộp gợi ý từ ngữ thông minh (IDE-Style Word Suggestions)", Location = new Point(16, 118), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            _chkAutoCorrectVi = new CheckBox { Text = "Tự động hỗ trợ gõ tiếng Việt Telex/VNI chuẩn xác", Location = new Point(16, 148), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };

            Label lblZoom = new Label { Text = "Mức thu phóng mặc định:", Location = new Point(16, 186), Size = new Size(160, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            _cboDefaultZoom = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(180, 183), Size = new Size(120, 26), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            _cboDefaultZoom.Items.AddRange(new object[] { "100%", "125%", "150%" });
            _cboDefaultZoom.SelectedIndex = 0;

            grpDisp.Controls.Add(_chkShowRuler);
            grpDisp.Controls.Add(_chkSmoothScroll);
            grpDisp.Controls.Add(_chkFormattingMarks);
            grpDisp.Controls.Add(_chkWordPrediction);
            grpDisp.Controls.Add(_chkAutoCorrectVi);
            grpDisp.Controls.Add(lblZoom);
            grpDisp.Controls.Add(_cboDefaultZoom);

            p.Controls.Add(grpDisp);
        }

        private void BuildTabAi(Panel p)
        {
            int curY = 16;
            Label lblTitle = new Label { Text = "Trí tuệ nhân tạo NextGen AI Copilot", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, curY), AutoSize = true };
            p.Controls.Add(lblTitle);
            curY += 36;

            GroupBox grpAi = new GroupBox { Text = "Cấu hình AI Copilot", Location = new Point(10, curY), Size = new Size(470, 200), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };

            _chkAiCopilot = new CheckBox { Text = "Kích hoạt trợ lý ảo NextGen AI Copilot trong toàn bộ ứng dụng", Location = new Point(16, 30), Size = new Size(430, 24), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Checked = true };
            grpAi.Controls.Add(_chkAiCopilot);

            Label lblProv = new Label { Text = "Mô hình AI mặc định:", Location = new Point(16, 70), Size = new Size(150, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            grpAi.Controls.Add(lblProv);

            _cboAiProvider = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(170, 67), Size = new Size(200, 26), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
            _cboAiProvider.Items.AddRange(new object[] { "Google Gemini Pro", "OpenAI GPT-4o", "DeepSeek-V3", "Ollama Local (Offline)" });
            _cboAiProvider.SelectedIndex = 0;
            grpAi.Controls.Add(_cboAiProvider);

            Button btnApiConfig = new Button
            {
                Text = "🔑 Cấu hình Khóa API & Máy chủ AI...",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                BackColor = Color.FromArgb(239, 246, 255),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(270, 34),
                Location = new Point(16, 115),
                Cursor = Cursors.Hand
            };
            btnApiConfig.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnApiConfig.Click += (s, e) =>
            {
                using (var dlg = new AiConfigDialog())
                {
                    dlg.ShowDialog(this);
                }
            };
            grpAi.Controls.Add(btnApiConfig);

            p.Controls.Add(grpAi);
        }

        private void BuildTabHelp(Panel p)
        {
            int curY = 16;
            Label lblTitle = new Label { Text = "Phím tắt nhanh & Thông tin Suite", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(10, curY), AutoSize = true };
            p.Controls.Add(lblTitle);
            curY += 36;

            GroupBox grpShortcuts = new GroupBox { Text = "Tra cứu phím tắt bàn phím", Location = new Point(10, curY), Size = new Size(470, 110), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };

            Label lblDesc = new Label
            {
                Text = "Tra cứu danh mục toàn bộ phím tắt Microsoft Word / PowerPoint / Excel chuẩn quốc tế.",
                Location = new Point(16, 26),
                Size = new Size(430, 36),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            grpShortcuts.Controls.Add(lblDesc);

            Button btnOpenShortcuts = new Button
            {
                Text = "⌨ Mở Bảng Tổng Hợp Phím Tắt",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                BackColor = Color.FromArgb(239, 246, 255),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(240, 32),
                Location = new Point(16, 64),
                Cursor = Cursors.Hand
            };
            btnOpenShortcuts.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnOpenShortcuts.Click += (s, e) =>
            {
                using (var sf = new ShortcutsHelpForm())
                {
                    sf.ShowDialog(this);
                }
            };
            grpShortcuts.Controls.Add(btnOpenShortcuts);
            p.Controls.Add(grpShortcuts);
            curY += 120;

            GroupBox grpAbout = new GroupBox { Text = "Thông tin bản quyền & Đơn vị phát triển", Location = new Point(10, curY), Size = new Size(470, 95), Font = new Font("Segoe UI", 9.25f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            Label lblAbout = new Label
            {
                Text = "ZENQ OFFICE SUITE - PHIÊN BẢN 4.0 (BUILD 2026)\nPhát triển bởi: NEXTGENEDUSPACE STUDIO • Trương Nguyễn Thế Danh\nTối ưu cho Windows 10/11 với hiệu năng Direct2D siêu tốc.",
                Location = new Point(16, 26),
                Size = new Size(430, 56),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            grpAbout.Controls.Add(lblAbout);
            p.Controls.Add(grpAbout);
        }

        private void LoadCurrentSettings()
        {
            var s = AppSettings.Instance;

            // Ribbon
            if (s.RibbonStyle == RibbonStyle.Standard) _rbRibbonStandard.Checked = true;
            else _rbRibbonIsland.Checked = true;

            // Theme
            if (s.Theme == AppTheme.Dark) _rbThemeDark.Checked = true;
            else _rbThemeLight.Checked = true;

            // Lang
            if (s.Language == AppLanguage.English) _rbLangEn.Checked = true;
            else _rbLangVi.Checked = true;

            // Auto Save
            _chkAutoSave.Checked = s.AutoSaveEnabled;
            if (s.AutoSaveIntervalSeconds <= 60) _cboAutoSaveInterval.SelectedIndex = 0;
            else if (s.AutoSaveIntervalSeconds <= 180) _cboAutoSaveInterval.SelectedIndex = 1;
            else if (s.AutoSaveIntervalSeconds <= 300) _cboAutoSaveInterval.SelectedIndex = 2;
            else _cboAutoSaveInterval.SelectedIndex = 3;

            // Display
            _chkShowRuler.Checked = s.ShowRuler;
            _chkSmoothScroll.Checked = s.EnableSmoothScroll;
            _chkFormattingMarks.Checked = s.ShowFormattingMarks;
            _chkWordPrediction.Checked = s.WordPredictionEnabled;
            _chkAutoCorrectVi.Checked = s.AutoCorrectVietnamese;

            if (s.DefaultZoom >= 1.45f) _cboDefaultZoom.SelectedIndex = 2;
            else if (s.DefaultZoom >= 1.20f) _cboDefaultZoom.SelectedIndex = 1;
            else _cboDefaultZoom.SelectedIndex = 0;

            // AI
            _chkAiCopilot.Checked = s.AiCopilotEnabled;
            _cboAiProvider.SelectedIndex = Math.Max(0, Math.Min(3, s.SelectedAiProvider));
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

            s.Save();
            s.NotifyChanged();

            if (_appForm != null)
            {
                _appForm.StatusText = "Đã lưu và áp dụng cài đặt ZenQ Office thành công!";
                _appForm.Invalidate();
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Cửa sổ Quản lý Phông chữ & Sửa lỗi Tiếng Việt cho ZenQ Office.
    /// Thiết kế chuẩn Win11 đồng bộ với Trợ lý AI (AiConfigDialog):
    /// - Giao diện thanh lịch, sạch sẽ, bo góc mượt mà.
    /// - Render chữ xem trước siêu nét với ClearType / DirectWrite.
    /// - Sửa lỗi và thay thế phông chữ TRỰC TIẾP trên Slide/Văn bản đang mở, không cần lưu sang file khác.
    /// </summary>
    public class VietnameseFontToolkitForm : Form
    {
        // Shell & Containers
        private TableLayoutPanel _shell;
        private Panel pnlHeader;
        private Panel pnlContent;
        private Panel pnlToolbar;
        private Panel pnlTableCard;
        private Panel pnlPreviewCard;
        private Panel pnlFooter;

        // Header controls
        private Label lblTitle;
        private Label lblSubtitle;
        private FlowLayoutPanel pnlBadges;
        private Label badgeTotalFonts;
        private Label badgeViStatus;
        private Label badgeEmbedded;

        // Toolbar controls
        private Label lblDocIcon;
        private Label lblCurrentDoc;
        private SettingsActionButton btnBrowse;
        private FluentComboBox cmbFilter;
        private SettingsInputFrame searchFrame;
        private TextBox txtSearch;

        // Table
        private FontToolkitD2DTable dgvFonts;

        // Preview Section
        private Label lblPreviewTitle;
        private Label lblPreviewFontBadge;
        private SettingsActionButton btnReplaceCurrentFont;
        private FlowLayoutPanel pnlPresetButtons;
        private FontSamplePresetButton btnPresetDoc;
        private FontSamplePresetButton btnPresetSentence;
        private FontSamplePresetButton btnPresetDiacritics;
        private TextBox txtPreviewSample;
        private FontToolkitPreviewSurface pbFontPreview;
        private Label lblDiagnosticInfo;
        private float currentPreviewFontSize = 18.0f;

        // Footer Actions
        private SettingsActionButton btnExportFonts;
        private SettingsActionButton btnInstallFonts;
        private SettingsActionButton btnFixDirectly;
        private SettingsActionButton btnClose;

        // Data
        private List<ExtractedFontInfo> currentFontList = new List<ExtractedFontInfo>();
        private List<ExtractedFontInfo> displayedFontList = new List<ExtractedFontInfo>();
        private ExtractedFontInfo selectedFont = null;
        private string activeDocumentPath = "";
        private string openEditorFilePath = "";
        private string docSampleText = "";
        private bool isLiveSlideMode = false;

        public VietnameseFontToolkitForm(string currentDocPath = null)
        {
            this.openEditorFilePath = currentDocPath;
            InitializeComponent();
            LoadActiveOrSpecifiedDocument(currentDocPath);
        }

        private void InitializeComponent()
        {
            Text = AppL10n.T("Quản lý Phông chữ & Tiếng Việt", "Fonts & Vietnamese Toolkit");
            ClientSize = new Size(820, 650);
            MinimumSize = new Size(720, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(244, 247, 252);
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);

            // Cho phép kéo thả tệp tài liệu
            AllowDrop = true;
            DragEnter += Form_DragEnter;
            DragDrop += Form_DragDrop;

            // 1. Shell TableLayoutPanel
            _shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                BackColor = Color.FromArgb(244, 247, 252)
            };
            _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));   // Header
            _shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // Content
            _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));    // Footer
            Controls.Add(_shell);

            // ==========================================
            // A. HEADER (Giống hệt phong cách AiConfigDialog)
            // ==========================================
            pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 6),
                BackColor = Color.FromArgb(244, 247, 252)
            };

            lblTitle = new Label
            {
                Text = AppL10n.T("Quản lý Phông chữ & Tiếng Việt", "Fonts & Vietnamese Toolkit"),
                Font = new Font("Segoe UI", 16.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 37, 52),
                Location = new Point(28, 14),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                Text = AppL10n.T("Kiểm tra độ tương thích tiếng Việt, trích xuất phông nhúng và sửa lỗi trực tiếp trên Slide.", "Check Vietnamese glyph support, extract embedded fonts, and fix slides in place."),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 110, 125),
                Location = new Point(28, 49),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Badges góc trên bên phải
            pnlBadges = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Location = new Point(ClientSize.Width - 360, 20),
                BackColor = Color.Transparent
            };

            badgeTotalFonts = CreateHeaderPill(AppL10n.T("0 phông", "0 fonts"), Color.FromArgb(238, 242, 255), Color.FromArgb(79, 70, 229));
            badgeViStatus = CreateHeaderPill(AppL10n.T("Chưa nạp tệp", "No file loaded"), Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105));
            badgeEmbedded = CreateHeaderPill(AppL10n.T("0 nhúng", "0 embedded"), Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206));

            pnlBadges.Controls.Add(badgeTotalFonts);
            pnlBadges.Controls.Add(badgeViStatus);
            pnlBadges.Controls.Add(badgeEmbedded);
            pnlHeader.Controls.Add(pnlBadges);

            _shell.Controls.Add(pnlHeader, 0, 0);

            // ==========================================
            // B. CONTENT (Scrollable / Tabular Layout)
            // ==========================================
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 2, 28, 8),
                BackColor = Color.FromArgb(244, 247, 252)
            };
            _shell.Controls.Add(pnlContent, 0, 1);

            TableLayoutPanel bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));  // Toolbar
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Table
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148)); // Preview Card
            pnlContent.Controls.Add(bodyLayout);

            // 1. Toolbar
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.FromArgb(244, 247, 252)
            };
            bodyLayout.Controls.Add(pnlToolbar, 0, 0);

            lblDocIcon = new Label
            {
                Text = "📄",
                Font = new Font("Segoe UI Emoji", 10.5f),
                Location = new Point(0, 8),
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlToolbar.Controls.Add(lblDocIcon);

            lblCurrentDoc = new Label
            {
                Text = AppL10n.T("Slide đang mở trong ZenQ Office", "Open presentation in ZenQ Office"),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, 8),
                Size = new Size(250, 24),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlToolbar.Controls.Add(lblCurrentDoc);

            btnBrowse = new SettingsActionButton
            {
                Text = AppL10n.T("Chọn tệp...", "Browse..."),
                Size = new Size(88, 30),
                Location = new Point(282, 5),
                Font = new Font("Segoe UI", 8.5f)
            };
            btnBrowse.Click += (s, e) => BrowseAndOpenFile();
            pnlToolbar.Controls.Add(btnBrowse);

            txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                PlaceholderText = AppL10n.T("Tìm phông chữ...", "Search fonts..."),
                Font = new Font("Segoe UI", 9f)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            searchFrame = new SettingsInputFrame(txtSearch)
            {
                Size = new Size(205, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlContent.Width - 205 - 48, 4)
            };
            pnlToolbar.Controls.Add(searchFrame);

            cmbFilter = new FluentComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(146, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlContent.Width - 367 - 48, 4),
                Font = new Font("Segoe UI", 8.5f)
            };
            cmbFilter.Items.AddRange(new object[] { AppL10n.T("Tất cả phông", "All fonts"), AppL10n.T("Phông cần sửa", "Needs fix"), AppL10n.T("Phông nhúng", "Embedded") });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlToolbar.Controls.Add(cmbFilter);

            // 2. Table DataGridView Frame
            pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            bodyLayout.Controls.Add(pnlTableCard, 0, 1);

            dgvFonts = new FontToolkitD2DTable { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.White };
            dgvFonts.SelectionChanged += DgvFonts_SelectionChanged;
            pnlTableCard.Controls.Add(dgvFonts);

            // 3. Preview Card (Thiết kế bo tròn thẻ card sạch đẹp)
            pnlPreviewCard = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 10)
            };
            bodyLayout.Controls.Add(pnlPreviewCard, 0, 2);

            lblPreviewTitle = new Label
            {
                Text = AppL10n.T("Xem trước:", "Preview:"),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 12),
                AutoSize = true
            };
            pnlPreviewCard.Controls.Add(lblPreviewTitle);

            lblPreviewFontBadge = new Label
            {
                Text = "Segoe UI",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(79, 70, 229),
                Location = new Point(86, 11),
                AutoSize = true
            };
            pnlPreviewCard.Controls.Add(lblPreviewFontBadge);

            // Nút thay thế tức thì 1 phông vào Slide
            btnReplaceCurrentFont = new SettingsActionButton
            {
                Text = AppL10n.T("⚡ Thay thế vào Slide", "⚡ Replace in Slide"),
                Primary = true,
                Size = new Size(154, 28),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(206, 7),
                Visible = false
            };
            btnReplaceCurrentFont.Click += (s, e) => DoReplaceSelectedFontDirectly();
            pnlPreviewCard.Controls.Add(btnReplaceCurrentFont);

            pnlPresetButtons = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                Location = new Point(pnlContent.Width - 260 - 48, 5),
                BackColor = Color.Transparent
            };

            btnPresetDoc = CreateSmallToggleBtn(AppL10n.T("Nội dung", "Document"));
            btnPresetDoc.Click += (s, e) => { txtPreviewSample.Text = !string.IsNullOrEmpty(docSampleText) ? docSampleText : "Tiếng Việt thân yêu"; pbFontPreview.Invalidate(); };

            btnPresetSentence = CreateSmallToggleBtn(AppL10n.T("Câu mẫu", "Sentence"));
            btnPresetSentence.Click += (s, e) => { txtPreviewSample.Text = "Hoàng tử bé bước đi trong ánh chiều tà rực rỡ, nhìn đàn chim én chao liệng."; pbFontPreview.Invalidate(); };

            btnPresetDiacritics = CreateSmallToggleBtn(AppL10n.T("Dấu thanh", "Diacritics"));
            btnPresetDiacritics.Click += (s, e) => { txtPreviewSample.Text = "a á à ả ã ạ ă ắ ằ ẳ ẵ ặ â ấ ầ ẩ ẫ ậ e é è ẻ ẽ ẹ ê ế ề ể ễ ệ o ó ò ỏ õ ọ ô ố ồ ổ ỗ ộ ơ ớ ờ ở ỡ ợ u ú ù ủ ũ ụ ư ứ ừ ử ữ ự i í ì ỉ ĩ ị y ý ỳ ỷ ỹ ỵ đ Đ"; pbFontPreview.Invalidate(); };

            pnlPresetButtons.Controls.Add(btnPresetDoc);
            pnlPresetButtons.Controls.Add(btnPresetSentence);
            pnlPresetButtons.Controls.Add(btnPresetDiacritics);
            pnlPreviewCard.Controls.Add(pnlPresetButtons);

            txtPreviewSample = new TextBox
            {
                Text = "Tiếng Việt thân yêu",
                Visible = false
            };
            txtPreviewSample.TextChanged += (s, e) => pbFontPreview.SampleText = txtPreviewSample.Text;
            pnlPreviewCard.Controls.Add(txtPreviewSample);

            // Canvas hiển thị chữ mượt mà, sắc nét
            pbFontPreview = new FontToolkitPreviewSurface
            {
                Location = new Point(16, 42),
                Size = new Size(pnlContent.Width - 80, 76),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                SampleText = "Tiếng Việt thân yêu",
                PreviewFontName = "Segoe UI",
                PreviewFontSize = currentPreviewFontSize
            };
            pnlPreviewCard.Controls.Add(pbFontPreview);

            lblDiagnosticInfo = new Label
            {
                Location = new Point(16, 124),
                Size = new Size(pnlContent.Width - 80, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            pnlPreviewCard.Controls.Add(lblDiagnosticInfo);

            // ==========================================
            // C. FOOTER ACTIONS (Giống phong cách AiConfigDialog)
            // ==========================================
            pnlFooter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 18),
                BackColor = Color.FromArgb(244, 247, 252)
            };
            _shell.Controls.Add(pnlFooter, 0, 2);

            var actionsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = Padding.Empty
            };
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Trích xuất
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // Cài vào
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // Spacer
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));  // Đóng
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Sửa trực tiếp
            pnlFooter.Controls.Add(actionsTable);

            btnExportFonts = new SettingsActionButton
            {
                Text = AppL10n.T("Trích xuất phông", "Extract Fonts"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Font = new Font("Segoe UI", 9f)
            };
            btnExportFonts.Click += (s, e) => DoExportFonts();
            actionsTable.Controls.Add(btnExportFonts, 0, 0);

            btnInstallFonts = new SettingsActionButton
            {
                Text = AppL10n.T("Cài vào máy", "Install"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Font = new Font("Segoe UI", 9f)
            };
            btnInstallFonts.Click += (s, e) => DoInstallFonts();
            actionsTable.Controls.Add(btnInstallFonts, 1, 0);

            btnClose = new SettingsActionButton
            {
                Text = AppL10n.T("Đóng", "Close"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Font = new Font("Segoe UI", 9.2f),
                DialogResult = DialogResult.Cancel
            };
            btnClose.Click += (s, e) => Close();
            actionsTable.Controls.Add(btnClose, 3, 0);

            btnFixDirectly = new SettingsActionButton
            {
                Text = AppL10n.T("⚡ Sửa lỗi vào Slide", "⚡ Fix in Slide"),
                Primary = true,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnFixDirectly.Click += (s, e) => DoFixDirectly();
            actionsTable.Controls.Add(btnFixDirectly, 4, 0);

            CancelButton = btnClose;
            AcceptButton = btnFixDirectly;

            Shown += (s, e) =>
            {
                FluentUi.RoundWindow(this);
                btnClose.Focus();
            };
        }

        private Label CreateHeaderPill(string text, Color bg, Color fg)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(3, 0, 3, 0),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lbl.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, fg), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, lbl.Width - 1, lbl.Height - 1);
                }
            };
            return lbl;
        }

        private FontSamplePresetButton CreateSmallToggleBtn(string text)
        {
            return new FontSamplePresetButton
            {
                Text = text,
                Size = new Size(76, 28),
                Margin = new Padding(3, 0, 3, 0),
                Font = new Font("Segoe UI", 8.5f)
            };
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0) LoadDocumentFile(files[0]);
        }

        private void BrowseAndOpenFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = AppL10n.T("Tệp Office & Phông chữ (*.pptx;*.docx;*.ttf;*.otf)|*.pptx;*.docx;*.ttf;*.otf|Bài thuyết trình PowerPoint (*.pptx)|*.pptx|Văn bản Word (*.docx)|*.docx|Phông chữ TrueType / OpenType (*.ttf;*.otf)|*.ttf;*.otf|Tất cả tệp (*.*)|*.*", "Office & Font Files (*.pptx;*.docx;*.ttf;*.otf)|*.pptx;*.docx;*.ttf;*.otf|PowerPoint (*.pptx)|*.pptx|Word (*.docx)|*.docx|Fonts (*.ttf;*.otf)|*.ttf;*.otf|All Files (*.*)|*.*");
                ofd.Title = AppL10n.T("Chọn tệp cần kiểm tra phông chữ", "Select File to Check Fonts");
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadDocumentFile(ofd.FileName);
                }
            }
        }

        private void LoadActiveOrSpecifiedDocument(string specifiedPath)
        {
            var app = AppForm.Instance;

            // 1. Nếu có Presentation đang mở trên Slide Canvas trong ZenQ Office -> Nạp trực tiếp!
            if (app != null && app.SlidesView?.Presentation != null && app.SlidesView.Presentation.Slides.Count > 0)
            {
                isLiveSlideMode = true;
                btnFixDirectly.Text = AppL10n.T("⚡ Sửa lỗi vào Slide", "⚡ Fix in Slide");
                if (app.Ribbon != null && app.Ribbon.ActiveDocIndex >= 0 && app.Ribbon.ActiveDocIndex < app.Ribbon.DocTabs.Count)
                {
                    var tab = app.Ribbon.DocTabs[app.Ribbon.ActiveDocIndex];
                    lblCurrentDoc.Text = !string.IsNullOrEmpty(tab.Title) ? tab.Title : AppL10n.T("Slide đang mở", "Open Slide");
                    activeDocumentPath = tab.FilePath ?? "";
                }
                else
                {
                    lblCurrentDoc.Text = AppL10n.T("Bài trình chiếu đang mở", "Open Presentation");
                    activeDocumentPath = specifiedPath ?? "";
                }
                LoadFromActivePresentation(app.SlidesView.Presentation);
                return;
            }

            // 2. Nếu có đường dẫn truyền vào hoặc tệp lưu trên ổ đĩa
            if (!string.IsNullOrEmpty(specifiedPath) && File.Exists(specifiedPath))
            {
                LoadDocumentFile(specifiedPath);
                return;
            }

            lblCurrentDoc.Text = AppL10n.T("Chưa có tài liệu nào", "No document loaded");
            badgeTotalFonts.Text = AppL10n.T("0 phông", "0 fonts");
            badgeViStatus.Text = AppL10n.T("Chưa nạp tệp", "No file loaded");
            badgeEmbedded.Text = AppL10n.T("0 nhúng", "0 embedded");
            btnFixDirectly.Enabled = false;
        }

        private void LoadFromActivePresentation(PresentationRoot pres)
        {
            if (pres == null) return;
            try
            {
                currentFontList = VietnameseFontToolkit.AnalyzePresentationModel(pres, activeDocumentPath);
                docSampleText = VietnameseFontToolkit.ExtractPresentationSampleText(pres, 120);
                if (string.IsNullOrEmpty(docSampleText)) docSampleText = "Tiếng Việt thân yêu";
                txtPreviewSample.Text = docSampleText;

                UpdateBadgesAndDisplay();
            }
            catch (Exception ex)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Lỗi khi đọc phông từ Slide: ", "Error reading fonts from Slide: ") + ex.Message, AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadDocumentFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                activeDocumentPath = path;
                string fname = Path.GetFileName(path);
                lblCurrentDoc.Text = fname;

                var app = AppForm.Instance;
                isLiveSlideMode = (app != null && app.Ribbon != null && app.Ribbon.CurrentMode == OfficeAppMode.Slides && app.SlidesView?.Presentation != null);
                btnFixDirectly.Text = isLiveSlideMode ? AppL10n.T("⚡ Sửa lỗi vào Slide", "⚡ Fix in Slide") : AppL10n.T("⚡ Sửa lỗi trực tiếp", "⚡ Fix Directly");

                currentFontList = VietnameseFontToolkit.AnalyzeDocumentFonts(path);
                docSampleText = VietnameseFontToolkit.ExtractDocumentSampleText(path, 120);
                if (string.IsNullOrEmpty(docSampleText)) docSampleText = "Tiếng Việt thân yêu";
                txtPreviewSample.Text = docSampleText;

                UpdateBadgesAndDisplay();
            }
            catch (Exception ex)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Không thể đọc phông từ tệp: ", "Unable to read fonts from file: ") + ex.Message, AppL10n.T("Lỗi đọc tệp", "Read Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateBadgesAndDisplay()
        {
            int total = currentFontList.Count;
            int needsFix = currentFontList.Count(f => !f.HasVietnameseSupport || (f.Format != null && (f.Format.Contains("TCVN3") || f.Format.Contains("VNI"))));
            int embedded = currentFontList.Count(f => f.IsEmbedded);

            badgeTotalFonts.Text = AppL10n.T($"{total} phông", $"{total} fonts");
            badgeEmbedded.Text = AppL10n.T($"{embedded} font nhúng", $"{embedded} embedded");

            if (needsFix > 0)
            {
                badgeViStatus.Text = AppL10n.T($"{needsFix} cần sửa", $"{needsFix} need fix");
                badgeViStatus.BackColor = Color.FromArgb(254, 242, 242);
                badgeViStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else
            {
                badgeViStatus.Text = AppL10n.T("100% Tiếng Việt", "100% Vietnamese");
                badgeViStatus.BackColor = Color.FromArgb(240, 253, 244);
                badgeViStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }

            btnExportFonts.Text = AppL10n.T($"Trích xuất ({embedded})", $"Extract ({embedded})");
            btnExportFonts.Enabled = embedded > 0;
            btnInstallFonts.Enabled = embedded > 0;
            btnFixDirectly.Enabled = total > 0;

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string keyword = txtSearch.Text?.Trim().ToLowerInvariant() ?? "";
            int filterMode = cmbFilter.SelectedIndex; // 0: All, 1: Needs fix, 2: Embedded

            displayedFontList = currentFontList.FindAll(f =>
            {
                if (filterMode == 1 && (f.HasVietnameseSupport && !(f.Format != null && (f.Format.Contains("TCVN3") || f.Format.Contains("VNI"))))) return false;
                if (filterMode == 2 && !f.IsEmbedded) return false;

                if (!string.IsNullOrEmpty(keyword))
                {
                    bool matchName = f.Typeface != null && f.Typeface.ToLowerInvariant().Contains(keyword);
                    bool matchSuggest = f.SuggestedVietnameseFont != null && f.SuggestedVietnameseFont.ToLowerInvariant().Contains(keyword);
                    if (!matchName && !matchSuggest) return false;
                }
                return true;
            });

            var rows = new List<FontToolkitD2DTable.Row>();
            foreach (var f in displayedFontList)
            {
                string statusText = (f.Format != null && (f.Format.Contains("TCVN3") || f.Format.Contains("VNI")))
                    ? AppL10n.T($"⚠ Mã {f.Format}", $"⚠ {f.Format} Encoding")
                    : (f.HasVietnameseSupport
                        ? AppL10n.T("✓ Chuẩn Unicode", "✓ Unicode Ready")
                        : AppL10n.T($"⚠ Thiếu {(f.MissingVietnameseChars != null ? f.MissingVietnameseChars.Count : 0)} dấu", $"⚠ Missing {(f.MissingVietnameseChars != null ? f.MissingVietnameseChars.Count : 0)} glyphs"));
                string srcText = f.IsEmbedded
                    ? AppL10n.T($"Nhúng ({f.FileSize / 1024} KB)", $"Embedded ({f.FileSize / 1024} KB)")
                    : AppL10n.T("Đã có trên máy", "System Installed");
                string sugText = !f.HasVietnameseSupport && !string.IsNullOrEmpty(f.SuggestedVietnameseFont) ? $"➔ {f.SuggestedVietnameseFont}" : "--";
                rows.Add(new FontToolkitD2DTable.Row(f, statusText, srcText, sugText));
            }

            dgvFonts.SetRows(rows);
            if (displayedFontList.Count > 0)
            {
                selectedFont = displayedFontList[0];
            }
            else
            {
                selectedFont = null;
            }
            UpdatePreviewCard();
        }

        private void DgvFonts_SelectionChanged(object sender, EventArgs e)
        {
            int idx = dgvFonts.SelectedIndex;
            if (idx >= 0 && idx < displayedFontList.Count)
            {
                selectedFont = displayedFontList[idx];
                UpdatePreviewCard();
            }
        }

        private void UpdatePreviewCard()
        {
            if (selectedFont == null)
            {
                lblPreviewFontBadge.Text = "--";
                btnReplaceCurrentFont.Visible = false;
                lblDiagnosticInfo.Text = AppL10n.T("Chọn một phông chữ trong bảng phía trên để xem trước.", "Select a font from the table above to preview.");
                pbFontPreview.PreviewFontName = "Segoe UI";
                pbFontPreview.Invalidate();
                return;
            }

            lblPreviewFontBadge.Text = selectedFont.Typeface;
            pbFontPreview.PreviewFontName = selectedFont.Typeface;

            // Nếu phông bị thiếu dấu -> Hiện nút thay thế 1-click vào Slide!
            if (!selectedFont.HasVietnameseSupport && !string.IsNullOrEmpty(selectedFont.SuggestedVietnameseFont))
            {
                btnReplaceCurrentFont.Text = AppL10n.T($"⚡ Thay sang {selectedFont.SuggestedVietnameseFont}", $"⚡ Switch to {selectedFont.SuggestedVietnameseFont}");
                btnReplaceCurrentFont.Visible = isLiveSlideMode;
                lblDiagnosticInfo.Text = AppL10n.T($"⚠ Phông '{selectedFont.Typeface}' thiếu {(selectedFont.MissingVietnameseChars != null ? selectedFont.MissingVietnameseChars.Count : 0)} dấu tiếng Việt. Bấm nút phía trên để tự động đổi sang '{selectedFont.SuggestedVietnameseFont}'.", $"⚠ Font '{selectedFont.Typeface}' is missing {(selectedFont.MissingVietnameseChars != null ? selectedFont.MissingVietnameseChars.Count : 0)} Vietnamese glyphs. Click above to replace with '{selectedFont.SuggestedVietnameseFont}'.");
                lblDiagnosticInfo.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else if ((selectedFont.Format != null && (selectedFont.Format.Contains("TCVN3") || selectedFont.Format.Contains("VNI"))))
            {
                btnReplaceCurrentFont.Visible = false;
                lblDiagnosticInfo.Text = AppL10n.T($"⚠ Phông đang dùng bảng mã cũ {selectedFont.Format}. Bấm 'Sửa lỗi vào Slide' để chuyển toàn bộ sang Unicode.", $"⚠ Font uses legacy {selectedFont.Format} encoding. Click 'Fix in Slide' to convert to Unicode.");
                lblDiagnosticInfo.ForeColor = Color.FromArgb(217, 119, 6);
            }
            else
            {
                btnReplaceCurrentFont.Visible = false;
                lblDiagnosticInfo.Text = AppL10n.T($"✓ Phông '{selectedFont.Typeface}' hỗ trợ đầy đủ 100% tiếng Việt chuẩn Unicode, nét chữ rõ ràng và mượt mà.", $"✓ Font '{selectedFont.Typeface}' has 100% Unicode Vietnamese support.");
                lblDiagnosticInfo.ForeColor = Color.FromArgb(22, 163, 74);
            }

            pbFontPreview.Invalidate();
        }

        /// <summary>
        /// Thay thế tức thì 1 phông chữ đang chọn trên toàn bộ Slide
        /// </summary>
        private void DoReplaceSelectedFontDirectly()
        {
            if (selectedFont == null || string.IsNullOrEmpty(selectedFont.SuggestedVietnameseFont)) return;

            var app = AppForm.Instance;
            if (app?.SlidesView?.Presentation == null)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Không tìm thấy bài thuyết trình đang mở.", "No open presentation found."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string oldFont = selectedFont.Typeface;
            string newFont = selectedFont.SuggestedVietnameseFont;

            int count = VietnameseFontToolkit.ReplaceSingleFontInPresentation(app.SlidesView.Presentation, oldFont, newFont);

            if (count > 0)
            {
                if (app.Ribbon != null && app.Ribbon.ActiveDocIndex >= 0)
                {
                    app.Ribbon.DocTabs[app.Ribbon.ActiveDocIndex].IsModified = true;
                }

                // Cập nhật tệp trên đĩa nếu có
                if (!string.IsNullOrEmpty(activeDocumentPath) && File.Exists(activeDocumentPath))
                {
                    try { Format.PptxHandler.SavePptx(activeDocumentPath, app.SlidesView.Presentation); } catch { }
                }

                app.SlidesView.Invalidate();
                app.Invalidate();

                LoadFromActivePresentation(app.SlidesView.Presentation);

                ZenQOffice.UI.FluentMessageBox.Show(this,
                    AppL10n.T($"Đã thay thế thành công phông '{oldFont}' thành '{newFont}' tại {count} vị trí trên toàn bộ các slide!", $"Replaced font '{oldFont}' with '{newFont}' across {count} location(s)."),
                    AppL10n.T("Thay thế phông hoàn tất", "Font Replaced"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                ZenQOffice.UI.FluentMessageBox.Show(this,
                    AppL10n.T($"Không tìm thấy văn bản nào đang sử dụng phông '{oldFont}'.", $"No text found using font '{oldFont}'."),
                    AppL10n.T("Thông báo", "Notice"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Sửa lỗi tiếng Việt và thay thế toàn bộ phông lỗi TRỰC TIẾP trên Slide/Tài liệu đang mở (Không lưu sang file khác!)
        /// </summary>
        private void DoFixDirectly()
        {
            var app = AppForm.Instance;

            // Tuỳ chọn chuẩn hóa
            FontFixOptions options = new FontFixOptions
            {
                ConvertTcvn3 = true,
                ConvertVni = true,
                NormalizeNfc = true,
                ReplaceUnsupportedFonts = true
            };

            // 1. Nếu đang mở Slide trong ZenQ Office -> Sửa trực tiếp trên Presentation trong RAM!
            if (app != null && app.SlidesView?.Presentation != null && app.SlidesView.Presentation.Slides.Count > 0)
            {
                var report = VietnameseFontToolkit.FixPresentationModel(app.SlidesView.Presentation, options);

                if (report != null && report.Success)
                {
                    // Đánh dấu tài liệu đã sửa
                    if (app.Ribbon != null && app.Ribbon.ActiveDocIndex >= 0)
                    {
                        app.Ribbon.DocTabs[app.Ribbon.ActiveDocIndex].IsModified = true;
                    }

                    // Đánh dấu làm mới ảnh thu nhỏ của toàn bộ Slide để cập nhật ngay trên thanh bên trái
                    foreach (var sl in app.SlidesView.Presentation.Slides)
                    {
                        if (sl != null) sl.IsThumbnailDirty = true;
                    }
                    try { ZenQOffice.UI.D2DGraphics.ClearFontCache(); } catch { }

                    // Tự động ghi đè tệp làm việc tạm/gốc trên đĩa nếu có để bảo toàn
                    if (!string.IsNullOrEmpty(activeDocumentPath) && File.Exists(activeDocumentPath))
                    {
                        try { Format.PptxHandler.SavePptx(activeDocumentPath, app.SlidesView.Presentation); } catch { }
                    }

                    // Vẽ lại Slide Canvas ngay lập tức
                    app.SlidesView.Invalidate();
                    app.Invalidate();

                    // Cập nhật lại danh sách phông trên hộp thoại
                    LoadFromActivePresentation(app.SlidesView.Presentation);

                    string repDetail = report.Message;
                    if (report.ReplacedFonts.Count > 0)
                    {
                        repDetail += AppL10n.T("\n\nCác phông đã được thay thế đẹp mắt:\n- ", "\n\nFonts replaced cleanly:\n- ") + string.Join("\n- ", report.ReplacedFonts);
                    }

                    ZenQOffice.UI.FluentMessageBox.Show(this,
                        repDetail + AppL10n.T("\n\nToàn bộ phông chữ lỗi đã được cập nhật trực tiếp trên Slide của bạn!", "\n\nAll incompatible fonts have been updated directly on your slides!"),
                        AppL10n.T("Sửa lỗi trực tiếp thành công", "Fix Complete"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    ZenQOffice.UI.FluentMessageBox.Show(this, report?.Message ?? AppL10n.T("Có lỗi khi xử lý bài trình chiếu.", "An error occurred while processing the presentation."), AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            // 2. Nếu người dùng chọn tệp ngoài từ ổ đĩa (không mở trong tab)
            if (!string.IsNullOrEmpty(activeDocumentPath) && File.Exists(activeDocumentPath))
            {
                string ext = Path.GetExtension(activeDocumentPath).ToLowerInvariant();
                string dir = Path.GetDirectoryName(activeDocumentPath);
                string nameNoExt = Path.GetFileNameWithoutExtension(activeDocumentPath);
                string defaultOut = Path.Combine(dir, $"{nameNoExt}_Fixed_Vietnamese{ext}");

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.FileName = Path.GetFileName(defaultOut);
                    sfd.Filter = ext == ".pptx" ? AppL10n.T("Bài thuyết trình PowerPoint (*.pptx)|*.pptx", "PowerPoint Presentation (*.pptx)|*.pptx") : AppL10n.T("Văn bản Word (*.docx)|*.docx", "Word Document (*.docx)|*.docx");
                    sfd.Title = AppL10n.T("Chọn nơi lưu tệp đã chuẩn hóa tiếng Việt", "Save Fixed File");
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        DocumentFixReport report = (ext == ".pptx")
                            ? VietnameseFontToolkit.FixPresentationVietnamese(activeDocumentPath, sfd.FileName, options)
                            : VietnameseFontToolkit.FixDocxVietnamese(activeDocumentPath, sfd.FileName, options);

                        if (report != null && report.Success)
                        {
                            ZenQOffice.UI.FluentMessageBox.Show(this,
                                AppL10n.T($"{report.Message}\n\nTệp đã lưu tại:\n{sfd.FileName}", $"{report.Message}\n\nSaved to:\n{sfd.FileName}"),
                                AppL10n.T("Hoàn tất chuẩn hóa", "Normalization Complete"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            LoadDocumentFile(sfd.FileName);
                        }
                    }
                }
            }
        }

        private void DoExportFonts()
        {
            List<ExtractedFontInfo> embeddedOnly = currentFontList.FindAll(f => f.Data != null && f.Data.Length > 0);

            if (embeddedOnly.Count == 0 && SlideFontManager.EmbeddedFontFiles.Count > 0)
            {
                foreach (var kvp in SlideFontManager.EmbeddedFontFiles)
                {
                    string tf = null;
                    SlideFontManager.EmbeddedFontTypefaces.TryGetValue(kvp.Key, out tf);
                    if (string.IsNullOrEmpty(tf)) tf = Path.GetFileNameWithoutExtension(kvp.Key);

                    embeddedOnly.Add(new ExtractedFontInfo
                    {
                        Typeface = tf,
                        FileName = $"{tf.Replace(' ', '_')}.ttf",
                        Data = kvp.Value,
                        IsEmbedded = true
                    });
                }
            }

            if (embeddedOnly.Count == 0)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Tài liệu này không chứa tệp phông chữ nhúng bên trong.", "This document contains no embedded fonts."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = AppL10n.T("Chọn thư mục để trích xuất các tệp phông chữ TrueType / OpenType (.ttf, .otf)", "Choose folder to extract TrueType / OpenType fonts (.ttf, .otf)");
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    int exported = 0;
                    foreach (var f in embeddedOnly)
                    {
                        if (f.Data != null && f.Data.Length > 0)
                        {
                            string safeName = Path.GetInvalidFileNameChars().Aggregate(f.FileName ?? $"{f.Typeface}.ttf", (current, c) => current.Replace(c, '_'));
                            string outPath = Path.Combine(fbd.SelectedPath, safeName);
                            try
                            {
                                File.WriteAllBytes(outPath, f.Data);
                                exported++;
                            }
                            catch { }
                        }
                    }

                    var res = ZenQOffice.UI.FluentMessageBox.Show(this,
                        AppL10n.T($"Đã trích xuất thành công {exported} phông chữ vào thư mục:\n{fbd.SelectedPath}\n\nBạn có muốn mở thư mục này ngay bây giờ không?", $"Extracted {exported} font(s) to:\n{fbd.SelectedPath}\n\nOpen this folder now?"),
                        AppL10n.T("Trích xuất phông chữ thành công", "Extraction Complete"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (res == DialogResult.Yes)
                    {
                        try { Process.Start(new ProcessStartInfo("explorer.exe", fbd.SelectedPath) { UseShellExecute = true }); } catch { }
                    }
                }
            }
        }

        private void DoInstallFonts()
        {
            List<ExtractedFontInfo> embeddedOnly = currentFontList.FindAll(f => f.Data != null && f.Data.Length > 0);
            if (embeddedOnly.Count == 0)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Không có tệp phông chữ nhúng nào trong tài liệu để cài đặt.", "No embedded fonts available to install."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = ZenQOffice.UI.FluentMessageBox.Show(this,
                AppL10n.T($"Bạn có muốn cài đặt {embeddedOnly.Count} phông chữ nhúng này vào hệ thống Windows không?\nSau khi cài, phông chữ sẽ sẵn sàng cho mọi phần mềm trên máy tính.", $"Install {embeddedOnly.Count} embedded font(s) for ZenQ Office?\nThey will be available for all presentations and documents."),
                AppL10n.T("Cài đặt phông chữ", "Install Fonts"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string localFontsDir = SlideFontManager.GetAppFontsDirectory();
            int installedCount = 0;

            foreach (var f in embeddedOnly)
            {
                if (f.Data != null && f.Data.Length > 0)
                {
                    string safeName = Path.GetInvalidFileNameChars().Aggregate(f.FileName ?? $"{f.Typeface}.ttf", (current, c) => current.Replace(c, '_'));
                    string destPath = Path.Combine(localFontsDir, safeName);
                    try
                    {
                        File.WriteAllBytes(destPath, f.Data);
                        
                        installedCount++;
                    }
                    catch { }
                }
            }

            ZenQOffice.UI.FluentMessageBox.Show(this,
                AppL10n.T($"Đã cài đặt thành công {installedCount} phông chữ vào hệ thống!", $"Successfully installed {installedCount} font(s)!"),
                AppL10n.T("Cài đặt hoàn tất", "Installation Complete"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Nạp lại dữ liệu
            if (isLiveSlideMode && AppForm.Instance?.SlidesView?.Presentation != null)
            {
                LoadFromActivePresentation(AppForm.Instance.SlidesView.Presentation);
            }
            else if (!string.IsNullOrEmpty(activeDocumentPath))
            {
                LoadDocumentFile(activeDocumentPath);
            }
        }
    }

    /// <summary>Font table drawn on a Direct2D WARP surface; input and selection stay native WinForms.</summary>
    internal sealed class FontToolkitD2DTable : AiGpuControl
    {
        internal sealed class Row
        {
            public readonly ExtractedFontInfo Font;
            public readonly string Status, Source, Suggestion;
            public Row(ExtractedFontInfo font, string status, string source, string suggestion)
            { Font = font; Status = status; Source = source; Suggestion = suggestion; }
        }

        private readonly List<Row> _rows = new List<Row>();
        private int _scrollOffset;
        public int SelectedIndex { get; private set; } = -1;
        public event EventHandler SelectionChanged;
        private const int HeaderHeight = 42, RowHeight = 48;

        public FontToolkitD2DTable()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }

        public void SetRows(IList<Row> rows)
        {
            _rows.Clear();
            if (rows != null) _rows.AddRange(rows);
            SelectedIndex = _rows.Count == 0 ? -1 : 0;
            _scrollOffset = 0;
            Invalidate();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var frame = AiGpuPaint.Begin(this, e))
            {
                var g = frame.Graphics;
                int w = Math.Max(1, Width), h = Math.Max(1, Height);
                using (var white = new SolidBrush(Color.White)) g.FillRectangle(white, 0, 0, w, h);
                using (var header = new SolidBrush(Color.FromArgb(248, 250, 253))) g.FillRectangle(header, 0, 0, w, HeaderHeight);
                using (var line = new Pen(Color.FromArgb(230, 235, 243), 1))
                    g.DrawLine(line, 0, HeaderHeight - 1, w, HeaderHeight - 1);

                float nameX = 20, statusX = w * .42f, sourceX = w * .67f, suggestX = w * .84f;
                DrawLabel(g, AppL10n.T("PHÔNG CHỮ", "FONT"), nameX, 13, 9f, Color.FromArgb(111, 124, 145), true);
                DrawLabel(g, AppL10n.T("TIẾNG VIỆT", "VIETNAMESE"), statusX, 13, 9f, Color.FromArgb(111, 124, 145), true);
                DrawLabel(g, AppL10n.T("NGUỒN", "SOURCE"), sourceX, 13, 9f, Color.FromArgb(111, 124, 145), true);
                DrawLabel(g, AppL10n.T("GỢI Ý", "SUGGESTION"), suggestX, 13, 9f, Color.FromArgb(111, 124, 145), true);

                int visible = Math.Max(0, (h - HeaderHeight) / RowHeight);
                int end = Math.Min(_rows.Count, _scrollOffset + visible + 1);
                for (int i = _scrollOffset; i < end; i++)
                {
                    int y = HeaderHeight + (i - _scrollOffset) * RowHeight;
                    var row = _rows[i];
                    if (i == SelectedIndex)
                    {
                        using (var selected = new SolidBrush(Color.FromArgb(242, 246, 255)))
                            g.FillRoundedRectangle(selected, new RectangleF(6, y + 3, w - 18, RowHeight - 6), 8);
                        using (var accent = new SolidBrush(Color.FromArgb(86, 91, 245)))
                            g.FillRoundedRectangle(accent, new RectangleF(7, y + 12, 3, RowHeight - 24), 2);
                    }
                    using (var separator = new Pen(Color.FromArgb(239, 242, 247), 1))
                        g.DrawLine(separator, 16, y + RowHeight - 1, w - 12, y + RowHeight - 1);

                    DrawLabel(g, row.Font?.Typeface ?? AppL10n.T("(không tên)", "(unnamed)"), nameX, y + 5, 10.2f, Color.FromArgb(28, 39, 57), true, Math.Max(70, statusX - nameX - 20));
                    DrawLabel(g, row.Status, statusX, y + 5, 9f, row.Font != null && row.Font.HasVietnameseSupport ? Color.FromArgb(19, 139, 88) : Color.FromArgb(194, 91, 30), false, Math.Max(70, sourceX - statusX - 10));
                    DrawLabel(g, row.Source, sourceX, y + 5, 9f, row.Font != null && row.Font.IsEmbedded ? Color.FromArgb(83, 75, 207) : Color.FromArgb(104, 117, 137), false, Math.Max(60, suggestX - sourceX - 10));
                    DrawLabel(g, row.Suggestion, suggestX, y + 5, 9f, row.Font != null && !string.IsNullOrEmpty(row.Font.SuggestedVietnameseFont) ? Color.FromArgb(77, 79, 196) : Color.FromArgb(153, 163, 178), false, Math.Max(45, w - suggestX - 24));
                }

                if (_rows.Count == 0)
                {
                    DrawLabel(g, AppL10n.T("Chưa tìm thấy phông chữ phù hợp", "No matching fonts found"), 20, HeaderHeight + 18, 10f, Color.FromArgb(125, 137, 155), false, w - 40);
                }

                if (_rows.Count > visible && visible > 0)
                {
                    float trackH = h - HeaderHeight - 8;
                    float thumbH = Math.Max(24, trackH * visible / _rows.Count);
                    float thumbY = HeaderHeight + 4 + (trackH - thumbH) * _scrollOffset / Math.Max(1, _rows.Count - visible);
                    using (var track = new SolidBrush(Color.FromArgb(246, 248, 251))) g.FillRoundedRectangle(track, new RectangleF(w - 8, HeaderHeight + 4, 4, trackH), 2);
                    using (var thumb = new SolidBrush(Color.FromArgb(197, 207, 223))) g.FillRoundedRectangle(thumb, new RectangleF(w - 8, thumbY, 4, thumbH), 2);
                }
            }
            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            int index = _scrollOffset + (e.Y - HeaderHeight) / RowHeight;
            if (e.Y >= HeaderHeight && index >= 0 && index < _rows.Count && SelectedIndex != index)
            {
                SelectedIndex = index;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int visible = Math.Max(1, (Height - HeaderHeight) / RowHeight);
            int delta = Math.Sign(e.Delta) * Math.Max(1, Math.Abs(e.Delta) / 120);
            _scrollOffset = Math.Max(0, Math.Min(Math.Max(0, _rows.Count - visible), _scrollOffset - delta * 3));
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int next = SelectedIndex;
            if (e.KeyCode == Keys.Down) next++;
            else if (e.KeyCode == Keys.Up) next--;
            else { base.OnKeyDown(e); return; }

            if (_rows.Count > 0)
            {
                SelectedIndex = Math.Max(0, Math.Min(_rows.Count - 1, next));
                int visible = Math.Max(1, (Height - HeaderHeight) / RowHeight);
                if (SelectedIndex < _scrollOffset) _scrollOffset = SelectedIndex;
                else if (SelectedIndex >= _scrollOffset + visible) _scrollOffset = SelectedIndex - visible + 1;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            e.Handled = true;
            base.OnKeyDown(e);
        }

        private static void DrawLabel(D2DGraphics g, string text, float x, float y, float size, Color color, bool bold, float width = 500)
        {
            using (var font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(text ?? string.Empty, font, brush, new RectangleF(x, y, Math.Max(1, width), RowHeight - 10), format);
        }
    }

    /// <summary>DirectWrite font specimen rendered through the same GPU/WARP surface as the table.</summary>
    internal sealed class FontToolkitPreviewSurface : AiGpuControl
    {
        private string _sampleText = "Tiếng Việt thân yêu";
        private string _previewFontName = "Segoe UI";
        private float _previewFontSize = 22f;
        public string SampleText { get => _sampleText; set { _sampleText = value; Invalidate(); } }
        public string PreviewFontName { get => _previewFontName; set { _previewFontName = value; Invalidate(); } }
        public float PreviewFontSize { get => _previewFontSize; set { _previewFontSize = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var frame = AiGpuPaint.Begin(this, e))
            {
                var g = frame.Graphics;
                using (var fill = new SolidBrush(Color.FromArgb(250, 251, 254))) g.FillRoundedRectangle(fill, new RectangleF(0, 0, Width, Height), 10);
                using (var border = new Pen(Color.FromArgb(231, 235, 243), 1)) g.DrawRoundedRectangle(border, new RectangleF(.5f, .5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), 10);
                string name = string.IsNullOrWhiteSpace(PreviewFontName) ? "Segoe UI" : PreviewFontName;
                var style = name.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Heavy", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Black", StringComparison.OrdinalIgnoreCase) >= 0 ? FontStyle.Bold : FontStyle.Regular;
                using (var font = SlideFontManager.CreateFont(name, Math.Max(12, Math.Min(PreviewFontSize, 38)), style, GraphicsUnit.Point))
                using (var brush = new SolidBrush(Color.FromArgb(22, 31, 49)))
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(string.IsNullOrWhiteSpace(SampleText) ? "Tiếng Việt thân yêu" : SampleText, font, brush, new RectangleF(20, 8, Math.Max(1, Width - 40), Math.Max(1, Height - 16)), format);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class FontSamplePresetButton : AiGpuButton
    {
        public FontSamplePresetButton() { BackColor = Color.Transparent; ForeColor = Color.FromArgb(55, 65, 81); FlatStyle = FlatStyle.Flat; }
        protected override void OnPaint(PaintEventArgs e)
        {
            using (var frame = AiGpuPaint.Begin(this, e))
            {
                var g = frame.Graphics;
                var bg = _pressed ? Color.FromArgb(232, 237, 249) : (_hovered ? Color.FromArgb(239, 243, 255) : Color.FromArgb(250, 251, 254));
                using (var fill = new SolidBrush(bg)) g.FillRoundedRectangle(fill, new RectangleF(0, 0, Width, Height), 7);
                using (var line = new Pen(_hovered ? Color.FromArgb(164, 174, 255) : Color.FromArgb(224, 230, 240), 1)) g.DrawRoundedRectangle(line, new RectangleF(.5f,.5f,Width-1,Height-1),7);
                using (var font = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var brush = new SolidBrush(ForeColor))
                using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(Text, font, brush, new RectangleF(3, 1, Width-6, Height-2), fmt);
            }
        }
        private bool _hovered, _pressed;
        protected override void OnMouseEnter(EventArgs e) { _hovered=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered=false; _pressed=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed=true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed=false; Invalidate(); base.OnMouseUp(e); }
    }
}

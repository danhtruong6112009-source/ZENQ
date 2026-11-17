using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Báo lỗi & Đóng góp ý kiến thiết kế chuẩn Windows 11 / Microsoft 365 Fluent Design.
    /// Sử dụng hoàn toàn Segoe Fluent Icons (FluentIconRenderer) và thẻ bo góc sắc nét, không dùng icon GDI/emoji lỗi thời.
    /// </summary>
    public class BugReportDialogForm : Form
    {
        private static readonly Color AccentBlue = Color.FromArgb(15, 108, 189);
        private static readonly Color AccentBlueHover = Color.FromArgb(17, 94, 163);
        private static readonly Color AccentSoftBg = Color.FromArgb(235, 243, 252);
        private static readonly Color AccentSoftBorder = Color.FromArgb(180, 214, 250);
        private static readonly Color CanvasBg = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(218, 224, 232);
        private static readonly Color InkPrimary = Color.FromArgb(30, 41, 59);
        private static readonly Color InkSecondary = Color.FromArgb(100, 116, 139);
        private static readonly Color SuccessGreen = Color.FromArgb(22, 163, 74);
        private static readonly Color ErrorRed = Color.FromArgb(220, 38, 38);

        private readonly AppForm _appForm;
        private string _selectedCategory = "Báo lỗi phần mềm";
        private Image _attachedScreenshot = null;

        private Panel _pnlHeader;
        private Panel _pnlBody;
        private Panel _pnlFooter;

        // Category Cards
        private FluentCategoryCard _btnCatBug;
        private FluentCategoryCard _btnCatFeature;
        private FluentCategoryCard _btnCatGeneral;

        // Input Fields
        private TextBox _txtTitle;
        private TextBox _txtDescription;
        private TextBox _txtContact;
        private TextBox _txtImageUrl;

        // Screenshot Preview Controls
        private Panel _pnlScreenshotCard;
        private Panel _pnlThumbHost;
        private PictureBox _picScreenshotPreview;
        private Label _lblScreenshotStatus;
        private FluentPillButton _btnPasteScreenshot;
        private FluentPillButton _btnBrowseScreenshot;
        private FluentPillButton _btnClearScreenshot;

        // Device Info
        private Panel _pnlDeviceCard;
        private CheckBox _chkIncludeDeviceInfo;
        private Label _lblDeviceInfoPreview;

        // Footer Actions
        private Label _lblStatus;
        private char _statusGlyph = '\uE946';
        private Color _statusGlyphColor = InkSecondary;
        private Panel _pnlStatusIcon;
        private FluentPillButton _btnCancel;
        private FluentPillButton _btnSubmit;
        private bool _isSubmitting = false;

        public BugReportDialogForm(AppForm appForm = null)
        {
            _appForm = appForm;
            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            this.Text = AppL10n.T("Báo lỗi & Đóng góp ý kiến - ZenQ Office", "Bug Report & Feedback - ZenQ Office");
            int workH = Screen.PrimaryScreen?.WorkingArea.Height ?? 768;
            int targetH = Math.Min(684, Math.Max(580, workH - 28));
            this.Size = new Size(608, targetH);
            this.MinimumSize = new Size(560, 560);
            this.StartPosition = (_appForm != null) ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = CanvasBg;
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            try
            {
                this.Icon = ZenQOfficeLogoRenderer.CreateAppIcon(32);
            }
            catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FluentUi.RoundWindow(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.V) && Clipboard.ContainsImage())
            {
                PasteScreenshotFromClipboard();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildUI()
        {
            this.SuspendLayout();

            const int contentLeft = 22;
            int contentW = this.ClientSize.Width - contentLeft * 2; // ~548

            // 1. CLEAN FLUENT LIGHT HEADER
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White
            };
            _pnlHeader.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Top 3px M365 Fluent accent strip
                using (SolidBrush topAccent = new SolidBrush(AccentBlue))
                {
                    g.FillRectangle(topAccent, 0, 0, _pnlHeader.Width, 3);
                }

                // Bottom divider
                using (Pen divPen = new Pen(Color.FromArgb(228, 233, 240), 1f))
                {
                    g.DrawLine(divPen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
                }

                // Soft rounded icon badge
                Rectangle badgeRect = new Rectangle(contentLeft, 15, 40, 40);
                using (GraphicsPath bp = CreateRoundedRectPath(badgeRect, 10))
                using (SolidBrush bFill = new SolidBrush(AccentSoftBg))
                using (Pen bBorder = new Pen(AccentSoftBorder, 1f))
                {
                    g.FillPath(bFill, bp);
                    g.DrawPath(bBorder, bp);
                }

                FluentIconRenderer.DrawGlyph(g, '\uED15', badgeRect, AccentBlue, 20f);

                using (Font titleFont = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold))
                using (SolidBrush bTitle = new SolidBrush(InkPrimary))
                {
                    g.DrawString(AppL10n.T("Báo lỗi & Đóng góp ý kiến", "Bug Report & Feedback"), titleFont, bTitle, contentLeft + 52, 14);
                }

                using (Font subFont = new Font("Segoe UI", 8.75f, FontStyle.Regular))
                using (SolidBrush bSub = new SolidBrush(InkSecondary))
                {
                    g.DrawString(AppL10n.T("Ý kiến của bạn được gửi trực tiếp tới hệ thống phát triển ZenQ Office", "Your feedback goes directly to the ZenQ Office team"), subFont, bSub, contentLeft + 52, 37);
                }
            };
            this.Controls.Add(_pnlHeader);

            // 2. FOOTER ACTION BAR
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Color.White
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(228, 233, 240), 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
                }
            };

            _pnlStatusIcon = new Panel
            {
                Location = new Point(contentLeft, 19),
                Size = new Size(18, 20),
                BackColor = Color.Transparent
            };
            _pnlStatusIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                FluentIconRenderer.DrawGlyph(e.Graphics, _statusGlyph, new Rectangle(0, 1, 16, 18), _statusGlyphColor, 13f);
            };
            _pnlFooter.Controls.Add(_pnlStatusIcon);

            _lblStatus = new Label
            {
                Text = AppL10n.T("Sẵn sàng gửi ý kiến phản hồi", "Ready to submit feedback"),
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                ForeColor = InkSecondary,
                Location = new Point(contentLeft + 22, 18),
                Size = new Size(304, 22),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            _pnlFooter.Controls.Add(_lblStatus);

            _btnCancel = new FluentPillButton
            {
                Text = AppL10n.T("Đóng", "Close"),
                Location = new Point(this.ClientSize.Width - contentLeft - 224, 13),
                Size = new Size(84, 32),
                IsPrimary = false,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            _btnCancel.Click += (s, e) => this.Close();
            _pnlFooter.Controls.Add(_btnCancel);

            _btnSubmit = new FluentPillButton
            {
                Text = AppL10n.T("Gửi báo cáo", "Submit Report"),
                Glyph = '\uE724',
                Location = new Point(this.ClientSize.Width - contentLeft - 132, 13),
                Size = new Size(132, 32),
                IsPrimary = true,
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold)
            };
            _btnSubmit.Click += async (s, e) => await SubmitReportAsync();
            _pnlFooter.Controls.Add(_btnSubmit);

            this.Controls.Add(_pnlFooter);

            // 3. BODY PANEL
            _pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = CanvasBg,
                Padding = new Padding(0, 0, 0, 12)
            };
            this.Controls.Add(_pnlBody);
            _pnlBody.BringToFront();

            int curY = 14;

            // 3.1 LOẠI PHẢN HỒI
            Label lblCat = new Label
            {
                Text = AppL10n.T("LOẠI PHẢN HỒI", "FEEDBACK TYPE"),
                Font = new Font("Segoe UI Semibold", 8.0f, FontStyle.Bold),
                ForeColor = InkSecondary,
                Location = new Point(contentLeft, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblCat);
            curY += 20;

            int pillGap = 10;
            int pillW = (contentW - pillGap * 2) / 3;
            _btnCatBug = CreateCategoryCard('\uEBE8', AppL10n.T("Báo lỗi phần mềm", "Bug Report"), contentLeft, curY, pillW, true);
            _btnCatFeature = CreateCategoryCard('\uEA80', AppL10n.T("Đề xuất tính năng", "Feature Request"), contentLeft + pillW + pillGap, curY, pillW, false);
            _btnCatGeneral = CreateCategoryCard('\uE8BD', AppL10n.T("Trải nghiệm chung", "General Feedback"), contentLeft + (pillW + pillGap) * 2, curY, contentW - (pillW + pillGap) * 2, false);

            _btnCatBug.Click += (s, e) => SelectCategory("Báo lỗi phần mềm", _btnCatBug);
            _btnCatFeature.Click += (s, e) => SelectCategory("Góp ý tính năng", _btnCatFeature);
            _btnCatGeneral.Click += (s, e) => SelectCategory("Trải nghiệm chung", _btnCatGeneral);

            _pnlBody.Controls.Add(_btnCatBug);
            _pnlBody.Controls.Add(_btnCatFeature);
            _pnlBody.Controls.Add(_btnCatGeneral);
            curY += 48;

            // 3.2 TIÊU ĐỀ TÓM TẮT
            _pnlBody.Controls.Add(CreateFieldLabel(AppL10n.T("Tiêu đề tóm tắt *", "Summary Title *"), contentLeft, curY));
            curY += 20;

            var titleWrap = CreateRoundedInputBox(contentLeft, curY, contentW, 34, false, '\0', AppL10n.T("Ví dụ: Không mở được menu chuột phải khi chọn hình...", "Example: Right-click menu does not open on shapes..."), out _txtTitle);
            _pnlBody.Controls.Add(titleWrap);
            curY += 42;

            // 3.3 NỘI DUNG CHI TIẾT
            _pnlBody.Controls.Add(CreateFieldLabel(AppL10n.T("Nội dung phản hồi chi tiết *", "Detailed Description *"), contentLeft, curY));
            curY += 20;

            var descWrap = CreateRoundedInputBox(contentLeft, curY, contentW, 88, true, '\0', AppL10n.T("Mô tả chi tiết lỗi gặp phải hoặc góp ý của bạn...", "Describe the bug or suggestion in detail..."), out _txtDescription);
            _pnlBody.Controls.Add(descWrap);
            curY += 96;

            // 3.4 NGƯỜI GỬI / EMAIL
            Label lblContact = new Label
            {
                Text = AppL10n.T("Người gửi / Email liên hệ (tùy chọn - để nhận phản hồi)", "Name / Contact Email (optional)"),
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                ForeColor = InkSecondary,
                Location = new Point(contentLeft, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblContact);
            curY += 20;

            var contactWrap = CreateRoundedInputBox(contentLeft, curY, contentW, 34, false, '\uE77B', AppL10n.T("Tên hoặc email của bạn...", "Your name or email..."), out _txtContact);
            _txtContact.Text = AppSettings.Instance.FeedbackUserContact ?? "";
            _pnlBody.Controls.Add(contactWrap);
            curY += 44;

            // 3.5 ẢNH CHỤP MÀN HÌNH
            Label lblImage = new Label
            {
                Text = AppL10n.T("Ảnh chụp màn hình", "Screenshot"),
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                ForeColor = InkPrimary,
                Location = new Point(contentLeft, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblImage);

            Label lblHintKey = new Label
            {
                Text = AppL10n.T("Hỗ trợ Win + Shift + S  •  Ctrl + V", "Supports Win + Shift + S  •  Ctrl + V"),
                Font = new Font("Segoe UI", 8.0f, FontStyle.Regular),
                ForeColor = AccentBlue,
                BackColor = Color.Transparent,
                Location = new Point(contentLeft + 132, curY + 1),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblHintKey);
            curY += 22;

            _btnPasteScreenshot = new FluentPillButton
            {
                Text = AppL10n.T("Dán ảnh Clipboard", "Paste Clipboard"),
                Glyph = '\uE77F', // Paste glyph
                Location = new Point(contentLeft, curY),
                Size = new Size(156, 32),
                IsTinted = true,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold)
            };
            _btnPasteScreenshot.Click += (s, e) => PasteScreenshotFromClipboard();
            _pnlBody.Controls.Add(_btnPasteScreenshot);

            _btnBrowseScreenshot = new FluentPillButton
            {
                Text = AppL10n.T("Chọn tệp ảnh...", "Choose Image..."),
                Glyph = '\uEB9F', // Picture glyph
                Location = new Point(contentLeft + 164, curY),
                Size = new Size(134, 32),
                IsPrimary = false,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular)
            };
            _btnBrowseScreenshot.Click += (s, e) => BrowseScreenshotFile();
            _pnlBody.Controls.Add(_btnBrowseScreenshot);

            int urlX = contentLeft + 306;
            var urlWrap = CreateRoundedInputBox(urlX, curY, contentW - 306, 32, false, '\uE71B', AppL10n.T("Hoặc dán link ảnh (Drive, Imgur...)", "Or paste image URL (Drive, Imgur...)"), out _txtImageUrl);
            _txtImageUrl.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
            _pnlBody.Controls.Add(urlWrap);
            curY += 38;

            // Dropzone Screenshot Preview Card
            _pnlScreenshotCard = new RoundedCardPanel
            {
                Location = new Point(contentLeft, curY),
                Size = new Size(contentW, 68),
                BackColor = Color.White,
                BorderColor = CardBorder,
                DashedBorder = true,
                CornerRadius = 8
            };

            _pnlThumbHost = new RoundedCardPanel
            {
                Location = new Point(10, 9),
                Size = new Size(84, 50),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderColor = Color.FromArgb(215, 222, 232),
                CornerRadius = 6
            };
            _pnlThumbHost.Paint += (s, e) =>
            {
                if (_attachedScreenshot == null)
                {
                    FluentIconRenderer.DrawGlyph(e.Graphics, '\uEB9F', new Rectangle(0, 0, _pnlThumbHost.Width, _pnlThumbHost.Height), Color.FromArgb(148, 163, 184), 20f);
                }
            };

            _picScreenshotPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = false
            };
            _pnlThumbHost.Controls.Add(_picScreenshotPreview);
            _pnlScreenshotCard.Controls.Add(_pnlThumbHost);

            _lblScreenshotStatus = new Label
            {
                Text = AppL10n.T("Chưa có ảnh chụp đính kèm.\nNhấn [Win + Shift + S] rồi bấm \"Dán ảnh Clipboard\" hoặc Ctrl + V.", "No screenshot attached.\nPress [Win + Shift + S], then click \"Paste Clipboard\" or press Ctrl + V."),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = InkSecondary,
                Location = new Point(104, 15),
                Size = new Size(contentW - 118, 40)
            };
            _pnlScreenshotCard.Controls.Add(_lblScreenshotStatus);

            _btnClearScreenshot = new FluentPillButton
            {
                Text = AppL10n.T("Xóa", "Clear"),
                Glyph = '\uE74D',
                Location = new Point(contentW - 82, 19),
                Size = new Size(70, 30),
                ForeColor = ErrorRed,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Visible = false
            };
            _btnClearScreenshot.Click += (s, e) => ClearScreenshot();
            _pnlScreenshotCard.Controls.Add(_btnClearScreenshot);

            _pnlBody.Controls.Add(_pnlScreenshotCard);
            curY += 76;

            // 3.6 DEVICE INFO CARD
            _pnlDeviceCard = new RoundedCardPanel
            {
                Location = new Point(contentLeft, curY),
                Size = new Size(contentW, 56),
                BackColor = Color.White,
                BorderColor = CardBorder,
                CornerRadius = 8
            };

            _chkIncludeDeviceInfo = new CheckBox
            {
                Text = AppL10n.T("Tự động gửi thông tin cấu hình máy (OS, RAM, Màn hình, Phiên bản)", "Include system diagnostics (OS, RAM, Display, Version)"),
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                ForeColor = InkPrimary,
                Location = new Point(12, 7),
                Size = new Size(contentW - 24, 22),
                Checked = true,
                BackColor = Color.Transparent
            };
            _pnlDeviceCard.Controls.Add(_chkIncludeDeviceInfo);

            string devSummary = FeedbackService.GetDeviceInfoSummary();
            _lblDeviceInfoPreview = new Label
            {
                Text = devSummary,
                Font = new Font("Segoe UI", 8.0f, FontStyle.Regular),
                ForeColor = InkSecondary,
                Location = new Point(30, 30),
                Size = new Size(contentW - 44, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            ToolTip devTip = new ToolTip();
            devTip.SetToolTip(_lblDeviceInfoPreview, devSummary);
            _pnlDeviceCard.Controls.Add(_lblDeviceInfoPreview);

            _pnlBody.Controls.Add(_pnlDeviceCard);
            curY += 66;

            this.ResumeLayout(true);
        }

        private Label CreateFieldLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                ForeColor = InkPrimary,
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private Panel CreateRoundedInputBox(int x, int y, int width, int height, bool multiline, char leadingGlyph, string placeholder, out TextBox innerBox)
        {
            RoundedInputPanel wrap = new RoundedInputPanel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.White,
                BorderColor = CardBorder,
                FocusBorderColor = AccentBlue,
                CornerRadius = 7
            };

            int textX = (leadingGlyph != '\0') ? 32 : 10;
            if (leadingGlyph != '\0')
            {
                Panel iconSlot = new Panel
                {
                    Location = new Point(8, (height - 20) / 2),
                    Size = new Size(20, 20),
                    BackColor = Color.Transparent
                };
                iconSlot.Paint += (s, e) =>
                {
                    FluentIconRenderer.DrawGlyph(e.Graphics, leadingGlyph, new Rectangle(0, 0, 20, 20), InkSecondary, 13f);
                };
                wrap.Controls.Add(iconSlot);
            }

            innerBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = InkPrimary,
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                Multiline = multiline,
                ScrollBars = ScrollBars.None,
                PlaceholderText = placeholder,
                Location = new Point(textX, multiline ? 8 : Math.Max(6, (height - 19) / 2)),
                Size = new Size(width - textX - 10, multiline ? (height - 16) : 20)
            };

            if (multiline)
            {
                TextBox multiTarget = innerBox;
                multiTarget.TextChanged += (s, e) =>
                {
                    ScrollBars desired = (multiTarget.GetLineFromCharIndex(multiTarget.TextLength) >= 4)
                        ? ScrollBars.Vertical
                        : ScrollBars.None;
                    if (multiTarget.ScrollBars != desired) multiTarget.ScrollBars = desired;
                };
            }

            innerBox.Enter += (s, e) => wrap.SetFocused(true);
            innerBox.Leave += (s, e) => wrap.SetFocused(false);
            wrap.Click += (s, e) => { /* allow clicking wrapper padding */ };

            wrap.Controls.Add(innerBox);
            TextBox capturedBox = innerBox;
            wrap.MouseDown += (s, e) => capturedBox.Focus();
            return wrap;
        }

        private FluentCategoryCard CreateCategoryCard(char glyph, string text, int x, int y, int w, bool isSelected)
        {
            return new FluentCategoryCard
            {
                Glyph = glyph,
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 38),
                IsSelected = isSelected,
                Cursor = Cursors.Hand
            };
        }

        private void SelectCategory(string category, FluentCategoryCard activeCard)
        {
            _selectedCategory = category;
            FluentCategoryCard[] all = new[] { _btnCatBug, _btnCatFeature, _btnCatGeneral };
            foreach (var c in all)
            {
                c.IsSelected = (c == activeCard);
                c.Invalidate();
            }
        }

        private void SetStatus(char glyph, Color color, string text)
        {
            _statusGlyph = glyph;
            _statusGlyphColor = color;
            _lblStatus.Text = text;
            _lblStatus.ForeColor = color;
            _pnlStatusIcon?.Invalidate();
        }

        private void SetAttachedScreenshot(Image img)
        {
            if (img == null) return;
            _attachedScreenshot?.Dispose();
            _attachedScreenshot = new Bitmap(img);
            _picScreenshotPreview.Image = _attachedScreenshot;
            _picScreenshotPreview.Visible = true;
            _pnlThumbHost.Invalidate();

            _lblScreenshotStatus.Text = AppL10n.T($"Đã đính kèm ảnh chụp ({img.Width} x {img.Height} px).\nSẵn sàng gửi kèm báo cáo.", $"Attached screenshot ({img.Width} x {img.Height} px).\nReady to send.");
            _lblScreenshotStatus.ForeColor = SuccessGreen;
            _lblScreenshotStatus.Width = _pnlScreenshotCard.Width - 196;
            _btnClearScreenshot.Visible = true;
        }

        private void ClearScreenshot()
        {
            if (_attachedScreenshot != null)
            {
                _attachedScreenshot.Dispose();
                _attachedScreenshot = null;
            }
            _picScreenshotPreview.Image = null;
            _picScreenshotPreview.Visible = false;
            _pnlThumbHost.Invalidate();

            _lblScreenshotStatus.Text = AppL10n.T("Chưa có ảnh chụp đính kèm.\nNhấn [Win + Shift + S] rồi bấm \"Dán ảnh Clipboard\" hoặc Ctrl + V.", "No screenshot attached.\nPress [Win + Shift + S], then click \"Paste Clipboard\" or press Ctrl + V.");
            _lblScreenshotStatus.ForeColor = InkSecondary;
            _lblScreenshotStatus.Width = _pnlScreenshotCard.Width - 118;
            _btnClearScreenshot.Visible = false;
        }

        private void PasteScreenshotFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    Image img = Clipboard.GetImage();
                    if (img != null)
                    {
                        SetAttachedScreenshot(img);
                        SetStatus('\uE73E', SuccessGreen, AppL10n.T("Đã dán ảnh chụp màn hình thành công!", "Pasted screenshot from Clipboard!"));
                        return;
                    }
                }

                if (Clipboard.ContainsText())
                {
                    string txt = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(txt) &&
                        (txt.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || txt.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    {
                        _txtImageUrl.Text = txt.Trim();
                        SetStatus('\uE71B', AccentBlue, AppL10n.T("Đã dán liên kết ảnh từ Clipboard.", "Pasted image URL from Clipboard."));
                        return;
                    }
                }

                FluentMessageBox.Show(
                    "Clipboard hiện không có ảnh chụp màn hình.\n\nCách làm:\n1. Nhấn [Win + Shift + S] để chụp vùng màn hình.\n2. Quay lại hộp thoại nhấn 'Dán ảnh Clipboard' hoặc Ctrl + V.",
                    AppL10n.T("Chưa có ảnh trong Clipboard", "No Image in Clipboard"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                FluentMessageBox.Show(AppL10n.T("Không thể đọc Clipboard: ", "Could not read Clipboard: ") + ex.Message, AppL10n.T("Lỗi Clipboard", "Clipboard Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BrowseScreenshotFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = AppL10n.T("Chọn ảnh chụp màn hình lỗi", "Select Screenshot Image");
                ofd.Filter = AppL10n.T("Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tất cả tệp (*.*)|*.*", "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*");
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                        {
                            Image img = Image.FromStream(stream);
                            SetAttachedScreenshot(img);
                        }
                    }
                    catch (Exception ex)
                    {
                        FluentMessageBox.Show(AppL10n.T("Không thể mở tệp ảnh: ", "Could not open image file: ") + ex.Message, AppL10n.T("Lỗi tệp", "File Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private string GetFinalImagePayload()
        {
            if (_attachedScreenshot != null)
            {
                return ConvertImageToBase64DataUri(_attachedScreenshot);
            }
            return _txtImageUrl.Text.Trim();
        }

        private static string ConvertImageToBase64DataUri(Image img, int maxWidth = 960, int maxHeight = 720, long quality = 70L)
        {
            if (img == null) return "";
            try
            {
                int w = img.Width;
                int h = img.Height;
                if (w > maxWidth || h > maxHeight)
                {
                    double ratio = Math.Min((double)maxWidth / w, (double)maxHeight / h);
                    w = (int)Math.Max(1, Math.Round(w * ratio));
                    h = (int)Math.Max(1, Math.Round(h * ratio));
                }

                using (Bitmap bmp = new Bitmap(w, h))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(img, new Rectangle(0, 0, w, h));
                    }

                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, jpgEncoder, encoderParams);
                        return "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return codecs[1];
        }

        private async Task SubmitReportAsync()
        {
            if (_isSubmitting) return;

            string title = _txtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                FluentMessageBox.Show(AppL10n.T("Vui lòng nhập Tiêu đề tóm tắt về lỗi hoặc góp ý của bạn.", "Please enter a short summary title."), AppL10n.T("Thiếu thông tin", "Missing Info"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTitle.Focus();
                return;
            }

            string desc = _txtDescription.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
            {
                FluentMessageBox.Show(AppL10n.T("Vui lòng nhập Nội dung phản hồi chi tiết để chúng tôi có thể hỗ trợ bạn tốt nhất.", "Please enter a detailed description."), AppL10n.T("Thiếu thông tin", "Missing Info"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDescription.Focus();
                return;
            }

            _isSubmitting = true;
            _btnSubmit.Enabled = false;
            _btnCancel.Enabled = false;
            SetStatus('\uE895', AccentBlue, AppL10n.T("Đang gửi phản hồi lên hệ thống...", "Submitting report..."));

            string contact = _txtContact.Text.Trim();
            string imgPayload = GetFinalImagePayload();
            string deviceInfo = _chkIncludeDeviceInfo.Checked ? _lblDeviceInfoPreview.Text.Trim() : "Không đính kèm";

            var payload = new FeedbackPayload
            {
                Category = _selectedCategory,
                Title = title,
                Description = desc,
                Contact = contact,
                DeviceInfo = deviceInfo,
                ImageUrl = imgPayload
            };

            var result = await FeedbackService.SendFeedbackAsync(payload);

            if (result.Success)
            {
                SetStatus('\uE73E', SuccessGreen, AppL10n.T("Đã ghi nhận phản hồi thành công!", "Feedback submitted successfully!"));
                try
                {
                    if (!string.IsNullOrWhiteSpace(contact))
                    {
                        AppSettings.Instance.FeedbackUserContact = contact;
                        AppSettings.Instance.Save();
                    }
                }
                catch { }

                await Task.Delay(1100);
                this.Close();
            }
            else
            {
                SetStatus('\uEA39', ErrorRed, result.Message);
                _btnSubmit.Enabled = true;
                _btnCancel.Enabled = true;
                _isSubmitting = false;
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private class RoundedCardPanel : Panel
        {
            public Color BorderColor { get; set; } = CardBorder;
            public int CornerRadius { get; set; } = 8;
            public bool DashedBorder { get; set; } = false;

            public RoundedCardPanel()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRectPath(rect, CornerRadius))
                using (SolidBrush fill = new SolidBrush(BackColor))
                using (Pen pen = new Pen(BorderColor, 1f))
                {
                    if (DashedBorder) pen.DashStyle = DashStyle.Dash;
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(pen, path);
                }
                base.OnPaint(e);
            }
        }

        private sealed class RoundedInputPanel : RoundedCardPanel
        {
            public Color FocusBorderColor { get; set; } = AccentBlue;
            private bool _focused = false;

            public void SetFocused(bool focused)
            {
                if (_focused != focused)
                {
                    _focused = focused;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRectPath(rect, CornerRadius))
                using (SolidBrush fill = new SolidBrush(BackColor))
                using (Pen pen = new Pen(_focused ? FocusBorderColor : BorderColor, _focused ? 1.6f : 1f))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private sealed class FluentCategoryCard : Control
        {
            public char Glyph { get; set; }
            public bool IsSelected { get; set; }
            private bool _hovered = false;

            public FluentCategoryCard()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color bg = IsSelected ? AccentSoftBg : (_hovered ? Color.FromArgb(248, 250, 252) : Color.White);
                Color border = IsSelected ? AccentBlue : (_hovered ? Color.FromArgb(180, 192, 208) : CardBorder);
                Color fg = IsSelected ? AccentBlue : Color.FromArgb(51, 65, 85);

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRectPath(rect, 8))
                using (SolidBrush fill = new SolidBrush(bg))
                using (Pen pen = new Pen(border, IsSelected ? 1.6f : 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }

                // Bottom active pill bar
                if (IsSelected)
                {
                    using (SolidBrush barB = new SolidBrush(AccentBlue))
                    using (GraphicsPath barP = CreateRoundedRectPath(new Rectangle(Width / 2 - 16, Height - 4, 32, 3), 2))
                    {
                        g.FillPath(barB, barP);
                    }
                }

                using (Font f = new Font("Segoe UI Semibold", 9.0f, IsSelected ? FontStyle.Bold : FontStyle.Regular))
                {
                    SizeF txtSize = g.MeasureString(Text ?? "", f);
                    float totalW = 20f + 6f + txtSize.Width;
                    float startX = Math.Max(8f, (Width - totalW) / 2f);

                    FluentIconRenderer.DrawGlyph(g, Glyph, new RectangleF(startX, 0, 20f, Height - 1), fg, 14f);
                    using (SolidBrush txtB = new SolidBrush(fg))
                    using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                    {
                        g.DrawString(Text ?? "", f, txtB, new RectangleF(startX + 24f, 0, Width - startX - 28f, Height - 1), sf);
                    }
                }
            }
        }

        private sealed class FluentPillButton : Control
        {
            public char Glyph { get; set; }
            public bool IsPrimary { get; set; }
            public bool IsTinted { get; set; }
            private bool _hovered = false;
            private bool _pressed = false;

            public FluentPillButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                ForeColor = InkPrimary;
            }

            protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color bg, border, fg;
                if (!Enabled)
                {
                    bg = Color.FromArgb(226, 232, 240);
                    border = Color.FromArgb(203, 213, 225);
                    fg = Color.FromArgb(148, 163, 184);
                }
                else if (IsPrimary)
                {
                    bg = _pressed ? Color.FromArgb(12, 82, 146) : (_hovered ? AccentBlueHover : AccentBlue);
                    border = bg;
                    fg = Color.White;
                }
                else if (IsTinted)
                {
                    bg = _pressed ? Color.FromArgb(215, 232, 250) : (_hovered ? Color.FromArgb(225, 238, 252) : AccentSoftBg);
                    border = Color.FromArgb(147, 197, 253);
                    fg = AccentBlue;
                }
                else
                {
                    bg = _pressed ? Color.FromArgb(236, 240, 245) : (_hovered ? Color.FromArgb(248, 250, 252) : Color.White);
                    border = Color.FromArgb(203, 213, 225);
                    fg = ForeColor;
                }

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRectPath(rect, 6))
                using (SolidBrush fill = new SolidBrush(bg))
                using (Pen pen = new Pen(border, 1f))
                {
                    g.FillPath(fill, path);
                    if (!IsPrimary) g.DrawPath(pen, path);
                }

                SizeF txtSize = g.MeasureString(Text ?? "", Font);
                bool hasGlyph = Glyph != '\0';
                float totalW = (hasGlyph ? 18f + 5f : 0f) + txtSize.Width;
                float startX = Math.Max(6f, (Width - totalW) / 2f);

                if (hasGlyph)
                {
                    FluentIconRenderer.DrawGlyph(g, Glyph, new RectangleF(startX, 0, 18f, Height), fg, 13f);
                    startX += 21f;
                }

                using (SolidBrush txtB = new SolidBrush(fg))
                using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    g.DrawString(Text ?? "", Font, txtB, new RectangleF(startX, 0, Width - startX - 4f, Height), sf);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Chia sẻ & Cộng tác trực tuyến chuẩn Fluent Design 2:
    /// - Tab 1: Cộng tác trực tuyến (Live Co-Edit, lưu mỗi 5s, Ctrl+Z riêng, cho phép Tắt phòng).
    /// - Tab 2: Gửi bản sao (Cloud 200 MB, giữ 30 ngày, tự tải về máy người nhận).
    /// </summary>
    public class CloudShareDialogForm : Form
    {
        private readonly string _currentDocTitle;
        private readonly OfficeAppMode _currentDocMode;
        private readonly Func<string, bool> _exportCurrentDocAction;
        private readonly string _defaultExt;
        private readonly Guid _activeCollabRoomId;
        private readonly string _activeCollabRoomCode;

        private bool _canCollabMode;
        private int _activeModeTab = 0; // 0 = Live Collab, 1 = Send Copy

        private Panel _pnlHeader;
        private Panel _pnlModeTabs;
        private Panel _pnlAccountWarn;

        private Panel _pnlLiveRoomCard;
        private Button _btnCopyRoomCode;
        private Button _btnStopRoom;

        private Panel _pnlQuotaCard;
        private Label _lblQuotaText;
        private Panel _pnlQuotaBarTrack;
        private long _usedBytes = CloudShareService.SenderUsedBytes;

        private Panel _pnlRecipInputWrap;
        private TextBox _txtRecipient;
        private ListBox _lstSuggestions;
        private System.Windows.Forms.Timer _searchDebounce;

        private Panel _pnlSourceCard;
        private RadioButton _radCurrentDoc;
        private RadioButton _radPickFile;
        private Panel _pnlFilePickWrap;
        private TextBox _txtFilePath;
        private Button _btnBrowse;
        private Label _lblFileSizeInfo;

        private Label _lblNoteTitle;
        private Panel _pnlNoteWrap;
        private TextBox _txtNote;

        private ProgressBar _progressBar;
        private Label _lblStatus;
        private Button _btnPrimaryAction;
        private Button _btnCancel;

        private bool _isBusy = false;
        private string _tempExportedFile = null;
        private Rectangle _tabLiveRect = Rectangle.Empty;
        private Rectangle _tabCopyRect = Rectangle.Empty;

        public bool SentSuccessfully { get; private set; } = false;
        public CollabRoomInfo CreatedOrJoinedRoom { get; private set; }
        public bool RoomClosed { get; private set; } = false;

        public CloudShareDialogForm(
            string currentDocTitle = null,
            OfficeAppMode currentDocMode = OfficeAppMode.Writer,
            string defaultExt = ".docx",
            Func<string, bool> exportCurrentDocAction = null,
            string preselectedDiskFile = null,
            Guid activeCollabRoomId = default,
            string activeCollabRoomCode = null)
        {
            _currentDocTitle = CleanDisplayTitle(currentDocTitle);
            _currentDocMode = currentDocMode;
            _defaultExt = string.IsNullOrEmpty(defaultExt) ? ".docx" : defaultExt;
            _exportCurrentDocAction = exportCurrentDocAction;
            _activeCollabRoomId = activeCollabRoomId;
            _activeCollabRoomCode = activeCollabRoomCode ?? "";

            _canCollabMode = (_currentDocMode == OfficeAppMode.Slides ||
                              _currentDocMode == OfficeAppMode.Calc ||
                              _currentDocMode == OfficeAppMode.Writer) && _exportCurrentDocAction != null;

            // If picking a disk file or no open collab-capable doc, default to Send Copy tab
            if ((!string.IsNullOrEmpty(preselectedDiskFile) && File.Exists(preselectedDiskFile)) || !_canCollabMode)
                _activeModeTab = 1;
            else
                _activeModeTab = 0;

            this.DoubleBuffered = true;
            InitializeUi();

            if (!string.IsNullOrEmpty(preselectedDiskFile) && File.Exists(preselectedDiskFile))
            {
                _radPickFile.Checked = true;
                _txtFilePath.Text = preselectedDiskFile;
            }
            else if (_exportCurrentDocAction == null)
            {
                _radCurrentDoc.Enabled = false;
                _radPickFile.Checked = true;
            }

            ApplyModeTabLayout();
            UpdateSourceUiState();
            _ = LoadQuotaAndSuggestionsAsync();
        }

        private static string CleanDisplayTitle(string raw)
        {
            string s = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return "";
            if (s.StartsWith("ZenQAutoSave_", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("ZenQCloud", StringComparison.OrdinalIgnoreCase))
                return AppL10n.T("Tài liệu đang mở", "Current Document");
            return s;
        }

        private static GraphicsPath CreateRoundedPath(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > bounds.Width) d = bounds.Width;
            if (d > bounds.Height) d = bounds.Height;
            if (d < 1f) d = 1f;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void ApplyRoundedRegion(Control ctrl, float radius)
        {
            if (ctrl == null || ctrl.Width <= 0 || ctrl.Height <= 0) return;
            using var path = CreateRoundedPath(new RectangleF(0, 0, ctrl.Width, ctrl.Height), radius);
            ctrl.Region = new Region(path);
        }

        private static void PaintCardBorder(Graphics g, Control card, Color borderCol, float radius = 10f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, card.Width - 1.5f, card.Height - 1.5f);
            using var path = CreateRoundedPath(rect, radius);
            using var bg = new SolidBrush(card.BackColor);
            using var pen = new Pen(borderCol, 1f);
            g.FillPath(bg, path);
            g.DrawPath(pen, path);
        }

        private static Button MakeFluentButton(string text, Point loc, Size sz, Color fill, Color fore, bool outline = false, Color? borderColor = null)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                BackColor = fill,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            if (outline && borderColor.HasValue)
            {
                Color bCol = borderColor.Value;
                btn.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    RectangleF r = new RectangleF(0.5f, 0.5f, btn.Width - 1.5f, btn.Height - 1.5f);
                    using var path = CreateRoundedPath(r, 8f);
                    using var pen = new Pen(bCol, 1.1f);
                    e.Graphics.DrawPath(pen, path);
                };
            }
            btn.SizeChanged += (s, e) => ApplyRoundedRegion(btn, 8f);
            ApplyRoundedRegion(btn, 8f);
            return btn;
        }

        private void InitializeUi()
        {
            this.Text = AppL10n.T("Chia sẻ & Cộng tác", "Share & Collaborate");
            this.ClientSize = new Size(540, 610);
            this.MinimumSize = new Size(540, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);

            // 1. Sleek Gradient Header
            _pnlHeader = new Panel { Dock = DockStyle.Top, Height = 68 };
            _pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, _pnlHeader.Width, _pnlHeader.Height);
                using (var br = new LinearGradientBrush(r, Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216), LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(br, r);

                // Icon circle badge
                RectangleF badge = new RectangleF(18, 15, 38, 38);
                using (var bBr = new SolidBrush(Color.FromArgb(46, 255, 255, 255)))
                    e.Graphics.FillEllipse(bBr, badge);
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE72D', badge, Color.White, 18f);

                using Font titleF = new Font("Segoe UI", 12.5f, FontStyle.Bold);
                using Font subF = new Font("Segoe UI", 8.6f, FontStyle.Regular);
                using SolidBrush wb = new SolidBrush(Color.White);
                using SolidBrush sb = new SolidBrush(Color.FromArgb(219, 234, 254));
                string headTitle = _activeCollabRoomId != Guid.Empty
                    ? AppL10n.T("Phòng cộng tác trực tuyến", "Live Collaboration Room")
                    : AppL10n.T("Chia sẻ tài liệu", "Share Document");
                string subText = string.IsNullOrEmpty(_currentDocTitle)
                    ? AppL10n.T("Chỉnh sửa cùng nhau theo thời gian thực hoặc gửi bản sao", "Co-edit in real time or send a copy")
                    : string.Format(AppL10n.T("Tệp: {0}", "File: {0}"), _currentDocTitle);
                e.Graphics.DrawString(headTitle, titleF, wb, 68f, 13f);
                e.Graphics.DrawString(subText, subF, sb, new RectangleF(68f, 38f, _pnlHeader.Width - 86f, 20f),
                    new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            };
            this.Controls.Add(_pnlHeader);

            // 2. Segmented Mode Switcher Strip
            _pnlModeTabs = new Panel { Location = new Point(20, 78), Size = new Size(500, 40), BackColor = Color.FromArgb(226, 232, 240) };
            _pnlModeTabs.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var bgPath = CreateRoundedPath(new RectangleF(0, 0, _pnlModeTabs.Width, _pnlModeTabs.Height), 10f);
                using var trackBr = new SolidBrush(Color.FromArgb(226, 232, 240));
                e.Graphics.FillPath(trackBr, bgPath);

                int pad = 4;
                int halfW = (_pnlModeTabs.Width - pad * 3) / 2;
                _tabLiveRect = new Rectangle(pad, pad, halfW, _pnlModeTabs.Height - pad * 2);
                _tabCopyRect = new Rectangle(pad * 2 + halfW, pad, halfW, _pnlModeTabs.Height - pad * 2);

                DrawModeSegmentTab(e.Graphics, _tabLiveRect, AppL10n.T("Cộng tác trực tuyến (Live)", "Live Co-Edit"), _activeModeTab == 0, _canCollabMode);
                DrawModeSegmentTab(e.Graphics, _tabCopyRect, AppL10n.T("Gửi bản sao (Cloud 30 ngày)", "Send Copy (30d Cloud)"), _activeModeTab == 1, true);
            };
            _pnlModeTabs.MouseClick += (s, e) =>
            {
                if (_isBusy) return;
                if (_canCollabMode && _tabLiveRect.Contains(e.Location) && _activeModeTab != 0)
                {
                    _activeModeTab = 0;
                    _pnlModeTabs.Invalidate();
                    ApplyModeTabLayout();
                }
                else if (_tabCopyRect.Contains(e.Location) && _activeModeTab != 1)
                {
                    _activeModeTab = 1;
                    _pnlModeTabs.Invalidate();
                    ApplyModeTabLayout();
                }
            };
            this.Controls.Add(_pnlModeTabs);

            // 3. Not-logged-in Warning Card
            _pnlAccountWarn = new Panel
            {
                Location = new Point(20, 126),
                Size = new Size(500, 44),
                BackColor = Color.FromArgb(254, 242, 242),
                Visible = !DatabaseAuthService.IsLoggedIn
            };
            _pnlAccountWarn.Paint += (s, e) => PaintCardBorder(e.Graphics, _pnlAccountWarn, Color.FromArgb(252, 165, 165), 10f);
            Label lblWarn = new Label
            {
                Text = AppL10n.T("Đăng nhập tài khoản ZenQ để sử dụng chia sẻ Cloud.", "Sign in to ZenQ before sharing."),
                Location = new Point(12, 12),
                Size = new Size(340, 20),
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold)
            };
            Button btnLoginNow = MakeFluentButton(AppL10n.T("Đăng nhập", "Sign In"), new Point(372, 7), new Size(116, 30), Color.FromArgb(220, 38, 38), Color.White);
            btnLoginNow.Click += async (s, e) =>
            {
                using (var dlg = new AuthDialogForm()) dlg.ShowDialog(this);
                _pnlAccountWarn.Visible = !DatabaseAuthService.IsLoggedIn;
                ApplyModeTabLayout();
                await LoadQuotaAndSuggestionsAsync();
            };
            _pnlAccountWarn.Controls.Add(lblWarn);
            _pnlAccountWarn.Controls.Add(btnLoginNow);
            this.Controls.Add(_pnlAccountWarn);

            // 4A. Active Live Room Management Card (with Copy Code + Stop/Close Room button)
            _pnlLiveRoomCard = new Panel
            {
                Location = new Point(20, 128),
                Size = new Size(500, 68),
                BackColor = Color.White
            };
            _pnlLiveRoomCard.Paint += (s, e) =>
            {
                Color bdr = _activeCollabRoomId != Guid.Empty ? Color.FromArgb(16, 185, 129) : Color.FromArgb(203, 213, 225);
                PaintCardBorder(e.Graphics, _pnlLiveRoomCard, bdr, 10f);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (_activeCollabRoomId != Guid.Empty)
                {
                    using var dotBr = new SolidBrush(Color.FromArgb(16, 185, 129));
                    e.Graphics.FillEllipse(dotBr, 14, 15, 10, 10);
                    using Font f1 = new Font("Segoe UI", 9.6f, FontStyle.Bold);
                    using Font f2 = new Font("Segoe UI", 8.3f);
                    using SolidBrush b1 = new SolidBrush(Color.FromArgb(6, 95, 70));
                    using SolidBrush b2 = new SolidBrush(Color.FromArgb(71, 85, 105));
                    e.Graphics.DrawString(string.Format(AppL10n.T("Phòng Live đang bật • Mã: {0}", "Live Room Active • Code: {0}"), _activeCollabRoomCode), f1, b1, 30, 10);
                    e.Graphics.DrawString(AppL10n.T("Tự lưu mỗi 5s • Ctrl+Z riêng từng người • Chủ thoát vẫn sửa được", "Auto-saved 5s • Per-user Ctrl+Z • Stays editable"), f2, b2, 14, 37);
                }
                else
                {
                    using Font f1 = new Font("Segoe UI", 9.3f, FontStyle.Bold);
                    using Font f2 = new Font("Segoe UI", 8.2f);
                    using SolidBrush b1 = new SolidBrush(Color.FromArgb(15, 23, 42));
                    using SolidBrush b2 = new SolidBrush(Color.FromArgb(100, 116, 139));
                    e.Graphics.DrawString(AppL10n.T("Chỉnh sửa cùng nhau theo thời gian thực", "Collaborate together in real time"), f1, b1, 14, 11);
                    e.Graphics.DrawString(AppL10n.T("Thấy vị trí đối phương đang làm • Lưu Cloud mỗi 5s • Ctrl+Z độc lập", "See live cursors & selections • Auto-saved 5s • Isolated Ctrl+Z"), f2, b2, 14, 36);
                }
            };
            _btnCopyRoomCode = MakeFluentButton(AppL10n.T("Chép mã", "Copy"), new Point(324, 10), new Size(80, 28), Color.FromArgb(241, 245, 249), Color.FromArgb(30, 41, 59), true, Color.FromArgb(203, 213, 225));
            _btnCopyRoomCode.Font = new Font("Segoe UI", 8.3f, FontStyle.Bold);
            _btnCopyRoomCode.Click += (s, e) =>
            {
                try { if (!string.IsNullOrEmpty(_activeCollabRoomCode)) Clipboard.SetText(_activeCollabRoomCode); } catch { }
                _lblStatus.Text = string.Format(AppL10n.T("Đã sao chép mã phòng {0} vào Clipboard!", "Copied room code {0} to Clipboard!"), _activeCollabRoomCode);
                _lblStatus.ForeColor = Color.FromArgb(5, 150, 105);
            };
            _btnStopRoom = MakeFluentButton(AppL10n.T("Tắt phòng", "Stop Room"), new Point(410, 10), new Size(80, 28), Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28), true, Color.FromArgb(252, 165, 165));
            _btnStopRoom.Font = new Font("Segoe UI", 8.3f, FontStyle.Bold);
            _btnStopRoom.Click += async (s, e) => await ExecuteStopOrLeaveRoomAsync();
            _pnlLiveRoomCard.Controls.Add(_btnCopyRoomCode);
            _pnlLiveRoomCard.Controls.Add(_btnStopRoom);
            this.Controls.Add(_pnlLiveRoomCard);

            // 4B. Cloud Quota Card (for Send Copy tab)
            _pnlQuotaCard = new Panel
            {
                Location = new Point(20, 128),
                Size = new Size(500, 56),
                BackColor = Color.White
            };
            _pnlQuotaCard.Paint += (s, e) => PaintCardBorder(e.Graphics, _pnlQuotaCard, Color.FromArgb(226, 232, 240), 10f);
            _lblQuotaText = new Label
            {
                Text = AppL10n.T("Cloud chia sẻ: Đang kiểm tra... / 200 MB", "Cloud Storage: Checking... / 200 MB"),
                Location = new Point(14, 9),
                Size = new Size(472, 19),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                AutoEllipsis = true
            };
            _pnlQuotaBarTrack = new Panel
            {
                Location = new Point(14, 34),
                Size = new Size(472, 10),
                BackColor = Color.Transparent
            };
            _pnlQuotaBarTrack.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var trackPath = CreateRoundedPath(new RectangleF(0, 0, _pnlQuotaBarTrack.Width, _pnlQuotaBarTrack.Height), 5f);
                using var trackBr = new SolidBrush(Color.FromArgb(226, 232, 240));
                e.Graphics.FillPath(trackBr, trackPath);
                double ratio = Math.Min(1.0, Math.Max(0.0, (double)_usedBytes / CloudShareService.CloudQuotaBytes));
                int fw = (int)Math.Round(_pnlQuotaBarTrack.Width * ratio);
                if (fw > 4)
                {
                    Color barCol = ratio >= 0.99 ? Color.FromArgb(220, 38, 38) : (ratio >= 0.80 ? Color.FromArgb(217, 119, 6) : Color.FromArgb(37, 99, 235));
                    using var fillPath = CreateRoundedPath(new RectangleF(0, 0, fw, _pnlQuotaBarTrack.Height), 5f);
                    using var fillBr = new SolidBrush(barCol);
                    e.Graphics.FillPath(fillBr, fillPath);
                }
            };
            _pnlQuotaCard.Controls.Add(_lblQuotaText);
            _pnlQuotaCard.Controls.Add(_pnlQuotaBarTrack);
            this.Controls.Add(_pnlQuotaCard);

            // 5. Recipient Search Input + Sleek Owner-Drawn Suggestions
            Label lblRecip = new Label
            {
                Name = "lblRecipTitle",
                Text = AppL10n.T("Mời hoặc gửi cho tài khoản:", "Recipient (@username or email):"),
                Location = new Point(20, 200),
                Size = new Size(500, 20),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            this.Controls.Add(lblRecip);

            _pnlRecipInputWrap = new Panel
            {
                Location = new Point(20, 224),
                Size = new Size(500, 36),
                BackColor = Color.White,
                Padding = new Padding(10, 7, 10, 5)
            };
            _pnlRecipInputWrap.Paint += (s, e) =>
            {
                Color c = _txtRecipient != null && _txtRecipient.Focused ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225);
                PaintCardBorder(e.Graphics, _pnlRecipInputWrap, c, 8f);
            };
            _txtRecipient = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = AppL10n.T("Nhập @username hoặc email (để trống nếu chỉ tạo mã phòng)...", "Enter @username or email (optional for room code)...")
            };
            _txtRecipient.GotFocus += (s, e) => _pnlRecipInputWrap.Invalidate();
            _txtRecipient.LostFocus += (s, e) => _pnlRecipInputWrap.Invalidate();
            _pnlRecipInputWrap.Controls.Add(_txtRecipient);
            this.Controls.Add(_pnlRecipInputWrap);

            _lstSuggestions = new ListBox
            {
                Location = new Point(20, 265),
                Size = new Size(500, 114),
                Font = new Font("Segoe UI", 9.0f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 36,
                IntegralHeight = false
            };
            _lstSuggestions.Paint += (s, e) => { };
            _lstSuggestions.DrawItem += DrawSuggestionItem;
            _lstSuggestions.Click += (s, e) =>
            {
                if (_lstSuggestions.SelectedItem is RegisteredRecipientUser u)
                {
                    _txtRecipient.Text = u.Username;
                    _txtRecipient.SelectionStart = _txtRecipient.Text.Length;
                }
            };
            _lstSuggestions.DoubleClick += (s, e) =>
            {
                if (_lstSuggestions.SelectedItem is RegisteredRecipientUser u)
                {
                    _txtRecipient.Text = u.Username;
                    if (_activeModeTab == 1 && _txtNote.Visible) _txtNote.Focus();
                }
            };
            this.Controls.Add(_lstSuggestions);

            _searchDebounce = new System.Windows.Forms.Timer { Interval = 260 };
            _searchDebounce.Tick += async (s, e) =>
            {
                _searchDebounce.Stop();
                await RefreshRecipientSuggestionsAsync(_txtRecipient.Text);
            };
            _txtRecipient.TextChanged += (s, e) =>
            {
                _searchDebounce.Stop();
                _searchDebounce.Start();
            };

            // 6. Document Source Card (Send Copy tab)
            _pnlSourceCard = new Panel
            {
                Location = new Point(20, 386),
                Size = new Size(500, 108),
                BackColor = Color.White
            };
            _pnlSourceCard.Paint += (s, e) => PaintCardBorder(e.Graphics, _pnlSourceCard, Color.FromArgb(226, 232, 240), 10f);

            string curDocLabel = string.IsNullOrEmpty(_currentDocTitle)
                ? AppL10n.T("Tài liệu đang mở hiện tại", "Currently open document")
                : string.Format(AppL10n.T("Tài liệu đang mở: {0}", "Open document: {0}"), _currentDocTitle);
            _radCurrentDoc = new RadioButton
            {
                Text = curDocLabel,
                Location = new Point(14, 10),
                Size = new Size(472, 22),
                Checked = true,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _radCurrentDoc.CheckedChanged += (s, e) => UpdateSourceUiState();
            _pnlSourceCard.Controls.Add(_radCurrentDoc);

            _radPickFile = new RadioButton
            {
                Text = AppL10n.T("Hoặc chọn tệp trên máy (.docx, .xlsx, .pptx, .pdf, ảnh...)", "Or pick a file from this PC (.docx, .xlsx, .pptx, .pdf, image...)"),
                Location = new Point(14, 34),
                Size = new Size(472, 22),
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _radPickFile.CheckedChanged += (s, e) => UpdateSourceUiState();
            _pnlSourceCard.Controls.Add(_radPickFile);

            _pnlFilePickWrap = new Panel
            {
                Location = new Point(34, 60),
                Size = new Size(358, 28),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(8, 5, 8, 4)
            };
            _pnlFilePickWrap.Paint += (s, e) => PaintCardBorder(e.Graphics, _pnlFilePickWrap, Color.FromArgb(203, 213, 225), 7f);
            _txtFilePath = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _pnlFilePickWrap.Controls.Add(_txtFilePath);
            _pnlSourceCard.Controls.Add(_pnlFilePickWrap);

            _btnBrowse = MakeFluentButton(AppL10n.T("Chọn tệp...", "Browse..."), new Point(398, 60), new Size(88, 28), Color.White, Color.FromArgb(30, 41, 59), true, Color.FromArgb(203, 213, 225));
            _btnBrowse.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _btnBrowse.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title = AppL10n.T("Chọn tệp để gửi chia sẻ", "Select File to Share"),
                    Filter = AppL10n.T(
                        "Tài liệu & Hình ảnh (*.docx;*.xlsx;*.pptx;*.pdf;*.png;*.jpg;*.jpeg)|*.docx;*.xlsx;*.pptx;*.pdf;*.png;*.jpg;*.jpeg|Tất cả tệp (*.*)|*.*",
                        "Documents & Images (*.docx;*.xlsx;*.pptx;*.pdf;*.png;*.jpg;*.jpeg)|*.docx;*.xlsx;*.pptx;*.pdf;*.png;*.jpg;*.jpeg|All Files (*.*)|*.*")
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    _radPickFile.Checked = true;
                    _txtFilePath.Text = ofd.FileName;
                    UpdateSourceUiState();
                }
            };
            _pnlSourceCard.Controls.Add(_btnBrowse);

            _lblFileSizeInfo = new Label
            {
                Location = new Point(34, 90),
                Size = new Size(452, 16),
                Font = new Font("Segoe UI", 8.0f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoEllipsis = true
            };
            _pnlSourceCard.Controls.Add(_lblFileSizeInfo);
            this.Controls.Add(_pnlSourceCard);

            // 7. Optional Note (Send Copy)
            _lblNoteTitle = new Label
            {
                Text = AppL10n.T("Lời nhắn (tuỳ chọn):", "Message (optional):"),
                Location = new Point(20, 498),
                Size = new Size(500, 18),
                Font = new Font("Segoe UI", 8.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(_lblNoteTitle);

            _pnlNoteWrap = new Panel
            {
                Location = new Point(20, 518),
                Size = new Size(500, 34),
                BackColor = Color.White,
                Padding = new Padding(10, 7, 10, 5)
            };
            _pnlNoteWrap.Paint += (s, e) => PaintCardBorder(e.Graphics, _pnlNoteWrap, Color.FromArgb(203, 213, 225), 8f);
            _txtNote = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                PlaceholderText = AppL10n.T("Ghi chú ngắn cho người nhận...", "Short note for recipient...")
            };
            _pnlNoteWrap.Controls.Add(_txtNote);
            this.Controls.Add(_pnlNoteWrap);

            // Progress & Status
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 522),
                Size = new Size(500, 10),
                Minimum = 0,
                Maximum = 100,
                Visible = false
            };
            this.Controls.Add(_progressBar);

            _lblStatus = new Label
            {
                Location = new Point(20, 536),
                Size = new Size(500, 18),
                Font = new Font("Segoe UI", 8.3f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoEllipsis = true
            };
            this.Controls.Add(_lblStatus);

            // 8. Footer Bar
            Panel bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
            bottomBar.Paint += (s, e) =>
            {
                using Pen p = new Pen(Color.FromArgb(226, 232, 240));
                e.Graphics.DrawLine(p, 0, 0, bottomBar.Width, 0);
            };
            _btnCancel = MakeFluentButton(AppL10n.T("Đóng", "Close"), new Point(256, 12), new Size(88, 34), Color.FromArgb(248, 250, 252), Color.FromArgb(51, 65, 85), true, Color.FromArgb(203, 213, 225));
            _btnCancel.Click += (s, e) => { if (!_isBusy) this.Close(); };

            _btnPrimaryAction = MakeFluentButton(AppL10n.T("Bắt đầu cộng tác", "Start Live Room"), new Point(352, 12), new Size(168, 34), Color.FromArgb(5, 150, 105), Color.White);
            _btnPrimaryAction.Click += async (s, e) =>
            {
                if (_activeModeTab == 0) await ExecuteStartCollabAsync();
                else await ExecuteSendAsync();
            };

            bottomBar.Controls.Add(_btnCancel);
            bottomBar.Controls.Add(_btnPrimaryAction);
            this.Controls.Add(bottomBar);
        }

        private void DrawModeSegmentTab(Graphics g, Rectangle rect, string text, bool active, bool enabled)
        {
            if (active)
            {
                using var p = CreateRoundedPath(rect, 7f);
                using var br = new SolidBrush(Color.White);
                g.FillPath(br, p);
            }
            Color fg = !enabled
                ? Color.FromArgb(148, 163, 184)
                : (active ? Color.FromArgb(15, 23, 42) : Color.FromArgb(71, 85, 105));
            using Font f = new Font("Segoe UI", 8.8f, active ? FontStyle.Bold : FontStyle.Regular);
            using SolidBrush tb = new SolidBrush(fg);
            using StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(text, f, tb, rect, sf);
        }

        private void DrawSuggestionItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstSuggestions.Items.Count) return;
            var u = _lstSuggestions.Items[e.Index] as RegisteredRecipientUser;
            if (u == null) return;

            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = sel ? Color.FromArgb(239, 246, 255) : Color.White;
            using (SolidBrush br = new SolidBrush(bg)) e.Graphics.FillRectangle(br, e.Bounds);

            // Subtle divider
            using (Pen div = new Pen(Color.FromArgb(241, 245, 249)))
                e.Graphics.DrawLine(div, e.Bounds.X + 44, e.Bounds.Bottom - 1, e.Bounds.Right - 8, e.Bounds.Bottom - 1);

            // Avatar circle with initials
            Color avCol = ColorTranslator.FromHtml(CollabRoomService.GetColorForUser(u.Id));
            Rectangle avRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 6, 24, 24);
            using (SolidBrush abr = new SolidBrush(avCol)) e.Graphics.FillEllipse(abr, avRect);
            string init = !string.IsNullOrWhiteSpace(u.Username) ? u.Username.Substring(0, 1).ToUpperInvariant() : "?";
            using (Font initF = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                e.Graphics.DrawString(init, initF, Brushes.White, avRect, sf);

            string title = string.IsNullOrWhiteSpace(u.FullName) ? $"@{u.Username}" : $"{u.FullName}  (@{u.Username})";
            using Font nf = new Font("Segoe UI", 9.0f, sel ? FontStyle.Bold : FontStyle.Regular);
            using SolidBrush nb = new SolidBrush(sel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(15, 23, 42));
            e.Graphics.DrawString(title, nf, nb, new RectangleF(e.Bounds.X + 42, e.Bounds.Y + 8, e.Bounds.Width - 52, 20),
                new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
        }

        private void ApplyModeTabLayout()
        {
            int y = _pnlAccountWarn.Visible ? 178 : 128;
            bool isLiveTab = (_activeModeTab == 0);

            _pnlLiveRoomCard.Visible = isLiveTab;
            _btnCopyRoomCode.Visible = isLiveTab && _activeCollabRoomId != Guid.Empty;
            _btnStopRoom.Visible = isLiveTab && _activeCollabRoomId != Guid.Empty;

            _pnlQuotaCard.Visible = !isLiveTab;
            _pnlSourceCard.Visible = !isLiveTab;
            _lblNoteTitle.Visible = !isLiveTab;
            _pnlNoteWrap.Visible = !isLiveTab;

            if (isLiveTab)
            {
                _pnlLiveRoomCard.Location = new Point(20, y);
                y += 78;

                var lblR = this.Controls["lblRecipTitle"] as Label;
                if (lblR != null)
                {
                    lblR.Text = _activeCollabRoomId != Guid.Empty
                        ? AppL10n.T("Mời thêm người vào phòng cộng tác:", "Invite another collaborator:")
                        : AppL10n.T("Mời cộng tác viên (@username hoặc email):", "Invite collaborator (@username or email):");
                    lblR.Location = new Point(20, y);
                }
                y += 22;
                _pnlRecipInputWrap.Location = new Point(20, y);
                y += 42;
                _lstSuggestions.Location = new Point(20, y);
                _lstSuggestions.Size = new Size(500, _pnlAccountWarn.Visible ? 136 : 186);
                y += _lstSuggestions.Height + 10;

                _progressBar.Location = new Point(20, y);
                _lblStatus.Location = new Point(20, y + 12);
                _lblStatus.Text = AppL10n.T("Mời trực tiếp theo tài khoản hoặc nhấn Bắt đầu để lấy mã phòng.", "Invite by account or click Start to generate a room code.");
                _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);

                _btnPrimaryAction.BackColor = Color.FromArgb(5, 150, 105);
                _btnPrimaryAction.Text = _activeCollabRoomId != Guid.Empty
                    ? AppL10n.T("Mời vào phòng", "Invite to Room")
                    : AppL10n.T("Bắt đầu cộng tác", "Start Live Room");
                this.ClientSize = new Size(540, y + 98);
            }
            else
            {
                _pnlQuotaCard.Location = new Point(20, y);
                y += 66;

                var lblR = this.Controls["lblRecipTitle"] as Label;
                if (lblR != null)
                {
                    lblR.Text = AppL10n.T("Người nhận (Username hoặc Email):", "Recipient (Username or Email):");
                    lblR.Location = new Point(20, y);
                }
                y += 22;
                _pnlRecipInputWrap.Location = new Point(20, y);
                y += 42;
                _lstSuggestions.Location = new Point(20, y);
                _lstSuggestions.Size = new Size(500, 76);
                y += 84;

                _pnlSourceCard.Location = new Point(20, y);
                y += 116;

                _lblNoteTitle.Location = new Point(20, y);
                y += 20;
                _pnlNoteWrap.Location = new Point(20, y);
                y += 42;

                _progressBar.Location = new Point(20, y);
                _lblStatus.Location = new Point(20, y + 12);
                _lblStatus.Text = AppL10n.T("Tệp lưu giữ trên Cloud 30 ngày • Người nhận không tốn dung lượng.", "Kept on Cloud for 30 days • Zero storage cost to recipients.");
                _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);

                _btnPrimaryAction.BackColor = Color.FromArgb(37, 99, 235);
                _btnPrimaryAction.Text = AppL10n.T("Gửi bản sao", "Send Copy");
                this.ClientSize = new Size(540, y + 98);
            }

            UpdateSourceUiState();
        }

        private async Task LoadQuotaAndSuggestionsAsync()
        {
            if (DatabaseAuthService.IsLoggedIn && DatabaseAuthService.CurrentUser != null)
            {
                try { _usedBytes = await CloudShareService.GetSenderUsedBytesAsync(DatabaseAuthService.CurrentUser.Id); }
                catch { }
            }
            else _usedBytes = 0;

            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke((Action)(() =>
                {
                    RefreshQuotaUi();
                    UpdateSourceUiState();
                }));
            }
            await RefreshRecipientSuggestionsAsync(_txtRecipient.Text);
        }

        private async Task RefreshRecipientSuggestionsAsync(string query)
        {
            try
            {
                var users = await CloudShareService.SearchRegisteredUsersAsync(query, 8);
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((Action)(() =>
                {
                    _lstSuggestions.BeginUpdate();
                    _lstSuggestions.Items.Clear();
                    foreach (var u in users) _lstSuggestions.Items.Add(u);
                    _lstSuggestions.EndUpdate();
                }));
            }
            catch { }
        }

        private void RefreshQuotaUi()
        {
            long remain = Math.Max(0, CloudShareService.CloudQuotaBytes - _usedBytes);
            double pct = Math.Min(100.0, 100.0 * _usedBytes / CloudShareService.CloudQuotaBytes);
            bool isFull = _usedBytes >= CloudShareService.CloudQuotaBytes;

            _lblQuotaText.Text = string.Format(
                AppL10n.T("Cloud chia sẻ: {0} / 200 MB ({1:0.#}% · Còn {2})", "Cloud Storage: {0} / 200 MB ({1:0.#}% · {2} free)"),
                CloudShareService.FormatSize(_usedBytes), pct, CloudShareService.FormatSize(remain));
            _lblQuotaText.ForeColor = isFull ? Color.FromArgb(185, 28, 28) : Color.FromArgb(30, 41, 59);
            _pnlQuotaBarTrack.Invalidate();
        }

        private void UpdateSourceUiState()
        {
            bool pick = _radPickFile.Checked;
            _txtFilePath.Enabled = pick;
            _btnBrowse.Enabled = pick;

            if (_activeModeTab == 0)
            {
                _btnPrimaryAction.Enabled = !_isBusy && DatabaseAuthService.IsLoggedIn;
                return;
            }

            bool quotaFull = _usedBytes >= CloudShareService.CloudQuotaBytes;
            bool sizeExceedsQuota = false;
            if (pick)
            {
                string p = _txtFilePath.Text;
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    long len = new FileInfo(p).Length;
                    long remain = Math.Max(0, CloudShareService.CloudQuotaBytes - _usedBytes);
                    sizeExceedsQuota = len > remain;
                    _lblFileSizeInfo.Text = string.Format(AppL10n.T("Kích thước: {0}{1}", "Size: {0}{1}"),
                        CloudShareService.FormatSize(len),
                        sizeExceedsQuota ? AppL10n.T(" (Vượt dung lượng trống!)", " (Exceeds free quota!)") : "");
                    _lblFileSizeInfo.ForeColor = sizeExceedsQuota ? Color.FromArgb(185, 28, 28) : Color.FromArgb(5, 150, 105);
                }
                else
                {
                    _lblFileSizeInfo.Text = AppL10n.T("Chưa chọn tệp.", "No file selected.");
                    _lblFileSizeInfo.ForeColor = Color.FromArgb(100, 116, 139);
                }
            }
            else
            {
                _lblFileSizeInfo.Text = AppL10n.T("Đóng gói đầy đủ nội dung & hình ảnh trước khi gửi.", "Packs full content & images cleanly before sending.");
                _lblFileSizeInfo.ForeColor = Color.FromArgb(100, 116, 139);
            }

            _btnPrimaryAction.Enabled = !_isBusy && DatabaseAuthService.IsLoggedIn && !quotaFull && !sizeExceedsQuota;
        }

        private async Task ExecuteStopOrLeaveRoomAsync()
        {
            if (_isBusy || _activeCollabRoomId == Guid.Empty) return;

            var confirm = FluentMessageBox.Show(
                AppL10n.T(
                    "Tắt phòng cộng tác trực tuyến trên tài liệu này?\n\n• Nếu bạn là chủ phòng, phòng sẽ đóng cho mọi người.\n• Tài liệu đang mở trên máy bạn vẫn được giữ nguyên.",
                    "Stop live collaboration room for this document?\n\n• If you are the host, the room closes for all peers.\n• Your current open document remains editable locally."),
                AppL10n.T("Tắt phòng cộng tác", "Stop Live Room"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _isBusy = true;
            _btnStopRoom.Enabled = false;
            _btnPrimaryAction.Enabled = false;
            _lblStatus.Text = AppL10n.T("Đang tắt phòng cộng tác...", "Closing collaboration room...");
            try
            {
                await CollabRoomService.LeaveOrDeleteRoomAsync(_activeCollabRoomId);
                RoomClosed = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                FluentMessageBox.Show(ex.Message, AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task ExecuteStartCollabAsync()
        {
            if (_isBusy) return;
            if (!DatabaseAuthService.IsLoggedIn)
            {
                FluentMessageBox.Show(AppL10n.T("Vui lòng đăng nhập trước.", "Please sign in first."), AppL10n.T("Yêu cầu đăng nhập", "Sign In"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string recipientInput = _txtRecipient.Text.Trim();
            _isBusy = true;
            _btnPrimaryAction.Enabled = false;
            _progressBar.Visible = true;
            _progressBar.Value = 30;
            _lblStatus.Text = AppL10n.T("Đang xử lý phòng cộng tác...", "Preparing collaboration room...");
            _lblStatus.ForeColor = Color.FromArgb(5, 150, 105);

            try
            {
                if (_activeCollabRoomId != Guid.Empty)
                {
                    if (string.IsNullOrEmpty(recipientInput))
                    {
                        FluentMessageBox.Show(AppL10n.T("Vui lòng chọn hoặc nhập @username người muốn mời thêm.", "Please select or enter @username to invite."),
                            AppL10n.T("Thiếu người nhận", "Missing User"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    var (ok, msg) = await CollabRoomService.InviteMemberAsync(_activeCollabRoomId, recipientInput);
                    _progressBar.Value = 100;
                    FluentMessageBox.Show(msg, AppL10n.T("Mời cộng tác", "Invite"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    if (ok) { this.DialogResult = DialogResult.OK; this.Close(); }
                    return;
                }

                if (_exportCurrentDocAction == null)
                {
                    FluentMessageBox.Show(AppL10n.T("Chỉ bật cộng tác được trên tài liệu đang mở.", "Requires an open document."), "ZenQ Cloud", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string safeTitle = string.IsNullOrWhiteSpace(_currentDocTitle) ? "Tai_Lieu_Live" : Path.GetFileNameWithoutExtension(_currentDocTitle);
                foreach (char ch in Path.GetInvalidFileNameChars()) safeTitle = safeTitle.Replace(ch, '_');
                string ext = _defaultExt.StartsWith(".") ? _defaultExt : "." + _defaultExt;
                string tempDir = Path.Combine(Path.GetTempPath(), "ZenQCollabInit_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                _tempExportedFile = Path.Combine(tempDir, safeTitle + ext);

                if (!_exportCurrentDocAction(_tempExportedFile) || !File.Exists(_tempExportedFile))
                {
                    FluentMessageBox.Show(AppL10n.T("Không thể đóng gói tài liệu hiện tại.", "Failed to pack active document."), AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] docBytes = await File.ReadAllBytesAsync(_tempExportedFile);
                _progressBar.Value = 70;

                var members = new List<RegisteredRecipientUser>();
                if (!string.IsNullOrEmpty(recipientInput))
                {
                    var target = await CloudShareService.FindRegisteredUserAsync(recipientInput);
                    if (target == null)
                    {
                        FluentMessageBox.Show(AppL10n.T("Không tìm thấy tài khoản người nhận.", "Recipient not found."), AppL10n.T("Không tìm thấy", "Not Found"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    members.Add(target);
                }

                var created = await CollabRoomService.CreateRoomAsync(safeTitle + ext, _currentDocMode, docBytes, members);
                _progressBar.Value = 100;
                if (created.Success && created.Room != null)
                {
                    CreatedOrJoinedRoom = created.Room;
                    SentSuccessfully = true;
                    try { Clipboard.SetText(created.Room.RoomCode); } catch { }
                    string extra = members.Count > 0 ? string.Format(AppL10n.T("\nĐã mời @{0} vào sửa chung.", "\nInvited @{0}."), members[0].Username) : "";
                    FluentMessageBox.Show(
                        string.Format(AppL10n.T("Đã bật phòng Live! Mã phòng: {0} (đã chép){1}\n• Tự lưu Cloud mỗi 5s  • Ctrl+Z tách riêng từng người", "Live Room started! Code: {0} (copied){1}\n• Auto-saved 5s  • Isolated Ctrl+Z"), created.Room.RoomCode, extra),
                        AppL10n.T("Cộng tác trực tuyến", "Live Co-Editing"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    FluentMessageBox.Show(created.Message, AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                CleanupTempExport();
                _isBusy = false;
                if (!IsDisposed) { _progressBar.Visible = false; UpdateSourceUiState(); }
            }
        }

        private async Task ExecuteSendAsync()
        {
            if (_isBusy) return;
            if (!DatabaseAuthService.IsLoggedIn)
            {
                FluentMessageBox.Show(AppL10n.T("Vui lòng đăng nhập trước.", "Please sign in first."), AppL10n.T("Yêu cầu đăng nhập", "Sign In"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_usedBytes >= CloudShareService.CloudQuotaBytes)
            {
                FluentMessageBox.Show(AppL10n.T("Ổ đĩa Cloud 200 MB đã đầy. Hãy xóa bớt tệp cũ trong mục Được chia sẻ.", "200 MB Cloud quota is full."), AppL10n.T("Đầy dung lượng", "Quota Full"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string recipient = _txtRecipient.Text.Trim();
            if (string.IsNullOrEmpty(recipient))
            {
                FluentMessageBox.Show(AppL10n.T("Vui lòng nhập @username hoặc email người nhận.", "Please enter recipient @username or email."), AppL10n.T("Thiếu người nhận", "Missing Recipient"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtRecipient.Focus();
                return;
            }

            string pathToSend;
            OfficeAppMode modeToSend;
            if (_radPickFile.Checked)
            {
                pathToSend = _txtFilePath.Text.Trim();
                if (string.IsNullOrEmpty(pathToSend) || !File.Exists(pathToSend))
                {
                    FluentMessageBox.Show(AppL10n.T("Vui lòng chọn tệp hợp lệ.", "Please choose a valid file."), AppL10n.T("Thiếu tệp", "Missing File"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                modeToSend = CloudShareService.ResolveDocMode(pathToSend);
            }
            else
            {
                if (_exportCurrentDocAction == null)
                {
                    FluentMessageBox.Show(AppL10n.T("Không có tài liệu đang mở.", "No open document."), "ZenQ Cloud", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string safeTitle = string.IsNullOrWhiteSpace(_currentDocTitle) ? "Tai_Lieu_ZenQ" : Path.GetFileNameWithoutExtension(_currentDocTitle);
                foreach (char ch in Path.GetInvalidFileNameChars()) safeTitle = safeTitle.Replace(ch, '_');
                string ext = _defaultExt.StartsWith(".") ? _defaultExt : "." + _defaultExt;
                string tempDir = Path.Combine(Path.GetTempPath(), "ZenQCloudSend_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                _tempExportedFile = Path.Combine(tempDir, safeTitle + ext);
                if (!_exportCurrentDocAction(_tempExportedFile) || !File.Exists(_tempExportedFile))
                {
                    FluentMessageBox.Show(AppL10n.T("Không thể đóng gói tài liệu để gửi.", "Failed to export document."), AppL10n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                pathToSend = _tempExportedFile;
                modeToSend = _currentDocMode;
            }

            _isBusy = true;
            _btnPrimaryAction.Enabled = false;
            _btnBrowse.Enabled = false;
            _progressBar.Visible = true;
            _progressBar.Value = 5;

            var progress = new Progress<(int pct, string msg)>(p =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                _progressBar.Value = Math.Clamp(p.pct, 0, 100);
                _lblStatus.Text = p.msg;
                _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
            });

            var result = await CloudShareService.SendFileAsync(recipient, pathToSend, modeToSend, _txtNote.Text.Trim(), progress);
            CleanupTempExport();
            _isBusy = false;
            _usedBytes = CloudShareService.SenderUsedBytes;
            RefreshQuotaUi();
            UpdateSourceUiState();

            if (result.Success)
            {
                SentSuccessfully = true;
                FluentMessageBox.Show(result.Message, AppL10n.T("Đã gửi", "Sent"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _progressBar.Visible = false;
                _lblStatus.Text = result.Message.Replace("\n", " ");
                _lblStatus.ForeColor = Color.FromArgb(185, 28, 28);
                FluentMessageBox.Show(result.Message, AppL10n.T("Không thể gửi tệp", "Send Failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CleanupTempExport()
        {
            if (!string.IsNullOrEmpty(_tempExportedFile))
            {
                try
                {
                    string dir = Path.GetDirectoryName(_tempExportedFile);
                    if (File.Exists(_tempExportedFile)) File.Delete(_tempExportedFile);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                catch { }
                _tempExportedFile = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchDebounce?.Dispose();
                CleanupTempExport();
            }
            base.Dispose(disposing);
        }
    }
}

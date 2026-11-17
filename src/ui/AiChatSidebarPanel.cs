using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Khung Chat Trá»£ lÃ½ AI Hiá»‡n Äáº¡i (AI Sidebar).
    /// - User ở bên PHẢI, AI ở bên TRÁI
    /// - Nút gửi vẽ vector polygon mũi tên (không dùng font text) + Anti-Alias tuyệt đối
    /// - Tin nhắn chào random khi mở
    /// - Animation dots "đang suy nghĩ..." chạy liên tục
    /// </summary>
    public class AiChatSidebarPanel : UserControl
    {
        private AppForm _appForm;
        private AiConfig _config;
        private AiKeyProfile _currentProfile;
        private DocTab _currentTab;

        public DocTab CurrentTab { get { return _currentTab; } }

        // UI Controls
        private Panel pnlHeader;
        private Label lblTitle;
        private ModernPillComboBox cboKey;
        private ModernPillComboBox cboModel;
        private Button btnIslandToggle;
        private Button btnSettings;
        private Button btnHistory;
        private Button btnNewChat;
        private Button btnClose;
        private ToolTip _headerToolTip;

        private bool _isIslandMode = false;
        public bool IsIslandMode
        {
            get => _isIslandMode;
            set
            {
                if (_isIslandMode != value)
                {
                    _isIslandMode = value;
                    AppSettings.Instance.AiChatIslandMode = value;
                    AppSettings.Instance.Save();
                    UpdateIslandStyle();
                    if (_appForm != null)
                    {
                        _appForm.PositionAiSidebar();
                        _appForm.Invalidate();
                        if (_appForm.WriterView != null) _appForm.WriterView.InvalidateLayoutAndTextCache();
                        if (_appForm.SlidesView != null) _appForm.SlidesView.Invalidate();
                    }
                }
            }
        }
        public bool UsesRoundedFrame => _isIslandMode || AppSettings.Instance.RibbonStyle == RibbonStyle.Island;
        public bool HasCustomPosition { get; set; } = false;
        private bool _isDraggingIsland = false;
        private Point _islandDragStartCursor;
        private Point _islandDragStartLocation;

        private string _currentSessionId = Guid.NewGuid().ToString("N");
        private static readonly Dictionary<string, ActiveAiGeneration> _activeGenerations = new Dictionary<string, ActiveAiGeneration>();

        private VirtualChatFeedPanel pnlChatFeed;
        private Panel pnlInputContainer;
        private Panel pnlInputPill;
        private TextBox txtInput;
        private AiSlashCommandPopup _slashPopup;

        // Interactive Canvas Panel (100% Direct2D / DirectX 11)
        private AiCanvasControl _canvasControl;
        private AiCanvasQuizData _lastCanvasQuiz;
        public bool IsCanvasOpen => _canvasControl != null && _canvasControl.Visible;

        // Thinking animation
        private Control _thinkingBubble;
        private ThinkingBubbleControl _currentThinkingControl;

        private Rectangle _sendBtnRect;
        private bool _isSendHover = false;
        private bool _isSendPressed = false;

        private bool _isBinding = false;
        private string _placeholderText => AppL10n.T("Hỏi AI hoặc gõ / để ra lệnh... (Enter)", "Ask AI or type / for commands... (Enter)");
        private bool _isPlaceholderActive = true;

                private static readonly string[] _greetings = new string[]
        {
            AppL10n.T("✦ Xin chào! Tôi là Trợ lý AI — hãy cho tôi biết bạn cần gì nhé!", "✦ Hello! I'm ZenQ AI Assistant — how can I help today?"),
            AppL10n.T("✦ Chào bạn! Tôi sẵn sàng hỗ trợ — hãy gõ yêu cầu của bạn!", "✦ Hi there! I'm ready to help — type your request!"),
            AppL10n.T("✦ Hi! Bạn cần thiết kế slide, viết văn bản hay phân tích dữ liệu?", "✦ Hi! Need help with slides, documents, or data?"),
            AppL10n.T("✦ Xin chào! Hãy mô tả yêu cầu và tôi sẽ thực hiện ngay!", "✦ Hello! Describe what you need and I'll do it right away!"),
            AppL10n.T("✦ Chào! Tôi có thể giúp gì cho bạn hôm nay?", "✦ Hi! What can I help you with today?")
        };

        public AiChatSidebarPanel(AppForm appForm)
        {
            _appForm = appForm;
            _config = AiConfig.Load();
            _currentProfile = _config.GetActiveProfile();

            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.Size = new Size(360, 560);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);

            _isIslandMode = AppSettings.Instance.AiChatIslandMode;
            InitializeComponents();
            ReloadProfilesAndModels();
            UpdateIslandStyle();
            AppSettings.Instance.SettingsChanged += OnAppearanceChanged;
        }

        private void OnAppearanceChanged(object sender, EventArgs e) => UpdateIslandStyle();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppSettings.Instance.SettingsChanged -= OnAppearanceChanged;
                _headerToolTip?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void SafeSetToolTip(Control c, string tip)
        {
            try
            {
                if (_headerToolTip != null && c != null && !c.IsDisposed)
                {
                    _headerToolTip.SetToolTip(c, tip);
                }
            }
            catch { }
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();
            this.Controls.Clear();

            // ==========================================
            // 1. HEADER PANEL
            // ==========================================
            pnlHeader = new AiGpuPanel
            {
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 6)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                AppIconRenderer.DrawAiIcon(g, 12, 11, 20);
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    g.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            Image aiIconImg = AppIconRenderer.GetScaledAiBitmap(26);
            lblTitle = new Label
            {
                Text = AppL10n.T("Trợ lý AI", "AI Assistant"),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(aiIconImg != null ? 36 : 12, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            // Header dragging for Island Mode
            pnlHeader.MouseDown += (s, e) =>
            {
                if (_isIslandMode && e.Button == MouseButtons.Left)
                {
                    // Do not drag if clicking buttons or dropdowns
                    if (cboKey != null && cboKey.Bounds.Contains(e.Location)) return;
                    if (cboModel != null && cboModel.Bounds.Contains(e.Location)) return;
                    if (btnSettings != null && btnSettings.Bounds.Contains(e.Location)) return;
                    if (btnHistory != null && btnHistory.Bounds.Contains(e.Location)) return;
                    if (btnNewChat != null && btnNewChat.Bounds.Contains(e.Location)) return;
                    if (btnClose != null && btnClose.Bounds.Contains(e.Location)) return;
                    if (btnIslandToggle != null && btnIslandToggle.Bounds.Contains(e.Location)) return;

                    _isDraggingIsland = true;
                    _islandDragStartCursor = Cursor.Position;
                    _islandDragStartLocation = this.Location;
                    pnlHeader.Capture = true;
                }
            };
            pnlHeader.MouseMove += (s, e) =>
            {
                if (_isDraggingIsland && this.Parent != null)
                {
                    int dx = Cursor.Position.X - _islandDragStartCursor.X;
                    int dy = Cursor.Position.Y - _islandDragStartCursor.Y;
                    int newX = _islandDragStartLocation.X + dx;
                    int newY = _islandDragStartLocation.Y + dy;
                    int minX = 10;
                    int maxX = Math.Max(minX, this.Parent.ClientSize.Width - this.Width - 10);
                    int minY = 60;
                    int maxY = Math.Max(minY, this.Parent.ClientSize.Height - 60);
                    newX = Math.Max(minX, Math.Min(maxX, newX));
                    newY = Math.Max(minY, Math.Min(maxY, newY));
                    this.Location = new Point(newX, newY);
                    this.HasCustomPosition = true;
                    this.Parent?.Invalidate();
                }
            };
            pnlHeader.MouseUp += (s, e) =>
            {
                if (_isDraggingIsland)
                {
                    _isDraggingIsland = false;
                    pnlHeader.Capture = false;
                }
            };

            // Nút Chuyển đổi Island Mode / Sidebar
            btnIslandToggle = new AiGpuButton
            {
                Text = "",
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnIslandToggle.FlatAppearance.BorderSize = 0;
            btnIslandToggle.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnIslandToggle.PointToClient(Cursor.Position);
                bool isHover = btnIslandToggle.ClientRectangle.Contains(pt);
                Color col = isHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139);
                if (isHover)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    using (GraphicsPath gp = AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(1, 1, btnIslandToggle.Width - 2, btnIslandToggle.Height - 2), 5))
                    {
                        g.FillPath(hb, gp);
                    }
                }
                if (_isIslandMode)
                {
                    // Icon docked sidebar
                    using (Pen p = new Pen(col, 1.3f))
                    {
                        g.DrawRectangle(p, 5, 5, 15, 14);
                        g.DrawLine(p, 14, 5, 14, 19);
                    }
                    using (SolidBrush sb = new SolidBrush(col))
                    {
                        g.FillRectangle(sb, 15, 6, 4, 12);
                    }
                }
                else
                {
                    // Icon floating island card
                    using (Pen p = new Pen(col, 1.3f))
                    {
                        using (GraphicsPath gp = RoundRect(new Rectangle(5, 5, 15, 14), 3))
                        {
                            g.DrawPath(p, gp);
                        }
                        g.DrawLine(p, 5, 9, 20, 9);
                    }
                    using (SolidBrush sb = new SolidBrush(col))
                    {
                        g.FillEllipse(sb, 7f, 6.5f, 2f, 2f);
                        g.FillEllipse(sb, 10f, 6.5f, 2f, 2f);
                    }
                }
            };
            btnIslandToggle.Click += (s, e) =>
            {
                this.IsIslandMode = !this.IsIslandMode;
                btnIslandToggle.Invalidate();
                SafeSetToolTip(btnIslandToggle, _isIslandMode
                    ? AppL10n.T("Chuyển sang Gắn cạnh (Docked - Đẩy khổ slide/tài liệu)", "Switch to Docked Sidebar Mode")
                    : AppL10n.T("Chuyển sang Chế độ Nổi (Floating Island - Không đẩy khổ slide/tài liệu)", "Switch to Floating Island Mode"));
            };
            pnlHeader.Controls.Add(btnIslandToggle);


            btnSettings = new AiGpuButton
            {
                Text = "",
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnSettings.PointToClient(Cursor.Position);
                Color col = btnSettings.ClientRectangle.Contains(pt)
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(100, 116, 139);
                FluentIconRenderer.DrawGlyph(g, '\uE713', new RectangleF(3, 3, 20, 20), col, 14f);
            };
            btnSettings.Click += (s, e) =>
            {
                using (var dlg = new AiConfigDialog())
                {
                    if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
                    {
                        ReloadProfilesAndModels();
                    }
                }
            };
            pnlHeader.Controls.Add(btnSettings);

            btnHistory = new AiGpuButton
            {
                Text = "",
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnHistory.FlatAppearance.BorderSize = 0;
            btnHistory.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnHistory.PointToClient(Cursor.Position);
                Color col = btnHistory.ClientRectangle.Contains(pt)
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(100, 116, 139);
                FluentIconRenderer.DrawGlyph(g, '\uE81C', new RectangleF(3, 3, 20, 20), col, 14f);
            };
            btnHistory.Click += (s, e) => ShowHistoryDialog();
            pnlHeader.Controls.Add(btnHistory);

            btnNewChat = new AiGpuButton
            {
                Text = "",
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnNewChat.FlatAppearance.BorderSize = 0;
            btnNewChat.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnNewChat.PointToClient(Cursor.Position);
                Color col = btnNewChat.ClientRectangle.Contains(pt)
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(100, 116, 139);
                FluentIconRenderer.DrawGlyph(g, '\uE710', new RectangleF(3, 3, 20, 20), col, 14f);
            };
            btnNewChat.Click += (s, e) => StartNewChatSession();
            pnlHeader.Controls.Add(btnNewChat);

            btnClose = new AiGpuButton
            {
                Text = "",
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnClose.PointToClient(Cursor.Position);
                bool isHover = btnClose.ClientRectangle.Contains(pt);
                if (isHover)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(254, 226, 226)))
                    using (GraphicsPath gp = AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(1, 1, btnClose.Width - 2, btnClose.Height - 2), 5))
                    {
                        g.FillPath(hb, gp);
                    }
                }
                Color col = isHover ? Color.FromArgb(220, 38, 38) : Color.FromArgb(100, 116, 139);
                FluentIconRenderer.DrawGlyph(g, '\uE711', new RectangleF(3, 3, 20, 20), col, 12f);
            };
            btnClose.Click += (s, e) =>
            {
                if (_currentTab != null) _currentTab.IsAiChatOpen = false;
                this.Visible = false;
                _appForm?.Invalidate();
            };
            pnlHeader.Controls.Add(btnClose);

            _headerToolTip = new ToolTip { AutoPopDelay = 4000, InitialDelay = 300, ReshowDelay = 200 };
            SafeSetToolTip(btnIslandToggle, _isIslandMode
                ? AppL10n.T("Chuyển sang Gắn cạnh (Docked - Đẩy khổ slide/tài liệu)", "Switch to Docked Sidebar Mode")
                : AppL10n.T("Chuyển sang Chế độ Nổi (Floating Island - Không đẩy khổ slide/tài liệu)", "Switch to Floating Island Mode"));
            SafeSetToolTip(btnSettings, AppL10n.T("Cấu hình AI & API Key", "AI & API Key Settings"));
            SafeSetToolTip(btnHistory, AppL10n.T("Lịch sử trò chuyện", "Chat History"));
            SafeSetToolTip(btnNewChat, AppL10n.T("Cuộc trò chuyện mới", "New Chat"));
            SafeSetToolTip(btnClose, AppL10n.T("Đóng Trợ lý AI", "Close AI Assistant"));

            cboKey = new ModernPillComboBox();
            cboKey.SelectedIndexChanged += (s, e) => OnKeySelectionChanged();
            pnlHeader.Controls.Add(cboKey);

            cboModel = new ModernPillComboBox();
            cboModel.SelectedIndexChanged += (s, e) => OnModelSelectionChanged();
            pnlHeader.Controls.Add(cboModel);

            this.Controls.Add(pnlHeader);

            // ==========================================
            // 2. CHAT FEED FLOWPANEL
            // ==========================================
            pnlChatFeed = new VirtualChatFeedPanel { Dock = DockStyle.Fill };
            this.Controls.Add(pnlChatFeed);

            // ==========================================
            // 3. INPUT CONTAINER PANEL
            // ==========================================
            pnlInputContainer = new AiGpuPanel
            {
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlInputContainer.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    g.DrawLine(p, 0, 0, pnlInputContainer.Width, 0);
                }
            };

            pnlInputPill = new AiGpuPanel
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlInputPill.Paint += PaintInputPill;

            pnlInputPill.MouseMove += (s, e) =>
            {
                bool inside = _sendBtnRect.Contains(e.Location);
                if (inside != _isSendHover)
                {
                    _isSendHover = inside;
                    pnlInputPill.Cursor = inside ? Cursors.Hand : Cursors.Default;
                    pnlInputPill.Invalidate();
                }
            };

            pnlInputPill.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && _sendBtnRect.Contains(e.Location))
                {
                    _isSendPressed = true;
                    pnlInputPill.Invalidate();
                }
            };

            pnlInputPill.MouseUp += (s, e) =>
            {
                if (_isSendPressed)
                {
                    _isSendPressed = false;
                    pnlInputPill.Invalidate();
                    if (_sendBtnRect.Contains(e.Location))
                    {
                        if (_isAiStreaming)
                        {
                            _abortAiStream = true;
                        }
                        else
                        {
                            SendMessageFromInput();
                        }
                    }
                }
            };

            txtInput = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = _placeholderText
            };
            txtInput.GotFocus += (s, e) =>
            {
                if (_isPlaceholderActive)
                {
                    txtInput.Text = "";
                    txtInput.ForeColor = Color.FromArgb(15, 23, 42);
                    _isPlaceholderActive = false;
                }
                pnlInputPill.Invalidate();
            };
            txtInput.LostFocus += (s, e) =>
            {
                if (_slashPopup != null && _slashPopup.Visible)
                {
                    Point pt = this.PointToClient(Cursor.Position);
                    if (_slashPopup.Bounds.Contains(pt))
                    {
                        return;
                    }
                    _slashPopup.Close();
                }
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    txtInput.Text = _placeholderText;
                    txtInput.ForeColor = Color.FromArgb(148, 163, 184);
                    _isPlaceholderActive = true;
                }
                pnlInputPill.Invalidate();
            };

            _slashPopup = new AiSlashCommandPopup();
            _slashPopup.Visible = false;
            _slashPopup.CommandSelected += HandleSlashCommandSelected;
            this.Controls.Add(_slashPopup);
            _slashPopup.BringToFront();

            txtInput.TextChanged += (s, e) =>
            {
                if (_isPlaceholderActive) return;
                string curText = txtInput.Text ?? "";
                if (curText.StartsWith("/"))
                {
                    int spaceIdx = curText.IndexOf(' ');
                    if (spaceIdx >= 0)
                    {
                        if (_slashPopup != null && _slashPopup.Visible) _slashPopup.Close();
                        return;
                    }
                    string prefix = curText;
                    var matchingCmds = AiSlashCommandEngine.FilterCommands(prefix);
                    if (matchingCmds.Count > 0)
                    {
                        _slashPopup.UpdateList(matchingCmds);
                        if (pnlInputContainer != null)
                        {
                            int chatW = IsCanvasOpen ? 380 : this.ClientSize.Width;
                            int popupW = Math.Max(260, chatW - 20);
                            int popupH = _slashPopup.Height;
                            int popupY = pnlInputContainer.Top - popupH - 2;
                            _slashPopup.Bounds = new Rectangle(10, popupY, popupW, popupH);
                            _slashPopup.Visible = true;
                            _slashPopup.BringToFront();
                        }
                    }
                    else
                    {
                        if (_slashPopup != null && _slashPopup.Visible) _slashPopup.Close();
                    }
                }
                else
                {
                    if (_slashPopup != null && _slashPopup.Visible) _slashPopup.Close();
                }
            };

            txtInput.KeyDown += (s, e) =>
            {
                if (_slashPopup != null && _slashPopup.Visible)
                {
                    if (e.KeyCode == Keys.Down)
                    {
                        _slashPopup.SelectNext();
                        e.SuppressKeyPress = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Up)
                    {
                        _slashPopup.SelectPrevious();
                        e.SuppressKeyPress = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                    {
                        _slashPopup.ConfirmSelection();
                        e.SuppressKeyPress = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Escape)
                    {
                        _slashPopup.Close();
                        e.SuppressKeyPress = true;
                        return;
                    }
                }

                if (e.KeyCode == Keys.Escape)
                {
                    if (this.ParentForm != null)
                    {
                        this.ParentForm.ActiveControl = null;
                        this.ParentForm.Focus();
                    }
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    SendMessageFromInput();
                }
            };
            pnlInputPill.Controls.Add(txtInput);

            pnlInputContainer.Controls.Add(pnlInputPill);
            this.Controls.Add(pnlInputContainer);

            this.ResumeLayout(false);
            PerformCustomLayout();
        }

        private void HandleSlashCommandSelected(AiSlashCommand cmd)
        {
            if (cmd == null) return;

            if (cmd.IsFilePicker)
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = AppL10n.T("Chọn một hoặc nhiều tệp để AI đọc và phân tích", "Select file(s) for AI to read and analyze");
                    ofd.Multiselect = true;
                    ofd.Filter = AppL10n.T("Tất cả tệp văn phòng (*.docx;*.pdf;*.xlsx;*.pptx;*.txt;*.md;*.csv)|*.docx;*.pdf;*.xlsx;*.pptx;*.txt;*.md;*.csv|Tất cả tệp (*.*)|*.*", "All Office Files (*.docx;*.pdf;*.xlsx;*.pptx;*.txt;*.md;*.csv)|*.docx;*.pdf;*.xlsx;*.pptx;*.txt;*.md;*.csv|All Files (*.*)|*.*");
                    if (ofd.ShowDialog(this.FindForm()) == DialogResult.OK && ofd.FileNames != null && ofd.FileNames.Length > 0)
                    {
                        var quotedList = new List<string>();
                        foreach (var fn in ofd.FileNames) quotedList.Add("\"" + fn + "\"");
                        txtInput.Text = "/file " + string.Join(" ", quotedList.ToArray()) + " ";
                        _isPlaceholderActive = false;
                        txtInput.ForeColor = Color.FromArgb(15, 23, 42);
                        txtInput.SelectionStart = txtInput.Text.Length;
                        txtInput.Focus();
                        return;
                    }
                }

                txtInput.Text = "/file ";
            }
            else if (cmd.Name == "/translate")
            {
                txtInput.Text = "/translate Tiếng Việt ";
            }
            else if (cmd.Name == "/quiz")
            {
                txtInput.Text = "/quiz 5 câu trắc nghiệm ";
            }
            else if (cmd.Name == "/help")
            {
                txtInput.Text = "/help";
            }
            else
            {
                txtInput.Text = cmd.Name + " ";
            }

            _isPlaceholderActive = false;
            txtInput.ForeColor = Color.FromArgb(15, 23, 42);
            txtInput.SelectionStart = txtInput.Text.Length;
            txtInput.Focus();
        }

        /// <summary>
        /// Vẽ input pill capsule + nút gửi tròn vector KHÔNG răng cưa.
        /// Mũi tên gửi vẽ bằng FillPolygon (vector polygon) + SmoothingMode.AntiAlias.
        /// </summary>
        private void PaintInputPill(object sender, PaintEventArgs e)
        {
            using var aiFrame = AiGpuPaint.Begin(this, e);
            var g = aiFrame.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Clear 4 corners with parent background
            if (pnlInputContainer != null)
            {
                using (SolidBrush pb = new SolidBrush(pnlInputContainer.BackColor))
                {
                    g.FillRectangle(pb, pnlInputPill.ClientRectangle);
                }
            }

            // 1. Vẽ Capsule Box mượt mà
            RectangleF r = new RectangleF(0.5f, 0.5f, pnlInputPill.Width - 1.5f, pnlInputPill.Height - 1.5f);
            using (GraphicsPath gp = AiVectorGlassRenderer.CreateRoundedPath(r, r.Height / 2f))
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPath(sb, gp);
                }
                Color borderColor = (txtInput != null && txtInput.Focused && !_isPlaceholderActive)
                    ? Color.FromArgb(99, 102, 241)
                    : Color.FromArgb(203, 213, 225);
                using (Pen p = new Pen(borderColor, 1.2f))
                {
                    g.DrawPath(p, gp);
                }
            }

            // 2. Vẽ Nút Gửi hình tròn Vector khử răng cưa
            int btnSize = 28;
            _sendBtnRect = new Rectangle(pnlInputPill.Width - btnSize - 5, (pnlInputPill.Height - btnSize) / 2, btnSize, btnSize);

            Color btnBg;
            if (_isSendPressed)
                btnBg = Color.FromArgb(55, 48, 163);
            else if (_isSendHover)
                btnBg = Color.FromArgb(67, 56, 202);
            else
                btnBg = Color.FromArgb(79, 70, 229);

            // Vẽ hình tròn nền nút bằng FillEllipse
            using (SolidBrush sb = new SolidBrush(btnBg))
            {
                g.FillEllipse(sb, _sendBtnRect);
            }

            // 3. Vẽ biểu tượng Fluent Icon Windows 11 (/send hoặc /stop) trong nút bấm
            if (_isAiStreaming)
            {
                FluentIconRenderer.DrawGlyph(g, "/stop", new RectangleF(_sendBtnRect.X, _sendBtnRect.Y, _sendBtnRect.Width, _sendBtnRect.Height), Color.White, 12f);
            }
            else
            {
                FluentIconRenderer.DrawGlyph(g, "/send", new RectangleF(_sendBtnRect.X, _sendBtnRect.Y, _sendBtnRect.Width, _sendBtnRect.Height), Color.White, 13f);
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            PerformCustomLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PerformCustomLayout();
            UpdateIslandStyle();
        }

        public void OpenCanvas(AiCanvasQuizData quiz)
        {
            if (quiz == null || quiz.Questions == null || quiz.Questions.Count == 0)
            {
                if (_lastCanvasQuiz != null && _lastCanvasQuiz.Questions != null && _lastCanvasQuiz.Questions.Count > 0)
                {
                    quiz = _lastCanvasQuiz;
                }
                else
                {
                    return;
                }
            }
            _lastCanvasQuiz = quiz;
            if (_canvasControl == null)
            {
                _canvasControl = new AiCanvasControl();
                _canvasControl.CloseRequested += () => CloseCanvas();
                _canvasControl.ExportToWriterRequested += (q) => ExportQuizToWriter(q);
                this.Controls.Add(_canvasControl);
            }
            _canvasControl.LoadQuiz(quiz);
            _canvasControl.Visible = true;
            _canvasControl.BringToFront();

            if (_appForm != null)
            {
                _appForm.PositionAiSidebar();
                _appForm.Invalidate();
            }
            PerformCustomLayout();
            this.Invalidate();
        }

        public void CloseCanvas()
        {
            if (_canvasControl != null)
            {
                _canvasControl.Visible = false;
            }
            if (_appForm != null)
            {
                _appForm.PositionAiSidebar();
                _appForm.Invalidate();
            }
            PerformCustomLayout();
            this.Invalidate();
        }

        public void ExportQuizToWriter(AiCanvasQuizData quiz)
        {
            if (quiz == null || _appForm == null) return;
            string docText = AiCanvasParser.FormatQuizForWriter(quiz);
            if (_appForm.Ribbon.CurrentMode != OfficeAppMode.Writer)
            {
                _appForm.SwitchAppMode(OfficeAppMode.Writer);
            }
            if (_appForm.WriterView != null)
            {
                _appForm.WriterView.InsertTextFromAI(docText);
                _appForm.WriterView.Invalidate();
                _appForm.Invalidate();
                ZenQOffice.UI.FluentMessageBox.Show(
                    AppL10n.T(string.Format("Đã nhập thành công {0} câu hỏi và đáp án chi tiết sang tài liệu Writer!", quiz.Questions.Count), string.Format("Exported {0} question(s) and answers to Writer!", quiz.Questions.Count)),
                    AppL10n.T("Xuất Đề Thi - ZenQ Writer", "Export Quiz - ZenQ Writer"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void PerformCustomLayout()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            int chatW = IsCanvasOpen ? 380 : w;
            int headerH = 80;
            int inputH = 54;
            int feedH = Math.Max(50, h - headerH - inputH);

            if (pnlHeader != null)
            {
                pnlHeader.Bounds = new Rectangle(0, 0, chatW, headerH);
                if (cboKey != null) cboKey.Bounds = new Rectangle(12, 44, 120, 26);
                if (cboModel != null) cboModel.Bounds = new Rectangle(138, 44, Math.Max(80, chatW - 150), 26);
                if (btnIslandToggle != null) btnIslandToggle.Location = new Point(chatW - 144, 8);
                if (btnSettings != null) btnSettings.Location = new Point(chatW - 116, 8);
                if (btnHistory != null) btnHistory.Location = new Point(chatW - 88, 8);
                if (btnNewChat != null) btnNewChat.Location = new Point(chatW - 60, 8);
                if (btnClose != null) btnClose.Location = new Point(chatW - 32, 8);
            }

            if (pnlChatFeed != null)
            {
                pnlChatFeed.Bounds = new Rectangle(0, headerH, chatW, feedH);
            }

            if (pnlInputContainer != null)
            {
                pnlInputContainer.Bounds = new Rectangle(0, headerH + feedH, chatW, inputH);
                if (pnlInputPill != null)
                {
                    pnlInputPill.Bounds = new Rectangle(10, 7, chatW - 20, 38);
                    _sendBtnRect = new Rectangle(pnlInputPill.Width - 33, 5, 28, 28);
                    if (txtInput != null) txtInput.Bounds = new Rectangle(12, 10, Math.Max(50, pnlInputPill.Width - 52), 20);
                }
            }

            if (_slashPopup != null && _slashPopup.Visible && pnlInputContainer != null)
            {
                int popupW = Math.Max(260, chatW - 20);
                int popupH = _slashPopup.Height;
                int popupY = pnlInputContainer.Top - popupH - 2;
                _slashPopup.Bounds = new Rectangle(10, popupY, popupW, popupH);
                _slashPopup.BringToFront();
            }

            if (_canvasControl != null && _canvasControl.Visible)
            {
                int canvasW = Math.Max(200, w - chatW);
                _canvasControl.Bounds = new Rectangle(chatW, 0, canvasW, h);
            }
            UpdateRoundedChildren();
        }

        public void UpdateIslandStyle()
        {
            if (this.Width <= 0 || this.Height <= 0) return;

            if (UsesRoundedFrame)
            {
                // Bo góc 12px đồng bộ phong cách với thumbnail & island card
                // The host draws the antialiased outer rim. Keep the integer HWND mask
                // three pixels inside that opaque rim so its stair steps are invisible.
                float inset = 3f;
                float radius = 12f - inset;
                using (GraphicsPath path = AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(inset, inset, this.Width - inset * 2, this.Height - inset * 2), radius))
                {
                    Region oldR = this.Region;
                    this.Region = new Region(path);
                    oldR?.Dispose();
                }
            }
            else
            {
                if (this.Region != null)
                {
                    this.Region.Dispose();
                    this.Region = null;
                }
            }
            UpdateRoundedChildren();
            this.Invalidate();
            _appForm?.Invalidate();
        }

        private void UpdateRoundedChildren()
        {
            foreach (Control child in new Control[] { pnlHeader, pnlInputContainer, _canvasControl })
            {
                if (child == null) continue;
                var previous = child.Region;
                if (UsesRoundedFrame && child.Width > 0 && child.Height > 0)
                {
                    using var path = AiGpuPaint.CreateSidebarChildOutline(child, this, 0);
                    child.Region = new Region(path);
                }
                else child.Region = null;
                previous?.Dispose();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg == WM_MOUSEWHEEL)
            {
                short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                HandleExternalMouseWheel(delta);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var frame = AiGpuPaint.Begin(this, e);
            DrawSidebarFrame(frame.Graphics);
        }

        internal void DrawHostBackdrop(D2DGraphics g, Rectangle bounds)
        {
            var state = g.Save();
            using (var path = AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(bounds.X + .5f, bounds.Y + .5f, bounds.Width - 1, bounds.Height - 1), 12f))
            {
                g.SetClip(path, CombineMode.Intersect);
                using var background = new SolidBrush(BackColor);
                g.FillRectangle(background, bounds);
                foreach (Control child in new Control[] { pnlHeader, pnlInputContainer, _canvasControl })
                {
                    if (child == null || !child.Visible) continue;
                    using var fill = new SolidBrush(child.BackColor);
                    g.FillRectangle(fill, new Rectangle(bounds.X + child.Left, bounds.Y + child.Top, child.Width, child.Height));
                }
            }
            g.Restore(state);
            using var border = new Pen(Color.FromArgb(213, 221, 232), 1f);
            g.DrawRoundedRectangle(border, new RectangleF(bounds.X + .5f, bounds.Y + .5f, bounds.Width - 1, bounds.Height - 1), 12f);
        }

        internal void DrawSidebarFrame(D2DGraphics g)
        {
            if (UsesRoundedFrame)
            {
                g.Clear(_appForm?.BackColor ?? Color.FromArgb(240, 241, 243));
                var bounds = new RectangleF(1, 1, Width - 2, Height - 2);
                using var fill = new SolidBrush(BackColor);
                using var border = new Pen(Color.FromArgb(213, 221, 232), 1f);
                g.FillRoundedRectangle(fill, bounds, 12f);
                g.DrawRoundedRectangle(border, bounds, 12f);
            }
            else
            {
                using var border = new Pen(Color.FromArgb(226, 232, 240), 1f);
                g.DrawLine(border, 0, 0, 0, Height);
            }
        }

        public void ReloadProfilesAndModels()
        {
            _isBinding = true;
            _config = AiConfig.Load();
            _currentProfile = _config.GetActiveProfile();

            cboKey.Items.Clear();
            cboModel.Items.Clear();

            if (_config.Profiles.Count == 0 || _currentProfile == null)
            {
                _isBinding = false;
                return;
            }

            int selIdx = 0;
            for (int i = 0; i < _config.Profiles.Count; i++)
            {
                var p = _config.Profiles[i];
                string displayName = !string.IsNullOrEmpty(p.Name) ? p.Name : AppL10n.T("(Chưa đặt tên)", "(Untitled)");
                cboKey.Items.Add(displayName);
                if (p.Id == _config.ActiveProfileId) selIdx = i;
            }

            if (cboKey.Items.Count > 0)
            {
                cboKey.SelectedIndex = Math.Max(0, Math.Min(cboKey.Items.Count - 1, selIdx));
            }

            _isBinding = false;
            PopulateModelsForActiveProfile();
        }

        private void OnKeySelectionChanged()
        {
            if (_isBinding) return;
            if (cboKey.SelectedIndex >= 0 && cboKey.SelectedIndex < _config.Profiles.Count)
            {
                _currentProfile = _config.Profiles[cboKey.SelectedIndex];
                _config.ActiveProfileId = _currentProfile.Id;
                _config.Save();

                PopulateModelsForActiveProfile();
            }
        }

        private void PopulateModelsForActiveProfile()
        {
            if (_currentProfile == null) return;
            _isBinding = true;

            cboModel.Items.Clear();

            List<string> models = new List<string>();
            if (_currentProfile.SavedModels != null && _currentProfile.SavedModels.Count > 0)
            {
                foreach (var m in _currentProfile.SavedModels)
                {
                    string cleanM = m;
                    if (_currentProfile.Provider == AiProviderType.GoogleAIStudio && cleanM == "gemini-3.1-flash")
                    {
                        cleanM = "gemini-3.1-flash-lite";
                    }
                    if (!string.IsNullOrEmpty(cleanM) && !cleanM.StartsWith("⏳") && !cleanM.StartsWith("⚠️") && !models.Contains(cleanM)
                        && AiKeyProfile.IsModelCompatible(_currentProfile.Provider, cleanM))
                    {
                        models.Add(cleanM);
                    }
                }
            }

            if (_currentProfile.Provider == AiProviderType.GoogleAIStudio)
            {
                if (_currentProfile.SelectedModel == "gemini-3.1-flash")
                {
                    _currentProfile.SelectedModel = "gemini-3.1-flash-lite";
                }
            }

            if (models.Count == 0)
            {
                var rec = AiConfig.GetRecommendedModels(_currentProfile.Provider);
                foreach (var r in rec)
                {
                    if (!models.Contains(r)) models.Add(r);
                }
            }

            foreach (var m in models)
            {
                cboModel.Items.Add(m);
            }

            int selIdx = 0;
            if (!string.IsNullOrEmpty(_currentProfile.SelectedModel))
            {
                int found = cboModel.Items.IndexOf(_currentProfile.SelectedModel);
                if (found >= 0) selIdx = found;
            }

            if (cboModel.Items.Count > 0)
            {
                cboModel.SelectedIndex = selIdx;
                _currentProfile.SelectedModel = cboModel.Items[selIdx].ToString();
                _config.Save();
            }

            _isBinding = false;
        }

        private void OnModelSelectionChanged()
        {
            if (_isBinding || _currentProfile == null) return;
            if (cboModel.SelectedIndex >= 0)
            {
                _currentProfile.SelectedModel = cboModel.SelectedItem.ToString();
                _config.Save();
            }
        }

        public void RefreshContextAndChips()
        {
            // Clean interface
        }

        public void HandleExternalMouseWheel(int delta)
        {
            if (pnlChatFeed == null) return;
            try
            {
                pnlChatFeed.DoMouseWheel(delta);
            }
            catch { }
        }

        public void SwitchTab(DocTab tab)
        {
            _currentTab = tab;

            if (tab != null)
            {
                if (string.IsNullOrEmpty(tab.AiChatSessionId))
                {
                    tab.AiChatSessionId = Guid.NewGuid().ToString("N");
                }
                _currentSessionId = tab.AiChatSessionId;

                if (tab.AiChatHistory == null)
                {
                    var savedHist = AiChatHistoryManager.LoadHistory();
                    tab.AiChatHistory = (savedHist != null && savedHist.Count > 0) ? savedHist : new List<AiChatMessage>();
                }
            }

            pnlChatFeed.ClearVirtualControls();
            _streamingAiRow = null;
            _streamingAiBubble = null;
            _streamingAiLabel = null;
            HideThinkingBubble();

            if (tab != null && tab.AiChatHistory != null && tab.AiChatHistory.Count > 0)
            {
                pnlChatFeed.SuspendVirtualLayout();
                for (int i = 0; i < tab.AiChatHistory.Count; i++)
                {
                    var msg = tab.AiChatHistory[i];
                    if (msg.Role == "user")
                    {
                        AddUserBubble(msg.Content, i);
                    }
                    else
                    {
                        AiCanvasQuizData quizData = null;
                        if (!string.IsNullOrEmpty(msg.CanvasQuizJson))
                        {
                            if (AiCanvasParser.TryParse(msg.CanvasQuizJson, out var p1)) quizData = p1;
                        }
                        if (quizData == null && !string.IsNullOrEmpty(msg.Content))
                        {
                            if (AiCanvasParser.TryParse(msg.Content, out var p2)) quizData = p2;
                        }
                        if (quizData != null)
                        {
                            _lastCanvasQuiz = quizData;
                        }
                        AddAiBubble(msg.Content, quizData);
                    }
                }
                pnlChatFeed.ResumeVirtualLayout();
            }
            else
            {
                // Hiện giao diện chào mừng phong cách Google Gemini / GPT khi mở tab mới
                AddGeminiWelcomeView();
            }

            // Kiểm tra xem tab này có tiến trình AI đang chạy ngầm hay không
            string sessId = (tab != null && !string.IsNullOrEmpty(tab.AiChatSessionId)) ? tab.AiChatSessionId : _currentSessionId;
            if (!string.IsNullOrEmpty(sessId) && _activeGenerations.ContainsKey(sessId))
            {
                var activeGen = _activeGenerations[sessId];
                if (!activeGen.IsCompleted)
                {
                    _isAiStreaming = true;
                    pnlInputPill.Invalidate();

                    string partial = activeGen.FullResponseBuilder.ToString();
                    if (string.IsNullOrEmpty(partial))
                    {
                        ShowThinkingBubble();
                    }
                    else
                    {
                        AddAiStreamingBubble(CleanAiOutputForChat(partial));
                    }
                }
                else
                {
                    _isAiStreaming = false;
                    pnlInputPill.Invalidate();
                }
            }
            else
            {
                _isAiStreaming = false;
                pnlInputPill.Invalidate();
            }

            pnlChatFeed.ResumeLayout(true);
            pnlChatFeed.PerformLayout();
            ScrollToBottomForce();
        }

        /// <summary>
        /// Hiện giao diện chào mừng phong cách Google Gemini / GPT:
        /// - Hiệu ứng ánh sáng Radiant Aura Glow huyền ảo
        /// - Biểu tượng ngôi sao 4 cánh Google Gemini lấp lánh đa sắc
        /// - Câu chào ngẫu nhiên truyền cảm hứng (AppL10n.T("Chúng ta nên bắt đầu từ đâu nhỉ?", "Where should we start?"), AppL10n.T("Bắt đầu làm việc thôi!", "Let's get to work!"),...)
        /// - 4 gợi ý câu hỏi mẫu thông minh (Chips)
        /// </summary>
        private void AddGeminiWelcomeView()
        {
            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;
            OfficeAppMode curMode = _appForm != null && _appForm.Ribbon != null ? _appForm.Ribbon.CurrentMode : OfficeAppMode.Home;
            GeminiWelcomeControl welcome = new GeminiWelcomeControl(feedW, curMode, (prompt) =>
            {
                txtInput.Text = prompt;
                txtInput.ForeColor = Color.FromArgb(15, 23, 42);
                _isPlaceholderActive = false;
                pnlInputPill.Invalidate();
                SendMessageFromInput();
            });
            pnlChatFeed.AddVirtualControl(welcome);
            ScrollToBottomForce();
        }

        public void StartNewChatSession()
        {
            if (_isAiStreaming)
            {
                _abortAiStream = true;
                _isAiStreaming = false;
            }

            if (_currentTab != null && _currentTab.AiChatHistory != null && _currentTab.AiChatHistory.Count > 0)
            {
                string oldSessId = !string.IsNullOrEmpty(_currentTab.AiChatSessionId) ? _currentTab.AiChatSessionId : _currentSessionId;
                AiChatHistoryManager.SaveCurrentSession(oldSessId, _currentTab.AiChatHistory);
                _currentTab.AiChatHistory.Clear();
            }

            _currentSessionId = Guid.NewGuid().ToString("N");
            if (_currentTab != null)
            {
                _currentTab.AiChatSessionId = _currentSessionId;
                _currentTab.AiChatHistory = new List<AiChatMessage>();
            }
            AiChatHistoryManager.SaveHistory(new List<AiChatMessage>());

            txtInput.Text = _placeholderText;
            txtInput.ForeColor = Color.FromArgb(148, 163, 184);
            _isPlaceholderActive = true;
            pnlInputPill.Invalidate();

            pnlChatFeed.SuspendLayout();
            pnlChatFeed.ClearVirtualControls();
            _streamingAiRow = null;
            _streamingAiBubble = null;
            _streamingAiLabel = null;
            HideThinkingBubble();
            AddGeminiWelcomeView();
            pnlChatFeed.ResumeLayout(true);
            pnlChatFeed.PerformLayout();
            ScrollToBottomForce();

            if (txtInput.CanFocus)
            {
                txtInput.Focus();
            }
        }

        private void ShowHistoryDialog()
        {
            // First save active session so it appears in history list
            if (_currentTab != null && _currentTab.AiChatHistory != null && _currentTab.AiChatHistory.Count > 0)
            {
                string sessId = !string.IsNullOrEmpty(_currentTab.AiChatSessionId) ? _currentTab.AiChatSessionId : _currentSessionId;
                AiChatHistoryManager.SaveCurrentSession(sessId, _currentTab.AiChatHistory);
            }

            using (var dlg = new AiChatHistoryDialog(this.ParentForm))
            {
                dlg.OnSelectSession = (session) =>
                {
                    if (session != null && session.Messages != null)
                    {
                        _currentSessionId = session.Id;
                        if (_currentTab != null)
                        {
                            _currentTab.AiChatSessionId = session.Id;
                            _currentTab.AiChatHistory = new List<AiChatMessage>(session.Messages);
                            AiChatHistoryManager.SaveHistory(_currentTab.AiChatHistory);
                        }
                        SwitchTab(_currentTab);
                    }
                };
                dlg.OnDeleteSession = (deletedId) =>
                {
                    if (_currentSessionId == deletedId)
                    {
                        StartNewChatSession();
                    }
                };
                dlg.OnStartNewChat = () =>
                {
                    StartNewChatSession();
                };
                dlg.ShowDialog(this);
            }
        }

        public void ClearChatHistory()
        {
            StartNewChatSession();
        }

        private void OnEditAndResend(string text, int msgIndex)
        {
            if (_isAiStreaming)
            {
                _abortAiStream = true;
                _isAiStreaming = false;
            }

            txtInput.Text = text ?? "";
            txtInput.ForeColor = Color.FromArgb(15, 23, 42);
            _isPlaceholderActive = false;
            pnlInputPill.Invalidate();

            if (_currentTab != null && _currentTab.AiChatHistory != null)
            {
                if (msgIndex >= 0 && msgIndex < _currentTab.AiChatHistory.Count)
                {
                    // XÃ³a cÃ¢u há» i nÃ y vÃ  toÃ n bá»™ cÃ¢u tráº£ lá» i AI/tin nháº¯n sau nÃ³
                    _currentTab.AiChatHistory.RemoveRange(msgIndex, _currentTab.AiChatHistory.Count - msgIndex);
                    AiChatHistoryManager.SaveHistory(_currentTab.AiChatHistory);
                }
            }

            SwitchTab(_currentTab);

            if (txtInput.CanFocus)
            {
                txtInput.Focus();
                txtInput.SelectionStart = txtInput.Text.Length;
            }
        }

        private Panel _streamingAiRow = null;
        private Panel _streamingAiBubble = null;
        private Label _streamingAiLabel = null;
        private StringBuilder _streamingTextBuilder = new StringBuilder();

        private void AddAiStreamingBubble(string initialChunk)
        {
            _streamingTextBuilder.Clear();
            _streamingTextBuilder.Append(initialChunk);

            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;
            int maxBubbleW = Math.Max(140, (int)(feedW * 0.82f));

            Font font = new Font("Segoe UI", 9.3f);
            int padX = 14;
            int padY = 9;

            string text = _streamingTextBuilder.ToString();
            Size txtSize = TextRenderer.MeasureText(text, font, new Size(maxBubbleW - padX * 2, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            int bubbleW = Math.Max(46, Math.Min(maxBubbleW, txtSize.Width + padX * 2));
            int bubbleH = Math.Max(32, txtSize.Height + padY * 2);

            _streamingAiBubble = new AiGpuPanel
            {
                Size = new Size(bubbleW, bubbleH),
                Location = new Point(2, 0),
                BackColor = Color.Transparent
            };

            _streamingAiBubble.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                var g = aiFrame.Graphics;
                Control c = s as Control;
                if (c == null) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, c.Width - 1, c.Height - 1);
                using (GraphicsPath gp = RoundRect(r, 14))
                {
                    using (SolidBrush sb = new SolidBrush(Color.White))
                    {
                        g.FillPath(sb, gp);
                    }
                    using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                    {
                        g.DrawPath(p, gp);
                    }
                }
            };

            _streamingAiLabel = new Label
            {
                Text = text,
                Font = font,
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                Bounds = new Rectangle(padX, padY, bubbleW - padX * 2, bubbleH - padY * 2)
            };
            _streamingAiBubble.Controls.Add(_streamingAiLabel);

            _streamingAiRow = new AiGpuPanel
            {
                Width = feedW,
                Height = bubbleH,
                BackColor = Color.Transparent,
                Margin = new Padding(2, 3, 2, 5)
            };
            _streamingAiRow.Controls.Add(_streamingAiBubble);

            pnlChatFeed.AddVirtualControl(_streamingAiRow);
            ScrollToBottomForce();
        }

        private static string CleanAiOutputForChat(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string clean = text;

            // Strip prompt context echoes
            clean = Regex.Replace(clean, @"<current_slide_context>[\s\S]*?</current_slide_context>", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"<document_context>[\s\S]*?</document_context>", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"<selected_text>[\s\S]*?</selected_text>", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"<calc_context>[\s\S]*?</calc_context>", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"<selected_cells_range>[\s\S]*?</selected_cells_range>", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"<user_question>[\s\S]*?</user_question>", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"<[^>]+>", "", RegexOptions.IgnoreCase); 
            clean = Regex.Replace(clean, @"\[(?:TAGGED|XIN_LỖI|ERROR|SYSTEM|TAG):?[^\]]*\]:?", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[(?:QUY TẮC|HƯỚNG DẪN)[^\]]*\][\s\S]*?\[/ZENQ_ACTION\]", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[OFFICE_AGENT_[^\]]*\]", "", RegexOptions.IgnoreCase);

            // Strip ALL action block variants (complete or streaming-incomplete)
            // Handles [ZENQ_ACTION:...], [TAG:ZENQ_ACTION:...], [HZENQ_ACTION:...], etc.
            clean = Regex.Replace(clean, @"\[[A-Za-z0-9_]*(?:ACTION|ZENQ)[A-Za-z0-9_:]*\][\s\S]*?(?:\[/[A-Za-z0-9_]*(?:ACTION|ZENQ)[A-Za-z0-9_]*\])", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[[A-Za-z0-9_]*(?:ACTION|ZENQ)[A-Za-z0-9_:]*\][\s\S]*", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[/?[A-Za-z0-9_]*(?:ACTION|ZENQ)[A-Za-z0-9_]*\]", "", RegexOptions.IgnoreCase);

            // Strip markdown code fences containing JSON or canvas
            clean = Regex.Replace(clean, @"```(?:json:canvas|canvas|json)?[\s\S]*?```", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"```(?:json:canvas|canvas|json)?[\s\S]*", "", RegexOptions.IgnoreCase);

            // Strip :canvas blocks (whether enclosed or raw)
            clean = Regex.Replace(clean, @":?canvas\s*\{[\s\S]*?\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @":?canvas\s*\{[\s\S]*", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"^:?canvas\s*$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            // Strip JSON blocks containing quiz, corrections, calc data
            clean = Regex.Replace(clean, @"\{[\s\r\n]*""(?:title|totalQuestions|questions|options)""[\s\S]*?\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\r\n]*""(?:title|totalQuestions|questions|options)""[\s\S]*", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""corrections""[\s\S]*?\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""corrections""[\s\S]*", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""original""[\s\S]*?""replacement""[\s\S]*?\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""original""[\s\S]*", "", RegexOptions.IgnoreCase);

            // Strip raw JSON canvas structure if AI printed it directly
            clean = Regex.Replace(clean, @"\{[\s\r\n]*""(?:theme|style|background|elements|type)""[\s\S]*\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\r\n]*""(?:theme|style|background|elements|type)""[\s\S]*", "", RegexOptions.IgnoreCase);

            // Strip raw tags/labels like [CAPTION], [SUBCAPTION], [Slide 1], [Shape #1...]
            clean = Regex.Replace(clean, @"\[(?:CAPTION|SUBCAPTION|Slide \d+|Shape #\d+[^\]]*)\]", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\*\*\[Shape #\d+[^\]]*\]\*\*", "", RegexOptions.IgnoreCase);

            // Normalize markdown for rich structured rendering
            clean = clean.Replace("```json", "").Replace("```", ""); // code fences
            clean = Regex.Replace(clean, @"^\s*[\*\-]\s+", "• ", RegexOptions.Multiline); // clean bullet points
            // Fix lone underscores like "VỊ TRÍ _ĐỊA LÝ" -> "VỊ TRÍ ĐỊA LÝ"
            clean = Regex.Replace(clean, @"(?<=[A-ZÀ-Ỹa-zà-ỹ0-9])\s*_\s*(?=[A-ZÀ-Ỹa-zà-ỹ0-9])", " ");

            // Normalize multiple blank lines
            clean = Regex.Replace(clean, @"\n{3,}", "\n\n");
            clean = Regex.Replace(clean, @"\s*//\s*$", "");

            return clean.Trim();
        }

        private void AppendToAiStreamingBubble(string newChunk)
        {
            if (_streamingAiRow == null || _streamingAiLabel == null || _streamingAiBubble == null)
            {
                AddAiStreamingBubble(newChunk);
                return;
            }

            _streamingTextBuilder.Append(newChunk);
            string fullRaw = _streamingTextBuilder.ToString();
            string displayText = CleanAiOutputForChat(fullRaw);
            if (string.IsNullOrEmpty(displayText))
            {
                OfficeAppMode curMode = _appForm != null && _appForm.Ribbon != null ? _appForm.Ribbon.CurrentMode : OfficeAppMode.Home;
                if (curMode == OfficeAppMode.Calc)
                    displayText = AppL10n.T("✦ Đang xử lý và ghi dữ liệu bảng tính...", "✦ Updating spreadsheet data...");
                else if (curMode == OfficeAppMode.Writer)
                    displayText = AppL10n.T("✦ Đang soạn thảo văn bản...", "✦ Writing document content...");
                else if (curMode == OfficeAppMode.Pdf)
                    displayText = AppL10n.T("✦ Đang phân tích tài liệu PDF...", "✦ Analyzing PDF document...");
                else
                    displayText = AppL10n.T("✦ Đang thiết kế slide cho bạn...", "✦ Designing slides for you...");
            }

            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;
            int maxBubbleW = Math.Max(140, (int)(feedW * 0.82f));

            Font font = _streamingAiLabel.Font;
            int padX = 14;
            int padY = 9;

            Size txtSize = TextRenderer.MeasureText(displayText, font, new Size(maxBubbleW - padX * 2, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            int bubbleW = Math.Max(46, Math.Min(maxBubbleW, txtSize.Width + padX * 2));
            int bubbleH = Math.Max(32, txtSize.Height + padY * 2);

            _streamingAiBubble.Size = new Size(bubbleW, bubbleH);
            _streamingAiLabel.Bounds = new Rectangle(padX, padY, bubbleW - padX * 2, bubbleH - padY * 2);
            _streamingAiLabel.Text = displayText + " ▌";

            if (_streamingAiRow.Height != bubbleH)
            {
                _streamingAiRow.Height = bubbleH;
            }
            _streamingAiBubble.Invalidate();
            ScrollToBottomIfNear();
        }

        private void FinishAiStreamingBubble()
        {
            if (_streamingAiRow != null && _streamingAiBubble != null && _streamingAiLabel != null)
            {
                string textToInsert = _streamingAiLabel.Text;
                
                OfficeAppMode curMode = _appForm != null ? _appForm.Ribbon.CurrentMode : OfficeAppMode.Home;
                string actionBtnText = (curMode == OfficeAppMode.Slides) ? AppL10n.T("Chèn vào Slide", "Insert to Slide") : ((curMode == OfficeAppMode.Calc) ? AppL10n.T("Chèn vào Bảng tính", "Insert to Sheet") : ((curMode == OfficeAppMode.Pdf) ? AppL10n.T("Sao chép phản hồi", "Copy Response") : (_appForm != null && _appForm.WriterView != null && !string.IsNullOrEmpty(_appForm.WriterView.GetSelectedText()) ? AppL10n.T("Thay thế vùng quét khối", "Replace Selection") : AppL10n.T("Chèn vào văn bản", "Insert into Document"))));
                char glyph = (curMode == OfficeAppMode.Slides) ? '\uE714' : ((curMode == OfficeAppMode.Calc) ? '\uE80A' : ((curMode == OfficeAppMode.Pdf) ? '\uE8C8' : '\uE70F'));

                AiInsertActionButton btnAction = new AiInsertActionButton
                {
                    Text = actionBtnText,
                    Image = FluentIconRenderer.GetGlyphBitmap(glyph, 12, 12, Color.FromArgb(100, 116, 139)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(4, 0, 4, 0),
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    BackColor = Color.FromArgb(248, 250, 252),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(140, 24),
                    Location = new Point(14, _streamingAiBubble.Height + 4),
                    Cursor = Cursors.Hand,
                };
                btnAction.FlatAppearance.BorderSize = 0;
                btnAction.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
                btnAction.Click += (s, e) =>
                {
                    if (_appForm == null) return;
                    string textSnapshot = textToInsert;
                    try {
                        if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Slides && _appForm.SlidesView != null)
                        {
                            _appForm.SlidesView.InsertTextFromAI(textSnapshot);
                            _appForm.Invalidate();
                        }
                        else if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Writer && _appForm.WriterView != null)
                        {
                            if (!string.IsNullOrEmpty(_appForm.WriterView.GetSelectedText()))
                            {
                                _appForm.WriterView.ReplaceSelectedText(textSnapshot, true);
                            }
                            else
                            {
                                _appForm.WriterView.InsertTextFromAI(textSnapshot);
                            }
                            _appForm.Invalidate();
                        }
                        else if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Pdf && _appForm.PdfView != null)
                        {
                            Clipboard.SetText(textSnapshot);
                            ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Đã sao chép nội dung phản hồi từ AI vào bộ nhớ tạm (Clipboard).", "Copied AI response to Clipboard."), "ZENQ AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    } catch (Exception ex) {
                        ZenQOffice.UI.FluentMessageBox.Show(ex.ToString(), AppL10n.T("ZENQ AI - Lỗi Chèn Text", "ZENQ AI - Insert Error"));
                    }
                };
                
                // ONLY detect corrections if user explicitly asked for proofreading/spellcheck or AI generated explicit diff/action tags
                bool isSpellCheckRequested = !string.IsNullOrEmpty(_lastUserQuery) &&
                    (_lastUserQuery.StartsWith("/check", StringComparison.OrdinalIgnoreCase) ||
                     _lastUserQuery.StartsWith("/fix", StringComparison.OrdinalIgnoreCase) ||
                     _lastUserQuery.IndexOf("chính tả", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     _lastUserQuery.IndexOf("sửa lỗi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     _lastUserQuery.IndexOf("bắt lỗi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     _lastUserQuery.IndexOf("proofread", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     _lastUserQuery.IndexOf("spell check", StringComparison.OrdinalIgnoreCase) >= 0);

                bool hasExplicitCorrectionTag = textToInsert.Contains("<replace") ||
                                                textToInsert.Contains("<diff") ||
                                                textToInsert.Contains("[ZENQ_ACTION:APPLY_SPELL_CORRECTIONS]") ||
                                                textToInsert.Contains("\"corrections\"");

                List<SpellCorrectionItem> detectedCorrs = null;
                if (isSpellCheckRequested || hasExplicitCorrectionTag)
                {
                    detectedCorrs = AiActionEngine.ExtractSpellCorrections(textToInsert);
                    if (_appForm != null && _appForm.WriterView != null && _appForm.WriterView.PieceTable != null && detectedCorrs != null && detectedCorrs.Count > 0)
                    {
                        string docTxt = _appForm.WriterView.PieceTable.GetText();
                        detectedCorrs = detectedCorrs.FindAll(c => !string.IsNullOrEmpty(c.Original) && docTxt.IndexOf(c.Original, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                }

                if (detectedCorrs != null && detectedCorrs.Count > 0 && _appForm != null && _appForm.WriterView != null)
                {
                    _appForm.WriterView.SetPendingAiCorrections(detectedCorrs);

                    AiInsertActionButton btnConfirm = new AiInsertActionButton
                    {
                        Text = AppL10n.T(string.Format("✓ Đồng ý áp dụng ({0})", detectedCorrs.Count), string.Format("✓ Apply ({0})", detectedCorrs.Count)),
                        Image = FluentIconRenderer.GetGlyphBitmap('\uE73E', 12, 12, Color.FromArgb(22, 163, 74)),
                        ImageAlign = ContentAlignment.MiddleLeft,
                        TextAlign = ContentAlignment.MiddleCenter,
                        TextImageRelation = TextImageRelation.ImageBeforeText,
                        Padding = new Padding(4, 0, 4, 0),
                        Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(22, 163, 74),
                        BackColor = Color.FromArgb(240, 253, 244),
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(140, 24),
                        Location = new Point(14, _streamingAiBubble.Height + 4),
                        Cursor = Cursors.Hand,
                    };
                    btnConfirm.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
                    btnConfirm.FlatAppearance.BorderSize = 1;
                    btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 252, 231);

                    AiInsertActionButton btnCancel = new AiInsertActionButton
                    {
                        Text = AppL10n.T("✕ Từ chối", "✕ Decline"),
                        Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 11, 11, Color.FromArgb(220, 38, 38)),
                        ImageAlign = ContentAlignment.MiddleLeft,
                        TextAlign = ContentAlignment.MiddleCenter,
                        TextImageRelation = TextImageRelation.ImageBeforeText,
                        Padding = new Padding(4, 0, 4, 0),
                        Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(220, 38, 38),
                        BackColor = Color.FromArgb(254, 242, 242),
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(85, 24),
                        Location = new Point(160, _streamingAiBubble.Height + 4),
                        Cursor = Cursors.Hand,
                    };
                    btnCancel.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
                    btnCancel.FlatAppearance.BorderSize = 1;
                    btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);

                    btnConfirm.Click += (s, e) =>
                    {
                        if (_appForm != null && _appForm.WriterView != null)
                        {
                            _appForm.WriterView.ApplyPendingAiCorrections();
                            btnConfirm.Text = AppL10n.T("Đã áp dụng!", "Applied!");
                            btnConfirm.Image = FluentIconRenderer.GetGlyphBitmap('\uE73E', 12, 12, Color.FromArgb(22, 163, 74));
                            btnConfirm.Enabled = false;
                            btnCancel.Visible = false;
                        }
                    };
                    btnCancel.Click += (s, e) =>
                    {
                        if (_appForm != null && _appForm.WriterView != null)
                        {
                            _appForm.WriterView.CancelPendingAiCorrections();
                            btnCancel.Text = AppL10n.T("Đã hủy", "Declined");
                            btnCancel.Image = null;
                            btnCancel.Enabled = false;
                            btnConfirm.Visible = false;
                        }
                    };

                    btnAction.Visible = false; // Ẩn nút insert mặc định đi vì đã có xác nhận/bỏ qua
                    _streamingAiRow.Height = _streamingAiBubble.Height + 34;
                    _streamingAiRow.Controls.Add(btnConfirm);
                    _streamingAiRow.Controls.Add(btnCancel);
                }
                else
                {
                    _streamingAiRow.Height = _streamingAiBubble.Height + 34;
                    _streamingAiRow.Controls.Add(btnAction);
                }
            }

            _streamingAiRow = null;
            _streamingAiBubble = null;
            _streamingAiLabel = null;
        }

        private bool _isAiStreaming = false;
        private bool _abortAiStream = false;

        private string _lastUserQuery = "";

        private void SendMessageFromInput()
        {
            string query = txtInput.Text != null ? txtInput.Text.Trim() : "";
            if (string.IsNullOrEmpty(query) || query == _placeholderText) return;
            _lastUserQuery = query;
            _isPlaceholderActive = false;

            _config = AiConfig.Load();
            _currentProfile = _config.GetActiveProfile();
            if (_currentProfile == null && _config.Profiles != null && _config.Profiles.Count > 0)
            {
                _currentProfile = _config.Profiles[0];
                _config.ActiveProfileId = _currentProfile.Id;
            }

            txtInput.Text = "";

            // Ghi nhận Tab và Session mục tiêu ngay lúc bấm gửi
            DocTab targetTab = _currentTab;
            if (targetTab != null && string.IsNullOrEmpty(targetTab.AiChatSessionId))
            {
                targetTab.AiChatSessionId = _currentSessionId;
            }
            string targetSessionId = (targetTab != null && !string.IsNullOrEmpty(targetTab.AiChatSessionId)) ? targetTab.AiChatSessionId : _currentSessionId;

            var userMsg = new AiChatMessage { Role = "user", Content = query, Timestamp = DateTime.Now };
            if (targetTab != null)
            {
                if (targetTab.AiChatHistory == null) targetTab.AiChatHistory = new List<AiChatMessage>();
                targetTab.AiChatHistory.Add(userMsg);
                AiChatHistoryManager.SaveHistory(targetTab.AiChatHistory);
                AiChatHistoryManager.SaveCurrentSession(targetSessionId, targetTab.AiChatHistory);
            }
            else
            {
                var globalHist = AiChatHistoryManager.LoadHistory();
                globalHist.Add(userMsg);
                AiChatHistoryManager.SaveHistory(globalHist);
                AiChatHistoryManager.SaveCurrentSession(targetSessionId, globalHist);
            }

            int userIdx = (targetTab != null && targetTab.AiChatHistory != null) ? (targetTab.AiChatHistory.Count - 1) : 0;
            AddUserBubble(query, userIdx);

            // Xử lý đàm thoại tương tác với đề xuất sửa đổi (Pending AI Diff)
            if (_appForm != null && _appForm.WriterView != null && _appForm.WriterView.HasPendingAiDiff)
            {
                if (IsUserAgreement(query))
                {
                    _appForm.WriterView.ApplyPendingAiCorrections();
                    string acceptReply = AppL10n.T("✅ Tôi đã áp dụng các thay đổi đề xuất vào tài liệu của bạn rồi nhé!", "✅ Applied the suggested changes to your document!");
                    var aiReplyMsg = new AiChatMessage { Role = "model", Content = acceptReply, Timestamp = DateTime.Now };
                    if (targetTab != null && targetTab.AiChatHistory != null)
                    {
                        targetTab.AiChatHistory.Add(aiReplyMsg);
                        AiChatHistoryManager.SaveCurrentSession(targetSessionId, targetTab.AiChatHistory);
                    }
                    AddAiBubble(acceptReply);
                    return;
                }
                else if (IsUserRejection(query))
                {
                    _appForm.WriterView.CancelPendingAiCorrections();
                    string rejectReply = AppL10n.T("❌ Tôi đã hủy bỏ đề xuất sửa đổi và giữ nguyên nội dung gốc của tài liệu.", "❌ Declined the suggested edits and kept the original content.");
                    var aiReplyMsg = new AiChatMessage { Role = "model", Content = rejectReply, Timestamp = DateTime.Now };
                    if (targetTab != null && targetTab.AiChatHistory != null)
                    {
                        targetTab.AiChatHistory.Add(aiReplyMsg);
                        AiChatHistoryManager.SaveCurrentSession(targetSessionId, targetTab.AiChatHistory);
                    }
                    AddAiBubble(rejectReply);
                    return;
                }
                else
                {
                    // Người dùng hỏi câu khác: tự động xóa diff cũ để không làm phiền mắt và tiếp tục hỏi câu mới
                    _appForm.WriterView.CancelPendingAiCorrections();
                }
            }

            // Kiểm tra lệnh Slash Command (ví dụ: /help, /file, /summary, /page, /translate, /table, /quiz, /explain)
            string slashPrompt;
            string instantLocalReply;
            bool isSlash = AiSlashCommandEngine.ProcessSlashCommand(query, _appForm, targetTab, out slashPrompt, out instantLocalReply);

            if (isSlash && !string.IsNullOrEmpty(instantLocalReply))
            {
                AddAiBubble(instantLocalReply);
                var localModelMsg = new AiChatMessage { Role = "model", Content = instantLocalReply, Timestamp = DateTime.Now };
                if (targetTab != null)
                {
                    targetTab.AiChatHistory.Add(localModelMsg);
                    AiChatHistoryManager.SaveHistory(targetTab.AiChatHistory);
                    AiChatHistoryManager.SaveCurrentSession(targetSessionId, targetTab.AiChatHistory);
                }
                return;
            }

            if (_currentProfile == null)
            {
                string noConfigMsg = AppL10n.T("⚠️ Chưa có cấu hình AI nào. Vui lòng vào Cài đặt để thiết lập API Key hoặc Ollama trước khi sử dụng.", "⚠️ No AI profile configured yet. Please open Settings to configure an API key or Ollama.");
                AddAiBubble(noConfigMsg);
                var warnMsg = new AiChatMessage { Role = "model", Content = noConfigMsg, Timestamp = DateTime.Now };
                if (targetTab != null)
                {
                    targetTab.AiChatHistory.Add(warnMsg);
                    AiChatHistoryManager.SaveHistory(targetTab.AiChatHistory);
                    AiChatHistoryManager.SaveCurrentSession(targetSessionId, targetTab.AiChatHistory);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentProfile.SelectedModel) && _currentProfile.SavedModels != null && _currentProfile.SavedModels.Count > 0)
            {
                _currentProfile.SelectedModel = _currentProfile.SavedModels[0];
            }

            // Khởi tạo hoạt họa tìm kiếm / suy nghĩ phong cách Google Gemini
            bool needWebSearch = AiWebSearchService.ShouldSearchWeb(query);
            string searchKw = needWebSearch ? AiWebSearchService.ExtractSearchQuery(query) : "";
            string initStatus = needWebSearch ? (AppL10n.T("🔍 Đang tra cứu internet: ", "🔍 Searching web: ") + searchKw + "...") : AppL10n.T("✦ Đang suy nghĩ...", "✦ Thinking...");
            ShowThinkingBubble(initStatus, needWebSearch);

            ActiveAiGeneration gen = new ActiveAiGeneration
            {
                SessionId = targetSessionId,
                TargetTab = targetTab,
                IsFirstChunk = true
            };
            lock (_activeGenerations)
            {
                _activeGenerations[targetSessionId] = gen;
            }

            _isAiStreaming = true;
            _abortAiStream = false;
            pnlInputPill.Invalidate();

            object _streamLock = new object();
            string _pendingStreamChunk = "";
            System.Windows.Forms.Timer streamUiTimer = new System.Windows.Forms.Timer { Interval = 30 };
            streamUiTimer.Tick += (s, e) =>
            {
                string chunkToUpdate = "";
                lock (_streamLock)
                {
                    if (string.IsNullOrEmpty(_pendingStreamChunk)) return;
                    chunkToUpdate = _pendingStreamChunk;
                    _pendingStreamChunk = "";
                }

                if (_currentTab == targetTab && _currentSessionId == targetSessionId && this.Visible && !this.IsDisposed)
                {
                    if (gen.IsFirstChunk)
                    {
                        gen.IsFirstChunk = false;
                        HideThinkingBubble();
                        AddAiStreamingBubble(chunkToUpdate);
                    }
                    else
                    {
                        AppendToAiStreamingBubble(chunkToUpdate);
                    }
                }
            };
            streamUiTimer.Start();

            Task.Run(async () =>
            {
                string basePrompt = isSlash ? slashPrompt : query;
                string promptToSend = basePrompt;

                if (needWebSearch && !string.IsNullOrWhiteSpace(searchKw))
                {
                    try
                    {
                        var webResults = await AiWebSearchService.SearchAsync(searchKw).ConfigureAwait(false);
                        if (webResults != null && webResults.Count > 0)
                        {
                            string webContext = AiWebSearchService.FormatResultsForPrompt(searchKw, webResults);
                            promptToSend = basePrompt + "\n\n" + webContext;
                        }
                    }
                    catch { }

                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        try
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                UpdateThinkingBubbleStatus(AppL10n.T("✦ Đang phân tích ngữ liệu & khởi tạo Canvas...", "✦ Analyzing content & building Canvas..."), false);
                            }));
                        }
                        catch { }
                    }
                }

                promptToSend = BuildPromptWithDocumentContext(promptToSend);

                string curModel = _config != null ? (_config.ModelName ?? "") : "";
                bool isZenQSlideAi = curModel.IndexOf("zenq-slide-ai", StringComparison.OrdinalIgnoreCase) >= 0 || curModel.IndexOf("zenq", StringComparison.OrdinalIgnoreCase) >= 0;
                OfficeAppMode currentMode = _appForm != null && _appForm.Ribbon != null ? _appForm.Ribbon.CurrentMode : OfficeAppMode.Home;

                string effectiveSysPrompt = _config != null ? _config.SystemInstructions : "";
                List<AiChatMessage> historyToSend = targetTab != null ? targetTab.AiChatHistory : null;

                if (isZenQSlideAi || currentMode == OfficeAppMode.Slides)
                {
                    effectiveSysPrompt = "Bạn là ZenQ Slide AI - giám đốc sáng tạo thiết kế slide đa phương tiện với ý tưởng thị giác riêng, bố cục tự do và typography có chủ đích cho ZenQOffice. Khi có thông tin tra cứu từ Internet trong phần input, bạn luôn tổng hợp tri thức, trích dẫn nguồn uy tín bằng lời nói tự nhiên trên khung chat, sau đó xuất lệnh [ZENQ_ACTION:GENERATE_SLIDE_CANVAS] để kiến tạo slide hoàn chỉnh.";
                    
                    if (IsSlideCreationRequest(basePrompt))
                    {
                        historyToSend = null;
                    }
                }

                AiService.SendPromptStreamAsync(_config, effectiveSysPrompt, promptToSend, "", (chunk) =>
            {
                gen.FullResponseBuilder.Append(chunk);
                lock (_streamLock)
                {
                    _pendingStreamChunk += chunk;
                }
            }, (response) =>
            {
                gen.IsCompleted = true;
                string fullRaw = gen.FullResponseBuilder.Length > 0 ? gen.FullResponseBuilder.ToString() : (response.Text ?? "");

                // Kiểm tra và bóc tách dữ liệu Canvas nếu có
                AiCanvasQuizData detectedQuiz = null;
                if (AiCanvasParser.TryParse(fullRaw, out detectedQuiz))
                {
                    _lastCanvasQuiz = detectedQuiz;
                }

                // Xử lý hành động AI tự động
                AiActionResult actionResult = null;
                if (_appForm != null)
                {
                    try { actionResult = AiActionEngine.ProcessAndExecute(_appForm, fullRaw); } catch { }
                }

                string cleanMsg = CleanAiOutputForChat(fullRaw);
                string finalSummaryMsg = "";

                if (!response.Success && string.IsNullOrEmpty(fullRaw))
                {
                    finalSummaryMsg = !string.IsNullOrWhiteSpace(response.ErrorMessage) ? response.ErrorMessage : AppL10n.T("❌ Không nhận được phản hồi hợp lệ từ máy chủ AI.", "❌ No valid response received from the AI server.");
                }
                else if (actionResult != null && actionResult.HasAction)
                {
                    if (string.IsNullOrEmpty(cleanMsg))
                    {
                        finalSummaryMsg = actionResult.ExecutionSummary;
                    }
                    else
                    {
                        finalSummaryMsg = cleanMsg + "\n\n" + actionResult.ExecutionSummary;
                    }
                }
                else if (!string.IsNullOrEmpty(cleanMsg))
                {
                    finalSummaryMsg = cleanMsg;
                }
                else
                {
                    finalSummaryMsg = AppL10n.T("✦ Đã xử lý xong yêu cầu của bạn.", "✦ Completed your request.");
                }

                // Báº¢O Ä áº¢M LÆ¯U Bá»€N Vá»®NG VÃ€O TAB & Lá»ŠCH Sá»¬ Ká»‚ Cáº¢ KHI PANEL Ä Ãƒ Ä Ã“NG HOáº¶C Ä Ãƒ CHUYá»‚N TAB
                string quizJson = detectedQuiz != null ? detectedQuiz.ToJson() : null;
                var modelMsg = new AiChatMessage { Role = "model", Content = finalSummaryMsg, CanvasQuizJson = quizJson, Timestamp = DateTime.Now };
                if (targetTab != null)
                {
                    if (targetTab.AiChatHistory == null) targetTab.AiChatHistory = new List<AiChatMessage>();
                    targetTab.AiChatHistory.Add(modelMsg);
                    AiChatHistoryManager.SaveHistory(targetTab.AiChatHistory);
                    AiChatHistoryManager.SaveCurrentSession(targetSessionId, targetTab.AiChatHistory);
                }
                else
                {
                    var globalHist = AiChatHistoryManager.LoadHistory();
                    globalHist.Add(modelMsg);
                    AiChatHistoryManager.SaveHistory(globalHist);
                    AiChatHistoryManager.SaveCurrentSession(targetSessionId, globalHist);
                }

                // Ä á»“ng bá»™ cáº­p nháº­t lÃªn giao diá»‡n náº¿u panel cÃ²n tá»“n táº¡i
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                streamUiTimer.Stop();
                                streamUiTimer.Dispose();
                            }
                            catch { }

                            if (_currentTab == targetTab && _currentSessionId == targetSessionId)
                            {
                                _isAiStreaming = false;
                                pnlInputPill.Invalidate();
                                txtInput.Enabled = true;

                                if (this.Visible)
                                {
                                    if (detectedQuiz != null)
                                    {
                                        OpenCanvas(detectedQuiz);
                                    }

                                    if (gen.IsFirstChunk)
                                    {
                                        HideThinkingBubble();
                                        AddAiBubble(finalSummaryMsg, detectedQuiz);
                                    }
                                    else
                                    {
                                        if (_streamingAiLabel != null)
                                        {
                                            _streamingAiLabel.Text = finalSummaryMsg;
                                        }
                                        if (detectedQuiz != null)
                                        {
                                            if (_streamingAiRow != null)
                                            {
                                                pnlChatFeed.RemoveVirtualControl(_streamingAiRow);
                                                _streamingAiRow = null;
                                                _streamingAiBubble = null;
                                                _streamingAiLabel = null;
                                            }
                                            AddAiBubble(finalSummaryMsg, detectedQuiz);
                                        }
                                        else
                                        {
                                            if (_streamingAiLabel != null)
                                            {
                                                _streamingAiLabel.Text = finalSummaryMsg;
                                            }
                                            FinishAiStreamingBubble();
                                        }
                                    }
                                    ScrollToBottomForce();
                                }
                            }

                            lock (_activeGenerations)
                            {
                                _activeGenerations.Remove(targetSessionId);
                            }
                        }));
                    }
                    catch
                    {
                        lock (_activeGenerations)
                        {
                            _activeGenerations.Remove(targetSessionId);
                        }
                    }
                }
                else
                {
                    lock (_activeGenerations)
                    {
                        _activeGenerations.Remove(targetSessionId);
                    }
                }
            }, historyToSend, () => (gen.AbortRequested || _abortAiStream));
            });
        }

        private bool IsUserAgreement(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return false;
            q = q.Trim().ToLowerInvariant().Trim('.', '!', '?', ',', ' ');

            string[] exacts = { "đồng ý", "dong y", "ok", "oke", "okay", "ừ", "u", "uh", "ừm", "um", "áp dụng", "ap dung", 
                                "áp dụng đi", "ap dung di", "sửa đi", "sua di", "sửa luôn", "sua luon", "chấp nhận", "chap nhan",
                                "thay đổi", "thay doi", "được", "duoc", "được rồi", "duoc roi", "yes", "y", "apply", "accept",
                                "confirm", "tiến hành", "chấp thuận", "làm đi", "lam di", "lưu", "luu", "chốt", "chot" };
            foreach (var word in exacts)
            {
                if (q == word) return true;
            }

            if (q.StartsWith("đồng ý") || q.StartsWith("dong y") || q.StartsWith("áp dụng") || q.StartsWith("ap dung") ||
                q.StartsWith("chấp nhận") || q.StartsWith("ok ") || q.StartsWith("oke ") || q.StartsWith("sửa đi") ||
                q.Contains("áp dụng giúp") || q.Contains("áp dụng giùm") || q.Contains("sửa giúp") || q.Contains("sửa giùm"))
            {
                return true;
            }
            return false;
        }

        private bool IsUserRejection(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return false;
            q = q.Trim().ToLowerInvariant().Trim('.', '!', '?', ',', ' ');

            string[] exacts = { "từ chối", "tu choi", "thôi", "thoi", "thôi khỏi", "thoi khoi", "hủy", "huy", "hủy bỏ", "huy bo",
                                "đừng", "dung", "đừng sửa", "dung sua", "không", "khong", "ko", "k", "không cần", "khong can",
                                "giữ nguyên", "giu nguyen", "bỏ qua", "bo qua", "bỏ", "bo", "cancel", "no", "reject", "dismiss",
                                "khỏi sửa", "khoi sua", "không sửa", "khong sua", "dừng", "dung lai" };
            foreach (var word in exacts)
            {
                if (q == word) return true;
            }

            if (q.StartsWith("từ chối") || q.StartsWith("tu choi") || q.StartsWith("không cần") || q.StartsWith("khong can") ||
                q.StartsWith("đừng sửa") || q.StartsWith("dung sua") || q.StartsWith("thôi") || q.StartsWith("hủy") ||
                q.Contains("giữ nguyên") || q.Contains("không sửa") || q.Contains("khỏi sửa"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hiện animation "AI đang suy nghĩ..." chuẩn Breathing Light 100% KHÔNG giật/flicker.
        /// </summary>
        private void ShowThinkingBubble(string statusText = null, bool isSearch = false)
        {
            HideThinkingBubble();

            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;

            ThinkingBubbleControl bubble = new ThinkingBubbleControl(statusText, isSearch);
            bubble.Location = new Point(2, 0);
            _currentThinkingControl = bubble;

            Panel row = new AiGpuPanel
            {
                Width = feedW,
                Height = 38,
                BackColor = Color.Transparent,
                Margin = new Padding(2, 3, 2, 5)
            };
            row.Controls.Add(bubble);

            _thinkingBubble = row;
            pnlChatFeed.AddVirtualControl(row);
            ScrollToBottomForce();
        }

        private void UpdateThinkingBubbleStatus(string statusText, bool isSearch = false)
        {
            if (_currentThinkingControl != null && !_currentThinkingControl.IsDisposed)
            {
                _currentThinkingControl.SetStatus(statusText, isSearch);
                ScrollToBottomIfNear();
            }
        }

        private void HideThinkingBubble()
        {
            if (_thinkingBubble != null)
            {
                pnlChatFeed.RemoveVirtualControl(_thinkingBubble);
                _thinkingBubble.Dispose();
                _thinkingBubble = null;
            }
        }

        /// <summary>
        /// Tin nháº¯n ngÆ°á» i dÃ¹ng â€” cÄƒn bÃªn PHáº¢I, gradient tÃ­m, kÃ¨m nÃºt â†© Há» i láº¡i vÃ  ðŸ“‹ Sao chÃ©p.
        /// </summary>
        private void AddUserBubble(string text, int msgIndex = -1)
        {
            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;

            if (msgIndex < 0 && _currentTab != null && _currentTab.AiChatHistory != null)
            {
                msgIndex = _currentTab.AiChatHistory.Count - 1;
            }

            UserChatBubbleControl bubble = new UserChatBubbleControl(text, msgIndex, feedW, OnEditAndResend);
            pnlChatFeed.AddVirtualControl(bubble);
            ScrollToBottomForce();
        }

        /// <summary>
        /// Tin nháº¯n AI â€” cÄƒn bÃªn TRÃ I, ná» n tráº¯ng viá» n xÃ¡m, kÃ¨m nÃºt hÃ nh Ä‘á»™ng chÃ¨n vÃ  ðŸ“‹ Sao chÃ©p.
        /// </summary>
        private void AddAiBubble(string text, AiCanvasQuizData canvasQuiz = null)
        {
            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;

            string displayText = CleanAiOutputForChat(text);
            if (string.IsNullOrEmpty(displayText))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.IndexOf("CALC", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("cells", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("spreadsheet", StringComparison.OrdinalIgnoreCase) >= 0)
                        displayText = AppL10n.T("✦ Đã cập nhật và ghi dữ liệu thành công vào Bảng tính.", "✦ Updated spreadsheet data successfully.");
                    else if (text.IndexOf("GENERATE_SLIDE", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("slides", StringComparison.OrdinalIgnoreCase) >= 0)
                        displayText = AppL10n.T("✦ Đã kiến tạo thành công bộ Slide theo yêu cầu của bạn.", "✦ Created presentation slides successfully.");
                    else if (text.IndexOf("MODIFY_SLIDE", StringComparison.OrdinalIgnoreCase) >= 0)
                        displayText = AppL10n.T("✦ Đã sửa đổi cấu hình và hiệu ứng trên Slide.", "✦ Updated slide layout and effects.");
                    else
                        displayText = text.Trim();
                }
                else
                {
                    displayText = AppL10n.T("✦ Đã xử lý xong yêu cầu của bạn.", "✦ Done.");
                }
            }

            OfficeAppMode curMode = _appForm != null ? _appForm.Ribbon.CurrentMode : OfficeAppMode.Home;

            AiChatBubbleControl bubble = new AiChatBubbleControl(displayText, feedW, curMode, (insertText) =>
            {
                if (_appForm == null) return;
                try
                {
                    if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Slides && _appForm.SlidesView != null)
                    {
                        _appForm.SlidesView.InsertTextFromAI(insertText);
                        _appForm.Invalidate();
                    }
                    else if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Writer && _appForm.WriterView != null)
                    {
                        if (!string.IsNullOrEmpty(_appForm.WriterView.GetSelectedText()))
                        {
                            _appForm.WriterView.ReplaceSelectedText(insertText, true);
                        }
                        else if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Pdf && _appForm.PdfView != null)
                        {
                            Clipboard.SetText(insertText);
                            ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Đã sao chép nội dung phản hồi từ AI vào bộ nhớ tạm (Clipboard).", "Copied AI response to Clipboard."), "ZENQ AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Calc && _appForm.CalcView != null)
                        {
                            Clipboard.SetText(insertText);
                            ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Đã sao chép nội dung phản hồi từ AI vào bộ nhớ tạm (Clipboard).", "Copied AI response to Clipboard."), "ZENQ AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        _appForm.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(ex.ToString(), AppL10n.T("ZENQ AI - Lỗi Chèn Text", "ZENQ AI - Insert Error"));
                }
            }, _appForm, canvasQuiz, () => {
                var targetQuiz = canvasQuiz ?? _lastCanvasQuiz;
                if (targetQuiz != null) OpenCanvas(targetQuiz);
            }, (quiz) => {
                var targetQuiz = quiz ?? _lastCanvasQuiz;
                if (targetQuiz != null) ExportQuizToWriter(targetQuiz);
            });

            pnlChatFeed.AddVirtualControl(bubble);
            ScrollToBottomForce();
        }

        private void ScrollToBottomForce()
        {
            if (pnlChatFeed == null) return;
            try
            {
                pnlChatFeed.ScrollToBottomForce();
            }
            catch { }
        }

        private void ScrollToBottomIfNear()
        {
            if (pnlChatFeed == null) return;
            try
            {
                pnlChatFeed.ScrollToBottomIfNear();
            }
            catch { }
        }


        private GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath gp = new GraphicsPath();
            int d = radius * 2;
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }

                #region Context Extraction
        private static bool IsSlideCreationRequest(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            string q = query.ToLowerInvariant();
            return q.Contains("slide") || q.Contains("silde") || q.Contains("thuyết trình") || 
                   q.Contains("trình chiếu") || q.Contains("tạo bài") || q.Contains("hàng loạt") ||
                   q.Contains("presentation") || q.Contains("deck") || q.Contains("trang chiếu") ||
                   q.Contains("morph") || q.Contains("chuyển cảnh") || q.Contains("chuyển tiếp") ||
                   q.Contains("transition") || q.Contains("hiệu ứng") || q.Contains("thiết kế") ||
                   q.Contains("bố cục") || q.Contains("phong cách") || q.Contains("làm lại") ||
                   q.Contains("redesign") || q.Contains("giao diện") || q.Contains("card") ||
                   q.Contains("màu xấu") || q.Contains("bố cục xấu") || q.Contains("thay đổi") ||
                   q.Contains("mở đầu") || q.Contains("trang bìa") || q.Contains("bìa") ||
                   q.Contains("nội dung") || q.Contains("thân") || q.Contains("trang kết") ||
                   q.Contains("kết thúc") || q.Contains("cảm ơn") || q.Contains("quy trình") ||
                   q.Contains("kpi") || q.Contains("thống kê") || q.Contains("tiến độ") ||
                   q.Contains("5 slide") || q.Contains("10 slide") || q.Contains("3 slide") ||
                   q.Contains("tạo 1") || q.Contains("tạo một") || q.Contains("tạo trang");
        }

        private static bool IsCalcModificationRequest(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            string q = query.ToLowerInvariant();
            return q.Contains("tạo bảng") || q.Contains("kẻ bảng") || q.Contains("tô màu") || 
                   q.Contains("đổi màu") || q.Contains("gộp ô") || q.Contains("chèn bảng") ||
                   q.Contains("thêm cột") || q.Contains("thêm hàng") || q.Contains("điền vào ô") ||
                   q.Contains("ghi vào ô") || q.Contains("set cell") || q.Contains("định dạng bảng") ||
                   q.Contains("chèn cột") || q.Contains("chèn hàng");
        }

        private string BuildPromptWithDocumentContext(string userQuery)
        {
            OfficeAppMode mode = _appForm != null ? _appForm.Ribbon.CurrentMode : OfficeAppMode.Home;
            StringBuilder sb = new StringBuilder();

            if (mode == OfficeAppMode.Slides && _appForm != null && _appForm.SlidesView != null)
            {
                string curModel = _config != null ? (_config.ModelName ?? "") : "";
                bool isZenQSlideAi = curModel.IndexOf("zenq-slide-ai", StringComparison.OrdinalIgnoreCase) >= 0 || curModel.IndexOf("zenq", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isZenQSlideAi)
                {
                    // Với mô hình fine-tuned cục bộ ZenQ Slide AI:
                    // Mô hình đã học sẵn toàn bộ cấu trúc và tri thức thiết kế trong weights.
                    // Chỉ gửi thẳng câu lệnh tự nhiên của người dùng, không nhồi nhét tài liệu/system prompt dài dòng làm loãng attention và gây văng hallucination!
                    return userQuery;
                }

                var pres = _appForm.SlidesView.Presentation;
                int curIdx = _appForm.SlidesView.ActiveSlideIndex;
                SlideNode curSlide = (pres != null && curIdx >= 0 && curIdx < pres.Slides.Count) ? pres.Slides[curIdx] : null;

                sb.AppendLine("<current_slide_context>");
                sb.AppendLine(string.Format("Tổng số Slide hiện tại: {0}, Slide đang chọn: {1}", pres != null ? pres.Slides.Count : 0, curIdx + 1));

                if (curSlide != null)
                {
                    List<string> curTexts = new List<string>();
                    foreach (var sh in curSlide.Shapes)
                    {
                        if (!string.IsNullOrWhiteSpace(sh.Text)) curTexts.Add(sh.Text.Trim().Replace('\r', ' ').Replace('\n', ' '));
                    }
                    sb.AppendLine(string.Format("Nội dung Slide đang chọn ({0}): {1}", curSlide.Title, string.Join(" | ", curTexts)));
                }

                if (!isZenQSlideAi && pres != null && pres.Slides.Count > 0)
                {
                    sb.AppendLine("Danh sách slide hiện tại:");
                    int maxList = Math.Min(10, pres.Slides.Count);
                    for (int sIdx = 0; sIdx < maxList; sIdx++)
                    {
                        var s = pres.Slides[sIdx];
                        sb.AppendLine(string.Format("Slide {0}: {1}", sIdx + 1, s.Title));
                    }
                }
                sb.AppendLine("</current_slide_context>");

                if (IsSlideCreationRequest(userQuery))
                {
                    string curSlideTitle = curSlide != null ? curSlide.Title : "";
                    sb.AppendLine(SlideDesignMetadata.BuildSlidePrompt(userQuery, pres != null ? pres.Slides.Count : 0, curIdx, curSlideTitle, pres?.CanvasWidth ?? 1920, pres?.CanvasHeight ?? 1080));

                }
            }
            else if (mode == OfficeAppMode.Writer && _appForm != null && _appForm.WriterView != null)
            {
                string docText = AiSlashCommandEngine.ExtractWriterTextWithFormatting(_appForm.WriterView, 45000);
                string selText = _appForm.WriterView.GetSelectedText();

                if (docText.Length > 45000) docText = docText.Substring(0, 45000) + "... [Đã rút gọn vì quá dài]";
                if (!string.IsNullOrWhiteSpace(docText))
                {
                    sb.AppendLine("<document_context>");
                    sb.AppendLine(docText);
                    sb.AppendLine("</document_context>");
                }
                if (!string.IsNullOrWhiteSpace(selText))
                {
                    sb.AppendLine("<selected_text>");
                    sb.AppendLine(selText);
                    sb.AppendLine("</selected_text>");
                }
                sb.AppendLine();
                sb.AppendLine("[HƯỚNG DẪN TRỢ LÝ SOẠN THẢO VĂN BẢN & TÌM SỬA LỖI / VIẾT BÀI]:");
                sb.AppendLine("1. KHI NGƯỜI DÙNG YÊU CẦU TÌM LỖI, SỬA CHÍNH TẢ, SOÁT LỖI, SỬA CÂU, THAY THẾ HOẶC VIẾT LẠI:");
                sb.AppendLine("   - BẮT BUỘC TRẢ LỜI NGẮN GỌN trong khung chat: 'Tôi đã tạo đề xuất sửa đổi trực tiếp trên trang (đoạn màu đỏ sẽ xoá, đoạn màu xanh sẽ thêm). Bạn hãy nhấp Đồng ý để áp dụng hoặc Từ chối để giữ nguyên.'");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG lặp lại toàn bộ tài liệu dài dòng trong khung chat.");
                sb.AppendLine("   - Kèm theo các thẻ hành động thay thế: <replace find=\"từ_hoặc_câu_cần_thay\" replace=\"từ_hoặc_câu_mới\" /> HOẶC khối [ZENQ_ACTION:APPLY_SPELL_CORRECTIONS].");
                sb.AppendLine("   - Nếu có <selected_text> (vùng đang quét khối), CHỈ tìm và sửa lỗi BÊN TRONG thẻ <selected_text>.");
                sb.AppendLine();
                sb.AppendLine("2. KHI NGƯỜI DÙNG YÊU CẦU VIẾT VĂN BẢN DÀI, BÀI VĂN / TIỂU LUẬN / PHÂN TÍCH (1000 từ, 2000 từ, viết chi tiết...):");
                sb.AppendLine("   - Hãy viết thật đầy đủ, chi tiết từng phần (Mở bài, Thân bài gồm nhiều luận điểm và ví dụ phân tích sâu sắc, Kết luận).");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG ĐƯỢC tóm tắt ngắn ngủn hoặc kết bài sớm. Viết đầy đủ dung lượng mà người dùng yêu cầu.");
                sb.AppendLine("3. TUYỆT ĐỐI KHÔNG ĐƯỢC HỎI LẠI NGƯỜI DÙNG khi dữ liệu đã có sẵn.");
            }
            else if (mode == OfficeAppMode.Calc && _appForm != null && _appForm.CalcView != null)
            {
                var calc = _appForm.CalcView;
                var sheet = calc.CurrentSheet;
                string activeSheetName = (calc.SheetNames != null && calc.ActiveSheetIndex >= 0 && calc.ActiveSheetIndex < calc.SheetNames.Count) 
                    ? calc.SheetNames[calc.ActiveSheetIndex] : "Sheet1";
                string selAddr = SparseMatrix.ColIndexToName(calc.SelectedCol) + calc.SelectedRow;

                uint minSelR = Math.Max(1, Math.Min(calc.SelectedRow, calc.SelEndRow > 0 ? calc.SelEndRow : calc.SelectedRow));
                uint maxSelR = Math.Max(calc.SelectedRow, calc.SelEndRow > 0 ? calc.SelEndRow : calc.SelectedRow);
                uint minSelC = Math.Max(1, Math.Min(calc.SelectedCol, calc.SelEndCol > 0 ? calc.SelEndCol : calc.SelectedCol));
                uint maxSelC = Math.Max(calc.SelectedCol, calc.SelEndCol > 0 ? calc.SelEndCol : calc.SelectedCol);

                bool isMultiCellSelected = (minSelR != maxSelR || minSelC != maxSelC);
                string selRangeStr = isMultiCellSelected 
                    ? string.Format("{0}:{1}", SparseMatrix.GetCellReference(minSelR, minSelC), SparseMatrix.GetCellReference(maxSelR, maxSelC))
                    : SparseMatrix.GetCellReference(minSelR, minSelC);

                sb.AppendLine("<calc_context>");
                sb.AppendLine("Trang tính hiện tại: " + activeSheetName);
                sb.AppendLine("Ô đang chọn (Active Cell): " + selAddr);
                sb.AppendLine(string.Format("Vùng ô đang quét khối / lựa chọn (Selected Range): {0}", selRangeStr));

                // 1. Phân tích chi tiết vùng ô đang quét khối (Selected Range)
                sb.AppendLine("<selected_cells_range>");
                sb.AppendLine(string.Format("Vùng chọn: {0} ({1} hàng x {2} cột)", selRangeStr, (maxSelR - minSelR + 1), (maxSelC - minSelC + 1)));

                double numSum = 0;
                int numCount = 0;
                int totalSelCells = 0;
                double numMin = double.MaxValue;
                double numMax = double.MinValue;

                List<string> selRowSummaries = new List<string>();
                for (uint r = minSelR; r <= Math.Min(maxSelR, minSelR + 100); r++)
                {
                    List<string> cellTexts = new List<string>();
                    for (uint c = minSelC; c <= Math.Min(maxSelC, minSelC + 50); c++)
                    {
                        totalSelCells++;
                        var cell = sheet != null ? sheet.GetCell(r, c) : null;
                        if (cell != null)
                        {
                            string disp = cell.GetDisplayText();
                            string refStr = SparseMatrix.GetCellReference(r, c);
                            if (cell.Type == CellType.Formula)
                                cellTexts.Add(string.Format("[{0}]={1} ({2})", refStr, cell.FormulaStr, disp));
                            else if (!string.IsNullOrWhiteSpace(disp))
                                cellTexts.Add(string.Format("[{0}] {1}", refStr, disp));

                            double val;
                            if (double.TryParse(cell.RawValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out val) ||
                                double.TryParse(cell.RawValue, out val))
                            {
                                numSum += val;
                                numCount++;
                                if (val < numMin) numMin = val;
                                if (val > numMax) numMax = val;
                            }
                        }
                    }
                    if (cellTexts.Count > 0)
                    {
                        selRowSummaries.Add(string.Format("Hàng {0}: {1}", r, string.Join(" | ", cellTexts.ToArray())));
                    }
                }

                if (numCount > 0)
                {
                    double numAvg = numSum / numCount;
                    sb.AppendLine(string.Format("Thống kê số liệu vùng quét khối: Tổng cộng (Sum) = {0}, Trung bình (Average) = {1:F2}, Nhỏ nhất (Min) = {2}, Lớn nhất (Max) = {3}, Số ô có giá trị số = {4}/{5}",
                        numSum, numAvg, numMin, numMax, numCount, totalSelCells));
                }

                sb.AppendLine("Chi tiết nội dung các ô trong vùng quét khối:");
                if (selRowSummaries.Count > 0)
                {
                    foreach (var rowLine in selRowSummaries) sb.AppendLine(rowLine);
                }
                else
                {
                    sb.AppendLine("(Các ô trong vùng quét khối hiện đang trống)");
                }
                sb.AppendLine("</selected_cells_range>");

                // 2. Toàn cảnh dữ liệu bảng tính
                if (sheet != null)
                {
                    // Quét các ô có dữ liệu
                    uint minR = uint.MaxValue, maxR = 0, minC = uint.MaxValue, maxC = 0;
                    var allCells = sheet.GetAllCells();
                    int cellCount = 0;
                    foreach (var kvp in allCells)
                    {
                        var coord = SparseMatrix.DecodeKey(kvp.Key);
                        uint r = coord.Item1;
                        uint c = coord.Item2;
                        if (r < minR) minR = r;
                        if (r > maxR) maxR = r;
                        if (c < minC) minC = c;
                        if (c > maxC) maxC = c;
                        cellCount++;
                    }

                    if (cellCount > 0 && maxR > 0 && maxC > 0)
                    {
                        sb.AppendLine(string.Format("Dữ liệu toàn bảng tính ({0} ô từ {1} đến {2}):", 
                            cellCount, 
                            SparseMatrix.GetCellReference(minR, minC), 
                            SparseMatrix.GetCellReference(maxR, maxC)));
                        
                        // Render dạng bảng rút gọn (tối đa 40 dòng x 15 cột để tiết kiệm token)
                        uint displayMaxR = Math.Min(maxR, minR + 35);
                        uint displayMaxC = Math.Min(maxC, minC + 12);

                        for (uint r = minR; r <= displayMaxR; r++)
                        {
                            List<string> rowTexts = new List<string>();
                            for (uint c = minC; c <= displayMaxC; c++)
                            {
                                var cell = sheet.GetCell(r, c);
                                if (cell != null)
                                {
                                    string refStr = SparseMatrix.GetCellReference(r, c);
                                    if (cell.Type == CellType.Formula)
                                        rowTexts.Add(string.Format("[{0}]={1} ({2})", refStr, cell.FormulaStr, cell.GetDisplayText()));
                                    else
                                        rowTexts.Add(string.Format("[{0}] {1}", refStr, cell.GetDisplayText()));
                                }
                            }
                            if (rowTexts.Count > 0)
                            {
                                selRowSummaries.Add(string.Format("Hàng {0}: {1}", r, string.Join(" | ", rowTexts.ToArray())));
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("Bảng tính hiện đang trống (chưa có dữ liệu).");
                    }
                }
                sb.AppendLine("</calc_context>");

                sb.AppendLine();
                sb.AppendLine("[TRI THỨC & NĂNG LỰC TƯƠNG TÁC BẢNG TÍNH CALC]:");
                sb.AppendLine("BẠN LÀ TRỢ LÝ AI THÔNG MINH CÓ KHẢ NĂNG GHI TRỰC TIẾP DỮ LIỆU VÀO TRANG TÍNH.");
                sb.AppendLine();
                sb.AppendLine("1. KHI NGƯỜI DÙNG YÊU CẦU: TẠO BẢNG, THAY BẢNG, MỞ RỘNG BẢNG, THÊM CỘT/HÀNG, VIẾT SỐ LIỆU, TÍNH TOÁN, HOẶC THÊM BẢNG PHÂN TÍCH:");
                sb.AppendLine("   - BẮT BUỘC XUẤT RA KHỐI HÀNH ĐỘNG: [ZENQ_ACTION:SET_CALC_DATA] ... [/ZENQ_ACTION] (hoặc bảng Markdown | Cột 1 | Cột 2 | ...)");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG ĐƯỢC CHỈ NÓI MỒM HAY GỢI Ý BẰNG CHỮ TRONG CHAT! PHẢI XUẤT RA DỮ LIỆU ĐỂ HỆ THỐNG GHI THẲNG VÀO BẢNG TÍNH!");
                sb.AppendLine("   - NGUYÊN TẮC BỐ CỤC BẢNG ĐẸP VÀ CHUYÊN NGHIỆP:");
                sb.AppendLine("     + Nếu thêm BẢNG PHÂN TÍCH / SO SÁNH / KPI phụ bên cạnh bảng chính: ĐẶT BẢNG NGANG SANG PHÍA BÊN PHẢI bảng hiện tại (bắt đầu cùng hàng 3) và CÁCH ÍT NHẤT 1 CỘT TRỐNG (ví dụ bảng cũ tới cột G thì bảng mới bắt đầu từ cột I3).");
                sb.AppendLine("     + Hoặc nếu đặt BÊN DƯỚI bảng cũ: phải CÁCH ÍT NHẤT 2-3 HÀNG TRỐNG sau dòng Tổng Cộng / Trung Bình (ví dụ bảng cũ hết ở dòng 15 thì bảng mới bắt đầu ở dòng 18).");
                sb.AppendLine("     + TUYỆT ĐỐI KHÔNG VẼ BẢNG ĐÈ LÊN DÒNG TỔNG CỘNG HAY DÒNG TRUNG BÌNH CỦA BẢNG CŨ!");
                sb.AppendLine("   - Cấu trúc JSON chuẩn:");
                sb.AppendLine("     [ZENQ_ACTION:SET_CALC_DATA]");
                sb.AppendLine("     {");
                sb.AppendLine("       \"clearExisting\": true, // true nếu thay thế bảng cũ, false nếu chỉ ghi thêm");
                sb.AppendLine("       \"ranges\": [");
                sb.AppendLine("         { \"range\": \"A3:H3\", \"bgColor\": \"#1E293B\", \"textColor\": \"#FFFFFF\", \"bold\": true, \"align\": \"center\" }");
                sb.AppendLine("       ],");
                sb.AppendLine("       \"cells\": [");
                sb.AppendLine("         { \"cell\": \"A3\", \"value\": \"Mã SP\" },");
                sb.AppendLine("         { \"cell\": \"B3\", \"value\": \"Tên Sản Phẩm\" },");
                sb.AppendLine("         { \"cell\": \"C3\", \"value\": \"Số Lượng\" },");
                sb.AppendLine("         { \"cell\": \"D3\", \"value\": \"Đơn Giá ($)\" },");
                sb.AppendLine("         { \"cell\": \"E3\", \"value\": \"Doanh Thu ($)\" },");
                sb.AppendLine("         { \"cell\": \"F3\", \"value\": \"Chi Phí ($)\" },");
                sb.AppendLine("         { \"cell\": \"G3\", \"value\": \"Lợi Nhuận ($)\" },");
                sb.AppendLine("         { \"cell\": \"A4\", \"value\": \"SP-001\" },");
                sb.AppendLine("         { \"cell\": \"B4\", \"value\": \"GenOffice Suite\" },");
                sb.AppendLine("         { \"cell\": \"C4\", \"value\": 250 },");
                sb.AppendLine("         { \"cell\": \"D4\", \"value\": 120 },");
                sb.AppendLine("         { \"cell\": \"E4\", \"formula\": \"=C4*D4\" },");
                sb.AppendLine("         { \"cell\": \"F4\", \"value\": 10000 },");
                sb.AppendLine("         { \"cell\": \"G4\", \"formula\": \"=E4-F4\" }");
                sb.AppendLine("       ]");
                sb.AppendLine("     }");
                sb.AppendLine("     [/ZENQ_ACTION]");
                sb.AppendLine();
                sb.AppendLine("2. KHI NGƯỜI DÙNG CHỈ ĐẶT CÂU HỎI PHÂN TÍCH / TÌM KIẾM THÔNG TIN CÓ SẴN:");
                sb.AppendLine("   - Đọc trực tiếp các con số trong bảng tính ở trên, tính toán và trả lời ngay lập tức.");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG HỎI LẠI! Dữ liệu đã có đủ 100%.");
            }
            else if (mode == OfficeAppMode.Pdf && _appForm != null && _appForm.PdfView != null && _appForm.PdfView.Document != null)
            {
                var pdfView = _appForm.PdfView;
                var pdfDoc = pdfView.Document;
                int totalPages = (pdfDoc.Pages != null) ? pdfDoc.Pages.Count : 0;
                int activePage = Math.Max(0, Math.Min(pdfView.ActivePageIndex, Math.Max(0, totalPages - 1)));
                string docTitle = !string.IsNullOrEmpty(pdfDoc.Title) ? pdfDoc.Title : (!string.IsNullOrEmpty(pdfDoc.FilePath) ? Path.GetFileName(pdfDoc.FilePath) : "Tài liệu PDF");
                string selText = (pdfView.SelectedText != null && !string.IsNullOrEmpty(pdfView.SelectedText.Text)) ? pdfView.SelectedText.Text : "";

                sb.AppendLine("<pdf_context>");
                sb.AppendLine(string.Format("Tài liệu PDF: {0}", docTitle));
                sb.AppendLine(string.Format("Tổng số trang: {0}, Trang đang xem: {1}", totalPages, activePage + 1));

                if (!string.IsNullOrWhiteSpace(selText))
                {
                    sb.AppendLine("<selected_text>");
                    sb.AppendLine(selText);
                    sb.AppendLine("</selected_text>");
                }

                // Outline / Bookmarks (Table of Contents)
                if (pdfDoc.Bookmarks != null && pdfDoc.Bookmarks.Count > 0)
                {
                    sb.AppendLine("<table_of_contents>");
                    foreach (var bm in pdfDoc.Bookmarks)
                    {
                        sb.AppendLine(string.Format("- {0} (Trang {1})", bm.Title, bm.TargetPageIndex + 1));
                    }
                    sb.AppendLine("</table_of_contents>");
                }

                // Trang hiện tại
                string curPageText = PdfConverterEngine.ExtractPageText(pdfDoc, activePage);
                if (string.IsNullOrWhiteSpace(curPageText))
                {
                    try { pdfView.AutoDetectTextForPage(activePage); curPageText = PdfConverterEngine.ExtractPageText(pdfDoc, activePage); } catch { }
                }
                sb.AppendLine(string.Format("<current_page number=\"{0}\">", activePage + 1));
                sb.AppendLine(!string.IsNullOrWhiteSpace(curPageText) ? curPageText : "[Trang này chủ yếu là hình ảnh/biểu đồ scan, không chứa văn bản ký tự]");
                sb.AppendLine("</current_page>");

                // Ngữ cảnh tài liệu tổng quát nếu yêu cầu tóm tắt hoặc tài liệu ngắn
                string qLower = (userQuery ?? "").ToLowerInvariant();
                bool isGeneralSummaryQuery = string.IsNullOrWhiteSpace(selText) && 
                    (qLower.Contains("tóm tắt") || 
                     qLower.Contains("tổng quan") || 
                     qLower.Contains("toàn bộ") ||
                     qLower.Contains("/summary") ||
                     qLower.Contains("nội dung tài liệu"));

                if (isGeneralSummaryQuery || totalPages <= 5)
                {
                    var docSb = new StringBuilder();
                    int charBudget = 25000;
                    for (int p = 0; p < totalPages && docSb.Length < charBudget; p++)
                    {
                        if (p == activePage) continue;
                        string pTxt = PdfConverterEngine.ExtractPageText(pdfDoc, p);
                        if (!string.IsNullOrWhiteSpace(pTxt))
                        {
                            docSb.AppendLine(string.Format("--- [Trang {0}] ---", p + 1));
                            docSb.AppendLine(pTxt.Length > 2000 ? pTxt.Substring(0, 2000) + "..." : pTxt);
                            docSb.AppendLine();
                        }
                    }
                    if (docSb.Length > 0)
                    {
                        sb.AppendLine("<document_pages_context>");
                        sb.AppendLine(docSb.ToString());
                        sb.AppendLine("</document_pages_context>");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("[HƯỚNG DẪN TRỢ LÝ ĐỌC & PHÂN TÍCH TÀI LIỆU PDF CHUYÊN NGHIỆP]:");
                sb.AppendLine("1. BẠN CÓ TOÀN BỘ NGỮ CẢNH CỦA TÀI LIỆU PDF TRÊN.");
                sb.AppendLine("   - Khi người dùng hỏi hoặc yêu cầu 'Tóm tắt nội dung tài liệu này': hãy dựa vào mục lục, nội dung các trang đã trích xuất ở trên để tóm tắt chi tiết, nêu rõ chủ đề, các phần chính và các luận điểm quan trọng.");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG TRẢ LỜI RẰNG 'Tôi chưa nhận được nội dung' vì dữ liệu đã được trích xuất đầy đủ trong ngữ cảnh <pdf_context>.");
                sb.AppendLine("2. HÀNH ĐỘNG ĐIỀU HƯỚNG TRANG PDF:");
                sb.AppendLine("   - Khi dẫn chứng thông tin ở trang cụ thể, bạn có thể xuất thẻ XML để phần mềm tự động cuộn đến trang đó:");
                sb.AppendLine("     <goto_page page=\"X\" /> (Ví dụ: <goto_page page=\"3\" />)");
                sb.AppendLine("</pdf_context>");
            }

            // 0. Multi-Turn Context Memory: Nhớ các tin nhắn trước của phiên chat tab hiện tại (ít nhất 1 tin nhắn trước)
            if (_currentTab != null && _currentTab.AiChatHistory != null && _currentTab.AiChatHistory.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("<previous_conversation_context>");
                sb.AppendLine("Lịch sử trao đổi gần nhất trong phiên làm việc của tab này:");
                int hCount = _currentTab.AiChatHistory.Count;
                int startH = Math.Max(0, hCount - 4); // Lấy đến 4 tin nhắn gần nhất
                for (int hi = startH; hi < hCount; hi++)
                {
                    var hMsg = _currentTab.AiChatHistory[hi];
                    string roleStr = (hMsg.Role == "user") ? "Người dùng" : "Trợ lý AI";
                    sb.AppendLine(string.Format("{0}: {1}", roleStr, hMsg.Content));
                }
                sb.AppendLine("</previous_conversation_context>");
            }

            sb.AppendLine();
            sb.AppendLine("[HƯỚNG DẪN THỰC THI THAO TÁC BẰNG XML TỰ DO (XML ACTION ENGINE)]:");
            sb.AppendLine("Bạn có toàn quyền sáng tạo và thực hiện bất kỳ thao tác nào bằng cách tự do xuất các thẻ XML trực tiếp:");
            sb.AppendLine("- Bảng tính (Calc):");
            sb.AppendLine("  + Tô màu ô/vùng: <highlight range=\"A1:A10\" color=\"vàng nhạt\" /> (hoặc color=\"#FEF08A\", color=\"xanh lá nhạt\", bold=\"true\",...)");
            sb.AppendLine("  + Điền công thức hoặc giá trị: <set cell=\"A11\" formula=\"=SUM(A1:A10)\" /> (Tự do dùng các hàm tối ưu: SUM, AVERAGE, COUNT, MAX, MIN, IF, VLOOKUP,...)");
            sb.AppendLine("  + Tạo Sheet: <create_sheet name=\"Tóm tắt\" />");
            sb.AppendLine("  + Ghi dữ liệu vào Sheet: <write sheet=\"Tóm tắt\" cell=\"A1\">Nội dung tóm tắt...</write>");
            sb.AppendLine("  + Xóa vùng ô: <clear range=\"A1:C10\" />");
            if(mode != OfficeAppMode.Slides || !IsSlideCreationRequest(userQuery))
            {
            sb.AppendLine("- Trình chiếu (Slides):");
            sb.AppendLine("  + Tạo slide tự do: <create_slide title=\"Tiêu đề\" background=\"#0F172A\"><shape type=\"rectangle\" x=\"100\" y=\"150\" width=\"400\" height=\"200\" fill=\"#1E293B\" /><text x=\"120\" y=\"170\" font_size=\"24\" color=\"#38BDF8\">Nội dung</text></create_slide>");
            }
            sb.AppendLine("- Soạn thảo (Writer): <insert_text>Nội dung...</insert_text>, <replace find=\"từ cũ\" replace=\"từ mới\" />");
            sb.AppendLine("Hệ thống sẽ tự động bóc tách và thực thi chính xác!");

            sb.AppendLine();
            sb.AppendLine("<user_question>");
            sb.AppendLine(userQuery);
            sb.AppendLine("</user_question>");
            sb.AppendLine();
            sb.AppendLine("HÃY TRẢ LỜI TRỰC TIẾP CÂU HỎI TRÊN DỰA TRÊN DỮ LIỆU ĐÃ CHO, TUYỆT ĐỐI KHÔNG HỎI LẠI:");

            return sb.ToString().Trim();
        }
        #endregion
    }

    /// <summary>
    /// Giao diện mở đầu đoạn chat phong cách Google Gemini / GPT:
    /// - Quầng sáng Radiant Aura Glow huyền ảo
    /// - Biểu tượng ngôi sao 4 cánh Google Gemini lấp lánh đa sắc
    /// - Câu chào ngẫu nhiên truyền cảm hứng (AppL10n.T("Chúng ta nên bắt đầu từ đâu nhỉ?", "Where should we start?"), AppL10n.T("Bắt đầu làm việc thôi!", "Let's get to work!"), "Xin chào!...")
    /// - 4 gợi ý câu hỏi thông minh (Suggestion Chips) thích ứng theo tài liệu hiện tại (Calc, Slides, Writer)
    /// </summary>
    public class GeminiWelcomeControl : AiGpuControl
    {
        private Action<string> _onSelectPrompt;
        private OfficeAppMode _mode;
        private string _greetingText;
        private List<string> _chips = new List<string>();
        private List<Rectangle> _chipRects = new List<Rectangle>();
        private int _hoveredChipIndex = -1;
        private System.Windows.Forms.Timer _pulseTimer;
        private float _pulsePhase = 0f;

        private static readonly string[] _geminiHeadings = new string[]
        {
            AppL10n.T("Chúng ta nên bắt đầu từ đâu nhỉ?", "Where should we start?"),
            AppL10n.T("Bắt đầu làm việc thôi!", "Let's get to work!"),
            AppL10n.T("Xin chào! Bạn cứ hỏi tôi bất cứ điều gì...", "Hello! Ask me anything..."),
            AppL10n.T("Hôm nay bạn cần tôi hỗ trợ gì nào?", "How can I help today?"),
            AppL10n.T("Cùng tạo nên những điều tuyệt vời nhé!", "Let's build something awesome!")
        };

        public GeminiWelcomeControl(int width, OfficeAppMode mode, Action<string> onSelectPrompt)
        {
            _mode = mode;
            _onSelectPrompt = onSelectPrompt;
            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);

            Random rng = new Random();
            _greetingText = _geminiHeadings[rng.Next(_geminiHeadings.Length)];

            InitChipsForMode(mode);

            int totalH = CalculateLayout(width);
            this.Size = new Size(width, totalH);

            _pulseTimer = new System.Windows.Forms.Timer { Interval = 60 };
            _pulseTimer.Tick += (s, e) =>
            {
                _pulsePhase += 0.08f;
                if (_pulsePhase > (float)Math.PI * 2) _pulsePhase -= (float)Math.PI * 2;
                Invalidate();
            };
            _pulseTimer.Start();
        }

        private void InitChipsForMode(OfficeAppMode mode)
        {
            _chips.Clear();
            if (mode == OfficeAppMode.Calc)
            {
                _chips.Add(AppL10n.T("Tính tổng cột dữ liệu (=SUM)", "Sum this data column (=SUM)"));
                _chips.Add(AppL10n.T("Tô màu highlight A1:A10 vàng nhạt", "Highlight A1:A10 in light yellow"));
                _chips.Add(AppL10n.T("Tạo sheet mới và ghi bản tóm tắt", "Create a new sheet with summary"));
                _chips.Add(AppL10n.T("Tối ưu hóa bảng tính và công thức", "Optimize sheet & formulas"));
            }
            else if (mode == OfficeAppMode.Slides)
            {
                _chips.Add(AppL10n.T("Tạo slide trình chiếu theo mô tả", "Create presentation slides from prompt"));
                _chips.Add(AppL10n.T("Đổi màu nền gradient sang trọng", "Apply an elegant gradient background"));
                _chips.Add(AppL10n.T("Viết dàn ý và nội dung thuyết trình", "Draft slide outline & speaker content"));
                _chips.Add(AppL10n.T("Thêm các thẻ phân tích số liệu", "Add KPI & data insight cards"));
            }
            else if (mode == OfficeAppMode.Pdf)
            {
                _chips.Add(AppL10n.T("Tóm tắt nội dung tài liệu PDF này", "Summarize this PDF document"));
                _chips.Add(AppL10n.T("Tóm tắt trang hiện tại đang xem", "Summarize the current page"));
                _chips.Add(AppL10n.T("Trích xuất các luận điểm và từ khóa chính", "Extract key points & keywords"));
                _chips.Add(AppL10n.T("Dịch nội dung trang này sang tiếng Anh", "Translate this page"));
            }
            else
            {
                _chips.Add(AppL10n.T("Tóm tắt nội dung tài liệu này", "Summarize this document"));
                _chips.Add(AppL10n.T("Sửa lỗi chính tả & câu từ tự động", "Proofread & fix spelling/grammar"));
                _chips.Add(AppL10n.T("Viết tiếp đoạn văn theo ý tưởng", "Continue writing from this draft"));
                _chips.Add(AppL10n.T("Lập dàn ý bài viết chi tiết", "Create a detailed document outline"));
            }
        }

        private int CalculateLayout(int width)
        {
            _chipRects.Clear();
            int topH = 150;
            int padX = 14;
            int chipW = width - padX * 2;
            int chipH = 34;
            int gapY = 8;
            int curY = topH;

            for (int i = 0; i < _chips.Count; i++)
            {
                _chipRects.Add(new Rectangle(padX, curY, chipW, chipH));
                curY += chipH + gapY;
            }

            return curY + 16;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CalculateLayout(this.ClientSize.Width);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var aiFrame = AiGpuPaint.Begin(this, e);
            var g = aiFrame.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush bgB = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgB, this.ClientRectangle);
            }

            int w = this.ClientSize.Width;
            float cx = w / 2f;
            float cy = 52f;

            // 1. Quầng sáng Radiant Aura Glow huyền ảo (breathing light)
            float pulseScale = 1.0f + 0.08f * (float)Math.Sin(_pulsePhase);
            float glowR = 75f * pulseScale;
            RectangleF glowRect = new RectangleF(cx - glowR, cy - glowR, glowR * 2, glowR * 2);

            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(glowRect);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = Color.FromArgb(70, 224, 231, 255);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 248, 250, 252) };
                    g.FillPath(pgb, gp);
                }
            }

            // 2. Logo Trợ lý AI chính thức chuẩn Ultra-HD (icon/AIicon.png)
            Image aiIcon = AppIconRenderer.GetScaledAiBitmap(56);
            if (aiIcon != null)
            {
                var prevInterp = g.InterpolationMode;
                var prevSmooth = g.SmoothingMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(aiIcon, cx - 28f, cy - 28f, 56f, 56f);
                g.InterpolationMode = prevInterp;
                g.SmoothingMode = prevSmooth;
            }
            else
            {
                // Fallback nếu chưa tải được ảnh
                RectangleF fbRect = new RectangleF(cx - 24f, cy - 24f, 48f, 48f);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(fbRect);
                    using (LinearGradientBrush gb = new LinearGradientBrush(fbRect, Color.FromArgb(79, 70, 229), Color.FromArgb(147, 51, 234), 45f))
                    {
                        g.FillPath(gb, gp);
                    }
                }
                FluentIconRenderer.DrawGlyph(g, '\uE945', fbRect, Color.White, 22f);
            }

            // 3. Câu mở đầu ngẫu nhiên
            using (Font titleF = new Font("Segoe UI", 12.5f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                RectangleF titleR = new RectangleF(10, 84, w - 20, 52);
                g.DrawString(_greetingText, titleF, txtB, titleR, sf);
            }

            // 4. Các Suggestion Chips với Icon Vector chuẩn HD
            using (Font chipF = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (StringFormat csf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                for (int i = 0; i < _chips.Count; i++)
                {
                    Rectangle cr = _chipRects[i];
                    bool isHover = (i == _hoveredChipIndex);
                    RectangleF crF = new RectangleF(cr.X + 0.5f, cr.Y + 0.5f, cr.Width - 1f, cr.Height - 1f);

                    using (GraphicsPath cp = AiVectorGlassRenderer.CreateRoundedPath(crF, 10f))
                    using (SolidBrush chipBg = new SolidBrush(isHover ? Color.FromArgb(245, 243, 255) : Color.White))
                    using (Pen chipPen = new Pen(isHover ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), isHover ? 1.4f : 1.0f))
                    {
                        g.FillPath(chipBg, cp);
                        g.DrawPath(chipPen, cp);
                    }

                    int iconSize = 14;
                    int iconX = cr.X + 11;
                    int iconY = cr.Y + (cr.Height - iconSize) / 2;
                    DrawChipIcon(g, i, iconX, iconY, isHover);

                    Rectangle textR = new Rectangle(cr.X + 32, cr.Y, cr.Width - 42, cr.Height);
                    Color textColor = isHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(51, 65, 85);
                    using (SolidBrush tb = new SolidBrush(textColor))
                    {
                        g.DrawString(_chips[i], chipF, tb, textR, csf);
                    }
                }
            }
        }

        private void DrawChipIcon(D2DGraphics g, int chipIndex, int x, int y, bool isHover)
        {
            Color iconColor = isHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139);
            string glyph = "/sparkle";
            if (_mode == OfficeAppMode.Calc)
            {
                switch (chipIndex)
                {
                    case 0: glyph = "/chart"; break;      // \uE9D2 Bar chart
                    case 1: glyph = "/highlight"; break;  // \uE7E6 Highlight
                    case 2: glyph = "/sheet"; break;      // \uE9D9 Grid / Sheet
                    default: glyph = "/sparkle"; break;   // \uE945 Sparkle AI
                }
            }
            else if (_mode == OfficeAppMode.Slides)
            {
                switch (chipIndex)
                {
                    case 0: glyph = "/slide"; break;      // \uE7B5 Presentation
                    case 1: glyph = "/color"; break;      // \uE790 Palette
                    case 2: glyph = "/bulb"; break;       // \uEA80 Idea
                    default: glyph = "/chart"; break;     // \uE9D2 Analytics
                }
            }
            else if (_mode == OfficeAppMode.Pdf)
            {
                switch (chipIndex)
                {
                    case 0: glyph = "/doc"; break;        // Document summary
                    case 1: glyph = "/page"; break;       // Current page
                    case 2: glyph = "/list"; break;       // Key points
                    default: glyph = "/globe"; break;     // Translate
                }
            }
            else // Writer / General
            {
                switch (chipIndex)
                {
                    case 0: glyph = "/doc"; break;        // \uE8A5 Document
                    case 1: glyph = "/check"; break;      // \uE73E Checkmark / Grammar
                    case 2: glyph = "/bulb"; break;       // \uEA80 Idea
                    default: glyph = "/list"; break;      // \uE8FD Bullet List
                }
            }

            FluentIconRenderer.DrawGlyph(g, glyph, new RectangleF(x, y - 1, 16, 16), iconColor, 12.5f);
        }

        private GraphicsPath CreateRoundRectPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = _hoveredChipIndex;
            _hoveredChipIndex = -1;
            for (int i = 0; i < _chipRects.Count; i++)
            {
                if (_chipRects[i].Contains(e.Location))
                {
                    _hoveredChipIndex = i;
                    break;
                }
            }
            if (_hoveredChipIndex != prev)
            {
                this.Cursor = (_hoveredChipIndex >= 0) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredChipIndex != -1)
            {
                _hoveredChipIndex = -1;
                this.Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            for (int i = 0; i < _chipRects.Count; i++)
            {
                if (_chipRects[i].Contains(e.Location))
                {
                    string prompt = _chips[i].Trim();
                    if (_onSelectPrompt != null)
                    {
                        _onSelectPrompt(prompt);
                    }
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pulseTimer != null)
            {
                _pulseTimer.Stop();
                _pulseTimer.Dispose();
                _pulseTimer = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Control hiển thị hiệu ứng "AI đang suy nghĩ..." chuẩn Breathing Light 100% Khắc Phục Chớp Nháy / Flicker.
    /// Khá»­ sáº¡ch chá»›p nhÃ¡y báº±ng DoubleBuffer pháº§n cá»©ng vÃ  hiá»‡u á»©ng thá»Ÿ Ã¡nh sÃ¡ng sang trá» ng (Apple/Gemini style).
    /// </summary>
    public class ThinkingBubbleControl : AiGpuControl
    {
        private float _phase = 0f;
        private System.Windows.Forms.Timer _timer;
        private string _statusText = AppL10n.T("Đang suy nghĩ...", "Thinking...");
        private bool _isSearch = false;

        public ThinkingBubbleControl(string initialStatus = null, bool isSearch = false)
        {
            _statusText = initialStatus ?? AppL10n.T("Đang suy nghĩ...", "Thinking...");
            _isSearch = isSearch;
            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);
            UpdateBubbleWidth();

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 30; // ~33 FPS smooth wave
            _timer.Tick += (s, e) =>
            {
                _phase += 0.12f;
                if (_phase > 62.83f) _phase = 0f;
                this.Invalidate();
            };
            _timer.Start();
        }

        public void SetStatus(string statusText, bool isSearch = false)
        {
            _statusText = statusText ?? AppL10n.T("Đang suy nghĩ...", "Thinking...");
            _isSearch = isSearch;
            UpdateBubbleWidth();
            this.Invalidate();
        }

        private void UpdateBubbleWidth()
        {
            int textW = TextRenderer.MeasureText(_statusText, this.Font).Width;
            int totalW = Math.Max(140, 36 + textW + 36);
            this.Size = new Size(totalW, 34);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var aiFrame = AiGpuPaint.Begin(this, e);
            var g = aiFrame.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Nền bubble bo tròn mềm mại phong cách Google Gemini
            Rectangle r = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath gp = RoundRect(r, 16))
            {
                using (SolidBrush sb = new SolidBrush(Color.White))
                {
                    g.FillPath(sb, gp);
                }
                Color borderColor = _isSearch ? Color.FromArgb(191, 219, 254) : Color.FromArgb(226, 232, 240);
                using (Pen p = new Pen(borderColor, 1.2f))
                {
                    g.DrawPath(p, gp);
                }
            }

            // 2. Icon bên trái: Google Gemini Sparkle hoặc Search Icon
            float wave = (float)(Math.Sin(_phase) * 0.5 + 0.5);
            if (_isSearch)
            {
                int sAlpha = (int)(180 + wave * 75);
                FluentIconRenderer.DrawGlyph(g, "/search", new RectangleF(10, 9, 16, 16), Color.FromArgb(sAlpha, 37, 99, 235), 12f);
            }
            else
            {
                // Gradient Gemini màu Xanh biển - Tím
                int gR = (int)(66 + (147 - 66) * wave);
                int gG = (int)(133 + (51 - 133) * wave);
                int gB = (int)(244 + (234 - 244) * wave);
                Color geminiColor = Color.FromArgb(Math.Min(255, Math.Max(0, gR)), Math.Min(255, Math.Max(0, gG)), Math.Min(255, Math.Max(0, gB)));
                FluentIconRenderer.DrawGlyph(g, "/sparkle", new RectangleF(10, 9, 16, 16), geminiColor, 13f);
            }

            // 3. Văn bản trạng thái
            int textX = 32;
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.DrawString(_statusText, this.Font, textBrush, textX, 7);
            }

            // 4. Ba chấm nảy sóng sinh động (Jumping Dots Animation)
            int dotsStartX = textX + TextRenderer.MeasureText(_statusText, this.Font).Width + 6;
            for (int i = 0; i < 3; i++)
            {
                float dotWave = (float)Math.Sin(_phase * 1.5f - i * 0.8f);
                float dotY = 17f - Math.Max(0f, dotWave * 3.5f);
                int dotAlpha = (int)(120 + Math.Max(0f, dotWave) * 135);
                Color dotColor = _isSearch ? Color.FromArgb(dotAlpha, 37, 99, 235) : Color.FromArgb(dotAlpha, 124, 58, 237);
                using (SolidBrush dotBrush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(dotBrush, dotsStartX + i * 7, dotY, 4.5f, 4.5f);
                }
            }
        }

        private GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath gp = new GraphicsPath();
            int d = radius * 2;
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// ComboBox bo tròn phong cách Fluent Pill hiện đại (Windows 11 / Apple).
    /// Khắc phục hoàn toàn giao diện Win32 xám 3D cũ kỹ, mũi tên vector v khử răng cưa.
    /// </summary>
    public class ModernPillComboBox : AiGpuControl
    {
        private bool _isHovered = false;
        private bool _isPressed = false;
        private int _selectedIndex = -1;
        private readonly List<object> _items = new List<object>();

        public event EventHandler SelectedIndexChanged;

        public class ItemCollection : System.Collections.IList
        {
            private readonly ModernPillComboBox _owner;
            public ItemCollection(ModernPillComboBox owner) { _owner = owner; }
            public int Count => _owner._items.Count;
            public bool IsReadOnly => false;
            public bool IsFixedSize => false;
            public object SyncRoot => this;
            public bool IsSynchronized => false;

            public object this[int index]
            {
                get => _owner._items[index];
                set { _owner._items[index] = value; _owner.Invalidate(); }
            }

            public int Add(object item)
            {
                _owner._items.Add(item);
                if (_owner._selectedIndex < 0 && _owner._items.Count == 1)
                {
                    _owner._selectedIndex = 0;
                }
                _owner.Invalidate();
                return _owner._items.Count - 1;
            }

            public void Clear()
            {
                _owner._items.Clear();
                _owner._selectedIndex = -1;
                _owner.Invalidate();
            }

            public bool Contains(object value) => _owner._items.Contains(value);
            public int IndexOf(object value) => _owner._items.IndexOf(value);
            public void Insert(int index, object value) { _owner._items.Insert(index, value); _owner.Invalidate(); }
            public void Remove(object value) { _owner._items.Remove(value); _owner.Invalidate(); }
            public void RemoveAt(int index) { _owner._items.RemoveAt(index); _owner.Invalidate(); }
            public void CopyTo(Array array, int index) => ((System.Collections.ICollection)_owner._items).CopyTo(array, index);
            public System.Collections.IEnumerator GetEnumerator() => _owner._items.GetEnumerator();
        }

        private readonly ItemCollection _itemCollection;
        public ItemCollection Items => _itemCollection;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = (value >= 0 && value < _items.Count) ? value : -1;
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    Invalidate();
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public object SelectedItem
        {
            get => (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;
            set
            {
                int idx = _items.IndexOf(value);
                if (idx >= 0) SelectedIndex = idx;
            }
        }

        public ModernPillComboBox()
        {
            _itemCollection = new ItemCollection(this);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = false;
            Font = new Font("Segoe UI", 9f);
            Size = new Size(120, 26);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Do not paint default rectangular background to prevent gray corner artifacts
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovered = false; _isPressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
                ShowDropdownMenu();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _isPressed = false; Invalidate(); }

        private void ShowDropdownMenu()
        {
            if (_items.Count == 0) return;

            List<string> itemStrings = new List<string>();
            foreach (var it in _items) itemStrings.Add(it?.ToString() ?? "");

            Point screenPt = this.PointToScreen(new Point(0, this.Height + 4));
            GlassDropDownPopup popup = new GlassDropDownPopup(itemStrings, _selectedIndex, this.Font, Math.Max(this.Width, 160));
            popup.Location = screenPt;
            popup.ItemSelected += (s, ev) =>
            {
                SelectedIndex = ev.Index;
            };
            popup.FormClosed += (s, ev) =>
            {
                _isPressed = false;
                Invalidate();
            };
            popup.Show(this.FindForm());
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var aiFrame = AiGpuPaint.Begin(this, e);
            var g = aiFrame.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Fill background with parent color so 4 corners blend 100% seamlessly
            if (Parent != null)
            {
                using (SolidBrush pb = new SolidBrush(Parent.BackColor))
                {
                    g.FillRectangle(pb, ClientRectangle);
                }
            }

            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color fillBg = _isPressed
                ? Color.FromArgb(238, 242, 255)
                : (_isHovered ? Color.FromArgb(248, 250, 252) : Color.FromArgb(241, 245, 249));
            Color borderCol = _isHovered ? Color.FromArgb(147, 174, 204) : Color.FromArgb(218, 225, 233);

            using (GraphicsPath gp = AiVectorGlassRenderer.CreateRoundedPath(r, Height / 2f))
            {
                using (SolidBrush sb = new SolidBrush(fillBg))
                {
                    g.FillPath(sb, gp);
                }
                using (Pen p = new Pen(borderCol, _isHovered ? 1.4f : 1.0f))
                {
                    g.DrawPath(p, gp);
                }
            }

            // Text
            string currentText = (SelectedIndex >= 0 && SelectedIndex < _items.Count)
                ? _items[SelectedIndex]?.ToString() ?? ""
                : "";

            int textW = Math.Max(10, Width - 24);
            Rectangle textRect = new Rectangle(8, 0, textW, Height);
            AiGpuPaint.DrawText(g, currentText, Font, textRect, Color.FromArgb(30, 41, 59),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Sleek vector chevron down arrow
            float ax = Width - 12f;
            float ay = Height / 2f;
            using (Pen arrowPen = new Pen(_isHovered ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), 1.5f))
            {
                arrowPen.StartCap = LineCap.Round;
                arrowPen.EndCap = LineCap.Round;
                arrowPen.LineJoin = LineJoin.Round;
                g.DrawLine(arrowPen, ax - 3.5f, ay - 1.5f, ax, ay + 2f);
                g.DrawLine(arrowPen, ax, ay + 2f, ax + 3.5f, ay - 1.5f);
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            return AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(r.X, r.Y, r.Width, r.Height), radius);
        }
    }

    /// <summary>
    /// Feed cuộn chat AI với hiệu ứng cuộn quán tính nhẹ nhàng chuẩn thumbnail (Smooth Momentum Scrolling).
    /// Loại bỏ 100% thanh cuộn Win32 thô kệch, thanh cuộn mờ ảo auto-fade phong cách Apple/Fluent.
    /// </summary>
    public class VirtualChatFeedPanel : AiGpuPanel
    {
        private class ScrollFeedCanvasPanel : AiGpuPanel
        {
            private VirtualChatFeedPanel _owner;
            public ScrollFeedCanvasPanel(VirtualChatFeedPanel owner)
            {
                _owner = owner;
                this.DoubleBuffered = false;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            }
            protected override void WndProc(ref Message m)
            {
                const int WM_MOUSEWHEEL = 0x020A;
                if (m.Msg == WM_MOUSEWHEEL)
                {
                    short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                    _owner?.DoMouseWheel(delta);
                    m.Result = IntPtr.Zero;
                    return;
                }
                base.WndProc(ref m);
            }
        }

        private Panel _canvas;
        private float _targetScrollY = 0f;
        private float _smoothScrollY = 0f;
        private System.Windows.Forms.Timer _smoothTimer;
        private System.Windows.Forms.Timer _fadeTimer;
        private int _scrollbarAlpha = 0;
        private bool _isHoverScrollbar = false;
        private bool _isDraggingScrollbar = false;
        private int _dragStartMouseY = 0;
        private float _dragStartScrollY = 0f;
        private int _totalContentHeight = 0;

        public VirtualChatFeedPanel()
        {
            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.AutoScroll = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Padding = new Padding(0);

            _canvas = new ScrollFeedCanvasPanel(this)
            {
                BackColor = Color.Transparent,
                Location = new Point(0, 0),
                Width = Math.Max(100, this.ClientSize.Width - 8)
            };
            this.Controls.Add(_canvas);

            _canvas.MouseWheel += (s, e) => { if (e is HandledMouseEventArgs hme) hme.Handled = true; DoMouseWheel(e.Delta); };
            this.MouseWheel += (s, e) => { if (e is HandledMouseEventArgs hme) hme.Handled = true; DoMouseWheel(e.Delta); };

            _smoothTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            _smoothTimer.Tick += (s, e) =>
            {
                int maxScroll = GetMaxScrollY();
                _targetScrollY = Math.Max(0, Math.Min(maxScroll, _targetScrollY));
                float diff = _targetScrollY - _smoothScrollY;
                if (Math.Abs(diff) <= 1.0f)
                {
                    _smoothScrollY = _targetScrollY;
                    _canvas.Top = -(int)Math.Round(_smoothScrollY);
                    _smoothTimer.Stop();
                }
                else
                {
                    _smoothScrollY += diff * 0.55f; // Giảm quán tính, dừng gọn gàng không trôi tuột
                    _canvas.Top = -(int)Math.Round(_smoothScrollY);
                }
                if (_scrollbarAlpha > 0)
                {
                    this.Invalidate(new Rectangle(this.Width - 12, 0, 12, this.Height));
                }
            };

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _fadeTimer.Tick += (s, e) =>
            {
                if (!_isHoverScrollbar && !_isDraggingScrollbar)
                {
                    _scrollbarAlpha = 0;
                    this.Invalidate();
                }
                _fadeTimer.Stop();
            };
        }

        public int GetMaxScrollY()
        {
            return Math.Max(0, _totalContentHeight - this.ClientSize.Height);
        }

        public void DoMouseWheel(int delta)
        {
            if (_isDraggingScrollbar) return;
            int max = GetMaxScrollY();
            if (max <= 0) return;
            // Cuộn êm ái, nhẹ nhàng 44px / nấc chuột (chuẩn Windows UI, không nhảy vọt)
            float notches = (float)delta / 120f;
            float step = notches * 44f;
            _targetScrollY = Math.Max(0, Math.Min(max, _targetScrollY - step));
            WakeScrollbar();
            if (!_smoothTimer.Enabled) _smoothTimer.Start();
        }

        private void WakeScrollbar()
        {
            _scrollbarAlpha = 220;
            _fadeTimer.Stop();
            _fadeTimer.Start();
            this.Invalidate();
        }

        public void AddVirtualControl(Control c)
        {
            if (c == null) return;
            _canvas.Controls.Add(c);
            AttachMouseWheelRecursive(c);
            PerformVirtualLayout();
        }

        public void RemoveVirtualControl(Control c)
        {
            if (c == null) return;
            _canvas.Controls.Remove(c);
            PerformVirtualLayout();
        }

        public void ClearVirtualControls()
        {
            _canvas.SuspendLayout();
            for (int i = _canvas.Controls.Count - 1; i >= 0; i--)
            {
                Control c = _canvas.Controls[i];
                _canvas.Controls.RemoveAt(i);
                if (c != null && !c.IsDisposed) c.Dispose();
            }
            _canvas.ResumeLayout(true);
            _targetScrollY = 0;
            _smoothScrollY = 0;
            _canvas.Top = 0;
            PerformVirtualLayout();
        }

        private void AttachMouseWheelRecursive(Control c)
        {
            if (c == null) return;
            c.MouseWheel -= OnChildMouseWheel;
            c.MouseWheel += OnChildMouseWheel;
            foreach (Control sub in c.Controls)
            {
                AttachMouseWheelRecursive(sub);
            }
        }

        private void OnChildMouseWheel(object sender, MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs hme) hme.Handled = true;
            DoMouseWheel(e.Delta);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg == WM_MOUSEWHEEL)
            {
                short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                DoMouseWheel(delta);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        public void SuspendVirtualLayout()
        {
            _canvas.SuspendLayout();
        }

        public void ResumeVirtualLayout()
        {
            _canvas.ResumeLayout(true);
            PerformVirtualLayout();
        }

        public void ForceUpdateVisible()
        {
            PerformVirtualLayout();
            this.Invalidate();
        }

        public void ScrollToBottomForce()
        {
            PerformVirtualLayout();
            int max = GetMaxScrollY();
            _targetScrollY = max;
            _smoothScrollY = max;
            _canvas.Top = -(int)Math.Round(_smoothScrollY);
            WakeScrollbar();
            this.Invalidate();
        }

        public void ScrollToBottomIfNear()
        {
            int max = GetMaxScrollY();
            if (_targetScrollY >= max - 160)
            {
                _targetScrollY = max;
                WakeScrollbar();
                if (!_smoothTimer.Enabled) _smoothTimer.Start();
            }
        }

        public void PerformVirtualLayout()
        {
            if (_canvas == null) return;
            int curY = 6;
            int padX = 6;
            int availW = Math.Max(120, this.ClientSize.Width - padX * 2 - 8);

            for (int i = 0; i < _canvas.Controls.Count; i++)
            {
                Control c = _canvas.Controls[i];
                if (!c.Visible) continue;
                c.Location = new Point(padX, curY);
                if (c.Width != availW)
                {
                    c.Width = availW;
                }
                curY += c.Height + 8;
            }

            _totalContentHeight = curY + 12;
            _canvas.Size = new Size(this.ClientSize.Width - 8, Math.Max(this.ClientSize.Height, _totalContentHeight));
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            PerformVirtualLayout();
            _targetScrollY = Math.Max(0, Math.Min(GetMaxScrollY(), _targetScrollY));
            _smoothScrollY = _targetScrollY;
            _canvas.Top = -(int)Math.Round(_smoothScrollY);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int trackX = this.ClientSize.Width - 10;
            bool insideGutter = e.X >= trackX;
            if (insideGutter != _isHoverScrollbar)
            {
                _isHoverScrollbar = insideGutter;
                if (_isHoverScrollbar) WakeScrollbar();
                this.Invalidate();
            }

            if (_isDraggingScrollbar)
            {
                int maxScroll = GetMaxScrollY();
                if (maxScroll > 0)
                {
                    int trackH = Math.Max(1, this.ClientSize.Height - 16);
                    int thumbH = Math.Min(trackH, Math.Max(26, (int)(trackH * Math.Min(1f, (float)ClientSize.Height / Math.Max(1, _totalContentHeight)))));
                    int deltaY = e.Y - _dragStartMouseY;
                    float scrollDelta = ((float)deltaY / Math.Max(1, trackH - thumbH)) * maxScroll;
                    _targetScrollY = Math.Max(0, Math.Min(maxScroll, _dragStartScrollY + scrollDelta));
                    _smoothScrollY = _targetScrollY;
                    _canvas.Top = -(int)Math.Round(_smoothScrollY);
                    this.Invalidate();
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int trackX = this.ClientSize.Width - 10;
            if (e.X >= trackX && e.Button == MouseButtons.Left)
            {
                _smoothTimer.Stop();
                _isDraggingScrollbar = true;
                Capture = true;
                _dragStartMouseY = e.Y;
                _dragStartScrollY = _smoothScrollY;
                WakeScrollbar();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDraggingScrollbar)
            {
                _isDraggingScrollbar = false;
                Capture = false;
                _fadeTimer.Start();
                this.Invalidate();
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) _isDraggingScrollbar = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int maxScroll = GetMaxScrollY();
            if (maxScroll > 0 && _scrollbarAlpha > 0)
            {
                using var aiFrame = AiGpuPaint.Begin(this, e);
                var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int trackH = this.ClientSize.Height - 16;
                int trackY = 8;
                int trackX = this.ClientSize.Width - 7;
                int barW = _isHoverScrollbar ? 5 : 4;

                float viewRatio = (float)this.ClientSize.Height / Math.Max(1, _totalContentHeight);
                int thumbH = Math.Max(26, (int)(trackH * Math.Min(1.0f, viewRatio)));
                float scrollRatio = _smoothScrollY / maxScroll;
                int thumbY = (int)(trackY + scrollRatio * (trackH - thumbH));

                Color thumbCol = Color.FromArgb(_scrollbarAlpha, _isHoverScrollbar ? Color.FromArgb(79, 70, 229) : Color.FromArgb(148, 163, 184));
                Rectangle thumbR = new Rectangle(trackX - (barW - 4), thumbY, barW, thumbH);
                using (GraphicsPath gp = RoundRect(thumbR, barW / 2))
                using (SolidBrush sb = new SolidBrush(thumbCol))
                {
                    g.FillPath(sb, gp);
                }
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            return AiVectorGlassRenderer.CreateRoundedPath(new RectangleF(r.X, r.Y, r.Width, r.Height), radius);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_smoothTimer != null) { _smoothTimer.Stop(); _smoothTimer.Dispose(); _smoothTimer = null; }
                if (_fadeTimer != null) { _fadeTimer.Stop(); _fadeTimer.Dispose(); _fadeTimer = null; }
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Bubble tin nhắn người dùng căn bên PHẢI với gradient hiện đại và nút chip chỉnh sửa/sao chép.
    /// </summary>
    public class UserChatBubbleControl : AiGpuControl
    {
        private string _text;
        private int _messageIndex;
        private Action<string, int> _onEditAndResend;
        private int _bubbleW;
        private int _bubbleH;
        private List<string> _renderedLines = new List<string>();
        private Rectangle _replyBtnRect;
        private Rectangle _copyBtnRect;
        private bool _isReplyHover = false;
        private bool _isCopyHover = false;
        private bool _isCopied = false;
        private System.Windows.Forms.Timer _copyTimer;

        public UserChatBubbleControl(string text, int messageIndex, int feedW, Action<string, int> onEditAndResend)
        {
            _text = text ?? "";
            _messageIndex = messageIndex;
            _onEditAndResend = onEditAndResend;

            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);
            this.Margin = new Padding(0, 4, 0, 8);

            int maxBubbleW = Math.Max(120, (int)(feedW * 0.78f));
            int padX = 14;
            int padY = 8;

            _renderedLines = WordWrapText(_text, this.Font, maxBubbleW - padX * 2);
            int lineH = 20;
            int maxLineW = 0;
            foreach (var l in _renderedLines)
            {
                int lw = TextRenderer.MeasureText(l, this.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
                if (lw > maxLineW) maxLineW = lw;
            }

            _bubbleW = Math.Max(50, Math.Min(maxBubbleW, maxLineW + padX * 2));
            _bubbleH = Math.Max(34, _renderedLines.Count * lineH + padY * 2);

            int btnH = 22;
            int replyBtnW = 92;
            int copyBtnW = 82;
            int btnY = _bubbleH + 6;

            _copyBtnRect = new Rectangle(feedW - copyBtnW - 8, btnY, copyBtnW, btnH);
            _replyBtnRect = new Rectangle(feedW - copyBtnW - 8 - replyBtnW - 4, btnY, replyBtnW, btnH);

            this.Size = new Size(feedW, _bubbleH + 34);
        }

        private static List<string> WordWrapText(string text, Font font, int maxW)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;

            string[] paragraphs = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var para in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(para))
                {
                    lines.Add("");
                    continue;
                }
                string[] words = para.Split(' ');
                StringBuilder cur = new StringBuilder();
                foreach (var w in words)
                {
                    string test = cur.Length == 0 ? w : cur.ToString() + " " + w;
                    int testW = TextRenderer.MeasureText(test, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    if (testW > maxW && cur.Length > 0)
                    {
                        lines.Add(cur.ToString());
                        cur.Clear();
                        cur.Append(w);
                    }
                    else
                    {
                        if (cur.Length > 0) cur.Append(" ");
                        cur.Append(w);
                    }
                }
                if (cur.Length > 0) lines.Add(cur.ToString());
            }
            return lines;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var aiFrame = AiGpuPaint.Begin(this, e);
            var g = aiFrame.Graphics;
            using (SolidBrush bgB = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgB, this.ClientRectangle);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int bX = Math.Max(2, this.ClientSize.Width - _bubbleW - 6);

            // Bóng đổ nhẹ êm
            Rectangle shadowR = new Rectangle(bX, 3, _bubbleW, _bubbleH);
            using (GraphicsPath gpS = RoundRect(shadowR, 14f))
            using (SolidBrush sbS = new SolidBrush(Color.FromArgb(24, 79, 70, 229)))
            {
                g.FillPath(sbS, gpS);
            }

            // Nền bong bóng tím
            Rectangle r = new Rectangle(bX, 2, _bubbleW, _bubbleH);
            using (GraphicsPath gp = RoundRect(r, 14f))
            using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(79, 70, 229), Color.FromArgb(99, 102, 241), 135f))
            {
                g.FillPath(lgb, gp);
            }

            // Vẽ từng dòng chữ với khoảng cách dòng 1.35x thoáng đãng
            int textX = bX + 14;
            int curY = 2 + 8;
            int lineH = 20;
            using (SolidBrush txtBrush = new SolidBrush(Color.White))
            {
                foreach (var line in _renderedLines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        g.DrawString(line, this.Font, txtBrush, textX, curY);
                    }
                    curY += lineH;
                }
            }

            // Nút "✏️ Chỉnh sửa"
            using (GraphicsPath p = RoundRect(_replyBtnRect, 11f))
            using (SolidBrush sb = new SolidBrush(_isReplyHover ? Color.FromArgb(238, 242, 255) : Color.FromArgb(241, 245, 249)))
            {
                g.FillPath(sb, p);
            }
            FluentIconRenderer.DrawGlyph(g, "/edit", new RectangleF(_replyBtnRect.X + 6, _replyBtnRect.Y + 3, 14, 14), _isReplyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), 11f);
            using (Font bf = new Font("Segoe UI", 8.2f))
            {
                Rectangle textR = new Rectangle(_replyBtnRect.X + 22, _replyBtnRect.Y, _replyBtnRect.Width - 24, _replyBtnRect.Height);
                AiGpuPaint.DrawText(g, AppL10n.T("Chỉnh sửa", "Edit"), bf, textR, _isReplyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            // Nút "📋 Sao chép"
            using (GraphicsPath p = RoundRect(_copyBtnRect, 11f))
            using (SolidBrush sb = new SolidBrush(_isCopied ? Color.FromArgb(220, 252, 231) : (_isCopyHover ? Color.FromArgb(238, 242, 255) : Color.FromArgb(241, 245, 249))))
            {
                g.FillPath(sb, p);
            }

            if (_isCopied)
            {
                FluentIconRenderer.DrawGlyph(g, "/check", new RectangleF(_copyBtnRect.X + 6, _copyBtnRect.Y + 3, 14, 14), Color.FromArgb(22, 163, 74), 11f);
                using (Font bf = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 22, _copyBtnRect.Y, _copyBtnRect.Width - 24, _copyBtnRect.Height);
                    AiGpuPaint.DrawText(g, AppL10n.T("Đã chép", "Copied"), bf, textR, Color.FromArgb(22, 163, 74), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
            else
            {
                FluentIconRenderer.DrawGlyph(g, "/copy", new RectangleF(_copyBtnRect.X + 6, _copyBtnRect.Y + 3, 14, 14), _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), 11f);
                using (Font bf = new Font("Segoe UI", 8.2f))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 22, _copyBtnRect.Y, _copyBtnRect.Width - 24, _copyBtnRect.Height);
                    AiGpuPaint.DrawText(g, AppL10n.T("Sao chép", "Copy"), bf, textR, _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool repHover = _replyBtnRect.Contains(e.Location);
            bool copHover = _copyBtnRect.Contains(e.Location);

            if (repHover != _isReplyHover || copHover != _isCopyHover)
            {
                _isReplyHover = repHover;
                _isCopyHover = copHover;
                this.Cursor = (repHover || copHover) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_replyBtnRect.Contains(e.Location))
            {
                if (_onEditAndResend != null)
                {
                    _onEditAndResend(_text, _messageIndex);
                }
            }
            else if (_copyBtnRect.Contains(e.Location))
            {
                try
                {
                    Clipboard.SetText(_text);
                    _isCopied = true;
                    Invalidate();

                    if (_copyTimer == null)
                    {
                        _copyTimer = new System.Windows.Forms.Timer { Interval = 1600 };
                        _copyTimer.Tick += (s, ev) =>
                        {
                            _isCopied = false;
                            if (_copyTimer != null)
                            {
                                _copyTimer.Stop();
                                _copyTimer.Dispose();
                                _copyTimer = null;
                            }
                            Invalidate();
                        };
                    }
                    _copyTimer.Start();
                }
                catch { }
            }
        }

        private static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            return AiVectorGlassRenderer.CreateRoundedPath(r, radius);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _copyTimer != null)
            {
                _copyTimer.Stop();
                _copyTimer.Dispose();
                _copyTimer = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Control kết xuất phản hồi AI cao cấp:
    /// - Phân tích cú pháp Markdown: Tiêu đề in đậm, danh sách dấu chấm có thụt đầu dòng (hanging indent), nhãn in đậm.
    /// - Bóng đổ nổi Fluent/Apple Card tinh tế, bo góc r = 14px.
    /// - Nút hành động chip capsule với biểu tượng chuẩn /slide (không nhầm sang camera / video).
    /// </summary>
    public class AiChatBubbleControl : AiGpuControl
    {
        private class FormattedBlock
        {
            public bool IsHeading;
            public bool IsBullet;
            public string BulletChar = "•";
            public string BoldPrefix = "";
            public string BodyText = "";
            public List<string> Lines = new List<string>();
        }

        private string _text;
        private int _bubbleW;
        private int _bubbleH;
        private List<FormattedBlock> _blocks = new List<FormattedBlock>();
        private Rectangle _actionBtnRect;
        private Rectangle _copyBtnRect;
        private bool _isActionHover = false;
        private bool _isCopyHover = false;
        private bool _isCopied = false;
        private System.Windows.Forms.Timer _copyTimer;
        private Action<string> _onInsertAction;
        private string _actionText;
        private OfficeAppMode _mode;

        private AiCanvasQuizData _canvasQuiz;
        private Action _onOpenCanvasAction;
        private Action<AiCanvasQuizData> _onExportWriterAction;
        private Rectangle _btnWriterQuizRect = Rectangle.Empty;
        private Rectangle _btnOpenCanvasRect = Rectangle.Empty;
        private bool _isWriterHover = false;
        private bool _isOpenCanvasHover = false;

        public AiChatBubbleControl(string text, int feedW, OfficeAppMode mode, Action<string> onInsert, AppForm appForm = null, AiCanvasQuizData canvasQuiz = null, Action onOpenCanvas = null, Action<AiCanvasQuizData> onExportWriter = null)
        {
            _text = text ?? "";
            _mode = mode;
            _onInsertAction = onInsert;
            _canvasQuiz = canvasQuiz;
            _onOpenCanvasAction = onOpenCanvas;
            _onExportWriterAction = onExportWriter;

            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.4f);
            this.Margin = new Padding(0, 4, 0, 8);

            int maxBubbleW = Math.Max(140, (int)(feedW * 0.88f));

            ParseMarkdownContent(maxBubbleW);

            if (_canvasQuiz != null)
            {
                if (feedW >= 390)
                {
                    _btnWriterQuizRect = new Rectangle(8, _bubbleH + 6, 175, 24);
                    _btnOpenCanvasRect = new Rectangle(8 + 175 + 6, _bubbleH + 6, 95, 24);
                    _copyBtnRect = new Rectangle(8 + 175 + 6 + 95 + 6, _bubbleH + 6, 85, 24);
                    this.Size = new Size(feedW, _bubbleH + 36);
                }
                else
                {
                    int wBtnW = Math.Max(120, Math.Min(175, feedW - 115));
                    _btnWriterQuizRect = new Rectangle(8, _bubbleH + 6, wBtnW, 24);
                    _btnOpenCanvasRect = new Rectangle(8 + wBtnW + 6, _bubbleH + 6, 95, 24);
                    _copyBtnRect = new Rectangle(8, _bubbleH + 34, 85, 22);
                    this.Size = new Size(feedW, _bubbleH + 62);
                }
                _bubbleW = Math.Max(_bubbleW, Math.Min(feedW - 16, Math.Max(_btnOpenCanvasRect.Right + 8, 300)));
            }
            else
            {
                _actionText = (mode == OfficeAppMode.Slides) ? AppL10n.T("Chèn vào Slide", "Insert to Slide") : ((mode == OfficeAppMode.Calc) ? AppL10n.T("Chèn vào Bảng tính", "Insert to Sheet") : ((mode == OfficeAppMode.Pdf) ? "Sao chép phản hồi" : (appForm != null && appForm.WriterView != null && !string.IsNullOrEmpty(appForm.WriterView.GetSelectedText()) ? "Thay thế vùng chọn" : "Chèn vào văn bản")));
                _actionBtnRect = new Rectangle(8, _bubbleH + 6, 140, 24);
                _copyBtnRect = new Rectangle(8 + 140 + 6, _bubbleH + 6, 85, 24);
                this.Size = new Size(feedW, _bubbleH + 36);
            }
        }

        private void ParseMarkdownContent(int maxBubbleW)
        {
            _blocks.Clear();
            string[] rawLines = _text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var raw in rawLines)
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 1. Tiêu đề Markdown (# Header hoặc ### Header)
                if (line.StartsWith("#"))
                {
                    string hText = line.TrimStart('#', ' ').Trim();
                    _blocks.Add(new FormattedBlock { IsHeading = true, BodyText = hText });
                    continue;
                }

                // 2. Dòng danh sách bullet (•, -, *, 1., 2.)
                bool isBullet = line.StartsWith("•") || line.StartsWith("- ") || line.StartsWith("* ");
                string bNumPrefix = "";
                if (!isBullet)
                {
                    var matchNum = Regex.Match(line, @"^(\d+)[\.\)]\s+");
                    if (matchNum.Success)
                    {
                        isBullet = true;
                        bNumPrefix = matchNum.Groups[1].Value + ".";
                        line = line.Substring(matchNum.Length).Trim();
                    }
                }
                else
                {
                    line = line.TrimStart('•', '-', '*', ' ').Trim();
                }

                if (isBullet)
                {
                    FormattedBlock block = new FormattedBlock
                    {
                        IsBullet = true,
                        BulletChar = string.IsNullOrEmpty(bNumPrefix) ? "•" : bNumPrefix
                    };

                    // Kiểm tra xem có nhãn in đậm kiểu **Label:** Rest hoặc Label: Rest không
                    if (line.StartsWith("**"))
                    {
                        int closeIdx = line.IndexOf("**", 2);
                        if (closeIdx > 2)
                        {
                            block.BoldPrefix = line.Substring(2, closeIdx - 2).Trim();
                            block.BodyText = line.Substring(closeIdx + 2).Trim();
                        }
                        else
                        {
                            block.BodyText = line.Replace("**", "");
                        }
                    }
                    else
                    {
                        int colonIdx = line.IndexOf(':');
                        if (colonIdx > 0 && colonIdx < 35)
                        {
                            block.BoldPrefix = line.Substring(0, colonIdx + 1).Trim();
                            block.BodyText = line.Substring(colonIdx + 1).Trim();
                        }
                        else
                        {
                            block.BodyText = line;
                        }
                    }

                    _blocks.Add(block);
                    continue;
                }

                // 3. Đoạn văn bản thông thường
                string body = line.Replace("**", "").Trim();
                _blocks.Add(new FormattedBlock { BodyText = body });
            }

            // Word wrap và tính kích thước chiều cao
            int curContentH = 14;
            int maxMeasuredW = 0;
            using (Font headingFont = new Font("Segoe UI", 10.2f, FontStyle.Bold))
            using (Font boldFont = new Font("Segoe UI", 9.4f, FontStyle.Bold))
            using (Font normalFont = new Font("Segoe UI", 9.4f, FontStyle.Regular))
            {
                foreach (var block in _blocks)
                {
                    if (block.IsHeading)
                    {
                        block.Lines = WordWrap(block.BodyText, headingFont, maxBubbleW - 32);
                        curContentH += block.Lines.Count * 22 + 6;
                        foreach (var l in block.Lines)
                        {
                            int lw = TextRenderer.MeasureText(l, headingFont, Size.Empty, TextFormatFlags.NoPadding).Width;
                            if (lw > maxMeasuredW) maxMeasuredW = lw;
                        }
                    }
                    else if (block.IsBullet)
                    {
                        int availW = maxBubbleW - 46;
                        string fullTxt = string.IsNullOrEmpty(block.BoldPrefix)
                            ? block.BodyText
                            : (block.BoldPrefix + " " + block.BodyText);
                        block.Lines = WordWrap(fullTxt, normalFont, availW);
                        curContentH += block.Lines.Count * 20 + 5;
                        foreach (var l in block.Lines)
                        {
                            int lw = TextRenderer.MeasureText(l, normalFont, Size.Empty, TextFormatFlags.NoPadding).Width + 20;
                            if (lw > maxMeasuredW) maxMeasuredW = lw;
                        }
                    }
                    else
                    {
                        block.Lines = WordWrap(block.BodyText, normalFont, maxBubbleW - 32);
                        curContentH += block.Lines.Count * 20 + 6;
                        foreach (var l in block.Lines)
                        {
                            int lw = TextRenderer.MeasureText(l, normalFont, Size.Empty, TextFormatFlags.NoPadding).Width;
                            if (lw > maxMeasuredW) maxMeasuredW = lw;
                        }
                    }
                }
            }

            _bubbleH = Math.Max(38, curContentH + 12);
            _bubbleW = Math.Max(150, Math.Min(maxBubbleW, maxMeasuredW + 36));
        }

        private static List<string> WordWrap(string text, Font font, int maxW)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;

            string[] words = text.Split(' ');
            StringBuilder cur = new StringBuilder();

            foreach (var w in words)
            {
                string test = cur.Length == 0 ? w : cur.ToString() + " " + w;
                int testW = TextRenderer.MeasureText(test, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                if (testW > maxW && cur.Length > 0)
                {
                    lines.Add(cur.ToString());
                    cur.Clear();
                    cur.Append(w);
                }
                else
                {
                    if (cur.Length > 0) cur.Append(" ");
                    cur.Append(w);
                }
            }
            if (cur.Length > 0) lines.Add(cur.ToString());
            return lines;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var aiFrame = AiGpuPaint.Begin(this, e);
            var g = aiFrame.Graphics;
            using (SolidBrush bgB = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgB, this.ClientRectangle);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Hiệu ứng đổ bóng mượt mà Card Apple/Fluent 2
            Rectangle shadowR1 = new Rectangle(4, 4, _bubbleW, _bubbleH);
            using (GraphicsPath gpS1 = RoundRect(shadowR1, 14f))
            using (SolidBrush sbS1 = new SolidBrush(Color.FromArgb(8, 0, 0, 0)))
            {
                g.FillPath(sbS1, gpS1);
            }

            Rectangle shadowR2 = new Rectangle(4, 3, _bubbleW, _bubbleH);
            using (GraphicsPath gpS2 = RoundRect(shadowR2, 14f))
            using (SolidBrush sbS2 = new SolidBrush(Color.FromArgb(14, 0, 0, 0)))
            {
                g.FillPath(sbS2, gpS2);
            }

            // 2. Nền thẻ trắng tinh khiết với viền tinh tế
            Rectangle r = new Rectangle(4, 2, _bubbleW, _bubbleH);
            using (GraphicsPath gp = RoundRect(r, 14f))
            {
                using (SolidBrush sb = new SolidBrush(Color.White))
                {
                    g.FillPath(sb, gp);
                }
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.1f))
                {
                    g.DrawPath(p, gp);
                }
            }

            // 3. Kết xuất nội dung định dạng phong phú (Markdown & Hanging Indents)
            int curY = 2 + 12;
            int cardLeft = 4 + 14;

            using (Font headingFont = new Font("Segoe UI", 10.2f, FontStyle.Bold))
            using (Font boldFont = new Font("Segoe UI", 9.4f, FontStyle.Bold))
            using (Font normalFont = new Font("Segoe UI", 9.4f, FontStyle.Regular))
            using (SolidBrush headingBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            using (SolidBrush bulletDotBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
            {
                foreach (var block in _blocks)
                {
                    if (block.IsHeading)
                    {
                        foreach (var l in block.Lines)
                        {
                            g.DrawString(l, headingFont, headingBrush, cardLeft, curY);
                            curY += 22;
                        }
                        curY += 4;
                    }
                    else if (block.IsBullet)
                    {
                        // Vẽ chấm tròn bullet màu Indigo hoặc số
                        if (block.BulletChar == "•")
                        {
                            g.FillEllipse(bulletDotBrush, cardLeft + 2, curY + 7, 4.5f, 4.5f);
                        }
                        else
                        {
                            using (Font numFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
                            {
                                g.DrawString(block.BulletChar, numFont, bulletDotBrush, cardLeft, curY + 1);
                            }
                        }

                        int bulletTextX = cardLeft + 16;
                        bool isFirstLine = true;

                        foreach (var l in block.Lines)
                        {
                            if (isFirstLine && !string.IsNullOrEmpty(block.BoldPrefix))
                            {
                                // Vẽ phần tiêu đề in đậm trước
                                g.DrawString(block.BoldPrefix, boldFont, headingBrush, bulletTextX, curY);
                                int prefixW = TextRenderer.MeasureText(block.BoldPrefix + " ", boldFont, Size.Empty, TextFormatFlags.NoPadding).Width;

                                // Vẽ phần còn lại của dòng đầu
                                string restOfFirst = l.StartsWith(block.BoldPrefix) ? l.Substring(block.BoldPrefix.Length).TrimStart() : l;
                                if (!string.IsNullOrEmpty(restOfFirst))
                                {
                                    g.DrawString(restOfFirst, normalFont, textBrush, bulletTextX + prefixW, curY);
                                }
                                isFirstLine = false;
                            }
                            else
                            {
                                g.DrawString(l, normalFont, textBrush, bulletTextX, curY);
                            }
                            curY += 20;
                        }
                        curY += 4;
                    }
                    else
                    {
                        foreach (var l in block.Lines)
                        {
                            g.DrawString(l, normalFont, textBrush, cardLeft, curY);
                            curY += 20;
                        }
                        curY += 5;
                    }
                }
            }

            // 4. Các nút hành động dưới thẻ
            if (_canvasQuiz != null)
            {
                // Nút "📄 Tạo file sang Writer"
                Color wBtnBg = _isWriterHover ? Color.FromArgb(67, 56, 202) : Color.FromArgb(79, 70, 229);
                using (GraphicsPath wPath = RoundRect(_btnWriterQuizRect, 11f))
                using (SolidBrush wBg = new SolidBrush(wBtnBg))
                using (Font bFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    g.FillPath(wBg, wPath);
                    AiGpuPaint.DrawText(g, AppL10n.T("📄 Tạo file sang Writer", "📄 Export to Writer"), bFont, _btnWriterQuizRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Nút "🎨 Mở Canvas"
                Color cBtnBg = _isOpenCanvasHover ? Color.FromArgb(224, 231, 255) : Color.FromArgb(238, 242, 255);
                using (GraphicsPath cPath = RoundRect(_btnOpenCanvasRect, 11f))
                using (SolidBrush cBg = new SolidBrush(cBtnBg))
                using (Pen cPen = new Pen(Color.FromArgb(199, 210, 254), 1f))
                using (Font bFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    g.FillPath(cBg, cPath);
                    g.DrawPath(cPen, cPath);
                    AiGpuPaint.DrawText(g, AppL10n.T("🎨 Mở Canvas", "🎨 Open Canvas"), bFont, _btnOpenCanvasRect, Color.FromArgb(67, 56, 202), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // Nút Chèn vào Slide / Văn bản / Bảng tính (Dùng biểu tượng chuẩn /slide hoặc tương ứng, KHÔNG nhầm camera)
            if (_canvasQuiz == null)
            {
                using (GraphicsPath abp = RoundRect(_actionBtnRect, 11f))
                using (SolidBrush abb = new SolidBrush(_isActionHover ? Color.FromArgb(238, 242, 255) : Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(abb, abp);
                }

                // Biểu tượng chuẩn: Slide = /slide (), Calc = /sheet (), Pdf = /copy, Writer = /edit
                char actGlyph = (_mode == OfficeAppMode.Slides) ? '' : ((_mode == OfficeAppMode.Calc) ? '' : ((_mode == OfficeAppMode.Pdf) ? '' : ''));
                FluentIconRenderer.DrawGlyph(g, actGlyph, new RectangleF(_actionBtnRect.X + 6, _actionBtnRect.Y + 4, 15, 15), _isActionHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), 11.5f);
                using (Font abf = new Font("Segoe UI", 8.2f))
                {
                    Rectangle tr = new Rectangle(_actionBtnRect.X + 24, _actionBtnRect.Y, _actionBtnRect.Width - 26, _actionBtnRect.Height);
                    AiGpuPaint.DrawText(g, _actionText, abf, tr, _isActionHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(71, 85, 105), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }

            // Nút "📋 Sao chép"
            using (GraphicsPath p = RoundRect(_copyBtnRect, 11f))
            using (SolidBrush sb = new SolidBrush(_isCopied ? Color.FromArgb(220, 252, 231) : (_isCopyHover ? Color.FromArgb(238, 242, 255) : Color.FromArgb(241, 245, 249))))
            {
                g.FillPath(sb, p);
            }

            if (_isCopied)
            {
                FluentIconRenderer.DrawGlyph(g, "/check", new RectangleF(_copyBtnRect.X + 6, _copyBtnRect.Y + 4, 15, 15), Color.FromArgb(22, 163, 74), 11f);
                using (Font bf = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 24, _copyBtnRect.Y, _copyBtnRect.Width - 26, _copyBtnRect.Height);
                    AiGpuPaint.DrawText(g, AppL10n.T("Đã chép", "Copied"), bf, textR, Color.FromArgb(22, 163, 74), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
            else
            {
                FluentIconRenderer.DrawGlyph(g, "/copy", new RectangleF(_copyBtnRect.X + 6, _copyBtnRect.Y + 4, 15, 15), _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), 11f);
                using (Font bf = new Font("Segoe UI", 8.2f))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 24, _copyBtnRect.Y, _copyBtnRect.Width - 26, _copyBtnRect.Height);
                    AiGpuPaint.DrawText(g, AppL10n.T("Sao chép", "Copy"), bf, textR, _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(71, 85, 105), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool actHover = !_actionBtnRect.IsEmpty && _actionBtnRect.Contains(e.Location);
            bool copHover = !_copyBtnRect.IsEmpty && _copyBtnRect.Contains(e.Location);
            bool wHover = !_btnWriterQuizRect.IsEmpty && _btnWriterQuizRect.Contains(e.Location);
            bool cHover = !_btnOpenCanvasRect.IsEmpty && _btnOpenCanvasRect.Contains(e.Location);

            if (actHover != _isActionHover || copHover != _isCopyHover || wHover != _isWriterHover || cHover != _isOpenCanvasHover)
            {
                _isActionHover = actHover;
                _isCopyHover = copHover;
                _isWriterHover = wHover;
                _isOpenCanvasHover = cHover;
                this.Cursor = (actHover || copHover || wHover || cHover) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!_btnWriterQuizRect.IsEmpty && _btnWriterQuizRect.Contains(e.Location))
            {
                if (_onExportWriterAction != null && _canvasQuiz != null)
                {
                    _onExportWriterAction(_canvasQuiz);
                }
            }
            else if (!_btnOpenCanvasRect.IsEmpty && _btnOpenCanvasRect.Contains(e.Location))
            {
                if (_onOpenCanvasAction != null)
                {
                    _onOpenCanvasAction();
                }
            }
            else if (!_actionBtnRect.IsEmpty && _actionBtnRect.Contains(e.Location) && _onInsertAction != null)
            {
                _onInsertAction(_text);
            }
            else if (_copyBtnRect.Contains(e.Location))
            {
                try
                {
                    Clipboard.SetText(_text);
                    _isCopied = true;
                    Invalidate();

                    if (_copyTimer == null)
                    {
                        _copyTimer = new System.Windows.Forms.Timer { Interval = 1600 };
                        _copyTimer.Tick += (s, ev) =>
                        {
                            _isCopied = false;
                            if (_copyTimer != null)
                            {
                                _copyTimer.Stop();
                                _copyTimer.Dispose();
                                _copyTimer = null;
                            }
                            Invalidate();
                        };
                    }
                    _copyTimer.Start();
                }
                catch { }
            }
        }

        private static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            return AiVectorGlassRenderer.CreateRoundedPath(r, radius);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _copyTimer != null)
            {
                _copyTimer.Stop();
                _copyTimer.Dispose();
                _copyTimer = null;
            }
            base.Dispose(disposing);
        }
    }


    /// <summary>
    /// Nút chèn AI không chiếm Focus (Selectable = false) để ngăn ngừa WinForms giật thanh cuộn khi click.
    /// </summary>
    public class AiInsertActionButton : Button
    {
        public AiInsertActionButton()
        {
            this.SetStyle(ControlStyles.Selectable, false);
            this.TabStop = false;
        }

        protected override bool ShowFocusCues
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Äáº¡i diá»‡n cho má»™t phiÃªn há»™i thoáº¡i chat AI riÃªng biá»‡t.
    /// </summary>
    public class AiChatSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<AiChatMessage> Messages { get; set; }

        public AiChatSession()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = AppL10n.T("Đoạn chat mới", "New Chat");
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Messages = new List<AiChatMessage>();
        }
    }

    /// <summary>
    /// Hộp thoại xem và quản lý danh sách các đoạn hội thoại AI đã lưu.
    /// </summary>
    public class AiChatHistoryDialog : Form
    {
        public Action<AiChatSession> OnSelectSession;
        public Action<string> OnDeleteSession;
        public Action OnStartNewChat;

        private FlowLayoutPanel pnlList;
        private ToolTip toolTip;

        public AiChatHistoryDialog(Form parent)
        {
            this.Text = AppL10n.T("Lịch sử đoạn chat AI", "AI Chat History");
            this.Size = new Size(440, 560);
            this.MinimumSize = new Size(400, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.DoubleBuffered = false;

            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 3000;
            toolTip.InitialDelay = 400;

            // 1. Header
            Panel pnlHeader = new AiGpuPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Draw vector clock icon
                using (Pen cp = new Pen(Color.FromArgb(79, 70, 229), 1.8f))
                {
                    cp.StartCap = LineCap.Round;
                    cp.EndCap = LineCap.Round;
                    g.DrawEllipse(cp, 16, 17, 18, 18);
                    g.DrawLine(cp, 25, 21, 25, 26);
                    g.DrawLine(cp, 25, 26, 29, 26);
                }
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = AppL10n.T("Lịch sử hội thoại", "Chat History"),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(42, 14),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Button btnClearAll = new AiGpuButton
            {
                Text = AppL10n.T("      Xóa tất cả", "      Clear All"),
                Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(239, 68, 68),
                BackColor = Color.FromArgb(254, 242, 242),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 30),
                Location = new Point(pnlHeader.Width - 126, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnClearAll.FlatAppearance.BorderSize = 1;
            btnClearAll.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                FluentIconRenderer.DrawGlyph(g, "/trash", new RectangleF(8, 7, 16, 16), Color.FromArgb(239, 68, 68), 12f);
            };
            btnClearAll.Click += (s, e) =>
            {
                var res = ZenQOffice.UI.FluentMessageBox.Show(
                    AppL10n.T("Bạn có chắc chắn muốn xóa toàn bộ lịch sử các đoạn chat?", "Are you sure you want to delete all chat history?"),
                    AppL10n.T("Xác nhận xóa tất cả", "Confirm Clear All"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (res == DialogResult.Yes)
                {
                    AiChatHistoryManager.ClearAllSessions();
                    if (OnStartNewChat != null) OnStartNewChat();
                    ReloadSessionList();
                }
            };
            pnlHeader.Controls.Add(btnClearAll);

            // 2. Footer
            Panel pnlFooter = new AiGpuPanel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                var g = aiFrame.Graphics;
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawLine(p, 0, 0, pnlFooter.Width, 0);
                }
            };

            Button btnNew = new AiGpuButton
            {
                Text = AppL10n.T("+ Bắt đầu đoạn chat mới", "+ Start New Chat"),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(79, 70, 229),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += (s, e) =>
            {
                if (OnStartNewChat != null) OnStartNewChat();
                this.Close();
            };
            pnlFooter.Controls.Add(btnNew);

            // 3. Middle List
            pnlList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12, 12, 12, 12),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // DOCKING ORDER FIX:
            // Add pnlList first, then pnlHeader & pnlFooter, then BringToFront on pnlList
            this.Controls.Add(pnlList);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
            pnlList.BringToFront();

            ReloadSessionList();
        }

        private void ReloadSessionList()
        {
            pnlList.SuspendLayout();
            pnlList.Controls.Clear();

            var sessions = AiChatHistoryManager.LoadAllSessions();
            if (sessions == null || sessions.Count == 0)
            {
                Panel emptyCard = new AiGpuPanel
                {
                    Size = new Size(pnlList.ClientSize.Width > 40 ? pnlList.ClientSize.Width - 26 : 380, 160),
                    Margin = new Padding(0, 40, 0, 0),
                    BackColor = Color.Transparent
                };
                emptyCard.Paint += (s, e) =>
                {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                var g = aiFrame.Graphics;
                    FluentIconRenderer.DrawGlyph(g, '\uE8BD', new Rectangle((emptyCard.Width - 32) / 2, 16, 32, 32), Color.FromArgb(203, 213, 225), 26f);
                };
                Label lblEmpty = new Label
                {
                    Text = AppL10n.T("Chưa có đoạn chat nào trong lịch sử.\nHãy trò chuyện với AI để lưu lại các cuộc hội thoại!", "No saved chats yet.\nStart a conversation with AI to save your history!"),
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(0, 48, 0, 0),
                    Dock = DockStyle.Fill
                };
                emptyCard.Controls.Add(lblEmpty);
                pnlList.Controls.Add(emptyCard);
            }
            else
            {
                // Sắp xếp theo ngày cập nhật mới nhất trước
                sessions.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));

                int targetCardW = pnlList.ClientSize.Width > 60 ? (pnlList.ClientSize.Width - pnlList.Padding.Horizontal - 4) : 384;
                if (targetCardW > 396) targetCardW = 396;

                foreach (var sess in sessions)
                {
                    Panel card = CreateSessionCard(sess, targetCardW);
                    pnlList.Controls.Add(card);
                }
            }

            pnlList.ResumeLayout(true);
        }

        private Panel CreateSessionCard(AiChatSession sess, int cardW)
        {
            Panel card = new AiGpuPanel
            {
                Size = new Size(cardW, 70),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };

            bool isHover = false;

            Action setHoverState = () =>
            {
                isHover = true;
                card.BackColor = Color.FromArgb(241, 245, 249);
                card.Invalidate();
            };

            Action resetHoverState = () =>
            {
                Point pt = card.PointToClient(Cursor.Position);
                if (!card.ClientRectangle.Contains(pt))
                {
                    isHover = false;
                    card.BackColor = Color.White;
                    card.Invalidate();
                }
            };

            card.MouseEnter += (s, e) => setHoverState();
            card.MouseLeave += (s, e) => resetHoverState();

            card.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderCol = isHover ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240);
                using (Pen p = new Pen(borderCol, isHover ? 1.5f : 1f))
                using (GraphicsPath gp = RoundRect(new RectangleF(0, 0, card.Width - 1, card.Height - 1), 8f))
                {
                    g.DrawPath(p, gp);
                }

                // Left icon badge
                RectangleF badgeRect = new RectangleF(12, 16, 38, 38);
                using (GraphicsPath bp = RoundRect(badgeRect, 6f))
                using (SolidBrush bb = new SolidBrush(isHover ? Color.FromArgb(224, 231, 255) : Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(bb, bp);
                }
                
                // Draw vector chat icon inside badge
                using (Pen iconPen = new Pen(Color.FromArgb(79, 70, 229), 1.6f))
                {
                    iconPen.StartCap = LineCap.Round;
                    iconPen.EndCap = LineCap.Round;
                    iconPen.LineJoin = LineJoin.Round;
                    
                    float ix = 20, iy = 24;
                    g.DrawRectangle(iconPen, ix, iy, 22, 15);
                    g.DrawLine(iconPen, ix + 5, iy + 5, ix + 17, iy + 5);
                    g.DrawLine(iconPen, ix + 5, iy + 9, ix + 13, iy + 9);
                }
            };

            // Title
            Label lblCardTitle = new Label
            {
                Text = !string.IsNullOrEmpty(sess.Title) ? AppL10n.T(sess.Title == "Ä oáº¡n chat má»›i" ? "Đoạn chat mới" : sess.Title, (sess.Title == "Ä oáº¡n chat má»›i" || sess.Title == "Đoạn chat mới") ? "New Chat" : sess.Title) : AppL10n.T("Đoạn chat không tên", "Untitled Chat"),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(58, 13),
                Size = new Size(cardW - 110, 22),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            lblCardTitle.Click += (s, e) =>
            {
                if (OnSelectSession != null) OnSelectSession(sess);
                this.Close();
            };
            lblCardTitle.MouseEnter += (s, e) => setHoverState();
            lblCardTitle.MouseLeave += (s, e) => resetHoverState();
            card.Controls.Add(lblCardTitle);

            // Subtitle
            string timeStr = sess.UpdatedAt.ToString("HH:mm dd/MM/yyyy");
            int msgCount = (sess.Messages != null) ? sess.Messages.Count : 0;
            Label lblSub = new Label
            {
                Text = AppL10n.T(string.Format("{0} tin nhắn • {1}", msgCount, timeStr), string.Format("{0} message(s) • {1}", msgCount, timeStr)),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(58, 38),
                Size = new Size(cardW - 110, 18),
                Cursor = Cursors.Hand
            };
            lblSub.Click += (s, e) =>
            {
                if (OnSelectSession != null) OnSelectSession(sess);
                this.Close();
            };
            lblSub.MouseEnter += (s, e) => setHoverState();
            lblSub.MouseLeave += (s, e) => resetHoverState();
            card.Controls.Add(lblSub);

            // 🗑 Delete single session button (Clear, prominent, with hover animation & tooltip)
            Button btnDel = new AiGpuButton
            {
                Text = "",
                Size = new Size(34, 34),
                Location = new Point(cardW - 46, 17),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnDel.FlatAppearance.BorderSize = 0;
            toolTip.SetToolTip(btnDel, AppL10n.T("Xóa đoạn chat này", "Delete chat"));

            bool isDelHover = false;
            btnDel.MouseEnter += (s, e) => { isDelHover = true; btnDel.Invalidate(); };
            btnDel.MouseLeave += (s, e) => { isDelHover = false; btnDel.Invalidate(); };

            btnDel.Paint += (s, e) =>
            {
                using var aiFrame = AiGpuPaint.Begin((Control)s, e);
                    var g = aiFrame.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (isDelHover)
                {
                    using (GraphicsPath dp = RoundRect(new RectangleF(0, 0, btnDel.Width - 1, btnDel.Height - 1), 6f))
                    using (SolidBrush db = new SolidBrush(Color.FromArgb(254, 226, 226)))
                    using (Pen dpPen = new Pen(Color.FromArgb(254, 202, 202), 1f))
                    {
                        g.FillPath(db, dp);
                        g.DrawPath(dpPen, dp);
                    }
                }
                Color delCol = isDelHover ? Color.FromArgb(220, 38, 38) : Color.FromArgb(148, 163, 184);
                FluentIconRenderer.DrawGlyph(g, "/trash", new RectangleF(6, 6, 22, 22), delCol, 14f);
            };

            btnDel.Click += (s, e) =>
            {
                var res = ZenQOffice.UI.FluentMessageBox.Show(
                    AppL10n.T(string.Format("Bạn có chắc chắn muốn xóa đoạn chat \"{0}\"?", !string.IsNullOrEmpty(sess.Title) ? sess.Title : "Đoạn chat"), string.Format("Delete chat \"{0}\"?", !string.IsNullOrEmpty(sess.Title) ? sess.Title : "Chat")),
                    AppL10n.T("Xác nhận xóa đoạn chat", "Confirm Delete"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (res == DialogResult.Yes)
                {
                    AiChatHistoryManager.DeleteSession(sess.Id);
                    if (OnDeleteSession != null) OnDeleteSession(sess.Id);
                    ReloadSessionList();
                }
            };
            card.Controls.Add(btnDel);

            card.Click += (s, e) =>
            {
                if (OnSelectSession != null) OnSelectSession(sess);
                this.Close();
            };

            return card;
        }

        private static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            return AiVectorGlassRenderer.CreateRoundedPath(r, radius);
        }
    }

    /// <summary>
    /// LÆ°u trá»¯ ngá»¯ cáº£nh cá»§a má»™t tÃ¡c vá»¥ AI Ä‘ang sinh cÃ¢u tráº£ lá»i trong luá»“ng ná»n (Background Generation).
    /// </summary>
    public class ActiveAiGeneration
    {
        public string SessionId;
        public DocTab TargetTab;
        public StringBuilder FullResponseBuilder = new StringBuilder();
        public bool IsCompleted = false;
        public bool AbortRequested = false;
        public bool IsFirstChunk = true;
    }

    /// <summary>
    /// Quáº£n lÃ½ lÆ°u vÃ  táº£i lá»‹ch sá»­ chat AI bá»n vá»¯ng theo tá»«ng phiÃªn há»™i thoáº¡i (Persistent Storage).
    /// </summary>
    public static class AiChatHistoryManager
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GenOffice",
            "chat_history.json"
        );

        private static readonly string SessionsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GenOffice",
            "chat_sessions.json"
        );

        public static List<AiChatMessage> LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    string json = File.ReadAllText(HistoryFilePath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        object parsed = JsonLite.Parse(json);
                        if (parsed is List<object>)
                        {
                            var list = (List<object>)parsed;
                            List<AiChatMessage> messages = new List<AiChatMessage>();
                            foreach (var item in list)
                            {
                                if (item is Dictionary<string, object>)
                                {
                                    var dict = (Dictionary<string, object>)item;
                                    string role = dict.ContainsKey("role") ? dict["role"].ToString() : "user";
                                    string content = dict.ContainsKey("content") ? dict["content"].ToString() : "";
                                    string canvasQuizJson = dict.ContainsKey("canvasQuizJson") ? dict["canvasQuizJson"]?.ToString() : null;
                                    DateTime ts = DateTime.Now;
                                    if (dict.ContainsKey("timestamp"))
                                    {
                                        DateTime.TryParse(dict["timestamp"].ToString(), out ts);
                                    }
                                    messages.Add(new AiChatMessage { Role = role, Content = content, CanvasQuizJson = canvasQuizJson, Timestamp = ts });
                                }
                            }
                            return messages;
                        }
                    }
                }
            }
            catch { }
            return new List<AiChatMessage>();
        }

        public static void SaveHistory(List<AiChatMessage> messages)
        {
            try
            {
                string dir = Path.GetDirectoryName(HistoryFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");
                if (messages != null)
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        var m = messages[i];
                        sb.Append("  {");
                        sb.Append("\"role\": \"").Append(EscapeJson(m.Role)).Append("\", ");
                        sb.Append("\"content\": \"").Append(EscapeJson(m.Content)).Append("\", ");
                        if (!string.IsNullOrEmpty(m.CanvasQuizJson))
                        {
                            sb.Append("\"canvasQuizJson\": \"").Append(EscapeJson(m.CanvasQuizJson)).Append("\", ");
                        }
                        sb.Append("\"timestamp\": \"").Append(m.Timestamp.ToString("o")).Append("\"");
                        sb.Append("}");
                        if (i < messages.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine("]");
                File.WriteAllText(HistoryFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static List<AiChatSession> LoadAllSessions()
        {
            try
            {
                if (File.Exists(SessionsFilePath))
                {
                    string json = File.ReadAllText(SessionsFilePath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        object parsed = JsonLite.Parse(json);
                        if (parsed is List<object>)
                        {
                            var list = (List<object>)parsed;
                            List<AiChatSession> sessions = new List<AiChatSession>();
                            foreach (var sObj in list)
                            {
                                if (sObj is Dictionary<string, object>)
                                {
                                    var dict = (Dictionary<string, object>)sObj;
                                    AiChatSession session = new AiChatSession();
                                    if (dict.ContainsKey("id")) session.Id = dict["id"].ToString();
                                    if (dict.ContainsKey("title")) session.Title = dict["title"].ToString();
                                    if (dict.ContainsKey("createdAt"))
                                    {
                                        DateTime ca;
                                        if (DateTime.TryParse(dict["createdAt"].ToString(), out ca)) session.CreatedAt = ca;
                                    }
                                    if (dict.ContainsKey("updatedAt"))
                                    {
                                        DateTime ua;
                                        if (DateTime.TryParse(dict["updatedAt"].ToString(), out ua)) session.UpdatedAt = ua;
                                    }
                                    if (dict.ContainsKey("messages") && dict["messages"] is List<object>)
                                    {
                                        var msgList = (List<object>)dict["messages"];
                                        foreach (var mObj in msgList)
                                        {
                                            if (mObj is Dictionary<string, object>)
                                            {
                                                var mDict = (Dictionary<string, object>)mObj;
                                                string role = mDict.ContainsKey("role") ? mDict["role"].ToString() : "user";
                                                string content = mDict.ContainsKey("content") ? mDict["content"].ToString() : "";
                                                string canvasQuizJson = mDict.ContainsKey("canvasQuizJson") ? mDict["canvasQuizJson"]?.ToString() : null;
                                                DateTime ts = DateTime.Now;
                                                if (mDict.ContainsKey("timestamp"))
                                                {
                                                    DateTime.TryParse(mDict["timestamp"].ToString(), out ts);
                                                }
                                                session.Messages.Add(new AiChatMessage { Role = role, Content = content, CanvasQuizJson = canvasQuizJson, Timestamp = ts });
                                            }
                                        }
                                    }
                                    sessions.Add(session);
                                }
                            }
                            return sessions;
                        }
                    }
                }
                else
                {
                    // Migrate single history if available
                    var singleHistory = LoadHistory();
                    if (singleHistory != null && singleHistory.Count > 0)
                    {
                        AiChatSession firstSess = new AiChatSession
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Title = ExtractTitleFromMessages(singleHistory),
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Messages = singleHistory
                        };
                        List<AiChatSession> initial = new List<AiChatSession> { firstSess };
                        SaveAllSessions(initial);
                        return initial;
                    }
                }
            }
            catch { }
            return new List<AiChatSession>();
        }

        public static void SaveAllSessions(List<AiChatSession> sessions)
        {
            try
            {
                string dir = Path.GetDirectoryName(SessionsFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");
                if (sessions != null)
                {
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        var s = sessions[i];
                        sb.Append("  {");
                        sb.Append("\"id\": \"").Append(EscapeJson(s.Id)).Append("\", ");
                        sb.Append("\"title\": \"").Append(EscapeJson(s.Title)).Append("\", ");
                        sb.Append("\"createdAt\": \"").Append(s.CreatedAt.ToString("o")).Append("\", ");
                        sb.Append("\"updatedAt\": \"").Append(s.UpdatedAt.ToString("o")).Append("\", ");
                        sb.Append("\"messages\": [");

                        if (s.Messages != null)
                        {
                            for (int j = 0; j < s.Messages.Count; j++)
                            {
                                var m = s.Messages[j];
                                sb.Append("{");
                                sb.Append("\"role\": \"").Append(EscapeJson(m.Role)).Append("\", ");
                                sb.Append("\"content\": \"").Append(EscapeJson(m.Content)).Append("\", ");
                                if (!string.IsNullOrEmpty(m.CanvasQuizJson))
                                {
                                    sb.Append("\"canvasQuizJson\": \"").Append(EscapeJson(m.CanvasQuizJson)).Append("\", ");
                                }
                                sb.Append("\"timestamp\": \"").Append(m.Timestamp.ToString("o")).Append("\"");
                                sb.Append("}");
                                if (j < s.Messages.Count - 1) sb.Append(", ");
                            }
                        }
                        sb.Append("]}");
                        if (i < sessions.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine("]");
                File.WriteAllText(SessionsFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static void SaveCurrentSession(string sessionId, List<AiChatMessage> messages)
        {
            if (string.IsNullOrEmpty(sessionId) || messages == null || messages.Count == 0) return;

            try
            {
                var all = LoadAllSessions();
                AiChatSession target = null;
                foreach (var s in all)
                {
                    if (s.Id == sessionId)
                    {
                        target = s;
                        break;
                    }
                }

                if (target == null)
                {
                    target = new AiChatSession
                    {
                        Id = sessionId,
                        CreatedAt = DateTime.Now
                    };
                    all.Add(target);
                }

                target.Title = ExtractTitleFromMessages(messages);
                target.UpdatedAt = DateTime.Now;
                target.Messages = new List<AiChatMessage>(messages);

                SaveAllSessions(all);
                SaveHistory(messages);
            }
            catch { }
        }

        public static void DeleteSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;
            try
            {
                var all = LoadAllSessions();
                all.RemoveAll(s => s.Id == sessionId);
                SaveAllSessions(all);
            }
            catch { }
        }

        public static void ClearAllSessions()
        {
            try
            {
                SaveAllSessions(new List<AiChatSession>());
                SaveHistory(new List<AiChatMessage>());
            }
            catch { }
        }

        private static string ExtractTitleFromMessages(List<AiChatMessage> messages)
        {
            if (messages == null || messages.Count == 0) return AppL10n.T("Đoạn chat mới", "New Chat");
            foreach (var m in messages)
            {
                if (m.Role == "user" && !string.IsNullOrWhiteSpace(m.Content))
                {
                    string clean = m.Content.Trim().Replace("\r", " ").Replace("\n", " ");
                    if (clean.Length > 40) clean = clean.Substring(0, 38) + "...";
                    return clean;
                }
            }
            return AppL10n.T("Đoạn chat mới", "New Chat");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}

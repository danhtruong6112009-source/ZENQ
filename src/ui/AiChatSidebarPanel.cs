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
        private ComboBox cboKey;
        private ComboBox cboModel;
        private Button btnSettings;
        private Button btnHistory;
        private Button btnNewChat;
        private Button btnClose;

        private string _currentSessionId = Guid.NewGuid().ToString("N");
        private static readonly Dictionary<string, ActiveAiGeneration> _activeGenerations = new Dictionary<string, ActiveAiGeneration>();

        private VirtualChatFeedPanel pnlChatFeed;
        private Panel pnlInputContainer;
        private Panel pnlInputPill;
        private TextBox txtInput;

        // Thinking animation
        private Control _thinkingBubble;

        private Rectangle _sendBtnRect;
        private bool _isSendHover = false;
        private bool _isSendPressed = false;

        private bool _isBinding = false;
        private string _placeholderText = "Hỏi AI hoặc nhập yêu cầu... (Enter)";
        private bool _isPlaceholderActive = true;

                private static readonly string[] _greetings = new string[]
        {
            "✦ Xin chào! Tôi là Trợ lý AI — hãy cho tôi biết bạn cần gì nhé!",
            "✦ Chào bạn! Tôi sẵn sàng hỗ trợ — hãy gõ yêu cầu của bạn!",
            "✦ Hi! Bạn cần thiết kế slide, viết văn bản hay phân tích dữ liệu?",
            "✦ Xin chào! Hãy mô tả yêu cầu và tôi sẽ thực hiện ngay!",
            "✦ Chào! Tôi có thể giúp gì cho bạn hôm nay?"
        };

        public AiChatSidebarPanel(AppForm appForm)
        {
            _appForm = appForm;
            _config = AiConfig.Load();
            _currentProfile = _config.GetActiveProfile();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.Size = new Size(360, 560);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);

            InitializeComponents();
            ReloadProfilesAndModels();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();
            this.Controls.Clear();

            // ==========================================
            // 1. HEADER PANEL
            // ==========================================
            pnlHeader = new Panel
            {
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 6)
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            lblTitle = new Label
            {
                Text = "✦ Trợ lý AI",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(12, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            btnSettings = new Button
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
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnSettings.PointToClient(Cursor.Position);
                Color col = btnSettings.ClientRectangle.Contains(pt)
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(100, 116, 139);
                AiVectorGlassRenderer.DrawVectorCog(e.Graphics, 3, 3, 20, col, 1.8f);
            };
            btnSettings.Click += (s, e) =>
            {
                if (_appForm != null) _appForm.ShowAiConfig();
            };
            pnlHeader.Controls.Add(btnSettings);

            btnHistory = new Button
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
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnHistory.PointToClient(Cursor.Position);
                Color col = btnHistory.ClientRectangle.Contains(pt)
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(100, 116, 139);
                AiVectorGlassRenderer.DrawVectorHistory(e.Graphics, 3, 3, 20, col, 1.8f);
            };
            btnHistory.Click += (s, e) => ShowHistoryDialog();
            pnlHeader.Controls.Add(btnHistory);

            btnNewChat = new Button
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
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Point pt = btnNewChat.PointToClient(Cursor.Position);
                Color col = btnNewChat.ClientRectangle.Contains(pt)
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(100, 116, 139);
                AiVectorGlassRenderer.DrawVectorPlus(e.Graphics, 3, 3, 20, col, 2.0f);
            };
            btnNewChat.Click += (s, e) => StartNewChatSession();
            pnlHeader.Controls.Add(btnNewChat);

            btnClose = new Button
            {
                Text = "✷",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Size = new Size(26, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) =>
            {
                if (_currentTab != null) _currentTab.IsAiChatOpen = false;
                this.Visible = false;
            };
            pnlHeader.Controls.Add(btnClose);

            cboKey = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat
            };
            cboKey.SelectedIndexChanged += (s, e) => OnKeySelectionChanged();
            pnlHeader.Controls.Add(cboKey);

            cboModel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat
            };
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
            pnlInputContainer = new Panel
            {
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlInputContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlInputContainer.Width, 0);
                }
            };

            pnlInputPill = new Panel
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
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    txtInput.Text = _placeholderText;
                    txtInput.ForeColor = Color.FromArgb(148, 163, 184);
                    _isPlaceholderActive = true;
                }
                pnlInputPill.Invalidate();
            };
            txtInput.KeyDown += (s, e) =>
            {
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

        /// <summary>
        /// Vẽ input pill capsule + nút gửi tròn vector KHÔNG răng cưa.
        /// Mũi tên gửi vẽ bằng FillPolygon (vector polygon) + SmoothingMode.AntiAlias.
        /// </summary>
        private void PaintInputPill(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // 1. Vẽ Capsule Box
            Rectangle r = new Rectangle(0, 0, pnlInputPill.Width - 1, pnlInputPill.Height - 1);
            using (GraphicsPath gp = RoundRect(r, 18))
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

            // 3. Vẽ biểu tượng Vector (Send hoặc Stop) trong nút bấm
            if (_isAiStreaming)
            {
                // Hình vuông dừng (Stop Square)
                float squareSize = 10f;
                float squareX = _sendBtnRect.X + (_sendBtnRect.Width - squareSize) / 2f;
                float squareY = _sendBtnRect.Y + (_sendBtnRect.Height - squareSize) / 2f;
                using (SolidBrush stopBrush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(stopBrush, squareX, squareY, squareSize, squareSize);
                }
            }
            else
            {
                float iconSize = 15f;
                float iconX = _sendBtnRect.X + (_sendBtnRect.Width - iconSize) / 2f - 0.5f;
                float iconY = _sendBtnRect.Y + (_sendBtnRect.Height - iconSize) / 2f + 0.5f;
                AiVectorGlassRenderer.DrawVectorLucideSend(g, iconX, iconY, iconSize, Color.White, 1.8f);
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
        }

        private void PerformCustomLayout()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            int headerH = 80;
            int inputH = 54;
            int feedH = Math.Max(50, h - headerH - inputH);

            if (pnlHeader != null)
            {
                pnlHeader.Bounds = new Rectangle(0, 0, w, headerH);
                if (cboKey != null) cboKey.Bounds = new Rectangle(12, 44, 120, 26);
                if (cboModel != null) cboModel.Bounds = new Rectangle(138, 44, Math.Max(80, w - 150), 26);
                if (btnSettings != null) btnSettings.Location = new Point(w - 116, 8);
                if (btnHistory != null) btnHistory.Location = new Point(w - 88, 8);
                if (btnNewChat != null) btnNewChat.Location = new Point(w - 60, 8);
                if (btnClose != null) btnClose.Location = new Point(w - 32, 8);
            }

            if (pnlChatFeed != null)
            {
                pnlChatFeed.Bounds = new Rectangle(0, headerH, w, feedH);
            }

            if (pnlInputContainer != null)
            {
                pnlInputContainer.Bounds = new Rectangle(0, headerH + feedH, w, inputH);
                if (pnlInputPill != null)
                {
                    pnlInputPill.Bounds = new Rectangle(10, 7, w - 20, 38);
                    _sendBtnRect = new Rectangle(pnlInputPill.Width - 33, 5, 28, 28);
                    if (txtInput != null) txtInput.Bounds = new Rectangle(12, 10, Math.Max(50, pnlInputPill.Width - 52), 20);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
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
                string displayName = !string.IsNullOrEmpty(p.Name) ? p.Name : "(Chưa đặt tên)";
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
                    if (!string.IsNullOrEmpty(m) && !m.StartsWith("⏳") && !m.StartsWith("⚠️") && !models.Contains(m))
                    {
                        models.Add(m);
                    }
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
                int step = (delta > 0) ? -80 : 80;
                int currentY = -pnlChatFeed.AutoScrollPosition.Y;
                int newY = Math.Max(0, currentY + step);
                pnlChatFeed.AutoScrollPosition = new Point(0, newY);
                pnlChatFeed.ForceUpdateVisible();
                pnlChatFeed.Invalidate();
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

                if (tab.AiChatHistory == null || tab.AiChatHistory.Count == 0)
                {
                    var persistent = AiChatHistoryManager.LoadHistory();
                    if (persistent != null && persistent.Count > 0)
                    {
                        tab.AiChatHistory = new List<AiChatMessage>(persistent);
                    }
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
                        AddAiBubble(msg.Content);
                    }
                }
                pnlChatFeed.ResumeVirtualLayout();
            }
            else
            {
                // Hiện tin nhắn chào random khi mở tab mới (chưa có lịch sử)
                AddWelcomeGreeting();
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
        /// Hiện 1 tin nhắn chào mừng random (khác nhau mỗi lần).
        /// </summary>
        private void AddWelcomeGreeting()
        {
            Random rng = new Random();
            string greeting = _greetings[rng.Next(_greetings.Length)];
            AddAiBubble(greeting);
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
            AddWelcomeGreeting();
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

            _streamingAiBubble = new Panel
            {
                Size = new Size(bubbleW, bubbleH),
                Location = new Point(2, 0),
                BackColor = Color.Transparent
            };

            // Bật DoubleBuffering bằng Reflection để khử triệt để hiện tượng chớp nháy (flicker) khi streaming liên tục
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, _streamingAiBubble, new object[] { true });

            _streamingAiBubble.Paint += (s, e) =>
            {
                Control c = s as Control;
                if (c == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, c.Width - 1, c.Height - 1);
                using (GraphicsPath gp = RoundRect(r, 14))
                {
                    using (SolidBrush sb = new SolidBrush(Color.White))
                    {
                        e.Graphics.FillPath(sb, gp);
                    }
                    using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
                    {
                        e.Graphics.DrawPath(p, gp);
                    }
                }
            };

            _streamingAiLabel = new Label
            {
                Text = text,
                Font = font,
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.Transparent,
                Bounds = new Rectangle(padX, padY, bubbleW - padX * 2, bubbleH - padY * 2)
            };
            _streamingAiBubble.Controls.Add(_streamingAiLabel);

            _streamingAiRow = new Panel
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
            clean = Regex.Replace(clean, @"\[(?:QUY TẮC|HƯỚNG DẪN)[^\]]*\][\s\S]*?\[/GENOFFICE_ACTION\]", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[OFFICE_AGENT_[^\]]*\]", "", RegexOptions.IgnoreCase);

            // Strip ALL action block variants (complete or streaming-incomplete)
            // Handles [GENOFFICE_ACTION:...], [TAG:GENOFFICE_ACTION:...], [HGENOFFICE_ACTION:...], etc.
            clean = Regex.Replace(clean, @"\[[A-Za-z0-9_]*(?:ACTION|GENOFFICE)[A-Za-z0-9_:]*\][\s\S]*?(?:\[/[A-Za-z0-9_]*(?:ACTION|GENOFFICE)[A-Za-z0-9_]*\])", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[[A-Za-z0-9_]*(?:ACTION|GENOFFICE)[A-Za-z0-9_:]*\][\s\S]*", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\[/?[A-Za-z0-9_]*(?:ACTION|GENOFFICE)[A-Za-z0-9_]*\]", "", RegexOptions.IgnoreCase);

            // Strip JSON blocks with spell corrections, calc data (complete and streaming-incomplete)
            clean = Regex.Replace(clean, @"\{[\s\S]*?""corrections""[\s\S]*?\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""corrections""[\s\S]*", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""original""[\s\S]*?""replacement""[\s\S]*?\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\S]*?""original""[\s\S]*", "", RegexOptions.IgnoreCase);

            // Strip markdown code fences containing JSON
            clean = Regex.Replace(clean, @"```(?:json)?\s*\{[\s\S]*?\}\s*```", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"```(?:json)?\s*\{[\s\S]*", "", RegexOptions.IgnoreCase);

            // Strip raw JSON canvas structure if AI printed it directly
            clean = Regex.Replace(clean, @"\{[\s\r\n]*""(?:theme|style|background|elements|type)""[\s\S]*\}", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\{[\s\r\n]*""(?:theme|style|background|elements|type)""[\s\S]*", "", RegexOptions.IgnoreCase);

            // Strip raw tags/labels like [CAPTION], [SUBCAPTION], [Slide 1], [Shape #1...]
            clean = Regex.Replace(clean, @"\[(?:CAPTION|SUBCAPTION|Slide \d+|Shape #\d+[^\]]*)\]", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\*\*\[Shape #\d+[^\]]*\]\*\*", "", RegexOptions.IgnoreCase);

            // Strip markdown symbols for chat readability
            clean = Regex.Replace(clean, @"^#{1,6}\s*", "", RegexOptions.Multiline); // headers #, ##, ###
            clean = clean.Replace("**", ""); // bold
            clean = clean.Replace("__", "");
            clean = clean.Replace("```json", "").Replace("```", ""); // code fences
            clean = Regex.Replace(clean, @"^\s*[\*\-]\s+", "• ", RegexOptions.Multiline); // clean bullet points

            // Normalize multiple blank lines
            clean = Regex.Replace(clean, @"\n{3,}", "\n\n");

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
                    displayText = "📊 Đang xử lý và ghi dữ liệu bảng tính...";
                else if (curMode == OfficeAppMode.Writer)
                    displayText = "📝 Đang soạn thảo văn bản...";
                else
                    displayText = "🎨 Đang thiết kế slide cho bạn...";
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
            _streamingAiLabel.Text = displayText;

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
                string actionBtnText = (curMode == OfficeAppMode.Slides) ? "🎨 Chèn vào Slide" : ((curMode == OfficeAppMode.Calc) ? "📊 Chèn vào Bảng tính" : (_appForm != null && _appForm.WriterView != null && !string.IsNullOrEmpty(_appForm.WriterView.GetSelectedText()) ? "📝 Thay thế vùng quét khối" : "📝 Chèn vào văn bản"));

                AiInsertActionButton btnAction = new AiInsertActionButton
                {
                    Text = actionBtnText,
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(136, 24),
                    Location = new Point(14, _streamingAiBubble.Height + 4),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft,
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
                    } catch (Exception ex) {
                        MessageBox.Show(ex.ToString(), "ZENQ AI - Lỗi Chèn Text");
                    }
                };
                
                var detectedCorrs = AiActionEngine.ExtractSpellCorrections(textToInsert);
                if (detectedCorrs != null && detectedCorrs.Count > 0)
                {
                    AiInsertActionButton btnConfirm = new AiInsertActionButton
                    {
                        Text = string.Format("✅ Xác nhận sửa {0} lỗi", detectedCorrs.Count),
                        Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(22, 163, 74),
                        BackColor = Color.FromArgb(240, 253, 244),
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(130, 24),
                        Location = new Point(14, _streamingAiBubble.Height + 4),
                        Cursor = Cursors.Hand,
                        TextAlign = ContentAlignment.MiddleCenter,
                    };
                    btnConfirm.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
                    btnConfirm.FlatAppearance.BorderSize = 1;
                    btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 252, 231);

                    AiInsertActionButton btnCancel = new AiInsertActionButton
                    {
                        Text = "❌ Bỏ qua",
                        Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(220, 38, 38),
                        BackColor = Color.FromArgb(254, 242, 242),
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(80, 24),
                        Location = new Point(148, _streamingAiBubble.Height + 4),
                        Cursor = Cursors.Hand,
                        TextAlign = ContentAlignment.MiddleCenter,
                    };
                    btnCancel.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
                    btnCancel.FlatAppearance.BorderSize = 1;
                    btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);

                    btnConfirm.Click += (s, e) =>
                    {
                        if (_appForm != null && _appForm.WriterView != null)
                        {
                            _appForm.WriterView.ApplyPendingAiCorrections();
                            btnConfirm.Text = "✅ Đã sửa!";
                            btnConfirm.Enabled = false;
                            btnCancel.Visible = false;
                        }
                    };
                    btnCancel.Click += (s, e) =>
                    {
                        if (_appForm != null && _appForm.WriterView != null)
                        {
                            _appForm.WriterView.CancelPendingAiCorrections();
                            btnCancel.Text = "Đã bỏ qua";
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

        private void SendMessageFromInput()
        {
            string query = txtInput.Text != null ? txtInput.Text.Trim() : "";
            if (string.IsNullOrEmpty(query) || query == _placeholderText) return;
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

            if (_currentProfile == null)
            {
                string noConfigMsg = "⚠️ Chưa có cấu hình AI nào. Vui lòng vào Cài đặt để thiết lập API Key hoặc Ollama trước khi sử dụng.";
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

            // Hiá»‡n animation "Ä ang suy nghÄ©..."
            ShowThinkingBubble();

            string promptToSend = BuildPromptWithDocumentContext(query);

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
            System.Windows.Forms.Timer streamUiTimer = new System.Windows.Forms.Timer { Interval = 45 };
            streamUiTimer.Tick += (s, e) =>
            {
                string chunkToUpdate = "";
                lock (_streamLock)
                {
                    if (string.IsNullOrEmpty(_pendingStreamChunk)) return;
                    chunkToUpdate = _pendingStreamChunk;
                    _pendingStreamChunk = "";
                }

                // Chá»‰ cáº­p nháº­t UI náº¿u Tab hiá»‡n táº¡i trÃ¹ng vá»›i Tab Ä‘ang táº¡o cÃ¢u tráº£ lá» i vÃ  panel Ä‘ang hiá»ƒn thá»‹
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

            AiService.SendPromptStreamAsync(_config, promptToSend, (chunk) =>
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
                    finalSummaryMsg = !string.IsNullOrWhiteSpace(response.ErrorMessage) ? response.ErrorMessage : "❌ Không nhận được phản hồi hợp lệ từ máy chủ AI.";
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
                    finalSummaryMsg = "✦ Đã xử lý xong yêu cầu của bạn.";
                }

                // Báº¢O Ä áº¢M LÆ¯U Bá»€N Vá»®NG VÃ€O TAB & Lá»ŠCH Sá»¬ Ká»‚ Cáº¢ KHI PANEL Ä Ãƒ Ä Ã“NG HOáº¶C Ä Ãƒ CHUYá»‚N TAB
                var modelMsg = new AiChatMessage { Role = "model", Content = finalSummaryMsg, Timestamp = DateTime.Now };
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
                                    if (gen.IsFirstChunk)
                                    {
                                        HideThinkingBubble();
                                        AddAiBubble(finalSummaryMsg);
                                    }
                                    else
                                    {
                                        if (_streamingAiLabel != null)
                                        {
                                            _streamingAiLabel.Text = finalSummaryMsg;
                                        }
                                        FinishAiStreamingBubble();
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
            }, targetTab != null ? targetTab.AiChatHistory : null, () => (gen.AbortRequested || _abortAiStream));
        }

        /// <summary>
        /// Hiện animation "AI đang suy nghĩ..." chuẩn Breathing Light 100% KHÔNG giật/flicker.
        /// </summary>
        private void ShowThinkingBubble()
        {
            HideThinkingBubble();

            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;

            ThinkingBubbleControl bubble = new ThinkingBubbleControl();
            bubble.Location = new Point(2, 0);

            Panel row = new Panel
            {
                Width = feedW,
                Height = 32,
                BackColor = Color.Transparent,
                Margin = new Padding(2, 3, 2, 5)
            };
            row.Controls.Add(bubble);

            _thinkingBubble = row;
            pnlChatFeed.AddVirtualControl(row);
            ScrollToBottomForce();
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
        private void AddAiBubble(string text)
        {
            int feedW = pnlChatFeed.ClientSize.Width - 46;
            if (feedW <= 100) feedW = 320;

            string displayText = CleanAiOutputForChat(text);
            if (string.IsNullOrEmpty(displayText))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.IndexOf("CALC", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("cells", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("spreadsheet", StringComparison.OrdinalIgnoreCase) >= 0)
                        displayText = "📊 Đã cập nhật và ghi dữ liệu thành công vào Bảng tính.";
                    else if (text.IndexOf("GENERATE_SLIDE", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("slides", StringComparison.OrdinalIgnoreCase) >= 0)
                        displayText = "🎨 Đã kiến tạo thành công bộ Slide theo yêu cầu của bạn.";
                    else if (text.IndexOf("MODIFY_SLIDE", StringComparison.OrdinalIgnoreCase) >= 0)
                        displayText = "✅ Đã sửa đổi cấu hình và hiệu ứng trên Slide.";
                    else
                        displayText = text.Trim();
                }
                else
                {
                    displayText = "✦ Đã xử lý xong yêu cầu của bạn.";
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
                        else
                        {
                            _appForm.WriterView.InsertTextFromAI(insertText);
                        }
                        _appForm.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "ZENQ AI - Lỗi Chèn Text");
                }
            }, _appForm);

            pnlChatFeed.AddVirtualControl(bubble);
            ScrollToBottomForce();
        }

        private void ScrollToBottomForce()
        {
            if (pnlChatFeed == null) return;
            pnlChatFeed.PerformLayout();
            try
            {
                pnlChatFeed.AutoScrollPosition = new Point(0, pnlChatFeed.DisplayRectangle.Height);
                pnlChatFeed.ForceUpdateVisible();
            }
            catch { }
        }

        private void ScrollToBottomIfNear()
        {
            if (pnlChatFeed == null) return;
            try
            {
                int maxScroll = pnlChatFeed.VerticalScroll.Maximum - pnlChatFeed.VerticalScroll.LargeChange;
                if (maxScroll <= 0) return;

                int currentScroll = pnlChatFeed.VerticalScroll.Value;
                if (currentScroll >= maxScroll - 100)
                {
                    pnlChatFeed.VerticalScroll.Value = Math.Max(0, maxScroll);
                    pnlChatFeed.ForceUpdateVisible();
                }
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
                   q.Contains("màu xấu") || q.Contains("bố cục xấu") || q.Contains("thay đổi");
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
                var pres = _appForm.SlidesView.Presentation;
                int curIdx = _appForm.SlidesView.ActiveSlideIndex;
                SlideNode curSlide = (pres != null && curIdx >= 0 && curIdx < pres.Slides.Count) ? pres.Slides[curIdx] : null;

                sb.AppendLine("<current_slide_context>");
                sb.AppendLine(string.Format("Tổng số Slide hiện tại: {0}, Slide đang chọn: {1}", pres != null ? pres.Slides.Count : 0, curIdx + 1));

                if (pres != null && pres.Slides.Count > 0)
                {
                    sb.AppendLine("Danh sách tiêu đề và nội dung các slide hiện tại:");
                    for (int sIdx = 0; sIdx < pres.Slides.Count; sIdx++)
                    {
                        var s = pres.Slides[sIdx];
                        List<string> texts = new List<string>();
                        foreach (var sh in s.Shapes)
                        {
                            if (!string.IsNullOrWhiteSpace(sh.Text)) texts.Add(sh.Text.Trim().Replace('\r', ' ').Replace('\n', ' '));
                        }
                        sb.AppendLine(string.Format("Slide {0} ('{1}'): {2}", sIdx + 1, s.Title, string.Join(" | ", texts.ToArray())));
                    }
                }
                sb.AppendLine("</current_slide_context>");

                if (IsSlideCreationRequest(userQuery))
                {
                    sb.AppendLine();
                    sb.AppendLine("[TRI THỨC THIẾT KẾ ĐỒ HỌA & QUY TẮC BỐ CỤC SLIDE ĐA NĂNG (CANVAS 1920 x 1080)]:");
                    sb.AppendLine("Bạn là Chuyên gia Thiết kế Đồ họa Slide cao cấp. Hãy tự do tư duy và sắp đặt các hình khối (shapes), khung ảnh (images), và văn bản (textboxes) theo đúng nhu cầu người dùng:");
                    sb.AppendLine();
                    sb.AppendLine("A. TẠO MỚI HOẶC THIẾT KẾ LẠI TOÀN BỘ SLIDE (FULL REDESIGN / MULTI-SLIDE GENERATION):");
                    sb.AppendLine("   - QUAN TRỌNG: Khi người dùng yêu cầu 'thiết kế lại', 'làm lại bố cục', 'thiết kế lại toàn bộ phong cách', 'màu xấu/bố cục xấu làm lại', hoặc 'tạo bài thuyết trình':");
                    sb.AppendLine("     + BẮT BUỘC DÙNG [GENOFFICE_ACTION:GENERATE_SLIDE_CANVAS] với danh sách \"slides\" chứa tất cả các slide!");
                    sb.AppendLine("     + TUYỆT ĐỐI KHÔNG DÙNG MODIFY_SLIDE trong trường hợp này! (MODIFY_SLIDE chỉ dùng khi người dùng nói rõ là 'chỉ đổi màu nền' hoặc 'chỉ đổi hiệu ứng' mà không đụng vào bố cục).");
                    sb.AppendLine("     + Phải giữ nguyên toàn bộ chủ đề & nội dung bài thuyết trình hiện tại nhưng TÁI CẤU TRÚC lại thành các thẻ bài (Cards), tiêu đề to rõ, icon pill, căn lề trái sang trọng, màu nền gradient sáng hoặc tối theo yêu cầu, và khai báo \"transition\": \"GenOffice_SmoothFlow\" cho từng slide.");
                    sb.AppendLine("   - Cấu trúc JSON khi tạo hoặc thiết kế lại nhiều slide:");
                    sb.AppendLine("     [GENOFFICE_ACTION:GENERATE_SLIDE_CANVAS]");
                    sb.AppendLine("     {");
                    sb.AppendLine("       \"theme\": \"CleanLight\",");
                    sb.AppendLine("       \"slides\": [");
                    sb.AppendLine("         {");
                    sb.AppendLine("           \"title\": \"Slide 1: ...\",");
                    sb.AppendLine("           \"transition\": \"GenOffice_SmoothFlow\",");
                    sb.AppendLine("           \"background\": { \"type\": \"gradient\", \"color1\": \"#F8FAFC\", \"color2\": \"#E2E8F0\", \"angle\": 135 },");
                    sb.AppendLine("           \"elements\": [ ... các thẻ cards và textboxes đẹp mắt ... ]");
                    sb.AppendLine("         },");
                    sb.AppendLine("         {");
                    sb.AppendLine("           \"title\": \"Slide 2: ...\",");
                    sb.AppendLine("           \"transition\": \"GenOffice_SmoothFlow\",");
                    sb.AppendLine("           \"background\": { \"type\": \"gradient\", \"color1\": \"#F8FAFC\", \"color2\": \"#E2E8F0\", \"angle\": 135 },");
                    sb.AppendLine("           \"elements\": [ ... các thẻ cards và textboxes đẹp mắt ... ]");
                    sb.AppendLine("         }");
                    sb.AppendLine("       ]");
                    sb.AppendLine("     }");
                    sb.AppendLine("     [/GENOFFICE_ACTION]");
                    sb.AppendLine();
                    sb.AppendLine("B. TẠO 1 SLIDE ĐƠN:");
                    sb.AppendLine("   - Cấu trúc JSON khi tạo 1 slide: [GENOFFICE_ACTION:GENERATE_SLIDE_CANVAS] { \"theme\": \"CleanLight\", \"title\": \"...\", \"background\": { ... }, \"elements\": [ ... ] } [/GENOFFICE_ACTION]");
                    sb.AppendLine();
                    sb.AppendLine("C. CÁC QUY TẮC BỐ CỤC KHÔNG GIAN NGHỆ THUẬT:");
                    sb.AppendLine("   1. Bố cục 1 bên Ảnh/Minh họa - 1 bên Chữ (Split View):");
                    sb.AppendLine("      - Cột Trái (Ảnh/Minh họa): bounds: [100, 160, 800, 820] (shape RoundedRectangle, viền tương phản #38BDF8, shadow).");
                    sb.AppendLine("      - Cột Phải (Chữ): Tiêu đề bounds: [960, 160, 860, 60], align: 'left'; Accent bar: bounds: [960, 230, 80, 5]; Văn bản: bounds: [960, 250, 860, 600], align: 'left'.");
                    sb.AppendLine("   2. Căn lề trái (align: 'left') cho các đoạn văn dài để tạo vẻ hiện đại, dễ đọc, không căn giữa tràn lan.");
                    sb.AppendLine("   3. TÍNH TOÁN CHIỀU CAO THẺ VỪA KHÍT NỘI DUNG (TUYỆT ĐỐI KHÔNG TẠO THẺ QUÁ CAO/QUÁ TO GÂY RỖNG ĐEN):");
                    sb.AppendLine("      - Thẻ có Tiêu đề + 2-3 dòng chữ ngắn: bounds: [x, y, w, h] với h: 220 đến 260.");
                    sb.AppendLine("      - Thẻ có Tiêu đề + 4-6 dòng chữ: bounds: [x, y, w, h] với h: 300 đến 350.");
                    sb.AppendLine("      - Thẻ KPI / Thống kê số: bounds: [x, y, w, h] với h: 180 đến 220.");
                    sb.AppendLine("   4. Tone tối: background '#0B0F19' - '#111827', chữ '#F8FAFC' / '#94A3B8'. Tone sáng: background '#F8FAFC' - '#EEF2F6', chữ '#0F172A'.");
                    sb.AppendLine();
                    sb.AppendLine("D. ĐỔI MÀU NỀN CHO SLIDE HIỆN TẠI (CHỈ KHI NGƯỜI DÙNG CHỈ YÊU CẦU ĐỔI NỀN):");
                    sb.AppendLine("   - Khi người dùng chỉ yêu cầu đổi màu nền mà không yêu cầu thiết kế lại:");
                    sb.AppendLine("     [GENOFFICE_ACTION:MODIFY_SLIDE]");
                    sb.AppendLine("     {");
                    sb.AppendLine("       \"allSlides\": true,");
                    sb.AppendLine("       \"background\": { \"type\": \"gradient\", \"color1\": \"#F8FAFC\", \"color2\": \"#E2E8F0\", \"angle\": 135 }");
                    sb.AppendLine("     }");
                    sb.AppendLine("     [/GENOFFICE_ACTION]");
                    sb.AppendLine();
                    sb.AppendLine("E. TRI THỨC PHỐI MÀU GRADIENT CAO CẤP (GRADIENT TONE PALETTES):");
                    sb.AppendLine("   - Khi tạo màu nền gradient hoặc hình khối gradient, sử dụng các cặp màu chuyển sắc hiện đại (góc nghiêng angle: 135 độ):");
                    sb.AppendLine("     1. Deep Tech (Xanh Đen Công Nghệ): color1: '#0B0F19', color2: '#1E293B', angle: 135");
                    sb.AppendLine("     2. Ocean Blue (Đại Dương Sâu): color1: '#0B1E3B', color2: '#1E3A8A', angle: 135");
                    sb.AppendLine("     3. Midnight Cyber (Tím Huyền Bí): color1: '#0F0C20', color2: '#2E1065', angle: 135");
                    sb.AppendLine("     4. Emerald Forest (Xanh Ngọc Lục Bảo): color1: '#022C22', color2: '#064E3B', angle: 135");
                    sb.AppendLine("     5. Sunset Luxury (Hoàng Hôn Đồng Sang Trọng): color1: '#1C1917', color2: '#78350F', angle: 135");
                    sb.AppendLine("     6. Clean Minimal Light (Sáng Hiện Đại): color1: '#F8FAFC', color2: '#E2E8F0', angle: 135");
                    sb.AppendLine();
                    sb.AppendLine("F. NGHỆ THUẬT CHUYỂN CẢNH (TRANSITIONS) & ĐỔI HIỆU ỨNG:");
                    sb.AppendLine("   - Khi tạo slide mới, mỗi slide trong 'slides' có thể khai báo: \"transition\": \"Tên_Hiệu_Ứng\".");
                    sb.AppendLine("   - KHI NGƯỜI DÙNG YÊU CẦU ĐỔI HIỆU ỨNG CHO CÁC SLIDE CỤ THỂ (ví dụ: 'đổi slide 5 và 7 và 12 thành morph', 'đổi slide 2, 4 thành chảy tràn'):");
                    sb.AppendLine("     [GENOFFICE_ACTION:MODIFY_SLIDE]");
                    sb.AppendLine("     {");
                    sb.AppendLine("       \"targetSlides\": [5, 7, 12],");
                    sb.AppendLine("       \"transition\": \"Morph\"");
                    sb.AppendLine("     }");
                    sb.AppendLine("     [/GENOFFICE_ACTION]");
                    sb.AppendLine("   - TÊN CÁC HIỆU ỨNG CHUYỂN CẢNH ĐỈNH CAO CÓ SẴN (BẮT BUỘC DÙNG ĐÚNG TÊN DƯỚI ĐÂY):");
                    sb.AppendLine("     * 'Chảy tràn' -> BẮT BUỘC dùng: \"GenOffice_SmoothFlow\" (hoặc 'Chảy tràn') - TUYỆT ĐỐI KHÔNG DÙNG 'Thủy dịch'!");
                    sb.AppendLine("     * 'Thủy dịch' -> Dùng: \"GenOffice_LiquidFluid\"");
                    sb.AppendLine("     * 'Lụa Vàng' -> Dùng: \"GenOffice_GoldenRibbon\"");
                    sb.AppendLine("     * 'Lăng kính' -> Dùng: \"GenOffice_CinematicPrism\"");
                    sb.AppendLine("     * 'Mưa số' -> Dùng: \"GenOffice_MatrixRain\"");
                    sb.AppendLine("     * 'Xung Neon' -> Dùng: \"GenOffice_NeonPulse\"");
                    sb.AppendLine("     * 'Lỗ sâu' -> Dùng: \"GenOffice_PortalWarp\"");
                    sb.AppendLine("     * 'Siêu tân tinh' -> Dùng: \"GenOffice_Supernova\"");
                    sb.AppendLine("     * 'Hologram' -> Dùng: \"GenOffice_HologramGlitch\"");
                    sb.AppendLine("     * 'Vạn hoa' -> Dùng: \"GenOffice_Kaleidoscope\"");
                    sb.AppendLine("     * 'Bão sao' -> Dùng: \"GenOffice_Starlight\"");
                    sb.AppendLine("     * 'Morph' -> Dùng: \"Morph\"");
                    sb.AppendLine("     * Phổ biến: 'Zoom', 'Fade', 'Push', 'Wipe', 'Cube', 'Flip', 'Origami', 'Airplane', 'PageCurl', 'Honeycomb', 'Glitter', 'Vortex'.");
                    sb.AppendLine("   - NGUYÊN TẮC VÀNG THIẾT KẾ SLIDE MORPH ĐỈNH CAO (KỸ THUẬT VẬT THỂ LIÊN SLIDE & NGOÀI MÀN HÌNH):");
                    sb.AppendLine("     1. Đồng bộ vật thể (Same MorphName): Các đối tượng cần chuyển đổi vị trí/kích thước giữa 2 slide liên tiếp phải có cùng 'morphName' (ví dụ: 'card_1', 'card_2', 'hero_box', 'main_title').");
                    sb.AppendLine("     2. Kỹ thuật Bay Ra / Bay Vào Ngoài Khung Hình (Off-screen Positioning):");
                    sb.AppendLine("        - Để 1 vật phẩm ở Slide N biến mất mượt mà sang Slide N+1: Đừng xóa nó, hãy đưa nó vào Slide N+1 nhưng đặt tọa độ NGOÀI màn hình 1920x1080 (ví dụ: Trên: bounds [x, -600, w, h], Dưới: bounds [x, 1200, w, h], Trái: bounds [-600, y, w, h], Phải: bounds [2100, y, w, h]).");
                    sb.AppendLine("        - Để 1 vật phẩm ở Slide N+1 bay từ ngoài vào: Hãy khai báo nó ở Slide N với tọa độ ngoài màn hình cùng 'morphName'. Khi sang Slide N+1, nó sẽ bay vút vào vị trí chính giữa!");
                    sb.AppendLine("     3. Ví dụ mẫu chuỗi Slide Morph 4 thẻ chuẩn chuyên nghiệp (như Apple Keynote):");
                    sb.AppendLine("        - Slide 1: 4 thẻ nằm thẳng hàng trong màn hình: card_1 [100, 320, 380, 520], card_2 [530, 320, 380, 520], card_3 [960, 320, 380, 520], card_4 [1390, 320, 380, 520].");
                    sb.AppendLine("        - Slide 2: transition: 'Morph'. Thẻ trọng tâm 'hero_center' [460, 260, 1000, 560] ở giữa. Đồng thời đưa 4 thẻ cũ ra 4 hướng ngoài màn hình: card_1 [100, -600, 380, 520] (bay lên trên), card_2 [-500, 320, 380, 520] (bay sang trái), card_3 [2100, 320, 380, 520] (bay sang phải), card_4 [1390, 1200, 380, 520] (bay xuống dưới).");
                    sb.AppendLine("        - Khi trình chiếu: 4 thẻ ở Slide 1 sẽ tự động bay tỏa ra 4 hướng ngoài khung hình và thẻ trọng tâm xuất hiện cực kỳ hoành tráng!");
                }
            }
            else if (mode == OfficeAppMode.Writer && _appForm != null && _appForm.WriterView != null)
            {
                string docText = _appForm.WriterView.PieceTable != null ? _appForm.WriterView.PieceTable.GetText() : "";
                string selText = _appForm.WriterView.GetSelectedText();

                if (docText.Length > 35000) docText = docText.Substring(0, 35000) + "... [Đã rút gọn vì quá dài]";
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
                sb.AppendLine("[HƯỚNG DẪN TRỢ LÝ SOẠN THẢO VĂN BẢN & TÌM SỬA LỖI CHÍNH TẢ / VIẾT BÀI CHI TIẾT]:");
                sb.AppendLine("1. KHI NGƯỜI DÙNG YÊU CẦU TÌM LỖI, SỬA CHÍNH TẢ, SOÁT LỖI, SỬA NGỮ PHÁP (TIẾNG VIỆT, TIẾNG ANH, HOẶC BẤT KỲ TIẾNG NÀO):");
                sb.AppendLine("   - Nếu có <selected_text> (vùng đang quét khối), CHỈ tìm và sửa lỗi chính tả BÊN TRONG thẻ <selected_text>. TUYỆT ĐỐI BỎ QUA các lỗi ở phần khác của <document_context>.");
                sb.AppendLine("   - Nếu không có <selected_text>, hãy kiểm tra toàn bộ văn bản trong <document_context>.");
                sb.AppendLine("   - Liệt kê chi tiết từng từ sai BỊ GIỚI HẠN trong phạm vi quét khối, giải thích và từ sửa đúng theo định dạng chuẩn:");
                sb.AppendLine("     ❌ `từ_sai` ➔ ✅ `từ_đúng` (Giải thích / Ngữ cảnh)");
                sb.AppendLine("   - BẮT BUỘC KÈM THEO KHỐI HÀNH ĐỘNG XÁC NHẬN SỬA ĐỂ PHẦN MỀM TỰ ĐỘNG THAY THẾ CHÍNH XÁC:");
                sb.AppendLine("     [GENOFFICE_ACTION:APPLY_SPELL_CORRECTIONS]");
                sb.AppendLine("     {");
                sb.AppendLine("       \"corrections\": [");
                sb.AppendLine("         { \"original\": \"từ_sai_1\", \"replacement\": \"từ_đúng_1\" },");
                sb.AppendLine("         { \"original\": \"từ_sai_2\", \"replacement\": \"từ_đúng_2\" }");
                sb.AppendLine("       ]");
                sb.AppendLine("     }");
                sb.AppendLine("     [/GENOFFICE_ACTION]");
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
                sb.AppendLine("   - BẮT BUỘC XUẤT RA KHỐI HÀNH ĐỘNG: [GENOFFICE_ACTION:SET_CALC_DATA] ... [/GENOFFICE_ACTION] (hoặc bảng Markdown | Cột 1 | Cột 2 | ...)");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG ĐƯỢC CHỈ NÓI MỒM HAY GỢI Ý BẰNG CHỮ TRONG CHAT! PHẢI XUẤT RA DỮ LIỆU ĐỂ HỆ THỐNG GHI THẲNG VÀO BẢNG TÍNH!");
                sb.AppendLine("   - NGUYÊN TẮC BỐ CỤC BẢNG ĐẸP VÀ CHUYÊN NGHIỆP:");
                sb.AppendLine("     + Nếu thêm BẢNG PHÂN TÍCH / SO SÁNH / KPI phụ bên cạnh bảng chính: ĐẶT BẢNG NGANG SANG PHÍA BÊN PHẢI bảng hiện tại (bắt đầu cùng hàng 3) và CÁCH ÍT NHẤT 1 CỘT TRỐNG (ví dụ bảng cũ tới cột G thì bảng mới bắt đầu từ cột I3).");
                sb.AppendLine("     + Hoặc nếu đặt BÊN DƯỚI bảng cũ: phải CÁCH ÍT NHẤT 2-3 HÀNG TRỐNG sau dòng Tổng Cộng / Trung Bình (ví dụ bảng cũ hết ở dòng 15 thì bảng mới bắt đầu ở dòng 18).");
                sb.AppendLine("     + TUYỆT ĐỐI KHÔNG VẼ BẢNG ĐÈ LÊN DÒNG TỔNG CỘNG HAY DÒNG TRUNG BÌNH CỦA BẢNG CŨ!");
                sb.AppendLine("   - Cấu trúc JSON chuẩn:");
                sb.AppendLine("     [GENOFFICE_ACTION:SET_CALC_DATA]");
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
                sb.AppendLine("     [/GENOFFICE_ACTION]");
                sb.AppendLine();
                sb.AppendLine("2. KHI NGƯỜI DÙNG CHỈ ĐẶT CÂU HỎI PHÂN TÍCH / TÌM KIẾM THÔNG TIN CÓ SẴN:");
                sb.AppendLine("   - Đọc trực tiếp các con số trong bảng tính ở trên, tính toán và trả lời ngay lập tức.");
                sb.AppendLine("   - TUYỆT ĐỐI KHÔNG HỎI LẠI! Dữ liệu đã có đủ 100%.");
            }

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
    /// Control hiển thị hiệu ứng "AI đang suy nghĩ..." chuẩn Breathing Light 100% Khắc Phục Chớp Nháy / Flicker.
    /// Khá»­ sáº¡ch chá»›p nhÃ¡y báº±ng DoubleBuffer pháº§n cá»©ng vÃ  hiá»‡u á»©ng thá»Ÿ Ã¡nh sÃ¡ng sang trá» ng (Apple/Gemini style).
    /// </summary>
    public class ThinkingBubbleControl : Control
    {
        private float _phase = 0f;
        private System.Windows.Forms.Timer _timer;

        public ThinkingBubbleControl()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.Size = new Size(135, 32);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 35; // ~30 FPS smooth breathing
            _timer.Tick += (s, e) =>
            {
                _phase += 0.08f;
                if (_phase > 62.83f) _phase = 0f;
                this.Invalidate();
            };
            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Khung bubble trắng viền xám mượt mà
            Rectangle r = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
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

            // 2. Hiệu ứng thở ánh sáng Breathing Glow cho Icon Sparkle ✦ và Text
            float wave = (float)(Math.Sin(_phase) * 0.5 + 0.5); // 0.0 -> 1.0
            int iconAlpha = (int)(130 + wave * 125); // 130 -> 255
            int textAlpha = (int)(110 + wave * 125);

            // Icon Sparkle ✦ bên trái
            AiVectorGlassRenderer.DrawVectorSparkle(g, 10, 8, 15, Color.FromArgb(iconAlpha, 79, 70, 229));

            // Text "Đang suy nghĩ..."
            using (Font font = new Font("Segoe UI", 9f, FontStyle.Italic))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(textAlpha, 71, 85, 105)))
            {
                g.DrawString("Đang suy nghĩ...", font, textBrush, 29, 7);
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
            if (disposing) Stop();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// FlowLayoutPanel kÃ©p vá»›i tÃ­nh nÄƒng ngÄƒn cháº·n WinForms tá»± Ä‘á»™ng giáº­t/kÃ©o thanh cuá»™n vá»  control con khi render.
    /// </summary>
        /// <summary>
    /// FlowLayoutPanel kép với tính năng ngăn chặn WinForms tự động giật/kéo thanh cuộn về control con khi render.
    /// </summary>
    public class VirtualChatFeedPanel : FlowLayoutPanel
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        private const int SB_HORZ = 0;

        public VirtualChatFeedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.AutoScroll = true;
            this.FlowDirection = FlowDirection.TopDown;
            this.WrapContents = false;
            this.HorizontalScroll.Maximum = 0;
            this.HorizontalScroll.Enabled = false;
            this.HorizontalScroll.Visible = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Padding = new Padding(6, 6, 6, 12);
        }

        public void SuspendVirtualLayout()
        {
            this.SuspendLayout();
        }

        public void ResumeVirtualLayout()
        {
            this.ResumeLayout(true);
            this.PerformLayout();
            KillHorizontalScrollBar();
        }

        public void AddVirtualControl(Control c)
        {
            if (c == null) return;
            this.Controls.Add(c);
            KillHorizontalScrollBar();
        }

        public void RemoveVirtualControl(Control c)
        {
            if (c == null) return;
            this.Controls.Remove(c);
        }

        public void ClearVirtualControls()
        {
            this.SuspendLayout();
            for (int i = this.Controls.Count - 1; i >= 0; i--)
            {
                Control c = this.Controls[i];
                this.Controls.RemoveAt(i);
                if (c != null && !c.IsDisposed)
                {
                    c.Dispose();
                }
            }
            this.ResumeLayout(true);
            this.AutoScrollPosition = new Point(0, 0);
        }

        public void ForceUpdateVisible()
        {
            this.Invalidate();
        }

        private void KillHorizontalScrollBar()
        {
            if (this.IsHandleCreated)
            {
                ShowScrollBar(this.Handle, SB_HORZ, false);
            }
        }

        protected override Point ScrollToControl(Control activeControl)
        {
            // Ngăn WinForms tự động giật màn hình khi focus vào control con
            return this.AutoScrollPosition;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            KillHorizontalScrollBar();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            KillHorizontalScrollBar();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style &= ~0x00100000; // WS_HSCROLL
                return cp;
            }
        }
    }

    public class UserChatBubbleControl : Control
    {
        private string _text;
        private int _messageIndex;
        private Action<string, int> _onEditAndResend;
        private int _bubbleW;
        private int _bubbleH;
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

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);
            this.Margin = new Padding(0, 4, 0, 8);

            int maxBubbleW = Math.Max(120, (int)(feedW * 0.78f));
            int padX = 14;
            int padY = 8;
            Size txtSize = TextRenderer.MeasureText(_text, this.Font, new Size(maxBubbleW - padX * 2, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            _bubbleW = Math.Max(42, Math.Min(maxBubbleW, txtSize.Width + padX * 2));
            _bubbleH = Math.Max(32, txtSize.Height + padY * 2);

            int btnH = 22;
            int replyBtnW = 76;
            int copyBtnW = 78;
            int btnY = _bubbleH + 6;

            _copyBtnRect = new Rectangle(feedW - copyBtnW - 8, btnY, copyBtnW, btnH);
            _replyBtnRect = new Rectangle(feedW - copyBtnW - 8 - replyBtnW - 4, btnY, replyBtnW, btnH);

            this.Size = new Size(feedW, _bubbleH + 32);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bgB = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgB, this.ClientRectangle);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Bong bóng tím bên phải
            int bX = Math.Max(2, this.ClientSize.Width - _bubbleW - 6);
            Rectangle r = new Rectangle(bX, 2, _bubbleW, _bubbleH);
            using (GraphicsPath gp = RoundRect(r, 14f))
            using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(79, 70, 229), Color.FromArgb(99, 102, 241), 135f))
            {
                g.FillPath(lgb, gp);
            }

            Rectangle txtR = new Rectangle(bX + 14, 2 + 8, _bubbleW - 28, _bubbleH - 16);
            TextRenderer.DrawText(g, _text, this.Font, txtR, Color.White, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            // 2. NÃºt "â†© Há»i láº¡i" (Edit & Resend)
            if (_isReplyHover)
            {
                using (GraphicsPath p = RoundRect(_replyBtnRect, 5f))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(238, 242, 255)))
                {
                    g.FillPath(sb, p);
                }
            }
            AiVectorGlassRenderer.DrawVectorReply(g, _replyBtnRect.X + 4, _replyBtnRect.Y + 3, 14, _isReplyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(148, 163, 184));
            using (Font bf = new Font("Segoe UI", 8.2f))
            {
                Rectangle textR = new Rectangle(_replyBtnRect.X + 20, _replyBtnRect.Y, _replyBtnRect.Width - 20, _replyBtnRect.Height);
                TextRenderer.DrawText(g, "Hỏi lại", bf, textR, _isReplyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            // 3. Nút "📋 Sao chép"
            if (_isCopyHover || _isCopied)
            {
                using (GraphicsPath p = RoundRect(_copyBtnRect, 5f))
                using (SolidBrush sb = new SolidBrush(_isCopied ? Color.FromArgb(220, 252, 231) : Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(sb, p);
                }
            }

            if (_isCopied)
            {
                AiVectorGlassRenderer.DrawVectorCheck(g, _copyBtnRect.X + 4, _copyBtnRect.Y + 3, 14, Color.FromArgb(22, 163, 74));
                using (Font bf = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 20, _copyBtnRect.Y, _copyBtnRect.Width - 20, _copyBtnRect.Height);
                    TextRenderer.DrawText(g, "Đã chép", bf, textR, Color.FromArgb(22, 163, 74), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
            else
            {
                AiVectorGlassRenderer.DrawVectorCopy(g, _copyBtnRect.X + 4, _copyBtnRect.Y + 3, 14, _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(148, 163, 184));
                using (Font bf = new Font("Segoe UI", 8.2f))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 20, _copyBtnRect.Y, _copyBtnRect.Width - 20, _copyBtnRect.Height);
                    TextRenderer.DrawText(g, "Sao chép", bf, textR, _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
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
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, radius, radius, 180, 90);
            gp.AddArc(r.Width + r.X - radius, r.Y, radius, radius, 270, 90);
            gp.AddArc(r.Width + r.X - radius, r.Height + r.Y - radius, radius, radius, 0, 90);
            gp.AddArc(r.X, r.Height + r.Y - radius, radius, radius, 90, 90);
            gp.CloseFigure();
            return gp;
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
    /// Control hiển thị tin nhắn AI siêu mượt mà với nút hành động chèn nội dung và nút 📋 Sao chép.
    /// </summary>
    public class AiChatBubbleControl : Control
    {
        private string _text;
        private int _bubbleW;
        private int _bubbleH;
        private Rectangle _actionBtnRect;
        private Rectangle _copyBtnRect;
        private bool _isActionHover = false;
        private bool _isCopyHover = false;
        private bool _isCopied = false;
        private System.Windows.Forms.Timer _copyTimer;
        private Action<string> _onInsertAction;
        private string _actionText;

        public AiChatBubbleControl(string text, int feedW, OfficeAppMode mode, Action<string> onInsert, AppForm appForm = null)
        {
            _text = text ?? "";
            _onInsertAction = onInsert;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.3f);
            this.Margin = new Padding(0, 4, 0, 8);

            int maxBubbleW = Math.Max(140, (int)(feedW * 0.82f));
            int padX = 14;
            int padY = 9;
            Size txtSize = TextRenderer.MeasureText(_text, this.Font, new Size(maxBubbleW - padX * 2, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            _bubbleW = Math.Max(46, Math.Min(maxBubbleW, txtSize.Width + padX * 2));
            _bubbleH = Math.Max(32, txtSize.Height + padY * 2);

            _actionText = (mode == OfficeAppMode.Slides) ? "🎨 Chèn vào Slide" : ((mode == OfficeAppMode.Calc) ? "📊 Chèn vào Bảng tính" : (appForm != null && appForm.WriterView != null && !string.IsNullOrEmpty(appForm.WriterView.GetSelectedText()) ? "📝 Thay thế vùng quét khối" : "📝 Chèn vào văn bản"));
            _actionBtnRect = new Rectangle(8, _bubbleH + 6, 136, 22);
            _copyBtnRect = new Rectangle(8 + 136 + 6, _bubbleH + 6, 80, 22);

            this.Size = new Size(feedW, _bubbleH + 34);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bgB = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgB, this.ClientRectangle);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Nền bong bóng AI
            Rectangle r = new Rectangle(4, 2, _bubbleW, _bubbleH);
            using (GraphicsPath gp = RoundRect(r, 14f))
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

            // Chữ bên trong
            Rectangle txtR = new Rectangle(4 + 14, 2 + 9, _bubbleW - 28, _bubbleH - 18);
            TextRenderer.DrawText(g, _text, this.Font, txtR, Color.FromArgb(15, 23, 42), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            // Nút Chèn
            if (_isActionHover)
            {
                using (GraphicsPath abp = RoundRect(_actionBtnRect, 6f))
                using (SolidBrush abb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(abb, abp);
                }
            }
            using (Font abf = new Font("Segoe UI", 8.2f))
            {
                TextRenderer.DrawText(g, _actionText, abf, _actionBtnRect, _isActionHover ? Color.FromArgb(67, 56, 202) : Color.FromArgb(100, 116, 139), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            // Nút Sao chép AI
            if (_isCopyHover || _isCopied)
            {
                using (GraphicsPath p = RoundRect(_copyBtnRect, 6f))
                using (SolidBrush sb = new SolidBrush(_isCopied ? Color.FromArgb(220, 252, 231) : Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(sb, p);
                }
            }

            if (_isCopied)
            {
                AiVectorGlassRenderer.DrawVectorCheck(g, _copyBtnRect.X + 4, _copyBtnRect.Y + 4, 14, Color.FromArgb(22, 163, 74));
                using (Font bf = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 20, _copyBtnRect.Y, _copyBtnRect.Width - 20, _copyBtnRect.Height);
                    TextRenderer.DrawText(g, "Đã chép", bf, textR, Color.FromArgb(22, 163, 74), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
            else
            {
                AiVectorGlassRenderer.DrawVectorCopy(g, _copyBtnRect.X + 4, _copyBtnRect.Y + 4, 14, _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(148, 163, 184));
                using (Font bf = new Font("Segoe UI", 8.2f))
                {
                    Rectangle textR = new Rectangle(_copyBtnRect.X + 20, _copyBtnRect.Y, _copyBtnRect.Width - 20, _copyBtnRect.Height);
                    TextRenderer.DrawText(g, "Sao chép", bf, textR, _isCopyHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool actHover = _actionBtnRect.Contains(e.Location);
            bool copHover = _copyBtnRect.Contains(e.Location);
            if (actHover != _isActionHover || copHover != _isCopyHover)
            {
                _isActionHover = actHover;
                _isCopyHover = copHover;
                this.Cursor = (actHover || copHover) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_actionBtnRect.Contains(e.Location) && _onInsertAction != null)
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
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, radius, radius, 180, 90);
            gp.AddArc(r.Width + r.X - radius, r.Y, radius, radius, 270, 90);
            gp.AddArc(r.Width + r.X - radius, r.Height + r.Y - radius, radius, radius, 0, 90);
            gp.AddArc(r.X, r.Height + r.Y - radius, radius, radius, 90, 90);
            gp.CloseFigure();
            return gp;
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
            Title = "Äoáº¡n chat má»›i";
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
            this.Text = "Lịch sử đoạn chat AI";
            this.Size = new Size(440, 560);
            this.MinimumSize = new Size(400, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.DoubleBuffered = true;

            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 3000;
            toolTip.InitialDelay = 400;

            // 1. Header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Draw vector clock icon
                using (Pen cp = new Pen(Color.FromArgb(79, 70, 229), 1.8f))
                {
                    cp.StartCap = LineCap.Round;
                    cp.EndCap = LineCap.Round;
                    e.Graphics.DrawEllipse(cp, 16, 17, 18, 18);
                    e.Graphics.DrawLine(cp, 25, 21, 25, 26);
                    e.Graphics.DrawLine(cp, 25, 26, 29, 26);
                }
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = "Lịch sử hội thoại",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(42, 14),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Button btnClearAll = new Button
            {
                Text = "      Xóa tất cả",
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
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                AiVectorGlassRenderer.DrawVectorTrash(e.Graphics, 8, 7, 16, Color.FromArgb(239, 68, 68));
            };
            btnClearAll.Click += (s, e) =>
            {
                var res = MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa toàn bộ lịch sử các đoạn chat?",
                    "Xác nhận xóa tất cả",
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
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
                }
            };

            Button btnNew = new Button
            {
                Text = "+ Bắt đầu đoạn chat mới",
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
                Panel emptyCard = new Panel
                {
                    Size = new Size(pnlList.ClientSize.Width > 40 ? pnlList.ClientSize.Width - 26 : 380, 160),
                    Margin = new Padding(0, 40, 0, 0),
                    BackColor = Color.Transparent
                };
                Label lblEmpty = new Label
                {
                    Text = "💬 Chưa có đoạn chat nào trong lịch sử.\nHãy trò chuyện với AI để lưu lại các cuộc hội thoại!",
                    Font = new Font("Segoe UI", 10f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    TextAlign = ContentAlignment.MiddleCenter,
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
            Panel card = new Panel
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
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderCol = isHover ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240);
                using (Pen p = new Pen(borderCol, isHover ? 1.5f : 1f))
                using (GraphicsPath gp = RoundRect(new RectangleF(0, 0, card.Width - 1, card.Height - 1), 8f))
                {
                    e.Graphics.DrawPath(p, gp);
                }

                // Left icon badge
                RectangleF badgeRect = new RectangleF(12, 16, 38, 38);
                using (GraphicsPath bp = RoundRect(badgeRect, 6f))
                using (SolidBrush bb = new SolidBrush(isHover ? Color.FromArgb(224, 231, 255) : Color.FromArgb(241, 245, 249)))
                {
                    e.Graphics.FillPath(bb, bp);
                }
                
                // Draw vector chat icon inside badge
                using (Pen iconPen = new Pen(Color.FromArgb(79, 70, 229), 1.6f))
                {
                    iconPen.StartCap = LineCap.Round;
                    iconPen.EndCap = LineCap.Round;
                    iconPen.LineJoin = LineJoin.Round;
                    
                    float ix = 20, iy = 24;
                    e.Graphics.DrawRectangle(iconPen, ix, iy, 22, 15);
                    e.Graphics.DrawLine(iconPen, ix + 5, iy + 5, ix + 17, iy + 5);
                    e.Graphics.DrawLine(iconPen, ix + 5, iy + 9, ix + 13, iy + 9);
                }
            };

            // Title
            Label lblCardTitle = new Label
            {
                Text = !string.IsNullOrEmpty(sess.Title) ? sess.Title : "Đoạn chat không tên",
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
                Text = string.Format("{0} tin nhắn • {1}", msgCount, timeStr),
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
            Button btnDel = new Button
            {
                Text = "",
                Size = new Size(34, 34),
                Location = new Point(cardW - 46, 17),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnDel.FlatAppearance.BorderSize = 0;
            toolTip.SetToolTip(btnDel, "Xóa đoạn chat này");

            bool isDelHover = false;
            btnDel.MouseEnter += (s, e) => { isDelHover = true; btnDel.Invalidate(); };
            btnDel.MouseLeave += (s, e) => { isDelHover = false; btnDel.Invalidate(); };

            btnDel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (isDelHover)
                {
                    using (GraphicsPath dp = RoundRect(new RectangleF(0, 0, btnDel.Width - 1, btnDel.Height - 1), 6f))
                    using (SolidBrush db = new SolidBrush(Color.FromArgb(254, 226, 226)))
                    using (Pen dpPen = new Pen(Color.FromArgb(254, 202, 202), 1f))
                    {
                        e.Graphics.FillPath(db, dp);
                        e.Graphics.DrawPath(dpPen, dp);
                    }
                }
                Color delCol = isDelHover ? Color.FromArgb(220, 38, 38) : Color.FromArgb(148, 163, 184);
                AiVectorGlassRenderer.DrawVectorTrash(e.Graphics, 6, 6, 22, delCol);
            };

            btnDel.Click += (s, e) =>
            {
                var res = MessageBox.Show(
                    string.Format("Bạn có chắc chắn muốn xóa đoạn chat \"{0}\"?", !string.IsNullOrEmpty(sess.Title) ? sess.Title : "Đoạn chat"),
                    "Xác nhận xóa đoạn chat",
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
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, radius, radius, 180, 90);
            gp.AddArc(r.Width + r.X - radius, r.Y, radius, radius, 270, 90);
            gp.AddArc(r.Width + r.X - radius, r.Height + r.Y - radius, radius, radius, 0, 90);
            gp.AddArc(r.X, r.Height + r.Y - radius, radius, radius, 90, 90);
            gp.CloseFigure();
            return gp;
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
                                    DateTime ts = DateTime.Now;
                                    if (dict.ContainsKey("timestamp"))
                                    {
                                        DateTime.TryParse(dict["timestamp"].ToString(), out ts);
                                    }
                                    messages.Add(new AiChatMessage { Role = role, Content = content, Timestamp = ts });
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
                                                DateTime ts = DateTime.Now;
                                                if (mDict.ContainsKey("timestamp"))
                                                {
                                                    DateTime.TryParse(mDict["timestamp"].ToString(), out ts);
                                                }
                                                session.Messages.Add(new AiChatMessage { Role = role, Content = content, Timestamp = ts });
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
            if (messages == null || messages.Count == 0) return "Äoáº¡n chat má»›i";
            foreach (var m in messages)
            {
                if (m.Role == "user" && !string.IsNullOrWhiteSpace(m.Content))
                {
                    string clean = m.Content.Trim().Replace("\r", " ").Replace("\n", " ");
                    if (clean.Length > 40) clean = clean.Substring(0, 38) + "...";
                    return clean;
                }
            }
            return "Äoáº¡n chat má»›i";
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

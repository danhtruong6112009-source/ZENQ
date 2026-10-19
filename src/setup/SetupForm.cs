using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ZenQOffice.Setup
{
    public enum SetupStep
    {
        TermsAgreement = 0,
        InstallOptions = 1,
        Installing = 2
    }

    #region Custom Modern Controls (Rounded & Fluent)

    public class ModernRoundedButton : Control
    {
        private bool _isHovered = false;
        private bool _isPressed = false;
        public float BorderRadius { get; set; }
        public Color BaseColor { get; set; }
        public Color HoverColor { get; set; }
        public Color PressedColor { get; set; }
        public Color BorderColor { get; set; }

        public ModernRoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);

            BorderRadius = 8f;
            BaseColor = Color.FromArgb(37, 99, 235);
            HoverColor = Color.FromArgb(29, 78, 216);
            PressedColor = Color.FromArgb(30, 64, 175);
            BorderColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _isPressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = SetupForm.CreateRoundedRectanglePath(rect, BorderRadius))
            {
                Color fill = BaseColor;
                if (!Enabled) fill = Color.FromArgb(203, 213, 225);
                else if (_isPressed) fill = PressedColor;
                else if (_isHovered) fill = HoverColor;

                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }

                if (BorderColor != Color.Transparent && Enabled)
                {
                    using (Pen pen = new Pen(BorderColor, 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            Color textColor = Enabled ? ForeColor : Color.FromArgb(148, 163, 184);
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(Text, Font, textBrush, new RectangleF(0, 0, Width, Height), sf);
            }
        }
    }

    public class ModernRoundedCheckBox : Control
    {
        private bool _checked = true;
        private bool _isHovered = false;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    Invalidate();
                    if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler CheckedChanged;

        public ModernRoundedCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);

            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);
            ForeColor = Color.FromArgb(30, 41, 59);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float boxSize = 20f;
            float boxY = (Height - boxSize) / 2f;
            RectangleF boxRect = new RectangleF(2f, boxY, boxSize, boxSize);

            using (GraphicsPath boxPath = SetupForm.CreateRoundedRectanglePath(boxRect, 5f))
            {
                if (_checked)
                {
                    using (SolidBrush fill = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    {
                        g.FillPath(fill, boxPath);
                    }

                    // Draw Crisp White Checkmark (✓)
                    using (Pen checkPen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        g.DrawLines(checkPen, new PointF[] {
                            new PointF(boxRect.Left + 5f, boxRect.Top + 10.5f),
                            new PointF(boxRect.Left + 8.5f, boxRect.Top + 14.5f),
                            new PointF(boxRect.Left + 15f, boxRect.Top + 6f)
                        });
                    }
                }
                else
                {
                    using (SolidBrush fill = new SolidBrush(Color.White))
                    using (Pen borderPen = new Pen(_isHovered ? Color.FromArgb(59, 130, 246) : Color.FromArgb(203, 213, 225), 1.5f))
                    {
                        g.FillPath(fill, boxPath);
                        g.DrawPath(borderPen, boxPath);
                    }
                }
            }

            // Draw label text
            using (SolidBrush tb = new SolidBrush(ForeColor))
            {
                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                g.DrawString(Text, Font, tb, new RectangleF(boxSize + 10f, 0, Width - boxSize - 10f, Height), sf);
            }
        }
    }

    public class ModernTermsPanel : Control
    {
        private float _scrollY = 0f;
        private float _maxScroll = 0f;

        public ModernTermsPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(248, 250, 252);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            _scrollY = Math.Max(0f, Math.Min(_maxScroll, _scrollY - e.Delta * 0.35f));
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Card background & border
            RectangleF cardRect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = SetupForm.CreateRoundedRectanglePath(cardRect, 8f))
            using (SolidBrush bg = new SolidBrush(BackColor))
            using (Pen border = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            // Inner clipping for smooth text scrolling
            RectangleF textClip = new RectangleF(14, 10, Width - 28, Height - 20);
            GraphicsState state = g.Save();
            g.SetClip(textClip);

            float curY = 12f - _scrollY;
            float textW = Width - 36;

            // Header
            using (Font hFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
            using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("ĐIỀU KHOẢN VÀ CHÍNH SÁCH SỬ DỤNG ZENQ OFFICE", hFont, hBrush, 14, curY);
                curY += 22f;
            }

            // Sections
            using (Font secFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            using (Font bodyFont = new Font("Segoe UI", 8.6f, FontStyle.Regular))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (SolidBrush bodyBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawString("1. Bản quyền và mục đích sử dụng", secFont, secBrush, 14, curY);
                curY += 18f;

                string b1 = "ZENQ OFFICE là bộ giải pháp phần mềm văn phòng thông minh được phát triển bởi NEXTGENEDUSPACE STUDIO nhằm mang lại công cụ xử lý văn bản, bảng tính, trình chiếu và tích hợp trí tuệ nhân tạo toàn diện cho người dùng.";
                SizeF sz1 = g.MeasureString(b1, bodyFont, (int)textW);
                g.DrawString(b1, bodyFont, bodyBrush, new RectangleF(14, curY, textW, sz1.Height));
                curY += sz1.Height + 10f;

                g.DrawString("2. Quyền riêng tư và bảo mật dữ liệu", secFont, secBrush, 14, curY);
                curY += 18f;

                string b2 = "Toàn bộ tài liệu và nội dung do bạn tạo lập đều được xử lý và lưu trữ trực tiếp trên thiết bị cá nhân. Chúng tôi cam kết tuyệt đối tôn trọng quyền riêng tư và không can thiệp trái phép vào thông tin cá nhân của người dùng.";
                SizeF sz2 = g.MeasureString(b2, bodyFont, (int)textW);
                g.DrawString(b2, bodyFont, bodyBrush, new RectangleF(14, curY, textW, sz2.Height));
                curY += sz2.Height + 10f;

                g.DrawString("3. Cam kết chất lượng dịch vụ", secFont, secBrush, 14, curY);
                curY += 18f;

                string b3 = "Ứng dụng liên tục được nâng cấp định kỳ nhằm mang lại hiệu suất tối ưu và trải nghiệm làm việc mượt mà nhất.";
                SizeF sz3 = g.MeasureString(b3, bodyFont, (int)textW);
                g.DrawString(b3, bodyFont, bodyBrush, new RectangleF(14, curY, textW, sz3.Height));
                curY += sz3.Height + 10f;
            }

            float totalH = curY + _scrollY;
            _maxScroll = Math.Max(0f, totalH - (Height - 20));

            g.Restore(state);

            // Slim Modern Scrollbar if needed
            if (_maxScroll > 0)
            {
                float trackH = Height - 24;
                float thumbH = Math.Max(24f, trackH * (Height / totalH));
                float thumbY = 12 + (_scrollY / _maxScroll) * (trackH - thumbH);

                using (GraphicsPath thumbPath = SetupForm.CreateRoundedRectanglePath(new RectangleF(Width - 10, thumbY, 5, thumbH), 2.5f))
                using (SolidBrush thumbBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    g.FillPath(thumbBrush, thumbPath);
                }
            }
        }
    }

    public class ModernTextBoxContainer : Panel
    {
        public TextBox InnerTextBox { get; private set; }
        private bool _isFocused = false;

        public ModernTextBoxContainer(string initialText)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            Padding = new Padding(10, 7, 10, 7);

            InnerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                Text = initialText,
                Dock = DockStyle.Fill
            };

            InnerTextBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            InnerTextBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };

            Controls.Add(InnerTextBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = SetupForm.CreateRoundedRectanglePath(rect, 6f))
            using (SolidBrush bg = new SolidBrush(BackColor))
            using (Pen border = new Pen(_isFocused ? Color.FromArgb(59, 130, 246) : Color.FromArgb(203, 213, 225), _isFocused ? 1.5f : 1.0f))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }
        }
    }

    #endregion

    public class SetupForm : Form
    {
        private SetupStep _currentStep = SetupStep.TermsAgreement;
        private System.Windows.Forms.Timer _animTimer;
        private Stopwatch _stopwatch;
        private float _progress = 0.05f;
        private float _targetProgress = 0.05f;
        private string _statusText = "Chuẩn bị cài đặt...";
        private bool _isCompleted = false;
        private bool _isDragging = false;
        private Point _dragStart;

        // Step 1 Controls
        private ModernTermsPanel _panelTerms;
        private ModernRoundedCheckBox _chkAgree;
        private ModernRoundedButton _btnStep1Next;

        // Step 2 Controls
        private Label _lblPath;
        private ModernTextBoxContainer _txtPathContainer;
        private ModernRoundedButton _btnBrowse;
        private ModernRoundedCheckBox _chkDesktopShortcut;
        private ModernRoundedCheckBox _chkStartMenuShortcut;
        private ModernRoundedCheckBox _chkCleanInstall;
        private ModernRoundedButton _btnStep2Back;
        private ModernRoundedButton _btnStep2Install;

        // Header Close Button
        private Button _btnClose;

        public SetupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(560, 480);
            BackColor = Color.White;
            DoubleBuffered = true;
            ShowInTaskbar = true;
            ShowIcon = true;
            Text = "Cài đặt ZENQ OFFICE";

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream icoStream = asm.GetManifestResourceStream("app.ico"))
                {
                    if (icoStream != null) Icon = new Icon(icoStream);
                    else if (File.Exists("ZenQOffice.ico")) Icon = new Icon("ZenQOffice.ico");
                    else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            using (GraphicsPath path = CreateRoundedRectanglePath(new RectangleF(0, 0, Width, Height), 16f))
            {
                Region = new Region(path);
            }

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDragging = true;
                    _dragStart = e.Location;
                }
            };
            MouseMove += (s, e) =>
            {
                if (_isDragging)
                {
                    Point cur = PointToScreen(e.Location);
                    Location = new Point(cur.X - _dragStart.X, cur.Y - _dragStart.Y);
                }
            };
            MouseUp += (s, e) => _isDragging = false;

            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            InitControls();
            SwitchToStep(SetupStep.TermsAgreement);

            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 16;
            _animTimer.Tick += (s, e) =>
            {
                if (_currentStep == SetupStep.Installing)
                {
                    if (_progress < _targetProgress)
                    {
                        _progress += (_targetProgress - _progress) * 0.12f;
                        if (Math.Abs(_targetProgress - _progress) < 0.005f) _progress = _targetProgress;
                    }

                    if (_isCompleted && _progress >= 0.99f && _stopwatch.ElapsedMilliseconds > 3000)
                    {
                        _animTimer.Stop();
                        LaunchAndExit();
                        return;
                    }
                }

                Invalidate();
            };
            _animTimer.Start();
        }

        private void InitControls()
        {
            // Close Button (✕)
            _btnClose = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                Location = new Point(Width - 40, 10),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(148, 163, 184),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => Application.Exit();
            Controls.Add(_btnClose);

            // ================= STEP 1: ĐIỀU KHOẢN =================
            _panelTerms = new ModernTermsPanel
            {
                Location = new Point(48, 246),
                Size = new Size(464, 130)
            };
            Controls.Add(_panelTerms);

            _chkAgree = new ModernRoundedCheckBox
            {
                Text = "Tôi đã đọc và đồng ý với điều khoản sử dụng của ZENQ OFFICE",
                Location = new Point(48, 386),
                Size = new Size(464, 24),
                Checked = true
            };
            _chkAgree.CheckedChanged += (s, e) =>
            {
                _btnStep1Next.Enabled = _chkAgree.Checked;
            };
            Controls.Add(_chkAgree);

            _btnStep1Next = new ModernRoundedButton
            {
                Text = "Tiếp tục →",
                Location = new Point(362, 422),
                Size = new Size(150, 40),
                BaseColor = Color.FromArgb(37, 99, 235),
                HoverColor = Color.FromArgb(29, 78, 216),
                PressedColor = Color.FromArgb(30, 64, 175)
            };
            _btnStep1Next.Click += (s, e) => SwitchToStep(SetupStep.InstallOptions);
            Controls.Add(_btnStep1Next);

            // ================= STEP 2: TÙY CHỌN CÀI ĐẶT =================
            _lblPath = new Label
            {
                Text = "Thư mục cài đặt:",
                Location = new Point(48, 246),
                Size = new Size(464, 20),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.Transparent
            };
            Controls.Add(_lblPath);

            _txtPathContainer = new ModernTextBoxContainer(ShortcutHelper.DefaultInstallPath)
            {
                Location = new Point(48, 268),
                Size = new Size(364, 32)
            };
            Controls.Add(_txtPathContainer);

            _btnBrowse = new ModernRoundedButton
            {
                Text = "Duyệt...",
                Location = new Point(420, 268),
                Size = new Size(92, 32),
                BaseColor = Color.FromArgb(241, 245, 249),
                HoverColor = Color.FromArgb(226, 232, 240),
                PressedColor = Color.FromArgb(203, 213, 225),
                BorderColor = Color.FromArgb(203, 213, 225),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BorderRadius = 6f
            };
            _btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Chọn thư mục cài đặt ZENQ OFFICE";
                    fbd.SelectedPath = _txtPathContainer.InnerTextBox.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        _txtPathContainer.InnerTextBox.Text = fbd.SelectedPath;
                    }
                }
            };
            Controls.Add(_btnBrowse);

            _chkDesktopShortcut = new ModernRoundedCheckBox
            {
                Text = "Tạo lối tắt ZENQ OFFICE trên màn hình nền Desktop",
                Location = new Point(48, 314),
                Size = new Size(464, 24),
                Checked = true
            };
            Controls.Add(_chkDesktopShortcut);

            _chkStartMenuShortcut = new ModernRoundedCheckBox
            {
                Text = "Thêm lối tắt ZENQ OFFICE vào Start Menu",
                Location = new Point(48, 344),
                Size = new Size(464, 24),
                Checked = true
            };
            Controls.Add(_chkStartMenuShortcut);

            _chkCleanInstall = new ModernRoundedCheckBox
            {
                Text = "Cài đặt mới hoàn toàn (Xóa sạch dữ liệu cũ và cấu hình trước đó)",
                Location = new Point(48, 374),
                Size = new Size(464, 24),
                Checked = false
            };
            Controls.Add(_chkCleanInstall);

            _btnStep2Back = new ModernRoundedButton
            {
                Text = "← Quay lại",
                Location = new Point(48, 422),
                Size = new Size(130, 40),
                BaseColor = Color.FromArgb(241, 245, 249),
                HoverColor = Color.FromArgb(226, 232, 240),
                PressedColor = Color.FromArgb(203, 213, 225),
                BorderColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BorderRadius = 8f
            };
            _btnStep2Back.Click += (s, e) => SwitchToStep(SetupStep.TermsAgreement);
            Controls.Add(_btnStep2Back);

            _btnStep2Install = new ModernRoundedButton
            {
                Text = "Cài đặt ngay",
                Location = new Point(362, 422),
                Size = new Size(150, 40),
                BaseColor = Color.FromArgb(16, 185, 129), // Emerald Green
                HoverColor = Color.FromArgb(5, 150, 105),
                PressedColor = Color.FromArgb(4, 120, 87),
                BorderRadius = 8f
            };
            _btnStep2Install.Click += (s, e) =>
            {
                SwitchToStep(SetupStep.Installing);
                StartInstallWorker();
            };
            Controls.Add(_btnStep2Install);
        }

        private void SwitchToStep(SetupStep step)
        {
            _currentStep = step;

            bool isStep1 = (step == SetupStep.TermsAgreement);
            bool isStep2 = (step == SetupStep.InstallOptions);
            bool isStep3 = (step == SetupStep.Installing);

            _panelTerms.Visible = isStep1;
            _chkAgree.Visible = isStep1;
            _btnStep1Next.Visible = isStep1;

            _lblPath.Visible = isStep2;
            _txtPathContainer.Visible = isStep2;
            _btnBrowse.Visible = isStep2;
            _chkDesktopShortcut.Visible = isStep2;
            _chkStartMenuShortcut.Visible = isStep2;
            _chkCleanInstall.Visible = isStep2;
            _btnStep2Back.Visible = isStep2;
            _btnStep2Install.Visible = isStep2;

            _btnClose.Enabled = !isStep3;

            Invalidate();
        }

        private void StartInstallWorker()
        {
            string chosenPath = _txtPathContainer.InnerTextBox.Text.Trim();
            if (string.IsNullOrEmpty(chosenPath)) chosenPath = ShortcutHelper.DefaultInstallPath;

            bool createDesktop = _chkDesktopShortcut.Checked;
            bool createStartMenu = _chkStartMenuShortcut.Checked;
            bool cleanInstall = _chkCleanInstall != null && _chkCleanInstall.Checked;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    // Terminate any running ZenQOffice / GenOffice instances to unlock binaries
                    try
                    {
                        foreach (var p in Process.GetProcessesByName("ZenQOffice"))
                        {
                            try { p.Kill(); p.WaitForExit(1500); } catch { }
                        }
                        foreach (var p in Process.GetProcessesByName("GenOffice"))
                        {
                            try { p.Kill(); p.WaitForExit(1500); } catch { }
                        }
                    }
                    catch { }

                    if (cleanInstall)
                    {
                        SetStatus("Đang dọn dẹp dữ liệu và cấu hình phiên bản cũ...", 0.10f);
                        ShortcutHelper.RemoveUserData();
                        Thread.Sleep(300);
                    }

                    if (!Directory.Exists(chosenPath)) Directory.CreateDirectory(chosenPath);

                    SetStatus("Chuẩn bị môi trường cài đặt...", 0.15f);
                    Thread.Sleep(400);

                    SetStatus("Đang giải nén tập tin ứng dụng ZENQ OFFICE...", 0.35f);

                    Assembly asm = Assembly.GetExecutingAssembly();
                    Stream payloadStream = asm.GetManifestResourceStream("payload.zip");

                    if (payloadStream != null)
                    {
                        using (ZipArchive zip = new ZipArchive(payloadStream, ZipArchiveMode.Read))
                        {
                            int totalEntries = zip.Entries.Count;
                            for (int i = 0; i < totalEntries; i++)
                            {
                                ZipArchiveEntry entry = zip.Entries[i];
                                string destPath = Path.Combine(chosenPath, entry.FullName);
                                string destFolder = Path.GetDirectoryName(destPath);
                                if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                                if (!string.IsNullOrEmpty(entry.Name))
                                {
                                    int retries = 3;
                                    while (retries > 0)
                                    {
                                        try
                                        {
                                            entry.ExtractToFile(destPath, true);
                                            break;
                                        }
                                        catch (IOException)
                                        {
                                            retries--;
                                            if (retries == 0) throw;
                                            Thread.Sleep(200);
                                        }
                                    }
                                }
                                float stepProg = 0.35f + ((float)(i + 1) / totalEntries) * 0.35f;
                                SetStatus(string.Format("Đang cài đặt: {0}...", entry.Name), stepProg);
                                Thread.Sleep(20);
                            }
                        }
                    }
                    else
                    {
                        string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                        string[] filesToCopy = new string[] {
                            "ZenQOffice.exe", "PdfSharp.dll", "PdfiumViewer.dll", "pdfium.dll", "ZenQOffice.ico", "ZenQOffice.png"
                        };

                        for (int i = 0; i < filesToCopy.Length; i++)
                            {
                            string f = filesToCopy[i];
                            string src = Path.Combine(currentDir, f);
                            if (File.Exists(src))
                            {
                                File.Copy(src, Path.Combine(chosenPath, f), true);
                            }
                            SetStatus(string.Format("Đang sao chép: {0}...", f), 0.35f + ((float)(i + 1) / filesToCopy.Length) * 0.35f);
                            Thread.Sleep(70);
                        }

                        CopyFolderIfExists(Path.Combine(currentDir, "x64"), Path.Combine(chosenPath, "x64"));
                        CopyFolderIfExists(Path.Combine(currentDir, "x86"), Path.Combine(chosenPath, "x86"));
                    }

                    // Generate Uninstall.exe
                    SetStatus("Đang thiết lập trình gỡ cài đặt...", 0.78f);
                    string uninstallExePath = Path.Combine(chosenPath, "Uninstall.exe");
                    try
                    {
                        File.Copy(Application.ExecutablePath, uninstallExePath, true);
                    }
                    catch { }
                    Thread.Sleep(250);

                    // Shortcuts & Registry
                    SetStatus("Đang thiết lập lối tắt và đăng ký hệ thống...", 0.88f);
                    string exePath = Path.Combine(chosenPath, "ZenQOffice.exe");
                    string icoPath = Path.Combine(chosenPath, "ZenQOffice.ico");

                    ShortcutHelper.CleanObsoleteShortcuts();

                    if (createDesktop)
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        ShortcutHelper.CreateShortcut(exePath, Path.Combine(desktop, "ZENQ OFFICE.lnk"), "ZENQ OFFICE", icoPath);
                    }

                    if (createStartMenu)
                    {
                        string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                        ShortcutHelper.CreateShortcut(exePath, Path.Combine(startMenu, "ZENQ OFFICE.lnk"), "ZENQ OFFICE", icoPath);
                    }

                    ShortcutHelper.RegisterUninstall(chosenPath);
                    Thread.Sleep(300);

                    SetStatus("Cài đặt hoàn tất! Đang khởi động ZENQ OFFICE...", 1.0f);
                    _isCompleted = true;
                }
                catch (Exception ex)
                {
                    SetStatus("Lỗi cài đặt: " + ex.Message, 1.0f);
                    _isCompleted = true;
                }
            });
        }

        private void CopyFolderIfExists(string src, string dest)
        {
            if (!Directory.Exists(src)) return;
            if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            foreach (string file in Directory.GetFiles(src))
            {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        private void SetStatus(string text, float targetProg)
        {
            _statusText = text;
            _targetProgress = targetProg;
        }

        private void LaunchAndExit()
        {
            try
            {
                string chosenPath = _txtPathContainer.InnerTextBox.Text.Trim();
                if (string.IsNullOrEmpty(chosenPath)) chosenPath = ShortcutHelper.DefaultInstallPath;
                string exePath = Path.Combine(chosenPath, "ZenQOffice.exe");
                if (File.Exists(exePath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(exePath);
                    psi.WorkingDirectory = chosenPath;
                    Process.Start(psi);
                }
            }
            catch { }

            Application.Exit();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            long elapsed = _stopwatch != null ? _stopwatch.ElapsedMilliseconds : 0;
            float timeSec = elapsed / 1000f;

            // 1. Nền Trắng Tinh Tế & Viền Bo Tròn Soft Border
            g.Clear(Color.White);
            using (GraphicsPath bPath = CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 16f))
            using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawPath(bPen, bPath);
            }

            // 2. VẼ BIỂU TƯỢNG MASCOT CỐ ĐỊNH CHÍNH GIỮA Ở ĐỈNH (VỊ TRÍ KHÔNG ĐỔI TRONG MỌI BƯỚC)
            float mascotX = Width / 2f;
            float mascotY = 105f;
            MascotRenderer.DrawMascot(g, mascotX, mascotY, 1.05f, timeSec, MascotMood.HappySmiling);

            // 3. TIÊU ĐỀ THEO TỪNG BƯỚC
            float textY = 196f;
            using (Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                string titleText = "Cài đặt ZENQ OFFICE";
                if (_currentStep == SetupStep.TermsAgreement)
                    titleText = "Chào mừng bạn đến với ZENQ OFFICE";
                else if (_currentStep == SetupStep.InstallOptions)
                    titleText = "Tùy chọn cài đặt ZENQ OFFICE";
                else if (_currentStep == SetupStep.Installing)
                    titleText = "Đang cài đặt ZENQ OFFICE...";

                g.DrawString(titleText, titleFont, titleB, new RectangleF(0, textY, Width, 30), sf);
            }

            // 4. TIẾN TRÌNH Ở BƯỚC 3 (INSTALLING)
            if (_currentStep == SetupStep.Installing)
            {
                float barW = 400f;
                float barH = 4.5f;
                float barX = (Width - barW) / 2f;
                float barY = 290f;

                RectangleF trackRect = new RectangleF(barX, barY, barW, barH);
                using (GraphicsPath trackPath = CreateRoundedRectanglePath(trackRect, barH / 2f))
                using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillPath(trackBg, trackPath);
                }

                float fillW = Math.Max(barH, barW * Math.Min(1.0f, Math.Max(0.02f, _progress)));
                RectangleF fillRect = new RectangleF(barX, barY, fillW, barH);
                using (GraphicsPath fillPath = CreateRoundedRectanglePath(fillRect, barH / 2f))
                using (LinearGradientBrush fillBg = new LinearGradientBrush(fillRect, Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), 0f))
                {
                    g.FillPath(fillBg, fillPath);
                }

                using (Font stFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
                using (SolidBrush stBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(_statusText, stFont, stBrush, new RectangleF(0, barY + 20, Width, 24), sf);
                }
            }
        }

        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

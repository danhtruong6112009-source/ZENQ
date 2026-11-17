using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Cửa sổ Đăng nhập / Đăng ký tài khoản ZenQ chuẩn Fluent Design Windows 11.
    /// Kiến trúc TableLayoutPanel 4 tầng đảm bảo Tabs luôn ở trên cùng, các ô nhập liệu
    /// không bao giờ bị đảo ngược hoặc che khuất, placeholder trực quan và tự động nhận diện email thông minh.
    /// </summary>
    public class AuthDialogForm : Form
    {
        private bool _isRegisterMode;

        // Shell & Header
        private TableLayoutPanel _shell;
        private Label _lblTitle;
        private Label _lblSubtitle;

        // Tab switchers (Windows 11 segment buttons)
        private SettingsActionButton _btnTabLogin;
        private SettingsActionButton _btnTabRegister;

        // Container panels for Login and Register
        private TableLayoutPanel _fieldsLogin;
        private TableLayoutPanel _fieldsRegister;

        // Login Controls
        private TextBox _txtLoginId;
        private TextBox _txtLoginPass;
        private SettingsActionButton _btnToggleLoginPass;
        private CheckBox _chkRememberMe;

        // Register Controls
        private TextBox _txtRegFullName;
        private TextBox _txtRegUser;
        private TextBox _txtRegEmail;
        private TextBox _txtRegPass;
        private SettingsActionButton _btnToggleRegPass;

        // Footer & Actions
        private Label _lblStatus;
        private SettingsActionButton _btnClose;
        private SettingsActionButton _btnSubmit;

        public AuthDialogForm(bool startWithRegister = false)
        {
            _isRegisterMode = startWithRegister;
            InitializeComponent();
            SetMode(_isRegisterMode);
        }

        private void InitializeComponent()
        {
            Text = AppL10n.T("Tài khoản ZenQ", "ZenQ Account");
            ClientSize = new Size(480, 520);
            MinimumSize = new Size(460, 440);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);

            // Shell: 
            // Row 0: Header (70px)
            // Row 1: Tab switcher (44px)
            // Row 2: Input fields (100% tự co giãn)
            // Row 3: Footer status & buttons (86px)
            _shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                BackColor = Color.White
            };
            _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            _shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            Controls.Add(_shell);

            // ── ROW 0: HEADER (Chuẩn typography Windows 11, không lộ thông tin kỹ thuật) ──
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 14, 28, 4),
                BackColor = Color.White
            };
            _lblSubtitle = new Label
            {
                Text = AppL10n.T("Đăng nhập để đồng bộ và truy cập kho mẫu slide đám mây.", "Sign in to sync and access the cloud template gallery."),
                Dock = DockStyle.Bottom,
                Height = 20,
                ForeColor = Color.FromArgb(100, 110, 125),
                Font = new Font("Segoe UI", 9f)
            };
            _lblTitle = new Label
            {
                Text = AppL10n.T("Tài khoản ZenQ", "ZenQ Account"),
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 37, 52)
            };
            header.Controls.Add(_lblSubtitle);
            header.Controls.Add(_lblTitle);
            _shell.Controls.Add(header, 0, 0);

            // ── ROW 1: TAB SWITCHER (Nút phân đoạn Đăng nhập / Đăng ký ở ngay trên cùng) ──
            var tabBarContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 4, 28, 6),
                BackColor = Color.White
            };
            var tabBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            tabBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tabBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _btnTabLogin = new SettingsActionButton
            {
                Text = AppL10n.T("Đăng nhập", "Sign In"),
                Dock = DockStyle.Fill,
                Primary = true,
                Margin = new Padding(0, 0, 4, 0),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold)
            };
            _btnTabLogin.Click += (s, e) => SetMode(false);

            _btnTabRegister = new SettingsActionButton
            {
                Text = AppL10n.T("Đăng ký tài khoản", "Create Account"),
                Dock = DockStyle.Fill,
                Primary = false,
                Margin = new Padding(4, 0, 0, 0),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold)
            };
            _btnTabRegister.Click += (s, e) => SetMode(true);

            tabBar.Controls.Add(_btnTabLogin, 0, 0);
            tabBar.Controls.Add(_btnTabRegister, 1, 0);
            tabBarContainer.Controls.Add(tabBar);
            _shell.Controls.Add(tabBarContainer, 0, 1);

            // ── ROW 2: BODY (Vùng cuộn chứa các ô nhập liệu) ──
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(28, 6, 28, 6),
                BackColor = Color.White
            };
            _shell.Controls.Add(body, 0, 2);

            // Helper tạo Group nhãn + khung nhập SettingsInputFrame
            Control CreateField(string labelText, Control inputControl, int inputH = 32)
            {
                var group = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = inputH + 28,
                    Margin = new Padding(0, 0, 0, 10)
                };
                var lbl = new Label
                {
                    Text = labelText,
                    Dock = DockStyle.Top,
                    Height = 22,
                    ForeColor = Color.FromArgb(65, 75, 90),
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular)
                };
                inputControl.Dock = DockStyle.Bottom;
                inputControl.Height = inputH;
                group.Controls.Add(lbl);
                group.Controls.Add(inputControl);
                return group;
            }

            // ── 1. BẢNG Ô NHẬP ĐĂNG NHẬP ──
            _fieldsLogin = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Margin = Padding.Empty,
                BackColor = Color.White
            };

            // Login Field 1: User / Email
            _txtLoginId = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.8f),
                PlaceholderText = AppL10n.T("Nhập tên đăng nhập hoặc địa chỉ email", "Enter username or email address")
            };
            var frameLoginId = new SettingsInputFrame(_txtLoginId);

            // Login Field 2: Password
            _txtLoginPass = new TextBox
            {
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 9.8f),
                PlaceholderText = AppL10n.T("Nhập mật khẩu của bạn", "Enter your password")
            };
            var frameLoginPass = new SettingsInputFrame(_txtLoginPass);
            _btnToggleLoginPass = new SettingsActionButton { Text = AppL10n.T("Hiện", "Show"), Size = new Size(50, 24), TabStop = false, Font = new Font("Segoe UI", 8.5f) };
            _btnToggleLoginPass.Click += (s, e) =>
            {
                _txtLoginPass.UseSystemPasswordChar = !_txtLoginPass.UseSystemPasswordChar;
                _btnToggleLoginPass.Text = _txtLoginPass.UseSystemPasswordChar ? AppL10n.T("Hiện", "Show") : AppL10n.T("Ẩn", "Hide");
            };
            frameLoginPass.Controls.Add(_btnToggleLoginPass);
            frameLoginPass.Padding = new Padding(10, 7, 58, 5);
            frameLoginPass.Layout += (s, e) => _btnToggleLoginPass.SetBounds(Math.Max(0, frameLoginPass.Width - 54), 4, 50, 24);

            _fieldsLogin.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fieldsLogin.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fieldsLogin.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fieldsLogin.Controls.Add(CreateField(AppL10n.T("Tên đăng nhập hoặc Email", "Username or Email"), frameLoginId), 0, 0);
            _fieldsLogin.Controls.Add(CreateField(AppL10n.T("Mật khẩu", "Password"), frameLoginPass), 0, 1);

            _chkRememberMe = new CheckBox
            {
                Text = AppL10n.T("Duy trì đăng nhập (không bị đăng xuất khi thoát ứng dụng)", "Keep me signed in"),
                Checked = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(2, 6, 2, 4),
                Cursor = Cursors.Hand
            };
            _fieldsLogin.Controls.Add(_chkRememberMe, 0, 2);

            // ── 2. BẢNG Ô NHẬP ĐĂNG KÝ (Thứ tự tự nhiên: Họ tên -> Tên đăng nhập -> Email -> Mật khẩu) ──
            _fieldsRegister = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Margin = Padding.Empty,
                BackColor = Color.White,
                Visible = false
            };

            // Reg Field 1: Full name
            _txtRegFullName = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.8f),
                PlaceholderText = AppL10n.T("Nhập họ và tên đầy đủ", "Enter full name")
            };
            var frameRegFullName = new SettingsInputFrame(_txtRegFullName);

            // Reg Field 2: Username
            _txtRegUser = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.8f),
                PlaceholderText = AppL10n.T("Tối thiểu 3 ký tự (viết liền, không dấu)", "Minimum 3 characters (letters/numbers)")
            };
            var frameRegUser = new SettingsInputFrame(_txtRegUser);

            // Reg Field 3: Email
            _txtRegEmail = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.8f),
                PlaceholderText = "example@domain.com"
            };
            var frameRegEmail = new SettingsInputFrame(_txtRegEmail);

            // Reg Field 4: Password
            _txtRegPass = new TextBox
            {
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 9.8f),
                PlaceholderText = AppL10n.T("Tối thiểu 6 ký tự", "Minimum 6 characters")
            };
            var frameRegPass = new SettingsInputFrame(_txtRegPass);
            _btnToggleRegPass = new SettingsActionButton { Text = AppL10n.T("Hiện", "Show"), Size = new Size(50, 24), TabStop = false, Font = new Font("Segoe UI", 8.5f) };
            _btnToggleRegPass.Click += (s, e) =>
            {
                _txtRegPass.UseSystemPasswordChar = !_txtRegPass.UseSystemPasswordChar;
                _btnToggleRegPass.Text = _txtRegPass.UseSystemPasswordChar ? AppL10n.T("Hiện", "Show") : AppL10n.T("Ẩn", "Hide");
            };
            frameRegPass.Controls.Add(_btnToggleRegPass);
            frameRegPass.Padding = new Padding(10, 7, 58, 5);
            frameRegPass.Layout += (s, e) => _btnToggleRegPass.SetBounds(Math.Max(0, frameRegPass.Width - 54), 4, 50, 24);

            _fieldsRegister.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fieldsRegister.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fieldsRegister.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _fieldsRegister.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Thêm các trường theo đúng thứ tự 0, 1, 2, 3 bằng TableLayoutPanel
            _fieldsRegister.Controls.Add(CreateField(AppL10n.T("Họ và tên hiển thị", "Display Name"), frameRegFullName), 0, 0);
            _fieldsRegister.Controls.Add(CreateField(AppL10n.T("Tên đăng nhập (username)", "Username"), frameRegUser), 0, 1);
            _fieldsRegister.Controls.Add(CreateField(AppL10n.T("Địa chỉ Email", "Email Address"), frameRegEmail), 0, 2);
            _fieldsRegister.Controls.Add(CreateField(AppL10n.T("Mật khẩu (tối thiểu 6 ký tự)", "Password (min 6 chars)"), frameRegPass), 0, 3);

            body.Controls.Add(_fieldsRegister);
            body.Controls.Add(_fieldsLogin);

            // ── ROW 3: FOOTER (Thông báo lỗi/thành công & Nút bấm) ──
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 6, 28, 14),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _shell.Controls.Add(footer, 0, 3);

            _lblStatus = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 9f),
                AutoEllipsis = true
            };
            footer.Controls.Add(_lblStatus);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                ColumnCount = 3,
                RowCount = 1
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Spacer
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // Đóng
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155)); // Submit

            _btnClose = new SettingsActionButton
            {
                Text = AppL10n.T("Đóng", "Close"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                DialogResult = DialogResult.Cancel
            };
            _btnClose.Click += (s, e) => Close();

            _btnSubmit = new SettingsActionButton
            {
                Text = AppL10n.T("Đăng nhập ngay", "Sign In"),
                Primary = true,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnSubmit.Click += async (s, e) => await HandleSubmitAsync();

            actions.Controls.Add(_btnClose, 1, 0);
            actions.Controls.Add(_btnSubmit, 2, 0);
            footer.Controls.Add(actions);

            CancelButton = _btnClose;
            AcceptButton = _btnSubmit;

            Shown += (s, e) =>
            {
                FluentUi.RoundWindow(this);
                if (_isRegisterMode) _txtRegFullName?.Focus();
                else _txtLoginId?.Focus();
            };
        }

        private void SetMode(bool registerMode)
        {
            _isRegisterMode = registerMode;
            _lblStatus.Text = "";

            if (_isRegisterMode)
            {
                _btnTabLogin.Primary = false;
                _btnTabRegister.Primary = true;
                _fieldsLogin.Visible = false;
                _fieldsRegister.Visible = true;
                _btnSubmit.Text = AppL10n.T("Tạo tài khoản ngay", "Create Account");
                _lblSubtitle.Text = AppL10n.T("Đăng ký tài khoản ZenQ để sử dụng đầy đủ các tính năng.", "Create a ZenQ account to unlock cloud templates and sync.");
                ClientSize = new Size(480, 560);
                _txtRegFullName.Focus();
            }
            else
            {
                _btnTabLogin.Primary = true;
                _btnTabRegister.Primary = false;
                _fieldsRegister.Visible = false;
                _fieldsLogin.Visible = true;
                _btnSubmit.Text = AppL10n.T("Đăng nhập ngay", "Sign In");
                _lblSubtitle.Text = AppL10n.T("Đăng nhập để đồng bộ và truy cập kho mẫu slide đám mây.", "Sign in to sync and access the cloud template gallery.");
                ClientSize = new Size(480, 440);
                _txtLoginId.Focus();
            }

            _btnTabLogin.Invalidate();
            _btnTabRegister.Invalidate();
        }

        private async Task HandleSubmitAsync()
        {
            if (_isRegisterMode)
            {
                await HandleRegisterAsync();
            }
            else
            {
                await HandleLoginAsync();
            }
        }

        private async Task HandleLoginAsync()
        {
            string id = _txtLoginId.Text.Trim();
            string pass = _txtLoginPass.Text;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pass))
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = AppL10n.T("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", "Please enter both username and password.");
                return;
            }

            _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
            _lblStatus.Text = AppL10n.T("Đang kiểm tra tài khoản...", "Verifying credentials...");
            _btnSubmit.Enabled = false;

            bool remember = _chkRememberMe?.Checked ?? true;
            var result = await DatabaseAuthService.LoginAsync(id, pass, remember);
            _btnSubmit.Enabled = true;

            if (result.Success)
            {
                _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                _lblStatus.Text = AppL10n.T($"Đăng nhập thành công! Xin chào, {result.User.FullName}", $"Signed in! Welcome, {result.User.FullName}");
                await Task.Delay(400);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = result.Error;
            }
        }

        private async Task HandleRegisterAsync()
        {
            string fn = _txtRegFullName.Text.Trim();
            string u = _txtRegUser.Text.Trim();
            string email = _txtRegEmail.Text.Trim();
            string pass = _txtRegPass.Text;

            // ── NHẬN DIỆN THÔNG MINH NẾU NGƯỜI DÙNG NHẬP NHẦM Ô ──
            // 1. Nếu ô email không có '@', nhưng ô Họ tên hoặc Tên đăng nhập có '@' -> Tự động tráo về ô email
            if (!email.Contains("@"))
            {
                if (fn.Contains("@") && fn.Contains("."))
                {
                    string tmp = email;
                    email = fn;
                    fn = tmp;
                }
                else if (u.Contains("@") && u.Contains("."))
                {
                    string tmp = email;
                    email = u;
                    u = tmp;
                }
            }

            // 2. Nếu tên đăng nhập rỗng nhưng có email -> Lấy phần trước '@' làm tên đăng nhập
            if (string.IsNullOrWhiteSpace(u) && email.Contains("@"))
            {
                u = email.Split('@')[0].Replace(".", "").Replace("-", "_");
            }

            // 3. Nếu họ và tên rỗng -> Dùng tên đăng nhập
            if (string.IsNullOrWhiteSpace(fn))
            {
                fn = u;
            }

            // 4. Nếu tên đăng nhập chứa khoảng trắng
            // -> Chuẩn hóa username an toàn, hoặc dùng tiền tố email
            if (u.Contains(" ") && email.Contains("@"))
            {
                // Giữ họ tên đầy đủ vào fn
                if (string.IsNullOrWhiteSpace(fn) || fn == u)
                {
                    fn = u;
                }
                // Tên đăng nhập gán bằng phần trước '@' của email cho gọn
                u = email.Split('@')[0].Replace(".", "").Replace("-", "_");
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = AppL10n.T("Địa chỉ Email không hợp lệ (ví dụ: example@domain.com).", "Invalid email address (e.g., example@domain.com).");
                _txtRegEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(u) || u.Length < 3)
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = AppL10n.T("Tên đăng nhập phải có tối thiểu 3 ký tự.", "Username must be at least 3 characters.");
                _txtRegUser.Focus();
                return;
            }

            if (string.IsNullOrEmpty(pass) || pass.Length < 6)
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = AppL10n.T("Mật khẩu phải có độ dài tối thiểu 6 ký tự.", "Password must be at least 6 characters.");
                _txtRegPass.Focus();
                return;
            }

            _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
            _lblStatus.Text = AppL10n.T("Đang tạo tài khoản...", "Creating account...");
            _btnSubmit.Enabled = false;

            var result = await DatabaseAuthService.RegisterAsync(u, email, pass, fn, "user");
            _btnSubmit.Enabled = true;

            if (result.Success)
            {
                _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                _lblStatus.Text = AppL10n.T($"Đăng ký thành công! Xin chào, {result.User.FullName ?? result.User.Username}", $"Account created! Welcome, {result.User.FullName ?? result.User.Username}");
                await Task.Delay(400);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = result.Error;
            }
        }
    }
}

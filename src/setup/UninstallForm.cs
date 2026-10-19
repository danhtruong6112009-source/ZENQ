using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ZenQOffice.Setup
{
    public class UninstallForm : Form
    {
        private System.Windows.Forms.Timer _animTimer;
        private Stopwatch _stopwatch;
        private float _progress = 0f;
        private float _targetProgress = 0f;
        private string _statusText = "";
        private bool _isUninstalling = false;
        private bool _isCompleted = false;
        private bool _isDragging = false;
        private Point _dragStart;

        private ModernRoundedButton _btnKeep;
        private ModernRoundedButton _btnUninstall;
        private ModernRoundedCheckBox _chkCleanUserData;
        private Button _btnClose;

        public UninstallForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(500, 420);
            BackColor = Color.FromArgb(248, 250, 252);
            DoubleBuffered = true;
            ShowInTaskbar = true;
            ShowIcon = true;
            Text = "Gỡ cài đặt ZENQ OFFICE";

            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
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

            using (GraphicsPath path = SetupForm.CreateRoundedRectanglePath(new RectangleF(0, 0, Width, Height), 16f))
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

            InitButtons();

            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 16;
            _animTimer.Tick += (s, e) =>
            {
                if (_progress < _targetProgress)
                {
                    _progress += (_targetProgress - _progress) * 0.12f;
                    if (Math.Abs(_targetProgress - _progress) < 0.005f) _progress = _targetProgress;
                }

                if (_isCompleted && _progress >= 0.99f)
                {
                    if (_stopwatch.ElapsedMilliseconds > 3500)
                    {
                        _animTimer.Stop();
                        Application.Exit();
                        return;
                    }
                }

                Invalidate();
            };
            _animTimer.Start();
        }

        private void InitButtons()
        {
            // Tùy chọn xóa sạch dữ liệu người dùng (Mặc định tích chọn để gỡ triệt để)
            _chkCleanUserData = new ModernRoundedCheckBox
            {
                Text = "Xóa sạch dữ liệu cá nhân, tệp nháp, lịch sử & cài đặt",
                Checked = true,
                Size = new Size(410, 24),
                Location = new Point(54, 280),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(_chkCleanUserData);

            // Nút Ở lại (Khuyến khích giữ lại app)
            _btnKeep = new ModernRoundedButton
            {
                Text = "Ở lại với ZENQ OFFICE",
                Size = new Size(184, 42),
                Location = new Point(78, 328),
                BaseColor = Color.FromArgb(16, 185, 129), // Emerald Green
                HoverColor = Color.FromArgb(5, 150, 105),
                PressedColor = Color.FromArgb(4, 120, 87),
                BorderRadius = 8f
            };
            _btnKeep.Click += (s, e) =>
            {
                Close();
            };
            Controls.Add(_btnKeep);

            // Nút Gỡ cài đặt
            _btnUninstall = new ModernRoundedButton
            {
                Text = "Gỡ cài đặt",
                Size = new Size(130, 42),
                Location = new Point(282, 328),
                BaseColor = Color.FromArgb(241, 245, 249),
                HoverColor = Color.FromArgb(254, 226, 226),
                PressedColor = Color.FromArgb(254, 202, 202),
                BorderColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(239, 68, 68), // Soft Red
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BorderRadius = 8f
            };
            _btnUninstall.Click += (s, e) =>
            {
                StartUninstall();
            };
            Controls.Add(_btnUninstall);

            // Nút Đóng (✕)
            _btnClose = new Button
            {
                Text = "✕",
                Size = new Size(28, 28),
                Location = new Point(Width - 36, 10),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(148, 163, 184),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);
        }

        private void StartUninstall()
        {
            if (_isUninstalling) return;
            _isUninstalling = true;

            bool cleanUserData = _chkCleanUserData != null && _chkCleanUserData.Checked;

            _btnKeep.Visible = false;
            _btnUninstall.Visible = false;
            if (_chkCleanUserData != null) _chkCleanUserData.Visible = false;
            _btnClose.Enabled = false;

            _stopwatch.Restart();

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SetStatus("Đang đóng các tiến trình ZENQ OFFICE...", 0.15f);
                    try
                    {
                        foreach (var p in Process.GetProcessesByName("ZenQOffice"))
                        {
                            try { p.Kill(); p.WaitForExit(1000); } catch { }
                        }
                        foreach (var p in Process.GetProcessesByName("GenOffice"))
                        {
                            try { p.Kill(); p.WaitForExit(1000); } catch { }
                        }
                    }
                    catch { }
                    Thread.Sleep(400);

                    SetStatus("Đang xóa lối tắt Desktop & Start Menu...", 0.35f);
                    ShortcutHelper.RemoveShortcuts();
                    Thread.Sleep(400);

                    SetStatus("Đang gỡ bỏ cấu hình đăng ký hệ thống...", 0.55f);
                    ShortcutHelper.UnregisterUninstall();
                    Thread.Sleep(400);

                    if (cleanUserData)
                    {
                        SetStatus("Đang xóa dữ liệu cá nhân, lịch sử & tệp nháp...", 0.75f);
                        ShortcutHelper.RemoveUserData();
                        Thread.Sleep(400);
                    }

                    SetStatus("Đang dọn dẹp tập tin ứng dụng...", 0.90f);
                    string installDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir) || installDir.TrimEnd('\\', '/').Equals(Path.GetTempPath().TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    {
                        installDir = ShortcutHelper.DefaultInstallPath;
                    }

                    string tempBat = Path.Combine(Path.GetTempPath(), "cleanup_ZenQOffice.bat");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("@echo off");
                    sb.AppendLine("timeout /t 1 /nobreak >nul");
                    sb.AppendLine(":wait_del");
                    sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", installDir));
                    if (!string.Equals(installDir, ShortcutHelper.DefaultInstallPath, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", ShortcutHelper.DefaultInstallPath));
                    }
                    if (cleanUserData)
                    {
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(appData, "GenOffice")));
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(appData, "ZenQOffice")));
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(localAppData, "GenOfficeDesktop")));
                        sb.AppendLine(string.Format("rmdir /s /q \"{0}\" >nul 2>&1", Path.Combine(localAppData, "ZenQOffice")));
                    }
                    sb.AppendLine(string.Format("if exist \"{0}\" (", installDir));
                    sb.AppendLine("    timeout /t 1 /nobreak >nul");
                    sb.AppendLine("    goto wait_del");
                    sb.AppendLine(")");
                    sb.AppendLine("(goto) 2>nul & del \"%~f0\"");

                    File.WriteAllText(tempBat, sb.ToString(), Encoding.Default);

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    psi.Arguments = string.Format("/c \"{0}\"", tempBat);
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = true;
                    Process.Start(psi);

                    SetStatus("Đã gỡ cài đặt hoàn tất và sạch sẽ. Tạm biệt và hẹn gặp lại bạn nhé!", 1.0f);
                    _isCompleted = true;
                }
                catch (Exception ex)
                {
                    SetStatus("Lỗi gỡ cài đặt: " + ex.Message, 1.0f);
                    _isCompleted = true;
                }
            });
        }

        private void SetStatus(string text, float targetProg)
        {
            _statusText = text;
            _targetProgress = targetProg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            long elapsed = _stopwatch != null ? _stopwatch.ElapsedMilliseconds : 0;
            float timeSec = elapsed / 1000f;

            // 1. Nền Xanh Xám Buồn & Viền Bo Tròn
            g.Clear(Color.FromArgb(248, 250, 252));
            using (GraphicsPath bPath = SetupForm.CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 16f))
            using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawPath(bPen, bPath);
            }

            // 2. VẼ BIỂU TƯỢNG NHÂN VẬT ZenQOffice BUỒN BÃ RƠI NƯỚC MẮT
            float mascotX = Width / 2f;
            float mascotY = 114f;
            MascotRenderer.DrawMascot(g, mascotX, mascotY, 1.12f, timeSec, MascotMood.SadCrying);

            // 3. TIÊU ĐỀ & MÔ TẢ TÂM TRẠNG
            float textY = 214f;
            using (Font titleFont = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (SolidBrush titleB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                string title = _isUninstalling ? "Đang gỡ cài đặt ZENQ OFFICE..." : "Bạn muốn gỡ cài đặt ZENQ OFFICE?";
                g.DrawString(title, titleFont, titleB, new RectangleF(0, textY, Width, 30), sf);
            }

            using (Font subFont = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush subB = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                string sub = _isUninstalling
                    ? "Đang dọn dẹp các tài nguyên khỏi máy tính của bạn..."
                    : "Chúng tôi sẽ rất nhớ bạn... Bạn có chắc chắn muốn chia tay không?";
                g.DrawString(sub, subFont, subB, new RectangleF(0, textY + 28, Width, 20), sf);
            }

            // 4. NẾU ĐANG GỠ CÀI ĐẶT: HIỆN THANH TIẾN TRÌNH
            if (_isUninstalling)
            {
                float barW = 340f;
                float barH = 4.0f;
                float barX = (Width - barW) / 2f;
                float barY = 308f;

                RectangleF trackRect = new RectangleF(barX, barY, barW, barH);
                using (GraphicsPath trackPath = SetupForm.CreateRoundedRectanglePath(trackRect, barH / 2f))
                using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    g.FillPath(trackBg, trackPath);
                }

                float fillW = Math.Max(barH, barW * Math.Min(1.0f, Math.Max(0.02f, _progress)));
                RectangleF fillRect = new RectangleF(barX, barY, fillW, barH);
                using (GraphicsPath fillPath = SetupForm.CreateRoundedRectanglePath(fillRect, barH / 2f))
                using (LinearGradientBrush fillBg = new LinearGradientBrush(fillRect, Color.FromArgb(239, 68, 68), Color.FromArgb(185, 28, 28), 0f))
                {
                    g.FillPath(fillBg, fillPath);
                }

                using (Font stFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
                using (SolidBrush stBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(_statusText, stFont, stBrush, new RectangleF(0, barY + 14, Width, 22), sf);
                }
            }
        }
    }
}

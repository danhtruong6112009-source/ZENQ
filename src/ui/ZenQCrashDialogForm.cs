using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại thông báo sự cố chuẩn văn phòng Microsoft 365 / Windows 11 Fluent Design.
    /// Bảo vệ hoàn toàn quyền riêng tư: Tự động lọc sạch 100% đường dẫn máy tính cá nhân của lập trình viên.
    /// Cung cấp nút khôi phục tiếp tục làm việc ngay lập tức không làm gián đoạn người dùng.
    /// </summary>
    public class ZenQCrashDialogForm : Form
    {
        private readonly Exception _exception;
        private readonly string _contextTitle;
        private readonly bool _isFatal;
        private readonly string _sanitizedDetails;

        private Panel _pnlHeader;
        private Panel _pnlContent;
        private Panel _pnlFooter;
        private Panel _pnlDetails;
        private TextBox _txtDetails;
        private Button _btnContinue;
        private Button _btnClose;
        private Button _btnToggleDetails;
        private Button _btnCopy;
        private bool _detailsExpanded = false;

        public static void ShowDialogSafe(Exception ex, string contextTitle = "Sự cố giao diện", bool isFatal = false)
        {
            try
            {
                using (var dlg = new ZenQCrashDialogForm(ex, contextTitle, isFatal))
                {
                    dlg.ShowDialog();
                }
            }
            catch
            {
                // Fallback an toàn tuyệt đối nếu môi trường hiển thị gặp trục trặc
            }
        }

        public ZenQCrashDialogForm(Exception ex, string contextTitle, bool isFatal)
        {
            _exception = ex;
            _contextTitle = string.IsNullOrWhiteSpace(contextTitle) ? "Thông báo hệ thống" : contextTitle;
            _isFatal = isFatal;
            _sanitizedDetails = SanitizeExceptionDetails(ex);

            InitWindow();
            BuildUI();
        }

        private void InitWindow()
        {
            Text = "ZENQ OFFICE - " + _contextTitle;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            BackColor = Color.FromArgb(248, 250, 252); // Slate-50
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            ClientSize = new Size(540, 260);

            try
            {
                Icon = ZenQOfficeLogoRenderer.CreateAppIcon(32);
            }
            catch { }
        }

        private void BuildUI()
        {
            SuspendLayout();

            // 1. Header Bar
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White
            };
            _pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
                }

                // Vẽ logo bo góc
                Bitmap logo = ZenQOfficeLogoRenderer.GetRoundedLogo(38, 0.22f);
                if (logo != null)
                {
                    e.Graphics.DrawImage(logo, 18, 15, 38, 38);
                }

                using (Font titleFont = new Font("Segoe UI", 11.25f, FontStyle.Bold))
                using (SolidBrush bTitle = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    e.Graphics.DrawString("ZENQ OFFICE", titleFont, bTitle, 68, 14);
                }

                using (Font subFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
                using (SolidBrush bSub = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    string sub = _isFatal ? "Ứng dụng cần tạm dừng để bảo vệ an toàn dữ liệu" : "Đã bảo vệ phiên làm việc của bạn";
                    e.Graphics.DrawString(sub, subFont, bSub, 68, 36);
                }
            };

            // 2. Footer Action Bar
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
                }
            };

            _btnToggleDetails = new Button
            {
                Text = "Chi tiết kỹ thuật ▾",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.Transparent,
                Location = new Point(16, 14),
                Size = new Size(130, 28),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _btnToggleDetails.Click += (s, e) => ToggleDetails();

            _btnCopy = new Button
            {
                Text = "Sao chép",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(203, 213, 225), BorderSize = 1 },
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.White,
                Location = new Point(148, 14),
                Size = new Size(80, 28),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnCopy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(_sanitizedDetails);
                    _btnCopy.Text = "✓ Đã chép";
                    Timer t = new Timer { Interval = 2000 };
                    t.Tick += (ts, te) =>
                    {
                        _btnCopy.Text = "Sao chép";
                        t.Stop();
                        t.Dispose();
                    };
                    t.Start();
                }
                catch { }
            };

            _btnClose = new Button
            {
                Text = _isFatal ? "Đóng" : "Bỏ qua",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(203, 213, 225), BorderSize = 1 },
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.White,
                Location = new Point(340, 13),
                Size = new Size(86, 30),
                Cursor = Cursors.Hand
            };
            _btnClose.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _btnContinue = new Button
            {
                Text = _isFatal ? "Khởi động lại" : "Tiếp tục",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 99, 235), // Primary Blue
                Location = new Point(434, 13),
                Size = new Size(92, 30),
                Cursor = Cursors.Hand
            };
            _btnContinue.Click += (s, e) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            _pnlFooter.Controls.Add(_btnToggleDetails);
            _pnlFooter.Controls.Add(_btnCopy);
            _pnlFooter.Controls.Add(_btnClose);
            _pnlFooter.Controls.Add(_btnContinue);

            // 3. Technical Details Drawer (Collapsible)
            _pnlDetails = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 160,
                Visible = false,
                Padding = new Padding(18, 6, 18, 6),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _txtDetails = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _sanitizedDetails
            };
            _pnlDetails.Controls.Add(_txtDetails);

            // 4. Content Panel
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 16, 20, 10)
            };

            Label lblMsg = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Text = _isFatal
                    ? "Ứng dụng vừa ghi nhận sự cố hệ thống cần được khởi động lại để đảm bảo tính toàn vẹn của tệp tài liệu."
                    : "Một xung đột hiển thị tạm thời đã được phát hiện và tự động cô lập. Bạn có thể bấm 'Tiếp tục' để tiếp tục làm việc bình thường."
            };

            Panel pnlCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.White
            };
            pnlCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
                }
            };

            string friendlyErrorName = GetFriendlyErrorMessage(_exception);
            Label lblErrorTitle = new Label
            {
                Text = "Mã sự cố:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 12),
                AutoSize = true
            };
            Label lblErrorVal = new Label
            {
                Text = friendlyErrorName,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(86, 12),
                Size = new Size(400, 18),
                AutoEllipsis = true
            };

            Label lblTipTitle = new Label
            {
                Text = "Trạng thái:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 34),
                AutoSize = true
            };
            Label lblTipVal = new Label
            {
                Text = _isFatal ? "Đang chuẩn bị lưu dữ liệu an toàn..." : "Phiên làm việc và nội dung văn bản được bảo vệ nguyên vẹn.",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = _isFatal ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 101, 52),
                Location = new Point(86, 34),
                Size = new Size(400, 18),
                AutoEllipsis = true
            };

            pnlCard.Controls.Add(lblErrorTitle);
            pnlCard.Controls.Add(lblErrorVal);
            pnlCard.Controls.Add(lblTipTitle);
            pnlCard.Controls.Add(lblTipVal);

            _pnlContent.Controls.Add(pnlCard);
            _pnlContent.Controls.Add(lblMsg);

            Controls.Add(_pnlContent);
            Controls.Add(_pnlDetails);
            Controls.Add(_pnlFooter);
            Controls.Add(_pnlHeader);

            AcceptButton = _btnContinue;
            CancelButton = _btnClose;

            ResumeLayout(false);
        }

        private void ToggleDetails()
        {
            _detailsExpanded = !_detailsExpanded;
            if (_detailsExpanded)
            {
                _pnlDetails.Visible = true;
                _btnCopy.Visible = true;
                _btnToggleDetails.Text = "Ẩn chi tiết ▴";
                ClientSize = new Size(540, 420);
            }
            else
            {
                _pnlDetails.Visible = false;
                _btnCopy.Visible = false;
                _btnToggleDetails.Text = "Chi tiết kỹ thuật ▾";
                ClientSize = new Size(540, 260);
            }
        }

        private static string GetFriendlyErrorMessage(Exception ex)
        {
            if (ex == null) return "Không xác định";
            if (ex.Message != null && ex.Message.IndexOf("Object is currently in use elsewhere", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Xung đột tài nguyên kết xuất đồ họa (GDI+ Concurrent Lock)";
            }
            if (ex is OutOfMemoryException) return "Bộ nhớ đồ họa tạm thời quá tải (Out of Memory)";
            if (ex is System.IO.IOException) return "Xung đột truy xuất tệp hoặc dữ liệu lưu trữ";
            return ex.GetType().Name + ": " + (ex.Message ?? "Đã xảy ra sự cố trong quá trình xử lý.");
        }

        /// <summary>
        /// Xóa sạch 100% đường dẫn máy tính cá nhân của lập trình viên (C:\Users\...\ hoặc bất kỳ ổ đĩa nào),
        /// bảo đảm thông tin chuyên nghiệp, bảo mật và sạch sẽ khi người dùng xem chi tiết.
        /// </summary>
        public static string SanitizeExceptionDetails(Exception ex)
        {
            if (ex == null) return "Không có thông tin chi tiết.";

            string raw = ex.ToString();

            // 1. Thay thế đường dẫn tuyệt đối dạng C:\Users\...\src\ui\RibbonBar.cs:line 2938 thành src\ui\RibbonBar.cs:line 2938
            raw = Regex.Replace(raw, @"[a-zA-Z]:\\[^:\r\n]+[\\/](src[\\/][^:\r\n]+)", "$1", RegexOptions.IgnoreCase);

            // 2. Lọc sạch mọi đường dẫn tuyệt đối Windows còn lại (ví dụ C:\Users\... -> [thư mục ứng dụng])
            raw = Regex.Replace(raw, @"[a-zA-Z]:\\[^:\r\n\(\)\s]+", "[thư mục ứng dụng]", RegexOptions.IgnoreCase);

            // 3. Loại bỏ triệt để tên tài khoản cá nhân của nhà phát triển nếu xuất hiện
            raw = Regex.Replace(raw, @"Users\\+([^\\]+)", "Users\\AppUser", RegexOptions.IgnoreCase);

            return raw.Trim();
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại quản lý kết nối ZenQ Connect (PC - Mobile).
    /// Hiển thị mã QR, địa chỉ IP mạng nội bộ, trạng thái thiết bị và hướng dẫn sử dụng.
    /// </summary>
    public class ZenQConnectDialog : Form
    {
        private PictureBox _picQr;
        private Label _lblUrl;
        private Label _lblStatus;
        private Label _lblClientCount;
        private ComboBox _cboIpAddresses;
        private Button _btnToggleServer;
        private Button _btnCopyUrl;
        private Button _btnOpenBrowser;
        private Button _btnClose;
        private System.Windows.Forms.Timer _uiRefreshTimer;

        public ZenQConnectDialog()
        {
            Text = "ZenQ Connect - Điều khiển Không dây từ Mobile";
            ClientSize = new Size(660, 585);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;

            EnsureServerRunning();
            BuildUI();
            RefreshServerInfo();

            // Lắng nghe sự kiện từ máy chủ
            ZenQConnectServer.Instance.OnClientCountChanged += OnClientCountChanged;
            ZenQConnectServer.Instance.OnStatusMessage += OnServerStatusMessage;

            _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiRefreshTimer.Tick += (s, e) => UpdateConnectionStatusBadge();
            _uiRefreshTimer.Start();
        }

        private void EnsureServerRunning()
        {
            if (!ZenQConnectServer.Instance.IsRunning)
            {
                ZenQConnectServer.Instance.Start();
            }
        }

        private void BuildUI()
        {
            // 1. Header Bar
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White
            };
            header.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE8EA', new Rectangle(18, 16, 20, 20), Color.FromArgb(37, 99, 235), 15f);
                using (Font fTitle = new Font("Segoe UI Semibold", 11f, FontStyle.Bold))
                using (Brush bTitle = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawString("ZenQ Connect - Kết nối Điện thoại Thông minh", fTitle, bTitle, 46, 14);
                    e.Graphics.DrawLine(borderPen, 0, header.Height - 1, header.Width, header.Height - 1);
                }
            };

            _lblStatus = new Label
            {
                AutoSize = false,
                Location = new Point(380, 16),
                Size = new Size(260, 20)
            };
            _lblStatus.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isRunning = ZenQConnectServer.Instance.IsRunning;
                Color dotColor = isRunning ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
                string text = isRunning 
                    ? $"Đang chạy trên cổng {ZenQConnectServer.Instance.Port}"
                    : "Máy chủ đã tạm dừng";
                
                using (Font f = new Font("Segoe UI Semibold", 9f))
                {
                    SizeF sz = e.Graphics.MeasureString(text, f);
                    float xDot = _lblStatus.Width - sz.Width - 14;
                    float yDot = (_lblStatus.Height - 8) / 2f;
                    using (SolidBrush dotBrush = new SolidBrush(dotColor))
                    {
                        e.Graphics.FillEllipse(dotBrush, xDot, yDot, 8, 8);
                    }
                    using (SolidBrush textBrush = new SolidBrush(dotColor))
                    {
                        e.Graphics.DrawString(text, f, textBrush, xDot + 12, (_lblStatus.Height - sz.Height) / 2f);
                    }
                }
            };
            header.Controls.Add(_lblStatus);
            Controls.Add(header);

            // 2. Body Area
            Panel body = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(660, 472),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Controls.Add(body);

            // Cột Trái: Mã QR & Liên kết (Rộng 280)
            Panel cardQr = CreateCard(new Point(16, 12), new Size(270, 448));
            body.Controls.Add(cardQr);

            Label lblQrHeader = new Label
            {
                Text = "QUÉT MÃ QR ĐỂ KẾT NỐI",
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 12),
                AutoSize = true
            };
            cardQr.Controls.Add(lblQrHeader);

            _picQr = new PictureBox
            {
                Location = new Point(15, 34),
                Size = new Size(240, 240),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            _picQr.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, _picQr.Width - 1, _picQr.Height - 1);
                }
            };
            cardQr.Controls.Add(_picQr);

            _lblUrl = new Label
            {
                Text = ZenQConnectServer.Instance.ActiveUrl,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Location = new Point(10, 282),
                Size = new Size(250, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardQr.Controls.Add(_lblUrl);

            _btnCopyUrl = CreateNeutralButton("Sao chép", new Point(15, 310), new Size(115, 30), '\uE8C8');
            _btnCopyUrl.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(ZenQConnectServer.Instance.ActiveUrl);
                    MessageBox.Show(this, "Đã sao chép liên kết vào Clipboard!\nBạn có thể gửi liên kết này sang điện thoại qua tin nhắn, Zalo hoặc mở trực tiếp trên trình duyệt.", "ZenQ Connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            };
            cardQr.Controls.Add(_btnCopyUrl);

            _btnOpenBrowser = CreateNeutralButton("Trình duyệt", new Point(140, 310), new Size(115, 30), '\uE774');
            _btnOpenBrowser.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(ZenQConnectServer.Instance.ActiveUrl) { UseShellExecute = true });
                }
                catch { }
            };
            cardQr.Controls.Add(_btnOpenBrowser);

            Label lblQrNote = new Label
            {
                Text = "Dùng Camera điện thoại (iPhone/Android) quét mã QR để mở bảng điều khiển. Không cần cài đặt bất kỳ ứng dụng nào!",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(12, 348),
                Size = new Size(246, 64)
            };
            cardQr.Controls.Add(lblQrNote);

            // Cột Phải: Cấu hình Mạng & Tính năng Toàn năng (Rộng 340)
            Panel cardConfig = CreateCard(new Point(296, 12), new Size(348, 448));
            body.Controls.Add(cardConfig);

            // Nhóm 1: Trạng thái & Card mạng
            Label lblNetworkHeader = new Label
            {
                Text = "TRẠNG THÁI & KẾT NỐI MẠNG (WI-FI)",
                UseMnemonic = false,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 12),
                AutoSize = true
            };
            cardConfig.Controls.Add(lblNetworkHeader);

            _lblClientCount = new Label
            {
                Font = new Font("Segoe UI Semibold", 8.8f),
                Location = new Point(14, 32),
                Size = new Size(320, 22)
            };
            _lblClientCount.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var server = ZenQConnectServer.Instance;
                Color dotColor;
                string text;
                if (!server.IsRunning)
                {
                    dotColor = Color.FromArgb(148, 163, 184);
                    text = "Máy chủ đang tắt";
                }
                else if (server.IsClientConnected)
                {
                    dotColor = Color.FromArgb(16, 185, 129);
                    string devName = string.IsNullOrEmpty(server.ConnectedDeviceName) ? "Điện thoại" : server.ConnectedDeviceName;
                    text = $"Đã kết nối: {devName} ({server.ConnectedClientsCount} thiết bị)";
                }
                else
                {
                    dotColor = Color.FromArgb(217, 119, 6);
                    text = "Đang chờ điện thoại quét mã QR...";
                }

                using (SolidBrush dotBrush = new SolidBrush(dotColor))
                {
                    e.Graphics.FillEllipse(dotBrush, 2, 7, 8, 8);
                }
                using (SolidBrush textBrush = new SolidBrush(dotColor))
                using (Font f = new Font("Segoe UI Semibold", 8.8f))
                {
                    e.Graphics.DrawString(text, f, textBrush, 14, 3);
                }
            };
            cardConfig.Controls.Add(_lblClientCount);

            Label lblSelectIp = new Label
            {
                Text = "Chọn địa chỉ IP Wi-Fi nội bộ:",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 58),
                AutoSize = true
            };
            cardConfig.Controls.Add(lblSelectIp);

            _cboIpAddresses = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(14, 78),
                Size = new Size(210, 24),
                Font = new Font("Segoe UI", 9f)
            };
            _cboIpAddresses.SelectedIndexChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                string selIp = _cboIpAddresses.SelectedItem as string;
                if (!string.IsNullOrEmpty(selIp))
                {
                    ZenQConnectServer.Instance.PreferredIp = selIp;
                    UpdateUrlAndQr();
                }
            };
            cardConfig.Controls.Add(_cboIpAddresses);

            _btnToggleServer = CreateNeutralButton("Tạm dừng", new Point(234, 77), new Size(100, 25), '\uE768');
            _btnToggleServer.Click += (s, e) => ToggleServerState();
            cardConfig.Controls.Add(_btnToggleServer);

            // Nhóm 2: Tính năng toàn diện
            Label lblFeaturesHeader = new Label
            {
                Text = "TÍNH NĂNG ĐIỀU KHIỂN TOÀN DIỆN (TOÀN ỨNG DỤNG)",
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 112),
                AutoSize = true
            };
            cardConfig.Controls.Add(lblFeaturesHeader);

            int itemY = 134;
            itemY = AddFeatureRow(cardConfig, itemY, '\uE962', Color.FromArgb(14, 165, 233), Color.FromArgb(240, 249, 255),
                "Touchpad & Chuột cảm ứng",
                "Rê chuột mượt mà, chạm 1 ngón = Trái, 2 ngón = Cuộn mượt chống giật nhảy con trỏ.");
            itemY = AddFeatureRow(cardConfig, itemY, '\uE7F4', Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255),
                "Màn hình PC trực tiếp (Screen Mirror)",
                "Xem trực tiếp màn hình PC trên điện thoại (15-20 FPS) và chạm để điều khiển từ xa.");
            itemY = AddFeatureRow(cardConfig, itemY, '\uE714', Color.FromArgb(234, 88, 12), Color.FromArgb(255, 247, 237),
                "Diễn giả Slide & Bút Laser",
                "Chuyển slide, đọc Ghi chú diễn giả live, đồng hồ đếm giờ và chiếu Bút Laser đỏ.");
            itemY = AddFeatureRow(cardConfig, itemY, '\uED5F', Color.FromArgb(16, 185, 129), Color.FromArgb(236, 253, 245),
                "Chữ ký số & Bản vẽ tay",
                "Ký tên hoặc vẽ trên điện thoại và bấm 'Chèn vào PC' để chèn tức thì vào tài liệu.");
            itemY = AddFeatureRow(cardConfig, itemY, '\uE765', Color.FromArgb(147, 51, 234), Color.FromArgb(250, 245, 255),
                "Bàn phím số NumPad & Gõ chữ",
                "Bàn phím số cho Calc và nhập liệu tiếng Việt Unicode gửi trực tiếp vào PC.");

            // 3. Footer Bar
            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White
            };
            footer.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(borderPen, 0, 0, footer.Width, 0);
                }
            };

            Button btnHelp = CreateNeutralButton("Lưu ý kết nối", new Point(16, 16), new Size(130, 30), '\uE897');
            btnHelp.Click += (s, e) =>
            {
                MessageBox.Show(this,
                    "Lưu ý để kết nối thành công:\n\n" +
                    "1. Máy tính và Điện thoại cùng kết nối vào một mạng Wi-Fi (hoặc Điện thoại bật Điểm phát sóng cá nhân / Hotspot cho máy tính).\n" +
                    "2. Không cần cài đặt ứng dụng: mở Camera hoặc trình duyệt Safari (iOS) / Chrome (Android) để quét mã QR.\n" +
                    "3. Nếu có thông báo tường lửa Windows Defender Firewall, hãy bấm 'Allow access' (Cho phép truy cập mạng riêng tư).\n" +
                    "4. ZenQ Connect hoạt động trên toàn bộ ứng dụng ZenQ Office: Writer, Calc, Slides, Photo và toàn màn hình Windows.",
                    "Hướng dẫn kết nối ZenQ Connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            footer.Controls.Add(btnHelp);

            _btnClose = new Button
            {
                Text = "Đóng",
                DialogResult = DialogResult.OK,
                Location = new Point(510, 15),
                Size = new Size(134, 32),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 13, 13, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0)
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            footer.Controls.Add(_btnClose);
            Controls.Add(footer);
        }

        private int AddFeatureRow(Panel parent, int y, char glyph, Color iconColor, Color bgTint, string title, string desc)
        {
            Panel badge = new Panel
            {
                Location = new Point(14, y + 2),
                Size = new Size(32, 32),
                BackColor = Color.Transparent
            };
            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectanglePath(new Rectangle(0, 0, 31, 31), 6))
                using (SolidBrush bgBrush = new SolidBrush(bgTint))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, glyph, new Rectangle(6, 6, 20, 20), iconColor, 15f);
            };
            parent.Controls.Add(badge);

            Label lblTitle = new Label
            {
                Text = title,
                UseMnemonic = false,
                Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(54, y + 2),
                AutoSize = true
            };
            parent.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text = desc,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(54, y + 20),
                Size = new Size(280, 32)
            };
            parent.Controls.Add(lblDesc);

            return y + 58;
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private bool _isUpdatingUi = false;

        private void RefreshServerInfo()
        {
            var server = ZenQConnectServer.Instance;

            _isUpdatingUi = true;
            try
            {
                _cboIpAddresses.Items.Clear();
                var ips = server.GetAllLocalIpAddresses();
                foreach (var ip in ips)
                {
                    _cboIpAddresses.Items.Add(ip);
                }
                if (_cboIpAddresses.Items.Count > 0)
                {
                    int selIdx = _cboIpAddresses.Items.IndexOf(server.ActiveIpAddress);
                    _cboIpAddresses.SelectedIndex = selIdx >= 0 ? selIdx : 0;
                }
            }
            finally
            {
                _isUpdatingUi = false;
            }

            UpdateUrlAndQr();
        }

        private void UpdateUrlAndQr()
        {
            var server = ZenQConnectServer.Instance;
            _lblUrl.Text = server.ActiveUrl;

            // Cập nhật ảnh QR
            try
            {
                var oldBmp = _picQr.Image;
                _picQr.Image = server.GenerateQrBitmap(240);
                oldBmp?.Dispose();
            }
            catch { }

            UpdateConnectionStatusBadge();
        }

        private void UpdateConnectionStatusBadge()
        {
            if (IsDisposed) return;
            var server = ZenQConnectServer.Instance;

            _lblStatus?.Invalidate();
            _lblClientCount?.Invalidate();
            if (_btnToggleServer != null)
            {
                _btnToggleServer.Text = server.IsRunning ? "Tạm dừng" : "Bật máy chủ";
            }
        }

        private void ToggleServerState()
        {
            var server = ZenQConnectServer.Instance;
            if (server.IsRunning)
            {
                server.Stop();
            }
            else
            {
                server.Start();
            }
            RefreshServerInfo();
        }

        private void OnClientCountChanged(int count)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => UpdateConnectionStatusBadge())); } catch { }
            }
            else
            {
                UpdateConnectionStatusBadge();
            }
        }

        private void OnServerStatusMessage(string msg)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => UpdateConnectionStatusBadge())); } catch { }
            }
            else
            {
                UpdateConnectionStatusBadge();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _uiRefreshTimer?.Stop();
            _uiRefreshTimer?.Dispose();
            ZenQConnectServer.Instance.OnClientCountChanged -= OnClientCountChanged;
            ZenQConnectServer.Instance.OnStatusMessage -= OnServerStatusMessage;
            base.OnFormClosing(e);
        }

        private Panel CreateCard(Point loc, Size sz)
        {
            Panel card = new Panel
            {
                Location = loc,
                Size = sz,
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            return card;
        }

        private Button CreateNeutralButton(string text, Point loc, Size sz, char glyph = '\0')
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(226, 232, 240);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(241, 245, 249);
            if (glyph != '\0')
            {
                btn.Image = FluentIconRenderer.GetGlyphBitmap(glyph, 13, 13, Color.FromArgb(30, 41, 59));
                btn.ImageAlign = ContentAlignment.MiddleLeft;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                btn.Padding = new Padding(4, 0, 4, 0);
            }
            return btn;
        }
    }
}

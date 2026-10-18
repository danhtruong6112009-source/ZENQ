using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class UpdateDialogForm : Form
    {
        private ReleaseInfo _releaseInfo;
        private Label _lblStatus;
        private Label _lblCurrentVersion;
        private Label _lblNewVersion;
        private ProgressBar _progressBar;
        private Label _lblProgress;
        private Button _btnUpdate;
        private Button _btnCheckAgain;
        private Button _btnClose;
        private bool _isDownloading = false;

        public UpdateDialogForm(ReleaseInfo initialInfo = null)
        {
            _releaseInfo = initialInfo;

            Text = "Cập nhật ứng dụng ZENQ OFFICE";
            Size = new Size(500, 290);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            try
            {
                Icon = GenOfficeLogoRenderer.CreateAppIcon(32);
            }
            catch { }

            InitComponents();

            if (_releaseInfo == null)
            {
                RunCheck();
            }
            else
            {
                UpdateUIWithReleaseInfo(_releaseInfo);
            }
        }

        private void InitComponents()
        {
            // 1. Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(borderPen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }

                // Official Logo
                GenOfficeLogoRenderer.DrawLogo(e.Graphics, 16, 12, 40);
            };

            Label lblTitle = new Label
            {
                Text = "Cập nhật Phần mềm ZENQ OFFICE",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(66, 12),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubTitle = new Label
            {
                Text = "Hệ thống kiểm tra và tải bản cập nhật tự động",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(68, 36),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblSubTitle);

            Controls.Add(pnlHeader);

            // 2. Version Card Box
            Panel pnlVersionCard = new Panel
            {
                Location = new Point(20, 78),
                Size = new Size(444, 72),
                BackColor = Color.White
            };
            pnlVersionCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, pnlVersionCard.Width - 1, pnlVersionCard.Height - 1);
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawRectangle(p, r);
                }
            };

            Label lblCurTitle = new Label
            {
                Text = "Phiên bản hiện tại:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 12),
                AutoSize = true
            };
            pnlVersionCard.Controls.Add(lblCurTitle);

            _lblCurrentVersion = new Label
            {
                Text = UpdateManager.CURRENT_VERSION_TAG,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 34),
                AutoSize = true
            };
            pnlVersionCard.Controls.Add(_lblCurrentVersion);

            Label lblArrow = new Label
            {
                Text = "➔",
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(160, 22),
                AutoSize = true
            };
            pnlVersionCard.Controls.Add(lblArrow);

            Label lblNewTitle = new Label
            {
                Text = "Phiên bản mới nhất:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(210, 12),
                AutoSize = true
            };
            pnlVersionCard.Controls.Add(lblNewTitle);

            _lblNewVersion = new Label
            {
                Text = "Đang kiểm tra...",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(2, 132, 199),
                Location = new Point(210, 34),
                AutoSize = true
            };
            pnlVersionCard.Controls.Add(_lblNewVersion);

            Controls.Add(pnlVersionCard);

            // 3. Status Label
            _lblStatus = new Label
            {
                Text = "Đang kết nối đến máy chủ cập nhật...",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 158),
                Size = new Size(444, 22),
                UseMnemonic = false
            };
            Controls.Add(_lblStatus);

            // 4. Progress Bar & Label
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 160),
                Size = new Size(444, 12),
                Visible = false
            };
            Controls.Add(_progressBar);

            _lblProgress = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 174),
                Size = new Size(444, 18),
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false,
                UseMnemonic = false
            };
            Controls.Add(_lblProgress);

            // 5. Bottom Action Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(borderPen, 0, 0, pnlBottom.Width, 0);
                }
            };

            _btnCheckAgain = new Button
            {
                Text = "Kiểm tra lại",
                Size = new Size(115, 32),
                Location = new Point(20, 10),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE72C', 13, 13, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            _btnCheckAgain.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCheckAgain.Click += (s, e) => RunCheck();
            pnlBottom.Controls.Add(_btnCheckAgain);

            _btnUpdate = new Button
            {
                Text = "Cập nhật ngay",
                Size = new Size(135, 32),
                Location = new Point(235, 10),
                BackColor = Color.FromArgb(210, 71, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE896', 14, 14, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            _btnUpdate.FlatAppearance.BorderSize = 0;
            _btnUpdate.Click += (s, e) => StartDownloadAndInstall();
            pnlBottom.Controls.Add(_btnUpdate);

            _btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(95, 32),
                Location = new Point(379, 10),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 13, 13, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0)
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnClose.Click += (s, e) =>
            {
                if (_isDownloading)
                {
                    CancelDownload();
                    return;
                }
                Close();
            };
            pnlBottom.Controls.Add(_btnClose);

            Controls.Add(pnlBottom);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDownloading)
            {
                CancelDownload();
            }
            base.OnFormClosing(e);
        }

        private void CancelDownload()
        {
            if (!_isDownloading) return;
            _isDownloading = false;
            UpdateManager.CancelActiveDownload();
            _progressBar.Visible = false;
            _lblProgress.Visible = false;
            _lblStatus.Visible = true;
            _lblStatus.Text = "ℹ️ Đã dừng tải bản cập nhật.";
            _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
            _btnUpdate.Enabled = true;
            _btnCheckAgain.Enabled = true;
            _btnClose.Text = "Đóng";
            _btnClose.Enabled = true;
        }

        private void RunCheck()
        {
            _btnCheckAgain.Enabled = false;
            _btnUpdate.Enabled = false;
            _lblStatus.Visible = true;
            _lblStatus.Text = "Đang kiểm tra phiên bản mới...";
            _lblStatus.ForeColor = Color.FromArgb(71, 85, 105);
            _lblNewVersion.Text = "Đang kiểm tra...";
            _lblNewVersion.ForeColor = Color.FromArgb(2, 132, 199);
            _progressBar.Visible = false;
            _lblProgress.Visible = false;

            UpdateManager.CheckForUpdateAsync((info) =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed) return;
                        _btnCheckAgain.Enabled = true;
                        _releaseInfo = info;
                        UpdateUIWithReleaseInfo(info);
                    });
                }
                catch { }
            });
        }

        private void UpdateUIWithReleaseInfo(ReleaseInfo info)
        {
            if (info == null) return;

            if (info.HasUpdate)
            {
                string newVer = string.IsNullOrEmpty(info.TagName) ? "Bản mới" : info.TagName;
                _lblNewVersion.Text = newVer;
                _lblNewVersion.ForeColor = Color.FromArgb(16, 185, 129); // Emerald green
                _lblStatus.Text = "✨ Đã có bản cập nhật mới (" + newVer + ") sẵn sàng tải xuống!";
                _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                _btnUpdate.Enabled = true;
                _btnUpdate.Text = "Cập nhật ngay";
                _btnUpdate.BackColor = Color.FromArgb(210, 71, 38);
            }
            else if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                _lblNewVersion.Text = UpdateManager.CURRENT_VERSION_TAG;
                _lblNewVersion.ForeColor = Color.FromArgb(71, 85, 105);
                _lblStatus.Text = "ℹ️ " + info.ErrorMessage;
                _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
                _btnUpdate.Enabled = false;
                _btnUpdate.Text = "Đã mới nhất";
                _btnUpdate.BackColor = Color.FromArgb(148, 163, 184);
            }
            else
            {
                _lblNewVersion.Text = string.IsNullOrEmpty(info.TagName) ? UpdateManager.CURRENT_VERSION_TAG : info.TagName;
                _lblNewVersion.ForeColor = Color.FromArgb(16, 185, 129);
                _lblStatus.Text = "✅ Ứng dụng của bạn đang ở phiên bản mới nhất (" + _lblNewVersion.Text + ").";
                _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                _btnUpdate.Enabled = false;
                _btnUpdate.Text = "Đã mới nhất";
                _btnUpdate.BackColor = Color.FromArgb(148, 163, 184);
            }
        }

        private void StartDownloadAndInstall()
        {
            if (_releaseInfo == null || _isDownloading) return;
            _isDownloading = true;

            _btnUpdate.Enabled = false;
            _btnCheckAgain.Enabled = false;
            _btnClose.Enabled = true;
            _btnClose.Text = "Hủy";
            _lblStatus.Visible = false;
            _progressBar.Visible = true;
            _progressBar.Value = 0;
            _lblProgress.Visible = true;
            _lblProgress.Text = "Đang bắt đầu tải xuống...";

            UpdateManager.DownloadAndApplyUpdate(_releaseInfo,
                (percent, currentBytes, totalBytes) =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            _progressBar.Value = Math.Max(0, Math.Min(100, percent));
                            double curMb = currentBytes / (1024.0 * 1024.0);
                            double totMb = totalBytes / (1024.0 * 1024.0);
                            _lblProgress.Text = string.Format("{0}% ({1:0.1} MB / {2:0.1} MB)", percent, curMb, totMb);
                        });
                    }
                    catch { }
                },
                () =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            _lblProgress.Text = "🎉 Tải xong! Đang cài đặt bản cập nhật...";
                            _lblProgress.ForeColor = Color.FromArgb(16, 185, 129);
                        });
                    }
                    catch { }
                },
                (errorMsg) =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            _isDownloading = false;
                            _btnUpdate.Enabled = true;
                            _btnCheckAgain.Enabled = true;
                            _btnClose.Enabled = true;
                            _btnClose.Text = "Đóng";
                            _progressBar.Visible = false;
                            _lblProgress.Visible = false;
                            _lblStatus.Visible = true;
                            _lblStatus.Text = "❌ " + errorMsg;
                            _lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                            MessageBox.Show(this, errorMsg, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    catch { }
                });
        }
    }
}

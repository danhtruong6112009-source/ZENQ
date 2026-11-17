using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class PeripheralHubDialog : Form
    {
        private CheckBox _chkTouchMode;
        private CheckBox _chkAutoPenInk;
        private CheckBox _chkPenPressure;
        private CheckBox _chkAutoTouchKbd;
        private CheckBox _chkBarcodeMode;
        private CheckBox _chkAutoNotify;

        private ComboBox _cboUsbDrives;
        private ListBox _lstUsbFiles;
        private Label _lblHardwareStatus;
        private Label _lblMonitorStatus;

        public string SelectedUsbFileToOpen { get; private set; }
        public Bitmap ScannedDocumentBitmapToInsert { get; private set; }
        public bool RequestStartPresenterView { get; private set; }
        public Func<string> GetCurrentDocumentPathCallback { get; set; }

        public PeripheralHubDialog(Func<string> getCurrentDocPathCallback = null)
        {
            GetCurrentDocumentPathCallback = getCurrentDocPathCallback;
            Text = ZenQOffice.Core.AppL10n.T("Thiết bị & Cảm ứng", "Devices & Touch");
            ClientSize = new Size(640, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;

            BuildUI();
            RefreshDeviceLists();
        }

        private void BuildUI()
        {
            var s = AppSettings.Instance;

            // Header đơn giản, sáng sủa theo chuẩn Windows 11 Settings
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.White
            };
            header.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE772', new Rectangle(18, 15, 18, 18), Color.FromArgb(51, 65, 85), 13.5f);
                using (Font fTitle = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold))
                using (Brush bTitle = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawString(ZenQOffice.Core.AppL10n.T("Thiết bị & Cảm ứng", "Devices & Touch"), fTitle, bTitle, 42, 13);
                    e.Graphics.DrawLine(borderPen, 0, header.Height - 1, header.Width, header.Height - 1);
                }
            };

            _lblHardwareStatus = new Label
            {
                Text = PeripheralDeviceManager.GetDigitizerCapabilitiesSummary(),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(250, 14),
                Size = new Size(372, 20),
                UseMnemonic = false
            };
            header.Controls.Add(_lblHardwareStatus);
            Controls.Add(header);

            Panel body = new Panel
            {
                Location = new Point(0, 48),
                Size = new Size(640, 442),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Controls.Add(body);

            // 1. Cảm ứng & Bút vẽ
            Panel cardTouch = CreateSectionCard(ZenQOffice.Core.AppL10n.T("Cảm ứng & Bút vẽ", "Touch & Stylus"), '\uED5F', new Point(16, 12), new Size(608, 104));
            body.Controls.Add(cardTouch);

            Button btnOpenOsk = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Bàn phím ảo", "Touch Keyboard"), new Point(356, 8), new Size(105, 25), '\uE765');
            btnOpenOsk.Click += (sender, e) => PeripheralDeviceManager.ToggleTouchKeyboard();
            cardTouch.Controls.Add(btnOpenOsk);

            Button btnOpenConnect = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Kết nối Mobile", "ZenQ Connect"), new Point(468, 8), new Size(126, 25), '\uE8EA');
            btnOpenConnect.Click += (sender, e) =>
            {
                using (var dlg = new ZenQConnectDialog())
                {
                    dlg.ShowDialog(this);
                }
            };
            cardTouch.Controls.Add(btnOpenConnect);

            _chkTouchMode = CreateCheck(ZenQOffice.Core.AppL10n.T("Chế độ cảm ứng (nút lớn & thanh nổi)", "Touch Mode (larger targets & floating bar)"), new Point(16, 40), new Size(284, 24), s.TouchModeEnabled);
            _chkAutoPenInk = CreateCheck(ZenQOffice.Core.AppL10n.T("Tự động bật Bút vẽ khi chạm bút", "Auto-enable Ink on Stylus touch"), new Point(312, 40), new Size(280, 24), s.AutoSwitchPenInk);
            _chkPenPressure = CreateCheck(ZenQOffice.Core.AppL10n.T("Nhận lực nhấn & xoay bút để tẩy", "Pen pressure & eraser tilt support"), new Point(16, 68), new Size(284, 24), s.PenPressureSensitivity);
            _chkAutoTouchKbd = CreateCheck(ZenQOffice.Core.AppL10n.T("Tự động mở bàn phím ảo khi nhập liệu", "Auto-show Touch Keyboard on text input"), new Point(312, 68), new Size(280, 24), s.AutoSummonTouchKeyboard);

            cardTouch.Controls.Add(_chkTouchMode);
            cardTouch.Controls.Add(_chkAutoPenInk);
            cardTouch.Controls.Add(_chkPenPressure);
            cardTouch.Controls.Add(_chkAutoTouchKbd);

            // 2. Màn hình & Trình chiếu
            Panel cardMonitor = CreateSectionCard(ZenQOffice.Core.AppL10n.T("Màn hình & Trình chiếu", "Displays & Projection"), '\uE7F4', new Point(16, 126), new Size(608, 80));
            body.Controls.Add(cardMonitor);

            _lblMonitorStatus = new Label
            {
                Text = ZenQOffice.Core.AppL10n.T("Đang kiểm tra...", "Detecting..."),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(200, 10),
                Size = new Size(392, 20),
                UseMnemonic = false
            };
            cardMonitor.Controls.Add(_lblMonitorStatus);

            Button btnProjExtend = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Mở rộng", "Extend"), new Point(16, 40), new Size(134, 28));
            btnProjExtend.Click += (sender, e) => PeripheralDeviceManager.SetDisplayProjectionMode("/extend");
            cardMonitor.Controls.Add(btnProjExtend);

            Button btnProjClone = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Nhân đôi", "Duplicate"), new Point(158, 40), new Size(134, 28));
            btnProjClone.Click += (sender, e) => PeripheralDeviceManager.SetDisplayProjectionMode("/clone");
            cardMonitor.Controls.Add(btnProjClone);

            Button btnProjInternal = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Chỉ màn hình chính", "PC Screen Only"), new Point(300, 40), new Size(146, 28));
            btnProjInternal.Click += (sender, e) => PeripheralDeviceManager.SetDisplayProjectionMode("/internal");
            cardMonitor.Controls.Add(btnProjInternal);

            Button btnPresenter = CreateNeutralButton("Presenter View", new Point(454, 40), new Size(138, 28));
            btnPresenter.Click += (sender, e) =>
            {
                SaveSettingsFromUI();
                RequestStartPresenterView = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            cardMonitor.Controls.Add(btnPresenter);

            // 3. Ổ đĩa USB
            Panel cardUsb = CreateSectionCard(ZenQOffice.Core.AppL10n.T("Ổ đĩa USB", "USB Drives"), '\uE88E', new Point(16, 216), new Size(608, 118));
            body.Controls.Add(cardUsb);

            _cboUsbDrives = new ZenQOffice.UI.FluentComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(16, 38),
                Size = new Size(286, 24)
            };
            _cboUsbDrives.SelectedIndexChanged += (sender, e) => RefreshUsbFilesList();
            cardUsb.Controls.Add(_cboUsbDrives);

            Button btnRefreshUsb = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Làm mới", "Refresh"), new Point(298, 36), new Size(92, 27), '\uE72C');
            btnRefreshUsb.Click += (sender, e) => RefreshDeviceLists();
            cardUsb.Controls.Add(btnRefreshUsb);

            Button btnBackupUsb = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Sao lưu", "Back Up"), new Point(396, 36), new Size(96, 27), '\uEA35');
            btnBackupUsb.Click += (sender, e) => DoBackupToSelectedUsb();
            cardUsb.Controls.Add(btnBackupUsb);

            Button btnOpenUsbFile = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Mở tệp", "Open File"), new Point(498, 36), new Size(94, 27), '\uE838');
            btnOpenUsbFile.Click += (sender, e) => OpenSelectedUsbFile();
            cardUsb.Controls.Add(btnOpenUsbFile);

            _lstUsbFiles = new ZenQOffice.UI.FluentListBox
            {
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(51, 65, 85),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 68),
                Size = new Size(576, 38)
            };
            _lstUsbFiles.DoubleClick += (sender, e) => OpenSelectedUsbFile();
            cardUsb.Controls.Add(_lstUsbFiles);

            // 4. Máy quét & Thông báo
            Panel cardScan = CreateSectionCard(ZenQOffice.Core.AppL10n.T("Máy quét & Thông báo", "Scanner & Notifications"), '\uE896', new Point(16, 344), new Size(608, 86));
            body.Controls.Add(cardScan);

            _chkBarcodeMode = CreateCheck(ZenQOffice.Core.AppL10n.T("Chế độ quét mã vạch trong bảng tính Calc", "Barcode Scanner mode in Calc"), new Point(16, 36), new Size(340, 22), s.CalcBarcodeScannerMode);
            _chkAutoNotify = CreateCheck(ZenQOffice.Core.AppL10n.T("Thông báo khi kết nối USB hoặc màn hình phụ", "Notify on USB or external display connect"), new Point(16, 58), new Size(340, 22), s.PeripheralAutoNotify);
            cardScan.Controls.Add(_chkBarcodeMode);
            cardScan.Controls.Add(_chkAutoNotify);

            Button btnWiaScan = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Máy Scan", "Scan"), new Point(380, 42), new Size(102, 28));
            btnWiaScan.Click += (sender, e) => PeripheralDeviceManager.LaunchWindowsScannerDialog();
            cardScan.Controls.Add(btnWiaScan);

            Button btnDocEnhance = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Làm nét ảnh", "Enhance Photo"), new Point(490, 42), new Size(102, 28));
            btnDocEnhance.Click += (sender, e) => ImportAndEnhanceDocumentPhoto();
            cardScan.Controls.Add(btnDocEnhance);

            // Footer
            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.White
            };
            footer.Paint += (sender, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(borderPen, 0, 0, footer.Width, 0);
                }
            };
            Controls.Add(footer);

            Button btnCancel = CreateNeutralButton(ZenQOffice.Core.AppL10n.T("Đóng", "Close"), new Point(422, 10), new Size(94, 30), '\uE711');
            btnCancel.Click += (sender, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            footer.Controls.Add(btnCancel);

            Button btnApply = CreatePrimaryButton(ZenQOffice.Core.AppL10n.T("Lưu", "Save"), new Point(524, 10), new Size(100, 30), '\uE74E');
            btnApply.Click += (sender, e) =>
            {
                SaveSettingsFromUI();
                DialogResult = DialogResult.OK;
                Close();
            };
            footer.Controls.Add(btnApply);
        }

        private Panel CreateSectionCard(string title, char glyph, Point loc, Size sz)
        {
            Panel card = new Panel
            {
                Location = loc,
                Size = sz,
                BackColor = Color.White
            };
            card.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                FluentIconRenderer.DrawGlyph(e.Graphics, glyph, new Rectangle(14, 11, 16, 16), Color.FromArgb(71, 85, 105), 11.5f);
                using (Font fSec = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold))
                using (Brush bSec = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawString(title, fSec, bSec, 34, 9);
                    e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            return card;
        }

        private CheckBox CreateCheck(string text, Point loc, Size sz, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = loc,
                Size = sz,
                Checked = isChecked,
                UseMnemonic = false
            };
        }

        private Button CreateNeutralButton(string text, Point loc, Size sz, char glyph = '\0')
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 8.75f),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
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

        private Button CreatePrimaryButton(string text, Point loc, Size sz, char glyph = '\0')
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 64, 175);
            if (glyph != '\0')
            {
                btn.Image = FluentIconRenderer.GetGlyphBitmap(glyph, 13, 13, Color.White);
                btn.ImageAlign = ContentAlignment.MiddleLeft;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                btn.Padding = new Padding(4, 0, 4, 0);
            }
            return btn;
        }

        private void RefreshDeviceLists()
        {
            // Monitors
            var monitors = PeripheralDeviceManager.GetConnectedMonitors();
            List<string> monDesc = new List<string>();
            foreach (var m in monitors)
            {
                monDesc.Add(string.Format("{0}×{1}{2}", m.Bounds.Width, m.Bounds.Height, m.IsPrimary ? ZenQOffice.Core.AppL10n.T(" (Chính)", " (Primary)") : ""));
            }
            _lblMonitorStatus.Text = string.Format(ZenQOffice.Core.AppL10n.T("{0} màn hình: {1}", "{0} display(s): {1}"),
                monitors.Count, string.Join(" • ", monDesc.ToArray()));

            // USB drives
            _cboUsbDrives.Items.Clear();
            var drives = PeripheralDeviceManager.GetRemovableDrives();
            foreach (var d in drives)
            {
                _cboUsbDrives.Items.Add(d.DisplayName);
            }
            if (_cboUsbDrives.Items.Count > 0)
            {
                _cboUsbDrives.SelectedIndex = 0;
            }
            else
            {
                _cboUsbDrives.Items.Add(ZenQOffice.Core.AppL10n.T("Chưa phát hiện ổ đĩa USB", "No USB drives detected"));
                _cboUsbDrives.SelectedIndex = 0;
                _lstUsbFiles.Items.Clear();
                _lstUsbFiles.Items.Add(ZenQOffice.Core.AppL10n.T("Chưa có USB nào được kết nối.", "No USB drive connected."));
            }
        }

        private void RefreshUsbFilesList()
        {
            _lstUsbFiles.Items.Clear();
            var drives = PeripheralDeviceManager.GetRemovableDrives();
            int idx = _cboUsbDrives.SelectedIndex;
            if (idx < 0 || idx >= drives.Count)
            {
                _lstUsbFiles.Items.Add(ZenQOffice.Core.AppL10n.T("Chưa có USB nào được kết nối.", "No USB drive connected."));
                return;
            }

            string root = drives[idx].DriveLetter;
            var files = PeripheralDeviceManager.ScanUsbOfficeFiles(root, 40);
            if (files.Count == 0)
            {
                _lstUsbFiles.Items.Add(ZenQOffice.Core.AppL10n.T("Không có tệp Office trên ", "No Office files on ") + root);
            }
            else
            {
                foreach (string f in files)
                {
                    _lstUsbFiles.Items.Add(f);
                }
            }
        }

        private void DoBackupToSelectedUsb()
        {
            var drives = PeripheralDeviceManager.GetRemovableDrives();
            int idx = _cboUsbDrives.SelectedIndex;
            if (idx < 0 || idx >= drives.Count)
            {
                ZenQOffice.UI.FluentMessageBox.Show(ZenQOffice.Core.AppL10n.T("Chưa có ổ đĩa USB nào được kết nối.", "No USB drive connected."), ZenQOffice.Core.AppL10n.T("Sao lưu ra USB", "Backup to USB"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string docPath = GetCurrentDocumentPathCallback != null ? GetCurrentDocumentPathCallback() : null;
            string destPath, err;
            if (PeripheralDeviceManager.BackupFileToUsb(docPath, drives[idx].DriveLetter, out destPath, out err))
            {
                ZenQOffice.UI.FluentMessageBox.Show(ZenQOffice.Core.AppL10n.T("Đã sao lưu tài liệu tại:\n", "Document backed up to:\n") + destPath, ZenQOffice.Core.AppL10n.T("Sao lưu thành công", "Backup Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshUsbFilesList();
            }
            else
            {
                ZenQOffice.UI.FluentMessageBox.Show(err ?? ZenQOffice.Core.AppL10n.T("Không thể sao lưu tệp ra USB.", "Could not back up file to USB."), ZenQOffice.Core.AppL10n.T("Sao lưu ra USB", "Backup to USB"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenSelectedUsbFile()
        {
            if (_lstUsbFiles.SelectedItem is string path && File.Exists(path))
            {
                SaveSettingsFromUI();
                SelectedUsbFileToOpen = path;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                var drives = PeripheralDeviceManager.GetRemovableDrives();
                int idx = _cboUsbDrives.SelectedIndex;
                if (idx >= 0 && idx < drives.Count && Directory.Exists(drives[idx].DriveLetter))
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.InitialDirectory = drives[idx].DriveLetter;
                        ofd.Title = ZenQOffice.Core.AppL10n.T("Mở tài liệu từ USB", "Open Document from USB");
                        ofd.Filter = ZenQOffice.Core.AppL10n.T("Tài liệu Office (*.docx;*.xlsx;*.pptx;*.pdf;*.csv;*.txt)|*.docx;*.xlsx;*.pptx;*.pdf;*.csv;*.txt|Tất cả tệp (*.*)|*.*", "Office Files (*.docx;*.xlsx;*.pptx;*.pdf;*.csv;*.txt)|*.docx;*.xlsx;*.pptx;*.pdf;*.csv;*.txt|All Files (*.*)|*.*");
                        if (ofd.ShowDialog(this) == DialogResult.OK)
                        {
                            SaveSettingsFromUI();
                            SelectedUsbFileToOpen = ofd.FileName;
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                    }
                }
            }
        }

        private void ImportAndEnhanceDocumentPhoto()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = ZenQOffice.Core.AppL10n.T("Chọn ảnh tài liệu để làm nét", "Select Document Photo to Enhance");
                ofd.Filter = ZenQOffice.Core.AppL10n.T("Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.tif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif", "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.tif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif");
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (Bitmap raw = new Bitmap(ofd.FileName))
                        {
                            DialogResult bwAsk = ZenQOffice.UI.FluentMessageBox.Show(
                                ZenQOffice.Core.AppL10n.T("Chuyển ảnh sang chế độ Đen-Trắng độ tương phản cao?\n\n• Yes: Đen-Trắng (rõ chữ)\n• No: Giữ màu gốc", "Convert photo to high-contrast Black & White?\n\n• Yes: Black & White (sharp text)\n• No: Keep original colors"),
                                ZenQOffice.Core.AppL10n.T("Làm nét ảnh tài liệu", "Enhance Document Photo"),
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            ScannedDocumentBitmapToInsert = PeripheralDeviceManager.EnhanceScannedDocumentImage(raw, bwAsk == DialogResult.Yes);
                            SaveSettingsFromUI();
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        ZenQOffice.UI.FluentMessageBox.Show(ZenQOffice.Core.AppL10n.T("Không thể xử lý ảnh: ", "Image processing error: ") + ex.Message, ZenQOffice.Core.AppL10n.Tr("Lỗi"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveSettingsFromUI()
        {
            var s = AppSettings.Instance;
            s.TouchModeEnabled = _chkTouchMode.Checked;
            s.AutoSwitchPenInk = _chkAutoPenInk.Checked;
            s.PenPressureSensitivity = _chkPenPressure.Checked;
            s.AutoSummonTouchKeyboard = _chkAutoTouchKbd.Checked;
            s.CalcBarcodeScannerMode = _chkBarcodeMode.Checked;
            s.PeripheralAutoNotify = _chkAutoNotify.Checked;
            s.Save();
        }
    }
}

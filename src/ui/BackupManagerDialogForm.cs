using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class BackupManagerDialogForm : Form
    {
        private DataGridView dgvBackups;
        private Label lblSummary;
        private TextBox txtSearch;
        private Button btnRestoreOriginal;
        private Button btnMoveToDesktop;
        private Button btnCopyToDesktop;
        private Button btnMoveToCustomFolder;
        private Button btnOpenFile;
        private Button btnOpenFolder;
        private Button btnPrune;
        private Button btnClose;

        private List<BackupItem> _allBackups = new List<BackupItem>();
        private List<BackupItem> _filteredBackups = new List<BackupItem>();
        private Action<string> _onOpenDocumentCallback;

        public BackupManagerDialogForm(Action<string> onOpenDocumentCallback = null)
        {
            _onOpenDocumentCallback = onOpenDocumentCallback;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = AppL10n.T("Quản lý Bản sao lưu Tệp gốc An toàn (Safety Vault) | ZenQ Office", "Original File Backup Vault | ZenQ Office");
            this.Size = new Size(1000, 680);
            this.MinimumSize = new Size(880, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252); // #F8FAFC
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // 1. Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(20, 14, 20, 10)
            };
            pnlHeader.Paint += (s, e) =>
            {
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uEA18', new Rectangle(20, 16, 26, 26), Color.FromArgb(37, 99, 235), 22f);
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = AppL10n.T("Kho Sao lưu Tệp Gốc An toàn (Safety Vault)", "Original File Safety Vault"),
                Font = new Font("Segoe UI", 13.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Slate 900
                AutoSize = true,
                Location = new Point(54, 14)
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text = AppL10n.T("Mỗi khi bạn mở hoặc lưu đè tệp tin, ZenQ Office luôn tự động sao lưu bản gốc nguyên vẹn vào kho an toàn.\nNếu tệp gặp sự cố hoặc bạn muốn lấy lại bản gốc, bạn có thể đưa tệp về Desktop, chuyển đến thư mục bất kỳ hoặc khôi phục trực tiếp.", "Whenever you open or save over a file, ZenQ Office keeps an intact backup copy here.\nIf a file runs into trouble, you can send it to Desktop, move it to any folder, or restore it directly."),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105), // Slate 600
                AutoSize = true,
                Location = new Point(54, 40)
            };
            pnlHeader.Controls.Add(lblDesc);

            // Divider line
            Panel divHeader = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlHeader.Controls.Add(divHeader);

            // 2. Bottom Action Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            this.Controls.Add(pnlBottom);

            Panel divBottom = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlBottom.Controls.Add(divBottom);

            btnMoveToDesktop = new Button
            {
                Text = AppL10n.T("Cắt ra Desktop", "Move to Desktop"),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8C6', 15, 15, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(37, 99, 235), // Blue 600
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMoveToDesktop.FlatAppearance.BorderSize = 0;
            btnMoveToDesktop.Click += (s, e) => RelocateSelected(true, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            pnlBottom.Controls.Add(btnMoveToDesktop);

            btnCopyToDesktop = new Button
            {
                Text = AppL10n.T("Chép ra Desktop", "Copy to Desktop"),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8C8', 15, 15, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnCopyToDesktop.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCopyToDesktop.Click += (s, e) => RelocateSelected(false, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            pnlBottom.Controls.Add(btnCopyToDesktop);

            btnMoveToCustomFolder = new Button
            {
                Text = AppL10n.T("Chuyển thư mục...", "Move to Folder..."),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8DE', 15, 15, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Size = new Size(165, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnMoveToCustomFolder.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMoveToCustomFolder.Click += (s, e) => ChooseCustomFolder();
            pnlBottom.Controls.Add(btnMoveToCustomFolder);

            btnRestoreOriginal = new Button
            {
                Text = AppL10n.T("Khôi phục vị trí gốc", "Restore Original"),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE777', 15, 15, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Size = new Size(165, 38),
                BackColor = Color.FromArgb(16, 185, 129), // Emerald 500
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRestoreOriginal.FlatAppearance.BorderSize = 0;
            btnRestoreOriginal.Click += (s, e) => RestoreSelectedToOriginal();
            pnlBottom.Controls.Add(btnRestoreOriginal);

            btnClose = new Button
            {
                Text = AppL10n.T("Đóng", "Close"),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 13, 13, Color.FromArgb(71, 85, 105)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Size = new Size(95, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            pnlBottom.Resize += (s, e) =>
            {
                int y = 16;
                btnMoveToDesktop.Location = new Point(16, y);
                btnCopyToDesktop.Location = new Point(btnMoveToDesktop.Right + 8, y);
                btnMoveToCustomFolder.Location = new Point(btnCopyToDesktop.Right + 8, y);
                btnRestoreOriginal.Location = new Point(btnMoveToCustomFolder.Right + 8, y);
                btnClose.Location = new Point(pnlBottom.ClientSize.Width - btnClose.Width - 16, y);
            };

            // 3. Toolbar / Filter Panel
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 8, 16, 8)
            };
            this.Controls.Add(pnlToolbar);

            lblSummary = new Label
            {
                Text = AppL10n.T("Đang tải dữ liệu sao lưu...", "Loading backup vault..."),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 17)
            };
            pnlToolbar.Controls.Add(lblSummary);

            txtSearch = new TextBox
            {
                Size = new Size(220, 26),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Text = AppL10n.T("Tìm kiếm tệp...", "Search files...")
            };
            txtSearch.GotFocus += (s, e) =>
            {
                if (txtSearch.Text == AppL10n.T("Tìm kiếm tệp...", "Search files..."))
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = Color.Black;
                }
            };
            txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = AppL10n.T("Tìm kiếm tệp...", "Search files...");
                    txtSearch.ForeColor = Color.FromArgb(100, 116, 139);
                }
            };
            txtSearch.TextChanged += (s, e) => FilterData();
            pnlToolbar.Controls.Add(txtSearch);

            btnOpenFile = new Button
            {
                Text = AppL10n.T("Mở tệp", "Open File"),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE7B3', 14, 14, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0),
                Size = new Size(88, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnOpenFile.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnOpenFile.Click += (s, e) => OpenSelectedBackup();
            pnlToolbar.Controls.Add(btnOpenFile);

            btnOpenFolder = new Button
            {
                Text = AppL10n.T("Thư mục", "Open Folder"),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE838', 14, 14, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0),
                Size = new Size(95, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnOpenFolder.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("explorer.exe", BackupManager.BackupFolder) { UseShellExecute = true }); } catch { }
            };
            pnlToolbar.Controls.Add(btnOpenFolder);

            btnPrune = new Button
            {
                Text = AppL10n.T("Dọn dẹp...", "Clean Up..."),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE74D', 14, 14, Color.FromArgb(185, 28, 28)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(4, 0, 4, 0),
                Size = new Size(110, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(185, 28, 28), // Red 700
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrune.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165); // Red 300
            btnPrune.Click += (s, e) => ShowPruneMenu();
            pnlToolbar.Controls.Add(btnPrune);

            pnlToolbar.Resize += (s, e) =>
            {
                int rx = pnlToolbar.ClientSize.Width - 16;
                btnPrune.Location = new Point(rx - btnPrune.Width, 11);
                btnOpenFolder.Location = new Point(btnPrune.Left - btnOpenFolder.Width - 6, 11);
                btnOpenFile.Location = new Point(btnOpenFolder.Left - btnOpenFile.Width - 6, 11);
                txtSearch.Location = new Point(btnOpenFile.Left - txtSearch.Width - 12, 12);
            };

            // 4. Main DataGridView
            dgvBackups = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(241, 245, 249),
                RowTemplate = { Height = 34 },
                EnableHeadersVisualStyles = false
            };

            dgvBackups.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            dgvBackups.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            dgvBackups.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            dgvBackups.ColumnHeadersHeight = 36;

            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = AppL10n.T("Tên tệp gốc", "File Name"), FillWeight = 25, Name = "ColName" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = AppL10n.T("Đường dẫn gốc", "Original Path"), FillWeight = 38, Name = "ColPath" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = AppL10n.T("Thời gian sao lưu", "Backup Time"), FillWeight = 18, Name = "ColTime" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = AppL10n.T("Kích thước", "Size"), FillWeight = 10, Name = "ColSize" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = AppL10n.T("Loại tệp", "Type"), FillWeight = 9, Name = "ColMode" });

            dgvBackups.CellDoubleClick += (s, e) => OpenSelectedBackup();

            Panel pnlGridWrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 16, 0),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlGridWrap.Controls.Add(dgvBackups);
            this.Controls.Add(pnlGridWrap);
            pnlGridWrap.BringToFront();
        }

        private void LoadData()
        {
            _allBackups = BackupManager.GetAllBackups();
            FilterData();
        }

        private void FilterData()
        {
            string q = (txtSearch.Text == AppL10n.T("Tìm kiếm tệp...", "Search files...")) ? "" : txtSearch.Text.Trim().ToLowerInvariant();
            _filteredBackups = new List<BackupItem>();

            foreach (var it in _allBackups)
            {
                if (string.IsNullOrEmpty(q) ||
                    it.OriginalFileName.ToLowerInvariant().Contains(q) ||
                    it.OriginalPath.ToLowerInvariant().Contains(q) ||
                    it.Mode.ToLowerInvariant().Contains(q))
                {
                    _filteredBackups.Add(it);
                }
            }

            dgvBackups.Rows.Clear();
            foreach (var it in _filteredBackups)
            {
                int rIdx = dgvBackups.Rows.Add(
                    it.OriginalFileName,
                    it.OriginalPath,
                    it.BackupTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    BackupManager.FormatFileSize(it.FileSizeBytes),
                    it.Mode
                );
                dgvBackups.Rows[rIdx].Tag = it;
            }

            long totalBytes = BackupManager.GetTotalBackupSizeBytes();
            lblSummary.Text = AppL10n.T($"Đang lưu trữ {_allBackups.Count} bản sao lưu ({BackupManager.FormatFileSize(totalBytes)}) trong kho an toàn", $"Storing {_allBackups.Count} backup(s) ({BackupManager.FormatFileSize(totalBytes)}) in Safety Vault");
        }

        private BackupItem GetSelectedBackup()
        {
            if (dgvBackups.SelectedRows.Count > 0)
            {
                return dgvBackups.SelectedRows[0].Tag as BackupItem;
            }
            return null;
        }

        private void RelocateSelected(bool cutAndPaste, string targetFolder)
        {
            var item = GetSelectedBackup();
            if (item == null)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Vui lòng chọn một tệp sao lưu trong danh sách.", "Please select a backup file from the list."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string actionName = cutAndPaste ? AppL10n.T("Cắt (Di chuyển)", "moved") : AppL10n.T("Sao chép", "copied");
            bool ok = BackupManager.RelocateBackup(item, targetFolder, cutAndPaste, out string finalPath);
            if (ok)
            {
                string msg = AppL10n.T($"Đã {actionName.ToLower()} bản sao lưu ra thành công:\n{finalPath}\n\nBạn có muốn mở thư mục chứa tệp ngay bây giờ không?", $"Successfully {actionName} backup to:\n{finalPath}\n\nOpen the containing folder now?");
                if (ZenQOffice.UI.FluentMessageBox.Show(this, msg, AppL10n.T("Hoàn thành", "Done"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{finalPath}\"") { UseShellExecute = true }); } catch { }
                }
                LoadData();
            }
            else
            {
                ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Không thể di chuyển tệp. Vui lòng kiểm tra quyền truy cập thư mục.", "Could not move file. Check folder permissions."), AppL10n.Tr("Lỗi"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChooseCustomFolder()
        {
            var item = GetSelectedBackup();
            if (item == null)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Vui lòng chọn một tệp sao lưu trong danh sách.", "Please select a backup file from the list."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = AppL10n.T("Chọn thư mục bạn muốn chuyển tệp sao lưu đến:", "Select destination folder for backup:");
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    var opt = ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Bạn muốn Cắt (Di chuyển) hay Sao chép tệp sang thư mục này?\n\n• Nhấn [Yes] để CẮT (Xóa khỏi kho backup, chuyển hẳn sang thư mục mới)\n• Nhấn [No] để SAO CHÉP (Giữ nguyên trong kho backup)", "Move or copy backup file to this folder?\n\n• [Yes]: MOVE out of vault\n• [No]: COPY and keep in vault"), AppL10n.T("Tùy chọn di chuyển", "Move / Copy Option"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (opt == DialogResult.Cancel) return;
                    bool cut = (opt == DialogResult.Yes);
                    RelocateSelected(cut, fbd.SelectedPath);
                }
            }
        }

        private void RestoreSelectedToOriginal()
        {
            var item = GetSelectedBackup();
            if (item == null)
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Vui lòng chọn một tệp sao lưu trong danh sách.", "Please select a backup file from the list."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string warn = AppL10n.T($"Bạn có chắc chắn muốn khôi phục tệp sao lưu ghi đè trở lại đường dẫn gốc:\n{item.OriginalPath}\n\nThao tác này sẽ thay thế hoàn toàn tệp hiện tại bằng bản sao lưu lúc {item.BackupTime:dd/MM/yyyy HH:mm:ss}.", $"Overwrite original file at:\n{item.OriginalPath}\n\nThis replaces the current file with backup from {item.BackupTime:yyyy-MM-dd HH:mm:ss}.");
            if (ZenQOffice.UI.FluentMessageBox.Show(this, warn, AppL10n.T("Xác nhận khôi phục", "Confirm Restore"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bool ok = BackupManager.RestoreToOriginal(item);
                if (ok)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Đã khôi phục tệp gốc thành công!\nNếu tệp đang mở trong ứng dụng, vui lòng mở lại để nạp nội dung đã khôi phục.", "Original file restored!\nRe-open the file if it is currently open."), AppL10n.T("Khôi phục thành công", "Restore Successful"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (_onOpenDocumentCallback != null)
                    {
                        _onOpenDocumentCallback(item.OriginalPath);
                        this.Close();
                    }
                }
                else
                {
                    ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Không thể khôi phục tệp. Có thể tệp gốc đang được mở bởi ứng dụng khác.", "Could not restore file. It may be locked by another process."), AppL10n.T("Lỗi khôi phục", "Restore Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenSelectedBackup()
        {
            var item = GetSelectedBackup();
            if (item == null) return;

            if (_onOpenDocumentCallback != null)
            {
                _onOpenDocumentCallback(item.BackupFullPath);
                this.Close();
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo(item.BackupFullPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Không thể mở tệp: ", "Could not open file: ") + ex.Message, AppL10n.Tr("Lỗi"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowPruneMenu()
        {
            ContextMenuStrip menu = new ZenQOffice.UI.FluentContextMenuStrip();
            menu.Items.Add(AppL10n.T("Dọn dẹp bản sao lưu cũ hơn 7 ngày", "Remove backups older than 7 days"), null, (s, e) =>
            {
                int count = BackupManager.PruneBackupsOlderThan(7);
                ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T($"Đã dọn dẹp {count} bản sao lưu cũ hơn 7 ngày.", $"Removed {count} backup(s) older than 7 days."), AppL10n.T("Dọn dẹp hoàn tất", "Cleanup Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            });

            menu.Items.Add(AppL10n.T("Dọn dẹp bản sao lưu cũ hơn 30 ngày", "Remove backups older than 30 days"), null, (s, e) =>
            {
                int count = BackupManager.PruneBackupsOlderThan(30);
                ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T($"Đã dọn dẹp {count} bản sao lưu cũ hơn 30 ngày.", $"Removed {count} backup(s) older than 30 days."), AppL10n.T("Dọn dẹp hoàn tất", "Cleanup Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            });

            menu.Items.Add("-");

            var sel = GetSelectedBackup();
            var delItem = menu.Items.Add(sel != null ? AppL10n.T($"Xóa bản sao lưu đang chọn ('{sel.OriginalFileName}')", $"Delete selected backup ('{sel.OriginalFileName}')") : AppL10n.T("Xóa bản sao lưu đang chọn", "Delete selected backup"), FluentIconRenderer.GetGlyphBitmap('\uE74D', 16, 16, Color.FromArgb(100, 116, 139)), (s, e) =>
            {
                if (sel != null && ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Bạn có chắc chắn muốn xóa bản sao lưu này?", "Delete this backup?"), AppL10n.T("Xác nhận", "Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    BackupManager.DeleteBackup(sel);
                    LoadData();
                }
            });
            delItem.Enabled = (sel != null);

            menu.Items.Add("-");

            var clearAllItem = menu.Items.Add(AppL10n.T("Xóa toàn bộ kho sao lưu (Giải phóng toàn bộ dung lượng)", "Delete All Backups (Free Space)"), FluentIconRenderer.GetGlyphBitmap('\uE74D', 16, 16, Color.FromArgb(220, 38, 38)), (s, e) =>
            {
                if (ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T("Bạn có chắc chắn muốn xóa toàn bộ tất cả bản sao lưu trong kho?\nThao tác này không thể hoàn tác.", "Delete ALL backups in the vault?\nThis cannot be undone."), AppL10n.T("Cảnh báo", "Warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int count = BackupManager.ClearAllBackups();
                    ZenQOffice.UI.FluentMessageBox.Show(this, AppL10n.T($"Đã xóa {count} bản sao lưu. Kho sao lưu đã được giải phóng hoàn toàn.", $"Deleted {count} backup(s). Vault is now empty."), AppL10n.T("Dọn dẹp hoàn tất", "Cleanup Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
            });
            clearAllItem.ForeColor = Color.Red;

            menu.Show(btnPrune, new Point(0, btnPrune.Height));
        }
    }
}

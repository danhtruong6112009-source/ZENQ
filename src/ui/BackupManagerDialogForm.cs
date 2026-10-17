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
            this.Text = "Quản lý Bản sao lưu Tệp gốc An toàn (Safety Vault) | ZenQ Office";
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
                Text = "Kho Sao lưu Tệp Gốc An toàn (Safety Vault)",
                Font = new Font("Segoe UI", 13.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Slate 900
                AutoSize = true,
                Location = new Point(54, 14)
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text = "Mỗi khi bạn mở hoặc lưu đè tệp tin, ZenQ Office luôn tự động sao lưu bản gốc nguyên vẹn vào kho an toàn.\nNếu tệp gặp sự cố hoặc bạn muốn lấy lại bản gốc, bạn có thể đưa tệp về Desktop, chuyển đến thư mục bất kỳ hoặc khôi phục trực tiếp.",
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
                Text = "Cắt ra Desktop",
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
                Text = "Chép ra Desktop",
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
                Text = "Chuyển thư mục...",
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
                Text = "Khôi phục vị trí gốc",
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
                Text = "Đóng",
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
                Text = "Đang tải dữ liệu sao lưu...",
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
                Text = "Tìm kiếm tệp..."
            };
            txtSearch.GotFocus += (s, e) =>
            {
                if (txtSearch.Text == "Tìm kiếm tệp...")
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = Color.Black;
                }
            };
            txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = "Tìm kiếm tệp...";
                    txtSearch.ForeColor = Color.FromArgb(100, 116, 139);
                }
            };
            txtSearch.TextChanged += (s, e) => FilterData();
            pnlToolbar.Controls.Add(txtSearch);

            btnOpenFile = new Button
            {
                Text = "Mở tệp",
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
                Text = "Thư mục",
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
                Text = "Dọn dẹp...",
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

            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên tệp gốc", FillWeight = 25, Name = "ColName" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đường dẫn gốc", FillWeight = 38, Name = "ColPath" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thời gian sao lưu", FillWeight = 18, Name = "ColTime" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kích thước", FillWeight = 10, Name = "ColSize" });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại tệp", FillWeight = 9, Name = "ColMode" });

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
            string q = (txtSearch.Text == "Tìm kiếm tệp...") ? "" : txtSearch.Text.Trim().ToLowerInvariant();
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
            lblSummary.Text = $"Đang lưu trữ {_allBackups.Count} bản sao lưu ({BackupManager.FormatFileSize(totalBytes)}) trong kho an toàn";
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
                MessageBox.Show("Vui lòng chọn một tệp sao lưu trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string actionName = cutAndPaste ? "Cắt (Di chuyển)" : "Sao chép";
            bool ok = BackupManager.RelocateBackup(item, targetFolder, cutAndPaste, out string finalPath);
            if (ok)
            {
                string msg = $"Đã {actionName.ToLower()} bản sao lưu ra thành công:\n{finalPath}\n\nBạn có muốn mở thư mục chứa tệp ngay bây giờ không?";
                if (MessageBox.Show(this, msg, "Hoàn thành", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{finalPath}\"") { UseShellExecute = true }); } catch { }
                }
                LoadData();
            }
            else
            {
                MessageBox.Show(this, "Không thể di chuyển tệp. Vui lòng kiểm tra quyền truy cập thư mục.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChooseCustomFolder()
        {
            var item = GetSelectedBackup();
            if (item == null)
            {
                MessageBox.Show("Vui lòng chọn một tệp sao lưu trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Chọn thư mục bạn muốn chuyển tệp sao lưu đến:";
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    var opt = MessageBox.Show(this, "Bạn muốn Cắt (Di chuyển) hay Sao chép tệp sang thư mục này?\n\n• Nhấn [Yes] để CẮT (Xóa khỏi kho backup, chuyển hẳn sang thư mục mới)\n• Nhấn [No] để SAO CHÉP (Giữ nguyên trong kho backup)", "Tùy chọn di chuyển", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
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
                MessageBox.Show("Vui lòng chọn một tệp sao lưu trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string warn = $"Bạn có chắc chắn muốn khôi phục tệp sao lưu ghi đè trở lại đường dẫn gốc:\n{item.OriginalPath}\n\nThao tác này sẽ thay thế hoàn toàn tệp hiện tại bằng bản sao lưu lúc {item.BackupTime:dd/MM/yyyy HH:mm:ss}.";
            if (MessageBox.Show(this, warn, "Xác nhận khôi phục", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bool ok = BackupManager.RestoreToOriginal(item);
                if (ok)
                {
                    MessageBox.Show(this, "Đã khôi phục tệp gốc thành công!\nNếu tệp đang mở trong ứng dụng, vui lòng mở lại để nạp nội dung đã khôi phục.", "Khôi phục thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (_onOpenDocumentCallback != null)
                    {
                        _onOpenDocumentCallback(item.OriginalPath);
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show(this, "Không thể khôi phục tệp. Có thể tệp gốc đang được mở bởi ứng dụng khác.", "Lỗi khôi phục", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(this, "Không thể mở tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowPruneMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Dọn dẹp bản sao lưu cũ hơn 7 ngày", null, (s, e) =>
            {
                int count = BackupManager.PruneBackupsOlderThan(7);
                MessageBox.Show(this, $"Đã dọn dẹp {count} bản sao lưu cũ hơn 7 ngày.", "Dọn dẹp hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            });

            menu.Items.Add("Dọn dẹp bản sao lưu cũ hơn 30 ngày", null, (s, e) =>
            {
                int count = BackupManager.PruneBackupsOlderThan(30);
                MessageBox.Show(this, $"Đã dọn dẹp {count} bản sao lưu cũ hơn 30 ngày.", "Dọn dẹp hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            });

            menu.Items.Add("-");

            var sel = GetSelectedBackup();
            var delItem = menu.Items.Add(sel != null ? $"Xóa bản sao lưu đang chọn ('{sel.OriginalFileName}')" : "Xóa bản sao lưu đang chọn", FluentIconRenderer.GetGlyphBitmap('\uE74D', 16, 16, Color.FromArgb(100, 116, 139)), (s, e) =>
            {
                if (sel != null && MessageBox.Show(this, "Bạn có chắc chắn muốn xóa bản sao lưu này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    BackupManager.DeleteBackup(sel);
                    LoadData();
                }
            });
            delItem.Enabled = (sel != null);

            menu.Items.Add("-");

            var clearAllItem = menu.Items.Add("Xóa toàn bộ kho sao lưu (Giải phóng toàn bộ dung lượng)", FluentIconRenderer.GetGlyphBitmap('\uE74D', 16, 16, Color.FromArgb(220, 38, 38)), (s, e) =>
            {
                if (MessageBox.Show(this, "Bạn có chắc chắn muốn xóa toàn bộ tất cả bản sao lưu trong kho?\nThao tác này không thể hoàn tác.", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int count = BackupManager.ClearAllBackups();
                    MessageBox.Show(this, $"Đã xóa {count} bản sao lưu. Kho sao lưu đã được giải phóng hoàn toàn.", "Dọn dẹp hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
            });
            clearAllItem.ForeColor = Color.Red;

            menu.Show(btnPrune, new Point(0, btnPrune.Height));
        }
    }
}

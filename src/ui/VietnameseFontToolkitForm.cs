using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class VietnameseFontToolkitForm : Form
    {
        private TextBox txtFilePath;
        private Button btnBrowse;
        private Button btnUseCurrent;
        private DataGridView dgvFonts;
        private Label lblTotalFonts;
        private Label lblEmbeddedFonts;
        private Label lblProblemFonts;
        private TextBox txtPreviewSample;
        private PictureBox pbFontPreview;
        private Label lblDiagnosticInfo;
        private Button btnExportFonts;
        private Button btnInstallFonts;
        private Button btnFixDocument;
        private ProgressBar progressBar;

        private List<ExtractedFontInfo> currentFontList = new List<ExtractedFontInfo>();
        private ExtractedFontInfo selectedFont = null;
        private string activeDocumentPath = "";
        private string openEditorFilePath = "";

        public VietnameseFontToolkitForm(string currentDocPath = null)
        {
            this.openEditorFilePath = currentDocPath;
            InitializeComponent();

            if (!string.IsNullOrEmpty(currentDocPath) && File.Exists(currentDocPath))
            {
                LoadDocumentFile(currentDocPath);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Bộ công cụ Trích xuất Font & Sửa lỗi Tiếng Việt | ZenQ Office";
            this.Size = new Size(980, 700);
            this.MinimumSize = new Size(880, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252); // #F8FAFC
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            this.AllowDrop = true;
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;

            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(15, 23, 42) // Slate 900
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "✨ BỘ CÔNG CỤ TRÍCH XUẤT FONT & SỬA LỖI TIẾNG VIỆT",
                Font = new Font("Segoe UI", 13.0f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 12),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Trích xuất font nhúng (.ttf/.otf), giữ nguyên phông chữ khi chuyển máy, sửa lỗi bảng mã TCVN3/VNI và khắc phục vỡ font tiếng Việt.",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
                Location = new Point(22, 40),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. File Selection Panel
            Panel pnlFile = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(241, 245, 249), // Slate 100
                Padding = new Padding(16, 12, 16, 10)
            };
            this.Controls.Add(pnlFile);

            Label lblFilePrompt = new Label
            {
                Text = "Tệp cần xử lý:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(18, 18),
                AutoSize = true
            };
            pnlFile.Controls.Add(lblFilePrompt);

            txtFilePath = new TextBox
            {
                Location = new Point(110, 15),
                Size = new Size(540, 26),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.0f)
            };
            pnlFile.Controls.Add(txtFilePath);

            btnBrowse = new Button
            {
                Text = "📂 Duyệt tệp...",
                Location = new Point(660, 13),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(37, 99, 235), // Blue 600
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) => BrowseAndOpenFile();
            pnlFile.Controls.Add(btnBrowse);

            btnUseCurrent = new Button
            {
                Text = "📑 Dùng tệp đang mở",
                Location = new Point(800, 13),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(15, 118, 110), // Teal 700
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = !string.IsNullOrEmpty(openEditorFilePath) && File.Exists(openEditorFilePath)
            };
            btnUseCurrent.FlatAppearance.BorderSize = 0;
            btnUseCurrent.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(openEditorFilePath) && File.Exists(openEditorFilePath))
                {
                    LoadDocumentFile(openEditorFilePath);
                }
            };
            pnlFile.Controls.Add(btnUseCurrent);

            // 3. KPI Statistics Bar
            Panel pnlKpi = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8)
            };
            this.Controls.Add(pnlKpi);

            lblTotalFonts = CreateKpiCard(pnlKpi, "Tổng phông chữ", "0", Color.FromArgb(37, 99, 235), 18);
            lblEmbeddedFonts = CreateKpiCard(pnlKpi, "Font nhúng có thể trích xuất", "0", Color.FromArgb(16, 185, 129), 330);
            lblProblemFonts = CreateKpiCard(pnlKpi, "Font thiếu dấu Tiếng Việt", "0", Color.FromArgb(239, 68, 68), 640);

            // 4. Bottom Action Bar
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16, 12, 16, 12)
            };
            this.Controls.Add(pnlBottom);

            progressBar = new ProgressBar
            {
                Location = new Point(18, 22),
                Size = new Size(240, 18),
                Visible = false
            };
            pnlBottom.Controls.Add(progressBar);

            btnExportFonts = new Button
            {
                Text = "📥 Trích xuất tất cả Font (.ttf/.otf)",
                Size = new Size(230, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportFonts.FlatAppearance.BorderSize = 0;
            btnExportFonts.Click += (s, e) => DoExportFonts();
            pnlBottom.Controls.Add(btnExportFonts);

            btnInstallFonts = new Button
            {
                Text = "⚡ Cài đặt vào ZenQ Office",
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstallFonts.FlatAppearance.BorderSize = 0;
            btnInstallFonts.Click += (s, e) => DoInstallFontToApp();
            pnlBottom.Controls.Add(btnInstallFonts);

            btnFixDocument = new Button
            {
                Text = "🛠️ Sửa lỗi Tiếng Việt trong tệp...",
                Size = new Size(240, 38),
                BackColor = Color.FromArgb(217, 119, 6), // Amber 600
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFixDocument.FlatAppearance.BorderSize = 0;
            btnFixDocument.Click += (s, e) => DoFixDocument();
            pnlBottom.Controls.Add(btnFixDocument);

            // Căn chỉnh các nút bottom theo lề phải
            pnlBottom.Resize += (s, e) =>
            {
                int rx = pnlBottom.ClientSize.Width - 16;
                btnFixDocument.Location = new Point(rx - btnFixDocument.Width, 12);
                btnInstallFonts.Location = new Point(btnFixDocument.Left - btnInstallFonts.Width - 10, 12);
                btnExportFonts.Location = new Point(btnInstallFonts.Left - btnExportFonts.Width - 10, 12);
            };

            // 5. Main Split View (Font List & Preview)
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 260,
                Panel1MinSize = 180,
                Panel2MinSize = 160
            };
            this.Controls.Add(split);
            split.BringToFront();

            // Panel 1: DataGridView danh sách font
            dgvFonts = new DataGridView
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
                RowTemplate = { Height = 34 },
                EnableHeadersVisualStyles = false
            };
            dgvFonts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            dgvFonts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            dgvFonts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            dgvFonts.ColumnHeadersHeight = 36;
            dgvFonts.SelectionChanged += DgvFonts_SelectionChanged;

            dgvFonts.Columns.Add("Typeface", "Tên Phông chữ (Family)");
            dgvFonts.Columns.Add("Format", "Định dạng");
            dgvFonts.Columns.Add("Source", "Nguồn gốc");
            dgvFonts.Columns.Add("Size", "Dung lượng");
            dgvFonts.Columns.Add("Vietnamese", "Hỗ trợ Tiếng Việt");
            dgvFonts.Columns.Add("Suggestion", "Gợi ý thay thế nếu lỗi");

            split.Panel1.Controls.Add(dgvFonts);

            // Panel 2: Live Preview & Diagnostics Box
            Panel pnlPreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8)
            };
            split.Panel2.Controls.Add(pnlPreview);

            Label lblPreviewHeader = new Label
            {
                Text = "👁 XEM TRƯỚC VĂN BẢN TIẾNG VIỆT VÀ CHẨN ĐOÁN LỖI DẤU:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 8),
                AutoSize = true
            };
            pnlPreview.Controls.Add(lblPreviewHeader);

            txtPreviewSample = new TextBox
            {
                Text = "Việt Nam đất nước tươi đẹp - Bách khoa toàn thư mở - Trí tuệ nhân tạo ZenQ 2026",
                Location = new Point(14, 32),
                Size = new Size(580, 26),
                Font = new Font("Segoe UI", 9.0f)
            };
            txtPreviewSample.TextChanged += (s, e) => pbFontPreview.Invalidate();
            pnlPreview.Controls.Add(txtPreviewSample);

            Button btnSampleDefault = new Button
            {
                Text = "Mẫu chuẩn",
                Location = new Point(602, 31),
                Size = new Size(90, 27),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSampleDefault.FlatAppearance.BorderSize = 0;
            btnSampleDefault.Click += (s, e) =>
            {
                txtPreviewSample.Text = "Việt Nam đất nước tươi đẹp - Bách khoa toàn thư mở - Trí tuệ nhân tạo ZenQ 2026";
            };
            pnlPreview.Controls.Add(btnSampleDefault);

            pbFontPreview = new PictureBox
            {
                Location = new Point(14, 66),
                Height = 65,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            pbFontPreview.Paint += PbFontPreview_Paint;
            pnlPreview.Controls.Add(pbFontPreview);

            lblDiagnosticInfo = new Label
            {
                Location = new Point(14, 138),
                Height = 45,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Text = "Chọn một phông chữ ở danh sách phía trên để xem trước và chẩn đoán mức độ tương thích tiếng Việt."
            };
            pnlPreview.Controls.Add(lblDiagnosticInfo);

            pnlPreview.Resize += (s, e) =>
            {
                int w = pnlPreview.ClientSize.Width - 28;
                txtPreviewSample.Width = Math.Max(200, w - 110);
                btnSampleDefault.Left = txtPreviewSample.Right + 8;
                pbFontPreview.Width = w;
                lblDiagnosticInfo.Width = w;
            };
        }

        private Label CreateKpiCard(Panel parent, string title, string val, Color accentCol, int left)
        {
            Panel card = new Panel
            {
                Location = new Point(left, 8),
                Size = new Size(290, 48),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(card);

            Panel accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = accentCol
            };
            card.Controls.Add(accentBar);

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 5),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            Label lblVal = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 12.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(10, 22),
                AutoSize = true
            };
            card.Controls.Add(lblVal);

            return lblVal;
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                LoadDocumentFile(files[0]);
            }
        }

        private void BrowseAndOpenFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Tệp Office & Font (*.pptx;*.docx;*.ttf;*.otf)|*.pptx;*.docx;*.ttf;*.otf|Bài thuyết trình PowerPoint (*.pptx)|*.pptx|Văn bản Word (*.docx)|*.docx|Phông chữ TrueType / OpenType (*.ttf;*.otf)|*.ttf;*.otf|Tất cả tệp (*.*)|*.*";
                ofd.Title = "Chọn tệp cần trích xuất phông chữ và sửa tiếng Việt";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadDocumentFile(ofd.FileName);
                }
            }
        }

        public void LoadDocumentFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            activeDocumentPath = path;
            txtFilePath.Text = path;
            dgvFonts.Rows.Clear();

            currentFontList = VietnameseFontToolkit.AnalyzeDocumentFonts(path);

            int total = currentFontList.Count;
            int embedded = 0;
            int problem = 0;

            foreach (var f in currentFontList)
            {
                if (f.IsEmbedded) embedded++;
                if (!f.HasVietnameseSupport) problem++;

                string viStatus = f.HasVietnameseSupport ? "✓ Đầy đủ (134/134)" : $"⚠ Thiếu {f.MissingVietnameseChars.Count} dấu";
                string sizeStr = f.FileSize > 0 ? $"{f.FileSize / 1024:N0} KB" : "--";
                string sourceStr = f.IsEmbedded ? "Nhúng trong tệp" : (f.IsInstalledOnSystem ? "Đã cài trên máy" : "Chưa có trên máy");

                int rowIdx = dgvFonts.Rows.Add(
                    f.Typeface,
                    f.Format,
                    sourceStr,
                    sizeStr,
                    viStatus,
                    f.HasVietnameseSupport ? "--" : f.SuggestedVietnameseFont
                );

                if (!f.HasVietnameseSupport)
                {
                    dgvFonts.Rows[rowIdx].Cells[4].Style.ForeColor = Color.FromArgb(220, 38, 38);
                    dgvFonts.Rows[rowIdx].Cells[4].Style.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
                }
                else
                {
                    dgvFonts.Rows[rowIdx].Cells[4].Style.ForeColor = Color.FromArgb(22, 163, 74);
                }
            }

            lblTotalFonts.Text = total.ToString();
            lblEmbeddedFonts.Text = embedded.ToString();
            lblProblemFonts.Text = problem.ToString();

            if (dgvFonts.Rows.Count > 0)
            {
                dgvFonts.Rows[0].Selected = true;
                selectedFont = currentFontList[0];
                UpdateDiagnosticPanel();
            }
            else
            {
                selectedFont = null;
                UpdateDiagnosticPanel();
            }

            pbFontPreview.Invalidate();
        }

        private void DgvFonts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvFonts.SelectedRows.Count > 0)
            {
                int idx = dgvFonts.SelectedRows[0].Index;
                if (idx >= 0 && idx < currentFontList.Count)
                {
                    selectedFont = currentFontList[idx];
                    UpdateDiagnosticPanel();
                    pbFontPreview.Invalidate();
                }
            }
        }

        private void UpdateDiagnosticPanel()
        {
            if (selectedFont == null)
            {
                lblDiagnosticInfo.Text = "Không có phông chữ nào được chọn.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(100, 116, 139);
                return;
            }

            if (selectedFont.HasVietnameseSupport)
            {
                lblDiagnosticInfo.Text = $"✓ Phông chữ '{selectedFont.Typeface}' hỗ trợ đầy đủ 100% bảng chữ cái và dấu thanh tiếng Việt.\nVăn bản sử dụng phông chữ này hiển thị mượt mà, không lo bị vỡ chữ hay lỗi font.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(22, 163, 74); // Green 600
            }
            else
            {
                string missPreview = selectedFont.MissingVietnameseChars.Count > 0
                    ? string.Join(", ", selectedFont.MissingVietnameseChars.GetRange(0, Math.Min(12, selectedFont.MissingVietnameseChars.Count))) + "..."
                    : "các nguyên âm có dấu";

                lblDiagnosticInfo.Text = $"⚠ CẢNH BÁO VỠ FONT: Phông '{selectedFont.Typeface}' thiếu {selectedFont.MissingVietnameseChars.Count} ký tự ({missPreview}).\n👉 Khuyến nghị: Bấm nút 'Sửa lỗi Tiếng Việt trong tệp' để tự động thay thế bằng '{selectedFont.SuggestedVietnameseFont}' có đầy đủ dấu.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(220, 38, 38); // Red 600
            }
        }

        private void PbFontPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string text = txtPreviewSample.Text;
            if (string.IsNullOrEmpty(text)) text = "Việt Nam";

            string fontName = (selectedFont != null && !string.IsNullOrEmpty(selectedFont.Typeface)) ? selectedFont.Typeface : "Segoe UI";

            using (Font f = SlideFontManager.CreateFont(fontName, 20.0f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(text, f, b, new PointF(12, 16));
            }
        }

        private void DoExportFonts()
        {
            if (currentFontList.Count == 0)
            {
                MessageBox.Show("Vui lòng mở một tệp PPTX hoặc DOCX có chứa phông chữ trước khi trích xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<ExtractedFontInfo> embeddedOnly = currentFontList.FindAll(f => f.Data != null && f.Data.Length > 0);
            if (embeddedOnly.Count == 0)
            {
                MessageBox.Show("Tệp này không chứa tệp phông chữ nhúng trực tiếp. Các phông chữ hiển thị hiện đang lấy từ máy tính.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Chọn thư mục để lưu các tệp phông chữ được trích xuất (.ttf / .otf):";
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string msg;
                    bool ok = VietnameseFontToolkit.ExportFonts(embeddedOnly, fbd.SelectedPath, out msg);
                    if (ok)
                    {
                        var res = MessageBox.Show(msg + "\n\nBạn có muốn mở thư mục vừa lưu không?", "Trích xuất hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            try { Process.Start("explorer.exe", fbd.SelectedPath); } catch { }
                        }
                    }
                    else
                    {
                        MessageBox.Show(msg, "Lỗi trích xuất", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DoInstallFontToApp()
        {
            if (selectedFont == null || selectedFont.Data == null)
            {
                MessageBox.Show("Vui lòng chọn một phông chữ nhúng trong danh sách để nạp vào ZenQ Office.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string msg;
            bool ok = VietnameseFontToolkit.InstallFontToApp(selectedFont, out msg);
            if (ok)
            {
                MessageBox.Show(msg, "Cài đặt thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDocumentFile(activeDocumentPath);
            }
            else
            {
                MessageBox.Show(msg, "Lỗi nạp phông chữ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DoFixDocument()
        {
            if (string.IsNullOrEmpty(activeDocumentPath) || !File.Exists(activeDocumentPath))
            {
                MessageBox.Show("Vui lòng chọn tệp PPTX hoặc DOCX cần sửa trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ext = Path.GetExtension(activeDocumentPath).ToLowerInvariant();
            if (ext != ".pptx" && ext != ".docx")
            {
                MessageBox.Show("Tính năng sửa lỗi tiếng Việt tự động hiện hỗ trợ các tệp trình chiếu PowerPoint (.pptx) và văn bản Word (.docx).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hộp thoại tùy chọn sửa
            using (Form optDlg = new Form())
            {
                optDlg.Text = "Tùy chọn Sửa lỗi Tiếng Việt & Vỡ Font";
                optDlg.Size = new Size(540, 360);
                optDlg.StartPosition = FormStartPosition.CenterParent;
                optDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                optDlg.MaximizeBox = false;
                optDlg.MinimizeBox = false;
                optDlg.BackColor = Color.White;

                Label lblOptTitle = new Label
                {
                    Text = "Chọn các tác vụ sửa lỗi tiếng Việt cần áp dụng:",
                    Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
                    Location = new Point(20, 18),
                    AutoSize = true
                };
                optDlg.Controls.Add(lblOptTitle);

                CheckBox chkTcvn3 = new CheckBox
                {
                    Text = "Tự động chuyển mã TCVN3 (ABC: .VnTime, .VnArial) sang Unicode chuẩn",
                    Checked = true,
                    Location = new Point(24, 55),
                    Size = new Size(480, 24)
                };
                optDlg.Controls.Add(chkTcvn3);

                CheckBox chkVni = new CheckBox
                {
                    Text = "Tự động chuyển mã VNI-Windows (VNI-Times, v.v.) sang Unicode chuẩn",
                    Checked = true,
                    Location = new Point(24, 85),
                    Size = new Size(480, 24)
                };
                optDlg.Controls.Add(chkVni);

                CheckBox chkNfc = new CheckBox
                {
                    Text = "Chuẩn hóa Unicode Tổ Hợp (NFD) sang Unicode Dựng Sẵn (NFC)",
                    Checked = true,
                    Location = new Point(24, 115),
                    Size = new Size(480, 24)
                };
                optDlg.Controls.Add(chkNfc);

                CheckBox chkReplaceFonts = new CheckBox
                {
                    Text = "Tự động thay thế các phông thiếu tiếng Việt bằng phông Việt hóa tương đương",
                    Checked = true,
                    Location = new Point(24, 145),
                    Size = new Size(480, 24)
                };
                optDlg.Controls.Add(chkReplaceFonts);

                Label lblSavePrompt = new Label
                {
                    Text = "Tệp sau khi sửa sẽ được lưu thành bản sao mới để bảo toàn dữ liệu gốc của bạn.",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Location = new Point(24, 185),
                    Size = new Size(480, 40)
                };
                optDlg.Controls.Add(lblSavePrompt);

                Button btnRunFix = new Button
                {
                    Text = "🚀 Bắt đầu Sửa lỗi",
                    Location = new Point(260, 260),
                    Size = new Size(140, 36),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    DialogResult = DialogResult.OK
                };
                optDlg.Controls.Add(btnRunFix);

                Button btnCancel = new Button
                {
                    Text = "Hủy bỏ",
                    Location = new Point(410, 260),
                    Size = new Size(95, 36),
                    BackColor = Color.FromArgb(241, 245, 249),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                optDlg.Controls.Add(btnCancel);

                if (optDlg.ShowDialog(this) == DialogResult.OK)
                {
                    string dir = Path.GetDirectoryName(activeDocumentPath);
                    string nameNoExt = Path.GetFileNameWithoutExtension(activeDocumentPath);
                    string defaultOut = Path.Combine(dir, $"{nameNoExt}_Fixed_Vietnamese{ext}");

                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.FileName = Path.GetFileName(defaultOut);
                        sfd.Filter = ext == ".pptx" ? "Bài thuyết trình PowerPoint (*.pptx)|*.pptx" : "Văn bản Word (*.docx)|*.docx";
                        sfd.Title = "Chọn nơi lưu tệp đã sửa lỗi Tiếng Việt";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            FontFixOptions options = new FontFixOptions
                            {
                                ConvertTcvn3 = chkTcvn3.Checked,
                                ConvertVni = chkVni.Checked,
                                NormalizeNfc = chkNfc.Checked,
                                ReplaceUnsupportedFonts = chkReplaceFonts.Checked
                            };

                            DocumentFixReport report = null;
                            if (ext == ".pptx")
                            {
                                report = VietnameseFontToolkit.FixPresentationVietnamese(activeDocumentPath, sfd.FileName, options);
                            }
                            else
                            {
                                report = VietnameseFontToolkit.FixDocxVietnamese(activeDocumentPath, sfd.FileName, options);
                            }

                            if (report != null && report.Success)
                            {
                                string repDetail = $"🎉 {report.Message}\n\nTệp đã lưu tại:\n{sfd.FileName}";
                                if (report.ReplacedFonts.Count > 0)
                                {
                                    repDetail += "\n\nCác phông chữ đã được thay thế đẹp mắt:\n- " + string.Join("\n- ", report.ReplacedFonts);
                                }

                                var res = MessageBox.Show(repDetail + "\n\nBạn có muốn mở tệp vừa sửa ngay bây giờ không?", "Hoàn tất sửa lỗi", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (res == DialogResult.Yes)
                                {
                                    try { Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
                                }

                                LoadDocumentFile(sfd.FileName);
                            }
                            else
                            {
                                MessageBox.Show(report?.Message ?? "Có lỗi xảy ra khi xử lý tệp.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }
    }
}

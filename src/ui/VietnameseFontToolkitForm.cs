using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class VietnameseFontToolkitForm : Form
    {
        private Label lblCurrentDoc;
        private Button btnBrowse;
        private Button btnUseCurrent;
        private DataGridView dgvFonts;
        
        // Header Status Badges
        private Label badgeTotalFonts;
        private Label badgeViStatus;
        private Label badgeEmbedded;

        // Preview Section
        private Label lblPreviewHeader;
        private Button btnPresetDoc;
        private Button btnPresetSentence;
        private Button btnPresetDiacritics;
        private TextBox txtPreviewSample;
        private PictureBox pbFontPreview;
        private Panel pnlDiagnosticBanner;
        private Label lblDiagnosticInfo;

        // Footer Actions
        private Label lblFooterHint;
        private Button btnExportFonts;
        private Button btnInstallFonts;
        private Button btnFixDocument;
        private Button btnClose;

        private List<ExtractedFontInfo> currentFontList = new List<ExtractedFontInfo>();
        private ExtractedFontInfo selectedFont = null;
        private string activeDocumentPath = "";
        private string openEditorFilePath = "";
        private string docSampleText = "";

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
            this.Text = "Quản lý Phông chữ & Sửa lỗi Tiếng Việt | ZenQ Office";
            this.Size = new Size(980, 700);
            this.MinimumSize = new Size(880, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            this.AllowDrop = true;
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;

            // 1. TOP HEADER (Modern Light Fluent Design)
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };
            this.Controls.Add(pnlHeader);

            // Icon Typography - Windows 11 Fluent Font Icon (\uE8D2)
            PictureBox pbHeaderIcon = new PictureBox
            {
                Location = new Point(20, 14),
                Size = new Size(40, 40),
                BackColor = Color.Transparent
            };
            pbHeaderIcon.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, 39, 39), 8))
                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(0, 0, 40, 40), Color.FromArgb(37, 99, 235), Color.FromArgb(79, 70, 229), 45f))
                {
                    g.FillPath(lgb, path);
                }
                FluentIconRenderer.DrawGlyph(g, '\uE8D2', new Rectangle(0, 0, 40, 40), Color.White, 22f);
            };
            pnlHeader.Controls.Add(pbHeaderIcon);

            Label lblTitle = new Label
            {
                Text = "Quản lý Phông chữ & Sửa lỗi Tiếng Việt",
                Font = new Font("Segoe UI", 12.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(68, 12),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Kiểm tra mức độ tương thích tiếng Việt, trích xuất phông nhúng và tự động sửa lỗi vỡ font / bảng mã TCVN3, VNI.",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(70, 36),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Badges Panel on Right
            FlowLayoutPanel pnlBadges = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(530, 16),
                Size = new Size(430, 36),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(pnlBadges);

            badgeEmbedded = CreateStatusBadge("0 phông nhúng", Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105));
            badgeViStatus = CreateStatusBadge("Chưa nạp tệp", Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105));
            badgeTotalFonts = CreateStatusBadge("0 phông chữ", Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105));

            pnlBadges.Controls.Add(badgeEmbedded);
            pnlBadges.Controls.Add(badgeViStatus);
            pnlBadges.Controls.Add(badgeTotalFonts);

            // 2. DOCUMENT BAR (Clean File Path & Actions)
            Panel pnlFile = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 8, 20, 8)
            };
            pnlFile.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlFile.Height - 1, pnlFile.Width, pnlFile.Height - 1);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE8A5', new Rectangle(18, 15, 16, 16), Color.FromArgb(51, 65, 85), 14f);
            };
            this.Controls.Add(pnlFile);

            Label lblDocPrompt = new Label
            {
                Text = "Tài liệu:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(38, 14),
                AutoSize = true
            };
            pnlFile.Controls.Add(lblDocPrompt);

            lblCurrentDoc = new Label
            {
                Text = "Chưa có tài liệu nào được chọn. Kéo thả tệp hoặc bấm 'Chọn tệp...' để bắt đầu.",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(96, 14),
                Size = new Size(572, 20),
                AutoEllipsis = true
            };
            pnlFile.Controls.Add(lblCurrentDoc);

            btnUseCurrent = new Button
            {
                Text = "Dùng tệp đang mở",
                Size = new Size(160, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Enabled = !string.IsNullOrEmpty(openEditorFilePath) && File.Exists(openEditorFilePath),
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8A5', 14, 14, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0)
            };
            btnUseCurrent.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnUseCurrent.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(openEditorFilePath) && File.Exists(openEditorFilePath))
                {
                    LoadDocumentFile(openEditorFilePath);
                }
            };
            pnlFile.Controls.Add(btnUseCurrent);

            btnBrowse = new Button
            {
                Text = "Chọn tệp khác...",
                Size = new Size(140, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE838', 14, 14, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0)
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnBrowse.Click += (s, e) => BrowseAndOpenFile();
            pnlFile.Controls.Add(btnBrowse);

            pnlFile.Resize += (s, e) =>
            {
                int rx = pnlFile.ClientSize.Width - 20;
                btnBrowse.Location = new Point(rx - btnBrowse.Width, 8);
                btnUseCurrent.Location = new Point(btnBrowse.Left - btnUseCurrent.Width - 8, 8);
                lblCurrentDoc.Width = Math.Max(100, btnUseCurrent.Left - lblCurrentDoc.Left - 16);
            };

            // 3. BOTTOM FOOTER BAR
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
                }
            };
            this.Controls.Add(pnlBottom);

            lblFooterHint = new Label
            {
                Text = "Mẹo: Nhấn chọn phông chữ trong danh sách để xem trước và nhận gợi ý chuẩn hóa.",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 18),
                AutoSize = true
            };
            pnlBottom.Controls.Add(lblFooterHint);

            btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 34),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 14, 14, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            btnFixDocument = new Button
            {
                Text = "Sửa lỗi Tiếng Việt & Chuyển mã...",
                Size = new Size(255, 34),
                BackColor = Color.FromArgb(37, 99, 235), // Primary Blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE90F', 16, 16, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(8, 0, 8, 0)
            };
            btnFixDocument.FlatAppearance.BorderSize = 0;
            btnFixDocument.Click += (s, e) => DoFixDocument();
            pnlBottom.Controls.Add(btnFixDocument);

            btnExportFonts = new Button
            {
                Text = "Trích xuất Font nhúng",
                Size = new Size(185, 34),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false,
                Image = FluentIconRenderer.GetGlyphBitmap('\uEDE1', 16, 16, Color.FromArgb(37, 99, 235)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(8, 0, 8, 0)
            };
            btnExportFonts.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnExportFonts.Click += (s, e) => DoExportFonts();
            pnlBottom.Controls.Add(btnExportFonts);

            btnInstallFonts = new Button
            {
                Text = "Cài vào ZenQ Office",
                Size = new Size(175, 34),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false,
                Image = FluentIconRenderer.GetGlyphBitmap('\uE896', 16, 16, Color.FromArgb(16, 185, 129)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(8, 0, 8, 0)
            };
            btnInstallFonts.FlatAppearance.BorderColor = Color.FromArgb(167, 243, 208);
            btnInstallFonts.Click += (s, e) => DoInstallFontToApp();
            pnlBottom.Controls.Add(btnInstallFonts);

            pnlBottom.Resize += (s, e) =>
            {
                int rx = pnlBottom.ClientSize.Width - 20;
                btnClose.Location = new Point(rx - btnClose.Width, 11);
                btnFixDocument.Location = new Point(btnClose.Left - btnFixDocument.Width - 10, 11);
                
                int nextLeft = btnFixDocument.Left;
                if (btnInstallFonts.Visible)
                {
                    btnInstallFonts.Location = new Point(nextLeft - btnInstallFonts.Width - 10, 11);
                    nextLeft = btnInstallFonts.Left;
                }
                if (btnExportFonts.Visible)
                {
                    btnExportFonts.Location = new Point(nextLeft - btnExportFonts.Width - 10, 11);
                    nextLeft = btnExportFonts.Left;
                }
                lblFooterHint.Width = Math.Max(100, nextLeft - lblFooterHint.Left - 10);
            };

            // 4. MAIN SPLIT VIEW (Font List & Preview Card)
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(226, 232, 240),
                SplitterWidth = 5
            };
            this.Controls.Add(split);
            split.BringToFront();

            this.Shown += (s, e) =>
            {
                try
                {
                    split.Panel1MinSize = 120;
                    split.Panel2MinSize = 160;
                    int targetDist = 280;
                    int maxDist = split.Height - split.Panel2MinSize;
                    if (targetDist > maxDist) targetDist = Math.Max(split.Panel1MinSize, maxDist - 10);
                    if (targetDist >= split.Panel1MinSize && targetDist <= split.Height - split.Panel2MinSize)
                    {
                        split.SplitterDistance = targetDist;
                    }
                }
                catch { }
            };

            // PANEL 1: MODERN DATAGRIDVIEW
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
                RowTemplate = { Height = 38 },
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(241, 245, 249)
            };
            dgvFonts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvFonts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            dgvFonts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            dgvFonts.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            dgvFonts.ColumnHeadersHeight = 36;
            dgvFonts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgvFonts.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 58, 138);
            dgvFonts.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            dgvFonts.SelectionChanged += DgvFonts_SelectionChanged;
            dgvFonts.CellPainting += DgvFonts_CellPainting;

            var colTypeface = dgvFonts.Columns.Add("Typeface", "Tên Phông chữ");
            dgvFonts.Columns[colTypeface].FillWeight = 28;
            var colVi = dgvFonts.Columns.Add("Vietnamese", "Tương thích Tiếng Việt");
            dgvFonts.Columns[colVi].FillWeight = 26;
            var colSource = dgvFonts.Columns.Add("Source", "Nguồn phông");
            dgvFonts.Columns[colSource].FillWeight = 18;
            var colFormat = dgvFonts.Columns.Add("Format", "Định dạng");
            dgvFonts.Columns[colFormat].FillWeight = 13;
            var colSuggestion = dgvFonts.Columns.Add("Suggestion", "Gợi ý khắc phục");
            dgvFonts.Columns[colSuggestion].FillWeight = 20;

            split.Panel1.Controls.Add(dgvFonts);

            // PANEL 2: PREVIEW & DIAGNOSTICS CARD
            Panel pnlPreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };
            split.Panel2.Controls.Add(pnlPreview);

            // Top Row: Preview Label & Quick Presets
            lblPreviewHeader = new Label
            {
                Text = "XEM TRƯỚC VĂN BẢN VỚI PHÔNG ĐANG CHỌN:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(40, 14),
                AutoSize = true
            };
            pnlPreview.Paint += (s, e) =>
            {
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE890', new Rectangle(18, 14, 16, 16), Color.FromArgb(71, 85, 105), 14f);
            };
            pnlPreview.Controls.Add(lblPreviewHeader);

            btnPresetDoc = CreatePresetButton("Trích từ tài liệu", 20, '\uE8A5');
            btnPresetDoc.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(docSampleText)) txtPreviewSample.Text = docSampleText;
            };
            pnlPreview.Controls.Add(btnPresetDoc);

            btnPresetSentence = CreatePresetButton("Câu văn tự nhiên", 170, '\uE8BD');
            btnPresetSentence.Click += (s, e) =>
            {
                txtPreviewSample.Text = "Hoàng hôn buông xuống trên vịnh biển tuyệt đẹp, sóng vỗ êm đềm.";
            };
            pnlPreview.Controls.Add(btnPresetSentence);

            btnPresetDiacritics = CreatePresetButton("Đầy đủ dấu thanh", 320, '\uE8D2');
            btnPresetDiacritics.Click += (s, e) =>
            {
                txtPreviewSample.Text = "A Ă Â E Ê I O Ô Ơ U Ư Y • á à ả ã ạ / ắ ằ ẳ ẵ ặ / ấ ầ ẩ ẫ ậ / đ";
            };
            pnlPreview.Controls.Add(btnPresetDiacritics);

            // Preview Text Input
            txtPreviewSample = new TextBox
            {
                Location = new Point(18, 42),
                Size = new Size(920, 26),
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White
            };
            txtPreviewSample.TextChanged += (s, e) => pbFontPreview.Invalidate();
            pnlPreview.Controls.Add(txtPreviewSample);

            // Preview Canvas (Sharp ClearType, Wrapped & Centered)
            pbFontPreview = new PictureBox
            {
                Location = new Point(18, 76),
                Height = 76,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.None
            };
            pbFontPreview.Paint += PbFontPreview_Paint;
            pnlPreview.Controls.Add(pbFontPreview);

            // Diagnostic Alert Banner Card
            pnlDiagnosticBanner = new Panel
            {
                Location = new Point(18, 160),
                Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), // Soft Green default
                Padding = new Padding(12, 6, 12, 6)
            };
            pnlDiagnosticBanner.Paint += (s, e) =>
            {
                Color borderCol = selectedFont == null ? Color.FromArgb(226, 232, 240) :
                    (selectedFont.HasVietnameseSupport ? Color.FromArgb(187, 247, 208) :
                    ((selectedFont.Format.Contains("TCVN3") || selectedFont.Format.Contains("VNI")) ? Color.FromArgb(254, 240, 138) : Color.FromArgb(254, 202, 202)));
                using (Pen p = new Pen(borderCol, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlDiagnosticBanner.Width - 1, pnlDiagnosticBanner.Height - 1);
                }

                char diagGlyph = selectedFont == null ? '\uE946' :
                    (selectedFont.HasVietnameseSupport ? '\uE73E' : '\uE7BA');
                Color diagColor = selectedFont == null ? Color.FromArgb(100, 116, 139) :
                    (selectedFont.HasVietnameseSupport ? Color.FromArgb(22, 101, 52) :
                    ((selectedFont.Format.Contains("TCVN3") || selectedFont.Format.Contains("VNI")) ? Color.FromArgb(180, 83, 9) : Color.FromArgb(185, 28, 28)));
                FluentIconRenderer.DrawGlyph(e.Graphics, diagGlyph, new Rectangle(12, 12, 20, 20), diagColor, 16f);
            };
            pnlPreview.Controls.Add(pnlDiagnosticBanner);

            lblDiagnosticInfo = new Label
            {
                Text = "Chọn một phông chữ ở danh sách phía trên để xem trước và kiểm tra độ tương thích tiếng Việt.",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(22, 101, 52),
                Location = new Point(38, 6),
                Size = new Size(880, 32),
                AutoEllipsis = true
            };
            pnlDiagnosticBanner.Controls.Add(lblDiagnosticInfo);

            pnlPreview.Resize += (s, e) =>
            {
                int w = pnlPreview.ClientSize.Width - 36;
                txtPreviewSample.Width = w;
                pbFontPreview.Width = w;
                pnlDiagnosticBanner.Width = w;
                lblDiagnosticInfo.Width = Math.Max(100, w - 46);

                int rx = pnlPreview.ClientSize.Width - 18;
                btnPresetDiacritics.Location = new Point(rx - btnPresetDiacritics.Width, 10);
                btnPresetSentence.Location = new Point(btnPresetDiacritics.Left - btnPresetSentence.Width - 6, 10);
                btnPresetDoc.Location = new Point(btnPresetSentence.Left - btnPresetDoc.Width - 6, 10);
            };
        }

        private Label CreateStatusBadge(string text, Color bg, Color fg)
        {
            Label lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                BackColor = bg,
                ForeColor = fg,
                AutoSize = true,
                Margin = new Padding(4, 3, 4, 3),
                Padding = new Padding(8, 4, 8, 4)
            };
            lbl.Paint += (s, e) =>
            {
                using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1), 6))
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };
            return lbl;
        }

        private Button CreatePresetButton(string text, int left, char glyph = '\0')
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(left, 10),
                Size = new Size(140, 26),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            if (glyph != '\0')
            {
                btn.Image = FluentIconRenderer.GetGlyphBitmap(glyph, 13, 13, Color.FromArgb(51, 65, 85));
                btn.ImageAlign = ContentAlignment.MiddleLeft;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                btn.Padding = new Padding(4, 0, 4, 0);
            }
            return btn;
        }

        private GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
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
                ofd.Filter = "Tệp Office & Font (*.docx;*.pptx;*.ttf;*.otf)|*.docx;*.pptx;*.ttf;*.otf|Văn bản Word (*.docx)|*.docx|Bài thuyết trình PowerPoint (*.pptx)|*.pptx|Phông chữ TrueType / OpenType (*.ttf;*.otf)|*.ttf;*.otf|Tất cả tệp (*.*)|*.*";
                ofd.Title = "Chọn tệp cần kiểm tra phông chữ và sửa tiếng Việt";
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
            lblCurrentDoc.Text = Path.GetFileName(path) + " (" + path + ")";
            dgvFonts.Rows.Clear();

            // 1. Trích xuất đoạn văn thực tế từ tài liệu để xem trước
            docSampleText = VietnameseFontToolkit.ExtractDocumentSampleText(path);
            if (!string.IsNullOrEmpty(docSampleText))
            {
                txtPreviewSample.Text = docSampleText;
                btnPresetDoc.Enabled = true;
            }
            else
            {
                txtPreviewSample.Text = "Hoàng hôn buông xuống trên vịnh biển tuyệt đẹp, sóng vỗ êm đềm.";
                btnPresetDoc.Enabled = false;
            }

            // 2. Phân tích danh sách phông chữ trong tệp
            currentFontList = VietnameseFontToolkit.AnalyzeDocumentFonts(path);

            int total = currentFontList.Count;
            int embedded = 0;
            int problem = 0;

            foreach (var f in currentFontList)
            {
                if (f.IsEmbedded) embedded++;
                if (!f.HasVietnameseSupport) problem++;

                string viStatus;
                if (f.Format.Contains("TCVN3"))
                {
                    viStatus = "⚠ Mã cũ TCVN3 (ABC)";
                }
                else if (f.Format.Contains("VNI"))
                {
                    viStatus = "⚠ Mã cũ VNI-Windows";
                }
                else if (f.HasVietnameseSupport)
                {
                    viStatus = "✓ Chuẩn Unicode (100%)";
                }
                else
                {
                    viStatus = $"⚠ Thiếu {f.MissingVietnameseChars.Count} dấu";
                }

                string sourceStr;
                if (f.IsEmbedded)
                {
                    sourceStr = f.FileSize > 0 ? $"Nhúng ({f.FileSize / 1024:N0} KB)" : "Nhúng trong tệp";
                }
                else if (f.IsInstalledOnSystem)
                {
                    sourceStr = "Đã có trên máy";
                }
                else
                {
                    sourceStr = "Chưa có trên máy";
                }

                string typefaceDisplay = f.IsUsedInDocumentBody ? $"{f.Typeface} (Nội dung)" : f.Typeface;

                dgvFonts.Rows.Add(
                    typefaceDisplay,
                    viStatus,
                    sourceStr,
                    f.Format,
                    f.HasVietnameseSupport ? "--" : f.SuggestedVietnameseFont
                );
            }

            // 3. Cập nhật Badge Trạng thái
            badgeTotalFonts.Text = $"{total} phông chữ";
            
            if (problem == 0)
            {
                badgeViStatus.Text = "100% Chuẩn Tiếng Việt";
                badgeViStatus.BackColor = Color.FromArgb(220, 252, 231);
                badgeViStatus.ForeColor = Color.FromArgb(22, 101, 52);
                btnFixDocument.BackColor = Color.FromArgb(241, 245, 249);
                btnFixDocument.ForeColor = Color.FromArgb(51, 65, 85);
            }
            else
            {
                badgeViStatus.Text = $"{problem} phông cần chuyển mã";
                badgeViStatus.BackColor = Color.FromArgb(254, 243, 199);
                badgeViStatus.ForeColor = Color.FromArgb(146, 64, 14);
                btnFixDocument.BackColor = Color.FromArgb(37, 99, 235);
                btnFixDocument.ForeColor = Color.White;
            }

            if (embedded > 0)
            {
                badgeEmbedded.Text = $"{embedded} font nhúng";
                badgeEmbedded.BackColor = Color.FromArgb(219, 234, 254);
                badgeEmbedded.ForeColor = Color.FromArgb(30, 64, 175);
                btnExportFonts.Visible = true;
                btnInstallFonts.Visible = true;
            }
            else
            {
                badgeEmbedded.Text = "Hệ thống (Không nhúng font)";
                badgeEmbedded.BackColor = Color.FromArgb(241, 245, 249);
                badgeEmbedded.ForeColor = Color.FromArgb(100, 116, 139);
                btnExportFonts.Visible = false;
                btnInstallFonts.Visible = false;
            }

            // 4. Chọn dòng đầu tiên
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

        private void DgvFonts_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Cột 1: Tình trạng Tiếng Việt (Render Soft Pill Badge với Fluent Icon)
            if (e.ColumnIndex == 1 && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);

                string rawStatus = e.Value.ToString();
                string cleanText = rawStatus.TrimStart('✓', '⚠', ' ', '\t');
                bool isSuccess = rawStatus.StartsWith("✓") || rawStatus.Contains("100%");
                bool isLegacyCode = rawStatus.Contains("TCVN3") || rawStatus.Contains("VNI");

                Color pillBg;
                Color pillFg;

                if (isSuccess)
                {
                    pillBg = Color.FromArgb(222, 247, 236); // Green 100
                    pillFg = Color.FromArgb(3, 84, 63);     // Green 800
                }
                else if (isLegacyCode)
                {
                    pillBg = Color.FromArgb(254, 240, 138); // Yellow 200
                    pillFg = Color.FromArgb(133, 77, 14);   // Yellow 900
                }
                else
                {
                    pillBg = Color.FromArgb(254, 226, 226); // Red 100
                    pillFg = Color.FromArgb(153, 27, 27);   // Red 800
                }

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (Font font = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(cleanText, font);
                    int pillW = (int)sz.Width + 28;
                    int pillH = 22;
                    int pillX = e.CellBounds.Left + 8;
                    int pillY = e.CellBounds.Top + (e.CellBounds.Height - pillH) / 2;

                    Rectangle pillRect = new Rectangle(pillX, pillY, pillW, pillH);
                    using (GraphicsPath path = GetRoundedRectangle(pillRect, 6))
                    using (SolidBrush bBg = new SolidBrush(pillBg))
                    using (SolidBrush bFg = new SolidBrush(pillFg))
                    {
                        g.FillPath(bBg, path);

                        // Draw genuine Windows 11 Fluent Icon
                        char glyph = isSuccess ? '\uE73E' : '\uE7BA';
                        FluentIconRenderer.DrawGlyph(g, glyph, new Rectangle(pillX + 6, pillY + (pillH - 12) / 2, 12, 12), pillFg, 10f);

                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                        Rectangle textRect = new Rectangle(pillX + 21, pillY, pillW - 22, pillH);
                        g.DrawString(cleanText, font, bFg, textRect, sf);
                    }
                }

                e.Handled = true;
            }
        }

        private void UpdateDiagnosticPanel()
        {
            if (selectedFont == null)
            {
                lblPreviewHeader.Text = "XEM TRƯỚC VĂN BẢN VỚI PHÔNG ĐANG CHỌN:";
                pnlDiagnosticBanner.BackColor = Color.FromArgb(248, 250, 252);
                lblDiagnosticInfo.Text = "Chọn một phông chữ trong danh sách phía trên để xem trước và kiểm tra độ tương thích tiếng Việt.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(100, 116, 139);
                pnlDiagnosticBanner.Invalidate();
                return;
            }

            lblPreviewHeader.Text = $"XEM TRƯỚC VĂN BẢN VỚI PHÔNG: {selectedFont.Typeface.ToUpperInvariant()}";

            if (selectedFont.Format.Contains("TCVN3"))
            {
                pnlDiagnosticBanner.BackColor = Color.FromArgb(254, 252, 232); // Amber 50
                lblDiagnosticInfo.Text = $"CẢNH BÁO BẢNG MÃ TCVN3 (ABC): Phông '{selectedFont.Typeface}' sử dụng mã 1-byte cũ, gây lỗi dấu khi mở trên máy khác.\nNhấn nút 'Sửa lỗi Tiếng Việt & Chuyển mã' để tự động chuyển toàn bộ văn bản sang Unicode chuẩn (Times New Roman).";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(146, 64, 14);
            }
            else if (selectedFont.Format.Contains("VNI"))
            {
                pnlDiagnosticBanner.BackColor = Color.FromArgb(254, 252, 232); // Amber 50
                lblDiagnosticInfo.Text = $"CẢNH BÁO BẢNG MÃ VNI: Phông '{selectedFont.Typeface}' sử dụng mã VNI-Windows cũ, không tương thích tiêu chuẩn hiện đại.\nNhấn nút 'Sửa lỗi Tiếng Việt & Chuyển mã' để tự động chuyển toàn bộ văn bản sang Unicode chuẩn.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(146, 64, 14);
            }
            else if (selectedFont.HasVietnameseSupport)
            {
                pnlDiagnosticBanner.BackColor = Color.FromArgb(240, 253, 244); // Green 50
                lblDiagnosticInfo.Text = $"Phông chữ '{selectedFont.Typeface}' hỗ trợ đầy đủ 100% tiếng Việt chuẩn Unicode.\nVăn bản sử dụng phông chữ này hiển thị rõ nét, mượt mà và an toàn trên mọi thiết bị.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                pnlDiagnosticBanner.BackColor = Color.FromArgb(254, 242, 242); // Red 50
                lblDiagnosticInfo.Text = $"CẢNH BÁO VỠ PHÔNG: Phông '{selectedFont.Typeface}' thiếu {selectedFont.MissingVietnameseChars.Count} ký tự có dấu tiếng Việt.\nKhuyến nghị: Thay thế bằng phông '{selectedFont.SuggestedVietnameseFont}' có kiểu dáng tương đương và đầy đủ dấu.";
                lblDiagnosticInfo.ForeColor = Color.FromArgb(153, 27, 27);
            }

            pnlDiagnosticBanner.Invalidate();
        }

        private void PbFontPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Draw Background and subtle border
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.FillRectangle(bgBrush, pbFontPreview.ClientRectangle);
            }
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, pbFontPreview.Width - 1, pbFontPreview.Height - 1);
            }

            string text = txtPreviewSample.Text;
            if (string.IsNullOrEmpty(text)) text = "Tiếng Việt thân yêu";

            string fontName = (selectedFont != null && !string.IsNullOrEmpty(selectedFont.Typeface)) ? selectedFont.Typeface : "Segoe UI";

            float fontSize = text.Length > 60 ? 13.5f : 16.0f;

            using (Font f = SlideFontManager.CreateFont(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                RectangleF layoutRect = new RectangleF(16, 10, pbFontPreview.Width - 32, pbFontPreview.Height - 20);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisWord,
                    FormatFlags = StringFormatFlags.NoClip
                };
                g.DrawString(text, f, b, layoutRect, sf);
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

            using (Form optDlg = new Form())
            {
                optDlg.Text = "Tùy chọn Sửa lỗi Tiếng Việt & Chuyển mã";
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
                    ForeColor = Color.FromArgb(15, 23, 42),
                    Location = new Point(20, 18),
                    AutoSize = true
                };
                optDlg.Controls.Add(lblOptTitle);

                CheckBox chkTcvn3 = new CheckBox
                {
                    Text = "Tự động chuyển mã TCVN3 (ABC: .VnTime, .VnArial) sang Unicode chuẩn",
                    Checked = true,
                    Location = new Point(24, 55),
                    Size = new Size(480, 24),
                    Font = new Font("Segoe UI", 9.0f)
                };
                optDlg.Controls.Add(chkTcvn3);

                CheckBox chkVni = new CheckBox
                {
                    Text = "Tự động chuyển mã VNI-Windows (VNI-Times, v.v.) sang Unicode chuẩn",
                    Checked = true,
                    Location = new Point(24, 85),
                    Size = new Size(480, 24),
                    Font = new Font("Segoe UI", 9.0f)
                };
                optDlg.Controls.Add(chkVni);

                CheckBox chkNfc = new CheckBox
                {
                    Text = "Chuẩn hóa Unicode Tổ Hợp (NFD) sang Unicode Dựng Sẵn (NFC)",
                    Checked = true,
                    Location = new Point(24, 115),
                    Size = new Size(480, 24),
                    Font = new Font("Segoe UI", 9.0f)
                };
                optDlg.Controls.Add(chkNfc);

                CheckBox chkReplaceFonts = new CheckBox
                {
                    Text = "Tự động thay thế các phông thiếu tiếng Việt bằng phông Việt hóa tương đương",
                    Checked = true,
                    Location = new Point(24, 145),
                    Size = new Size(480, 24),
                    Font = new Font("Segoe UI", 9.0f)
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
                    Text = "Bắt đầu Sửa lỗi",
                    Location = new Point(230, 260),
                    Size = new Size(165, 36),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    DialogResult = DialogResult.OK,
                    Cursor = Cursors.Hand,
                    Image = FluentIconRenderer.GetGlyphBitmap('\uE90F', 16, 16, Color.White),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleCenter,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(8, 0, 8, 0)
                };
                btnRunFix.FlatAppearance.BorderSize = 0;
                optDlg.Controls.Add(btnRunFix);

                Button btnCancel = new Button
                {
                    Text = "Hủy bỏ",
                    Location = new Point(405, 260),
                    Size = new Size(100, 36),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(51, 65, 85),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel,
                    Cursor = Cursors.Hand,
                    Image = FluentIconRenderer.GetGlyphBitmap('\uE711', 14, 14, Color.FromArgb(51, 65, 85)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleCenter,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(6, 0, 6, 0)
                };
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
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
                                string repDetail = $"{report.Message}\n\nTệp đã lưu tại:\n{sfd.FileName}";
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

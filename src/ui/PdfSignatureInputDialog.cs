using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class PdfSignatureInputDialog : Form
    {
        public string SignerName { get; private set; }
        public string SignDate { get; private set; }
        public string Reason { get; private set; }
        public Color SignatureColor { get; private set; }
        public Image SignatureImage { get; private set; }
        public List<List<PointF>> SignatureStrokes { get; private set; }
        public int SigningMode { get; private set; } // 0 = Type, 1 = Draw, 2 = Upload

        private TabControl tabControl;
        
        // Tab 1: Type
        private TextBox txtSignerName;
        private TextBox txtSignDate;
        private ComboBox cbReason;
        private Panel pnlColorPreview;
        
        // Tab 2: Draw
        private Panel pnlDrawCanvas;
        private List<List<PointF>> currentDrawingStrokes = new List<List<PointF>>();
        private List<PointF> currentStroke = null;
        private Color drawColor = Color.FromArgb(30, 58, 138); // Navy ink
        private Panel pnlDrawColorPreview;

        // Tab 3: Upload
        private PictureBox pbUploadPreview;
        private Label lblUploadStatus;
        private CheckBox chkRemoveWhiteBg;
        private Image rawUploadedImage = null;

        public PdfSignatureInputDialog(string defaultSigner = "Ban Giám Đốc", string defaultReason = "Đã phê duyệt tài liệu", Color? defaultColor = null, int initialTab = 0)
        {
            this.Text = "Ký Tên Điện Tử - GenOffice E-Sign (3 Phương thức Ký)";
            this.Size = new Size(580, 570);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            SignatureColor = defaultColor ?? Color.FromArgb(30, 58, 138);
            SignatureStrokes = new List<List<PointF>>();

            InitUI(defaultSigner, defaultReason, initialTab);
        }

        private void InitUI(string defaultSigner, string defaultReason, int initialTab)
        {
            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 58, 138)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Font hFont = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (Font sFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("CHỨNG THỰC CHỮ KÝ ĐIỆN TỬ PDF", hFont, Brushes.White, 16, 12);
                    e.Graphics.DrawString("Hỗ trợ 3 hình thức: Gõ thông tin chứng thư, Vẽ ký tay trực tiếp hoặc Tải ảnh chữ ký", sFont, Brushes.LightSteelBlue, 18, 38);
                }
            };
            this.Controls.Add(pnlHeader);

            // 2. TabControl with 3 Signature Methods
            tabControl = new TabControl
            {
                Location = new Point(14, 75),
                Size = new Size(536, 395),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            // ==================== TAB 1: GÕ THÔNG TIN (TYPE) ====================
            TabPage tabType = new TabPage("1. Gõ thông tin (Type)");
            tabType.BackColor = Color.White;
            tabType.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BuildTypeTab(tabType, defaultSigner, defaultReason);
            tabControl.TabPages.Add(tabType);

            // ==================== TAB 2: VẼ TAY (DRAW) ====================
            TabPage tabDraw = new TabPage("2. Vẽ tay chữ ký (Draw)");
            tabDraw.BackColor = Color.White;
            tabDraw.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BuildDrawTab(tabDraw);
            tabControl.TabPages.Add(tabDraw);

            // ==================== TAB 3: TẢI ẢNH (UPLOAD) ====================
            TabPage tabUpload = new TabPage("3. Tải ảnh chữ ký (Upload)");
            tabUpload.BackColor = Color.White;
            tabUpload.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BuildUploadTab(tabUpload);
            tabControl.TabPages.Add(tabUpload);

            if (initialTab >= 0 && initialTab < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = initialTab;
            }

            this.Controls.Add(tabControl);

            // 3. Bottom Action Buttons
            int btnY = 485;
            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                Location = new Point(315, btnY),
                Size = new Size(105, 36),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

            Button btnOK = new Button
            {
                Text = "Ký tài liệu",
                Location = new Point(430, btnY),
                Size = new Size(120, 36),
                BackColor = Color.FromArgb(30, 58, 138),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) => ApplySignatureAndClose();

            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOK);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BuildTypeTab(TabPage page, string defaultSigner, string defaultReason)
        {
            int startY = 16;
            int lblX = 16;
            int txtX = 150;
            int ctrlW = 310;

            // Người ký
            Label lblSigner = new Label { Text = "Người ký / Đơn vị:", Location = new Point(lblX, startY + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            txtSignerName = new TextBox { Location = new Point(txtX, startY), Width = ctrlW, Text = string.IsNullOrEmpty(defaultSigner) ? "Ban Giám Đốc" : defaultSigner };
            page.Controls.Add(lblSigner);
            page.Controls.Add(txtSignerName);

            // Thời gian ký
            startY += 45;
            Label lblDate = new Label { Text = "Thời gian ký:", Location = new Point(lblX, startY + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            txtSignDate = new TextBox { Location = new Point(txtX, startY), Width = ctrlW, Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm") };
            page.Controls.Add(lblDate);
            page.Controls.Add(txtSignDate);

            // Lý do / Mục đích
            startY += 45;
            Label lblReason = new Label { Text = "Mục đích ký:", Location = new Point(lblX, startY + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            cbReason = new ComboBox { Location = new Point(txtX, startY), Width = ctrlW, DropDownStyle = ComboBoxStyle.DropDown };
            cbReason.Items.AddRange(new object[] { "Đã phê duyệt tài liệu", "Xác nhận tính pháp lý", "Ký duyệt hợp đồng", "Đã thanh toán hợp lệ", "Xác nhận đã đọc và đồng ý", "Bản thảo phê duyệt sơ bộ" });
            cbReason.Text = string.IsNullOrEmpty(defaultReason) ? "Đã phê duyệt tài liệu" : defaultReason;
            page.Controls.Add(lblReason);
            page.Controls.Add(cbReason);

            // Màu chứng thư
            startY += 45;
            Label lblColor = new Label { Text = "Màu chứng thư:", Location = new Point(lblX, startY + 4), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            pnlColorPreview = new Panel { Location = new Point(txtX, startY), Size = new Size(32, 26), BackColor = SignatureColor, BorderStyle = BorderStyle.FixedSingle };

            Color[] quickColors = new Color[] { Color.FromArgb(30, 58, 138), Color.FromArgb(220, 38, 38), Color.FromArgb(15, 23, 42), Color.FromArgb(4, 120, 87) };
            int qX = txtX + 42;
            for (int k = 0; k < quickColors.Length; k++)
            {
                Color qc = quickColors[k];
                Button btnQ = new Button { Location = new Point(qX, startY), Size = new Size(26, 26), BackColor = qc, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnQ.FlatAppearance.BorderSize = 1;
                btnQ.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnQ.Click += (s, e) => { SignatureColor = qc; pnlColorPreview.BackColor = qc; };
                page.Controls.Add(btnQ);
                qX += 32;
            }

            Button btnMore = new Button { Text = "Màu khác...", Location = new Point(qX + 4, startY), Size = new Size(85, 26), Font = new Font("Segoe UI", 8.5f), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMore.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMore.Click += (s, e) =>
            {
                Point screenPt = btnMore.PointToScreen(new Point(0, btnMore.Height));
                ColorPaletteForm palette = new ColorPaletteForm(screenPt, false);
                palette.OnColorSelected += (c) => { SignatureColor = c; pnlColorPreview.BackColor = c; };
                palette.Show(this);
            };
            page.Controls.Add(lblColor);
            page.Controls.Add(pnlColorPreview);
            page.Controls.Add(btnMore);

            // Preview Box
            startY += 55;
            Panel pnlPreviewBox = new Panel { Location = new Point(lblX, startY), Size = new Size(500, 115), BackColor = Color.FromArgb(248, 250, 252), BorderStyle = BorderStyle.FixedSingle };
            pnlPreviewBox.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(8, 8, pnlPreviewBox.Width - 16, pnlPreviewBox.Height - 16);
                using (Pen p = new Pen(SignatureColor, 1.8f))
                {
                    g.DrawRectangle(p, r);
                }

                // Draw Shield Vector Icon
                RectangleF shieldRect = new RectangleF(18, 16, 18, 20);
                using (GraphicsPath sp = new GraphicsPath())
                {
                    sp.AddLine(shieldRect.X, shieldRect.Y, shieldRect.Right, shieldRect.Y);
                    sp.AddLine(shieldRect.Right, shieldRect.Y, shieldRect.Right, shieldRect.Y + shieldRect.Height * 0.6f);
                    sp.AddLine(shieldRect.Right, shieldRect.Y + shieldRect.Height * 0.6f, shieldRect.X + shieldRect.Width / 2f, shieldRect.Bottom);
                    sp.AddLine(shieldRect.X + shieldRect.Width / 2f, shieldRect.Bottom, shieldRect.X, shieldRect.Y + shieldRect.Height * 0.6f);
                    sp.CloseFigure();
                    using (SolidBrush sb = new SolidBrush(SignatureColor)) g.FillPath(sb, sp);
                    using (Pen wp = new Pen(Color.White, 1.8f))
                    {
                        g.DrawLine(wp, shieldRect.X + 4, shieldRect.Y + 10, shieldRect.X + 8, shieldRect.Y + 14);
                        g.DrawLine(wp, shieldRect.X + 8, shieldRect.Y + 14, shieldRect.Right - 4, shieldRect.Y + 6);
                    }
                }

                using (Font tf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (Font df = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (SolidBrush tb = new SolidBrush(SignatureColor))
                using (SolidBrush gb = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    g.DrawString("ĐÃ KÝ ĐIỆN TỬ BỞI: " + txtSignerName.Text, tf, tb, 42, 16);
                    g.DrawString("Thời gian: " + txtSignDate.Text, df, gb, 42, 45);
                    g.DrawString("Mục đích: " + cbReason.Text, df, gb, 42, 68);
                }
            };
            txtSignerName.TextChanged += (s, e) => pnlPreviewBox.Invalidate();
            txtSignDate.TextChanged += (s, e) => pnlPreviewBox.Invalidate();
            cbReason.TextChanged += (s, e) => pnlPreviewBox.Invalidate();
            page.Controls.Add(pnlPreviewBox);
        }

        private void BuildDrawTab(TabPage page)
        {
            Label lblHint = new Label
            {
                Text = "Dùng chuột hoặc bút cảm ứng vẽ trực tiếp chữ ký vào khung bên dưới:",
                Location = new Point(14, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            page.Controls.Add(lblHint);

            // Canvas drawing panel
            pnlDrawCanvas = new Panel
            {
                Location = new Point(14, 38),
                Size = new Size(500, 230),
                BackColor = Color.FromArgb(255, 255, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Cross
            };

            pnlDrawCanvas.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Guideline
                using (Pen guideP = new Pen(Color.FromArgb(226, 232, 240), 1f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLine(guideP, 20, pnlDrawCanvas.Height - 45, pnlDrawCanvas.Width - 20, pnlDrawCanvas.Height - 45);
                }

                if (currentDrawingStrokes.Count == 0 && currentStroke == null)
                {
                    using (Font hintF = new Font("Segoe UI", 11f, FontStyle.Italic))
                    using (SolidBrush hintB = new SolidBrush(Color.FromArgb(203, 213, 225)))
                    {
                        e.Graphics.DrawString("Ký tên vào đây...", hintF, hintB, 20, pnlDrawCanvas.Height / 2 - 12);
                    }
                    return;
                }

                using (Pen pen = new Pen(drawColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    foreach (var strk in currentDrawingStrokes)
                    {
                        if (strk.Count > 1) g.DrawLines(pen, strk.ToArray());
                        else if (strk.Count == 1) g.FillEllipse(new SolidBrush(drawColor), strk[0].X - 1.5f, strk[0].Y - 1.5f, 3f, 3f);
                    }
                    if (currentStroke != null && currentStroke.Count > 1)
                    {
                        g.DrawLines(pen, currentStroke.ToArray());
                    }
                }
            };

            pnlDrawCanvas.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    currentStroke = new List<PointF>();
                    currentStroke.Add(new PointF(e.X, e.Y));
                    pnlDrawCanvas.Invalidate();
                }
            };

            pnlDrawCanvas.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && currentStroke != null)
                {
                    currentStroke.Add(new PointF(e.X, e.Y));
                    pnlDrawCanvas.Invalidate();
                }
            };

            pnlDrawCanvas.MouseUp += (s, e) =>
            {
                if (currentStroke != null)
                {
                    if (currentStroke.Count > 0) currentDrawingStrokes.Add(currentStroke);
                    currentStroke = null;
                    pnlDrawCanvas.Invalidate();
                }
            };

            page.Controls.Add(pnlDrawCanvas);

            // Controls below drawing canvas: Color selection & Clear button
            int ctrlY = 282;
            Label lblInk = new Label { Text = "Màu mực:", Location = new Point(14, ctrlY + 6), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            pnlDrawColorPreview = new Panel { Location = new Point(90, ctrlY + 2), Size = new Size(30, 26), BackColor = drawColor, BorderStyle = BorderStyle.FixedSingle };

            Color[] inkColors = new Color[] { Color.FromArgb(30, 58, 138), Color.FromArgb(15, 23, 42), Color.FromArgb(220, 38, 38) };
            int iX = 130;
            for (int k = 0; k < inkColors.Length; k++)
            {
                Color ic = inkColors[k];
                Button btnInk = new Button { Location = new Point(iX, ctrlY + 2), Size = new Size(26, 26), BackColor = ic, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnInk.FlatAppearance.BorderSize = 1;
                btnInk.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnInk.Click += (s, e) => { drawColor = ic; pnlDrawColorPreview.BackColor = ic; pnlDrawCanvas.Invalidate(); };
                page.Controls.Add(btnInk);
                iX += 32;
            }

            Button btnClear = new Button
            {
                Text = "Xóa nét vẽ",
                Location = new Point(380, ctrlY),
                Size = new Size(130, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
            btnClear.Click += (s, e) =>
            {
                currentDrawingStrokes.Clear();
                currentStroke = null;
                pnlDrawCanvas.Invalidate();
            };

            page.Controls.Add(lblInk);
            page.Controls.Add(pnlDrawColorPreview);
            page.Controls.Add(btnClear);
        }

        private void BuildUploadTab(TabPage page)
        {
            Label lblHint = new Label
            {
                Text = "Chọn file ảnh chữ ký hoặc con dấu cá nhân (PNG trong suốt, JPG, BMP):",
                Location = new Point(14, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            page.Controls.Add(lblHint);

            Button btnBrowse = new Button
            {
                Text = "Chọn tệp ảnh...",
                Location = new Point(14, 38),
                Size = new Size(140, 34),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;

            lblUploadStatus = new Label
            {
                Text = "Chưa chọn tệp ảnh nào",
                Location = new Point(165, 46),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            chkRemoveWhiteBg = new CheckBox
            {
                Text = "Tự động khử nền trắng (Làm trong suốt chữ ký)",
                Location = new Point(14, 82),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138)
            };
            chkRemoveWhiteBg.CheckedChanged += (s, e) => UpdateUploadPreview();

            pbUploadPreview = new PictureBox
            {
                Location = new Point(14, 115),
                Size = new Size(500, 190),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            btnBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn ảnh chữ ký / con dấu cá nhân";
                    ofd.Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            rawUploadedImage = Image.FromFile(ofd.FileName);
                            lblUploadStatus.Text = Path.GetFileName(ofd.FileName);
                            lblUploadStatus.ForeColor = Color.DarkGreen;
                            UpdateUploadPreview();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Không thể mở tệp ảnh: " + ex.Message, "Lỗi nạp ảnh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            page.Controls.Add(lblHint);
            page.Controls.Add(btnBrowse);
            page.Controls.Add(lblUploadStatus);
            page.Controls.Add(chkRemoveWhiteBg);
            page.Controls.Add(pbUploadPreview);
        }

        private void UpdateUploadPreview()
        {
            if (rawUploadedImage == null) return;
            if (chkRemoveWhiteBg.Checked)
            {
                pbUploadPreview.Image = MakeWhiteTransparent(rawUploadedImage);
            }
            else
            {
                pbUploadPreview.Image = rawUploadedImage;
            }
        }

        private Bitmap MakeWhiteTransparent(Image src)
        {
            Bitmap bmp = new Bitmap(src);
            Bitmap res = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    // If near white, make transparent
                    if (c.R > 220 && c.G > 220 && c.B > 220)
                    {
                        res.SetPixel(x, y, Color.FromArgb(0, 255, 255, 255));
                    }
                    else
                    {
                        res.SetPixel(x, y, c);
                    }
                }
            }
            return res;
        }

        private void ApplySignatureAndClose()
        {
            SigningMode = tabControl.SelectedIndex;

            SignerName = txtSignerName.Text.Trim();
            if (string.IsNullOrEmpty(SignerName)) SignerName = "Ban Giám Đốc";
            SignDate = txtSignDate.Text.Trim();
            if (string.IsNullOrEmpty(SignDate)) SignDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            Reason = cbReason.Text.Trim();
            if (string.IsNullOrEmpty(Reason)) Reason = "Đã phê duyệt tài liệu";

            if (SigningMode == 1) // Draw mode
            {
                SignatureColor = drawColor;
                SignatureStrokes = new List<List<PointF>>(currentDrawingStrokes);

                // Convert drawing to transparent bitmap image
                if (pnlDrawCanvas.Width > 0 && pnlDrawCanvas.Height > 0 && currentDrawingStrokes.Count > 0)
                {
                    Bitmap drawBmp = new Bitmap(pnlDrawCanvas.Width, pnlDrawCanvas.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(drawBmp))
                    {
                        g.Clear(Color.Transparent);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (Pen pen = new Pen(drawColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        {
                            foreach (var strk in currentDrawingStrokes)
                            {
                                if (strk.Count > 1) g.DrawLines(pen, strk.ToArray());
                                else if (strk.Count == 1) g.FillEllipse(new SolidBrush(drawColor), strk[0].X - 1.5f, strk[0].Y - 1.5f, 3f, 3f);
                            }
                        }
                    }
                    SignatureImage = drawBmp;
                }
            }
            else if (SigningMode == 2) // Upload image mode
            {
                if (pbUploadPreview.Image != null)
                {
                    SignatureImage = (Image)pbUploadPreview.Image.Clone();
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (this.Owner != null && !this.Owner.IsDisposed)
            {
                try
                {
                    this.Owner.Activate();
                    this.Owner.BringToFront();
                }
                catch { }
            }
        }
    }
}
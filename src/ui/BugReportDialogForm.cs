using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Báo lỗi & Đóng góp ý kiến chuẩn Microsoft 365 / Windows 11 Fluent Design.
    /// - Hoàn toàn không dùng icon GDI lỗi thời: Toàn bộ icon được vẽ bằng vector glyph (SSAA 4x) qua FluentIconRenderer.
    /// - Hỗ trợ dán trực tiếp ảnh chụp màn hình từ Clipboard (Win + Shift + S hoặc Ctrl+V).
    /// - Có khung xem trước hình ảnh (Thumbnail preview).
    /// - Bố cục chuẩn mực, không bị che khuất tiêu đề hay các nút chuyển loại phản hồi.
    /// </summary>
    public class BugReportDialogForm : Form
    {
        private readonly AppForm _appForm;
        private string _selectedCategory = "Báo lỗi phần mềm";
        private Image _attachedScreenshot = null;

        // UI Panels
        private Panel _pnlHeader;
        private Panel _pnlBody;
        private Panel _pnlFooter;

        // Category Pills
        private Button _btnCatBug;
        private Button _btnCatFeature;
        private Button _btnCatGeneral;

        // Form Fields
        private TextBox _txtTitle;
        private TextBox _txtDescription;
        private TextBox _txtContact;
        private TextBox _txtImageUrl;

        // Screenshot Preview Controls
        private Panel _pnlScreenshotCard;
        private PictureBox _picScreenshotPreview;
        private Label _lblScreenshotStatus;
        private Button _btnPasteScreenshot;
        private Button _btnBrowseScreenshot;
        private Button _btnClearScreenshot;

        // Device Info
        private CheckBox _chkIncludeDeviceInfo;
        private Label _lblDeviceInfoPreview;

        // Footer Actions
        private Label _lblStatus;
        private Button _btnCancel;
        private Button _btnSubmit;
        private bool _isSubmitting = false;

        public BugReportDialogForm(AppForm appForm = null)
        {
            _appForm = appForm;
            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            this.Text = "Báo lỗi & Đóng góp ý kiến - ZENQ OFFICE";
            this.Size = new Size(640, 720);
            this.MinimumSize = new Size(600, 650);
            this.StartPosition = (_appForm != null) ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252); // Slate-50
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            try
            {
                this.Icon = ZenQOfficeLogoRenderer.CreateAppIcon(32);
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Bắt phím Ctrl + V: Nếu trong Clipboard có ảnh chụp màn hình thì tự động dán ảnh
            if (keyData == (Keys.Control | Keys.V))
            {
                if (Clipboard.ContainsImage())
                {
                    PasteScreenshotFromClipboard();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildUI()
        {
            this.SuspendLayout();

            // 1. HEADER (Navy Slate-900)
            _pnlHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(this.ClientSize.Width, 68),
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(15, 23, 42) // Slate-900
            };
            _pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Biểu tượng Feedback \uED15 bằng vector Fluent
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uED15', new Rectangle(20, 18, 32, 32), Color.FromArgb(56, 189, 248), 24f);

                using (Font titleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
                using (SolidBrush bTitle = new SolidBrush(Color.White))
                {
                    e.Graphics.DrawString("BÁO LỖI VÀ ĐÓNG GÓP Ý KIẾN", titleFont, bTitle, 66, 14);
                }

                using (Font subFont = new Font("Segoe UI", 8.75f, FontStyle.Regular))
                using (SolidBrush bSub = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    e.Graphics.DrawString("Ý kiến của bạn sẽ được gửi thẳng lên hệ thống phát triển ZenQ Office", subFont, bSub, 66, 38);
                }
            };
            this.Controls.Add(_pnlHeader);

            // 2. FOOTER ACTION BAR
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(241, 245, 249) // Slate-100
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
                }
            };

            _lblStatus = new Label
            {
                Text = "Sẵn sàng gửi ý kiến phản hồi",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 20),
                Size = new Size(330, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlFooter.Controls.Add(_lblStatus);

            _btnCancel = new Button
            {
                Text = "Đóng",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(203, 213, 225), BorderSize = 1 },
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(370, 14),
                Size = new Size(90, 32),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            _btnCancel.Click += (s, e) => this.Close();
            _pnlFooter.Controls.Add(_btnCancel);

            _btnSubmit = new Button
            {
                Text = "   Gửi báo cáo",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(37, 99, 235), // Royal Blue
                ForeColor = Color.White,
                Location = new Point(475, 14),
                Size = new Size(130, 32),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            _btnSubmit.Paint += (s, e) =>
            {
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE724', new Rectangle(12, 8, 16, 16), Color.White, 13f);
            };
            _btnSubmit.Click += async (s, e) => await SubmitReportAsync();
            _pnlFooter.Controls.Add(_btnSubmit);

            this.Controls.Add(_pnlFooter);

            // 3. BODY CONTENT PANEL (Bắt đầu chính xác từ Y = 68 để không bị Header che khuất)
            _pnlBody = new Panel
            {
                Location = new Point(0, 68),
                Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 68 - 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            this.Controls.Add(_pnlBody);
            _pnlBody.BringToFront();

            int curY = 16;

            // 3.1 Nhãn: Loại phản hồi
            Label lblCat = new Label
            {
                Text = "LOẠI PHẢN HỒI",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblCat);
            curY += 22;

            // 3 Pills lựa chọn loại phản hồi
            _btnCatBug = CreateCategoryPill("🐞 Báo lỗi phần mềm", 24, curY, true);
            _btnCatFeature = CreateCategoryPill("💡 Đề xuất tính năng", 205, curY, false);
            _btnCatGeneral = CreateCategoryPill("💬 Trải nghiệm chung", 386, curY, false);

            _btnCatBug.Click += (s, e) => SelectCategory("Báo lỗi phần mềm", _btnCatBug);
            _btnCatFeature.Click += (s, e) => SelectCategory("Góp ý tính năng", _btnCatFeature);
            _btnCatGeneral.Click += (s, e) => SelectCategory("Trải nghiệm chung", _btnCatGeneral);

            _pnlBody.Controls.Add(_btnCatBug);
            _pnlBody.Controls.Add(_btnCatFeature);
            _pnlBody.Controls.Add(_btnCatGeneral);
            curY += 44;

            // 3.2 Tiêu đề lỗi / tóm tắt
            Label lblTitle = new Label
            {
                Text = "Tiêu đề tóm tắt *",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblTitle);
            curY += 22;

            _txtTitle = new TextBox
            {
                Location = new Point(24, curY),
                Size = new Size(570, 26),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlBody.Controls.Add(_txtTitle);
            curY += 36;

            // 3.3 Nội dung phản hồi chi tiết
            Label lblDesc = new Label
            {
                Text = "Nội dung phản hồi chi tiết *",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblDesc);
            curY += 22;

            _txtDescription = new TextBox
            {
                Location = new Point(24, curY),
                Size = new Size(570, 96),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlBody.Controls.Add(_txtDescription);
            curY += 108;

            // 3.4 Thông tin liên hệ
            Label lblContact = new Label
            {
                Text = "Người gửi / Email liên hệ (tùy chọn - để nhận phản hồi)",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblContact);
            curY += 20;

            _txtContact = new TextBox
            {
                Location = new Point(24, curY),
                Size = new Size(570, 26),
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Text = AppSettings.Instance.FeedbackUserContact ?? ""
            };
            _pnlBody.Controls.Add(_txtContact);
            curY += 36;

            // 3.5 Ảnh chụp màn hình & Clipboard
            Label lblImage = new Label
            {
                Text = "Ảnh chụp màn hình (hỗ trợ Win + Shift + S rồi bấm Dán ảnh hoặc nhấn Ctrl+V)",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, curY),
                AutoSize = true
            };
            _pnlBody.Controls.Add(lblImage);
            curY += 22;

            _btnPasteScreenshot = new Button
            {
                Text = "   Dán ảnh Clipboard",
                Location = new Point(24, curY),
                Size = new Size(160, 30),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(59, 130, 246), BorderSize = 1 },
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(29, 78, 216),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            _btnPasteScreenshot.Paint += (s, e) =>
            {
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE777', new Rectangle(8, 7, 16, 16), Color.FromArgb(29, 78, 216), 13f);
            };
            _btnPasteScreenshot.Click += (s, e) => PasteScreenshotFromClipboard();
            _pnlBody.Controls.Add(_btnPasteScreenshot);

            _btnBrowseScreenshot = new Button
            {
                Text = "Chọn tệp ảnh...",
                Location = new Point(192, curY),
                Size = new Size(115, 30),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(203, 213, 225), BorderSize = 1 },
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular)
            };
            _btnBrowseScreenshot.Click += (s, e) => BrowseScreenshotFile();
            _pnlBody.Controls.Add(_btnBrowseScreenshot);

            _txtImageUrl = new TextBox
            {
                Location = new Point(315, curY + 2),
                Size = new Size(279, 26),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Hoặc nhập liên kết ảnh (Drive, Imgur...)"
            };
            _pnlBody.Controls.Add(_txtImageUrl);
            curY += 36;

            // Khung xem trước ảnh chụp màn hình (Screenshot Preview Card)
            _pnlScreenshotCard = new Panel
            {
                Location = new Point(24, curY),
                Size = new Size(570, 72),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle
            };

            _picScreenshotPreview = new PictureBox
            {
                Location = new Point(8, 6),
                Size = new Size(88, 58),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(226, 232, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlScreenshotCard.Controls.Add(_picScreenshotPreview);

            _lblScreenshotStatus = new Label
            {
                Text = "Chưa có ảnh chụp đính kèm.\nNhấn [Win + Shift + S] chụp màn hình rồi bấm \"Dán ảnh Clipboard\" hoặc nhấn Ctrl + V.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(106, 14),
                Size = new Size(380, 42)
            };
            _pnlScreenshotCard.Controls.Add(_lblScreenshotStatus);

            _btnClearScreenshot = new Button
            {
                Text = "Xóa ảnh",
                Location = new Point(494, 20),
                Size = new Size(66, 28),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(203, 213, 225), BorderSize = 1 },
                BackColor = Color.White,
                ForeColor = Color.FromArgb(220, 38, 38),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                Visible = false
            };
            _btnClearScreenshot.Click += (s, e) => ClearScreenshot();
            _pnlScreenshotCard.Controls.Add(_btnClearScreenshot);

            _pnlBody.Controls.Add(_pnlScreenshotCard);
            curY += 80;

            // 3.6 Checkbox Đính kèm cấu hình máy
            _chkIncludeDeviceInfo = new CheckBox
            {
                Text = "Tự động gửi thông tin cấu hình máy (OS, RAM, Màn hình, Phiên bản app)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, curY),
                Size = new Size(570, 24),
                Checked = true
            };
            _pnlBody.Controls.Add(_chkIncludeDeviceInfo);
            curY += 26;

            string devSummary = FeedbackService.GetDeviceInfoSummary();
            _lblDeviceInfoPreview = new Label
            {
                Text = devSummary,
                Font = new Font("Consolas", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(241, 245, 249),
                Location = new Point(24, curY),
                Size = new Size(570, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlBody.Controls.Add(_lblDeviceInfoPreview);
            curY += 40;

            this.ResumeLayout(true);
        }

        private Button CreateCategoryPill(string text, int x, int y, bool isSelected)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(175, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.0f, isSelected ? FontStyle.Bold : FontStyle.Regular),
                BackColor = isSelected ? Color.FromArgb(239, 246, 255) : Color.White,
                ForeColor = isSelected ? Color.FromArgb(29, 78, 216) : Color.FromArgb(71, 85, 105),
                Tag = isSelected
            };
            btn.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(59, 130, 246) : Color.FromArgb(203, 213, 225);
            btn.FlatAppearance.BorderSize = isSelected ? 2 : 1;
            return btn;
        }

        private void SelectCategory(string category, Button activeBtn)
        {
            _selectedCategory = category;
            Button[] all = new Button[] { _btnCatBug, _btnCatFeature, _btnCatGeneral };
            foreach (var b in all)
            {
                bool isCur = (b == activeBtn);
                b.BackColor = isCur ? Color.FromArgb(239, 246, 255) : Color.White;
                b.ForeColor = isCur ? Color.FromArgb(29, 78, 216) : Color.FromArgb(71, 85, 105);
                b.Font = new Font("Segoe UI", 9.0f, isCur ? FontStyle.Bold : FontStyle.Regular);
                b.FlatAppearance.BorderColor = isCur ? Color.FromArgb(59, 130, 246) : Color.FromArgb(203, 213, 225);
                b.FlatAppearance.BorderSize = isCur ? 2 : 1;
            }
        }

        private void SetAttachedScreenshot(Image img)
        {
            if (img == null) return;
            _attachedScreenshot = new Bitmap(img);
            _picScreenshotPreview.Image = _attachedScreenshot;
            _lblScreenshotStatus.Text = $"✓ Đã đính kèm ảnh chụp màn hình ({img.Width}x{img.Height} px).\nSẵn sàng tải lên cùng báo cáo lỗi.";
            _lblScreenshotStatus.ForeColor = Color.FromArgb(22, 163, 74);
            _btnClearScreenshot.Visible = true;
        }

        private void ClearScreenshot()
        {
            if (_attachedScreenshot != null)
            {
                _attachedScreenshot.Dispose();
                _attachedScreenshot = null;
            }
            _picScreenshotPreview.Image = null;
            _lblScreenshotStatus.Text = "Chưa có ảnh chụp đính kèm.\nNhấn [Win + Shift + S] chụp màn hình rồi bấm \"Dán ảnh Clipboard\" hoặc nhấn Ctrl + V.";
            _lblScreenshotStatus.ForeColor = Color.FromArgb(100, 116, 139);
            _btnClearScreenshot.Visible = false;
        }

        private void PasteScreenshotFromClipboard()
        {
            try
            {
                // ƯU TIÊN 1: Kiểm tra xem Clipboard có chứa Ảnh chụp màn hình không
                if (Clipboard.ContainsImage())
                {
                    Image img = Clipboard.GetImage();
                    if (img != null)
                    {
                        SetAttachedScreenshot(img);
                        _lblStatus.Text = "✓ Đã dán ảnh chụp màn hình thành công!";
                        _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                        return;
                    }
                }

                // ƯU TIÊN 2: Nếu không có ảnh nhưng có link văn bản
                if (Clipboard.ContainsText())
                {
                    string txt = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(txt) && (txt.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || txt.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    {
                        _txtImageUrl.Text = txt.Trim();
                        _lblStatus.Text = "✓ Đã dán liên kết ảnh từ Clipboard";
                        _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
                        return;
                    }
                }

                ZenQOffice.UI.FluentMessageBox.Show("Clipboard hiện không có ảnh chụp màn hình.\n\nCách làm:\n1. Nhấn tổ hợp phím [Win + Shift + S] trên bàn phím để chụp màn hình.\n2. Quay lại đây nhấn nút 'Dán ảnh Clipboard' (hoặc nhấn Ctrl + V) là xong!", "Chưa có ảnh trong Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ZenQOffice.UI.FluentMessageBox.Show("Không thể đọc Clipboard: " + ex.Message, "Lỗi Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BrowseScreenshotFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh chụp màn hình lỗi";
                ofd.Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tất cả tệp (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                        {
                            Image img = Image.FromStream(stream);
                            SetAttachedScreenshot(img);
                        }
                    }
                    catch (Exception ex)
                    {
                        ZenQOffice.UI.FluentMessageBox.Show("Không thể mở tệp ảnh: " + ex.Message, "Lỗi tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private string GetFinalImagePayload()
        {
            // Nếu có ảnh chụp đính kèm trực tiếp từ Clipboard hoặc File, mã hóa Base64 JPEG chất lượng cao
            if (_attachedScreenshot != null)
            {
                return ConvertImageToBase64DataUri(_attachedScreenshot);
            }

            // Nếu người dùng nhập liên kết web
            return _txtImageUrl.Text.Trim();
        }

        private static string ConvertImageToBase64DataUri(Image img, int maxWidth = 960, int maxHeight = 720, long quality = 70L)
        {
            if (img == null) return "";
            try
            {
                int w = img.Width;
                int h = img.Height;
                if (w > maxWidth || h > maxHeight)
                {
                    double ratioW = (double)maxWidth / w;
                    double ratioH = (double)maxHeight / h;
                    double ratio = Math.Min(ratioW, ratioH);
                    w = (int)Math.Max(1, Math.Round(w * ratio));
                    h = (int)Math.Max(1, Math.Round(h * ratio));
                }

                using (Bitmap bmp = new Bitmap(w, h))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(img, new Rectangle(0, 0, w, h));
                    }

                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, jpgEncoder, encoderParams);
                        byte[] bytes = ms.ToArray();
                        return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return codecs[1]; // fallback jpeg
        }

        private async Task SubmitReportAsync()
        {
            if (_isSubmitting) return;

            string title = _txtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                ZenQOffice.UI.FluentMessageBox.Show("Vui lòng nhập Tiêu đề tóm tắt về lỗi hoặc góp ý của bạn.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTitle.Focus();
                return;
            }

            string desc = _txtDescription.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
            {
                ZenQOffice.UI.FluentMessageBox.Show("Vui lòng nhập Nội dung phản hồi chi tiết để chúng tôi có thể hỗ trợ bạn tốt nhất.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDescription.Focus();
                return;
            }

            _isSubmitting = true;
            _btnSubmit.Enabled = false;
            _btnCancel.Enabled = false;
            _btnSubmit.BackColor = Color.FromArgb(148, 163, 184);
            _lblStatus.Text = "⏳ Đang gửi báo cáo lên Google Sheet...";
            _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);

            string contact = _txtContact.Text.Trim();
            string imgPayload = GetFinalImagePayload();
            string deviceInfo = _chkIncludeDeviceInfo.Checked ? _lblDeviceInfoPreview.Text.Trim() : "Không đính kèm";

            var payload = new FeedbackPayload
            {
                Category = _selectedCategory,
                Title = title,
                Description = desc,
                Contact = contact,
                DeviceInfo = deviceInfo,
                ImageUrl = imgPayload
            };

            var result = await FeedbackService.SendFeedbackAsync(payload);

            if (result.Success)
            {
                _lblStatus.Text = "✓ Đã ghi nhận báo cáo thành công!";
                _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);

                try
                {
                    if (!string.IsNullOrWhiteSpace(contact))
                    {
                        AppSettings.Instance.FeedbackUserContact = contact;
                        AppSettings.Instance.Save();
                    }
                }
                catch { }

                await Task.Delay(1100);
                this.Close();
            }
            else
            {
                _lblStatus.Text = "❌ " + result.Message;
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _btnSubmit.Enabled = true;
                _btnCancel.Enabled = true;
                _btnSubmit.BackColor = Color.FromArgb(37, 99, 235);
                _isSubmitting = false;
            }
        }
    }
}

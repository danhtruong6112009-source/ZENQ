using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class TemplateUploadDialogForm : Form
    {
        private readonly string _pptxPath;
        private TextBox _txtTitle;
        private TextBox _txtId;
        private FluentComboBox _cboCategory;
        private TextBox _txtTagInput;
        private TagChipsCanvas _tagChipsCanvas;
        private SuggestedChipsCanvas _suggestedChipsCanvas;
        private SettingsActionButton _btnUpload;
        private SettingsActionButton _btnCancel;
        private Panel _previewCard;

        private readonly List<string> _tags = new List<string>();
        private Bitmap _previewBmp;
        private int _slideCount = 1;

        public string ResultTitle { get; private set; } = "";
        public string ResultId { get; private set; } = "";
        public string ResultCategory { get; private set; } = "Thuyết trình";
        public List<string> ResultTags => new List<string>(_tags);
        public bool SyncToCloud => true;
        public int DetectedSlideCount => _slideCount;

        private static readonly string[] CategoryKeys = new string[]
        {
            "Thuyết trình", "Giáo dục", "Kinh doanh", "Tài chính", "Thương hiệu", "Marketing", "Cá nhân"
        };

        private static readonly string[] SuggestedTags = new string[]
        {
            "Thuyết trình", "Báo cáo", "Giáo dục", "Địa lí", "Lịch sử",
            "Việt Nam", "Kinh doanh", "Tài chính", "Marketing", "Khoa học",
            "Công nghệ", "Dự án", "Hiện đại", "Tối giản"
        };

        public TemplateUploadDialogForm(string pptxPath)
        {
            _pptxPath = pptxPath;
            string defaultTitle = Path.GetFileNameWithoutExtension(pptxPath) ?? AppL10n.T("Mẫu thuyết trình mới", "New Presentation Template");
            string defaultId = GenerateSlugId(defaultTitle);

            PresentationRoot tempPres = null;
            try
            {
                tempPres = ZenQOffice.Format.PptxHandler.LoadPptx(pptxPath);
                if (tempPres != null && tempPres.Slides != null && tempPres.Slides.Count > 0)
                {
                    _slideCount = tempPres.Slides.Count;
                    var sv = new SlidesView { Presentation = tempPres, ActiveSlideIndex = 0 };
                    _previewBmp = sv.RenderSlideToBitmap(tempPres.Slides[0], 480, 270);
                }
            }
            catch { }
            finally
            {
                TempCleanupManager.DisposeAndDeletePresentationTempMedia(tempPres, null);
            }

            InitializeComponent(defaultTitle, defaultId);
            AutoPopulateInitialTags(defaultTitle);
        }

        private void AutoPopulateInitialTags(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            AddTag(title.Trim());
        }

        public static string GenerateSlugId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "tpl_" + Guid.NewGuid().ToString("N").Substring(0, 6);

            string normalized = RemoveDiacritics(input).ToLowerInvariant();
            var chars = normalized.Select(c => (char.IsLetterOrDigit(c) ? c : '_')).ToArray();
            string slug = new string(chars);
            while (slug.Contains("__")) slug = slug.Replace("__", "_");
            slug = slug.Trim('_');
            if (string.IsNullOrEmpty(slug))
                slug = "tpl_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            return slug;
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ') sb.Append('d');
                    else if (c == 'Đ') sb.Append('D');
                    else sb.Append(c);
                }
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private void InitializeComponent(string defaultTitle, string defaultId)
        {
            Text = AppL10n.T("Tải mẫu lên", "Upload Template");
            ClientSize = new Size(580, 565);
            MinimumSize = new Size(540, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            Controls.Add(shell);

            // 1. Header đồng bộ hoàn toàn với bảng Trợ lý AI (AiConfigDialog)
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 8),
                BackColor = Color.White
            };
            header.Controls.Add(new Label
            {
                Text = AppL10n.T("Đặt tên, mã định danh và từ khóa để dễ dàng tìm kiếm trong kho mẫu.", "Set a title, identifier slug, and keywords to make your template easy to find."),
                Dock = DockStyle.Bottom,
                Height = 22,
                ForeColor = Color.FromArgb(100, 110, 125)
            });
            header.Controls.Add(new Label
            {
                Text = AppL10n.T("Tải mẫu lên", "Upload Template"),
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 37, 52)
            });
            shell.Controls.Add(header, 0, 0);

            // 2. Body
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 4, 24, 8),
                BackColor = Color.White
            };
            shell.Controls.Add(scroll, 0, 1);

            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = Padding.Empty
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            scroll.Controls.Add(fields);

            Control Field(string label, Control input, int height = 34)
            {
                var group = new Panel
                {
                    Dock = DockStyle.Fill,
                    Height = height + 27,
                    Margin = new Padding(0, 0, 0, 10)
                };
                group.Controls.Add(new Label
                {
                    Text = label,
                    Dock = DockStyle.Top,
                    Height = 22,
                    ForeColor = Color.FromArgb(65, 75, 90),
                    Font = new Font("Segoe UI", 9f)
                });
                input.Dock = DockStyle.Bottom;
                input.Height = height;
                group.Controls.Add(input);
                return group;
            }

            void Row(Control control, int height)
            {
                int row = fields.RowCount++;
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
                fields.Controls.Add(control, 0, row);
                fields.SetColumnSpan(control, 2);
            }

            // Hàng 1: Thẻ xem trước thu gọn, tinh tế (không hiển thị thông tin hệ thống rườm rà)
            _previewCard = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = Color.White
            };
            _previewCard.Paint += PaintPreviewCard;
            Row(_previewCard, 88);

            // Hàng 2: Tên mẫu
            _txtTitle = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Text = defaultTitle,
                PlaceholderText = AppL10n.T("Nhập tên bài thuyết trình...", "Enter presentation name...")
            };
            bool userEditedId = false;
            _txtTitle.TextChanged += (s, e) =>
            {
                if (!userEditedId && _txtId != null)
                {
                    _txtId.Text = GenerateSlugId(_txtTitle.Text);
                }
                _previewCard?.Invalidate();
            };
            Row(Field(AppL10n.T("Tên mẫu", "Template Name"), new SettingsInputFrame(_txtTitle)), 68);

            // Hàng 3: Mã định danh & Danh mục (2 cột song song chuẩn AiConfigDialog)
            _txtId = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Text = defaultId,
                PlaceholderText = AppL10n.T("ma_dinh_danh", "template_id")
            };
            _txtId.KeyPress += (s, e) => { userEditedId = true; };

            _cboCategory = new FluentComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                IntegralHeight = false,
                MaxDropDownItems = 8
            };
            foreach (var ck in CategoryKeys)
                _cboCategory.Items.Add(AppL10n.TrCategory(ck));
            _cboCategory.SelectedIndex = 0;
            _cboCategory.SelectedIndexChanged += (s, e) => _previewCard?.Invalidate();

            int idCatRow = fields.RowCount++;
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            var idField = Field(AppL10n.T("Mã định danh", "Identifier ID"), new SettingsInputFrame(_txtId));
            idField.Margin = new Padding(0, 0, 10, 10);
            var catField = Field(AppL10n.T("Danh mục", "Category"), _cboCategory);
            fields.Controls.Add(idField, 0, idCatRow);
            fields.Controls.Add(catField, 1, idCatRow);

            // Hàng 4: Ô nhập từ khóa tích hợp nút "Thêm" bên trong khung SettingsInputFrame
            _txtTagInput = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = AppL10n.T("Nhập từ khóa rồi nhấn Enter hoặc dấu phẩy...", "Type a keyword and press Enter or comma...")
            };
            var tagInputFrame = new SettingsInputFrame(_txtTagInput);
            var btnAddTag = new SettingsActionButton
            {
                Text = AppL10n.T("Thêm", "Add"),
                Size = new Size(56, 24),
                TabStop = true
            };
            tagInputFrame.Controls.Add(btnAddTag);
            tagInputFrame.Padding = new Padding(10, 7, 66, 5);
            tagInputFrame.Layout += (s, e) => btnAddTag.SetBounds(Math.Max(0, tagInputFrame.Width - 61), 5, 56, 24);
            btnAddTag.Click += (s, e) => CommitTagInput();
            _txtTagInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Oemcomma)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    CommitTagInput();
                }
            };
            Row(Field(AppL10n.T("Từ khóa tìm kiếm", "Search Keywords"), tagInputFrame), 66);

            // Hàng 5: Khung thẻ từ khóa đã gắn + Gợi ý nhanh (vẽ bằng D2DGraphics sắc nét)
            var tagsSection = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };

            _tagChipsCanvas = new TagChipsCanvas(_tags, RemoveTag)
            {
                Dock = DockStyle.Top,
                Height = 72
            };

            var lblSuggest = new Label
            {
                Text = AppL10n.T("Gợi ý nhanh", "Quick Suggestions"),
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(0, 6, 0, 0),
                ForeColor = Color.FromArgb(100, 110, 125),
                Font = new Font("Segoe UI", 8.5f)
            };

            _suggestedChipsCanvas = new SuggestedChipsCanvas(
                SuggestedTags.Select(t => AppL10n.T(t, t switch {
                    "Thuyết trình" => "Presentation", "Báo cáo" => "Report", "Giáo dục" => "Education",
                    "Địa lí" => "Geography", "Lịch sử" => "History", "Việt Nam" => "Vietnam",
                    "Kinh doanh" => "Business", "Tài chính" => "Finance", "Marketing" => "Marketing",
                    "Khoa học" => "Science", "Công nghệ" => "Technology", "Dự án" => "Project",
                    "Hiện đại" => "Modern", "Tối giản" => "Minimalist", _ => t
                })).ToArray(),
                _tags, AddTag)
            {
                Dock = DockStyle.Fill
            };

            tagsSection.Controls.Add(_suggestedChipsCanvas);
            tagsSection.Controls.Add(lblSuggest);
            tagsSection.Controls.Add(_tagChipsCanvas);
            Row(tagsSection, 152);

            // 3. Footer đồng bộ hoàn toàn với AiConfigDialog
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 16),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(232, 237, 243));
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            shell.Controls.Add(footer, 0, 2);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            _btnCancel = new SettingsActionButton
            {
                Text = AppL10n.T("Đóng", "Close"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.Click += (s, e) => Close();

            _btnUpload = new SettingsActionButton
            {
                Text = AppL10n.T("Tải mẫu lên", "Upload"),
                Primary = true,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            _btnUpload.Click += OnSubmitUpload;

            actions.Controls.Add(_btnCancel, 1, 0);
            actions.Controls.Add(_btnUpload, 2, 0);
            footer.Controls.Add(actions);

            CancelButton = _btnCancel;
            AcceptButton = _btnUpload;

            Shown += (s, e) =>
            {
                FluentUi.RoundWindow(this);
                _txtTitle.Focus();
                _txtTitle.SelectAll();
            };
        }

        private void PaintPreviewCard(object sender, PaintEventArgs e)
        {
            if (_previewCard == null || _previewCard.Width <= 2 || _previewCard.Height <= 2) return;
            using var g = new D2DGraphics(e.Graphics, _previewCard.Width, _previewCard.Height);
            g.Clear(Color.White);

            var cardRect = new RectangleF(0.5f, 0.5f, _previewCard.Width - 1f, _previewCard.Height - 1f);
            using (var bg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (var border = new Pen(Color.FromArgb(216, 222, 230), 1f))
            {
                g.FillRoundedRectangle(bg, cardRect, 6f);
                g.DrawRoundedRectangle(border, cardRect, 6f);
            }

            RectangleF thumbRect = new RectangleF(10f, 8f, 112f, 62f);
            using (var thumbBg = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                g.FillRoundedRectangle(thumbBg, thumbRect, 4f);
            }
            if (_previewBmp != null)
            {
                g.DrawImage(_previewBmp, thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height);
            }

            string displayTitle = !string.IsNullOrWhiteSpace(_txtTitle?.Text)
                ? _txtTitle.Text.Trim()
                : (Path.GetFileNameWithoutExtension(_pptxPath) ?? AppL10n.T("Mẫu thuyết trình", "Presentation Template"));
            string category = _cboCategory?.SelectedItem?.ToString() ?? AppL10n.TrCategory("Thuyết trình");

            using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var metaFont = new Font("Segoe UI", 8.75f, FontStyle.Regular);
            FluentUi.Text(g, displayTitle, titleFont, Color.FromArgb(28, 37, 52), new RectangleF(136f, 15f, _previewCard.Width - 148f, 24f));
            FluentUi.Text(g, AppL10n.IsEn ? $"{category}  ·  {_slideCount} slides" : $"{category}  ·  {_slideCount} trang chiếu", metaFont, Color.FromArgb(100, 110, 125), new RectangleF(136f, 41f, _previewCard.Width - 148f, 20f));
        }

        private void CommitTagInput()
        {
            if (_txtTagInput == null) return;
            string raw = _txtTagInput.Text;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    AddTag(p.Trim());
                }
                _txtTagInput.Clear();
            }
            _txtTagInput.Focus();
        }

        private void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            tag = tag.Trim().TrimStart('#').Trim();
            if (string.IsNullOrEmpty(tag)) return;
            if (_tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                return;

            _tags.Add(tag);
            _tagChipsCanvas?.Invalidate();
            _suggestedChipsCanvas?.Invalidate();
        }

        private void RemoveTag(string tag)
        {
            _tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            _tagChipsCanvas?.Invalidate();
            _suggestedChipsCanvas?.Invalidate();
        }

        private async void OnSubmitUpload(object sender, EventArgs e)
        {
            if (DatabaseAuthService.CurrentUser != null)
            {
                await DatabaseAuthService.RefreshCurrentUserAsync();
                if (DatabaseAuthService.CurrentUser.Ban)
                {
                    MessageBox.Show(
                        "T?i kho?n c?a b?n ?? b? Qu?n tr? vi?n kh?a do vi ph?m quy ??nh c?ng ??ng.\nB?n kh?ng c? quy?n ??ng t?i m?u thuy?t tr?nh l?n h? th?ng.",
                        "T?i kho?n b? kh?a",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            if (_txtTagInput != null && !string.IsNullOrWhiteSpace(_txtTagInput.Text))
            {
                CommitTagInput();
            }

            string title = (_txtTitle?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(title))
            {
                title = Path.GetFileNameWithoutExtension(_pptxPath) ?? AppL10n.T("Mẫu thuyết trình", "Presentation Template");
            }

            string id = (_txtId?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(id))
            {
                id = GenerateSlugId(title);
            }

            ResultTitle = title;
            ResultId = id;
            int catIdx = _cboCategory != null ? _cboCategory.SelectedIndex : 0;
            ResultCategory = (catIdx >= 0 && catIdx < CategoryKeys.Length) ? CategoryKeys[catIdx] : "Thuyết trình";

            if (_tags.Count == 0)
            {
                _tags.Add(ResultTitle);
            }

            if (_btnUpload != null)
            {
                _btnUpload.Enabled = false;
                _btnUpload.Text = AppL10n.T("Đang tải lên...", "Uploading...");
            }
            if (_btnCancel != null)
            {
                _btnCancel.Enabled = false;
            }

            bool uploadSuccess = await Task.Run(() =>
            {
                try
                {
                    var tmpl = OnlineSlideTemplateManager.AddCustomTemplateFromPptx(
                        _pptxPath,
                        ResultTitle,
                        ResultId,
                        ResultCategory,
                        ResultTags,
                        SyncToCloud,
                        waitForUpload: true);
                    return tmpl != null;
                }
                catch
                {
                    return false;
                }
            });

            if (uploadSuccess)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                if (_btnUpload != null)
                {
                    _btnUpload.Enabled = true;
                    _btnUpload.Text = AppL10n.T("Tải mẫu lên", "Upload");
                }
                if (_btnCancel != null)
                {
                    _btnCancel.Enabled = true;
                }
                MessageBox.Show(
                    AppL10n.T("Không thể tải mẫu lên kho đám mây GitHub. Vui lòng kiểm tra lại kết nối mạng và thử lại.", "Unable to upload template to GitHub cloud. Please check your internet connection and try again."),
                    AppL10n.T("Tải mẫu lên", "Upload Template"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _previewBmp?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
        }

        private sealed class TagChipsCanvas : Control
        {
            private readonly List<string> _tags;
            private readonly Action<string> _onRemove;
            private readonly List<(RectangleF Bounds, string Tag)> _hitRects = new List<(RectangleF, string)>();
            private int _hoverIdx = -1;

            public TagChipsCanvas(List<string> tags, Action<string> onRemove)
            {
                _tags = tags;
                _onRemove = onRemove;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
                Cursor = Cursors.Default;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var g = new D2DGraphics(e.Graphics, Math.Max(1, Width), Math.Max(1, Height));
                g.Clear(Color.White);

                var box = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                using (var fill = new SolidBrush(Color.FromArgb(250, 251, 253)))
                using (var border = new Pen(Color.FromArgb(216, 222, 230), 1f))
                {
                    g.FillRoundedRectangle(fill, box, 5f);
                    g.DrawRoundedRectangle(border, box, 5f);
                }

                _hitRects.Clear();

                if (_tags == null || _tags.Count == 0)
                {
                    using var emptyFont = new Font("Segoe UI", 8.75f);
                    FluentUi.Text(g, AppL10n.T("Chưa có từ khóa nào được thêm.", "No keywords added yet."), emptyFont, Color.FromArgb(148, 163, 184), new RectangleF(12f, 0f, Width - 24f, Height));
                    return;
                }

                using var chipFont = new Font("Segoe UI", 8.75f);
                float x = 10f, y = 8f, rowH = 26f, gap = 6f;

                for (int i = 0; i < _tags.Count; i++)
                {
                    string tag = _tags[i];
                    Size sz = TextRenderer.MeasureText(tag, chipFont);
                    float chipW = Math.Min(Width - 20f, sz.Width + 28f);

                    if (x + chipW > Width - 10f && x > 10f)
                    {
                        x = 10f;
                        y += rowH + gap;
                    }
                    if (y + rowH > Height - 4f) break;

                    var rect = new RectangleF(x, y, chipW, rowH);
                    _hitRects.Add((rect, tag));

                    bool hov = (i == _hoverIdx);
                    using (var pillFill = new SolidBrush(hov ? Color.FromArgb(224, 231, 255) : Color.FromArgb(238, 242, 255)))
                    using (var pillBorder = new Pen(Color.FromArgb(199, 210, 254), 1f))
                    {
                        g.FillRoundedRectangle(pillFill, rect, 5f);
                        g.DrawRoundedRectangle(pillBorder, rect, 5f);
                    }

                    FluentUi.Text(g, tag, chipFont, Color.FromArgb(67, 56, 202), new RectangleF(rect.X + 8f, rect.Y, rect.Width - 24f, rect.Height));

                    float cx = rect.Right - 11f;
                    float cy = rect.Y + rect.Height / 2f;
                    using (var xPen = new Pen(hov ? Color.FromArgb(220, 38, 38) : Color.FromArgb(100, 116, 139), 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(xPen, cx - 3f, cy - 3f, cx + 3f, cy + 3f);
                        g.DrawLine(xPen, cx + 3f, cy - 3f, cx - 3f, cy + 3f);
                    }

                    x += chipW + gap;
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int idx = -1;
                for (int i = 0; i < _hitRects.Count; i++)
                {
                    if (_hitRects[i].Bounds.Contains(e.Location)) { idx = i; break; }
                }
                if (idx != _hoverIdx)
                {
                    _hoverIdx = idx;
                    Cursor = _hoverIdx >= 0 ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoverIdx != -1)
                {
                    _hoverIdx = -1;
                    Cursor = Cursors.Default;
                    Invalidate();
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left) return;
                for (int i = 0; i < _hitRects.Count; i++)
                {
                    if (_hitRects[i].Bounds.Contains(e.Location))
                    {
                        _onRemove?.Invoke(_hitRects[i].Tag);
                        break;
                    }
                }
            }
        }

        private sealed class SuggestedChipsCanvas : Control
        {
            private readonly string[] _suggestions;
            private readonly List<string> _selectedTags;
            private readonly Action<string> _onAdd;
            private readonly List<(RectangleF Bounds, string Tag)> _hitRects = new List<(RectangleF, string)>();
            private int _hoverIdx = -1;

            public SuggestedChipsCanvas(string[] suggestions, List<string> selectedTags, Action<string> onAdd)
            {
                _suggestions = suggestions;
                _selectedTags = selectedTags;
                _onAdd = onAdd;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var g = new D2DGraphics(e.Graphics, Math.Max(1, Width), Math.Max(1, Height));
                g.Clear(Color.White);
                _hitRects.Clear();

                using var chipFont = new Font("Segoe UI", 8.5f);
                float x = 0f, y = 4f, rowH = 24f, gap = 6f;

                for (int i = 0; i < _suggestions.Length; i++)
                {
                    string st = _suggestions[i];
                    bool alreadyAdded = _selectedTags != null && _selectedTags.Any(t => string.Equals(t, st, StringComparison.OrdinalIgnoreCase));
                    string label = "+ " + st;
                    Size sz = TextRenderer.MeasureText(label, chipFont);
                    float chipW = sz.Width + 14f;

                    if (x + chipW > Width && x > 0f)
                    {
                        x = 0f;
                        y += rowH + gap;
                    }
                    if (y + rowH > Height) break;

                    var rect = new RectangleF(x, y, chipW, rowH);
                    _hitRects.Add((rect, st));

                    bool hov = (i == _hoverIdx) && !alreadyAdded;
                    Color bgCol = alreadyAdded ? Color.FromArgb(241, 245, 249) : (hov ? Color.FromArgb(238, 242, 255) : Color.White);
                    Color borderCol = alreadyAdded ? Color.FromArgb(226, 232, 240) : (hov ? Color.FromArgb(165, 180, 252) : Color.FromArgb(216, 222, 230));
                    Color txtCol = alreadyAdded ? Color.FromArgb(148, 163, 184) : (hov ? Color.FromArgb(67, 56, 202) : Color.FromArgb(65, 75, 90));

                    using (var fill = new SolidBrush(bgCol))
                    using (var border = new Pen(borderCol, 1f))
                    {
                        g.FillRoundedRectangle(fill, rect, 5f);
                        g.DrawRoundedRectangle(border, rect, 5f);
                    }

                    FluentUi.Text(g, label, chipFont, txtCol, rect, true);
                    x += chipW + gap;
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int idx = -1;
                for (int i = 0; i < _hitRects.Count; i++)
                {
                    if (_hitRects[i].Bounds.Contains(e.Location)) { idx = i; break; }
                }
                if (idx != _hoverIdx)
                {
                    _hoverIdx = idx;
                    Cursor = _hoverIdx >= 0 ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoverIdx != -1)
                {
                    _hoverIdx = -1;
                    Cursor = Cursors.Default;
                    Invalidate();
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left) return;
                for (int i = 0; i < _hitRects.Count; i++)
                {
                    if (_hitRects[i].Bounds.Contains(e.Location))
                    {
                        _onAdd?.Invoke(_hitRects[i].Tag);
                        break;
                    }
                }
            }
        }
    }
}

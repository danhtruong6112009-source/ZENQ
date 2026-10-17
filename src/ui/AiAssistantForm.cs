using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Cửa sổ Trợ lý AI Toàn năng (GenOffice Universal AI Copilot).
    /// Hoạt động trên mọi thẻ làm việc: Writer, Calc, Slides, PDF, Home.
    /// Tự động bắt ngữ cảnh tài liệu, đề xuất tác vụ nhanh và áp dụng trực tiếp kết quả vào tài liệu.
    /// </summary>
    public class AiAssistantForm : Form
    {
        private AppForm _appForm;
        private AiConfig _config;

        private Label lblHeaderTitle;
        private Label lblModelBadge;
        private Button btnSettings;

        private FlowLayoutPanel pnlChips;
        private TextBox txtContext;
        private TextBox txtPrompt;
        private Button btnSend;
        private Label lblStatus;

        private TextBox txtResponse;
        private Button btnApply;
        private Button btnCopy;
        private Button btnClear;

        public AiAssistantForm(AppForm appForm)
        {
            _appForm = appForm;
            _config = AiConfig.Load();

            InitializeComponent();
            RefreshContextAndChips();
        }

        private void InitializeComponent()
        {
            this.Text = "ZENQ AI Assistant - Trợ lý Trí tuệ Nhân tạo";
            this.Size = new Size(720, 780);
            this.MinimumSize = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f);

            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlTop.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
                }
                using (SolidBrush b = new SolidBrush(Color.FromArgb(99, 102, 241)))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 5, pnlTop.Height);
                }
                FluentIconRenderer.DrawGlyph(e.Graphics, '\uE781', new Rectangle(14, 12, 22, 22), Color.FromArgb(99, 102, 241), 18f);
            };

            lblHeaderTitle = new Label
            {
                Text = "ZENQ AI Assistant",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(42, 10),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblHeaderTitle);

            lblModelBadge = new Label
            {
                Text = GetModelBadgeText(),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(79, 70, 229),
                BackColor = Color.FromArgb(238, 242, 255),
                Location = new Point(230, 14),
                AutoSize = true,
                Padding = new Padding(6, 3, 6, 3)
            };
            pnlTop.Controls.Add(lblModelBadge);

            btnSettings = new Button
            {
                Text = "Thiết lập AI",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE713', 15, 15, Color.FromArgb(51, 65, 85)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Size = new Size(130, 34),
                Location = new Point(555, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSettings.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnSettings.Click += (s, e) =>
            {
                using (AiConfigDialog dlg = new AiConfigDialog())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _config = AiConfig.Load();
                        lblModelBadge.Text = GetModelBadgeText();
                    }
                }
            };
            pnlTop.Controls.Add(btnSettings);

            this.Controls.Add(pnlTop);

            // Main Body Split/Layout
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                AutoScroll = true
            };

            // Section: Context Snippet
            Label lblCtx = new Label
            {
                Text = "Ngữ cảnh tài liệu hiện tại:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 8),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblCtx);

            txtContext = new TextBox
            {
                Location = new Point(16, 28),
                Size = new Size(670, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.8f),
                ScrollBars = ScrollBars.Vertical
            };
            pnlBody.Controls.Add(txtContext);

            // Section: Quick Action Chips
            Label lblChips = new Label
            {
                Text = "Gợi ý tác vụ nhanh (Nhấp để chọn):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 84),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblChips);

            pnlChips = new FlowLayoutPanel
            {
                Location = new Point(16, 104),
                Size = new Size(670, 68),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                WrapContents = true
            };
            pnlBody.Controls.Add(pnlChips);

            // Section: User Prompt
            Label lblPrompt = new Label
            {
                Text = "Yêu cầu của bạn (Nhấn Enter để gửi, Shift+Enter để xuống dòng):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 180),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblPrompt);

            txtPrompt = new TextBox
            {
                Location = new Point(16, 202),
                Size = new Size(545, 68),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10f)
            };
            txtPrompt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    ExecutePrompt();
                }
            };
            pnlBody.Controls.Add(txtPrompt);

            btnSend = new Button
            {
                Text = "Gửi yêu cầu",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE724', 16, 16, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(570, 202),
                Size = new Size(116, 68),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += (s, e) => ExecutePrompt();
            pnlBody.Controls.Add(btnSend);

            lblStatus = new Label
            {
                Text = "Sẵn sàng nhận yêu cầu.",
                Location = new Point(16, 276),
                Size = new Size(670, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnlBody.Controls.Add(lblStatus);

            // Section: Output & Action Buttons
            Label lblOut = new Label
            {
                Text = "Kết quả phản hồi từ AI:",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 302),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblOut);

            txtResponse = new TextBox
            {
                Location = new Point(16, 324),
                Size = new Size(670, 290),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlBody.Controls.Add(txtResponse);

            Panel pnlActionFooter = new Panel
            {
                Location = new Point(16, 622),
                Size = new Size(670, 45),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            btnApply = new Button
            {
                Text = "Áp dụng vào tài liệu",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE73E', 16, 16, Color.White),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(8, 0, 8, 0),
                Location = new Point(0, 4),
                Size = new Size(200, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) => ApplyResultToDoc();
            pnlActionFooter.Controls.Add(btnApply);

            btnCopy = new Button
            {
                Text = "Sao chép",
                Image = FluentIconRenderer.GetGlyphBitmap('\uE8C8', 15, 15, Color.FromArgb(30, 41, 59)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(6, 0, 6, 0),
                Location = new Point(210, 4),
                Size = new Size(120, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCopy.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtResponse.Text))
                {
                    Clipboard.SetText(txtResponse.Text);
                    lblStatus.Text = "Đã sao chép nội dung vào Clipboard!";
                }
            };
            pnlActionFooter.Controls.Add(btnCopy);

            btnClear = new Button
            {
                Text = "Xóa trắng",
                Location = new Point(380, 4),
                Size = new Size(100, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.2f),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClear.Click += (s, e) =>
            {
                txtResponse.Clear();
                btnApply.Enabled = false;
            };
            pnlActionFooter.Controls.Add(btnClear);

            pnlBody.Controls.Add(pnlActionFooter);
            this.Controls.Add(pnlBody);
        }

        private string GetModelBadgeText()
        {
            if (_config == null) return "Chưa cấu hình";
            return string.Format("{0} • {1}", _config.Provider, _config.ModelName);
        }

        private OfficeAppMode GetCurrentMode()
        {
            if (_appForm != null && _appForm.Ribbon != null)
            {
                return _appForm.Ribbon.CurrentMode;
            }
            return OfficeAppMode.Home;
        }

        public void RefreshContextAndChips()
        {
            _config = AiConfig.Load();
            lblModelBadge.Text = GetModelBadgeText();

            OfficeAppMode mode = GetCurrentMode();
            pnlChips.Controls.Clear();

            string contextDesc = "";

            if (mode == OfficeAppMode.Writer)
            {
                var writer = _appForm != null ? _appForm.WriterView : null;
                string sel = (writer != null && Math.Abs(writer.SelectionEnd - writer.SelectionStart) > 0) ? writer.GetSelectedText() : "";
                if (!string.IsNullOrEmpty(sel))
                {
                    contextDesc = "Văn bản Word [Đang chọn " + sel.Length + " ký tự]: \"" + (sel.Length > 100 ? sel.Substring(0, 100) + "..." : sel) + "\"";
                }
                else
                {
                    contextDesc = "Văn bản Word: Soạn thảo tại vị trí con trỏ.";
                }

                AddChip("Viết bài tự động theo chủ đề...", "Hãy viết một bài văn bản hoàn chỉnh và chuyên nghiệp về chủ đề: ", '\uE70F');
                AddChip("Viết lại đoạn văn chuyên nghiệp hơn", "Hãy viết lại đoạn văn bản sau đây với văn phong trang trọng, mượt mà và thuyết phục hơn:\n", '\uE90F');
                AddChip("Tóm tắt nội dung chính", "Hãy tóm tắt ngắn gọn các ý chính của đoạn văn bản sau thành danh sách gạch đầu dòng:\n", '\uE7C3');
                AddChip("Sửa lỗi chính tả & ngữ pháp", "Hãy rà soát và sửa tất cả các lỗi chính tả, dấu câu, ngữ pháp trong đoạn văn sau:\n", '\uE73E');
                AddChip("Dịch sang Tiếng Anh chuẩn", "Hãy dịch đoạn văn bản sau sang tiếng Anh học thuật/công sở chuẩn xác:\n", '\uE774');
                AddChip("Tạo bảng biểu số liệu mẫu", "Hãy tạo một bảng biểu mẫu gồm 4 cột (STT, Hạng mục, Số lượng, Đơn giá) với 5 hàng dữ liệu mẫu.", '\uE80A');
            }
            else if (mode == OfficeAppMode.Calc)
            {
                var calc = _appForm != null ? _appForm.CalcView : null;
                if (calc != null)
                {
                    string sName = (calc.SheetNames != null && calc.ActiveSheetIndex >= 0 && calc.ActiveSheetIndex < calc.SheetNames.Count) ? calc.SheetNames[calc.ActiveSheetIndex] : "Sheet1";
                    contextDesc = string.Format("Bảng tính Excel: Đang chọn Ô R{0}C{1} (Sheet: {2})", calc.SelectedRow, calc.SelectedCol, sName);
                }
                else
                {
                    contextDesc = "Bảng tính Excel: Đang làm việc trên trang tính.";
                }

                AddChip("Viết công thức Excel theo yêu cầu", "Hãy viết công thức Excel chính xác (bắt đầu bằng dấu =) để: ", '\uE8EF');
                AddChip("Tạo bảng dữ liệu bán hàng mẫu", "Hãy tạo một bảng số liệu doanh thu 5 sản phẩm gồm các cột: Mã SP, Tên SP, Số lượng, Đơn giá, Thành tiền (=C*D)", '\uE80A');
                AddChip("Phân tích số liệu và xu hướng", "Hãy phân tích và đưa ra nhận xét chuyên sâu từ bảng số liệu sau:\n", '\uE9D9');
                AddChip("Viết công thức VLOOKUP / XLOOKUP", "Hãy viết công thức XLOOKUP tìm kiếm mã sản phẩm ở cột A và lấy giá ở cột D.", '\uE721');
                AddChip("Viết công thức tính tổng (SUMIFS)", "Hãy viết công thức SUMIFS tính tổng doanh thu theo điều kiện phòng ban.", '\uE8EF');
            }
            else if (mode == OfficeAppMode.Slides)
            {
                var slides = _appForm != null ? _appForm.SlidesView : null;
                if (slides != null && slides.Presentation != null)
                {
                    contextDesc = string.Format("Trình chiếu PowerPoint: Slide {0}/{1} (Tiêu đề: {2})", slides.ActiveSlideIndex + 1, slides.Presentation.Slides.Count, slides.Presentation.Slides.Count > slides.ActiveSlideIndex ? slides.Presentation.Slides[slides.ActiveSlideIndex].Title : "");
                }
                else
                {
                    contextDesc = "Trình chiếu PowerPoint: Bản trình bày Slides.";
                }

                AddChip("Tạo bài thuyết trình 5 slide hoàn chỉnh", "Hãy lập dàn ý và tạo bài thuyết trình hoàn chỉnh gồm 5 Slide (có Slide 1: Tiêu đề, Slide 2, 3, 4, 5 với các ý chính gạch đầu dòng) về chủ đề: ", '\uE714');
                AddChip("Thêm slide mới phân tích chi tiết", "Hãy thêm 1 Slide mới có Tiêu đề và 4 gạch đầu dòng chi tiết về: ", '\uE710');
                AddChip("Tóm tắt ý chính thành Bullet Points", "Hãy chuyển nội dung sau thành các gạch đầu dòng súc tích, ấn tượng cho slide thuyết trình:\n", '\uE8D2');
                AddChip("Gợi ý dàn ý bài thuyết trình", "Hãy xây dựng cấu trúc dàn ý 7 slide thu hút nhà đầu tư cho dự án khởi nghiệp về: ", '\uE781');
            }
            else if (mode == OfficeAppMode.Pdf)
            {
                var pdf = _appForm != null ? _appForm.PdfView : null;
                contextDesc = string.Format("Tài liệu PDF: Đang xem trang {0}", pdf != null ? (pdf.ActivePageIndex + 1).ToString() : "1");

                AddChip("Hỏi đáp về tài liệu PDF này", "Dựa trên nội dung tài liệu PDF, hãy giải đáp câu hỏi sau: ", '\uE897');
                AddChip("Tóm tắt trang hiện tại", "Hãy tóm tắt các điểm then chốt nhất của trang tài liệu PDF này.", '\uE7C3');
                AddChip("Trích xuất các số liệu quan trọng", "Hãy liệt kê toàn bộ các số liệu, mốc thời gian và thông tin cốt lõi trong tài liệu.", '\uE721');
            }
            else
            {
                contextDesc = "Màn hình chính ZENQ OFFICE: Trợ lý sẵn sàng hỗ trợ mọi việc.";
                AddChip("Gợi ý mẫu kế hoạch công việc", "Hãy tạo một bản kế hoạch công việc tuần chi tiết.", '\uE781');
                AddChip("Soạn thư ngỏ gửi khách hàng", "Hãy soạn một email lịch sự và trang trọng gửi đối tác để: ", '\uE715');
                AddChip("Viết hướng dẫn tin học văn phòng", "Hãy giải thích cách dùng hàm INDEX MATCH kết hợp trong Excel.", '\uE7F4');
            }

            txtContext.Text = contextDesc;
        }

        private void AddChip(string title, string promptTemplate, char glyph = '\0')
        {
            Button chip = new Button
            {
                Text = title,
                AutoSize = true,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Color.FromArgb(67, 56, 202),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(3)
            };
            if (glyph != '\0')
            {
                chip.Image = FluentIconRenderer.GetGlyphBitmap(glyph, 13, 13, Color.FromArgb(67, 56, 202));
                chip.ImageAlign = ContentAlignment.MiddleLeft;
                chip.TextImageRelation = TextImageRelation.ImageBeforeText;
                chip.Padding = new Padding(6, 0, 6, 0);
            }
            chip.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            chip.Click += (s, e) =>
            {
                txtPrompt.Text = promptTemplate;
                txtPrompt.Focus();
                txtPrompt.SelectionStart = txtPrompt.Text.Length;
            };
            pnlChips.Controls.Add(chip);
        }

        private void ExecutePrompt()
        {
            string userPrompt = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userPrompt))
            {
                MessageBox.Show("Vui lòng nhập yêu cầu của bạn trước khi gửi.", "ZENQ AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_config == null || (_config.Provider != AiProviderType.Ollama && string.IsNullOrWhiteSpace(_config.ApiKey)))
            {
                var dr = MessageBox.Show("Bạn chưa nhập API Key cho Trợ lý AI.\nBạn có muốn mở bảng Cấu hình để nhập API Key ngay không?", "ZENQ AI - Chưa có API Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    using (AiConfigDialog dlg = new AiConfigDialog())
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            _config = AiConfig.Load();
                            lblModelBadge.Text = GetModelBadgeText();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            btnSend.Enabled = false;
            lblStatus.Text = "Đang gửi yêu cầu tới AI (" + _config.ModelName + ")... Vui lòng đợi...";
            lblStatus.ForeColor = Color.FromArgb(202, 138, 4);

            string contextData = txtContext.Text;
            OfficeAppMode mode = GetCurrentMode();
            if (mode == OfficeAppMode.Writer && _appForm != null && _appForm.WriterView != null)
            {
                string sel = _appForm.WriterView.GetSelectedText();
                if (!string.IsNullOrEmpty(sel)) contextData += "\nNội dung chọn:\n" + sel;
            }

            AiService.SendPromptAsync(_config, _config.SystemInstructions, userPrompt, contextData, resp =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    btnSend.Enabled = true;
                    if (resp.Success)
                    {
                        txtResponse.Text = resp.Text;
                        lblStatus.Text = "Hoàn thành sau " + resp.DurationMs + " ms từ " + resp.ModelUsed;
                        lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                        btnApply.Enabled = true;
                    }
                    else
                    {
                        txtResponse.Text = "ĐÃ XẢY RA LỖI:\n" + resp.ErrorMessage;
                        lblStatus.Text = "Lỗi khi gọi API: " + resp.ErrorMessage;
                        lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                        btnApply.Enabled = false;
                    }
                }));
            });
        }

        private void ApplyResultToDoc()
        {
            string outText = txtResponse.Text;
            if (string.IsNullOrWhiteSpace(outText)) return;

            var actionResult = AiActionEngine.ProcessAndExecute(_appForm, outText);
            if (actionResult != null && actionResult.HasAction)
            {
                lblStatus.Text = actionResult.ExecutionSummary;
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                lblStatus.Text = "Đã xử lý yêu cầu.";
                lblStatus.ForeColor = Color.FromArgb(79, 70, 229);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class PageSetupForm : Form
    {
        public float TopMargin { get; set; }
        public float BottomMargin { get; set; }
        public float LeftMargin { get; set; }
        public float RightMargin { get; set; }
        public bool IsLandscape { get; set; }
        public string PaperSize { get; set; }

        public float PaperWidthInches { get; set; }
        public float PaperHeightInches { get; set; }

        // Layout tab results
        public int SectionStartType { get; set; } = 0; // 0=New page, 1=New column, 2=Continuous, 3=Even, 4=Odd
        public bool DifferentOddAndEven { get; set; }
        public bool DifferentFirstPage { get; set; }
        public float HeaderDistanceInches { get; set; } = 0.5f;
        public float FooterDistanceInches { get; set; } = 0.5f;
        public int VerticalAlignment { get; set; } = 0; // 0=Top, 1=Center, 2=Justified, 3=Bottom

        private NumericUpDown numTop;
        private NumericUpDown numBottom;
        private NumericUpDown numLeft;
        private NumericUpDown numRight;
        private RadioButton rdoPortrait;
        private RadioButton rdoLandscape;
        private ComboBox cboPaper;
        private NumericUpDown numPaperWidth;
        private NumericUpDown numPaperHeight;
        private PictureBox picPaperPreview;
        private ComboBox cboApplyToPaper;

        private ComboBox cboSectionStart;
        private CheckBox chkDiffOddEven;
        private CheckBox chkDiffFirstPage;
        private NumericUpDown numHeaderEdge;
        private NumericUpDown numFooterEdge;
        private ComboBox cboVertAlign;
        private PictureBox picLayoutPreview;

        public event Action OnApply;

        public PageSetupForm(float curTop, float curBottom, float curLeft, float curRight, bool curLandscape, int initialTabIndex = 0, string curPaper = "A4")
        {
            TopMargin = curTop;
            BottomMargin = curBottom;
            LeftMargin = curLeft;
            RightMargin = curRight;
            IsLandscape = curLandscape;
            PaperSize = string.IsNullOrEmpty(curPaper) ? "A4" : curPaper;
            PaperWidthInches = (PaperSize == "Letter") ? 8.5f : (PaperSize == "Legal" ? 8.5f : (PaperSize == "A3" ? 11.69f : 8.27f));
            PaperHeightInches = (PaperSize == "Letter") ? 11.0f : (PaperSize == "Legal" ? 14.0f : (PaperSize == "A3" ? 16.54f : 11.69f));

            Text = AppL10n.T("Thiết lập trang", "Page Setup");
            ClientSize = new Size(476, 492);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f);

            TabControl tabControl = new ZenQOffice.UI.FluentTabControl
            {
                Location = new Point(14, 14),
                Size = new Size(448, 424),
                Font = new Font("Segoe UI", 9.2f)
            };

            // ==================== TAB 1: CĂN LỀ ====================
            TabPage tabMargins = new TabPage(AppL10n.T("Căn lề", "Margins"));
            tabMargins.BackColor = Color.White;

            Label lblMarginsTitle = new Label { Text = AppL10n.T("Thông số lề trang (inch)", "Page Margins (in)"), Font = new Font("Segoe UI", 9.2f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(16, 14), AutoSize = true };
            tabMargins.Controls.Add(lblMarginsTitle);

            Label lblTop = new Label { Text = AppL10n.T("Lề trên:", "Top:"), Location = new Point(16, 44), AutoSize = true };
            numTop = new NumericUpDown { Location = new Point(110, 41), Size = new Size(80, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)TopMargin };
            tabMargins.Controls.Add(lblTop);
            tabMargins.Controls.Add(numTop);

            Label lblBottom = new Label { Text = AppL10n.T("Lề dưới:", "Bottom:"), Location = new Point(225, 44), AutoSize = true };
            numBottom = new NumericUpDown { Location = new Point(320, 41), Size = new Size(80, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)BottomMargin };
            tabMargins.Controls.Add(lblBottom);
            tabMargins.Controls.Add(numBottom);

            Label lblLeft = new Label { Text = AppL10n.T("Lề trái:", "Left:"), Location = new Point(16, 80), AutoSize = true };
            numLeft = new NumericUpDown { Location = new Point(110, 77), Size = new Size(80, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)LeftMargin };
            tabMargins.Controls.Add(lblLeft);
            tabMargins.Controls.Add(numLeft);

            Label lblRight = new Label { Text = AppL10n.T("Lề phải:", "Right:"), Location = new Point(225, 80), AutoSize = true };
            numRight = new NumericUpDown { Location = new Point(320, 77), Size = new Size(80, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)RightMargin };
            tabMargins.Controls.Add(lblRight);
            tabMargins.Controls.Add(numRight);

            // Orientation
            GroupBox grpOrient = new GroupBox { Text = AppL10n.T("Hướng trang giấy", "Orientation"), Location = new Point(16, 114), Size = new Size(406, 66) };
            rdoPortrait = new RadioButton { Text = AppL10n.T("📄 Dọc (Portrait)", "📄 Portrait"), Location = new Point(24, 26), AutoSize = true, Checked = !IsLandscape };
            rdoLandscape = new RadioButton { Text = AppL10n.T("📜 Ngang (Landscape)", "📜 Landscape"), Location = new Point(210, 26), AutoSize = true, Checked = IsLandscape };
            grpOrient.Controls.Add(rdoPortrait);
            grpOrient.Controls.Add(rdoLandscape);
            tabMargins.Controls.Add(grpOrient);

            // Preview Box (Real-Time Proportional Preview Engine)
            GroupBox grpPreview = new GroupBox { Text = AppL10n.T("Xem trước trực tiếp", "Live Preview"), Location = new Point(16, 190), Size = new Size(406, 178) };
            PictureBox picPreview = new PictureBox { Location = new Point((406 - 98) / 2, 22), Size = new Size(98, 130), BackColor = Color.White };
            picPreview.Paint += (s, e) =>
            {
                int pW = picPreview.Width - 1;
                int pH = picPreview.Height - 1;

                e.Graphics.DrawRectangle(Pens.Black, 0, 0, pW, pH);

                float stdW = rdoLandscape.Checked ? 11.0f : 8.5f;
                float stdH = rdoLandscape.Checked ? 8.5f : 11.0f;

                int mL = Math.Max(4, (int)(pW * ((float)numLeft.Value / stdW)));
                int mR = Math.Max(4, (int)(pW * ((float)numRight.Value / stdW)));
                int mT = Math.Max(4, (int)(pH * ((float)numTop.Value / stdH)));
                int mB = Math.Max(4, (int)(pH * ((float)numBottom.Value / stdH)));

                mL = Math.Min(mL, pW / 2 - 4);
                mR = Math.Min(mR, pW / 2 - 4);
                mT = Math.Min(mT, pH / 2 - 4);
                mB = Math.Min(mB, pH / 2 - 4);

                using (Pen bluePen = new Pen(Color.DodgerBlue, 1.5f))
                {
                    e.Graphics.DrawRectangle(bluePen, mL, mT, pW - mL - mR, pH - mT - mB);
                }

                using (Pen linePen = new Pen(Color.FromArgb(160, 160, 160), 1.5f))
                {
                    int startY = mT + 5;
                    int endY = pH - mB - 5;
                    int startX = mL + 4;
                    int endX = pW - mR - 4;

                    if (endX > startX)
                    {
                        for (int y = startY; y <= endY; y += 6)
                        {
                            e.Graphics.DrawLine(linePen, startX, y, endX, y);
                        }
                    }
                }
            };

            EventHandler updatePreview = (s, ev) =>
            {
                if (rdoLandscape.Checked)
                {
                    picPreview.Size = new Size(134, 98);
                    picPreview.Location = new Point((406 - 134) / 2, 36);
                }
                else
                {
                    picPreview.Size = new Size(98, 130);
                    picPreview.Location = new Point((406 - 98) / 2, 22);
                }
                picPreview.Invalidate();
            };

            numTop.ValueChanged += updatePreview;
            numBottom.ValueChanged += updatePreview;
            numLeft.ValueChanged += updatePreview;
            numRight.ValueChanged += updatePreview;
            rdoPortrait.CheckedChanged += updatePreview;
            rdoLandscape.CheckedChanged += updatePreview;

            grpPreview.Controls.Add(picPreview);
            tabMargins.Controls.Add(grpPreview);
            tabControl.TabPages.Add(tabMargins);

            // ==================== TAB 2: KHỔ GIẤY ====================
            TabPage tabPaper = new TabPage(AppL10n.T("Khổ giấy", "Paper"));
            tabPaper.BackColor = Color.White;

            Label lblPaperHead = new Label { Text = AppL10n.T("Khổ giấy:", "Paper size:"), Location = new Point(16, 14), AutoSize = true };
            cboPaper = new ZenQOffice.UI.FluentComboBox { Location = new Point(16, 36), Size = new Size(406, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboPaper.Items.AddRange(new object[] {
                "Letter", "Tabloid", "Legal", "Statement", "Executive", "A3", "A4", "A5", "B4 (JIS)", "B5 (JIS)", AppL10n.T("Tùy chỉnh", "Custom size")
            });
            cboPaper.SelectedItem = PaperSize;
            if (cboPaper.SelectedIndex == -1) cboPaper.SelectedIndex = 6; // Default A4

            Label lblPW = new Label { Text = AppL10n.T("Chiều rộng (in):", "Width (in):"), Location = new Point(16, 72), AutoSize = true };
            numPaperWidth = new NumericUpDown { Location = new Point(16, 94), Size = new Size(192, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 2.0m, Maximum = 40.0m, Value = (decimal)PaperWidthInches };

            Label lblPH = new Label { Text = AppL10n.T("Chiều cao (in):", "Height (in):"), Location = new Point(230, 72), AutoSize = true };
            numPaperHeight = new NumericUpDown { Location = new Point(230, 94), Size = new Size(192, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 2.0m, Maximum = 40.0m, Value = (decimal)PaperHeightInches };

            cboPaper.SelectedIndexChanged += (s, e) =>
            {
                string sel = cboPaper.SelectedItem.ToString();
                if (sel == "Letter") { numPaperWidth.Value = 8.5m; numPaperHeight.Value = 11.0m; }
                else if (sel == "Tabloid") { numPaperWidth.Value = 11.0m; numPaperHeight.Value = 17.0m; }
                else if (sel == "Legal") { numPaperWidth.Value = 8.5m; numPaperHeight.Value = 14.0m; }
                else if (sel == "Statement") { numPaperWidth.Value = 5.5m; numPaperHeight.Value = 8.5m; }
                else if (sel == "Executive") { numPaperWidth.Value = 7.25m; numPaperHeight.Value = 10.5m; }
                else if (sel == "A3") { numPaperWidth.Value = 11.69m; numPaperHeight.Value = 16.54m; }
                else if (sel == "A4") { numPaperWidth.Value = 8.27m; numPaperHeight.Value = 11.69m; }
                else if (sel == "A5") { numPaperWidth.Value = 5.83m; numPaperHeight.Value = 8.27m; }
                else if (sel == "B4 (JIS)") { numPaperWidth.Value = 10.12m; numPaperHeight.Value = 14.33m; }
                else if (sel == "B5 (JIS)") { numPaperWidth.Value = 7.17m; numPaperHeight.Value = 10.12m; }
                if (picPaperPreview != null) picPaperPreview.Invalidate();
            };

            numPaperWidth.ValueChanged += (s, e) => { if (picPaperPreview != null) picPaperPreview.Invalidate(); };
            numPaperHeight.ValueChanged += (s, e) => { if (picPaperPreview != null) picPaperPreview.Invalidate(); };

            // Paper source
            GroupBox grpSource = new GroupBox { Text = AppL10n.T("Nguồn giấy", "Paper source"), Location = new Point(16, 126), Size = new Size(406, 100) };
            Label lblTray1 = new Label { Text = AppL10n.T("Trang đầu:", "First page:"), Location = new Point(14, 22), AutoSize = true };
            ListBox lstTray1 = new ZenQOffice.UI.FluentListBox { Location = new Point(14, 42), Size = new Size(182, 48) };
            lstTray1.Items.AddRange(new object[] { AppL10n.T("Khay mặc định", "Default tray"), AppL10n.T("Nạp thủ công", "Manual feed") });
            lstTray1.SelectedIndex = 0;

            Label lblTray2 = new Label { Text = AppL10n.T("Các trang khác:", "Other pages:"), Location = new Point(210, 22), AutoSize = true };
            ListBox lstTray2 = new ZenQOffice.UI.FluentListBox { Location = new Point(210, 42), Size = new Size(182, 48) };
            lstTray2.Items.AddRange(new object[] { AppL10n.T("Khay mặc định", "Default tray"), AppL10n.T("Nạp thủ công", "Manual feed") });
            lstTray2.SelectedIndex = 0;

            grpSource.Controls.AddRange(new Control[] { lblTray1, lstTray1, lblTray2, lstTray2 });

            // Preview Group for Paper Tab
            GroupBox grpPaperPrev = new GroupBox { Text = AppL10n.T("Xem trước", "Preview"), Location = new Point(16, 232), Size = new Size(406, 144) };
            picPaperPreview = new PictureBox { Location = new Point(18, 20), Size = new Size(95, 112), BackColor = Color.White };
            picPaperPreview.Paint += (s, e) =>
            {
                int pw = picPaperPreview.Width - 1;
                int ph = picPaperPreview.Height - 1;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(Pens.Black, 0, 0, pw, ph);

                using (Pen linePen = new Pen(Color.FromArgb(51, 65, 85), 1.2f))
                {
                    for (int ly = 12; ly <= ph - 12; ly += 6)
                    {
                        e.Graphics.DrawLine(linePen, 10, ly, pw - 10, ly);
                    }
                }
            };
            grpPaperPrev.Controls.Add(picPaperPreview);

            Label lblApplyPaper = new Label { Text = AppL10n.T("Áp dụng vào:", "Apply to:"), Location = new Point(140, 26), AutoSize = true };
            cboApplyToPaper = new ZenQOffice.UI.FluentComboBox { Location = new Point(140, 48), Size = new Size(248, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboApplyToPaper.Items.AddRange(new object[] { AppL10n.T("Toàn bộ tài liệu", "Whole document"), AppL10n.T("Từ điểm này trở đi", "This point forward") });
            cboApplyToPaper.SelectedIndex = 0;
            grpPaperPrev.Controls.Add(lblApplyPaper);
            grpPaperPrev.Controls.Add(cboApplyToPaper);

            Button btnPrintOpt = new Button { Text = AppL10n.T("Tùy chọn in...", "Print Options..."), Location = new Point(140, 92), Size = new Size(248, 28), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
            btnPrintOpt.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            grpPaperPrev.Controls.Add(btnPrintOpt);

            tabPaper.Controls.AddRange(new Control[] { lblPaperHead, cboPaper, lblPW, numPaperWidth, lblPH, numPaperHeight, grpSource, grpPaperPrev });
            tabControl.TabPages.Add(tabPaper);

            // ==================== TAB 3: BỐ TRÍ ====================
            TabPage tabLayout = new TabPage(AppL10n.T("Bố trí", "Layout"));
            tabLayout.BackColor = Color.White;

            GroupBox grpSection = new GroupBox { Text = AppL10n.T("Phần (Section)", "Section"), Location = new Point(16, 12), Size = new Size(406, 58) };
            grpSection.Controls.Add(new Label { Text = AppL10n.T("Bắt đầu phần:", "Section start:"), Location = new Point(14, 24), AutoSize = true });
            cboSectionStart = new ZenQOffice.UI.FluentComboBox { Location = new Point(192, 20), Size = new Size(198, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboSectionStart.Items.AddRange(new object[] { AppL10n.T("Trang mới", "New page"), AppL10n.T("Cột mới", "New column"), AppL10n.T("Liên tục", "Continuous"), AppL10n.T("Trang chẵn", "Even page"), AppL10n.T("Trang lẻ", "Odd page") });
            cboSectionStart.SelectedIndex = Math.Max(0, Math.Min(4, SectionStartType));
            grpSection.Controls.Add(cboSectionStart);
            tabLayout.Controls.Add(grpSection);

            GroupBox grpHdrFtr = new GroupBox { Text = AppL10n.T("Đầu trang và Chân trang", "Headers and footers"), Location = new Point(16, 76), Size = new Size(406, 114) };
            chkDiffOddEven = new CheckBox { Text = AppL10n.T("Khác nhau trang chẵn && lẻ", "Different odd && even pages"), Location = new Point(14, 24), AutoSize = true, Checked = DifferentOddAndEven };
            chkDiffFirstPage = new CheckBox { Text = AppL10n.T("Khác nhau trang đầu", "Different first page"), Location = new Point(14, 48), AutoSize = true, Checked = DifferentFirstPage };
            grpHdrFtr.Controls.Add(chkDiffOddEven);
            grpHdrFtr.Controls.Add(chkDiffFirstPage);

            grpHdrFtr.Controls.Add(new Label { Text = AppL10n.T("Đầu trang (in):", "Header (in):"), Location = new Point(14, 78), AutoSize = true });
            numHeaderEdge = new NumericUpDown { Location = new Point(116, 75), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.0m, Maximum = 5.0m, Value = (decimal)HeaderDistanceInches };
            grpHdrFtr.Controls.Add(numHeaderEdge);

            grpHdrFtr.Controls.Add(new Label { Text = AppL10n.T("Chân trang (in):", "Footer (in):"), Location = new Point(214, 78), AutoSize = true });
            numFooterEdge = new NumericUpDown { Location = new Point(320, 75), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.0m, Maximum = 5.0m, Value = (decimal)FooterDistanceInches };
            grpHdrFtr.Controls.Add(numFooterEdge);
            tabLayout.Controls.Add(grpHdrFtr);

            GroupBox grpPageAlign = new GroupBox { Text = AppL10n.T("Trang", "Page"), Location = new Point(16, 196), Size = new Size(406, 58) };
            grpPageAlign.Controls.Add(new Label { Text = AppL10n.T("Căn dọc:", "Vertical alignment:"), Location = new Point(14, 24), AutoSize = true });
            cboVertAlign = new ZenQOffice.UI.FluentComboBox { Location = new Point(192, 20), Size = new Size(198, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboVertAlign.Items.AddRange(new object[] { AppL10n.T("Trên (Top)", "Top"), AppL10n.T("Giữa (Center)", "Center"), AppL10n.T("Đều (Justified)", "Justified"), AppL10n.T("Dưới (Bottom)", "Bottom") });
            cboVertAlign.SelectedIndex = Math.Max(0, Math.Min(3, VerticalAlignment));
            cboVertAlign.SelectedIndexChanged += (s, e) => picLayoutPreview?.Invalidate();
            grpPageAlign.Controls.Add(cboVertAlign);
            tabLayout.Controls.Add(grpPageAlign);

            GroupBox grpLayoutPrev = new GroupBox { Text = AppL10n.T("Xem trước", "Preview"), Location = new Point(16, 260), Size = new Size(406, 118) };
            picLayoutPreview = new PictureBox { Location = new Point(18, 20), Size = new Size(84, 88), BackColor = Color.White };
            picLayoutPreview.Paint += (s, e) =>
            {
                int pw = picLayoutPreview.Width - 1;
                int ph = picLayoutPreview.Height - 1;
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawRectangle(Pens.Black, 0, 0, pw, ph);
                int va = cboVertAlign != null ? cboVertAlign.SelectedIndex : 0;
                int startY = 12;
                if (va == 1) startY = ph / 2 - 14;
                else if (va == 3) startY = ph - 40;
                int step = (va == 2) ? 14 : 6;
                using (Pen linePen = new Pen(Color.FromArgb(71, 85, 105), 1.3f))
                {
                    for (int li = 0; li < 5; li++)
                    {
                        int ly = startY + li * step;
                        if (ly < ph - 8) e.Graphics.DrawLine(linePen, 10, ly, pw - 10, ly);
                    }
                }
            };
            grpLayoutPrev.Controls.Add(picLayoutPreview);
            chkDiffOddEven.CheckedChanged += (s, e) => picLayoutPreview.Invalidate();
            chkDiffFirstPage.CheckedChanged += (s, e) => picLayoutPreview.Invalidate();
            numHeaderEdge.ValueChanged += (s, e) => picLayoutPreview.Invalidate();
            numFooterEdge.ValueChanged += (s, e) => picLayoutPreview.Invalidate();

            grpLayoutPrev.Controls.Add(new Label { Text = AppL10n.T("Áp dụng cho:", "Apply to:"), Location = new Point(140, 26), AutoSize = true });
            ComboBox cboApplyLayout = new ZenQOffice.UI.FluentComboBox { Location = new Point(140, 48), Size = new Size(248, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboApplyLayout.Items.AddRange(new object[] { AppL10n.T("Toàn bộ tài liệu", "Whole document"), AppL10n.T("Từ điểm này trở đi", "This point forward") });
            cboApplyLayout.SelectedIndex = 0;
            grpLayoutPrev.Controls.Add(cboApplyLayout);
            tabLayout.Controls.Add(grpLayoutPrev);
            tabControl.TabPages.Add(tabLayout);

            if (initialTabIndex >= 0 && initialTabIndex < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = initialTabIndex;
            }

            Controls.Add(tabControl);

            // Buttons: Đồng ý (OK), Hủy bỏ (Cancel), Đặt làm mặc định
            Button btnDefault = new Button
            {
                Text = AppL10n.T("Đặt làm mặc định", "Set As Default"),
                Location = new Point(14, 450),
                Size = new Size(150, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat
            };
            btnDefault.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            Controls.Add(btnDefault);

            Button btnOK = new Button
            {
                Text = AppL10n.T("Đồng ý", "OK"),
                DialogResult = DialogResult.OK,
                Location = new Point(268, 450),
                Size = new Size(94, 30),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) =>
            {
                TopMargin = (float)numTop.Value;
                BottomMargin = (float)numBottom.Value;
                LeftMargin = (float)numLeft.Value;
                RightMargin = (float)numRight.Value;
                IsLandscape = rdoLandscape.Checked;
                PaperSize = cboPaper.SelectedItem.ToString();
                PaperWidthInches = (float)numPaperWidth.Value;
                PaperHeightInches = (float)numPaperHeight.Value;
                SectionStartType = Math.Max(0, cboSectionStart.SelectedIndex);
                DifferentOddAndEven = chkDiffOddEven.Checked;
                DifferentFirstPage = chkDiffFirstPage.Checked;
                HeaderDistanceInches = (float)numHeaderEdge.Value;
                FooterDistanceInches = (float)numFooterEdge.Value;
                VerticalAlignment = Math.Max(0, cboVertAlign.SelectedIndex);
                if (OnApply != null) OnApply();
            };
            Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = AppL10n.T("Hủy bỏ", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(370, 450),
                Size = new Size(92, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            Controls.Add(btnCancel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
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

            Text = "Thiết lập trang (Page Setup)";
            Size = new Size(420, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(243, 244, 246);
            Font = new Font("Segoe UI", 9f);

            TabControl tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(380, 420),
                Font = new Font("Segoe UI", 9f)
            };

            // ==================== TAB 1: CĂN LỀ (MARGINS) ====================
            TabPage tabMargins = new TabPage("Căn lề (Margins)");
            tabMargins.BackColor = Color.White;

            Label lblMarginsTitle = new Label { Text = "Thông số lề trang (Margin)", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(12, 10), AutoSize = true };
            tabMargins.Controls.Add(lblMarginsTitle);

            Label lblTop = new Label { Text = "Lề trên (Top):", Location = new Point(12, 35), AutoSize = true };
            numTop = new NumericUpDown { Location = new Point(105, 32), Size = new Size(65, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)TopMargin };
            tabMargins.Controls.Add(lblTop);
            tabMargins.Controls.Add(numTop);

            Label lblBottom = new Label { Text = "Lề dưới (Bottom):", Location = new Point(190, 35), AutoSize = true };
            numBottom = new NumericUpDown { Location = new Point(290, 32), Size = new Size(65, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)BottomMargin };
            tabMargins.Controls.Add(lblBottom);
            tabMargins.Controls.Add(numBottom);

            Label lblLeft = new Label { Text = "Lề trái (Left):", Location = new Point(12, 70), AutoSize = true };
            numLeft = new NumericUpDown { Location = new Point(105, 67), Size = new Size(65, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)LeftMargin };
            tabMargins.Controls.Add(lblLeft);
            tabMargins.Controls.Add(numLeft);

            Label lblRight = new Label { Text = "Lề phải (Right):", Location = new Point(190, 70), AutoSize = true };
            numRight = new NumericUpDown { Location = new Point(290, 67), Size = new Size(65, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 5.0m, Value = (decimal)RightMargin };
            tabMargins.Controls.Add(lblRight);
            tabMargins.Controls.Add(numRight);

            // Orientation
            GroupBox grpOrient = new GroupBox { Text = "Hướng trang giấy (Orientation)", Location = new Point(12, 105), Size = new Size(345, 68) };
            rdoPortrait = new RadioButton { Text = "📄 Dọc (Portrait)", Location = new Point(20, 26), AutoSize = true, Checked = !IsLandscape };
            rdoLandscape = new RadioButton { Text = "📜 Ngang (Landscape)", Location = new Point(180, 26), AutoSize = true, Checked = IsLandscape };
            grpOrient.Controls.Add(rdoPortrait);
            grpOrient.Controls.Add(rdoLandscape);
            tabMargins.Controls.Add(grpOrient);

            // Preview Box (Real-Time Proportional Preview Engine)
            GroupBox grpPreview = new GroupBox { Text = "Xem trước trực tiếp (Preview)", Location = new Point(12, 180), Size = new Size(345, 175) };
            PictureBox picPreview = new PictureBox { Location = new Point((345 - 95) / 2, 20), Size = new Size(95, 125), BackColor = Color.White };
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
                    picPreview.Size = new Size(130, 95);
                    picPreview.Location = new Point((345 - 130) / 2, 35);
                }
                else
                {
                    picPreview.Size = new Size(95, 125);
                    picPreview.Location = new Point((345 - 95) / 2, 20);
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

            // ==================== TAB 2: KHỔ GIẤY (PAPER) ====================
            TabPage tabPaper = new TabPage("Khổ giấy (Paper)");
            tabPaper.BackColor = Color.White;

            Label lblPaperHead = new Label { Text = "Khổ giấy (Paper size):", Location = new Point(15, 12), AutoSize = true };
            cboPaper = new ComboBox { Location = new Point(15, 32), Size = new Size(335, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboPaper.Items.AddRange(new object[] {
                "Letter", "Tabloid", "Legal", "Statement", "Executive", "A3", "A4", "A5", "B4 (JIS)", "B5 (JIS)", "Tùy chỉnh (Custom size)"
            });
            cboPaper.SelectedItem = PaperSize;
            if (cboPaper.SelectedIndex == -1) cboPaper.SelectedIndex = 6; // Default A4

            Label lblPW = new Label { Text = "Chiều rộng (Width):", Location = new Point(15, 66), AutoSize = true };
            numPaperWidth = new NumericUpDown { Location = new Point(15, 86), Size = new Size(155, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 2.0m, Maximum = 40.0m, Value = (decimal)PaperWidthInches };

            Label lblPH = new Label { Text = "Chiều cao (Height):", Location = new Point(195, 66), AutoSize = true };
            numPaperHeight = new NumericUpDown { Location = new Point(195, 86), Size = new Size(155, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 2.0m, Maximum = 40.0m, Value = (decimal)PaperHeightInches };

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
            GroupBox grpSource = new GroupBox { Text = "Nguồn giấy (Paper source)", Location = new Point(15, 120), Size = new Size(335, 100) };
            Label lblTray1 = new Label { Text = "Trang đầu (First page):", Location = new Point(10, 20), AutoSize = true };
            ListBox lstTray1 = new ListBox { Location = new Point(10, 40), Size = new Size(150, 50) };
            lstTray1.Items.AddRange(new object[] { "Khay mặc định (Default)", "Nạp thủ công (Manual)" });
            lstTray1.SelectedIndex = 0;

            Label lblTray2 = new Label { Text = "Các trang khác (Other):", Location = new Point(175, 20), AutoSize = true };
            ListBox lstTray2 = new ListBox { Location = new Point(175, 40), Size = new Size(150, 50) };
            lstTray2.Items.AddRange(new object[] { "Khay mặc định (Default)", "Nạp thủ công (Manual)" });
            lstTray2.SelectedIndex = 0;

            grpSource.Controls.AddRange(new Control[] { lblTray1, lstTray1, lblTray2, lstTray2 });

            // Preview Group for Paper Tab
            GroupBox grpPaperPrev = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(15, 226), Size = new Size(335, 145) };
            picPaperPreview = new PictureBox { Location = new Point(15, 18), Size = new Size(95, 115), BackColor = Color.White };
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

            Label lblApplyPaper = new Label { Text = "Áp dụng vào (Apply to):", Location = new Point(130, 30), AutoSize = true };
            cboApplyToPaper = new ComboBox { Location = new Point(130, 52), Size = new Size(190, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboApplyToPaper.Items.AddRange(new object[] { "Toàn bộ tài liệu (Whole document)", "Từ điểm này trở đi (This point forward)" });
            cboApplyToPaper.SelectedIndex = 0;
            grpPaperPrev.Controls.Add(lblApplyPaper);
            grpPaperPrev.Controls.Add(cboApplyToPaper);

            Button btnPrintOpt = new Button { Text = "Tùy chọn in (Print Options)...", Location = new Point(130, 95), Size = new Size(190, 26), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
            grpPaperPrev.Controls.Add(btnPrintOpt);

            tabPaper.Controls.AddRange(new Control[] { lblPaperHead, cboPaper, lblPW, numPaperWidth, lblPH, numPaperHeight, grpSource, grpPaperPrev });
            tabControl.TabPages.Add(tabPaper);

            // ==================== TAB 3: BỐ TRÍ (LAYOUT) ====================
            TabPage tabLayout = new TabPage("Bố trí (Layout)");
            tabLayout.BackColor = Color.White;

            Label lblSecStart = new Label { Text = "Bắt đầu ngắt trang: Trang mới", Location = new Point(15, 20), AutoSize = true };
            Label lblHeader = new Label { Text = "Khoảng cách Đầu trang (Header): 0.5\"", Location = new Point(15, 60), AutoSize = true };
            Label lblFooter = new Label { Text = "Khoảng cách Chân trang (Footer): 0.5\"", Location = new Point(15, 90), AutoSize = true };
            Label lblVertAlign = new Label { Text = "Căn lề dọc trang: Căn đỉnh (Top)", Location = new Point(15, 130), AutoSize = true };

            tabLayout.Controls.Add(lblSecStart);
            tabLayout.Controls.Add(lblHeader);
            tabLayout.Controls.Add(lblFooter);
            tabLayout.Controls.Add(lblVertAlign);
            tabControl.TabPages.Add(tabLayout);

            if (initialTabIndex >= 0 && initialTabIndex < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = initialTabIndex;
            }

            Controls.Add(tabControl);

            // Buttons: Đồng ý (OK), Hủy bỏ (Cancel), Đặt làm mặc định
            Button btnDefault = new Button
            {
                Text = "Đặt làm mặc định (Default)",
                Location = new Point(12, 442),
                Size = new Size(160, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(btnDefault);

            Button btnOK = new Button
            {
                Text = "Đồng ý (OK)",
                DialogResult = DialogResult.OK,
                Location = new Point(218, 442),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
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
                if (OnApply != null) OnApply();
            };
            Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(310, 442),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(btnCancel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
}

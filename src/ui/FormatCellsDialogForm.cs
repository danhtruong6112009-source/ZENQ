using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Định dạng Ô (Format Cells) chuẩn Microsoft Excel 365.
    /// Hỗ trợ đầy đủ 6 tab: Số (Number), Căn chỉnh (Alignment), Phông chữ (Font),
    /// Đường viền (Border), Tô màu (Fill), và Bảo vệ (Protection).
    /// </summary>
    public class FormatCellsDialogForm : Form
    {
        private readonly CalcView _calcView;
        private TabControl _tabControl;

        // Tab 1: Số (Number)
        private ListBox _lstCategory;
        private NumericUpDown _numDecimals;
        private ComboBox _cboCurrencySymbol;
        private Label _lblSamplePreview;
        private Panel _pnlNumberOptions;

        // Tab 2: Căn chỉnh (Alignment)
        private ComboBox _cboHAlign;
        private ComboBox _cboVAlign;
        private NumericUpDown _numIndent;
        private CheckBox _chkWrapText;
        private CheckBox _chkMergeCells;

        // Tab 3: Phông chữ (Font)
        private ListBox _lstFontName;
        private ListBox _lstFontStyle;
        private ListBox _lstFontSize;
        private TextBox _txtFontName;
        private TextBox _txtFontStyle;
        private TextBox _txtFontSize;
        private Button _btnTextColor;
        private Color _selectedTextColor;
        private CheckBox _chkUnderline;
        private CheckBox _chkStrikethrough;
        private Label _lblFontPreview;

        // Tab 4: Đường viền (Border)
        private Button _btnBorderNone;
        private Button _btnBorderOutline;
        private Button _btnBorderAll;
        private CheckBox _chkBorderTop;
        private CheckBox _chkBorderBottom;
        private CheckBox _chkBorderLeft;
        private CheckBox _chkBorderRight;
        private Button _btnBorderColor;
        private Color _selectedBorderColor = Color.Black;
        private ComboBox _cboBorderStyle;

        // Tab 5: Tô màu (Fill)
        private Panel _pnlColorPalette;
        private Color _selectedBgColor = Color.White;
        private Label _lblFillPreview;
        private Button _btnNoColor;

        // Tab 6: Bảo vệ (Protection)
        private CheckBox _chkLocked;
        private CheckBox _chkHidden;

        // Buttons
        private Button _btnOk;
        private Button _btnCancel;

        public FormatCellsDialogForm(CalcView calcView, int initialTab = 0)
        {
            _calcView = calcView;
            InitializeComponent();
            LoadCurrentFormat();

            if (_tabControl != null && initialTab >= 0 && initialTab < _tabControl.TabCount)
            {
                _tabControl.SelectedIndex = initialTab;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Định dạng Ô (Format Cells)";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(243, 244, 246);

            _tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(540, 415),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };

            // 1. Tab Số (Number)
            TabPage tabNumber = new TabPage("Số (Number)");
            tabNumber.BackColor = Color.White;
            BuildNumberTab(tabNumber);
            _tabControl.TabPages.Add(tabNumber);

            // 2. Tab Căn chỉnh (Alignment)
            TabPage tabAlignment = new TabPage("Căn chỉnh (Alignment)");
            tabAlignment.BackColor = Color.White;
            BuildAlignmentTab(tabAlignment);
            _tabControl.TabPages.Add(tabAlignment);

            // 3. Tab Phông chữ (Font)
            TabPage tabFont = new TabPage("Phông chữ (Font)");
            tabFont.BackColor = Color.White;
            BuildFontTab(tabFont);
            _tabControl.TabPages.Add(tabFont);

            // 4. Tab Đường viền (Border)
            TabPage tabBorder = new TabPage("Đường viền (Border)");
            tabBorder.BackColor = Color.White;
            BuildBorderTab(tabBorder);
            _tabControl.TabPages.Add(tabBorder);

            // 5. Tab Tô màu (Fill)
            TabPage tabFill = new TabPage("Tô màu (Fill)");
            tabFill.BackColor = Color.White;
            BuildFillTab(tabFill);
            _tabControl.TabPages.Add(tabFill);

            // 6. Tab Bảo vệ (Protection)
            TabPage tabProtection = new TabPage("Bảo vệ (Protection)");
            tabProtection.BackColor = Color.White;
            BuildProtectionTab(tabProtection);
            _tabControl.TabPages.Add(tabProtection);

            this.Controls.Add(_tabControl);

            // Buttons OK / Cancel
            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(376, 437),
                Size = new Size(84, 30),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) => ApplyFormatting();
            this.Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(468, 437),
                Size = new Size(84, 30),
                BackColor = Color.FromArgb(229, 231, 235),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        #region Tab 1: Số (Number)
        private void BuildNumberTab(TabPage tab)
        {
            Label lblCat = new Label { Text = "Loại định dạng (Category):", Location = new Point(16, 14), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            tab.Controls.Add(lblCat);

            _lstCategory = new ListBox { Location = new Point(16, 36), Size = new Size(160, 260) };
            _lstCategory.Items.AddRange(new object[] {
                "Chung (General)",
                "Số (Number)",
                "Tiền tệ (Currency)",
                "Kế toán (Accounting)",
                "Ngày tháng (Date)",
                "Giờ (Time)",
                "Phần trăm (Percentage)",
                "Phân số (Fraction)",
                "Khoa học (Scientific)",
                "Văn bản (Text)"
            });
            _lstCategory.SelectedIndex = 0;
            _lstCategory.SelectedIndexChanged += (s, e) => UpdateNumberOptionsUI();
            tab.Controls.Add(_lstCategory);

            // Sample Box
            GroupBox grpSample = new GroupBox { Text = "Mẫu xem trước (Sample)", Location = new Point(190, 30), Size = new Size(330, 68) };
            _lblSamplePreview = new Label { Text = "12345.67", Location = new Point(16, 26), Size = new Size(295, 28), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11f, FontStyle.Bold), BackColor = Color.FromArgb(249, 250, 251), BorderStyle = BorderStyle.FixedSingle };
            grpSample.Controls.Add(_lblSamplePreview);
            tab.Controls.Add(grpSample);

            // Detailed Options
            _pnlNumberOptions = new Panel { Location = new Point(190, 110), Size = new Size(330, 250) };
            tab.Controls.Add(_pnlNumberOptions);

            Label lblDec = new Label { Text = "Số chữ số thập phân:", Location = new Point(0, 10), AutoSize = true };
            _pnlNumberOptions.Controls.Add(lblDec);

            _numDecimals = new NumericUpDown { Location = new Point(160, 8), Size = new Size(60, 24), Minimum = 0, Maximum = 10, Value = 2 };
            _numDecimals.ValueChanged += (s, e) => UpdateSamplePreview();
            _pnlNumberOptions.Controls.Add(_numDecimals);

            Label lblSymbol = new Label { Text = "Ký hiệu tiền tệ:", Location = new Point(0, 46), AutoSize = true };
            _pnlNumberOptions.Controls.Add(lblSymbol);

            _cboCurrencySymbol = new ComboBox { Location = new Point(120, 44), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboCurrencySymbol.Items.AddRange(new object[] { "₫ (VND)", "$ (USD)", "€ (EUR)", "¥ (JPY)", "£ (GBP)", "None (Không)" });
            _cboCurrencySymbol.SelectedIndex = 0;
            _cboCurrencySymbol.SelectedIndexChanged += (s, e) => UpdateSamplePreview();
            _pnlNumberOptions.Controls.Add(_cboCurrencySymbol);
        }

        private void UpdateNumberOptionsUI()
        {
            int cat = _lstCategory.SelectedIndex;
            _numDecimals.Enabled = (cat == 1 || cat == 2 || cat == 3 || cat == 6 || cat == 8);
            _cboCurrencySymbol.Enabled = (cat == 2 || cat == 3);
            UpdateSamplePreview();
        }

        private void UpdateSamplePreview()
        {
            int cat = _lstCategory.SelectedIndex;
            int dec = (int)_numDecimals.Value;
            double sampleVal = 12345.6789;

            switch (cat)
            {
                case 0: // General
                    _lblSamplePreview.Text = "12345.6789";
                    break;
                case 1: // Number
                    _lblSamplePreview.Text = sampleVal.ToString("N" + dec);
                    break;
                case 2: // Currency
                case 3: // Accounting
                    string sym = _cboCurrencySymbol.SelectedIndex switch
                    {
                        0 => " ₫",
                        1 => "$",
                        2 => "€",
                        3 => "¥",
                        4 => "£",
                        _ => ""
                    };
                    if (sym.StartsWith(" ")) _lblSamplePreview.Text = sampleVal.ToString("N" + dec) + sym;
                    else _lblSamplePreview.Text = sym + sampleVal.ToString("N" + dec);
                    break;
                case 4: // Date
                    _lblSamplePreview.Text = DateTime.Now.ToString("dd/MM/yyyy");
                    break;
                case 5: // Time
                    _lblSamplePreview.Text = DateTime.Now.ToString("HH:mm:ss");
                    break;
                case 6: // Percentage
                    _lblSamplePreview.Text = (0.7545).ToString("P" + dec);
                    break;
                case 7: // Fraction
                    _lblSamplePreview.Text = "12345 2/3";
                    break;
                case 8: // Scientific
                    _lblSamplePreview.Text = sampleVal.ToString("E" + dec);
                    break;
                case 9: // Text
                    _lblSamplePreview.Text = "12345.67";
                    break;
            }
        }
        #endregion

        #region Tab 2: Căn chỉnh (Alignment)
        private void BuildAlignmentTab(TabPage tab)
        {
            GroupBox grpAlign = new GroupBox { Text = "Căn lề văn bản (Text alignment)", Location = new Point(16, 16), Size = new Size(500, 140) };

            Label lblH = new Label { Text = "Căn lề ngang (Horizontal):", Location = new Point(16, 30), AutoSize = true };
            grpAlign.Controls.Add(lblH);

            _cboHAlign = new ComboBox { Location = new Point(180, 26), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboHAlign.Items.AddRange(new object[] { "Mặc định (General)", "Trái (Left)", "Giữa (Center)", "Phải (Right)" });
            _cboHAlign.SelectedIndex = 0;
            grpAlign.Controls.Add(_cboHAlign);

            Label lblIndent = new Label { Text = "Thụt lề (Indent):", Location = new Point(380, 30), AutoSize = true };
            grpAlign.Controls.Add(lblIndent);

            _numIndent = new NumericUpDown { Location = new Point(440, 28), Size = new Size(46, 24), Minimum = 0, Maximum = 15, Value = 0 };
            grpAlign.Controls.Add(_numIndent);

            Label lblV = new Label { Text = "Căn lề dọc (Vertical):", Location = new Point(16, 76), AutoSize = true };
            grpAlign.Controls.Add(lblV);

            _cboVAlign = new ComboBox { Location = new Point(180, 72), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboVAlign.Items.AddRange(new object[] { "Trên (Top)", "Giữa (Middle)", "Dưới (Bottom)" });
            _cboVAlign.SelectedIndex = 1;
            grpAlign.Controls.Add(_cboVAlign);

            tab.Controls.Add(grpAlign);

            GroupBox grpControl = new GroupBox { Text = "Điều khiển văn bản (Text control)", Location = new Point(16, 170), Size = new Size(500, 110) };

            _chkWrapText = new CheckBox { Text = "Tự động xuống dòng (Wrap text)", Location = new Point(16, 30), AutoSize = true };
            grpControl.Controls.Add(_chkWrapText);

            _chkMergeCells = new CheckBox { Text = "Gộp các ô đang chọn (Merge cells)", Location = new Point(16, 64), AutoSize = true };
            grpControl.Controls.Add(_chkMergeCells);

            tab.Controls.Add(grpControl);
        }
        #endregion

        #region Tab 3: Phông chữ (Font)
        private void BuildFontTab(TabPage tab)
        {
            // Font Family
            Label lblFont = new Label { Text = "Phông chữ:", Location = new Point(16, 12), AutoSize = true };
            tab.Controls.Add(lblFont);

            _txtFontName = new TextBox { Location = new Point(16, 32), Size = new Size(180, 24), Text = "Calibri" };
            tab.Controls.Add(_txtFontName);

            _lstFontName = new ListBox { Location = new Point(16, 58), Size = new Size(180, 160) };
            string[] commonFonts = new string[] { "Calibri", "Aptos", "Arial", "Segoe UI", "Times New Roman", "Tahoma", "Verdana", "Georgia", "Courier New", "Trebuchet MS", "Consolas" };
            _lstFontName.Items.AddRange(commonFonts);
            _lstFontName.SelectedIndex = 0;
            _lstFontName.SelectedIndexChanged += (s, e) => {
                if (_lstFontName.SelectedItem != null) _txtFontName.Text = _lstFontName.SelectedItem.ToString();
                UpdateFontPreview();
            };
            tab.Controls.Add(_lstFontName);

            // Font Style
            Label lblStyle = new Label { Text = "Kiểu chữ:", Location = new Point(208, 12), AutoSize = true };
            tab.Controls.Add(lblStyle);

            _txtFontStyle = new TextBox { Location = new Point(208, 32), Size = new Size(120, 24), Text = "Thường" };
            tab.Controls.Add(_txtFontStyle);

            _lstFontStyle = new ListBox { Location = new Point(208, 58), Size = new Size(120, 160) };
            _lstFontStyle.Items.AddRange(new object[] { "Thường (Regular)", "Nghiêng (Italic)", "Đậm (Bold)", "Đậm Nghiêng (Bold Italic)" });
            _lstFontStyle.SelectedIndex = 0;
            _lstFontStyle.SelectedIndexChanged += (s, e) => {
                if (_lstFontStyle.SelectedItem != null) _txtFontStyle.Text = _lstFontStyle.SelectedItem.ToString();
                UpdateFontPreview();
            };
            tab.Controls.Add(_lstFontStyle);

            // Font Size
            Label lblSize = new Label { Text = "Cỡ chữ:", Location = new Point(340, 12), AutoSize = true };
            tab.Controls.Add(lblSize);

            _txtFontSize = new TextBox { Location = new Point(340, 32), Size = new Size(60, 24), Text = "11" };
            tab.Controls.Add(_txtFontSize);

            _lstFontSize = new ListBox { Location = new Point(340, 58), Size = new Size(60, 160) };
            _lstFontSize.Items.AddRange(new object[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 });
            _lstFontSize.SelectedIndex = 3; // 11
            _lstFontSize.SelectedIndexChanged += (s, e) => {
                if (_lstFontSize.SelectedItem != null) _txtFontSize.Text = _lstFontSize.SelectedItem.ToString();
                UpdateFontPreview();
            };
            tab.Controls.Add(_lstFontSize);

            // Text Color
            Label lblCol = new Label { Text = "Màu chữ:", Location = new Point(416, 12), AutoSize = true };
            tab.Controls.Add(lblCol);

            _selectedTextColor = Color.Black;
            _btnTextColor = new Button { Location = new Point(416, 32), Size = new Size(100, 26), Text = "Màu chữ...", BackColor = Color.Black, ForeColor = Color.White };
            _btnTextColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = _selectedTextColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        _selectedTextColor = cd.Color;
                        _btnTextColor.BackColor = cd.Color;
                        _btnTextColor.ForeColor = (cd.Color.R * 0.299 + cd.Color.G * 0.587 + cd.Color.B * 0.114 > 150) ? Color.Black : Color.White;
                        UpdateFontPreview();
                    }
                }
            };
            tab.Controls.Add(_btnTextColor);

            // Effects
            _chkUnderline = new CheckBox { Text = "Gạch chân (Underline)", Location = new Point(416, 75), AutoSize = true };
            _chkUnderline.CheckedChanged += (s, e) => UpdateFontPreview();
            tab.Controls.Add(_chkUnderline);

            _chkStrikethrough = new CheckBox { Text = "Gạch ngang (Strikethrough)", Location = new Point(416, 105), AutoSize = true };
            _chkStrikethrough.CheckedChanged += (s, e) => UpdateFontPreview();
            tab.Controls.Add(_chkStrikethrough);

            // Preview Box
            GroupBox grpFontPreview = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(16, 230), Size = new Size(500, 100) };
            _lblFontPreview = new Label { Text = "AaBbCc ZyZ 1234", Location = new Point(16, 24), Size = new Size(468, 60), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White };
            grpFontPreview.Controls.Add(_lblFontPreview);
            tab.Controls.Add(grpFontPreview);
        }

        private void UpdateFontPreview()
        {
            if (_lblFontPreview == null) return;
            string fn = !string.IsNullOrEmpty(_txtFontName?.Text) ? _txtFontName.Text : "Calibri";
            float sz = 14f;
            if (float.TryParse(_txtFontSize?.Text, out float parsedSz) && parsedSz > 4) sz = Math.Min(24f, parsedSz);

            FontStyle fs = FontStyle.Regular;
            int styleIdx = _lstFontStyle?.SelectedIndex ?? 0;
            if (styleIdx == 1) fs = FontStyle.Italic;
            else if (styleIdx == 2) fs = FontStyle.Bold;
            else if (styleIdx == 3) fs = FontStyle.Bold | FontStyle.Italic;

            if (_chkUnderline?.Checked == true) fs |= FontStyle.Underline;
            if (_chkStrikethrough?.Checked == true) fs |= FontStyle.Strikeout;

            try
            {
                _lblFontPreview.Font = new Font(fn, sz, fs);
            }
            catch
            {
                _lblFontPreview.Font = new Font("Segoe UI", sz, fs);
            }
            _lblFontPreview.ForeColor = _selectedTextColor;
        }
        #endregion

        #region Tab 4: Đường viền (Border)
        private void BuildBorderTab(TabPage tab)
        {
            Label lblPresets = new Label { Text = "Mẫu thiết lập sẵn (Presets):", Location = new Point(16, 14), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            tab.Controls.Add(lblPresets);

            _btnBorderNone = new Button { Text = "❌ Không viền", Location = new Point(16, 38), Size = new Size(110, 36), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnBorderNone.Click += (s, e) => SetBorderToggles(false, false, false, false);
            tab.Controls.Add(_btnBorderNone);

            _btnBorderOutline = new Button { Text = "⬜ Viền ngoài", Location = new Point(136, 38), Size = new Size(110, 36), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnBorderOutline.Click += (s, e) => SetBorderToggles(true, true, true, true);
            tab.Controls.Add(_btnBorderOutline);

            _btnBorderAll = new Button { Text = "⊞ Tất cả viền", Location = new Point(256, 38), Size = new Size(110, 36), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnBorderAll.Click += (s, e) => SetBorderToggles(true, true, true, true);
            tab.Controls.Add(_btnBorderAll);

            // Individual toggles
            GroupBox grpBorder = new GroupBox { Text = "Tùy chỉnh từng cạnh (Borders)", Location = new Point(16, 90), Size = new Size(500, 160) };

            _chkBorderTop = new CheckBox { Text = "Viền trên (Top Border)", Location = new Point(20, 32), AutoSize = true };
            grpBorder.Controls.Add(_chkBorderTop);

            _chkBorderBottom = new CheckBox { Text = "Viền dưới (Bottom Border)", Location = new Point(20, 64), AutoSize = true };
            grpBorder.Controls.Add(_chkBorderBottom);

            _chkBorderLeft = new CheckBox { Text = "Viền trái (Left Border)", Location = new Point(200, 32), AutoSize = true };
            grpBorder.Controls.Add(_chkBorderLeft);

            _chkBorderRight = new CheckBox { Text = "Viền phải (Right Border)", Location = new Point(200, 64), AutoSize = true };
            grpBorder.Controls.Add(_chkBorderRight);

            Label lblStyle = new Label { Text = "Kiểu nét vẽ:", Location = new Point(20, 110), AutoSize = true };
            grpBorder.Controls.Add(lblStyle);

            _cboBorderStyle = new ComboBox { Location = new Point(100, 106), Size = new Size(120, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboBorderStyle.Items.AddRange(new object[] { "Mảnh (Thin)", "Trung bình (Medium)", "Đậm (Thick)", "Nét đứt (Dashed)", "Nét đôi (Double)" });
            _cboBorderStyle.SelectedIndex = 0;
            grpBorder.Controls.Add(_cboBorderStyle);

            Label lblCol = new Label { Text = "Màu viền:", Location = new Point(260, 110), AutoSize = true };
            grpBorder.Controls.Add(lblCol);

            _btnBorderColor = new Button { Location = new Point(330, 106), Size = new Size(120, 26), Text = "Chọn màu...", BackColor = Color.Black, ForeColor = Color.White };
            _btnBorderColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = _selectedBorderColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        _selectedBorderColor = cd.Color;
                        _btnBorderColor.BackColor = cd.Color;
                        _btnBorderColor.ForeColor = (cd.Color.R * 0.299 + cd.Color.G * 0.587 + cd.Color.B * 0.114 > 150) ? Color.Black : Color.White;
                    }
                }
            };
            grpBorder.Controls.Add(_btnBorderColor);

            tab.Controls.Add(grpBorder);
        }

        private void SetBorderToggles(bool top, bool bottom, bool left, bool right)
        {
            _chkBorderTop.Checked = top;
            _chkBorderBottom.Checked = bottom;
            _chkBorderLeft.Checked = left;
            _chkBorderRight.Checked = right;
        }
        #endregion

        #region Tab 5: Tô màu (Fill)
        private void BuildFillTab(TabPage tab)
        {
            Label lblTitle = new Label { Text = "Màu nền ô (Background color):", Location = new Point(16, 14), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            tab.Controls.Add(lblTitle);

            _btnNoColor = new Button { Text = "Không màu (No Color)", Location = new Point(16, 40), Size = new Size(160, 30), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnNoColor.Click += (s, e) => {
                _selectedBgColor = Color.Empty;
                UpdateFillPreview();
            };
            tab.Controls.Add(_btnNoColor);

            // Palette container
            _pnlColorPalette = new Panel { Location = new Point(16, 80), Size = new Size(500, 170), AutoScroll = false };
            Color[] palette = new Color[] {
                Color.White, Color.Black, Color.FromArgb(231, 230, 230), Color.FromArgb(68, 84, 106), Color.FromArgb(68, 114, 196),
                Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0), Color.FromArgb(91, 155, 213), Color.FromArgb(112, 173, 71),
                Color.FromArgb(242, 242, 242), Color.FromArgb(127, 127, 127), Color.FromArgb(208, 206, 206), Color.FromArgb(214, 220, 228), Color.FromArgb(222, 235, 247),
                Color.FromArgb(251, 229, 214), Color.FromArgb(237, 237, 237), Color.FromArgb(255, 242, 204), Color.FromArgb(222, 235, 247), Color.FromArgb(226, 239, 218),
                Color.FromArgb(217, 217, 217), Color.FromArgb(89, 89, 89), Color.FromArgb(174, 170, 170), Color.FromArgb(172, 185, 202), Color.FromArgb(189, 215, 238),
                Color.FromArgb(248, 203, 173), Color.FromArgb(219, 219, 219), Color.FromArgb(255, 230, 153), Color.FromArgb(189, 215, 238), Color.FromArgb(198, 224, 180),
                Color.FromArgb(191, 191, 191), Color.FromArgb(38, 38, 38), Color.FromArgb(117, 113, 113), Color.FromArgb(132, 151, 176), Color.FromArgb(143, 170, 220),
                Color.FromArgb(244, 176, 132), Color.FromArgb(201, 201, 201), Color.FromArgb(255, 217, 102), Color.FromArgb(155, 194, 230), Color.FromArgb(169, 208, 142)
            };

            int px = 4, py = 4, colW = 46, colH = 26;
            for (int i = 0; i < palette.Length; i++)
            {
                Color c = palette[i];
                Button btnCol = new Button
                {
                    Location = new Point(px, py),
                    Size = new Size(colW, colH),
                    BackColor = c,
                    FlatStyle = FlatStyle.Flat
                };
                btnCol.FlatAppearance.BorderColor = Color.LightGray;
                btnCol.Click += (s, e) => {
                    _selectedBgColor = c;
                    UpdateFillPreview();
                };
                _pnlColorPalette.Controls.Add(btnCol);

                px += colW + 4;
                if ((i + 1) % 10 == 0)
                {
                    px = 4;
                    py += colH + 4;
                }
            }
            tab.Controls.Add(_pnlColorPalette);

            // More colors button
            Button btnMore = new Button { Text = "Nhiều màu hơn (More Colors)...", Location = new Point(16, 260), Size = new Size(200, 28), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnMore.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = _selectedBgColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        _selectedBgColor = cd.Color;
                        UpdateFillPreview();
                    }
                }
            };
            tab.Controls.Add(btnMore);

            // Fill preview
            GroupBox grpFillPrev = new GroupBox { Text = "Mẫu xem trước (Sample)", Location = new Point(230, 255), Size = new Size(286, 75) };
            _lblFillPreview = new Label { Location = new Point(16, 22), Size = new Size(254, 38), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            grpFillPrev.Controls.Add(_lblFillPreview);
            tab.Controls.Add(grpFillPrev);
        }

        private void UpdateFillPreview()
        {
            if (_lblFillPreview == null) return;
            if (_selectedBgColor.IsEmpty || _selectedBgColor.A == 0)
            {
                _lblFillPreview.BackColor = Color.White;
                _lblFillPreview.Text = "Không màu";
                _lblFillPreview.TextAlign = ContentAlignment.MiddleCenter;
            }
            else
            {
                _lblFillPreview.BackColor = _selectedBgColor;
                _lblFillPreview.Text = "";
            }
        }
        #endregion

        #region Tab 6: Bảo vệ (Protection)
        private void BuildProtectionTab(TabPage tab)
        {
            _chkLocked = new CheckBox
            {
                Text = "Khóa ô (Locked)",
                Location = new Point(24, 24),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            tab.Controls.Add(_chkLocked);

            Label lblLockedDesc = new Label
            {
                Text = "Khóa ô hoặc ẩn công thức chỉ có tác dụng khi bạn bảo vệ bảng tính\n(Tab Xem lại > Bảo vệ Bảng tính / Review > Protect Sheet).",
                Location = new Point(44, 48),
                Size = new Size(460, 40),
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblLockedDesc);

            _chkHidden = new CheckBox
            {
                Text = "Ẩn công thức (Hidden)",
                Location = new Point(24, 100),
                AutoSize = true,
                Checked = false,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            tab.Controls.Add(_chkHidden);

            Label lblHiddenDesc = new Label
            {
                Text = "Ngăn người dùng xem công thức hiển thị trên thanh công thức khi chọn ô.",
                Location = new Point(44, 124),
                Size = new Size(460, 40),
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblHiddenDesc);
        }
        #endregion

        private void LoadCurrentFormat()
        {
            if (_calcView == null) return;
            CalcSelectionFormat fmt = _calcView.GetSelectionFormat();
            if (fmt == null) return;

            // Load Font
            if (!string.IsNullOrEmpty(fmt.FontName))
            {
                _txtFontName.Text = fmt.FontName;
                int idx = _lstFontName.FindStringExact(fmt.FontName);
                if (idx >= 0) _lstFontName.SelectedIndex = idx;
            }
            if (fmt.FontSize > 0)
            {
                _txtFontSize.Text = fmt.FontSize.ToString("0.#");
                int sIdx = _lstFontSize.Items.IndexOf((int)fmt.FontSize);
                if (sIdx >= 0) _lstFontSize.SelectedIndex = sIdx;
            }

            int fStyle = 0;
            if (fmt.Bold && fmt.Italic) fStyle = 3;
            else if (fmt.Bold) fStyle = 2;
            else if (fmt.Italic) fStyle = 1;
            _lstFontStyle.SelectedIndex = fStyle;

            _chkUnderline.Checked = fmt.Underline;
            if (fmt.TextColor != 0)
            {
                _selectedTextColor = Color.FromArgb(fmt.TextColor);
                _btnTextColor.BackColor = _selectedTextColor;
                _btnTextColor.ForeColor = (_selectedTextColor.R * 0.299 + _selectedTextColor.G * 0.587 + _selectedTextColor.B * 0.114 > 150) ? Color.Black : Color.White;
            }

            // Load Alignment
            if (fmt.Align >= 0 && fmt.Align <= 3) _cboHAlign.SelectedIndex = (fmt.Align == 0) ? 1 : (fmt.Align == 1 ? 2 : 3);
            if (fmt.VAlign >= 0 && fmt.VAlign <= 2) _cboVAlign.SelectedIndex = fmt.VAlign;
            _chkWrapText.Checked = fmt.WrapText;
            _chkMergeCells.Checked = fmt.IsMerged;
            _numIndent.Value = Math.Max(0, Math.Min(15, fmt.Indent));

            // Load Number Format
            if (!string.IsNullOrEmpty(fmt.NumberFormat))
            {
                string nf = fmt.NumberFormat.ToLowerInvariant();
                if (nf.Contains("curren") || nf.Contains("tiền")) _lstCategory.SelectedIndex = 2;
                else if (nf.Contains("number") || nf.Contains("số")) _lstCategory.SelectedIndex = 1;
                else if (nf.Contains("percent") || nf.Contains("%")) _lstCategory.SelectedIndex = 6;
                else if (nf.Contains("date") || nf.Contains("ngày")) _lstCategory.SelectedIndex = 4;
                else if (nf.Contains("text") || nf.Contains("chữ")) _lstCategory.SelectedIndex = 9;
                else _lstCategory.SelectedIndex = 0;
            }
            if (fmt.DecimalPlaces >= 0 && fmt.DecimalPlaces <= 10)
            {
                _numDecimals.Value = fmt.DecimalPlaces;
            }

            // Load Fill
            if (fmt.BgColor != 0 && fmt.BgColor != unchecked((int)0xFFFFFFFF))
            {
                _selectedBgColor = Color.FromArgb(fmt.BgColor);
                UpdateFillPreview();
            }

            UpdateFontPreview();
            UpdateSamplePreview();
        }

        private void ApplyFormatting()
        {
            if (_calcView == null) return;
            _calcView.SaveUndoState();

            // 1. Phông chữ (Font)
            string fontName = !string.IsNullOrEmpty(_txtFontName.Text) ? _txtFontName.Text.Trim() : "Calibri";
            float fontSize = 11f;
            if (float.TryParse(_txtFontSize.Text, out float parsedSz) && parsedSz > 4) fontSize = parsedSz;

            bool bold = (_lstFontStyle.SelectedIndex == 2 || _lstFontStyle.SelectedIndex == 3);
            bool italic = (_lstFontStyle.SelectedIndex == 1 || _lstFontStyle.SelectedIndex == 3);
            bool underline = _chkUnderline.Checked;
            bool strikethrough = _chkStrikethrough.Checked;
            Color textCol = _selectedTextColor;

            // 2. Căn chỉnh (Alignment)
            int hAlign = _cboHAlign.SelectedIndex switch
            {
                1 => 0, // Left
                2 => 1, // Center
                3 => 2, // Right
                _ => 0
            };
            int vAlign = _cboVAlign.SelectedIndex; // 0: Top, 1: Middle, 2: Bottom
            int indent = (int)_numIndent.Value;
            bool wrap = _chkWrapText.Checked;

            // 3. Số (Number)
            string numFmt = _lstCategory.SelectedIndex switch
            {
                1 => "Number",
                2 => "Currency",
                3 => "Currency",
                4 => "Date",
                5 => "Time",
                6 => "Percent",
                7 => "Fraction",
                8 => "Scientific",
                9 => "Text",
                _ => "General"
            };
            int decPlaces = (int)_numDecimals.Value;

            // 4. Đường viền (Borders)
            bool bTop = _chkBorderTop.Checked;
            bool bBottom = _chkBorderBottom.Checked;
            bool bLeft = _chkBorderLeft.Checked;
            bool bRight = _chkBorderRight.Checked;
            int bThickness = _cboBorderStyle.SelectedIndex switch
            {
                1 => 2, // Medium
                2 => 3, // Thick
                3 => 1, // Dashed
                4 => 4, // Double
                _ => 1  // Thin
            };
            Color bCol = _selectedBorderColor;

            // 5. Tô màu (Fill)
            Color bgCol = _selectedBgColor;

            // Áp dụng hàng loạt cho vùng ô đang chọn (ForEachSelectedCell)
            _calcView.ForEachSelectedCell(cell =>
            {
                if (cell == null || cell.Format == null) return;

                // Font
                cell.Format.FontName = fontName;
                cell.Format.FontSize = fontSize;
                cell.Format.Bold = bold;
                cell.Format.Italic = italic;
                cell.Format.Underline = underline;
                cell.Format.Strikethrough = strikethrough;
                cell.Format.TextColor = textCol.ToArgb();

                // Alignment
                cell.Format.Align = hAlign;
                cell.Format.VAlign = vAlign;
                cell.Format.Indent = indent;
                cell.Format.WrapText = wrap;

                // Number
                cell.Format.NumberFormat = numFmt;
                cell.Format.DecimalPlaces = decPlaces;

                // Fill
                if (!bgCol.IsEmpty && bgCol.A > 0)
                {
                    cell.Format.BgColor = bgCol.ToArgb();
                }

                // Borders
                if (cell.Format.Borders == null) cell.Format.Borders = new CellBorderSettings();
                if (bTop) { cell.Format.Borders.Top = true; cell.Format.Borders.TopColor = bCol.ToArgb(); cell.Format.Borders.TopThickness = bThickness; }
                if (bBottom) { cell.Format.Borders.Bottom = true; cell.Format.Borders.BottomColor = bCol.ToArgb(); cell.Format.Borders.BottomThickness = bThickness; }
                if (bLeft) { cell.Format.Borders.Left = true; cell.Format.Borders.LeftColor = bCol.ToArgb(); cell.Format.Borders.LeftThickness = bThickness; }
                if (bRight) { cell.Format.Borders.Right = true; cell.Format.Borders.RightColor = bCol.ToArgb(); cell.Format.Borders.RightThickness = bThickness; }
            });

            if (_chkMergeCells.Checked)
            {
                _calcView.MergeAndCenter();
            }

            _calcView.Invalidate();
        }
    }
}

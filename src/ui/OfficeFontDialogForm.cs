using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Phông chữ (Font Dialog) chuẩn Microsoft Office 365 (Word/Writer).
    /// </summary>
    public class OfficeFontDialogForm : Form
    {
        public string FontNameResult { get; set; } = "Calibri";
        public float FontSizeResult { get; set; } = 11f;
        public bool BoldResult { get; set; } = false;
        public bool ItalicResult { get; set; } = false;
        public bool UnderlineResult { get; set; } = false;
        public bool StrikethroughResult { get; set; } = false;
        public bool SuperscriptResult { get; set; } = false;
        public bool SubscriptResult { get; set; } = false;
        public Color TextColorResult { get; set; } = Color.Black;
        public float CharacterSpacingResult { get; set; } = 0f;

        private ListBox _lstFont;
        private ListBox _lstStyle;
        private ListBox _lstSize;
        private TextBox _txtFont;
        private TextBox _txtStyle;
        private TextBox _txtSize;

        private Button _btnColor;
        private CheckBox _chkStrike;
        private CheckBox _chkDoubleStrike;
        private CheckBox _chkSuperscript;
        private CheckBox _chkSubscript;
        private CheckBox _chkSmallCaps;
        private CheckBox _chkAllCaps;

        private ComboBox _cboUnderlineStyle;
        private Button _btnUnderlineColor;
        private Color _selectedUnderlineColor = Color.Black;

        private ComboBox _cboSpacingType;
        private NumericUpDown _numSpacingPt;

        private Label _lblPreview;

        private Button _btnOk;
        private Button _btnCancel;

        public OfficeFontDialogForm(string fontName, float fontSize, bool bold, bool italic, bool underline, bool strike, Color textColor)
        {
            FontNameResult = !string.IsNullOrEmpty(fontName) ? fontName : "Calibri";
            FontSizeResult = fontSize > 0 ? fontSize : 11f;
            BoldResult = bold;
            ItalicResult = italic;
            UnderlineResult = underline;
            StrikethroughResult = strike;
            TextColorResult = textColor != Color.Transparent && textColor.A > 0 ? textColor : Color.Black;

            InitializeComponent();
            PopulateCurrentValues();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            this.Text = "Phông chữ (Font)";
            this.Size = new Size(540, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Tab Control: Phông chữ & Khoảng cách nâng cao
            TabControl tabCtrl = new TabControl { Location = new Point(12, 12), Size = new Size(502, 450) };

            TabPage tabFont = new TabPage("Phông chữ (Font)");
            tabFont.BackColor = Color.White;
            BuildFontTab(tabFont);
            tabCtrl.TabPages.Add(tabFont);

            TabPage tabAdvanced = new TabPage("Nâng cao (Advanced)");
            tabAdvanced.BackColor = Color.White;
            BuildAdvancedTab(tabAdvanced);
            tabCtrl.TabPages.Add(tabAdvanced);

            this.Controls.Add(tabCtrl);

            // Bottom Buttons
            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(334, 474),
                Size = new Size(86, 32),
                BackColor = Color.FromArgb(16, 110, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) => SaveAndClose();
            this.Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = "Hủy bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(428, 474),
                Size = new Size(86, 32),
                BackColor = Color.FromArgb(235, 236, 238),
                ForeColor = Color.FromArgb(50, 49, 48),
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.FlatAppearance.BorderColor = Color.LightGray;
            _btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        private void BuildFontTab(TabPage tab)
        {
            // Font Family
            Label lblF = new Label { Text = "Phông chữ:", Location = new Point(16, 10), AutoSize = true };
            tab.Controls.Add(lblF);

            _txtFont = new TextBox { Location = new Point(16, 30), Size = new Size(160, 24) };
            tab.Controls.Add(_txtFont);

            _lstFont = new ListBox { Location = new Point(16, 56), Size = new Size(160, 120) };
            string[] sysFonts = new string[] { "Calibri", "Aptos", "Arial", "Segoe UI", "Times New Roman", "Tahoma", "Verdana", "Georgia", "Courier New", "Trebuchet MS", "Consolas" };
            _lstFont.Items.AddRange(sysFonts);
            _lstFont.SelectedIndexChanged += (s, e) => {
                if (_lstFont.SelectedItem != null) _txtFont.Text = _lstFont.SelectedItem.ToString();
                UpdatePreview();
            };
            tab.Controls.Add(_lstFont);

            // Font Style
            Label lblS = new Label { Text = "Kiểu phông:", Location = new Point(190, 10), AutoSize = true };
            tab.Controls.Add(lblS);

            _txtStyle = new TextBox { Location = new Point(190, 30), Size = new Size(130, 24) };
            tab.Controls.Add(_txtStyle);

            _lstStyle = new ListBox { Location = new Point(190, 56), Size = new Size(130, 120) };
            _lstStyle.Items.AddRange(new object[] { "Thường (Regular)", "Nghiêng (Italic)", "Đậm (Bold)", "Đậm Nghiêng (Bold Italic)" });
            _lstStyle.SelectedIndexChanged += (s, e) => {
                if (_lstStyle.SelectedItem != null) _txtStyle.Text = _lstStyle.SelectedItem.ToString();
                UpdatePreview();
            };
            tab.Controls.Add(_lstStyle);

            // Font Size
            Label lblSz = new Label { Text = "Cỡ chữ:", Location = new Point(334, 10), AutoSize = true };
            tab.Controls.Add(lblSz);

            _txtSize = new TextBox { Location = new Point(334, 30), Size = new Size(60, 24) };
            tab.Controls.Add(_txtSize);

            _lstSize = new ListBox { Location = new Point(334, 56), Size = new Size(60, 120) };
            _lstSize.Items.AddRange(new object[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 });
            _lstSize.SelectedIndexChanged += (s, e) => {
                if (_lstSize.SelectedItem != null) _txtSize.Text = _lstSize.SelectedItem.ToString();
                UpdatePreview();
            };
            tab.Controls.Add(_lstSize);

            // Colors & Underlines
            Label lblCol = new Label { Text = "Màu phông chữ:", Location = new Point(16, 186), AutoSize = true };
            tab.Controls.Add(lblCol);

            _btnColor = new Button { Location = new Point(16, 206), Size = new Size(140, 26), Text = "Màu chữ...", BackColor = Color.Black, ForeColor = Color.White };
            _btnColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = TextColorResult })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        TextColorResult = cd.Color;
                        _btnColor.BackColor = cd.Color;
                        _btnColor.ForeColor = (cd.Color.R * 0.299 + cd.Color.G * 0.587 + cd.Color.B * 0.114 > 150) ? Color.Black : Color.White;
                        UpdatePreview();
                    }
                }
            };
            tab.Controls.Add(_btnColor);

            Label lblUnd = new Label { Text = "Kiểu gạch chân:", Location = new Point(190, 186), AutoSize = true };
            tab.Controls.Add(lblUnd);

            _cboUnderlineStyle = new ComboBox { Location = new Point(190, 206), Size = new Size(130, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboUnderlineStyle.Items.AddRange(new object[] { "(không có)", "Nét đơn (Single)", "Nét đôi (Double)", "Nét đậm (Thick)", "Nét đứt (Dashed)" });
            _cboUnderlineStyle.SelectedIndex = UnderlineResult ? 1 : 0;
            _cboUnderlineStyle.SelectedIndexChanged += (s, e) => UpdatePreview();
            tab.Controls.Add(_cboUnderlineStyle);

            Label lblUndCol = new Label { Text = "Màu gạch chân:", Location = new Point(334, 186), AutoSize = true };
            tab.Controls.Add(lblUndCol);

            _btnUnderlineColor = new Button { Location = new Point(334, 206), Size = new Size(140, 26), Text = "Tự động", BackColor = Color.Black, ForeColor = Color.White };
            _btnUnderlineColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = _selectedUnderlineColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        _selectedUnderlineColor = cd.Color;
                        _btnUnderlineColor.BackColor = cd.Color;
                    }
                }
            };
            tab.Controls.Add(_btnUnderlineColor);

            // Effects
            GroupBox grpEffects = new GroupBox { Text = "Hiệu ứng (Effects)", Location = new Point(16, 240), Size = new Size(468, 86) };

            _chkStrike = new CheckBox { Text = "Gạch ngang", Location = new Point(16, 22), AutoSize = true, Checked = StrikethroughResult };
            _chkStrike.CheckedChanged += (s, e) => UpdatePreview();
            grpEffects.Controls.Add(_chkStrike);

            _chkDoubleStrike = new CheckBox { Text = "Gạch ngang kép", Location = new Point(16, 50), AutoSize = true };
            grpEffects.Controls.Add(_chkDoubleStrike);

            _chkSuperscript = new CheckBox { Text = "Chỉ số trên (x²)", Location = new Point(160, 22), AutoSize = true };
            _chkSuperscript.CheckedChanged += (s, e) => { if (_chkSuperscript.Checked) _chkSubscript.Checked = false; UpdatePreview(); };
            grpEffects.Controls.Add(_chkSuperscript);

            _chkSubscript = new CheckBox { Text = "Chỉ số dưới (x₂)", Location = new Point(160, 50), AutoSize = true };
            _chkSubscript.CheckedChanged += (s, e) => { if (_chkSubscript.Checked) _chkSuperscript.Checked = false; UpdatePreview(); };
            grpEffects.Controls.Add(_chkSubscript);

            _chkSmallCaps = new CheckBox { Text = "Chữ hoa nhỏ", Location = new Point(310, 22), AutoSize = true };
            grpEffects.Controls.Add(_chkSmallCaps);

            _chkAllCaps = new CheckBox { Text = "Tất cả chữ in hoa", Location = new Point(310, 50), AutoSize = true };
            grpEffects.Controls.Add(_chkAllCaps);

            tab.Controls.Add(grpEffects);

            // Preview
            GroupBox grpPrev = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(16, 332), Size = new Size(468, 76) };
            _lblPreview = new Label { Text = "Văn bản mẫu Office 365 AaBbCc", Location = new Point(14, 20), Size = new Size(440, 46), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            grpPrev.Controls.Add(_lblPreview);
            tab.Controls.Add(grpPrev);
        }

        private void BuildAdvancedTab(TabPage tab)
        {
            GroupBox grpSpacing = new GroupBox { Text = "Khoảng cách ký tự (Character Spacing)", Location = new Point(16, 16), Size = new Size(468, 120) };

            Label lblType = new Label { Text = "Giãn cách:", Location = new Point(16, 32), AutoSize = true };
            grpSpacing.Controls.Add(lblType);

            _cboSpacingType = new ComboBox { Location = new Point(120, 28), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboSpacingType.Items.AddRange(new object[] { "Bình thường (Normal)", "Mở rộng (Expanded)", "Thu hẹp (Condensed)" });
            _cboSpacingType.SelectedIndex = 0;
            _cboSpacingType.SelectedIndexChanged += (s, e) => {
                _numSpacingPt.Enabled = (_cboSpacingType.SelectedIndex > 0);
            };
            grpSpacing.Controls.Add(_cboSpacingType);

            Label lblBy = new Label { Text = "Bởi (Pt):", Location = new Point(300, 32), AutoSize = true };
            grpSpacing.Controls.Add(lblBy);

            _numSpacingPt = new NumericUpDown { Location = new Point(360, 30), Size = new Size(60, 24), DecimalPlaces = 1, Increment = 0.5m, Minimum = 0m, Maximum = 20m, Value = 1m, Enabled = false };
            grpSpacing.Controls.Add(_numSpacingPt);

            tab.Controls.Add(grpSpacing);
        }

        private void PopulateCurrentValues()
        {
            _txtFont.Text = FontNameResult;
            int fIdx = _lstFont.FindStringExact(FontNameResult);
            if (fIdx >= 0) _lstFont.SelectedIndex = fIdx;

            _txtSize.Text = FontSizeResult.ToString("0.#");
            int szIdx = _lstSize.Items.IndexOf((int)FontSizeResult);
            if (szIdx >= 0) _lstSize.SelectedIndex = szIdx;

            int stIdx = 0;
            if (BoldResult && ItalicResult) stIdx = 3;
            else if (BoldResult) stIdx = 2;
            else if (ItalicResult) stIdx = 1;
            _lstStyle.SelectedIndex = stIdx;

            _btnColor.BackColor = TextColorResult;
            _btnColor.ForeColor = (TextColorResult.R * 0.299 + TextColorResult.G * 0.587 + TextColorResult.B * 0.114 > 150) ? Color.Black : Color.White;
        }

        private void UpdatePreview()
        {
            if (_lblPreview == null) return;
            string fn = !string.IsNullOrEmpty(_txtFont?.Text) ? _txtFont.Text.Trim() : "Calibri";
            float sz = 13f;
            if (float.TryParse(_txtSize?.Text, out float parsed) && parsed > 4) sz = Math.Min(22f, parsed);

            FontStyle fs = FontStyle.Regular;
            int stIdx = _lstStyle?.SelectedIndex ?? 0;
            if (stIdx == 1) fs = FontStyle.Italic;
            else if (stIdx == 2) fs = FontStyle.Bold;
            else if (stIdx == 3) fs = FontStyle.Bold | FontStyle.Italic;

            if (_cboUnderlineStyle?.SelectedIndex > 0) fs |= FontStyle.Underline;
            if (_chkStrike?.Checked == true) fs |= FontStyle.Strikeout;

            try
            {
                _lblPreview.Font = new Font(fn, sz, fs);
            }
            catch
            {
                _lblPreview.Font = new Font("Segoe UI", sz, fs);
            }
            _lblPreview.ForeColor = TextColorResult;
        }

        private void SaveAndClose()
        {
            FontNameResult = !string.IsNullOrEmpty(_txtFont.Text) ? _txtFont.Text.Trim() : "Calibri";
            if (float.TryParse(_txtSize.Text, out float sz) && sz > 2) FontSizeResult = sz;

            BoldResult = (_lstStyle.SelectedIndex == 2 || _lstStyle.SelectedIndex == 3);
            ItalicResult = (_lstStyle.SelectedIndex == 1 || _lstStyle.SelectedIndex == 3);
            UnderlineResult = (_cboUnderlineStyle.SelectedIndex > 0);
            StrikethroughResult = _chkStrike.Checked;
            SuperscriptResult = _chkSuperscript.Checked;
            SubscriptResult = _chkSubscript.Checked;

            if (_cboSpacingType.SelectedIndex == 1) CharacterSpacingResult = (float)_numSpacingPt.Value;
            else if (_cboSpacingType.SelectedIndex == 2) CharacterSpacingResult = -(float)_numSpacingPt.Value;
            else CharacterSpacingResult = 0f;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

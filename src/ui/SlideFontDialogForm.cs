using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class SlideFontDialogForm : Form
    {
        public string FontNameResult { get; set; }
        public float FontSizeResult { get; set; }
        public bool BoldResult { get; set; }
        public bool ItalicResult { get; set; }
        public bool UnderlineResult { get; set; }
        public bool StrikethroughResult { get; set; }
        public bool ShadowResult { get; set; }
        public int TextColorResult { get; set; }
        public float CharacterSpacingResult { get; set; }

        private ListBox lstFont;
        private ListBox lstStyle;
        private ListBox lstSize;
        private TextBox txtFont;
        private TextBox txtStyle;
        private TextBox txtSize;

        private Button btnColor;
        private Color selectedColor;
        private CheckBox chkStrike;
        private CheckBox chkDoubleStrike;
        private CheckBox chkSuperscript;
        private CheckBox chkSubscript;
        private CheckBox chkSmallCaps;
        private CheckBox chkAllCaps;
        private CheckBox chkShadow;

        private ComboBox cboUnderlineStyle;
        private Button btnUnderlineColor;
        private Color selectedUnderlineColor;

        private ComboBox cboSpacingType;
        private NumericUpDown numSpacingPt;

        private Panel pnlPreview;
        private Label lblPreview;

        public SlideFontDialogForm(ShapeNode shape)
        {
            FontNameResult = shape?.FontName ?? "Segoe UI";
            FontSizeResult = shape?.FontSize > 0 ? shape.FontSize : 18f;
            BoldResult = shape?.Bold ?? false;
            ItalicResult = shape?.Italic ?? false;
            UnderlineResult = shape?.Underline ?? false;
            StrikethroughResult = shape?.Strikethrough ?? false;
            ShadowResult = shape?.HasTextShadow ?? false;
            TextColorResult = (shape != null && shape.TextColor != 0) ? shape.TextColor : 0x000000;
            CharacterSpacingResult = shape?.CharacterSpacing ?? 0f;

            selectedColor = Color.FromArgb((TextColorResult >> 16) & 0xFF, (TextColorResult >> 8) & 0xFF, TextColorResult & 0xFF);
            selectedUnderlineColor = selectedColor;

            InitializeComponent();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            Text = "Phông chữ (Font)";
            Size = new Size(540, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9f);

            TabControl tabCtrl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(500, 450),
                Font = new Font("Segoe UI", 9f)
            };

            // ==================== Tab 1: Phông chữ ====================
            TabPage tabFont = new TabPage("Phông chữ");
            tabFont.BackColor = Color.White;

            Label lblF = new Label { Text = "Phông chữ:", Location = new Point(14, 12), AutoSize = true };
            txtFont = new TextBox { Location = new Point(14, 32), Size = new Size(160, 23), Text = FontNameResult };
            lstFont = new ListBox { Location = new Point(14, 56), Size = new Size(160, 110) };

            InstalledFontCollection fonts = new InstalledFontCollection();
            foreach (var fam in fonts.Families) lstFont.Items.Add(fam.Name);
            if (lstFont.Items.Contains(FontNameResult)) lstFont.SelectedItem = FontNameResult;
            else if (lstFont.Items.Count > 0) lstFont.SelectedIndex = 0;

            lstFont.SelectedIndexChanged += (s, e) => {
                if (lstFont.SelectedItem != null) {
                    txtFont.Text = lstFont.SelectedItem.ToString();
                    FontNameResult = txtFont.Text;
                    UpdatePreview();
                }
            };
            txtFont.TextChanged += (s, e) => {
                int idx = lstFont.FindString(txtFont.Text);
                if (idx >= 0) lstFont.SelectedIndex = idx;
                FontNameResult = txtFont.Text;
                UpdatePreview();
            };

            Label lblS = new Label { Text = "Kiểu phông:", Location = new Point(184, 12), AutoSize = true };
            txtStyle = new TextBox { Location = new Point(184, 32), Size = new Size(130, 23) };
            lstStyle = new ListBox { Location = new Point(184, 56), Size = new Size(130, 110) };
            lstStyle.Items.AddRange(new object[] { "Thường (Regular)", "Nghiêng (Italic)", "Đậm (Bold)", "Đậm Nghiêng (Bold Italic)" });
            int styleIdx = 0;
            if (BoldResult && ItalicResult) styleIdx = 3;
            else if (BoldResult) styleIdx = 2;
            else if (ItalicResult) styleIdx = 1;
            lstStyle.SelectedIndex = styleIdx;
            txtStyle.Text = lstStyle.SelectedItem.ToString();

            lstStyle.SelectedIndexChanged += (s, e) => {
                if (lstStyle.SelectedItem != null) {
                    txtStyle.Text = lstStyle.SelectedItem.ToString();
                    BoldResult = (lstStyle.SelectedIndex == 2 || lstStyle.SelectedIndex == 3);
                    ItalicResult = (lstStyle.SelectedIndex == 1 || lstStyle.SelectedIndex == 3);
                    UpdatePreview();
                }
            };

            Label lblSz = new Label { Text = "Cỡ chữ:", Location = new Point(324, 12), AutoSize = true };
            txtSize = new TextBox { Location = new Point(324, 32), Size = new Size(65, 23), Text = ((int)FontSizeResult).ToString() };
            lstSize = new ListBox { Location = new Point(324, 56), Size = new Size(65, 110) };
            int[] sizes = new int[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 40, 44, 48, 54, 60, 66, 72, 80, 96 };
            foreach (int sz in sizes) lstSize.Items.Add(sz.ToString());
            int sIdx = lstSize.FindString(((int)FontSizeResult).ToString());
            if (sIdx >= 0) lstSize.SelectedIndex = sIdx;

            lstSize.SelectedIndexChanged += (s, e) => {
                if (lstSize.SelectedItem != null) {
                    txtSize.Text = lstSize.SelectedItem.ToString();
                    float parsed;
                    if (float.TryParse(txtSize.Text, out parsed)) FontSizeResult = parsed;
                    UpdatePreview();
                }
            };
            txtSize.TextChanged += (s, e) => {
                float parsed;
                if (float.TryParse(txtSize.Text, out parsed)) {
                    FontSizeResult = parsed;
                    UpdatePreview();
                }
            };

            tabFont.Controls.AddRange(new Control[] { lblF, txtFont, lstFont, lblS, txtStyle, lstStyle, lblSz, txtSize, lstSize });

            // Color & Underline
            Label lblClr = new Label { Text = "Màu phông chữ:", Location = new Point(14, 178), AutoSize = true };
            btnColor = new Button { Location = new Point(14, 198), Size = new Size(160, 26), BackColor = selectedColor, FlatStyle = FlatStyle.Flat };
            btnColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = selectedColor }) {
                    if (cd.ShowDialog() == DialogResult.OK) {
                        selectedColor = cd.Color;
                        btnColor.BackColor = selectedColor;
                        TextColorResult = (selectedColor.R << 16) | (selectedColor.G << 8) | selectedColor.B;
                        UpdatePreview();
                    }
                }
            };

            Label lblUnd = new Label { Text = "Kiểu gạch chân:", Location = new Point(184, 178), AutoSize = true };
            cboUnderlineStyle = new ComboBox { Location = new Point(184, 198), Size = new Size(130, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboUnderlineStyle.Items.AddRange(new object[] { "(không có)", "Nét đơn", "Nét đôi", "Nét dày", "Nét chấm" });
            cboUnderlineStyle.SelectedIndex = UnderlineResult ? 1 : 0;
            cboUnderlineStyle.SelectedIndexChanged += (s, e) => {
                UnderlineResult = (cboUnderlineStyle.SelectedIndex > 0);
                UpdatePreview();
            };

            Label lblUndClr = new Label { Text = "Màu gạch chân:", Location = new Point(324, 178), AutoSize = true };
            btnUnderlineColor = new Button { Location = new Point(324, 198), Size = new Size(65, 26), BackColor = selectedUnderlineColor, FlatStyle = FlatStyle.Flat };
            btnUnderlineColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = selectedUnderlineColor }) {
                    if (cd.ShowDialog() == DialogResult.OK) {
                        selectedUnderlineColor = cd.Color;
                        btnUnderlineColor.BackColor = selectedUnderlineColor;
                        UpdatePreview();
                    }
                }
            };

            tabFont.Controls.AddRange(new Control[] { lblClr, btnColor, lblUnd, cboUnderlineStyle, lblUndClr, btnUnderlineColor });

            // Effects Checkboxes Group
            GroupBox grpEffects = new GroupBox { Text = "Hiệu ứng (Effects)", Location = new Point(14, 235), Size = new Size(465, 80) };
            chkStrike = new CheckBox { Text = "Gạch ngang", Location = new Point(16, 22), AutoSize = true, Checked = StrikethroughResult };
            chkDoubleStrike = new CheckBox { Text = "Gạch ngang đôi", Location = new Point(16, 48), AutoSize = true };
            chkSuperscript = new CheckBox { Text = "Chỉ số trên", Location = new Point(155, 22), AutoSize = true };
            chkSubscript = new CheckBox { Text = "Chỉ số dưới", Location = new Point(155, 48), AutoSize = true };
            chkSmallCaps = new CheckBox { Text = "Chữ hoa nhỏ", Location = new Point(285, 22), AutoSize = true };
            chkAllCaps = new CheckBox { Text = "Tất cả chữ hoa", Location = new Point(285, 48), AutoSize = true };
            chkShadow = new CheckBox { Text = "Đổ bóng chữ", Location = new Point(385, 22), AutoSize = true, Checked = ShadowResult };

            EventHandler effChange = (s, e) => {
                StrikethroughResult = chkStrike.Checked || chkDoubleStrike.Checked;
                ShadowResult = chkShadow.Checked;
                UpdatePreview();
            };
            chkStrike.CheckedChanged += effChange;
            chkDoubleStrike.CheckedChanged += effChange;
            chkSuperscript.CheckedChanged += effChange;
            chkSubscript.CheckedChanged += effChange;
            chkSmallCaps.CheckedChanged += effChange;
            chkAllCaps.CheckedChanged += effChange;
            chkShadow.CheckedChanged += effChange;

            grpEffects.Controls.AddRange(new Control[] { chkStrike, chkDoubleStrike, chkSuperscript, chkSubscript, chkSmallCaps, chkAllCaps, chkShadow });
            tabFont.Controls.Add(grpEffects);

            // ==================== Tab 2: Khoảng cách ký tự ====================
            TabPage tabSpacing = new TabPage("Khoảng cách ký tự (Character Spacing)");
            tabSpacing.BackColor = Color.White;

            Label lblSpcT = new Label { Text = "Khoảng cách:", Location = new Point(20, 24), AutoSize = true };
            cboSpacingType = new ComboBox { Location = new Point(130, 21), Size = new Size(160, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboSpacingType.Items.AddRange(new object[] { "Bình thường (Normal)", "Mở rộng (Expanded)", "Thu hẹp (Condensed)" });
            cboSpacingType.SelectedIndex = CharacterSpacingResult > 0 ? 1 : (CharacterSpacingResult < 0 ? 2 : 0);

            Label lblBy = new Label { Text = "Bằng (pt):", Location = new Point(20, 60), AutoSize = true };
            numSpacingPt = new NumericUpDown { Location = new Point(130, 58), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 50, Value = (decimal)Math.Abs(CharacterSpacingResult) };

            EventHandler spcChange = (s, e) => {
                if (cboSpacingType.SelectedIndex == 1) CharacterSpacingResult = (float)numSpacingPt.Value;
                else if (cboSpacingType.SelectedIndex == 2) CharacterSpacingResult = -(float)numSpacingPt.Value;
                else CharacterSpacingResult = 0f;
                UpdatePreview();
            };
            cboSpacingType.SelectedIndexChanged += spcChange;
            numSpacingPt.ValueChanged += spcChange;

            tabSpacing.Controls.AddRange(new Control[] { lblSpcT, cboSpacingType, lblBy, numSpacingPt });

            tabCtrl.TabPages.Add(tabFont);
            tabCtrl.TabPages.Add(tabSpacing);
            Controls.Add(tabCtrl);

            // Preview Box at Bottom
            GroupBox grpPreview = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(12, 335 + 12), Size = new Size(500, 75) };
            pnlPreview = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            lblPreview = new Label {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "AaBbYyZz - Tiếng Việt Có Dấu"
            };
            pnlPreview.Controls.Add(lblPreview);
            grpPreview.Controls.Add(pnlPreview);
            tabFont.Controls.Add(grpPreview);

            // Action Buttons
            Button btnVietnameseToolkit = new Button { 
                Text = "🇻🇳 Sửa Lỗi Font & Trích Xuất...", 
                Location = new Point(12, 475), 
                Size = new Size(210, 30), 
                BackColor = Color.FromArgb(243, 244, 246), 
                ForeColor = Color.FromArgb(31, 41, 55), 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular)
            };
            btnVietnameseToolkit.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnVietnameseToolkit.Click += (s, e) => {
                using (var toolkit = new VietnameseFontToolkitForm())
                {
                    toolkit.ShowDialog(this);
                }
            };

            Button btnOk = new Button { Text = "Đồng ý", DialogResult = DialogResult.OK, Location = new Point(340, 475), Size = new Size(80, 30), BackColor = Color.FromArgb(210, 71, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(430, 475), Size = new Size(80, 30), BackColor = Color.FromArgb(229, 231, 235), FlatStyle = FlatStyle.Flat };

            btnOk.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { btnVietnameseToolkit, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void UpdatePreview()
        {
            if (lblPreview == null) return;
            try
            {
                FontStyle style = FontStyle.Regular;
                if (BoldResult) style |= FontStyle.Bold;
                if (ItalicResult) style |= FontStyle.Italic;
                if (UnderlineResult) style |= FontStyle.Underline;
                if (StrikethroughResult) style |= FontStyle.Strikeout;

                float previewSz = Math.Min(24f, Math.Max(10f, FontSizeResult));
                lblPreview.Font = new Font(FontNameResult, previewSz, style);
                lblPreview.ForeColor = selectedColor;

                string sample = "AaBbYyZz - Tiếng Việt";
                if (chkSmallCaps != null && chkSmallCaps.Checked) sample = sample.ToUpper();
                if (chkAllCaps != null && chkAllCaps.Checked) sample = sample.ToUpper();
                lblPreview.Text = sample;
            }
            catch
            {
                lblPreview.Font = new Font("Segoe UI", 14f);
            }
        }

        public void ApplyToShape(ShapeNode shape)
        {
            if (shape == null) return;
            shape.FontName = FontNameResult;
            shape.FontSize = FontSizeResult;
            shape.Bold = BoldResult;
            shape.Italic = ItalicResult;
            shape.Underline = UnderlineResult;
            shape.Strikethrough = StrikethroughResult;
            shape.HasTextShadow = ShadowResult;
            shape.TextColor = TextColorResult;
            shape.CharacterSpacing = CharacterSpacingResult;

            if (shape.TextRuns != null && shape.TextRuns.Count > 0)
            {
                foreach (var run in shape.TextRuns)
                {
                    run.FontName = FontNameResult;
                    run.FontSize = FontSizeResult;
                    run.Bold = BoldResult;
                    run.Italic = ItalicResult;
                    run.Underline = UnderlineResult;
                    run.TextColor = TextColorResult;
                }
            }
        }
    }
}

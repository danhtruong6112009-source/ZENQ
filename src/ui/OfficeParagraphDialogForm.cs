using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Hộp thoại Đoạn văn (Paragraph Dialog) chuẩn Microsoft Office 365 (Word/Writer).
    /// </summary>
    public class OfficeParagraphDialogForm : Form
    {
        public int AlignmentResult { get; set; } = 0; // 0: Left, 1: Center, 2: Right, 3: Justify
        public float LineSpacingResult { get; set; } = 1.15f;
        public float SpaceBeforeResult { get; set; } = 0f;
        public float SpaceAfterResult { get; set; } = 8f;
        public float LeftIndentResult { get; set; } = 0f;
        public float RightIndentResult { get; set; } = 0f;
        public float FirstLineIndentResult { get; set; } = 0f;
        public float HangingIndentResult { get; set; } = 0f;

        private ComboBox _cboAlignment;
        private NumericUpDown _numLeftIndent;
        private NumericUpDown _numRightIndent;
        private ComboBox _cboSpecialIndent;
        private NumericUpDown _numSpecialBy;

        private NumericUpDown _numSpaceBefore;
        private NumericUpDown _numSpaceAfter;
        private ComboBox _cboLineSpacing;
        private NumericUpDown _numLineSpacingAt;

        private Panel _pnlPreview;
        private Label _lblPreview1;
        private Label _lblPreview2;

        private Button _btnOk;
        private Button _btnCancel;

        public OfficeParagraphDialogForm(int align = 0, float lineSpacing = 1.15f, float spaceBefore = 0f, float spaceAfter = 8f, float leftIndent = 0f, float rightIndent = 0f, float firstLineIndent = 0f, float hangingIndent = 0f)
        {
            AlignmentResult = Math.Max(0, Math.Min(3, align));
            LineSpacingResult = lineSpacing > 0 ? lineSpacing : 1.15f;
            SpaceBeforeResult = Math.Max(0, spaceBefore);
            SpaceAfterResult = Math.Max(0, spaceAfter);
            LeftIndentResult = leftIndent;
            RightIndentResult = rightIndent;
            FirstLineIndentResult = firstLineIndent;
            HangingIndentResult = hangingIndent;

            InitializeComponent();
            PopulateInitialValues();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            this.Text = "Đoạn văn (Paragraph)";
            this.Size = new Size(540, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Tab Control
            TabControl tabCtrl = new TabControl { Location = new Point(12, 12), Size = new Size(502, 420) };
            TabPage tabMain = new TabPage("Thụt lề & Giãn cách (Indents and Spacing)");
            tabMain.BackColor = Color.White;

            // 1. General (Chung)
            GroupBox grpGen = new GroupBox { Text = "Chung (General)", Location = new Point(16, 12), Size = new Size(468, 64) };

            Label lblAlign = new Label { Text = "Căn lề (Alignment):", Location = new Point(16, 26), AutoSize = true };
            grpGen.Controls.Add(lblAlign);

            _cboAlignment = new ComboBox { Location = new Point(160, 24), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboAlignment.Items.AddRange(new object[] { "Trái (Left)", "Giữa (Center)", "Phải (Right)", "Căn đều hai bên (Justified)" });
            _cboAlignment.SelectedIndex = AlignmentResult;
            _cboAlignment.SelectedIndexChanged += (s, e) => UpdatePreview();
            grpGen.Controls.Add(_cboAlignment);

            tabMain.Controls.Add(grpGen);

            // 2. Indentation (Thụt lề)
            GroupBox grpIndent = new GroupBox { Text = "Thụt lề (Indentation)", Location = new Point(16, 82), Size = new Size(468, 100) };

            Label lblLeft = new Label { Text = "Bên trái (Left):", Location = new Point(16, 28), AutoSize = true };
            grpIndent.Controls.Add(lblLeft);

            _numLeftIndent = new NumericUpDown { Location = new Point(100, 26), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = -5m, Maximum = 10m, Value = 0m };
            _numLeftIndent.ValueChanged += (s, e) => UpdatePreview();
            grpIndent.Controls.Add(_numLeftIndent);

            Label lblRight = new Label { Text = "Bên phải (Right):", Location = new Point(16, 62), AutoSize = true };
            grpIndent.Controls.Add(lblRight);

            _numRightIndent = new NumericUpDown { Location = new Point(100, 60), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = -5m, Maximum = 10m, Value = 0m };
            _numRightIndent.ValueChanged += (s, e) => UpdatePreview();
            grpIndent.Controls.Add(_numRightIndent);

            Label lblSpecial = new Label { Text = "Đặc biệt (Special):", Location = new Point(210, 28), AutoSize = true };
            grpIndent.Controls.Add(lblSpecial);

            _cboSpecialIndent = new ComboBox { Location = new Point(320, 24), Size = new Size(130, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboSpecialIndent.Items.AddRange(new object[] { "(Không có)", "Dòng đầu (First line)", "Nhô dòng (Hanging)" });
            _cboSpecialIndent.SelectedIndex = 0;
            _cboSpecialIndent.SelectedIndexChanged += (s, e) => {
                _numSpecialBy.Enabled = (_cboSpecialIndent.SelectedIndex > 0);
                UpdatePreview();
            };
            grpIndent.Controls.Add(_cboSpecialIndent);

            Label lblBy = new Label { Text = "Khoảng cách (By):", Location = new Point(210, 62), AutoSize = true };
            grpIndent.Controls.Add(lblBy);

            _numSpecialBy = new NumericUpDown { Location = new Point(320, 60), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0m, Maximum = 5m, Value = 0.5m, Enabled = false };
            _numSpecialBy.ValueChanged += (s, e) => UpdatePreview();
            grpIndent.Controls.Add(_numSpecialBy);

            tabMain.Controls.Add(grpIndent);

            // 3. Spacing (Giãn cách)
            GroupBox grpSpacing = new GroupBox { Text = "Giãn cách (Spacing)", Location = new Point(16, 188), Size = new Size(468, 100) };

            Label lblBefore = new Label { Text = "Trước (Before pt):", Location = new Point(16, 28), AutoSize = true };
            grpSpacing.Controls.Add(lblBefore);

            _numSpaceBefore = new NumericUpDown { Location = new Point(116, 26), Size = new Size(60, 24), DecimalPlaces = 0, Increment = 6m, Minimum = 0m, Maximum = 120m, Value = (decimal)SpaceBeforeResult };
            _numSpaceBefore.ValueChanged += (s, e) => UpdatePreview();
            grpSpacing.Controls.Add(_numSpaceBefore);

            Label lblAfter = new Label { Text = "Sau (After pt):", Location = new Point(16, 62), AutoSize = true };
            grpSpacing.Controls.Add(lblAfter);

            _numSpaceAfter = new NumericUpDown { Location = new Point(116, 60), Size = new Size(60, 24), DecimalPlaces = 0, Increment = 6m, Minimum = 0m, Maximum = 120m, Value = (decimal)SpaceAfterResult };
            _numSpaceAfter.ValueChanged += (s, e) => UpdatePreview();
            grpSpacing.Controls.Add(_numSpaceAfter);

            Label lblLineSp = new Label { Text = "Cách dòng:", Location = new Point(210, 28), AutoSize = true };
            grpSpacing.Controls.Add(lblLineSp);

            _cboLineSpacing = new ComboBox { Location = new Point(320, 24), Size = new Size(130, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboLineSpacing.Items.AddRange(new object[] { "Đơn (Single 1.0)", "1.15 dòng", "1.5 dòng", "Đôi (Double 2.0)", "Đa dòng (Multiple)" });
            _cboLineSpacing.SelectedIndex = 1; // 1.15 default
            _cboLineSpacing.SelectedIndexChanged += (s, e) => {
                _numLineSpacingAt.Enabled = (_cboLineSpacing.SelectedIndex == 4);
                UpdatePreview();
            };
            grpSpacing.Controls.Add(_cboLineSpacing);

            Label lblAt = new Label { Text = "Tại (At):", Location = new Point(210, 62), AutoSize = true };
            grpSpacing.Controls.Add(lblAt);

            _numLineSpacingAt = new NumericUpDown { Location = new Point(320, 60), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.5m, Maximum = 5m, Value = 1.15m, Enabled = false };
            _numLineSpacingAt.ValueChanged += (s, e) => UpdatePreview();
            grpSpacing.Controls.Add(_numLineSpacingAt);

            tabMain.Controls.Add(grpSpacing);

            // 4. Preview Box
            GroupBox grpPrev = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(16, 294), Size = new Size(468, 86) };
            _pnlPreview = new Panel { Location = new Point(12, 20), Size = new Size(444, 56), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            _lblPreview1 = new Label { Text = "Đoạn văn bản mẫu hiển thị căn lề và khoảng cách dòng thực tế.", Location = new Point(6, 4), Size = new Size(430, 22), BackColor = Color.Transparent };
            _lblPreview2 = new Label { Text = "Kiểm tra sự thay đổi trực quan của văn bản trước khi áp dụng vào tài liệu Word.", Location = new Point(6, 28), Size = new Size(430, 22), BackColor = Color.Transparent };
            _pnlPreview.Controls.Add(_lblPreview1);
            _pnlPreview.Controls.Add(_lblPreview2);
            grpPrev.Controls.Add(_pnlPreview);
            tabMain.Controls.Add(grpPrev);

            tabCtrl.TabPages.Add(tabMain);
            this.Controls.Add(tabCtrl);

            // Bottom Buttons
            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(334, 440),
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
                Location = new Point(428, 440),
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

        private void PopulateInitialValues()
        {
            _cboAlignment.SelectedIndex = Math.Max(0, Math.Min(3, AlignmentResult));

            if (FirstLineIndentResult > 0)
            {
                _cboSpecialIndent.SelectedIndex = 1;
                _numSpecialBy.Value = (decimal)FirstLineIndentResult;
            }
            else if (HangingIndentResult > 0)
            {
                _cboSpecialIndent.SelectedIndex = 2;
                _numSpecialBy.Value = (decimal)HangingIndentResult;
            }
            else
            {
                _cboSpecialIndent.SelectedIndex = 0;
            }

            if (Math.Abs(LineSpacingResult - 1.0f) < 0.05f) _cboLineSpacing.SelectedIndex = 0;
            else if (Math.Abs(LineSpacingResult - 1.15f) < 0.05f) _cboLineSpacing.SelectedIndex = 1;
            else if (Math.Abs(LineSpacingResult - 1.5f) < 0.05f) _cboLineSpacing.SelectedIndex = 2;
            else if (Math.Abs(LineSpacingResult - 2.0f) < 0.05f) _cboLineSpacing.SelectedIndex = 3;
            else
            {
                _cboLineSpacing.SelectedIndex = 4;
                _numLineSpacingAt.Value = (decimal)LineSpacingResult;
            }
        }

        private void UpdatePreview()
        {
            if (_lblPreview1 == null) return;
            ContentAlignment ca = ContentAlignment.MiddleLeft;
            int align = _cboAlignment?.SelectedIndex ?? 0;
            if (align == 1) ca = ContentAlignment.MiddleCenter;
            else if (align == 2) ca = ContentAlignment.MiddleRight;
            else if (align == 3) ca = ContentAlignment.MiddleLeft;

            _lblPreview1.TextAlign = ca;
            _lblPreview2.TextAlign = ca;
        }

        private void SaveAndClose()
        {
            AlignmentResult = _cboAlignment.SelectedIndex;
            LeftIndentResult = (float)_numLeftIndent.Value;
            RightIndentResult = (float)_numRightIndent.Value;

            if (_cboSpecialIndent.SelectedIndex == 1)
            {
                FirstLineIndentResult = (float)_numSpecialBy.Value;
                HangingIndentResult = 0f;
            }
            else if (_cboSpecialIndent.SelectedIndex == 2)
            {
                FirstLineIndentResult = 0f;
                HangingIndentResult = (float)_numSpecialBy.Value;
            }
            else
            {
                FirstLineIndentResult = 0f;
                HangingIndentResult = 0f;
            }

            SpaceBeforeResult = (float)_numSpaceBefore.Value;
            SpaceAfterResult = (float)_numSpaceAfter.Value;

            LineSpacingResult = _cboLineSpacing.SelectedIndex switch
            {
                0 => 1.0f,
                1 => 1.15f,
                2 => 1.5f,
                3 => 2.0f,
                4 => (float)_numLineSpacingAt.Value,
                _ => 1.15f
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

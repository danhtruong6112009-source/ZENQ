using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class SlideDimensionsDialog : Form
    {
        private const float PX_PER_CM = 56.6929f; // Standard 144 DPI metric conversion (1920 px = ~33.867 cm, 1080 px = ~19.05 cm)

        private ComboBox _cboPresets;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private RadioButton _rbLandscape;
        private RadioButton _rbPortrait;
        private RadioButton _rbMaximize;
        private RadioButton _rbEnsureFit;
        private Button _btnOk;
        private Button _btnCancel;

        public float TargetCanvasWidth { get; private set; } = 1920f;
        public float TargetCanvasHeight { get; private set; } = 1080f;
        public bool IsMaximizeScaling { get; private set; } = true;

        public SlideDimensionsDialog(float currentWidth, float currentHeight)
        {
            TargetCanvasWidth = currentWidth > 50f ? currentWidth : 1920f;
            TargetCanvasHeight = currentHeight > 50f ? currentHeight : 1080f;

            InitializeComponent();
            LoadCurrentValues();
        }

        private void InitializeComponent()
        {
            Text = "Kích cỡ Trang chiếu (Slide Size)";
            Width = 460;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            int curY = 16;

            Label lblPreset = new Label
            {
                Text = "Kích cỡ trang chiếu cho (Slides sized for):",
                Location = new Point(20, curY),
                Size = new Size(380, 20),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            Controls.Add(lblPreset);
            curY += 24;

            _cboPresets = new ComboBox
            {
                Location = new Point(20, curY),
                Size = new Size(395, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboPresets.Items.Add("Màn hình rộng (16:9)");
            _cboPresets.Items.Add("Tiêu chuẩn (4:3)");
            _cboPresets.Items.Add("Giấy A4 (210 x 297 mm)");
            _cboPresets.Items.Add("Giấy A3 (297 x 420 mm)");
            _cboPresets.Items.Add("Giấy Thư (Letter 8.5 x 11 in)");
            _cboPresets.Items.Add("Biểu ngữ (Banner)");
            _cboPresets.Items.Add("Tùy chỉnh (Custom)");
            _cboPresets.SelectedIndexChanged += OnPresetChanged;
            Controls.Add(_cboPresets);
            curY += 38;

            Label lblWidth = new Label
            {
                Text = "Chiều rộng (Width - cm):",
                Location = new Point(20, curY),
                Size = new Size(180, 20),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(lblWidth);

            Label lblHeight = new Label
            {
                Text = "Chiều cao (Height - cm):",
                Location = new Point(225, curY),
                Size = new Size(180, 20),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(lblHeight);
            curY += 22;

            _numWidth = new NumericUpDown
            {
                Location = new Point(20, curY),
                Size = new Size(180, 26),
                DecimalPlaces = 2,
                Minimum = 2m,
                Maximum = 150m,
                Increment = 0.5m
            };
            _numWidth.ValueChanged += (s, e) => { if (_cboPresets.SelectedIndex != 6) _cboPresets.SelectedIndex = 6; };
            Controls.Add(_numWidth);

            _numHeight = new NumericUpDown
            {
                Location = new Point(225, curY),
                Size = new Size(190, 26),
                DecimalPlaces = 2,
                Minimum = 2m,
                Maximum = 150m,
                Increment = 0.5m
            };
            _numHeight.ValueChanged += (s, e) => { if (_cboPresets.SelectedIndex != 6) _cboPresets.SelectedIndex = 6; };
            Controls.Add(_numHeight);
            curY += 40;

            GroupBox gbOrient = new GroupBox
            {
                Text = "Hướng trang chiếu (Orientation)",
                Location = new Point(20, curY),
                Size = new Size(395, 75),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _rbLandscape = new RadioButton
            {
                Text = "Ngang (Landscape)",
                Location = new Point(25, 28),
                Size = new Size(160, 24),
                Checked = true
            };
            _rbLandscape.CheckedChanged += OnOrientationChanged;
            gbOrient.Controls.Add(_rbLandscape);

            _rbPortrait = new RadioButton
            {
                Text = "Dọc (Portrait)",
                Location = new Point(210, 28),
                Size = new Size(160, 24)
            };
            _rbPortrait.CheckedChanged += OnOrientationChanged;
            gbOrient.Controls.Add(_rbPortrait);
            Controls.Add(gbOrient);
            curY += 86;

            GroupBox gbFit = new GroupBox
            {
                Text = "Tỷ lệ tỷ xích đối tượng (Scaling)",
                Location = new Point(20, curY),
                Size = new Size(395, 68),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _rbMaximize = new RadioButton
            {
                Text = "Tối đa hóa (Maximize)",
                Location = new Point(25, 26),
                Size = new Size(170, 24),
                Checked = true
            };
            gbFit.Controls.Add(_rbMaximize);

            _rbEnsureFit = new RadioButton
            {
                Text = "Đảm bảo Vừa khít (Ensure Fit)",
                Location = new Point(210, 26),
                Size = new Size(175, 24)
            };
            gbFit.Controls.Add(_rbEnsureFit);
            Controls.Add(gbFit);
            curY += 80;

            _btnOk = new Button
            {
                Text = "Đồng ý",
                Location = new Point(225, curY),
                Size = new Size(95, 32),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(210, 71, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += OnOkClick;
            Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(330, curY),
                Size = new Size(85, 32),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void LoadCurrentValues()
        {
            float wCm = TargetCanvasWidth / PX_PER_CM;
            float hCm = TargetCanvasHeight / PX_PER_CM;

            bool isLandscape = wCm >= hCm;
            _rbLandscape.Checked = isLandscape;
            _rbPortrait.Checked = !isLandscape;

            _numWidth.Value = Math.Max(2m, Math.Min(150m, (decimal)wCm));
            _numHeight.Value = Math.Max(2m, Math.Min(150m, (decimal)hCm));

            float ratio = TargetCanvasWidth / TargetCanvasHeight;
            if (Math.Abs(ratio - (16.0f / 9.0f)) < 0.05f)
            {
                _cboPresets.SelectedIndex = 0; // 16:9
            }
            else if (Math.Abs(ratio - (4.0f / 3.0f)) < 0.05f)
            {
                _cboPresets.SelectedIndex = 1; // 4:3
            }
            else
            {
                _cboPresets.SelectedIndex = 6; // Custom
            }
        }

        private void OnPresetChanged(object sender, EventArgs e)
        {
            bool isLandscape = _rbLandscape.Checked;
            switch (_cboPresets.SelectedIndex)
            {
                case 0: // 16:9
                    SetDims(33.87f, 19.05f, isLandscape);
                    break;
                case 1: // 4:3
                    SetDims(25.4f, 19.05f, isLandscape);
                    break;
                case 2: // A4
                    SetDims(27.52f, 19.05f, isLandscape);
                    break;
                case 3: // A3
                    SetDims(38.92f, 27.52f, isLandscape);
                    break;
                case 4: // Letter
                    SetDims(27.94f, 21.59f, isLandscape);
                    break;
                case 5: // Banner
                    SetDims(50.8f, 20.32f, isLandscape);
                    break;
            }
        }

        private void SetDims(float w, float h, bool isLandscape)
        {
            float actualW = isLandscape ? Math.Max(w, h) : Math.Min(w, h);
            float actualH = isLandscape ? Math.Min(w, h) : Math.Max(w, h);
            _numWidth.Value = (decimal)actualW;
            _numHeight.Value = (decimal)actualH;
        }

        private void OnOrientationChanged(object sender, EventArgs e)
        {
            decimal w = _numWidth.Value;
            decimal h = _numHeight.Value;
            if (_rbLandscape.Checked && w < h)
            {
                _numWidth.Value = h;
                _numHeight.Value = w;
            }
            else if (_rbPortrait.Checked && w > h)
            {
                _numWidth.Value = h;
                _numHeight.Value = w;
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            float wCm = (float)_numWidth.Value;
            float hCm = (float)_numHeight.Value;

            TargetCanvasWidth = wCm * PX_PER_CM;
            TargetCanvasHeight = hCm * PX_PER_CM;
            IsMaximizeScaling = _rbMaximize.Checked;
        }
    }
}

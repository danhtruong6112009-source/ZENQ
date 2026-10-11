using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Compact, clean Light-Themed Dialog for configuring Neon Pulse animation parameters.
    /// Uses TabControl to minimize space, integrates Office-style ColorPaletteForm, and updates canvas in real-time.
    /// </summary>
    public class NeonEffectOptionsDialogForm : Form
    {
        private ShapeAnimation _anim;
        private ShapeNode _shape;
        private Action _onChanged;

        // Tab 1 Controls
        private RadioButton rbTargetBoth;
        private RadioButton rbTargetShape;
        private RadioButton rbTargetText;

        private RadioButton rbModeMono;
        private RadioButton rbModeRainbow;

        private Panel pnlColorRow;
        private Button btnColorBox;
        private TextBox txtHexColor;
        private Button btnAutoColor;

        // Tab 2 Controls
        private TrackBar tbBloomRadius;
        private Label lblBloomRadiusVal;
        private TrackBar tbIntensity;
        private Label lblIntensityVal;
        private CheckBox chkCornerFlares;
        private CheckBox chkInfiniteLoop;
        private ComboBox cboRepeatCount;
        private Label lblRepeatCount;

        public NeonEffectOptionsDialogForm(ShapeAnimation anim, ShapeNode shape, Action onChanged = null)
        {
            _anim = anim ?? new ShapeAnimation { Type = AnimationType.GenOffice_NeonPulse };
            _shape = shape;
            _onChanged = onChanged;

            // Ensure defaults
            if (_anim.NeonBloomRadius <= 0) _anim.NeonBloomRadius = 26f;
            if (_anim.NeonIntensity <= 0) _anim.NeonIntensity = 1.0f;
            if (_anim.NeonColor == 0 && shape != null)
            {
                if (shape.StrokeColor != 0 && shape.StrokeColor != 0x00FFFFFF)
                    _anim.NeonColor = shape.StrokeColor;
                else if (shape.TextColor != 0 && shape.TextColor != 0x00FFFFFF)
                    _anim.NeonColor = shape.TextColor;
            }
            if (_anim.NeonColor == 0) _anim.NeonColor = Color.FromArgb(0, 240, 255).ToArgb();

            InitializeComponent();
            LoadAnimationValues();
        }

        private void InitializeComponent()
        {
            Text = "Tùy chỉnh phát sáng Neon";
            Size = new Size(420, 365);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(248, 249, 251);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // 1. Header Bar (Compact)
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.White
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240)))
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            Label lblHeader = new Label
            {
                Text = "Tùy chỉnh phát sáng Neon",
                Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 8),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeader);

            // 2. Bottom Action Bar
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240)))
                    e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
            };

            Button btnApply = new Button
            {
                Text = "Áp dụng",
                Size = new Size(88, 28),
                Location = new Point(pnlBottom.Width - 100, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) =>
            {
                SyncToAnimation();
                if (_onChanged != null) _onChanged();
                DialogResult = DialogResult.OK;
                Close();
            };

            Button btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(75, 28),
                Location = new Point(pnlBottom.Width - 185, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.0f)
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClose.Click += (s, e) => { Close(); };

            pnlBottom.Controls.Add(btnApply);
            pnlBottom.Controls.Add(btnClose);

            // 3. TabControl for Clean & Compact Layout
            TabControl tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.0f),
                Padding = new Point(14, 5)
            };

            TabPage tabColor = new TabPage("Mục tiêu & Màu sắc")
            {
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };

            TabPage tabEffect = new TabPage("Hào quang & Lặp lại")
            {
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };

            tabMain.TabPages.Add(tabColor);
            tabMain.TabPages.Add(tabEffect);

            // ==========================================
            // BUILD TAB 1: Mục tiêu & Màu sắc
            // ==========================================
            BuildTab1Content(tabColor);

            // ==========================================
            // BUILD TAB 2: Hào quang & Lặp lại
            // ==========================================
            BuildTab2Content(tabEffect);

            Controls.Add(tabMain);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
        }

        private void BuildTab1Content(TabPage page)
        {
            int curY = 10;

            // 1. Mục tiêu áp dụng
            Label lblTarget = new Label
            {
                Text = "Mục tiêu áp dụng:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, curY),
                AutoSize = true
            };
            page.Controls.Add(lblTarget);
            curY += 22;

            rbTargetBoth = new RadioButton
            {
                Text = "Cả khung viền và chữ",
                Location = new Point(20, curY),
                Size = new Size(180, 22),
                Checked = true
            };
            rbTargetBoth.CheckedChanged += (s, e) => { if (rbTargetBoth.Checked) { _anim.NeonTarget = NeonGlowTarget.Both; TriggerUpdate(); } };
            page.Controls.Add(rbTargetBoth);
            curY += 24;

            rbTargetShape = new RadioButton
            {
                Text = "Chỉ khung viền",
                Location = new Point(20, curY),
                Size = new Size(180, 22)
            };
            rbTargetShape.CheckedChanged += (s, e) => { if (rbTargetShape.Checked) { _anim.NeonTarget = NeonGlowTarget.ShapeOnly; TriggerUpdate(); } };
            page.Controls.Add(rbTargetShape);
            curY += 24;

            rbTargetText = new RadioButton
            {
                Text = "Chỉ văn bản",
                Location = new Point(20, curY),
                Size = new Size(180, 22)
            };
            rbTargetText.CheckedChanged += (s, e) => { if (rbTargetText.Checked) { _anim.NeonTarget = NeonGlowTarget.TextOnly; TriggerUpdate(); } };
            page.Controls.Add(rbTargetText);
            curY += 32;

            // 2. Chế độ màu
            Label lblMode = new Label
            {
                Text = "Chế độ màu:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, curY),
                AutoSize = true
            };
            page.Controls.Add(lblMode);
            curY += 22;

            rbModeMono = new RadioButton
            {
                Text = "Đơn sắc",
                Location = new Point(20, curY),
                Size = new Size(100, 22),
                Checked = true
            };
            rbModeMono.CheckedChanged += (s, e) =>
            {
                if (rbModeMono.Checked)
                {
                    _anim.NeonColorMode = NeonColorMode.Monochrome;
                    pnlColorRow.Visible = true;
                    TriggerUpdate();
                }
            };
            page.Controls.Add(rbModeMono);

            rbModeRainbow = new RadioButton
            {
                Text = "Cầu vồng",
                Location = new Point(135, curY),
                Size = new Size(120, 22)
            };
            rbModeRainbow.CheckedChanged += (s, e) =>
            {
                if (rbModeRainbow.Checked)
                {
                    _anim.NeonColorMode = NeonColorMode.Rainbow;
                    pnlColorRow.Visible = false;
                    TriggerUpdate();
                }
            };
            page.Controls.Add(rbModeRainbow);
            curY += 30;

            // 3. Panel chọn màu sắc đơn sắc
            pnlColorRow = new Panel
            {
                Location = new Point(16, curY),
                Size = new Size(365, 34),
                BackColor = Color.Transparent
            };

            Label lblColor = new Label
            {
                Text = "Màu:",
                Location = new Point(4, 7),
                AutoSize = true,
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlColorRow.Controls.Add(lblColor);

            btnColorBox = new Button
            {
                Location = new Point(42, 3),
                Size = new Size(46, 25),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(0, 240, 255)
            };
            btnColorBox.FlatAppearance.BorderColor = Color.FromArgb(180, 190, 205);
            btnColorBox.FlatAppearance.BorderSize = 1;
            btnColorBox.Click += (s, e) =>
            {
                Point screenPt = btnColorBox.PointToScreen(new Point(0, btnColorBox.Height + 2));
                Color cur = btnColorBox.BackColor;
                ColorPaletteForm palette = new ColorPaletteForm(screenPt, cur, false, false);
                palette.OnColorSelected += (c) =>
                {
                    SetColor(c);
                };
                palette.Show();
            };
            pnlColorRow.Controls.Add(btnColorBox);

            txtHexColor = new TextBox
            {
                Location = new Point(96, 4),
                Size = new Size(72, 23),
                Font = new Font("Consolas", 9.0f),
                Text = "#00F0FF"
            };
            txtHexColor.TextChanged += (s, e) =>
            {
                if (txtHexColor.Focused)
                {
                    string hex = txtHexColor.Text.Trim().TrimStart('#');
                    if (hex.Length == 6)
                    {
                        try
                        {
                            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                            Color c = Color.FromArgb(r, g, b);
                            btnColorBox.BackColor = c;
                            _anim.NeonColor = c.ToArgb();
                            TriggerUpdate();
                        }
                        catch { }
                    }
                }
            };
            pnlColorRow.Controls.Add(txtHexColor);

            btnAutoColor = new Button
            {
                Text = "Màu theo hình",
                Location = new Point(176, 3),
                Size = new Size(105, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnAutoColor.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnAutoColor.Click += (s, e) =>
            {
                Color autoCol = Color.FromArgb(0, 240, 255);
                if (_shape != null)
                {
                    if (_shape.StrokeColor != 0 && _shape.StrokeColor != 0x00FFFFFF)
                        autoCol = Color.FromArgb((_shape.StrokeColor >> 16) & 0xFF, (_shape.StrokeColor >> 8) & 0xFF, _shape.StrokeColor & 0xFF);
                    else if (_shape.TextColor != 0 && _shape.TextColor != 0x00FFFFFF)
                        autoCol = Color.FromArgb((_shape.TextColor >> 16) & 0xFF, (_shape.TextColor >> 8) & 0xFF, _shape.TextColor & 0xFF);
                }
                SetColor(autoCol);
            };
            pnlColorRow.Controls.Add(btnAutoColor);

            page.Controls.Add(pnlColorRow);
        }

        private void BuildTab2Content(TabPage page)
        {
            int curY = 10;

            // 1. Bán kính tỏa sáng
            lblBloomRadiusVal = new Label
            {
                Text = "Bán kính tỏa sáng: 26 px",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, curY),
                AutoSize = true
            };
            page.Controls.Add(lblBloomRadiusVal);
            curY += 20;

            tbBloomRadius = new TrackBar
            {
                Minimum = 6,
                Maximum = 60,
                Value = 26,
                TickFrequency = 6,
                Location = new Point(12, curY),
                Size = new Size(365, 32),
                Height = 32
            };
            tbBloomRadius.ValueChanged += (s, e) =>
            {
                _anim.NeonBloomRadius = tbBloomRadius.Value;
                lblBloomRadiusVal.Text = "Bán kính tỏa sáng: " + tbBloomRadius.Value + " px";
                TriggerUpdate();
            };
            page.Controls.Add(tbBloomRadius);
            curY += 38;

            // 2. Cường độ phát sáng
            lblIntensityVal = new Label
            {
                Text = "Cường độ phát sáng: 100 %",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, curY),
                AutoSize = true
            };
            page.Controls.Add(lblIntensityVal);
            curY += 20;

            tbIntensity = new TrackBar
            {
                Minimum = 20,
                Maximum = 200,
                Value = 100,
                TickFrequency = 20,
                Location = new Point(12, curY),
                Size = new Size(365, 32),
                Height = 32
            };
            tbIntensity.ValueChanged += (s, e) =>
            {
                _anim.NeonIntensity = tbIntensity.Value / 100.0f;
                lblIntensityVal.Text = "Cường độ phát sáng: " + tbIntensity.Value + " %";
                TriggerUpdate();
            };
            page.Controls.Add(tbIntensity);
            curY += 38;

            // 3. Tia lóe sáng 4 góc
            chkCornerFlares = new CheckBox
            {
                Text = "Bật tia lóe sáng 4 góc",
                Location = new Point(16, curY),
                Size = new Size(200, 22),
                Checked = true
            };
            chkCornerFlares.CheckedChanged += (s, e) =>
            {
                _anim.NeonCornerFlares = chkCornerFlares.Checked;
                TriggerUpdate();
            };
            page.Controls.Add(chkCornerFlares);
            curY += 26;

            // 4. Lặp vô hạn & Số lần lặp
            chkInfiniteLoop = new CheckBox
            {
                Text = "Lặp vô hạn",
                Location = new Point(16, curY),
                Size = new Size(110, 22),
                Checked = true
            };
            chkInfiniteLoop.CheckedChanged += (s, e) =>
            {
                _anim.NeonInfiniteLoop = chkInfiniteLoop.Checked;
                lblRepeatCount.Enabled = !chkInfiniteLoop.Checked;
                cboRepeatCount.Enabled = !chkInfiniteLoop.Checked;
                TriggerUpdate();
            };
            page.Controls.Add(chkInfiniteLoop);

            lblRepeatCount = new Label
            {
                Text = "Số lần:",
                Location = new Point(135, curY + 2),
                AutoSize = true,
                ForeColor = Color.FromArgb(51, 65, 85),
                Enabled = false
            };
            page.Controls.Add(lblRepeatCount);

            cboRepeatCount = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(185, curY),
                Size = new Size(95, 23),
                Enabled = false
            };
            cboRepeatCount.Items.AddRange(new object[] { "1 lần", "2 lần", "3 lần", "5 lần", "10 lần" });
            cboRepeatCount.SelectedIndex = 0;
            cboRepeatCount.SelectedIndexChanged += (s, e) =>
            {
                if (!chkInfiniteLoop.Checked)
                {
                    int repeats = 1;
                    if (cboRepeatCount.SelectedIndex == 1) repeats = 2;
                    else if (cboRepeatCount.SelectedIndex == 2) repeats = 3;
                    else if (cboRepeatCount.SelectedIndex == 3) repeats = 5;
                    else if (cboRepeatCount.SelectedIndex == 4) repeats = 10;
                    _anim.NeonRepeatCount = repeats;
                    TriggerUpdate();
                }
            };
            page.Controls.Add(cboRepeatCount);
        }

        private void SetColor(Color c)
        {
            btnColorBox.BackColor = c;
            txtHexColor.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
            _anim.NeonColor = c.ToArgb();
            TriggerUpdate();
        }

        private void TriggerUpdate()
        {
            SyncToAnimation();
            if (_onChanged != null) _onChanged();
        }

        private void LoadAnimationValues()
        {
            // Target
            if (_anim.NeonTarget == NeonGlowTarget.ShapeOnly) rbTargetShape.Checked = true;
            else if (_anim.NeonTarget == NeonGlowTarget.TextOnly) rbTargetText.Checked = true;
            else rbTargetBoth.Checked = true;

            // Mode
            if (_anim.NeonColorMode == NeonColorMode.Rainbow)
            {
                rbModeRainbow.Checked = true;
                pnlColorRow.Visible = false;
            }
            else
            {
                rbModeMono.Checked = true;
                pnlColorRow.Visible = true;
            }

            // Color
            Color c = _anim.NeonColor != 0 ? Color.FromArgb(_anim.NeonColor) : Color.FromArgb(0, 240, 255);
            btnColorBox.BackColor = c;
            txtHexColor.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);

            // Bloom
            int bloom = (int)Math.Max(6, Math.Min(60, _anim.NeonBloomRadius));
            tbBloomRadius.Value = bloom;
            lblBloomRadiusVal.Text = "Bán kính tỏa sáng: " + bloom + " px";

            // Intensity
            int inten = (int)Math.Max(20, Math.Min(200, (_anim.NeonIntensity > 0 ? _anim.NeonIntensity : 1.0f) * 100));
            tbIntensity.Value = inten;
            lblIntensityVal.Text = "Cường độ phát sáng: " + inten + " %";

            // Corner Flares
            chkCornerFlares.Checked = _anim.NeonCornerFlares;

            // Infinite loop
            chkInfiniteLoop.Checked = _anim.NeonInfiniteLoop;
            lblRepeatCount.Enabled = !_anim.NeonInfiniteLoop;
            cboRepeatCount.Enabled = !_anim.NeonInfiniteLoop;

            int reps = _anim.NeonRepeatCount;
            if (reps == 2) cboRepeatCount.SelectedIndex = 1;
            else if (reps == 3) cboRepeatCount.SelectedIndex = 2;
            else if (reps == 5) cboRepeatCount.SelectedIndex = 3;
            else if (reps == 10) cboRepeatCount.SelectedIndex = 4;
            else cboRepeatCount.SelectedIndex = 0;
        }

        private void SyncToAnimation()
        {
            if (rbTargetShape.Checked) _anim.NeonTarget = NeonGlowTarget.ShapeOnly;
            else if (rbTargetText.Checked) _anim.NeonTarget = NeonGlowTarget.TextOnly;
            else _anim.NeonTarget = NeonGlowTarget.Both;

            if (rbModeRainbow.Checked) _anim.NeonColorMode = NeonColorMode.Rainbow;
            else _anim.NeonColorMode = NeonColorMode.Monochrome;

            _anim.NeonColor = btnColorBox.BackColor.ToArgb();
            _anim.NeonBloomRadius = tbBloomRadius.Value;
            _anim.NeonIntensity = tbIntensity.Value / 100.0f;
            _anim.NeonCornerFlares = chkCornerFlares.Checked;
            _anim.NeonInfiniteLoop = chkInfiniteLoop.Checked;

            int repeats = 1;
            if (cboRepeatCount.SelectedIndex == 1) repeats = 2;
            else if (cboRepeatCount.SelectedIndex == 2) repeats = 3;
            else if (cboRepeatCount.SelectedIndex == 3) repeats = 5;
            else if (cboRepeatCount.SelectedIndex == 4) repeats = 10;
            _anim.NeonRepeatCount = repeats;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    /// <summary>
    /// Authentic MS PowerPoint "Set Up Show" Dialog Window
    /// </summary>
    public class SetUpSlideShowForm : Form
    {
        public int ShowType { get; set; } // 0: Speaker FullScreen, 1: Individual Window, 2: Kiosk
        public bool LoopUntilEsc { get; set; }
        public bool ShowWithoutNarration { get; set; }
        public bool ShowWithoutAnimation { get; set; }
        public bool DisableHardwareAcceleration { get; set; }
        public Color PenColor { get; set; }
        public Color LaserPointerColor { get; set; }
        public bool ShowAllSlides { get; set; }
        public int SlideFrom { get; set; }
        public int SlideTo { get; set; }
        public int CustomShowIndex { get; set; }
        public bool AdvanceWithTimings { get; set; }
        public int MonitorIndex { get; set; }
        public string Resolution { get; set; }
        public bool UsePresenterView { get; set; }

        private RadioButton rbSpeaker;
        private RadioButton rbWindow;
        private RadioButton rbKiosk;
        private CheckBox chkLoop;
        private CheckBox chkNoNarration;
        private CheckBox chkNoAnimation;
        private CheckBox chkDisableHw;
        private Button btnPenColor;
        private Button btnLaserColor;
        private RadioButton rbShowAll;
        private RadioButton rbShowRange;
        private RadioButton rbCustomShow;
        private NumericUpDown numFrom;
        private NumericUpDown numTo;
        private ComboBox cboCustomShows;
        private RadioButton rbAdvanceManual;
        private RadioButton rbAdvanceTimings;
        private ComboBox cboMonitors;
        private ComboBox cboResolution;
        private CheckBox chkPresenterView;
        private int _totalSlides;

        public SetUpSlideShowForm(int totalSlides, int showType, bool loopUntilEsc, bool noNarration, bool noAnimation, bool advanceTimings, bool usePresenterView, int monitorIdx, int fromSlide, int toSlide)
        {
            _totalSlides = Math.Max(1, totalSlides);
            ShowType = showType;
            LoopUntilEsc = loopUntilEsc;
            ShowWithoutNarration = noNarration;
            ShowWithoutAnimation = noAnimation;
            AdvanceWithTimings = advanceTimings;
            UsePresenterView = usePresenterView;
            MonitorIndex = monitorIdx;
            SlideFrom = Math.Max(1, fromSlide);
            SlideTo = toSlide <= 0 ? _totalSlides : Math.Min(_totalSlides, toSlide);
            ShowAllSlides = (fromSlide <= 1 && toSlide >= _totalSlides);
            PenColor = Color.Red;
            LaserPointerColor = Color.Red;
            Resolution = "Use Current Resolution";

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Set Up Show";
            Size = new Size(540, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            HelpButton = true;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            BackColor = Color.FromArgb(243, 244, 246);

            // ==================== 1. Group: Show type ====================
            GroupBox gbType = new GroupBox
            {
                Text = "Show type",
                Location = new Point(12, 10),
                Size = new Size(248, 115),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            rbSpeaker = new RadioButton { Text = "Presented by a speaker (full screen)", Location = new Point(10, 22), AutoSize = true, Checked = (ShowType == 0) };
            rbWindow = new RadioButton { Text = "Browsed by an individual (window)", Location = new Point(10, 50), AutoSize = true, Checked = (ShowType == 1) };
            rbKiosk = new RadioButton { Text = "Browsed at a kiosk (full screen)", Location = new Point(10, 78), AutoSize = true, Checked = (ShowType == 2) };

            rbSpeaker.CheckedChanged += (s, e) =>
            {
                if (rbSpeaker.Checked)
                {
                    if (chkPresenterView != null) chkPresenterView.Enabled = true;
                    if (chkLoop != null) chkLoop.Enabled = true;
                }
            };
            rbWindow.CheckedChanged += (s, e) =>
            {
                if (rbWindow.Checked)
                {
                    if (chkPresenterView != null) { chkPresenterView.Checked = false; chkPresenterView.Enabled = false; }
                    if (chkLoop != null) chkLoop.Enabled = true;
                }
            };
            rbKiosk.CheckedChanged += (s, e) =>
            {
                if (rbKiosk.Checked)
                {
                    if (chkLoop != null) { chkLoop.Checked = true; chkLoop.Enabled = false; }
                    if (chkPresenterView != null) { chkPresenterView.Checked = false; chkPresenterView.Enabled = false; }
                }
            };
            gbType.Controls.AddRange(new Control[] { rbSpeaker, rbWindow, rbKiosk });

            // ==================== 2. Group: Show options ====================
            GroupBox gbOptions = new GroupBox
            {
                Text = "Show options",
                Location = new Point(12, 132),
                Size = new Size(248, 205),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            chkLoop = new CheckBox { Text = "Loop continuously until 'Esc'", Location = new Point(10, 22), AutoSize = true, Checked = (ShowType == 2) || LoopUntilEsc, Enabled = (ShowType != 2) };
            chkNoNarration = new CheckBox { Text = "Show without narration", Location = new Point(10, 48), AutoSize = true, Checked = ShowWithoutNarration };
            chkNoAnimation = new CheckBox { Text = "Show without animation", Location = new Point(10, 74), AutoSize = true, Checked = ShowWithoutAnimation };
            chkDisableHw = new CheckBox { Text = "Disable hardware graphics acceleration", Location = new Point(10, 100), AutoSize = true, Checked = DisableHardwareAcceleration };

            Label lblPen = new Label { Text = "Pen color:", Location = new Point(10, 134), AutoSize = true };
            btnPenColor = CreateColorDropdownButton(115, 130, PenColor, c => { PenColor = c; });

            Label lblLaser = new Label { Text = "Laser pointer color:", Location = new Point(10, 168), AutoSize = true };
            btnLaserColor = CreateColorDropdownButton(115, 164, LaserPointerColor, c => { LaserPointerColor = c; });

            gbOptions.Controls.AddRange(new Control[] { chkLoop, chkNoNarration, chkNoAnimation, chkDisableHw, lblPen, btnPenColor, lblLaser, btnLaserColor });

            // ==================== 3. Group: Show slides ====================
            GroupBox gbRange = new GroupBox
            {
                Text = "Show slides",
                Location = new Point(268, 10),
                Size = new Size(248, 140),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            rbShowAll = new RadioButton { Text = "All", Location = new Point(10, 22), AutoSize = true, Checked = ShowAllSlides };
            rbShowRange = new RadioButton { Text = "From:", Location = new Point(10, 50), AutoSize = true, Checked = !ShowAllSlides };

            numFrom = new NumericUpDown { Location = new Point(75, 48), Size = new Size(52, 23), Minimum = 1, Maximum = _totalSlides, Value = SlideFrom };
            Label lblTo = new Label { Text = "To:", Location = new Point(135, 52), AutoSize = true };
            numTo = new NumericUpDown { Location = new Point(165, 48), Size = new Size(52, 23), Minimum = 1, Maximum = _totalSlides, Value = SlideTo };

            rbCustomShow = new RadioButton { Text = "Custom show:", Location = new Point(10, 80), AutoSize = true, Checked = false };
            cboCustomShows = new ComboBox { Location = new Point(32, 104), Size = new Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            cboCustomShows.Items.Add("(No custom shows)");
            cboCustomShows.SelectedIndex = 0;

            rbShowAll.CheckedChanged += (s, e) => UpdateRangeControls();
            rbShowRange.CheckedChanged += (s, e) => UpdateRangeControls();
            rbCustomShow.CheckedChanged += (s, e) => UpdateRangeControls();

            gbRange.Controls.AddRange(new Control[] { rbShowAll, rbShowRange, numFrom, lblTo, numTo, rbCustomShow, cboCustomShows });

            // ==================== 4. Group: Advance slides ====================
            GroupBox gbAdvance = new GroupBox
            {
                Text = "Advance slides",
                Location = new Point(268, 156),
                Size = new Size(248, 85),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            rbAdvanceManual = new RadioButton { Text = "Manually", Location = new Point(10, 24), AutoSize = true, Checked = !AdvanceWithTimings };
            rbAdvanceTimings = new RadioButton { Text = "Using timings, if present", Location = new Point(10, 50), AutoSize = true, Checked = AdvanceWithTimings };
            gbAdvance.Controls.AddRange(new Control[] { rbAdvanceManual, rbAdvanceTimings });

            // ==================== 5. Group: Multiple monitors ====================
            GroupBox gbMonitors = new GroupBox
            {
                Text = "Multiple monitors",
                Location = new Point(268, 248),
                Size = new Size(248, 180),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            Label lblMon = new Label { Text = "Slide show monitor:", Location = new Point(10, 22), AutoSize = true };
            cboMonitors = new ComboBox { Location = new Point(10, 44), Size = new Size(224, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboMonitors.Items.Add("Automatic");
            for (int m = 0; m < Screen.AllScreens.Length; m++)
            {
                cboMonitors.Items.Add(string.Format("Monitor {0}: {1}", m + 1, Screen.AllScreens[m].DeviceName));
            }
            cboMonitors.SelectedIndex = Math.Min(cboMonitors.Items.Count - 1, Math.Max(0, MonitorIndex));

            Label lblRes = new Label { Text = "Resolution:", Location = new Point(10, 76), AutoSize = true };
            cboResolution = new ComboBox { Location = new Point(10, 98), Size = new Size(224, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboResolution.Items.AddRange(new object[] { "Use Current Resolution", "1920x1080", "1600x900", "1366x768", "1280x720", "1024x768", "800x600" });
            cboResolution.SelectedIndex = 0;

            chkPresenterView = new CheckBox
            {
                Text = "Use Presenter View",
                Location = new Point(10, 134),
                AutoSize = true,
                Checked = (ShowType == 0) && UsePresenterView,
                Enabled = (ShowType == 0)
            };
            gbMonitors.Controls.AddRange(new Control[] { lblMon, cboMonitors, lblRes, cboResolution, chkPresenterView });

            // ==================== Bottom Buttons ====================
            Button btnOk = new Button
            {
                Text = "OK",
                Location = new Point(356, 442),
                Size = new Size(76, 26),
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) =>
            {
                ShowType = rbSpeaker.Checked ? 0 : (rbWindow.Checked ? 1 : 2);
                LoopUntilEsc = chkLoop.Checked;
                ShowWithoutNarration = chkNoNarration.Checked;
                ShowWithoutAnimation = chkNoAnimation.Checked;
                DisableHardwareAcceleration = chkDisableHw.Checked;
                ShowAllSlides = rbShowAll.Checked;
                SlideFrom = (int)numFrom.Value;
                SlideTo = (int)numTo.Value;
                AdvanceWithTimings = rbAdvanceTimings.Checked;
                MonitorIndex = cboMonitors.SelectedIndex;
                Resolution = cboResolution.SelectedItem != null ? cboResolution.SelectedItem.ToString() : "Use Current Resolution";
                UsePresenterView = chkPresenterView.Checked;
                Close();
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(440, 442),
                Size = new Size(76, 26),
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { gbType, gbOptions, gbRange, gbAdvance, gbMonitors, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UpdateRangeControls();
        }

        private void UpdateRangeControls()
        {
            numFrom.Enabled = rbShowRange.Checked;
            numTo.Enabled = rbShowRange.Checked;
            cboCustomShows.Enabled = rbCustomShow.Checked;
        }

        private Button CreateColorDropdownButton(int x, int y, Color initialColor, Action<Color> onColorChanged)
        {
            Button btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(48, 26),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Cursor = Cursors.Hand
            };

            Color curCol = initialColor;

            btn.Paint += (s, pe) =>
            {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Pen icon with color underline
                int px = 6;
                int py = 4;
                using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.2f))
                {
                    g.DrawLine(p, px + 8, py, px + 12, py + 4);
                    g.DrawLine(p, px + 12, py + 4, px + 4, py + 12);
                    g.DrawLine(p, px + 4, py + 12, px, py + 12);
                    g.DrawLine(p, px, py + 12, px, py + 8);
                    g.DrawLine(p, px, py + 8, px + 8, py);
                }
                // Underline color bar
                using (SolidBrush cb = new SolidBrush(curCol))
                {
                    g.FillRectangle(cb, 4, 18, 20, 4);
                }
                // Dropdown arrow
                using (SolidBrush ab = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.FillPolygon(ab, new Point[] { new Point(32, 10), new Point(38, 10), new Point(35, 14) });
                }
            };

            btn.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = curCol;
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        curCol = cd.Color;
                        if (onColorChanged != null) onColorChanged(curCol);
                        btn.Invalidate();
                    }
                }
            };

            return btn;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class SlideFormatShapeForm : Form
    {
        public int FillColorResult { get; set; }
        public float FillOpacityResult { get; set; }
        public bool HasGradientFillResult { get; set; }
        public int GradientColor2Result { get; set; }
        public float GradientAngleResult { get; set; }

        public float StrokeWidthResult { get; set; }
        public int StrokeColorResult { get; set; }
        public DashStyle DashStyleResult { get; set; }

        public bool HasShadowResult { get; set; }
        public float ShadowBlurResult { get; set; }
        public float ShadowOffsetXResult { get; set; }
        public float ShadowOffsetYResult { get; set; }
        public float SoftEdgeRadiusResult { get; set; }

        public float MarginLeftResult { get; set; }
        public float MarginRightResult { get; set; }
        public float MarginTopResult { get; set; }
        public float MarginBottomResult { get; set; }

        private RadioButton rdoNoFill;
        private RadioButton rdoSolidFill;
        private RadioButton rdoGradFill;
        private Button btnFillColor;
        private Button btnGradColor2;
        private TrackBar tbFillOpacity;
        private Label lblOpacityVal;
        private TrackBar tbGradAngle;
        private Label lblAngleVal;

        private RadioButton rdoNoLine;
        private RadioButton rdoSolidLine;
        private Button btnLineColor;
        private NumericUpDown numLineWidth;
        private ComboBox cboDashStyle;

        private CheckBox chkShadow;
        private TrackBar tbShadowBlur;
        private Label lblBlurVal;
        private TrackBar tbShadowDist;
        private Label lblDistVal;
        private TrackBar tbSoftEdges;
        private Label lblSoftVal;

        private NumericUpDown numMarginL;
        private NumericUpDown numMarginR;
        private NumericUpDown numMarginT;
        private NumericUpDown numMarginB;

        private PictureBox picPreview;
        private Color currentFillColor;
        private Color currentGradColor2;
        private Color currentLineColor;

        public SlideFormatShapeForm(ShapeNode shape)
        {
            FillColorResult = shape != null ? shape.FillColor : 0x00FFFFFF;
            FillOpacityResult = shape != null ? shape.FillOpacity : 1.0f;
            HasGradientFillResult = shape != null ? shape.HasGradientFill : false;
            GradientColor2Result = shape != null ? shape.GradientColor2 : 0x00FFFFFF;
            GradientAngleResult = shape != null ? shape.GradientAngle : 90f;

            StrokeWidthResult = shape != null ? shape.StrokeWidth : 1.5f;
            StrokeColorResult = shape != null ? shape.StrokeColor : 0x4472C4;
            DashStyleResult = shape != null ? shape.DashStyle : DashStyle.Solid;

            HasShadowResult = shape != null ? shape.HasShadow : false;
            ShadowBlurResult = shape != null ? shape.ShadowBlur : 4f;
            ShadowOffsetXResult = shape != null ? shape.ShadowOffsetX : 3f;
            ShadowOffsetYResult = shape != null ? shape.ShadowOffsetY : 3f;
            SoftEdgeRadiusResult = shape != null ? shape.SoftEdgeRadius : 0f;

            MarginLeftResult = shape?.MarginLeft ?? 10f;
            MarginRightResult = shape?.MarginRight ?? 10f;
            MarginTopResult = shape?.MarginTop ?? 5f;
            MarginBottomResult = shape?.MarginBottom ?? 5f;

            currentFillColor = (FillColorResult != 0 && FillColorResult != 0x00FFFFFF)
                ? Color.FromArgb((FillColorResult >> 16) & 0xFF, (FillColorResult >> 8) & 0xFF, FillColorResult & 0xFF)
                : Color.FromArgb(68, 114, 196);

            currentGradColor2 = (GradientColor2Result != 0 && GradientColor2Result != 0x00FFFFFF)
                ? Color.FromArgb((GradientColor2Result >> 16) & 0xFF, (GradientColor2Result >> 8) & 0xFF, GradientColor2Result & 0xFF)
                : Color.FromArgb(46, 117, 182);

            currentLineColor = (StrokeColorResult != 0 && StrokeColorResult != 0x00FFFFFF)
                ? Color.FromArgb((StrokeColorResult >> 16) & 0xFF, (StrokeColorResult >> 8) & 0xFF, StrokeColorResult & 0xFF)
                : Color.FromArgb(47, 85, 151);

            InitializeComponent();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            Text = "Định dạng Hình & Viền (Format Shape)";
            Size = new Size(560, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9f);

            TabControl tabCtrl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(520, 450),
                Font = new Font("Segoe UI", 9f)
            };

            // ==================== Tab 1: Tô màu & Viền ====================
            TabPage tabFillLine = new TabPage("Tô màu & Viền (Fill & Line)");
            tabFillLine.BackColor = Color.White;

            // Section 1: Tô màu
            GroupBox grpFill = new GroupBox { Text = "Tô màu (Fill)", Location = new Point(14, 10), Size = new Size(485, 140) };
            rdoNoFill = new RadioButton { Text = "Không tô (No fill)", Location = new Point(16, 22), AutoSize = true, Checked = (FillColorResult == 0x00FFFFFF) };
            rdoSolidFill = new RadioButton { Text = "Màu đơn (Solid fill)", Location = new Point(16, 46), AutoSize = true, Checked = (FillColorResult != 0x00FFFFFF && !HasGradientFillResult) };
            rdoGradFill = new RadioButton { Text = "Chuyển sắc (Gradient)", Location = new Point(16, 70), AutoSize = true, Checked = HasGradientFillResult };

            Label lblFClr = new Label { Text = "Màu 1:", Location = new Point(175, 24), AutoSize = true };
            btnFillColor = new Button { Location = new Point(220, 20), Size = new Size(45, 24), BackColor = currentFillColor, FlatStyle = FlatStyle.Flat };
            btnFillColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = currentFillColor }) {
                    if (cd.ShowDialog() == DialogResult.OK) {
                        currentFillColor = cd.Color;
                        btnFillColor.BackColor = currentFillColor;
                        FillColorResult = (currentFillColor.R << 16) | (currentFillColor.G << 8) | currentFillColor.B;
                        UpdatePreview();
                    }
                }
            };

            Label lblGClr = new Label { Text = "Màu 2:", Location = new Point(280, 24), AutoSize = true };
            btnGradColor2 = new Button { Location = new Point(325, 20), Size = new Size(45, 24), BackColor = currentGradColor2, FlatStyle = FlatStyle.Flat };
            btnGradColor2.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = currentGradColor2 }) {
                    if (cd.ShowDialog() == DialogResult.OK) {
                        currentGradColor2 = cd.Color;
                        btnGradColor2.BackColor = currentGradColor2;
                        GradientColor2Result = (currentGradColor2.R << 16) | (currentGradColor2.G << 8) | currentGradColor2.B;
                        UpdatePreview();
                    }
                }
            };

            Label lblOp = new Label { Text = "Độ trong suốt:", Location = new Point(175, 54), AutoSize = true };
            tbFillOpacity = new TrackBar { Location = new Point(265, 50), Size = new Size(160, 30), Minimum = 0, Maximum = 100, Value = (int)(FillOpacityResult * 100) };
            lblOpacityVal = new Label { Text = tbFillOpacity.Value + "%", Location = new Point(430, 54), AutoSize = true };
            tbFillOpacity.ValueChanged += (s, e) => {
                FillOpacityResult = tbFillOpacity.Value / 100f;
                lblOpacityVal.Text = tbFillOpacity.Value + "%";
                UpdatePreview();
            };

            Label lblAng = new Label { Text = "Góc độ:", Location = new Point(175, 90), AutoSize = true };
            tbGradAngle = new TrackBar { Location = new Point(265, 86), Size = new Size(160, 30), Minimum = 0, Maximum = 360, Value = (int)GradientAngleResult };
            lblAngleVal = new Label { Text = tbGradAngle.Value + "°", Location = new Point(430, 90), AutoSize = true };
            tbGradAngle.ValueChanged += (s, e) => {
                GradientAngleResult = tbGradAngle.Value;
                lblAngleVal.Text = tbGradAngle.Value + "°";
                UpdatePreview();
            };

            EventHandler fillRadioChanged = (s, e) => {
                if (rdoNoFill.Checked) FillColorResult = 0x00FFFFFF;
                else FillColorResult = (currentFillColor.R << 16) | (currentFillColor.G << 8) | currentFillColor.B;
                HasGradientFillResult = rdoGradFill.Checked;
                btnGradColor2.Enabled = rdoGradFill.Checked;
                tbGradAngle.Enabled = rdoGradFill.Checked;
                UpdatePreview();
            };
            rdoNoFill.CheckedChanged += fillRadioChanged;
            rdoSolidFill.CheckedChanged += fillRadioChanged;
            rdoGradFill.CheckedChanged += fillRadioChanged;

            grpFill.Controls.AddRange(new Control[] { rdoNoFill, rdoSolidFill, rdoGradFill, lblFClr, btnFillColor, lblGClr, btnGradColor2, lblOp, tbFillOpacity, lblOpacityVal, lblAng, tbGradAngle, lblAngleVal });
            tabFillLine.Controls.Add(grpFill);

            // Section 2: Viền hình
            GroupBox grpLine = new GroupBox { Text = "Viền hình (Line)", Location = new Point(14, 155), Size = new Size(485, 110) };
            rdoNoLine = new RadioButton { Text = "Không viền (No line)", Location = new Point(16, 24), AutoSize = true, Checked = (StrokeWidthResult <= 0f) };
            rdoSolidLine = new RadioButton { Text = "Viền đơn (Solid line)", Location = new Point(16, 52), AutoSize = true, Checked = (StrokeWidthResult > 0f) };

            Label lblLClr = new Label { Text = "Màu viền:", Location = new Point(175, 24), AutoSize = true };
            btnLineColor = new Button { Location = new Point(240, 20), Size = new Size(45, 24), BackColor = currentLineColor, FlatStyle = FlatStyle.Flat };
            btnLineColor.Click += (s, e) => {
                using (ColorDialog cd = new ColorDialog { Color = currentLineColor }) {
                    if (cd.ShowDialog() == DialogResult.OK) {
                        currentLineColor = cd.Color;
                        btnLineColor.BackColor = currentLineColor;
                        StrokeColorResult = (currentLineColor.R << 16) | (currentLineColor.G << 8) | currentLineColor.B;
                        UpdatePreview();
                    }
                }
            };

            Label lblLW = new Label { Text = "Độ dày (pt):", Location = new Point(175, 58), AutoSize = true };
            numLineWidth = new NumericUpDown { Location = new Point(255, 56), Size = new Size(70, 24), DecimalPlaces = 2, Minimum = 0.25m, Maximum = 20m, Increment = 0.25m, Value = (decimal)Math.Max(0.25f, StrokeWidthResult) };
            numLineWidth.ValueChanged += (s, e) => {
                if (rdoSolidLine.Checked) StrokeWidthResult = (float)numLineWidth.Value;
                UpdatePreview();
            };

            Label lblDash = new Label { Text = "Kiểu nét:", Location = new Point(335, 58), AutoSize = true };
            cboDashStyle = new ComboBox { Location = new Point(390, 56), Size = new Size(85, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboDashStyle.Items.AddRange(new object[] { "Liền (Solid)", "Nét đứt (Dash)", "Chấm (Dot)" });
            cboDashStyle.SelectedIndex = DashStyleResult == DashStyle.Dash ? 1 : (DashStyleResult == DashStyle.Dot ? 2 : 0);
            cboDashStyle.SelectedIndexChanged += (s, e) => {
                DashStyleResult = cboDashStyle.SelectedIndex == 1 ? DashStyle.Dash : (cboDashStyle.SelectedIndex == 2 ? DashStyle.Dot : DashStyle.Solid);
                UpdatePreview();
            };

            EventHandler lineRadioChanged = (s, e) => {
                if (rdoNoLine.Checked) StrokeWidthResult = 0f;
                else StrokeWidthResult = (float)numLineWidth.Value;
                btnLineColor.Enabled = rdoSolidLine.Checked;
                numLineWidth.Enabled = rdoSolidLine.Checked;
                cboDashStyle.Enabled = rdoSolidLine.Checked;
                UpdatePreview();
            };
            rdoNoLine.CheckedChanged += lineRadioChanged;
            rdoSolidLine.CheckedChanged += lineRadioChanged;

            grpLine.Controls.AddRange(new Control[] { rdoNoLine, rdoSolidLine, lblLClr, btnLineColor, lblLW, numLineWidth, lblDash, cboDashStyle });
            tabFillLine.Controls.Add(grpLine);

            // Preview Box in Tab 1
            GroupBox grpP = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(14, 270), Size = new Size(485, 140) };
            picPreview = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.White };
            picPreview.Paint += DrawPreviewShape;
            grpP.Controls.Add(picPreview);
            tabFillLine.Controls.Add(grpP);

            // ==================== Tab 2: Hiệu ứng & Đổ bóng ====================
            TabPage tabEffects = new TabPage("Hiệu ứng & Đổ bóng (Effects)");
            tabEffects.BackColor = Color.White;

            GroupBox grpShadow = new GroupBox { Text = "Đổ bóng (Shadow)", Location = new Point(14, 12), Size = new Size(485, 160) };
            chkShadow = new CheckBox { Text = "Bật hiệu ứng đổ bóng (Enable Shadow)", Location = new Point(16, 26), AutoSize = true, Checked = HasShadowResult };

            Label lblBl = new Label { Text = "Độ mờ (Blur):", Location = new Point(16, 60), AutoSize = true };
            tbShadowBlur = new TrackBar { Location = new Point(120, 56), Size = new Size(200, 30), Minimum = 0, Maximum = 40, Value = (int)Math.Max(0, Math.Min(40, ShadowBlurResult)) };
            lblBlurVal = new Label { Text = tbShadowBlur.Value + " px", Location = new Point(330, 60), AutoSize = true };
            tbShadowBlur.ValueChanged += (s, e) => {
                ShadowBlurResult = tbShadowBlur.Value;
                lblBlurVal.Text = tbShadowBlur.Value + " px";
                UpdatePreview();
            };

            Label lblDs = new Label { Text = "Khoảng cách:", Location = new Point(16, 100), AutoSize = true };
            tbShadowDist = new TrackBar { Location = new Point(120, 96), Size = new Size(200, 30), Minimum = 0, Maximum = 40, Value = (int)Math.Max(0, Math.Min(40, ShadowOffsetXResult)) };
            lblDistVal = new Label { Text = tbShadowDist.Value + " px", Location = new Point(330, 100), AutoSize = true };
            tbShadowDist.ValueChanged += (s, e) => {
                ShadowOffsetXResult = tbShadowDist.Value;
                ShadowOffsetYResult = tbShadowDist.Value;
                lblDistVal.Text = tbShadowDist.Value + " px";
                UpdatePreview();
            };

            chkShadow.CheckedChanged += (s, e) => {
                HasShadowResult = chkShadow.Checked;
                tbShadowBlur.Enabled = chkShadow.Checked;
                tbShadowDist.Enabled = chkShadow.Checked;
                UpdatePreview();
            };

            grpShadow.Controls.AddRange(new Control[] { chkShadow, lblBl, tbShadowBlur, lblBlurVal, lblDs, tbShadowDist, lblDistVal });
            tabEffects.Controls.Add(grpShadow);

            GroupBox grpSoft = new GroupBox { Text = "Làm mềm mép (Soft Edges)", Location = new Point(14, 185), Size = new Size(485, 80) };
            Label lblSft = new Label { Text = "Bán kính mép:", Location = new Point(16, 30), AutoSize = true };
            tbSoftEdges = new TrackBar { Location = new Point(120, 26), Size = new Size(200, 30), Minimum = 0, Maximum = 40, Value = (int)Math.Max(0, Math.Min(40, SoftEdgeRadiusResult)) };
            lblSoftVal = new Label { Text = tbSoftEdges.Value + " pt", Location = new Point(330, 30), AutoSize = true };
            tbSoftEdges.ValueChanged += (s, e) => {
                SoftEdgeRadiusResult = tbSoftEdges.Value;
                lblSoftVal.Text = tbSoftEdges.Value + " pt";
                UpdatePreview();
            };
            grpSoft.Controls.AddRange(new Control[] { lblSft, tbSoftEdges, lblSoftVal });
            tabEffects.Controls.Add(grpSoft);

            // ==================== Tab 3: Hộp chữ & Lề ====================
            TabPage tabMargins = new TabPage("Lề hộp chữ (Margins)");
            tabMargins.BackColor = Color.White;

            GroupBox grpMargins = new GroupBox { Text = "Lề trong (Internal Margins)", Location = new Point(14, 12), Size = new Size(485, 140) };
            Label lblML = new Label { Text = "Lề trái (pt):", Location = new Point(20, 30), AutoSize = true };
            numMarginL = new NumericUpDown { Location = new Point(110, 28), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = (decimal)MarginLeftResult };
            numMarginL.ValueChanged += (s, e) => MarginLeftResult = (float)numMarginL.Value;

            Label lblMR = new Label { Text = "Lề phải (pt):", Location = new Point(240, 30), AutoSize = true };
            numMarginR = new NumericUpDown { Location = new Point(330, 28), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = (decimal)MarginRightResult };
            numMarginR.ValueChanged += (s, e) => MarginRightResult = (float)numMarginR.Value;

            Label lblMT = new Label { Text = "Lề trên (pt):", Location = new Point(20, 75), AutoSize = true };
            numMarginT = new NumericUpDown { Location = new Point(110, 73), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = (decimal)MarginTopResult };
            numMarginT.ValueChanged += (s, e) => MarginTopResult = (float)numMarginT.Value;

            Label lblMB = new Label { Text = "Lề dưới (pt):", Location = new Point(240, 75), AutoSize = true };
            numMarginB = new NumericUpDown { Location = new Point(330, 73), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = (decimal)MarginBottomResult };
            numMarginB.ValueChanged += (s, e) => MarginBottomResult = (float)numMarginB.Value;

            grpMargins.Controls.AddRange(new Control[] { lblML, numMarginL, lblMR, numMarginR, lblMT, numMarginT, lblMB, numMarginB });
            tabMargins.Controls.Add(grpMargins);

            tabCtrl.TabPages.Add(tabFillLine);
            tabCtrl.TabPages.Add(tabEffects);
            tabCtrl.TabPages.Add(tabMargins);
            Controls.Add(tabCtrl);

            // Action Buttons
            Button btnOk = new Button { Text = "Đồng ý", DialogResult = DialogResult.OK, Location = new Point(360, 475), Size = new Size(80, 30), BackColor = Color.FromArgb(210, 71, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(450, 475), Size = new Size(80, 30), BackColor = Color.FromArgb(229, 231, 235), FlatStyle = FlatStyle.Flat };

            btnOk.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void UpdatePreview()
        {
            if (picPreview != null) picPreview.Invalidate();
        }

        private void DrawPreviewShape(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = picPreview.ClientSize.Width;
            int ph = picPreview.ClientSize.Height;

            Rectangle rect = new Rectangle(pw / 2 - 80, ph / 2 - 40, 160, 80);

            // Draw shadow
            if (HasShadowResult)
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    Rectangle shadowR = new Rectangle(rect.X + (int)ShadowOffsetXResult, rect.Y + (int)ShadowOffsetYResult, rect.Width, rect.Height);
                    g.FillRectangle(sb, shadowR);
                }
            }

            // Draw fill
            if (FillColorResult != 0x00FFFFFF)
            {
                int alpha = (int)(Math.Max(0f, Math.Min(1f, FillOpacityResult)) * 255);
                Color fc = Color.FromArgb(alpha, currentFillColor.R, currentFillColor.G, currentFillColor.B);

                if (HasGradientFillResult)
                {
                    Color gc2 = Color.FromArgb(alpha, currentGradColor2.R, currentGradColor2.G, currentGradColor2.B);
                    using (LinearGradientBrush lgb = new LinearGradientBrush(rect, fc, gc2, GradientAngleResult))
                    {
                        g.FillRectangle(lgb, rect);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(fc))
                    {
                        g.FillRectangle(b, rect);
                    }
                }
            }

            // Draw outline
            if (StrokeWidthResult > 0f)
            {
                using (Pen p = new Pen(currentLineColor, Math.Max(1f, StrokeWidthResult)) { DashStyle = DashStyleResult })
                {
                    g.DrawRectangle(p, rect);
                }
            }

            // Sample text
            using (Font f = new Font("Segoe UI", 9f))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Mẫu Hình Vẽ", f, tb, rect, sf);
            }
        }

        public void ApplyToShape(ShapeNode shape)
        {
            if (shape == null) return;
            shape.FillColor = FillColorResult;
            shape.FillOpacity = FillOpacityResult;
            shape.HasGradientFill = HasGradientFillResult;
            shape.GradientColor2 = GradientColor2Result;
            shape.GradientAngle = GradientAngleResult;

            shape.StrokeWidth = StrokeWidthResult;
            shape.StrokeColor = StrokeColorResult;
            shape.DashStyle = DashStyleResult;

            shape.HasShadow = HasShadowResult;
            shape.ShadowBlur = ShadowBlurResult;
            shape.ShadowOffsetX = ShadowOffsetXResult;
            shape.ShadowOffsetY = ShadowOffsetYResult;
            shape.SoftEdgeRadius = SoftEdgeRadiusResult;

            shape.MarginLeft = MarginLeftResult;
            shape.MarginRight = MarginRightResult;
            shape.MarginTop = MarginTopResult;
            shape.MarginBottom = MarginBottomResult;
        }
    }
}

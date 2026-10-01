using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class SlideParagraphDialogForm : Form
    {
        public TextAlignment AlignmentResult { get; set; }
        public VerticalTextAlignment VerticalAlignmentResult { get; set; }
        public float LineSpacingResult { get; set; }
        public float SpaceBeforeResult { get; set; }
        public float SpaceAfterResult { get; set; }
        public float MarginLeftResult { get; set; }
        public float MarginRightResult { get; set; }

        private ComboBox cboAlignment;
        private ComboBox cboVerticalAlignment;
        private NumericUpDown numIndentLeft;
        private NumericUpDown numIndentRight;
        private NumericUpDown numSpaceBefore;
        private NumericUpDown numSpaceAfter;
        private ComboBox cboLineSpacing;
        private NumericUpDown numLineSpacingAt;

        private Panel pnlPreview;
        private Label lblPreviewP1;
        private Label lblPreviewP2;

        public SlideParagraphDialogForm(ShapeNode shape)
        {
            AlignmentResult = shape?.Alignment ?? TextAlignment.Left;
            VerticalAlignmentResult = shape?.VerticalAlignment ?? VerticalTextAlignment.Top;
            LineSpacingResult = shape != null ? shape.LineSpacing : 1.2f;
            SpaceBeforeResult = 0f;
            SpaceAfterResult = 0f;
            MarginLeftResult = shape?.MarginLeft ?? 10f;
            MarginRightResult = shape?.MarginRight ?? 10f;

            if (shape != null && shape.TextRuns != null && shape.TextRuns.Count > 0)
            {
                foreach (var r in shape.TextRuns)
                {
                    if (r.ParaSpaceBefore.HasValue) SpaceBeforeResult = r.ParaSpaceBefore.Value;
                    if (r.ParaSpaceAfter.HasValue) SpaceAfterResult = r.ParaSpaceAfter.Value;
                    if (r.ParaLineSpacing.HasValue) LineSpacingResult = r.ParaLineSpacing.Value;
                    break;
                }
            }

            InitializeComponent();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            Text = "Đoạn văn (Paragraph)";
            Size = new Size(520, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9f);

            // Group 1: General (Chung - Căn chỉnh)
            GroupBox grpGeneral = new GroupBox { Text = "Chung (General)", Location = new Point(14, 12), Size = new Size(475, 75) };
            
            Label lblAlign = new Label { Text = "Căn chỉnh:", Location = new Point(16, 26), AutoSize = true };
            cboAlignment = new ComboBox { Location = new Point(90, 23), Size = new Size(130, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboAlignment.Items.AddRange(new object[] { "Trái (Left)", "Giữa (Center)", "Phải (Right)", "Đều 2 bên (Justified)" });
            cboAlignment.SelectedIndex = AlignmentResult == TextAlignment.Center ? 1 : (AlignmentResult == TextAlignment.Right ? 2 : (AlignmentResult == TextAlignment.Justify ? 3 : 0));
            cboAlignment.SelectedIndexChanged += (s, e) => {
                AlignmentResult = cboAlignment.SelectedIndex == 1 ? TextAlignment.Center : (cboAlignment.SelectedIndex == 2 ? TextAlignment.Right : (cboAlignment.SelectedIndex == 3 ? TextAlignment.Justify : TextAlignment.Left));
                UpdatePreview();
            };

            Label lblVert = new Label { Text = "Căn dọc:", Location = new Point(245, 26), AutoSize = true };
            cboVerticalAlignment = new ComboBox { Location = new Point(310, 23), Size = new Size(140, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboVerticalAlignment.Items.AddRange(new object[] { "Trên cùng (Top)", "Ở giữa (Middle)", "Dưới cùng (Bottom)" });
            cboVerticalAlignment.SelectedIndex = VerticalAlignmentResult == VerticalTextAlignment.Middle ? 1 : (VerticalAlignmentResult == VerticalTextAlignment.Bottom ? 2 : 0);
            cboVerticalAlignment.SelectedIndexChanged += (s, e) => {
                VerticalAlignmentResult = cboVerticalAlignment.SelectedIndex == 1 ? VerticalTextAlignment.Middle : (cboVerticalAlignment.SelectedIndex == 2 ? VerticalTextAlignment.Bottom : VerticalTextAlignment.Top);
                UpdatePreview();
            };

            grpGeneral.Controls.AddRange(new Control[] { lblAlign, cboAlignment, lblVert, cboVerticalAlignment });
            Controls.Add(grpGeneral);

            // Group 2: Indentation (Thụt lề)
            GroupBox grpIndent = new GroupBox { Text = "Thụt lề (Indentation)", Location = new Point(14, 95), Size = new Size(475, 75) };
            
            Label lblIndL = new Label { Text = "Bên trái (pt):", Location = new Point(16, 28), AutoSize = true };
            numIndentLeft = new NumericUpDown { Location = new Point(100, 26), Size = new Size(90, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 200, Value = (decimal)MarginLeftResult };
            numIndentLeft.ValueChanged += (s, e) => { MarginLeftResult = (float)numIndentLeft.Value; UpdatePreview(); };

            Label lblIndR = new Label { Text = "Bên phải (pt):", Location = new Point(245, 28), AutoSize = true };
            numIndentRight = new NumericUpDown { Location = new Point(340, 26), Size = new Size(90, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 200, Value = (decimal)MarginRightResult };
            numIndentRight.ValueChanged += (s, e) => { MarginRightResult = (float)numIndentRight.Value; UpdatePreview(); };

            grpIndent.Controls.AddRange(new Control[] { lblIndL, numIndentLeft, lblIndR, numIndentRight });
            Controls.Add(grpIndent);

            // Group 3: Spacing (Giãn cách)
            GroupBox grpSpacing = new GroupBox { Text = "Giãn cách (Spacing)", Location = new Point(14, 180), Size = new Size(475, 120) };

            Label lblSpcBef = new Label { Text = "Trước đoạn (pt):", Location = new Point(16, 26), AutoSize = true };
            numSpaceBefore = new NumericUpDown { Location = new Point(125, 23), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = (decimal)SpaceBeforeResult };
            numSpaceBefore.ValueChanged += (s, e) => { SpaceBeforeResult = (float)numSpaceBefore.Value; UpdatePreview(); };

            Label lblSpcAft = new Label { Text = "Sau đoạn (pt):", Location = new Point(16, 65), AutoSize = true };
            numSpaceAfter = new NumericUpDown { Location = new Point(125, 62), Size = new Size(80, 24), DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = (decimal)SpaceAfterResult };
            numSpaceAfter.ValueChanged += (s, e) => { SpaceAfterResult = (float)numSpaceAfter.Value; UpdatePreview(); };

            Label lblLnSpc = new Label { Text = "Cách dòng:", Location = new Point(245, 26), AutoSize = true };
            cboLineSpacing = new ComboBox { Location = new Point(325, 23), Size = new Size(130, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboLineSpacing.Items.AddRange(new object[] { "Đơn (1.0 line)", "1.5 dòng", "Đôi (2.0 lines)", "Nhiều dòng (Multiple)" });

            Label lblAt = new Label { Text = "Bằng:", Location = new Point(245, 65), AutoSize = true };
            numLineSpacingAt = new NumericUpDown { Location = new Point(325, 62), Size = new Size(80, 24), DecimalPlaces = 2, Minimum = 0.5m, Maximum = 5m, Increment = 0.1m, Value = 1.2m };

            if (Math.Abs(LineSpacingResult - 1.0f) < 0.05f) { cboLineSpacing.SelectedIndex = 0; numLineSpacingAt.Value = 1.0m; }
            else if (Math.Abs(LineSpacingResult - 1.5f) < 0.05f) { cboLineSpacing.SelectedIndex = 1; numLineSpacingAt.Value = 1.5m; }
            else if (Math.Abs(LineSpacingResult - 2.0f) < 0.05f) { cboLineSpacing.SelectedIndex = 2; numLineSpacingAt.Value = 2.0m; }
            else { cboLineSpacing.SelectedIndex = 3; numLineSpacingAt.Value = (decimal)Math.Max(0.5f, Math.Min(5.0f, LineSpacingResult)); }

            cboLineSpacing.SelectedIndexChanged += (s, e) => {
                if (cboLineSpacing.SelectedIndex == 0) numLineSpacingAt.Value = 1.0m;
                else if (cboLineSpacing.SelectedIndex == 1) numLineSpacingAt.Value = 1.5m;
                else if (cboLineSpacing.SelectedIndex == 2) numLineSpacingAt.Value = 2.0m;
                LineSpacingResult = (float)numLineSpacingAt.Value;
                UpdatePreview();
            };
            numLineSpacingAt.ValueChanged += (s, e) => {
                LineSpacingResult = (float)numLineSpacingAt.Value;
                UpdatePreview();
            };

            grpSpacing.Controls.AddRange(new Control[] { lblSpcBef, numSpaceBefore, lblSpcAft, numSpaceAfter, lblLnSpc, cboLineSpacing, lblAt, numLineSpacingAt });
            Controls.Add(grpSpacing);

            // Group 4: Preview (Xem trước)
            GroupBox grpPreview = new GroupBox { Text = "Xem trước (Preview)", Location = new Point(14, 310), Size = new Size(475, 115) };
            pnlPreview = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
            
            lblPreviewP1 = new Label {
                Dock = DockStyle.Top,
                Height = 45,
                Text = "Đoạn văn mẫu thứ nhất minh họa căn chỉnh lề, khoảng cách dòng và thụt lề chuẩn PowerPoint.",
                Font = new Font("Segoe UI", 9f)
            };
            lblPreviewP2 = new Label {
                Dock = DockStyle.Top,
                Height = 45,
                Text = "Đoạn văn tiếp theo thể hiện khoảng cách giữa hai đoạn (Before/After Spacing) trực quan.",
                Font = new Font("Segoe UI", 9f)
            };
            pnlPreview.Controls.Add(lblPreviewP2);
            pnlPreview.Controls.Add(lblPreviewP1);
            grpPreview.Controls.Add(pnlPreview);
            Controls.Add(grpPreview);

            // Action Buttons
            Button btnOk = new Button { Text = "Đồng ý", DialogResult = DialogResult.OK, Location = new Point(320, 438), Size = new Size(80, 30), BackColor = Color.FromArgb(210, 71, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(410, 438), Size = new Size(80, 30), BackColor = Color.FromArgb(229, 231, 235), FlatStyle = FlatStyle.Flat };

            btnOk.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void UpdatePreview()
        {
            if (lblPreviewP1 == null || lblPreviewP2 == null) return;
            ContentAlignment align = ContentAlignment.TopLeft;
            if (AlignmentResult == TextAlignment.Center) align = ContentAlignment.TopCenter;
            else if (AlignmentResult == TextAlignment.Right) align = ContentAlignment.TopRight;

            lblPreviewP1.TextAlign = align;
            lblPreviewP2.TextAlign = align;
            lblPreviewP2.Margin = new Padding(0, (int)SpaceBeforeResult, 0, 0);
        }

        public void ApplyToShape(ShapeNode shape)
        {
            if (shape == null) return;
            shape.Alignment = AlignmentResult;
            shape.VerticalAlignment = VerticalAlignmentResult;
            shape.LineSpacing = LineSpacingResult;
            shape.MarginLeft = MarginLeftResult;
            shape.MarginRight = MarginRightResult;

            if (shape.TextRuns != null && shape.TextRuns.Count > 0)
            {
                foreach (var r in shape.TextRuns)
                {
                    r.ParaAlignment = AlignmentResult;
                    r.ParaLineSpacing = LineSpacingResult;
                    r.ParaSpaceBefore = SpaceBeforeResult;
                    r.ParaSpaceAfter = SpaceAfterResult;
                }
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class LayoutOptionsDialogForm : Form
    {
        public int WrapMode { get; set; } // 0=InLine, 1=Square, 2=Tight, 3=Through, 4=TopBottom, 5=Behind, 6=InFront
        public float DistanceTop { get; set; }
        public float DistanceBottom { get; set; }
        public float DistanceLeft { get; set; }
        public float DistanceRight { get; set; }
        public bool MoveWithText { get; set; }
        public bool AllowOverlap { get; set; }
        public float ShapeWidthInches { get; set; }
        public float ShapeHeightInches { get; set; }
        public float RotationAngle { get; set; }

        private Panel[] wrapPanels;
        private string[] wrapNames = new string[] { "In line with text", "Square", "Tight", "Through", "Top and bottom", "Behind text", "In front of text" };

        private NumericUpDown numDistTop;
        private NumericUpDown numDistBottom;
        private NumericUpDown numDistLeft;
        private NumericUpDown numDistRight;

        private NumericUpDown numShapeWidth;
        private NumericUpDown numShapeHeight;
        private NumericUpDown numRotation;

        public event Action OnApply;

        public LayoutOptionsDialogForm(DocShapeObject activeShape, int initialTabIndex = 0)
        {
            WrapMode = activeShape != null ? activeShape.WrapMode : 1;
            DistanceTop = 0f;
            DistanceBottom = 0f;
            DistanceLeft = 0.13f;
            DistanceRight = 0.13f;
            MoveWithText = true;
            AllowOverlap = true;
            ShapeWidthInches = activeShape != null ? (activeShape.BaseWidth + activeShape.WidthDelta) / 96f : 2.5f;
            ShapeHeightInches = activeShape != null ? (activeShape.BaseHeight + activeShape.HeightDelta) / 96f : 1.8f;
            RotationAngle = activeShape != null ? activeShape.RotationAngle : 0f;

            Text = "Bố cục đối tượng (Layout)";
            Size = new Size(500, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(243, 244, 246);
            Font = new Font("Segoe UI", 9f);

            TabControl tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(460, 375),
                Font = new Font("Segoe UI", 9f)
            };

            // ==================== TAB 1: POSITION ====================
            TabPage tabPos = new TabPage("Vị trí (Position)");
            tabPos.BackColor = Color.White;

            // Horizontal Group
            GroupBox grpHoriz = new GroupBox { Text = "Theo chiều ngang (Horizontal)", Location = new Point(10, 10), Size = new Size(430, 130) };
            RadioButton rdoHAlign = new RadioButton { Text = "Căn gióng (Alignment)", Location = new Point(15, 22), Size = new Size(150, 22), Checked = true };
            ComboBox cboHAlign = new ComboBox { Location = new Point(170, 20), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHAlign.Items.AddRange(new object[] { "Trái (Left)", "Giữa (Center)", "Phải (Right)" });
            cboHAlign.SelectedIndex = 0;
            Label lblHRel = new Label { Text = "so với", Location = new Point(275, 23), AutoSize = true };
            ComboBox cboHRel = new ComboBox { Location = new Point(320, 20), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHRel.Items.AddRange(new object[] { "Cột (Column)", "Lề (Margin)", "Trang (Page)" });
            cboHRel.SelectedIndex = 0;

            RadioButton rdoHAbs = new RadioButton { Text = "Vị trí tuyệt đối (Absolute)", Location = new Point(15, 54), Size = new Size(150, 22) };
            NumericUpDown numHAbs = new NumericUpDown { Location = new Point(170, 52), Size = new Size(95, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = -5m, Maximum = 20m, Value = 0m };
            Label lblHAbsRel = new Label { Text = "bên phải", Location = new Point(275, 55), AutoSize = true };
            ComboBox cboHAbsRel = new ComboBox { Location = new Point(320, 52), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHAbsRel.Items.AddRange(new object[] { "Cột (Column)", "Lề (Margin)", "Trang (Page)" });
            cboHAbsRel.SelectedIndex = 0;

            RadioButton rdoHRel = new RadioButton { Text = "Vị trí tương đối (Relative)", Location = new Point(15, 86), Size = new Size(150, 22) };
            NumericUpDown numHRelVal = new NumericUpDown { Location = new Point(170, 84), Size = new Size(95, 24), Minimum = 0, Maximum = 100, Value = 0 };
            Label lblHRelUnit = new Label { Text = "% so với", Location = new Point(275, 87), AutoSize = true };
            ComboBox cboHRelTo = new ComboBox { Location = new Point(320, 84), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHRelTo.Items.AddRange(new object[] { "Trang (Page)", "Lề (Margin)" });
            cboHRelTo.SelectedIndex = 0;

            grpHoriz.Controls.AddRange(new Control[] { rdoHAlign, cboHAlign, lblHRel, cboHRel, rdoHAbs, numHAbs, lblHAbsRel, cboHAbsRel, rdoHRel, numHRelVal, lblHRelUnit, cboHRelTo });
            tabPos.Controls.Add(grpHoriz);

            // Vertical Group
            GroupBox grpVert = new GroupBox { Text = "Theo chiều dọc (Vertical)", Location = new Point(10, 145), Size = new Size(430, 100) };
            RadioButton rdoVAlign = new RadioButton { Text = "Căn gióng (Alignment)", Location = new Point(15, 22), Size = new Size(150, 22), Checked = true };
            ComboBox cboVAlign = new ComboBox { Location = new Point(170, 20), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboVAlign.Items.AddRange(new object[] { "Trên (Top)", "Giữa (Middle)", "Dưới (Bottom)" });
            cboVAlign.SelectedIndex = 0;
            Label lblVRel = new Label { Text = "so với", Location = new Point(275, 23), AutoSize = true };
            ComboBox cboVRel = new ComboBox { Location = new Point(320, 20), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboVRel.Items.AddRange(new object[] { "Trang (Page)", "Lề (Margin)", "Đoạn (Paragraph)" });
            cboVRel.SelectedIndex = 0;

            RadioButton rdoVAbs = new RadioButton { Text = "Vị trí tuyệt đối (Absolute)", Location = new Point(15, 54), Size = new Size(150, 22) };
            NumericUpDown numVAbs = new NumericUpDown { Location = new Point(170, 52), Size = new Size(95, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = -5m, Maximum = 20m, Value = 0m };
            Label lblVAbsRel = new Label { Text = "bên dưới", Location = new Point(275, 55), AutoSize = true };
            ComboBox cboVAbsRel = new ComboBox { Location = new Point(320, 52), Size = new Size(95, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboVAbsRel.Items.AddRange(new object[] { "Đoạn (Paragraph)", "Lề (Margin)", "Trang (Page)" });
            cboVAbsRel.SelectedIndex = 0;

            grpVert.Controls.AddRange(new Control[] { rdoVAlign, cboVAlign, lblVRel, cboVRel, rdoVAbs, numVAbs, lblVAbsRel, cboVAbsRel });
            tabPos.Controls.Add(grpVert);

            // Options Group
            GroupBox grpOpt = new GroupBox { Text = "Tùy chọn khác (Options)", Location = new Point(10, 250), Size = new Size(430, 75) };
            CheckBox chkMoveText = new CheckBox { Text = "Di chuyển cùng văn bản (Move with text)", Location = new Point(15, 22), Size = new Size(200, 22), Checked = MoveWithText };
            CheckBox chkLockAnchor = new CheckBox { Text = "Khóa mỏ neo (Lock anchor)", Location = new Point(15, 46), Size = new Size(200, 22) };
            CheckBox chkOverlap = new CheckBox { Text = "Cho phép chồng lấn (Allow overlap)", Location = new Point(220, 22), Size = new Size(200, 22), Checked = AllowOverlap };
            CheckBox chkTableCell = new CheckBox { Text = "Bố cục trong ô bảng", Location = new Point(220, 46), Size = new Size(200, 22), Checked = true };
            grpOpt.Controls.AddRange(new Control[] { chkMoveText, chkLockAnchor, chkOverlap, chkTableCell });
            tabPos.Controls.Add(grpOpt);

            tabControl.TabPages.Add(tabPos);

            // ==================== TAB 2: TEXT WRAPPING ====================
            TabPage tabWrap = new TabPage("Ngắt dòng (Text Wrapping)");
            tabWrap.BackColor = Color.White;

            GroupBox grpWrapStyle = new GroupBox { Text = "Kiểu ngắt dòng (Wrapping style)", Location = new Point(10, 10), Size = new Size(430, 185) };
            wrapPanels = new Panel[7];

            int[] cardX = new int[] { 15, 100, 185, 270, 355, 15, 100 };
            int[] cardY = new int[] { 22, 22, 22, 22, 22, 102, 102 };

            for (int wi = 0; wi < 7; wi++)
            {
                int wIdx = wi;
                Panel wp = new Panel
                {
                    Location = new Point(cardX[wi], cardY[wi]),
                    Size = new Size(68, 70),
                    Cursor = Cursors.Hand
                };

                wp.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    bool isSel = (WrapMode == wIdx);
                    if (isSel)
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(224, 242, 254))) g.FillRectangle(b, 0, 0, 67, 69);
                        using (Pen p = new Pen(Color.FromArgb(2, 132, 199), 1.8f)) g.DrawRectangle(p, 0, 0, 67, 69);
                    }
                    else
                    {
                        using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1.0f)) g.DrawRectangle(p, 0, 0, 67, 69);
                    }

                    // Card inner icon box (36x36) at X=16, Y=6
                    int bx = 16;
                    int by = 6;
                    int bw = 36;
                    int bh = 36;
                    using (Pen p = new Pen(Color.FromArgb(148, 163, 184), 1.0f)) g.DrawRectangle(p, bx, by, bw, bh);

                    // Draw blue text lines & black arc
                    using (Pen bluePen = new Pen(Color.FromArgb(2, 132, 199), 1.2f))
                    using (Pen arcPen = new Pen(wIdx == 5 ? Color.FromArgb(148, 163, 184) : Color.FromArgb(15, 23, 42), 2.2f))
                    {
                        arcPen.StartCap = LineCap.Round;
                        arcPen.EndCap = LineCap.Round;

                        if (wIdx == 0) // In line
                        {
                            g.DrawLine(bluePen, bx + 4, by + 10, bx + bw - 4, by + 10);
                            g.DrawArc(arcPen, bx + 10, by + 14, 16, 14, 180, 180);
                            g.DrawLine(bluePen, bx + 4, by + 26, bx + bw - 4, by + 26);
                        }
                        else if (wIdx == 1) // Square
                        {
                            g.DrawLine(bluePen, bx + 4, by + 8, bx + bw - 4, by + 8);
                            g.DrawLine(bluePen, bx + 4, by + 16, bx + 10, by + 16);
                            g.DrawLine(bluePen, bx + bw - 10, by + 16, bx + bw - 4, by + 16);
                            g.DrawLine(bluePen, bx + 4, by + 24, bx + 10, by + 24);
                            g.DrawLine(bluePen, bx + bw - 10, by + 24, bx + bw - 4, by + 24);
                            g.DrawArc(arcPen, bx + 11, by + 14, 14, 14, 180, 180);
                            g.DrawLine(bluePen, bx + 4, by + 30, bx + bw - 4, by + 30);
                        }
                        else if (wIdx == 2 || wIdx == 3) // Tight / Through
                        {
                            g.DrawLine(bluePen, bx + 4, by + 8, bx + bw - 4, by + 8);
                            g.DrawLine(bluePen, bx + 4, by + 14, bx + 11, by + 14);
                            g.DrawLine(bluePen, bx + bw - 11, by + 14, bx + bw - 4, by + 14);
                            g.DrawLine(bluePen, bx + 4, by + 22, bx + 11, by + 22);
                            g.DrawLine(bluePen, bx + bw - 11, by + 22, bx + bw - 4, by + 22);
                            g.DrawArc(arcPen, bx + 10, by + 13, 16, 15, 180, 180);
                            g.DrawLine(bluePen, bx + 4, by + 29, bx + bw - 4, by + 29);
                        }
                        else if (wIdx == 4) // Top and bottom
                        {
                            g.DrawLine(bluePen, bx + 4, by + 8, bx + bw - 4, by + 8);
                            g.DrawArc(arcPen, bx + 10, by + 14, 16, 14, 180, 180);
                            g.DrawLine(bluePen, bx + 4, by + 28, bx + bw - 4, by + 28);
                        }
                        else if (wIdx == 5) // Behind text
                        {
                            g.DrawArc(arcPen, bx + 10, by + 14, 16, 14, 180, 180);
                            g.DrawLine(bluePen, bx + 4, by + 10, bx + bw - 4, by + 10);
                            g.DrawLine(bluePen, bx + 4, by + 18, bx + bw - 4, by + 18);
                            g.DrawLine(bluePen, bx + 4, by + 26, bx + bw - 4, by + 26);
                        }
                        else if (wIdx == 6) // In front of text
                        {
                            g.DrawLine(bluePen, bx + 4, by + 10, bx + bw - 4, by + 10);
                            g.DrawLine(bluePen, bx + 4, by + 18, bx + bw - 4, by + 18);
                            g.DrawLine(bluePen, bx + 4, by + 26, bx + bw - 4, by + 26);
                            g.DrawArc(arcPen, bx + 10, by + 14, 16, 14, 180, 180);
                        }
                    }

                    // Label
                    using (Font lf = new Font("Segoe UI", 7.0f, isSel ? FontStyle.Bold : FontStyle.Regular))
                    using (Brush lb = new SolidBrush(isSel ? Color.FromArgb(2, 132, 199) : Color.FromArgb(51, 65, 85)))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        Rectangle labelRect = new Rectangle(0, 44, 68, 24);
                        g.DrawString(wrapNames[wIdx], lf, lb, labelRect, sf);
                    }
                };

                wp.Click += (s, e) =>
                {
                    WrapMode = wIdx;
                    foreach (var p in wrapPanels) if (p != null) p.Invalidate();
                };

                wrapPanels[wi] = wp;
                grpWrapStyle.Controls.Add(wp);
            }
            tabWrap.Controls.Add(grpWrapStyle);

            // Distance from text Group
            GroupBox grpDist = new GroupBox { Text = "Khoảng cách đến văn bản (Distance from text)", Location = new Point(10, 205), Size = new Size(430, 110) };

            Label lblDTop = new Label { Text = "Trên (Top):", Location = new Point(20, 28), AutoSize = true };
            numDistTop = new NumericUpDown { Location = new Point(90, 25), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.05m, Minimum = 0m, Maximum = 5m, Value = (decimal)DistanceTop };
            Label lblDBot = new Label { Text = "Dưới (Bottom):", Location = new Point(20, 65), AutoSize = true };
            numDistBottom = new NumericUpDown { Location = new Point(90, 62), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.05m, Minimum = 0m, Maximum = 5m, Value = (decimal)DistanceBottom };

            Label lblDLeft = new Label { Text = "Trái (Left):", Location = new Point(220, 28), AutoSize = true };
            numDistLeft = new NumericUpDown { Location = new Point(290, 25), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.05m, Minimum = 0m, Maximum = 5m, Value = (decimal)DistanceLeft };
            Label lblDRight = new Label { Text = "Phải (Right):", Location = new Point(220, 65), AutoSize = true };
            numDistRight = new NumericUpDown { Location = new Point(290, 62), Size = new Size(70, 24), DecimalPlaces = 2, Increment = 0.05m, Minimum = 0m, Maximum = 5m, Value = (decimal)DistanceRight };

            grpDist.Controls.AddRange(new Control[] { lblDTop, numDistTop, lblDBot, numDistBottom, lblDLeft, numDistLeft, lblDRight, numDistRight });
            tabWrap.Controls.Add(grpDist);

            tabControl.TabPages.Add(tabWrap);

            // ==================== TAB 3: SIZE ====================
            TabPage tabSize = new TabPage("Kích thước (Size)");
            tabSize.BackColor = Color.White;

            GroupBox grpSize = new GroupBox { Text = "Kích thước và Xoay (Size and rotate)", Location = new Point(10, 10), Size = new Size(430, 130) };

            Label lblH = new Label { Text = "Chiều cao (Height):", Location = new Point(20, 30), AutoSize = true };
            numShapeHeight = new NumericUpDown { Location = new Point(150, 27), Size = new Size(80, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.2m, Maximum = 50m, Value = (decimal)ShapeHeightInches };

            Label lblW = new Label { Text = "Chiều rộng (Width):", Location = new Point(20, 65), AutoSize = true };
            numShapeWidth = new NumericUpDown { Location = new Point(150, 62), Size = new Size(80, 24), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.2m, Maximum = 50m, Value = (decimal)ShapeWidthInches };

            Label lblRot = new Label { Text = "Góc xoay (Rotation):", Location = new Point(20, 98), AutoSize = true };
            numRotation = new NumericUpDown { Location = new Point(150, 95), Size = new Size(80, 24), Minimum = 0, Maximum = 360, Value = (decimal)RotationAngle };

            grpSize.Controls.AddRange(new Control[] { lblH, numShapeHeight, lblW, numShapeWidth, lblRot, numRotation });
            tabSize.Controls.Add(grpSize);

            tabControl.TabPages.Add(tabSize);

            if (initialTabIndex >= 0 && initialTabIndex < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = initialTabIndex;
            }

            Controls.Add(tabControl);

            // Bottom Buttons: OK & Cancel
            Button btnOK = new Button
            {
                Text = "Đồng ý (OK)",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 398),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnOK.Click += (s, e) =>
            {
                DistanceTop = (float)numDistTop.Value;
                DistanceBottom = (float)numDistBottom.Value;
                DistanceLeft = (float)numDistLeft.Value;
                DistanceRight = (float)numDistRight.Value;
                ShapeWidthInches = (float)numShapeWidth.Value;
                ShapeHeightInches = (float)numShapeHeight.Value;
                RotationAngle = (float)numRotation.Value;
                if (OnApply != null) OnApply();
            };
            Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "Hủy bỏ (Cancel)",
                DialogResult = DialogResult.Cancel,
                Location = new Point(392, 398),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(btnCancel);
        }
    }
}

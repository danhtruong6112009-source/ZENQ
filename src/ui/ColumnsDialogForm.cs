using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class ColumnsDialogForm : Form
    {
        public int ColumnCount { get; set; }
        public float ColumnSpacingInches { get; set; }
        public bool LineBetween { get; set; }
        public string PresetType { get; set; }
        public string ApplyTo { get; set; }
        public bool EqualColumnWidth { get; set; }
        public float Col1WidthInches { get; set; }

        private NumericUpDown numColumns;
        private CheckBox chkLineBetween;
        private CheckBox chkEqualWidth;
        private NumericUpDown numCol1Width;
        private NumericUpDown numCol1Spacing;
        private ComboBox cboApplyTo;
        private PictureBox picPreview;
        private Panel[] presetPanels;
        private string[] presetNames = new string[] { "One", "Two", "Three", "Left", "Right" };

        public event Action OnApply;

        public ColumnsDialogForm(int curCols, float curSpacing, bool curLineBetween, string curPreset, bool curEqualWidth = true, float curCol1Width = 0f)
        {
            ColumnCount = curCols > 0 ? curCols : 1;
            ColumnSpacingInches = curSpacing > 0 ? curSpacing : 0.5f;
            LineBetween = curLineBetween;
            PresetType = string.IsNullOrEmpty(curPreset) ? (ColumnCount == 1 ? "One" : (ColumnCount == 2 ? "Two" : (ColumnCount == 3 ? "Three" : "One"))) : curPreset;
            ApplyTo = "Whole document";
            EqualColumnWidth = (PresetType == "Left" || PresetType == "Right") ? false : curEqualWidth;
            Col1WidthInches = curCol1Width > 0 ? curCol1Width : (ColumnCount >= 2 ? (float)Math.Round(Math.Max(1.0f, (6.27f - (ColumnCount - 1) * ColumnSpacingInches) / ColumnCount), 2) : 6.27f);

            Text = AppL10n.T("Chia cột (Columns)", "Columns");
            Size = new Size(468, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(243, 244, 246);
            Font = new Font("Segoe UI", 9f);

            // ==================== 1. PRESETS GROUP ====================
            GroupBox grpPresets = new GroupBox
            {
                Text = AppL10n.T("Mẫu có sẵn (Presets)", "Presets"),
                Location = new Point(12, 10),
                Size = new Size(428, 84),
                BackColor = Color.Transparent
            };

            presetPanels = new Panel[5];
            int pStartX = 28;
            int pGap = 78;

            for (int i = 0; i < 5; i++)
            {
                int pIdx = i;
                Panel pnl = new Panel
                {
                    Location = new Point(pStartX + i * pGap, 16),
                    Size = new Size(58, 56),
                    Cursor = Cursors.Hand
                };

                pnl.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    bool isSel = (PresetType == presetNames[pIdx]);
                    if (isSel)
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(224, 242, 254))) g.FillRectangle(b, 0, 0, 57, 55);
                        using (Pen p = new Pen(Color.FromArgb(2, 132, 199), 1.8f)) g.DrawRectangle(p, 0, 0, 57, 55);
                    }
                    else
                    {
                        using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1.0f)) g.DrawRectangle(p, 0, 0, 57, 55);
                    }

                    // Card icon (32x24) at X=13, Y=6
                    int cx = 13;
                    int cy = 6;
                    int cw = 32;
                    int ch = 24;
                    using (Pen p = new Pen(Color.FromArgb(100, 116, 139), 1.0f)) g.DrawRectangle(p, cx, cy, cw, ch);
                    using (SolidBrush textB = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    {
                        string pName = presetNames[pIdx];
                        if (pName == "One")
                        {
                            for (int ly = cy + 4; ly <= cy + ch - 4; ly += 4) g.FillRectangle(textB, cx + 3, ly, cw - 6, 2);
                        }
                        else if (pName == "Two")
                        {
                            int mid = cx + cw / 2;
                            for (int ly = cy + 4; ly <= cy + ch - 4; ly += 4)
                            {
                                g.FillRectangle(textB, cx + 3, ly, mid - cx - 5, 2);
                                g.FillRectangle(textB, mid + 2, ly, cx + cw - mid - 5, 2);
                            }
                        }
                        else if (pName == "Three")
                        {
                            int colW = (cw - 8) / 3;
                            for (int ly = cy + 4; ly <= cy + ch - 4; ly += 4)
                            {
                                g.FillRectangle(textB, cx + 2, ly, colW, 2);
                                g.FillRectangle(textB, cx + 2 + colW + 2, ly, colW, 2);
                                g.FillRectangle(textB, cx + 2 + 2 * (colW + 2), ly, colW, 2);
                            }
                        }
                        else if (pName == "Left")
                        {
                            for (int ly = cy + 4; ly <= cy + ch - 4; ly += 4)
                            {
                                g.FillRectangle(textB, cx + 3, ly, 6, 2);
                                g.FillRectangle(textB, cx + 11, ly, cw - 14, 2);
                            }
                        }
                        else if (pName == "Right")
                        {
                            for (int ly = cy + 4; ly <= cy + ch - 4; ly += 4)
                            {
                                g.FillRectangle(textB, cx + 3, ly, cw - 14, 2);
                                g.FillRectangle(textB, cx + cw - 9, ly, 6, 2);
                            }
                        }
                    }

                    // Label
                    using (Font lf = new Font("Segoe UI", 8.0f, isSel ? FontStyle.Bold : FontStyle.Regular))
                    using (Brush lb = new SolidBrush(isSel ? Color.FromArgb(2, 132, 199) : Color.FromArgb(51, 65, 85)))
                    {
                        SizeF sz = g.MeasureString(presetNames[pIdx], lf);
                        g.DrawString(presetNames[pIdx], lf, lb, (58 - sz.Width) / 2, 36);
                    }
                };

                pnl.Click += (s, e) =>
                {
                    PresetType = presetNames[pIdx];
                    if (PresetType == "One") numColumns.Value = 1;
                    else if (PresetType == "Two" || PresetType == "Left" || PresetType == "Right") numColumns.Value = 2;
                    else if (PresetType == "Three") numColumns.Value = 3;
                    if (chkEqualWidth != null)
                    {
                        chkEqualWidth.Checked = (PresetType != "Left" && PresetType != "Right");
                        if (numCol1Width != null) numCol1Width.Enabled = !chkEqualWidth.Checked && (int)numColumns.Value > 1;
                    }
                    foreach (var p in presetPanels) if (p != null) p.Invalidate();
                    if (picPreview != null) picPreview.Invalidate();
                };

                presetPanels[i] = pnl;
                grpPresets.Controls.Add(pnl);
            }

            Controls.Add(grpPresets);

            // ==================== 2. NUMBER OF COLUMNS & LINE BETWEEN ====================
            Label lblNum = new Label { Text = AppL10n.T("Số lượng cột:", "Number of columns:"), Location = new Point(14, 104), AutoSize = true };
            numColumns = new NumericUpDown { Location = new Point(106, 101), Size = new Size(54, 24), Minimum = 1, Maximum = 10, Value = ColumnCount };
            numColumns.ValueChanged += (s, e) =>
            {
                ColumnCount = (int)numColumns.Value;
                if (ColumnCount == 1) PresetType = "One";
                else if (ColumnCount == 2) { if (PresetType != "Left" && PresetType != "Right") PresetType = "Two"; }
                else if (ColumnCount == 3) PresetType = "Three";
                else PresetType = "Custom";
                if (numCol1Width != null && chkEqualWidth != null)
                {
                    numCol1Width.Enabled = !chkEqualWidth.Checked && ColumnCount > 1;
                }
                foreach (var p in presetPanels) if (p != null) p.Invalidate();
                if (picPreview != null) picPreview.Invalidate();
            };
            Controls.Add(lblNum);
            Controls.Add(numColumns);

            chkLineBetween = new CheckBox { Text = AppL10n.T("Đường kẻ giữa các cột (Line between)", "Line between"), Location = new Point(176, 103), AutoSize = true, Checked = LineBetween };
            chkLineBetween.CheckedChanged += (s, e) => { LineBetween = chkLineBetween.Checked; if (picPreview != null) picPreview.Invalidate(); };
            Controls.Add(chkLineBetween);

            // ==================== 3. WIDTH AND SPACING ====================
            GroupBox grpWidth = new GroupBox { Text = AppL10n.T("Độ rộng && Khoảng cách (in)", "Width && spacing (in)"), Location = new Point(12, 136), Size = new Size(276, 126) };
            Label lblColH = new Label { Text = AppL10n.T("Cột 1:", "Col #1:"), Location = new Point(12, 32), AutoSize = true };
            Label lblW = new Label { Text = AppL10n.T("Rộng:", "Width:"), Location = new Point(62, 32), AutoSize = true };
            numCol1Width = new NumericUpDown { Location = new Point(104, 29), Size = new Size(58, 22), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.5m, Maximum = 10.0m, Value = 6.5m };

            Label lblS = new Label { Text = AppL10n.T("Cách:", "Spacing:"), Location = new Point(170, 32), AutoSize = true };
            numCol1Spacing = new NumericUpDown { Location = new Point(210, 29), Size = new Size(54, 22), DecimalPlaces = 2, Increment = 0.1m, Minimum = 0.1m, Maximum = 3.0m, Value = (decimal)ColumnSpacingInches };
            numCol1Spacing.ValueChanged += (s, e) => { ColumnSpacingInches = (float)numCol1Spacing.Value; if (picPreview != null) picPreview.Invalidate(); };

            grpWidth.Controls.Add(lblColH);
            grpWidth.Controls.Add(lblW);
            grpWidth.Controls.Add(numCol1Width);
            grpWidth.Controls.Add(lblS);
            grpWidth.Controls.Add(numCol1Spacing);

            chkEqualWidth = new CheckBox { Text = AppL10n.T("Độ rộng cột bằng nhau (Equal width)", "Equal column width"), Location = new Point(12, 74), AutoSize = true, Checked = EqualColumnWidth };
            numCol1Width.Value = Math.Max(numCol1Width.Minimum, Math.Min(numCol1Width.Maximum, (decimal)Col1WidthInches));
            numCol1Width.Enabled = !chkEqualWidth.Checked && ColumnCount > 1;
            numCol1Width.ValueChanged += (s, e) => { Col1WidthInches = (float)numCol1Width.Value; if (picPreview != null) picPreview.Invalidate(); };
            chkEqualWidth.CheckedChanged += (s, e) =>
            {
                EqualColumnWidth = chkEqualWidth.Checked;
                numCol1Width.Enabled = !EqualColumnWidth && ColumnCount > 1;
                if (picPreview != null) picPreview.Invalidate();
            };
            grpWidth.Controls.Add(chkEqualWidth);

            Controls.Add(grpWidth);

            // ==================== 4. PREVIEW ====================
            GroupBox grpPrev = new GroupBox { Text = AppL10n.T("Xem trước", "Preview"), Location = new Point(296, 136), Size = new Size(144, 126) };
            picPreview = new PictureBox { Location = new Point(20, 22), Size = new Size(104, 94), BackColor = Color.White };
            picPreview.Paint += DrawPreview;
            grpPrev.Controls.Add(picPreview);
            Controls.Add(grpPrev);

            // ==================== 5. APPLY TO & BUTTONS ====================
            Label lblApply = new Label { Text = AppL10n.T("Áp dụng vào:", "Apply to:"), Location = new Point(14, 278), AutoSize = true };
            cboApplyTo = new ZenQOffice.UI.FluentComboBox { Location = new Point(106, 274), Size = new Size(240, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cboApplyTo.Items.AddRange(new object[] { AppL10n.T("Toàn bộ tài liệu (Whole document)", "Whole document"), AppL10n.T("Đoạn văn bản đã chọn", "Selected text"), AppL10n.T("Từ điểm này trở đi", "This point forward") });
            cboApplyTo.SelectedIndex = 0;
            Controls.Add(lblApply);
            Controls.Add(cboApplyTo);

            Button btnOK = new Button
            {
                Text = AppL10n.T("Đồng ý", "OK"),
                DialogResult = DialogResult.OK,
                Location = new Point(272, 316),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnOK.Click += (s, e) =>
            {
                ColumnCount = (int)numColumns.Value;
                ColumnSpacingInches = (float)numCol1Spacing.Value;
                LineBetween = chkLineBetween.Checked;
                EqualColumnWidth = chkEqualWidth.Checked;
                Col1WidthInches = EqualColumnWidth ? 0f : (float)numCol1Width.Value;
                ApplyTo = cboApplyTo.SelectedItem.ToString();
                if (OnApply != null) OnApply();
            };
            Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = AppL10n.T("Hủy bỏ", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(360, 316),
                Size = new Size(80, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(btnCancel);
        }

        private void DrawPreview(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = picPreview.Width - 1;
            int ph = picPreview.Height - 1;
            using (Pen borderPen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
            {
                g.DrawRectangle(borderPen, 0, 0, pw, ph);
            }

            // Top simulated lines
            using (Pen lightPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
            {
                g.DrawLine(lightPen, 10, 10, pw - 10, 10);
                g.DrawLine(lightPen, 10, 15, pw - 10, 15);
            }

            // Middle simulated columns with black filled bands
            int colCount = (int)numColumns.Value;
            int topY = 25;
            int botY = ph - 25;
            int startX = 10;
            int totalW = pw - 20;

            using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Pen sepPen = new Pen(Color.FromArgb(100, 116, 139), 1.0f))
            {
                if (colCount == 1)
                {
                    for (int ly = topY; ly <= botY; ly += 5) g.FillRectangle(darkBrush, startX, ly, totalW, 2);
                }
                else if (colCount == 2)
                {
                    if (PresetType == "Left")
                    {
                        int w1 = totalW / 3;
                        int w2 = totalW - w1 - 8;
                        for (int ly = topY; ly <= botY; ly += 5)
                        {
                            g.FillRectangle(darkBrush, startX, ly, w1, 2);
                            g.FillRectangle(darkBrush, startX + w1 + 8, ly, w2, 2);
                        }
                        if (chkLineBetween.Checked) g.DrawLine(sepPen, startX + w1 + 4, topY, startX + w1 + 4, botY + 2);
                    }
                    else if (PresetType == "Right")
                    {
                        int w2 = totalW / 3;
                        int w1 = totalW - w2 - 8;
                        for (int ly = topY; ly <= botY; ly += 5)
                        {
                            g.FillRectangle(darkBrush, startX, ly, w1, 2);
                            g.FillRectangle(darkBrush, startX + w1 + 8, ly, w2, 2);
                        }
                        if (chkLineBetween.Checked) g.DrawLine(sepPen, startX + w1 + 4, topY, startX + w1 + 4, botY + 2);
                    }
                    else
                    {
                        int cw = (totalW - 8) / 2;
                        for (int ly = topY; ly <= botY; ly += 5)
                        {
                            g.FillRectangle(darkBrush, startX, ly, cw, 2);
                            g.FillRectangle(darkBrush, startX + cw + 8, ly, cw, 2);
                        }
                        if (chkLineBetween.Checked) g.DrawLine(sepPen, startX + cw + 4, topY, startX + cw + 4, botY + 2);
                    }
                }
                else if (colCount >= 3)
                {
                    int cw = (totalW - (colCount - 1) * 6) / colCount;
                    for (int ly = topY; ly <= botY; ly += 5)
                    {
                        for (int c = 0; c < colCount; c++)
                        {
                            g.FillRectangle(darkBrush, startX + c * (cw + 6), ly, cw, 2);
                        }
                    }
                    if (chkLineBetween.Checked)
                    {
                        for (int c = 0; c < colCount - 1; c++)
                        {
                            int lx = startX + (c + 1) * (cw + 6) - 3;
                            g.DrawLine(sepPen, lx, topY, lx, botY + 2);
                        }
                    }
                }
            }

            // Bottom simulated lines
            using (Pen lightPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
            {
                g.DrawLine(lightPen, 10, ph - 15, pw - 10, ph - 15);
                g.DrawLine(lightPen, 10, ph - 10, pw - 10, ph - 10);
            }
        }
    }
}

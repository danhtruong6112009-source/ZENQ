using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class BordersShadingForm : Form
    {
        private WriterView writerView;
        private TabControl tabControl;
        private TabPage tabBorders;
        private TabPage tabShading;

        // Border settings
        private int currentSetting = 1; // 0=None, 1=Box, 2=Shadow, 3=3D, 4=Custom
        private float selectedWidthPt = 1.0f;
        private Color selectedColor = Color.Black;
        private DashStyle selectedDashStyle = DashStyle.Solid;
        private bool topBorder = true;
        private bool bottomBorder = true;
        private bool leftBorder = true;
        private bool rightBorder = true;

        // Controls
        private Panel previewPanel;
        private ComboBox cboWidth;
        private ComboBox cboApplyTo;
        private Button btnColor;
        private ListBox lstStyle;

        public BordersShadingForm(WriterView view)
        {
            writerView = view;
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Borders and Shading (Viền và Đổ màu nền)";
            Size = new Size(540, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.0f);
            BackColor = Color.FromArgb(245, 246, 248);

            tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(500, 380),
                Font = new Font("Segoe UI", 9.0f)
            };

            tabBorders = new TabPage("Borders (Viền)");
            tabShading = new TabPage("Shading (Tô nền)");

            SetupBordersTab();
            SetupShadingTab();

            tabControl.TabPages.Add(tabBorders);
            tabControl.TabPages.Add(tabShading);
            Controls.Add(tabControl);

            // Bottom Buttons: OK, Cancel
            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(340, 402),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => ApplyAndClose();

            Button btnCancel = new Button
            {
                Text = "Cancel (Hủy)",
                DialogResult = DialogResult.Cancel,
                Location = new Point(430, 402),
                Size = new Size(82, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.Click += (s, e) => Close();

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void SetupBordersTab()
        {
            tabBorders.BackColor = Color.White;

            // 1. Setting Group (None, Box, Shadow, 3-D, Custom)
            GroupBox grpSetting = new GroupBox
            {
                Text = "Setting (Kiểu viền)",
                Location = new Point(10, 10),
                Size = new Size(110, 320)
            };

            string[] settings = new string[] { "None (Không)", "Box (Hộp)", "Shadow (Bóng)", "3-D (Nổi)", "Custom (Tự chọn)" };
            int sy = 25;
            for (int i = 0; i < settings.Length; i++)
            {
                int idx = i;
                RadioButton rb = new RadioButton
                {
                    Text = settings[i],
                    Location = new Point(10, sy),
                    Size = new Size(95, 24),
                    Checked = (i == 1)
                };
                rb.CheckedChanged += (s, e) =>
                {
                    if (rb.Checked)
                    {
                        currentSetting = idx;
                        if (idx == 0) { topBorder = bottomBorder = leftBorder = rightBorder = false; }
                        else if (idx == 1 || idx == 2 || idx == 3) { topBorder = bottomBorder = leftBorder = rightBorder = true; }
                        previewPanel.Invalidate();
                    }
                };
                grpSetting.Controls.Add(rb);
                sy += 55;
            }
            tabBorders.Controls.Add(grpSetting);

            // 2. Style, Color, Width
            GroupBox grpStyle = new GroupBox
            {
                Text = "Style (Nét & Màu)",
                Location = new Point(130, 10),
                Size = new Size(150, 320)
            };

            Label lblStyle = new Label { Text = "Style:", Location = new Point(10, 20), AutoSize = true };
            lstStyle = new ListBox
            {
                Location = new Point(10, 38),
                Size = new Size(130, 100),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22
            };
            lstStyle.Items.Add("Solid (Liền nét)");
            lstStyle.Items.Add("Dashed (Nét đứt)");
            lstStyle.Items.Add("Dotted (Nét chấm)");
            lstStyle.SelectedIndex = 0;
            lstStyle.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                e.DrawBackground();
                using (Pen p = new Pen(e.ForeColor, 2))
                {
                    if (e.Index == 1) p.DashStyle = DashStyle.Dash;
                    else if (e.Index == 2) p.DashStyle = DashStyle.Dot;
                    int y = e.Bounds.Top + e.Bounds.Height / 2;
                    e.Graphics.DrawLine(p, e.Bounds.Left + 5, y, e.Bounds.Right - 5, y);
                }
            };
            lstStyle.SelectedIndexChanged += (s, e) =>
            {
                if (lstStyle.SelectedIndex == 1) selectedDashStyle = DashStyle.Dash;
                else if (lstStyle.SelectedIndex == 2) selectedDashStyle = DashStyle.Dot;
                else selectedDashStyle = DashStyle.Solid;
                previewPanel.Invalidate();
            };

            Label lblColor = new Label { Text = "Color (Màu):", Location = new Point(10, 145), AutoSize = true };
            btnColor = new Button
            {
                Location = new Point(10, 165),
                Size = new Size(130, 25),
                BackColor = Color.Black,
                Text = "■ Đen",
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = selectedColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        selectedColor = cd.Color;
                        btnColor.BackColor = selectedColor;
                        btnColor.ForeColor = (selectedColor.R * 0.3 + selectedColor.G * 0.59 + selectedColor.B * 0.11 > 128) ? Color.Black : Color.White;
                        btnColor.Text = "■ Chọn";
                        previewPanel.Invalidate();
                    }
                }
            };

            Label lblWidth = new Label { Text = "Width (Độ dày):", Location = new Point(10, 200), AutoSize = true };
            cboWidth = new ComboBox
            {
                Location = new Point(10, 220),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboWidth.Items.AddRange(new object[] { "1/4 pt (0.25 pt)", "1/2 pt (0.5 pt)", "3/4 pt (0.75 pt)", "1 pt (1.0 pt)", "1 1/2 pt (1.5 pt)", "2 1/4 pt (2.25 pt)", "3 pt (3.0 pt)", "4 1/2 pt (4.5 pt)", "6 pt (6.0 pt)" });
            cboWidth.SelectedIndex = 3;
            cboWidth.SelectedIndexChanged += (s, e) =>
            {
                float[] widths = new float[] { 0.25f, 0.5f, 0.75f, 1.0f, 1.5f, 2.25f, 3.0f, 4.5f, 6.0f };
                if (cboWidth.SelectedIndex >= 0 && cboWidth.SelectedIndex < widths.Length)
                {
                    selectedWidthPt = widths[cboWidth.SelectedIndex];
                }
                previewPanel.Invalidate();
            };

            grpStyle.Controls.AddRange(new Control[] { lblStyle, lstStyle, lblColor, btnColor, lblWidth, cboWidth });
            tabBorders.Controls.Add(grpStyle);

            // 3. Preview Group & Interactive Buttons
            GroupBox grpPreview = new GroupBox
            {
                Text = "Preview (Xem trước)",
                Location = new Point(290, 10),
                Size = new Size(195, 320)
            };

            previewPanel = new Panel
            {
                Location = new Point(45, 45),
                Size = new Size(110, 110),
                BackColor = Color.White
            };
            previewPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Preview paragraph dummy text lines
                using (Brush grayB = new SolidBrush(Color.FromArgb(230, 230, 230)))
                {
                    g.FillRectangle(grayB, 15, 25, 80, 8);
                    g.FillRectangle(grayB, 15, 40, 80, 8);
                    g.FillRectangle(grayB, 15, 55, 80, 8);
                    g.FillRectangle(grayB, 15, 70, 50, 8);
                }

                // Border Lines
                float penThick = Math.Max(1.0f, selectedWidthPt * 1.5f);
                using (Pen bPen = new Pen(selectedColor, penThick))
                {
                    bPen.DashStyle = selectedDashStyle;
                    if (topBorder) g.DrawLine(bPen, 10, 10, 100, 10);
                    if (bottomBorder) g.DrawLine(bPen, 10, 100, 100, 100);
                    if (leftBorder) g.DrawLine(bPen, 10, 10, 10, 100);
                    if (rightBorder) g.DrawLine(bPen, 100, 10, 100, 100);
                }
            };

            // Interactive Toggle Border Buttons around preview box
            Button btnTop = CreateBorderToggleBtn("▲ Top", new Point(75, 18), () => { topBorder = !topBorder; currentSetting = 4; previewPanel.Invalidate(); });
            Button btnBottom = CreateBorderToggleBtn("▼ Bot", new Point(75, 160), () => { bottomBorder = !bottomBorder; currentSetting = 4; previewPanel.Invalidate(); });
            Button btnLeft = CreateBorderToggleBtn("◄", new Point(15, 85), () => { leftBorder = !leftBorder; currentSetting = 4; previewPanel.Invalidate(); });
            Button btnRight = CreateBorderToggleBtn("►", new Point(160, 85), () => { rightBorder = !rightBorder; currentSetting = 4; previewPanel.Invalidate(); });

            Label lblApply = new Label { Text = "Apply to (Áp dụng cho):", Location = new Point(15, 215), AutoSize = true };
            cboApplyTo = new ComboBox
            {
                Location = new Point(15, 235),
                Size = new Size(165, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboApplyTo.Items.AddRange(new object[] { "Paragraph (Đoạn văn)", "Text (Văn bản chọn)" });
            bool hasSel = (writerView != null && writerView.SelectionStart != writerView.SelectionEnd);
            cboApplyTo.SelectedIndex = hasSel ? 1 : 0;

            grpPreview.Controls.AddRange(new Control[] { previewPanel, btnTop, btnBottom, btnLeft, btnRight, lblApply, cboApplyTo });
            tabBorders.Controls.Add(grpPreview);
        }

        private Button CreateBorderToggleBtn(string text, Point loc, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(50, 24),
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5f)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetupShadingTab()
        {
            tabShading.BackColor = Color.White;

            Label lblFill = new Label { Text = "Fill (Màu nền đoạn văn):", Location = new Point(20, 20), AutoSize = true };
            Panel pnlColorPalette = new Panel
            {
                Location = new Point(20, 50),
                Size = new Size(450, 220),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Standard Word Theme Color Palette Grid
            Color[] colors = new Color[]
            {
                Color.White, Color.Black, Color.FromArgb(231, 230, 230), Color.FromArgb(68, 84, 106), Color.FromArgb(91, 155, 213),
                Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0), Color.FromArgb(68, 114, 196), Color.FromArgb(112, 173, 71),
                Color.FromArgb(242, 242, 242), Color.FromArgb(127, 127, 127), Color.FromArgb(208, 206, 206), Color.FromArgb(214, 220, 228), Color.FromArgb(222, 235, 247),
                Color.FromArgb(251, 228, 213), Color.FromArgb(237, 237, 237), Color.FromArgb(255, 242, 204), Color.FromArgb(221, 235, 247), Color.FromArgb(226, 239, 218)
            };

            int cx = 15, cy = 15;
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];
                Button cBtn = new Button
                {
                    Location = new Point(cx, cy),
                    Size = new Size(36, 36),
                    BackColor = c,
                    FlatStyle = FlatStyle.Flat
                };
                cBtn.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
                cBtn.Click += (s, e) =>
                {
                    if (writerView != null)
                    {
                        writerView.ApplyFormatToSelection(null, -1, null, null, null, null, null, c);
                    }
                };
                pnlColorPalette.Controls.Add(cBtn);

                cx += 42;
                if ((i + 1) % 10 == 0)
                {
                    cx = 15;
                    cy += 42;
                }
            }

            tabShading.Controls.AddRange(new Control[] { lblFill, pnlColorPalette });
        }

        private void ApplyAndClose()
        {
            if (writerView != null)
            {
                writerView.SaveUndoState();
                bool applyToText = (cboApplyTo != null && cboApplyTo.SelectedIndex == 1);
                int selMin = Math.Min(writerView.SelectionStart, writerView.SelectionEnd);
                int selMax = Math.Max(writerView.SelectionStart, writerView.SelectionEnd);

                if (applyToText && selMax > selMin)
                {
                    string fullText = writerView.PieceTable != null ? writerView.PieceTable.GetText() : "";
                    while (selMax > selMin && selMax <= fullText.Length && (fullText[selMax - 1] == '\r' || fullText[selMax - 1] == '\n'))
                    {
                        selMax--;
                    }

                    int targetStart = selMin;
                    int targetLen = selMax - selMin;

                    if (writerView.TextBorders == null) writerView.TextBorders = new List<TextBorderRange>();
                    writerView.TextBorders.RemoveAll(tb => (tb.StartIndex + tb.Length > targetStart && tb.StartIndex < targetStart + targetLen));

                    var pStarts = writerView.GetSelectedOrCurrentParagraphStarts();
                    foreach (int pStart in pStarts)
                    {
                        if (writerView.ParagraphBorders.ContainsKey(pStart)) writerView.ParagraphBorders.Remove(pStart);
                    }

                    if (topBorder || bottomBorder || leftBorder || rightBorder)
                    {
                        writerView.TextBorders.Add(new TextBorderRange(targetStart, targetLen, topBorder, bottomBorder, leftBorder, rightBorder, selectedColor, selectedWidthPt));
                    }
                }
                else
                {
                    var pStarts = writerView.GetSelectedOrCurrentParagraphStarts();
                    foreach (int pStart in pStarts)
                    {
                        if (!topBorder && !bottomBorder && !leftBorder && !rightBorder)
                        {
                            if (writerView.ParagraphBorders.ContainsKey(pStart)) writerView.ParagraphBorders.Remove(pStart);
                        }
                        else
                        {
                            ParagraphBorderInfo info = new ParagraphBorderInfo
                            {
                                Top = topBorder,
                                Bottom = bottomBorder,
                                Left = leftBorder,
                                Right = rightBorder,
                                Color = selectedColor,
                                SizePt = selectedWidthPt,
                                IsHorizontalLine = false
                            };
                            writerView.ParagraphBorders[pStart] = info;
                        }
                    }
                }

                writerView.InvalidateLayoutAndTextCache();
                if (writerView.InvalidateCallback != null) writerView.InvalidateCallback();
            }
            Close();
        }
    }
}

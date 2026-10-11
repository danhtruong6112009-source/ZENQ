using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class DiagonalHeaderDialog : Form
    {
        private TextBox txtTopRight;
        private TextBox txtBottomLeft;
        private ComboBox cmbDiagonalType;
        private ComboBox cmbThickness;
        private Button btnPalette;
        private List<Button> swatchButtons = new List<Button>();
        private Color selectedColor = Color.FromArgb(30, 41, 59);
        private Panel pnlPreview;

        public string TopRightText { get { return txtTopRight != null ? txtTopRight.Text.Trim() : ""; } }
        public string BottomLeftText { get { return txtBottomLeft != null ? txtBottomLeft.Text.Trim() : ""; } }
        public bool IsDiagonalDown { get { return cmbDiagonalType != null && cmbDiagonalType.SelectedIndex == 0; } }
        public bool IsDiagonalUp { get { return cmbDiagonalType != null && cmbDiagonalType.SelectedIndex == 1; } }
        public int Thickness { get { return cmbThickness != null ? (cmbThickness.SelectedIndex + 1) : 1; } }
        public int LineColorArgb { get { return selectedColor.ToArgb(); } }
        public bool IsClearRequested { get; private set; }

        public DiagonalHeaderDialog(CellData existingCell = null)
        {
            if (existingCell != null && existingCell.Format != null && existingCell.Format.Borders != null && existingCell.Format.Borders.DiagonalColor != 0)
            {
                selectedColor = Color.FromArgb(existingCell.Format.Borders.DiagonalColor);
            }
            InitializeUI(existingCell);
        }

        private void InitializeUI(CellData existingCell)
        {
            this.Text = "Kẻ Chéo Ô & Tạo 2 Tiêu Đề";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(500, 450);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(16, 124, 65)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (Font hFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (Font subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                {
                    e.Graphics.DrawString("📐 Kẻ Đường Chéo & Chia Đôi Tiêu Đề Ô", hFont, Brushes.White, 14, 8);
                    e.Graphics.DrawString("Tạo ô tiêu đề góc bảng chia 2 phần (Cột bên trên, Hàng bên dưới)", subFont, Brushes.WhiteSmoke, 16, 28);
                }
            };
            this.Controls.Add(pnlHeader);

            int curY = 62;

            // Top-Right Title (Column Header)
            Label lblTR = new Label
            {
                Text = "1. Tiêu đề Cột (Góc Trên - Phải):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, curY),
                AutoSize = true
            };
            this.Controls.Add(lblTR);
            curY += 20;

            txtTopRight = new TextBox
            {
                Location = new Point(16, curY),
                Size = new Size(224, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Text = (existingCell != null && existingCell.Format != null && !string.IsNullOrEmpty(existingCell.Format.DiagonalTopRightText))
                    ? existingCell.Format.DiagonalTopRightText : "Tháng"
            };
            txtTopRight.TextChanged += (s, e) => pnlPreview.Invalidate();
            this.Controls.Add(txtTopRight);

            // Bottom-Left Title (Row Header)
            Label lblBL = new Label
            {
                Text = "2. Tiêu đề Hàng (Góc Dưới - Trái):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(250, curY - 20),
                AutoSize = true
            };
            this.Controls.Add(lblBL);

            txtBottomLeft = new TextBox
            {
                Location = new Point(250, curY),
                Size = new Size(220, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Text = (existingCell != null && existingCell.Format != null && !string.IsNullOrEmpty(existingCell.Format.DiagonalBottomLeftText))
                    ? existingCell.Format.DiagonalBottomLeftText : "Sản phẩm"
            };
            txtBottomLeft.TextChanged += (s, e) => pnlPreview.Invalidate();
            this.Controls.Add(txtBottomLeft);
            curY += 34;

            // Quick Presets
            Label lblPresets = new Label
            {
                Text = "Mẫu gợi ý nhanh:",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(16, curY + 3),
                AutoSize = true
            };
            this.Controls.Add(lblPresets);

            string[,] presets = new string[,] {
                { "Tháng", "Sản phẩm" },
                { "Môn học", "Học sinh" },
                { "Ngày", "Nội dung" },
                { "Thứ", "Tiết học" }
            };

            int prX = 114;
            for (int p = 0; p < 4; p++)
            {
                string tr = presets[p, 0];
                string bl = presets[p, 1];
                Button btnPreset = new Button
                {
                    Text = string.Format("{0} \\ {1}", tr, bl),
                    Size = new Size(84, 22),
                    Location = new Point(prX, curY),
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                    BackColor = Color.FromArgb(241, 245, 249),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnPreset.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnPreset.Click += (s, e) =>
                {
                    txtTopRight.Text = tr;
                    txtBottomLeft.Text = bl;
                    pnlPreview.Invalidate();
                };
                this.Controls.Add(btnPreset);
                prX += 88;
            }
            curY += 32;

            // Diagonal Type & Style
            Label lblType = new Label { Text = "Kiểu đường chéo:", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Location = new Point(16, curY), AutoSize = true };
            this.Controls.Add(lblType);

            cmbDiagonalType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(16, curY + 20),
                Size = new Size(224, 24)
            };
            cmbDiagonalType.Items.Add("╲ Đường chéo xuống (Chuẩn Excel)");
            cmbDiagonalType.Items.Add("╱ Đường chéo lên");
            cmbDiagonalType.SelectedIndex = (existingCell != null && existingCell.Format != null && existingCell.Format.Borders != null && existingCell.Format.Borders.DiagonalUp) ? 1 : 0;
            cmbDiagonalType.SelectedIndexChanged += (s, e) => pnlPreview.Invalidate();
            this.Controls.Add(cmbDiagonalType);

            Label lblThick = new Label { Text = "Độ dày nét kẻ:", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Location = new Point(250, curY), AutoSize = true };
            this.Controls.Add(lblThick);

            cmbThickness = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(250, curY + 20),
                Size = new Size(220, 24)
            };
            cmbThickness.Items.Add("Mảnh (1px)");
            cmbThickness.Items.Add("Vừa (2px)");
            cmbThickness.Items.Add("Dày (3px)");
            cmbThickness.SelectedIndex = (existingCell != null && existingCell.Format != null && existingCell.Format.Borders != null && existingCell.Format.Borders.DiagonalThickness > 0)
                ? Math.Min(2, existingCell.Format.Borders.DiagonalThickness - 1) : 0;
            cmbThickness.SelectedIndexChanged += (s, e) => pnlPreview.Invalidate();
            this.Controls.Add(cmbThickness);
            curY += 52;

            // Modern Color Palette Swatches (No old Windows modal dialog)
            Label lblColor = new Label { Text = "Màu nét kẻ:", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Location = new Point(16, curY), AutoSize = true };
            this.Controls.Add(lblColor);

            Color[] quickColors = new Color[]
            {
                Color.FromArgb(30, 41, 59),   // Dark Slate
                Color.FromArgb(100, 116, 139), // Slate Gray
                Color.FromArgb(16, 124, 65),  // Excel Green
                Color.FromArgb(37, 99, 235),  // Royal Blue
                Color.FromArgb(220, 38, 38),  // Red
                Color.FromArgb(234, 88, 12),  // Orange
                Color.FromArgb(124, 58, 237), // Purple
                Color.FromArgb(202, 138, 4)   // Gold
            };

            int swatchX = 16;
            for (int i = 0; i < quickColors.Length; i++)
            {
                Color c = quickColors[i];
                Button btnSw = new Button
                {
                    Size = new Size(28, 24),
                    Location = new Point(swatchX, curY + 20),
                    BackColor = c,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnSw.FlatAppearance.BorderSize = (c.ToArgb() == selectedColor.ToArgb()) ? 2 : 1;
                btnSw.FlatAppearance.BorderColor = (c.ToArgb() == selectedColor.ToArgb()) ? Color.Black : Color.FromArgb(203, 213, 225);
                btnSw.Click += (s, e) =>
                {
                    selectedColor = c;
                    UpdateSwatchHighlight();
                    pnlPreview.Invalidate();
                };
                swatchButtons.Add(btnSw);
                this.Controls.Add(btnSw);
                swatchX += 32;
            }

            btnPalette = new Button
            {
                Text = "Bảng màu ▾",
                Location = new Point(swatchX + 6, curY + 20),
                Size = new Size(110, 25),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnPalette.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnPalette.Click += (s, e) =>
            {
                Point pt = this.PointToScreen(new Point(btnPalette.Left, btnPalette.Bottom + 2));
                ColorPaletteForm colDlg = new ColorPaletteForm(pt, selectedColor, false, false);
                colDlg.OnColorSelected += (c) =>
                {
                    selectedColor = c;
                    UpdateSwatchHighlight();
                    pnlPreview.Invalidate();
                };
                colDlg.Show(this);
            };
            this.Controls.Add(btnPalette);
            curY += 54;

            // Live Preview Box
            Label lblPrev = new Label { Text = "Xem trước trực tiếp (Live Preview):", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Location = new Point(16, curY), AutoSize = true };
            this.Controls.Add(lblPrev);
            curY += 20;

            pnlPreview = new Panel
            {
                Location = new Point(16, curY),
                Size = new Size(454, 76),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlPreview.Paint += (s, e) => DrawPreview(e.Graphics, pnlPreview.ClientRectangle);
            this.Controls.Add(pnlPreview);

            // Bottom Action Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlBottom.Paint += (s, e) => e.Graphics.DrawLine(Pens.LightGray, 0, 0, pnlBottom.Width, 0);

            Button btnClear = new Button
            {
                Text = "Xóa gạch chéo",
                Size = new Size(110, 30),
                Location = new Point(16, 9),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
            btnClear.Click += (s, e) =>
            {
                IsClearRequested = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlBottom.Controls.Add(btnClear);

            Button btnOk = new Button
            {
                Text = "Áp dụng",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 30),
                Location = new Point(284, 9),
                BackColor = Color.FromArgb(16, 124, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            pnlBottom.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Size = new Size(76, 30),
                Location = new Point(382, 9),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            pnlBottom.Controls.Add(btnCancel);

            this.Controls.Add(pnlBottom);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void UpdateSwatchHighlight()
        {
            foreach (var b in swatchButtons)
            {
                bool isSel = (b.BackColor.ToArgb() == selectedColor.ToArgb());
                b.FlatAppearance.BorderSize = isSel ? 2 : 1;
                b.FlatAppearance.BorderColor = isSel ? Color.Black : Color.FromArgb(203, 213, 225);
            }
        }

        private void DrawPreview(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw cell border
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, bounds.Width - 1, bounds.Height - 1);
            }

            int thick = Thickness;
            using (Pen diagPen = new Pen(selectedColor, thick))
            {
                if (IsDiagonalDown)
                {
                    g.DrawLine(diagPen, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                }
                else if (IsDiagonalUp)
                {
                    g.DrawLine(diagPen, bounds.Left, bounds.Bottom, bounds.Right, bounds.Top);
                }
            }

            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (SolidBrush txtBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                // Top-Right Text
                string tr = txtTopRight.Text;
                if (!string.IsNullOrEmpty(tr))
                {
                    SizeF szTR = g.MeasureString(tr, f);
                    float xTR = bounds.Right - szTR.Width - 14;
                    float yTR = bounds.Top + 8;
                    g.DrawString(tr, f, txtBrush, xTR, yTR);
                }

                // Bottom-Left Text
                string bl = txtBottomLeft.Text;
                if (!string.IsNullOrEmpty(bl))
                {
                    SizeF szBL = g.MeasureString(bl, f);
                    float xBL = bounds.Left + 14;
                    float yBL = bounds.Bottom - szBL.Height - 8;
                    g.DrawString(bl, f, txtBrush, xBL, yBL);
                }
            }
        }
    }
}

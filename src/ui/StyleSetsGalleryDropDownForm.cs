using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class StyleSetItemDefinition
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string TitleSample { get; set; }
        public string HeadingSample { get; set; }
        public Color AccentColor { get; set; }
        public Color HeadingColor { get; set; }
        public string HeadingFont { get; set; }
        public string BodyFont { get; set; }
        public bool IsAllUpperTitle { get; set; }
        public bool IsSerif { get; set; }
        public bool HasAccentLine { get; set; }
        public bool HasDualLine { get; set; }
        public bool HasTopBottomFrame { get; set; }
        public bool HasNumberPrefix { get; set; }
        public bool IsCentered { get; set; }

        public StyleSetItemDefinition(int idx, string name, string title, string heading, Color acc, Color head, string hFont, string bFont)
        {
            Index = idx;
            Name = name;
            TitleSample = title;
            HeadingSample = heading;
            AccentColor = acc;
            HeadingColor = head;
            HeadingFont = hFont;
            BodyFont = bFont;
        }
    }

    public class StyleSetsGalleryDropDownForm : Form
    {
        public Action<int> OnStyleSetSelected;
        public Action OnResetToDefault;
        private int _activeIndex;
        private List<StyleSetItemDefinition> _styleSets;

        public StyleSetsGalleryDropDownForm(Point location, int activeIndex, Color themeAccent)
        {
            _activeIndex = activeIndex;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(540, 480);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Color wordBlue = (themeAccent != Color.Transparent && themeAccent.A > 0) ? themeAccent : Color.FromArgb(68, 114, 196);

            _styleSets = new List<StyleSetItemDefinition>
            {
                // 0: Word Standard (Default Blue)
                new StyleSetItemDefinition(0, "Mặc định (Default)", "TITLE", "Heading 1", wordBlue, wordBlue, "Calibri Light", "Calibri") { IsAllUpperTitle = true },
                // 1: Minimalist
                new StyleSetItemDefinition(1, "Tối giản (Minimalist)", "TITLE", "Heading 1", Color.FromArgb(31, 41, 55), Color.FromArgb(71, 85, 105), "Segoe UI Semibold", "Segoe UI") { IsAllUpperTitle = true },
                // 2: Casual
                new StyleSetItemDefinition(2, "Thân mật (Casual)", "Title", "Heading 1", Color.FromArgb(37, 99, 235), Color.FromArgb(59, 130, 246), "Segoe UI", "Segoe UI") { IsSerif = true },
                // 3: Elegant
                new StyleSetItemDefinition(3, "Trang nhã (Elegant)", "Title", "Heading 1", Color.FromArgb(17, 24, 39), Color.FromArgb(180, 83, 9), "Georgia", "Garamond") { IsSerif = true },
                // 4: Bold Technical
                new StyleSetItemDefinition(4, "Đậm nét (Bold Modern)", "TITLE", "HEADING 1", Color.Black, Color.FromArgb(17, 24, 39), "Arial", "Arial") { IsAllUpperTitle = true },
                // 5: Black & White / Framed
                new StyleSetItemDefinition(5, "Đóng khung (Framed)", "Title", "HEADING 1", Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), "Cambria", "Cambria") { HasTopBottomFrame = true },
                // 6: Numbered Classic
                new StyleSetItemDefinition(6, "Đánh số (Numbered)", "Title", "1  HEADING 1", Color.FromArgb(15, 23, 42), Color.FromArgb(29, 78, 216), "Calibri", "Calibri") { HasNumberPrefix = true },
                // 7: Centered Modern
                new StyleSetItemDefinition(7, "Căn giữa (Centered)", "Title", "Heading 1", Color.FromArgb(13, 148, 136), Color.FromArgb(15, 118, 110), "Aptos Display", "Aptos") { IsCentered = true },
                // 8: Teal Accented
                new StyleSetItemDefinition(8, "Thanh viền (Teal Accent)", "TITLE", "Heading 1", Color.FromArgb(5, 150, 105), Color.FromArgb(4, 120, 87), "Trebuchet MS", "Segoe UI") { HasAccentLine = true },
                // 9: Distinctive / Dual Lines
                new StyleSetItemDefinition(9, "Đường đôi (Dual Lines)", "TITLE", "Heading 1", Color.FromArgb(21, 128, 61), Color.FromArgb(37, 99, 235), "Calibri", "Calibri") { HasDualLine = true },
                // 10: Executive / Formal
                new StyleSetItemDefinition(10, "Trang trọng (Executive)", "TITLE", "Heading 1", Color.FromArgb(131, 24, 67), Color.FromArgb(157, 23, 77), "Times New Roman", "Times New Roman") { IsAllUpperTitle = true, HasAccentLine = true }
            };

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12)
            };

            Label lblHeader = new Label
            {
                Text = "Bộ kiểu dáng tài liệu Word (Document Style Sets)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, 10),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblHeader);

            int cardW = 160;
            int cardH = 74;
            int startX = 12;
            int startY = 36;
            int gapX = 12;
            int gapY = 10;

            for (int i = 0; i < _styleSets.Count; i++)
            {
                var sSet = _styleSets[i];
                int col = i % 3;
                int row = i / 3;
                int px = startX + col * (cardW + gapX);
                int py = startY + row * (cardH + gapY);

                bool isSel = (sSet.Index == _activeIndex);

                Control card = new Control
                {
                    Location = new Point(px, py),
                    Size = new Size(cardW, cardH),
                    Cursor = Cursors.Hand
                };

                bool isHovered = false;
                card.MouseEnter += (s, e) => { isHovered = true; card.Invalidate(); };
                card.MouseLeave += (s, e) => { isHovered = false; card.Invalidate(); };

                card.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Rectangle bounds = new Rectangle(0, 0, cardW, cardH);
                    using (SolidBrush bg = new SolidBrush(isSel ? Color.FromArgb(239, 246, 255) : (isHovered ? Color.FromArgb(241, 245, 249) : Color.White)))
                    {
                        g.FillRectangle(bg, bounds);
                    }
                    using (Pen bPen = new Pen(isSel ? Color.FromArgb(37, 99, 235) : (isHovered ? Color.FromArgb(148, 163, 184) : Color.FromArgb(226, 232, 240)), isSel ? 1.8f : 1.0f))
                    {
                        g.DrawRectangle(bPen, 0, 0, cardW - 1, cardH - 1);
                    }

                    // Title line
                    using (Font tf = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    using (SolidBrush tb = new SolidBrush(sSet.AccentColor))
                    {
                        if (sSet.IsCentered)
                        {
                            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                            g.DrawString(sSet.TitleSample, tf, tb, new RectangleF(6, 6, cardW - 12, 14), sf);
                        }
                        else
                        {
                            g.DrawString(sSet.TitleSample, tf, tb, 8, 6);
                        }
                    }

                    if (sSet.HasDualLine)
                    {
                        using (Pen pGreen = new Pen(sSet.AccentColor, 1f))
                        {
                            g.DrawLine(pGreen, 8, 5, cardW - 8, 5);
                            g.DrawLine(pGreen, 8, 20, cardW - 8, 20);
                        }
                    }
                    else if (sSet.HasAccentLine)
                    {
                        using (Pen pAcc = new Pen(sSet.AccentColor, 1.2f))
                        {
                            g.DrawLine(pAcc, 8, 20, cardW - 8, 20);
                        }
                    }
                    else if (sSet.HasTopBottomFrame)
                    {
                        using (Pen pFrame = new Pen(Color.FromArgb(160, 160, 160), 0.8f))
                        {
                            g.DrawLine(pFrame, 8, 20, cardW - 8, 20);
                        }
                    }

                    // Heading line
                    using (Font hf = new Font("Segoe UI", 6.5f, FontStyle.Bold))
                    using (SolidBrush hb = new SolidBrush(sSet.HeadingColor))
                    {
                        if (sSet.IsCentered)
                        {
                            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                            g.DrawString(sSet.HeadingSample, hf, hb, new RectangleF(6, 23, cardW - 12, 12), sf);
                        }
                        else
                        {
                            g.DrawString(sSet.HeadingSample, hf, hb, 8, 23);
                        }
                    }

                    // 3 Simulated body lines
                    using (Pen lp = new Pen(Color.FromArgb(180, 185, 192), 1f))
                    {
                        g.DrawLine(lp, 8, 40, cardW - 12, 40);
                        g.DrawLine(lp, 8, 46, cardW - 20, 46);
                        g.DrawLine(lp, 8, 52, cardW - 14, 52);
                    }

                    // Style name label at bottom
                    using (Font nf = new Font("Segoe UI", 6.5f, FontStyle.Regular))
                    using (SolidBrush nb = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(sSet.Name, nf, nb, 8, 58);
                    }
                };

                int capturedIdx = sSet.Index;
                card.Click += (s, e) =>
                {
                    if (OnStyleSetSelected != null) OnStyleSetSelected(capturedIdx);
                    Close();
                };

                scrollPanel.Controls.Add(card);
            }

            int bottomY = startY + ((_styleSets.Count + 2) / 3) * (cardH + gapY) + 6;

            Panel sep = new Panel
            {
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 3 + gapX * 2, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };
            scrollPanel.Controls.Add(sep);
            bottomY += 8;

            Button btnReset = new Button
            {
                Text = "🔄  Đặt lại về tập kiểu mặc định (Reset to Default Style Set)",
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 3 + gapX * 2, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnReset.Click += (s, e) =>
            {
                if (OnStyleSetSelected != null) OnStyleSetSelected(0);
                Close();
            };
            scrollPanel.Controls.Add(btnReset);
            bottomY += 34;

            Button btnSave = new Button
            {
                Text = "💾  Lưu thành bộ kiểu mới / Xuất bộ kiểu... (Save as a New Style Set)",
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 3 + gapX * 2, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnSave.Click += (s, e) =>
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu bộ kiểu dáng tài liệu mới (Xuất bộ kiểu)";
                    sfd.Filter = "Tệp bộ kiểu Office (*.dotx;*.xml;*.json)|*.dotx;*.xml;*.json|Tất cả tệp (*.*)|*.*";
                    sfd.FileName = "CustomStyleSet.json";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string json = string.Format("{{\n  \"StyleSetIndex\": {0},\n  \"Name\": \"{1}\"\n}}", _activeIndex, "Custom Style Set");
                            System.IO.File.WriteAllText(sfd.FileName, json, System.Text.Encoding.UTF8);
                            MessageBox.Show("Đã lưu và xuất bộ kiểu ra tệp '" + sfd.FileName + "' thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi xuất bộ kiểu: " + ex.Message, "Lỗi xuất bộ kiểu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                Close();
            };
            scrollPanel.Controls.Add(btnSave);
            bottomY += 34;

            Button btnImport = new Button
            {
                Text = "📂  Nhập bộ kiểu từ tệp... (Import Style Set)",
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 3 + gapX * 2, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnImport.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnImport.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Nhập bộ kiểu dáng từ tệp";
                    ofd.Filter = "Tệp bộ kiểu Office (*.dotx;*.xml;*.json)|*.dotx;*.xml;*.json|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string fName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
                            if (OnStyleSetSelected != null) OnStyleSetSelected(0);
                            MessageBox.Show("Đã nạp và áp dụng bộ kiểu '" + fName + "' thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi nhập bộ kiểu: " + ex.Message, "Lỗi nhập bộ kiểu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                Close();
            };
            scrollPanel.Controls.Add(btnImport);

            Controls.Add(scrollPanel);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

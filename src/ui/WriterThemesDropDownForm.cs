using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class WriterThemeItem
    {
        public string Name { get; set; }
        public string HeadingFont { get; set; }
        public string BodyFont { get; set; }
        public Color[] Palette { get; set; }

        public WriterThemeItem(string name, string hFont, string bFont, Color[] palette)
        {
            Name = name;
            HeadingFont = hFont;
            BodyFont = bFont;
            Palette = palette;
        }
    }

    public class WriterThemesDropDownForm : Form
    {
        public Action<WriterThemeItem> OnThemeSelected;
        private List<WriterThemeItem> _themes;
        private string _activeThemeName;

        public WriterThemesDropDownForm(Point location, string activeThemeName = "Chuẩn Word (Blue)")
        {
            _activeThemeName = activeThemeName ?? "Office";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(410, 480);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _themes = new List<WriterThemeItem>
            {
                new WriterThemeItem("Office", "Calibri Light", "Calibri", new Color[] {
                    Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0)
                }),
                new WriterThemeItem("Hiện đại (Modern)", "Segoe UI Semibold", "Segoe UI", new Color[] {
                    Color.FromArgb(37, 99, 235), Color.FromArgb(59, 130, 246), Color.FromArgb(147, 197, 253), Color.FromArgb(219, 234, 254)
                }),
                new WriterThemeItem("Trang trọng (Formal)", "Times New Roman", "Times New Roman", new Color[] {
                    Color.FromArgb(30, 58, 138), Color.FromArgb(30, 64, 175), Color.FromArgb(59, 130, 246), Color.FromArgb(147, 197, 253)
                }),
                new WriterThemeItem("Cổ điển (Classic)", "Georgia", "Garamond", new Color[] {
                    Color.FromArgb(120, 53, 15), Color.FromArgb(180, 83, 9), Color.FromArgb(217, 119, 6), Color.FromArgb(253, 230, 138)
                }),
                new WriterThemeItem("Ngọc lục bảo (Emerald)", "Trebuchet MS", "Segoe UI", new Color[] {
                    Color.FromArgb(6, 95, 70), Color.FromArgb(5, 150, 105), Color.FromArgb(16, 185, 129), Color.FromArgb(110, 231, 183)
                }),
                new WriterThemeItem("Hoàng hôn (Sunset)", "Century Gothic", "Calibri", new Color[] {
                    Color.FromArgb(154, 52, 18), Color.FromArgb(234, 88, 12), Color.FromArgb(249, 115, 22), Color.FromArgb(254, 215, 170)
                }),
                new WriterThemeItem("Hoàng gia (Royal Purple)", "Calibri", "Calibri", new Color[] {
                    Color.FromArgb(88, 28, 135), Color.FromArgb(124, 58, 237), Color.FromArgb(139, 92, 246), Color.FromArgb(221, 214, 254)
                }),
                new WriterThemeItem("Thạch phiến (Slate)", "Segoe UI", "Arial", new Color[] {
                    Color.FromArgb(30, 41, 59), Color.FromArgb(51, 65, 85), Color.FromArgb(100, 116, 139), Color.FromArgb(203, 213, 225)
                }),
                new WriterThemeItem("Đại dương (Teal Ocean)", "Aptos Display", "Aptos", new Color[] {
                    Color.FromArgb(19, 78, 74), Color.FromArgb(13, 148, 136), Color.FromArgb(20, 184, 166), Color.FromArgb(153, 246, 228)
                }),
                new WriterThemeItem("Đơn sắc (Monochrome)", "Arial", "Arial", new Color[] {
                    Color.FromArgb(17, 24, 39), Color.FromArgb(55, 65, 81), Color.FromArgb(107, 114, 128), Color.FromArgb(229, 231, 235)
                })
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
                Text = "Chủ đề tài liệu Word & Office (Themes)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, 10),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblHeader);

            int cardW = 180;
            int cardH = 68;
            int startX = 12;
            int startY = 36;
            int gapX = 10;
            int gapY = 8;

            for (int i = 0; i < _themes.Count; i++)
            {
                var theme = _themes[i];
                int row = i / 2;
                int col = i % 2;
                int px = startX + col * (cardW + gapX);
                int py = startY + row * (cardH + gapY);

                bool isSel = string.Equals(theme.Name, _activeThemeName, StringComparison.OrdinalIgnoreCase) ||
                             (_activeThemeName.StartsWith("Chuẩn") && theme.Name == "Office");

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
                    using (SolidBrush bgBrush = new SolidBrush(isSel ? Color.FromArgb(239, 246, 255) : (isHovered ? Color.FromArgb(241, 245, 249) : Color.White)))
                    {
                        g.FillRectangle(bgBrush, bounds);
                    }

                    using (Pen borderPen = new Pen(isSel ? Color.FromArgb(37, 99, 235) : (isHovered ? Color.FromArgb(148, 163, 184) : Color.FromArgb(226, 232, 240)), isSel ? 1.8f : 1.0f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, cardW - 1, cardH - 1);
                    }

                    // 4-Color Swatch Bar
                    int swatchX = 8;
                    int swatchY = 8;
                    for (int c = 0; c < 4; c++)
                    {
                        using (SolidBrush swBrush = new SolidBrush(theme.Palette[c]))
                        {
                            g.FillRectangle(swBrush, swatchX + c * 10, swatchY, 9, 20);
                        }
                    }

                    // Theme Name
                    using (Font titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    using (SolidBrush titleBrush = new SolidBrush(isSel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(30, 41, 59)))
                    {
                        g.DrawString(theme.Name, titleFont, titleBrush, 54, 8);
                    }

                    // Font Pairing preview
                    using (Font fontLabel = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                    using (SolidBrush fontBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(theme.HeadingFont + " / " + theme.BodyFont, fontLabel, fontBrush, 8, 38);
                    }
                };

                card.Click += (s, e) =>
                {
                    if (OnThemeSelected != null) OnThemeSelected(theme);
                    Close();
                };

                scrollPanel.Controls.Add(card);
            }

            // Bottom Actions (Reset, Browse/Import, Save/Export) matching Microsoft Word
            int bottomY = startY + ((_themes.Count + 1) / 2) * (cardH + gapY) + 4;

            // Separator
            Panel sepLine = new Panel
            {
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 2 + gapX, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };
            scrollPanel.Controls.Add(sepLine);
            bottomY += 8;

            Button btnReset = new Button
            {
                Text = "🔄  Đặt lại về chủ đề từ mẫu (Reset to Template Theme)",
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 2 + gapX, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnReset.Click += (s, e) =>
            {
                if (_themes.Count > 0 && OnThemeSelected != null)
                {
                    OnThemeSelected(_themes[0]);
                }
                Close();
            };
            scrollPanel.Controls.Add(btnReset);
            bottomY += 34;

            Button btnBrowse = new Button
            {
                Text = "📂  Duyệt tìm chủ đề / Nhập chủ đề từ tệp... (Browse for Themes)",
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 2 + gapX, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Duyệt tìm tệp chủ đề Office (Nhập chủ đề)";
                    ofd.Filter = "Tệp chủ đề Office (*.json;*.thmx)|*.json;*.thmx|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string content = System.IO.File.ReadAllText(ofd.FileName, System.Text.Encoding.UTF8);
                            string tName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
                            string hf = "Segoe UI Semibold";
                            string bf = "Segoe UI";
                            Color[] cols = new Color[] { Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0) };

                            if (content.Contains("\"HeadingFont\""))
                            {
                                int idx = content.IndexOf("\"HeadingFont\"");
                                int startQuote = content.IndexOf("\"", idx + 13) + 1;
                                int endQuote = content.IndexOf("\"", startQuote);
                                if (startQuote > 0 && endQuote > startQuote) hf = content.Substring(startQuote, endQuote - startQuote);
                            }
                            if (content.Contains("\"BodyFont\""))
                            {
                                int idx = content.IndexOf("\"BodyFont\"");
                                int startQuote = content.IndexOf("\"", idx + 10) + 1;
                                int endQuote = content.IndexOf("\"", startQuote);
                                if (startQuote > 0 && endQuote > startQuote) bf = content.Substring(startQuote, endQuote - startQuote);
                            }

                            var importedTheme = new WriterThemeItem(tName, hf, bf, cols);
                            if (OnThemeSelected != null) OnThemeSelected(importedTheme);
                            MessageBox.Show("Đã nhập và áp dụng chủ đề '" + tName + "' thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi nhập tệp chủ đề: " + ex.Message, "Lỗi nhập chủ đề", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                Close();
            };
            scrollPanel.Controls.Add(btnBrowse);
            bottomY += 34;

            Button btnSave = new Button
            {
                Text = "💾  Lưu chủ đề hiện tại / Xuất chủ đề... (Save Current Theme)",
                Location = new Point(12, bottomY),
                Size = new Size(cardW * 2 + gapX, 28),
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
                    sfd.Title = "Lưu chủ đề tài liệu hiện tại (Xuất chủ đề)";
                    sfd.Filter = "Tệp chủ đề Office (*.json)|*.json|Tệp Office Theme (*.thmx)|*.thmx";
                    sfd.FileName = (_activeThemeName ?? "CustomTheme") + ".json";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string json = string.Format("{{\n  \"Name\": \"{0}\",\n  \"HeadingFont\": \"Calibri Light\",\n  \"BodyFont\": \"Calibri\",\n  \"Colors\": [\"#4472C4\", \"#ED7D31\", \"#A5A5A5\", \"#FFC000\"]\n}}", _activeThemeName ?? "Office");
                            System.IO.File.WriteAllText(sfd.FileName, json, System.Text.Encoding.UTF8);
                            MessageBox.Show("Đã lưu và xuất chủ đề ra tệp '" + sfd.FileName + "' thành công!", "ZenQ Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi xuất tệp chủ đề: " + ex.Message, "Lỗi xuất chủ đề", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                Close();
            };
            scrollPanel.Controls.Add(btnSave);

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

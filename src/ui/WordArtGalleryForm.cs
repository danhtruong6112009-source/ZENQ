using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class WordArtGalleryForm : Form
    {
        public string WordArtText { get; private set; }
        public int SelectedStyleIndex { get; private set; }

        private TextBox txtInput;
        private int hoveredIndex = -1;
        private int selectedIndex = 0;

        private struct WordArtStyle
        {
            public Color FillColor1;
            public Color FillColor2;
            public Color OutlineColor;
            public bool HasShadow;
            public WordArtStyle(Color f1, Color f2, Color outColor, bool shadow)
            {
                FillColor1 = f1; FillColor2 = f2; OutlineColor = outColor; HasShadow = shadow;
            }
        }

        private static WordArtStyle[] styles = new WordArtStyle[]
        {
            new WordArtStyle(Color.Black, Color.FromArgb(60, 60, 60), Color.Transparent, true),
            new WordArtStyle(Color.FromArgb(0, 114, 198), Color.FromArgb(0, 50, 130), Color.Navy, true),
            new WordArtStyle(Color.FromArgb(216, 59, 1), Color.FromArgb(180, 20, 0), Color.DarkRed, true),
            new WordArtStyle(Color.FromArgb(16, 124, 65), Color.FromArgb(10, 80, 40), Color.DarkGreen, true),
            new WordArtStyle(Color.FromArgb(112, 48, 160), Color.FromArgb(60, 20, 100), Color.Purple, true),
            new WordArtStyle(Color.Gold, Color.Orange, Color.DarkOrange, true),
            new WordArtStyle(Color.White, Color.LightSkyBlue, Color.FromArgb(0, 120, 215), true),
            new WordArtStyle(Color.Silver, Color.DimGray, Color.Black, true),
            new WordArtStyle(Color.Crimson, Color.DeepPink, Color.Maroon, true),
            new WordArtStyle(Color.Teal, Color.DarkCyan, Color.DarkSlateGray, true),
            new WordArtStyle(Color.FromArgb(240, 150, 20), Color.FromArgb(200, 80, 10), Color.Brown, true),
            new WordArtStyle(Color.MediumPurple, Color.Indigo, Color.DarkSlateBlue, true),
            new WordArtStyle(Color.LimeGreen, Color.ForestGreen, Color.DarkGreen, true),
            new WordArtStyle(Color.DeepSkyBlue, Color.DodgerBlue, Color.MidnightBlue, true),
            new WordArtStyle(Color.Coral, Color.Tomato, Color.Firebrick, true)
        };

        public WordArtGalleryForm()
        {
            this.Text = "Chọn Kiểu Chữ Nghệ Thuật (WordArt)";
            this.Size = new Size(540, 390);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);

            Label lblText = new Label { Text = "Văn bản chữ nghệ thuật:", Location = new Point(20, 14), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            this.Controls.Add(lblText);

            txtInput = new TextBox { Text = "ZENQ WORDART", Location = new Point(20, 36), Width = 485, Font = new Font("Segoe UI", 10.0f, FontStyle.Bold) };
            this.Controls.Add(txtInput);

            Label lblPick = new Label { Text = "Chọn phong cách nghệ thuật (Click để chọn):", Location = new Point(20, 72), AutoSize = true };
            this.Controls.Add(lblPick);

            Panel gridPanel = new Panel { Location = new Point(20, 95), Size = new Size(485, 200), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            gridPanel.Paint += GridPanel_Paint;
            gridPanel.MouseMove += (s, e) =>
            {
                int c = e.X / 96;
                int r = e.Y / 65;
                int idx = r * 5 + c;
                if (idx >= 0 && idx < 15 && idx != hoveredIndex)
                {
                    hoveredIndex = idx;
                    gridPanel.Invalidate();
                }
            };
            gridPanel.MouseLeave += (s, e) => { hoveredIndex = -1; gridPanel.Invalidate(); };
            gridPanel.MouseClick += (s, e) =>
            {
                int c = e.X / 96;
                int r = e.Y / 65;
                int idx = r * 5 + c;
                if (idx >= 0 && idx < 15)
                {
                    selectedIndex = idx;
                    gridPanel.Invalidate();
                }
            };
            this.Controls.Add(gridPanel);

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn WordArt",
                Location = new Point(325, 10),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                WordArtText = string.IsNullOrEmpty(txtInput.Text.Trim()) ? "WORDART" : txtInput.Text.Trim();
                SelectedStyleIndex = selectedIndex;
            };
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(430, 10),
                Size = new Size(75, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            for (int i = 0; i < 15; i++)
            {
                int r = i / 5;
                int c = i % 5;
                Rectangle box = new Rectangle(c * 96 + 4, r * 65 + 4, 88, 56);

                if (i == selectedIndex)
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(220, 238, 255))) g.FillRectangle(sb, box);
                    g.DrawRectangle(new Pen(Color.FromArgb(0, 120, 215), 1.8f), box);
                }
                else if (i == hoveredIndex)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(240, 245, 255))) g.FillRectangle(hb, box);
                    g.DrawRectangle(Pens.SteelBlue, box);
                }
                else
                {
                    g.DrawRectangle(Pens.LightGray, box);
                }

                var st = styles[i];
                using (Font af = new Font("Arial Black", 18.0f, FontStyle.Bold))
                {
                    if (st.HasShadow)
                    {
                        g.DrawString("A", af, new SolidBrush(Color.FromArgb(50, 0, 0, 0)), box.X + 32, box.Y + 12);
                    }
                    using (LinearGradientBrush lgb = new LinearGradientBrush(box, st.FillColor1, st.FillColor2, LinearGradientMode.Vertical))
                    {
                        g.DrawString("A", af, lgb, box.X + 30, box.Y + 10);
                    }
                    if (st.OutlineColor != Color.Transparent)
                    {
                        // Accent line underneath
                        g.DrawLine(new Pen(st.OutlineColor, 2f), box.X + 24, box.Bottom - 10, box.Right - 24, box.Bottom - 10);
                    }
                }
            }
        }
    }
}

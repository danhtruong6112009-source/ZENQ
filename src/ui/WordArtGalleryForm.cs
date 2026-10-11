using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class WordArtGalleryForm : Form
    {
        public string WordArtText { get; private set; }
        public int SelectedStyleIndex { get; private set; }

        private TextBox txtInput;
        private int hoveredIndex = -1;
        private int selectedIndex = 0;

        public class WordArtPreset
        {
            public Color FillColor1 { get; set; }
            public Color FillColor2 { get; set; }
            public bool HasGradient { get; set; }
            public float GradientAngle { get; set; }
            public Color OutlineColor { get; set; }
            public float OutlineWidth { get; set; }
            public bool HasShadow { get; set; }
            public Color ShadowColor { get; set; }
            public string FontName { get; set; }

            public WordArtPreset(Color f1, Color f2, bool hasGrad, float angle, Color outCol, float outW, bool shadow, Color shadowCol, string font = "Segoe UI Black")
            {
                FillColor1 = f1;
                FillColor2 = f2;
                HasGradient = hasGrad;
                GradientAngle = angle;
                OutlineColor = outCol;
                OutlineWidth = outW;
                HasShadow = shadow;
                ShadowColor = shadowCol;
                FontName = font;
            }
        }

        private static readonly WordArtPreset[] presets = new WordArtPreset[]
        {
            // 0: Classic Charcoal Black with soft drop shadow
            new WordArtPreset(Color.FromArgb(30, 41, 59), Color.FromArgb(15, 23, 42), false, 90f, Color.Transparent, 0f, true, Color.FromArgb(70, 0, 0, 0)),
            
            // 1: Modern Deep Blue with subtle glow
            new WordArtPreset(Color.FromArgb(2, 132, 199), Color.FromArgb(3, 105, 161), true, 90f, Color.FromArgb(7, 89, 133), 1.6f, true, Color.FromArgb(50, 2, 132, 199)),
            
            // 2: Warm Amber / Sunset Orange Gradient with Dark Maroon Outline
            new WordArtPreset(Color.FromArgb(251, 146, 60), Color.FromArgb(234, 88, 12), true, 90f, Color.FromArgb(124, 45, 18), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 3: Emerald Green Gradient with Deep Green Outline
            new WordArtPreset(Color.FromArgb(52, 211, 153), Color.FromArgb(5, 150, 105), true, 90f, Color.FromArgb(6, 78, 59), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 4: Royal Purple Gradient with Dark Violet Outline
            new WordArtPreset(Color.FromArgb(192, 132, 252), Color.FromArgb(126, 34, 206), true, 90f, Color.FromArgb(59, 7, 100), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 5: Metallic Pure Gold (Yellow to Amber) with Rich Dark Gold Outline
            new WordArtPreset(Color.FromArgb(253, 224, 71), Color.FromArgb(217, 119, 6), true, 90f, Color.FromArgb(120, 53, 15), 2.2f, true, Color.FromArgb(70, 120, 53, 15)),
            
            // 6: Iconic PowerPoint White Fill with Crisp Deep Blue Outline & Blue Shadow
            new WordArtPreset(Color.White, Color.White, false, 90f, Color.FromArgb(0, 120, 212), 2.4f, true, Color.FromArgb(60, 0, 120, 212)),
            
            // 7: Titanium Chrome / Platinum Gradient with Charcoal Outline
            new WordArtPreset(Color.FromArgb(248, 250, 252), Color.FromArgb(148, 163, 184), true, 90f, Color.FromArgb(51, 65, 85), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 8: Ruby Red Gradient with Dark Maroon Outline
            new WordArtPreset(Color.FromArgb(244, 63, 94), Color.FromArgb(190, 18, 60), true, 90f, Color.FromArgb(76, 5, 25), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 9: Electric Neon Cyan / Aqua with Dark Navy Outline
            new WordArtPreset(Color.FromArgb(6, 182, 212), Color.FromArgb(2, 132, 199), true, 90f, Color.FromArgb(15, 23, 42), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 10: Bright Sun Gold with Chocolate Outline & Deep Offset Shadow
            new WordArtPreset(Color.FromArgb(251, 191, 36), Color.FromArgb(245, 158, 11), true, 90f, Color.FromArgb(69, 26, 3), 2.2f, true, Color.FromArgb(80, 0, 0, 0)),
            
            // 11: Classic Poster White Fill with Crisp Charcoal Outline & Drop Shadow
            new WordArtPreset(Color.White, Color.White, false, 90f, Color.FromArgb(30, 41, 59), 2.4f, true, Color.FromArgb(80, 0, 0, 0)),
            
            // 12: Sunset Rose / Pink Coral Gradient with Burgundy Outline
            new WordArtPreset(Color.FromArgb(253, 164, 175), Color.FromArgb(251, 113, 133), true, 90f, Color.FromArgb(136, 19, 55), 2.0f, true, Color.FromArgb(60, 0, 0, 0)),
            
            // 13: Crystal Ice Blue Gradient with Navy Outline
            new WordArtPreset(Color.FromArgb(224, 242, 254), Color.FromArgb(125, 211, 252), true, 90f, Color.FromArgb(3, 105, 161), 2.0f, true, Color.FromArgb(50, 3, 105, 161)),
            
            // 14: Electric Lime Tech Gradient with Dark Forest Outline
            new WordArtPreset(Color.FromArgb(163, 230, 53), Color.FromArgb(101, 163, 13), true, 90f, Color.FromArgb(26, 46, 5), 2.0f, true, Color.FromArgb(60, 0, 0, 0))
        };

        public static WordArtPreset GetPreset(int index)
        {
            if (index >= 0 && index < presets.Length) return presets[index];
            return presets[0];
        }

        private class WordArtDoubleBufferedPanel : Panel
        {
            public WordArtDoubleBufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }

        private WordArtDoubleBufferedPanel gridPanel;

        public WordArtGalleryForm()
        {
            this.Text = "Kiểu Chữ Nghệ Thuật (WordArt - Microsoft 365)";
            this.Size = new Size(540, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9.0f);
            this.DoubleBuffered = true;

            Label lblText = new Label
            {
                Text = "Văn bản chữ nghệ thuật:",
                Location = new Point(20, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            this.Controls.Add(lblText);

            txtInput = new TextBox
            {
                Text = "ZENQ WORDART",
                Location = new Point(20, 36),
                Width = 485,
                Font = new Font("Segoe UI", 10.0f, FontStyle.Bold)
            };
            this.Controls.Add(txtInput);

            Label lblPick = new Label
            {
                Text = "Chọn phong cách WordArt (Nhấp đúp hoặc bấm Chèn để áp dụng):",
                Location = new Point(20, 72),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            this.Controls.Add(lblPick);

            gridPanel = new WordArtDoubleBufferedPanel
            {
                Location = new Point(20, 95),
                Size = new Size(485, 210),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            gridPanel.Paint += GridPanel_Paint;
            gridPanel.MouseMove += GridPanel_MouseMove;
            gridPanel.MouseLeave += (s, e) => { if (hoveredIndex != -1) { hoveredIndex = -1; gridPanel.Invalidate(); } };
            gridPanel.MouseClick += GridPanel_MouseClick;
            gridPanel.MouseDoubleClick += (s, e) =>
            {
                ConfirmAndClose();
            };
            this.Controls.Add(gridPanel);

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 242, 245) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "Chèn WordArt",
                Location = new Point(310, 10),
                Size = new Size(115, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => ConfirmAndClose();
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(435, 10),
                Size = new Size(70, 28),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void ConfirmAndClose()
        {
            WordArtText = string.IsNullOrEmpty(txtInput.Text.Trim()) ? "ZENQ WORDART" : txtInput.Text.Trim();
            SelectedStyleIndex = selectedIndex;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void GridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            int c = e.X / 96;
            int r = e.Y / 68;
            int idx = -1;
            if (c >= 0 && c < 5 && r >= 0 && r < 3)
            {
                idx = r * 5 + c;
                if (idx >= presets.Length) idx = -1;
            }

            if (idx != hoveredIndex)
            {
                hoveredIndex = idx;
                gridPanel.Invalidate();
            }
        }

        private void GridPanel_MouseClick(object sender, MouseEventArgs e)
        {
            int c = e.X / 96;
            int r = e.Y / 68;
            if (c >= 0 && c < 5 && r >= 0 && r < 3)
            {
                int idx = r * 5 + c;
                if (idx >= 0 && idx < presets.Length)
                {
                    selectedIndex = idx;
                    gridPanel.Invalidate();
                }
            }
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            for (int i = 0; i < presets.Length; i++)
            {
                int r = i / 5;
                int c = i % 5;
                Rectangle box = new Rectangle(c * 96 + 5, r * 68 + 5, 86, 58);

                bool isSel = (i == selectedIndex);
                bool isHov = (i == hoveredIndex);

                if (isSel)
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(224, 242, 254))) g.FillRectangle(sb, box);
                    using (Pen p = new Pen(Color.FromArgb(2, 132, 199), 2.0f)) g.DrawRectangle(p, box);
                }
                else if (isHov)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249))) g.FillRectangle(hb, box);
                    using (Pen p = new Pen(Color.FromArgb(148, 163, 184), 1.2f)) g.DrawRectangle(p, box);
                }
                else
                {
                    using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1.0f)) g.DrawRectangle(p, box);
                }

                var st = presets[i];
                DrawWordArtLetter(g, box, st);
            }
        }

        private void DrawWordArtLetter(Graphics g, Rectangle box, WordArtPreset st)
        {
            float emSize = 26f;
            using (Font font = new Font(st.FontName ?? "Segoe UI Black", emSize, FontStyle.Bold))
            using (GraphicsPath path = new GraphicsPath())
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                RectangleF textBounds = new RectangleF(box.X, box.Y - 2, box.Width, box.Height);
                path.AddString("A", font.FontFamily, (int)font.Style, emSize * g.DpiY / 72.0f, textBounds, sf);

                // 1. Shadow
                if (st.HasShadow)
                {
                    using (Matrix sm = new Matrix())
                    {
                        sm.Translate(2.2f, 2.2f);
                        using (GraphicsPath shadowPath = (GraphicsPath)path.Clone())
                        {
                            shadowPath.Transform(sm);
                            using (SolidBrush shBrush = new SolidBrush(st.ShadowColor))
                            {
                                g.FillPath(shBrush, shadowPath);
                            }
                        }
                    }
                }

                // 2. Outline Stroke
                if (st.OutlineColor != Color.Transparent && st.OutlineWidth > 0f)
                {
                    using (Pen outPen = new Pen(st.OutlineColor, st.OutlineWidth))
                    {
                        outPen.LineJoin = LineJoin.Round;
                        g.DrawPath(outPen, path);
                    }
                }

                // 3. Fill (Gradient or Solid)
                if (st.HasGradient)
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(box, st.FillColor1, st.FillColor2, st.GradientAngle))
                    {
                        g.FillPath(lgb, path);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(st.FillColor1))
                    {
                        g.FillPath(b, path);
                    }
                }
            }
        }
    }
}

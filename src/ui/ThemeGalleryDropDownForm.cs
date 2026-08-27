using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class ThemeGalleryDropDownForm : Form
    {
        public event Action<string> OnThemeSelected;
        private PresentationRoot _presentation;

        public ThemeGalleryDropDownForm(Point screenLocation, PresentationRoot presentation)
        {
            _presentation = presentation;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(520, 560);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10)
            };

            int curY = 10;

            // 1. Lát Cắt & Hình Học Không Gian (Geometry & Slices)
            AddSection(scrollPanel, ref curY, "📐 Lát Cắt & Hình Học Chuẩn PowerPoint (Slices & Geometry)", new string[] {
                "Facet", "Slice", "Bauhaus", "Ion"
            });

            // 2. Sóng Lụa & Công Nghệ Tối (Glowing Waves & Dark Luxury)
            AddSection(scrollPanel, ref curY, "🌌 Sóng Lụa Quang Phổ & Công Nghệ Tối (Cyber Waves & Dark Luxury)", new string[] {
                "Cyber Wave", "Cyber Neon", "Gold Luxury", "Warm Sunset"
            });

            // 3. Cây Cỏ & Tự Nhiên (Botanical & Floral Art)
            AddSection(scrollPanel, ref curY, "🌿 Cây Cỏ, Hoa Lá & Thảo Mộc (Botanical & Nature)", new string[] {
                "Organic", "Floral Pastel", "Damask", "Watercolor"
            });

            // 4. Doanh Nghiệp & Cơ Bản (Corporate & Minimalist)
            AddSection(scrollPanel, ref curY, "🏢 Doanh Nghiệp & Cơ Bản (Corporate & Minimalist)", new string[] {
                "Blank", "Office", "Wisp", "Celestial", "Slate"
            });

            Controls.Add(scrollPanel);

            Deactivate += (s, e) => Close();
        }

        private void AddSection(Panel parent, ref int curY, string headerText, string[] themeNames)
        {
            Label lbl = new Label
            {
                Text = headerText,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(10, curY),
                AutoSize = true
            };
            parent.Controls.Add(lbl);
            curY += 24;

            int col = 0;
            int cardW = 114;
            int cardH = 76;
            int gapX = 10;
            int gapY = 10;
            int startX = 10;

            for (int i = 0; i < themeNames.Length; i++)
            {
                string tName = themeNames[i];
                PresentationTheme theme = _presentation != null && _presentation.Themes != null
                    ? _presentation.Themes.Find(t => t.Name == tName)
                    : null;

                if (theme == null) continue;

                int posX = startX + col * (cardW + gapX);
                int posY = curY;

                bool isSel = (_presentation != null && _presentation.CurrentTheme == theme.Name);

                ThemeCardControl card = new ThemeCardControl(theme, isSel)
                {
                    Location = new Point(posX, posY),
                    Size = new Size(cardW, cardH),
                    Cursor = Cursors.Hand
                };

                card.Click += (s, e) =>
                {
                    if (OnThemeSelected != null) OnThemeSelected(theme.Name);
                    Close();
                };

                parent.Controls.Add(card);

                col++;
                if (col >= 4)
                {
                    col = 0;
                    curY += cardH + gapY;
                }
            }

            if (col != 0)
            {
                curY += cardH + gapY;
            }

            curY += 10;
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

    public class ThemeCardControl : Control
    {
        private PresentationTheme _theme;
        private bool _isSelected;
        private bool _isHovered;

        public ThemeCardControl(PresentationTheme theme, bool isSelected)
        {
            _theme = theme;
            _isSelected = isSelected;
            DoubleBuffered = true;

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            RibbonBar.DrawThemeMiniCardPreview(g, rect, _theme, _isSelected, _isHovered);
        }
    }
}

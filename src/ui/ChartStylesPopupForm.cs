using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ChartStylesPopupForm : Form
    {
        private DocChartData chartData;
        private Action onStateChanged;

        private Panel pnlStyle;
        private Panel pnlColor;

        public ChartStylesPopupForm(Point screenPos, DocChartData data, Action onChange)
        {
            this.chartData = data;
            this.onStateChanged = onChange;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = screenPos;
            this.Size = new Size(210, 320);
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9.0f);

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Top Tab Buttons: Style | Color
            Panel pnlTabs = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(248, 250, 252) };
            Button btnStyle = new Button
            {
                Text = "Style",
                Location = new Point(8, 4),
                Size = new Size(92, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                BackColor = Color.White
            };
            btnStyle.FlatAppearance.BorderColor = Color.FromArgb(16, 124, 65);

            Button btnColor = new Button
            {
                Text = "Color",
                Location = new Point(106, 4),
                Size = new Size(92, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            btnColor.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);

            btnStyle.Click += (s, e) =>
            {
                btnStyle.BackColor = Color.White;
                btnStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
                btnStyle.FlatAppearance.BorderColor = Color.FromArgb(16, 124, 65);
                btnColor.BackColor = Color.FromArgb(248, 250, 252);
                btnColor.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
                btnColor.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
                pnlStyle.Visible = true;
                pnlColor.Visible = false;
            };

            btnColor.Click += (s, e) =>
            {
                btnColor.BackColor = Color.White;
                btnColor.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
                btnColor.FlatAppearance.BorderColor = Color.FromArgb(16, 124, 65);
                btnStyle.BackColor = Color.FromArgb(248, 250, 252);
                btnStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
                btnStyle.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
                pnlStyle.Visible = false;
                pnlColor.Visible = true;
            };

            pnlTabs.Controls.Add(btnStyle);
            pnlTabs.Controls.Add(btnColor);
            this.Controls.Add(pnlTabs);

            // Tab 1: Style Presets
            pnlStyle = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(8) };
            string[] styleNames = new string[] { "Style 1 (Clean White)", "Style 2 (Dark Slate)", "Style 3 (Soft Gray)", "Style 4 (Minimalist)" };
            for (int i = 0; i < 4; i++)
            {
                int styleIdx = i;
                Panel card = new Panel { Size = new Size(185, 75), Location = new Point(10, 8 + i * 82), BackColor = Color.White, Cursor = Cursors.Hand };
                card.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    bool isSel = (chartData.StylePresetIndex == styleIdx);

                    using (Pen p = new Pen(isSel ? Color.FromArgb(16, 124, 65) : Color.FromArgb(209, 213, 219), isSel ? 2f : 1f))
                    {
                        g.DrawRectangle(p, 1, 1, card.Width - 3, card.Height - 3);
                    }

                    DocChartData thumb = chartData.Clone();
                    thumb.StylePresetIndex = styleIdx;
                    thumb.Title = "";
                    ChartRenderer.DrawChart(g, new Rectangle(4, 4, card.Width - 8, card.Height - 8), thumb, false, 0.45f);
                };
                card.Click += (s, e) =>
                {
                    chartData.StylePresetIndex = styleIdx;
                    pnlStyle.Invalidate(true);
                    if (onStateChanged != null) onStateChanged();
                };
                pnlStyle.Controls.Add(card);
            }
            this.Controls.Add(pnlStyle);

            // Tab 2: Color Palettes
            pnlColor = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(8), Visible = false };
            Color[][] palettes = new Color[][]
            {
                new Color[] { Color.FromArgb(68, 114, 196), Color.FromArgb(237, 125, 49), Color.FromArgb(165, 165, 165), Color.FromArgb(255, 192, 0) }, // Office Classic
                new Color[] { Color.FromArgb(112, 48, 160), Color.FromArgb(0, 176, 240), Color.FromArgb(0, 176, 80), Color.FromArgb(255, 192, 0) },   // Colorful Accent
                new Color[] { Color.FromArgb(31, 78, 120), Color.FromArgb(47, 85, 151), Color.FromArgb(65, 113, 156), Color.FromArgb(142, 169, 219) }, // Monochromatic Blue
                new Color[] { Color.FromArgb(230, 81, 0), Color.FromArgb(245, 124, 0), Color.FromArgb(255, 167, 38), Color.FromArgb(255, 204, 128) }  // Warm Amber
            };
            string[] palNames = new string[] { "Office Classic", "Colorful Accent", "Monochrome Blue", "Warm Amber" };

            for (int i = 0; i < palettes.Length; i++)
            {
                int palIdx = i;
                Panel card = new Panel { Size = new Size(185, 48), Location = new Point(10, 8 + i * 56), BackColor = Color.White, Cursor = Cursors.Hand };
                card.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    bool isSel = (chartData.ColorPaletteIndex == palIdx);

                    using (Pen p = new Pen(isSel ? Color.FromArgb(16, 124, 65) : Color.FromArgb(209, 213, 219), isSel ? 2f : 1f))
                    {
                        g.DrawRectangle(p, 1, 1, card.Width - 3, card.Height - 3);
                    }

                    g.DrawString(palNames[palIdx], new Font("Segoe UI", 8.5f), Brushes.Black, 8, 4);

                    for (int k = 0; k < palettes[palIdx].Length; k++)
                    {
                        Rectangle swatch = new Rectangle(8 + k * 28, 22, 24, 18);
                        using (SolidBrush sb = new SolidBrush(palettes[palIdx][k])) g.FillRectangle(sb, swatch);
                    }
                };
                card.Click += (s, e) =>
                {
                    chartData.ColorPaletteIndex = palIdx;
                    pnlColor.Invalidate(true);
                    if (onStateChanged != null) onStateChanged();
                };
                pnlColor.Controls.Add(card);
            }
            this.Controls.Add(pnlColor);

            this.Deactivate += (s, e) => this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderP = new Pen(Color.FromArgb(16, 124, 65), 1.5f))
            {
                e.Graphics.DrawRectangle(borderP, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ChartFiltersPopupForm : Form
    {
        private DocChartData chartData;
        private Action onStateChanged;

        private List<CheckBox> seriesCheckboxes = new List<CheckBox>();
        private List<CheckBox> categoryCheckboxes = new List<CheckBox>();

        public ChartFiltersPopupForm(Point screenPos, DocChartData data, Action onChange)
        {
            this.chartData = data;
            this.onStateChanged = onChange;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = screenPos;
            this.Size = new Size(220, 360);
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9.0f);

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Top Tab Strip: Values | Names
            Panel pnlTabs = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.FromArgb(248, 250, 252) };
            Label lblValues = new Label
            {
                Text = "Values",
                Location = new Point(12, 6),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 65),
                AutoSize = true
            };
            Label lblNames = new Label
            {
                Text = "Names",
                Location = new Point(78, 6),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true
            };
            pnlTabs.Controls.Add(lblValues);
            pnlTabs.Controls.Add(lblNames);
            this.Controls.Add(pnlTabs);

            // Bottom Buttons: Apply | Select Data...
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Color.FromArgb(248, 250, 252) };
            Button btnApply = new Button
            {
                Text = "Apply",
                Location = new Point(10, 8),
                Size = new Size(72, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.White
            };
            btnApply.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnApply.Click += (s, e) =>
            {
                ApplyFilters();
                if (onStateChanged != null) onStateChanged();
                this.Close();
            };

            LinkLabel lnkSelect = new LinkLabel
            {
                Text = "Select Data...",
                Location = new Point(115, 12),
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(16, 124, 65),
                AutoSize = true
            };
            pnlBottom.Controls.Add(btnApply);
            pnlBottom.Controls.Add(lnkSelect);
            this.Controls.Add(pnlBottom);

            // Scrollable Content
            Panel pnlContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(8) };
            int curY = 6;

            // 1. Series Header
            Label lblSeries = new Label
            {
                Text = "◢  Series",
                Location = new Point(8, curY),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblSeries);
            curY += 22;

            CheckBox chkAllSeries = new CheckBox { Text = "(Select All)", Location = new Point(18, curY), Size = new Size(170, 19), Font = new Font("Segoe UI", 8.5f), Checked = true };
            chkAllSeries.CheckedChanged += (s, e) =>
            {
                foreach (var chk in seriesCheckboxes) chk.Checked = chkAllSeries.Checked;
            };
            pnlContent.Controls.Add(chkAllSeries);
            curY += 20;

            Color[] colors = new Color[]
            {
                Color.FromArgb(68, 114, 196),
                Color.FromArgb(237, 125, 49),
                Color.FromArgb(165, 165, 165),
                Color.FromArgb(255, 192, 0)
            };

            int serCount = chartData.SeriesNames != null ? chartData.SeriesNames.Count : 0;
            for (int i = 0; i < serCount; i++)
            {
                int serIdx = i;
                CheckBox chk = new CheckBox
                {
                    Text = chartData.SeriesNames[i],
                    Location = new Point(18, curY),
                    Size = new Size(170, 19),
                    Font = new Font("Segoe UI", 8.0f),
                    Checked = (chartData.SeriesVisible == null || chartData.SeriesVisible.Count <= i) ? true : chartData.SeriesVisible[i]
                };
                seriesCheckboxes.Add(chk);
                pnlContent.Controls.Add(chk);
                curY += 20;
            }

            curY += 8;

            // 2. Categories Header
            Label lblCategories = new Label
            {
                Text = "◢  Categories",
                Location = new Point(8, curY),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblCategories);
            curY += 22;

            CheckBox chkAllCat = new CheckBox { Text = "(Select All)", Location = new Point(18, curY), Size = new Size(170, 19), Font = new Font("Segoe UI", 8.5f), Checked = true };
            chkAllCat.CheckedChanged += (s, e) =>
            {
                foreach (var chk in categoryCheckboxes) chk.Checked = chkAllCat.Checked;
            };
            pnlContent.Controls.Add(chkAllCat);
            curY += 20;

            int catCount = chartData.Categories != null ? chartData.Categories.Count : 0;
            for (int i = 0; i < catCount; i++)
            {
                int catIdx = i;
                CheckBox chk = new CheckBox
                {
                    Text = chartData.Categories[i],
                    Location = new Point(18, curY),
                    Size = new Size(170, 19),
                    Font = new Font("Segoe UI", 8.0f),
                    Checked = (chartData.CategoriesVisible == null || chartData.CategoriesVisible.Count <= i) ? true : chartData.CategoriesVisible[i]
                };
                categoryCheckboxes.Add(chk);
                pnlContent.Controls.Add(chk);
                curY += 20;
            }

            this.Controls.Add(pnlContent);
            this.Deactivate += (s, e) => this.Close();
        }

        private void ApplyFilters()
        {
            if (chartData.SeriesVisible == null) chartData.SeriesVisible = new List<bool>();
            chartData.SeriesVisible.Clear();
            foreach (var chk in seriesCheckboxes) chartData.SeriesVisible.Add(chk.Checked);

            if (chartData.CategoriesVisible == null) chartData.CategoriesVisible = new List<bool>();
            chartData.CategoriesVisible.Clear();
            foreach (var chk in categoryCheckboxes) chartData.CategoriesVisible.Add(chk.Checked);
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

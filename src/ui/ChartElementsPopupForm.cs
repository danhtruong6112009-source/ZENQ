using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ChartElementsPopupForm : Form
    {
        private DocChartData chartData;
        private Action onStateChanged;

        public ChartElementsPopupForm(Point screenPos, DocChartData data, Action onChange)
        {
            this.chartData = data;
            this.onStateChanged = onChange;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = screenPos;
            this.Size = new Size(185, 230);
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9.0f);

            InitializeUI();
        }

        private void InitializeUI()
        {
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.White };
            Label lblTitle = new Label
            {
                Text = "Chart Elements",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 124, 65),
                Location = new Point(8, 6),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            string[] items = new string[]
            {
                "Axes",
                "Axis Titles",
                "Chart Title",
                "Data Labels",
                "Data Table",
                "Error Bars",
                "Gridlines",
                "Legend",
                "Trendline"
            };

            int startY = 32;
            for (int i = 0; i < items.Length; i++)
            {
                int itemIdx = i;
                CheckBox chk = new CheckBox
                {
                    Text = items[i],
                    Location = new Point(12, startY + i * 21),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(30, 41, 59)
                };

                if (itemIdx == 0) chk.Checked = chartData.ShowAxes;
                else if (itemIdx == 2) chk.Checked = chartData.ShowTitle;
                else if (itemIdx == 3) chk.Checked = chartData.ShowDataLabels;
                else if (itemIdx == 4) chk.Checked = chartData.ShowDataTable;
                else if (itemIdx == 6) chk.Checked = chartData.ShowGridlines;
                else if (itemIdx == 7) chk.Checked = chartData.ShowLegend;

                chk.CheckedChanged += (s, e) =>
                {
                    if (itemIdx == 0) chartData.ShowAxes = chk.Checked;
                    else if (itemIdx == 2) chartData.ShowTitle = chk.Checked;
                    else if (itemIdx == 3) chartData.ShowDataLabels = chk.Checked;
                    else if (itemIdx == 4) chartData.ShowDataTable = chk.Checked;
                    else if (itemIdx == 6) chartData.ShowGridlines = chk.Checked;
                    else if (itemIdx == 7) chartData.ShowLegend = chk.Checked;

                    if (onStateChanged != null) onStateChanged();
                };

                this.Controls.Add(chk);
            }

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

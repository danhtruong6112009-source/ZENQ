using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class MarginsDropDownForm : Form
    {
        public event Action<int> OnMarginPresetSelected;
        public event Action OnCustomMarginsClicked;

        public MarginsDropDownForm(Point screenLocation, int currentMarginMode)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(230, 290);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };

            // Presets: 0: Normal, 1: Narrow, 2: Moderate, 3: Wide
            string[][] presets = new string[][]
            {
                new string[] { "Normal (Chuẩn)", "Top: 1\"    Bottom: 1\"", "Left: 1\"    Right: 1\"" },
                new string[] { "Narrow (Hẹp)", "Top: 0.5\"  Bottom: 0.5\"", "Left: 0.5\"  Right: 0.5\"" },
                new string[] { "Moderate (Vừa)", "Top: 1\"    Bottom: 1\"", "Left: 0.75\" Right: 0.75\"" },
                new string[] { "Wide (Rộng)", "Top: 1\"    Bottom: 1\"", "Left: 2\"    Right: 2\"" }
            };

            int itemY = 6;
            for (int i = 0; i < presets.Length; i++)
            {
                int modeIdx = i;
                Panel pnlItem = new Panel
                {
                    Location = new Point(4, itemY),
                    Size = new Size(220, 52),
                    BackColor = (currentMarginMode == i) ? Color.FromArgb(225, 240, 255) : Color.White,
                    Cursor = Cursors.Hand
                };

                Label lblName = new Label
                {
                    Text = presets[i][0],
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Location = new Point(40, 4),
                    AutoSize = true
                };
                Label lblRow1 = new Label
                {
                    Text = presets[i][1],
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                    ForeColor = Color.DimGray,
                    Location = new Point(40, 22),
                    AutoSize = true
                };
                Label lblRow2 = new Label
                {
                    Text = presets[i][2],
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                    ForeColor = Color.DimGray,
                    Location = new Point(40, 35),
                    AutoSize = true
                };

                // Miniature Icon
                PictureBox picIcon = new PictureBox
                {
                    Location = new Point(6, 6),
                    Size = new Size(28, 38),
                    BackColor = Color.White
                };
                picIcon.Paint += (s, e) =>
                {
                    e.Graphics.DrawRectangle(Pens.SteelBlue, 0, 0, 27, 37);
                    int pad = (modeIdx == 1) ? 2 : ((modeIdx == 2) ? 4 : ((modeIdx == 3) ? 8 : 5));
                    e.Graphics.DrawRectangle(Pens.DodgerBlue, pad, pad, 27 - 2 * pad, 37 - 2 * pad);
                };

                pnlItem.Controls.Add(picIcon);
                pnlItem.Controls.Add(lblName);
                pnlItem.Controls.Add(lblRow1);
                pnlItem.Controls.Add(lblRow2);

                pnlItem.Click += (s, e) =>
                {
                    if (OnMarginPresetSelected != null) OnMarginPresetSelected(modeIdx);
                    Close();
                };
                foreach (Control c in pnlItem.Controls)
                {
                    c.Click += (s, e) =>
                    {
                        if (OnMarginPresetSelected != null) OnMarginPresetSelected(modeIdx);
                        Close();
                    };
                }

                pnlMain.Controls.Add(pnlItem);
                itemY += 56;
            }

            // Divider line
            Panel pnlSep = new Panel { Location = new Point(6, itemY + 2), Size = new Size(216, 1), BackColor = Color.LightGray };
            pnlMain.Controls.Add(pnlSep);

            // Custom Margins... Button
            Button btnCustom = new Button
            {
                Text = "Custom Margins... (Lề tùy chỉnh)",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.Navy,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(4, itemY + 6),
                Size = new Size(220, 30),
                Cursor = Cursors.Hand
            };
            btnCustom.FlatAppearance.BorderSize = 0;
            btnCustom.Click += (s, e) =>
            {
                Close();
                if (OnCustomMarginsClicked != null) OnCustomMarginsClicked();
            };
            pnlMain.Controls.Add(btnCustom);

            Deactivate += (s, e) => Close();
            Controls.Add(pnlMain);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1);
        }
    }
}

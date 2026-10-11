using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class TablePickerForm : Form
    {
        public event Action<int, int> OnTableSelected; // cols, rows
        public event Action<int, int> OnTableHovered;  // cols, rows live preview
        public event Action OnTableHoverLeave;         // clear live preview
        public event Action OnCustomInsertClicked;
        public event Action OnDrawTableClicked;
        public event Action OnExcelTableClicked;

        private int hoverCols = 0;
        private int hoverRows = 0;
        private Label lblTitle;
        private Panel pnlGrid;

        public TablePickerForm(Point screenLocation)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(236, 290);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            lblTitle = new Label
            {
                Text = "Chèn Bảng (Insert Table)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(210, 71, 38),
                Location = new Point(10, 8),
                AutoSize = true
            };
            pnlMain.Controls.Add(lblTitle);

            pnlGrid = new Panel
            {
                Location = new Point(10, 32),
                Size = new Size(216, 172),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            pnlGrid.Paint += PnlGrid_Paint;
            pnlGrid.MouseMove += PnlGrid_MouseMove;
            pnlGrid.MouseLeave += (s, e) =>
            {
                hoverCols = 0;
                hoverRows = 0;
                lblTitle.Text = "Chèn Bảng (Insert Table)";
                pnlGrid.Invalidate();
                if (OnTableHoverLeave != null) OnTableHoverLeave();
            };
            pnlGrid.MouseClick += PnlGrid_MouseClick;
            pnlMain.Controls.Add(pnlGrid);

            // Separator
            Panel pnlSep = new Panel { Location = new Point(10, 210), Size = new Size(216, 1), BackColor = Color.FromArgb(226, 232, 240) };
            pnlMain.Controls.Add(pnlSep);

            // 1. Insert Table... option
            Button btnCustom = new Button
            {
                Text = "   ▦  Chèn Bảng (Insert Table)...",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(8, 214),
                Size = new Size(220, 24),
                Cursor = Cursors.Hand
            };
            btnCustom.FlatAppearance.BorderSize = 0;
            btnCustom.MouseEnter += (s, e) => btnCustom.BackColor = Color.FromArgb(241, 245, 249);
            btnCustom.MouseLeave += (s, e) => btnCustom.BackColor = Color.White;
            btnCustom.Click += (s, e) =>
            {
                Close();
                if (OnCustomInsertClicked != null) OnCustomInsertClicked();
            };
            pnlMain.Controls.Add(btnCustom);

            // 2. Draw Table option
            Button btnDraw = new Button
            {
                Text = "   ✏️  Vẽ Bảng (Draw Table)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(8, 238),
                Size = new Size(220, 24),
                Cursor = Cursors.Hand
            };
            btnDraw.FlatAppearance.BorderSize = 0;
            btnDraw.MouseEnter += (s, e) => btnDraw.BackColor = Color.FromArgb(241, 245, 249);
            btnDraw.MouseLeave += (s, e) => btnDraw.BackColor = Color.White;
            btnDraw.Click += (s, e) =>
            {
                Close();
                if (OnDrawTableClicked != null) OnDrawTableClicked();
                else if (OnTableSelected != null) OnTableSelected(4, 3);
            };
            pnlMain.Controls.Add(btnDraw);

            // 3. Excel Spreadsheet option
            Button btnExcel = new Button
            {
                Text = "   📊  Bảng tính Excel (Spreadsheet)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(21, 128, 61),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(8, 262),
                Size = new Size(220, 24),
                Cursor = Cursors.Hand
            };
            btnExcel.FlatAppearance.BorderSize = 0;
            btnExcel.MouseEnter += (s, e) => btnExcel.BackColor = Color.FromArgb(241, 245, 249);
            btnExcel.MouseLeave += (s, e) => btnExcel.BackColor = Color.White;
            btnExcel.Click += (s, e) =>
            {
                Close();
                if (OnExcelTableClicked != null) OnExcelTableClicked();
                else if (OnTableSelected != null) OnTableSelected(5, 5);
            };
            pnlMain.Controls.Add(btnExcel);

            Deactivate += (s, e) =>
            {
                if (OnTableHoverLeave != null) OnTableHoverLeave();
                Close();
            };
            Controls.Add(pnlMain);
        }

        private void PnlGrid_MouseMove(object sender, MouseEventArgs e)
        {
            int col = Math.Min(10, Math.Max(1, (e.X / 21) + 1));
            int row = Math.Min(8, Math.Max(1, (e.Y / 21) + 1));

            if (col != hoverCols || row != hoverRows)
            {
                hoverCols = col;
                hoverRows = row;
                lblTitle.Text = hoverCols + "x" + hoverRows + " Table";
                pnlGrid.Invalidate();

                if (OnTableHovered != null)
                {
                    OnTableHovered(hoverCols, hoverRows);
                }
            }
        }

        private void PnlGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (hoverCols > 0 && hoverRows > 0)
            {
                int cols = hoverCols;
                int rows = hoverRows;
                if (OnTableHoverLeave != null) OnTableHoverLeave();
                if (OnTableSelected != null) OnTableSelected(cols, rows);
                Close();
            }
        }

        private void PnlGrid_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
            using (Pen activeBorderPen = new Pen(Color.FromArgb(234, 88, 12), 1.5f))
            using (SolidBrush activeBrush = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 10; c++)
                    {
                        Rectangle cellRect = new Rectangle(c * 21 + 1, r * 21 + 1, 18, 18);
                        bool isHighlight = (c < hoverCols && r < hoverRows);

                        if (isHighlight)
                        {
                            g.FillRectangle(activeBrush, cellRect);
                            g.DrawRectangle(activeBorderPen, cellRect);
                        }
                        else
                        {
                            g.FillRectangle(Brushes.White, cellRect);
                            g.DrawRectangle(borderPen, cellRect);
                        }
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1.0f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

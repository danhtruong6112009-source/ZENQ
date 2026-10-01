using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class CellContentDropdownForm : Form
    {
        private CalcView _calcView;
        private Form _parentForm;
        private uint _row;
        private uint _col;
        private string _cellCoord;
        private TextBox _txtContent;
        private Button _btnAutoFit;
        private Button _btnClose;
        private Panel _headerPanel;
        private bool _isClosing = false;

        public CellContentDropdownForm(CalcView calcView, Form parentForm, Point screenAnchor, uint row, uint col, string initialText)
        {
            _calcView = calcView;
            _parentForm = parentForm;
            _row = row;
            _col = col;
            _cellCoord = GetColumnName(col) + row;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int formW = 460;
            int formH = 240;

            // Header panel (36px high)
            _headerPanel = new Panel();
            _headerPanel.Height = 36;
            _headerPanel.Dock = DockStyle.Top;
            _headerPanel.BackColor = Color.FromArgb(246, 248, 250);
            _headerPanel.Paint += HeaderPanel_Paint;

            // AutoFit Row Height Button
            _btnAutoFit = new Button();
            _btnAutoFit.Text = "⇕ Tự dãn hàng";
            _btnAutoFit.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            _btnAutoFit.Size = new Size(110, 24);
            _btnAutoFit.Location = new Point(formW - 150, 6);
            _btnAutoFit.FlatStyle = FlatStyle.Flat;
            _btnAutoFit.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnAutoFit.BackColor = Color.White;
            _btnAutoFit.Cursor = Cursors.Hand;
            _btnAutoFit.Click += (s, e) =>
            {
                CommitText();
                if (_calcView != null)
                {
                    _calcView.AutoFitSelectedRows();
                    if (_parentForm != null) _parentForm.Invalidate();
                }
                Close();
            };

            // Close button
            _btnClose = new Button();
            _btnClose.Text = "✕";
            _btnClose.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            _btnClose.Size = new Size(26, 24);
            _btnClose.Location = new Point(formW - 34, 6);
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.BackColor = Color.Transparent;
            _btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.Click += (s, e) => Close();

            _headerPanel.Controls.Add(_btnAutoFit);
            _headerPanel.Controls.Add(_btnClose);

            // Multiline Scrollable TextBox
            Panel textWrapper = new Panel();
            textWrapper.Dock = DockStyle.Fill;
            textWrapper.Padding = new Padding(8, 6, 8, 8);
            textWrapper.BackColor = Color.White;

            _txtContent = new TextBox();
            _txtContent.Multiline = true;
            _txtContent.WordWrap = true;
            _txtContent.ScrollBars = ScrollBars.Vertical;
            _txtContent.Dock = DockStyle.Fill;
            _txtContent.BorderStyle = BorderStyle.None;
            _txtContent.Font = new Font("Segoe UI", 10.0f, FontStyle.Regular);
            _txtContent.BackColor = Color.White;
            _txtContent.ForeColor = Color.FromArgb(15, 23, 42);
            _txtContent.Text = initialText ?? "";
            _txtContent.SelectionStart = _txtContent.Text.Length;
            _txtContent.SelectionLength = 0;

            _txtContent.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    Close();
                }
                else if (e.KeyCode == Keys.Enter && e.Control)
                {
                    e.SuppressKeyPress = true;
                    CommitText();
                    Close();
                }
            };

            textWrapper.Controls.Add(_txtContent);

            Controls.Add(textWrapper);
            Controls.Add(_headerPanel);

            // Anchor & Screen boundary clamping
            Rectangle scr = Screen.FromPoint(screenAnchor).WorkingArea;
            int posX = screenAnchor.X;
            int posY = screenAnchor.Y;

            if (posX + formW > scr.Right) posX = scr.Right - formW - 8;
            if (posX < scr.Left) posX = scr.Left + 8;

            if (posY + formH > scr.Bottom)
            {
                // Show above anchor if not enough space below
                posY = Math.Max(scr.Top + 8, screenAnchor.Y - formH - 24);
            }

            Location = new Point(posX, posY);
            Size = new Size(formW, formH);

            Deactivate += (s, e) =>
            {
                if (!_isClosing)
                {
                    CommitText();
                    Close();
                }
            };
        }

        private void HeaderPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Cell Address Badge (e.g. [ D2 ])
            using (Font fBadge = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush bBadgeBg = new SolidBrush(Color.FromArgb(16, 124, 65)))
            using (SolidBrush bBadgeTxt = new SolidBrush(Color.White))
            {
                SizeF sz = g.MeasureString(_cellCoord, fBadge);
                RectangleF badgeRect = new RectangleF(8, 7, sz.Width + 12, 22);
                using (GraphicsPath path = CreateRoundedPath(badgeRect, 4f))
                {
                    g.FillPath(bBadgeBg, path);
                }
                g.DrawString(_cellCoord, fBadge, bBadgeTxt, badgeRect.X + 6, badgeRect.Y + 3);
            }

            // Draw Title Text
            using (Font fTitle = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush bTitle = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                float titleX = 8 + g.MeasureString(_cellCoord, new Font("Segoe UI", 9.0f, FontStyle.Bold)).Width + 20;
                g.DrawString("Nội dung ô tính (Scrollable Reader / Editor)", fTitle, bTitle, titleX, 9);
            }

            // Divider line below header
            using (Pen pLine = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawLine(pLine, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Outer 1.5px border with Office Excel green accent
            using (Pen pBorder = new Pen(Color.FromArgb(16, 124, 65), 1.5f))
            {
                e.Graphics.DrawRectangle(pBorder, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }

        private void CommitText()
        {
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                if (_calcView != null && _txtContent != null)
                {
                    _calcView.SetCellTextExplicit(_row, _col, _txtContent.Text);
                    if (_parentForm != null) _parentForm.Invalidate();
                }
            }
            catch { }
        }

        private static string GetColumnName(uint col)
        {
            string colName = "";
            uint temp = col;
            while (temp > 0)
            {
                uint mod = (temp - 1) % 26;
                colName = (char)('A' + mod) + colName;
                temp = (temp - 1) / 26;
            }
            return colName;
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

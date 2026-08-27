using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class SizeDropDownForm : Form
    {
        private TextBox txtCustomSize;
        private ListBox lstSizes;
        public float SelectedFontSize { get; private set; }
        public event Action<float> OnSizeSelected;

        public SizeDropDownForm(float currentSize, Point screenLocation)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;

            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int desiredW = 95;
            int desiredH = 260;
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            if (targetY + desiredH > wa.Bottom)
            {
                int aboveY = targetY - desiredH - 30;
                if (aboveY >= wa.Top)
                {
                    targetY = aboveY;
                }
                else
                {
                    targetY = Math.Max(wa.Top + 5, wa.Bottom - desiredH - 5);
                    desiredH = Math.Min(desiredH, wa.Bottom - targetY - 5);
                }
            }
            if (targetX + desiredW > wa.Right) targetX = wa.Right - desiredW - 5;
            if (targetX < wa.Left) targetX = wa.Left + 5;

            Location = new Point(targetX, targetY);
            Size = new Size(desiredW, desiredH);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(243, 244, 246),
                Padding = new Padding(4)
            };

            txtCustomSize = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = currentSize.ToString("0.#"),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle
            };

            txtCustomSize.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    float parsed;
                    if (float.TryParse(txtCustomSize.Text, out parsed) && parsed > 3 && parsed <= 144)
                    {
                        SelectedFontSize = parsed;
                        if (OnSizeSelected != null) OnSizeSelected(SelectedFontSize);
                        Close();
                    }
                }
            };

            topPanel.Controls.Add(txtCustomSize);

            lstSizes = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                BorderStyle = BorderStyle.None,
                ItemHeight = 26,
                Font = new Font("Segoe UI", 9.5f)
            };

            float[] sizes = new float[] { 8, 9, 10, 10.5f, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            int selectedIndex = -1;
            for (int idx = 0; idx < sizes.Length; idx++)
            {
                float sz = sizes[idx];
                lstSizes.Items.Add(sz.ToString("0.#"));
                if (Math.Abs(sz - currentSize) < 0.1f)
                {
                    selectedIndex = idx;
                }
            }

            if (selectedIndex >= 0)
            {
                lstSizes.SelectedIndex = selectedIndex;
            }

            lstSizes.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= lstSizes.Items.Count) return;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                string szText = lstSizes.Items[e.Index].ToString();

                RectangleF itemRect = new RectangleF(e.Bounds.X + 3, e.Bounds.Y + 1, e.Bounds.Width - 6, e.Bounds.Height - 2);

                if (isSelected)
                {
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(239, 246, 255)))
                    using (Pen borderPen = new Pen(Color.FromArgb(147, 197, 253), 1.0f))
                    {
                        FillRoundedRect(g, bgBrush, itemRect, 3);
                        DrawRoundedRect(g, borderPen, itemRect, 3);
                    }
                }
                else
                {
                    using (SolidBrush bgBrush = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(bgBrush, e.Bounds);
                    }
                }

                using (Font itemFont = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush textBrush = new SolidBrush(isSelected ? Color.FromArgb(30, 64, 175) : Color.FromArgb(31, 41, 55)))
                {
                    SizeF textSize = g.MeasureString(szText, itemFont);
                    float textX = e.Bounds.X + (e.Bounds.Width - textSize.Width) / 2.0f;
                    g.DrawString(szText, itemFont, textBrush, textX, e.Bounds.Y + 3);
                }
            };

            lstSizes.MouseDown += (s, e) =>
            {
                int idx = lstSizes.IndexFromPoint(e.Location);
                if (idx >= 0 && idx < lstSizes.Items.Count)
                {
                    lstSizes.SelectedIndex = idx;
                    float parsed;
                    if (float.TryParse(lstSizes.Items[idx].ToString(), out parsed))
                    {
                        SelectedFontSize = parsed;
                        if (OnSizeSelected != null) OnSizeSelected(SelectedFontSize);
                        Close();
                    }
                }
            };

            lstSizes.Click += (s, e) =>
            {
                if (lstSizes.SelectedItem != null)
                {
                    float parsed;
                    if (float.TryParse(lstSizes.SelectedItem.ToString(), out parsed))
                    {
                        SelectedFontSize = parsed;
                        if (OnSizeSelected != null) OnSizeSelected(SelectedFontSize);
                        Close();
                    }
                }
            };

            Deactivate += (s, e) => Close();

            Controls.Add(lstSizes);
            Controls.Add(topPanel);
        }

        private void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                float d = radius * 2;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        private void DrawRoundedRect(Graphics g, Pen pen, RectangleF rect, float radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                float d = radius * 2;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(209, 213, 219), 1.5f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class FontDropDownForm : Form
    {
        private TextBox txtSearch;
        private ListBox lstFonts;
        private List<string> allFonts = new List<string>();
        public string SelectedFontName { get; private set; }
        public event Action<string> OnFontSelected;

        public FontDropDownForm(string currentFont, Point screenLocation, IEnumerable<string> fontList = null)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            
            Rectangle wa = Screen.GetWorkingArea(screenLocation);
            int desiredW = 260;
            int desiredH = 340;
            int targetX = screenLocation.X;
            int targetY = screenLocation.Y;

            // If overflowing bottom edge, flip above the trigger or clamp height
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

            // Search Header Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(243, 244, 246),
                Padding = new Padding(6, 6, 6, 6)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                Text = "",
                BorderStyle = BorderStyle.FixedSingle
            };

            topPanel.Controls.Add(txtSearch);

            lstFonts = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                BorderStyle = BorderStyle.None,
                ItemHeight = 32,
                Font = new Font("Segoe UI", 9.5f)
            };

            if (fontList != null)
            {
                foreach (var f in fontList)
                {
                    if (!string.IsNullOrWhiteSpace(f) && !allFonts.Contains(f))
                    {
                        allFonts.Add(f);
                    }
                }
            }

            if (allFonts.Count == 0)
            {
                allFonts = new List<string>
                {
                    "Calibri",
                    "Times New Roman",
                    "Arial",
                    "Arial Black",
                    "Bahnschrift",
                    "Cambria",
                    "Comic Sans MS",
                    "Courier New",
                    "Georgia",
                    "Impact",
                    "Segoe UI",
                    "Segoe Script",
                    "Tahoma",
                    "Trebuchet MS",
                    "Verdana"
                };
            }

            PopulateList("", currentFont);

            txtSearch.TextChanged += (s, e) =>
            {
                PopulateList(txtSearch.Text, currentFont);
            };

            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstFonts.Items.Count > 0)
                {
                    lstFonts.Focus();
                    if (lstFonts.SelectedIndex < lstFonts.Items.Count - 1)
                    {
                        lstFonts.SelectedIndex++;
                    }
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter && lstFonts.SelectedItem != null)
                {
                    SelectCurrentFontAndClose();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };

            lstFonts.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && lstFonts.SelectedItem != null)
                {
                    SelectCurrentFontAndClose();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };

            lstFonts.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= lstFonts.Items.Count) return;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                string fontName = lstFonts.Items[e.Index].ToString();

                // Item background with 4px margin & rounded corners
                RectangleF itemRect = new RectangleF(e.Bounds.X + 4, e.Bounds.Y + 2, e.Bounds.Width - 8, e.Bounds.Height - 4);

                if (isSelected)
                {
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(239, 246, 255))) // Soft Accent Blue (#EFF6FF)
                    using (Pen borderPen = new Pen(Color.FromArgb(147, 197, 253), 1.0f)) // Soft Blue Border (#93C5FD)
                    {
                        FillRoundedRect(g, bgBrush, itemRect, 4);
                        DrawRoundedRect(g, borderPen, itemRect, 4);
                    }
                }
                else
                {
                    using (SolidBrush bgBrush = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(bgBrush, e.Bounds);
                    }
                }

                // Create font instance matching exact family name for authentic preview
                string cleanFamilyName = fontName.Replace("(Headings)", "").Replace("(Body)", "").Trim();
                using (Font previewFont = CreateSafeFont(cleanFamilyName, 10.5f))
                using (SolidBrush textBrush = new SolidBrush(isSelected ? Color.FromArgb(30, 64, 175) : Color.FromArgb(31, 41, 55)))
                {
                    g.DrawString(fontName, previewFont, textBrush, e.Bounds.X + 12, e.Bounds.Y + 6);
                }
            };

            lstFonts.MouseDown += (s, e) =>
            {
                int idx = lstFonts.IndexFromPoint(e.Location);
                if (idx >= 0 && idx < lstFonts.Items.Count)
                {
                    lstFonts.SelectedIndex = idx;
                    SelectCurrentFontAndClose();
                }
            };

            lstFonts.Click += (s, e) =>
            {
                if (lstFonts.SelectedItem != null)
                {
                    SelectCurrentFontAndClose();
                }
            };

            Deactivate += (s, e) => Close();
            Controls.Add(lstFonts);
            Controls.Add(topPanel);
        }

        private void PopulateList(string filter, string currentFont)
        {
            lstFonts.BeginUpdate();
            lstFonts.Items.Clear();

            string cleanFilter = (filter ?? "").Trim().ToLower();
            int selectedIndex = -1;

            for (int idx = 0; idx < allFonts.Count; idx++)
            {
                string fName = allFonts[idx];
                if (string.IsNullOrEmpty(cleanFilter) || fName.ToLower().Contains(cleanFilter))
                {
                    int itemIdx = lstFonts.Items.Add(fName);
                    if (string.Equals(fName, currentFont, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = itemIdx;
                    }
                }
            }

            if (selectedIndex >= 0)
            {
                lstFonts.SelectedIndex = selectedIndex;
            }
            else if (lstFonts.Items.Count > 0)
            {
                lstFonts.SelectedIndex = 0;
            }

            lstFonts.EndUpdate();
        }

        private void SelectCurrentFontAndClose()
        {
            if (lstFonts.SelectedItem != null)
            {
                string raw = lstFonts.SelectedItem.ToString();
                SelectedFontName = raw.Replace("(Headings)", "").Replace("(Body)", "").Trim();
                if (OnFontSelected != null) OnFontSelected(SelectedFontName);
                Close();
            }
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

        private Font CreateSafeFont(string familyName, float size)
        {
            try
            {
                return new Font(familyName, size, FontStyle.Regular);
            }
            catch
            {
                try
                {
                    return new Font(familyName, size, FontStyle.Bold);
                }
                catch
                {
                    return new Font("Segoe UI", size, FontStyle.Regular);
                }
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

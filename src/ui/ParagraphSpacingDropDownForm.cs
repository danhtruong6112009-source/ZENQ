using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GenOffice.UI
{
    public class ParagraphSpacingItem
    {
        public string Name { get; set; }
        public string Details { get; set; }
        public float LineSpacingFactor { get; set; }
        public float BeforePt { get; set; }
        public float AfterPt { get; set; }
        public int IconType { get; set; } // 0=Default, 1=NoSpace, 2=Compact, 3=Tight, 4=Open, 5=Relaxed, 6=Double

        public ParagraphSpacingItem(string name, string details, float lineSpacing, float before, float after, int iconType = 0)
        {
            Name = name;
            Details = details;
            LineSpacingFactor = lineSpacing;
            BeforePt = before;
            AfterPt = after;
            IconType = iconType;
        }
    }

    public class ParagraphSpacingDropDownForm : Form
    {
        public Action<ParagraphSpacingItem> OnSpacingSelected;
        public Action OnCustomSpacingClicked;

        private ParagraphSpacingItem _defaultItem;
        private List<ParagraphSpacingItem> _builtinItems;
        private string _activeName;

        public ParagraphSpacingDropDownForm(Point location, string activeName = "Mặc định")
        {
            _activeName = activeName ?? "Mặc định";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = new Size(240, 395);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            _defaultItem = new ParagraphSpacingItem("Default", "Trước: 0pt, Sau: 8pt, Dòng: 1.15x", 1.15f, 0f, 8f, 0);

            _builtinItems = new List<ParagraphSpacingItem>
            {
                new ParagraphSpacingItem("No Paragraph Space", "Trước: 0pt, Sau: 0pt, Dòng: 1.00x", 1.00f, 0f, 0f, 1),
                new ParagraphSpacingItem("Compact", "Trước: 0pt, Sau: 4pt, Dòng: 1.00x", 1.00f, 0f, 4f, 2),
                new ParagraphSpacingItem("Tight", "Trước: 0pt, Sau: 6pt, Dòng: 1.15x", 1.15f, 0f, 6f, 3),
                new ParagraphSpacingItem("Open", "Trước: 0pt, Sau: 10pt, Dòng: 1.15x", 1.15f, 0f, 10f, 4),
                new ParagraphSpacingItem("Relaxed", "Trước: 0pt, Sau: 12pt, Dòng: 1.25x", 1.25f, 0f, 12f, 5),
                new ParagraphSpacingItem("Double", "Trước: 0pt, Sau: 8pt, Dòng: 2.00x", 2.00f, 0f, 8f, 6)
            };

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            int curY = 6;

            // SECTION 1: Style Set
            Label lblStyleSet = new Label
            {
                Text = "Style Set",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(14, curY),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblStyleSet);
            curY += 22;

            // Default Item Card
            Control defaultRow = CreateItemRow(_defaultItem, 10, curY, 218, 38, IsActive(_defaultItem));
            contentPanel.Controls.Add(defaultRow);
            curY += 44;

            // Separator 1
            Panel sep1 = new Panel
            {
                Location = new Point(10, curY),
                Size = new Size(220, 1),
                BackColor = Color.FromArgb(229, 231, 235)
            };
            contentPanel.Controls.Add(sep1);
            curY += 6;

            // SECTION 2: Built-In
            Label lblBuiltIn = new Label
            {
                Text = "Built-In",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(14, curY),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblBuiltIn);
            curY += 22;

            foreach (var it in _builtinItems)
            {
                Control row = CreateItemRow(it, 10, curY, 218, 34, IsActive(it));
                contentPanel.Controls.Add(row);
                curY += 36;
            }

            curY += 2;
            // Separator 2
            Panel sep2 = new Panel
            {
                Location = new Point(10, curY),
                Size = new Size(220, 1),
                BackColor = Color.FromArgb(229, 231, 235)
            };
            contentPanel.Controls.Add(sep2);
            curY += 6;

            // Custom Paragraph Spacing... Link
            Label lblCustom = new Label
            {
                Text = "Custom Paragraph Spacing...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(28, curY),
                Size = new Size(200, 24),
                Cursor = Cursors.Hand
            };
            lblCustom.MouseEnter += (s, e) => { lblCustom.ForeColor = Color.FromArgb(29, 78, 216); lblCustom.Font = new Font("Segoe UI", 8.5f, FontStyle.Underline); };
            lblCustom.MouseLeave += (s, e) => { lblCustom.ForeColor = Color.FromArgb(30, 41, 59); lblCustom.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular); };
            lblCustom.Click += (s, e) =>
            {
                Close();
                if (OnCustomSpacingClicked != null) OnCustomSpacingClicked();
            };
            contentPanel.Controls.Add(lblCustom);

            Controls.Add(contentPanel);
            Deactivate += (s, e) => Close();
        }

        private bool IsActive(ParagraphSpacingItem it)
        {
            if (string.IsNullOrEmpty(_activeName)) return false;
            return string.Equals(it.Name, _activeName, StringComparison.OrdinalIgnoreCase) ||
                   _activeName.IndexOf(it.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   it.Name.IndexOf(_activeName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Control CreateItemRow(ParagraphSpacingItem item, int x, int y, int w, int h, bool isSel)
        {
            Control row = new Control
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                Cursor = Cursors.Hand
            };

            bool isHovered = false;
            row.MouseEnter += (s, e) => { isHovered = true; row.Invalidate(); };
            row.MouseLeave += (s, e) => { isHovered = false; row.Invalidate(); };

            row.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (isSel || isHovered)
                {
                    using (SolidBrush b = new SolidBrush(isSel ? Color.FromArgb(239, 246, 255) : Color.FromArgb(243, 244, 246)))
                    {
                        g.FillRectangle(b, 0, 0, row.Width, row.Height);
                    }
                    using (Pen p = new Pen(isSel ? Color.FromArgb(37, 99, 235) : Color.FromArgb(209, 213, 219), 1f))
                    {
                        g.DrawRectangle(p, 0, 0, row.Width - 1, row.Height - 1);
                    }
                }

                // Draw Icon
                DrawSpacingVectorIcon(g, item.IconType, 6, (row.Height - 24) / 2, 24, 24, isSel);

                // Draw Text
                using (Font nf = new Font("Segoe UI", 9.0f, isSel ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush nb = new SolidBrush(isSel ? Color.FromArgb(29, 78, 216) : Color.FromArgb(31, 41, 55)))
                {
                    g.DrawString(item.Name, nf, nb, 38, (row.Height - 16) / 2);
                }
            };

            row.Click += (s, e) =>
            {
                if (OnSpacingSelected != null) OnSpacingSelected(item);
                Close();
            };

            return row;
        }

        private void DrawSpacingVectorIcon(Graphics g, int iconType, int x, int y, int w, int h, bool isSel)
        {
            Color lineCol = isSel ? Color.FromArgb(37, 99, 235) : Color.FromArgb(75, 85, 99);
            Color arrowCol = Color.FromArgb(37, 99, 235); // Blue arrow

            using (Pen lp = new Pen(lineCol, 1.2f))
            using (Pen ap = new Pen(arrowCol, 1.2f))
            {
                if (iconType == 0) // Default: Box with lines and curved arrow/brush
                {
                    using (Pen bp = new Pen(Color.FromArgb(156, 163, 175), 1.0f))
                    {
                        g.DrawRectangle(bp, x, y, w - 1, h - 1);
                    }
                    g.DrawLine(lp, x + 4, y + 6, x + 18, y + 6);
                    g.DrawLine(lp, x + 4, y + 10, x + 16, y + 10);
                    g.DrawLine(lp, x + 4, y + 14, x + 18, y + 14);
                    g.DrawLine(lp, x + 4, y + 18, x + 14, y + 18);

                    // Brush / Arrow
                    using (Pen arrowPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
                    {
                        g.DrawArc(arrowPen, x + 3, y + 3, 10, 8, 180, 180);
                        g.DrawLine(arrowPen, x + 3, y + 7, x + 1, y + 5);
                        g.DrawLine(arrowPen, x + 3, y + 7, x + 5, y + 5);
                    }
                }
                else if (iconType == 1) // No Paragraph Space: 5 tight horizontal lines
                {
                    for (int k = 0; k < 5; k++)
                    {
                        g.DrawLine(lp, x + 2, y + 3 + k * 4, x + 22, y + 3 + k * 4);
                    }
                }
                else if (iconType == 2) // Compact: 4 lines + vertical down arrow
                {
                    g.DrawLine(lp, x + 2, y + 3, x + 22, y + 3);
                    g.DrawLine(lp, x + 2, y + 8, x + 22, y + 8);
                    g.DrawLine(lp, x + 2, y + 13, x + 22, y + 13);
                    g.DrawLine(lp, x + 2, y + 18, x + 22, y + 18);

                    // Vertical arrow
                    g.DrawLine(ap, x + 12, y + 9, x + 12, y + 18);
                    g.DrawLine(ap, x + 9, y + 15, x + 12, y + 18);
                    g.DrawLine(ap, x + 15, y + 15, x + 12, y + 18);
                }
                else if (iconType == 3) // Tight: 4 lines + vertical down arrow
                {
                    g.DrawLine(lp, x + 2, y + 2, x + 22, y + 2);
                    g.DrawLine(lp, x + 2, y + 7, x + 22, y + 7);
                    g.DrawLine(lp, x + 2, y + 14, x + 22, y + 14);
                    g.DrawLine(lp, x + 2, y + 19, x + 22, y + 19);

                    // Vertical arrow
                    g.DrawLine(ap, x + 12, y + 8, x + 12, y + 18);
                    g.DrawLine(ap, x + 9, y + 15, x + 12, y + 18);
                    g.DrawLine(ap, x + 15, y + 15, x + 12, y + 18);
                }
                else if (iconType == 4) // Open: 3 lines + down arrow
                {
                    g.DrawLine(lp, x + 2, y + 3, x + 22, y + 3);
                    g.DrawLine(lp, x + 2, y + 11, x + 22, y + 11);
                    g.DrawLine(lp, x + 2, y + 19, x + 22, y + 19);

                    // Vertical arrow
                    g.DrawLine(ap, x + 12, y + 6, x + 12, y + 18);
                    g.DrawLine(ap, x + 9, y + 15, x + 12, y + 18);
                    g.DrawLine(ap, x + 15, y + 15, x + 12, y + 18);
                }
                else if (iconType == 5) // Relaxed: 3 lines + down arrow
                {
                    g.DrawLine(lp, x + 2, y + 2, x + 22, y + 2);
                    g.DrawLine(lp, x + 2, y + 10, x + 22, y + 10);
                    g.DrawLine(lp, x + 2, y + 20, x + 22, y + 20);

                    // Vertical arrow
                    g.DrawLine(ap, x + 12, y + 7, x + 12, y + 19);
                    g.DrawLine(ap, x + 9, y + 16, x + 12, y + 19);
                    g.DrawLine(ap, x + 15, y + 16, x + 12, y + 19);
                }
                else if (iconType == 6) // Double: 2 lines with wide double gap + down arrow
                {
                    g.DrawLine(lp, x + 2, y + 3, x + 22, y + 3);
                    g.DrawLine(lp, x + 2, y + 19, x + 22, y + 19);

                    // Vertical arrow
                    g.DrawLine(ap, x + 12, y + 6, x + 12, y + 18);
                    g.DrawLine(ap, x + 9, y + 15, x + 12, y + 18);
                    g.DrawLine(ap, x + 15, y + 15, x + 12, y + 18);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(209, 213, 219), 1.0f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}

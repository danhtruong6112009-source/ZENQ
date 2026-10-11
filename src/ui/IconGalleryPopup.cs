using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class IconGalleryPopup : Form
    {
        public Action<string> OnIconSelected;

        public class IconItem
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public Rectangle Bounds { get; set; }
        }

        private class IconDoubleBufferedPanel : Panel
        {
            public IconDoubleBufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }

        private IconDoubleBufferedPanel canvas;
        private List<IconItem> items = new List<IconItem>();
        private int hoveredIndex = -1;
        private ToolTip tip;

        public IconGalleryPopup()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(340, 360);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.DoubleBuffered = true;
            this.Padding = new Padding(1);

            tip = new ToolTip();

            // Populate icon catalog from VectorIconPresets
            string[] iconIds = new string[] {
                "Target", "Rocket", "Lightbulb", "Chart",
                "ShoppingCart", "Laptop", "User", "Trophy",
                "Bell", "Calendar", "Lock", "CheckCircle",
                "StarBadge", "WaveLine"
            };

            string[] iconNames = new string[] {
                "Mục tiêu", "Tên lửa", "Ý tưởng", "Biểu đồ",
                "Giỏ hàng", "Máy tính", "Người dùng", "Cúp vàng",
                "Thông báo", "Lịch trình", "Bảo mật", "Hoàn thành",
                "Ngôi sao", "Biểu tượng sóng"
            };

            for (int i = 0; i < iconIds.Length; i++)
            {
                items.Add(new IconItem { Id = iconIds[i], Name = iconNames[i] });
            }

            Panel header = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(241, 245, 249) };
            Label lblTitle = new Label
            {
                Text = "Biểu tượng Vector (Microsoft 365 Icons)",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(10, 8),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(24, 24),
                Location = new Point(306, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            header.Controls.Add(btnClose);
            this.Controls.Add(header);

            canvas = new IconDoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.White };
            canvas.Paint += Canvas_Paint;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeave += (s, e) => { if (hoveredIndex != -1) { hoveredIndex = -1; canvas.Invalidate(); } };
            canvas.MouseClick += Canvas_MouseClick;
            this.Controls.Add(canvas);

            this.Deactivate += (s, e) => this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Crisp drop border
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(203, 213, 225), 1.5f), 0, 0, Width - 1, Height - 1);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            int foundIdx = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Bounds.Contains(e.Location))
                {
                    foundIdx = i;
                    break;
                }
            }

            if (foundIdx != hoveredIndex)
            {
                hoveredIndex = foundIdx;
                canvas.Invalidate();
                if (hoveredIndex >= 0 && hoveredIndex < items.Count)
                {
                    tip.SetToolTip(canvas, items[hoveredIndex].Name);
                }
                else
                {
                    tip.SetToolTip(canvas, null);
                }
            }
        }

        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (hoveredIndex >= 0 && hoveredIndex < items.Count)
            {
                string selId = items[hoveredIndex].Id;
                this.Close();
                OnIconSelected?.Invoke(selId);
            }
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int cols = 4;
            int cellW = 74;
            int cellH = 68;
            int startX = 14;
            int startY = 10;

            for (int i = 0; i < items.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                Rectangle box = new Rectangle(startX + c * cellW, startY + r * cellH, cellW - 6, cellH - 6);
                items[i].Bounds = box;

                bool isHovered = (i == hoveredIndex);
                if (isHovered)
                {
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    using (Pen hp = new Pen(Color.FromArgb(99, 102, 241), 1.2f))
                    {
                        DrawRoundedRectangle(g, hb, hp, box, 6);
                    }
                }

                // Vector Icon
                float iconSz = 28f;
                float ix = box.X + (box.Width - iconSz) / 2f;
                float iy = box.Y + 6f;

                using (GraphicsPath path = VectorIconPresets.CreateIconPath(items[i].Id, iconSz, iconSz))
                {
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(ix, iy);
                        path.Transform(m);
                    }

                    Color iconCol = isHovered ? Color.FromArgb(79, 70, 229) : Color.FromArgb(51, 65, 85);
                    using (SolidBrush b = new SolidBrush(iconCol))
                    using (Pen p = new Pen(iconCol, 1.2f))
                    {
                        g.FillPath(b, path);
                        g.DrawPath(p, path);
                    }
                }

                // Label
                using (Font sf = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (SolidBrush textBrush = new SolidBrush(isHovered ? Color.FromArgb(67, 56, 202) : Color.FromArgb(71, 85, 105)))
                using (StringFormat sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    RectangleF textRect = new RectangleF(box.X, box.Y + 38, box.Width, 20);
                    g.DrawString(items[i].Name, sf, textBrush, textRect, sfC);
                }
            }
        }

        private static Font sfOrDefault(Font f) => f;

        private void DrawRoundedRectangle(Graphics g, Brush b, Pen p, Rectangle r, int radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                if (b != null) g.FillPath(b, path);
                if (p != null) g.DrawPath(p, path);
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }
    }
}

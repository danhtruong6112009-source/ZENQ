using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class TransitionEffectOptionsDropDownForm : Form
    {
        public event Action<TransitionDirection> OnDirectionSelected;
        public event Action<MorphType> OnMorphModeSelected;

        public TransitionEffectOptionsDropDownForm(Point screenLocation, TransitionType transType, TransitionDirection currentDir, MorphType currentMorph)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(4)
            };

            int curY = 4;
            int itemW = 190;
            int itemH = 32;

            if (transType == TransitionType.Morph)
            {
                AddMorphOption(pnlContent, ref curY, itemW, itemH, MorphType.Objects, "Objects (Đối tượng)", currentMorph == MorphType.Objects);
                AddMorphOption(pnlContent, ref curY, itemW, itemH, MorphType.Words, "Words (Từ ngữ)", currentMorph == MorphType.Words);
                AddMorphOption(pnlContent, ref curY, itemW, itemH, MorphType.Characters, "Characters (Ký tự)", currentMorph == MorphType.Characters);
            }
            else if (transType == TransitionType.Split)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Vertical Out (Dọc mở ra)", currentDir == TransitionDirection.Vertical, "↕");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.In, "Vertical In (Dọc khép vào)", currentDir == TransitionDirection.In, "⇕");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Horizontal Out (Ngang mở ra)", currentDir == TransitionDirection.Horizontal, "↔");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Out, "Horizontal In (Ngang khép vào)", currentDir == TransitionDirection.Out, "⇖");
            }
            else if (transType == TransitionType.Blinds || transType == TransitionType.Comb)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Vertical (Chiều dọc)", currentDir == TransitionDirection.Vertical, "||");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Horizontal (Chiều ngang)", currentDir == TransitionDirection.Horizontal, "=");
            }
            else if (transType == TransitionType.FlyThrough || transType == TransitionType.Box || transType == TransitionType.Zoom)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.In, "In (Phóng to vào trong)", currentDir == TransitionDirection.In, "↘↖");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Out, "Out (Thu nhỏ ra ngoài)", currentDir == TransitionDirection.Out, "↖↘");
            }
            else
            {
                // Default directional 4-way transitions (Push, Wipe, Reveal, Cover, Uncover, Pan, Cube, etc.)
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromBottom, "From Bottom (Từ dưới lên)", currentDir == TransitionDirection.FromBottom, "↑");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromLeft, "From Left (Từ trái sang)", currentDir == TransitionDirection.FromLeft, "→");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromRight, "From Right (Từ phải sang)", currentDir == TransitionDirection.FromRight, "←");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromTop, "From Top (Từ trên xuống)", currentDir == TransitionDirection.FromTop, "↓");
            }

            Size = new Size(itemW + 8, curY + 6);
            Controls.Add(pnlContent);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(170, 170, 170), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AddDirectionOption(Panel parent, ref int curY, int w, int h, TransitionDirection dir, string text, bool isSelected, string symbol)
        {
            Panel item = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(w, h),
                BackColor = isSelected ? Color.FromArgb(204, 232, 255) : Color.White,
                Cursor = Cursors.Hand
            };

            bool isHovered = false;
            item.MouseEnter += (s, e) => { isHovered = true; item.Invalidate(); };
            item.MouseLeave += (s, e) => { isHovered = false; item.Invalidate(); };

            item.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                if (isSelected)
                {
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(204, 232, 255)))
                    using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                    {
                        g.FillRectangle(selBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(selPen, 0, 0, w - 1, h - 1);
                    }
                }
                else if (isHovered)
                {
                    using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(229, 241, 251)))
                    using (Pen hovPen = new Pen(Color.FromArgb(112, 192, 231), 1.0f))
                    {
                        g.FillRectangle(hovBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(hovPen, 0, 0, w - 1, h - 1);
                    }
                }

                // Direction Icon Box
                Rectangle iconBox = new Rectangle(8, (h - 18) / 2, 22, 18);
                using (SolidBrush b = new SolidBrush(isSelected ? Color.FromArgb(37, 99, 235) : Color.FromArgb(240, 244, 248)))
                using (Pen p = new Pen(Color.FromArgb(30, 64, 175), 1.2f))
                {
                    g.FillRectangle(b, iconBox);
                    g.DrawRectangle(p, iconBox);
                }

                using (Font symFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (SolidBrush symBrush = new SolidBrush(isSelected ? Color.White : Color.FromArgb(30, 64, 175)))
                {
                    StringFormat sfSym = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(symbol, symFont, symBrush, iconBox, sfSym);
                }

                // Text Label
                using (Font txtFont = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush txtBrush = new SolidBrush(isSelected ? Color.FromArgb(0, 80, 160) : Color.FromArgb(38, 38, 38)))
                {
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(text, txtFont, txtBrush, new RectangleF(36, 0, w - 40, h), sf);
                }
            };

            item.Click += (s, e) =>
            {
                if (OnDirectionSelected != null) OnDirectionSelected(dir);
                Close();
            };

            parent.Controls.Add(item);
            curY += h + 2;
        }

        private void AddMorphOption(Panel parent, ref int curY, int w, int h, MorphType morph, string text, bool isSelected)
        {
            Panel item = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(w, h),
                BackColor = isSelected ? Color.FromArgb(204, 232, 255) : Color.White,
                Cursor = Cursors.Hand
            };

            bool isHovered = false;
            item.MouseEnter += (s, e) => { isHovered = true; item.Invalidate(); };
            item.MouseLeave += (s, e) => { isHovered = false; item.Invalidate(); };

            item.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                if (isSelected)
                {
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(204, 232, 255)))
                    using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                    {
                        g.FillRectangle(selBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(selPen, 0, 0, w - 1, h - 1);
                    }
                }
                else if (isHovered)
                {
                    using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(229, 241, 251)))
                    using (Pen hovPen = new Pen(Color.FromArgb(112, 192, 231), 1.0f))
                    {
                        g.FillRectangle(hovBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(hovPen, 0, 0, w - 1, h - 1);
                    }
                }

                // Morph Icon
                Rectangle iconBox = new Rectangle(8, (h - 18) / 2, 22, 18);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(37, 99, 235)))
                using (Pen p = new Pen(Color.FromArgb(30, 64, 175), 1.2f))
                {
                    g.FillRectangle(b, iconBox);
                    g.DrawRectangle(p, iconBox);
                    g.FillEllipse(Brushes.Gold, iconBox.X + 7, iconBox.Y + 5, 8, 8);
                }

                // Text Label
                using (Font txtFont = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush txtBrush = new SolidBrush(isSelected ? Color.FromArgb(0, 80, 160) : Color.FromArgb(38, 38, 38)))
                {
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(text, txtFont, txtBrush, new RectangleF(36, 0, w - 40, h), sf);
                }
            };

            item.Click += (s, e) =>
            {
                if (OnMorphModeSelected != null) OnMorphModeSelected(morph);
                Close();
            };

            parent.Controls.Add(item);
            curY += h + 2;
        }
    }
}

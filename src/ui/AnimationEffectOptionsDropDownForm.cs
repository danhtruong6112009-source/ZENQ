using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public class AnimationEffectOptionsDropDownForm : Form
    {
        public event Action<TransitionDirection> OnDirectionSelected;
        public event Action<ShapeAnimation> OnAnimationUpdated;
        public event Action OnOpenAdvancedNeonDialog;

        public AnimationEffectOptionsDropDownForm(Point screenLocation, AnimationType animType, TransitionDirection currentDir)
            : this(screenLocation, new ShapeAnimation { Type = animType, Direction = currentDir }, null)
        {
        }

        public AnimationEffectOptionsDropDownForm(Point screenLocation, ShapeAnimation currentAnim, ShapeNode currentShape)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            AnimationType animType = currentAnim != null ? currentAnim.Type : AnimationType.None;
            TransitionDirection currentDir = currentAnim != null ? currentAnim.Direction : TransitionDirection.Smooth;

            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(4)
            };

            int curY = 4;
            int itemW = 250;
            int itemH = 34;

            if (animType == AnimationType.GenOffice_NeonPulse)
            {
                itemW = 265;
                // Header 1: Target
                AddCategoryHeader(pnlContent, ref curY, itemW, "MỤC TIÊU PHÁT SÁNG:");
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Cả khung viền và chữ", currentAnim.NeonTarget == NeonGlowTarget.Both, () =>
                {
                    currentAnim.NeonTarget = NeonGlowTarget.Both;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Chỉ khung viền", currentAnim.NeonTarget == NeonGlowTarget.ShapeOnly, () =>
                {
                    currentAnim.NeonTarget = NeonGlowTarget.ShapeOnly;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Chỉ văn bản", currentAnim.NeonTarget == NeonGlowTarget.TextOnly, () =>
                {
                    currentAnim.NeonTarget = NeonGlowTarget.TextOnly;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });

                // Header 2: Looping
                AddCategoryHeader(pnlContent, ref curY, itemW, "CHẾ ĐỘ LẶP:");
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Lặp vô hạn", currentAnim.NeonInfiniteLoop, () =>
                {
                    currentAnim.NeonInfiniteLoop = true;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Chạy 1 lần", !currentAnim.NeonInfiniteLoop, () =>
                {
                    currentAnim.NeonInfiniteLoop = false;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });

                // Header 3: Quick Neon Colors
                AddCategoryHeader(pnlContent, ref curY, itemW, "MÀU SẮC NEON:");
                AddColorOption(pnlContent, ref curY, itemW, itemH, Color.FromArgb(0, 240, 255), "Xanh ngọc Neon", currentAnim.NeonColor == Color.FromArgb(0, 240, 255).ToArgb() && currentAnim.NeonColorMode != NeonColorMode.Rainbow, () =>
                {
                    currentAnim.NeonColorMode = NeonColorMode.Monochrome;
                    currentAnim.NeonColor = Color.FromArgb(0, 240, 255).ToArgb();
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddColorOption(pnlContent, ref curY, itemW, itemH, Color.FromArgb(255, 0, 127), "Hồng Magenta", currentAnim.NeonColor == Color.FromArgb(255, 0, 127).ToArgb() && currentAnim.NeonColorMode != NeonColorMode.Rainbow, () =>
                {
                    currentAnim.NeonColorMode = NeonColorMode.Monochrome;
                    currentAnim.NeonColor = Color.FromArgb(255, 0, 127).ToArgb();
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddColorOption(pnlContent, ref curY, itemW, itemH, Color.FromArgb(168, 85, 247), "Tím Neon", currentAnim.NeonColor == Color.FromArgb(168, 85, 247).ToArgb() && currentAnim.NeonColorMode != NeonColorMode.Rainbow, () =>
                {
                    currentAnim.NeonColorMode = NeonColorMode.Monochrome;
                    currentAnim.NeonColor = Color.FromArgb(168, 85, 247).ToArgb();
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddColorOption(pnlContent, ref curY, itemW, itemH, Color.FromArgb(255, 184, 0), "Vàng hổ phách", currentAnim.NeonColor == Color.FromArgb(255, 184, 0).ToArgb() && currentAnim.NeonColorMode != NeonColorMode.Rainbow, () =>
                {
                    currentAnim.NeonColorMode = NeonColorMode.Monochrome;
                    currentAnim.NeonColor = Color.FromArgb(255, 184, 0).ToArgb();
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Hiệu ứng Cầu vồng", currentAnim.NeonColorMode == NeonColorMode.Rainbow, () =>
                {
                    currentAnim.NeonColorMode = NeonColorMode.Rainbow;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });
                AddActionOption(pnlContent, ref curY, itemW, itemH, "Tự động theo màu hình", currentAnim.NeonColor == 0 && currentAnim.NeonColorMode != NeonColorMode.Rainbow, () =>
                {
                    currentAnim.NeonColorMode = NeonColorMode.Monochrome;
                    currentAnim.NeonColor = 0;
                    if (OnAnimationUpdated != null) OnAnimationUpdated(currentAnim);
                    Close();
                });

                // Separator & Advanced Neon Studio Dialog
                AddSeparator(pnlContent, ref curY, itemW);
                AddAdvancedDialogButton(pnlContent, ref curY, itemW, itemH + 2, "Tùy chỉnh thông số chi tiết...", () =>
                {
                    Close();
                    if (OnOpenAdvancedNeonDialog != null) OnOpenAdvancedNeonDialog();
                });
            }
            else if (animType == AnimationType.GenOffice_KineticSnap)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromLeft, "Từ bên trái (From Left)", currentDir == TransitionDirection.FromLeft || currentDir == TransitionDirection.Smooth, "→");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromRight, "Từ bên phải (From Right)", currentDir == TransitionDirection.FromRight, "←");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromTop, "Từ trên xuống (From Top)", currentDir == TransitionDirection.FromTop, "↓");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromBottom, "Từ dưới lên (From Bottom)", currentDir == TransitionDirection.FromBottom, "↑");
            }
            else if (animType == AnimationType.GenOffice_GoldenRibbonSwirl)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromBottom, "Lướt sóng từ dưới lên (From Bottom)", currentDir == TransitionDirection.FromBottom || currentDir == TransitionDirection.Smooth, "↑");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromTop, "Lướt sóng từ trên xuống (From Top)", currentDir == TransitionDirection.FromTop, "↓");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromLeft, "Lướt sóng từ bên trái (From Left)", currentDir == TransitionDirection.FromLeft, "→");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromRight, "Lướt sóng từ bên phải (From Right)", currentDir == TransitionDirection.FromRight, "←");
            }
            else if (animType == AnimationType.GenOffice_Cinematic3DFlip)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Lật ngang trục Y (Horizontal 3D Flip)", currentDir == TransitionDirection.Horizontal || currentDir == TransitionDirection.Smooth || currentDir == TransitionDirection.FromLeft, "↔");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Lật dọc trục X (Vertical 3D Flip)", currentDir == TransitionDirection.Vertical || currentDir == TransitionDirection.FromTop, "↕");
            }
            else if (animType == AnimationType.GenOffice_OrigamiUnfold)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Mở cánh gấp ngang (Horizontal Unfold)", currentDir == TransitionDirection.Horizontal || currentDir == TransitionDirection.Smooth || currentDir == TransitionDirection.FromLeft, "↔");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Mở cánh gấp dọc (Vertical Unfold)", currentDir == TransitionDirection.Vertical || currentDir == TransitionDirection.FromTop, "↕");
            }
            else if (animType == AnimationType.GenOffice_VortexSpiral)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.In, "Xoáy vào thuận chiều (Clockwise In)", currentDir == TransitionDirection.In || currentDir == TransitionDirection.Smooth, "🌀");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Out, "Xoáy ra ngược chiều (Counter-CW Out)", currentDir == TransitionDirection.Out, "🔄");
            }
            else if (animType == AnimationType.GenOffice_GlitchHologram)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Nhiễu sóng quét ngang (Horizontal Scan)", currentDir == TransitionDirection.Horizontal || currentDir == TransitionDirection.Smooth, "↔");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Nhiễu sóng quét dọc (Vertical Scan)", currentDir == TransitionDirection.Vertical, "↕");
            }
            else if (animType == AnimationType.GenOffice_LiquidMorph)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Biến dạng nảy dọc (Vertical Wobble)", currentDir == TransitionDirection.Vertical || currentDir == TransitionDirection.Smooth, "↕");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Biến dạng nảy ngang (Horizontal Wobble)", currentDir == TransitionDirection.Horizontal, "↔");
            }
            else if (animType == AnimationType.SplitIn || animType == AnimationType.SplitOut)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Vertical, "Dọc mở ra (Vertical Out)", currentDir == TransitionDirection.Vertical, "↕");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.In, "Dọc khép vào (Vertical In)", currentDir == TransitionDirection.In, "⇕");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Horizontal, "Ngang mở ra (Horizontal Out)", currentDir == TransitionDirection.Horizontal, "↔");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.Out, "Ngang khép vào (Horizontal In)", currentDir == TransitionDirection.Out, "⇖");
            }
            else if (animType == AnimationType.MotionLine || animType == AnimationType.MotionArc || animType == AnimationType.MotionTurn)
            {
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromBottom, "Hướng xuống dưới (Down)", currentDir == TransitionDirection.FromBottom || currentDir == TransitionDirection.Smooth, "↓");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromRight, "Hướng sang phải (Right)", currentDir == TransitionDirection.FromRight, "→");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromLeft, "Hướng sang trái (Left)", currentDir == TransitionDirection.FromLeft, "←");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromTop, "Hướng lên trên (Up)", currentDir == TransitionDirection.FromTop, "↑");
            }
            else
            {
                // Standard 4-way direction animations (FlyIn, FloatIn, WipeIn, FlyOut, FloatOut, WipeOut, PeekIn)
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromBottom, "Từ dưới lên (From Bottom)", currentDir == TransitionDirection.FromBottom || currentDir == TransitionDirection.Smooth, "↑");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromLeft, "Từ bên trái (From Left)", currentDir == TransitionDirection.FromLeft, "→");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromRight, "Từ bên phải (From Right)", currentDir == TransitionDirection.FromRight, "←");
                AddDirectionOption(pnlContent, ref curY, itemW, itemH, TransitionDirection.FromTop, "Từ trên xuống (From Top)", currentDir == TransitionDirection.FromTop, "↓");
            }

            int finalH = Math.Min(480, curY + 8);
            Size = new Size(itemW + 10, finalH);
            Controls.Add(pnlContent);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 185, 190), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AddCategoryHeader(Panel parent, ref int curY, int w, string title)
        {
            Label lbl = new Label
            {
                Text = title,
                Location = new Point(8, curY + 2),
                Size = new Size(w - 16, 18),
                ForeColor = Color.FromArgb(109, 40, 217),
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold)
            };
            parent.Controls.Add(lbl);
            curY += 22;
        }

        private void AddSeparator(Panel parent, ref int curY, int w)
        {
            Panel sep = new Panel
            {
                Location = new Point(8, curY + 2),
                Size = new Size(w - 16, 1),
                BackColor = Color.FromArgb(229, 231, 235)
            };
            parent.Controls.Add(sep);
            curY += 6;
        }

        private void AddActionOption(Panel parent, ref int curY, int w, int h, string text, bool isSelected, Action onClick)
        {
            Panel item = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(w, h),
                BackColor = isSelected ? Color.FromArgb(237, 233, 254) : Color.White,
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
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(237, 233, 254)))
                    using (Pen selPen = new Pen(Color.FromArgb(139, 92, 246), 1.2f))
                    {
                        g.FillRectangle(selBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(selPen, 0, 0, w - 1, h - 1);
                    }
                }
                else if (isHovered)
                {
                    using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                    using (Pen hovPen = new Pen(Color.FromArgb(209, 213, 219), 1.0f))
                    {
                        g.FillRectangle(hovBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(hovPen, 0, 0, w - 1, h - 1);
                    }
                }

                using (Font txtFont = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush txtBrush = new SolidBrush(isSelected ? Color.FromArgb(91, 33, 182) : Color.FromArgb(31, 41, 55)))
                {
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(text, txtFont, txtBrush, new RectangleF(12, 0, w - 16, h), sf);
                }
            };

            item.Click += (s, e) => { if (onClick != null) onClick(); };
            parent.Controls.Add(item);
            curY += h + 2;
        }

        private void AddColorOption(Panel parent, ref int curY, int w, int h, Color color, string name, bool isSelected, Action onClick)
        {
            Panel item = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(w, h),
                BackColor = isSelected ? Color.FromArgb(237, 233, 254) : Color.White,
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
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(237, 233, 254)))
                    using (Pen selPen = new Pen(Color.FromArgb(139, 92, 246), 1.2f))
                    {
                        g.FillRectangle(selBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(selPen, 0, 0, w - 1, h - 1);
                    }
                }
                else if (isHovered)
                {
                    using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                    using (Pen hovPen = new Pen(Color.FromArgb(209, 213, 219), 1.0f))
                    {
                        g.FillRectangle(hovBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(hovPen, 0, 0, w - 1, h - 1);
                    }
                }

                // Color Chip Box
                Rectangle cBox = new Rectangle(10, (h - 18) / 2, 18, 18);
                using (SolidBrush cb = new SolidBrush(color))
                using (Pen cp = new Pen(Color.FromArgb(100, 0, 0, 0), 1.0f))
                {
                    g.FillRectangle(cb, cBox);
                    g.DrawRectangle(cp, cBox);
                }

                // Text
                using (Font txtFont = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush txtBrush = new SolidBrush(isSelected ? Color.FromArgb(91, 33, 182) : Color.FromArgb(31, 41, 55)))
                {
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(name, txtFont, txtBrush, new RectangleF(36, 0, w - 40, h), sf);
                }
            };

            item.Click += (s, e) => { if (onClick != null) onClick(); };
            parent.Controls.Add(item);
            curY += h + 2;
        }

        private void AddAdvancedDialogButton(Panel parent, ref int curY, int w, int h, string text, Action onClick)
        {
            Panel item = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(245, 243, 255),
                Cursor = Cursors.Hand
            };

            bool isHovered = false;
            item.MouseEnter += (s, e) => { isHovered = true; item.Invalidate(); };
            item.MouseLeave += (s, e) => { isHovered = false; item.Invalidate(); };

            item.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                using (SolidBrush bg = new SolidBrush(isHovered ? Color.FromArgb(237, 233, 254) : Color.FromArgb(245, 243, 255)))
                using (Pen p = new Pen(Color.FromArgb(139, 92, 246), 1.0f))
                {
                    g.FillRectangle(bg, 0, 0, w - 1, h - 1);
                    g.DrawRectangle(p, 0, 0, w - 1, h - 1);
                }

                using (Font txtFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (SolidBrush txtBrush = new SolidBrush(Color.FromArgb(109, 40, 217)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(text, txtFont, txtBrush, new RectangleF(0, 0, w, h), sf);
                }
            };

            item.Click += (s, e) => { if (onClick != null) onClick(); };
            parent.Controls.Add(item);
            curY += h + 3;
        }

        private void AddDirectionOption(Panel parent, ref int curY, int w, int h, TransitionDirection dir, string text, bool isSelected, string symbol)
        {
            Panel item = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(w, h),
                BackColor = isSelected ? Color.FromArgb(237, 233, 254) : Color.White,
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
                    using (SolidBrush selBg = new SolidBrush(Color.FromArgb(237, 233, 254)))
                    using (Pen selPen = new Pen(Color.FromArgb(139, 92, 246), 1.2f))
                    {
                        g.FillRectangle(selBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(selPen, 0, 0, w - 1, h - 1);
                    }
                }
                else if (isHovered)
                {
                    using (SolidBrush hovBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                    using (Pen hovPen = new Pen(Color.FromArgb(209, 213, 219), 1.0f))
                    {
                        g.FillRectangle(hovBg, 0, 0, w - 1, h - 1);
                        g.DrawRectangle(hovPen, 0, 0, w - 1, h - 1);
                    }
                }

                // Direction Icon Box
                Rectangle iconBox = new Rectangle(8, (h - 20) / 2, 24, 20);
                using (SolidBrush b = new SolidBrush(isSelected ? Color.FromArgb(124, 58, 237) : Color.FromArgb(243, 244, 246)))
                using (Pen p = new Pen(isSelected ? Color.FromArgb(109, 40, 217) : Color.FromArgb(209, 213, 219), 1.2f))
                {
                    g.FillRectangle(b, iconBox);
                    g.DrawRectangle(p, iconBox);
                }

                using (Font symFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (SolidBrush symBrush = new SolidBrush(isSelected ? Color.White : Color.FromArgb(75, 85, 99)))
                {
                    StringFormat sfSym = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(symbol, symFont, symBrush, iconBox, sfSym);
                }

                // Text Label
                using (Font txtFont = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush txtBrush = new SolidBrush(isSelected ? Color.FromArgb(91, 33, 182) : Color.FromArgb(31, 41, 55)))
                {
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(text, txtFont, txtBrush, new RectangleF(38, 0, w - 42, h), sf);
                }
            };

            item.Click += (s, e) =>
            {
                if (OnDirectionSelected != null) OnDirectionSelected(dir);
                Close();
            };

            parent.Controls.Add(item);
            curY += h + 3;
        }
    }
}

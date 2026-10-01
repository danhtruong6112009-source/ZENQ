using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class AnimationsGalleryDropDownForm : Form
    {
        public event Action<AnimationType> OnAnimationSelected;

        public AnimationsGalleryDropDownForm(Point screenLocation, AnimationType currentAnimation = AnimationType.None)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(680, 560);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 12)
            };

            int curY = 8;

            // 0. None (Không có)
            AddNoneCategory(scrollPanel, ref curY, currentAnimation);

            // 1. 🌟 Độc Quyền ZenQ (ZenQ Exclusive Animations - Tím/Vàng Kim 🌟)
            AddCategory(scrollPanel, ref curY, "Hiệu ứng Độc quyền ZenQ (Exclusive Animations)", currentAnimation, new Tuple<AnimationType, string>[] {
                Tuple.Create(AnimationType.GenOffice_NeonPulse, "Neon Glow"),
                Tuple.Create(AnimationType.GenOffice_LiquidMorph, "Liquid Fluid"),
                Tuple.Create(AnimationType.GenOffice_Cinematic3DFlip, "3D Flip & Flare"),
                Tuple.Create(AnimationType.GenOffice_GlitchHologram, "Cyber Hologram"),
                Tuple.Create(AnimationType.GenOffice_KineticSnap, "Kinetic Snap"),
                Tuple.Create(AnimationType.GenOffice_VortexSpiral, "Cosmic Vortex"),
                Tuple.Create(AnimationType.GenOffice_OrigamiUnfold, "3D Origami"),
                Tuple.Create(AnimationType.GenOffice_StarlightAssembly, "Starlight Burst"),
                Tuple.Create(AnimationType.GenOffice_ElasticJellyPop, "Jelly Pop"),
                Tuple.Create(AnimationType.GenOffice_GoldenRibbonSwirl, "Golden Ribbon")
            }, "Exclusive");

            // 2. Entrance (Xuất hiện - Màu xanh lá 🟢)
            AddCategory(scrollPanel, ref curY, "Entrance", currentAnimation, new Tuple<AnimationType, string>[] {
                Tuple.Create(AnimationType.Appear, "Appear"),
                Tuple.Create(AnimationType.FadeIn, "Fade"),
                Tuple.Create(AnimationType.FlyIn, "Fly In"),
                Tuple.Create(AnimationType.FloatIn, "Float In"),
                Tuple.Create(AnimationType.SplitIn, "Split"),
                Tuple.Create(AnimationType.WipeIn, "Wipe"),
                Tuple.Create(AnimationType.ShapeIn, "Shape"),
                Tuple.Create(AnimationType.WheelIn, "Wheel"),
                Tuple.Create(AnimationType.RandomBarsIn, "Random Bars"),
                Tuple.Create(AnimationType.GrowAndTurn, "Grow & Turn"),
                Tuple.Create(AnimationType.ZoomIn, "Zoom"),
                Tuple.Create(AnimationType.Swivel, "Swivel"),
                Tuple.Create(AnimationType.Bounce, "Bounce")
            }, "Entrance");

            // 3. Emphasis (Nhấn mạnh - Màu vàng cam 🟡)
            AddCategory(scrollPanel, ref curY, "Emphasis", currentAnimation, new Tuple<AnimationType, string>[] {
                Tuple.Create(AnimationType.Pulse, "Pulse"),
                Tuple.Create(AnimationType.ColorPulse, "Color Pulse"),
                Tuple.Create(AnimationType.Teeter, "Teeter"),
                Tuple.Create(AnimationType.Spin, "Spin"),
                Tuple.Create(AnimationType.GrowShrink, "Grow/Shrink"),
                Tuple.Create(AnimationType.Desaturate, "Desaturate"),
                Tuple.Create(AnimationType.Darken, "Darken"),
                Tuple.Create(AnimationType.Lighten, "Lighten"),
                Tuple.Create(AnimationType.Transparency, "Transparency"),
                Tuple.Create(AnimationType.ObjectColor, "Object Color"),
                Tuple.Create(AnimationType.ComplementaryColor, "Compleme..."),
                Tuple.Create(AnimationType.LineColor, "Line Color"),
                Tuple.Create(AnimationType.FillColor, "Fill Color"),
                Tuple.Create(AnimationType.BrushColor, "Brush Color"),
                Tuple.Create(AnimationType.FontColor, "Font Color"),
                Tuple.Create(AnimationType.Underline, "Underline"),
                Tuple.Create(AnimationType.BoldFlash, "Bold Flash"),
                Tuple.Create(AnimationType.BoldReveal, "Bold Reveal"),
                Tuple.Create(AnimationType.Wave, "Wave")
            }, "Emphasis");

            // 4. Exit (Thoát / Biến mất - Màu đỏ 🔴)
            AddCategory(scrollPanel, ref curY, "Exit", currentAnimation, new Tuple<AnimationType, string>[] {
                Tuple.Create(AnimationType.Disappear, "Disappear"),
                Tuple.Create(AnimationType.FadeOut, "Fade"),
                Tuple.Create(AnimationType.FlyOut, "Fly Out"),
                Tuple.Create(AnimationType.FloatOut, "Float Out"),
                Tuple.Create(AnimationType.SplitOut, "Split"),
                Tuple.Create(AnimationType.WipeOut, "Wipe"),
                Tuple.Create(AnimationType.ShapeOut, "Shape"),
                Tuple.Create(AnimationType.WheelOut, "Wheel"),
                Tuple.Create(AnimationType.RandomBarsOut, "Random Bars"),
                Tuple.Create(AnimationType.ShrinkAndTurn, "Shrink & Turn"),
                Tuple.Create(AnimationType.ZoomOut, "Zoom"),
                Tuple.Create(AnimationType.SwivelOut, "Swivel"),
                Tuple.Create(AnimationType.BounceOut, "Bounce")
            }, "Exit");

            // 5. Motion Paths (Đường chuyển động 🔵)
            AddCategory(scrollPanel, ref curY, "Motion Paths", currentAnimation, new Tuple<AnimationType, string>[] {
                Tuple.Create(AnimationType.MotionLine, "Lines"),
                Tuple.Create(AnimationType.MotionArc, "Arcs"),
                Tuple.Create(AnimationType.MotionTurn, "Turns"),
                Tuple.Create(AnimationType.MotionShape, "Shapes"),
                Tuple.Create(AnimationType.MotionLoop, "Loops"),
                Tuple.Create(AnimationType.MotionCustom, "Custom Path")
            }, "MotionPaths");

            // 6. Footer Links (More Effects...)
            AddFooterLinks(scrollPanel, ref curY);

            Controls.Add(scrollPanel);
            Deactivate += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(Color.FromArgb(180, 180, 180), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void AddNoneCategory(Panel parent, ref int curY, AnimationType currentAnimation)
        {
            Label lblNone = new Label
            {
                Text = "None",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, curY),
                AutoSize = true
            };
            parent.Controls.Add(lblNone);
            curY += 22;

            Panel pnlNone = new Panel
            {
                Location = new Point(14, curY),
                Size = new Size(68, 54),
                BackColor = (currentAnimation == AnimationType.None) ? Color.FromArgb(220, 230, 245) : Color.FromArgb(248, 249, 250),
                Cursor = Cursors.Hand
            };

            pnlNone.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (Pen p = new Pen((currentAnimation == AnimationType.None) ? Color.FromArgb(37, 99, 235) : Color.FromArgb(210, 215, 220), 1.0f))
                {
                    g.DrawRectangle(p, 0, 0, 67, 53);
                }

                // Gray outline star
                DrawStarVector(g, 34, 18, 12, 5.5f, Color.FromArgb(120, 130, 140), false);

                using (Font f = new Font("Segoe UI", 8.0f))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString("None", f, Brushes.Black, new RectangleF(0, 32, 68, 18), sf);
                }
            };

            pnlNone.Click += (s, e) =>
            {
                if (OnAnimationSelected != null) OnAnimationSelected(AnimationType.None);
                Close();
            };

            parent.Controls.Add(pnlNone);
            curY += 64;
        }

        private void AddCategory(Panel parent, ref int curY, string title, AnimationType currentAnimation, Tuple<AnimationType, string>[] items, string categoryType)
        {
            // Category Title Divider
            Panel headerPnl = new Panel { Location = new Point(14, curY), Size = new Size(628, 24) };
            headerPnl.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int textX = 0;
                if (categoryType == "Exclusive")
                {
                    DrawStarVector(g, 7, 9, 7.0f, 3.2f, Color.FromArgb(245, 158, 11), true);
                    textX = 18;
                }
                using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(categoryType == "Exclusive" ? Color.FromArgb(109, 40, 217) : Color.FromArgb(30, 41, 59)))
                {
                    g.DrawString(title, f, b, textX, 2);
                }
                using (Pen p = new Pen(categoryType == "Exclusive" ? Color.FromArgb(233, 213, 255) : Color.FromArgb(235, 238, 242), 1.0f))
                {
                    g.DrawLine(p, 0, 22, 628, 22);
                }
            };
            parent.Controls.Add(headerPnl);
            curY += 28;

            int col = 0;
            int rowY = curY;
            int itemW = 72;
            int itemH = (categoryType == "Exclusive") ? 68 : 64;

            for (int i = 0; i < items.Length; i++)
            {
                AnimationType aType = items[i].Item1;
                string aName = items[i].Item2;
                bool isSelected = (aType == currentAnimation);
                bool isExclusive = (categoryType == "Exclusive");

                Panel pnl = new Panel
                {
                    Location = new Point(14 + col * (itemW + 6), rowY),
                    Size = new Size(itemW, itemH),
                    BackColor = isSelected ? (isExclusive ? Color.FromArgb(243, 236, 255) : Color.FromArgb(220, 230, 245)) : (isExclusive ? Color.FromArgb(253, 251, 255) : Color.FromArgb(250, 250, 250)),
                    Cursor = Cursors.Hand
                };

                pnl.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    Color borderColor = isSelected ? (isExclusive ? Color.FromArgb(139, 92, 246) : Color.FromArgb(37, 99, 235)) : (isExclusive ? Color.FromArgb(230, 220, 245) : Color.FromArgb(220, 224, 230));
                    using (Pen p = new Pen(borderColor, isSelected ? 1.5f : 1.0f))
                    {
                        g.DrawRectangle(p, 0, 0, itemW - 1, itemH - 1);
                    }

                    // Render Vector Icon for this Animation
                    DrawAnimationVector(g, itemW / 2, 17, aType, categoryType);

                    using (Font f = new Font("Segoe UI", 7.0f, isSelected ? FontStyle.Bold : FontStyle.Regular))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString(aName, f, Brushes.Black, new RectangleF(1, 33, itemW - 2, 32), sf);
                    }
                };

                pnl.MouseEnter += (s, e) => { if (!isSelected) pnl.BackColor = isExclusive ? Color.FromArgb(248, 240, 255) : Color.FromArgb(240, 245, 255); };
                pnl.MouseLeave += (s, e) => { if (!isSelected) pnl.BackColor = isExclusive ? Color.FromArgb(253, 251, 255) : Color.FromArgb(250, 250, 250); };

                pnl.Click += (s, e) =>
                {
                    if (OnAnimationSelected != null) OnAnimationSelected(aType);
                    Close();
                };

                parent.Controls.Add(pnl);

                col++;
                if (col >= 8)
                {
                    col = 0;
                    rowY += itemH + 6;
                }
            }

            if (col > 0) rowY += itemH + 6;
            curY = rowY + 10;
        }

        public event Action<string> OnOpenMoreEffects;

        private void AddFooterLinks(Panel parent, ref int curY)
        {
            Panel footerPnl = new Panel { Location = new Point(14, curY), Size = new Size(628, 150), BackColor = Color.White };
            footerPnl.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235), 1.0f))
                {
                    g.DrawLine(p, 0, 0, 628, 0);
                }
            };

            string[] linkTexts = new string[] {
                "🌟 Thêm hiệu ứng độc quyền ZenQ (More Exclusive Effects)...",
                "⭐ Thêm hiệu ứng xuất hiện (More Entrance Effects)...",
                "⭐ Thêm hiệu ứng nhấn mạnh (More Emphasis Effects)...",
                "⭐ Thêm hiệu ứng biến mất (More Exit Effects)...",
                "⭐ Thêm đường chuyển động (More Motion Paths)..."
            };
            string[] linkCats = new string[] {
                "Exclusive",
                "Entrance",
                "Emphasis",
                "Exit",
                "MotionPaths"
            };
            Color[] linkColors = new Color[] {
                Color.FromArgb(147, 51, 234),
                Color.FromArgb(22, 163, 74),
                Color.FromArgb(202, 138, 4),
                Color.FromArgb(220, 38, 38),
                Color.FromArgb(37, 99, 235)
            };

            for (int k = 0; k < linkTexts.Length; k++)
            {
                string catKey = linkCats[k];
                Label linkLbl = new Label
                {
                    Text = linkTexts[k],
                    Font = new Font("Segoe UI", 9.0f),
                    ForeColor = linkColors[k],
                    Location = new Point(8, 8 + k * 26),
                    AutoSize = true,
                    Cursor = Cursors.Hand
                };
                linkLbl.MouseEnter += (s, e) => { linkLbl.Font = new Font("Segoe UI", 9.0f, FontStyle.Underline); };
                linkLbl.MouseLeave += (s, e) => { linkLbl.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular); };
                linkLbl.Click += (s, e) =>
                {
                    Close();
                    if (OnOpenMoreEffects != null)
                    {
                        OnOpenMoreEffects(catKey);
                    }
                };
                footerPnl.Controls.Add(linkLbl);
            }

            parent.Controls.Add(footerPnl);
            curY += 160;
        }

        public static void DrawAnimationVector(Graphics g, float cx, float cy, AnimationType type, string category)
        {
            switch (category)
            {
                case "Exclusive":
                    DrawExclusiveVector(g, cx, cy, type);
                    break;
                case "Entrance":
                    DrawEntranceVector(g, cx, cy, type);
                    break;
                case "Emphasis":
                    DrawEmphasisVector(g, cx, cy, type);
                    break;
                case "Exit":
                    DrawExitVector(g, cx, cy, type);
                    break;
                case "MotionPaths":
                    DrawMotionPathVector(g, cx, cy, type);
                    break;
                default:
                    DrawStarVector(g, cx, cy, 11, 5, Color.FromArgb(34, 197, 94), true);
                    break;
            }
        }

        public static void DrawExclusiveVector(Graphics g, float cx, float cy, AnimationType type)
        {
            Color purple = Color.FromArgb(139, 92, 246);
            Color gold = Color.FromArgb(245, 158, 11);
            Color cyan = Color.FromArgb(6, 182, 212);
            Color rose = Color.FromArgb(244, 63, 94);
            Color emerald = Color.FromArgb(16, 185, 129);

            switch (type)
            {
                case AnimationType.GenOffice_NeonPulse:
                    // Dual neon glowing rings + center glowing star
                    using (Pen np1 = new Pen(Color.FromArgb(120, cyan), 2.2f))
                    using (Pen np2 = new Pen(Color.FromArgb(180, purple), 1.2f))
                    {
                        g.DrawEllipse(np1, cx - 14, cy - 14, 28, 28);
                        g.DrawEllipse(np2, cx - 11, cy - 11, 22, 22);
                    }
                    DrawStarVector(g, cx, cy, 9.5f, 4.2f, purple, true);
                    break;

                case AnimationType.GenOffice_LiquidMorph:
                    // Liquid droplet silhouette + fluid wobble ripple
                    using (Pen lp = new Pen(Color.FromArgb(14, 165, 233), 1.6f))
                    using (SolidBrush lb = new SolidBrush(Color.FromArgb(180, 14, 165, 233)))
                    {
                        PointF[] dropPts = new PointF[] {
                            new PointF(cx, cy - 13),
                            new PointF(cx + 8, cy - 2),
                            new PointF(cx + 9, cy + 5),
                            new PointF(cx + 5, cy + 11),
                            new PointF(cx - 5, cy + 11),
                            new PointF(cx - 9, cy + 5),
                            new PointF(cx - 8, cy - 2)
                        };
                        g.FillPolygon(lb, dropPts);
                        g.DrawPolygon(lp, dropPts);
                    }
                    DrawStarVector(g, cx, cy + 2, 6f, 2.8f, Color.White, true);
                    break;

                case AnimationType.GenOffice_Cinematic3DFlip:
                    // 3D perspective rotated star with golden specular flare
                    using (SolidBrush bShadow = new SolidBrush(Color.FromArgb(100, 79, 70, 229)))
                    {
                        PointF[] skewed = new PointF[] {
                            new PointF(cx - 2, cy - 11),
                            new PointF(cx + 7, cy - 7),
                            new PointF(cx + 3, cy + 9),
                            new PointF(cx - 6, cy + 5)
                        };
                        g.FillPolygon(bShadow, skewed);
                    }
                    DrawStarVector(g, cx, cy, 10f, 4.5f, Color.FromArgb(99, 102, 241), true);
                    // Specular 4-point sparkle flare at top-right
                    using (Pen flarePen = new Pen(gold, 1.8f))
                    {
                        g.DrawLine(flarePen, cx + 5, cy - 11, cx + 13, cy - 11);
                        g.DrawLine(flarePen, cx + 9, cy - 15, cx + 9, cy - 7);
                    }
                    break;

                case AnimationType.GenOffice_GlitchHologram:
                    // RGB split holographic stars + scanlines
                    DrawStarVector(g, cx - 3, cy, 9.5f, 4.2f, Color.FromArgb(160, 239, 68, 68), false);
                    DrawStarVector(g, cx + 3, cy, 9.5f, 4.2f, Color.FromArgb(160, 59, 130, 246), false);
                    DrawStarVector(g, cx, cy, 9.5f, 4.2f, emerald, true);
                    using (Pen scPen = new Pen(Color.FromArgb(180, cyan), 1.0f))
                    {
                        g.DrawLine(scPen, cx - 13, cy - 4, cx + 13, cy - 4);
                        g.DrawLine(scPen, cx - 11, cy + 4, cx + 11, cy + 4);
                    }
                    break;

                case AnimationType.GenOffice_KineticSnap:
                    // Magnetic velocity dual acceleration arrows + spring snap star
                    using (Pen spPen = new Pen(gold, 1.8f))
                    {
                        g.DrawLine(spPen, cx - 14, cy - 7, cx - 8, cy);
                        g.DrawLine(spPen, cx - 8, cy, cx - 14, cy + 7);
                        g.DrawLine(spPen, cx - 10, cy - 7, cx - 4, cy);
                        g.DrawLine(spPen, cx - 4, cy, cx - 10, cy + 7);
                    }
                    DrawStarVector(g, cx + 4, cy, 10.5f, 4.8f, Color.FromArgb(234, 88, 12), true);
                    break;

                case AnimationType.GenOffice_VortexSpiral:
                    // Cosmic spiral curve curling into central star
                    using (Pen vPen = new Pen(Color.FromArgb(147, 51, 234), 1.6f))
                    {
                        PointF[] spiral = new PointF[] {
                            new PointF(cx + 12, cy),
                            new PointF(cx + 8, cy - 10),
                            new PointF(cx - 8, cy - 9),
                            new PointF(cx - 10, cy + 6),
                            new PointF(cx + 3, cy + 8),
                            new PointF(cx + 5, cy - 2),
                            new PointF(cx - 2, cy - 2)
                        };
                        g.DrawCurve(vPen, spiral, 0.6f);
                    }
                    DrawStarVector(g, cx, cy, 8.5f, 3.8f, Color.FromArgb(168, 85, 247), true);
                    break;

                case AnimationType.GenOffice_OrigamiUnfold:
                    // 3D Origami dual-folding facet star
                    using (SolidBrush leftWing = new SolidBrush(Color.FromArgb(52, 211, 153)))
                    using (SolidBrush rightWing = new SolidBrush(Color.FromArgb(5, 150, 105)))
                    using (Pen foldPen = new Pen(Color.FromArgb(4, 120, 87), 1.2f))
                    {
                        PointF[] leftPts = new PointF[] { new PointF(cx, cy - 11), new PointF(cx - 11, cy - 2), new PointF(cx - 6, cy + 10), new PointF(cx, cy + 4) };
                        PointF[] rightPts = new PointF[] { new PointF(cx, cy - 11), new PointF(cx + 11, cy - 2), new PointF(cx + 6, cy + 10), new PointF(cx, cy + 4) };
                        g.FillPolygon(leftWing, leftPts);
                        g.FillPolygon(rightWing, rightPts);
                        g.DrawPolygon(foldPen, leftPts);
                        g.DrawPolygon(foldPen, rightPts);
                        g.DrawLine(foldPen, cx, cy - 11, cx, cy + 4);
                    }
                    break;

                case AnimationType.GenOffice_StarlightAssembly:
                    // 4 Convergent cosmic particle trails + center burst star
                    using (Pen stPen = new Pen(Color.FromArgb(236, 72, 153), 1.4f))
                    using (SolidBrush pB = new SolidBrush(gold))
                    {
                        g.DrawLine(stPen, cx - 13, cy - 10, cx - 5, cy - 4);
                        g.DrawLine(stPen, cx + 13, cy - 10, cx + 5, cy - 4);
                        g.DrawLine(stPen, cx - 13, cy + 10, cx - 5, cy + 4);
                        g.DrawLine(stPen, cx + 13, cy + 10, cx + 5, cy + 4);
                        g.FillEllipse(pB, cx - 14, cy - 11, 4, 4);
                        g.FillEllipse(pB, cx + 11, cy - 11, 4, 4);
                        g.FillEllipse(pB, cx - 14, cy + 8, 4, 4);
                        g.FillEllipse(pB, cx + 11, cy + 8, 4, 4);
                    }
                    DrawStarVector(g, cx, cy, 9.5f, 4.2f, gold, true);
                    break;

                case AnimationType.GenOffice_ElasticJellyPop:
                    // Jelly bounce dome + highlight shine
                    using (SolidBrush jb = new SolidBrush(rose))
                    using (Pen jp = new Pen(Color.FromArgb(225, 29, 72), 1.5f))
                    using (SolidBrush shineB = new SolidBrush(Color.FromArgb(160, 255, 255, 255)))
                    {
                        g.FillEllipse(jb, cx - 12, cy - 9, 24, 20);
                        g.DrawEllipse(jp, cx - 12, cy - 9, 24, 20);
                        g.FillEllipse(shineB, cx - 7, cy - 7, 7, 4);
                    }
                    DrawStarVector(g, cx, cy + 1, 6f, 2.8f, Color.White, true);
                    break;

                case AnimationType.GenOffice_GoldenRibbonSwirl:
                default:
                    // Luxurious Gold Star surrounded by flowing ribbon curve
                    using (Pen ribPen = new Pen(gold, 2.0f))
                    {
                        PointF[] rib = new PointF[] {
                            new PointF(cx - 13, cy + 6),
                            new PointF(cx - 8, cy - 11),
                            new PointF(cx + 8, cy - 9),
                            new PointF(cx + 12, cy + 7),
                            new PointF(cx - 2, cy + 12)
                        };
                        g.DrawCurve(ribPen, rib, 0.7f);
                    }
                    DrawStarVector(g, cx, cy, 9.5f, 4.2f, Color.FromArgb(217, 119, 6), true);
                    break;
            }
        }

        public static void DrawEntranceVector(Graphics g, float cx, float cy, AnimationType type)
        {
            Color green = Color.FromArgb(34, 197, 94);
            switch (type)
            {
                case AnimationType.Appear:
                    DrawStarVector(g, cx, cy, 11, 5, green, true);
                    // Sparkle lines
                    using (Pen sp = new Pen(green, 1.2f))
                    {
                        g.DrawLine(sp, cx - 14, cy, cx - 11, cy);
                        g.DrawLine(sp, cx + 11, cy, cx + 14, cy);
                        g.DrawLine(sp, cx, cy - 14, cx, cy - 11);
                    }
                    break;
                case AnimationType.FadeIn:
                    // Fading green star
                    DrawStarVector(g, cx, cy, 11, 5, Color.FromArgb(120, 34, 197, 94), true);
                    DrawStarVector(g, cx, cy, 11, 5, green, false);
                    break;
                case AnimationType.FlyIn:
                case AnimationType.FloatIn:
                    DrawStarVector(g, cx, cy - 2, 10, 4.5f, green, true);
                    // Upward arrow
                    using (Pen ap = new Pen(green, 1.8f))
                    {
                        ap.EndCap = LineCap.ArrowAnchor;
                        g.DrawLine(ap, cx, cy + 13, cx, cy + 2);
                    }
                    break;
                case AnimationType.SplitIn:
                    DrawStarVector(g, cx - 2, cy, 10, 4.5f, green, true);
                    DrawStarVector(g, cx + 2, cy, 10, 4.5f, green, false);
                    break;
                case AnimationType.WipeIn:
                    DrawStarVector(g, cx, cy, 11, 5, green, true);
                    using (Pen wp = new Pen(Color.White, 2.0f))
                    {
                        g.DrawLine(wp, cx, cy - 10, cx, cy + 10);
                    }
                    break;
                case AnimationType.ShapeIn:
                    using (Pen cp = new Pen(green, 1.2f))
                    {
                        g.DrawEllipse(cp, cx - 13, cy - 13, 26, 26);
                    }
                    DrawStarVector(g, cx, cy, 9, 4, green, true);
                    break;
                case AnimationType.WheelIn:
                    DrawStarVector(g, cx, cy, 10, 4.5f, green, true);
                    using (Pen sp = new Pen(Color.FromArgb(16, 185, 129), 1.0f))
                    {
                        g.DrawLine(sp, cx - 13, cy - 13, cx + 13, cy + 13);
                        g.DrawLine(sp, cx + 13, cy - 13, cx - 13, cy + 13);
                    }
                    break;
                case AnimationType.RandomBarsIn:
                    DrawStarVector(g, cx, cy, 10, 4.5f, green, true);
                    using (Pen bp = new Pen(Color.White, 1.5f))
                    {
                        g.DrawLine(bp, cx - 10, cy - 4, cx + 10, cy - 4);
                        g.DrawLine(bp, cx - 10, cy + 4, cx + 10, cy + 4);
                    }
                    break;
                case AnimationType.GrowAndTurn:
                    DrawStarVector(g, cx - 4, cy + 2, 7, 3, green, false);
                    DrawStarVector(g, cx + 4, cy - 3, 10, 4.5f, green, true);
                    break;
                case AnimationType.ZoomIn:
                    DrawStarVector(g, cx, cy, 10, 4.5f, green, true);
                    using (Pen zp = new Pen(green, 1.5f))
                    {
                        g.DrawLine(zp, cx - 12, cy - 12, cx - 8, cy - 8);
                        g.DrawLine(zp, cx + 12, cy - 12, cx + 8, cy - 8);
                        g.DrawLine(zp, cx - 12, cy + 12, cx - 8, cy + 8);
                        g.DrawLine(zp, cx + 12, cy + 12, cx + 8, cy + 8);
                    }
                    break;
                case AnimationType.Swivel:
                    DrawStarVector(g, cx, cy, 10, 4.5f, green, true);
                    using (Pen swp = new Pen(Color.FromArgb(16, 185, 129), 1.5f))
                    {
                        g.DrawArc(swp, cx - 13, cy - 6, 26, 12, 0, 180);
                    }
                    break;
                case AnimationType.Bounce:
                    DrawStarVector(g, cx, cy - 3, 10, 4.5f, green, true);
                    using (Pen bnp = new Pen(green, 1.2f))
                    {
                        g.DrawArc(bnp, cx - 12, cy + 4, 12, 8, 180, 180);
                        g.DrawArc(bnp, cx, cy + 6, 12, 6, 180, 180);
                    }
                    break;
                default:
                    DrawStarVector(g, cx, cy, 11, 5, green, true);
                    break;
            }
        }

        public static void DrawEmphasisVector(Graphics g, float cx, float cy, AnimationType type)
        {
            Color orange = Color.FromArgb(234, 179, 8);
            switch (type)
            {
                case AnimationType.Pulse:
                case AnimationType.ColorPulse:
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, true);
                    using (Pen pp = new Pen(Color.FromArgb(245, 158, 11), 1.2f))
                    {
                        g.DrawArc(pp, cx - 14, cy - 10, 6, 20, 90, 180);
                        g.DrawArc(pp, cx + 8, cy - 10, 6, 20, 270, 180);
                    }
                    break;
                case AnimationType.Teeter:
                    GraphicsState state = g.Save();
                    g.TranslateTransform(cx, cy);
                    g.RotateTransform(-18);
                    DrawStarVector(g, 0, 0, 10, 4.5f, orange, true);
                    g.Restore(state);
                    break;
                case AnimationType.Spin:
                    DrawStarVector(g, cx, cy, 9, 4, orange, true);
                    using (Pen sp = new Pen(Color.FromArgb(217, 119, 6), 1.6f))
                    {
                        sp.EndCap = LineCap.ArrowAnchor;
                        g.DrawArc(sp, cx - 13, cy - 13, 26, 26, 45, 270);
                    }
                    break;
                case AnimationType.GrowShrink:
                    DrawStarVector(g, cx - 5, cy + 2, 7, 3, orange, false);
                    DrawStarVector(g, cx + 5, cy - 3, 11, 5, orange, true);
                    break;
                case AnimationType.Desaturate:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(148, 163, 184), true);
                    break;
                case AnimationType.Darken:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(180, 83, 9), true);
                    break;
                case AnimationType.Lighten:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(254, 240, 138), true);
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, false);
                    break;
                case AnimationType.Transparency:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(120, 234, 179, 8), true);
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, false);
                    break;
                case AnimationType.ObjectColor:
                case AnimationType.FillColor:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(37, 99, 235), true);
                    break;
                case AnimationType.ComplementaryColor:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(236, 72, 153), true);
                    break;
                case AnimationType.LineColor:
                    DrawStarVector(g, cx, cy, 10, 4.5f, Color.FromArgb(37, 99, 235), false);
                    break;
                case AnimationType.FontColor:
                case AnimationType.BrushColor:
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, false);
                    using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                    {
                        g.DrawString("A", f, Brushes.Crimson, cx - 5, cy - 7);
                    }
                    break;
                case AnimationType.Underline:
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, false);
                    using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Underline | FontStyle.Bold))
                    {
                        g.DrawString("U", f, Brushes.Navy, cx - 5, cy - 7);
                    }
                    break;
                case AnimationType.BoldFlash:
                case AnimationType.BoldReveal:
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, false);
                    using (Font f = new Font("Segoe UI", 9.0f, FontStyle.Bold))
                    {
                        g.DrawString("B", f, Brushes.DarkGoldenrod, cx - 5, cy - 7);
                    }
                    break;
                case AnimationType.Wave:
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, false);
                    using (Pen wp = new Pen(orange, 1.5f))
                    {
                        g.DrawBezier(wp, cx - 10, cy + 8, cx - 5, cy + 4, cx + 5, cy + 12, cx + 10, cy + 8);
                    }
                    break;
                default:
                    DrawStarVector(g, cx, cy, 10, 4.5f, orange, true);
                    break;
            }
        }

        public static void DrawExitVector(Graphics g, float cx, float cy, AnimationType type)
        {
            Color red = Color.FromArgb(239, 68, 68);
            switch (type)
            {
                case AnimationType.Disappear:
                    DrawStarVector(g, cx, cy, 11, 5, red, true);
                    using (Pen dp = new Pen(red, 1.2f))
                    {
                        g.DrawLine(dp, cx - 14, cy - 6, cx - 10, cy - 3);
                        g.DrawLine(dp, cx + 14, cy - 6, cx + 10, cy - 3);
                    }
                    break;
                case AnimationType.FadeOut:
                    DrawStarVector(g, cx, cy, 11, 5, Color.FromArgb(100, 239, 68, 68), true);
                    DrawStarVector(g, cx, cy, 11, 5, red, false);
                    break;
                case AnimationType.FlyOut:
                case AnimationType.FloatOut:
                    DrawStarVector(g, cx, cy + 2, 10, 4.5f, red, true);
                    using (Pen ap = new Pen(red, 1.8f))
                    {
                        ap.EndCap = LineCap.ArrowAnchor;
                        g.DrawLine(ap, cx, cy - 13, cx, cy - 2);
                    }
                    break;
                case AnimationType.SplitOut:
                    DrawStarVector(g, cx - 2, cy, 10, 4.5f, red, true);
                    DrawStarVector(g, cx + 2, cy, 10, 4.5f, red, false);
                    break;
                case AnimationType.WipeOut:
                    DrawStarVector(g, cx, cy, 11, 5, red, true);
                    using (Pen wp = new Pen(Color.White, 2.0f))
                    {
                        g.DrawLine(wp, cx, cy - 10, cx, cy + 10);
                    }
                    break;
                case AnimationType.ShapeOut:
                    using (Pen cp = new Pen(red, 1.2f))
                    {
                        g.DrawEllipse(cp, cx - 13, cy - 13, 26, 26);
                    }
                    DrawStarVector(g, cx, cy, 8, 3.5f, red, true);
                    break;
                case AnimationType.WheelOut:
                    DrawStarVector(g, cx, cy, 10, 4.5f, red, true);
                    using (Pen sp = new Pen(Color.FromArgb(220, 38, 38), 1.0f))
                    {
                        g.DrawLine(sp, cx - 13, cy - 13, cx + 13, cy + 13);
                        g.DrawLine(sp, cx + 13, cy - 13, cx - 13, cy + 13);
                    }
                    break;
                case AnimationType.RandomBarsOut:
                    DrawStarVector(g, cx, cy, 10, 4.5f, red, true);
                    using (Pen bp = new Pen(Color.White, 1.5f))
                    {
                        g.DrawLine(bp, cx - 10, cy - 4, cx + 10, cy - 4);
                        g.DrawLine(bp, cx - 10, cy + 4, cx + 10, cy + 4);
                    }
                    break;
                case AnimationType.ShrinkAndTurn:
                    DrawStarVector(g, cx + 4, cy - 2, 10, 4.5f, red, true);
                    DrawStarVector(g, cx - 4, cy + 3, 6, 2.5f, red, false);
                    break;
                case AnimationType.ZoomOut:
                    DrawStarVector(g, cx, cy, 10, 4.5f, red, true);
                    using (Pen zp = new Pen(red, 1.5f))
                    {
                        zp.EndCap = LineCap.ArrowAnchor;
                        g.DrawLine(zp, cx - 12, cy - 12, cx - 6, cy - 6);
                        g.DrawLine(zp, cx + 12, cy + 12, cx + 6, cy + 6);
                    }
                    break;
                case AnimationType.SwivelOut:
                    DrawStarVector(g, cx, cy, 10, 4.5f, red, true);
                    using (Pen swp = new Pen(Color.FromArgb(220, 38, 38), 1.5f))
                    {
                        g.DrawArc(swp, cx - 13, cy - 6, 26, 12, 180, 180);
                    }
                    break;
                case AnimationType.BounceOut:
                    DrawStarVector(g, cx, cy - 3, 10, 4.5f, red, true);
                    using (Pen bnp = new Pen(red, 1.2f))
                    {
                        g.DrawArc(bnp, cx - 12, cy + 4, 12, 8, 0, 180);
                    }
                    break;
                default:
                    DrawStarVector(g, cx, cy, 11, 5, red, true);
                    break;
            }
        }

        public static void DrawMotionPathVector(Graphics g, float cx, float cy, AnimationType type)
        {
            Color green = Color.FromArgb(34, 197, 94);
            Color red = Color.FromArgb(239, 68, 68);

            switch (type)
            {
                case AnimationType.MotionLine:
                    using (Pen lp = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
                    {
                        g.DrawLine(lp, cx, cy - 10, cx, cy + 10);
                    }
                    using (SolidBrush gb = new SolidBrush(green)) g.FillEllipse(gb, cx - 3, cy - 12, 6, 6);
                    using (SolidBrush rb = new SolidBrush(red)) g.FillEllipse(rb, cx - 3, cy + 8, 6, 6);
                    break;
                case AnimationType.MotionArc:
                    using (Pen ap = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
                    {
                        g.DrawArc(ap, cx - 12, cy - 8, 24, 16, 180, 180);
                    }
                    using (SolidBrush gb = new SolidBrush(green)) g.FillEllipse(gb, cx - 14, cy, 6, 6);
                    using (SolidBrush rb = new SolidBrush(red)) g.FillEllipse(rb, cx + 8, cy, 6, 6);
                    break;
                case AnimationType.MotionTurn:
                    using (Pen tp = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
                    {
                        PointF[] turnPts = new PointF[] { new PointF(cx - 10, cy - 10), new PointF(cx + 8, cy - 10), new PointF(cx + 8, cy + 10) };
                        g.DrawLines(tp, turnPts);
                    }
                    using (SolidBrush gb = new SolidBrush(green)) g.FillEllipse(gb, cx - 13, cy - 13, 6, 6);
                    using (SolidBrush rb = new SolidBrush(red)) g.FillEllipse(rb, cx + 5, cy + 7, 6, 6);
                    break;
                case AnimationType.MotionShape:
                    using (Pen sp = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
                    {
                        g.DrawEllipse(sp, cx - 11, cy - 11, 22, 22);
                    }
                    using (SolidBrush gb = new SolidBrush(green)) g.FillEllipse(gb, cx - 3, cy - 14, 6, 6);
                    break;
                case AnimationType.MotionLoop:
                    using (Pen lop = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
                    {
                        g.DrawEllipse(lop, cx - 13, cy - 6, 12, 12);
                        g.DrawEllipse(lop, cx + 1, cy - 6, 12, 12);
                    }
                    using (SolidBrush gb = new SolidBrush(green)) g.FillEllipse(gb, cx - 15, cy - 3, 6, 6);
                    break;
                default: // Custom Path
                    using (Pen cp = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
                    {
                        PointF[] sPts = new PointF[] {
                            new PointF(cx, cy - 12),
                            new PointF(cx + 6, cy - 6),
                            new PointF(cx - 6, cy),
                            new PointF(cx + 6, cy + 6),
                            new PointF(cx, cy + 12)
                        };
                        g.DrawCurve(cp, sPts);
                    }
                    using (SolidBrush gb = new SolidBrush(green)) g.FillEllipse(gb, cx - 3, cy - 14, 6, 6);
                    using (SolidBrush rb = new SolidBrush(red)) g.FillEllipse(rb, cx - 3, cy + 9, 6, 6);
                    break;
            }
        }

        public static void DrawStarVector(Graphics g, float cx, float cy, float rOuter, float rInner, Color col, bool isFilled = true)
        {
            PointF[] pts = new PointF[10];
            double step = Math.PI / 5.0;
            double angle = -Math.PI / 2.0;
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? rOuter : rInner;
                pts[i] = new PointF(cx + (float)(r * Math.Cos(angle)), cy + (float)(r * Math.Sin(angle)));
                angle += step;
            }
            if (isFilled)
            {
                using (SolidBrush b = new SolidBrush(col))
                {
                    g.FillPolygon(b, pts);
                }
            }
            using (Pen p = new Pen(col, 1.2f))
            {
                g.DrawPolygon(p, pts);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public class TextEffectPreset
    {
        public string Name;
        public Color Color;
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public Color Highlight;
        public Color GlowColor;
        public bool HasOutline;
        public Color OutlineColor;

        public TextEffectPreset(string name, Color color, bool bold, bool italic, bool underline, Color highlight, Color glowColor, bool hasOutline, Color outlineColor)
        {
            Name = name;
            Color = color;
            Bold = bold;
            Italic = italic;
            Underline = underline;
            Highlight = highlight;
            GlowColor = glowColor;
            HasOutline = hasOutline;
            OutlineColor = outlineColor;
        }
    }

    public class TextEffectsDropDownForm : Form
    {
        private readonly Action<TextEffectPreset, bool> _callback;
        private int _hoverPresetIndex = -1;
        private int _hoverMenuItemIndex = -1;

        private static readonly TextEffectPreset[] Presets = new TextEffectPreset[]
        {
            // Row 1
            new TextEffectPreset("Classic Blue Outline & Shadow", Color.FromArgb(16, 110, 190), true, false, false, Color.Transparent, Color.FromArgb(186, 230, 253), true, Color.FromArgb(16, 110, 190)),
            new TextEffectPreset("Orange Sunset & Warm Glow", Color.FromArgb(234, 88, 12), true, false, false, Color.Transparent, Color.FromArgb(254, 215, 170), false, Color.Empty),
            new TextEffectPreset("Gold Luxury Metallic", Color.FromArgb(217, 119, 6), true, false, false, Color.Transparent, Color.FromArgb(254, 240, 138), false, Color.Empty),
            new TextEffectPreset("Emerald Green Accent", Color.FromArgb(5, 150, 105), true, false, false, Color.Transparent, Color.FromArgb(167, 243, 208), false, Color.Empty),
            new TextEffectPreset("Royal Purple Neon", Color.FromArgb(124, 58, 237), true, false, false, Color.Transparent, Color.FromArgb(221, 214, 254), false, Color.Empty),

            // Row 2
            new TextEffectPreset("Crimson Impact", Color.FromArgb(220, 38, 38), true, false, false, Color.Transparent, Color.FromArgb(254, 202, 202), false, Color.Empty),
            new TextEffectPreset("Slate Metallic & Shadow", Color.FromArgb(71, 85, 105), true, true, false, Color.Transparent, Color.FromArgb(203, 213, 225), false, Color.Empty),
            new TextEffectPreset("Cyan Modern Outline", Color.FromArgb(8, 145, 178), true, false, false, Color.Transparent, Color.FromArgb(165, 243, 252), true, Color.FromArgb(8, 145, 178)),
            new TextEffectPreset("Rose Elegance", Color.FromArgb(225, 29, 72), true, true, false, Color.Transparent, Color.FromArgb(251, 207, 232), false, Color.Empty),
            new TextEffectPreset("Deep Indigo Prestige", Color.FromArgb(67, 56, 202), true, false, false, Color.Transparent, Color.FromArgb(199, 210, 254), false, Color.Empty),

            // Row 3
            new TextEffectPreset("Forest Pine Shadow", Color.FromArgb(22, 101, 52), true, false, false, Color.Transparent, Color.FromArgb(187, 247, 208), false, Color.Empty),
            new TextEffectPreset("Bronze Copper Tone", Color.FromArgb(194, 65, 12), true, false, false, Color.Transparent, Color.FromArgb(255, 237, 213), false, Color.Empty),
            new TextEffectPreset("Teal Minimalist", Color.FromArgb(15, 118, 110), true, false, false, Color.Transparent, Color.FromArgb(153, 246, 228), false, Color.Empty),
            new TextEffectPreset("Night Navy Classic", Color.FromArgb(30, 58, 138), true, false, false, Color.Transparent, Color.FromArgb(191, 219, 254), false, Color.Empty),
            new TextEffectPreset("Charcoal Sleek", Color.FromArgb(31, 41, 55), false, false, false, Color.Transparent, Color.FromArgb(229, 231, 235), false, Color.Empty)
        };

        private static readonly string[] MenuItems = new string[]
        {
            "Đường viền chữ (Outline)",
            "Đổ bóng chữ (Shadow)",
            "Phản chiếu chữ (Reflection)",
            "Hiệu ứng phát sáng (Glow)",
            "Xoá tất cả hiệu ứng chữ (Clear Effects)"
        };

        public TextEffectsDropDownForm(Point screenPos, Action<TextEffectPreset, bool> callback)
        {
            _callback = callback;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.White;
            DoubleBuffered = true;

            int w = 264;
            int h = 312;

            Rectangle wa = Screen.GetWorkingArea(screenPos);
            int x = screenPos.X;
            int y = screenPos.Y;
            if (x + w > wa.Right) x = wa.Right - w - 5;
            if (y + h > wa.Bottom) y = wa.Bottom - h - 5;
            if (x < wa.Left) x = wa.Left + 5;
            if (y < wa.Top) y = wa.Top + 5;

            Location = new Point(x, y);
            Size = new Size(w, h);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            Deactivate += delegate(object s, EventArgs e) { Close(); };
        }

        private Rectangle GetPresetBoxRect(int index)
        {
            int boxW = 42;
            int boxH = 40;
            int gapX = 6;
            int gapY = 6;
            int startX = 12;
            int startY = 32;

            int r = index / 5;
            int c = index % 5;
            return new Rectangle(startX + c * (boxW + gapX), startY + r * (boxH + gapY), boxW, boxH);
        }

        private Rectangle GetMenuItemRect(int index)
        {
            int startY = 176;
            int itemH = 25;
            if (index < 4)
            {
                return new Rectangle(8, startY + index * itemH, Width - 16, itemH);
            }
            else
            {
                // Item 4 (Clear) is after a divider line
                return new Rectangle(8, startY + 4 * itemH + 6, Width - 16, itemH);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int oldPreset = _hoverPresetIndex;
            int oldMenu = _hoverMenuItemIndex;

            _hoverPresetIndex = -1;
            _hoverMenuItemIndex = -1;

            for (int i = 0; i < Presets.Length; i++)
            {
                if (GetPresetBoxRect(i).Contains(e.Location))
                {
                    _hoverPresetIndex = i;
                    break;
                }
            }

            if (_hoverPresetIndex == -1)
            {
                for (int i = 0; i < MenuItems.Length; i++)
                {
                    if (GetMenuItemRect(i).Contains(e.Location))
                    {
                        _hoverMenuItemIndex = i;
                        break;
                    }
                }
            }

            if (oldPreset != _hoverPresetIndex || oldMenu != _hoverMenuItemIndex)
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverPresetIndex = -1;
            _hoverMenuItemIndex = -1;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;

            if (_hoverPresetIndex >= 0 && _hoverPresetIndex < Presets.Length)
            {
                if (_callback != null)
                {
                    _callback(Presets[_hoverPresetIndex], false);
                }
                Close();
                return;
            }

            if (_hoverMenuItemIndex >= 0 && _hoverMenuItemIndex < MenuItems.Length)
            {
                if (_hoverMenuItemIndex == 4) // Clear Effects
                {
                    if (_callback != null)
                    {
                        _callback(null, true);
                    }
                    Close();
                    return;
                }
                else if (_hoverMenuItemIndex == 0) // Outline
                {
                    if (_callback != null) _callback(Presets[0], false);
                    Close();
                    return;
                }
                else if (_hoverMenuItemIndex == 1) // Shadow
                {
                    if (_callback != null) _callback(Presets[6], false);
                    Close();
                    return;
                }
                else if (_hoverMenuItemIndex == 2) // Reflection
                {
                    if (_callback != null) _callback(Presets[9], false);
                    Close();
                    return;
                }
                else if (_hoverMenuItemIndex == 3) // Glow
                {
                    if (_callback != null) _callback(Presets[1], false);
                    Close();
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Form Border
            using (Pen borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header Title
            using (Font titleFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Kiểu chữ nghệ thuật & Hiệu ứng", titleFont, titleBrush, 12, 9);
            }

            // Presets Grid
            for (int i = 0; i < Presets.Length; i++)
            {
                Rectangle rBox = GetPresetBoxRect(i);
                bool isHover = (i == _hoverPresetIndex);

                if (isHover)
                {
                    using (SolidBrush hoverBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    using (Pen hoverPen = new Pen(Color.FromArgb(148, 163, 184), 1.0f))
                    {
                        g.FillRectangle(hoverBrush, rBox);
                        g.DrawRectangle(hoverPen, rBox.X, rBox.Y, rBox.Width - 1, rBox.Height - 1);
                    }
                }
                else
                {
                    using (Pen boxBorder = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
                    {
                        g.DrawRectangle(boxBorder, rBox.X, rBox.Y, rBox.Width - 1, rBox.Height - 1);
                    }
                }

                // Render preset letter 'A'
                TextEffectPreset p = Presets[i];
                float letterX = rBox.X + (rBox.Width / 2f) - 9f;
                float letterY = rBox.Y + (rBox.Height / 2f) - 13f;

                using (Font letterFont = new Font("Segoe UI", 15.0f, FontStyle.Bold))
                {
                    // Glow / Soft Shadow
                    if (p.GlowColor != Color.Empty && p.GlowColor != Color.Transparent)
                    {
                        using (SolidBrush glowB = new SolidBrush(p.GlowColor))
                        {
                            g.DrawString("A", letterFont, glowB, letterX + 1.2f, letterY + 1.2f);
                        }
                    }

                    // Main Body or Outline
                    if (p.HasOutline)
                    {
                        using (SolidBrush whiteB = new SolidBrush(Color.White))
                        using (Pen outlineP = new Pen(p.OutlineColor, 1.5f) { LineJoin = LineJoin.Round })
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            gp.AddString("A", letterFont.FontFamily, (int)FontStyle.Bold, 20f, new PointF(letterX, letterY), StringFormat.GenericDefault);
                            g.FillPath(whiteB, gp);
                            g.DrawPath(outlineP, gp);
                        }
                    }
                    else
                    {
                        using (SolidBrush letterBrush = new SolidBrush(p.Color))
                        {
                            g.DrawString("A", letterFont, letterBrush, letterX, letterY);
                        }
                    }
                }
            }

            // Divider line above Menu Items
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.DrawLine(divPen, 10, 172, Width - 10, 172);
            }

            // Menu Items
            using (Font menuFont = new Font("Segoe UI", 8.75f, FontStyle.Regular))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(38, 38, 42)))
            using (SolidBrush redBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
            using (Pen chevronPen = new Pen(Color.FromArgb(148, 163, 184), 1.2f))
            {
                for (int i = 0; i < MenuItems.Length; i++)
                {
                    Rectangle rItem = GetMenuItemRect(i);
                    bool isHover = (i == _hoverMenuItemIndex);

                    if (isHover)
                    {
                        using (SolidBrush itemHover = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        using (Pen itemBorder = new Pen(Color.FromArgb(203, 213, 225), 1f))
                        {
                            g.FillRectangle(itemHover, rItem);
                            g.DrawRectangle(itemBorder, rItem.X, rItem.Y, rItem.Width - 1, rItem.Height - 1);
                        }
                    }

                    // Draw Icon
                    float iconX = rItem.X + 8;
                    float iconY = rItem.Y + 4;

                    if (i == 0) // Outline icon
                    {
                        using (Pen pOut = new Pen(Color.FromArgb(16, 110, 190), 1.2f))
                        using (Font fMini = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            using (GraphicsPath gp = new GraphicsPath())
                            {
                                gp.AddString("A", fMini.FontFamily, (int)FontStyle.Bold, 11f, new PointF(iconX, iconY), StringFormat.GenericDefault);
                                g.DrawPath(pOut, gp);
                            }
                        }
                    }
                    else if (i == 1) // Shadow icon
                    {
                        using (Font fMini = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        using (SolidBrush bShadow = new SolidBrush(Color.FromArgb(180, 185, 195)))
                        using (SolidBrush bMain = new SolidBrush(Color.FromArgb(38, 38, 42)))
                        {
                            g.DrawString("A", fMini, bShadow, iconX + 1.5f, iconY + 1.5f);
                            g.DrawString("A", fMini, bMain, iconX, iconY);
                        }
                    }
                    else if (i == 2) // Reflection icon
                    {
                        using (Font fMini = new Font("Segoe UI", 8.0f, FontStyle.Bold))
                        using (SolidBrush bMain = new SolidBrush(Color.FromArgb(38, 38, 42)))
                        using (SolidBrush bRefl = new SolidBrush(Color.FromArgb(160, 200, 235)))
                        {
                            g.DrawString("A", fMini, bMain, iconX, iconY - 2);
                            g.DrawString("v", fMini, bRefl, iconX + 1f, iconY + 7);
                        }
                    }
                    else if (i == 3) // Glow icon
                    {
                        using (Font fMini = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        using (SolidBrush bGlow = new SolidBrush(Color.FromArgb(254, 215, 170)))
                        using (SolidBrush bMain = new SolidBrush(Color.FromArgb(234, 88, 12)))
                        {
                            g.DrawString("A", fMini, bGlow, iconX + 1f, iconY + 1f);
                            g.DrawString("A", fMini, bMain, iconX, iconY);
                        }
                    }
                    else if (i == 4) // Clear Effects (Eraser)
                    {
                        using (Pen pEraser = new Pen(Color.FromArgb(220, 38, 38), 1.2f))
                        using (SolidBrush bEraser = new SolidBrush(Color.FromArgb(254, 226, 226)))
                        {
                            PointF[] poly = new PointF[] {
                                new PointF(iconX + 3f, iconY + 1f),
                                new PointF(iconX + 12f, iconY + 1f),
                                new PointF(iconX + 9f, iconY + 14f),
                                new PointF(iconX, iconY + 14f)
                            };
                            g.FillPolygon(bEraser, poly);
                            g.DrawPolygon(pEraser, poly);
                            g.DrawLine(pEraser, iconX + 4.5f, iconY + 7f, iconX + 10.5f, iconY + 7f);
                        }
                    }

                    // Text
                    SolidBrush itemBrush = (i == 4) ? redBrush : textBrush;
                    g.DrawString(MenuItems[i], menuFont, itemBrush, rItem.X + 30, rItem.Y + 4);

                    // Right arrow chevron for submenus
                    if (i < 4)
                    {
                        float chvX = rItem.Right - 14;
                        float chvY = rItem.Y + 8;
                        g.DrawLine(chevronPen, chvX, chvY, chvX + 3.5f, chvY + 3.5f);
                        g.DrawLine(chevronPen, chvX + 3.5f, chvY + 3.5f, chvX, chvY + 7f);
                    }
                }
            }

            // Divider line above Clear
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                Rectangle rClear = GetMenuItemRect(4);
                g.DrawLine(divPen, 10, rClear.Y - 4, Width - 10, rClear.Y - 4);
            }
        }
    }
}

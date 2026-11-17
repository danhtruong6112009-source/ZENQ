using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public enum GraphicPopoverType
    {
        Stroke,
        CornerRadius,
        Flip,
        Transparency,
        Shadow
    }

    public class GraphicPopoverForm : Form
    {
        private GraphicPopoverType _type;
        private ShapeNode _shape;
        private SlidesView _slidesView;
        private Form _parentForm;

        // Hover / Drag states
        private int _hoveredDashIdx = -1;
        private int _hoveredFlipIdx = -1;
        private bool _isDraggingSlider1 = false;
        private bool _isDraggingSlider2 = false;
        private bool _savedUndo = false;

        // Layout bounds
        private Rectangle[] _dashCardBounds = new Rectangle[5];
        private Rectangle _strokeColorSwatchBounds = Rectangle.Empty;
        private Rectangle _sliderTrack1Bounds = Rectangle.Empty;
        private Rectangle _sliderTrack2Bounds = Rectangle.Empty;
        private Rectangle _valuePill1Bounds = Rectangle.Empty;
        private Rectangle _valuePill2Bounds = Rectangle.Empty;
        private Rectangle _flipHBounds = Rectangle.Empty;
        private Rectangle _flipVBounds = Rectangle.Empty;

        private static readonly ShapeType[] PolygonSidesMap = new ShapeType[]
        {
            ShapeType.Triangle,      // 3
            ShapeType.Rectangle,     // 4
            ShapeType.Pentagon,      // 5
            ShapeType.Hexagon,       // 6
            ShapeType.Octagon,       // 8
            ShapeType.Starburst      // 12
        };

        public GraphicPopoverForm(Point screenPos, GraphicPopoverType type, ShapeNode shape, SlidesView slidesView, Form parentForm)
        {
            _type = type;
            _shape = shape;
            _slidesView = slidesView;
            _parentForm = parentForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.White;

            switch (_type)
            {
                case GraphicPopoverType.Stroke:
                    Size = new Size(310, 152);
                    break;
                case GraphicPopoverType.CornerRadius:
                    Size = new Size(295, 142);
                    break;
                case GraphicPopoverType.Flip:
                    Size = new Size(190, 94);
                    break;
                case GraphicPopoverType.Transparency:
                    Size = new Size(285, 86);
                    break;
                case GraphicPopoverType.Shadow:
                    Size = new Size(295, 146);
                    break;
            }

            Rectangle screen = Screen.FromPoint(screenPos).WorkingArea;
            int fx = Math.Max(screen.Left + 8, Math.Min(screenPos.X, screen.Right - Width - 8));
            int fy = Math.Max(screen.Top + 8, Math.Min(screenPos.Y, screen.Bottom - Height - 8));
            Location = new Point(fx, fy);

            Deactivate += (s, e) => { try { Close(); } catch { } };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        private void ApplyToSelectedShapes(Action<ShapeNode> action)
        {
            if (_slidesView != null && _slidesView.SelectedShapeIndices != null && _slidesView.SelectedShapeIndices.Count > 1 && _slidesView.ActiveSlide != null)
            {
                foreach (int sIdx in _slidesView.SelectedShapeIndices)
                {
                    if (sIdx >= 0 && sIdx < _slidesView.ActiveSlide.Shapes.Count)
                    {
                        action(_slidesView.ActiveSlide.Shapes[sIdx]);
                    }
                }
            }
            else if (_shape != null)
            {
                action(_shape);
            }
        }

        private void EnsureUndoSaved()
        {
            if (!_savedUndo && _slidesView != null)
            {
                _slidesView.SaveUndoState();
                _savedUndo = true;
            }
        }

        private void RefreshCanvas()
        {
            if (_shape != null)
            {
                _shape.InvalidateDisplayCache();
                _shape.InvalidateShadowCache();
            }
            if (_slidesView != null)
            {
                if (_slidesView.ActiveSlide != null) _slidesView.ActiveSlide.IsThumbnailDirty = true;
                _slidesView.Invalidate();
            }
            if (_parentForm != null) _parentForm.Invalidate();
            Invalidate();
        }

        private int GetCurrentSideCount()
        {
            if (_shape == null) return 4;
            switch (_shape.Type)
            {
                case ShapeType.Triangle:
                case ShapeType.RightTriangle:
                    return 3;
                case ShapeType.Pentagon:
                    return 5;
                case ShapeType.Hexagon:
                case ShapeType.LuxuryHexagon:
                    return 6;
                case ShapeType.Octagon:
                    return 8;
                case ShapeType.Starburst:
                    return 12;
                default:
                    return 4;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
            {
                g.FillRectangle(bg, borderRect);
                g.DrawRectangle(borderPen, borderRect);
            }

            if (_shape == null) return;

            switch (_type)
            {
                case GraphicPopoverType.Stroke:
                    DrawStrokePopover(g);
                    break;
                case GraphicPopoverType.CornerRadius:
                    DrawCornerRadiusPopover(g);
                    break;
                case GraphicPopoverType.Flip:
                    DrawFlipPopover(g);
                    break;
                case GraphicPopoverType.Transparency:
                    DrawTransparencyPopover(g);
                    break;
                case GraphicPopoverType.Shadow:
                    DrawShadowPopover(g);
                    break;
            }
        }

        private void DrawStrokePopover(Graphics g)
        {
            // 5 Canva-style dash cards: 0=None, 1=Solid, 2=Long Dash, 3=Short Dash, 4=Dotted
            int cardW = 50;
            int cardH = 38;
            int gap = 8;
            int startX = 14;
            int topY = 14;

            int activeIdx = 0;
            if (_shape.StrokeWidth > 0.05f)
            {
                if (_shape.DashStyle == DashStyle.Solid) activeIdx = 1;
                else if (_shape.DashStyle == DashStyle.Dash) activeIdx = 2;
                else if (_shape.DashStyle == DashStyle.DashDot) activeIdx = 3;
                else if (_shape.DashStyle == DashStyle.Dot) activeIdx = 4;
                else activeIdx = 1;
            }

            for (int i = 0; i < 5; i++)
            {
                Rectangle r = new Rectangle(startX + i * (cardW + gap), topY, cardW, cardH);
                _dashCardBounds[i] = r;

                bool isSelected = (i == activeIdx);
                bool isHovered = (i == _hoveredDashIdx);

                Color fillC = isSelected ? Color.FromArgb(245, 243, 255) : (isHovered ? Color.FromArgb(248, 250, 252) : Color.White);
                Color bdrC = isSelected ? Color.FromArgb(124, 58, 237) : Color.FromArgb(203, 213, 225);
                float bdrW = isSelected ? 2.0f : 1.0f;

                using (GraphicsPath gp = CreateRoundedRect(r, 8))
                using (SolidBrush fb = new SolidBrush(fillC))
                using (Pen bp = new Pen(bdrC, bdrW))
                {
                    g.FillPath(fb, gp);
                    g.DrawPath(bp, gp);
                }

                int cy = r.Y + r.Height / 2;
                if (i == 0)
                {
                    // Ban / No-stroke icon
                    Rectangle cRect = new Rectangle(r.X + (r.Width - 18) / 2, r.Y + (r.Height - 18) / 2, 18, 18);
                    using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.7f))
                    {
                        g.DrawEllipse(p, cRect);
                        g.DrawLine(p, cRect.X + 3, cRect.Bottom - 3, cRect.Right - 3, cRect.Y + 3);
                    }
                }
                else if (i == 1)
                {
                    using (Pen p = new Pen(Color.FromArgb(30, 41, 59), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(p, r.X + 11, cy, r.Right - 11, cy);
                    }
                }
                else if (i == 2)
                {
                    using (Pen p = new Pen(Color.FromArgb(30, 41, 59), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(p, r.X + 10, cy, r.X + 22, cy);
                        g.DrawLine(p, r.X + 28, cy, r.Right - 10, cy);
                    }
                }
                else if (i == 3)
                {
                    using (Pen p = new Pen(Color.FromArgb(30, 41, 59), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(p, r.X + 10, cy, r.X + 16, cy);
                        g.DrawLine(p, r.X + 22, cy, r.X + 28, cy);
                        g.DrawLine(p, r.X + 34, cy, r.X + 40, cy);
                    }
                }
                else if (i == 4)
                {
                    using (SolidBrush db = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    {
                        for (int dx = 12; dx <= 38; dx += 6)
                        {
                            g.FillEllipse(db, r.X + dx - 2, cy - 2, 4, 4);
                        }
                    }
                }
            }

            // Stroke Weight label + color swatch
            using (Font lblFont = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString(AppL10n.Tr("Độ đậm đường viền"), lblFont, txtB, 14, 66);
            }

            // Color swatch circle on the right of label
            _strokeColorSwatchBounds = new Rectangle(Width - 36, 64, 20, 20);
            Color sc = Color.FromArgb(255, Color.FromArgb(_shape.StrokeColor));
            using (SolidBrush sb = new SolidBrush(sc))
            using (Pen sp = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
            {
                g.FillEllipse(sb, _strokeColorSwatchBounds);
                g.DrawEllipse(sp, _strokeColorSwatchBounds);
            }

            float val = Math.Max(0f, Math.Min(50f, _shape.StrokeWidth));
            DrawCanvaSliderRow(g, 96, val, 0f, 50f, ((int)Math.Round(val)).ToString(), out _sliderTrack1Bounds, out _valuePill1Bounds);
        }

        private void DrawCornerRadiusPopover(Graphics g)
        {
            using (Font lblFont = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString(AppL10n.Tr("Bo tròn góc"), lblFont, txtB, 14, 14);
                float crVal = Math.Max(0f, Math.Min(100f, _shape.CornerRadius));
                DrawCanvaSliderRow(g, 38, crVal, 0f, 100f, ((int)Math.Round(crVal)).ToString(), out _sliderTrack1Bounds, out _valuePill1Bounds);

                g.DrawString(AppL10n.Tr("Cạnh"), lblFont, txtB, 14, 76);
                int sides = GetCurrentSideCount();
                DrawCanvaSliderRow(g, 100, sides, 3f, 12f, sides.ToString(), out _sliderTrack2Bounds, out _valuePill2Bounds);
            }
        }

        private void DrawTransparencyPopover(Graphics g)
        {
            using (Font lblFont = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString(AppL10n.Tr("Độ trong suốt"), lblFont, txtB, 14, 14);
                float opVal = Math.Max(0f, Math.Min(100f, _shape.FillOpacity * 100f));
                DrawCanvaSliderRow(g, 40, opVal, 0f, 100f, ((int)Math.Round(opVal)).ToString(), out _sliderTrack1Bounds, out _valuePill1Bounds);
            }
        }

        private void DrawShadowPopover(Graphics g)
        {
            using (Font lblFont = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString(AppL10n.Tr("Độ mờ bóng đổ"), lblFont, txtB, 14, 14);
                float blurVal = _shape.HasShadow ? Math.Max(0f, Math.Min(60f, _shape.ShadowBlur)) : 0f;
                DrawCanvaSliderRow(g, 38, blurVal, 0f, 60f, ((int)Math.Round(blurVal)).ToString(), out _sliderTrack1Bounds, out _valuePill1Bounds);

                g.DrawString(AppL10n.Tr("Khoảng cách bóng"), lblFont, txtB, 14, 76);
                float distVal = _shape.HasShadow ? Math.Max(0f, Math.Min(40f, _shape.ShadowOffsetY)) : 0f;
                DrawCanvaSliderRow(g, 100, distVal, 0f, 40f, ((int)Math.Round(distVal)).ToString(), out _sliderTrack2Bounds, out _valuePill2Bounds);
            }
        }

        private void DrawFlipPopover(Graphics g)
        {
            _flipHBounds = new Rectangle(10, 10, Width - 20, 34);
            _flipVBounds = new Rectangle(10, 48, Width - 20, 34);

            using (Font f = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                if (_hoveredFlipIdx == 0)
                {
                    using (GraphicsPath gp = CreateRoundedRect(_flipHBounds, 6))
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        g.FillPath(hb, gp);
                }
                DrawFlipIcon(g, _flipHBounds.X + 10, _flipHBounds.Y + 8, true);
                g.DrawString(AppL10n.Tr("Lật ngang"), f, txtB, _flipHBounds.X + 36, _flipHBounds.Y + 7);

                if (_hoveredFlipIdx == 1)
                {
                    using (GraphicsPath gp = CreateRoundedRect(_flipVBounds, 6))
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                        g.FillPath(hb, gp);
                }
                DrawFlipIcon(g, _flipVBounds.X + 10, _flipVBounds.Y + 8, false);
                g.DrawString(AppL10n.Tr("Lật dọc"), f, txtB, _flipVBounds.X + 36, _flipVBounds.Y + 7);
            }
        }

        private void DrawFlipIcon(Graphics g, int x, int y, bool horizontal)
        {
            using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.4f))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(124, 58, 237)))
            {
                if (horizontal)
                {
                    g.DrawLine(p, x + 9, y, x + 9, y + 18);
                    g.DrawPolygon(p, new Point[] { new Point(x + 1, y + 15), new Point(x + 7, y + 3), new Point(x + 7, y + 15) });
                    g.FillPolygon(b, new Point[] { new Point(x + 17, y + 15), new Point(x + 11, y + 3), new Point(x + 11, y + 15) });
                }
                else
                {
                    g.DrawLine(p, x, y + 9, x + 18, y + 9);
                    g.DrawPolygon(p, new Point[] { new Point(x + 3, y + 1), new Point(x + 15, y + 7), new Point(x + 3, y + 7) });
                    g.FillPolygon(b, new Point[] { new Point(x + 3, y + 17), new Point(x + 15, y + 11), new Point(x + 3, y + 11) });
                }
            }
        }

        private void DrawCanvaSliderRow(Graphics g, int y, float val, float minVal, float maxVal, string pillText, out Rectangle trackBounds, out Rectangle pillBounds)
        {
            int pillW = 48;
            int pillH = 28;
            int pillX = Width - 14 - pillW;
            int pillY = y;
            pillBounds = new Rectangle(pillX, pillY, pillW, pillH);

            int trackX = 16;
            int trackW = pillX - trackX - 14;
            int trackY = y + pillH / 2;
            trackBounds = new Rectangle(trackX, trackY - 10, trackW, 20);

            float ratio = (maxVal > minVal) ? Math.Max(0f, Math.Min(1f, (val - minVal) / (maxVal - minVal))) : 0f;
            int fillW = (int)Math.Round(trackW * ratio);

            // Background track
            using (Pen bgTrack = new Pen(Color.FromArgb(226, 232, 240), 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(bgTrack, trackX, trackY, trackX + trackW, trackY);
            }

            // Active purple track (Canva signature #7C3AED)
            if (fillW > 1)
            {
                using (Pen fgTrack = new Pen(Color.FromArgb(124, 58, 237), 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(fgTrack, trackX, trackY, trackX + fillW, trackY);
                }
            }

            // Circular thumb (white with purple/grey border)
            int thumbX = trackX + fillW;
            Rectangle thumbRect = new Rectangle(thumbX - 8, trackY - 8, 16, 16);
            using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(28, 0, 0, 0)))
            {
                g.FillEllipse(shadowB, thumbRect.X + 1, thumbRect.Y + 1, thumbRect.Width, thumbRect.Height);
            }
            using (SolidBrush tb = new SolidBrush(Color.White))
            using (Pen tp = new Pen(Color.FromArgb(124, 58, 237), 2.0f))
            {
                g.FillEllipse(tb, thumbRect);
                g.DrawEllipse(tp, thumbRect);
            }

            // Numeric pill box on the right
            using (GraphicsPath gp = CreateRoundedRect(pillBounds, 6))
            using (SolidBrush pb = new SolidBrush(Color.White))
            using (Pen pp = new Pen(Color.FromArgb(203, 213, 225), 1.1f))
            {
                g.FillPath(pb, gp);
                g.DrawPath(pp, gp);
            }

            using (Font valFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(pillText, valFont, txtB, pillBounds, sf);
            }
        }

        private void ApplySlider1FromX(int mouseX)
        {
            if (_sliderTrack1Bounds.Width <= 0 || _shape == null) return;
            EnsureUndoSaved();
            float ratio = Math.Max(0f, Math.Min(1f, (float)(mouseX - _sliderTrack1Bounds.X) / _sliderTrack1Bounds.Width));

            if (_type == GraphicPopoverType.Stroke)
            {
                float w = (float)Math.Round(ratio * 50f);
                ApplyToSelectedShapes(shp =>
                {
                    shp.StrokeWidth = w;
                    if (w > 0 && (shp.StrokeColor == 0x00FFFFFF || shp.StrokeColor == 0))
                    {
                        shp.StrokeColor = 0x1E293B;
                    }
                    shp.InvalidateDisplayCache();
                });
            }
            else if (_type == GraphicPopoverType.CornerRadius)
            {
                float cr = (float)Math.Round(ratio * 100f);
                ApplyToSelectedShapes(shp =>
                {
                    shp.CornerRadius = cr;
                    if (shp.Type == ShapeType.Rectangle && cr > 0.5f)
                    {
                        shp.Type = ShapeType.RoundedRectangle;
                    }
                    else if (shp.Type == ShapeType.RoundedRectangle && cr <= 0.5f)
                    {
                        shp.Type = ShapeType.Rectangle;
                    }
                    shp.InvalidateDisplayCache();
                });
            }
            else if (_type == GraphicPopoverType.Transparency)
            {
                float op = (float)Math.Round(ratio * 100f) / 100f;
                ApplyToSelectedShapes(shp =>
                {
                    shp.FillOpacity = op;
                    shp.InvalidateDisplayCache();
                });
            }
            else if (_type == GraphicPopoverType.Shadow)
            {
                float blur = (float)Math.Round(ratio * 60f);
                ApplyToSelectedShapes(shp =>
                {
                    shp.HasShadow = (blur > 0.5f || shp.ShadowOffsetY > 0.5f);
                    shp.ShadowBlur = blur;
                    if (shp.HasShadow && shp.ShadowOpacity <= 0.05f) shp.ShadowOpacity = 0.32f;
                    shp.InvalidateShadowCache();
                });
            }

            RefreshCanvas();
        }

        private void ApplySlider2FromX(int mouseX)
        {
            if (_sliderTrack2Bounds.Width <= 0 || _shape == null) return;
            EnsureUndoSaved();
            float ratio = Math.Max(0f, Math.Min(1f, (float)(mouseX - _sliderTrack2Bounds.X) / _sliderTrack2Bounds.Width));

            if (_type == GraphicPopoverType.CornerRadius)
            {
                int rawSides = 3 + (int)Math.Round(ratio * 9f);
                ShapeType newType = ShapeType.Rectangle;
                if (rawSides <= 3) newType = ShapeType.Triangle;
                else if (rawSides == 4) newType = (_shape.CornerRadius > 0.5f) ? ShapeType.RoundedRectangle : ShapeType.Rectangle;
                else if (rawSides == 5) newType = ShapeType.Pentagon;
                else if (rawSides <= 7) newType = ShapeType.Hexagon;
                else if (rawSides <= 10) newType = ShapeType.Octagon;
                else newType = ShapeType.Starburst;

                ApplyToSelectedShapes(shp =>
                {
                    if (shp.Type != ShapeType.TextBox && shp.Type != ShapeType.Image)
                    {
                        shp.Type = newType;
                        shp.InvalidateDisplayCache();
                    }
                });
            }
            else if (_type == GraphicPopoverType.Shadow)
            {
                float dist = (float)Math.Round(ratio * 40f);
                ApplyToSelectedShapes(shp =>
                {
                    shp.HasShadow = (dist > 0.5f || shp.ShadowBlur > 0.5f);
                    shp.ShadowOffsetY = dist;
                    if (shp.HasShadow && shp.ShadowBlur <= 0.5f) shp.ShadowBlur = 12f;
                    if (shp.HasShadow && shp.ShadowOpacity <= 0.05f) shp.ShadowOpacity = 0.32f;
                    shp.InvalidateShadowCache();
                });
            }

            RefreshCanvas();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _shape == null) return;

            if (_type == GraphicPopoverType.Stroke)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (_dashCardBounds[i].Contains(e.Location))
                    {
                        EnsureUndoSaved();
                        int idx = i;
                        ApplyToSelectedShapes(shp =>
                        {
                            if (idx == 0)
                            {
                                shp.StrokeWidth = 0f;
                            }
                            else
                            {
                                if (shp.StrokeWidth <= 0.1f) shp.StrokeWidth = 3f;
                                if (shp.StrokeColor == 0x00FFFFFF || shp.StrokeColor == 0) shp.StrokeColor = 0x1E293B;
                                if (idx == 1) shp.DashStyle = DashStyle.Solid;
                                else if (idx == 2) shp.DashStyle = DashStyle.Dash;
                                else if (idx == 3) shp.DashStyle = DashStyle.DashDot;
                                else if (idx == 4) shp.DashStyle = DashStyle.Dot;
                            }
                            shp.InvalidateDisplayCache();
                        });
                        RefreshCanvas();
                        return;
                    }
                }

                if (!_strokeColorSwatchBounds.IsEmpty && _strokeColorSwatchBounds.Contains(e.Location))
                {
                    Point pt = PointToScreen(new Point(_strokeColorSwatchBounds.Left, _strokeColorSwatchBounds.Bottom + 4));
                    int sc = _shape.StrokeColor;
                    Color curC = Color.FromArgb((sc >> 16) & 0xFF, (sc >> 8) & 0xFF, sc & 0xFF);
                    ColorPaletteForm colDlg = new ColorPaletteForm(pt, curC, false);
                    colDlg.OnColorSelected += (c) =>
                    {
                        EnsureUndoSaved();
                        ApplyToSelectedShapes(shp =>
                        {
                            shp.StrokeColor = c.ToArgb() & 0xFFFFFF;
                            if (shp.StrokeWidth <= 0.1f) shp.StrokeWidth = 2.0f;
                            shp.InvalidateDisplayCache();
                        });
                        RefreshCanvas();
                    };
                    colDlg.Show();
                    return;
                }
            }

            if (_type == GraphicPopoverType.Flip)
            {
                if (_flipHBounds.Contains(e.Location))
                {
                    EnsureUndoSaved();
                    ApplyToSelectedShapes(shp => shp.Transform.FlipHorizontal = !shp.Transform.FlipHorizontal);
                    RefreshCanvas();
                    Close();
                    return;
                }
                if (_flipVBounds.Contains(e.Location))
                {
                    EnsureUndoSaved();
                    ApplyToSelectedShapes(shp => shp.Transform.FlipVertical = !shp.Transform.FlipVertical);
                    RefreshCanvas();
                    Close();
                    return;
                }
            }

            if (!_sliderTrack1Bounds.IsEmpty && _sliderTrack1Bounds.Contains(e.Location))
            {
                _isDraggingSlider1 = true;
                ApplySlider1FromX(e.X);
                return;
            }

            if (!_sliderTrack2Bounds.IsEmpty && _sliderTrack2Bounds.Contains(e.Location))
            {
                _isDraggingSlider2 = true;
                ApplySlider2FromX(e.X);
                return;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDraggingSlider1)
            {
                ApplySlider1FromX(e.X);
                return;
            }
            if (_isDraggingSlider2)
            {
                ApplySlider2FromX(e.X);
                return;
            }

            int oldDash = _hoveredDashIdx;
            int oldFlip = _hoveredFlipIdx;
            _hoveredDashIdx = -1;
            _hoveredFlipIdx = -1;

            if (_type == GraphicPopoverType.Stroke)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (_dashCardBounds[i].Contains(e.Location))
                    {
                        _hoveredDashIdx = i;
                        break;
                    }
                }
            }
            else if (_type == GraphicPopoverType.Flip)
            {
                if (_flipHBounds.Contains(e.Location)) _hoveredFlipIdx = 0;
                else if (_flipVBounds.Contains(e.Location)) _hoveredFlipIdx = 1;
            }

            bool onInteractive = (_hoveredDashIdx >= 0 || _hoveredFlipIdx >= 0 ||
                                  (!_sliderTrack1Bounds.IsEmpty && _sliderTrack1Bounds.Contains(e.Location)) ||
                                  (!_sliderTrack2Bounds.IsEmpty && _sliderTrack2Bounds.Contains(e.Location)) ||
                                  (!_strokeColorSwatchBounds.IsEmpty && _strokeColorSwatchBounds.Contains(e.Location)));
            Cursor = onInteractive ? Cursors.Hand : Cursors.Default;

            if (oldDash != _hoveredDashIdx || oldFlip != _hoveredFlipIdx)
            {
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingSlider1 = false;
            _isDraggingSlider2 = false;
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

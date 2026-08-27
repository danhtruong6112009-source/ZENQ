using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public enum RecordToolType
    {
        None,
        LaserPointer,
        Eraser,
        Pen,
        Highlighter
    }

    public class PenAnnotationStroke
    {
        public List<PointF> Points = new List<PointF>();
        public Color Color { get; set; }
        public float Width { get; set; }
        public bool IsHighlighter { get; set; }
    }

    /// <summary>
    /// Modern PowerPoint "Record Slide Show" Studio Interface (Images 2 & 3)
    /// </summary>
    public class RecordSlideShowForm : Form
    {
        private SlidesView _slidesView;
        private PresentationRoot _presentation;
        private int _currentSlideIndex = 0;

        // Recording state
        private bool _isRecording = false;
        private bool _isPaused = false;
        private int _countdown = 0;
        private Timer _countdownTimer;
        private Timer _recordTimer;
        private DateTime _slideStartTime;
        private DateTime _totalStartTime;
        private TimeSpan _slideElapsed = TimeSpan.Zero;
        private TimeSpan _totalElapsed = TimeSpan.Zero;

        // Tools
        private RecordToolType _activeTool = RecordToolType.None;
        private Color _selectedColor = Color.FromArgb(239, 68, 68); // Red default
        private Point _laserPoint = Point.Empty;
        private bool _isDrawing = false;
        private PenAnnotationStroke _currentStroke = null;
        private Dictionary<int, List<PenAnnotationStroke>> _slideAnnotations = new Dictionary<int, List<PenAnnotationStroke>>();

        // Media Toggles
        private bool _micEnabled = true;
        private bool _cameraEnabled = false;
        private bool _cameraPreviewEnabled = false;
        private bool _showNotes = false;

        // 12 Standard Colors
        private readonly Color[] _paletteColors = new Color[]
        {
            Color.FromArgb(255, 255, 255), // White
            Color.FromArgb(0, 0, 0),       // Black
            Color.FromArgb(153, 27, 27),   // Dark Red
            Color.FromArgb(239, 68, 68),   // Red
            Color.FromArgb(249, 115, 22),  // Orange
            Color.FromArgb(250, 204, 21),  // Yellow
            Color.FromArgb(132, 204, 22),  // Light Green
            Color.FromArgb(16, 185, 129),  // Green
            Color.FromArgb(6, 182, 212),   // Light Blue
            Color.FromArgb(59, 130, 246),  // Blue
            Color.FromArgb(30, 58, 138),   // Dark Blue
            Color.FromArgb(147, 51, 234)   // Purple
        };

        public RecordSlideShowForm(SlidesView slidesView, int startSlideIndex = 0)
        {
            _slidesView = slidesView;
            _presentation = slidesView.Presentation;
            _currentSlideIndex = Math.Max(0, Math.Min(_presentation != null ? _presentation.Slides.Count - 1 : 0, startSlideIndex));

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(24, 24, 27);
            DoubleBuffered = true;
            KeyPreview = true;

            _countdownTimer = new Timer { Interval = 1000 };
            _countdownTimer.Tick += (s, e) =>
            {
                _countdown--;
                if (_countdown <= 0)
                {
                    _countdownTimer.Stop();
                    StartRecordingActual();
                }
                Invalidate();
            };

            _recordTimer = new Timer { Interval = 100 };
            _recordTimer.Tick += (s, e) =>
            {
                if (_isRecording && !_isPaused)
                {
                    _slideElapsed = DateTime.Now - _slideStartTime;
                    _totalElapsed = DateTime.Now - _totalStartTime;
                    Invalidate();
                }
            };

            KeyDown += RecordSlideShowForm_KeyDown;
        }

        private void RecordSlideShowForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                StopAndExit();
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space || e.KeyCode == Keys.PageDown)
            {
                NextSlide();
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.PageUp)
            {
                PrevSlide();
            }
            else if (e.KeyCode == Keys.R && e.Control)
            {
                ToggleRecord();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // 1. Draw Studio Top Bar (Height: 64px)
            DrawTopBar(g, w);

            // 2. Draw Center Slide Canvas Area
            Rectangle slideRect = GetSlideRect(w, h);
            DrawSlideArea(g, slideRect);

            // 3. Draw Side Navigation Arrows (< and >)
            DrawNavArrows(g, slideRect);

            // 4. Draw Studio Bottom Bar
            DrawBottomBar(g, w, h);

            // 5. Draw Slide Notes Panel if open
            if (_showNotes)
            {
                DrawNotesPanel(g, w);
            }

            // 6. Draw 3-2-1 Countdown Overlay
            if (_countdown > 0)
            {
                using (SolidBrush dimB = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                {
                    g.FillRectangle(dimB, 0, 0, w, h);
                }
                using (Font countFont = new Font("Segoe UI", 72f, FontStyle.Bold))
                using (SolidBrush txtB = new SolidBrush(Color.White))
                {
                    string cnt = _countdown.ToString();
                    SizeF sz = g.MeasureString(cnt, countFont);
                    g.DrawString(cnt, countFont, txtB, (w - sz.Width) / 2f, (h - sz.Height) / 2f);
                }
            }
        }

        private Rectangle GetSlideRect(int w, int h)
        {
            int topH = 64;
            int botH = 70;
            int availW = w - 160;
            int availH = h - topH - botH - 20;

            float targetRatio = (_presentation != null && !_presentation.IsWidescreen) ? (4f / 3f) : (16f / 9f);
            int slideW = availW;
            int slideH = (int)(availW / targetRatio);

            if (slideH > availH)
            {
                slideH = availH;
                slideW = (int)(availH * targetRatio);
            }

            int sx = (w - slideW) / 2;
            int sy = topH + (availH - slideH) / 2 + 10;
            return new Rectangle(sx, sy, slideW, slideH);
        }

        private void DrawTopBar(Graphics g, int w)
        {
            // Subtle top line
            using (Pen sep = new Pen(Color.FromArgb(39, 39, 42), 1f))
            {
                g.DrawLine(sep, 0, 64, w, 64);
            }

            // Left Buttons: RECORD, STOP, REPLAY
            // [● RECORD]
            Rectangle btnRec = new Rectangle(24, 12, 60, 44);
            using (SolidBrush redB = new SolidBrush(Color.FromArgb(239, 68, 68)))
            using (Pen redP = new Pen(Color.FromArgb(239, 68, 68), 2f))
            {
                if (_isRecording)
                {
                    // Pause bars
                    g.FillRectangle(redB, btnRec.X + 22, btnRec.Y + 4, 5, 16);
                    g.FillRectangle(redB, btnRec.X + 33, btnRec.Y + 4, 5, 16);
                }
                else
                {
                    g.FillEllipse(redB, btnRec.X + 22, btnRec.Y + 4, 16, 16);
                    g.DrawEllipse(redP, btnRec.X + 19, btnRec.Y + 1, 22, 22);
                }
            }
            using (Font lblFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(228, 228, 231)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(_isRecording ? "PAUSE" : "RECORD", lblFont, txtB, btnRec.X + 30, btnRec.Y + 26, sf);

                // [■ STOP]
                Rectangle btnStop = new Rectangle(90, 12, 54, 44);
                using (SolidBrush stopB = new SolidBrush(_isRecording ? Color.FromArgb(244, 244, 245) : Color.FromArgb(82, 82, 91)))
                {
                    g.FillRectangle(stopB, btnStop.X + 18, btnStop.Y + 5, 16, 16);
                }
                g.DrawString("STOP", lblFont, _isRecording ? txtB : Brushes.Gray, btnStop.X + 26, btnStop.Y + 26, sf);

                // [▶ REPLAY]
                Rectangle btnRep = new Rectangle(148, 12, 54, 44);
                using (SolidBrush repB = new SolidBrush(Color.FromArgb(161, 161, 170)))
                {
                    PointF[] tri = new PointF[] {
                        new PointF(btnRep.X + 20, btnRep.Y + 4),
                        new PointF(btnRep.X + 36, btnRep.Y + 13),
                        new PointF(btnRep.X + 20, btnRep.Y + 22)
                    };
                    g.FillPolygon(repB, tri);
                }
                g.DrawString("REPLAY", lblFont, txtB, btnRep.X + 27, btnRep.Y + 26, sf);

                // Center [≡ NOTES ▾]
                Rectangle btnNotes = new Rectangle(w / 2 - 40, 14, 80, 40);
                using (Pen iconP = new Pen(Color.FromArgb(212, 212, 216), 1.5f))
                {
                    g.DrawLine(iconP, btnNotes.X + 30, btnNotes.Y + 4, btnNotes.X + 50, btnNotes.Y + 4);
                    g.DrawLine(iconP, btnNotes.X + 30, btnNotes.Y + 9, btnNotes.X + 50, btnNotes.Y + 9);
                    g.DrawLine(iconP, btnNotes.X + 30, btnNotes.Y + 14, btnNotes.X + 44, btnNotes.Y + 14);
                }
                g.DrawString("NOTES", lblFont, txtB, btnNotes.X + 40, btnNotes.Y + 20, sf);

                // Right Buttons: [✕ CLEAR ▾], [⚙ SETTINGS ▾], [✕ CLOSE]
                Rectangle btnClear = new Rectangle(w - 240, 18, 70, 30);
                using (Pen xPen = new Pen(Color.FromArgb(212, 212, 216), 2f))
                {
                    g.DrawLine(xPen, btnClear.X + 4, btnClear.Y + 4, btnClear.X + 14, btnClear.Y + 14);
                    g.DrawLine(xPen, btnClear.X + 14, btnClear.Y + 4, btnClear.X + 4, btnClear.Y + 14);
                }
                g.DrawString("CLEAR ▾", lblFont, txtB, btnClear.X + 22, btnClear.Y + 3);

                Rectangle btnSet = new Rectangle(w - 155, 18, 80, 30);
                DrawGearIcon(g, btnSet.X + 8, btnSet.Y + 9);
                g.DrawString("SETTINGS ▾", lblFont, txtB, btnSet.X + 20, btnSet.Y + 3);

                // Top-Right Close Button
                Rectangle btnClose = new Rectangle(w - 40, 16, 28, 28);
                using (Pen cPen = new Pen(Color.FromArgb(161, 161, 170), 1.5f))
                {
                    g.DrawLine(cPen, btnClose.X + 8, btnClose.Y + 8, btnClose.X + 20, btnClose.Y + 20);
                    g.DrawLine(cPen, btnClose.X + 20, btnClose.Y + 8, btnClose.X + 8, btnClose.Y + 20);
                }
            }
        }

        private void DrawGearIcon(Graphics g, int x, int y)
        {
            using (Pen p = new Pen(Color.FromArgb(212, 212, 216), 1.5f))
            {
                g.DrawEllipse(p, x - 4, y - 4, 8, 8);
                g.DrawLine(p, x - 6, y, x + 6, y);
                g.DrawLine(p, x, y - 6, x, y + 6);
            }
        }

        private void DrawSlideArea(Graphics g, Rectangle r)
        {
            if (_presentation == null || _presentation.Slides.Count == 0) return;
            SlideNode slide = _presentation.Slides[_currentSlideIndex];

            // 1. Draw Slide Background & Shapes
            GraphicsState state = g.Save();
            g.SetClip(r);

            // Draw slide background
            if (slide.IsGradientBg)
            {
                Color c1 = Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000));
                Color c2 = Color.FromArgb(slide.BgColor2 | unchecked((int)0xFF000000));
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, c1, c2, slide.BgGradientAngle))
                {
                    g.FillRectangle(lgb, r);
                }
            }
            else
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000))))
                {
                    g.FillRectangle(sb, r);
                }
            }

            // Draw Slide Shapes via SlidesView helper
            float scale = (float)r.Width / _presentation.CanvasWidth;
            for (int i = 0; i < slide.Shapes.Count; i++)
            {
                ShapeNode shape = slide.Shapes[i];
                if (!shape.IsVisible) continue;
                float baseW = shape.Transform.Width * scale;
                float baseH = shape.Transform.Height * scale;
                float sx = r.X + shape.Transform.X * scale;
                float sy = r.Y + shape.Transform.Y * scale;

                RectangleF sRect = new RectangleF(sx, sy, baseW, baseH);

                if (shape.CachedImage != null)
                {
                    g.DrawImage(shape.CachedImage, sRect);
                }
                else
                {
                    using (SolidBrush fb = new SolidBrush(Color.FromArgb(shape.FillColor | unchecked((int)0xFF000000))))
                    {
                        g.FillRectangle(fb, sRect);
                    }
                }

                if (!string.IsNullOrEmpty(shape.Text))
                {
                    using (Font tf = new Font(shape.FontName ?? "Segoe UI", Math.Max(8f, shape.FontSize * scale * 0.8f), shape.Bold ? FontStyle.Bold : FontStyle.Regular))
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(shape.TextColor | unchecked((int)0xFF000000))))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = shape.Alignment == TextAlignment.Center ? StringAlignment.Center : (shape.Alignment == TextAlignment.Right ? StringAlignment.Far : StringAlignment.Near),
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(shape.Text, tf, tb, sRect, sf);
                    }
                }
            }

            // Draw Recorded Pen / Highlighter Annotations on this slide
            if (_slideAnnotations.ContainsKey(_currentSlideIndex))
            {
                foreach (var stroke in _slideAnnotations[_currentSlideIndex])
                {
                    DrawStroke(g, stroke);
                }
            }
            if (_currentStroke != null)
            {
                DrawStroke(g, _currentStroke);
            }

            // Draw Glowing Laser Pointer (Image 3)
            if (_activeTool == RecordToolType.LaserPointer && !_laserPoint.IsEmpty && r.Contains(_laserPoint))
            {
                DrawLaserDot(g, _laserPoint.X, _laserPoint.Y);
            }

            g.Restore(state);

            // Subtle border around slide
            using (Pen bPen = new Pen(Color.FromArgb(63, 63, 70), 1f))
            {
                g.DrawRectangle(bPen, r);
            }
        }

        private void DrawStroke(Graphics g, PenAnnotationStroke stroke)
        {
            if (stroke.Points == null || stroke.Points.Count < 2) return;
            int alpha = stroke.IsHighlighter ? 110 : 255;
            using (Pen p = new Pen(Color.FromArgb(alpha, stroke.Color), stroke.Width))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, stroke.Points.ToArray());
            }
        }

        private void DrawLaserDot(Graphics g, int x, int y)
        {
            // Glowing Halo Laser Pointer (Image 3)
            using (SolidBrush halo3 = new SolidBrush(Color.FromArgb(30, 239, 68, 68)))
            using (SolidBrush halo2 = new SolidBrush(Color.FromArgb(80, 239, 68, 68)))
            using (SolidBrush halo1 = new SolidBrush(Color.FromArgb(180, 239, 68, 68)))
            using (SolidBrush core = new SolidBrush(Color.White))
            {
                g.FillEllipse(halo3, x - 18, y - 18, 36, 36);
                g.FillEllipse(halo2, x - 12, y - 12, 24, 24);
                g.FillEllipse(halo1, x - 7, y - 7, 14, 14);
                g.FillEllipse(core, x - 3, y - 3, 6, 6);
            }
        }

        private void DrawNavArrows(Graphics g, Rectangle slideRect)
        {
            // Left Navigation Button <
            int leftBtnX = Math.Max(20, slideRect.X - 60);
            int arrowY = slideRect.Y + slideRect.Height / 2 - 20;
            Rectangle leftArrowRect = new Rectangle(leftBtnX, arrowY, 40, 40);

            using (SolidBrush bgB = new SolidBrush(Color.FromArgb(120, 39, 39, 42)))
            using (Pen p = new Pen(Color.FromArgb(212, 212, 216), 2f))
            {
                g.FillEllipse(bgB, leftArrowRect);
                g.DrawEllipse(p, leftArrowRect);
                g.DrawLine(p, leftArrowRect.X + 24, leftArrowRect.Y + 12, leftArrowRect.X + 15, leftArrowRect.Y + 20);
                g.DrawLine(p, leftArrowRect.X + 15, leftArrowRect.Y + 20, leftArrowRect.X + 24, leftArrowRect.Y + 28);
            }

            // Right Navigation Button >
            int rightBtnX = Math.Min(ClientSize.Width - 60, slideRect.Right + 20);
            Rectangle rightArrowRect = new Rectangle(rightBtnX, arrowY, 40, 40);

            using (SolidBrush bgB = new SolidBrush(Color.FromArgb(120, 39, 39, 42)))
            using (Pen p = new Pen(Color.FromArgb(212, 212, 216), 2f))
            {
                g.FillEllipse(bgB, rightArrowRect);
                g.DrawEllipse(p, rightArrowRect);
                g.DrawLine(p, rightArrowRect.X + 16, rightArrowRect.Y + 12, rightArrowRect.X + 25, rightArrowRect.Y + 20);
                g.DrawLine(p, rightArrowRect.X + 25, rightArrowRect.Y + 20, rightArrowRect.X + 16, rightArrowRect.Y + 28);
            }
        }

        private void DrawBottomBar(Graphics g, int w, int h)
        {
            int barY = h - 64;

            // 1. Bottom-Left: Slide Counter & Timers
            using (Font infoFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (SolidBrush textB = new SolidBrush(Color.FromArgb(212, 212, 216)))
            {
                int totalS = _presentation != null ? _presentation.Slides.Count : 1;
                string slideStr = string.Format("Slide {0} of {1}", _currentSlideIndex + 1, totalS);
                g.DrawString(slideStr, infoFont, textB, 24, barY + 8);

                string timeStr = string.Format("{0:D1}:{1:D2} / {2:D1}:{3:D2}",
                    (int)_slideElapsed.TotalMinutes, _slideElapsed.Seconds,
                    (int)_totalElapsed.TotalMinutes, _totalElapsed.Seconds);
                using (Font timeFont = new Font("Segoe UI", 11f, FontStyle.Regular))
                {
                    g.DrawString(timeStr, timeFont, textB, 24, barY + 28);
                }
            }

            // 2. Bottom-Center: Interactive Tools & 12 Colors Palette
            int toolStartX = w / 2 - 300;
            int toolY = barY + 14;

            // Laser Pointer Button
            DrawToolButton(g, toolStartX, toolY, RecordToolType.LaserPointer, "Laser");
            // Eraser Button
            DrawToolButton(g, toolStartX + 36, toolY, RecordToolType.Eraser, "Eraser");
            // Pen Button
            DrawToolButton(g, toolStartX + 72, toolY, RecordToolType.Pen, "Pen");
            // Highlighter Button
            DrawToolButton(g, toolStartX + 108, toolY, RecordToolType.Highlighter, "Highlighter");

            // 12 Palette Colors
            int colorStartX = toolStartX + 154;
            for (int i = 0; i < _paletteColors.Length; i++)
            {
                Color c = _paletteColors[i];
                Rectangle cRect = new Rectangle(colorStartX + i * 26, toolY + 2, 22, 22);

                using (SolidBrush cb = new SolidBrush(c))
                {
                    g.FillRectangle(cb, cRect);
                }

                if (_selectedColor.ToArgb() == c.ToArgb())
                {
                    using (Pen selPen = new Pen(Color.White, 2f))
                    {
                        g.DrawRectangle(selPen, cRect.X - 2, cRect.Y - 2, cRect.Width + 4, cRect.Height + 4);
                    }
                }
                else
                {
                    using (Pen bPen = new Pen(Color.FromArgb(82, 82, 91), 1f))
                    {
                        g.DrawRectangle(bPen, cRect);
                    }
                }
            }

            // 3. Bottom-Right: Mic, Camera, and Preview Toggles
            int rightToolX = w - 150;
            DrawMediaToggle(g, rightToolX, toolY, "Mic", _micEnabled);
            DrawMediaToggle(g, rightToolX + 38, toolY, "Camera", _cameraEnabled);
            DrawMediaToggle(g, rightToolX + 76, toolY, "Preview", _cameraPreviewEnabled);
        }

        private void DrawToolButton(Graphics g, int x, int y, RecordToolType tool, string name)
        {
            Rectangle btnRect = new Rectangle(x, y, 30, 30);
            bool isSelected = (_activeTool == tool);

            if (isSelected)
            {
                using (SolidBrush selB = new SolidBrush(Color.FromArgb(63, 63, 70)))
                {
                    g.FillRectangle(selB, btnRect);
                }
                using (Pen selP = new Pen(Color.FromArgb(239, 68, 68), 1.5f))
                {
                    g.DrawRectangle(selP, btnRect);
                }
            }

            using (Pen p = new Pen(isSelected ? Color.White : Color.FromArgb(161, 161, 170), 1.5f))
            {
                switch (tool)
                {
                    case RecordToolType.LaserPointer:
                        g.DrawEllipse(p, x + 11, y + 11, 8, 8);
                        g.DrawLine(p, x + 7, y + 15, x + 23, y + 15);
                        g.DrawLine(p, x + 15, y + 7, x + 15, y + 23);
                        break;
                    case RecordToolType.Eraser:
                        g.DrawRectangle(p, x + 8, y + 10, 14, 10);
                        g.FillRectangle(Brushes.White, x + 9, y + 11, 5, 8);
                        break;
                    case RecordToolType.Pen:
                        g.DrawLine(p, x + 20, y + 7, x + 10, y + 21);
                        g.DrawLine(p, x + 10, y + 21, x + 8, y + 22);
                        g.DrawLine(p, x + 8, y + 22, x + 9, y + 20);
                        break;
                    case RecordToolType.Highlighter:
                        g.DrawRectangle(p, x + 9, y + 8, 12, 14);
                        using (SolidBrush yb = new SolidBrush(Color.FromArgb(180, 250, 204, 21)))
                        {
                            g.FillRectangle(yb, x + 10, y + 9, 10, 12);
                        }
                        break;
                }
            }
        }

        private void DrawMediaToggle(Graphics g, int x, int y, string type, bool enabled)
        {
            Rectangle btnRect = new Rectangle(x, y, 32, 30);
            using (Pen p = new Pen(enabled ? Color.FromArgb(228, 228, 231) : Color.FromArgb(113, 113, 122), 1.5f))
            {
                if (type == "Mic")
                {
                    g.DrawEllipse(p, x + 11, y + 7, 10, 12);
                    g.DrawArc(p, x + 8, y + 10, 16, 10, 0, 180);
                    g.DrawLine(p, x + 16, y + 20, x + 16, y + 24);
                    if (!enabled) g.DrawLine(Pens.Crimson, x + 6, y + 6, x + 26, y + 24);
                }
                else if (type == "Camera")
                {
                    g.DrawRectangle(p, x + 7, y + 9, 14, 12);
                    g.DrawPolygon(p, new Point[] { new Point(x + 21, y + 12), new Point(x + 26, y + 9), new Point(x + 26, y + 21), new Point(x + 21, y + 18) });
                    if (!enabled) g.DrawLine(Pens.Crimson, x + 6, y + 6, x + 26, y + 24);
                }
                else
                {
                    g.DrawRectangle(p, x + 8, y + 8, 16, 14);
                    g.DrawLine(p, x + 8, y + 8, x + 24, y + 22);
                }
            }
        }

        private void DrawNotesPanel(Graphics g, int w)
        {
            Rectangle notesRect = new Rectangle(w / 2 - 250, 68, 500, 180);
            using (SolidBrush nb = new SolidBrush(Color.FromArgb(245, 39, 39, 42)))
            using (Pen np = new Pen(Color.FromArgb(63, 63, 70), 1f))
            {
                g.FillRectangle(nb, notesRect);
                g.DrawRectangle(np, notesRect);
            }

            SlideNode slide = (_presentation != null && _currentSlideIndex < _presentation.Slides.Count) ? _presentation.Slides[_currentSlideIndex] : null;
            string notes = (slide != null && !string.IsNullOrEmpty(slide.Notes)) ? slide.Notes : "(Chưa có ghi chú cho slide này)";

            using (Font nFont = new Font("Segoe UI", 12f, FontStyle.Regular))
            using (SolidBrush txtB = new SolidBrush(Color.FromArgb(244, 244, 245)))
            {
                g.DrawString(notes, nFont, txtB, new RectangleF(notesRect.X + 16, notesRect.Y + 16, notesRect.Width - 32, notesRect.Height - 32));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // 1. Top Bar Clicks
            if (e.Y <= 64)
            {
                // [● RECORD]
                if (e.X >= 24 && e.X <= 84)
                {
                    ToggleRecord();
                    return;
                }
                // [■ STOP]
                if (e.X >= 90 && e.X <= 144)
                {
                    StopRecording();
                    return;
                }
                // [▶ REPLAY]
                if (e.X >= 148 && e.X <= 202)
                {
                    ReplayCurrentSlide();
                    return;
                }
                // [≡ NOTES]
                if (e.X >= w / 2 - 40 && e.X <= w / 2 + 40)
                {
                    _showNotes = !_showNotes;
                    Invalidate();
                    return;
                }
                // [✕ CLEAR ▾]
                if (e.X >= w - 240 && e.X <= w - 170)
                {
                    ShowClearMenu(e.Location);
                    return;
                }
                // [⚙ SETTINGS ▾]
                if (e.X >= w - 155 && e.X <= w - 75)
                {
                    ShowSettingsMenu(e.Location);
                    return;
                }
                // [✕ CLOSE]
                if (e.X >= w - 45)
                {
                    StopAndExit();
                    return;
                }
            }

            // 2. Bottom Bar Clicks
            if (e.Y >= h - 64)
            {
                int toolStartX = w / 2 - 300;
                int toolY = h - 64 + 14;

                // Tool selection
                if (e.X >= toolStartX && e.X < toolStartX + 36)
                {
                    _activeTool = (_activeTool == RecordToolType.LaserPointer) ? RecordToolType.None : RecordToolType.LaserPointer;
                    Invalidate();
                    return;
                }
                if (e.X >= toolStartX + 36 && e.X < toolStartX + 72)
                {
                    _activeTool = (_activeTool == RecordToolType.Eraser) ? RecordToolType.None : RecordToolType.Eraser;
                    Invalidate();
                    return;
                }
                if (e.X >= toolStartX + 72 && e.X < toolStartX + 108)
                {
                    _activeTool = (_activeTool == RecordToolType.Pen) ? RecordToolType.None : RecordToolType.Pen;
                    Invalidate();
                    return;
                }
                if (e.X >= toolStartX + 108 && e.X < toolStartX + 144)
                {
                    _activeTool = (_activeTool == RecordToolType.Highlighter) ? RecordToolType.None : RecordToolType.Highlighter;
                    Invalidate();
                    return;
                }

                // Color Palette selection
                int colorStartX = toolStartX + 154;
                for (int i = 0; i < _paletteColors.Length; i++)
                {
                    Rectangle cRect = new Rectangle(colorStartX + i * 26, toolY + 2, 22, 22);
                    if (cRect.Contains(e.Location))
                    {
                        _selectedColor = _paletteColors[i];
                        if (_activeTool == RecordToolType.None || _activeTool == RecordToolType.Eraser)
                        {
                            _activeTool = RecordToolType.Pen;
                        }
                        Invalidate();
                        return;
                    }
                }

                // Media toggles
                int rightToolX = w - 150;
                if (e.X >= rightToolX && e.X < rightToolX + 36)
                {
                    _micEnabled = !_micEnabled;
                    Invalidate();
                    return;
                }
                if (e.X >= rightToolX + 38 && e.X < rightToolX + 74)
                {
                    _cameraEnabled = !_cameraEnabled;
                    Invalidate();
                    return;
                }
                if (e.X >= rightToolX + 76)
                {
                    _cameraPreviewEnabled = !_cameraPreviewEnabled;
                    Invalidate();
                    return;
                }
            }

            // 3. Navigation Arrows Clicks
            Rectangle slideRect = GetSlideRect(w, h);
            int leftBtnX = Math.Max(20, slideRect.X - 60);
            int arrowY = slideRect.Y + slideRect.Height / 2 - 20;
            if (new Rectangle(leftBtnX, arrowY, 40, 40).Contains(e.Location))
            {
                PrevSlide();
                return;
            }
            int rightBtnX = Math.Min(w - 60, slideRect.Right + 20);
            if (new Rectangle(rightBtnX, arrowY, 40, 40).Contains(e.Location))
            {
                NextSlide();
                return;
            }

            // 4. Slide Canvas Drawing Interaction
            if (slideRect.Contains(e.Location))
            {
                if (_activeTool == RecordToolType.Pen || _activeTool == RecordToolType.Highlighter)
                {
                    _isDrawing = true;
                    _currentStroke = new PenAnnotationStroke
                    {
                        Color = _selectedColor,
                        Width = (_activeTool == RecordToolType.Highlighter) ? 14f : 3.5f,
                        IsHighlighter = (_activeTool == RecordToolType.Highlighter)
                    };
                    _currentStroke.Points.Add(e.Location);
                    Invalidate();
                }
                else if (_activeTool == RecordToolType.Eraser)
                {
                    EraseStrokeAt(e.Location);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _laserPoint = e.Location;

            if (_isDrawing && _currentStroke != null)
            {
                _currentStroke.Points.Add(e.Location);
                Invalidate();
            }
            else if (_activeTool == RecordToolType.LaserPointer)
            {
                Invalidate();
            }
            else if (_activeTool == RecordToolType.Eraser && (e.Button & MouseButtons.Left) != 0)
            {
                EraseStrokeAt(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDrawing && _currentStroke != null)
            {
                if (!_slideAnnotations.ContainsKey(_currentSlideIndex))
                {
                    _slideAnnotations[_currentSlideIndex] = new List<PenAnnotationStroke>();
                }
                _slideAnnotations[_currentSlideIndex].Add(_currentStroke);
                _currentStroke = null;
                _isDrawing = false;
                Invalidate();
            }
        }

        private void EraseStrokeAt(Point pt)
        {
            if (_slideAnnotations.ContainsKey(_currentSlideIndex))
            {
                var list = _slideAnnotations[_currentSlideIndex];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    foreach (var p in list[i].Points)
                    {
                        float dx = p.X - pt.X;
                        float dy = p.Y - pt.Y;
                        if (dx * dx + dy * dy < 250f)
                        {
                            list.RemoveAt(i);
                            Invalidate();
                            break;
                        }
                    }
                }
            }
        }

        private void ToggleRecord()
        {
            if (!_isRecording)
            {
                // Start 3-second countdown
                _countdown = 3;
                _countdownTimer.Start();
                Invalidate();
            }
            else
            {
                _isPaused = !_isPaused;
                Invalidate();
            }
        }

        private void StartRecordingActual()
        {
            _isRecording = true;
            _isPaused = false;
            _slideStartTime = DateTime.Now;
            _totalStartTime = DateTime.Now;
            _recordTimer.Start();
            Invalidate();
        }

        private void StopRecording(bool promptSaveVideo = true)
        {
            bool wasRecording = _isRecording;
            _isRecording = false;
            _isPaused = false;
            _recordTimer.Stop();

            // Save timings to current slide
            if (_presentation != null && _currentSlideIndex < _presentation.Slides.Count)
            {
                SlideNode slide = _presentation.Slides[_currentSlideIndex];
                slide.AutoAdvance = true;
                slide.AutoAdvanceSeconds = Math.Max(1.0f, (float)_slideElapsed.TotalSeconds);
            }
            Invalidate();

            if (promptSaveVideo)
            {
                DialogResult ask = MessageBox.Show(
                    "Đã hoàn thành phiên ghi hình bài thuyết trình!\n\nBạn có muốn chọn vị trí lưu tệp video trình chiếu (.mp4 / .wmv / .avi) ngay bây giờ không?",
                    "Lưu Bản Ghi Video (Save Recorded Presentation Video)",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ask == DialogResult.Yes)
                {
                    PromptAndSaveVideoFile();
                }
            }
        }

        public void PromptAndSaveVideoFile()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Chọn vị trí lưu Video Trình chiếu (Save Recorded Video)";
                sfd.Filter = "Tệp Video MP4 (*.mp4)|*.mp4|Tệp Windows Media Video (*.wmv)|*.wmv|Tệp Video AVI (*.avi)|*.avi|Tất cả tệp (*.*)|*.*";
                sfd.FileName = "Presentation_Recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp4";
                sfd.DefaultExt = "mp4";
                sfd.AddExtension = true;

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    string targetPath = sfd.FileName;
                    ExportPresentationVideo(targetPath);
                }
            }
        }

        private void ExportPresentationVideo(string targetPath, bool showSuccessDialog = true)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                if (_presentation == null || _presentation.Slides.Count == 0)
                {
                    if (showSuccessDialog) MessageBox.Show("Không có slide nào để xuất video!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int videoW = _presentation.IsWidescreen ? 1920 : 1440;
                int videoH = 1080;
                int fps = 10; // 10 FPS standard video

                List<Bitmap> slideBitmaps = new List<Bitmap>();
                List<float> slideDurations = new List<float>();

                for (int i = 0; i < _presentation.Slides.Count; i++)
                {
                    SlideNode slide = _presentation.Slides[i];
                    Bitmap bmp = RenderSlideBitmap(i, videoW, videoH);
                    slideBitmaps.Add(bmp);

                    float duration = slide.AutoAdvance && slide.AutoAdvanceSeconds > 0 ? slide.AutoAdvanceSeconds : 4.0f;
                    slideDurations.Add(duration);
                }

                // Write video file
                SaveAsAviMjpegVideo(targetPath, slideBitmaps, slideDurations, fps);

                // Clean up bitmaps
                foreach (var b in slideBitmaps)
                {
                    b.Dispose();
                }

                Cursor = Cursors.Default;

                if (showSuccessDialog)
                {
                    DialogResult openFolder = MessageBox.Show(
                        string.Format("Bản ghi video đã được xuất thành công tại:\n\n{0}\n\nBạn có muốn mở thư mục chứa video không?", targetPath),
                        "Xuất Video Thành Công (Export Complete)",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (openFolder == DialogResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + targetPath + "\"");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                Console.WriteLine("EXPORT ERROR: " + ex.ToString());
                if (showSuccessDialog) MessageBox.Show("Lỗi khi xuất video: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap RenderSlideBitmap(int slideIndex, int targetW, int targetH)
        {
            Bitmap bmp = new Bitmap(targetW, targetH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, targetW, targetH);
                SlideNode slide = _presentation.Slides[slideIndex];

                // Background
                if (slide.IsGradientBg)
                {
                    Color c1 = Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000));
                    Color c2 = Color.FromArgb(slide.BgColor2 | unchecked((int)0xFF000000));
                    using (LinearGradientBrush lgb = new LinearGradientBrush(r, c1, c2, slide.BgGradientAngle))
                    {
                        g.FillRectangle(lgb, r);
                    }
                }
                else
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(slide.BgColor | unchecked((int)0xFF000000))))
                    {
                        g.FillRectangle(sb, r);
                    }
                }

                // Shapes
                float scale = (float)targetW / _presentation.CanvasWidth;
                for (int s = 0; s < slide.Shapes.Count; s++)
                {
                    ShapeNode shape = slide.Shapes[s];
                    if (!shape.IsVisible) continue;

                    float baseW = shape.Transform.Width * scale;
                    float baseH = shape.Transform.Height * scale;
                    float sx = shape.Transform.X * scale;
                    float sy = shape.Transform.Y * scale;
                    RectangleF sRect = new RectangleF(sx, sy, baseW, baseH);

                    if (shape.CachedImage != null)
                    {
                        g.DrawImage(shape.CachedImage, sRect);
                    }
                    else
                    {
                        using (SolidBrush fb = new SolidBrush(Color.FromArgb(shape.FillColor | unchecked((int)0xFF000000))))
                        {
                            g.FillRectangle(fb, sRect);
                        }
                    }

                    if (!string.IsNullOrEmpty(shape.Text))
                    {
                        using (Font tf = new Font(shape.FontName ?? "Segoe UI", Math.Max(12f, shape.FontSize * scale), shape.Bold ? FontStyle.Bold : FontStyle.Regular))
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(shape.TextColor | unchecked((int)0xFF000000))))
                        {
                            StringFormat sf = new StringFormat
                            {
                                Alignment = shape.Alignment == TextAlignment.Center ? StringAlignment.Center : (shape.Alignment == TextAlignment.Right ? StringAlignment.Far : StringAlignment.Near),
                                LineAlignment = StringAlignment.Center
                            };
                            g.DrawString(shape.Text, tf, tb, sRect, sf);
                        }
                    }
                }

                // Ink Annotations
                if (_slideAnnotations.ContainsKey(slideIndex))
                {
                    foreach (var stroke in _slideAnnotations[slideIndex])
                    {
                        DrawStroke(g, stroke);
                    }
                }
            }
            return bmp;
        }

        private void SaveAsAviMjpegVideo(string targetPath, List<Bitmap> slideFrames, List<float> durations, int fps)
        {
            if (slideFrames.Count == 0) return;

            int width = slideFrames[0].Width;
            int height = slideFrames[0].Height;

            // Encode all unique slide frames to JPEG byte arrays
            List<byte[]> jpegFrames = new List<byte[]>();
            System.Drawing.Imaging.ImageCodecInfo jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
            System.Drawing.Imaging.EncoderParameters encParams = new System.Drawing.Imaging.EncoderParameters(1);
            encParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);

            foreach (var bmp in slideFrames)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    bmp.Save(ms, jpgEncoder, encParams);
                    jpegFrames.Add(ms.ToArray());
                }
            }

            // Total frame count
            int totalFrames = 0;
            for (int i = 0; i < durations.Count; i++)
            {
                totalFrames += Math.Max(1, (int)(durations[i] * fps));
            }

            int microSecPerFrame = (int)(1000000.0 / fps);

            string dir = System.IO.Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            using (System.IO.FileStream fs = new System.IO.FileStream(targetPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs))
            {
                // AVI RIFF Header
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                long riffSizePos = bw.BaseStream.Position;
                bw.Write((int)0); // Placeholder for file size
                bw.Write(System.Text.Encoding.ASCII.GetBytes("AVI "));

                // LIST hdrl
                bw.Write(System.Text.Encoding.ASCII.GetBytes("LIST"));
                long hdrlSizePos = bw.BaseStream.Position;
                bw.Write((int)0); // Placeholder for hdrl size
                bw.Write(System.Text.Encoding.ASCII.GetBytes("hdrl"));

                // avih (Main AVI Header)
                bw.Write(System.Text.Encoding.ASCII.GetBytes("avih"));
                bw.Write((int)56); // cb
                bw.Write((int)microSecPerFrame); // dwMicroSecPerFrame
                bw.Write((int)0); // dwMaxBytesPerSec
                bw.Write((int)0); // dwPaddingGranularity
                bw.Write((int)0x110); // dwFlags (AVIF_HASINDEX | AVIF_ISINTERLEAVED)
                bw.Write((int)totalFrames); // dwTotalFrames
                bw.Write((int)0); // dwInitialFrames
                bw.Write((int)1); // dwStreams
                bw.Write((int)0); // dwSuggestedBufferSize
                bw.Write((int)width); // dwWidth
                bw.Write((int)height); // dwHeight
                bw.Write((int)0); // dwReserved[0]
                bw.Write((int)0); // dwReserved[1]
                bw.Write((int)0); // dwReserved[2]
                bw.Write((int)0); // dwReserved[3]

                // LIST strl
                bw.Write(System.Text.Encoding.ASCII.GetBytes("LIST"));
                long strlSizePos = bw.BaseStream.Position;
                bw.Write((int)0); // Placeholder for strl size
                bw.Write(System.Text.Encoding.ASCII.GetBytes("strl"));

                // strh (Stream Header)
                bw.Write(System.Text.Encoding.ASCII.GetBytes("strh"));
                bw.Write((int)56);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("vids"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("MJPG"));
                bw.Write((int)0); // dwFlags
                bw.Write((short)0); // wPriority
                bw.Write((short)0); // wLanguage
                bw.Write((int)0); // dwInitialFrames
                bw.Write((int)1); // dwScale
                bw.Write((int)fps); // dwRate
                bw.Write((int)0); // dwStart
                bw.Write((int)totalFrames); // dwLength
                bw.Write((int)100000); // dwSuggestedBufferSize
                bw.Write((int)10000); // dwQuality
                bw.Write((int)0); // dwSampleSize
                bw.Write((short)0); // rcFrame.left
                bw.Write((short)0); // rcFrame.top
                bw.Write((short)width); // rcFrame.right
                bw.Write((short)height); // rcFrame.bottom

                // strf (Stream Format: BITMAPINFOHEADER)
                bw.Write(System.Text.Encoding.ASCII.GetBytes("strf"));
                bw.Write((int)40); // biSize
                bw.Write((int)width); // biWidth
                bw.Write((int)height); // biHeight
                bw.Write((short)1); // biPlanes
                bw.Write((short)24); // biBitCount
                bw.Write(System.Text.Encoding.ASCII.GetBytes("MJPG")); // biCompression
                bw.Write((int)(width * height * 3)); // biSizeImage
                bw.Write((int)0); // biXPelsPerMeter
                bw.Write((int)0); // biYPelsPerMeter
                bw.Write((int)0); // biClrUsed
                bw.Write((int)0); // biClrImportant

                // Close strl
                long strlEnd = bw.BaseStream.Position;
                bw.BaseStream.Seek(strlSizePos, System.IO.SeekOrigin.Begin);
                bw.Write((int)(strlEnd - strlSizePos - 4));
                bw.BaseStream.Seek(strlEnd, System.IO.SeekOrigin.Begin);

                // Close hdrl
                long hdrlEnd = bw.BaseStream.Position;
                bw.BaseStream.Seek(hdrlSizePos, System.IO.SeekOrigin.Begin);
                bw.Write((int)(hdrlEnd - hdrlSizePos - 4));
                bw.BaseStream.Seek(hdrlEnd, System.IO.SeekOrigin.Begin);

                // LIST movi
                bw.Write(System.Text.Encoding.ASCII.GetBytes("LIST"));
                long moviSizePos = bw.BaseStream.Position;
                bw.Write((int)0); // Placeholder for movi size
                bw.Write(System.Text.Encoding.ASCII.GetBytes("movi"));

                long moviDataStart = bw.BaseStream.Position;
                List<long> chunkOffsets = new List<long>();
                List<int> chunkSizes = new List<int>();

                // Write video frames
                for (int s = 0; s < slideFrames.Count; s++)
                {
                    byte[] frameBytes = jpegFrames[s];
                    int frameRepeat = Math.Max(1, (int)(durations[s] * fps));

                    for (int f = 0; f < frameRepeat; f++)
                    {
                        long chunkOffset = bw.BaseStream.Position - moviDataStart + 4;
                        chunkOffsets.Add(chunkOffset);
                        chunkSizes.Add(frameBytes.Length);

                        bw.Write(System.Text.Encoding.ASCII.GetBytes("00dc"));
                        bw.Write((int)frameBytes.Length);
                        bw.Write(frameBytes);

                        // Pad byte if odd length
                        if (frameBytes.Length % 2 != 0)
                        {
                            bw.Write((byte)0);
                        }
                    }
                }

                // Close movi
                long moviEnd = bw.BaseStream.Position;
                bw.BaseStream.Seek(moviSizePos, System.IO.SeekOrigin.Begin);
                bw.Write((int)(moviEnd - moviSizePos - 4));
                bw.BaseStream.Seek(moviEnd, System.IO.SeekOrigin.Begin);

                // idx1 (Index table)
                bw.Write(System.Text.Encoding.ASCII.GetBytes("idx1"));
                bw.Write((int)(totalFrames * 16));
                for (int i = 0; i < totalFrames; i++)
                {
                    bw.Write(System.Text.Encoding.ASCII.GetBytes("00dc"));
                    bw.Write((int)0x10); // AVIIF_KEYFRAME
                    bw.Write((int)chunkOffsets[i]);
                    bw.Write((int)chunkSizes[i]);
                }

                // Close RIFF file size
                long fileEnd = bw.BaseStream.Position;
                bw.BaseStream.Seek(riffSizePos, System.IO.SeekOrigin.Begin);
                bw.Write((int)(fileEnd - 8));
            }
        }

        private static System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            foreach (System.Drawing.Imaging.ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void ReplayCurrentSlide()
        {
            _slideElapsed = TimeSpan.Zero;
            _slideStartTime = DateTime.Now;
            Invalidate();
        }

        private void NextSlide()
        {
            if (_presentation != null && _currentSlideIndex < _presentation.Slides.Count - 1)
            {
                if (_isRecording)
                {
                    SlideNode slide = _presentation.Slides[_currentSlideIndex];
                    slide.AutoAdvance = true;
                    slide.AutoAdvanceSeconds = (float)_slideElapsed.TotalSeconds;
                }

                _currentSlideIndex++;
                _slideStartTime = DateTime.Now;
                _slideElapsed = TimeSpan.Zero;
                Invalidate();
            }
        }

        private void PrevSlide()
        {
            if (_currentSlideIndex > 0)
            {
                _currentSlideIndex--;
                _slideStartTime = DateTime.Now;
                _slideElapsed = TimeSpan.Zero;
                Invalidate();
            }
        }

        private void ShowClearMenu(Point loc)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Xóa bản ghi trên slide hiện tại (Clear on Current Slide)", null, (s, e) =>
            {
                if (_slideAnnotations.ContainsKey(_currentSlideIndex))
                {
                    _slideAnnotations[_currentSlideIndex].Clear();
                }
                if (_presentation != null && _currentSlideIndex < _presentation.Slides.Count)
                {
                    _presentation.Slides[_currentSlideIndex].AutoAdvanceSeconds = 0;
                    _presentation.Slides[_currentSlideIndex].AutoAdvance = false;
                }
                Invalidate();
            });
            menu.Items.Add("Xóa bản ghi trên tất cả slide (Clear on All Slides)", null, (s, e) =>
            {
                _slideAnnotations.Clear();
                if (_presentation != null)
                {
                    foreach (var sl in _presentation.Slides)
                    {
                        sl.AutoAdvanceSeconds = 0;
                        sl.AutoAdvance = false;
                    }
                }
                Invalidate();
            });
            menu.Show(this, loc);
        }

        private void ShowSettingsMenu(Point loc)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("🎤 Thiết bị Microphone: Mặc định (Default Mic)", null, null).Enabled = false;
            menu.Items.Add("📷 Thiết bị Camera: Webcam HD", null, null).Enabled = false;
            menu.Show(this, loc);
        }

        private void StopAndExit()
        {
            if (_isRecording)
            {
                StopRecording(true);
            }
            Close();
        }
    }
}

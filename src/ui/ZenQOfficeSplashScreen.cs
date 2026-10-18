using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Màn hình khởi động phong cách Microsoft Office tối giản, thanh lịch với tông màu trắng (Clean White).
    /// Trong quá trình nạp, hệ thống thực hiện khôi phục phiên làm việc trước (các Tab tài liệu đã mở khi bấm X tắt app).
    /// </summary>
    public class GenOfficeSplashScreen : Form
    {
        private System.Windows.Forms.Timer _animTimer;
        private Stopwatch _stopwatch;
        private float _progress = 0f;
        private float _targetProgress = 0f;
        private string _statusMessage = "Đang khởi động...";
        private bool _isClosing = false;
        private AppForm _mainAppForm = null;
        private bool _sessionRestored = false;
        private Thread _bgWorker = null;

        public Action OnSplashFinished { get; set; }
        public AppForm CreatedAppForm { get { return _mainAppForm; } }

        public GenOfficeSplashScreen(bool autoInit = true)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 580;
            Height = 320;
            DoubleBuffered = true;
            Opacity = 0.0;
            BackColor = Color.White;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            // Bo góc tinh tế 12px chuẩn thiết kế giao diện Windows / Office 365
            using (GraphicsPath path = GenOfficeLogoRenderer.CreateRoundedRectanglePath(new RectangleF(0, 0, Width, Height), 12f))
            {
                Region = new Region(path);
            }

            Click += (s, e) => SkipIntro();
            KeyDown += (s, e) => SkipIntro();

            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            if (autoInit)
            {
                // Khởi động luồng nạp và khôi phục tab phiên làm việc trước
                StartBackgroundInit();

                _animTimer = new System.Windows.Forms.Timer();
                _animTimer.Interval = 16; // 60 FPS
                _animTimer.Tick += OnAnimationTick;
                _animTimer.Start();
            }
        }

        private void StartBackgroundInit()
        {
            _bgWorker = new Thread(new ThreadStart(BackgroundInitWork));
            _bgWorker.IsBackground = true;
            _bgWorker.SetApartmentState(ApartmentState.STA);
            _bgWorker.Start();
        }

        private void BackgroundInitWork()
        {
            try
            {
                // Giai đoạn 1: Khởi động hệ thống
                SetStatus("Đang khởi động ZENQ OFFICE...", 0.20f);
                Thread.Sleep(500);

                // Giai đoạn 2: Nạp các mô-đun
                SetStatus("Đang khởi tạo các mô-đun ứng dụng...", 0.45f);
                Thread.Sleep(600);

                // Giai đoạn 3: Kiểm tra phiên làm việc trước
                SetStatus("Đang kiểm tra phiên làm việc trước...", 0.65f);
                Thread.Sleep(400);

                var session = SessionManager.LoadSession();
                if (session != null && session.Tabs != null && session.Tabs.Count > 0)
                {
                    for (int i = 0; i < session.Tabs.Count; i++)
                    {
                        var tab = session.Tabs[i];
                        string title = !string.IsNullOrEmpty(tab.Title) ? tab.Title : "Tài liệu " + (i + 1);
                        SetStatus(string.Format("Đang khôi phục: {0}...", title), 0.65f + ((float)(i + 1) / session.Tabs.Count) * 0.28f);
                        Thread.Sleep(300);
                    }
                }

                _sessionRestored = true;
                SetStatus("Khởi tạo hoàn tất. Đang mở Workspace...", 1.0f);
                Thread.Sleep(350);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BG INIT ERR: " + ex.Message);
                _sessionRestored = true;
            }
        }

        private void SetStatus(string msg, float targetProg)
        {
            _statusMessage = msg;
            _targetProgress = Math.Max(_targetProgress, targetProg);
        }

        private void SkipIntro()
        {
            if (_sessionRestored)
            {
                _progress = 1.0f;
                _targetProgress = 1.0f;
            }
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;

            // Phase 1: Fade-in mượt mà (0 - 400ms)
            if (elapsed < 400)
            {
                Opacity = Math.Min(1.0, elapsed / 400.0);
            }
            else if (!_isClosing)
            {
                Opacity = 1.0;
            }

            // Tiến trình tiến dần đều siêu mượt
            _progress += (_targetProgress - _progress) * 0.08f;

            // Phase 2: Khi đã hoàn tất khôi phục và qua tối thiểu 2.8s -> Fade-out mở app
            if (_sessionRestored && elapsed >= 2800 && _progress >= 0.96f)
            {
                if (!_isClosing)
                {
                    _isClosing = true;
                }

                // Fade-out êm dịu
                Opacity -= 0.06;
                if (Opacity <= 0.02)
                {
                    _animTimer.Stop();
                    Hide();

                    if (OnSplashFinished != null)
                    {
                        OnSplashFinished();
                    }
                    else
                    {
                        if (_mainAppForm == null)
                        {
                            _mainAppForm = new AppForm();
                        }
                        _mainAppForm.Show();
                        _mainAppForm.Activate();
                        _mainAppForm.Focus();
                    }
                    Close();
                    return;
                }
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            long elapsed = _stopwatch != null ? _stopwatch.ElapsedMilliseconds : 0;
            float timeSec = elapsed / 1000f;

            // 1. Nền Trắng Tinh Tế (Clean White Background)
            g.Clear(Color.White);

            // 2. Viền Soft Border
            using (GraphicsPath bPath = GenOfficeLogoRenderer.CreateRoundedRectanglePath(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 12f))
            using (Pen bPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawPath(bPen, bPath);
            }

            // 3. Logo chính thức GenOffice
            float logoX = 48f;
            float logoY = 48f;
            float logoSize = 76f;
            GenOfficeLogoRenderer.DrawLogo(g, logoX, logoY, logoSize, true, true);

            // 4. Typography phong cách Microsoft Office
            float textX = logoX + logoSize + 22f;
            float textY = logoY + 12f;

            // Tên "ZENQ OFFICE" viết hoa toàn bộ
            using (Font titleFont = new Font("Segoe UI", 28f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush titleB = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("ZENQ OFFICE", titleFont, titleB, textX, textY);
            }

            // Subtitle: NEXTGENEDUSPACE STUDIO
            using (Font subFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush subB = new SolidBrush(Color.FromArgb(37, 99, 235)))
            {
                g.DrawString("NEXTGENEDUSPACE STUDIO", subFont, subB, textX, textY + 36f);
            }

            // 5. Hình minh họa nhân vật văn phòng thưởng thức cà phê bốc khói (Góc phải màn hình)
            DrawHumanCoffeeCharacter(g, Width - 105f, 108f, 0.95f, timeSec);

            // 6. Thanh chỉ báo tiến trình / Loading Dots phong cách MS Office
            float barX = 48f;
            float barY = 220f;
            float barW = Width - barX * 2f;
            float barH = 3.5f;

            // Animated Office Dots (5 chấm nhảy mượt mà kiểu Microsoft 365)
            float dotAreaX = barX;
            float dotAreaY = 160f;
            int numDots = 5;
            float dotSpacing = 16f;
            for (int i = 0; i < numDots; i++)
            {
                float phase = timeSec * 4.5f - i * 0.45f;
                float dotYOffset = (float)Math.Max(0, Math.Sin(phase) * 6f);
                float dotAlpha = (float)Math.Max(0.2, Math.Min(1.0, Math.Sin(phase) * 0.8 + 0.2));

                Color dotColor = Color.FromArgb((int)(dotAlpha * 255), 37, 99, 235); // Office Blue #2563EB
                using (SolidBrush dotB = new SolidBrush(dotColor))
                {
                    g.FillEllipse(dotB, dotAreaX + i * dotSpacing, dotAreaY - dotYOffset, 6f, 6f);
                }
            }

            // Dòng trạng thái nạp thời gian thực (Hiển thị đang mở tài liệu nào)
            using (Font statusFont = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush stBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.DrawString(_statusMessage, statusFont, stBrush, barX, barY - 26f);
            }

            // Thanh tiến trình siêu mảnh hiện đại (Thin Accent Progress Bar)
            RectangleF trackRect = new RectangleF(barX, barY, barW, barH);
            using (GraphicsPath trackPath = GenOfficeLogoRenderer.CreateRoundedRectanglePath(trackRect, barH / 2f))
            using (SolidBrush trackBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.FillPath(trackBg, trackPath);
            }

            float fillW = Math.Max(barH, barW * Math.Min(1.0f, Math.Max(0.02f, _progress)));
            RectangleF fillRect = new RectangleF(barX, barY, fillW, barH);
            using (GraphicsPath fillPath = GenOfficeLogoRenderer.CreateRoundedRectanglePath(fillRect, barH / 2f))
            using (SolidBrush fillBg = new SolidBrush(Color.FromArgb(37, 99, 235))) // Microsoft Royal Blue
            {
                g.FillPath(fillBg, fillPath);
            }

            // 7. Dòng bản quyền dưới cùng phong cách Office
            using (Font footerFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush footerBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                g.DrawString("© 2026 NEXTGENEDUSPACE STUDIO • Trương Nguyễn Thế Danh", footerFont, footerBrush, new RectangleF(barX, Height - 36f, barW, 20f), sf);

                StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString("Phiên bản 5.5", footerFont, footerBrush, new RectangleF(barX, Height - 36f, barW, 20f), sfLeft);
            }
        }

        #region Stylish Animated Human Coffee Character Vector Art
        private void DrawHumanCoffeeCharacter(Graphics g, float cx, float cy, float scale, float timeSec)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(scale, scale);

            // 1. Nền tròn pastel mềm mại (#F1F5F9)
            using (GraphicsPath circle = new GraphicsPath())
            using (SolidBrush circleBrush = new SolidBrush(Color.FromArgb(243, 246, 252)))
            {
                circle.AddEllipse(-65, -55, 130, 130);
                g.FillPath(circleBrush, circle);
            }

            // Hạt lấp lánh (Sparkles) nhấp nháy êm ái
            float sp1 = (float)(0.6 + 0.4 * Math.Sin(timeSec * 3.2));
            DrawStar(g, -50, -42, 6.5f * sp1, Color.FromArgb(245, 158, 11)); // Amber star
            DrawStar(g, 56, -36, 6.0f * (1.2f - 0.4f * sp1), Color.FromArgb(59, 130, 246)); // Blue star

            // Hạt cà phê bay lơ lửng và đung đưa nhẹ nhàng
            float beanY = -18f + (float)Math.Sin(timeSec * 2.5) * 3f;
            float beanRot = (float)Math.Sin(timeSec * 2.0) * 15f;
            GraphicsState beanState = g.Save();
            g.TranslateTransform(46, beanY);
            g.RotateTransform(beanRot);
            using (SolidBrush beanBrush = new SolidBrush(Color.FromArgb(160, 120, 53, 15)))
            {
                g.FillEllipse(beanBrush, -5, -7, 10, 14);
            }
            using (Pen beanLine = new Pen(Color.FromArgb(254, 243, 199), 1.2f))
            {
                g.DrawArc(beanLine, -3, -5, 6, 10, 40, 100);
            }
            g.Restore(beanState);

            // 2. Chuyển động nhịp thở và nghiêng đầu nhẹ nhàng (Breathing & Head Tilt)
            float breathY = (float)(Math.Sin(timeSec * 2.2) * 1.4); // Nhịp thở lên xuống
            float headTilt = (float)(Math.Sin(timeSec * 1.6) * 1.8); // Góc nghiêng đầu nhẹ
            float mugSipOffset = (float)(Math.Sin(timeSec * 2.2) * 2.2); // Tách nâng lên thưởng thức hương thơm

            // Thân người / Áo len xanh ấm áp (Vibrant Blue #3B82F6 / #2563EB)
            using (GraphicsPath torso = new GraphicsPath())
            using (LinearGradientBrush torsoBrush = new LinearGradientBrush(new RectangleF(-45, 15, 90, 60), Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), 90f))
            {
                torso.AddArc(-38, 14 + breathY * 0.4f, 76, 68, 180, 180);
                torso.AddLine(38, 48, 38, 65);
                torso.AddLine(38, 65, -38, 65);
                torso.AddLine(-38, 65, -38, 48);
                torso.CloseFigure();
                g.FillPath(torsoBrush, torso);
            }

            // Cổ áo trắng
            using (SolidBrush collar = new SolidBrush(Color.White))
            {
                g.FillPie(collar, -14, 10 + breathY * 0.5f, 28, 16, 0, 180);
            }

            // 3. Cổ & Đầu (Với góc nghiêng hoạt họa & nhịp thở)
            GraphicsState headState = g.Save();
            g.TranslateTransform(0, breathY);
            g.RotateTransform(headTilt);

            using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                // Cổ
                g.FillRectangle(skin, -8, 6, 16, 14);

                // Đầu
                g.FillEllipse(skin, -24, -40, 48, 48);

                // Tai
                g.FillEllipse(skin, -27, -22, 9, 12);
                g.FillEllipse(skin, 18, -22, 9, 12);
            }

            // Mái tóc thời trang gọn gàng (Dark Navy #0F172A)
            using (GraphicsPath hair = new GraphicsPath())
            using (SolidBrush hairBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                hair.AddArc(-26, -46, 52, 44, 160, 220);
                hair.AddCurve(new PointF[] {
                    new PointF(24, -22),
                    new PointF(16, -32),
                    new PointF(2, -28),
                    new PointF(-14, -34),
                    new PointF(-24, -20)
                });
                hair.CloseFigure();
                g.FillPath(hairBrush, hair);

                // Đỉnh tóc
                g.FillEllipse(hairBrush, -16, -48, 26, 16);
            }

            // Biểu cảm khuôn mặt & Chu kỳ chớp mắt tự nhiên (Blink Cycle)
            float blinkPhase = (timeSec * 0.65f) % 3.0f; // Chớp mắt mỗi ~3s
            bool isBlinking = (blinkPhase > 2.82f);

            using (Pen glassPen = new Pen(Color.FromArgb(30, 41, 59), 2.0f))
            using (SolidBrush blush = new SolidBrush(Color.FromArgb(100, 244, 63, 94)))
            using (Pen eyePen = new Pen(Color.FromArgb(30, 41, 59), 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // Gọng kính tròn
                g.DrawEllipse(glassPen, -18, -26, 15, 15);
                g.DrawEllipse(glassPen, 3, -26, 15, 15);
                g.DrawLine(glassPen, -3, -18, 3, -18);

                // Ánh sáng phản chiếu kính
                using (Pen sheenPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(sheenPen, -15, -23, -11, -19);
                    g.DrawLine(sheenPen, 6, -23, 10, -19);
                }

                if (isBlinking)
                {
                    // Chớp mắt vòng cung xuống
                    g.DrawArc(eyePen, -15, -19, 9, 6, 20, 140);
                    g.DrawArc(eyePen, 6, -19, 9, 6, 20, 140);
                }
                else
                {
                    // Mắt cười tươi thư giãn (^_^)
                    g.DrawArc(eyePen, -15, -20, 9, 7, 190, 160);
                    g.DrawArc(eyePen, 6, -20, 9, 7, 190, 160);
                }

                // Má hồng
                g.FillEllipse(blush, -22, -12, 8, 5);
                g.FillEllipse(blush, 14, -12, 8, 5);

                // Nụ cười ấm áp (Sóng cười nhẹ theo nhịp thở)
                float smileH = 8f + (float)Math.Sin(timeSec * 2.2) * 1.5f;
                g.DrawArc(eyePen, -5, -12, 10, smileH, 20, 140);
            }

            g.Restore(headState);

            // 4. Cánh tay & Tách cà phê nâng hạ nhịp nhàng theo nhịp thở
            float cupCenterY = 16f + breathY - mugSipOffset;
            float mugX = 2f;
            float mugY = cupCenterY;
            float mugW = 26f;
            float mugH = 24f;

            // Cánh tay giữ tách
            using (Pen armPen = new Pen(Color.FromArgb(37, 99, 235), 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // Tay trái
                g.DrawCurve(armPen, new PointF[] {
                    new PointF(-28, 24 + breathY * 0.5f),
                    new PointF(-18, 40 + breathY * 0.8f - mugSipOffset * 0.5f),
                    new PointF(-4, 44 + breathY - mugSipOffset)
                });
                // Tay phải nâng tách
                g.DrawCurve(armPen, new PointF[] {
                    new PointF(28, 24 + breathY * 0.5f),
                    new PointF(18, 34 + breathY * 0.8f - mugSipOffset * 0.7f),
                    new PointF(8, 28 + breathY - mugSipOffset)
                });
            }

            // Bàn tay
            using (SolidBrush skin = new SolidBrush(Color.FromArgb(254, 215, 170)))
            {
                g.FillEllipse(skin, -8, 36 + breathY - mugSipOffset, 11, 11);
                g.FillEllipse(skin, 4, 24 + breathY - mugSipOffset, 11, 11);
            }

            // Quai cầm
            using (Pen handlePen = new Pen(Color.FromArgb(245, 158, 11), 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(handlePen, mugX + mugW - 3, mugY + 4, 11, 14, 280, 160);
            }

            // Thân tách gốm vàng ấm
            using (GraphicsPath cupPath = new GraphicsPath())
            using (LinearGradientBrush cupBrush = new LinearGradientBrush(new RectangleF(mugX, mugY, mugW, mugH), Color.FromArgb(251, 191, 36), Color.FromArgb(245, 158, 11), 90f))
            {
                cupPath.AddArc(mugX, mugY + mugH - 10, 10, 10, 90, 90);
                cupPath.AddArc(mugX + mugW - 10, mugY + mugH - 10, 10, 10, 0, 90);
                cupPath.AddLine(mugX + mugW, mugY, mugX, mugY);
                cupPath.CloseFigure();
                g.FillPath(cupBrush, cupPath);

                // Trái tim trắng trên tách
                using (SolidBrush heartB = new SolidBrush(Color.White))
                {
                    float hx = mugX + 8.5f;
                    float hy = mugY + 8f;
                    g.FillEllipse(heartB, hx, hy, 4.5f, 4.5f);
                    g.FillEllipse(heartB, hx + 4.5f, hy, 4.5f, 4.5f);
                    g.FillPolygon(heartB, new PointF[] {
                        new PointF(hx - 0.5f, hy + 2.5f),
                        new PointF(hx + 9.5f, hy + 2.5f),
                        new PointF(hx + 4.5f, hy + 8f)
                    });
                }
            }

            // Mặt cà phê
            using (SolidBrush coffee = new SolidBrush(Color.FromArgb(120, 53, 15)))
            {
                g.FillEllipse(coffee, mugX + 2, mugY - 2, mugW - 4, 6);
            }

            // 5. KHÓI CÀ PHÊ BỐC LÊN UỐN LƯỢN ĐỘNG 60 FPS
            DrawAnimatedSteam(g, mugX + mugW / 2f, mugY - 4, timeSec);

            g.Restore(state);
        }

        private static void DrawAnimatedSteam(Graphics g, float originX, float originY, float timeSec)
        {
            float[] offsets = new float[] { -5f, 0f, 6f };
            float[] speedMults = new float[] { 0.9f, 1.15f, 0.85f };
            float[] phaseShifts = new float[] { 0f, 2.1f, 4.2f };

            for (int i = 0; i < 3; i++)
            {
                float t = timeSec * 2.4f * speedMults[i] + phaseShifts[i];
                float heightRatio = (t % 3.0f) / 3.0f; // 0.0 to 1.0 cycle

                float alpha = (float)(Math.Sin(heightRatio * Math.PI) * 0.8);
                if (alpha <= 0.02f) continue;

                float baseY = originY - (heightRatio * 36f);
                float baseX = originX + offsets[i];
                float wave = (float)Math.Sin(t * 3.2f) * 4.0f;

                using (GraphicsPath steam = new GraphicsPath())
                using (Pen steamPen = new Pen(Color.FromArgb((int)(alpha * 255), 148, 163, 184), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    PointF p0 = new PointF(baseX, baseY);
                    PointF p1 = new PointF(baseX + wave, baseY - 8);
                    PointF p2 = new PointF(baseX - wave, baseY - 18);
                    PointF p3 = new PointF(baseX + wave * 0.6f, baseY - 26);

                    steam.AddBezier(p0, p1, p2, p3);
                    g.DrawPath(steamPen, steam);
                }
            }
        }

        private static void DrawStar(Graphics g, float x, float y, float size, Color color)
        {
            if (size <= 0.5f) return;
            using (SolidBrush b = new SolidBrush(color))
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] {
                    new PointF(x, y - size),
                    new PointF(x + size * 0.28f, y - size * 0.28f),
                    new PointF(x + size, y),
                    new PointF(x + size * 0.28f, y + size * 0.28f),
                    new PointF(x, y + size),
                    new PointF(x - size * 0.28f, y + size * 0.28f),
                    new PointF(x - size, y),
                    new PointF(x - size * 0.28f, y - size * 0.28f)
                });
                g.FillPath(b, p);
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_animTimer != null)
                {
                    _animTimer.Stop();
                    _animTimer.Dispose();
                    _animTimer = null;
                }
                if (_stopwatch != null)
                {
                    _stopwatch.Stop();
                    _stopwatch = null;
                }
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Alias chính thức ZenQOfficeSplashScreen
    /// </summary>
    public class ZenQOfficeSplashScreen : GenOfficeSplashScreen
    {
        public ZenQOfficeSplashScreen(bool autoInit = true) : base(autoInit) { }
    }
}

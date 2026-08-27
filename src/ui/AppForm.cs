using System;
using System.Drawing;
using System.Windows.Forms;
using GenOffice.Core;

namespace GenOffice.UI
{
    public class AppForm : Form
    {
        public RibbonBar Ribbon { get; set; }
        public HomeView HomeView { get; set; }
        public WriterView WriterView { get; set; }
        public CalcView CalcView { get; set; }
        public SlidesView SlidesView { get; set; }
        public PdfView PdfView { get; set; }

        public string StatusText { get; set; }
        public float ZoomLevel { get; set; }

        private int SlideWheelTickCount = 0;
        private int LastSlideWheelDirection = 0;
        private DateTime LastSlideWheelTime = DateTime.MinValue;

        private FormWindowState _prevWindowState = FormWindowState.Normal;
        private FormBorderStyle _prevFormBorderStyle = FormBorderStyle.Sizable;
        private Rectangle _prevBounds = Rectangle.Empty;
        private SlideShowAudienceForm _audienceForm = null;

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        private string _originalTitle = null;

        public void StartSlideShow(bool fromBeginning, bool forcePresenterView = false)
        {
            if (_prevBounds == Rectangle.Empty || _prevBounds.Width < 200)
            {
                _prevWindowState = WindowState;
                _prevFormBorderStyle = FormBorderStyle;
                _prevBounds = Bounds;
            }
            if (_originalTitle == null)
            {
                _originalTitle = Text;
            }

            if (!SlidesView.IsPresenting)
            {
                SlidesView.StartPresenting(fromBeginning);
            }

            Screen primary = Screen.FromControl(this) ?? Screen.PrimaryScreen;
            Screen[] allScreens = Screen.AllScreens;

            if (SlidesView.ShowType == 1)
            {
                // Chế độ 1: Cá nhân xem (Dạng cửa sổ - Windowed presentation)
                // PowerPoint hiển thị bài chiếu trong một cửa sổ có tiêu đề và khung viền điều chỉnh kích thước
                SlidesView.IsPresenterViewMode = false;
                TopMost = false;
                FormBorderStyle = FormBorderStyle.Sizable;
                Text = "Trình chiếu PowerPoint - " + (SlidesView != null && SlidesView.Presentation != null && !string.IsNullOrEmpty(SlidesView.Presentation.Title) ? SlidesView.Presentation.Title : "Trình bày 1");
            }
            else if (SlidesView.ShowType == 2)
            {
                // Chế độ 2: Duyệt tại Kiosk (Toàn màn hình lặp lại - Kiosk Mode)
                SlidesView.IsPresenterViewMode = false;
                SlidesView.LoopUntilEsc = true;
                StartPosition = FormStartPosition.Manual;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = primary.Bounds;
                TopMost = true;
            }
            else
            {
                // Chế độ 0: Người thuyết trình (Toàn màn hình / Speaker Fullscreen)
                if (allScreens.Length > 1 && (SlidesView.UsePresenterView || forcePresenterView))
                {
                    // Multi-Monitor (2 màn hình như PowerPoint):
                    // Màn chính (Screen 1): AppForm mở Chế độ Diễn giả (Presenter View)
                    // Màn phụ (Screen 2 / Máy chiếu): Mở SlideShowAudienceForm toàn màn hình cho khán giả
                    Screen audienceScreen = null;
                    if (SlidesView.SelectedMonitorIndex > 0 && SlidesView.SelectedMonitorIndex <= allScreens.Length)
                    {
                        audienceScreen = allScreens[SlidesView.SelectedMonitorIndex - 1];
                    }
                    else
                    {
                        foreach (Screen s in allScreens)
                        {
                            if (!s.DeviceName.Equals(primary.DeviceName, StringComparison.OrdinalIgnoreCase))
                            {
                                audienceScreen = s;
                                break;
                            }
                        }
                    }
                    if (audienceScreen == null) audienceScreen = allScreens[1];

                    if (_audienceForm != null)
                    {
                        _audienceForm.Close();
                        _audienceForm = null;
                    }
                    _audienceForm = new SlideShowAudienceForm(SlidesView, this, audienceScreen);
                    _audienceForm.Show();

                    SlidesView.IsPresenterViewMode = true;

                    // Tối đa hóa AppForm trên màn hình chính che kín hoàn toàn taskbar
                    TopMost = true;
                    StartPosition = FormStartPosition.Manual;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Normal;
                    Bounds = primary.Bounds;
                }
                else
                {
                    // Single Screen (1 màn hình):
                    // Nếu forcePresenterView = true (Alt+F5), bật Presenter View trên màn hình duy nhất
                    SlidesView.IsPresenterViewMode = forcePresenterView;
                    StartPosition = FormStartPosition.Manual;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Normal;
                    Bounds = primary.Bounds;
                    TopMost = true;
                }
            }

            Invalidate();
        }

        public void StopSlideShow()
        {
            if (_audienceForm != null)
            {
                _audienceForm.Close();
                _audienceForm = null;
            }

            SlidesView.IsPresenting = false;
            SlidesView.IsEndSlideShowScreen = false;
            SlidesView.IsSlideSorterModalOpen = false;
            SlidesView.IsSlideZoomActive = false;

            TopMost = false;
            FormBorderStyle = (_prevFormBorderStyle != FormBorderStyle.None) ? _prevFormBorderStyle : FormBorderStyle.Sizable;
            WindowState = _prevWindowState;
            if (_prevBounds.Width > 200 && _prevBounds.Height > 200)
            {
                Bounds = _prevBounds;
            }
            if (_originalTitle != null)
            {
                Text = _originalTitle;
                _originalTitle = null;
            }

            Invalidate();
        }

        public void HandleSlideShowKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                StopSlideShow();
            }
            else
            {
                SlidesView.HandleKeyDown(key);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
        }

        public AppForm()
        {
            Text = "GENOFFICE - Bộ Ứng Dụng Văn Phòng Đa Năng";
            Size = new Size(1366, 800);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            try
            {
                TimeBeginPeriod(1);
            }
            catch { }

            Ribbon = new RibbonBar();
            HomeView = new HomeView();
            WriterView = new WriterView();
            CalcView = new CalcView();
            SlidesView = new SlidesView();
            PdfView = new PdfView();

            SlidesView.RequestStartSlideShow += (fromBeginning) =>
            {
                StartSlideShow(fromBeginning);
            };
            SlidesView.RequestStopSlideShow += () =>
            {
                StopSlideShow();
            };
            SlidesView.InvalidateCallback = () =>
            {
                Invalidate();
            };

            StatusText = "Sẵn sàng";
            ZoomLevel = 1.0f;

            // Apply GenOffice Custom 3-in-1 Master Logo Icon
            try
            {
                Icon = GenOfficeLogoRenderer.CreateAppIcon(32);
            }
            catch { }

            // Cursor blink timer (530ms like Windows standard)
            Timer cursorTimer = new Timer();
            cursorTimer.Interval = 530;
            cursorTimer.Tick += (s, e) =>
            {
                WriterView.CursorVisible = !WriterView.CursorVisible;
                if (PdfView != null) PdfView.ShowCursor = !PdfView.ShowCursor;
                if ((Ribbon.CurrentMode == OfficeAppMode.Writer || Ribbon.CurrentMode == OfficeAppMode.Pdf) && !SlidesView.IsPresenting)
                    Invalidate();
            };
            cursorTimer.Start();

            // 60 FPS Smooth Kinetic Scroll & Morph Transition Animation Timer (16ms)
            Timer animTimer = new Timer();
            animTimer.Interval = 16;
            animTimer.Tick += (s, e) =>
            {
                bool anim1 = WriterView.UpdateSmoothScroll();
                bool anim2 = HomeView.UpdateSmoothScroll(ClientSize.Height);
                bool anim3 = false;
                if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null)
                {
                    anim3 = SlidesView.UpdateAnimationAndTransition();
                }
                if (Ribbon.IsDraggingTab)
                {
                    Ribbon.ProcessDragAutoScroll(ClientSize.Width);
                }
                bool anim4 = Ribbon.UpdateTabScroll();
                bool anim5 = (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null) && PdfView.UpdateSmoothScroll();
                if (anim1 || anim2 || anim3 || anim4 || anim5)
                {
                    if (anim3 && !SlidesView.IsPresenting)
                    {
                        // Invalidate ONLY the slide canvas area to save 70% CPU rasterization workload
                        int ribbonH = 145;
                        int statusH = 26;
                        int stageY = ribbonH;
                        int stageH = ClientSize.Height - ribbonH - statusH;
                        if (stageH > 0)
                        {
                            Invalidate(new Rectangle(0, stageY, ClientSize.Width, stageH));
                        }
                        else
                        {
                            Invalidate();
                        }
                    }
                    else
                    {
                        Invalidate();
                    }

                    if (anim3 && _audienceForm != null && SlidesView.IsPresenting)
                    {
                        _audienceForm.Invalidate();
                    }
                }
            };
            animTimer.Start();

            // Auto-save open tabs and workspace session when user exits / clicks X
            this.FormClosing += (s, e) =>
            {
                try
                {
                    SessionManager.SaveSession(this);
                }
                catch { }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (SlidesView.IsPresenting)
            {
                SlidesView.DrawFullscreenPresenter(g, ClientSize.Width, ClientSize.Height, true);
                return;
            }

            // 1. Draw Active Viewport (Home / Writer / Calc / Slides / Pdf)
            if (Ribbon.CurrentMode == OfficeAppMode.Home)
            {
                HomeView.Draw(g, ClientSize.Width, ClientSize.Height);
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                WriterView.Draw(g, ClientSize.Width, ClientSize.Height, Ribbon);
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.Draw(g, ClientSize.Width, ClientSize.Height);
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                SlidesView.Draw(g, ClientSize.Width, ClientSize.Height);
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.Draw(g, ClientSize.Width, ClientSize.Height, Ribbon);
            }

            // 2. Draw Ribbon Bar
            Ribbon.Draw(g, ClientSize.Width, ClientSize.Height, WriterView, SlidesView, CalcView, PdfView);

            // 3. Draw Status Bar (Ẩn hoàn toàn khi ở Trang chủ Home)
            if (Ribbon.CurrentMode != OfficeAppMode.Home)
            {
                DrawStatusBar(g, ClientSize.Width, ClientSize.Height);
            }
        }

        public int ActiveViewMode = 1; // 0 = Chế độ đọc, 1 = Bố cục in, 2 = Bố cục Web
        public bool IsDraggingZoomSlider = false;

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            int count = 0;
            bool inWord = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetterOrDigit(text[i]))
                {
                    if (!inWord)
                    {
                        count++;
                        inWord = true;
                    }
                }
                else
                {
                    inWord = false;
                }
            }
            return count;
        }

        private void UpdateZoomFromMouseX(int mouseX)
        {
            int w = ClientSize.Width;
            int sliderX = w - 142;
            int sliderW = 70;
            float frac = (float)(mouseX - sliderX) / (float)sliderW;
            frac = Math.Max(0.0f, Math.Min(1.0f, frac));
            if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                // Non-linear mapping: 50% of track maps to 100% zoom (1.0f)
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.10f + (frac / 0.5f) * 0.90f; // 10% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 3.00f; // 100% to 400%
                SlidesView.ZoomLevel = newZoom;
                ZoomLevel = newZoom;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.20f + (frac / 0.5f) * 0.80f; // 20% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 2.00f; // 100% to 300%
                PdfView.ZoomLevel = newZoom;
                PdfView.InvalidateAllCaches();
                ZoomLevel = newZoom;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.20f + (frac / 0.5f) * 0.80f; // 20% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 3.00f; // 100% to 400%
                CalcView.ZoomFactor = newZoom;
                ZoomLevel = newZoom;
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                float newZoom;
                if (frac <= 0.5f)
                    newZoom = 0.20f + (frac / 0.5f) * 0.80f; // 20% to 100%
                else
                    newZoom = 1.00f + ((frac - 0.5f) / 0.5f) * 1.50f; // 100% to 250%
                WriterView.ZoomLevel = newZoom;
                ZoomLevel = newZoom;
            }
            Invalidate();
        }

        private void DrawStatusBar(Graphics g, int width, int height)
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Home) return;
            int barH = 26;
            Rectangle statusRect = new Rectangle(0, height - barH, width, barH);
            
            // MS Word Signature Status Bar Color (#2B579A Navy Blue)
            Color statusBg = (Ribbon.PrimaryColor == Color.White) ? Color.FromArgb(43, 87, 154) : Ribbon.PrimaryColor;
            using (SolidBrush statusBrush = new SolidBrush(statusBg))
            {
                g.FillRectangle(statusBrush, statusRect);
            }

            // Top subtle accent line
            using (Pen topPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1))
            {
                g.DrawLine(topPen, 0, height - barH, width, height - barH);
            }

            using (Font sFont = new Font("Segoe UI", 9.0f, FontStyle.Regular))
            using (Font iconFont = new Font("Segoe UI Symbol", 9.0f, FontStyle.Regular))
            {
                // LEFT SECTION: Page count, Word/Char count, Language Proofing
                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    string text = WriterView.PieceTable.GetText();
                    int totalWords = CountWords(text);
                    int totalChars = text.Length;
                    int selLen = Math.Abs(WriterView.SelectionEnd - WriterView.SelectionStart);
                    string selStr = (selLen > 0) ? (" (" + selLen + " được chọn)") : "";

                    int curPg = WriterView.CurrentActivePage;
                    int totPgs = WriterView.TotalPageCount;
                    string pageStr = string.Format("Trang {0} / {1}", curPg, totPgs);
                    string wordStr = string.Format("{0:N0} từ, {1:N0} ký tự{2}", totalWords, totalChars, selStr);

                    g.DrawString(pageStr, sFont, Brushes.White, 16, height - 20);
                    g.DrawString(wordStr, sFont, Brushes.White, 110, height - 20);

                    // Proofing / Language icon [📖✓]
                    g.DrawString("📖✓  Tiếng Việt (Unicode)", sFont, Brushes.White, 310, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    string addr = SparseMatrix.ColIndexToName(CalcView.SelectedCol) + CalcView.SelectedRow;
                    g.DrawString("Sẵn sàng   |   Ô chọn: " + addr, sFont, Brushes.White, 16, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    string info = "Slide " + (SlidesView.ActiveSlideIndex + 1) + " / " + SlidesView.Presentation.Slides.Count;
                    g.DrawString(info, sFont, Brushes.White, 16, height - 20);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    int curP = PdfView.ActivePageIndex + 1;
                    int totP = (PdfView.Document != null) ? Math.Max(1, PdfView.Document.Pages.Count) : 1;
                    string pageStr = string.Format("Trang {0} / {1}", curP, totP);
                    g.DrawString(pageStr, sFont, Brushes.White, 16, height - 20);
                    g.DrawString("PDF Editor (Chuẩn PDF 1.7)", sFont, Brushes.White, 140, height - 20);
                }
                else
                {
                    g.DrawString("Sẵn sàng", sFont, Brushes.White, 16, height - 20);
                }

                // RIGHT SECTION: View Mode icons (Read 📖, Print 📄, Web 🌐), Smooth Drag Zoom Slider, Zoom Percentage
                float activeZoom = 1.0f;
                if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                    activeZoom = SlidesView.ZoomLevel;
                else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                    activeZoom = WriterView.ZoomLevel;
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                    activeZoom = PdfView.ZoomLevel;
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                    activeZoom = (CalcView.ZoomFactor > 0) ? CalcView.ZoomFactor : 1.0f;

                int currentZoom = (int)Math.Round(activeZoom * 100);

                // 1. View Mode Icons (Read Mode 📖, Print Layout 📄, Web Layout 🌐)
                int modeX = width - 246;
                string[] modeIcons = new string[] { "📖", "📄", "🌐" };
                for (int m = 0; m < 3; m++)
                {
                    Rectangle mRect = new Rectangle(modeX + m * 24, height - 23, 22, 20);
                    if (ActiveViewMode == m)
                    {
                        using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                        {
                            g.FillRectangle(activeBg, mRect);
                        }
                        g.DrawRectangle(Pens.White, mRect);
                    }
                    g.DrawString(modeIcons[m], iconFont, Brushes.White, mRect.X + 3, mRect.Y + 2);
                }

                // 2. Zoom Minus [-] Button
                int zoomMinusX = width - 164;
                Rectangle minusRect = new Rectangle(zoomMinusX, height - 23, 18, 20);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("-", sFont, Brushes.White, new RectangleF(minusRect.X, minusRect.Y, minusRect.Width, minusRect.Height), sf);
                }

                // 3. Smooth Continuous Drag Trackbar Line (70px)
                int sliderX = width - 142;
                int sliderW = 70;
                int sliderY = height - 13;
                using (Pen trackPen = new Pen(Color.FromArgb(180, 255, 255, 255), 2))
                {
                    g.DrawLine(trackPen, sliderX, sliderY, sliderX + sliderW, sliderY);
                }

                // 100% Center Notch Tick (at exact 50% midpoint of track)
                int midNotchX = sliderX + sliderW / 2;
                using (Pen notchPen = new Pen(Color.FromArgb(240, 255, 255, 255), 1.5f))
                {
                    g.DrawLine(notchPen, midNotchX, sliderY - 4, midNotchX, sliderY + 4);
                }

                // Slider Handle Knob (Vertical white pill indicator)
                float zoomFrac = 0.5f;
                if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.10f) / 0.90f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 3.00f);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.20f) / 0.80f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 2.00f);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.20f) / 0.80f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 3.00f);
                }
                else
                {
                    if (activeZoom <= 1.00f)
                        zoomFrac = 0.5f * Math.Max(0.0f, (activeZoom - 0.20f) / 0.80f);
                    else
                        zoomFrac = 0.5f + 0.5f * Math.Min(1.0f, (activeZoom - 1.00f) / 1.50f);
                }
                zoomFrac = Math.Max(0.0f, Math.Min(1.0f, zoomFrac));
                int knobX = sliderX + (int)Math.Round(zoomFrac * sliderW);
                Rectangle knobRect = new Rectangle(knobX - 3, sliderY - 6, 7, 13);
                g.FillRectangle(Brushes.White, knobRect);
                g.DrawRectangle(Pens.DarkSlateGray, knobRect);

                // 4. Zoom Plus [+] Button
                int zoomPlusX = width - 68;
                Rectangle plusRect = new Rectangle(zoomPlusX, height - 23, 18, 20);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("+", sFont, Brushes.White, new RectangleF(plusRect.X, plusRect.Y, plusRect.Width, plusRect.Height), sf);
                }

                // 5. Zoom Percentage Text (100%)
                string pctStr = currentZoom + "%";
                Rectangle pctRect = new Rectangle(width - 46, height - 23, 42, 20);
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(pctStr, sFont, Brushes.White, pctRect, sf);
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            WriterFloatingMiniToolbarForm.CloseCurrent();

            // 0. Khi rê chuột vào vùng thanh trạng thái / thanh kéo zoom ở dưới đáy (Y >= ClientSize.Height - 30 hoặc vùng Zoom góc phải)
            // Lăn chuột phóng to / thu nhỏ tài liệu NGAY LẬP TỨC mà không cần nhấn phím Ctrl!
            if (Ribbon.CurrentMode != OfficeAppMode.Home && (e.Y >= ClientSize.Height - 30 || (e.X >= ClientSize.Width - 250 && e.Y >= ClientSize.Height - 36)))
            {
                float zoomStep = (e.Delta > 0) ? 0.10f : -0.10f;
                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    WriterView.ZoomLevel = Math.Max(0.20f, Math.Min(2.50f, WriterView.ZoomLevel + zoomStep));
                    ZoomLevel = WriterView.ZoomLevel;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                    CalcView.ZoomFactor = Math.Max(0.20f, Math.Min(4.00f, CalcView.ZoomFactor + zoomStep));
                    ZoomLevel = CalcView.ZoomFactor;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    float sStep = (SlidesView.ZoomLevel >= 1.5f || (SlidesView.ZoomLevel > 1.4f && e.Delta > 0)) ? 0.25f : 0.10f;
                    SlidesView.ZoomLevel = Math.Max(0.10f, Math.Min(4.00f, SlidesView.ZoomLevel + ((e.Delta > 0) ? sStep : -sStep)));
                    ZoomLevel = SlidesView.ZoomLevel;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.ZoomLevel = Math.Max(0.20f, Math.Min(3.00f, PdfView.ZoomLevel + zoomStep));
                    PdfView.InvalidateAllCaches();
                    ZoomLevel = PdfView.ZoomLevel;
                }
                Invalidate();
                return;
            }

            // Horizontal Mouse Wheel Scroll for Document Tabs on Title Bar (except Pinned Home Tab)
            if (e.Y <= 36)
            {
                int scrollDelta = (e.Delta > 0) ? -120 : 120;
                int docAreaX = 265;
                int docAreaW = Math.Max(100, ClientSize.Width - docAreaX - 10);
                int totalDocTabsWidth = (Ribbon.DocTabs.Count - 1) * (160 + 4) + 36;
                int maxTabScroll = Math.Max(0, totalDocTabsWidth - docAreaW);
                Ribbon.TargetTabScrollX = Math.Max(0, Math.Min(maxTabScroll, Ribbon.TargetTabScrollX + scrollDelta));
                Ribbon.UpdateTabScroll();
                Invalidate(new Rectangle(0, 0, ClientSize.Width, 36));
                return;
            }

            int scrollAmount = (e.Delta > 0) ? -65 : 65;

            if (Ribbon.CurrentMode == OfficeAppMode.Home)
            {
                int maxScroll = HomeView.GetMaxScroll(ClientSize.Height, ClientSize.Width);
                HomeView.TargetScrollY = Math.Max(0, Math.Min(maxScroll, HomeView.TargetScrollY + scrollAmount));
                HomeView.UpdateSmoothScroll(ClientSize.Height);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                if (WriterView.ShowSelectionPane && e.X >= ClientSize.Width - 260 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                {
                    WriterView.HandleSelectionPaneMouseWheel(e.Delta);
                    Invalidate();
                    return;
                }

                if (ModifierKeys == Keys.Control)
                {
                    if (e.Delta > 0) WriterView.ZoomLevel = Math.Min(2.5f, WriterView.ZoomLevel + 0.05f);
                    else WriterView.ZoomLevel = Math.Max(0.2f, WriterView.ZoomLevel - 0.05f);
                    Invalidate();
                }
                else
                {
                    int maxScroll = WriterView.GetMaxScrollY(ClientSize.Height, Ribbon);
                    WriterView.TargetScrollY = Math.Max(0, Math.Min(maxScroll, WriterView.TargetScrollY + scrollAmount * 1.5f));
                    WriterView.UpdateSmoothScroll();

                    if (WriterView.IsDraggingShape)
                    {
                        WriterView.UpdateShapeDragPosition(e.X, e.Y, ClientSize.Width, Ribbon);
                    }
                    if (WriterView.IsDraggingChart)
                    {
                        WriterView.UpdateChartDragPosition(e.X, e.Y, ClientSize.Width, Ribbon);
                    }

                    Invalidate();
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                if (ModifierKeys == Keys.Control)
                {
                    float zoomStep = (e.Delta > 0) ? 0.10f : -0.10f;
                    if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                    CalcView.ZoomFactor = Math.Max(0.20f, Math.Min(4.00f, CalcView.ZoomFactor + zoomStep));
                    ZoomLevel = CalcView.ZoomFactor;
                    Invalidate();
                    return;
                }
                else
                {
                    if (e.Delta > 0)
                    {
                        if (CalcView.ScrollRow > 1) CalcView.ScrollRow--;
                    }
                    else
                    {
                        CalcView.ScrollRow++;
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                // ONLY scroll/zoom if cursor is inside the slide area (NOT on Ribbon y <= 155, NOT on Status Bar y >= Height - 26)
                if (e.Y <= 155 || e.Y >= ClientSize.Height - 26)
                {
                    return;
                }

                // 1. Khi giữ phím Ctrl + Lăn chuột: Luôn Zoom phóng to / thu nhỏ canvas slide theo vị trí chuột (kể cả khi đang Xóa nền, Cắt ảnh, Sửa đối tượng)
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (SlidesView != null)
                    {
                        SlidesView.ZoomAt(e.Location, e.Delta, ClientSize.Width, ClientSize.Height);
                        ZoomLevel = SlidesView.ZoomLevel;
                        Invalidate();
                    }
                    return;
                }

                // 2. Cuộn chuột điều chỉnh kích thước bút vẽ trong Chế độ Xóa nền PowerPoint (khi không giữ Ctrl)
                if (SlidesView != null && SlidesView.IsRemovingBackground)
                {
                    if (e.Delta > 0) SlidesView.BgBrushRadius = Math.Min(100f, SlidesView.BgBrushRadius + 2f);
                    else SlidesView.BgBrushRadius = Math.Max(3f, SlidesView.BgBrushRadius - 2f);
                    Invalidate();
                    return;
                }
                else
                {
                    // Reset 2-notch accumulator if last scroll was > 800ms ago
                    if ((DateTime.Now - LastSlideWheelTime).TotalMilliseconds > 800)
                    {
                        SlideWheelTickCount = 0;
                    }
                    LastSlideWheelTime = DateTime.Now;

                    int scrollDir = (e.Delta > 0) ? 1 : -1; // 1 = Up (Prev), -1 = Down (Next)

                    if (e.X <= 210)
                    {
                        // Scroll Thumbnails Pane (Cuộn danh sách slide thu nhỏ)
                        int thumbW = 150;
                        int thumbH = SlidesView.Presentation.IsWidescreen ? (thumbW * 9 / 16) : (thumbW * 3 / 4);
                        int totalThumbH = SlidesView.Presentation.Slides.Count * (thumbH + 14);
                        int visibleH = Math.Max(100, ClientSize.Height - 155 - 26 - 44);
                        int bottomPadding = 120; // Khoảng trống đệm dưới cùng ("đụng slide cuối thì cho 1 khoảng trống chứ nó không quá sát")
                        int maxThumbScroll = Math.Max(0, totalThumbH + bottomPadding - visibleH);

                        int scrollStep = (e.Delta > 0) ? -60 : 60; // Lăn xuống -> tăng ThumbScrollY -> các slide di chuyển lên để thấy slide dưới
                        SlidesView.ThumbScrollY = Math.Max(0, Math.Min(maxThumbScroll, SlidesView.ThumbScrollY + scrollStep));
                    }
                    else
                    {
                        int canvasW = ClientSize.Width - 210 - (SlidesView.IsSidebarOpen ? 280 : 0);
                        int canvasH = ClientSize.Height - 155 - 26;
                        int minScrollX, maxScrollX, minScrollY, maxScrollY;
                        SlidesView.GetScrollLimits(canvasW, canvasH, out minScrollX, out maxScrollX, out minScrollY, out maxScrollY);

                        int oldScroll = SlidesView.CanvasScrollY;
                        int slideScrollStep = (e.Delta > 0) ? -65 : 65;
                        int targetScrollY = Math.Max(minScrollY, Math.Min(maxScrollY, SlidesView.CanvasScrollY + slideScrollStep));

                        // 1. Cho phép cuộn slide lên xuống linh hoạt trong không gian làm việc kể cả khi thu nhỏ
                        if (targetScrollY != oldScroll)
                        {
                            SlidesView.CanvasScrollY = targetScrollY;
                            SlideWheelTickCount = 0;
                        }
                        else
                        {
                            // 2. Khi đã chạm tới kịch mép trên hoặc mép dưới: Yêu cầu cuộn mạnh liên tục (5 nấc lăn) mới chuyển slide như PowerPoint!
                            if ((DateTime.Now - LastSlideWheelTime).TotalMilliseconds > 600)
                            {
                                SlideWheelTickCount = 0;
                            }
                            LastSlideWheelTime = DateTime.Now;

                            if (scrollDir == LastSlideWheelDirection)
                            {
                                SlideWheelTickCount++;
                            }
                            else
                            {
                                LastSlideWheelDirection = scrollDir;
                                SlideWheelTickCount = 1;
                            }

                            if (SlideWheelTickCount >= 5)
                            {
                                SlideWheelTickCount = 0;
                                int prevIdx = SlidesView.ActiveSlideIndex;

                                if (scrollDir < 0 && SlidesView.ActiveSlideIndex < SlidesView.Presentation.Slides.Count - 1)
                                {
                                    SlidesView.ActiveSlideIndex++;
                                    SlidesView.SelectedShapeIndex = -1;
                                    SlidesView.CanvasScrollY = minScrollY + 60;
                                    if (SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                                    {
                                        SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, true);
                                    }
                                }
                                else if (scrollDir > 0 && SlidesView.ActiveSlideIndex > 0)
                                {
                                    SlidesView.ActiveSlideIndex--;
                                    SlidesView.SelectedShapeIndex = -1;
                                    SlidesView.CanvasScrollY = maxScrollY - 60;
                                    if (SlidesView.ActiveSlide != null && SlidesView.ActiveSlide.Transition != TransitionType.None)
                                    {
                                        SlidesView.TriggerTransition(prevIdx, SlidesView.ActiveSlideIndex, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                if (ModifierKeys == Keys.Control)
                {
                    if (e.Delta > 0) PdfView.ZoomLevel = Math.Min(3.0f, PdfView.ZoomLevel + 0.1f);
                    else PdfView.ZoomLevel = Math.Max(0.2f, PdfView.ZoomLevel - 0.1f);
                    PdfView.InvalidateAllCaches();
                    Invalidate();
                }
                else
                {
                    int viewH = ClientSize.Height - 155 - 26;
                    if (PdfView.ShowThumbnailsSidebar && e.X <= PdfView.SidebarWidth)
                    {
                        // Cuộn thanh trang thu nhỏ Thumbnail ở Sidebar bên trái
                        int maxSidebarScroll = PdfView.GetMaxSidebarScrollY(viewH - 36);
                        int sideScrollAmount = (e.Delta > 0) ? -80 : 80;
                        PdfView.TargetSidebarScrollY = Math.Max(0, Math.Min(maxSidebarScroll, PdfView.TargetSidebarScrollY + sideScrollAmount));
                        PdfView.SidebarScrollY = PdfView.TargetSidebarScrollY;
                    }
                    else
                    {
                        // Cuộn các trang PDF chính
                        int maxScroll = PdfView.GetMaxScrollY(viewH);
                        int wheelStep = (e.Delta > 0) ? -130 : 130;
                        PdfView.TargetScrollY = Math.Max(0, Math.Min(maxScroll, PdfView.TargetScrollY + wheelStep));
                        PdfView.ScrollY = PdfView.TargetScrollY;
                        PdfView.UpdateActivePageFromScroll(ClientSize.Width, ClientSize.Height, 155, 26);
                    }
                    Invalidate();
                }
            }
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            WriterFloatingMiniToolbarForm.CloseCurrent();
            if (!SlidesView.IsPresenting)
            {
                // Status Bar Interactions (Y >= height - 26, chỉ áp dụng cho Writer, Calc, Slides)
                if (Ribbon.CurrentMode != OfficeAppMode.Home && e.Y >= ClientSize.Height - 26)
                {
                    int w = ClientSize.Width;

                    // View Mode Icons (Read Mode 0, Print Layout 1, Web Layout 2)
                    int modeX = w - 246;
                    for (int m = 0; m < 3; m++)
                    {
                        if (e.X >= modeX + m * 24 && e.X <= modeX + m * 24 + 22)
                        {
                            ActiveViewMode = m;
                            WriterView.ViewMode = m;
                            Invalidate();
                            return;
                        }
                    }

                    // Zoom Minus (-)
                    if (e.X >= w - 164 && e.X < w - 144)
                    {
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            float step = (SlidesView.ZoomLevel > 1.5f) ? 0.25f : 0.10f;
                            SlidesView.ZoomLevel = Math.Max(0.10f, SlidesView.ZoomLevel - step);
                            ZoomLevel = SlidesView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            PdfView.ZoomLevel = Math.Max(0.20f, PdfView.ZoomLevel - 0.10f);
                            PdfView.InvalidateAllCaches();
                            ZoomLevel = PdfView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                            CalcView.ZoomFactor = Math.Max(0.20f, CalcView.ZoomFactor - 0.10f);
                            ZoomLevel = CalcView.ZoomFactor;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            WriterView.ZoomLevel = Math.Max(0.20f, WriterView.ZoomLevel - 0.10f);
                            ZoomLevel = WriterView.ZoomLevel;
                        }
                        Invalidate();
                        return;
                    }

                    // Zoom Plus (+)
                    if (e.X >= w - 68 && e.X < w - 48)
                    {
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            float step = (SlidesView.ZoomLevel >= 1.5f) ? 0.25f : 0.10f;
                            SlidesView.ZoomLevel = Math.Min(4.00f, SlidesView.ZoomLevel + step);
                            ZoomLevel = SlidesView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            PdfView.ZoomLevel = Math.Min(3.00f, PdfView.ZoomLevel + 0.10f);
                            PdfView.InvalidateAllCaches();
                            ZoomLevel = PdfView.ZoomLevel;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            if (CalcView.ZoomFactor <= 0) CalcView.ZoomFactor = 1.0f;
                            CalcView.ZoomFactor = Math.Min(4.00f, CalcView.ZoomFactor + 0.10f);
                            ZoomLevel = CalcView.ZoomFactor;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            WriterView.ZoomLevel = Math.Min(2.50f, WriterView.ZoomLevel + 0.10f);
                            ZoomLevel = WriterView.ZoomLevel;
                        }
                        Invalidate();
                        return;
                    }

                    // Zoom Slider Track Bar (70px) - Enable Dragging!
                    if (e.X >= w - 146 && e.X <= w - 68)
                    {
                        IsDraggingZoomSlider = true;
                        UpdateZoomFromMouseX(e.X);
                        return;
                    }

                    // Zoom Percentage Reset (100%)
                    if (e.X >= w - 46 && e.X <= w - 4)
                    {
                        if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                        {
                            SlidesView.ZoomLevel = 1.0f; // 100% standard fit for slides
                            SlidesView.CanvasScrollX = 0;
                            SlidesView.CanvasScrollY = 0;
                            ZoomLevel = 1.0f;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                        {
                            PdfView.ZoomLevel = 1.0f;
                            PdfView.InvalidateAllCaches();
                            ZoomLevel = 1.0f;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                        {
                            CalcView.ZoomFactor = 1.0f;
                            ZoomLevel = 1.0f;
                        }
                        else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                        {
                            WriterView.ZoomLevel = 1.0f;
                            ZoomLevel = 1.0f;
                        }
                        Invalidate();
                        return;
                    }
                }

                int ribbonH = (Ribbon.CurrentMode == OfficeAppMode.Home) ? 64 : 155;
                if (e.Y <= ribbonH)
                {
                    if (e.Y <= 36 && e.Button == MouseButtons.Left)
                    {
                        Ribbon.StartTabDrag(e.X, e.Y, ClientSize.Width);
                    }
                    Ribbon.HandleClick(e.X, e.Y, WriterView, this, SlidesView, CalcView, PdfView);
                    Invalidate();
                    return;
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Home)
                {
                    string docTitle;
                    OfficeAppMode? clickedMode = HomeView.HandleClick(e.X, e.Y, ClientSize.Width, ClientSize.Height, out docTitle);
                    if (clickedMode.HasValue)
                    {
                        Ribbon.AddDocTab(docTitle, clickedMode.Value, SlidesView, WriterView, CalcView, PdfView);
                        if (clickedMode.Value == OfficeAppMode.Writer)
                        {
                            var rDoc = HomeView.RecentDocs.Find(d => d.Title == docTitle);
                            if (rDoc != null && System.IO.File.Exists(rDoc.FilePath))
                            {
                                WriterView.LoadDocumentFile(rDoc.FilePath, Ribbon);
                            }
                            else
                            {
                                WriterView.PieceTable = new PieceTable("");
                                WriterView.FormattedRuns.Clear();
                                WriterView.CursorPosition = 0;
                            }
                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                        }
                        else if (clickedMode.Value == OfficeAppMode.Slides && SlidesView != null)
                        {
                            var rDoc = HomeView.RecentDocs.Find(d => d.Title == docTitle);
                            if (rDoc != null && System.IO.File.Exists(rDoc.FilePath))
                            {
                                SlidesView.Presentation = GenOffice.Format.PptxHandler.LoadPptx(rDoc.FilePath);
                            }
                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                        }
                        else if (clickedMode.Value == OfficeAppMode.Calc && CalcView != null)
                        {
                            var rDoc = HomeView.RecentDocs.Find(d => d.Title == docTitle);
                            if (rDoc != null && System.IO.File.Exists(rDoc.FilePath))
                            {
                                GenOffice.Format.XlsxHandler.LoadXlsxOrCsv(rDoc.FilePath, CalcView);
                            }
                            else
                            {
                                CalcView.PopulateSampleData(docTitle);
                            }
                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                        }
                        else if (clickedMode.Value == OfficeAppMode.Pdf && PdfView != null)
                        {
                            var rDoc = HomeView.RecentDocs.Find(d => d.Title == docTitle);
                            if (rDoc != null && System.IO.File.Exists(rDoc.FilePath))
                            {
                                PdfView.Document = GenOffice.Format.PdfHandler.LoadPdf(rDoc.FilePath);
                            }
                            else
                            {
                                PdfView.Document = new PdfDocument(docTitle);
                            }
                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                        }
                    }
                    Invalidate();
                    return;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    if (WriterView.ShowSelectionPane && e.X >= ClientSize.Width - 260 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        if (WriterView.HandleSelectionPaneMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                        {
                            Invalidate();
                            return;
                        }
                    }

                    if (WriterView.ShowNavigationPane && e.X <= 280 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        if (WriterView.HandleNavMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                        {
                            Invalidate();
                            return;
                        }
                    }
                    else if (WriterView.ShowNavigationPane && e.X > 280)
                    {
                        WriterView.NavSearchFocused = false;
                    }

                    if (e.Button == MouseButtons.Right)
                    {
                        WriterView.HandleRightClick(e.X, e.Y, this, Ribbon);
                        Invalidate();
                        return;
                    }

                    if (e.Clicks == 2)
                    {
                        using (Graphics g = CreateGraphics())
                        {
                            WriterView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, g, Ribbon);
                        }
                        Invalidate();
                        return;
                    }

                    using (Graphics g = CreateGraphics())
                    {
                        WriterView.HandleMouseDown(e.X, e.Y, ClientSize.Width, g, Ribbon);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        CalcView.HandleRightClick(e.X, e.Y, this, Ribbon);
                        Invalidate();
                        return;
                    }
                    bool handled = CalcView.HandleMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button, this);
                    if (!handled)
                    {
                        CalcView.HandleClick(e.X, e.Y, ClientSize.Height);
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleMouseDown(e.Location, ClientSize.Width, ClientSize.Height, e.Button, this);
                    if (SlidesView.SelectedShapeIndex >= 0)
                    {
                        Ribbon.ActiveRibbonTab = 7; // Tự động mở tab Thiết kế Bảng hoặc Định dạng Hình dạng
                    }
                    else if (Ribbon.ActiveRibbonTab == 7)
                    {
                        Ribbon.ActiveRibbonTab = 0; // Nhấp ra ngoài vùng trống -> Quay về tab Trang đầu
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleMouseDown(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button, this);
                }
                Invalidate();
            }
            else
            {
                SlidesView.HandleMouseDown(e.Location, ClientSize.Width, ClientSize.Height, e.Button, this);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left && (Ribbon.IsDraggingTab || Ribbon.DraggingTabIndex >= 1))
            {
                Ribbon.UpdateTabDrag(e.X, ClientSize.Width);
                if (Ribbon.IsDraggingTab)
                {
                    Invalidate(new Rectangle(0, 0, ClientSize.Width, 36));
                    return;
                }
            }

            if (IsDraggingZoomSlider && e.Button == MouseButtons.Left)
            {
                UpdateZoomFromMouseX(e.X);
                Invalidate();
                return;
            }

            bool hoverMoved = (Ribbon.HoverX != e.X || Ribbon.HoverY != e.Y);
            if (hoverMoved)
            {
                Ribbon.UpdateHover(e.X, e.Y);
                if (e.Y <= 155)
                {
                    Cursor = Cursors.Default;
                    Cursor.Current = Cursors.Default;
                    Invalidate(new Rectangle(0, 0, ClientSize.Width, 155));
                }
            }

            if (e.Y <= 155 || e.Y >= ClientSize.Height - 25)
            {
                Cursor = Cursors.Default;
                Cursor.Current = Cursors.Default;
            }

            if (!SlidesView.IsPresenting)
            {
                if (Ribbon.CurrentMode == OfficeAppMode.Home)
                {
                    if (HomeView.HandleMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height))
                    {
                        Invalidate();
                    }
                    if (HomeView.HoverTabIndex >= 0 || HomeView.HoverCardIndex >= 0 || HomeView.HoverRowIndex >= 0 || HomeView.HoverHelpBtn)
                    {
                        Cursor = Cursors.Hand;
                        Cursor.Current = Cursors.Hand;
                    }
                    else if (e.Y > 64)
                    {
                        Cursor = Cursors.Default;
                        Cursor.Current = Cursors.Default;
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    if (WriterView.ShowSelectionPane && e.X >= ClientSize.Width - 260 && e.Y >= 155 && e.Y <= ClientSize.Height - 26)
                    {
                        WriterView.HandleSelectionPaneMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                        Invalidate();
                    }

                    using (Graphics g = CreateGraphics())
                    {
                        WriterView.HandleMouseMove(e.X, e.Y, ClientSize.Width, g, Ribbon);
                    }
                    this.Cursor = Cursor.Current;
                    if (e.Button == MouseButtons.Left)
                    {
                        Invalidate();
                    }
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    CalcView.HandleMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = CalcView.GetCursorForPosition(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleMouseMove(e.Location, ClientSize.Width, ClientSize.Height);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = SlidesView.GetCursorForPosition(e.Location, ClientSize.Width, ClientSize.Height);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleMouseMove(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                    if (e.Y > 155 && e.Y < ClientSize.Height - 25)
                    {
                        Cursor newCur = PdfView.GetCursorAtPoint(e.X, e.Y, ClientSize.Width, ClientSize.Height, 155);
                        Cursor = newCur;
                        Cursor.Current = newCur;
                    }
                    Invalidate();
                }
            }
            else
            {
                SlidesView.HandleMouseMove(e.Location, ClientSize.Width, ClientSize.Height);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            IsDraggingZoomSlider = false;

            if (Ribbon.IsDraggingTab)
            {
                Ribbon.FinishTabDrag(ClientSize.Width);
                Invalidate();
                return;
            }
            else if (Ribbon.DraggingTabIndex >= 1)
            {
                Ribbon.CancelTabDrag();
            }
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                using (Graphics g = CreateGraphics())
                {
                    WriterView.HandleMouseUp(e.X, e.Y, ClientSize.Width, g, Ribbon);
                }
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.HandleMouseUp(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                bool wasDrawing = SlidesView.IsDrawingActive;
                SlidesView.HandleMouseUp(e.Location, ClientSize.Width, ClientSize.Height);
                if (wasDrawing && SlidesView.SelectedShapeIndex >= 0)
                {
                    Ribbon.ActiveRibbonTab = 7; // Vừa vẽ xong -> Mở ngay tab Định dạng Hình dạng
                }
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.HandleMouseUp(e.X, e.Y, ClientSize.Width, ClientSize.Height, e.Button);
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                using (Graphics g = CreateGraphics())
                {
                    WriterView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, g, Ribbon);
                }
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                CalcView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                SlidesView.HandleDoubleClick(e.Location, ClientSize.Width, ClientSize.Height, this);
                Invalidate();
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                PdfView.HandleDoubleClick(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            WriterFloatingMiniToolbarForm.CloseCurrent();
            if (!SlidesView.IsPresenting)
            {
                if (char.IsControl(e.KeyChar) && e.KeyChar != '\r' && e.KeyChar != '\b')
                {
                    return;
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    WriterView.CursorVisible = true;
                    if (WriterView.ShowNavigationPane && WriterView.NavSearchFocused)
                    {
                        if (e.KeyChar == '\b')
                        {
                            if (WriterView.NavSearchQuery.Length > 0)
                            {
                                WriterView.NavSearchQuery = WriterView.NavSearchQuery.Substring(0, WriterView.NavSearchQuery.Length - 1);
                                WriterView.PerformNavSearch();
                            }
                        }
                        else if (e.KeyChar == '\r' || e.KeyChar == '\n')
                        {
                            WriterView.SelectNextNavMatch();
                        }
                        else if (!char.IsControl(e.KeyChar))
                        {
                            WriterView.NavSearchQuery += e.KeyChar;
                            WriterView.PerformNavSearch();
                        }
                        Invalidate();
                        return;
                    }

                    if (WriterView.IsEditingHeaderFooter)
                    {
                        if (e.KeyChar == '\b')
                        {
                            if (WriterView.IsEditingHeaderMode)
                            {
                                if (WriterView.IsHeaderTextSelected)
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0) WriterView.ThreeColHeaderLeft = "";
                                        else if (WriterView.ActiveThreeColIndex == 1) WriterView.ThreeColHeaderCenter = "";
                                        else if (WriterView.ActiveThreeColIndex == 2) WriterView.ThreeColHeaderRight = "";
                                    }
                                    else
                                    {
                                        WriterView.HeaderTitleText = "";
                                        WriterView.HeaderText = "";
                                    }
                                    WriterView.IsHeaderTextSelected = false;
                                }
                                else
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0)
                                        {
                                            if (WriterView.ThreeColHeaderLeft == "[Nhập tại đây]") WriterView.ThreeColHeaderLeft = "";
                                            else if (WriterView.ThreeColHeaderLeft.Length > 0) WriterView.ThreeColHeaderLeft = WriterView.ThreeColHeaderLeft.Substring(0, WriterView.ThreeColHeaderLeft.Length - 1);
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 1)
                                        {
                                            if (WriterView.ThreeColHeaderCenter == "[Nhập tại đây]") WriterView.ThreeColHeaderCenter = "";
                                            else if (WriterView.ThreeColHeaderCenter.Length > 0) WriterView.ThreeColHeaderCenter = WriterView.ThreeColHeaderCenter.Substring(0, WriterView.ThreeColHeaderCenter.Length - 1);
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 2)
                                        {
                                            if (WriterView.ThreeColHeaderRight == "[Nhập tại đây]") WriterView.ThreeColHeaderRight = "";
                                            else if (WriterView.ThreeColHeaderRight.Length > 0) WriterView.ThreeColHeaderRight = WriterView.ThreeColHeaderRight.Substring(0, WriterView.ThreeColHeaderRight.Length - 1);
                                        }
                                    }
                                    else
                                    {
                                        string curT = WriterView.HeaderTitleText;
                                        if (curT == "TIÊU ĐỀ ĐẦU TRANG" || curT == "[Tiêu đề tài liệu]" || curT == "[Nhập tại đây]")
                                        {
                                            WriterView.HeaderTitleText = "";
                                            WriterView.HeaderText = "";
                                        }
                                        else if (WriterView.HeaderTitleText.Length > 0)
                                        {
                                            WriterView.HeaderTitleText = WriterView.HeaderTitleText.Substring(0, WriterView.HeaderTitleText.Length - 1);
                                            WriterView.HeaderText = WriterView.HeaderTitleText;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (WriterView.FooterText.Length > 0) WriterView.FooterText = WriterView.FooterText.Substring(0, WriterView.FooterText.Length - 1);
                            }
                        }
                        else if (e.KeyChar != '\r')
                        {
                            if (WriterView.IsEditingHeaderMode)
                            {
                                if (WriterView.IsHeaderTextSelected)
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0) WriterView.ThreeColHeaderLeft = e.KeyChar.ToString();
                                        else if (WriterView.ActiveThreeColIndex == 1) WriterView.ThreeColHeaderCenter = e.KeyChar.ToString();
                                        else if (WriterView.ActiveThreeColIndex == 2) WriterView.ThreeColHeaderRight = e.KeyChar.ToString();
                                    }
                                    else
                                    {
                                        WriterView.HeaderTitleText = e.KeyChar.ToString();
                                        WriterView.HeaderText = WriterView.HeaderTitleText;
                                    }
                                    WriterView.IsHeaderTextSelected = false;
                                }
                                else
                                {
                                    if (WriterView.CurrentHeaderPreset == HeaderFooterPreset.ThreeColumns)
                                    {
                                        if (WriterView.ActiveThreeColIndex == 0)
                                        {
                                            if (WriterView.ThreeColHeaderLeft == "[Nhập tại đây]") WriterView.ThreeColHeaderLeft = "";
                                            WriterView.ThreeColHeaderLeft += e.KeyChar;
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 1)
                                        {
                                            if (WriterView.ThreeColHeaderCenter == "[Nhập tại đây]") WriterView.ThreeColHeaderCenter = "";
                                            WriterView.ThreeColHeaderCenter += e.KeyChar;
                                        }
                                        else if (WriterView.ActiveThreeColIndex == 2)
                                        {
                                            if (WriterView.ThreeColHeaderRight == "[Nhập tại đây]") WriterView.ThreeColHeaderRight = "";
                                            WriterView.ThreeColHeaderRight += e.KeyChar;
                                        }
                                    }
                                    else
                                    {
                                        string curT = WriterView.HeaderTitleText;
                                        if (curT == "TIÊU ĐỀ ĐẦU TRANG" || curT == "[Tiêu đề tài liệu]" || curT == "[Nhập tại đây]")
                                        {
                                            WriterView.HeaderTitleText = "";
                                            WriterView.HeaderText = "";
                                        }
                                        WriterView.HeaderTitleText += e.KeyChar;
                                        WriterView.HeaderText = WriterView.HeaderTitleText;
                                    }
                                }
                            }
                            else
                            {
                                WriterView.FooterText += e.KeyChar;
                            }
                        }
                    }
                    else if (WriterView.ActiveChart != null)
                    {
                        // Khi đang chọn biểu đồ: Không ghi vào trang văn bản
                        e.Handled = true;
                        return;
                    }
                    else if (WriterView.ActiveShape != null)
                    {
                        var sh = WriterView.ActiveShape;
                        if (sh.ShapeType == "Image")
                        {
                            // Khi đang chọn hình ảnh: Không ghi vào trang văn bản
                            e.Handled = true;
                            return;
                        }

                        if (!sh.IsEditingText)
                        {
                            if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r')
                            {
                                sh.IsEditingText = true;
                                sh.TextContent = (sh.TextContent ?? "") + (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                sh.TextCursorIndex = sh.TextContent.Length;
                                MarkActiveTabModified();
                            }
                            e.Handled = true;
                            Invalidate();
                            return;
                        }

                        if (sh.TextContent == null) sh.TextContent = "";
                        int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                        if (e.KeyChar == (char)Keys.Back)
                        {
                            if (cur > 0 && sh.TextContent.Length > 0)
                            {
                                sh.TextContent = sh.TextContent.Substring(0, cur - 1) + sh.TextContent.Substring(cur);
                                sh.TextCursorIndex = cur - 1;
                            }
                        }
                        else if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r' || e.KeyChar == '\n')
                        {
                            string ins = (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                            sh.TextContent = sh.TextContent.Insert(cur, ins);
                            sh.TextCursorIndex = cur + ins.Length;
                        }
                        MarkActiveTabModified();
                        e.Handled = true;
                        Invalidate();
                        return;
                    }
                    else
                    {
                        WriterView.HandleChar(e.KeyChar);
                        MarkActiveTabModified();
                    }
                    Invalidate();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (CalcView.SelectedShape != null)
                    {
                        var sh = CalcView.SelectedShape;
                        if (sh.ShapeType != "Image")
                        {
                            if (!sh.IsEditingText)
                            {
                                if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r')
                                {
                                    sh.IsEditingText = true;
                                    sh.IsSelected = true;
                                    sh.TextContent = (sh.TextContent ?? "") + (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                    sh.TextCursorIndex = sh.TextContent.Length;
                                    MarkActiveTabModified();
                                }
                                e.Handled = true;
                                Invalidate();
                                return;
                            }

                            if (sh.TextContent == null) sh.TextContent = "";
                            int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                            if (e.KeyChar == (char)Keys.Back)
                            {
                                if (cur > 0 && sh.TextContent.Length > 0)
                                {
                                    sh.TextContent = sh.TextContent.Substring(0, cur - 1) + sh.TextContent.Substring(cur);
                                    sh.TextCursorIndex = cur - 1;
                                }
                            }
                            else if (!char.IsControl(e.KeyChar) || e.KeyChar == '\r' || e.KeyChar == '\n')
                            {
                                string ins = (e.KeyChar == '\r' ? "\n" : e.KeyChar.ToString());
                                sh.TextContent = sh.TextContent.Insert(cur, ins);
                                sh.TextCursorIndex = cur + ins.Length;
                            }
                            MarkActiveTabModified();
                            e.Handled = true;
                            Invalidate();
                            return;
                        }
                    }
                    CalcView.HandleChar(e.KeyChar);
                    MarkActiveTabModified();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleKeyPress(e.KeyChar);
                    MarkActiveTabModified();
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleKeyPress(e.KeyChar);
                    MarkActiveTabModified();
                }
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (SlidesView.IsPresenting)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    StopSlideShow();
                    return;
                }
                SlidesView.HandleKeyDown(e.KeyCode);
                Invalidate();
                if (_audienceForm != null) _audienceForm.Invalidate();
            }
            else
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && WriterView.IsFormatPainterActive)
                    {
                        WriterView.CancelFormatPainter();
                        Invalidate();
                        return;
                    }
                    if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null && CalcView.IsFormatPainterActive)
                    {
                        CalcView.IsFormatPainterActive = false;
                        Invalidate();
                        return;
                    }
                }

                if (e.KeyCode == Keys.Tab)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        if (e.Shift) WriterView.Outdent();
                        else WriterView.IndentOrInsertTab();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                // --- Phím chức năng F1, F4, F12, F5 ---
                if (e.KeyCode == Keys.F1)
                {
                    ShowShortcutsHelpDialog();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.F4)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null) WriterView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView != null) SlidesView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null) CalcView.Redo();
                    else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null) PdfView.Redo();
                    MarkActiveTabModified();
                    Invalidate();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.F12)
                {
                    PerformSaveAs();
                    e.Handled = true;
                    return;
                }

                if (e.Shift && e.KeyCode == Keys.F3)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.ChangeCaseCycle();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Alt && e.Shift && e.KeyCode == Keys.D)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertCurrentDate();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Alt && e.Shift && e.KeyCode == Keys.T)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertCurrentTime();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Alt && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertEquation();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Shift && e.KeyCode == Keys.Enter && !e.Control)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null && WriterView.ActiveShape == null && WriterView.ActiveChart == null)
                    {
                        WriterView.InsertSoftLineBreak();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Control && e.Alt && e.KeyCode == Keys.M)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.InsertNewComment();
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Control && e.KeyCode == Keys.Space)
                {
                    if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                    {
                        WriterView.ClearFormatting();
                        WriterView.UpdateRibbonState(Ribbon);
                        MarkActiveTabModified();
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.P:
                            PerformPrint();
                            return;
                        case Keys.H:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                var frForm = new FindReplaceForm();
                                frForm.OnFindNext += (findText) =>
                                {
                                    string full = WriterView.PieceTable.GetText();
                                    int next = full.IndexOf(findText, WriterView.CursorPosition, StringComparison.OrdinalIgnoreCase);
                                    if (next < 0) next = full.IndexOf(findText, 0, StringComparison.OrdinalIgnoreCase);
                                    if (next >= 0)
                                    {
                                        WriterView.SelectionStart = next;
                                        WriterView.SelectionEnd = next + findText.Length;
                                        WriterView.CursorPosition = WriterView.SelectionEnd;
                                        Invalidate();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Không tìm thấy '" + findText + "' trong tài liệu.", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                };
                                frForm.OnReplace += (findText, repText) =>
                                {
                                    string full = WriterView.PieceTable.GetText();
                                    int selMin = Math.Min(WriterView.SelectionStart, WriterView.SelectionEnd);
                                    int selLen = Math.Abs(WriterView.SelectionEnd - WriterView.SelectionStart);
                                    if (selLen > 0 && full.Substring(selMin, selLen).Equals(findText, StringComparison.OrdinalIgnoreCase))
                                    {
                                        WriterView.SaveUndoState();
                                        WriterView.PieceTable.Remove(selMin, selLen);
                                        WriterView.PieceTable.Insert(selMin, repText);
                                        WriterView.SelectionStart = selMin;
                                        WriterView.SelectionEnd = selMin + repText.Length;
                                        WriterView.CursorPosition = WriterView.SelectionEnd;
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                };
                                frForm.OnReplaceAll += (findText, repText) =>
                                {
                                    string full = WriterView.PieceTable.GetText();
                                    if (full.IndexOf(findText, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        WriterView.SaveUndoState();
                                        int count = 0;
                                        int idx = 0;
                                        while ((idx = full.IndexOf(findText, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                                        {
                                            WriterView.PieceTable.Remove(idx, findText.Length);
                                            WriterView.PieceTable.Insert(idx, repText);
                                            idx += repText.Length;
                                            count++;
                                            full = WriterView.PieceTable.GetText();
                                        }
                                        MarkActiveTabModified();
                                        Invalidate();
                                        MessageBox.Show("Đã thay thế tất cả " + count + " vị trí trùng khớp!", "Tìm kiếm & Thay thế", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                };
                                frForm.Show(this);
                                return;
                            }
                            break;
                        case Keys.K:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                int selMin = Math.Min(WriterView.SelectionStart, WriterView.SelectionEnd);
                                int selLen = Math.Abs(WriterView.SelectionEnd - WriterView.SelectionStart);
                                string selTxt = (selLen > 0) ? WriterView.PieceTable.GetText().Substring(selMin, selLen) : "";
                                using (var hDlg = new InsertHyperlinkForm(selTxt))
                                {
                                    if (hDlg.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.InsertHyperlink(hDlg.DisplayText, hDlg.TargetUrl, hDlg.ScreenTip);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                            {
                                CellData cell = CalcView.CurrentSheet.GetCell(CalcView.SelectedRow, CalcView.SelectedCol);
                                string curTxt = cell != null ? (cell.RawValue ?? "") : "";
                                using (var hDlg = new InsertHyperlinkForm(curTxt))
                                {
                                    if (hDlg.ShowDialog(this) == DialogResult.OK)
                                    {
                                        CalcView.InsertHyperlink(hDlg.DisplayText, hDlg.TargetUrl, hDlg.ScreenTip);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            break;
                        case Keys.G:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ShowNavigationPane = true;
                                WriterView.NavSearchFocused = true;
                                WriterView.NavActiveTab = 0;
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.L:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ApplyAlignmentToSelectionOrParagraph(0);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.E:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ApplyAlignmentToSelectionOrParagraph(1);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.R:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ApplyAlignmentToSelectionOrParagraph(2);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.J:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ApplyAlignmentToSelectionOrParagraph(3);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D1:
                        case Keys.NumPad1:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetLineSpacing(1.0f);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D2:
                        case Keys.NumPad2:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetLineSpacing(2.0f);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D5:
                        case Keys.NumPad5:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetLineSpacing(1.5f);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.D0:
                        case Keys.NumPad0:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleSpaceBeforeParagraph();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.ZoomActualSize();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.M:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                if (e.Shift) WriterView.Outdent();
                                else WriterView.IndentOrInsertTab();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.T:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.SetHangingIndent(!e.Shift);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Q:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ClearParagraphFormatting();
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.OemCloseBrackets: // Ctrl + ] (Tăng 1pt)
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.IncreaseFontSize(1f);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.OemOpenBrackets: // Ctrl + [ (Giảm 1pt)
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.DecreaseFontSize(1f);
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.OemPeriod: // Ctrl + Shift + >
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.IncreaseFontSizePreset();
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Oemcomma: // Ctrl + Shift + <
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.DecreaseFontSizePreset();
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.F:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                // Ctrl + Shift + F: Font Dialog
                                using (FontDialog fd = new FontDialog())
                                {
                                    fd.Font = new Font(!string.IsNullOrEmpty(WriterView.TypingFont) ? WriterView.TypingFont : "Times New Roman", WriterView.TypingFontSize > 0 ? WriterView.TypingFontSize : 12f);
                                    fd.ShowColor = true;
                                    fd.Color = WriterView.TypingColor;
                                    if (fd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.ApplyFontToSelectionOrTyping(fd.Font.Name);
                                        WriterView.ApplyFontSizeToSelectionOrTyping(fd.Font.SizeInPoints);
                                        WriterView.ApplyBoldToSelectionOrTyping(fd.Font.Bold);
                                        WriterView.ApplyItalicToSelectionOrTyping(fd.Font.Italic);
                                        WriterView.ApplyUnderlineToSelectionOrTyping(fd.Font.Underline);
                                        WriterView.ApplyStrikeToSelectionOrTyping(fd.Font.Strikeout);
                                        WriterView.ApplyFontColorToSelectionOrTyping(fd.Color);
                                        WriterView.UpdateRibbonState(Ribbon);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                WriterView.ShowNavigationPane = true;
                                WriterView.NavSearchFocused = true;
                                WriterView.NavActiveTab = 2; // Results tab
                                WriterView.PerformNavSearch();
                                Invalidate();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                PdfView.ShowSearchBar = !PdfView.ShowSearchBar;
                                Invalidate();
                            }
                            return;
                        case Keys.Z:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Undo();
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Undo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                CalcView.Undo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                PdfView.Undo();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.Y:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Redo();
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Redo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                CalcView.Redo();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                PdfView.Redo();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.C:
                            if (e.Shift)
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null) WriterView.CopyFormatting();
                                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null) CalcView.IsFormatPainterActive = true;
                            }
                            else
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer) WriterView.Copy();
                                else if (Ribbon.CurrentMode == OfficeAppMode.Slides) SlidesView.Copy();
                                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.EditingText != null)
                                {
                                    try { if (!string.IsNullOrEmpty(PdfView.EditingBuffer)) Clipboard.SetText(PdfView.EditingBuffer); } catch { }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.Enter:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                if (e.Shift) WriterView.InsertColumnBreak();
                                else WriterView.InsertPageBreak();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.D8:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ShowFormattingMarks = !WriterView.ShowFormattingMarks;
                                Invalidate();
                            }
                            return;
                        case Keys.X:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                WriterView.Cut();
                                MarkActiveTabModified();
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Cut();
                                MarkActiveTabModified();
                            }
                            Invalidate();
                            return;
                        case Keys.V:
                            if (e.Shift)
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                                {
                                    WriterView.PasteFormatting();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Calc && CalcView != null)
                                {
                                    CalcView.PasteFormatting();
                                    MarkActiveTabModified();
                                }
                            }
                            else
                            {
                                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                                {
                                    WriterView.Paste();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                                {
                                    SlidesView.Paste();
                                    MarkActiveTabModified();
                                }
                                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.EditingText != null)
                                {
                                    try
                                    {
                                        if (Clipboard.ContainsText())
                                        {
                                            string pasteTxt = Clipboard.GetText();
                                            if (!string.IsNullOrEmpty(pasteTxt))
                                            {
                                                if (PdfView.EditingBuffer == null) PdfView.EditingBuffer = "";
                                                int cur = Math.Max(0, Math.Min(PdfView.CursorPosition, PdfView.EditingBuffer.Length));
                                                PdfView.EditingBuffer = PdfView.EditingBuffer.Insert(cur, pasteTxt);
                                                PdfView.CursorPosition = cur + pasteTxt.Length;
                                                PdfView.EditingText.Text = PdfView.EditingBuffer;
                                                PdfView.Document.IsModified = true;
                                                MarkActiveTabModified();
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.D:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleDoubleUnderline();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                using (FontDialog fd = new FontDialog())
                                {
                                    fd.Font = new Font(!string.IsNullOrEmpty(WriterView.TypingFont) ? WriterView.TypingFont : "Times New Roman", WriterView.TypingFontSize > 0 ? WriterView.TypingFontSize : 12f);
                                    fd.ShowColor = true;
                                    fd.Color = WriterView.TypingColor;
                                    if (fd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.ApplyFontToSelectionOrTyping(fd.Font.Name);
                                        WriterView.ApplyFontSizeToSelectionOrTyping(fd.Font.SizeInPoints);
                                        WriterView.ApplyBoldToSelectionOrTyping(fd.Font.Bold);
                                        WriterView.ApplyItalicToSelectionOrTyping(fd.Font.Italic);
                                        WriterView.ApplyUnderlineToSelectionOrTyping(fd.Font.Underline);
                                        WriterView.ApplyStrikeToSelectionOrTyping(fd.Font.Strikeout);
                                        WriterView.ApplyFontColorToSelectionOrTyping(fd.Color);
                                        WriterView.UpdateRibbonState(Ribbon);
                                        MarkActiveTabModified();
                                        Invalidate();
                                    }
                                }
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                SlidesView.Duplicate();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.A:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ApplyAllCaps();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                WriterView.SelectAll();
                                WriterView.UpdateRibbonState(Ribbon);
                            }
                            Invalidate();
                            return;
                        case Keys.B:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                Ribbon.IsBold = !Ribbon.IsBold;
                                WriterView.ApplyBoldToSelectionOrTyping(Ribbon.IsBold);
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView.SelectedShape != null)
                            {
                                SlidesView.SelectedShape.Bold = !SlidesView.SelectedShape.Bold;
                            }
                            Invalidate();
                            return;
                        case Keys.I:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                Ribbon.IsItalic = !Ribbon.IsItalic;
                                WriterView.ApplyItalicToSelectionOrTyping(Ribbon.IsItalic);
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView.SelectedShape != null)
                            {
                                SlidesView.SelectedShape.Italic = !SlidesView.SelectedShape.Italic;
                            }
                            Invalidate();
                            return;
                        case Keys.U:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                Ribbon.IsUnderline = !Ribbon.IsUnderline;
                                WriterView.ApplyUnderlineToSelectionOrTyping(Ribbon.IsUnderline);
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides && SlidesView.SelectedShape != null)
                            {
                                SlidesView.SelectedShape.Underline = !SlidesView.SelectedShape.Underline;
                            }
                            Invalidate();
                            return;
                        case Keys.S:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                Ribbon.HandleClick(64, 80, WriterView, this);
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Filter = "Tệp Trình Chiếu PowerPoint (*.pptx)|*.pptx|Tệp XML Presentation (*.xml)|*.xml";
                                    sfd.Title = "Lưu Bản Trình Chiếu Microsoft PowerPoint 2016-365";
                                    if (sfd.ShowDialog() == DialogResult.OK)
                                    {
                                        GenOffice.Format.PptxHandler.SavePptx(sfd.FileName, SlidesView.Presentation);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        MessageBox.Show("Đã lưu bản trình chiếu chuẩn .pptx thành công!", "GENOFFICE PowerPoint", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Title = "Lưu Bảng tính GENOFFICE Calc (Chuẩn Microsoft Excel .xlsx / .csv)";
                                    sfd.Filter = "Tệp Excel (*.xlsx)|*.xlsx|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*";
                                    if (sfd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        GenOffice.Format.XlsxHandler.SaveXlsxOrCsv(sfd.FileName, CalcView);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        MessageBox.Show("Đã lưu bảng tính thành công!", "GENOFFICE Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                using (SaveFileDialog sfd = new SaveFileDialog())
                                {
                                    sfd.Title = "Lưu tệp PDF chuẩn 1.7";
                                    sfd.Filter = "Tệp PDF (*.pdf)|*.pdf";
                                    sfd.DefaultExt = "pdf";
                                    sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                                    if (sfd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        bool ok = GenOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                                        if (ok)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            Invalidate();
                                            MessageBox.Show("Đã lưu tệp PDF '" + sfd.FileName + "' thành công (Chuẩn PDF 1.7 tương thích 100% với Adobe Acrobat & Chrome)!", "GenOffice PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.O:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở tài liệu GENOFFICE Writer (Chuẩn Microsoft Word OpenXML .docx)";
                                    ofd.Filter = "Tài liệu Word (*.docx)|*.docx|Văn bản OpenOffice Writer (*.odt)|*.odt|Tệp Rich Text (*.rtf)|*.rtf|Trang Web HTML (*.html)|*.html|Tệp Văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        WriterView.LoadDocumentFile(ofd.FileName, Ribbon);
                                        string fName = System.IO.Path.GetFileName(ofd.FileName);
                                        Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                        Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                                        Invalidate();
                                        MessageBox.Show("Đã mở tài liệu '" + fName + "' thành công!", "GENOFFICE Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở bản trình chiếu PowerPoint (.pptx)";
                                    ofd.Filter = "Tệp Trình Chiếu PowerPoint (*.pptx;*.xml)|*.pptx;*.xml|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        SlidesView.Presentation = GenOffice.Format.PptxHandler.LoadPptx(ofd.FileName);
                                        SlidesView.ActiveSlideIndex = 0;
                                        SlidesView.SelectedShapeIndex = -1;
                                        SlidesView.CanvasScrollX = 0;
                                        SlidesView.CanvasScrollY = 0;
                                        SlidesView.ZoomLevel = 1.0f;
                                        string fName = System.IO.Path.GetFileName(ofd.FileName);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                                        Invalidate();
                                        MessageBox.Show("Đã mở bản trình chiếu '" + fName + "' thành công!", "GenOffice PowerPoint", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở Bảng tính GenOffice Calc (Chuẩn Microsoft Excel .xlsx / .csv)";
                                    ofd.Filter = "Tệp Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        bool success = GenOffice.Format.XlsxHandler.LoadXlsxOrCsv(ofd.FileName, CalcView);
                                        if (success)
                                        {
                                            string fName = System.IO.Path.GetFileName(ofd.FileName);
                                            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                            {
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = ofd.FileName;
                                                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                            }
                                            Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                                            Invalidate();
                                            MessageBox.Show("Đã mở bảng tính '" + fName + "' thành công!", "GenOffice Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                }
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                            {
                                using (OpenFileDialog ofd = new OpenFileDialog())
                                {
                                    ofd.Title = "Mở tệp PDF";
                                    ofd.Filter = "Tệp PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*";
                                    if (ofd.ShowDialog(this) == DialogResult.OK)
                                    {
                                        var loadedDoc = GenOffice.Format.PdfHandler.LoadPdf(ofd.FileName);
                                        PdfView.Document = loadedDoc;
                                        PdfView.ActivePageIndex = 0;
                                        string fName = System.IO.Path.GetFileName(ofd.FileName);
                                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                                        {
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = fName;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].PdfData = loadedDoc;
                                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                                        }
                                        Ribbon.SaveTabState(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                                        Invalidate();
                                        MessageBox.Show("Đã mở tệp PDF '" + fName + "' thành công!", "GenOffice PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            Invalidate();
                            return;
                        case Keys.N:
                            if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                                Ribbon.AddDocTab("Trình chiếu " + Ribbon.DocTabs.Count + ".pptx", OfficeAppMode.Slides, SlidesView, WriterView, CalcView, PdfView);
                            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                                Ribbon.AddDocTab("Bảng tính " + Ribbon.DocTabs.Count + ".xlsx", OfficeAppMode.Calc, SlidesView, WriterView, CalcView, PdfView);
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                                Ribbon.AddDocTab("Tài liệu PDF " + Ribbon.DocTabs.Count + ".pdf", OfficeAppMode.Pdf, SlidesView, WriterView, CalcView, PdfView);
                            else
                                Ribbon.AddDocTab("Văn bản " + Ribbon.DocTabs.Count + ".docx", OfficeAppMode.Writer, SlidesView, WriterView, CalcView, PdfView);
                            Invalidate();
                            return;
                        case Keys.W:
                            if (e.Shift && Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                WriterView.ToggleWordsUnderline();
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            Ribbon.CloseDocTab(Ribbon.ActiveDocIndex, SlidesView, WriterView, CalcView, PdfView);
                            Invalidate();
                            return;
                        case Keys.Add:
                        case Keys.Oemplus:
                            if (Ribbon.CurrentMode == OfficeAppMode.Writer && WriterView != null)
                            {
                                if (e.Shift) WriterView.ToggleSuperscript(); // Ctrl + Shift + +
                                else WriterView.ToggleSubscript();           // Ctrl + =
                                WriterView.UpdateRibbonState(Ribbon);
                                MarkActiveTabModified();
                                Invalidate();
                                return;
                            }
                            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.ZoomIn();
                                Invalidate();
                                return;
                            }
                            break;
                        case Keys.Subtract:
                        case Keys.OemMinus:
                            if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null)
                            {
                                PdfView.ZoomOut();
                                Invalidate();
                                return;
                            }
                            break;
                    }
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Pdf && PdfView != null && PdfView.Document != null)
                {
                    if (e.KeyCode == Keys.PageDown)
                    {
                        if (PdfView.ActivePageIndex < PdfView.Document.Pages.Count - 1)
                        {
                            PdfView.ActivePageIndex++;
                            PdfView.ScrollToPage(PdfView.ActivePageIndex, ClientSize.Width, ClientSize.Height);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (e.KeyCode == Keys.PageUp)
                    {
                        if (PdfView.ActivePageIndex > 0)
                        {
                            PdfView.ActivePageIndex--;
                            PdfView.ScrollToPage(PdfView.ActivePageIndex, ClientSize.Width, ClientSize.Height);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (e.KeyCode == Keys.Home && !e.Control)
                    {
                        PdfView.ActivePageIndex = 0;
                        PdfView.ScrollToPage(0, ClientSize.Width, ClientSize.Height);
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                    else if (e.KeyCode == Keys.End && !e.Control)
                    {
                        PdfView.ActivePageIndex = Math.Max(0, PdfView.Document.Pages.Count - 1);
                        PdfView.ScrollToPage(PdfView.ActivePageIndex, ClientSize.Width, ClientSize.Height);
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        PdfView.HandleKeyDown(e.KeyCode);
                        if (PdfView.Document.IsModified) MarkActiveTabModified();
                        Invalidate();
                        return;
                    }
                }

                if (e.KeyCode == Keys.F5 && Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    if (e.Alt)
                    {
                        StartSlideShow(false, true); // Alt+F5 -> Presenter View
                    }
                    else
                    {
                        StartSlideShow(e.Shift ? false : true); // F5 -> from beginning, Shift+F5 -> from current
                    }
                    e.Handled = true;
                    return;
                }

                if (Ribbon.CurrentMode == OfficeAppMode.Writer)
                {
                    // 1. Khi đang chọn Biểu đồ (Chart)
                    if (WriterView.ActiveChart != null)
                    {
                        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                        {
                            WriterView.DeleteActiveChart();
                            MarkActiveTabModified();
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        int moveStep = e.Shift ? 10 : 2;
                        if (e.KeyCode == Keys.Left) { WriterView.ActiveChart.PageX -= moveStep; Invalidate(); e.Handled = true; return; }
                        if (e.KeyCode == Keys.Right) { WriterView.ActiveChart.PageX += moveStep; Invalidate(); e.Handled = true; return; }
                        if (e.KeyCode == Keys.Up) { WriterView.ActiveChart.PageY -= moveStep; Invalidate(); e.Handled = true; return; }
                        if (e.KeyCode == Keys.Down) { WriterView.ActiveChart.PageY += moveStep; Invalidate(); e.Handled = true; return; }

                        e.Handled = true;
                        return;
                    }

                    // 2. Khi đang chọn Hình khối hoặc Hình ảnh (Shape / Image)
                    if (WriterView.ActiveShape != null)
                    {
                        var sh = WriterView.ActiveShape;

                        // Trường hợp chọn hình/ảnh ngoài (chưa ở chế độ gõ văn bản bên trong shape)
                        if (sh.ShapeType == "Image" || !sh.IsEditingText)
                        {
                            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                            {
                                WriterView.DeleteActiveShape();
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            int moveStep = e.Shift ? 10 : 2;
                            if (e.KeyCode == Keys.Left) { sh.PageX -= moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Right) { sh.PageX += moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Up) { sh.PageY -= moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Down) { sh.PageY += moveStep; Invalidate(); e.Handled = true; return; }

                            if (sh.ShapeType != "Image" && (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter))
                            {
                                sh.IsEditingText = true;
                                sh.TextCursorIndex = sh.TextContent != null ? sh.TextContent.Length : 0;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            e.Handled = true;
                            return;
                        }

                        // Trường hợp đang gõ văn bản bên trong Shape
                        if (sh.TextContent == null) sh.TextContent = "";
                        int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                        if (e.KeyCode == Keys.Left)
                        {
                            sh.TextCursorIndex = Math.Max(0, cur - 1);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Right)
                        {
                            sh.TextCursorIndex = Math.Min(sh.TextContent.Length, cur + 1);
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Home)
                        {
                            sh.TextCursorIndex = 0;
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.End)
                        {
                            sh.TextCursorIndex = sh.TextContent.Length;
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyCode == Keys.Delete)
                        {
                            if (cur < sh.TextContent.Length)
                            {
                                sh.TextContent = sh.TextContent.Substring(0, cur) + sh.TextContent.Substring(cur + 1);
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            else if (sh.TextContent.Length == 0)
                            {
                                WriterView.DeleteActiveShape();
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                        }
                        if (e.KeyCode == Keys.Escape)
                        {
                            sh.IsEditingText = false;
                            Invalidate();
                            e.Handled = true;
                            return;
                        }
                        e.Handled = true;
                        return;
                    }

                    WriterView.CursorVisible = true; // Always show cursor on keydown
                    if (e.KeyCode == Keys.PageDown) WriterView.ScrollY = Math.Min(WriterView.GetMaxScrollY(ClientSize.Height, Ribbon), WriterView.ScrollY + 200);
                    else if (e.KeyCode == Keys.PageUp) WriterView.ScrollY = Math.Max(0, WriterView.ScrollY - 200);
                    WriterView.HandleKeyDown(e.KeyCode, Ribbon);
                    // Scroll to keep cursor visible
                    e.Handled = true;
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
                {
                    if (CalcView.SelectedShape != null)
                    {
                        var sh = CalcView.SelectedShape;

                        // Nếu đang gõ văn bản bên trong Shape
                        if (sh.IsEditingText && sh.ShapeType != "Image")
                        {
                            if (sh.TextContent == null) sh.TextContent = "";
                            int cur = Math.Max(0, Math.Min(sh.TextCursorIndex, sh.TextContent.Length));

                            if (e.KeyCode == Keys.Left)
                            {
                                sh.TextCursorIndex = Math.Max(0, cur - 1);
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.Right)
                            {
                                sh.TextCursorIndex = Math.Min(sh.TextContent.Length, cur + 1);
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.Home)
                            {
                                sh.TextCursorIndex = 0;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.End)
                            {
                                sh.TextCursorIndex = sh.TextContent.Length;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (e.KeyCode == Keys.Delete)
                            {
                                if (cur < sh.TextContent.Length)
                                {
                                    sh.TextContent = sh.TextContent.Substring(0, cur) + sh.TextContent.Substring(cur + 1);
                                    MarkActiveTabModified();
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                                else if (sh.TextContent.Length == 0)
                                {
                                    CalcView.Shapes.Remove(sh);
                                    CalcView.SelectedShape = null;
                                    MarkActiveTabModified();
                                    Invalidate();
                                    e.Handled = true;
                                    return;
                                }
                            }
                            if (e.KeyCode == Keys.Escape)
                            {
                                sh.IsEditingText = false;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            // Đang chọn Shape nhưng chưa ở chế độ gõ
                            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                            {
                                CalcView.Shapes.Remove(CalcView.SelectedShape);
                                CalcView.SelectedShape = null;
                                MarkActiveTabModified();
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            if (sh.ShapeType != "Image" && (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter))
                            {
                                sh.IsEditingText = true;
                                sh.IsSelected = true;
                                sh.TextCursorIndex = sh.TextContent != null ? sh.TextContent.Length : 0;
                                Invalidate();
                                e.Handled = true;
                                return;
                            }
                            int moveStep = e.Shift ? 10 : 2;
                            if (e.KeyCode == Keys.Left) { CalcView.SelectedShape.PageX = Math.Max(0, CalcView.SelectedShape.PageX - moveStep); Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Right) { CalcView.SelectedShape.PageX += moveStep; Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Up) { CalcView.SelectedShape.PageY = Math.Max(0, CalcView.SelectedShape.PageY - moveStep); Invalidate(); e.Handled = true; return; }
                            if (e.KeyCode == Keys.Down) { CalcView.SelectedShape.PageY += moveStep; Invalidate(); e.Handled = true; return; }
                            e.Handled = true;
                            return;
                        }
                    }
                    CalcView.HandleKeyDown(e.KeyCode);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
                {
                    SlidesView.HandleKeyDown(e.KeyCode);
                }
                else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
                {
                    PdfView.HandleKeyDown(e.KeyCode);
                    MarkActiveTabModified();
                }
            }
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            // Allow Up/Down/Left/Right/Home/End/Enter/Escape/Tab to be processed by OnKeyDown in Writer, Slides, Calc, and Pdf mode
            if (Ribbon != null && (Ribbon.CurrentMode == OfficeAppMode.Writer || Ribbon.CurrentMode == OfficeAppMode.Slides || Ribbon.CurrentMode == OfficeAppMode.Calc || Ribbon.CurrentMode == OfficeAppMode.Pdf))
            {
                switch (keyData)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.Enter:
                    case Keys.Tab:
                    case Keys.Escape:
                        return true;
                }
            }
            return base.IsInputKey(keyData);
        }

        public void ShowShortcutsHelpDialog()
        {
            try
            {
                using (var helpForm = new ShortcutsHelpForm())
                {
                    helpForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở bảng tra cứu phím tắt: " + ex.Message, "Trợ giúp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void PerformSaveAs()
        {
            if (Ribbon.CurrentMode == OfficeAppMode.Writer)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu Tài liệu GENOFFICE Writer với tên khác (Save As)";
                    sfd.Filter = "Tài liệu Word (*.docx)|*.docx|Văn bản OpenOffice (*.odt)|*.odt|Tệp Rich Text (*.rtf)|*.rtf|Trang Web HTML (*.html)|*.html|Tệp Văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        WriterView.SaveDocumentFile(sfd.FileName, Ribbon);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        Invalidate();
                        MessageBox.Show("Đã lưu tài liệu '" + sfd.FileName + "' thành công!", "GenOffice Writer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Slides)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Tệp Trình Chiếu PowerPoint (*.pptx)|*.pptx|Tệp XML Presentation (*.xml)|*.xml";
                    sfd.Title = "Lưu Bản Trình Chiếu với tên khác (Save As)";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        GenOffice.Format.PptxHandler.SavePptx(sfd.FileName, SlidesView.Presentation);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        Invalidate();
                        MessageBox.Show("Đã lưu bản trình chiếu thành công!", "GenOffice PowerPoint", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Calc)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu Bảng tính với tên khác (Save As)";
                    sfd.Filter = "Tệp Excel (*.xlsx)|*.xlsx|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        GenOffice.Format.XlsxHandler.SaveXlsxOrCsv(sfd.FileName, CalcView);
                        if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].FilePath = sfd.FileName;
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                        }
                        Invalidate();
                        MessageBox.Show("Đã lưu bảng tính thành công!", "GenOffice Calc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (Ribbon.CurrentMode == OfficeAppMode.Pdf)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Lưu tệp PDF với tên khác (Save As)";
                    sfd.Filter = "Tệp PDF (*.pdf)|*.pdf";
                    sfd.DefaultExt = "pdf";
                    if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
                    {
                        sfd.FileName = Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title.Replace(" *", "");
                    }
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        bool ok = GenOffice.Format.PdfHandler.SavePdf(PdfView.Document, sfd.FileName);
                        if (ok)
                        {
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].Title = System.IO.Path.GetFileName(sfd.FileName);
                            Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = false;
                            Invalidate();
                            MessageBox.Show("Đã lưu tệp PDF thành công!", "GenOffice PDF Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        public void PerformPrint()
        {
            try
            {
                using (PrintDialog pd = new PrintDialog())
                {
                    pd.UseEXDialog = true;
                    if (pd.ShowDialog(this) == DialogResult.OK)
                    {
                        MessageBox.Show("Đã gửi lệnh in tài liệu đến máy in: " + pd.PrinterSettings.PrinterName, "In Ấn - GenOffice Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động trình quản lý in ấn: " + ex.Message, "In ấn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MarkActiveTabModified()
        {
            if (Ribbon.ActiveDocIndex >= 0 && Ribbon.ActiveDocIndex < Ribbon.DocTabs.Count)
            {
                Ribbon.DocTabs[Ribbon.ActiveDocIndex].IsModified = true;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }
}

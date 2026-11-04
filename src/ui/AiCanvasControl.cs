using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bảng điều khiển Canvas Tương tác (Interactive Canvas Panel) theo phong cách Gemini Canvas.
    /// KẾT XUẤT 100% BẰNG DIRECT2D / DIRECTWRITE (KHÔNG DÙNG GDI/GDI+ CHO CANVAS)
    /// Hỗ trợ làm bài trắc nghiệm, đúng/sai, điền khuyết, tự luận trực tiếp với chấm điểm tức thì.
    /// Tích hợp màn hình báo cáo kết quả đồ thị bán nguyệt chuẩn Gemini (không dùng MessageBox popup).
    /// </summary>
    public class AiCanvasControl : Control
    {
        public AiCanvasQuizData QuizData { get; private set; }
        public int CurrentIndex { get; set; } = 0;
        public int CorrectCount { get; private set; } = 0;
        public int WrongCount { get; private set; } = 0;
        public bool IsResultScreen { get; set; } = false;

        public event Action CloseRequested;
        public event Action<AiCanvasQuizData> ExportToWriterRequested;

        // Bounding boxes for hit-testing
        private Rectangle _btnCloseRect = Rectangle.Empty;
        private Rectangle _btnPrevRect = Rectangle.Empty;
        private Rectangle _btnNextRect = Rectangle.Empty;
        private List<Rectangle> _optionRects = new List<Rectangle>();
        private Rectangle _btnCheckFillBlankRect = Rectangle.Empty;
        private Rectangle _btnShowEssayAnsRect = Rectangle.Empty;

        // Result screen buttons
        private Rectangle _btnReviewRect = Rectangle.Empty;
        private Rectangle _btnRetakeRect = Rectangle.Empty;

        // Hover tracking
        private Point _mousePos = Point.Empty;
        private bool _isCloseHover = false;
        private bool _isPrevHover = false;
        private bool _isNextHover = false;
        private int _hoveredOptionIndex = -1;
        private bool _isReviewHover = false;
        private bool _isRetakeHover = false;
        private int _scrollY = 0;
        private int _maxScroll = 0;

        // D2D Fonts
        private readonly Font _titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        private readonly Font _progFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private readonly Font _badgeFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private readonly Font _qNumFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Font _typeFont = new Font("Segoe UI", 7.8f, FontStyle.Bold);
        private readonly Font _qTextFont = new Font("Segoe UI", 9.8f, FontStyle.Regular);
        private readonly Font _optFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private readonly Font _expFont = new Font("Segoe UI", 8.8f, FontStyle.Regular);
        private readonly Font _btnFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);

        // Result Screen Fonts
        private readonly Font _resHeaderFont = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        private readonly Font _gaugeScoreFont = new Font("Segoe UI", 24f, FontStyle.Bold);
        private readonly Font _gaugeLabelFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private readonly Font _resSubFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        private readonly Font _resHeadingFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        private readonly Font _resSectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Font _resBodyFont = new Font("Segoe UI", 9.3f, FontStyle.Regular);

        public AiCanvasControl()
        {
            this.DoubleBuffered = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.White;
        }

        public void LoadQuiz(AiCanvasQuizData quiz)
        {
            QuizData = quiz ?? new AiCanvasQuizData();
            CurrentIndex = 0;
            IsResultScreen = false;
            _scrollY = 0;
            _maxScroll = 0;
            RecalculateScores();
            this.Invalidate();
        }

        private void RecalculateScores()
        {
            CorrectCount = 0;
            WrongCount = 0;
            if (QuizData == null || QuizData.Questions == null) return;
            foreach (var q in QuizData.Questions)
            {
                if (q.IsAnswered)
                {
                    if (q.IsCorrect) CorrectCount++;
                    else WrongCount++;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (this.Width <= 0 || this.Height <= 0) return;

            // KẾT XUẤT 100% DIRECT2D / DIRECTWRITE (GPU / Microsoft WARP)
            using (var frame = AiGpuPaint.Begin(this, e))
            {
                var g = frame.Graphics;
                if (IsResultScreen)
                {
                    RenderResultScreenDirect2D(g);
                }
                else
                {
                    RenderCanvasDirect2D(g);
                }
            }
        }

        private void RenderCanvasDirect2D(D2DGraphics g)
        {
            int w = this.Width;
            int h = this.Height;

            // 1. Nền Canvas trắng tinh khiết & Viền trái phân cách
            using (SolidBrush bgBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
                g.DrawLine(borderPen, 0, 0, 0, h);
            }

            if (QuizData == null || QuizData.Questions == null || QuizData.Questions.Count == 0)
            {
                using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString("Chưa có nội dung đề bài trên Canvas.\nHãy gõ /canvas [yêu cầu] trong khung chat để tạo đề thi tương tác.", _qTextFont, emptyBrush, new RectangleF(30, 60, w - 60, 100));
                }
                return;
            }

            int total = QuizData.Questions.Count;
            if (CurrentIndex < 0) CurrentIndex = 0;
            if (CurrentIndex >= total) CurrentIndex = total - 1;

            AiCanvasQuestion currentQ = QuizData.Questions[CurrentIndex];

            // ==========================================
            // 2. HEADER: Tiêu đề + Nút Đóng [ ✕ ]
            // ==========================================
            int headerH = 46;
            _btnCloseRect = new Rectangle(w - 38, 11, 26, 26);

            // Nút đóng
            if (_isCloseHover)
            {
                using (SolidBrush closeBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillRoundedRectangle(closeBg, _btnCloseRect, 6f);
                }
            }
            using (Pen closePen = new Pen(Color.FromArgb(100, 116, 139), 1.6f))
            {
                closePen.StartCap = LineCap.Round;
                closePen.EndCap = LineCap.Round;
                int cx = _btnCloseRect.X + _btnCloseRect.Width / 2;
                int cy = _btnCloseRect.Y + _btnCloseRect.Height / 2;
                g.DrawLine(closePen, cx - 4, cy - 4, cx + 4, cy + 4);
                g.DrawLine(closePen, cx + 4, cy - 4, cx - 4, cy + 4);
            }

            // Tiêu đề đề bài
            string titleStr = QuizData.Title ?? "Đề ôn tập tương tác";
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                RectangleF titleRect = new RectangleF(24, 13, w - 80, 26);
                g.DrawString(titleStr, _titleFont, titleBrush, titleRect);
            }

            // ==========================================
            // 3. PROGRESS BAR & SCORE BADGES (1/20, [🔴 0 Sai], [🟢 0 Đúng])
            // ==========================================
            int subY = headerH + 6;
            int barX = 24;
            int rightBadgeW = 150;
            int barW = Math.Max(60, w - barX - rightBadgeW - 70);
            int barH = 5;

            // Rãnh trượt mờ nhạt
            using (SolidBrush trackB = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (SolidBrush fillB = new SolidBrush(Color.FromArgb(37, 99, 235))) // Accent Blue
            {
                g.FillRoundedRectangle(trackB, new RectangleF(barX, subY + 9, barW, barH), 2.5f);

                float progressRatio = (float)(CurrentIndex + 1) / total;
                float fillW = Math.Max(8f, barW * progressRatio);
                g.FillRoundedRectangle(fillB, new RectangleF(barX, subY + 9, fillW, barH), 2.5f);
            }

            // Chỉ số câu "1/20"
            string progText = string.Format("{0}/{1}", CurrentIndex + 1, total);
            using (SolidBrush progBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.DrawString(progText, _progFont, progBrush, barX + barW + 10, subY + 3);
            }

            // Huy hiệu [ 🔴 WrongCount ] & [ 🟢 CorrectCount ]
            int badgeY = subY;
            int bRedX = w - 146;
            Rectangle redRect = new Rectangle(bRedX, badgeY, 56, 24);
            using (SolidBrush bRedBg = new SolidBrush(Color.FromArgb(254, 226, 226)))
            using (Pen bRedPen = new Pen(Color.FromArgb(254, 202, 202), 1f))
            using (SolidBrush bRedTxt = new SolidBrush(Color.FromArgb(220, 38, 38)))
            {
                g.FillRoundedRectangle(bRedBg, redRect, 12f);
                g.DrawRoundedRectangle(bRedPen, redRect, 12f);
                g.DrawString(string.Format("✕ {0}", WrongCount), _badgeFont, bRedTxt, redRect.X + 11, redRect.Y + 4);
            }

            int bGreenX = w - 82;
            Rectangle greenRect = new Rectangle(bGreenX, badgeY, 56, 24);
            using (SolidBrush bGrnBg = new SolidBrush(Color.FromArgb(220, 252, 231)))
            using (Pen bGrnPen = new Pen(Color.FromArgb(187, 247, 208), 1f))
            using (SolidBrush bGrnTxt = new SolidBrush(Color.FromArgb(22, 163, 74)))
            {
                g.FillRoundedRectangle(bGrnBg, greenRect, 12f);
                g.DrawRoundedRectangle(bGrnPen, greenRect, 12f);
                g.DrawString(string.Format("✓ {0}", CorrectCount), _badgeFont, bGrnTxt, greenRect.X + 11, greenRect.Y + 4);
            }

            // ==========================================
            // 4. NỘI DUNG CÂU HỎI
            // ==========================================
            int curY = subY + 34;

            // Nhãn số thứ tự câu hỏi + Badge phân loại câu
            string qNumText = string.Format("Câu hỏi {0}", currentQ.Number > 0 ? currentQ.Number : (CurrentIndex + 1));
            using (SolidBrush qNumBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(qNumText, _qNumFont, qNumBrush, 24, curY);
            }

            string typeBadge = currentQ.Type == CanvasQuestionType.TrueFalse ? "Đúng / Sai" :
                               currentQ.Type == CanvasQuestionType.FillBlank ? "Điền khuyết" :
                               currentQ.Type == CanvasQuestionType.Essay ? "Tự luận" : "Trắc nghiệm 4 lựa chọn";

            int typeBadgeW = 130;
            Rectangle typeRect = new Rectangle(120, curY - 1, typeBadgeW, 20);
            using (SolidBrush typeBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen typePen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (SolidBrush typeTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.FillRoundedRectangle(typeBg, typeRect, 4f);
                g.DrawRoundedRectangle(typePen, typeRect, 4f);
                g.DrawString(typeBadge, _typeFont, typeTxt, typeRect.X + 8, typeRect.Y + 2);
            }

            curY += 28;

            // Nội dung câu hỏi (Multi-line word wrap)
            using (SolidBrush qTextBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                SizeF qTextSz = g.MeasureString(currentQ.QuestionText ?? "", _qTextFont, w - 48);
                float qH = Math.Max(28f, (float)Math.Ceiling(qTextSz.Height) + 6);
                RectangleF qTextRect = new RectangleF(24, curY, w - 48, qH);
                g.DrawString(currentQ.QuestionText ?? "", _qTextFont, qTextBrush, qTextRect);
                curY += (int)qH + 14;
            }

            // ==========================================
            // 5. CÁC LỰA CHỌN TRẢ LỜI (A, B, C, D) HOẶC TỰ LUẬN
            // ==========================================
            _optionRects.Clear();

            if (currentQ.Type == CanvasQuestionType.MultipleChoice || currentQ.Type == CanvasQuestionType.TrueFalse)
            {
                int optGap = 8;
                int optW = w - 48;

                for (int i = 0; i < currentQ.Options.Count; i++)
                {
                    string rawOpt = currentQ.Options[i] ?? "";
                    char letter = (char)('A' + i);
                    string optDisplay = rawOpt;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(optDisplay, @"^[A-Da-d]\."))
                    {
                        optDisplay = string.Format("{0}.  {1}", letter, optDisplay);
                    }
                    else
                    {
                        optDisplay = optDisplay.Insert(2, " ");
                    }

                    float availTextW = optW - 56;
                    SizeF optTextSz = g.MeasureString(optDisplay, _optFont, (int)availTextW);
                    int optH = Math.Max(42, (int)Math.Ceiling(optTextSz.Height) + 18);

                    Rectangle optRect = new Rectangle(24, curY, optW, optH);
                    _optionRects.Add(optRect);

                    bool isHover = (i == _hoveredOptionIndex) && !currentQ.IsAnswered;
                    bool isSelected = (currentQ.SelectedOptionIndex == i);
                    bool isCorrectOpt = (i == currentQ.CorrectOptionIndex);

                    Color cardBg = Color.FromArgb(248, 250, 252);
                    Color borderCol = Color.FromArgb(226, 232, 240);
                    Color textCol = Color.FromArgb(30, 41, 59);

                    if (currentQ.IsAnswered)
                    {
                        if (isSelected)
                        {
                            if (isCorrectOpt)
                            {
                                cardBg = Color.FromArgb(220, 252, 231);
                                borderCol = Color.FromArgb(22, 163, 74);
                                textCol = Color.FromArgb(20, 83, 45);
                            }
                            else
                            {
                                cardBg = Color.FromArgb(254, 226, 226);
                                borderCol = Color.FromArgb(220, 38, 38);
                                textCol = Color.FromArgb(127, 29, 29);
                            }
                        }
                        else if (isCorrectOpt)
                        {
                            cardBg = Color.FromArgb(240, 253, 244);
                            borderCol = Color.FromArgb(74, 222, 128);
                            textCol = Color.FromArgb(20, 83, 45);
                        }
                    }
                    else if (isHover)
                    {
                        cardBg = Color.FromArgb(238, 242, 255);
                        borderCol = Color.FromArgb(99, 102, 241);
                    }

                    using (SolidBrush cBg = new SolidBrush(cardBg))
                    using (Pen cPen = new Pen(borderCol, (isSelected || (currentQ.IsAnswered && isCorrectOpt)) ? 1.5f : 1f))
                    using (SolidBrush cTxt = new SolidBrush(textCol))
                    {
                        g.FillRoundedRectangle(cBg, optRect, 8f);
                        g.DrawRoundedRectangle(cPen, optRect, 8f);

                        RectangleF optTextRect = new RectangleF(optRect.X + 16, optRect.Y + 9, availTextW, optH - 16);
                        g.DrawString(optDisplay, _optFont, cTxt, optTextRect);

                        if (currentQ.IsAnswered)
                        {
                            int iconY = optRect.Y + (optH - 18) / 2;
                            if (isSelected)
                            {
                                string statIcon = isCorrectOpt ? "✓" : "✕";
                                Color statCol = isCorrectOpt ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
                                using (SolidBrush statBrush = new SolidBrush(statCol))
                                using (Font statFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                                {
                                    g.DrawString(statIcon, statFont, statBrush, optRect.Right - 32, iconY);
                                }
                            }
                            else if (isCorrectOpt)
                            {
                                using (SolidBrush statBrush = new SolidBrush(Color.FromArgb(22, 163, 74)))
                                using (Font statFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                                {
                                    g.DrawString("✓", statFont, statBrush, optRect.Right - 32, iconY);
                                }
                            }
                        }
                    }

                    curY += optH + optGap;
                }
            }
            else if (currentQ.Type == CanvasQuestionType.FillBlank)
            {
                Rectangle boxRect = new Rectangle(24, curY, w - 48, 48);
                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen bPen = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                using (SolidBrush bTxt = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    g.FillRoundedRectangle(bBg, boxRect, 8f);
                    g.DrawRoundedRectangle(bPen, boxRect, 8f);
                    string fillHint = currentQ.IsAnswered ? ("Đáp án chuẩn: " + currentQ.CorrectAnswerText) : "Hãy suy nghĩ đáp án để điền vào chỗ trống...";
                    g.DrawString(fillHint, _optFont, bTxt, boxRect.X + 14, boxRect.Y + 14);
                }

                curY += 56;

                _btnCheckFillBlankRect = new Rectangle(24, curY, 140, 32);
                using (SolidBrush chkBg = new SolidBrush(Color.FromArgb(37, 99, 235)))
                using (SolidBrush chkTxt = new SolidBrush(Color.White))
                {
                    g.FillRoundedRectangle(chkBg, _btnCheckFillBlankRect, 6f);
                    g.DrawString("Kiểm tra đáp án", _btnFont, chkTxt, _btnCheckFillBlankRect.X + 18, _btnCheckFillBlankRect.Y + 7);
                }

                curY += 40;
            }
            else if (currentQ.Type == CanvasQuestionType.Essay)
            {
                Rectangle essayBox = new Rectangle(24, curY, w - 48, 90);
                using (SolidBrush bBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen bPen = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
                using (SolidBrush bTxt = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.FillRoundedRectangle(bBg, essayBox, 8f);
                    g.DrawRoundedRectangle(bPen, essayBox, 8f);
                    g.DrawString("Khu vực viết câu trả lời / dàn ý cho bài thi tự luận...", _optFont, bTxt, essayBox.X + 14, essayBox.Y + 12);
                }

                curY += 98;

                _btnShowEssayAnsRect = new Rectangle(24, curY, 180, 32);
                using (SolidBrush chkBg = new SolidBrush(Color.FromArgb(79, 70, 229)))
                using (SolidBrush chkTxt = new SolidBrush(Color.White))
                {
                    g.FillRoundedRectangle(chkBg, _btnShowEssayAnsRect, 6f);
                    g.DrawString("Xem gợi ý & biểu điểm", _btnFont, chkTxt, _btnShowEssayAnsRect.X + 14, _btnShowEssayAnsRect.Y + 7);
                }

                curY += 40;
            }

            // ==========================================
            // 6. KHUNG GIẢI THÍCH CHI TIẾT
            // ==========================================
            if (currentQ.IsAnswered || currentQ.ShowExplanation)
            {
                curY += 6;
                int expH = 68;
                Rectangle expRect = new Rectangle(24, curY, w - 48, expH);
                using (SolidBrush expBg = new SolidBrush(Color.FromArgb(239, 246, 255)))
                using (Pen expPen = new Pen(Color.FromArgb(191, 219, 254), 1f))
                using (SolidBrush expTitleBrush = new SolidBrush(Color.FromArgb(30, 58, 138)))
                using (SolidBrush expTextBrush = new SolidBrush(Color.FromArgb(30, 64, 175)))
                {
                    g.FillRoundedRectangle(expBg, expRect, 8f);
                    g.DrawRoundedRectangle(expPen, expRect, 8f);

                    g.DrawString("💡 Lời giải chi tiết:", _badgeFont, expTitleBrush, expRect.X + 12, expRect.Y + 8);
                    string expStr = !string.IsNullOrEmpty(currentQ.Explanation) ? currentQ.Explanation :
                                    (!string.IsNullOrEmpty(currentQ.CorrectAnswerText) ? ("Gợi ý: " + currentQ.CorrectAnswerText) : "Đáp án chính xác dựa theo kiến thức của bài học.");
                    g.DrawString(expStr, _expFont, expTextBrush, new RectangleF(expRect.X + 12, expRect.Y + 28, expRect.Width - 24, expRect.Height - 32));
                }
            }

            // ==========================================
            // 7. FOOTER NAVIGATION: [ ‹ Quay lại ]  [ Tiếp theo › ]
            // ==========================================
            int footerY = h - 52;
            using (Pen fBorder = new Pen(Color.FromArgb(241, 245, 249), 1f))
            {
                g.DrawLine(fBorder, 24, footerY - 8, w - 24, footerY - 8);
            }

            int btnH = 34;
            bool isLast = (CurrentIndex == total - 1);

            // Nút Tiếp theo (hoặc Hoàn thành) nằm sát lề phải cách 24px
            int nextW = isLast ? 136 : 105;
            int nextX = w - 24 - nextW;
            _btnNextRect = new Rectangle(nextX, footerY, nextW, btnH);

            // Nút Quay lại nằm bên trái Nút Tiếp theo, cách 12px
            int prevW = 105;
            int prevX = nextX - 12 - prevW;
            _btnPrevRect = new Rectangle(prevX, footerY, prevW, btnH);

            // Vẽ Nút Quay lại
            bool prevEnabled = (CurrentIndex > 0);
            Color prevBg = prevEnabled ? (_isPrevHover ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)) : Color.FromArgb(248, 250, 252);
            Color prevTxt = prevEnabled ? Color.FromArgb(51, 65, 85) : Color.FromArgb(148, 163, 184);

            using (SolidBrush pBg = new SolidBrush(prevBg))
            using (Pen pBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (SolidBrush pTxt = new SolidBrush(prevTxt))
            {
                g.FillRoundedRectangle(pBg, _btnPrevRect, 17f);
                g.DrawRoundedRectangle(pBorder, _btnPrevRect, 17f);
                string prevText = "‹ Quay lại";
                SizeF sPrev = g.MeasureString(prevText, _btnFont);
                g.DrawString(prevText, _btnFont, pTxt, _btnPrevRect.X + (_btnPrevRect.Width - sPrev.Width) / 2f, _btnPrevRect.Y + (_btnPrevRect.Height - sPrev.Height) / 2f);
            }

            // Vẽ Nút Tiếp theo (hoặc Hoàn thành)
            Color nextBg = _isNextHover ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235);
            using (SolidBrush nBg = new SolidBrush(nextBg))
            using (SolidBrush nTxt = new SolidBrush(Color.White))
            {
                g.FillRoundedRectangle(nBg, _btnNextRect, 17f);
                string nextText = isLast ? "Hoàn thành 🎉" : "Tiếp theo ›";
                SizeF sNext = g.MeasureString(nextText, _btnFont);
                g.DrawString(nextText, _btnFont, nTxt, _btnNextRect.X + (_btnNextRect.Width - sNext.Width) / 2f, _btnNextRect.Y + (_btnNextRect.Height - sNext.Height) / 2f);
            }
        }

        /// <summary>
        /// KẾT XUẤT MÀN HÌNH KẾT QUẢ ĐỒ THỊ CHUẨN GEMINI CANVAS (100% DIRECT2D)
        /// Không dùng MessageBox popup! Có đồ thị vòng cung bán nguyệt, điểm số và phân tích điểm mạnh.
        /// </summary>
        private void RenderResultScreenDirect2D(D2DGraphics g)
        {
            int w = this.Width;
            int h = this.Height;

            // 1. Nền trắng Canvas & Viền phân cách trái
            using (SolidBrush bgBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
                g.DrawLine(borderPen, 0, 0, 0, h);
            }

            // 2. Header: Tiêu đề đề thi + Nút Đóng
            _btnCloseRect = new Rectangle(w - 38, 11, 26, 26);
            if (_isCloseHover)
            {
                using (SolidBrush closeBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    g.FillRoundedRectangle(closeBg, _btnCloseRect, 6f);
                }
            }
            using (Pen closePen = new Pen(Color.FromArgb(100, 116, 139), 1.6f))
            {
                closePen.StartCap = LineCap.Round;
                closePen.EndCap = LineCap.Round;
                int cx = _btnCloseRect.X + _btnCloseRect.Width / 2;
                int cy = _btnCloseRect.Y + _btnCloseRect.Height / 2;
                g.DrawLine(closePen, cx - 4, cy - 4, cx + 4, cy + 4);
                g.DrawLine(closePen, cx + 4, cy - 4, cx - 4, cy + 4);
            }

            string titleStr = QuizData != null ? (QuizData.Title ?? "Đề ôn tập") : "Kết quả bài làm";
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(titleStr, _titleFont, titleBrush, new RectangleF(24, 13, w - 80, 26));
            }

            // 3. Tiêu đề "Điểm" căn giữa
            float centerX = w / 2f;
            using (SolidBrush headBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                string headerLabel = "Điểm";
                SizeF sHead = g.MeasureString(headerLabel, _resHeaderFont);
                g.DrawString(headerLabel, _resHeaderFont, headBrush, centerX - sHead.Width / 2f, 54);
            }

            // 4. Đồ thị vòng cung bán nguyệt (Semi-circular Gauge Chart)
            float gaugeRadius = 68f;
            float gaugeCenterY = 162f;
            RectangleF gaugeRect = new RectangleF(centerX - gaugeRadius, gaugeCenterY - gaugeRadius, gaugeRadius * 2f, gaugeRadius * 2f);

            int total = (QuizData != null && QuizData.Questions != null) ? QuizData.Questions.Count : 1;
            float correctRatio = Math.Max(0f, Math.Min(1f, (float)CorrectCount / total));
            float sweepAngle = correctRatio * 180f;

            // Vòng cung nền màu hồng phấn (chưa chính xác / còn lại)
            using (Pen pinkArcPen = new Pen(Color.FromArgb(254, 205, 211), 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(pinkArcPen, gaugeRect, 180f, 180f);
            }

            // Vòng cung xanh dương rực rỡ (tỷ lệ chính xác)
            if (sweepAngle > 0.5f)
            {
                using (Pen blueArcPen = new Pen(Color.FromArgb(37, 99, 235), 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(blueArcPen, gaugeRect, 180f, sweepAngle);
                }
            }

            // Điểm số bên trong vòng cung: e.g. "16/20"
            string scoreNumStr = string.Format("{0}/{1}", CorrectCount, total);
            using (SolidBrush scoreBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                SizeF sNum = g.MeasureString(scoreNumStr, _gaugeScoreFont);
                g.DrawString(scoreNumStr, _gaugeScoreFont, scoreBrush, centerX - sNum.Width / 2f, gaugeCenterY - 40);
            }

            // Nhãn "Chính xác"
            using (SolidBrush accBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                string accStr = "Chính xác";
                SizeF sAcc = g.MeasureString(accStr, _gaugeLabelFont);
                g.DrawString(accStr, _gaugeLabelFont, accBrush, centerX - sAcc.Width / 2f, gaugeCenterY + 4);
            }

            // 5. Chú thích bên dưới: "Đã bỏ qua 0 câu hỏi • X câu trả lời chưa chính xác"
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                string subStr = string.Format("Đã hoàn thành {0} câu hỏi  •  {1} câu trả lời chưa chính xác", total, WrongCount);
                SizeF sSub = g.MeasureString(subStr, _resSubFont);
                g.DrawString(subStr, _resSubFont, subBrush, centerX - sSub.Width / 2f, gaugeCenterY + 42);
            }

            // 6. Tiêu đề đánh giá năng lực
            int evalY = (int)gaugeCenterY + 76;
            string evalHeading = (correctRatio >= 0.85f) ? "Xuất sắc! Còn một vài điểm cần xem lại." :
                                 (correctRatio >= 0.65f) ? "Khá tốt! Bạn đã nắm vững phần lớn kiến thức." :
                                 "Cần cố gắng thêm! Hãy xem lại các câu chưa chính xác.";

            using (SolidBrush evalBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(evalHeading, _resHeadingFont, evalBrush, 32, evalY);
            }

            // 7. Nhận xét phân tích "Điểm mạnh"
            evalY += 38;
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Điểm mạnh", _resSectionFont, secBrush, 32, evalY);
            }

            evalY += 24;
            using (SolidBrush bodyBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                string b1 = string.Format("◦ Nắm vững kiến thức trọng tâm: Bạn đã hoàn thành tốt {0} câu hỏi trong bài kiểm tra ôn tập.", CorrectCount);
                g.DrawString(b1, _resBodyFont, bodyBrush, new RectangleF(32, evalY, w - 64, 40));
            }

            if (WrongCount > 0)
            {
                evalY += 44;
                using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString("Điểm cần xem lại", _resSectionFont, secBrush, 32, evalY);
                }

                evalY += 24;
                using (SolidBrush bodyBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
                {
                    string b2 = string.Format("◦ Bạn có {0} câu trả lời chưa chính xác. Hãy bấm nút 'Xem lại bài làm' bên dưới để đọc kỹ lời giải chi tiết của từng câu.", WrongCount);
                    g.DrawString(b2, _resBodyFont, bodyBrush, new RectangleF(32, evalY, w - 64, 40));
                }
            }

            // 8. Các nút hành động ở chân trang: [ ‹ Xem lại bài làm ]  [ 🔄 Làm lại đề này ]
            int footerY = h - 54;
            int btnW = 150;
            int btnH = 34;

            _btnReviewRect = new Rectangle((int)centerX - btnW - 10, footerY, btnW, btnH);
            _btnRetakeRect = new Rectangle((int)centerX + 10, footerY, btnW, btnH);

            // Nút Xem lại bài làm
            Color revBg = _isReviewHover ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252);
            using (SolidBrush rBg = new SolidBrush(revBg))
            using (Pen rBorder = new Pen(Color.FromArgb(203, 213, 225), 1.2f))
            using (SolidBrush rTxt = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.FillRoundedRectangle(rBg, _btnReviewRect, 17f);
                g.DrawRoundedRectangle(rBorder, _btnReviewRect, 17f);
                g.DrawString("‹ Xem lại bài làm", _btnFont, rTxt, _btnReviewRect.X + 22, _btnReviewRect.Y + 8);
            }

            // Nút Làm lại đề này
            Color retakeBg = _isRetakeHover ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235);
            using (SolidBrush rkBg = new SolidBrush(retakeBg))
            using (SolidBrush rkTxt = new SolidBrush(Color.White))
            {
                g.FillRoundedRectangle(rkBg, _btnRetakeRect, 17f);
                g.DrawString("🔄 Làm lại đề này", _btnFont, rkTxt, _btnRetakeRect.X + 20, _btnRetakeRect.Y + 8);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!IsResultScreen && _maxScroll > 0)
            {
                _scrollY = Math.Max(0, Math.Min(_maxScroll, _scrollY - (e.Delta / 3)));
                this.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePos = e.Location;

            bool prevCloseHover = _isCloseHover;
            bool prevPrevHover = _isPrevHover;
            bool prevNextHover = _isNextHover;
            int prevOptHover = _hoveredOptionIndex;
            bool prevRevHover = _isReviewHover;
            bool prevRetHover = _isRetakeHover;

            _isCloseHover = _btnCloseRect.Contains(_mousePos);

            if (IsResultScreen)
            {
                _isReviewHover = _btnReviewRect.Contains(_mousePos);
                _isRetakeHover = _btnRetakeRect.Contains(_mousePos);
                if (_isCloseHover != prevCloseHover || _isReviewHover != prevRevHover || _isRetakeHover != prevRetHover)
                {
                    this.Cursor = (_isCloseHover || _isReviewHover || _isRetakeHover) ? Cursors.Hand : Cursors.Default;
                    this.Invalidate();
                }
                return;
            }

            _isPrevHover = _btnPrevRect.Contains(_mousePos) && (CurrentIndex > 0);
            _isNextHover = _btnNextRect.Contains(_mousePos);

            _hoveredOptionIndex = -1;
            for (int i = 0; i < _optionRects.Count; i++)
            {
                if (_optionRects[i].Contains(_mousePos))
                {
                    _hoveredOptionIndex = i;
                    break;
                }
            }

            if (_isCloseHover != prevCloseHover || _isPrevHover != prevPrevHover || _isNextHover != prevNextHover || _hoveredOptionIndex != prevOptHover)
            {
                this.Cursor = (_isCloseHover || _isPrevHover || _isNextHover || _hoveredOptionIndex >= 0) ? Cursors.Hand : Cursors.Default;
                this.Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 1. Nút Close
            if (_btnCloseRect.Contains(e.Location))
            {
                CloseRequested?.Invoke();
                return;
            }

            // Xử lý click trên Màn hình kết quả Gemini
            if (IsResultScreen)
            {
                if (_btnReviewRect.Contains(e.Location))
                {
                    // Xem lại bài làm từ câu 1
                    IsResultScreen = false;
                    CurrentIndex = 0;
                    this.Invalidate();
                    return;
                }
                if (_btnRetakeRect.Contains(e.Location))
                {
                    // Reset làm lại đề
                    if (QuizData != null && QuizData.Questions != null)
                    {
                        foreach (var q in QuizData.Questions)
                        {
                            q.IsAnswered = false;
                            q.SelectedOptionIndex = -1;
                            q.IsCorrect = false;
                            q.ShowExplanation = false;
                        }
                    }
                    IsResultScreen = false;
                    CurrentIndex = 0;
                    RecalculateScores();
                    this.Invalidate();
                    return;
                }
                return;
            }

            if (QuizData == null || QuizData.Questions == null || QuizData.Questions.Count == 0) return;

            AiCanvasQuestion currentQ = QuizData.Questions[CurrentIndex];

            // 2. Click chọn đáp án trắc nghiệm
            if (!currentQ.IsAnswered)
            {
                for (int i = 0; i < _optionRects.Count; i++)
                {
                    if (_optionRects[i].Contains(e.Location))
                    {
                        currentQ.SelectedOptionIndex = i;
                        currentQ.IsAnswered = true;
                        if (i == currentQ.CorrectOptionIndex)
                        {
                            currentQ.IsCorrect = true;
                            CorrectCount++;
                        }
                        else
                        {
                            currentQ.IsCorrect = false;
                            WrongCount++;
                        }
                        currentQ.ShowExplanation = true;
                        this.Invalidate();
                        return;
                    }
                }
            }

            // 3. Click nút Kiểm tra điền khuyết
            if (!_btnCheckFillBlankRect.IsEmpty && _btnCheckFillBlankRect.Contains(e.Location))
            {
                currentQ.IsAnswered = true;
                currentQ.IsCorrect = true;
                currentQ.ShowExplanation = true;
                RecalculateScores();
                this.Invalidate();
                return;
            }

            // 4. Click nút Xem gợi ý tự luận
            if (!_btnShowEssayAnsRect.IsEmpty && _btnShowEssayAnsRect.Contains(e.Location))
            {
                currentQ.IsAnswered = true;
                currentQ.ShowExplanation = true;
                this.Invalidate();
                return;
            }

            // 5. Nút Quay lại
            if (_btnPrevRect.Contains(e.Location) && CurrentIndex > 0)
            {
                CurrentIndex--;
                this.Invalidate();
                return;
            }

            // 6. Nút Tiếp theo (hoặc Hoàn thành)
            if (_btnNextRect.Contains(e.Location))
            {
                if (CurrentIndex < QuizData.Questions.Count - 1)
                {
                    CurrentIndex++;
                    this.Invalidate();
                }
                else
                {
                    // Hoàn thành bài thi -> Chuyển sang Màn hình kết quả Gemini (KHÔNG dùng MessageBox popup!)
                    RecalculateScores();
                    IsResultScreen = true;
                    this.Invalidate();
                }
                return;
            }
        }
    }
}

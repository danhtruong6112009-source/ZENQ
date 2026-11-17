using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using ZenQOffice.Core;
using Graphics = ZenQOffice.UI.D2DGraphics;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Trang Tệp (Backstage View) chuẩn Microsoft Office 365 (PowerPoint, Excel, Word),
    /// tích hợp bộ chọn đa dạng tỷ lệ trình chiếu đa năng (16:9, 4:3, A4, FB, Insta, TikTok...)
    /// kèm chức năng "Tùy chỉnh" kích thước linh hoạt bằng pixel, cm, mm, inch.
    /// Toàn bộ đồ họa sử dụng Vector 2D khử răng cưa mượt mà và Fluent Vector Icons.
    /// </summary>
    public class OfficeBackstageView
    {
        private readonly AppForm _appForm;

        public bool IsOpen { get; set; } = false;
        public int ActiveSidebarTab { get; set; } = 0; // 0: Trang chủ, 1: Mới, 2: Mở, 3: Thông tin, 4: Tùy chọn
        public int ActiveRecentTab { get; set; } = 0;  // 0: Gần đây, 1: Đã ghim
        public string SearchText { get; set; } = "";
        public bool IsSearchFocused { get; set; } = false;

        // Cuộn trang mượt mà 60 FPS
        public float SmoothScrollY { get; set; } = 0f;
        public float TargetScrollY { get; set; } = 0f;
        private int _lastWidth = 1200;
        private int _lastHeight = 800;
        private long _scrollLastTs = 0;
        private double _openAnimStartMs = -10000.0;
        private const double BackstageRevealDurationMs = 175.0;
        private bool _sidebarPillInit = false;
        private int _sidebarPillLastTab = -1;
        private float _sidebarPillCurY = 0f;
        private float _sidebarPillStartY = 0f;
        private float _sidebarPillTargetY = 0f;
        private double _sidebarPillStartMs = -10000.0;
        private const double SidebarPillDurationMs = 160.0;

        // Trình chiếu Slides: Kích thước & Tùy chỉnh
        public bool ShowCustomSizePanel { get; set; } = false; // "bấm vô chữ tuỳ chỉnh nó mới hiện"
        public string CustomWidthText { get; set; } = "1920";
        public string CustomHeightText { get; set; } = "1080";
        public string CustomUnit { get; set; } = "px"; // px, cm, mm, inch
        public int FocusedInputIndex { get; set; } = -1; // 0: Width, 1: Height, -1: None

        // Hover States
        public bool HoverBackButton { get; set; } = false;
        public int HoverSidebarIndex { get; set; } = -1;
        public int HoverTemplateCardIndex { get; set; } = -1;
        public int HoverRecentRowIndex { get; set; } = -1;
        public int HoverPinIndex { get; set; } = -1;
        public int HoverUnitBtnIndex { get; set; } = -1;
        public bool HoverCreateCustomBtn { get; set; } = false;
        public bool HoverBrowseFileBtn { get; set; } = false;

        public int HoverModePillIndex { get; set; } = -1;

        public bool IsHoverHand => HoverBackButton || HoverSidebarIndex >= 0 ||
                                   HoverTemplateCardIndex >= 0 || HoverRecentRowIndex >= 0 ||
                                   HoverPinIndex >= 0 || HoverUnitBtnIndex >= 0 ||
                                   HoverCreateCustomBtn || HoverBrowseFileBtn ||
                                   HoverModePillIndex >= 0;

        // Hit-test Bounds
        private RectangleF _backButtonBounds = RectangleF.Empty;
        private readonly List<RectangleF> _sidebarItemBounds = new List<RectangleF>();
        private readonly List<RectangleF> _modePillBounds = new List<RectangleF>();
        private readonly List<RectangleF> _templateCardBounds = new List<RectangleF>();
        private readonly List<RectangleF> _recentRowBounds = new List<RectangleF>();
        private readonly List<RectangleF> _pinBtnBounds = new List<RectangleF>();
        private RectangleF _recentTabBtnBounds = RectangleF.Empty;
        private RectangleF _pinnedTabBtnBounds = RectangleF.Empty;
        private RectangleF _searchBoxBounds = RectangleF.Empty;
        private RectangleF _browseBtnBounds = RectangleF.Empty;
        private RectangleF _optionsBtnBounds = RectangleF.Empty;
        private RectangleF _feedbackBtnBounds = RectangleF.Empty;

        // Custom size panel bounds
        private RectangleF _customPanelBounds = RectangleF.Empty;
        private RectangleF _customWidthBoxBounds = RectangleF.Empty;
        private RectangleF _customHeightBoxBounds = RectangleF.Empty;
        private readonly List<RectangleF> _unitBtnBounds = new List<RectangleF>();
        private readonly List<RectangleF> _quickRatioBtnBounds = new List<RectangleF>();
        private RectangleF _createCustomBtnBounds = RectangleF.Empty;

        // Định nghĩa các tỷ lệ cho Slides
        public struct SlideRatioPreset
        {
            public string Title;
            public string Subtitle;
            public string Badge;
            public float Width;
            public float Height;
            public string Unit;
            public bool IsCustom;

            public SlideRatioPreset(string title, string subtitle, string badge, float w, float h, string unit = "px", bool isCustom = false)
            {
                Title = title;
                Subtitle = subtitle;
                Badge = badge;
                Width = w;
                Height = h;
                Unit = unit;
                IsCustom = isCustom;
            }
        }

        public readonly List<SlideRatioPreset> SlidePresets = new List<SlideRatioPreset>
        {
            new SlideRatioPreset("Thuyết trình 16:9", "1920 × 1080 px", "16:9", 1920f, 1080f),
            new SlideRatioPreset("Thuyết trình 4:3", "1024 × 768 px", "4:3", 1024f, 768f),
            new SlideRatioPreset("Tài liệu A4 Ngang", "29.7 × 21.0 cm", "A4", 1123f, 794f, "cm"),
            new SlideRatioPreset("Tài liệu A4 Dọc", "21.0 × 29.7 cm", "A4 Dọc", 794f, 1123f, "cm"),
            new SlideRatioPreset("Bài đăng Facebook", "1200 × 630 px", "Facebook", 1200f, 630f),
            new SlideRatioPreset("Bài đăng Instagram", "1080 × 1080 px", "1:1", 1080f, 1080f),
            new SlideRatioPreset("Nội dung TikTok và Reels", "1080 × 1920 px", "9:16", 1080f, 1920f),
            new SlideRatioPreset("Áp phích Poster", "42.0 × 59.4 cm", "Poster", 1587f, 2245f, "cm"),
            new SlideRatioPreset("Đồ họa Thông tin", "800 × 2000 px", "Dọc", 800f, 2000f),
            new SlideRatioPreset("Tùy chỉnh", "Tự đặt kích thước", "", 0f, 0f, "px", true)
        };

        public OfficeBackstageView(AppForm appForm)
        {
            _appForm = appForm;
        }

        public OfficeAppMode? OverrideNewMode { get; set; } = null;

        public OfficeAppMode GetEffectiveMode()
        {
            if (OverrideNewMode.HasValue) return OverrideNewMode.Value;
            if (_appForm != null && _appForm.Ribbon != null)
            {
                if (_appForm.Ribbon.CurrentMode == OfficeAppMode.Home) return OfficeAppMode.Slides;
                return _appForm.Ribbon.CurrentMode;
            }
            return OfficeAppMode.Slides;
        }

        public void Open(int tab = 0, OfficeAppMode? overrideMode = null)
        {
            IsOpen = true;
            ActiveSidebarTab = tab;
            OverrideNewMode = overrideMode;
            SmoothScrollY = 0f;
            TargetScrollY = 0f;
            _scrollLastTs = 0;
            _openAnimStartMs = FluentMotion.NowMs;
            _sidebarPillInit = false;
            _sidebarPillLastTab = -1;
            SearchText = "";
            IsSearchFocused = false;
            FocusedInputIndex = -1;
            if (_appForm != null)
            {
                _appForm.SlidesView?.HideSearchBox();
                _appForm.Invalidate();
            }
        }

        public void Close()
        {
            IsOpen = false;
            IsSearchFocused = false;
            FocusedInputIndex = -1;
            OverrideNewMode = null;
            if (_appForm != null)
            {
                _appForm.Cursor = Cursors.Default;
                _appForm.Invalidate();
            }
        }

        private Color GetThemeColor()
        {
            OfficeAppMode mode = GetEffectiveMode();
            switch (mode)
            {
                case OfficeAppMode.Writer: return Color.FromArgb(24, 90, 189);   // Word Deep Blue (#185ABD)
                case OfficeAppMode.Calc:   return Color.FromArgb(16, 124, 65);   // Excel Forest Green (#107C41)
                case OfficeAppMode.Slides: return Color.FromArgb(196, 62, 28);   // PowerPoint Rust Red (#C43E1C)
                case OfficeAppMode.Photo:  return Color.FromArgb(30, 41, 59);    // Slate Dark (#1E293B)
                case OfficeAppMode.Pdf:    return Color.FromArgb(179, 11, 0);    // PDF Crimson (#B30B00)
                default:                   return Color.FromArgb(0, 120, 212);   // Office Modern Blue (#0078D4)
            }
        }

        private string GetAppDisplayName()
        {
            OfficeAppMode mode = GetEffectiveMode();
            switch (mode)
            {
                case OfficeAppMode.Writer: return "Văn bản";
                case OfficeAppMode.Calc:   return "Bảng tính";
                case OfficeAppMode.Slides: return "Bản trình chiếu";
                case OfficeAppMode.Photo:  return "Thiết kế & Ảnh";
                case OfficeAppMode.Pdf:    return "Tài liệu PDF";
                default:                   return "ZenQ Office";
            }
        }

        public bool UpdateSmoothScroll(int windowHeight, int windowWidth)
        {
            double dt = FluentMotion.DeltaSeconds(ref _scrollLastTs);
            float scrollBlend = FluentMotion.DampFactor(dt, 0.026);
            int maxScroll = GetMaxScroll(windowHeight, windowWidth);
            TargetScrollY = Math.Max(0, Math.Min(maxScroll, (float)Math.Round(TargetScrollY)));

            float smooth = SmoothScrollY;
            bool moving = FluentMotion.StepAxis(ref smooth, TargetScrollY, scrollBlend, 1.0f);
            SmoothScrollY = smooth;
            if (!moving) _scrollLastTs = 0;

            double now = FluentMotion.NowMs;
            bool revealAnim = (now - _openAnimStartMs) >= 0 && (now - _openAnimStartMs) < BackstageRevealDurationMs;
            bool pillAnim = _sidebarPillInit && (now - _sidebarPillStartMs) >= 0 && (now - _sidebarPillStartMs) < SidebarPillDurationMs;
            return moving || revealAnim || pillAnim;
        }

        private int GetMaxScroll(int windowHeight, int windowWidth)
        {
            int totalContentHeight = 220; // Greeting + Section header
            totalContentHeight += 165;    // Template cards
            if (ShowCustomSizePanel) totalContentHeight += 120; // Custom panel

            List<RecentDocItem> docs = GetDisplayDocs();
            totalContentHeight += Math.Max(1, docs.Count) * 52 + 120;

            int viewportH = Math.Max(200, windowHeight);
            return Math.Max(0, totalContentHeight - viewportH + 40);
        }

        private static bool IsItemMatchingMode(RecentDocItem item, OfficeAppMode mode)
        {
            if (item == null) return false;
            string pathOrTitle = !string.IsNullOrEmpty(item.FilePath) ? item.FilePath : (item.Title ?? "");
            string ext = "";
            try { ext = Path.GetExtension(pathOrTitle).ToLowerInvariant(); } catch { }

            if (!string.IsNullOrEmpty(ext))
            {
                switch (mode)
                {
                    case OfficeAppMode.Slides:
                        return ext == ".pptx" || ext == ".ppt";
                    case OfficeAppMode.Writer:
                        return ext == ".docx" || ext == ".doc" || ext == ".odt" || ext == ".rtf" || ext == ".txt";
                    case OfficeAppMode.Calc:
                        return ext == ".xlsx" || ext == ".xls" || ext == ".ods" || ext == ".csv" || ext == ".tsv";
                    case OfficeAppMode.Pdf:
                        return ext == ".pdf";
                    case OfficeAppMode.Photo:
                        return ext == ".svg" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp" || ext == ".bmp" || ext == ".tiff";
                }
            }

            return item.Mode == mode;
        }

        private List<RecentDocItem> GetDisplayDocs()
        {
            var result = new List<RecentDocItem>();
            if (_appForm == null || _appForm.HomeView == null || _appForm.HomeView.RecentDocs == null)
                return result;

            var baseList = _appForm.HomeView.RecentDocs;
            string kw = SearchText.Trim().ToLowerInvariant();
            OfficeAppMode curMode = GetEffectiveMode();

            foreach (var item in baseList)
            {
                if (!IsItemMatchingMode(item, curMode)) continue;
                if (ActiveRecentTab == 1 && !item.IsStarred) continue;
                if (!string.IsNullOrEmpty(kw))
                {
                    if ((item.Title != null && item.Title.ToLowerInvariant().Contains(kw)) ||
                        (item.FilePath != null && item.FilePath.ToLowerInvariant().Contains(kw)))
                    {
                        result.Add(item);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }
            return result;
        }

        // ==========================================
        // VẼ GIAO DIỆN BACKSTAGE 2D & VECTOR ICONS
        // ==========================================
        public void Draw(System.Drawing.Graphics g, int width, int height)
        {
            if (g == null) return;
            using (D2DGraphics d2d = new D2DGraphics(g, width, height))
            {
                Draw(d2d, width, height);
            }
        }

        public void Draw(Graphics g, int width, int height)
        {
            if (_appForm != null)
            {
                _appForm.SlidesView?.HideSearchBox();
            }
            _lastWidth = width;
            _lastHeight = height;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int scroll = (int)Math.Round(SmoothScrollY);
            Color themeColor = GetThemeColor();

            // 1. Phông nền toàn bộ vùng chính (Off-white sang trọng chuẩn Office 365)
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            {
                g.FillRectangle(bgBrush, 0, 0, width, height);
            }

            // 2. THANH ĐIỀU HƯỚNG BÊN TRÁI (LEFT SIDEBAR RAIL)
            float sidebarWidth = 210f;
            RectangleF sidebarRect = new RectangleF(0, 0, sidebarWidth, height);
            using (SolidBrush sbBrush = new SolidBrush(themeColor))
            {
                g.FillRectangle(sbBrush, sidebarRect);
            }

            // 2.1 Nút quay lại (Circular Back Button <-)
            _backButtonBounds = new RectangleF(16, 14, 38, 38);
            if (HoverBackButton)
            {
                using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(45, 255, 255, 255)))
                {
                    g.FillEllipse(hoverBg, _backButtonBounds);
                }
            }
            using (Pen circlePen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.2f))
            {
                g.DrawEllipse(circlePen, _backButtonBounds);
            }
            // Vẽ biểu tượng mũi tên quay lại chuẩn Fluent Vector
            FluentIconRenderer.DrawGlyph(g, '\uE72B', new Rectangle((int)_backButtonBounds.X + 9, (int)_backButtonBounds.Y + 9, 20, 20), Color.White, 16f);

            // Tên Phân hệ (e.g. "PowerPoint", "Excel", "Word")
            using (Font titleFont = new Font("Segoe UI", 12.0f, FontStyle.Bold))
            using (SolidBrush txtBrush = new SolidBrush(Color.White))
            {
                g.DrawString(AppL10n.Tr(GetAppDisplayName()), titleFont, txtBrush, 64, 21);
            }

            // 2.2 Các mục Menu Điều hướng bên trái
            _sidebarItemBounds.Clear();
            string[] navLabels = new string[] {
                "Trang chủ",            // 0
                "Mới",                  // 1
                "Mở",                   // 2
                "---",                  // 3 (Separator)
                "Thông tin",            // 4
                "Lưu",                  // 5
                "Lưu dưới dạng",        // 6
                "In",                   // 7
                "Chia sẻ & Gửi Email",  // 8
                "Xuất",                 // 9
                "Bản sao lưu tệp gốc",  // 10
                "Font & Tiếng Việt",    // 11 (Trích xuất & Sửa lỗi Font)
                "---",                  // 12 (Separator)
                "Đóng"                  // 13
            };
            char[] navGlyphs = new char[] {
                '\uE80F', // Home
                '\uE7C3', // Page/New
                '\uE838', // Open Folder
                '\0',
                '\uE946', // Info
                '\uE74E', // Save
                '\uE792', // Save As
                '\uE2F6', // Print
                '\uE715', // Mail / Share
                '\uEDE1', // Export
                '\uEA35', // Safety Vault / Shield / History / Backup
                '\uE8D2', // Font / Text
                '\0',
                '\uE711'  // Close
            };

            float curItemY = 68f;
            using (Font itemFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (Font itemActiveFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush activeBg = new SolidBrush(Color.FromArgb(48, 0, 0, 0)))
            using (SolidBrush hoverBg = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            using (Pen sepPen = new Pen(Color.FromArgb(45, 255, 255, 255), 1.0f))
            {
                float targetPillY = -1f;
                float scanY = 68f;
                for (int i = 0; i < navLabels.Length; i++)
                {
                    if (navLabels[i] == "---") { scanY += 14f; continue; }
                    if (ActiveSidebarTab == i) { targetPillY = scanY; break; }
                    scanY += 39f;
                }
                if (targetPillY >= 0f)
                {
                    double pillNow = FluentMotion.NowMs;
                    if (!_sidebarPillInit)
                    {
                        _sidebarPillInit = true;
                        _sidebarPillLastTab = ActiveSidebarTab;
                        _sidebarPillStartY = _sidebarPillCurY = _sidebarPillTargetY = targetPillY;
                        _sidebarPillStartMs = -10000.0;
                    }
                    else if (_sidebarPillLastTab != ActiveSidebarTab)
                    {
                        _sidebarPillLastTab = ActiveSidebarTab;
                        _sidebarPillStartY = _sidebarPillCurY;
                        _sidebarPillTargetY = targetPillY;
                        _sidebarPillStartMs = pillNow;
                    }
                    else
                    {
                        _sidebarPillTargetY = targetPillY;
                    }

                    double pillElapsed = pillNow - _sidebarPillStartMs;
                    if (pillElapsed >= 0 && pillElapsed < SidebarPillDurationMs)
                    {
                        float pe = FluentMotion.EaseOutQuint((float)(pillElapsed / SidebarPillDurationMs));
                        _sidebarPillCurY = FluentMotion.Lerp(_sidebarPillStartY, _sidebarPillTargetY, pe);
                    }
                    else
                    {
                        _sidebarPillCurY = _sidebarPillTargetY;
                    }

                    g.FillRoundedRectangle(activeBg, new RectangleF(8f, _sidebarPillCurY, sidebarWidth - 16f, 36f), 8f);
                    using (SolidBrush lineBr = new SolidBrush(Color.White))
                    {
                        g.FillRoundedRectangle(lineBr, new RectangleF(8f, _sidebarPillCurY + 8f, 3.5f, 20f), 2f);
                    }
                }

                for (int i = 0; i < navLabels.Length; i++)
                {
                    if (navLabels[i] == "---")
                    {
                        g.DrawLine(sepPen, 18f, curItemY + 6f, sidebarWidth - 18f, curItemY + 6f);
                        curItemY += 14f;
                        _sidebarItemBounds.Add(RectangleF.Empty);
                        continue;
                    }

                    RectangleF itemRect = new RectangleF(8f, curItemY, sidebarWidth - 16f, 36f);
                    _sidebarItemBounds.Add(new RectangleF(0f, curItemY, sidebarWidth, 38f));

                    bool isActive = (ActiveSidebarTab == i);
                    bool isHover = (HoverSidebarIndex == i);

                    if (!isActive && isHover)
                    {
                        g.FillRoundedRectangle(hoverBg, itemRect, 8f);
                    }

                    // Biểu tượng Vector
                    FluentIconRenderer.DrawGlyph(g, navGlyphs[i], new Rectangle(20, (int)curItemY + 8, 20, 20), Color.White, 15.5f);

                    // Tiêu đề
                    g.DrawString(AppL10n.Tr(navLabels[i]), isActive ? itemActiveFont : itemFont, textBrush, 48f, curItemY + 9f);

                    curItemY += 39f;
                }

                // Mục "Báo lỗi & Góp ý" ghim ngay trên Tùy chọn ở góc dưới Sidebar
                _feedbackBtnBounds = new RectangleF(0, height - 88f, sidebarWidth, 38f);
                RectangleF fbVisual = new RectangleF(8f, height - 87f, sidebarWidth - 16f, 36f);
                bool isHoverFeedback = (HoverSidebarIndex == 101);
                if (isHoverFeedback) g.FillRoundedRectangle(hoverBg, fbVisual, 8f);
                FluentIconRenderer.DrawGlyph(g, '\uED15', new Rectangle(20, (int)_feedbackBtnBounds.Y + 9, 20, 20), Color.White, 15.5f);
                g.DrawString(AppL10n.T("Báo lỗi & Góp ý", "Feedback"), itemFont, textBrush, 48f, _feedbackBtnBounds.Y + 10f);

                // Mục "Tùy chọn / Cài đặt" ghim ở góc dưới Sidebar
                _optionsBtnBounds = new RectangleF(0, height - 46f, sidebarWidth, 38f);
                RectangleF optVisual = new RectangleF(8f, height - 45f, sidebarWidth - 16f, 36f);
                bool isHoverOpt = (HoverSidebarIndex == 100);
                if (isHoverOpt) g.FillRoundedRectangle(hoverBg, optVisual, 8f);
                FluentIconRenderer.DrawGlyph(g, '\uE713', new Rectangle(20, (int)_optionsBtnBounds.Y + 9, 20, 20), Color.White, 15.5f);
                g.DrawString(AppL10n.T("Tùy chọn", "Options"), itemFont, textBrush, 48f, _optionsBtnBounds.Y + 10f);
            }

            // 3. VÙNG NỘI DUNG CHÍNH (MAIN WORKSPACE CONTENT)
            double revealElapsed = FluentMotion.NowMs - _openAnimStartMs;
            float revealDx = (revealElapsed >= 0 && revealElapsed < BackstageRevealDurationMs)
                ? 18f * (1f - FluentMotion.EaseOutQuint((float)(revealElapsed / BackstageRevealDurationMs)))
                : 0f;
            float contentX = sidebarWidth + 44f + revealDx;
            float contentW = width - (sidebarWidth + 44f) - 44f;
            if (contentW < 400f) contentW = 400f;

            GraphicsState gClip = g.Save();
            g.SetClip(new RectangleF(sidebarWidth, 0, width - sidebarWidth, height));

            float topY = 28f - scroll;

            // 3.1 Tiêu đề Chào mừng theo thời gian (Good morning / Good afternoon / Good evening)
            int hour = DateTime.Now.Hour;
            string greeting = (hour < 12) ? AppL10n.T("Chào buổi sáng", "Good morning") : ((hour < 18) ? AppL10n.T("Chào buổi chiều", "Good afternoon") : AppL10n.T("Chào buổi tối", "Good evening"));
            using (Font greetFont = new Font("Segoe UI", 20.0f, FontStyle.Bold))
            using (SolidBrush greetBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(greeting, greetFont, greetBrush, contentX, topY);
            }
            topY += 46f;

            // 3.2 Nhóm: MỚI (NEW)
            FluentIconRenderer.DrawGlyph(g, '\uE70D', new RectangleF(contentX, topY + 4f, 14f, 14f), Color.FromArgb(30, 41, 59), 10.5f);
            using (Font secFont = new Font("Segoe UI", 12.0f, FontStyle.Bold))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(AppL10n.T("Mới", "New"), secFont, secBrush, contentX + 18f, topY);
            }

            OfficeAppMode curMode = GetEffectiveMode();

            // Mode switcher pills (bo tròn viên thuốc chuẩn Fluent, không gai góc)
            _modePillBounds.Clear();
            float pillX = contentX + 70f;
            float pillY = topY - 3f;
            (OfficeAppMode mode, string title, char icon)[] pills = new (OfficeAppMode, string, char)[]
            {
                (OfficeAppMode.Slides, "Bản trình chiếu", '\uE740'),
                (OfficeAppMode.Writer, "Văn bản", '\uE8D2'),
                (OfficeAppMode.Calc, "Bảng tính", '\uF158'),
                (OfficeAppMode.Photo, "Thiết kế & Ảnh", '\uEB9F')
            };

            using (Font pillFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            {
                for (int pIdx = 0; pIdx < pills.Length; pIdx++)
                {
                    var pItem = pills[pIdx];
                    string pTitleDisp = AppL10n.Tr(pItem.title);
                    SizeF sz = g.MeasureString(pTitleDisp, pillFont);
                    float iconW = 16f;
                    float pW = MathF.Round(sz.Width + iconW + 26f);
                    float pH = 30f;
                    RectangleF pRect = new RectangleF(pillX, pillY, pW, pH);
                    _modePillBounds.Add(pRect);

                    bool isPillSel = (curMode == pItem.mode);
                    bool isPillHov = (HoverModePillIndex == pIdx);

                    if (isPillSel)
                    {
                        using (SolidBrush selBg = new SolidBrush(themeColor))
                        {
                            g.FillRoundedRectangle(selBg, pRect, 15f);
                        }
                        FluentIconRenderer.DrawGlyph(g, pItem.icon, new RectangleF(pillX + 10f, pillY + 7f, iconW, iconW), Color.White, 12f);
                        using (SolidBrush pTxt = new SolidBrush(Color.White))
                        {
                            g.DrawString(pTitleDisp, pillFont, pTxt, pillX + iconW + 14f, pillY + 6f);
                        }
                    }
                    else
                    {
                        using (SolidBrush idleBg = new SolidBrush(isPillHov ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)))
                        using (Pen idlePen = new Pen(isPillHov ? Color.FromArgb(203, 213, 225) : Color.FromArgb(226, 232, 240), 1f))
                        {
                            g.FillRoundedRectangle(idleBg, pRect, 15f);
                            g.DrawRoundedRectangle(idlePen, pRect, 15f);
                        }
                        FluentIconRenderer.DrawGlyph(g, pItem.icon, new RectangleF(pillX + 10f, pillY + 7f, iconW, iconW), Color.FromArgb(71, 85, 105), 12f);
                        using (SolidBrush pTxt = new SolidBrush(Color.FromArgb(51, 65, 85)))
                        {
                            g.DrawString(pTitleDisp, pillFont, pTxt, pillX + iconW + 14f, pillY + 6f);
                        }
                    }

                    pillX += pW + 10f;
                }
            }

            topY += 38f;

            // Danh sách Thẻ Mẫu / Presets (Chuẩn tỷ lệ cho Slides, hoặc Templates cho Word/Excel)
            _templateCardBounds.Clear();
            float cardW = 132f;
            float cardH = 142f;
            float cardSpacing = 14f;
            float curCardX = contentX;

            if (curMode == OfficeAppMode.Slides)
            {
                for (int i = 0; i < SlidePresets.Count; i++)
                {
                    // Tự động xuống dòng nếu vượt quá chiều ngang màn hình
                    if (curCardX + cardW > contentX + contentW && i > 0)
                    {
                        curCardX = contentX;
                        topY += cardH + 14f;
                    }

                    RectangleF cRect = new RectangleF(curCardX, topY, cardW, cardH);
                    _templateCardBounds.Add(cRect);
                    DrawSlidePresetCard(g, cRect, SlidePresets[i], HoverTemplateCardIndex == i, themeColor);
                    curCardX += cardW + cardSpacing;
                }
                topY += cardH + 18f;

                // ==========================================
                // BẢNG ĐIỀU KHIỂN TÙY CHỈNH KÍCH THƯỚC (CUSTOM SIZE PANEL)
                // "bấm vô chữ tuỳ chỉnh nó mới hiện"
                // ==========================================
                if (ShowCustomSizePanel)
                {
                    DrawCustomSizePanel(g, contentX, topY, Math.Min(contentW, 760f), themeColor);
                    topY += _customPanelBounds.Height + 20f;
                }
            }
            else if (curMode == OfficeAppMode.Photo)
            {
                // Thẻ mẫu Thiết kế
                string[] photoTmpls = new string[] { "Bản vẽ Full HD 1920x1080", "Ảnh vuông 1080x1080", "Ảnh chuẩn 1280x720", "Áp phích áp ảnh 4K", "Biểu ngữ mạng xã hội" };
                for (int i = 0; i < photoTmpls.Length; i++)
                {
                    RectangleF cRect = new RectangleF(curCardX, topY, cardW, cardH);
                    _templateCardBounds.Add(cRect);
                    DrawGenericTemplateCard(g, cRect, photoTmpls[i], '\uEB9F', HoverTemplateCardIndex == i, themeColor, i == 0);
                    curCardX += cardW + cardSpacing;
                }
                topY += cardH + 20f;
            }
            else if (curMode == OfficeAppMode.Calc)
            {
                // Thẻ mẫu Bảng tính
                string[] calcTmpls = new string[] { "Sổ làm việc trống", "Bảng tính ngân sách", "Lịch kế hoạch tuần", "Bảng chấm công", "Báo cáo doanh số" };
                for (int i = 0; i < calcTmpls.Length; i++)
                {
                    RectangleF cRect = new RectangleF(curCardX, topY, cardW, cardH);
                    _templateCardBounds.Add(cRect);
                    DrawGenericTemplateCard(g, cRect, calcTmpls[i], '\uF158', HoverTemplateCardIndex == i, themeColor, i == 0);
                    curCardX += cardW + cardSpacing;
                }
                topY += cardH + 20f;
            }
            else
            {
                // Thẻ mẫu Văn bản
                string[] wordTmpls = new string[] { "Tài liệu trống", "Báo cáo trang bìa", "Sơ yếu lý lịch", "Công văn và Thư từ", "Bản tin nội bộ" };
                for (int i = 0; i < wordTmpls.Length; i++)
                {
                    RectangleF cRect = new RectangleF(curCardX, topY, cardW, cardH);
                    _templateCardBounds.Add(cRect);
                    DrawGenericTemplateCard(g, cRect, wordTmpls[i], '\uE7C3', HoverTemplateCardIndex == i, themeColor, i == 0);
                    curCardX += cardW + cardSpacing;
                }
                topY += cardH + 20f;
            }

            // Đường kẻ phân cách nhẹ nhàng giữa phần Mới và Danh sách tệp Gần đây
            using (Pen divPen = new Pen(Color.FromArgb(226, 232, 240), 1.0f))
            {
                g.DrawLine(divPen, contentX, topY, contentX + contentW, topY);
            }
            topY += 18f;

            // 3.3 Nhóm: GẦN ĐÂY (RECENT) & ĐÃ GHIM (PINNED)
            float tabBtnY = topY;
            using (Font tabFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (Font tabIdleFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
            using (SolidBrush darkBr = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush grayBr = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush lineBr = new SolidBrush(themeColor))
            {
                // Nút "Gần đây"
                _recentTabBtnBounds = new RectangleF(contentX, tabBtnY, 76f, 28f);
                g.DrawString(AppL10n.T("Gần đây", "Recent"), ActiveRecentTab == 0 ? tabFont : tabIdleFont, ActiveRecentTab == 0 ? darkBr : grayBr, _recentTabBtnBounds.X, _recentTabBtnBounds.Y);
                if (ActiveRecentTab == 0)
                {
                    g.FillRoundedRectangle(lineBr, new RectangleF(_recentTabBtnBounds.X, _recentTabBtnBounds.Y + 24f, 58f, 3f), 1.5f);
                }

                // Nút "Đã ghim"
                _pinnedTabBtnBounds = new RectangleF(contentX + 85f, tabBtnY, 76f, 28f);
                g.DrawString(AppL10n.T("Đã ghim", "Pinned"), ActiveRecentTab == 1 ? tabFont : tabIdleFont, ActiveRecentTab == 1 ? darkBr : grayBr, _pinnedTabBtnBounds.X, _pinnedTabBtnBounds.Y);
                if (ActiveRecentTab == 1)
                {
                    g.FillRoundedRectangle(lineBr, new RectangleF(_pinnedTabBtnBounds.X, _pinnedTabBtnBounds.Y + 24f, 58f, 3f), 1.5f);
                }
            }

            // Hộp tìm kiếm tài liệu ở góc phải
            float searchW = 240f;
            _searchBoxBounds = new RectangleF(contentX + contentW - searchW, tabBtnY - 2f, searchW, 30f);
            DrawSearchBox(g, _searchBoxBounds);

            topY += 42f;

            // 3.4 BẢNG DANH SÁCH TÀI LIỆU GẦN ĐÂY
            DrawRecentFilesTable(g, contentX, topY, contentW, themeColor);

            g.Restore(gClip);
        }

        // ==========================================
        // VẼ THẺ PRESET TỶ LỆ TRÌNH CHIẾU (FLUENT ANTI-ALIASED D2D)
        // ==========================================
        private void DrawSlidePresetCard(Graphics g, RectangleF bounds, SlideRatioPreset preset, bool isHover, Color themeColor)
        {
            // Đổ bóng nổi bật khử răng cưa mềm mại chuẩn Fluent Design
            if (isHover)
            {
                using (SolidBrush shadowBr = new SolidBrush(Color.FromArgb(20, 15, 23, 42)))
                {
                    g.FillRoundedRectangle(shadowBr, new RectangleF(bounds.X, bounds.Y + 3f, bounds.Width, bounds.Height), 14f);
                }
            }

            using (SolidBrush bgBrush = new SolidBrush(isHover ? Color.FromArgb(254, 254, 255) : Color.White))
            using (Pen borderPen = new Pen(isHover ? themeColor : Color.FromArgb(226, 232, 240), isHover ? 1.6f : 1.0f))
            {
                g.FillRoundedRectangle(bgBrush, bounds, 14f);
                g.DrawRoundedRectangle(borderPen, bounds, 14f);
            }

            // Vùng hình thu nhỏ tỷ lệ bo góc 10px mềm mại
            RectangleF thumbBox = new RectangleF(bounds.X + 10f, bounds.Y + 10f, bounds.Width - 20f, 74f);
            using (SolidBrush tbBg = new SolidBrush(isHover ? Color.FromArgb(238, 242, 250) : Color.FromArgb(246, 248, 251)))
            using (Pen tbPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRoundedRectangle(tbBg, thumbBox, 10f);
                g.DrawRoundedRectangle(tbPen, thumbBox, 10f);
            }

            if (preset.IsCustom)
            {
                // Biểu tượng Bảng màu / Thước kẻ Cỡ tùy chỉnh hiện đại
                Rectangle iconCircle = new Rectangle((int)thumbBox.X + (int)thumbBox.Width / 2 - 17, (int)thumbBox.Y + 10, 34, 34);
                using (SolidBrush cirBg = new SolidBrush(Color.FromArgb(254, 242, 242)))
                {
                    g.FillEllipse(cirBg, iconCircle);
                }
                FluentIconRenderer.DrawGlyph(g, '\uE790', iconCircle, themeColor, 18f);
                using (Font cFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                using (SolidBrush cBr = new SolidBrush(themeColor))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(AppL10n.T("Cỡ tùy chỉnh", "Custom Size"), cFont, cBr, thumbBox.X + thumbBox.Width / 2f, thumbBox.Y + 47f, sf);
                }
            }
            else
            {
                // Vẽ khung tỷ lệ chính xác thu nhỏ bên trong (mô phỏng slide thực tế, chống gai góc)
                float topPad = !string.IsNullOrEmpty(preset.Badge) ? 20f : 8f;
                float availW = thumbBox.Width - 18f;
                float availH = thumbBox.Height - topPad - 8f;
                float aspect = preset.Width / Math.Max(1f, preset.Height);

                float tw = availW;
                float th = tw / aspect;
                if (th > availH)
                {
                    th = availH;
                    tw = th * aspect;
                }

                float tx = MathF.Round(thumbBox.X + (thumbBox.Width - tw) / 2f);
                float ty = MathF.Round(thumbBox.Y + topPad + (availH - th) / 2f);

                RectangleF miniSlide = new RectangleF(tx, ty, MathF.Round(tw), MathF.Round(th));
                using (SolidBrush shadowMini = new SolidBrush(Color.FromArgb(18, 15, 23, 42)))
                using (SolidBrush msBg = new SolidBrush(Color.White))
                using (Pen msPen = new Pen(isHover ? themeColor : Color.FromArgb(203, 213, 225), 1.1f))
                {
                    g.FillRoundedRectangle(shadowMini, new RectangleF(miniSlide.X, miniSlide.Y + 1.5f, miniSlide.Width, miniSlide.Height), 4f);
                    g.FillRoundedRectangle(msBg, miniSlide, 4f);
                    g.DrawRoundedRectangle(msPen, miniSlide, 4f);
                }

                // Điểm nhấn bố cục slide thu nhỏ (dải tiêu đề và gạch nội dung mềm mại)
                if (miniSlide.Width >= 24f && miniSlide.Height >= 16f)
                {
                    float padS = Math.Max(3f, miniSlide.Width * 0.08f);
                    float barW = Math.Max(12f, miniSlide.Width * 0.45f);
                    float barH = Math.Max(2f, Math.Min(4f, miniSlide.Height * 0.12f));
                    using (SolidBrush lineAccent = new SolidBrush(isHover ? Color.FromArgb(140, themeColor) : Color.FromArgb(203, 213, 225)))
                    using (SolidBrush lineSub = new SolidBrush(Color.FromArgb(226, 232, 240)))
                    {
                        g.FillRoundedRectangle(lineAccent, new RectangleF(miniSlide.X + padS, miniSlide.Y + padS, barW, barH), 1.5f);
                        if (miniSlide.Height >= 24f)
                        {
                            g.FillRoundedRectangle(lineSub, new RectangleF(miniSlide.X + padS, miniSlide.Y + padS + barH + 3f, miniSlide.Width * 0.32f, barH * 0.8f), 1f);
                        }
                    }
                }

                // Huy hiệu tỷ lệ nhỏ (Badge pill)
                if (!string.IsNullOrEmpty(preset.Badge))
                {
                    using (Font bdFont = new Font("Segoe UI", 6.8f, FontStyle.Bold))
                    {
                        SizeF bSz = g.MeasureString(preset.Badge, bdFont);
                        float pillW = Math.Max(22f, MathF.Round(bSz.Width + 10f));
                        RectangleF badgeRect = new RectangleF(thumbBox.X + 6f, thumbBox.Y + 4f, pillW, 14f);
                        using (SolidBrush bdBg = new SolidBrush(isHover ? Color.FromArgb(32, themeColor) : Color.FromArgb(226, 232, 240)))
                        using (SolidBrush bdTxt = new SolidBrush(isHover ? themeColor : Color.FromArgb(71, 85, 105)))
                        {
                            g.FillRoundedRectangle(bdBg, badgeRect, 7f);
                            StringFormat bSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(preset.Badge, bdFont, bdTxt, badgeRect, bSf);
                        }
                    }
                }
            }

            // Tên và thông số kích thước bên dưới
            using (Font titleF = new Font("Segoe UI", 8.4f, FontStyle.Bold))
            using (Font subF = new Font("Segoe UI", 7.6f, FontStyle.Regular))
            using (SolidBrush tBr = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush sBr = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(AppL10n.Tr(preset.Title), titleF, tBr, new RectangleF(bounds.X + 10f, bounds.Y + 92f, bounds.Width - 20f, 18f), sf);
                g.DrawString(AppL10n.Tr(preset.Subtitle), subF, sBr, bounds.X + 10f, bounds.Y + 112f);
            }
        }

        private void DrawGenericTemplateCard(Graphics g, RectangleF bounds, string title, char icon, bool isHover, Color themeColor, bool isBlank)
        {
            if (isHover)
            {
                using (SolidBrush shadowBr = new SolidBrush(Color.FromArgb(14, 15, 23, 42)))
                {
                    g.FillRoundedRectangle(shadowBr, new RectangleF(bounds.X, bounds.Y + 2f, bounds.Width, bounds.Height), 12f);
                }
            }

            using (SolidBrush bgBrush = new SolidBrush(isHover ? Color.FromArgb(252, 253, 255) : Color.White))
            using (Pen borderPen = new Pen(isHover ? themeColor : Color.FromArgb(226, 232, 240), isHover ? 1.6f : 1.0f))
            {
                g.FillRoundedRectangle(bgBrush, bounds, 12f);
                g.DrawRoundedRectangle(borderPen, bounds, 12f);
            }

            RectangleF thumbBox = new RectangleF(bounds.X + 10f, bounds.Y + 10f, bounds.Width - 20f, 74f);
            using (SolidBrush tbBg = new SolidBrush(isHover ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)))
            using (Pen tbPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillRoundedRectangle(tbBg, thumbBox, 8f);
                g.DrawRoundedRectangle(tbPen, thumbBox, 8f);
            }

            if (isBlank)
            {
                // Thẻ tài liệu trắng cơ bản bo tròn
                using (SolidBrush wBg = new SolidBrush(Color.White))
                using (Pen wPen = new Pen(isHover ? themeColor : Color.FromArgb(148, 163, 184), 1.2f))
                {
                    RectangleF doc = new RectangleF(thumbBox.X + 22f, thumbBox.Y + 8f, thumbBox.Width - 44f, thumbBox.Height - 16f);
                    g.FillRoundedRectangle(wBg, doc, 3f);
                    g.DrawRoundedRectangle(wPen, doc, 3f);
                }
            }
            else
            {
                FluentIconRenderer.DrawGlyph(g, icon, new Rectangle((int)thumbBox.X + (int)thumbBox.Width / 2 - 14, (int)thumbBox.Y + 22, 28, 28), themeColor, 22f);
            }

            using (Font titleF = new Font("Segoe UI", 8.4f, FontStyle.Bold))
            using (SolidBrush tBr = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(AppL10n.Tr(title), titleF, tBr, new RectangleF(bounds.X + 10f, bounds.Y + 92f, bounds.Width - 20f, 38f), sf);
            }
        }

        // ==========================================
        // VẼ BẢNG TÙY CHỈNH KÍCH THƯỚC (CUSTOM SIZE)
        // ==========================================
        private void DrawCustomSizePanel(Graphics g, float x, float y, float width, Color themeColor)
        {
            _customPanelBounds = new RectangleF(x, y, width, 106f);

            using (SolidBrush cpBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen cpPen = new Pen(Color.FromArgb(226, 232, 240), 1.2f))
            {
                g.FillRoundedRectangle(cpBg, _customPanelBounds, 12f);
                g.DrawRoundedRectangle(cpPen, _customPanelBounds, 12f);
            }

            // Vạch màu nhấn phân biệt
            using (SolidBrush acBr = new SolidBrush(themeColor))
            {
                g.FillRoundedRectangle(acBr, new RectangleF(x + 6f, y + 12f, 4f, _customPanelBounds.Height - 24f), 2f);
            }

            // Tiêu đề bảng điều khiển
            using (Font hFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                FluentIconRenderer.DrawGlyph(g, '\uE740', new RectangleF(x + 18f, y + 13f, 15f, 15f), Color.FromArgb(30, 41, 59), 13f);
                g.DrawString(AppL10n.T("Tùy chỉnh kích thước trang chiếu:", "Custom Slide Dimensions:"), hFont, hBrush, x + 38f, y + 12f);
            }

            float rowY = y + 42f;

            // 1. Ô nhập Chiều Rộng
            using (Font lblFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            using (Font valFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (SolidBrush lblBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            using (SolidBrush valBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(AppL10n.T("Chiều rộng:", "Width:"), lblFont, lblBrush, x + 18f, rowY + 6f);
                _customWidthBoxBounds = new RectangleF(x + 90f, rowY, 82f, 32f);

                bool isWFocus = (FocusedInputIndex == 0);
                using (SolidBrush boxBg = new SolidBrush(Color.White))
                using (Pen boxPen = new Pen(isWFocus ? themeColor : Color.FromArgb(203, 213, 225), isWFocus ? 1.8f : 1.0f))
                {
                    g.FillRoundedRectangle(boxBg, _customWidthBoxBounds, 6f);
                    g.DrawRoundedRectangle(boxPen, _customWidthBoxBounds, 6f);
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                    g.DrawString(CustomWidthText + (isWFocus ? "|" : ""), valFont, valBrush, _customWidthBoxBounds, sf);
                }

                // 2. Ô nhập Chiều Cao
                g.DrawString(AppL10n.T("Chiều cao:", "Height:"), lblFont, lblBrush, x + 186f, rowY + 6f);
                _customHeightBoxBounds = new RectangleF(x + 252f, rowY, 82f, 32f);

                bool isHFocus = (FocusedInputIndex == 1);
                using (SolidBrush boxBg = new SolidBrush(Color.White))
                using (Pen boxPen = new Pen(isHFocus ? themeColor : Color.FromArgb(203, 213, 225), isHFocus ? 1.8f : 1.0f))
                {
                    g.FillRoundedRectangle(boxBg, _customHeightBoxBounds, 6f);
                    g.DrawRoundedRectangle(boxPen, _customHeightBoxBounds, 6f);
                    StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                    g.DrawString(CustomHeightText + (isHFocus ? "|" : ""), valFont, valBrush, _customHeightBoxBounds, sf);
                }
            }

            // 3. Bộ chọn Đơn vị tính: px | cm | mm | inch
            string[] units = new string[] { "px", "cm", "mm", "inch" };
            _unitBtnBounds.Clear();
            float unitX = x + 348f;
            using (Font uFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                for (int u = 0; u < units.Length; u++)
                {
                    RectangleF uRect = new RectangleF(unitX, rowY + 2f, 44f, 28f);
                    _unitBtnBounds.Add(uRect);

                    bool isSelected = string.Equals(CustomUnit, units[u], StringComparison.OrdinalIgnoreCase);
                    bool isHoverU = (HoverUnitBtnIndex == u);

                    using (SolidBrush uBg = new SolidBrush(isSelected ? themeColor : (isHoverU ? Color.FromArgb(226, 232, 240) : Color.White)))
                    using (Pen uPen = new Pen(isSelected ? themeColor : Color.FromArgb(203, 213, 225), 1f))
                    using (SolidBrush uTxt = new SolidBrush(isSelected ? Color.White : Color.FromArgb(71, 85, 105)))
                    {
                        g.FillRoundedRectangle(uBg, uRect, 6f);
                        g.DrawRoundedRectangle(uPen, uRect, 6f);
                        StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                        g.DrawString(units[u], uFont, uTxt, uRect, sf);
                    }
                    unitX += 48f;
                }
            }

            // 4. Nút bấm Tạo thiết kế mới (Primary Action Button)
            _createCustomBtnBounds = new RectangleF(unitX + 16f, rowY, 152f, 32f);
            using (SolidBrush btnBg = new SolidBrush(HoverCreateCustomBtn ? ControlPaint.Light(themeColor, 0.15f) : themeColor))
            using (Font btnFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (SolidBrush btnTxt = new SolidBrush(Color.White))
            {
                g.FillRoundedRectangle(btnBg, _createCustomBtnBounds, 8f);
                FluentIconRenderer.DrawGlyph(g, '\uE710', new RectangleF(_createCustomBtnBounds.X + 10f, _createCustomBtnBounds.Y + 8f, 16f, 16f), Color.White, 13f);
                StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                g.DrawString(AppL10n.T("Tạo thiết kế mới", "Create Design"), btnFont, btnTxt, new RectangleF(_createCustomBtnBounds.X + 30f, _createCustomBtnBounds.Y, _createCustomBtnBounds.Width - 32f, _createCustomBtnBounds.Height), sf);
            }
        }

        // ==========================================
        // VẼ DANH SÁCH TÀI LIỆU GẦN ĐÂY (RECENT TABLE)
        // ==========================================
        private void DrawRecentFilesTable(Graphics g, float x, float y, float width, Color themeColor)
        {
            _recentRowBounds.Clear();
            _pinBtnBounds.Clear();

            List<RecentDocItem> docs = GetDisplayDocs();
            OfficeAppMode curMode = GetEffectiveMode();

            // Tiêu đề cột
            float colNameW = width * 0.46f;
            float colPathW = width * 0.34f;
            float colDateW = width * 0.20f;

            using (Font headFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush headBr = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString(AppL10n.T("Tên", "Name"), headFont, headBr, x + 44f, y);
                g.DrawString(AppL10n.T("Vị trí", "Location"), headFont, headBr, x + colNameW, y);
                g.DrawString(AppL10n.T("Ngày sửa đổi", "Date Modified"), headFont, headBr, x + colNameW + colPathW, y);
            }
            y += 24f;

            using (Pen divPen = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
            {
                g.DrawLine(divPen, x, y, x + width, y);
            }
            y += 4f;

            if (docs.Count == 0)
            {
                using (Font emptyFont = new Font("Segoe UI", 9.8f, FontStyle.Regular))
                using (SolidBrush emptyBr = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    string modeExtText = curMode switch
                    {
                        OfficeAppMode.Slides => AppL10n.T("trình chiếu (.pptx, .ppt)", "presentation (.pptx, .ppt)"),
                        OfficeAppMode.Writer => AppL10n.T("văn bản (.docx, .doc, .odt, .rtf, .txt)", "document (.docx, .doc, .odt, .rtf, .txt)"),
                        OfficeAppMode.Calc => AppL10n.T("bảng tính (.xlsx, .xls, .ods, .csv)", "spreadsheet (.xlsx, .xls, .ods, .csv)"),
                        OfficeAppMode.Pdf => AppL10n.T("tài liệu PDF (.pdf)", "PDF (.pdf)"),
                        OfficeAppMode.Photo => AppL10n.T("thiết kế & ảnh (.svg, .png, .jpg)", "design & image (.svg, .png, .jpg)"),
                        _ => AppL10n.T("tài liệu", "document")
                    };
                    g.DrawString(AppL10n.T($"Chưa có tệp {modeExtText} nào trong danh sách gần đây.", $"No recent {modeExtText} files yet."), emptyFont, emptyBr, x + 44f, y + 20f);
                }
                return;
            }

            float rowH = 52f;
            using (Font nameFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
            using (Font pathFont = new Font("Segoe UI", 8.4f, FontStyle.Regular))
            using (Font dateFont = new Font("Segoe UI", 8.4f, FontStyle.Regular))
            using (SolidBrush nameBr = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (SolidBrush grayBr = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (SolidBrush hoverRowBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (Pen rowLine = new Pen(Color.FromArgb(241, 245, 249), 1.0f))
            {
                StringFormat ellipsisSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

                for (int r = 0; r < docs.Count; r++)
                {
                    RectangleF rRect = new RectangleF(x, y, width, rowH);
                    _recentRowBounds.Add(rRect);

                    bool isHover = (HoverRecentRowIndex == r);
                    if (isHover)
                    {
                        g.FillRoundedRectangle(hoverRowBg, rRect, 8f);
                    }

                    RecentDocItem item = docs[r];

                    // Biểu tượng ứng dụng 3D sắc nét theo đúng định dạng
                    AppIconRenderer.DrawAppIcon(g, x + 10f, y + 13f, 26f, curMode);

                    // Tên tệp
                    RectangleF nameRect = new RectangleF(x + 44f, y + 17f, colNameW - 50f, 20f);
                    g.DrawString(item.Title ?? "", nameFont, nameBr, nameRect, ellipsisSf);

                    // Đường dẫn thư mục
                    string dir = "";
                    try { dir = Path.GetDirectoryName(item.FilePath) ?? ""; } catch { }
                    if (string.IsNullOrEmpty(dir)) dir = AppL10n.T("Tài liệu", "Documents");
                    RectangleF pathRect = new RectangleF(x + colNameW, y + 17f, colPathW - 16f, 20f);
                    g.DrawString(dir, pathFont, grayBr, pathRect, ellipsisSf);

                    // Ngày sửa đổi
                    RectangleF dateRect = new RectangleF(x + colNameW + colPathW, y + 17f, colDateW - 36f, 20f);
                    g.DrawString(item.LastModified ?? "", dateFont, grayBr, dateRect, ellipsisSf);

                    // Biểu tượng Ghim (Pin 📌)
                    RectangleF pinRect = new RectangleF(x + width - 32f, y + 16f, 20f, 20f);
                    _pinBtnBounds.Add(pinRect);

                    bool isHoverPin = (HoverPinIndex == r);
                    char pinGlyph = item.IsStarred ? '\uE840' : '\uE718';
                    Color pinColor = item.IsStarred ? themeColor : (isHoverPin ? Color.FromArgb(30, 41, 59) : Color.FromArgb(148, 163, 184));
                    if (item.IsStarred || isHover || isHoverPin)
                    {
                        FluentIconRenderer.DrawGlyph(g, pinGlyph, new Rectangle((int)pinRect.X, (int)pinRect.Y, 18, 18), pinColor, 15f);
                    }

                    g.DrawLine(rowLine, x + 10f, y + rowH, x + width - 10f, y + rowH);
                    y += rowH;
                }
            }
        }

        private void DrawSearchBox(Graphics g, RectangleF bounds)
        {
            using (SolidBrush sbBg = new SolidBrush(IsSearchFocused ? Color.White : Color.FromArgb(248, 250, 252)))
            using (Pen sbPen = new Pen(IsSearchFocused ? GetThemeColor() : Color.FromArgb(226, 232, 240), IsSearchFocused ? 1.5f : 1.0f))
            {
                g.FillRoundedRectangle(sbBg, bounds, 15f);
                g.DrawRoundedRectangle(sbPen, bounds, 15f);
            }

            FluentIconRenderer.DrawGlyph(g, '\uE721', new Rectangle((int)bounds.X + 10, (int)bounds.Y + 7, 16, 16), IsSearchFocused ? GetThemeColor() : Color.FromArgb(148, 163, 184), 13f);

            using (Font sFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
            using (SolidBrush sBr = new SolidBrush(string.IsNullOrEmpty(SearchText) ? Color.FromArgb(148, 163, 184) : Color.FromArgb(15, 23, 42)))
            {
                string displayText = string.IsNullOrEmpty(SearchText) ? (IsSearchFocused ? "|" : AppL10n.T("Tìm kiếm tệp gần đây...", "Search recent files...")) : SearchText + (IsSearchFocused ? "|" : "");
                g.DrawString(displayText, sFont, sBr, bounds.X + 30f, bounds.Y + 6f);
            }
        }

        private GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (rect.Width <= 0 || rect.Height <= 0) return path;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ==========================================
        // XỬ LÝ SỰ KIỆN CHUỘT VÀ BÀN PHÍM
        // ==========================================
        public void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // 1. Click nút Quay lại (Back <-)
            if (_backButtonBounds.Contains(e.Location))
            {
                Close();
                return;
            }

            // 2. Click các mục bên Sidebar điều hướng
            for (int i = 0; i < _sidebarItemBounds.Count; i++)
            {
                if (!_sidebarItemBounds[i].IsEmpty && _sidebarItemBounds[i].Contains(e.Location))
                {
                    HandleSidebarAction(i);
                    return;
                }
            }

            // Click Báo lỗi & Góp ý ở cuối Sidebar (trên Tùy chọn)
            if (!_feedbackBtnBounds.IsEmpty && _feedbackBtnBounds.Contains(e.Location))
            {
                using (var dlg = new BugReportDialogForm(_appForm))
                {
                    dlg.ShowDialog(_appForm);
                }
                return;
            }

            // Click Tùy chọn ở cuối Sidebar
            if (!_optionsBtnBounds.IsEmpty && _optionsBtnBounds.Contains(e.Location))
            {
                using (var dlg = new OfficeOptionsDialogForm(_appForm))
                {
                    dlg.ShowDialog(_appForm);
                }
                return;
            }

            // Click các nút chuyển loại tài liệu Mới (Pills: Slides, Word, Excel)
            for (int p = 0; p < _modePillBounds.Count; p++)
            {
                if (_modePillBounds[p].Contains(e.Location))
                {
                    if (p == 0) OverrideNewMode = OfficeAppMode.Slides;
                    else if (p == 1) OverrideNewMode = OfficeAppMode.Writer;
                    else if (p == 2) OverrideNewMode = OfficeAppMode.Calc;
                    else if (p == 3) OverrideNewMode = OfficeAppMode.Photo;
                    SmoothScrollY = 0f;
                    TargetScrollY = 0f;
                    if (_appForm != null) _appForm.Invalidate();
                    return;
                }
            }

            // 3. Click các tab Gần đây / Đã ghim
            if (_recentTabBtnBounds.Contains(e.Location))
            {
                ActiveRecentTab = 0;
                SmoothScrollY = 0f;
                TargetScrollY = 0f;
                return;
            }
            if (_pinnedTabBtnBounds.Contains(e.Location))
            {
                ActiveRecentTab = 1;
                SmoothScrollY = 0f;
                TargetScrollY = 0f;
                return;
            }

            // 4. Click ô Tìm kiếm
            if (_searchBoxBounds.Contains(e.Location))
            {
                IsSearchFocused = true;
                FocusedInputIndex = -1;
                return;
            }
            else
            {
                IsSearchFocused = false;
            }

            // 5. Click Thẻ Mẫu / Presets
            OfficeAppMode curMode = GetEffectiveMode();
            for (int t = 0; t < _templateCardBounds.Count; t++)
            {
                if (_templateCardBounds[t].Contains(e.Location))
                {
                    if (curMode == OfficeAppMode.Slides)
                    {
                        var preset = SlidePresets[t];
                        if (preset.IsCustom)
                        {
                            // "bấm vô chữ tuỳ chỉnh nó mới hiện"
                            ShowCustomSizePanel = !ShowCustomSizePanel;
                            return;
                        }
                        else
                        {
                            // Tạo ngay slide theo tỷ lệ đã chọn
                            CreateNewSlidePresentation(preset.Width, preset.Height, preset.Title);
                            return;
                        }
                    }
                    else if (curMode == OfficeAppMode.Photo)
                    {
                        CreateNewPhotoDocument(t);
                        return;
                    }
                    else if (curMode == OfficeAppMode.Calc)
                    {
                        CreateNewCalcWorkbook(t);
                        return;
                    }
                    else
                    {
                        CreateNewWriterDocument(t);
                        return;
                    }
                }
            }

            // 6. Click bên trong Bảng Tùy chỉnh Kích thước (Custom Size Panel)
            if (ShowCustomSizePanel)
            {
                if (_customWidthBoxBounds.Contains(e.Location))
                {
                    FocusedInputIndex = 0; // Focus ô Chiều rộng
                    return;
                }
                if (_customHeightBoxBounds.Contains(e.Location))
                {
                    FocusedInputIndex = 1; // Focus ô Chiều cao
                    return;
                }

                // Chọn đơn vị tính: px | cm | mm | inch
                string[] units = new string[] { "px", "cm", "mm", "inch" };
                for (int u = 0; u < _unitBtnBounds.Count; u++)
                {
                    if (_unitBtnBounds[u].Contains(e.Location))
                    {
                        CustomUnit = units[u];
                        return;
                    }
                }

                // Nút "Tạo thiết kế mới"
                if (_createCustomBtnBounds.Contains(e.Location))
                {
                    ApplyAndCreateCustomDesign();
                    return;
                }
            }

            // 7. Click Danh sách Tệp gần đây (Mở tệp hoặc Đổi trạng thái Ghim)
            for (int p = 0; p < _pinBtnBounds.Count; p++)
            {
                if (_pinBtnBounds[p].Contains(e.Location))
                {
                    List<RecentDocItem> docs = GetDisplayDocs();
                    if (p < docs.Count)
                    {
                        docs[p].IsStarred = !docs[p].IsStarred;
                        if (_appForm != null && _appForm.HomeView != null)
                        {
                            _appForm.HomeView.SaveRecentDocs();
                        }
                    }
                    return;
                }
            }

            for (int r = 0; r < _recentRowBounds.Count; r++)
            {
                if (_recentRowBounds[r].Contains(e.Location))
                {
                    List<RecentDocItem> docs = GetDisplayDocs();
                    if (r < docs.Count)
                    {
                        OpenRecentDocument(docs[r]);
                    }
                    return;
                }
            }
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            HoverBackButton = _backButtonBounds.Contains(e.Location);

            HoverSidebarIndex = -1;
            for (int i = 0; i < _sidebarItemBounds.Count; i++)
            {
                if (!_sidebarItemBounds[i].IsEmpty && _sidebarItemBounds[i].Contains(e.Location))
                {
                    HoverSidebarIndex = i;
                    break;
                }
            }
            if (!_feedbackBtnBounds.IsEmpty && _feedbackBtnBounds.Contains(e.Location)) HoverSidebarIndex = 101;
            else if (!_optionsBtnBounds.IsEmpty && _optionsBtnBounds.Contains(e.Location)) HoverSidebarIndex = 100;

            HoverModePillIndex = -1;
            for (int p = 0; p < _modePillBounds.Count; p++)
            {
                if (_modePillBounds[p].Contains(e.Location))
                {
                    HoverModePillIndex = p;
                    break;
                }
            }

            HoverTemplateCardIndex = -1;
            for (int t = 0; t < _templateCardBounds.Count; t++)
            {
                if (_templateCardBounds[t].Contains(e.Location))
                {
                    HoverTemplateCardIndex = t;
                    break;
                }
            }

            HoverUnitBtnIndex = -1;
            for (int u = 0; u < _unitBtnBounds.Count; u++)
            {
                if (_unitBtnBounds[u].Contains(e.Location))
                {
                    HoverUnitBtnIndex = u;
                    break;
                }
            }

            HoverCreateCustomBtn = _createCustomBtnBounds.Contains(e.Location);

            HoverRecentRowIndex = -1;
            for (int r = 0; r < _recentRowBounds.Count; r++)
            {
                if (_recentRowBounds[r].Contains(e.Location))
                {
                    HoverRecentRowIndex = r;
                    break;
                }
            }

            HoverPinIndex = -1;
            for (int p = 0; p < _pinBtnBounds.Count; p++)
            {
                if (_pinBtnBounds[p].Contains(e.Location))
                {
                    HoverPinIndex = p;
                    break;
                }
            }
        }

        public void OnMouseWheel(MouseEventArgs e)
        {
            int max = GetMaxScroll(_lastHeight, _lastWidth);
            float step = -e.Delta * 0.65f;
            float diff = TargetScrollY - SmoothScrollY;
            float lead = ((step > 0 && diff > 0) || (step < 0 && diff < 0)) ? Math.Sign(diff) * Math.Min(Math.Abs(diff), 50f) : 0f;
            TargetScrollY = Math.Max(0, Math.Min(max, SmoothScrollY + lead + step));
            UpdateSmoothScroll(_lastHeight, _lastWidth);
        }

        public void OnKeyPress(KeyPressEventArgs e)
        {
            if (IsSearchFocused)
            {
                if (e.KeyChar == '\b')
                {
                    if (SearchText.Length > 0) SearchText = SearchText.Substring(0, SearchText.Length - 1);
                }
                else if (e.KeyChar >= 32)
                {
                    SearchText += e.KeyChar;
                }
                return;
            }

            if (ShowCustomSizePanel && FocusedInputIndex >= 0)
            {
                if (e.KeyChar == '\b')
                {
                    if (FocusedInputIndex == 0 && CustomWidthText.Length > 0)
                        CustomWidthText = CustomWidthText.Substring(0, CustomWidthText.Length - 1);
                    else if (FocusedInputIndex == 1 && CustomHeightText.Length > 0)
                        CustomHeightText = CustomHeightText.Substring(0, CustomHeightText.Length - 1);
                }
                else if (char.IsDigit(e.KeyChar) || e.KeyChar == '.')
                {
                    if (FocusedInputIndex == 0) CustomWidthText += e.KeyChar;
                    else if (FocusedInputIndex == 1) CustomHeightText += e.KeyChar;
                }
            }
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (ShowCustomSizePanel && FocusedInputIndex >= 0)
                {
                    ApplyAndCreateCustomDesign();
                }
            }
        }

        private void HandleSidebarAction(int index)
        {
            switch (index)
            {
                case 0: // Trang chủ
                case 1: // Mới
                    ActiveSidebarTab = index;
                    SmoothScrollY = 0;
                    TargetScrollY = 0;
                    break;
                case 2: // Mở (Open file theo đúng phân hệ đang chọn)
                    {
                        OfficeAppMode openMode = GetEffectiveMode();
                        Close();
                        if (_appForm != null)
                        {
                            if (_appForm.Ribbon != null) _appForm.Ribbon.CurrentMode = openMode;
                            _appForm.PerformQuickOpen();
                        }
                    }
                    break;
                case 4: // Thông tin
                    ActiveSidebarTab = 4;
                    break;
                case 5: // Lưu (Quick Save)
                    Close();
                    if (_appForm != null) _appForm.PerformQuickSave();
                    break;
                case 6: // Lưu dưới dạng (Save As)
                    Close();
                    if (_appForm != null) _appForm.PerformSaveAs();
                    break;
                case 7: // In (Print)
                    Close();
                    if (_appForm != null) _appForm.PerformPrint();
                    break;
                case 8: // Chia sẻ Cloud & Gửi Email (Share / Email)
                    Close();
                    if (_appForm != null)
                    {
                        var shareMenu = new ContextMenuStrip();
                        shareMenu.Items.Add(AppL10n.T("Chia sẻ tới tài khoản ZenQ (200 MB)...", "Share to ZenQ Account (200 MB)..."), null, (ms, me) =>
                        {
                            _appForm.OpenCloudShareDialogForCurrentDoc();
                        });
                        shareMenu.Items.Add(AppL10n.T("📧 Gửi qua ứng dụng Email...", "📧 Send via Email App..."), null, (ms, me) =>
                        {
                            string curPath = null;
                            string docTitle = "Tai_Lieu_ZenQOffice";
                            if (_appForm.Ribbon != null && _appForm.Ribbon.ActiveDocIndex >= 0 && _appForm.Ribbon.ActiveDocIndex < _appForm.Ribbon.DocTabs.Count)
                            {
                                var activeTab = _appForm.Ribbon.DocTabs[_appForm.Ribbon.ActiveDocIndex];
                                curPath = activeTab.FilePath;
                                if (!string.IsNullOrEmpty(curPath))
                                    docTitle = System.IO.Path.GetFileNameWithoutExtension(curPath);
                                else if (!string.IsNullOrEmpty(activeTab.Title))
                                    docTitle = activeTab.Title;
                            }

                            using (var emailDlg = new SendEmailDialogForm(
                                docTitle,
                                curPath,
                                saveTempCopyAction: (tempPath) => _appForm.ExportActiveDocumentForCloudShare(tempPath),
                                exportPdfAction: (pdfPath) =>
                                {
                                    try
                                    {
                                        if (_appForm.Ribbon != null && _appForm.Ribbon.CurrentMode == OfficeAppMode.Writer && _appForm.WriterView != null)
                                            return _appForm.WriterView.ExportToPdf(pdfPath, _appForm.Ribbon);
                                    }
                                    catch { }
                                    return false;
                                }))
                            {
                                emailDlg.ShowDialog(_appForm);
                            }
                        });
                        shareMenu.Show(Cursor.Position);
                    }
                    break;
                case 9: // Xuất (Multi-Format Export)
                    Close();
                    if (_appForm != null) _appForm.PerformExport();
                    break;
                case 10: // Bản sao lưu tệp gốc (Safety Vault)
                    Close();
                    if (_appForm != null) _appForm.OpenBackupManagerDialog();
                    break;
                case 11: // Font & Tiếng Việt (Trích xuất & Sửa lỗi Font)
                    Close();
                    {
                        string curDocPath = null;
                        if (_appForm != null && _appForm.Ribbon != null && _appForm.Ribbon.ActiveDocIndex >= 0 && _appForm.Ribbon.ActiveDocIndex < _appForm.Ribbon.DocTabs.Count)
                        {
                            curDocPath = _appForm.Ribbon.DocTabs[_appForm.Ribbon.ActiveDocIndex].FilePath;
                        }
                        using (var dlg = new VietnameseFontToolkitForm(curDocPath))
                        {
                            dlg.ShowDialog(_appForm);
                        }
                    }
                    break;
                case 13: // Đóng
                    Close();
                    if (_appForm != null)
                    {
                        if (_appForm.Ribbon != null && _appForm.Ribbon.DocTabs.Count > 1)
                        {
                            _appForm.Ribbon.CloseDocTab(_appForm.Ribbon.ActiveDocIndex, _appForm.SlidesView, _appForm.WriterView, _appForm.CalcView, _appForm.PdfView);
                        }
                        else
                        {
                            _appForm.Close();
                        }
                    }
                    break;
            }
        }

        private void ApplyAndCreateCustomDesign()
        {
            float w = 1920f;
            float h = 1080f;
            float.TryParse(CustomWidthText, out w);
            float.TryParse(CustomHeightText, out h);
            if (w <= 10f) w = 1920f;
            if (h <= 10f) h = 1080f;

            // Chuyển đổi đơn vị tính sang Pixel chuẩn 96 DPI
            if (string.Equals(CustomUnit, "cm", StringComparison.OrdinalIgnoreCase))
            {
                w = (float)Math.Round(w * 37.795f);
                h = (float)Math.Round(h * 37.795f);
            }
            else if (string.Equals(CustomUnit, "mm", StringComparison.OrdinalIgnoreCase))
            {
                w = (float)Math.Round(w * 3.7795f);
                h = (float)Math.Round(h * 3.7795f);
            }
            else if (string.Equals(CustomUnit, "inch", StringComparison.OrdinalIgnoreCase))
            {
                w = (float)Math.Round(w * 96f);
                h = (float)Math.Round(h * 96f);
            }

            CreateNewSlidePresentation(w, h, $"Trình chiếu Tùy chỉnh {CustomWidthText}x{CustomHeightText}{CustomUnit}.pptx");
        }

        public void CreateNewSlidePresentation(float canvasW, float canvasH, string title = "Bản trình chiếu Mới.pptx")
        {
            if (_appForm == null || _appForm.Ribbon == null) return;

            var pres = new PresentationRoot
            {
                CanvasWidth = canvasW,
                CanvasHeight = canvasH,
                IsWidescreen = (Math.Abs(canvasW / Math.Max(1f, canvasH) - (16f / 9f)) < 0.05f),
                Title = title
            };

            // Tạo duy nhất 1 trang trắng không chữ không hiệu ứng cho tất cả các kiểu
            pres.Slides.Clear();
            var slide = new SlideNode
            {
                Title = "Trang 1",
                Layout = SlideLayoutType.Blank,
                BgColor = 0xFFFFFF,
                Shapes = new List<ShapeNode>(),
                Transition = TransitionType.None,
                TransitionDuration = 0f
            };
            pres.Slides.Add(slide);

            _appForm.Ribbon.AddDocTab(title, OfficeAppMode.Slides, _appForm.SlidesView, _appForm.WriterView, _appForm.CalcView, _appForm.PdfView, _appForm.PhotoView);
            int activeIdx = _appForm.Ribbon.ActiveDocIndex;
            if (activeIdx >= 0 && activeIdx < _appForm.Ribbon.DocTabs.Count)
            {
                _appForm.Ribbon.DocTabs[activeIdx].PresentationData = pres;
                _appForm.Ribbon.DocTabs[activeIdx].DraftFile = "";
            }

            if (_appForm.SlidesView != null)
            {
                _appForm.SlidesView.Presentation = pres;
                _appForm.SlidesView.ActiveSlideIndex = 0;
                _appForm.SlidesView.ZoomLevel = 1.0f;
                _appForm.SlidesView.CanvasScrollX = 0;
                _appForm.SlidesView.CanvasScrollY = 0;
                _appForm.SlidesView.SelectedShapeIndex = -1;
                _appForm.SlidesView.InvalidateAllThumbnails();
            }

            _appForm.SetModeIcon(OfficeAppMode.Slides);
            _appForm.PositionAiConfigPanel();
            Close();
        }

        private void CreateNewWriterDocument(int templateIndex)
        {
            if (_appForm == null || _appForm.Ribbon == null) return;
            string docTitle = "Văn bản Mới.docx";
            string openPath = null;

            if (templateIndex == 1)
            {
                openPath = null;
                if (!string.IsNullOrEmpty(openPath)) docTitle = "Mẫu - Đơn xin nghỉ phép.docx";
            }
            else if (templateIndex == 2)
            {
                openPath = null;
                if (!string.IsNullOrEmpty(openPath)) docTitle = "Mẫu - Báo cáo tiến độ dự án.docx";
            }

            _appForm.Ribbon.AddDocTab(docTitle, OfficeAppMode.Writer, _appForm.SlidesView, _appForm.WriterView, _appForm.CalcView, _appForm.PdfView, _appForm.PhotoView);
            int activeIdx = _appForm.Ribbon.ActiveDocIndex;
            if (activeIdx >= 0 && activeIdx < _appForm.Ribbon.DocTabs.Count)
            {
                _appForm.Ribbon.DocTabs[activeIdx].DraftFile = "";
                if (!string.IsNullOrEmpty(openPath))
                {
                    _appForm.Ribbon.DocTabs[activeIdx].FilePath = openPath;
                    if (_appForm.WriterView != null) _appForm.WriterView.LoadDocumentFile(openPath, _appForm.Ribbon);
                }
            }
            try { SessionManager.SaveSessionMetadata(_appForm); } catch { }
            _appForm.SetModeIcon(OfficeAppMode.Writer);
            _appForm.PositionAiConfigPanel();
            Close();
        }

        private void CreateNewCalcWorkbook(int templateIndex)
        {
            if (_appForm == null || _appForm.Ribbon == null) return;
            string docTitle = "Bảng tính Mới.xlsx";
            string openPath = null;

            if (templateIndex == 3)
            {
                openPath = null;
                if (!string.IsNullOrEmpty(openPath)) docTitle = "Mẫu - Bảng chấm công.csv";
            }

            _appForm.Ribbon.AddDocTab(docTitle, OfficeAppMode.Calc, _appForm.SlidesView, _appForm.WriterView, _appForm.CalcView, _appForm.PdfView, _appForm.PhotoView);
            int activeIdx = _appForm.Ribbon.ActiveDocIndex;
            if (activeIdx >= 0 && activeIdx < _appForm.Ribbon.DocTabs.Count)
            {
                _appForm.Ribbon.DocTabs[activeIdx].DraftFile = "";
                if (!string.IsNullOrEmpty(openPath))
                {
                    _appForm.Ribbon.DocTabs[activeIdx].FilePath = openPath;
                    if (_appForm.CalcView != null) Format.XlsxHandler.LoadXlsxOrCsv(openPath, _appForm.CalcView);
                }
            }
            _appForm.SetModeIcon(OfficeAppMode.Calc);
            _appForm.PositionAiConfigPanel();
            Close();
        }

        private void CreateNewPhotoDocument(int templateIndex)
        {
            if (_appForm == null || _appForm.Ribbon == null) return;
            string docTitle = "Thiết kế Mới.svg";
            int w = 1920, h = 1080;
            if (templateIndex == 1) { w = 1080; h = 1080; docTitle = "Thiết kế Vuông 1080x1080.svg"; }
            else if (templateIndex == 2) { w = 1280; h = 720; docTitle = "Thiết kế HD 1280x720.svg"; }
            else if (templateIndex == 3) { w = 3840; h = 2160; docTitle = "Áp phích Poster 4K.svg"; }

            _appForm.Ribbon.AddDocTab(docTitle, OfficeAppMode.Photo, _appForm.SlidesView, _appForm.WriterView, _appForm.CalcView, _appForm.PdfView, _appForm.PhotoView);
            int activeIdx = _appForm.Ribbon.ActiveDocIndex;
            if (activeIdx >= 0 && activeIdx < _appForm.Ribbon.DocTabs.Count)
            {
                _appForm.Ribbon.DocTabs[activeIdx].DraftFile = "";
                _appForm.Ribbon.DocTabs[activeIdx].PhotoData = new PhotoDocument(w, h, docTitle);
            }
            if (_appForm.PhotoView != null)
            {
                _appForm.PhotoView.Document = _appForm.Ribbon.DocTabs[activeIdx].PhotoData;
                _appForm.PhotoView.CenterCanvas(_appForm.ClientSize.Width, _appForm.ClientSize.Height);
            }
            _appForm.SetModeIcon(OfficeAppMode.Photo);
            _appForm.PositionAiConfigPanel();
            Close();
        }

        private void OpenRecentDocument(RecentDocItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath) || !File.Exists(item.FilePath))
            {
                ZenQOffice.UI.FluentMessageBox.Show(AppL10n.T("Tệp này không tồn tại hoặc đã bị di chuyển:\n", "This file does not exist or has been moved:\n") + (item?.FilePath ?? ""), AppL10n.T("Không tìm thấy tệp", "File Not Found"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_appForm == null || _appForm.Ribbon == null) return;

            OfficeAppMode targetMode = GetEffectiveMode();
            if (!IsItemMatchingMode(item, targetMode))
            {
                return;
            }
            item.Mode = targetMode;

            _appForm.Ribbon.AddDocTab(item.Title, targetMode, _appForm.SlidesView, _appForm.WriterView, _appForm.CalcView, _appForm.PdfView, _appForm.PhotoView);
            int activeIdx = _appForm.Ribbon.ActiveDocIndex;
            if (activeIdx >= 0 && activeIdx < _appForm.Ribbon.DocTabs.Count)
            {
                _appForm.Ribbon.DocTabs[activeIdx].FilePath = item.FilePath;
                _appForm.Ribbon.DocTabs[activeIdx].DraftFile = "";
            }
            try { SessionManager.SaveSessionMetadata(_appForm); } catch { }

            if (item.Mode == OfficeAppMode.Writer && _appForm.WriterView != null)
            {
                _appForm.WriterView.LoadDocumentFile(item.FilePath, _appForm.Ribbon);
            }
            else if (item.Mode == OfficeAppMode.Slides && _appForm.SlidesView != null)
            {
                _appForm.SlidesView.Presentation = Format.PptxHandler.LoadPptx(item.FilePath);
                _appForm.SlidesView.ActiveSlideIndex = 0;
                _appForm.SlidesView.ZoomLevel = 1.0f;
                _appForm.SlidesView.CanvasScrollX = 0;
                _appForm.SlidesView.CanvasScrollY = 0;
                _appForm.SlidesView.SelectedShapeIndex = -1;
                if (activeIdx >= 0 && activeIdx < _appForm.Ribbon.DocTabs.Count)
                {
                    _appForm.Ribbon.DocTabs[activeIdx].PresentationData = _appForm.SlidesView.Presentation;
                }
            }
            else if (item.Mode == OfficeAppMode.Calc && _appForm.CalcView != null)
            {
                Format.XlsxHandler.LoadXlsxOrCsv(item.FilePath, _appForm.CalcView);
            }
            else if (item.Mode == OfficeAppMode.Pdf && _appForm.PdfView != null)
            {
                _appForm.PdfView.Document = Format.PdfHandler.LoadPdf(item.FilePath);
                _appForm.PdfView.ActivePageIndex = 0;
            }
            else if (item.Mode == OfficeAppMode.Photo && _appForm.PhotoView != null)
            {
                _appForm.PhotoView.Document = PhotoDocument.LoadFromFile(item.FilePath);
                _appForm.PhotoView.CenterCanvas(_appForm.ClientSize.Width, _appForm.ClientSize.Height);
            }

            _appForm.SetModeIcon(item.Mode);
            _appForm.PositionAiConfigPanel();
            Close();
        }
    }
}

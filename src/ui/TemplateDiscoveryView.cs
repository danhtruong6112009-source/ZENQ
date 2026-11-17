using System;

using System.Collections.Generic;

using System.Linq;

using System.Drawing;

using System.Drawing.Drawing2D;

using System.IO;

using System.Windows.Forms;

using ZenQOffice.Core;



namespace ZenQOffice.UI

{

    public class TemplateDiscoveryView

    {

        public float ScrollY { get; set; } = 0f;

        public float TargetScrollY { get; set; } = 0f;

        public float SmoothScrollY { get; set; } = 0f;

        public float MaxScrollY { get; private set; } = 0f;

        private long _scrollAnimLastTs = 0;

        private float _clipViewTopY = 0f;

        private float _clipViewBottomY = 4000f;



        private string _searchQuery = "";

        public string SearchQuery

        {

            get => _searchQuery;

            set

            {

                string val = value ?? "";

                if (_searchQuery != val)

                {

                    _searchQuery = val;

                    SearchCaretIndex = Math.Max(0, Math.Min(SearchCaretIndex, _searchQuery.Length));

                    SearchSelectionStart = Math.Max(0, Math.Min(SearchSelectionStart, _searchQuery.Length));

                }

            }

        }



        public bool IsSearchFocused { get; set; } = false;

        public int SearchCaretIndex { get; set; } = 0;

        public int SearchSelectionStart { get; set; } = 0;

        public bool IsDraggingSearchSelection { get; set; } = false;

        private readonly List<float> _searchCharXOffsets = new List<float>();



        public string ActiveCategory { get; set; } = "Tất cả";



        // Hover & Interaction states

        public int HoveredCardIndex { get; private set; } = -1;

        public int HoveredCategoryIndex { get; private set; } = -1;

        public int HoveredQuickCardIndex { get; private set; } = -1;

        public bool HoverBackButton { get; private set; } = false;

        public bool HoverUploadButton { get; private set; } = false;

        public bool HoverReloadButton { get; private set; } = false;

        public bool HoverRetryOfflineButton { get; private set; } = false;



        // Timing states for Hover Carousel (Sequential all-slides, 1.0s per slide, no page skipping)

        private int _activeCarouselCardIndex = -1;

        private SlideTemplateItem _activeCarouselTemplate = null;

        private int _carouselCurSlide = 0;

        private float _carouselTransitionProgress = 0f; // 0.0 = holding on _carouselCurSlide, 0.0 -> 1.0 = sliding to nextSlide

        private DateTime _carouselSlideHoldStart = DateTime.MinValue;

        private DateTime _carouselTransitionStart = DateTime.MinValue;

        private float _carouselScrollOffset = 0f;

        private int _displayedSlideIndex = 0;

        private bool _hasUnrenderedCards = false;

        private bool _lastBlinkState = false;



        // Canva-style Template Preview Modal States

        public SlideTemplateItem PreviewModalTemplate { get; set; } = null;

        public int PreviewModalSlideIndex { get; set; } = 0;

        public float PreviewModalScrollY { get; set; } = 0f;

        public float PreviewModalMaxScrollY { get; private set; } = 0f;

        public float PreviewModalThumbScrollX = 0f;

        public float PreviewModalThumbTargetScrollX = 0f;

        public float PreviewModalThumbMaxScrollX = 0f;

        public RectangleF PreviewModalBoxRect = RectangleF.Empty;

        public RectangleF PreviewModalCloseBtnRect = RectangleF.Empty;

        public RectangleF PreviewModalUseBtnRect = RectangleF.Empty;
        public RectangleF PreviewModalDeleteBtnRect = RectangleF.Empty;
        public bool HoverModalDeleteBtn = false;

        public RectangleF PreviewModalThumbLeftBtnRect = RectangleF.Empty;

        public RectangleF PreviewModalThumbRightBtnRect = RectangleF.Empty;

        public bool HoverModalCloseBtn { get; private set; } = false;

        public bool HoverModalUseBtn { get; private set; } = false;

        public bool HoverModalThumbLeftBtn { get; private set; } = false;

        public bool HoverModalThumbRightBtn { get; private set; } = false;

        public int HoveredModalThumbIndex { get; private set; } = -1;

        public int HoveredModalRelatedIndex { get; private set; } = -1;

        private readonly List<RectangleF> _modalThumbRects = new List<RectangleF>();

        private readonly List<RectangleF> _modalRelatedCardRects = new List<RectangleF>();

        private readonly List<SlideTemplateItem> _modalRelatedCardItems = new List<SlideTemplateItem>();



        // Interactive layout rectangles

        public RectangleF BackButtonRect { get; private set; } = RectangleF.Empty;

        public RectangleF SearchBoxRect { get; private set; } = RectangleF.Empty;

        public RectangleF UploadBtnRect { get; private set; } = RectangleF.Empty;

        public RectangleF ReloadBtnRect { get; private set; } = RectangleF.Empty;

        public RectangleF RetryOfflineBtnRect { get; private set; } = RectangleF.Empty;



        private readonly List<RectangleF> _categoryPillRects = new List<RectangleF>();

        private readonly List<RectangleF> _quickCardRects = new List<RectangleF>();

        private readonly List<RectangleF> _templateCardRects = new List<RectangleF>();

        private readonly List<SlideTemplateItem> _templateCardItems = new List<SlideTemplateItem>();

        private readonly List<RectangleF> _templateUseBtnRects = new List<RectangleF>();



        public Func<List<RecentDocItem>> RecentDocsProvider { get; set; }



        // Horizontal Carousel State: Mẫu thịnh hành (Trending)

        public float TrendingScrollX = 0f;

        public float TrendingTargetScrollX = 0f;

        public float TrendingMaxScrollX = 0f;

        public RectangleF TrendingLeftBtnRect = RectangleF.Empty;

        public RectangleF TrendingRightBtnRect = RectangleF.Empty;

        public bool HoverTrendingLeftBtn = false;

        public bool HoverTrendingRightBtn = false;



        // Horizontal Carousel State: Dành cho bạn (For You)

        public float ForYouScrollX = 0f;

        public float ForYouTargetScrollX = 0f;

        public float ForYouMaxScrollX = 0f;

        public RectangleF ForYouLeftBtnRect = RectangleF.Empty;

        public RectangleF ForYouRightBtnRect = RectangleF.Empty;

        public bool HoverForYouLeftBtn = false;

        public bool HoverForYouRightBtn = false;



        private readonly string[] _categories = new string[]

        {

            "Tất cả", "Thuyết trình", "Kinh doanh", "Tài chính", "Thương hiệu", "Giáo dục", "Marketing"

        };



        private readonly struct QuickCategoryCard

        {

            public readonly string Title;

            public readonly string Subtitle;

            public readonly Color BgColor1;

            public readonly Color BgColor2;

            public readonly string CategoryFilter;



            public QuickCategoryCard(string title, string sub, Color c1, Color c2, string cat)

            {

                Title = title;

                Subtitle = sub;

                BgColor1 = c1;

                BgColor2 = c2;

                CategoryFilter = cat;

            }

        }



        private readonly QuickCategoryCard[] _quickCards = new QuickCategoryCard[]

        {

            new QuickCategoryCard("Bài thuyết trình", "Chuẩn 16:9 sắc nét", Color.FromArgb(234, 88, 12), Color.FromArgb(249, 115, 22), "Thuyết trình"),

            new QuickCategoryCard("Kế hoạch kinh doanh", "Pitch Deck gọi vốn", Color.FromArgb(79, 70, 229), Color.FromArgb(99, 102, 241), "Kinh doanh"),

            new QuickCategoryCard("Báo cáo tài chính", "Quản trị dòng tiền & KPI", Color.FromArgb(5, 150, 105), Color.FromArgb(16, 185, 129), "Tài chính"),

            new QuickCategoryCard("Hồ sơ năng lực", "Thương hiệu & Đội ngũ", Color.FromArgb(37, 99, 235), Color.FromArgb(59, 130, 246), "Thương hiệu"),

            new QuickCategoryCard("Bài giảng giáo dục", "Khóa học trực quan", Color.FromArgb(124, 58, 237), Color.FromArgb(139, 92, 246), "Giáo dục")

        };



        public TemplateDiscoveryView()

        {

            OnlineSlideTemplateManager.Initialize();

        }



        public bool UpdateAnimation()

        {

            bool needsRedraw = false;



            // Nhấp nháy con trỏ soạn thảo trong ô tìm kiếm

            if (IsSearchFocused)

            {

                bool curBlink = ((DateTime.UtcNow.Ticks / 10000) / 500) % 2 == 0;

                if (curBlink != _lastBlinkState)

                {

                    _lastBlinkState = curBlink;

                    needsRedraw = true;

                }

            }



            // 1. Frame-rate independent silky vertical & horizontal smooth scroll

            double dt = FluentMotion.DeltaSeconds(ref _scrollAnimLastTs);

            float scrollBlend = FluentMotion.DampFactor(dt, 0.046);



            float smoothY = SmoothScrollY;

            if (FluentMotion.StepAxis(ref smoothY, TargetScrollY, scrollBlend, 0.35f))

            {

                SmoothScrollY = smoothY;

                ScrollY = smoothY;

                needsRedraw = true;

            }



            // Hoạt ảnh quét ánh sáng Shimmer cho các Khung Xương (Skeleton) đang chờ render

            if (_hasUnrenderedCards)

            {

                needsRedraw = true;

            }



            // Smooth horizontal scroll cho Mẫu thịnh hành (Trending)

            float trendX = TrendingScrollX;

            if (FluentMotion.StepAxis(ref trendX, TrendingTargetScrollX, scrollBlend, 0.4f))

            {

                TrendingScrollX = trendX;

                needsRedraw = true;

            }



            // Smooth horizontal scroll cho Dành cho bạn (For You)

            float fyX = ForYouScrollX;

            if (FluentMotion.StepAxis(ref fyX, ForYouTargetScrollX, scrollBlend, 0.4f))

            {

                ForYouScrollX = fyX;

                needsRedraw = true;

            }



            if (SmoothScrollY == TargetScrollY && TrendingScrollX == TrendingTargetScrollX && ForYouScrollX == ForYouTargetScrollX)

            {

                _scrollAnimLastTs = 0;

            }



            // Smooth horizontal scroll cho Slide Thumbnails trong Modal Preview

            if (Math.Abs(PreviewModalThumbTargetScrollX - PreviewModalThumbScrollX) > 0.8f)

            {

                PreviewModalThumbScrollX += (PreviewModalThumbTargetScrollX - PreviewModalThumbScrollX) * 0.28f;

                needsRedraw = true;

            }

            else if (PreviewModalThumbScrollX != PreviewModalThumbTargetScrollX)

            {

                PreviewModalThumbScrollX = PreviewModalThumbTargetScrollX;

                needsRedraw = true;

            }



            // 2. Hover Carousel Timing Animation (1.0s đổi 1 slide tuần tự, không bao giờ nhảy trang)

            if (_activeCarouselCardIndex != -1 && _activeCarouselTemplate != null)

            {

                int slideCount = Math.Max(1, _activeCarouselTemplate.slideCount);

                float prevOffset = _carouselScrollOffset;

                int prevDisp = _displayedSlideIndex;



                if (slideCount > 1)

                {

                    int nextSlide = (_carouselCurSlide + 1) % slideCount;

                    if (_carouselTransitionProgress <= 0f)

                    {

                        double holdSec = (DateTime.UtcNow - _carouselSlideHoldStart).TotalSeconds;

                        // Giữ đúng 0.74s + 0.26s trượt = 1.0s đổi 1 trang

                        if (holdSec >= 0.74)

                        {

                            // Chỉ chuyển sang trang kế tiếp khi ảnh trang kế tiếp đã sẵn sàng trong bộ nhớ (tránh nhảy cóc từ trang 1 sang trang 3)

                            var bmpNext = OnlineSlideTemplateManager.GetSlideBitmap(_activeCarouselTemplate.id, nextSlide);

                            if (bmpNext != null)

                            {

                                _carouselTransitionStart = DateTime.UtcNow;

                                _carouselTransitionProgress = 0.001f;

                            }

                            else

                            {

                                OnlineSlideTemplateManager.QueueSlideRender(_activeCarouselTemplate.id, nextSlide);

                            }

                        }

                    }



                    if (_carouselTransitionProgress > 0f)

                    {

                        float t = (float)(DateTime.UtcNow - _carouselTransitionStart).TotalSeconds / 0.26f;

                        if (t >= 1.0f)

                        {

                            _carouselCurSlide = nextSlide;

                            _carouselTransitionProgress = 0f;

                            _carouselSlideHoldStart = DateTime.UtcNow;

                            _carouselScrollOffset = _carouselCurSlide;

                            _displayedSlideIndex = _carouselCurSlide;

                        }

                        else

                        {

                            float eased = EaseInOutQuad(Math.Max(0f, Math.Min(1f, t)));

                            _carouselTransitionProgress = eased;

                            _carouselScrollOffset = _carouselCurSlide + eased;

                            _displayedSlideIndex = (eased < 0.5f) ? _carouselCurSlide : nextSlide;

                        }

                        needsRedraw = true;

                    }

                    else

                    {

                        _carouselScrollOffset = _carouselCurSlide;

                        _displayedSlideIndex = _carouselCurSlide;

                    }

                }

                else

                {

                    _carouselScrollOffset = 0f;

                    _displayedSlideIndex = 0;

                }



                if (Math.Abs(prevOffset - _carouselScrollOffset) > 0.001f || prevDisp != _displayedSlideIndex)

                {

                    needsRedraw = true;

                }

            }

            else

            {

                if (_carouselScrollOffset != 0f || _displayedSlideIndex != 0)

                {

                    _carouselCurSlide = 0;

                    _carouselTransitionProgress = 0f;

                    _carouselScrollOffset = 0f;

                    _displayedSlideIndex = 0;

                    needsRedraw = true;

                }

            }



            return needsRedraw;

        }



        private static float EaseInOutQuad(float t)

        {

            return t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;

        }



        private static float EaseInOutCubic(float t)

        {

            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;

        }



        public void Draw(D2DGraphics g, float x, float y, float width, float height, int mouseX, int mouseY)

        {

            if (width <= 0 || height <= 0) return;



            // 1. RENDER TOP NAVIGATION BAR (Cố định ở đỉnh vùng trung tâm)

            float topBarH = 68f;

            DrawTopNavBar(g, x, y, width, topBarH, mouseX, mouseY);



            // 2. SCROLLABLE CONTENT VIEWPORT

            float viewY = y + topBarH;

            float viewH = height - topBarH;

            if (viewH <= 0) return;



            _clipViewTopY = viewY;

            _clipViewBottomY = viewY + viewH;

            g.PushClip(new RectangleF(x, viewY, width, viewH));



            float scrollPixelY = MathF.Round(SmoothScrollY);

            float curY = viewY - scrollPixelY;



            // 2.1 Section Title & Upload Action Bar

            curY = DrawSectionHeader(g, x + 24f, curY + 20f, width - 48f, mouseX, mouseY);



            // 2.4 Main Templates Responsive Grid

            curY = DrawTemplatesGrid(g, x + 24f, curY + 16f, width - 48f, mouseX, mouseY);



            g.PopClip();



            // 3. Cập nhật giới hạn cuộn (MaxScrollY) & Vẽ thanh cuộn Direct2D nếu cần

            float totalContentH = (curY + SmoothScrollY) - viewY + 40f;

            MaxScrollY = Math.Max(0f, totalContentH - viewH);

            if (TargetScrollY > MaxScrollY) TargetScrollY = MaxScrollY;

            if (TargetScrollY < 0f) TargetScrollY = 0f;



            if (MaxScrollY > 0)

            {

                DrawScrollbar(g, x + width - 10f, viewY, 6f, viewH, viewH / totalContentH, SmoothScrollY / MaxScrollY);

            }



            // 4. VẼ HỘP THOẠI XEM TRƯỚC MẪU PHONG CÁCH CANVA (Nếu người dùng bấm vào mẫu)

            if (PreviewModalTemplate != null)

            {

                DrawTemplatePreviewModal(g, x, y, width, height, mouseX, mouseY);

            }

        }



        private void RebuildSearchCharOffsets(D2DGraphics g, Font sf, float sx)

        {

            _searchCharXOffsets.Clear();

            _searchCharXOffsets.Add(sx);

            if (string.IsNullOrEmpty(_searchQuery)) return;



            for (int i = 1; i <= _searchQuery.Length; i++)

            {

                string sub = _searchQuery.Substring(0, i);

                SizeF sz = g.MeasureString(sub, sf);

                _searchCharXOffsets.Add(sx + sz.Width);

            }

        }



        public int GetSearchCharIndexAtX(float mouseX)

        {

            float sx = SearchBoxRect.X + 38f;

            if (string.IsNullOrEmpty(_searchQuery)) return 0;



            if (_searchCharXOffsets.Count == _searchQuery.Length + 1)

            {

                if (mouseX <= _searchCharXOffsets[0]) return 0;

                for (int i = 0; i < _searchQuery.Length; i++)

                {

                    float mid = (_searchCharXOffsets[i] + _searchCharXOffsets[i + 1]) * 0.5f;

                    if (mouseX < mid) return i;

                }

                return _searchQuery.Length;

            }



            // Fallback ước lượng nếu chưa kịp đo trên D2D

            float rel = mouseX - sx;

            if (rel <= 0) return 0;

            int idx = (int)Math.Round(rel / 7.2f);

            return Math.Max(0, Math.Min(_searchQuery.Length, idx));

        }



        private void DrawTopNavBar(D2DGraphics g, float x, float y, float width, float height, int mouseX, int mouseY)

        {

            // Phông nền thanh công cụ mờ đục phong cách Fluent Glass

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(250, 252, 255)))

            using (Pen borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))

            {

                g.FillRectangle(bg, x, y, width, height);

                g.DrawLine(borderPen, x, y + height - 1f, x + width, y + height - 1f);

            }



            float midY = y + (height - 38f) / 2f;



            // 1. Mũi tên trở về DUY NHẤT (Nút tròn 38x38 phong cách tối giản, CHỈ có mũi tên ←, không có chữ)

            BackButtonRect = new RectangleF(x + 20f, midY, 38f, 38f);

            using (SolidBrush btnBg = new SolidBrush(HoverBackButton ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252)))

            using (Pen btnPen = new Pen(HoverBackButton ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), HoverBackButton ? 1.4f : 1f))

            {

                g.FillRoundedRectangle(btnBg, BackButtonRect, 19f);

                g.DrawRoundedRectangle(btnPen, BackButtonRect, 19f);

            }

            Color arrowColor = HoverBackButton ? Color.FromArgb(79, 70, 229) : Color.FromArgb(51, 65, 85);

            FluentIconRenderer.DrawGlyphD2D(g, '\uE72B', BackButtonRect, arrowColor, 13f);



            // 2. Hộp tìm kiếm mẫu DUY NHẤT trên thanh trên cùng (Search Box)

            float searchX = BackButtonRect.Right + 14f;

            float searchW = width - (searchX - x) - 24f;

            SearchBoxRect = new RectangleF(searchX, midY, searchW, 38f);



            using (SolidBrush sbBg = new SolidBrush(IsSearchFocused ? Color.White : Color.FromArgb(248, 250, 252)))

            using (Pen sbPen = new Pen(IsSearchFocused ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), IsSearchFocused ? 1.6f : 1f))

            {

                g.FillRoundedRectangle(sbBg, SearchBoxRect, 19f);

                g.DrawRoundedRectangle(sbPen, SearchBoxRect, 19f);

            }

            using (Font sf = new Font("Segoe UI", 9.2f, FontStyle.Regular))

            {

                float sx = SearchBoxRect.X + 38f;

                float sy = SearchBoxRect.Y + 9f;



                RebuildSearchCharOffsets(g, sf, sx);



                int caretIdx = Math.Max(0, Math.Min(SearchCaretIndex, _searchQuery.Length));

                int selStartIdx = Math.Max(0, Math.Min(SearchSelectionStart, _searchQuery.Length));



                if (string.IsNullOrEmpty(_searchQuery))

                {

                    if (!IsSearchFocused)

                    {

                        using (SolidBrush ph = new SolidBrush(Color.FromArgb(148, 163, 184)))

                        {

                            g.DrawString(AppL10n.T("Tìm kiếm theo tên mẫu, mã ID hoặc từ khóa (tags)...", "Search by template name, ID, or tags..."), sf, ph, sx, sy);

                        }

                    }

                    else

                    {

                        using (SolidBrush ph = new SolidBrush(Color.FromArgb(180, 190, 205)))

                        {

                            g.DrawString(AppL10n.T("Nhập tên mẫu, mã ID hoặc từ khóa...", "Enter template name, ID, or keyword..."), sf, ph, sx, sy);

                        }

                    }

                }

                else

                {

                    // Vẽ vùng bôi đen (quét khối văn bản) nếu có

                    if (IsSearchFocused && caretIdx != selStartIdx && _searchCharXOffsets.Count == _searchQuery.Length + 1)

                    {

                        int minSel = Math.Min(caretIdx, selStartIdx);

                        int maxSel = Math.Max(caretIdx, selStartIdx);

                        float selX1 = _searchCharXOffsets[minSel];

                        float selX2 = _searchCharXOffsets[maxSel];

                        float selW = Math.Max(2f, selX2 - selX1);

                        using (SolidBrush selBrush = new SolidBrush(Color.FromArgb(75, 99, 102, 241)))

                        {

                            g.FillRectangle(selBrush, selX1, SearchBoxRect.Y + 7f, selW, 24f);

                        }

                    }



                    using (SolidBrush tx = new SolidBrush(Color.FromArgb(15, 23, 42)))

                    {

                        g.DrawString(_searchQuery, sf, tx, sx, sy);

                    }

                }



                // Vẽ con nháy (Caret) nhấp nháy tại đúng vị trí di chuyển

                if (IsSearchFocused)

                {

                    bool blinkOn = ((DateTime.UtcNow.Ticks / 10000) / 500) % 2 == 0 || IsDraggingSearchSelection;

                    if (blinkOn)

                    {

                        float caretX = sx;

                        if (caretIdx > 0 && caretIdx < _searchCharXOffsets.Count)

                        {

                            caretX = _searchCharXOffsets[caretIdx];

                        }

                        using (Pen caretPen = new Pen(Color.FromArgb(79, 70, 229), 1.6f))

                        {

                            g.DrawLine(caretPen, caretX, SearchBoxRect.Y + 9f, caretX, SearchBoxRect.Y + 29f);

                        }

                    }

                }



                // Biểu tượng kính lúp Fluent Windows 11

                FluentIconRenderer.DrawGlyphD2D(g, '\uE721', new RectangleF(SearchBoxRect.X + 12f, SearchBoxRect.Y + 10f, 18f, 18f),

                    IsSearchFocused ? Color.FromArgb(79, 70, 229) : Color.FromArgb(100, 116, 139), 12f);



                // Nút xóa tìm kiếm nếu có nội dung

                if (!string.IsNullOrEmpty(_searchQuery))

                {

                    FluentIconRenderer.DrawGlyphD2D(g, '\uE711', new RectangleF(SearchBoxRect.Right - 26f, SearchBoxRect.Y + 11f, 16f, 16f), Color.FromArgb(148, 163, 184), 10f);

                }

            }

        }



        private float DrawCategoryPillsRow(D2DGraphics g, float x, float y, float width, int mouseX, int mouseY)

        {

            _categoryPillRects.Clear();

            float curX = x;

            float pillH = 34f;



            using (Font pf = new Font("Segoe UI", 9.2f, FontStyle.Bold))

            {

                for (int i = 0; i < _categories.Length; i++)

                {

                    string cat = _categories[i];

                    bool isActive = (ActiveCategory == cat);

                    bool isHover = (HoveredCategoryIndex == i);



                    string catDisp = AppL10n.TrCategory(cat);
                    float textW = (catDisp.Length * 9.5f) + 28f;

                    RectangleF r = new RectangleF(curX, y, textW, pillH);

                    _categoryPillRects.Add(r);



                    Color bgCol = isActive ? Color.FromArgb(79, 70, 229) : (isHover ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252));

                    Color borderCol = isActive ? Color.FromArgb(79, 70, 229) : (isHover ? Color.FromArgb(165, 180, 252) : Color.FromArgb(226, 232, 240));

                    Color textCol = isActive ? Color.White : (isHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(71, 85, 105));



                    using (SolidBrush pb = new SolidBrush(bgCol))

                    using (Pen pp = new Pen(borderCol, 1.2f))

                    {

                        g.FillRoundedRectangle(pb, r, 17f);

                        g.DrawRoundedRectangle(pp, r, 17f);

                    }



                    using (SolidBrush tb = new SolidBrush(textCol))

                    {

                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                        g.DrawString(catDisp, pf, tb, r, sf);

                    }



                    curX += textW + 10f;

                }

            }



            return y + pillH;

        }



        private float DrawQuickCategoriesRow(D2DGraphics g, float x, float y, float width, int mouseX, int mouseY)

        {

            _quickCardRects.Clear();

            int count = _quickCards.Length;

            float spacing = 14f;

            float cardW = (width - (count - 1) * spacing) / count;

            if (cardW < 140f) cardW = 140f;

            float cardH = 76f;



            using (Font titleF = new Font("Segoe UI", 10f, FontStyle.Bold))

            using (Font subF = new Font("Segoe UI", 8.2f, FontStyle.Regular))

            {

                for (int i = 0; i < count; i++)

                {

                    float cx = x + i * (cardW + spacing);

                    RectangleF r = new RectangleF(cx, y, cardW, cardH);

                    _quickCardRects.Add(r);



                    bool isHover = (HoveredQuickCardIndex == i);

                    QuickCategoryCard card = _quickCards[i];



                    // Nền thẻ chuyển màu tươi tắn chuẩn thiết kế hiện đại

                    using (LinearGradientBrush lgb = new LinearGradientBrush(r, card.BgColor1, card.BgColor2, 135f))

                    using (Pen bp = new Pen(Color.FromArgb(isHover ? 200 : 70, 255, 255, 255), isHover ? 2f : 1f))

                    {

                        g.FillRoundedRectangle(lgb, r, 14f);

                        g.DrawRoundedRectangle(bp, r, 14f);

                    }



                    // Tên chuyên mục & chú thích

                    using (SolidBrush wb = new SolidBrush(Color.White))

                    using (SolidBrush swb = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))

                    {

                        g.DrawString(AppL10n.Tr(card.Title), titleF, wb, cx + 14f, y + 16f);

                        g.DrawString(AppL10n.Tr(card.Subtitle), subF, swb, cx + 14f, y + 42f);

                    }

                }

            }



            return y + cardH;

        }



        private float DrawSectionHeader(D2DGraphics g, float x, float y, float width, int mouseX, int mouseY)

        {

            float rx = MathF.Round(x);

            float ry = MathF.Round(y);

            using (Font hf = new Font("Segoe UI", 16f, FontStyle.Bold))

            using (SolidBrush hb = new SolidBrush(Color.FromArgb(15, 23, 42)))

            {

                g.DrawString(AppL10n.T("Khám phá kho mẫu ZenQ", "Explore ZenQ Templates"), hf, hb, rx, ry);

            }



            // Nút "↻ Tải lại" và "+ Tải mẫu lên" (Chỉ hiển thị cho Nhà sáng tạo (Creator) và Quản trị viên (Admin); Thành viên thông thường không có nút này)
            bool canUpload = DatabaseAuthService.CurrentUser != null &&
                             (DatabaseAuthService.CurrentUser.IsCreator || DatabaseAuthService.CurrentUser.IsAdmin);

            if (canUpload)
            {
                UploadBtnRect = new RectangleF(MathF.Round(rx + width - 140f), MathF.Round(ry + 2f), 140f, 36f);
                ReloadBtnRect = new RectangleF(MathF.Round(UploadBtnRect.X - 108f), MathF.Round(ry + 2f), 98f, 36f);
            }
            else
            {
                UploadBtnRect = RectangleF.Empty;
                ReloadBtnRect = new RectangleF(MathF.Round(rx + width - 108f), MathF.Round(ry + 2f), 98f, 36f);
            }

            using (SolidBrush rlBg = new SolidBrush(HoverReloadButton ? Color.FromArgb(238, 242, 255) : Color.FromArgb(248, 250, 252)))
            using (Pen rlPen = new Pen(HoverReloadButton ? Color.FromArgb(99, 102, 241) : Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillRoundedRectangle(rlBg, ReloadBtnRect, 18f);
                g.DrawRoundedRectangle(rlPen, ReloadBtnRect, 18f);
            }
            using (Font rf = new Font("Segoe UI", 8.8f, FontStyle.Bold))
            using (SolidBrush rt = new SolidBrush(HoverReloadButton ? Color.FromArgb(79, 70, 229) : Color.FromArgb(51, 65, 85)))
            {
                StringFormat rsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(AppL10n.T("↻ Tải lại", "↻ Reload"), rf, rt, ReloadBtnRect, rsf);
            }

            if (canUpload && !UploadBtnRect.IsEmpty)
            {
                using (LinearGradientBrush upBg = new LinearGradientBrush(UploadBtnRect, Color.FromArgb(99, 102, 241), Color.FromArgb(79, 70, 229), 90f))
                using (Pen upPen = new Pen(HoverUploadButton ? Color.FromArgb(67, 56, 202) : Color.FromArgb(99, 102, 241), 1f))
                {
                    g.FillRoundedRectangle(upBg, UploadBtnRect, 18f);
                    g.DrawRoundedRectangle(upPen, UploadBtnRect, 18f);
                }
                using (Font uf = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (SolidBrush ut = new SolidBrush(Color.White))
                {
                    StringFormat usf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(AppL10n.T("+ Tải mẫu lên", "+ Upload Template"), uf, ut, UploadBtnRect, usf);
                }
            }



            return ry + 46f;

        }



        private List<SlideTemplateItem> GetTrendingTemplates(List<SlideTemplateItem> all)

        {

            if (all == null || all.Count == 0) return new List<SlideTemplateItem>();





            int daySeed = DateTime.Today.Year * 366 + DateTime.Today.DayOfYear;



            // Xếp hạng mẫu thịnh hành: Điểm sử dụng thực tế của người dùng + Pseudo-random hash của ngày hôm nay

            var scored = all.Select(t =>

            {

                int usage = OnlineSlideTemplateManager.GetUsageCount(t.id);

                int dayHash = Math.Abs((t.id + "_" + daySeed).GetHashCode()) % 1000;

                double score = (usage * 200.0) + dayHash;

                return new { Item = t, Score = score };

            })

            .OrderByDescending(x => x.Score)

            .Take(Math.Min(15, all.Count))

            .ToList();



            Random rnd = new Random(daySeed);

            var pool = scored.Select(x => x.Item).ToList();

            var result = new List<SlideTemplateItem>();

            int targetCount = Math.Min(10, pool.Count);

            while (result.Count < targetCount && pool.Count > 0)

            {

                int idx = rnd.Next(pool.Count);

                result.Add(pool[idx]);

                pool.RemoveAt(idx);

            }

            return result;

        }



        private List<SlideTemplateItem> GetForYouTemplates(List<SlideTemplateItem> all)

        {

            if (all == null || all.Count == 0) return new List<SlideTemplateItem>();





            List<string> userKeywords = new List<string>();

            try

            {

                var recentDocs = RecentDocsProvider?.Invoke();

                if (recentDocs != null)

                {

                    foreach (var doc in recentDocs)

                    {

                        if (!string.IsNullOrEmpty(doc.Title))

                        {

                            var words = doc.Title.Split(new[] { ' ', '-', '_', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var w in words)

                            {

                                if (w.Length >= 3 && !userKeywords.Contains(w.ToLowerInvariant()))

                                {

                                    userKeywords.Add(w.ToLowerInvariant());

                                }

                            }

                        }

                    }

                }

            }

            catch { }



            int daySeed = DateTime.Today.Year * 366 + DateTime.Today.DayOfYear + 88;



            var scored = all.Select(t =>

            {

                double matchScore = 0.0;

                string tText = (t.title + " " + t.category + " " + string.Join(" ", t.tags ?? new List<string>())).ToLowerInvariant();

                foreach (var kw in userKeywords)

                {

                    if (tText.Contains(kw)) matchScore += 50.0;

                }



                int dayHash = Math.Abs((t.id + "_foryou_" + daySeed).GetHashCode()) % 100;

                double totalScore = matchScore + dayHash;

                return new { Item = t, Score = totalScore };

            })

            .OrderByDescending(x => x.Score)

            .Take(Math.Min(15, all.Count))

            .ToList();



            Random rnd = new Random(daySeed);

            var pool = scored.Select(x => x.Item).ToList();

            var result = new List<SlideTemplateItem>();

            int targetCount = Math.Min(10, pool.Count);

            while (result.Count < targetCount && pool.Count > 0)

            {

                int idx = rnd.Next(pool.Count);

                result.Add(pool[idx]);

                pool.RemoveAt(idx);

            }

            return result;

        }



        private void DrawFloatingArrowButton(D2DGraphics g, RectangleF btnRect, bool isLeft, bool isHover)

        {

            // 1. Đổ bóng mềm mại cho nút tròn nổi

            using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(24, 0, 0, 0)))

            {

                g.FillEllipse(shadowB, btnRect.X, btnRect.Y + 2.5f, btnRect.Width, btnRect.Height);

            }



            // 2. Nền trắng tròn tinh tế phong cách Canva

            using (SolidBrush bgB = new SolidBrush(isHover ? Color.FromArgb(250, 250, 255) : Color.White))

            using (Pen borderPen = new Pen(isHover ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), isHover ? 1.5f : 1f))

            {

                g.FillEllipse(bgB, btnRect);

                g.DrawEllipse(borderPen, btnRect);

            }



            // 3. Biểu tượng mũi tên chevron

            char glyph = isLeft ? '\uE76B' : '\uE76C'; // ChevronLeft / ChevronRight

            Color iconColor = isHover ? Color.FromArgb(79, 70, 229) : Color.FromArgb(30, 41, 59);

            FluentIconRenderer.DrawGlyphD2D(g, glyph, btnRect, iconColor, 12f);

        }



        private float DrawHorizontalCarouselSection(

            D2DGraphics g,

            string title,

            List<SlideTemplateItem> items,

            float x,

            float y,

            float width,

            ref float scrollX,

            float targetScrollX,

            out float maxScrollX,

            out RectangleF leftBtnRect,

            out RectangleF rightBtnRect,

            bool hoverLeft,

            bool hoverRight)

        {

            leftBtnRect = RectangleF.Empty;

            rightBtnRect = RectangleF.Empty;

            maxScrollX = 0f;



            if (items == null || items.Count == 0) return y;



            // Tiêu đề đề mục

            using (Font tf = new Font("Segoe UI", 13.5f, FontStyle.Bold))

            using (SolidBrush tb = new SolidBrush(Color.FromArgb(15, 23, 42)))

            {

                g.DrawString(title, tf, tb, x, y);

            }



            float stripY = y + 32f;

            float cardW = 280f;

            float cardH = MathF.Round(cardW * 9f / 16f); // ~158f (16:9)

            float spacing = 16f;



            float totalW = items.Count * cardW + (items.Count - 1) * spacing;

            maxScrollX = Math.Max(0f, totalW - width);

            if (scrollX > maxScrollX) scrollX = maxScrollX;

            if (scrollX < 0f) scrollX = 0f;



            // Vùng hiển thị cuộn ngang (Clip viewport)
            g.PushClip(new RectangleF(x, stripY, width, cardH + 2f));
            for (int i = 0; i < items.Count; i++)
            {
                float cx = MathF.Round(x + i * (cardW + spacing) - scrollX);
                if (cx + cardW < x - 20f || cx > x + width + 20f) continue; // Culling
                RectangleF cardRect = new RectangleF(cx, stripY, cardW, cardH);
                int cardIdx = _templateCardRects.Count;
                _templateCardRects.Add(cardRect);
                _templateCardItems.Add(items[i]);
                bool isHover = (HoveredCardIndex == cardIdx);
                if (stripY + cardH >= _clipViewTopY - 24f && stripY <= _clipViewBottomY + 24f)
                    DrawSingleTemplateCard(g, cardRect, items[i], cardIdx, isHover);
            }



            g.PopClip();



            // Nút mũi tên tròn lướt slide chuẩn ảnh đính kèm

            float btnSize = 40f;

            float btnY = stripY + (cardH - btnSize) / 2f;



            if (scrollX > 8f)

            {

                leftBtnRect = new RectangleF(x + 6f, btnY, btnSize, btnSize);

                DrawFloatingArrowButton(g, leftBtnRect, true, hoverLeft);

            }



            if (scrollX < maxScrollX - 8f && maxScrollX > 0f)

            {

                rightBtnRect = new RectangleF(x + width - btnSize - 6f, btnY, btnSize, btnSize);

                DrawFloatingArrowButton(g, rightBtnRect, false, hoverRight);

            }



            return stripY + cardH + 26f;

        }



        private float DrawGridCards(D2DGraphics g, List<SlideTemplateItem> items, float x, float y, float width)

        {

            int cols = (width >= 860f) ? 3 : 2;

            float spacing = 20f;

            float cardW = MathF.Round((width - (cols - 1) * spacing) / cols);

            float cardH = MathF.Round(cardW * 9f / 16f); // 16:9



            for (int i = 0; i < items.Count; i++)

            {

                int col = i % cols;

                int row = i / cols;



                float cx = MathF.Round(x + col * (cardW + spacing));

                float cy = MathF.Round(y + row * (cardH + spacing));



                RectangleF cardRect = new RectangleF(cx, cy, cardW, cardH);
                int cardIdx = _templateCardRects.Count;
                _templateCardRects.Add(cardRect);
                _templateCardItems.Add(items[i]);
                bool isHover = (HoveredCardIndex == cardIdx);
                if (cardRect.Bottom >= _clipViewTopY - 24f && cardRect.Y <= _clipViewBottomY + 24f)
                    DrawSingleTemplateCard(g, cardRect, items[i], cardIdx, isHover);

            }



            int totalRows = (items.Count + cols - 1) / cols;

            return y + totalRows * (cardH + spacing);

        }



        private void DrawSingleTemplateCard(D2DGraphics g, RectangleF cardRect, SlideTemplateItem tmpl, int cardIndex, bool isHover)
        {
            bool isActiveCarousel = (_activeCarouselCardIndex == cardIndex && _activeCarouselTemplate == tmpl);
            float offset = isActiveCarousel ? _carouselScrollOffset : 0f;
            int slideCount = tmpl.slideCount > 0 ? tmpl.slideCount : 1;

            int baseSlide = ((int)Math.Floor(offset)) % slideCount;
            if (baseSlide < 0) baseSlide = 0;
            float frac = offset - (float)Math.Floor(offset);
            int nextSlide = (baseSlide + 1) % slideCount;

            Bitmap bmpCurr = OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, baseSlide) ?? OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, 0);
            Bitmap bmpNext = (frac > 0.001f && slideCount > 1) ? OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, nextSlide) : null;
            bool isSliding = (frac > 0.001f && slideCount > 1 && bmpNext != null);

            if (isSliding)
            {
                using (GraphicsPath cardClip = CreateRoundedRectanglePath(cardRect, 14f))
                {
                    g.PushClip(cardClip);
                    float slideCurrX = MathF.Round(cardRect.X - frac * cardRect.Width);
                    if (bmpCurr != null)
                        g.DrawImage(bmpCurr, slideCurrX, cardRect.Y, cardRect.Width, cardRect.Height);
                    else
                    {
                        _hasUnrenderedCards = true;
                        DrawSkeletonThumbnail(g, new RectangleF(slideCurrX, cardRect.Y, cardRect.Width, cardRect.Height), isHover, cardIndex);
                    }
                    float slideNextX = MathF.Round(cardRect.X + (1.0f - frac) * cardRect.Width);
                    g.DrawImage(bmpNext, slideNextX, cardRect.Y, cardRect.Width, cardRect.Height);
                    g.PopClip();
                }
            }
            else
            {
                if (bmpCurr != null)
                    g.DrawRoundedImage(bmpCurr, cardRect, 14f);
                else
                {
                    _hasUnrenderedCards = true;
                    DrawSkeletonThumbnail(g, cardRect, isHover, cardIndex);
                }
            }

            // 2. Carousel Dots Indicator & Slide Counter Badge (inset inside rounded card, zero PushLayer needed!)
            if (slideCount > 1)
            {
                int dispIdx = (isActiveCarousel && OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, _displayedSlideIndex % slideCount) != null) ? (_displayedSlideIndex % slideCount) : 0;
                float dotH = 5.5f;
                float dotY = cardRect.Bottom - 14f;
                int maxDots = Math.Min(slideCount, 12);
                float totalDotsW = maxDots * 11f + 12f;
                float dotStartX = cardRect.X + (cardRect.Width - totalDotsW) / 2f;

                for (int d = 0; d < maxDots; d++)
                {
                    bool isDotActive = (d == (dispIdx % maxDots));
                    float dotW = isDotActive ? 16f : 5.5f;
                    RectangleF dotR = new RectangleF(dotStartX, dotY, dotW, dotH);
                    using (SolidBrush dotB = new SolidBrush(isDotActive ? Color.White : Color.FromArgb(145, 255, 255, 255)))
                        g.FillRoundedRectangle(dotB, dotR, 2.75f);
                    dotStartX += dotW + 5.5f;
                }

                int activeCounter = dispIdx + 1;
                RectangleF counterRect = new RectangleF(cardRect.Right - 54f, cardRect.Y + 10f, 44f, 22f);
                using (SolidBrush cntBg = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                    g.FillRoundedRectangle(cntBg, counterRect, 11f);
                using (Font cntFont = new Font("Segoe UI", 7.8f, FontStyle.Bold))
                using (SolidBrush cntTxt = new SolidBrush(Color.White))
                {
                    StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString($"{activeCounter}/{slideCount}", cntFont, cntTxt, counterRect, csf);
                }
            }

            // 4. Viền bo góc 14px (sáng viền indigo khi hover)
            using (Pen cardBorder = new Pen(isHover ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), isHover ? 2f : 1f))
            {
                g.DrawRoundedRectangle(cardBorder, cardRect, 14f);
            }
        }



        private static bool MatchesTemplateSearch(SlideTemplateItem t, string rawQuery)
        {
            if (t == null) return false;
            if (string.IsNullOrWhiteSpace(rawQuery)) return true;

            string qLower = rawQuery.Trim().ToLowerInvariant();
            string qNoDia = TemplateUploadDialogForm.RemoveDiacritics(qLower);

            // 1. Kh?p tr?c ti?p m? ID ng??i d?ng ??ng m?u (v? d?: g? "1", "#1", "id:1", "id: 1", "uid:1", "author:1", "tacgia:1")
            if (t.authorId > 0)
            {
                string cleanIdQuery = qLower.TrimStart('#').Trim();
                if (cleanIdQuery.StartsWith("id:") || cleanIdQuery.StartsWith("id=") || cleanIdQuery.StartsWith("uid:") || cleanIdQuery.StartsWith("author:") || cleanIdQuery.StartsWith("tacgia:"))
                {
                    int colonIdx = cleanIdQuery.IndexOfAny(new[] { ':', '=' });
                    if (colonIdx >= 0) cleanIdQuery = cleanIdQuery.Substring(colonIdx + 1).Trim();
                }
                if (int.TryParse(cleanIdQuery, out int searchedAuthorId) && searchedAuthorId == t.authorId)
                {
                    return true;
                }
            }

            // 2. T?m ki?m t? nhi?n bao g?m t?n t?c gi? v? m? ID t?c gi?
            string authorStr = t.authorName ?? "";
            string authorIdStr = t.authorId > 0 ? $"{t.authorId} #{t.authorId} id:{t.authorId} uid:{t.authorId}" : "";

            string haystack = string.Join(" ",
                t.id ?? "",
                t.title ?? "",
                t.subtitle ?? "",
                t.category ?? "",
                authorStr,
                authorIdStr,
                t.tags != null ? string.Join(" ", t.tags) : ""
            ).ToLowerInvariant();

            string haystackNoDia = TemplateUploadDialogForm.RemoveDiacritics(haystack);

            if (haystack.Contains(qLower) || haystackNoDia.Contains(qNoDia))
                return true;

            // T?ch t?ng t? kh?a con trong c?u t?m ki?m (gi?ng Canva)
            var tokens = qNoDia.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
            {
                bool allTokensMatch = true;
                foreach (var tk in tokens)
                {
                    if (!haystackNoDia.Contains(tk))
                    {
                        allTokensMatch = false;
                        break;
                    }
                }
                if (allTokensMatch) return true;
            }

            return false;
        }



        private float DrawTemplatesGrid(D2DGraphics g, float x, float y, float width, int mouseX, int mouseY)

        {

            _templateCardRects.Clear();

            _templateCardItems.Clear();

            _templateUseBtnRects.Clear();

            RetryOfflineBtnRect = RectangleF.Empty;

            _hasUnrenderedCards = false;



            // Kiểm tra kết nối Internet thời gian thực: mẫu là 100% Cloud, mất mạng không dùng offline

            if (!OnlineSlideTemplateManager.CheckInternetConnected() || OnlineSlideTemplateManager.IsOffline)

            {

                OnlineSlideTemplateManager.ClearMemoryForOffline();



                RectangleF offlineBox = new RectangleF(x, y + 16f, width, 180f);

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(254, 242, 242)))

                using (Pen border = new Pen(Color.FromArgb(254, 202, 202), 1f))

                {

                    g.FillRoundedRectangle(bg, offlineBox, 14f);

                    g.DrawRoundedRectangle(border, offlineBox, 14f);

                }



                using (Font titleF = new Font("Segoe UI", 13f, FontStyle.Bold))

                using (Font subF = new Font("Segoe UI", 9.5f, FontStyle.Regular))

                using (SolidBrush titleB = new SolidBrush(Color.FromArgb(185, 28, 28)))

                using (SolidBrush subB = new SolidBrush(Color.FromArgb(127, 29, 29)))

                {

                    StringFormat csf = new StringFormat { Alignment = StringAlignment.Center };

                    g.DrawString(AppL10n.T("Mất kết nối Internet", "No Internet Connection"), titleF, titleB, new RectangleF(x, y + 42f, width, 28f), csf);

                    g.DrawString(AppL10n.T("Kho mẫu thuyết trình trực tuyến (Cloud) yêu cầu kết nối mạng. Vui lòng kiểm tra Internet và thử tải lại.", "Cloud presentation templates require an Internet connection. Please check your network and try again."), subF, subB, new RectangleF(x + 20f, y + 76f, width - 40f, 36f), csf);

                }



                RetryOfflineBtnRect = new RectangleF(x + (width - 140f) / 2f, y + 122f, 140f, 36f);

                using (SolidBrush btnBg = new SolidBrush(HoverRetryOfflineButton ? Color.FromArgb(185, 28, 28) : Color.FromArgb(220, 38, 38)))

                using (Font btnF = new Font("Segoe UI", 9f, FontStyle.Bold))

                using (SolidBrush btnTxt = new SolidBrush(Color.White))

                {

                    g.FillRoundedRectangle(btnBg, RetryOfflineBtnRect, 18f);

                    StringFormat bsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                    g.DrawString(AppL10n.T("↻ Thử tải lại", "↻ Try Again"), btnF, btnTxt, RetryOfflineBtnRect, bsf);

                }



                return y + 220f;

            }



            var list = OnlineSlideTemplateManager.Templates;



            // Nếu đang tải hoặc kho rỗng

            if (list == null || list.Count == 0)

            {

                using (Font emptyFont = new Font("Segoe UI", 11f, FontStyle.Regular))

                using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))

                {

                    string msg = OnlineSlideTemplateManager.IsLoading

                        ? AppL10n.T("Đang đồng bộ danh mục mẫu từ đám mây GitHub...", "Syncing templates from cloud...")

                        : AppL10n.T("Hiện tại chưa có mẫu khả dụng trên đám mây", "No templates available in the cloud yet");

                    g.DrawString(msg, emptyFont, emptyBrush, x, y + 20f);

                }

                return y + 60f;

            }



            // A. KHI ĐANG TÌM KIẾM THEO TỪ KHÓA / ID / TAGS

            if (!string.IsNullOrWhiteSpace(_searchQuery))

            {

                var searchList = list.FindAll(t => MatchesTemplateSearch(t, _searchQuery));



                using (Font secFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))

                using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))

                {

                    g.DrawString(AppL10n.T($"Kết quả tìm kiếm cho: \"{_searchQuery}\" ({searchList.Count} mẫu)", $"Search results for \"{_searchQuery}\" ({searchList.Count})"), secFont, secBrush, x, y);

                }



                if (searchList.Count == 0)

                {

                    using (Font emptyFont = new Font("Segoe UI", 11f, FontStyle.Regular))

                    using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))

                    {

                        g.DrawString(AppL10n.T("Không tìm thấy mẫu khớp với tên, mã ID hoặc từ khóa này.", "No templates match that name, ID, or keyword."), emptyFont, emptyBrush, x, y + 42f);

                    }

                    return y + 80f;

                }



                return DrawGridCards(g, searchList, x, y + 36f, width);

            }



            // B. KHI Ở TRẠNG THÁI KHÁM PHÁ MẪU MẶC ĐỊNH

            float curY = y;



            // Khi c? t? 5 m?u tr? l?n m?i chia th?m ph?n m?c Trending / For You.

            // Khi ch? c? ?t m?u, hi?n th? duy nh?t danh m?c T?t c? m?u ?? giao di?n s?ch s?, kh?ng b? l?p l?i.

            if (list.Count > 0)

            {

                var trendingList = GetTrendingTemplates(list);

                if (trendingList != null && trendingList.Count > 0)

                {

                    curY = DrawHorizontalCarouselSection(g, AppL10n.T("Mẫu thịnh hành", "Trending Templates"), trendingList, x, curY, width,

                        ref TrendingScrollX, TrendingTargetScrollX, out TrendingMaxScrollX, out TrendingLeftBtnRect, out TrendingRightBtnRect, HoverTrendingLeftBtn, HoverTrendingRightBtn);

                }

                var forYouList = GetForYouTemplates(list);

                if (forYouList != null && forYouList.Count > 0)

                {

                    curY = DrawHorizontalCarouselSection(g, AppL10n.T("Dành cho bạn", "Recommended for You"), forYouList, x, curY, width,

                        ref ForYouScrollX, ForYouTargetScrollX, out ForYouMaxScrollX, out ForYouLeftBtnRect, out ForYouRightBtnRect, HoverForYouLeftBtn, HoverForYouRightBtn);

                }

            }



            // 3. DANH MỤC: TẤT CẢ MẪU (All Templates Grid)

            using (Font secFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))

            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))

            {

                g.DrawString(AppL10n.T("Tất cả mẫu", "All Templates"), secFont, secBrush, x, curY);

            }

            curY += 34f;



            return DrawGridCards(g, list, x, curY, width);

        }



        private void DrawScrollbar(D2DGraphics g, float x, float y, float width, float height, float thumbRatio, float scrollRatio)

        {

            thumbRatio = Math.Max(0.1f, Math.Min(1.0f, thumbRatio));

            float thumbH = Math.Max(28f, height * thumbRatio);

            float thumbY = y + (height - thumbH) * Math.Max(0f, Math.Min(1.0f, scrollRatio));



            RectangleF tr = new RectangleF(x, thumbY, width, thumbH);

            using (SolidBrush tb = new SolidBrush(Color.FromArgb(140, 148, 163, 184)))

            {

                g.FillRoundedRectangle(tb, tr, width / 2f);

            }

        }



        private void TriggerCarouselForCard(int cardIdx, SlideTemplateItem tmpl)

        {

            if (tmpl == null)

            {

                _activeCarouselCardIndex = -1;

                _activeCarouselTemplate = null;

                return;

            }



            if (_activeCarouselCardIndex != cardIdx || _activeCarouselTemplate != tmpl)

            {

                _activeCarouselCardIndex = cardIdx;

                _activeCarouselTemplate = tmpl;

                _carouselCurSlide = 0;

                _carouselTransitionProgress = 0f;

                _carouselSlideHoldStart = DateTime.UtcNow;

                _carouselScrollOffset = 0f;

                _displayedSlideIndex = 0;



                // Hàng đợi tải trước toàn bộ các trang của mẫu này theo đúng thứ tự 0 -> N-1

                int cnt = Math.Max(1, tmpl.slideCount);

                for (int s = 0; s < cnt; s++)

                {

                    OnlineSlideTemplateManager.QueueSlideRender(tmpl.id, s);

                }

            }

        }



        public void OpenPreviewModal(SlideTemplateItem tmpl)
        {
            if (tmpl == null) return;
            PreviewModalTemplate = tmpl;
            PreviewModalSlideIndex = 0;
            PreviewModalScrollY = 0f;
            PreviewModalMaxScrollY = 0f;
            PreviewModalThumbScrollX = 0f;
            PreviewModalThumbTargetScrollX = 0f;
            PreviewModalThumbMaxScrollX = 0f;
            HoveredModalThumbIndex = -1;
            HoveredModalRelatedIndex = -1;
            HoverModalCloseBtn = false;
            HoverModalUseBtn = false;
            HoverModalThumbLeftBtn = false;
            HoverModalThumbRightBtn = false;
            HoverModalDeleteBtn = false;
            PreviewModalDeleteBtnRect = RectangleF.Empty;
            HoveredCardIndex = -1;
            HoverBackButton = false;
            HoverUploadButton = false;
            HoverReloadButton = false;
            HoverRetryOfflineButton = false;
            HoverTrendingLeftBtn = false;
            HoverTrendingRightBtn = false;
            HoverForYouLeftBtn = false;
            HoverForYouRightBtn = false;



            int cnt = Math.Max(1, tmpl.slideCount);

            for (int s = 0; s < cnt; s++)

            {

                OnlineSlideTemplateManager.QueueSlideRender(tmpl.id, s);

            }

        }



        public void ClosePreviewModal()

        {

            PreviewModalTemplate = null;

            HoverModalCloseBtn = false;

            HoverModalUseBtn = false;

            HoverModalThumbLeftBtn = false;

            HoverModalThumbRightBtn = false;
            HoverModalDeleteBtn = false;
            PreviewModalDeleteBtnRect = RectangleF.Empty;

            HoveredModalThumbIndex = -1;

            HoveredModalRelatedIndex = -1;

            _modalThumbRects.Clear();

            _modalRelatedCardRects.Clear();

            _modalRelatedCardItems.Clear();

        }



        private List<SlideTemplateItem> GetRelatedTemplates(SlideTemplateItem current, List<SlideTemplateItem> all)

        {

            var result = new List<SlideTemplateItem>();

            if (current == null || all == null || all.Count == 0) return result;



            var curWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string rawWords = (current.title + " " + current.category + " " + (current.tags != null ? string.Join(" ", current.tags) : ""));

            foreach (var w in rawWords.Split(new[] { ' ', ',', ';', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries))

            {

                if (w.Length >= 2) curWords.Add(TemplateUploadDialogForm.RemoveDiacritics(w.ToLowerInvariant()));

            }



            var scored = all

                .Where(t => t != null && t.id != current.id)

                .Select(t =>

                {

                    double score = 0;

                    if (!string.IsNullOrEmpty(t.category) && string.Equals(t.category, current.category, StringComparison.OrdinalIgnoreCase))

                        score += 40;

                    if (current.authorId > 0 && t.authorId == current.authorId)

                        score += 35;



                    string hay = TemplateUploadDialogForm.RemoveDiacritics((t.title + " " + t.category + " " + (t.tags != null ? string.Join(" ", t.tags) : "")).ToLowerInvariant());

                    foreach (var kw in curWords)

                    {

                        if (hay.Contains(kw)) score += 15;

                    }

                    return new { Item = t, Score = score };

                })

                .OrderByDescending(x => x.Score)

                .Select(x => x.Item)

                .ToList();



            return scored;

        }



        private void DrawTemplatePreviewModal(D2DGraphics g, float viewX, float viewY, float viewW, float viewH, int mouseX, int mouseY)

        {

            _modalThumbRects.Clear();

            _modalRelatedCardRects.Clear();

            _modalRelatedCardItems.Clear();

            PreviewModalThumbLeftBtnRect = RectangleF.Empty;

            PreviewModalThumbRightBtnRect = RectangleF.Empty;



            var tmpl = PreviewModalTemplate;

            if (tmpl == null) return;



            // 1. Phông nền tối mờ (Backdrop) phủ lên vùng Khám phá mẫu

            RectangleF backdropRect = new RectangleF(viewX, viewY, viewW, viewH);

            using (SolidBrush backdropB = new SolidBrush(Color.FromArgb(148, 15, 23, 42)))

            {

                g.FillRectangle(backdropB, backdropRect);

            }



            // 2. Hộp thoại trắng bo tròn chuẩn Canva ở chính giữa

            float modalW = Math.Min(980f, viewW - 56f);

            float modalH = Math.Min(760f, viewH - 36f);

            if (modalW < 540f) modalW = Math.Max(320f, viewW - 20f);

            if (modalH < 400f) modalH = Math.Max(280f, viewH - 20f);



            float modalX = MathF.Round(viewX + (viewW - modalW) / 2f);

            float modalY = MathF.Round(viewY + (viewH - modalH) / 2f);

            PreviewModalBoxRect = new RectangleF(modalX, modalY, modalW, modalH);



            // Đổ bóng hộp thoại

            using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))

            {

                g.FillRoundedRectangle(shadowB, new RectangleF(modalX, modalY + 6f, modalW, modalH), 18f);

            }



            // Nền trắng hộp thoại

            using (SolidBrush modalBg = new SolidBrush(Color.White))

            {

                g.FillRoundedRectangle(modalBg, PreviewModalBoxRect, 18f);

            }



            // Tính vị trí nút đóng (X) ở góc phải trên (sẽ vẽ sau cùng để luôn nổi trên cùng, không bao giờ bị đè)
            float closeSize = 36f;
            float closeX = (modalX + modalW + closeSize + 14f <= viewX + viewW)
                ? (modalX + modalW + 10f)
                : (modalX + modalW - closeSize - 14f);
            float closeY = modalY + 12f;
            PreviewModalCloseBtnRect = new RectangleF(closeX, closeY, closeSize, closeSize);

            // 3. Nội dung bên trong hộp thoại (dùng AxisAlignedClip hình chữ nhật trực tiếp trên nền trắng đục để DirectWrite ClearType sắc nét tuyệt đối 100%)
            g.PushClip(new RectangleF(modalX + 2f, modalY + 12f, modalW - 4f, modalH - 24f));
            {
                float pad = 28f;
                float innerW = modalW - pad * 2f;
                float curY = modalY + pad - MathF.Round(PreviewModalScrollY);

                // Chia 2 cột phần trên: Cột trái (~60% - Khung chiếu lớn + dải trang thu nhỏ), Cột phải (~40% - Tên mẫu & nút Tùy chỉnh mẫu này)
                float leftW = MathF.Round(innerW * 0.60f);
                float rightW = innerW - leftW - 28f;
                float leftX = modalX + pad;
                float rightX = leftX + leftW + 28f;

                int slideCount = Math.Max(1, tmpl.slideCount);
                if (PreviewModalSlideIndex < 0 || PreviewModalSlideIndex >= slideCount)
                    PreviewModalSlideIndex = 0;

                // 3.1 KHUNG CHIẾU LỚN (BÊN TRÁI) - Tự động thích ứng tỉ lệ ảnh thực tế (16:9, 4:3, Banner, Vuông, Dọc...)
                Bitmap bigBmp = OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, PreviewModalSlideIndex)
                             ?? OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, 0);
                float tmplAspect = 16f / 9f;
                if (bigBmp != null && bigBmp.Height > 0)
                {
                    tmplAspect = (float)bigBmp.Width / (float)bigBmp.Height;
                }

                float bigPreviewH = Math.Min(modalH * 0.52f, MathF.Round(leftW / tmplAspect));
                if (bigPreviewH < 100f) bigPreviewH = 100f;
                RectangleF bigPreviewRect = new RectangleF(leftX, curY, leftW, bigPreviewH);

                using (GraphicsPath bigClip = CreateRoundedRectanglePath(bigPreviewRect, 12f))
                {
                    g.PushClip(bigClip);
                    if (bigBmp != null)
                    {
                        float drawW = bigPreviewRect.Width;
                        float drawH = drawW / tmplAspect;
                        if (drawH > bigPreviewRect.Height)
                        {
                            drawH = bigPreviewRect.Height;
                            drawW = drawH * tmplAspect;
                        }
                        float drawX = bigPreviewRect.X + (bigPreviewRect.Width - drawW) / 2f;
                        float drawY = bigPreviewRect.Y + (bigPreviewRect.Height - drawH) / 2f;
                        g.DrawImage(bigBmp, drawX, drawY, drawW, drawH);
                    }
                    else
                    {
                        _hasUnrenderedCards = true;
                        DrawSkeletonThumbnail(g, bigPreviewRect, false, 0);
                    }
                    g.PopClip();
                }
                using (Pen bigBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    g.DrawRoundedRectangle(bigBorder, bigPreviewRect, 12f);
                }

                // 3.2 DẢI THUMBNAIL CÁC TRANG SLIDE (BÊN DƯỚI KHUNG LỚN)
                float thumbStripY = bigPreviewRect.Bottom + 14f;
                float thumbW = 104f;
                float thumbH = Math.Max(26f, Math.Min(80f, MathF.Round(thumbW / tmplAspect)));
                float thumbGap = 10f;

                float totalThumbsW = slideCount * thumbW + (slideCount - 1) * thumbGap;
                PreviewModalThumbMaxScrollX = Math.Max(0f, totalThumbsW - leftW);
                if (PreviewModalThumbScrollX > PreviewModalThumbMaxScrollX) PreviewModalThumbScrollX = PreviewModalThumbMaxScrollX;
                if (PreviewModalThumbScrollX < 0f) PreviewModalThumbScrollX = 0f;

                g.PushClip(new RectangleF(leftX - 2f, thumbStripY - 3f, leftW + 4f, thumbH + 6f));
                for (int s = 0; s < slideCount; s++)
                {
                    float tx = MathF.Round(leftX + s * (thumbW + thumbGap) - PreviewModalThumbScrollX);
                    RectangleF tRect = new RectangleF(tx, thumbStripY, thumbW, thumbH);
                    _modalThumbRects.Add(tRect);

                    if (tx + thumbW < leftX - 10f || tx > leftX + leftW + 10f) continue;

                    bool isSelected = (s == PreviewModalSlideIndex);
                    bool isThumbHover = (HoveredModalThumbIndex == s);

                    using (GraphicsPath tClip = CreateRoundedRectanglePath(tRect, 8f))
                    {
                        g.PushClip(tClip);
                        Bitmap sBmp = OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, s);
                        if (sBmp != null)
                        {
                            g.DrawImage(sBmp, tRect.X, tRect.Y, tRect.Width, tRect.Height);
                        }
                        else
                        {
                            _hasUnrenderedCards = true;
                            DrawSkeletonThumbnail(g, tRect, false, s);
                        }
                        g.PopClip();
                    }

                    if (isSelected)
                    {
                        RectangleF outerRing = new RectangleF(tRect.X - 2f, tRect.Y - 2f, tRect.Width + 4f, tRect.Height + 4f);
                        using (Pen activePen = new Pen(Color.FromArgb(124, 58, 237), 2.2f))
                        {
                            g.DrawRoundedRectangle(activePen, outerRing, 10f);
                        }
                    }
                    else
                    {
                        using (Pen normPen = new Pen(isThumbHover ? Color.FromArgb(139, 92, 246) : Color.FromArgb(226, 232, 240), 1.2f))
                        {
                            g.DrawRoundedRectangle(normPen, tRect, 8f);
                        }
                    }
                }
                g.PopClip();

                // Nút mũi tên cuộn dải trang thu nhỏ nếu có nhiều trang
                float arrowSz = 32f;
                float arrowY = thumbStripY + (thumbH - arrowSz) / 2f;
                if (PreviewModalThumbScrollX > 4f)
                {
                    PreviewModalThumbLeftBtnRect = new RectangleF(leftX + 4f, arrowY, arrowSz, arrowSz);
                    DrawFloatingArrowButton(g, PreviewModalThumbLeftBtnRect, true, HoverModalThumbLeftBtn);
                }
                if (PreviewModalThumbScrollX < PreviewModalThumbMaxScrollX - 4f && PreviewModalThumbMaxScrollX > 0f)
                {
                    PreviewModalThumbRightBtnRect = new RectangleF(leftX + leftW - arrowSz - 4f, arrowY, arrowSz, arrowSz);
                    DrawFloatingArrowButton(g, PreviewModalThumbRightBtnRect, false, HoverModalThumbRightBtn);
                }

                // 3.3 CỘT THÔNG TIN & NÚT HÀNH ĐỘNG (BÊN PHẢI)
                // Vẽ bằng DirectWrite vector sắc nét, chừa khoảng trống bên phải để không bao giờ chạm vào nút Đóng (X)
                float infoY = curY + 4f;
                string displayTitle = !string.IsNullOrWhiteSpace(tmpl.title)
                    ? tmpl.title.Trim()
                    : (!string.IsNullOrWhiteSpace(tmpl.id) ? tmpl.id.Replace("-", " ").Replace("_", " ") : AppL10n.T("Mẫu thuyết trình", "Presentation Template"));

                float titleMaxW = (closeX < modalX + modalW)
                    ? Math.Max(120f, closeX - rightX - 12f)
                    : rightW;

                using (Font titleFont = new Font("Segoe UI", 16f, FontStyle.Bold))
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    SizeF measuredTitle = g.MeasureString(displayTitle, titleFont, (int)titleMaxW);
                    float titleBoxH = Math.Max(34f, Math.Min(84f, measuredTitle.Height + 6f));
                    RectangleF titleRect = new RectangleF(rightX, infoY, titleMaxW, titleBoxH);
                    StringFormat tsf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        Trimming = StringTrimming.EllipsisWord
                    };
                    g.DrawString(displayTitle, titleFont, titleBrush, titleRect, tsf);
                    infoY += titleBoxH + 8f;
                }

                // Thông tin Tác giả và ID người dùng dưới tên bài (viết tách thành 2 hàng riêng biệt)
                string authName = !string.IsNullOrWhiteSpace(tmpl.authorName) ? tmpl.authorName.Trim() : "Trương Nguyễn Thế Danh";
                int authId = tmpl.authorId > 0 ? tmpl.authorId : 1;

                // Hàng 1: Họ tên tác giả
                using (Font authFont = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (SolidBrush authBrush = new SolidBrush(Color.FromArgb(79, 70, 229)))
                {
                    RectangleF authRect = new RectangleF(rightX, infoY, rightW, 22f);
                    StringFormat asf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(AppL10n.T($"👤 Tác giả: {authName}", $"👤 Author: {authName}"), authFont, authBrush, authRect, asf);
                    infoY += 24f;
                }

                // Hàng 2: ID người dùng (hàng riêng biệt)
                using (Font idFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush idBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    RectangleF idRect = new RectangleF(rightX, infoY, rightW, 22f);
                    StringFormat isf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(AppL10n.T($"🆔 ID người dùng: #{authId}", $"🆔 User ID: #{authId}"), idFont, idBrush, idRect, isf);
                    infoY += 26f;
                }

                // Thông số kỹ thuật slide (16:9, kích thước, số trang)
                using (Font subFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    string metaLine = AppL10n.T($"Bài thuyết trình (16:9)  •  1920 × 1080 px  •  {slideCount} trang", $"Presentation (16:9)  •  1920 × 1080 px  •  {slideCount} slides");
                    RectangleF metaRect = new RectangleF(rightX, infoY, rightW, 24f);
                    StringFormat msf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(metaLine, subFont, subBrush, metaRect, msf);
                    infoY += 32f;
                }

                // Nút chính màu tím Canva: "Tùy chỉnh mẫu này"
                PreviewModalUseBtnRect = new RectangleF(rightX, infoY, rightW, 42f);
                Color btnColor1 = HoverModalUseBtn ? Color.FromArgb(109, 40, 217) : Color.FromArgb(124, 58, 237);
                Color btnColor2 = HoverModalUseBtn ? Color.FromArgb(91, 33, 182) : Color.FromArgb(109, 40, 217);
                using (LinearGradientBrush btnGrad = new LinearGradientBrush(PreviewModalUseBtnRect, btnColor1, btnColor2, 0f))
                {
                    g.FillRoundedRectangle(btnGrad, PreviewModalUseBtnRect, 10f);
                }
                using (Font btnFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (SolidBrush btnTxt = new SolidBrush(Color.White))
                {
                    StringFormat bsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(AppL10n.T("Tùy chỉnh mẫu này", "Use This Template"), btnFont, btnTxt, PreviewModalUseBtnRect, bsf);
                }
                infoY += 48f;

                // Nút Xóa mẫu dành riêng cho Quản trị viên (Admin)
                bool isAdmin = DatabaseAuthService.CurrentUser != null && DatabaseAuthService.CurrentUser.IsAdmin;
                if (isAdmin)
                {
                    PreviewModalDeleteBtnRect = new RectangleF(rightX, infoY, rightW, 36f);
                    using (SolidBrush delBg = new SolidBrush(HoverModalDeleteBtn ? Color.FromArgb(254, 226, 226) : Color.FromArgb(254, 242, 242)))
                    using (Pen delPen = new Pen(HoverModalDeleteBtn ? Color.FromArgb(239, 68, 68) : Color.FromArgb(252, 165, 165), 1f))
                    {
                        g.FillRoundedRectangle(delBg, PreviewModalDeleteBtnRect, 8f);
                        g.DrawRoundedRectangle(delPen, PreviewModalDeleteBtnRect, 8f);
                    }
                    using (Font delFont = new Font("Segoe UI", 9.2f, FontStyle.Bold))
                    using (SolidBrush delTxt = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        StringFormat dsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(AppL10n.T("🗑 Xóa mẫu này khỏi hệ thống", "🗑 Delete Template"), delFont, delTxt, PreviewModalDeleteBtnRect, dsf);
                    }
                    infoY += 42f;
                }
                else
                {
                    PreviewModalDeleteBtnRect = RectangleF.Empty;
                }

                // 3.4 PHẦN DƯỚI: "Nội dung tương tự" (Đề xuất mẫu liên quan)
                float secY = Math.Max(thumbStripY + thumbH + 28f, (isAdmin ? PreviewModalDeleteBtnRect.Bottom : PreviewModalUseBtnRect.Bottom) + 36f);
                using (Font relTitleFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                using (SolidBrush relTitleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    StringFormat rsf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(AppL10n.T("Nội dung tương tự", "Similar Templates"), relTitleFont, relTitleBrush, new RectangleF(leftX, secY, innerW, 28f), rsf);
                }
                secY += 34f;

                var relatedList = GetRelatedTemplates(tmpl, OnlineSlideTemplateManager.Templates);
                if (relatedList.Count == 0)
                {
                    using (Font emptyF = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (SolidBrush emptyB = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString(AppL10n.T("Chưa có mẫu liên quan khác trong kho trực tuyến.", "No related templates found online."), emptyF, emptyB, leftX, secY);
                    }
                    secY += 40f;
                }
                else
                {
                    int cols = (innerW >= 760f) ? 4 : 3;
                    float gap = 14f;
                    float rCardW = MathF.Round((innerW - (cols - 1) * gap) / cols);
                    float rCardH = MathF.Round(rCardW * 9f / 16f);

                    for (int i = 0; i < relatedList.Count; i++)
                    {
                        int col = i % cols;
                        int row = i / cols;
                        float rcx = MathF.Round(leftX + col * (rCardW + gap));
                        float rcy = MathF.Round(secY + row * (rCardH + gap));

                        RectangleF rRect = new RectangleF(rcx, rcy, rCardW, rCardH);
                        _modalRelatedCardRects.Add(rRect);
                        _modalRelatedCardItems.Add(relatedList[i]);

                        if (rcy + rCardH < modalY - 20f || rcy > modalY + modalH + 20f) continue;

                        int virtualCardIdx = 10000 + i;
                        bool isRelHover = (HoveredModalRelatedIndex == i);
                        DrawSingleTemplateCard(g, rRect, relatedList[i], virtualCardIdx, isRelHover);
                    }

                    int totalRows = (relatedList.Count + cols - 1) / cols;
                    secY += totalRows * (rCardH + gap) + 12f;
                }

                float totalModalContentH = (secY + MathF.Round(PreviewModalScrollY)) - modalY + 16f;
                PreviewModalMaxScrollY = Math.Max(0f, totalModalContentH - modalH);
                if (PreviewModalScrollY > PreviewModalMaxScrollY) PreviewModalScrollY = PreviewModalMaxScrollY;
                if (PreviewModalScrollY < 0f) PreviewModalScrollY = 0f;

                if (PreviewModalMaxScrollY > 0f)
                {
                    DrawScrollbar(g, modalX + modalW - 9f, modalY + 14f, 5f, modalH - 28f, modalH / totalModalContentH, PreviewModalScrollY / PreviewModalMaxScrollY);
                }
            }
            g.PopClip();

            using (Pen modalBorder = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.DrawRoundedRectangle(modalBorder, PreviewModalBoxRect, 18f);
            }

            // Vẽ nút đóng (X) sau cùng để luôn nổi sắc nét phía trên
            using (SolidBrush closeShadow = new SolidBrush(Color.FromArgb(28, 0, 0, 0)))
            {
                g.FillEllipse(closeShadow, closeX, closeY + 2f, closeSize, closeSize);
            }
            using (SolidBrush closeBg = new SolidBrush(HoverModalCloseBtn ? Color.FromArgb(241, 245, 249) : Color.White))
            using (Pen closePen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                g.FillEllipse(closeBg, PreviewModalCloseBtnRect);
                g.DrawEllipse(closePen, PreviewModalCloseBtnRect);
            }
            FluentIconRenderer.DrawGlyphD2D(g, '', PreviewModalCloseBtnRect, Color.FromArgb(30, 41, 59), 11f);
        }



        public bool HandleMouseMove(int mouseX, int mouseY)
        {
            if (PreviewModalTemplate != null)
            {
                HoveredCardIndex = -1;
                HoverBackButton = false;
                HoverUploadButton = false;
                HoverReloadButton = false;
                HoverRetryOfflineButton = false;
                HoverTrendingLeftBtn = false;
                HoverTrendingRightBtn = false;
                HoverForYouLeftBtn = false;
                HoverForYouRightBtn = false;

                bool prevClose = HoverModalCloseBtn;

                bool prevUse = HoverModalUseBtn;

                bool prevL = HoverModalThumbLeftBtn;

                bool prevR = HoverModalThumbRightBtn;
                bool prevDel = HoverModalDeleteBtn;

                int prevThumb = HoveredModalThumbIndex;

                int prevRel = HoveredModalRelatedIndex;



                HoverModalCloseBtn = !PreviewModalCloseBtnRect.IsEmpty && PreviewModalCloseBtnRect.Contains(mouseX, mouseY);

                HoverModalUseBtn = !PreviewModalUseBtnRect.IsEmpty && PreviewModalUseBtnRect.Contains(mouseX, mouseY);

                HoverModalThumbLeftBtn = !PreviewModalThumbLeftBtnRect.IsEmpty && PreviewModalThumbLeftBtnRect.Contains(mouseX, mouseY);

                HoverModalThumbRightBtn = !PreviewModalThumbRightBtnRect.IsEmpty && PreviewModalThumbRightBtnRect.Contains(mouseX, mouseY);
                HoverModalDeleteBtn = !PreviewModalDeleteBtnRect.IsEmpty && PreviewModalDeleteBtnRect.Contains(mouseX, mouseY);



                HoveredModalThumbIndex = -1;

                if (!HoverModalThumbLeftBtn && !HoverModalThumbRightBtn)

                {

                    for (int i = 0; i < _modalThumbRects.Count; i++)

                    {

                        if (_modalThumbRects[i].Contains(mouseX, mouseY))

                        {

                            HoveredModalThumbIndex = i;

                            break;

                        }

                    }

                }



                HoveredModalRelatedIndex = -1;

                for (int i = 0; i < _modalRelatedCardRects.Count; i++)

                {

                    if (_modalRelatedCardRects[i].Contains(mouseX, mouseY) && PreviewModalBoxRect.Contains(mouseX, mouseY))

                    {

                        HoveredModalRelatedIndex = i;

                        break;

                    }

                }



                if (HoveredModalRelatedIndex >= 0 && HoveredModalRelatedIndex < _modalRelatedCardItems.Count)

                {

                    TriggerCarouselForCard(10000 + HoveredModalRelatedIndex, _modalRelatedCardItems[HoveredModalRelatedIndex]);

                }

                else

                {

                    TriggerCarouselForCard(-1, null);

                }



                return (prevClose != HoverModalCloseBtn || prevUse != HoverModalUseBtn || prevDel != HoverModalDeleteBtn ||

                        prevL != HoverModalThumbLeftBtn || prevR != HoverModalThumbRightBtn ||

                        prevThumb != HoveredModalThumbIndex || prevRel != HoveredModalRelatedIndex);

            }



            if (IsDraggingSearchSelection && IsSearchFocused)

            {

                int newCaret = GetSearchCharIndexAtX(mouseX);

                if (newCaret != SearchCaretIndex)

                {

                    SearchCaretIndex = newCaret;

                    return true;

                }

            }



            int prevHoverCard = HoveredCardIndex;

            int prevHoverCat = HoveredCategoryIndex;

            int prevHoverQuick = HoveredQuickCardIndex;

            bool prevBack = HoverBackButton;

            bool prevUp = HoverUploadButton;

            bool prevReload = HoverReloadButton;

            bool prevRetry = HoverRetryOfflineButton;

            bool prevTLeft = HoverTrendingLeftBtn;

            bool prevTRight = HoverTrendingRightBtn;

            bool prevFLeft = HoverForYouLeftBtn;

            bool prevFRight = HoverForYouRightBtn;



            HoverBackButton = BackButtonRect.Contains(mouseX, mouseY);

            HoverUploadButton = !UploadBtnRect.IsEmpty && UploadBtnRect.Contains(mouseX, mouseY);

            HoverReloadButton = !ReloadBtnRect.IsEmpty && ReloadBtnRect.Contains(mouseX, mouseY);

            HoverRetryOfflineButton = !RetryOfflineBtnRect.IsEmpty && RetryOfflineBtnRect.Contains(mouseX, mouseY);

            HoverTrendingLeftBtn = (!TrendingLeftBtnRect.IsEmpty && TrendingLeftBtnRect.Contains(mouseX, mouseY));

            HoverTrendingRightBtn = (!TrendingRightBtnRect.IsEmpty && TrendingRightBtnRect.Contains(mouseX, mouseY));

            HoverForYouLeftBtn = (!ForYouLeftBtnRect.IsEmpty && ForYouLeftBtnRect.Contains(mouseX, mouseY));

            HoverForYouRightBtn = (!ForYouRightBtnRect.IsEmpty && ForYouRightBtnRect.Contains(mouseX, mouseY));



            HoveredCategoryIndex = -1;

            HoveredQuickCardIndex = -1;



            HoveredCardIndex = -1;

            if (!HoverTrendingLeftBtn && !HoverTrendingRightBtn && !HoverForYouLeftBtn && !HoverForYouRightBtn)

            {

                for (int i = 0; i < _templateCardRects.Count; i++)

                {

                    if (_templateCardRects[i].Contains(mouseX, mouseY))

                    {

                        HoveredCardIndex = i;

                        break;

                    }

                }

            }



            // Carousel Hover State Machine Trigger

            if (HoveredCardIndex >= 0 && HoveredCardIndex < _templateCardItems.Count)

            {

                TriggerCarouselForCard(HoveredCardIndex, _templateCardItems[HoveredCardIndex]);

            }

            else

            {

                TriggerCarouselForCard(-1, null);

            }



            return (prevHoverCard != HoveredCardIndex || prevHoverCat != HoveredCategoryIndex ||

                    prevHoverQuick != HoveredQuickCardIndex || prevBack != HoverBackButton ||

                    prevUp != HoverUploadButton || prevReload != HoverReloadButton ||

                    prevRetry != HoverRetryOfflineButton || prevTLeft != HoverTrendingLeftBtn ||

                    prevTRight != HoverTrendingRightBtn || prevFLeft != HoverForYouLeftBtn ||

                    prevFRight != HoverForYouRightBtn);

        }



        public void HandleMouseUp()

        {

            IsDraggingSearchSelection = false;

        }



        public bool HandleMouseWheel(int delta)

        {

            if (PreviewModalTemplate != null)

            {

                PreviewModalScrollY = MathF.Round(Math.Max(0f, Math.Min(PreviewModalMaxScrollY, PreviewModalScrollY - (delta / 120f) * 84f)));

                return true;

            }



            float step = (delta / 120f) * 112f;

            float diff = TargetScrollY - SmoothScrollY;

            float basePos = ((-step) * diff >= 0f) ? TargetScrollY : SmoothScrollY;

            TargetScrollY = MathF.Round(Math.Max(0f, Math.Min(MaxScrollY, basePos - step)));

            if (TargetScrollY < 0f) TargetScrollY = 0f;

            if (TargetScrollY > MaxScrollY) TargetScrollY = MaxScrollY;

            return true;

        }



        #region Search Box Full Keyboard & Selection Handling



        public bool HasSearchSelection => SearchCaretIndex != SearchSelectionStart;



        public void DeleteSearchSelection()

        {

            if (!HasSearchSelection) return;

            int min = Math.Min(SearchCaretIndex, SearchSelectionStart);

            int max = Math.Max(SearchCaretIndex, SearchSelectionStart);

            if (min >= 0 && max <= _searchQuery.Length)

            {

                _searchQuery = _searchQuery.Remove(min, max - min);

                SearchCaretIndex = min;

                SearchSelectionStart = min;

                TargetScrollY = 0f;

                SmoothScrollY = 0f;

            }

        }



        public void InsertSearchText(string text)

        {

            if (string.IsNullOrEmpty(text)) return;

            text = text.Replace("\r", "").Replace("\n", "");

            if (HasSearchSelection)

            {

                DeleteSearchSelection();

            }

            int pos = Math.Max(0, Math.Min(SearchCaretIndex, _searchQuery.Length));

            _searchQuery = _searchQuery.Insert(pos, text);

            SearchCaretIndex = pos + text.Length;

            SearchSelectionStart = SearchCaretIndex;

            TargetScrollY = 0f;

            SmoothScrollY = 0f;

        }



        public bool HandleSearchKeyDown(KeyEventArgs e)

        {

            if (PreviewModalTemplate != null && e.KeyCode == Keys.Escape)

            {

                ClosePreviewModal();

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (!IsSearchFocused) return false;



            int len = _searchQuery.Length;

            SearchCaretIndex = Math.Max(0, Math.Min(SearchCaretIndex, len));

            SearchSelectionStart = Math.Max(0, Math.Min(SearchSelectionStart, len));



            if (e.Control && e.KeyCode == Keys.A)

            {

                SearchSelectionStart = 0;

                SearchCaretIndex = len;

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.Control && e.KeyCode == Keys.C)

            {

                if (HasSearchSelection)

                {

                    int min = Math.Min(SearchCaretIndex, SearchSelectionStart);

                    int max = Math.Max(SearchCaretIndex, SearchSelectionStart);

                    try { Clipboard.SetText(_searchQuery.Substring(min, max - min)); } catch { }

                }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.Control && e.KeyCode == Keys.X)

            {

                if (HasSearchSelection)

                {

                    int min = Math.Min(SearchCaretIndex, SearchSelectionStart);

                    int max = Math.Max(SearchCaretIndex, SearchSelectionStart);

                    try { Clipboard.SetText(_searchQuery.Substring(min, max - min)); } catch { }

                    DeleteSearchSelection();

                }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.Control && e.KeyCode == Keys.V)

            {

                try

                {

                    if (Clipboard.ContainsText())

                    {

                        InsertSearchText(Clipboard.GetText());

                    }

                }

                catch { }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.KeyCode == Keys.Left)

            {

                if (e.Shift)

                {

                    if (SearchCaretIndex > 0) SearchCaretIndex--;

                }

                else

                {

                    if (HasSearchSelection)

                    {

                        SearchCaretIndex = Math.Min(SearchCaretIndex, SearchSelectionStart);

                        SearchSelectionStart = SearchCaretIndex;

                    }

                    else if (SearchCaretIndex > 0)

                    {

                        SearchCaretIndex--;

                        SearchSelectionStart = SearchCaretIndex;

                    }

                }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.KeyCode == Keys.Right)

            {

                if (e.Shift)

                {

                    if (SearchCaretIndex < len) SearchCaretIndex++;

                }

                else

                {

                    if (HasSearchSelection)

                    {

                        SearchCaretIndex = Math.Max(SearchCaretIndex, SearchSelectionStart);

                        SearchSelectionStart = SearchCaretIndex;

                    }

                    else if (SearchCaretIndex < len)

                    {

                        SearchCaretIndex++;

                        SearchSelectionStart = SearchCaretIndex;

                    }

                }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.KeyCode == Keys.Home)

            {

                SearchCaretIndex = 0;

                if (!e.Shift) SearchSelectionStart = 0;

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.KeyCode == Keys.End)

            {

                SearchCaretIndex = len;

                if (!e.Shift) SearchSelectionStart = len;

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.KeyCode == Keys.Delete)

            {

                if (HasSearchSelection)

                {

                    DeleteSearchSelection();

                }

                else if (SearchCaretIndex < len)

                {

                    _searchQuery = _searchQuery.Remove(SearchCaretIndex, 1);

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            if (e.KeyCode == Keys.Escape)

            {

                if (HasSearchSelection)

                {

                    SearchSelectionStart = SearchCaretIndex;

                }

                else

                {

                    IsSearchFocused = false;

                }

                e.Handled = true;

                e.SuppressKeyPress = true;

                return true;

            }



            return false;

        }



        public bool HandleSearchKeyPress(KeyPressEventArgs e)

        {

            if (!IsSearchFocused) return false;



            if (e.KeyChar == '\b')

            {

                if (HasSearchSelection)

                {

                    DeleteSearchSelection();

                }

                else if (SearchCaretIndex > 0 && _searchQuery.Length > 0)

                {

                    _searchQuery = _searchQuery.Remove(SearchCaretIndex - 1, 1);

                    SearchCaretIndex--;

                    SearchSelectionStart = SearchCaretIndex;

                    TargetScrollY = 0f;

                    SmoothScrollY = 0f;

                }

                e.Handled = true;

                return true;

            }

            else if (e.KeyChar == '\r' || e.KeyChar == '\n')

            {

                IsSearchFocused = false;

                IsDraggingSearchSelection = false;

                e.Handled = true;

                return true;

            }

            else if (!char.IsControl(e.KeyChar))

            {

                InsertSearchText(e.KeyChar.ToString());

                e.Handled = true;

                return true;

            }



            return false;

        }



        #endregion



        public OfficeAppMode? HandleClick(int mouseX, int mouseY, out string openFilePath, out string docTitle, out bool exitToHome)

        {

            openFilePath = null;

            docTitle = null;

            exitToHome = false;



            // 0. NẾU ĐANG MỞ HỘP THOẠI XEM TRƯỚC MẪU (CANVA PREVIEW MODAL)

            if (PreviewModalTemplate != null)

            {

                // Bấm nút Đóng (X)

                if (!PreviewModalCloseBtnRect.IsEmpty && PreviewModalCloseBtnRect.Contains(mouseX, mouseY))

                {

                    ClosePreviewModal();

                    return null;

                }



                // Bấm ra ngoài hộp thoại (trên vùng nền tối mờ Backdrop) -> đóng hộp thoại

                if (!PreviewModalBoxRect.IsEmpty && !PreviewModalBoxRect.Contains(mouseX, mouseY))

                {

                    ClosePreviewModal();

                    return null;

                }



                // Bấm nút mũi tên cuộn dải trang thu nhỏ

                float thumbStep = (104f + 10f) * 2f;

                if (!PreviewModalThumbLeftBtnRect.IsEmpty && PreviewModalThumbLeftBtnRect.Contains(mouseX, mouseY))

                {

                    PreviewModalThumbTargetScrollX = Math.Max(0f, PreviewModalThumbTargetScrollX - thumbStep);

                    return null;

                }

                if (!PreviewModalThumbRightBtnRect.IsEmpty && PreviewModalThumbRightBtnRect.Contains(mouseX, mouseY))

                {

                    PreviewModalThumbTargetScrollX = Math.Min(PreviewModalThumbMaxScrollX, PreviewModalThumbTargetScrollX + thumbStep);

                    return null;

                }



                // Bấm chọn 1 trang thu nhỏ (Slide Thumbnail) để đổi trang xem trước lớn

                for (int i = 0; i < _modalThumbRects.Count; i++)

                {

                    if (_modalThumbRects[i].Contains(mouseX, mouseY))

                    {

                        PreviewModalSlideIndex = i;

                        return null;

                    }

                }



                // Bấm nút chính "Tùy chỉnh mẫu này" -> lúc này mới tải mẫu về thư mục tạm của tab và mở trình chỉnh sửa!

                if (!PreviewModalUseBtnRect.IsEmpty && PreviewModalUseBtnRect.Contains(mouseX, mouseY))

                {

                    var tmpl = PreviewModalTemplate;

                    string errMsg;

                    string pptxPath = OnlineSlideTemplateManager.DownloadTemplateForTabUse(tmpl, out errMsg);

                    if (!string.IsNullOrEmpty(pptxPath) && File.Exists(pptxPath))

                    {

                        ClosePreviewModal();

                        openFilePath = pptxPath;

                        string cleanTitle = !string.IsNullOrWhiteSpace(tmpl.title)

                            ? tmpl.title.Trim()

                            : Path.GetFileNameWithoutExtension(pptxPath);

                        if (!cleanTitle.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))

                        {

                            cleanTitle += ".pptx";

                        }

                        docTitle = cleanTitle;

                        return OfficeAppMode.Slides;

                    }

                    else

                    {

                        FluentMessageBox.Show(

                            errMsg ?? AppL10n.T("Không thể tải mẫu từ đám mây. Vui lòng thử lại sau giây lát.", "Could not download the template from cloud. Please try again shortly."),

                            AppL10n.T("Kho mẫu trực tuyến", "Online Templates"),

                            MessageBoxButtons.OK,

                            MessageBoxIcon.Warning);

                        return null;

                    }

                }



                // Bấm nút "Xóa mẫu này" dành cho Quản trị viên (Admin)
                if (!PreviewModalDeleteBtnRect.IsEmpty && PreviewModalDeleteBtnRect.Contains(mouseX, mouseY))
                {
                    if (DatabaseAuthService.CurrentUser == null || !DatabaseAuthService.CurrentUser.IsAdmin)
                    {
                        FluentMessageBox.Show(AppL10n.T("Chỉ Quản trị viên mới có quyền thực hiện thao tác xóa mẫu.", "Only administrators can delete templates."), AppL10n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }

                    var tmplToDelete = PreviewModalTemplate;
                    var confirm = FluentMessageBox.Show(
                        AppL10n.T($"Bạn có chắc chắn muốn xóa mẫu '{tmplToDelete.title}' vĩnh viễn khỏi kho trực tuyến không?\n\nTệp thuyết trình và toàn bộ dữ liệu ảnh xem trước sẽ bị xóa hoàn toàn khỏi kho lưu trữ đám mây.", $"Permanently delete template '{tmplToDelete.title}' from the cloud gallery?\n\nThe presentation file and all preview thumbnails will be removed."),
                        AppL10n.T("Xác nhận xóa mẫu", "Confirm Delete Template"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Yes)
                    {
                        ClosePreviewModal();
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            var (ok, err) = await OnlineSlideTemplateManager.DeleteTemplateFromCloudAsync(tmplToDelete).ConfigureAwait(false);
                            if (ok)
                            {
                                FluentMessageBox.Show(AppL10n.T($"Đã xóa thành công mẫu '{tmplToDelete.title}' khỏi kho lưu trữ trực tuyến.", $"Deleted template '{tmplToDelete.title}' from the online gallery."), AppL10n.T("Xóa thành công", "Deleted"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                FluentMessageBox.Show(err ?? AppL10n.T("Không thể xóa mẫu. Vui lòng kiểm tra lại kết nối mạng.", "Failed to delete template. Please check your internet connection."), AppL10n.T("Lỗi xóa mẫu", "Delete Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        });
                    }
                    return null;
                }

                // Bấm vào 1 mẫu trong phần "Nội dung tương tự" bên dưới -> chuyển hộp thoại sang xem mẫu đó

                for (int i = 0; i < _modalRelatedCardRects.Count; i++)

                {

                    if (_modalRelatedCardRects[i].Contains(mouseX, mouseY) && i < _modalRelatedCardItems.Count)

                    {

                        OpenPreviewModal(_modalRelatedCardItems[i]);

                        return null;

                    }

                }



                return null;

            }



            // 1. Mũi tên trở về DUY NHẤT (Quay về Trang chủ)

            if (BackButtonRect.Contains(mouseX, mouseY))

            {

                IsSearchFocused = false;

                IsDraggingSearchSelection = false;

                exitToHome = true;

                return null;

            }



            // 1.1 Tìm kiếm mẫu (Search Box)

            if (SearchBoxRect.Contains(mouseX, mouseY))

            {

                if (!string.IsNullOrEmpty(_searchQuery) && mouseX >= SearchBoxRect.Right - 28f)

                {

                    _searchQuery = "";

                    SearchCaretIndex = 0;

                    SearchSelectionStart = 0;

                    IsSearchFocused = true;

                    IsDraggingSearchSelection = false;

                    return null;

                }



                IsSearchFocused = true;

                int clickedIdx = GetSearchCharIndexAtX(mouseX);

                if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)

                {

                    SearchCaretIndex = clickedIdx;

                }

                else

                {

                    SearchCaretIndex = clickedIdx;

                    SearchSelectionStart = clickedIdx;

                }

                IsDraggingSearchSelection = true;

                return null;

            }

            else

            {

                IsSearchFocused = false;

                IsDraggingSearchSelection = false;

            }



            // 3.5 Nút "↻ Tải lại" hoặc "↻ Thử tải lại" khi mất mạng

            if ((!ReloadBtnRect.IsEmpty && ReloadBtnRect.Contains(mouseX, mouseY)) ||

                (!RetryOfflineBtnRect.IsEmpty && RetryOfflineBtnRect.Contains(mouseX, mouseY)))

            {

                System.Threading.Tasks.Task.Run(async () =>

                {

                    try { await OnlineSlideTemplateManager.RefreshCatalogFromGitHubAsync().ConfigureAwait(false); } catch { }

                });

                return null;

            }



            // 4. Nút "Tải mẫu lên" (+ Thêm mẫu tùy chỉnh kèm Hộp thoại đặt Tên, ID, Từ khóa như Canva)

            if (!UploadBtnRect.IsEmpty && UploadBtnRect.Contains(mouseX, mouseY))
            {
                if (!DatabaseAuthService.IsLoggedIn)
                {
                    var dlgResult = FluentMessageBox.Show(
                        AppL10n.T("Vui lòng đăng nhập tài khoản để sử dụng tính năng đăng tải mẫu thuyết trình.\n\nBạn có muốn đăng nhập ngay bây giờ không?", "Please sign in to upload presentation templates.\n\nWould you like to sign in now?"),
                        AppL10n.T("Yêu cầu đăng nhập", "Sign In Required"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    if (dlgResult == DialogResult.Yes)
                    {
                        using (var authForm = new AuthDialogForm())
                        {
                            authForm.ShowDialog();
                            AppForm.Instance?.Invalidate();
                        }
                    }
                    return null;
                }

                if (!DatabaseAuthService.CurrentUser.IsCreator && !DatabaseAuthService.CurrentUser.IsAdmin)
                {
                    FluentMessageBox.Show(
                        AppL10n.T("Tài khoản của bạn hiện chưa được cấp quyền đăng tải mẫu mới.\n\nChỉ các tài khoản Nhà sáng tạo đã được duyệt mới có thể xuất bản mẫu lên kho trực tuyến.", "Your account does not have permission to upload new templates.\n\nOnly approved Creator or Admin accounts can publish templates."),
                        AppL10n.T("Thông báo quyền hạn", "Permission Required"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return null;
                }

                if (!OnlineSlideTemplateManager.CheckInternetConnected())
                {
                    OnlineSlideTemplateManager.ClearMemoryForOffline();
                    FluentMessageBox.Show(AppL10n.T("Mất kết nối mạng. Vui lòng kiểm tra lại kết nối để đăng tải mẫu.", "No internet connection. Please check your connection before uploading."), AppL10n.T("Mất kết nối mạng", "No Internet Connection"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                using (OpenFileDialog ofd = new OpenFileDialog())

                {

                    ofd.Title = AppL10n.T("Chọn bài thuyết trình mẫu (PowerPoint PPTX / ZenQ Slides)", "Choose a Presentation Template (PowerPoint PPTX / ZenQ Slides)");

                    ofd.Filter = AppL10n.T("Tệp thuyết trình (*.pptx;*.zenq)|*.pptx;*.zenq|Tất cả tệp (*.*)|*.*", "Presentation Files (*.pptx;*.zenq)|*.pptx;*.zenq|All Files (*.*)|*.*");

                    if (ofd.ShowDialog() == DialogResult.OK)

                    {

                        using (var metaDlg = new TemplateUploadDialogForm(ofd.FileName))

                        {

                            if (metaDlg.ShowDialog() == DialogResult.OK)

                            {

                                ActiveCategory = "Tất cả";

                                TargetScrollY = 0f;

                                SmoothScrollY = 0f;

                            }

                        }

                    }

                }

                return null;

            }



            // 4.1 Nút mũi tên lướt ngang Mẫu thịnh hành & Dành cho bạn

            float step = 280f + 16f;

            if (!TrendingLeftBtnRect.IsEmpty && TrendingLeftBtnRect.Contains(mouseX, mouseY))

            {

                TrendingTargetScrollX = Math.Max(0f, TrendingTargetScrollX - step);

                return null;

            }

            if (!TrendingRightBtnRect.IsEmpty && TrendingRightBtnRect.Contains(mouseX, mouseY))

            {

                TrendingTargetScrollX = Math.Min(TrendingMaxScrollX, TrendingTargetScrollX + step);

                return null;

            }

            if (!ForYouLeftBtnRect.IsEmpty && ForYouLeftBtnRect.Contains(mouseX, mouseY))

            {

                ForYouTargetScrollX = Math.Max(0f, ForYouTargetScrollX - step);

                return null;

            }

            if (!ForYouRightBtnRect.IsEmpty && ForYouRightBtnRect.Contains(mouseX, mouseY))

            {

                ForYouTargetScrollX = Math.Min(ForYouMaxScrollX, ForYouTargetScrollX + step);

                return null;

            }



            // 5. Chọn thẻ mẫu: Mở hộp thoại xem trước mẫu kèm Nội dung tương tự chuẩn Canva (khi bấm "Tùy chỉnh mẫu này" mới tải mẫu về tab)

            for (int i = 0; i < _templateCardRects.Count; i++)

            {

                if (_templateCardRects[i].Contains(mouseX, mouseY) && i < _templateCardItems.Count)

                {

                    OpenPreviewModal(_templateCardItems[i]);

                    return null;

                }

            }



            return null;

        }



        private void DrawSkeletonThumbnail(D2DGraphics g, RectangleF thumbRect, bool isHover, int cardIndex)

        {

            // 1. Phông nền xám đơn giản, hiện đại (hoàn toàn không có lưới ca rô theo yêu cầu)

            using (SolidBrush baseBg = new SolidBrush(Color.FromArgb(243, 244, 246))) // #F3F4F6

            {

                g.FillRoundedRectangle(baseBg, thumbRect, 14f);

            }



            // 2. Các đường gạch tượng trưng cho tiêu đề, dòng chữ & bố cục slide (co giãn theo tỷ lệ khung hình)

            float scale = Math.Min(thumbRect.Width / 360f, thumbRect.Height / 202f);

            scale = Math.Max(0.28f, Math.Min(1.4f, scale));



            float padX = Math.Max(6f, 18f * scale);

            float padY = Math.Max(6f, 18f * scale);

            float leftX = thumbRect.X + padX;

            float topY = thumbRect.Y + padY;



            // Đường gạch thanh tiêu đề chính (Title Line)

            float tH = Math.Max(4f, 14f * scale);

            RectangleF titleBone = new RectangleF(leftX, topY, thumbRect.Width * 0.44f, tH);



            // Đường gạch tiêu đề phụ (Subtitle Line)

            float sH = Math.Max(3f, 9f * scale);

            float gapY = Math.Max(3f, 8f * scale);

            RectangleF subBone = new RectangleF(leftX, titleBone.Bottom + gapY, thumbRect.Width * 0.28f, sH);



            // Các đường gạch tượng trưng cho chữ nội dung (Content Text Lines)

            float lH = Math.Max(2.5f, 7f * scale);

            RectangleF line1 = new RectangleF(leftX, subBone.Bottom + gapY * 1.3f, thumbRect.Width * 0.38f, lH);

            RectangleF line2 = new RectangleF(leftX, line1.Bottom + gapY, thumbRect.Width * 0.32f, lH);

            RectangleF line3 = new RectangleF(leftX, line2.Bottom + gapY, thumbRect.Width * 0.24f, lH);



            // Khung đồ họa / hình ảnh minh họa bên phải

            float rightBoxW = thumbRect.Width * 0.34f;

            float rightBoxH = thumbRect.Height * 0.58f;

            RectangleF rightBox = new RectangleF(thumbRect.Right - rightBoxW - padX, thumbRect.Y + (thumbRect.Height - rightBoxH) / 2f, rightBoxW, rightBoxH);



            var boneItems = new (RectangleF Rect, float Radius)[]

            {

                (titleBone, 5f),

                (subBone, 4f),

                (line1, 4f),

                (line2, 4f),

                (line3, 4f),

                (rightBox, 8f)

            };



            // Vẽ màu xám gốc của từng đường gạch / khung hình (Base neutral gray)

            using (SolidBrush boneBaseBrush = new SolidBrush(Color.FromArgb(224, 228, 234))) // #E0E4EA

            {

                foreach (var bone in boneItems)

                {

                    g.FillRoundedRectangle(boneBaseBrush, bone.Rect, bone.Radius);

                }

            }



            // 3. HIỆU ỨNG ÁNH SÁNG QUÉT SHIMMER GIỐNG YOUTUBE

            float shimmerPeriod = 1400f; // 1.4 giây lặp lại 1 chu kỳ quét

            float timeMs = (DateTime.UtcNow.Ticks / 10000) % (long)shimmerPeriod;

            float shimmerPhase = timeMs / shimmerPeriod; // 0.0 -> 1.0



            float sweepW = thumbRect.Width * 0.55f;

            float sweepX = thumbRect.X - sweepW + shimmerPhase * (thumbRect.Width + sweepW * 2f);

            RectangleF sweepRect = new RectangleF(sweepX, thumbRect.Y, sweepW, thumbRect.Height);



            using (LinearGradientBrush shimmerBrush = new LinearGradientBrush(

                sweepRect, Color.Transparent, Color.Transparent, 15f))

            {

                ColorBlend cb = new ColorBlend(4);

                cb.Colors = new Color[]

                {

                    Color.FromArgb(0, 255, 255, 255),

                    Color.FromArgb(130, 255, 255, 255),

                    Color.FromArgb(220, 255, 255, 255),

                    Color.FromArgb(0, 255, 255, 255)

                };

                cb.Positions = new float[] { 0f, 0.45f, 0.55f, 1f };

                shimmerBrush.InterpolationColors = cb;



                foreach (var bone in boneItems)

                {

                    if (sweepRect.Right > bone.Rect.Left && sweepRect.Left < bone.Rect.Right)

                    {

                        using (GraphicsPath itemPath = CreateRoundedRectanglePath(bone.Rect, bone.Radius))

                        {

                            g.FillPath(shimmerBrush, itemPath);

                        }

                    }

                }

            }

        }



        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)

        {

            GraphicsPath path = new GraphicsPath();

            float d = radius * 2f;

            if (rect.Width < d) d = rect.Width;

            if (rect.Height < d) d = rect.Height;



            path.AddArc(rect.X, rect.Y, d, d, 180, 90);

            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);

            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);

            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);

            path.CloseFigure();

            return path;

        }



        private static GraphicsPath CreateTopRoundedPath(RectangleF rect, float radius)

        {

            GraphicsPath path = new GraphicsPath();

            float d = radius * 2f;

            if (rect.Width < d) d = rect.Width;

            if (rect.Height < d) d = rect.Height;



            path.AddArc(rect.X, rect.Y, d, d, 180, 90);

            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);

            path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom);

            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);

            path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + radius);

            path.CloseFigure();

            return path;

        }

    }

}


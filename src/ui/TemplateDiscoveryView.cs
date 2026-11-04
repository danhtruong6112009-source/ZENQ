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

        public string SearchQuery { get; set; } = "";
        public bool IsSearchFocused { get; set; } = false;
        public string ActiveCategory { get; set; } = "Tất cả";

        // Hover & Interaction states
        public int HoveredCardIndex { get; private set; } = -1;
        public int HoveredCategoryIndex { get; private set; } = -1;
        public int HoveredQuickCardIndex { get; private set; } = -1;
        public bool HoverBackButton { get; private set; } = false;
        public bool HoverUploadButton { get; private set; } = false;

        // Timing states for Hover Carousel
        private int _activeCarouselCardIndex = -1;
        private DateTime _carouselStartTime = DateTime.MinValue;
        private float _carouselScrollOffset = 0f; // 0.0 = slide 0, 1.0 = slide 1, 2.0 = slide 2
        private int _displayedSlideIndex = 0;
        private bool _hasUnrenderedCards = false;

        // Interactive layout rectangles
        public RectangleF BackButtonRect { get; private set; } = RectangleF.Empty;
        public RectangleF SearchBoxRect { get; private set; } = RectangleF.Empty;
        public RectangleF UploadBtnRect { get; private set; } = RectangleF.Empty;

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

            // 1. Smooth scroll interpolation
            if (Math.Abs(TargetScrollY - SmoothScrollY) > 0.5f)
            {
                SmoothScrollY += (TargetScrollY - SmoothScrollY) * 0.22f;
                ScrollY = SmoothScrollY;
                needsRedraw = true;
            }
            else if (SmoothScrollY != TargetScrollY)
            {
                SmoothScrollY = TargetScrollY;
                ScrollY = SmoothScrollY;
                needsRedraw = true;
            }

            // Hoạt ảnh quét ánh sáng Shimmer cho các Khung Xương (Skeleton) đang chờ render
            if (_hasUnrenderedCards)
            {
                needsRedraw = true;
            }

            // Smooth horizontal scroll cho Mẫu thịnh hành (Trending)
            if (Math.Abs(TrendingTargetScrollX - TrendingScrollX) > 0.5f)
            {
                TrendingScrollX += (TrendingTargetScrollX - TrendingScrollX) * 0.22f;
                needsRedraw = true;
            }
            else if (TrendingScrollX != TrendingTargetScrollX)
            {
                TrendingScrollX = TrendingTargetScrollX;
                needsRedraw = true;
            }

            // Smooth horizontal scroll cho Dành cho bạn (For You)
            if (Math.Abs(ForYouTargetScrollX - ForYouScrollX) > 0.5f)
            {
                ForYouScrollX += (ForYouTargetScrollX - ForYouScrollX) * 0.22f;
                needsRedraw = true;
            }
            else if (ForYouScrollX != ForYouTargetScrollX)
            {
                ForYouScrollX = ForYouTargetScrollX;
                needsRedraw = true;
            }

            // 2. Hover Carousel Timing Animation
            // User spec: "khi re echuootj vô nó cuộn qua phải khoảng 3 slide mỗi 3s cuộn 1 cái và hết 3 slide có hiệu ứng cuộn lại mượt slide đầu dừng 5s và lặp lại cuộn khi chuột vẫn rê ở đó"
            if (_activeCarouselCardIndex >= 0)
            {
                float elapsed = (float)(DateTime.UtcNow - _carouselStartTime).TotalSeconds;

                // Cycle structure:
                // Slide 0: 0.0s -> 3.0s (3s)
                // Scroll 0->1: 3.0s -> 3.35s (0.35s smooth easeInOut)
                // Slide 1: 3.35s -> 6.35s (3s)
                // Scroll 1->2: 6.35s -> 6.70s (0.35s smooth easeInOut)
                // Slide 2: 6.70s -> 9.70s (3s)
                // Rewind 2->0: 9.70s -> 10.30s (0.60s smooth rewind easeInOut)
                // Slide 0 pause: 10.30s -> 15.30s (5.0s pause at slide 0)
                // Total cycle = 15.30s -> repeat
                float cycleLen = 15.30f;
                if (elapsed >= cycleLen)
                {
                    _carouselStartTime = DateTime.UtcNow;
                    elapsed = 0f;
                }

                float prevOffset = _carouselScrollOffset;
                int prevDisp = _displayedSlideIndex;

                if (elapsed < 3.0f)
                {
                    _carouselScrollOffset = 0f;
                    _displayedSlideIndex = 0;
                }
                else if (elapsed < 3.35f)
                {
                    float p = (elapsed - 3.0f) / 0.35f;
                    _carouselScrollOffset = EaseInOutQuad(p); // 0.0 -> 1.0
                    _displayedSlideIndex = (p < 0.5f) ? 0 : 1;
                }
                else if (elapsed < 6.35f)
                {
                    _carouselScrollOffset = 1.0f;
                    _displayedSlideIndex = 1;
                }
                else if (elapsed < 6.70f)
                {
                    float p = (elapsed - 6.35f) / 0.35f;
                    _carouselScrollOffset = 1.0f + EaseInOutQuad(p); // 1.0 -> 2.0
                    _displayedSlideIndex = (p < 0.5f) ? 1 : 2;
                }
                else if (elapsed < 9.70f)
                {
                    _carouselScrollOffset = 2.0f;
                    _displayedSlideIndex = 2;
                }
                else if (elapsed < 10.30f)
                {
                    float p = (elapsed - 9.70f) / 0.60f;
                    // Cuộn lại mượt mà về slide đầu (2.0 -> 0.0)
                    _carouselScrollOffset = 2.0f * (1.0f - EaseInOutCubic(p));
                    _displayedSlideIndex = 0;
                }
                else
                {
                    // Dừng 5s tại slide đầu!
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
                if (_carouselScrollOffset != 0f)
                {
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

            g.PushClip(new RectangleF(x, viewY, width, viewH));

            float scrollPixelY = MathF.Round(SmoothScrollY);
            float curY = viewY - scrollPixelY;

            // 2.1 Section Title & Upload Action Bar (Đã bỏ ảnh 1 Quick Cards & ảnh 2 Category Pills bị che)
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

            using (SolidBrush sbBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
            using (Pen sbPen = new Pen(IsSearchFocused ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), IsSearchFocused ? 1.5f : 1f))
            {
                g.FillRoundedRectangle(sbBg, SearchBoxRect, 19f);
                g.DrawRoundedRectangle(sbPen, SearchBoxRect, 19f);
            }
            using (Font sf = new Font("Segoe UI", 9.2f, FontStyle.Regular))
            {
                float sx = SearchBoxRect.X + 38f;
                float sy = SearchBoxRect.Y + 9f;
                if (string.IsNullOrEmpty(SearchQuery))
                {
                    using (SolidBrush ph = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        g.DrawString("Tìm kiếm trong kho mẫu thuyết trình, báo cáo...", sf, ph, sx, sy);
                    }
                }
                else
                {
                    using (SolidBrush tx = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        g.DrawString(SearchQuery, sf, tx, sx, sy);
                    }
                }
                // Biểu tượng kính lúp Fluent Windows 11
                FluentIconRenderer.DrawGlyphD2D(g, '\uE721', new RectangleF(SearchBoxRect.X + 12f, SearchBoxRect.Y + 10f, 18f, 18f), Color.FromArgb(100, 116, 139), 12f);

                // Nút xóa tìm kiếm nếu có nội dung
                if (!string.IsNullOrEmpty(SearchQuery))
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

                    float textW = (cat.Length * 9.5f) + 28f;
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
                        g.DrawString(cat, pf, tb, r, sf);
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
                        g.DrawString(card.Title, titleF, wb, cx + 14f, y + 16f);
                        g.DrawString(card.Subtitle, subF, swb, cx + 14f, y + 42f);
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
                g.DrawString("Khám phá kho mẫu ZenQ", hf, hb, rx, ry);
            }

            // Nút "+ Tải mẫu lên" được chuyển xuống tiêu đề mục một cách gọn gàng, tinh tế
            UploadBtnRect = new RectangleF(MathF.Round(rx + width - 140f), MathF.Round(ry + 2f), 140f, 36f);
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
                g.DrawString("+ Tải mẫu lên", uf, ut, UploadBtnRect, usf);
            }

            return ry + 46f;
        }

        private List<SlideTemplateItem> GetTrendingTemplates(List<SlideTemplateItem> all)
        {
            if (all == null || all.Count == 0) return new List<SlideTemplateItem>();
            if (all.Count <= 10) return new List<SlideTemplateItem>(all);

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
            if (all.Count <= 10) return new List<SlideTemplateItem>(all);

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
                DrawSingleTemplateCard(g, cardRect, items[i], cardIdx, isHover);
            }

            int totalRows = (items.Count + cols - 1) / cols;
            return y + totalRows * (cardH + spacing);
        }

        private void DrawSingleTemplateCard(D2DGraphics g, RectangleF cardRect, SlideTemplateItem tmpl, int cardIndex, bool isHover)
        {
            // 1. Thẻ nền bo góc 14px chứa Thumbnail
            using (GraphicsPath cardClip = CreateRoundedRectanglePath(cardRect, 14f))
            {
                g.PushClip(cardClip);

                float offset = (HoveredCardIndex == cardIndex && _activeCarouselCardIndex == cardIndex) ? _carouselScrollOffset : 0f;
                int slideCount = tmpl.slideCount > 0 ? tmpl.slideCount : 1;

                // Vẽ các slide cuộn ngang khi hover
                int baseSlide = (int)Math.Floor(offset);
                float frac = offset - baseSlide;

                // Slide hiện tại
                Bitmap bmpCurr = OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, baseSlide % slideCount);
                float slideCurrX = MathF.Round(cardRect.X - frac * cardRect.Width);
                if (bmpCurr != null)
                {
                    g.DrawImage(bmpCurr, slideCurrX, cardRect.Y, cardRect.Width, cardRect.Height);
                }
                else
                {
                    _hasUnrenderedCards = true;
                    DrawSkeletonThumbnail(g, new RectangleF(slideCurrX, cardRect.Y, cardRect.Width, cardRect.Height), isHover, cardIndex);
                }

                // Slide kế tiếp trượt vào từ bên phải
                if (frac > 0.001f && slideCount > 1)
                {
                    int nextSlide = (baseSlide + 1) % slideCount;
                    Bitmap bmpNext = OnlineSlideTemplateManager.GetSlideBitmap(tmpl.id, nextSlide);
                    float slideNextX = MathF.Round(cardRect.X + (1.0f - frac) * cardRect.Width);
                    if (bmpNext != null)
                    {
                        g.DrawImage(bmpNext, slideNextX, cardRect.Y, cardRect.Width, cardRect.Height);
                    }
                    else
                    {
                        _hasUnrenderedCards = true;
                        DrawSkeletonThumbnail(g, new RectangleF(slideNextX, cardRect.Y, cardRect.Width, cardRect.Height), isHover, cardIndex);
                    }
                }

                // 2. Carousel Dots Indicator ở đáy thumbnail (nếu nhiều hơn 1 slide)
                if (slideCount > 1)
                {
                    float dotH = 6f;
                    float dotY = cardRect.Bottom - 16f;
                    float totalDotsW = slideCount * 12f + 16f;
                    float dotStartX = cardRect.X + (cardRect.Width - totalDotsW) / 2f;

                    for (int d = 0; d < slideCount; d++)
                    {
                        int activeIdx = (HoveredCardIndex == cardIndex && _activeCarouselCardIndex == cardIndex) ? _displayedSlideIndex : 0;
                        bool isDotActive = (d == activeIdx);
                        float dotW = isDotActive ? 18f : 6f;

                        RectangleF dotR = new RectangleF(dotStartX, dotY, dotW, dotH);
                        using (SolidBrush dotB = new SolidBrush(isDotActive ? Color.White : Color.FromArgb(140, 255, 255, 255)))
                        {
                            g.FillRoundedRectangle(dotB, dotR, 3f);
                        }
                        dotStartX += dotW + 6f;
                    }

                    // 3. Slide Counter Badge ở góc trên phải của Thumbnail
                    int activeCounter = (HoveredCardIndex == cardIndex && _activeCarouselCardIndex == cardIndex) ? (_displayedSlideIndex + 1) : 1;
                    RectangleF counterRect = new RectangleF(cardRect.Right - 54f, cardRect.Y + 10f, 44f, 22f);
                    using (SolidBrush cntBg = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                    {
                        g.FillRoundedRectangle(cntBg, counterRect, 11f);
                    }
                    using (Font cntFont = new Font("Segoe UI", 7.8f, FontStyle.Bold))
                    using (SolidBrush cntTxt = new SolidBrush(Color.White))
                    {
                        StringFormat csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString($"{activeCounter}/{slideCount}", cntFont, cntTxt, counterRect, csf);
                    }
                }

                g.PopClip();
            }

            // 4. Viền bo góc 14px (sáng viền indigo khi hover)
            using (Pen cardBorder = new Pen(isHover ? Color.FromArgb(99, 102, 241) : Color.FromArgb(226, 232, 240), isHover ? 2f : 1f))
            {
                g.DrawRoundedRectangle(cardBorder, cardRect, 14f);
            }
        }

        private float DrawTemplatesGrid(D2DGraphics g, float x, float y, float width, int mouseX, int mouseY)
        {
            _templateCardRects.Clear();
            _templateCardItems.Clear();
            _templateUseBtnRects.Clear();
            _hasUnrenderedCards = false;

            var list = OnlineSlideTemplateManager.Templates;

            // Nếu kho rỗng -> hiển thị duy nhất 1 dòng lịch sự
            if (list == null || list.Count == 0)
            {
                using (Font emptyFont = new Font("Segoe UI", 11f, FontStyle.Regular))
                using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.DrawString("Hiện tại chưa có mẫu khả dụng", emptyFont, emptyBrush, x, y + 20f);
                }
                return y + 60f;
            }

            // A. KHI ĐANG TÌM KIẾM THEO TỪ KHÓA
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                string q = SearchQuery.ToLowerInvariant();
                var searchList = list.FindAll(t => (t.title ?? "").ToLowerInvariant().Contains(q) || (t.subtitle ?? "").ToLowerInvariant().Contains(q));

                using (Font secFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))
                using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString($"Kết quả tìm kiếm cho: \"{SearchQuery}\"", secFont, secBrush, x, y);
                }

                if (searchList.Count == 0)
                {
                    using (Font emptyFont = new Font("Segoe UI", 11f, FontStyle.Regular))
                    using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                    {
                        g.DrawString("Hiện tại chưa có mẫu khả dụng", emptyFont, emptyBrush, x, y + 42f);
                    }
                    return y + 80f;
                }

                return DrawGridCards(g, searchList, x, y + 36f, width);
            }

            // B. KHI Ở TRẠNG THÁI KHÁM PHÁ MẪU MẶC ĐỊNH
            float curY = y;

            // 1. DANH MỤC: MẪU THỊNH HÀNH (Trending)
            // Lấy khoảng 10 mẫu ngẫu nhiên từ top 10-15 mẫu thịnh hành dựa vào độ sử dụng & seed ngày
            var trendingList = GetTrendingTemplates(list);
            if (trendingList != null && trendingList.Count > 0)
            {
                curY = DrawHorizontalCarouselSection(
                    g,
                    "Mẫu thịnh hành",
                    trendingList,
                    x,
                    curY,
                    width,
                    ref TrendingScrollX,
                    TrendingTargetScrollX,
                    out TrendingMaxScrollX,
                    out TrendingLeftBtnRect,
                    out TrendingRightBtnRect,
                    HoverTrendingLeftBtn,
                    HoverTrendingRightBtn);
            }

            // 2. DANH MỤC: DÀNH CHO BẠN (For You)
            // Đề xuất mẫu dựa theo từ khóa, chuyên mục người dùng hay sử dụng trong tài liệu gần đây
            var forYouList = GetForYouTemplates(list);
            if (forYouList != null && forYouList.Count > 0)
            {
                curY = DrawHorizontalCarouselSection(
                    g,
                    "Dành cho bạn",
                    forYouList,
                    x,
                    curY,
                    width,
                    ref ForYouScrollX,
                    ForYouTargetScrollX,
                    out ForYouMaxScrollX,
                    out ForYouLeftBtnRect,
                    out ForYouRightBtnRect,
                    HoverForYouLeftBtn,
                    HoverForYouRightBtn);
            }

            // 3. DANH MỤC: TẤT CẢ MẪU (All Templates Grid)
            using (Font secFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (SolidBrush secBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Tất cả mẫu", secFont, secBrush, x, curY);
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

        public bool HandleMouseMove(int mouseX, int mouseY)
        {
            int prevHoverCard = HoveredCardIndex;
            int prevHoverCat = HoveredCategoryIndex;
            int prevHoverQuick = HoveredQuickCardIndex;
            bool prevBack = HoverBackButton;
            bool prevUp = HoverUploadButton;
            bool prevTLeft = HoverTrendingLeftBtn;
            bool prevTRight = HoverTrendingRightBtn;
            bool prevFLeft = HoverForYouLeftBtn;
            bool prevFRight = HoverForYouRightBtn;

            HoverBackButton = BackButtonRect.Contains(mouseX, mouseY);
            HoverUploadButton = UploadBtnRect.Contains(mouseX, mouseY);
            HoverTrendingLeftBtn = (!TrendingLeftBtnRect.IsEmpty && TrendingLeftBtnRect.Contains(mouseX, mouseY));
            HoverTrendingRightBtn = (!TrendingRightBtnRect.IsEmpty && TrendingRightBtnRect.Contains(mouseX, mouseY));
            HoverForYouLeftBtn = (!ForYouLeftBtnRect.IsEmpty && ForYouLeftBtnRect.Contains(mouseX, mouseY));
            HoverForYouRightBtn = (!ForYouRightBtnRect.IsEmpty && ForYouRightBtnRect.Contains(mouseX, mouseY));

            HoveredCategoryIndex = -1;
            HoveredQuickCardIndex = -1;

            HoveredCardIndex = -1;
            for (int i = 0; i < _templateCardRects.Count; i++)
            {
                if (_templateCardRects[i].Contains(mouseX, mouseY))
                {
                    HoveredCardIndex = i;
                    break;
                }
            }

            // Carousel Hover State Machine Trigger
            if (HoveredCardIndex >= 0)
            {
                if (_activeCarouselCardIndex != HoveredCardIndex)
                {
                    _activeCarouselCardIndex = HoveredCardIndex;
                    _carouselStartTime = DateTime.UtcNow;
                    _carouselScrollOffset = 0f;
                    _displayedSlideIndex = 0;
                }
            }
            else
            {
                _activeCarouselCardIndex = -1;
            }

            return (prevHoverCard != HoveredCardIndex || prevHoverCat != HoveredCategoryIndex ||
                    prevHoverQuick != HoveredQuickCardIndex || prevBack != HoverBackButton ||
                    prevUp != HoverUploadButton || prevTLeft != HoverTrendingLeftBtn ||
                    prevTRight != HoverTrendingRightBtn || prevFLeft != HoverForYouLeftBtn ||
                    prevFRight != HoverForYouRightBtn);
        }

        public bool HandleMouseWheel(int delta)
        {
            TargetScrollY -= (delta / 120f) * 110f;
            if (TargetScrollY < 0f) TargetScrollY = 0f;
            if (TargetScrollY > MaxScrollY) TargetScrollY = MaxScrollY;
            return true;
        }

        public OfficeAppMode? HandleClick(int mouseX, int mouseY, out string openFilePath, out string docTitle, out bool exitToHome)
        {
            openFilePath = null;
            docTitle = null;
            exitToHome = false;

            // 1. Mũi tên trở về DUY NHẤT (Quay về Trang chủ)
            if (BackButtonRect.Contains(mouseX, mouseY))
            {
                exitToHome = true;
                return null;
            }

            // 1.1 Tìm kiếm mẫu (Search Box)
            if (SearchBoxRect.Contains(mouseX, mouseY))
            {
                if (!string.IsNullOrEmpty(SearchQuery) && mouseX >= SearchBoxRect.Right - 28f)
                {
                    SearchQuery = "";
                    IsSearchFocused = true;
                    return null;
                }
                IsSearchFocused = true;
                return null;
            }
            else
            {
                IsSearchFocused = false;
            }

            // (Đã loại bỏ hoàn toàn Quick Categories và Category Pills)

            // 4. Nút "Tải mẫu lên" (+ Thêm mẫu tùy chỉnh)
            if (UploadBtnRect.Contains(mouseX, mouseY))
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn bài thuyết trình mẫu (PowerPoint PPTX / ZenQ Slides)";
                    ofd.Filter = "Tệp thuyết trình (*.pptx;*.zenq)|*.pptx;*.zenq|Tất cả tệp (*.*)|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        var customTmpl = OnlineSlideTemplateManager.AddCustomTemplateFromPptx(ofd.FileName);
                        if (customTmpl != null)
                        {
                            ActiveCategory = "Tất cả";
                            TargetScrollY = 0f;
                            SmoothScrollY = 0f;
                        }
                    }
                }
                return null;
            }

            // 4.1 Nút mũi tên lướt ngang Mẫu thịnh hành & Dành cho bạn
            float step = 280f + 16f;
            if (!TrendingLeftBtnRect.IsEmpty && TrendingLeftBtnRect.Contains(mouseX, mouseY))
            {
                TrendingTargetScrollX = Math.Max(0f, TrendingTargetScrollX - step * 2f);
                return null;
            }
            if (!TrendingRightBtnRect.IsEmpty && TrendingRightBtnRect.Contains(mouseX, mouseY))
            {
                TrendingTargetScrollX = Math.Min(TrendingMaxScrollX, TrendingTargetScrollX + step * 2f);
                return null;
            }
            if (!ForYouLeftBtnRect.IsEmpty && ForYouLeftBtnRect.Contains(mouseX, mouseY))
            {
                ForYouTargetScrollX = Math.Max(0f, ForYouTargetScrollX - step * 2f);
                return null;
            }
            if (!ForYouRightBtnRect.IsEmpty && ForYouRightBtnRect.Contains(mouseX, mouseY))
            {
                ForYouTargetScrollX = Math.Min(ForYouMaxScrollX, ForYouTargetScrollX + step * 2f);
                return null;
            }

            // 5. Chọn thẻ mẫu (Mở mẫu trong ZenQ Slides)
            for (int i = 0; i < _templateCardRects.Count; i++)
            {
                if (_templateCardRects[i].Contains(mouseX, mouseY) && i < _templateCardItems.Count)
                {
                    var tmpl = _templateCardItems[i];
                    string pptxPath = OnlineSlideTemplateManager.EnsureTemplatePptx(tmpl);
                    if (!string.IsNullOrEmpty(pptxPath) && File.Exists(pptxPath))
                    {
                        openFilePath = pptxPath;
                        docTitle = $"Mẫu - {tmpl.title}.pptx";
                        return OfficeAppMode.Slides;
                    }
                }
            }

            return null;
        }
                private void DrawSkeletonThumbnail(D2DGraphics g, RectangleF thumbRect, bool isHover, int cardIndex)
        {
            // 1. Phông nền xám đơn giản, hiện đại (hoàn toàn không có lưới ca rô theo yêu cầu)
            using (SolidBrush baseBg = new SolidBrush(Color.FromArgb(243, 244, 246))) // #F3F4F6
            {
                g.FillRectangle(baseBg, thumbRect);
            }

            // 2. Các đường gạch tượng trưng cho tiêu đề, dòng chữ & bố cục slide
            float leftX = thumbRect.X + 20f;
            float topY = thumbRect.Y + 22f;

            // Đường gạch thanh tiêu đề chính (Title Line)
            RectangleF titleBone = new RectangleF(leftX, topY, thumbRect.Width * 0.46f, 15f);

            // Đường gạch tiêu đề phụ (Subtitle Line)
            RectangleF subBone = new RectangleF(leftX, topY + 24f, thumbRect.Width * 0.30f, 10f);

            // Các đường gạch tượng trưng cho chữ nội dung (Content Text Lines)
            RectangleF line1 = new RectangleF(leftX, topY + 44f, thumbRect.Width * 0.40f, 8f);
            RectangleF line2 = new RectangleF(leftX, topY + 58f, thumbRect.Width * 0.35f, 8f);
            RectangleF line3 = new RectangleF(leftX, topY + 72f, thumbRect.Width * 0.25f, 8f);

            // Khung đồ họa / hình ảnh minh họa bên phải
            float rightBoxW = thumbRect.Width * 0.36f;
            float rightBoxH = thumbRect.Height * 0.58f;
            RectangleF rightBox = new RectangleF(thumbRect.Right - rightBoxW - 20f, thumbRect.Y + (thumbRect.Height - rightBoxH) / 2f, rightBoxW, rightBoxH);

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
            // Vệt sáng quét mượt mà từ trái qua phải, chiếu sáng qua các đường gạch chữ và khung hình
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

                // Ánh sáng quét chỉ chiếu rực lên bề mặt các đường gạch chữ và khung hình (chuẩn YouTube)
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